using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Painel de operacoes com o relatorio do dia (frente I, GDD §4.1; Direcao §15), modal como as outras (segura o relogio). No alto, o saldo de
    /// hoje, o Cash e o terminal; na lista, os alertas do critico ao informativo (cada um com o botao que leva a sala, ao banheiro, ao voo ou ao
    /// aviao, Direcao §15), as 2 partidas da onda com o portao e a situacao, o patio e as chegadas, o relatorio do dia (receita, a conta prevista,
    /// o saldo e o dia de ontem), a satisfacao com os 3 motivos e as contas do dia, e as ocorrencias de hoje, da mais nova para a mais antiga. So
    /// le o nucleo (Sim.Painel.cs); abre pela pausa, pelo selo da satisfacao e pela flag -tela painel.
    /// </summary>
    public sealed partial class Hud
    {
        const int PainelAlertas = 6, PainelOcorrencias = 5;

        Modal _painel;
        Resumo _painelResumo;
        Item _pnCalmo, _pnSolo, _pnDia, _pnSat, _pnSemOcorrencia;
        readonly Item[] _pnAlerta = new Item[PainelAlertas], _pnVoo = new Item[2], _pnOcorr = new Item[PainelOcorrencias];
        readonly Alerta[] _pnAlvo = new Alerta[PainelAlertas];
        float _pnPinta;

        void BuildPainel(RectTransform canvas)
        {
            _painel = NovaFolhaModal(canvas, "Painel", Textos.Painel, Art.Bilhete(), 1600f);
            RectTransform l = NovoResumo(_painel, Textos.SaldoHoje, Art.Moeda(), Art.Coin, out _painelResumo);
            _pnCalmo = NovoItem(l, "SemAlerta", Art.Estrela(), Art.Good, null, 150f, 24f);
            _pnCalmo.Nome.text = Textos.TudoEmOrdem; _pnCalmo.Linha.text = Textos.SemAlertas;
            for (int k = 0; k < PainelAlertas; k++)
            {
                int kk = k;
                _pnAlerta[k] = NovoItem(l, "Alerta" + k, Art.Relogio(), Art.Bad, () => AbreAlerta(kk));
                _pnAlerta[k].Btn.Secundario(Textos.Abrir);
            }
            for (int f = 0; f < 2; f++)
            {
                int ff = f;
                _pnVoo[f] = NovoItem(l, "Voo" + f, Art.Mala(), Art.VooCor[f], () => { Fecha(_painel); AbreVoo(ff); });
                _pnVoo[f].Btn.Secundario(Textos.Abrir);
            }
            _pnSolo = NovoItem(l, "Patio", Art.Aviao(), Art.Azul, null);
            _pnSolo.Nome.text = Textos.AvioesEChegadas;
            _pnDia = NovoItem(l, "Relatorio", Art.Moeda(), Art.Coin, null);
            _pnDia.Nome.text = Textos.RelatorioDia;
            _pnSat = NovoItem(l, "Satisfacao", Art.Heart(), Art.Good, null);
            _pnSemOcorrencia = NovoItem(l, "SemOcorrencia", Art.Bilhete(), Art.Vazio, null, 150f, 24f);
            _pnSemOcorrencia.Nome.text = Textos.SemOcorrencias;
            for (int k = 0; k < PainelOcorrencias; k++) _pnOcorr[k] = NovoItem(l, "Ocorrencia" + k, Art.Bilhete(), Art.Suave, null);
        }

        void AbrePainel()
        {
            _pnPinta = 0f;
            Abre(_painel);
        }

        static Color CorGravidade(Gravidade g) => g == Gravidade.Critico ? Art.Bad : g == Gravidade.Importante ? Art.Warn : Art.AzulLuz;

        static Sprite IconeAlerta(Alerta a)
        {
            switch (a.Alvo)
            {
                case Alvo.Estacao: return Art.IconeEstacao(a.Indice);
                case Alvo.Area: return a.Indice == (int)Area.Banheiro ? Art.Gota() : Art.IconeEstacao(a.Indice);
                case Alvo.Caixa: return Art.Moeda();
                default: return Art.Aviao();
            }
        }

        /// <summary>O botao do alerta k: a camera vai a sala (e a ficha dela abre), ao banheiro, ou o voo abre; o caixa diz o que faltou.</summary>
        void AbreAlerta(int k)
        {
            Alerta a = _pnAlvo[k];
            Fecha(_painel);
            switch (a.Alvo)
            {
                case Alvo.Estacao: M.Foca((Estacao)a.Indice); Seleciona(Sel.Estacao, a.Indice); break;
                case Alvo.Area:
                    if (a.Indice == (int)Area.Banheiro) M.FocaBanheiro();
                    else { M.Foca((Estacao)a.Indice); Seleciona(Sel.Estacao, a.Indice); }   // Area e Estacao tem o mesmo indice nas 6 salas com passageiro
                    break;
                case Alvo.Voo: case Alvo.Aviao: AbreVoo(a.Indice); break;
                default: Toast(Textos.AlertaInfo(a)); break;
            }
        }

        void PintaPainel(Sim s)
        {
            if (!_painel.Rt.gameObject.activeSelf) return;
            if ((_pnPinta -= Time.unscaledDeltaTime) > 0f) return;   // 4 vezes por segundo: os alertas e o dia montam listas
            _pnPinta = 0.25f;
            _painelResumo.Pinta(s, Textos.Saldo(s.SaldoHoje));

            List<Alerta> al = s.Alertas();
            _pnCalmo.Rt.gameObject.SetActive(al.Count == 0);
            for (int k = 0; k < PainelAlertas; k++)
            {
                Item it = _pnAlerta[k];
                bool tem = k < al.Count;
                it.Rt.gameObject.SetActive(tem);
                if (!tem) continue;
                Alerta a = _pnAlvo[k] = al[k];
                it.Placa.color = CorGravidade(a.Gravidade);
                it.Glifo.sprite = IconeAlerta(a);
                it.Nome.text = Textos.AlertaNome[(int)a.Tipo];
                it.Linha.text = Textos.GravidadeNome[(int)a.Gravidade] + " · " + Textos.AlertaLugar(a, s);
                it.Info.text = Textos.AlertaInfo(a);
            }

            for (int f = 0; f < 2; f++)
            {
                Item it = _pnVoo[f];
                LinhaVoo lv = s.Partida(f);
                Voo v = s.Voos[f];
                bool voa = v != null && lv.Situacao != SituacaoVoo.SemVoo;
                it.Nome.text = voa ? Textos.Voo(v.Id) + " · " + Textos.DestinoDe(v.Id) : Textos.AviaoNome[f];
                it.Linha.text = Textos.Portao(f + 1) + " · " + Textos.SituacaoNome[(int)lv.Situacao] + (lv.EmRisco ? " · " + Textos.EmRisco : "");
                it.Linha.color = lv.Situacao == SituacaoVoo.Atrasado || lv.EmRisco ? Art.Perigo : Art.Fraco;
                it.Info.text = voa ? Textos.ABordo(v) + " · " + Textos.Hora(v.Onda * (double)s.T.Onda + v.FechaPasso * (double)Sim.Step) : "";
            }

            int c = s.Congestionada;
            _pnSolo.Linha.text = Textos.EmSolo(s.AvioesEmSolo, s.Avioes.Length) + " · " + Textos.Chegando(s.AvioesChegando);
            _pnSolo.Info.text = Textos.PaxChegando(s.ChegantesNoTerminal) + (c >= 0 ? " · " + Textos.MaiorFila(c, s.Estacoes[c].Fila.Count) : "");

            _pnDia.Linha.text = Textos.ReceitaDespesa(s.ReceitaHoje, s.DespesaHoje);
            _pnDia.Info.text = Textos.SaldoLinha(s.SaldoHoje) + (s.Ontem is DiaConta ontem ? " · " + Textos.Ontem(ontem) : "");
            _pnDia.Placa.color = s.Pausado ? Art.Bad : Art.Coin;

            bool sat = s.SatAmostras > 0;
            int nota = sat ? SatNota(s) : 0;
            _pnSat.Nome.text = sat ? Textos.Satisfacao + " " + nota : Textos.Satisfacao;
            _pnSat.Placa.color = sat ? CorSat(s.CarinhaDe(nota)) : Art.Vazio;
            if (sat)
            {
                string m = "";
                for (int k = 0; k < 3; k++)
                {
                    Fator f = s.SatTop(k);
                    m += (k > 0 ? " · " : "") + Textos.FatorNome[(int)f] + " " + Textos.SatPeso(s.SatFator(f));
                }
                _pnSat.Linha.text = m;
            }
            else _pnSat.Linha.text = Textos.SemSatisfacao;
            _pnSat.Info.text = Textos.DoDia(s.ReclamacoesHoje, s.AtrasosHoje, s.PerdidosHoje);

            List<Ocorrencia> oc = s.OcorrenciasHoje();
            _pnSemOcorrencia.Rt.gameObject.SetActive(oc.Count == 0);
            for (int k = 0; k < PainelOcorrencias; k++)
            {
                Item it = _pnOcorr[k];
                bool tem = k < oc.Count;
                it.Rt.gameObject.SetActive(tem);
                if (!tem) continue;
                Ocorrencia o = oc[oc.Count - 1 - k];   // a mais nova primeiro
                it.Placa.color = o.Eficaz ? Art.Good : o.Aberta ? Art.Warn : Art.Bad;
                it.Nome.text = Textos.OcorrenciaNome[(int)o.Tipo] + " · " + Textos.HoraDoDia(s.HoraDe(o.Passo));
                it.Linha.text = Textos.Explica(o);
                it.Info.text = Textos.OcorrenciaResultado(o);
            }
        }
    }
}
