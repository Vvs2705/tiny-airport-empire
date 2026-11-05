using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Telemetria sem dado pessoal: catalogo de eventos e o p10 do FPS do performance_sample (Telemetria.cs).</summary>
    public class TelemetriaTests
    {
        static bool Anda(Desempenho d, float dt, int n, out AmostraFps ultima)
        {
            bool saiu = false;
            ultima = default;
            for (int i = 0; i < n; i++) if (d.Quadro(dt, out AmostraFps a)) { saiu = true; ultima = a; }
            return saiu;
        }

        [Test]
        public void Catalogo_SoNomesFixosEmSnakeCase()
        {
            foreach (string e in Telemetria.Eventos) Assert.That(Regex.IsMatch(e, "^[a-z][a-z0-9_]*$"), e);
            Assert.That(Telemetria.Valido("performance_sample"));
            Assert.That(Telemetria.Valido("device_class"));
            Assert.That(Telemetria.Valido("Performance_Sample"), Is.False);
            Assert.That(Telemetria.Valido("joao@email.com"), Is.False);
            Assert.That(Telemetria.Valido(""), Is.False);
            Assert.That(Telemetria.Valido(null), Is.False);
        }

        [Test]
        public void DeviceClass_GrossoComTeto()
        {
            Assert.That(Telemetria.MemoriaGb(2900), Is.EqualTo(3));
            Assert.That(Telemetria.MemoriaGb(3800), Is.EqualTo(4));
            Assert.That(Telemetria.MemoriaGb(65536), Is.EqualTo(16));
            Assert.That(Telemetria.MemoriaGb(-1), Is.EqualTo(0));
            Assert.That(Telemetria.Nucleos(64), Is.EqualTo(16));
        }

        [Test]
        public void Calcula_P10EMedia()
        {
            // 90 quadros de 1/60 e 10 de 1/20: p10 = o quadro no indice 90 depois de ordenar = 1/20 -> 20 FPS
            var dts = new List<float>();
            for (int i = 0; i < 90; i++) dts.Add(1f / 60f);
            for (int i = 0; i < 10; i++) dts.Add(1f / 20f);
            AmostraFps a = Desempenho.Calcula(dts);
            Assert.That(a.P10x10, Is.EqualTo(200));
            Assert.That(a.Media, Is.EqualTo(100f / (90f / 60f + 10f / 20f)).Within(0.05f));
            Assert.That(a.Quadros, Is.EqualTo(100));
            Assert.That(Desempenho.Calcula(new List<float>()).Quadros, Is.EqualTo(0));
        }

        [Test]
        public void Janela_SaiDepoisDoAquecimentoEDaJanela()
        {
            var d = new Desempenho();
            const float dt = 1f / 60f;
            int aquece = (int)System.Math.Ceiling(Desempenho.Aquecimento / dt), janela = (int)System.Math.Ceiling(Desempenho.Janela / dt);
            Assert.That(Anda(d, dt, aquece + janela - 5, out _), Is.False, "antes de fechar a janela");
            Assert.That(Anda(d, dt, 10, out AmostraFps a), Is.True);
            Assert.That(a.P10x10, Is.EqualTo(600));
            Assert.That(a.Mediax10, Is.EqualTo(600));
            // a janela seguinte nao repete o aquecimento
            Assert.That(Anda(d, dt, janela + 1, out _), Is.True);
        }

        [Test]
        public void Buraco_RecomecaSemQuadroLento()
        {
            var d = new Desempenho();
            const float dt = 1f / 30f;
            Anda(d, dt, 1000, out _);
            Assert.That(d.Quadro(30f, out _), Is.False, "30 s em segundo plano");
            int aquece = (int)System.Math.Ceiling(Desempenho.Aquecimento / dt), janela = (int)System.Math.Ceiling(Desempenho.Janela / dt);
            Assert.That(Anda(d, dt, aquece + janela - 3, out _), Is.False, "o buraco zerou a janela e voltou ao aquecimento");
            Assert.That(Anda(d, dt, 6, out AmostraFps a), Is.True);
            Assert.That(a.P10x10, Is.EqualTo(300), "o buraco nao entrou como quadro lento");
        }

        [Test]
        public void QuadroPodre_Ignorado()
        {
            var d = new Desempenho();
            Assert.That(d.Quadro(float.NaN, out _), Is.False);
            Assert.That(d.Quadro(float.PositiveInfinity, out _), Is.False);
            Assert.That(d.Quadro(0f, out _), Is.False);
            Assert.That(d.Quadro(-1f, out _), Is.False);
        }
    }
}
