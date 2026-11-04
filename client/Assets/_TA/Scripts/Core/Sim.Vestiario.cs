using System;

namespace TA.Core
{
    /// <summary>Fase da transferencia (Direcao §9.3): a View anda com o boneco e troca o uniforme por ela; a ficha mostra o nome e o tempo.</summary>
    public enum Transferencia { Nenhuma, Encerrando, IndoVestiario, Trocando, IndoPosto }

    /// <summary>
    /// Frente E: transferencia pelo vestiario (GDD §4.1; Direcao §9.3 e §9.6). Nada novo no save: a fase sai do Funcionario.Andando (os passos
    /// que faltam), contada do fim: ultima perna, troca (Tuning.Troca) e ida ao vestiario, que somam a Caminhada da v0.1, e, antes, o resto do
    /// atendimento em curso. A soma nao cresce: com +1 s, o LentoGuiado fecha a campanha em 131 de 200 sementes (BALANCE.md §13). Funcionario.De guarda o uniforme que ele veste ate o fim da troca; De == Em com Andando > 0 e' so a caminhada (voltou
    /// ao setor do uniforme). Quem esta em transferencia nao atende (Andando > 0 tira das Presentes).
    /// ponytail: a validacao de qualificacao e treinamento (Direcao §9.3, passos 3 e 4) fica no roteiro (GDD §4.4).
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Passos de uma transferencia, sem o encerramento: os da caminhada da v0.1, com ou sem a regra.</summary>
        public int TransferePassos => Passos(T.Caminhada);

        /// <summary>Passos da troca e da ultima perna (a ida ao vestiario fica com o resto, o passo impar).</summary>
        int TrocaPassos => Math.Min(Passos(T.Troca), TransferePassos - 2);
        int Volta => (TransferePassos - TrocaPassos) / 2;

        /// <summary>Em que parte da transferencia o funcionario esta (Nenhuma = presente no posto).</summary>
        public Transferencia Fase(Funcionario f)
        {
            if (f.Andando == 0) return Transferencia.Nenhuma;
            if (!T.RegraVestiario || f.De == f.Em || f.Andando <= Volta) return Transferencia.IndoPosto;
            return f.Andando <= Volta + TrocaPassos ? Transferencia.Trocando : f.Andando <= TransferePassos ? Transferencia.IndoVestiario : Transferencia.Encerrando;
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
            return 1f - Math.Min(f.Andando, TransferePassos) / (float)TransferePassos;
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

        /// <summary>Teto do Andando de um save: a transferencia mais o atendimento mais longo da origem (lixo vira o teto; sem troca, so a transferencia).</summary>
        int AndandoMax(Estacao em, Estacao de) => de == em ? TransferePassos : TransferePassos + TempoBase(de);
    }
}
