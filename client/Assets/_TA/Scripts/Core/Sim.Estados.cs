using System;

namespace TA.Core
{
    public sealed partial class Sim
    {
        /// <summary>
        /// Estado de cada estacao (GDD §5.6, CONTRATO_V01 §3.6), a cada 0,5 s de jogo: para cada passageiro na fila ou em atendimento, a hora
        /// estimada de embarque "no ritmo atual" (posicao na fila x tempo da estacao / servidores, mais o tempo das estacoes seguintes sem
        /// fila, esperando a abertura do embarque). Folga = fechamento do voo dele - essa hora. Critico = alguma folga negativa; atencao =
        /// menor que Tuning.AtencaoFolga. Estacao sem funcionario conta 1 servidor (o toque do jogador): sem isso toda fila de toque seria
        /// "critica" e o vermelho deixaria de dizer qual voo esta em risco. Muda de estado com emitir = Ev.Fila (o diario mede o K2).
        /// ponytail: a fila das estacoes seguintes nao entra na conta (otimista); suficiente para o bot e a cor, refinar se o playtest pedir.
        /// </summary>
        public void AtualizaEstados(bool emitir = false)
        {
            double agora = PassoNaOnda * (double)Step;
            foreach (Station s in Estacoes)
            {
                double min = double.PositiveInfinity;
                int risco = -1;
                int servidores = Math.Max(1, Math.Min(s.Postos, Presentes(s.Tipo)));
                double t = TempoSeg(s.Tipo) * (Falhou(s.Tipo) ? T.FalhaFator : 1);
                foreach (Pax p in s.Atendendo)
                {
                    Voo v = VooDe(p);
                    if (v != null) Pior(v, s.Tipo == Estacao.Portao ? agora + p.Resto * (double)Step : Saida(s.Tipo, agora + p.Resto * (double)Step, v), ref min, ref risco);
                }
                for (int pos = 0; pos < s.Fila.Count; pos++)
                {
                    Voo v = VooDe(s.Fila[pos]);
                    if (v == null) continue;
                    double fim;
                    if (s.Tipo == Estacao.Portao)
                    {
                        int antes = 0;   // so quem embarca antes dele: voo que abre antes ou junto
                        for (int j = 0; j < pos; j++) { Voo o = VooDe(s.Fila[j]); if (o != null && o.AbrePasso <= v.AbrePasso) antes++; }
                        fim = Math.Max(agora, v.AbrePasso * (double)Step) + (antes + 1) * t / servidores;
                    }
                    else fim = Saida(s.Tipo, agora + (pos + 1) * t / servidores, v);
                    Pior(v, fim, ref min, ref risco);
                }
                Estado e = min < 0 ? Estado.Critico : min < T.AtencaoFolga ? Estado.Atencao : Estado.Normal;
                bool mudou = e != s.Estado;
                s.Estado = e; s.Folga = min; s.VooEmRisco = e == Estado.Normal ? -1 : risco;
                if (emitir && mudou) Emit(Ev.Fila, (int)s.Tipo, s.Fila.Count, (int)e);
            }
        }

        /// <summary>Hora (s na onda) em que embarca quem sai da estacao `e` no instante `sai`.</summary>
        double Saida(Estacao e, double sai, Voo v)
        {
            if (e == Estacao.CheckIn || e == Estacao.Bagagem) sai += TempoSeg(Estacao.Seguranca);
            return Math.Max(sai, v.AbrePasso * (double)Step) + TempoSeg(Estacao.Portao);
        }

        static void Pior(Voo v, double fim, ref double min, ref int risco)
        {
            double folga = v.FechaPasso * (double)Step - fim;
            if (folga < min) { min = folga; risco = v.Id; }
        }
    }
}
