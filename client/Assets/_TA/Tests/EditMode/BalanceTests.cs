using System;
using System.Globalization;
using System.Linq;
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
            public double SemTrip, Inspecoes, Hangares, Checagens, Trocas, Incidentes, Reacoes;
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
                l.Incidentes += r.Incidentes * k; l.Reacoes += r.Reacoes * k;
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
        public static Tuning SemFaseC() => FechaC3(new Tuning { RegraTripulacao = false, RegraPatio = false, RegraManutencao = false });

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

        /// <summary>Os 2 sistemas com escolha da Fase C2 (CONTRATO_FASE_C2 §5): a especializacao (item 5) e as 4 estacoes novas (item 4, uma
        /// decisao so: tirar gente do fluxo para uma estacao nova). As estacoes uma a uma saem na tabela, sem portao (BALANCE.md D-C2c).</summary>
        public static readonly string[] SistemasC2 = { "especialista", "estacoes" };
        static readonly string[] EstacoesC2 = { "bagagem", "alimentacao", "oficina", "despacho" };

        /// <summary>So o sistema `so` com regra ("estacoes" = as 4; ou uma estacao pelo nome), tudo aberto desde o comeco (o desbloqueio so
        /// atrasa a entrada).</summary>
        public static Tuning SoC2(string so, Tuning b = null)
        {
            Tuning t = b ?? new Tuning();
            bool todas = so == "estacoes";
            t.RegraEspecialista = so == "especialista"; t.RegraBagagem = todas || so == "bagagem"; t.RegraAlimentacao = todas || so == "alimentacao";
            t.RegraOficina = todas || so == "oficina"; t.RegraDespacho = todas || so == "despacho";
            t.Abre = new int[8]; t.PerfilMarco = new int[6]; t.EspecialistaMarco = 0;
            return t;
        }

        /// <summary>A chave de cada sistema no Tuning padrao (o que o portao deixou com regra).</summary>
        public static bool RegraPadrao(string so)
        {
            Tuning t = Tuning.Padrao;
            return so == "especialista" ? t.RegraEspecialista : t.RegraBagagem && t.RegraAlimentacao && t.RegraOficina && t.RegraDespacho;
        }

        /// <summary>Portao do GDD §5.13 por sistema novo com escolha: a equipe planejada &gt;= 1,10 x a automatica, ou o sistema fica so visual.</summary>
        [Test]
        public void Portao_FaseC2_PorSistema()
        {
            var ci = CultureInfo.InvariantCulture;
            TestContext.Out.WriteLine("| sistema | emb. planejada | emb. automatica | receita planejada | receita automatica | ganho | regra |\n|---|---|---|---|---|---|---|");
            string falhas = "";
            foreach (string so in new[] { "especialista", "estacoes", "bagagem", "alimentacao", "oficina", "despacho" })
            {
                Tuning t = SoC2(so);
                Linha pl = Rodar(BotProfile.EquipePlanejada, t, Sementes, OndasFaseC), au = Rodar(BotProfile.EquipeAutomatica, t, Sementes, OndasFaseC);
                double g = pl.Receita / au.Receita;
                bool sistema = System.Array.IndexOf(SistemasC2, so) >= 0;
                TestContext.Out.WriteLine(string.Format(ci, "| {0} | {1:0.0}% | {2:0.0}% | {3:0} | {4:0} | {5:0.000} | {6} |", so, pl.Embarque * 100, au.Embarque * 100,
                    pl.Receita, au.Receita, g, !sistema ? "(parte das estacoes)" : RegraPadrao(so) ? "ligada" : "so visual"));
                if (sistema && g < 1.10 && RegraPadrao(so)) falhas += " " + so + "=" + g.ToString("0.000", ci);
            }
            Assert.IsEmpty(falhas, "GDD §5.13: sistema com regra precisa de ganho >= 1,10");
        }

        /// <summary>O "antes" da Fase C2: as estacoes novas, os perfis novos e a especializacao nunca abrem (a main da Fase C).</summary>
        public static Tuning SemFaseC2() => FechaC3(new Tuning { Abre = new[] { 0, 0, 0, 0, 99, 99, 99, 99 }, PerfilMarco = new[] { 0, 0, 0, 99, 99, 99 }, EspecialistaMarco = 99 });

        [Test]
        public void Antes_FaseC2()
        {
            Tuning t = SemFaseC2();
            Linha bom = Rodar(BotProfile.Bom, t), lento = Rodar(BotProfile.Lento, t), parado = Rodar(BotProfile.Parado, t);
            Imprime("antes da Fase C2 (a main da Fase C)", new[] { bom, lento, parado });
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, "ganho de mover (Bom/Parado) = {0:0.000}", bom.Receita / parado.Receita));
        }

        /// <summary>O "antes" da Fase C3: so as 2 rotas iniciais e Vale Verde (a main da Fase C2). Tem de repetir a tabela dela, numero a numero.</summary>
        public static Tuning SemFaseC3() => FechaC3(new Tuning());

        static Tuning FechaC3(Tuning t)
        {
            t.Rotas = Tabelas.Rotas.Take(2).ToArray();
            t.Aeroportos = Tabelas.Aeroportos.Take(1).ToArray();
            return t;
        }

        [Test]
        public void Antes_FaseC3()
        {
            Tuning t = SemFaseC3();
            Linha bom = Rodar(BotProfile.Bom, t), lento = Rodar(BotProfile.Lento, t), parado = Rodar(BotProfile.Parado, t);
            Imprime("antes da Fase C3 (a main da Fase C2)", new[] { bom, lento, parado });
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, "ganho de mover (Bom/Parado) = {0:0.000}", bom.Receita / parado.Receita));
            Assert.AreEqual((6913, 3838, 4911), ((int)Math.Round(bom.Receita), (int)Math.Round(lento.Receita), (int)Math.Round(parado.Receita)), "o comeco nao mudou");
        }

        /// <summary>Tudo aberto desde o comeco (o desbloqueio so atrasa a entrada), comecando no aeroporto `aero`: o que a Fase C3 mede.</summary>
        public static Tuning AbertoC3(int aero)
        {
            var t = new Tuning { Abre = new int[8], PerfilMarco = new int[6], AeroportoInicial = aero };
            t.Rotas = Tabelas.Rotas.Select(r => { Rota c = r.Copia(); c.Marco = 0; return c; }).ToArray();
            t.Aeroportos = Tabelas.Aeroportos.Select(a => { Aeroporto c = a.Copia(); c.Marco = 0; return c; }).ToArray();
            return t;
        }

        static BotProfile Sem(string nome, bool rotas, bool viaja, bool incidentes)
        {
            BotProfile p = BotProfile.Bom;
            p.Name = nome; p.Rotas = rotas; p.Viaja = viaja; p.Incidentes = incidentes;
            return p;
        }

        static void Portao(string sistema, Tuning t, BotProfile pl, BotProfile au, ref string falhas, bool regra = true, bool portao = true)
        {
            var ci = CultureInfo.InvariantCulture;
            Linha a = Rodar(pl, t, Sementes, OndasFaseC), b = Rodar(au, t, Sementes, OndasFaseC);
            double g = a.Receita / b.Receita;
            TestContext.Out.WriteLine(string.Format(ci, "| {0} | {1:0.0}% | {2:0.0}% | {3:0} | {4:0} | {5:0.000} | {6:0.0} | {7:0.0} |", sistema, a.Embarque * 100, b.Embarque * 100, a.Receita, b.Receita, g, a.Incidentes, a.Reacoes));
            if (portao && regra && g < 1.10) falhas += " " + sistema + "=" + g.ToString("0.000", ci);
        }

        /// <summary>Portao do GDD §5.13 por sistema da Fase C3 (CONTRATO_FASE_C3 §7), 200 sementes x 20 ondas: rotas e incidentes em cada aeroporto
        /// onde existem, aeroportos partindo de Vale Verde. Os tipos de incidente um a um saem na tabela, sem portao proprio.</summary>
        [Test]
        public void Portao_FaseC3_PorSistema()
        {
            TestContext.Out.WriteLine("| sistema | emb. planejador | emb. automatico | receita planejador | receita automatico | ganho | incidentes | reacoes |\n|---|---|---|---|---|---|---|---|");
            string falhas = "";
            BotProfile comRotas = Sem("com rotas", true, false, false), semRotas = Sem("sem rotas", false, false, false);
            for (int a = 0; a < 3; a++)
            {
                Tuning t = AbertoC3(a);
                t.RegraIncidentes = false;
                Portao("rotas em " + Tabelas.Aeroportos[a].Nome, t, comRotas, semRotas, ref falhas, Tuning.Padrao.RegraRotas);
            }
            Tuning av = AbertoC3(0);
            av.RegraIncidentes = false;
            Portao("aeroportos", av, Sem("viaja", true, true, false), comRotas, ref falhas, Tuning.Padrao.RegraAeroportos);
            BotProfile reage = Sem("reage", true, false, true);
            for (int a = 1; a < 3; a++) Portao("incidentes em " + Tabelas.Aeroportos[a].Nome, AbertoC3(a), reage, comRotas, ref falhas, Tuning.Padrao.RegraIncidentes);
            for (int tipo = 0; tipo < 4; tipo++)
            {
                Tuning t = AbertoC3(2);
                int k = tipo;
                t.Aeroportos = t.Aeroportos.Select(x => { Aeroporto c = x.Copia(); c.Pesos = new int[4]; c.Pesos[k] = 1; return c; }).ToArray();
                Portao("so " + (TipoIncidente)tipo, t, reage, comRotas, ref falhas, true, false);
            }
            Assert.IsEmpty(falhas, "GDD §5.13: sistema com regra precisa de ganho >= 1,10");
        }

        /// <summary>TA-6 (GDD §5.7): nenhuma rota domina. As 2 vagas presas em cada rota, sem incidente, no mesmo aeroporto: a melhor rende no
        /// maximo 1,20 x a segunda.</summary>
        [Test]
        public void Portao_TA6_NenhumaRotaDomina()
        {
            var ci = CultureInfo.InvariantCulture;
            string falhas = "";
            TestContext.Out.WriteLine("| aeroporto | rota | embarque | receita |\n|---|---|---|---|");
            for (int a = 0; a < 3; a++)
            {
                var rs = Enumerable.Range(0, Tabelas.Rotas.Length).Where(r => Tabelas.Rotas[r].Aeroporto == a).ToArray();
                var receita = rs.Select(r =>
                {
                    Tuning t = AbertoC3(a);
                    t.RegraIncidentes = false;
                    t.RotaFixa = r;
                    Linha l = Rodar(Sem("fixa", false, false, false), t, Sementes, OndasFaseC);
                    TestContext.Out.WriteLine(string.Format(ci, "| {0} | {1} | {2:0.0}% | {3:0} |", Tabelas.Aeroportos[a].Nome, Tabelas.Rotas[r].Nome, l.Embarque * 100, l.Receita));
                    return l.Receita;
                }).OrderByDescending(x => x).ToArray();
                if (receita[0] > 1.20 * receita[1]) falhas += " " + Tabelas.Aeroportos[a].Nome + "=" + (receita[0] / receita[1]).ToString("0.000", ci);
            }
            Assert.IsEmpty(falhas, "TA-6: a melhor rota <= 1,20 x a segunda");
        }

        /// <summary>A partida longa: 2 h de jogo (40 ondas) pelo desbloqueio normal, com a chegada a cada aeroporto.</summary>
        [Test]
        public void Longo_FaseC3()
        {
            var ci = CultureInfo.InvariantCulture;
            foreach (BotProfile p in BotProfile.All)
            {
                double emb = 0, rec = 0, inc = 0, reac = 0; int[] chegou = new int[3]; double[] onda = new double[3];
                for (int seed = 1; seed <= Sementes; seed++)
                {
                    Bot.Resultado r = Bot.Medir(p, seed, 40);
                    emb += r.Embarque / Sementes; rec += r.Receita / (double)Sementes; inc += r.Incidentes / (double)Sementes; reac += r.Reacoes / (double)Sementes;
                    for (int a = 0; a < 3; a++) if (r.ChegouEm[a] >= 0) { chegou[a]++; onda[a] += r.ChegouEm[a]; }
                }
                TestContext.Out.WriteLine(string.Format(ci, "{0}: embarque {1:0.0}% receita {2:0} incidentes {3:0.0} reacoes {4:0.0} chegou Costa {5}/200 onda {6:0.0}, Aurora {7}/200 onda {8:0.0}", p.Name, emb * 100, rec, inc, reac,
                    chegou[1], chegou[1] > 0 ? onda[1] / chegou[1] : -1, chegou[2], chegou[2] > 0 ? onda[2] / chegou[2] : -1));
            }
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
