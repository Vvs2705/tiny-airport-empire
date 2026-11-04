using System;
using System.Collections.Generic;
using System.Text;

namespace TA.Core
{
    /// <summary>Os suprimentos do patio (frente ta-suprimentos). Indice vai no save: so cresce no fim.</summary>
    public enum Suprimento { Combustivel, Refeicoes }

    /// <summary>
    /// Combustivel e refeicoes com estoque (frente ta-suprimentos; ideia do codigo de outra IA, refeita no nosso padrao). Cada turnaround leva
    /// Tuning.SupGasto cargas de cada um: o servico do patio que leva o suprimento (Combustivel; Limpeza, que e' o caminhao do catering) roda como antes
    /// e, quando acaba, tira as cargas do deposito. Faltou: o caminhao espera no aviao (Aviao.Carga = CargaEspera) ate a entrega, e so a decolagem
    /// atrasa (Aviao.PatioPronto); o embarque nunca espera suprimento. Reposicao automatica: com o deposito no SupPonto, sai a entrega que enche o
    /// tanque, com prazo e custo fixos da tabela; o custo entra na conta do dia (Despesa.Suprimentos): nao pede Cash na hora, nao vira divida e nunca
    /// falta por falta de caixa. Melhoria: tanque maior (menos entregas por dia). Nada sorteado. Tuning.RegraSuprimentos = false = so visual.
    /// </summary>
    public sealed partial class Sim
    {
        public const int Suprimentos = 2;
        /// <summary>Aviao.Carga: o servico ainda corre (tira do deposito quando acabar) ou acabou e o deposito nao tinha (espera a entrega).</summary>
        public const int CargaPede = 1, CargaEspera = 2;

        /// <summary>Por Suprimento: cargas no deposito, cargas da entrega a caminho (0 = nenhuma), o passo em que ela chega e o nivel do tanque.</summary>
        public readonly int[] Estoque = new int[Suprimentos], Pedido = new int[Suprimentos], TanqueNivel = new int[Suprimentos];
        public readonly long[] Entrega = new long[Suprimentos];
        /// <summary>Vezes que um aviao ficou esperando suprimento (o bot e a medida leem; nao vai no save).</summary>
        public int FaltasSuprimento;

        /// <summary>O servico do patio que leva o suprimento (o caminhao que espera quando falta).</summary>
        public static Servico ServicoDe(Suprimento s) => s == Suprimento.Combustivel ? Servico.Combustivel : Servico.Limpeza;
        public int Tanque(Suprimento s) => T.SupTanque[(int)s] + T.SupTanqueMais[(int)s] * TanqueNivel[(int)s];
        public int Gasto(int aviao, Suprimento s) => T.SupGasto[(int)s][aviao];
        public int PrazoPassos(Suprimento s) => Passos(T.SupPrazo[(int)s]);
        /// <summary>Passos ate a entrega a caminho chegar (-1 = nenhuma a caminho).</summary>
        public long EntregaEm(Suprimento s) => Pedido[(int)s] > 0 ? Math.Max(0, Entrega[(int)s] - Steps) : -1;
        public bool Esperando(int aviao, Suprimento s) => aviao >= 0 && aviao < Avioes.Length && Avioes[aviao].Carga[(int)s] == CargaEspera;

        /// <summary>Aviso antes de faltar: o 1o aviao, pela ordem de chegada, que vai pedir mais do que o deposito tem antes de a entrega chegar (no
        /// patio sem a carga, ou voando e pousando antes da entrega). -1 = ninguem.</summary>
        public int VaiFaltar(Suprimento s)
        {
            if (!T.RegraSuprimentos) return -1;
            int i = (int)s, sobra = Estoque[i];
            foreach (Aviao a in PorChegada())
            {
                bool pede = a.NoPatio ? a.Carga[i] > 0 : a.Hangar == 0 && a.Chega >= 0 && (Pedido[i] == 0 || a.Chega < Entrega[i]);
                if (pede && (sobra -= Gasto(a.Id, s)) < 0) return a.Id;
            }
            return -1;
        }

        Aviao[] PorChegada() => Avioes[1].Chega < Avioes[0].Chega ? new[] { Avioes[1], Avioes[0] } : Avioes;

        /// <summary>Um passo, logo depois dos servicos: a entrega que chegou enche o deposito, quem terminou o servico carrega (na ordem de chegada)
        /// e o deposito no ponto pede a entrega.</summary>
        void PassoSuprimentos()
        {
            if (!T.RegraSuprimentos) return;
            for (int i = 0; i < Suprimentos; i++)
                if (Pedido[i] > 0 && Steps >= Entrega[i])
                {
                    int n = Tanque((Suprimento)i) - Estoque[i];   // enche o tanque de agora, com o que se gastou no caminho e o tanque novo
                    Estoque[i] += n;
                    Emit(Ev.SuprimentoChegou, i, n);
                    Pedido[i] = 0; Entrega[i] = 0;
                }
            foreach (Aviao a in PorChegada())
            {
                if (!a.NoPatio) continue;
                for (int i = 0; i < Suprimentos; i++)
                {
                    if (a.Carga[i] == 0 || a.Falta[(int)ServicoDe((Suprimento)i)] > 0) continue;
                    int g = Gasto(a.Id, (Suprimento)i);
                    if (Estoque[i] < g)
                    {
                        if (a.Carga[i] == CargaPede) { a.Carga[i] = CargaEspera; FaltasSuprimento++; Emit(Ev.SuprimentoFaltou, a.Id, i); }
                        continue;
                    }
                    Estoque[i] -= g;
                    a.Carga[i] = 0;
                    // o servico acabou neste passo ou antes: o turnaround fecha aqui quando a carga era o que faltava
                    if (a.PatioPronto) Emit(Ev.TurnaroundFeito, a.Id, a.VooId >= 0 ? a.VooId : -1, a.FechouEm >= 0 ? 1 : 0);
                }
            }
            for (int i = 0; i < Suprimentos; i++)
                if (Pedido[i] == 0 && Estoque[i] <= T.SupPonto[i]) Pede((Suprimento)i);
        }

        /// <summary>A entrega enche o tanque: prazo e custo fixos, e o custo vai para a conta de hoje.</summary>
        void Pede(Suprimento s)
        {
            int i = (int)s, n = Tanque(s) - Estoque[i];
            if (n <= 0) return;
            Pedido[i] = n;
            Entrega[i] = Steps + PrazoPassos(s);
            Gasta(T.SupCusto[i], Despesa.Suprimentos);
            Emit(Ev.SuprimentoPedido, i, n, T.SupCusto[i]);
        }

        /// <summary>Custo do proximo nivel do tanque (-1 = no maximo).</summary>
        public long CustoTanque(Suprimento s)
        {
            if ((int)s < 0 || (int)s >= Suprimentos) return -1;
            int n = TanqueNivel[(int)s];
            return n >= T.SupTanqueCusto.Length ? -1 : T.SupTanqueCusto[n];
        }

        /// <summary>Tanque maior: +SupTanqueMais cargas, a proxima entrega ja enche ate o novo tamanho. Pausa com a conta atrasada, como toda melhoria.</summary>
        public bool BuyTanque(Suprimento s)
        {
            long c = CustoTanque(s);
            if (!PodeComprar(c)) return false;
            Cash -= c;
            TanqueNivel[(int)s]++;
            Emit(Ev.SuprimentoMelhorou, (int)s, (int)Melhoria.Capacidade, TanqueNivel[(int)s]);
            return true;
        }

        /// <summary>Schema 15: por suprimento, deposito, entrega a caminho (cargas e passo) e nivel do tanque; por aviao, a carga que falta (o padrao fica de fora).</summary>
        void SalvaSuprimentos(StringBuilder sb)
        {
            for (int i = 0; i < Suprimentos; i++)
                sb.Append("sup.").Append(i).Append('=').Append(Estoque[i]).Append(',').Append(Pedido[i]).Append(',').Append(Entrega[i].ToString(Ci))
                  .Append(',').Append(TanqueNivel[i]).Append('\n');
            foreach (Aviao a in Avioes)
                if (a.Carga[0] != 0 || a.Carga[1] != 0) sb.Append("carga.").Append(a.Id).Append('=').Append(a.Carga[0]).Append(',').Append(a.Carga[1]).Append('\n');
        }

        /// <summary>Le o schema 15 depois dos avioes, com tudo limitado. Os schemas 14 e anteriores nao tem as chaves: deposito cheio, nada a caminho e
        /// nenhum aviao devendo carga neste turnaround.</summary>
        void CarregaSuprimentos(Dictionary<string, string> kv)
        {
            for (int i = 0; i < Suprimentos; i++)
            {
                long[] x = Longs(kv, "sup." + i);
                if (x.Length < 4) continue;
                TanqueNivel[i] = (int)Math.Max(0, Math.Min(T.SupTanqueCusto.Length, x[3]));
                int tanque = Tanque((Suprimento)i);
                Estoque[i] = (int)Math.Max(0, Math.Min(tanque, x[0]));
                Pedido[i] = (int)Math.Max(0, Math.Min(tanque - Estoque[i], x[1]));
                Entrega[i] = Pedido[i] > 0 ? Math.Max(Steps, Math.Min(Steps + PrazoPassos((Suprimento)i), x[2])) : 0;
            }
            foreach (Aviao a in Avioes)
            {
                int[] c = Ints(kv, "carga." + a.Id);
                for (int i = 0; i < Suprimentos; i++)
                    a.Carga[i] = !a.NoPatio || !T.RegraSuprimentos || i >= c.Length || c[i] <= 0 ? 0
                        : a.Falta[(int)ServicoDe((Suprimento)i)] > 0 ? CargaPede : Math.Min(CargaEspera, c[i]);
            }
        }
    }
}
