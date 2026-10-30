using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TA.Tests
{
    /// <summary>Audio do GDD §12 dentro do Unity (fora do dotnet test: precisa do import dos clipes e da View): banco importado,
    /// fallback silencioso, volumes separados e a volta do segundo plano sem pico.</summary>
    public class AudioTests
    {
        GameObject _host;
        float _som, _musica;

        [SetUp]
        public void Abre()
        {
            _som = Audio.Som;
            _musica = Audio.Musica;
            _host = new GameObject("audio");
            Audio.Init(_host);
        }

        [TearDown]
        public void Fecha()
        {
            Audio.SetVolumes(_som, _musica);
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void Banco_ImportadoComOTipoDeCarga()
        {
            AudioClip[] sfx = Resources.LoadAll<AudioClip>("Audio/sfx");
            foreach (string som in new[] { "toque", "melhoria", "embarque", "decolagem_helice", "decolagem_jato", "recompensa", "alerta", "missao" })
                Assert.IsTrue(sfx.Any(c => c.name.StartsWith(som + "_")), som);
            Assert.IsTrue(sfx.All(c => c.loadType == AudioClipLoadType.DecompressOnLoad));
            foreach (string faixa in new[] { "vale_verde", "costa_azul", "aurora_global" })
                Assert.AreEqual(AudioClipLoadType.Streaming, Resources.Load<AudioClip>("Audio/musica/" + faixa).loadType, faixa);
        }

        [Test]
        public void ClipeAusente_FicaEmSilencio()
        {
            Audio.Play("nao_existe");
            Audio.Play(null);
            Audio.Music("nao_existe");
            Assert.IsFalse(_host.GetComponents<AudioSource>().Any(s => s.isPlaying));
        }

        [Test]
        public void Volumes_SeparadosELimitados()
        {
            Audio.SetVolumes(0.25f, 0.75f);
            Assert.AreEqual(0.25f, Audio.Som);
            Assert.AreEqual(0.75f, Audio.Musica);
            Audio.Musica = 2f;
            Assert.AreEqual(1f, Audio.Musica);
            Assert.AreEqual(0.25f, Audio.Som);
        }

        [Test]
        public void VoltarDoSegundoPlano_NaoVoltaAlto()
        {
            Audio.SetVolumes(1f, 1f);
            Audio.MusicaAeroporto(1);
            AudioSource[] fontes = _host.GetComponents<AudioSource>();
            foreach (AudioSource s in fontes) s.volume = 1f;
            Audio.SegundoPlano(true);
            Audio.SegundoPlano(false);
            Assert.IsTrue(fontes.All(s => s.volume == 0f || !s.isPlaying));
            Assert.IsTrue(fontes.Any(s => s.clip != null && s.loop && s.volume == 0f), "a musica segue parada no zero ate a rampa");
        }
    }
}
