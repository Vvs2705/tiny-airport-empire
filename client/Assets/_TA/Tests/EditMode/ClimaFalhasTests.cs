using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Regras da Fase C4 (CONTRATO_FASE_C4 §2 a §4): relogio do jogo, demanda, os 5 climas com previsao e as saidas, e as 3 falhas de
    /// aviao. Cada evento tem o teste de regra e entra no teste de determinismo. As invariantes ficam no ExploitTests.</summary>
    public class ClimaFalhasTests
    {
        const int Aurora = 2;

        /// <summary>Aeroporto `aero`, tudo aberto, com o clima `tipo` em toda onda permitida.</summary>
        internal static Tuning ComClima(TipoIncidente tipo, int aero = Aurora)
        {
            Tuning t = BalanceTests.AbertoC4(aero);
            t.RegraFalhas = false;
            int col = Array.FindIndex(Tabelas.Climas, c => c.Tipo == tipo);
            t.Aeroportos = t.Aeroportos.Select(x =>
            {
                Aeroporto c = x.Copia();
                c.Clima = Enumerable.Range(0, 4).Select(_ => Enumerable.Range(0, Tabelas.Climas.Length).Select(i => i == col ? 1f : 0f).ToArray()).ToArray();
                return c;
            }).ToArray();
            return t;
        }

        /// <summary>Sem clima nem incidente, com a falha `f` em todo voo no aviso.</summary>
        internal static Tuning ComFalha(TipoFalha f)
        {
            Tuning t = BalanceTests.AbertoC4(Aurora);
            t.RegraClima = t.RegraIncidentes = false;
            t.FalhaAviaoChance = 1f;
            t.FalhaAviaoPesos = new int[3];
            t.FalhaAviaoPesos[(int)f - 1] = 1;
            return t;
        }

        static Sim Pronta(Tuning t) => EstacoesPerfisTests.Pronta(t);

        /// <summary>Anda ate o evento, sem tocar em nada (null = nao veio em `seg` s).</summary>
        internal static Evento? Espera(Sim s, Ev tipo, float seg = 200f)
        {
            for (int i = 0; i < seg * Sim.Hz; i++)
            {
                s.Tick(Sim.Step);
                foreach (Evento e in s.Events) if (e.Tipo == tipo) return e;
            }
            return null;
        }

        static long T0(Sim s) => (long)s.Onda * s.OndaPassos;

        // ------------------------------------------------------------------ relogio e demanda

        /// <summary>O diario guarda estes enums pelo indice: so crescem no fim (como o EnumsTests faz com os das fases anteriores).</summary>
        [Test]
        public void EnumsDaFaseC4_SoCrescemNoFim()
        {
            string[] ev = Enum.GetNames(typeof(Ev));
            CollectionAssert.AreEqual(new[] { "Previsto", "Atrasado", "Degelou", "Pane", "PaneReparo", "PaneReparada", "Rejeicao", "RetornoPedido", "PousoSeguro", "Remarcados" },
                ev.Skip(Array.IndexOf(ev, "Priorizado") + 1).Take(10).ToArray());
            CollectionAssert.AreEqual(new[] { "Verao", "Outono", "Inverno", "Primavera" }, Enum.GetNames(typeof(Epoca)));
            CollectionAssert.AreEqual(new[] { "Nenhuma", "Pane", "Rejeicao", "Retorno" }, Enum.GetNames(typeof(TipoFalha)));
        }

        [Test]
        public void Relogio_1Onda3Horas_Dia8Ondas_Ano32Ondas()
        {
            var noite = new Sim();
            Assert.AreEqual((6f, 0, false, Epoca.Verao), (noite.Hora, noite.DiaJogo, noite.Noite, noite.Epoca), "a onda 0 comeca as 06:00, no verao");
            foreach ((int onda, float hora, int dia, bool escuro, Epoca e) in new[]
            {
                (1, 9f, 0, false, Epoca.Verao), (4, 18f, 0, true, Epoca.Verao), (6, 0f, 0, true, Epoca.Verao), (7, 3f, 0, true, Epoca.Verao),
                (8, 6f, 1, false, Epoca.Outono), (16, 6f, 2, false, Epoca.Inverno), (24, 6f, 3, false, Epoca.Primavera), (32, 6f, 4, false, Epoca.Verao),
            })
            {
                var s = new Sim();
                s.IrParaOnda(onda);
                s.Tick(Sim.Step);
                Assert.AreEqual(hora, s.Hora, 0.01f, "onda " + onda);
                Assert.AreEqual((dia, escuro, e), (s.DiaJogo, s.Noite, s.Epoca), "onda " + onda);
            }
        }

        [Test]
        public void Demanda_PicosDoDia_EstacaoDoAno_SoDepoisDoMarco1()
        {
            Tuning t = BalanceTests.AbertoC4(1);
            t.ClimaMarco = 1; t.RegraClima = t.RegraIncidentes = false;
            var s = new Sim(t, 1);
            RegrasTests.Run(s, 1f);
            int salvador = (int)Math.Round(t.Pax(0) * (double)t.Rotas[4].Pax);
            Assert.AreEqual(salvador, s.Voos[0].Total, "antes do marco 1 a demanda nao muda");

            foreach ((int onda, double fator) in new[] { (0, 1.25 * 1.3), (2, 0.9 * 1.3), (6, 0.75 * 1.3), (8, 1.25 * 0.9), (16, 1.25 * 0.8) })
            {
                var o = new Sim(t, 1) { TotalEmbarcados = 150 };
                o.IrParaOnda(onda);
                o.Tick(Sim.Step);
                Assert.AreEqual((int)Math.Round(t.Pax(onda) * (double)t.Rotas[4].Pax * (double)(float)(fator / 1.0)), o.Voos[0].Total, 1, "onda " + onda);
            }
            Tuning desligada = BalanceTests.AbertoC4(1);
            desligada.RegraEpocas = desligada.RegraPicos = false;
            var d = new Sim(desligada, 1);
            RegrasTests.Run(d, 1f);
            Assert.AreEqual(salvador, d.Voos[0].Total, "chave desligada: so visual");
        }

        [Test]
        public void Tabelas_MediaDoDia1_ClimaDeCadaAeroporto()
        {
            Assert.AreEqual(1.0, new Tuning().DemandaHora.Average(x => (double)x), 1e-6);
            Aeroporto[] a = Tabelas.Aeroportos;
            Assert.AreEqual(4, a.Count(x => x.Clima.Length == 4) + 1, "4 estacoes do ano nos 3 aeroportos");
            int C(TipoIncidente t) => Array.FindIndex(Tabelas.Climas, c => c.Tipo == t);
            Assert.Greater(a[0].Clima[(int)Epoca.Verao][C(TipoIncidente.Temporal)], 0f, "Vale Verde: temporal de verao");
            Assert.Greater(a[1].Clima.Max(e => e[C(TipoIncidente.Vento)]), 0f, "Costa Azul: vento forte");
            Assert.Greater(a[2].Clima[(int)Epoca.Inverno][C(TipoIncidente.Neve)], 0f, "Aurora: neve");
            Assert.AreEqual(0f, a[0].Clima.Max(e => e[C(TipoIncidente.Neve)]), "neve so em Aurora");
            Assert.IsTrue(a.All(x => x.Clima.All(e => e.Sum() < 1f)), "sobra onda de tempo bom");
        }

        // ------------------------------------------------------------------ clima

        [Test]
        public void Clima_SoDepoisDoMarco1_ComoOComeco()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var s = new Sim(null, seed);
                var b = new Bot(BotProfile.Parado);
                for (int i = 0; i < 6 * 180 * Sim.Hz && s.Marco < 1; i++)
                {
                    b.Step(s);
                    Assert.IsFalse(s.Events.Any(e => e.Tipo == Ev.Previsto || e.Tipo == Ev.IncidenteAviso), "semente " + seed);
                    if (s.Marco < 1) Assert.IsNull(s.Previsao);
                }
            }
        }

        /// <summary>Na virada, a previsao da onda anterior tem de ser o clima que veio: acerta em todas as sementes e nos 3 aeroportos.</summary>
        [Test]
        public void Previsao_AcertaSempre_EAvisaAntes()
        {
            int vistos = 0;
            for (int aero = 0; aero < 3; aero++)
                for (int seed = 1; seed <= 6; seed++)
                {
                    var s = new Sim(BalanceTests.AbertoC4(aero), seed);
                    BotProfile p = BotProfile.Bom;
                    p.Viaja = false;   // viagem marcada muda a previsao: a chegada nao tem perigo
                    var b = new Bot(p);
                    TipoIncidente? previsto = s.Previsao, veio = null;
                    for (int i = 0; i < 64 * s.OndaPassos; i++)
                    {
                        b.Step(s);
                        foreach (Evento e in s.Events)
                            if (e.Tipo == Ev.IncidenteAviso && s.ClimaDe((TipoIncidente)e.A) != null) veio = (TipoIncidente)e.A;
                        if (s.Steps % s.OndaPassos != 0) continue;
                        Assert.AreEqual(previsto, veio, "aeroporto " + aero + " semente " + seed + " onda " + s.Onda);
                        if (veio != null) vistos++;
                        previsto = s.Previsao;
                        veio = null;
                    }
                }
            Assert.Greater(vistos, 200);
        }

        [Test]
        public void FrenteFria_VemDaTabelaDeClima_ComOsNumerosDoTA4()
        {
            Sim s = Pronta(ComClima(TipoIncidente.FrenteFria));
            Incidente i = RotasIncidentesTests.AteOAviso(s);
            Assert.AreEqual((TipoIncidente.FrenteFria, 30 * Sim.Hz, 60 * Sim.Hz), (i.Tipo, i.Inicio - s.Steps + 0, i.Fim - i.Inicio), "aviso de 30 s e pista fechada por 60 s");
            RotasIncidentesTests.Ate(s, i.Inicio + 1);
            Assert.AreEqual((true, false), (s.PistaFechada, s.PatioParado));
            Tuning semClima = RotasIncidentesTests.Com(TipoIncidente.FrenteFria);
            Assert.IsFalse(semClima.RegraClima, "com a chave desligada ela volta aos pesos do aeroporto, como na C3");
        }

        [Test]
        public void Temporal_RaioPerto_PatioEPistaParam_PorTempoDefinido()
        {
            Sim s = Pronta(ComClima(TipoIncidente.Temporal));
            Incidente i = RotasIncidentesTests.AteOAviso(s);
            Assert.AreEqual(s.ClimaDe(TipoIncidente.Temporal).Seg * Sim.Hz, i.Fim - i.Inicio, "duracao conhecida no aviso");
            RotasIncidentesTests.Ate(s, i.Inicio + 1);
            Assert.AreEqual((true, true), (s.PistaFechada, s.PatioParado));
            int[] falta = s.Avioes.SelectMany(a => a.Falta).ToArray();
            int bordo = s.Voos.Sum(v => v.Embarcados);
            var ev = new List<Evento>();
            while (s.Steps < i.Fim - 1) { s.Tick(Sim.Step); ev.AddRange(s.Events); }
            CollectionAssert.AreEqual(falta, s.Avioes.SelectMany(a => a.Falta).ToArray(), "os servicos ficam parados");
            Assert.AreEqual(bordo, s.Voos.Sum(v => v.Embarcados), "ninguem embarca com raio perto");
            Assert.IsFalse(ev.Any(e => e.Tipo == Ev.Decolou || e.Tipo == Ev.Pousou), "ninguem pousa nem decola");
            RotasIncidentesTests.Ate(s, i.Fim + 1);
            Assert.AreEqual((false, false), (s.PistaFechada, s.PatioParado));
        }

        [Test]
        public void Neblina_CadaPousoEDecolagemEspera_SaiSemBonus()
        {
            Sim s = Pronta(ComClima(TipoIncidente.Neblina));
            Incidente i = RotasIncidentesTests.AteOAviso(s);
            RotasIncidentesTests.Ate(s, i.Inicio + 1);
            Assert.AreEqual((int)(s.ClimaDe(TipoIncidente.Neblina).Espera * Sim.Hz), s.EsperaPista);
            Evento? d = null;
            while (s.Steps < i.Fim && d == null) d = Espera(s, Ev.Decolou, 1f);
            Assert.IsNotNull(d, "alguem decola na janela");
            Assert.GreaterOrEqual(d.Value.C, (int)s.ClimaDe(TipoIncidente.Neblina).Espera - 1, "a decolagem esperou");
            RotasIncidentesTests.Ate(s, i.Fim + 1);
            Assert.AreEqual(0, s.EsperaPista);
        }

        [Test]
        public void Vento_RajadaFechaAPista_SemPararOPatio()
        {
            Sim s = Pronta(ComClima(TipoIncidente.Vento, 1));
            Incidente i = RotasIncidentesTests.AteOAviso(s);
            RotasIncidentesTests.Ate(s, i.Inicio + 1);
            Assert.AreEqual((true, false, 0), (s.PistaFechada, s.PatioParado, s.EsperaPista));
            Assert.AreEqual(45 * Sim.Hz, i.Fim - i.Inicio);
        }

        [Test]
        public void Adiantar_NoTemporal_ContinuaEmbarcandoPelaPonte_NaNeblinaPegaAVez()
        {
            Sim s = Pronta(ComClima(TipoIncidente.Temporal));
            Incidente i;
            do { RotasIncidentesTests.Ate(s, s.Steps + s.OndaPassos); i = RotasIncidentesTests.AteOAviso(s); } while (i.Vaga != 0);
            s.Hire();
            Assert.IsTrue(s.Adianta());
            RotasIncidentesTests.Ate(s, i.Inicio + 1);
            int bordo = s.Voos[0].Embarcados;
            RotasIncidentesTests.Ate(s, i.Inicio + 12 * Sim.Hz);
            Assert.Greater(s.Voos[0].Embarcados + s.St(Estacao.Portao).Atendendo.Count + s.St(Estacao.Portao).Fila.Count, bordo - 1, "o portao segue");

            Sim n = Pronta(ComClima(TipoIncidente.Neblina));
            Incidente j = RotasIncidentesTests.AteOAviso(n);
            Assert.IsTrue(n.Adianta());
            Voo v = n.Voos[j.Vaga];
            var ev = new List<Evento>();
            for (int k = 0; k < 170 * Sim.Hz && !ev.Any(e => e.Tipo == Ev.Decolou && e.B == v.Id); k++) { n.Tick(Sim.Step); ev.AddRange(n.Events); }
            Assert.LessOrEqual(ev.Single(e => e.Tipo == Ev.Decolou && e.B == v.Id).C, 20, "quem adiantou nao espera a neblina");
        }

        [Test]
        public void Neve_ExigeDegelo_QuemAdiantaJaDegelou()
        {
            Sim s = Pronta(ComClima(TipoIncidente.Neve));
            Incidente i = RotasIncidentesTests.AteOAviso(s);
            Voo v = s.Voos[i.Vaga];
            RotasIncidentesTests.Ate(s, T0(s) + v.FechaPasso + 2);
            Aviao a = s.Avioes[v.Indice];
            if (a.FechouEm >= 0) Assert.IsTrue(s.Degelando(a.Id), "o caminhao de degelo esta no aviao");
            Assert.IsNotNull(Espera(s, Ev.Degelou), "degelou e decolou");

            Sim o = Pronta(ComClima(TipoIncidente.Neve));
            Incidente j = RotasIncidentesTests.AteOAviso(o);
            Assert.IsTrue(o.Adianta());
            Voo w = o.Voos[j.Vaga];
            var ev = new List<Evento>();
            for (int k = 0; k < 170 * Sim.Hz && !ev.Any(e => e.Tipo == Ev.VooPartiu && e.A == w.Id); k++) { o.Tick(Sim.Step); ev.AddRange(o.Events); }
            Assert.IsFalse(o.Degelando(w.Indice), "quem adiantou nao fica esperando o caminhao");
        }

        [Test]
        public void Atrasa_ProPortaoDepoisDeReabrir_SemBonus()
        {
            Tuning t = ComClima(TipoIncidente.FrenteFria);
            Sim s = Pronta(t);
            Incidente i;
            do { RotasIncidentesTests.Ate(s, s.Steps + s.OndaPassos); i = RotasIncidentesTests.AteOAviso(s); } while (i.Vaga != 0);
            Voo v = s.Voos[0];
            int antes = v.FechaPasso;
            Assert.IsTrue(s.Atrasa());
            Assert.AreEqual((i.Fim - T0(s) + (int)(t.AtrasoFolga * Sim.Hz), true, true), ((long)v.FechaPasso, v.Atrasado, i.Resolvido));
            Assert.Greater(v.FechaPasso, antes);
            Assert.IsFalse(s.Atrasa(), "uma vez so");
            Assert.IsFalse(s.Adianta(), "uma saida por clima");
            var ev = RegrasTests.Coleta(s, 120f);
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.VooPartiu && e.A == v.Id), "fecha no novo horario");
            Assert.AreEqual(0, ev.Count(e => e.Tipo == Ev.Bonus && e.A == v.Id), "atrasar custa o bonus");
        }

        [Test]
        public void Atrasa_NaoPassaDaViradaDaOnda_NemAntesDoAviso()
        {
            var s = new Sim(ComClima(TipoIncidente.FrenteFria), 1);
            RegrasTests.Run(s, 1f);
            Assert.IsFalse(s.Atrasa(), "sem clima, nada a atrasar");
            Sim o = Pronta(ComClima(TipoIncidente.FrenteFria));
            Incidente i;
            do { RotasIncidentesTests.Ate(o, o.Steps + o.OndaPassos); i = RotasIncidentesTests.AteOAviso(o); } while (i.Vaga != 1);
            Assert.IsFalse(o.Atrasa(), "a frente fria do voo 2 passa da virada: nao ha como atrasar");
            Assert.IsFalse(o.Voos[1].Atrasado);
        }

        [Test]
        public void Adianta_ValeParaOs5Climas_ForaDoAvisoNao()
        {
            foreach (Clima c in Tabelas.Climas)
            {
                Sim s = Pronta(ComClima(c.Tipo));
                Incidente i = RotasIncidentesTests.AteOAviso(s);
                Assert.IsTrue(s.Adianta(), c.Tipo.ToString());
                Assert.AreEqual((true, true), (i.Resolvido, s.Voos[i.Vaga].Adiantado), c.Tipo.ToString());
                Assert.IsFalse(s.Adianta());
            }
            Sim o = Pronta(ComClima(TipoIncidente.Temporal));
            Incidente j = RotasIncidentesTests.AteOAviso(o);
            RotasIncidentesTests.Ate(o, j.Inicio);
            Assert.IsFalse(o.Adianta(), "depois do aviso ja nao da");
        }

        // ------------------------------------------------------------------ falhas de aviao

        /// <summary>O aviao 0 ja no voo da onda, com desgaste `d`, antes de o embarque abrir.</summary>
        static Sim NoPatio(TipoFalha f, int d)
        {
            Sim s = Pronta(ComFalha(f));
            s.Cash = 1000;
            RegrasTests.Run(s, 21f);
            s.Avioes[0].Desgaste = d;
            return s;
        }

        [Test]
        public void Falha_NuncaAbaixoDoAviso()
        {
            foreach (TipoFalha f in new[] { TipoFalha.Pane, TipoFalha.Rejeicao, TipoFalha.Retorno })
            {
                Sim s = NoPatio(f, 79);
                var ev = RegrasTests.Coleta(s, 170f);
                Assert.IsFalse(ev.Any(e => e.Tipo == Ev.Pane || e.Tipo == Ev.Rejeicao || e.Tipo == Ev.RetornoPedido), f.ToString());
                Assert.IsTrue(ev.Any(e => e.Tipo == Ev.Decolou && e.A == 0), f.ToString());
            }
        }

        [Test]
        public void Checagem_TiraDaFaixaDeRisco_EEvitaAFalha()
        {
            Sim s = NoPatio(TipoFalha.Pane, 85);
            Assert.IsTrue(s.EmRisco(0));
            Assert.IsTrue(s.Checagem(0));
            RegrasTests.Run(s, 9f);
            Assert.IsFalse(s.EmRisco(0), "85 - 10 = 75");
            var ev = RegrasTests.Coleta(s, 100f);
            Assert.IsFalse(ev.Any(e => e.Tipo == Ev.Pane), "voar acima do aviso e' escolha do jogador");
        }

        [Test]
        public void Pane_SeguraOEmbarque_ReparoPagoEmSegundos()
        {
            Sim s = NoPatio(TipoFalha.Pane, 85);
            Assert.IsNotNull(Espera(s, Ev.Pane));
            Assert.AreEqual((true, false, false), (s.Avioes[0].Pane, s.Voos[0].Embarque, s.Pronto(0)));
            RegrasTests.Run(s, 0.1f);
            Assert.AreEqual((true, true), (s.Avioes[0].Check > 0, s.Avioes[0].Inspecao), "a inspecao obrigatoria comeca sozinha");
            Assert.IsFalse(s.Checagem(0), "pane nao se resolve com checagem");
            long cash = s.Cash;
            Assert.IsTrue(s.ReparaAviao(0));
            Assert.AreEqual((cash - s.T.PaneCusto, false), (s.Cash, s.Avioes[0].Inspecao));
            Assert.IsFalse(s.ReparaAviao(0), "nao paga 2 vezes");
            Assert.IsNotNull(Espera(s, Ev.PaneReparada, s.T.PaneSeg + 1f));
            Assert.AreEqual((false, true), (s.Avioes[0].Pane, s.Voos[0].Embarque), "o embarque abre logo depois");
        }

        [Test]
        public void Pane_SemPagar_InspecaoResolveDeGraca_MasOVooVaiEmbora()
        {
            Sim s = NoPatio(TipoFalha.Pane, 85);
            s.Cash = 0;
            Assert.IsNotNull(Espera(s, Ev.Pane));
            Voo v = s.Voos[0];
            var ev = RegrasTests.Coleta(s, 111f);
            Assert.AreEqual(0, ev.Count(e => e.Tipo == Ev.PaneReparo), "a inspecao nao custa Cash");
            Assert.IsFalse(s.Avioes[0].Pane, "sem Cash o jogo nao trava");
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.PaneReparada && e.A == 0));
            Assert.AreEqual(v.Total, ev.Single(e => e.Tipo == Ev.VooPerdido && e.A == v.Id).B, "perdeu so o proprio voo");
        }

        [Test]
        public void Pane_HangarTambemResolve()
        {
            Sim s = NoPatio(TipoFalha.Pane, 85);
            Assert.IsNotNull(Espera(s, Ev.Pane));
            Assert.IsTrue(s.Hangar(0));
            Assert.AreEqual((false, 0), (s.Avioes[0].Pane, s.Avioes[0].Check));
        }

        [Test]
        public void Reparo_MecanicoOcupadoComOutroAviao_NaoDa()
        {
            Sim s = NoPatio(TipoFalha.Pane, 85);
            Assert.IsNotNull(Espera(s, Ev.Pane));
            RegrasTests.Run(s, 0.1f);
            s.Avioes[1].Check = 100;
            Assert.IsFalse(s.ReparaAviao(0));
            Assert.IsFalse(s.ReparaAviao(1), "sem pane nao ha o que reparar");
            Assert.IsFalse(s.ReparaAviao(7));
        }

        [Test]
        public void Rejeicao_AbortaNaPista_BloqueiaPoucoTempo_ESaiDepois()
        {
            Sim s = NoPatio(TipoFalha.Rejeicao, 85);
            long cash = s.Cash;
            Assert.IsNotNull(Espera(s, Ev.Rejeicao));
            Assert.AreEqual((s.Steps - 1 + (int)(s.T.PistaBloqueioSeg * Sim.Hz), true), (s.PistaAte, s.Avioes[0].NoPatio));
            Assert.GreaterOrEqual(s.Cash, cash, "o voo ja pagou: nada sai do saldo");
            Assert.IsNull(Espera(s, Ev.Decolou, s.T.RejeicaoSeg - 2f), "o aviao volta e espera");
            var ev = RegrasTests.Coleta(s, 4f);
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.Decolou && e.A == 0));
            Assert.AreEqual(0, ev.Count(e => e.Tipo == Ev.Rejeicao), "uma falha por voo");
        }

        [Test]
        public void Rejeicao_SeguraOOutroAviaoEnquantoAPistaBloqueia()
        {
            Sim s = NoPatio(TipoFalha.Rejeicao, 85);
            Assert.IsNotNull(Espera(s, Ev.Rejeicao));
            Aviao b = s.Avioes[1];
            b.Chega = s.Steps; b.NoPatio = false;
            RegrasTests.Run(s, 5f);
            Assert.IsFalse(b.NoPatio, "pouso espera a pista liberar");
            RegrasTests.Run(s, s.T.PistaBloqueioSeg);
            Assert.IsTrue(b.NoPatio);
        }

        [Test]
        public void Retorno_DecolaPedeParaVoltarEPousaSeguro_ComPrioridade()
        {
            Sim s = NoPatio(TipoFalha.Retorno, 85);
            Assert.IsNotNull(Espera(s, Ev.Decolou));
            long subiu = s.Steps;
            Aviao a = s.Avioes[0];
            Assert.AreEqual(false, a.NoPatio);
            Assert.IsNotNull(Espera(s, Ev.RetornoPedido, s.T.RetornoPede + 1f));
            Assert.AreEqual(subiu + (int)(s.T.RetornoPede * Sim.Hz) - 1, s.Steps - 1);
            var ev = RegrasTests.Coleta(s, s.T.RetornoVolta + 1f);
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.PousoSeguro && e.A == 0), "todo retorno termina em pouso seguro");
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.Remarcados && e.A == 0), "passageiros remarcados");
            Assert.AreEqual((true, true), (a.NoPatio, a.Pane));
            Assert.Greater(s.PistaAte, s.Steps - 30, "a pista fica bloqueada por pouco tempo");
            Assert.Less(a.Chega - subiu, (long)(s.T.Trecho * Sim.Hz), "volta antes do trecho");
        }

        [Test]
        public void Retorno_PassaNaFrenteDaPistaFechada()
        {
            Sim s = NoPatio(TipoFalha.Retorno, 85);
            Assert.IsNotNull(Espera(s, Ev.Decolou));
            Aviao a = s.Avioes[0];
            s.PistaAte = s.Steps + 3600;
            Assert.IsNotNull(Espera(s, Ev.PousoSeguro, 60f), "emergencia nao espera a pista");
            Assert.IsTrue(a.NoPatio);
        }

        // ------------------------------------------------------------------ determinismo e regressao de cada evento

        static readonly Ev[] EventosC4 = { Ev.Previsto, Ev.Atrasado, Ev.Degelou, Ev.Pane, Ev.PaneReparo, Ev.PaneReparada, Ev.Rejeicao, Ev.RetornoPedido, Ev.PousoSeguro, Ev.Remarcados };

        static List<string> Eventos(int seed, int ondas, BotProfile p, Dictionary<Ev, int> conta = null)
        {
            Tuning t = BalanceTests.AbertoC4(Aurora);
            var s = new Sim(t, seed);
            var b = new Bot(p);
            var log = new List<string>();
            for (int i = 0; i < ondas * s.OndaPassos; i++)
            {
                if (i % s.OndaPassos == 0) foreach (Aviao a in s.Avioes) a.Desgaste = Math.Max(a.Desgaste, 85);   // sempre na faixa: todo voo pode falhar
                if (seed % 2 == 0) s.Atrasa();   // o bot adianta: nas sementes pares o portao atrasa antes
                b.Step(s);
                foreach (Evento e in s.Events)
                {
                    log.Add(s.Steps + ":" + e);
                    if (conta != null && Array.IndexOf(EventosC4, e.Tipo) >= 0) { conta.TryGetValue(e.Tipo, out int n); conta[e.Tipo] = n + 1; }
                }
            }
            return log;
        }

        [Test]
        public void Determinismo_MesmaSemente_MesmosEventos_ComTodosOsEventosDaC4()
        {
            var conta = new Dictionary<Ev, int>();
            for (int seed = 1; seed <= 6; seed++)
            {
                List<string> a = Eventos(seed, 40, BotProfile.Bom, conta), b = Eventos(seed, 40, BotProfile.Bom);
                CollectionAssert.AreEqual(a, b, "semente " + seed);
            }
            foreach (Ev e in EventosC4) Assert.IsTrue(conta.ContainsKey(e), e + " nao apareceu em 240 ondas: o teste nao cobre esse evento");
        }

        [Test]
        public void Determinismo_SementeDiferente_DaOutroDia()
        {
            CollectionAssert.AreNotEqual(Eventos(1, 20, BotProfile.Bom), Eventos(2, 20, BotProfile.Bom));
        }

        [Test]
        public void Desligado_NaoGeraNenhumEventoDaC4()
        {
            var s = new Sim(BalanceTests.SemFaseC4(BalanceTests.AbertoC3(Aurora)), 3);
            var b = new Bot(BotProfile.Bom);
            for (int i = 0; i < 20 * s.OndaPassos; i++)
            {
                if (i % s.OndaPassos == 0) foreach (Aviao a in s.Avioes) a.Desgaste = Math.Max(a.Desgaste, 85);
                b.Step(s);
                Assert.IsFalse(s.Events.Any(e => Array.IndexOf(EventosC4, e.Tipo) >= 0));
            }
        }
    }
}
