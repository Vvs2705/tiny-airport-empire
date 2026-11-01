using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace TA
{
    /// <summary>
    /// Folha pre-renderizada de uma pessoa (client/tools/render_sprites.py; copia enxuta de dc:Scripts/View/SpriteSheet.cs):
    /// Resources/Sprites/&lt;ator&gt;/meta.json + 1 PNG por clipe, linha = direcao (linha 0 no topo), coluna = quadro. No TA a folha e
    /// diagonal (`diag`: linha 0 olha para baixo e a esquerda na tela, linha 1 para cima e a esquerda; a direita e o espelho) e traz a
    /// mascara do uniforme empilhada na metade de baixo (`mask`), que o shader TA/SpriteTinta le (Shaders/SpriteTinta.shader).
    /// Carrega 1 vez por nome (inclusive a falha) e cria cada Sprite na 1a vez que e pedido. Sem folha (pasta vazia, meta ruim,
    /// -nographics) o Get devolve null e o Mundo desenha o boneco procedural da v0.1.
    /// </summary>
    public sealed class SpriteSheet
    {
        [Serializable] public sealed class ClipMeta { public string name, file; public int frames; public bool loop; public float fps; }
        [Serializable] public sealed class Meta { public int size, rows; public bool diag, mask; public float ppu; public float[] pivot; public List<ClipMeta> clips; }

        static readonly Dictionary<string, SpriteSheet> Cache = new Dictionary<string, SpriteSheet>();
        static Material _mat;

        /// <summary>Material unico de todas as folhas (TA/SpriteTinta; o Setup.Apply o poe no build). Sem o shader, o padrao do sprite.</summary>
        public static Material Material
        {
            get
            {
                if (_mat == null) { Shader sh = Shader.Find("TA/SpriteTinta"); if (sh != null) _mat = new Material(sh); }
                return _mat;
            }
        }

        public readonly int Rows;
        readonly float _ppu, _size;
        readonly bool _mask;
        readonly Vector2 _pivot;
        readonly ClipMeta[] _clips;
        readonly Texture2D[] _tex;
        readonly Sprite[][] _spr;

        SpriteSheet(Meta m, Texture2D[] tex)
        {
            Rows = m.rows; _ppu = m.ppu; _size = m.size; _mask = m.mask;
            _pivot = m.pivot != null && m.pivot.Length >= 2 ? new Vector2(m.pivot[0], m.pivot[1]) : new Vector2(0.5f, 0f);
            _clips = m.clips.ToArray(); _tex = tex; _spr = new Sprite[tex.Length][];
        }

        public static SpriteSheet Get(string name)
        {
            if (!Cache.TryGetValue(name, out SpriteSheet s)) Cache[name] = s = Load(name);
            return s;
        }

        static SpriteSheet Load(string name)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return null;
            var json = Resources.Load<TextAsset>("Sprites/" + name + "/meta");
            if (json == null) return null;
            Meta m = null;
            try { m = JsonUtility.FromJson<Meta>(json.text); }
            catch (ArgumentException) { }
            if (m == null || m.size <= 0 || m.ppu <= 0f || !m.diag || m.rows != 2 || m.clips == null || m.clips.Count == 0)
            {
                Debug.LogWarning("SpriteSheet " + name + ": meta.json invalido, usando procedural");
                return null;
            }
            var tex = new Texture2D[m.clips.Count];
            int ok = 0;
            for (int i = 0; i < tex.Length; i++)
            {
                ClipMeta c = m.clips[i];
                string file = string.IsNullOrEmpty(c.file) ? c.name : Path.GetFileNameWithoutExtension(c.file);
                Texture2D t = c.frames > 0 ? Resources.Load<Texture2D>("Sprites/" + name + "/" + file) : null;
                if (t == null || t.width < c.frames || t.height < m.rows) { Debug.LogWarning("SpriteSheet " + name + ": clipe " + c.name + " sem PNG, pulado"); continue; }
                tex[i] = t; ok++;
            }
            return ok > 0 ? new SpriteSheet(m, tex) : null;
        }

        /// <summary>Indice do clipe `a` (ou `b`, ou Idle, ou o 1o que existe).</summary>
        public int Pick(string a, string b = null)
        {
            int i = Clip(a);
            if (i < 0 && b != null) i = Clip(b);
            if (i < 0) i = Clip("Idle");
            if (i >= 0) return i;
            for (i = 0; _tex[i] == null; i++) { }   // o Load garante ao menos 1 clipe com PNG
            return i;
        }

        int Clip(string name)
        {
            for (int i = 0; i < _clips.Length; i++) if (_tex[i] != null && _clips[i].name == name) return i;
            return -1;
        }

        public int Quadros(int clip) => _clips[clip].frames;

        /// <summary>Quadro no tempo `t` (s): sempre em laco (andar, parado e atender sao lacos no jogo).</summary>
        public int FrameAt(int clip, float t)
        {
            ClipMeta c = _clips[clip];
            return Mathf.Max(0, (int)(t * (c.fps > 0f ? c.fps : 12f))) % c.frames;
        }

        /// <summary>Sprite do quadro: celula pela textura real (o import pode reduzir a folha), so na metade de cima quando ha mascara.</summary>
        public Sprite Frame(int clip, int row, int frame)
        {
            ClipMeta c = _clips[clip];
            row = Mathf.Clamp(row, 0, Rows - 1); frame = Mathf.Clamp(frame, 0, c.frames - 1);
            Sprite[] arr = _spr[clip] ?? (_spr[clip] = new Sprite[Rows * c.frames]);
            int k = row * c.frames + frame;
            if (arr[k] != null) return arr[k];
            Texture2D t = _tex[clip];
            float cw = t.width / (float)c.frames, alto = _mask ? t.height * 0.5f : t.height, ch = alto / Rows;
            return arr[k] = Sprite.Create(t, new Rect(frame * cw, t.height - alto + (Rows - 1 - row) * ch, cw, ch), _pivot, _ppu * cw / _size, 0, SpriteMeshType.FullRect);
        }
    }
}
