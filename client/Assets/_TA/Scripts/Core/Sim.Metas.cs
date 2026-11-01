using System;

namespace TA.Core
{
    /// <summary>
    /// Campanha principal (CONTRATO_FASE_C5, docs/CAMPANHA.md): os objetivos de cada aeroporto concluem na ordem da tabela. Meta de estado le o
    /// jogo; meta de contador conta eventos naquele aeroporto, tambem antes de o objetivo virar o ativo: a cadeia nao espera a semente repetir
    /// um perigo que ja passou. Conclui no passo fixo e paga Cash fixo, fora do Ganha: sem 2x e fora do cofre.
    /// A campanha guia, nao tranca: nenhum objetivo abre nada. Com o jogo fechado nada anda. Tuning.RegraCampanha = false = conta e mostra, sem pagar.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Objetivos feitos em cada cadeia e o contador de cada objetivo.</summary>
        readonly int[] _feitos;
        readonly int[][] _conta;

        public int ObjetivosFeitos(int aeroporto) => _feitos[aeroporto];

        /// <summary>O objetivo ativo do aeroporto (null = cadeia fechada).</summary>
        public Objetivo ObjetivoAtual(int aeroporto) => _feitos[aeroporto] < T.Objetivos[aeroporto].Length ? T.Objetivos[aeroporto][_feitos[aeroporto]] : null;

        public bool CampanhaCompleta
        {
            get
            {
                for (int a = 0; a < _feitos.Length; a++) if (ObjetivoAtual(a) != null) return false;
                return true;
            }
        }

        /// <summary>Onde o objetivo ativo esta, para comparar com o Alvo dele (0 = cadeia fechada).</summary>
        public int Progresso(int aeroporto)
        {
            Objetivo o = ObjetivoAtual(aeroporto);
            if (o == null) return 0;
            switch (o.Meta)
            {
                case Meta.Marco: return Marco;
                case Meta.Equipe: return Staff.Count;
                case Meta.Velocidade: return St((Estacao)o.Arg).Vel;
                case Meta.Capacidade: return St((Estacao)o.Arg).Cap;
                case Meta.Chegou: return Pago(o.Arg) && ProximoAeroporto != o.Arg ? 1 : 0;
                default: return _conta[aeroporto][_feitos[aeroporto]];
            }
        }

        /// <summary>Fim do passo fixo: conclui e paga. Uma por cadeia e por passo.</summary>
        void PassoMetas()
        {
            for (int a = 0; a < _feitos.Length; a++)
            {
                Objetivo o = ObjetivoAtual(a);
                if (o == null || Progresso(a) < o.Alvo) continue;
                int pago = T.RegraCampanha ? o.Recompensa : 0;
                Cash += pago;
                Emit(Ev.ObjetivoConcluido, a, _feitos[a]++, pago);
                if (T.RegraCampanha && ObjetivoAtual(a) == null) GanhaGems(a);
            }
        }

        /// <summary>Soma nos objetivos ainda abertos deste aeroporto (zera = a sequencia de Seguidos quebrou).</summary>
        void Conta(Meta m, int arg = -1, bool zera = false)
        {
            int a = AeroportoAtual;
            Objetivo[] c = T.Objetivos[a];
            for (int i = _feitos[a]; i < c.Length; i++)
                if (c[i].Meta == m && (c[i].Arg < 0 || c[i].Arg == arg)) _conta[a][i] = zera ? 0 : Math.Min(c[i].Alvo, _conta[a][i] + 1);
        }

        /// <summary>Todo evento passa aqui (Emit). Oferta, anuncio e cofre nao entram: nenhum objetivo depende deles.</summary>
        void AnotaMeta(Ev t, int a, int b, int c)
        {
            switch (t)
            {
                case Ev.Embarcou: Conta(Meta.Embarcados); break;
                case Ev.Historia: Conta(Meta.Historias); break;
                case Ev.Bonus: Conta(Meta.Pontuais); break;
                case Ev.HangarFeito: Conta(Meta.Hangar); break;
                case Ev.ChecagemIniciada: if (c == 0) Conta(Meta.Checagens); break;
                case Ev.Movido: if (St((Estacao)c).Estado != Estado.Normal) Conta(Meta.Movidos); break;
                case Ev.IncidenteFim: if (b == 1) Conta(ClimaDe((TipoIncidente)a) != null ? Meta.Climas : Meta.Incidentes, a); break;
                case Ev.VooPartiu:
                    if (c == 0) break;
                    int rota = Voos[a % 2].Rota;
                    if (b > 0) Conta(Meta.Voos, rota);
                    if (b * 100 >= c * T.LotadoPct) { Conta(Meta.Cheios, rota); Conta(Meta.Seguidos); }
                    else Conta(Meta.Seguidos, -1, true);
                    break;
            }
        }
    }
}
