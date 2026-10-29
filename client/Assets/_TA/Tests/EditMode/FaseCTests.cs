using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Regras da Fase C (CONTRATO_FASE_C §1-§4): ciclo do aviao, tripulacao, patio e manutencao. As invariantes ficam no ExploitTests.</summary>
    public class FaseCTests
    {
        static void Run(Sim s, float seg) => RegrasTests.Run(s, seg);
        static System.Collections.Generic.List<Evento> Coleta(Sim s, float seg) => RegrasTests.Coleta(s, seg);

        /// <summary>Equipe do terminal completa (o fluxo nao atrapalha) e 2 pessoas de patio (o patio nao atrasa).</summary>
        static Sim Pronta(Tuning t = null)
        {
            var s = new Sim(t, 1) { Cash = 10000 };
            while (s.Hire()) { }
            s.EquipePatio = 2;
            s.Cash = 0;
            return s;
        }

        /// <summary>A regra da tripulacao ligada (o padrao e' so visual: BALANCE.md D-C2).</summary>
        static Tuning ComTrip() => new Tuning { RegraTripulacao = true };

        static Tripulante Trip(Sim s, Funcao f, int i) => s.Tripulacao.Where(t => t.Funcao == f).ElementAt(i);

        [Test]
        public void Ciclo_EncostaNaHora_EscalaAutomaticaPegaOPrimeiroLivre_EmbarqueAbreNaHora()
        {
            Sim s = Pronta();
            var ev = Coleta(s, 20f);
            Assert.IsFalse(s.Avioes[0].NoPatio, "voo 1: encosta 100 s antes do fechamento (aos 20 s)");
            ev = Coleta(s, Sim.Step);
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.Pousou && e.A == 0 && e.B == 0));
            Assert.AreEqual((0, 0), (Trip(s, Funcao.Piloto, 0).Aviao, Trip(s, Funcao.Comissario, 0).Aviao), "o 1o livre na ordem do quadro");
            Assert.AreEqual(2, ev.Count(e => e.Tipo == Ev.Escalado && e.B == 0 && e.C == 0));
            ev = Coleta(s, 40f);
            Assert.IsTrue(s.Voos[0].Embarque, "pronto antes da hora: o embarque abre aos 60 s, como na v0.1");
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.EmbarqueAberto && e.A == 0));
            ev = Coleta(s, 60f);
            Evento d = ev.Single(e => e.Tipo == Ev.Decolou);
            Assert.AreEqual((0, 0, 0), (d.A, d.B, d.C), "decolou no fechamento, sem atraso");
            Assert.AreEqual((1, 15), (Trip(s, Funcao.Piloto, 0).Trechos, s.Avioes[0].Desgaste));
            Run(s, 80f);
            Assert.IsTrue(s.Avioes[0].NoPatio, "voa 80 s e volta para o voo 1 da onda seguinte");
            Assert.AreEqual(2, s.Avioes[0].VooId);
        }

        [Test]
        public void Tripulacao_SemNinguemLivre_EmbarqueEspera_AvisaUmaVez()
        {
            Sim s = Pronta(ComTrip());
            foreach (Tripulante t in s.Tripulacao) { t.Trechos = s.T.TrechosAteDescanso; t.Sala = 0; }   // todos descansando ate os 180 s
            var ev = Coleta(s, 121f);
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.SemTripulacao && e.A == 0), "aviso antecipado, 1x por voo");
            Assert.AreEqual(0, ev.Count(e => e.Tipo == Ev.EmbarqueAberto && e.A == 0), "sem tripulacao o embarque nao abre");
            Assert.AreEqual(0, s.Voos[0].Embarcados);
            Assert.AreEqual(0, ev.Count(e => e.Tipo == Ev.Decolou), "voo sem embarque nao decola");
            Assert.IsTrue(s.Avioes[0].NoPatio, "o aviao fica no patio para o proximo voo");
            Run(s, 59f + 20f);
            Assert.AreEqual((2, 0), (s.Avioes[0].VooId, Trip(s, Funcao.Piloto, 0).Aviao), "descansados aos 180 s: o voo 1 da onda 2 tem tripulacao");
        }

        [Test]
        public void Tripulacao_DepoisDeNTrechos_DescansaNaSala()
        {
            Sim s = Pronta(ComTrip());
            Tripulante p0 = Trip(s, Funcao.Piloto, 0);
            p0.Trechos = s.T.TrechosAteDescanso - 1;
            Run(s, 121f);
            Assert.AreEqual(s.T.TrechosAteDescanso, p0.Trechos);
            var ev = Coleta(s, 80f);   // pousa aos 200 s
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.Descanso && e.A == p0.Id && e.B == 180));
            Assert.IsFalse(s.Livre(p0));
            Assert.AreEqual(Trip(s, Funcao.Piloto, 2).Aviao, 0, "o reserva assume");
            Run(s, 179f);
            Assert.IsFalse(s.Livre(p0));
            Run(s, 1f);
            Assert.AreEqual((0, 0), (p0.Trechos, p0.Aviao), "180 s na sala: descansado, e o aviao 0 que pousou agora o leva (o 1o livre na ordem)");
        }

        [Test]
        public void Tripulacao_EscalaManual_SoComOAviaoNoPatioEAntesDoEmbarque()
        {
            Sim s = Pronta(ComTrip());
            Assert.IsFalse(s.Escala(0, 2), "aviao ainda nao encostou");
            Run(s, 25f);
            Tripulante p0 = Trip(s, Funcao.Piloto, 0), p2 = Trip(s, Funcao.Piloto, 2), c0 = Trip(s, Funcao.Comissario, 0);
            p0.Sala = 50;
            Assert.IsTrue(s.Escala(0, p2.Id));
            Assert.AreEqual((-1, 0, 50), (p0.Aviao, p2.Aviao, p0.Sala), "o trocado volta para a sala com o descanso que tinha");
            Assert.AreEqual(1, s.ABordo(0, Funcao.Piloto));
            Assert.IsFalse(s.Escala(0, c0.Id), "quem ja esta a bordo nao esta livre");
            Assert.IsFalse(s.Escala(7, p0.Id), "voo que nao existe");
            Assert.IsFalse(s.Escala(0, 99));
            Assert.IsTrue(s.Escala(0, p0.Id), "pode destrocar");
            Run(s, 36f);
            Assert.IsTrue(s.Voos[0].Embarque);
            Assert.IsFalse(s.Escala(0, p2.Id), "embarque aberto: a escala trava");
        }

        [Test]
        public void Tripulacao_Contratacao_PrecoFixo_TetoSobeComOMarco()
        {
            var s = new Sim { Cash = 10000 };
            Assert.AreEqual((-1L, -1L), (s.CustoTripulante(Funcao.Piloto), s.CustoTripulante(Funcao.Comissario)), "marco 0: o teto e' a tripulacao inicial");
            Assert.IsFalse(s.HireTripulante(Funcao.Piloto));
            s.TotalEmbarcados = 150;
            Assert.AreEqual((300L, 150L), (s.CustoTripulante(Funcao.Piloto), s.CustoTripulante(Funcao.Comissario)));
            Assert.IsTrue(s.HireTripulante(Funcao.Piloto));
            Tripulante novo = s.Tripulacao.Last();
            Assert.AreEqual((6, Funcao.Piloto, "Cmte. Bruno", true), (novo.Id, novo.Funcao, novo.Nome, s.Livre(novo)), "entra descansado, com o proximo nome");
            Assert.AreEqual(-1L, s.CustoTripulante(Funcao.Piloto), "teto 4 no marco 1");
            Assert.AreEqual(10000 - 300, s.Cash);
            s.Cash = 149;
            Assert.IsFalse(s.HireTripulante(Funcao.Comissario), "sem Cash");
        }

        [Test]
        public void Patio_EmbarqueSoComDesembarqueELimpeza_ServicosNaOrdem()
        {
            Sim s = Pronta();
            s.EquipePatio = 1;
            var ev = Coleta(s, 50f);
            int[] feitos = ev.Where(e => e.Tipo == Ev.ServicoFeito && e.A == 0).Select(e => e.B).ToArray();
            CollectionAssert.AreEqual(new[] { (int)Servico.Desembarque, (int)Servico.Limpeza }, feitos, "1 pessoa: desembarque e depois limpeza");
            Assert.IsTrue(s.Pronto(0));
            var t = new Tuning { ServicoSeg = new[] { 30f, 30f, 1f, 1f } };   // 60 s de patio antes do embarque: 20 s de atraso
            s = Pronta(t);
            s.EquipePatio = 1;
            Run(s, 60f + Sim.Step);
            Assert.AreEqual((false, 1), (s.Voos[0].Embarque, s.AtrasosPatio), "na hora do embarque, sem a limpeza: espera e conta o atraso do patio");
            Run(s, 20f);
            Assert.IsTrue(s.Voos[0].Embarque);
        }

        [Test]
        public void Patio_UmaPessoa_AtrasaADecolagem_SemBonus()
        {
            Sim s = Pronta();
            s.EquipePatio = 1;
            var ev = Coleta(s, 125f);
            Evento d = ev.Single(e => e.Tipo == Ev.Decolou && e.A == 0);
            Assert.AreEqual(4, d.C, "a bagagem do voo 1 esperou o desembarque e a limpeza do voo 2: decola 4 s depois do portao");
            Assert.AreEqual(0, ev.Count(e => e.Tipo == Ev.Bonus && e.A == 0), "atrasado: sem bonus de pontualidade, mesmo cheio");
            Assert.AreEqual(s.Voos[0].Total, s.Voos[0].Embarcados);
            Assert.AreEqual(1, s.AtrasosPatio);
            ev = Coleta(s, 55f);
            Assert.AreEqual(0, ev.Single(e => e.Tipo == Ev.Decolou && e.A == 1).C, "o voo 2 sai na hora");
            s.Cash = 39;
            Assert.AreEqual((false, 40L), (s.HirePatio(), s.CustoPatio()), "a 2a pessoa custa 40 (com 39, nao contrata)");
        }

        [Test]
        public void Patio_Upgrades_CustoTempoEVeiculos()
        {
            var s = new Sim { Cash = 10000 };
            long[] vel = new long[4];
            for (int i = 0; i < 4; i++) { vel[i] = s.CustoPatio(Servico.Bagagem, Melhoria.Velocidade); s.BuyPatio(Servico.Bagagem, Melhoria.Velocidade); }
            CollectionAssert.AreEqual(new long[] { 30, 50, 75, -1 }, vel);
            Assert.AreEqual((int)System.Math.Round(24.0 * System.Math.Pow(0.88, 3) * Sim.Hz), s.ServicoPassos(Servico.Bagagem));
            Assert.AreEqual(120, s.CustoPatio(Servico.Combustivel, Melhoria.Capacidade));
            Assert.IsTrue(s.BuyPatio(Servico.Combustivel, Melhoria.Capacidade));
            Assert.AreEqual((2, -1L), (s.Veiculos(Servico.Combustivel), s.CustoPatio(Servico.Combustivel, Melhoria.Capacidade)), "mais um caminhao de combustivel");
            Assert.AreEqual((40L, 150L), (s.CustoPatio(), Contrata(s)));
            Assert.AreEqual((-1L, 3), (s.CustoPatio(), s.EquipePatio), "teto 3");
        }

        static long Contrata(Sim s) { s.HirePatio(); long c = s.CustoPatio(); s.HirePatio(); return c; }

        [Test]
        public void Manutencao_Desgaste_AvisoEm80_PendenteEm100_InspecaoAutomatica()
        {
            Sim s = Pronta();
            s.Avioes[0].Desgaste = 75; s.Avioes[1].Desgaste = 95;
            var ev = Coleta(s, 171f);
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.ManutencaoAviso && e.A == 0 && e.B == 90));
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.ManutencaoPendente && e.A == 1 && e.B == 100));
            ev = Coleta(s, 80f);   // o aviao 1 pousa aos 250 s, pendente
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.ChecagemIniciada && e.A == 1 && e.B == 0 && e.C == 1), "o mecanico vai sozinho, sem Cash");
            Assert.IsFalse(s.Pronto(1), "pendente: o embarque nao abre");
            Assert.IsFalse(s.Checagem(1), "pendente recebe a inspecao, nao a checagem rapida");
            ev = Coleta(s, 110f);
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.ChecagemFeita && e.A == 1 && e.B == 50), "a inspecao tira so metade");
            Assert.AreEqual(0, ev.Count(e => e.Tipo == Ev.EmbarqueAberto && e.A == 3), "110 s de inspecao: o voo 2 da onda 2 nao embarca");
            Assert.AreEqual(s.T.Pax(1), ev.Single(e => e.Tipo == Ev.VooPerdido && e.A == 3).B, "custa so a receita dele");
        }

        [Test]
        public void Manutencao_ChecagemRapida_UmaPorTurnaround_SoAntesDoEmbarque_SoComCash()
        {
            Sim s = Pronta();
            Run(s, 25f);
            Aviao a = s.Avioes[0];
            a.Desgaste = 30;
            s.Cash = s.T.CheckCusto - 1;
            Assert.IsFalse(s.Checagem(0), "sem Cash");
            s.Cash = s.T.CheckCusto;
            Assert.IsTrue(s.Checagem(0));
            Assert.AreEqual((0L, false), (s.Cash, s.MecanicoLivre));
            s.Cash = 100;
            Assert.IsFalse(s.Checagem(0), "uma por turnaround");
            var ev = Coleta(s, 8f);
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.ChecagemFeita && e.A == 0 && e.B == 20));
            Assert.IsFalse(s.Checagem(1), "aviao que nao esta no patio");
            Run(s, 50f);
            Assert.IsFalse(s.Checagem(0), "embarque aberto");
        }

        [Test]
        public void Manutencao_Hangar_ZeraEVoltaPronto_OVooDaquelaOndaNaoEmbarca()
        {
            Sim s = Pronta();
            Run(s, 30f);
            s.Avioes[0].Desgaste = 90;
            s.Cash = s.T.HangarCusto - 1;
            Assert.IsFalse(s.Hangar(0), "sem Cash");
            s.Cash = s.T.HangarCusto;
            Assert.IsTrue(s.Hangar(0));
            Assert.AreEqual((0L, false, -1, -1), (s.Cash, s.Avioes[0].NoPatio, Trip(s, Funcao.Piloto, 0).Aviao, Trip(s, Funcao.Comissario, 0).Aviao),
                "a tripulacao volta para a sala");
            var ev = Coleta(s, 150f + Sim.Step);
            Assert.AreEqual(0, ev.Count(e => e.Tipo == Ev.EmbarqueAberto && e.A == 0));
            Assert.AreEqual(s.T.Pax(0), ev.Single(e => e.Tipo == Ev.VooPerdido && e.A == 0).B, "custa so o voo dele");
            Assert.AreEqual(0, ev.Count(e => e.Tipo == Ev.VooPerdido && e.A == 1), "o outro voo segue");
            ev = Coleta(s, 30f);
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.HangarFeito && e.A == 0));
            Aviao a = s.Avioes[0];
            Assert.AreEqual((true, true, 0, 2), (a.NoPatio, a.PatioPronto, a.Desgaste, a.VooId), "volta limpo, abastecido e zerado para o voo 1 da onda 2");
            Run(s, 30f);
            Assert.IsTrue(s.Voos[0].Embarque);
            Assert.IsFalse(s.Hangar(0), "embarque aberto: nao vai ao hangar");
        }

        [Test]
        public void RegrasDesligadas_SoVisual_EmbarqueAbreSemTripulacaoPatioNemManutencao()
        {
            Sim s = Pronta(BalanceTests.SemFaseC());
            foreach (Tripulante t in s.Tripulacao) { t.Trechos = 9; t.Sala = 0; }
            s.Avioes[0].Desgaste = 100;
            s.EquipePatio = 1;
            var ev = Coleta(s, 121f);
            Assert.IsTrue(s.Voos[0].Embarque);
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.Bonus && e.A == 0), "como na v0.1");
            Assert.AreEqual(0, ev.Count(e => e.Tipo == Ev.ChecagemIniciada || e.Tipo == Ev.SemTripulacao));
            Assert.AreEqual(100, s.Avioes[0].Desgaste, "sem regra o desgaste nao anda");
        }
    }
}
