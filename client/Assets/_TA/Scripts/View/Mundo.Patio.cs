using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Patio vivo (GDD §5.13), so visual: nada aqui muda regra.
    ///  - 2 avioes, 1 por voo da onda (indice 0 = o regional, 1 = o jato), no ciclo pouso -> taxi -> estande -> pushback (com o
    ///    rebocador preso no nariz) -> taxi -> decolagem, em trajetos fixos (polilinha com curvas arredondadas, sem fisica).
    ///  - Estande G (ponte de embarque) e estande R (remoto). O voo novo pousa e vai para o G se estiver livre, senao para o R; quando o G
    ///    libera, o aviao do R vai para o G. VooCriado = o aviao chega; VooPartiu = o aviao sai.
    ///  - 6 veiculos: rebocador, caminhao de combustivel, carrinho de bagagem (volta do G), caminhao-escada, onibus e catering (volta
    ///    do R). Andam em volta do aviao estacionado e param quando o estande esta vazio ("ainda sem regra").
    ///  - Sombra de cada aviao e veiculo como elipse no chao (sem sombra dinamica, GDD §10.4).
    /// </summary>
    public sealed partial class Mundo
    {
        // pista ao norte (z = 23, pousa e decola para leste), taxiway em z = 19, estandes de nariz para o terminal (para -z)
        const float PistaZ = 23f, TaxiZ = 19f, EstandeZ = 13.8f;
        static readonly float[] EstandeX = { 11.4f, 21f };
        static readonly string[] AviaoMalha = { "aviao_regional", "aviao_narrow" };
        static readonly Vector2 ReboquePark = new Vector2(13.9f, 9.9f);

        /// <summary>Trecho de trajeto: pontos densos com o comprimento acumulado; velocidade de v0 a v1 ao longo dele; de re = o nariz
        /// aponta contra o movimento (pushback).</summary>
        sealed class Trecho
        {
            public readonly List<Vector3> P = new List<Vector3>();
            public readonly List<float> L = new List<float>();
            public float V0, V1;
            public bool Re, Reboque;
            public float Total => L.Count > 0 ? L[L.Count - 1] : 0f;
        }

        enum Fase { Fora, Chegando, NoPatio, Saindo }

        sealed class Aviao
        {
            public Transform T;
            public SpriteRenderer Sombra, Selo;
            public float Meio = 3.7f, Espera;
            public Fase Fase;
            public int Estande = -1, Voo = -1;
            public bool QuerChegar, QuerPartir;
            public readonly Queue<Trecho> Trechos = new Queue<Trecho>();
            public float S;
        }

        sealed class Veiculo
        {
            public Transform T;
            public SpriteRenderer Sombra;
            public int Volta = -1;   // -1 = o rebocador
            public float S, Vel;
            public Trecho Rota;
        }

        readonly Aviao[] _avioes = { new Aviao(), new Aviao() };
        readonly List<Veiculo> _veiculos = new List<Veiculo>();
        Trecho[] _voltas;

        // ------------------------------------------------------------------ construcao

        void Patio3D()
        {
            Chao("Patio", new Vector3(15.5f, -0.06f, 13.5f), new Vector2(19.5f, 8.8f), Asfalto);
            Chao("Taxiway", new Vector3(10f, -0.055f, TaxiZ), new Vector2(38f, 2.6f), Asfalto);
            Chao("Pista", new Vector3(10f, -0.05f, PistaZ), new Vector2(46f, 3.8f), Art.Hex(0x4A4F5B));
            foreach (float x in new[] { -6f, 26f }) Chao("Ligacao", new Vector3(x, -0.055f, (TaxiZ + PistaZ) * 0.5f), new Vector2(2.6f, PistaZ - TaxiZ), Asfalto);
            for (int i = 0; i < 14; i++) Chao("FaixaPista", new Vector3(-12f + i * 3.2f, -0.04f, PistaZ), new Vector2(1.6f, 0.14f), Art.Hex(0xF4F1E8));
            for (int i = 0; i < 6; i++)
            {
                Chao("Cabeceira", new Vector3(-12.2f, -0.04f, PistaZ - 1.25f + i * 0.5f), new Vector2(1.4f, 0.22f), Art.Hex(0xF4F1E8));
                Chao("Cabeceira", new Vector3(32.2f, -0.04f, PistaZ - 1.25f + i * 0.5f), new Vector2(1.4f, 0.22f), Art.Hex(0xF4F1E8));
            }
            for (int i = 0; i < 18; i++) Chao("FaixaTaxi", new Vector3(-7f + i * 2f, -0.045f, TaxiZ), new Vector2(1f, 0.1f), Art.Hex(0xF2C14E));
            for (int g = 0; g < 2; g++)   // linha de entrada do estande (amarela) e a caixa de seguranca
                Chao("LinhaEstande", new Vector3(EstandeX[g], -0.045f, (EstandeZ + TaxiZ) * 0.5f - 1.6f), new Vector2(0.1f, TaxiZ - EstandeZ + 1.8f), Art.Hex(0xF2C14E));

            for (int k = 0; k < 2; k++)
            {
                Aviao a = _avioes[k];
                a.T = Peca(AviaoMalha[k], Vector3.zero, 0f);
                if (a.T == null) a.T = AviaoPrimitivo(k);
                else a.Meio = Medida(a.T).z * 0.5f;
                a.Sombra = SombraDe(a.T);
                a.Selo = Billboard("Selo" + k, Art.Aviao(), Art.VooCor[k], 0.9f);
                a.T.gameObject.SetActive(false);
            }

            // voltas dos veiculos: ao lado do aviao do G (por baixo da asa direita) e do R (asa esquerda)
            _voltas = new[]
            {
                Caminho(1.2f, 0f, 0f, V(13.0f, 11.0f), V(15.4f, 11.0f), V(15.4f, 16.8f), V(13.0f, 16.8f), V(13.0f, 11.0f)),
                Caminho(1.2f, 0f, 0f, V(17.2f, 11.0f), V(19.4f, 11.0f), V(19.4f, 16.8f), V(17.2f, 16.8f), V(17.2f, 11.0f)),
            };
            NovoVeiculo("rebocador", -1, 0f, 2.0f, Art.Hex(0xF2C14E), 1.5f);
            NovoVeiculo("caminhao_combustivel", 0, 0.1f, 1.6f, Art.Hex(0xE0533D), 2.6f);
            NovoVeiculo("carrinho_bagagem", 0, 0.6f, 1.6f, Art.Hex(0x5DADE2), 2.6f);
            NovoVeiculo("escada_embarque", 1, 0.25f, 1.3f, Art.Hex(0xE5E8E8), 2.3f);
            NovoVeiculo("onibus_patio", 1, 0.75f, 1.5f, Art.Hex(0x3FA7D6), 3.2f);
            NovoVeiculo("caminhao_catering", 1, 0.5f, 1.4f, Art.Hex(0xF2F4F7), 2.8f);
            _veiculos[0].T.position = V3(ReboquePark);

            // ao abrir: os voos da onda em curso chegam (o 2o, 8 s depois): o mundo abre com um pouso
            Sim s = S;
            for (int f = 0; f < 2; f++)
                if (s.Voos[f] != null && !s.Voos[f].Partiu) Chega(s.Voos[f].Id, f * 8f);
        }

        static Vector3 V(float x, float z, float y = 0f) => new Vector3(x, y, z);

        /// <summary>Faixa plana de chao (pista, faixa, patio): cubo achatado com a cor por MaterialPropertyBlock.</summary>
        void Chao(string nome, Vector3 centro, Vector2 tam, Color cor) => Bloco(nome, PrimitiveType.Cube, centro, new Vector3(tam.x, 0.02f, tam.y), cor);

        Transform AviaoPrimitivo(int k)
        {
            var t = new GameObject("Aviao" + k).transform;
            t.SetParent(_raiz, false);
            Color branco = Art.Hex(0xF2F4F7), cinza = Art.Hex(0xA6ACAF);   // greybox da v0.1, de frente para +z
            Bloco("Fuselagem", PrimitiveType.Capsule, new Vector3(0f, 1.0f, 0f), new Vector3(0.95f, 3.6f, 0.95f), branco, t).transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            Bloco("Asas", PrimitiveType.Cube, new Vector3(0f, 0.85f, 0.2f), new Vector3(6.4f, 0.08f, 1.3f), branco, t);
            Bloco("Estabilizador", PrimitiveType.Cube, new Vector3(0f, 1.15f, -3.1f), new Vector3(2.3f, 0.06f, 0.7f), branco, t);
            Bloco("Cauda", PrimitiveType.Cube, new Vector3(0f, 1.85f, -3.1f), new Vector3(0.08f, 1.3f, 1.0f), Art.VooCor[k], t);
            Bloco("Cabine", PrimitiveType.Cube, new Vector3(0f, 1.25f, 3.25f), new Vector3(0.7f, 0.2f, 0.3f), cinza, t);
            return t;
        }

        void NovoVeiculo(string malha, int volta, float fase, float vel, Color cor, float comp)
        {
            var v = new Veiculo { Volta = volta, S = fase, Vel = vel };
            v.T = Peca(malha, Vector3.zero, 0f);
            if (v.T == null)
            {
                v.T = new GameObject(malha).transform;
                v.T.SetParent(_raiz, false);
                Bloco("Carroceria", PrimitiveType.Cube, new Vector3(0f, 0.3f, -0.1f), new Vector3(0.55f, 0.45f, comp - 0.4f), cor, v.T);
                Bloco("Cabine", PrimitiveType.Cube, new Vector3(0f, 0.38f, comp * 0.5f - 0.2f), new Vector3(0.55f, 0.55f, 0.35f), Art.Hex(0xECF0F1), v.T);
            }
            v.Sombra = SombraDe(v.T);
            if (volta >= 0) v.S = fase * _voltas[volta].Total;
            _veiculos.Add(v);
        }

        /// <summary>Elipse escura no chao, do tamanho da planta do objeto (medida uma vez, parado em yaw 0).</summary>
        SpriteRenderer SombraDe(Transform t)
        {
            Vector3 m = Medida(t);
            SpriteRenderer r = Art.NewSprite(_raiz, "Sombra" + t.name, Art.Disc(), new Color(0f, 0f, 0f, 0.28f), -32000, Vector2.zero, new Vector2(m.x * 0.9f, m.z * 0.9f));
            return r;
        }

        /// <summary>Medida (x, y, z) do objeto em yaw 0, pelos renderers.</summary>
        static Vector3 Medida(Transform t)
        {
            Quaternion q = t.rotation;
            t.rotation = Quaternion.identity;
            var b = new Bounds(t.position, Vector3.zero);
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            t.rotation = q;
            return b.size;
        }

        // ------------------------------------------------------------------ trajetos

        /// <summary>Polilinha com as quinas arredondadas (raio `raio`, Bezier quadratica), amostrada a cada ~0,25 m.</summary>
        static Trecho Caminho(float raio, float v0, float v1, params Vector3[] p)
        {
            var t = new Trecho { V0 = v0, V1 = v1 };
            void Add(Vector3 q)
            {
                if (t.P.Count > 0 && (q - t.P[t.P.Count - 1]).sqrMagnitude < 1e-6f) return;
                t.L.Add(t.P.Count == 0 ? 0f : t.L[t.L.Count - 1] + Vector3.Distance(t.P[t.P.Count - 1], q));
                t.P.Add(q);
            }
            void Reta(Vector3 a, Vector3 b)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / 0.25f));
                for (int i = 0; i <= n; i++) Add(Vector3.Lerp(a, b, i / (float)n));
            }
            Vector3 ini = p[0];
            for (int i = 1; i < p.Length - 1; i++)
            {
                Vector3 a = p[i - 1], c = p[i], d = p[i + 1];
                float r = Mathf.Min(raio, Vector3.Distance(a, c) * 0.5f, Vector3.Distance(c, d) * 0.5f);
                Vector3 e = c + (a - c).normalized * r, s = c + (d - c).normalized * r;
                Reta(ini, e);
                for (int k = 1; k <= 8; k++)
                {
                    float u = k / 8f;
                    Add((1 - u) * (1 - u) * e + 2 * (1 - u) * u * c + u * u * s);
                }
                ini = s;
            }
            Reta(ini, p[p.Length - 1]);
            return t;
        }

        static void Amostra(Trecho t, float s, out Vector3 pos, out Vector3 dir)
        {
            s = Mathf.Clamp(s, 0f, t.Total);
            int i = t.L.BinarySearch(s);
            if (i < 0) i = ~i;
            i = Mathf.Clamp(i, 1, t.P.Count - 1);
            float l = t.L[i] - t.L[i - 1];
            pos = Vector3.Lerp(t.P[i - 1], t.P[i], l > 0f ? (s - t.L[i - 1]) / l : 1f);
            dir = t.P[i] - t.P[i - 1];
        }

        /// <summary>Pouso de oeste para leste, rolagem, saida pela ligacao leste e taxi ate o estande `g`, de nariz para o terminal.</summary>
        void Pousa(Aviao a, int g)
        {
            float x = EstandeX[g];
            a.Trechos.Enqueue(Caminho(0f, 11f, 8f, V(-34f, PistaZ, 9f), V(-5f, PistaZ, 0f)));
            a.Trechos.Enqueue(Caminho(0f, 8f, 2.6f, V(-5f, PistaZ), V(22f, PistaZ)));
            a.Trechos.Enqueue(Caminho(2.6f, 2.6f, 1.2f, V(22f, PistaZ), V(26f, PistaZ), V(26f, TaxiZ), V(x, TaxiZ), V(x, EstandeZ)));
        }

        /// <summary>Pushback (de re, rebocado: a cauda gira para leste e o nariz fica para oeste), taxi ate a cabeceira oeste e decolagem.</summary>
        void Parte(Aviao a)
        {
            float x = EstandeX[Mathf.Max(0, a.Estande)];
            Trecho push = Caminho(2.4f, 0.9f, 0.9f, V(x, EstandeZ), V(x, TaxiZ), V(x + 2.6f, TaxiZ));
            push.Re = true;
            push.Reboque = a.Estande == 0;   // na ponte o rebocador empurra; no remoto o aviao sai sozinho
            a.Trechos.Enqueue(push);
            a.Trechos.Enqueue(Caminho(2.6f, 1.2f, 2.6f, V(x + 2.6f, TaxiZ), V(-6f, TaxiZ), V(-6f, PistaZ), V(-3f, PistaZ)));
            a.Trechos.Enqueue(Caminho(0f, 2f, 14f, V(-3f, PistaZ), V(16f, PistaZ)));
            a.Trechos.Enqueue(Caminho(0f, 14f, 16f, V(16f, PistaZ), V(44f, PistaZ, 9f)));
        }

        /// <summary>Do estande remoto para o da ponte: pushback curto e taxi pela taxiway.</summary>
        void ParaPonte(Aviao a)
        {
            float x = EstandeX[1], g = EstandeX[0];
            Trecho push = Caminho(2.4f, 0.9f, 0.9f, V(x, EstandeZ), V(x, TaxiZ), V(x + 2.6f, TaxiZ));
            push.Re = true;
            a.Trechos.Enqueue(push);
            a.Trechos.Enqueue(Caminho(2.6f, 1.2f, 1.2f, V(x + 2.6f, TaxiZ), V(g, TaxiZ), V(g, EstandeZ)));
        }

        // ------------------------------------------------------------------ quadro a quadro

        void Chega(int vooId, float espera)
        {
            Aviao a = _avioes[vooId % 2];
            if (a.Voo == vooId && (a.QuerChegar || a.Fase != Fase.Fora)) return;   // o mesmo voo de novo (aberto no meio da onda)
            a.Voo = vooId; a.QuerChegar = true; a.Espera = espera;
        }

        bool PonteLivre(Aviao quem)
        {
            foreach (Aviao o in _avioes) if (o != quem && o.Estande == 0) return false;
            return true;
        }

        void Patio(Sim s, float dt)
        {
            foreach (Aviao a in _avioes) AviaoPasso(a, dt);
            for (int i = 0; i < _veiculos.Count; i++) VeiculoPasso(_veiculos[i], dt);
        }

        void AviaoPasso(Aviao a, float dt)
        {
            if (a.Espera > 0f) a.Espera -= dt;
            if (a.Trechos.Count == 0)
            {
                if (a.Fase == Fase.Chegando) a.Fase = Fase.NoPatio;
                if (a.Fase == Fase.Saindo) { a.Fase = Fase.Fora; a.Estande = -1; }
                if (a.Fase == Fase.Fora && a.QuerChegar && a.Espera <= 0f)
                {
                    a.Estande = PonteLivre(a) ? 0 : 1;
                    Pousa(a, a.Estande);
                    a.Fase = Fase.Chegando; a.QuerChegar = false; a.S = 0f;
                }
                else if (a.Fase == Fase.NoPatio && a.QuerPartir)
                {
                    Parte(a);
                    a.Fase = Fase.Saindo; a.QuerPartir = false; a.S = 0f;
                }
                else if (a.Fase == Fase.NoPatio && a.Estande == 1 && PonteLivre(a))
                {
                    ParaPonte(a);
                    a.Estande = 0; a.Fase = Fase.Chegando; a.S = 0f;
                }
            }
            bool visivel = a.Fase != Fase.Fora || a.Trechos.Count > 0;
            a.T.gameObject.SetActive(visivel);
            a.Sombra.enabled = visivel;
            a.Selo.enabled = visivel && a.Fase != Fase.Saindo && a.Voo >= 0;
            if (!visivel) return;
            if (a.Trechos.Count > 0)
            {
                Trecho t = a.Trechos.Peek();
                float v = Mathf.Lerp(t.V0, t.V1, t.Total > 0f ? a.S / t.Total : 1f);
                a.S += v * Mathf.Max(0f, dt);
                Amostra(t, a.S, out Vector3 pos, out Vector3 dir);
                if (t.Re) dir = -dir;
                Vector3 plano = new Vector3(dir.x, 0f, dir.z);
                // nariz um pouco para cima na subida e quase reto na descida (aviao de verdade pousa de nariz alto, nao mergulhando)
                float pitch = plano.sqrMagnitude > 1e-8f ? Mathf.Clamp(-Mathf.Atan2(dir.y, plano.magnitude) * Mathf.Rad2Deg * 0.6f, -9f, 2f) : 0f;
                if (plano.sqrMagnitude > 1e-8f) a.T.rotation = Quaternion.LookRotation(plano.normalized, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);
                a.T.position = pos;
                if (t.Reboque) Reboca(a);
                if (a.S >= t.Total)
                {
                    a.Trechos.Dequeue();
                    a.S = 0f;
                    if (t.Reboque) RebocadorVolta();
                    if (t.Re && a.Fase == Fase.Saindo) a.Estande = -1;   // a ponte libera quando o pushback acaba
                }
            }
            Vector3 p = a.T.position;
            float alt = Mathf.Clamp01(p.y / 9f);
            a.Sombra.transform.position = new Vector3(p.x + p.y * 0.35f, 0.015f, p.z + p.y * 0.35f);
            a.Sombra.transform.rotation = Quaternion.Euler(90f, a.T.eulerAngles.y, 0f);
            a.Sombra.color = new Color(0f, 0f, 0f, 0.28f * (1f - alt));
            if (a.Selo.enabled)
            {
                Vector3 ps = p + Vector3.up * 4.4f;
                a.Selo.transform.position = ps;
                a.Selo.transform.rotation = _cam.transform.rotation;
                a.Selo.color = Art.VooCor[a.Voo % 2];
                a.Selo.sortingOrder = Ordem(ps) + 9;
            }
        }

        /// <summary>Rebocador preso no nariz durante o pushback, de frente para o aviao.</summary>
        void Reboca(Aviao a)
        {
            Veiculo r = _veiculos[0];
            Vector3 nariz = a.T.forward;
            nariz.y = 0f;
            r.Rota = null;
            r.T.position = new Vector3(a.T.position.x, 0f, a.T.position.z) + nariz.normalized * (a.Meio + 0.9f);
            r.T.rotation = Quaternion.LookRotation(-nariz.normalized, Vector3.up);
        }

        void RebocadorVolta()
        {
            Veiculo r = _veiculos[0];
            r.Rota = Caminho(1f, 2f, 2f, r.T.position, new Vector3(r.T.position.x, 0f, ReboquePark.y + 1.5f), V3(ReboquePark));
            r.S = 0f;
        }

        void VeiculoPasso(Veiculo v, float dt)
        {
            Trecho t;
            float vel;
            if (v.Volta < 0)
            {
                if (v.Rota == null) { Sombra(v); return; }
                t = v.Rota; vel = v.Vel;
                if (v.S >= t.Total) { v.Rota = null; Sombra(v); return; }
            }
            else
            {
                t = _voltas[v.Volta];
                bool ocupado = false;
                foreach (Aviao a in _avioes) if (a.Fase == Fase.NoPatio && a.Estande == v.Volta) ocupado = true;
                vel = ocupado ? v.Vel : 0f;
                if (v.S >= t.Total) v.S -= t.Total;
            }
            v.S += vel * Mathf.Max(0f, dt);
            Amostra(t, v.Volta >= 0 ? Mathf.Repeat(v.S, t.Total) : v.S, out Vector3 pos, out Vector3 dir);
            v.T.position = pos;
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-8f) v.T.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            Sombra(v);
        }

        static void Sombra(Veiculo v)
        {
            v.Sombra.transform.position = new Vector3(v.T.position.x, 0.015f, v.T.position.z);
            v.Sombra.transform.rotation = Quaternion.Euler(90f, v.T.eulerAngles.y, 0f);
        }

        /// <summary>Game: eventos do nucleo que o patio mostra (o voo novo chega, o voo que fecha sai).</summary>
        void EventoPatio(Evento e)
        {
            if (e.Tipo == Ev.VooCriado) Chega(e.A, e.A % 2 * 8f);
            else if (e.Tipo == Ev.VooPartiu) _avioes[e.A % 2].QuerPartir = true;
        }
    }
}
