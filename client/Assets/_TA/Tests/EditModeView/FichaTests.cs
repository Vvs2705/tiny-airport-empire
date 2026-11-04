using NUnit.Framework;
using TA.Core;
using UnityEngine;

namespace TA.Tests
{
    /// <summary>A ficha ao tocar: o humor diz o motivo que o nucleo conhece, a ficha some com quem saiu da tela e toda linha cabe no piso de 32 px.</summary>
    public class FichaTests
    {
        static float Largura(Font f, string t)
        {
            var s = new TextGenerationSettings { font = f, fontSize = Hud.MenorTexto, fontStyle = FontStyle.Normal, scaleFactor = 1f, lineSpacing = 1f, richText = false,
                generationExtents = new Vector2(4000f, 200f), horizontalOverflow = HorizontalWrapMode.Overflow, verticalOverflow = VerticalWrapMode.Overflow };
            return new TextGenerator().GetPreferredWidth(t, s);
        }

        static Sim ComFila()
        {
            var s = new Sim(new Tuning(), 1);
            for (int i = 0; i < 40 * Sim.Hz && s.St(Estacao.CheckIn).Fila.Count < 3; i++) s.Tick(Sim.Step);   // sem equipe no inicio: a fila cresce
            Assert.Greater(s.St(Estacao.CheckIn).Fila.Count, 0, "sem fila no check-in");
            return s;
        }

        [Test]
        public void Humor_DizOMotivoDoNucleo()
        {
            Sim s = ComFila();
            Station ck = s.St(Estacao.CheckIn), portao = s.St(Estacao.Portao);
            Voo v = s.Voos[0];
            Assert.AreEqual(Humor.Furioso, Hud.HumorDe(null, v, 0f, true, false), "voo perdido");
            Assert.AreEqual(Humor.Contente, Hud.HumorDe(null, v, 0f, true, true));
            ck.Estado = Estado.Normal; ck.VooEmRisco = -1; v.Atrasado = false;
            Assert.AreEqual(Humor.Tranquilo, Hud.HumorDe(ck, v, 1f, false, false));
            Assert.AreEqual(Humor.Impaciente, Hud.HumorDe(ck, v, Hud.EsperaLonga + 1f, false, false), "espera longa");
            Assert.AreEqual(Humor.Tranquilo, Hud.HumorDe(portao, v, Hud.EsperaLonga + 1f, false, false), "esperar o embarque abrir nao e espera longa");
            ck.VooEmRisco = v.Id; ck.Estado = Estado.Atencao;
            Assert.AreEqual(Humor.Preocupado, Hud.HumorDe(ck, v, 1f, false, false));
            v.Atrasado = true;
            Assert.AreEqual(Humor.Chateado, Hud.HumorDe(ck, v, 1f, false, false), "voo atrasado");
            ck.Estado = Estado.Critico;
            Assert.AreEqual(Humor.Irritado, Hud.HumorDe(ck, v, 1f, false, false), "no ritmo da fila, perde o voo");
        }

        [Test]
        public void Ficha_PassageiroNaFilaEQuemSumiu()
        {
            Sim s = ComFila();
            Pax p = s.St(Estacao.CheckIn).Fila[0];
            FichaDados d = Hud.FichaPax(s, p.Id, true, false);
            StringAssert.Contains(Textos.Voo(s.Voos[p.Voo].Id), d.Nome);
            StringAssert.StartsWith(Textos.PaxNaFila[0], d.Linha);
            StringAssert.Contains(Textos.Tempo((s.Voos[p.Voo].FechaPasso - s.PassoNaOnda) * Sim.Step), d.Info, "quanto falta para o portao fechar");
            Assert.IsNull(Hud.FichaPax(s, 999999, false, false), "fora do nucleo e da tela, a ficha fecha");
            Assert.AreEqual(Textos.IndoEmbora, Hud.FichaPax(s, 999999, true, false).Linha, "saiu sem embarcar");
            s.Cash += 10000;
            Assert.IsTrue(s.Hire(), "contratar a Ana");
            Assert.IsTrue(Hud.FichaFunc(s, 0, false).Mover, "o funcionario tem o Mover");
            foreach (Estacao e in System.Enum.GetValues(typeof(Estacao))) Assert.IsNotEmpty(Hud.FichaEstacao(s, e).Nota, e.ToString());
        }

        [Test]
        public void Ficha_TodaLinhaCabeNoCartao()
        {
            Font corpo = Art.Font(), nome = Art.Titulo();
            Assert.Greater(Largura(corpo, "Recife"), 0f, "gerador de texto sem fonte neste modo");
            var linhas = new System.Collections.Generic.List<string>(Textos.PaxNaFila) { Textos.NaFilaDoEmbarque + Textos.ComHistoria, Textos.IndoEmbora, Textos.PortaoFechou,
                Textos.NoAeroporto(599f, 599f, true), Textos.GargaloRisco(199), Textos.GargaloFolga(199), Textos.GargaloFalha, Textos.GargaloToque, Textos.GargaloFechada,
                Textos.Balcoes(4, 12.5f) + " · " + Textos.NaFila(60) + " · " + Textos.NaEstacao(6), Textos.MelhoriaNome(Melhoria.Velocidade, 5, 5) + " · " + Textos.MelhoriaNome(Melhoria.Capacidade, 2, 2),
                Textos.FaltamEmbarques(10000), Textos.VoltaSala, Textos.Trechos(3, 3), Textos.EscalaNaTela };
            linhas.AddRange(Textos.PaxAtendido);
            for (int h = 0; h < Textos.Humor.Length; h++) linhas.Add(Textos.HumorLinha(h));
            foreach (string l in linhas) Assert.LessOrEqual(Largura(corpo, l), Hud.LargFicha, l);
            foreach (string d in Textos.Destino) Assert.LessOrEqual(Largura(nome, Textos.Voo(199) + " · " + d), Hud.LargFicha, d);
            for (int p = 0; p < Textos.Perfil.Length; p++)   // com o Mover a direita
            {
                Assert.LessOrEqual(Largura(nome, Textos.Funcionario[p] + " · " + Textos.Perfil[p]), Hud.LargFichaMover, Textos.Perfil[p]);
                Assert.LessOrEqual(Largura(corpo, Textos.PerfilInfo(p)), Hud.LargFichaMover, Textos.PerfilInfo(p));
            }
            foreach (string l in new[] { Textos.AtendendoUm, Textos.ChamandoProximo, Textos.EsperandoPax, Textos.NaChecagem, Textos.ProntoChecagem, Textos.NoBonus, Textos.Preparo(1f, 599f) })
                Assert.LessOrEqual(Largura(corpo, l), Hud.LargFichaMover, l);
            foreach (string fase in Textos.FaseTransf) Assert.LessOrEqual(Largura(corpo, Textos.Funcionario[5] + " · " + fase), Hud.LargFichaMover, fase);
            for (int e = 0; e < Textos.Estacao.Length; e++)
                foreach (bool andando in new[] { false, true }) Assert.LessOrEqual(Largura(corpo, Textos.EmEstacao(e, andando)), Hud.LargFichaMover, Textos.EmEstacao(e, andando));
        }
    }
}
