using System;
using TA.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// HUD em uGUI por codigo (padrao do dc:Scripts/View/Hud.cs, referencia 1080x1920, dentro da area segura), em faixas finas por cima do
    /// Mundo (o aeroporto com os bonecos, Mundo.cs): barra de cima (Cash, onda e hora, pausa), quadro de partidas (TA-1: voo, destino,
    /// hora de fechamento, estado, a bordo e a barra do tempo que falta, com a cor do voo que esta na mala dos passageiros), terminal
    /// (marco) e a barra de baixo (contratar, melhorias, 2x renda). So le o Sim e manda intencao pelo Game. Telas modais (Hud.Telas.cs) e
    /// tutorial (Tour.cs) seguram o relogio (Bloqueia).
    /// </summary>
    public sealed partial class Hud : MonoBehaviour
    {
        public const float RefW = 1080f, RefH = 1920f;
        /// <summary>Faixa da area segura (fracao da altura) onde o mundo aparece: entre a barra de baixo e o quadro.</summary>
        const float MundoY0 = 0.11f, MundoY1 = 0.795f;

        Game _g;
        Sim S => _g.Sim;
        RectTransform _canvas, _root;
        Text _cash, _relogio, _terminal, _toast, _aviso;
        float _toastT;
        Button _contratar, _melhoriasBtn, _receita;
        readonly Linha[] _linhas = new Linha[2];

        /// <summary>O aeroporto com os bonecos.</summary>
        public Mundo M { get; private set; }

        sealed class Linha
        {
            public Image Chip, Barra, BarraFundo, Coracao;
            public Text Voo, Fecha, Status, ABordo, Historia;
        }

        /// <summary>O relogio do jogo para: tela modal aberta, explicacao do tutorial ou anuncio.</summary>
        public bool Bloqueia => ModalAberto || TourSegura;
        public bool Arrastando => M != null && M.Arrastando >= 0;
        /// <summary>O Mundo aceita toque/arrasto: sem tela modal nem explicacao do tutorial (o veu do passo de acao ja segura o resto).</summary>
        public bool MundoAceita(Vector2 tela) => !ModalAberto && !TourSegura;
        /// <summary>Tutorial na tela: o mundo nao faz pan (o buraco do veu fica no lugar).</summary>
        public bool Tutorial => TourAberto;

        // ------------------------------------------------------------------ construcao

        public void Build(Game g)
        {
            _g = g;
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).GetComponent<EventSystem>();
                es.pixelDragThreshold = Mathf.RoundToInt(Dp.Px(10f));   // limiar de ARRASTO em dp, separado do toque (GDD §5.5)
            }
            var go = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(RefW, RefH);
            sc.matchWidthOrHeight = 0f;   // largura fixa: em tela 20:9 sobra altura para o mundo
            _canvas = (RectTransform)go.transform;
            _root = Art.Node(_canvas, "AreaSegura", Vector2.zero, Vector2.one);
            AreaSegura.AncorarDentro(_root);

            RectTransform rotulos = Art.Node(_root, "Rotulos", Vector2.zero, Vector2.one);   // nomes e estados em cima do mundo, embaixo das barras
            M = new Mundo(this, g, rotulos);
            BuildTopo();
            BuildQuadro();
            BuildBase();
            _terminal = Art.Outlined(Art.NewText(_root, "Terminal", 28, new Vector2(0.03f, 0.79f), new Vector2(0.97f, 0.81f), TextAnchor.MiddleRight), 2f);
            _terminal.color = Art.Dim;
            _toast = Art.Outlined(Art.NewText(_root, "Toast", 40, new Vector2(0.04f, 0.72f), new Vector2(0.96f, 0.77f)), 3f);
            _toast.color = Art.BoardInk;
            _aviso = Art.Outlined(Art.NewText(_root, "Aviso", 30, new Vector2(0.02f, 0.905f), new Vector2(0.98f, 0.94f)), 2f);
            _aviso.color = Art.Bad;
            _aviso.text = S.SaveRefused ? Textos.SaveRecusado : "";
            BuildTelas(_canvas);
            BuildTour(_canvas);
        }

        void BuildTopo()
        {
            RectTransform topo = Art.Panel(_root, "Topo", Art.Dark, new Vector2(0f, 0.94f), Vector2.one).rectTransform;
            Art.Img(topo, "Moeda", Art.Disc(), Art.Coin, new Vector2(0.02f, 0.15f), new Vector2(0.09f, 0.85f));
            _cash = Art.Outlined(Art.NewText(topo, "Cash", 54, new Vector2(0.1f, 0f), new Vector2(0.42f, 1f), TextAnchor.MiddleLeft));
            _relogio = Art.Outlined(Art.NewText(topo, "Relogio", 44, new Vector2(0.42f, 0f), new Vector2(0.84f, 1f)));
            Button pausa = Art.NewButton(topo, Textos.Pausa, 44, Art.Off, new Vector2(0.86f, 0.06f), new Vector2(0.98f, 0.94f), () => AbreTela("pausa"));
            Art.Toque(pausa, 20f, 20f);
        }

        void BuildQuadro()
        {
            RectTransform q = Art.Panel(_root, "Quadro", Art.Board, new Vector2(0.01f, 0.81f), new Vector2(0.99f, 0.937f)).rectTransform;
            Text t = Art.NewText(q, "Titulo", 30, new Vector2(0.03f, 0.84f), new Vector2(0.6f, 0.99f), TextAnchor.MiddleLeft);
            t.text = Textos.Quadro; t.color = Art.Dim;
            for (int f = 0; f < 2; f++)
            {
                float y1 = 0.84f - f * 0.42f, y0 = y1 - 0.42f;
                RectTransform r = Art.Node(q, "Voo" + f, new Vector2(0.02f, y0), new Vector2(0.98f, y1));
                var l = new Linha();
                l.Chip = Art.Img(r, "Chip", Art.Mala(), Art.VooCor[f], new Vector2(0f, 0.45f), new Vector2(0.05f, 0.98f));
                l.Voo = Art.NewText(r, "Voo", 36, new Vector2(0.06f, 0.5f), new Vector2(0.44f, 0.98f), TextAnchor.MiddleLeft);
                l.Fecha = Art.NewText(r, "Fecha", 34, new Vector2(0.44f, 0.5f), new Vector2(0.64f, 0.98f), TextAnchor.MiddleLeft);
                l.Status = Art.NewText(r, "Status", 32, new Vector2(0.64f, 0.5f), new Vector2(0.83f, 0.98f), TextAnchor.MiddleLeft);
                l.ABordo = Art.NewText(r, "ABordo", 32, new Vector2(0.83f, 0.5f), new Vector2(1f, 0.98f), TextAnchor.MiddleRight);
                foreach (Text x in new[] { l.Voo, l.Fecha, l.Status, l.ABordo }) x.color = Art.BoardInk;
                l.BarraFundo = Art.Img(r, "BarraFundo", Art.Square(), Art.A(Art.Ink, 0.12f), new Vector2(0.06f, 0.3f), new Vector2(1f, 0.44f));
                l.BarraFundo.preserveAspect = false;
                l.Barra = Art.Img(l.BarraFundo.transform, "Barra", Art.Square(), Art.Good, Vector2.zero, Vector2.one);
                l.Barra.preserveAspect = false;
                l.Coracao = Art.Img(r, "Coracao", Art.Heart(), Art.Bad, new Vector2(0.06f, 0f), new Vector2(0.09f, 0.28f));
                l.Historia = Art.NewText(r, "Historia", 26, new Vector2(0.1f, 0f), new Vector2(1f, 0.28f), TextAnchor.MiddleLeft);
                l.Historia.color = Art.Paper;
                _linhas[f] = l;
            }
        }

        void BuildBase()
        {
            RectTransform b = Art.Panel(_root, "Base", Art.Dark, Vector2.zero, new Vector2(1f, 0.105f)).rectTransform;
            _contratar = Art.NewButton(b, "", 40, Art.Good, new Vector2(0.02f, 0.12f), new Vector2(0.34f, 0.88f), () => _g.Hire());
            _melhoriasBtn = Art.NewButton(b, Textos.Melhorias, 40, Art.EstacaoCor[1], new Vector2(0.36f, 0.12f), new Vector2(0.64f, 0.88f), () => AbreTela("melhorias"));
            _receita = Art.NewButton(b, "", 34, Art.Coin, new Vector2(0.66f, 0.12f), new Vector2(0.98f, 0.88f), () => _g.VerAnuncio(Anuncio.Receita));
        }

        // ------------------------------------------------------------------ quadro a quadro

        void LateUpdate()
        {
            if (_g == null || _g.Sim == null) return;
            Sim s = S;
            M.Atualiza(_g.DtJogo, RegiaoMundo());
            _cash.text = Textos.Num(s.Cash);
            _relogio.text = Textos.Onda(Math.Max(0, s.Onda) + 1) + " · " + Textos.Hora(s.Time);
            _terminal.text = Textos.Terminal(s.Marco + 1);
            for (int f = 0; f < 2; f++) PintaLinha(_linhas[f], s.Voos[f], s);
            long c = s.CustoContratar();
            Art.Rotulo(_contratar).text = Textos.Contratar(c);
            _contratar.interactable = c >= 0 && s.Cash >= c;
            bool boost = s.BoostLeft > 0;
            _receita.gameObject.SetActive(boost || s.Anuncios.Pode(Anuncio.Receita));
            _receita.interactable = !boost && Anuncios.Veto(Arrastando, TourAberto, ModalAberto) == null;
            Art.Rotulo(_receita).text = boost ? Textos.Receita2xAtiva(s.BoostLeft / Sim.Hz) : Textos.Receita2x(s.Anuncios.HojeDe(Anuncio.Receita), s.T.BoostPorDia);
            if (_toastT > 0f && (_toastT -= Time.unscaledDeltaTime) <= 0f) _toast.text = "";
            PintaTelas();
            PintaTour();
        }

        /// <summary>Faixa da tela (px) entre a barra de baixo e o quadro, dentro da area segura.</summary>
        Rect RegiaoMundo()
        {
            Rect r = NaTela(_root);
            return Rect.MinMaxRect(r.xMin, r.yMin + r.height * MundoY0, r.xMax, r.yMin + r.height * MundoY1);
        }

        void PintaLinha(Linha l, Voo v, Sim s)
        {
            if (v == null) return;
            double fecha = v.Onda * (double)s.T.Onda + v.FechaPasso * (double)Sim.Step;
            l.Voo.text = Textos.Voo(v.Id) + " " + Textos.DestinoDe(v.Id);
            l.Fecha.text = Textos.Fecha(fecha);
            l.Status.text = Textos.Status(v);
            l.ABordo.text = Textos.ABordo(v);
            float falta = v.Partiu ? 0f : Mathf.Clamp01((v.FechaPasso - s.PassoNaOnda) / (float)v.FechaPasso);
            l.Barra.rectTransform.anchorMax = new Vector2(falta, 1f);
            l.Barra.color = v.Partiu ? Art.Off : falta < 0.2f ? Art.Bad : falta < 0.45f ? Art.Warn : Art.Good;
            bool h = HistoriaNoAeroporto(s, v);
            l.Coracao.enabled = h;
            l.Historia.text = h ? Textos.Historia[0] : "";
        }

        /// <summary>O passageiro com historia deste voo ainda esta no aeroporto.</summary>
        static bool HistoriaNoAeroporto(Sim s, Voo v)
        {
            if (v.Partiu) return false;
            foreach (Station st in s.Stations)
            {
                foreach (Pax p in st.Fila) if (p.Historia && p.Voo == v.Indice) return true;
                foreach (Pax p in st.Atendendo) if (p.Historia && p.Voo == v.Indice) return true;
            }
            return false;
        }

        public void Toast(string t)
        {
            _toast.text = t;
            _toastT = 3f;
        }

        internal RectTransform QuadroRt => (RectTransform)_linhas[0].Voo.transform.parent.parent;
        internal RectTransform ContratarRt => (RectTransform)_contratar.transform;
        internal RectTransform MelhoriasRt => (RectTransform)_melhoriasBtn.transform;
    }
}
