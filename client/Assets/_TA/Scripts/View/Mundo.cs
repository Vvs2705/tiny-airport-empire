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
    ///    roda do mouse). O enquadramento e de perto, nas salas abertas (a sala nova reenquadra): da para ler o uniforme; o patio e a pista
    ///    ficam a um pan.
    ///  - Planta em escada, para o fluxo subir na tela em retrato: entrada -> check-in -> seguranca (leste) -> loja (norte) -> portao
    ///    (leste) -> ponte de embarque -> aviao no patio. As 4 salas da Fase C2 encostam nessas: bagagem (norte do check-in), alimentacao
    ///    (sul do portao), despacho (leste do portao) e oficina (leste do despacho, com porta para o patio). Fechada pelo marco, a sala e
    ///    so o piso com a placa. Salas de 4 x 4 em corte (paredes baixas, sem teto), portas na tabela Portas, fila indiana entre fitas e
    ///    os balcoes junto a parede de saida. Atras das salas, o predio do terminal, que cresce com o marco; no patio, torre e hangar.
    ///  - Escala (docs/COMPETITIVO.md §h.8, decidida em 01/11 com fotos): adulto de 0,5 m (Gente) e 0,2 m de ombros, entao a grade de 4 m tem 20
    ///    pessoas de lado, 17 de piso util (Interno) e um corredor de 3 pessoas entre as salas. Regua em pessoas: cadeira 1, balcao 3,4, porta 2,2.
    ///    O patio nao muda: o aviao de 9 m tem 18 pessoas de comprimento e a fuselagem de 1 m cabe gente em pe.
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

        /// <summary>Grade das salas, corredor entre elas (metade para cada sala: as paredes ficam recuadas) e o adulto. `Em` mede a partir da parede.</summary>
        public const float Lado = 4f, Corredor = 0.6f, Gente = 0.5f, Interno = Lado - Corredor;
        const float Elev = 45f, VelPax = 1.2f, Troca = 0.35f, EscalaFunc = Gente, PortaLarg = 0.45f;
        /// <summary>Fileiras de quem atende e de quem e atendido, a partir da parede de saida; a fila em pessoas (1,35 por lugar).</summary>
        const float AtrasD = 0.16f, BalcaoD = 0.62f, FrenteD = 1.08f, FaixaD = 1.25f, FilaD = 1.42f, FilaPasso = 0.27f, FilaLinha = 0.34f;
        static readonly float[] Desloc = { -0.6f, 0.6f, -1.3f, 1.3f };   // balcoes ao longo da parede de saida, com o corredor no meio

        /// <summary>Sala de cada estacao: canto (x, z), porta de entrada e de saida (no meio da parede) e a direcao da saida (para fora).</summary>
        struct Sala { public Vector2 Min, In, Out, N, T; }

        static readonly Sala[] Salas =
        {
            Nova(0f, 0f, new Vector2(0f, 2f), new Vector2(4f, 2f), new Vector2(1f, 0f)),     // check-in: entra a oeste, sai a leste
            Nova(4f, 0f, new Vector2(4f, 2f), new Vector2(6f, 4f), new Vector2(0f, 1f)),     // seguranca: entra a oeste, sai ao norte
            Nova(4f, 4f, new Vector2(6f, 4f), new Vector2(8f, 6f), new Vector2(1f, 0f)),     // loja: entra ao sul, sai a leste
            Nova(8f, 4f, new Vector2(8f, 6f), new Vector2(10f, 8f), new Vector2(0f, 1f)),    // portao: entra a oeste, sai ao norte (ponte)
            Nova(0f, 4f, new Vector2(2f, 4f), new Vector2(2f, 8f), new Vector2(0f, 1f)),     // bagagem: entra pelo check-in, balcoes ao norte
            Nova(8f, 0f, new Vector2(10f, 4f), new Vector2(10f, 0f), new Vector2(0f, -1f)),  // alimentacao: entra pelo portao, balcoes ao sul
            Nova(16f, 4f, new Vector2(16f, 6f), new Vector2(18f, 8f), new Vector2(0f, 1f)),  // oficina: entra pelo despacho, sai ao norte (patio)
            Nova(12f, 4f, new Vector2(12f, 6f), new Vector2(16f, 6f), new Vector2(1f, 0f)),  // despacho: entra pelo portao, sai para a oficina
        };

        /// <summary>Porta no meio de uma parede: liga as salas A e B (B = -1: para fora).</summary>
        struct Porta { public int A, B; public Vector2 P; public Porta(int a, int b, float x, float z) { A = a; B = b; P = new Vector2(x, z); } }

        static readonly Porta[] Portas =
        {
            new Porta(0, 1, 4f, 2f), new Porta(1, 2, 6f, 4f), new Porta(2, 3, 8f, 6f), new Porta(0, 4, 2f, 4f), new Porta(3, 5, 10f, 4f), new Porta(3, 7, 12f, 6f), new Porta(7, 6, 16f, 6f),
            new Porta(0, -1, 0f, 2f), new Porta(3, -1, 10f, 8f), new Porta(3, -1, 11.2f, 8f), new Porta(6, -1, 18f, 8f),
        };

        /// <summary>Acrescenta a `r` as portas do caminho da sala `de` ate `para`: busca em largura nas Portas, a partir do destino.</summary>
        public static void PelasPortas(int de, int para, List<Vector2> r)
        {
            var prox = new int[Salas.Length];
            for (int i = 0; i < prox.Length; i++) prox[i] = -1;
            prox[para] = para;
            var fila = new Queue<int>();
            fila.Enqueue(para);
            while (fila.Count > 0)
            {
                int z = fila.Dequeue();
                foreach (Porta p in Portas)
                {
                    int o = p.A == z ? p.B : p.B == z ? p.A : -1;
                    if (o >= 0 && prox[o] < 0) { prox[o] = z; fila.Enqueue(o); }
                }
            }
            for (int z = de; z != para && prox[z] >= 0; z = prox[z])
                foreach (Porta p in Portas)
                    if ((p.A == z && p.B == prox[z]) || (p.B == z && p.A == prox[z])) { r.Add(p.P); break; }
        }

        /// <summary>Malha de balcao de cada estacao e a escala dela (a loja e um quiosque grande: 0,6 cabe 1 por posto). A oficina tem a
        /// bancada aqui e o predio dela fica no mundo (Planta): o mecanico da Fase C mora la antes de a sala abrir.</summary>
        static readonly string[] BalcaoMalha = { "guiche_checkin", "raio_x", "loja", "catraca_embarque", "esteira_bagagem", "alimentacao", "balcao_informacoes", "balcao_informacoes" };
        static readonly float[] BalcaoEscala = { EscalaGuiche, EscalaSeg, 0.6f, 0.55f, EscalaBalcaoMala, 0.6f, 0.75f, 0.75f };
        /// <summary>Nivel de Velocidade a partir do qual o balcao troca pela malha `<malha>_n2` e `_n3` (Lotes 5 e 6): o upgrade muda o predio
        /// (GDD §5.13). Sem a malha daquele nivel, fica a do nivel abaixo.</summary>
        static readonly int[] NivelMalha = { 0, 2, 4 };
        static readonly Color ReservaCor = new Color(1f, 0.93f, 0.78f);
        /// <summary>Placa no chao, do lado de fora da parede mais perto da camera (onde nao ha outra sala).</summary>
        static readonly Vector2[] PlacaEm = { new Vector2(2.2f, -0.9f), new Vector2(5.5f, -0.85f), new Vector2(5f, 5f), new Vector2(8.7f, 8.75f), new Vector2(-0.95f, 5.6f), new Vector2(9.9f, -0.85f), new Vector2(17.7f, 3.2f), new Vector2(13.9f, 3.2f) };

        static Sala Nova(float x, float z, Vector2 entra, Vector2 sai, Vector2 n) =>
            new Sala { Min = new Vector2(x, z), In = entra, Out = sai, N = n, T = new Vector2(n.y, n.x) };

        static readonly Vector2 Calcada = new Vector2(-2.6f, 2f);

        /// <summary>Ponto da sala a `d` da parede de saida e `a` ao longo dela.</summary>
        static Vector2 Em(int e, float d, float a) => Salas[e].Out - Salas[e].N * (d + Corredor * 0.5f) + Salas[e].T * a;
        static Vector2 Balcao(int e, int i) => Em(e, e == SalaCheckIn ? ChkBalcaoD : BalcaoD, Posto(e, i));
        static Vector2 NoBalcao(int e, int i) => Em(e, e == SalaCheckIn ? ChkFrenteD : e == Portao ? BalcaoD + PassoPodio : FrenteD, Posto(e, i));
        static Vector2 AtrasBalcao(int e, int i) => e == SalaSeg ? Agente(i) : Em(e, e == SalaCheckIn ? ChkAtrasD : e == Portao ? BalcaoD - PassoPodio : AtrasD, Posto(e, i));
        static Vector2 Centro(int e) => Salas[e].Min + new Vector2(Lado * 0.5f, Lado * 0.5f);
        /// <summary>Rumo (graus de yaw do Unity) da direcao `d` do chao: +z = 0.</summary>
        static float Rumo(Vector2 d) => Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;

        /// <summary>Lugar k da fila indiana: 5 fileiras de 12 paralelas aos balcoes, em serpentina (60 cabem: o estresse do K3).</summary>
        public static Vector2 Fila(int e, int k)
        {
            int r = Mathf.Min(k / 12, 4), c = k % 12;
            float a = (r % 2 == 0 ? c - 5.5f : 5.5f - c) * FilaPasso;
            return Em(e, FilaD + r * FilaLinha, a);
        }

        public static int SalaEm(Vector2 p)
        {
            for (int e = 0; e < Salas.Length; e++)
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
        readonly Boneco[] _func = new Boneco[Sim.MaxEquipe];
        readonly int[] _colete = { -1, -1, -1, -1, -1, -1 };
        readonly float[] _troca = new float[Sim.MaxEquipe], _falha = new float[Sim.MaxEquipe];
        readonly Estacao[] _rotaPara = new Estacao[Sim.MaxEquipe];
        readonly Text[] _nome = new Text[Sim.MaxEquipe];
        readonly Renderer[] _piso = new Renderer[8];
        readonly Renderer[][] _moldura = new Renderer[8][];
        readonly GameObject[,,] _mesa = new GameObject[8, 4, 3];
        readonly SpriteRenderer[,] _arco = new SpriteRenderer[8, 4], _vazio = new SpriteRenderer[8, 4];
        readonly GameObject[,] _fita = new GameObject[8, 4];
        readonly SpriteRenderer[] _mao = new SpriteRenderer[8];
        readonly TextMesh[] _placa = new TextMesh[8];
        readonly RectTransform[] _selo = new RectTransform[8];
        readonly Text[] _seloTexto = new Text[8];
        readonly Image[] _seloFundo = new Image[8];
        /// <summary>O que aparece quando a sala abre pelo marco (paredes, balcoes, fitas), a sala que esta aberta na tela e os predios por nivel.</summary>
        readonly Transform[] _sala = new Transform[8];
        readonly bool[] _aberta = new bool[8];
        Transform _cont;
        Transform[] _hangar, _torre, _terminal;

        // camera e gesto
        Vector3 _alvo;
        float _tam = 6f;
        bool _enquadrado;
        int _salaNova = -1;
        string _vista;
        Rect _regiao;
        int _press = -2;
        Vector2 _pressPos, _ultimo;
        bool _arrasto, _pan;
        float _pinca;

        Sim S => _g.Sim;
        /// <summary>Funcionario sendo arrastado (-1 = nenhum).</summary>
        public int Arrastando => _arrasto ? _press : -1;

        static readonly Color CorParede = Art.Hex(0xD9DEE8), Fita = Art.Hex(0x2F6FE4), Asfalto = Art.Hex(0x5A5F6B), Grama = Art.Hex(0x7FB069), Trancada = Art.Hex(0xA9ADB5);

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

        // ------------------------------------------------------------------ quadro a quadro

        /// <summary>Hud.LateUpdate: camera, gestos, passageiros, funcionarios, salas, patio e rotulos. `dt` = s de jogo do quadro (0 parado).</summary>
        public void Atualiza(float dt, Rect regiao)
        {
            _regiao = regiao;
            if (!_enquadrado && regiao.height > 10f) Enquadra();
            Entrada();
            Posiciona();
            Sim s = S;
            EmbarquePasso(s, dt);
            SincronizaPax(s, dt);
            Funcionarios(s, dt);
            Salas3D(s);
            Patio(s, dt);
            AndaPatio(dt);
        }

        /// <summary>Largura da tela em m no enquadramento: mais que isso, o corpo do adulto (0,45 m na folha) fica abaixo de 28 px na foto 540 x 960 (ART_BIBLE §3).</summary>
        const float LarguraLeitura = 8.6f;

        /// <summary>Metros por pixel e centro em que `pts` cabem na faixa entre o quadro e a barra de baixo.</summary>
        float Quadro(List<Vector3> pts, out Vector3 centro)
        {
            Vector3 dir = _cam.transform.right, up = _cam.transform.up;
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            foreach (Vector3 p in pts)
            {
                float x = Vector3.Dot(p, dir), y = Vector3.Dot(p, up);
                x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
            }
            centro = dir * ((x0 + x1) * 0.5f) + up * ((y0 + y1) * 0.5f);
            return Mathf.Max((x1 - x0) / _regiao.width, (y1 - y0) / _regiao.height);
        }

        /// <summary>Enquadramento inicial, de perto: as 4 salas do fluxo (o patio e a pista ficam a um pan; a pinca afasta). As salas
        /// abertas entram no quadro enquanto a pessoa ainda se le; depois disso a camera fica no fluxo e a sala que abriu ganha um pan (Evento).</summary>
        void Enquadra()
        {
            Vector3 dir = _cam.transform.right, up = _cam.transform.up;
            var pts = new List<Vector3> { new Vector3(0.1f, 0f, 0.1f), new Vector3(0.1f, 0f, 3.9f), new Vector3(7.9f, 0f, 0.1f), new Vector3(4.1f, 0f, 7.9f), new Vector3(11.9f, 0f, 4.1f), new Vector3(11.9f, 0.6f, 7.9f) };
            float upp = Quadro(pts, out Vector3 centro), teto = LarguraLeitura / Screen.width;
            for (int e = 4; e < Salas.Length; e++)
                if (S.Desbloqueada((Estacao)e))
                    foreach (Vector2 c in new[] { Vector2.zero, new Vector2(Lado, 0f), new Vector2(0f, Lado), new Vector2(Lado, Lado) }) pts.Add(V3(Salas[e].Min + c, 1f));
            float uppTodas = Quadro(pts, out Vector3 centroTodas);
            if (uppTodas <= teto || _vista == "todas") { upp = uppTodas; centro = centroTodas; }
            else
            {
                upp = Mathf.Min(upp, teto);
                if (_salaNova >= 0) { Vector3 n = V3(Centro(_salaNova)); centro = dir * Vector3.Dot(n, dir) + up * Vector3.Dot(n, up); }
            }
            _salaNova = -1;
            _tam = Screen.height * upp * 0.5f;
            Vector2 off = _regiao.center - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            _alvo = NoChao(centro - dir * off.x * upp - up * off.y * upp);
            _enquadrado = true;
            if (_vista == "perto") { _alvo += new Vector3(-1.5f, 0f, -1.2f); _tam *= 0.55f; }
            else if (_vista == "patio") { _alvo = new Vector3(10.5f, 0f, 10.5f); _tam = 9.5f; }
            else if (_vista == "geral") { _alvo = new Vector3(21f, 0f, 13f); _tam = 19f; }
            else if (_vista == "garagem") { _alvo = new Vector3(33f, 0f, 13f); _tam = 9f; }
            else if (_vista == "seguranca") { _alvo = new Vector3(6f, 0f, 2.4f); _tam = 2.4f; }
            else if (_vista == "checkin") { _alvo = V3(Centro(SalaCheckIn)) + new Vector3(0.9f, 0f, 0f); _tam = 2.4f; }
            else if (_vista == "hangar") { _alvo = new Vector3(33f, 0f, 24f); _tam = 11f; }
            else if (_vista == "portao") { _alvo = V3(Centro(Portao)); _tam = 3.1f; }
            else if (_vista == "malas") { _alvo = V3(Centro(SalaMalas)) + new Vector3(0f, 0f, 1.2f); _tam = 3.8f; }
            else if (_vista == "ponte") { _alvo = new Vector3(10.2f, 0f, 11.6f); _tam = 4.6f; }
            else if (_vista == "escada") { _alvo = new Vector3(21f, 0f, 14f); _tam = 4.2f; }
            else if (_vista == "chegadas") { _alvo = new Vector3(4.6f, 0f, 10.2f); _tam = 3.6f; }
            else if (_vista != null && _vista.Length == 5 && _vista.StartsWith("sala")) { _alvo = V3(Centro(_vista[4] - '0')); _tam = 4.4f; }
        }

        /// <summary>Game (-vista, so no -testsession): enquadramento das fotos e do -record (perto = zoom nas salas; hangar = o caminho ate ele; ponte, escada e chegadas = o embarque de perto; patio = salas, estandes
        /// e a ponta da pista; geral = tudo; todas = as salas abertas; sala0 a sala7 = uma sala de perto).</summary>
        public void Vista(string v) { _vista = v; _enquadrado = false; }

        /// <summary>Projeta um ponto no chao (Y = 0) ao longo da direcao da camera.</summary>
        Vector3 NoChao(Vector3 p)
        {
            Vector3 f = _cam.transform.forward;
            return p - f * (p.y / f.y);
        }

        void Posiciona()
        {
            _tam = Mathf.Clamp(_tam, 1.6f, 20f);
            _alvo.x = Mathf.Clamp(_alvo.x, -8f, 40f);
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
            MostraNivel(_hangar, NivelDe(s.St(Estacao.Manutencao).Vel));
            MostraNivel(_torre, Mathf.Min(s.Marco, 2));
            MostraNivel(_terminal, Mathf.Min(s.Marco, 2));
            LinhasAtivas(s);
            for (int e = 0; e < Salas.Length; e++)
            {
                Station st = s.Estacoes[e];
                bool aberta = s.Desbloqueada(st.Tipo);
                if (aberta != _aberta[e] || _sala[e].gameObject.activeSelf != aberta) { _aberta[e] = aberta; _sala[e].gameObject.SetActive(aberta); }
                if (!aberta)
                {
                    Cor(_piso[e], Trancada);
                    string trancada = Textos.PlacaTrancada(e, s.T.Abre[e]);
                    if (_placa[e].text != trancada) { _placa[e].text = trancada; _placa[e].characterSize = 0.03f; }
                    _mao[e].enabled = false;
                    _selo[e].gameObject.SetActive(false);
                    for (int i = 0; i < 4; i++) _vazio[e, i].enabled = false;
                    continue;
                }
                bool fechada = e != (int)Estacao.CheckIn && e != (int)Estacao.Seguranca && e != (int)Estacao.Portao && s.Alocados(st.Tipo) == 0;
                Cor(_piso[e], Color.Lerp(Art.EstacaoCor[e], Color.white, fechada ? 0.1f : 0.55f) * (fechada ? 0.6f : 1f));
                Color cor = st.Estado == Estado.Critico ? Art.Bad : st.Estado == Estado.Atencao ? Art.Warn : Art.Good;
                bool destino = Arrastando >= 0 && s.Staff[Arrastando].Em != st.Tipo;
                if (destino) cor = Color.Lerp(Art.Good, Color.white, sob == e ? 0.6f : 0f);
                float pulso = st.Estado == Estado.Normal && !destino ? 0.55f : 0.6f + 0.4f * Mathf.PingPong(Time.unscaledTime * 2f, 1f);
                foreach (Renderer m in _moldura[e]) { Cor(m, cor * pulso); m.transform.localScale = Escalada(m.transform.localScale, destino ? 0.2f : 0.12f); }
                for (int i = 0; i < 4; i++)
                {
                    int nivel = NivelDe(st.Vel);
                    while (nivel > 0 && _mesa[e, i, nivel] == null) nivel--;
                    for (int n = 0; n < NivelMalha.Length; n++) if (_mesa[e, i, n] != null) _mesa[e, i, n].SetActive(i < st.Postos && n == nivel);
                }
                for (int r = 0; r < 4; r++) if (_fita[e, r] != null) _fita[e, r].SetActive(st.Fila.Count > 12 * (r + 1));   // a fita abre conforme a fila cresce: ninguem a atravessa
                int gente = s.Presentes(st.Tipo);
                for (int i = 0; i < 4; i++)   // posto sem ninguem: um X em cima do balcao
                {
                    bool vazio = i < st.Postos && i >= gente && !(e == Portao && i == 0);   // no 1o podio do portao ha sempre a agente
                    _vazio[e, i].enabled = vazio;
                    EstadoPosto(s, st, e, i, vazio);
                    if (!vazio) continue;
                    Vector3 pv = V3(Balcao(e, i), 0.68f);
                    _vazio[e, i].transform.SetPositionAndRotation(pv, _cam.transform.rotation);
                    _vazio[e, i].sortingOrder = Ordem(pv) + 8;
                }
                bool toque = s.PodeTocar(st.Tipo);
                _mao[e].enabled = toque;
                if (toque)
                {
                    Vector3 pm = V3(Em(e, FrenteD, 0f), 0.95f);
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
                    Coloca(_selo[e], V3(Centro(e), 1.2f));
                    Prende(_selo[e]);
                    _seloTexto[e].text = Textos.EstadoTexto(st.Estado, st.VooEmRisco);
                    _seloTexto[e].color = st.Estado == Estado.Critico ? Art.Bad : Art.Warn;
                }
            }
        }

        /// <summary>Nivel da malha (0..2) pelo nivel de Velocidade.</summary>
        static int NivelDe(int vel)
        {
            int n = 0;
            for (int i = 1; i < NivelMalha.Length; i++) if (vel >= NivelMalha[i]) n = i;
            return n;
        }

        /// <summary>Engrossa a moldura (lado curto) para o destino aceso, sem mexer no comprimento.</summary>
        static Vector3 Escalada(Vector3 s, float g) => s.x < s.z ? new Vector3(g, s.y, s.z) : new Vector3(s.x, s.y, g);

        /// <summary>Game: eventos do nucleo que o mundo mostra (embarque pela ponte, chegada e saida dos avioes).</summary>
        public void Evento(Evento e)
        {
            if (e.Tipo == Ev.Embarcou) _embarcou.Add(e.A);
            else if (e.Tipo == Ev.EstacaoAberta) { _enquadrado = false; _salaNova = e.A; }   // a sala nova entra no quadro
            EventoPatio(e);
        }

        /// <summary>Para a medida do K3 (-perf): pessoas, avioes e veiculos ativos agora.</summary>
        public string Contagem()
        {
            int pessoas = _pax.Count + _patio.Count + _equipe.Count + _andantes.Count + S.Staff.Count, avioes = 0;
            foreach (Aviao a in _avioes) if (a.T.gameObject.activeSelf) avioes++;
            int ligados = 0;   // aproxima as chamadas de desenho: o PC prende em 60 qps e nao mostra piora
            foreach (Renderer r in _raiz.GetComponentsInChildren<Renderer>()) if (r.enabled) ligados++;
            return "pessoas=" + pessoas + " avioes=" + avioes + " veiculos=" + _veiculos.Count + " renderers=" + ligados;
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

        /// <summary>Leva a sala para o meio da faixa do mundo, sem mudar o zoom (toque no aviso de perigo).</summary>
        public void Foca(Estacao e) => _alvo += V3(Chao(ZonaTela(e).center) - Chao(_regiao.center));

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
                int av = _h.Tutorial ? -1 : AviaoEm(sp);
                if (av >= 0) { _h.AbreVoo(av); return; }
                int z = SalaEm(Chao(sp));
                if (z >= 0) _g.Tap((Estacao)z);
            }
        }
    }
}
