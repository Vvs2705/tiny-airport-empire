using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Suprimentos no patio (frente ta-suprimentos), so tela. O caminhao de combustivel ou de catering que terminou o servico e nao achou carga no
    /// deposito fica no aviao (Precisa le Aviao.Carga) com o alerta pulsando em cima, ate a entrega chegar e a carga entrar. Segue o nucleo quadro a
    /// quadro: reabrir o jogo no meio da falta cai no mesmo desenho.
    /// </summary>
    public sealed partial class Mundo
    {
        readonly Dictionary<Veiculo, SpriteRenderer> _esperaSup = new Dictionary<Veiculo, SpriteRenderer>();

        /// <summary>O suprimento que o servico carrega (-1 = nenhum): Combustivel e Limpeza (o caminhao do catering).</summary>
        static int SupDo(int servico) => servico == (int)Sim.ServicoDe(Suprimento.Combustivel) ? (int)Suprimento.Combustivel
            : servico == (int)Sim.ServicoDe(Suprimento.Refeicoes) ? (int)Suprimento.Refeicoes : -1;

        /// <summary>O caminhao fica no aviao enquanto a carga dele nao entrou.</summary>
        static bool Carregando(int servico, TA.Core.Aviao c)
        {
            int i = SupDo(servico);
            return i >= 0 && c.Carga[i] != 0;
        }

        void SuprimentosPasso(Sim s)
        {
            Quaternion q = _cam.transform.rotation;
            foreach (Veiculo v in _veiculos)
            {
                int i = v.Tipo != null ? SupDo(v.Tipo.Servico) : -1;
                if (i < 0) continue;
                bool espera = v.Lida == Lida.Servindo && v.Aviao >= 0 && s.Avioes[v.Aviao].Carga[i] == Sim.CargaEspera;
                if (!_esperaSup.TryGetValue(v, out SpriteRenderer r))
                {
                    if (!espera) continue;
                    _esperaSup[v] = r = Billboard("EsperaSup" + _esperaSup.Count, Art.Alerta(), Art.Warn, 0.75f);
                }
                r.enabled = espera;
                if (!espera) continue;
                Vector3 p = v.T.position + Vector3.up * 2.4f;
                r.transform.SetPositionAndRotation(p, q);
                r.color = Art.A(Art.Warn, Pulso());
                r.sortingOrder = Ordem(p) + 9;
            }
        }
    }
}
