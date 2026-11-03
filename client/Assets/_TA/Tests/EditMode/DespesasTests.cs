using System;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Regras da Fase C8 (CONTRATO_FASE_C8 §2 a §4): a tabela, a conta no fechamento do dia, a falta de caixa sem divida e o relatorio do dia.
    /// A migracao do schema 8 fica no SaveTests; os portoes, no BalanceTests.</summary>
    public class DespesasTests
    {
        const long T = 1700000000;

        static long DiaPassos(Sim s) => (long)s.OndaPassos * s.T.DiaOndas;

        /// <summary>Do ultimo passo do dia em curso ao 1o do seguinte: s.Events fica com os eventos do passo que fecha a conta.</summary>
        internal static void ViraODia(Sim s)
        {
            if (s.Steps == 0) s.Tick(Sim.Step);
            s.Steps = (s.DiaJogo + 1) * DiaPassos(s) - 1;
            s.Tick(Sim.Step);
            Assert.IsFalse(s.Events.Any(e => e.Tipo == Ev.DiaFechado), "o ultimo passo do dia ainda nao cobra");
            s.Tick(Sim.Step);
        }

        static Evento Fechou(Sim s) => s.Events.Single(e => e.Tipo == Ev.DiaFechado);

        // ------------------------------------------------------------------ a tabela

        [Test]
        public void Tabela_UmaLinhaPorPerfilFuncaoEEstacao()
        {
            Tuning t = Tuning.Padrao;
            Assert.AreEqual((Enum.GetValues(typeof(Perfil)).Length, Enum.GetValues(typeof(Funcao)).Length, 8, 8),
                (t.Salario.Length, t.SalarioTrip.Length, t.Manutencao.Length, t.ManutencaoNivel.Length));
            Assert.IsTrue(t.Salario.Concat(t.SalarioTrip).Concat(t.Manutencao).All(x => x > 0) && t.SalarioPatio > 0, "todo mundo e toda estacao tem custo");
            Assert.AreEqual(0, t.ManutencaoNivel[(int)Estacao.Despacho], "o despacho nao tem melhoria");
        }

        /// <summary>A conta e' de quem esta contratado e das estacoes ativas pelo nivel: fechada pelo marco ou so visual nao cobra.</summary>
        [Test]
        public void Folha_QuemEstaContratado_EAsEstacoesAtivasPeloNivel()
        {
            var s = new Sim(null, 1) { Cash = 1000000 };
            Tuning t = s.T;
            Assert.AreEqual((0L, 3L * t.SalarioTrip[0] + 3L * t.SalarioTrip[1], (long)t.SalarioPatio, (long)t.Manutencao.Take(4).Sum()),
                (s.Folha(Despesa.Equipe), s.Folha(Despesa.Tripulacao), s.Folha(Despesa.Patio), s.Folha(Despesa.Manutencao)), "jogo novo: so a tripulacao inicial, 1 de patio e as 4 estacoes da v0.1");
            Assert.IsTrue(s.Hire(Perfil.Atendente) && s.HirePatio());
            Assert.IsTrue(s.Buy(Estacao.CheckIn, Melhoria.Velocidade) && s.Buy(Estacao.CheckIn, Melhoria.Velocidade) && s.Buy(Estacao.CheckIn, Melhoria.Capacidade));
            Assert.AreEqual(((long)t.Salario[0], 2L * t.SalarioPatio, t.Manutencao.Take(4).Sum() + 3L * t.ManutencaoNivel[0]),
                (s.Folha(Despesa.Equipe), s.Folha(Despesa.Patio), s.Folha(Despesa.Manutencao)));
            Assert.AreEqual(Enumerable.Range(0, 4).Sum(d => s.Folha((Despesa)d)), s.Folha());

            var v = new Sim(new Tuning { Abre = new int[8], RegraBagagem = false }, 1);
            Assert.AreEqual((long)(t.Manutencao.Sum() - t.Manutencao[(int)Estacao.Bagagem]), v.Folha(Despesa.Manutencao), "a bagagem so visual nao cobra");
        }

        // ------------------------------------------------------------------ o fechamento

        [Test]
        public void Fechamento_CobraNoPrimeiroPassoDoDiaSeguinte_UmaVez()
        {
            var s = new Sim(null, 1) { Cash = 10000 };
            long[] folha = Enumerable.Range(0, 4).Select(d => s.Folha((Despesa)d)).ToArray();
            ViraODia(s);
            Evento e = Fechou(s);
            Assert.AreEqual((0, (int)folha.Sum(), 0), (e.A, e.B, e.C));
            Assert.AreEqual(10000 - folha.Sum(), s.Cash, "sem gente nao ha receita: so a conta sai");
            DiaConta d = s.Historico.Single();
            CollectionAssert.AreEqual(folha, d.Despesas);
            Assert.AreEqual((0, folha.Sum(), 0L, 1, false), (d.Dia, d.Pago, d.Falta, s.Hoje.Dia, s.Pausado));
            for (int i = 0; i < 300; i++) s.Tick(Sim.Step);
            Assert.AreEqual((10000 - folha.Sum(), 1), (s.Cash, s.Historico.Count), "o resto do dia nao cobra de novo");
        }

        /// <summary>GDD de direcao §12 e o guardrail do plano de 03/11: faltou caixa, o Cash vai a zero e para; contratar e melhorar pausam ate o caixa
        /// cobrir, e a conta se paga sozinha.</summary>
        [Test]
        public void FaltaDeCaixa_CashNuncaNegativo_PausaEQuitaSozinho()
        {
            var s = new Sim(new Tuning { TripTeto = new[] { 9, 9, 9, 9 } }, 1) { Cash = 100 };
            long folha = s.Folha();
            ViraODia(s);
            Assert.AreEqual((0L, folha - 100, true), (s.Cash, s.Atrasada, s.Pausado));
            Assert.AreEqual((100, (int)(folha - 100)), (Fechou(s).B, Fechou(s).C));

            s.Cash = s.Atrasada - 1;   // da para tudo abaixo, menos para a conta
            Assert.IsFalse(s.Hire(Perfil.Atendente) || s.HireTripulante(Funcao.Comissario) || s.HirePatio(), "contratacao pausada");
            Assert.IsFalse(s.Buy(Estacao.CheckIn, Melhoria.Velocidade) || s.BuyPatio(Servico.Limpeza, Melhoria.Velocidade), "melhoria pausada");
            s.Tick(Sim.Step);
            Assert.AreEqual(folha - 100, s.Atrasada, "nao paga em parte");

            s.Cash = s.Atrasada + 500;
            s.Tick(Sim.Step);
            Assert.AreEqual((500L, 0L, false), (s.Cash, s.Atrasada, s.Pausado));
            Evento q = s.Events.Single(e => e.Tipo == Ev.ContaQuitada);
            Assert.AreEqual((0, (int)(folha - 100)), (q.A, q.B));
            Assert.AreEqual((folha, 0L), (s.Historico[0].Pago, s.Historico[0].Falta));
            Assert.IsTrue(s.Hire(Perfil.Atendente), "a pausa acabou");
        }

        /// <summary>O que faltou nao se acumula: o fechamento seguinte perdoa e cobra so o dia dele. Nunca ha divida maior que uma conta.</summary>
        [Test]
        public void Falta_PerdoadaNoFechamentoSeguinte_NuncaViraDivida()
        {
            var s = new Sim(null, 1);
            long folha = s.Folha();
            for (int dia = 0; dia < 10; dia++)
            {
                ViraODia(s);
                Assert.AreEqual((0L, folha), (s.Cash, s.Atrasada), "dia " + dia);
            }
            Assert.IsTrue(s.Historico.All(d => d.Pago == 0 && d.Falta == folha));
        }

        [Test]
        public void RegraDesligada_SoORelatorio()
        {
            var s = new Sim(new Tuning { RegraDespesas = false }, 1) { Cash = 500 };
            ViraODia(s);
            Assert.AreEqual((0, 0, 0), (Fechou(s).A, Fechou(s).B, Fechou(s).C));
            Assert.AreEqual((500L, 0L, 0L, false), (s.Cash, s.Folha(), s.Historico[0].Despesa, s.Pausado));
        }

        [Test]
        public void Historico_GuardaOsUltimos7Dias()
        {
            var s = new Sim(null, 1) { Cash = 1000000 };
            for (int i = 0; i < 9; i++) ViraODia(s);
            CollectionAssert.AreEqual(Enumerable.Range(2, Sim.DiasHistorico), s.Historico.Select(d => d.Dia));
            Assert.AreEqual(9, s.Hoje.Dia);
        }

        // ------------------------------------------------------------------ o relatorio

        /// <summary>O relatorio do dia (o que o painel de operacoes le) bate com o que os eventos pagaram, fonte por fonte.</summary>
        [Test]
        public void Relatorio_ReceitaPorFonte_BateComOsEventos()
        {
            var s = new Sim(null, 3);
            var b = new Bot(BotProfile.Guiado);
            long tarifas = 0, vendas = 0, bonus = 0, premios = 0, pago = 0;
            int fechados = 0;
            for (int i = 0; i < 12 * s.OndaPassos; i++)
            {
                b.Step(s);
                foreach (Evento e in s.Events)
                    switch (e.Tipo)
                    {
                        case Ev.Embarcou: tarifas += e.B; break;
                        case Ev.Venda: vendas += e.B; break;
                        case Ev.Bonus: bonus += e.B; break;
                        case Ev.ObjetivoConcluido: case Ev.MissaoConcluida: premios += e.C; break;
                        case Ev.PasseNivel: if (s.T.Temporadas[e.A].Gratis[e.B - 1].Premio == Premio.Cash) premios += e.C; break;
                        case Ev.PasseNivel: if (s.T.Temporadas[e.A].Gratis[e.B - 1].Premio == Premio.Cash) premios += e.C; break;
                        case Ev.DiaFechado: pago += e.B; fechados++; break;
                        case Ev.ContaQuitada: pago += e.B; break;
                    }
            }
            Assert.AreEqual(1, fechados, "12 ondas: o dia 0 fechou");
            DiaConta[] dias = s.Historico.Append(s.Hoje).ToArray();
            long Soma(Fonte f) => dias.Sum(d => d.Receitas[(int)f]);
            Assert.AreEqual((tarifas, vendas, bonus, premios), (Soma(Fonte.Tarifas), Soma(Fonte.Bagagem) + Soma(Fonte.Loja) + Soma(Fonte.Alimentacao), Soma(Fonte.Bonus), Soma(Fonte.Premios)));
            Assert.Greater(Soma(Fonte.Loja) + Soma(Fonte.Alimentacao), 0, "o Guiado poe gente na loja");
            Assert.AreEqual((pago, 0L, 0L), (dias.Sum(d => d.Pago), Soma(Fonte.Cofre), Soma(Fonte.Compras)));
            foreach (DiaConta d in dias) Assert.AreEqual(d.Receita - d.Despesa, d.Lucro);
            Assert.Greater(s.Historico[0].Lucro, 0, "o 1o dia do Guiado da lucro");
        }

        [Test]
        public void Relatorio_CofreECompras_TemFontePropriaENaoMexemNaConta()
        {
            var s = new Sim(null, 1) { RateEma = 1 };
            s.Tick(Sim.Step);
            long folha = s.Folha();
            long cofre = s.ApplyOffline(3600, T);
            Assert.AreEqual(Credito.Creditado, s.Creditar("GPA.1", "welcome_bundle_01"));
            Assert.AreEqual((cofre, 1000L), (s.Hoje.Receitas[(int)Fonte.Cofre], s.Hoje.Receitas[(int)Fonte.Compras]));
            Assert.AreEqual(folha, s.Folha(), "comprar Cash nao muda a conta");
        }

        // ------------------------------------------------------------------ invariantes

        /// <summary>Sem derrota inevitavel: com a conta 1 000 x maior que a receita, o Cash nunca fica negativo, todo voo sai e o terminal continua
        /// embarcando no ultimo dia. So contratar e melhorar param.</summary>
        [Test]
        public void Invariante_C8_ContaImpagavel_NaoTrancaOJogo()
        {
            var t = new Tuning { Salario = Enumerable.Repeat(100000, 6).ToArray(), Manutencao = Enumerable.Repeat(100000, 8).ToArray() };
            for (int seed = 1; seed <= 10; seed++)
            {
                var s = new Sim(t, seed);
                var b = new Bot(BotProfile.Lento);
                int voos = 0; long embarcouNoFim = 0;
                for (int i = 0; i < 24 * s.OndaPassos; i++)
                {
                    b.Step(s);
                    Assert.GreaterOrEqual(s.Cash, 0, "semente " + seed);
                    voos += s.Events.Count(e => e.Tipo == Ev.VooPartiu);
                    if (s.DiaJogo == 2) embarcouNoFim += s.Events.Count(e => e.Tipo == Ev.Embarcou);
                }
                Assert.AreEqual(48, voos, "semente " + seed + ": todo voo sai");
                Assert.Greater(embarcouNoFim, 0, "semente " + seed + ": o terminal continua embarcando");
                Assert.IsTrue(s.Pausado && s.Atrasada <= s.Historico.Last().Despesa, "semente " + seed + ": a falta e' de uma conta so");
            }
        }

        /// <summary>A conta nao depende de voo: quem embarca todo mundo e quem perde todos os voos pagam o mesmo pelo mesmo aeroporto. Embarcar mais so
        /// cobra mais quando o marco abre estacao nova (a conta e' do aeroporto maior), nunca porque um voo se perdeu: aqui o marco fica fechado.</summary>
        [Test]
        public void Invariante_C8_ContaNaoDependeDeVoo()
        {
            var t = new Tuning { Marcos = new[] { 99999, 99999, 99999 } };
            Sim a = new Sim(t, 1) { Cash = 100000 }, b = new Sim(t, 1) { Cash = 100000 };
            for (long i = 0; i < DiaPassos(a) + 1; i++)
            {
                foreach (Estacao e in new[] { Estacao.CheckIn, Estacao.Seguranca, Estacao.Portao }) a.Tap(e);
                a.Tick(Sim.Step);
                b.Tick(Sim.Step);
            }
            Assert.Less(a.TotalPerdidos, b.TotalPerdidos);
            Assert.AreEqual(b.Historico.Single().Despesas, a.Historico.Single().Despesas);
            Assert.AreEqual(b.Historico[0].Pago, a.Historico[0].Pago);
        }

        /// <summary>Offline continua sem perda e com teto de 2 h: com o jogo fechado o dia nao vira, entao nada se cobra, e o cofre entra inteiro.</summary>
        [Test]
        public void Invariante_C8_Offline_NaoCobraNemViraODia()
        {
            var s = new Sim(null, 1) { Cash = 0 };
            s.Tick(Sim.Step);
            s.Steps = DiaPassos(s) - 1;
            s.RateEma = 1;
            Sim b = Sim.Load(s.Save("x", T), null, 1);
            long pago = b.ApplyOffline(10 * 3600, T + 10 * 3600);
            Assert.AreEqual(Math.Min((long)(0.25 * 7200), b.OfflineMaxCash()), pago, "teto de 2 h");
            Assert.AreEqual((pago, s.Steps, 0, false), (b.Cash, b.Steps, b.Historico.Count, b.Events.Any(e => e.Tipo == Ev.DiaFechado)));
            b.Tick(Sim.Step);
            Assert.AreEqual(0, b.Historico.Count, "o ultimo passo do dia ainda nao cobra");
            b.Tick(Sim.Step);
            Assert.AreEqual(Math.Max(0, pago - b.Historico[0].Despesa), b.Cash, "a conta sai so no 1o passo jogado do dia seguinte");
        }

        // ------------------------------------------------------------------ save

        /// <summary>Fechar e abrir no meio de cada onda da o mesmo jogo, com as contas: o relatorio e a conta atrasada estao no save.</summary>
        [Test]
        public void Save_ReabrirNoMeioDaOnda_DaOMesmoJogo()
        {
            foreach (BotProfile p in new[] { BotProfile.Bom, BotProfile.Lento })
            {
                var t = new Tuning { Salario = Enumerable.Repeat(900, 6).ToArray() };   // aperta: o Lento fica com conta atrasada
                Bot.Resultado a = Bot.Medir(p, 5, 26, t), b = Bot.Medir(p, 5, 26, t, true);
                Assert.AreEqual((a.Receita, a.Despesas, a.Apertos, a.Embarque), (b.Receita, b.Despesas, b.Apertos, b.Embarque), p.Name);
                Assert.Greater(a.Despesas, 0);
            }
            var s = new Sim(null, 1) { Cash = 100 };
            ViraODia(s);
            ViraODia(s);
            string texto = s.Save("x", T);
            Sim r = Sim.Load(texto, null, 1);
            Assert.AreEqual((s.Atrasada, s.Hoje.Dia, 2), (r.Atrasada, r.Hoje.Dia, r.Historico.Count));
            Assert.AreEqual(texto, r.Save("x", T), "ida e volta estavel");
        }

        [Test]
        public void LixoDoRelatorio_ViraPadraoLimitado()
        {
            const long t = 2 * 8 * 5400 + 90;   // dia 2
            string payload = "t=" + t + "\natrasada=999999\nhoje=2,0,1,2,3,4,5,6,7,8,0,0,0,0\n" +
                "dc.0=0,999,1,2,3,4,5,6,7,9000000000000000000,10,10,10,10\ndc.1=0,1,1,1,1,1,1,1,1,1,1,1,1,1\ndc.2=1,5,-7,0,0,0,0,0,0,0,30,0,0,0\ndc.3=1,1\n";
            Sim s = Sim.Load(SaveEnvelope.Wrap(payload, "x", 1));
            Assert.IsFalse(s.SaveRefused);
            Assert.AreEqual(new[] { 0, 1 }, s.Historico.Select(d => d.Dia).ToArray(), "dia fora de ordem ou linha curta sai");
            Assert.AreEqual((40L, 1L << 50, 0L, 5L), (s.Historico[0].Pago, s.Historico[0].Receitas[7], s.Historico[1].Receitas[0], s.Historico[1].Pago), "pago ate a conta; receita no teto e nunca negativa");
            Assert.AreEqual((25L, 2), (s.Atrasada, s.Hoje.Dia), "atrasada no maximo o que faltou no ultimo dia");

            Sim f = Sim.Load(SaveEnvelope.Wrap("t=90\natrasada=50\nhoje=9,0,1,1,1,1,1,1,1,1,0,0,0,0\ndc.0=-1,0,0,0,0,0,0,0,0,0,9,0,0,0\n", "x", 1));
            Assert.AreEqual((-1, 0, 0L), (f.Hoje.Dia, f.Historico.Count, f.Atrasada), "dia no futuro ou negativo: relatorio vazio, nada atrasado");
        }
    }
}
