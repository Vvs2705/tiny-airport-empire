using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Posto com gente de verdade, só apresentação: quem atende e por quanto tempo continua sendo do núcleo.
    ///  - Cada passageiro fica no posto em que chegou (o núcleo tira da lista quem termina e os outros sobem nela).
    ///  - Check-in: cada guichê tem painel com o número na parede, bancada com monitor, teclado e impressora, e as 2 balanças ficam entre os pares
    ///    de guichês, com a esteira que passa pelo vão, corre rente à parede e entra na boca da parede lateral.
    ///  - O atendimento é um roteiro na fração do tempo do serviço (EntregaAte..DevolveDe): o gesto de cada um acompanha a etapa, não um laço solto. O
    ///    passageiro entrega o documento, põe a mala na balança se for despachar e recebe o documento de volta; o atendente pega, digita, etiqueta e devolve.
    ///  - Posto com gente tem as faixas no chão e as telas acesas; posto sem ninguém fica vazio de verdade: tudo apagado, cordão fechando a passagem e a
    ///    placa de fora de serviço. Quem está vindo para o posto já abre o cordão, e quem está sendo atendido pelo toque do jogador acende o posto.
    ///  - Atrás da bancada e das máquinas o boneco sai do chão para a câmera: a folha deita para trás e entraria no painel.
    /// As medidas são para EscalaSeg 0,5 (LinhaM), como na linha da segurança.
    /// </summary>
    public sealed partial class Mundo
    {
        const int SalaCheckIn = (int)Estacao.CheckIn;
        /// <summary>Guichês ao longo da parede: 2 pares, com a balança no meio de cada par e o corredor da porta entre os 2 de dentro.</summary>
        static readonly float[] ChkA = { -0.45f, 0.45f, -1.25f, 1.25f };
        /// <summary>Distâncias da parede de saída: esteira, atendente, centro da bancada e passageiro; a balança no vão do par, a meia sala até a boca
        /// lateral e a largura, o fundo e a altura da bancada.</summary>
        const float ChkEsteiraD = 0.14f, ChkAtrasD = 0.3f, ChkBalcaoD = 0.52f, ChkFrenteD = 0.8f, ChkBalancaD = 0.6f, ChkBalancaA = 0.85f, MeiaChk = 1.62f, BancadaL = 0.6f, BancadaF = 0.28f, BancadaH = 0.32f;
        /// <summary>O roteiro, em fração do atendimento: entrega o documento até aqui; põe a mala; etiqueta; e a devolução começa aqui.</summary>
        const float EntregaAte = 0.16f, MalaDe = 0.22f, MalaAte = 0.38f, EtiquetaAte = 0.6f, DevolveDe = 0.82f;
        /// <summary>O roteiro inteiro cabe em tanto, em s de jogo, quando o serviço é mais curto que isso.</summary>
        const float RoteiroBalcao = 2.4f, RoteiroLinha = 3.2f;
        /// <summary>Escala da malha do guichê e do cordão, de 1 m de largura.</summary>
        const float EscalaGuiche = EscalaSeg * 1.2f;
        static readonly Color Apagado = new Color(0.58f, 0.6f, 0.66f), CorBancada = Art.Hex(0x24406E), CorTampo = Art.Hex(0xEEF1F5), CorPedra = Art.Hex(0xC9B8A0), CorMonitor = Art.Hex(0x20242C), CorTela = Art.Hex(0x7FD4FF), CorTelaOff = Art.Hex(0x303642), CorPainel = Art.Hex(0x1E4E86), CorVidro = Art.Hex(0xBFE3F2);

        readonly Pax[,] _noPosto = new Pax[8, 4];
        readonly GameObject[,] _faixas = new GameObject[2, 4], _fechado = new GameObject[2, 4];
        readonly bool[,] _apagado = new bool[2, 4];
        readonly List<Renderer>[] _telas = new List<Renderer>[4];
        readonly Renderer[] _visor = new Renderer[4], _tira = new Renderer[4];
        readonly TextMesh[] _numero = new TextMesh[4];
        readonly SpriteRenderer[] _doc = new SpriteRenderer[4];

        /// <summary>O boneco sai do chão para a câmera atrás da bancada e das máquinas.</summary>
        static float FrenteAtras => LinhaM(0.3f);

        /// <summary>Bancada de um guichê no nível `n` da Velocidade (a malha do Tripo entra no lugar quando existir): corpo, tampo, monitor, teclado e impressora;
        /// o nível 1 ganha 2º monitor e divisória de vidro, o 2 ganha tampo de pedra e faixa de luz na frente.</summary>
        GameObject Bancada3D(int i, int n)
        {
            const int e = SalaCheckIn;
            Transform sala = _cont;
            _cont = Grupo("Bancada" + i + n, sala);
            Vector2 c = Balcao(e, i), frente = c - Salas[e].N * (LinhaM(BancadaF) * 0.5f + 0.004f);
            float l = LinhaM(BancadaL), f = LinhaM(BancadaF), h = LinhaM(BancadaH), topo = h + LinhaM(0.015f);
            Bloco("Corpo", PrimitiveType.Cube, V3(c, h * 0.5f), new Vector3(f, h, l), CorBancada);
            Bloco("Rodape", PrimitiveType.Cube, V3(c, LinhaM(0.015f)), new Vector3(f + LinhaM(0.02f), LinhaM(0.03f), l + LinhaM(0.02f)), CorMonitor);
            Bloco("Tampo", PrimitiveType.Cube, V3(c, h), new Vector3(f + LinhaM(0.05f), LinhaM(0.03f), l + LinhaM(0.05f)), n == 2 ? CorPedra : CorTampo);
            if (n == 2) _telas[i].Add(Bloco("Luz", PrimitiveType.Cube, V3(frente, h * 0.2f), new Vector3(0.012f, LinhaM(0.02f), l * 0.9f), CorTela));
            Vector2 t = Salas[e].T * Mathf.Sign(ChkBalancaA * Mathf.Sign(ChkA[i]) - ChkA[i]) * LinhaM(0.11f);
            Monitor(i, c + Salas[e].N * LinhaM(0.05f) - t, topo);
            if (n >= 1) Monitor(i, c + Salas[e].N * LinhaM(0.05f) + t, topo);
            Bloco("Teclado", PrimitiveType.Cube, V3(c + Salas[e].N * LinhaM(0.1f), topo + LinhaM(0.006f)), new Vector3(LinhaM(0.05f), LinhaM(0.012f), LinhaM(0.14f)), CorMonitor);
            Bloco("Impressora", PrimitiveType.Cube, V3(c - Salas[e].N * LinhaM(0.03f) + t * 2f, topo + LinhaM(0.03f)), new Vector3(LinhaM(0.08f), LinhaM(0.06f), LinhaM(0.08f)), Art.Paper);
            if (n >= 1) Bloco("Vidro", PrimitiveType.Cube, V3(c - Salas[e].N * LinhaM(0.1f), topo + LinhaM(0.1f)), new Vector3(0.012f, LinhaM(0.2f), l * 0.8f), CorVidro);
            GameObject g = _cont.gameObject;
            _cont = sala;
            return g;
        }

        void Monitor(int i, Vector2 p, float topo)
        {
            Vector2 rumo = new Vector2(0.5f, -0.87f);   // meio de lado: o atendente vê, e a câmera também
            Quaternion q = Quaternion.LookRotation(V3(rumo));
            float l = LinhaM(0.15f), h = LinhaM(0.1f), y = topo + LinhaM(0.03f) + h * 0.5f;
            Bloco("Pe", PrimitiveType.Cube, V3(p, topo + LinhaM(0.015f)), Vector3.one * LinhaM(0.03f), CorMonitor);
            Bloco("Monitor", PrimitiveType.Cube, V3(p, y), new Vector3(l, h, 0.014f), CorMonitor).transform.rotation = q;
            Renderer tela = Bloco("Tela", PrimitiveType.Cube, V3(p + rumo * 0.012f, y), new Vector3(l * 0.86f, h * 0.8f, 0.006f), CorTela);
            tela.transform.rotation = q;
            _telas[i].Add(tela);
        }

        /// <summary>Painel do guichê na parede, com o número: o posto fica mais alto que quem atende. Balança e esteira ficam por par (Esteiras3D).</summary>
        void Guiche3D(int i)
        {
            const int e = SalaCheckIn;
            _telas[i] = _telas[i] ?? new List<Renderer>();
            _visor[i] = Bloco("Visor", PrimitiveType.Cube, V3(Balcao(e, i) - Salas[e].N * (LinhaM(BancadaF) * 0.5f + 0.004f), LinhaM(BancadaH) * 0.62f), new Vector3(0.012f, LinhaM(0.08f), LinhaM(BancadaL) * 0.42f), CorTelaOff);
            Vector2 c = Em(e, LinhaM(0.03f), Posto(e, i));
            float l = LinhaM(BancadaL + 0.02f), h = LinhaM(0.62f), g = LinhaM(0.05f);
            Bloco("Painel", PrimitiveType.Cube, V3(c, h * 0.5f), new Vector3(g, h, l), CorPainel);
            _tira[i] = Bloco("Tira", PrimitiveType.Cube, V3(c - Salas[e].N * (g * 0.5f + 0.003f), h - LinhaM(0.03f)), new Vector3(0.012f, LinhaM(0.02f), l * 0.92f), CorTela);
            var go = new GameObject("Numero" + i);
            go.transform.SetParent(_cont, false);
            go.transform.SetPositionAndRotation(V3(c - Salas[e].N * (g * 0.5f + 0.01f), h - LinhaM(0.14f)), Quaternion.LookRotation(V3(Salas[e].N)));
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = Art.Font();
            tm.fontSize = 72;
            tm.characterSize = LinhaM(0.025f);
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontStyle = FontStyle.Bold;
            tm.color = Color.white;
            tm.text = (i + 1).ToString();
            go.GetComponent<MeshRenderer>().sharedMaterial = Art.Font().material;
            _numero[i] = tm;
            _doc[i] = Billboard("Doc" + i, Art.Bilhete(), Color.white, 0.2f * EscalaFunc);
        }

        /// <summary>A balança no vão de cada par e a esteira até a boca na parede lateral (Sala3D, uma vez).</summary>
        void Esteiras3D()
        {
            for (int lado = -1; lado <= 1; lado += 2)
            {
                Vector2 p = Em(SalaCheckIn, LinhaM(ChkBalancaD), lado * LinhaM(ChkBalancaA)), canto = Em(SalaCheckIn, LinhaM(ChkEsteiraD), lado * LinhaM(ChkBalancaA)), boca = Em(SalaCheckIn, LinhaM(ChkEsteiraD), lado * FimChk);
                Peca("balanca_bagagem", V3(p), Rumo(-Salas[SalaCheckIn].N), LinhaM(0.4f));
                Esteira(p, canto);
                Esteira(canto, boca);
                Boca(boca);
            }
        }

        static float FimChk => Mathf.Min(LinhaM(MeiaChk), Lado * 0.5f);
        static Vector2 BalancaChk(int i) => Em(SalaCheckIn, LinhaM(ChkBalancaD), Mathf.Sign(ChkA[i]) * LinhaM(ChkBalancaA));

        static void RotaChk(List<Vector2> r, int i)
        {
            float lado = Mathf.Sign(ChkA[i]);
            r.Add(Em(SalaCheckIn, LinhaM(ChkEsteiraD), lado * LinhaM(ChkBalancaA)));
            r.Add(Em(SalaCheckIn, LinhaM(ChkEsteiraD), lado * FimChk));
        }

        /// <summary>O chão da bancada do guichê `i` (PostosTests).</summary>
        public static Rect CaixaBancada(int i)
        {
            Vector2 c = Balcao(SalaCheckIn, i), t = Salas[SalaCheckIn].T * (LinhaM(BancadaL) * 0.5f);
            return Caixa(c - t, c + t, LinhaM(BancadaF) * 0.5f);
        }

        /// <summary>Onde para quem vai despachar: de lado no guichê, perto da balança do par.</summary>
        public static Vector2 PeBalanca(int i) => NoBalcao(SalaCheckIn, i) + Salas[SalaCheckIn].T * (Mathf.Sign(Vector2.Dot(BalancaChk(i) - NoBalcao(SalaCheckIn, i), Salas[SalaCheckIn].T)) * LinhaM(0.16f));

        /// <summary>A mala fica na balança do check-in se ela é despachada e a sala de bagagem, sem gente, não vai pegá-la.</summary>
        static bool DespachaAqui(Sim s, Pax p) => p.Mala && s.Presentes(Estacao.Bagagem) == 0;

        /// <summary>Faixas no chão (posto aberto) e cordão com placa (posto vazio) diante do balcão ou da linha da segurança (Sala3D).</summary>
        void Posto3D(int e, int i)
        {
            if (e == SalaCheckIn) Guiche3D(i);
            Vector2 n = Salas[e].N, t = Salas[e].T, frente = e == SalaSeg ? Parada(i, 0) : NoBalcao(e, i);
            Transform sala = _cont;
            _cont = Grupo("Faixas" + i, sala);
            _faixas[e, i] = _cont.gameObject;
            for (int f = 0; f < 3; f++)   // a pessoa da vez para aqui
            {
                Vector2 p = frente - n * ((0.2f + f * 0.1f) * EscalaFunc);
                Vector3 tam = Mathf.Abs(n.x) > 0.5f ? new Vector3(0.05f * EscalaFunc, 0.016f, 0.56f * EscalaFunc) : new Vector3(0.56f * EscalaFunc, 0.016f, 0.05f * EscalaFunc);
                Bloco("Faixa", PrimitiveType.Cube, V3(p, 0.008f), tam, Art.Paper);
            }
            _cont = Grupo("Fechado" + i, sala);
            _fechado[e, i] = _cont.gameObject;
            if (Peca("cordao_fila", V3(frente), Rumo(n), EscalaGuiche) == null) Bloco("Cordao", PrimitiveType.Cube, V3(frente, 0.2f), Mathf.Abs(n.x) > 0.5f ? new Vector3(0.02f, 0.03f, 0.6f) : new Vector3(0.6f, 0.03f, 0.02f), Fita);
            Vector2 placa = frente - n * (0.16f * EscalaFunc) + t * (0.22f * EscalaFunc);
            if (Peca("placa_fora_de_servico", V3(placa), Rumo(-n), 0.5f * EscalaFunc) == null) Bloco("Placa", PrimitiveType.Cube, V3(placa, 0.2f), Vector3.one * 0.2f, Art.Bad);
            _cont = sala;
        }

        static Transform Grupo(string nome, Transform pai)
        {
            var g = new GameObject(nome).transform;
            g.SetParent(pai, false);
            return g;
        }

        static bool ComRoteiro(int e) => e == SalaCheckIn || e == SalaSeg;

        /// <summary>Cada um da lista de atendimento no posto em que chegou. O núcleo não espera a pessoa andar até o balcão: no check-in e na segurança, quem
        /// ele já liberou fica na tela até o roteiro acabar, a não ser que outro precise do posto.</summary>
        void Reparte(Sim s, Station st)
        {
            int e = (int)st.Tipo;
            for (int i = 0; i < 4; i++)
            {
                Pax p = _noPosto[e, i];
                if (p == null || st.Atendendo.Contains(p)) continue;
                if (!ComRoteiro(e) || !_pax.TryGetValue(p.Id, out Boneco b) || b.Saindo || (b.RotDur > 0f && b.RotT >= b.RotDur)) Libera(e, i);
            }
            int gente = s.Presentes(st.Tipo), limite = gente > 0 ? Mathf.Min(gente, st.Postos) : st.Postos;
            foreach (Pax p in st.Atendendo)
                if (!NoPosto(e, p)) _noPosto[e, PostoLivre(st, e, limite)] = p;
        }

        bool NoPosto(int e, Pax p)
        {
            for (int i = 0; i < 4; i++) if (_noPosto[e, i] == p) return true;
            return false;
        }

        /// <summary>O 1º posto livre com gente; sem nenhum, o de quem o núcleo já liberou; sem gente em nenhum, qualquer um.</summary>
        int PostoLivre(Station st, int e, int limite)
        {
            for (int i = 0; i < limite; i++) if (_noPosto[e, i] == null) return i;
            for (int i = 0; i < limite; i++) if (!st.Atendendo.Contains(_noPosto[e, i])) { Libera(e, i); return i; }
            for (int i = 0; i < 4; i++) if (_noPosto[e, i] == null) return i;
            return 0;
        }

        void Libera(int e, int i)
        {
            if (_pax.TryGetValue(_noPosto[e, i].Id, out Boneco b)) b.RotT = b.RotDur = 0f;
            _noPosto[e, i] = null;
        }

        /// <summary>Liga: o passageiro que o núcleo já mandou para a sala `e` ainda está no posto da sala anterior.</summary>
        bool SeguraNoPosto(Boneco b, Pax p, int e) => b.Zona != e && b.Zona >= 0 && ComRoteiro(b.Zona) && b.Vaga >= VagaPosto && _noPosto[b.Zona, b.Vaga - VagaPosto] == p;

        /// <summary>Quanto do atendimento já passou na tela. No check-in e na segurança o relógio começa quando o passageiro chega, dura o que falta no núcleo e
        /// nunca menos que o roteiro inteiro legível. Negativo = ainda não chegou.</summary>
        float Andamento(Pax p, int e)
        {
            if (!ComRoteiro(e)) return 1f - p.Resto / (float)Mathf.Max(1, p.Total);
            return _pax.TryGetValue(p.Id, out Boneco b) && b.RotDur > 0f ? Mathf.Clamp01(b.RotT / b.RotDur) : -1f;
        }

        float Avanca(Sim s, Pax p, int e, float dt)
        {
            if (ComRoteiro(e) && _pax.TryGetValue(p.Id, out Boneco b) && b.NoBalcao)
            {
                if (b.RotDur <= 0f && b.Caminho.Count == 0) b.RotDur = Mathf.Max(e == SalaSeg ? RoteiroLinha : RoteiroBalcao, s.TempoSeg((Estacao)e) * p.Resto / Mathf.Max(1, p.Total));
                if (b.RotDur > 0f) b.RotT += dt;
            }
            return Mathf.Max(0f, Andamento(p, e));
        }

        /// <summary>Salas3D, por posto: o que muda quando ele fica sem gente. `vazio` = comprado e sem funcionário.</summary>
        void EstadoPosto(Sim s, Station st, int e, int i, bool vazio)
        {
            if (e != SalaCheckIn && e != SalaSeg) return;
            bool apagado = vazio && _noPosto[e, i] == null;
            _faixas[e, i].SetActive(i < st.Postos && !apagado);
            _fechado[e, i].SetActive(apagado && i >= s.Alocados(st.Tipo));
            if (e == SalaCheckIn)
            {
                _numero[i].gameObject.SetActive(i < st.Postos);
                _visor[i].gameObject.SetActive(i < st.Postos);
                Pax p = _noPosto[e, i];
                Cor(_visor[i], p != null ? Art.VooCor[p.Voo] : apagado ? CorTelaOff : CorTela);
            }
            if (_apagado[e, i] == apagado) return;
            _apagado[e, i] = apagado;
            Color c = apagado ? Apagado : Color.white;
            for (int n = 0; n < NivelMalha.Length; n++) Tinge(_mesa[e, i, n], c);
            if (e == SalaSeg) { Tinge(_portico[i] != null ? _portico[i].gameObject : null, c); Tinge(_cabine[i] != null ? _cabine[i].gameObject : null, c); return; }
            foreach (Renderer r in _telas[i]) Cor(r, apagado ? CorTelaOff : CorTela);
            Cor(_tira[i], apagado ? CorTelaOff : CorTela);
            _numero[i].color = apagado ? Apagado : Color.white;
        }

        /// <summary>Tinge as malhas do Tripo. As primitivas têm a cor delas e quem apaga são as telas.</summary>
        static void Tinge(GameObject g, Color c)
        {
            if (g == null) return;
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true)) if (r.name == "malha") Cor(r, c);
        }

        /// <summary>Pinta: quanto o boneco em `p` sai do chão para a câmera. Só atrás da bancada do check-in e das máquinas da segurança.</summary>
        static float FrentePosto(Vector2 p)
        {
            int e = SalaEm(p);
            bool atras = e == SalaSeg ? AtrasDaLinha(p) : e == SalaCheckIn && Vector2.Dot(p - Balcao(e, 0), Salas[e].N) > 0.02f;
            return atras ? FrenteAtras : 0f;
        }

        // ------------------------------------------------------------------ o roteiro do atendimento

        /// <summary>0 a 1 e de volta a 0 entre as frações `a` e `b`: o braço vai e volta dentro da etapa.</summary>
        static float IdaEVolta(float f, float a, float b) => Mathf.PingPong(Mathf.InverseLerp(a, b, f) * 2f, 1f);

        /// <summary>Quadro do gesto pela fração `t` (0 a 1); t negativo = laço no tempo.</summary>
        static void Gesto(Boneco b, int clipe, float t)
        {
            b.Gesto = clipe;
            b.GestoT = t;
        }

        /// <summary>Quem atende no posto `i`, parado no lugar: pega o documento, digita, etiqueta a mala e devolve; na segurança olha a tela do raio-x enquanto a
        /// bandeja anda e passa o scanner na vistoria.</summary>
        void GestoFunc(Boneco b, int e, int i)
        {
            Pax p = _noPosto[e, i];
            if (p == null) return;
            float f = Andamento(p, e);
            b.Atende = f >= 0f;
            if (f < 0f) return;
            if (e == SalaSeg)
            {
                if (_pax.TryGetValue(p.Id, out Boneco dono) && Vistoria(p) && dono.Etapa == 2 && dono.Caminho.Count == 0) Gesto(b, b.Bastao, Mathf.PingPong(Time.time * 1.6f, 1f));
                else if (f < 0.8f) { Gesto(b, b.TelaRaioX, -1f); Olha(b, Salas[e].T * -Mathf.Sign(SegA[i])); }
                return;
            }
            if (f < EntregaAte) Gesto(b, b.Pega, IdaEVolta(f, 0f, EntregaAte));
            else if (f >= DevolveDe) Gesto(b, b.Pega, IdaEVolta(f, DevolveDe, 1f));
            else if (e == SalaCheckIn && f >= MalaAte && f < EtiquetaAte && DespachaAqui(S, p))
            {
                Gesto(b, b.Etiqueta, IdaEVolta(f, MalaAte, EtiquetaAte));
                Olha(b, BalancaChk(i) - b.Pos);
            }
            else Gesto(b, b.Digita, -1f);
        }

        /// <summary>O passageiro no balcão: entrega o documento, põe a mala na balança e recebe o documento de volta. O documento passa de uma mão à outra.</summary>
        void GestoPax(Sim s, Boneco b, Pax p, int e, int i, float f)
        {
            if (e == SalaCheckIn) DocPasso(i, b, f);
            if (b.Caminho.Count > 0) return;
            if (f < EntregaAte) Gesto(b, b.Entrega, IdaEVolta(f, 0f, EntregaAte));
            else if (f >= DevolveDe) Gesto(b, b.Entrega, IdaEVolta(f, DevolveDe, 1f));
            else if (e != SalaCheckIn || !DespachaAqui(s, p) || b.Despachou) return;
            else if (f >= MalaAte) Pousa(b, p);
            else if (f >= MalaDe)
            {
                Gesto(b, b.PoeMala, Mathf.InverseLerp(MalaDe, MalaAte, f));
                Olha(b, BalancaChk(i) - b.Pos);
            }
        }

        void DocPasso(int i, Boneco b, float f)
        {
            bool ve = b.Caminho.Count == 0 && (f < EntregaAte || f >= DevolveDe);
            _doc[i].enabled = ve;
            if (!ve) return;
            float t = f < EntregaAte ? Mathf.InverseLerp(0.3f, 0.7f, f / EntregaAte) : 1f - Mathf.InverseLerp(0.3f, 0.7f, (f - DevolveDe) / (1f - DevolveDe));
            Vector3 c = V3(Vector2.Lerp(b.Pos, AtrasBalcao(SalaCheckIn, i), t), LinhaM(0.36f)) - _cam.transform.forward * LinhaM(0.6f);
            _doc[i].transform.SetPositionAndRotation(c, _cam.transform.rotation);
            _doc[i].sortingOrder = Ordem(c) + 9;
        }
    }
}
