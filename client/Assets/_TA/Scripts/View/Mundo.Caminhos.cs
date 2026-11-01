using System.Collections.Generic;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Caminho das pessoas dentro das salas, só apresentação: ninguém atravessa parede, móvel ou equipamento (client/tools/diag_caixas.py mede).
    /// A carga lê a caixa de cada peça da cena (LePecas) e a rota confere só as ligadas naquela hora: sala fechada, posto fechado, nível do balcão,
    /// fita e cordão que aparecem com a fila. Não há tabela de sala aqui, então vale para qualquer mobília. Só roda quando a rota muda, nunca no quadro.
    ///  - Contorna é o Eixos com olhos: a perna em L, se estiver livre; senão, o menor caminho numa grade da sala, com o mínimo de curvas, porque a
    ///    folha tem 4 rumos, que são os eixos do mundo.
    ///  - Sem barreiras (testes, sem cena), Contorna é igual ao Eixos.
    /// </summary>
    public sealed partial class Mundo
    {
        /// <summary>Caixa de uma peça no chão: centro, eixo da frente e meias medidas (L na frente, W no lado).</summary>
        sealed class Barreira { public GameObject Go; public Vector2 C, U; public float L, W; }

        const int GradeN = 100;
        const float Celula = Lado / GradeN;
        /// <summary>Distância que o corpo guarda das peças: meia largura do adulto (0,1 m) menos a tolerância do diag_caixas.py e um pouco de margem. Passagem estreita tenta de novo com menos.</summary>
        static readonly float[] Folgas = { 0.09f, 0.06f, 0.03f };
        /// <summary>A gente passa por dentro ou a peça anda com ela. O nome do diag_caixas.py é SEM_CORPO.</summary>
        static readonly HashSet<string> SemCorpo = new HashSet<string> { "portico_detector", "cabine_scanner_corpo", "bandeja_seguranca", "Bandeja", "scanner_mao", "mala_rodinha", "Mala", "MalaSeg", "Pilha" };
        static readonly int[] Vizinhos = { 1, -1, GradeN, -GradeN };
        static readonly List<Barreira>[] _barreiras = new List<Barreira>[8];
        static readonly int[] _grade = new int[GradeN * GradeN], _busca = new int[GradeN * GradeN];   // _grade: -1 = peça, 0 = livre, n = n passos desde a saída
        static readonly List<Vector2> _curvas = new List<Vector2>();

        /// <summary>Na carga: a caixa de cada peça da sala com altura de corpo. Lê a malha, como o DiagCaixas, e só os filhos da sala: veículo e avião
        /// nascem na origem, dentro do check-in, antes de irem para o pátio.</summary>
        void LePecas()
        {
            var vistas = new HashSet<Transform>();
            for (int e = 0; e < _barreiras.Length; e++)
            {
                _barreiras[e] = new List<Barreira>();
                foreach (MeshFilter f in _sala[e].GetComponentsInChildren<MeshFilter>(true))
                {
                    Transform t = f.name == "malha" ? f.transform.parent : f.transform;
                    if (!vistas.Add(t) || SemCorpo.Contains(t.name)) continue;
                    Bounds b = Caixa(t);
                    if (b.max.y + t.position.y < 0.1f || b.min.y + t.position.y > Gente) continue;
                    Quaternion q = Quaternion.Euler(0f, t.eulerAngles.y, 0f);
                    Vector3 c = t.position + q * b.center, u = q * Vector3.forward;
                    var p = new Barreira { Go = t.gameObject, C = new Vector2(c.x, c.z), U = new Vector2(u.x, u.z), L = b.extents.z, W = b.extents.x };
                    if (t.name == "cordao_fila") p.L = 0.01f;   // a caixa é a da diagonal da malha, mais larga que o corredor da fila; a corda é um fio
                    _barreiras[e].Add(p);
                }
            }
        }

        /// <summary>Testes: troca as peças lidas da cena por esta (go = null: sem peças, e Contorna volta a ser Eixos).</summary>
        public static void PecaDeTeste(int e, GameObject go, Vector2 c, float l, float w)
        {
            for (int i = 0; i < _barreiras.Length; i++) _barreiras[i] = null;
            if (go != null) _barreiras[e] = new List<Barreira> { new Barreira { Go = go, C = c, U = Vector2.up, L = l, W = w } };
        }

        /// <summary>A reta de `a` a `b` passa a menos de `f` da peça? (recorte da reta nos 2 eixos da caixa)</summary>
        static bool Bate(Barreira c, Vector2 a, Vector2 b, float f)
        {
            Vector2 v = new Vector2(c.U.y, -c.U.x), d = b - a, o = a - c.C;
            float t0 = 0f, t1 = 1f;
            return Recorta(Vector2.Dot(o, c.U), Vector2.Dot(d, c.U), c.L + f, ref t0, ref t1) && Recorta(Vector2.Dot(o, v), Vector2.Dot(d, v), c.W + f, ref t0, ref t1);
        }

        static bool Recorta(float o, float d, float m, ref float t0, ref float t1)
        {
            if (Mathf.Abs(d) < 1e-6f) return Mathf.Abs(o) < m;
            float a = (-m - o) / d, b = (m - o) / d;
            t0 = Mathf.Max(t0, Mathf.Min(a, b));
            t1 = Mathf.Min(t1, Mathf.Max(a, b));
            return t0 < t1;
        }

        static bool Livre(int e, Vector2 a, Vector2 b, float f)
        {
            foreach (Barreira c in _barreiras[e])
                if (c.Go.activeInHierarchy && Bate(c, a, b, f)) return false;
            return true;
        }

        static bool EmL(int e, Vector2 p, Vector2 q, bool xPorUltimo)
        {
            Vector2 c = xPorUltimo ? new Vector2(p.x, q.y) : new Vector2(q.x, p.y);
            return Livre(e, p, c, Folgas[0]) && Livre(e, c, q, Folgas[0]);
        }

        /// <summary>De p até q na mesma sala, sem encostar em peça: Eixos, se der, na ordem pedida ou na outra; senão, a grade.</summary>
        public static void Contorna(List<Vector2> r, ref Vector2 p, Vector2 q, bool xPorUltimo)
        {
            int e = SalaEm(p);
            bool grade = e >= 0 && e == SalaEm(q) && _barreiras[e] != null;
            if (!grade || EmL(e, p, q, xPorUltimo)) { Eixos(r, ref p, q, xPorUltimo); return; }
            if (EmL(e, p, q, !xPorUltimo)) { Eixos(r, ref p, q, !xPorUltimo); return; }
            for (int i = 0; i < Folgas.Length; i++)
                if (Busca(r, e, ref p, q, Folgas[i], i == Folgas.Length - 1)) return;
            Eixos(r, ref p, q, xPorUltimo);
        }

        /// <summary>`q`, ou o ponto mais perto dele ao longo de `lado` que não encosta em peça: a fileira de cadeiras chega perto da linha da porta.</summary>
        static Vector2 Solta(Vector2 q, Vector2 lado)
        {
            int e = SalaEm(q);
            if (e < 0 || _barreiras[e] == null) return q;
            for (int i = 0; i < 3; i++)
                for (int s = 1; s >= -1; s -= 2)
                {
                    Vector2 c = q + lado * (s * i * Celula);
                    if (Livre(e, c, c, Folgas[0])) return c;
                }
            return q;
        }

        /// <summary>Busca em largura na grade da sala `e`. Se a saída ou a chegada estiverem coladas numa peça, usa a célula livre mais perto. Falso = não
        /// chegou perto de q com esta folga. A grade é refeita a cada busca: as peças ligadas mudam com a fila, e a busca é rara.</summary>
        static bool Busca(List<Vector2> r, int e, ref Vector2 p, Vector2 q, float f, bool ultima)
        {
            Vector2 o = Salas[e].Min;
            for (int i = 0; i < _grade.Length; i++)
            {
                int x = i % GradeN, z = i / GradeN;
                _grade[i] = x == 0 || z == 0 || x == GradeN - 1 || z == GradeN - 1 ? -1 : 0;
            }
            foreach (Barreira c in _barreiras[e])
            {
                if (!c.Go.activeInHierarchy) continue;
                float raio = Mathf.Sqrt(c.L * c.L + c.W * c.W) + f;
                int x0 = Mathf.Max(0, Mathf.FloorToInt((c.C.x - raio - o.x) / Celula)), x1 = Mathf.Min(GradeN - 1, Mathf.CeilToInt((c.C.x + raio - o.x) / Celula));
                int z0 = Mathf.Max(0, Mathf.FloorToInt((c.C.y - raio - o.y) / Celula)), z1 = Mathf.Min(GradeN - 1, Mathf.CeilToInt((c.C.y + raio - o.y) / Celula));
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        Vector2 m = Ponto(e, x + z * GradeN);
                        if (Bate(c, m, m, f)) _grade[x + z * GradeN] = -1;
                    }
            }
            int ini = MaisPerto(e, p, false);
            if (ini < 0) return false;
            _grade[ini] = 1;
            _busca[0] = ini;
            for (int h = 0, t = 1; h < t; h++)
            {
                int c = _busca[h];
                foreach (int v in Vizinhos)
                    if (_grade[c + v] == 0) { _grade[c + v] = _grade[c] + 1; _busca[t++] = c + v; }
            }
            int fim = MaisPerto(e, q, true);
            Vector2 chegou = Ponto(e, fim);
            bool perto = Vector2.Distance(chegou, q) < Celula * 1.5f;
            if (!perto && !ultima) return false;

            // de trás para a frente, seguindo reto enquanto der: entre os caminhos mais curtos, o de menos curvas
            _curvas.Clear();
            int rumo = 0, primeiro = 0, passo = fim;
            while (_grade[passo] > 1)
            {
                int d = rumo != 0 && _grade[passo + rumo] == _grade[passo] - 1 ? rumo : 0;
                if (d == 0)
                {
                    foreach (int v in Vizinhos)
                        if (_grade[passo + v] == _grade[passo] - 1) { d = v; break; }
                    if (rumo == 0) primeiro = d;
                    else _curvas.Add(Ponto(e, passo));
                    rumo = d;
                }
                passo += d;
            }
            int n = _curvas.Count - 1;
            if (perto && n >= 0 && Livre(e, Reto(_curvas[0], q, primeiro), q, f)) _curvas[0] = Reto(_curvas[0], q, primeiro);   // as pontas retas, na linha de p e de q, não na da célula
            Vector2 saida = Ponto(e, ini);
            if (Vector2.Distance(saida, p) >= Celula) r.Add(saida);
            else if (n >= 0 && Livre(e, p, Reto(_curvas[n], p, rumo), f)) _curvas[n] = Reto(_curvas[n], p, rumo);
            for (int i = _curvas.Count - 1; i >= 0; i--) r.Add(_curvas[i]);
            p = perto ? q : chegou;
            r.Add(p);
            return true;
        }

        /// <summary>`c` na linha de `p` ao longo do rumo da grade.</summary>
        static Vector2 Reto(Vector2 c, Vector2 p, int rumo) => Mathf.Abs(rumo) == 1 ? new Vector2(c.x, p.y) : new Vector2(p.x, c.y);

        static Vector2 Ponto(int e, int i) => Salas[e].Min + new Vector2(i % GradeN + 0.5f, i / GradeN + 0.5f) * Celula;

        /// <summary>A célula mais perto de `p`: das alcançadas (`achadas`) ou das livres. Varre a grade inteira, o que a busca em largura já fez.</summary>
        static int MaisPerto(int e, Vector2 p, bool achadas)
        {
            int melhor = -1;
            float d = float.MaxValue;
            for (int i = 0; i < _grade.Length; i++)
            {
                if (achadas ? _grade[i] <= 0 : _grade[i] != 0) continue;
                float l = (Ponto(e, i) - p).sqrMagnitude;
                if (l < d) { d = l; melhor = i; }
            }
            return melhor;
        }

        /// <summary>O alvo de quem já está no lugar mudou (a fila andou, o posto trocou de etapa): troca só a última perna, sem cortar a fita da fileira nem a
        /// máquina. A tela anda atrás do núcleo: se a pessoa ainda está em outra sala, vai pelas portas.</summary>
        void Segue(Boneco b, Vector2 alvo, bool xPorUltimo)
        {
            int n = b.Caminho.Count;
            if (alvo == b.Alvo || (n == 0 && (alvo - b.Pos).sqrMagnitude <= 4e-4f)) return;
            b.Alvo = alvo;
            Vector2 p = n > 1 ? b.Caminho[n - 2] : b.Pos;
            if (n > 0) b.Caminho.RemoveAt(n - 1);
            int e = SalaEm(alvo);
            if (e >= 0) AteASala(b.Caminho, ref p, e, e);
            Contorna(b.Caminho, ref p, alvo, xPorUltimo);
        }
    }
}
