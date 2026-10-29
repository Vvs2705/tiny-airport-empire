using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Save da v0.1 (CONTRATO_V01 §5 e §8 linha 5): ida e volta, retomada no meio da onda, lixo e schema futuro (o resto em CoreTests).</summary>
    public class SaveTests
    {
        const long T = 1700000000;

        static Sim Jogado(float seg, int seed = 7)
        {
            var s = new Sim(null, seed);
            var b = new Bot(BotProfile.Bom);
            for (int i = 0; i < (int)(seg * Sim.Hz); i++) b.Step(s);
            return s;
        }

        [Test]
        public void IdaEVolta_TextoEstavel()
        {
            Sim s = Jogado(250f);
            string a = s.Save("t", T);
            Sim b = Sim.Load(a);
            Assert.IsFalse(b.SaveRefused);
            Assert.AreEqual(a, b.Save("t", T), "Load(Save(x)).Save == Save(x)");
            Assert.AreEqual((s.Cash, s.Steps, s.Onda, s.TotalEmbarcados, s.Staff.Count), (b.Cash, b.Steps, b.Onda, b.TotalEmbarcados, b.Staff.Count));
        }

        /// <summary>Fechar no meio da onda (com fila, gente em atendimento e funcionario andando) e reabrir: o jogo continua exatamente igual,
        /// porque o relogio do quadro ficou parado e as chegadas sao funcao da semente.</summary>
        [Test]
        public void RetomadaNoMeioDaOnda_ContinuaIgual()
        {
            Sim s = Jogado(270f);   // onda 2, aos 90 s: os dois voos com fila
            Assert.Greater(s.St(Estacao.CheckIn).Pessoas + s.St(Estacao.Seguranca).Pessoas, 0, "tem gente no meio do fluxo");
            s.Move(0, s.Staff[0].Em == Estacao.Portao ? Estacao.CheckIn : Estacao.Portao);   // e um funcionario andando
            Sim b = Sim.Load(s.Save("t", T));
            var b1 = new Bot(BotProfile.Bom); var b2 = new Bot(BotProfile.Bom);
            for (int i = 0; i < 400 * Sim.Hz; i++) { b1.Step(s); b2.Step(b); }
            Assert.AreEqual(s.Save("t", T + 1), b.Save("t", T + 1));
            Assert.Greater(s.Onda, 2);
        }

        /// <summary>O -autoplay do build: retomada igual e linha com os numeros (o portao do verify.sh full le esta linha).</summary>
        [Test]
        public void Autoplay_RetomadaIgual_ImprimeOsNumeros()
        {
            string linha = Bot.Autoplay(2, out bool ok);
            Assert.IsTrue(ok, linha);
            StringAssert.StartsWith("AUTOPLAY OK ondas=2 embarque_bom=", linha);
            StringAssert.Contains("ganho_mover=", linha);
            StringAssert.EndsWith("retomada=igual", linha);
        }

        [Test]
        public void Lixo_ViraPadraoLimitado()
        {
            string payload = "t=90\ncash=-5\nseed=3\nonda=0\nemb=-9\nalbum=-1\nbst=999999999\nema=NaN\nest.0=99,99\nest.1=-3,x\n" +
                             "func.0=9,9,99999\nfunc.1=0,0,0\nfunc.2=0,0,0\nfunc.3=0,0,0\nfunc.4=1,1,0\n" +
                             "voo.0=999,999,-5,1,0\nfila.0=5;5;12;99999;abc;1005\nat.1=3:999999;4:-2\nad.cofre2x=-1,-1\nad.xx=1,1\n";
            Sim s = Sim.Load(SaveEnvelope.Wrap(payload, "x", 1));
            Assert.IsFalse(s.SaveRefused);
            Assert.AreEqual((0L, 0L, 0, 0.0), (s.Cash, s.TotalEmbarcados, s.Album, s.RateEma));
            Assert.AreEqual(s.T.BoostSegundos * Sim.Hz, s.BoostLeft);
            Assert.AreEqual((5, 2, 4), (s.St(Estacao.CheckIn).Vel, s.St(Estacao.CheckIn).Cap, s.St(Estacao.CheckIn).Postos));
            Assert.AreEqual(Sim.MaxFuncionarios, s.Staff.Count);
            Assert.AreEqual((Estacao.Portao, 75), (s.Staff[0].Em, s.Staff[0].Andando));
            Assert.AreEqual(10, s.Voos[0].Criados, "criados no total do voo");
            Assert.AreEqual(1, s.St(Estacao.CheckIn).Fila.Count, "duplicado, de outra onda, de outro voo ou lixo: sai");
            Assert.AreEqual(2, s.St(Estacao.Seguranca).Atendendo.Count);
            Assert.AreEqual(s.TempoPassos(Estacao.Seguranca), s.St(Estacao.Seguranca).Atendendo[0].Resto);
            Assert.AreEqual(1, s.St(Estacao.Seguranca).Atendendo[1].Resto);
            Assert.AreEqual(0, s.Anuncios.Pago[0]);
            s.Tick(1f);   // e o jogo anda
        }

        [Test]
        public void OndaQueNaoBateComORelogio_ComecaADoRelogio()
        {
            Sim s = Sim.Load(SaveEnvelope.Wrap("t=5500\nonda=0\nvoo.0=10,3,24,1,0\nfila.0=5\n", "x", 1));
            Assert.AreEqual(-1, s.Onda);
            s.Tick(Sim.Step);
            Assert.AreEqual((1, 0), (s.Onda, s.Voos[0].Embarcados));
        }

        [Test]
        public void SchemaFuturo_RecusaENaoGrava()
        {
            string futuro = SaveEnvelope.Wrap("cash=5\n", "x", 1).Replace("schema=" + Sim.Schema, "schema=" + (Sim.Schema + 1));
            Sim s = Sim.Load(futuro);
            Assert.IsTrue(s.SaveRefused, "schema futuro (ou o SHA que nao bate) nunca abre como estado novo calado");
            Assert.IsNull(s.Save("x", 2));
        }
    }
}
