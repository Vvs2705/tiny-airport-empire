using System;
using System.Collections.Generic;
using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// Tutorial guiado, passos 1-12 da Origem ("Onboarding"), no desenho do dc:Scripts/View/Tour.cs: veu com buraco no alvo, contorno
    /// dourado, seta e cartao com o que/quando/por que. Explicacao = o relogio do quadro para e so o ENTENDI anda; acao = so o alvo recebe o
    /// toque e o passo termina quando a acao acontece (lida do Sim). Um passo por vez, na ordem; passo cuja acao ja aconteceu e' pulado
    /// sem aparecer (quem contratou antes nao ve "contrate"). Os passos 10-12 (historia, terminal que cresceu, cofre) sao de evento: nascem
    /// quando o fato acontece, depois dos 9 primeiros, com 20 s de folga entre eles, e um que nao veio nao segura o seguinte. Cada passo aparece uma vez (PlayerPrefs "ta_tour"; Pausa > Rever tutorial
    /// zera). "Pular tutorial" sempre visivel. Desligado no -testsession e no -bot; -tutorial liga sem gravar; -tela passoN mostra o passo N.
    /// </summary>
    public sealed partial class Hud
    {
        sealed class Passo
        {
            public Func<bool> Quando, Feito;     // Feito null = explicacao (ENTENDI)
            public Func<Rect> Alvo;              // px de tela; null ou vazio = cartao no meio, sem buraco
            public bool Arrastar, Fala, Evento;
        }

        public const string TourKey = "ta_tour";
        /// <summary>Os passos 1-9 formam o tutorial (tutorial_completed); os 10-12 sao dicas de evento.</summary>
        const int PassosGuiados = 9;
        const float FolgaEventos = 20f;
        public bool TourLigado { get; set; }
        public bool TourGrava { get; set; }
        /// <summary>-bot com -tutorial: o ENTENDI se aperta sozinho em 2 s.</summary>
        public bool TourAuto { get; set; }

        Passo[] _passos;
        readonly HashSet<int> _feitos = new HashSet<int>();
        int _atual = -1;
        float _tourT;
        bool _tourComecou, _forcado, _cofreVisto;
        float _fimT = -99f;
        RectTransform _tour, _tourCard, _tourDedo;
        readonly Image[] _veu = new Image[4];
        Image _tourBorda, _tourSeta, _tourRetrato;
        Text _tourTitulo, _tourTexto, _tourConta, _tourQuem;
        Button _tourOk;

        bool TourAberto => _tour != null && _tour.gameObject.activeSelf;
        bool TourSegura => TourAberto && _passos[_atual].Feito == null;

        void BuildTour(RectTransform canvas)
        {
            Sim s = S;
            _passos = new[]
            {
                new Passo { Quando = () => true, Fala = true },
                new Passo { Quando = () => s.St(Estacao.CheckIn).Fila.Count > 0, Alvo = () => M.ZonaTela(Estacao.CheckIn),
                            Feito = () => s.St(Estacao.CheckIn).Atendendo.Count > 0 || s.Presentes(Estacao.CheckIn) > 0 || s.TotalEmbarcados > 0 },
                new Passo { Quando = () => s.St(Estacao.Seguranca).Fila.Count > 0, Alvo = () => M.ZonaTela(Estacao.Seguranca),
                            Feito = () => s.St(Estacao.Seguranca).Atendendo.Count > 0 || s.Presentes(Estacao.Seguranca) > 0 || s.TotalEmbarcados > 0 },
                new Passo { Quando = () => s.St(Estacao.Portao).Pessoas + s.St(Estacao.Loja).Pessoas > 0 || s.TotalEmbarcados > 0, Alvo = () => NaTela(QuadroRt) },
                new Passo { Quando = () => s.PodeTocar(Estacao.Portao) || s.TotalEmbarcados > 0, Alvo = () => M.ZonaTela(Estacao.Portao),
                            Feito = () => s.TotalEmbarcados > 0 },
                new Passo { Quando = () => s.Staff.Count > 0 || s.Cash >= s.CustoContratar(), Alvo = () => NaTela(ContratarRt), Feito = () => s.Staff.Count > 0 },
                new Passo { Quando = () => s.Alocados(Estacao.Seguranca) > 0 || (s.Staff.Count > 0 && s.St(Estacao.Seguranca).Estado != Estado.Normal),
                            Alvo = () => M.FuncTela(QuemArrasta(s)), Arrastar = true, Feito = () => s.Alocados(Estacao.Seguranca) > 0 },
                new Passo { Quando = () => true, Alvo = () => M.ZonaTela(Estacao.Seguranca) },
                new Passo { Quando = () => s.St(Estacao.Seguranca).Vel > 0 || s.Cash >= s.Custo(Estacao.Seguranca, Melhoria.Velocidade), Alvo = () => NaTela(MelhoriasRt),
                            Feito = () => s.St(Estacao.Seguranca).Vel > 0 },
                new Passo { Quando = () => LinhaDaHistoria(s) >= 0, Alvo = () => NaTela(LinhaRt(Math.Max(0, LinhaDaHistoria(s)))), Evento = true },
                new Passo { Quando = () => s.Marco >= 1, Alvo = () => NaTela(TerminalRt), Evento = true },
                new Passo { Quando = () => _cofreVisto, Alvo = () => NaTela(CashRt), Evento = true },
            };
            foreach (string id in PlayerPrefs.GetString(TourKey, "").Split(','))
                if (int.TryParse(id, out int n)) _feitos.Add(n);

            _tour = Art.Node(canvas, "Tour", Vector2.zero, Vector2.one);
            for (int i = 0; i < 4; i++)
            {
                _veu[i] = Art.Img(_tour, "Veu", Art.Square(), new Color(0f, 0f, 0f, 0.72f), Vector2.zero, Vector2.zero);
                _veu[i].preserveAspect = false; _veu[i].raycastTarget = true;   // segura o toque fora do alvo
            }
            _tourBorda = Art.Img(_tour, "Borda", Art.Contorno(), Art.Gold, Vector2.zero, Vector2.zero);
            _tourBorda.type = Image.Type.Sliced; _tourBorda.preserveAspect = false;
            _tourSeta = Art.Img(_tour, "Seta", Art.Seta(), Art.Gold, Vector2.zero, Vector2.zero);
            _tourDedo = Art.Img(_tour, "Dedo", Art.Disc(), Art.A(Color.white, 0.85f), Vector2.zero, Vector2.zero).rectTransform;
            _tourDedo.sizeDelta = new Vector2(90f, 90f);
            Art.Img(_tourDedo, "Halo", Art.Ring(), Art.Gold, new Vector2(-0.3f, -0.3f), new Vector2(1.3f, 1.3f));

            Image card = Art.Panel(_tour, "Cartao", Art.Paper, new Vector2(0.04f, 0.36f), new Vector2(0.96f, 0.64f));
            card.raycastTarget = true;
            _tourCard = card.rectTransform;
            _tourRetrato = Art.Img(_tourCard, "Lara", Art.Disc(), Art.EstacaoCor[0], new Vector2(0.03f, 0.62f), new Vector2(0.17f, 0.97f));
            _tourQuem = Art.NewText(_tourCard, "Quem", 28, new Vector2(0.19f, 0.86f), new Vector2(0.97f, 0.98f), TextAnchor.MiddleLeft);
            _tourQuem.color = Art.Off; _tourQuem.text = Textos.Lara;
            _tourTitulo = Art.NewText(_tourCard, "Titulo", 46, new Vector2(0.04f, 0.68f), new Vector2(0.86f, 0.86f), TextAnchor.MiddleLeft);
            _tourTitulo.color = Art.Tinta;
            _tourConta = Art.NewText(_tourCard, "Conta", 30, new Vector2(0.84f, 0.68f), new Vector2(0.97f, 0.86f), TextAnchor.MiddleRight);
            _tourConta.color = Art.Off;
            _tourTexto = Art.NewText(_tourCard, "Texto", 34, new Vector2(0.04f, 0.27f), new Vector2(0.96f, 0.68f), TextAnchor.UpperLeft);
            _tourTexto.color = Art.Tinta;
            _tourOk = Art.NewButton(_tourCard, Textos.Entendi, 40, Art.Good, new Vector2(0.55f, 0.04f), new Vector2(0.96f, 0.25f), () => Conclui(true));
            Art.NewButton(_tourCard, Textos.Pular, 30, Art.Off, new Vector2(0.04f, 0.04f), new Vector2(0.45f, 0.25f), PulaTour);
            _tour.gameObject.SetActive(false);
        }

        /// <summary>Linha do quadro cujo voo tem o passageiro com historia no aeroporto (-1 = nenhuma). Sem ela, a foto do passo 10 cai na linha 0.</summary>
        int LinhaDaHistoria(Sim s)
        {
            for (int f = 0; f < _linhas.Length; f++)
                if (s.Voos[f] != null && HistoriaNoAeroporto(s, s.Voos[f])) return f;
            return -1;
        }

        RectTransform LinhaRt(int f) => (RectTransform)_linhas[f].Voo.transform.parent;
        RectTransform TerminalRt => _terminal.rectTransform;
        RectTransform CashRt => _cash.rectTransform;

        static int QuemArrasta(Sim s)
        {
            foreach (Funcionario f in s.Staff) if (f.Em != Estacao.Seguranca) return f.Id;
            return 0;
        }

        /// <summary>-tela passoN: mostra o passo N agora (foto), sem esperar o gatilho.</summary>
        public void MostraPasso(int n)
        {
            if (n < 1 || n > _passos.Length) return;
            TourLigado = true;
            _forcado = true;   // a foto mostra o passo mesmo que a acao ja tenha acontecido
            _tourT = 0f;
            _atual = n - 1;
            _tour.gameObject.SetActive(true);
            _tour.SetAsLastSibling();
        }

        void ReverTour()
        {
            _feitos.Clear();
            _cofreVisto = false;
            if (TourGrava) { PlayerPrefs.DeleteKey(TourKey); PlayerPrefs.Save(); }
            TourLigado = true;
            _tourComecou = false;
            _atual = -1;
        }

        void PulaTour()
        {
            for (int i = 1; i <= _passos.Length; i++)
                if (_feitos.Add(i)) _g.Diario("tour_step", i, 0);
            Grava();
            _tour.gameObject.SetActive(false);
            _atual = -1;
            _g.Diario("tutorial_completed", 1);
        }

        void Conclui(bool mostrado)
        {
            int n = _atual + 1;
            _feitos.Add(n);
            if (mostrado) _g.Diario("tour_step", n, 1);
            Grava();
            _tour.gameObject.SetActive(false);
            _atual = -1;
            _forcado = false;
            _fimT = Time.unscaledTime;
            if (n == PassosGuiados) _g.Diario("tutorial_completed", 0);
        }

        void Grava()
        {
            if (!TourGrava) return;
            PlayerPrefs.SetString(TourKey, string.Join(",", _feitos));
            PlayerPrefs.Save();
        }

        void PintaTour()
        {
            if (!TourLigado || _passos == null) return;
            if (_cofre.Rt.gameObject.activeSelf) _cofreVisto = true;   // o passo 12 nasce depois que o cofre fecha
            if (!TourAberto)
            {
                if (ModalAberto || Arrastando) return;
                for (int i = 0; i < _passos.Length; i++)
                {
                    if (_feitos.Contains(i + 1)) continue;
                    Passo p = _passos[i];
                    if (p.Feito != null && p.Feito()) { _feitos.Add(i + 1); Grava(); continue; }   // ja fez sozinho: nao ensina o que ele ja sabe
                    if (!p.Quando()) { if (p.Evento) continue; return; }   // um por vez, na ordem; evento que nao veio nao segura os outros
                    if (p.Evento && Time.unscaledTime - _fimT < FolgaEventos) return;
                    if (!_tourComecou) { _tourComecou = true; _g.Diario("tutorial_started"); }
                    _atual = i; _tourT = 0f;
                    _tour.gameObject.SetActive(true);
                    _tour.SetAsLastSibling();
                    break;
                }
                if (!TourAberto) return;
            }
            Passo a = _passos[_atual];
            _tourT += Time.unscaledDeltaTime;
            if (a.Feito != null && !_forcado && a.Feito()) { Conclui(true); return; }
            if (a.Feito == null && TourAuto && !_forcado && _tourT > 2f) { Conclui(true); return; }
            _tourTitulo.text = Textos.TourTitulo[_atual];
            _tourTexto.text = Textos.TourTexto[_atual];
            _tourConta.text = Textos.TourConta(_atual + 1, _passos.Length);
            _tourOk.gameObject.SetActive(a.Feito == null);
            _tourRetrato.enabled = _tourQuem.enabled = a.Fala;
            _tourTitulo.rectTransform.anchorMin = new Vector2(a.Fala ? 0.19f : 0.04f, 0.68f);   // com a Lara, o titulo fica ao lado do retrato
            Rect r = a.Alvo != null ? a.Alvo() : Rect.zero;
            if (r.width <= 0f) r = new Rect(Screen.width * 0.5f, Screen.height * 0.5f, 0f, 0f);
            Buraco(r);
            bool alvoEmCima = r.center.y > Screen.height * 0.5f;
            _tourCard.anchorMin = new Vector2(0.04f, alvoEmCima ? 0.08f : 0.56f);
            _tourCard.anchorMax = new Vector2(0.96f, alvoEmCima ? 0.36f : 0.84f);
            bool temAlvo = r.width > 0f;
            _tourBorda.enabled = _tourSeta.enabled = temAlvo;
            _tourDedo.gameObject.SetActive(temAlvo && a.Arrastar);
            if (!temAlvo) return;
            float w = Screen.width, h = Screen.height, pad = 10f;
            _tourBorda.rectTransform.anchorMin = new Vector2((r.xMin - pad) / w, (r.yMin - pad) / h);
            _tourBorda.rectTransform.anchorMax = new Vector2((r.xMax + pad) / w, (r.yMax + pad) / h);
            float bob = 0.012f * Mathf.Sin(Time.unscaledTime * 6f);
            float sy = alvoEmCima ? (r.yMin - pad) / h - 0.07f + bob : (r.yMax + pad) / h + 0.01f + bob;
            _tourSeta.rectTransform.anchorMin = new Vector2(r.center.x / w - 0.04f, sy);
            _tourSeta.rectTransform.anchorMax = new Vector2(r.center.x / w + 0.04f, sy + 0.06f);
            _tourSeta.rectTransform.localEulerAngles = new Vector3(0f, 0f, alvoEmCima ? 180f : 0f);
            if (a.Arrastar)
            {
                // o dedo mostra o gesto do TA-2: do funcionario ate a seguranca, em loop
                Rect d = M.ZonaTela(Estacao.Seguranca);
                float k = Mathf.Repeat(Time.unscaledTime * 0.6f, 1f);
                Vector2 tela = Vector2.Lerp(r.center, d.center, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k * 1.3f)));
                _tourDedo.anchorMin = _tourDedo.anchorMax = new Vector2(tela.x / w, tela.y / h);
            }
        }

        /// <summary>Retangulo do alvo em px de tela (canvas overlay: os cantos de mundo ja sao px).</summary>
        static Rect NaTela(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        void Buraco(Rect r)
        {
            float w = Screen.width, h = Screen.height;
            float x0 = Mathf.Clamp01(r.xMin / w), x1 = Mathf.Clamp01(r.xMax / w), y0 = Mathf.Clamp01(r.yMin / h), y1 = Mathf.Clamp01(r.yMax / h);
            Poe(_veu[0], new Vector2(0f, y1), Vector2.one);
            Poe(_veu[1], Vector2.zero, new Vector2(1f, y0));
            Poe(_veu[2], new Vector2(0f, y0), new Vector2(x0, y1));
            Poe(_veu[3], new Vector2(x1, y0), new Vector2(1f, y1));
        }

        static void Poe(Image i, Vector2 a, Vector2 b)
        {
            i.rectTransform.anchorMin = a; i.rectTransform.anchorMax = b;
            i.rectTransform.offsetMin = i.rectTransform.offsetMax = Vector2.zero;
        }
    }
}
