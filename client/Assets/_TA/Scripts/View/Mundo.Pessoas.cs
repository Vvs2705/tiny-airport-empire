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
    ///  - Funcionarios (Ana, Beto, Cida): andam de sala em sala pelas portas no tempo da Tuning.Caminhada e, ao chegar, o uniforme assenta na
    ///    cor do setor (0,35 s); atras do balcao, "atende" quando o posto dele tem passageiro.
    ///  - Patio: piloto, comissaria e comissario (Lote 3), despachante, bagageiro e mecanico com o uniforme da funcao, andando perto dos avioes (so visual).
    ///  - Pool: os bonecos de passageiro voltam para a pilha e trocam de folha ao renascer (nada se cria ou destroi no quadro quente).
    /// </summary>
    public sealed partial class Mundo
    {
        static readonly string[] PaxAtor = { "passageiro_a", "passageira_b", "passageiro_idoso", "passageira_b", "passageiro_a", "crianca" };
        /// <summary>Corpo do passageiro: hash de Fibonacci do id (a conta linear so caia em indices pares e a fila saia quase toda igual).</summary>
        static string CorpoDe(Pax p) => PaxAtor[(int)(((uint)p.Id * 2654435761u) >> 24) % PaxAtor.Length];
        static readonly string[] FuncAtor = { "atendente", "seguranca", "vendedor" };   // Ana, Beto, Cida (Textos.Funcionario; a vendedora do Tripo e mulher: o Caio virou Cida em 29/10)
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

        readonly List<Ator> _patio = new List<Ator>();

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
            foreach (Station st in s.Stations)
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
                    _arco[e, i].sprite = Art.Arc(1f - p.Resto / (float)s.TempoPassos(st.Tipo));
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
                for (int z = b.Zona + 1; z <= e; z++) { b.Caminho.Add(Salas[z].In); if (z < e) b.Caminho.Add(Salas[z].Out); }   // de porta em porta
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

        /// <summary>Saiu do nucleo: embarcou (Ev.Embarcou) = pela porta norte do portao e pela ponte ate o aviao; senao perdeu o voo = sai
        /// pela porta por onde entrou.</summary>
        void Sai(int id, Boneco b)
        {
            b.Saindo = true;
            b.Humor.enabled = false;
            b.Caminho.Clear();
            int z = Mathf.Clamp(b.Zona, 0, 3);
            if (_embarcou.Contains(id)) { b.Caminho.Add(Salas[3].Out); b.Caminho.Add(PonteFim); }
            else b.Caminho.Add(Salas[z].In + (Salas[z].In - Centro(z)).normalized * 0.6f);   // um passo para fora da porta
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
                if ((int)f.Em >= Salas.Length) { b.T.gameObject.SetActive(false); _nome[k].enabled = false; continue; }   // estacao sem sala no mundo (Fase C2): a View dela desenha
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
            r.Add(Centro(de));
            for (int z = de; z < para; z++) { r.Add(Salas[z].Out); r.Add(Centro(z + 1)); }
            for (int z = de; z > para; z--) { r.Add(Salas[z].In); r.Add(Centro(z - 1)); }
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

        // ------------------------------------------------------------------ gente do patio (tripulacao e tecnicos, so visual)

        void GentePatio()
        {
            NovoAtor("piloto", 2.5f, new Vector2(12.7f, 9.7f), new Vector2(13.3f, 12.0f));
            NovoAtor("comissaria", 3f, new Vector2(12.1f, 9.0f), new Vector2(12.8f, 10.9f));
            NovoAtor("comissario", 3.5f, new Vector2(12.9f, 8.8f), new Vector2(13.7f, 10.7f));
            NovoAtor("despachante", 3f, new Vector2(11.4f, 9.4f), new Vector2(11.9f, 9.4f));
            NovoAtor("bagageiro", 1.5f, new Vector2(14.3f, 15.9f), new Vector2(14.3f, 13.0f));
            NovoAtor("mecanico", 2f, new Vector2(19.7f, 12.3f), new Vector2(22.3f, 12.3f));
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
        }
    }
}
