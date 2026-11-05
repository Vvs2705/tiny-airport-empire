using System;
using System.Collections.Generic;
using System.Text;

namespace TA.Core
{
    /// <summary>As regras automaticas de operacao (frente ta-politicas). Indice vai no save e no registro: so cresce no fim.</summary>
    public enum Politica { Hangar, Estoque, Reforco }

    /// <summary>
    /// Politicas do diretor (frente ta-politicas; ideia do codigo de outra IA, refeita no nosso padrao): regras que o jogador liga e regula para tirar
    /// a microgestao repetitiva, sem jogar por ele. Todas desligadas no jogo novo; o X de cada uma sai de uma lista fixa da tabela (Tuning.PoliticaX).
    /// - Hangar: o aviao no patio, antes do embarque, com desgaste >= X% vai ao hangar (a mesma intencao do jogador, com o preco e o voo dela).
    /// - Estoque: o deposito abaixo de PoliticaRepoe% do tanque pede a entrega antes do ponto automatico, ate X Cash por dia de jogo nessas entregas.
    /// - Reforco: a fila mais longa, com X pessoas ou mais e posto livre, recebe 1 funcionario parado da estacao mais calma, pelo vestiario
    ///   (a troca vale), no maximo 1 a cada ReforcoCada s; nunca tira ninguem de estacao fora do normal nem da oficina quando um aviao precisa dela.
    /// Travas: nunca gasta alem do teto, nunca deixa o caixa abaixo da conta do proximo fechamento, nunca com a conta atrasada e nunca os 2 avioes no
    /// hangar. Cada acao entra no registro (TipoOcorrencia.Politica, Motivo.Politica) e sai em Ev.PoliticaAgiu. O reforco nao conta para as missoes de
    /// mover: quem joga e' o jogador. Nada sorteado; tudo desligado = o jogo de antes, numero a numero.
    /// </summary>
    public sealed partial class Sim
    {
        public const int Politicas = 3;

        /// <summary>Por Politica: ligada e o indice do X na lista fixa (T.PoliticaX).</summary>
        public readonly bool[] PolLigada = new bool[Politicas];
        public readonly int[] PolNivel = new int[Politicas];
        /// <summary>Cash das entregas que a politica de estoque pediu no dia de jogo de agora (o teto vale contra ele).</summary>
        public long PolGasto { get; private set; }
        int _polDia = -1;
        long _reforcoEm;
        bool _pelaPolitica;

        /// <summary>O X que vale agora: desgaste (%), teto (Cash por dia) ou fila (pessoas).</summary>
        public int PoliticaX(Politica p) => T.PoliticaX[(int)p][PolNivel[(int)p]];

        void NovasPoliticas() { for (int p = 0; p < Politicas; p++) PolNivel[p] = Math.Max(0, Math.Min(T.PoliticaX[p].Length - 1, T.PoliticaPadrao[p])); }

        /// <summary>Intencao da tela: liga ou desliga. false = politica invalida ou ja estava assim.</summary>
        public bool LigaPolitica(Politica p, bool ligada)
        {
            if ((int)p < 0 || (int)p >= Politicas || PolLigada[(int)p] == ligada) return false;
            PolLigada[(int)p] = ligada;
            Emit(Ev.PoliticaMudou, (int)p, ligada ? 1 : 0, PolNivel[(int)p]);
            return true;
        }

        /// <summary>Intencao da tela: escolhe o X pelo indice na lista fixa.</summary>
        public bool RegulaPolitica(Politica p, int nivel)
        {
            if ((int)p < 0 || (int)p >= Politicas || nivel < 0 || nivel >= T.PoliticaX[(int)p].Length || PolNivel[(int)p] == nivel) return false;
            PolNivel[(int)p] = nivel;
            Emit(Ev.PoliticaMudou, (int)p, PolLigada[(int)p] ? 1 : 0, nivel);
            return true;
        }

        /// <summary>A oficina e' posto critico enquanto um aviao esta pendente, em pane ou em checagem.</summary>
        bool OficinaCritica()
        {
            foreach (Aviao a in Avioes) if (a.Pendente || a.Pane || a.Check > 0) return true;
            return false;
        }

        /// <summary>Um passo, depois do patio. As decisoes saem 1 vez por s de jogo.</summary>
        void PassoPoliticas()
        {
            if (Steps % Hz != 0 || !(PolLigada[0] || PolLigada[1] || PolLigada[2])) return;
            if (_polDia != DiaJogo) { _polDia = DiaJogo; PolGasto = 0; }
            if (PolLigada[(int)Politica.Hangar] && T.RegraManutencao && !Pausado)
                foreach (Aviao a in Avioes)
                {
                    int d = a.Desgaste;
                    // ponytail: um aviao por vez; o outro segue voando enquanto este revisa
                    if (d < PoliticaX(Politica.Hangar) || Avioes[1 - a.Id].Hangar > 0 || Cash - T.HangarCusto < Folha() || !Hangar(a.Id)) continue;
                    Registra(Politica.Hangar, d, T.HangarCusto, Estacao.Manutencao);
                    Emit(Ev.PoliticaAgiu, (int)Politica.Hangar, a.Id, d);
                    break;
                }
            if (PolLigada[(int)Politica.Estoque] && T.RegraSuprimentos)
                for (int i = 0; i < Suprimentos; i++)
                {
                    var s = (Suprimento)i;
                    if (Pedido[i] > 0 || Estoque[i] * 100 >= Tanque(s) * T.PoliticaRepoe || PolGasto + T.SupCusto[i] > PoliticaX(Politica.Estoque)) continue;
                    int antes = Estoque[i];
                    Pede(s);
                    PolGasto += T.SupCusto[i];
                    Registra(Politica.Estoque, antes, T.SupCusto[i], Estacao.Despacho);
                    Emit(Ev.PoliticaAgiu, (int)Politica.Estoque, i, antes);
                }
            if (PolLigada[(int)Politica.Reforco] && Steps >= _reforcoEm) Reforca();
        }

        /// <summary>A fila mais longa (com posto livre) recebe quem esta parado na estacao mais calma.</summary>
        void Reforca()
        {
            Station alvo = null;
            foreach (Station st in Estacoes)
                if (Ativa(st.Tipo) && st.Fila.Count >= PoliticaX(Politica.Reforco) && Alocados(st.Tipo) < st.Postos && (alvo == null || st.Fila.Count > alvo.Fila.Count)) alvo = st;
            if (alvo == null) return;
            Funcionario quem = null;
            Station de = null;
            bool oficina = OficinaCritica();
            foreach (Funcionario f in Staff)
            {
                Station st = St(f.Em);
                if (f.Andando > 0 || st == alvo || st.Estado != Estado.Normal || oficina && f.Em == Estacao.Manutencao) continue;
                if (de == null || st.Folga > de.Folga || st.Folga == de.Folga && st.Pessoas < de.Pessoas) { quem = f; de = st; }
            }
            if (quem == null) return;
            int fila = alvo.Fila.Count;
            _pelaPolitica = true;
            bool foi = Move(quem.Id, alvo.Tipo);
            _pelaPolitica = false;
            if (!foi) return;
            _reforcoEm = Steps + Passos(T.ReforcoCada);
            Registra(Politica.Reforco, fila, 0, alvo.Tipo);
            Emit(Ev.PoliticaAgiu, (int)Politica.Reforco, quem.Id, (int)alvo.Tipo);
        }

        /// <summary>A acao entra no registro de ocorrencias: o que fez (Sub), a causa (desgaste, deposito ou fila), o custo e onde.</summary>
        void Registra(Politica p, int causa, long custo, Estacao onde)
        {
            if (!T.RegraHistorico) return;
            Ocorrencia o = Nova(TipoOcorrencia.Politica, (int)p, -1, Steps);
            o.Causa = Math.Max(0, Math.Min(100, causa)); o.Custo = custo; o.Auto = true; o.Motivo = Motivo.Politica; o.Estacao = onde;
        }

        /// <summary>Schema 18: as 3 ligadas (bits), os 3 niveis, o gasto da reposicao no dia e o passo do proximo reforco. Tudo no padrao: a chave fica de fora.</summary>
        void SalvaPoliticas(StringBuilder sb)
        {
            int bits = (PolLigada[0] ? 1 : 0) | (PolLigada[1] ? 2 : 0) | (PolLigada[2] ? 4 : 0);
            bool padrao = bits == 0 && PolGasto == 0 && _reforcoEm <= Steps;
            for (int p = 0; p < Politicas; p++) padrao &= PolNivel[p] == T.PoliticaPadrao[p];
            if (padrao) return;
            sb.Append("pol=").Append(bits).Append(',').Append(PolNivel[0]).Append(',').Append(PolNivel[1]).Append(',').Append(PolNivel[2])
              .Append(',').Append(PolGasto.ToString(Ci)).Append(',').Append(_polDia).Append(',').Append(_reforcoEm.ToString(Ci)).Append('\n');
        }

        /// <summary>Le o schema 18, com tudo limitado. Os schemas 17 e anteriores nao tem a chave: tudo desligado, no X padrao.</summary>
        void CarregaPoliticas(Dictionary<string, string> kv)
        {
            long[] x = Longs(kv, "pol");
            if (x.Length < 7) return;
            for (int p = 0; p < Politicas; p++)
            {
                PolLigada[p] = (x[0] & 1 << p) != 0;
                PolNivel[p] = (int)Math.Max(0, Math.Min(T.PoliticaX[p].Length - 1, x[1 + p]));
            }
            _polDia = (int)Math.Max(-1, Math.Min(DiaJogo, x[5]));
            PolGasto = _polDia == DiaJogo ? Math.Max(0, Math.Min(PoliticaX(Politica.Estoque), x[4])) : 0;
            _reforcoEm = Math.Max(0, Math.Min(Steps + Passos(T.ReforcoCada), x[6]));
        }
    }
}
