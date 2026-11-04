using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Frente F: limpeza e banheiro (GDD §4.1 linha F; Direcao §10 e §22). Regras e invariantes em Sim.Limpeza.cs.</summary>
    public class LimpezaTests
    {
        const long T0 = 1_800_000_000;

        static int P(float seg) => System.Math.Max(1, (int)System.Math.Round(seg * Sim.Hz));

        static void Anda(Sim s, int passos) { for (int i = 0; i < passos; i++) s.Tick(Sim.Step); }

        /// <summary>Um Sim parado no 1o passo, sem gente chegando nem banheiro sendo usado (passageiros fixos em 0 nao existem: chega so 1 por voo,
        /// longe), para mexer na sujeira a mao.</summary>
        static Sim Parado(Tuning t = null)
        {
            var s = new Sim(t ?? new Tuning(), 1);
            s.Tick(Sim.Step);
            System.Array.Clear(s.Sujeira, 0, s.Sujeira.Length);
            return s;
        }

        [Test]
        public void JogoNovo_UmZeladorNoDeposito_TudoLimpo_SalarioNaFolhaDaEquipe()
        {
            var s = new Sim(null, 1);
            Assert.AreEqual(1, s.Limpeza.Count);
            Zelador z = s.Limpeza[0];
            Assert.AreEqual((Area.Banheiro, Faxina.Livre, 0), (z.Em, z.Fase, z.Andando), "comeca livre no deposito, que fica no banheiro");
            Assert.IsTrue(s.Sujeira.All(x => x == 0));
            Assert.AreEqual((2, true, 0), (s.Banheiro.Cabines, s.BanheiroAberto, s.Banheiro.Pessoas));
            Assert.AreEqual((long)s.T.SalarioLimpeza, s.Folha(Despesa.Equipe), "a v0.1 nao contrata ninguem: so a limpeza na folha da equipe");
            Assert.AreEqual(3, s.TetoLimpeza);
        }

        [Test]
        public void RegraDesligada_SemBanheiroSemSujeiraSemEquipe()
        {
            var t = new Tuning { RegraLimpeza = false };
            var s = new Sim(t, 2);
            var bot = new Bot(BotProfile.Bom);
            for (int i = 0; i < 2 * s.OndaPassos; i++)
            {
                bot.Step(s);
                Assert.AreEqual(0, s.Banheiro.Pessoas, "ninguem vai ao banheiro");
            }
            Assert.AreEqual((0, 0L, -1L), (s.Limpeza.Count, s.Folha(Despesa.Equipe) - s.Staff.Sum(f => (long)s.Salario(f.Perfil, f.Experiencia)), s.CustoLimpeza()));
            Assert.IsTrue(s.Sujeira.All(x => x == 0), "nada suja");
            Assert.IsFalse(s.HireLimpeza() || s.Limpar(Area.CheckIn));
            Sim r = Sim.Load(s.Save("t", T0), t, 2);
            Assert.AreEqual(0, r.Limpeza.Count, "o save do jogo sem a regra nao traz equipe de limpeza");
        }

        [Test]
        public void AtendimentoSujaASalaDaEstacao_OficinaEDespachoNao()
        {
            Assert.AreEqual((0, 3, 5, -1, -1), (Sim.AreaDe(Estacao.CheckIn), Sim.AreaDe(Estacao.Portao), Sim.AreaDe(Estacao.Alimentacao), Sim.AreaDe(Estacao.Manutencao), Sim.AreaDe(Estacao.Despacho)));
            var s = new Sim(null, 1) { Cash = 1000 };
            Assert.IsTrue(s.Hire());   // Ana no check-in
            int antes = 0;
            for (int i = 0; i < 40 * Sim.Hz && s.Events.All(e => e.Tipo != Ev.Embarcou); i++)
            {
                s.Tick(Sim.Step);
                int atendidos = s.Events.Count(e => e.Tipo == Ev.Toque);   // nao ha toque: Ana atende
                Assert.AreEqual(0, atendidos);
                if (s.Sujeira[(int)Area.CheckIn] > antes) { Assert.AreEqual(antes + s.T.SujaPorPax[(int)Area.CheckIn], s.Sujeira[(int)Area.CheckIn], "um passageiro de cada vez"); break; }
                antes = s.Sujeira[(int)Area.CheckIn];
            }
            Assert.Greater(s.Sujeira[(int)Area.CheckIn], 0, "o check-in sujou");
        }

        /// <summary>O caminho inteiro de quem vai ao banheiro: ida, fila, cabine (suja), volta e a fila do portao, nos passos do Tuning.</summary>
        [Test]
        public void Banheiro_IdaFilaCabineVolta_NosPassosDoTuning_ESujaPorUso()
        {
            var s = new Sim(null, 4) { Cash = 100000 };
            var bot = new Bot(BotProfile.Bom);
            Pax p = null;
            for (int i = 0; i < 2 * s.OndaPassos && p == null; i++)
            {
                bot.Step(s);
                p = s.Banheiro.Indo.FirstOrDefault(x => x.Resto == s.BanheiroAndarPassos - 1);   // entrou neste passo (o passo da saida ja conta na ida)
            }
            Assert.IsNotNull(p, "alguem foi ao banheiro em 2 ondas");
            Assert.IsTrue(p.Banheiro);
            Voo v = s.VooDe(p);
            Assert.Greater(v.AbrePasso - s.PassoNaOnda, 2 * s.BanheiroAndarPassos + s.UsoPassos + P(s.T.BanheiroFolga) - 2, "so vai quem volta antes de o embarque abrir");
            int ida = 0, uso = 0, volta = 0;
            int suja = s.Sujeira[(int)Area.Banheiro];
            while (!s.St(Estacao.Portao).Fila.Contains(p) && !s.St(Estacao.Portao).Atendendo.Contains(p))
            {
                Assert.AreEqual(1, new[] { s.Banheiro.Indo, s.Banheiro.Fila, s.Banheiro.Usando, s.Banheiro.Voltando }.Count(l => l.Contains(p)), "em um lugar so");
                if (s.Banheiro.Indo.Contains(p)) ida++;
                if (s.Banheiro.Usando.Contains(p)) uso++;
                if (s.Banheiro.Voltando.Contains(p)) { if (volta == 0) Assert.GreaterOrEqual(s.Sujeira[(int)Area.Banheiro], System.Math.Min(Sim.SujeiraMax, suja + s.T.SujaPorPax[(int)Area.Banheiro]) - s.T.LimpaPorSeg / Sim.Hz, "o uso sujou"); volta++; }
                suja = s.Sujeira[(int)Area.Banheiro];
                bot.Step(s);
            }
            Assert.AreEqual((s.BanheiroAndarPassos, s.UsoPassos, s.BanheiroAndarPassos), (ida + 1, uso, volta));
        }

        /// <summary>Ninguem perde o voo por causa do banheiro: em 5 sementes x 4 ondas com o Bom e o Lento, nenhum voo fecha com alguem dele la.</summary>
        [Test]
        public void Invariante_NinguemFicaNoBanheiroQuandoOVooFecha()
        {
            int usos = 0;
            foreach (BotProfile b in new[] { BotProfile.Bom, BotProfile.Lento })
                for (int seed = 1; seed <= 5; seed++)
                {
                    var s = new Sim(null, seed);
                    var bot = new Bot(b);
                    for (int i = 0; i < 4 * s.OndaPassos; i++)
                    {
                        foreach (Voo v in s.Voos)
                            if (v != null && !v.Partiu && s.PassoNaOnda + 1 >= v.FechaPasso)
                                Assert.IsFalse(new[] { s.Banheiro.Indo, s.Banheiro.Fila, s.Banheiro.Usando, s.Banheiro.Voltando }.Any(l => l.Any(p => p.Onda == v.Onda && p.Voo == v.Indice)),
                                    b.Name + " semente " + seed + ": voo " + v.Id + " fecha com gente no banheiro");
                        usos += s.Banheiro.Usando.Count(p => p.Resto == s.UsoPassos);
                        bot.Step(s);
                    }
                }
            Assert.Greater(usos, 50, "o banheiro e' usado");
        }

        /// <summary>Com a folga, ninguem fica la quando o voo fecha; se ficar (portao adiantado, save editado), o voo leva: perdeu, como numa fila.</summary>
        [Test]
        public void FechaOVoo_LevaQuemFicouNoBanheiro()
        {
            Sim s = Parado();
            Pax p = s.NovoPax(Sim.PaxId(0, 0, 400));
            p.Resto = p.Total = 100000;
            s.Banheiro.Indo.Add(p);
            Voo v = s.Voos[0];
            while (!v.Partiu) s.Tick(Sim.Step);
            Assert.AreEqual(0, s.Banheiro.Pessoas);
        }

        [Test]
        public void Escala_VaiAMaisSujaAcimaDoGatilho_SoLimpaDepoisDeChegar_ELiberaNoZero()
        {
            Sim s = Parado();
            s.Sujeira[(int)Area.CheckIn] = 300;
            s.Sujeira[(int)Area.Alimentacao] = 500;
            s.Sujeira[(int)Area.Loja] = s.T.LimpezaGatilho - 1;   // abaixo do gatilho: a escala nao pega
            s.Tick(Sim.Step);
            Zelador z = s.Limpeza[0];
            Assert.AreEqual((Area.Alimentacao, Area.Banheiro, Faxina.Indo, false), (z.Em, z.De, z.Fase, z.Ordem));
            Assert.IsTrue(s.Events.Any(e => e.Tipo == Ev.LimpezaTarefa && e.A == 0 && e.B == (int)Area.Alimentacao && e.C == 0));
            Assert.AreEqual(P(1f + (s.T.LimpezaTrecho[(int)Area.Banheiro] + s.T.LimpezaTrecho[(int)Area.Alimentacao]) / s.T.LimpezaVel), z.AndarTotal, "o caminho da planta: banheiro -> portao -> alimentacao");
            Assert.AreEqual(14f, s.Metros(Area.Banheiro, Area.Alimentacao));
            Assert.AreEqual(12f, s.Metros(Area.Bagagem, Area.Loja), "bagagem -> check-in -> seguranca -> loja");
            int ida = z.Andando;
            while (z.Fase == Faxina.Indo)
            {
                Assert.AreEqual(500, s.Sujeira[(int)Area.Alimentacao], "a sujeira so cai com o zelador la (a animacao segue o estado)");
                s.Tick(Sim.Step);
                ida--;
            }
            Assert.AreEqual(0, ida, "andou o AndarTotal inteiro");
            Assert.IsTrue(s.Events.Any(e => e.Tipo == Ev.LimpezaChegou && e.B == (int)Area.Alimentacao));
            int n = 0;
            while (z.Fase == Faxina.Limpando) { s.Tick(Sim.Step); n++; }
            Assert.AreEqual((0, Faxina.Livre), (s.Sujeira[(int)Area.Alimentacao], z.Fase));
            Assert.AreEqual(500 / (s.T.LimpaPorSeg / Sim.Hz), n, "LimpaPorSeg por s");
            Assert.IsTrue(s.Events.Any(e => e.Tipo == Ev.Limpou && e.B == (int)Area.Alimentacao));
            s.Tick(Sim.Step);
            Assert.AreEqual((Area.CheckIn, Area.Alimentacao, Faxina.Indo), (z.Em, z.De, z.Fase), "livre, pega a proxima acima do gatilho; a loja fica");
        }

        [Test]
        public void Ordem_DoJogador_VemAntesDaEscala_EsperaUmLivre_ERecusaOQueNaoFazSentido()
        {
            Sim s = Parado();
            Assert.IsFalse(s.Limpar(Area.CheckIn), "area limpa");
            Assert.IsFalse(s.Limpar((Area)99));
            s.Sujeira[(int)Area.CheckIn] = 40;   // abaixo do gatilho: so a ordem leva
            Assert.IsTrue(s.Limpar(Area.CheckIn));
            Zelador z = s.Limpeza[0];
            Assert.AreEqual((Area.CheckIn, Faxina.Indo, true), (z.Em, z.Fase, z.Ordem));
            Assert.IsTrue(s.Events.Any(e => e.Tipo == Ev.LimpezaPedida && e.A == (int)Area.CheckIn && e.B == 0));
            Assert.IsFalse(s.Limpar(Area.CheckIn), "ja tem zelador la");
            s.Sujeira[(int)Area.Portao] = 900;
            s.Sujeira[(int)Area.Loja] = 30;
            Assert.IsTrue(s.Limpar(Area.Loja), "ninguem livre: espera");
            Assert.IsFalse(s.Limpar(Area.Loja), "ja esta na espera");
            Assert.AreEqual(new[] { Area.Loja }, s.Ordens.ToArray());
            Assert.IsTrue(s.Events.Any(e => e.Tipo == Ev.LimpezaPedida && e.A == (int)Area.Loja && e.B == -1));
            while (z.Em == Area.CheckIn) s.Tick(Sim.Step);
            Assert.AreEqual((Area.Loja, true), (z.Em, z.Ordem), "a ordem antes do portao, que esta bem mais sujo");
            Assert.IsEmpty(s.Ordens);
        }

        [Test]
        public void Interdicao_NaSujeiraMaxima_AFilaVolta_OZeladorVaiPrimeiro_EReabreLimpo()
        {
            Sim s = Parado();
            var fila = new[] { s.NovoPax(Sim.PaxId(0, 1, 0)), s.NovoPax(Sim.PaxId(0, 1, 1)) };
            s.Banheiro.Fila.AddRange(fila);
            s.Sujeira[(int)Area.Banheiro] = s.T.SujeiraInterdita - s.T.SujaPorPax[(int)Area.Banheiro];
            s.Sujeira[(int)Area.Portao] = Sim.SujeiraMax;   // mais suja que o banheiro: mesmo assim o banheiro fechado vem antes
            s.Limpeza[0].Fase = Faxina.Limpando; s.Limpeza[0].Em = Area.CheckIn; s.Sujeira[(int)Area.CheckIn] = 2 * (s.UsoPassos + 2);   // ocupado ate depois da interdicao
            s.Tick(Sim.Step);   // os 2 entram nas cabines
            Assert.AreEqual(2, s.Banheiro.Usando.Count);
            Anda(s, s.UsoPassos);
            Assert.IsTrue(s.Banheiro.Interditado, "o 1o uso chegou ao teto");
            Assert.IsTrue(s.Events.Any(e => e.Tipo == Ev.BanheiroInterditado && e.A == 1) || s.Banheiro.Interditado);
            Assert.IsFalse(s.BanheiroAberto);
            Assert.AreEqual(2, s.Banheiro.Voltando.Count, "quem estava na cabine termina e volta");
            s.Banheiro.Fila.Add(s.NovoPax(Sim.PaxId(0, 1, 2)));
            s.Tick(Sim.Step);
            Assert.AreEqual((0, 1L), (s.Banheiro.Fila.Count, s.Banheiro.Negados), "a fila de um banheiro fechado volta ao portao");
            Zelador z = s.Limpeza[0];
            while (z.Em == Area.CheckIn) s.Tick(Sim.Step);
            Assert.AreEqual(Area.Banheiro, z.Em, "o banheiro fechado antes da area mais suja");
            while (s.Banheiro.Interditado) s.Tick(Sim.Step);
            Assert.AreEqual((0, Faxina.Livre), (s.Sujeira[(int)Area.Banheiro], z.Fase), "reabre so limpo");
            Assert.IsTrue(s.Events.Any(e => e.Tipo == Ev.BanheiroInterditado && e.A == 0));
        }

        [Test]
        public void Contratacao_PrecoFixo_TetoDe3_PausaComAContaAtrasada_FolhaSobe()
        {
            var s = new Sim(null, 1) { Cash = 1000 };
            Assert.AreEqual(60L, s.CustoLimpeza());
            long folha = s.Folha(Despesa.Equipe);
            Assert.IsTrue(s.HireLimpeza());
            Assert.AreEqual((940L, 2, 150L, folha + s.T.SalarioLimpeza), (s.Cash, s.Limpeza.Count, s.CustoLimpeza(), s.Folha(Despesa.Equipe)));
            Assert.IsTrue(s.Events.Any(e => e.Tipo == Ev.LimpezaContratada && e.A == 2 && e.B == 60));
            Assert.AreEqual((Area.Banheiro, Faxina.Livre, 1), (s.Limpeza[1].Em, s.Limpeza[1].Fase, s.Limpeza[1].Id));
            Assert.IsTrue(s.HireLimpeza());
            Assert.AreEqual((-1L, 3), (s.CustoLimpeza(), s.Limpeza.Count));
            Assert.IsFalse(s.HireLimpeza(), "no teto");

            var p = new Sim(new Tuning { Salario = Enumerable.Repeat(100000, 6).ToArray() }, 1) { Cash = 100000 };
            Assert.IsTrue(p.Hire());
            p.Cash = 100;
            DespesasTests.ViraODia(p);
            Assert.IsTrue(p.Pausado);
            p.Cash = 1000;
            Assert.IsFalse(p.HireLimpeza(), "contratar pausa com a conta atrasada");
        }

        /// <summary>Gravar no meio de tudo (gente no banheiro, zelador indo, ordem na espera, sujeira) e reabrir da o mesmo jogo, passo a passo.</summary>
        [Test]
        public void Save_NoMeioDoBanheiroEDaLimpeza_IdaEVoltaEstavel_EOMesmoJogo()
        {
            var s = new Sim(null, 4) { Cash = 100000 };
            var bot = new Bot(BotProfile.Bom);
            for (int i = 0; i < 2 * s.OndaPassos && !(s.Banheiro.Indo.Count > 0 && s.Banheiro.Usando.Count > 0); i++) bot.Step(s);
            Assert.IsTrue(s.Banheiro.Indo.Count > 0 && s.Banheiro.Usando.Count > 0, "gente indo e na cabine");
            s.Cash += 1000;
            Assert.IsTrue(s.HireLimpeza());
            s.Sujeira[(int)Area.Bagagem] = 700;

            s.Limpeza[0].Fase = Faxina.Limpando; s.Limpeza[0].Em = Area.Loja; s.Sujeira[(int)Area.Loja] = 600;
            s.Tick(Sim.Step);
            Assert.IsTrue(s.Limpar(Area.CheckIn) || s.Sujeira[(int)Area.CheckIn] == 0 || s.TemZelador(Area.CheckIn));
            s.Sujeira[(int)Area.Seguranca] = 10;
            s.Limpar(Area.Seguranca);
            s.Banheiro.Negados = 7;
            string texto = s.Save("t", T0);
            Sim r = Sim.Load(texto, null, 4);
            Assert.AreEqual(texto, r.Save("t", T0), "ida e volta estavel");
            Assert.AreEqual((s.Banheiro.Pessoas, s.Limpeza.Count, s.Ordens.Count, 7L), (r.Banheiro.Pessoas, r.Limpeza.Count, r.Ordens.Count, r.Banheiro.Negados));
            for (int i = 0; i < s.OndaPassos / 2; i++)
            {
                s.Tick(Sim.Step); r.Tick(Sim.Step);
            }
            Assert.AreEqual(s.Save("t", T0 + 90), r.Save("t", T0 + 90), "reabrir da o mesmo jogo");
        }

        [Test]
        public void SemAsChavesDaFrenteF_AEquipeDoJogoNovo_TudoLimpo()
        {
            var s = new Sim(null, 3) { Cash = 5000 };
            Anda(s, s.OndaPassos / 2);
            SaveEnvelope.Result env = SaveEnvelope.Unwrap(s.Save("t", T0));
            string[] chaves = { "suj=", "bh=", "bhneg=", "zels=", "zel.", "lord=", "bh." };
            string payload = string.Join("\n", env.Payload.Split('\n').Where(l => !chaves.Any(l.StartsWith)));
            Sim m = Sim.Load(SaveEnvelope.Wrap(payload, "t", T0), null, 3);
            Assert.IsFalse(m.SaveRefused);
            Assert.AreEqual((1, Faxina.Livre, 0, false), (m.Limpeza.Count, m.Limpeza[0].Fase, m.Banheiro.Pessoas, m.Banheiro.Interditado));
            Assert.IsTrue(m.Sujeira.All(x => x == 0));
        }

        [Test]
        public void Lixo_NoSave_ViraOPadraoLimitado()
        {
            var s = new Sim(null, 3);
            Anda(s, 10);
            SaveEnvelope.Result env = SaveEnvelope.Unwrap(s.Save("t", T0));
            string lixo = env.Payload.Replace("zels=1", "zels=99") + "\nsuj=-5,99999,abc\nbh=1\nbhneg=-9\nzel.0=77,-3,9,99999,-1,1\nzel.1=lixo\nlord=3,3,99,-1,2\n";
            Sim m = Sim.Load(SaveEnvelope.Wrap(lixo, "t", T0), null, 3);
            Assert.AreEqual(m.TetoLimpeza, m.Limpeza.Count, "a equipe no teto");
            Assert.IsTrue(m.Sujeira.All(x => x >= 0 && x <= Sim.SujeiraMax));
            Assert.AreEqual((0, Sim.SujeiraMax), (m.Sujeira[0], m.Sujeira[1]));
            Assert.AreEqual(0L, m.Banheiro.Negados);
            Assert.IsFalse(m.Banheiro.Interditado, "interditado sem sujeira no banheiro nao fica");
            Zelador z = m.Limpeza[0];
            Assert.AreEqual((Area.Banheiro, Area.CheckIn, Faxina.Livre, 0, false), (z.Em, z.De, z.Fase, z.Andando, z.Ordem), "fase invalida vira Livre, area fora da faixa vai para a ponta");
            Assert.AreEqual(new[] { Area.Portao, Area.Loja }, m.Ordens.ToArray());
            m.Tick(Sim.Step);
        }

        /// <summary>Invariantes no bot lento (3 sementes x 3 ondas): cada passageiro num lugar so, cabines no teto, sujeira na faixa e so caindo com
        /// zelador limpando la, equipe no teto, e o Cash nunca negativo.</summary>
        [Test]
        public void Invariantes_NoBotLento_TresOndas()
        {
            for (int seed = 1; seed <= 3; seed++)
            {
                var s = new Sim(null, seed);
                var bot = new Bot(BotProfile.Lento);
                var antes = new int[Sim.Areas];
                int limpezas = 0;
                for (int i = 0; i < 3 * s.OndaPassos; i++)
                {
                    bot.Step(s);
                    var ids = new HashSet<int>();
                    foreach (Station st in s.Estacoes) foreach (Pax p in st.Fila.Concat(st.Atendendo)) Assert.IsTrue(ids.Add(p.Id), "passageiro em 2 lugares");
                    foreach (Pax p in s.Banheiro.Indo.Concat(s.Banheiro.Fila).Concat(s.Banheiro.Usando).Concat(s.Banheiro.Voltando)) Assert.IsTrue(ids.Add(p.Id), "passageiro no banheiro e numa fila");
                    Assert.LessOrEqual(s.Banheiro.Usando.Count, s.Banheiro.Cabines);
                    Assert.LessOrEqual(s.Limpeza.Count, s.TetoLimpeza);
                    Assert.GreaterOrEqual(s.Cash, 0);
                    for (int a = 0; a < Sim.Areas; a++)
                    {
                        Assert.IsTrue(s.Sujeira[a] >= 0 && s.Sujeira[a] <= Sim.SujeiraMax);
                        if (s.Sujeira[a] < antes[a])
                            Assert.IsTrue(s.Limpeza.Any(z => z.Em == (Area)a && (z.Fase == Faxina.Limpando || s.Events.Any(e => e.Tipo == Ev.Limpou && e.B == a))), "a sujeira so cai com alguem limpando la");
                        antes[a] = s.Sujeira[a];
                    }
                    foreach (Zelador z in s.Limpeza) Assert.IsTrue(z.Fase != Faxina.Indo || z.Andando > 0 && z.Andando <= z.AndarTotal);
                    limpezas += s.Events.Count(e => e.Tipo == Ev.Limpou);
                }
                Assert.Greater(limpezas, 0, "a escala automatica limpou");
            }
        }
    }
}
