using System;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>
    /// Portao da Fase B (CONTRATO_V01 §8, linhas 1 e 2), como os BalanceTests do Forge: [Explicit], 200 sementes x 10 ondas por perfil, e
    /// imprime a tabela que vai para o docs/BALANCE.md. Rodar: dotnet test client/tools/coretests --filter "FullyQualifiedName~BalanceTests"
    /// TA-1: embarque medio por voo &gt;= 85% com o Bom e &gt;= 60% com o Lento. TA-2: receita do Bom &gt;= 1,10 x a do Parado (mesmas sementes).
    /// </summary>
    [Explicit("balanceamento: 200 sementes x 10 ondas por perfil")]
    public class BalanceTests
    {
        public const int Sementes = 200, Ondas = 10;

        public struct Linha { public BotProfile P; public double Embarque, EmbarqueP10, Receita, Moves, Taps; public double[] Marco; }

        public static Linha Rodar(BotProfile p, Tuning t = null, int sementes = Sementes, int ondas = Ondas)
        {
            var l = new Linha { P = p, Marco = new double[3] };
            var emb = new double[sementes];
            var marcos = new int[3];
            for (int seed = 1; seed <= sementes; seed++)
            {
                Bot.Resultado r = Bot.Medir(p, seed, ondas, t);
                emb[seed - 1] = r.Embarque;
                l.Embarque += r.Embarque / sementes; l.Receita += r.Receita / (double)sementes;
                l.Moves += r.Moves / (double)sementes; l.Taps += r.Taps / (double)sementes;
                for (int m = 0; m < 3; m++) if (r.MarcoEm[m] >= 0) { l.Marco[m] += r.MarcoEm[m]; marcos[m]++; }
            }
            for (int m = 0; m < 3; m++) l.Marco[m] = marcos[m] > 0 ? l.Marco[m] / marcos[m] : -1;
            Array.Sort(emb);
            l.EmbarqueP10 = emb[sementes / 10];
            return l;
        }

        public static string Tabela(Linha[] ls)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder("| perfil | embarque medio | p10 | receita | movimentos | toques | marco 1 | marco 2 | marco 3 |\n|---|---|---|---|---|---|---|---|---|\n");
            foreach (Linha l in ls)
                sb.Append(string.Format(ci, "| {0} | {1:0.0}% | {2:0.0}% | {3:0} | {4:0.0} | {5:0} | {6} | {7} | {8} |\n", l.P.Name, l.Embarque * 100, l.EmbarqueP10 * 100,
                    l.Receita, l.Moves, l.Taps, Min(l.Marco[0]), Min(l.Marco[1]), Min(l.Marco[2])));
            return sb.ToString();
        }

        static string Min(double s) => s < 0 ? "-" : (s / 60).ToString("0.0", CultureInfo.InvariantCulture) + " min";

        [Test]
        public void Portao_TA1_TA2()
        {
            Linha bom = Rodar(BotProfile.Bom), lento = Rodar(BotProfile.Lento), parado = Rodar(BotProfile.Parado);
            double ganho = bom.Receita / parado.Receita;
            TestContext.Out.WriteLine(Tabela(new[] { bom, lento, parado }));
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, "ganho de mover (Bom/Parado) = {0:0.000}", ganho));
            Assert.GreaterOrEqual(bom.Embarque, 0.85, "TA-1: Bom");
            Assert.GreaterOrEqual(lento.Embarque, 0.60, "TA-1: Lento");
            Assert.GreaterOrEqual(ganho, 1.10, "TA-2: Bom / Parado");
        }
    }
}
