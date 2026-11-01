using NUnit.Framework;
using UnityEngine;

namespace TA.Tests
{
    /// <summary>As 2 fontes OFL entram no build, cobrem os nomes do jogo com acento e as placas de chao tem material. E os estados do BotaoPreco.</summary>
    public class FontesTests
    {
        [TestCase("Fonts/Nunito-ExtraBold")]
        [TestCase("Fonts/LilitaOne-Regular")]
        public void Fonte_CobreOsNomesDoJogo(string caminho)
        {
            var f = Resources.Load<Font>(caminho);
            Assert.IsNotNull(f, caminho);
            foreach (string[] lista in new[] { Textos.Estacao, Textos.Destino, Textos.Perfil, Textos.Funcionario, Textos.Servico, Textos.Funcao })
                foreach (string t in lista)
                    foreach (char c in t) Assert.IsTrue(f.HasCharacter(c), caminho + " sem '" + c + "' em " + t);
        }

        [Test]
        public void Placas_UsamAFonteDoTexto()
        {
            Assert.IsNotNull(Art.Font().material);
            StringAssert.Contains("Nunito", Art.Font().name);
            StringAssert.Contains("Lilita", Art.Titulo().name);
        }

        [Test]
        public void BotaoPreco_DizOEstadoEOQueFalta()
        {
            var raiz = new GameObject("raiz", typeof(RectTransform));
            BotaoPreco b = Art.NovoBotaoPreco(raiz.transform, Vector2.zero, Vector2.one, () => { });

            b.Preco("Contratar", 100, 100);
            Assert.IsTrue(b.B.interactable);
            Assert.AreEqual("Contratar", b.Rotulo.text);
            Assert.AreEqual(Art.Moeda(), b.Icone.sprite);

            b.Preco("Contratar", 100, 40);
            Assert.IsFalse(b.B.interactable);
            Assert.AreEqual(Textos.Faltam(60), b.Rotulo.text);
            Assert.AreEqual("100", b.Valor.text);

            b.Travado("Terminal", "Nível 3");
            Assert.IsFalse(b.B.interactable);
            Assert.AreEqual(Art.Cadeado(), b.Icone.sprite);

            b.Maximo("Máximo");
            Assert.IsFalse(b.B.interactable);
            Assert.IsFalse(b.Rotulo.gameObject.activeSelf);

            b.Secundario("Mover");
            Assert.IsTrue(b.B.interactable);
            Assert.IsFalse(b.Icone.gameObject.activeSelf);

            b.Principal("Continuar", Art.Compra);
            Assert.IsTrue(b.B.interactable);
            Assert.IsFalse(b.Rotulo.gameObject.activeSelf);
            Assert.AreEqual("Continuar", b.Valor.text);

            b.Video("Dobrar", 1200);
            Assert.IsTrue(b.B.interactable);
            Assert.AreEqual(Art.Video, b.Fundo.color);
            Assert.AreEqual("+" + Textos.Num(1200), b.Valor.text);
            Object.DestroyImmediate(raiz);
        }
    }
}
