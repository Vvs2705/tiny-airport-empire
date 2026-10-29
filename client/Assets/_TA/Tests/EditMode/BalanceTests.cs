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
    /// Fase C (CONTRATO_FASE_C §8): Planejador &gt;= 1,10 x Automatico, 200 sementes x 20 ondas (o ciclo da manutencao passa de 10 ondas).
    /// </summary>
    [Explicit("balanceamento: 200 sementes x 10 ondas por perfil")]
    public class BalanceTests
    {
        public const int Sementes = 200, Ondas = 10, OndasFaseC = 20;

        public struct Linha
        {
            public BotProfile P; public double Embarque, EmbarqueP10, Receita, Moves, Taps; public double[] Marco;
            public double SemTrip, Inspecoes, Hangares, Checagens, Trocas;
        }

        public static Linha Rodar(BotProfile p, Tuning t = null, int sementes = Sementes, int ondas = Ondas)
        {
            var l = new Linha { P = p, Marco = new double[3] };
            var emb = new double[sementes];
            var marcos = new int[3];
            for (int seed = 1; seed <= sementes; seed++)
            {
                Bot.Resultado r = Bot.Medir(p, seed, ondas, t);
                emb[seed - 1] = r.Embarque;
                double k = 1.0 / sementes;
                l.Embarque += r.Embarque * k; l.Receita += r.Receita * k; l.Moves += r.Moves * k; l.Taps += r.Taps * k;
                l.SemTrip += r.SemTrip * k; l.Inspecoes += r.Inspecoes * k; l.Hangares += r.Hangares * k; l.Checagens += r.Checagens * k; l.Trocas += r.Trocas * k;
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

        /// <summary>A tabela da Fase C: o que cada perfil fez com a tripulacao e a manutencao (medias por partida).</summary>
        public static string TabelaFaseC(Linha[] ls)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder("| perfil | embarque medio | receita | voos sem tripulacao | inspecoes | hangar | checagens | trocas de escala |\n|---|---|---|---|---|---|---|---|\n");
            foreach (Linha l in ls)
                sb.Append(string.Format(ci, "| {0} | {1:0.0}% | {2:0} | {3:0.0} | {4:0.0} | {5:0.0} | {6:0.0} | {7:0.0} |\n", l.P.Name, l.Embarque * 100, l.Receita,
                    l.SemTrip, l.Inspecoes, l.Hangares, l.Checagens, l.Trocas));
            return sb.ToString();
        }

        static string Min(double s) => s < 0 ? "-" : (s / 60).ToString("0.0", CultureInfo.InvariantCulture) + " min";

        /// <summary>As regras da v0.1: tripulacao, patio e manutencao so visuais (o "antes" do BALANCE.md).</summary>
        public static Tuning SemFaseC() => new Tuning { RegraTripulacao = false, RegraPatio = false, RegraManutencao = false };

        static void Imprime(string titulo, Linha[] ls, bool faseC = false)
        {
            TestContext.Out.WriteLine(titulo);
            TestContext.Out.WriteLine(faseC ? TabelaFaseC(ls) : Tabela(ls));
        }

        [Test]
        public void Portao_TA1_TA2()
        {
            Linha bom = Rodar(BotProfile.Bom), lento = Rodar(BotProfile.Lento), parado = Rodar(BotProfile.Parado);
            double ganho = bom.Receita / parado.Receita;
            Imprime("depois (Fase C ligada)", new[] { bom, lento, parado });
            Imprime("", new[] { bom, lento, parado }, true);
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, "ganho de mover (Bom/Parado) = {0:0.000}", ganho));
            Assert.GreaterOrEqual(bom.Embarque, 0.85, "TA-1: Bom");
            Assert.GreaterOrEqual(lento.Embarque, 0.60, "TA-1: Lento");
            Assert.GreaterOrEqual(ganho, 1.10, "TA-2: Bom / Parado");
        }

        /// <summary>O "antes": as mesmas sementes com as regras da Fase C desligadas tem de dar a tabela da v0.1 (BALANCE.md §3).</summary>
        [Test]
        public void Antes_RegrasDaV01()
        {
            Tuning t = SemFaseC();
            Linha bom = Rodar(BotProfile.Bom, t), lento = Rodar(BotProfile.Lento, t), parado = Rodar(BotProfile.Parado, t);
            Imprime("antes (regras da v0.1)", new[] { bom, lento, parado });
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, "ganho de mover (Bom/Parado) = {0:0.000}", bom.Receita / parado.Receita));
        }

        /// <summary>Portao de design do GDD §5.13: Planejador &gt;= 1,10 x Automatico com as mesmas sementes; e quanto cada sistema rende sozinho.</summary>
        [Test]
        public void Portao_FaseC_PlanejadorContraAutomatico()
        {
            Linha pl = Rodar(BotProfile.Planejador, null, Sementes, OndasFaseC), au = Rodar(BotProfile.Automatico, null, Sementes, OndasFaseC);
            double ganho = pl.Receita / au.Receita;
            Imprime("Fase C, 200 sementes x 20 ondas", new[] { pl, au }, true);
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, "ganho de planejar (Planejador/Automatico) = {0:0.000}", ganho));
            foreach (string so in new[] { "tripulacao", "manutencao", "tripulacao e manutencao" })
            {
                var t = new Tuning { RegraTripulacao = so.StartsWith("tripulacao"), RegraManutencao = so.EndsWith("manutencao") };
                double g = Rodar(BotProfile.Planejador, t, Sementes, OndasFaseC).Receita / Rodar(BotProfile.Automatico, t, Sementes, OndasFaseC).Receita;
                TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, "so {0} (com o patio): ganho {1:0.000}", so, g));
            }
            Assert.GreaterOrEqual(ganho, 1.10, "GDD §5.13: Planejador / Automatico");
        }

        /// <summary>Varredura do BALANCE.md (Fase C): cada sistema sozinho, 100 sementes x 20 ondas, ganho de planejar por variante.</summary>
        [Test]
        public void Varredura_FaseC()
        {
            var ci = CultureInfo.InvariantCulture;
            void Mede(string nome, Tuning t)
            {
                Linha pl = Rodar(BotProfile.Planejador, t, 100, OndasFaseC), au = Rodar(BotProfile.Automatico, t, 100, OndasFaseC);
                TestContext.Out.WriteLine(string.Format(ci, "| {0} | {1:0.0}% | {2:0.0}% | {3:0} | {4:0} | {5:0.000} | {6:0.0}/{7:0.0} | {8:0.0}/{9:0.0} |", nome,
                    pl.Embarque * 100, au.Embarque * 100, pl.Receita, au.Receita, pl.Receita / au.Receita, pl.SemTrip, au.SemTrip, pl.Hangares, au.Inspecoes));
            }
            TestContext.Out.WriteLine("| variante | emb. plan. | emb. auto. | receita plan. | receita auto. | ganho | sem trip. p/a | hangar p / insp. a |");
            foreach (int[] v in new[] { new[] { 3, 180 }, new[] { 2, 180 }, new[] { 3, 240 }, new[] { 2, 240 }, new[] { 2, 300 } })
                Mede("trip N=" + v[0] + " M=" + v[1], new Tuning { RegraTripulacao = true, RegraManutencao = false, TrechosAteDescanso = v[0], DescansoSeg = v[1] });
            foreach (int[] v in new[] { new[] { 10, 5, 15, 90 }, new[] { 10, 5, 15, 110 }, new[] { 10, 10, 10, 110 }, new[] { 15, 10, 10, 110 }, new[] { 12, 6, 10, 110 }, new[] { 20, 10, 10, 110 } })
                Mede("manut W=" + v[0] + " Q=" + v[1] + " C=" + v[2] + " I=" + v[3], new Tuning { RegraTripulacao = false, DesgastePorTrecho = v[0], CheckTira = v[1], CheckCusto = v[2], InspecaoSeg = v[3] });
        }
    }
}
