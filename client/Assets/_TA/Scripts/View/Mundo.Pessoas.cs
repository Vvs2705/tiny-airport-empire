using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Pessoas do mundo (GDD §5.13, D-18): sprites pre-renderizados do Tripo (render_sprites.py, 4 diagonais com espelho) num quad virado
    /// para a camera, com o uniforme ou a mala numa mascara que o shader tinge pela cor do vertice (SpriteTinta).
    ///  - Passageiros (4 corpos) com a mala na cor do voo; andam pela fila, vao ao balcao, seguem pelas portas e embarcam pela ponte; quem
    ///    perde o voo sai pela porta. Na fila critica, os 3 da frente mostram o balao de irritacao.
    ///  - Funcionarios (os 6 perfis): andam de sala em sala pelas portas no tempo da Tuning.Caminhada e, ao chegar, o uniforme assenta na
    ///    cor do setor (0,35 s); atras do balcao, "atende" quando o posto dele tem passageiro.
    ///  - Patio (com o uniforme da funcao): a equipe de patio (uma pessoa por sim.EquipePatio) vai ate o servico que comecou; o mecanico sai da
    ///    oficina e vai ao aviao em checagem; a tripulacao escalada anda da sala ate o aviao e volta (GDD §5.11); o despachante sinaliza o estande G.
    ///  - Pool: os bonecos de passageiro voltam para a pilha e trocam de folha ao renascer (nada se cria ou destroi no quadro quente).
    /// </summary>
    public sealed partial class Mundo
    {
        static readonly string[] PaxAtor = { "passageiro_a", "passageira_b", "passageiro_idoso", "passageira_b", "passageiro_a", "crianca" };
        /// <summary>Corpo do passageiro: hash de Fibonacci do id (a conta linear so caia em indices pares e a fila saia quase toda igual).</summary>
        static string CorpoDe(Pax p) => PaxAtor[(int)(((uint)p.Id * 2654435761u) >> 24) % PaxAtor.Length];
        static readonly string[] FuncAtor = { "atendente", "seguranca", "vendedor", "bagageiro", "mecanico", "despachante" };   // Textos.Funcionario (a vendedora do Tripo e mulher: o Caio virou Cida em 29/10)
        const float PassoNatural = 1.1f;   // m/s em que o andar da folha nao patina (adulto de 1 m)

        sealed class Boneco
        {
            public Transform T;
            public SpriteRenderer Sombra, Corpo, Coracao, Humor;
            public SpriteSheet Folha;
            public int Andar, Parado, Atender, Clip = -1;
            public float Tempo, Vel;
            public Color Tinta = Color.white;
            public Vector2 Pos;
            public readonly List<Vector2> Caminho = new List<Vector2>();
            public int Zona = -1;
            public bool Saindo, Andou, Cima, Atende;
            public float Dir = 1f, Escala = 1f;
        }

        /// <summary>Gente do patio: pontos de uma volta curta e a pausa em cada um.</summary>
        sealed class Ator { public Boneco B; public Vector2[] Pontos; public float Pausa, Espera; public int I; }

        /// <summary>Tripulante andando entre a sala e o aviao: volta para o pool quando chega.</summary>
        sealed class Andante { public Boneco B; }

        static readonly Color ColetePatio = Art.Hex(0xF2A33A), MacacaoMecanico = Art.Hex(0x2A7A88), ColeteSinal = Art.Hex(0xE8D24A);
        /// <summary>Onde a pessoa de patio fica em cada servico: estande G, depois R (desembarque, limpeza, combustivel, bagagem).</summary>
        static readonly Vector2[,] PontoServico =
        {
            { new Vector2(10.2f, 11.5f), new Vector2(12.2f, 11.6f), new Vector2(13.9f, 14.2f), new Vector2(12.2f, 15.4f) },
            { new Vector2(22.1f, 10.8f), new Vector2(20.3f, 11.6f), new Vector2(18.7f, 14.2f), new Vector2(20.3f, 15.4f) },
        };
        static readonly Vector2 MecanicoCasa = new Vector2(17.9f, 7.4f), PortaOficina = new Vector2(18f, 8f);
        static readonly Vector2[] MecanicoPonto = { new Vector2(12.4f, 10.5f), new Vector2(20f, 10.5f) };
        /// <summary>A sala da tripulacao, a porta do aviao em cada estande e o caminho do embarque remoto (porta do portao, patio e escada).</summary>
        static readonly Vector2 SalaTrip = new Vector2(7.6f, 8.6f), PonteIni = new Vector2(10f, 8.4f), RemotoPorta = new Vector2(11.2f, 8f), RemotoPatio = new Vector2(21.8f, 8.8f), RemotoFim = new Vector2(21.8f, 11.1f);

        readonly List<Ator> _patio = new List<Ator>();
        readonly List<Boneco> _equipe = new List<Boneco>();
        readonly List<int> _equipeLida = new List<int>();   // aviao x 4 + servico de cada pessoa de patio (-1 = sem servico)
        readonly List<Andante> _andantes = new List<Andante>();
        readonly List<int> _tripEm = new List<int>();   // onde a tela viu cada tripulante: -1 = sala, senao o aviao
        Boneco _mecanico;
        int _mecanicoEm = -1;

        Boneco NovoBoneco(string ator)
        {
            var b = new Boneco { T = new GameObject(ator).transform };
            b.T.SetParent(_raiz, false);
            b.Sombra = Art.NewSprite(b.T, "Sombra", Art.Disc(), new Color(0f, 0f, 0f, 0.3f), 0, new Vector2(0f, 0.02f), new Vector2(0.46f, 0.16f));
            b.Corpo = Art.NewSprite(b.T, "Corpo", null, Color.white, 0, Vector2.zero, Vector2.one);
            if (SpriteSheet.Material != null) b.Corpo.sharedMaterial = SpriteSheet.Material;
            b.Coracao = Art.NewSprite(b.T, "Historia", Art.Heart(), Art.Bad, 0, new Vector2(0f, 1.12f), new Vector2(0.26f, 0.26f));
            b.Humor = Art.NewSprite(b.T, "Humor", Art.Alerta(), Art.Bad, 0, new Vector2(0.18f, 1.1f), new Vector2(0.24f, 0.24f));
            b.Coracao.enabled = b.Humor.enabled = false;
            Veste(b, ator);
            return b;
        }

        /// <summary>Troca a folha do boneco (o pool reaproveita o boneco com outro corpo). Sem folha (pasta vazia, -nographics): uma
        /// silhueta arredondada no lugar, para o mundo continuar legivel.</summary>
        static void Veste(Boneco b, string ator)
        {
            SpriteSheet f = SpriteSheet.Get(ator) ?? SpriteSheet.Get(PaxAtor[0]);
            b.Folha = f;
            b.Clip = -1;
            if (f == null)
            {
                b.Corpo.sprite = Art.Rounded();
                b.Corpo.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                b.Corpo.transform.localScale = new Vector3(0.36f, 0.7f, 1f);
                return;
            }
            b.Andar = f.Pick("Walk"); b.Parado = f.Pick("Idle"); b.Atender = f.Pick("Atende", "Idle");
        }

        /// <summary>Olhar na direcao `d` do chao: para cima/baixo e esquerda/direita na tela (a folha tem as 2 diagonais da esquerda).</summary>
        void Olha(Boneco b, Vector2 d)
        {
            Vector3 v = V3(d);
            float lado = Vector3.Dot(v, _cam.transform.right), cima = Vector3.Dot(v, _cam.transform.up);
            if (Mathf.Abs(lado) > 1e-5f) b.Dir = Mathf.Sign(lado);
            if (Mathf.Abs(cima) > 1e-5f) b.Cima = cima > 0f;
        }

        // ------------------------------------------------------------------ passageiros

        void SincronizaPax(Sim s, float dt)
        {
            _vistos.Clear();
            foreach (Station st in s.Estacoes)
            {
                int e = (int)st.Tipo;
                for (int k = 0; k < st.Fila.Count; k++) Liga(st.Fila[k], e, Fila(e, k), s, false, st.Estado == Estado.Critico && k < 3);
                for (int i = 0; i < 4; i++)
                {
                    bool ocupado = i < st.Atendendo.Count;
                    _arco[e, i].enabled = ocupado;
                    if (!ocupado) continue;
                    Pax p = st.Atendendo[i];
                    Liga(p, e, NoBalcao(e, i), s, true, false);
                    _arco[e, i].sprite = Art.Arc(1f - p.Resto / (float)Mathf.Max(1, p.Total));
                    Vector3 pa = V3(NoBalcao(e, i), 1.35f);
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
                Anda(b, VelPax * dt, dt);
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

        void Liga(Pax p, int e, Vector2 alvo, Sim s, bool noBalcao, bool bravo)
        {
            _vistos.Add(p.Id);
            if (!_pax.TryGetValue(p.Id, out Boneco b))
            {
                string ator = CorpoDe(p);
                if (_livres.Count > 0) { b = _livres.Pop(); Veste(b, ator); } else b = NovoBoneco(ator);
                b.T.gameObject.SetActive(true);
                b.Pos = Calcada + new Vector2(-0.4f * (p.Indice % 3), 0.3f * (p.Indice % 4) - 0.45f);
                b.Zona = -1; b.Saindo = false; b.Caminho.Clear();
                b.Coracao.enabled = p.Historia;
                _pax[p.Id] = b;
            }
            if (b.Zona != e)
            {
                b.Caminho.Clear();
                if (b.Zona < 0) b.Caminho.Add(Salas[0].In);
                PelasPortas(Mathf.Max(0, b.Zona), e, b.Caminho);   // de porta em porta
                b.Caminho.Add(alvo);
                b.Zona = e;
            }
            else if (b.Caminho.Count == 0) b.Caminho.Add(alvo);
            else b.Caminho[b.Caminho.Count - 1] = alvo;
            if (b.Caminho.Count == 0 || (b.Caminho.Count == 1 && (b.Caminho[0] - b.Pos).sqrMagnitude < 1e-4f)) Olha(b, Salas[e].N);   // parado: de frente para o balcao
            b.Humor.enabled = bravo;
            Voo v = s.VooDe(p);
            Color mala = Art.VooCor[p.Voo];
            b.Tinta = e == (int)Estacao.Portao && v != null && !v.Embarque && !noBalcao ? Color.Lerp(mala, Art.Hex(0x8A8F99), 0.5f) : mala;   // no portao, apagada ate abrir o embarque
        }

        /// <summary>Saiu do nucleo: embarcou (Ev.Embarcou) = pela ponte ate o aviao do estande G, ou a pe pelo patio ate a escada do R; senao
        /// perdeu o voo = sai pela porta por onde entrou.</summary>
        void Sai(int id, Boneco b)
        {
            b.Saindo = true;
            b.Humor.enabled = false;
            b.Caminho.Clear();
            int z = Mathf.Clamp(b.Zona, 0, 5);
            if (!_embarcou.Contains(id)) b.Caminho.Add(Salas[z].In + (Salas[z].In - Centro(z)).normalized * 0.6f);   // um passo para fora da porta
            else if (id / 500 % 2 == 0) { b.Caminho.Add(Salas[3].Out); b.Caminho.Add(PonteFim); }
            else { b.Caminho.Add(RemotoPorta); b.Caminho.Add(RemotoPatio); b.Caminho.Add(RemotoFim); }
        }

        void Anda(Boneco b, float passo, float dt)
        {
            Vector2 antes = b.Pos;
            while (passo > 0f && b.Caminho.Count > 0)
            {
                Vector2 d = b.Caminho[0] - b.Pos;
                float l = d.magnitude;
                if (l <= passo) { b.Pos = b.Caminho[0]; passo -= l; b.Caminho.RemoveAt(0); }
                else { b.Pos += d / l * passo; passo = 0f; }
            }
            Movido(b, b.Pos - antes, dt);
        }

        void Movido(Boneco b, Vector2 mov, float dt)
        {
            b.Andou = mov.sqrMagnitude > 1e-8f;
            b.Vel = b.Andou && dt > 0f ? mov.magnitude / dt : 0f;
            if (b.Andou) Olha(b, mov);
        }

        /// <summary>Billboard: o quadro do clipe (andar no ritmo da passada, parado, atendendo), o espelho e a ordem por profundidade.</summary>
        void Pinta(Boneco b)
        {
            Vector3 p = V3(b.Pos);
            b.T.position = p;
            b.T.rotation = _cam.transform.rotation;
            b.T.localScale = new Vector3(b.Escala, b.Escala, 1f);
            if (b.Folha != null)
            {
                int clip = b.Andou ? b.Andar : b.Atende ? b.Atender : b.Parado;
                if (clip != b.Clip) { b.Clip = clip; b.Tempo = 0f; }
                b.Tempo += Time.deltaTime * (b.Andou ? Mathf.Clamp(b.Vel / PassoNatural, 0.7f, 2.3f) : 1f);
                b.Corpo.sprite = b.Folha.Frame(clip, b.Cima ? 1 : 0, b.Folha.FrameAt(clip, b.Tempo));
                b.Corpo.flipX = b.Dir > 0f;
            }
            b.Corpo.color = b.Folha != null ? b.Tinta : Color.Lerp(b.Tinta, Art.Hex(0xD5DBDB), 0.3f);
            int o = Ordem(p);
            b.Sombra.sortingOrder = o; b.Corpo.sortingOrder = o + 3; b.Coracao.sortingOrder = b.Humor.sortingOrder = o + 8;
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
                if (_colete[k] < 0) { _colete[k] = (int)f.Em; b.Pos = Lugar(s, f, out _); _troca[k] = Troca; }   // contratado: aparece ja de uniforme
                b.Atende = false;
                if (Arrastando == k) { b.Andou = false; }   // o arrasto posiciona
                else if (f.Andando > 0)
                {
                    if (_rota[k].Count == 0 || _rotaPara[k] != f.Em) Rota(k, s, f);
                    float total = Mathf.Max(1f, s.T.Caminhada * Sim.Hz);
                    Vector2 antes = b.Pos;
                    b.Pos = NaRota(_rota[k], 1f - f.Andando / total);
                    Movido(b, b.Pos - antes, Mathf.Max(dt, 1e-4f));
                }
                else
                {
                    _rota[k].Clear();
                    b.Caminho.Clear();
                    Vector2 lugar = Lugar(s, f, out int posto);
                    b.Caminho.Add(lugar);
                    Anda(b, VelPax * 1.5f * Mathf.Max(dt, Time.deltaTime), Mathf.Max(dt, Time.deltaTime));
                    if (!b.Andou)
                    {
                        Olha(b, -Salas[(int)f.Em].N);   // de frente para a fila
                        b.Atende = posto < s.St(f.Em).Atendendo.Count;
                    }
                    if (_colete[k] != (int)f.Em) { _colete[k] = (int)f.Em; _troca[k] = Troca; }   // chegou: veste o uniforme do setor
                }
                float t = _troca[k] > 0f ? 1f - _troca[k] / Troca : 1f;
                if (_troca[k] > 0f) _troca[k] = Mathf.Max(0f, _troca[k] - Time.deltaTime);
                b.Tinta = Color.Lerp(Color.white, Art.EstacaoCor[_colete[k]], t);   // flash claro que assenta na cor do setor
                b.Escala = EscalaFunc * ((Arrastando == k ? 1.25f : 1f) + 0.3f * Mathf.Sin(Mathf.PI * t) * (_troca[k] > 0f ? 1f : 0f));
                Vector2 real = b.Pos;
                if (_falha[k] > 0f)
                {
                    b.Pos += new Vector2(_cam.transform.right.x, _cam.transform.right.z) * Mathf.Sin(Time.unscaledTime * 60f) * 0.12f * _falha[k];
                    _falha[k] = Mathf.Max(0f, _falha[k] - Time.unscaledDeltaTime * 2f);
                }
                Pinta(b);
                b.Pos = real;
                Coloca(_nome[k].rectTransform, V3(b.Pos, 1.2f * b.Escala));
                _nome[k].color = f.Andando > 0 ? Art.Warn : Art.Ink;
            }
        }

        /// <summary>Lugar do funcionario atras do balcao da sala (o n-esimo da sala, na ordem de contratacao) e o posto dele.</summary>
        static Vector2 Lugar(Sim s, Funcionario f, out int posto)
        {
            int n = 0;
            foreach (Funcionario o in s.Staff) { if (o == f) break; if (o.Em == f.Em) n++; }
            posto = n % 4;
            return AtrasBalcao((int)f.Em, posto);
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
            PelasPortas(de, para, r);
            r.Add(Lugar(s, f, out _));
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

        // ------------------------------------------------------------------ gente do patio

        void GentePatio()
        {
            NovoAtor("despachante", 3f, new Vector2(12.2f, 9.2f), new Vector2(12.7f, 9.2f));
            if (_patio.Count > 0) _patio[0].B.Tinta = ColeteSinal;
            _mecanico = NovoBoneco("mecanico");
            _mecanico.Pos = MecanicoCasa;
            _mecanico.Tinta = MacacaoMecanico;
        }

        void NovoAtor(string ator, float pausa, params Vector2[] pontos)
        {
            if (SpriteSheet.Get(ator) == null && SpriteSheet.Get(PaxAtor[0]) != null) return;   // sem a folha dele: nao vira passageiro no patio
            var a = new Ator { B = NovoBoneco(ator), Pontos = pontos, Pausa = pausa, Espera = pausa * 0.5f };
            a.B.Pos = pontos[0];
            _patio.Add(a);
        }

        void AndaPatio(float dt)
        {
            foreach (Ator a in _patio)
            {
                Boneco b = a.B;
                if (b.Caminho.Count == 0)
                {
                    a.Espera -= dt;
                    if (a.Espera <= 0f) { a.I = (a.I + 1) % a.Pontos.Length; b.Caminho.Add(a.Pontos[a.I]); a.Espera = a.Pausa; }
                }
                Anda(b, 1.2f * dt, dt);
                Pinta(b);
            }
            EquipePasso(S, dt);
            MecanicoPasso(S, dt);
            TripulacaoPasso(S, dt);
        }

        /// <summary>Equipe de patio: uma pessoa por sim.EquipePatio. Cada servico que comecou chama a primeira sem servico; quem termina volta
        /// para a beira do patio.</summary>
        void EquipePasso(Sim s, float dt)
        {
            while (_equipe.Count < s.EquipePatio)
            {
                Boneco b = NovoBoneco("bagageiro");
                b.Pos = Ocioso(_equipe.Count);
                b.Tinta = ColetePatio;
                _equipe.Add(b);
                _equipeLida.Add(-1);
            }
            for (int i = 0; i < _equipe.Count; i++)
            {
                int l = _equipeLida[i];
                if (l >= 0 && !s.Avioes[l / 4].Fazendo[l % 4]) { _equipeLida[i] = -1; Vai(_equipe[i], Ocioso(i)); }
            }
            for (int k = 0; k < 2; k++)
                for (int sv = 0; sv < 4; sv++)
                {
                    int l = k * 4 + sv;
                    if (!s.Avioes[k].Fazendo[sv] || _avioes[k].Fase != Fase.NoPatio || _equipeLida.Contains(l)) continue;
                    int i = _equipeLida.IndexOf(-1);
                    if (i < 0) continue;
                    _equipeLida[i] = l;
                    Vai(_equipe[i], PontoServico[k, sv]);
                }
            for (int i = 0; i < _equipe.Count; i++)
            {
                Boneco b = _equipe[i];
                Anda(b, 2.2f * dt, dt);
                b.Atende = _equipeLida[i] >= 0 && b.Caminho.Count == 0;
                Pinta(b);
            }
        }

        static Vector2 Ocioso(int i) => new Vector2(14.4f + i * 0.5f, 9.8f);

        static void Vai(Boneco b, Vector2 p)
        {
            b.Caminho.Clear();
            b.Caminho.Add(p);
        }

        /// <summary>O mecanico (GDD §5.12): sai da oficina pela porta do patio ate o aviao em checagem e volta quando acaba.</summary>
        void MecanicoPasso(Sim s, float dt)
        {
            int k = -1;
            for (int i = 0; i < 2; i++) if (s.Avioes[i].Check > 0 && s.Avioes[i].NoPatio) k = i;
            Boneco b = _mecanico;
            if (k != _mecanicoEm)
            {
                _mecanicoEm = k;
                b.Caminho.Clear();
                if (k >= 0)
                {
                    if (b.Pos.y < PortaOficina.y) b.Caminho.Add(PortaOficina);
                    b.Caminho.Add(new Vector2(PortaOficina.x, 9.4f));
                    b.Caminho.Add(new Vector2(MecanicoPonto[k].x, 9.4f));
                    b.Caminho.Add(MecanicoPonto[k]);
                }
                else
                {
                    b.Caminho.Add(new Vector2(b.Pos.x, 9.4f));
                    b.Caminho.Add(new Vector2(PortaOficina.x, 9.4f));
                    b.Caminho.Add(PortaOficina);
                    b.Caminho.Add(MecanicoCasa);
                }
            }
            Anda(b, 2.6f * dt, dt);
            b.Atende = k >= 0 && b.Caminho.Count == 0;
            if (!b.Andou && b.Atende) Olha(b, _avioes[k].T.position.x > b.Pos.x ? Vector2.right : Vector2.left);
            Pinta(b);
        }

        /// <summary>Quem o nucleo poe a bordo anda da sala ate a porta do aviao; quem desce faz o caminho de volta (GDD §5.11). O save abre com
        /// cada um ja no lugar.</summary>
        void TripulacaoPasso(Sim s, float dt)
        {
            for (int i = 0; i < s.Tripulacao.Count; i++)
            {
                Tripulante t = s.Tripulacao[i];
                if (i >= _tripEm.Count) _tripEm.Add(t.Aviao);
                if (_tripEm[i] == t.Aviao) continue;
                if (_tripEm[i] >= 0) Caminha(t, _tripEm[i], true);
                if (t.Aviao >= 0) Caminha(t, t.Aviao, false);
                _tripEm[i] = t.Aviao;
            }
            for (int i = _andantes.Count - 1; i >= 0; i--)
            {
                Boneco b = _andantes[i].B;
                Anda(b, 1.7f * dt, dt);
                Pinta(b);
                if (b.Caminho.Count > 0) continue;
                b.T.gameObject.SetActive(false);
                _livres.Push(b);
                _andantes.RemoveAt(i);
            }
        }

        void Caminha(Tripulante t, int aviao, bool desce)
        {
            string ator = t.Funcao == Funcao.Piloto ? "piloto" : t.Indice % 2 == 0 ? "comissaria" : "comissario";
            Boneco b;
            if (_livres.Count > 0) { b = _livres.Pop(); Veste(b, ator); } else b = NovoBoneco(ator);
            b.T.gameObject.SetActive(true);
            b.Tinta = Color.white;
            b.Coracao.enabled = b.Humor.enabled = false;
            b.Saindo = false; b.Zona = -1;
            b.Caminho.Clear();
            var r = new List<Vector2> { SalaTrip, PonteIni };
            if (aviao == 0) r.Add(PonteFim);
            else { r.Add(RemotoPatio); r.Add(RemotoFim); }
            if (desce) r.Reverse();
            b.Pos = r[0];
            for (int i = 1; i < r.Count; i++) b.Caminho.Add(r[i]);
            _andantes.Add(new Andante { B = b });
        }
    }
}
