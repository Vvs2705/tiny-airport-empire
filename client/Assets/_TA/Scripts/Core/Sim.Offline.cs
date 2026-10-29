using System;

namespace TA.Core
{
    public sealed partial class Sim
    {
        /// <summary>Renda online por s, media movel de Tuning.RendaTau (so a base: o 2x do anuncio nao infla o cofre).</summary>
        public double RateEma;
        /// <summary>Unix s: o maior instante ja visto (Save) e o id do ultimo resgate do cofre (o SavedAt do save que abriu).</summary>
        public long SavedAt, LastClaim;
        /// <summary>Cash do ultimo resgate (o rewarded "Cofre 2x" dobra este valor, 1x por LastClaim).</summary>
        public long LastClaimCash;
        /// <summary>s que o relogio do aparelho estava atras do SavedAt, so no 1o Save de cada recuo (senao 0): a View loga como sinal.</summary>
        public long ClockWentBack;
        bool _clockBehind;

        /// <summary>Dia local (virada as 04:00 no fuso gravado na 1a abertura, como o Metas.cs do Forge). So cresce: relogio para tras nao
        /// reabre o dia. 0 = nunca chamado (o bot e os testes jogam um dia so).</summary>
        public long Dia, Fuso;
        public bool FusoGravado;

        /// <summary>
        /// Cofre offline (GDD §6.3, ApplyOffline do Forge): 25% da renda online medida (RateEma) x o tempo fora, com teto de 2 h como trava do
        /// nucleo (Tuning.OfflineCap: o Tuning so encurta) e nunca mais que 2x o upgrade mais barato ainda a venda. dt &lt;= 0 paga 0; o claim
        /// e' idempotente por id (o SavedAt do save): id igual ou mais antigo nunca paga de novo. O relogio do quadro NAO anda: nenhum voo se
        /// perde com o jogo fechado.
        /// </summary>
        public long ApplyOffline(long elapsedSeconds, long claimId)
        {
            if (elapsedSeconds <= 0 || claimId <= LastClaim) return 0;
            LastClaim = claimId;
            long capped = Math.Min(elapsedSeconds, T.OfflineCap);
            long cash = (long)Math.Min(Math.Floor(RateEma * T.OfflineFator * capped), OfflineMaxCash());
            LastClaimCash = Math.Max(0, cash);
            if (cash <= 0) return 0;
            Cash += cash;
            Emit(Ev.Offline, (int)Math.Min(int.MaxValue, cash), (int)capped);
            return cash;
        }

        /// <summary>Teto relativo do cofre: 2x o proximo upgrade a venda.</summary>
        public long OfflineMaxCash() => (long)Math.Round(T.OfflineTetoUpgrades * MaisBaratoTravado());

        /// <summary>A View chama ao abrir e ao voltar da pausa: grava o fuso na 1a vez e anda o dia local (nunca para tras).</summary>
        public void Agora(long nowUnix, long fusoSeg)
        {
            if (!FusoGravado) { Fuso = fusoSeg; FusoGravado = true; }
            Dia = Math.Max(Dia, DiaLocal(nowUnix, Fuso));
        }

        public static long DiaLocal(long nowUnix, long fuso)
        {
            long a = nowUnix + fuso - 4 * 3600;
            return a >= 0 ? a / 86400 : -((-a + 86399) / 86400);
        }
    }
}
