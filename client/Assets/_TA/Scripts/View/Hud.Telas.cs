using System;
using System.Collections.Generic;
using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// Telas modais (seguram o relogio do quadro enquanto abertas): melhorias (efeito de cada upgrade escrito antes de comprar, GDD §5.6),
    /// "mover para" (o botao alternativo ao arrasto, GDD §5.5), cofre offline (intervalo e calculo, GDD §6.3), anuncio simulado (v0.1 sem
    /// SDK: 3 s e so entao o Pagar do nucleo; fechar antes = sem recompensa), pausa e album das historias (TA-3).
    /// </summary>
    public sealed partial class Hud
    {
        sealed class Modal { public RectTransform Rt; public Text Titulo, Corpo; }

        Modal _melhorias, _mover, _cofre, _anuncio, _pausa, _album;
        readonly List<Modal> _modais = new List<Modal>();
        readonly Button[,] _melhoriaBtn = new Button[4, 2];
        readonly Button[] _moverBtn = new Button[4];
        Button _dobrar;
        int _moverQuem = -1;
        float _anuncioFalta;
        Anuncio _anuncioQual;
        string _anuncioTx;

        public bool ModalAberto { get { foreach (Modal m in _modais) if (m.Rt.gameObject.activeSelf) return true; return false; } }

        Modal NovoModal(RectTransform canvas, string nome, float y0, float y1)
        {
            var m = new Modal();
            Image veu = Art.Img(canvas, nome, Art.Square(), new Color(0f, 0f, 0f, 0.62f), Vector2.zero, Vector2.one);
            veu.preserveAspect = false; veu.raycastTarget = true;   // segura o toque do jogo atras
            m.Rt = veu.rectTransform;
            RectTransform p = Art.Panel(m.Rt, "Painel", Art.Painel, new Vector2(0.05f, y0), new Vector2(0.95f, y1)).rectTransform;
            m.Titulo = Art.Outlined(Art.NewText(p, "Titulo", 50, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f)), 3f);
            m.Corpo = Art.NewText(p, "Corpo", 36, new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.85f), TextAnchor.UpperLeft);
            m.Rt.gameObject.SetActive(false);
            _modais.Add(m);
            return m;
        }

        static RectTransform PainelDe(Modal m) => (RectTransform)m.Titulo.transform.parent;

        void BuildTelas(RectTransform canvas)
        {
            _melhorias = NovoModal(canvas, "Melhorias", 0.14f, 0.88f);
            _melhorias.Titulo.text = Textos.Melhorias;
            RectTransform pm = PainelDe(_melhorias);
            for (int i = 0; i < 4; i++)
            {
                float y1 = 0.84f - i * 0.18f;
                Text n = Art.NewText(pm, "Est" + i, 38, new Vector2(0.05f, y1 - 0.05f), new Vector2(0.95f, y1), TextAnchor.MiddleLeft);
                n.text = Textos.Estacao[i]; n.color = Color.Lerp(Art.EstacaoCor[i], Art.Ink, 0.5f);
                for (int m = 0; m < 2; m++)
                {
                    var e = (Estacao)i; var mm = (Melhoria)m;
                    _melhoriaBtn[i, m] = Art.NewButton(pm, "", 30, m == 0 ? Art.EstacaoCor[1] : Art.EstacaoCor[2],
                        new Vector2(0.04f + m * 0.47f, y1 - 0.165f), new Vector2(0.49f + m * 0.47f, y1 - 0.055f), () => _g.Buy(e, mm));
                }
            }
            _melhorias.Corpo.text = "";
            Art.NewButton(pm, Textos.Fechar, 40, Art.Off, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.1f), () => Fecha(_melhorias));

            _mover = NovoModal(canvas, "Mover", 0.3f, 0.72f);
            RectTransform pv = PainelDe(_mover);
            _mover.Corpo.text = "";
            for (int i = 0; i < 4; i++)
            {
                var e = (Estacao)i;
                _moverBtn[i] = Art.NewButton(pv, Textos.Estacao[i], 38, Art.EstacaoCor[i], new Vector2(0.05f + (i % 2) * 0.46f, 0.6f - (i / 2) * 0.28f),
                    new Vector2(0.49f + (i % 2) * 0.46f, 0.82f - (i / 2) * 0.28f), () => { int q = _moverQuem; Fecha(_mover); _g.Move(q, e); });
            }
            Art.NewButton(pv, Textos.Cancelar, 38, Art.Off, new Vector2(0.3f, 0.04f), new Vector2(0.7f, 0.2f), () => Fecha(_mover));

            _cofre = NovoModal(canvas, "Cofre", 0.3f, 0.7f);
            RectTransform pc = PainelDe(_cofre);
            Art.NewButton(pc, Textos.Coletar, 40, Art.Good, new Vector2(0.05f, 0.05f), new Vector2(0.45f, 0.24f), () => Fecha(_cofre));
            _dobrar = Art.NewButton(pc, "", 32, Art.Coin, new Vector2(0.5f, 0.05f), new Vector2(0.95f, 0.24f), () => { Fecha(_cofre); _g.VerAnuncio(Anuncio.Cofre); });

            _anuncio = NovoModal(canvas, "Anuncio", 0.35f, 0.65f);
            _anuncio.Titulo.text = Textos.AnuncioSimulado;
            Art.NewButton(PainelDe(_anuncio), Textos.FecharSemRecompensa, 34, Art.Off, new Vector2(0.15f, 0.06f), new Vector2(0.85f, 0.3f), () => Fecha(_anuncio));

            _pausa = NovoModal(canvas, "Pausa", 0.25f, 0.75f);
            _pausa.Titulo.text = Textos.Pausado;
            _pausa.Corpo.text = "";
            RectTransform pp = PainelDe(_pausa);
            Art.NewButton(pp, Textos.Continuar, 40, Art.Good, new Vector2(0.15f, 0.66f), new Vector2(0.85f, 0.82f), () => Fecha(_pausa));
            Art.NewButton(pp, Textos.ReverTutorial, 40, Art.EstacaoCor[0], new Vector2(0.15f, 0.46f), new Vector2(0.85f, 0.62f), () => { Fecha(_pausa); ReverTour(); });
            Art.NewButton(pp, Textos.ExportarDiario, 40, Art.EstacaoCor[3], new Vector2(0.15f, 0.26f), new Vector2(0.85f, 0.42f), () => Toast(_g.ExportarDiario()));
            Art.NewButton(pp, Textos.Album, 40, Art.EstacaoCor[1], new Vector2(0.15f, 0.06f), new Vector2(0.85f, 0.22f), () => { Fecha(_pausa); AbreTela("album"); });

            _album = NovoModal(canvas, "Album", 0.3f, 0.7f);
            _album.Titulo.text = Textos.Album;
            Art.NewButton(PainelDe(_album), Textos.Fechar, 40, Art.Off, new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.22f), () => Fecha(_album));
        }

        void Fecha(Modal m) => m.Rt.gameObject.SetActive(false);

        static void Abre(Modal m)
        {
            m.Rt.gameObject.SetActive(true);
            m.Rt.SetAsLastSibling();
        }

        /// <summary>Abre uma tela pelo nome (botoes e a flag -tela): melhorias, mover, cofre, anuncio, pausa, album. "jogo" = nenhuma.</summary>
        public void AbreTela(string nome)
        {
            switch (nome)
            {
                case "melhorias": Abre(_melhorias); break;
                case "mover": if (S.Staff.Count > 0) AbreMover(0); break;
                case "pausa": Abre(_pausa); break;
                case "album": Abre(_album); break;
                case "anuncio": _g.VerAnuncio(Anuncio.Receita); break;
            }
        }

        internal void AbreMover(int quem)
        {
            _moverQuem = quem;
            _mover.Titulo.text = Textos.Mover(quem);
            Abre(_mover);
        }

        /// <summary>Game: cofre ao abrir ou ao voltar da pausa (so com >= 60 s fora e Cash > 0).</summary>
        public void AbreCofre(long cash, long seg)
        {
            _cofre.Titulo.text = Textos.Coletar;
            _cofre.Corpo.text = Textos.Cofre(cash, seg);
            Abre(_cofre);
        }

        /// <summary>Game: anuncio simulado de `a` com o id de transacao `tx` (Pagar so quando acabar).</summary>
        public void AbreAnuncio(Anuncio a, string tx)
        {
            _anuncioQual = a; _anuncioTx = tx; _anuncioFalta = 3f;
            Abre(_anuncio);
        }

        void PintaTelas()
        {
            Sim s = S;
            if (_melhorias.Rt.gameObject.activeSelf)
                for (int i = 0; i < 4; i++)
                    for (int m = 0; m < 2; m++)
                    {
                        var e = (Estacao)i; var mm = (Melhoria)m;
                        long c = s.Custo(e, mm);
                        int n = s.Nivel(e, mm);
                        string efeito = mm == Melhoria.Velocidade
                            ? Textos.Seg(s.TempoSeg(e)) + (c < 0 ? "" : " → " + Textos.Seg((float)(s.T.Tempo[i] * Math.Pow(s.T.VelFator, n + 1))))
                            : s.St(e).Postos + (c < 0 ? "" : " → " + (s.St(e).Postos + 1));
                        Art.Rotulo(_melhoriaBtn[i, m]).text = Textos.MelhoriaLinha(mm, n, s.NivelMax(mm), efeito + " · " + Textos.Custo(c));
                        _melhoriaBtn[i, m].interactable = c >= 0 && s.Cash >= c;
                    }
            if (_mover.Rt.gameObject.activeSelf && _moverQuem >= 0 && _moverQuem < s.Staff.Count)
                for (int i = 0; i < 4; i++) _moverBtn[i].interactable = s.Staff[_moverQuem].Em != (Estacao)i;
            if (_cofre.Rt.gameObject.activeSelf)
            {
                bool pode = s.Anuncios.Pode(Anuncio.Cofre);
                _dobrar.gameObject.SetActive(pode);
                if (pode) Art.Rotulo(_dobrar).text = Textos.Dobrar(s.Anuncios.Previa(Anuncio.Cofre));
            }
            if (_anuncio.Rt.gameObject.activeSelf)
            {
                _anuncioFalta -= Time.unscaledDeltaTime;
                _anuncio.Corpo.text = Textos.AnuncioConta(Mathf.CeilToInt(Mathf.Max(0f, _anuncioFalta)));
                if (_anuncioFalta <= 0f) { Fecha(_anuncio); _g.AnuncioConcluido(_anuncioQual, _anuncioTx); }
            }
            if (_album.Rt.gameObject.activeSelf)
            {
                var sb = new System.Text.StringBuilder();
                for (int h = 0; h < Textos.Historia.Length; h++) sb.Append(Textos.AlbumLinha(h, (s.Album & (1 << h)) != 0)).Append('\n');
                _album.Corpo.text = sb.ToString();
            }
        }
    }
}
