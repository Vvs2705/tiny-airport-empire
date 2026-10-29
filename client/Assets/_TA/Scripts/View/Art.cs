using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Rect = UnityEngine.Rect;

namespace TA
{
    /// <summary>
    /// Arte 100% gerada por codigo (zero asset), copia enxuta do Art.cs do Dungeon Courier (dc:Scripts/View/Art.cs): mascaras com 4 amostras
    /// de antialias e cache por chave, mais os helpers de uGUI por codigo (Node, Img, NewText, Panel, NewButton). Greybox da v0.1: a arte do
    /// Tripo entra por cima destes metodos depois do portao da Fase B (D-14). Cada estado tem cor E forma/texto (critico nunca e so cor).
    /// </summary>
    public static class Art
    {
        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
        public static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // Paleta greybox (GDD_ORIGEM "Interface e marca": azul-marinho da interface, amarelo de aeroporto nos avisos)
        public static readonly Color Ink = Hex(0xF7F3EA), Tinta = Hex(0x101826), Bad = Hex(0xFF3B30), Good = Hex(0x45C46B), Warn = Hex(0xFFB020),
            Coin = Hex(0xF5A623), Gold = Hex(0xE0B23A), Bg = Hex(0x14203A), Dim = Hex(0x8A98B8), Off = Hex(0x4F5866), Dark = Hex(0x0B1220),
            Painel = Hex(0x1E2D4D), Board = Hex(0x0E1526), BoardInk = Hex(0xFFD54A), Paper = Hex(0xF3EEE2);
        /// <summary>Cor de cada estacao (Estacao) e de cada voo da onda (o passageiro tem a cor do voo dele, ligando a fila ao quadro).</summary>
        public static readonly Color[] EstacaoCor = { Hex(0x2F6F9E), Hex(0x5B4A9E), Hex(0x2F8A6A), Hex(0x9E5B2F) };
        public static readonly Color[] VooCor = { Hex(0x4FC3F7), Hex(0xFF8A65) };
        public static readonly Color[] FuncCor = { Hex(0xFFD54A), Hex(0xB2FF59), Hex(0xFF80AB) };

        const int Side = 128;
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Square() => Get("sq", (x, y) => true);
        public static Sprite Disc() => Get("disc", (x, y) => x * x + y * y <= 0.96f);
        public static Sprite Ring() => Get("ring", (x, y) => { float d = x * x + y * y; return d <= 0.96f && d >= 0.66f; });
        public static Sprite Triangle() => Get("tri", (x, y) => y > -0.8f && Mathf.Abs(x) < (0.9f - y) * 0.58f);
        public static Sprite Heart() => Get("heart", (x, y) =>
        {
            float X = x * 1.2f, Y = y * 1.2f + 0.15f, a = X * X + Y * Y - 1f;
            return a * a * a - X * X * Y * Y * Y <= 0f;
        });
        public static Sprite Check() => Get("check", (x, y) => SegD(x, y, -0.7f, 0f, -0.2f, -0.55f) < 0.15f || SegD(x, y, -0.2f, -0.55f, 0.75f, 0.6f) < 0.15f);
        /// <summary>Alerta: triangulo com "!" vazado (estado critico = cor + icone + texto).</summary>
        public static Sprite Alerta() => Get("alerta", (x, y) => y > -0.8f && Mathf.Abs(x) < (0.9f - y) * 0.58f
            && !(Mathf.Abs(x) < 0.09f && ((y > -0.25f && y < 0.45f) || (y > -0.58f && y < -0.4f))));
        /// <summary>Mao do toque (dedo indicador para cima).</summary>
        public static Sprite Mao() => Get("mao", (x, y) => (Mathf.Abs(x + 0.05f) < 0.16f && y > -0.1f && y < 0.85f)
            || (Mathf.Abs(x) < 0.5f && y > -0.85f && y < 0.05f && (x * x * 0.6f + (y + 0.4f) * (y + 0.4f) < 0.36f || Mathf.Abs(x) < 0.42f)));
        /// <summary>Aviao visto de cima, nariz para a direita: fuselagem com nariz redondo, asas enflechadas e empenagem.</summary>
        public static Sprite Aviao() => Get("aviao2", (x, y) =>
            (Mathf.Abs(y) < 0.11f && x > -0.92f && x < 0.8f) || (x >= 0.8f && (x - 0.8f) * (x - 0.8f) + y * y < 0.0121f)
            || (Mathf.Abs(y) < 0.85f && x < 0.22f - Mathf.Abs(y) * 0.55f && x > -0.08f - Mathf.Abs(y) * 0.55f)
            || (Mathf.Abs(y) < 0.36f && x < -0.62f - Mathf.Abs(y) * 0.45f && x > -0.9f - Mathf.Abs(y) * 0.2f));

        // ---------- bonecos (greybox do Mundo; a arte do Tripo troca estes sprites mantendo as camadas: corpo, colete tingido, cabeca, cabelo) ----------
        public static Sprite Rounded() => Get("rounded", RoundedIn);
        static bool RoundedIn(float x, float y)
        {
            const float r = 0.3f;
            float qx = Mathf.Max(Mathf.Abs(x) - (0.96f - r), 0f), qy = Mathf.Max(Mathf.Abs(y) - (0.96f - r), 0f);
            return qx * qx + qy * qy <= r * r;
        }
        /// <summary>Colete (uniforme da estacao, tingido pelo SpriteRenderer): retangulo arredondado com decote em V.</summary>
        public static Sprite Colete() => Get("colete", (x, y) => RoundedIn(x, y) && !(y > 0.05f && Mathf.Abs(x) < (y - 0.05f) * 0.75f));
        /// <summary>Cabelo: meia cabeca de cima, com franja.</summary>
        public static Sprite Cabelo() => Get("cabelo", (x, y) => x * x + y * y <= 0.96f && y > 0.05f - 0.25f * Mathf.Abs(x));
        public static Sprite Mala() => Get("mala", (x, y) => RoundedIn(x, (y + 0.2f) * 1.25f) || (Mathf.Abs(x) < 0.4f && y > 0.55f && y < 0.85f && Mathf.Abs(x) > 0.2f) || (Mathf.Abs(x) < 0.4f && y > 0.75f && y < 0.85f));

        /// <summary>Arco de progresso (anel cheio no sentido horario a partir do topo ate frac). ponytail: 24 degraus em cache.</summary>
        public static Sprite Arc(float frac)
        {
            int k = Mathf.Clamp(Mathf.RoundToInt(frac * 24f), 0, 24);
            return Arcs[k] != null ? Arcs[k] : (Arcs[k] = MakeArc(k));
        }
        static Sprite MakeArc(int k) => Get("arc" + k, (x, y) =>
        {
            float d = x * x + y * y;
            return d <= 0.96f && d >= 0.6f && Mathf.Repeat(Mathf.Atan2(x, y) / (Mathf.PI * 2f), 1f) <= k / 24f;
        });
        static readonly Sprite[] Arcs = new Sprite[25];

        static float SegD(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            float t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy));
            float ex = px - (ax + t * dx), ey = py - (ay + t * dy);
            return Mathf.Sqrt(ex * ex + ey * ey);
        }

        // cache conferido ANTES de criar a closure (licao do Forge Street: lambda capturada aloca na entrada do metodo)
        static Sprite Get(string key, Func<float, float, bool> inside) => Cached(key) ?? Mask(key, Supersample(inside));
        static Func<int, int, byte> Supersample(Func<float, float, bool> inside) => (x, y) =>
        {
            int n = 0;
            for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                    if (inside((x + 0.25f + sx * 0.5f) / Side * 2f - 1f, (y + 0.25f + sy * 0.5f) / Side * 2f - 1f)) n++;
            return (byte)(n * 63 + (n == 4 ? 3 : 0));
        };
        static Sprite Soft(string key, Func<float, float, float> f) => Cached(key) ?? Mask(key, (x, y) =>
            (byte)(255f * Mathf.Clamp01(f((x + 0.5f) / Side * 2f - 1f, (y + 0.5f) / Side * 2f - 1f))));
        static Sprite Cached(string key) => Cache.TryGetValue(key, out Sprite s) && s != null ? s : null;

        static Sprite Mask(string key, Func<int, int, byte> alpha)
        {
            var tex = new Texture2D(Side, Side, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontUnloadUnusedAsset };
            var px = new Color32[Side * Side];
            for (int y = 0; y < Side; y++)
                for (int x = 0; x < Side; x++) px[y * Side + x] = new Color32(255, 255, 255, alpha(x, y));
            tex.SetPixels32(px);
            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, Side, Side), new Vector2(0.5f, 0.5f), Side);
            Cache[key] = s;
            return s;
        }

        public static SpriteRenderer NewSprite(Transform parent, string name, Sprite sprite, Color color, int order, Vector2 pos, Vector2 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite; r.color = color; r.sortingOrder = order;
            return r;
        }

        // ---------- uGUI por codigo (sem TextMeshPro); fonte embutida do Unity ate as fontes OFL da ART_BIBLE ----------
        static Font _font;
        public static Font Font() => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        static float CaixaDist(float x, float y, int s, float raio)
        {
            float h = s * 0.5f - 0.5f, qx = Mathf.Abs(x - s * 0.5f) - (h - raio), qy = Mathf.Abs(y - s * 0.5f) - (h - raio);
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - raio;
        }

        /// <summary>Sprite 9-fatias s x s pintado por pixel, 100 px por unidade = 1 px da referencia de 1080.</summary>
        static Sprite Nove(string key, int s, int borda, Func<int, int, Color32> pixel)
        {
            if (Cache.TryGetValue(key, out Sprite sp) && sp != null) return sp;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++) px[y * s + x] = pixel(x, y);
            t.SetPixels32(px);
            t.Apply(false, true);
            sp = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(borda, borda, borda, borda));
            Cache[key] = sp;
            return sp;
        }

        static Color32 C32(Color c, float a) => new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), (byte)(Mathf.Clamp01(a) * 255f));

        /// <summary>Botao em cinza, tingido pela cor do Image: contorno escuro de 4 px, face com degrade e aba de relevo de 12 px na base.</summary>
        public static Sprite Botao() => Nove("botao9", 96, 36, (x, y) =>
        {
            float d = CaixaDist(x + 0.5f, y + 0.5f, 96, 26f), dentro = -d;
            float v = dentro < 4f ? 0.16f : y + 0.5f < 16f ? 0.6f : Mathf.Lerp(0.84f, 1f, (y + 0.5f) / 96f);
            return C32(new Color(v, v, v), 0.5f - d);
        });

        /// <summary>Destaque do tour e do destino valido: contorno arredondado de ~7 px com brilho suave, tingido pelo Image.</summary>
        public static Sprite Contorno() => Nove("contorno9", 64, 26, (x, y) =>
        {
            float d = CaixaDist(x + 0.5f, y + 0.5f, 64, 22f) + 9f;
            return C32(Color.white, Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(d) / 3.5f), 0.35f * Mathf.Clamp01(1f - Mathf.Abs(d) / 9f)));
        });

        /// <summary>Balao de fala (Lara, historia): branco com contorno de tinta de 4 px e cantos de 26 px; tingido pelo Image.</summary>
        public static Sprite Balao() => Nove("balao9", 96, 34, (x, y) =>
        {
            float dd = CaixaDist(x + 0.5f, y + 0.5f, 96, 26f), dentro = -dd;
            return C32(dentro < 4f ? Tinta : Color.white, 0.5f - dd);
        });

        /// <summary>Seta do tour apontando para baixo; o Tour gira 180 graus quando o alvo esta acima do cartao.</summary>
        public static Sprite Seta() => Soft("seta", (x, y) =>
        {
            float cabeca = Mathf.Clamp01(((y + 0.95f) * 0.95f - Mathf.Abs(x)) * 12f) * Mathf.Clamp01((0.1f - y) * 12f);
            float haste = Mathf.Clamp01((0.24f - Mathf.Abs(x)) * 12f) * Mathf.Clamp01((y + 0.05f) * 12f) * Mathf.Clamp01((0.95f - y) * 12f);
            return Mathf.Max(cabeca, haste);
        });

        public static RectTransform Node(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            Image i = Node(parent, name, anchorMin, anchorMax).gameObject.AddComponent<Image>();
            i.sprite = sprite; i.color = color; i.preserveAspect = true; i.raycastTarget = false;
            return i;
        }

        public static Text NewText(Transform parent, string name, int size, Vector2 anchorMin, Vector2 anchorMax, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = Node(parent, name, anchorMin, anchorMax).gameObject.AddComponent<Text>();
            t.font = Font(); t.fontSize = size; t.color = Ink; t.alignment = align; t.raycastTarget = false; t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = Mathf.Max(10, size * 2 / 3); t.resizeTextMaxSize = size;
            return t;
        }

        /// <summary>Contorno escuro: le sobre qualquer fundo.</summary>
        public static Text Outlined(Text t, float px = 2f)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = A(Tinta, 0.95f); o.effectDistance = new Vector2(px, -px);
            return t;
        }

        /// <summary>Retangulo arredondado 9-fatias para uGUI (canto de ~20 px na referencia).</summary>
        public static Sprite Box()
        {
            if (Cache.TryGetValue("box9", out Sprite s) && s != null) return s;
            Sprite m = Get("box9mask", (x, y) =>
            {
                const float r = 0.62f;
                float qx = Mathf.Max(Mathf.Abs(x) - (1f - r), 0f), qy = Mathf.Max(Mathf.Abs(y) - (1f - r), 0f);
                return qx * qx + qy * qy <= r * r;
            });
            s = Sprite.Create(m.texture, new Rect(0, 0, Side, Side), new Vector2(0.5f, 0.5f), 200f, 0, SpriteMeshType.FullRect, new Vector4(44, 44, 44, 44));
            Cache["box9"] = s;
            return s;
        }

        public static Image Panel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var img = Node(parent, name, anchorMin, anchorMax).gameObject.AddComponent<Image>();
            img.sprite = Box(); img.type = Image.Type.Sliced; img.color = color;
            return img;
        }

        /// <summary>Botao: painel arredondado + rotulo em negrito, estados pelo ColorTint do uGUI e o Aperta (afunda no toque).</summary>
        public static Button NewButton(Transform parent, string label, int size, Color color, Vector2 anchorMin, Vector2 anchorMax, Action onClick)
        {
            var img = Node(parent, "Btn " + label, anchorMin, anchorMax).gameObject.AddComponent<Image>();
            img.sprite = Botao(); img.type = Image.Type.Sliced; img.color = color;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cores = b.colors; cores.pressedColor = new Color(0.8f, 0.8f, 0.8f); cores.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f); b.colors = cores;
            img.gameObject.AddComponent<Aperta>();
            b.onClick.AddListener(() => onClick());
            Text t = NewText(img.transform, "Rotulo", size, Vector2.zero, Vector2.one);
            t.rectTransform.offsetMin = new Vector2(12f, 14f); t.rectTransform.offsetMax = new Vector2(-12f, -4f);   // fora do contorno e da aba
            t.text = label;
            Outlined(t, Mathf.Max(2f, size / 16f));
            return b;
        }

        public static Text Rotulo(Button b) => b.GetComponentInChildren<Text>();

        /// <summary>Aumenta a area de toque sem mudar o desenho: filho transparente que recebe o raycast. pad em px da referencia por lado.</summary>
        public static void Toque(Component alvo, float padX, float padY)
        {
            Image hit = Node(alvo.transform, "AreaDeToque", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.rectTransform.offsetMin = new Vector2(-padX, -padY); hit.rectTransform.offsetMax = new Vector2(padX, padY);
            hit.rectTransform.SetAsFirstSibling();
        }
    }

    /// <summary>Botao afunda um pouco enquanto o dedo esta em cima (resposta ao toque alem da cor; so se o botao estiver ativo).</summary>
    public sealed class Aperta : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Selectable _s;
        public void OnPointerDown(PointerEventData e)
        {
            if (_s == null) _s = GetComponent<Selectable>();
            if (_s == null || _s.IsInteractable()) transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        }
        public void OnPointerUp(PointerEventData e) => transform.localScale = Vector3.one;
        public void OnPointerExit(PointerEventData e) => transform.localScale = Vector3.one;
    }

    /// <summary>dp -> px (copia do dc:Scripts/View/Dp.cs). Numero que o dedo sente vive em dp: px = dp x dpi / 160.</summary>
    public static class Dp
    {
        public static float Dpi { get { float d = Screen.dpi; return d > 0f ? d : 160f; } }
        public static float Px(float dp) => dp * Dpi / 160f;
        /// <summary>Piso do alvo de toque (GDD §5.5, §11.2).</summary>
        public const float AlvoMinimoDp = 48f;
    }

    /// <summary>Area segura (notch, camera, barra de gestos), copia do dc:Scripts/View/AreaSegura.cs.</summary>
    public static class AreaSegura
    {
        public static void AncorarDentro(RectTransform rt)
        {
            Rect s = Screen.safeArea;
            float w = Mathf.Max(1f, Screen.width), h = Mathf.Max(1f, Screen.height);
            rt.anchorMin = new Vector2(Mathf.Max(s.xMin, 0f) / w, Mathf.Max(s.yMin, 0f) / h);
            rt.anchorMax = new Vector2(Mathf.Min(s.xMax, w) / w, Mathf.Min(s.yMax, h) / h);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
