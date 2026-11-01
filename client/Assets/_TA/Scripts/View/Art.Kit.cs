using System;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>Hierarquia de texto: tamanho, fonte e cor saem daqui. Titulo, Secao, Nome e Valor em Lilita One; Corpo e Legenda em Nunito.</summary>
    public enum Tipo { Titulo, Secao, Nome, Valor, Corpo, Legenda }

    /// <summary>Folha modal: Corpo e a area clara onde a tela monta o conteudo.</summary>
    public sealed class Folha
    {
        public RectTransform Rt, Corpo;
        public Text Titulo;
        public float Altura;
        /// <summary>Chamar ao abrir: a altura pedida, sem passar da area segura.</summary>
        public void Ajusta() => Rt.sizeDelta = new Vector2(0f, Mathf.Min(Altura, ((RectTransform)Rt.parent).rect.height - 40f));
    }

    /// <summary>Rotulo em cima, valor embaixo e icone ao lado, num cartao.</summary>
    public sealed class Numero { public Text Rotulo, Valor; }

    /// <summary>Barra de nivel: um degrau por nivel, para ver de longe quanto falta.</summary>
    public sealed class BarraNivel
    {
        readonly Image[] _pip;
        public BarraNivel(Image[] pip) { _pip = pip; }
        public Transform Pai => _pip[0].transform.parent;
        public void Pinta(int nivel, Color cheia) { for (int i = 0; i < _pip.Length; i++) _pip[i].color = i < nivel ? cheia : Art.Vazio; }
    }

    /// <summary>
    /// Botao com os estados da interface: Preco (verde; sem Cash, cinza e dizendo quanto falta), Travado (cadeado e motivo), Maximo (check) e
    /// Secundario (acao gratis e discreta, como Mover). A tela escolhe o estado a cada quadro; a regra continua no Sim.
    /// </summary>
    public sealed class BotaoPreco
    {
        public Button B;
        public Text Rotulo, Valor;
        public Image Fundo, Icone;
        public RectTransform Linha;
        public Outline ContornoRotulo, ContornoValor;

        public void Preco(string rotulo, long custo, long cash)
        {
            bool pode = cash >= custo;
            Arruma(true, pode ? Art.Compra : Art.Apagado, Art.Moeda(), Color.white, pode, pode);
            Rotulo.text = pode ? rotulo : Textos.Faltam(custo - cash);
            Rotulo.color = pode ? Color.white : Art.Perigo;
            Valor.text = Textos.Num(custo);
            Valor.color = pode ? Color.white : Art.Tinta;
        }

        /// <summary>motivo em cima (ou o que falta), o alvo ao lado do cadeado.</summary>
        public void Travado(string motivo, string alvo)
        {
            Arruma(true, Art.Apagado, Art.Cadeado(), Art.Fraco, false, false);
            Rotulo.text = motivo; Rotulo.color = Art.Fraco;
            Valor.text = alvo; Valor.color = Art.Tinta;
        }

        public void Maximo(string texto)
        {
            Arruma(false, Art.Feito, Art.Check(), Art.FeitoTinta, false, false);
            Valor.text = texto; Valor.color = Art.FeitoTinta;
        }

        public void Secundario(string rotulo)
        {
            Arruma(false, Art.Suave, null, Color.white, false, true);
            Valor.text = rotulo; Valor.color = Art.Azul;
        }

        void Arruma(bool duasLinhas, Color fundo, Sprite icone, Color corIcone, bool contorno, bool ativo)
        {
            Fundo.color = fundo;
            B.interactable = ativo;
            Rotulo.gameObject.SetActive(duasLinhas);
            Linha.anchorMax = new Vector2(1f, duasLinhas ? 0.58f : 0.96f);
            Icone.gameObject.SetActive(icone != null);
            Icone.sprite = icone; Icone.color = corIcone;
            ContornoRotulo.enabled = ContornoValor.enabled = contorno;
        }
    }

    /// <summary>
    /// A linguagem visual da interface (docs/ART_BIBLE.md §5): texto por hierarquia, folha modal, lista rolavel, cartao de embarque, numero com
    /// rotulo, barra de nivel e botao com estados. As telas montam por aqui, em px da referencia de 1080; Art.cs fica com sprites e paleta.
    /// </summary>
    public static partial class Art
    {
        const float Cabecalho = 172f;
        static readonly int[] TipoTam = { 72, 46, 44, 44, 36, 32 };

        public static Text Escreve(Transform parent, string name, Tipo tipo, Vector2 anchorMin, Vector2 anchorMax, TextAnchor align = TextAnchor.MiddleLeft)
        {
            Text t = NewText(parent, name, TipoTam[(int)tipo], anchorMin, anchorMax, align);
            t.color = tipo == Tipo.Legenda ? Fraco : Tinta;
            if (tipo < Tipo.Corpo) t.font = Titulo();
            return t;
        }

        /// <summary>Melhoria "a » b": o valor novo em verde (GDD §5.6: o efeito antes de comprar).</summary>
        public static string DePara(string a, string b) => a + " » <color=#1C6B3A>" + b + "</color>";

        /// <summary>Tamanho fixo em px, preso a um ponto do pai (0..1), que tambem vira o pivo.</summary>
        public static void Fixa(RectTransform rt, Vector2 ponto, Vector2 pos, Vector2 tam)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = ponto;
            rt.anchoredPosition = pos;
            rt.sizeDelta = tam;
        }

        /// <summary>Esticado no pai, com margens em px.</summary>
        public static void Margem(RectTransform rt, float esq, float baixo, float dir, float cima)
        {
            rt.offsetMin = new Vector2(esq, baixo);
            rt.offsetMax = new Vector2(-dir, -cima);
        }

        static Sprite Pilula() => Nove("pilula9", 48, 23, (x, y) => C32(Color.white, 0.5f - CaixaDist(x + 0.5f, y + 0.5f, 48, 23f)));
        static Sprite Folha9() => Nove("folha9", 128, 60, (x, y) => C32(Color.white, 0.5f - CaixaDist(x + 0.5f, y + 0.5f, 128, 52f)));

        /// <summary>Cartao branco com cantos de 28 px e sombra curta embaixo; a arte ocupa ate 10 px da borda do retangulo.</summary>
        static Sprite Cartao9() => Nove("cartao9", 128, 52, (x, y) =>
        {
            float d = CaixaDist(x + 0.5f, y + 0.5f, 128, 38f) + 10f, sd = CaixaDist(x + 0.5f, y + 6.5f, 128, 38f) + 10f;
            float a = Mathf.Clamp01(0.5f - d), s = 0.26f * Mathf.Clamp01(1f - Mathf.Max(sd, 0f) / 9f);
            return C32(Color.Lerp(Hex(0x142847), Color.white, a), a + s * (1f - a));
        });

        static Image Fatia(Transform parent, string name, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            Image i = Img(parent, name, sprite, color, anchorMin, anchorMax);
            i.type = Image.Type.Sliced; i.preserveAspect = false;
            return i;
        }

        /// <summary>Folha ancorada ao pe da area segura: cabecalho azul com placa amarela, titulo e X, corpo claro. `veu` = o fundo do modal; tocar fora fecha.</summary>
        public static Folha NovaFolha(RectTransform veu, string titulo, Sprite icone, float altura, Action fechar)
        {
            Image fora = Node(veu, "ToqueFora", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            fora.color = Color.clear;
            fora.gameObject.AddComponent<Button>().onClick.AddListener(() => fechar());
            RectTransform area = Node(veu, "AreaSegura", Vector2.zero, Vector2.one);
            AreaSegura.AncorarDentro(area);

            Image f = Fatia(area, "Folha", Folha9(), Azul, Vector2.zero, Vector2.right);
            f.raycastTarget = true;
            f.rectTransform.pivot = new Vector2(0.5f, 0f);
            Image corpo = Img(f.transform, "Fundo", Square(), CorFolha, Vector2.zero, Vector2.one);
            corpo.preserveAspect = false;
            Margem(corpo.rectTransform, 0f, -300f, 0f, Cabecalho);   // desce alem da area segura: a barra de gestos nao mostra o jogo
            Image fio = Img(f.transform, "Fio", Square(), Placa, Vector2.up, Vector2.one);
            fio.preserveAspect = false;
            fio.rectTransform.offsetMin = new Vector2(0f, -Cabecalho); fio.rectTransform.offsetMax = new Vector2(0f, -Cabecalho + 8f);

            Fixa(NovaPlaca(f.transform, "Placa", icone, Placa, Azul).rectTransform, Vector2.up, new Vector2(32f, -28f), new Vector2(116f, 116f));
            Text t = Escreve(f.transform, "Titulo", Tipo.Titulo, Vector2.up, Vector2.one);
            t.rectTransform.offsetMin = new Vector2(172f, -146f); t.rectTransform.offsetMax = new Vector2(-170f, -26f);
            t.color = Color.white; t.text = titulo;

            Button x = NewButton(f.transform, "", 40, AzulLuz, Vector2.one, Vector2.one, fechar);
            Fixa((RectTransform)x.transform, Vector2.one, new Vector2(-28f, -30f), new Vector2(112f, 112f));
            Toque(x, 18f, 18f);
            Img(x.transform, "Fechar", Fechar(), Color.white, new Vector2(0.24f, 0.28f), new Vector2(0.76f, 0.8f));

            RectTransform miolo = Node(corpo.transform, "Corpo", Vector2.zero, Vector2.one);
            Margem(miolo, 0f, 300f, 0f, 0f);
            return new Folha { Rt = f.rectTransform, Corpo = miolo, Titulo = t, Altura = altura };
        }

        /// <summary>Lista rolavel na vertical; devolve o conteudo, onde entram os cartoes. Se couber tudo, nao rola.</summary>
        public static RectTransform NovaLista(Transform parent, Vector2 anchorMin, Vector2 anchorMax, float espaco = 8f)
        {
            RectTransform vista = Node(parent, "Lista", anchorMin, anchorMax);
            vista.gameObject.AddComponent<Image>().color = Color.clear;   // recebe o arrasto entre os botoes
            vista.gameObject.AddComponent<RectMask2D>();
            RectTransform c = Node(vista, "Conteudo", Vector2.up, Vector2.one);
            c.pivot = new Vector2(0.5f, 1f);
            var g = c.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = espaco; g.padding = new RectOffset(0, 0, 0, 24);
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = true; g.childForceExpandHeight = false;
            c.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var s = vista.gameObject.AddComponent<ScrollRect>();
            s.content = c; s.viewport = vista; s.horizontal = false; s.scrollSensitivity = 40f;
            s.movementType = ScrollRect.MovementType.Clamped;
            return c;
        }

        /// <summary>Cartao da lista. canhoto > 0: separa a faixa direita, a do botao, como num cartao de embarque (picote e 2 recortes).</summary>
        public static RectTransform NovoCartao(Transform lista, string name, float altura, float canhoto = 0f)
        {
            Image c = Fatia(lista, name, Cartao9(), Color.white, Vector2.zero, Vector2.one);
            LayoutElement e = c.gameObject.AddComponent<LayoutElement>();
            e.minHeight = e.preferredHeight = altura;
            if (canhoto > 0f)
            {
                Image p = Img(c.transform, "Picote", Pontilhado(), Vazio, Vector2.right, Vector2.one);
                p.preserveAspect = false;
                p.rectTransform.offsetMin = new Vector2(-canhoto - 3f, 28f); p.rectTransform.offsetMax = new Vector2(-canhoto + 3f, -28f);
                for (int i = 0; i < 2; i++)
                    Fixa(Img(c.transform, "Recorte", Disc(), CorFolha, Vector2.right, Vector2.right).rectTransform, new Vector2(1f, i), new Vector2(-canhoto + 19f, i == 0 ? -9f : 9f), new Vector2(38f, 38f));
            }
            return c.rectTransform;
        }

        /// <summary>Placa de icone de cantos redondos, 62% de glifo. Devolve o fundo; o glifo e o unico Image filho.</summary>
        public static Image NovaPlaca(Transform parent, string name, Sprite glifo, Color fundo, Color cor)
        {
            Image p = Img(parent, name, Rounded(), fundo, Vector2.zero, Vector2.zero);
            Img(p.transform, "Glifo", glifo, cor, new Vector2(0.19f, 0.19f), new Vector2(0.81f, 0.81f));
            return p;
        }

        public static Numero NovoNumero(Transform parent, string name, Sprite icone, Color cor, string rotulo, Vector2 anchorMin, Vector2 anchorMax)
        {
            Image c = Fatia(parent, name, Cartao9(), Color.white, anchorMin, anchorMax);
            Fixa(Img(c.transform, "Icone", icone, cor, Vector2.up, Vector2.up).rectTransform, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(68f, 68f));
            var n = new Numero
            {
                Rotulo = Escreve(c.transform, "Rotulo", Tipo.Legenda, new Vector2(0f, 0.5f), new Vector2(1f, 0.9f)),
                Valor = Escreve(c.transform, "Valor", Tipo.Valor, new Vector2(0f, 0.1f), new Vector2(1f, 0.55f)),
            };
            Margem(n.Rotulo.rectTransform, 112f, 0f, 18f, 0f);
            Margem(n.Valor.rectTransform, 112f, 0f, 18f, 0f);
            n.Rotulo.text = rotulo;
            return n;
        }

        public static BarraNivel NovaBarraNivel(Transform parent, string name, int max, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform r = Node(parent, name, anchorMin, anchorMax);
            var pip = new Image[max];
            for (int i = 0; i < max; i++)
            {
                pip[i] = Fatia(r, "Nivel" + i, Pilula(), Vazio, new Vector2((float)i / max, 0f), new Vector2((i + 1f) / max, 1f));
                pip[i].rectTransform.offsetMax = new Vector2(-6f, 0f);
            }
            return new BarraNivel(pip);
        }

        public static BotaoPreco NovoBotaoPreco(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Action onClick)
        {
            Button b = NewButton(parent, "", 40, Compra, anchorMin, anchorMax, onClick);
            b.transition = Selectable.Transition.None;   // a cor e o estado: quem pinta e o BotaoPreco
            Text r = Rotulo(b);
            r.fontSize = r.resizeTextMaxSize = 34;
            r.rectTransform.anchorMin = new Vector2(0f, 0.52f); r.rectTransform.anchorMax = new Vector2(1f, 0.96f);
            Margem(r.rectTransform, 10f, 0f, 10f, 0f);
            RectTransform linha = Node(b.transform, "Linha", new Vector2(0f, 0.12f), new Vector2(1f, 0.58f));
            var g = linha.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.childAlignment = TextAnchor.MiddleCenter; g.spacing = 10f;
            g.childControlWidth = g.childControlHeight = true; g.childForceExpandWidth = g.childForceExpandHeight = false;
            Image ic = Img(linha, "Icone", Moeda(), Color.white, Vector2.zero, Vector2.one);
            LayoutElement e = ic.gameObject.AddComponent<LayoutElement>();
            e.preferredWidth = e.preferredHeight = 46f;
            Text v = Outlined(Escreve(linha, "Valor", Tipo.Valor, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter), 2f);
            v.resizeTextForBestFit = false; v.horizontalOverflow = HorizontalWrapMode.Overflow;
            return new BotaoPreco { B = b, Rotulo = r, Valor = v, Fundo = (Image)b.targetGraphic, Icone = ic, Linha = linha,
                ContornoRotulo = r.GetComponent<Outline>(), ContornoValor = v.GetComponent<Outline>() };
        }
    }
}
