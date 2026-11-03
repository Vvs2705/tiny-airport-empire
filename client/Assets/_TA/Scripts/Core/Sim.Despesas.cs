using System;
using System.Collections.Generic;
using System.Text;

namespace TA.Core
{
    /// <summary>De onde veio o Cash do dia (CONTRATO_FASE_C8 §3). Premios = campanha e missoes; Cofre = offline e o rewarded do cofre; Compras = Cash de
    /// produto e de pacote de Gems. Indice vai no save: so cresce no fim.</summary>
    public enum Fonte { Tarifas, Bagagem, Loja, Alimentacao, Bonus, Premios, Cofre, Compras }

    /// <summary>Categorias da conta do dia (CONTRATO_FASE_C8 §2). Indice vai no save: so cresce no fim.</summary>
    public enum Despesa { Equipe, Tripulacao, Patio, Manutencao }

    /// <summary>O relatorio de um dia do jogo: receita por fonte, a conta do fechamento por categoria e quanto dela o caixa pagou.</summary>
    public sealed class DiaConta
    {
        public int Dia;
        public readonly long[] Receitas = new long[8], Despesas = new long[4];
        /// <summary>Pago no fechamento e, se faltou, depois, ate o fechamento seguinte (Sim.Atrasada).</summary>
        public long Pago;
        public long Receita => Soma(Receitas);
        public long Despesa => Soma(Despesas);
        public long Lucro => Receita - Despesa;
        /// <summary>O que o fechamento seguinte perdoou ou ainda vai perdoar: nunca vira divida.</summary>
        public long Falta => Despesa - Pago;

        static long Soma(long[] v) { long s = 0; foreach (long x in v) s += x; return s; }
    }

    /// <summary>
    /// Despesas e relatorio do dia (CONTRATO_FASE_C8). A conta do dia do jogo (DiaJogo: 8 ondas) sai no 1o passo do dia seguinte: salario de quem esta
    /// contratado (perfil, funcao da tripulacao e patio) e manutencao das estacoes ativas pelo nivel. Com o jogo fechado o dia nao vira, entao offline
    /// nunca cobra. Cash nunca fica negativo: o caixa paga o que tem; o que faltou fica Atrasada, pausa contratacoes e melhorias e se paga sozinho
    /// quando o caixa cobrir; o fechamento seguinte perdoa o resto. Nao ha divida, juro nem derrota. Tuning.RegraDespesas = false = so o relatorio.
    /// </summary>
    public sealed partial class Sim
    {
        public const int DiasHistorico = 7;
        /// <summary>Teto de cada numero do relatorio lido do save: a soma das 8 fontes nao estoura.</summary>
        const long TetoConta = 1L << 50;

        /// <summary>O dia em curso (a receita que ja entrou; Despesas fica em zero ate fechar) e os ultimos DiasHistorico fechados, do mais antigo ao mais novo.</summary>
        public DiaConta Hoje { get; private set; } = new DiaConta { Dia = -1 };
        public readonly List<DiaConta> Historico = new List<DiaConta>();
        /// <summary>O que faltou no ultimo fechamento (0 = pago). Enquanto houver, contratacoes e melhorias ficam pausadas.</summary>
        public long Atrasada { get; private set; }
        public bool Pausado => Atrasada > 0;

        /// <summary>Quanto a categoria custa por dia agora (a conta do proximo fechamento, se nada mudar). 0 com a regra desligada.</summary>
        public long Folha(Despesa d)
        {
            if (!T.RegraDespesas) return 0;
            long n = 0;
            switch (d)
            {
                case Despesa.Equipe: foreach (Funcionario f in Staff) n += T.Salario[(int)f.Perfil]; break;
                case Despesa.Tripulacao: foreach (Tripulante t in Tripulacao) n += T.SalarioTrip[(int)t.Funcao]; break;
                case Despesa.Patio: n = (long)EquipePatio * T.SalarioPatio; break;
                default:
                    // ponytail: o despacho nao tem melhoria; as visuais (regra desligada) nao cobram
                    foreach (Station s in Estacoes) if (Ativa(s.Tipo)) n += T.Manutencao[(int)s.Tipo] + (long)T.ManutencaoNivel[(int)s.Tipo] * (s.Vel + s.Cap);
                    break;
            }
            return n;
        }

        public long Folha()
        {
            long n = 0;
            for (int d = 0; d < 4; d++) n += Folha((Despesa)d);
            return n;
        }

        /// <summary>Todo Cash que entra passa aqui, com a fonte (o relatorio do dia). Satura em vez de estourar.</summary>
        internal void Recebe(long v, Fonte f)
        {
            Cash = Cash > long.MaxValue - v ? long.MaxValue : Cash + v;
            Hoje.Receitas[(int)f] += v;
        }

        /// <summary>Comeco do passo fixo: paga a conta atrasada se o caixa cobre e, se o dia virou, fecha o anterior.</summary>
        void PassoDespesas()
        {
            if (Atrasada > 0 && Cash >= Atrasada)
            {
                Cash -= Atrasada;
                DiaConta d = Historico[Historico.Count - 1];
                d.Pago += Atrasada;
                Emit(Ev.ContaQuitada, d.Dia, (int)Math.Min(int.MaxValue, Atrasada));
                Atrasada = 0;
            }
            if (Hoje.Dia < 0) Hoje.Dia = DiaJogo;   // jogo novo ou save sem relatorio: o dia comeca a contar agora
            if (Hoje.Dia == DiaJogo) return;
            for (int d = 0; d < 4; d++) Hoje.Despesas[d] = Folha((Despesa)d);
            long conta = Hoje.Despesa;
            Hoje.Pago = Math.Min(conta, Cash);
            Cash -= Hoje.Pago;
            Atrasada = conta - Hoje.Pago;   // a de ontem que sobrou fica perdoada
            Historico.Add(Hoje);
            if (Historico.Count > DiasHistorico) Historico.RemoveAt(0);
            Emit(Ev.DiaFechado, Hoje.Dia, (int)Math.Min(int.MaxValue, Hoje.Pago), (int)Math.Min(int.MaxValue, Atrasada));
            Hoje = new DiaConta { Dia = DiaJogo };
        }

        /// <summary>Schema 9 (CONTRATO_FASE_C8 §5): a conta atrasada, o dia em curso e os fechados. Linha: dia, pago, 8 receitas, 4 despesas.</summary>
        void SalvaDespesas(StringBuilder sb)
        {
            Kv(sb, "atrasada", Atrasada);
            void Linha(string k, DiaConta c) =>
                sb.Append(k).Append('=').Append(c.Dia).Append(',').Append(c.Pago).Append(',').Append(string.Join(",", c.Receitas)).Append(',').Append(string.Join(",", c.Despesas)).Append('\n');
            Linha("hoje", Hoje);
            for (int i = 0; i < Historico.Count; i++) Linha("dc." + i, Historico[i]);
        }

        /// <summary>Le o schema 9, com tudo limitado. O schema 8 nao tem as chaves: relatorio vazio, nada atrasado, e o dia em curso comeca a contar no 1o passo.</summary>
        void CarregaDespesas(Dictionary<string, string> kv)
        {
            DiaConta Le(string k, int min)
            {
                long[] x = Longs(kv, k);
                if (x.Length < 14 || x[0] < min || x[0] > DiaJogo) return null;
                var c = new DiaConta { Dia = (int)x[0] };
                for (int i = 0; i < 8; i++) c.Receitas[i] = Math.Max(0, Math.Min(TetoConta, x[2 + i]));
                for (int i = 0; i < 4; i++) c.Despesas[i] = Math.Max(0, Math.Min(TetoConta, x[10 + i]));
                c.Pago = Math.Max(0, Math.Min(c.Despesa, x[1]));
                return c;
            }
            Hoje = Le("hoje", 0) ?? new DiaConta { Dia = -1 };
            for (int i = 0; i < DiasHistorico; i++)
                if (Le("dc." + i, Historico.Count > 0 ? Historico[Historico.Count - 1].Dia + 1 : 0) is DiaConta c && (Hoje.Dia < 0 || c.Dia < Hoje.Dia)) Historico.Add(c);
            Atrasada = Historico.Count > 0 ? Math.Max(0, Math.Min(Historico[Historico.Count - 1].Falta, Get(kv, "atrasada", 0))) : 0;
        }
    }
}
