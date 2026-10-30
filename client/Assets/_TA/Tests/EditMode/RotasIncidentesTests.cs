using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Regras da Fase C3 (CONTRATO_FASE_C3 §2 a §4): as tabelas, abrir e programar rotas, viajar, rota longa, conexao, turistica e os 4
    /// incidentes com as saidas. As invariantes ficam no ExploitTests.</summary>
    public class RotasIncidentesTests
    {
        const int Curitiba = 2, Salvador = 4, Brasilia = 7, Guarulhos = 8, Manaus = 10;

        /// <summary>Aeroporto `aero`, tudo aberto, com um incidente do `tipo` em toda onda permitida.</summary>
        internal static Tuning Com(TipoIncidente tipo, int aero = 2)
        {
            Tuning t = BalanceTests.AbertoC3(aero);
            t.IncidenteTeto = 1f;
            t.Aeroportos = t.Aeroportos.Select(x => { Aeroporto c = x.Copia(); c.Incidentes = 1f; c.Pesos = new int[4]; c.Pesos[(int)tipo] = 1; return c; }).ToArray();
            return t;
        }

        internal static Incidente AteOAviso(Sim s)
        {
            for (int i = 0; i < 1800 * Sim.Hz && s.Inc == null; i++) s.Tick(Sim.Step);
            Assert.IsNotNull(s.Inc, "veio incidente");
            return s.Inc;
        }

        internal static void Ate(Sim s, long passo)
        {
            while (s.Steps < passo) s.Tick(Sim.Step);
        }

        static Tuning Fixa(int rota, int aero)
        {
            Tuning t = BalanceTests.AbertoC3(aero);
            t.RegraIncidentes = false;
            t.RotaFixa = rota;
            return t;
        }

        [Test]
        public void Tabelas_12Rotas_3Aeroportos_NaDistribuicaoDoGdd()
        {
            Rota[] r = Tabelas.Rotas;
            Assert.AreEqual((12, 3), (r.Length, Tabelas.Aeroportos.Length));
            Assert.AreEqual(12, r.Select(x => x.Id).Distinct().Count(), "ids unicos");
            Assert.AreEqual(4, r.Count(x => x.Aeroporto == 0 && x.Tipo == TipoRota.Curta));
            Assert.AreEqual((3, 1), (r.Count(x => x.Aeroporto == 1 && x.Tipo == TipoRota.Turistica), r.Count(x => x.Aeroporto == 1 && x.Tipo == TipoRota.Conexao)));
            Assert.AreEqual((2, 2), (r.Count(x => x.Aeroporto == 2 && x.Tipo == TipoRota.Longa), r.Count(x => x.Aeroporto == 2 && x.Tipo == TipoRota.Conexao)));
            for (int a = 0; a < 3; a++) Assert.AreEqual(2, r.Count(x => x.Aeroporto == a && x.Custo == 0), "2 rotas sem preco, uma por vaga");
            foreach (Rota x in r.Take(2)) Assert.AreEqual((1f, 1f, 1f, 0f, 0f, 0, 1), (x.Pax, x.Tarifa, x.Gasto, x.Pico, x.Extra, x.Risco, x.Cada), "as iniciais sao as da v0.1");
            Assert.IsTrue(r.Where(x => x.Tipo == TipoRota.Longa).All(x => x.Cada == 2 && x.Extra >= new Tuning().Onda));
        }

        [Test]
        public void Rota_AbrePeloMarcoEPrecoFixo_EProgramaValeNaProximaOnda()
        {
            var s = new Sim { Cash = 5000 };
            Assert.AreEqual((true, true, false), (s.RotaAberta(0), s.RotaAberta(1), s.RotaAberta(Curitiba)));
            Assert.AreEqual(-1, s.CustoRota(Curitiba), "antes do marco 1");
            Assert.IsFalse(s.AbreRota(Curitiba));
            Assert.IsFalse(s.Programa(0, Curitiba), "rota fechada nao se programa");
            s.TotalEmbarcados = 150;
            Assert.AreEqual(250, s.CustoRota(Curitiba));
            Assert.AreEqual(-1, s.CustoRota(Salvador), "de aeroporto ainda nao pago");
            Assert.IsTrue(s.AbreRota(Curitiba));
            Assert.AreEqual((4750L, -1L), (s.Cash, s.CustoRota(Curitiba)));
            RegrasTests.Run(s, 1f);
            Assert.IsTrue(s.Programa(0, Curitiba));
            Assert.IsFalse(s.Programa(2, Curitiba));
            Assert.IsFalse(s.Programa(0, Salvador));
            Assert.AreEqual((0, s.T.Pax(0)), (s.Voos[0].Rota, s.Voos[0].Total), "o voo desta onda nao muda");
            RegrasTests.Run(s, 180f);
            Assert.AreEqual((Curitiba, (int)System.Math.Round(s.T.Pax(1) * 1.3), 1), (s.Voos[0].Rota, s.Voos[0].Total, s.Voos[1].Rota));
        }

        [Test]
        public void Viajar_PagaUmaVez_ChegaNaProximaOnda_EVoltarEGratis()
        {
            var s = new Sim { Cash = 5000 };
            RegrasTests.Run(s, 1f);
            Assert.AreEqual(-1, s.CustoViagem(1), "antes do marco 2");
            s.TotalEmbarcados = 600;
            Assert.AreEqual(1200, s.CustoViagem(1));
            Assert.AreEqual(-1, s.CustoViagem(2), "Aurora e' do marco 3");
            Assert.IsTrue(s.Viajar(1));
            Assert.AreEqual((3800L, 0, -1L), (s.Cash, s.AeroportoAtual, s.CustoViagem(1)));
            var ev = RegrasTests.Coleta(s, 180f);
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.AeroportoChegada && e.A == 1));
            Assert.AreEqual((1, Salvador, 5, Salvador, 5), (s.AeroportoAtual, s.Prog[0], s.Prog[1], s.Voos[0].Rota, s.Voos[1].Rota));
            Assert.IsTrue(s.RotaAberta(Salvador));
            Assert.AreEqual(0, s.CustoViagem(0));
            Assert.IsTrue(s.Viajar(0));
            RegrasTests.Run(s, 180f);
            Assert.AreEqual((0, 3800L, 0), (s.AeroportoAtual, s.Cash, s.Prog[0]));
            Assert.AreEqual(0, s.CustoViagem(1), "voltar ao que ja pagou e' gratis");
        }

        [Test]
        public void Longa_AviaoFicaFora_AVagaFicaSemVoo_ENadaSePerde()
        {
            Sim s = EstacoesPerfisTests.Pronta(Fixa(Manaus, 2));
            Assert.AreEqual(Manaus, s.Prog[0]);
            RegrasTests.Run(s, 175f);
            Aviao a = s.Avioes[0];
            Assert.AreEqual((false, 2), (a.NoPatio, a.VoltaOnda), "saiu na onda 0, volta na 2");
            long perdidos = s.TotalPerdidos;
            var ev = RegrasTests.Coleta(s, 180f);
            Voo v = s.Voos[0];
            Assert.AreEqual((1, 0, true), (v.Onda, v.Total, v.Partiu), "onda 1: a vaga nao tem voo");
            Assert.AreEqual(perdidos, s.TotalPerdidos, "sem voo nao ha perdido");
            Assert.AreEqual(0, ev.Count(e => e.Tipo == Ev.VooPerdido));
            RegrasTests.Run(s, 40f);
            Assert.AreEqual((2, true), (s.Voos[0].Onda, a.NoPatio));
            Assert.AreEqual((int)System.Math.Round(s.T.Pax(2) * (double)s.T.Rotas[Manaus].Pax), s.Voos[0].Total, "volta com o volume da rota");
            Assert.AreEqual(s.Voos[0].Id, a.VooId);
        }

        [Test]
        public void Conexao_EntraNaSeguranca_EmBloco()
        {
            Sim s = EstacoesPerfisTests.Pronta(Fixa(Guarulhos, 2));
            s.Tick(Sim.Step);
            Voo v = s.Voos[0];
            double fim = (s.T.Fecha[0] - s.T.ChegadaFolga) * Sim.Hz;
            Assert.GreaterOrEqual(s.Chegada(v, 0), (int)(fim * 0.7), "bloco no fim da janela");
            RegrasTests.Run(s, 60f);
            Assert.AreEqual(0, s.St(Estacao.CheckIn).Pessoas, "ninguem passa no check-in");
            Assert.Greater(s.TotalEmbarcados + s.St(Estacao.Seguranca).Pessoas + s.St(Estacao.Portao).Pessoas, 0);
        }

        [Test]
        public void Turistica_ChegadaConcentrada_EGastoMaior()
        {
            Sim s = EstacoesPerfisTests.Pronta(Fixa(Salvador, 1), Perfil.Bagageiro);
            s.Tick(Sim.Step);
            Assert.IsTrue(s.Move(3, Estacao.Loja));
            s.Staff[3].Andando = 0;
            Voo v = s.Voos[1];
            Assert.GreaterOrEqual(s.Chegada(v, 0), (int)((s.T.Fecha[1] - s.T.ChegadaFolga) * Sim.Hz * 0.5), "ninguem na 1a metade da janela");
            var ev = RegrasTests.Coleta(s, 170f);
            Assert.Greater(ev.Count(e => e.Tipo == Ev.Venda), 0);
            Assert.IsTrue(ev.Where(e => e.Tipo == Ev.Venda).All(e => e.B == 9), "loja 6 x 1,5");
        }

        [Test]
        public void CostaAzul_Maresia_GastaMaisOAviao()
        {
            Tuning t = Fixa(Salvador, 1);
            Sim s = EstacoesPerfisTests.Pronta(t);
            RegrasTests.Run(s, 175f);
            Assert.AreEqual((int)System.Math.Round(t.DesgastePorTrecho * 1.5), s.Avioes[0].Desgaste);
        }

        [Test]
        public void FrenteFria_SemSaida_NinguemEmbarcaNemDecola_EOVooPerdeOBonus()
        {
            Sim s = EstacoesPerfisTests.Pronta(Com(TipoIncidente.FrenteFria));
            Incidente i = AteOAviso(s);
            Assert.AreEqual((30L * Sim.Hz, 60L * Sim.Hz), (i.Inicio - s.Steps, i.Fim - i.Inicio), "TA-4: 30 s de aviso, 60 s de pista fechada");
            Voo v = s.Voos[i.Vaga];
            Assert.IsFalse(s.PistaFechada);
            int folga = s.TempoPassos(Estacao.Portao) + 1;   // quem ja estava passando termina
            bool bonus = false;
            while (s.Steps < i.Fim)
            {
                long t = s.Steps;
                s.Tick(Sim.Step);
                bonus |= s.Events.Any(e => e.Tipo == Ev.Bonus && e.A == v.Id);
                if (t < i.Inicio + folga) continue;
                Assert.IsFalse(s.Events.Any(e => e.Tipo == Ev.Embarcou || e.Tipo == Ev.Decolou || e.Tipo == Ev.Pousou), "pista fechada: t=" + t);
                Assert.IsFalse(s.PodeTocar(Estacao.Portao));
            }
            Assert.IsFalse(bonus, "fechou com a pista fechada: sem bonus de pontualidade");
            s.Tick(Sim.Step);
            Assert.IsNull(s.Inc);
        }

        [Test]
        public void FrenteFria_AdiantarNoAviso_SalvaOVoo()
        {
            Sim s = EstacoesPerfisTests.Pronta(Com(TipoIncidente.FrenteFria));
            Incidente i = AteOAviso(s);
            Voo v = s.Voos[i.Vaga];
            Assert.IsTrue(s.Adianta());
            Assert.IsFalse(s.Adianta(), "uma vez so");
            Assert.AreEqual(s.PassoNaOnda, v.AbrePasso, "o embarque abre ja");
            int noInicio = -1;
            bool decolou = false;
            while (s.Steps < i.Fim)
            {
                if (s.Steps == i.Inicio) noInicio = v.Embarcados;
                s.Tick(Sim.Step);
                decolou |= s.Events.Any(e => e.Tipo == Ev.Decolou && e.A == i.Vaga);
            }
            Assert.Greater(v.Embarcados, noInicio, "continuou embarcando com a pista fechada");
            Assert.IsTrue(v.Partiu && i.Resolvido);
            Assert.AreEqual(0, v.Perdidos, "quem adianta nao perde o voo");
            Assert.IsFalse(decolou, "a decolagem espera a pista abrir");
            s.Tick(Sim.Step);
            Assert.IsFalse(s.Adianta());
        }

        [Test]
        public void Falha_MaisLenta_ORepararNoAvisoEvita_EOReparoUsaOMecanico()
        {
            Sim s = EstacoesPerfisTests.Pronta(Com(TipoIncidente.Falha));
            Incidente i = AteOAviso(s);
            Assert.AreEqual(30L * Sim.Hz, i.Inicio - s.Steps);
            Assert.IsTrue(i.Estacao == Estacao.CheckIn || i.Estacao == Estacao.Seguranca || i.Estacao == Estacao.Portao);
            Ate(s, i.Inicio);
            s.Tick(Sim.Step);
            Assert.IsTrue(s.Falhou(i.Estacao));
            Station st = s.St(i.Estacao);
            st.Atendendo.Clear();
            Pax p = s.NovoPax(Sim.PaxId(0, i.Vaga, 0));
            p.Resto = 300;
            st.Atendendo.Add(p);
            for (int k = 0; k < 30; k++) s.Tick(Sim.Step);
            Assert.AreEqual(300 - 30 / s.T.FalhaFator, p.Resto, "1 passo em cada FalhaFator");

            Sim r = EstacoesPerfisTests.Pronta(Com(TipoIncidente.Falha));
            Incidente j = AteOAviso(r);
            long c = r.Cash;
            Assert.IsTrue(r.Repara());
            Assert.AreEqual((c - r.T.ReparoCusto, false), (r.Cash, r.MecanicoLivre));
            Assert.IsFalse(r.Repara(), "um reparo por vez");
            Ate(r, r.Steps + (long)(r.T.ReparoSeg * Sim.Hz) + 1);
            Assert.AreEqual((true, true), (j.Resolvido, r.MecanicoLivre));
            Ate(r, j.Inicio + 1);
            Assert.IsFalse(r.Falhou(j.Estacao), "reparou no aviso: a falha nao vem");
        }

        [Test]
        public void Falha_OMecanicoNaEstacao_ReparaDeGraca()
        {
            Sim s = EstacoesPerfisTests.Pronta(Com(TipoIncidente.Falha), Perfil.Mecanico);
            Incidente i = AteOAviso(s);
            Assert.IsTrue(s.Move(3, i.Estacao));
            s.Staff[3].Andando = 0;
            var ev = RegrasTests.Coleta(s, s.T.ReparoSeg + 0.1f);
            Assert.IsTrue(i.Resolvido);
            Assert.AreEqual(0, ev.Single(e => e.Tipo == Ev.ReparoIniciado).B, "de graca: o mecanico e' trabalho, nao pagamento");
        }

        [Test]
        public void Pico_GrupoExtraChegaDepoisDasChegadasNormais()
        {
            Sim s = EstacoesPerfisTests.Pronta(Com(TipoIncidente.Pico));
            Incidente i = AteOAviso(s);
            Voo v = s.Voos[i.Vaga];
            Assert.AreEqual((int)System.Math.Round(s.T.Pax(0) * (double)s.T.PicoExtra), v.Grupo);
            Assert.AreEqual(v.Base + v.Grupo, v.Total);
            Assert.AreEqual(i.Inicio, (long)s.Chegada(v, v.Base));
            Assert.Greater(s.Chegada(v, v.Total - 1), s.Chegada(v, v.Base));
            Assert.IsFalse(v.GrupoConexao);
            Ate(s, i.Inicio + 2);
            Assert.Greater(s.St(Estacao.CheckIn).Pessoas, 0, "o grupo entra pelo check-in");
        }

        [Test]
        public void Conexao_PriorizarFuraAFila_EOGrupoPagaMais()
        {
            Pax Primeiro(bool prioriza)
            {
                Sim s = EstacoesPerfisTests.Pronta(Com(TipoIncidente.Conexao));
                Incidente i = AteOAviso(s);
                Voo v = s.Voos[i.Vaga];
                Assert.IsTrue(v.GrupoConexao);
                if (prioriza) { Assert.IsTrue(s.Prioriza()); Assert.IsFalse(s.Prioriza()); }
                Station seg = s.St(Estacao.Seguranca);
                seg.Fila.Clear(); seg.Atendendo.Clear();
                seg.Fila.Add(s.NovoPax(Sim.PaxId(0, i.Vaga, 0)));
                seg.Fila.Add(s.NovoPax(Sim.PaxId(0, i.Vaga, v.Base)));
                s.Tick(Sim.Step);
                return seg.Atendendo[0];
            }
            Assert.AreEqual(0, Primeiro(false).Indice, "sem priorizar, a ordem da fila");
            Assert.Greater(Primeiro(true).Indice, 0, "priorizado passa a frente");

            Sim r = EstacoesPerfisTests.Pronta(Com(TipoIncidente.Conexao));
            Incidente j = AteOAviso(r);
            Voo w = r.Voos[j.Vaga];
            int tarifa = (int)System.Math.Round(r.Tarifa * (double)r.T.Rotas[w.Rota].Tarifa);
            Assert.IsTrue(r.Prioriza());
            var ev = RegrasTests.Coleta(r, (float)((j.Fim - r.Steps) * Sim.Step));
            var grupo = ev.Where(e => e.Tipo == Ev.Embarcou && e.A / 500 % 2 == j.Vaga && e.A % 500 >= w.Base).ToList();
            Assert.Greater(grupo.Count, 0, "o grupo embarcou");
            Assert.IsTrue(grupo.All(e => e.B == r.T.ConexaoTarifa * tarifa));
        }

        [Test]
        public void Incidente_SaiDaSemente_EReproduzivel()
        {
            string Lista(int seed)
            {
                var s = new Sim(BalanceTests.AbertoC3(2), seed);
                var b = new Bot(BotProfile.Bom);
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < 20 * 180 * Sim.Hz; i++)
                {
                    b.Step(s);
                    foreach (Evento e in s.Events) if (e.Tipo == Ev.IncidenteAviso) sb.Append(s.Onda).Append(':').Append(e.A).Append(':').Append(e.B).Append(' ');
                }
                return sb.ToString();
            }
            Assert.IsNotEmpty(Lista(1));
            Assert.AreEqual(Lista(1), Lista(1), "reproduzivel");
            Assert.AreNotEqual(Lista(1), Lista(2));
        }

        [Test]
        public void Incidente_UmPorVez_ComFolgaDepois()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var s = new Sim(Com(TipoIncidente.Falha), seed);
                int ultima = -9, abertos = 0;
                for (int i = 0; i < 20 * 180 * Sim.Hz; i++)
                {
                    s.Tick(Sim.Step);
                    foreach (Evento e in s.Events)
                    {
                        if (e.Tipo == Ev.IncidenteAviso) { abertos++; Assert.Greater(s.Onda - ultima, s.T.IncidenteFolga, "semente " + seed); ultima = s.Onda; }
                        if (e.Tipo == Ev.IncidenteFim) abertos--;
                        Assert.LessOrEqual(abertos, 1);
                    }
                }
                Assert.Greater(ultima, 0);
            }
        }

        [Test]
        public void ValeVerde_SemRotaDeRisco_NaoTemIncidente()
        {
            Bot.Resultado r = Bot.Medir(BotProfile.Bom, 3, 20, BalanceTests.SemFaseC3());
            Assert.AreEqual(0, r.Incidentes);
        }

        [Test]
        public void SoVisual_RotasEIncidentes_NaoMudamOsNumeros()
        {
            Tuning fixa = Fixa(Manaus, 2);
            fixa.RegraRotas = false;
            Sim s = EstacoesPerfisTests.Pronta(fixa);
            s.Tick(Sim.Step);
            Assert.AreEqual((Manaus, s.T.Pax(0)), (s.Voos[0].Rota, s.Voos[0].Total), "o nome muda, os numeros nao");
            RegrasTests.Run(s, 185f);
            Assert.Greater(s.Voos[0].Total, 0, "sem rota longa de verdade");

            Tuning visual = Com(TipoIncidente.FrenteFria), sem = Com(TipoIncidente.FrenteFria);
            visual.RegraIncidentes = false;
            sem.RegraIncidentes = false;
            sem.Aeroportos = sem.Aeroportos.Select(x => { Aeroporto c = x.Copia(); c.Incidentes = 0f; return c; }).ToArray();
            sem.Rotas = sem.Rotas.Select(x => { Rota c = x.Copia(); c.Risco = 0; return c; }).ToArray();
            Bot.Resultado a = Bot.Medir(BotProfile.Bom, 3, 8, visual), b = Bot.Medir(BotProfile.Bom, 3, 8, sem);
            Assert.Greater(a.Incidentes, 0, "o aviso continua");
            Assert.AreEqual((b.Receita, b.Embarque), (a.Receita, a.Embarque));
        }

        [Test]
        public void ConteudoNovoFechado_OBotJogaIgualAoDeAntes()
        {
            for (int seed = 1; seed <= 3; seed++)
            {
                BotProfile sem = BotProfile.Bom;
                sem.Rotas = sem.Viaja = sem.Incidentes = false;
                Bot.Resultado a = Bot.Medir(BotProfile.Bom, seed, 6, BalanceTests.SemFaseC3()), b = Bot.Medir(sem, seed, 6);
                Assert.AreEqual((b.Receita, b.Embarque, b.Moves), (a.Receita, a.Embarque, a.Moves));
            }
        }
    }
}
