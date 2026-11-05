using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Frente G: satisfacao com motivo (GDD §4.1 linha G; Direcao §8.4). Regras e invariantes em Sim.Satisfacao.cs.</summary>
    public class SatisfacaoTests
    {
        const long T0 = 1_800_000_000;

        static List<Evento> Anda(Sim s, float seg)
        {
            var ev = new List<Evento>();
            for (int i = 0, n = (int)System.Math.Round(seg * Sim.Hz); i < n; i++) { s.Tick(Sim.Step); ev.AddRange(s.Events); }
            return ev;
        }

        /// <summary>Os voos da 1a onda ja criados e ninguem na equipe: quem chega para no check-in.</summary>
        static Sim Novo(Tuning t = null)
        {
            var s = new Sim(t ?? new Tuning(), 1);
            s.Tick(Sim.Step);
            return s;
        }

        static Tuning ComRotas(TipoRota tipo)
        {
            var t = new Tuning();
            t.Rotas = t.Rotas.Select(r => r.Copia()).ToArray();
            t.Rotas[0].Tipo = t.Rotas[1].Tipo = tipo;
            return t;
        }

        /// <summary>Um passageiro de fora da fila natural (indice alto), do voo 0 da 1a onda.</summary>
        static Pax Avulso(Sim s, int i = 0) => s.NovoPax(Sim.PaxId(0, 0, 400 + i));

        [Test]
        public void Perfil_SaiDaRotaDoVoo_OGrupoDeConexaoEConexao()
        {
            foreach ((TipoRota r, PerfilPax p) in new[] { (TipoRota.Curta, PerfilPax.Executivo), (TipoRota.Turistica, PerfilPax.Familia), (TipoRota.Conexao, PerfilPax.Conexao), (TipoRota.Longa, PerfilPax.Viajante) })
            {
                Sim s = Novo(ComRotas(r));
                Assert.AreEqual(p, s.PerfilDe(s.NovoPax(Sim.PaxId(0, 0, 0))), r.ToString());
            }
            Sim c = Novo();
            Voo v = c.Voos[0];
            int b = v.Total;
            v.Grupo = 2; v.Total += 2; v.GrupoConexao = true;
            Assert.AreEqual(PerfilPax.Conexao, c.PerfilDe(c.NovoPax(Sim.PaxId(0, 0, b))), "o grupo extra de conexao do incidente");
            Assert.AreEqual(PerfilPax.Executivo, c.PerfilDe(c.NovoPax(Sim.PaxId(0, 0, 0))));
        }

        [Test]
        public void Tolerancia_OExecutivoToleraMenosEspera_AFamiliaMenosSujeira()
        {
            Sim ex = Novo(ComRotas(TipoRota.Curta)), fa = Novo(ComRotas(TipoRota.Turistica));
            Pax pe = Avulso(ex), pf = Avulso(fa);
            Assert.AreEqual((ex.T.SatMais[0], ex.T.SatMais[1]), (ex.Peso(pe, Fator.Espera), ex.Peso(pe, Fator.Limpeza)), "sem fila e tudo limpo: os dois ajudam");
            pe.Espera = pf.Espera = 30 * Sim.Hz;
            Assert.Less(ex.Peso(pe, Fator.Espera), fa.Peso(pf, Fator.Espera), "30 s de fila pesam mais no executivo");
            pe.Espera = pf.Espera = 0;
            pe.Sujo = pf.Sujo = 300;
            Assert.Less(fa.Peso(pf, Fator.Limpeza), ex.Peso(pe, Fator.Limpeza), "a mesma sujeira pesa mais na familia");
            pe.Sujo = 0;
            pf.Espera = 999 * Sim.Hz; pf.Sujo = Sim.SujeiraMax;
            Assert.AreEqual((-fa.T.SatTeto[0], -fa.T.SatTeto[1]), (fa.Peso(pf, Fator.Espera), fa.Peso(pf, Fator.Limpeza)), "cada fator para no teto dele");
            Assert.AreEqual(fa.T.SatBase - fa.T.SatTeto[0] - fa.T.SatTeto[1], fa.Nota(pf));
            Assert.AreEqual(Fator.Espera, fa.Principal(pf), "o motivo principal e o que mais pesa contra");
            Assert.AreEqual(Fator.Espera, ex.Principal(pe), "sem nada contra, o que mais ajuda (empate: a ordem)");
        }

        [Test]
        public void Portao_SentaAteAcabarACadeira_AtrasoSoDepoisDaHoraDeAbrir_EAFilaSomaEspera()
        {
            Sim s = Novo(new Tuning { CadeirasPortao = 2 });
            Voo v = s.Voos[0];
            s.Avioes[0].Hangar = 999999;   // o aviao nao fica pronto: o embarque nao abre na hora
            List<Pax> gente = Enumerable.Range(0, 4).Select(i => Avulso(s, i)).ToList();
            s.St(Estacao.Portao).Fila.AddRange(gente);
            Pax fila = Avulso(s, 10);
            s.St(Estacao.CheckIn).Fila.Add(fila);
            Anda(s, 10f);
            Assert.IsTrue(gente[0].Sentou && gente[1].Sentou && gente[0].EmPe == 0, "as 2 cadeiras, na ordem de chegada");
            Assert.IsTrue(!gente[2].Sentou && gente[2].EmPe > 0 && gente[3].EmPe > 0, "sem cadeira, em pe");
            Assert.AreEqual((s.T.SatMais[4], true), (s.Peso(gente[0], Fator.Conforto), s.Peso(gente[3], Fator.Conforto) < 0));
            Assert.IsTrue(gente.All(p => p.Espera == 0 && p.Atraso == 0), "esperar o embarque abrir e o combinado: nao e fila nem atraso");
            Assert.AreEqual(10f, fila.Espera * Sim.Step, 0.5f, "a fila do check-in soma espera");
            Assert.AreEqual((int)Estacao.Portao, s.SatOnde(Fator.Conforto));
            int falta = v.AbrePasso - s.PassoNaOnda;
            Anda(s, falta * Sim.Step + 5f);
            Assert.IsFalse(v.Embarque);
            Assert.IsTrue(gente.All(p => p.Atraso * Sim.Step >= 4.5f && s.Peso(p, Fator.Pontualidade) < 0), "passou da hora de abrir: atraso");
            Assert.AreEqual(0, s.SatOnde(Fator.Pontualidade), "o voo com atraso");
            Assert.AreEqual(0, s.Peso(fila, Fator.Pontualidade), "no check-in ninguem ve o atraso do portao");
        }

        [Test]
        public void PerdeuOVoo_NaFila_AEsperaVaiAoTeto_ESaiNaMedia()
        {
            Sim s = Novo();   // ninguem na equipe: o voo 0 fecha com todos no check-in
            Voo v = s.Voos[0];
            List<Evento> ev = Anda(s, (v.FechaPasso - s.PassoNaOnda) * Sim.Step + 0.5f);
            Assert.IsTrue(ev.Any(e => e.Tipo == Ev.VooPerdido && e.A == v.Id && e.B == v.Total));
            Assert.AreEqual(v.Total, s.SatAmostras);
            Assert.AreEqual(-s.T.SatTeto[0], s.SatFator(Fator.Espera), 1e-9, "perdeu na fila: a espera no teto");
            Assert.AreEqual(Fator.Espera, s.SatTop(0));
            Assert.AreEqual(Carinha.Brava, s.CarinhaDe((int)System.Math.Round(s.SatMedia)));
        }

        [Test]
        public void Banheiro_SoPesaEmQuemPrecisava_FilaENegadoContra_UsarSemFilaAjuda()
        {
            Sim s = Novo();
            Pax p = Avulso(s);
            p.Banheiro = true;
            Assert.AreEqual(0, s.Peso(p, Fator.Banheiro), "precisava e nao foi (sem folga): nao conta");
            p.UsouBanheiro = true;
            Assert.AreEqual(s.T.SatMais[2], s.Peso(p, Fator.Banheiro));
            p.FilaBanheiro = 20 * Sim.Hz;
            Assert.Less(s.Peso(p, Fator.Banheiro), 0, "20 s na porta");
            p.Negado = true;
            Assert.AreEqual(-s.T.SatNegado, s.Peso(p, Fator.Banheiro));

            Pax q = Avulso(s, 1);
            q.Banheiro = true;
            s.Banheiro.Fila.Add(q);
            s.Sujeira[(int)Area.Banheiro] = Sim.SujeiraMax;
            s.Banheiro.Interditado = true;
            Anda(s, 0.1f);
            Assert.IsTrue(q.Negado, "a fila do banheiro fechado volta ao portao: achou fechado");
            Assert.AreEqual((int)Area.Banheiro, s.SatOnde(Fator.Banheiro));
            Assert.AreEqual((int)Area.Banheiro, s.SatOnde(Fator.Limpeza), "a area mais suja");
        }

        [Test]
        public void Reclama_UmaVez_DoMotivoPrincipal_EZeraNoDiaSeguinte()
        {
            Sim s = Novo();
            Pax p = Avulso(s);
            p.Espera = 200 * Sim.Hz; p.Sujo = Sim.SujeiraMax;
            Assert.Less(s.Nota(p), s.T.SatReclama);
            s.St(Estacao.CheckIn).Fila.Add(p);
            List<Evento> ev = Anda(s, 3f);
            Evento[] r = ev.Where(e => e.Tipo == Ev.Reclamou && e.A == p.Id).ToArray();
            Assert.AreEqual(1, r.Length, "1x por passageiro");
            Assert.AreEqual((int)Fator.Espera, r[0].B, "do motivo principal");
            Assert.IsTrue(p.Reclamou);
            Assert.AreEqual(1, s.Reclamacoes(Fator.Espera));
            while (s.DiaJogo == 0) s.Steps += s.OndaPassos;
            s.Tick(Sim.Step);
            Assert.AreEqual(0, s.Reclamacoes(Fator.Espera), "as do dia: o dia seguinte comeca do zero");
        }

        [Test]
        public void Aeroporto_TodoQueSaiEntraNaMedia_OTopEOrdenado_ENadaForaDaFaixa()
        {
            var s = new Sim(null, 3);
            var bot = new Bot(BotProfile.Lento);
            long sairam = 0;
            for (int i = 0; i < 3 * s.OndaPassos; i++)
            {
                bot.Step(s);
                foreach (Evento e in s.Events)
                {
                    if (e.Tipo == Ev.VooPartiu) sairam += e.C;
                    Assert.AreNotEqual(Ev.Gorjeta, e.Tipo, "sem a regra, nada de gorjeta");
                }
                if (i % 15 != 0) continue;
                foreach (Station st in s.Estacoes)
                    foreach (Pax p in st.Fila.Concat(st.Atendendo))
                    {
                        int n = s.Nota(p);
                        Assert.IsTrue(n >= 0 && n <= 100, "nota " + n);
                        for (int f = 0; f < Sim.Fatores; f++)
                        {
                            int w = s.Peso(p, (Fator)f);
                            Assert.IsTrue(w >= -s.T.SatTeto[f] && w <= s.T.SatMais[f], (Fator)f + " " + w);
                        }
                    }
            }
            Assert.Greater(sairam, 0);
            Assert.AreEqual(sairam, s.SatAmostras, "embarcou ou perdeu o voo: entra na media");
            Assert.IsTrue(s.SatMedia >= 0 && s.SatMedia <= 100);
            Assert.AreEqual(Sim.Fatores, Enumerable.Range(0, Sim.Fatores).Select(k => s.SatTop(k)).Distinct().Count());
            for (int k = 1; k < Sim.Fatores; k++) Assert.GreaterOrEqual(System.Math.Abs(s.SatFator(s.SatTop(k - 1))), System.Math.Abs(s.SatFator(s.SatTop(k))));
            for (int f = 0; f < Sim.Fatores; f++) Assert.IsTrue(s.SatFator((Fator)f) >= -s.T.SatTeto[f] && s.SatFator((Fator)f) <= s.T.SatMais[f]);
        }

        /// <summary>GDD §6.1 e §5.4: a satisfacao nao e moeda e nao pune. Sem a gorjeta, um aeroporto onde todos odeiam tudo joga igual, numero a numero.</summary>
        [Test]
        public void SoLe_SemAGorjeta_OJogoEOMesmoNumeroANumero_ComAGorjetaNinguemContenteNaoGanhaNada()
        {
            var odeia = new Tuning { SatBase = 0, SatReclama = 101, CadeirasPortao = 0, TolEspera = new[] { 0.01f, 0.01f, 0.01f, 0.01f }, TolSujeira = new[] { 1, 1, 1, 1 } };
            var odeiaComGorjeta = new Tuning { SatBase = 0, SatReclama = 101, RegraSatBonus = true };
            foreach (int seed in new[] { 1, 2 })
            {
                Bot.Resultado a = Bot.Medir(BotProfile.Bom, seed, 3), b = Bot.Medir(BotProfile.Bom, seed, 3, odeia), c = Bot.Medir(BotProfile.Bom, seed, 3, odeiaComGorjeta);
                Assert.AreEqual((a.Receita, a.Embarque, a.Moves, a.Despesas, a.Limpezas), (b.Receita, b.Embarque, b.Moves, b.Despesas, b.Limpezas), "seed " + seed);
                Assert.AreEqual((a.Receita, a.Embarque, 0L), (c.Receita, c.Embarque, c.Gorjetas), "ninguem contente: a regra nao muda nada");
                Assert.Greater(b.Reclamacoes, a.Reclamacoes, "a nota mudou, o jogo nao");
                Assert.Less(b.SatMedia, a.SatMedia);
                Assert.GreaterOrEqual(b.SatMedia, 0.0, "a nota nunca sai de 0 a 100");
            }
        }

        [Test]
        public void Gorjeta_SoComARegra_SoDeQuemEmbarcaContente_SatBonusDaTarifa()
        {
            var t = new Tuning { RegraSatBonus = true };
            var s = new Sim(t, 1);
            var bot = new Bot(BotProfile.Bom);
            var tarifa = new Dictionary<int, int>();
            int gorjetas = 0;
            for (int i = 0; i < 2 * s.OndaPassos; i++)
            {
                bot.Step(s);
                foreach (Evento e in s.Events)
                    if (e.Tipo == Ev.Embarcou) tarifa[e.A] = e.B;
                    else if (e.Tipo == Ev.Gorjeta)
                    {
                        gorjetas++;
                        Assert.IsTrue(tarifa.ContainsKey(e.A), "so quem embarcou");
                        Assert.AreEqual((int)System.Math.Round(tarifa[e.A] * (double)t.SatBonus, System.MidpointRounding.AwayFromZero), e.B);
                    }
            }
            Assert.Greater(gorjetas, 0);
            Assert.Less(gorjetas, tarifa.Count + 1);
            Assert.AreEqual(0, Bot.Medir(BotProfile.Bom, 1, 2).Gorjetas, "sem a regra (o padrao), nenhuma");
        }

        [Test]
        public void Save_NoMeioDaOnda_IdaEVoltaEstavel_EOMesmoJogo()
        {
            var a = new Sim(null, 5);
            var bot = new Bot(BotProfile.Lento);
            for (int i = 0; i < (int)(1.6 * a.OndaPassos); i++) bot.Step(a);
            string t = a.Save("t", T0);
            StringAssert.Contains("\nsatp=", t);
            Sim b = Sim.Load(t, null, 5);
            Assert.AreEqual(t, b.Save("t", T0), "ida e volta estavel");
            Assert.AreEqual((a.SatMedia, a.SatAmostras, a.Reclamacoes(Fator.Espera)), (b.SatMedia, b.SatAmostras, b.Reclamacoes(Fator.Espera)));
            for (int e = 0; e < a.Estacoes.Length; e++)
                Assert.AreEqual(a.Estacoes[e].Fila.Select(a.Nota), b.Estacoes[e].Fila.Select(b.Nota), "a nota de quem esta na fila " + (Estacao)e);
            for (int i = 0; i < a.OndaPassos; i++) { a.Tick(Sim.Step); b.Tick(Sim.Step); }
            Assert.AreEqual((a.SatMedia, a.SatAmostras, a.Cash), (b.SatMedia, b.SatAmostras, b.Cash), "reabrir da o mesmo jogo");
            Assert.AreEqual(Enumerable.Range(0, Sim.Fatores).Select(f => a.Reclamacoes((Fator)f)), Enumerable.Range(0, Sim.Fatores).Select(f => b.Reclamacoes((Fator)f)));
        }

        [Test]
        public void Lixo_NoSave_ViraOPadraoLimitado()
        {
            var s = new Sim(null, 3);
            var bot = new Bot(BotProfile.Lento);
            for (int i = 0; i < 40 * Sim.Hz; i++) bot.Step(s);
            Pax p = s.Estacoes.SelectMany(st => st.Fila).First();
            string payload = SaveEnvelope.Unwrap(s.Save("t", T0)).Payload
                + "\nsat=-7,0,-1,-2,-3,-4,-5\nsatm=NaN,999,-999,abc\nsatp=" + p.Id + ",-5,99999,-1,2147483647,-3,15;lixo;1,2,3\n";
            Sim m = Sim.Load(SaveEnvelope.Wrap(payload, "t", T0), null, 3);
            Assert.AreEqual((0L, -1.0), (m.SatAmostras, m.SatMedia), "sem amostra valida: ninguem saiu ainda");
            Assert.IsTrue(Enumerable.Range(0, Sim.Fatores).All(f => m.Reclamacoes((Fator)f) == 0 && m.SatFator((Fator)f) == 0));
            Pax q = m.Estacoes.SelectMany(st => st.Fila).First(x => x.Id == p.Id);
            Assert.AreEqual((0, Sim.SujeiraMax, 0, m.OndaPassos, 0), (q.Espera, q.Sujo, q.FilaBanheiro, q.Atraso, q.EmPe), "cada contador no limite");
            Assert.AreEqual(q.Banheiro, q.Negado, "achou fechado so quem precisava");
            Assert.IsTrue(q.Sentou && q.Reclamou);
            Assert.IsTrue(m.Nota(q) >= 0 && m.Nota(q) <= 100);
        }

        /// <summary>Save real gravado pela main b099e80 (schema 16, com as historias, os suprimentos, a limpeza e a esteira), na onda 15 da semente 7:
        /// abre no 17 com ninguem saido e todos zerados, a ida e volta e estavel, e migrar e o mesmo jogo de jogar ate ali com o codigo novo, fora a
        /// satisfacao, que so le (a cadeia 12 -> 13 -> 14 -> 15 -> 16 esta no SaveTests, no SuprimentosTests e no HistoriasTests).</summary>
        [Test]
        public void MigracaoDoSchema16_SaveReal_SatisfacaoZerada_EOMesmoJogo()
        {
            Sim m = Sim.Load(V16, null, 7);
            Assert.IsFalse(m.SaveRefused, "schema 16 migra, nao e recusado");
            Assert.AreEqual((15, 83700L), (m.Onda, m.Steps));
            Assert.AreEqual((0L, -1.0), (m.SatAmostras, m.SatMedia), "ninguem saiu ainda");
            Pax[] todos = m.Estacoes.SelectMany(st => st.Fila.Concat(st.Atendendo)).ToArray();
            Assert.IsNotEmpty(todos);
            Assert.IsTrue(todos.All(p => p.Espera + p.Sujo + p.FilaBanheiro + p.Atraso + p.EmPe == 0 && !p.Reclamou), "todos chegam zerados");
            string texto = m.Save("0.17.0", T0);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", texto);
            StringAssert.Contains("\nsat=0,", texto);
            Assert.AreEqual(texto, Sim.Load(texto, null, 7).Save("0.17.0", T0), "ida e volta estavel no schema 17");
            var n = new Sim(null, 7);
            var bn = new Bot(BotProfile.Bom);
            for (int i = 0; i < (int)(2790f * Sim.Hz); i++) bn.Step(n);
            Assert.AreEqual(SemSat(n.Save("0.16.0", T0)), SemSat(Sim.Load(V16, null, 7).Save("0.16.0", T0)), "migrar = jogar ate ali com o codigo novo");
            var bm = new Bot(BotProfile.Bom);
            for (int i = 0; i < 600 * Sim.Hz; i++) { bm.Step(m); bn.Step(n); }
            Assert.AreEqual((n.Cash, n.TotalEmbarcados, n.TotalPerdidos), (m.Cash, m.TotalEmbarcados, m.TotalPerdidos), "e o mesmo jogo dali em diante");
            Assert.Greater(m.SatAmostras, 0, "a satisfacao conta dali em diante");
        }

        /// <summary>Sem as linhas da satisfacao e o hash, com os double em 5 casas (o save real saiu do .NET, e o Mono do Unity difere na 8a casa).</summary>
        static string SemSat(string save) => string.Join("\n", save.Split('\n').Where(l => !l.StartsWith("sat") && !l.StartsWith("sha256="))
            .Select(l => System.Text.RegularExpressions.Regex.Replace(l, @"-?\d+\.\d+", x => double.Parse(x.Value, System.Globalization.CultureInfo.InvariantCulture)
                .ToString("F5", System.Globalization.CultureInfo.InvariantCulture))));

        const string V16 =
            "sha256=9cb7510ad9b4ed92926c011ee150b5a76c82b27c5c56bdd5c654373d2efba02b\nschema=16\ncontent=0.16.0\nat=1800000000\nt=83700\ncash=5792\nseed=7\nonda=15" +
            "\nemb=1020\nperd=28\nalbum=1175\nbst=0\nema=9.39020983056686\nsaved=1800000000\nclaim=0\nclaimcash=0\ndia=0\nest.0=5,2\nfila.0=15501;15502;15503;15504" +
            ";15505;15506;15507;15508;15509;15510;15511;15512;15513;15514;15515;15516;15517;15518;15519;15520;15521;15522;15523;15524;15525;15526;15527;15528;15529" +
            ";15530;15531;15532;15533;15534\nat.0=15044:2;15045:32;15500:62\nest.1=5,2\nat.1=15042:40;15043:70\nest.2=0,0\nest.3=5,2\nfila.3=15011;15016;15015;1501" +
            "7;15014;15020;15019;15018;15022;15023;15021;15024;15025;15026;15027;15028;15029;15031;15030;15033;15032;15035;15036;15034;15037;15038;15039;15040;1504" +
            "1\nest.4=5,2\nest.5=5,2\nest.6=5,2\nest.7=0,0\nfunc.0=1,0,0,0,1\nfunc.1=1,0,0,1,1\nfunc.2=4,0,0,3,1\nfunc.3=5,1,0,2,1\nfunc.4=6,0,0,4,1\nfunc.5=7,1,0," +
            "5,1\nvoo.0=46,13,182,1,0\nvoo.1=35,0,0,0,0\naviao.0=50,82200,1,30,-1,0,0,1,0,0\nsrv.0=0:0,0:0,0:0,0:0\naviao.1=100,83700,0,-1,-1,0,0,1,0,0\nsrv.1=0:0," +
            "0:0,0:0,0:0\ntrip.0=0,1,3872,0\ntrip.1=0,1,5400,1\ntrip.2=0,3,1499,-1\ntrip.3=1,1,3842,0\ntrip.4=1,1,5400,1\ntrip.5=1,3,1499,-1\npatio=3\npatioatraso=" +
            "8\naero=1,3,-1\nrotas=127\nprog=6,6\nvr.0=6,46,0,0,0,0,1800,0\nvr.1=6,46,0,0,0,0,3300,0\nav.0=6,15\nav.1=6,15\nstt.0=1,31,5.666665818542242\nstt.1=0,-" +
            "1,27.900001425296068\nstt.3=1,30,8.099999587982893\nreparo=0\nincw=14\ncamp.0=6,10,0,0,3,0,1,0,1,3,0\ncamp.1=1,0,1,40,40,3,0,1,0,2,0\ncamp.2=0,0,0,0,0" +
            ",0,0,0,0,0,0\nmiss=1,0\nmd.0=14,4,0,1\nmd.1=12,30,0,1\nmd.2=6,300,0,1\nms.0=4,30,1,0\nms.1=2,10,0,0\nms.2=3,20,0,0\nms.3=1,0,5,0\nms.4=0,1000,0,0\ncq=" +
            "34849,0,0,30,30,27,4,24,70,0,232,252,25,0,2,1,0,0,0,0,6\ngems=0\npasse=0,0,350,3\natrasada=0\nhoje=1,0,7818,1015,0,1904,2236,1200,0,0,0,0,0,0,33\ndc.0" +
            "=0,644,4570,445,0,528,1000,790,0,0,210,108,54,228,44\nhist=3600,4,-1,0,0,10,0,4,0,0,0,6,0,0,0,0;25200,3,-1,0,8,26,3,0,0,0,0,2,3,7,0,0;41460,1,4,0,15,4" +
            "0,0,7,1,0,0,0,0,0,0,0;43200,5,-1,0,-1,0,0,0,0,1,161,12,0,0,0,0;46800,3,-1,0,16,58,16,0,0,0,0,1,3,18,2,0;62400,0,1,0,23,32,0,0,3,1,0,0,0,0,0,0;69900,3," +
            "-1,0,25,54,7,0,0,0,0,1,3,2,3,0;73800,3,-1,0,26,43,2,0,0,0,0,2,3,16,0,0;79260,1,5,0,29,40,0,0,1,0,0,0,0,0,0,0\nsuj=176,120,0,114,176,12,80\nbh=0\nbhneg" +
            "=0\nzels=1\nzel.0=5,1,0,0,330,0\nsup.0=4,0,0,0\nsup.1=4,0,0,0\nhp=11,15026,1,4095\n";
    }
}
