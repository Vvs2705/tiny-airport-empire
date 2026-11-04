using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Frente E: transferencia pelo vestiario (GDD §4.1; Direcao §9.3 e §9.6). Regras e invariantes em Sim.Vestiario.cs.</summary>
    public class VestiarioTests
    {
        const long T0 = 1_800_000_000;

        static Sim ComAna(Tuning t = null)
        {
            var s = new Sim(t ?? Tuning.Padrao, 1) { Cash = 10000 };
            Assert.IsTrue(s.Hire());   // Ana, no check-in
            s.Cash = 0;
            return s;
        }

        static int P(float seg) => System.Math.Max(1, (int)System.Math.Round(seg * Sim.Hz));

        [Test]
        public void Transferencia_EncerraVaiTrocaEChega_NaOrdem_UniformeSoNoVestiario()
        {
            Sim s = ComAna();
            Funcionario f = s.Staff[0];
            Assert.AreEqual(0, s.St(Estacao.CheckIn).Atendendo.Count, "sem atendimento: nao ha o que encerrar");
            Assert.IsTrue(s.Move(0, Estacao.Seguranca));
            int troca = P(s.T.Troca), volta = (P(s.T.Caminhada) - troca) / 2, ida = P(s.T.Caminhada) - troca - volta;
            Assert.AreEqual(P(s.T.Caminhada), f.Andando, "o tempo da caminhada da v0.1, agora com a troca dentro");
            Assert.AreEqual(f.Andando, s.TransferePassos);
            var fases = new List<Transferencia>();
            var veste = new List<Estacao>();
            var chegou = 0;
            while (f.Andando > 0)
            {
                fases.Add(s.Fase(f)); veste.Add(s.Veste(f));
                Assert.AreEqual((0, 0), (s.Presentes(Estacao.CheckIn), s.Presentes(Estacao.Seguranca)), "em transferencia nao atende em nenhuma");
                s.Tick(Sim.Step);
                chegou += s.Events.Count(e => e.Tipo == Ev.Chegou && e.A == 0);
            }
            Assert.AreEqual(Enumerable.Repeat(Transferencia.IndoVestiario, ida).Concat(Enumerable.Repeat(Transferencia.Trocando, troca))
                .Concat(Enumerable.Repeat(Transferencia.IndoPosto, volta)).ToList(), fases);
            Assert.AreEqual(Enumerable.Repeat(Estacao.CheckIn, ida + troca).Concat(Enumerable.Repeat(Estacao.Seguranca, volta)).ToList(), veste,
                "o uniforme muda no fim da troca, la dentro, nunca no caminho");
            Assert.AreEqual((Transferencia.Nenhuma, Estacao.Seguranca, 1, 1, 1f), (s.Fase(f), s.Veste(f), s.Presentes(Estacao.Seguranca), chegou, s.Preparo(f)));
        }

        [Test]
        public void Encerrando_TerminaOAtendimentoNoPostoAntesDeSair()
        {
            Sim s = ComAna();
            Station ci = s.St(Estacao.CheckIn);
            for (int i = 0; i < 120 * Sim.Hz && ci.Atendendo.Count == 0; i++) s.Tick(Sim.Step);
            Assert.AreEqual((1, 1), (ci.Atendendo.Count, s.Presentes(Estacao.CheckIn)), "a Ana esta atendendo");
            int resto = ci.Atendendo[0].Resto, quem = ci.Atendendo[0].Id;
            Funcionario f = s.Staff[0];
            Assert.IsTrue(s.Move(0, Estacao.Seguranca));
            Assert.AreEqual(resto + s.TransferePassos, f.Andando, "termina a tarefa, depois a transferencia");
            Assert.AreEqual((Transferencia.Encerrando, Estacao.CheckIn, 0f), (s.Fase(f), s.Veste(f), s.Preparo(f)));
            Assert.AreEqual(f.Andando, Sim.Load(s.Save("t", T0)).Staff[0].Andando, "o save guarda a transferencia inteira, com o encerramento");
            for (int i = 0; i < resto; i++)
            {
                Assert.AreEqual(Transferencia.Encerrando, s.Fase(f));
                s.Tick(Sim.Step);
                Assert.IsFalse(ci.Atendendo.Any(p => p.Id != quem), "encerrando nao comeca outro atendimento");
            }
            Assert.AreEqual((0, Transferencia.IndoVestiario), (ci.Atendendo.Count, s.Fase(f)), "o passageiro saiu do balcao e ela foi");
        }

        [Test]
        public void ComOutroNoPosto_SaiSemEsperar()
        {
            var s = new Sim(Tuning.Padrao, 1) { Cash = 100000 };
            Assert.IsTrue(s.Hire()); Assert.IsTrue(s.Hire());
            Assert.IsTrue(s.Move(1, Estacao.CheckIn));
            s.Staff[1].Andando = 0;
            Station ci = s.St(Estacao.CheckIn);
            for (int i = 0; i < 120 * Sim.Hz && ci.Atendendo.Count != 1; i++) s.Tick(Sim.Step);
            Assert.AreEqual((1, 2), (ci.Atendendo.Count, s.Presentes(Estacao.CheckIn)));
            Assert.IsTrue(s.Move(0, Estacao.Seguranca));
            Assert.AreEqual(s.TransferePassos, s.Staff[0].Andando, "o outro atende: nada a encerrar");
        }

        [Test]
        public void VoltarAoSetorDoUniforme_SoACaminhada_DepoisDaTroca_VestiarioDeNovo()
        {
            Sim s = ComAna();
            Funcionario f = s.Staff[0];
            Assert.IsTrue(s.Move(0, Estacao.Seguranca));
            RegrasTests.Run(s, 1f);
            Assert.IsTrue(s.Move(0, Estacao.CheckIn), "desistiu antes de trocar");
            Assert.AreEqual((P(s.T.Caminhada), Transferencia.IndoPosto, Estacao.CheckIn, Estacao.CheckIn), (f.Andando, s.Fase(f), s.Veste(f), f.De));

            Assert.IsTrue(s.Move(0, Estacao.Seguranca));
            while (s.Fase(f) != Transferencia.IndoPosto) s.Tick(Sim.Step);
            Assert.AreEqual(Estacao.Seguranca, s.Veste(f));
            Assert.IsTrue(s.Move(0, Estacao.Portao), "ja trocou: outro setor e' outra troca");
            Assert.AreEqual((s.TransferePassos, Transferencia.IndoVestiario, Estacao.Seguranca), (f.Andando, s.Fase(f), s.Veste(f)));
        }

        [Test]
        public void Tempos_DaTabela_RegraDesligadaEAV01()
        {
            Sim s = ComAna();
            Assert.IsTrue(s.Move(0, Estacao.Loja));
            Assert.AreEqual(P(s.T.Caminhada), s.Staff[0].Andando, "ida, troca e volta somam a caminhada da v0.1");

            Sim v01 = ComAna(new Tuning { RegraVestiario = false });
            Assert.IsTrue(v01.Move(0, Estacao.Seguranca));
            Assert.AreEqual((P(v01.T.Caminhada), Transferencia.IndoPosto), (v01.Staff[0].Andando, v01.Fase(v01.Staff[0])));
        }

        [Test]
        public void SaveNoMeioDaTroca_ReabreNaMesmaFase()
        {
            Sim s = ComAna();
            Assert.IsTrue(s.Move(0, Estacao.Seguranca));
            RegrasTests.Run(s, 1f);
            Funcionario f = s.Staff[0];
            Assert.AreEqual(Transferencia.Trocando, s.Fase(f));
            string a = s.Save("t", T0);
            Sim b = Sim.Load(a);
            Funcionario g = b.Staff[0];
            Assert.AreEqual((f.Andando, f.De, f.Em, s.Fase(f), s.Veste(f), s.Preparo(f)), (g.Andando, g.De, g.Em, b.Fase(g), b.Veste(g), b.Preparo(g)));
            Assert.AreEqual(a, b.Save("t", T0));
            RegrasTests.Run(s, 10f); RegrasTests.Run(b, 10f);
            Assert.AreEqual(s.Save("t", T0), b.Save("t", T0), "retomada igual");
        }

        /// <summary>Invariantes num jogo inteiro do bot que mais move (Lento): sem um Movido, a fase so avanca e quem chegou fica; o uniforme so
        /// muda no fim da troca; o preparo nunca volta.</summary>
        [Test]
        public void Invariantes_NoBotLento_TresOndas()
        {
            var s = new Sim(Tuning.Padrao, 3);
            var bot = new Bot(BotProfile.Lento);
            var fase = new Transferencia[Sim.MaxEquipe];
            var veste = new Estacao[Sim.MaxEquipe];
            var preparo = new float[Sim.MaxEquipe];
            int moves = 0, trocas = 0, vistos = 0;
            for (int i = 0; i < 3 * s.OndaPassos; i++)
            {
                bot.Step(s);
                for (int k = 0; k < s.Staff.Count; k++)
                {
                    Funcionario f = s.Staff[k];
                    Transferencia agora = s.Fase(f);
                    Estacao v = s.Veste(f);
                    if (s.Events.Any(e => e.Tipo == Ev.Movido && e.A == f.Id)) moves++;
                    else if (k < vistos)
                    {
                        Assert.IsTrue(fase[k] == Transferencia.Nenhuma ? agora == Transferencia.Nenhuma : agora >= fase[k] || agora == Transferencia.Nenhuma,
                            "a fase so avanca: " + fase[k] + " -> " + agora);
                        Assert.GreaterOrEqual(s.Preparo(f), preparo[k]);
                        if (v != veste[k]) { Assert.AreEqual(Transferencia.Trocando, fase[k], "o uniforme so muda no fim da troca"); trocas++; }
                    }
                    fase[k] = agora; veste[k] = v; preparo[k] = s.Preparo(f);
                }
                vistos = s.Staff.Count;
            }
            Assert.Greater(moves, 0, "o Lento moveu alguem");
            Assert.Greater(trocas, 0, "e alguem trocou de uniforme");
        }
    }
}
