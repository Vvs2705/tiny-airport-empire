using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Passe de temporada, trilha gratuita (CONTRATO_FASE_C7 §2): de onde vem o ponto, o nivel que paga sozinho, a virada e a tabela igual ao docs/PASSE.md.</summary>
    public class PasseTests
    {
        const string Bandeirolas = "terminal_decor_flags_s1";

        /// <summary>A tabela com outras temporadas e o resto do jogo de sempre.</summary>
        internal static Tuning Com(params Temporada[] ts) => new Tuning { Temporadas = ts, RegraDespesas = false };   // a conta do dia: DespesasTests

        internal static Temporada Curta(string id, int dias) => new Temporada { Id = id, Produto = "season_pass_" + id, Dias = dias, Gratis = Tabelas.Temporadas[0].Gratis };

        /// <summary>Joga ate o passo `ate` com o bot e devolve os eventos de passe de cada fonte.</summary>
        static (int voos, int diarias, int semanais, int objetivos) Joga(Sim s, Bot b, long ate)
        {
            int voos = 0, diarias = 0, semanais = 0, objetivos = 0;
            while (s.Steps < ate)
            {
                b.Step(s);
                foreach (Evento e in s.Events)
                {
                    if (e.Tipo == Ev.VooPartiu && e.C > 0) voos++;
                    if (e.Tipo == Ev.MissaoConcluida) { if (e.A == 0) diarias++; else semanais++; }
                    if (e.Tipo == Ev.ObjetivoConcluido) objetivos++;
                }
            }
            return (voos, diarias, semanais, objetivos);
        }

        [Test]
        public void Tabela_30NiveisFixos_CosmeticoUnico()
        {
            foreach (Temporada t in Tabelas.Temporadas)
            {
                Assert.AreEqual((30, 28), (t.Gratis.Length, t.Dias), t.Id);
                Produto p = Array.Find(Tabelas.Produtos, x => x.Id == t.Produto);
                Assert.IsTrue(p != null && p.Tipo == TipoProduto.NaoConsumivel, "o premium e' um produto nao consumivel da loja: " + t.Produto);
                Assert.IsTrue(t.Gratis.All(n => n.Valor > 0), "nivel sem premio");
                Assert.IsTrue(t.Gratis.Where(n => n.Premio == Premio.Tempo).All(n => n.Valor <= 15 * 60), "tempo e' recompensa de baixo valor (GDD §7.2)");
                Assert.IsTrue(t.Gratis.Where(n => n.Premio != Premio.Cosmetico).All(n => n.Id == ""));
            }
            string[] cosmeticos = Tabelas.Temporadas.SelectMany(t => t.Gratis).Where(n => n.Premio == Premio.Cosmetico).Select(n => n.Id).ToArray();
            Assert.AreEqual(cosmeticos.Length, cosmeticos.Distinct().Count(), "cosmetico repetido");
            Assert.IsFalse(cosmeticos.Any(c => c == "" || Array.Exists(Tabelas.Produtos, p => p.Id == c)), "cosmetico do passe nao se vende na loja");
            Assert.AreEqual(Tabelas.Temporadas.Length, Tabelas.Temporadas.Select(t => t.Id).Distinct().Count());
        }

        /// <summary>docs/PASSE.md e' a lista que o Vinicius le: tem de ser a tabela do codigo, linha a linha.</summary>
        [Test]
        public void PasseMd_EATabela_LinhaALinha()
        {
            string dir = TestContext.CurrentContext.TestDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "docs", "PASSE.md"))) dir = Path.GetDirectoryName(dir);
            if (dir == null) Assert.Ignore("sem o docs/ do repositorio aqui");
            string[][] linhas = File.ReadAllLines(Path.Combine(dir, "docs", "PASSE.md")).Where(l => l.StartsWith("| ")).Select(l => l.Split('|').Select(c => c.Trim()).ToArray())
                .Where(l => int.TryParse(l[1], out _)).ToArray();
            Nivel[] g = Tabelas.Temporadas[0].Gratis;
            Assert.AreEqual(g.Length, linhas.Length, "um nivel por linha no PASSE.md");
            for (int i = 0; i < g.Length; i++)
            {
                string esperado = g[i].Premio == Premio.Cash ? g[i].Valor + " Cash"
                    : g[i].Premio == Premio.Tempo ? g[i].Valor / 60 + " min de 2×" : "`" + g[i].Id + "`";
                Assert.AreEqual((i + 1).ToString(), linhas[i][1]);
                Assert.AreEqual(esperado, linhas[i][2], "nivel " + (i + 1));
            }
        }

        /// <summary>Ponto de temporada = voo que partiu (com ou sem gente), diaria, semanal e objetivo da campanha, nos valores do Tuning.</summary>
        [Test]
        public void Pontos_SaemDoVooDaMissaoEDaCampanha()
        {
            var s = new Sim(null, 4);
            var t = s.T;
            var (voos, diarias, semanais, objetivos) = Joga(s, new Bot(BotProfile.Completo), 20L * s.OndaPassos);
            Assert.IsTrue(voos > 0 && diarias > 0 && semanais > 0 && objetivos > 0, "as 4 fontes aconteceram");
            int pontos = voos * t.PasseVoo + diarias * t.PasseDiaria + semanais * t.PasseSemanal + objetivos * t.PasseObjetivo;
            Assert.AreEqual(Math.Min(30 * t.PassePontosNivel, pontos), s.PontosPasse);
            Assert.AreEqual(Math.Min(30, pontos / t.PassePontosNivel), s.NivelPasse);
        }

        /// <summary>O nivel paga sozinho no passo fixo em que fecha, uma vez, na ordem da tabela e no valor dela.</summary>
        [Test]
        public void Nivel_PagaSozinhoUmaVez_NoValorDaTabela()
        {
            var s = new Sim(null, 1) { PontosPasse = 3 * 100 + 99 };
            s.Tick(Sim.Step);
            Evento[] ev = s.Events.Where(e => e.Tipo == Ev.PasseNivel).ToArray();
            Assert.AreEqual("0:1:200,0:2:300,0:3:300", string.Join(",", ev.Select(e => e.A + ":" + e.B + ":" + e.C)));
            Assert.AreEqual((500L, 300 * Sim.Hz - 1, 3), (s.Cash, s.BoostLeft, s.NivelPasse), "200 + 300 Cash e 5 min de 2x");
            s.Tick(Sim.Step);
            Assert.IsFalse(s.Events.Any(e => e.Tipo == Ev.PasseNivel), "nao paga de novo");
            s.PontosPasse += 1;
            s.Tick(Sim.Step);
            Assert.AreEqual((4, 900L), (s.Events.Single(e => e.Tipo == Ev.PasseNivel).B, s.Cash), "o nivel 4 paga 400");
        }

        [Test]
        public void Cosmetico_FicaNaPosse_EVaiNoSave()
        {
            var s = new Sim(null, 1) { PontosPasse = 1000 };
            Assert.IsFalse(s.Tem(Bandeirolas));
            s.Tick(Sim.Step);
            Assert.IsTrue(s.Tem(Bandeirolas), "nivel 10");
            Sim r = Sim.Load(s.Save("t", 1));
            Assert.AreEqual((true, 10, 1000), (r.Tem(Bandeirolas), r.NivelPasse, r.PontosPasse));
            r.Tick(Sim.Step);
            Assert.IsFalse(r.Events.Any(e => e.Tipo == Ev.PasseNivel), "reabrir nao paga de novo");
        }

        /// <summary>O tempo soma ao 2x que ja corre (o booster das Gems), sem perder nada.</summary>
        [Test]
        public void Tempo_SomaAo2xQueCorre()
        {
            var s = new Sim(null, 1) { PontosPasse = 200, BoostLeft = 1800 * Sim.Hz };
            s.Tick(Sim.Step);
            Assert.AreEqual((1800 + 300) * Sim.Hz - 1, s.BoostLeft);
            Assert.AreEqual((1800 + 300) * Sim.Hz - 1, Sim.Load(s.Save("t", 1)).BoostLeft, "reabrir nao corta");
        }

        [Test]
        public void RegraDesligada_ContaEMostra_NaoPaga()
        {
            var s = new Sim(new Tuning { RegraPasse = false }, 1) { PontosPasse = 1000 };
            s.Tick(Sim.Step);
            Assert.AreEqual((10, 0L, 0, false), (s.NivelPasse, s.Cash, s.BoostLeft, s.Tem(Bandeirolas)));
            Assert.IsTrue(s.Events.Where(e => e.Tipo == Ev.PasseNivel).All(e => e.C == 0));
            Assert.AreEqual(10, s.Events.Count(e => e.Tipo == Ev.PasseNivel));
            var b = new Sim(new Tuning { RegraPasse = false }, 4);
            Joga(b, new Bot(BotProfile.Completo), 10L * b.OndaPassos);
            Assert.Greater(b.PontosPasse, 0, "conta");
        }

        /// <summary>No fim dos dias do jogo a temporada vira: os pontos zeram, Cash e cosmetico ficam. Sem a proxima na tabela, o passe fecha.</summary>
        [Test]
        public void Temporada_ViraNoFimDosDias_EFechaSemAProxima()
        {
            var s = new Sim(Com(Curta("s1", 1)), 1) { PontosPasse = 1000 };
            s.Tick(Sim.Step);
            long cash = s.Cash;
            Assert.AreEqual((1, 10), (s.DiasRestantesPasse, s.NivelPasse));
            long dia = 8L * s.OndaPassos;
            while (s.Steps < dia) s.Tick(Sim.Step);
            Assert.AreEqual("s1", s.Temporada.Id, "o ultimo passo do dia ainda e' da temporada");
            s.Tick(Sim.Step);
            Assert.AreEqual(((Temporada)null, 0, 0, 0), (s.Temporada, s.PontosPasse, s.NivelPasse, s.DiasRestantesPasse), "virou e fechou");
            Assert.AreEqual((true, cash), (s.Tem(Bandeirolas), s.Cash), "Cash e cosmetico ficam");
            while (s.Steps < 3 * dia) s.Tick(Sim.Step);
            Assert.AreEqual(0, s.PontosPasse, "fechado nao conta");

            // uma atualizacao traz a s2: abre no dia em que chega, com os dias inteiros
            Sim n = Sim.Load(s.Save("t", 1), Com(Curta("s1", 1), Curta("s2", 2)), 1);
            Assert.AreEqual(("s2", 2, 0), (n.Temporada.Id, n.DiasRestantesPasse, n.PontosPasse));
            n.PontosPasse = 100;
            n.Tick(Sim.Step);
            Assert.AreEqual((1, 1), (n.Events.Single(e => e.Tipo == Ev.PasseNivel).A, n.Events.Single(e => e.Tipo == Ev.PasseNivel).B));
        }

        [Test]
        public void DuasTemporadas_ASegundaAbreNoDiaEmQueAPrimeiraFecha()
        {
            var s = new Sim(Com(Curta("s1", 1), Curta("s2", 2)), 1) { PontosPasse = 300 };
            long dia = 8L * s.OndaPassos;
            while (s.Steps <= dia) s.Tick(Sim.Step);
            Assert.AreEqual(("s2", 1, 2, 0), (s.Temporada.Id, s.DiaJogo, s.DiasRestantesPasse, s.PontosPasse));
            s.PontosPasse = 100;
            s.Tick(Sim.Step);
            Evento e = s.Events.Single(x => x.Tipo == Ev.PasseNivel);
            Assert.AreEqual((1, 1, 200), (e.A, e.B, e.C), "a s2 paga do nivel 1");
        }

        [Test]
        public void MesmaSemente_MesmoPasse()
        {
            Bot.Resultado a = Bot.Medir(BotProfile.Completo, 9, 24), b = Bot.Medir(BotProfile.Completo, 9, 24);
            Assert.AreEqual((a.Niveis, a.PagoPasse, a.Receita), (b.Niveis, b.PagoPasse, b.Receita));
            Assert.Greater(a.Niveis, 0);
        }
    }
}
