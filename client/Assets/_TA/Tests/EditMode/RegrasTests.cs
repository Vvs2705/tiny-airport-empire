using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Regras da v0.1 (CONTRATO_V01 §3 e §4): onda, fluxo FIFO, toque, equipe, estados, upgrades, marcos, historia, loja, bonus.</summary>
    public class RegrasTests
    {
        internal static void Run(Sim s, float seg)
        {
            int n = (int)System.Math.Round(seg * Sim.Hz);
            for (int i = 0; i < n; i++) s.Tick(Sim.Step);
        }

        /// <summary>Anda passo a passo e junta todos os eventos.</summary>
        internal static System.Collections.Generic.List<Evento> Coleta(Sim s, float seg)
        {
            var l = new System.Collections.Generic.List<Evento>();
            int n = (int)System.Math.Round(seg * Sim.Hz);
            for (int i = 0; i < n; i++) { s.Tick(Sim.Step); l.AddRange(s.Events); }
            return l;
        }

        /// <summary>Tira da fila do check-in o 1o passageiro do voo 1 e o poe no portao.</summary>
        static void PoeNoPortao(Sim s)
        {
            var fila = s.St(Estacao.CheckIn).Fila;
            int i = fila.FindIndex(p => p.Voo == 0);
            s.St(Estacao.Portao).Fila.Add(fila[i]);
            fila.RemoveAt(i);
        }

        static Sim ComEquipe(int n)
        {
            var s = new Sim { Cash = 10000 };
            for (int i = 0; i < n; i++) Assert.IsTrue(s.Hire());
            s.Cash = 0;
            return s;
        }

        [Test]
        public void Onda_Mostra2VoosComHorarioEmTempoDeJogo()
        {
            var s = new Sim();
            var ev = Coleta(s, Sim.Step);
            Assert.AreEqual(2, ev.Count(e => e.Tipo == Ev.VooCriado));
            Assert.AreEqual((120 * Sim.Hz, 170 * Sim.Hz), (s.Voos[0].FechaPasso, s.Voos[1].FechaPasso));
            Assert.AreEqual((60 * Sim.Hz, 110 * Sim.Hz), (s.Voos[0].AbrePasso, s.Voos[1].AbrePasso), "embarque abre 60 s antes");
            Assert.AreEqual((10, 10), (s.Voos[0].Total, s.Voos[1].Total));
            Run(s, 180f);
            Assert.AreEqual((1, 2, 14), (s.Onda, s.Voos[0].Id, s.Voos[0].Total), "a onda 2 comeca aos 180 s com +4 passageiros por voo");
        }

        [Test]
        public void Chegadas_EspalhadasAteFechamentoMenos70_ETodosChegam()
        {
            var s = new Sim();
            Run(s, Sim.Step);
            for (int f = 0; f < 2; f++)
                for (int i = 0; i < s.Voos[f].Total; i++)
                    Assert.That(s.Chegada(s.Voos[f], i), Is.InRange(0, (int)((s.T.Fecha[f] - 70f) * Sim.Hz)));
            Run(s, 101f);
            Assert.AreEqual((10, 10), (s.Voos[0].Criados, s.Voos[1].Criados));
            Assert.AreEqual(20, s.St(Estacao.CheckIn).Pessoas, "ninguem atende: todos na fila do check-in");
        }

        [Test]
        public void PortaoFechaNaHora_QuemNaoEmbarcouPerdeOVoo()
        {
            var s = new Sim { Cash = 77 };
            var ev = Coleta(s, 120f + Sim.Step);
            Evento partiu = ev.Single(e => e.Tipo == Ev.VooPartiu);
            Assert.AreEqual((0, 0, 10), (partiu.A, partiu.B, partiu.C));
            Assert.AreEqual(10, ev.Single(e => e.Tipo == Ev.VooPerdido).B);
            Assert.AreEqual(10, s.St(Estacao.CheckIn).Pessoas, "so os do voo 2 continuam");
            Assert.AreEqual(77, s.Cash, "perder o voo nao tira Cash");
        }

        [Test]
        public void Fluxo_CheckInSegurancaPortao_FIFO()
        {
            Sim s = ComEquipe(3);   // check-in, seguranca, portao
            var ev = Coleta(s, 170f + Sim.Step);
            int[] emb = ev.Where(e => e.Tipo == Ev.Embarcou && e.A / 500 % 2 == 0).Select(e => e.A % 500).ToArray();
            Assert.AreEqual(10, emb.Length, "o voo 1 embarca inteiro");
            CollectionAssert.AreEqual(emb.OrderBy(x => x).ToArray(), emb, "FIFO: embarcam na ordem de chegada");
            Assert.AreEqual(0, ev.Count(e => e.Tipo == Ev.Venda), "loja sem funcionario: ninguem compra");
        }

        [Test]
        public void Toque_SemFuncionario_ComecaUmAtendimentoNoTempoDaEstacao()
        {
            var s = new Sim();
            Assert.IsFalse(s.Tap(Estacao.CheckIn), "fila vazia");
            Run(s, 40f);
            Station ci = s.St(Estacao.CheckIn);
            int fila = ci.Fila.Count;
            Assert.IsTrue(s.Tap(Estacao.CheckIn));
            Assert.IsTrue(s.Tap(Estacao.CheckIn), "2 balcoes no check-in");
            Assert.IsFalse(s.Tap(Estacao.CheckIn), "sem balcao livre");
            Assert.AreEqual((fila - 2, 2), (ci.Fila.Count, ci.Atendendo.Count));
            Assert.AreEqual(4 * Sim.Hz, ci.Atendendo[0].Resto, "o toque nao acelera: 4,0 s");
            Run(s, 4f);
            Assert.AreEqual((0, 2), (ci.Atendendo.Count, s.St(Estacao.Seguranca).Fila.Count));
            Assert.IsFalse(s.Tap(Estacao.Loja), "loja sem funcionario esta fechada");
        }

        [Test]
        public void Toque_EstacaoComFuncionarioNaoPrecisa()
        {
            Sim s = ComEquipe(1);
            Run(s, 30f);
            Assert.IsFalse(s.Tap(Estacao.CheckIn));
            Assert.IsFalse(s.PodeTocar(Estacao.CheckIn));
        }

        [Test]
        public void Portao_SoEmbarcaDepoisDeAbrir()
        {
            var s = new Sim();
            Run(s, 50f);
            PoeNoPortao(s);
            Assert.IsFalse(s.Tap(Estacao.Portao), "embarque do voo 1 abre aos 60 s");
            Run(s, 10.1f);
            Assert.IsTrue(s.Tap(Estacao.Portao));
        }

        [Test]
        public void Mover_Anda2_5sSemAtenderEmNenhumaDasDuas()
        {
            Sim s = ComEquipe(1);
            Assert.IsFalse(s.Move(0, Estacao.CheckIn), "ja esta la");
            Assert.IsFalse(s.Move(5, Estacao.Seguranca), "funcionario que nao existe");
            Assert.IsTrue(s.Move(0, Estacao.Seguranca));
            var ev = Coleta(s, 2.5f - Sim.Step);
            Assert.AreEqual((0, 0), (s.Presentes(Estacao.CheckIn), s.Presentes(Estacao.Seguranca)));
            Assert.AreEqual(1, s.Alocados(Estacao.Seguranca));
            ev = Coleta(s, Sim.Step);
            Assert.AreEqual(1, s.Presentes(Estacao.Seguranca));
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.Chegou && e.B == (int)Estacao.Seguranca));
        }

        [Test]
        public void Contratacao_PrecoFixo_TetoDe3_NoPrimeiroPostoVazio()
        {
            var s = new Sim { Cash = 19 };
            Assert.IsFalse(s.Hire());
            s.Cash = 570;
            Assert.AreEqual(20, s.CustoContratar()); Assert.IsTrue(s.Hire());
            Assert.AreEqual(150, s.CustoContratar()); Assert.IsTrue(s.Hire());
            Assert.AreEqual(400, s.CustoContratar()); Assert.IsTrue(s.Hire());
            Assert.AreEqual((-1L, 0L), (s.CustoContratar(), s.Cash));
            Assert.IsFalse(s.Hire());
            CollectionAssert.AreEqual(new[] { Estacao.CheckIn, Estacao.Seguranca, Estacao.Portao }, s.Staff.Select(f => f.Em).ToArray());
        }

        [Test]
        public void Upgrades_CustoTempoECapacidade()
        {
            var s = new Sim { Cash = 100000 };
            long[] vel = new long[6], cap = new long[3];
            for (int i = 0; i < 6; i++) { vel[i] = s.Custo(Estacao.Seguranca, Melhoria.Velocidade); s.Buy(Estacao.Seguranca, Melhoria.Velocidade); }
            for (int i = 0; i < 3; i++) { cap[i] = s.Custo(Estacao.Seguranca, Melhoria.Capacidade); s.Buy(Estacao.Seguranca, Melhoria.Capacidade); }
            CollectionAssert.AreEqual(new long[] { 40, 65, 100, 165, 260, -1 }, vel);
            CollectionAssert.AreEqual(new long[] { 300, 750, -1 }, cap);
            Assert.AreEqual(100000 - 630 - 1050, s.Cash);
            Assert.AreEqual(4, s.St(Estacao.Seguranca).Postos, "2 balcoes + 2 niveis");
            Assert.AreEqual((int)System.Math.Round(5.0 * System.Math.Pow(0.88, 5) * Sim.Hz), s.TempoPassos(Estacao.Seguranca), "-12% por nivel");
            Assert.IsFalse(s.Buy(Estacao.Seguranca, Melhoria.Velocidade), "no maximo");
            s.Cash = 10;
            Assert.IsFalse(s.Buy(Estacao.Portao, Melhoria.Velocidade), "sem Cash");
        }

        [Test]
        public void Estado_CriticoMostraOVooEmRisco_NormalSemFila()
        {
            Sim s = ComEquipe(3);
            Run(s, 1f);
            s.AtualizaEstados();
            Assert.AreEqual(Estado.Normal, s.St(Estacao.Seguranca).Estado);
            Run(s, 100f);   // aos 101 s o voo 1 fecha em 19 s
            Station seg = s.St(Estacao.Seguranca);
            for (int i = 0; i < 6; i++) seg.Fila.Add(new Pax { Id = Sim.PaxId(0, 0, 400 + i) });   // 6 x 5 s com 1 funcionario: nao da
            s.AtualizaEstados();
            Assert.AreEqual((Estado.Critico, 0), (seg.Estado, seg.VooEmRisco));
            Assert.Less(seg.Folga, 0);
            seg.Fila.Clear();
            seg.Fila.Add(new Pax { Id = Sim.PaxId(0, 0, 400) });
            s.AtualizaEstados();
            Assert.AreEqual(Estado.Atencao, seg.Estado, "da tempo, mas com menos de 20 s de folga");
        }

        [Test]
        public void Marcos_AumentamATarifa()
        {
            var s = new Sim();
            Assert.AreEqual((0, 8), (s.Marco, s.Tarifa));
            s.TotalEmbarcados = 149;
            Run(s, 59f);
            PoeNoPortao(s);
            Run(s, 1.1f);
            Assert.IsTrue(s.Tap(Estacao.Portao));
            var ev = Coleta(s, 1.5f);
            Assert.AreEqual(1, ev.Count(e => e.Tipo == Ev.Marco && e.A == 1));
            Assert.AreEqual(8, ev.Single(e => e.Tipo == Ev.Embarcou).B, "o passageiro do marco paga a tarifa de antes");
            Assert.AreEqual((1, 10), (s.Marco, s.Tarifa));
            s.TotalEmbarcados = 1500;
            Assert.AreEqual((3, 14), (s.Marco, s.Tarifa));
        }

        [Test]
        public void Historia_1PorOndaAPartirDaOnda2()
        {
            var s = new Sim();
            for (int w = 0; w < 6; w++)
            {
                int n = 0;
                for (int f = 0; f < 2; f++)
                    for (int i = 0; i < s.T.Pax(w); i++)
                        if (s.NovoPax(Sim.PaxId(w, f, i)).Historia) n++;
                Assert.AreEqual(w == 0 ? 0 : 1, n, "onda " + (w + 1));
            }
        }

        [Test]
        public void Historia_EmbarcadaATempoEntraNoAlbum()
        {
            Sim s = ComEquipe(3);
            s.Cash = 100000;
            foreach (Station st in s.Stations) { s.Buy(st.Tipo, Melhoria.Velocidade); s.Buy(st.Tipo, Melhoria.Capacidade); }
            var ev = Coleta(s, 360f);
            Evento h = ev.Single(e => e.Tipo == Ev.Historia);
            Assert.AreEqual((0, 1), (h.A, h.C));
            Assert.AreEqual(1, s.Album);
        }

        [Test]
        public void Loja_AbertaVende6_FechadaPassaDireto()
        {
            Sim s = ComEquipe(3);
            s.Cash = 10000;
            s.Move(2, Estacao.Loja);   // o do portao vai para a loja; o portao fica no toque
            s.Cash = 0;
            var ev = Coleta(s, 100f);
            var vendas = ev.Where(e => e.Tipo == Ev.Venda).ToList();
            Assert.Greater(vendas.Count, 0);
            Assert.IsTrue(vendas.All(e => e.B == 6));
        }

        [Test]
        public void Bonus_VooCheio_Mais25PorCento()
        {
            Sim s = ComEquipe(3);
            var ev = Coleta(s, 120f + Sim.Step);
            Evento p = ev.Single(e => e.Tipo == Ev.VooPartiu);
            Assert.AreEqual(p.C, p.B, "voo 1 cheio");
            Assert.AreEqual(20, ev.Single(e => e.Tipo == Ev.Bonus).B, "25% de 10 x 8");
        }

        [Test]
        public void Receita2x_DobraATarifaEnquantoDura()
        {
            Sim s = ComEquipe(3);
            string tx = s.Anuncios.Oferta(Anuncio.Receita);
            Assert.AreEqual(2, s.Anuncios.Pagar(tx));
            var ev = Coleta(s, 100f);
            Assert.IsTrue(ev.Where(e => e.Tipo == Ev.Embarcou).All(e => e.B == 16));
        }

        [Test]
        public void Determinismo_MesmaSementeMesmoJogo_SementeMudaAsChegadas()
        {
            string Joga(int seed)
            {
                var s = new Sim(null, seed);
                var b = new Bot(BotProfile.Bom);
                for (int i = 0; i < 400 * Sim.Hz; i++) b.Step(s);
                return s.Save("x", 1);
            }
            Assert.AreEqual(Joga(3), Joga(3));
            Assert.AreNotEqual(Joga(3), Joga(4));
        }
    }
}
