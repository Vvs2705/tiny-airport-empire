using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// O botao de velocidade na barra de cima, ao lado do relogio (GDD de direcao §5): cada toque passa de 1x a 2x, 3x e de volta a 1x. Na pausa
    /// operacional (o II, Game.Pausa) ele fica amarelo com o triangulo de continuar: o relogio e o mundo param, e as telas e as melhorias
    /// seguem abertas. O 3x roda mais passos fixos por quadro (Game.Anda), sem mudar o passo.
    /// </summary>
    public sealed partial class Hud
    {
        Button _vel;
        Image _velFace, _velPlay;
        Text _velTexto;
        int _velPintada = -1;

        void BuildVelocidade(RectTransform topo)
        {
            _vel = Art.NewButton(topo, Textos.Vezes(1), 44, Art.AzulLuz, Vector2.up, Vector2.up, () => _g.ProximaVelocidade());
            Art.Fixa((RectTransform)_vel.transform, Vector2.up, new Vector2(490f, 0f), new Vector2(112f, 112f));
            Art.Toque(_vel, 0f, 18f);
            _velFace = (Image)_vel.targetGraphic;
            _velTexto = Art.Rotulo(_vel);
            _velPlay = Art.Img(_vel.transform, "Continuar", Art.Triangle(), Art.Tinta, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Art.Fixa(_velPlay.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(4f, 4f), new Vector2(56f, 56f));
            _velPlay.rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f);   // o triangulo aponta para cima; o continuar, para a direita
        }

        void PintaVelocidade()
        {
            int v = _g.Velocidade;
            if (v == _velPintada) return;
            _velPintada = v;
            _velFace.color = v == 0 ? Art.Placa : Art.AzulLuz;
            _velTexto.text = v == 0 ? "" : Textos.Vezes(v);
            _velPlay.enabled = v == 0;
        }
    }
}
