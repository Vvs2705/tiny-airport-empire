using System;

namespace TA.Core
{
    /// <summary>
    /// As 8 estacoes e os 6 perfis (CONTRATO_FASE_C2): o que abre em cada marco, o que cada estacao nova faz e quanto o especialista rende.
    /// Antes do marco 1 nada daqui muda o jogo da v0.1 com a Fase C. Sem sorteio: a semente so diz quem tem mala ou fome.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>A estacao ja abriu pelo marco do terminal (Ev.EstacaoAberta).</summary>
        public bool Desbloqueada(Estacao e) => (int)e >= 0 && (int)e < Estacoes.Length && Marco >= T.Abre[(int)e];

        /// <summary>Aberta e com regra (chave Tuning.Regra*: desligada = so visual, a equipe fica la mas a estacao nao rende).</summary>
        public bool Ativa(Estacao e)
        {
            if (!Desbloqueada(e)) return false;
            switch (e)
            {
                case Estacao.Bagagem: return T.RegraBagagem;
                case Estacao.Alimentacao: return T.RegraAlimentacao;
                case Estacao.Manutencao: return T.RegraOficina;
                case Estacao.Despacho: return T.RegraDespacho;
                default: return true;
            }
        }

        /// <summary>Paradas opcionais do fluxo (loja, bagagem, alimentacao): so com alguem presente; sem ninguem o passageiro passa direto.</summary>
        static bool Opcional(Estacao e) => e == Estacao.Loja || e == Estacao.Bagagem || e == Estacao.Alimentacao;

        bool Funcionando(Estacao e) => Ativa(e) && Presentes(e) > 0;

        /// <summary>Onde o especialista vende mais em vez de atender mais rapido (o Vendedor, GDD §5.5).</summary>
        static bool Vende(Estacao e) => e == Estacao.Loja || e == Estacao.Alimentacao;

        /// <summary>O passageiro vai a parada opcional `e`: tem o motivo (loja, mala, fome), ela funciona e o voo dele tem LojaFolgaMin s de folga.</summary>
        bool Para(bool motivo, Estacao e, Voo v, int loc) => motivo && Funcionando(e) && v != null && v.FechaPasso - loc > T.LojaFolgaMin * Hz;

        /// <summary>A especializacao ja vale (marco Tuning.EspecialistaMarco e a chave RegraEspecialista).</summary>
        public bool Especializa => T.RegraEspecialista && Marco >= T.EspecialistaMarco;

        bool Especialista(Funcionario f, Estacao e) => Especializa && Funcionario.Especialidade(f.Perfil, e);

        /// <summary>Quanto a estacao rende pelo especialista presente: Tuning.Especialista se ha um la, senao 1 (tempo / x; venda x).
        /// ponytail: vale para a estacao toda, nao por balcao; por balcao se o playtest pedir.</summary>
        public float Rende(Estacao e)
        {
            foreach (Funcionario f in Staff) if (f.Em == e && f.Andando == 0 && Especialista(f, e)) return T.Especialista;
            return 1f;
        }

        /// <summary>Forca dos presentes na oficina ou no despacho (ate os postos dela): 1 por pessoa, Tuning.Especialista o especialista.</summary>
        public float Forca(Estacao e)
        {
            float soma = 0f; int n = 0;
            foreach (Funcionario f in Staff)
                if (f.Em == e && f.Andando == 0 && n++ < St(e).Postos) soma += Especialista(f, e) ? T.Especialista : 1f;
            return soma;
        }

        /// <summary>Passos da checagem e da inspecao: o mecanico da Fase C sozinho, ou dividido com a oficina (CONTRATO_FASE_C2 §2: tempo /
        /// (1 + forca), com o upgrade de velocidade da oficina). Sai no comeco: quem chega ou sai no meio nao muda a que ja comecou.</summary>
        int PassosOficina(float seg) => Ativa(Estacao.Manutencao)
            ? Passos((float)(seg * Math.Pow(T.VelFator, St(Estacao.Manutencao).Vel) / (1f + Forca(Estacao.Manutencao))))
            : Passos(seg);
    }
}
