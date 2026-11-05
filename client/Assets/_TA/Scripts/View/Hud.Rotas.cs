using System.Collections.Generic;
using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// Rotas e aeroportos (CONTRATO_FASE_C3 §2 e §3): uma folha com uma aba por aeroporto. No alto, o que o aeroporto traz de diferente (ritmo
    /// das chegadas, desgaste e incidentes) e o mapa dos destinos, um painel de aeroporto: o aeroporto no centro e cada rota na direcao da cidade,
    /// mais longe quanto mais dura o trecho; aberta em amarelo, trancada apagada e com cadeado, e na cor do voo a que esta programada. Embaixo, a
    /// viagem, as 2 vagas da proxima onda e as 4 rotas, cada uma com distancia, frequencia, receita e risco e o botao do estado: abrir pelo preco,
    /// por na vaga escolhida ou o motivo da tranca. No HUD, o atalho fica ao lado da faixa da meta, com o aeroporto em que o jogador esta e o selo
    /// do que abriu desde a ultima visita. So le o Sim e manda intencao pelo Game (Viajar, AbreRota, Programa), que o nucleo valida.
    /// </summary>
    public sealed partial class Hud
    {
        /// <summary>Mapa em px da referencia de 1080: elipse de RaioX x RaioY ate a rota mais longa, em volta do centro.</summary>
        public const float LargMapa = 1032f, AltMapa = 560f, RaioX = 380f, RaioY = 200f, LargRotuloMapa = 250f;
        /// <summary>Largura das linhas do cartao de rota e do nome do aeroporto no atalho; o teste confere que cabe.</summary>
        public const float LargTextoRota = 554f, LargAeroAtalho = 206f;
        const float AltAtalhoRotas = 144f, LargAtalhoRotas = RefW - 48f - LargFaixaMeta, AltCartaoRota = 236f;

        /// <summary>O botao de cada rota, na ordem em que o estado e decidido.</summary>
        public enum EstadoRota { NaVaga, Programar, Aberta, Comprar, Terminal, Viaje }
        public enum EstadoViagem { Aqui, Marcada, Espere, Terminal, Voltar, Comprar }

        /// <summary>Longitude e latitude pelo Id da tabela. Os 3 aeroportos sao de mentira: ficam onde o desafio de cada um faz sentido.</summary>
        static readonly Dictionary<string, Vector2> Coordenada = new Dictionary<string, Vector2>
        {
            { "vv", new Vector2(-48.6f, -22.4f) }, { "ca", new Vector2(-36.3f, -10.6f) }, { "ag", new Vector2(-45f, -21f) },
            { "vv_campinas", new Vector2(-47.1f, -22.9f) }, { "vv_ribeirao", new Vector2(-47.8f, -21.2f) }, { "vv_curitiba", new Vector2(-49.3f, -25.4f) }, { "vv_bh", new Vector2(-43.9f, -19.9f) },
            { "ca_salvador", new Vector2(-38.5f, -13f) }, { "ca_recife", new Vector2(-34.9f, -8.05f) }, { "ca_fortaleza", new Vector2(-38.5f, -3.7f) }, { "ca_brasilia", new Vector2(-47.9f, -14.5f) },   // ponytail: real -15,8; 1,3° ao norte só no desenho, para o ponto não cobrir o de Salvador
            { "ag_guarulhos", new Vector2(-46.5f, -23.4f) }, { "ag_galeao", new Vector2(-43.2f, -22.8f) }, { "ag_manaus", new Vector2(-60f, -3.1f) }, { "ag_lisboa", new Vector2(-9.1f, 38.7f) },
        };

        /// <summary>Areas do mapa que os destinos nao podem cobrir, a partir do centro: o nome do aeroporto, o estado e a legenda das vagas.</summary>
        public static readonly Rect[] ReservaMapa =
        {
            new Rect(28f - LargMapa / 2f, AltMapa / 2f - 72f, 560f, 56f), new Rect(28f - LargMapa / 2f, 14f - AltMapa / 2f, 560f, 48f),
            new Rect(LargMapa / 2f - 358f, 14f - AltMapa / 2f, 330f, 48f),
        };

        sealed class Destino { public Image Traco, Anel, Ponto, Glifo; public Text Nome; }
        sealed class CartaoRota { public Item It; public Image Destaque; public readonly Image[] Chip = new Image[2]; }

        Modal _rotas;
        int _rotasAero, _rotasVaga, _rotasVistas;
        RectTransform _rotasLista, _vagas, _atalhoRotas;
        Button[] _rotasAba;
        Image[] _rotasTranca;
        Numero _ritmo, _desgaste, _incidentes;
        Image _mapaHub, _mapaAqui, _atalhoRotasPlaca, _seloRotas;
        Text _mapaTitulo, _mapaEstado, _atalhoRotasAero, _seloRotasN;
        Destino[] _destino;
        CartaoRota[] _cartaoRota;
        Item _viagem;
        readonly Button[] _vagaBtn = new Button[2];
        readonly Text[] _vagaDestino = new Text[2];

        // ------------------------------------------------------------------ geometria do mapa (o teste confere)

        /// <summary>Centro do ponto da rota r, em px a partir do centro do mapa (y para cima): na direcao da cidade, a 60% do raio no trecho mais
        /// curto e no raio inteiro no mais longo.</summary>
        public static Vector2 NoMapa(Rota[] rotas, Aeroporto[] aeroportos, int r)
        {
            Rota x = rotas[r];
            Vector2 o = Coordenada[aeroportos[x.Aeroporto].Id], d = Coordenada[x.Id];
            float maior = 0f;
            foreach (Rota y in rotas) maior = Mathf.Max(maior, y.Extra);
            float ang = Mathf.Atan2(d.y - o.y, (d.x - o.x) * Mathf.Cos(o.y * Mathf.Deg2Rad)), f = 0.6f + 0.4f * Mathf.Sqrt(maior > 0f ? x.Extra / maior : 0f);
            return new Vector2(Mathf.Cos(ang) * RaioX * f, Mathf.Sin(ang) * RaioY * f);
        }

        /// <summary>O nome do destino no ponto p (canto de baixo a esquerda e tamanho): ao lado do ponto se a rota vai mais para os lados, senao em
        /// cima (rota para o norte) ou embaixo.</summary>
        public static Rect RotuloNoMapa(Vector2 p, float largura)
        {
            float c = Mathf.Cos(Mathf.Atan2(p.y / RaioY, p.x / RaioX));
            if (Mathf.Abs(c) > 0.6f) return new Rect(c > 0f ? p.x + 28f : p.x - 28f - largura, p.y - 21f, largura, 42f);
            return new Rect(p.x - largura / 2f, p.y >= 0f ? p.y + 22f : p.y - 64f, largura, 42f);
        }

        static TextAnchor Alinha(Vector2 p)
        {
            float c = Mathf.Cos(Mathf.Atan2(p.y / RaioY, p.x / RaioX));
            return c > 0.6f ? TextAnchor.MiddleLeft : c < -0.6f ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter;
        }

        // ------------------------------------------------------------------ estados (a regra e do Sim; aqui so a ordem do que mostrar)

        public static EstadoViagem EstadoDaViagem(Sim s, int a)
        {
            if (a == s.AeroportoAtual) return EstadoViagem.Aqui;
            if (a == s.ProximoAeroporto) return EstadoViagem.Marcada;
            if (s.ProximoAeroporto >= 0) return EstadoViagem.Espere;
            long c = s.CustoViagem(a);
            return c < 0 ? EstadoViagem.Terminal : c == 0 ? EstadoViagem.Voltar : EstadoViagem.Comprar;
        }

        /// <summary>Aberta: na vaga, programavel (so no aeroporto em que o jogador esta) ou so aberta. Fechada: a venda, antes do marco ou de
        /// aeroporto ainda nao pago.</summary>
        public static EstadoRota EstadoDaRota(Sim s, int r, int vaga)
        {
            Rota x = s.T.Rotas[r];
            if (s.RotaAberta(r)) return x.Aeroporto != s.AeroportoAtual ? EstadoRota.Aberta : s.Prog[vaga] == r ? EstadoRota.NaVaga : EstadoRota.Programar;
            if (s.CustoRota(r) >= 0) return EstadoRota.Comprar;
            return s.Marco < x.Marco ? EstadoRota.Terminal : EstadoRota.Viaje;
        }

        /// <summary>Bit por rota que da para abrir agora e por viagem a venda (16 + aeroporto): o selo do atalho conta as que o jogador nao viu.</summary>
        static int Novidades(Sim s)
        {
            int m = 0;
            for (int r = 0; r < s.T.Rotas.Length; r++) if (s.CustoRota(r) >= 0) m |= 1 << r;
            for (int a = 0; a < s.T.Aeroportos.Length; a++) if (s.CustoViagem(a) > 0) m |= 1 << (16 + a);
            return m;
        }

        static int Bits(int m) { int n = 0; for (; m != 0; m &= m - 1) n++; return n; }

        static bool TemPico(Rota[] rotas, int a) { foreach (Rota x in rotas) if (x.Aeroporto == a && x.Pico > 0f) return true; return false; }

        static Sprite IconeRota(TipoRota t)
        {
            switch (t) { case TipoRota.Turistica: return Art.Sacola(); case TipoRota.Conexao: return Art.Bilhete(); case TipoRota.Longa: return Art.Relogio(); default: return Art.Aviao(); }
        }

        static Color CorRota(TipoRota t) => Art.EstacaoCor[t == TipoRota.Turistica ? 5 : t == TipoRota.Conexao ? 1 : t == TipoRota.Longa ? 7 : 0];

        // ------------------------------------------------------------------ construcao

        void BuildRotas(RectTransform canvas)
        {
            Sim s = S;
            _rotas = NovaFolhaModal(canvas, "Rotas", Textos.Rotas, Art.Aviao(), 1600f);
            RectTransform corpo = _rotas.F.Corpo;
            int n = s.T.Aeroportos.Length;
            _rotasAba = new Button[n];
            _rotasTranca = new Image[n];
            for (int a = 0; a < n; a++)
            {
                int aa = a;
                _rotasAba[a] = Art.NewButton(corpo, Textos.AeroportoNome[a], 40, Art.Suave, new Vector2((float)a / n, 1f), new Vector2((a + 1f) / n, 1f), () => MostraAero(aa));
                var r = (RectTransform)_rotasAba[a].transform;
                r.offsetMin = new Vector2(a == 0 ? 24f : 6f, -160f); r.offsetMax = new Vector2(a == n - 1 ? -24f : -6f, -16f);
                Art.Rotulo(_rotasAba[a]).resizeTextMinSize = MenorTexto;
                _rotasTranca[a] = Art.Img(_rotasAba[a].transform, "Tranca", Art.Cadeado(), Art.Fraco, Vector2.zero, Vector2.zero);
                Art.Fixa(_rotasTranca[a].rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(36f, 36f));
            }
            _rotasLista = Art.NovaLista(corpo, Vector2.zero, Vector2.one);
            Art.Margem((RectTransform)_rotasLista.parent, 24f, 16f, 24f, 176f);

            RectTransform nums = Art.Node(_rotasLista, "Numeros", Vector2.zero, Vector2.one);
            LayoutElement e = nums.gameObject.AddComponent<LayoutElement>();
            e.minHeight = e.preferredHeight = 156f;
            _ritmo = Art.NovoNumero(nums, "Ritmo", Art.Pessoas(), Art.Azul, Textos.Ritmo, Vector2.zero, new Vector2(0.34f, 1f));
            _desgaste = Art.NovoNumero(nums, "Desgaste", Art.Chave(), Art.EstacaoCor[6], Textos.DesgasteNome, new Vector2(0.34f, 0f), new Vector2(0.67f, 1f));
            _incidentes = Art.NovoNumero(nums, "Incidentes", Art.Alerta(), Art.Warn, Textos.Incidentes, new Vector2(0.67f, 0f), Vector2.one);

            BuildMapa(s);
            _viagem = NovoItem(_rotasLista, "Viagem", Art.Aviao(), Art.Azul, () => _g.Viajar(_rotasAero), AltCartaoRota);
            _viagem.Info.text = Textos.ViagemInfo;
            BuildVagas();
            _cartaoRota = new CartaoRota[s.T.Rotas.Length];
            for (int r = 0; r < _cartaoRota.Length; r++)
            {
                int rr = r;
                Rota x = s.T.Rotas[r];
                var c = new CartaoRota { It = NovoItem(_rotasLista, "Rota" + r, IconeRota(x.Tipo), CorRota(x.Tipo), () => TocaRota(rr), AltCartaoRota) };
                c.It.Nome.text = Textos.Destino[r];
                c.It.Linha.text = Textos.RotaLinha(x, s.TrechoSeg(r));
                c.It.Info.text = Textos.RotaInfo(x, s.T.RiscoChance);
                c.Destaque = Art.Img(c.It.Rt, "Destaque", Art.Contorno(), Art.Placa, Vector2.zero, Vector2.one);
                c.Destaque.type = Image.Type.Sliced; c.Destaque.preserveAspect = false;
                for (int f = 0; f < 2; f++)
                {
                    c.Chip[f] = Art.Img(c.It.Rt, "Vaga" + f, Art.Mala(), Art.VooCor[f], Vector2.one, Vector2.one);
                    Art.Fixa(c.Chip[f].rectTransform, Vector2.one, new Vector2(-306f - (1 - f) * 48f, -28f), new Vector2(40f, 40f));
                }
                _cartaoRota[r] = c;
            }
            BuildAtalhoRotas();
        }

        /// <summary>Painel escuro como o quadro de partidas: linhas embaixo, depois o aeroporto, os pontos e os nomes.</summary>
        void BuildMapa(Sim s)
        {
            Image mapa = Art.Panel(_rotasLista, "Mapa", Art.Board, Vector2.zero, Vector2.one);
            LayoutElement e = mapa.gameObject.AddComponent<LayoutElement>();
            e.minHeight = e.preferredHeight = AltMapa;
            Transform m = mapa.transform;
            var c = new Vector2(0.5f, 0.5f);
            _destino = new Destino[s.T.Rotas.Length];
            for (int r = 0; r < _destino.Length; r++)
            {
                Vector2 p = NoMapa(s.T.Rotas, s.T.Aeroportos, r);
                var d = new Destino { Traco = Art.Img(m, "Linha" + r, Art.Square(), Art.Placa, c, c) };
                d.Traco.preserveAspect = false;
                RectTransform t = d.Traco.rectTransform;
                t.pivot = new Vector2(0f, 0.5f);
                t.anchoredPosition = p.normalized * 64f;   // da borda do aeroporto a borda do ponto
                t.sizeDelta = new Vector2(Mathf.Max(0f, p.magnitude - 64f - 28f), 6f);
                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg);
                _destino[r] = d;
            }
            _mapaAqui = Art.Img(m, "Aqui", Art.Ring(), Art.Placa, c, c);
            Art.Fixa(_mapaAqui.rectTransform, c, Vector2.zero, new Vector2(140f, 140f));
            _mapaHub = Art.Img(m, "Aeroporto", Art.Disc(), Art.Azul, c, c);
            Art.Fixa(_mapaHub.rectTransform, c, Vector2.zero, new Vector2(112f, 112f));
            Art.Img(_mapaHub.transform, "Glifo", Art.Aviao(), Color.white, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            for (int r = 0; r < _destino.Length; r++)
            {
                Vector2 p = NoMapa(s.T.Rotas, s.T.Aeroportos, r);
                Destino d = _destino[r];
                d.Anel = Art.Img(m, "Anel" + r, Art.Ring(), Color.white, c, c);
                Art.Fixa(d.Anel.rectTransform, c, p, new Vector2(58f, 58f));
                d.Ponto = Art.Img(m, "Ponto" + r, Art.Disc(), Color.white, c, c);
                Art.Fixa(d.Ponto.rectTransform, c, p, new Vector2(44f, 44f));
                d.Glifo = Art.Img(d.Ponto.transform, "Glifo", Art.Cadeado(), Art.Dim, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
                d.Nome = Art.Escreve(m, "Nome" + r, Tipo.Legenda, c, c, Alinha(p));
                d.Nome.font = Art.Titulo(); d.Nome.resizeTextMinSize = MenorTexto; d.Nome.text = Textos.Destino[r];
                Rect q = RotuloNoMapa(p, LargRotuloMapa);
                d.Nome.rectTransform.pivot = Vector2.zero;
                d.Nome.rectTransform.anchoredPosition = q.position; d.Nome.rectTransform.sizeDelta = q.size;
            }
            _mapaTitulo = NoMapaTexto(m, "Titulo", Tipo.Secao, ReservaMapa[0], TextAnchor.MiddleLeft, Art.Placa);
            _mapaEstado = NoMapaTexto(m, "Estado", Tipo.Legenda, ReservaMapa[1], TextAnchor.MiddleLeft, Art.Ink);
            Rect leg = ReservaMapa[2];
            for (int f = 0; f < 2; f++)
            {
                float x = leg.x + f * 176f;
                Art.Fixa(Art.Img(m, "Legenda" + f, Art.Mala(), Art.VooCor[f], c, c).rectTransform, c, new Vector2(x + 18f, leg.center.y), new Vector2(36f, 36f));
                NoMapaTexto(m, "Vaga" + f, Tipo.Legenda, new Rect(x + 44f, leg.y, 110f, leg.height), TextAnchor.MiddleLeft, Art.Ink).text = Textos.Vaga(f);
            }
        }

        static Text NoMapaTexto(Transform m, string nome, Tipo tipo, Rect q, TextAnchor alinha, Color cor)
        {
            var c = new Vector2(0.5f, 0.5f);
            Text t = Art.Escreve(m, nome, tipo, c, c, alinha);
            t.rectTransform.pivot = Vector2.zero;
            t.rectTransform.anchoredPosition = q.position; t.rectTransform.sizeDelta = q.size;
            t.color = cor; t.resizeTextMinSize = MenorTexto;
            return t;
        }

        /// <summary>As 2 vagas do quadro, com a cor do voo: a escolhida recebe a rota tocada embaixo, e vale na proxima onda.</summary>
        void BuildVagas()
        {
            _vagas = Art.NovoCartao(_rotasLista, "Vagas", 268f);
            Text t = Art.Escreve(_vagas, "Titulo", Tipo.Nome, Vector2.up, Vector2.one);
            t.rectTransform.offsetMin = new Vector2(32f, -76f); t.rectTransform.offsetMax = new Vector2(-32f, -16f);
            t.text = Textos.ProximaOnda;
            for (int f = 0; f < 2; f++)
            {
                int ff = f;
                Button b = Art.NewButton(_vagas, Textos.Vaga(f), 40, Art.Suave, new Vector2(f * 0.5f, 1f), new Vector2(0.5f + f * 0.5f, 1f), () => _rotasVaga = ff);
                var r = (RectTransform)b.transform;
                r.offsetMin = new Vector2(f == 0 ? 28f : 8f, -244f); r.offsetMax = new Vector2(f == 0 ? -8f : -28f, -88f);
                Art.Fixa(Art.Img(b.transform, "Chip", Art.Mala(), Art.VooCor[f], Vector2.zero, Vector2.zero).rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(60f, 60f));
                Text nome = Art.Rotulo(b);
                nome.alignment = TextAnchor.MiddleLeft; nome.font = Art.Titulo(); nome.resizeTextMinSize = MenorTexto;
                nome.rectTransform.anchorMin = new Vector2(0f, 0.5f); nome.rectTransform.anchorMax = Vector2.one;
                Art.Margem(nome.rectTransform, 98f, 0f, 16f, 8f);
                _vagaDestino[f] = Art.Escreve(b.transform, "Destino", Tipo.Legenda, Vector2.zero, new Vector2(1f, 0.5f));
                Art.Margem(_vagaDestino[f].rectTransform, 98f, 10f, 16f, 0f);
                _vagaDestino[f].resizeTextMinSize = MenorTexto;
                _vagaBtn[f] = b;
            }
        }

        /// <summary>Ao lado da faixa da meta: placa na cor do aeroporto, "Rotas" e o nome do aeroporto. Toque abre a folha.</summary>
        void BuildAtalhoRotas()
        {
            _atalhoRotas = Art.NovoCartao(_root, "AtalhoRotas", AltAtalhoRotas);
            Art.Fixa(_atalhoRotas, Vector2.zero, new Vector2(32f + LargFaixaMeta, AltBase + 14f), new Vector2(LargAtalhoRotas, AltAtalhoRotas));
            Image fundo = _atalhoRotas.GetComponent<Image>();
            fundo.raycastTarget = true;   // o toque no atalho nao vai para o mundo
            Button b = _atalhoRotas.gameObject.AddComponent<Button>();
            b.targetGraphic = fundo;
            b.onClick.AddListener(() => AbreTela("rotas"));
            _atalhoRotas.gameObject.AddComponent<Aperta>();
            _atalhoRotasPlaca = Art.NovaPlaca(_atalhoRotas, "Placa", Art.Aviao(), Art.Azul, Color.white);
            Art.Fixa(_atalhoRotasPlaca.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(72f, 72f));
            Text t = EscreveMeta(_atalhoRotas, "Nome", Tipo.Nome);
            NaFaixa(t.rectTransform, 96f, 18f, 10f, 56f);
            t.text = Textos.Rotas;
            _atalhoRotasAero = EscreveMeta(_atalhoRotas, "Aeroporto", Tipo.Legenda);
            NaFaixa(_atalhoRotasAero.rectTransform, 96f, 76f, 10f, 48f);
            _atalhoRotasAero.font = Art.Titulo();
            _seloRotas = NovoSelo(_atalhoRotas, out _seloRotasN);
        }

        // ------------------------------------------------------------------ abrir e tocar

        /// <summary>-tela rotas abre no aeroporto em que o jogador esta; rotas0, rotas1 e rotas2 abrem na aba daquele aeroporto (as fotos).</summary>
        void AbreRotas(string nome)
        {
            Abre(_rotas);
            int a = nome.Length > 5 && int.TryParse(nome.Substring(5), out int k) ? k : S.AeroportoAtual;
            MostraAero(Mathf.Clamp(a, 0, _rotasAba.Length - 1));
        }

        void MostraAero(int a)
        {
            _rotasAero = a;
            for (int r = 0; r < _cartaoRota.Length; r++)
            {
                bool deste = S.T.Rotas[r].Aeroporto == a;
                Destino d = _destino[r];
                _cartaoRota[r].It.Rt.gameObject.SetActive(deste);
                d.Traco.gameObject.SetActive(deste); d.Anel.gameObject.SetActive(deste); d.Ponto.gameObject.SetActive(deste); d.Nome.gameObject.SetActive(deste);
            }
            _rotasLista.anchoredPosition = Vector2.zero;
            PintaRotasTela(S);
        }

        void TocaRota(int r)
        {
            switch (EstadoDaRota(S, r, _rotasVaga))
            {
                case EstadoRota.Comprar: _g.AbreRota(r); break;
                case EstadoRota.Programar: _g.Programa(_rotasVaga, r); break;
            }
        }

        // ------------------------------------------------------------------ quadro a quadro

        void PintaRotas(Sim s)
        {
            bool mostra = !TourAberto && !Arrastando;   // como a faixa da meta: no arrasto, nao tapa a estacao de destino
            _atalhoRotas.gameObject.SetActive(mostra);
            if (mostra)
            {
                int a = Mathf.Clamp(s.AeroportoAtual, 0, AeroCor.Length - 1), novas = Novidades(s) & ~_rotasVistas;
                _atalhoRotasPlaca.color = AeroCor[a];
                _atalhoRotasAero.text = Textos.AeroportoNome[a];
                _seloRotas.gameObject.SetActive(novas != 0);
                _seloRotasN.text = Bits(novas).ToString();
            }
            if (_rotas.Rt.gameObject.activeSelf) PintaRotasTela(s);
        }

        void PintaRotasTela(Sim s)
        {
            _rotasVistas |= Novidades(s);
            int a = _rotasAero;
            Aeroporto ap = s.T.Aeroportos[a];
            Color cor = AeroCor[a % AeroCor.Length];
            bool aqui = a == s.AeroportoAtual;
            for (int i = 0; i < _rotasAba.Length; i++)
            {
                bool sel = i == a;
                ((Image)_rotasAba[i].targetGraphic).color = sel ? AeroCor[i % AeroCor.Length] : Art.Suave;
                Text t = Art.Rotulo(_rotasAba[i]);
                t.color = sel ? Color.white : Art.Azul;
                t.GetComponent<Outline>().enabled = sel;
                bool tranca = s.Marco < s.T.Aeroportos[i].Marco;
                _rotasTranca[i].gameObject.SetActive(tranca);
                t.rectTransform.offsetMin = new Vector2(tranca ? 52f : 12f, 14f);   // o nome nao passa por cima do cadeado
            }
            _ritmo.Valor.text = TemPico(s.T.Rotas, a) ? Textos.EmPicos : Textos.Estavel;
            _desgaste.Valor.text = Textos.Fator(ap.Desgaste);
            _incidentes.Valor.text = Textos.Pct(ap.Incidentes);

            PintaViagem(s, a, cor);
            _mapaTitulo.text = Textos.AeroportoNome[a].ToUpperInvariant();
            _mapaEstado.text = _viagem.Linha.text;
            _mapaHub.color = cor;
            _mapaAqui.enabled = aqui;

            _vagas.gameObject.SetActive(aqui);   // programar so vale no aeroporto em que o jogador esta
            for (int f = 0; f < 2; f++)
            {
                bool sel = f == _rotasVaga;
                ((Image)_vagaBtn[f].targetGraphic).color = sel ? Art.Azul : Art.Suave;
                Text t = Art.Rotulo(_vagaBtn[f]);
                t.color = sel ? Color.white : Art.Azul;
                t.GetComponent<Outline>().enabled = sel;
                _vagaDestino[f].text = Textos.ParaDestino(Textos.Destino[s.Prog[f] % Textos.Destino.Length]);
                _vagaDestino[f].color = sel ? Art.Placa : Art.Fraco;
            }

            for (int r = 0; r < _cartaoRota.Length; r++)
            {
                Rota x = s.T.Rotas[r];
                if (x.Aeroporto != a) continue;
                bool aberta = s.RotaAberta(r), vaga0 = aqui && s.Prog[0] == r, vaga1 = aqui && s.Prog[1] == r;
                CartaoRota c = _cartaoRota[r];
                Item it = c.It;
                it.Placa.color = aberta ? CorRota(x.Tipo) : Art.Vazio;
                it.Nome.color = aberta ? Art.Tinta : Art.Fraco;
                c.Chip[0].enabled = vaga0; c.Chip[1].enabled = vaga1;
                c.Destaque.enabled = aqui && s.Prog[_rotasVaga] == r;
                switch (EstadoDaRota(s, r, _rotasVaga))
                {
                    case EstadoRota.NaVaga: it.Btn.Maximo(Textos.Vaga(_rotasVaga)); break;
                    case EstadoRota.Programar: it.Btn.Secundario(Textos.Programar); break;
                    case EstadoRota.Aberta: it.Btn.Maximo(Textos.Aberta); break;
                    case EstadoRota.Comprar: it.Btn.Preco(Textos.Abrir, s.CustoRota(r), s.Cash); break;
                    case EstadoRota.Terminal: it.Btn.Travado(Textos.TerminalNome, Textos.NivelNome(x.Marco + 1)); break;
                    default: it.Btn.Travado(Textos.ViajeAntes, ""); break;
                }

                Destino d = _destino[r];
                d.Ponto.color = !aberta ? Art.Off : vaga0 ? Art.VooCor[0] : vaga1 ? Art.VooCor[1] : CorRota(x.Tipo);
                d.Glifo.enabled = !aberta || vaga0 || vaga1;
                d.Glifo.sprite = aberta ? Art.Mala() : Art.Cadeado();
                d.Glifo.color = aberta ? Art.Tinta : Art.Dim;
                d.Anel.color = vaga0 && vaga1 ? Art.VooCor[1] : aberta ? Color.white : Art.Dim;
                d.Traco.color = aberta ? Art.A(Art.Placa, 0.85f) : Art.A(Art.Dim, 0.35f);
                d.Traco.rectTransform.sizeDelta = new Vector2(d.Traco.rectTransform.sizeDelta.x, aberta ? 6f : 4f);
                d.Nome.color = aberta ? Art.Placa : Art.Dim;
            }
        }

        void PintaViagem(Sim s, int a, Color cor)
        {
            Item it = _viagem;
            it.Nome.text = Textos.AeroportoNome[a];
            it.Placa.color = cor; it.Nome.color = Art.Tinta;
            switch (EstadoDaViagem(s, a))
            {
                case EstadoViagem.Aqui: it.Linha.text = Textos.VoceEstaAqui; it.Btn.Maximo(Textos.Aqui); break;
                case EstadoViagem.Marcada: it.Linha.text = Textos.ChegaNaProxima; it.Btn.Maximo(Textos.Marcada); break;
                case EstadoViagem.Espere: it.Linha.text = Textos.ViagemMarcada(s.ProximoAeroporto); it.Btn.Travado(Textos.Espere, ""); break;
                case EstadoViagem.Terminal: PintaTrancada(it, s.T.Aeroportos[a].Marco, s); break;
                case EstadoViagem.Voltar: it.Linha.text = Textos.VoltarGratis; it.Btn.Secundario(Textos.Viajar); break;
                default: it.Linha.text = Textos.PagaUmaVez; it.Btn.Preco(Textos.Viajar, s.CustoViagem(a), s.Cash); break;
            }
        }
    }
}
