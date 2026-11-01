using System;
using TA.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// HUD em uGUI por codigo (padrao do dc:Scripts/View/Hud.cs, referencia 1080x1920, dentro da area segura), em faixas de altura fixa por cima do
    /// Mundo (o aeroporto com os bonecos, Mundo.cs): tela mais alta so da mais mundo. No alto, a barra (Cash, relogio, onda, nivel, pausa) e o quadro
    /// de partidas (TA-1: voo, destino, hora de fechamento e situacao, com a cor do voo que esta na mala dos passageiros, o a bordo, o que pede
    /// atencao e a barra do tempo que falta; cada linha e um botao que abre o voo). Embaixo, os atalhos (contratar ou equipe, melhorias, patio,
    /// tripulacao, 2x renda). So le o Sim e manda intencao pelo Game. Telas modais (Hud.Telas.cs e Hud.Operacao.cs) e tutorial (Tour.cs) seguram
    /// o relogio (Bloqueia).
    /// </summary>
    public sealed partial class Hud : MonoBehaviour
    {
        public const float RefW = 1080f, RefH = 1920f;
        /// <summary>Alturas em px da referencia de 1080. O mundo aparece no que sobra da area segura entre AltCima e AltBase.</summary>
        const float AltBarra = 112f, AltLinha = 108f, AltTituloQuadro = 44f, AltQuadro = AltTituloQuadro + 2f * AltLinha + 14f, AltFio = 6f,
            AltCima = AltBarra + AltQuadro + 10f + AltFio, AltAtalho = 148f, AltBase = AltAtalho + 16f + AltFio, LargAtalho = 207f;
        /// <summary>Colunas do quadro, em px; o teste confere que o nome mais longo cabe.</summary>
        public const float LargDestino = 288f, LargSituacao = 338f, LargNota = 636f;
        /// <summary>Piso de texto (ART_BIBLE §5): o best fit do NewText desce ate 2/3 do tamanho; aqui, so ate o piso.</summary>
        public const int MenorTexto = 32;

        Game _g;
        Sim S => _g.Sim;
        RectTransform _canvas, _root, _quadro, _toastFundo;
        Text _cash, _relogio, _onda, _toast, _aviso;
        float _toastT;
        BotaoPreco _contratar;
        Button _equipeAtalho, _melhoriasBtn, _receita;
        readonly Linha[] _linhas = new Linha[2];

        /// <summary>O aeroporto com os bonecos.</summary>
        public Mundo M { get; private set; }

        sealed class Linha
        {
            public Image StatusIcone, NotaIcone, Barra;
            public Text Voo, Destino, Fecha, Status, ABordo, Nota;
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
            BuildBase();
            BuildAvisos();
            BuildTelas(_canvas);
            BuildOperacao(_canvas);
            BuildTour(_canvas);
        }

        /// <summary>Texto em px, preso ao canto superior esquerdo do pai.</summary>
        static Text Texto(Transform pai, string nome, Tipo tipo, Color cor, float x, float y, float l, float a, TextAnchor alinha = TextAnchor.MiddleLeft)
        {
            Text t = Art.Escreve(pai, nome, tipo, Vector2.up, Vector2.up, alinha);
            Art.Fixa(t.rectTransform, Vector2.up, new Vector2(x, -y), new Vector2(l, a));
            t.color = cor; t.resizeTextMinSize = MenorTexto;
            return t;
        }

        static Image Icone(Transform pai, string nome, Sprite s, Color cor, float x, float y, float lado)
        {
            Image i = Art.Img(pai, nome, s, cor, Vector2.up, Vector2.up);
            Art.Fixa(i.rectTransform, Vector2.up, new Vector2(x, -y), new Vector2(lado, lado));
            return i;
        }

        /// <summary>Fundo da faixa, passando da area segura para o jogo nao aparecer atras do notch nem da barra de gestos; o fio amarelo e o da Folha.</summary>
        static void Faixa(RectTransform faixa, float passaCima, float passaBaixo, Vector2 fioMin, Vector2 fioMax)
        {
            Image f = Art.Img(faixa, "Fundo", Art.Square(), Art.Azul, Vector2.zero, Vector2.one);
            f.preserveAspect = false; f.raycastTarget = true;   // o toque na faixa nao vai para o mundo
            f.rectTransform.offsetMin = new Vector2(0f, -passaBaixo); f.rectTransform.offsetMax = new Vector2(0f, passaCima);
            Image fio = Art.Img(faixa, "Fio", Art.Square(), Art.Placa, fioMin, fioMax);
            fio.preserveAspect = false;
            if (fioMin.y > 0f) fio.rectTransform.offsetMin = new Vector2(0f, -AltFio); else fio.rectTransform.offsetMax = new Vector2(0f, AltFio);
        }

        void BuildTopo()
        {
            RectTransform topo = Art.Node(_root, "Topo", Vector2.up, Vector2.one);
            topo.offsetMin = new Vector2(0f, -AltCima);
            Faixa(topo, 300f, 0f, Vector2.zero, Vector2.right);
            Icone(topo, "Moeda", Art.Moeda(), Color.white, 20f, 16f, 80f);
            _cash = Texto(topo, "Cash", Tipo.Titulo, Color.white, 112f, 12f, 370f, 88f);
            Icone(topo, "IconeRelogio", Art.Relogio(), Art.Placa, 498f, 24f, 64f);
            _relogio = Texto(topo, "Relogio", Tipo.Secao, Color.white, 576f, 6f, 360f, 56f);
            _onda = Texto(topo, "Onda", Tipo.Legenda, Art.Suave, 576f, 60f, 360f, 46f);
            Button pausa = Art.NewButton(topo, Textos.Pausa, 44, Art.AzulLuz, Vector2.one, Vector2.one, () => AbreTela("pausa"));
            Art.Fixa((RectTransform)pausa.transform, Vector2.one, new Vector2(-20f, 0f), new Vector2(112f, 112f));
            Art.Toque(pausa, 18f, 18f);
            BuildQuadro(topo);
        }

        /// <summary>Painel de aeroporto: tinta amarela sobre preto, uma coluna por dado. Abaixo de cada voo, o a bordo e o que importa saber agora.</summary>
        void BuildQuadro(RectTransform topo)
        {
            _quadro = Art.Panel(topo, "Quadro", Art.Board, Vector2.up, Vector2.one).rectTransform;
            _quadro.offsetMin = new Vector2(10f, -AltBarra - AltQuadro); _quadro.offsetMax = new Vector2(-10f, -AltBarra);
            Icone(_quadro, "Aviao", Art.Aviao(), Art.Placa, 26f, 3f, 38f);
            Coluna(Textos.Quadro, Art.Placa, 80f, 300f);
            Coluna(Textos.ColunaFecha, Art.Dim, 520f, 120f);
            Coluna(Textos.ColunaSituacao, Art.Dim, 650f, 300f);
            for (int f = 0; f < 2; f++)
            {
                int ff = f;
                // o botao do voo e metade do quadro, com a linha dentro: 144 px ou mais de alvo, passando um pouco do quadro no voo de baixo
                float corte = AltTituloQuadro + AltLinha + 3f, y0 = f * corte, y1 = f == 0 ? corte : corte + 144f;
                RectTransform toque = Art.Node(_quadro, "Toque" + f, Vector2.up, Vector2.one);
                toque.offsetMin = new Vector2(0f, -y1); toque.offsetMax = new Vector2(0f, -y0);
                toque.gameObject.AddComponent<Image>().color = Color.clear;
                Button b = toque.gameObject.AddComponent<Button>();
                b.onClick.AddListener(() => AbreVoo(ff));
                toque.gameObject.AddComponent<Aperta>();
                Image fundo = Art.Panel(toque, "Voo", Art.A(Color.white, 0.07f), Vector2.up, Vector2.one);
                fundo.raycastTarget = false;
                b.targetGraphic = fundo;
                RectTransform r = fundo.rectTransform;
                float y = AltTituloQuadro + f * (AltLinha + 6f) - y0;
                r.offsetMin = new Vector2(10f, -y - AltLinha); r.offsetMax = new Vector2(-10f, -y);
                var l = new Linha();
                Icone(r, "Chip", Art.Mala(), Art.VooCor[f], 14f, 6f, 46f);
                l.Voo = Texto(r, "Voo", Tipo.Nome, Art.Placa, 70f, 2f, 150f, 52f);
                l.Destino = Texto(r, "Destino", Tipo.Nome, Art.Placa, 222f, 2f, LargDestino, 52f);
                l.Fecha = Texto(r, "Fecha", Tipo.Valor, Art.Placa, 510f, 2f, 114f, 52f);
                l.StatusIcone = Icone(r, "IconeStatus", Art.Bilhete(), Art.Ink, 640f, 8f, 40f);
                l.Status = Texto(r, "Status", Tipo.Corpo, Art.Ink, 688f, 2f, LargSituacao, 52f);
                l.Status.font = Art.Titulo();
                Icone(r, "Pessoas", Art.Pessoas(), Art.Ink, 72f, 56f, 34f);
                l.ABordo = Texto(r, "ABordo", Tipo.Legenda, Art.Ink, 114f, 50f, 226f, 46f);
                l.NotaIcone = Icone(r, "IconeNota", Art.Pessoa(), Art.Dim, 346f, 56f, 34f);
                l.Nota = Texto(r, "Nota", Tipo.Legenda, Art.Dim, 390f, 50f, LargNota, 46f);
                Image trilho = Art.Panel(r, "BarraFundo", Art.A(Art.Ink, 0.14f), Vector2.zero, Vector2.right);
                trilho.raycastTarget = false;
                trilho.rectTransform.offsetMin = new Vector2(14f, 6f); trilho.rectTransform.offsetMax = new Vector2(-14f, 14f);
                l.Barra = Art.Panel(trilho.transform, "Barra", Art.Good, Vector2.zero, Vector2.one);
                l.Barra.raycastTarget = false;
                _linhas[f] = l;
            }
        }

        void Coluna(string texto, Color cor, float x, float l)
        {
            Text t = Texto(_quadro, "Coluna", Tipo.Legenda, cor, x, 0f, l, AltTituloQuadro);
            t.font = Art.Titulo(); t.text = texto;
        }

        /// <summary>Atalho da barra de baixo, na vaga i: icone em cima, rotulo embaixo. Claro = gratis; compra e video tem cor propria (ART_BIBLE §5).</summary>
        Button NovoAtalho(Transform b, int i, Sprite icone, Color face, Color tinta, string rotulo, Action acao)
        {
            Button x = Art.NewButton(b, rotulo, MenorTexto, face, Vector2.zero, Vector2.zero, acao);
            NaVaga(x.transform, i);
            Text t = Art.Rotulo(x);
            t.color = tinta; t.GetComponent<Outline>().enabled = false;
            Art.Margem(t.rectTransform, 8f, 14f, 8f, 88f);
            Art.Fixa(Art.Img(x.transform, "Icone", icone, tinta, Vector2.zero, Vector2.zero).rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(68f, 68f));
            return x;
        }

        static void NaVaga(Transform t, int i) => Art.Fixa((RectTransform)t, Vector2.zero, new Vector2(10f + i * (LargAtalho + 6f), 10f), new Vector2(LargAtalho, AltAtalho));

        void BuildBase()
        {
            RectTransform b = Art.Node(_root, "Base", Vector2.zero, Vector2.right);
            b.offsetMax = new Vector2(0f, AltBase);
            Faixa(b, 0f, 300f, Vector2.up, Vector2.one);
            _contratar = Art.NovoBotaoPreco(b, Vector2.zero, Vector2.zero, () => _g.Hire());
            NaVaga(_contratar.B.transform, 0);
            _contratar.Rotulo.resizeTextMinSize = MenorTexto;
            _equipeAtalho = NovoAtalho(b, 0, Art.Pessoas(), Art.Suave, Art.Azul, Textos.Equipe, () => AbreTela("equipe"));
            _melhoriasBtn = NovoAtalho(b, 1, Art.Raio(), Art.Suave, Art.Azul, Textos.Melhorias, () => AbreTela("melhorias"));
            NovoAtalho(b, 2, Art.Caminhao(), Art.Suave, Art.Azul, Textos.Patio, () => AbreTela("patio"));
            NovoAtalho(b, 3, Art.Aviao(), Art.Suave, Art.Azul, Textos.Tripulacao, () => AbreTela("tripulacao"));
            _receita = NovoAtalho(b, 4, Art.Moeda(), Art.Video, Color.white, "", () => _g.VerAnuncio(Anuncio.Receita));
            ((RectTransform)_receita.transform.Find("Icone")).anchoredPosition = new Vector2(-40f, -14f);
            Art.Outlined(Texto(_receita.transform, "Dobro", Tipo.Secao, Color.white, 108f, 20f, 90f, 56f), 2f).text = Textos.Dobro;
        }

        /// <summary>Logo abaixo do quadro, por cima do mundo: nunca em cima de um dado, e sem aviso nao aparece nada.</summary>
        void BuildAvisos()
        {
            Image f = Art.Panel(_root, "Toast", Art.A(Art.Tinta, 0.9f), Vector2.up, Vector2.one);
            f.raycastTarget = false;
            _toastFundo = f.rectTransform;
            _toastFundo.offsetMin = new Vector2(20f, -AltCima - 100f); _toastFundo.offsetMax = new Vector2(-20f, -AltCima - 12f);
            _toast = Art.Escreve(_toastFundo, "Texto", Tipo.Corpo, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            _toast.color = Art.Placa; _toast.resizeTextMinSize = MenorTexto;
            Art.Margem(_toast.rectTransform, 20f, 0f, 20f, 0f);
            _toastFundo.gameObject.SetActive(false);
            _aviso = Art.Outlined(Texto(_root, "Aviso", Tipo.Legenda, Art.Bad, 20f, AltCima + 108f, 1040f, 96f, TextAnchor.UpperCenter));
            _aviso.text = S.SaveRefused ? Textos.SaveRecusado : "";
        }

        /// <summary>Antes do 1o marco o botao contrata direto (o passo do tutorial); depois de o marco abrir perfil novo, ou com os 3 da v0.1, abre a Equipe.</summary>
        bool EquipeNoBotao(Sim s) => s.CustoContratar() < 0 || s.Marco >= 1;

        // ------------------------------------------------------------------ quadro a quadro

        void LateUpdate()
        {
            if (_g == null || _g.Sim == null) return;
            Sim s = S;
            M.Atualiza(_g.DtJogo, RegiaoMundo());
            _cash.text = Textos.Num(s.Cash);
            _relogio.text = Textos.Hora(s.Time);
            _onda.text = Textos.Onda(Math.Max(0, s.Onda) + 1) + " · " + Textos.NivelNome(s.Marco + 1);
            for (int f = 0; f < 2; f++) PintaLinha(_linhas[f], s.Voos[f], s);
            bool equipe = EquipeNoBotao(s);
            _equipeAtalho.gameObject.SetActive(equipe);
            _contratar.B.gameObject.SetActive(!equipe);
            if (!equipe) _contratar.Preco(Textos.ContratarCurto, s.CustoContratar(), s.Cash);
            bool boost = s.BoostLeft > 0;
            _receita.interactable = !boost && s.Anuncios.Pode(Anuncio.Receita) && Anuncios.Veto(Arrastando, TourAberto, ModalAberto) == null;
            Art.Rotulo(_receita).text = boost ? Textos.Tempo(s.BoostLeft / (float)Sim.Hz) : Textos.Receita2x(s.Anuncios.HojeDe(Anuncio.Receita), s.T.BoostPorDia);
            if (_toastT > 0f && (_toastT -= Time.unscaledDeltaTime) <= 0f) _toastFundo.gameObject.SetActive(false);
            PintaTelas();
            PintaTour();
        }

        /// <summary>Faixa da tela (px) entre a barra de baixo e o quadro, dentro da area segura.</summary>
        Rect RegiaoMundo()
        {
            Rect r = NaTela(_root);
            float px = r.height / _root.rect.height;   // px de tela por px da referencia
            return Rect.MinMaxRect(r.xMin, r.yMin + AltBase * px, r.xMax, r.yMax - AltCima * px);
        }

        void PintaLinha(Linha l, Voo v, Sim s)
        {
            if (v == null) return;
            double fecha = v.Onda * (double)s.T.Onda + v.FechaPasso * (double)Sim.Step;
            l.Voo.text = Textos.Voo(v.Id);
            l.Destino.text = Textos.DestinoDe(v.Id);
            l.Fecha.text = Textos.Hora(fecha);
            TA.Core.Aviao a = s.Avioes[v.Indice];
            l.Status.text = Textos.Status(v, a, s.PassoNaOnda);
            (Sprite icone, Color cor) = Situacao(v, a, s.PassoNaOnda);
            l.StatusIcone.sprite = icone;
            l.StatusIcone.color = l.Status.color = cor;
            l.ABordo.text = Textos.ABordo(v);
            float falta = v.Partiu ? 0f : Mathf.Clamp01((v.FechaPasso - s.PassoNaOnda) / (float)v.FechaPasso);
            l.Barra.rectTransform.anchorMax = new Vector2(falta, 1f);
            l.Barra.color = falta < 0.2f ? Art.Bad : falta < 0.45f ? Art.Warn : Art.Good;

            // uma nota so: o que pede acao agora, depois o que e bom saber
            if (falta > 0f && falta < 0.2f && v.Embarcados < v.Total) Nota(l, Art.Relogio(), Art.Bad, Art.Bad, Textos.FechaEm(Mathf.CeilToInt((v.FechaPasso - s.PassoNaOnda) * Sim.Step)));
            else if (a.Desgaste >= s.T.DesgasteAviso) Nota(l, Art.Chave(), CorDesgaste(a, s), CorDesgaste(a, s), Textos.Desgaste(a.Desgaste));
            else if (HistoriaNoAeroporto(s, v)) Nota(l, Art.Heart(), Art.Bad, Art.Paper, Textos.Historia[0]);
            else
            {
                string piloto = "", comissario = "";
                foreach (Tripulante t in s.Tripulacao)
                    if (t.Aviao == a.Id) { if (t.Funcao == Funcao.Piloto) piloto = t.Nome; else comissario = t.Nome; }
                Nota(l, Art.Pessoa(), Art.Dim, Art.Dim, Textos.Equipagem(piloto, comissario));
            }
        }

        static void Nota(Linha l, Sprite icone, Color corIcone, Color cor, string texto)
        {
            l.NotaIcone.sprite = icone; l.NotaIcone.color = corIcone;
            l.Nota.text = texto; l.Nota.color = cor;
        }

        /// <summary>Icone e cor do estado do voo, na ordem de Textos.Status: o estado nunca e so cor. Vermelho so no que pede acao agora.</summary>
        public static (Sprite icone, Color cor) Situacao(Voo v, TA.Core.Aviao a, int passoNaOnda)
        {
            if (v.Partiu) return v.Perdidos == 0 ? (Art.Check(), Art.Dim) : (Art.Alerta(), Art.Warn);
            if (v.Embarque) return (Art.Pessoas(), Art.Good);
            if (passoNaOnda < v.AbrePasso) return (Art.Bilhete(), Art.Ink);
            if (a.Hangar > 0) return (Art.Chave(), Art.Warn);
            if (!a.NoPatio) return (Art.Aviao(), Art.Ink);
            if (a.Pendente) return (Art.Alerta(), Art.Bad);
            return a.Check > 0 ? (Art.Chave(), Art.Warn) : (Art.Caminhao(), Art.Ink);
        }

        /// <summary>O passageiro com historia deste voo ainda esta no aeroporto.</summary>
        static bool HistoriaNoAeroporto(Sim s, Voo v)
        {
            if (v.Partiu) return false;
            foreach (Station st in s.Estacoes)
            {
                foreach (Pax p in st.Fila) if (p.Historia && p.Voo == v.Indice) return true;
                foreach (Pax p in st.Atendendo) if (p.Historia && p.Voo == v.Indice) return true;
            }
            return false;
        }

        public void Toast(string t)
        {
            _toast.text = t;
            _toastFundo.gameObject.SetActive(true);
            _toastT = 3f;
        }

        internal RectTransform QuadroRt => _quadro;
        internal RectTransform ContratarRt => (RectTransform)_contratar.B.transform;
        internal RectTransform MelhoriasRt => (RectTransform)_melhoriasBtn.transform;
    }
}
