using NUnit.Framework;
using TA.Core;
using UnityEngine;

namespace TA.Tests
{
    /// <summary>O quadro de partidas: estado nunca so por cor, e o nome mais longo cabe na coluna sem descer do piso de 32 px.</summary>
    public class HudTests
    {
        static float Largura(Font f, int tam, string t)
        {
            var s = new TextGenerationSettings { font = f, fontSize = tam, fontStyle = FontStyle.Normal, scaleFactor = 1f, lineSpacing = 1f, richText = false,
                generationExtents = new Vector2(4000f, 200f), horizontalOverflow = HorizontalWrapMode.Overflow, verticalOverflow = VerticalWrapMode.Overflow };
            return new TextGenerator().GetPreferredWidth(t, s);
        }

        [Test]
        public void Situacao_CadaEstadoTemTextoEIconeProprios()
        {
            var casos = new (Voo v, Aviao a, int passo, Sprite icone)[]
            {
                (new Voo { Partiu = true, Total = 5, Embarcados = 5 }, new Aviao(), 0, Art.Check()),
                (new Voo { Partiu = true, Total = 5, Embarcados = 3 }, new Aviao(), 0, Art.Alerta()),
                (new Voo { Embarque = true }, new Aviao(), 0, Art.Pessoas()),
                (new Voo { AbrePasso = 10 }, new Aviao(), 0, Art.Bilhete()),
                (new Voo(), new Aviao { Hangar = 1 }, 0, Art.Chave()),
                (new Voo(), new Aviao(), 0, Art.Aviao()),
                (new Voo(), new Aviao { NoPatio = true, Check = 1 }, 0, Art.Chave()),
                (new Voo(), new Aviao { NoPatio = true }, 0, Art.Caminhao()),
            };
            var textos = new System.Collections.Generic.HashSet<string>();
            foreach (var c in casos)
            {
                Assert.AreEqual(c.icone, Hud.Situacao(c.v, c.a, c.passo).icone, Textos.Status(c.v, c.a, c.passo));
                textos.Add(Textos.Status(c.v, c.a, c.passo));
            }
            Assert.AreEqual(casos.Length, textos.Count, "dois estados com o mesmo texto");
        }

        [Test]
        public void Quadro_NomeMaisLongoCabeNaColuna()
        {
            Assert.Greater(Largura(Art.Titulo(), Hud.MenorTexto, "Recife"), 0f, "gerador de texto sem fonte neste modo");
            foreach (string d in Textos.Destino) Assert.LessOrEqual(Largura(Art.Titulo(), 44, d), Hud.LargDestino, d);
            Assert.LessOrEqual(Largura(Art.Titulo(), 36, "PARTIU · 99 ficaram"), Hud.LargSituacao);
            Assert.LessOrEqual(Largura(Art.Font(), Hud.MenorTexto, Textos.Historia[0]), Hud.LargNota);
        }
    }
}
