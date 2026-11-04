using System.Collections.Generic;
using NUnit.Framework;
using TA.Core;
using UnityEngine;

namespace TA.Tests
{
    /// <summary>
    /// A tela de rotas (Hud.Rotas.cs): no mapa, nenhum nome cobre outro, um ponto, o aeroporto ou o titulo, e tudo cabe no painel; toda linha
    /// do cartao cabe no piso de 32 px e diz distancia, frequencia, receita e risco; o botao de cada rota e da viagem segue o estado do nucleo.
    /// </summary>
    public class RotasTests
    {
        static float Largura(Font f, int tam, string t)
        {
            var s = new TextGenerationSettings { font = f, fontSize = tam, fontStyle = FontStyle.Normal, scaleFactor = 1f, lineSpacing = 1f, richText = false,
                generationExtents = new Vector2(4000f, 200f), horizontalOverflow = HorizontalWrapMode.Overflow, verticalOverflow = VerticalWrapMode.Overflow };
            return new TextGenerator().GetPreferredWidth(t, s);
        }

        static void Anda(Sim s, float seg) { for (float t = 0f; t < seg; t += Sim.MaxFrame) s.Tick(Sim.MaxFrame); }

        [Test]
        public void Mapa_NomesCabemNoPainelSemCobrirNada()
        {
            var t = new Tuning();
            var painel = new Rect(-Hud.LargMapa / 2f, -Hud.AltMapa / 2f, Hud.LargMapa, Hud.AltMapa);
            for (int a = 0; a < t.Aeroportos.Length; a++)
            {
                var ocupado = new List<(Rect r, string quem)> { (new Rect(-70f, -70f, 140f, 140f), "aeroporto") };
                foreach (Rect q in Hud.ReservaMapa) ocupado.Add((q, "titulo, estado ou legenda"));
                for (int r = 0; r < t.Rotas.Length; r++)
                {
                    if (t.Rotas[r].Aeroporto != a) continue;
                    Vector2 p = Hud.NoMapa(t.Rotas, t.Aeroportos, r);
                    string nome = Textos.Destino[r];
                    float w = Largura(Art.Titulo(), Hud.MenorTexto, nome);
                    Assert.LessOrEqual(w, Hud.LargRotuloMapa, nome);
                    Rect rotulo = Hud.RotuloNoMapa(p, w), ponto = new Rect(p.x - 29f, p.y - 29f, 58f, 58f);
                    foreach (Rect q in new[] { rotulo, ponto })
                    {
                        Assert.IsTrue(painel.Contains(q.min) && painel.Contains(q.max), nome + " sai do mapa: " + q);
                        foreach (var o in ocupado) Assert.IsFalse(q.Overlaps(o.r), nome + " cobre " + o.quem);
                    }
                    ocupado.Add((rotulo, nome)); ocupado.Add((ponto, nome));
                }
            }
            Vector2 manaus = Hud.NoMapa(t.Rotas, t.Aeroportos, 10), guarulhos = Hud.NoMapa(t.Rotas, t.Aeroportos, 8);
            Assert.Greater(manaus.magnitude, guarulhos.magnitude, "trecho mais longo, ponto mais longe");
            Assert.Less(manaus.x, 0f, "Manaus fica a oeste");
        }

        [Test]
        public void Cartao_TodaLinhaDizOQueARotaTrazECabe()
        {
            var s = new Sim(new Tuning(), 1);
            Font corpo = Art.Font();
            for (int r = 0; r < s.T.Rotas.Length; r++)
            {
                Rota x = s.T.Rotas[r];
                string linha = Textos.RotaLinha(x, s.TrechoSeg(r));
                string[] info = Textos.RotaInfo(x, s.T.RiscoChance).Split('\n');
                StringAssert.Contains(Textos.Tempo(s.TrechoSeg(r)), linha, x.Id + ": distancia");
                StringAssert.Contains(x.Cada > 1 ? x.Cada.ToString() : "toda", linha, x.Id + ": frequencia");
                StringAssert.Contains(Textos.Fator(x.Receita), info[0], x.Id + ": receita");
                if (x.Risco > 0) StringAssert.Contains(Textos.Pct(x.Risco * s.T.RiscoChance), info[0], x.Id + ": risco");
                Assert.AreEqual(2, info.Length, x.Id);
                foreach (string l in new[] { linha, info[0], info[1] }) Assert.LessOrEqual(Largura(corpo, Hud.MenorTexto, l), Hud.LargTextoRota, x.Id + ": " + l);
            }
            foreach (string l in Textos.ViagemInfo.Split('\n')) Assert.LessOrEqual(Largura(corpo, Hud.MenorTexto, l), Hud.LargTextoRota, l);
            for (int a = 0; a < s.T.Aeroportos.Length; a++)
            {
                Assert.LessOrEqual(Largura(Art.Titulo(), Hud.MenorTexto, Textos.AeroportoNome[a]), Hud.LargAeroAtalho, Textos.AeroportoNome[a]);
                Assert.LessOrEqual(Largura(corpo, Hud.MenorTexto, Textos.ViagemMarcada(a)), Hud.LargTextoRota, Textos.ViagemMarcada(a));
            }
        }

        [Test]
        public void Estado_SegueONucleo_DoMarcoAViagem()
        {
            const int Curitiba = 2, Salvador = 4;
            var s = new Sim(new Tuning { RegraCampanha = false, RegraMissoes = false }, 1) { Cash = 5000 };
            Anda(s, 1f);
            Assert.AreEqual((Hud.EstadoRota.NaVaga, Hud.EstadoRota.Programar), (Hud.EstadoDaRota(s, 0, 0), Hud.EstadoDaRota(s, 0, 1)), "a vaga 2 voa Ribeirao");
            Assert.AreEqual((Hud.EstadoRota.Terminal, Hud.EstadoViagem.Aqui, Hud.EstadoViagem.Terminal), (Hud.EstadoDaRota(s, Curitiba, 0), Hud.EstadoDaViagem(s, 0), Hud.EstadoDaViagem(s, 1)));
            s.TotalEmbarcados = 600;   // marco 2
            Assert.AreEqual((Hud.EstadoRota.Comprar, Hud.EstadoRota.Viaje, Hud.EstadoViagem.Comprar), (Hud.EstadoDaRota(s, Curitiba, 0), Hud.EstadoDaRota(s, Salvador, 0), Hud.EstadoDaViagem(s, 1)));
            Assert.IsTrue(s.AbreRota(Curitiba));
            Assert.AreEqual(Hud.EstadoRota.Programar, Hud.EstadoDaRota(s, Curitiba, 0));
            Assert.IsTrue(s.Programa(0, Curitiba));
            Assert.AreEqual(Hud.EstadoRota.NaVaga, Hud.EstadoDaRota(s, Curitiba, 0));
            Assert.IsTrue(s.Viajar(1));
            Assert.AreEqual((Hud.EstadoViagem.Marcada, Hud.EstadoViagem.Espere), (Hud.EstadoDaViagem(s, 1), Hud.EstadoDaViagem(s, 2)));
            Anda(s, s.T.Onda);
            Assert.AreEqual(1, s.AeroportoAtual);
            Assert.AreEqual((Hud.EstadoViagem.Aqui, Hud.EstadoViagem.Voltar, Hud.EstadoRota.Aberta), (Hud.EstadoDaViagem(s, 1), Hud.EstadoDaViagem(s, 0), Hud.EstadoDaRota(s, Curitiba, 0)));
            Assert.AreEqual(Hud.EstadoRota.NaVaga, Hud.EstadoDaRota(s, Salvador, 0), "na chegada, as rotas sem preco entram nas vagas");
        }
    }
}
