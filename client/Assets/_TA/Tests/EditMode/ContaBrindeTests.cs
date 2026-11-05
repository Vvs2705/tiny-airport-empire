using System;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>
    /// Frente de contas (Sim.Conta.cs, docs/CONTAS.md): o brinde de ligar a conta. Uma vez por save (e o save e' o da conta), valor fixo,
    /// o booster no teto do passe, e o schema 19 que guarda o "ja deu".
    /// </summary>
    public class ContaBrindeTests
    {
        const long U = 1800000000;

        [Test]
        public void Brinde_UmaVez_GemsEBooster_DaTabela()
        {
            var s = new Sim(null, 7);
            long gems = s.Gems;
            Assert.IsFalse(s.BrindeConta);
            Assert.IsTrue(s.DaBrindeConta());
            Assert.AreEqual(gems + s.T.BrindeContaGems, s.Gems);
            Assert.AreEqual(s.T.BrindeContaBoostSeg * Sim.Hz, s.BoostLeft);
            Assert.IsTrue(s.BrindeConta);
            Assert.IsFalse(s.DaBrindeConta(), "segunda vez nao paga");
            Assert.AreEqual(gems + s.T.BrindeContaGems, s.Gems);
        }

        [Test]
        public void Brinde_VaiNoSave_EDepoisDeCarregarNaoPagaDeNovo()
        {
            var s = new Sim(null, 7);
            Assert.IsTrue(s.DaBrindeConta());
            string texto = s.Save("0.19.0", U);
            StringAssert.Contains("\nbrc=1\n", texto);
            Sim c = Sim.Load(texto, null, 7);
            Assert.IsTrue(c.BrindeConta);
            Assert.IsFalse(c.DaBrindeConta(), "trocar de aparelho ou baixar o save da nuvem nao paga 2x");
            Assert.AreEqual(s.Gems, c.Gems);
        }

        [Test]
        public void Brinde_BoosterNaoPassaDoTeto_ENaoEncurtaOQueJaTem()
        {
            var s = new Sim(null, 7);
            s.BoostLeft = int.MaxValue / 2;
            s.DaBrindeConta();
            Assert.LessOrEqual(s.BoostLeft, 2 * Math.Max(s.T.BoostSegundos, s.T.BoosterSeg) * Sim.Hz);
            var t = new Sim(null, 7);
            t.BoostLeft = 5;
            t.DaBrindeConta();
            Assert.AreEqual(t.T.BrindeContaBoostSeg * Sim.Hz, t.BoostLeft);
        }

        [Test]
        public void Brinde_NoTetoDasGems_NaoPassa()
        {
            var s = new Sim(null, 7);
            s.Gems = s.T.GemsTeto - 10;
            Assert.IsTrue(s.DaBrindeConta());
            Assert.AreEqual(s.T.GemsTeto, s.Gems);
        }

        [Test]
        public void Brinde_SaveRecusado_NaoPaga()
        {
            Sim s = Sim.Load("sha256=00\nlixo", null, 7);
            Assert.IsTrue(s.SaveRefused);
            Assert.IsFalse(s.DaBrindeConta());
            Assert.AreEqual(0, s.Gems);
        }

        [Test]
        public void Brinde_Evento_GemsGanhasComAeroportoMenosUm()
        {
            var s = new Sim(null, 7);
            s.Events.Clear();
            s.DaBrindeConta();
            Assert.IsTrue(s.Events.Exists(e => e.Tipo == Ev.GemsGanhas && e.A == -1 && e.B == s.T.BrindeContaGems && e.C == s.T.BrindeContaBoostSeg));
        }

        /// <summary>O schema 18 (sem a chave) abre sem o brinde dado, e um save sem brinde grava sem a chave: o formato do 18 nao mudou.</summary>
        [Test]
        public void MigracaoDoSchema18_SemBrinde_MesmoSave()
        {
            var n = new Sim(null, 7);
            var b = new Bot(BotProfile.Bom);
            for (int i = 0; i < 300 * Sim.Hz; i++) b.Step(n);
            string atual = n.Save("0.19.0", U);
            StringAssert.DoesNotContain("\nbrc=", atual);
            string v18 = ComSchema(atual, 18);
            Sim m = Sim.Load(v18, null, 7);
            Assert.IsFalse(m.SaveRefused, "schema 18 migra, nao e' recusado");
            Assert.IsFalse(m.BrindeConta);
            Assert.AreEqual(n.Gems, m.Gems);
            Assert.IsTrue(m.DaBrindeConta(), "quem vem do 18 ainda ganha o brinde ao ligar a conta");
        }

        /// <summary>O mesmo save com outro numero de schema no cabecalho (e a soma refeita).</summary>
        static string ComSchema(string texto, int schema)
        {
            string body = texto.Substring(texto.IndexOf('\n') + 1).Replace("schema=" + Sim.Schema + "\n", "schema=" + schema + "\n");
            using (SHA256 sha = SHA256.Create())
                return "sha256=" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(body))).Replace("-", "").ToLowerInvariant() + "\n" + body;
        }
    }
}
