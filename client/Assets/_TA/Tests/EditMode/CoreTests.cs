using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Esqueleto da Fase A: relogio de passo fixo e save com envelope. Rodam no Unity (EditMode) e no dotnet test client/tools/coretests.</summary>
    public class CoreTests
    {
        [Test]
        public void Tick_AvancaEmPassosFixos()
        {
            var s = new Sim();
            s.Tick(0.1f);
            Assert.AreEqual(3, s.Steps);
            s.Tick(1f / 60f);
            s.Tick(1f / 60f);
            Assert.AreEqual(4, s.Steps);
        }

        [Test]
        public void Tick_DtInvalidoNaoAndaNemTrava()
        {
            var s = new Sim();
            s.Tick(0f);
            s.Tick(-1f);
            s.Tick(float.NaN);
            Assert.AreEqual(0, s.Steps);
            s.Tick(0.1f);
            Assert.AreEqual(3, s.Steps, "depois de um NaN o relogio continua andando");
        }

        [Test]
        public void Tick_QuadroLongoLimitadoAoMaxFrame()
        {
            var s = new Sim();
            s.Tick(60f);
            Assert.AreEqual(7, s.Steps);   // 0,25 s = 7,5 passos; o resto fica para o proximo quadro
        }

        [Test]
        public void Save_IdaEVolta()
        {
            var s = new Sim { Cash = 1234 };
            s.Tick(0.2f);
            Sim back = Sim.Load(s.Save("teste", 1700000000));
            Assert.IsFalse(back.SaveRefused);
            Assert.AreEqual(s.Steps, back.Steps);
            Assert.AreEqual(1234, back.Cash);
        }

        [Test]
        public void Load_VazioEJogoNovo()
        {
            Sim s = Sim.Load("");
            Assert.IsFalse(s.SaveRefused);
            Assert.AreEqual(0, s.Steps);
            Assert.IsFalse(Sim.Load(null).SaveRefused);
        }

        [Test]
        public void Load_CorrompidoRecusaENaoGravaPorCima()
        {
            string t = new Sim { Cash = 50 }.Save("teste", 1);
            Sim s = Sim.Load(t.Replace("cash=50", "cash=99"));
            Assert.IsTrue(s.SaveRefused);
            Assert.IsNull(s.Save("teste", 2));
        }

        [Test]
        public void Load_SchemaFuturoRecusa()
        {
            // envelope valido de um build mais novo: mesma soma que o Wrap faz, com schema + 1
            string body = "schema=" + (Sim.Schema + 1) + "\ncontent=x\nat=1\nt=1\ncash=1\n";
            string hash;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                hash = System.BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(body))).Replace("-", "").ToLowerInvariant();
            string futuro = "sha256=" + hash + "\n" + body;
            Assert.AreEqual(SaveEnvelope.Status.NewerSchema, SaveEnvelope.Unwrap(futuro).Status);
            Sim s = Sim.Load(futuro);
            Assert.IsTrue(s.SaveRefused);
            Assert.IsNull(s.Save("teste", 2));
        }

        [Test]
        public void Load_SemCabecalhoRecusa()
        {
            Assert.AreEqual(SaveEnvelope.Status.Legacy, SaveEnvelope.Unwrap("t=1\ncash=1\n").Status);
            Assert.IsTrue(Sim.Load("t=1\ncash=1\n").SaveRefused);
        }

        [Test]
        public void Load_CashNegativoViraZero()
        {
            Assert.AreEqual(0, Sim.Load(SaveEnvelope.Wrap("cash=-5\n", "x", 1)).Cash);
        }

        [Test]
        public void Envelope_GuardaVersaoEData()
        {
            SaveEnvelope.Result r = SaveEnvelope.Unwrap(new Sim().Save("0.0.1-alpha", 1700000000));
            Assert.AreEqual(SaveEnvelope.Status.Ok, r.Status);
            Assert.AreEqual(Sim.Schema, r.Schema);
            Assert.AreEqual("0.0.1-alpha", r.Content);
            Assert.AreEqual(1700000000, r.WrittenAt);
        }
    }
}
