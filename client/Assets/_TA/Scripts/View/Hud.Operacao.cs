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
        Resumo _vooResumo, _tripResumo, _patioResumo;
        BarraNivel _vooBarra;
        Item _vooEstado, _checar, _hangar, _vooEscala, _patioHire;
        readonly Item[,] _troca = new Item[2, MaxTrip - 1];
        readonly int[,] _trocaId = new int[2, MaxTrip - 1];
        readonly Item[,] _tripLinha = new Item[2, MaxTrip];
        readonly BarraNivel[,] _tripTrechos = new BarraNivel[2, MaxTrip];
        readonly Item[] _tripHire = new Item[2];
        readonly Item[] _patioAviao = new Item[2];
        readonly BarraNivel[] _patioDesgaste = new BarraNivel[2];
        readonly Compra[,] _patioCompra = new Compra[4, 2];
        readonly Text[] _equipeNome = new Text[Sim.MaxEquipe], _equipeCargo = new Text[Sim.MaxEquipe], _equipeInfo = new Text[Sim.MaxEquipe];
        readonly Image[] _equipePlaca = new Image[Sim.MaxEquipe];
        readonly BotaoPreco[] _equipeBtn = new BotaoPreco[Sim.MaxEquipe];
        Numero _equipeTime, _equipeCash, _equipeNivel;
        BarraNivel _equipeBarra;

        void BuildOperacao(RectTransform canvas)
        {
            BuildVoo(canvas);
            BuildTrip(canvas);
            BuildPatio(canvas);

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

        void BuildVoo(RectTransform canvas)
        {
            _voo = NovaFolhaModal(canvas, "Voo", "", Art.Aviao(), 1600f);
            RectTransform lista = NovoResumo(_voo, Textos.DesgasteNome, Art.Chave(), Art.Azul, out _vooResumo);
            _vooBarra = Art.NovaBarraNivel(_voo.F.Corpo, "Desgaste", 10, Vector2.up, Vector2.one);
            var barra = (RectTransform)_vooBarra.Pai;
            barra.offsetMin = new Vector2(36f, -196f); barra.offsetMax = new Vector2(-30f, -180f);
            Art.Margem((RectTransform)lista.parent, 24f, 0f, 24f, 208f);
            _vooEstado = NovoItem(lista, "Estado", Art.Aviao(), Art.VooCor[0], null, 136f, 18f);
            _vooEstado.Linha.text = Textos.Reparo;
            _checar = NovoItem(lista, "Checagem", Art.Chave(), Art.EstacaoCor[(int)Estacao.Manutencao], () => { int k = _vooK; Fecha(_voo); _g.Checagem(k); }, 236f);
            _checar.Nome.text = Textos.ChecagemNome;
            _hangar = NovoItem(lista, "Hangar", Art.Relogio(), Art.EstacaoCor[3], () => { int k = _vooK; Fecha(_voo); _g.Hangar(k); }, 236f);
            _hangar.Nome.text = Textos.HangarNome;
            _hangar.Info.text = Textos.AvisoHangar;
            _vooEscala = NovoItem(lista, "Escala", Art.Pessoas(), Art.Azul, null, 136f, 18f);
            _vooEscala.Nome.text = Textos.Escala;
            for (int f = 0; f < 2; f++)
                for (int i = 0; i < MaxTrip - 1; i++)
                {
                    int ff = f, ii = i;
                    _troca[f, i] = NovoItem(lista, "Troca" + f + i, Art.Pessoa(), Art.EstacaoCor[f == 0 ? 0 : 2], () => { Voo v = S.Voos[_vooK]; if (v != null) _g.Escala(v.Id, _trocaId[ff, ii]); }, 168f, 34f);
                }
        }

        void BuildTrip(RectTransform canvas)
        {
            _trip = NovaFolhaModal(canvas, "Tripulacao", Textos.Tripulacao, Art.Aviao(), 1600f);
            RectTransform lista = NovoResumo(_trip, Textos.Tripulacao, Art.Pessoas(), Art.Azul, out _tripResumo);
            for (int f = 0; f < 2; f++)
            {
                var ff = (Funcao)f;
                _tripHire[f] = NovoItem(lista, "Funcao" + f, f == 0 ? Art.Aviao() : Art.Pessoa(), Art.EstacaoCor[f == 0 ? 0 : 2], () => _g.HireTripulante(ff));
                _tripHire[f].Nome.text = Textos.Funcao[f];
                for (int i = 0; i < MaxTrip; i++)
                {
                    _tripLinha[f, i] = NovoItem(lista, "Trip" + f + i, Art.Check(), Art.Compra, null, 124f, 12f, 216f);
                    _tripTrechos[f, i] = Art.NovaBarraNivel(_tripLinha[f, i].Rt, "Trechos", S.T.TrechosAteDescanso, Vector2.zero, Vector2.zero);
                    Art.Fixa((RectTransform)_tripTrechos[f, i].Pai, new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(200f, 22f));
                }
            }
        }

        void BuildPatio(RectTransform canvas)
        {
            _patio = NovaFolhaModal(canvas, "Patio", Textos.Patio, Art.Caminhao(), 1600f);
            RectTransform lista = NovoResumo(_patio, Textos.Equipe, Art.Pessoas(), Art.Azul, out _patioResumo);
            _patioHire = NovoItem(lista, "Equipe", Art.Pessoas(), Art.Azul, () => _g.HirePatio(), 168f, 34f);
            _patioHire.Nome.text = Textos.EquipePatioNome;
            _patioHire.Linha.text = Textos.UmServico;
            for (int k = 0; k < 2; k++)
            {
                int kk = k;
                _patioAviao[k] = NovoItem(lista, "Aviao" + k, Art.Aviao(), Art.VooCor[k], () => { Fecha(_patio); AbreVoo(kk); });
                _patioAviao[k].Btn.Secundario(Textos.VerVoo);
                _patioDesgaste[k] = Art.NovaBarraNivel(_patioAviao[k].Rt, "Desgaste", 10, Vector2.up, Vector2.one);
                var barra = (RectTransform)_patioDesgaste[k].Pai;
                barra.offsetMin = new Vector2(400f, -150f); barra.offsetMax = new Vector2(-306f, -130f);
            }
            for (int sv = 0; sv < 4; sv++)
            {
                var ss = (Servico)sv;
                RectTransform c = NovoCartaoCompra(lista, "Servico" + sv, Textos.Servico[sv], Art.IconeServico(sv), Art.Azul);
                _patioCompra[sv, 0] = NovaCompra(c, 0, Art.Raio(), S.T.PatioVelNiveis, () => _g.BuyPatio(ss, Melhoria.Velocidade));
                _patioCompra[sv, 1] = NovaCompra(c, 1, Art.Caminhao(), S.T.PatioCapNiveis, () => _g.BuyPatio(ss, Melhoria.Capacidade));
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
            int custo = s.T.CheckCusto;
            _voo.Titulo.text = v != null ? Textos.VooTitulo(v.Id) : Textos.AviaoNome[_vooK];
            _vooResumo.Pinta(s, Textos.Porcento(a.Desgaste));
            _vooBarra.Pinta(NivelDesgaste(a), CorDesgaste(a, s));
            _vooEstado.Placa.color = Art.VooCor[_vooK];
            _vooEstado.Nome.text = Textos.AviaoEstado(_vooK, a, a.Hangar * Sim.Step);

            string trava = Textos.PorQueNaoChecar(a, embarcando, s.MecanicoLivre, custo, custo);   // cash = custo: a falta de Cash quem diz e o botao
            _checar.Linha.text = Textos.ChecagemEfeito(s.T.CheckTira, s.T.CheckSeg);
            _checar.Info.text = trava;
            if (trava.Length > 0 || s.Cash >= custo && !s.PodeChecagem(_vooK)) _checar.Btn.Travado(Textos.AgoraNao, Textos.Num(custo));
            else _checar.Btn.Preco(Textos.Checar, custo, s.Cash);
            _hangar.Linha.text = Textos.HangarEfeito(s.T.HangarSeg);
            if (s.Cash >= s.T.HangarCusto && !s.PodeHangar(_vooK)) _hangar.Btn.Travado(Textos.AgoraNao, Textos.Num(s.T.HangarCusto));
            else _hangar.Btn.Preco(Textos.Revisar, s.T.HangarCusto, s.Cash);

            string piloto = "", comissario = "";
            foreach (Tripulante t in s.Tripulacao)
                if (t.Aviao == _vooK) { if (t.Funcao == Funcao.Piloto) piloto = t.Nome; else comissario = t.Nome; }
            bool pode = s.PodeEscala(_vooK);
            _vooEscala.Linha.text = !a.NoPatio ? Textos.EscalaSemVoo : !pode ? Textos.EscalaFechada : Textos.Equipagem(piloto, comissario);
            for (int f = 0; f < 2; f++)
            {
                int i = 0;
                foreach (Tripulante t in s.Tripulacao)
                {
                    if ((int)t.Funcao != f || t.Aviao == _vooK || i >= MaxTrip - 1) continue;
                    _trocaId[f, i] = t.Id;
                    Item it = _troca[f, i++];
                    it.Rt.gameObject.SetActive(pode);
                    it.Nome.text = t.Nome;
                    it.Linha.text = TripStatus(s, t);
                    if (s.Livre(t)) it.Btn.Secundario(Textos.Escalar);
                    else if (t.Aviao >= 0) it.Btn.Travado(Textos.ABordoCurto, s.Voos[t.Aviao] != null ? Textos.Voo(s.Voos[t.Aviao].Id) : "");
                    else it.Btn.Travado(Textos.DescansaCurto, Textos.Tempo(s.DescansoFalta(t)));
                }
                for (; i < MaxTrip - 1; i++) _troca[f, i].Rt.gameObject.SetActive(false);
            }
        }

        static Color CorDesgaste(TA.Core.Aviao a, Sim s) => a.Pendente ? Art.Bad : a.Desgaste >= s.T.DesgasteAviso ? Art.Warn : Art.Good;
        static int NivelDesgaste(TA.Core.Aviao a) => (a.Desgaste + 9) / 10;   // 10 degraus: 1% ja acende o primeiro

        static string TripStatus(Sim s, Tripulante t)
        {
            Voo v = t.Aviao >= 0 ? s.Voos[t.Aviao] : null;
            return Textos.TripStatus(t, v != null ? v.Id : -1, s.DescansoFalta(t), s.T.TrechosAteDescanso);
        }

        void PintaTrip(Sim s)
        {
            int limite = s.T.TrechosAteDescanso, teto = s.TetoTripulacao;
            _tripResumo.Pinta(s, Textos.DeTotal(s.Tripulacao.Count, 2 * teto));
            for (int f = 0; f < 2; f++)
            {
                int i = 0;
                foreach (Tripulante t in s.Tripulacao)
                {
                    if ((int)t.Funcao != f || i >= MaxTrip) continue;
                    bool bordo = t.Aviao >= 0, descansa = !bordo && !s.Livre(t);
                    Item it = _tripLinha[f, i];
                    it.Rt.gameObject.SetActive(true);
                    it.Nome.text = t.Nome;
                    it.Linha.text = TripStatus(s, t);
                    it.Placa.color = bordo ? Art.Azul : descansa ? Art.Fraco : Art.Compra;
                    it.Glifo.sprite = bordo ? Art.Aviao() : descansa ? Art.Relogio() : Art.Check();
                    _tripTrechos[f, i++].Pinta(t.Trechos, t.Trechos >= limite ? Art.Warn : Art.Compra);
                }
                for (; i < MaxTrip; i++) _tripLinha[f, i].Rt.gameObject.SetActive(false);
                long c = s.CustoTripulante((Funcao)f);
                bool sobe = c < 0 && s.Marco < s.T.Marcos.Length;
                Item h = _tripHire[f];
                h.Linha.text = Textos.DeTotal(s.Quantos((Funcao)f), teto);
                h.Info.text = sobe ? Textos.LimiteSobe : "";
                if (c >= 0) h.Btn.Preco(Textos.ContratarCurto, c, s.Cash);
                else if (sobe) h.Btn.Travado(Textos.TerminalNome, Textos.NivelNome(s.Marco + 2));
                else h.Btn.Maximo(Textos.Completa);
            }
        }

        void PintaPatio(Sim s)
        {
            _patioResumo.Pinta(s, Textos.DeTotal(s.EquipePatio, s.T.PatioIniciais + s.T.PatioContratacao.Length));
            long c = s.CustoPatio();
            if (c >= 0) _patioHire.Btn.Preco(Textos.ContratarCurto, c, s.Cash); else _patioHire.Btn.Maximo(Textos.Completa);
            for (int k = 0; k < 2; k++)
            {
                TA.Core.Aviao a = s.Avioes[k];
                Voo v = s.Voos[k];
                Item it = _patioAviao[k];
                it.Nome.text = v != null ? Textos.VooTitulo(v.Id) : Textos.AviaoNome[k];
                it.Linha.text = Textos.AviaoEstado(k, a, a.Hangar * Sim.Step);
                it.Info.text = Textos.Desgaste(a.Desgaste);
                _patioDesgaste[k].Pinta(NivelDesgaste(a), CorDesgaste(a, s));
            }
            for (int sv = 0; sv < 4; sv++)
            {
                var ss = (Servico)sv;
                long cv = s.CustoPatio(ss, Melhoria.Velocidade), cc = s.CustoPatio(ss, Melhoria.Capacidade);
                int nv = s.NivelPatio(ss, Melhoria.Velocidade), nc = s.NivelPatio(ss, Melhoria.Capacidade), veic = s.Veiculos(ss);
                float seg = s.ServicoPassos(ss) * Sim.Step;
                _patioCompra[sv, 0].Pinta(Textos.MelhoriaNome(Melhoria.Velocidade, nv, s.T.PatioVelNiveis),
                    Textos.PorAviao(cv >= 0 ? Art.DePara(Textos.Seg(seg), Textos.Seg(seg * s.T.VelFator)) : Textos.Seg(seg)), nv, cv, s.Cash);
                _patioCompra[sv, 1].Pinta(Textos.VeiculosNome(nc, s.T.PatioCapNiveis),
                    Textos.VeiculosDe(cc >= 0 ? Art.DePara(veic.ToString(), (veic + 1).ToString()) : veic.ToString()), nc, cc, s.Cash);
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
