#if VS_UGS
// Cola com a Unity Authentication e o Cloud Save (UGS). So existe com os pacotes instalados: o Vstack.Conta.Unity.asmdef liga VS_UGS
// por versionDefines quando com.unity.services.authentication esta no manifest. Google Play Games entra com VS_GPGS (pacote
// com.google.play.games) e Facebook com VS_FACEBOOK (simbolo nas Player Settings, depois de importar o SDK). Sem eles, o provedor nao e'
// oferecido e o botao nao aparece. Compila na rodada do PC (README.md desta pasta, passo 3).
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Networking;
#if VS_GPGS
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
#if VS_FACEBOOK
using Facebook.Unity;
#endif

namespace Vstack.Conta.Unity
{
    /// <summary>IServicoConta e INuvem sobre a UGS. Toda excecao vira Resultado (o modulo nunca ve excecao do SDK).</summary>
    public sealed class ContaUgs : IServicoConta, INuvem
    {
        /// <summary>A chave do save no Cloud Save (uma so, o texto inteiro).</summary>
        public const string Chave = "save";
        const string GooglePlayGamesId = "google-play-games", FacebookId = "facebook.com", AppleId = "apple.com";

        readonly string _ambiente;
        bool _iniciado;
        readonly List<Provedor> _ligados = new List<Provedor>();

        /// <summary>ambiente: o da UGS ("production" no build da loja, "development" no resto).</summary>
        public ContaUgs(string ambiente) { _ambiente = ambiente; }

        static IAuthenticationService A => AuthenticationService.Instance;

        public bool Entrou => _iniciado && A.IsSignedIn;
        public string Jogador => Entrou ? A.PlayerId : null;
        public string Token => Entrou ? A.AccessToken : null;
        public IReadOnlyCollection<Provedor> Ligados => _ligados;

        public bool Oferece(Provedor p)
        {
#if VS_GPGS && UNITY_ANDROID
            if (p == Provedor.GooglePlayGames) return true;
#endif
#if VS_FACEBOOK
            if (p == Provedor.Facebook) return true;
#endif
            return false;
        }

        public async Task<Resultado> Entrar()
        {
            try
            {
                if (!_iniciado)
                {
                    if (UnityServices.State != ServicesInitializationState.Initialized)
                        await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName(_ambiente));
                    _iniciado = true;
                }
                if (!A.IsSignedIn) await A.SignInAnonymouslyAsync();   // com sessao guardada, volta a mesma conta (ligada ou nao)
                await LeLigados();
                return Resultado.Ok;
            }
            catch (Exception e) { return Erro(e); }
        }

        public async Task<Resultado> Ligar(Provedor p)
        {
            try
            {
                string credencial = await Credencial(p);
                if (credencial == null) return Resultado.Cancelado;
                switch (p)
                {
                    case Provedor.GooglePlayGames: await A.LinkWithGooglePlayGamesAsync(credencial); break;
                    case Provedor.Facebook: await A.LinkWithFacebookAsync(credencial); break;
                    default: return Resultado.Falhou;
                }
                await LeLigados();
                return Resultado.Ok;
            }
            catch (Exception e) { return Erro(e); }
        }

        public async Task<Resultado> TrocarPara(Provedor p)
        {
            try
            {
                string credencial = await Credencial(p);
                if (credencial == null) return Resultado.Cancelado;
                A.SignOut(true);   // esquece a sessao anonima deste aparelho (o progresso dela ja foi avisado)
                switch (p)
                {
                    case Provedor.GooglePlayGames: await A.SignInWithGooglePlayGamesAsync(credencial); break;
                    case Provedor.Facebook: await A.SignInWithFacebookAsync(credencial); break;
                    default: return Resultado.Falhou;
                }
                await LeLigados();
                return Resultado.Ok;
            }
            catch (Exception e)
            {
                Resultado r = Erro(e);
                if (!A.IsSignedIn) await Entrar();   // nunca fica sem conta: volta para uma anonima
                return r;
            }
        }

        public async Task<Resultado> Desligar(Provedor p)
        {
            try
            {
                switch (p)
                {
                    case Provedor.GooglePlayGames: await A.UnlinkGooglePlayGamesAsync(); break;
                    case Provedor.Facebook: await A.UnlinkFacebookAsync(); break;
                    default: return Resultado.Falhou;
                }
                await LeLigados();
                return Resultado.Ok;
            }
            catch (Exception e) { return Erro(e); }
        }

        public async Task<Resultado> Excluir()
        {
            try
            {
                await A.DeleteAccountAsync();
                A.SignOut(true);
                _ligados.Clear();
#if VS_FACEBOOK
                if (FB.IsLoggedIn) FB.LogOut();
#endif
                return Resultado.Ok;
            }
            catch (Exception e) { return Erro(e); }
        }

        public async Task<Baixado> Baixar()
        {
            try
            {
                var d = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { Chave });
                string texto = d.TryGetValue(Chave, out var item) ? item.Value.GetAs<string>() : null;
                return new Baixado { Resultado = Resultado.Ok, Texto = texto };
            }
            catch (Exception e) { return new Baixado { Resultado = Erro(e) }; }
        }

        public async Task<Resultado> Subir(string texto)
        {
            try
            {
                await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { Chave, texto } });
                return Resultado.Ok;
            }
            catch (Exception e) { return Erro(e); }
        }

        public async Task<Resultado> Apagar()
        {
            try
            {
                await CloudSaveService.Instance.Data.Player.DeleteAsync(Chave);
                return Resultado.Ok;
            }
            catch (CloudSaveException e) when (e.Reason == CloudSaveExceptionReason.NotFound) { return Resultado.Ok; }
            catch (Exception e) { return Erro(e); }
        }

        /// <summary>
        /// DELETE {servidor}/v1/conta com o token: o servidor do jogo apaga o historico de compras desta conta (servidor/src/conta.js).
        /// Para Contas.Excluir. servidor vazio = o jogo nao tem servidor: nada a apagar.
        /// </summary>
        public static async Task<Resultado> ApagaNoServidor(string servidor, string token)
        {
            if (string.IsNullOrEmpty(servidor)) return Resultado.Ok;
            using (var w = UnityWebRequest.Delete(servidor.TrimEnd('/') + "/v1/conta"))
            {
                w.SetRequestHeader("Authorization", "Bearer " + token);
                w.downloadHandler = new DownloadHandlerBuffer();
                w.timeout = 15;
                var op = w.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                if (w.result == UnityWebRequest.Result.ConnectionError) return Resultado.SemRede;
                return w.responseCode == 200 || w.responseCode == 404 ? Resultado.Ok : Resultado.Falhou;
            }
        }

        async Task LeLigados()
        {
            _ligados.Clear();
            PlayerInfo info = await A.GetPlayerInfoAsync();
            foreach (Identity i in info.Identities ?? new List<Identity>())
            {
                if (i.TypeId == GooglePlayGamesId) _ligados.Add(Provedor.GooglePlayGames);
                else if (i.TypeId == FacebookId) _ligados.Add(Provedor.Facebook);
                else if (i.TypeId == AppleId) _ligados.Add(Provedor.Apple);
            }
        }

        static Resultado Erro(Exception e)
        {
            if (e is AuthenticationException a)
            {
                if (a.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked) return Resultado.JaLigadaAOutra;
                if (a.ErrorCode == AuthenticationErrorCodes.AccountLinkLimitExceeded) return Resultado.JaLigadaAOutra;
            }
            if (e is RequestFailedException r && r.ErrorCode == CommonErrorCodes.TransportError) return Resultado.SemRede;
            Debug.LogWarning("conta: " + e.GetType().Name);   // so o tipo: a mensagem do SDK pode trazer id
            return Resultado.Falhou;
        }

        /// <summary>A credencial do provedor (codigo do servidor do Play Games, token do Facebook). null = o jogador cancelou.</summary>
        static Task<string> Credencial(Provedor p)
        {
            var t = new TaskCompletionSource<string>();
#if VS_GPGS && UNITY_ANDROID
            if (p == Provedor.GooglePlayGames)
            {
                PlayGamesPlatform.Activate();
                PlayGamesPlatform.Instance.ManuallyAuthenticate(status =>
                {
                    if (status != SignInStatus.Success) { t.TrySetResult(null); return; }
                    PlayGamesPlatform.Instance.RequestServerSideAccess(true, code => t.TrySetResult(string.IsNullOrEmpty(code) ? null : code));
                });
                return t.Task;
            }
#endif
#if VS_FACEBOOK
            if (p == Provedor.Facebook)
            {
                IniciaFacebook(() =>
                    // so o perfil publico: o jogo nao pede e-mail nem amigos
                    FB.LogInWithReadPermissions(new List<string> { "public_profile" }, r =>
                        t.TrySetResult(r == null || r.Cancelled || !string.IsNullOrEmpty(r.Error) ? null : AccessToken.CurrentAccessToken?.TokenString)));
                return t.Task;
            }
#endif
            t.TrySetResult(null);
            return t.Task;
        }

#if VS_FACEBOOK
        /// <summary>O SDK do Facebook so serve para o login: sem eventos automaticos e sem o id de publicidade (Data safety, GDD §9.3).</summary>
        static void IniciaFacebook(Action pronto)
        {
            void Desliga()
            {
                FB.Mobile.SetAutoLogAppEventsEnabled(false);
                FB.Mobile.SetAdvertiserIDCollectionEnabled(false);
                pronto();
            }
            if (FB.IsInitialized) Desliga(); else FB.Init(Desliga);
        }
#endif
    }
}
#endif
