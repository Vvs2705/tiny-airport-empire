using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TA.EditorTools
{
    /// <summary>
    /// Settings, cena e builds por script (copia do dc:Editor/Setup.cs, mesmo padrao do Forge Street). Idempotente. Em batch:
    ///   Unity -batchmode -quit -projectPath client -executeMethod TA.EditorTools.Setup.Apply
    ///   Unity -batchmode -quit -projectPath client -executeMethod TA.EditorTools.Setup.BuildWindows (ou BuildAndroidDev)
    /// A cena e' artefato gerado e vazia: o Game nasce sozinho (RuntimeInitializeOnLoadMethod).
    /// Fase A: so builds de desenvolvimento. Release, AAB e chave de upload esperam o nome e o appId (D-03, GDD §23.2).
    /// </summary>
    public static class Setup
    {
        // D-03 (2026-11-01): nome "Little Terminal", busca de marca limpa em docs/NOME.md. O appId nao muda depois de publicado;
        // trocar aqui (e em ProjectSettings.asset) so com decisao nova do Vinicius.
        public const string AppId = "br.com.vstack.littleterminal", ProductName = "Little Terminal";
        const string Scene = "Assets/_TA/Scenes/Main.unity";
        const string ApkDir = "Builds/android";
        /// <summary>Versao mostrada ao jogador (Application.version) e no save.</summary>
        public const string Version = "0.0.1-alpha";

        /// <summary>versionCode do Android, gerado da versao (nunca editado a mao, nunca reutilizado): (maior*10000 + menor*100 + patch)*10
        /// + estagio (alpha 1, beta 2, rc 5, final 9). 0.0.1-alpha = 11; 1.0.0 = 100009.</summary>
        public static int VersionCode(string v)
        {
            int dash = v.IndexOf('-');
            string[] n = (dash < 0 ? v : v.Substring(0, dash)).Split('.');
            string tag = dash < 0 ? "" : v.Substring(dash + 1);
            int stage = tag.StartsWith("alpha") ? 1 : tag.StartsWith("beta") ? 2 : tag.StartsWith("rc") ? 5 : 9;
            int Part(int i) => i < n.Length && int.TryParse(n[i], out int x) ? x : 0;
            return (Part(0) * 10000 + Part(1) * 100 + Part(2)) * 10 + stage;
        }

        const string Icone = "Assets/_TA/Art/Marca/icone_app.png";

        [MenuItem("TA/Aplicar settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "V-STACK";
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.Android.bundleVersionCode = VersionCode(Version);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AppId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = false;   // GDD §5.4: voo nenhum se perde com o jogo em segundo plano

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;      // a Play exige 64 bits
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // ponytail: hipotese ate fixar o aparelho minimo
            // 36 fixo: a Play exige 36 desde 31/08/2026 e o Unity 6.3 promete 35 e 36; o Auto segue o SDK instalado, que ja traz o android-37.0
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.predictiveBackSupport = true; // API 36
            SimbolosDaConta();
            ConfiguraFacebook();

            // Windows: janela retrato 540x960 (dev) para jogar e fotografar no PC.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true;
            // icone do app (Art/Marca, docs/FICHA_PLAY.md §3): o icone padrao vale para todas as plataformas; o Unity gera os tamanhos do Android
            // ponytail: icone legado; o adaptativo (camada de frente ate 0,64) entra se o launcher cortar o aviao no aparelho
            Texture2D icone = AssetDatabase.LoadAssetAtPath<Texture2D>(Icone);
            if (icone != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icone }, IconKind.Any);
            else Debug.LogWarning("TA Setup: falta " + Icone);

            if (!File.Exists(Scene))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Scene));
                EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single), Scene);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            IncluiShader("Legacy Shaders/Diffuse");   // as primitivas e as malhas do Mundo (Mundo.Material): sem isto o build sai magenta
            IncluiShader("TA/SpriteTinta");   // as folhas das pessoas (SpriteSheet.Material), com o uniforme tingido
            IncluiShader("TA/Sombra");   // a sombra plana dos avioes (Mundo.SombraPlana)
            AssetDatabase.SaveAssets();
            Debug.Log("TA Setup: ok (" + AppId + " " + Version + " code " + VersionCode(Version) + ", retrato, Android ARM64 IL2CPP API 26+)");
        }

        const string RspConta = "Assets/Vstack/Conta/Unity/csc.rsp";

        /// <summary>Logins da conta (docs/CONTAS.md §6): os SDKs ficam fora do git, so no PC do build de loja. O VS_GPGS e o VS_FACEBOOK vao num
        /// csc.rsp do assembly da conta (tambem fora do git), nunca nas Player Settings: o CI, sem os SDKs, compila sem eles.</summary>
        [InitializeOnLoadMethod]
        static void SimbolosDaConta()
        {
            string rsp = (Directory.Exists("Assets/GooglePlayGames") ? "-define:VS_GPGS " : "") + (Directory.Exists("Assets/FacebookSDK") ? "-define:VS_FACEBOOK " : "");
            string era = File.Exists(RspConta) ? File.ReadAllText(RspConta) : "";
            if (rsp == era) return;
            if (rsp == "") File.Delete(RspConta); else File.WriteAllText(RspConta, rsp);
            AssetDatabase.ImportAsset("Assets/Vstack/Conta/Unity/Vstack.Conta.Unity.asmdef", ImportAssetOptions.ForceUpdate);   // recompila o assembly
        }

        /// <summary>FacebookSettings do SDK (fora do git, dentro de Assets/FacebookSDK) a partir de %USERPROFILE%\.tiny-airport\facebook.txt
        /// (linha 1 o App ID, linha 2 o Client Token; quem cola o token e' o Vinicius). So login: eventos automaticos, metadados e id de
        /// publicidade desligados (GDD §9.3). Por reflexao, para compilar sem o SDK (CI). Depois regera o manifesto Android do SDK.</summary>
        [MenuItem("TA/Configurar Facebook")]
        public static void ConfiguraFacebook()
        {
            System.Type fs = System.Type.GetType("Facebook.Unity.Settings.FacebookSettings, Facebook.Unity.Settings");
            string txt = Path.Combine(System.Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".tiny-airport", "facebook.txt");
            if (fs == null || !File.Exists(txt)) return;
            string[] l = File.ReadAllLines(txt);
            if (l.Length < 2 || l[0].Trim() == "" || l[1].Trim() == "") { Debug.LogWarning("TA Setup: facebook.txt sem App ID e Client Token"); return; }
            void Poe(string nome, object v) => fs.GetProperty(nome)?.SetValue(null, v);
            Poe("AppIds", new System.Collections.Generic.List<string> { l[0].Trim() });
            Poe("ClientTokens", new System.Collections.Generic.List<string> { l[1].Trim() });
            Poe("AutoLogAppEventsEnabled", false);
            Poe("AdvertiserIDCollectionEnabled", false);
            Poe("AutoLogMetaDataEnabled", false);
            Poe("AutoLogProductImprovementEnabled", false);
            if (fs.GetProperty("Instance")?.GetValue(null) is Object inst) EditorUtility.SetDirty(inst);
            AssetDatabase.SaveAssets();
            System.Type.GetType("Facebook.Unity.Editor.ManifestMod, Facebook.Unity.Editor")?.GetMethod("GenerateManifest")?.Invoke(null, null);
            Debug.Log("TA Setup: Facebook configurado (so login, sem eventos automaticos nem id de publicidade)");
        }

        /// <summary>Poe o shader em "Always Included Shaders": o Mundo cria o material em tempo de execucao, sem asset que o referencie.</summary>
        static void IncluiShader(string nome)
        {
            Shader sh = Shader.Find(nome);
            if (sh == null) { Debug.LogError("TA Setup: shader " + nome + " nao achado"); return; }
            var gs = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(gs);
            SerializedProperty lista = so.FindProperty("m_AlwaysIncludedShaders");
            for (int i = 0; i < lista.arraySize; i++) if (lista.GetArrayElementAtIndex(i).objectReferenceValue == sh) return;
            lista.InsertArrayElementAtIndex(lista.arraySize);
            lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = sh;
            so.ApplyModifiedProperties();
        }

        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/win/TinyAirport.exe");
        public static void BuildAndroidDev() => Build(BuildTarget.Android, ApkDir + "/TinyAirport-dev.apk");

        /// <summary>AAB de loja, assinado com a chave de upload, so no PC do dono: a chave, a senha e o AAB ficam em %USERPROFILE%\.tiny-airport
        /// (fora do repositorio, que e' publico; docs/NOME.md e SEGURANCA_PRIVACIDADE.md §5). Nunca roda no CI.</summary>
        public static void BuildAndroidRelease()
        {
            string dir = Path.Combine(System.Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".tiny-airport");
            string jks = Path.Combine(dir, "littleterminal-upload.jks"), pass = Path.Combine(dir, "littleterminal-upload.pass");
            if (!File.Exists(jks) || !File.Exists(pass))
            {
                Debug.LogError("TA Setup: sem a chave de upload em " + dir + " (docs/NOME.md)");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            string senha = File.ReadAllText(pass).Trim();   // nunca vai para log
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = jks;
            PlayerSettings.Android.keystorePass = senha;
            PlayerSettings.Android.keyaliasName = "upload";
            PlayerSettings.Android.keyaliasPass = senha;
            bool ok = false;
            try { ok = Build(BuildTarget.Android, Path.Combine(dir, "release", "LittleTerminal-" + Version + ".aab"), true); }
            finally
            {
                // a senha e o caminho da chave nao ficam no ProjectSettings nem no proximo build de dev
                PlayerSettings.Android.keystorePass = PlayerSettings.Android.keyaliasPass = PlayerSettings.Android.keystoreName = "";
                PlayerSettings.Android.useCustomKeystore = false;
                EditorUserBuildSettings.buildAppBundle = false;
            }
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Durante o BuildAndroidRelease: o SemIdDePublicidade tira o android:debuggable que o manifesto do SDK do Facebook traz fixo.</summary>
        internal static bool EmRelease;

        static bool Build(BuildTarget target, string path, bool release = false)
        {
            EmRelease = release;
            Apply();
            BuildReport r = null;
            try
            {
                if (target == BuildTarget.Android)
                {
                    // regra do Vinicius (2026-10-09): fica so' o APK atual; os anteriores sao apagados antes de gerar o novo
                    if (Directory.Exists(ApkDir)) foreach (string apk in Directory.GetFiles(ApkDir, "*.apk")) File.Delete(apk);
                    // o empacotamento incremental do Gradle reaproveita o APK anterior e ja' deixou buracos/sem libunity no Forge Street
                    const string gradleOut = "Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build";
                    if (Directory.Exists(gradleOut)) Directory.Delete(gradleOut, true);
                    EditorUserBuildSettings.buildAppBundle = release;
                    if (release) Directory.CreateDirectory(Path.GetDirectoryName(path));
                    else PlayerSettings.Android.useCustomKeystore = false;   // APK de dev: chave de debug do Unity
                }
                r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { Scene },
                    locationPathName = path,
                    target = target,
                    options = release ? BuildOptions.None : BuildOptions.Development,
                });
                Debug.Log($"BuildSummary({target} {(release ? "release" : "dev")} {Version}): result={r.summary.result} errors={r.summary.totalErrors} size={r.summary.totalSize} path={path}");
                Relatorio(r, target);
            }
            catch (System.Exception e) { Debug.LogError("TA Setup: build abortado: " + e.Message); }
            bool ok = r != null && r.summary.result == BuildResult.Succeeded;
            if (Application.isBatchMode && !release) EditorApplication.Exit(ok ? 0 : 1);
            return ok;
        }

        /// <summary>Resumo do build em Builds/build_&lt;alvo&gt;.txt (erros e avisos de cada passo): o CI (unity.yml) sobe este arquivo como
        /// artefato, porque la o log do Editor vai para a saida do job.</summary>
        static void Relatorio(BuildReport r, BuildTarget target)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{target} dev {Version}: result={r.summary.result} errors={r.summary.totalErrors} warnings={r.summary.totalWarnings} " +
                          $"size={r.summary.totalSize} tempo={r.summary.totalTime}");
            foreach (BuildStep passo in r.steps)
                foreach (BuildStepMessage m in passo.messages)
                    if (m.type != LogType.Log) sb.AppendLine($"[{m.type}] {passo.name}: {m.content}");
            Directory.CreateDirectory("Builds");
            File.WriteAllText($"Builds/build_{target}.txt", sb.ToString());
        }
    }

    /// <summary>Anuncio nao personalizado para todos (guardrail do estudio; GDD §7.2): tira do APK o ID de publicidade e as APIs do Privacy
    /// Sandbox que um SDK de anuncio traga. Marcadores tools:node="remove" no manifesto do unityLibrary, que tem prioridade sobre as
    /// dependencias (AARs) na fusao do Gradle. Copia do dc:Editor/Setup.cs; vale desde ja para nenhum SDK futuro trazer o AD_ID calado.</summary>
    sealed class SemIdDePublicidade : UnityEditor.Android.IPostGenerateGradleAndroidProject
    {
        static readonly string[] Tirar =
        {
            "com.google.android.gms.permission.AD_ID", "android.permission.ACCESS_ADSERVICES_AD_ID",
            "android.permission.ACCESS_ADSERVICES_ATTRIBUTION", "android.permission.ACCESS_ADSERVICES_TOPICS",
            // o SDK do Facebook (so login, docs/CONTAS.md) traz a audiencia do Privacy Sandbox e o referenciador de instalacao, que sao de anuncio
            "android.permission.ACCESS_ADSERVICES_CUSTOM_AUDIENCE", "com.google.android.finsky.permission.BIND_GET_INSTALL_REFERRER_SERVICE",
        };
        public int callbackOrder => 999;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string f = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(f)) { Debug.LogWarning("TA Setup: manifesto do unityLibrary nao achado: " + f); return; }
            const string tools = "http://schemas.android.com/tools", android = "http://schemas.android.com/apk/res/android";
            var doc = new System.Xml.XmlDocument();
            doc.Load(f);
            System.Xml.XmlElement root = doc.DocumentElement;
            if (!root.HasAttribute("xmlns:tools")) root.SetAttribute("xmlns:tools", tools);
            foreach (string p in Tirar)
            {
                System.Xml.XmlElement e = doc.CreateElement("uses-permission");
                e.SetAttribute("name", android, p);
                e.SetAttribute("node", tools, "remove");
                root.AppendChild(e);
            }
            if (Setup.EmRelease)   // a Play recusa app depuravel; o checa_apk --release confere
                foreach (System.Xml.XmlElement app in doc.GetElementsByTagName("application")) app.RemoveAttribute("debuggable", android);
            doc.Save(f);
            Debug.Log("TA Setup: AD_ID e Privacy Sandbox removidos do manifesto (" + Tirar.Length + ")");
        }
    }
}
