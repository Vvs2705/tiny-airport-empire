using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TA.Tests
{
    /// <summary>Saguao de chegada (Mundo.Chegada, frente H): ninguem atravessa a esteira nem a parede, so anda nos eixos do mundo e cada lugar em
    /// volta da esteira e de uma pessoa so.</summary>
    public class ChegadaViewTests
    {
        const int N = 10;
        const float Corpo = 0.1f, Tolerancia = 0.02f, Parede = 0.04f;

        static List<Vector2> Chega(int vaga)
        {
            var r = new List<Vector2>();
            Mundo.RotaChegada(r, vaga, N);
            return r;
        }

        static List<Vector2> Sai(int vaga)
        {
            var r = new List<Vector2> { Mundo.LugarEsteira(vaga, N) };
            Mundo.RotaSaida(r, (float)(vaga % N) / N);
            return r;
        }

        /// <summary>Pontos a cada 2 cm da rota.</summary>
        static IEnumerable<Vector2> Amostra(List<Vector2> r)
        {
            for (int i = 1; i < r.Count; i++)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(r[i - 1], r[i]) / 0.02f));
                for (int j = 0; j <= n; j++) yield return Vector2.Lerp(r[i - 1], r[i], j / (float)n);
            }
        }

        static void Confere(List<Vector2> r, string quem)
        {
            float mx = Mundo.VoltaX + Mundo.BordaVolta + Corpo + Tolerancia, mz = Mundo.VoltaZ + Mundo.BordaVolta + Corpo + Tolerancia;
            Vector2 min = Mundo.SaguaoMin + Vector2.one * 0.3f, max = Mundo.SaguaoMin + new Vector2(Mundo.SaguaoL, Mundo.SaguaoP) - Vector2.one * 0.3f;
            for (int i = 1; i < r.Count; i++)
            {
                Vector2 d = r[i] - r[i - 1];
                Assert.IsTrue(Mathf.Abs(d.x) < 1e-4f || Mathf.Abs(d.y) < 1e-4f, quem + ": perna na diagonal de " + r[i - 1] + " a " + r[i]);
            }
            foreach (Vector2 p in Amostra(r))
            {
                Vector2 e = p - Mundo.EsteiraC;
                Assert.IsFalse(Mathf.Abs(e.x) < mx && Mathf.Abs(e.y) < mz, quem + ": encostou na esteira em " + p);
                bool dentro = p.x > min.x - 0.3f && p.x < max.x + 0.3f && p.y > min.y - 0.3f && p.y < max.y + 0.3f;
                if (!dentro) continue;
                // dentro da sala (com as paredes): longe da face da parede, salvo no vao da porta
                bool portaN = Mathf.Abs(p.x - Mundo.PortaNorte.x) < 0.225f - Corpo - Tolerancia, portaO = Mathf.Abs(p.y - Mundo.PortaOeste.y) < 0.225f - Corpo - Tolerancia;
                float folga = Corpo + Parede + Tolerancia;
                if (!portaO) Assert.Greater(p.x, min.x + folga, quem + ": parede oeste em " + p);
                Assert.Less(p.x, max.x - folga, quem + ": parede leste em " + p);
                Assert.Greater(p.y, min.y + folga, quem + ": parede sul em " + p);
                if (!portaN) Assert.Less(p.y, max.y - folga, quem + ": parede norte em " + p);
            }
        }

        [Test]
        public void QuemChega_PelaPortaNorte_AteOLugar_SemEncostar()
        {
            for (int v = -1; v < 3 * N; v++)
            {
                List<Vector2> r = Chega(v);
                Assert.AreEqual(Mundo.BocaCorredor, r[0], "comeca no passeio norte");
                Assert.AreEqual(v < 0 ? Mundo.SaidaRua : Mundo.LugarEsteira(v, N), r[r.Count - 1]);
                Confere(r, "chega " + v);
            }
        }

        [Test]
        public void QuemPegou_SaiPelaPortaOeste_SemEncostar()
        {
            for (int v = 0; v < 3 * N; v++)
            {
                List<Vector2> r = Sai(v);
                Assert.AreEqual(Mundo.SaidaRua, r[r.Count - 1], "termina na calcada");
                CollectionAssert.Contains(r, Mundo.PortaOeste, "pela porta oeste");
                Confere(r, "sai " + v);
            }
        }

        [Test]
        public void CadaLugar_EmVoltaDaEsteira_DeUmaPessoaSo()
        {
            var lugares = new List<Vector2>();
            for (int v = 0; v < 3 * N; v++)
            {
                Vector2 p = Mundo.LugarEsteira(v, N);
                foreach (Vector2 q in lugares) Assert.Greater(Vector2.Distance(p, q), 0.2f, "dois lugares colados: " + p);
                lugares.Add(p);
            }
        }
    }
}
