using System;
using System.Collections.Generic;
using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// Patio vivo (GDD §5.12 e §5.13): a tela segue o nucleo (sim.Avioes) e nada aqui muda regra.
    ///  - 2 avioes, 1 por voo da onda (0 = o regional, 1 = o jato), cada um no seu estande: G (ponte de embarque) e R (remoto). O pouso comeca
    ///    PousoSeg antes da hora do nucleo (Aviao.Chega) e encosta quando ele encosta; Ev.Decolou solta o pushback (com o rebocador preso no
    ///    nariz no G), o taxi, a corrida e a decolagem; o hangar leva o aviao pela taxiway e o traz de volta antes de o nucleo liberar.
    ///  - Veiculos por servico (Aviao.Fazendo): limpeza = catering, combustivel = caminhao, bagagem = carrinho, cada um com a vaga numa coluna
    ///    entre os estandes (mao unica: sobe a coluna, faz a volta pela rua do norte, para ao lado da fuselagem e volta pela rua do sul).
    ///    Desembarque no G e pela ponte; no R, escada e onibus do lado de fora, que voltam de re. +1 veiculo (Capacidade) aparece na vaga de tras.
    ///  - Em cima do aviao: o icone do servico que falta com o anel de progresso (GDD §5.12), e o aviso de desgaste a partir de 80%.
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
        /// <summary>s do pouso ate parar no estande e da saida do hangar ate o estande (medidos com -diag): a tela comeca esse tempo antes do nucleo.</summary>
        static readonly float[] PousoSeg = { 24f, 19f };
        const float VoltaHangarSeg = 20f;
        /// <summary>Hangar no fim da taxiway, com a porta para oeste (a vista da camera): o aviao entra reto, de nariz; sai de re, com a cauda
        /// na ligacao leste da pista, e segue de frente: nenhum giro no lugar.</summary>
        static readonly Vector3 HangarEm = new Vector3(36.4f, 0f, TaxiZ);
        const float HangarYaw = 90f, HangarEscala = 1.1f, LigacaoX = 26f;
        /// <summary>Rebocador: vaga (de frente para oeste, na beira do patio) e o ponto em que a barra encosta no nariz do aviao da ponte.</summary>
        static readonly Vector3 ReboqueVaga = new Vector3(14.6f, 0f, 8.5f), ReboqueNariz = new Vector3(11.4f, 0f, 9.4f);
        enum Reboque { Vago, Indo, Pronto, Preso, Voltando }

        // ruas de servico entre os estandes, em mao unica: faixa de cada estande (ao lado da fuselagem), a coluna da vaga de cada servico
        // (limpeza, combustivel, bagagem), a parada (pivo) na faixa e as 2 vagas da coluna (0 = a do sul, 1 = a do norte, a que sai primeiro)
        const float RuaNorte = 17.4f, RuaSul = 10.6f;
        static readonly float[] FaixaX = { 12.9f, 19.6f }, ColunaX = { 0f, 15f, 16.3f, 17.6f }, ParadaZ = { 0f, 14f, 16.5f, 16.5f }, VagaZ = { 11.5f, 14.3f };
        static readonly string[] VeiculoMalha = { "", "caminhao_catering", "caminhao_combustivel", "carrinho_bagagem" };
        static readonly float[] VeiculoEixo = { 0f, 0.75f, 0.75f, 0.6f }, VeiculoComp = { 0f, 2.8f, 2.6f, 2.6f };
        static readonly Color[] VeiculoCor = { Color.white, Art.Hex(0xF2F4F7), Art.Hex(0xE0533D), Art.Hex(0x5DADE2) };
        /// <summary>Escada e onibus so no estande R, pela direita do aviao: vaga e parada; voltam de re pelo mesmo caminho.</summary>
        static readonly Vector3[] EscadaRota = { new Vector3(25.2f, 0f, 15.4f), new Vector3(25.2f, 0f, 11.6f), new Vector3(23.7f, 0f, 11.6f) };
        static readonly Vector3[] OnibusRota = { new Vector3(28.4f, 0f, 9.4f), new Vector3(25.3f, 0f, 9.4f) };

        /// <summary>Trecho de trajeto: pontos densos, comprimento acumulado e curvatura em cada ponto; V = cruzeiro, A e F = aceleracao e
        /// freada (m/s2); Para = termina parado; Sai = velocidade maxima na saida do trecho (o limite de curva so enxerga o trecho atual: a curva
        /// no comeco do trecho seguinte entra aqui); Re = o nariz aponta contra o movimento (pushback).</summary>
        sealed class Trecho
        {
            public readonly List<Vector3> P = new List<Vector3>();
            public readonly List<float> L = new List<float>(), K = new List<float>();
            public float V = 2f, A = 0.5f, F = 0.8f, Sai = -1f, PausaDepois;
            public bool Para, Re, Reboque;
            public float Total => L.Count > 0 ? L[L.Count - 1] : 0f;
        }

        enum Fase { Fora, Chegando, NoPatio, Saindo, IndoHangar, Hangar }
        enum Lida { Parado, Indo, Servindo, Voltando }

        sealed class Aviao
        {
            public int Id;
            public Transform T, Sombra, Malha;
            public SpriteRenderer Selo, Anel, Alerta;
            public Text Rotulo;
            public Quaternion Base = Quaternion.identity;
            public float Nariz = 3.7f, Pausa, V, S, Pitch, Roll, PitchVoo;
            public int CheckTotal;
            public Fase Fase;
            public bool QuerPartir;
            public readonly Queue<Trecho> Trechos = new Queue<Trecho>();
        }

        sealed class Veiculo
        {
            public Transform T, Malha;
            public SpriteRenderer Sombra;
            public Quaternion Base = Quaternion.identity;
            public int Servico = -1, Aviao = -1, Vaga = 1;   // Servico -1 = o rebocador; Aviao = o que esta atendendo
            public float S, V, Vel, Comp, Pitch, Roll;
            public Lida Lida;
            public Vector3[] Rota;   // escada e onibus (ida; a volta e de re)
            public readonly Queue<Trecho> Rotas = new Queue<Trecho>();
        }

        readonly Aviao[] _avioes = { new Aviao { Id = 0 }, new Aviao { Id = 1 } };
        readonly List<Veiculo> _veiculos = new List<Veiculo>();
        Veiculo _escada, _onibus;
        Reboque _reboque;
        System.IO.StreamWriter _diag;
        float _diagT, _diagUlt;

        // ------------------------------------------------------------------ construcao

        void Patio3D()
        {
            Chao("Patio", new Vector3(15.5f, -0.06f, 13.05f), new Vector2(19.5f, 9.7f), Asfalto);
            Chao("Taxiway", new Vector3(14f, -0.055f, TaxiZ), new Vector2(46f, 2.6f), Asfalto);
            Chao("Pista", new Vector3(10f, -0.05f, PistaZ), new Vector2(46f, 3.8f), Art.Hex(0x4A4F5B));
            foreach (float x in new[] { -6f, 26f }) Chao("Ligacao", new Vector3(x, -0.055f, (TaxiZ + PistaZ) * 0.5f), new Vector2(2.6f, PistaZ - TaxiZ), Asfalto);
            Chao("PatioOnibus", new Vector3(27.2f, -0.058f, 9.4f), new Vector2(4.4f, 1.8f), Asfalto);
            for (int i = 0; i < 14; i++) Chao("FaixaPista", new Vector3(-12f + i * 3.2f, -0.04f, PistaZ), new Vector2(1.6f, 0.14f), Art.Hex(0xF4F1E8));
            for (int i = 0; i < 6; i++)
            {
                Chao("Cabeceira", new Vector3(-12.2f, -0.04f, PistaZ - 1.25f + i * 0.5f), new Vector2(1.4f, 0.22f), Art.Hex(0xF4F1E8));
                Chao("Cabeceira", new Vector3(32.2f, -0.04f, PistaZ - 1.25f + i * 0.5f), new Vector2(1.4f, 0.22f), Art.Hex(0xF4F1E8));
            }
            for (int i = 0; i < 20; i++) Chao("FaixaTaxi", new Vector3(-7f + i * 2f, -0.045f, TaxiZ), new Vector2(1f, 0.1f), Art.Hex(0xF2C14E));
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
                a.Anel = Billboard("Anel" + k, Art.Arc(0f), Art.Good, 1.5f);
                a.Alerta = Billboard("Alerta" + k, Art.Alerta(), Art.Warn, 0.8f);
                a.Rotulo = Art.Outlined(Art.NewText(_rotulos, "Desgaste" + k, 26, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)), 2f);
                a.Rotulo.rectTransform.sizeDelta = new Vector2(120f, 36f);
                a.Rotulo.enabled = false;
                a.T.gameObject.SetActive(false);
                if (a.Sombra != null) a.Sombra.gameObject.SetActive(false);
            }

            Veiculo r = NovoVeiculo("rebocador", -1, 2.2f, 0.35f, Art.Hex(0xF2C14E), 1.5f);
            r.T.SetPositionAndRotation(ReboqueVaga, Quaternion.Euler(0f, -90f, 0f));
            Sombra(r);
            for (int sv = 1; sv < 4; sv++) NoServico(sv, 1);
            _escada = NaRota("escada_embarque", 1.3f, 0.7f, Art.Hex(0xE5E8E8), 2.3f, EscadaRota);
            _onibus = NaRota("onibus_patio", 1.5f, 0.9f, Art.Hex(0x3FA7D6), 3.2f, OnibusRota);
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

        /// <summary>Veiculo com o pivo no eixo traseiro (`eixo` = do eixo ao centro da malha, em m).</summary>
        Veiculo NovoVeiculo(string malha, int servico, float vel, float eixo, Color cor, float comp)
        {
            var v = new Veiculo { Servico = servico, Vel = vel, Comp = comp };
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
            _veiculos.Add(v);
            return v;
        }

        /// <summary>Veiculo de um servico na vaga `vaga` da coluna dele, de frente para o norte.</summary>
        Veiculo NoServico(int sv, int vaga)
        {
            Veiculo v = NovoVeiculo(VeiculoMalha[sv], sv, 2.4f, VeiculoEixo[sv], VeiculoCor[sv], VeiculoComp[sv]);
            v.Vaga = vaga;
            v.T.SetPositionAndRotation(V(ColunaX[sv], VagaZ[vaga]), Quaternion.identity);
            Sombra(v);
            return v;
        }

        /// <summary>Escada ou onibus: fica no comeco da rota, de frente para ela.</summary>
        Veiculo NaRota(string malha, float vel, float eixo, Color cor, float comp, Vector3[] rota)
        {
            Veiculo v = NovoVeiculo(malha, 0, vel, eixo, cor, comp);
            v.Rota = rota;
            v.T.SetPositionAndRotation(rota[0], Quaternion.LookRotation(rota[1] - rota[0], Vector3.up));
            Sombra(v);
            return v;
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
            s = Mathf.Clamp(s, 0f, t.Total);
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

        /// <summary>Pouso de oeste para leste (planeio, arredondamento e toque), rolagem freando, saida pela ligacao leste e taxi ate o estande
        /// dele, de nariz para o terminal.</summary>
        void Pousa(Aviao a)
        {
            float x = EstandeX[a.Id], z = ZEstande(a);
            Trecho ar = Caminho(0f, 10f, V(-34f, PistaZ, 9f), V(-16f, PistaZ, 3.4f), V(-8f, PistaZ, 1.0f), V(-4f, PistaZ, 0.22f), V(0f, PistaZ));
            Trecho rola = Caminho(0f, 2.6f, V(0f, PistaZ), V(21f, PistaZ));
            rola.F = 2.2f; rola.Sai = 1.3f;   // o reverso segura: sai da rolagem na velocidade da curva da ligacao
            Trecho taxi = Caminho(RaioAviao, 2.6f, V(21f, PistaZ), V(26f, PistaZ), V(26f, TaxiZ), V(x, TaxiZ), V(x, z));
            taxi.A = 0.4f; taxi.F = 0.6f; taxi.Para = true;
            a.V = 10f; a.QuerPartir = false;
            a.Trechos.Enqueue(ar); a.Trechos.Enqueue(rola); a.Trechos.Enqueue(taxi);
        }

        /// <summary>Recuo de re, com a cauda virando para `lado` (+1 = leste, o nariz fica para oeste), parado na taxiway para o rebocador sair.</summary>
        Trecho Recuo(float x, float z, bool reboque, float lado)
        {
            Trecho t = Caminho(3f, 0.9f, V(x, z), V(x, TaxiZ), V(x + 3f * lado, TaxiZ));
            t.Re = t.Para = true; t.A = 0.3f; t.F = 0.4f; t.Reboque = reboque;
            t.PausaDepois = reboque ? 8f : 2f;
            return t;
        }

        void Parte(Aviao a)
        {
            float x = EstandeX[a.Id];
            a.Trechos.Enqueue(Recuo(x, ZEstande(a), a.Id == 0, 1f));
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

        /// <summary>Do estande ao hangar: recua com o nariz para leste e segue reto pela taxiway ate dentro da porta.</summary>
        void ParaHangar(Aviao a)
        {
            float x = EstandeX[a.Id];
            a.Trechos.Enqueue(Recuo(x, ZEstande(a), a.Id == 0, -1f));
            Trecho taxi = Caminho(RaioAviao, 2.6f, V(x - 3f, TaxiZ), V(HangarEm.x, TaxiZ));
            taxi.A = 0.4f; taxi.F = 0.6f; taxi.Para = true;
            a.Trechos.Enqueue(taxi);
        }

        /// <summary>Do hangar ao estande: sai de re ate a cauda entrar na ligacao, desce de frente e taxia para oeste ate o estande.</summary>
        void DoHangar(Aviao a)
        {
            float x = EstandeX[a.Id];
            Trecho re = Caminho(3f, 1.2f, V(HangarEm.x, TaxiZ), V(LigacaoX, TaxiZ), V(LigacaoX, TaxiZ + 3.4f));
            re.Re = re.Para = true; re.A = 0.3f; re.F = 0.4f;
            a.Trechos.Enqueue(re);
            Trecho taxi = Caminho(RaioAviao, 2.6f, V(LigacaoX, TaxiZ + 3.4f), V(LigacaoX, TaxiZ), V(x, TaxiZ), V(x, ZEstande(a)));
            taxi.A = 0.4f; taxi.F = 0.6f; taxi.Para = true;
            a.Trechos.Enqueue(taxi);
        }

        // ------------------------------------------------------------------ quadro a quadro

        void Patio(Sim s, float dt)
        {
            for (int k = 0; k < _avioes.Length; k++) AviaoPasso(_avioes[k], s.Avioes[k], s, dt);
            ServicosPasso(s);
            for (int i = 0; i < _veiculos.Count; i++) VeiculoPasso(_veiculos[i], dt);
            DiagPasso(dt);
        }

        /// <summary>O aviao parou o trecho: decide o proximo pelo estado do nucleo (nao ha lista de eventos a repor: reabrir o jogo cai aqui).</summary>
        void Decide(Aviao a, TA.Core.Aviao c, Sim s)
        {
            if (a.Fase == Fase.Chegando) a.Fase = Fase.NoPatio;
            else if (a.Fase == Fase.Saindo) a.Fase = Fase.Fora;
            else if (a.Fase == Fase.IndoHangar) a.Fase = Fase.Hangar;
            float hangar = c.Hangar * Sim.Step;
            switch (a.Fase)
            {
                case Fase.Fora:
                    if (c.NoPatio) { NoEstande(a); a.Fase = Fase.NoPatio; }
                    else if (c.Hangar > 0) { a.T.SetPositionAndRotation(HangarEm, Quaternion.Euler(0f, 90f, 0f)); a.Fase = Fase.Hangar; }
                    else if (c.Chega >= 0 && (c.Chega - s.Steps) * Sim.Step <= PousoSeg[a.Id]) { Pousa(a); a.Fase = Fase.Chegando; a.S = 0f; }
                    break;
                case Fase.Hangar:
                    if (c.NoPatio || hangar <= VoltaHangarSeg) { DoHangar(a); a.Fase = Fase.Chegando; a.S = 0f; a.V = 0f; }
                    break;
                case Fase.NoPatio:
                    bool revisa = hangar > VoltaHangarSeg;
                    if (!revisa && !a.QuerPartir) break;
                    if (a.Id == 1 && (_escada.Lida == Lida.Indo || _escada.Lida == Lida.Servindo)) break;   // a escada sai antes
                    if (a.Id != 0 || _reboque == Reboque.Pronto)
                    {
                        if (a.Id == 0) _reboque = Reboque.Preso;
                        if (revisa) ParaHangar(a); else Parte(a);
                        a.Fase = revisa ? Fase.IndoHangar : Fase.Saindo; a.QuerPartir = false; a.S = 0f; a.V = 0f;
                    }
                    else if (_reboque == Reboque.Vago) ReboqueVai();   // espera o rebocador encostar no nariz
                    break;
            }
        }

        /// <summary>Abriu o jogo com o aviao no patio: ja esta no estande.</summary>
        void NoEstande(Aviao a)
        {
            a.T.SetPositionAndRotation(V(EstandeX[a.Id], ZEstande(a)), Quaternion.Euler(0f, 180f, 0f));
            a.QuerPartir = false;
        }

        void AviaoPasso(Aviao a, TA.Core.Aviao c, Sim s, float dt)
        {
            if (a.Trechos.Count == 0) Decide(a, c, s);
            bool visivel = a.Fase != Fase.Fora || a.Trechos.Count > 0;
            a.T.gameObject.SetActive(visivel);
            if (a.Sombra != null) a.Sombra.gameObject.SetActive(visivel && a.T.position.y < 5f);   // no alto, a sombra fica longe e some
            if (visivel)
            {
                float vAnt = a.V, yawAnt = a.T.eulerAngles.y;
                if (a.Pausa > 0f) a.Pausa -= dt;
                else if (a.Trechos.Count > 0)
                {
                    Trecho t = a.Trechos.Peek();
                    a.V = Passo(a.V, t, a.S, Freia(a, t) ? 0f : t.V, t.Para || t.Sai >= 0f ? t.Total - a.S : 1e4f, LatAviao, dt);
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
                    }
                }
                if (dt > 0f) Inclina(a.Malha, a.Base, (a.V - vAnt) / dt, a.V * Mathf.DeltaAngle(yawAnt, a.T.eulerAngles.y) * Mathf.Deg2Rad / dt, dt, ref a.Pitch, ref a.Roll, 0.5f);
                Vector3 p = a.T.position;
                if (a.Sombra != null)
                {
                    a.Sombra.position = new Vector3(p.x + 0.25f + p.y * 0.35f, 0.015f, p.z + p.y * 0.35f);   // a luz vem do alto a esquerda
                    a.Sombra.rotation = Quaternion.Euler(0f, a.T.eulerAngles.y, 0f);
                }
            }
            Selos(a, c, s, visivel);
        }

        /// <summary>Outro aviao na frente, no chao e no mesmo sentido: um atraso do nucleo pode junta-los na taxiway; quem vem atras espera.</summary>
        bool Freia(Aviao a, Trecho t)
        {
            if (t.V > 2.7f || t.Re) return false;
            foreach (Aviao o in _avioes)
            {
                if (o == a || !o.T.gameObject.activeSelf || o.T.position.y > 0.5f) continue;
                Vector3 d = o.T.position - a.T.position;
                float frente = Vector3.Dot(d, a.T.forward);
                if (frente > 0f && frente < 8.5f && Mathf.Abs(Vector3.Dot(d, a.T.right)) < 2.5f && Vector3.Dot(o.T.forward, a.T.forward) > 0f) return true;
            }
            return false;
        }

        /// <summary>Em cima do aviao (ou do hangar): o icone do servico com o anel de progresso; sem servico, o aviao na cor do voo. O aviso de
        /// desgaste (a partir de 80%, GDD §5.12) fica ao lado, com o numero.</summary>
        void Selos(Aviao a, TA.Core.Aviao c, Sim s, bool visivel)
        {
            bool parado = visivel && (a.Fase == Fase.NoPatio || a.Fase == Fase.Hangar || a.Fase == Fase.Chegando && a.T.position.y < 0.5f);
            a.CheckTotal = c.Check > 0 ? Mathf.Max(a.CheckTotal, c.Check) : 0;
            Sprite icone = Art.Aviao();
            float frac = -1f;
            if (a.Fase == Fase.Hangar || c.Hangar > 0) { icone = Art.Chave(); frac = 1f - c.Hangar * Sim.Step / s.T.HangarSeg; }
            else if (c.Check > 0) { icone = Art.Chave(); frac = 1f - c.Check / (float)a.CheckTotal; }
            else
                for (int sv = 0; sv < 4; sv++)
                    if (c.Fazendo[sv] || (frac < 0f && c.Falta[sv] > 0))
                    {
                        icone = Art.IconeServico(sv);
                        frac = 1f - c.Falta[sv] / (float)s.ServicoPassos((Servico)sv);
                        if (c.Fazendo[sv]) break;
                    }
            Vector3 ps = (a.Fase == Fase.Hangar ? HangarEm : a.T.position) + Vector3.up * (a.Fase == Fase.Hangar ? 6.4f : 4.4f);
            Quaternion q = _cam.transform.rotation;
            int ordem = Ordem(ps) + 9;
            a.Selo.enabled = parado;
            a.Anel.enabled = parado && frac >= 0f;
            if (parado)
            {
                a.Selo.sprite = icone;
                a.Selo.color = Art.VooCor[a.Id];
                a.Selo.transform.SetPositionAndRotation(ps, q);
                a.Selo.sortingOrder = ordem;
            }
            if (a.Anel.enabled)
            {
                a.Anel.sprite = Art.Arc(frac);
                a.Anel.transform.SetPositionAndRotation(ps, q);
                a.Anel.sortingOrder = ordem + 1;
            }
            bool aviso = parado && c.Desgaste >= s.T.DesgasteAviso;
            a.Alerta.enabled = a.Rotulo.enabled = aviso;
            if (aviso)
            {
                Vector3 pa = ps + _cam.transform.right * 1.2f;
                a.Alerta.color = c.Pendente ? Art.Bad : Art.Warn;
                a.Alerta.transform.SetPositionAndRotation(pa, q);
                a.Alerta.sortingOrder = ordem + 1;
                a.Rotulo.text = Textos.Porcento(c.Desgaste);
                a.Rotulo.color = a.Alerta.color;
                Coloca(a.Rotulo.rectTransform, pa - _cam.transform.up * 0.75f);
            }
        }

        /// <summary>Aviao no ponto da tela (-1 = nenhum): o selo, com o alvo minimo de toque, ou o corpo dele no chao.</summary>
        int AviaoEm(Vector2 tela)
        {
            Vector2 c = Chao(tela);
            float r = Mathf.Max(Dp.Px(Dp.AlvoMinimoDp) * 0.5f, 1f);
            foreach (Aviao a in _avioes)
            {
                if (!a.Selo.enabled) continue;
                Vector3 p = a.Fase == Fase.Hangar ? HangarEm : a.T.position;
                if (Vector2.Distance(Tela(a.Selo.transform.position), tela) <= r || (Mathf.Abs(c.x - p.x) < 1.8f && Mathf.Abs(c.y - p.z) < a.Nariz)) return a.Id;
            }
            return -1;
        }

        // ------------------------------------------------------------------ rebocador

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

        /// <summary>Solto na taxiway: recua fazendo a curva para o norte, cruza a taxiway para o sul, atravessa o estande vazio, passa na frente
        /// das ruas de servico e volta para a vaga (de frente para oeste): nenhum giro no lugar.</summary>
        void RebocadorVolta()
        {
            Veiculo r = _veiculos[0];
            Vector3 p = r.T.position, tras = -r.T.forward, fim = p + tras * 1.6f + Vector3.forward * 1.6f;
            Trecho re = Caminho(1.4f, 1.1f, p, p + tras * 1.6f, fim);
            re.Re = re.Para = true; re.A = 0.6f; re.F = 0.7f;
            Trecho vai = Caminho(1.4f, 2.2f, fim, new Vector3(fim.x, 0f, 12.4f), new Vector3(11.8f, 0f, 12.4f), new Vector3(11.8f, 0f, 9.7f), new Vector3(15.9f, 0f, 9.7f), new Vector3(15.9f, 0f, ReboqueVaga.z), ReboqueVaga);
            vai.A = 0.7f; vai.F = 0.8f; vai.Para = true;
            r.Rotas.Enqueue(re); r.Rotas.Enqueue(vai);
            r.S = 0f;
            _reboque = Reboque.Voltando;
        }

        // ------------------------------------------------------------------ veiculos de servico

        /// <summary>Liga os veiculos ao nucleo: quem terminou volta; o servico que comecou (Aviao.Fazendo) chama o veiculo dele.</summary>
        void ServicosPasso(Sim s)
        {
            foreach (Veiculo v in _veiculos)
            {
                if (v.Servico < 0) continue;
                if (v.Lida == Lida.Servindo && !Ativo(v, s)) Volta(v);
                else if (v.Lida == Lida.Parado && v.Rota == null && v.Vaga == 0 && VagaNorteLivre(v)) Avanca(v);
            }
            for (int k = 0; k < _avioes.Length; k++)
            {
                if (_avioes[k].Fase != Fase.NoPatio) continue;
                TA.Core.Aviao c = s.Avioes[k];
                if (c.Fazendo[0] && k == 1) { Vai(_escada, 1); Vai(_onibus, 1); }   // no estande G o desembarque e pela ponte
                for (int sv = 1; sv < 4; sv++) if (c.Fazendo[sv]) Chama(sv, k, s);
            }
        }

        /// <summary>O servico continua: a escada fica ate o aviao sair; os outros, enquanto o nucleo faz o servico.</summary>
        bool Ativo(Veiculo v, Sim s)
        {
            Aviao a = _avioes[v.Aviao];
            if (a.Fase != Fase.NoPatio) return false;
            if (v == _escada) return !a.QuerPartir && s.Avioes[v.Aviao].Hangar == 0;
            return s.Avioes[v.Aviao].Fazendo[v.Servico];
        }

        void Chama(int sv, int k, Sim s)
        {
            Veiculo livre = null;
            int frota = 0;
            foreach (Veiculo v in _veiculos)
            {
                if (v.Servico != sv) continue;
                frota++;
                if (v.Aviao == k) return;
                if (v.Lida == Lida.Parado && (livre == null || v.Vaga > livre.Vaga)) livre = v;
            }
            if (livre == null && frota < s.Veiculos((Servico)sv))
            {
                livre = NoServico(sv, 0);   // o +1 veiculo da Capacidade
                if (VagaNorteLivre(livre)) { livre.Vaga = 1; livre.T.position = V(ColunaX[sv], VagaZ[1]); }
            }
            if (livre != null) Vai(livre, k);
        }

        void Vai(Veiculo v, int k)
        {
            if (v.Aviao == k || v.Lida != Lida.Parado) return;
            v.Aviao = k; v.Lida = Lida.Indo; v.S = 0f;
            Trecho t;
            if (v.Rota != null) t = Caminho(RaioVeic, v.Vel, v.Rota);
            else
            {
                float x = ColunaX[v.Servico], f = FaixaX[k];
                t = Caminho(RaioVeic, v.Vel, V(x, VagaZ[v.Vaga]), V(x, RuaNorte), V(f, RuaNorte), V(f, ParadaZ[v.Servico]));
            }
            t.A = 0.7f; t.F = 0.9f; t.Para = true;
            v.Rotas.Enqueue(t);
        }

        void Volta(Veiculo v)
        {
            int k = v.Aviao;
            v.Aviao = -1; v.Lida = Lida.Voltando; v.S = 0f;
            Trecho t;
            if (v.Rota != null)
            {
                var r = (Vector3[])v.Rota.Clone();
                Array.Reverse(r);
                t = Caminho(RaioVeic, v.Vel * 0.8f, r);
                t.Re = true;
            }
            else
            {
                v.Vaga = VagaNorteLivre(v) ? 1 : 0;
                float x = ColunaX[v.Servico], f = FaixaX[k];
                t = Caminho(RaioVeic, v.Vel, V(f, ParadaZ[v.Servico]), V(f, RuaSul), V(x, RuaSul), V(x, VagaZ[v.Vaga]));
            }
            t.A = 0.7f; t.F = 0.9f; t.Para = true;
            v.Rotas.Enqueue(t);
        }

        /// <summary>A coluna e uma fila: sai primeiro quem esta ao norte, e quem ficou ao sul anda para la.</summary>
        void Avanca(Veiculo v)
        {
            float x = ColunaX[v.Servico];
            Trecho t = Caminho(0f, v.Vel, V(x, VagaZ[0]), V(x, VagaZ[1]));
            t.A = 0.7f; t.F = 0.9f; t.Para = true;
            v.Vaga = 1; v.Lida = Lida.Voltando; v.S = 0f;
            v.Rotas.Enqueue(t);
        }

        bool VagaNorteLivre(Veiculo v)
        {
            foreach (Veiculo o in _veiculos)
                if (o != v && o.Servico == v.Servico && o.Rota == null && o.Vaga == 1 && (o.Lida == Lida.Parado || o.Lida == Lida.Voltando)) return false;
            return true;
        }

        /// <summary>Quem vem atras espera (fila natural nas faixas). Conta a distancia de freada, para nao encostar.</summary>
        bool FrenteLivre(Veiculo v, Trecho t)
        {
            Vector3 f = t.Re ? -v.T.forward : v.T.forward, c = Centro(v), lado = new Vector3(f.z, 0f, -f.x);
            float freada = v.V * v.V / (2f * t.F);
            foreach (Veiculo o in _veiculos)
            {
                if (o == v) continue;
                Vector3 d = Centro(o) - c;
                float frente = Vector3.Dot(d, f);
                if (frente > 0f && frente < (v.Comp + o.Comp) * 0.5f + 0.05f + freada && Mathf.Abs(Vector3.Dot(d, lado)) < 1.2f) return false;
            }
            return true;
        }

        static Vector3 Centro(Veiculo v) => v.T.position + v.T.forward * (v.Comp * 0.4f);

        void VeiculoPasso(Veiculo v, float dt)
        {
            float vAnt = v.V, yawAnt = v.T.eulerAngles.y;
            bool reboque = v.Servico < 0;
            if (reboque && _reboque == Reboque.Preso) return;
            if (v.Rotas.Count == 0)
            {
                if (reboque)
                {
                    if (_reboque == Reboque.Indo) _reboque = Reboque.Pronto;
                    else if (_reboque == Reboque.Voltando) _reboque = Reboque.Vago;
                }
                else if (v.Lida == Lida.Indo) v.Lida = Lida.Servindo;
                else if (v.Lida == Lida.Voltando) v.Lida = Lida.Parado;
                v.V = 0f;
            }
            else
            {
                Trecho t = v.Rotas.Peek();
                v.V = Passo(v.V, t, v.S, reboque || FrenteLivre(v, t) ? t.V : 0f, t.Para ? t.Total - v.S : 1e4f, LatVeic, dt);
                v.S += v.V * Mathf.Max(0f, dt);
                Pose(v.T, t, v.S);
                if (v.S >= t.Total - 0.01f) { v.Rotas.Dequeue(); v.S = 0f; v.V = 0f; }
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

        /// <summary>Game: o nucleo mandou o aviao sair (o estado dele ja diz o resto, em Decide).</summary>
        void EventoPatio(Evento e)
        {
            if (e.Tipo == Ev.Decolou) _avioes[e.A].QuerPartir = true;
        }
    }
}
