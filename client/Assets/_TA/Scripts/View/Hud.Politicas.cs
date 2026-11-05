using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Politicas do diretor (frente ta-politicas, Sim.Politicas.cs): a folha aberta pelo 1o cartao da tela de Melhorias (e por -tela politicas). Cada
    /// politica tem o interruptor (o botao da direita), o X da lista fixa (o botao do meio passa ao proximo; a barra mostra qual dos 3) e a trava
    /// escrita. Cada acao sai num aviso curto (AvisoPolitica, pelo Game) e fica no registro de ocorrencias.
    /// </summary>
    public sealed partial class Hud
    {
        Modal _politicas;
        Item _politicasEntrada;
        readonly Item[] _polItem = new Item[Sim.Politicas];
        readonly BotaoPreco[] _polX = new BotaoPreco[Sim.Politicas];
        readonly BarraNivel[] _polBarra = new BarraNivel[Sim.Politicas];

        void BuildPoliticas(RectTransform canvas)
        {
            _politicas = NovaFolhaModal(canvas, "Politicas", Textos.Politicas, Art.Chave(), 1300f);
            RectTransform lista = Art.NovaLista(_politicas.F.Corpo, Vector2.zero, Vector2.one);
            Art.Margem((RectTransform)lista.parent, 24f, 0f, 24f, 16f);
            Sprite[] icone = { Art.Chave(), Art.Gota(), Art.Pessoas() };
            for (int k = 0; k < Sim.Politicas; k++)
            {
                int kk = k;
                Item it = NovoItem(lista, "Politica" + k, icone[k], Art.EstacaoCor[k == 0 ? 6 : k == 1 ? 7 : 1], () => _g.LigaPolitica((Politica)kk, !S.PolLigada[kk]), 250f, 20f, 166f);
                it.Nome.text = Textos.PoliticaNome[k];
                _polX[k] = BotaoDoCartao(it.Rt, () => _g.RegulaPolitica((Politica)kk, (S.PolNivel[kk] + 1) % S.T.PoliticaX[kk].Length));
                Art.Fixa((RectTransform)_polX[k].B.transform, new Vector2(1f, 0.5f), new Vector2(-286f, 0f), new Vector2(150f, 146f));
                _polBarra[k] = Art.NovaBarraNivel(it.Rt, "X", S.T.PoliticaX[k].Length, Vector2.up, Vector2.one);
                var r = (RectTransform)_polBarra[k].Pai;
                r.offsetMin = new Vector2(172f, -226f); r.offsetMax = new Vector2(-472f, -206f);
                _polItem[k] = it;
            }
        }

        /// <summary>O 1o cartao da tela de Melhorias: quantas estao ligadas e o botao que abre a folha.</summary>
        void EntradaPoliticas(RectTransform lista)
        {
            _politicasEntrada = NovoItem(lista, "Politicas", Art.Chave(), Art.EstacaoCor[6], () => Abre(_politicas), 186f);
            _politicasEntrada.Nome.text = Textos.Politicas;
            _politicasEntrada.Btn.Secundario(Textos.Abrir);
        }

        void PintaPoliticas(Sim s)
        {
            if (_melhorias.Rt.gameObject.activeSelf)
            {
                int n = 0;
                foreach (bool b in s.PolLigada) if (b) n++;
                _politicasEntrada.Linha.text = Textos.PoliticasLigadas(n, Sim.Politicas);
            }
            if (!_politicas.Rt.gameObject.activeSelf) return;
            for (int k = 0; k < Sim.Politicas; k++)
            {
                Item it = _polItem[k];
                int x = s.PoliticaX((Politica)k);
                Interruptor(it, s.PolLigada[k]);
                _polX[k].Secundario(Textos.PoliticaValor(k, x));
                _polBarra[k].Pinta(s.PolNivel[k] + 1, s.PolLigada[k] ? Art.Compra : Art.Azul);
                it.Linha.text = Textos.PoliticaRegra(k, x);
                it.Info.text = k == (int)Politica.Estoque && s.PolLigada[k] ? Textos.PoliticaGasto(s.PolGasto, x) : Textos.PoliticaTrava[k];
            }
        }

        /// <summary>O aviso de cada acao (Ev.PoliticaAgiu), so do evento e do estado: o que a politica fez e por que.</summary>
        public static string AvisoPolitica(Sim s, Evento e)
        {
            switch ((Politica)e.A)
            {
                case Politica.Hangar: return Textos.PoliticaHangar(Textos.AviaoNome[Mathf.Clamp(e.B, 0, 1)], e.C);
                case Politica.Estoque: return Textos.PoliticaEstoque(Mathf.Clamp(e.B, 0, Sim.Suprimentos - 1), e.C, s.Tanque((Suprimento)Mathf.Clamp(e.B, 0, Sim.Suprimentos - 1)));
                default: return Textos.PoliticaReforco(e.B >= 0 && e.B < s.Staff.Count ? Textos.Nome(s.Staff[e.B]) : "", Mathf.Clamp(e.C, 0, 7));
            }
        }
    }
}
