using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Regras da Fase C5 (CONTRATO_FASE_C5 §2): a tabela, as metas de estado e de contador, a ordem da cadeia e o pagamento. As
    /// invariantes ficam no ExploitTests; a migracao, no SaveTests; os portoes, no BalanceTests.</summary>
    public class CampanhaTests
    {
        /// <summary>Um objetivo de teste.</summary>
        internal static Objetivo Obj(Meta m, int alvo = 1, int arg = -1, int cash = 50) => new Objetivo { Id = "t", Meta = m, Alvo = alvo, Arg = arg, Recompensa = cash };

        /// <summary>So a cadeia `o` no aeroporto `aero`; as outras ficam vazias.</summary>
        internal static Tuning So(int aero, params Objetivo[] o) => Em(new Tuning(), aero, o);

        internal static Tuning Em(Tuning t, int aero, params Objetivo[] o)
        {
            t.RegraCampanha = true;
            t.Objetivos = new[] { new Objetivo[0], new Objetivo[0], new Objetivo[0] };
            t.Objetivos[aero] = o;
            return t;
        }

        static BotProfile SoNoAeroporto(BotProfile p)
        {
            p.Viaja = false;
            return p;
        }

        static List<Evento> Joga(Sim s, BotProfile p, int ondas)
        {
            var bot = new Bot(p);
            var ev = new List<Evento>();
            long fim = s.Steps + (long)ondas * s.OndaPassos;
            while (s.Steps < fim)
            {
                bot.Step(s);
                ev.AddRange(s.Events);
            }
            return ev;
        }

        // ------------------------------------------------------------------ a tabela

        [Test]
        public void Tabela_30Objetivos_10PorAeroporto_IdsNaOrdem()
        {
            // o save guarda a posicao na cadeia: objetivo novo so no fim, e um id nunca muda
            string[][] ids =
            {
                new[] { "vv_embarque", "vv_ana", "vv_equipe", "vv_mover", "vv_balcao", "vv_rosa", "vv_loja", "vv_ceu", "vv_curitiba", "vv_terminal" },
                new[] { "ca_chegada", "ca_salvador", "ca_comida", "ca_bagagem", "ca_checagem", "ca_incidente", "ca_vento", "ca_hangar", "ca_fortaleza", "ca_terminal" },
                new[] { "ag_chegada", "ag_guarulhos", "ag_conexao", "ag_pontual", "ag_neve", "ag_manaus", "ag_portao", "ag_lisboa", "ag_sequencia", "ag_hub" },
            };
            Assert.AreEqual(3, Tabelas.Objetivos.Length);
            for (int a = 0; a < 3; a++)
            {
                CollectionAssert.AreEqual(ids[a], Tabelas.Objetivos[a].Select(o => o.Id).ToArray(), Tabelas.Aeroportos[a].Nome);
                foreach (Objetivo o in Tabelas.Objetivos[a])
                {
                    StringAssert.StartsWith(Tabelas.Aeroportos[a].Id + "_", o.Id);
                    Assert.Greater(o.Recompensa, 0, o.Id);
                    Assert.Greater(o.Alvo, 0, o.Id);
                    Assert.IsNotEmpty(o.Nome, o.Id);
                }
            }
        }

        /// <summary>Nenhum objetivo pede o que o jogo nao tem (CONTRATO_FASE_C5 §4, invariante 6): a rota e' do aeroporto, o alvo cabe no teto e o
        /// perigo pedido acontece ali. Clima e' sempre "qualquer um": a cadeia nao espera um clima raro.</summary>
        [Test]
        public void Tabela_CoerenteComORestoDoJogo()
        {
            Tuning t = Tuning.Padrao;
            for (int a = 0; a < 3; a++)
                for (int i = 0; i < t.Objetivos[a].Length; i++)
                {
                    Objetivo o = t.Objetivos[a][i];
                    switch (o.Meta)
                    {
                        case Meta.Cheios: case Meta.Voos: if (o.Arg >= 0) Assert.AreEqual(a, t.Rotas[o.Arg].Aeroporto, o.Id); break;
                        case Meta.Chegou: Assert.AreEqual((a, 0, 1), (o.Arg, i, o.Alvo), o.Id); break;
                        case Meta.Velocidade: Assert.LessOrEqual(o.Alvo, t.VelNiveis, o.Id); break;
                        case Meta.Capacidade: Assert.LessOrEqual(o.Alvo, t.CapNiveis, o.Id); break;
                        case Meta.Equipe: Assert.LessOrEqual(o.Alvo, Sim.MaxEquipe, o.Id); break;
                        case Meta.Marco: Assert.LessOrEqual(o.Alvo, t.Marcos.Length, o.Id); break;
                        case Meta.Atendidos: CollectionAssert.Contains(new[] { Estacao.Loja, Estacao.Bagagem, Estacao.Alimentacao }, (Estacao)o.Arg, o.Id); break;
                        case Meta.Climas: Assert.AreEqual(-1, o.Arg, o.Id + ": clima e' sempre qualquer um"); break;
                        case Meta.Incidentes: if (o.Arg >= 0) Assert.Greater(t.Aeroportos[a].Pesos[o.Arg], 0, o.Id); break;
                    }
                }
            Assert.AreNotEqual(Meta.Chegou, t.Objetivos[0][0].Meta, "em Vale Verde o jogo ja comeca");
        }

        /// <summary>docs/CAMPANHA.md e' a lista que o Vinicius le: tem de ser a tabela do codigo, linha a linha.</summary>
        [Test]
        public void CampanhaMd_IgualATabela()
        {
            string dir = TestContext.CurrentContext.TestDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "docs", "CAMPANHA.md"))) dir = Path.GetDirectoryName(dir);
            if (dir == null) Assert.Ignore("sem o docs/ do repositorio aqui");
            string[][] linhas = File.ReadAllLines(Path.Combine(dir, "docs", "CAMPANHA.md")).Where(l => l.StartsWith("| ")).Select(l => l.Split('|').Select(c => c.Trim()).ToArray()).ToArray();
            for (int a = 0; a < 3; a++)
                for (int i = 0; i < 10; i++)
                {
                    Objetivo o = Tabelas.Objetivos[a][i];
                    string[] c = linhas.Single(l => l[2] == o.Id);
                    string meta = "`" + o.Meta + (o.Arg >= 0 ? "[" + o.Arg + "]" : "") + " ≥ " + o.Alvo + "`";
                    Assert.AreEqual((i + 1 + "", o.Nome, meta, o.Recompensa + ""), (c[1], c[3], c[5], c[6]), o.Id);
                }
            Assert.AreEqual(30, linhas.Count(l => l[2].Length > 3 && l[2][2] == '_'), "nada a mais no CAMPANHA.md");
        }

        // ------------------------------------------------------------------ regras

        [Test]
        public void MetaDeEstado_ConcluiNoPassoFixo_PagaEAvancaACadeia()
        {
            var s = new Sim(So(0, Obj(Meta.Equipe, 1, -1, 77), Obj(Meta.Equipe, 2)), 1) { Cash = 20 };
            Assert.AreEqual((0, 0, false), (s.ObjetivosFeitos(0), s.Progresso(0), s.CampanhaCompleta));
            Assert.IsTrue(s.Hire());
            Assert.AreEqual((1, 0, 0L), (s.Progresso(0), s.ObjetivosFeitos(0), s.Cash), "a compra nao conclui: so o passo fixo");
            s.Tick(Sim.Step);
            Evento e = s.Events.Single(x => x.Tipo == Ev.ObjetivoConcluido);
            Assert.AreEqual((0, 0, 77), (e.A, e.B, e.C));
            Assert.AreEqual((1, 77L), (s.ObjetivosFeitos(0), s.Cash));
            Assert.AreEqual(Meta.Equipe, s.ObjetivoAtual(0).Meta);
            Assert.AreEqual(2, s.ObjetivoAtual(0).Alvo);
            Assert.AreEqual(1, s.Progresso(0), "meta de estado le o jogo");
        }

        [Test]
        public void MetaDeEstadoJaCumprida_ConcluiEmCascata_UmaPorPasso()
        {
            var s = new Sim(So(0, Obj(Meta.Marco), Obj(Meta.Marco), Obj(Meta.Marco), Obj(Meta.Embarcados)), 1) { TotalEmbarcados = 150 };
            s.Tick(Sim.Step);
            Assert.AreEqual(1, s.ObjetivosFeitos(0));
            s.Tick(Sim.Step);
            s.Tick(Sim.Step);
            Assert.AreEqual(3, s.ObjetivosFeitos(0));
            s.Tick(Sim.Step);
            Assert.AreEqual(3, s.ObjetivosFeitos(0), "contador nao conclui sozinho");
            Assert.AreEqual(150L, s.Cash);
        }

        [Test]
        public void Estado_NivelECapacidade_LemOJogo_EmQualquerAeroporto()
        {
            var s = new Sim(So(2, Obj(Meta.Velocidade, 2, (int)Estacao.CheckIn), Obj(Meta.Capacidade, 1, (int)Estacao.Portao)), 1) { Cash = 1000 };
            Assert.IsTrue(s.Buy(Estacao.CheckIn, Melhoria.Velocidade));
            s.Tick(Sim.Step);
            Assert.AreEqual((0, 1), (s.ObjetivosFeitos(2), s.Progresso(2)));
            Assert.IsTrue(s.Buy(Estacao.CheckIn, Melhoria.Velocidade));
            s.Tick(Sim.Step);
            Assert.AreEqual((1, 0), (s.ObjetivosFeitos(2), s.Progresso(2)));
            Assert.IsTrue(s.Buy(Estacao.Portao, Melhoria.Capacidade));
            s.Tick(Sim.Step);
            Assert.IsNull(s.ObjetivoAtual(2), "fora de Aurora: meta de estado vale do mesmo jeito");
        }

        /// <summary>Chegou: so depois de chegar. Marcar a viagem nao conclui, e voltar nao desfaz.</summary>
        [Test]
        public void Chegou_SoDepoisDeChegar()
        {
            var s = new Sim(So(1, Obj(Meta.Chegou, 1, 1), Obj(Meta.Embarcados, 99)), 1) { TotalEmbarcados = 600, Cash = 5000 };
            s.Tick(Sim.Step);
            Assert.IsTrue(s.Viajar(1));
            s.Tick(Sim.Step);
            Assert.AreEqual(0, s.ObjetivosFeitos(1), "marcou, ainda nao chegou");
            while (s.AeroportoAtual != 1) s.Tick(Sim.Step);
            s.Tick(Sim.Step);
            Assert.AreEqual(1, s.ObjetivosFeitos(1));
        }

        [Test]
        public void Contador_ContaAntesDeVirarOAtivo_MasConcluiNaOrdem()
        {
            Tuning t = So(0, Obj(Meta.Equipe, 1), Obj(Meta.Embarcados, 5));
            var s = new Sim(t, 1);
            for (int i = 0; i < s.OndaPassos && s.TotalEmbarcados < 8; i++)
            {
                foreach (Estacao e in new[] { Estacao.CheckIn, Estacao.Seguranca, Estacao.Portao }) s.Tap(e);
                s.Tick(Sim.Step);
            }
            Assert.GreaterOrEqual(s.TotalEmbarcados, 8L, "embarcou no toque");
            Assert.AreEqual((0, 0), (s.ObjetivosFeitos(0), s.Progresso(0)), "a ordem segura: o ativo ainda e' a contratacao");
            s.Cash = 20;
            Assert.IsTrue(s.Hire());
            s.Tick(Sim.Step);
            Assert.AreEqual(Meta.Embarcados, s.ObjetivoAtual(0).Meta);
            Assert.GreaterOrEqual(s.Progresso(0), 5, "o que ja embarcou vale");
            s.Tick(Sim.Step);
            Assert.AreEqual(2, s.ObjetivosFeitos(0), "concluiu no passo seguinte");
        }

        [Test]
        public void Contador_SoContaNoAeroportoDoObjetivo()
        {
            var fora = new Sim(So(1, Obj(Meta.Embarcados, 1)), 1);
            Joga(fora, BotProfile.Bom, 2);
            Assert.Greater(fora.TotalEmbarcados, 0);
            Assert.AreEqual((0, 0), (fora.Progresso(1), fora.ObjetivosFeitos(1)), "embarcou em Vale Verde: nao conta para a Costa Azul");

            Tuning t = So(1, Obj(Meta.Embarcados, 1));
            t.AeroportoInicial = 1;
            var la = new Sim(t, 1);
            Joga(la, BotProfile.Bom, 2);
            Assert.AreEqual(1, la.ObjetivosFeitos(1));
        }

        /// <summary>O que cada contador conta, contra os eventos da mesma partida.</summary>
        [Test]
        public void Contadores_SeguemOsEventos()
        {
            void Confere(Meta m, Func<Evento, bool> conta, bool temDeTer = true)
            {
                var s = new Sim(So(0, Obj(m, 999999)), 3);
                List<Evento> ev = Joga(s, SoNoAeroporto(BotProfile.Bom), 16);
                int n = ev.Count(conta);
                Assert.AreEqual(n, s.Progresso(0), m.ToString());
                if (temDeTer) Assert.Greater(n, 0, m + ": entrou em jogo");
            }
            Confere(Meta.Embarcados, e => e.Tipo == Ev.Embarcou);
            Confere(Meta.Historias, e => e.Tipo == Ev.Historia);
            Confere(Meta.Pontuais, e => e.Tipo == Ev.Bonus);
            Confere(Meta.Checagens, e => e.Tipo == Ev.ChecagemIniciada && e.C == 0);
            Confere(Meta.Climas, e => e.Tipo == Ev.IncidenteFim && e.B == 1 && e.A >= (int)TipoIncidente.Temporal || e.Tipo == Ev.IncidenteFim && e.B == 1 && e.A == (int)TipoIncidente.FrenteFria);
            Confere(Meta.Voos, e => e.Tipo == Ev.VooPartiu && e.B > 0);
            Confere(Meta.Cheios, e => e.Tipo == Ev.VooPartiu && e.C > 0 && e.B * 100 >= e.C * Tuning.Padrao.LotadoPct);
        }

        [Test]
        public void VoosECheios_SoNaRotaPedida()
        {
            const int Rota = 1;
            var s = new Sim(So(0, Obj(Meta.Cheios, 999, Rota)), 1);
            var v = new Sim(So(0, Obj(Meta.Voos, 999, Rota)), 1);
            int cheios = 0, voos = 0;
            var bs = new Bot(BotProfile.Bom); var bv = new Bot(BotProfile.Bom);
            for (int i = 0; i < 6 * s.OndaPassos; i++)
            {
                bs.Step(s); bv.Step(v);
                foreach (Evento e in s.Events)
                {
                    if (e.Tipo != Ev.VooPartiu || s.Voos[e.A % 2].Rota != Rota) continue;
                    if (e.B > 0) voos++;
                    if (e.C > 0 && e.B * 100 >= e.C * s.T.LotadoPct) cheios++;
                }
            }
            Assert.Greater(voos, 0);
            Assert.AreEqual((cheios, voos), (s.Progresso(0), v.Progresso(0)));
        }

        [Test]
        public void Seguidos_PerdaZeraASequencia()
        {
            var s = new Sim(So(0, Obj(Meta.Seguidos, 999)), 1);
            var bot = new Bot(SoNoAeroporto(BotProfile.Lento));
            int seguidos = 0, perdas = 0, maior = 0;
            for (int i = 0; i < 24 * s.OndaPassos; i++)
            {
                bot.Step(s);
                foreach (Evento e in s.Events)
                {
                    if (e.Tipo != Ev.VooPartiu || e.C == 0) continue;
                    if (e.B * 100 >= e.C * s.T.LotadoPct) maior = Math.Max(maior, ++seguidos);
                    else { seguidos = 0; perdas++; }
                }
                Assert.AreEqual(seguidos, s.Progresso(0));
            }
            Assert.Greater(perdas, 0, "houve voo com perda");
            Assert.Greater(maior, 1, "houve sequencia");
        }

        [Test]
        public void Atendidos_SoNaEstacaoPedida()
        {
            var s = new Sim(So(0, Obj(Meta.Atendidos, 999, (int)Estacao.Loja)), 1) { Cash = 1000 };
            while (s.Hire()) { }
            Assert.AreEqual(0, s.Progresso(0));
            s.Move(2, Estacao.Loja);
            int vendas = 0;
            for (int i = 0; i < 3 * s.OndaPassos; i++)
            {
                s.Tick(Sim.Step);
                vendas += s.Events.Count(e => e.Tipo == Ev.Venda);
            }
            Assert.Greater(vendas, 0, "a loja vendeu");
            Assert.AreEqual(vendas, s.Progresso(0));
        }

        [Test]
        public void Movidos_SoParaEstacaoApertada()
        {
            var s = new Sim(So(0, Obj(Meta.Movidos, 5)), 1) { Cash = 1000 };
            while (s.Hire()) { }
            Assert.IsTrue(s.Move(0, Estacao.Seguranca));
            Assert.AreEqual(0, s.Progresso(0), "para estacao normal nao conta");
            s.St(Estacao.Portao).Estado = Estado.Atencao;
            Assert.IsTrue(s.Move(1, Estacao.Portao));
            s.St(Estacao.Loja).Estado = Estado.Critico;
            Assert.IsTrue(s.Move(2, Estacao.Loja));
            Assert.AreEqual(2, s.Progresso(0));
        }

        /// <summary>Clima so conta resolvido: segurar sem fazer nada nao conta. As saidas contam.</summary>
        [Test]
        public void Climas_SoContaResolvido()
        {
            Tuning t = Em(ClimaFalhasTests.ComClima(TipoIncidente.Temporal, 0), 0, Obj(Meta.Climas, 2));
            Sim s = EstacoesPerfisTests.Pronta(t);
            Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.IncidenteAviso, 400f));
            Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.IncidenteFim, 200f));
            Assert.AreEqual(0, s.Progresso(0), "segurou: nao resolveu");

            Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.IncidenteAviso, 600f));
            Assert.IsTrue(s.Adianta());
            Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.IncidenteFim, 200f));
            Assert.AreEqual(1, s.Progresso(0));

            Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.IncidenteAviso, 600f));
            Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.IncidenteInicio, 100f));
            Assert.IsTrue(s.Atrasa());
            Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.ObjetivoConcluido, 200f));
        }

        /// <summary>Incidente com Arg: so aquele tipo. A saida resolve; clima nao conta como incidente.</summary>
        [Test]
        public void Incidentes_SoOTipoPedido()
        {
            Tuning Conexao()
            {
                Tuning t = BalanceTests.AbertoC3(2);
                t.Aeroportos = t.Aeroportos.Select(x => { Aeroporto c = x.Copia(); c.Pesos = new[] { 0, 0, 0, 1 }; c.Incidentes = 1f; return c; }).ToArray();
                t.IncidenteTeto = 1f;
                return t;
            }
            foreach ((int tipo, int conta) in new[] { (3, 1), (1, 0) })
            {
                Sim s = EstacoesPerfisTests.Pronta(Em(Conexao(), 2, Obj(Meta.Incidentes, 5, tipo)));
                Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.IncidenteAviso, 600f));
                Assert.IsTrue(s.Prioriza());
                Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.IncidenteFim, 200f));
                Assert.AreEqual(conta, s.Progresso(2), "tipo " + tipo);
            }
        }

        [Test]
        public void Hangar_ContaAoSair()
        {
            Sim s = EstacoesPerfisTests.Pronta(So(0, Obj(Meta.Hangar, 1)));
            s.Cash = 1000;
            for (int i = 0; i < 400 * Sim.Hz && !s.PodeHangar(0); i++) s.Tick(Sim.Step);
            Assert.IsTrue(s.Hangar(0));
            Assert.AreEqual(0, s.Progresso(0), "ainda no hangar");
            Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.HangarFeito, 300f));
            Assert.AreEqual(1, s.ObjetivosFeitos(0) + s.Progresso(0));
        }

        [Test]
        public void RegraDesligada_ContaEMostra_NaoPaga()
        {
            Tuning t = So(0, Obj(Meta.Equipe, 1, -1, 500));
            t.RegraCampanha = false;
            var s = new Sim(t, 1) { Cash = 20 };
            Assert.IsTrue(s.Hire());
            s.Tick(Sim.Step);
            Assert.AreEqual((1, 0L, true), (s.ObjetivosFeitos(0), s.Cash, s.CampanhaCompleta));
            Assert.AreEqual(0, s.Events.Single(e => e.Tipo == Ev.ObjetivoConcluido).C);
        }

        [Test]
        public void CampanhaCompleta_SoComAsTresCadeiasFechadas()
        {
            Tuning t = So(0, Obj(Meta.Marco));
            t.Objetivos[2] = new[] { Obj(Meta.Equipe) };
            var s = new Sim(t, 1) { TotalEmbarcados = 150, Cash = 20 };
            s.Tick(Sim.Step);
            Assert.AreEqual((1, false), (s.ObjetivosFeitos(0), s.CampanhaCompleta));
            Assert.IsNull(s.ObjetivoAtual(0));
            Assert.AreEqual(0, s.Progresso(0), "cadeia fechada");
            s.Hire();
            s.Tick(Sim.Step);
            Assert.IsTrue(s.CampanhaCompleta);
        }

        /// <summary>A campanha e' a mesma com e sem fechar e abrir o jogo no meio de cada onda.</summary>
        [Test]
        public void Reproduzivel_ComESemFecharEAbrir()
        {
            foreach (BotProfile p in new[] { BotProfile.Guiado, BotProfile.Bom })
            {
                Bot.Resultado a = Bot.Medir(p, 2, 24), b = Bot.Medir(p, 2, 24), c = Bot.Medir(p, 2, 24, null, true);
                Assert.Greater(a.Objetivos, 4, p.Name);
                CollectionAssert.AreEqual(a.ObjetivoEm, b.ObjetivoEm, p.Name + ": mesma semente, mesma campanha");
                CollectionAssert.AreEqual(a.ObjetivoEm, c.ObjetivoEm, p.Name + ": fechar e abrir nao muda");
                Assert.AreEqual((a.Recompensas, a.Receita), (c.Recompensas, c.Receita), p.Name);
            }
        }

        [Test]
        public void EnumDoEvento_SoCresceNoFim()
        {
            string[] ev = Enum.GetNames(typeof(Ev));
            Assert.AreEqual("ObjetivoConcluido", ev[Array.IndexOf(ev, "Remarcados") + 1]);
        }
    }
}
