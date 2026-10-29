using System;

namespace TA.Core
{
    /// <summary>
    /// Perfil do bot (dados, nao subclasse, como o BotProfile do Forge; GDD §6.2, CONTRATO_V01 §7). Decide a cada `Decide` s de jogo:
    /// Bom = 1,0 s; Lento (x0,4) = 2,5 s; Parado = o Bom que nunca move funcionario (a base do TA-2). Nenhum assiste anuncio.
    /// </summary>
    public struct BotProfile
    {
        /// <summary>Planeja = escala, checagem e hangar (CONTRATO_FASE_C §8); sem ele, tudo fica no padrao do nucleo.</summary>
        public string Name; public float Decide; public bool Move, Planeja;

        public static readonly BotProfile Bom = new BotProfile { Name = "Bom", Decide = 1f, Move = true, Planeja = true };
        public static readonly BotProfile Lento = new BotProfile { Name = "Lento", Decide = 2.5f, Move = true, Planeja = true };
        public static readonly BotProfile Parado = new BotProfile { Name = "Parado", Decide = 1f, Move = false, Planeja = true };
        /// <summary>Portao de design do GDD §5.13: o Bom que planeja contra o Bom que deixa tudo no automatico.</summary>
        public static readonly BotProfile Planejador = new BotProfile { Name = "Planejador", Decide = 1f, Move = true, Planeja = true };
        public static readonly BotProfile Automatico = new BotProfile { Name = "Automatico", Decide = 1f, Move = true, Planeja = false };
        public static readonly BotProfile[] All = { Bom, Lento, Parado };
    }

    /// <summary>
    /// Joga sozinho, sem render, pelas mesmas intencoes da View (Tap, Move, Buy, Hire). Politica do CONTRATO_V01 §7, uma decisao por vez:
    /// 1) se uma estacao esta critica e outra tem folga (normal), move o funcionario da estacao com mais folga para a critica, se la houver
    ///    posto livre para ele (mover para estacao sem posto livre nao ajuda: ele ficaria parado);
    /// 2) senao, toca a estacao sem funcionario que tem fila (a de menor folga);
    /// 3) e compra a contratacao ou o upgrade mais barato que couber (upgrade da loja so com a loja aberta: loja fechada nao vende), e a
    ///    proxima pessoa de patio depois que o patio atrasou um embarque (CONTRATO_FASE_C §8).
    /// Com Planeja, tambem (Planeja): escala o tripulante mais descansado, checa o aviao quando cabe antes do embarque e manda o pendente
    /// ao hangar no lugar da inspecao. Nao e' IA de jogo: e' um jogador previsivel para medir o TA-1, o TA-2 e o portao do §5.13.
    /// </summary>
    public sealed class Bot
    {
        public BotProfile P;
        public int Moves, Taps, Buys, Trocas, Checagens, Hangares;
        float _t;

        public Bot(BotProfile p) { P = p; }

        /// <summary>Se for a hora, uma decisao; depois um passo fixo do jogo (Events = a decisao e o passo, lidos uma vez).</summary>
        public void Step(Sim s)
        {
            Pensa(s, Sim.Step);
            s.Tick(Sim.Step);
        }

        /// <summary>Conta `dt` s de jogo e, a cada P.Decide, decide (mover ou tocar, e comprar). A View (-bot) chama antes do sim.Tick.</summary>
        public void Pensa(Sim s, float dt)
        {
            _t += dt;
            if (_t + 1e-4f < P.Decide) return;
            _t = Math.Min(_t - P.Decide, P.Decide);   // le o estado das estacoes como a tela mostra (atualizado a cada 0,5 s)
            if (!(P.Move && Mover(s)) && Tocar(s)) Taps++;
            if (Comprar(s)) Buys++;
            if (P.Planeja) Planeja(s);
        }

        /// <summary>
        /// Tripulacao: em cada voo no patio antes do embarque, a bordo fica quem tem menos trechos (descansado = 0); no empate, quem acabou de
        /// voltar (menos tempo na sala), para quem esta descansando terminar o descanso. Manutencao: pendente vai ao hangar (zera, em vez da
        /// inspecao que tira so metade e segura o embarque); senao, checagem rapida se o desgaste passou de CheckTira e ela termina antes de o
        /// embarque abrir.
        /// </summary>
        void Planeja(Sim s)
        {
            foreach (Aviao a in s.Avioes)
            {
                Voo v = s.VooDo(a);
                if (!a.NoPatio || v == null || v.Embarque || a.FechouEm >= 0) continue;
                if (a.Pendente) { if (s.Hangar(a.Id)) Hangares++; continue; }
                if (a.Desgaste >= s.T.CheckTira && (v.AbrePasso - s.PassoNaOnda) * Sim.Step >= s.T.CheckSeg && s.Checagem(a.Id)) Checagens++;
                foreach (Tripulante bordo in s.Tripulacao)
                {
                    if (bordo.Aviao != a.Id) continue;
                    Tripulante melhor = bordo;
                    foreach (Tripulante t in s.Tripulacao)
                        if (t.Funcao == bordo.Funcao && s.Livre(t) && Antes(s, t, melhor)) melhor = t;
                    if (melhor != bordo && s.Escala(v.Id, melhor.Id)) { Trocas++; break; }   // a lista mudou: o resto no proximo segundo
                }
            }
        }

        static int Efetivos(Sim s, Tripulante t) => t.Aviao < 0 && s.DescansoFalta(t) <= 0 ? 0 : t.Trechos;

        static bool Antes(Sim s, Tripulante a, Tripulante b)
        {
            int ea = Efetivos(s, a), eb = Efetivos(s, b);
            return ea != eb ? ea < eb : a.Sala < b.Sala;
        }

        bool Mover(Sim s)
        {
            Station alvo = null;
            foreach (Station st in s.Stations)
                if (st.Tipo != Estacao.Loja && st.Estado == Estado.Critico && s.Alocados(st.Tipo) < st.Postos && (alvo == null || st.Folga < alvo.Folga)) alvo = st;
            if (alvo == null) return false;
            Funcionario quem = null; double folga = 0;
            foreach (Funcionario f in s.Staff)
            {
                Station de = s.St(f.Em);
                if (f.Andando > 0 || de == alvo || de.Estado != Estado.Normal) continue;
                if (quem == null || de.Folga > folga) { quem = f; folga = de.Folga; }
            }
            if (quem == null || !s.Move(quem.Id, alvo.Tipo)) return false;
            Moves++;
            return true;
        }

        static bool Tocar(Sim s)
        {
            Station alvo = null;
            foreach (Station st in s.Stations)
                if (s.PodeTocar(st.Tipo) && (alvo == null || st.Folga < alvo.Folga)) alvo = st;
            return alvo != null && s.Tap(alvo.Tipo);
        }

        int _patioVisto;

        bool Comprar(Sim s)
        {
            long best = s.CustoContratar(); Estacao be = 0; Melhoria bm = 0; bool hire = best >= 0;
            foreach (Station st in s.Stations)
            {
                if (st.Tipo == Estacao.Loja && s.Alocados(Estacao.Loja) == 0) continue;
                foreach (Melhoria m in new[] { Melhoria.Velocidade, Melhoria.Capacidade })
                {
                    long c = s.Custo(st.Tipo, m);
                    if (c >= 0 && (best < 0 || c < best)) { best = c; be = st.Tipo; bm = m; hire = false; }
                }
            }
            if (s.AtrasosPatio > _patioVisto && s.CustoPatio() >= 0 && (best < 0 || s.CustoPatio() <= best))
            {
                if (!s.HirePatio()) return false;
                _patioVisto = s.AtrasosPatio;
                return true;
            }
            if (best < 0 || s.Cash < best) return false;
            return hire ? s.Hire() : s.Buy(be, bm);
        }

        // ------------------------------------------------------------------ medicao (BalanceTests e -autoplay)

        public struct Resultado
        {
            /// <summary>Embarque medio por voo (embarcados/total, media dos voos), receita total (Cash ganho, sem gastos) e o s de jogo em que
            /// cada marco do terminal chegou (-1 = nao chegou).</summary>
            public double Embarque; public long Receita; public float[] MarcoEm; public int Moves, Taps, Voos;
            /// <summary>Fase C: voos sem tripulacao na hora (aviso), inspecoes obrigatorias, idas ao hangar, checagens rapidas e trocas de escala.</summary>
            public int SemTrip, Inspecoes, Hangares, Checagens, Trocas;
        }

        /// <summary>Joga `ondas` ondas inteiras com o perfil `p` e a semente `seed`, sem render. `reabre` = fecha e reabre o jogo (Save ->
        /// Load) no meio de cada onda: o resultado tem de ser o mesmo de sem fechar (retomada do -autoplay).</summary>
        public static Resultado Medir(BotProfile p, int seed, int ondas, Tuning t = null, bool reabre = false)
        {
            var s = new Sim(t, seed);
            var bot = new Bot(p);
            var r = new Resultado { MarcoEm = new[] { -1f, -1f, -1f } };
            double soma = 0;
            long passos = (long)ondas * s.OndaPassos;
            while (s.Steps < passos)
            {
                if (reabre && s.Steps % s.OndaPassos == s.OndaPassos / 2) s = Sim.Load(s.Save("autoplay", 1000 + s.Steps), t, seed);
                bot.Step(s);
                foreach (Evento e in s.Events)
                    switch (e.Tipo)
                    {
                        case Ev.VooPartiu: if (e.C > 0) { soma += e.B / (double)e.C; r.Voos++; } break;
                        case Ev.Embarcou: case Ev.Venda: r.Receita += e.B; break;
                        case Ev.Bonus: r.Receita += e.B; break;
                        case Ev.Marco: if (e.A >= 1 && e.A <= 3 && r.MarcoEm[e.A - 1] < 0) r.MarcoEm[e.A - 1] = s.Time; break;
                        case Ev.SemTripulacao: r.SemTrip++; break;
                        case Ev.ChecagemIniciada: if (e.C == 1) r.Inspecoes++; else r.Checagens++; break;
                        case Ev.HangarIniciado: r.Hangares++; break;
                        case Ev.Escalado: if (e.C == 1) r.Trocas++; break;
                    }
            }
            r.Embarque = r.Voos > 0 ? soma / r.Voos : 0;
            r.Moves = bot.Moves; r.Taps = bot.Taps;
            return r;
        }

        /// <summary>
        /// -autoplay N (portao do build, CONTRATO_V01 §7): semente 1, N ondas com o Bom, o Lento e o Parado; o Bom de novo fechando e reabrindo
        /// no meio de cada onda (tem de dar igual). Linha com o embarque medio, o ganho de mover (Bom/Parado) e o tempo ate cada marco.
        /// ok = jogou todos os voos, embarcou alguem e a retomada deu igual. Os portoes do TA-1/TA-2 (200 sementes) ficam nos BalanceTests.
        /// </summary>
        public static string Autoplay(int ondas, out bool ok)
        {
            Resultado bom = Medir(BotProfile.Bom, 1, ondas), lento = Medir(BotProfile.Lento, 1, ondas), parado = Medir(BotProfile.Parado, 1, ondas);
            Resultado re = Medir(BotProfile.Bom, 1, ondas, null, true);
            bool igual = re.Receita == bom.Receita && re.Embarque == bom.Embarque && re.Voos == bom.Voos;
            ok = igual && bom.Voos == 2 * ondas && bom.Embarque > 0;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            string M(float x) => x < 0 ? "-" : (x / 60f).ToString("0.0", ci) + "min";
            return string.Format(ci, "AUTOPLAY {0} ondas={1} embarque_bom={2:0.0}% embarque_lento={3:0.0}% embarque_parado={4:0.0}% ganho_mover={5:0.000} " +
                "receita_bom={6} moves_bom={7} marcos_bom={8},{9},{10} retomada={11}", ok ? "OK" : "FAIL", ondas, bom.Embarque * 100, lento.Embarque * 100,
                parado.Embarque * 100, parado.Receita > 0 ? bom.Receita / (double)parado.Receita : 0, bom.Receita, bom.Moves,
                M(bom.MarcoEm[0]), M(bom.MarcoEm[1]), M(bom.MarcoEm[2]), igual ? "igual" : "DIFERENTE");
        }
    }
}
