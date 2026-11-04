using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TA.Core;
using UnityEngine;

namespace TA.Tests
{
    /// <summary>A tela do passe: o nivel que o nucleo pagou aparece para coletar e o Coletar so marca como visto; a trilha premium fica trancada
    /// sem a posse e sem preco; todo texto cabe sem descer do piso de 32 px.</summary>
    public class PasseTests
    {
        static float Largura(Font f, string t)
        {
            var s = new TextGenerationSettings { font = f, fontSize = Hud.MenorTexto, fontStyle = FontStyle.Normal, scaleFactor = 1f, lineSpacing = 1f, richText = false,
                generationExtents = new Vector2(4000f, 200f), horizontalOverflow = HorizontalWrapMode.Overflow, verticalOverflow = VerticalWrapMode.Overflow };
            return new TextGenerator().GetPreferredWidth(t, s);
        }

        static Sim Ligado()
        {
            var s = new Sim(new Tuning(), 1);
            s.Tick(Sim.Step);
            Assert.IsNotNull(s.Temporada, "o jogo novo abre a temporada 1");
            return s;
        }

        [Test]
        public void Resgate_NivelPagoApareceParaColetarEOColetarNaoPagaDeNovo()
        {
            Sim s = Ligado();
            Nivel[] g = s.Temporada.Gratis;
            Assert.AreEqual(Premio.Cash, g[0].Premio, "a conta abaixo usa o Cash do nivel 1");
            long cash = s.Cash;
            s.Events.Clear();
            s.PontosPasse = 2 * s.T.PassePontosNivel + 40;
            s.Tick(Sim.Step);
            Assert.AreEqual(2, s.NivelPasse);
            Assert.GreaterOrEqual(s.Cash, cash + g[0].Valor, "o nucleo paga ao fechar o nivel, antes de qualquer toque");
            var aColetar = new HashSet<int>();
            foreach (Evento e in s.Events) if (e.Tipo == Ev.PasseNivel) aColetar.Add(e.B);   // o que o Game.Trata passa ao Hud.PasseNivel
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, aColetar);
            Assert.AreEqual(Hud.EstadoNivel.Coletar, Hud.EstadoDoNivel(s, 1, aColetar));
            Assert.AreEqual(Hud.EstadoNivel.Proximo, Hud.EstadoDoNivel(s, 3, aColetar));
            Assert.AreEqual(Hud.EstadoNivel.Trancado, Hud.EstadoDoNivel(s, 4, aColetar));

            long antes = s.Cash;
            aColetar.Remove(1);   // o botao Coletar do nivel 1
            Assert.AreEqual(Hud.EstadoNivel.Recebido, Hud.EstadoDoNivel(s, 1, aColetar));
            Assert.AreEqual(Hud.EstadoNivel.Coletar, Hud.EstadoDoNivel(s, 2, aColetar));
            Assert.AreEqual(antes, s.Cash, "coletar nao paga de novo");
            Assert.AreEqual(Hud.EstadoNivel.Recebido, Hud.EstadoDoNivel(s, 2, new HashSet<int>()), "reaberto o jogo: ja recebido, nada se perde");
        }

        [Test]
        public void Premium_TrancadaSemPosseSemPrecoELiberadaComAPosse()
        {
            Sim s = Ligado();
            Assert.IsFalse(Hud.PremiumAberto(s));
            Assert.AreEqual(Textos.ComALoja, Hud.PremiumNoNivel(Hud.PremiumAberto(s)));
            foreach (string t in new[] { Textos.PremiumTitulo, Textos.PremiumTrancada, Textos.PremiumNaoPrecisa, Textos.ComALoja, Textos.Premium })
                Assert.IsFalse(Regex.IsMatch(t, @"\d|R\$|US\$"), "a trilha premium nao tem preco: " + t);
            Assert.AreEqual(Credito.Creditado, s.Creditar("tx-teste-passe", s.Temporada.Produto), "o save de teste com a posse");
            Assert.IsTrue(Hud.PremiumAberto(s));
            Assert.AreEqual(Textos.Liberada, Hud.PremiumNoNivel(Hud.PremiumAberto(s)));
        }

        [Test]
        public void Textos_CabemNoCartaoENoAtalho()
        {
            Assert.Greater(Largura(Art.Font(), "Passe"), 0f, "gerador de texto sem fonte neste modo");
            var t = new Tuning();
            foreach (Temporada temp in Tabelas.Temporadas)
            {
                foreach (Nivel n in temp.Gratis)
                {
                    string p = Textos.PremioNome(n);
                    if (n.Premio == Premio.Cosmetico) Assert.AreNotEqual(n.Id, p, "cosmetico sem nome na tela");
                    Assert.LessOrEqual(Largura(Art.Titulo(), p), Hud.LargPremio, p);
                }
                int topo = temp.Gratis.Length * t.PassePontosNivel;
                foreach (string e in new[] { Textos.Recebido, Textos.FaltamPontos(t.PassePontosNivel - 1), Textos.AbreCom(topo) })
                    Assert.LessOrEqual(Largura(Art.Font(), e), Hud.LargPremio, e);
                foreach (string r in Textos.RegrasPasse(t, temp.Dias)) Assert.LessOrEqual(Largura(Art.Font(), r), 1.8f * Hud.LargRegra, r);   // 2 linhas
                Assert.LessOrEqual(Largura(Art.Font(), Textos.NivelCurto(temp.Gratis.Length)), Hud.LargNivelAtalho);
            }
            foreach (bool aberto in new[] { false, true }) Assert.LessOrEqual(Largura(Art.Font(), Hud.PremiumNoNivel(aberto)), Hud.LargCanhoto);
        }
    }
}
