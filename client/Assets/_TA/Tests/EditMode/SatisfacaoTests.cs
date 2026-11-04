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
    }
}
