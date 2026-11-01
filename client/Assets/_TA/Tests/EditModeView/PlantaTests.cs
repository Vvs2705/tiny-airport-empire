using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TA.Tests
{
    /// <summary>A planta das 8 salas (Mundo.PelasPortas): ninguem atravessa parede, de qualquer sala para qualquer sala.</summary>
    public class PlantaTests
    {
        [Test]
        public void TodaSala_ChegaEmTodaSala_PelasPortas()
        {
            for (int de = 0; de < 8; de++)
                for (int para = 0; para < 8; para++)
                {
                    var r = new List<Vector2>();
                    Mundo.PelasPortas(de, para, r);
                    if (de == para) { Assert.AreEqual(0, r.Count); continue; }
                    Assert.Greater(r.Count, 0, de + " -> " + para);
                    Assert.LessOrEqual(r.Count, 6, de + " -> " + para);
                }
        }

        [Test]
        public void Portao_ChegaAoLugarPeloCorredorDaFileira()
        {
            var lugar = new Vector2(10.58f, 4.5f);   // no meio das fileiras do bloco leste
            foreach (Vector2 de in new[] { new Vector2(10f, 4.1f), new Vector2(8.1f, 6f), new Vector2(9.2f, 6.6f) })   // porta sul, porta oeste, podio
            {
                var r = new List<Vector2>();
                Vector2 p = de;
                Mundo.RotaSala(r, ref p, lugar);
                Assert.AreEqual(lugar, p);
                Vector2 ant = de;
                foreach (Vector2 q in r)
                {
                    Assert.IsTrue(Mathf.Approximately(ant.x, q.x) || Mathf.Approximately(ant.y, q.y), de + ": em diagonal");
                    if (q.y < 5.6f) Assert.IsTrue(Mathf.Approximately(q.x, lugar.x) || Mathf.Approximately(q.x, de.x), de + ": cortou as cadeiras em " + q);
                    ant = q;
                }
            }
        }

        [Test]
        public void Bagagem_FicaEntreOCheckInEASeguranca()
        {
            var r = new List<Vector2>();
            Mundo.PelasPortas(4, 1, r);   // a volta da bagagem passa pelo check-in
            CollectionAssert.AreEqual(new[] { new Vector2(2f, 4f), new Vector2(4f, 2f) }, r);
        }
    }
}
