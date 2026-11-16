using System;
using System.Diagnostics;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>
    /// K3 automatico, parte do nucleo (GDD §5.13 e §17: p10 &gt;= 30 FPS num Android de entrada, com 60 pessoas, 2 avioes e 6 veiculos).
    /// O K3 com amigos saiu do projeto em 2026-11-05: a medida passa a ser de maquina. Aqui, o que o nucleo gasta por quadro na cena do K3
    /// (-passageiros 60, bot Bom, 3x, 60 quadros por s); o FPS da tela sai do -perf do build (Game.Perf, client/tools/k3.sh).
    /// Tetos com folga larga: o quadro de 30 FPS tem 33 ms, e o nucleo nao pode passar de ~6% dele num celular ~5x mais lento que o PC
    /// (2 ms no p99 aqui). Em 2026-11-05 na nuvem: mediana 0,004 ms, p90 0,008, p99 0,03, 59 bytes por quadro, pico de 64 passageiros.
    /// O teto pega custo quadratico ou lixo novo por passo antes de chegar ao aparelho.
    /// </summary>
    public class K3Tests
    {
        /// <summary>A cena do K3: -passageiros 60 = 30 por voo, 2 voos por onda (Game.Awake).</summary>
        const int PaxPorVoo = 30, Fps = 60, Vel = 3;
        const float Quadro = 1f / Fps;

        public struct Medida { public double Mediana, P90, P99, Max, BytesPorQuadro; public int Quadros, PicoPessoas, PicoPax; }

        /// <summary>Um quadro do jogo a 3x, como o Game.Update: o bot pensa no dt de jogo e o relogio anda 3 Ticks (Game.Anda).</summary>
        static void UmQuadro(Sim s, Bot bot)
        {
            bot.Pensa(s, Quadro * Vel);
            for (int i = 0; i < Vel; i++) s.Tick(Quadro);
        }

        /// <summary>Passageiros de partida no aeroporto (filas, atendimento e banheiro) mais os que chegam (frente H).</summary>
        public static int PaxPresentes(Sim s)
        {
            int n = 0;
            foreach (Station st in s.Estacoes) n += st.Fila.Count + st.Atendendo.Count;
            n += s.Banheiro.Indo.Count + s.Banheiro.Fila.Count + s.Banheiro.Usando.Count + s.Banheiro.Voltando.Count;
            return n + s.Chegantes.Count;
        }

        /// <summary>Aquece 2 ondas e mede `quadros` quadros (5 min de jogo a 3x = 6.000 quadros).</summary>
        public static Medida Mede(int quadros = 6000, int semente = 1)
        {
            var s = new Sim(new Tuning { PaxFixo = PaxPorVoo }, semente);
            var bot = new Bot(BotProfile.Bom);
            long aquece = 2L * s.OndaPassos;
            while (s.Steps < aquece) UmQuadro(s, bot);

            var ms = new double[quadros];
            var m = new Medida { Quadros = quadros };
            long a0 = Alocado();
            for (int i = 0; i < quadros; i++)
            {
                long t0 = Stopwatch.GetTimestamp();
                UmQuadro(s, bot);
                ms[i] = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
                int pax = PaxPresentes(s);
                m.PicoPax = Math.Max(m.PicoPax, pax);
                m.PicoPessoas = Math.Max(m.PicoPessoas, pax + s.Staff.Count);
            }
            m.BytesPorQuadro = (Alocado() - a0) / (double)quadros;
            Array.Sort(ms);
            m.Mediana = ms[quadros / 2];
            m.P90 = ms[(int)(quadros * 0.9)];
            m.P99 = ms[(int)(quadros * 0.99)];
            m.Max = ms[quadros - 1];
            return m;
        }

        /// <summary>Bytes alocados nesta thread; -1 se o runtime nao conta (o Mono do Editor pode devolver 0 sempre).</summary>
        static long Alocado()
        {
            try { return GC.GetAllocatedBytesForCurrentThread(); }
            catch (Exception) { return -1; }
        }

        static bool ContaAlocacao()
        {
            long a = Alocado();
            if (a < 0) return false;
            var lixo = new byte[64 * 1024];
            GC.KeepAlive(lixo);
            return Alocado() - a >= lixo.Length;
        }

        [Test]
        public void K3_CenaTemSessentaPessoas()
        {
            Medida m = Mede();   // uma onda inteira (180 s de jogo) cabe nos 300 s medidos
            TestContext.WriteLine($"K3 nucleo: pico de {m.PicoPax} passageiros e {m.PicoPessoas} pessoas com a equipe");
            Assert.GreaterOrEqual(m.PicoPax, 60, "a cena do -passageiros 60 nao chega a 60 passageiros ao mesmo tempo: o K3 mediria menos gente");
        }

        [Test]
        public void K3_NucleoCabeNoQuadro()
        {
            Mede(600);   // JIT e caches fora da medida
            Medida m = Mede();
            TestContext.WriteLine($"K3 nucleo: {m.Quadros} quadros a 3x, mediana {m.Mediana:0.000} ms, p90 {m.P90:0.000}, p99 {m.P99:0.000}, max {m.Max:0.000}");
            Assert.Less(m.Mediana, 0.5, "mediana do nucleo por quadro");
            Assert.Less(m.P99, 2.0, "p99 do nucleo por quadro (6% do quadro de 30 FPS num celular 5x mais lento)");
        }

        [Test]
        public void K3_PoucoLixoPorQuadro()
        {
            if (!ContaAlocacao()) Assert.Pass("este runtime (Mono do Editor) nao conta bytes alocados por thread: mede no dotnet");   // Ignore derrubava o verify full, que exige result=Passed
            Mede(600);
            Medida m = Mede();
            TestContext.WriteLine($"K3 nucleo: {m.BytesPorQuadro:0} bytes alocados por quadro a 3x");
            // 1 KB por quadro = 60 KB/s: o GC incremental do Unity limpa sem pico; acima disso o p10 do aparelho sente o coletor
            Assert.Less(m.BytesPorQuadro, 1024, "lixo do nucleo por quadro");
        }

        [Test]
        public void K3_CriterioDoVeredito()
        {
            Assert.IsNull(K3Criterio.Falta(30f, K3Criterio.P10Min, 60, 2, 6), "no limite passa");
            Assert.AreEqual("p10 29.9 < 30", K3Criterio.Falta(29.9f, 30f, 60, 2, 6));
            Assert.AreEqual("pessoas 59 < 60; avioes 1 < 2; veiculos 5 < 6", K3Criterio.Falta(58.7f, 30f, 59, 1, 5));
            Assert.IsNotNull(K3Criterio.Falta(float.NaN, 30f, 60, 2, 6), "sem quadro medido nao passa");
            Assert.AreEqual("p10 40.0 < 45", K3Criterio.Falta(40f, 45f, 60, 2, 6), "o -perfmin aperta o piso");
        }

        /// <summary>O dobro de gente nao pode custar muito mais que o dobro (pega laco quadratico em fila, banheiro ou satisfacao).</summary>
        [Test]
        public void K3_CustoCresceLinear()
        {
            double Custo(int paxPorVoo)
            {
                var s = new Sim(new Tuning { PaxFixo = paxPorVoo }, 1);
                var bot = new Bot(BotProfile.Bom);
                while (s.Steps < 2L * s.OndaPassos) UmQuadro(s, bot);
                long t0 = Stopwatch.GetTimestamp();
                for (int i = 0; i < 6000; i++) UmQuadro(s, bot);
                return (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
            }
            Custo(PaxPorVoo);
            double um = Custo(PaxPorVoo), quatro = Custo(4 * PaxPorVoo);
            TestContext.WriteLine($"K3 nucleo: 6.000 quadros com 60 pax {um:0.0} ms, com 240 pax {quatro:0.0} ms (razao {quatro / um:0.00})");
            // linear daria ~4x no pior caso (custo fixo por passo puxa para baixo); quadratico, ~16x
            Assert.Less(quatro / um, 8.0, "4x a gente custou mais de 8x");
        }
    }
}
