using TA.Core;

namespace TA
{
    /// <summary>
    /// Qual som toca para cada evento do nucleo (GDD §12). Funcao pura (testavel): o Game chama Audio.Play com o resultado.
    /// Nada de queda, incendio ou morte: o alerta e' um aviso curto e suave. Evento sem som devolve null.
    /// Decolagem: helice no 1o aeroporto (Vale Verde), jato nos outros.
    /// </summary>
    public static class AudioEventos
    {
        public static string Som(Ev tipo, int aeroporto)
        {
            switch (tipo)
            {
                case Ev.Toque: return "toque";
                case Ev.Melhorou:
                case Ev.PatioMelhorou:
                case Ev.SuprimentoMelhorou: return "melhoria";
                case Ev.EmbarqueAberto: return "embarque";
                case Ev.Decolou: return aeroporto <= 0 ? "decolagem_helice" : "decolagem_jato";
                case Ev.Recompensa:
                case Ev.Bonus:
                case Ev.PasseNivel: return "recompensa";
                case Ev.VooPerdido:
                case Ev.SemTripulacao:
                case Ev.ManutencaoPendente: return "alerta";
                case Ev.MissaoConcluida:
                case Ev.ObjetivoConcluido:
                case Ev.ConquistaFeita: return "missao";
                default: return null;
            }
        }
    }
}
