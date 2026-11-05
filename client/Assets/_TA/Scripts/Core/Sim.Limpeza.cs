using System;
using System.Collections.Generic;
using System.Text;

namespace TA.Core
{
    /// <summary>Area publica com sujeira (frente F). As 6 primeiras tem o indice da Estacao da sala; Banheiro e' a sala nova. Indice vai no save:
    /// so cresce no fim.</summary>
    public enum Area { CheckIn, Seguranca, Loja, Portao, Bagagem, Alimentacao, Banheiro }

    /// <summary>O que o funcionario de limpeza esta fazendo (a tela anda e esfrega por isto). Indice vai no save.</summary>
    public enum Faxina { Livre, Indo, Limpando }

    /// <summary>Funcionario de limpeza (Direcao §10). Id = ordem de contratacao. Em = a area da tarefa (ou onde terminou a ultima), De = de onde saiu.</summary>
    public sealed class Zelador
    {
        public int Id;
        public Area Em = Area.Banheiro, De = Area.Banheiro;
        public Faxina Fase;
        /// <summary>Passos que faltam da caminhada e o total dela (a tela: 1 - Andando/AndarTotal do caminho ja feito).</summary>
        public int Andando, AndarTotal;
        /// <summary>A tarefa veio de uma ordem do jogador (Sim.Limpar), nao da escala automatica.</summary>
        public bool Ordem;
    }

    /// <summary>O banheiro do terminal (lado do embarque): quem esta indo, a fila na porta, quem esta nas cabines e quem volta ao portao. Resto do
    /// passageiro = passos que faltam da caminhada ou do uso.</summary>
    public sealed class Banheiro
    {
        public int Cabines;
        public readonly List<Pax> Indo = new List<Pax>(), Fila = new List<Pax>(), Usando = new List<Pax>(), Voltando = new List<Pax>();
        /// <summary>Fechado para limpeza: a sujeira chegou ao teto e so reabre limpo (Sujeira 0).</summary>
        public bool Interditado;
        /// <summary>Quem precisava e achou fechado (ou foi barrado na fila) desde o jogo novo: o motivo da frente G.</summary>
        public long Negados;
        public int Pessoas => Indo.Count + Fila.Count + Usando.Count + Voltando.Count;
    }

    /// <summary>
    /// Frente F: limpeza e banheiro (GDD §4.1 linha F, Direcao §10 e §22; contrato em docs/CONTRATO_LANCAMENTO.md §3). Tudo sai do relogio do
    /// quadro e da semente: nada sorteado, nada pago com Gems.
    ///  - Banheiro: quem tem Pax.Banheiro (semente) e chega ao portao com folga (volta antes de o embarque abrir, com BanheiroFolga s de sobra) vai
    ///    antes ao banheiro: anda BanheiroAndar s, entra na fila da porta, usa uma das BanheiroCabines por BanheiroUso s e anda de volta ao portao.
    ///    Cada uso suja o banheiro. Na sujeira maxima ele interdita: a fila e quem chega voltam ao portao (Negados) e so reabre limpo.
    ///    Quem nao tem folga vai direto: o banheiro nunca faz perder voo (o voo que fecha leva quem ainda esta la, como numa fila).
    ///  - Sujeira (0 a SujeiraMax por area): cada passageiro atendido suja a sala da estacao (SujaPorPax).
    ///  - Limpeza: a equipe comeca com LimpezaIniciais e contrata ate o teto (LimpezaContratacao, so Cash, pausa com a conta atrasada). Livre, o
    ///    zelador pega a ordem do jogador (Limpar), senao o banheiro interditado, senao a area mais suja acima do LimpezaGatilho que ninguem
    ///    pegou. Anda o caminho da planta (LimpezaTrecho), limpa LimpaPorSeg por s ate zerar e fica livre ali.
    ///  - Salario (SalarioLimpeza) entra na Folha(Despesa.Equipe). A sujeira e a fila ainda nao mexem no Cash: a satisfacao (frente G) le Sujeira,
    ///    Banheiro e Negados. RegraLimpeza = false = o jogo de antes, numero a numero (sem banheiro, sem sujeira, sem equipe de limpeza).
    /// </summary>
    public sealed partial class Sim
    {
        public const int SujeiraMax = 1000;
        public const int Areas = 7;

        /// <summary>Sujeira de cada area (0 a SujeiraMax), na ordem de Area.</summary>
        public readonly int[] Sujeira = new int[Areas];
        public readonly Banheiro Banheiro = new Banheiro();
        public readonly List<Zelador> Limpeza = new List<Zelador>();
        /// <summary>Ordens do jogador que esperam um zelador livre, na ordem.</summary>
        public readonly List<Area> Ordens = new List<Area>();

        /// <summary>O banheiro recebe gente agora.</summary>
        public bool BanheiroAberto => T.RegraLimpeza && !Banheiro.Interditado;
        public int TetoLimpeza => T.LimpezaIniciais + T.LimpezaContratacao.Length;
        public int UsoPassos => Passos(T.BanheiroUso);
        public int BanheiroAndarPassos => Passos(T.BanheiroAndar);

        /// <summary>Area em que o atendimento da estacao suja (-1 = oficina e despacho, sem passageiro).</summary>
        public static int AreaDe(Estacao e) => (int)e < (int)Area.Banheiro ? (int)e : -1;

        /// <summary>Em que area ja ha um zelador com tarefa (indo ou limpando).</summary>
        public bool TemZelador(Area a)
        {
            foreach (Zelador z in Limpeza) if (z.Fase != Faxina.Livre && z.Em == a) return true;
            return false;
        }

        // ------------------------------------------------------------------ planta (a arvore de salas: portao no meio)

        /// <summary>Sala vizinha no caminho ate o portao: check-in -> seguranca -> loja -> portao; bagagem pelo check-in; alimentacao e banheiro saem do portao.</summary>
        static readonly int[] Pai = { 1, 2, 3, -1, 0, 3, 3 };

        /// <summary>Metros do caminho entre duas areas pela planta (soma dos LimpezaTrecho ate o encontro).</summary>
        public float Metros(Area a, Area b)
        {
            int x = (int)a, y = (int)b;
            float m = 0f;
            while (x != y)
            {
                if (Fundo(x) >= Fundo(y)) { m += T.LimpezaTrecho[x]; x = Pai[x]; }
                else { m += T.LimpezaTrecho[y]; y = Pai[y]; }
            }
            return m;
        }

        static int Fundo(int a) { int n = 0; while (Pai[a] >= 0) { a = Pai[a]; n++; } return n; }

        /// <summary>Passos da caminhada do zelador de `a` ate `b`, com o carrinho (no minimo 1 s: pegar o carrinho).</summary>
        public int AndarLimpeza(Area a, Area b) => Passos(1f + Metros(a, b) / T.LimpezaVel);

        // ------------------------------------------------------------------ intencoes

        /// <summary>Custo do proximo zelador (-1 = no teto ou regra desligada). Preco fixo pela ordem.</summary>
        public long CustoLimpeza()
        {
            int i = Limpeza.Count - T.LimpezaIniciais;
            return !T.RegraLimpeza || i < 0 || i >= T.LimpezaContratacao.Length ? -1 : T.LimpezaContratacao[i];
        }

        /// <summary>Contrata um zelador: comeca livre no deposito (banheiro) e pega tarefa no passo seguinte.</summary>
        public bool HireLimpeza()
        {
            long c = CustoLimpeza();
            if (!PodeComprar(c)) return false;
            Cash -= c;
            Limpeza.Add(new Zelador { Id = Limpeza.Count });
            Emit(Ev.LimpezaContratada, Limpeza.Count, (int)c);
            return true;
        }

        /// <summary>Ordem do jogador (Direcao §3.3: a ordem vem antes da escala): o zelador livre mais perto vai agora; sem ninguem livre, a ordem
        /// espera o primeiro que acabar. false = area limpa, ja tem zelador la, ja esta na espera, ou ninguem na equipe.</summary>
        public bool Limpar(Area a)
        {
            if (!T.RegraLimpeza || (int)a < 0 || (int)a >= Areas || Limpeza.Count == 0 || Sujeira[(int)a] == 0 || TemZelador(a) || Ordens.Contains(a)) return false;
            Zelador perto = null;
            foreach (Zelador z in Limpeza)
                if (z.Fase == Faxina.Livre && (perto == null || Metros(z.Em, a) < Metros(perto.Em, a))) perto = z;
            if (perto == null) { Ordens.Add(a); Emit(Ev.LimpezaPedida, (int)a, -1); return true; }
            Emit(Ev.LimpezaPedida, (int)a, perto.Id);
            Vai(perto, a, true);
            return true;
        }

        void Vai(Zelador z, Area a, bool ordem)
        {
            z.De = z.Em;
            z.Em = a;
            z.Fase = Faxina.Indo;
            z.Andando = z.AndarTotal = AndarLimpeza(z.De, a);
            z.Ordem = ordem;
            Emit(Ev.LimpezaTarefa, z.Id, (int)a, ordem ? 1 : 0);
        }

        // ------------------------------------------------------------------ passo

        /// <summary>Sim.Avanca: o atendimento terminado suja a sala da estacao.</summary>
        void Suja(Estacao e)
        {
            int a = AreaDe(e);
            if (T.RegraLimpeza && a >= 0) Suja((Area)a, T.SujaPorPax[a]);
        }

        void Suja(Area a, int n)
        {
            int i = (int)a;
            Sujeira[i] = Math.Min(SujeiraMax, Sujeira[i] + n);
            if (a == Area.Banheiro && Sujeira[i] >= T.SujeiraInterdita && !Banheiro.Interditado) { Banheiro.Interditado = true; Emit(Ev.BanheiroInterditado, 1); }
        }

        /// <summary>Sim.Avanca: quem saiu da seguranca, da loja ou da alimentacao para o portao passa antes no banheiro, se precisa e tem folga.</summary>
        void AoPortao(Pax p, Voo v, int loc)
        {
            if (p.Banheiro && BanheiroAberto && v != null && v.AbrePasso - loc > 2 * BanheiroAndarPassos + UsoPassos + Passos(T.BanheiroFolga))
            {
                p.Resto = p.Total = BanheiroAndarPassos;
                Banheiro.Indo.Add(p);
            }
            else
            {
                if (p.Banheiro && T.RegraLimpeza && Banheiro.Interditado) p.Negado = true;   // frente G: precisava e achou fechado
                St(Estacao.Portao).Fila.Add(p);
            }
        }

        /// <summary>Sim.Fecha: o voo que fechou leva quem ainda estava no banheiro (perdeu, como numa fila; com a folga, so com o portao adiantado).</summary>
        void TiraDoBanheiro(Voo v)
        {
            Predicate<Pax> dele = p => p.Onda == v.Onda && p.Voo == v.Indice;
            Banheiro.Indo.RemoveAll(dele); Banheiro.Fila.RemoveAll(dele); Banheiro.Usando.RemoveAll(dele); Banheiro.Voltando.RemoveAll(dele);
        }

        /// <summary>Passo fixo, depois dos atendimentos: banheiro e equipe de limpeza.</summary>
        void PassoLimpeza()
        {
            if (!T.RegraLimpeza) return;
            Banheiro b = Banheiro;
            for (int i = 0; i < b.Voltando.Count; i++)
                if (--b.Voltando[i].Resto <= 0) { St(Estacao.Portao).Fila.Add(b.Voltando[i]); b.Voltando.RemoveAt(i--); }
            for (int i = 0; i < b.Usando.Count; i++)
                if (--b.Usando[i].Resto <= 0) { VoltaAoPortao(b.Usando[i]); b.Usando.RemoveAt(i--); Suja(Area.Banheiro, T.SujaPorPax[(int)Area.Banheiro]); }
            for (int i = 0; i < b.Indo.Count; i++)
                if (--b.Indo[i].Resto <= 0) { b.Fila.Add(b.Indo[i]); b.Indo.RemoveAt(i--); }
            if (b.Interditado)
            {
                // fechado para limpeza: quem esperava volta ao portao (quem ja estava na cabine termina)
                b.Negados += b.Fila.Count;
                foreach (Pax p in b.Fila) { p.Negado = true; VoltaAoPortao(p); }
                b.Fila.Clear();
            }
            while (b.Usando.Count < b.Cabines && b.Fila.Count > 0)
            {
                Pax p = b.Fila[0];
                b.Fila.RemoveAt(0);
                p.Resto = p.Total = UsoPassos;
                UsaBanheiro(p);
                b.Usando.Add(p);
            }
            foreach (Zelador z in Limpeza)
            {
                if (z.Fase == Faxina.Livre) Escala(z);
                if (z.Fase == Faxina.Indo && --z.Andando <= 0)
                {
                    z.Andando = 0;
                    z.Fase = Faxina.Limpando;
                    Emit(Ev.LimpezaChegou, z.Id, (int)z.Em);
                }
                else if (z.Fase == Faxina.Limpando)
                {
                    int a = (int)z.Em;
                    Sujeira[a] = Math.Max(0, Sujeira[a] - Math.Max(1, T.LimpaPorSeg / Hz));
                    if (Sujeira[a] > 0) continue;
                    z.Fase = Faxina.Livre;
                    z.Ordem = false;
                    Emit(Ev.Limpou, z.Id, a);
                    if (z.Em == Area.Banheiro && b.Interditado) { b.Interditado = false; Emit(Ev.BanheiroInterditado, 0); }
                }
            }
        }

        void VoltaAoPortao(Pax p)
        {
            p.Resto = p.Total = BanheiroAndarPassos;
            Banheiro.Voltando.Add(p);
        }

        /// <summary>A escala automatica do zelador livre: a ordem mais antiga, o banheiro interditado, a area mais suja acima do gatilho (empate: a mais
        /// perto, depois a ordem de Area). Nada para fazer: fica onde esta.</summary>
        void Escala(Zelador z)
        {
            while (Ordens.Count > 0)
            {
                Area o = Ordens[0];
                Ordens.RemoveAt(0);
                if (Sujeira[(int)o] > 0 && !TemZelador(o)) { Vai(z, o, true); return; }
            }
            if (Banheiro.Interditado && !TemZelador(Area.Banheiro)) { Vai(z, Area.Banheiro, false); return; }
            int alvo = -1;
            for (int a = 0; a < Areas; a++)
            {
                if (Sujeira[a] < T.LimpezaGatilho || TemZelador((Area)a)) continue;
                if (alvo < 0 || Sujeira[a] > Sujeira[alvo] || Sujeira[a] == Sujeira[alvo] && Metros(z.Em, (Area)a) < Metros(z.Em, (Area)alvo)) alvo = a;
            }
            if (alvo >= 0) Vai(z, (Area)alvo, false);
        }

        // ------------------------------------------------------------------ save (schema 14)

        /// <summary>Schema 14: a equipe de limpeza (onde esta, fase, passos), a sujeira, o banheiro (interdicao, negados) e as ordens. Quem esta no
        /// banheiro vai em CarregaBanheiro, com as filas da onda.</summary>
        void SalvaLimpeza(StringBuilder sb)
        {
            sb.Append("suj=").Append(string.Join(",", Sujeira)).Append('\n');
            Kv(sb, "bh", Banheiro.Interditado ? 1 : 0);
            Kv(sb, "bhneg", Banheiro.Negados);
            Kv(sb, "zels", Limpeza.Count);
            foreach (Zelador z in Limpeza)
                sb.Append("zel.").Append(z.Id).Append('=').Append((int)z.Em).Append(',').Append((int)z.De).Append(',').Append((int)z.Fase).Append(',')
                  .Append(z.Andando).Append(',').Append(z.AndarTotal).Append(',').Append(z.Ordem ? 1 : 0).Append('\n');
            if (Ordens.Count > 0) sb.Append("lord=").Append(string.Join(",", Ordens.ConvertAll(a => ((int)a).ToString(Ci)))).Append('\n');
            void Lista(string k, List<Pax> l, bool resto)
            {
                if (l.Count > 0) sb.Append(k).Append('=').Append(string.Join(";", l.ConvertAll(p => p.Id.ToString(Ci) + (resto ? ":" + p.Resto.ToString(Ci) : "")))).Append('\n');
            }
            Lista("bh.indo", Banheiro.Indo, true); Lista("bh.fila", Banheiro.Fila, false); Lista("bh.uso", Banheiro.Usando, true); Lista("bh.volta", Banheiro.Voltando, true);
        }

        /// <summary>Le o schema 14, com tudo limitado (lixo vira o padrao). O schema 13 nao tem as chaves: a equipe inicial no deposito, tudo limpo.</summary>
        void CarregaLimpeza(Dictionary<string, string> kv)
        {
            if (!T.RegraLimpeza) return;   // o jogo de antes: sem sujeira e sem equipe de limpeza
            int[] s = Ints(kv, "suj");
            for (int a = 0; a < Areas; a++) Sujeira[a] = a < s.Length ? Math.Max(0, Math.Min(SujeiraMax, s[a])) : 0;
            Banheiro.Interditado = Get(kv, "bh", 0) != 0 && Sujeira[(int)Area.Banheiro] > 0;
            Banheiro.Negados = Math.Max(0, Get(kv, "bhneg", 0));
            if (!kv.ContainsKey("zels")) return;   // schema 13 ou antes: fica a equipe do jogo novo
            Limpeza.Clear();
            int n = (int)Math.Max(0, Math.Min(TetoLimpeza, Get(kv, "zels", 0)));
            for (int i = 0; i < n; i++)
            {
                int[] x = Ints(kv, "zel." + i);
                var z = new Zelador { Id = i };
                if (x.Length >= 6)
                {
                    z.Em = Ar(x[0]); z.De = Ar(x[1]);
                    z.Fase = x[2] == (int)Faxina.Indo || x[2] == (int)Faxina.Limpando ? (Faxina)x[2] : Faxina.Livre;
                    int teto = AndarLimpeza(z.De, z.Em);
                    z.AndarTotal = Math.Max(0, Math.Min(teto, x[4]));
                    z.Andando = z.Fase == Faxina.Indo ? Math.Max(1, Math.Min(teto, x[3])) : 0;
                    z.AndarTotal = Math.Max(z.AndarTotal, z.Andando);
                    z.Ordem = x[5] != 0 && z.Fase != Faxina.Livre;
                }
                Limpeza.Add(z);
            }
            foreach (int a in Ints(kv, "lord"))
                if (a >= 0 && a < Areas && !Ordens.Contains((Area)a) && Ordens.Count < Areas) Ordens.Add((Area)a);
        }

        static Area Ar(int i) => (Area)Math.Max(0, Math.Min(Areas - 1, i));

        /// <summary>Quem esta no banheiro (CarregaOnda, com os `vistos` das filas: ninguem em dois lugares). Lixo no passo vira o teto.</summary>
        void CarregaBanheiro(Dictionary<string, string> kv, HashSet<int> vistos)
        {
            if (!T.RegraLimpeza) return;
            void Le(string k, List<Pax> l, int teto)
            {
                if (!kv.TryGetValue(k, out string v)) return;
                foreach (string tok in v.Split(';'))
                {
                    string[] a = tok.Split(':');
                    Pax p = Valido(L(a[0], -1), vistos);
                    if (p == null) continue;
                    p.Resto = p.Total = teto;
                    if (a.Length == 2) p.Resto = (int)Math.Max(1, Math.Min(teto, L(a[1], teto)));
                    l.Add(p);
                }
            }
            Le("bh.indo", Banheiro.Indo, BanheiroAndarPassos);
            Le("bh.fila", Banheiro.Fila, 0);
            Le("bh.uso", Banheiro.Usando, UsoPassos);
            Le("bh.volta", Banheiro.Voltando, BanheiroAndarPassos);
            while (Banheiro.Usando.Count > Banheiro.Cabines) { Banheiro.Fila.Insert(0, Banheiro.Usando[Banheiro.Usando.Count - 1]); Banheiro.Usando.RemoveAt(Banheiro.Usando.Count - 1); }
        }
    }
}
