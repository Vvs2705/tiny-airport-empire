using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Banheiro e limpeza (frente F), so apresentacao: tudo segue sim.Banheiro, sim.Sujeira e sim.Limpeza (Sim.Limpeza.cs); nada aqui muda regra.
    ///  - Planta: sala de 4 x 4 no canto livre a leste do vestiario, com a porta no corredor x = 16. Quem vem do portao sai pela porta leste dele,
    ///    desce o corredor x = 12 ate a quina, segue o corredor z = 4 e desce ate a porta: nao fecha porta nem fila de outra sala. Dentro, as
    ///    cabines na parede leste, a bancada das pias na parede sul, o deposito com o carrinho no canto sudoeste e a fila ao longo da parede norte.
    ///  - Passageiro: Indo anda ate o lugar da fila no ritmo do nucleo; na fila, em pe; na cabine (uma fixa por pessoa enquanto usa); Voltando passa
    ///    pela pia e anda ate a porta leste do portao, de onde a fila do portao segue (Mundo.Espera).
    ///  - Sujeira: ManchasPorArea no piso de cada area, uma a cada SujeiraMax / ManchasPorArea; somem uma a uma enquanto o zelador limpa.
    ///  - Zelador: com o carrinho (o 2o, com a lavadora) empurrado a frente. Indo anda o caminho da planta e chega quando o nucleo diz (Andando);
    ///    Limpando fica ao lado da ultima mancha, com o cone de piso molhado, e passa para a seguinte quando ela some; Livre para onde terminou.
    ///  - Interditado: o letreiro diz "em limpeza" e o cone fica na porta.
    ///  - Malhas do Tripo: cabine_banheiro, pia_banheiro, carrinho_limpeza, maquina_piso e cone_piso_molhado (client/tools/malhas_ta.py); sem elas, as
    ///    primitivas daqui.
    /// </summary>
    public sealed partial class Mundo
    {
        static readonly Vector2 BanMin = new Vector2(16f, 0f), BanEntra = new Vector2(16.45f, 2f), Deposito = new Vector2(16.75f, 0.75f), Pia = new Vector2(18.3f, 0.95f);
        /// <summary>Do portao ate o banheiro pelos corredores: dentro do portao junto a porta leste, a porta (12, 6), a quina (12, 4), o corredor z = 4
        /// ate x = 16, a porta do banheiro e a entrada.</summary>
        static readonly Vector2[] AoBanheiro = { new Vector2(11.55f, 6f), new Vector2(12f, 6f), new Vector2(12f, 4f), new Vector2(16f, 4f), new Vector2(16f, 2f), BanEntra };
        /// <summary>Boneco.Zona de quem esta no banheiro: indo, na fila, na cabine, voltando.</summary>
        const int ZonaBanheiro = 40;
        const int ManchasPorArea = 5, CabinesMax = 4;
        static readonly Color CorLimpeza = Art.Hex(0x3FA796), CorMancha = new Color(0.45f, 0.36f, 0.24f), CorAviso = Art.Hex(0xF2C230), CorLouca = Art.Hex(0xF4F6F8), CorDivisoria = Art.Hex(0x5FB3B3);

        readonly int[] _cabineDe = { -1, -1, -1, -1 };   // id de quem usa cada cabine (a tela fixa a cabine de cada um)
        readonly Renderer[,] _mancha = new Renderer[Sim.Areas, ManchasPorArea];
        readonly List<Boneco> _zel = new List<Boneco>();
        readonly List<Transform> _carro = new List<Transform>(), _cone = new List<Transform>();
        readonly List<int> _zelRota = new List<int>();   // area * 3 + fase da rota feita (-1 = nenhuma)
        Transform _coneDaPorta;
        TextMesh _placaBanheiro;

        public static bool DentroBanheiro(Vector2 p) => p.x > BanMin.x + 0.1f && p.x < BanMin.x + Lado && p.y > BanMin.y && p.y < BanMin.y + Lado;

        /// <summary>Lugar k da fila do banheiro: a cabeca junto das cabines, a fila anda para a porta ao longo da parede norte (10 por fileira).</summary>
        public static Vector2 FilaBanheiro(int k) => new Vector2(18.9f - 0.26f * (k % 10), 3.3f - 0.3f * Mathf.Min(k / 10, 2));
        /// <summary>Dentro da cabine i (parede leste) e a frente dela.</summary>
        public static Vector2 Cabine(int i) => new Vector2(19.3f, 2.75f - 0.45f * i);
        static Vector2 FrenteCabine(int i) => new Vector2(18.7f, 2.75f - 0.45f * i);

        /// <summary>Lugar da mancha k da area a: no meio do piso das salas, longe dos balcoes (Em mede da parede de saida); no banheiro, entre a porta e as cabines.</summary>
        public static Vector2 Mancha(int a, int k)
        {
            if (a == (int)Area.Banheiro) return new[] { new Vector2(17.3f, 1.7f), new Vector2(18.1f, 2.3f), new Vector2(17.6f, 2.8f), new Vector2(18.4f, 1.6f), new Vector2(17.1f, 2.4f) }[k];
            Vector2 da = new[] { new Vector2(1.3f, -0.9f), new Vector2(2.3f, 0.7f), new Vector2(1.8f, -0.1f), new Vector2(2.9f, -1.2f), new Vector2(2.6f, 1.3f) }[k];
            return Em(a, da.x, da.y);
        }

        /// <summary>Trecho do corredor do banheiro em que p esta (entre AoBanheiro[s] e AoBanheiro[s + 1]); -1 = fora.</summary>
        public static int TrechoBanheiro(Vector2 p)
        {
            for (int s = 1; s < AoBanheiro.Length - 1; s++)
            {
                Vector2 min = Vector2.Min(AoBanheiro[s], AoBanheiro[s + 1]) - Vector2.one * 0.12f, max = Vector2.Max(AoBanheiro[s], AoBanheiro[s + 1]) + Vector2.one * 0.12f;
                if (p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y) return s;
            }
            return -1;
        }

        /// <summary>De p (numa sala, no corredor ou ja no banheiro) ate `alvo` dentro do banheiro. `sala` = a sala do nucleo, se p esta fora das salas.</summary>
        void RotaAoBanheiro(List<Vector2> r, Vector2 p, int sala, Vector2 alvo)
        {
            if (!DentroBanheiro(p))
            {
                int s = TrechoBanheiro(p);
                if (s < 0)
                {
                    int aqui = SalaEm(p);
                    AteASala(r, ref p, aqui >= 0 ? aqui : Mathf.Clamp(sala, 0, Salas.Length - 1), Portao);
                    RotaSala(r, ref p, AoBanheiro[0]);
                    s = 0;
                }
                for (int i = s + 1; i < AoBanheiro.Length; i++) r.Add(p = AoBanheiro[i]);
            }
            Eixos(r, ref p, alvo, false);   // na largura da sala e depois ao longo da fila: ninguem corta a fila
        }

        /// <summary>Do banheiro (ou do corredor) ate a porta leste do portao: da cabine passa pela pia.</summary>
        public static void RotaDoBanheiro(List<Vector2> r, Vector2 p, bool pia)
        {
            int s = TrechoBanheiro(p);
            if (DentroBanheiro(p) || s < 0)
            {
                if (pia) { Eixos(r, ref p, new Vector2(FrenteCabine(0).x, p.y), false); Eixos(r, ref p, Pia, false); }
                Eixos(r, ref p, BanEntra, true);
                s = AoBanheiro.Length - 1;
            }
            for (int i = s; i >= 0; i--) r.Add(AoBanheiro[i]);
        }

        /// <summary>Velocidade para chegar quando o nucleo chega (passos que faltam), no teto em que a folha acompanha.</summary>
        static float NoRitmo(Boneco b, int passos) => passos > 0 ? Mathf.Clamp(Resta(b) / (passos * Sim.Step), 0.6f, VelMax) : VelPax;

        static bool Tem(List<Pax> l, int id)
        {
            foreach (Pax p in l) if (p.Id == id) return true;
            return false;
        }

        // ------------------------------------------------------------------ passageiros (SincronizaPax, antes das salas)

        void BanheiroPax(Sim s)
        {
            Banheiro bh = s.Banheiro;
            // quem saiu do banheiro no nucleo (voltou ao portao, ou o voo fechou) com o boneco ainda la: a fila do portao segue da porta leste
            foreach (KeyValuePair<int, Boneco> kv in _pax)
            {
                Boneco b = kv.Value;
                if (b.Zona < ZonaBanheiro || Tem(bh.Indo, kv.Key) || Tem(bh.Fila, kv.Key) || Tem(bh.Usando, kv.Key) || Tem(bh.Voltando, kv.Key)) continue;
                if (SalaEm(b.Pos) != Portao) { b.Pos = AoBanheiro[0]; b.Caminho.Clear(); }   // ponytail: o atraso raro do boneco sobre o nucleo vira um passo curto ate a porta
                b.Zona = Portao; b.Vaga = -1; b.Pressa = 0f;
            }
            for (int i = 0; i < _cabineDe.Length; i++) if (_cabineDe[i] >= 0 && !Tem(bh.Usando, _cabineDe[i])) _cabineDe[i] = -1;
            for (int i = 0; i < bh.Indo.Count; i++) NoBanheiro(s, bh.Indo[i], 0, bh.Fila.Count + i);
            for (int i = 0; i < bh.Fila.Count; i++) NoBanheiro(s, bh.Fila[i], 1, i);
            foreach (Pax p in bh.Usando) NoBanheiro(s, p, 2, CabineDe(p.Id));
            foreach (Pax p in bh.Voltando) NoBanheiro(s, p, 3, 0);
        }

        int CabineDe(int id)
        {
            int livre = -1;
            for (int i = 0; i < _cabineDe.Length; i++)
            {
                if (_cabineDe[i] == id) return i;
                if (_cabineDe[i] < 0 && livre < 0) livre = i;
            }
            if (livre >= 0) _cabineDe[livre] = id;
            return Mathf.Max(0, livre);
        }

        void NoBanheiro(Sim s, Pax p, int fase, int k)
        {
            _vistos.Add(p.Id);
            if (!_pax.TryGetValue(p.Id, out Boneco b)) b = PaxNovo(p);
            int zona = ZonaBanheiro + fase;
            Vector2 alvo = fase == 2 ? Cabine(k) : fase == 3 ? AoBanheiro[0] : FilaBanheiro(k);
            if (b.Zona != zona)
            {
                b.Caminho.Clear();
                b.Vaga = b.Cadeira = -1; b.Senta = 0f; b.NoBalcao = false;
                if (fase == 3 && (DentroBanheiro(b.Pos) || TrechoBanheiro(b.Pos) >= 0)) RotaDoBanheiro(b.Caminho, b.Pos, b.Zona == ZonaBanheiro + 2);
                else if (fase == 3)
                {
                    // o boneco nem chegou ao banheiro (atras do nucleo): segue direto para a porta leste do portao
                    Vector2 q = b.Pos;
                    AteASala(b.Caminho, ref q, SalaEm(q) >= 0 ? SalaEm(q) : Portao, Portao);
                    RotaSala(b.Caminho, ref q, AoBanheiro[0]);
                }
                else if (fase == 2) { Vector2 q = b.Pos; if (!DentroBanheiro(q)) RotaAoBanheiro(b.Caminho, q, b.Zona, FrenteCabine(k)); else Eixos(b.Caminho, ref q, FrenteCabine(k), false); b.Caminho.Add(alvo); }
                else RotaAoBanheiro(b.Caminho, b.Pos, b.Zona, alvo);
                b.Zona = zona; b.Alvo = alvo;
            }
            else if (fase <= 1 && b.Alvo != alvo)
            {
                b.Alvo = alvo;
                if (b.Caminho.Count > 0) b.Caminho[b.Caminho.Count - 1] = alvo;   // a fila andou: so o fim do caminho muda
                else b.Caminho.Add(alvo);
            }
            b.Pressa = fase == 0 || fase == 3 ? NoRitmo(b, p.Resto) : 0f;
            if (b.Caminho.Count == 0) Olha(b, fase == 2 ? Vector2.left : Vector2.right);   // na cabine, de frente para a porta; na fila, para as cabines
            b.Humor.enabled = false;
            b.Tinta = Art.VooCor[p.Voo];
        }

        // ------------------------------------------------------------------ sujeira e equipe (Atualiza, depois dos funcionarios)

        void LimpezaPasso(Sim s, float dt)
        {
            for (int a = 0; a < Sim.Areas; a++)
            {
                int n = Mathf.CeilToInt(s.Sujeira[a] * (float)ManchasPorArea / Sim.SujeiraMax);
                for (int k = 0; k < ManchasPorArea; k++) if (_mancha[a, k] != null) _mancha[a, k].enabled = k < n;
            }
            if (_placaBanheiro != null) _placaBanheiro.text = s.Banheiro.Interditado ? Textos.BanheiroFechado : Textos.Banheiro;
            if (_coneDaPorta != null) _coneDaPorta.gameObject.SetActive(s.Banheiro.Interditado);
            while (_zel.Count < s.Limpeza.Count) NovoZelador(_zel.Count);
            for (int i = 0; i < _zel.Count; i++)
            {
                Boneco b = _zel[i];
                bool existe = i < s.Limpeza.Count;
                b.T.gameObject.SetActive(existe);
                _carro[i].gameObject.SetActive(existe);
                if (!existe) { _cone[i].gameObject.SetActive(false); continue; }
                Zelador z = s.Limpeza[i];
                b.Atende = false;
                int a = (int)z.Em, rota = a * 3 + (int)z.Fase;
                if (z.Fase != Faxina.Livre)
                {
                    int visiveis = Mathf.CeilToInt(s.Sujeira[a] * (float)ManchasPorArea / Sim.SujeiraMax);
                    Vector2 mancha = Mancha(a, Mathf.Clamp(visiveis - 1, 0, ManchasPorArea - 1)), alvo = mancha + new Vector2(0.22f, 0f);
                    if (_zelRota[i] != rota && z.Fase == Faxina.Indo || _zelRota[i] < 0)
                    {
                        b.Caminho.Clear();
                        if (z.Em == Area.Banheiro) RotaAoBanheiro(b.Caminho, b.Pos, (int)z.De, alvo);
                        else
                        {
                            Vector2 p = b.Pos;
                            if (DentroBanheiro(p) || TrechoBanheiro(p) >= 0) { RotaDoBanheiro(b.Caminho, p, false); p = AoBanheiro[0]; }
                            RotaPessoa(b.Caminho, p, SalaEm(p) >= 0 ? SalaEm(p) : Portao, a, alvo, false);
                        }
                        b.Alvo = alvo;
                    }
                    else if (z.Fase == Faxina.Limpando && b.Alvo != alvo)
                    {
                        b.Alvo = alvo;   // a mancha sumiu: um passo ate a seguinte
                        if (z.Em == Area.Banheiro || b.Caminho.Count > 0) b.Caminho.Add(alvo); else Segue(b, alvo, false);
                    }
                    Anda(b, z.Fase == Faxina.Indo ? NoRitmo(b, z.Andando) : VelFunc * 0.6f, dt);
                    bool esfrega = z.Fase == Faxina.Limpando && b.Caminho.Count == 0;
                    b.Atende = esfrega;
                    if (esfrega) Olha(b, mancha - b.Pos);
                    _cone[i].gameObject.SetActive(z.Fase == Faxina.Limpando);
                    if (z.Fase == Faxina.Limpando) _cone[i].position = V3(mancha + new Vector2(-0.18f, 0.16f));
                }
                else { Anda(b, VelFunc, dt); _cone[i].gameObject.SetActive(false); }
                _zelRota[i] = rota;
                Empurra(i, b);
                Pinta(b);
            }
        }

        /// <summary>O carrinho anda a frente de quem empurra; parado, fica ao lado de quem limpa.</summary>
        void Empurra(int i, Boneco b)
        {
            Transform c = _carro[i];
            Vector2 d = b.Caminho.Count > 0 ? b.Caminho[0] - b.Pos : Vector2.zero;
            if (d.sqrMagnitude > 1e-4f)
            {
                d.Normalize();
                c.position = V3(b.Pos + d * 0.3f);
                c.rotation = Quaternion.Euler(0f, Rumo(d), 0f);
            }
            else if (!b.Andou && (V3(b.Pos) - c.position).sqrMagnitude > 0.5f) c.position = V3(b.Pos + new Vector2(0f, -0.3f));   // jogo reaberto: o carrinho vai com ele
        }

        void NovoZelador(int i)
        {
            Boneco b = NovoBoneco("bagageiro");
            b.Tinta = CorLimpeza;
            b.Pos = Deposito + new Vector2(0.35f * i, 0.4f);
            _zel.Add(b);
            _zelRota.Add(-1);
            bool lavadora = i % 2 == 1;
            Transform c = Peca(lavadora ? "maquina_piso" : "carrinho_limpeza", V3(b.Pos + new Vector2(0f, -0.3f)), 0f);
            if (c == null)
            {
                c = new GameObject(lavadora ? "Lavadora" : "Carrinho").transform;
                c.SetParent(_raiz, false);
                c.localPosition = V3(b.Pos + new Vector2(0f, -0.3f));
                if (lavadora)
                {
                    Bloco("Tanque", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), new Vector3(0.22f, 0.12f, 0.26f), Art.Hex(0x7E8A97), c);
                    Bloco("Cabo", PrimitiveType.Cube, new Vector3(0f, 0.2f, -0.14f), new Vector3(0.16f, 0.03f, 0.03f), Art.Hex(0x3A4254), c);
                }
                else
                {
                    Bloco("Base", PrimitiveType.Cube, new Vector3(0f, 0.09f, 0f), new Vector3(0.2f, 0.14f, 0.3f), Art.Hex(0x2E7D9A), c);
                    Bloco("Balde", PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0.06f), new Vector3(0.11f, 0.05f, 0.11f), CorAviso, c);
                    Bloco("Saco", PrimitiveType.Cube, new Vector3(0f, 0.2f, -0.08f), new Vector3(0.12f, 0.12f, 0.08f), Art.Hex(0x3A4254), c);
                    Bloco("Esfregao", PrimitiveType.Cylinder, new Vector3(0.07f, 0.26f, 0.06f), new Vector3(0.015f, 0.13f, 0.015f), Art.Hex(0xC9A96E), c);
                }
            }
            _carro.Add(c);
            _cone.Add(Cone(V3(b.Pos)));
        }

        Transform Cone(Vector3 pos)
        {
            Transform t = Peca("cone_piso_molhado", pos, 30f);
            if (t != null) { t.gameObject.SetActive(false); return t; }
            t = new GameObject("PisoMolhado").transform;
            t.SetParent(_raiz, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0f, 30f, 0f);
            Bloco("Placa", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f), new Vector3(0.09f, 0.12f, 0.02f), CorAviso, t).transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            t.gameObject.SetActive(false);
            return t;
        }

        // ------------------------------------------------------------------ planta (Planta, antes do Funde)

        void Banheiro3D()
        {
            Vector2 c = BanMin + Vector2.one * (Lado * 0.5f), m = BanMin + Vector2.one * (Corredor * 0.5f);
            Renderer piso = Bloco("PisoBanheiro", PrimitiveType.Cube, new Vector3(c.x, -0.05f, c.y), new Vector3(Interno, 0.1f, Interno), PisoPublico);
            if (Material != null) { piso.sharedMaterial = MatPiso(); Cor(piso, PisoPublico); }
            float porta = BanEntra.y - m.y;
            const int Rodape = (int)Estacao.Bagagem;   // rodape turquesa, como a placa
            Muro(Rodape, m, Vector2.right, 0f, Interno, AltoFrente);                                     // sul
            Muro(Rodape, m + Vector2.up * Interno, Vector2.right, 0f, Interno, AltoFundo);               // norte
            Muro(Rodape, m, Vector2.up, 0f, porta - PortaLarg * 0.5f, AltoFrente);                       // oeste, ate a porta
            Muro(Rodape, m, Vector2.up, porta + PortaLarg * 0.5f, Interno, AltoFrente);                  // oeste, depois da porta
            Muro(Rodape, m + Vector2.right * Interno, Vector2.up, 0f, Interno, AltoFundo);               // leste
            Fixo(PrimitiveType.Cube, V3(new Vector2(16f, 2f), 0.002f), new Vector3(Corredor, 0.004f, PortaLarg + 0.1f), CorCapacho);
            Fixo(PrimitiveType.Cube, new Vector3(14f, 0.002f, 4f), new Vector3(4f, 0.004f, 0.12f), CorZona);   // faixa do corredor ate o banheiro
            Fixo(PrimitiveType.Cube, new Vector3(16f, 0.002f, 3f), new Vector3(0.12f, 0.004f, 2f), CorZona);
            int cabines = Mathf.Min(CabinesMax, _g.Sim != null ? _g.Sim.Banheiro.Cabines : Tuning.Padrao.BanheiroCabines);
            for (int i = 0; i < cabines; i++)
            {
                Vector2 q = Cabine(i);
                if (Peca("cabine_banheiro", V3(q), Rumo(Vector2.left)) != null) continue;
                // primitiva: 2 divisorias, a porta entreaberta e o vaso
                foreach (float dz in new[] { -0.21f, 0.21f }) Fixo(PrimitiveType.Cube, new Vector3(q.x, 0.27f, q.y + dz), new Vector3(0.5f, 0.54f, 0.03f), CorDivisoria);
                Fixo(PrimitiveType.Cube, new Vector3(q.x - 0.32f, 0.26f, q.y - 0.12f), new Vector3(0.18f, 0.48f, 0.03f), CorDivisoria);
                Fixo(PrimitiveType.Cylinder, new Vector3(q.x + 0.12f, 0.06f, q.y), new Vector3(0.14f, 0.06f, 0.12f), CorLouca);
            }
            if (Peca("pia_banheiro", V3(new Vector2(Pia.x, 0.45f)), 0f) == null)
            {
                Fixo(PrimitiveType.Cube, new Vector3(Pia.x, 0.2f, 0.5f), new Vector3(0.6f, 0.4f, 0.22f), Art.Hex(0x2E7D9A));
                foreach (float dx in new[] { -0.15f, 0.15f }) Fixo(PrimitiveType.Cylinder, new Vector3(Pia.x + dx, 0.41f, 0.5f), new Vector3(0.14f, 0.02f, 0.12f), CorLouca);
                Fixo(PrimitiveType.Cube, new Vector3(Pia.x, 0.62f, 0.34f), new Vector3(0.56f, 0.26f, 0.02f), Art.Hex(0xBFE3F2));   // espelho
            }
            Fixo(PrimitiveType.Cube, V3(Deposito + new Vector2(-0.2f, -0.25f), 0.25f), new Vector3(0.36f, 0.5f, 0.2f), Art.Hex(0x8A8F99));   // prateleira do deposito
            for (int a = 0; a < Sim.Areas; a++)
                for (int k = 0; k < ManchasPorArea; k++)
                {
                    Vector2 q = Mancha(a, k);
                    Renderer r = Bloco("Mancha" + a + k, PrimitiveType.Cylinder, V3(q, 0.004f), new Vector3(0.2f - 0.02f * k, 0.002f, 0.13f + 0.015f * k), CorMancha);
                    r.transform.localRotation = Quaternion.Euler(0f, 37f * (a + k), 0f);
                    r.enabled = false;
                    _mancha[a, k] = r;
                }
            _coneDaPorta = Cone(V3(BanEntra + new Vector2(0.15f, 0.3f)));
            _placaBanheiro = Letreiro(new Vector3(c.x, AltoFundo + 0.2f, BanMin.y + Lado - Corredor * 0.5f), Textos.Banheiro, CorDivisoria);
        }
    }
}
