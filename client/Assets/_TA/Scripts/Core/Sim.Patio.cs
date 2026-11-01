using System;

namespace TA.Core
{
    /// <summary>
    /// Ciclo do aviao, patio e manutencao (CONTRATO_FASE_C §1, §3 e §4). Sem sorteio: tudo sai do relogio do quadro.
    /// Encosta PatioAntes s antes do fechamento -> servicos do turnaround e tripulacao -> embarque so com o aviao liberado (Pronto) ->
    /// portao fecha na hora -> decola quando embarcou e o patio terminou -> voa o Trecho e volta para o voo da onda seguinte.
    /// </summary>
    public sealed partial class Sim
    {
        public readonly Aviao[] Avioes;
        /// <summary>Pessoas da equipe de patio (cada uma faz um servico por vez) e os niveis de Velocidade e Capacidade (+1 veiculo) por servico.</summary>
        public int EquipePatio;
        public readonly int[] PatioVel = new int[4], PatioCap = new int[4];
        /// <summary>Vezes que o embarque chegou na hora sem o desembarque e a limpeza prontos (o bot compra a 2a pessoa de patio).</summary>
        public int AtrasosPatio;

        int Passos(float seg) => Math.Max(1, (int)Math.Round(seg * Hz));
        public int ServicoPassos(Servico s) => Math.Max(1, (int)Math.Round(T.ServicoSeg[(int)s] * Math.Pow(T.VelFator, PatioVel[(int)s]) * Hz));
        public int Veiculos(Servico s) => 1 + PatioCap[(int)s];
        public bool MecanicoLivre { get { if (Reparo > 0) return false; foreach (Aviao a in Avioes) if (a.Check > 0) return false; return true; } }

        /// <summary>Voo da onda atual que o aviao vai fazer (null = nenhum aberto).</summary>
        public Voo VooDo(Aviao a) => a.VooId >= 0 && Voos[a.Id] != null && Voos[a.Id].Id == a.VooId ? Voos[a.Id] : null;

        /// <summary>O embarque do voo do aviao pode abrir: no patio, desembarque e limpeza feitos, tripulacao completa a bordo e manutencao em
        /// dia (nem pendente nem em checagem). Regra desligada no Tuning = nao segura.</summary>
        public bool Pronto(int aviao) => aviao >= 0 && aviao < Avioes.Length && Pronto(Avioes[aviao]);

        bool Pronto(Aviao a) => a.NoPatio && a.Hangar == 0 && a.VooId >= 0 && a.FechouEm < 0 && !a.Pane
            && (!T.RegraPatio || a.Falta[(int)Servico.Desembarque] == 0 && a.Falta[(int)Servico.Limpeza] == 0)
            && (!T.RegraManutencao || !a.Pendente && a.Check == 0)
            && (!T.RegraTripulacao || TripCompleta(a));

        long ChegadaNaOnda(int w, int f) => (long)w * OndaPassos + Passos(T.Fecha[f] - T.PatioAntes);

        /// <summary>Onda nova: agenda o 1o pouso (jogo novo, -onda K) e da o voo novo a quem esta parado no patio sem voo.</summary>
        void PatioNaOnda(int w)
        {
            foreach (Aviao a in Avioes)
            {
                if (a.Chega < 0) a.Chega = ChegadaNaOnda(w, a.Id);
                if (a.NoPatio && a.VooId < 0) Liga(a);
            }
        }

        void Liga(Aviao a)
        {
            Voo v = Voos[a.Id];
            a.VooId = v != null && !v.Partiu ? v.Id : -1;
            if (a.VooId >= 0) a.Rota = v.Rota;
            a.FechouEm = -1; a.Checou = false; a.AvisouTrip = false;
        }

        /// <summary>Um passo do patio, antes do embarque: hangar, pousos, escala automatica, servicos e checagens.</summary>
        void Patio()
        {
            foreach (Aviao a in Avioes)
            {
                if (a.Hangar > 0) { if (--a.Hangar == 0) { a.Desgaste = 0; Encosta(a, true); Emit(Ev.HangarFeito, a.Id); } }
                else if (!a.NoPatio && a.Chega >= 0 && (a.Retorno > 0 ? Steps >= a.Chega : Steps >= a.Chega + EsperaPista && !PistaFechada && Steps >= PistaAte)) Encosta(a, false);
                if (a.Retorno > 0 && Steps == a.Retorno) Emit(Ev.RetornoPedido, a.Id);
            }
            EscalaAuto();
            Servicos();
            Checagens();
        }

        /// <summary>Pousou e encostou: a tripulacao desce (e descansa se bateu o limite), o turnaround comeca (do hangar volta pronto) e o
        /// aviao pega o voo dele da onda, se ainda nao fechou.</summary>
        void Encosta(Aviao a, bool doHangar)
        {
            a.NoPatio = true; a.Chega = Steps;
            if (a.Retorno > 0) PousoDeRetorno(a);
            for (int s = 0; s < 4; s++) { a.Falta[s] = doHangar ? 0 : ServicoPassos((Servico)s); a.Fazendo[s] = false; }
            Desembarca(a, true);
            Liga(a);
            Emit(Ev.Pousou, a.Id, a.VooId);
        }

        void Servicos()
        {
            if (PatioParado) return;
            int livres = EquipePatio;
            foreach (Aviao a in Avioes) for (int s = 0; s < 4; s++) if (a.Fazendo[s]) livres--;
            // primeiro o que segura o embarque (desembarque e limpeza), depois combustivel e bagagem; avioes por ordem de chegada
            int o = Avioes[1].Chega < Avioes[0].Chega ? 1 : 0;
            for (int passada = 0; passada < 2; passada++)
                for (int k = 0; k < 2; k++)
                {
                    Aviao a = Avioes[k == 0 ? o : 1 - o];
                    if (!a.NoPatio) continue;
                    for (int s = passada * 2; s < passada * 2 + 2 && livres > 0; s++)
                        if (a.Falta[s] > 0 && !a.Fazendo[s] && (s == (int)Servico.Combustivel || s == 0 || a.Falta[0] == 0) && EmUso(s) < Veiculos((Servico)s))
                        {
                            a.Fazendo[s] = true;
                            livres--;
                        }
                }
            foreach (Aviao a in Avioes)
                for (int s = 0; s < 4; s++)
                {
                    if (!a.Fazendo[s] || --a.Falta[s] > 0) continue;
                    a.Fazendo[s] = false;
                    Emit(Ev.ServicoFeito, a.Id, s);
                    if (a.PatioPronto) Emit(Ev.TurnaroundFeito, a.Id, a.VooId >= 0 ? a.VooId : -1, a.FechouEm >= 0 ? 1 : 0);
                }
        }

        int EmUso(int s)
        {
            int n = 0;
            foreach (Aviao a in Avioes) if (a.Fazendo[s]) n++;
            return n;
        }

        void Checagens()
        {
            foreach (Aviao a in Avioes)
                if (a.Check > 0 && --a.Check == 0)
                {
                    a.Desgaste = Math.Max(0, a.Desgaste - (a.Inspecao ? T.InspecaoTira : T.CheckTira));
                    a.Inspecao = false;
                    Emit(Ev.ChecagemFeita, a.Id, a.Desgaste);
                    if (a.Pane) { a.Pane = false; Emit(Ev.PaneReparada, a.Id); }
                }
            if (!T.RegraManutencao) return;
            // o padrao: aviao pendente no patio recebe a inspecao obrigatoria assim que o mecanico esta livre (sem Cash; custa tempo)
            foreach (Aviao a in Avioes)
                if (a.NoPatio && (a.Pendente || a.Pane) && a.Check == 0 && a.FechouEm < 0 && MecanicoLivre)
                {
                    a.Check = a.Pane ? Passos(T.InspecaoSeg) : PassosOficina(T.InspecaoSeg); a.Inspecao = true; a.Checou = true;
                    Emit(Ev.ChecagemIniciada, a.Id, 0, 1);
                }
        }

        /// <summary>Hora do embarque sem o desembarque e a limpeza prontos: conta o atraso do patio (uma vez por voo, no passo da abertura).</summary>
        void ContaAtrasoPatio(Voo v)
        {
            Aviao a = Avioes[v.Indice];
            if (T.RegraPatio && a.NoPatio && a.VooId == v.Id && (a.Falta[0] > 0 || a.Falta[1] > 0)) AtrasosPatio++;
        }

        /// <summary>Portao fechou (Sim.Fecha): com gente a bordo, falta so decolar (patio por terminar = decolagem atrasada, conta o atraso);
        /// sem embarque, o aviao fica no patio para o proximo voo.</summary>
        void FechaAviao(Voo v)
        {
            Aviao a = Avioes[v.Indice];
            if (a.VooId != v.Id) return;
            if (!v.Embarque) { a.VooId = -1; return; }
            a.FechouEm = Steps;
            if (T.RegraPatio && !a.PatioPronto) AtrasosPatio++;
        }

        /// <summary>Saiu na hora: o patio terminou ate o portao fechar (o bonus de pontualidade pede isto).</summary>
        bool NoHorario(Voo v)
        {
            Aviao a = Avioes[v.Indice];
            return !PistaFechada && !v.Atrasado && EsperaDecolagem(a) == 0 && (!T.RegraPatio || a.VooId == v.Id && a.PatioPronto);
        }

        /// <summary>Decola quando embarcou (portao fechado) e o patio terminou: conta o trecho da tripulacao e soma o desgaste.</summary>
        void Decolagens()
        {
            if (PistaFechada) return;
            foreach (Aviao a in Avioes)
            {
                if (!a.NoPatio || a.VooId < 0 || a.FechouEm < 0 || T.RegraPatio && !a.PatioPronto) continue;
                if (Steps < PistaAte || Steps < a.SaiEm || Steps < a.FechouEm + EsperaDecolagem(a) || RejeitouDecolagem(a)) continue;
                if (ClimaAgora?.Degelo > 0) Emit(Ev.Degelou, a.Id, a.VooId);
                TipoFalha falha = FalhaDe(a);
                Emit(Ev.Decolou, a.Id, a.VooId, (int)Math.Round((Steps - a.FechouEm) * (double)Step));
                foreach (Tripulante t in Tripulacao) if (t.Aviao == a.Id) t.Trechos++;
                if (T.RegraManutencao)
                {
                    int antes = a.Desgaste;
                    a.Desgaste = Math.Min(100, a.Desgaste + (int)Math.Round(T.DesgastePorTrecho * (double)Aero.Desgaste));
                    if (a.Desgaste >= 100 && antes < 100) Emit(Ev.ManutencaoPendente, a.Id, a.Desgaste);
                    else if (a.Desgaste >= T.DesgasteAviso && antes < T.DesgasteAviso) Emit(Ev.ManutencaoAviso, a.Id, a.Desgaste);
                }
                for (int s = 0; s < 4; s++) { a.Falta[s] = 0; a.Fazendo[s] = false; }
                a.VoltaOnda = a.VooId / 2 + Efeito(a.Rota).Cada;
                a.NoPatio = false; a.VooId = -1; a.FechouEm = -1; a.SaiEm = 0;
                a.Chega = Steps + Passos(TrechoSeg(a.Rota));
                if (falha == TipoFalha.Retorno) DecolouComRetorno(a);
            }
        }

        // ------------------------------------------------------------------ intencoes do patio e da manutencao

        /// <summary>Custo da proxima pessoa de patio (-1 = no teto). Preco fixo pela ordem.</summary>
        public long CustoPatio()
        {
            int i = EquipePatio - T.PatioIniciais;
            return i < 0 || i >= T.PatioContratacao.Length ? -1 : T.PatioContratacao[i];
        }

        public bool HirePatio()
        {
            long c = CustoPatio();
            if (c < 0 || Cash < c) return false;
            Cash -= c;
            EquipePatio++;
            Emit(Ev.PatioContratado, EquipePatio, (int)c);
            return true;
        }

        public int NivelPatio(Servico s, Melhoria m) => m == Melhoria.Velocidade ? PatioVel[(int)s] : PatioCap[(int)s];

        /// <summary>Custo do proximo nivel do servico (-1 = no maximo): Velocidade 30 x 1,6^n (arredondado a 5), Capacidade (+1 veiculo) 120.</summary>
        public long CustoPatio(Servico s, Melhoria m)
        {
            if ((int)s < 0 || (int)s >= 4) return -1;
            int n = NivelPatio(s, m);
            if (n >= (m == Melhoria.Velocidade ? T.PatioVelNiveis : T.PatioCapNiveis)) return -1;
            if (m == Melhoria.Capacidade) return T.PatioCapCusto;
            return 5 * (long)Math.Round(T.PatioVelCusto * Math.Pow(T.PatioVelCresce, n) / 5.0, MidpointRounding.AwayFromZero);
        }

        public bool BuyPatio(Servico s, Melhoria m)
        {
            long c = CustoPatio(s, m);
            if (c < 0 || Cash < c) return false;
            Cash -= c;
            if (m == Melhoria.Velocidade) PatioVel[(int)s]++; else PatioCap[(int)s]++;
            Emit(Ev.PatioMelhorou, (int)s, (int)m, NivelPatio(s, m));
            return true;
        }

        /// <summary>Aviao no patio cujo embarque ainda nao abriu (nem fechou com gente a bordo): onde a checagem e o hangar valem.</summary>
        Aviao AntesDoEmbarque(int aviao)
        {
            if (aviao < 0 || aviao >= Avioes.Length) return null;
            Aviao a = Avioes[aviao];
            if (!a.NoPatio || a.FechouEm >= 0 || (VooDo(a)?.Embarque ?? false)) return null;
            return a;
        }

        /// <summary>A tela pergunta antes de oferecer: a checagem, o hangar e a troca da escala valem agora (as condicoes das intencoes).</summary>
        public bool PodeChecagem(int aviao)
        {
            Aviao a = AntesDoEmbarque(aviao);
            return a != null && T.RegraManutencao && !a.Checou && a.Check == 0 && !a.Pendente && !a.Pane && a.Desgaste > 0 && MecanicoLivre && Cash >= T.CheckCusto;
        }

        public bool PodeHangar(int aviao) => AntesDoEmbarque(aviao) != null && T.RegraManutencao && Cash >= T.HangarCusto;

        public bool PodeEscala(int aviao) => AntesDoEmbarque(aviao) is Aviao a && a.VooId >= 0;

        /// <summary>Checagem rapida (CONTRATO_FASE_C §4): o mecanico tira CheckTira de desgaste em CheckSeg s, por CheckCusto Cash, uma por
        /// turnaround e antes de o embarque abrir. Pendente nao: esse recebe a inspecao obrigatoria. So Cash e tempo.</summary>
        public bool Checagem(int aviao)
        {
            if (!PodeChecagem(aviao)) return false;
            Aviao a = Avioes[aviao];
            Cash -= T.CheckCusto;
            a.Check = PassosOficina(T.CheckSeg); a.Inspecao = false; a.Checou = true;
            Emit(Ev.ChecagemIniciada, a.Id, T.CheckCusto, 0);
            return true;
        }

        /// <summary>Revisao no hangar: por HangarCusto Cash o aviao sai do patio por HangarSeg s e volta com desgaste 0, limpo e abastecido. O
        /// voo dele (se havia) nao embarca: custa so a receita dele. A tripulacao volta para a sala; uma inspecao em andamento e' trocada pelo
        /// hangar. So Cash e tempo.</summary>
        public bool Hangar(int aviao)
        {
            if (!PodeHangar(aviao)) return false;
            Aviao a = Avioes[aviao];
            Cash -= T.HangarCusto;
            Desembarca(a, false);
            for (int s = 0; s < 4; s++) { a.Falta[s] = 0; a.Fazendo[s] = false; }
            a.Check = 0; a.NoPatio = false; a.VooId = -1; a.FechouEm = -1; a.Pane = false;
            a.Hangar = Passos(T.HangarSeg);
            Emit(Ev.HangarIniciado, a.Id, T.HangarCusto);
            return true;
        }
    }
}
