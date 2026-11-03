using System;

namespace TA.Core
{
    /// <summary>Fase da transferencia (Direcao §9.3): a View anda com o boneco e troca o uniforme por ela; a ficha mostra o nome e o tempo.</summary>
    public enum Transferencia { Nenhuma, Encerrando, IndoVestiario, Trocando, IndoPosto }

    /// <summary>
    /// Frente E: transferencia pelo vestiario (GDD §4.1; Direcao §9.3 e §9.6). Nada novo no save: a fase sai do Funcionario.Andando (os passos
    /// que faltam), contada do fim: ultima perna (meia Caminhada), troca (Tuning.Troca), ida ao vestiario (meia Caminhada) e, antes, o resto do
    /// atendimento em curso. Funcionario.De guarda o uniforme que ele veste ate o fim da troca; De == Em com Andando > 0 e' so a caminhada (voltou
    /// ao setor do uniforme). Quem esta em transferencia nao atende (Andando > 0 tira das Presentes).
    /// ponytail: a validacao de qualificacao e treinamento (Direcao §9.3, passos 3 e 4) fica no roteiro (GDD §4.4).
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Ida ao vestiario e volta ao posto: as duas somam a caminhada da v0.1 (o vestiario fica no caminho).</summary>
        int Perna => Passos(T.Caminhada * 0.5f);

        /// <summary>Passos de uma transferencia, sem o encerramento (com a regra desligada, so a Caminhada).</summary>
        public int TransferePassos => T.RegraVestiario ? 2 * Perna + Passos(T.Troca) : Passos(T.Caminhada);

        /// <summary>Em que parte da transferencia o funcionario esta (Nenhuma = presente no posto).</summary>
        public Transferencia Fase(Funcionario f)
        {
            if (f.Andando == 0) return Transferencia.Nenhuma;
            int volta = Perna;
            if (!T.RegraVestiario || f.De == f.Em || f.Andando <= volta) return Transferencia.IndoPosto;
            int troca = volta + Passos(T.Troca);
            return f.Andando <= troca ? Transferencia.Trocando : f.Andando <= troca + volta ? Transferencia.IndoVestiario : Transferencia.Encerrando;
        }

        /// <summary>O uniforme que ele veste agora: o do setor de origem ate o fim da troca, no vestiario.</summary>
        public Estacao Veste(Funcionario f)
        {
            Transferencia t = Fase(f);
            return t == Transferencia.Nenhuma || t == Transferencia.IndoPosto ? f.Em : f.De;
        }

        /// <summary>Quanto da preparacao ja foi (0 a 1; 1 = no posto). O encerramento fica no 0: ele ainda esta no posto antigo.</summary>
        public float Preparo(Funcionario f)
        {
            if (f.Andando == 0) return 1f;
            int total = f.De == f.Em ? Passos(T.Caminhada) : TransferePassos;
            return 1f - Math.Min(f.Andando, total) / (float)total;
        }

        /// <summary>Passos ate o atendimento em curso terminar (Direcao §9.3, passo 5): quem sai conclui a tarefa. 0 quando os outros presentes
        /// cobrem os atendimentos ou nao ha nenhum. ponytail: espera o mais longo da estacao, nao o "dele" (o nucleo nao liga passageiro a
        /// balcao); com falha de equipamento o atendimento anda mais devagar e ele sai antes do fim.</summary>
        int Encerra(Estacao e)
        {
            Station s = St(e);
            if (!T.RegraVestiario || s.Atendendo.Count < Presentes(e)) return 0;
            int n = 0;
            foreach (Pax p in s.Atendendo) n = Math.Max(n, p.Resto);
            return n;
        }

        /// <summary>Teto do Andando de um save: a transferencia mais o atendimento mais longo da origem (lixo vira o teto; so a caminhada sem troca).</summary>
        int AndandoMax(Estacao em, Estacao de) => de == em ? Passos(T.Caminhada) : TransferePassos + TempoBase(de);
    }
}
