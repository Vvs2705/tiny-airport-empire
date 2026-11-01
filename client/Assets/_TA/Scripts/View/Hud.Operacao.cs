using System;
using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// Telas da operacao (Fase C e C2), modais como as outras (seguram o relogio): o voo (toque no quadro de partidas ou no aviao: manutencao,
    /// com checagem rapida ou hangar, e a troca da escala), a tripulacao (quadro de pilotos e comissarios, contratacao), o patio (equipe,
    /// upgrades por servico e os 2 avioes) e a equipe (os 6 perfis, com desbloqueio pelo marco). So leem o Sim e mandam intencao pelo Game.
    /// </summary>
    public sealed partial class Hud
    {
        const int MaxTrip = 6;   // Tripulante.Nomes

        Modal _voo, _trip, _patio, _equipe;
        int _vooK;
        Text _vooEstado, _vooDesgaste, _vooMotivo, _vooEscala;
        Image _vooBarra;
        Button _checar, _hangar;
        readonly Button[,] _trocaBtn = new Button[2, MaxTrip - 1];
        readonly int[,] _trocaId = new int[2, MaxTrip - 1];
        readonly Text[,] _tripLinha = new Text[2, MaxTrip];
        readonly Button[] _tripHire = new Button[2];
        Text _patioEquipe;
        Button _patioHire;
        readonly Button[] _patioAviao = new Button[2];
        readonly Button[,] _patioBtn = new Button[4, 2];
        readonly Text[] _equipeNome = new Text[Sim.MaxEquipe], _equipeCargo = new Text[Sim.MaxEquipe], _equipeInfo = new Text[Sim.MaxEquipe];
        readonly Image[] _equipePlaca = new Image[Sim.MaxEquipe];
        readonly BotaoPreco[] _equipeBtn = new BotaoPreco[Sim.MaxEquipe];
        Numero _equipeTime, _equipeCash, _equipeNivel;
        BarraNivel _equipeBarra;

        void BuildOperacao(RectTransform canvas)
        {
            _voo = NovoModal(canvas, "Voo", 0.05f, 0.95f);
            RectTransform pv = PainelDe(_voo);
            _voo.Corpo.text = "";
            _vooEstado = Art.NewText(pv, "Estado", 36, new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.91f), TextAnchor.MiddleLeft);
            _vooDesgaste = Art.NewText(pv, "Desgaste", 36, new Vector2(0.05f, 0.83f), new Vector2(0.5f, 0.87f), TextAnchor.MiddleLeft);
            Image fundo = Art.Img(pv, "BarraFundo", Art.Square(), Art.A(Art.Ink, 0.15f), new Vector2(0.5f, 0.84f), new Vector2(0.95f, 0.86f));
            fundo.preserveAspect = false;
            _vooBarra = Art.Img(fundo.transform, "Barra", Art.Square(), Art.Good, Vector2.zero, Vector2.one);
            _vooBarra.preserveAspect = false;
            _checar = Art.NewButton(pv, "", 30, Art.EstacaoCor[(int)Estacao.Manutencao], new Vector2(0.04f, 0.72f), new Vector2(0.49f, 0.82f), () => { int k = _vooK; Fecha(_voo); _g.Checagem(k); });
            _hangar = Art.NewButton(pv, "", 30, Art.EstacaoCor[3], new Vector2(0.51f, 0.72f), new Vector2(0.96f, 0.82f), () => { int k = _vooK; Fecha(_voo); _g.Hangar(k); });
            _vooMotivo = Art.NewText(pv, "Motivo", 28, new Vector2(0.05f, 0.63f), new Vector2(0.95f, 0.715f), TextAnchor.UpperLeft);
            Text esc = Art.NewText(pv, "EscalaTitulo", 34, new Vector2(0.05f, 0.59f), new Vector2(0.95f, 0.63f), TextAnchor.MiddleLeft);
            esc.text = Textos.Escala; esc.color = Art.Dim;
            _vooEscala = Art.NewText(pv, "Escala", 30, new Vector2(0.05f, 0.545f), new Vector2(0.95f, 0.59f), TextAnchor.MiddleLeft);
            for (int f = 0; f < 2; f++)
                for (int i = 0; i < MaxTrip - 1; i++)
                {
                    int ff = f, ii = i;
                    float y1 = 0.54f - i * 0.075f;
                    _trocaBtn[f, i] = Art.NewButton(pv, "", 28, f == 0 ? Art.EstacaoCor[0] : Art.EstacaoCor[2], new Vector2(0.04f + f * 0.47f, y1 - 0.07f), new Vector2(0.49f + f * 0.47f, y1),
                        () => { Voo v = S.Voos[_vooK]; if (v != null) _g.Escala(v.Id, _trocaId[ff, ii]); });
                }
            Art.NewButton(pv, Textos.Fechar, 40, Art.Off, new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.13f), () => Fecha(_voo));

            _trip = NovoModal(canvas, "Tripulacao", 0.08f, 0.92f);
            _trip.Titulo.text = Textos.Tripulacao;
            _trip.Corpo.text = "";
            RectTransform pt = PainelDe(_trip);
            for (int f = 0; f < 2; f++)
            {
                float x0 = 0.03f + f * 0.48f, x1 = 0.49f + f * 0.48f;
                Text t = Art.NewText(pt, "Funcao" + f, 34, new Vector2(x0, 0.8f), new Vector2(x1, 0.85f), TextAnchor.MiddleLeft);
                t.text = Textos.Funcao[f]; t.color = Art.Dim;
                for (int i = 0; i < MaxTrip; i++)
                {
                    float y1 = 0.8f - i * 0.085f;
                    _tripLinha[f, i] = Art.NewText(pt, "Trip" + f + i, 28, new Vector2(x0, y1 - 0.08f), new Vector2(x1, y1), TextAnchor.MiddleLeft);
                }
                var ff = (Funcao)f;
                _tripHire[f] = Art.NewButton(pt, "", 28, Art.Good, new Vector2(x0, 0.16f), new Vector2(x1, 0.27f), () => _g.HireTripulante(ff));
            }
            Art.NewButton(pt, Textos.Fechar, 40, Art.Off, new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.13f), () => Fecha(_trip));

            _patio = NovoModal(canvas, "Patio", 0.08f, 0.92f);
            _patio.Titulo.text = Textos.Patio;
            _patio.Corpo.text = "";
            RectTransform pp = PainelDe(_patio);
            _patioEquipe = Art.NewText(pp, "Equipe", 36, new Vector2(0.05f, 0.76f), new Vector2(0.6f, 0.85f), TextAnchor.MiddleLeft);
            _patioHire = Art.NewButton(pp, "", 30, Art.Good, new Vector2(0.62f, 0.75f), new Vector2(0.96f, 0.86f), () => _g.HirePatio());
            for (int k = 0; k < 2; k++)
            {
                int kk = k;
                _patioAviao[k] = Art.NewButton(pp, "", 30, Art.VooCor[k], new Vector2(0.04f + k * 0.47f, 0.63f), new Vector2(0.49f + k * 0.47f, 0.74f), () => { Fecha(_patio); AbreVoo(kk); });
            }
            for (int sv = 0; sv < 4; sv++)
            {
                float y1 = 0.62f - sv * 0.115f;
                Text n = Art.NewText(pp, "Servico" + sv, 28, new Vector2(0.04f, y1 - 0.105f), new Vector2(0.27f, y1), TextAnchor.MiddleLeft);
                n.text = Textos.Servico[sv];
                for (int m = 0; m < 2; m++)
                {
                    var ss = (Servico)sv; var mm = (Melhoria)m;
                    _patioBtn[sv, m] = Art.NewButton(pp, "", 28, m == 0 ? Art.EstacaoCor[1] : Art.EstacaoCor[2], new Vector2(0.28f + m * 0.35f, y1 - 0.105f), new Vector2(0.62f + m * 0.35f, y1), () => _g.BuyPatio(ss, mm));
                }
            }
            Art.NewButton(pp, Textos.Fechar, 40, Art.Off, new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.13f), () => Fecha(_patio));

            _equipe = NovaFolhaModal(canvas, "Equipe", Textos.Equipe, Art.Pessoas(), 1600f);
            RectTransform pe = _equipe.F.Corpo;
            RectTransform nums = Art.Node(pe, "Numeros", Vector2.up, Vector2.one);
            nums.offsetMin = new Vector2(24f, -172f); nums.offsetMax = new Vector2(-24f, -16f);
            _equipeTime = Art.NovoNumero(nums, "Contratados", Art.Pessoas(), Art.Azul, Textos.Contratados, Vector2.zero, new Vector2(0.34f, 1f));
            _equipeCash = Art.NovoNumero(nums, "Cash", Art.Moeda(), Color.white, Textos.CashNome, new Vector2(0.34f, 0f), new Vector2(0.67f, 1f));
            _equipeNivel = Art.NovoNumero(nums, "Terminal", Art.Estrela(), Art.Coin, Textos.TerminalNome, new Vector2(0.67f, 0f), Vector2.one);
            _equipeBarra = Art.NovaBarraNivel(pe, "Vagas", Sim.MaxEquipe, Vector2.up, Vector2.one);
            RectTransform vagas = (RectTransform)_equipeBarra.Pai;
            vagas.offsetMin = new Vector2(36f, -196f); vagas.offsetMax = new Vector2(-30f, -180f);
            RectTransform lista = Art.NovaLista(pe, Vector2.zero, Vector2.one);
            Art.Margem((RectTransform)lista.parent, 24f, 0f, 24f, 208f);
            for (int p = 0; p < Sim.MaxEquipe; p++)
            {
                int pp2 = p;
                RectTransform c = Art.NovoCartao(lista, "Perfil" + p, 186f, 290f);
                _equipePlaca[p] = Art.NovaPlaca(c, "Placa", Art.IconePerfil(p), Art.Apagado, Color.white);
                Art.Fixa(_equipePlaca[p].rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(126f, 126f));
                _equipeNome[p] = Art.Escreve(c, "Nome", Tipo.Nome, new Vector2(0f, 0.6f), new Vector2(1f, 0.9f));
                _equipeCargo[p] = Art.Escreve(c, "Cargo", Tipo.Legenda, new Vector2(0f, 0.36f), new Vector2(1f, 0.62f));
                _equipeInfo[p] = Art.Escreve(c, "Info", Tipo.Legenda, new Vector2(0f, 0.1f), new Vector2(1f, 0.37f));
                foreach (Text t in new[] { _equipeNome[p], _equipeCargo[p], _equipeInfo[p] }) Art.Margem(t.rectTransform, 172f, 0f, 306f, 0f);
                _equipeBtn[p] = Art.NovoBotaoPreco(c, Vector2.zero, Vector2.zero, () => EquipeAcao(pp2));
                Art.Fixa((RectTransform)_equipeBtn[p].B.transform, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(246f, 146f));
            }
        }

        /// <summary>Quem ja veio: "mover para". Quem nao veio: contrata (os 3 da v0.1 pela ordem, com Hire(); Dani, Edu e Fatima pelo perfil).</summary>
        void EquipeAcao(int p)
        {
            Sim s = S;
            int quem = s.Staff.FindIndex(f => f.Perfil == (Perfil)p);
            if (quem >= 0) { Fecha(_equipe); AbreMover(quem); }
            else if (p < Sim.MaxFuncionarios) _g.Hire();
            else _g.Hire((Perfil)p);
        }

        /// <summary>Toque no voo do quadro, no aviao ou na tela do patio.</summary>
        public void AbreVoo(int aviao)
        {
            _vooK = Mathf.Clamp(aviao, 0, 1);
            Abre(_voo);
        }

        void PintaOperacao()
        {
            Sim s = S;
            if (_voo.Rt.gameObject.activeSelf) PintaVoo(s);
            if (_trip.Rt.gameObject.activeSelf) PintaTrip(s);
            if (_patio.Rt.gameObject.activeSelf) PintaPatio(s);
            if (_equipe.Rt.gameObject.activeSelf) PintaEquipe(s);
        }

        void PintaVoo(Sim s)
        {
            TA.Core.Aviao a = s.Avioes[_vooK];
            Voo v = s.Voos[_vooK];
            bool embarcando = v != null && v.Embarque && !v.Partiu;
            _voo.Titulo.text = v != null ? Textos.VooTitulo(v.Id) : Textos.AviaoNome[_vooK];
            _vooEstado.text = Textos.AviaoEstado(_vooK, a, a.Hangar * Sim.Step);
            _vooDesgaste.text = Textos.Desgaste(a.Desgaste);
            _vooBarra.rectTransform.anchorMax = new Vector2(a.Desgaste / 100f, 1f);
            _vooBarra.color = CorDesgaste(a, s);
            Art.Rotulo(_checar).text = Textos.ChecagemBotao(s.T.CheckTira, s.T.CheckSeg, s.T.CheckCusto);
            _checar.interactable = s.PodeChecagem(_vooK);
            Art.Rotulo(_hangar).text = Textos.HangarBotao(s.T.HangarSeg, s.T.HangarCusto);
            _hangar.interactable = s.PodeHangar(_vooK);
            _vooMotivo.text = Textos.PorQueNaoChecar(a, embarcando, s.MecanicoLivre, s.Cash, s.T.CheckCusto) + "\n" + Textos.AvisoHangar + " " + Textos.Reparo;

            string piloto = "", comissario = "";
            foreach (Tripulante t in s.Tripulacao)
                if (t.Aviao == _vooK) { if (t.Funcao == Funcao.Piloto) piloto = t.Nome; else comissario = t.Nome; }
            bool pode = s.PodeEscala(_vooK);
            _vooEscala.text = !a.NoPatio ? Textos.EscalaSemVoo : !pode ? Textos.EscalaFechada : Textos.Equipagem(piloto, comissario);
            for (int f = 0; f < 2; f++)
            {
                int i = 0;
                foreach (Tripulante t in s.Tripulacao)
                {
                    if ((int)t.Funcao != f || t.Aviao == _vooK || i >= MaxTrip - 1) continue;
                    _trocaId[f, i] = t.Id;
                    _trocaBtn[f, i].gameObject.SetActive(pode);
                    _trocaBtn[f, i].interactable = s.Livre(t);
                    Art.Rotulo(_trocaBtn[f, i]).text = Textos.TripTroca(t, s.T.TrechosAteDescanso);
                    i++;
                }
                for (; i < MaxTrip - 1; i++) _trocaBtn[f, i].gameObject.SetActive(false);
            }
        }

        static Color CorDesgaste(TA.Core.Aviao a, Sim s) => a.Pendente ? Art.Bad : a.Desgaste >= s.T.DesgasteAviso ? Art.Warn : Art.Good;

        void PintaTrip(Sim s)
        {
            for (int f = 0; f < 2; f++)
            {
                int i = 0;
                foreach (Tripulante t in s.Tripulacao)
                {
                    if ((int)t.Funcao != f || i >= MaxTrip) continue;
                    Voo v = t.Aviao >= 0 ? s.Voos[t.Aviao] : null;
                    _tripLinha[f, i++].text = t.Nome + "\n" + Textos.TripStatus(t, v != null ? v.Id : -1, s.DescansoFalta(t), s.T.TrechosAteDescanso);
                }
                for (; i < MaxTrip; i++) _tripLinha[f, i].text = "";
                long c = s.CustoTripulante((Funcao)f);
                Art.Rotulo(_tripHire[f]).text = Textos.TripContratar((Funcao)f, c, s.TetoTripulacao);
                _tripHire[f].interactable = c >= 0 && s.Cash >= c;
            }
        }

        void PintaPatio(Sim s)
        {
            _patioEquipe.text = Textos.PatioEquipe(s.EquipePatio);
            long c = s.CustoPatio();
            Art.Rotulo(_patioHire).text = Textos.PatioContratar(c);
            _patioHire.interactable = c >= 0 && s.Cash >= c;
            for (int k = 0; k < 2; k++)
            {
                Voo v = s.Voos[k];
                Art.Rotulo(_patioAviao[k]).text = Textos.AviaoLinha(v != null ? v.Id : -1, s.Avioes[k].Desgaste);
            }
            for (int sv = 0; sv < 4; sv++)
            {
                var ss = (Servico)sv;
                long cv = s.CustoPatio(ss, Melhoria.Velocidade), cc = s.CustoPatio(ss, Melhoria.Capacidade);
                float seg = s.ServicoPassos(ss) * Sim.Step;
                Art.Rotulo(_patioBtn[sv, 0]).text = Textos.ServicoVel(s.NivelPatio(ss, Melhoria.Velocidade), s.T.PatioVelNiveis, seg, seg * s.T.VelFator, cv);
                Art.Rotulo(_patioBtn[sv, 1]).text = Textos.VeiculosLinha(s.Veiculos(ss), 1 + s.T.PatioCapNiveis, cc);
                _patioBtn[sv, 0].interactable = cv >= 0 && s.Cash >= cv;
                _patioBtn[sv, 1].interactable = cc >= 0 && s.Cash >= cc;
            }
        }

        void PintaEquipe(Sim s)
        {
            _equipeTime.Valor.text = Textos.DeTotal(s.Staff.Count, Sim.MaxEquipe);
            _equipeCash.Valor.text = Textos.Num(s.Cash);
            _equipeNivel.Valor.text = Textos.NivelNome(s.Marco + 1);
            _equipeBarra.Pinta(s.Staff.Count, Art.Compra);
            int proximo = 0;   // Ana, Beto e Cida vem na ordem (Sim.Hire)
            while (proximo < Sim.MaxFuncionarios && s.Contratado((Perfil)proximo)) proximo++;
            for (int p = 0; p < Sim.MaxEquipe; p++)
            {
                var perfil = (Perfil)p;
                int quem = s.Staff.FindIndex(f => f.Perfil == perfil), marco = s.T.PerfilMarco[p];
                bool veio = quem >= 0, aberto = s.Marco >= marco, naVez = p >= Sim.MaxFuncionarios || p == proximo;
                _equipeNome[p].text = Textos.Funcionario[p];
                _equipeNome[p].color = aberto ? Art.Tinta : Art.Fraco;
                _equipeCargo[p].text = Textos.Perfil[p];
                _equipeInfo[p].text = veio ? Textos.EmEstacao((int)s.Staff[quem].Em, s.Staff[quem].Andando > 0)
                    : aberto ? Textos.PerfilInfo(p) : Textos.FaltamEmbarques(s.T.Marcos[marco - 1] - s.TotalEmbarcados);
                _equipePlaca[p].color = aberto ? Art.EstacaoCor[(int)Funcionario.Casa(perfil)] : Art.Vazio;
                if (veio) _equipeBtn[p].Secundario(Textos.MoverCurto);
                else if (!aberto) _equipeBtn[p].Travado(Textos.TerminalNome, Textos.NivelNome(marco + 1));
                else if (!naVez) _equipeBtn[p].Travado(Textos.ContrateAntes, Textos.Funcionario[proximo]);
                else _equipeBtn[p].Preco(Textos.ContratarCurto, s.CustoContratar(perfil), s.Cash);
            }
        }
    }
}
