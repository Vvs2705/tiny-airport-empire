using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// A mala do passageiro, só na tela: o núcleo diz quem despacha (Pax.Mala, com gente na sala de bagagem) e quanto dura; nada aqui muda regra.
    ///  - Na mão: a folha normal traz a mala grande; a folha `corpo_mao` traz só a mala de mão, para a cor do voo continuar na pessoa.
    ///  - Despacho: cada posto tem esteira e balança. A pessoa chega ao balcão e pousa a mala na balança; depois de pesada, ela vai pela esteira
    ///    do posto até a esteira da parede, passa pela boca no meio dela e entra no terminal.
    ///  - Segurança: a mesma mala deitada na bandeja (Mundo.Seguranca.cs), com a pessoa só com a mala de mão até pegá-la de volta.
    ///  - Pátio: o que entrou no terminal sai empilhado no carrinho do avião do voo e a pilha baixa com o serviço de bagagem.
    /// </summary>
    public sealed partial class Mundo
    {
        const int SalaMalas = (int)Estacao.Bagagem, MaxMalas = 16, MaxPilha = 6;
        const float EscalaMala = 0.5f, EscalaBalanca = 0.5f, EscalaBalcaoMala = 0.5f, EscalaBandeja = 0.35f, MeiaMala = 0.21f, MeiaEspessura = 0.07f;
        /// <summary>Esteira: velocidade (m/s de jogo), altura, largura, distância da parede, lado da balança a partir do posto, a fachada do
        /// terminal e o tempo da mala na balança (s de jogo).</summary>
        const float VelEsteira = 0.9f, AltEsteira = 0.12f, LargEsteira = 0.16f, DEsteira = 0.12f, LadoBalanca = 0.4f, TerminalZ = 10.75f, Pesando = 1.4f;
        static readonly Color CorEsteira = Art.Hex(0x4A505C), CorBorda = Art.Hex(0xC9CFD8), CorBoca = Art.Hex(0x2B2F38), CorVerga = Art.Hex(0xF2C14E);

        sealed class Mala
        {
            public Transform T; public Renderer R;
            public int Voo, Ponto;
            public float Espera;
            public readonly List<Vector2> Rota = new List<Vector2>();
        }

        readonly Stack<Mala> _malasLivres = new Stack<Mala>();
        readonly List<Mala> _naEsteira = new List<Mala>();
        readonly Mala[] _balanca = new Mala[8];   // 4 da sala de bagagem e, depois, os 4 guichês do check-in (Mundo.Postos.cs)
        readonly GameObject[] _postoMala = new GameObject[4];
        readonly Dictionary<Veiculo, Mala[]> _pilha = new Dictionary<Veiculo, Mala[]>();
        /// <summary>Malas que já entraram no terminal, por avião, e se ele estava no pátio no quadro anterior (zera quando parte).</summary>
        readonly int[] _noTerminal = new int[2];
        readonly bool[] _pousado = new bool[2];
        int _malasCriadas;

        /// <summary>Malha da mala (pai = `T`, pivô nas rodinhas) ou um bloco sem a pasta ArtLocal.</summary>
        Mala NovaMala(string nome)
        {
            var m = new Mala { T = Peca("mala_rodinha", Vector3.zero, 0f, EscalaMala) };
            if (m.T == null) { m.R = Bloco(nome, PrimitiveType.Cube, Vector3.zero, new Vector3(0.22f, 0.42f, 0.14f), Color.white); m.T = m.R.transform; }
            else m.R = m.T.GetComponentInChildren<Renderer>();
            m.T.gameObject.SetActive(false);
            return m;
        }

        /// <summary>Deitada de costas, centrada em `c` (coordenadas do pai), com o comprimento no rumo `yaw`.</summary>
        static void Deita(Mala m, Vector3 c, float yaw, float escala)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(90f, 0f, 0f);
            m.T.SetLocalPositionAndRotation(c + Vector3.up * (MeiaEspessura * escala) - q * Vector3.up * (MeiaMala * escala), q);
            m.T.localScale = Vector3.one * escala;
        }

        /// <summary>A folha da pessoa: com a mala grande ou só com a de mão.</summary>
        static void SoMao(Boneco b, Pax p, bool soMao)
        {
            if (b.SoMao == soMao) return;
            b.SoMao = soMao;
            Veste(b, CorpoDe(p) + (soMao ? "_mao" : ""));
        }

        // ------------------------------------------------------------------ planta

        /// <summary>Lado do posto em que fica a balança: sempre para o meio da sala, onde há folga entre os postos.</summary>
        static float ABalanca(int i)
        {
            float a = Posto(SalaMalas, i);
            return a - Mathf.Sign(a) * LadoBalanca;
        }

        public static Vector2 PontoBalanca(int i) => i >= 4 ? BalancaChk(i - 4) : Balcao(SalaMalas, i) + Salas[SalaMalas].T * (ABalanca(i) - Posto(SalaMalas, i));
        public static Vector2 BalcaoMala(int i) => Balcao(SalaMalas, i);

        /// <summary>Da balança do posto `i` até a fachada do terminal: esteira do posto, esteira da parede, boca no meio dela.</summary>
        public static void RotaMala(List<Vector2> r, int i)
        {
            r.Clear();
            if (i >= 4) { RotaChk(r, i - 4); return; }
            r.Add(Em(SalaMalas, DEsteira, ABalanca(i)));
            r.Add(Em(SalaMalas, DEsteira, 0f));
            r.Add(new Vector2(Salas[SalaMalas].Out.x, TerminalZ));
        }

        /// <summary>Balança e esteira de um posto (Sala3D); o grupo acompanha os postos abertos.</summary>
        void Balanca3D(int i)
        {
            Transform sala = _cont;
            var g = new GameObject("PostoMala" + i).transform;
            g.SetParent(sala, false);
            _cont = g;
            Peca("balanca_bagagem", V3(PontoBalanca(i)), Rumo(-Salas[SalaMalas].N), EscalaBalanca);
            Esteira(PontoBalanca(i), Em(SalaMalas, DEsteira, ABalanca(i)));
            _cont = sala;
            _postoMala[i] = g.gameObject;
        }

        /// <summary>A esteira da parede, com as pontas nas esteiras dos postos de fora, e a saída até o terminal (Sala3D).</summary>
        void Esteira3D()
        {
            Esteira(Em(SalaMalas, DEsteira, ABalanca(2)), Em(SalaMalas, DEsteira, ABalanca(3)));
            Vector2 meio = Em(SalaMalas, DEsteira, 0f), fim = new Vector2(meio.x, TerminalZ);
            Esteira(meio, fim);
            Boca(Em(SalaMalas, 0f, 0f));
            Boca(fim);
        }

        /// <summary>Vão da esteira numa parede: batentes, verga amarela e a cortina escura.</summary>
        void Boca(Vector2 p)
        {
            Bloco("BocaCortina", PrimitiveType.Cube, V3(p, 0.2f), new Vector3(0.3f, 0.4f, 0.05f), CorBoca);
            for (int lado = -1; lado <= 1; lado += 2)
                Bloco("BocaBatente", PrimitiveType.Cube, V3(p + Vector2.right * (lado * 0.17f), 0.21f), new Vector3(0.05f, 0.42f, 0.12f), CorBorda);
            Bloco("BocaVerga", PrimitiveType.Cube, V3(p, 0.44f), new Vector3(0.4f, 0.07f, 0.14f), CorVerga);
        }

        /// <summary>Trecho reto: a lona escura em cima da borda clara, um pouco mais larga e mais baixa.</summary>
        void Esteira(Vector2 a, Vector2 b, float alt = AltEsteira, float larg = LargEsteira)
        {
            Vector2 c = (a + b) * 0.5f;
            bool x = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.y - a.y);
            float l = Vector2.Distance(a, b) + larg;
            Bloco("EsteiraBorda", PrimitiveType.Cube, V3(c, (alt - 0.02f) * 0.5f), x ? new Vector3(l, alt - 0.02f, larg + 0.06f) : new Vector3(larg + 0.06f, alt - 0.02f, l), CorBorda);
            Bloco("EsteiraLona", PrimitiveType.Cube, V3(c, alt * 0.5f), x ? new Vector3(l, alt, larg) : new Vector3(larg, alt, l), CorEsteira);
        }

        // ------------------------------------------------------------------ quadro a quadro

        /// <summary>Liga, antes de mudar a rota. O núcleo não espera a pessoa: quem estava indo ao balcão termina o caminho, pousa a mala e só então
        /// segue. True = ainda indo, a rota fica como está.</summary>
        bool IndoAoBalcao(Boneco b, Pax p, int e, int vaga, bool noBalcao)
        {
            if (SeguraNoPosto(b, p, e)) return true;
            if (e != Portao && noBalcao) b.Vaga = vaga;
            if (b.Zona != SalaMalas || !b.NoBalcao || b.Despachou) return false;
            if (b.Caminho.Count == 0) { Pousa(b, p); return false; }
            return e != SalaMalas;
        }

        void Pousa(Boneco b, Pax p)
        {
            b.Despachou = true;
            SoMao(b, p, true);
            int i = b.Vaga - VagaPosto + (b.Zona == SalaCheckIn ? 4 : 0);
            if (_balanca[i] != null) Solta(i);
            Mala m = _malasLivres.Count > 0 ? _malasLivres.Pop() : _malasCriadas++ < MaxMalas ? NovaMala("Mala") : null;
            if (m == null) return;
            m.Voo = p.Voo;
            m.Espera = Pesando;
            Cor(m.R, Art.VooCor[p.Voo]);
            m.T.position = V3(PontoBalanca(i), 0.22f * EscalaBalanca);
            m.T.localScale = Vector3.one * EscalaMala;
            m.T.gameObject.SetActive(true);
            _balanca[i] = m;
        }

        void Solta(int i)
        {
            Mala m = _balanca[i];
            _balanca[i] = null;
            RotaMala(m.Rota, i);
            m.Ponto = 0;
            m.T.position = V3(PontoBalanca(i), AltEsteira);
            _naEsteira.Add(m);
        }

        void MalasPasso(Sim s, float dt)
        {
            int postos = s.St(Estacao.Bagagem).Postos;
            for (int i = 0; i < 4; i++) _postoMala[i].SetActive(i < postos);
            for (int i = 0; i < _balanca.Length; i++)
                if (_balanca[i] != null && (_balanca[i].Espera -= dt) <= 0f) Solta(i);
            for (int k = _naEsteira.Count - 1; k >= 0; k--)
            {
                Mala m = _naEsteira[k];
                Vector3 alvo = V3(m.Rota[m.Ponto], AltEsteira);
                m.T.position = Vector3.MoveTowards(m.T.position, alvo, VelEsteira * dt);
                if ((m.T.position - alvo).sqrMagnitude > 1e-4f || ++m.Ponto < m.Rota.Count) continue;
                _noTerminal[m.Voo]++;
                m.T.gameObject.SetActive(false);
                _malasLivres.Push(m);
                _naEsteira.RemoveAt(k);
            }
            Carrinhos(s);
        }

        /// <summary>Pilha no carrinho: as malas do avião que ele foi servir, até 6, que baixam com o serviço de bagagem.</summary>
        void Carrinhos(Sim s)
        {
            for (int k = 0; k < _noTerminal.Length; k++)
            {
                bool pousado = s.Avioes[k].NoPatio;
                if (_pousado[k] && !pousado) _noTerminal[k] = 0;
                _pousado[k] = pousado;
            }
            int passos = s.ServicoPassos(Servico.Bagagem);
            foreach (Veiculo v in _veiculos)
            {
                if (v.Tipo == null || v.Tipo.Servico != (int)Servico.Bagagem) continue;
                if (!_pilha.TryGetValue(v, out Mala[] pilha)) _pilha[v] = pilha = NovaPilha(v);
                int k = v.Aviao, n = 0;
                if (k >= 0 && v.Lida != Lida.Voltando)
                {
                    n = Mathf.Min(MaxPilha, _noTerminal[k]);
                    if (v.Lida == Lida.Servindo) n = Mathf.CeilToInt(n * s.Avioes[k].Falta[(int)Servico.Bagagem] / (float)passos);
                }
                for (int i = 0; i < pilha.Length; i++)
                {
                    pilha[i].T.gameObject.SetActive(i < n);
                    if (i < n) Cor(pilha[i].R, Art.VooCor[k]);
                }
            }
        }

        /// <summary>6 malas na caçamba do carrinho (atrás do pivô), 2 camadas de 3, deitadas de través.</summary>
        Mala[] NovaPilha(Veiculo v)
        {
            var pilha = new Mala[MaxPilha];
            for (int i = 0; i < MaxPilha; i++)
            {
                Mala m = NovaMala("Pilha");
                m.T.SetParent(v.T, false);
                Deita(m, new Vector3(0f, 0.36f + i / 3 * 0.15f * EscalaMala, -0.12f + (i % 3 - 1) * 0.24f * EscalaMala), 90f, EscalaMala);
                pilha[i] = m;
            }
            return pilha;
        }
    }
}
