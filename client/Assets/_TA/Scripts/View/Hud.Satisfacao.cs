using System;
using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// Satisfacao do aeroporto no HUD (frente G, Direcao §8.4): o coracao na cor da carinha com a media de quem saiu e os 3 motivos que mais pesam,
    /// cada um com o icone e o peso medio. Tocar no coracao abre o painel de operacoes (Hud.Painel.cs); tocar num motivo leva a camera a sala dele e abre a ficha da sala (ou abre o voo atrasado) e diz o motivo,
    /// onde pesa e as reclamacoes do dia. Fica a direita, logo acima da ficha, por cima do mundo; some ate o 1o passageiro sair.
    /// </summary>
    public sealed partial class Hud
    {
        const float AltSat = 96f, LargSatNota = 152f;
        /// <summary>Largura do selo e de cada motivo (px da referencia).</summary>
        public const float LargSat = 560f, LargSatMotivo = 132f;

        RectTransform _sat;
        Image _satCoracao;
        Text _satNota;
        readonly Button[] _satBtn = new Button[3];
        readonly Image[] _satIcone = new Image[3];
        readonly Text[] _satPeso = new Text[3];
        readonly Fator[] _satVez = new Fator[3];

        void BuildSatisfacao()
        {
            Image fundo = Art.Panel(_root, "Satisfacao", Art.A(Art.Tinta, 0.9f), Vector2.zero, Vector2.zero);
            fundo.raycastTarget = true;   // o toque no selo nao vai para o mundo
            _sat = fundo.rectTransform;
            Art.Fixa(_sat, Vector2.right, new Vector2(-16f, AltBase + 14f + AltFicha + 10f), new Vector2(LargSat, AltSat));
            Button total = _sat.gameObject.AddComponent<Button>();
            total.targetGraphic = fundo;
            total.onClick.AddListener(() => AbreTela("painel"));   // o coracao e a nota abrem o painel de operacoes, com o relatorio do dia (frente I)
            _satCoracao = Icone(_sat, "Coracao", Art.Heart(), Art.Good, 14f, 18f, 60f);
            _satNota = Texto(_sat, "Nota", Tipo.Titulo, Color.white, 80f, 8f, LargSatNota - 80f, 80f);
            for (int k = 0; k < 3; k++)
            {
                int kk = k;
                Button b = _satBtn[k] = Art.NewButton(_sat, "", MenorTexto, Art.AzulLuz, Vector2.up, Vector2.up, () => AbreMotivo(kk));
                Art.Fixa((RectTransform)b.transform, Vector2.up, new Vector2(LargSatNota + k * (LargSatMotivo + 4f), -8f), new Vector2(LargSatMotivo, AltSat - 16f));
                _satIcone[k] = Icone(b.transform, "Icone", Art.Relogio(), Art.Suave, 8f, 18f, 44f);
                _satPeso[k] = Texto(b.transform, "Peso", Tipo.Secao, Color.white, 56f, 6f, LargSatMotivo - 60f, AltSat - 28f);
            }
            _sat.gameObject.SetActive(false);
        }

        static int SatNota(Sim s) => (int)Math.Round(s.SatMedia, MidpointRounding.AwayFromZero);

        static Color CorSat(Carinha c) => c == Carinha.Contente ? Art.Good : c == Carinha.Brava ? Art.Bad : Art.Warn;

        static Sprite IconeFator(Fator f)
        {
            switch (f)
            {
                case Fator.Espera: return Art.Relogio();
                case Fator.Limpeza: return Art.Estrela();
                case Fator.Banheiro: return Art.Gota();
                case Fator.Pontualidade: return Art.Aviao();
                default: return Art.Pessoa();
            }
        }

        void PintaSatisfacao(Sim s)
        {
            bool tem = s.SatAmostras > 0;
            _sat.gameObject.SetActive(tem);
            if (!tem) return;
            _satCoracao.color = CorSat(s.CarinhaDe(SatNota(s)));
            _satNota.text = SatNota(s).ToString(System.Globalization.CultureInfo.InvariantCulture);
            for (int k = 0; k < 3; k++)
            {
                Fator f = _satVez[k] = s.SatTop(k);
                double w = s.SatFator(f);
                Color cor = w <= -0.5 ? Art.Bad : w >= 0.5 ? Art.Good : Art.Suave;
                _satIcone[k].sprite = IconeFator(f);
                _satIcone[k].color = cor;
                _satPeso[k].text = Textos.SatPeso(w);
                _satPeso[k].color = cor;
            }
        }

        /// <summary>O motivo k do selo: a camera vai a sala (e a ficha dela abre) ou o voo atrasado abre; o aviso diz onde pesa e as reclamacoes.</summary>
        void AbreMotivo(int k)
        {
            Sim s = S;
            Fator f = _satVez[k];
            int onde = s.SatOnde(f);
            string nome;
            if (f == Fator.Pontualidade)
            {
                Voo v = s.Voos[onde];
                nome = v != null ? Textos.Voo(v.Id) : "";
                AbreVoo(onde);
            }
            else if ((f == Fator.Limpeza || f == Fator.Banheiro) && onde == (int)Area.Banheiro)
            {
                nome = Textos.Banheiro;
                M.FocaBanheiro();
            }
            else
            {
                nome = Textos.Estacao[onde];   // Area e Estacao tem o mesmo indice nas 6 salas com passageiro
                M.Foca((Estacao)onde);
                Seleciona(Sel.Estacao, onde);
            }
            Toast(Textos.SatMotivo(f, nome, s.SatFator(f), s.Reclamacoes(f)));
        }
    }
}
