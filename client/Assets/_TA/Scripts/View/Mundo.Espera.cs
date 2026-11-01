using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// A sala de espera do portão, só apresentação: quem embarca e quando continua sendo do núcleo (fila e atendimento do portão). Fileiras de
    /// cadeiras dos dois lados do corredor; quem espera senta. Ao abrir o embarque, os primeiros da fila do núcleo levantam e formam uma
    /// serpentina diante do pódio, passam pela agente e seguem para a porta. Sem cadeira livre, fica em pé no fim da mesma serpentina. A agente
    /// é só de tela enquanto o portão não tem funcionário: sem custo e sem efeito no núcleo. Tudo é relativo à sala (Em, Interno): ela pode crescer.
    /// </summary>
    public sealed partial class Mundo
    {
        const int Portao = (int)Estacao.Portao;
        /// <summary>Podios ao longo da parede: os de fora abrem caminho para quem sai pela porta do estande remoto (a = 1,2).</summary>
        static readonly float[] PodioA = { -0.6f, 0.6f, -1.45f, 1.45f };
        /// <summary>Vaga da pessoa no portão: cadeira (o índice), lugar na serpentina (VagaFila + k) ou pódio (VagaPosto + posto).</summary>
        const int VagaFila = 1000, VagaPosto = 2000, Chamados = 13;
        /// <summary>Medidas em m, na régua do adulto de 0,5 m. A cadeira tem 4 lugares, encosto em -Frente e o centro do assento a RecuoAssento à frente do centro da malha.</summary>
        const float EscalaCadeira = 0.5f, LarguraCadeira = 1.6f * EscalaCadeira, PassoAssento = LarguraCadeira / 4f, PassoFileira = 0.44f, FolgaParede = 0.14f, RecuoAssento = 0.045f;
        /// <summary>De pé diante do lugar, antes de sentar; meio corredor central; d do corredor atrás da fila, na linha das portas oeste e leste; 1ª fileira da serpentina; a agente e a pessoa da vez, cada uma de um lado do pódio.</summary>
        const float RecuoPe = 0.17f, MeioCorredor = 0.17f, DCorredor = 1.62f, DFila = 1.06f, PassoFila = 0.22f, PassoLinha = 0.24f, FolgaPorta = 0.24f, PassoPodio = 0.22f;
        /// <summary>Tempo de sentar (s) e o quanto o boneco sai do chão para a câmera: sentado, desenha por cima da própria cadeira e por baixo da da frente;
        /// no pódio, por cima do tampo, que é alto e está logo atrás dele.</summary>
        const float TempoSenta = 0.42f, FrenteSentado = 0.25f, FrentePodio = 0.28f;

        /// <summary>Um lugar: o centro do assento e o ponto no corredor da fileira, de onde a pessoa senta e para onde levanta.</summary>
        struct Assento { public Vector2 P, Pe; public Assento(Vector2 p, Vector2 pe) { P = p; Pe = pe; } }

        readonly List<Assento> _assentos = new List<Assento>();
        readonly List<Vector2> _cadeiras = new List<Vector2>();   // o centro de cada malha, para o -diag
        readonly List<int> _dono = new List<int>();   // quem está em cada lugar (-1 = livre)
        readonly HashSet<int> _sentados = new HashSet<int>();
        readonly List<Vector2> _serpente = new List<Vector2>();
        int _pernas;   // lugares nas 3 fileiras: dali em diante a serpentina é o excedente, que não anda em linha reta
        readonly List<GameObject> _cordao = new List<GameObject>();   // a corda entre 2 fileiras só aparece quando a fila chega na de trás
        readonly List<int> _cordaoDe = new List<int>();
        readonly GameObject[] _zona = new GameObject[4];
        Boneco _agente, _porteiro;
        bool _agenteSaiu;

        /// <summary>Para onde quem está sentado olha.</summary>
        static Vector2 FrenteCadeira => -Salas[Portao].T;
        static float DeSaida(Vector2 p) => Vector2.Dot(Salas[Portao].Out - p, Salas[Portao].N);
        static float Ao(Vector2 p) => Vector2.Dot(p - Salas[Portao].Out, Salas[Portao].T);

        /// <summary>Cadeiras, serpentina, cordas e zona de atendimento (Sala3D): as cadeiras no fundo da sala, de costas para a parede leste de cada bloco.</summary>
        void Espera3D()
        {
            float borda = Interno * 0.5f;
            for (float a = borda - FolgaParede; a > MeioCorredor + RecuoPe + 0.1f; a -= PassoFileira) Fileiras(a);   // bloco leste
            for (float a = -MeioCorredor - 0.11f; a > -borda + RecuoPe + 0.1f; a -= PassoFileira) Fileiras(a);       // bloco oeste
            for (int i = 0; i < _assentos.Count; i++) _dono.Add(-1);

            float aMax = borda - 0.14f, aMin = -borda + FolgaPorta + 0.1f;
            float fim = 0f;
            int perna1 = Perna(DFila, ref fim, aMax, PassoFila);
            int perna2 = perna1 + Perna(DFila + PassoLinha, ref fim, aMin, -PassoFila);
            _pernas = perna2 + Perna(DFila + 2f * PassoLinha, ref fim, aMax, PassoFila);
            // o excedente do estresse: em pé no corredor central e nos corredores das fileiras, em linha
            for (float d = DCorredor + 0.1f; d < Interno - 0.15f; d += PassoFila) _serpente.Add(Em(Portao, d, 0f));
            foreach (Assento c in _assentos)
                if (_serpente.IndexOf(c.Pe) < 0) _serpente.Add(c.Pe);
            Cordao(DFila + PassoLinha * 0.5f, 0.06f, aMax - 0.17f, perna1);
            Cordao(DFila + PassoLinha * 1.5f, aMin + 0.17f, aMax, perna2);

            for (int i = 0; i < 4; i++)
            {
                _zona[i] = new GameObject("Zona" + i);
                _zona[i].transform.SetParent(_cont, false);
                Transform pai = _cont;
                _cont = _zona[i].transform;
                for (int f = 0; f < 4; f++)   // faixas no chão diante do pódio: a pessoa da vez para aqui
                    Bloco("Faixa", PrimitiveType.Cube, V3(NoBalcao(Portao, i) - Salas[Portao].N * (0.02f + f * 0.06f - 0.09f), 0.008f), new Vector3(0.3f, 0.016f, 0.03f), Art.Paper);
                _cont = pai;
            }
        }

        /// <summary>As cadeiras de uma coluna em `a`, do fundo da sala até o corredor.</summary>
        void Fileiras(float a)
        {
            for (float d = Interno - 0.05f - LarguraCadeira * 0.5f; d - LarguraCadeira * 0.5f > DCorredor + 0.08f; d -= LarguraCadeira + 0.02f)
            {
                Vector2 c = Em(Portao, d, a);
                _cadeiras.Add(c);
                Transform t = Peca("cadeiras_espera", V3(c), Rumo(FrenteCadeira), EscalaCadeira);
                if (t == null) Bloco("Cadeira", PrimitiveType.Cube, V3(c, 0.08f), new Vector3(0.21f, 0.16f, LarguraCadeira), Art.Hex(0x2F5FA8));
                for (int i = 0; i < 4; i++)
                {
                    Vector2 p = Em(Portao, d + (i - 1.5f) * PassoAssento, a) + FrenteCadeira * RecuoAssento;
                    _assentos.Add(new Assento(p, p + FrenteCadeira * RecuoPe));
                }
            }
        }

        /// <summary>Uma fileira da serpentina em `d`, de `a` até `ate`: soma os lugares, devolve quantos e deixa `a` no último, para a próxima virar no eixo.</summary>
        int Perna(float d, ref float a, float ate, float passo)
        {
            int n = 0;
            for (; passo > 0f ? a <= ate : a >= ate; a += passo, n++) _serpente.Add(Em(Portao, d, a));
            a -= passo;
            return n;
        }

        /// <summary>Corda entre 2 fileiras da serpentina, de `a0` a `a1`, que aparece quando a fila passa de `n` pessoas.</summary>
        void Cordao(float d, float a0, float a1, int n)
        {
            const float Passo = 0.4f;
            for (float a = a0 + Passo * 0.5f; a <= a1; a += Passo)
            {
                Transform t = Peca("cordao_fila", V3(Em(Portao, d, a)), Rumo(Salas[Portao].N), Passo);
                if (t == null) t = Bloco("Cordao", PrimitiveType.Cube, V3(Em(Portao, d, a), 0.2f), new Vector3(Passo, 0.02f, 0.02f), Fita).transform;
                t.gameObject.SetActive(false);
                _cordao.Add(t.gameObject);
                _cordaoDe.Add(n);
            }
        }

        // ------------------------------------------------------------------ quadro a quadro

        static bool Aberto(Sim s, Pax p)
        {
            Voo v = s.VooDe(p);
            return v != null && v.Embarque && (!s.PistaFechada || v.Adiantado);
        }

        /// <summary>Vaga de cada um na fila do portão: primeiro os chamados (voo aberto), na ordem do núcleo, na frente da serpentina; os outros
        /// na cadeira que já têm ou na primeira que achar; sem cadeira, em pé atrás dos chamados.</summary>
        void FilaPortao(Sim s, Station st)
        {
            bool critico = st.Estado == Estado.Critico;
            int k = 0;
            for (int i = 0; i < st.Fila.Count && k < Chamados; i++)
                if (Aberto(s, st.Fila[i])) { Liga(st.Fila[i], Portao, Lugar(k), s, false, critico && k < 3, VagaFila + k); k++; }
            int chamados = 0;
            _sentados.Clear();
            for (int i = 0; i < st.Fila.Count; i++)
            {
                Pax p = st.Fila[i];
                if (Aberto(s, p) && chamados++ < Chamados) continue;
                int c = Cadeira(p.Id);
                if (c >= 0) { _sentados.Add(p.Id); Liga(p, Portao, _assentos[c].Pe, s, false, false, c); }
                else { Liga(p, Portao, Lugar(k), s, false, false, VagaFila + k); k++; }
            }
            for (int c = 0; c < _dono.Count; c++) if (_dono[c] >= 0 && !_sentados.Contains(_dono[c])) _dono[c] = -1;
            for (int i = 0; i < _cordao.Count; i++) _cordao[i].SetActive(k > _cordaoDe[i]);
            for (int i = 0; i < 4; i++) _zona[i].SetActive(i < st.Postos);
        }

        Vector2 Lugar(int k) => _serpente[Mathf.Min(k, _serpente.Count - 1)];

        /// <summary>A cadeira do passageiro; sem nenhuma, a primeira livre a partir de um ponto que depende do id (a sala enche espalhada, não em ordem).</summary>
        int Cadeira(int id)
        {
            int n = _dono.Count, c = _dono.IndexOf(id);
            if (c >= 0 || n == 0) return c;
            int ini = (int)(((uint)id * 2654435761u) >> 20) % n;
            for (int i = 0; i < n; i++)
            {
                c = (ini + i) % n;
                if (_dono[c] < 0) { _dono[c] = id; return c; }
            }
            return -1;
        }

        /// <summary>Quem já está na sala mudou de vaga: na serpentina anda lugar a lugar (não corta a corda); nos outros casos, pelos corredores.</summary>
        void MudaVaga(Boneco b, Vector2 alvo, int vaga)
        {
            int era = b.Vaga;
            if (vaga == era) return;
            b.Vaga = vaga;
            bool fila = vaga >= VagaFila && vaga < VagaPosto, eraFila = era >= VagaFila && era < VagaPosto;
            if (fila && eraFila && Mathf.Max(vaga, era) - VagaFila < _pernas)
            {
                int passo = vaga > era ? 1 : -1;
                for (int i = era + passo; i != vaga + passo; i += passo) b.Caminho.Add(Lugar(i - VagaFila));
                return;
            }
            b.Caminho.Clear();
            Vector2 p = b.Cadeira >= 0 ? _assentos[b.Cadeira].Pe : b.Pos;
            RotaSala(b.Caminho, ref p, alvo);
        }

        /// <summary>De p até alvo dentro do portão: quem está entre as cadeiras sai pelo corredor da fileira e anda pelo corredor de trás da fila.</summary>
        public static void RotaSala(List<Vector2> r, ref Vector2 p, Vector2 alvo)
        {
            if (DeSaida(p) > DCorredor) Contorna(r, ref p, Em(Portao, DCorredor, Ao(p)), false);
            if (DeSaida(alvo) > DCorredor) Contorna(r, ref p, Em(Portao, DCorredor, Ao(alvo)), true);
            Contorna(r, ref p, alvo, true);
        }

        /// <summary>Sai(): quem deixa a sala do meio das cadeiras passa antes pelo corredor.</summary>
        void SaiDaFileira(Boneco b, ref Vector2 p)
        {
            b.Vaga = -1;
            if (b.Cadeira >= 0) p = _assentos[b.Cadeira].Pe;
            if (DeSaida(p) > DCorredor) Contorna(b.Caminho, ref p, Em(Portao, DCorredor, Ao(p)), false);
        }

        /// <summary>Senta ao chegar diante do lugar e levanta quando a vaga muda: a pessoa recua até o assento, sem deslizar o pé. true = parada nisso.</summary>
        bool Sentando(Boneco b, float dt)
        {
            bool quer = b.Vaga >= 0 && b.Vaga < VagaFila && b.Caminho.Count == 0;
            if (quer && b.Cadeira < 0) b.Cadeira = b.Vaga;
            if (b.Cadeira < 0) return false;
            b.Senta = Mathf.MoveTowards(b.Senta, quer ? 1f : 0f, dt / TempoSenta);
            Assento c = _assentos[b.Cadeira];
            b.Pos = Vector2.Lerp(c.Pe, c.P, b.Senta);
            Movido(b, Vector2.zero, dt);
            Olha(b, FrenteCadeira);
            if (b.Senta > 0f || quer) return true;
            b.Cadeira = -1;
            return false;
        }

        /// <summary>Agente de embarque no pódio: sem funcionário no portão, a agente de tela atende; quando alguém chega, ela sai pela porta do túnel.
        /// O colega fica junto da 2ª porta, sempre.</summary>
        void AgentesPortao(Sim s, float dt)
        {
            if (_agente == null)
            {
                _agente = NovoBoneco("agente_portao_f");
                _porteiro = NovoBoneco("agente_portao");
                _agente.Pos = AtrasBalcao(Portao, 0);
                _porteiro.Pos = Em(Portao, 0.2f, 1.52f);
                _agente.Tinta = _porteiro.Tinta = Color.white;
            }
            Station st = s.St(Estacao.Portao);
            bool real = s.Presentes(Estacao.Portao) > 0;
            if (real != _agenteSaiu)
            {
                _agenteSaiu = real;
                _agente.T.gameObject.SetActive(true);
                Vector2 porta = Em(Portao, 0.1f, 0f), lado = Em(Portao, 0.3f, 0f), casa = AtrasBalcao(Portao, 0);
                _agente.Caminho.Clear();
                if (real) { _agente.Caminho.Add(lado); _agente.Caminho.Add(porta); }
                else { _agente.Caminho.Add(lado); _agente.Caminho.Add(casa); }
            }
            Anda(_agente, VelFunc, dt);
            bool parou = _agente.Caminho.Count == 0;
            if (real && parou) _agente.T.gameObject.SetActive(false);
            if (parou) Olha(_agente, -Salas[Portao].N);
            _agente.Atende = !real && parou && st.Atendendo.Count > 0;
            Pinta(_agente);
            _porteiro.T.gameObject.SetActive(s.Presentes(Estacao.Portao) < 4);
            Olha(_porteiro, FrenteCadeira - Salas[Portao].N);
            Movido(_porteiro, Vector2.zero, dt);
            Pinta(_porteiro);
        }
    }
}
