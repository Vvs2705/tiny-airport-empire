using System;
using System.Collections.Generic;
using System.Text;

namespace TA.Core
{
    /// <summary>O que o registro guarda (frente ta-historico). Indice vai no save: so cresce no fim.</summary>
    // ponytail: a falta de caixa entra no fim quando a frente A (despesas) chegar a main
    public enum TipoOcorrencia { Incidente, Clima, FalhaAviao, VooPerdido, VooAtrasado }

    /// <summary>A chave da explicacao curta (Textos.Motivo): por que deu certo ou onde travou. Indice vai no save: so cresce no fim.</summary>
    public enum Motivo { Nenhum, Fila, SemEquipe, Equipamento, Clima, Pista, Patio, Tripulacao, Manutencao, Pane, Hangar, Pouso }

    /// <summary>Uma ocorrencia do dia de jogo: o que aconteceu, a causa, a resposta e o resultado (GDD de direcao §14).</summary>
    public sealed class Ocorrencia
    {
        public TipoOcorrencia Tipo;
        /// <summary>TipoIncidente (incidente e clima) ou TipoFalha (falha de aviao); -1 nos voos.</summary>
        public int Sub = -1;
        /// <summary>Passo do relogio do quadro em que comecou (Sim.HoraDe da a hora do jogo) e o dia do jogo dele.</summary>
        public long Passo; public int Dia;
        /// <summary>A causa alem do tipo: a estacao com falha de equipamento ou o desgaste (%) do aviao que falhou; 0 no resto.</summary>
        public int Causa;
        /// <summary>Voo atingido (Id; -1 = nenhum), os passageiros dele (no retorno: os remarcados), os que perderam o voo e os s de atraso da decolagem.</summary>
        public int Voo = -1, Total, Perdidos, Atraso;
        /// <summary>A resposta: Segurar = ninguem fez nada; Auto = o nucleo resolveu sozinho (mecanico na estacao, inspecao obrigatoria). Custo = Cash dela.</summary>
        public Opcao Resposta; public bool Auto; public long Custo;
        /// <summary>A chave da explicacao e os argumentos dela: estacao, s para a fila andar (min no quadro) e postos abertos.</summary>
        public Motivo Motivo; public Estacao Estacao; public int Espera, Postos;
        /// <summary>Em andamento: o incidente nao terminou, o voo nao fechou ou nao decolou, ou o aviao da falha ainda nao voltou ao normal.</summary>
        public bool Aberta;
        /// <summary>Deu certo: ninguem perdeu o voo e ele saiu na hora.</summary>
        public bool Eficaz => Perdidos == 0 && Atraso == 0;
    }

    /// <summary>
    /// Registro de ocorrencias (frente ta-historico, GDD de direcao §14): os 4 incidentes, o clima, as falhas de aviao e o voo perdido ou
    /// atrasado, com causa, resposta, resultado e a chave da explicacao. So observa: le os eventos (Emit) e o portao que fecha (Fecha), nunca
    /// muda o jogo (Tuning.RegraHistorico = false da o mesmo jogo, numero a numero). Sem relogio do aparelho: tudo sai do passo fixo. Guarda
    /// HistoricoDias dias do jogo, ate HistoricoPorDia por dia, no save. A tela vem nas frentes G (satisfacao) e I (painel).
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>O dia do jogo de agora e os 2 anteriores; ate 24 por dia (o mais antigo do dia sai).</summary>
        public const int HistoricoDias = 3, HistoricoPorDia = 24;

        readonly List<Ocorrencia> _hist = new List<Ocorrencia>();

        /// <summary>As ocorrencias guardadas, na ordem em que entraram (a ultima = a ultima ocorrencia do painel).</summary>
        public IReadOnlyList<Ocorrencia> Historico { get { Poda(); return _hist; } }

        /// <summary>Tira o que passou da retencao (o dia vira sem evento: quem le, grava ou anota poda antes).</summary>
        void Poda() => _hist.RemoveAll(r => r.Dia <= DiaJogo - HistoricoDias);

        /// <summary>Hora do jogo (0 a 24) no passo `passo` do relogio do quadro.</summary>
        public float HoraDe(long passo) => (T.HoraInicial + 24f * (passo % DiaPassos) / DiaPassos) % 24f;

        /// <summary>Todo evento passa aqui (Emit), depois do que ele mudou.</summary>
        void AnotaHistorico(Ev t, int a, int b, int c)
        {
            if (!T.RegraHistorico) return;
            Ocorrencia o;
            switch (t)
            {
                case Ev.IncidenteAviso: case Ev.IncidenteInicio: DoIncidente(); break;
                case Ev.Adiantado: Responde(DoIncidente(), Opcao.Adiantar, 0, false); break;
                case Ev.Atrasado: Responde(DoIncidente(), Opcao.Atrasar, 0, false); break;
                case Ev.Priorizado: Responde(DoIncidente(), Opcao.Priorizar, 0, false); break;
                case Ev.ReparoIniciado: Responde(DoIncidente(), Opcao.Reparar, b, b == 0); break;
                case Ev.IncidenteFim: if ((o = DoIncidente()) != null) o.Aberta = !Fechou(o.Voo); break;
                case Ev.Pane: case Ev.Rejeicao:
                    o = Nova(TipoOcorrencia.FalhaAviao, (int)(t == Ev.Pane ? TipoFalha.Pane : TipoFalha.Rejeicao), b, Steps);
                    o.Causa = Avioes[a].Desgaste; o.Total = VooPorId(b)?.Total ?? 0; o.Aberta = true;
                    break;
                // a inspecao obrigatoria comeca sozinha; pagar o reparo ou mandar ao hangar e' a resposta do jogador e vale por cima
                case Ev.ChecagemIniciada: if (c == 1 && (o = DaFalha(a)) != null && o.Resposta == Opcao.Segurar) Responde(o, Opcao.Reparar, 0, true); break;
                case Ev.PaneReparo: Responde(DaFalha(a), Opcao.Reparar, b, false); break;
                case Ev.HangarIniciado: if ((o = DaFalha(a)) != null) { Responde(o, Opcao.Hangar, b, false); o.Aberta = false; } break;
                case Ev.PaneReparada: if ((o = DaFalha(a)) != null) o.Aberta = false; break;
                case Ev.Decolou: Decolou(a, b, c); break;
            }
        }

        /// <summary>Portao fechando (Sim.Fecha, antes de os passageiros que ficaram sairem das filas): o resultado e, com perdidos, onde travou.</summary>
        void AnotaVoo(Voo v)
        {
            if (!T.RegraHistorico) return;
            int perdidos = v.Total - v.Embarcados;
            bool ligada = false;
            foreach (Ocorrencia o in _hist)
            {
                if (o.Voo != v.Id) continue;
                Resultado(o, v, perdidos);
                // o incidente que ainda corre fecha no IncidenteFim; a pane, quando o aviao volta ao normal (reparo ou hangar)
                if (o.Tipo <= TipoOcorrencia.Clima) o.Aberta = _inc != null && o.Passo == _inc.Inicio;
                ligada = true;
            }
            if (ligada || v.Total == 0) return;
            if (perdidos > 0) Resultado(Nova(TipoOcorrencia.VooPerdido, -1, v.Id, Steps), v, perdidos);
            else if (v.Embarque && !NoHorario(v))
            {
                // a mesma conta do bonus de pontualidade: o patio nao terminou ou o clima segura a pista
                Ocorrencia o = Nova(TipoOcorrencia.VooAtrasado, -1, v.Id, Steps);
                Aviao a = Avioes[v.Indice];
                o.Total = v.Total; o.Aberta = true;
                o.Motivo = T.RegraPatio && !(a.VooId == v.Id && a.PatioPronto) ? Motivo.Patio : Motivo.Clima;
            }
        }

        void Resultado(Ocorrencia o, Voo v, int perdidos)
        {
            o.Total = v.Total; o.Perdidos = perdidos;
            if (perdidos > 0) Travou(o, v); else o.Motivo = Motivo.Nenhum;
        }

        /// <summary>Onde ficou a maior parte de quem perdeu o voo (na ordem das estacoes, no empate) e por que essa estacao nao deu conta.</summary>
        void Travou(Ocorrencia o, Voo v)
        {
            int max = 0;
            foreach (Station s in Estacoes)
            {
                int n = 0;
                foreach (Pax p in s.Fila) if (p.Onda == v.Onda && p.Voo == v.Indice) n++;
                foreach (Pax p in s.Atendendo) if (p.Onda == v.Onda && p.Voo == v.Indice) n++;
                if (n > max) { max = n; o.Estacao = s.Tipo; }
            }
            Estacao e = o.Estacao;
            Aviao a = Avioes[v.Indice];
            o.Postos = Math.Min(St(e).Postos, Presentes(e));
            o.Espera = (int)Math.Round(St(e).Pessoas * TempoSeg(e) / Math.Max(1, o.Postos));
            o.Motivo = max == 0 ? Motivo.Nenhum   // ninguem numa fila: so com save adulterado
                : e == Estacao.Portao && !v.Embarque ? (a.Hangar > 0 ? Motivo.Hangar : !a.NoPatio ? Motivo.Pouso : a.Pane ? Motivo.Pane
                    : T.RegraManutencao && (a.Pendente || a.Check > 0) ? Motivo.Manutencao : T.RegraTripulacao && !TripCompleta(a) ? Motivo.Tripulacao : Motivo.Patio)
                : e == Estacao.Portao && PistaFechada && !v.Adiantado ? Motivo.Clima
                : Falhou(e) ? Motivo.Equipamento
                : Presentes(e) == 0 ? Motivo.SemEquipe
                : Motivo.Fila;
        }

        /// <summary>Decolou: o atraso vai para o que ja fala desse voo (sem nada, a pista estava bloqueada por outro aviao) e a falha com retorno
        /// entra agora, porque a semente ja decidiu (FalhaDe e' pura).</summary>
        // ponytail: o retorno aparece no registro RetornoPede s antes do aviso; esconder ate Ev.RetornoPedido pede o voo no Aviao
        void Decolou(int aviao, int voo, int atraso)
        {
            bool ligada = false;
            for (int i = _hist.Count - 1; i >= 0; i--)
            {
                Ocorrencia o = _hist[i];
                if (o.Voo != voo) continue;
                ligada = true;
                if (o.Tipo == TipoOcorrencia.VooAtrasado && atraso == 0) { _hist.RemoveAt(i); continue; }   // o patio terminou no mesmo segundo
                o.Atraso = atraso;
                if (o.Tipo == TipoOcorrencia.VooAtrasado || o.Tipo == TipoOcorrencia.FalhaAviao && o.Sub == (int)TipoFalha.Rejeicao) o.Aberta = false;
            }
            if (!ligada && atraso > 0) { Ocorrencia o = Nova(TipoOcorrencia.VooAtrasado, -1, voo, Steps); o.Total = VooPorId(voo)?.Total ?? 0; o.Atraso = atraso; o.Motivo = Motivo.Pista; }
            Aviao a = Avioes[aviao];
            if (FalhaDe(a) != TipoFalha.Retorno) return;
            Ocorrencia r = Nova(TipoOcorrencia.FalhaAviao, (int)TipoFalha.Retorno, voo, Steps);
            r.Causa = a.Desgaste; r.Total = VooPorId(voo)?.Embarcados ?? 0; r.Aberta = true;
        }

        /// <summary>O registro do incidente em curso; o 1o evento dele abre (o aviso pode cair antes da onda e nao ser emitido).</summary>
        Ocorrencia DoIncidente()
        {
            Incidente i = _inc;
            if (i == null) return null;
            for (int k = _hist.Count - 1; k >= 0; k--)
            {
                Ocorrencia r = _hist[k];
                if (r.Aberta && r.Tipo <= TipoOcorrencia.Clima && r.Passo == i.Inicio && r.Sub == (int)i.Tipo) return r;
            }
            Voo v = Voos[i.Vaga];
            Ocorrencia o = Nova(ClimaDe(i.Tipo) != null ? TipoOcorrencia.Clima : TipoOcorrencia.Incidente, (int)i.Tipo, v.Id, i.Inicio);
            o.Total = v.Total; o.Aberta = true;
            if (i.Tipo == TipoIncidente.Falha) o.Causa = (int)i.Estacao;
            return o;
        }

        /// <summary>A pane do aviao (no portao ou depois do retorno) que ainda nao acabou: aberta ate o reparo ou o hangar.</summary>
        Ocorrencia DaFalha(int aviao)
        {
            for (int k = _hist.Count - 1; k >= 0; k--)
            {
                Ocorrencia r = _hist[k];
                if (r.Aberta && r.Tipo == TipoOcorrencia.FalhaAviao && r.Sub != (int)TipoFalha.Rejeicao && r.Voo % 2 == aviao) return r;
            }
            return null;
        }

        static void Responde(Ocorrencia o, Opcao r, long custo, bool auto)
        {
            if (o == null) return;
            o.Resposta = r; o.Custo += custo; o.Auto = auto;
        }

        /// <summary>O voo da onda atual com este Id (null = de outra onda).</summary>
        Voo VooPorId(int id) => id >= 0 && Voos[id % 2] is Voo v && v.Id == id ? v : null;

        bool Fechou(int voo) => !(VooPorId(voo) is Voo v) || v.Partiu;

        Ocorrencia Nova(TipoOcorrencia tipo, int sub, int voo, long passo)
        {
            int dia = (int)(passo / DiaPassos);
            Poda();
            int doDia = 0;
            foreach (Ocorrencia r in _hist) if (r.Dia == dia) doDia++;
            if (doDia >= HistoricoPorDia) _hist.RemoveAt(_hist.FindIndex(r => r.Dia == dia));
            var o = new Ocorrencia { Tipo = tipo, Sub = sub, Voo = voo, Passo = passo, Dia = dia };
            _hist.Add(o);
            return o;
        }

        // ------------------------------------------------------------------ save (schema 12)

        /// <summary>Schema 12: uma linha, as ocorrencias separadas por ';' e os campos por ','. Sem nenhuma, a chave fica de fora.</summary>
        void SalvaHistorico(StringBuilder sb)
        {
            Poda();
            if (_hist.Count == 0) return;
            sb.Append("hist=");
            for (int k = 0; k < _hist.Count; k++)
            {
                Ocorrencia o = _hist[k];
                if (k > 0) sb.Append(';');
                sb.Append(string.Join(",", new long[] { o.Passo, (int)o.Tipo, o.Sub, o.Causa, o.Voo, o.Total, o.Perdidos, o.Atraso, (int)o.Resposta, o.Auto ? 1 : 0,
                    o.Custo, (int)o.Motivo, (int)o.Estacao, o.Espera, o.Postos, o.Aberta ? 1 : 0 }));
            }
            sb.Append('\n');
        }

        /// <summary>Le o schema 12, com tudo limitado: ocorrencia torta ou longe no futuro sai; o teto do dia fica com as mais novas. O schema 9
        /// nao tem a chave: o registro comeca vazio.</summary>
        void CarregaHistorico(Dictionary<string, string> kv)
        {
            if (!kv.TryGetValue("hist", out string h)) return;
            foreach (string reg in h.Split(';'))
            {
                string[] f = reg.Split(',');
                if (f.Length < 16) continue;
                long[] n = Array.ConvertAll(f, x => L(x, long.MinValue));
                // o incidente entra no aviso com o passo do inicio: ate 2 ondas a frente, como o `inc=`
                if (Array.IndexOf(n, long.MinValue) >= 0 || n[0] < 0 || n[0] > Steps + 2L * OndaPassos) continue;
                var o = new Ocorrencia
                {
                    Passo = n[0], Dia = (int)(n[0] / DiaPassos),
                    Tipo = (TipoOcorrencia)Lim(n[1], 0, Enum.GetValues(typeof(TipoOcorrencia)).Length - 1),
                    Sub = (int)Lim(n[2], -1, Enum.GetValues(typeof(TipoIncidente)).Length - 1), Causa = (int)Lim(n[3], 0, 100),
                    Voo = (int)Lim(n[4], -1, Steps / OndaPassos * 2 + 1), Total = (int)Lim(n[5], 0, 499), Atraso = (int)Lim(n[7], 0, 3600),
                    Resposta = (Opcao)Lim(n[8], 0, Enum.GetValues(typeof(Opcao)).Length - 1), Auto = n[9] != 0, Custo = Lim(n[10], 0, int.MaxValue),
                    Motivo = (Motivo)Lim(n[11], 0, Enum.GetValues(typeof(Motivo)).Length - 1), Estacao = (Estacao)Lim(n[12], 0, Estacoes.Length - 1),
                    Espera = (int)Lim(n[13], 0, 3600), Postos = (int)Lim(n[14], 0, 8), Aberta = n[15] != 0,
                };
                o.Perdidos = (int)Lim(n[6], 0, o.Total);
                _hist.Add(o);   // a retencao poda quem le, grava ou anota
            }
            for (int k = _hist.Count - 1; k >= 0; k--)
            {
                int doDia = 0;
                for (int j = k + 1; j < _hist.Count; j++) if (_hist[j].Dia == _hist[k].Dia) doDia++;
                if (doDia >= HistoricoPorDia) _hist.RemoveAt(k);
            }
        }

        static long Lim(long x, long lo, long hi) => Math.Max(lo, Math.Min(hi, x));
    }
}
