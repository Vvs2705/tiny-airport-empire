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
    /// Fase A: so builds de desenvolvimento. Release, AAB, chave de upload e icones esperam o nome e o appId (D-03, GDD §23.2).
    /// </summary>
    public static class Setup
    {
        // ponytail: appId e nome PROVISORIOS, so para build de desenvolvimento. D-03 (nome comercial e appId br.com.vstack.<nome>) esta
        // pendente; nunca publicar com estes: o appId nao muda depois de publicado. Trocar aqui (e so aqui) quando o Vinicius decidir.
        public const string AppId = "br.com.vstack.tinyairport.dev", ProductName = "Tiny Airport (dev)";
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

            // Windows: janela retrato 540x960 (dev) para jogar e fotografar no PC.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true;

            if (!File.Exists(Scene))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Scene));
                EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single), Scene);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            IncluiShader("Legacy Shaders/Diffuse");   // as primitivas do Mundo (Mundo.Material): sem isto o build sai magenta
            AssetDatabase.SaveAssets();
            Debug.Log("TA Setup: ok (" + AppId + " " + Version + " code " + VersionCode(Version) + ", retrato, Android ARM64 IL2CPP API 26+)");
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

        static void Build(BuildTarget target, string path)
        {
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
                    EditorUserBuildSettings.buildAppBundle = false;
                    PlayerSettings.Android.useCustomKeystore = false;   // APK de dev: chave de debug do Unity
                }
                r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { Scene },
                    locationPathName = path,
                    target = target,
                    options = BuildOptions.Development,
                });
                Debug.Log($"BuildSummary({target} dev {Version}): result={r.summary.result} errors={r.summary.totalErrors} size={r.summary.totalSize} path={path}");
            }
            catch (System.Exception e) { Debug.LogError("TA Setup: build abortado: " + e.Message); }
            if (Application.isBatchMode) EditorApplication.Exit(r != null && r.summary.result == BuildResult.Succeeded ? 0 : 1);
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
            doc.Save(f);
            Debug.Log("TA Setup: AD_ID e Privacy Sandbox removidos do manifesto (" + Tirar.Length + ")");
        }
    }
}
