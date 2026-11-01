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
    ///    nariz no G), o taxi, a corrida e a decolagem; o hangar leva o aviao pela taxiway ate a frente da porta (a porta da malha e menor que a
    ///    envergadura: ele para no patio do hangar) e o traz de volta antes de o nucleo liberar.
    ///  - Cruzamentos medidos com -diag e client/tools/diag_caixas.py: a taxiway passa atras das caudas e longe da pista com a asa inteira.
    ///  - Os veiculos estao em Mundo.Veiculos.cs.
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
        const float PistaZ = 30.2f, TaxiZ = 24.6f, NarizZ = 11.2f, RaioAviao = 3.4f, RaioVeic = 1.5f;
        const float LatAviao = 0.5f, LatVeic = 0.8f;   // aceleracao lateral maxima (m/s2): limita a velocidade na curva
        static readonly float[] EstandeX = { 11.4f, 22.5f };
        /// <summary>Linha do nariz em cada estande: o R recua 0,5 m para os veiculos caberem entre a rua e a asa baixa do jato.</summary>
        static readonly float[] NarizEm = { NarizZ, 11.7f };
        static readonly string[] AviaoMalha = { "aviao_regional", "aviao_narrow" };
        /// <summary>s do pouso ate parar no estande e da saida do hangar ate o estande (medidos com -diag): a tela comeca esse tempo antes do nucleo.</summary>
        static readonly float[] PousoSeg = { 24.4f, 19f };
        const float VoltaHangarSeg = 20f;
        /// <summary>Hangar no fim da taxiway, com a porta para oeste (a vista da camera; a boca da malha fica em +z, dai o yaw). O aviao para de nariz
        /// na frente da porta (HangarVaga), com a cauda fora do alcance de quem usa a ligacao; sai de re ate a ligacao leste e segue de frente: nenhum
        /// giro no lugar. A ligacao fica na linha do estande R: o jato desce reto.</summary>
        static readonly Vector3 HangarEm = new Vector3(41.9f, 0f, TaxiZ);
        const float HangarYaw = -90f, HangarEscala = 1.5f, HangarFrente = 37.1f, LigacaoX = 22.5f;
        /// <summary>Rebocador: vaga (na boca do estande G, de cauda para o nariz) e o ponto em que a barra encosta no nariz do aviao da ponte.</summary>
        static readonly Vector3 ReboqueVaga = new Vector3(11.4f, 0f, 9.3f), ReboqueNariz = new Vector3(11.4f, 0f, NarizZ - 0.5f);
        enum Reboque { Vago, Indo, Pronto, Preso, Voltando }

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

        readonly Aviao[] _avioes = { new Aviao { Id = 0 }, new Aviao { Id = 1 } };
        System.IO.StreamWriter _diag;
        float _diagT, _diagUlt;

        // ------------------------------------------------------------------ construcao

        void Patio3D()
        {
            Chao("Patio", new Vector3(28.65f, -0.06f, 16.9f), new Vector2(45.8f, 18f), Asfalto);
            Chao("Taxiway", new Vector3(14f, -0.055f, TaxiZ), new Vector2(46f, 2.6f), Asfalto);
            Chao("Pista", new Vector3(10f, -0.05f, PistaZ), new Vector2(46f, 3.8f), Art.Hex(0x4A4F5B));
            foreach (float x in new[] { -6f, LigacaoX }) Chao("Ligacao", new Vector3(x, -0.055f, (TaxiZ + PistaZ) * 0.5f), new Vector2(2.6f, PistaZ - TaxiZ), Asfalto);
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

            Veiculos3D();
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

        static float ZEstande(Aviao a) => NarizEm[a.Id] + a.Nariz;

        /// <summary>x do pivo de quem espera na frente da porta do hangar, com o nariz a 0,6 m da fachada.</summary>
        static float HangarVaga(Aviao a) => HangarFrente - 0.6f - a.Nariz;

        /// <summary>Taxi a partir de `p`, na ligacao, ate o estande dele, de nariz para o terminal: o jato desce reto, o regional segue a taxiway.</summary>
        static Trecho TaxiEstande(Aviao a, params Vector3[] p)
        {
            float x = EstandeX[a.Id];
            var q = new List<Vector3>(p);
            if (x != LigacaoX) { q.Add(V(LigacaoX, TaxiZ)); q.Add(V(x, TaxiZ)); }
            q.Add(V(x, ZEstande(a)));
            Trecho t = Caminho(RaioAviao, 2.6f, q.ToArray());
            t.A = 0.4f; t.F = 0.6f; t.Para = true;
            return t;
        }

        /// <summary>Pouso de oeste para leste (planeio, arredondamento e toque), rolagem freando, saida pela ligacao leste e taxi ate o estande.</summary>
        void Pousa(Aviao a)
        {
            Trecho ar = Caminho(0f, 10f, V(-34f, PistaZ, 9f), V(-16f, PistaZ, 3.4f), V(-8f, PistaZ, 1.0f), V(-4f, PistaZ, 0.22f), V(0f, PistaZ));
            Trecho rola = Caminho(0f, 2.6f, V(0f, PistaZ), V(LigacaoX - 5f, PistaZ));
            rola.F = 2.2f; rola.Sai = 1.3f;   // o reverso segura: sai da rolagem na velocidade da curva da ligacao
            a.V = 10f; a.QuerPartir = false;
            a.Trechos.Enqueue(ar); a.Trechos.Enqueue(rola); a.Trechos.Enqueue(TaxiEstande(a, V(LigacaoX - 5f, PistaZ), V(LigacaoX, PistaZ)));
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
            Trecho corre = Caminho(0f, 14f, V(-2f, PistaZ), V(12f, PistaZ));
            corre.A = 1.7f;
            a.Trechos.Enqueue(corre);
            Trecho sobe = Caminho(0f, 16f, V(12f, PistaZ), V(22f, PistaZ, 2.4f), V(36f, PistaZ, 8f), V(52f, PistaZ, 14f));   // passa por cima do hangar
            sobe.A = 1f;
            a.Trechos.Enqueue(sobe);
        }

        /// <summary>Do estande ao hangar: recua com o nariz para leste e segue reto pela taxiway ate a frente da porta.</summary>
        void ParaHangar(Aviao a)
        {
            float x = EstandeX[a.Id];
            a.Trechos.Enqueue(Recuo(x, ZEstande(a), a.Id == 0, -1f));
            Trecho taxi = Caminho(RaioAviao, 2.6f, V(x - 3f, TaxiZ), V(HangarVaga(a), TaxiZ));
            taxi.A = 0.4f; taxi.F = 0.6f; taxi.Para = true;
            a.Trechos.Enqueue(taxi);
        }

        /// <summary>Do hangar ao estande: sai de re ate a cauda entrar na ligacao, desce de frente e taxia para oeste ate o estande.</summary>
        void DoHangar(Aviao a)
        {
            Trecho re = Caminho(3f, 1.2f, V(HangarVaga(a), TaxiZ), V(LigacaoX, TaxiZ), V(LigacaoX, TaxiZ + 3.4f));
            re.Re = re.Para = true; re.A = 0.3f; re.F = 0.4f;
            a.Trechos.Enqueue(re);
            a.Trechos.Enqueue(TaxiEstande(a, V(LigacaoX, TaxiZ + 3.4f)));
        }

        // ------------------------------------------------------------------ quadro a quadro

        void Patio(Sim s, float dt)
        {
            for (int k = 0; k < _avioes.Length; k++) AviaoPasso(_avioes[k], s.Avioes[k], s, dt);
            ServicosPasso(s);
            for (int i = 0; i < _veiculos.Count; i++) VeiculoPasso(_veiculos[i], dt);
            _naFaixa.Clear();
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
                    else if (c.Hangar > 0) { a.T.SetPositionAndRotation(V(HangarVaga(a), TaxiZ), Quaternion.Euler(0f, 90f, 0f)); a.Fase = Fase.Hangar; }
                    else if (c.Chega >= 0 && (c.Chega - s.Steps) * Sim.Step <= PousoSeg[a.Id]) { Pousa(a); a.Fase = Fase.Chegando; a.S = 0f; }
                    break;
                case Fase.Hangar:
                    if (c.NoPatio || hangar <= VoltaHangarSeg) { DoHangar(a); a.Fase = Fase.Chegando; a.S = 0f; a.V = 0f; }
                    break;
                case Fase.NoPatio:
                    bool revisa = hangar > VoltaHangarSeg;
                    if (!revisa && !a.QuerPartir) break;
                    foreach (Veiculo v in _veiculos) if (v.Aviao == a.Id) return;   // quem esta encostado sai antes
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
            Vector3 ps = a.T.position + Vector3.up * 4.4f;
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
                Vector3 p = a.T.position;
                if (Vector2.Distance(Tela(a.Selo.transform.position), tela) <= r || (Mathf.Abs(c.x - p.x) < 1.8f && Mathf.Abs(c.y - p.z) < a.Nariz)) return a.Id;
            }
            return -1;
        }

        /// <summary>-diag arq.csv (so no -testsession): grava a cada ~0,1 s de jogo a pose de cada aviao e veiculo em movimento; o
        /// client/tools/diag_patio.py calcula escorregamento, velocidade, aceleracao e giro.</summary>
        public void Diag(string arq)
        {
            _diag = new System.IO.StreamWriter(arq) { AutoFlush = false };
            _diag.WriteLine("t,peca,x,y,z,yaw,sx,sy,sz");
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            for (int e = 0; e < Salas.Length; e++)
                for (int i = 0; i < 4; i++)
                    if (_mesa[e, i, 0] != null && _mesa[e, i, 0].activeSelf)
                    {
                        Vector2 b = e == (int)Estacao.Seguranca ? Em(e, 1f, SegA[i] + BeltX) : Balcao(e, i);
                        _diag.WriteLine(string.Format(inv, "0,balcao#{0}.{1},{2:0.000},0,{3:0.000},0", e, i, b.x, b.y));
                    }
            DiagCaixas(inv);
        }

        /// <summary>As caixas do client/tools/diag_caixas.py: a medida de cada peca que anda (movel#, com o centro relativo ao pivo em yaw 0) e cada
        /// peca parada do cenario (fixa#, com o centro no mundo). Le a malha, nao o Renderer: predio de outro nivel e sala fechada estao desligados.</summary>
        void DiagCaixas(System.IFormatProvider inv)
        {
            var anda = new List<Transform>();
            foreach (Aviao a in _avioes) anda.Add(a.T);
            foreach (Veiculo v in _veiculos) anda.Add(v.T);
            foreach (Transform t in anda)
            {
                Bounds b = Caixa(t);
                _diag.WriteLine(string.Format(inv, "0,movel#{0},{1:0.000},{2:0.000},{3:0.000},0,{4:0.000},{5:0.000},{6:0.000}", t.name, b.center.x, b.min.y, b.center.z, b.size.x, b.size.y, b.size.z));
            }
            var vistas = new HashSet<Transform>();
            foreach (MeshFilter f in _raiz.GetComponentsInChildren<MeshFilter>(true))
            {
                Transform t = f.name == "malha" ? f.transform.parent : f.transform;
                if (t.name.StartsWith("Sombra") || !vistas.Add(t) || anda.Exists(m => t.IsChildOf(m))) continue;
                Bounds b = Caixa(t);
                if (b.max.y + t.position.y < 0.1f) continue;   // pista, faixa, piso: sem altura, ninguem atravessa
                Vector3 c = t.position + Quaternion.Euler(0f, t.eulerAngles.y, 0f) * b.center;
                _diag.WriteLine(string.Format(inv, "0,fixa#{0},{1:0.000},{2:0.000},{3:0.000},{4:0.00},{5:0.000},{6:0.000},{7:0.000}", t.name, c.x, b.min.y + t.position.y, c.z, t.eulerAngles.y, b.size.x, b.size.y, b.size.z));
            }
        }

        /// <summary>Caixa do objeto em yaw 0, relativa ao pivo, pelos limites das malhas.</summary>
        static Bounds Caixa(Transform t)
        {
            Quaternion q = Quaternion.Euler(0f, -t.eulerAngles.y, 0f);
            var b = new Bounds();
            bool um = false;
            foreach (MeshFilter f in t.GetComponentsInChildren<MeshFilter>(true))
            {
                Bounds m = f.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = m.center + Vector3.Scale(m.extents, new Vector3((i & 1) * 2 - 1, (i >> 1 & 1) * 2 - 1, (i >> 2 & 1) * 2 - 1));
                    Vector3 p = q * (f.transform.TransformPoint(c) - t.position);
                    if (um) b.Encapsulate(p); else { b = new Bounds(p, Vector3.zero); um = true; }
                }
            }
            return b;
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
            foreach (KeyValuePair<int, Boneco> kv in _pax) DiagBoneco(inv, "pax#" + kv.Key, kv.Value);
            for (int k = 0; k < S.Staff.Count; k++) DiagBoneco(inv, "func#" + k, _func[k]);
            for (int i = 0; i < _equipe.Count; i++) DiagBoneco(inv, "equipe#" + i, _equipe[i]);
            for (int i = 0; i < _patio.Count; i++) DiagBoneco(inv, "ator#" + i, _patio[i].B);
            for (int i = 0; i < _andantes.Count; i++) DiagBoneco(inv, "trip#" + i, _andantes[i].B);
            DiagBoneco(inv, "mec#0", _mecanico);
        }

        /// <summary>Pessoa no -diag (client/tools/diag_pessoas.py): y = 1 andando; yaw = o rumo da folha, que e sempre um eixo do mundo.</summary>
        void DiagBoneco(System.IFormatProvider inv, string nome, Boneco b)
        {
            float yaw = b.Dir > 0f ? (b.Cima ? 90f : 180f) : (b.Cima ? 0f : 270f);
            _diag.WriteLine(string.Format(inv, "{0:0.00},{1},{2:0.000},{3},{4:0.000},{5:0}", _diagT, nome, b.Pos.x, b.Andou ? 1 : 0, b.Pos.y, yaw));
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
