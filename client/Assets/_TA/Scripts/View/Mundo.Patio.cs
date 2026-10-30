using System;
using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Patio vivo (GDD §5.13), so visual: nada aqui muda regra.
    ///  - 2 avioes, 1 por voo da onda (indice 0 = o regional, 1 = o jato), no ciclo pouso -> rolagem -> taxi -> estande -> pushback (com o
    ///    rebocador preso no nariz) -> taxi -> corrida -> decolagem.
    ///  - Estande G (ponte de embarque) e estande R (remoto). O voo novo pousa e vai para o G se estiver livre, senao para o R; quando o G
    ///    libera, o aviao do R vai para o G. VooCriado = o aviao chega; VooPartiu = o aviao sai.
    ///  - 6 veiculos: rebocador, caminhao de combustivel, carrinho de bagagem (volta do G), caminhao-escada, onibus e catering (volta
    ///    do R). Andam em volta do aviao estacionado com paradas de servico e esperam quando o estande esta vazio ("ainda sem regra").
    ///  - Movimento sem patinar (medido com -diag e client/tools/diag_patio.py): o pivo de cada peca e o eixo traseiro (o trem principal
    ///    no aviao) e o rumo e a tangente do trajeto nesse ponto, como num carro de verdade; a frente de cada malha do Tripo esta na
    ///    tabela Frente; as curvas sao arcos de raio fixo; a velocidade sobe e desce com aceleracao limitada, freia para a curva
    ///    (aceleracao lateral) e para o ponto de parada; o corpo inclina um pouco na freada e na curva (a malha e um bloco so, sem roda).
    ///  - Sombra: silhueta achatada nos avioes (SombraPlana), elipse nos veiculos (sem sombra dinamica, GDD §10.4).
    /// </summary>
    public sealed partial class Mundo
    {
        // pista ao norte (pousa e decola para leste), taxiway e estandes de nariz para o terminal (para -z)
        const float PistaZ = 23f, TaxiZ = 19f, NarizZ = 10.2f, RaioAviao = 3.4f, RaioVeic = 1.5f;
        const float LatAviao = 0.5f, LatVeic = 0.8f;   // aceleracao lateral maxima (m/s2): limita a velocidade na curva
        static readonly float[] EstandeX = { 11.4f, 21f };
        static readonly string[] AviaoMalha = { "aviao_regional", "aviao_narrow" };
        /// <summary>Rebocador: vaga (de frente para oeste, na beira do patio) e o ponto em que a barra encosta no nariz do aviao da ponte.</summary>
        static readonly Vector3 ReboqueVaga = new Vector3(14.6f, 0f, 8.5f), ReboqueNariz = new Vector3(11.4f, 0f, 9.4f);
        enum Reboque { Vago, Indo, Pronto, Preso, Voltando }

        /// <summary>Trecho de trajeto: pontos densos, comprimento acumulado e curvatura em cada ponto; V = cruzeiro, A e F = aceleracao e
        /// freada (m/s2); Para = termina parado; Sai = velocidade maxima na saida do trecho (o limite de curva so enxerga o trecho atual: a curva
        /// no comeco do trecho seguinte entra aqui); Re = o nariz aponta contra o movimento (pushback).</summary>
        sealed class Trecho
        {
            public readonly List<Vector3> P = new List<Vector3>();
            public readonly List<float> L = new List<float>(), K = new List<float>();
            public float V = 2f, A = 0.5f, F = 0.8f, Sai = -1f, PausaDepois;
            public bool Para, Re, Reboque, Laco;
            public float Total => L.Count > 0 ? L[L.Count - 1] : 0f;
        }

        enum Fase { Fora, Chegando, NoPatio, Saindo }

        sealed class Aviao
        {
            public Transform T, Sombra, Malha;
            public SpriteRenderer Selo;
            public Quaternion Base = Quaternion.identity;
            public float Nariz = 3.7f, Espera, Pausa, V, S, Pitch, Roll, PitchVoo;
            public Fase Fase;
            public int Estande = -1, Voo = -1;
            public bool QuerChegar, QuerPartir;
            public readonly Queue<Trecho> Trechos = new Queue<Trecho>();
        }

        sealed class Veiculo
        {
            public Transform T, Malha;
            public SpriteRenderer Sombra;
            public Quaternion Base = Quaternion.identity;
            public int Volta = -1, Prox;   // Volta -1 = o rebocador; Prox = proxima parada da volta
            public float S, V, Vel, Comp, Espera, Pitch, Roll;
            public float[] Paradas;
            public readonly Queue<Trecho> Rotas = new Queue<Trecho>();
        }

        readonly Aviao[] _avioes = { new Aviao(), new Aviao() };
        readonly List<Veiculo> _veiculos = new List<Veiculo>();
        Trecho[] _voltas;
        Reboque _reboque;
        System.IO.StreamWriter _diag;
        float _diagT, _diagUlt;

        // ------------------------------------------------------------------ construcao

        void Patio3D()
        {
            Chao("Patio", new Vector3(15.5f, -0.06f, 13.05f), new Vector2(19.5f, 9.7f), Asfalto);
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
            for (int g = 0; g < 2; g++)   // linha de entrada do estande (amarela)
                Chao("LinhaEstande", new Vector3(EstandeX[g], -0.045f, (NarizZ + TaxiZ) * 0.5f + 0.4f), new Vector2(0.1f, TaxiZ - NarizZ - 1.4f), Art.Hex(0xF2C14E));

            for (int k = 0; k < 2; k++)
            {
                Aviao a = _avioes[k];
                a.T = Peca(AviaoMalha[k], Vector3.zero, 0f);
                if (a.T == null) a.T = AviaoPrimitivo(k);
                else
                {
                    a.Malha = a.T.GetChild(0);
                    a.Base = a.Malha.localRotation;
                    a.Nariz = Medida(a.T).z * 0.5f;
                }
                a.Sombra = SombraPlana(a.T);
                a.Selo = Billboard("Selo" + k, Art.Aviao(), Art.VooCor[k], 0.9f);
                a.T.gameObject.SetActive(false);
                if (a.Sombra != null) a.Sombra.gameObject.SetActive(false);
            }

            // voltas dos veiculos: ao lado do aviao do G (por baixo da asa direita) e do R (asa esquerda), em sentido unico
            _voltas = new[]
            {
                Volta(V(14.2f, 11.2f), V(15.5f, 11.2f), V(15.5f, 16.9f), V(12.9f, 16.9f), V(12.9f, 11.2f), V(14.2f, 11.2f)),
                Volta(V(18.3f, 11.2f), V(19.6f, 11.2f), V(19.6f, 16.9f), V(17f, 16.9f), V(17f, 11.2f), V(18.3f, 11.2f)),
            };
            NovoVeiculo("rebocador", -1, 2.2f, 0.35f, Art.Hex(0xF2C14E), 1.5f);
            NovoVeiculo("caminhao_combustivel", 0, 1.6f, 0.75f, Art.Hex(0xE0533D), 2.6f, 0.16f, 0.66f);
            NovoVeiculo("carrinho_bagagem", 0, 1.5f, 0.6f, Art.Hex(0x5DADE2), 2.6f, 0.42f, 0.92f);
            NovoVeiculo("escada_embarque", 1, 1.3f, 0.7f, Art.Hex(0xE5E8E8), 2.3f, 0.1f, 0.6f);
            NovoVeiculo("onibus_patio", 1, 1.5f, 0.9f, Art.Hex(0x3FA7D6), 3.2f, 0.34f, 0.84f);
            NovoVeiculo("caminhao_catering", 1, 1.4f, 0.75f, Art.Hex(0xF2F4F7), 2.8f, 0.58f, 0.08f);
            Veiculo r = _veiculos[0];
            r.T.SetPositionAndRotation(ReboqueVaga, Quaternion.Euler(0f, -90f, 0f));
            Sombra(r);

            // ao abrir: os voos da onda em curso chegam (o 2o, 8 s depois): o mundo abre com um pouso
            Sim s = S;
            for (int f = 0; f < 2; f++)
                if (s.Voos[f] != null && !s.Voos[f].Partiu) Chega(s.Voos[f].Id, f * 8f);
        }

        static Vector3 V(float x, float z, float y = 0f) => new Vector3(x, y, z);

        static Trecho Volta(params Vector3[] p)
        {
            Trecho t = Caminho(RaioVeic, 1.6f, p);
            t.Laco = true; t.A = 0.6f; t.F = 0.9f;
            return t;
        }

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

        /// <summary>Veiculo com o pivo no eixo traseiro (`eixo` = do eixo ao centro da malha, em m) e as paradas da volta (fracao).</summary>
        void NovoVeiculo(string malha, int volta, float vel, float eixo, Color cor, float comp, params float[] paradas)
        {
            var v = new Veiculo { Volta = volta, Vel = vel, Comp = comp };
            v.T = Peca(malha, Vector3.zero, 0f, 1f, eixo);
            if (v.T != null) { v.Malha = v.T.GetChild(0); v.Base = v.Malha.localRotation; }
            else
            {
                v.T = new GameObject(malha).transform;
                v.T.SetParent(_raiz, false);
                Bloco("Carroceria", PrimitiveType.Cube, new Vector3(0f, 0.3f, -0.1f), new Vector3(0.55f, 0.45f, comp - 0.4f), cor, v.T);
                Bloco("Cabine", PrimitiveType.Cube, new Vector3(0f, 0.38f, comp * 0.5f - 0.2f), new Vector3(0.55f, 0.55f, 0.35f), Art.Hex(0xECF0F1), v.T);
            }
            v.Sombra = SombraDe(v.T);
            if (volta >= 0)
            {
                float total = _voltas[volta].Total;
                v.Paradas = new float[paradas.Length];
                for (int i = 0; i < paradas.Length; i++) v.Paradas[i] = paradas[i] * total;
                Array.Sort(v.Paradas);
                v.S = v.Paradas[0];   // comeca parado na 1a parada
                v.Prox = 1 % v.Paradas.Length;
                v.Espera = 1f + volta + _veiculos.Count * 1.3f;
                Pose(v.T, _voltas[volta], v.S);
            }
            _veiculos.Add(v);
        }

        /// <summary>Elipse escura no chao, do tamanho da planta do objeto (medida uma vez, parado em yaw 0).</summary>
        SpriteRenderer SombraDe(Transform t)
        {
            Vector3 m = Medida(t);
            return Art.NewSprite(_raiz, "Sombra" + t.name, Art.Disc(), new Color(0f, 0f, 0f, 0.28f), -32000, Vector2.zero, new Vector2(m.x * 0.9f, m.z * 0.9f));
        }

        /// <summary>Medida (x, y, z) do objeto em yaw 0, pelos renderers.</summary>
        static Vector3 Medida(Transform t)
        {
            Quaternion q = t.rotation;
            t.rotation = Quaternion.identity;
            var b = new Bounds(t.position, Vector3.zero);
            bool um = false;
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
            {
                if (um) b.Encapsulate(r.bounds); else { b = r.bounds; um = true; }
            }
            t.rotation = q;
            return b.size;
        }

        // ------------------------------------------------------------------ trajetos

        /// <summary>Polilinha com as quinas em arco de circulo (tangente `raio` a partir da quina), amostrada a cada ~0,2 m, com a curvatura em
        /// cada ponto. Nas pontas o arco usa ate 85% do segmento (sobra um pedaco reto: o rumo no ponto de parada e exato); entre 2 quinas, a metade.</summary>
        static Trecho Caminho(float raio, float v, params Vector3[] p)
        {
            var t = new Trecho { V = v };
            void Add(Vector3 q, float k)
            {
                if (t.P.Count > 0 && (q - t.P[t.P.Count - 1]).sqrMagnitude < 1e-6f) return;
                t.L.Add(t.P.Count == 0 ? 0f : t.L[t.L.Count - 1] + Vector3.Distance(t.P[t.P.Count - 1], q));
                t.P.Add(q);
                t.K.Add(k);
            }
            void Reta(Vector3 a, Vector3 b)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / 0.2f));
                for (int i = 0; i <= n; i++) Add(Vector3.Lerp(a, b, i / (float)n), 0f);
            }
            Vector3 ini = p[0];
            for (int i = 1; i < p.Length - 1; i++)
            {
                Vector3 a = p[i - 1], c = p[i], d = p[i + 1], ua = (a - c).normalized, ub = (d - c).normalized;
                float tan = Mathf.Min(raio, Vector3.Distance(a, c) * (i == 1 ? 0.85f : 0.5f), Vector3.Distance(c, d) * (i == p.Length - 2 ? 0.85f : 0.5f));
                float meio = Vector3.Angle(ua, ub) * 0.5f * Mathf.Deg2Rad;
                Vector3 e = c + ua * tan, s = c + ub * tan;
                Reta(ini, e);
                if (tan > 0.01f && meio < 1.55f)
                {
                    float r = tan * Mathf.Tan(meio);
                    Vector3 o = c + (ua + ub).normalized * (tan / Mathf.Cos(meio)), v0 = e - o, v1 = s - o;
                    int n = Mathf.Max(4, Mathf.CeilToInt(r * (Mathf.PI - 2f * meio) / 0.2f));
                    for (int k = 1; k <= n; k++) Add(o + Vector3.Slerp(v0, v1, k / (float)n), 1f / Mathf.Max(r, 0.05f));
                }
                ini = s;
            }
            Reta(ini, p[p.Length - 1]);
            return t;
        }

        static int Indice(Trecho t, float s)
        {
            int i = t.L.BinarySearch(s);
            return Mathf.Clamp(i < 0 ? ~i : i, 1, t.P.Count - 1);
        }

        static Vector3 Ponto(Trecho t, float s)
        {
            s = t.Laco ? Mathf.Repeat(s, t.Total) : Mathf.Clamp(s, 0f, t.Total);
            int i = Indice(t, s);
            float l = t.L[i] - t.L[i - 1];
            return Vector3.Lerp(t.P[i - 1], t.P[i], l > 0f ? (s - t.L[i - 1]) / l : 1f);
        }

        /// <summary>Tangente suavizada do trajeto em s (diferenca centrada de 0,6 m), no sentido do movimento.</summary>
        static Vector3 Tangente(Trecho t, float s) => Ponto(t, s + 0.3f) - Ponto(t, s - 0.3f);

        /// <summary>Poe a peca no trajeto: o pivo (eixo traseiro) em s e o rumo na tangente; de re, o rumo e o oposto.</summary>
        static void Pose(Transform p, Trecho t, float s)
        {
            Vector3 d = Tangente(t, s);
            d.y = 0f;
            p.position = Ponto(t, s);
            if (d.sqrMagnitude > 1e-6f) p.rotation = Quaternion.LookRotation(t.Re ? -d : d, Vector3.up);
        }

        /// <summary>Velocidade do proximo quadro: sobe ate `alvo` com a aceleracao do trecho, freia para a parada ou a saida do trecho a `falta` m
        /// e para cada curva ao alcance da freada (v2 = aLat / curvatura).</summary>
        static float Passo(float v, Trecho t, float s, float alvo, float falta, float aLat, float dt)
        {
            float sai = Mathf.Max(t.Sai, 0f), teto = Mathf.Min(alvo, Mathf.Sqrt(sai * sai + 2f * t.F * Mathf.Max(falta, 0f)) + 0.06f);
            float alcance = v * v / (2f * t.F) + 0.8f;
            for (int i = Indice(t, Mathf.Min(s, t.Total)); i < t.P.Count && t.L[i] <= s + alcance; i++)
                if (t.K[i] > 1e-3f) teto = Mathf.Min(teto, Mathf.Sqrt(aLat / t.K[i] + 2f * t.F * Mathf.Max(t.L[i] - s, 0f)));
            return Mathf.MoveTowards(v, teto, (v < teto ? t.A : t.F) * dt);
        }

        /// <summary>A malha e um bloco so (sem roda que gire): o peso aparece no corpo. Mergulha na freada, levanta na partida e deita para fora da curva.</summary>
        static void Inclina(Transform malha, Quaternion basal, float longit, float lateral, float dt, ref float pitch, ref float roll, float forca)
        {
            if (malha == null || dt <= 0f) return;
            pitch = Mathf.MoveTowards(pitch, Mathf.Clamp(-longit * 1.6f * forca, -2f, 2f), 6f * dt);
            roll = Mathf.MoveTowards(roll, Mathf.Clamp(lateral * 1.6f * forca, -2.2f, 2.2f), 6f * dt);
            malha.localRotation = Quaternion.Euler(pitch, 0f, roll) * basal;
        }

        static float ZEstande(Aviao a) => NarizZ + a.Nariz;

        /// <summary>Pouso de oeste para leste (planeio, arredondamento e toque), rolagem freando, saida pela ligacao leste e taxi ate o estande `g`,
        /// de nariz para o terminal.</summary>
        void Pousa(Aviao a, int g)
        {
            float x = EstandeX[g], z = ZEstande(a);
            Trecho ar = Caminho(0f, 10f, V(-34f, PistaZ, 9f), V(-16f, PistaZ, 3.4f), V(-8f, PistaZ, 1.0f), V(-4f, PistaZ, 0.22f), V(0f, PistaZ));
            Trecho rola = Caminho(0f, 2.6f, V(0f, PistaZ), V(21f, PistaZ));
            rola.F = 2.2f; rola.Sai = 1.3f;   // o reverso segura: sai da rolagem na velocidade da curva da ligacao
            Trecho taxi = Caminho(RaioAviao, 2.6f, V(21f, PistaZ), V(26f, PistaZ), V(26f, TaxiZ), V(x, TaxiZ), V(x, z));
            taxi.A = 0.4f; taxi.F = 0.6f; taxi.Para = true;
            a.V = 10f;
            a.Trechos.Enqueue(ar); a.Trechos.Enqueue(rola); a.Trechos.Enqueue(taxi);
        }

        /// <summary>Recuo de re (a cauda gira para leste e o nariz fica para oeste), parado na taxiway para o rebocador sair, e o resto.</summary>
        Trecho Recuo(float x, float z, bool reboque)
        {
            Trecho t = Caminho(3f, 0.9f, V(x, z), V(x, TaxiZ), V(x + 3f, TaxiZ));
            t.Re = t.Para = true; t.A = 0.3f; t.F = 0.4f; t.Reboque = reboque;
            t.PausaDepois = reboque ? 8f : 2f;
            return t;
        }

        void Parte(Aviao a)
        {
            float x = EstandeX[Mathf.Max(0, a.Estande)];
            a.Trechos.Enqueue(Recuo(x, ZEstande(a), a.Estande == 0));
            Trecho taxi = Caminho(RaioAviao, 2.6f, V(x + 3f, TaxiZ), V(-6f, TaxiZ), V(-6f, PistaZ), V(-2f, PistaZ));
            taxi.A = 0.4f; taxi.F = 0.6f;
            a.Trechos.Enqueue(taxi);
            Trecho corre = Caminho(0f, 14f, V(-2f, PistaZ), V(16f, PistaZ));
            corre.A = 1.7f;
            a.Trechos.Enqueue(corre);
            Trecho sobe = Caminho(0f, 16f, V(16f, PistaZ), V(30f, PistaZ, 2.2f), V(46f, PistaZ, 9f));
            sobe.A = 1f;
            a.Trechos.Enqueue(sobe);
        }

        /// <summary>Do estande remoto para o da ponte: recua por conta propria e taxia pela taxiway.</summary>
        void ParaPonte(Aviao a)
        {
            a.Trechos.Enqueue(Recuo(EstandeX[1], ZEstande(a), false));
            Trecho taxi = Caminho(RaioAviao, 2.2f, V(EstandeX[1] + 3f, TaxiZ), V(EstandeX[0], TaxiZ), V(EstandeX[0], ZEstande(a)));
            taxi.A = 0.4f; taxi.F = 0.6f; taxi.Para = true;
            a.Trechos.Enqueue(taxi);
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
            if (_reboque == Reboque.Voltando) return false;   // o rebocador cruza o estande na volta
            foreach (Aviao o in _avioes) if (o != quem && o.Estande == 0) return false;
            return true;
        }

        void Patio(Sim s, float dt)
        {
            foreach (Aviao a in _avioes) AviaoPasso(a, dt);
            for (int i = 0; i < _veiculos.Count; i++) VeiculoPasso(_veiculos[i], dt);
            DiagPasso(dt);
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
                    if (a.Estande != 0 || _reboque == Reboque.Pronto)
                    {
                        if (a.Estande == 0) _reboque = Reboque.Preso;
                        Parte(a);
                        a.Fase = Fase.Saindo; a.QuerPartir = false; a.S = 0f; a.V = 0f;
                    }
                    else if (_reboque == Reboque.Vago) ReboqueVai();   // espera o rebocador encostar no nariz
                }
                else if (a.Fase == Fase.NoPatio && a.Estande == 1 && PonteLivre(a))
                {
                    ParaPonte(a);
                    a.Estande = 0; a.Fase = Fase.Chegando; a.S = 0f; a.V = 0f;
                }
            }
            bool visivel = a.Fase != Fase.Fora || a.Trechos.Count > 0;
            a.T.gameObject.SetActive(visivel);
            if (a.Sombra != null) a.Sombra.gameObject.SetActive(visivel && a.T.position.y < 5f);   // no alto, a sombra fica longe e some
            a.Selo.enabled = visivel && a.Fase != Fase.Saindo && a.Voo >= 0;
            if (!visivel) return;
            float vAnt = a.V, yawAnt = a.T.eulerAngles.y;
            if (a.Pausa > 0f) a.Pausa -= dt;
            else if (a.Trechos.Count > 0)
            {
                Trecho t = a.Trechos.Peek();
                a.V = Passo(a.V, t, a.S, t.V, t.Para || t.Sai >= 0f ? t.Total - a.S : 1e4f, LatAviao, dt);
                a.S += a.V * Mathf.Max(0f, dt);
                Pose(a.T, t, a.S);
                Vector3 d = Tangente(t, a.S);
                // nariz sobe devagar: arredonda no pouso e roda na decolagem (nao mergulha na descida)
                float alvo = Mathf.Clamp(-Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg * 0.6f, -10f, 1f);
                if (t.V <= 2.6f || t.Re) alvo = 0f;
                a.PitchVoo = Mathf.MoveTowards(a.PitchVoo, alvo, 5f * dt);
                a.T.rotation *= Quaternion.Euler(a.PitchVoo, 0f, 0f);
                if (t.Reboque) Reboca(a, dt);
                if (a.S >= t.Total - 0.01f)
                {
                    a.Trechos.Dequeue();
                    a.S = Mathf.Max(0f, a.S - t.Total);
                    if (t.Para) { a.V = 0f; a.S = 0f; }
                    a.Pausa = t.PausaDepois;
                    if (t.Reboque) RebocadorVolta();
                    if (t.Re && a.Fase == Fase.Saindo) a.Estande = -1;   // a ponte libera quando o recuo acaba
                }
            }
            if (dt > 0f) Inclina(a.Malha, a.Base, (a.V - vAnt) / dt, a.V * Mathf.DeltaAngle(yawAnt, a.T.eulerAngles.y) * Mathf.Deg2Rad / dt, dt, ref a.Pitch, ref a.Roll, 0.5f);
            Vector3 p = a.T.position;
            if (a.Sombra != null)
            {
                a.Sombra.position = new Vector3(p.x + 0.25f + p.y * 0.35f, 0.015f, p.z + p.y * 0.35f);   // a luz vem do alto a esquerda
                a.Sombra.rotation = Quaternion.Euler(0f, a.T.eulerAngles.y, 0f);
            }
            if (a.Selo.enabled)
            {
                Vector3 ps = p + Vector3.up * 4.4f;
                a.Selo.transform.position = ps;
                a.Selo.transform.rotation = _cam.transform.rotation;
                a.Selo.color = Art.VooCor[a.Voo % 2];
                a.Selo.sortingOrder = Ordem(ps) + 9;
            }
        }

        /// <summary>O aviao da ponte quer sair: o rebocador sai da vaga, faz a curva e encosta a barra no nariz.</summary>
        void ReboqueVai()
        {
            Veiculo r = _veiculos[0];
            Trecho t = Caminho(1f, r.Vel, ReboqueVaga, new Vector3(ReboqueNariz.x, 0f, ReboqueVaga.z), ReboqueNariz);
            t.A = 0.7f; t.F = 0.8f; t.Para = true;
            r.Rotas.Enqueue(t);
            r.S = 0f;
            _reboque = Reboque.Indo;
        }

        /// <summary>Rebocador preso no nariz durante o pushback, empurrando.</summary>
        void Reboca(Aviao a, float dt)
        {
            Veiculo r = _veiculos[0];
            Vector3 nariz = a.T.forward;
            nariz.y = 0f;
            nariz.Normalize();
            Vector3 p = new Vector3(a.T.position.x, 0f, a.T.position.z) + nariz * (a.Nariz + (NarizZ - ReboqueNariz.z)), d = p - r.T.position;
            r.T.position = p;
            // a barra articula: o rebocador aponta para onde o nariz anda, nao para o eixo do aviao, e nao escorrega de lado na curva do recuo
            if (dt > 0f && d.magnitude / dt > 0.15f) r.T.rotation = Quaternion.RotateTowards(r.T.rotation, Quaternion.LookRotation(d, Vector3.up), 90f * dt);
            Sombra(r);
        }

        /// <summary>Solto na taxiway: recua fazendo a curva para o norte, cruza a taxiway para o sul, atravessa o patio pelo estande vazio e volta
        /// para a vaga (de frente para oeste): nenhum giro no lugar.</summary>
        void RebocadorVolta()
        {
            Veiculo r = _veiculos[0];
            Vector3 p = r.T.position, tras = -r.T.forward, fim = p + tras * 1.6f + Vector3.forward * 1.6f;
            Trecho re = Caminho(1.4f, 1.1f, p, p + tras * 1.6f, fim);
            re.Re = re.Para = true; re.A = 0.6f; re.F = 0.7f;
            Trecho vai = Caminho(1.4f, 2.2f, fim, new Vector3(fim.x, 0f, 12.4f), new Vector3(15.7f, 0f, 12.4f), new Vector3(15.7f, 0f, ReboqueVaga.z), ReboqueVaga);
            vai.A = 0.7f; vai.F = 0.8f; vai.Para = true;
            r.Rotas.Enqueue(re); r.Rotas.Enqueue(vai);
            r.S = 0f;
            _reboque = Reboque.Voltando;
        }

        void VeiculoPasso(Veiculo v, float dt)
        {
            float vAnt = v.V, yawAnt = v.T.eulerAngles.y;
            if (v.Volta < 0)
            {
                if (_reboque == Reboque.Preso) return;
                if (v.Rotas.Count == 0)
                {
                    if (_reboque == Reboque.Indo) _reboque = Reboque.Pronto;
                    else if (_reboque == Reboque.Voltando) _reboque = Reboque.Vago;
                    v.V = 0f;
                }
                else
                {
                    Trecho t = v.Rotas.Peek();
                    v.V = Passo(v.V, t, v.S, t.V, t.Para ? t.Total - v.S : 1e4f, LatVeic, dt);
                    v.S += v.V * Mathf.Max(0f, dt);
                    Pose(v.T, t, v.S);
                    if (v.S >= t.Total - 0.01f) { v.Rotas.Dequeue(); v.S = 0f; v.V = 0f; }
                }
            }
            else
            {
                Trecho t = _voltas[v.Volta];
                bool ocupado = false;
                foreach (Aviao a in _avioes) if (a.Fase == Fase.NoPatio && a.Estande == v.Volta) ocupado = true;
                if (v.Espera > 0f && ocupado) v.Espera -= dt;   // parado na parada: servico em andamento, ou sem aviao para atender
                float falta = Mathf.Repeat(v.Paradas[v.Prox] - v.S, t.Total);
                foreach (Veiculo o in _veiculos)   // nao encosta em quem vai na frente na mesma volta
                    if (o != v && o.Volta == v.Volta) falta = Mathf.Min(falta, Mathf.Repeat(o.S - v.S, t.Total) - (o.Comp + v.Comp) * 0.5f - 0.7f);
                float alvo = v.Espera > 0f ? 0f : v.Vel;
                v.V = Passo(v.V, t, Mathf.Repeat(v.S, t.Total), alvo, falta, LatVeic, dt);
                v.S = Mathf.Repeat(v.S + v.V * Mathf.Max(0f, dt), t.Total);
                if (v.Espera <= 0f && Mathf.Repeat(v.Paradas[v.Prox] - v.S, t.Total) < 0.04f)
                {
                    v.V = 0f;
                    v.Prox = (v.Prox + 1) % v.Paradas.Length;
                    v.Espera = 5f + 2.5f * v.Prox;
                }
                Pose(v.T, t, v.S);
            }
            if (dt > 0f) Inclina(v.Malha, v.Base, (v.V - vAnt) / dt, v.V * Mathf.DeltaAngle(yawAnt, v.T.eulerAngles.y) * Mathf.Deg2Rad / dt, dt, ref v.Pitch, ref v.Roll, 1f);
            Sombra(v);
        }

        static void Sombra(Veiculo v)
        {
            v.Sombra.transform.position = new Vector3(v.T.position.x, 0.015f, v.T.position.z);
            v.Sombra.transform.rotation = Quaternion.Euler(90f, v.T.eulerAngles.y, 0f);
        }

        /// <summary>-diag arq.csv (so no -testsession): grava a cada ~0,1 s de jogo a pose de cada aviao e veiculo em movimento; o
        /// client/tools/diag_patio.py calcula escorregamento, velocidade, aceleracao e giro.</summary>
        public void Diag(string arq)
        {
            _diag = new System.IO.StreamWriter(arq) { AutoFlush = false };
            _diag.WriteLine("t,peca,x,y,z,yaw");
        }

        public void DiagFim() { _diag?.Dispose(); _diag = null; }

        void DiagPasso(float dt)
        {
            if (_diag == null || dt <= 0f) return;
            _diagT += dt;
            if (_diagT - _diagUlt < 0.1f) return;
            _diagUlt = _diagT;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            for (int i = 0; i < _avioes.Length; i++)
                if (_avioes[i].T.gameObject.activeSelf) DiagLinha(inv, AviaoMalha[i], _avioes[i].T);
            for (int i = 0; i < _veiculos.Count; i++) DiagLinha(inv, _veiculos[i].T.name + "#" + i, _veiculos[i].T);
        }

        void DiagLinha(System.IFormatProvider inv, string nome, Transform t)
        {
            Vector3 p = t.position;
            _diag.WriteLine(string.Format(inv, "{0:0.00},{1},{2:0.000},{3:0.000},{4:0.000},{5:0.00}", _diagT, nome, p.x, p.y, p.z, t.eulerAngles.y));
        }

        /// <summary>Game: eventos do nucleo que o patio mostra (o voo novo chega, o voo que fecha sai).</summary>
        void EventoPatio(Evento e)
        {
            if (e.Tipo == Ev.VooCriado) Chega(e.A, e.A % 2 * 8f);
            else if (e.Tipo == Ev.VooPartiu) _avioes[e.A % 2].QuerPartir = true;
        }
    }
}
