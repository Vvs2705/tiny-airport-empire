using System.Linq;
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

        /// <summary>Save real gravado pela v0.1 (schema 1, main de 2026-10-29, semente 7, onda 2 aos 90 s): voo 1 embarcando, 10 no portao
        /// esperando o voo 2, uma funcionaria andando.</summary>
        const string V01 = "sha256=fe9ea6d207aa239e57f9c156d01e4ff1422542e417be16f38d3399a94943e815\nschema=1\ncontent=0.1.0\nat=1700000000\n" +
            "t=8100\ncash=42\nseed=7\nonda=1\nemb=34\nperd=0\nalbum=0\nbst=0\nema=1.0283608811182714\nsaved=1700000000\nclaim=0\nclaimcash=0\ndia=0\n" +
            "est.0=2,0\nat.0=1512:70\nest.1=2,0\nat.1=1510:55;1511:115\nest.2=0,0\nest.3=1,0\n" +
            "fila.3=1500;1501;1502;1503;1504;1505;1506;1507;1508;1509\nfunc.0=3,0,75\nvoo.0=14,14,112,1,0\nvoo.1=13,0,0,0,0\n";

        [Test]
        public void MigracaoDaV01_AbreComTudoEOVooDaFilaEmbarca()
        {
            Sim s = Sim.Load(V01);
            Assert.IsFalse(s.SaveRefused, "schema 1 migra, nao e' recusado");
            Assert.AreEqual((42L, 8100L, 1, 34L, 1, Estacao.Portao, 75), (s.Cash, s.Steps, s.Onda, s.TotalEmbarcados, s.Staff.Count, s.Staff[0].Em, s.Staff[0].Andando));
            Assert.AreEqual((10, 1, 2, 14, 13), (s.St(Estacao.Portao).Fila.Count, s.St(Estacao.CheckIn).Atendendo.Count, s.St(Estacao.Seguranca).Atendendo.Count,
                s.Voos[0].Embarcados, s.Voos[1].Criados));
            foreach (Aviao a in s.Avioes)
                Assert.AreEqual((true, true, 0, s.Voos[a.Id].Id), (a.NoPatio, a.PatioPronto, a.Desgaste, a.VooId), "aviao " + a.Id + ": no patio, pronto, no voo dele");
            Assert.AreEqual((1, 1, 1, 1), (s.ABordo(0, Funcao.Piloto), s.ABordo(0, Funcao.Comissario), s.ABordo(1, Funcao.Piloto), s.ABordo(1, Funcao.Comissario)));
            Assert.AreEqual(s.T.PatioIniciais, s.EquipePatio);
            string a2 = s.Save("t", T);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", a2);
            Assert.AreEqual(a2, Sim.Load(a2).Save("t", T), "depois de migrar, a ida e volta e' estavel");
            var ev = RegrasTests.Coleta(s, 30f);   // aos 120 s o voo 2 (o dos 10 no portao) abre o embarque, como na v0.1
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.EmbarqueAberto && e.A == 3));
            Assert.Greater(ev.Count(e => e.Tipo == Ev.Embarcou && e.A / 500 % 2 == 1), 0);
        }

        /// <summary>Fechar e reabrir no meio de cada onda, com hangar, checagem, inspecao e tripulacao em jogo: 20 ondas iguais a sem fechar.</summary>
        [Test]
        public void RetomadaComFaseC_ContinuaIgual()
        {
            foreach (Tuning t in new[] { null, new Tuning { RegraTripulacao = true, DescansoSeg = 240f } })
                foreach (BotProfile p in new[] { BotProfile.Planejador, BotProfile.Automatico })
                {
                    Bot.Resultado a = Bot.Medir(p, 3, 20, t), b = Bot.Medir(p, 3, 20, t, true);
                    Assert.AreEqual((a.Receita, a.Embarque, a.Hangares, a.Inspecoes, a.Checagens, a.SemTrip), (b.Receita, b.Embarque, b.Hangares, b.Inspecoes, b.Checagens, b.SemTrip), p.Name);
                    Assert.Greater(a.Hangares + a.Inspecoes, 0, "a manutencao entrou em jogo");
                }
        }

        [Test]
        public void LixoDaFaseC_ViraPadraoLimitado()
        {
            string payload = "t=900\nonda=0\nvoo.0=3,0,0,0,0\npatio=99\npatioatraso=-4\npv.0=9,9\npv.9=1,1\n" +
                             "aviao.0=500,-7,1,99,999999,999999,1,1,-3,1\nsrv.0=99999:1,-5:1,x:1\naviao.1=-9,99999999,0,1,0,0,0,0,999999,0\n" +
                             "trip.0=0,99,999999,0\ntrip.1=7,-3,-3,1\ntrip.2=0,1,1,0\ntrip.3=0,0,0,5\ntrip.4=0,0,0,-1\ntrip.5=0,0,0,-1\ntrip.6=0,0,0,-1\n";
            Sim s = Sim.Load(SaveEnvelope.Wrap(payload, "x", 1));
            Assert.IsFalse(s.SaveRefused);
            Assert.AreEqual((s.T.PatioIniciais + s.T.PatioContratacao.Length, 0, s.T.PatioVelNiveis, s.T.PatioCapNiveis), (s.EquipePatio, s.AtrasosPatio, s.PatioVel[0], s.PatioCap[0]));
            Aviao a0 = s.Avioes[0], a1 = s.Avioes[1];
            Assert.AreEqual((100, true, 0, -1L), (a0.Desgaste, a0.NoPatio, a0.VooId, a0.FechouEm), "voo 99 nao e' desta onda: pega o voo 1 dela");
            Assert.AreEqual(s.ServicoPassos(Servico.Desembarque), a0.Falta[0]);
            Assert.AreEqual((0, 0), (a0.Falta[1], a0.Falta[2]));
            Assert.AreEqual((0, s.T.HangarSeg * Sim.Hz, false, -1), (a1.Desgaste, (float)a1.Hangar, a1.NoPatio, a1.VooId));
            Assert.AreEqual(s.T.TripIniciais + s.T.TripCusto[0].Length, s.Tripulacao.Count(x => x.Funcao == Funcao.Piloto), "pilotos no teto");
            Assert.AreEqual((s.T.TrechosAteDescanso, (int)System.Math.Round(s.T.DescansoSeg * Sim.Hz), 0), (s.Tripulacao[0].Trechos, s.Tripulacao[0].Sala, s.Tripulacao[0].Aviao));
            Assert.AreEqual((Funcao.Comissario, 0, 0, -1), (s.Tripulacao[1].Funcao, s.Tripulacao[1].Trechos, s.Tripulacao[1].Sala, s.Tripulacao[1].Aviao), "comissario a bordo do aviao no hangar: sala");
            Assert.AreEqual((-1, -1), (s.Tripulacao[2].Aviao, s.Tripulacao[3].Aviao), "vaga cheia e aviao que nao existe: sala");
            s.Tick(1f);   // e o jogo anda
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
