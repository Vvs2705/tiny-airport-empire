using System;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Fase D1 (CONTRATO_FASE_D1): catalogo, credito por id de transacao, o que as Gems compram e as invariantes do §4. A migracao fica no
    /// SaveTests; os portoes, no BalanceTests.</summary>
    public class ComprasTests
    {
        const string Pequeno = "gems_pack_small", Grande = "gems_pack_large", Boas = "welcome_bundle_01", Passe = "season_pass_s1", Pintura = "aircraft_skin_blue_01";
        const int Pacote = 0, Booster = 1, CosmeticoItem = 2;

        static Sim Reabre(Sim s) => Sim.Load(s.Save("t", 1000));

        static Sim ComGems(long gems = 5000, Tuning t = null) => new Sim(t, 1) { Gems = gems };

        /// <summary>Todo o estado do jogo no save, menos o que a compra pode mudar: voos, passageiros, marcos, campanha, avioes e a renda media.</summary>
        static string SoOJogo(Sim s) =>
            string.Join("\n", s.Save("t", 1).Split('\n').Where(l => !new[] { "sha256=", "cash=", "gems=", "bst=", "posse=", "tx=" }.Any(l.StartsWith)));

        static Sim Jogado(BotProfile p, int ondas, int seed = 3)
        {
            var s = new Sim(null, seed);
            var b = new Bot(p);
            for (long i = 0; i < ondas * (long)s.OndaPassos + 900; i++) b.Step(s);
            return s;
        }

        // ------------------------------------------------------------------ catalogo

        [Test]
        public void Catalogo_OsIdsDoGDD_E_NaoConsumiveisNoLugar()
        {
            CollectionAssert.AreEqual(new[] { Boas, Pequeno, "gems_pack_medium", Grande, Passe, Pintura, "terminal_decor_01" }, Tabelas.Produtos.Select(p => p.Id).ToArray());
            CollectionAssert.AreEqual(new[] { Pequeno, "gems_pack_medium", Grande }, Tabelas.Produtos.Where(p => p.Tipo == TipoProduto.Consumivel).Select(p => p.Id).ToArray());
            Assert.IsTrue(Tabelas.Produtos.Where(p => p.Tipo == TipoProduto.Consumivel).All(p => p.Gems > 0 && p.Cash == 0), "consumivel e' pacote de Gems");
        }

        /// <summary>GDD §7.1: preco, moeda e localizacao vem da loja. Numero com casa decimal no catalogo seria preco hardcoded.</summary>
        [Test]
        public void Catalogo_SemPrecoReal()
        {
            foreach (System.Reflection.FieldInfo f in typeof(Produto).GetFields())
            {
                StringAssert.DoesNotContain("reco", f.Name);
                CollectionAssert.Contains(new[] { typeof(string), typeof(TipoProduto), typeof(long), typeof(int) }, f.FieldType, f.Name);
            }
        }

        /// <summary>GDD §6.1: so estes 3 tipos. Tipo novo, como repor voo perdido ou pular a perda, cai aqui.</summary>
        [Test]
        public void Gems_SoCompramOsTresDoGDD()
        {
            CollectionAssert.AreEqual(new[] { "PacoteCash", "Booster", "Cosmetico" }, Enum.GetNames(typeof(TipoItem)));
            Assert.IsTrue(Tabelas.ItensGems.All(i => i.Preco > 0), "preco fixo em Gems");
            foreach (ItemGems i in Tabelas.ItensGems.Where(x => x.Tipo == TipoItem.Cosmetico))
                Assert.AreEqual(TipoProduto.NaoConsumivel, Tabelas.Produtos.Single(p => p.Id == i.Id).Tipo, i.Id);
        }

        // ------------------------------------------------------------------ credito (o ponto do servidor)

        [Test]
        public void Credito_AMesmaTransacaoRendeUmaSo()
        {
            var s = new Sim();
            Assert.AreEqual(Credito.Creditado, s.Creditar("GPA.1", Grande));
            long gems = s.Gems;
            Assert.AreEqual(1200, gems);
            Assert.AreEqual(Credito.Repetido, s.Creditar("GPA.1", Grande), "callback repetido");
            Assert.AreEqual(Credito.Repetido, s.Creditar("GPA.1", Pequeno), "o id manda: pedir outro produto com a mesma transacao nao paga");
            Assert.AreEqual(gems, s.Gems);
            Assert.AreEqual(Credito.Creditado, s.Creditar("GPA.2", Grande), "outra transacao paga");
            Assert.AreEqual(2400, s.Gems);
        }

        [Test]
        public void Credito_ReabrirOJogoNaoPagaDeNovo()
        {
            var s = new Sim();
            s.Creditar("GPA.1", Boas);
            Sim r = Reabre(Reabre(s));
            Assert.AreEqual((Credito.Repetido, s.Gems, s.Cash, true), (r.Creditar("GPA.1", Boas), r.Gems, r.Cash, r.Tem(Boas)));
        }

        /// <summary>GDD §16.6: produto indisponivel e dado invalido nao mudam nada, nem queimam o id.</summary>
        [Test]
        public void Credito_Invalido_NaoMudaNada()
        {
            var s = new Sim();
            foreach (string tx in new[] { null, "", "   " }) Assert.AreEqual(Credito.Invalido, s.Creditar(tx, Grande));
            foreach (string p in new[] { null, "", "gems_pack_giant" }) Assert.AreEqual(Credito.Invalido, s.Creditar("GPA.1", p));
            Assert.AreEqual((0L, 0L), (s.Gems, s.Cash));
            Assert.AreEqual(Credito.Creditado, s.Creditar("GPA.1", Grande));
        }

        /// <summary>GDD §7.3: o pacote de boas-vindas nao paga 2 vezes na mesma conta, nem com outra transacao.</summary>
        [Test]
        public void Credito_NaoConsumivel_UmaVezSo()
        {
            var s = new Sim();
            s.Creditar("GPA.1", Boas);
            long gems = s.Gems, cash = s.Cash;
            Assert.AreEqual(Credito.JaTem, s.Creditar("GPA.2", Boas));
            Assert.AreEqual((gems, cash), (s.Gems, s.Cash));
            Assert.AreEqual(Credito.Repetido, s.Creditar("GPA.2", Boas), "a transacao foi vista");
        }

        /// <summary>GDD §7.3: reinstalar sem save na nuvem traz o conteudo de volta, com o save novo. E' restauracao, nao duplicacao.</summary>
        [Test]
        public void Credito_Restaurar_NoSaveNovo_Volta()
        {
            var s = new Sim();
            s.Creditar("GPA.1", Passe);
            var novo = new Sim();
            Assert.AreEqual(Credito.Creditado, novo.Creditar("GPA.1", Passe));
            Assert.IsTrue(novo.Tem(Passe));
        }

        /// <summary>Save que nao abriu nao grava: creditar perderia a compra. Sem acknowledge, a loja segura (CONTRATO_FASE_D1 §4).</summary>
        [Test]
        public void Credito_ComSaveRecusado_NaoCredita()
        {
            Sim s = Sim.Load("sha256=errado\nschema=1\ncontent=t\nat=1\n");
            Assert.IsTrue(s.SaveRefused);
            Assert.AreEqual(Credito.SaveRecusado, s.Creditar("GPA.1", Grande));
            Assert.AreEqual(0, s.Gems);
        }

        [Test]
        public void Credito_AcimaDoTeto_NaoEngoleACompra()
        {
            var s = new Sim(new Tuning { GemsTeto = 150 }, 1);
            Assert.AreEqual(Credito.Creditado, s.Creditar("a", Pequeno));
            Assert.AreEqual(Credito.Teto, s.Creditar("b", Pequeno));
            Assert.AreEqual(100, s.Gems);
            Assert.IsTrue(s.Gastar(Pacote));
            Assert.AreEqual(Credito.Creditado, s.Creditar("b", Pequeno), "sem anotar o id: depois de gastar, a mesma transacao entra");
        }

        /// <summary>GDD §8.5: o save nao guarda o token de compra.</summary>
        [Test]
        public void Credito_TokenNuncaFicaNoSave()
        {
            var s = new Sim();
            s.Creditar("token-de-compra", Grande);
            StringAssert.DoesNotContain("token-de-compra", s.Save("t", 1));
        }

        // ------------------------------------------------------------------ o que as Gems compram

        [Test]
        public void Pacote_AcompanhaARenda_ComPisoETeto()
        {
            Sim s = ComGems();
            Tuning t = s.T;
            Assert.AreEqual(t.PacotePiso, s.PreviaGasto(Pacote), "sem renda");
            s.RateEma = 5.5;
            Assert.AreEqual(5.5 * t.PacoteSeg, s.PreviaGasto(Pacote));
            long previa = s.PreviaGasto(Pacote), cash = s.Cash;
            Assert.IsTrue(s.Gastar(Pacote));
            Assert.AreEqual((cash + previa, 5000L - t.ItensGems[Pacote].Preco), (s.Cash, s.Gems), "entrega o que mostrou");
            s.RateEma = 1e300;
            Assert.AreEqual(t.PacoteTeto, s.PreviaGasto(Pacote));
            Assert.LessOrEqual(t.PacoteSeg, 3600, "GDD §7.2: ate 1 h de renda");
        }

        [Test]
        public void Booster_2xPor30Min_SemAcumular()
        {
            Sim s = ComGems();
            Assert.IsTrue(s.Gastar(Booster));
            Assert.AreEqual(1800 * Sim.Hz, s.BoostLeft);
            Assert.IsFalse(s.Gastar(Booster), "com o 2x ligado");
            Assert.IsFalse(s.Anuncios.Pode(Anuncio.Receita), "o anuncio nao soma");
            Assert.AreEqual(1800 * Sim.Hz, Reabre(s).BoostLeft, "reabrir nao corta para os 5 min do anuncio");

            Sim a = ComGems();
            a.Anuncios.Pagar(a.Anuncios.Oferta(Anuncio.Receita));
            Assert.Greater(a.BoostLeft, 0);
            Assert.IsFalse(a.Gastar(Booster), "com o 2x do anuncio ligado");
            Assert.AreEqual(5000, a.Gems);
        }

        [Test]
        public void Cosmetico_UmaVez_ECompraDiretaConta()
        {
            Sim s = ComGems();
            Assert.IsTrue(s.Gastar(CosmeticoItem));
            Assert.IsTrue(s.Tem(Pintura));
            long gems = s.Gems;
            Assert.IsFalse(s.Gastar(CosmeticoItem));
            Assert.AreEqual(gems, s.Gems);
            Assert.IsTrue(Reabre(s).Tem(Pintura));

            Sim d = ComGems();
            d.Creditar("GPA.1", Pintura);
            Assert.IsFalse(d.PodeGastar(CosmeticoItem), "ja comprou com dinheiro");
        }

        [Test]
        public void Gastar_SemSaldoOuForaDaTabela_NaoMudaNada()
        {
            Sim s = ComGems(10);
            for (int i = -1; i <= s.T.ItensGems.Length; i++) Assert.IsFalse(s.Gastar(i), "item " + i);
            Assert.AreEqual((10L, 0L, 0), (s.Gems, s.Cash, s.BoostLeft));
        }

        [Test]
        public void Eventos_DizemOQueEntrou()
        {
            Sim s = ComGems(1000);
            s.Creditar("GPA.1", Boas);
            s.Gastar(Booster);
            CollectionAssert.AreEqual(new[] { (Ev.CompraCreditada, 0, 200, 1000), (Ev.GemsGastas, Booster, s.T.ItensGems[Booster].Preco, s.T.BoosterSeg) },
                s.Events.Select(e => (e.Tipo, e.A, e.B, e.C)).ToArray());
        }

        // ------------------------------------------------------------------ meta clara do GDD §6.1

        [Test]
        public void CadeiaFechada_PagaGemsUmaVez()
        {
            Tuning t = CampanhaTests.So(0, CampanhaTests.Obj(Meta.Embarcados), CampanhaTests.Obj(Meta.Embarcados, 2));
            var s = new Sim(t, 1);
            var b = new Bot(BotProfile.Bom);
            while (s.ObjetivosFeitos(0) < 1) { b.Step(s); if (s.ObjetivosFeitos(0) < 2) Assert.AreEqual(0, s.Gems, "antes de fechar a cadeia"); }
            while (s.ObjetivosFeitos(0) < 2) b.Step(s);
            Assert.AreEqual(t.GemsPorCadeia, s.Gems);
            Sim r = Sim.Load(s.Save("t", 1000), t, 1);
            for (int i = 0; i < 300; i++) r.Tick(Sim.Step);
            Assert.AreEqual(t.GemsPorCadeia, r.Gems, "reabrir nao paga de novo");
        }

        [Test]
        public void CampanhaSemPagar_NaoDaGems()
        {
            Tuning t = CampanhaTests.So(0, CampanhaTests.Obj(Meta.Embarcados));
            t.RegraCampanha = false;
            var s = new Sim(t, 1);
            var b = new Bot(BotProfile.Bom);
            while (s.ObjetivosFeitos(0) < 1) b.Step(s);
            Assert.AreEqual(0, s.Gems);
        }

        // ------------------------------------------------------------------ invariantes (CONTRATO_FASE_D1 §4)

        /// <summary>Repor voo perdido nao se compra, nem pular a perda, nem objetivo, marco ou manutencao: todo o resto do save fica igual.</summary>
        [Test]
        public void Invariante_D1_ComprarNaoTocaNoJogo()
        {
            Sim s = Jogado(BotProfile.Lento, 7);
            Assert.Greater(s.TotalPerdidos, 0, "ha voo perdido para repor");
            string antes = SoOJogo(s);
            Assert.AreEqual(Credito.Creditado, s.Creditar("a", Grande));
            for (int i = 0; i < s.T.ItensGems.Length; i++) Assert.IsTrue(s.Gastar(i), s.T.ItensGems[i].Id);
            foreach (Produto p in s.T.Produtos) s.Creditar("b" + p.Id, p.Id);
            Assert.AreEqual(antes, SoOJogo(s));
        }

        /// <summary>Conteudo fixo em toda semente: nada aleatorio pago (ECA Digital).</summary>
        [Test]
        public void Invariante_D1_ConteudoFixo_SemSorteio()
        {
            foreach (Produto p in Tabelas.Produtos)
            {
                var ganhos = Enumerable.Range(1, 50).Select(seed =>
                {
                    var s = new Sim(null, seed);
                    s.Creditar("GPA." + seed, p.Id);
                    return (s.Gems, s.Cash);
                }).Distinct().ToArray();
                Assert.AreEqual(new[] { ((long)p.Gems, p.Cash) }, ganhos, p.Id);
            }
        }

        /// <summary>Offline continua limitado a 2 h, nunca da Gems e nao gasta o booster: com o jogo fechado nada anda.</summary>
        [Test]
        public void Invariante_D1_Offline_SemGemsE2h()
        {
            Sim s = Jogado(BotProfile.Bom, 4);
            s.Creditar("a", Grande);
            s.Gastar(Booster);
            Sim dois = Reabre(s), dez = Reabre(s);
            long ate2h = dois.ApplyOffline(7200, 5000), acima = dez.ApplyOffline(36000, 5000);
            Assert.Greater(ate2h, 0);
            Assert.AreEqual(ate2h, acima, "teto de 2 h");
            Assert.AreEqual((s.Gems, s.BoostLeft, s.Steps), (dez.Gems, dez.BoostLeft, dez.Steps));
        }

        [Test]
        public void Invariante_D1_AnuncioNaoDaGems()
        {
            Sim s = new Sim();
            Assert.Greater(s.Anuncios.Pagar(s.Anuncios.Oferta(Anuncio.Receita)), 0);
            s.Cash = 500; s.ApplyOffline(600, 10); s.LastClaimCash = 100;
            s.Anuncios.Pagar(s.Anuncios.Oferta(Anuncio.Cofre));
            Assert.AreEqual(0, s.Gems);
        }

        /// <summary>O Cash comprado fica fora da renda media: o pacote nao se alimenta dele mesmo e o cofre nao aprende com a compra.</summary>
        [Test]
        public void Invariante_D1_CompraForaDoCofre()
        {
            Sim a = Jogado(BotProfile.Bom, 4), b = Jogado(BotProfile.Bom, 4);
            b.Creditar("a", Grande);
            b.Creditar("b", Boas);
            b.Gastar(Pacote);
            a.Tick(Sim.Step); b.Tick(Sim.Step);
            Assert.AreEqual((a.RateEma, a.OfflineMaxCash()), (b.RateEma, b.OfflineMaxCash()));
        }

        /// <summary>GDD §8.6: nem save editado faz o saldo virar negativo.</summary>
        [Test]
        public void Invariante_D1_SaldoNuncaEstoura()
        {
            var s = new Sim { Cash = long.MaxValue - 10 };
            s.Creditar("a", Boas);
            Assert.AreEqual(long.MaxValue, s.Cash);
            s.Gastar(Pacote);
            Assert.AreEqual(long.MaxValue, s.Cash);
        }

        [Test]
        public void LixoDoSave_ViraPadraoLimitado()
        {
            Sim Abre(string corpo) => Sim.Load(SaveEnvelope.Wrap(corpo, "t", 1));
            Assert.AreEqual(0, Abre("gems=-5\n").Gems);
            Assert.AreEqual(0, Abre("gems=lixo\n").Gems);
            Assert.AreEqual(Tuning.Padrao.GemsTeto, Abre("gems=99999999999999\n").Gems);
            Sim p = Abre("posse=" + Pequeno + ",sumiu," + Pintura + "\n");
            Assert.AreEqual((false, false, true), (p.Tem(Pequeno), p.Tem("sumiu"), p.Tem(Pintura)));
            Assert.AreEqual(Credito.Creditado, Abre("tx=,,lixo,\n").Creditar("GPA.1", Grande));
        }

        [Test]
        public void IdaEVolta_TextoEstavel_ERetomadaIgual()
        {
            Sim s = Jogado(BotProfile.PacoteGrande, 4);
            s.Creditar("z", Boas);
            s.Creditar("a", Pintura);
            string texto = s.Save("t", 1000);
            Assert.AreEqual(texto, Sim.Load(texto).Save("t", 1000));

            Bot.Resultado corrido = Bot.Medir(BotProfile.PacoteGrande, 3, 6), reaberto = Bot.Medir(BotProfile.PacoteGrande, 3, 6, null, true);
            Assert.AreEqual((corrido.Receita, corrido.Embarque, corrido.Objetivos), (reaberto.Receita, reaberto.Embarque, reaberto.Objetivos));
        }
    }
}
