using System.Collections.Generic;
using NUnit.Framework;
using TA.Core;
using UnityEngine;

namespace TA.Tests
{
    /// <summary>Frente F: o banheiro na planta (Mundo.Limpeza.cs) fora de qualquer sala, a fila e as cabines dentro dele, as manchas no piso da
    /// propria area e a volta ao portao pelos corredores, sem diagonal.</summary>
    public class LimpezaPlantaTests
    {
        [Test]
        public void Banheiro_ForaDasSalas_FilaECabinesDentro()
        {
            for (int k = 0; k < 30; k++)
            {
                Assert.IsTrue(Mundo.DentroBanheiro(Mundo.FilaBanheiro(k)), "lugar " + k + " da fila");
                Assert.AreEqual(-1, Mundo.SalaEm(Mundo.FilaBanheiro(k)), "a fila nao invade sala");
            }
            for (int i = 0; i < 4; i++) Assert.IsTrue(Mundo.DentroBanheiro(Mundo.Cabine(i)) && Mundo.SalaEm(Mundo.Cabine(i)) < 0, "cabine " + i);
            Assert.IsFalse(Mundo.DentroBanheiro(new Vector2(14f, 2f)), "o vestiario nao e' o banheiro");
            Assert.IsFalse(Mundo.DentroBanheiro(new Vector2(18f, 6f)), "nem a oficina");
        }

        [Test]
        public void Manchas_NoPisoDaPropriaArea()
        {
            for (int a = 0; a < Sim.Areas; a++)
                for (int k = 0; k < 5; k++)
                {
                    Vector2 m = Mundo.Mancha(a, k);
                    if (a == (int)Area.Banheiro) Assert.IsTrue(Mundo.DentroBanheiro(m), "mancha " + k + " do banheiro");
                    else Assert.AreEqual(a, Mundo.SalaEm(m), "mancha " + k + " da area " + a);
                }
        }

        [Test]
        public void Volta_DaCabineAoPortao_PelaPiaEPelosCorredores_SemDiagonal()
        {
            for (int i = 0; i < 4; i++)
            {
                var r = new List<Vector2>();
                Vector2 p = Mundo.Cabine(i);
                Mundo.RotaDoBanheiro(r, p, true);
                Assert.AreEqual((int)Estacao.Portao, Mundo.SalaEm(r[r.Count - 1]), "termina dentro do portao");
                Assert.IsTrue(r.Exists(v => Mundo.TrechoBanheiro(v) >= 0), "pelo corredor");
                foreach (Vector2 v in r)
                {
                    Vector2 d = v - p;
                    Assert.IsTrue(Mathf.Abs(d.x) < 1e-3f || Mathf.Abs(d.y) < 1e-3f, "sem diagonal: " + p + " -> " + v);
                    Vector2 meio = Vector2.Lerp(p, v, 0.5f);
                    int s = Mundo.SalaEm(meio);
                    bool dentro = Mathf.Repeat(meio.x, Mundo.Lado) > 0.31f && Mathf.Repeat(meio.x, Mundo.Lado) < 3.69f && Mathf.Repeat(meio.y, Mundo.Lado) > 0.31f && Mathf.Repeat(meio.y, Mundo.Lado) < 3.69f;
                    Assert.IsTrue(s < 0 || s == (int)Estacao.Portao || !dentro, "nao corta outra sala (o corredor fica na divisa): " + p + " -> " + v);
                    p = v;
                }
            }
        }
    }
}
