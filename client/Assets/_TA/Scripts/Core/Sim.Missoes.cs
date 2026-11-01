using System;
using System.Collections.Generic;
using System.Text;

namespace TA.Core
{
    /// <summary>Uma missao em curso: a linha da tabela, o contador de cada parte e se ja pagou (paga uma vez).</summary>
    public sealed class Missao
    {
        public Objetivo O;
        public int Indice, Conta, ContaE;
        public bool Feita;
        public bool Cumpriu => Conta >= O.Alvo && (O.E == null || ContaE >= O.E.Alvo);
    }

    /// <summary>
    /// Missoes diarias e semanais e conquistas (CONTRATO_FASE_C6, docs/MISSOES.md). Reaproveita os contadores da campanha: todo Conta passa aqui.
    /// O dia e a semana sao os do jogo (DiaJogo, que sai de Steps): com o jogo fechado nada vira, e faltar um dia nao existe. A virada so troca a lista:
    /// missao aberta sai sem custo. A semente escolhe quais linhas entram, nunca o valor: a recompensa e' a da tabela, fora do Ganha (sem 2x, fora do cofre).
    /// Conquista e' permanente, conta em qualquer aeroporto e nao paga Cash. Tuning.RegraMissoes = false = conta e mostra, sem pagar.
    /// </summary>
    public sealed partial class Sim
    {
        public const int SemanaDias = 7;

        /// <summary>As 3 diarias e as 5 semanais em curso (GDD §6.5). Null antes do 1o passo.</summary>
        public readonly Missao[] Diarias = new Missao[3], Semanais = new Missao[5];

        readonly int[] _marca;
        long _conquistas;
        int _dia = -1, _semana = -1;

        public int SemanaJogo => DiaJogo / SemanaDias;
        public bool ConquistaFeita(int i) => (_conquistas >> i & 1) != 0;

        public int ConquistasFeitas
        {
            get
            {
                int n = 0;
                for (int i = 0; i < T.Conquistas.Length; i++) if (ConquistaFeita(i)) n++;
                return n;
            }
        }

        /// <summary>Onde a conquista esta, para comparar com o Alvo dela. A feita fica no alvo: a sequencia que quebrou depois nao desfaz.</summary>
        public int ProgressoConquista(int i)
        {
            Objetivo o = T.Conquistas[i];
            if (ConquistaFeita(i)) return o.Alvo;
            int n = Mede(o);
            return n >= 0 ? n : _marca[i];
        }

        /// <summary>Fim do passo fixo: conclui e paga, conquista o que chegou no alvo e, se o dia virou, troca a lista.</summary>
        void PassoMissoes()
        {
            Conclui(Diarias, 0);
            Conclui(Semanais, 1);
            for (int i = 0; i < T.Conquistas.Length; i++)
            {
                if (ConquistaFeita(i) || ProgressoConquista(i) < T.Conquistas[i].Alvo) continue;
                _conquistas |= 1L << i;
                Emit(Ev.ConquistaFeita, i);
            }
            if (DiaJogo != _dia) { _dia = DiaJogo; Sorteia(Diarias, T.Diarias, _dia, 40); }
            if (SemanaJogo != _semana) { _semana = SemanaJogo; Sorteia(Semanais, T.Semanais, _semana, 41); }
        }

        void Conclui(Missao[] vagas, int tipo)
        {
            foreach (Missao m in vagas)
            {
                if (m == null || m.Feita || !m.Cumpriu) continue;
                m.Feita = true;
                int pago = T.RegraMissoes ? m.O.Recompensa : 0;
                Cash += pago;
                Emit(Ev.MissaoConcluida, tipo, m.Indice, pago);
            }
        }

        /// <summary>A semente da uma nota por linha e entram as menores que cabem, sem repetir a meta: o conjunto balanceado do GDD §6.5.</summary>
        void Sorteia(Missao[] vagas, Objetivo[] tabela, int periodo, int sal)
        {
            Array.Clear(vagas, 0, vagas.Length);
            for (int v = 0; v < vagas.Length; v++)
            {
                int melhor = -1;
                double menor = 2;
                for (int i = 0; i < tabela.Length; i++)
                {
                    double nota = U(periodo, 0, i, sal);
                    if (nota < menor && Elegivel(tabela[i]) && !Repete(vagas, tabela[i])) { melhor = i; menor = nota; }
                }
                if (melhor >= 0) vagas[v] = new Missao { O = tabela[melhor], Indice = melhor };
            }
        }

        /// <summary>A linha do marco atual, que da para fazer daqui: nenhuma missao pede o que o jogo ainda nao tem.</summary>
        bool Elegivel(Objetivo o) => o.Marco == Marco && Cabe(o) && (o.E == null || Cabe(o.E));

        bool Cabe(Objetivo o)
        {
            switch (o.Meta)
            {
                case Meta.Atendidos: return Ativa((Estacao)o.Arg);
                case Meta.Checagens: case Meta.Hangar: return Ativa(Estacao.Manutencao);
                case Meta.Melhorias: return AVenda() >= o.Alvo;
                case Meta.Voos: case Meta.Cheios: return o.Arg < 0 || RotaAberta(o.Arg) && T.Rotas[o.Arg].Aeroporto == AeroportoAtual;
                default: return true;
            }
        }

        /// <summary>Niveis de melhoria ainda a venda nas estacoes abertas: acabado o catalogo, sai do sorteio.</summary>
        int AVenda()
        {
            int n = 0;
            foreach (Station s in Estacoes)
                if (Custo(s.Tipo, Melhoria.Velocidade) >= 0 || Custo(s.Tipo, Melhoria.Capacidade) >= 0)
                    n += T.VelNiveis - s.Vel + T.CapNiveis - s.Cap;
            return n;
        }

        static bool Repete(Missao[] vagas, Objetivo o)
        {
            foreach (Missao m in vagas) if (m != null && m.O.Meta == o.Meta) return true;
            return false;
        }

        static bool Casa(Objetivo o, Meta m, int arg) => o.Meta == m && (o.Arg < 0 || o.Arg == arg);

        static void Soma(Missao[] vagas, Meta m, int arg)
        {
            foreach (Missao x in vagas)
            {
                if (x == null) continue;
                if (Casa(x.O, m, arg)) x.Conta = Math.Min(x.O.Alvo, x.Conta + 1);
                if (x.O.E != null && Casa(x.O.E, m, arg)) x.ContaE = Math.Min(x.O.E.Alvo, x.ContaE + 1);
            }
        }

        /// <summary>Todo contador da campanha passa aqui, em qualquer aeroporto. Nenhuma missao usa Seguidos: progresso de missao nunca recua.</summary>
        void ContaMissoes(Meta m, int arg, bool zera)
        {
            Soma(Diarias, m, arg);
            Soma(Semanais, m, arg);
            for (int i = 0; i < _marca.Length; i++)
                if (Casa(T.Conquistas[i], m, arg)) _marca[i] = zera ? 0 : Math.Min(T.Conquistas[i].Alvo, _marca[i] + 1);
        }

        /// <summary>Schema 7 (CONTRATO_FASE_C6 §5): a lista em curso, com os contadores, e as conquistas.</summary>
        void SalvaMissoes(StringBuilder sb)
        {
            sb.Append("miss=").Append(_dia).Append(',').Append(_semana).Append('\n');
            void Vagas(string k, Missao[] vagas)
            {
                for (int v = 0; v < vagas.Length; v++)
                    if (vagas[v] is Missao m)
                        sb.Append(k).Append(v).Append('=').Append(m.Indice).Append(',').Append(m.Conta).Append(',').Append(m.ContaE).Append(',').Append(m.Feita ? 1 : 0).Append('\n');
            }
            Vagas("md.", Diarias);
            Vagas("ms.", Semanais);
            sb.Append("cq=").Append(_conquistas).Append(',').Append(string.Join(",", _marca)).Append('\n');
        }

        /// <summary>Le o schema 7, com tudo limitado. O schema 6 nao tem as chaves: a lista sai no 1o passo, contador comeca em zero e o que o estado ja prova conquista.</summary>
        void CarregaMissoes(Dictionary<string, string> kv)
        {
            int[] p = Ints(kv, "miss");
            if (p.Length >= 2) { _dia = Math.Max(-1, Math.Min(DiaJogo, p[0])); _semana = Math.Max(-1, Math.Min(SemanaJogo, p[1])); }
            void Vagas(string k, Missao[] vagas, Objetivo[] tabela)
            {
                for (int v = 0; v < vagas.Length; v++)
                {
                    int[] x = Ints(kv, k + v);
                    if (x.Length < 4 || x[0] < 0 || x[0] >= tabela.Length) continue;
                    Objetivo o = tabela[x[0]];
                    vagas[v] = new Missao { O = o, Indice = x[0], Conta = Math.Max(0, Math.Min(o.Alvo, x[1])), ContaE = o.E == null ? 0 : Math.Max(0, Math.Min(o.E.Alvo, x[2])), Feita = x[3] != 0 };
                }
            }
            Vagas("md.", Diarias, T.Diarias);
            Vagas("ms.", Semanais, T.Semanais);
            long[] c = Longs(kv, "cq");
            if (c.Length == 0) return;
            _conquistas = c[0] & (1L << _marca.Length) - 1;
            for (int i = 0; i < _marca.Length && i + 1 < c.Length; i++) _marca[i] = (int)Math.Max(0, Math.Min(T.Conquistas[i].Alvo, c[i + 1]));
        }
    }
}
