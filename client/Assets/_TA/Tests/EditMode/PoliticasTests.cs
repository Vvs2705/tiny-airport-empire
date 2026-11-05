using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>
    /// Frente ta-politicas (Sim.Politicas.cs): as 3 regras automaticas que o jogador liga e regula. Hangar no X% de desgaste, reposicao antes do ponto com
    /// teto por dia, reforco da fila mais longa pelo vestiario. Travas: nunca passa do teto, nunca deixa o caixa abaixo da conta do proximo fechamento,
    /// nunca com a conta atrasada, nunca os 2 avioes no hangar, nunca tira ninguem de estacao fora do normal nem da oficina que um aviao precisa; o
    /// reforco nao conta para a missao de mover. Cada acao vai ao registro. As travas estao no mutacao.py ("politica:").
    /// </summary>
    public class PoliticasTests
    {
        static Tuning Calmo() => new Tuning { RegraIncidentes = false, RegraClima = false, RegraEpocas = false, RegraPicos = false, RegraFalhas = false };

        /// <summary>Anda `seg` s e devolve os eventos.</summary>
        static List<Evento> Anda(Sim s, float seg)
        {
            var l = new List<Evento>();
            for (int i = 0; i < (int)Math.Round(seg * Sim.Hz); i++) { s.Tick(Sim.Step); l.AddRange(s.Events); }
            return l;
        }

        static IEnumerable<Evento> Agiu(List<Evento> l, Politica p) => l.Where(e => e.Tipo == Ev.PoliticaAgiu && e.A == (int)p);

        /// <summary>O regional (aviao 0) pousou e o embarque dele nao abriu: onde o hangar vale.</summary>
        static Sim NoPatio(int seed = 1)
        {
            var s = new Sim(Calmo(), seed) { Cash = 5000 };
            for (int i = 0; i < 400 * Sim.Hz && !s.PodeHangar(0); i++) s.Tick(Sim.Step);
            Assert.IsTrue(s.PodeHangar(0), "o regional pousou antes do embarque");
            return s;
        }

        [Test]
        public void JogoNovo_TodasDesligadas_NoXPadrao_ESemChaveNoSave()
        {
            var s = new Sim(null, 1);
            Assert.AreEqual(new[] { false, false, false }, s.PolLigada);
            Assert.AreEqual((80, 24, 10), (s.PoliticaX(Politica.Hangar), s.PoliticaX(Politica.Estoque), s.PoliticaX(Politica.Reforco)), "o X do meio da lista");
            Anda(s, 10f);
            StringAssert.DoesNotContain("\npol=", s.Save("t", 1), "tudo no padrao: o save e' o de antes");
            Assert.IsFalse(s.LigaPolitica(Politica.Hangar, false), "ja estava desligada");
            Assert.IsFalse(s.RegulaPolitica(Politica.Hangar, 3), "fora da lista");
            Assert.IsFalse(s.LigaPolitica((Politica)3, true));
            Assert.IsTrue(s.RegulaPolitica(Politica.Hangar, 2));
            Assert.AreEqual(90, s.PoliticaX(Politica.Hangar));
        }

        [Test]
        public void Hangar_NoX_VaiAoHangar_CobraRegistraEAvisa_AbaixoNao()
        {
            Sim s = NoPatio();
            s.Avioes[0].Desgaste = 80;
            s.LigaPolitica(Politica.Estoque, true);
            Assert.IsEmpty(Agiu(Anda(s, 1f), Politica.Hangar), "desligada: nada (com outra ligada)");
            s.LigaPolitica(Politica.Hangar, true);
            s.RegulaPolitica(Politica.Hangar, 2);   // 90%
            Assert.IsEmpty(Agiu(Anda(s, 1f), Politica.Hangar), "abaixo do X: nada");
            s.RegulaPolitica(Politica.Hangar, 1);   // 80%
            List<Evento> l = Anda(s, 1f);
            Evento e = Agiu(l, Politica.Hangar).Single();
            Assert.AreEqual((0, 80), (e.B, e.C), "o aviao e o desgaste");
            Assert.IsTrue(l.Any(x => x.Tipo == Ev.HangarIniciado && x.A == 0 && x.B == s.T.HangarCusto), "a mesma intencao do jogador, com o preco dela");
            Assert.Greater(s.Avioes[0].Hangar, 0);
            Ocorrencia o = s.Ocorrencias.Single(x => x.Tipo == TipoOcorrencia.Politica);
            Assert.AreEqual(((int)Politica.Hangar, 80, (long)s.T.HangarCusto, Motivo.Politica, true, -1, Estacao.Manutencao), (o.Sub, o.Causa, o.Custo, o.Motivo, o.Auto, o.Voo, o.Estacao));
        }

        [Test]
        public void Hangar_NaoDeixaOCaixaAbaixoDaConta_NemComOOutroNoHangar()
        {
            Sim s = NoPatio();
            s.Avioes[0].Desgaste = 95;
            s.LigaPolitica(Politica.Hangar, true);
            s.Avioes[1].Hangar = 999;   // o jato revisando
            Assert.IsEmpty(Agiu(Anda(s, 1f), Politica.Hangar), "um aviao por vez");
            s.Avioes[1].Hangar = 0;
            Assert.Greater(s.Folha(), 0, "a tripulacao tem salario");
            s.Cash = s.T.HangarCusto + s.Folha() - 1;
            Assert.IsEmpty(Agiu(Anda(s, 1f), Politica.Hangar), "o hangar deixaria a conta de amanha sem caixa");
            s.Cash = s.T.HangarCusto + s.Folha();
            Assert.AreEqual(1, Agiu(Anda(s, 1f), Politica.Hangar).Count());
            Assert.GreaterOrEqual(s.Cash, s.Folha(), "sobrou a conta do proximo fechamento");
        }

        /// <summary>Com a conta atrasada (Pausado), a politica para como as melhorias: so o jogador decide gastar.</summary>
        [Test]
        public void Hangar_ComAContaAtrasada_NaoAge()
        {
            var s = new Sim(Calmo(), 1) { Cash = 100000 };
            for (int k = 0; k < Sim.MaxEquipe; k++) s.Hire((Perfil)k);   // os 3 da v0.1 (o resto pede marco)
            while (!s.Pausado) { s.Cash = 0; s.Tick(Sim.Step); }   // o dia fecha sem caixa
            while (s.Staff.Count > 0) s.Demite(0);   // a folha cai: o caixa cobre o hangar e a conta nova, mas nao a atrasada
            s.Cash = s.T.HangarCusto + s.Folha() + 1;
            Assert.IsTrue(s.Pausado && s.Atrasada > s.Cash, "a conta atrasada segue sem pagar");
            s.LigaPolitica(Politica.Hangar, true);
            foreach (Aviao a in s.Avioes) a.Desgaste = 95;
            var l = new List<Evento>();
            for (int i = 0; i < 400 * Sim.Hz && l.Count == 0; i++)
            {
                s.Tick(Sim.Step);
                l.AddRange(s.Events.Where(e => e.Tipo == Ev.PoliticaAgiu));
                if (s.PodeHangar(0) || s.PodeHangar(1)) Assert.IsTrue(s.Pausado);
            }
            Assert.IsEmpty(l, "pausado: a politica nao gasta");
            Assert.IsTrue(s.PodeHangar(0) || s.PodeHangar(1) || s.Avioes.Any(a => a.NoPatio), "houve aviao no patio para mandar");
        }

        [Test]
        public void Estoque_AbaixoDe75PorCento_PedeAntesDoPonto_NaContaDoDia()
        {
            var s = new Sim(Calmo(), 1);
            s.LigaPolitica(Politica.Estoque, true);
            s.Estoque[0] = 6;   // 6/8 = 75%: ainda nao
            Assert.IsEmpty(Agiu(Anda(s, 1f), Politica.Estoque));
            s.Estoque[0] = 5;
            List<Evento> l = Anda(s, 1f);
            Assert.AreEqual((0, 5), (Agiu(l, Politica.Estoque).Single().B, Agiu(l, Politica.Estoque).Single().C));
            Assert.AreEqual(3, s.Pedido[0], "enche o tanque, como a reposicao automatica");
            Assert.AreEqual(0, s.Pedido[1], "as refeicoes estao cheias");
            Assert.AreEqual(s.T.SupCusto[0], s.PolGasto);
            Assert.IsEmpty(Agiu(Anda(s, 1f), Politica.Estoque), "com a entrega a caminho, nao pede de novo");
            Assert.AreEqual(s.T.SupCusto[0], s.PolGasto);
            Assert.AreEqual(s.T.SupCusto[0], s.Hoje.Despesas[(int)Despesa.Suprimentos], "na conta do dia, nao no caixa");
            Ocorrencia o = s.Ocorrencias.Single(x => x.Tipo == TipoOcorrencia.Politica);
            Assert.AreEqual(((int)Politica.Estoque, 5, (long)s.T.SupCusto[0], Estacao.Despacho), (o.Sub, o.Causa, o.Custo, o.Estacao));
        }

        [Test]
        public void Estoque_NuncaPassaDoTeto_EOTetoVoltaNoDiaSeguinte()
        {
            var t = Calmo();
            t.SupPrazo = new[] { 2f, 2f };
            var s = new Sim(t, 1);
            s.LigaPolitica(Politica.Estoque, true);
            s.RegulaPolitica(Politica.Estoque, 0);   // 12 por dia: combustivel (6) e refeicoes (5), e nada mais
            int teto = s.PoliticaX(Politica.Estoque), dia = s.DiaJogo, pedidos = 0;
            for (int i = 0; i < 120 * Sim.Hz; i++)
            {
                for (int k = 0; k < Sim.Suprimentos; k++) if (s.Pedido[k] == 0) s.Estoque[k] = 4;   // acima do ponto (3), abaixo de 75%
                s.Tick(Sim.Step);
                pedidos += s.Events.Count(e => e.Tipo == Ev.PoliticaAgiu);
                Assert.LessOrEqual(s.PolGasto, teto);
            }
            Assert.AreEqual((2, 11L), (pedidos, s.PolGasto), "o 3o pedido passaria do teto");
            Assert.AreEqual(dia, s.DiaJogo);
            while (s.DiaJogo == dia) s.Tick(Sim.Step);
            for (int k = 0; k < Sim.Suprimentos; k++) if (s.Pedido[k] == 0) s.Estoque[k] = 4;
            List<Evento> l = Anda(s, 1f);
            Assert.AreEqual(2, Agiu(l, Politica.Estoque).Count(), "dia novo, teto novo");
            Assert.AreEqual(11L, s.PolGasto);
        }

        /// <summary>Ana no check-in e Caio no portao; a seguranca sem ninguem enche a fila. O reforco leva quem esta parado na estacao mais calma (o
        /// portao vazio), pelo vestiario, e o movimento nao conta para a missao de mover (o mesmo movimento feito pelo jogador conta).</summary>
        static Sim SegurancaVazia(int seed = 1)
        {
            var s = new Sim(Calmo(), seed) { Cash = 100000 };
            s.Hire(); s.Hire(); s.Hire();   // check-in, seguranca, portao
            s.Demite(1);
            Assert.AreEqual((Estacao.CheckIn, Estacao.Portao), (s.Staff[0].Em, s.Staff[1].Em));
            s.RegulaPolitica(Politica.Reforco, 0);   // 6 pessoas
            return s;
        }

        static int Movidos(Sim s) => s.Diarias.Concat(s.Semanais).Where(m => m != null && m.O.Meta == Meta.Movidos).Sum(m => m.Conta)
            + s.Diarias.Concat(s.Semanais).Where(m => m != null && m.O.E?.Meta == Meta.Movidos).Sum(m => m.ContaE);

        /// <summary>A 1a semente em que o sorteio do dia ou da semana pos uma missao de mover.</summary>
        static int SementeComMover()
        {
            for (int k = 1; k <= 200; k++)
            {
                var s = new Sim(Calmo(), k);
                s.Tick(Sim.Step);
                if (s.Diarias.Concat(s.Semanais).Any(m => m != null && (m.O.Meta == Meta.Movidos || m.O.E?.Meta == Meta.Movidos))) return k;
            }
            Assert.Fail("nenhuma semente sorteou a missao de mover");
            return -1;
        }

        [Test]
        public void Reforco_FilaLonga_LevaQuemEstaNaEstacaoMaisCalma_PeloVestiario_SemContarMissao()
        {
            int seed = SementeComMover();
            Sim s = SegurancaVazia(seed), eu = SegurancaVazia(seed);
            // a missao so conta o movimento para uma estacao apertada: espera a seguranca sair do normal
            for (int i = 0; i < 300 * Sim.Hz && s.St(Estacao.Seguranca).Estado == Estado.Normal; i++) { s.Tick(Sim.Step); eu.Tick(Sim.Step); }
            Assert.AreNotEqual(Estado.Normal, s.St(Estacao.Seguranca).Estado, "a seguranca apertou");
            s.LigaPolitica(Politica.Reforco, true);
            Evento e = default;
            for (int i = 0; i < 300 * Sim.Hz && e.Tipo != Ev.PoliticaAgiu; i++)
            {
                s.Tick(Sim.Step);
                e = s.Events.FirstOrDefault(x => x.Tipo == Ev.PoliticaAgiu);
                if (e.Tipo != Ev.PoliticaAgiu) eu.Tick(Sim.Step);
            }
            Assert.AreEqual((Ev.PoliticaAgiu, (int)Politica.Reforco, 1, (int)Estacao.Seguranca), (e.Tipo, e.A, e.B, e.C), "Caio vai do portao para a seguranca");
            Funcionario caio = s.Staff[1];
            Assert.AreEqual((Estacao.Seguranca, Estacao.Portao), (caio.Em, caio.De));
            Assert.AreEqual(s.TransferePassos - 1, caio.Andando, "a transferencia inteira, pelo vestiario (o passo ja andou 1)");
            Ocorrencia o = s.Ocorrencias.Single(x => x.Tipo == TipoOcorrencia.Politica);
            Assert.AreEqual(((int)Politica.Reforco, Estacao.Seguranca, 0L), (o.Sub, o.Estacao, o.Custo));
            Assert.GreaterOrEqual(o.Causa, 6, "a fila que pediu o reforco");
            // o jogador faz o mesmo movimento no mesmo passo: o dele conta, o da politica nao
            int antes = Movidos(eu);
            Assert.IsTrue(eu.Move(1, Estacao.Seguranca));
            eu.Tick(Sim.Step);
            Assert.AreEqual(antes + 1, Movidos(eu), "o movimento do jogador conta");
            Assert.AreEqual(antes, Movidos(s), "o da politica, nao");
            var fases = new HashSet<Transferencia>();
            while (caio.Andando > 0) { fases.Add(s.Fase(caio)); s.Tick(Sim.Step); }
            Assert.IsTrue(fases.Contains(Transferencia.Trocando), "troca o uniforme no vestiario");
        }

        [Test]
        public void Reforco_UmACadaReforcoCada_ENuncaDeEstacaoForaDoNormal()
        {
            Sim s = SegurancaVazia();
            s.LigaPolitica(Politica.Reforco, true);
            var vezes = new List<long>();
            for (int i = 0; i < 300 * Sim.Hz; i++)
            {
                s.AtualizaEstados();
                var antes = s.Staff.ToDictionary(f => f.Id, f => (f.Em, s.St(f.Em).Estado, f.Andando));
                int[] alocados = s.Estacoes.Select(x => s.Alocados(x.Tipo)).ToArray();
                s.Tick(Sim.Step);
                foreach (Evento e in s.Events.Where(x => x.Tipo == Ev.PoliticaAgiu))
                {
                    vezes.Add(s.Steps);
                    Assert.AreEqual((Estado.Normal, 0), (antes[e.B].Estado, antes[e.B].Andando), "so sai quem esta parado numa estacao normal");
                    Assert.Less(alocados[e.C], s.St((Estacao)e.C).Postos, "so vai para onde ha posto livre");
                }
            }
            Assert.That(vezes.Count, Is.GreaterThan(1));
            Assert.IsTrue(s.Ocorrencias.Where(o => o.Tipo == TipoOcorrencia.Politica).All(o => o.Causa >= 6), "so com a fila no X");
            for (int k = 1; k < vezes.Count; k++) Assert.GreaterOrEqual(vezes[k] - vezes[k - 1], (long)Math.Round(s.T.ReforcoCada * Sim.Hz), "um reforco a cada ReforcoCada s");
        }

        /// <summary>Os 3 nos postos de sempre: o portao (1 posto) fica com a fila mais longa antes do embarque abrir, mas cheio. O reforco nao
        /// leva ninguem para la: la ele ficaria parado.</summary>
        [Test]
        public void Reforco_NaoLevaParaEstacaoSemPostoLivre()
        {
            var s = new Sim(Calmo(), 1) { Cash = 100000 };
            s.Hire(); s.Hire(); s.Hire();
            s.RegulaPolitica(Politica.Reforco, 0);
            s.LigaPolitica(Politica.Reforco, true);
            Station portao = s.St(Estacao.Portao);
            bool cheioComFila = false;
            for (int i = 0; i < 300 * Sim.Hz; i++)
            {
                bool cheio = s.Alocados(Estacao.Portao) >= portao.Postos;
                cheioComFila |= cheio && portao.Fila.Count >= 6 && s.Estacoes.All(x => x.Fila.Count <= portao.Fila.Count) && s.Steps % Sim.Hz == 0;
                s.Tick(Sim.Step);
                if (cheio) Assert.IsFalse(s.Events.Any(e => e.Tipo == Ev.PoliticaAgiu && e.C == (int)Estacao.Portao), "o portao esta cheio");
            }
            Assert.IsTrue(cheioComFila, "o portao cheio teve a fila mais longa");
        }

        [Test]
        public void Reforco_NaoTiraOMecanicoComAviaoPendente()
        {
            var s = new Sim(Calmo(), 1) { Cash = 100000 };
            s.TotalEmbarcados = s.T.Marcos[1];   // marco 2: a oficina abre
            s.Hire(); s.Hire(); s.Hire();
            s.Demite(1);
            Assert.IsTrue(s.Move(1, Estacao.Manutencao), "Caio na oficina");
            while (s.Staff[1].Andando > 0) s.Tick(Sim.Step);
            s.RegulaPolitica(Politica.Reforco, 0);
            s.LigaPolitica(Politica.Reforco, true);
            s.Avioes[1].Desgaste = 100;   // o jato voando, pendente: a oficina espera por ele
            List<Evento> l = Anda(s, 200f);
            Assert.IsFalse(Agiu(l, Politica.Reforco).Any(e => e.B == 1), "o mecanico fica");
        }

        [Test]
        public void Registro_AcaoDaPoliticaSaiAntesDasOcorrencias()
        {
            var t = Calmo();
            t.SupPrazo = new[] { 1f, 1f };
            t.PoliticaX = new[] { new[] { 70, 80, 90 }, new[] { 100000, 100000, 100000 }, new[] { 6, 10, 15 } };
            var s = new Sim(t, 1);
            s.LigaPolitica(Politica.Estoque, true);
            for (int i = 0; i < 200 * Sim.Hz; i++)
            {
                for (int k = 0; k < Sim.Suprimentos; k++) if (s.Pedido[k] == 0) s.Estoque[k] = 4;
                s.Tick(Sim.Step);
            }
            var doDia = s.Ocorrencias.Where(o => o.Dia == s.DiaJogo).ToList();
            Assert.AreEqual(Sim.OcorrenciasPorDia, doDia.Count, "o dia encheu");
            Assert.IsTrue(s.Ocorrencias.Any(o => o.Tipo != TipoOcorrencia.Politica) || !doDia.Any(o => o.Tipo != TipoOcorrencia.Politica),
                "as ocorrencias de jogo ficam; quem sai e' a acao automatica mais antiga");
            int jogo = s.Ocorrencias.Count(o => o.Tipo != TipoOcorrencia.Politica);
            var sem = new Sim(Calmo(), 1);
            for (int i = 0; i < 200 * Sim.Hz; i++) sem.Tick(Sim.Step);
            Assert.AreEqual(sem.Ocorrencias.Count, jogo, "as mesmas ocorrencias de jogo de quem nao ligou nada");
        }

        [Test]
        public void Save_IdaEVolta_ELixoLimitado()
        {
            var s = new Sim(Calmo(), 3);
            s.LigaPolitica(Politica.Hangar, true); s.LigaPolitica(Politica.Reforco, true);
            s.RegulaPolitica(Politica.Estoque, 2); s.RegulaPolitica(Politica.Reforco, 0);
            Anda(s, 5f);
            string a = s.Save("t", 1);
            StringAssert.Contains("\npol=5,1,2,0,0,", a);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", a);
            Sim r = Sim.Load(a, Calmo(), 3);
            Assert.AreEqual(new[] { true, false, true }, r.PolLigada);
            Assert.AreEqual(new[] { 1, 2, 0 }, r.PolNivel);
            Assert.AreEqual(a, r.Save("t", 1), "ida e volta estavel");
            string lixo = a.Replace("\npol=5,1,2,0,0,", "\npol=255,-4,99,7,-1,");
            Sim z = Sim.Load(SaveEnvelope.Wrap(lixo.Substring(lixo.IndexOf("\nt=", StringComparison.Ordinal) + 1), "t", 1), Calmo(), 3);
            Assert.AreEqual(new[] { true, true, true }, z.PolLigada);
            Assert.AreEqual(new[] { 0, 2, 2 }, z.PolNivel, "nivel dentro da lista");
            Assert.AreEqual(0, z.PolGasto, "gasto negativo vira 0");
        }

        /// <summary>Fechar e reabrir no meio de cada onda, com as politicas ligadas, da o mesmo jogo (a retomada do -autoplay).</summary>
        [Test]
        public void Retomada_ComAsPoliticas_EOMesmoJogo()
        {
            foreach (BotProfile p in new[] { BotProfile.BomPoliticas, BotProfile.ParadoPoliticas })
                for (int seed = 1; seed <= 2; seed++)
                {
                    Bot.Resultado a = Bot.Medir(p, seed, 4), b = Bot.Medir(p, seed, 4, null, true);
                    Assert.AreEqual((a.Receita, a.Embarque, a.Moves, a.Politicas[2], a.Suprimentos), (b.Receita, b.Embarque, b.Moves, b.Politicas[2], b.Suprimentos), p.Name + " semente " + seed);
                    Assert.Greater(a.Politicas[2], 0, "o reforco agiu");
                }
        }

        /// <summary>Invariante: com as 3 ligadas, a cada passo o caixa nunca fica negativo e o gasto da reposicao fica no teto; o hangar da politica
        /// sempre deixa a conta do proximo fechamento no caixa e nunca poe o 2o aviao no hangar.</summary>
        [Test]
        public void Invariante_ComAsPoliticas_CaixaETetoNoLimite()
        {
            var t = new Tuning { PoliticaPadrao = new[] { 0, 0, 0 } };   // o X mais ativo
            foreach (BotProfile p in new[] { BotProfile.ComPoliticas(BotProfile.Automatico, "AutoPoliticas"), BotProfile.LentoPoliticas })
                for (int seed = 1; seed <= 3; seed++)
                {
                    var s = new Sim(t, seed);
                    var b = new Bot(p);
                    int hangares = 0;
                    for (long i = 0; i < 16L * s.OndaPassos; i++)
                    {
                        b.Step(s);
                        Assert.GreaterOrEqual(s.Cash, 0);
                        Assert.LessOrEqual(s.PolGasto, s.PoliticaX(Politica.Estoque));
                        foreach (Evento e in s.Events.Where(x => x.Tipo == Ev.PoliticaAgiu && x.A == (int)Politica.Hangar))
                        {
                            hangares++;
                            Assert.GreaterOrEqual(s.Cash, s.Folha(), p.Name + " semente " + seed + ": o hangar deixou a conta");
                            Assert.AreEqual(0, s.Avioes[1 - e.B].Hangar, "um aviao por vez");
                        }
                    }
                    if (p.Name == "AutoPoliticas") Assert.Greater(hangares, 0, "sem checagem, o desgaste chega ao X");
                }
        }

        /// <summary>Save real do schema 17 (commit c814e5f, a main com a satisfacao da frente G, #73): semente 7, bot Bom, 2 790 s.</summary>
        const string V17 = "sha256=4bfaa414668c7457e139b4fbe9096237a3bda1d1765daa56a029210c7c8faad0\nschema=17\ncontent=0.17.0\nat=1800000000\nt=83700\ncash=5792\nseed=7\nonda=15" +
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
            "=0\nzels=1\nzel.0=5,1,0,0,330,0\nsup.0=4,0,0,0\nsup.1=4,0,0,0\nhp=11,15026,1,4095\nsat=1048,1,25,0,0,0,0\nsatm=75.77641788243535,-2.926038169254486,-0" +
            ".4102012121479099,0.01654660112394964,3.245386015359444,0.8507246473543898,2410.925569163271,888.5393064826692,0,1071.7548450992601,6.410380556230992," +
            "34.315099512899685,0,0,348.8382553040206,7.592885893077482\nsatp=15501,870,0,0,0,0,0;15502,825,0,0,0,0,0;15503,810,0,0,0,0,0;15504,765,0,0,0,0,0;15505" +
            ",750,0,0,0,0,0;15506,720,0,0,0,0,0;15507,705,0,0,0,0,0;15508,675,0,0,0,0,0;15509,645,0,0,0,0,0;15510,630,0,0,0,0,0;15511,600,0,0,0,0,0;15512,555,0,0,0" +
            ",0,0;15513,555,0,0,0,0,0;15514,525,0,0,0,0,0;15515,480,0,0,0,0,0;15516,465,0,0,0,0,0;15517,450,0,0,0,0,0;15518,405,0,0,0,0,0;15519,390,0,0,0,0,0;15520" +
            ",360,0,0,0,0,0;15521,345,0,0,0,0,0;15522,315,0,0,0,0,0;15523,285,0,0,0,0,0;15524,255,0,0,0,0,0;15525,210,0,0,0,0,0;15526,195,0,0,0,0,0;15527,165,0,0,0" +
            ",0,0;15528,135,0,0,0,0,0;15529,120,0,0,0,0,0;15530,90,0,0,0,0,0;15531,75,0,0,0,0,0;15532,45,0,0,0,0,0;15533,30,0,0,0,0,0;15044,1155,0,0,0,0,0;15045,11" +
            "70,0,0,0,0,0;15500,870,0,0,0,0,0;15042,1140,168,0,0,0,0;15043,1155,172,0,0,0,0;15011,1005,204,0,300,0,2;15016,1050,64,0,300,0,2;15015,1005,216,0,300,0" +
            ",2;15017,1005,152,0,300,0,2;15014,1035,228,0,300,0,2;15020,1095,80,0,285,0,2;15019,1095,240,0,240,0,2;15018,1110,156,0,240,0,2;15022,1140,88,0,210,0,2" +
            ";15023,1200,92,0,120,0,2;15021,1140,252,0,90,0,2;15024,1245,96,0,75,0,2;15025,1260,264,0,0,0,0;15026,1260,164,0,0,0,0;15027,1215,276,0,0,0,0;15028,126" +
            "0,112,0,0,0,0;15029,1245,116,0,0,0,0;15031,1230,124,0,0,0,0;15030,1185,120,0,0,0,0;15033,1170,168,0,0,0,0;15032,1155,128,0,0,0,0;15035,1170,140,0,0,0," +
            "0;15036,1155,144,0,0,0,0;15034,1140,172,0,0,0,0;15037,1140,148,0,0,0,0;15038,1140,152,0,0,0,0;15039,1140,156,0,0,0,0;15040,1110,160,0,0,0,0;15041,1110" +
            ",164,0,0,0,0\n";

        /// <summary>Os campos double mudam na 8a casa entre o Mono do Unity e o dotnet: saem da comparacao (satm = as medias da satisfacao).</summary>
        static string SemDouble(string save) =>
            string.Join("\n", save.Split('\n').Where(l => !l.StartsWith("sha256=") && !l.StartsWith("schema=") && !l.StartsWith("ema=") && !l.StartsWith("stt.") && !l.StartsWith("satm=")));

        /// <summary>O schema 17 (sem politicas) abre com as 3 desligadas no X padrao, grava sem a chave e o jogo e' o mesmo: migrar = jogar ate ali com o
        /// codigo novo, e o save sai igual ao do 17 fora o cabecalho.</summary>
        [Test]
        public void MigracaoDoSchema17_SaveReal_TudoDesligado_EOMesmoJogo()
        {
            const long U = 1800000000;
            Sim m = Sim.Load(V17, null, 7);
            Assert.IsFalse(m.SaveRefused, "schema 17 migra, nao e' recusado");
            Assert.AreEqual(new[] { false, false, false }, m.PolLigada);
            Assert.AreEqual(new[] { 1, 1, 1 }, m.PolNivel);
            string texto = m.Save("0.17.0", U);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", texto);
            StringAssert.DoesNotContain("\npol=", texto);
            Assert.AreEqual(SemDouble(V17), SemDouble(texto), "o formato do 17 nao mudou");
            var n = new Sim(null, 7);
            var b = new Bot(BotProfile.Bom);
            for (int i = 0; i < 2790 * Sim.Hz; i++) b.Step(n);
            Assert.AreEqual(SemDouble(n.Save("0.17.0", U)), SemDouble(texto), "migrar = jogar ate ali com o codigo novo");
        }
    }
}
