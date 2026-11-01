using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Veiculos do patio (GDD §5.12), so tela. A garagem fica a leste, aberta para o sul, com o patio de manobra na frente: cada veiculo tem a
    /// sua vaga e a sua faixa, para nenhum encostar em outro (medido com client/tools/diag_pessoas.py).
    ///  - O aviao encosta: os veiculos dos servicos que faltam saem de re da vaga, seguem a rua oeste (mao unica, na frente dos narizes), sobem a
    ///    faixa deles ao lado da fuselagem e esperam la. O nucleo faz o servico (Aviao.Fazendo); o veiculo sai quando acaba (Aviao.Falta) pela
    ///    rua norte, desce a avenida (ou o retorno, a leste do jato) e volta pela rua leste, de frente para a vaga. Se o outro aviao precisa
    ///    dele, vai direto.
    ///  - Faixas: 3 do lado do regional (catering, bagagem, combustivel), 3 do lado direito do jato e, do lado esquerdo do jato, onibus e
    ///    escada, que ficam ate ele sair. No estande G o desembarque e pela ponte.
    ///  - O +1 veiculo da Capacidade tem vaga pintada ao lado da garagem.
    /// </summary>
    public sealed partial class Mundo
    {
        const float RuaOeste = 10.4f, RuaLeste = 8.85f, RuaNorte = 18.4f, Avenida = 17.2f, Retorno = 28.3f;
        /// <summary>Garagem (malha aberta para o sul em yaw 0, 6 vagas de 1,6 m) e a linha das vagas: 3 a mais, pintadas, para o +1 veiculo.</summary>
        static readonly Vector3 GaragemEm = new Vector3(35.6f, 0f, 14.6f);
        const float VagaX0 = 31.6f, VagaPasso = 1.6f, VagaZ = 13.1f;
        static float VagaX(int i) => VagaX0 + i * VagaPasso;

        /// <summary>Cada tipo de veiculo: Servico, faixa em cada estande (-1 = nao vai), parada (pivo) na faixa e vaga na garagem. Na ordem das
        /// vagas: o catering sai antes, porque a limpeza vem logo depois do desembarque.</summary>
        sealed class Tipo
        {
            public string Malha; public int Servico, Vaga, Extra; public float Vel, Eixo, Comp, Parada; public Color Cor; public float[] Faixa;
        }

        static readonly Tipo[] Tipos =
        {
            new Tipo { Malha = "caminhao_catering", Servico = 1, Vaga = 0, Extra = 6, Vel = 2.6f, Eixo = 0.75f, Comp = 2.8f, Parada = 12.3f, Cor = Art.Hex(0xF2F4F7), Faixa = new[] { 12.8f, 24f } },
            new Tipo { Malha = "caminhao_combustivel", Servico = 2, Vaga = 1, Extra = 7, Vel = 2.6f, Eixo = 0.75f, Comp = 2.6f, Parada = 13.6f, Cor = Art.Hex(0xE0533D), Faixa = new[] { 15.3f, 26.5f } },
            new Tipo { Malha = "carrinho_bagagem", Servico = 3, Vaga = 2, Extra = 8, Vel = 2.6f, Eixo = 0.6f, Comp = 2.6f, Parada = 14.8f, Cor = Art.Hex(0x5DADE2), Faixa = new[] { 14.05f, 25.25f } },
            new Tipo { Malha = "escada_embarque", Servico = 0, Vaga = 3, Vel = 2f, Eixo = 0.7f, Comp = 2.3f, Parada = 12.4f, Cor = Art.Hex(0xE5E8E8), Faixa = new[] { -1f, 20.5f } },
            new Tipo { Malha = "onibus_patio", Servico = 0, Vaga = 4, Vel = 2f, Eixo = 0.9f, Comp = 3.2f, Parada = 12f, Cor = Art.Hex(0x3FA7D6), Faixa = new[] { -1f, 19.1f } },
        };

        sealed class Veiculo
        {
            public Transform T, Malha;
            public SpriteRenderer Sombra;
            public Quaternion Base = Quaternion.identity;
            public Tipo Tipo;   // null = o rebocador
            public int Aviao = -1, Vaga;
            public float S, V, Vel, Comp, Pitch, Roll, Parado;
            public Lida Lida;
            public readonly Queue<Trecho> Rotas = new Queue<Trecho>();
        }

        enum Lida { Parado, Indo, Servindo, Voltando }

        readonly List<Veiculo> _veiculos = new List<Veiculo>();
        Reboque _reboque;

        void Veiculos3D()
        {
            Peca("garagem_patio", GaragemEm, 0f);
            for (int i = 6; i <= 9; i++) Chao("VagaLinha", new Vector3(VagaX(i) - VagaPasso * 0.5f, -0.04f, 14.6f), new Vector2(0.09f, 4f), Art.Hex(0xF2C14E));
            Chao("VagaFundo", new Vector3(VagaX(7), -0.04f, 16.6f), new Vector2(VagaPasso * 3f, 0.09f), Art.Hex(0xF2C14E));
            // vaga pintada do rebocador
            foreach (float z in new[] { ReboqueVaga.z - 0.5f, ReboqueVaga.z + 0.5f }) Chao("VagaReboque", new Vector3(14.3f, -0.04f, z), new Vector2(2f, 0.07f), Art.Hex(0xF2C14E));
            Veiculo r = NovoVeiculo("rebocador", null, 2.2f, 0.35f, Art.Hex(0xF2C14E), 1.5f);
            r.T.SetPositionAndRotation(ReboqueVaga, Quaternion.Euler(0f, -90f, 0f));
            Sombra(r);
            foreach (Tipo t in Tipos) NaVaga(t, t.Vaga);
        }

        /// <summary>Veiculo com o pivo no eixo traseiro (`eixo` = do eixo ao centro da malha, em m).</summary>
        Veiculo NovoVeiculo(string malha, Tipo tipo, float vel, float eixo, Color cor, float comp)
        {
            var v = new Veiculo { Tipo = tipo, Vel = vel, Comp = comp };
            v.T = Peca(malha, Vector3.zero, 0f, 1f, eixo);
            if (v.T != null) { v.Malha = v.T.GetChild(0); v.Base = v.Malha.localRotation; }
            else
            {
                v.T = new GameObject(malha).transform;
                v.T.SetParent(_raiz, false);
                Bloco("Carroceria", PrimitiveType.Cube, new Vector3(0f, 0.3f, -0.1f), new Vector3(0.55f, 0.45f, comp - 0.4f), cor, v.T);
                Bloco("Cabine", PrimitiveType.Cube, new Vector3(0f, 0.38f, comp * 0.5f - 0.2f), new Vector3(0.55f, 0.55f, 0.35f), Art.Hex(0xECF0F1), v.T);
            }
            v.Sombra = SombraDe(v.T);
            _veiculos.Add(v);
            return v;
        }

        /// <summary>Veiculo do tipo `t` na vaga `vaga`, de nariz para dentro da garagem.</summary>
        Veiculo NaVaga(Tipo t, int vaga)
        {
            Veiculo v = NovoVeiculo(t.Malha, t, t.Vel, t.Eixo, t.Cor, t.Comp);
            v.Vaga = vaga;
            v.T.SetPositionAndRotation(V(VagaX(vaga), VagaZ), Quaternion.identity);
            Sombra(v);
            return v;
        }

        // ------------------------------------------------------------------ rebocador

        /// <summary>O aviao da ponte quer sair: o rebocador sai da vaga, faz a curva e encosta a barra no nariz.</summary>
        void ReboqueVai()
        {
            Veiculo r = _veiculos[0];
            Trecho t = Caminho(1f, r.Vel, ReboqueVaga, new Vector3(ReboqueNariz.x, 0f, ReboqueVaga.z), ReboqueNariz);
            t.A = 0.7f; t.F = 0.8f; t.Para = true;
            r.Rotas.Enqueue(t);
            r.S = 0f;
            _reboque = Reboque.Indo;
        }

        /// <summary>Rebocador preso no nariz durante o pushback, empurrando.</summary>
        void Reboca(Aviao a, float dt)
        {
            Veiculo r = _veiculos[0];
            Vector3 nariz = a.T.forward;
            nariz.y = 0f;
            nariz.Normalize();
            Vector3 p = new Vector3(a.T.position.x, 0f, a.T.position.z) + nariz * (a.Nariz + (NarizZ - ReboqueNariz.z)), d = p - r.T.position;
            r.T.position = p;
            // a barra articula: o rebocador aponta para onde o nariz anda, nao para o eixo do aviao, e nao escorrega de lado na curva do recuo
            if (dt > 0f && d.magnitude / dt > 0.15f) r.T.rotation = Quaternion.RotateTowards(r.T.rotation, Quaternion.LookRotation(d, Vector3.up), 90f * dt);
            Sombra(r);
        }

        /// <summary>Solto na taxiway: recua fazendo a curva para o norte, cruza a taxiway para o sul, atravessa o estande vazio, passa na frente
        /// da rua oeste e volta para a vaga (de frente para oeste): nenhum giro no lugar.</summary>
        void RebocadorVolta()
        {
            Veiculo r = _veiculos[0];
            Vector3 p = r.T.position, tras = -r.T.forward, fim = p + tras * 1.6f + Vector3.forward * 1.6f;
            Trecho re = Caminho(1.4f, 1.1f, p, p + tras * 1.6f, fim);
            re.Re = re.Para = true; re.A = 0.6f; re.F = 0.7f;
            Trecho vai = Caminho(1.4f, 2.2f, fim, new Vector3(fim.x, 0f, NarizZ + 2.2f), new Vector3(11.8f, 0f, NarizZ + 2.2f), new Vector3(11.8f, 0f, 9.5f), new Vector3(15.9f, 0f, 9.5f), new Vector3(15.9f, 0f, ReboqueVaga.z), ReboqueVaga);
            vai.A = 0.7f; vai.F = 0.8f; vai.Para = true;
            r.Rotas.Enqueue(re); r.Rotas.Enqueue(vai);
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

        /// <summary>Um por vez no patio de manobra, na frente da garagem: quem esta recuando ou entrando na vaga segura a proxima saida.</summary>
        bool ManobraOcupada()
        {
            foreach (Veiculo v in _veiculos)
                if (v.Tipo != null && v.Lida != Lida.Parado && v.T.position.x > Retorno - 0.5f && v.T.position.z < VagaZ + 0.1f) return true;
            return false;
        }

        /// <summary>Sai de re da vaga (a cauda vira para leste) e segue de frente pela rua oeste ate a faixa.</summary>
        void Sai(Veiculo v, int k)
        {
            Tipo t = v.Tipo;
            float x = VagaX(v.Vaga), f = t.Faixa[k];
            Trecho re = Caminho(RaioVeic, 1.3f, V(x, VagaZ), V(x, RuaOeste), V(x + VagaPasso, RuaOeste));
            re.Re = re.Para = true; re.A = 0.7f; re.F = 0.9f;
            v.Rotas.Enqueue(re);
            v.Rotas.Enqueue(DeFrente(v, V(x + VagaPasso, RuaOeste), V(f, RuaOeste), V(f, t.Parada)));
            v.Aviao = k; v.Lida = Lida.Indo; v.S = 0f;
        }

        /// <summary>Acabou: sobe a faixa ate a rua norte, desce a avenida (lado do regional) ou o retorno (lado direito do jato) e volta pela rua
        /// leste; se o outro aviao precisa, e ninguem foi, vai direto para a faixa dele.</summary>
        void Libera(Veiculo v, Sim s)
        {
            Tipo t = v.Tipo;
            int k = v.Aviao, o = 1 - k;
            float f = t.Faixa[k], rua = f > EstandeX[1] ? Retorno : Avenida;
            var p = new List<Vector3> { V(f, t.Parada), V(f, RuaNorte), V(rua, RuaNorte) };
            v.Aviao = -1;
            bool ocupado = false;
            foreach (Veiculo w in _veiculos) if (w.Tipo == t && w.Aviao == o) ocupado = true;
            if (t.Servico > 0 && !ocupado && Precisa(t, o, s))
            {
                float q = rua == Avenida ? RuaLeste : RuaOeste;   // da avenida segue a rua leste, do retorno a rua oeste
                p.Add(V(rua, q)); p.Add(V(t.Faixa[o], q)); p.Add(V(t.Faixa[o], t.Parada));
                v.Aviao = o; v.Lida = Lida.Indo;
            }
            else
            {
                p.Add(V(rua, RuaLeste)); p.Add(V(VagaX(v.Vaga), RuaLeste)); p.Add(V(VagaX(v.Vaga), VagaZ));
                v.Lida = Lida.Voltando;
            }
            v.Rotas.Enqueue(DeFrente(v, p.ToArray()));
            v.S = 0f;
        }

        Trecho DeFrente(Veiculo v, params Vector3[] p)
        {
            Trecho t = Caminho(RaioVeic, v.Vel, p);
            t.A = 0.8f; t.F = 1f; t.Para = true;
            return t;
        }

        /// <summary>Quem vem atras espera (fila natural). Conta a distancia de freada, para nao encostar. Depois de 6 s parado segue ate se soltar:
        /// e a saida de um cruzamento travado, que nao deve acontecer (a medida das caixas acusaria).</summary>
        bool FrenteLivre(Veiculo v, Trecho t, float dt)
        {
            Vector3 f = t.Re ? -v.T.forward : v.T.forward, c = Centro(v), lado = new Vector3(f.z, 0f, -f.x);
            float freada = v.V * v.V / (2f * t.F);
            bool cruza = Cruza(v, t, out float rua, out float cruzaX);
            foreach (Veiculo o in _veiculos)
            {
                if (o == v) continue;
                Vector3 d = Centro(o) - c;
                float frente = Vector3.Dot(d, f);
                bool naRua = cruza && o.Lida != Lida.Parado && Mathf.Abs(o.T.position.z - rua) < 0.4f && Mathf.Abs(o.T.forward.x) > 0.7f
                    && Mathf.Abs(o.T.position.x - cruzaX) < (rua == RuaOeste ? 9f : 3.4f);
                if (naRua || (frente > 0f && frente < (v.Comp + o.Comp) * 0.5f + 0.5f + freada && Mathf.Abs(Vector3.Dot(d, lado)) < 1.2f))
                {
                    v.Parado += dt;
                    return v.Parado >= 6f;
                }
            }
            v.Parado = 0f;
            return true;
        }

        /// <summary>O trajeto entra nos proximos metros na rua oeste ou na rua norte, vindo de fora dela: `rua` e o x do cruzamento. Quem cruza
        /// espera quem ja vem pela rua, antes de o nariz chegar nela.</summary>
        static bool Cruza(Veiculo v, Trecho t, out float rua, out float x)
        {
            rua = x = 0f;
            float z = v.T.position.z;
            if (v.Tipo == null || (v.T.forward.z < -0.3f && z < RuaOeste)) return false;   // acabou de cruzar: termina a curva antes de parar, com a cauda fora da rua
            for (int i = Indice(t, v.S); i < t.P.Count && t.L[i] <= v.S + 5.5f; i += 3)
            {
                float pz = t.P[i].z;
                rua = Mathf.Abs(pz - RuaOeste) < Mathf.Abs(pz - RuaNorte) ? RuaOeste : RuaNorte;
                if (Mathf.Abs(pz - rua) < 0.9f && Mathf.Abs(z - rua) >= 0.5f) { x = t.P[i].x; return true; }
            }
            return false;
        }

        static Vector3 Centro(Veiculo v) => v.T.position + v.T.forward * (v.Comp * 0.4f);

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
                v.V = Passo(v.V, t, v.S, reboque || FrenteLivre(v, t, dt) ? t.V : 0f, t.Para ? t.Total - v.S : 1e4f, LatVeic, dt);
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
