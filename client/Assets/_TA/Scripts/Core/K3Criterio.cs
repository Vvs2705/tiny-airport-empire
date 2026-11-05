using System.Collections.Generic;
using System.Globalization;

namespace TA.Core
{
    /// <summary>
    /// O criterio do K3 (GDD §5.13, P-13): p10 do FPS &gt;= 30 com 60 pessoas, 2 avioes em movimento e 6 veiculos de patio na tela.
    /// Um lugar so para o veredito do -perf do build (Game.Perf), do client/tools/k3.sh e dos K3Tests. Sem K3 com gente desde 2026-11-05:
    /// a medida e' de maquina, no PC a cada leva e no aparelho no ultimo passo.
    /// </summary>
    public static class K3Criterio
    {
        public const float P10Min = 30f;
        public const int Pessoas = 60, Avioes = 2, Veiculos = 6;

        /// <summary>null = passou; senao, o que faltou, em pt-BR ("p10 24,1 &lt; 30; pessoas 52 &lt; 60").</summary>
        public static string Falta(float p10, float p10Min, int pessoas, int avioes, int veiculos)
        {
            var f = new List<string>();
            if (!(p10 >= p10Min)) f.Add(string.Format(CultureInfo.InvariantCulture, "p10 {0:0.0} < {1:0.#}", p10, p10Min));   // NaN tambem falha
            if (pessoas < Pessoas) f.Add("pessoas " + pessoas + " < " + Pessoas);
            if (avioes < Avioes) f.Add("avioes " + avioes + " < " + Avioes);
            if (veiculos < Veiculos) f.Add("veiculos " + veiculos + " < " + Veiculos);
            return f.Count == 0 ? null : string.Join("; ", f);
        }
    }
}
