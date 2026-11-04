using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>
    /// Frente ta-suprimentos (Sim.Suprimentos.cs): combustivel e refeicoes com estoque, consumo no fim do servico do patio, reposicao automatica
    /// com prazo e custo fixos na conta do dia, aviso antes de faltar e o tanque maior. Faltar so atrasa a decolagem: o embarque nunca espera,
    /// ninguem perde o voo por isso, e a conta atrasada nunca segura a entrega. As travas estao no mutacao.py ("suprimento:").
    /// </summary>
    public class SuprimentosTests
    {
        const int Comb = (int)Suprimento.Combustivel, Ref = (int)Suprimento.Refeicoes;

        /// <summary>Sem incidente, clima e falha: o patio da onda 0 sai igual com e sem a regra. `prazo` = s da entrega dos dois.</summary>
        static Tuning Calmo(float prazo = 0f, bool regra = true)
        {
            var t = new Tuning { RegraIncidentes = false, RegraClima = false, RegraEpocas = false, RegraPicos = false, RegraFalhas = false, RegraSuprimentos = regra };
            if (prazo > 0f) t.SupPrazo = new[] { prazo, prazo };
            return t;
        }

        /// <summary>Anda com o bot e guarda cada evento com o passo em que saiu (o Tick ja avancou o relogio: o passo e' Steps - 1).</summary>
        static List<(long Passo, Evento E)> Joga(Sim s, Bot b, float seg)
        {
            var l = new List<(long, Evento)>();
            int n = (int)Math.Round(seg * Sim.Hz);
            for (int i = 0; i < n; i++) { b.Step(s); foreach (Evento e in s.Events) l.Add((s.Steps - 1, e)); }
            return l;
        }

        static long Primeiro(List<(long Passo, Evento E)> l, Func<Evento, bool> f) => l.Where(x => f(x.E)).Select(x => x.Passo).DefaultIfEmpty(-1).First();

        [Test]
        public void JogoNovo_DepositoCheio_NadaACaminho_UmCaminhaoPorSuprimento()
        {
            var s = new Sim(Calmo(), 1);
            Assert.AreEqual((8, 8, 0, 0, -1L), (s.Estoque[Comb], s.Estoque[Ref], s.Pedido[Comb], s.Pedido[Ref], s.EntregaEm(Suprimento.Combustivel)));
            Assert.AreEqual((Servico.Combustivel, Servico.Limpeza), (Sim.ServicoDe(Suprimento.Combustivel), Sim.ServicoDe(Suprimento.Refeicoes)), "o catering e' o caminhao da limpeza");
            Assert.AreEqual((1, 2), (s.Gasto(0, Suprimento.Combustivel), s.Gasto(1, Suprimento.Refeicoes)), "o jato leva o dobro do regional");
        }

        /// <summary>O deposito vazio e a entrega so aos 150 s: o regional embarca na hora, perde ninguem, e so decola quando a entrega chega. O portao fecha
        /// com o caminhao esperando: o registro diz que foi o suprimento.</summary>
        [Test]
        public void Falta_SoAtrasaADecolagem_OEmbarqueAbreNaHora_EORegistroDizSuprimento()
        {
            Sim a = new Sim(Calmo(150f), 3), b = new Sim(Calmo(150f, false), 3);
            a.Estoque[Comb] = a.Estoque[Ref] = 0;
            var la = Joga(a, new Bot(BotProfile.Bom), 180f);
            var lb = Joga(b, new Bot(BotProfile.Bom), 180f);
            long entrega = 150 * Sim.Hz;
            Assert.AreEqual(Primeiro(lb, e => e.Tipo == Ev.EmbarqueAberto && e.A == 0), Primeiro(la, e => e.Tipo == Ev.EmbarqueAberto && e.A == 0), "o embarque nao espera suprimento");
            Evento pa = la.Single(x => x.E.Tipo == Ev.VooPartiu && x.E.A == 0).E, pb = lb.Single(x => x.E.Tipo == Ev.VooPartiu && x.E.A == 0).E;
            Assert.AreEqual((pb.B, pb.C), (pa.B, pa.C), "embarca quem embarcaria sem a falta: ninguem perde o voo");
            Assert.IsTrue(la.Any(x => x.E.Tipo == Ev.SuprimentoFaltou && x.E.A == 0 && x.E.B == Ref), "o catering faltou ao regional");
            Assert.IsTrue(la.Any(x => x.E.Tipo == Ev.SuprimentoFaltou && x.E.A == 0 && x.E.B == Comb), "o combustivel faltou ao regional");
            long da = Primeiro(la, e => e.Tipo == Ev.Decolou && e.A == 0), db = Primeiro(lb, e => e.Tipo == Ev.Decolou && e.A == 0);
            Assert.That(db, Is.GreaterThan(0L).And.LessThan(entrega), "sem a regra decola antes da entrega");
            Assert.GreaterOrEqual(da, entrega, "com a falta, so decola depois da entrega");
            Assert.AreEqual(Primeiro(la, e => e.Tipo == Ev.SuprimentoChegou && e.A == Comb), entrega, "a entrega chega no prazo");
            Ocorrencia o = a.Ocorrencias.Single(x => x.Voo == 0);
            Assert.AreEqual((TipoOcorrencia.VooAtrasado, Motivo.Suprimento, 0), (o.Tipo, o.Motivo, o.Perdidos), "o registro explica o atraso");
            Assert.Greater(o.Atraso, 0);
        }

        /// <summary>1 carga de combustivel e a entrega so aos 150 s: o regional leva a ultima; o jato, que pousa antes da entrega, vai faltar. O aviso
        /// vem antes da falta e some quando a entrega chega.</summary>
        [Test]
        public void Aviso_VemAntesDaFalta_ESomeComAEntrega()
        {
            var s = new Sim(Calmo(150f), 3);
            s.Estoque[Comb] = 1;
            var b = new Bot(BotProfile.Bom);
            long aviso = -1, faltou = -1, depois = -1;
            for (int i = 0; i < 180 * Sim.Hz; i++)
            {
                b.Step(s);
                int quem = s.VaiFaltar(Suprimento.Combustivel);
                if (quem >= 0 && aviso < 0) { aviso = s.Steps; Assert.AreEqual(1, quem, "o jato e' quem vai faltar"); }
                foreach (Evento e in s.Events)
                {
                    if (e.Tipo == Ev.SuprimentoFaltou && e.B == Comb) { Assert.AreEqual(1, e.A, "o regional leva a ultima carga"); if (faltou < 0) faltou = s.Steps - 1; }
                    if (e.Tipo == Ev.SuprimentoChegou && e.A == Comb) depois = s.Steps - 1;
                }
                if (depois >= 0 && s.Steps > depois) Assert.AreEqual(-1, s.VaiFaltar(Suprimento.Combustivel), "com o deposito cheio, nada a avisar");
            }
            Assert.That(aviso, Is.GreaterThan(0L).And.LessThan(70L * Sim.Hz), "avisou com o jato ainda voando");
            Assert.That(faltou, Is.GreaterThan(aviso + 30 * Sim.Hz), "o aviso vem bem antes da falta");
            Assert.AreEqual(150L * Sim.Hz, depois);
            Assert.IsFalse(s.Esperando(1, Suprimento.Combustivel), "carregou na chegada");
        }

        /// <summary>No ponto do pedido sai a entrega que enche o tanque, com o custo fixo na conta de hoje; ela chega no prazo exato, e o fechamento cobra.</summary>
        [Test]
        public void Reposicao_NoPonto_EncheOTanque_NoPrazo_EOCustoVaiParaAConta()
        {
            var s = new Sim(Calmo(), 3) { Cash = 10000 };
            s.Estoque[Comb] = 4;
            var b = new Bot(BotProfile.Bom);
            long pediu = -1, chegou = -1;
            for (int i = 0; i < 179 * Sim.Hz; i++)
            {
                int antes = s.Estoque[Comb];
                b.Step(s);
                foreach (Evento e in s.Events)
                    if (e.Tipo == Ev.SuprimentoPedido)
                    {
                        Assert.AreEqual((-1L, Comb, 5, 6), (pediu, e.A, e.B, e.C), "um pedido so: o regional levou 1, ficam 3 (o ponto) e pede 5 para os 8, por 6");
                        pediu = s.Steps - 1;
                    }
                    else if (e.Tipo == Ev.SuprimentoChegou)
                    {
                        chegou = s.Steps - 1;
                        Assert.AreEqual((Comb, 8 - antes, 8), (e.A, e.B, s.Estoque[Comb]), "a entrega enche o tanque (o jato levou 2 antes dela)");
                    }
            }
            Assert.AreEqual(pediu + s.PrazoPassos(Suprimento.Combustivel), chegou, "chega no prazo");
            Assert.AreEqual(6L, s.Hoje.Despesas[(int)Despesa.Suprimentos]);
            Assert.AreEqual(6L, s.Folha(Despesa.Suprimentos), "a conta de hoje mostra a entrega");
            long folha = s.Folha();
            DespesasTests.ViraODia(s);
            DiaConta d = s.Historico.Single();
            Assert.AreEqual((6L, folha, folha), (d.Despesas[(int)Despesa.Suprimentos], d.Despesa, d.Pago), "o fechamento cobra junto");
            Assert.AreEqual(0L, s.Hoje.Despesas[(int)Despesa.Suprimentos], "o dia novo comeca sem entrega");
        }

        /// <summary>Faltou caixa no fechamento: contratar e melhorar pausam, mas a entrega sai e chega. A conta so cresce o custo dela.</summary>
        [Test]
        public void ContaAtrasada_NaoSeguraAEntrega_SoOTanqueMaior()
        {
            var s = new Sim(Calmo(), 1) { Cash = 0 };
            DespesasTests.ViraODia(s);
            Assert.IsTrue(s.Pausado, "faltou caixa");
            s.Estoque[Comb] = 0;
            var ev = RegrasTests.Coleta(s, Sim.Step);
            Assert.IsTrue(ev.Any(e => e.Tipo == Ev.SuprimentoPedido && e.A == Comb), "pede mesmo pausado");
            ev = RegrasTests.Coleta(s, 45f);
            Assert.AreEqual((true, true), (ev.Any(e => e.Tipo == Ev.SuprimentoChegou && e.A == Comb), s.Pausado), "chegou com o caixa ainda zerado");
            Assert.IsFalse(s.BuyTanque(Suprimento.Combustivel), "a melhoria pausa como todas");
        }

        [Test]
        public void TanqueMaior_DoisNiveis_AProximaEntregaEncheAteONovo()
        {
            var s = new Sim(Calmo(), 1) { Cash = 1000 };
            Assert.AreEqual(40L, s.CustoTanque(Suprimento.Combustivel));
            Assert.IsTrue(s.BuyTanque(Suprimento.Combustivel));
            Assert.AreEqual((960L, 12, 80L), (s.Cash, s.Tanque(Suprimento.Combustivel), s.CustoTanque(Suprimento.Combustivel)));
            Assert.IsTrue(s.BuyTanque(Suprimento.Combustivel));
            Assert.AreEqual((16, -1L, 8), (s.Tanque(Suprimento.Combustivel), s.CustoTanque(Suprimento.Combustivel), s.Tanque(Suprimento.Refeicoes)), "so o combustivel cresceu");
            Assert.IsFalse(s.BuyTanque(Suprimento.Combustivel), "no maximo");
            Assert.AreEqual(-1L, s.CustoTanque((Suprimento)7), "suprimento que nao existe nao tem preco");
            s.Estoque[Comb] = 3;
            var ev = RegrasTests.Coleta(s, Sim.Step);
            Assert.IsTrue(ev.Any(e => e.Tipo == Ev.SuprimentoMelhorou && e.A == Comb && e.B == (int)Melhoria.Capacidade && e.C == 2));
            Assert.AreEqual(13, s.Pedido[Comb], "enche ate os 16");
            ev = RegrasTests.Coleta(s, 45f);   // o regional so tira combustivel depois dos 70 s
            Assert.AreEqual((13, 16), (ev.Single(e => e.Tipo == Ev.SuprimentoChegou && e.A == Comb).B, s.Estoque[Comb]), "a entrega enche o tanque novo");
        }

        /// <summary>Do hangar o aviao volta limpo e abastecido: nao tira nada do deposito.</summary>
        [Test]
        public void Hangar_VoltaAbastecido_SemTirarDoDeposito()
        {
            var s = new Sim(Calmo(), 1) { Cash = 1000 };
            RegrasTests.Run(s, 25f);
            Assert.IsTrue(s.Hangar(0));
            Assert.AreEqual(new[] { 0, 0 }, s.Avioes[0].Carga, "o hangar leva a carga devida");
            RegrasTests.Run(s, 181f);
            Aviao a = s.Avioes[0];
            Assert.IsTrue(a.NoPatio && a.PatioPronto, "voltou pronto");
            Assert.AreEqual(new[] { 0, 0 }, a.Carga);
            Assert.AreEqual((8 - s.Gasto(1, Suprimento.Combustivel), 8 - s.Gasto(1, Suprimento.Refeicoes)), (s.Estoque[Comb], s.Estoque[Ref]), "so o jato tirou do deposito");
        }

        [Test]
        public void RegraDesligada_DepositoParado_NadaNaConta_NadaEspera()
        {
            var s = new Sim(Calmo(150f, false), 3);
            s.Estoque[Comb] = 0;
            var l = Joga(s, new Bot(BotProfile.Bom), 3 * 180f);
            Assert.IsFalse(l.Any(x => x.E.Tipo >= Ev.SuprimentoPedido && x.E.Tipo <= Ev.SuprimentoMelhorou), "nenhum evento de suprimento");
            Assert.AreEqual((0, 8, 0L, -1), (s.Estoque[Comb], s.Estoque[Ref], s.Hoje.Despesas[(int)Despesa.Suprimentos], s.VaiFaltar(Suprimento.Combustivel)));
            Assert.IsTrue(s.Avioes.All(a => a.Carga.All(c => c == 0)));
        }

        /// <summary>Gravar no meio da falta (caminhao esperando, entrega a caminho) e reabrir da o mesmo jogo, passo a passo.</summary>
        [Test]
        public void Save_NoMeioDaFalta_IdaEVolta_EOMesmoJogo()
        {
            Tuning t = Calmo(150f);
            var s = new Sim(t, 3);
            s.Estoque[Comb] = s.Estoque[Ref] = 0;
            RegrasTests.Run(s, 100f);
            Assert.IsTrue(s.Esperando(0, Suprimento.Refeicoes), "o catering espera no regional");
            string texto = s.Save("t", 1);
            StringAssert.Contains("\ncarga.0=", texto);
            Sim r = Sim.Load(texto, t, 3);
            Assert.AreEqual(texto, r.Save("t", 1), "ida e volta");
            RegrasTests.Run(s, 120f);
            RegrasTests.Run(r, 120f);
            Assert.AreEqual(s.Save("t", 2), r.Save("t", 2), "reabrir da o mesmo jogo");
        }

        [Test]
        public void Lixo_ViraPadraoLimitado()
        {
            Tuning t = Calmo();
            var s = new Sim(t, 3);
            RegrasTests.Run(s, 30f);   // o regional pousou e o desembarque corre: limpeza e combustivel por fazer
            string payload = SaveEnvelope.Unwrap(s.Save("t", 1)).Payload;
            payload = string.Join("\n", payload.Split('\n').Where(x => !x.StartsWith("sup.") && !x.StartsWith("carga.")))
                + "sup.0=-5,99,-7,99\nsup.1=x\ncarga.0=9,9\ncarga.1=2,2\n";
            Sim r = Sim.Load(SaveEnvelope.Wrap(payload, "t", 1), t, 3);
            Assert.IsFalse(r.SaveRefused);
            Assert.AreEqual((0, 16, r.Steps, 2, 16), (r.Estoque[Comb], r.Pedido[Comb], r.Entrega[Comb], r.TanqueNivel[Comb], r.Tanque(Suprimento.Combustivel)));
            Assert.AreEqual((8, 0, 0L), (r.Estoque[Ref], r.Pedido[Ref], r.Entrega[Ref]), "linha torta: o padrao");
            Assert.AreEqual(new[] { Sim.CargaPede, Sim.CargaPede }, r.Avioes[0].Carga, "servico por fazer: a carga ainda nao espera");
            Assert.AreEqual(new[] { 0, 0 }, r.Avioes[1].Carga, "o jato esta voando: nao deve carga");
        }

        /// <summary>Save real do schema 14 (commit a2ad131, a main com a esteira #62 e a limpeza #67): semente 7, bot Bom, 2 790 s.</summary>
        const string V14 = "sha256=a145ac43f64c185656e42f99119f2f34266ba93d1e4238a2ec7ec762afa92934\nschema=14\ncontent=0.14.0\nat=1800000000\nt=83700\ncash=5836\nseed=7\nonda=15" +
            "\nemb=1020\nperd=28\nalbum=1\nbst=0\nema=9.39020983056686\nsaved=1800000000\nclaim=0\nclaimcash=0\ndia=0\nest.0=5,2\nfila.0=15501;15502;15503;15504;15" +
            "505;15506;15507;15508;15509;15510;15511;15512;15513;15514;15515;15516;15517;15518;15519;15520;15521;15522;15523;15524;15525;15526;15527;15528;15529;15" +
            "530;15531;15532;15533;15534\nat.0=15044:2;15045:32;15500:62\nest.1=5,2\nat.1=15042:40;15043:70\nest.2=0,0\nest.3=5,2\nfila.3=15011;15016;15015;15017;1" +
            "5014;15020;15019;15018;15022;15023;15021;15024;15025;15026;15027;15028;15029;15031;15030;15033;15032;15035;15036;15034;15037;15038;15039;15040;15041\n" +
            "est.4=5,2\nest.5=5,2\nest.6=5,2\nest.7=0,0\nfunc.0=1,0,0,0,1\nfunc.1=1,0,0,1,1\nfunc.2=4,0,0,3,1\nfunc.3=5,1,0,2,1\nfunc.4=6,0,0,4,1\nfunc.5=7,1,0,5,1" +
            "\nvoo.0=46,13,182,1,0\nvoo.1=35,0,0,0,0\naviao.0=50,82200,1,30,-1,0,0,1,0,0\nsrv.0=0:0,0:0,0:0,0:0\naviao.1=100,83700,0,-1,-1,0,0,1,0,0\nsrv.1=0:0,0:0" +
            ",0:0,0:0\ntrip.0=0,1,3872,0\ntrip.1=0,1,5400,1\ntrip.2=0,3,1499,-1\ntrip.3=1,1,3842,0\ntrip.4=1,1,5400,1\ntrip.5=1,3,1499,-1\npatio=3\npatioatraso=8\n" +
            "aero=1,3,-1\nrotas=127\nprog=6,6\nvr.0=6,46,0,0,0,0,1800,0\nvr.1=6,46,0,0,0,0,3300,0\nav.0=6,15\nav.1=6,15\nstt.0=1,31,5.666665818542242\nstt.1=0,-1,2" +
            "7.900001425296068\nstt.3=1,30,8.099999587982893\nreparo=0\nincw=14\ncamp.0=6,10,0,0,3,0,1,0,1,3,0\ncamp.1=1,0,1,40,40,3,0,1,0,2,0\ncamp.2=0,0,0,0,0,0," +
            "0,0,0,0,0\nmiss=1,0\nmd.0=14,4,0,1\nmd.1=12,30,0,1\nmd.2=6,300,0,1\nms.0=4,30,1,0\nms.1=2,10,0,0\nms.2=3,20,0,0\nms.3=1,0,5,0\nms.4=0,1000,0,0\ncq=348" +
            "49,0,0,30,30,27,4,24,70,0,232,252,25,0,2,1,0,0,0,0,6\ngems=0\npasse=0,0,350,3\natrasada=0\nhoje=1,0,7818,1015,0,1904,2236,1200,0,0,0,0,0,0\ndc.0=0,600" +
            ",4570,445,0,528,1000,790,0,0,210,108,54,228\nhist=3600,4,-1,0,0,10,0,4,0,0,0,6,0,0,0,0;25200,3,-1,0,8,26,3,0,0,0,0,2,3,7,0,0;41460,1,4,0,15,40,0,7,1,0" +
            ",0,0,0,0,0,0;43200,5,-1,0,-1,0,0,0,0,1,117,12,0,0,0,0;46800,3,-1,0,16,58,16,0,0,0,0,1,3,18,2,0;62400,0,1,0,23,32,0,0,3,1,0,0,0,0,0,0;69900,3,-1,0,25,5" +
            "4,7,0,0,0,0,1,3,2,3,0;73800,3,-1,0,26,43,2,0,0,0,0,2,3,16,0,0;79260,1,5,0,29,40,0,0,1,0,0,0,0,0,0,0\nsuj=176,120,0,114,176,12,80\nbh=0\nbhneg=0\nzels=" +
            "1\nzel.0=5,1,0,0,330,0\n";

        static string SemEma(string save) =>
            string.Join("\n", save.Split('\n').Where(l => !l.StartsWith("ema=") && !l.StartsWith("sha256=") && !l.StartsWith("stt.") && !l.StartsWith("camp.") && !l.StartsWith("miss=") && !l.StartsWith("md.") && !l.StartsWith("ms.") && !l.StartsWith("cq=") && !l.StartsWith("passe=")
                && !l.StartsWith("atrasada=") && !l.StartsWith("hoje=") && !l.StartsWith("dc.") && !l.StartsWith("hist=") && !l.StartsWith("hp=")));

        /// <summary>O schema 14 (sem suprimentos) abre com o deposito cheio, nada a caminho, nenhum aviao devendo carga e a 5a categoria zerada; e migrar =
        /// jogar ate ali com o codigo novo sem a regra (o jogo que o 14 gravava). A cadeia 12 -> 13 -> 14 esta no SaveTests.</summary>
        [Test]
        public void MigracaoDoSchema14_SaveReal_DepositoCheio_EOMesmoJogo()
        {
            const long U = 1800000000;
            Sim m = Sim.Load(V14, null, 7);
            Assert.IsFalse(m.SaveRefused, "schema 14 migra, nao e' recusado");
            Assert.AreEqual((8, 8, 0, 0), (m.Estoque[Comb], m.Estoque[Ref], m.Pedido[Comb], m.Pedido[Ref]));
            Assert.IsTrue(m.Avioes.All(a => a.Carga.All(c => c == 0)));
            Assert.AreEqual((5, 0L, 0L), (m.Historico[0].Despesas.Length, m.Historico[0].Despesas[4], m.Hoje.Despesas[4]));
            string texto = m.Save("0.15.0", U);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", texto);
            StringAssert.Contains("\nsup.0=8,0,0,0\nsup.1=8,0,0,0\n", texto);
            Assert.AreEqual(texto, Sim.Load(texto, null, 7).Save("0.15.0", U), "ida e volta estavel no schema novo");
            var sem = new Tuning { RegraSuprimentos = false, RegraHistorias = false };
            var n = new Sim(sem, 7);
            var b = new Bot(BotProfile.Bom);
            for (int i = 0; i < 2790 * Sim.Hz; i++) b.Step(n);
            Assert.AreEqual(SemEma(n.Save("0.14.0", U)), SemEma(Sim.Load(V14, sem, 7).Save("0.14.0", U)), "migrar = jogar ate ali com o codigo novo");
        }

        /// <summary>Invariante: com a tabela padrao, os bots nunca ficam sem suprimento (a reposicao automatica chega antes), o deposito fica entre 0 e o
        /// tanque com a entrega a caminho cabendo nele, e o aviso nunca acende.</summary>
        [Test]
        public void Invariante_TabelaPadrao_NuncaFalta_DepositoNoLimite()
        {
            foreach (BotProfile p in new[] { BotProfile.Bom, BotProfile.Lento, BotProfile.Guiado })
                for (int seed = 1; seed <= 6; seed++)
                {
                    var s = new Sim(null, seed);
                    var b = new Bot(p);
                    int pedidos = 0;
                    for (long i = 0; i < 16L * s.OndaPassos; i++)
                    {
                        b.Step(s);
                        for (int k = 0; k < Sim.Suprimentos; k++)
                        {
                            var su = (Suprimento)k;
                            Assert.That(s.Estoque[k], Is.InRange(0, s.Tanque(su)));
                            Assert.LessOrEqual(s.Estoque[k] + s.Pedido[k], s.Tanque(su), "a entrega cabe no tanque");
                            Assert.AreEqual(-1, s.VaiFaltar(su), p.Name + " semente " + seed + ": aviso no passo " + s.Steps);
                        }
                        pedidos += s.Events.Count(e => e.Tipo == Ev.SuprimentoPedido);
                    }
                    Assert.AreEqual(0, s.FaltasSuprimento, p.Name + " semente " + seed);
                    Assert.Greater(pedidos, 4, "a reposicao trabalha");
                }
        }
    }
}
