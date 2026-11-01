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
        public void Malas_EsteiraDoPostoPassaLongeDosBalcoesESegueAteOTerminal()
        {
            var r = new List<Vector2>();
            for (int i = 0; i < 4; i++)
            {
                Mundo.RotaMala(r, i);
                Vector2 balanca = Mundo.PontoBalanca(i);
                Assert.IsTrue(Mathf.Approximately(balanca.x, r[0].x), i + ": esteira do posto em diagonal");
                for (int j = 0; j < 4; j++) Assert.Greater(Mathf.Abs(balanca.x - Mundo.BalcaoMala(j).x), 0.25f, i + " passa pelo balcao " + j);
                Assert.IsTrue(Mathf.Approximately(r[0].y, r[1].y), i + ": esteira da parede torta");
                Assert.IsTrue(Mathf.Approximately(r[1].x, r[2].x), i + ": saida torta");
                Assert.Greater(r[2].y, 10f, i + ": nao chegou ao terminal");
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
                    if (q.y < 6f) Assert.IsTrue(Mathf.Approximately(q.x, lugar.x) || Mathf.Approximately(q.x, de.x), de + ": cortou as cadeiras em " + q);
                    ant = q;
                }
            }
        }

        [Test]
        public void Contorna_DaVoltaNaPecaLigada_SemDiagonal()
        {
            var go = new GameObject("peca");
            try
            {
                Mundo.PecaDeTeste(0, go, new Vector2(2f, 2f), 0.05f, 1.2f);   // de x 0,8 a 3,2, no meio do caminho
                var r = new List<Vector2>();
                Vector2 de = new Vector2(2f, 1f), q = new Vector2(2f, 3f), p = de;
                Mundo.Contorna(r, ref p, q, true);
                Assert.AreEqual(q, p);
                Assert.LessOrEqual(r.Count, 6, "curvas demais");
                Vector2 ant = de;
                foreach (Vector2 c in r)
                {
                    Assert.IsTrue(Mathf.Abs(ant.x - c.x) < 0.05f || Mathf.Abs(ant.y - c.y) < 0.05f, "em diagonal de " + ant + " a " + c);
                    bool cruza = (ant.y - 2f) * (c.y - 2f) < 0f;
                    Assert.IsFalse(cruza && Mathf.Min(ant.x, c.x) < 3.28f && Mathf.Max(ant.x, c.x) > 0.72f, "atravessou de " + ant + " a " + c);
                    ant = c;
                }
                go.SetActive(false);
                r.Clear();
                p = de;
                Mundo.Contorna(r, ref p, q, true);
                Assert.AreEqual(1, r.Count, "peca desligada nao barra");
            }
            finally
            {
                Mundo.PecaDeTeste(0, null, Vector2.zero, 0f, 0f);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Bagagem_FicaEntreOCheckInEASeguranca()
        {
            var r = new List<Vector2>();
            Mundo.PelasPortas(4, 1, r);   // a volta da bagagem passa pelo check-in
            CollectionAssert.AreEqual(new[] { new Vector2(2f, 4f), new Vector2(4f, 2f) }, r);
        }

        [Test]
        public void Fila60_CabeNoPisoDaSala_SemEncostarNaParede()
        {
            const float folga = Mundo.Corredor * 0.5f + 0.1f;   // meia pessoa da parede
            for (int e = 0; e < 8; e++)
                for (int k = 0; k < 60; k++)
                {
                    Vector2 p = Mundo.Fila(e, k);
                    Assert.AreEqual(e, Mundo.SalaEm(p), e + " lugar " + k);
                    float x = Mathf.Repeat(p.x, Mundo.Lado), z = Mathf.Repeat(p.y, Mundo.Lado);
                    Assert.That(x, Is.InRange(folga, Mundo.Lado - folga), e + " lugar " + k);
                    Assert.That(z, Is.InRange(folga, Mundo.Lado - folga), e + " lugar " + k);
                }
        }
    }
}
