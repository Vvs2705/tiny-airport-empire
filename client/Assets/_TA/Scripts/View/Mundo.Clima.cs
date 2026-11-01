using System;
using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// O que a Fase C4 simula aparece no cenario (GDD §5.8 e §5.14), so tela: le sim.Inc e o estado dos avioes, sem mudar regra. O aviso
    /// escrito e as opcoes ficam no Hud. Um perigo por vez (o nucleo garante): ha um totem so, um guia so, um tecnico so.
    ///  - Clima: veu sobre o mundo e 2 camadas de riscos em movimento, embaixo dos nomes e das barras. O ceu fecha ja no aviso (ForcaAviso) e
    ///    a forca e total quando o clima comeca. O temporal tem clarao, no maximo 1 a cada RaioMin s, sem raio nem fogo no chao. A neve cobre o gramado.
    ///  - Pista fechada (clima, rejeicao e pouso de retorno): X sobre a pista.
    ///  - Incidente: o totem fica na sala atingida (SalaDo) desde o aviso: amarelo no aviso, vermelho em curso, verde resolvido.
    ///  - Falha de equipamento: raio sobre os balcoes e cones; no reparo, o anel de progresso e o tecnico, que entra pela porta da sala.
    ///  - Pico: o onibus para na calcada do check-in e a guia com a bandeira entra na sala. Conexao: a guia entra na seguranca.
    ///  - Aviao em pane: cones na frente do nariz e alerta ate o mecanico chegar (MecanicoPasso ja vai ao aviao em inspecao).
    /// </summary>
    public sealed partial class Mundo
    {
        /// <summary>Como o clima aparece: veu (cor e alfa), camadas (alfa, velocidade em telhas por s, para esquerda e para baixo, e tamanho da
        /// telha) e os N riscos da textura, de meio-eixo Rx por Ry px, com o eixo Ry na direcao do movimento.</summary>
        public sealed class Tempo
        {
            public Color Veu; public float VeuAlfa, Alfa, Tam = 1f, Rx, Ry; public int N; public Vector2 Vel; public bool Raios;
            public Texture2D Tex;
        }

        /// <summary>Indice = TipoIncidente; null = nao e' clima.</summary>
        static readonly Tempo[] Tempos =
        {
            new Tempo { Veu = Art.Hex(0x34435C), VeuAlfa = 0.26f, Alfa = 0.55f, Vel = new Vector2(0.3f, 2.2f), N = 70, Rx = 0.9f, Ry = 15f },
            null, null, null,
            new Tempo { Veu = Art.Hex(0x1C2740), VeuAlfa = 0.44f, Alfa = 0.7f, Vel = new Vector2(0.9f, 3.2f), N = 100, Rx = 1.1f, Ry = 20f, Raios = true },
            new Tempo { Veu = Art.Hex(0x8A8F80), VeuAlfa = 0.1f, Alfa = 0.4f, Tam = 1.4f, Vel = new Vector2(-2.6f, 0.15f), N = 26, Rx = 0.9f, Ry = 34f },
            new Tempo { Veu = Art.Hex(0xDDE3E8), VeuAlfa = 0.38f, Alfa = 0.75f, Tam = 2.2f, Vel = new Vector2(-0.05f, 0f), N = 16, Rx = 40f, Ry = 62f },
            new Tempo { Veu = Art.Hex(0xB9C9DE), VeuAlfa = 0.2f, Alfa = 0.95f, Vel = new Vector2(0.06f, 0.42f), N = 60, Rx = 2.6f, Ry = 2.6f },
        };

        public const float ForcaAviso = 0.35f;
        const float RaioMin = 4f, ClaraoSeg = 0.35f, OnibusFica = 12f, OnibusParada = 11f, RuaX = -4.4f;
        static readonly Color Clarao = new Color(0.9f, 0.93f, 1f, 0.34f), Neve = Art.Hex(0xE9EEF2), Pau = Art.Hex(0x3A3F4B);

        /// <summary>Quem entra pela porta da sala so enquanto dura o incidente (Sala = -1: fora).</summary>
        sealed class Visita { public Boneco B; public int Sala = -1; }

        Image _veu;
        readonly RawImage[] _camada = new RawImage[2];
        readonly Vector2[] _uv = new Vector2[2];
        Tempo _tempo;
        float _forca, _clarao, _raioEm = RaioMin, _gramadoNeve;
        Renderer _gramado;
        readonly SpriteRenderer[] _pistaX = new SpriteRenderer[2], _raioBalcao = new SpriteRenderer[4], _alertaPane = new SpriteRenderer[2];
        SpriteRenderer _totemFundo, _totemIcone, _reparoAnel, _bandeira, _bandeiraPau;
        Transform _totem;
        readonly Transform[] _conesSala = new Transform[2];
        readonly Transform[,] _conesPane = new Transform[2, 4];
        Visita _tecnico, _guia;
        Veiculo _onibus;
        Trecho _rotaOnibus;

        // ------------------------------------------------------------------ puro (ClimaTests)

        /// <summary>Forca do clima no passo: nada antes do aviso (sim.Inc = null) nem fora de clima, ForcaAviso no aviso, 1 em curso.</summary>
        public static float Forca(Incidente i, long passo) => i == null || Tempos[(int)i.Tipo] == null ? 0f : passo >= i.Inicio ? 1f : ForcaAviso;

        public static Tempo TempoDe(TipoIncidente t) => Tempos[(int)t];

        /// <summary>Sala onde o incidente aparece: a falha na estacao dela, o pico no check-in, a conexao na seguranca, e o clima que fecha a
        /// pista no portao, que para de embarcar. Neblina e neve so atrasam a pista: nenhuma sala (-1).</summary>
        public static int SalaDo(Incidente i)
        {
            switch (i.Tipo)
            {
                case TipoIncidente.Falha: return (int)i.Estacao;
                case TipoIncidente.Pico: return (int)Estacao.CheckIn;
                case TipoIncidente.Conexao: return (int)Estacao.Seguranca;
                case TipoIncidente.Neblina: case TipoIncidente.Neve: return -1;
                default: return Portao;
            }
        }

        /// <summary>-cenario (so no -testsession): nome de um TipoIncidente ou "Pane" = acontece na proxima onda, para as fotos e o -record.</summary>
        public static void Ensaia(Sim s, string nome)
        {
            Tuning t = s.T;
            if (nome == "Pane")
            {
                t.FalhaAviaoChance = 1f; t.FalhaAviaoPesos = new[] { 1, 0, 0 };
                foreach (TA.Core.Aviao a in s.Avioes) a.Desgaste = 85;
                return;
            }
            if (!Enum.TryParse(nome, out TipoIncidente tipo)) return;
            int coluna = Array.FindIndex(t.Climas, c => c.Tipo == tipo);
            var linha = new float[t.Climas.Length];
            if (coluna >= 0) linha[coluna] = 1f;
            Aeroporto aero = t.Aeroportos[s.AeroportoAtual];
            aero.Clima = new[] { linha, linha, linha, linha };
            t.RegraClima = coluna >= 0; t.ClimaMarco = 0;
            if (coluna >= 0) return;
            aero.Pesos = new int[4]; aero.Pesos[(int)tipo] = 1;
            aero.Incidentes = 1f; t.IncidenteTeto = 1f;
        }

        /// <summary>Textura de 256 x 256 que repete sem emenda: N riscos macios, com a volta nas bordas.</summary>
        static Texture2D Manchas(Tempo t)
        {
            const int L = 256;
            var alfa = new float[L * L];
            var r = new System.Random(7);
            Vector2 e = t.Vel.normalized, n = new Vector2(-e.y, e.x);
            int raio = Mathf.CeilToInt(Mathf.Max(t.Rx, t.Ry));
            for (int k = 0; k < t.N; k++)
            {
                int cx = r.Next(L), cy = r.Next(L);
                float peso = 0.5f + 0.5f * (float)r.NextDouble();
                for (int y = -raio; y <= raio; y++)
                    for (int x = -raio; x <= raio; x++)
                    {
                        float a = (x * n.x + y * n.y) / t.Rx, b = (x * e.x + y * e.y) / t.Ry, d = a * a + b * b;
                        if (d >= 1f) continue;
                        int i = (cy + y + L) % L * L + (cx + x + L) % L;
                        alfa[i] = Mathf.Min(1f, alfa[i] + (1f - d) * peso);
                    }
            }
            var px = new Color32[L * L];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, (byte)(alfa[i] * 255f));
            var tex = new Texture2D(L, L, TextureFormat.RGBA32, false) { name = "Tempo", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // ------------------------------------------------------------------ construcao

        void Clima3D()
        {
            _veu = Art.Panel(_rotulos, "Veu", Color.clear, Vector2.zero, Vector2.one);
            _veu.raycastTarget = _veu.enabled = false;
            for (int c = 0; c < _camada.Length; c++)
            {
                _camada[c] = Art.Node(_rotulos, "Tempo" + c, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
                _camada[c].raycastTarget = _camada[c].enabled = false;
            }
            _camada[1].transform.SetAsFirstSibling();   // embaixo dos nomes e dos selos: quem le o jogo continua lendo
            _camada[0].transform.SetAsFirstSibling();
            _veu.transform.SetAsFirstSibling();
            _gramado = _raiz.Find("Gramado").GetComponent<Renderer>();
            for (int i = 0; i < _pistaX.Length; i++)
            {
                _pistaX[i] = Art.NewSprite(_raiz, "PistaFechada", Art.Xis(), Art.Warn, -31900, Vector2.zero, new Vector2(3.2f, 3.2f));
                _pistaX[i].transform.SetPositionAndRotation(new Vector3(2f + i * 18f, 0.02f, PistaZ), Quaternion.Euler(90f, 0f, 0f));
                _pistaX[i].enabled = false;
            }
            _totem = new GameObject("Totem").transform;
            _totem.SetParent(_raiz, false);
            Bloco("Base", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.4f, 0.02f, 0.4f), Pau, _totem);
            Bloco("Pau", PrimitiveType.Cylinder, new Vector3(0f, 0.7f, 0f), new Vector3(0.06f, 0.7f, 0.06f), Pau, _totem);
            _totem.gameObject.SetActive(false);
            _totemFundo = Billboard("TotemFundo", Art.Disc(), Art.Dark, 0.66f);
            _totemIcone = Billboard("TotemIcone", Art.Alerta(), Art.Warn, 0.44f);
            _reparoAnel = Billboard("ReparoAnel", Art.Arc(0f), Art.Good, 0.5f);
            for (int i = 0; i < _raioBalcao.Length; i++) _raioBalcao[i] = Billboard("RaioBalcao" + i, Art.Raio(), Art.Warn, 0.3f);
            for (int i = 0; i < _conesSala.Length; i++) _conesSala[i] = Cone();
            for (int k = 0; k < 2; k++)
            {
                _alertaPane[k] = Billboard("AlertaPane" + k, Art.Alerta(), Art.Bad, 0.9f);
                for (int i = 0; i < 4; i++)
                {
                    _conesPane[k, i] = Cone();
                    _conesPane[k, i].position = V(EstandeX[k] - 1.2f + i * 0.8f, NarizZ - 0.3f);
                }
            }
            _tecnico = NovaVisita("mecanico", MacacaoMecanico);
            _guia = NovaVisita("despachante", ColeteSinal);
            _bandeiraPau = Art.NewSprite(_guia.B.T, "BandeiraPau", Art.Square(), Pau, 0, new Vector2(0.24f, 0.78f), new Vector2(0.025f, 0.95f));
            _bandeira = Art.NewSprite(_guia.B.T, "Bandeira", Art.Triangle(), Art.Placa, 0, new Vector2(0.4f, 1.12f), new Vector2(0.3f, 0.32f));
            _bandeira.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            _onibus = NovoVeiculo("onibus_patio", null, 3f, 0.9f, Art.Hex(0x3FA7D6), 3.2f);
            _veiculos.Remove(_onibus);   // nao entra na fila do patio: anda so na rua
            _onibus.T.gameObject.SetActive(false);
            _onibus.Sombra.enabled = false;
            _rotaOnibus = Caminho(0f, 3f, V(RuaX, -9f), V(RuaX, 17f));
            _rotaOnibus.A = 0.8f; _rotaOnibus.F = 1f;
        }

        Transform Cone()
        {
            Transform t = Peca("cone", Vector3.zero, 0f);
            if (t == null) t = Bloco("Cone", PrimitiveType.Cube, Vector3.zero, new Vector3(0.2f, 0.3f, 0.2f), Art.Warn).transform;
            t.gameObject.SetActive(false);
            return t;
        }

        Visita NovaVisita(string ator, Color tinta)
        {
            var v = new Visita { B = NovoBoneco(ator) };
            v.B.Tinta = tinta;
            v.B.T.gameObject.SetActive(false);
            return v;
        }

        // ------------------------------------------------------------------ quadro a quadro

        /// <summary>Atualiza, depois das pessoas e do patio. `dt` = s de jogo do quadro (0 parado); o ceu anda no tempo real, como os pulsos da sala.</summary>
        void ClimaPasso(Sim s, float dt)
        {
            Incidente i = s.Inc;
            CeuPasso(i, s.Steps);
            bool fechada = s.PistaFechada || s.Steps < s.PistaAte;
            foreach (SpriteRenderer x in _pistaX)
            {
                x.enabled = fechada;
                if (fechada) x.color = Art.A(Art.Warn, Pulso());
            }
            TotemPasso(i, s);
            FalhaPasso(i, s, dt);
            int grupo = i != null && (i.Tipo == TipoIncidente.Pico || i.Tipo == TipoIncidente.Conexao) ? SalaDo(i) : -1;
            if (grupo >= 0) _bandeira.color = i.Tipo == TipoIncidente.Pico ? Art.Placa : Art.Good;
            VisitaPasso(_guia, grupo, grupo >= 0 ? LugarGuia(grupo) : Vector2.zero, dt);
            _bandeira.sortingOrder = _bandeiraPau.sortingOrder = Ordem(V3(_guia.B.Pos)) + 4;
            OnibusPasso(i != null && i.Tipo == TipoIncidente.Pico && (s.Steps - i.Inicio) * Sim.Step < OnibusFica, dt);
            PanePasso(s);
        }

        static float Pulso() => 0.55f + 0.45f * Mathf.PingPong(Time.unscaledTime * 2f, 1f);

        void CeuPasso(Incidente i, long passo)
        {
            float real = Time.unscaledDeltaTime;
            Tempo t = i != null ? Tempos[(int)i.Tipo] : null;
            if (t != null) _tempo = t;
            _forca = Mathf.MoveTowards(_forca, Forca(i, passo), real / 1.5f);
            float neve = _tempo == Tempos[(int)TipoIncidente.Neve] ? _forca : 0f;
            if (neve != _gramadoNeve) { _gramadoNeve = neve; Cor(_gramado, Color.Lerp(Grama, Neve, neve * 0.8f)); }
            bool ligado = _tempo != null && _forca > 0f;
            _veu.enabled = _camada[0].enabled = _camada[1].enabled = ligado;
            if (!ligado) return;
            if (_tempo.Tex == null) _tempo.Tex = Manchas(_tempo);
            _clarao = Mathf.MoveTowards(_clarao, 0f, real / ClaraoSeg);
            if (_tempo.Raios && _forca >= 1f && (_raioEm -= real) <= 0f)
            {
                _clarao = 1f;
                _raioEm = UnityEngine.Random.Range(RaioMin, RaioMin * 2f);
            }
            _veu.color = Color.Lerp(Art.A(_tempo.Veu, _tempo.VeuAlfa * _forca), Clarao, _clarao);
            for (int c = 0; c < _camada.Length; c++)
            {
                float perto = c == 0 ? 1f : 1.7f, telha = Screen.width * 0.42f * _tempo.Tam * perto;
                Vector2 uv = _uv[c] + _tempo.Vel * (perto * real);
                _uv[c] = new Vector2(Mathf.Repeat(uv.x, 1f), Mathf.Repeat(uv.y, 1f));
                _camada[c].texture = _tempo.Tex;
                _camada[c].color = new Color(1f, 1f, 1f, _tempo.Alfa * _forca * (c == 0 ? 0.6f : 1f));
                _camada[c].uvRect = new Rect(_uv[c].x, _uv[c].y, Screen.width / telha, Screen.height / telha);
            }
        }

        static Sprite IconeDo(TipoIncidente t)
        {
            switch (t)
            {
                case TipoIncidente.Falha: return Art.Raio();
                case TipoIncidente.Pico: return Art.Pessoas();
                case TipoIncidente.Conexao: return Art.Relogio();
                default: return Art.Cadeado();
            }
        }

        /// <summary>Num canto da sala, longe da fila e dos balcoes.</summary>
        void TotemPasso(Incidente i, Sim s)
        {
            int sala = i != null ? SalaDo(i) : -1;
            bool tem = sala >= 0;
            _totem.gameObject.SetActive(tem);
            _totemFundo.enabled = _totemIcone.enabled = tem;
            if (!tem) return;
            Vector3 p = V3(Em(sala, 3.7f, 1.75f));
            _totem.position = p;
            Vector3 topo = p + Vector3.up * 1.6f;
            Quaternion q = _cam.transform.rotation;
            int ordem = Ordem(topo) + 9;
            _totemFundo.transform.SetPositionAndRotation(topo, q);
            _totemFundo.sortingOrder = ordem;
            _totemIcone.transform.SetPositionAndRotation(topo, q);
            _totemIcone.sortingOrder = ordem + 1;
            _totemIcone.sprite = IconeDo(i.Tipo);
            Color cor = i.Resolvido ? Art.Good : s.Steps >= i.Inicio ? Art.Bad : Art.Warn;
            _totemIcone.color = Art.A(cor, i.Resolvido ? 1f : Pulso());
        }

        /// <summary>Falha de equipamento: do aviso ao fim do reparo. O tecnico so entra se nao ha mecanico da equipe na sala.</summary>
        void FalhaPasso(Incidente i, Sim s, float dt)
        {
            bool falha = i != null && i.Tipo == TipoIncidente.Falha && !i.Resolvido;
            int e = falha ? (int)i.Estacao : -1;
            Quaternion q = _cam.transform.rotation;
            for (int n = 0; n < _raioBalcao.Length; n++)
            {
                bool liga = falha && n < s.Estacoes[e].Postos;
                _raioBalcao[n].enabled = liga;
                if (!liga) continue;
                Vector3 p = V3(Balcao(e, n), 1.3f);
                _raioBalcao[n].transform.SetPositionAndRotation(p, q);
                _raioBalcao[n].sortingOrder = Ordem(p) + 9;
                _raioBalcao[n].color = Art.A(s.Steps >= i.Inicio ? Art.Bad : Art.Warn, Pulso());
            }
            for (int n = 0; n < _conesSala.Length; n++)
            {
                _conesSala[n].gameObject.SetActive(falha);
                if (falha) _conesSala[n].position = V3(Em(e, 0.9f + n * 0.7f, -1.85f));
            }
            bool reparo = falha && s.Reparo > 0;
            _reparoAnel.enabled = reparo;
            if (reparo)
            {
                Vector3 p = V3(Balcao(e, 0), 1.3f);
                _reparoAnel.sprite = Art.Arc(1f - s.Reparo * Sim.Step / s.T.ReparoSeg);
                _reparoAnel.transform.SetPositionAndRotation(p, q);
                _reparoAnel.sortingOrder = Ordem(p) + 10;
            }
            bool daEquipe = false;
            if (falha) foreach (Funcionario f in s.Staff) if (f.Perfil == Perfil.Mecanico && f.Em == i.Estacao) daEquipe = true;
            VisitaPasso(_tecnico, reparo && !daEquipe ? e : -1, falha ? Em(e, 1.25f, -1.3f) : Vector2.zero, dt);
        }

        /// <summary>Do lado de dentro da porta da sala, sem tampar quem entra.</summary>
        static Vector2 LugarGuia(int e)
        {
            Vector2 d = (Centro(e) - Salas[e].In).normalized;
            return Salas[e].In + d * 0.4f + new Vector2(-d.y, d.x) * 0.85f;
        }

        /// <summary>Entra pela porta quando `sala` liga e sai por ela quando desliga.</summary>
        void VisitaPasso(Visita v, int sala, Vector2 lugar, float dt)
        {
            Boneco b = v.B;
            if (sala != v.Sala)
            {
                if (sala >= 0)
                {
                    b.Pos = Salas[sala].In;
                    Vai(b, lugar);
                    b.T.gameObject.SetActive(true);
                }
                else Vai(b, Salas[v.Sala].In);
                v.Sala = sala;
            }
            if (!b.T.gameObject.activeSelf) return;
            Anda(b, 2f, dt);
            b.Atende = sala >= 0 && b.Caminho.Count == 0;
            Pinta(b);
            if (sala < 0 && b.Caminho.Count == 0) b.T.gameObject.SetActive(false);
        }

        /// <summary>Sobe a rua, para na calcada do check-in enquanto o grupo desce e segue.</summary>
        void OnibusPasso(bool fica, float dt)
        {
            Veiculo o = _onibus;
            bool ativo = o.T.gameObject.activeSelf;
            if (!ativo && !fica) return;
            if (!ativo) { o.S = 0f; o.V = 0f; o.T.gameObject.SetActive(true); o.Sombra.enabled = true; }
            bool segura = fica && o.S <= OnibusParada;
            o.V = Passo(o.V, _rotaOnibus, o.S, _rotaOnibus.V, segura ? OnibusParada - o.S : 1e4f, LatVeic, dt);
            o.S = Mathf.Min(o.S + o.V * Mathf.Max(0f, dt), segura ? OnibusParada : _rotaOnibus.Total);
            Pose(o.T, _rotaOnibus, o.S);
            Sombra(o);
            if (o.S < _rotaOnibus.Total) return;
            o.T.gameObject.SetActive(false);
            o.Sombra.enabled = false;
        }

        void PanePasso(Sim s)
        {
            for (int k = 0; k < _avioes.Length; k++)
            {
                TA.Core.Aviao c = s.Avioes[k];
                bool pane = c.Pane && _avioes[k].Fase == Fase.NoPatio;
                for (int i = 0; i < 4; i++) _conesPane[k, i].gameObject.SetActive(pane);
                bool espera = pane && c.Check == 0;   // com o mecanico la, o selo do aviao ja mostra a chave e o anel
                _alertaPane[k].enabled = espera;
                if (!espera) continue;
                Vector3 p = _avioes[k].T.position + Vector3.up * 4.4f - _cam.transform.right * 1.2f;
                _alertaPane[k].transform.SetPositionAndRotation(p, _cam.transform.rotation);
                _alertaPane[k].sortingOrder = Ordem(p) + 10;
                _alertaPane[k].color = Art.A(Art.Bad, Pulso());
            }
        }
    }
}
