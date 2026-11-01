using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>O que o aviso mostra agora. Sai so do estado do Sim: o perigo acabou, o aviso some.</summary>
    public sealed class AvisoPerigo
    {
        public Sprite Icone;
        public Color Cor;
        /// <summary>Titulo com a contagem; Texto em 2 linhas: causa e duracao, depois o que fazer (GDD §5.8).</summary>
        public string Titulo, Texto;
        /// <summary>Saidas com botao; Segurar e nao fazer nada, sem botao.</summary>
        public Opcao Acao = Opcao.Segurar, Acao2 = Opcao.Segurar;
        /// <summary>O toque abre este aviao (>= 0) ou leva a camera a esta sala.</summary>
        public int Aviao = -1;
        public Estacao Sala;
    }

    /// <summary>
    /// Aviso de perigo logo abaixo do quadro: incidente e clima desde o aviso de 30 s (GDD §5.8, CONTRATO_FASE_C4 §6.5) e falha de aviao quando
    /// acontece, porque o aviso previo dela e o desgaste na nota do quadro. O cenario do clima e do Mundo.Clima.cs; aqui so a interface.
    /// </summary>
    public sealed partial class Hud
    {
        const float AltAlerta = 164f;
        /// <summary>Largura do texto do aviso, em px; o teste confere que toda linha cabe.</summary>
        public const float LargAviso = 534f;

        RectTransform _alerta;
        Image _alertaPlaca, _alertaGlifo;
        Text _alertaTitulo, _alertaTexto;
        readonly BotaoPreco[] _alertaBtn = new BotaoPreco[2];
        AvisoPerigo _alertaAgora;
        float _toastY;

        void BuildAlerta()
        {
            Image fundo = Art.Panel(_root, "Alerta", Art.A(Art.Tinta, 0.94f), Vector2.up, Vector2.one);
            _alerta = fundo.rectTransform;
            _alerta.offsetMin = new Vector2(16f, -AltCima - 8f - AltAlerta); _alerta.offsetMax = new Vector2(-16f, -AltCima - 8f);
            Button b = _alerta.gameObject.AddComponent<Button>();
            b.targetGraphic = fundo;
            b.onClick.AddListener(IrAoPerigo);
            _alerta.gameObject.AddComponent<Aperta>();
            _alertaPlaca = Art.NovaPlaca(_alerta, "Placa", Art.Alerta(), Art.Warn, Art.Tinta);
            _alertaGlifo = _alertaPlaca.transform.GetChild(0).GetComponent<Image>();
            Art.Fixa(_alertaPlaca.rectTransform, Vector2.up, new Vector2(14f, -30f), new Vector2(104f, 104f));
            _alertaTitulo = Texto(_alerta, "Titulo", Tipo.Nome, Art.Placa, 132f, 8f, LargAviso, 54f);
            _alertaTexto = Texto(_alerta, "Texto", Tipo.Legenda, Art.Ink, 132f, 62f, LargAviso, 94f, TextAnchor.UpperLeft);
            for (int k = 0; k < 2; k++)
            {
                int kk = k;
                _alertaBtn[k] = Art.NovoBotaoPreco(_alerta, Vector2.zero, Vector2.zero, () => Age(kk));
                Art.Fixa((RectTransform)_alertaBtn[k].B.transform, Vector2.up, new Vector2(858f - k * 184f, -8f), new Vector2(176f, AltAtalho));
                _alertaBtn[k].Rotulo.resizeTextMinSize = MenorTexto;
                _alertaBtn[k].Valor.fontSize = 36;
            }
            _alerta.gameObject.SetActive(false);
            _toastY = _toastFundo.anchoredPosition.y;
        }

        void PintaAlerta(Sim s)
        {
            AvisoPerigo p = _alertaAgora = AvisoDaVez(s);
            _alerta.gameObject.SetActive(p != null);
            _toastFundo.anchoredPosition = new Vector2(0f, _toastY - (p != null ? AltAlerta + 8f : 0f));
            if (p == null) return;
            _alertaPlaca.color = p.Cor;
            _alertaGlifo.sprite = p.Icone;
            _alertaTitulo.text = p.Titulo;
            _alertaTexto.text = p.Texto;
            PintaSaida(_alertaBtn[0], p.Acao, p, s);
            PintaSaida(_alertaBtn[1], p.Acao2, p, s);
        }

        static void PintaSaida(BotaoPreco b, Opcao o, AvisoPerigo p, Sim s)
        {
            b.B.gameObject.SetActive(o != Opcao.Segurar);
            if (o == Opcao.Reparar) b.Preco(Textos.Reparar, p.Aviao >= 0 ? s.T.PaneCusto : s.T.ReparoCusto, s.Cash);
            else if (o != Opcao.Segurar) b.Secundario(o == Opcao.Adiantar ? Textos.Adiantar : o == Opcao.Atrasar ? Textos.Atrasar : Textos.Priorizar);
        }

        void Age(int k)
        {
            AvisoPerigo p = _alertaAgora;
            if (p == null) return;
            Opcao o = k == 0 ? p.Acao : p.Acao2;
            if (o == Opcao.Adiantar) _g.Adianta();
            else if (o == Opcao.Priorizar) _g.Prioriza();
            else if (o == Opcao.Atrasar) { if (!_g.Atrasa()) Toast(Textos.AtrasoNao); }
            else if (o == Opcao.Reparar && !(p.Aviao >= 0 ? _g.ReparaAviao(p.Aviao) : _g.Repara())) Toast(Textos.MecanicoOcupado);
        }

        void IrAoPerigo()
        {
            AvisoPerigo p = _alertaAgora;
            if (p == null) return;
            if (p.Aviao >= 0) AbreVoo(p.Aviao); else M.Foca(p.Sala);
        }

        /// <summary>Um aviso por vez: a falha de aviao vem primeiro, porque ja aconteceu; o incidente ainda da tempo.</summary>
        public static AvisoPerigo AvisoDaVez(Sim s)
        {
            foreach (TA.Core.Aviao a in s.Avioes)
            {
                AvisoPerigo p = DoAviao(s, a);
                if (p != null) return p;
            }
            return s.Inc != null ? DoIncidente(s, s.Inc) : null;
        }

        static int Segundos(long passos) => Mathf.CeilToInt(passos / (float)Sim.Hz);

        public static AvisoPerigo DoAviao(Sim s, TA.Core.Aviao a)
        {
            string nome = Textos.AviaoNome[a.Id];
            if (a.Pane && a.NoPatio && a.Hangar == 0)
            {
                bool consertando = a.Check > 0 && !a.Inspecao;
                return new AvisoPerigo { Icone = Art.Alerta(), Cor = Art.Bad, Titulo = Textos.EmPane(nome), Aviao = a.Id, Acao = consertando ? Opcao.Segurar : Opcao.Reparar,
                    Texto = Textos.CausaPane + "\n" + (consertando ? Textos.Consertando : Textos.FazerPane) };
            }
            if (a.Retorno > 0 && s.Steps >= a.Retorno)
                return new AvisoPerigo { Icone = Art.Aviao(), Cor = Art.Warn, Titulo = Textos.Voltando(nome, Segundos(a.Chega - s.Steps)), Aviao = a.Id,
                    Texto = Textos.PousoPrioridade + "\n" + Textos.Prontidao };
            if (a.SaiEm > s.Steps)
                return new AvisoPerigo { Icone = Art.Aviao(), Cor = Art.Warn, Titulo = Textos.Abortou(nome), Aviao = a.Id,
                    Texto = Textos.SaiEm(Segundos(a.SaiEm - s.Steps)) + "\n" + Textos.NadaAFazer };
            return null;
        }

        public static AvisoPerigo DoIncidente(Sim s, Incidente i)
        {
            Clima c = s.ClimaDe(i.Tipo);
            if (i.Resolvido && c == null) return null;   // reparou ou priorizou: acabou
            Voo v = s.Voos[i.Vaga];
            bool antes = s.Steps < i.Inicio;
            string nome = i.Tipo == TipoIncidente.Falha ? Textos.FalhaEm((int)i.Estacao) : Textos.Perigo[(int)i.Tipo];
            var p = new AvisoPerigo { Icone = IconePerigo(i.Tipo), Cor = Art.Warn,
                Titulo = antes ? Textos.PerigoEm(nome, Segundos(i.Inicio - s.Steps)) : Textos.PerigoResta(nome, Segundos(i.Fim - s.Steps)) };
            if (c != null)
            {
                bool decidido = i.Resolvido || v.Partiu;
                p.Aviao = i.Vaga;
                p.Acao = decidido ? Opcao.Segurar : antes ? Opcao.Adiantar : Opcao.Atrasar;
                p.Acao2 = !decidido && antes ? Opcao.Atrasar : Opcao.Segurar;
                p.Texto = Textos.CausaClima(c) + "\n" + (decidido ? Textos.JaDecidiu : antes ? Textos.FazerAdiantar(v.Id) : Textos.FazerAtrasar(v.Id));
            }
            else if (i.Tipo == TipoIncidente.Falha)
            {
                p.Sala = i.Estacao;
                p.Acao = s.Reparo > 0 ? Opcao.Segurar : Opcao.Reparar;
                p.Texto = Textos.CausaFalha(s.T.FalhaFator, s.T.FalhaSeg) + "\n" + (s.Reparo > 0 ? Textos.Consertando : Textos.FazerReparar);
            }
            else if (i.Tipo == TipoIncidente.Pico)
            {
                p.Sala = Estacao.CheckIn;
                p.Texto = Textos.CausaGrupo(v.Id) + "\n" + Textos.FazerReforcar((int)Estacao.CheckIn);
            }
            else
            {
                p.Sala = Estacao.Seguranca;
                p.Acao = Opcao.Priorizar;
                p.Texto = Textos.CausaGrupo(v.Id) + "\n" + Textos.FazerPriorizar;
            }
            return p;
        }

        static Sprite IconePerigo(TipoIncidente t)
        {
            switch (t)
            {
                case TipoIncidente.FrenteFria: return Art.Gota();
                case TipoIncidente.Falha: return Art.Chave();
                case TipoIncidente.Pico: return Art.Pessoas();
                case TipoIncidente.Conexao: return Art.Relogio();
                case TipoIncidente.Temporal: return Art.Raio();
                default: return Art.Alerta();
            }
        }
    }
}
