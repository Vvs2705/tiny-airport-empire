using System.Collections.Generic;
using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// Construcao do aeroporto (Mundo.cs): blocos, malhas do Tripo, predios por nivel, as 8 salas com paredes, balcoes, fitas e placa. Roda uma
    /// vez, no construtor; o que muda quadro a quadro esta em Mundo.cs.
    /// </summary>
    public sealed partial class Mundo
    {
        Renderer Bloco(string nome, PrimitiveType tipo, Vector3 centro, Vector3 tam, Color cor, Transform pai = null)
        {
            GameObject go = GameObject.CreatePrimitive(tipo);
            go.name = nome;
            // sem colisor: o projeto nao tem o modulo de fisica (a selecao e por conta, no chao em Y = 0)
            go.transform.SetParent(pai != null ? pai : _cont != null ? _cont : _raiz, false);
            go.transform.localPosition = centro;
            go.transform.localScale = tam;
            Renderer r = go.GetComponent<Renderer>();
            if (Material != null) r.sharedMaterial = Material;
            Cor(r, cor);
            return r;
        }

        static void Cor(Renderer r, Color c)
        {
            _mpb.SetColor("_Color", c);
            r.SetPropertyBlock(_mpb);
        }

        /// <summary>Yaw que leva a frente da malha do Tripo ao +z: a maioria sai de frente, mas estas sairam de lado ou de costas e andavam
        /// escorregando 90 graus (medido em 30/10 com -diag; a frente de cada uma foi conferida na vista de cima e de ponta no Blender).</summary>
        static readonly Dictionary<string, float> Frente = new Dictionary<string, float>
        {
            { "aviao_narrow", -90f }, { "rebocador", -90f }, { "carrinho_bagagem", -90f }, { "caminhao_combustivel", -90f }, { "caminhao_catering", -90f }, { "escada_embarque", 180f }, { "catraca_embarque", 90f },
        };

        /// <summary>Malha do Tripo (Resources/Malhas/nome.fbx + nome.png, pivo no chao, de frente para +z) num pai que gira em yaw; null se a
        /// pasta ArtLocal nao existe (quem chama desenha a primitiva). Material Legacy Diffuse com a textura, 1 por peca.</summary>
        Transform Peca(string nome, Vector3 pos, float yaw, float escala = 1f, float eixo = 0f)
        {
            var src = Resources.Load<GameObject>("Malhas/" + nome);
            MeshFilter mf = src != null ? src.GetComponentInChildren<MeshFilter>() : null;
            if (mf == null || Material == null) return null;
            if (!_matMalha.TryGetValue(nome, out Material mat))
                _matMalha[nome] = mat = new Material(Material) { name = nome, mainTexture = Resources.Load<Texture2D>("Malhas/" + nome) };
            var pai = new GameObject(nome).transform;
            pai.SetParent(_cont != null ? _cont : _raiz, false);
            pai.localPosition = pos;
            pai.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var go = new GameObject("malha");
            go.transform.SetParent(pai, false);
            Frente.TryGetValue(nome, out float frente);
            go.transform.localRotation = Quaternion.Euler(0f, frente, 0f) * mf.transform.rotation;   // o giro do import (FBX do Blender), relativo a raiz do modelo
            go.transform.localPosition = new Vector3(0f, 0f, eixo);   // pivo `eixo` m atras do centro da malha (o eixo traseiro dos veiculos)
            go.transform.localScale = mf.transform.lossyScale * escala;
            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return pai;
        }

        /// <summary>As 3 malhas de um predio que cresce (`nome`, `nome_n2`, `nome_n3`), a do nivel ativa (MostraNivel). Sem elas, nada.</summary>
        Transform[] Niveis(string nome, Vector3 pos, float yaw, float escala = 1f) =>
            new[] { Peca(nome, pos, yaw, escala), Peca(nome + "_n2", pos, yaw, escala), Peca(nome + "_n3", pos, yaw, escala) };

        /// <summary>Liga a malha do nivel `n` (0..2); sem ela, a de baixo.</summary>
        static void MostraNivel(Transform[] t, int n)
        {
            while (n > 0 && t[n] == null) n--;
            for (int i = 0; i < t.Length; i++) if (t[i] != null) t[i].gameObject.SetActive(i == n);
        }

        /// <summary>Sombra plana de uma malha: a mesma malha achatada no chao (TA/Sombra). Null sem o shader ou sem malha (primitivas).</summary>
        Transform SombraPlana(Transform t)
        {
            MeshFilter mf = t.GetComponentInChildren<MeshFilter>();
            if (_matSombra == null) { Shader sh = Shader.Find("TA/Sombra"); if (sh != null) _matSombra = new Material(sh); }
            if (mf == null || _matSombra == null || mf.transform.parent != t) return null;
            var pai = new GameObject("Sombra" + t.name).transform;
            pai.SetParent(_raiz, false);
            pai.localScale = new Vector3(1f, 0.002f, 1f);
            var go = new GameObject("malha");
            go.transform.SetParent(pai, false);
            go.transform.localRotation = mf.transform.localRotation;
            go.transform.localScale = mf.transform.localScale;
            go.transform.localPosition = mf.transform.localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = _matSombra;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return pai;
        }

        void Planta()
        {
            Bloco("Gramado", PrimitiveType.Cube, new Vector3(9f, -0.12f, 9f), new Vector3(90f, 0.1f, 90f), Grama);
            Bloco("Calcada", PrimitiveType.Cube, new Vector3(-1.6f, -0.06f, 2f), new Vector3(3.2f, 0.1f, 4.4f), Art.Hex(0xB8B2A6));
            Bloco("Rua", PrimitiveType.Cube, new Vector3(-4.4f, -0.07f, 4f), new Vector3(2.6f, 0.1f, 22f), Asfalto);
            Bloco("Saguao", PrimitiveType.Cube, new Vector3(6f, -0.08f, 4f), new Vector3(12.6f, 0.1f, 8.6f), CorCorredor);
            Bloco("SaguaoLeste", PrimitiveType.Cube, new Vector3(16.1f, -0.08f, 6f), new Vector3(8f, 0.1f, 4.6f), CorCorredor);
            for (int e = 0; e < Salas.Length; e++) { Sala3D(e); Obra(e); }
            Capachos();
            Funde();
            Ponte3D();
            // cenario (so com as malhas; sem elas o greybox da v0.1 nao tinha nada aqui)
            _terminal = Niveis("terminal_vale_verde", new Vector3(1.2f, 0f, 13.2f), 180f);
            _torre = Niveis("torre_controle", new Vector3(26.5f, 0f, 6.3f), 225f);
            _hangar = Niveis("hangar_manutencao", HangarEm, HangarYaw, HangarEscala);
            if (_hangar[2] != null) _hangar[2].position += Vector3.left * 2.66f;   // a malha do nivel 3 e rasa: alinha a fachada com as outras
            Peca("oficina_mecanico", new Vector3(22f, 0f, 5.8f), 180f, 0.75f);
            Peca("sala_tripulacao", new Vector3(6.2f, 0f, 9.9f), 90f, 0.85f);
            foreach (Vector2 v in new[] { new Vector2(-0.5f, 4.5f), new Vector2(-0.5f, -0.5f), new Vector2(8.5f, -0.5f), new Vector2(12.5f, 3.5f) })
                Peca("vaso_planta", V3(v), 0f, 0.7f);
            // arvores so nas bordas (esquerda, direita e atras da pista, fora do alcance da asa): na frente das salas tampariam a fila
            foreach (Vector2 v in new[] { new Vector2(-8f, 2f), new Vector2(-8.5f, 8f), new Vector2(-6f, 14.5f), new Vector2(15f, -2f), new Vector2(19f, 1f), new Vector2(0f, 38.6f), new Vector2(9f, 39.1f), new Vector2(18f, 39.3f) })
                Peca("arvore", V3(v), v.x * 37f);
            foreach (Vector2 v in new[] { new Vector2(8.2f, 12.9f), new Vector2(46f, 7.2f) }) Peca("poste", V3(v), 45f, 0.75f);
            foreach (Vector2 v in new[] { new Vector2(7.3f, 15.6f) }) Peca("cone", V3(v), 0f, 0.7f);
            Peca("conteiner_bagagem", new Vector3(7f, 0f, 16.6f), 0f);
            Peca("conteiner_bagagem", new Vector3(8.3f, 0f, 16.6f), 0f);
            Peca("biruta", new Vector3(30f, 0f, 19.2f), 0f);
            Peca("radar", new Vector3(29.2f, 0f, 4.4f), 0f);
            Patio3D();
        }

        void Sala3D(int e)
        {
            Sala s = Salas[e];
            Vector2 c = Centro(e);
            _piso[e] = Bloco("Piso" + e, PrimitiveType.Cube, new Vector3(c.x, -0.05f, c.y), new Vector3(Interno, 0.1f, Interno), Color.white);
            if (Material != null) _piso[e].sharedMaterial = MatPiso();
            Placa(e);
            // paredes em corte: as de fundo sobem, as da frente deixam ver dentro; ficam de pe com a sala trancada
            Vector2 m = s.Min + Vector2.one * (Corredor * 0.5f);
            Parede(e, new Vector2(m.x, m.y), new Vector2(m.x + Interno, m.y), AltoFrente);                       // sul
            Parede(e, new Vector2(m.x, m.y + Interno), new Vector2(m.x + Interno, m.y + Interno), AltoFundo);   // norte
            Parede(e, new Vector2(m.x, m.y), new Vector2(m.x, m.y + Interno), AltoFrente);                       // oeste
            Parede(e, new Vector2(m.x + Interno, m.y), new Vector2(m.x + Interno, m.y + Interno), AltoFundo);   // leste
            _sala[e] = _cont = new GameObject("Sala" + e).transform;
            _cont.SetParent(_raiz, false);
            // moldura de estado no chao (normal/atencao/critico; destino aceso no arrasto)
            _moldura[e] = new Renderer[4];
            for (int i = 0; i < 4; i++)
            {
                bool hor = i < 2;
                float off = (i % 2 == 0 ? -1f : 1f) * (Interno * 0.5f - 0.08f);
                Vector3 p = hor ? new Vector3(c.x, 0.01f, c.y + off) : new Vector3(c.x + off, 0.01f, c.y);
                _moldura[e][i] = Bloco("Moldura" + e + i, PrimitiveType.Cube, p, hor ? new Vector3(Interno - 0.1f, 0.02f, 0.12f) : new Vector3(0.12f, 0.02f, Interno - 0.1f), Art.Good);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector2 b = e == (int)Estacao.Seguranca ? Em(e, SegMaquinaD, SegA[i] + BeltX) : Balcao(e, i);
                Transform t = Peca(BalcaoMalha[e], V3(b), Rumo(-s.N), BalcaoEscala[e]);   // de frente para a fila
                if (t != null)
                {
                    _mesa[e, i, 0] = t.gameObject;
                    string ant = BalcaoMalha[e];
                    for (int n = 1; n < NivelMalha.Length; n++)
                    {
                        string nome = BalcaoMalha[e] + "_n" + (n + 1);
                        Transform tn = MesaNivel(e, n, b);
                        if (tn == null) tn = Peca(nome, V3(b), Rumo(-s.N), BalcaoEscala[e]);
                        if (tn != null) ant = nome;
                        else if ((tn = Peca(ant, V3(b), Rumo(-s.N), BalcaoEscala[e] * (1f + 0.08f * n))) != null)   // reserva ate o export chegar
                            foreach (Renderer r in tn.GetComponentsInChildren<Renderer>()) Cor(r, ReservaCor);   // (ASSETS §2.6): maior e dourado
                        _mesa[e, i, n] = tn != null ? tn.gameObject : null;
                    }
                }
                else
                {
                    bool longoX = Mathf.Abs(s.N.y) > 0.5f;
                    _mesa[e, i, 0] = Bloco("Balcao" + e + i, PrimitiveType.Cube, new Vector3(b.x, 0.25f, b.y), longoX ? new Vector3(0.65f, 0.4f, 0.4f) : new Vector3(0.4f, 0.4f, 0.65f), Art.Paper).gameObject;
                }
                _arco[e, i] = Billboard("Arco" + e + i, Art.Arc(0f), Art.Good, 0.24f);
                _vazio[e, i] = Billboard("Vazio" + e + i, Art.Xis(), Art.A(Art.Off, 0.85f), 0.22f);
                if (e == (int)Estacao.Seguranca) Seguranca3D(i);
                if (e == SalaMalas) Balanca3D(i);
            }
            if (e == SalaMalas) Esteira3D();
            if (e == Portao) Espera3D();
            else if (e != (int)Estacao.Manutencao && e != (int)Estacao.Despacho)   // oficina e despacho nao tem fila de passageiros
            {
                Tapete(e, 2.1f, 2.84f, 1.7f);
                // fitas da fila: postes nas pontas das fileiras
                for (int r = 0; r < 5; r++)
                    for (int lado = -1; lado <= 1; lado += 2)
                    {
                        Vector2 p = Em(e, FilaD + (r + 0.5f) * FilaLinha, lado * PosteA);
                        Fixo(PrimitiveType.Cylinder, new Vector3(p.x, 0.12f, p.y), new Vector3(0.03f, 0.12f, 0.03f), Fita);
                    }
                for (int r = 0; r < 4; r++)
                {
                    Vector2 p = Em(e, FilaD + (r + 0.5f) * FilaLinha, (r % 2 == 0 ? 0.16f : -0.16f));
                    bool longoX = Mathf.Abs(s.T.x) > 0.5f;
                    _fita[e, r] = Bloco("Fita", PrimitiveType.Cube, new Vector3(p.x, 0.2f, p.y), longoX ? new Vector3(FitaLarg, 0.03f, 0.02f) : new Vector3(0.02f, 0.03f, FitaLarg), Fita).gameObject;
                }
            }
            Mobilia(e);
            _cont = null;
            _mao[e] = Billboard("Toque" + e, Art.Mao(), Art.BoardInk, 0.6f);
        }

        /// <summary>Parede de a ate b, com vao onde houver porta (tabela Portas).</summary>
        void Parede(int e, Vector2 a, Vector2 b, float alto)
        {
            Vector2 d = (b - a).normalized;
            float len = Vector2.Distance(a, b);
            var cortes = new List<float>();
            foreach (Porta porta in Portas)
            {
                float t = Vector2.Dot(porta.P - a, d);
                if (t > 0f && t < len && Vector2.Distance(a + d * t, porta.P) < Corredor * 0.5f + 0.01f) cortes.Add(t);   // a porta fica no meio do corredor
            }
            cortes.Sort();
            float ini = 0f;
            foreach (float t in cortes) { Muro(e, a, d, ini, t - PortaLarg * 0.5f, alto); ini = t + PortaLarg * 0.5f; }
            Muro(e, a, d, ini, len, alto);
        }

        void Muro(int e, Vector2 a, Vector2 d, float t0, float t1, float alto)
        {
            if (t1 - t0 < 0.05f) return;
            Vector2 c = a + d * ((t0 + t1) * 0.5f);
            float l = t1 - t0 + 0.06f;
            bool x = Mathf.Abs(d.x) > 0.5f;
            Vector3 tam(float h, float esp) => x ? new Vector3(l, h, esp) : new Vector3(esp, h, l);
            Fixo(PrimitiveType.Cube, new Vector3(c.x, alto * 0.5f, c.y), tam(alto, EspParede), CorParede);
            Fixo(PrimitiveType.Cube, new Vector3(c.x, alto + 0.012f, c.y), tam(0.024f, EspParede + 0.03f), CorCapa);
            if (alto > AltoFrente) Fixo(PrimitiveType.Cube, new Vector3(c.x, 0.09f, c.y), tam(0.18f, EspParede + 0.012f), Art.EstacaoCor[e]);   // rodape na cor da sala
        }

        /// <summary>Letreiro em cima da parede norte (quadro na cor do setor com o nome, um pouco deitado para a camera; atras dele so ha corredor) e o selo do estado, que so aparece
        /// em atencao ou critico.</summary>
        void Placa(int e)
        {
            Vector3 p = V3(Salas[e].Min + new Vector2(Lado * 0.5f, Lado - Corredor * 0.5f), AltoFundo + 0.2f);
            Quaternion rosto = Quaternion.Euler(20f, 0f, 0f);
            Bloco("Placa" + e, PrimitiveType.Cube, p, new Vector3(1.8f, 0.34f, 0.03f), Art.EstacaoCor[e]).transform.rotation = rosto;
            var go = new GameObject("PlacaNome" + e);
            go.transform.SetParent(_raiz, false);
            go.transform.SetPositionAndRotation(p + rosto * Vector3.back * 0.02f, rosto);
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = Art.Font();
            tm.fontSize = 72;
            tm.characterSize = 0.03f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = Art.Ink;
            tm.text = Textos.Estacao[e];
            go.GetComponent<MeshRenderer>().sharedMaterial = Art.Font().material;
            _placa[e] = tm;

            RectTransform r = Art.Node(_rotulos, "Selo" + e, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            r.sizeDelta = new Vector2(250f, 40f);
            _selo[e] = r;
            _seloFundo[e] = Art.Panel(r, "Fundo", Art.A(Art.Dark, 0.7f), Vector2.zero, Vector2.one);
            _seloFundo[e].raycastTarget = false;
            Art.Img(r, "Icone", Art.Alerta(), Color.white, new Vector2(0.02f, 0.1f), new Vector2(0.15f, 0.9f)).raycastTarget = false;
            _seloTexto[e] = Art.Outlined(Art.NewText(r, "Estado", 20, new Vector2(0.17f, 0f), new Vector2(0.98f, 1f), TextAnchor.MiddleLeft), 2f);
        }

        SpriteRenderer Billboard(string nome, Sprite s, Color c, float tam)
        {
            SpriteRenderer r = Art.NewSprite(_raiz, nome, s, c, 0, Vector2.zero, new Vector2(tam, tam));
            r.enabled = false;
            return r;
        }
    }
}
