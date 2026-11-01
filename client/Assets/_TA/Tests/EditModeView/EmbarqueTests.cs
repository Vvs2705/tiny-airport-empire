using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TA.Tests
{
    /// <summary>Embarque (Mundo.Embarque): a pessoa sobe sem degrau, some na boca do tunel, e a ponte encosta na fuselagem sem atravessar.</summary>
    public class EmbarqueTests
    {
        const float Passo = 0.02f;

        /// <summary>A rota da sala da tripulacao ate a porta do aviao, amostrada a cada 2 cm.</summary>
        static List<Vector2> Pontos(int k)
        {
            Vector2 p = new Vector2(7.6f, 8.6f);
            var r = new List<Vector2> { p };
            Mundo.RotaAviao(r, ref p, k);
            var pts = new List<Vector2>();
            for (int i = 1; i < r.Count; i++)
            {
                int n = Mathf.CeilToInt(Vector2.Distance(r[i - 1], r[i]) / Passo);
                for (int j = 0; j <= n; j++) pts.Add(Vector2.Lerp(r[i - 1], r[i], n == 0 ? 1f : j / (float)n));
            }
            return pts;
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Sobe_SemDegrau_AteAPorta(int k)
        {
            float alt = 0f;
            foreach (Vector2 p in Pontos(k))
            {
                float a = Mundo.AlturaEmbarque(k, p);
                Assert.GreaterOrEqual(a, alt - 1e-3f, "desceu em " + p);
                Assert.Less(a - alt, 0.06f, "degrau em " + p);
                alt = a;
            }
            Assert.Greater(alt, 1.2f, "chegou baixo na porta");
        }

        [Test]
        public void NaPonte_SomeNaBoca_ENaoVoltaAteAPorta()
        {
            bool sumiu = false;
            foreach (Vector2 p in Pontos(0))
            {
                float a = Mundo.AlfaEmbarque(0, p);
                if (a <= 0f) sumiu = true;
                else if (sumiu) Assert.Fail("reapareceu no tunel em " + p);
            }
            Assert.IsTrue(sumiu);
        }

        [Test]
        public void NaEscada_AparecePorInteiro_AteAFuselagem()
        {
            List<Vector2> pts = Pontos(1);
            foreach (Vector2 p in pts)
                if (p.x < 21.25f) Assert.AreEqual(1f, Mundo.AlfaEmbarque(1, p), "sumiu antes da passarela em " + p);
            Assert.AreEqual(0f, Mundo.AlfaEmbarque(1, pts[pts.Count - 1]), "ficou visivel dentro do aviao");
        }

        [Test]
        public void Ponte_EncostaSemAtravessarAFuselagem()
        {
            // medidas no Blender: ponta fechada da ponte a 1,7 m do centro e 0,39 m de meia largura; regional no estande G em x 11,4, com 0,99 m
            // de meia largura na altura da ponta; a helice oeste comeca em z 13,2
            float ponta = Mundo.PonteEm.z + 1.7f * Mundo.PonteEscala.z, leste = Mundo.PonteEm.x + 0.39f * Mundo.PonteEscala.x;
            Assert.Less(ponta, 13.1f, "ponta na helice");
            Assert.LessOrEqual(leste, 11.4f - 0.99f, "ponte dentro da fuselagem");
            Assert.Greater(leste, 11.4f - 0.99f - 0.1f, "ponte longe da porta");
        }
    }
}
