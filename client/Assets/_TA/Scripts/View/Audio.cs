using System.Collections.Generic;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Audio do jogo (GDD §12). Clipes sintetizados por client/tools/audio_gen.py:
    /// Resources/Audio/sfx/&lt;nome&gt;_&lt;n&gt;.wav (variacoes do mesmo som) e Resources/Audio/musica/&lt;faixa&gt;.wav (lacos).
    /// Sons: toque, melhoria, embarque, decolagem_helice, decolagem_jato, recompensa, alerta, missao.
    /// Play: escolhe uma variacao (nunca a mesma 2x seguidas), varia o pitch +-4%, no maximo MaxPorSom vozes do mesmo nome e
    /// Vozes no total (rouba a mais antiga). Music: crossfade de Fade s entre 2 AudioSource; null ou "" esvazia.
    /// Volumes 0..1 separados no PlayerPrefs (ta_som, ta_musica). Clipe ausente ou que falhou: nao toca nada (o jogo nao quebra).
    /// Segundo plano: ao sair corta os efeitos e zera a musica; ao voltar a musica sobe do zero em Volta s. Sem alocacao por quadro.
    /// </summary>
    public static class Audio
    {
        const int Vozes = 12, MaxPorSom = 3;
        const float Fade = 1.2f, Volta = 2.5f, PitchVar = 0.04f;
        const string SomKey = "ta_som", MusicaKey = "ta_musica";

        sealed class Banco { public AudioClip[] Clips; public int Ultimo = -1; public float Quando = -99f; }

        static readonly Dictionary<string, Banco> Bancos = new Dictionary<string, Banco>();
        static AudioSource[] _vozes;
        static readonly string[] Dono = new string[Vozes];
        static readonly float[] Inicio = new float[Vozes];
        static AudioSource _mAtual, _mVelha;
        static Fader _fader;
        static string _faixa;
        static float _som = -1f, _musica = -1f;

        static bool Pronto => _vozes != null && _vozes[0] != null;   // ja iniciado (e o host ainda existe)

        public static float Som { get { if (_som < 0f) _som = PlayerPrefs.GetFloat(SomKey, 1f); return _som; } set => SetVolumes(value, Musica); }
        public static float Musica { get { if (_musica < 0f) _musica = PlayerPrefs.GetFloat(MusicaKey, 0.8f); return _musica; } set => SetVolumes(Som, value); }

        public static void Init(GameObject host)
        {
            if (Pronto) return;
            var grupos = new Dictionary<string, List<AudioClip>>();
            foreach (AudioClip c in Resources.LoadAll<AudioClip>("Audio/sfx"))
            {
                string n = Nome(c.name);
                if (!grupos.TryGetValue(n, out List<AudioClip> l)) grupos[n] = l = new List<AudioClip>();
                l.Add(c);
            }
            Bancos.Clear();
            foreach (var g in grupos) Bancos[g.Key] = new Banco { Clips = g.Value.ToArray() };

            _vozes = new AudioSource[Vozes];
            for (int i = 0; i < Vozes; i++) { _vozes[i] = Fonte(host, false); Dono[i] = null; }
            _mAtual = Fonte(host, true);
            _mVelha = Fonte(host, true);
            _fader = host.AddComponent<Fader>();
            _fader.enabled = false;
            host.AddComponent<Retorno>();
            if (Object.FindAnyObjectByType<AudioListener>() == null) host.AddComponent<AudioListener>();   // a cena nasce sem ouvinte: sem ele tudo sai mudo
            _faixa = null;
        }

        /// <summary>Toca o som `nome` (no maximo uma vez por `minGap` s: varios toques no mesmo quadro viram um).</summary>
        public static void Play(string nome, float pitch = 1f, float minGap = 0.05f)
        {
            if (!Pronto || Som <= 0f || nome == null || !Bancos.TryGetValue(nome, out Banco b)) return;
            float agora = Time.unscaledTime;
            if (agora - b.Quando < minGap) return;
            b.Quando = agora;

            int n = b.Clips.Length, k = 0;
            if (n > 1) { k = Random.Range(0, b.Ultimo < 0 ? n : n - 1); if (b.Ultimo >= 0 && k >= b.Ultimo) k++; }
            b.Ultimo = k;
            if (b.Clips[k] == null || b.Clips[k].loadState == AudioDataLoadState.Failed) return;

            int livre = -1, velha = -1, velhaMesmo = -1, mesmo = 0;
            for (int i = 0; i < Vozes; i++)
            {
                if (!_vozes[i].isPlaying) { if (livre < 0) livre = i; continue; }
                if (velha < 0 || Inicio[i] < Inicio[velha]) velha = i;
                if (Dono[i] != nome) continue;
                mesmo++;
                if (velhaMesmo < 0 || Inicio[i] < Inicio[velhaMesmo]) velhaMesmo = i;
            }
            int v = mesmo >= MaxPorSom ? velhaMesmo : livre >= 0 ? livre : velha;
            AudioSource s = _vozes[v];
            s.Stop();
            s.clip = b.Clips[k];
            s.pitch = pitch * (1f + Random.Range(-PitchVar, PitchVar));
            s.volume = Som;
            s.Play();
            Dono[v] = nome;
            Inicio[v] = agora;
        }

        /// <summary>Troca a musica com crossfade. A mesma faixa nao reinicia; null/"" faz fade para o silencio; faixa ausente fica como esta.</summary>
        public static void Music(string faixa)
        {
            if (!Pronto || faixa == _faixa || (string.IsNullOrEmpty(faixa) && string.IsNullOrEmpty(_faixa))) return;
            AudioClip c = string.IsNullOrEmpty(faixa) ? null : Resources.Load<AudioClip>("Audio/musica/" + faixa);
            if (!string.IsNullOrEmpty(faixa) && (c == null || c.loadState == AudioDataLoadState.Failed)) return;
            _faixa = faixa;
            AudioSource t = _mVelha; _mVelha = _mAtual; _mAtual = t;
            _mAtual.Stop();
            _mAtual.clip = c;
            _mAtual.volume = 0f;
            if (c != null) _mAtual.Play();
            _fader.T = 0f;
            _fader.Dur = Fade;
            _fader.enabled = true;
        }

        static readonly string[] Aeroportos = { "vale_verde", "costa_azul", "aurora_global" };

        /// <summary>Musica do aeroporto (1-3; fora disso, a 1): a mesma composicao com variacoes sutis (GDD §12).</summary>
        public static void MusicaAeroporto(int aeroporto) => Music(Aeroportos[aeroporto >= 1 && aeroporto <= Aeroportos.Length ? aeroporto - 1 : 0]);

        /// <summary>Volumes 0..1 (Configuracoes). Sem PlayerPrefs.Save aqui: o slider chama a cada quadro; o Game grava no Save/pausa/saida.</summary>
        public static void SetVolumes(float som, float musica)
        {
            _som = Mathf.Clamp01(som);
            _musica = Mathf.Clamp01(musica);
            PlayerPrefs.SetFloat(SomKey, _som);
            PlayerPrefs.SetFloat(MusicaKey, _musica);
            if (_mAtual != null && !_fader.enabled) _mAtual.volume = _musica;
        }

        /// <summary>Chamado pelo Retorno na pausa do app: nada de som alto na volta (GDD §12).</summary>
        public static void SegundoPlano(bool saiu)
        {
            if (!Pronto) return;
            if (saiu)
            {
                foreach (AudioSource v in _vozes) v.Stop();
                _mVelha.Stop();
                _mVelha.clip = null;
                _mAtual.volume = 0f;
                _fader.enabled = false;
                return;
            }
            _fader.T = 0f;
            _fader.Dur = Volta;
            _fader.enabled = true;
        }

        static AudioSource Fonte(GameObject host, bool musica)
        {
            AudioSource s = host.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.loop = musica;
            s.priority = musica ? 0 : 128;   // musica nunca e cortada pelo limite de vozes do Unity
            return s;
        }

        /// <summary>"recompensa_2" -> "recompensa"; "decolagem_jato_1" -> "decolagem_jato".</summary>
        static string Nome(string s)
        {
            int i = s.LastIndexOf('_');
            if (i <= 0 || i == s.Length - 1) return s;
            for (int k = i + 1; k < s.Length; k++) if (!char.IsDigit(s[k])) return s;
            return s.Substring(0, i);
        }

        /// <summary>Rampa da musica em tempo real (vale na pausa); desliga sozinho no fim (zero custo parado). O dt tem teto porque o
        /// 1o quadro depois do segundo plano traz todo o tempo fora: sem o teto a volta seria no volume cheio.</summary>
        sealed class Fader : MonoBehaviour
        {
            public float T, Dur = Fade;

            void Update()
            {
                T = Mathf.Min(1f, T + Mathf.Min(Time.unscaledDeltaTime, 0.1f) / Dur);
                float m = Musica;
                _mAtual.volume = T * m;
                _mVelha.volume = Mathf.Min(_mVelha.volume, (1f - T) * m);
                if (T < 1f) return;
                _mVelha.Stop();
                _mVelha.clip = null;
                enabled = false;
            }
        }

        /// <summary>Sempre ligado e sem Update: o Fader passa a maior parte do tempo desligado e nao serviria para ouvir a pausa.</summary>
        sealed class Retorno : MonoBehaviour
        {
            void OnApplicationPause(bool pausado) => SegundoPlano(pausado);
        }
    }
}
