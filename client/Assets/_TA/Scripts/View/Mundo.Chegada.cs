using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Saguao de chegada com a esteira de restituicao (frente H), so apresentacao: tudo segue sim.Chegantes, sim.Esteira e sim.Carrinho
    /// (Sim.Chegada.cs); nada aqui muda regra.
    ///  - Planta: sala de 8 x 4 ao sul da seguranca e da alimentacao (a frente da camera), com porta ao norte, no fim do corredor entre as
    ///    salas, e porta a oeste para a calcada da rua. No meio, a esteira em volta retangular (a folha tem 4 rumos, os eixos do mundo:
    ///    ninguem anda na diagonal), com a ilha no meio, onde o carrinho empilha a fila de malas, e a boca na ponta leste.
    ///  - Quem desce: aparece na porta do aviao quando o nucleo o solta (desembarque do patio) e o aviao da tela ja parou; desce pela ponte
    ///    (G) ou pela escada (R, Mundo.Embarque), segue o passeio norte ate o corredor entre a loja e o portao, desce o corredor ate a porta
    ///    norte e anda pela faixa de fora da esteira ate o seu lugar (Chegante.Vaga): ninguem atravessa a esteira nem a parede.
    ///  - Espera de frente para a esteira. A mala dele (cor da pessoa) gira nos lugares do nucleo; quando ela passa em frente (o nucleo diz
    ///    Pegando), encosta, faz o gesto de pegar e a folha troca para a da mala grande. Depois sai pela porta oeste e some na calcada.
    ///  - Quem nao tem mala despachada passa pela faixa direto para a saida, ja com a mala de mao.
    ///  - Malhas do Tripo: esteira_restituicao e carrinho_passageiro (client/tools/malhas_ta.py); sem elas, as primitivas daqui.
    /// </summary>
    public sealed partial class Mundo
    {
        /// <summary>Canto, centro da esteira, meias medidas da volta das malas e o afastamento da borda da esteira ate a volta.</summary>
        public static readonly Vector2 SaguaoMin = new Vector2(4f, -4f), EsteiraC = new Vector2(8f, -2f);
        public const float SaguaoL = 8f, SaguaoP = 4f, VoltaX = 0.7f, VoltaZ = 0.3f, BordaVolta = 0.1f;
        /// <summary>Fileiras em volta (a 0,28 m uma da outra, a 1a a 0,28 m da borda) e a faixa de quem anda, por fora delas: a 1,22 m da volta, o corpo
        /// (0,1 m) fica a 0,04 m da face das paredes norte e sul (a sala tem 3,4 m por dentro).</summary>
        const float Fileira = 0.28f, Faixa = 1.12f, EscalaCarrinhoMao = 1f;
        const int FileirasEsteira = 3, NaIlha = 5;
        /// <summary>Volta (fracao 0..1, a partir do meio do lado leste, anti-horario) da porta norte e da porta oeste na faixa.</summary>
        const float UNorte = 0.25f, UOeste = 0.5f;
        /// <summary>Portas, o fim da calcada na rua e a boca do corredor entre a loja e o portao, no passeio norte.</summary>
        public static readonly Vector2 PortaNorte = new Vector2(8f, -0.3f), PortaOeste = new Vector2(4.3f, -2f), SaidaRua = new Vector2(-2.6f, -2f), BocaCorredor = new Vector2(8f, PasseioZ);
        /// <summary>Cor da mala de quem chega: uma por pessoa (a mala certa se ve), longe das 2 cores de voo do quadro.</summary>
        static readonly Color[] CorMala = { Art.Hex(0x7E57C2), Art.Hex(0x26A69A), Art.Hex(0xEC407A), Art.Hex(0x8D6E63), Art.Hex(0xFFCA28), Art.Hex(0x5C6BC0) };
        static readonly Color CorPisoChegada = Art.Hex(0xDCE6EE), CorPlaca = Art.Hex(0x26A69A);

        sealed class Chegou { public Boneco B; public int Id, Vaga = -1; public EstadoChegada Visto; public bool Saindo; }

        readonly Dictionary<int, Chegou> _chegando = new Dictionary<int, Chegou>();
        readonly HashSet<int> _chegVistos = new HashSet<int>();
        readonly List<int> _chegFim = new List<int>();
        Mala[] _malaVolta, _malaIlha;

        static int Hash(int id) => (int)(((uint)id * 2654435761u) >> 24);
        static Color CorDaMala(int id) => CorMala[Hash(id) % CorMala.Length];
        static string CorpoChegada(int id) => PaxAtor[Hash(id) % PaxAtor.Length];

        // ------------------------------------------------------------------ geometria

        /// <summary>Ponto da volta `u` (0..1, a partir do meio do lado leste, anti-horario) da volta das malas, levado `o` m para fora pela normal
        /// do lado: o lugar de cada fileira e o da faixa ficam na mesma reta que o lugar da esteira, num eixo do mundo.</summary>
        static Vector2 NaVolta(float u, float o)
        {
            float x = VoltaX, z = VoltaZ, s = Mathf.Repeat(u, 1f) * 4f * (x + z);
            if (s < z) return EsteiraC + new Vector2(x + o, s);
            s -= z;
            if (s < 2f * x) return EsteiraC + new Vector2(x - s, z + o);
            s -= 2f * x;
            if (s < 2f * z) return EsteiraC + new Vector2(-x - o, z - s);
            s -= 2f * z;
            if (s < 2f * x) return EsteiraC + new Vector2(-x + s, -z - o);
            s -= 2f * x;
            return EsteiraC + new Vector2(x + o, -z + s);
        }

        /// <summary>As quinas da volta, em fracao (NE, NO, SO, SE), e a quina do retangulo `o` m para fora.</summary>
        static readonly float[] Quinas = { VoltaZ / (4f * (VoltaX + VoltaZ)), (VoltaZ + 2f * VoltaX) / (4f * (VoltaX + VoltaZ)), (3f * VoltaZ + 2f * VoltaX) / (4f * (VoltaX + VoltaZ)), (3f * VoltaZ + 4f * VoltaX) / (4f * (VoltaX + VoltaZ)) };
        static Vector2 Quina(int i, float o) => EsteiraC + new Vector2((i == 0 || i == 3 ? 1f : -1f) * (VoltaX + o), (i < 2 ? 1f : -1f) * (VoltaZ + o));

        /// <summary>Afastamento da fileira `f` (0 colada na esteira) a partir da volta das malas.</summary>
        static float Afasta(int f) => BordaVolta + Fileira * (Mathf.Min(f, FileirasEsteira - 1) + 1);

        /// <summary>Lugar em volta da esteira da vaga `v` do nucleo, com `n` lugares na esteira.</summary>
        public static Vector2 LugarEsteira(int v, int n) => NaVolta((float)(v % n) / n, Afasta(v / n));

        /// <summary>Pela faixa de fora, de `a` ate `b` (fracoes), pelo lado mais curto, quina a quina.</summary>
        static void PelaFaixa(List<Vector2> r, float a, float b)
        {
            float ccw = Mathf.Repeat(b - a, 1f);
            bool anti = ccw <= 0.5f;
            float total = anti ? ccw : 1f - ccw;
            var ds = new List<Vector2>();   // (distancia, quina)
            for (int i = 0; i < Quinas.Length; i++)
            {
                float d = anti ? Mathf.Repeat(Quinas[i] - a, 1f) : Mathf.Repeat(a - Quinas[i], 1f);
                if (d > 1e-4f && d < total - 1e-4f) ds.Add(new Vector2(d, i));
            }
            ds.Sort((p, q) => p.x.CompareTo(q.x));
            foreach (Vector2 d in ds) r.Add(Quina((int)d.y, BordaVolta + Faixa));
            r.Add(NaVolta(b, BordaVolta + Faixa));
        }

        /// <summary>Do passeio norte ate o lugar `vaga` (ou ate a calcada, sem vaga): corredor entre as salas, porta norte e a faixa.</summary>
        public static void RotaChegada(List<Vector2> r, int vaga, int n)
        {
            r.Add(BocaCorredor);
            r.Add(new Vector2(PortaNorte.x, 0f));
            r.Add(NaVolta(UNorte, BordaVolta + Faixa));
            if (vaga < 0) { PelaFaixa(r, UNorte, UOeste); Saida(r); return; }
            float u = (float)(vaga % n) / n;
            PelaFaixa(r, UNorte, u);
            r.Add(LugarEsteira(vaga, n));
        }

        /// <summary>Do lugar em volta da esteira (fracao `u`) ate a calcada: para fora ate a faixa, pela faixa ate a porta oeste e pela calcada ate a rua.</summary>
        public static void RotaSaida(List<Vector2> r, float u)
        {
            r.Add(NaVolta(u, BordaVolta + Faixa));
            PelaFaixa(r, u, UOeste);
            Saida(r);
        }

        static void Saida(List<Vector2> r)
        {
            r.Add(PortaOeste);
            r.Add(SaidaRua);
        }

        // ------------------------------------------------------------------ construcao

        /// <summary>Planta, antes do Funde: piso, paredes com as 2 portas, letreiro, a esteira, os carrinhos e a calcada ate a rua.</summary>
        void Chegada3D()
        {
            Vector2 m = SaguaoMin + Vector2.one * (Corredor * 0.5f);
            float l = SaguaoL - Corredor, p = SaguaoP - Corredor;
            Bloco("PisoChegada", PrimitiveType.Cube, new Vector3(m.x + l * 0.5f, -0.05f, m.y + p * 0.5f), new Vector3(l, 0.1f, p), CorPisoChegada);
            Chao("CalcadaChegada", new Vector3((SaidaRua.x - 0.6f + PortaOeste.x) * 0.5f, -0.04f, PortaOeste.y), new Vector2(PortaOeste.x - SaidaRua.x + 0.6f, 0.7f), PisoPasseio);
            ParedeSaguao(new Vector2(m.x, m.y), new Vector2(m.x + l, m.y), AltoFrente, -1f);                 // sul
            ParedeSaguao(new Vector2(m.x, m.y + p), new Vector2(m.x + l, m.y + p), AltoFundo, PortaNorte.x);  // norte, com a porta do corredor
            ParedeSaguao(new Vector2(m.x, m.y), new Vector2(m.x, m.y + p), AltoFrente, PortaOeste.y);         // oeste, com a porta da rua
            ParedeSaguao(new Vector2(m.x + l, m.y), new Vector2(m.x + l, m.y + p), AltoFundo, -1f);           // leste
            PlacaSaguao(V3(new Vector2(EsteiraC.x + 2.2f, m.y + p), AltoFundo + 0.2f));
            Esteira3D_Restituicao();
            for (int i = 0; i < 3; i++)   // carrinhos de mao encostados no canto sudoeste, longe da faixa: so decoracao
            {
                Vector3 c = new Vector3(m.x + 0.45f + i * 0.3f, 0f, m.y + 0.35f);
                if (Peca("carrinho_passageiro", c, 0f, EscalaCarrinhoMao) != null) continue;
                Bloco("CarrinhoMao", PrimitiveType.Cube, c + Vector3.up * 0.1f, new Vector3(0.2f, 0.2f, 0.32f), Art.Hex(0x9AA6BC));
            }
        }

        /// <summary>Parede de `a` a `b` com o vao de uma porta em `porta` (coordenada ao longo dela; &lt; 0 = sem porta).</summary>
        void ParedeSaguao(Vector2 a, Vector2 b, float alto, float porta)
        {
            Vector2 d = (b - a).normalized;
            bool x = Mathf.Abs(d.x) > 0.5f;
            float len = Vector2.Distance(a, b), t = porta < 0f ? -1f : x ? porta - a.x : porta - a.y;
            foreach (Vector2 v in t > 0f && t < len ? new[] { new Vector2(0f, t - PortaLarg * 0.5f), new Vector2(t + PortaLarg * 0.5f, len) } : new[] { new Vector2(0f, len) })
            {
                Vector2 c = a + d * ((v.x + v.y) * 0.5f);
                float l = v.y - v.x + 0.06f;
                Vector3 tam(float h, float esp) => x ? new Vector3(l, h, esp) : new Vector3(esp, h, l);
                Fixo(PrimitiveType.Cube, new Vector3(c.x, alto * 0.5f, c.y), tam(alto, EspParede), CorParede);   // como o Muro das salas, com o rodape na cor do saguao
                Fixo(PrimitiveType.Cube, new Vector3(c.x, alto + 0.012f, c.y), tam(0.024f, EspParede + 0.03f), CorCapa);
                if (alto > AltoFrente) Fixo(PrimitiveType.Cube, new Vector3(c.x, 0.09f, c.y), tam(0.18f, EspParede + 0.012f), CorPlaca);
            }
        }

        void PlacaSaguao(Vector3 p)
        {
            Quaternion rosto = Quaternion.Euler(20f, 0f, 0f);
            Bloco("PlacaChegada", PrimitiveType.Cube, p, new Vector3(2.4f, 0.34f, 0.03f), CorPlaca).transform.rotation = rosto;
            var go = new GameObject("PlacaChegadaNome");
            go.transform.SetParent(_raiz, false);
            go.transform.SetPositionAndRotation(p + rosto * Vector3.back * 0.02f, rosto);
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = Art.Font();
            tm.fontSize = 72;
            tm.characterSize = 0.03f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = Art.Ink;
            tm.text = Textos.Restituicao;
            go.GetComponent<MeshRenderer>().sharedMaterial = Art.Font().material;
        }

        /// <summary>A esteira: a malha do Tripo (sem malas: as malas sao as do nucleo) na medida de 1,6 x 0,8 m, ou base, chapas, ilha e boca em
        /// primitivas. As malas da volta e da ilha nascem no 1o quadro, quando o nucleo diz quantos lugares ha.</summary>
        void Esteira3D_Restituicao()
        {
            float bx = VoltaX + BordaVolta, bz = VoltaZ + BordaVolta;
            Transform t = Peca("esteira_restituicao", V3(EsteiraC), 0f);
            if (t != null)
            {
                Bounds b = Caixa(t);
                if (b.size.x > 0.01f) t.localScale = Vector3.one * (2f * bx / b.size.x);
                return;
            }
            Bloco("EsteiraBase", PrimitiveType.Cube, V3(EsteiraC, 0.1f), new Vector3(2f * bx + 0.08f, 0.2f, 2f * bz + 0.08f), CorEsteira);
            Bloco("EsteiraChapas", PrimitiveType.Cube, V3(EsteiraC, 0.205f), new Vector3(2f * bx, 0.012f, 2f * bz), CorBorda);
            Bloco("EsteiraIlha", PrimitiveType.Cube, V3(EsteiraC, 0.25f), new Vector3(2f * (VoltaX - 0.12f), 0.08f, 2f * (VoltaZ - 0.12f)), CorBoca);
            Bloco("EsteiraBoca", PrimitiveType.Cube, V3(EsteiraC + new Vector2(VoltaX - 0.16f, 0f), 0.31f), new Vector3(0.08f, 0.14f, 0.26f), CorVerga);
        }

        // ------------------------------------------------------------------ quadro a quadro

        /// <summary>O aviao da tela ja parou no estande: quem o nucleo soltou comeca a descer.</summary>
        bool PodeDescer(int k) => k >= 0 && k < _avioes.Length && _avioes[k].Fase == Fase.NoPatio;

        void ChegadaPasso(Sim s, float dt)
        {
            int n = s.Esteira.Length;
            MalasDaEsteira(s, n);
            _chegVistos.Clear();
            foreach (Chegante c in s.Chegantes)
            {
                if (c.Estado == EstadoChegada.NoAviao) continue;
                if (!_chegando.TryGetValue(c.Id, out Chegou g))
                {
                    bool fresco = c.Estado == EstadoChegada.Andando && c.Resto > (s.T.ChegadaAndar[Mathf.Clamp(c.Aviao, 0, s.T.ChegadaAndar.Length - 1)] - 8f) * Sim.Hz;
                    if (fresco && !PodeDescer(c.Aviao)) continue;   // o aviao da tela ainda taxia: desce quando parar
                    _chegando[c.Id] = g = NovaChegada(c, n, fresco);
                }
                _chegVistos.Add(c.Id);
                SegueChegada(g, c, n);
                float vel = c.Estado != EstadoChegada.Andando ? VelMax : c.Mala ? Mathf.Max(VelPax, Resta(g.B) / Mathf.Max(0.5f, c.Resto * Sim.Step - 1f)) : VelPax;   // quem tem vaga chega antes do nucleo dizer que chegou
                Anda(g.B, vel, dt);
                if (g.B.Caminho.Count == 0 && !NoSaguao(g.B.Pos)) Refaz(g, c, n);
                if (g.B.Caminho.Count == 0 && g.Vaga < 0) g.B.T.gameObject.SetActive(false);   // sem mala: chegou a rua antes de o nucleo o tirar
                if (g.B.Caminho.Count == 0 && c.Estado != EstadoChegada.Andando) Olha(g.B, ParaEsteira(LugarEsteira(g.Vaga, n)));
                PintaChegada(g, c, s, n);
            }
            _chegFim.Clear();
            foreach (KeyValuePair<int, Chegou> kv in _chegando)
            {
                Chegou g = kv.Value;
                if (_chegVistos.Contains(kv.Key)) continue;
                if (!g.Saindo) SaiChegada(g, n);
                Anda(g.B, VelPax, dt);
                g.B.Gesto = -1;
                Pinta(g.B);
                if (g.B.Caminho.Count == 0) _chegFim.Add(kv.Key);
            }
            foreach (int id in _chegFim)
            {
                Boneco b = _chegando[id].B;
                _chegando.Remove(id);
                b.T.gameObject.SetActive(false);
                _livres.Push(b);
            }
        }

        static bool NoSaguao(Vector2 p) => p.x > SaguaoMin.x + Corredor * 0.5f && p.x < SaguaoMin.x + SaguaoL - Corredor * 0.5f && p.y > SaguaoMin.y + Corredor * 0.5f && p.y < SaguaoMin.y + SaguaoP - Corredor * 0.5f;

        /// <summary>Rede de protecao: parou fora do saguao (o aviao saiu com a pessoa ainda na porta, sem a escada; ou a sem mala na rua, que sai
        /// do nucleo no quadro seguinte). Com vaga, volta pelo passeio norte, sem cortar caminho; sem vaga, fica: a saida a recolhe.</summary>
        void Refaz(Chegou g, Chegante c, int n)
        {
            if (g.Vaga < 0 || g.B.Pos == SaidaRua) return;
            g.B.Pos = BocaCorredor;
            g.B.Estande = -1;
            RotaChegada(g.B.Caminho, g.Vaga, n);
        }

        static float Resta(Boneco b)
        {
            float d = 0f;
            Vector2 q = b.Pos;
            foreach (Vector2 c in b.Caminho) { d += Vector2.Distance(q, c); q = c; }
            return d;
        }

        /// <summary>De frente para a esteira: o eixo do mundo que aponta do lugar para o meio dela.</summary>
        static Vector2 ParaEsteira(Vector2 lugar)
        {
            Vector2 d = EsteiraC - lugar;
            float ex = Mathf.Abs(d.x) - VoltaX, ez = Mathf.Abs(d.y) - VoltaZ;
            return ex > ez ? new Vector2(Mathf.Sign(d.x), 0f) : new Vector2(0f, Mathf.Sign(d.y));
        }

        Chegou NovaChegada(Chegante c, int n, bool fresco)
        {
            string corpo = CorpoChegada(c.Id) + (c.Mala ? "_mao" : "");
            Boneco b;
            if (_livres.Count > 0) { b = _livres.Pop(); Veste(b, corpo); } else b = NovoBoneco(corpo);
            b.T.gameObject.SetActive(true);
            b.Zona = -1; b.Estande = -1; b.Saindo = false; b.NoBalcao = false; b.VelAtual = 0f; b.Senta = 0f; b.Cadeira = b.Vaga = -1; b.Etapa = 0; b.RotT = b.RotDur = 0f;
            b.SoMao = c.Mala; b.Despachou = false; b.Gesto = -1;
            b.Coracao.enabled = b.Humor.enabled = false;
            b.Tinta = CorDaMala(c.Id);
            b.Caminho.Clear();
            var g = new Chegou { B = b, Id = c.Id, Visto = EstadoChegada.NoAviao, Vaga = c.Vaga };   // Visto atras: o 1o SegueChegada aplica o estado (save aberto pegando)
            if (fresco)
            {
                Trilha(b, BocaCorredor, c.Aviao, true);   // da porta do aviao ao passeio norte (Mundo.Embarque): ponte ou escada
                b.Caminho.RemoveAt(b.Caminho.Count - 1);
                RotaChegada(b.Caminho, c.Mala ? c.Vaga : -1, n);
            }
            else if (c.Estado == EstadoChegada.Andando)
            {
                b.Pos = BocaCorredor;   // save aberto no meio da caminhada: vem do passeio
                RotaChegada(b.Caminho, c.Mala ? c.Vaga : -1, n);
            }
            else b.Pos = LugarEsteira(c.Vaga, n);   // o save abre com cada um no lugar
            return g;
        }

        /// <summary>O nucleo mudou o estado: pegando = encosta na esteira (fileira da frente) e a mala passa para a mao.</summary>
        void SegueChegada(Chegou g, Chegante c, int n)
        {
            if (c.Estado == g.Visto) return;
            g.Visto = c.Estado;
            Boneco b = g.B;
            if (c.Estado == EstadoChegada.Pegando)
            {
                Vector2 frente = NaVolta((float)(c.Vaga % n) / n, Afasta(0));
                if (b.Caminho.Count == 0) b.Caminho.Add(frente);
                else b.Caminho[b.Caminho.Count - 1] = LugarEsteira(c.Vaga, n);
                if (b.Caminho[b.Caminho.Count - 1] != frente) b.Caminho.Add(frente);
                Veste(b, CorpoChegada(c.Id));   // a mala grande, na cor dela, vai para a mao
                b.SoMao = false;
            }
        }

        void PintaChegada(Chegou g, Chegante c, Sim s, int n)
        {
            Boneco b = g.B;
            b.Gesto = -1;
            b.NoBalcao = c.Estado == EstadoChegada.Pegando;
            if (c.Estado == EstadoChegada.Pegando && b.Caminho.Count == 0 && b.Folha != null)
            {
                b.Gesto = b.Pega;
                b.GestoT = 1f - c.Resto / Mathf.Max(1f, s.T.PegarSeg * Sim.Hz);
            }
            Pinta(b);
            b.Gesto = -1;
        }

        /// <summary>Saiu do nucleo: com a mala (ou sem mala despachada) vai pela faixa ate a porta oeste e pela calcada ate a rua.</summary>
        void SaiChegada(Chegou g, int n)
        {
            g.Saindo = true;
            Boneco b = g.B;
            b.NoBalcao = false;
            if (g.Vaga < 0) return;   // sem mala: a rota ja termina na rua
            if (b.SoMao) { Veste(b, CorpoChegada(g.Id)); b.SoMao = false; }   // a tela nao viu o Pegando (quadro longo): a mala vai para a mao aqui
            RotaSaida(b.Caminho, (float)(g.Vaga % n) / n);   // depois do lugar (se ainda nao chegou): para fora, nunca pela esteira
        }

        /// <summary>As malas que giram (uma por lugar, na posicao do nucleo, com a volta continua entre os passos) e as da fila na ilha.</summary>
        void MalasDaEsteira(Sim s, int n)
        {
            if (_malaVolta == null)
            {
                _malaVolta = new Mala[n];
                for (int j = 0; j < n; j++) _malaVolta[j] = NovaMala("Mala");
                _malaIlha = new Mala[NaIlha];
                for (int i = 0; i < NaIlha; i++) _malaIlha[i] = NovaMala("Mala");
            }
            float giro = (float)((double)s.Steps / s.EsteiraPassos);
            for (int j = 0; j < n && j < _malaVolta.Length; j++)
            {
                Mala m = _malaVolta[j];
                int dono = s.Esteira[j];
                m.T.gameObject.SetActive(dono >= 0);
                if (dono < 0) continue;
                float u = Mathf.Repeat((j + giro) / n, 1f);
                Vector2 p = NaVolta(u, 0f), q = NaVolta(u + 0.01f, 0f);
                Cor(m.R, CorDaMala(dono));
                Deita(m, V3(p, 0.21f), Rumo(q - p), EscalaMala);
            }
            for (int i = 0; i < NaIlha; i++)
            {
                Mala m = _malaIlha[i];
                bool tem = i < s.Carrinho.Count;
                m.T.gameObject.SetActive(tem);
                if (!tem) continue;
                Cor(m.R, CorDaMala(s.Carrinho[i]));
                Deita(m, V3(EsteiraC + new Vector2(VoltaX - 0.3f - i * 0.2f, 0f), 0.29f), 0f, EscalaMala);
            }
        }

        // ------------------------------------------------------------------ -diag e testes

        /// <summary>-diag: as 2 metades do saguao como salas do diag_caixas.py (pessoa x parede e pessoa x esteira).</summary>
        void DiagSaguao(System.IFormatProvider inv)
        {
            for (int i = 0; i < 2; i++)
                _diag.WriteLine(string.Format(inv, "0,sala#{0},{1:0.000},0,{2:0.000},0,{3:0.000}", Salas.Length + i, SaguaoMin.x + i * Lado, SaguaoMin.y, Lado));
        }

        void DiagChegada(System.IFormatProvider inv)
        {
            foreach (KeyValuePair<int, Chegou> kv in _chegando) DiagBoneco(inv, "pax#c" + kv.Key, kv.Value.B);
        }

        /// <summary>Alguem que chega ainda no trilho do aviao `k` (o aviao espera, como no embarque).</summary>
        bool ChegandoNoTrilho(int k)
        {
            foreach (Chegou g in _chegando.Values) if (NoCaminho(g.B, k)) return true;
            return false;
        }
    }
}
