using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>
    /// Frente H (GDD §4.1 linha H, Direcao §8.3): quem chega desce com o desembarque do patio, espera a mala numa esteira com capacidade, tira a
    /// mala na ordem da esteira e sai. Sem Cash: a chegada nao mexe no TA-1 nem no TA-2 (Chegada_NaoMexeNaEconomia).
    /// </summary>
    public class ChegadaTests
    {
        const long T0 = 1700000000;

        static Sim Jogado(float seg, int seed = 7, Tuning t = null, Action<Sim> cada = null)
        {
            var s = new Sim(t, seed);
            var b = new Bot(BotProfile.Bom);
            for (int i = 0; i < (int)(seg * Sim.Hz); i++) { b.Step(s); cada?.Invoke(s); }
            return s;
        }

        /// <summary>Um passo do nucleo por vez (o Tick acumula: confere que andou exatamente um).</summary>
        static void UmPasso(Sim s)
        {
            long antes = s.Steps;
            s.Tick(Sim.Step);
            if (s.Steps == antes) s.Tick(1e-4f);
            Assert.AreEqual(antes + 1, s.Steps, "um passo por Tick");
        }

        static void Anda(Sim s, float seg) { for (int i = 0; i < (int)(seg * Sim.Hz); i++) UmPasso(s); }

        /// <summary>Anda ate o 1o pouso com gente do aviao `a` (Ev.Pousou e alguem dele chegando).</summary>
        static void AtePouso(Sim s, int a, float teto = 400f)
        {
            for (int i = 0; i < (int)(teto * Sim.Hz); i++)
            {
                UmPasso(s);
                if (s.Events.Any(e => e.Tipo == Ev.Pousou && e.A == a) && s.Chegantes.Any(c => c.Aviao == a)) return;
            }
            Assert.Fail("o aviao " + a + " nao pousou com gente");
        }

        static string SemChegada(string save) =>
            string.Join("\n", save.Split('\n').Where(l => !(l.StartsWith("cheg=") || l.StartsWith("chav.") || l.StartsWith("esteira=") || l.StartsWith("carrinho=") || l.StartsWith("sha256="))));

        /// <summary>As invariantes da chegada: cada mala fora do aviao esta em um lugar so (esteira ou carrinho) e so quem pegou esta sem ela
        /// nos dois; ninguem divide lugar em volta da esteira; o tempo que falta casa com o estado.</summary>
        static void Confere(Sim s)
        {
            Assert.AreEqual(s.T.EsteiraCap, s.Esteira.Length, "lugares da esteira");
            var ids = new HashSet<int>();
            foreach (Chegante c in s.Chegantes) Assert.IsTrue(ids.Add(c.Id), "Id repetido " + c.Id);
            var malas = new List<int>(s.Carrinho);
            malas.AddRange(s.Esteira.Where(x => x >= 0));
            Assert.AreEqual(malas.Count, malas.Distinct().Count(), "mala em dois lugares");
            var vagas = new HashSet<int>();
            foreach (Chegante c in s.Chegantes)
            {
                bool fora = malas.Contains(c.Id);
                Assert.AreEqual(c.MalaSolta && c.Estado != EstadoChegada.Pegando, fora, "mala de " + c.Id + " (" + c.Estado + ") fora do lugar");
                Assert.IsTrue(c.Mala || !c.MalaSolta, "mala que nao existe");
                Assert.IsTrue(c.Mala || c.Estado <= EstadoChegada.Andando, "sem mala nao espera nem pega");
                bool temVaga = c.Mala && c.Estado != EstadoChegada.NoAviao;
                Assert.AreEqual(temVaga, c.Vaga >= 0, "vaga de " + c.Id + " (" + c.Estado + ")");
                if (temVaga) Assert.IsTrue(vagas.Add(c.Vaga), "dois no mesmo lugar em volta da esteira");
                bool anda = c.Estado == EstadoChegada.Andando || c.Estado == EstadoChegada.Pegando;
                Assert.AreEqual(anda, c.Resto > 0, "tempo que falta de " + c.Id + " (" + c.Estado + ")");
            }
            foreach (int id in malas) Assert.IsTrue(ids.Contains(id), "mala sem dono " + id);
        }

        [Test]
        public void Chegada_NaoMexeNaEconomia()
        {
            var sem = new Tuning { ChegadaFator = 0f };
            Sim a = Jogado(1500f, 11), b = Jogado(1500f, 11, sem);
            Assert.AreEqual((b.Cash, b.TotalEmbarcados, b.TotalPerdidos, b.Marco), (a.Cash, a.TotalEmbarcados, a.TotalPerdidos, a.Marco), "a chegada nao muda o jogo");
            Assert.AreEqual(SemChegada(b.Save("t", T0)), SemChegada(a.Save("t", T0)), "o resto do save e o mesmo");
        }

        [Test]
        public void Invariantes_EmTodoPasso_ENinguemFicaParaSempre()
        {
            foreach (int seed in new[] { 1, 7, 23 })
            {
                int pegaram = 0, semMala = 0, maxCheg = 0;
                var vistos = new HashSet<int>();
                Jogado(2200f, seed, null, s =>
                {
                    Confere(s);
                    foreach (Chegante c in s.Chegantes)
                    {
                        if (c.Estado == EstadoChegada.Pegando && vistos.Add(c.Id)) pegaram++;
                        if (!c.Mala && vistos.Add(c.Id)) semMala++;
                        long pouso = (c.Id / 64) >> 1;
                        Assert.Less(s.Steps - pouso, 2L * s.OndaPassos, "quem chegou sai antes de 2 ondas (" + c.Estado + ")");
                    }
                    maxCheg = Math.Max(maxCheg, s.Chegantes.Count);
                });
                Assert.Greater(pegaram, 20, "gente pegou a mala");
                Assert.Greater(semMala, 3, "e gente sem mala passou direto");
                Assert.LessOrEqual(maxCheg, 2 * new Tuning().ChegadaMax, "no maximo 2 avioes chegando");
            }
        }

        [Test]
        public void VooQueChega_TrazPassageirosEMalas_EDesceComODesembarque()
        {
            var s = new Sim(null, 5);
            AtePouso(s, 0);
            Aviao a = s.Avioes[0];
            List<Chegante> d = s.Chegantes.Where(c => c.Aviao == 0).ToList();
            int n = Math.Min(s.T.ChegadaMax, (int)Math.Round(s.T.Pax(s.Onda) * (double)s.T.ChegadaFator));
            Assert.AreEqual(n, d.Count, "chegam ChegadaFator x os passageiros da onda");
            Assert.IsTrue(d.All(c => c.Estado == EstadoChegada.NoAviao && !c.MalaSolta), "no pouso, todos a bordo");
            Assert.Greater(d.Count(c => c.Mala), 0, "alguem traz mala");
            int antes = 0;
            while (a.Falta[(int)Servico.Desembarque] > 0)
            {
                UmPasso(s);
                int desceram = d.Count(c => c.Estado != EstadoChegada.NoAviao);
                Assert.GreaterOrEqual(desceram, antes, "so desce, nunca volta");
                antes = desceram;
                Assert.IsFalse(d.Any(c => c.MalaSolta), "mala so sai com o servico de bagagem");
            }
            UmPasso(s);
            Assert.IsTrue(d.All(c => c.Estado != EstadoChegada.NoAviao), "desembarque feito = todos desceram");
            Assert.IsTrue(d.Where(c => c.Mala).All(c => c.Vaga >= 0), "quem tem mala reservou lugar na esteira");
            while (a.Falta[(int)Servico.Bagagem] > 0) UmPasso(s);
            UmPasso(s);
            Assert.IsTrue(d.Where(c => c.Mala).All(c => c.MalaSolta), "bagagem feita = todas as malas fora do aviao");
            Confere(s);
        }

        [Test]
        public void Esteira_TemCapacidade_OCarrinhoEsperaNaOrdem()
        {
            // donos que nunca chegam: as malas enchem a esteira e o resto espera no carrinho, na ordem
            var t = new Tuning { EsteiraCap = 3, ChegadaAndar = new[] { 900f, 900f }, ChegadaMalaChance = 1f, PatioIniciais = 2 };
            var s = new Sim(t, 3);
            AtePouso(s, 0);
            while (s.Avioes[0].Falta[(int)Servico.Bagagem] > 0) UmPasso(s);
            Anda(s, 5f);
            List<int> dono = s.Chegantes.Where(c => c.Aviao == 0).Select(c => c.Id).ToList();
            Assert.Greater(dono.Count, 3);
            Assert.AreEqual(3, s.Esteira.Count(x => x >= 0), "esteira cheia: 3 lugares");
            CollectionAssert.AreEqual(dono.Skip(3), s.Carrinho.Where(dono.Contains).ToList(), "o resto espera no carrinho, na ordem em que saiu do aviao");
            CollectionAssert.AreEquivalent(dono.Take(3), s.Esteira.Where(x => x >= 0), "as 3 primeiras estao na esteira");
            Anda(s, 30f);
            Assert.AreEqual(3, s.Esteira.Count(x => x >= 0), "ninguem pega: a esteira so gira");
            Confere(s);
        }

        [Test]
        public void MalaSaiNaOrdemDaEsteira_QuemPegouVaiEmbora()
        {
            var s = new Sim(null, 9);
            int pegaram = 0, sairam = 0;
            var pegando = new Dictionary<int, long>();
            for (int i = 0; i < 900 * Sim.Hz; i++)
            {
                int[] esteira = (int[])s.Esteira.Clone();
                long giro = s.Steps / s.EsteiraPassos;
                bool volta = s.Steps % s.EsteiraPassos == 0;
                var antes = s.Chegantes.ToDictionary(c => c.Id, c => (c.Estado, c.Vaga));
                UmPasso(s);
                foreach (Chegante c in s.Chegantes)
                {
                    if (c.Estado != EstadoChegada.Pegando || !antes.TryGetValue(c.Id, out var a) || a.Estado == EstadoChegada.Pegando) continue;
                    Assert.AreEqual(EstadoChegada.Esperando, a.Estado, "so pega quem esta esperando em volta da esteira");
                    Assert.IsTrue(volta, "pega quando a esteira anda um lugar");
                    int j = Array.IndexOf(esteira, c.Id);
                    Assert.GreaterOrEqual(j, 0, "a mala estava na esteira");
                    Assert.AreEqual(c.Vaga % esteira.Length, (int)((j + giro) % esteira.Length), "a mala passou em frente a quem pegou");
                    pegando[c.Id] = s.Steps;
                    pegaram++;
                }
                foreach (int id in antes.Keys)
                    if (!s.Chegantes.Any(c => c.Id == id) && pegando.TryGetValue(id, out long desde))
                    {
                        Assert.AreEqual(s.T.PegarSeg * Sim.Hz, s.Steps - desde, 1.01, "tirou a mala e foi embora");
                        sairam++;
                    }
            }
            Assert.Greater(pegaram, 10);
            Assert.Greater(sairam, 10);
        }

        [Test]
        public void Boca_PoeAPrimeiraDoCarrinho_NoLugarVazio()
        {
            // a esteira lenta (6 s por lugar) junta malas no carrinho: a ordem dele aparece
            foreach (Tuning tu in new[] { new Tuning { PatioIniciais = 2 }, new Tuning { EsteiraSeg = 6f } })
            {
            var s = new Sim(tu, 4);
            int entraram = 0, fila = 0;
            for (int i = 0; i < 600 * Sim.Hz; i++)
            {
                fila = Math.Max(fila, s.Carrinho.Count);
                var carrinho = new List<int>(s.Carrinho);
                var soltas = new HashSet<int>(s.Chegantes.Where(c => c.MalaSolta).Select(c => c.Id));
                int[] esteira = (int[])s.Esteira.Clone();
                long giro = s.Steps / s.EsteiraPassos;
                UmPasso(s);
                carrinho.AddRange(s.Chegantes.Where(c => c.MalaSolta && !soltas.Contains(c.Id)).Select(c => c.Id));   // as que sairam do aviao neste passo vao para o fim
                foreach (int id in s.Esteira.Where(x => x >= 0 && Array.IndexOf(esteira, x) < 0))
                {
                    Assert.AreEqual(carrinho[0], id, "entra a 1a do carrinho");
                    int j = Array.IndexOf(s.Esteira, id), n = esteira.Length;
                    Assert.AreEqual(0, (int)((j + giro) % n), "pela boca (posicao 0)");
                    int antes = esteira[j];
                    Assert.IsTrue(antes < 0 || s.Chegantes.Any(c => c.Id == antes && c.Estado == EstadoChegada.Pegando), "num lugar vazio (ou que o dono esvaziou neste passo)");
                    entraram++;
                }
            }
            Assert.Greater(entraram, 20);
            if (tu.EsteiraSeg > 1f) Assert.Greater(fila, 2, "o carrinho juntou malas");
            }
        }

        [Test]
        public void HangarNoMeioDoDesembarque_TodosDescem_RetornoEHangarNaoTrazemNinguem()
        {
            var s = new Sim(new Tuning { HangarCusto = 0 }, 6);
            AtePouso(s, 0);
            Anda(s, 2f);
            Assert.IsTrue(s.Hangar(0), "o hangar vale antes do embarque");
            UmPasso(s);
            List<Chegante> d = s.Chegantes.Where(c => c.Aviao == 0).ToList();
            Assert.IsTrue(d.All(c => c.Estado != EstadoChegada.NoAviao), "o aviao saiu do patio: todos ja desceram");
            Assert.IsTrue(d.Where(c => c.Mala).All(c => c.MalaSolta), "e as malas ja estao fora");
            int ids = s.Chegantes.Count(c => c.Aviao == 0);
            while (s.Avioes[0].Hangar > 0) UmPasso(s);
            UmPasso(s);
            Assert.LessOrEqual(s.Chegantes.Count(c => c.Aviao == 0), ids, "do hangar o aviao volta vazio");
            Anda(s, 120f);
            Confere(s);
            Assert.IsFalse(s.Chegantes.Any(c => d.Exists(x => x.Id == c.Id)), "e todo mundo daquele pouso ja saiu");
        }

        [Test]
        public void SaveNoMeioDaChegada_ContinuaIgual_EIdaEVoltaEstavel()
        {
            Sim s = Jogado(1, 7);
            var b0 = new Bot(BotProfile.Bom);
            for (int i = 0; i < 2000 * Sim.Hz && !(s.Carrinho.Count > 0 && s.Esteira.Any(x => x >= 0)); i++) b0.Step(s);
            Assert.IsTrue(s.Carrinho.Count > 0 && s.Esteira.Any(x => x >= 0), "no meio: malas na esteira e no carrinho");
            string a = s.Save("t", T0);
            StringAssert.Contains("\ncheg=", a);
            StringAssert.Contains("\nesteira=", a);
            Sim b = Sim.Load(a);
            Assert.IsFalse(b.SaveRefused);
            Assert.AreEqual(a, b.Save("t", T0), "Load(Save(x)).Save == Save(x)");
            Confere(b);
            var b1 = new Bot(BotProfile.Bom); var b2 = new Bot(BotProfile.Bom);
            for (int i = 0; i < 300 * Sim.Hz; i++) { b1.Step(s); b2.Step(b); }
            Assert.AreEqual(s.Save("t", T0 + 1), b.Save("t", T0 + 1), "reabrir no meio da chegada da o mesmo jogo");
        }

        [Test]
        public void SaveAdulterado_LimpaSemPerderMala()
        {
            Sim s = Jogado(1, 7);
            var bot = new Bot(BotProfile.Bom);
            for (int i = 0; i < 2000 * Sim.Hz && !(s.Carrinho.Count > 0 && s.Esteira.Count(x => x >= 0) > 1); i++) bot.Step(s);
            string txt = SaveEnvelopeTexto(s.Save("t", T0));
            Chegante x = s.Chegantes.First(c => c.Mala && c.MalaSolta && c.Estado != EstadoChegada.Pegando);
            Chegante semMala = new Chegante { Id = 999999, Aviao = 0 };
            string lixo = txt.Replace("\ncarrinho=", "\ncarrinho=abc,-5," + x.Id + "," + x.Id + ",")   // repetida, lixo e uma que ja esta na esteira
                .Replace("\ncheg=", "\ncheg=" + x.Id + ":0:2:0:0:1;" + "123:9:9:9:9:9;" + "64:1:3:5:0:1;");   // Id repetido, aviao e estado fora da faixa, sem mala pegando
            lixo = lixo.Replace("\nesteira=", "\nesteira=" + semMala.Id + ",");
            Sim b = Sim.Load(SaveEnvelope.Wrap(lixo, "t", T0));
            Assert.IsFalse(b.SaveRefused);
            Confere(b);
            string semCarrinho = string.Join("\n", txt.Split('\n').Where(l => !l.StartsWith("carrinho=")));
            Sim c2 = Sim.Load(SaveEnvelope.Wrap(semCarrinho, "t", T0));
            Confere(c2);
            Assert.AreEqual(s.Carrinho.Count, c2.Carrinho.Count, "a mala que o save perdeu volta para o carrinho");
            var bb = new Bot(BotProfile.Bom);
            for (int i = 0; i < 400 * Sim.Hz; i++) { bb.Step(b); if (i % 30 == 0) Confere(b); }
        }

        /// <summary>O payload do save (sem o cabecalho do envelope), para adulterar.</summary>
        static string SaveEnvelopeTexto(string save) => SaveEnvelope.Unwrap(save).Payload;

        [Test]
        public void SchemaSemChegada_AbreSemNinguemChegando()
        {
            Sim s = Jogado(300f, 7);
            string payload = SemChegada(SaveEnvelopeTexto(s.Save("t", T0)));
            Sim b = Sim.Load(SaveEnvelope.Wrap(payload, "t", T0));
            Assert.IsFalse(b.SaveRefused);
            Assert.AreEqual(0, b.Chegantes.Count + b.Carrinho.Count + b.Esteira.Count(x => x >= 0));
            var bot = new Bot(BotProfile.Bom);
            int chegaram = 0;
            for (int i = 0; i < 400 * Sim.Hz; i++) { bot.Step(b); chegaram = Math.Max(chegaram, b.Chegantes.Count); }
            Confere(b);
            Assert.Greater(chegaram, 0, "o pouso seguinte ja traz gente");
        }
    }
}
