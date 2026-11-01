using NUnit.Framework;
using TA.Core;
using UnityEngine;

namespace TA.Tests
{
    /// <summary>O quadro de partidas e o aviso de perigo: estado nunca so por cor, e o texto mais longo cabe sem descer do piso de 32 px.</summary>
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
                (new Voo(), new Aviao { NoPatio = true, Pane = true }, 0, Art.Alerta()),
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

        static Sim SimLigado()
        {
            var s = new Sim(new Tuning(), 1);
            s.Tick(Sim.Step);
            return s;
        }

        [Test]
        public void Aviso_TodoPerigoDizCausaOQueFazerEQuantoFalta()
        {
            Sim s = SimLigado();
            foreach (TipoIncidente t in System.Enum.GetValues(typeof(TipoIncidente)))
            {
                var i = new Incidente { Tipo = t, Estacao = Estacao.Seguranca, Inicio = s.Steps + 30 * Sim.Hz, Fim = s.Steps + 90 * Sim.Hz };
                AvisoPerigo p = Hud.DoIncidente(s, i);
                StringAssert.Contains("30", p.Titulo, t.ToString());
                Assert.LessOrEqual(Largura(Art.Titulo(), 44, p.Titulo), Hud.LargAviso, p.Titulo);
                string[] linhas = p.Texto.Split('\n');
                Assert.AreEqual(2, linhas.Length, p.Texto);
                foreach (string l in linhas) Assert.LessOrEqual(Largura(Art.Font(), Hud.MenorTexto, l), Hud.LargAviso, l);
                CollectionAssert.Contains(Sim.Opcoes(t), p.Acao, t.ToString());
                CollectionAssert.Contains(Sim.Opcoes(t), p.Acao2, t.ToString());
                if (s.ClimaDe(t) != null) Assert.AreEqual(0, p.Aviao, "clima leva ao aviao da vaga");
                else Assert.Less(p.Aviao, 0, "incidente leva a sala");
            }
        }

        [Test]
        public void Aviso_SomeAoResolverEMudaQuandoComeca()
        {
            Sim s = SimLigado();
            var i = new Incidente { Tipo = TipoIncidente.Temporal, Inicio = s.Steps + 30 * Sim.Hz, Fim = s.Steps + 90 * Sim.Hz };
            Assert.AreEqual(Opcao.Adiantar, Hud.DoIncidente(s, i).Acao);
            Assert.AreEqual(Opcao.Atrasar, Hud.DoIncidente(s, i).Acao2);
            i.Inicio = s.Steps;
            Assert.AreEqual(Opcao.Atrasar, Hud.DoIncidente(s, i).Acao, "depois de comecar, nao da mais para adiantar");
            i.Resolvido = true;
            Assert.AreEqual(Opcao.Segurar, Hud.DoIncidente(s, i).Acao, "o clima segue na tela, sem botao");
            i.Tipo = TipoIncidente.Conexao;
            Assert.IsNull(Hud.DoIncidente(s, i), "incidente resolvido some");
        }

        [Test]
        public void Aviso_FalhaDeAviaoLevaAoAviaoComASaidaCerta()
        {
            Sim s = SimLigado();
            Assert.IsNull(Hud.AvisoDaVez(s), "sem perigo, sem aviso");
            Aviao a = s.Avioes[1];
            a.NoPatio = true; a.Pane = true;
            AvisoPerigo p = Hud.AvisoDaVez(s);
            Assert.AreEqual(Opcao.Reparar, p.Acao);
            Assert.AreEqual(1, p.Aviao);
            a.Check = 10;
            Assert.AreEqual(Opcao.Segurar, Hud.AvisoDaVez(s).Acao, "o mecanico ja esta nele");
            a.Inspecao = true;
            Assert.AreEqual(Opcao.Reparar, Hud.AvisoDaVez(s).Acao, "a inspecao de graca nao tira a saida paga");
            a.Pane = false; a.Check = 0; a.SaiEm = s.Steps + 40 * Sim.Hz;
            p = Hud.AvisoDaVez(s);
            Assert.AreEqual(Opcao.Segurar, p.Acao, "rejeicao: e so esperar");
            StringAssert.Contains("40", p.Texto);
            a.SaiEm = 0; a.NoPatio = false; a.Retorno = s.Steps; a.Chega = s.Steps + 25 * Sim.Hz;
            StringAssert.Contains("25", Hud.AvisoDaVez(s).Titulo);
        }
    }
}
