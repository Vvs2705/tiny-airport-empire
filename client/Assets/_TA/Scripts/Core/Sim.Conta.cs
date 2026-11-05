using System;
using System.Collections.Generic;
using System.Text;

namespace TA.Core
{
    /// <summary>
    /// Frente de contas (docs/CONTAS.md): o brinde de ligar a conta ao Google Play Games ou ao Facebook. A conta em si mora no modulo
    /// reaproveitavel (client/Assets/Vstack/Conta) e o nucleo nao sabe quem e' o jogador: so que este save ja ganhou o brinde. Uma vez por save,
    /// e o save e' o da conta (vai para a nuvem com ela), entao e' uma vez por conta: desligar e ligar de novo, ligar o segundo login ou trocar
    /// para uma conta que ja existia nao paga de novo. Valor fixo da tabela (Tuning.BrindeConta*), nada sorteado, nada pago. O booster nao
    /// acumula alem do teto, como o do passe.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Este save ja ganhou o brinde de ligar a conta.</summary>
        public bool BrindeConta { get; private set; }

        /// <summary>A conta acabou de ganhar o primeiro login (Contas.Ligar com PrimeiraLigacao). false = ja deu (ou save recusado): nada muda.</summary>
        public bool DaBrindeConta()
        {
            if (BrindeConta || SaveRefused) return false;
            BrindeConta = true;
            int gems = (int)Math.Min(T.BrindeContaGems, T.GemsTeto - Gems);
            Gems += gems;
            BoostLeft = (int)Math.Min(BoostTeto, Math.Max(BoostLeft, (long)T.BrindeContaBoostSeg * Hz));
            Emit(Ev.GemsGanhas, -1, gems, T.BrindeContaBoostSeg);
            return true;
        }

        /// <summary>Schema 19: so grava quando ja deu.</summary>
        void SalvaConta(StringBuilder sb)
        {
            if (BrindeConta) sb.Append("brc=1\n");
        }

        /// <summary>Le o schema 19. Os schemas 18 e anteriores nao tem a chave: o brinde ainda nao foi dado.</summary>
        void CarregaConta(Dictionary<string, string> kv)
        {
            BrindeConta = Get(kv, "brc", 0) == 1;
        }
    }
}
