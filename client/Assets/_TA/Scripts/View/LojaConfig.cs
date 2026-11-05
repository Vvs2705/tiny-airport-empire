#if TA_IAP || TA_LEVELPLAY
// So existe com a loja ou o anuncio real ligados (versionDefines do TA.asmdef, docs/LOJA_SERVIDOR.md §5).
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Enderecos e ids da loja e do anuncio (nenhum e' segredo: todos vao dentro do APK), lidos de Resources/loja_config.json, que fica fora do
    /// git (.gitignore). Sem o arquivo, ou num build sem os pacotes, tudo continua simulado como na v0.1. Teste = build de desenvolvimento ou
    /// "teste": true no arquivo: servidor de teste, anuncio de teste e test suite do LevelPlay; a configuracao de producao so vale num build
    /// de release com "teste": false (GDD §7.2, §8.3, §8.7). Modelo do arquivo: docs/LOJA_SERVIDOR.md §4.
    /// </summary>
    [System.Serializable]
    public sealed class LojaConfig
    {
        public string servidor = "";            // https://...run.app (o do ambiente: falso, teste ou producao)
        public string levelplayApp = "";        // app key do LevelPlay (conta da V-STACK)
        public string levelplayRecompensado = ""; // ad unit id do rewarded
        public bool teste = true;

        public bool Teste => teste || Debug.isDebugBuild;
        public bool TemServidor => servidor.StartsWith("https://");
        public bool TemAnuncio => levelplayApp.Length > 0 && levelplayRecompensado.Length > 0;

        public static LojaConfig Carrega()
        {
            var a = Resources.Load<TextAsset>("loja_config");
            if (a == null) return new LojaConfig();
            try { return JsonUtility.FromJson<LojaConfig>(a.text) ?? new LojaConfig(); }
            catch (System.ArgumentException) { return new LojaConfig(); }
        }
    }
}
#endif
