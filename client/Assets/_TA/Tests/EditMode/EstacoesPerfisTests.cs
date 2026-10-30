using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Regras da Fase C2 (CONTRATO_FASE_C2 §1 e §2): desbloqueio pelo marco, os 6 perfis, o especialista e as 4 estacoes novas.</summary>
    public class EstacoesPerfisTests
    {
        static void Run(Sim s, float seg) => RegrasTests.Run(s, seg);
        static System.Collections.Generic.List<Evento> Coleta(Sim s, float seg) => RegrasTests.Coleta(s, seg);

        /// <summary>Tudo aberto desde o comeco e com regra, inclusive a especializacao (so visual no padrao: BALANCE.md D-C2a).</summary>
        internal static Tuning Aberto() => new Tuning { Abre = new int[8], PerfilMarco = new int[6], EspecialistaMarco = 0, RegraEspecialista = true };

        /// <summary>Os 3 da v0.1, mais os perfis pedidos ja na estacao deles, e 2 pessoas de patio (o patio nao atrasa).</summary>
        internal static Sim Pronta(Tuning t, params Perfil[] extra)
        {
            var s = new Sim(t, 1) { Cash = 100000 };
            while (s.Hire()) { }
            foreach (Perfil p in extra) { Assert.IsTrue(s.Hire(p), p.ToString()); NaCasa(s, s.Staff.Count - 1); }
            s.EquipePatio = 2;
            s.Cash = 0;
            return s;
        }

        /// <summary>Leva o funcionario para a estacao do perfil sem esperar a caminhada.</summary>
        internal static void NaCasa(Sim s, int f)
        {
            Assert.IsTrue(s.Move(f, Funcionario.Casa(s.Staff[f].Perfil)));
            s.Staff[f].Andando = 0;
        }

        [Test]
        public void Desbloqueio_PeloMarco_AntesDoMarco1EAV01()
        {
            var s = new Sim(new Tuning { RegraEspecialista = true }, 1) { Cash = 100000 };
            for (int e = 4; e < 8; e++) Assert.IsFalse(s.Desbloqueada((Estacao)e), ((Estacao)e).ToString());
            Assert.AreEqual((20L, -1L, -1L), (s.CustoContratar(), s.CustoContratar(Perfil.Bagageiro), s.Custo(Estacao.Bagagem, Melhoria.Velocidade)));
            while (s.Hire()) { }
            Assert.IsFalse(s.Move(0, Estacao.Bagagem), "estacao fechada nao recebe ninguem");
            Assert.IsFalse(s.Especializa);
            Assert.AreEqual(120, s.TempoPassos(Estacao.CheckIn), "antes do marco 1 a Ana e' generalista");

            s.TotalEmbarcados = 149;
            s.Cash = 0;
            var ev = new System.Collections.Generic.List<Evento>();
            for (int i = 0; i < 600 * Sim.Hz && s.Marco == 0; i++) { s.Tick(Sim.Step); ev.AddRange(s.Events); }
            Assert.AreEqual(1, s.Marco);
            CollectionAssert.AreEquivalent(new[] { (int)Estacao.Bagagem, (int)Estacao.Alimentacao }, ev.Where(e => e.Tipo == Ev.EstacaoAberta).Select(e => e.A));
            Assert.IsTrue(s.Especializa && s.Desbloqueada(Estacao.Bagagem) && s.Desbloqueada(Estacao.Alimentacao));
            Assert.AreEqual((300L, -1L, false), (s.CustoContratar(Perfil.Bagageiro), s.CustoContratar(Perfil.Mecanico), s.Desbloqueada(Estacao.Manutencao)));
            s.TotalEmbarcados = 600;
            Assert.IsTrue(s.Desbloqueada(Estacao.Manutencao) && s.Desbloqueada(Estacao.Despacho));
            Assert.AreEqual((450L, 450L), (s.CustoContratar(Perfil.Mecanico), s.CustoContratar(Perfil.Despachante)));
        }

        [Test]
        public void Perfis_SeisFixos_TodosNoPrimeiroPostoVazio()
        {
            var s = new Sim { Cash = 100000, TotalEmbarcados = 600 };
            while (s.Hire()) { }
            Assert.AreEqual(Sim.MaxFuncionarios, s.Staff.Count, "Hire() continua parando nos 3 da v0.1");
            foreach (Perfil p in new[] { Perfil.Bagageiro, Perfil.Mecanico, Perfil.Despachante }) Assert.IsTrue(s.Hire(p));
            Assert.IsFalse(s.Hire(Perfil.Mecanico), "um por perfil");
            Assert.AreEqual(Sim.MaxEquipe, s.Staff.Count);
            CollectionAssert.AreEqual(System.Enum.GetValues(typeof(Perfil)), s.Staff.Select(f => f.Perfil));
            CollectionAssert.AreEqual(new[] { Estacao.CheckIn, Estacao.Seguranca, Estacao.Portao, Estacao.Loja, Estacao.CheckIn, Estacao.CheckIn },
                s.Staff.Select(f => f.Em));
            Assert.AreEqual(100000 - 20 - 150 - 400 - 300 - 450 - 450, s.Cash);
        }

        [Test]
        public void Especialista_RendeNaEstacaoDele_OsOutrosRendem1()
        {
            var s = new Sim(Aberto(), 1) { Cash = 100000 };
            while (s.Hire()) { }
            Assert.AreEqual((80, 100), (s.TempoPassos(Estacao.CheckIn), s.TempoPassos(Estacao.Seguranca)), "Ana e Beto: 4 s e 5 s / 1,5");
            Assert.AreEqual(45, s.TempoPassos(Estacao.Portao), "portao sem a Atendente: 1,5 s");
            s.Move(0, Estacao.Portao);
            s.Move(2, Estacao.CheckIn);
            Run(s, 3f);
            Assert.AreEqual((120, 30), (s.TempoPassos(Estacao.CheckIn), s.TempoPassos(Estacao.Portao)), "Caio no check-in rende 1; Ana no portao, 1,5");
            Assert.AreEqual(1.5f, s.Rende(Estacao.Portao));
            s.Move(2, Estacao.Loja);
            Run(s, 3f);
            Assert.AreEqual((1.5f, 90), (s.Rende(Estacao.Loja), s.TempoPassos(Estacao.Loja)), "o Vendedor vende mais, nao atende mais rapido");
        }

        [Test]
        public void Bagagem_SoQuemTemMala_PagaATaxa_SemNinguemPassaDireto()
        {
            Sim s = Pronta(Aberto(), Perfil.Bagageiro);
            var ev = Coleta(s, 100f);
            var taxas = ev.Where(e => e.Tipo == Ev.Venda && e.B == s.T.MalaTaxa).Select(e => s.NovoPax(e.A)).ToList();
            Assert.Greater(taxas.Count, 0);
            Assert.IsTrue(taxas.All(p => p.Mala), "so quem tem mala despachada vai a bagagem");
            Assert.Greater(ev.Count(e => e.Tipo == Ev.Embarcou), 0);

            Sim sem = Pronta(Aberto());
            var ev2 = Coleta(sem, 100f);
            Assert.AreEqual(0, ev2.Count(e => e.Tipo == Ev.Venda && e.B == s.T.MalaTaxa), "sem a Bagageira a esteira esta fechada");
            Assert.AreEqual(0, sem.St(Estacao.Bagagem).Pessoas);
            Assert.IsFalse(sem.Tap(Estacao.Bagagem), "parada opcional nao aceita toque");
        }

        [Test]
        public void Alimentacao_QuemTemFome_ComOVendedorVendeMais()
        {
            Sim s = Pronta(Aberto());
            s.Move(2, Estacao.Alimentacao);   // Caio, o Vendedor, sai do portao
            var ev = Coleta(s, 110f);
            var refeicoes = ev.Where(e => e.Tipo == Ev.Venda && s.NovoPax(e.A).Fome && !s.NovoPax(e.A).Loja).ToList();
            Assert.Greater(refeicoes.Count, 0);
            Assert.IsTrue(refeicoes.All(e => e.B == (int)System.Math.Round(s.T.ComidaVenda * 1.5)), "venda x 1,5 com o Vendedor");
            Assert.IsTrue(ev.Where(e => e.Tipo == Ev.Venda).All(e => s.NovoPax(e.A).Fome), "na alimentacao so quem tem fome (a loja esta fechada)");
        }

        [Test]
        public void Oficina_ComGente_InspecaoDivideOTempo_EOVooDoPendenteEmbarca()
        {
            Sim sem = Pronta(Aberto()), com = Pronta(Aberto(), Perfil.Mecanico);
            sem.Avioes[1].Desgaste = 100; com.Avioes[1].Desgaste = 100;
            Run(sem, 180f);
            var ev = Coleta(com, 180f);
            Assert.AreEqual(0, sem.Voos[1].Embarcados, "Fase C: so o mecanico, 110 s: o voo do pendente nao embarca");
            Assert.AreEqual(sem.Voos[1].Total, com.Voos[1].Embarcados, "com o Edu na oficina: 110 / (1 + 1,5) = 44 s, embarca todo mundo");
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.ChecagemFeita && e.A == 1 && e.B == 50), "a inspecao tira o mesmo tanto");
        }

        [Test]
        public void Despacho_ComGente_BonusDePontualidadeMaior()
        {
            Sim sem = Pronta(Aberto()), com = Pronta(Aberto(), Perfil.Despachante);
            var a = Coleta(sem, 125f).Single(e => e.Tipo == Ev.Bonus && e.A == 0);
            var b = Coleta(com, 125f).Single(e => e.Tipo == Ev.Bonus && e.A == 0);
            long receita = sem.Voos[0].Receita;
            Assert.AreEqual(receita, com.Voos[0].Receita);
            Assert.AreEqual(((int)System.Math.Round(receita * 0.25), (int)System.Math.Round(receita * (0.25 + 0.25 * 1.5))), (a.B, b.B));
            Assert.AreEqual(-1, com.Custo(Estacao.Despacho, Melhoria.Velocidade), "o despacho nao tem upgrade");
        }

        [Test]
        public void ChaveDesligada_SoVisual_AEquipeFicaMasNaoRende()
        {
            Tuning t = Aberto();
            t.RegraBagagem = false; t.RegraOficina = false; t.RegraEspecialista = false;
            Sim s = Pronta(t, Perfil.Bagageiro, Perfil.Mecanico);
            s.Avioes[1].Desgaste = 100;
            var ev = Coleta(s, 180f);
            Assert.AreEqual(Estacao.Bagagem, s.Staff[3].Em, "a Dani esta na esteira (a tela mostra)...");
            Assert.AreEqual(0, ev.Count(e => e.Tipo == Ev.Venda && e.B == t.MalaTaxa), "...mas ninguem vai la");
            Assert.AreEqual(0, s.Voos[1].Embarcados, "oficina so visual: a inspecao continua de 110 s");
            Assert.AreEqual((-1L, 120), (s.Custo(Estacao.Bagagem, Melhoria.Velocidade), s.TempoPassos(Estacao.CheckIn)), "sem upgrade a venda e sem especialista");
        }
    }
}
