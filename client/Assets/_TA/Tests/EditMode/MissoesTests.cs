using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Regras da Fase C6 (CONTRATO_FASE_C6 §2): as tabelas, a virada pelo dia do jogo, a escolha pela semente, o pagamento e as conquistas. As
    /// invariantes ficam no ExploitTests; a migracao, no SaveTests; os portoes, no BalanceTests.</summary>
    public class MissoesTests
    {
        static Objetivo Obj(Meta m, int alvo = 1, int arg = -1, int cash = 50) => CampanhaTests.Obj(m, alvo, arg, cash);

        /// <summary>Sem campanha, so as tabelas do teste.</summary>
        internal static Tuning Com(Objetivo[] diarias, Objetivo[] semanais = null, Objetivo[] conquistas = null) => new Tuning
        {
            Objetivos = new[] { new Objetivo[0], new Objetivo[0], new Objetivo[0] }, Diarias = diarias, Semanais = semanais ?? new Objetivo[0], Conquistas = conquistas ?? new Objetivo[0],
        };

        static Objetivo[] Sem => new Objetivo[0];

        static void Joga(Sim s, BotProfile p, int ondas)
        {
            var bot = new Bot(p);
            for (int i = 0; i < ondas * s.OndaPassos; i++) bot.Step(s);
        }

        static string[] Ids(Missao[] vagas) => vagas.Select(m => m?.O.Id).ToArray();

        // ------------------------------------------------------------------ as tabelas

        [Test]
        public void Tabelas_IdsUnicos_PrefixoDoMarco_RecompensaFixa()
        {
            string[] ids = Tabelas.Diarias.Concat(Tabelas.Semanais).Concat(Tabelas.Conquistas).Select(o => o.Id).ToArray();
            CollectionAssert.AllItemsAreUnique(ids);
            foreach (Objetivo o in Tabelas.Diarias) { StringAssert.StartsWith("d" + o.Marco + "_", o.Id); Assert.Greater(o.Recompensa, 0, o.Id); Assert.IsNull(o.E, o.Id + ": diaria tem uma parte"); }
            foreach (Objetivo o in Tabelas.Semanais)
            {
                StringAssert.StartsWith("s" + o.Marco + "_", o.Id);
                Assert.Greater(o.Recompensa, 0, o.Id);
                Assert.IsNotNull(o.E, o.Id + ": semanal combina dois sistemas (GDD §6.5)");
                Assert.IsFalse(o.Meta == o.E.Meta && o.Arg == o.E.Arg, o.Id);
            }
            Assert.AreEqual(20, Tabelas.Conquistas.Length, "GDD §4.1");
            foreach (Objetivo o in Tabelas.Conquistas) { StringAssert.StartsWith("c_", o.Id); Assert.AreEqual((0, 0, (Objetivo)null), (o.Recompensa, o.Marco, o.E), o.Id + ": conquista nao paga Cash"); }
        }

        /// <summary>Missao mede o que acontece no periodo: nada de estado (conclui sozinha), nada que recue (Seguidos), nada que dependa so do sorteio na diaria.</summary>
        [Test]
        public void Tabelas_MissaoSoContaEvento_ENuncaRecua()
        {
            Meta[] estado = { Meta.Marco, Meta.Equipe, Meta.Velocidade, Meta.Capacidade, Meta.Chegou, Meta.Passageiros, Meta.Rotas, Meta.Campanha, Meta.Missoes, Meta.Seguidos };
            foreach (Objetivo o in Tabelas.Diarias.Concat(Tabelas.Semanais).Concat(Tabelas.Semanais.Select(x => x.E)))
                CollectionAssert.DoesNotContain(estado, o.Meta, o.Id);
            foreach (Objetivo o in Tabelas.Diarias)
                Assert.IsFalse(o.Meta == Meta.Climas || o.Meta == Meta.Incidentes, o.Id + ": diaria nao espera a semente");
        }

        /// <summary>Nenhuma missao pede o que o jogo nao tem naquele marco: a rota abre ate ali e o alvo cabe no periodo.</summary>
        [Test]
        public void Tabelas_CoerentesComORestoDoJogo()
        {
            Tuning t = Tuning.Padrao;
            void Parte(Objetivo o, int marco, string id, int voos)
            {
                switch (o.Meta)
                {
                    case Meta.Voos: case Meta.Cheios:
                        if (o.Arg >= 0) Assert.LessOrEqual(t.Rotas[o.Arg].Marco, marco, id);
                        Assert.LessOrEqual(o.Alvo, voos, id + ": cabe no periodo");
                        break;
                    case Meta.Atendidos: CollectionAssert.Contains(new[] { Estacao.Loja, Estacao.Bagagem, Estacao.Alimentacao }, (Estacao)o.Arg, id); break;
                    case Meta.Climas: case Meta.Incidentes: Assert.AreEqual(-1, o.Arg, id + ": perigo e' sempre qualquer um"); break;
                }
            }
            foreach (Objetivo o in t.Diarias) Parte(o, o.Marco, o.Id, 2 * t.DiaOndas);
            foreach (Objetivo o in t.Semanais) { Parte(o, o.Marco, o.Id, 2 * t.DiaOndas * Sim.SemanaDias); Parte(o.E, o.Marco, o.Id, 2 * t.DiaOndas * Sim.SemanaDias); }
            foreach (Objetivo o in t.Conquistas)
                switch (o.Meta)
                {
                    case Meta.Equipe: Assert.AreEqual(Sim.MaxEquipe, o.Alvo, o.Id); break;
                    case Meta.Rotas: Assert.AreEqual(t.Rotas.Length, o.Alvo, o.Id); break;
                    case Meta.Campanha: Assert.AreEqual(t.Objetivos.Sum(c => c.Length), o.Alvo, o.Id); break;
                    case Meta.Chegou: Assert.AreEqual(t.Aeroportos.Length - 1, o.Arg, o.Id); break;
                }
        }

        /// <summary>Em todo marco, com as rotas fechadas e o catalogo no fim, a lista enche sem repetir meta: nenhuma vaga fica vazia.</summary>
        [Test]
        public void Lista_EmTodoMarco_EncheSemRepetirMeta()
        {
            for (int marco = 0; marco <= 3; marco++)
                foreach (bool semCatalogo in new[] { false, true })
                    for (int seed = 1; seed <= 40; seed++)
                    {
                        var s = new Sim(null, seed) { TotalEmbarcados = marco == 0 ? 0 : Tuning.Padrao.Marcos[marco - 1] };
                        if (semCatalogo) foreach (Station e in s.Estacoes) { e.Vel = s.T.VelNiveis; e.Cap = s.T.CapNiveis; }
                        s.Tick(Sim.Step);
                        foreach (Missao[] vagas in new[] { s.Diarias, s.Semanais })
                        {
                            CollectionAssert.AllItemsAreNotNull(vagas, "marco " + marco + " semente " + seed);
                            CollectionAssert.AllItemsAreUnique(vagas.Select(m => m.O.Meta).ToArray());
                            Assert.IsTrue(vagas.All(m => m.O.Marco == marco), "so a linha do marco");
                        }
                    }
        }

        /// <summary>docs/MISSOES.md e' a lista que o Vinicius le: tem de ser as tabelas do codigo, linha a linha.</summary>
        [Test]
        public void MissoesMd_IgualAsTabelas()
        {
            string dir = TestContext.CurrentContext.TestDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "docs", "MISSOES.md"))) dir = Path.GetDirectoryName(dir);
            if (dir == null) Assert.Ignore("sem o docs/ do repositorio aqui");
            string[][] linhas = File.ReadAllLines(Path.Combine(dir, "docs", "MISSOES.md")).Where(l => l.StartsWith("| ")).Select(l => l.Split('|').Select(c => c.Trim()).ToArray()).ToArray();
            string M(Objetivo o) => "`" + o.Meta + (o.Arg >= 0 ? "[" + o.Arg + "]" : "") + " ≥ " + o.Alvo + "`";
            foreach (Objetivo o in Tabelas.Diarias.Concat(Tabelas.Semanais))
            {
                string[] c = linhas.Single(l => l[1] == o.Id);
                Assert.AreEqual((o.Nome, o.Marco + "", M(o) + (o.E != null ? " e " + M(o.E) : ""), o.Recompensa + ""), (c[2], c[3], c[4], c[5]), o.Id);
            }
            foreach (Objetivo o in Tabelas.Conquistas)
            {
                string[] c = linhas.Single(l => l[1] == o.Id);
                Assert.AreEqual((o.Nome, M(o)), (c[2], c[3]), o.Id);
            }
            Assert.AreEqual(Tabelas.Diarias.Length + Tabelas.Semanais.Length + Tabelas.Conquistas.Length, linhas.Count(l => Regex.IsMatch(l[1], "^([ds][0-3]|c)_")), "nada a mais no MISSOES.md");
        }

        // ------------------------------------------------------------------ a virada

        [Test]
        public void Virada_DiariaNoDiaDoJogo_SemanalNaSemana()
        {
            var s = new Sim(null, 5);
            s.Tick(Sim.Step);
            Assert.AreEqual((0, 0), (s.DiaJogo, s.SemanaJogo));
            string[] dia0 = Ids(s.Diarias);
            s.Semanais[0].Conta = 1;
            long diaPassos = (long)s.OndaPassos * s.T.DiaOndas;

            s.Steps = diaPassos - 2;
            s.Tick(Sim.Step);
            CollectionAssert.AreEqual(dia0, Ids(s.Diarias), "ainda e' o mesmo dia");
            s.Tick(Sim.Step);
            s.Tick(Sim.Step);
            Assert.AreEqual((1, 0), (s.DiaJogo, s.SemanaJogo));
            CollectionAssert.AreNotEqual(dia0, Ids(s.Diarias), "dia novo, lista nova");
            Assert.AreEqual(1, s.Semanais[0].Conta, "a semanal continua");

            s.Steps = diaPassos * Sim.SemanaDias;
            s.Tick(Sim.Step);
            Assert.AreEqual((7, 1), (s.DiaJogo, s.SemanaJogo));
            Assert.AreEqual(0, s.Semanais[0].Conta, "semana nova, contador novo");
        }

        [Test]
        public void Lista_SaiDaSemente_MesmaSementeMesmaLista()
        {
            string[] Lista(int seed)
            {
                var s = new Sim(null, seed);
                s.Tick(Sim.Step);
                return Ids(s.Diarias).Concat(Ids(s.Semanais)).ToArray();
            }
            CollectionAssert.AreEqual(Lista(9), Lista(9));
            Assert.Greater(Enumerable.Range(1, 30).Select(x => string.Join(",", Lista(x))).Distinct().Count(), 3, "a semente varia a lista");
        }

        /// <summary>O que nao da para fazer agora fica de fora: estacao fechada, rota de outro aeroporto ou ainda fechada, catalogo no fim.</summary>
        [Test]
        public void Lista_SoEntraOQueDaParaFazerAqui()
        {
            Missao[] Lista(Objetivo o, Action<Sim> arruma = null)
            {
                var s = new Sim(Com(new[] { o }), 1);
                arruma?.Invoke(s);
                s.Tick(Sim.Step);
                return s.Diarias;
            }
            Assert.IsNull(Lista(Obj(Meta.Atendidos, 1, (int)Estacao.Bagagem))[0], "bagagem abre no marco 1");
            Assert.IsNull(Lista(Obj(Meta.Checagens))[0], "oficina abre no marco 2");
            Assert.IsNull(Lista(Obj(Meta.Voos, 1, 2))[0], "rota fechada");
            Assert.IsNull(Lista(Obj(Meta.Voos, 1, 4))[0], "rota de outro aeroporto");
            Assert.IsNotNull(Lista(Obj(Meta.Voos, 1, 0))[0], "rota aberta aqui");
            Assert.IsNotNull(Lista(Obj(Meta.Melhorias, 3))[0]);
            Assert.IsNull(Lista(Obj(Meta.Melhorias, 3), s => { foreach (Station e in s.Estacoes) { e.Vel = s.T.VelNiveis; e.Cap = s.T.CapNiveis; } })[0], "catalogo no fim");
            Objetivo marco1 = Obj(Meta.Embarcados);
            marco1.Marco = 1;
            Assert.IsNull(Lista(marco1)[0], "linha de outro marco");
        }

        // ------------------------------------------------------------------ contar e pagar

        [Test]
        public void Conclui_NoPassoFixo_PagaFixoEFicaFeita()
        {
            var s = new Sim(Com(new[] { Obj(Meta.Melhorias, 1, -1, 77) }), 1) { Cash = 40 };
            s.Tick(Sim.Step);
            Assert.IsTrue(s.Buy(Estacao.CheckIn, Melhoria.Velocidade));
            Assert.AreEqual((1, false, 0L), (s.Diarias[0].Conta, s.Diarias[0].Feita, s.Cash), "a compra nao conclui: so o passo fixo");
            s.Tick(Sim.Step);
            Evento e = s.Events.Single(x => x.Tipo == Ev.MissaoConcluida);
            Assert.AreEqual((0, 0, 77), (e.A, e.B, e.C));
            Assert.AreEqual((true, 77L), (s.Diarias[0].Feita, s.Cash));
            s.Tick(Sim.Step);
            Assert.IsFalse(s.Events.Any(x => x.Tipo == Ev.MissaoConcluida), "paga uma vez");
            Assert.AreEqual(77L, s.Cash);
        }

        [Test]
        public void Semanal_PedeAsDuasPartes()
        {
            Objetivo o = Obj(Meta.Melhorias, 2, -1, 300);
            o.E = Obj(Meta.Movidos, 1);
            var s = new Sim(Com(Sem, new[] { o }), 1) { Cash = 1000 };
            s.Tick(Sim.Step);
            Assert.IsTrue(s.Buy(Estacao.CheckIn, Melhoria.Velocidade));
            Assert.IsTrue(s.Buy(Estacao.CheckIn, Melhoria.Velocidade));
            s.Tick(Sim.Step);
            Assert.AreEqual((2, 0, false), (s.Semanais[0].Conta, s.Semanais[0].ContaE, s.Semanais[0].Feita), "falta a 2a parte");
            Assert.IsTrue(s.Hire());
            s.St(Estacao.Portao).Estado = Estado.Critico;
            Assert.IsTrue(s.Move(0, Estacao.Portao));
            s.Tick(Sim.Step);
            Assert.IsTrue(s.Semanais[0].Feita);
            Evento e = s.Events.Single(x => x.Tipo == Ev.MissaoConcluida);
            Assert.AreEqual((1, 0, 300), (e.A, e.B, e.C));
        }

        [Test]
        public void Contadores_SeguemOsEventos_EmQualquerAeroporto()
        {
            void Confere(Meta m, Func<Evento, bool> conta, int alvo = 999999)
            {
                Tuning t = Com(new[] { Obj(m, alvo) });
                t.AeroportoInicial = 1;
                var s = new Sim(t, 3);
                var bot = new Bot(new BotProfile { Name = "x", Decide = 1f, Move = true, Planeja = true, Equipe = true });
                int n = 0;
                for (int i = 0; i < 7 * s.OndaPassos; i++)
                {
                    bot.Step(s);
                    n += s.Events.Count(conta);
                }
                Assert.Greater(n, 0, m + ": entrou em jogo");
                Assert.AreEqual(n, s.Diarias[0].Conta, m.ToString());
            }
            Confere(Meta.Embarcados, e => e.Tipo == Ev.Embarcou);
            Confere(Meta.Voos, e => e.Tipo == Ev.VooPartiu && e.B > 0);
            Confere(Meta.Cheios, e => e.Tipo == Ev.VooPartiu && e.C > 0 && e.B * 100 >= e.C * Tuning.Padrao.LotadoPct);
            Confere(Meta.Melhorias, e => e.Tipo == Ev.Melhorou, 28);
        }

        /// <summary>Com Arg, so aquela rota conta: as 2 vagas em Campinas nao andam a missao nem a conquista de Ribeirao Preto.</summary>
        [Test]
        public void SoARotaPedidaConta()
        {
            var s = new Sim(Com(new[] { Obj(Meta.Voos, 999, 1) }, null, new[] { Obj(Meta.Voos, 999, 0), Obj(Meta.Voos, 999, 1) }), 1);
            Assert.IsTrue(s.Programa(1, 0));
            BotProfile p = BotProfile.Bom;
            p.Rotas = p.Viaja = false;
            Joga(s, p, 3);
            Assert.AreEqual(0, s.Diarias[0].Conta, "missao de outra rota");
            Assert.AreEqual(0, s.ProgressoConquista(1), "conquista de outra rota");
            Assert.Greater(s.ProgressoConquista(0), 2);
        }

        [Test]
        public void RegraDesligada_ContaEMostra_NaoPaga()
        {
            Tuning t = Com(new[] { Obj(Meta.Melhorias, 1, -1, 500) });
            t.RegraMissoes = false;
            var s = new Sim(t, 1) { Cash = 40 };
            s.Tick(Sim.Step);
            s.Buy(Estacao.CheckIn, Melhoria.Velocidade);
            s.Tick(Sim.Step);
            Assert.AreEqual((true, 0L), (s.Diarias[0].Feita, s.Cash));
            Assert.AreEqual(0, s.Events.Single(e => e.Tipo == Ev.MissaoConcluida).C);
        }

        // ------------------------------------------------------------------ conquistas

        [Test]
        public void Conquista_DeEstado_LeOJogo_DeContador_ContaSempre()
        {
            Tuning t = Com(Sem, null, new[] { Obj(Meta.Equipe, 1), Obj(Meta.Passageiros, 5), Obj(Meta.Melhorias, 2) });
            var s = new Sim(t, 1) { Cash = 100, TotalEmbarcados = 4 };
            s.Tick(Sim.Step);
            Assert.AreEqual(0, s.ConquistasFeitas);
            Assert.AreEqual((0, 4, 0), (s.ProgressoConquista(0), s.ProgressoConquista(1), s.ProgressoConquista(2)));
            s.Hire();
            s.TotalEmbarcados = 5;
            s.Buy(Estacao.CheckIn, Melhoria.Velocidade);
            s.Tick(Sim.Step);
            Assert.AreEqual((true, true, false), (s.ConquistaFeita(0), s.ConquistaFeita(1), s.ConquistaFeita(2)));
            Assert.AreEqual(1, s.ProgressoConquista(2));
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, s.Events.Where(e => e.Tipo == Ev.ConquistaFeita).Select(e => e.A));
            s.Tick(Sim.Step);
            Assert.IsFalse(s.Events.Any(e => e.Tipo == Ev.ConquistaFeita), "uma vez");
        }

        [Test]
        public void Conquista_RotasCampanhaEMissoes()
        {
            Tuning t = Com(new[] { Obj(Meta.Melhorias, 1, -1, 0) }, null, new[] { Obj(Meta.Rotas, 2), Obj(Meta.Campanha, 1), Obj(Meta.Missoes, 1) });
            t.Objetivos[0] = new[] { Obj(Meta.Equipe, 1, -1, 0) };
            var s = new Sim(t, 1) { Cash = 60 };
            s.Tick(Sim.Step);
            Assert.AreEqual(2, s.ProgressoConquista(0), "as 2 rotas iniciais");
            Assert.IsTrue(s.ConquistaFeita(0));
            s.Hire();
            s.Buy(Estacao.CheckIn, Melhoria.Velocidade);
            s.Tick(Sim.Step);
            s.Tick(Sim.Step);
            Assert.AreEqual((true, true), (s.ConquistaFeita(1), s.ConquistaFeita(2)));
        }

        /// <summary>A sequencia recua, a conquista nao: feita uma vez, feita para sempre.</summary>
        [Test]
        public void Conquista_Seguidos_FicaFeitaDepoisDeQuebrar()
        {
            Sim s = new Sim(Com(Sem, null, new[] { Obj(Meta.Seguidos, 2) }), 1);
            var bot = new Bot(BotProfile.Lento);
            bool quebrou = false;
            for (int i = 0; i < 20 * s.OndaPassos && !quebrou; i++)
            {
                bot.Step(s);
                quebrou = s.ConquistaFeita(0) && s.Events.Any(e => e.Tipo == Ev.VooPartiu && e.C > 0 && e.B * 100 < e.C * s.T.LotadoPct);
            }
            Assert.IsTrue(quebrou, "fechou e depois quebrou");
            Assert.AreEqual((true, 2), (s.ConquistaFeita(0), s.ProgressoConquista(0)));
        }

        // ------------------------------------------------------------------ reproduzivel

        [Test]
        public void Reproduzivel_ComESemFecharEAbrir()
        {
            foreach (BotProfile p in new[] { BotProfile.Missionario, BotProfile.Bom })
            {
                Bot.Resultado a = Bot.Medir(p, 2, 24), b = Bot.Medir(p, 2, 24), c = Bot.Medir(p, 2, 24, null, true);
                Assert.Greater(a.Diarias, 3, p.Name);
                Assert.AreEqual((a.Diarias, a.Semanais, a.PagoMissoes, a.Conquistas), (b.Diarias, b.Semanais, b.PagoMissoes, b.Conquistas), p.Name + ": mesma semente");
                Assert.AreEqual((a.Diarias, a.Semanais, a.PagoMissoes, a.Conquistas, a.Receita), (c.Diarias, c.Semanais, c.PagoMissoes, c.Conquistas, c.Receita), p.Name + ": fechar e abrir nao muda");
                CollectionAssert.AreEqual(a.ConquistaEm, c.ConquistaEm, p.Name);
            }
        }

        [Test]
        public void EnumDoEvento_SoCresceNoFim()
        {
            string[] ev = Enum.GetNames(typeof(Ev));
            Assert.AreEqual(new[] { "ObjetivoConcluido", "MissaoConcluida", "ConquistaFeita" }, ev.Skip(Array.IndexOf(ev, "ObjetivoConcluido")).Take(3).ToArray());
        }
    }
}
