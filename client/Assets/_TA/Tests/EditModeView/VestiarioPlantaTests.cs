using System.Collections.Generic;
using NUnit.Framework;
using TA.Core;
using UnityEngine;

namespace TA.Tests
{
    /// <summary>Frente E: de qualquer sala ao armario do vestiario (Mundo.Vestiario.cs) sem atravessar parede nem passar pela alimentacao ou pelo
    /// despacho, que estao em obra no comeco.</summary>
    public class VestiarioPlantaTests
    {
        static bool Dentro(Vector2 p, int e) => Mundo.SalaEm(p) == e && Mathf.Repeat(p.x, Mundo.Lado) > 0.31f && Mathf.Repeat(p.x, Mundo.Lado) < 3.69f
                                                 && Mathf.Repeat(p.y, Mundo.Lado) > 0.31f && Mathf.Repeat(p.y, Mundo.Lado) < 3.69f;

        [Test]
        public void TodaSala_ChegaAoArmario_PeloCorredor()
        {
            for (int de = 0; de < 8; de++)
                for (int k = 0; k < Sim.MaxEquipe; k++)
                {
                    var r = new List<Vector2>();
                    Vector2 p = Mundo.Fila(de, 0);
                    Mundo.RotaVestiario(r, p, de, k);
                    string q = de + " -> armario " + k;
                    Assert.AreEqual(Mundo.VestLugar(k), r[r.Count - 1], q);
                    Assert.IsTrue(Mundo.NoVestiario(Mundo.VestLugar(k)), q);
                    Assert.IsTrue(r.Exists(v => Vector2.Distance(v, new Vector2(12f, 2f)) < 1e-3f), q + ": entra pela porta");
                    foreach (Vector2 v in r)
                    {
                        Vector2 d = v - p;
                        Assert.IsTrue(Mathf.Abs(d.x) < 1e-3f || Mathf.Abs(d.y) < 1e-3f, q + ": diagonal de " + p + " a " + v);
                        if (de != (int)Estacao.Alimentacao) Assert.IsFalse(Dentro(v, (int)Estacao.Alimentacao), q + ": pela alimentacao em " + v);
                        if (de != (int)Estacao.Despacho && de != (int)Estacao.Manutencao) Assert.IsFalse(Dentro(v, (int)Estacao.Despacho), q + ": pelo despacho em " + v);
                        p = v;
                    }
                }
        }
    }
}
