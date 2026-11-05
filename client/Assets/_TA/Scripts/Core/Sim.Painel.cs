using System;
using System.Collections.Generic;

namespace TA.Core
{
    /// <summary>Classe do alerta (Direcao §15): critico, importante ou informativo. A ordem e' a prioridade no painel.</summary>
    public enum Gravidade { Critico, Importante, Informativo }

    /// <summary>O que o alerta diz. Nao vai no save (o painel e' recalculado a cada leitura).</summary>
    public enum TipoAlerta { ContaAtrasada, Pane, Equipamento, FilaCritica, Incidente, VooAtrasado, Inspecao, Suprimento, BanheiroFechado, FilaAtencao, Sujeira, SatisfacaoBaixa }

    /// <summary>Para onde o toque no alerta leva (Direcao §15: todo alerta abre o local, a pessoa, o voo ou a sala).</summary>
    public enum Alvo { Estacao, Area, Voo, Aviao, Caixa }

    /// <summary>Um alerta do painel: o tipo, a classe, o alvo do toque (Indice: a Estacao, a Area, a vaga em Voos ou o aviao) e um numero do texto
    /// (a fila, o que faltou no caixa, o TipoIncidente, o Suprimento, a sujeira ou o Fator, conforme o tipo).</summary>
    public struct Alerta
    {
        public TipoAlerta Tipo; public Gravidade Gravidade; public Alvo Alvo; public int Indice; public long Valor;
    }

    /// <summary>Situacao de um voo da onda no painel. Atrasado = devia estar embarcando ou ja devia ter decolado e nao decolou.</summary>
    public enum SituacaoVoo { SemVoo, Previsto, Embarcando, Decolou, Atrasado }

    /// <summary>Uma linha de partida do painel: a vaga (= o portao e o aviao), o voo, a situacao, quem embarcou e se alguma fila poe o voo em risco.</summary>
    public struct LinhaVoo
    {
        public int Vaga, Id, Rota, Embarcados, Total, AbrePasso, FechaPasso;
        public SituacaoVoo Situacao;
        public bool EmRisco;
    }

    /// <summary>
    /// Painel de operacoes com o relatorio do dia (frente I, GDD §4.1; Direcao §15). So le o que as outras frentes ja calculam e nao cria regra nem
    /// numero: as partidas (sim.Voos), os avioes em solo e chegando (sim.Avioes, frente H), as filas pelo Estado das estacoes, os alertas
    /// (conta atrasada da frente A, pane, equipamento, incidente, inspecao, suprimento, banheiro e sujeira da frente F, satisfacao da frente G),
    /// e o relatorio: a receita do dia (sim.Hoje), a conta do proximo fechamento (sim.Folha), o dia fechado anterior (sim.Historico), a satisfacao
    /// com os motivos (sim.SatTop) e as ocorrencias do dia (sim.Ocorrencias, a ultima e' a do fim da lista). Nada vai no save: chamar ou nao chamar
    /// da o mesmo jogo, numero a numero (PainelTests).
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Os alertas de agora, do critico ao informativo (na mesma classe, na ordem de TipoAlerta e do indice). Lista nova a cada chamada.</summary>
        public List<Alerta> Alertas()
        {
            var r = new List<Alerta>();
            void Add(TipoAlerta t, Gravidade g, Alvo a, int i, long v = 0) => r.Add(new Alerta { Tipo = t, Gravidade = g, Alvo = a, Indice = i, Valor = v });

            if (Pausado) Add(TipoAlerta.ContaAtrasada, Gravidade.Critico, Alvo.Caixa, 0, Atrasada);
            foreach (Aviao a in Avioes) if (a.Pane) Add(TipoAlerta.Pane, Gravidade.Critico, Alvo.Aviao, a.Id);
            foreach (Station s in Estacoes) if (Falhou(s.Tipo)) Add(TipoAlerta.Equipamento, Gravidade.Critico, Alvo.Estacao, (int)s.Tipo);
            foreach (Station s in Estacoes)
                if (Desbloqueada(s.Tipo) && s.Estado == Estado.Critico) Add(TipoAlerta.FilaCritica, Gravidade.Critico, Alvo.Estacao, (int)s.Tipo, s.Fila.Count);
            // o incidente avisado ou em curso que ainda pede resposta; a falha de equipamento ja entrou acima
            if (Inc is Incidente inc && !inc.Resolvido && Steps < inc.Fim && inc.Tipo != TipoIncidente.Falha)
                Add(TipoAlerta.Incidente, Gravidade.Importante, Alvo.Voo, inc.Vaga, (int)inc.Tipo);
            for (int f = 0; f < Voos.Length; f++) if (Situacao(f) == SituacaoVoo.Atrasado) Add(TipoAlerta.VooAtrasado, Gravidade.Importante, Alvo.Voo, f);
            foreach (Aviao a in Avioes) if (a.Pendente && !a.Pane && a.Hangar == 0) Add(TipoAlerta.Inspecao, Gravidade.Importante, Alvo.Aviao, a.Id, a.Desgaste);
            for (int s = 0; s < Suprimentos; s++)
                if (VaiFaltar((Suprimento)s) is int av && av >= 0) Add(TipoAlerta.Suprimento, Gravidade.Importante, Alvo.Aviao, av, s);
            if (T.RegraLimpeza && Banheiro.Interditado) Add(TipoAlerta.BanheiroFechado, Gravidade.Importante, Alvo.Area, (int)Area.Banheiro);
            foreach (Station s in Estacoes)
                if (Desbloqueada(s.Tipo) && s.Estado == Estado.Atencao) Add(TipoAlerta.FilaAtencao, Gravidade.Importante, Alvo.Estacao, (int)s.Tipo, s.Fila.Count);
            if (T.RegraLimpeza)
            {
                // a menor tolerancia de sujeira dos perfis: a partir dela alguem ja se incomoda (frente G); o banheiro interditado ja entrou acima
                int tol = int.MaxValue;
                foreach (int x in T.TolSujeira) tol = Math.Min(tol, x);
                for (int a = 0; a < Areas; a++)
                    if (Sujeira[a] >= tol && !(a == (int)Area.Banheiro && Banheiro.Interditado)) Add(TipoAlerta.Sujeira, Gravidade.Informativo, Alvo.Area, a, Sujeira[a]);
            }
            if (SatAmostras > 0 && SatMedia < T.SatReclama)
            {
                Fator f = SatTop(0);
                int onde = SatOnde(f);
                Alvo alvo = f == Fator.Pontualidade ? Alvo.Voo : f == Fator.Limpeza || f == Fator.Banheiro ? Alvo.Area : Alvo.Estacao;
                Add(TipoAlerta.SatisfacaoBaixa, Gravidade.Informativo, alvo, onde, (int)f);
            }
            // estavel: a classe primeiro e, dentro dela, a ordem em que entraram
            var ordem = new List<Alerta>(r.Count);
            for (int g = 0; g <= (int)Gravidade.Informativo; g++) foreach (Alerta x in r) if ((int)x.Gravidade == g) ordem.Add(x);
            return ordem;
        }

        /// <summary>A situacao do voo da vaga f agora: sem voo (rota longa), previsto, embarcando, decolou ou atrasado (a hora de abrir passou sem o
        /// embarque abrir, ou o portao fechou e o aviao ainda nao decolou).</summary>
        public SituacaoVoo Situacao(int f)
        {
            Voo v = f >= 0 && f < Voos.Length ? Voos[f] : null;
            if (v == null || v.Total == 0) return SituacaoVoo.SemVoo;
            Aviao a = Avioes[v.Indice];
            if (v.Partiu) return a.VooId == v.Id && a.FechouEm >= 0 ? SituacaoVoo.Atrasado : SituacaoVoo.Decolou;
            if (v.Embarque) return SituacaoVoo.Embarcando;
            return PassoNaOnda >= v.AbrePasso ? SituacaoVoo.Atrasado : SituacaoVoo.Previsto;
        }

        /// <summary>As partidas da onda (uma linha por vaga), para o painel.</summary>
        public LinhaVoo Partida(int f)
        {
            Voo v = f >= 0 && f < Voos.Length ? Voos[f] : null;
            var l = new LinhaVoo { Vaga = f, Id = -1, Situacao = Situacao(f) };
            if (v == null) return l;
            l.Id = v.Id; l.Rota = v.Rota; l.Embarcados = v.Embarcados; l.Total = v.Total; l.AbrePasso = v.AbrePasso; l.FechaPasso = v.FechaPasso;
            foreach (Station s in Estacoes) if (s.VooEmRisco == v.Id && s.Estado != Estado.Normal) l.EmRisco = true;
            return l;
        }

        /// <summary>Avioes no patio agora (em solo) e os que voam de volta para ca (nem no patio nem no hangar).</summary>
        public int AvioesEmSolo { get { int n = 0; foreach (Aviao a in Avioes) if (a.NoPatio) n++; return n; } }
        public int AvioesChegando { get { int n = 0; foreach (Aviao a in Avioes) if (!a.NoPatio && a.Hangar == 0 && a.Chega >= 0) n++; return n; } }

        /// <summary>Quem chegou e ainda esta no terminal (frente H): descendo, andando ou esperando a mala.</summary>
        public int ChegantesNoTerminal => Chegantes.Count;

        /// <summary>A estacao mais congestionada agora (a pior Estado e, no empate, a maior fila); -1 sem fila nenhuma.</summary>
        public int Congestionada
        {
            get
            {
                int e = -1;
                foreach (Station s in Estacoes)
                {
                    if (!Desbloqueada(s.Tipo) || s.Fila.Count == 0) continue;
                    if (e < 0 || s.Estado > Estacoes[e].Estado || s.Estado == Estacoes[e].Estado && s.Fila.Count > Estacoes[e].Fila.Count) e = (int)s.Tipo;
                }
                return e;
            }
        }

        // ------------------------------------------------------------------ relatorio do dia

        /// <summary>A receita que ja entrou hoje (sim.Hoje).</summary>
        public long ReceitaHoje => Hoje.Receita;
        /// <summary>A conta que o proximo fechamento cobra, se nada mudar (sim.Folha(); 0 com a regra das despesas desligada).</summary>
        public long DespesaHoje => Folha();
        /// <summary>Receita de hoje menos a conta prevista: pode ser negativo (e' a previsao, nao o caixa).</summary>
        public long SaldoHoje => ReceitaHoje - DespesaHoje;
        /// <summary>O ultimo dia fechado (null antes do 1o fechamento).</summary>
        public DiaConta Ontem => Historico.Count > 0 ? Historico[Historico.Count - 1] : null;

        /// <summary>As ocorrencias do dia do jogo de agora, na ordem em que entraram (a ultima e' a mais nova).</summary>
        public List<Ocorrencia> OcorrenciasHoje()
        {
            var r = new List<Ocorrencia>();
            int d = DiaJogo;
            foreach (Ocorrencia o in Ocorrencias) if (o.Dia == d) r.Add(o);
            return r;
        }

        /// <summary>A ocorrencia mais nova guardada (null sem nenhuma).</summary>
        public Ocorrencia UltimaOcorrencia { get { IReadOnlyList<Ocorrencia> h = Ocorrencias; return h.Count > 0 ? h[h.Count - 1] : null; } }

        /// <summary>Hoje: os voos que decolaram com atraso ou ainda esperam (VooAtrasado e as ocorrencias com Atraso), e os passageiros que perderam o
        /// voo, contados 1x por voo (o voo pode ter mais de uma ocorrencia: o incidente e a falha).</summary>
        public int AtrasosHoje
        {
            get
            {
                var voos = new HashSet<int>();
                foreach (Ocorrencia o in OcorrenciasHoje()) if (o.Voo >= 0 && (o.Atraso > 0 || o.Tipo == TipoOcorrencia.VooAtrasado)) voos.Add(o.Voo);
                return voos.Count;
            }
        }

        public int PerdidosHoje
        {
            get
            {
                var porVoo = new Dictionary<int, int>();
                foreach (Ocorrencia o in OcorrenciasHoje())
                    if (o.Voo >= 0 && o.Perdidos > 0) porVoo[o.Voo] = Math.Max(o.Perdidos, porVoo.TryGetValue(o.Voo, out int n) ? n : 0);
                int t = 0;
                foreach (int n in porVoo.Values) t += n;
                return t;
            }
        }

        /// <summary>As reclamacoes do dia, somando os motivos (frente G).</summary>
        public int ReclamacoesHoje { get { int n = 0; for (int f = 0; f < Fatores; f++) n += Reclamacoes((Fator)f); return n; } }
    }
}
