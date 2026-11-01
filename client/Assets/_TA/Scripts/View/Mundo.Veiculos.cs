using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Veiculos do patio (GDD §5.12), so tela. A garagem fica a leste, com a boca para o sul (5 vaos entre os pilares da malha) e a rua na frente:
    /// cada veiculo tem a sua vaga e a sua faixa, e nenhum atravessa peca, aviao ou outro veiculo (medido com client/tools/diag_caixas.py).
    ///  - Duas ruas de mao unica na frente dos narizes: a oeste leva do patio de manobra aos avioes, a leste traz de volta. Ao norte delas sobra a
    ///    calcada (Calcada, para a gente do patio) e as faixas, com todo veiculo inteiro antes da asa: a do jato e baixa, nao ha como passar por baixo.
    ///  - O aviao encosta: os veiculos dos servicos que faltam saem de frente da vaga, seguem a rua oeste, passam da faixa e entram nela de re, com
    ///    o nariz para a rua, sem encostar no vizinho. O nucleo faz o servico (Aviao.Fazendo); o veiculo sai de frente quando acaba (Aviao.Falta),
    ///    cruza a rua oeste e volta pela rua leste ate entrar de re na vaga. Se o regional precisa dele e ele esta no jato, vai direto.
    ///  - Faixas: 3 do lado direito de cada aviao (bagagem, catering e combustivel no regional; bagagem, combustivel e catering no jato) e, do lado
    ///    esquerdo do jato, onibus e escada, que ficam ate ele sair. No estande G o desembarque e pela ponte.
    ///  - O +1 veiculo da Capacidade tem vaga pintada ao lado da garagem.
    /// </summary>
    public sealed partial class Mundo
    {
        const float RuaOeste = 10.1f, RuaLeste = 8.7f;
        /// <summary>Garagem: a boca da malha fica em +z, dai o yaw 180. A escala da a altura livre do catering sob a verga (2,89 m) e o y poe o piso
        /// da malha no nivel do chao.</summary>
        static readonly Vector3 GaragemEm = new Vector3(36.6f, -0.26f, 14.53f);
        const float GaragemYaw = 180f, GaragemEscala = 1.75f, VagaZ = 13.45f, Ajuste = 1.5f;
        /// <summary>x das vagas: o meio de cada vao da malha (a 1,75 de escala, de 1,9 a 2,5 m livres) e, depois, as 3 pintadas do +1 veiculo.</summary>
        static readonly float[] Vagas = { 30.74f, 33.58f, 36.56f, 39.62f, 42.68f, 46.4f, 48.1f, 49.8f };

        /// <summary>Cada tipo de veiculo: Servico, faixa e parada (pivo, com o nariz para a rua) em cada estande (-1 = nao vai) e vaga na garagem.</summary>
        sealed class Tipo
        {
            public string Malha; public int Servico, Vaga, Extra; public float Vel, Eixo, Comp; public Color Cor; public float[] Faixa, Parada;
        }

        static readonly Tipo[] Tipos =
        {
            new Tipo { Malha = "caminhao_catering", Servico = 1, Vaga = 1, Extra = 5, Vel = 2.6f, Eixo = 0.75f, Comp = 2.8f, Cor = Art.Hex(0xF2F4F7), Faixa = new[] { 15.7f, 27.2f }, Parada = new[] { 13.4f, 13.55f } },
            new Tipo { Malha = "caminhao_combustivel", Servico = 2, Vaga = 2, Extra = 6, Vel = 2.6f, Eixo = 0.75f, Comp = 2.6f, Cor = Art.Hex(0xE0533D), Faixa = new[] { 16.95f, 25.95f }, Parada = new[] { 13.4f, 13.4f } },
            new Tipo { Malha = "carrinho_bagagem", Servico = 3, Vaga = 0, Extra = 7, Vel = 2.6f, Eixo = 0.6f, Comp = 2.6f, Cor = Art.Hex(0x5DADE2), Faixa = new[] { 14.5f, 24.8f }, Parada = new[] { 13.2f, 13.25f } },
            new Tipo { Malha = "escada_embarque", Servico = 0, Vaga = 3, Vel = 2f, Eixo = 0.7f, Comp = 2.3f, Cor = Art.Hex(0xE5E8E8), Faixa = new[] { -1f, 20.8f }, Parada = new[] { 0f, 13.2f } },
            new Tipo { Malha = "onibus_patio", Servico = 0, Vaga = 4, Vel = 2f, Eixo = 0.9f, Comp = 3.2f, Cor = Art.Hex(0x3FA7D6), Faixa = new[] { -1f, 18.75f }, Parada = new[] { 0f, 13.8f } },
        };

        sealed class Veiculo
        {
            public Transform T, Malha;
            public SpriteRenderer Sombra;
            public Quaternion Base = Quaternion.identity;
            public Tipo Tipo;   // null = o rebocador
            public int Aviao = -1, Vaga;
            public float S, V, Vel, Eixo, Comp, Larg, Pitch, Roll, Parado;
            public Lida Lida;
            public readonly Queue<Trecho> Rotas = new Queue<Trecho>();
        }

        enum Lida { Parado, Indo, Servindo, Voltando }

        readonly List<Veiculo> _veiculos = new List<Veiculo>();
        /// <summary>Quem esta na faixa das ruas e da calcada neste quadro (Anda): os veiculos freiam para eles.</summary>
        readonly List<Vector2> _naFaixa = new List<Vector2>();
        Reboque _reboque;

        void Veiculos3D()
        {
            Peca("garagem_patio", GaragemEm, GaragemYaw, GaragemEscala);
            for (int i = 0; i <= 3; i++) Chao("VagaLinha", new Vector3(Vagas[5] - 0.85f + i * 1.7f, -0.04f, VagaZ + 0.3f), new Vector2(0.09f, 4f), Art.Hex(0xF2C14E));
            Chao("VagaFundo", new Vector3(Vagas[6], -0.04f, VagaZ + 2.3f), new Vector2(1.7f * 3f, 0.09f), Art.Hex(0xF2C14E));
            // vaga pintada do rebocador
            foreach (float x in new[] { ReboqueVaga.x - 0.5f, ReboqueVaga.x + 0.5f }) Chao("VagaReboque", new Vector3(x, -0.04f, ReboqueVaga.z - 0.3f), new Vector2(0.07f, 1.8f), Art.Hex(0xF2C14E));
            Veiculo r = NovoVeiculo("rebocador", null, 2.2f, 0.35f, Art.Hex(0xF2C14E), 1.5f);
            r.T.SetPositionAndRotation(ReboqueVaga, Quaternion.Euler(0f, 180f, 0f));
            Sombra(r);
            foreach (Tipo t in Tipos) NaVaga(t, t.Vaga);
        }

        /// <summary>Veiculo com o pivo no eixo traseiro (`eixo` = do eixo ao centro da malha, em m).</summary>
        Veiculo NovoVeiculo(string malha, Tipo tipo, float vel, float eixo, Color cor, float comp)
        {
            var v = new Veiculo { Tipo = tipo, Vel = vel, Eixo = eixo, Comp = comp };
            v.T = Peca(malha, Vector3.zero, 0f, 1f, eixo);
            if (v.T != null) { v.Malha = v.T.GetChild(0); v.Base = v.Malha.localRotation; }
            else
            {
                v.T = new GameObject(malha).transform;
                v.T.SetParent(_raiz, false);
                Bloco("Carroceria", PrimitiveType.Cube, new Vector3(0f, 0.3f, -0.1f), new Vector3(0.55f, 0.45f, comp - 0.4f), cor, v.T);
                Bloco("Cabine", PrimitiveType.Cube, new Vector3(0f, 0.38f, comp * 0.5f - 0.2f), new Vector3(0.55f, 0.55f, 0.35f), Art.Hex(0xECF0F1), v.T);
            }
            v.Larg = Medida(v.T).x;
            v.Sombra = SombraDe(v.T);
            _veiculos.Add(v);
            return v;
        }

        /// <summary>Veiculo do tipo `t` na vaga `vaga`, de nariz para a rua.</summary>
        Veiculo NaVaga(Tipo t, int vaga)
        {
            Veiculo v = NovoVeiculo(t.Malha, t, t.Vel, t.Eixo, t.Cor, t.Comp);
            v.Vaga = vaga;
            v.T.SetPositionAndRotation(V(Vagas[vaga], VagaZ), Quaternion.Euler(0f, 180f, 0f));
            Sombra(v);
            return v;
        }

        // ------------------------------------------------------------------ rebocador

        /// <summary>O aviao da ponte quer sair: o rebocador recua da vaga e encosta a barra no nariz.</summary>
        void ReboqueVai()
        {
            Veiculo r = _veiculos[0];
            Trecho t = Caminho(1f, 0.8f, ReboqueVaga, ReboqueNariz);
            t.A = 0.5f; t.F = 0.6f; t.Re = t.Para = true;
            r.Rotas.Enqueue(t);
            r.S = 0f;
            _reboque = Reboque.Indo;
        }

        /// <summary>Rebocador preso no nariz durante o pushback, empurrando de re.</summary>
        void Reboca(Aviao a, float dt)
        {
            Veiculo r = _veiculos[0];
            Vector3 nariz = a.T.forward;
            nariz.y = 0f;
            nariz.Normalize();
            Vector3 p = new Vector3(a.T.position.x, 0f, a.T.position.z) + nariz * (a.Nariz + (NarizZ - ReboqueNariz.z)), d = p - r.T.position;
            r.T.position = p;
            // a barra articula: o rebocador segue para onde o nariz anda, nao o eixo do aviao, e nao escorrega de lado na curva do recuo
            if (dt > 0f && d.magnitude / dt > 0.15f) r.T.rotation = Quaternion.RotateTowards(r.T.rotation, Quaternion.LookRotation(-d, Vector3.up), 90f * dt);
            Sombra(r);
        }

        /// <summary>Solto na taxiway: segue em frente ate sair do caminho do aviao, faz a volta atras do estande e desce a linha do estande ate a
        /// vaga: nenhum giro no lugar.</summary>
        void RebocadorVolta()
        {
            Veiculo r = _veiculos[0];
            const float z = 19.5f;
            Vector3 p = r.T.position;
            float x = p.x + 1.6f * Mathf.Sign(r.T.forward.x);
            Trecho vai = Caminho(1.4f, 2.2f, p, V(x, p.z), V(x, z), V(ReboqueVaga.x, z), ReboqueVaga);
            vai.A = 0.7f; vai.F = 0.8f; vai.Para = true;
            r.Rotas.Enqueue(vai);
            r.S = 0f;
            _reboque = Reboque.Voltando;
        }

        // ------------------------------------------------------------------ servicos

        /// <summary>Liga os veiculos ao nucleo: quem acabou sai; o aviao que precisa chama, um veiculo por vez saindo da garagem.</summary>
        void ServicosPasso(Sim s)
        {
            foreach (Veiculo v in _veiculos)
                if (v.Tipo != null && v.Lida == Lida.Servindo && !Precisa(v.Tipo, v.Aviao, s)) Libera(v, s);
            for (int k = 0; k < _avioes.Length; k++)
                foreach (Tipo t in Tipos)
                    if (Precisa(t, k, s)) Chama(t, k, s);
        }

        /// <summary>O aviao precisa do veiculo agora: parado no estande e, nos servicos, com o servico por fazer; escada e onibus ficam ate ele sair.</summary>
        bool Precisa(Tipo t, int k, Sim s)
        {
            Aviao a = _avioes[k];
            TA.Core.Aviao c = s.Avioes[k];
            if (t.Faixa[k] < 0f || a.Fase != Fase.NoPatio || a.QuerPartir || c.Hangar > 0) return false;
            return t.Servico == 0 || c.Falta[t.Servico] > 0;
        }

        void Chama(Tipo t, int k, Sim s)
        {
            Veiculo livre = null;
            int frota = 0;
            foreach (Veiculo v in _veiculos)
            {
                if (v.Tipo != t) continue;
                frota++;
                if (v.Aviao == k) return;
                if (v.Lida == Lida.Parado && livre == null) livre = v;
            }
            if (ManobraOcupada()) return;
            if (livre == null && t.Servico > 0 && frota < s.Veiculos((Servico)t.Servico)) livre = NaVaga(t, t.Extra);   // o +1 veiculo da Capacidade
            if (livre == null) return;
            Sai(livre, k);
        }

        /// <summary>Um por vez na frente da garagem: quem ainda esta nas ruas do lado dela, entrando ou saindo, segura a proxima saida.</summary>
        bool ManobraOcupada()
        {
            foreach (Veiculo v in _veiculos)
                if (v.Tipo != null && v.Lida != Lida.Parado && v.T.position.x > Vagas[0] - 3f && v.T.position.z < VagaZ + 0.1f) return true;
            return false;
        }

        /// <summary>Segue de frente pela rua oeste, passa da faixa e entra nela de re, com o nariz para a rua.</summary>
        void EntraDeRe(Veiculo v, int k, params Vector3[] p)
        {
            Tipo t = v.Tipo;
            float f = t.Faixa[k];
            var q = new List<Vector3>(p) { V(f - Ajuste, RuaOeste) };
            v.Rotas.Enqueue(DeFrente(v, q.ToArray()));
            Trecho re = Caminho(RaioVeic, 1.3f, V(f - Ajuste, RuaOeste), V(f, RuaOeste), V(f, t.Parada[k]));
            re.Re = re.Para = true; re.A = 0.7f; re.F = 0.9f;
            v.Rotas.Enqueue(re);
            v.Aviao = k; v.Lida = Lida.Indo;
        }

        /// <summary>Sai de frente da vaga, vira na rua oeste e vai ate a faixa.</summary>
        void Sai(Veiculo v, int k)
        {
            float x = Vagas[v.Vaga];
            EntraDeRe(v, k, V(x, VagaZ), V(x, RuaOeste));
            v.S = 0f;
        }

        /// <summary>Acabou: sai de frente da faixa, cruza a rua oeste e volta pela rua leste ate entrar de re na vaga. Do jato, se o regional precisa e
        /// ninguem foi, vira na rua oeste e vai direto para a faixa dele.</summary>
        void Libera(Veiculo v, Sim s)
        {
            Tipo t = v.Tipo;
            int k = v.Aviao, o = 1 - k;
            float f = t.Faixa[k], x = Vagas[v.Vaga];
            v.Aviao = -1;
            bool ocupado = false;
            foreach (Veiculo w in _veiculos) if (w.Tipo == t && w.Aviao == o) ocupado = true;
            if (t.Servico > 0 && o == 0 && !ocupado && Precisa(t, o, s)) EntraDeRe(v, o, V(f, t.Parada[k]), V(f, RuaOeste));
            else
            {
                v.Rotas.Enqueue(DeFrente(v, V(f, t.Parada[k]), V(f, RuaLeste), V(x + Ajuste, RuaLeste)));
                Trecho re = Caminho(RaioVeic, 1.3f, V(x + Ajuste, RuaLeste), V(x, RuaLeste), V(x, VagaZ));
                re.Re = re.Para = true; re.A = 0.7f; re.F = 0.9f;
                v.Rotas.Enqueue(re);
                v.Lida = Lida.Voltando;
            }
            v.S = 0f;
        }

        Trecho DeFrente(Veiculo v, params Vector3[] p)
        {
            Trecho t = Caminho(RaioVeic, v.Vel, p);
            t.A = 0.8f; t.F = 1f; t.Para = true;
            return t;
        }

        /// <summary>Quem vem atras espera (fila natural), e quem atravessa a rua tem a vez: conta a distancia de freada, para nao encostar. Depois de
        /// 6 s parado atras de outro veiculo segue ate se soltar: e a saida de um cruzamento travado, que nao deve acontecer (diag_caixas.py acusaria).</summary>
        bool FrenteLivre(Veiculo v, Trecho t, float dt)
        {
            Vector3 f = t.Re ? -v.T.forward : v.T.forward, c = Centro(v), lado = new Vector3(f.z, 0f, -f.x);
            float freada = v.V * v.V / (2f * t.F);
            foreach (Vector2 q in _naFaixa)
            {
                Vector3 d = V(q.x, q.y) - c;
                float frente = Vector3.Dot(d, f);
                if (frente > 0f && frente < v.Comp * 0.5f + 0.5f + freada && Mathf.Abs(Vector3.Dot(d, lado)) < v.Larg * 0.5f + 0.45f) return false;
            }
            float oeste = Cruza(v, t, RuaOeste), leste = Cruza(v, t, RuaLeste);
            foreach (Veiculo o in _veiculos)
            {
                if (o == v) continue;
                Vector3 d = Centro(o) - c;
                float frente = Vector3.Dot(d, f);
                if (NaRua(o, RuaOeste, oeste) || NaRua(o, RuaLeste, leste) || (frente > 0f && frente < (v.Comp + o.Comp) * 0.5f + 0.5f + freada && Mathf.Abs(Vector3.Dot(d, lado)) < 1.2f))
                {
                    v.Parado += dt;
                    return v.Parado >= 6f;
                }
            }
            v.Parado = 0f;
            return true;
        }

        /// <summary>`o` vem pela `rua`, perto do `x` em que outro quer cruzar (x negativo = nao cruza).</summary>
        static bool NaRua(Veiculo o, float rua, float x) =>
            x >= 0f && o.Lida != Lida.Parado && Mathf.Abs(o.T.position.z - rua) < 0.4f && Mathf.Abs(o.T.forward.x) > 0.7f && Mathf.Abs(o.T.position.x - x) < 9f;

        /// <summary>O x em que o trajeto entra na `rua` nos proximos metros, vindo de fora dela (-1 = nao entra): no cruzamento, quem cruza espera quem
        /// ja vem pela rua, antes de o nariz chegar nela.</summary>
        static float Cruza(Veiculo v, Trecho t, float rua)
        {
            if (v.Tipo == null || Mathf.Abs(v.T.position.z - rua) < 0.5f) return -1f;
            float rumo = (t.Re ? -1f : 1f) * v.T.forward.z, z = v.T.position.z;
            if ((rumo < -0.3f && z < rua) || (rumo > 0.3f && z > rua)) return -1f;   // acabou de cruzar: termina a curva antes de parar, com a cauda fora da rua
            for (int i = Indice(t, v.S); i < t.P.Count && t.L[i] <= v.S + 5.5f; i += 3)
                if (Mathf.Abs(t.P[i].z - rua) < 0.9f) return t.P[i].x;
            return -1f;
        }

        static Vector3 Centro(Veiculo v) => v.T.position + v.T.forward * v.Eixo;

        /// <summary>A pessoa espera: o proximo passo entra na caixa de um veiculo (com folga maior se ele esta andando) e chega mais perto dele.</summary>
        bool VeiculoNoPasso(Boneco b)
        {
            Vector2 prox = b.Pos + (b.Caminho[0] - b.Pos).normalized * 0.4f;
            foreach (Veiculo v in _veiculos)
            {
                Vector3 c = Centro(v), f = v.T.forward;
                Vector2 eixo = new Vector2(f.x, f.z), lado = new Vector2(f.z, -f.x), a = prox - new Vector2(c.x, c.z), n = b.Pos - new Vector2(c.x, c.z);
                float folga = v.V > 0.1f ? 0.45f : 0.25f, ax = Mathf.Abs(Vector2.Dot(a, eixo)), ay = Mathf.Abs(Vector2.Dot(a, lado));
                if (ax < v.Comp * 0.5f + folga && ay < v.Larg * 0.5f + folga && (ax < Mathf.Abs(Vector2.Dot(n, eixo)) || ay < Mathf.Abs(Vector2.Dot(n, lado)))) return true;
            }
            return false;
        }

        void VeiculoPasso(Veiculo v, float dt)
        {
            float vAnt = v.V, yawAnt = v.T.eulerAngles.y;
            bool reboque = v.Tipo == null;
            if (reboque && _reboque == Reboque.Preso) return;
            if (v.Rotas.Count == 0)
            {
                if (reboque)
                {
                    if (_reboque == Reboque.Indo) _reboque = Reboque.Pronto;
                    else if (_reboque == Reboque.Voltando) _reboque = Reboque.Vago;
                }
                else if (v.Lida == Lida.Indo) v.Lida = Lida.Servindo;
                else if (v.Lida == Lida.Voltando) v.Lida = Lida.Parado;
                v.V = 0f;
            }
            else
            {
                Trecho t = v.Rotas.Peek();
                v.V = Passo(v.V, t, v.S, FrenteLivre(v, t, dt) ? t.V : 0f, t.Para ? t.Total - v.S : 1e4f, LatVeic, dt);
                v.S += v.V * Mathf.Max(0f, dt);
                Pose(v.T, t, v.S);
                if (v.S >= t.Total - 0.01f) { v.Rotas.Dequeue(); v.S = 0f; v.V = 0f; }
            }
            if (dt > 0f) Inclina(v.Malha, v.Base, (v.V - vAnt) / dt, v.V * Mathf.DeltaAngle(yawAnt, v.T.eulerAngles.y) * Mathf.Deg2Rad / dt, dt, ref v.Pitch, ref v.Roll, 1f);
            Sombra(v);
        }

        static void Sombra(Veiculo v)
        {
            v.Sombra.transform.position = new Vector3(v.T.position.x, 0.015f, v.T.position.z);
            v.Sombra.transform.rotation = Quaternion.Euler(90f, v.T.eulerAngles.y, 0f);
        }
    }
}
