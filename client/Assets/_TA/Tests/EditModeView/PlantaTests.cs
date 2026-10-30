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
        public void Bagagem_FicaEntreOCheckInEASeguranca()
        {
            var r = new List<Vector2>();
            Mundo.PelasPortas(4, 1, r);   // a volta da bagagem passa pelo check-in
            CollectionAssert.AreEqual(new[] { new Vector2(2f, 4f), new Vector2(4f, 2f) }, r);
        }
    }
}
