using System;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Regras da Fase C9 (CONTRATO_FASE_C9 §2 a §4): os 3 candidatos de cada vaga, contratar e demitir. A migracao do schema 10 fica no
    /// SaveTests; o portao, no BalanceTests.</summary>
    public class EquipeTests
    {
        static readonly Experiencia[] Niveis = { Experiencia.Junior, Experiencia.Pleno, Experiencia.Senior };
        static readonly Perfil[] Perfis = (Perfil[])Enum.GetValues(typeof(Perfil));

        /// <summary>Tudo aberto (estacoes e perfis) e Cash de sobra.</summary>
        static Sim Aberto(Tuning t = null, int seed = 1)
        {
            t = t ?? new Tuning();
            t.Abre = new int[8]; t.PerfilMarco = new int[6];
            return new Sim(t, seed) { Cash = 1000000 };
        }

        /// <summary>seg de jogo em passos fixos (o Tick tem teto de Sim.MaxFrame por chamada).</summary>
        static void Anda(Sim s, float seg)
        {
            for (int i = 0; i < (int)(seg * Sim.Hz); i++) s.Tick(Sim.Step);
        }

        static long[] Lista(Sim s) => Perfis.SelectMany(p => Niveis.SelectMany(n => new[] { s.CustoContratar(p, n), s.Salario(p, n), (long)(s.Rendimento(n) * 100) })).ToArray();

        // ------------------------------------------------------------------ os 3 candidatos

        [Test]
        public void Tabela_TresCandidatos_OPlenoEOPrecoDeAntes_NenhumDomina()
        {
            Tuning t = Tuning.Padrao;
            Assert.AreEqual((3, 3, 3, 3), (Niveis.Length, t.ExpRende.Length, t.ExpContratacao.Length, t.ExpSalario.Length));
            Assert.AreEqual((100, 100, 100), (t.ExpRende[1], t.ExpContratacao[1], t.ExpSalario[1]), "o Pleno e' o de antes");
            for (int n = 1; n < 3; n++)
                Assert.IsTrue(t.ExpRende[n] > t.ExpRende[n - 1] && t.ExpContratacao[n] > t.ExpContratacao[n - 1] && t.ExpSalario[n] > t.ExpSalario[n - 1],
                    "quem rende mais custa mais, na contratacao e por dia: nenhum candidato domina outro");
            Sim s = Aberto();
            foreach (Perfil p in Perfis)
                Assert.AreEqual((s.CustoContratar(p), (long)t.Salario[(int)p]), (s.CustoContratar(p, Experiencia.Pleno), s.Salario(p, Experiencia.Pleno)), p.ToString());
            Assert.AreEqual((10L, 40L), (s.CustoContratar(Perfil.Atendente, Experiencia.Junior), s.CustoContratar(Perfil.Atendente, Experiencia.Senior)));
            Assert.AreEqual((25L, 53L), (s.Salario(Perfil.Atendente, Experiencia.Junior), s.Salario(Perfil.Atendente, Experiencia.Senior)), "meio arredonda para cima, sem float");
        }

        /// <summary>TA-5 e §7.1: a lista e' a mesma em todo jogo. Nao depende da semente, do relogio, das Gems nem de compra: nada pago troca a lista.</summary>
        [Test]
        public void Candidatos_FixosEmTodoJogo_NadaPagoTrocaALista()
        {
            long[] base1 = Lista(Aberto(null, 1));
            CollectionAssert.AreEqual(base1, Lista(Aberto(null, 987654)), "a semente nao muda a lista");
            Sim s = Aberto(null, 3);
            Anda(s, 600f);
            CollectionAssert.AreEqual(base1, Lista(s), "o relogio nao muda a lista");
            Assert.AreEqual(Credito.Creditado, s.Creditar("GPA.9", "gems_pack_large"));
            for (int i = 0; i < s.T.ItensGems.Length; i++) s.Gastar(i);
            CollectionAssert.AreEqual(base1, Lista(s), "Gems e compra nao mudam a lista");
        }

        [Test]
        public void Hire_CadaNivel_CobraOPrecoDele_EEntraNaFolha()
        {
            foreach (Experiencia n in Niveis)
            {
                Sim s = Aberto();
                long c = s.CustoContratar(Perfil.Seguranca, n), cash = s.Cash, folha = s.Folha(Despesa.Equipe);
                Assert.IsTrue(s.Hire(Perfil.Seguranca, n));
                Assert.AreEqual((cash - c, n, folha + s.Salario(Perfil.Seguranca, n)), (s.Cash, s.Staff[0].Experiencia, s.Folha(Despesa.Equipe)), n.ToString());
                Evento e = s.Events.Single(x => x.Tipo == Ev.Contratado);
                Assert.AreEqual((0, (int)c, (int)Estacao.CheckIn), (e.A, e.B, e.C));
                Assert.IsTrue(Niveis.All(o => s.CustoContratar(Perfil.Seguranca, o) == -1 && !s.Hire(Perfil.Seguranca, o)), "a vaga fechou: um por perfil");
            }
        }

        [Test]
        public void Hire_Recusa_SemCaixa_NivelInvalido_AntesDoMarco()
        {
            var s = new Sim(null, 1) { Cash = 15 };
            Assert.IsFalse(s.Hire(Perfil.Atendente, Experiencia.Pleno), "o Pleno custa 20");
            Assert.AreEqual((15L, 0), (s.Cash, s.Staff.Count));
            Assert.AreEqual((-1L, -1L), (s.CustoContratar(Perfil.Atendente, (Experiencia)3), s.CustoContratar(Perfil.Atendente, (Experiencia)(-1))));
            Assert.IsFalse(s.Hire(Perfil.Atendente, (Experiencia)3));
            Assert.IsTrue(Niveis.All(n => s.CustoContratar(Perfil.Mecanico, n) == -1), "antes do marco a vaga nao abre");
            Assert.IsTrue(s.Hire(Perfil.Atendente, Experiencia.Junior), "o Junior cabe nos 15");
            Assert.AreEqual(5L, s.Cash);
        }

        /// <summary>O nivel muda o rendimento como o especialista: o tempo de atendimento, a venda e a forca da oficina. Desligado, todos rendem como o Pleno.</summary>
        [Test]
        public void Rende_PeloNivel_NoTempoNaVendaENaForca()
        {
            int[] tempo = new int[3];
            foreach (Experiencia n in Niveis)
            {
                Sim s = Aberto();
                Assert.IsTrue(s.Hire(Perfil.Atendente, n) && s.Hire(Perfil.Mecanico, n));
                Assert.IsTrue(s.Move(1, Estacao.Manutencao));
                Anda(s, 5f);
                Assert.AreEqual(s.Rendimento(n), s.Rende(Estacao.CheckIn), n.ToString());
                Assert.AreEqual(s.Rendimento(n), s.Forca(Estacao.Manutencao), n.ToString());
                tempo[(int)n] = s.TempoPassos(Estacao.CheckIn);
            }
            Assert.IsTrue(tempo[0] > tempo[1] && tempo[1] > tempo[2], "o Junior atende mais devagar e o Senior mais rapido");
            Assert.AreEqual(new Sim(null, 1).TempoPassos(Estacao.CheckIn), tempo[1], "o Pleno atende no tempo de antes");

            Sim d = Aberto(new Tuning { RegraExperiencia = false });
            Assert.IsTrue(d.Hire(Perfil.Atendente, Experiencia.Senior));
            Assert.AreEqual((1f, (long)d.T.Contratacao[0], (long)d.T.Salario[0]), (d.Rende(Estacao.CheckIn), 1000000 - d.Cash, d.Folha(Despesa.Equipe)),
                "regra desligada: so o nome muda");
        }

        // ------------------------------------------------------------------ demitir

        /// <summary>Direcao §9.2: o desligamento atualiza a escala, o custo e a cobertura do posto. Sem custo, sem devolver a contratacao, e a vaga
        /// reabre com os mesmos 3 candidatos.</summary>
        [Test]
        public void Demite_AtualizaEscalaFolhaECobertura_EReabreAVaga()
        {
            Sim s = Aberto();
            Assert.IsTrue(s.Hire(Perfil.Atendente, Experiencia.Junior) && s.Hire(Perfil.Seguranca, Experiencia.Senior) && s.Hire(Perfil.Vendedor, Experiencia.Pleno));
            Assert.AreEqual(Estacao.Seguranca, s.Staff[1].Em);
            long cash = s.Cash, folha = s.Folha(Despesa.Equipe);
            s.Tick(Sim.Step);
            Assert.IsTrue(s.Demite(1));
            Evento e = s.Events.Single(x => x.Tipo == Ev.Demitido);
            Assert.AreEqual((1, (int)Perfil.Seguranca, (int)Experiencia.Senior), (e.A, e.B, e.C));
            Assert.AreEqual(cash, s.Cash, "sem custo e sem devolver");
            Assert.AreEqual(folha - s.Salario(Perfil.Seguranca, Experiencia.Senior), s.Folha(Despesa.Equipe), "a folha cai");
            Assert.AreEqual((0, 0), (s.Presentes(Estacao.Seguranca), s.Alocados(Estacao.Seguranca)), "o posto ficou descoberto");
            Assert.AreEqual((2, 0, 1, Perfil.Vendedor), (s.Staff.Count, s.Staff[0].Id, s.Staff[1].Id, s.Staff[1].Perfil), "quem vinha depois sobe uma posicao");
            Assert.IsFalse(s.Contratado(Perfil.Seguranca));
            Assert.IsTrue(Niveis.All(n => s.CustoContratar(Perfil.Seguranca, n) == Aberto().CustoContratar(Perfil.Seguranca, n)), "a vaga reabre com os mesmos 3");
            Assert.IsTrue(s.Move(1, Estacao.Loja) && s.Staff[1].Perfil == Perfil.Vendedor, "o indice novo move a pessoa certa");
            Assert.IsFalse(s.Demite(2) || s.Demite(-1), "indice fora da equipe");
            Assert.IsTrue(s.Hire(Perfil.Seguranca, Experiencia.Junior), "recontrata pela vaga reaberta");
        }

        /// <summary>Contratar e demitir em laco nunca gera Cash: cada volta custa a contratacao inteira.</summary>
        [Test]
        public void Invariante_C9_ContratarEDemitirNuncaDaCash()
        {
            Sim s = Aberto();
            long cash = s.Cash, gasto = 0;
            for (int i = 0; i < 50; i++)
            {
                Experiencia n = Niveis[i % 3];
                gasto += s.CustoContratar(Perfil.Bagageiro, n);
                Assert.IsTrue(s.Hire(Perfil.Bagageiro, n) && s.Demite(s.Staff.Count - 1));
            }
            Assert.AreEqual(cash - gasto, s.Cash);
        }

        /// <summary>Sem derrota inevitavel (GDD §0.5): demitir todo mundo no meio do jogo volta ao comeco, no toque; todo voo sai e o terminal continua
        /// embarcando, e o bot volta a contratar.</summary>
        [Test]
        public void Invariante_C9_DemitirTodos_NaoTrancaOJogo()
        {
            for (int seed = 1; seed <= 5; seed++)
            {
                var s = new Sim(null, seed);
                var b = new Bot(BotProfile.Bom);
                int voos = 0; long embarcou = 0;
                for (int i = 0; i < 8 * s.OndaPassos; i++)
                {
                    if (i % (2 * s.OndaPassos) == s.OndaPassos / 2) while (s.Staff.Count > 0) Assert.IsTrue(s.Demite(0));
                    b.Step(s);
                    Assert.GreaterOrEqual(s.Cash, 0);
                    voos += s.Events.Count(e => e.Tipo == Ev.VooPartiu);
                    embarcou += s.Events.Count(e => e.Tipo == Ev.Embarcou);
                }
                Assert.AreEqual(16, voos, "semente " + seed + ": todo voo sai");
                Assert.Greater(embarcou, 0, "semente " + seed);
                Assert.Greater(s.Staff.Count, 0, "semente " + seed + ": o bot volta a contratar");
            }
        }

        /// <summary>Demitir vale com a conta atrasada e e' a saida dela: a folha cai, o fechamento seguinte perdoa a de ontem e cobra so a dele, que
        /// agora cabe, e a pausa acaba.</summary>
        [Test]
        public void Invariante_C9_ComContaAtrasada_DemitirTiraDaPausa()
        {
            var s = Aberto(new Tuning { Salario = Enumerable.Repeat(100000, 6).ToArray() });
            Assert.IsTrue(s.Hire(Perfil.Atendente, Experiencia.Junior) && s.Hire(Perfil.Seguranca, Experiencia.Junior));
            s.Cash = 5000;
            DespesasTests.ViraODia(s);
            Assert.IsTrue(s.Pausado, "a folha de 2 juniores com salario de 100 000 nao cabe");
            Assert.IsFalse(s.Hire(Perfil.Vendedor, Experiencia.Junior), "contratar pausa");
            Assert.IsTrue(s.Demite(1) && s.Demite(0), "demitir nao pausa");
            Assert.AreEqual(0L, s.Folha(Despesa.Equipe));
            s.Cash = s.Folha() + 10;   // cobre a conta de um dia sem a equipe, nunca a atrasada
            DespesasTests.ViraODia(s);
            Assert.AreEqual((false, 0L), (s.Pausado, s.Atrasada), "o dia seguinte, sem a equipe, cabe");
            Assert.GreaterOrEqual(s.Cash, 10L);
            s.Cash += s.CustoContratar(Perfil.Vendedor, Experiencia.Junior);
            Assert.IsTrue(s.Hire(Perfil.Vendedor, Experiencia.Junior), "a pausa acabou");
        }

        // ------------------------------------------------------------------ save

        /// <summary>Fechar e reabrir com 3 niveis e uma demissao no meio da onda: o nivel volta, a folha e a cobertura batem e o jogo segue igual.</summary>
        [Test]
        public void Save_NivelEDemissao_IdaEVoltaEContinuaIgual()
        {
            const long U = 1800000000;
            Sim a = Aberto(null, 4), b = Aberto(null, 4);
            foreach (Sim s in new[] { a, b })
            {
                Anda(s, 30f);
                Assert.IsTrue(s.Hire(Perfil.Atendente, Experiencia.Senior) && s.Hire(Perfil.Seguranca, Experiencia.Junior) && s.Hire(Perfil.Mecanico, Experiencia.Pleno) && s.Hire(Perfil.Vendedor, Experiencia.Junior));
                Assert.IsTrue(s.Demite(2));
                Anda(s, 41f);
            }
            string texto = a.Save("0.12.0", U);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", texto);
            Sim c = Sim.Load(texto, a.T, 4);
            Assert.AreEqual(a.Staff.Select(f => (f.Id, f.Perfil, f.Experiencia, f.Em)), c.Staff.Select(f => (f.Id, f.Perfil, f.Experiencia, f.Em)));
            Assert.AreEqual((a.Folha(), a.Rende(Estacao.CheckIn), a.Rende(Estacao.Seguranca)), (c.Folha(), c.Rende(Estacao.CheckIn), c.Rende(Estacao.Seguranca)));
            Assert.AreEqual(texto, c.Save("0.12.0", U), "ida e volta estavel no schema 12");
            Anda(b, 300f); Anda(c, 300f);
            Assert.AreEqual((b.Cash, b.TotalEmbarcados, b.Steps, b.Folha()), (c.Cash, c.TotalEmbarcados, c.Steps, c.Folha()), "reabrir no meio da onda da o mesmo jogo");
        }

        [Test]
        public void LixoNoNivel_ViraOPleno()
        {
            foreach (string nv in new[] { "9", "-1", "3" })
            {
                Sim s = Sim.Load(SaveEnvelope.Wrap("t=90\nfunc.0=0,0,0,0," + nv + "\n", "x", 1));
                Assert.IsFalse(s.SaveRefused);
                Assert.AreEqual((1, Experiencia.Pleno), (s.Staff.Count, s.Staff[0].Experiencia), nv);
            }
        }
    }
}
