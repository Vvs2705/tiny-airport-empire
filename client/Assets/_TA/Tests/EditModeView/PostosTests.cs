using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TA.Tests
{
    /// <summary>A linha da segurança (Mundo.Parada): ninguém atravessa máquina, nem o passageiro nas paradas, nem quem sai até a porta, nem o agente.</summary>
    public class PostosTests
    {
        const int Segurancas = 4;
        static readonly Vector2 Porta = new Vector2(6f, 4f);

        static void Livre(List<Vector2> rota, string nome)
        {
            for (int s = 1; s < rota.Count; s++)
                for (float t = 0f; t <= 1f; t += 0.02f)
                {
                    Vector2 p = Vector2.Lerp(rota[s - 1], rota[s], t);
                    for (int i = 0; i < Segurancas; i++)
                        foreach (Rect c in Mundo.CaixasLinha(i)) Assert.IsFalse(c.Contains(p), nome + " atravessa a linha " + i + " em " + p);
                }
        }

        [Test]
        public void Passageiro_FazAsParadasESaiPelaPorta_SemTocarNasMaquinas()
        {
            for (int i = 0; i < Segurancas; i++)
            {
                var rota = new List<Vector2>();
                for (int k = 0; k < 4; k++) rota.Add(Mundo.Parada(i, k));
                Vector2 fim = rota[3];
                rota.Add(new Vector2(Porta.x, fim.y));
                rota.Add(Porta);
                Livre(rota, "linha " + i);
            }
        }

        [Test]
        public void Passageiro_SaiDoGuichePeloVaoDaPorta_SemTocarNasBancadas()
        {
            Vector2 porta = new Vector2(4f, 2f);
            for (int i = 0; i < Segurancas; i++)
            {
                Vector2 de = Mundo.PeBalanca(i);
                var rota = new List<Vector2> { de, new Vector2(de.x, porta.y), porta };
                for (int s = 1; s < rota.Count; s++)
                    for (float t = 0f; t <= 1f; t += 0.02f)
                        for (int k = 0; k < Segurancas; k++)
                            Assert.IsFalse(Mundo.CaixaBancada(k).Contains(Vector2.Lerp(rota[s - 1], rota[s], t)), "guiche " + i + " atravessa a bancada " + k);
            }
        }

        [Test]
        public void Agente_ChegaAoPostoPelaRaiaDaPessoa()
        {
            for (int i = 0; i < Segurancas; i++)
            {
                var rota = new List<Vector2>();
                Vector2 p = Mundo.Parada(i, 0) + new Vector2(0f, -0.4f);
                rota.Add(p);
                Mundo.CruzaLinha(rota, ref p, Mundo.Agente(i));
                rota.Add(Mundo.Agente(i));
                Assert.Greater(rota.Count, 3, "o agente cortou caminho na linha " + i);
                Livre(rota, "agente " + i);
            }
        }
    }
}
