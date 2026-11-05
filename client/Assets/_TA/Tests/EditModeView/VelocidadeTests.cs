using System.Collections.Generic;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>A velocidade do relogio (Game.Anda): o 3x roda mais passos fixos por quadro, sem mudar o passo, e da o mesmo jogo do 1x.</summary>
    public class VelocidadeTests
    {
        const float Quadro = 1f / 60f;

        /// <summary>O bot Bom joga 10 min de jogo na velocidade v, a 60 qps, pelo Game.Anda. Ele decide depois dos Ticks do quadro, com o que a tela
        /// mostra: a cada 1 s de jogo, o mesmo instante nas duas velocidades (antes dos Ticks, a 3x ele veria o jogo 3 Ticks atrasado).</summary>
        static Sim Joga(int v, List<string> eventos)
        {
            var s = new Sim(Tuning.Padrao, 7);
            var bot = new Bot(BotProfile.Bom);
            for (int q = 0; q < 36000 / v; q++)
            {
                Game.Anda(s, Quadro, v, e => eventos.Add(e.Tipo + " " + e.A + " " + e.B + " " + e.C));
                bot.Pensa(s, Quadro * v);
            }
            return s;
        }

        [Test]
        public void TresX_MesmoJogoQueUmX_MesmaSemente()
        {
            Assert.Fail("quebrado de proposito: prova que o CI do Unity derruba o check (branch descartavel, nao fazer merge)");
            var e1 = new List<string>();
            var e3 = new List<string>();
            Sim a = Joga(1, e1), b = Joga(3, e3);
            Assert.Greater(a.TotalEmbarcados, 0, "o bot jogou");
            Assert.AreEqual(a.Steps, b.Steps, "o mesmo numero de passos fixos");
            Assert.AreEqual(e1.Count, e3.Count, "nenhum evento perdido entre os Ticks do quadro");
            CollectionAssert.AreEqual(e1, e3);
            Assert.AreEqual(a.Save("t", 1700000000), b.Save("t", 1700000000), "o estado inteiro igual");
            Assert.AreEqual(a.ApplyOffline(3 * 3600, 1), b.ApplyOffline(3 * 3600, 1), "o cofre offline nao depende da velocidade");
        }

        [Test]
        public void TresX_TresVezesOsPassosPorQuadro_ComOMesmoPasso()
        {
            Sim a = new Sim(), b = new Sim();
            for (int q = 0; q < 60; q++) { Game.Anda(a, Quadro, 1, e => { }); Game.Anda(b, Quadro, 3, e => { }); }
            Assert.AreEqual(30, a.Steps);
            Assert.AreEqual(90, b.Steps);
            Assert.AreEqual(3f, b.Time, 1e-4f);
        }

        [Test]
        public void Pausa_ParaORelogioMasEntregaAsIntencoes()
        {
            var s = new Sim { Cash = 100000 };
            Game.Anda(s, Quadro, 1, e => { });
            long passos = s.Steps;
            var vistos = new List<Ev>();
            s.Buy(Estacao.CheckIn, Melhoria.Velocidade);
            for (int q = 0; q < 120; q++) Game.Anda(s, Quadro, 0, e => vistos.Add(e.Tipo));
            Assert.AreEqual(passos, s.Steps, "na pausa o relogio nao anda");
            CollectionAssert.Contains(vistos, Ev.Melhorou, "a melhoria comprada na pausa chega a tela");
        }
    }
}
