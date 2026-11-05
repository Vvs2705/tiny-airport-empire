using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>TA-3, as 20 historias (Sim.Historias.cs, docs/HISTORIAS.md): a tabela, o sorteio pela semente, cada pedido, o save e as
    /// invariantes. O alcance em 200 sementes e' um portao do BalanceTests (Portao_Historias, pelo CI).</summary>
    public class HistoriasTests
    {
        const long U = 1800000000;

        /// <summary>So a historia h, em Vale Verde, no marco 0; os 6 perfis contrataveis desde ja.</summary>
        static Tuning Uma(Historia h) => new Tuning { Historias = new[] { h }, PerfilMarco = new int[6] };

        /// <summary>Ana (check-in), Beto (seguranca), Cida (portao) e Dani (loja, a 1a livre), com velocidade e capacidade no maximo das 4 da
        /// v0.1: todo mundo embarca.</summary>
        static Sim Forte(Tuning t, int seed = 1)
        {
            t = t ?? new Tuning();
            t.PerfilMarco = new int[6];
            var s = new Sim(t, seed) { Cash = 1000000 };
            for (int i = 0; i < 3; i++) Assert.IsTrue(s.Hire());
            Assert.IsTrue(s.Hire(Perfil.Bagageiro));
            Assert.AreEqual(Estacao.Loja, s.Staff[3].Em, "a 4a vai para a loja");
            foreach (Station st in s.Stations)
                for (int k = 0; k < 8; k++) { s.Buy(st.Tipo, Melhoria.Velocidade); s.Buy(st.Tipo, Melhoria.Capacidade); }
            return s;
        }

        /// <summary>Anda ate o fim da onda `w` (o comeco da seguinte) e devolve os eventos. `antes` roda a cada passo.</summary>
        static List<Evento> AteOFimDa(Sim s, int w, Action antes = null)
        {
            var ev = new List<Evento>();
            while (s.Onda <= w) { antes?.Invoke(); s.Tick(Sim.Step); ev.AddRange(s.Events); }
            return ev;
        }

        static bool Embarcou(List<Evento> ev, int pax) => ev.Any(e => e.Tipo == Ev.Embarcou && e.A == pax);

        // ------------------------------------------------------------------ a tabela

        [Test]
        public void Tabela_20_DistribuidasECoerentesComOJogo()
        {
            Historia[] t = Tabelas.Historias;
            Assert.AreEqual(20, t.Length);
            Assert.AreEqual(t.Length, t.Select(h => h.Id).Distinct().Count(), "Id unico");
            Assert.AreEqual(new[] { 8, 6, 6 }, Enumerable.Range(0, 3).Select(a => t.Count(h => h.Aeroporto == a)).ToArray(), "8, 6 e 6 por aeroporto");
            Assert.AreEqual(new[] { 4, 4, 12 }, new[] { 0, 1, 2 }.Select(m => t.Count(h => h.Marco == m) + (m == 2 ? t.Count(h => h.Marco == 3) : 0)).ToArray(), "espalhadas pelos marcos");
            Assert.IsTrue(t[0].Id == "rosa_casamento" && t[0].Onde == null && !t[0].Todos && t[0].Marco == 0, "a Dona Rosa (tutorial, objetivo vv_rosa) so pede embarcar");
            var tu = new Tuning();
            foreach (Historia h in t)
            {
                Assert.GreaterOrEqual(h.Marco, tu.Aeroportos[h.Aeroporto].Marco, h.Id + ": o aeroporto ja abriu");
                if (h.Onde is Estacao e)
                {
                    Assert.LessOrEqual(tu.Abre[(int)e], h.Marco, h.Id + ": a estacao ja abriu");
                    Assert.IsTrue(e != Estacao.Manutencao && e != Estacao.Despacho, h.Id + ": estacao com passageiro");
                    // em Aurora Global as rotas gratis sao de conexao: o passageiro entra pela seguranca
                    Assert.IsFalse(h.Aeroporto == 2 && (e == Estacao.CheckIn || e == Estacao.Bagagem), h.Id + ": conexao nao passa pelo balcao");
                }
                if (h.Quem is Perfil q)
                {
                    Assert.IsNotNull(h.Onde, h.Id + ": quem atende, onde");
                    Assert.LessOrEqual(tu.PerfilMarco[(int)q], h.Marco, h.Id + ": o perfil ja pode ser contratado");
                }
                Assert.IsFalse(h.Todos && h.Onde != null, h.Id + ": um pedido so");
            }
            Assert.AreEqual(20, Enumerable.Range(0, 20).Count(h => (new Sim().TodasHistorias >> h & 1) == 1));
        }

        /// <summary>Nada se compra: a tabela nao tem preco e o sorteio, o pedido e o album nao leem Cash, Gems nem anuncio.</summary>
        [Test]
        public void NenhumaDependeDeCompraOuAnuncio()
        {
            foreach (var f in typeof(Historia).GetFields())
                Assert.IsFalse(new[] { "Custo", "Preco", "Gems", "Produto", "Anuncio", "Recompensa" }.Any(x => f.Name.Contains(x)), f.Name);
            Sim a = Forte(null, 5), b = Forte(null, 5);
            Assert.AreEqual(Credito.Creditado, b.Creditar("tx-hist", "gems_pack_large"));
            Assert.IsTrue(b.Gastar(Array.FindIndex(b.T.ItensGems, i => i.Tipo == TipoItem.Booster)), "2x ligado");
            b.Cash += 5000000;
            for (int w = 0; w < 8; w++)
            {
                AteOFimDa(a, w); AteOFimDa(b, w);
                Assert.AreEqual((a.HistoriaOnda, a.HistoriaPax, a.HistoriaPasso, a.HistoriasSaiu, a.Album), (b.HistoriaOnda, b.HistoriaPax, b.HistoriaPasso, b.HistoriasSaiu, b.Album), "onda " + w);
            }
            Assert.Greater(a.Album, 1, "entregou mais de uma");
        }

        // ------------------------------------------------------------------ o sorteio

        [Test]
        public void UmaPorOnda_APartirDaSegunda_ADonaRosaPrimeiro()
        {
            for (int seed = 1; seed <= 10; seed++)
            {
                var s = new Sim(null, seed);
                var bot = new Bot(BotProfile.Bom);
                for (int w = 0; w < 8; w++)
                {
                    while (s.Onda < w || s.Onda == w && s.PassoNaOnda < s.OndaPassos / 2) bot.Step(s);   // no meio da onda: as chegadas ja sairam
                    int n = 0, quem = -1;
                    foreach (Voo v in s.Voos)
                        for (int i = 0; i < v.Total; i++)
                            if (s.NovoPax(Sim.PaxId(w, v.Indice, i)).Historia) { n++; quem = Sim.PaxId(w, v.Indice, i); }
                    Assert.AreEqual(w == 0 ? 0 : 1, n, "semente " + seed + ", onda " + (w + 1));
                    Assert.AreEqual(w == 0 ? -1 : quem, s.HistoriaPax);
                    if (w == 1) Assert.AreEqual(0, s.HistoriaOnda, "a Dona Rosa abre o album");
                }
            }
        }

        /// <summary>A mesma semente da a mesma sequencia; sementes diferentes, outra. Uma historia so volta quando as ineditas que cabem no
        /// aeroporto e no marco acabaram.</summary>
        [Test]
        public void Sorteio_PelaSemente_IneditasPrimeiro()
        {
            var seqs = new List<string>();
            for (int seed = 1; seed <= 12; seed++)
            {
                var s = new Sim(null, seed);
                var bot = new Bot(BotProfile.Guiado);
                var seq = new List<int>();
                int saiu = 0;
                while (s.Onda < 40)
                {
                    int onda = s.Onda;
                    bot.Step(s);
                    if (s.Onda == onda || s.HistoriaOnda < 0) { saiu = s.HistoriasSaiu; continue; }
                    seq.Add(s.HistoriaOnda);
                    Historia atual = s.HistoriaAtual;
                    Assert.IsTrue(atual.Aeroporto == s.AeroportoAtual && atual.Marco <= s.Marco, "semente " + seed + ": " + atual.Id + " fora do aeroporto ou do marco");
                    if ((saiu >> s.HistoriaOnda & 1) == 1)
                        for (int h = 0; h < s.T.Historias.Length; h++)
                        {
                            Historia x = s.T.Historias[h];
                            bool cabe = x.Aeroporto == s.AeroportoAtual && x.Marco <= s.Marco && (x.Onde == null || s.Ativa(x.Onde.Value));
                            Assert.IsFalse(cabe && (saiu >> h & 1) == 0, "semente " + seed + ": repetiu " + s.HistoriaOnda + " com a " + h + " inedita");
                        }
                    saiu = s.HistoriasSaiu;
                }
                Assert.Greater(seq.Count, 30);
                seqs.Add(string.Join(",", seq));
            }
            Assert.Greater(seqs.Distinct().Count(), 6, "a semente muda a ordem");
            var r = new Sim(null, 3); var rb = new Bot(BotProfile.Guiado);
            var rs = new List<int>();
            while (r.Onda < 40) { int o = r.Onda; rb.Step(r); if (r.Onda != o && r.HistoriaOnda >= 0) rs.Add(r.HistoriaOnda); }
            Assert.AreEqual(seqs[2], string.Join(",", rs), "reproduzivel");
        }

        // ------------------------------------------------------------------ os pedidos

        [Test]
        public void SoEmbarcar_EntraNoAlbumAoEmbarcar()
        {
            Sim s = Forte(Uma(new Historia { Id = "t" }));
            List<Evento> ev = AteOFimDa(s, 1);
            Evento h = ev.Single(e => e.Tipo == Ev.Historia);
            Assert.AreEqual((0, 1), (h.A, h.C));
            Assert.IsTrue(Embarcou(ev, h.B));
            int i = ev.FindIndex(e => e.Tipo == Ev.Embarcou && e.A == h.B), j = ev.IndexOf(h);
            Assert.AreEqual(i + 1, j, "no mesmo passo do embarque, logo depois");
            Assert.AreEqual(1, s.Album);
        }

        [Test]
        public void Parada_SoComALojaAberta()
        {
            foreach (bool aberta in new[] { true, false })
            {
                Sim s = Forte(Uma(new Historia { Id = "t", Onde = Estacao.Loja }));
                if (!aberta) Assert.IsTrue(s.Move(3, Estacao.CheckIn), "fecha a loja");
                AteOFimDa(s, 0);
                Assert.AreEqual(0, s.HistoriaOnda);
                Assert.IsTrue(s.NovoPax(s.HistoriaPax).Loja, "vai para quem ja queria a loja");
                int pax = s.HistoriaPax;
                List<Evento> ev = AteOFimDa(s, 1);
                Assert.IsTrue(Embarcou(ev, pax), "embarcou nos dois casos");
                Assert.AreEqual(aberta, ev.Any(e => e.Tipo == Ev.Venda && e.A == pax), "passou pela loja so com ela aberta");
                Assert.AreEqual(aberta ? 1 : 0, s.Album, aberta ? "comprou e embarcou" : "loja fechada: nao fecha");
            }
        }

        [Test]
        public void Perfil_SoComOPerfilPedidoNaEstacao()
        {
            foreach (bool ana in new[] { true, false })
            {
                Sim s = Forte(Uma(new Historia { Id = "t", Onde = Estacao.CheckIn, Quem = Perfil.Atendente }));
                if (!ana) { Assert.IsTrue(s.Move(0, Estacao.Portao)); Assert.IsTrue(s.Move(2, Estacao.CheckIn)); }   // a Cida no check-in
                AteOFimDa(s, 0);
                int pax = s.HistoriaPax;
                List<Evento> ev = AteOFimDa(s, 1);
                Assert.IsTrue(Embarcou(ev, pax), "embarcou nos dois casos");
                Assert.AreEqual(ana ? 1 : 0, s.Album, ana ? "a Ana atendeu" : "outra pessoa atendeu: nao fecha");
            }
        }

        [Test]
        public void Todos_FechaComOPortao_SoSemPerdidos()
        {
            foreach (bool perde in new[] { false, true })
            {
                Sim s = Forte(Uma(new Historia { Id = "t", Todos = true }));
                AteOFimDa(s, 0);
                int pax = s.HistoriaPax, voo = pax / 500 % 2;
                // perde: depois que ele embarca, a Cida sai do portao e quem vem atras fica
                List<Evento> ev = AteOFimDa(s, 1, () => { if (perde && s.HistoriaPasso == 2 && s.Staff[2].Em == Estacao.Portao) s.Move(2, Estacao.CheckIn); });
                Assert.IsTrue(Embarcou(ev, pax));
                Evento partiu = ev.First(e => e.Tipo == Ev.VooPartiu && e.A == 2 + voo);
                Assert.AreEqual(perde, partiu.B < partiu.C, "perdeu gente so no caso que tira a Cida");
                Assert.AreEqual(perde ? 0 : 1, s.Album);
                if (!perde)
                {
                    int h = ev.FindIndex(e => e.Tipo == Ev.Historia), p = ev.IndexOf(partiu);
                    Assert.Greater(h, ev.FindIndex(e => e.Tipo == Ev.Embarcou && e.A == pax) + 1, "nao fecha ao embarcar");
                    Assert.Less(h, p, "fecha no fechamento do portao, antes do VooPartiu");
                }
                Assert.AreEqual(0, s.HistoriaPasso);
            }
        }

        /// <summary>A historia que nao fechou volta numa proxima onda; a entregue so volta com o album do aeroporto cheio, sem entrar de novo.</summary>
        [Test]
        public void NaoFechou_Volta_Entregue_NaoContaDeNovo()
        {
            var t = Uma(new Historia { Id = "a", Onde = Estacao.Loja });
            Sim s = Forte(t);
            Assert.IsTrue(s.Move(3, Estacao.CheckIn), "loja fechada na onda 2");
            AteOFimDa(s, 1);
            Assert.AreEqual(0, s.Album);
            Assert.IsTrue(s.Move(3, Estacao.Loja));
            List<Evento> ev = AteOFimDa(s, 3);
            Assert.AreEqual(new[] { 1, 0 }, ev.Where(e => e.Tipo == Ev.Historia).Select(e => e.C).ToArray(), "volta, fecha (nova) e fecha de novo (repetida)");
            Assert.AreEqual(1, s.Album);
        }

        [Test]
        public void SemARegra_AV01()
        {
            var t = new Tuning { RegraHistorias = false };
            Sim s = Forte(t);
            List<Evento> ev = AteOFimDa(s, 2);
            Assert.AreEqual(-1, s.HistoriaOnda);
            Assert.IsTrue(ev.Where(e => e.Tipo == Ev.Historia).All(e => e.A == 0), "so a Dona Rosa");
            Assert.AreEqual(1, s.Album);
            Assert.AreEqual(1, s.HistoriasSaiu, "a entregue conta como vista");
        }

        // ------------------------------------------------------------------ save

        [Test]
        public void Retomada_ComAHistoriaNoMeio_ContinuaIgual()
        {
            var s = new Sim(null, 4);
            var b0 = new Bot(BotProfile.Bom);
            while (!(s.Onda >= 2 && s.HistoriaPax >= 0 && s.Estacoes.Any(st => st.Fila.Exists(p => p.Historia) || st.Atendendo.Exists(p => p.Historia)))) b0.Step(s);
            string a = s.Save("t", U);
            StringAssert.Contains("\nhp=" + s.HistoriaOnda + "," + s.HistoriaPax + ",", a);
            Sim r = Sim.Load(a);
            Assert.AreEqual((s.HistoriaOnda, s.HistoriaPax, s.HistoriaPasso, s.HistoriasSaiu, s.Album), (r.HistoriaOnda, r.HistoriaPax, r.HistoriaPasso, r.HistoriasSaiu, r.Album));
            Assert.AreEqual(a, r.Save("t", U));
            var b1 = new Bot(BotProfile.Bom); var b2 = new Bot(BotProfile.Bom);   // bots novos dos dois lados: o bot tem memoria propria
            for (int i = 0; i < 600 * Sim.Hz; i++) { b1.Step(s); b2.Step(r); }
            Assert.AreEqual(s.Save("t", U + 1), r.Save("t", U + 1));
        }

        [Test]
        public void LixoDoSave_ViraPadraoLimitado()
        {
            Sim s = Jogado(400f);
            string p = SaveEnvelope.Unwrap(s.Save("x", U)).Payload;
            Assert.GreaterOrEqual(s.HistoriaOnda, 0);
            foreach (string lixo in new[] { "hp=99,5,7,-1\nalbum=-5\n", "hp=0,123456,9,99999999\n", "hp=a,b\n", "hp=3," + (s.Onda * 1000 + 499) + ",9,0\n" })
            {
                Sim m = Sim.Load(SaveEnvelope.Wrap(p.Replace("\nhp=", "\nhp0=") + lixo, "x", U), s.T, s.Seed);
                Assert.IsFalse(m.SaveRefused);
                Assert.That(m.HistoriaOnda, Is.InRange(0, m.T.Historias.Length - 1), lixo + ": sorteia a da onda de novo");
                Assert.AreEqual(m.Onda, m.HistoriaPax / 1000, "o passageiro e' desta onda");
                Assert.Less(m.HistoriaPax % 500, m.Voos[m.HistoriaPax / 500 % 2].Total, "e existe no voo");
                Assert.That(m.HistoriaPasso, Is.InRange(0, 2));
                Assert.AreEqual(0, m.Album & ~m.TodasHistorias, "album so com historias da tabela");
                Assert.AreEqual(0, m.HistoriasSaiu & ~m.TodasHistorias);
                Assert.AreEqual(m.Album, m.HistoriasSaiu & m.Album, "a entregue ja saiu");
            }
        }

        static Sim Jogado(float seg, int seed = 7, Tuning t = null)
        {
            var s = new Sim(t, seed);
            var b = new Bot(BotProfile.Bom);
            for (int i = 0; i < (int)(seg * Sim.Hz); i++) b.Step(s);
            return s;
        }

        /// <summary>Save real gravado pela main 219c1de (schema 15, com a esteira, a limpeza e os suprimentos), na onda 15 da semente 7: abre no 16
        /// com a historia da onda sorteada, as entregues como ja vistas, e sem a regra e' o mesmo jogo de jogar ate ali com o codigo novo. A
        /// cadeia 12 -> 13 -> 14 -> 15 esta no SaveTests e no SuprimentosTests.</summary>
        [Test]
        public void MigracaoDoSchema15_SaveReal_SorteiaAHistoriaDaOnda()
        {
            Sim m = Sim.Load(V15, null, 7);
            Assert.IsFalse(m.SaveRefused, "schema 15 migra, nao e' recusado");
            Assert.AreEqual((15, 1), (m.Onda, m.Album));
            Assert.GreaterOrEqual(m.HistoriaOnda, 0, "a onda 15 tem historia");
            Assert.AreEqual(1 | 1 << m.HistoriaOnda, m.HistoriasSaiu, "a Dona Rosa entregue e a da onda");
            Pax[] com = m.Estacoes.SelectMany(st => st.Fila.Concat(st.Atendendo)).Where(p => p.Historia).ToArray();
            Assert.IsTrue(com.Length == 0 || com.Length == 1 && com[0].Id == m.HistoriaPax, "no maximo um, o da historia");
            string texto = m.Save("0.16.0", U);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", texto);
            StringAssert.Contains("\nhp=" + m.HistoriaOnda + "," + m.HistoriaPax + ",", texto);
            Assert.AreEqual(texto, Sim.Load(texto, null, 7).Save("0.16.0", U), "ida e volta estavel no schema 16");
            var sem = new Tuning { RegraHistorias = false };
            Sim n = Jogado(2790f, 7, sem);
            Assert.AreEqual(SemHp(n.Save("0.15.0", U)), SemHp(Sim.Load(V15, sem, 7).Save("0.15.0", U)), "migrar = jogar ate ali com o codigo novo");
        }

        /// <summary>Sem a linha hp=, a satisfacao (schema 17: sat, satm, satp, que so le o jogo) e o hash, com os double em 5 casas: o save real saiu do
        /// .NET, e o Mono do Unity difere na 8a casa.</summary>
        static string SemHp(string save) => string.Join("\n", save.Split('\n').Where(l => !l.StartsWith("hp=") && !l.StartsWith("sat") && !l.StartsWith("sha256="))
            .Select(l => System.Text.RegularExpressions.Regex.Replace(l, @"-?\d+\.\d+", x => double.Parse(x.Value, System.Globalization.CultureInfo.InvariantCulture)
                .ToString("F5", System.Globalization.CultureInfo.InvariantCulture))));

        const string V15 =
            "sha256=a814abdff5f82102b723e0fdf20b8a890eaffb9edf31b1f8e97aec25e67af893\nschema=15\ncontent=0.15.0\nat=1800000000\nt=83700\ncash=5792\nseed=7\nonda=15" +
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
            "49,0,0,30,30,27,4,24,70,0,232,252,25,0,2,1,0,0,0,0,6\ngems=0\npasse=0,0,350,3\natrasada=0\nhoje=1,0,7818,1015,0,1904,2236,1200,0,0,0,0,0,0,33\ndc.0=0," +
            "644,4570,445,0,528,1000,790,0,0,210,108,54,228,44\nhist=3600,4,-1,0,0,10,0,4,0,0,0,6,0,0,0,0;25200,3,-1,0,8,26,3,0,0,0,0,2,3,7,0,0;41460,1,4,0,15,40,0" +
            ",7,1,0,0,0,0,0,0,0;43200,5,-1,0,-1,0,0,0,0,1,161,12,0,0,0,0;46800,3,-1,0,16,58,16,0,0,0,0,1,3,18,2,0;62400,0,1,0,23,32,0,0,3,1,0,0,0,0,0,0;69900,3,-1," +
            "0,25,54,7,0,0,0,0,1,3,2,3,0;73800,3,-1,0,26,43,2,0,0,0,0,2,3,16,0,0;79260,1,5,0,29,40,0,0,1,0,0,0,0,0,0,0\nsuj=176,120,0,114,176,12,80\nbh=0\nbhneg=0" +
            "\nzels=1\nzel.0=5,1,0,0,330,0\nsup.0=4,0,0,0\nsup.1=4,0,0,0\n";
    }
}
