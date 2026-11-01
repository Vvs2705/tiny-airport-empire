using System;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Save da v0.1 (CONTRATO_V01 §5 e §8 linha 5): ida e volta, retomada no meio da onda, lixo e schema futuro (o resto em CoreTests).</summary>
    public class SaveTests
    {
        const long T = 1700000000;

        /// <summary>Migrar nao paga a campanha: para comparar com jogar ate ali, os dois sem o Cash dela (CONTRATO_FASE_C5 §5).</summary>
        static readonly Tuning SemC5 = new Tuning { RegraCampanha = false, RegraMissoes = false };

        static Sim Jogado(float seg, int seed = 7, Tuning t = null)
        {
            var s = new Sim(t, seed);
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

        /// <summary>Save real gravado pelo codigo do schema 2 (main de 2026-10-29, PR #12: semente 7, bot Bom, 700 s = onda 4 aos 160 s): Ana e
        /// Beto contratados, 2 pessoas de patio, um aviao em voo com desgaste 40, o outro no patio esperando o portao fechar, a 22 embarques do
        /// marco 1.</summary>
        const string V02 = "sha256=4d706be7ab4c1128d40dfe3ee993f47a6ddc221dbbdefe1282e12a0b63f768f7\nschema=2\ncontent=0.2.0\nat=1700000000\n" +
            "t=21000\ncash=11\nseed=7\nonda=3\nemb=128\nperd=0\nalbum=1\nbst=0\nema=2.449953885136964\nsaved=1700000000\nclaim=0\nclaimcash=0\ndia=0\n" +
            "est.0=4,0\nest.1=4,0\nest.2=0,0\nest.3=3,0\nfunc.0=0,0,0\nfunc.1=1,1,0\nvoo.0=22,22,176,1,1\nvoo.1=22,22,176,1,0\n" +
            "aviao.0=40,22200,0,-1,-1,0,0,1,0,0\nsrv.0=0:0,0:0,0:0,0:0\naviao.1=15,18300,1,7,-1,0,0,1,0,0\nsrv.1=0:0,0:0,0:0,0:0\n" +
            "trip.0=0,2,0,0\ntrip.1=0,0,5400,1\ntrip.2=0,2,2669,-1\ntrip.3=1,2,0,0\ntrip.4=1,0,5400,1\ntrip.5=1,2,2639,-1\npatio=2\npatioatraso=1\n";

        /// <summary>CONTRATO_FASE_C2 §4: o save do schema 2 abre com os perfis pela ordem e da o mesmo jogo que jogar ate ali com o codigo novo,
        /// tambem depois de passar o marco 1 (especializacao e estacoes novas entram igual).</summary>
        [Test]
        public void MigracaoDoSchema2_SaveReal_DaOMesmoJogoQueJogarComOCodigoNovo()
        {
            Sim m = Sim.Load(V02, SemC5, 7);
            Assert.IsFalse(m.SaveRefused, "schema 2 migra, nao e' recusado");
            Assert.AreEqual((11L, 21000L, 128L, 2, 2), (m.Cash, m.Steps, m.TotalEmbarcados, m.Staff.Count, m.EquipePatio));
            CollectionAssert.AreEqual(new[] { Perfil.Atendente, Perfil.Seguranca }, m.Staff.Select(f => f.Perfil), "os generalistas viram perfis pela ordem");
            Assert.AreEqual((40, false, 15, true, 7), (m.Avioes[0].Desgaste, m.Avioes[0].NoPatio, m.Avioes[1].Desgaste, m.Avioes[1].NoPatio, m.Avioes[1].VooId));

            Sim n = Jogado(700f, 7, SemC5);   // o mesmo jogo, jogado do zero pelo codigo novo
            string a = m.Save("0.3.0", T);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", a);
            Assert.AreEqual(SemEma(n.Save("0.3.0", T)), SemEma(a), "migrar = jogar ate ali com o codigo novo");
            Assert.AreEqual(a, Sim.Load(a, SemC5, 7).Save("0.3.0", T), "ida e volta estavel no schema 3");

            var b1 = new Bot(BotProfile.Bom); var b2 = new Bot(BotProfile.Bom);
            for (int i = 0; i < 300 * Sim.Hz; i++) { b1.Step(m); b2.Step(n); }
            Assert.GreaterOrEqual(m.Marco, 1, "passou do marco 1: perfis e estacoes novas em jogo");
            Assert.AreEqual(SemEma(n.Save("0.3.0", T + 1)), SemEma(m.Save("0.3.0", T + 1)));
        }

        /// <summary>Save real gravado pelo codigo do schema 3 (main de 2026-10-30, PR #18: semente 7, bot Bom, 1.000 s = onda 5 aos 100 s): depois do
        /// marco 1, com as 8 estacoes gravadas, filas nas 4 da v0.1 e um servico de patio em andamento.</summary>
        const string V03 = "sha256=28c1323b4eff598349d3bd7b5f7e28280191327108308f96edeb25582435dfa1\nschema=3\ncontent=0.3.0\nat=1700000000\nt=30000\ncash=121\n" +
            "seed=7\nonda=5\nemb=205\nperd=3\nalbum=1\nbst=0\nema=3.068808198567419\nsaved=1700000000\nclaim=0\nclaimcash=0\ndia=0\nest.0=5,0\nfila.0=5523;5524;5525;5526;5527;5528;5529\n" +
            "at.0=5521:2;5522:32\nest.1=5,0\nfila.1=5516;5517;5518;5519;5520\nat.1=5029:15;5515:57\nest.2=0,0\nest.3=4,0\nfila.3=5500;5501;5502;5503;5504;5505;5506;5507;5508;5509;5510;5511;5512;5513;5514\n" +
            "at.3=5028:26\nest.4=0,0\nest.5=0,0\nest.6=0,0\nest.7=0,0\nfunc.0=1,3,0,0\nfunc.1=1,1,0,1\nvoo.0=30,28,280,1,0\nvoo.1=30,0,0,0,0\naviao.0=35,27600,1,10,-1,0,0,1,0,0\n" +
            "srv.0=0:0,0:0,0:0,0:0\naviao.1=25,29100,1,11,-1,0,0,1,0,0\nsrv.1=0:0,0:0,0:0,420:1\ntrip.0=0,3,2399,-1\ntrip.1=0,1,3842,0\ntrip.2=0,1,0,1\n" +
            "trip.3=1,3,2399,-1\ntrip.4=1,1,3812,0\ntrip.5=1,1,0,1\npatio=2\npatioatraso=1\n";

        /// <summary>O Bom sem o que a Fase C3 acrescentou: o jogo que o schema 3 gravava.</summary>
        static Bot SemC3()
        {
            BotProfile p = BotProfile.Bom;
            p.Rotas = p.Viaja = p.Incidentes = p.Clima = p.Falhas = false;
            return new Bot(p);
        }

        /// <summary>CONTRATO_FASE_C3 §6: o save do schema 3 abre em Vale Verde, com as rotas iniciais, e da o mesmo jogo que jogar ate ali com o
        /// codigo novo; e o que entrou (rota, viagem) funciona depois de migrar.</summary>
        [Test]
        public void MigracaoDoSchema3_SaveReal_DaOMesmoJogoQueJogarComOCodigoNovo()
        {
            Tuning t = BalanceTests.SemFaseC4();   // o jogo que o schema 3 gravava, sem o clima que abre no marco 1
            Sim m = Sim.Load(V03, t, 7);
            Assert.IsFalse(m.SaveRefused, "schema 3 migra, nao e' recusado");
            Assert.AreEqual((121L, 30000L, 205L, 2, 1), (m.Cash, m.Steps, m.TotalEmbarcados, m.Staff.Count, m.Marco));
            Assert.AreEqual((0, 3, 0, 1, -1), (m.AeroportoAtual, m.RotasAbertas, m.Prog[0], m.Prog[1], m.ProximoAeroporto));
            Assert.IsNull(m.Inc);
            Assert.AreEqual((0, 1, 0, 0), (m.Voos[0].Rota, m.Voos[1].Rota, m.Voos[0].Grupo, m.Avioes[0].VoltaOnda));
            Assert.AreEqual((30, 30), (m.Voos[0].Total, m.Voos[1].Total));

            Sim n = new Sim(t, 7);
            Bot bn = SemC3();
            for (int i = 0; i < 1000 * Sim.Hz; i++) bn.Step(n);
            string a = m.Save("0.4.0", T);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", a);
            Assert.AreEqual(SemEma(n.Save("0.4.0", T)), SemEma(a), "migrar = jogar ate ali com o codigo novo");
            Assert.AreEqual(a, Sim.Load(a, t, 7).Save("0.4.0", T), "ida e volta estavel");

            Bot b1 = SemC3(), b2 = SemC3();
            for (int i = 0; i < 300 * Sim.Hz; i++) { b1.Step(m); b2.Step(n); }
            Assert.AreEqual(SemEma(n.Save("0.4.0", T + 1)), SemEma(m.Save("0.4.0", T + 1)));
            m.Cash = 1000;
            Assert.IsTrue(m.AbreRota(2), "a rota do marco 1 abre no save migrado");
            Assert.IsTrue(m.Programa(0, 2));
        }

        /// <summary>Save real gravado pelo codigo do schema 4 (main de 2026-10-30, PR #23: semente 7, bot Bom, 700 s = onda 3 aos 160 s): antes do
        /// marco 1, ainda sem clima nem demanda, com os 2 voos da onda e a equipe gravados.</summary>
        const string V04 = "sha256=dca7e568b519139d46fcefe333a5df531c5ee6398f0a0b117f7d2955565caa4d\nschema=4\ncontent=0.4.0\nat=1800000000\nt=21000\ncash=11\n" +
            "seed=7\nonda=3\nemb=128\nperd=0\nalbum=1\nbst=0\nema=2.449953885136964\nsaved=1800000000\nclaim=0\nclaimcash=0\ndia=0\nest.0=4,0\nest.1=4,0\n" +
            "est.2=0,0\nest.3=3,0\nest.4=0,0\nest.5=0,0\nest.6=0,0\nest.7=0,0\nfunc.0=0,0,0,0\nfunc.1=1,1,0,1\nvoo.0=22,22,176,1,1\nvoo.1=22,22,176,1,0\n" +
            "aviao.0=40,22200,0,-1,-1,0,0,1,0,0\nsrv.0=0:0,0:0,0:0,0:0\naviao.1=15,18300,1,7,-1,0,0,1,0,0\nsrv.1=0:0,0:0,0:0,0:0\ntrip.0=0,2,0,0\n" +
            "trip.1=0,0,5400,1\ntrip.2=0,2,2669,-1\ntrip.3=1,2,0,0\ntrip.4=1,0,5400,1\ntrip.5=1,2,2639,-1\npatio=2\npatioatraso=1\naero=0,1,-1\n" +
            "rotas=3\nprog=0,1\nvr.0=0,22,0,0,0,0,1800,0\nvr.1=1,22,0,0,0,0,3300,0\nreparo=0\nincw=-99\n";

        /// <summary>CONTRATO_FASE_C4 §7: o save do schema 4 abre sem nada em andamento e da o mesmo jogo que jogar ate ali com o codigo novo. Depois
        /// os dois passam do marco 1 juntos, com o clima, a demanda e as falhas valendo.</summary>
        [Test]
        public void MigracaoDoSchema4_SaveReal_DaOMesmoJogoQueJogarComOCodigoNovo()
        {
            const long U = 1800000000;   // o instante gravado no save
            Sim m = Sim.Load(V04, SemC5, 7);
            Assert.IsFalse(m.SaveRefused, "schema 4 migra, nao e' recusado");
            Assert.AreEqual((11L, 21000L, 128L, 0, 3), (m.Cash, m.Steps, m.TotalEmbarcados, m.Marco, m.Onda));
            Assert.IsFalse(m.Avioes.Any(a => a.Pane || a.Retorno > 0 || a.SaiEm > 0 || a.Falhou >= 0), "nada em andamento");
            Assert.AreEqual((false, 0L), (m.Voos.Any(v => v.Atrasado), m.PistaAte));

            Sim n = Jogado(700f, 7, SemC5);
            string texto = m.Save("0.5.0", U);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", texto);
            Assert.AreEqual(SemEma(n.Save("0.5.0", U)), SemEma(texto), "migrar = jogar ate ali com o codigo novo");
            Assert.AreEqual(texto, Sim.Load(texto, SemC5, 7).Save("0.5.0", U), "ida e volta estavel");

            var b1 = new Bot(BotProfile.Bom);
            var b2 = new Bot(BotProfile.Bom);
            for (int i = 0; i < 1800 * Sim.Hz; i++) { b1.Step(m); b2.Step(n); }
            Assert.GreaterOrEqual(m.Marco, 1, "passou do marco 1");
            Assert.AreEqual(SemEma(n.Save("0.5.0", U + 1)), SemEma(m.Save("0.5.0", U + 1)));
        }

        /// <summary>Save real gravado pelo codigo do schema 5 (commit 948458f, 2026-11-01: semente 7, bot Bom, 2.790 s = onda 15 aos 90 s): na Costa
        /// Azul, marco 2, com a equipe inteira, 7 rotas abertas, filas cheias e os 2 voos da onda gravados.</summary>
        const string V05 = "sha256=2c88bda8725ac7740f9b059488ee84165c7b48e1fb34b4ceec759aa9237400d4\nschema=5\ncontent=0.5.0\nat=1800000000\nt=83700\ncash=975\nseed=7\n" +
            "onda=15\nemb=959\nperd=45\nalbum=1\nbst=0\nema=9.27154823570382\nsaved=1800000000\nclaim=0\nclaimcash=0\ndia=0\nest.0=5,2\nfila.0=15501;1550" +
            "2;15503;15504;15505;15506;15507;15508;15509;15510;15511;15512;15513;15514;15515;15516;15517;15518;15519;15520;15521;15522;15523;15524;15525;" +
            "15526;15527;15528;15529;15530;15531;15532;15533;15534\nat.0=15036:2;15500:62\nest.1=5,2\nfila.1=15027;15028;15029;15030;15031;15032;15033;15" +
            "034;15035\nat.1=15026:48\nest.2=0,0\nest.3=5,2\nest.4=5,2\nest.5=5,2\nest.6=5,2\nest.7=0,0\nfunc.0=3,1,0,0\nfunc.1=3,1,0,1\nfunc.2=4,3,0,3\n" +
            "func.3=5,0,0,2\nfunc.4=6,0,0,4\nfunc.5=7,0,0,5\nvoo.0=37,26,364,1,0\nvoo.1=35,0,0,0,0\naviao.0=50,81900,1,30,-1,0,0,1,0,0\nsrv.0=0:0,0:0,0:0" +
            ",0:0\naviao.1=100,83700,0,-1,-1,0,0,1,0,0\nsrv.1=0:0,0:0,0:0,0:0\ntrip.0=0,1,3212,0\ntrip.1=0,1,5400,1\ntrip.2=0,3,1799,-1\ntrip.3=1,1,3182," +
            "0\ntrip.4=1,1,5400,1\ntrip.5=1,3,1799,-1\npatio=3\npatioatraso=8\naero=1,3,-1\nrotas=127\nprog=5,6\nvr.0=5,37,0,0,0,0,1800,0\nvr.1=6,46,0,0," +
            "0,0,3300,0\nav.0=5,15\nav.1=5,15\nstt.0=1,31,5.666665818542242\nstt.1=1,30,6.0000005178153515\nreparo=0\nincw=14\n";

        /// <summary>CONTRATO_FASE_C5 §5: com a campanha sem pagar, migrar da o mesmo jogo que jogar ate ali com o codigo novo, e os dois seguem iguais.</summary>
        [Test]
        public void MigracaoDoSchema5_SaveReal_DaOMesmoJogoQueJogarComOCodigoNovo()
        {
            const long U = 1800000000;   // o instante gravado no save
            Sim m = Sim.Load(V05, SemC5, 7);
            Assert.IsFalse(m.SaveRefused, "schema 5 migra, nao e' recusado");
            Assert.AreEqual((975L, 83700L, 959L, 2, 1, 15), (m.Cash, m.Steps, m.TotalEmbarcados, m.Marco, m.AeroportoAtual, m.Onda));
            Assert.AreEqual((0, 0, 0), (m.ObjetivosFeitos(0), m.ObjetivosFeitos(1), m.ObjetivosFeitos(2)), "nada feito");

            Sim n = Jogado(2790f, 7, SemC5);
            string texto = m.Save("0.6.0", U);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", texto);
            StringAssert.Contains("\ncamp.0=0,0,0,0,0,0,0,0,0,0,0\n", texto);
            Assert.AreEqual(SemEma(n.Save("0.6.0", U)), SemEma(texto), "migrar = jogar ate ali com o codigo novo");
            Assert.AreEqual(texto, Sim.Load(texto, SemC5, 7).Save("0.6.0", U), "ida e volta estavel no schema 6");

            var b1 = new Bot(BotProfile.Bom);
            var b2 = new Bot(BotProfile.Bom);
            for (int i = 0; i < 1800 * Sim.Hz; i++) { b1.Step(m); b2.Step(n); }
            Assert.AreEqual(SemEma(n.Save("0.6.0", U + 1)), SemEma(m.Save("0.6.0", U + 1)));
        }

        /// <summary>Com a campanha pagando: o que o estado prova conclui nos primeiros passos e paga uma vez. Contador comeca em zero.</summary>
        [Test]
        public void MigracaoDoSchema5_ComACampanha_OQueOEstadoProvaConclui()
        {
            Sim m = Sim.Load(V05, null, 7);
            int pago = 0;
            for (int i = 0; i < 3; i++)
            {
                m.Tick(Sim.Step);
                pago += m.Events.Where(e => e.Tipo == Ev.ObjetivoConcluido).Sum(e => e.C);
            }
            Assert.AreEqual((0, 1, 0), (m.ObjetivosFeitos(0), m.ObjetivosFeitos(1), m.ObjetivosFeitos(2)), "ja esta na Costa Azul; o resto pede contador");
            Assert.AreEqual(Tabelas.Objetivos[1][0].Recompensa, pago);
            Assert.AreEqual(("vv_embarque", "ca_salvador", "ag_chegada"), (m.ObjetivoAtual(0).Id, m.ObjetivoAtual(1).Id, m.ObjetivoAtual(2).Id));
            long cash = m.Cash;
            Sim b = Sim.Load(m.Save("0.6.0", 1800000001));
            Assert.AreEqual((cash, 1), (b.Cash, b.ObjetivosFeitos(1)), "reabrir nao paga de novo");
        }

        /// <summary>Save real gravado pelo codigo do schema 6 (commit 804cfb7, 2026-11-01: semente 7, bot Bom, 2.790 s = onda 15 aos 90 s): na Costa
        /// Azul, marco 2, com a equipe inteira, 6 objetivos em Vale Verde e 1 na Costa.</summary>
        const string V06 = "sha256=0d4291c77816ae8db7db8266a3e076292f74b06fc585aca4c19ae5efea866aa0\nschema=6\ncontent=0.6.0\nat=1800000000\nt=83700\ncash=1894\nsee" +
            "d=7\nonda=15\nemb=988\nperd=29\nalbum=1\nbst=0\nema=9.266758917094505\nsaved=1800000000\nclaim=0\nclaimcash=0\ndia=0\nest.0=5,2\nfila.0=15501;" +
            "15502;15503;15504;15505;15506;15507;15508;15509;15510;15511;15512;15513;15514;15515;15516;15517;15518;15519;15520;15521;15522;1552" +
            "3;15524;15525;15526;15527;15528;15529;15530;15531;15532;15533;15534\nat.0=15036:2;15500:62\nest.1=5,2\nfila.1=15027;15028;15029;15030" +
            ";15031;15032;15033;15034;15035\nat.1=15026:48\nest.2=0,0\nest.3=5,2\nest.4=5,2\nest.5=5,2\nest.6=5,2\nest.7=0,0\nfunc.0=3,1,0,0\nfunc.1=3,1" +
            ",0,1\nfunc.2=4,3,0,3\nfunc.3=5,0,0,2\nfunc.4=6,0,0,4\nfunc.5=7,0,0,5\nvoo.0=37,26,364,1,0\nvoo.1=35,0,0,0,0\naviao.0=50,81900,1,30,-1,0,0" +
            ",1,0,0\nsrv.0=0:0,0:0,0:0,0:0\naviao.1=100,83700,0,-1,-1,0,0,1,0,0\nsrv.1=0:0,0:0,0:0,0:0\ntrip.0=0,1,3212,0\ntrip.1=0,1,5400,1\ntrip.2=" +
            "0,3,1799,-1\ntrip.3=1,1,3182,0\ntrip.4=1,1,5400,1\ntrip.5=1,3,1799,-1\npatio=3\npatioatraso=8\naero=1,3,-1\nrotas=127\nprog=5,6\nvr.0=5,37," +
            "0,0,0,0,1800,0\nvr.1=6,46,0,0,0,0,3300,0\nav.0=5,15\nav.1=5,15\nstt.0=1,31,5.666665818542242\nstt.1=1,30,6.0000005178153515\nreparo=0\nin" +
            "cw=14\ncamp.0=6,10,0,0,3,0,1,0,1,3,0\ncamp.1=1,0,1,28,21,2,0,0,0,0,0\ncamp.2=0,0,0,0,0,0,0,0,0,0,0\n";

        static readonly Tuning SemC6 = new Tuning { RegraMissoes = false };

        /// <summary>CONTRATO_FASE_C6 §5: com as missoes sem pagar, migrar da o mesmo jogo que jogar ate ali com o codigo novo, e os dois seguem iguais.</summary>
        [Test]
        public void MigracaoDoSchema6_SaveReal_DaOMesmoJogoQueJogarComOCodigoNovo()
        {
            const long U = 1800000000;   // o instante gravado no save
            Sim m = Sim.Load(V06, SemC6, 7);
            Assert.IsFalse(m.SaveRefused, "schema 6 migra, nao e' recusado");
            Assert.AreEqual((1894L, 83700L, 988L, 2, 1, 15), (m.Cash, m.Steps, m.TotalEmbarcados, m.Marco, m.AeroportoAtual, m.Onda));
            Assert.AreEqual((6, 1, 0), (m.ObjetivosFeitos(0), m.ObjetivosFeitos(1), m.ObjetivosFeitos(2)), "a campanha continua");
            Assert.IsNull(m.Diarias[0], "nada de missao ainda");
            Assert.AreEqual(0, m.ConquistasFeitas);

            Sim n = Jogado(2790f, 7, SemC6);
            string texto = m.Save("0.7.0", U);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", texto);
            StringAssert.Contains("\nmiss=-1,-1\n", texto);
            Assert.AreEqual(SemEma(n.Save("0.7.0", U)), SemEma(texto), "migrar = jogar ate ali com o codigo novo");
            Assert.AreEqual(texto, Sim.Load(texto, SemC6, 7).Save("0.7.0", U), "ida e volta estavel no schema 7");

            var b1 = new Bot(BotProfile.Bom);
            var b2 = new Bot(BotProfile.Bom);
            for (int i = 0; i < 1800 * Sim.Hz; i++) { b1.Step(m); b2.Step(n); }
            Assert.AreEqual(SemEma(n.Save("0.7.0", U + 1)), SemEma(m.Save("0.7.0", U + 1)));
        }

        /// <summary>Com as missoes pagando: a lista sai no 1o passo, contador comeca em zero e o que o estado prova conquista uma vez.</summary>
        [Test]
        public void MigracaoDoSchema6_ComAsMissoes_OQueOEstadoProvaConquista()
        {
            Sim m = Sim.Load(V06, null, 7);
            m.Tick(Sim.Step);
            Assert.IsTrue(m.Diarias.All(x => x != null && x.Conta == 0) && m.Semanais.All(x => x != null && x.Conta == 0), "lista nova, do marco atual");
            Assert.IsTrue(m.Diarias.All(x => x.O.Marco == 2));
            int equipe = Array.FindIndex(Tabelas.Conquistas, c => c.Id == "c_equipe"), mil = Array.FindIndex(Tabelas.Conquistas, c => c.Id == "c_passageiros_1");
            Assert.AreEqual((true, false, 988), (m.ConquistaFeita(equipe), m.ConquistaFeita(mil), m.ProgressoConquista(mil)));
            Assert.AreEqual(1, m.Events.Count(e => e.Tipo == Ev.ConquistaFeita));
            Sim b = Sim.Load(m.Save("0.7.0", 1800000001));
            b.Tick(Sim.Step);
            Assert.AreEqual((1, 0), (b.ConquistasFeitas, b.Events.Count(e => e.Tipo == Ev.ConquistaFeita)), "reabrir nao conquista de novo");
        }

        [Test]
        public void Missoes_IdaEVolta_RetomadaIgual()
        {
            Sim s = new Sim(null, 4);
            var b = new Bot(BotProfile.Missionario);
            for (int i = 0; i < 16 * s.OndaPassos + 100; i++) b.Step(s);
            Assert.Greater(s.ConquistasFeitas, 2);
            string a = s.Save("t", T);
            Sim r = Sim.Load(a);
            Assert.AreEqual(a, r.Save("t", T));
            for (int i = 0; i < 3; i++) Assert.AreEqual((s.Diarias[i].O, s.Diarias[i].Conta, s.Diarias[i].Feita), (r.Diarias[i].O, r.Diarias[i].Conta, r.Diarias[i].Feita));
            for (int i = 0; i < 5; i++) Assert.AreEqual((s.Semanais[i].O, s.Semanais[i].Conta, s.Semanais[i].ContaE), (r.Semanais[i].O, r.Semanais[i].Conta, r.Semanais[i].ContaE));
            for (int i = 0; i < Tabelas.Conquistas.Length; i++) Assert.AreEqual(s.ProgressoConquista(i), r.ProgressoConquista(i));
        }

        [Test]
        public void LixoDasMissoes_ViraPadraoLimitado()
        {
            Sim s = Sim.Load(SaveEnvelope.Wrap("t=90\nmiss=99,99\nmd.0=999,1,1,0\nmd.1=-1,0,0,0\nmd.2=0,99999,-5,1\nms.0=0,-5,99999,0\ncq=0,0,0,99999,-3\n", "x", 1));
            Assert.IsTrue(s.Diarias[0] == null && s.Diarias[1] == null, "linha fora da tabela sai");
            Assert.AreEqual((Tabelas.Diarias[0].Alvo, 0, true), (s.Diarias[2].Conta, s.Diarias[2].ContaE, s.Diarias[2].Feita), "contador no teto; diaria nao tem 2a parte");
            Assert.AreEqual((0, Tabelas.Semanais[0].E.Alvo), (s.Semanais[0].Conta, s.Semanais[0].ContaE));
            Assert.AreEqual((Tabelas.Conquistas[2].Alvo, 0), (s.ProgressoConquista(2), s.ProgressoConquista(3)), "contador de conquista no teto");
            s.Tick(Sim.Step);
            Assert.AreEqual(Tabelas.Diarias[0], s.Diarias[2].O, "dia gravado no futuro nao sorteia de novo");
            Sim neg = Sim.Load(SaveEnvelope.Wrap("t=90\nmiss=-9,-9\ncq=-1\n", "x", 1));
            Assert.AreEqual(Tabelas.Conquistas.Length, neg.ConquistasFeitas, "mascara limitada a tabela");
            neg.Tick(Sim.Step);
            Assert.IsNotNull(neg.Diarias[0], "dia invalido: lista nova");
        }

        [Test]
        public void Campanha_IdaEVolta_RetomadaIgual()
        {
            Sim s = new Sim(null, 4);
            var b = new Bot(BotProfile.Guiado);
            for (int i = 0; i < 16 * s.OndaPassos + 100; i++) b.Step(s);
            Assert.Greater(s.ObjetivosFeitos(0), 3);
            string a = s.Save("t", T);
            Sim r = Sim.Load(a);
            Assert.AreEqual(a, r.Save("t", T));
            for (int e = 0; e < 3; e++) Assert.AreEqual((s.ObjetivosFeitos(e), s.Progresso(e)), (r.ObjetivosFeitos(e), r.Progresso(e)));
        }

        [Test]
        public void LixoDaCampanha_ViraPadraoLimitado()
        {
            Sim s = Sim.Load(SaveEnvelope.Wrap("t=90\nemb=10\ncash=5\ncamp.0=99,99\ncamp.1=-3,-3,99999\ncamp.2=1,0,99999\n", "x", 1));
            Assert.AreEqual((10, 0, 1), (s.ObjetivosFeitos(0), s.ObjetivosFeitos(1), s.ObjetivosFeitos(2)), "feitos no teto da cadeia");
            Assert.IsNull(s.ObjetivoAtual(0));
            Assert.AreEqual(Tabelas.Objetivos[2][1].Alvo, s.Progresso(2), "contador no alvo do objetivo");
            Assert.AreEqual(5L, s.Cash, "ler o save nao paga");
            Sim x = Sim.Load(SaveEnvelope.Wrap("camp.0=a,b,c\n", "x", 1));
            Assert.AreEqual((0, 0, 0), (x.ObjetivosFeitos(0), x.ObjetivosFeitos(1), x.ObjetivosFeitos(2)));
        }

        /// <summary>Fechar e reabrir no meio de cada onda com clima, demanda e falhas em jogo: 40 ondas iguais a sem fechar.</summary>
        [Test]
        public void RetomadaComFaseC4_ContinuaIgual()
        {
            int climas = 0, falhas = 0;
            foreach (int aero in new[] { 0, 1, 2 })
                foreach (BotProfile p in new[] { BotProfile.Bom, BotProfile.Lento })
                    for (int seed = 1; seed <= 2; seed++)
                    {
                        Tuning t = BalanceTests.AbertoC4(aero);
                        Bot.Resultado a = Bot.Medir(p, seed, 40, t), b = Bot.Medir(p, seed, 40, t, true);
                        Assert.AreEqual((a.Receita, a.Embarque, a.Climas, a.Falhas, a.Reparos), (b.Receita, b.Embarque, b.Climas, b.Falhas, b.Reparos), p.Name + " semente " + seed);
                        climas += a.Climas; falhas += a.Falhas;
                    }
            Assert.Greater(climas, 0, "o clima entrou em jogo");
            Assert.Greater(falhas, 0, "as falhas entraram em jogo");
        }

        [Test]
        public void FaseC4_EmAndamento_VoltaDoSave()
        {
            Sim s = EstacoesPerfisTests.Pronta(ClimaFalhasTests.ComClima(TipoIncidente.Temporal));
            Incidente i = RotasIncidentesTests.AteOAviso(s);
            Assert.AreEqual(TipoIncidente.Temporal, i.Tipo);
            Sim b = Sim.Load(s.Save("x", T), s.T, s.Seed);
            Assert.AreEqual((TipoIncidente.Temporal, i.Inicio, i.Fim), (b.Inc.Tipo, b.Inc.Inicio, b.Inc.Fim), "tipo novo volta do save");

            Sim f = EstacoesPerfisTests.Pronta(ClimaFalhasTests.ComClima(TipoIncidente.FrenteFria));
            Incidente j;
            do { RotasIncidentesTests.Ate(f, f.Steps + f.OndaPassos); j = RotasIncidentesTests.AteOAviso(f); } while (j.Vaga != 0);
            Assert.IsTrue(f.Atrasa());
            Sim g = Sim.Load(f.Save("x", T), f.T, f.Seed);
            Assert.AreEqual((f.Voos[0].FechaPasso, true), (g.Voos[0].FechaPasso, g.Voos[0].Atrasado), "o portao atrasado continua atrasado");

            Sim p = EstacoesPerfisTests.Pronta(ClimaFalhasTests.ComFalha(TipoFalha.Pane));
            RegrasTests.Run(p, 21f);
            p.Avioes[0].Desgaste = 85;
            Assert.IsNotNull(ClimaFalhasTests.Espera(p, Ev.Pane));
            Sim q = Sim.Load(p.Save("x", T), p.T, p.Seed);
            Assert.AreEqual((true, p.Avioes[0].Falhou), (q.Avioes[0].Pane, q.Avioes[0].Falhou), "a pane volta e nao sorteia de novo");

            Sim r = EstacoesPerfisTests.Pronta(ClimaFalhasTests.ComFalha(TipoFalha.Rejeicao));
            RegrasTests.Run(r, 21f);
            r.Avioes[0].Desgaste = 85;
            Assert.IsNotNull(ClimaFalhasTests.Espera(r, Ev.Rejeicao));
            Sim u = Sim.Load(r.Save("x", T), r.T, r.Seed);
            Assert.AreEqual((r.Avioes[0].SaiEm, r.PistaAte), (u.Avioes[0].SaiEm, u.PistaAte), "a rejeicao continua de onde estava");

            Sim v = EstacoesPerfisTests.Pronta(ClimaFalhasTests.ComFalha(TipoFalha.Retorno));
            RegrasTests.Run(v, 21f);
            v.Avioes[0].Desgaste = 85;
            Assert.IsNotNull(ClimaFalhasTests.Espera(v, Ev.Decolou));
            Sim w = Sim.Load(v.Save("x", T), v.T, v.Seed);
            Assert.AreEqual((v.Avioes[0].Retorno, v.Avioes[0].Chega), (w.Avioes[0].Retorno, w.Avioes[0].Chega), "o retorno continua no ar");
            Assert.IsNotNull(ClimaFalhasTests.Espera(w, Ev.PousoSeguro, 60f), "e pousa");
        }

        [Test]
        public void LixoDaFaseC4_ViraPadraoLimitado()
        {
            Sim s = EstacoesPerfisTests.Pronta(ClimaFalhasTests.ComFalha(TipoFalha.Pane));
            RegrasTests.Run(s, 21f);
            string p = SaveEnvelope.Unwrap(s.Save("x", T)).Payload + "falha.0=1,-9,999999999,999999999\nfalha.1=x,y\npista=999999999\nva.0=-5\nva.1=999999\n";
            Sim b = Sim.Load(SaveEnvelope.Wrap(p, "x", T), s.T, s.Seed);
            Assert.IsFalse(b.SaveRefused);
            Assert.AreEqual((-1, 0L, 0L), (b.Avioes[0].Falhou, b.Avioes[0].SaiEm, b.Avioes[0].Retorno), "sem voo fechado nem aviao no ar");
            Assert.LessOrEqual(b.PistaAte, b.Steps + (long)(s.T.PistaBloqueioSeg * Sim.Hz));
            Assert.Less(b.Voos[1].FechaPasso, b.OndaPassos, "portao nunca passa da virada");
            Assert.AreEqual(s.Voos[0].FechaPasso, b.Voos[0].FechaPasso, "portao nunca fecha antes da hora");
        }

        /// <summary>Fechar e reabrir no meio de cada onda, com rotas, viagem, rota longa e os 4 incidentes em jogo: 20 ondas iguais a sem fechar.</summary>
        [Test]
        public void RetomadaComFaseC3_ContinuaIgual()
        {
            foreach (int aero in new[] { 1, 2 })
                foreach (BotProfile p in new[] { BotProfile.Bom, BotProfile.Lento })
                    for (int seed = 1; seed <= 3; seed++)
                    {
                        Tuning t = BalanceTests.AbertoC3(aero);
                        Bot.Resultado a = Bot.Medir(p, seed, 20, t), b = Bot.Medir(p, seed, 20, t, true);
                        Assert.AreEqual((a.Receita, a.Embarque, a.Incidentes, a.Reacoes, a.Viagens), (b.Receita, b.Embarque, b.Incidentes, b.Reacoes, b.Viagens), p.Name + " semente " + seed);
                        Assert.Greater(a.Incidentes, 0, "os incidentes entraram em jogo");
                    }
        }

        [Test]
        public void LixoDaFaseC3_ViraPadraoLimitado()
        {
            string payload = "t=90\nemb=10\nonda=0\naero=9,-1,9\nrotas=999999\nprog=99,-4\nvr.0=99,99999,999,-5,1,1,-9,999999\nvoo.0=5,0,0,0,0\n" +
                             "av.0=99,99999\ninc=9,9,9,5,4,3,1\nreparo=99999\nincw=99999\n";
            Sim s = Sim.Load(SaveEnvelope.Wrap(payload, "x", 1));
            Assert.IsFalse(s.SaveRefused);
            Assert.AreEqual((0, -1, 3), (s.AeroportoAtual, s.ProximoAeroporto, s.RotasAbertas), "aeroporto e rota acima do marco nao valem");
            Assert.AreEqual((0, 1), (s.Prog[0], s.Prog[1]));
            Assert.IsNull(s.Inc);
            Assert.AreEqual(0, s.Reparo);
            Assert.AreEqual((0, 2), (s.Avioes[0].Rota, s.Avioes[0].VoltaOnda));
            Voo v = s.Voos[0];
            Assert.AreEqual((0, 499, s.T.Pax(0)), (v.Rota, v.Total, v.Grupo));
            Assert.AreEqual((0, (int)(s.T.Fecha[0] * Sim.Hz)), (v.AbrePasso, v.FechaPasso));
            s.Tick(1f);   // e o jogo anda

            string inc = "t=5400\nemb=10\nonda=0\ninc=1,1,1,5000,5500,99999999,0\n";
            Assert.IsNull(Sim.Load(SaveEnvelope.Wrap(inc, "x", 1)).Inc, "incidente fora do relogio: nenhum");
        }

        /// <summary>O save sem o hash, sem a media da renda, sem o estado das estacoes (2 s depois de reabrir ele e' o mesmo) e sem a campanha
        /// (migrar comeca do zero, jogar nao): o V02 foi gravado
        /// no .NET e o Mono do Unity difere no ultimo digito do double. Em jogo o save e' lido pelo mesmo runtime que gravou (IdaEVolta cobre o ema).</summary>
        static string SemEma(string save) =>
            string.Join("\n", save.Split('\n').Where(l => !l.StartsWith("ema=") && !l.StartsWith("sha256=") && !l.StartsWith("stt.") && !l.StartsWith("camp.") && !l.StartsWith("miss=") && !l.StartsWith("md.") && !l.StartsWith("ms.") && !l.StartsWith("cq=")));

        [Test]
        public void LixoDaFaseC2_ViraPadraoLimitado()
        {
            string payload = "t=90\nemb=10\nest.6=3,1\nfunc.0=6,6,0,4\nfunc.1=0,0,0,4\nfunc.2=1,1,0,99\nfunc.3=0,0,0\nat.4=1:5\n";
            Sim s = Sim.Load(SaveEnvelope.Wrap(payload, "x", 1));
            Assert.IsFalse(s.SaveRefused);
            Assert.AreEqual((0, 0), (s.St(Estacao.Manutencao).Vel, s.St(Estacao.Manutencao).Cap), "oficina ainda fechada: nivel do save nao vale");
            Assert.AreEqual(1, s.Staff.Count, "perfil repetido encerra a lista");
            Assert.AreEqual((Perfil.Mecanico, Estacao.Portao), (s.Staff[0].Perfil, s.Staff[0].Em), "estacao fechada cai nas 4 da v0.1");
            Assert.AreEqual(0, s.St(Estacao.Bagagem).Pessoas);
            s.Tick(1f);
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
