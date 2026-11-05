#if TA_LEVELPLAY
// Compila so com o LevelPlay no projeto: o TA.asmdef liga TA_LEVELPLAY por versionDefines quando o pacote com.unity.services.levelplay
// entra (docs/LOJA_SERVIDOR.md §5, rodada do PC). Sem o pacote, o anuncio continua simulado (Hud.Telas).
using TA.Core;
using Unity.Services.LevelPlay;

namespace TA
{
    /// <summary>
    /// O rewarded real (GDD §7.2). Privacidade ANTES do Init: nao personalizado para todos (sem consentimento de personalizacao, "do not sell"
    /// e nao dirigido a crianca); o AD_ID sai do manifesto no Setup.SemIdDePublicidade. O checa_repo derruba este arquivo se o Init vier antes
    /// do SetConsent(false) e do do_not_sell. Teste (build de desenvolvimento ou config "teste"): metadado de test suite e o atalho para abri-la.
    /// Regras (limite diario, 1x por id, o que paga) continuam no nucleo (Anuncios.cs): aqui so se mostra e se avisa que acabou.
    /// </summary>
    public sealed class AnunciosLevelPlay
    {
        readonly LojaConfig _cfg;
        LevelPlayRewardedAd _ad;
        string _tx;
        System.Action<string> _pago;

        public bool Pronto => _ad != null && _ad.IsAdReady();

        public AnunciosLevelPlay(LojaConfig cfg) { _cfg = cfg; }

        public void Inicia()
        {
            if (!_cfg.TemAnuncio) return;
            LevelPlay.SetConsent(false);
            LevelPlay.SetMetaData("do_not_sell", "true");
            LevelPlay.SetMetaData("is_child_directed", "false");
            if (_cfg.Teste) LevelPlay.SetMetaData("is_test_suite", "enable");
            LevelPlay.OnInitSuccess += _ => Cria();
            LevelPlay.Init(_cfg.levelplayApp);
        }

        void Cria()
        {
            _ad = new LevelPlayRewardedAd(_cfg.levelplayRecompensado);
            _ad.OnAdRewarded += (info, recompensa) => { string tx = _tx; _tx = null; if (tx != null) _pago?.Invoke(tx); };
            _ad.OnAdClosed += _ => { _tx = null; _ad.LoadAd(); };
            _ad.OnAdDisplayFailed += _ => { _tx = null; _ad.LoadAd(); };
            _ad.LoadAd();
        }

        /// <summary>Mostra o anuncio da oferta `tx` (Anuncios.Oferta). pago(tx) so no fim com recompensa; fechar antes nao paga (GDD §7.2).</summary>
        public bool Mostra(string tx, System.Action<string> pago)
        {
            if (!Pronto || tx == null) return false;
            _tx = tx;
            _pago = pago;
            _ad.ShowAd();
            return true;
        }

        /// <summary>So em teste: a test suite do LevelPlay (flag -adtestsuite, GDD §7.2).</summary>
        public void AbreTestSuite()
        {
            if (_cfg.Teste) LevelPlay.LaunchTestSuite();
        }
    }
}
#endif
