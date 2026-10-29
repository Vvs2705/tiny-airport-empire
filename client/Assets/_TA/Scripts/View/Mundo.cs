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
    /// O aeroporto em 3D em tempo real (direcao do Vinicius em 29/10: personagens se movendo, trocando de setor e de uniforme, no padrao
    /// visual dos lideres do genero). Le o Sim a cada quadro; nada aqui muda regra.
    ///  - Camera ortografica em angulo isometrico fixo (35 graus de cima, 45 de lado; nao gira), com pan (arrastar no vazio) e zoom (pinca,
    ///    roda do mouse). O enquadramento inicial poe o terminal e o patio na faixa entre o quadro e a barra de baixo.
    ///  - Planta em escada, para o fluxo subir na tela em retrato: entrada -> check-in -> seguranca (leste) -> loja (norte) -> portao
    ///    (leste) -> ponte de embarque -> aviao no patio. Salas de 4 x 4 em corte (paredes baixas, sem teto), portas entre salas, fila
    ///    indiana entre fitas e os balcoes junto a parede de saida. Primitivas do Unity (cubos, capsula, cilindros) com cor por
    ///    MaterialPropertyBlock: os modelos do Tripo entram no lugar delas como malha.
    ///  - Patio vivo: o aviao do voo que esta embarcando (cauda na cor do voo), escada, caminhao de combustivel e carrinho de bagagem
    ///    rodando em volta; quando o voo parte, o aviao taxia para fora e volta o proximo. So visual (sem regra nova no nucleo).
    ///  - Pessoas = billboard (sprites virados para a camera) em camadas: corpo, colete tingido na cor do setor, cabeca e cabelo. O
    ///    passageiro leva a mala na cor do voo (o chip do quadro) e anda pela fila, vai ao balcao, segue pelas portas e embarca pela ponte;
    ///    quem perde o voo sai pela porta. O funcionario (cabelo e nome fixos) anda de sala em sala pelas portas no tempo da
    ///    Tuning.Caminhada e, ao chegar, troca o colete com um efeito de 0,35 s. A folha do Tripo entra nos mesmos sprites.
    ///  - Toque na sala = atendimento manual; arrastar o funcionario (alvo de 48 dp no minimo, limiar em dp separado do toque, boneco
    ///    acima do dedo, salas de destino acesas) ou tocar nele = "mover para".
    /// </summary>
    public sealed class Mundo
    {
        // ------------------------------------------------------------------ planta (unidades de mundo; Y para cima, chao em Y = 0)

        public const float Lado = 4f;
        const float VelPax = 2.6f, Troca = 0.35f, EscalaFunc = 1f, EscalaPax = 0.85f, PortaLarg = 0.9f;
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

        static Sala Nova(float x, float z, Vector2 entra, Vector2 sai, Vector2 n) =>
            new Sala { Min = new Vector2(x, z), In = entra, Out = sai, N = n, T = new Vector2(n.y, n.x) };

        static readonly Vector2 Calcada = new Vector2(-2.6f, 2f), PonteFim = new Vector2(10f, 11.1f);
        const float AviaoX = 11.6f, AviaoZ = 12f;

        /// <summary>Ponto da sala a `d` da parede de saida e `a` ao longo dela.</summary>
        static Vector2 Em(int e, float d, float a) => Salas[e].Out - Salas[e].N * d + Salas[e].T * a;
        static Vector2 Balcao(int e, int i) => Em(e, 1.0f, Desloc[i]);
        static Vector2 NoBalcao(int e, int i) => Em(e, 1.55f, Desloc[i]);
        static Vector2 AtrasBalcao(int e, int i) => Em(e, 0.45f, Desloc[i]);
        static Vector2 Centro(int e) => Salas[e].Min + new Vector2(Lado * 0.5f, Lado * 0.5f);

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

        sealed class Boneco
        {
            public Transform T;
            public SpriteRenderer Sombra, PernaE, PernaD, Corpo, Colete, Cabeca, Cabelo, Olho, Mala, Coracao;
            public Vector2 Pos;
            public readonly List<Vector2> Caminho = new List<Vector2>();
            public int Zona = -1;
            public bool Saindo, Andou;
            public float Fase, Dir = 1f, Escala = 1f;
        }

        readonly Hud _h;
        readonly Game _g;
        readonly Camera _cam;
        readonly Transform _raiz;
        readonly RectTransform _rotulos;
        static MaterialPropertyBlock _mpb;
        static Material _mat;
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
        readonly Renderer[,] _mesa = new Renderer[4, 4];
        readonly SpriteRenderer[,] _arco = new SpriteRenderer[4, 4];
        readonly SpriteRenderer[] _mao = new SpriteRenderer[4];
        readonly RectTransform[] _rotulo = new RectTransform[4];
        readonly Text[] _rSub = new Text[4], _rEstado = new Text[4];
        readonly Image[] _rIcone = new Image[4];
        Transform _aviao;
        Renderer _cauda;
        readonly Transform[] _veiculo = new Transform[2];
        readonly float[] _veiculoT = { 0f, 0.5f };
        float _decola = -1f;

        // camera e gesto
        Vector3 _alvo;
        float _tam = 6f;
        bool _enquadrado;
        Rect _regiao;
        int _press = -2;
        Vector2 _pressPos, _ultimo;
        bool _arrasto, _pan;
        float _pinca;

        Sim S => _g.Sim;
        /// <summary>Funcionario sendo arrastado (-1 = nenhum).</summary>
        public int Arrastando => _arrasto ? _press : -1;

        static readonly Color[] Pele = { Art.Hex(0xF1C27D), Art.Hex(0xE0AC69), Art.Hex(0xC68642), Art.Hex(0x8D5524), Art.Hex(0xFFDBAC) };
        static readonly Color[] Roupa = { Art.Hex(0x6C7A89), Art.Hex(0xA569BD), Art.Hex(0x5DADE2), Art.Hex(0xF4D03F), Art.Hex(0x58D68D), Art.Hex(0xEC7063), Art.Hex(0xD5DBDB) };
        static readonly Color[] CabeloFunc = { Art.Hex(0x3B2314), Art.Hex(0xD35400), Art.Hex(0x111111) };
        static readonly Color CorParede = Art.Hex(0xD9DEE8), Fita = Art.Hex(0x2F6FE4), Asfalto = Art.Hex(0x3C4250), Grama = Art.Hex(0x5E8C4A);

        public Mundo(Hud h, Game g, RectTransform rotulos)
        {
            _h = h; _g = g; _rotulos = rotulos;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _cam = Object.FindAnyObjectByType<Camera>();
            _cam.orthographic = true;
            _cam.transform.rotation = Quaternion.Euler(35f, 45f, 0f);
            _cam.nearClipPlane = 0.1f; _cam.farClipPlane = 200f;
            _cam.transparencySortMode = TransparencySortMode.Orthographic;
            var luz = new GameObject("Sol").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.05f;
            luz.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);
            _raiz = new GameObject("Mundo").transform;
            Planta();
            for (int k = 0; k < _func.Length; k++)
            {
                _func[k] = NovoBoneco(true);
                _func[k].Cabelo.color = CabeloFunc[k];
                _func[k].T.gameObject.SetActive(false);
                _nome[k] = Art.Outlined(Art.NewText(_rotulos, "Nome" + k, 26, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)), 2f);
                _nome[k].rectTransform.sizeDelta = new Vector2(160f, 34f);
                _nome[k].text = Textos.Funcionario[k];
            }
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

        void Planta()
        {
            Bloco("Gramado", PrimitiveType.Cube, new Vector3(7f, -0.12f, 7f), new Vector3(30f, 0.1f, 30f), Grama);
            Bloco("Calcada", PrimitiveType.Cube, new Vector3(-1.6f, -0.06f, 2f), new Vector3(3.2f, 0.1f, 4.4f), Art.Hex(0xB8B2A6));
            Bloco("Patio", PrimitiveType.Cube, new Vector3(11.5f, -0.06f, 12.4f), new Vector3(11f, 0.1f, 8.6f), Asfalto);
            for (int i = 0; i < 5; i++) Bloco("Faixa", PrimitiveType.Cube, new Vector3(7.2f + i * 2.1f, -0.005f, 15.6f), new Vector3(1.1f, 0.01f, 0.12f), Art.BoardInk);
            for (int e = 0; e < 4; e++) Sala3D(e);
            // ponte de embarque, da porta norte do portao ate a porta do aviao
            Bloco("Ponte", PrimitiveType.Cube, new Vector3(10f, 0.04f, 9.55f), new Vector3(0.8f, 0.08f, 3.1f), Art.Hex(0x9AA6BC));
            Bloco("PonteLadoE", PrimitiveType.Cube, new Vector3(9.58f, 0.3f, 9.55f), new Vector3(0.06f, 0.5f, 3.1f), Art.Hex(0x6E7A90));
            Bloco("PonteLadoD", PrimitiveType.Cube, new Vector3(10.42f, 0.3f, 9.55f), new Vector3(0.06f, 0.5f, 3.1f), Art.Hex(0x6E7A90));
            Aviao3D();
            _veiculo[0] = Veiculo("Combustivel", Art.Hex(0xF4D03F), 1.3f);
            _veiculo[1] = Veiculo("Bagagem", Art.Hex(0x5DADE2), 0.9f);
            Bloco("Escada", PrimitiveType.Cube, new Vector3(13.9f, 0.45f, 13.1f), new Vector3(0.5f, 0.12f, 1.2f), Art.Hex(0xE5E8E8)).transform.localEulerAngles = new Vector3(28f, 0f, 0f);
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
                bool longoX = Mathf.Abs(s.N.y) > 0.5f;
                _mesa[e, i] = Bloco("Balcao" + e + i, PrimitiveType.Cube, new Vector3(b.x, 0.25f, b.y), longoX ? new Vector3(0.55f, 0.5f, 0.3f) : new Vector3(0.3f, 0.5f, 0.55f), Art.Paper);
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
            BuildRotulo(e);
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
            foreach (float t in cortes) { Trecho(a, d, ini, t - PortaLarg * 0.5f); ini = t + PortaLarg * 0.5f; }
            Trecho(a, d, ini, len);
        }

        void Trecho(Vector2 a, Vector2 d, float t0, float t1)
        {
            if (t1 - t0 < 0.05f) return;
            Vector2 c = a + d * ((t0 + t1) * 0.5f);
            float l = t1 - t0;
            bool x = Mathf.Abs(d.x) > 0.5f;
            Bloco("Parede", PrimitiveType.Cube, new Vector3(c.x, 0.17f, c.y), x ? new Vector3(l + 0.06f, 0.34f, 0.08f) : new Vector3(0.08f, 0.34f, l + 0.06f), CorParede);
        }

        void Aviao3D()
        {
            _aviao = new GameObject("Aviao").transform;
            _aviao.SetParent(_raiz, false);
            _aviao.localPosition = new Vector3(AviaoX, 0f, AviaoZ);
            Color branco = Art.Hex(0xF2F4F7), cinza = Art.Hex(0xA6ACAF);
            Bloco("Fuselagem", PrimitiveType.Capsule, new Vector3(0f, 1.0f, 0f), new Vector3(0.95f, 3.6f, 0.95f), branco, _aviao).transform.localEulerAngles = new Vector3(0f, 0f, 90f);
            Bloco("Asas", PrimitiveType.Cube, new Vector3(-0.2f, 0.85f, 0f), new Vector3(1.3f, 0.08f, 6.4f), branco, _aviao);
            Bloco("MotorE", PrimitiveType.Cylinder, new Vector3(-0.1f, 0.5f, -1.6f), new Vector3(0.4f, 0.35f, 0.4f), cinza, _aviao).transform.localEulerAngles = new Vector3(0f, 0f, 90f);
            Bloco("MotorD", PrimitiveType.Cylinder, new Vector3(-0.1f, 0.5f, 1.6f), new Vector3(0.4f, 0.35f, 0.4f), cinza, _aviao).transform.localEulerAngles = new Vector3(0f, 0f, 90f);
            Bloco("Estabilizador", PrimitiveType.Cube, new Vector3(3.1f, 1.15f, 0f), new Vector3(0.7f, 0.06f, 2.3f), branco, _aviao);
            _cauda = Bloco("Cauda", PrimitiveType.Cube, new Vector3(3.1f, 1.85f, 0f), new Vector3(1.0f, 1.3f, 0.08f), Art.VooCor[0], _aviao);
            Bloco("Cabine", PrimitiveType.Cube, new Vector3(-3.25f, 1.25f, 0f), new Vector3(0.3f, 0.2f, 0.7f), Art.Hex(0x2C3E50), _aviao);
        }

        Transform Veiculo(string nome, Color cor, float comp)
        {
            var t = new GameObject(nome).transform;
            t.SetParent(_raiz, false);
            Bloco("Carroceria", PrimitiveType.Cube, new Vector3(0f, 0.3f, -0.1f), new Vector3(0.55f, 0.45f, comp), cor, t);
            Bloco("CabineV", PrimitiveType.Cube, new Vector3(0f, 0.38f, comp * 0.5f + 0.1f), new Vector3(0.55f, 0.55f, 0.35f), Art.Hex(0xECF0F1), t);
            return t;
        }

        void BuildRotulo(int e)
        {
            RectTransform r = Art.Node(_rotulos, "Rotulo" + e, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            r.pivot = new Vector2(0f, 1f);   // pendurado no canto esquerdo da sala, para dentro dela
            r.sizeDelta = new Vector2(330f, 116f);
            _rotulo[e] = r;
            Image fundo = Art.Panel(r, "Fundo", Art.A(Art.Dark, 0.62f), Vector2.zero, Vector2.one);
            fundo.raycastTarget = false;   // le sobre a multidao, sem roubar o toque da sala
            Text nome = Art.Outlined(Art.NewText(r, "Nome", 32, new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.98f), TextAnchor.MiddleLeft), 3f);
            nome.text = Textos.Estacao[e];
            nome.color = Color.Lerp(Art.EstacaoCor[e], Art.Ink, 0.55f);
            _rSub[e] = Art.Outlined(Art.NewText(r, "Sub", 22, new Vector2(0.04f, 0.38f), new Vector2(0.96f, 0.62f), TextAnchor.MiddleLeft), 2f);
            _rIcone[e] = Art.Img(r, "Icone", Art.Alerta(), Art.Bad, new Vector2(0.03f, 0.04f), new Vector2(0.12f, 0.36f));
            _rEstado[e] = Art.Outlined(Art.NewText(r, "Estado", 24, new Vector2(0.13f, 0.02f), new Vector2(0.98f, 0.38f), TextAnchor.MiddleLeft), 2f);
        }

        SpriteRenderer Billboard(string nome, Sprite s, Color c, float tam)
        {
            SpriteRenderer r = Art.NewSprite(_raiz, nome, s, c, 0, Vector2.zero, new Vector2(tam, tam));
            r.enabled = false;
            return r;
        }

        Boneco NovoBoneco(bool func)
        {
            var b = new Boneco { T = new GameObject(func ? "Funcionario" : "Passageiro").transform };
            b.T.SetParent(_raiz, false);
            b.Sombra = Art.NewSprite(b.T, "Sombra", Art.Disc(), new Color(0f, 0f, 0f, 0.3f), 0, new Vector2(0f, 0.02f), new Vector2(0.46f, 0.16f));
            b.PernaE = Art.NewSprite(b.T, "PernaE", Art.Square(), Art.Hex(0x2C3E50), 0, new Vector2(-0.07f, 0.1f), new Vector2(0.09f, 0.2f));
            b.PernaD = Art.NewSprite(b.T, "PernaD", Art.Square(), Art.Hex(0x2C3E50), 0, new Vector2(0.07f, 0.1f), new Vector2(0.09f, 0.2f));
            b.Mala = Art.NewSprite(b.T, "Mala", Art.Mala(), Color.white, 0, new Vector2(-0.25f, 0.14f), new Vector2(0.2f, 0.24f));
            b.Corpo = Art.NewSprite(b.T, "Corpo", Art.Rounded(), func ? Art.Hex(0xF2F3F4) : Color.white, 0, new Vector2(0f, 0.37f), new Vector2(0.34f, 0.38f));
            b.Colete = Art.NewSprite(b.T, "Colete", Art.Colete(), Color.white, 0, new Vector2(0f, 0.39f), new Vector2(0.37f, 0.34f));
            b.Cabeca = Art.NewSprite(b.T, "Cabeca", Art.Disc(), Pele[0], 0, new Vector2(0f, 0.69f), new Vector2(0.27f, 0.27f));
            b.Cabelo = Art.NewSprite(b.T, "Cabelo", Art.Cabelo(), Art.Hex(0x3B2314), 0, new Vector2(0f, 0.73f), new Vector2(0.29f, 0.27f));
            b.Olho = Art.NewSprite(b.T, "Olhos", Art.Square(), Art.Tinta, 0, new Vector2(0.05f, 0.69f), new Vector2(0.09f, 0.035f));
            b.Coracao = Art.NewSprite(b.T, "Historia", Art.Heart(), Art.Bad, 0, new Vector2(0f, 1.02f), new Vector2(0.26f, 0.26f));
            b.Colete.enabled = func;
            b.Mala.enabled = !func;
            b.Coracao.enabled = false;
            return b;
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
        }

        /// <summary>Enquadramento inicial: o terminal e o patio inteiros na faixa entre o quadro e a barra de baixo.</summary>
        void Enquadra()
        {
            Vector3 dir = _cam.transform.right, up = _cam.transform.up;
            // o terminal e o aviao (o patio inteiro fica a um pan de distancia): bonecos maiores na tela
            var pts = new[] { new Vector3(-2.6f, 0f, 0f), new Vector3(8f, 0f, 0f), new Vector3(12f, 0f, 4f), new Vector3(0f, 1f, 4f), new Vector3(4f, 1f, 8f), new Vector3(12f, 1f, 8f), new Vector3(10f, 1.5f, 13f), new Vector3(14f, 1.5f, 12f) };
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
        }

        /// <summary>Projeta um ponto no chao (Y = 0) ao longo da direcao da camera.</summary>
        Vector3 NoChao(Vector3 p)
        {
            Vector3 f = _cam.transform.forward;
            return p - f * (p.y / f.y);
        }

        void Posiciona()
        {
            _tam = Mathf.Clamp(_tam, 2.5f, 12f);
            _alvo.x = Mathf.Clamp(_alvo.x, -3f, 17f);
            _alvo.z = Mathf.Clamp(_alvo.z, -2f, 17f);
            _cam.orthographicSize = _tam;
            _cam.transform.position = _alvo - _cam.transform.forward * 60f;
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
        int Ordem(Vector3 p) => -(int)(Vector3.Dot(p - _alvo, _cam.transform.forward) * 100f) * 10;   // relativo ao alvo: cabe no short do sortingOrder

        void SincronizaPax(Sim s, float dt)
        {
            _vistos.Clear();
            foreach (Station st in s.Stations)
            {
                int e = (int)st.Tipo;
                for (int k = 0; k < st.Fila.Count; k++) Liga(st.Fila[k], e, Fila(e, k), s, false);
                for (int i = 0; i < 4; i++)
                {
                    bool ocupado = i < st.Atendendo.Count;
                    _arco[e, i].enabled = ocupado;
                    if (!ocupado) continue;
                    Pax p = st.Atendendo[i];
                    Liga(p, e, NoBalcao(e, i), s, true);
                    _arco[e, i].sprite = Art.Arc(1f - p.Resto / (float)s.TempoPassos(st.Tipo));
                    Vector3 pa = V3(NoBalcao(e, i), 1.15f);
                    _arco[e, i].transform.position = pa;
                    _arco[e, i].transform.rotation = _cam.transform.rotation;
                    _arco[e, i].sortingOrder = Ordem(pa) + 9;
                }
            }
            _sair.Clear();
            foreach (KeyValuePair<int, Boneco> kv in _pax)
            {
                Boneco b = kv.Value;
                if (!_vistos.Contains(kv.Key) && !b.Saindo) Sai(kv.Key, b);
                Anda(b, VelPax * dt);
                Pinta(b);
                if (b.Saindo && b.Caminho.Count == 0) _sair.Add(kv.Key);
            }
            foreach (int id in _sair)
            {
                Boneco b = _pax[id];
                _pax.Remove(id);
                _embarcou.Remove(id);
                b.T.gameObject.SetActive(false);
                _livres.Push(b);
            }
        }

        void Liga(Pax p, int e, Vector2 alvo, Sim s, bool noBalcao)
        {
            _vistos.Add(p.Id);
            if (!_pax.TryGetValue(p.Id, out Boneco b))
            {
                b = _livres.Count > 0 ? _livres.Pop() : NovoBoneco(false);
                b.T.gameObject.SetActive(true);
                b.Pos = Calcada + new Vector2(-0.4f * (p.Indice % 3), 0.3f * (p.Indice % 4) - 0.45f);
                b.Zona = -1; b.Saindo = false; b.Caminho.Clear(); b.Escala = EscalaPax;
                b.Corpo.color = Roupa[(p.Id * 7 + p.Indice) % Roupa.Length];
                b.Cabeca.color = Pele[(p.Id * 3 + 1) % Pele.Length];
                b.Cabelo.color = (p.Id & 1) == 0 ? Art.Hex(0x3B2314) : Art.Hex(0x1B1B1B);
                b.Coracao.enabled = p.Historia;
                _pax[p.Id] = b;
            }
            if (b.Zona != e)
            {
                b.Caminho.Clear();
                for (int z = b.Zona + 1; z <= e; z++) { b.Caminho.Add(Salas[z].In); if (z < e) b.Caminho.Add(Salas[z].Out); }   // de porta em porta
                b.Caminho.Add(alvo);
                b.Zona = e;
            }
            else if (b.Caminho.Count == 0) b.Caminho.Add(alvo);
            else b.Caminho[b.Caminho.Count - 1] = alvo;
            Voo v = s.VooDe(p);
            b.Mala.color = e == (int)Estacao.Portao && v != null && !v.Embarque && !noBalcao ? Art.A(Art.VooCor[p.Voo], 0.55f) : Art.VooCor[p.Voo];
        }

        /// <summary>Saiu do nucleo: embarcou (Ev.Embarcou) = pela porta norte do portao e pela ponte ate o aviao; senao perdeu o voo = sai
        /// pela porta por onde entrou.</summary>
        void Sai(int id, Boneco b)
        {
            b.Saindo = true;
            b.Caminho.Clear();
            int z = Mathf.Clamp(b.Zona, 0, 3);
            if (_embarcou.Contains(id)) { b.Caminho.Add(Salas[3].Out); b.Caminho.Add(PonteFim); }
            else b.Caminho.Add(Salas[z].In + (Salas[z].In - Centro(z)).normalized * 0.6f);   // um passo para fora da porta
        }

        void Anda(Boneco b, float passo)
        {
            Vector2 antes = b.Pos;
            while (passo > 0f && b.Caminho.Count > 0)
            {
                Vector2 d = b.Caminho[0] - b.Pos;
                float l = d.magnitude;
                if (l <= passo) { b.Pos = b.Caminho[0]; passo -= l; b.Caminho.RemoveAt(0); }
                else { b.Pos += d / l * passo; passo = 0f; }
            }
            Vector2 mov = b.Pos - antes;
            b.Andou = mov.sqrMagnitude > 1e-8f;
            if (b.Andou)
            {
                float lado = Vector3.Dot(V3(mov), _cam.transform.right);   // direcao do olhar na tela
                if (Mathf.Abs(lado) > 1e-5f) b.Dir = Mathf.Sign(lado);
            }
        }

        /// <summary>Billboard: o boneco vira para a camera; balanco do andar, direcao do olhar e ordem por profundidade.</summary>
        void Pinta(Boneco b)
        {
            if (b.Andou) b.Fase += Time.deltaTime * 14f;
            float bob = b.Andou ? Mathf.Abs(Mathf.Sin(b.Fase)) * 0.05f : 0f, squash = b.Andou ? 1f - Mathf.Abs(Mathf.Sin(b.Fase)) * 0.05f : 1f;
            Vector3 p = V3(b.Pos);
            b.T.position = p + _cam.transform.up * bob;
            b.T.rotation = _cam.transform.rotation;
            b.T.localScale = new Vector3(b.Escala * (2f - squash), b.Escala * squash, 1f);
            float perna = b.Andou ? Mathf.Sin(b.Fase) * 0.035f : 0f;
            b.PernaE.transform.localPosition = new Vector3(-0.07f, 0.1f + perna, 0f);
            b.PernaD.transform.localPosition = new Vector3(0.07f, 0.1f - perna, 0f);
            b.Olho.transform.localPosition = new Vector3(0.045f * b.Dir, 0.69f, 0f);
            b.Mala.transform.localPosition = new Vector3(-0.25f * b.Dir, 0.14f, 0f);
            int o = Ordem(p);
            b.Sombra.sortingOrder = o; b.PernaE.sortingOrder = b.PernaD.sortingOrder = o + 1; b.Mala.sortingOrder = o + 2;
            b.Corpo.sortingOrder = o + 3; b.Colete.sortingOrder = o + 4; b.Cabeca.sortingOrder = o + 5; b.Cabelo.sortingOrder = o + 6;
            b.Olho.sortingOrder = o + 7; b.Coracao.sortingOrder = o + 8;
        }

        // ------------------------------------------------------------------ funcionarios

        void Funcionarios(Sim s, float dt)
        {
            for (int k = 0; k < _func.Length; k++)
            {
                Boneco b = _func[k];
                bool existe = k < s.Staff.Count;
                b.T.gameObject.SetActive(existe);
                _nome[k].enabled = existe;
                if (!existe) continue;
                Funcionario f = s.Staff[k];
                if (_colete[k] < 0) { _colete[k] = (int)f.Em; b.Pos = Lugar(s, f); _troca[k] = Troca; }   // contratado: aparece ja de uniforme
                if (Arrastando == k) { }   // o arrasto posiciona
                else if (f.Andando > 0)
                {
                    if (_rota[k].Count == 0 || _rotaPara[k] != f.Em) Rota(k, s, f);
                    float total = Mathf.Max(1f, s.T.Caminhada * Sim.Hz);
                    Vector2 antes = b.Pos;
                    b.Pos = NaRota(_rota[k], 1f - f.Andando / total);
                    b.Andou = (b.Pos - antes).sqrMagnitude > 1e-8f;
                    float lado = Vector3.Dot(V3(b.Pos - antes), _cam.transform.right);
                    if (Mathf.Abs(lado) > 1e-5f) b.Dir = Mathf.Sign(lado);
                }
                else
                {
                    _rota[k].Clear();
                    b.Caminho.Clear();
                    b.Caminho.Add(Lugar(s, f));
                    Anda(b, VelPax * 1.5f * Mathf.Max(dt, Time.deltaTime));
                    if (_colete[k] != (int)f.Em) { _colete[k] = (int)f.Em; _troca[k] = Troca; }   // chegou: veste o uniforme do setor
                }
                float t = _troca[k] > 0f ? 1f - _troca[k] / Troca : 1f;
                if (_troca[k] > 0f) _troca[k] = Mathf.Max(0f, _troca[k] - Time.deltaTime);
                b.Colete.color = Color.Lerp(Color.white, Art.EstacaoCor[_colete[k]], t);   // flash branco que assenta na cor do setor
                b.Escala = EscalaFunc * ((Arrastando == k ? 1.25f : 1f) + 0.3f * Mathf.Sin(Mathf.PI * t) * (_troca[k] > 0f ? 1f : 0f));
                Vector2 real = b.Pos;
                if (_falha[k] > 0f)
                {
                    b.Pos += new Vector2(_cam.transform.right.x, _cam.transform.right.z) * Mathf.Sin(Time.unscaledTime * 60f) * 0.12f * _falha[k];
                    _falha[k] = Mathf.Max(0f, _falha[k] - Time.unscaledDeltaTime * 2f);
                }
                Pinta(b);
                b.Pos = real;
                Coloca(_nome[k].rectTransform, V3(b.Pos, 1.05f * b.Escala));
                _nome[k].color = f.Andando > 0 ? Art.Warn : Art.Ink;
            }
        }

        /// <summary>Lugar do funcionario atras do balcao da sala (o n-esimo da sala, na ordem de contratacao).</summary>
        static Vector2 Lugar(Sim s, Funcionario f)
        {
            int n = 0;
            foreach (Funcionario o in s.Staff) { if (o == f) break; if (o.Em == f.Em) n++; }
            return AtrasBalcao((int)f.Em, n % 4);
        }

        /// <summary>Caminho de sala em sala pelas portas (nunca pela parede): do lugar dele ate o centro da sala, porta a porta, e o balcao.</summary>
        void Rota(int k, Sim s, Funcionario f)
        {
            List<Vector2> r = _rota[k];
            r.Clear();
            _rotaPara[k] = f.Em;
            int de = SalaEm(_func[k].Pos), para = (int)f.Em;
            if (de < 0) de = (int)f.De;
            r.Add(_func[k].Pos);
            r.Add(Centro(de));
            for (int z = de; z < para; z++) { r.Add(Salas[z].Out); r.Add(Centro(z + 1)); }
            for (int z = de; z > para; z--) { r.Add(Salas[z].In); r.Add(Centro(z - 1)); }
            r.Add(Lugar(s, f));
        }

        static Vector2 NaRota(List<Vector2> r, float t)
        {
            float total = 0f;
            for (int i = 1; i < r.Count; i++) total += Vector2.Distance(r[i - 1], r[i]);
            float alvo = Mathf.Clamp01(t) * total;
            for (int i = 1; i < r.Count; i++)
            {
                float l = Vector2.Distance(r[i - 1], r[i]);
                if (alvo <= l && l > 0f) return Vector2.Lerp(r[i - 1], r[i], alvo / l);
                alvo -= l;
            }
            return r[r.Count - 1];
        }

        // ------------------------------------------------------------------ salas, rotulos e patio

        void Salas3D(Sim s)
        {
            int sob = _arrasto ? SalaEm(Chao(Input.mousePosition)) : -1;
            for (int e = 0; e < 4; e++)
            {
                Station st = s.Stations[e];
                bool fechada = st.Tipo == Estacao.Loja && s.Alocados(Estacao.Loja) == 0;
                Cor(_piso[e], Color.Lerp(Art.EstacaoCor[e], Color.white, fechada ? 0.1f : 0.45f) * (fechada ? 0.6f : 1f));
                Color cor = st.Estado == Estado.Critico ? Art.Bad : st.Estado == Estado.Atencao ? Art.Warn : Art.Good;
                bool destino = Arrastando >= 0 && s.Staff[Arrastando].Em != st.Tipo;
                if (destino) cor = Color.Lerp(Art.Good, Color.white, sob == e ? 0.6f : 0f);
                float pulso = st.Estado == Estado.Normal && !destino ? 0.55f : 0.6f + 0.4f * Mathf.PingPong(Time.unscaledTime * 2f, 1f);
                foreach (Renderer m in _moldura[e]) { Cor(m, cor * pulso); m.transform.localScale = Escalada(m.transform.localScale, destino ? 0.2f : 0.12f); }
                for (int i = 0; i < 4; i++) _mesa[e, i].enabled = i < st.Postos;
                bool toque = s.PodeTocar(st.Tipo);
                _mao[e].enabled = toque;
                if (toque)
                {
                    Vector3 pm = V3(Em(e, 1.3f, 0f), 1.4f);
                    _mao[e].transform.position = pm;
                    _mao[e].transform.rotation = _cam.transform.rotation;
                    _mao[e].transform.localScale = Vector3.one * (0.55f + 0.1f * Mathf.PingPong(Time.unscaledTime * 3f, 1f));
                    _mao[e].sortingOrder = Ordem(pm) + 9;
                }
                Coloca(_rotulo[e], V3(Salas[e].Min + new Vector2(0.15f, Lado - 0.15f), 0.6f));   // o canto mais a esquerda na tela
                Prende(_rotulo[e]);
                _rSub[e].text = Textos.Balcoes(st.Postos, s.TempoSeg(st.Tipo));
                _rIcone[e].enabled = st.Estado != Estado.Normal;
                _rIcone[e].color = cor;
                _rEstado[e].text = fechada ? Textos.Fechada : Textos.EstadoTexto(st.Estado, st.VooEmRisco);
                _rEstado[e].color = fechada ? Art.Dim : st.Estado == Estado.Critico ? Art.Bad : st.Estado == Estado.Atencao ? Art.Warn : Art.Good;
            }
        }

        /// <summary>Engrossa a moldura (lado curto) para o destino aceso, sem mexer no comprimento.</summary>
        static Vector3 Escalada(Vector3 s, float g) => s.x < s.z ? new Vector3(g, s.y, s.z) : new Vector3(s.x, s.y, g);

        /// <summary>Aviao do voo que esta embarcando (cauda na cor dele) e os veiculos rodando em volta; voo que parte = o aviao taxia para fora.</summary>
        void Patio(Sim s, float dt)
        {
            Voo v = null;
            foreach (Voo x in s.Voos) if (x != null && !x.Partiu) { v = x; break; }
            if (_decola >= 0f)
            {
                _decola += Time.deltaTime;
                float t = _decola / 2f;
                _aviao.localPosition = new Vector3(AviaoX + t * t * 12f, 0f, AviaoZ + t * 2f);
                if (t >= 1f) _decola = -1f;
            }
            else _aviao.localPosition = new Vector3(AviaoX, 0f, AviaoZ);
            Cor(_cauda, v != null ? Art.VooCor[v.Indice] : Art.Off);
            for (int i = 0; i < _veiculo.Length; i++)
            {
                _veiculoT[i] = Mathf.Repeat(_veiculoT[i] + Mathf.Max(dt, 0f) * 0.08f, 1f);
                Vector2 p = NoCircuito(_veiculoT[i]), q = NoCircuito(_veiculoT[i] + 0.01f);
                _veiculo[i].localPosition = V3(p);
                _veiculo[i].localRotation = Quaternion.LookRotation(V3(q - p).normalized, Vector3.up);
            }
        }

        /// <summary>Volta retangular em torno do aviao (0..1).</summary>
        static Vector2 NoCircuito(float t)
        {
            // atras da asa e na frente da fuselagem: nao cruza a ponte, a asa nem o aviao
            Vector2 a = new Vector2(12.4f, 9.3f), b = new Vector2(16.6f, 9.3f), c = new Vector2(16.6f, 10.9f), d = new Vector2(12.4f, 10.9f);
            t = Mathf.Repeat(t, 1f) * 4f;
            return t < 1f ? Vector2.Lerp(a, b, t) : t < 2f ? Vector2.Lerp(b, c, t - 1f) : t < 3f ? Vector2.Lerp(c, d, t - 2f) : Vector2.Lerp(d, a, t - 3f);
        }

        /// <summary>Game: eventos do nucleo que o mundo mostra (embarque pela ponte, decolagem).</summary>
        public void Evento(Evento e)
        {
            if (e.Tipo == Ev.Embarcou) _embarcou.Add(e.A);
            else if (e.Tipo == Ev.VooPartiu) _decola = 0f;
        }

        void Coloca(RectTransform rt, Vector3 mundo)
        {
            Vector3 sp = _cam.WorldToScreenPoint(mundo);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rotulos, sp, null, out Vector2 lp)) rt.localPosition = lp;
        }

        /// <summary>Mantem o rotulo inteiro dentro da tela (a sala da borda nao corta o nome nem o estado).</summary>
        void Prende(RectTransform rt)
        {
            Rect area = _rotulos.rect;
            Vector3 p = rt.localPosition;
            p.x = Mathf.Clamp(p.x, area.xMin + 8f, area.xMax - rt.sizeDelta.x - 8f);
            p.y = Mathf.Clamp(p.y, area.yMin + rt.sizeDelta.y + 8f, area.yMax - 8f);
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
