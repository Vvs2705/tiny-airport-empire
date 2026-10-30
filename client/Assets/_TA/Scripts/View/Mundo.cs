using System;
using System.Collections.Generic;
using TA.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TA
{
    /// <summary>
    /// O aeroporto em 3D em tempo real (direcao do Vinicius em 29/10; GDD §5.13 e D-18). Le o Sim a cada quadro; nada aqui muda regra.
    ///  - Camera ortografica em angulo isometrico fixo (45 graus de cima, 45 de lado; nao gira), com pan (arrastar no vazio) e zoom (pinca,
    ///    roda do mouse). O enquadramento inicial e de perto, nas 4 salas: da para ler o uniforme; o patio e a pista ficam a um pan.
    ///  - Planta em escada, para o fluxo subir na tela em retrato: entrada -> check-in -> seguranca (leste) -> loja (norte) -> portao
    ///    (leste) -> ponte de embarque -> aviao no patio. Salas de 4 x 4 em corte (paredes baixas, sem teto), portas entre salas, fila
    ///    indiana entre fitas e os balcoes junto a parede de saida. Atras das salas, o predio do terminal; no patio, torre e hangar.
    ///  - Corpos rigidos = malhas do Tripo decimadas (client/tools/malhas_ta.py, em Resources/Malhas de Assets/_TA/ArtLocal, fora do git
    ///    pela licenca; GDD §10.2). Sem a pasta (clone do repositorio), as primitivas da v0.1 entram no lugar.
    ///  - Placa pequena no chao de cada sala (nome; "fechada" na loja sem gente) e, so quando ha problema, um selo curto do estado acima
    ///    da sala: os rotulos escuros da v0.1 tampavam o mundo (foto v01_telas).
    ///  - Pessoas (Mundo.Pessoas.cs) e patio com os avioes e veiculos (Mundo.Patio.cs).
    ///  - Toque na sala = atendimento manual; arrastar o funcionario (alvo de 48 dp no minimo, limiar em dp separado do toque, boneco
    ///    acima do dedo, salas de destino acesas) ou tocar nele = "mover para".
    /// </summary>
    public sealed partial class Mundo
    {
        // ------------------------------------------------------------------ planta (unidades de mundo = m; Y para cima, chao em Y = 0)

        public const float Lado = 4f;
        const float Elev = 45f, VelPax = 2.6f, Troca = 0.35f, EscalaFunc = 1f, PortaLarg = 0.9f;
        static readonly float[] Desloc = { -0.8f, 0.8f, -1.55f, 1.55f };   // balcoes ao longo da parede de saida, com o corredor no meio

        /// <summary>Sala de cada estacao: canto (x, z), porta de entrada e de saida (no meio da parede) e a direcao da saida (para fora).</summary>
        struct Sala { public Vector2 Min, In, Out, N, T; }

        static readonly Sala[] Salas =
        {
            Nova(0f, 0f, new Vector2(0f, 2f), new Vector2(4f, 2f), new Vector2(1f, 0f)),     // check-in: entra a oeste, sai a leste
            Nova(4f, 0f, new Vector2(4f, 2f), new Vector2(6f, 4f), new Vector2(0f, 1f)),     // seguranca: entra a oeste, sai ao norte
            Nova(4f, 4f, new Vector2(6f, 4f), new Vector2(8f, 6f), new Vector2(1f, 0f)),     // loja: entra ao sul, sai a leste
            Nova(8f, 4f, new Vector2(8f, 6f), new Vector2(10f, 8f), new Vector2(0f, 1f)),    // portao: entra a oeste, sai ao norte (ponte)
        };

        /// <summary>Malha de balcao de cada estacao e a escala dela (a loja e um quiosque grande: 0,6 cabe 1 por posto).</summary>
        static readonly string[] BalcaoMalha = { "balcao_checkin", "raio_x", "loja", "portao_embarque" };
        static readonly float[] BalcaoEscala = { 1f, 1f, 0.6f, 1f };
        /// <summary>Nivel de Velocidade a partir do qual o balcao troca pela malha `<malha>_n2` e `_n3` (Lotes 5 e 6): o upgrade muda o predio
        /// (GDD §5.13). Sem a malha daquele nivel, fica a do nivel abaixo.</summary>
        static readonly int[] NivelMalha = { 0, 2, 4 };
        static readonly Color ReservaCor = new Color(1f, 0.93f, 0.78f);
        /// <summary>Placa no chao, do lado de fora da parede mais perto da camera (onde nao ha outra sala).</summary>
        static readonly Vector2[] PlacaEm = { new Vector2(2.7f, -1.05f), new Vector2(6.7f, -1.05f), new Vector2(2.95f, 6.7f), new Vector2(10.7f, 2.95f) };

        static Sala Nova(float x, float z, Vector2 entra, Vector2 sai, Vector2 n) =>
            new Sala { Min = new Vector2(x, z), In = entra, Out = sai, N = n, T = new Vector2(n.y, n.x) };

        static readonly Vector2 Calcada = new Vector2(-2.6f, 2f), PonteFim = new Vector2(10f, 11.1f);

        /// <summary>Ponto da sala a `d` da parede de saida e `a` ao longo dela.</summary>
        static Vector2 Em(int e, float d, float a) => Salas[e].Out - Salas[e].N * d + Salas[e].T * a;
        static Vector2 Balcao(int e, int i) => Em(e, 1.0f, Desloc[i]);
        static Vector2 NoBalcao(int e, int i) => Em(e, 1.55f, Desloc[i]);
        static Vector2 AtrasBalcao(int e, int i) => Em(e, 0.45f, Desloc[i]);
        static Vector2 Centro(int e) => Salas[e].Min + new Vector2(Lado * 0.5f, Lado * 0.5f);
        /// <summary>Rumo (graus de yaw do Unity) da direcao `d` do chao: +z = 0.</summary>
        static float Rumo(Vector2 d) => Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;

        /// <summary>Lugar k da fila indiana: 5 fileiras de 12 paralelas aos balcoes, em serpentina (60 cabem: o estresse do K3).</summary>
        static Vector2 Fila(int e, int k)
        {
            int r = Mathf.Min(k / 12, 4), c = k % 12;
            float a = -1.7f + (r % 2 == 0 ? c : 11 - c) * 0.31f;
            return Em(e, 2.05f + r * 0.42f, a);
        }

        static int SalaEm(Vector2 p)
        {
            for (int e = 0; e < 4; e++)
            {
                Vector2 m = Salas[e].Min;
                if (p.x >= m.x && p.x <= m.x + Lado && p.y >= m.y && p.y <= m.y + Lado) return e;
            }
            return -1;
        }

        static Vector3 V3(Vector2 p, float y = 0f) => new Vector3(p.x, y, p.y);

        // ------------------------------------------------------------------ estado

        readonly Hud _h;
        readonly Game _g;
        readonly Camera _cam;
        readonly Transform _raiz;
        readonly RectTransform _rotulos;
        static MaterialPropertyBlock _mpb;
        static Material _mat, _matSombra;
        static readonly Dictionary<string, Material> _matMalha = new Dictionary<string, Material>();
        /// <summary>Um material para todas as primitivas (Legacy Diffuse, iluminado e leve; o Setup.Apply o poe no build), cor por
        /// MaterialPropertyBlock. Sem o shader (editor sem o Setup), fica o material padrao.</summary>
        static Material Material
        {
            get
            {
                if (_mat == null) { Shader sh = Shader.Find("Legacy Shaders/Diffuse"); if (sh != null) _mat = new Material(sh); }
                return _mat;
            }
        }
        readonly Dictionary<int, Boneco> _pax = new Dictionary<int, Boneco>();
        readonly Stack<Boneco> _livres = new Stack<Boneco>();
        readonly HashSet<int> _vistos = new HashSet<int>(), _embarcou = new HashSet<int>();
        readonly List<int> _sair = new List<int>();
        readonly Boneco[] _func = new Boneco[Sim.MaxFuncionarios];
        readonly int[] _colete = { -1, -1, -1 };
        readonly float[] _troca = new float[Sim.MaxFuncionarios], _falha = new float[Sim.MaxFuncionarios];
        readonly Estacao[] _rotaPara = new Estacao[Sim.MaxFuncionarios];
        readonly List<Vector2>[] _rota = { new List<Vector2>(), new List<Vector2>(), new List<Vector2>() };
        readonly Text[] _nome = new Text[Sim.MaxFuncionarios];
        readonly Renderer[] _piso = new Renderer[4];
        readonly Renderer[][] _moldura = new Renderer[4][];
        readonly GameObject[,,] _mesa = new GameObject[4, 4, 3];
        readonly SpriteRenderer[,] _arco = new SpriteRenderer[4, 4];
        readonly SpriteRenderer[] _mao = new SpriteRenderer[4];
        readonly TextMesh[] _placa = new TextMesh[4];
        readonly RectTransform[] _selo = new RectTransform[4];
        readonly Text[] _seloTexto = new Text[4];
        readonly Image[] _seloFundo = new Image[4];

        // camera e gesto
        Vector3 _alvo;
        float _tam = 6f;
        bool _enquadrado;
        string _vista;
        Rect _regiao;
        int _press = -2;
        Vector2 _pressPos, _ultimo;
        bool _arrasto, _pan;
        float _pinca;

        Sim S => _g.Sim;
        /// <summary>Funcionario sendo arrastado (-1 = nenhum).</summary>
        public int Arrastando => _arrasto ? _press : -1;

        static readonly Color CorParede = Art.Hex(0xD9DEE8), Fita = Art.Hex(0x2F6FE4), Asfalto = Art.Hex(0x5A5F6B), Grama = Art.Hex(0x7FB069);

        public Mundo(Hud h, Game g, RectTransform rotulos)
        {
            _h = h; _g = g; _rotulos = rotulos;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _cam = Object.FindAnyObjectByType<Camera>();
            _cam.orthographic = true;
            _cam.transform.rotation = Quaternion.Euler(Elev, 45f, 0f);   // o mesmo angulo das folhas (render_sprites.py, preset TA)
            _cam.nearClipPlane = 0.1f; _cam.farClipPlane = 300f;
            _cam.transparencySortMode = TransparencySortMode.Orthographic;
            var luz = new GameObject("Sol").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 0.72f;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);   // do alto a esquerda, como a luz gravada nas folhas (ART_BIBLE §2)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.54f, 0.58f);
            _raiz = new GameObject("Mundo").transform;
            Planta();
            for (int k = 0; k < _func.Length; k++)
            {
                _func[k] = NovoBoneco(FuncAtor[k]);
                _func[k].T.gameObject.SetActive(false);
                _nome[k] = Art.Outlined(Art.NewText(_rotulos, "Nome" + k, 24, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)), 2f);
                _nome[k].rectTransform.sizeDelta = new Vector2(160f, 32f);
                _nome[k].text = Textos.Funcionario[k];
            }
            GentePatio();
        }

        // ------------------------------------------------------------------ construcao da planta

        Renderer Bloco(string nome, PrimitiveType tipo, Vector3 centro, Vector3 tam, Color cor, Transform pai = null)
        {
            GameObject go = GameObject.CreatePrimitive(tipo);
            go.name = nome;
            // sem colisor: o projeto nao tem o modulo de fisica (a selecao e por conta, no chao em Y = 0)
            go.transform.SetParent(pai != null ? pai : _raiz, false);
            go.transform.localPosition = centro;
            go.transform.localScale = tam;
            Renderer r = go.GetComponent<Renderer>();
            if (Material != null) r.sharedMaterial = Material;
            Cor(r, cor);
            return r;
        }

        static void Cor(Renderer r, Color c)
        {
            _mpb.SetColor("_Color", c);
            r.SetPropertyBlock(_mpb);
        }

        /// <summary>Malha do Tripo (Resources/Malhas/nome.fbx + nome.png, pivo no chao, de frente para +z) num pai que gira em yaw; null se a
        /// pasta ArtLocal nao existe (quem chama desenha a primitiva). Material Legacy Diffuse com a textura, 1 por peca.</summary>
        Transform Peca(string nome, Vector3 pos, float yaw, float escala = 1f)
        {
            var src = Resources.Load<GameObject>("Malhas/" + nome);
            MeshFilter mf = src != null ? src.GetComponentInChildren<MeshFilter>() : null;
            if (mf == null || Material == null) return null;
            if (!_matMalha.TryGetValue(nome, out Material mat))
                _matMalha[nome] = mat = new Material(Material) { name = nome, mainTexture = Resources.Load<Texture2D>("Malhas/" + nome) };
            var pai = new GameObject(nome).transform;
            pai.SetParent(_raiz, false);
            pai.localPosition = pos;
            pai.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var go = new GameObject("malha");
            go.transform.SetParent(pai, false);
            go.transform.localRotation = mf.transform.rotation;   // o giro do import (FBX do Blender), relativo a raiz do modelo
            go.transform.localScale = mf.transform.lossyScale * escala;
            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return pai;
        }

        /// <summary>Sombra plana de uma malha: a mesma malha achatada no chao (TA/Sombra). Null sem o shader ou sem malha (primitivas).</summary>
        Transform SombraPlana(Transform t)
        {
            MeshFilter mf = t.GetComponentInChildren<MeshFilter>();
            if (_matSombra == null) { Shader sh = Shader.Find("TA/Sombra"); if (sh != null) _matSombra = new Material(sh); }
            if (mf == null || _matSombra == null || mf.transform.parent != t) return null;
            var pai = new GameObject("Sombra" + t.name).transform;
            pai.SetParent(_raiz, false);
            pai.localScale = new Vector3(1f, 0.002f, 1f);
            var go = new GameObject("malha");
            go.transform.SetParent(pai, false);
            go.transform.localRotation = mf.transform.localRotation;
            go.transform.localScale = mf.transform.localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = _matSombra;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return pai;
        }

        void Planta()
        {
            Bloco("Gramado", PrimitiveType.Cube, new Vector3(9f, -0.12f, 9f), new Vector3(90f, 0.1f, 90f), Grama);
            Bloco("Calcada", PrimitiveType.Cube, new Vector3(-1.6f, -0.06f, 2f), new Vector3(3.2f, 0.1f, 4.4f), Art.Hex(0xB8B2A6));
            Bloco("Rua", PrimitiveType.Cube, new Vector3(-4.4f, -0.07f, 4f), new Vector3(2.6f, 0.1f, 22f), Asfalto);
            Bloco("Saguao", PrimitiveType.Cube, new Vector3(6f, -0.08f, 4f), new Vector3(12.6f, 0.1f, 8.6f), Art.Hex(0xCFC7B5));
            for (int e = 0; e < 4; e++) Sala3D(e);
            // ponte de embarque, da porta norte do portao ate a porta do aviao
            Transform ponte = Peca("ponte_embarque", new Vector3(10f, 0f, 9.7f), 0f);
            if (ponte == null)
            {
                Bloco("Ponte", PrimitiveType.Cube, new Vector3(10f, 0.04f, 9.55f), new Vector3(0.8f, 0.08f, 3.1f), Art.Hex(0x9AA6BC));
                Bloco("PonteLadoE", PrimitiveType.Cube, new Vector3(9.58f, 0.3f, 9.55f), new Vector3(0.06f, 0.5f, 3.1f), Art.Hex(0x6E7A90));
                Bloco("PonteLadoD", PrimitiveType.Cube, new Vector3(10.42f, 0.3f, 9.55f), new Vector3(0.06f, 0.5f, 3.1f), Art.Hex(0x6E7A90));
            }
            // cenario (so com as malhas; sem elas o greybox da v0.1 nao tinha nada aqui)
            Peca("terminal_vale_verde", new Vector3(1.2f, 0f, 13.2f), 180f);
            Peca("torre_controle", new Vector3(26.5f, 0f, 6.5f), 225f);
            Peca("hangar_manutencao", new Vector3(30f, 0f, 13.5f), 270f);
            Peca("cadeiras_espera", new Vector3(-1.9f, 0f, 4.6f), 90f);
            Peca("cadeiras_espera", new Vector3(-1.9f, 0f, -0.6f), 90f);
            foreach (Vector2 v in new[] { new Vector2(-0.5f, 4.5f), new Vector2(-0.5f, -0.5f), new Vector2(8.5f, -0.5f), new Vector2(12.5f, 3.5f) })
                Peca("vaso_planta", V3(v), 0f);
            // arvores so nas bordas (esquerda, direita e atras da pista): na frente das salas tampariam a fila
            foreach (Vector2 v in new[] { new Vector2(-8f, 2f), new Vector2(-8.5f, 8f), new Vector2(-6f, 14.5f), new Vector2(15f, -2f), new Vector2(19f, 1f), new Vector2(0f, 27.5f), new Vector2(9f, 27.8f), new Vector2(18f, 27.5f) })
                Peca("arvore", V3(v), v.x * 37f);
            foreach (Vector2 v in new[] { new Vector2(6.8f, 10.2f), new Vector2(16.2f, 9.8f), new Vector2(25f, 10.2f) }) Peca("poste", V3(v), 45f, 0.75f);
            foreach (Vector2 v in new[] { new Vector2(7.3f, 14.6f), new Vector2(15.5f, 14.6f), new Vector2(17.1f, 14.6f), new Vector2(25f, 14.6f) }) Peca("cone", V3(v), 0f);
            Peca("conteiner_bagagem", new Vector3(16.3f, 0f, 9.7f), 0f);
            Peca("conteiner_bagagem", new Vector3(16.3f, 0f, 8.4f), 0f);
            Peca("biruta", new Vector3(31f, 0f, 19.8f), 0f);
            Peca("radar", new Vector3(29.2f, 0f, 4.4f), 0f);
            Patio3D();
        }

        void Sala3D(int e)
        {
            Sala s = Salas[e];
            Vector2 c = Centro(e);
            _piso[e] = Bloco("Piso" + e, PrimitiveType.Cube, new Vector3(c.x, -0.05f, c.y), new Vector3(Lado - 0.04f, 0.1f, Lado - 0.04f), Color.white);
            // moldura de estado no chao (normal/atencao/critico; destino aceso no arrasto)
            _moldura[e] = new Renderer[4];
            for (int i = 0; i < 4; i++)
            {
                bool hor = i < 2;
                float off = (i % 2 == 0 ? -1f : 1f) * (Lado * 0.5f - 0.08f);
                Vector3 p = hor ? new Vector3(c.x, 0.01f, c.y + off) : new Vector3(c.x + off, 0.01f, c.y);
                _moldura[e][i] = Bloco("Moldura" + e + i, PrimitiveType.Cube, p, hor ? new Vector3(Lado - 0.1f, 0.02f, 0.12f) : new Vector3(0.12f, 0.02f, Lado - 0.1f), Art.Good);
            }
            // paredes baixas (corte) com as portas de entrada e de saida
            Vector2 m = s.Min;
            Parede(new Vector2(m.x, m.y), new Vector2(m.x + Lado, m.y), s);                 // sul
            Parede(new Vector2(m.x, m.y + Lado), new Vector2(m.x + Lado, m.y + Lado), s);   // norte
            Parede(new Vector2(m.x, m.y), new Vector2(m.x, m.y + Lado), s);                 // oeste
            Parede(new Vector2(m.x + Lado, m.y), new Vector2(m.x + Lado, m.y + Lado), s);   // leste
            for (int i = 0; i < 4; i++)
            {
                Vector2 b = Balcao(e, i);
                Transform t = Peca(BalcaoMalha[e], V3(b), Rumo(-s.N), BalcaoEscala[e]);   // de frente para a fila
                if (t != null)
                {
                    _mesa[e, i, 0] = t.gameObject;
                    string ant = BalcaoMalha[e];
                    for (int n = 1; n < NivelMalha.Length; n++)
                    {
                        string nome = BalcaoMalha[e] + "_n" + (n + 1);
                        Transform tn = Peca(nome, V3(b), Rumo(-s.N), BalcaoEscala[e]);
                        if (tn != null) ant = nome;
                        else if ((tn = Peca(ant, V3(b), Rumo(-s.N), BalcaoEscala[e] * (1f + 0.08f * n))) != null)   // reserva ate o export chegar
                            foreach (Renderer r in tn.GetComponentsInChildren<Renderer>()) Cor(r, ReservaCor);   // (ASSETS §2.6): maior e dourado
                        _mesa[e, i, n] = tn != null ? tn.gameObject : null;
                    }
                }
                else
                {
                    bool longoX = Mathf.Abs(s.N.y) > 0.5f;
                    _mesa[e, i, 0] = Bloco("Balcao" + e + i, PrimitiveType.Cube, new Vector3(b.x, 0.25f, b.y), longoX ? new Vector3(0.55f, 0.5f, 0.3f) : new Vector3(0.3f, 0.5f, 0.55f), Art.Paper).gameObject;
                }
                _arco[e, i] = Billboard("Arco" + e + i, Art.Arc(0f), Art.Good, 0.32f);
            }
            // fitas da fila: postes nas pontas das fileiras
            for (int r = 0; r < 5; r++)
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    Vector2 p = Em(e, 2.05f + r * 0.42f + 0.21f, lado * 1.85f);
                    Bloco("Poste", PrimitiveType.Cylinder, new Vector3(p.x, 0.2f, p.y), new Vector3(0.05f, 0.2f, 0.05f), Fita);
                }
            for (int r = 0; r < 4; r++)
            {
                Vector2 p = Em(e, 2.05f + r * 0.42f + 0.21f, (r % 2 == 0 ? 0.25f : -0.25f));
                bool longoX = Mathf.Abs(s.T.x) > 0.5f;
                Bloco("Fita", PrimitiveType.Cube, new Vector3(p.x, 0.32f, p.y), longoX ? new Vector3(3.2f, 0.03f, 0.02f) : new Vector3(0.02f, 0.03f, 3.2f), Fita);
            }
            _mao[e] = Billboard("Toque" + e, Art.Mao(), Art.BoardInk, 0.6f);
            Placa(e);
        }

        /// <summary>Parede baixa de a ate b, com vao de porta onde estiver a entrada ou a saida da sala.</summary>
        void Parede(Vector2 a, Vector2 b, Sala s)
        {
            Vector2 d = (b - a).normalized;
            float len = Vector2.Distance(a, b);
            var cortes = new List<float>();
            foreach (Vector2 porta in new[] { s.In, s.Out })
            {
                float t = Vector2.Dot(porta - a, d);
                if (t > 0f && t < len && Vector2.Distance(a + d * t, porta) < 0.01f) cortes.Add(t);
            }
            cortes.Sort();
            float ini = 0f;
            foreach (float t in cortes) { Muro(a, d, ini, t - PortaLarg * 0.5f); ini = t + PortaLarg * 0.5f; }
            Muro(a, d, ini, len);
        }

        void Muro(Vector2 a, Vector2 d, float t0, float t1)
        {
            if (t1 - t0 < 0.05f) return;
            Vector2 c = a + d * ((t0 + t1) * 0.5f);
            float l = t1 - t0;
            bool x = Mathf.Abs(d.x) > 0.5f;
            Bloco("Parede", PrimitiveType.Cube, new Vector3(c.x, 0.17f, c.y), x ? new Vector3(l + 0.06f, 0.34f, 0.08f) : new Vector3(0.08f, 0.34f, l + 0.06f), CorParede);
        }

        /// <summary>Placa no chao (faixa na cor do setor com o nome deitado, lido de frente pela camera) e o selo do estado, que so aparece
        /// em atencao ou critico.</summary>
        void Placa(int e)
        {
            Vector3 p = V3(PlacaEm[e], 0.012f);
            Vector3 longe = new Vector3(_cam.transform.forward.x, 0f, _cam.transform.forward.z).normalized;   // "para cima" na tela
            Bloco("Placa" + e, PrimitiveType.Cube, p, new Vector3(2.3f, 0.02f, 0.5f), Art.EstacaoCor[e]).transform.rotation = Quaternion.LookRotation(longe, Vector3.up);
            var go = new GameObject("PlacaNome" + e);
            go.transform.SetParent(_raiz, false);
            go.transform.SetPositionAndRotation(p + Vector3.up * 0.02f, Quaternion.LookRotation(Vector3.down, longe));
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = Art.Font();
            tm.fontSize = 72;
            tm.characterSize = 0.045f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = Art.Ink;
            tm.text = Textos.Estacao[e];
            go.GetComponent<MeshRenderer>().sharedMaterial = Art.Font().material;
            _placa[e] = tm;

            RectTransform r = Art.Node(_rotulos, "Selo" + e, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            r.sizeDelta = new Vector2(250f, 40f);
            _selo[e] = r;
            _seloFundo[e] = Art.Panel(r, "Fundo", Art.A(Art.Dark, 0.7f), Vector2.zero, Vector2.one);
            _seloFundo[e].raycastTarget = false;
            Art.Img(r, "Icone", Art.Alerta(), Color.white, new Vector2(0.02f, 0.1f), new Vector2(0.15f, 0.9f)).raycastTarget = false;
            _seloTexto[e] = Art.Outlined(Art.NewText(r, "Estado", 20, new Vector2(0.17f, 0f), new Vector2(0.98f, 1f), TextAnchor.MiddleLeft), 2f);
        }

        SpriteRenderer Billboard(string nome, Sprite s, Color c, float tam)
        {
            SpriteRenderer r = Art.NewSprite(_raiz, nome, s, c, 0, Vector2.zero, new Vector2(tam, tam));
            r.enabled = false;
            return r;
        }

        // ------------------------------------------------------------------ quadro a quadro

        /// <summary>Hud.LateUpdate: camera, gestos, passageiros, funcionarios, salas, patio e rotulos. `dt` = s de jogo do quadro (0 parado).</summary>
        public void Atualiza(float dt, Rect regiao)
        {
            _regiao = regiao;
            if (!_enquadrado && regiao.height > 10f) Enquadra();
            Entrada();
            Posiciona();
            Sim s = S;
            SincronizaPax(s, dt);
            Funcionarios(s, dt);
            Salas3D(s);
            Patio(s, dt);
            AndaPatio(dt);
        }

        /// <summary>Enquadramento inicial, de perto: as 4 salas e a boca da ponte na faixa entre o quadro e a barra de baixo (o patio e a
        /// pista ficam a um pan; a pinca afasta).</summary>
        void Enquadra()
        {
            Vector3 dir = _cam.transform.right, up = _cam.transform.up;
            var pts = new[] { new Vector3(-0.6f, 0f, 0f), new Vector3(8f, 0f, -0.6f), new Vector3(1.6f, 0f, -1.6f), new Vector3(12.4f, 0f, 4f), new Vector3(0f, 1f, 4.4f), new Vector3(4f, 1f, 8.4f), new Vector3(12f, 1f, 8f), new Vector3(10f, 1f, 10f) };
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            foreach (Vector3 p in pts)
            {
                float x = Vector3.Dot(p, dir), y = Vector3.Dot(p, up);
                x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
            }
            float upp = Mathf.Max((x1 - x0) / _regiao.width, (y1 - y0) / _regiao.height);
            _tam = Screen.height * upp * 0.5f;
            Vector3 centro = dir * ((x0 + x1) * 0.5f) + up * ((y0 + y1) * 0.5f);
            Vector2 off = _regiao.center - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            _alvo = NoChao(centro - dir * off.x * upp - up * off.y * upp);
            _enquadrado = true;
            if (_vista == "perto") { _alvo += new Vector3(-1.5f, 0f, -1.2f); _tam *= 0.55f; }
            else if (_vista == "patio") { _alvo = new Vector3(10.5f, 0f, 10.5f); _tam = 9.5f; }
            else if (_vista == "geral") { _alvo = new Vector3(11f, 0f, 11f); _tam = 15f; }
        }

        /// <summary>Game (-vista, so no -testsession): enquadramento das fotos e do -record (perto = zoom nas salas; patio = salas, estandes
        /// e a ponta da pista; geral = tudo).</summary>
        public void Vista(string v) { _vista = v; _enquadrado = false; }

        /// <summary>Projeta um ponto no chao (Y = 0) ao longo da direcao da camera.</summary>
        Vector3 NoChao(Vector3 p)
        {
            Vector3 f = _cam.transform.forward;
            return p - f * (p.y / f.y);
        }

        void Posiciona()
        {
            _tam = Mathf.Clamp(_tam, 2.2f, 16f);
            _alvo.x = Mathf.Clamp(_alvo.x, -8f, 32f);
            _alvo.z = Mathf.Clamp(_alvo.z, -4f, 26f);
            _cam.orthographicSize = _tam;
            _cam.transform.position = _alvo - _cam.transform.forward * 120f;
        }

        Vector2 Chao(Vector2 tela)
        {
            Ray r = _cam.ScreenPointToRay(new Vector3(tela.x, tela.y, 0f));
            if (Mathf.Abs(r.direction.y) < 1e-4f) return Vector2.zero;
            Vector3 p = r.origin - r.direction * (r.origin.y / r.direction.y);
            return new Vector2(p.x, p.z);
        }

        Vector2 Tela(Vector3 mundo) => _cam.WorldToScreenPoint(mundo);

        /// <summary>Profundidade para a ordem dos sprites (quem esta mais perto da camera desenha por cima).</summary>
        int Ordem(Vector3 p) => Mathf.Clamp(-(int)(Vector3.Dot(p - _alvo, _cam.transform.forward) * 100f) * 10, -31000, 31000);   // relativo ao alvo: cabe no short do sortingOrder

        // ------------------------------------------------------------------ salas e placas

        void Salas3D(Sim s)
        {
            int sob = _arrasto ? SalaEm(Chao(Input.mousePosition)) : -1;
            for (int e = 0; e < 4; e++)
            {
                Station st = s.Stations[e];
                bool fechada = st.Tipo == Estacao.Loja && s.Alocados(Estacao.Loja) == 0;
                Cor(_piso[e], Color.Lerp(Art.EstacaoCor[e], Color.white, fechada ? 0.1f : 0.55f) * (fechada ? 0.6f : 1f));
                Color cor = st.Estado == Estado.Critico ? Art.Bad : st.Estado == Estado.Atencao ? Art.Warn : Art.Good;
                bool destino = Arrastando >= 0 && s.Staff[Arrastando].Em != st.Tipo;
                if (destino) cor = Color.Lerp(Art.Good, Color.white, sob == e ? 0.6f : 0f);
                float pulso = st.Estado == Estado.Normal && !destino ? 0.55f : 0.6f + 0.4f * Mathf.PingPong(Time.unscaledTime * 2f, 1f);
                foreach (Renderer m in _moldura[e]) { Cor(m, cor * pulso); m.transform.localScale = Escalada(m.transform.localScale, destino ? 0.2f : 0.12f); }
                for (int i = 0; i < 4; i++)
                {
                    int nivel = 0;
                    for (int n = 1; n < NivelMalha.Length; n++) if (_mesa[e, i, n] != null && st.Vel >= NivelMalha[n]) nivel = n;
                    for (int n = 0; n < NivelMalha.Length; n++) if (_mesa[e, i, n] != null) _mesa[e, i, n].SetActive(i < st.Postos && n == nivel);
                }
                bool toque = s.PodeTocar(st.Tipo);
                _mao[e].enabled = toque;
                if (toque)
                {
                    Vector3 pm = V3(Em(e, 1.3f, 0f), 1.6f);
                    _mao[e].transform.position = pm;
                    _mao[e].transform.rotation = _cam.transform.rotation;
                    _mao[e].transform.localScale = Vector3.one * (0.55f + 0.1f * Mathf.PingPong(Time.unscaledTime * 3f, 1f));
                    _mao[e].sortingOrder = Ordem(pm) + 9;
                }
                string nome = fechada ? Textos.Estacao[e] + " " + Textos.FechadaCurto : Textos.Estacao[e];
                if (_placa[e].text != nome) { _placa[e].text = nome; _placa[e].characterSize = fechada ? 0.032f : 0.045f; }
                bool alerta = st.Estado != Estado.Normal;
                _selo[e].gameObject.SetActive(alerta);
                if (alerta)
                {
                    Coloca(_selo[e], V3(Centro(e), 1.9f));
                    Prende(_selo[e]);
                    _seloTexto[e].text = Textos.EstadoTexto(st.Estado, st.VooEmRisco);
                    _seloTexto[e].color = st.Estado == Estado.Critico ? Art.Bad : Art.Warn;
                }
            }
        }

        /// <summary>Engrossa a moldura (lado curto) para o destino aceso, sem mexer no comprimento.</summary>
        static Vector3 Escalada(Vector3 s, float g) => s.x < s.z ? new Vector3(g, s.y, s.z) : new Vector3(s.x, s.y, g);

        /// <summary>Game: eventos do nucleo que o mundo mostra (embarque pela ponte, chegada e saida dos avioes).</summary>
        public void Evento(Evento e)
        {
            if (e.Tipo == Ev.Embarcou) _embarcou.Add(e.A);
            EventoPatio(e);
        }

        /// <summary>Para a medida do K3 (-perf): pessoas, avioes e veiculos ativos agora.</summary>
        public string Contagem()
        {
            int pessoas = _pax.Count + _patio.Count + S.Staff.Count, avioes = 0;
            foreach (Aviao a in _avioes) if (a.T.gameObject.activeSelf) avioes++;
            return "pessoas=" + pessoas + " avioes=" + avioes + " veiculos=" + _veiculos.Count;
        }

        void Coloca(RectTransform rt, Vector3 mundo)
        {
            Vector3 sp = _cam.WorldToScreenPoint(mundo);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rotulos, sp, null, out Vector2 lp)) rt.localPosition = lp;
        }

        /// <summary>Mantem o selo inteiro dentro da tela (a sala da borda nao corta o estado).</summary>
        void Prende(RectTransform rt)
        {
            Rect area = _rotulos.rect;
            Vector3 p = rt.localPosition;
            float hx = rt.sizeDelta.x * 0.5f, hy = rt.sizeDelta.y * 0.5f;
            p.x = Mathf.Clamp(p.x, area.xMin + hx + 8f, area.xMax - hx - 8f);
            p.y = Mathf.Clamp(p.y, area.yMin + hy + 8f, area.yMax - hy - 8f);
            rt.localPosition = p;
        }

        /// <summary>Retangulo da sala na tela (px), para o tutorial: a caixa dos 4 cantos do chao e de 1 m acima.</summary>
        public Rect ZonaTela(Estacao e)
        {
            Vector2 m = Salas[(int)e].Min;
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                Vector2 p = Tela(new Vector3(m.x + (i & 1) * Lado, (i & 4) != 0 ? 1f : 0f, m.y + ((i >> 1) & 1) * Lado));
                x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        /// <summary>Retangulo do funcionario na tela (px), com o alvo minimo de toque; vazio se ainda nao foi contratado.</summary>
        public Rect FuncTela(int k)
        {
            if (k >= S.Staff.Count) return Rect.zero;
            Vector2 c = Tela(V3(_func[k].Pos, 0.5f * EscalaFunc));
            float r = Mathf.Max(Dp.Px(Dp.AlvoMinimoDp) * 0.5f, Vector2.Distance(Tela(V3(_func[k].Pos)), Tela(V3(_func[k].Pos, 0.55f))));
            return new Rect(c.x - r, c.y - r, 2f * r, 2f * r);
        }

        // ------------------------------------------------------------------ toque, arrasto, pan e zoom

        static bool SobreUi()
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;
            return Input.touchCount > 0 ? es.IsPointerOverGameObject(Input.GetTouch(0).fingerId) : es.IsPointerOverGameObject();
        }

        /// <summary>Dedo/mouse no mundo. Toque na sala = atendimento manual; no funcionario = "mover para"; arrastar o funcionario = mover;
        /// arrastar no vazio = pan; pinca ou roda = zoom.</summary>
        void Entrada()
        {
            if (Input.touchCount >= 2)
            {
                float d = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
                if (_pinca > 0f && d > 1f) _tam *= _pinca / d;
                _pinca = d;
                _press = -2; _arrasto = false; _pan = false;
                return;
            }
            _pinca = 0f;
            float roda = Input.mouseScrollDelta.y;
            if (Mathf.Abs(roda) > 0.01f && !SobreUi()) _tam *= roda > 0f ? 0.9f : 1.1f;

            Sim s = S;
            Vector2 sp = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                _press = -2; _arrasto = false; _pan = false;
                if (SobreUi() || !_h.MundoAceita(sp)) return;
                _pressPos = _ultimo = sp;
                _press = -1;
                float melhor = float.MaxValue;
                for (int k = 0; k < s.Staff.Count; k++)
                {
                    Rect r = FuncTela(k);
                    float d = Vector2.Distance(r.center, sp);
                    if (r.Contains(sp) && d < melhor) { melhor = d; _press = k; }
                }
                return;
            }
            if (_press == -2) return;
            bool longe = (sp - _pressPos).magnitude > Dp.Px(10f);   // limiar de arrasto, separado do toque
            if (Input.GetMouseButton(0))
            {
                if (_press >= 0 && !_arrasto && longe) _arrasto = true;
                if (_arrasto) _func[_press].Pos = Chao(sp + new Vector2(0f, Dp.Px(40f)));   // acima do dedo: nao tampa o destino
                if (_press == -1 && !_pan && longe && !_h.Tutorial) _pan = true;
                if (_pan) { Vector2 a = Chao(_ultimo), b = Chao(sp); _alvo += V3(a - b); }
                _ultimo = sp;
                return;
            }
            if (!Input.GetMouseButtonUp(0)) return;
            int quem = _press;
            bool arrastou = _arrasto, pan = _pan;
            _press = -2; _arrasto = false; _pan = false;
            if (quem >= 0 && arrastou)
            {
                int z = SalaEm(Chao(sp));
                if (z >= 0 && _g.Move(quem, (Estacao)z)) return;
                _falha[quem] = 1f;   // falha: volta tremendo e diz o que fazer
                _h.Toast(Textos.SoltaFora);
            }
            else if (quem >= 0) _h.AbreMover(quem);
            else if (!pan && !longe)
            {
                int z = SalaEm(Chao(sp));
                if (z >= 0) _g.Tap((Estacao)z);
            }
        }
    }
}
