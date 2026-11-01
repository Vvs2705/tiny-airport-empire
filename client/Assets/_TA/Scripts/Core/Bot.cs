using System;
using System.Linq;

namespace TA.Core
{
    /// <summary>
    /// Perfil do bot (dados, nao subclasse, como o BotProfile do Forge; GDD §6.2, CONTRATO_V01 §7). Decide a cada `Decide` s de jogo:
    /// Bom = 1,0 s; Lento (x0,4) = 2,5 s; Parado = o Bom que nunca move funcionario (a base do TA-2). Nenhum assiste anuncio.
    /// </summary>
    public struct BotProfile
    {
        /// <summary>Planeja = escala, checagem e hangar (CONTRATO_FASE_C §8); Equipe = especialista em casa e gente na oficina quando um aviao
        /// esta pendente (CONTRATO_FASE_C2 §5). Sem eles, tudo fica no padrao do nucleo. Rotas = abre e programa rotas; Viaja = vai ao proximo
        /// aeroporto; Incidentes = reage ao aviso (CONTRATO_FASE_C3 §7).</summary>
        public string Name; public float Decide; public bool Move, Planeja, Equipe, Rotas, Viaja, Incidentes;
        /// <summary>Clima = reage ao aviso de clima; Falhas = paga o reparo da pane (CONTRATO_FASE_C4 §8). Sem eles, segura e espera a inspecao.</summary>
        public bool Clima, Falhas;
        /// <summary>Segue = troca para o aeroporto com a melhor estacao do ano (CONTRATO_FASE_C4 §8).</summary>
        public bool Segue;
        /// <summary>Objetivos = joga pelo objetivo ativo da campanha (CONTRATO_FASE_C5 §6). Sem ele o bot nunca olha a campanha.</summary>
        public bool Objetivos;
        /// <summary>Compra = produtos creditados no 1o passo (o bot faz o papel do servidor). Gasta = gasta as Gems: booster sempre, pacote de Cash com pouco Cash (CONTRATO_FASE_D1 §7).</summary>
        public string[] Compra; public bool Gasta;

        public static readonly BotProfile Bom = new BotProfile { Name = "Bom", Decide = 1f, Move = true, Planeja = true, Equipe = true, Rotas = true, Viaja = true, Incidentes = true, Clima = true, Falhas = true };
        public static readonly BotProfile Lento = new BotProfile { Name = "Lento", Decide = 2.5f, Move = true, Planeja = true, Equipe = true, Rotas = true, Viaja = true, Incidentes = true, Clima = true, Falhas = true };
        public static readonly BotProfile Parado = new BotProfile { Name = "Parado", Decide = 1f, Move = false, Planeja = true, Rotas = true, Viaja = true, Incidentes = true, Clima = true, Falhas = true };
        /// <summary>Portao de design do GDD §5.13 (Fase C): o Bom que planeja contra o Bom que deixa tudo no automatico.</summary>
        public static readonly BotProfile Planejador = new BotProfile { Name = "Planejador", Decide = 1f, Move = true, Planeja = true, Equipe = true };
        public static readonly BotProfile Automatico = new BotProfile { Name = "Automatico", Decide = 1f, Move = true, Planeja = false, Equipe = true };
        /// <summary>Portao da Fase C2: o mesmo Bom com e sem o planejamento da equipe (as 4 estacoes novas e os perfis).</summary>
        public static readonly BotProfile EquipePlanejada = new BotProfile { Name = "EquipePlanejada", Decide = 1f, Move = true, Planeja = true, Equipe = true };
        public static readonly BotProfile EquipeAutomatica = new BotProfile { Name = "EquipeAutomatica", Decide = 1f, Move = true, Planeja = true };
        /// <summary>Portao da Fase C5: o Bom e o Lento que jogam pela campanha.</summary>
        public static readonly BotProfile Guiado = new BotProfile { Name = "Guiado", Decide = 1f, Move = true, Planeja = true, Equipe = true, Rotas = true, Viaja = true, Incidentes = true, Clima = true, Falhas = true, Objetivos = true };
        public static readonly BotProfile LentoGuiado = new BotProfile { Name = "LentoGuiado", Decide = 2.5f, Move = true, Planeja = true, Equipe = true, Rotas = true, Viaja = true, Incidentes = true, Clima = true, Falhas = true, Objetivos = true };
        /// <summary>Portao da Fase D1: o Guiado que gasta o que ganhou, o que compra uma vez e o que o dinheiro nunca falta.</summary>
        public static readonly BotProfile GastaGanho = Comprador("GastaGanho");
        public static readonly BotProfile BoasVindas = Comprador("BoasVindas", "welcome_bundle_01");
        public static readonly BotProfile PacoteGrande = Comprador("PacoteGrande", "gems_pack_large");
        public static readonly BotProfile Baleia = Comprador("Baleia", Enumerable.Repeat("gems_pack_large", 200).ToArray());
        public static readonly BotProfile[] All = { Bom, Lento, Parado };

        static BotProfile Comprador(string nome, params string[] compra)
        {
            BotProfile p = Guiado;
            p.Name = nome; p.Compra = compra; p.Gasta = true;
            return p;
        }
    }

    /// <summary>
    /// Joga sozinho, sem render, pelas mesmas intencoes da View (Tap, Move, Buy, Hire). Politica do CONTRATO_V01 §7, uma decisao por vez:
    /// 1) se uma estacao esta critica e outra tem folga (normal), move o funcionario da estacao com mais folga para a critica, se la houver
    ///    posto livre para ele (mover para estacao sem posto livre nao ajuda: ele ficaria parado);
    /// 2) senao, toca a estacao sem funcionario que tem fila (a de menor folga);
    /// 3) e compra a contratacao ou o upgrade mais barato que couber (upgrade da loja so com a loja aberta: loja fechada nao vende), e a
    ///    proxima pessoa de patio depois que o patio atrasou um embarque (CONTRATO_FASE_C §8).
    /// Com Planeja, tambem (Planeja): escala o tripulante mais descansado, checa o aviao quando cabe antes do embarque e manda o pendente
    /// ao hangar no lugar da inspecao. Com Equipe, cuida das estacoes novas e dos perfis (Bot.Equipe). Nao e' IA de jogo: e' um jogador previsivel para medir o TA-1, o TA-2 e o portao do §5.13.
    /// </summary>
    public sealed class Bot
    {
        public BotProfile P;
        public int Moves, Taps, Buys, Trocas, Checagens, Hangares, Reacoes, Reparos;
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
            bool moveu = P.Move && Mover(s);
            if (!moveu && Tocar(s)) Taps++;
            bool junta = P.Objetivos && Guia(s);
            if (!junta && Comprar(s)) Buys++;
            if (P.Planeja) Planeja(s);
            if (!moveu && P.Move && P.Equipe) Equipe(s);
            if (P.Rotas && !_rotaFixa) Programa(s);
            if (P.Incidentes) Reage(s);
            if (P.Clima && s.T.RegraClima) ReageClima(s);
            if (P.Segue) SegueEpoca(s);
            if (P.Falhas) foreach (Aviao a in s.Avioes) if (a.Pane && s.ReparaAviao(a.Id)) Reparos++;
            if (P.Gasta) Gasta(s);
        }

        bool _rotaFixa;
        int _comprou;

        /// <summary>Fase D1: o maximo que a compra acelera. Cash nunca falta enquanto houver Gems, e o 2x fica sempre ligado.</summary>
        void Gasta(Sim s)
        {
            const long Folga = 5000;
            for (; _comprou < P.Compra.Length; _comprou++) s.Creditar("bot:" + _comprou, P.Compra[_comprou]);
            for (int i = 0; i < s.T.ItensGems.Length; i++)
                if (s.T.ItensGems[i].Tipo == TipoItem.Booster || s.T.ItensGems[i].Tipo == TipoItem.PacoteCash && s.Cash < Folga) s.Gastar(i);
        }

        /// <summary>
        /// Fase C5: faz o que o objetivo ativo pede; cadeia fechada, vai ao proximo aeroporto com cadeia aberta. O que o bot de sempre ja faz
        /// (embarcar, mover, checar, reagir) nao pede nada aqui. true = esta juntando Cash para o objetivo: nada de outra compra no caminho.
        /// </summary>
        bool Guia(Sim s)
        {
            _rotaFixa = false;
            Objetivo o = s.ObjetivoAtual(s.AeroportoAtual);
            if (o == null)
            {
                for (int a = 0; a < s.T.Objetivos.Length; a++)
                {
                    int destino = a;
                    if (s.ObjetivoAtual(a) != null) return Junta(s, s.CustoViagem(a), () => s.Viajar(destino));
                }
                return false;
            }
            switch (o.Meta)
            {
                case Meta.Equipe: return Junta(s, s.CustoContratar(), s.Hire);
                case Meta.Velocidade: return Junta(s, s.Custo((Estacao)o.Arg, Melhoria.Velocidade), () => s.Buy((Estacao)o.Arg, Melhoria.Velocidade));
                case Meta.Capacidade: return Junta(s, s.Custo((Estacao)o.Arg, Melhoria.Capacidade), () => s.Buy((Estacao)o.Arg, Melhoria.Capacidade));
                case Meta.Cheios:
                case Meta.Voos:
                    if (!s.RotaAberta(o.Arg)) return Junta(s, s.CustoRota(o.Arg), () => s.AbreRota(o.Arg));
                    Fixa(s, o.Arg);
                    return false;
                case Meta.Seguidos:
                    Fixa(s, RotaLeve(s));
                    return false;
                case Meta.Atendidos:
                    PoeGente(s, (Estacao)o.Arg);
                    return false;
                case Meta.Hangar:
                    foreach (Aviao a in s.Avioes)
                        if (a.Desgaste > 0 && s.PodeHangar(a.Id) && s.Hangar(a.Id)) { Hangares++; break; }
                    return false;
                default: return false;
            }
        }

        static bool Junta(Sim s, long custo, Func<bool> compra) => custo >= 0 && (s.Cash < custo || !compra());

        void Fixa(Sim s, int rota)
        {
            _rotaFixa = true;
            for (int f = 0; f < 2; f++) if (s.Prog[f] != rota) s.Programa(f, rota);
        }

        /// <summary>A rota aberta do aeroporto com menos passageiros: a que a equipe aguenta sem perder ninguem.</summary>
        static int RotaLeve(Sim s)
        {
            int leve = s.Prog[0];
            for (int r = 0; r < s.T.Rotas.Length; r++)
                if (s.T.Rotas[r].Aeroporto == s.AeroportoAtual && s.RotaAberta(r) && s.T.Rotas[r].Pax < s.T.Rotas[leve].Pax) leve = r;
            return leve;
        }

        /// <summary>Estacao sem ninguem: vai quem esta com mais folga.</summary>
        void PoeGente(Sim s, Estacao e)
        {
            if (s.Alocados(e) > 0) return;
            Funcionario quem = null;
            foreach (Funcionario f in s.Staff)
                if (f.Andando == 0 && (quem == null || s.St(f.Em).Folga > s.St(quem.Em).Folga)) quem = f;
            if (quem != null && s.Move(quem.Id, e)) Moves++;
        }

        /// <summary>Fase C4: adianta no aviso; quem perdeu o aviso ainda pode atrasar o portao.</summary>
        void ReageClima(Sim s)
        {
            Incidente i = s.Inc;
            if (i != null && !i.Resolvido && s.ClimaDe(i.Tipo) != null && (s.Adianta() || s.Atrasa())) Reacoes++;
        }

        /// <summary>Fase C4: na virada da estacao do ano, vai ao aeroporto ja pago em que a receita da rota mais rica x demanda fica melhor.</summary>
        void SegueEpoca(Sim s)
        {
            int w = s.Onda + 1, melhor = s.AeroportoAtual;
            if (s.ProximoAeroporto >= 0) return;
            double Nota(int a)
            {
                double r = 0;
                for (int i = 0; i < s.T.Rotas.Length; i++) if (s.T.Rotas[i].Aeroporto == a && s.RotaAberta(i)) r = Math.Max(r, s.T.Rotas[i].Receita);
                return r * s.DemandaDe(a, w);
            }
            double nota = Nota(melhor) * 1.05;
            for (int a = 0; a < s.T.Aeroportos.Length; a++)
                if (a != melhor && s.CustoViagem(a) == 0 && Nota(a) > nota) { melhor = a; nota = Nota(a); }
            if (melhor != s.AeroportoAtual) s.Viajar(melhor);
        }

        readonly int[] _vooVisto = { -1, -1 };
        double _embarque = 1;

        /// <summary>Fase C3: depois de cada voo, a vaga sobe para a rota aberta de receita maior se ele saiu cheio (>= Sobe) e desce se perdeu
        /// gente demais (&lt; Desce). Assim a rota acompanha o que a equipe da conta.</summary>
        void Programa(Sim s)
        {
            const double Sobe = 0.95, Desce = 0.85;
            for (int f = 0; f < 2; f++)
            {
                Voo v = s.Voos[f];
                if (v == null || !v.Partiu || v.Total == 0 || v.Id == _vooVisto[f]) continue;
                _vooVisto[f] = v.Id;
                _embarque = v.Embarcados / (double)v.Total;
                float atual = s.T.Rotas[s.Prog[f]].Receita;
                int alvo = -1;
                for (int r = 0; r < s.T.Rotas.Length; r++)
                {
                    float x = s.T.Rotas[r].Receita;
                    if (!s.RotaAberta(r) || s.T.Rotas[r].Aeroporto != s.AeroportoAtual) continue;
                    if (_embarque >= Sobe && x > atual && (alvo < 0 || x < s.T.Rotas[alvo].Receita)) alvo = r;
                    if (_embarque < Desce && x < atual && (alvo < 0 || x > s.T.Rotas[alvo].Receita)) alvo = r;
                }
                if (alvo >= 0) s.Programa(f, alvo);
            }
        }

        /// <summary>Fase C3: a saida de cada incidente assim que o aviso aparece. No pico, leva gente ao check-in antes do grupo chegar.</summary>
        void Reage(Sim s)
        {
            Incidente i = s.Inc;
            if (i == null) return;
            if (i.Tipo == TipoIncidente.FrenteFria && !s.T.RegraClima && s.Adianta() || i.Tipo == TipoIncidente.Falha && s.Repara() || i.Tipo == TipoIncidente.Conexao && s.Prioriza()) { Reacoes++; return; }
            if (i.Tipo != TipoIncidente.Pico || i.Inicio == _picoVisto || !P.Move || s.Alocados(Estacao.CheckIn) >= s.St(Estacao.CheckIn).Postos) return;
            _picoVisto = i.Inicio;   // uma pessoa por pico: a Equipe a leva de volta depois dele
            foreach (Funcionario f in s.Staff)
                if (f.Andando == 0 && !Fluxo(f.Em) && s.St(f.Em).Estado == Estado.Normal && s.Move(f.Id, Estacao.CheckIn)) { Moves++; Reacoes++; return; }
        }

        long _picoVisto = -1;

        /// <summary>
        /// Fase C2: 1) aviao pendente (no ar ou no patio) e ninguem na oficina: leva o mecanico (ou quem sobra numa estacao normal) para ela
        /// chegar antes do pouso; 2) senao, quem esta fora da especialidade numa estacao normal volta para a estacao do perfil se la cabe
        /// (os 3 da v0.1 so depois da especializacao; o mecanico so vai quando a oficina precisa). Uma mudanca por decisao.
        /// </summary>
        void Equipe(Sim s)
        {
            if (s.Inc?.Tipo == TipoIncidente.Pico) return;   // quem foi reforcar o check-in fica ate o pico passar
            if (OficinaPrecisa(s) && s.Alocados(Estacao.Manutencao) == 0)
            {
                Funcionario quem = null;
                foreach (Funcionario f in s.Staff)
                {
                    if (f.Andando > 0 || f.Em == Estacao.Manutencao || s.St(f.Em).Estado == Estado.Critico) continue;
                    bool mec = f.Perfil == Perfil.Mecanico;
                    if (quem == null || mec || quem.Perfil != Perfil.Mecanico && s.St(f.Em).Estado == Estado.Normal && s.St(f.Em).Folga > s.St(quem.Em).Folga) quem = f;
                }
                if (quem != null && s.Move(quem.Id, Estacao.Manutencao)) Moves++;
                return;
            }
            foreach (Funcionario f in s.Staff)
            {
                Estacao casa = Funcionario.Casa(f.Perfil);
                // Ana e Beto so voltam pela especializacao; o Caio vai a alimentacao (estacao nova) quando ela abre
                if (f.Andando > 0 || f.Perfil == Perfil.Mecanico || !s.Especializa && (int)casa < 4) continue;
                if (!s.Ativa(casa) || Funcionario.Especialidade(f.Perfil, f.Em) || s.St(f.Em).Estado != Estado.Normal || s.Alocados(casa) >= s.St(casa).Postos) continue;
                if (f.Em == Estacao.Manutencao && OficinaPrecisa(s)) continue;
                if (s.Move(f.Id, casa)) { Moves++; return; }
            }
        }

        static bool OficinaPrecisa(Sim s)
        {
            if (!s.Ativa(Estacao.Manutencao)) return false;
            foreach (Aviao a in s.Avioes) if (a.Pendente && a.Hangar == 0 && (!a.NoPatio || a.Check > 0)) return true;
            return false;
        }

        static bool Fluxo(Estacao e) => e == Estacao.CheckIn || e == Estacao.Seguranca || e == Estacao.Portao;

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
                // com gente na oficina a inspecao ja salva o voo (Fase C2): o hangar so quando o mecanico esta sozinho
                if (a.Pendente) { if (!(s.Ativa(Estacao.Manutencao) && s.Forca(Estacao.Manutencao) > 0) && s.Hangar(a.Id)) Hangares++; continue; }
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
            foreach (Station st in s.Estacoes)
                if (Fluxo(st.Tipo) && st.Estado == Estado.Critico && s.Alocados(st.Tipo) < st.Postos && (alvo == null || st.Folga < alvo.Folga)) alvo = st;
            if (alvo == null) return false;
            // com Equipe: prefere o especialista da estacao critica e nao tira ninguem da oficina enquanto um aviao precisa dela
            // e so tira alguem de uma estacao nova (a casa dele) se nao houver outro
            Funcionario quem = null; double folga = 0; int quemNota = 0;
            bool oficina = P.Equipe && OficinaPrecisa(s);
            foreach (Funcionario f in s.Staff)
            {
                Station de = s.St(f.Em);
                if (f.Andando > 0 || de == alvo || de.Estado != Estado.Normal || oficina && f.Em == Estacao.Manutencao) continue;
                bool esp = P.Equipe && s.Especializa && Funcionario.Especialidade(f.Perfil, alvo.Tipo);
                bool fixo = P.Equipe && (int)f.Em >= 4 && f.Em == Funcionario.Casa(f.Perfil);
                int nota = (fixo ? 0 : 2) + (esp ? 1 : 0);
                if (quem == null || nota > quemNota || nota == quemNota && de.Folga > folga) { quem = f; folga = de.Folga; quemNota = nota; }
            }
            if (quem == null || !s.Move(quem.Id, alvo.Tipo)) return false;
            Moves++;
            return true;
        }

        static bool Tocar(Sim s)
        {
            Station alvo = null;
            foreach (Station st in s.Estacoes)
                if (s.PodeTocar(st.Tipo) && (alvo == null || st.Folga < alvo.Folga)) alvo = st;
            return alvo != null && s.Tap(alvo.Tipo);
        }

        int _patioVisto;

        bool Comprar(Sim s)
        {
            // hire: -2 = o proximo da v0.1 (Hire()), >= 0 = o perfil novo, -1 = upgrade
            long best = s.CustoContratar(); Estacao be = 0; Melhoria bm = 0; int hire = best >= 0 ? -2 : -1;
            for (int p = Sim.MaxFuncionarios; p < Sim.MaxEquipe; p++)
            {
                long c = s.CustoContratar((Perfil)p);
                if (c >= 0 && (best < 0 || c < best)) { best = c; hire = p; }
            }
            foreach (Station st in s.Estacoes)
            {
                if (!Fluxo(st.Tipo) && s.Alocados(st.Tipo) == 0) continue;   // estacao sem ninguem nao rende: nao melhora
                foreach (Melhoria m in new[] { Melhoria.Velocidade, Melhoria.Capacidade })
                {
                    long c = s.Custo(st.Tipo, m);
                    if (c >= 0 && (best < 0 || c < best)) { best = c; be = st.Tipo; bm = m; hire = -1; }
                }
            }
            if (s.AtrasosPatio > _patioVisto && s.CustoPatio() >= 0 && (best < 0 || s.CustoPatio() <= best))
            {
                if (!s.HirePatio()) return false;
                _patioVisto = s.AtrasosPatio;
                return true;
            }
            // Fase C3: a rota do aeroporto atual e a viagem entram na conta do mais barato (hire = -3 rota, -4 viagem)
            int rota = -1;
            if (P.Rotas)
                for (int r = 0; r < s.T.Rotas.Length; r++)
                {
                    long c = s.T.Rotas[r].Aeroporto == s.AeroportoAtual ? s.CustoRota(r) : -1;
                    if (c >= 0 && (best < 0 || c < best)) { best = c; rota = r; hire = -3; }
                }
            long v = s.CustoViagem(s.AeroportoAtual + 1);
            if (P.Viaja && !P.Objetivos && _embarque >= ViajaCom && v >= 0 && (best < 0 || v < best)) { best = v; hire = -4; }
            if (best < 0 || s.Cash < best) return false;
            return hire == -2 ? s.Hire() : hire == -3 ? s.AbreRota(rota) : hire == -4 ? s.Viajar(s.AeroportoAtual + 1) : hire >= 0 ? s.Hire((Perfil)hire) : s.Buy(be, bm);
        }

        /// <summary>So viaja quem esta dando conta: embarque do ultimo voo.</summary>
        const double ViajaCom = 0.9;

        // ------------------------------------------------------------------ medicao (BalanceTests e -autoplay)

        public struct Resultado
        {
            /// <summary>Embarque medio por voo (embarcados/total, media dos voos), receita total (Cash ganho, sem gastos) e o s de jogo em que
            /// cada marco do terminal chegou (-1 = nao chegou).</summary>
            public double Embarque; public long Receita; public float[] MarcoEm; public int Moves, Taps, Voos, SemVoo;
            /// <summary>Fase C: voos sem tripulacao na hora (aviso), inspecoes obrigatorias, idas ao hangar, checagens rapidas e trocas de escala.</summary>
            public int SemTrip, Inspecoes, Hangares, Checagens, Trocas;
            /// <summary>Fase C3: incidentes, reacoes do bot, viagens e onda em que chegou a cada aeroporto (-1 = nao chegou).</summary>
            public int Incidentes, Reacoes, Viagens; public int[] ChegouEm;
            /// <summary>Fase C4: climas, falhas de aviao e reparos pagos.</summary>
            public int Climas, Falhas, Reparos;
            /// <summary>Fase C5: objetivos feitos, Cash pago por eles e a onda em que cada um fechou (aeroporto x 10 + indice; -1 = nao fechou).</summary>
            public int Objetivos; public long Recompensas; public int[] ObjetivoEm;
        }

        /// <summary>Joga `ondas` ondas inteiras com o perfil `p` e a semente `seed`, sem render. `reabre` = fecha e reabre o jogo (Save ->
        /// Load) no meio de cada onda: o resultado tem de ser o mesmo de sem fechar (retomada do -autoplay). `ateFechar` = para quando a campanha fecha.</summary>
        public static Resultado Medir(BotProfile p, int seed, int ondas, Tuning t = null, bool reabre = false, bool ateFechar = false)
        {
            var s = new Sim(t, seed);
            var bot = new Bot(p);
            var r = new Resultado { MarcoEm = new[] { -1f, -1f, -1f }, ChegouEm = new[] { 0, -1, -1 }, ObjetivoEm = new int[30] };
            for (int i = 0; i < 30; i++) r.ObjetivoEm[i] = -1;
            double soma = 0;
            long passos = (long)ondas * s.OndaPassos;
            while (s.Steps < passos && !(ateFechar && s.CampanhaCompleta))
            {
                if (reabre && s.Steps % s.OndaPassos == s.OndaPassos / 2) s = Sim.Load(s.Save("autoplay", 1000 + s.Steps), t, seed);
                bot.Step(s);
                foreach (Evento e in s.Events)
                    switch (e.Tipo)
                    {
                        case Ev.VooPartiu: if (e.C > 0) { soma += e.B / (double)e.C; r.Voos++; } else r.SemVoo++; break;
                        case Ev.Embarcou: case Ev.Venda: r.Receita += e.B; break;
                        case Ev.Bonus: r.Receita += e.B; break;
                        case Ev.Marco: if (e.A >= 1 && e.A <= 3 && r.MarcoEm[e.A - 1] < 0) r.MarcoEm[e.A - 1] = s.Time; break;
                        case Ev.SemTripulacao: r.SemTrip++; break;
                        case Ev.ChecagemIniciada: if (e.C == 1) r.Inspecoes++; else r.Checagens++; break;
                        case Ev.HangarIniciado: r.Hangares++; break;
                        case Ev.Escalado: if (e.C == 1) r.Trocas++; break;
                        case Ev.IncidenteInicio: r.Incidentes++; if (s.ClimaDe((TipoIncidente)e.A) != null) r.Climas++; break;
                        case Ev.Pane: case Ev.Rejeicao: case Ev.RetornoPedido: r.Falhas++; break;
                        case Ev.AeroportoChegada: r.Viagens++; if (r.ChegouEm[e.A] < 0) r.ChegouEm[e.A] = s.Onda; break;
                        case Ev.ObjetivoConcluido: r.Objetivos++; r.Recompensas += e.C; r.ObjetivoEm[e.A * 10 + e.B] = s.Onda; break;
                    }
            }
            r.Embarque = r.Voos > 0 ? soma / r.Voos : 0;
            r.Moves = bot.Moves; r.Taps = bot.Taps; r.Reacoes = bot.Reacoes; r.Reparos = bot.Reparos;
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
            ok = igual && bom.Voos + bom.SemVoo == 2 * ondas && bom.Embarque > 0;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            string M(float x) => x < 0 ? "-" : (x / 60f).ToString("0.0", ci) + "min";
            return string.Format(ci, "AUTOPLAY {0} ondas={1} embarque_bom={2:0.0}% embarque_lento={3:0.0}% embarque_parado={4:0.0}% ganho_mover={5:0.000} " +
                "receita_bom={6} moves_bom={7} marcos_bom={8},{9},{10} retomada={11}", ok ? "OK" : "FAIL", ondas, bom.Embarque * 100, lento.Embarque * 100,
                parado.Embarque * 100, parado.Receita > 0 ? bom.Receita / (double)parado.Receita : 0, bom.Receita, bom.Moves,
                M(bom.MarcoEm[0]), M(bom.MarcoEm[1]), M(bom.MarcoEm[2]), igual ? "igual" : "DIFERENTE");
        }
    }
}
