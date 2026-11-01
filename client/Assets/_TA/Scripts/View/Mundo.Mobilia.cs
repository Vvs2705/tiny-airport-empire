using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// O que veste as salas, na regua de pessoas (docs/COMPETITIVO.md §h.8): piso de ladrilho, tapete da fila, canteiro de obra da sala trancada,
    /// o movel dos postos e a mobilia. Toda sala segue o mesmo desenho: balcoes na parede de saida, fila no meio sobre o tapete e moveis nas faixas
    /// livres junto as paredes, sem fechar as portas. Peca alta so nas paredes de fundo (norte e leste), que sobem a 0,6 m; as da frente (sul e
    /// oeste) ficam baixas para a camera ver dentro. Check-in e seguranca tem postos proprios, e o portao, Mundo.Espera.cs.
    /// </summary>
    public sealed partial class Mundo
    {
        const float AltoFundo = 0.6f, AltoFrente = 0.14f, EspParede = 0.08f, LadoLadrilho = 0.85f, PosteA = 1.42f, FitaLarg = 2.54f;
        static readonly Color PisoPublico = Art.Hex(0xE8E2D4), PisoServico = Art.Hex(0xAEB6C4), CorCorredor = Art.Hex(0x7C8596), CorCapa = Art.Hex(0xFFFFFF), CorCapacho = Art.Hex(0x3A4254), CorZona = Art.Hex(0xF2C14E);

        readonly Transform[] _obra = new Transform[8];
        readonly Dictionary<(Transform pai, Color cor), List<CombineInstance>> _fixos = new Dictionary<(Transform pai, Color cor), List<CombineInstance>>();
        readonly Dictionary<PrimitiveType, Mesh> _primitiva = new Dictionary<PrimitiveType, Mesh>();
        Material _matPiso;

        /// <summary>Movel fixo: sala, malha, a `D` da parede de saida e `A` ao longo dela, virado para N * FN + T * FT.</summary>
        public readonly struct Mov
        {
            public readonly int E;
            public readonly string Malha;
            public readonly float D, A, FN, FT, Escala;
            public Mov(int e, string malha, float d, float a, float fn, float ft, float escala = 1f) { E = e; Malha = malha; D = d; A = a; FN = fn; FT = ft; Escala = escala; }
            public Vector2 P => Em(E, D, A);
        }

        const int SalaLoja = (int)Estacao.Loja, SalaComida = (int)Estacao.Alimentacao, SalaOficina = (int)Estacao.Manutencao, SalaDespacho = (int)Estacao.Despacho;

        public static readonly Mov[] Moveis =
        {
            new Mov(SalaLoja, "prateleiras_loja", 1.2f, 1.54f, 0f, -1f),
            new Mov(SalaLoja, "prateleiras_loja", 1.8f, 1.54f, 0f, -1f),
            new Mov(SalaLoja, "prateleiras_loja", 2.4f, 1.54f, 0f, -1f),
            new Mov(SalaLoja, "maquina_venda", 3.2f, 1.1f, 1f, 0f),
            new Mov(SalaLoja, "banco", 3.22f, 0.1f, 1f, 0f, 0.62f),
            new Mov(SalaLoja, "vaso_planta", 3.2f, 1.52f, 1f, 0f, 0.7f),
            new Mov(SalaLoja, "vaso_planta", 3.2f, -1.52f, 1f, 0f, 0.7f),

            new Mov(SalaMalas, "conteiner_bagagem", 3.1f, 1.3f, 0f, -1f, 0.42f),
            new Mov(SalaMalas, "conteiner_bagagem", 2.45f, 1.4f, 0f, -1f, 0.42f),
            new Mov(SalaMalas, "placa_direcao", 3.15f, 0.65f, -1f, 0f),
            new Mov(SalaMalas, "vaso_planta", 3.2f, -1.52f, 1f, 0f, 0.7f),

            new Mov(SalaComida, "mesas_alimentacao", 3.05f, -1.05f, 1f, 0f),
            new Mov(SalaComida, "mesas_alimentacao", 3.05f, 0.1f, 1f, 0f),
            new Mov(SalaComida, "bebedouro", 1.15f, 1.52f, 0f, -1f, 0.8f),
            new Mov(SalaComida, "lixeira_terminal", 2.3f, 1.54f, 0f, -1f, 0.6f),
            new Mov(SalaComida, "carrinho_cafe", 2.9f, 1.42f, 0f, -1f),
            new Mov(SalaComida, "vaso_planta", 3.2f, -1.52f, 1f, 0f, 0.7f),

            new Mov(SalaOficina, "escada_manutencao", 1.3f, 1.5f, 0f, -1f),
            new Mov(SalaOficina, "caixa_ferramentas", 1.85f, 1.52f, 0f, -1f),
            new Mov(SalaOficina, "gerador_solo", 2.55f, -0.2f, 0f, -1f),
            new Mov(SalaOficina, "cavalete_manutencao", 3.0f, -0.8f, 1f, 0f),
            new Mov(SalaOficina, "carrinho_limpeza", 2.75f, 1.5f, 0f, -1f),

            new Mov(SalaDespacho, "sofa_lounge", 3.12f, 0.85f, 1f, 0f, 0.7f),
            new Mov(SalaDespacho, "mesa_lounge", 3.1f, -0.75f, 1f, 0f),
            new Mov(SalaDespacho, "bebedouro", 2.75f, 1.52f, 0f, -1f, 0.8f),
            new Mov(SalaDespacho, "vaso_planta", 3.2f, -1.52f, 1f, 0f, 0.7f),
        };

        Mesh Primitiva(PrimitiveType tipo)
        {
            if (_primitiva.TryGetValue(tipo, out Mesh m)) return m;
            GameObject go = GameObject.CreatePrimitive(tipo);
            _primitiva[tipo] = m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            return m;
        }

        /// <summary>Bloco que nunca muda: entra na malha unica da sua cor, na sala em construcao (Funde), e nao custa um draw call por peca.</summary>
        void Fixo(PrimitiveType tipo, Vector3 centro, Vector3 tam, Color cor)
        {
            var chave = (_cont, cor);
            if (!_fixos.TryGetValue(chave, out List<CombineInstance> l)) _fixos[chave] = l = new List<CombineInstance>();
            l.Add(new CombineInstance { mesh = Primitiva(tipo), transform = Matrix4x4.TRS(centro, Quaternion.identity, tam) });
        }

        void Funde()
        {
            foreach (var kv in _fixos)
            {
                var malha = new Mesh { name = "Fixos" };
                malha.CombineMeshes(kv.Value.ToArray(), true, true);
                var go = new GameObject("Fixos");
                go.transform.SetParent(kv.Key.pai != null ? kv.Key.pai : _raiz, false);
                go.AddComponent<MeshFilter>().sharedMesh = malha;
                var r = go.AddComponent<MeshRenderer>();
                if (Material != null) r.sharedMaterial = Material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                Cor(r, kv.Key.cor);
            }
            _fixos.Clear();
        }

        static Color PisoCor(int e) => Color.Lerp(e == SalaOficina || e == SalaDespacho ? PisoServico : PisoPublico, Art.EstacaoCor[e], 0.1f);

        /// <summary>Quatro ladrilhos claros com rejunte, repetidos no piso; a cor da sala vem do _Color.</summary>
        static Texture2D Ladrilho()
        {
            const int n = 64, meio = n / 2;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "Ladrilho", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float v = x % meio < 2 || y % meio < 2 ? 0.84f : (x / meio + y / meio) % 2 == 0 ? 1f : 0.96f;
                    t.SetPixel(x, y, new Color(v, v, v));
                }
            t.Apply(true, true);
            return t;
        }

        Material MatPiso()
        {
            if (_matPiso == null) _matPiso = new Material(Material) { name = "Piso", mainTexture = Ladrilho(), mainTextureScale = Vector2.one * (Interno / LadoLadrilho) };
            return _matPiso;
        }

        /// <summary>Tapete a `d` da parede de saida (sob a fila, ou na sala de estar do despacho): a cor da estacao fica aqui, com borda mais forte, e o piso segue claro.</summary>
        void Tapete(int e, float d, float larg, float prof)
        {
            Vector2 c = Em(e, d, 0f);
            bool x = Mathf.Abs(Salas[e].N.x) > 0.5f;
            Vector3 tam(float l, float p) => x ? new Vector3(p, 0.004f, l) : new Vector3(l, 0.004f, p);
            Fixo(PrimitiveType.Cube, V3(c, 0.002f), tam(larg, prof), Art.EstacaoCor[e]);
            Fixo(PrimitiveType.Cube, V3(c, 0.004f), tam(larg - 0.1f, prof - 0.1f), Color.Lerp(Art.EstacaoCor[e], PisoCor(e), 0.62f));
        }

        /// <summary>Zona de trabalho pintada no piso da oficina, em volta do gerador.</summary>
        void ZonaOficina(int e)
        {
            const float larg = 1.6f, prof = 1.2f, esp = 0.05f;
            Vector2 c = Em(e, 2.55f, -0.2f), t = Salas[e].T * (larg * 0.5f), n = Salas[e].N * (prof * 0.5f);
            foreach (Vector2 p in new[] { c + t, c - t }) Fixo(PrimitiveType.Cube, V3(p, 0.002f), new Vector3(esp, 0.004f, prof + esp), CorZona);
            foreach (Vector2 p in new[] { c + n, c - n }) Fixo(PrimitiveType.Cube, V3(p, 0.002f), new Vector3(larg + esp, 0.004f, esp), CorZona);
        }

        /// <summary>Capacho nas portas: marca a passagem no corredor escuro.</summary>
        void Capachos()
        {
            foreach (Porta p in Portas)
                Fixo(PrimitiveType.Cube, V3(p.P, 0.002f), Mathf.Repeat(p.P.x, Lado) < 0.01f ? new Vector3(Corredor, 0.004f, PortaLarg + 0.1f) : new Vector3(PortaLarg + 0.1f, 0.004f, Corredor), CorCapacho);
        }

        /// <summary>Sala trancada: paredes de pe e canteiro de obra no piso cinza, ligados enquanto ela nao abre.</summary>
        void Obra(int e)
        {
            _cont = _obra[e] = new GameObject("Obra" + e).transform;
            _cont.SetParent(_raiz, false);
            Vector2 n = Salas[e].N, t = Salas[e].T;
            Peca("barreira_obra", V3(Em(e, 1.7f, -0.55f)), Rumo(-n));
            Peca("barreira_obra", V3(Em(e, 1.7f, 0.55f)), Rumo(-n));
            Peca("cone", V3(Em(e, 2.3f, -1f)), 0f, 0.7f);
            Peca("cone", V3(Em(e, 2.3f, 1f)), 0f, 0.7f);
            Peca("conteiner_bagagem", V3(Em(e, 0.9f, 0.9f)), Rumo(-t), 0.42f);
            _cont = null;
        }

        void Mobilia(int e)
        {
            foreach (Mov m in Moveis)
                if (m.E == e) Peca(m.Malha, V3(m.P), Rumo(Salas[e].N * m.FN + Salas[e].T * m.FT), m.Escala);
            if (e == SalaOficina) ZonaOficina(e);
            else if (e == SalaDespacho) { Tapete(e, 2.75f, 2.4f, 1f); Painel(e, 1.75f, 1.3f, Art.Board, Art.BoardInk); }
            else if (e == SalaComida) Painel(e, 2.6f, 0.7f, Art.Board, Art.Paper);
            else if (e == SalaMalas) Painel(e, 1.2f, 0.8f, Art.Board, Art.BoardInk);
        }

        /// <summary>Painel na parede de fundo do lado +T da sala: fundo escuro e 3 linhas acesas (quadro de voos, cardapio).</summary>
        void Painel(int e, float d, float larg, Color fundo, Color linha)
        {
            Vector2 p = Em(e, d, Interno * 0.5f - 0.05f);
            bool x = Mathf.Abs(Salas[e].T.x) > 0.5f;
            Vector3 tam(float l, float h) => x ? new Vector3(0.02f, h, l) : new Vector3(l, h, 0.02f);
            Fixo(PrimitiveType.Cube, V3(p, 0.4f), tam(larg, 0.3f), fundo);
            for (int i = 0; i < 3; i++)
                Fixo(PrimitiveType.Cube, V3(p - Salas[e].T * 0.012f, 0.32f + i * 0.08f), tam(larg - 0.1f, 0.03f), linha);
        }

        /// <summary>Nivel `n` do posto da loja e da alimentacao: as malhas loja e alimentacao sao miniaturas de sala com moveis menores que o passageiro.
        /// Aqui o movel e sempre da regua, e o upgrade acrescenta o caixa no nivel 2 e o telao atras do balcao no nivel 3.</summary>
        Transform MesaNivel(int e, int n, Vector2 b)
        {
            if (e != SalaLoja && e != SalaComida) return null;
            bool bar = e == SalaComida && n == 2;
            Transform t = Peca(bar ? "bar_lounge" : BalcaoMalha[e], V3(b), Rumo(-Salas[e].N), bar ? 1f : BalcaoEscala[e]);
            if (t == null) return null;
            Transform cont = _cont;
            _cont = t;
            Peca("caixa_registradora", new Vector3(0.18f, 0.45f, 0.1f), 0f);
            _cont = cont;
            if (n == 2) Bloco("Telao", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0.06f - BalcaoD), new Vector3(0.46f, 0.2f, 0.03f), Art.EstacaoCor[e], t);
            return t;
        }
    }
}
