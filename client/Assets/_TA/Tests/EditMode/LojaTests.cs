using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Compra em 2 passos (Loja.cs, GDD §7.4 e §16.6): validar no servidor, creditar, gravar e so entao confirmar na loja.</summary>
    public class LojaTests
    {
        const string Pequeno = "gems_pack_small", Pintura = "aircraft_skin_blue_01", Boas = "welcome_bundle_01";

        static (Loja, Sim) Nova(long gems = 0)
        {
            var s = new Sim(null, 1) { Gems = gems };
            return (new Loja(() => s), s);
        }

        /// <summary>O caminho feliz, passo a passo: nada e' confirmado antes do save gravado.</summary>
        [Test]
        public void Compra_ValidaCreditaGravaEConfirma()
        {
            var (l, s) = Nova();
            Loja.Pedido p = l.Recebida("tok1", Pequeno, false, false, 0);
            Assert.AreEqual(EstadoCompra.Validando, p.Estado);
            Assert.AreSame(p, l.ProximoParaValidar(0));
            Assert.IsNull(l.ProximoParaValidar(0), "um pedido no servidor por vez");
            l.Resposta("tok1", Veredito.Ok, "GPA.1", null, 0);
            Assert.AreEqual(100, s.Gems);
            Assert.AreEqual(EstadoCompra.Gravar, p.Estado);
            CollectionAssert.IsEmpty(l.ParaConfirmar(), "sem save gravado, nada de acknowledge");
            l.Gravado();
            CollectionAssert.AreEqual(new[] { "tok1" }, l.ParaConfirmar().Select(x => x.Token));
            l.Confirmada("tok1");
            CollectionAssert.IsEmpty(l.Pedidos);
        }

        /// <summary>§16.6 repeticao de callback e reabrir o app antes do acknowledge: o mesmo tx credita 1x.</summary>
        [Test]
        public void CallbackRepetido_E_Reentrega_CreditamUmaVez()
        {
            var (l, s) = Nova();
            l.Recebida("tok1", Pequeno, false, false, 0);
            l.Recebida("tok1", Pequeno, false, false, 0);
            Assert.AreEqual(1, l.Pedidos.Count);
            l.ProximoParaValidar(0);
            l.Resposta("tok1", Veredito.Ok, "GPA.1", null, 0);
            l.Resposta("tok1", Veredito.Ok, "GPA.1", null, 0);
            Assert.AreEqual(100, s.Gems);

            // o app fechou antes do acknowledge: a loja reentrega, o servidor diz ok de novo, o nucleo responde Repetido e confirma
            var l2 = new Loja(() => s);
            l2.Recebida("tok1", Pequeno, false, false, 5);
            l2.ProximoParaValidar(5);
            l2.Resposta("tok1", Veredito.Ok, "GPA.1", null, 5);
            Assert.AreEqual(Credito.Repetido, l2.Do("tok1").Credito);
            Assert.AreEqual(100, s.Gems);
            l2.Gravado();
            Assert.AreEqual(1, l2.ParaConfirmar().Count());
        }

        /// <summary>§16.6 backend indisponivel e depois reconectado: espera crescente, sem credito ate o ok.</summary>
        [Test]
        public void ServidorFora_EsperaCrescente_E_CreditaAoVoltar()
        {
            var (l, s) = Nova();
            l.Recebida("tok1", Pequeno, false, false, 0);
            double t = 0;
            double[] esperas = new double[10];
            for (int i = 0; i < esperas.Length; i++)
            {
                Loja.Pedido p = l.ProximoParaValidar(t);
                Assert.IsNotNull(p, "tentativa " + i);
                l.Resposta("tok1", Veredito.Indisponivel, null, null, t);
                esperas[i] = p.Proxima - t;
                Assert.IsNull(l.ProximoParaValidar(t + esperas[i] - 0.01), "antes da hora, nao");
                t = p.Proxima;
            }
            CollectionAssert.AreEqual(new double[] { 2, 4, 8, 16, 32, 64, 128, 256, 300, 300 }, esperas);
            Assert.AreEqual(0, s.Gems);
            l.ProximoParaValidar(t);
            l.Resposta("tok1", Veredito.Ok, "GPA.1", null, t);
            Assert.AreEqual(100, s.Gems);
        }

        /// <summary>Ok sem tx (resposta truncada) nao credita: conta como servidor fora.</summary>
        [Test]
        public void OkSemTx_NaoCredita()
        {
            var (l, s) = Nova();
            l.Recebida("tok1", Pequeno, false, false, 0);
            l.ProximoParaValidar(0);
            l.Resposta("tok1", Veredito.Ok, " ", null, 0);
            Assert.AreEqual(0, s.Gems);
            Assert.AreEqual(EstadoCompra.Validando, l.Do("tok1").Estado);
        }

        /// <summary>§16.6 compra pendente (pagamento ainda nao aprovado): nada vai ao servidor nem e' creditado; quando a loja entrega de novo
        /// sem pendencia, segue.</summary>
        [Test]
        public void PagamentoPendente_EsperaALoja()
        {
            var (l, s) = Nova();
            l.Recebida("tok1", Pequeno, true, false, 0);
            Assert.AreEqual(EstadoCompra.Esperando, l.Do("tok1").Estado);
            Assert.IsNull(l.ProximoParaValidar(1000));
            l.Recebida("tok1", Pequeno, false, false, 1000);
            l.ProximoParaValidar(1000);
            l.Resposta("tok1", Veredito.Pendente, null, null, 1000);
            Assert.AreEqual(EstadoCompra.Esperando, l.Do("tok1").Estado, "o servidor tambem pode dizer pendente");
            Assert.AreEqual(0, s.Gems);
        }

        /// <summary>§16.3 token forjado, cancelado ou reembolsado: recusado, sem credito e sem acknowledge.</summary>
        [Test]
        public void Recusada_NaoCreditaNemConfirma()
        {
            var (l, s) = Nova();
            l.Recebida("forjado", Pequeno, false, false, 0);
            l.ProximoParaValidar(0);
            l.Resposta("forjado", Veredito.Recusado, "GPA.9", "desconhecido", 0);
            Assert.AreEqual(0, s.Gems);
            Assert.AreEqual(EstadoCompra.Recusada, l.Do("forjado").Estado);
            Assert.AreEqual("desconhecido", l.Do("forjado").Motivo);
            l.Gravado();
            CollectionAssert.IsEmpty(l.ParaConfirmar());
            l.Esquecer("forjado");
            CollectionAssert.IsEmpty(l.Pedidos);
        }

        /// <summary>CONTRATO_FASE_D1 §4.10: acima do teto ou com o save recusado, nao credita nem confirma (a loja devolve); da para tentar
        /// de novo depois de gastar Gems.</summary>
        [Test]
        public void Teto_Falha_SemConfirmar_E_TentaDeNovo()
        {
            var t = new Tuning();
            var (l, s) = Nova(t.GemsTeto - 50);
            l.Recebida("tok1", Pequeno, false, false, 0);
            l.ProximoParaValidar(0);
            l.Resposta("tok1", Veredito.Ok, "GPA.1", null, 0);
            Assert.AreEqual(Credito.Teto, l.Do("tok1").Credito);
            Assert.AreEqual(EstadoCompra.Falhou, l.Do("tok1").Estado);
            l.Gravado();
            CollectionAssert.IsEmpty(l.ParaConfirmar());
            s.Gems = 0;
            l.TentarDeNovo("tok1", 1);
            l.ProximoParaValidar(1);
            l.Resposta("tok1", Veredito.Ok, "GPA.1", null, 1);
            Assert.AreEqual(100, s.Gems);
            Assert.AreEqual(EstadoCompra.Gravar, l.Do("tok1").Estado);
        }

        /// <summary>§16.6 restaurar depois de reinstalar: o nao consumivel volta para a posse e nao e' confirmado de novo na loja.</summary>
        [Test]
        public void Restaurar_DevolveAPosse_SemConfirmarDeNovo()
        {
            var (l, s) = Nova();
            l.Recebida("tokPintura", Pintura, false, true, 0);
            l.ProximoParaValidar(0);
            l.Resposta("tokPintura", Veredito.Ok, "GPA.2", null, 0);
            Assert.IsTrue(s.Tem(Pintura));
            l.Gravado();
            CollectionAssert.IsEmpty(l.ParaConfirmar());
            CollectionAssert.IsEmpty(l.Pedidos);
        }

        /// <summary>Pacote de boas-vindas ja na posse com outra transacao: JaTem, confirma (a loja nao cobra 2x) e nao paga de novo.</summary>
        [Test]
        public void NaoConsumivelJaNaPosse_ConfirmaSemPagar()
        {
            var (l, s) = Nova();
            Assert.AreEqual(Credito.Creditado, s.Creditar("GPA.antigo", Boas));
            long gems = s.Gems;
            l.Recebida("tok2", Boas, false, false, 0);
            l.ProximoParaValidar(0);
            l.Resposta("tok2", Veredito.Ok, "GPA.novo", null, 0);
            Assert.AreEqual(Credito.JaTem, l.Do("tok2").Credito);
            Assert.AreEqual(gems, s.Gems);
            l.Gravado();
            Assert.AreEqual(1, l.ParaConfirmar().Count());
        }

        /// <summary>§16.3 produto desconhecido, token vazio ou enorme, o mesmo token com outro produto, fila cheia: ignorado.</summary>
        [Test]
        public void Entrada_Invalida_Ignorada()
        {
            var (l, _) = Nova();
            Assert.IsNull(l.Recebida("tok", "gems_pack_giant", false, false, 0));
            Assert.IsNull(l.Recebida("", Pequeno, false, false, 0));
            Assert.IsNull(l.Recebida(null, Pequeno, false, false, 0));
            Assert.IsNull(l.Recebida(new string('x', Loja.MaxToken + 1), Pequeno, false, false, 0));
            Assert.IsNotNull(l.Recebida("tok", Pequeno, false, false, 0));
            Assert.IsNull(l.Recebida("tok", "gems_pack_large", false, false, 0), "token nao muda de produto");
            for (int i = 0; i < Loja.MaxPedidos + 10; i++) l.Recebida("t" + i, Pequeno, false, false, 0);
            Assert.AreEqual(Loja.MaxPedidos, l.Pedidos.Count);
            l.Resposta("nunca-pedido", Veredito.Ok, "GPA.x", null, 0);
            l.Resposta("tok", Veredito.Ok, "GPA.x", null, 0);
            Assert.AreEqual(EstadoCompra.Validando, l.Do("tok").Estado, "resposta sem pedido no servidor nao vale");
        }

        /// <summary>A Loja segue o Sim atual: excluir os dados troca o Sim, e o credito vai para o novo.</summary>
        [Test]
        public void SegueOSimAtual()
        {
            var a = new Sim(null, 1);
            Sim atual = a;
            var l = new Loja(() => atual);
            l.Recebida("tok1", Pequeno, false, false, 0);
            l.ProximoParaValidar(0);
            atual = new Sim(null, 2);
            l.Resposta("tok1", Veredito.Ok, "GPA.1", null, 0);
            Assert.AreEqual(0, a.Gems);
            Assert.AreEqual(100, atual.Gems);
        }
    }
}
