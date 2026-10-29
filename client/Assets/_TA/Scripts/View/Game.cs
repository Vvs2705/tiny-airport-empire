using System;
using System.Collections;
using System.Globalization;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Fluxo do jogo, o unico MonoBehaviour (nasce sozinho na cena vazia por RuntimeInitializeOnLoadMethod), copiado da casca do
    /// Dungeon Courier (dc:Scripts/View/Game.cs). Fase A: chama sim.Tick com o dt do quadro e mostra o relogio; save em disco, HUD,
    /// toque e diario CSV chegam na Fase B (docs/CONTRATO_V01.md).
    /// Flags (leitura de argumentos do DC; as outras do CONTRATO_V01 §7 entram com o sistema que usam):
    ///   -autoplay N   sem render: simula N ondas de 180 s de jogo, confere o save em ida e volta, loga AUTOPLAY OK|FAIL e sai 0/1.
    ///                 Usar com -batchmode -nographics (client/tools/verify.sh full).
    ///   -speed N      multiplica o dt (0,1..20).
    ///   -shot f.png [-shotdelay s]   foto da tela depois de s segundos reais (padrao 1) e sai 0.
    /// </summary>
    public sealed class Game : MonoBehaviour
    {
        const float Onda = 180f;   // s de jogo por onda (CONTRATO_V01 §4); o autoplay da Fase A so mede o relogio
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public Sim Sim { get; private set; }
        float _speed = 1f;
        bool _headless;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        }

        void Awake()
        {
            // vSyncCount 0: senao a qualidade Medium (vSyncCount 1) prende o quadro ao vsync de 120 Hz (POCO F4) e o targetFrameRate nao vale
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            _headless = Arg("-autoplay") != null;
            if (_headless) return;   // o Start roda o autoplay sem montar camera

            _speed = Mathf.Clamp(ArgF("-speed", 1f), 0.1f, 20f);
            Sim = new Sim();
            Camera cam = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.12f, 0.2f);   // azul-marinho da interface (GDD_ORIGEM, "Interface e marca")
        }

        void Start()
        {
            if (_headless) { Autoplay(); return; }
            string shot = Arg("-shot");
            if (!string.IsNullOrEmpty(shot)) StartCoroutine(Shot(shot));
        }

        void Update()
        {
            if (!_headless) Sim.Tick(Time.deltaTime * _speed);
        }

        // ponytail: HUD de texto do IMGUI so para o esqueleto mostrar que esta vivo; a HUD por codigo (Art.cs do DC) chega na Fase B
        void OnGUI()
        {
            if (_headless) return;
            GUI.Label(new Rect(16, 16, Screen.width - 32, 60),
                string.Format(Inv, "Tiny Airport (dev) - esqueleto da Fase A\ntempo de jogo {0:0.0} s   Cash {1}", Sim.Time, Sim.Cash));
        }

        // ------------------------------------------------------------------ dev: autoplay, foto

        /// <summary>-autoplay N (smoke do build, sem render): N ondas de relogio em quadros de 1/60 s. OK = o relogio andou N x 180 s e o save
        /// faz ida e volta pelo envelope sem perder nada.</summary>
        void Autoplay()
        {
            int n = int.TryParse(Arg("-autoplay"), NumberStyles.Integer, Inv, out int v) && v > 0 ? v : 8;
            var sim = new Sim();
            bool ok = true;
            try
            {
                for (int i = 0; i < n; i++)
                {
                    for (int q = 0; q < Onda * 60; q++) sim.Tick(1f / 60f);
                    Debug.Log(string.Format(Inv, "AUTOPLAY #{0} tempo_jogo={1:0.0}s passos={2}", i + 1, sim.Time, sim.Steps));
                }
                Sim back = Sim.Load(sim.Save(Application.version, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
                bool round = !back.SaveRefused && back.Steps == sim.Steps && back.Cash == sim.Cash;
                if (!round) Debug.Log("AUTOPLAY save ida-e-volta diferente");
                ok = round && Mathf.Abs(sim.Time - n * Onda) < 1f;
            }
            catch (Exception ex) { ok = false; Debug.LogError("AUTOPLAY excecao: " + ex); }
            Debug.Log(string.Format(Inv, "AUTOPLAY {0} ondas={1} tempo_jogo={2:0}s passos={3} save=ida-e-volta", ok ? "OK" : "FAIL", n, sim.Time, sim.Steps));
            Application.Quit(ok ? 0 : 1);
        }

        IEnumerator Shot(string path)
        {
            yield return new WaitForSecondsRealtime(ArgF("-shotdelay", 1f));
            Debug.Log(string.Format(Inv, "SHOT {0} t={1:0.0} {2}x{3}", path, Sim.Time, Screen.width, Screen.height));
            ScreenCapture.CaptureScreenshot(path);
            yield return null;
            yield return null;
            Application.Quit(0);
        }

        static float ArgF(string name, float def) => float.TryParse(Arg(name), NumberStyles.Float, Inv, out float v) ? v : def;

        /// <summary>Valor da flag (Environment.GetCommandLineArgs): null = ausente; "" = presente sem valor.</summary>
        static string Arg(string name)
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, name);
            if (i < 0) return null;
            return i + 1 < a.Length && !a[i + 1].StartsWith("-", StringComparison.Ordinal) ? a[i + 1] : "";
        }
    }
}
