using System;
using System.Collections.Generic;

// Tipos e numeros do nucleo (docs/CONTRATO_V01.md §3 e §4). Enum salvo por indice: so cresce no FIM; o nome de exibicao nunca e' chave.
namespace TA.Core
{
    /// <summary>As 4 estacoes da v0.1, na ordem do fluxo. Indice vai no save.</summary>
    public enum Estacao { CheckIn, Seguranca, Loja, Portao }

    /// <summary>Upgrades por estacao (CONTRATO_V01 §4).</summary>
    public enum Melhoria { Velocidade, Capacidade }

    /// <summary>Estado da estacao (GDD §5.6): critico = alguem da fila, no ritmo atual, nao embarca antes do fechamento; atencao = folga
    /// menor que Tuning.AtencaoFolga.</summary>
    public enum Estado { Normal, Atencao, Critico }

    /// <summary>Eventos de um Tick (a lista limpa a cada Tick). A View usa para som, efeito e o diario CSV (CONTRATO_V01 §6).</summary>
    public enum Ev
    {
        VooCriado,          // A = voo, B = passageiros, C = passo de fechamento (na onda)       -> flight_spawned
        EmbarqueAberto,     // A = voo                                                           -> flight_boarding_started
        VooPartiu,          // A = voo, B = embarcados, C = total                                -> flight_departed
        VooPerdido,         // A = voo, B = perdidos, C = total (so com perdidos > 0)            -> flight_missed
        Embarcou,           // A = passageiro, B = Cash recebido
        Venda,              // A = passageiro, B = Cash recebido (loja)
        Toque,              // A = estacao (toque manual que comecou um atendimento)
        Movido,             // A = funcionario, B = de, C = para                                 -> staff_moved
        Chegou,             // A = funcionario, B = estacao
        Contratado,         // A = funcionario, B = custo, C = estacao                           -> staff_hired
        Melhorou,           // A = estacao, B = melhoria, C = nivel novo                         -> station_upgraded
        Marco,              // A = marco do terminal (1..3)
        Historia,           // A = historia, B = passageiro, C = 1 se entrou agora no album      -> story_delivered
        Offline,            // A = Cash, B = s considerados                                      -> offline_claim
        Recompensa,         // A = anuncio, B = valor                                            -> reward_granted
        Fila,               // A = estacao, B = tamanho da fila, C = estado (a cada 10 s e na troca de estado) -> queue_length_sampled
        Bonus,              // A = voo, B = Cash do bonus de pontualidade
    }

    public struct Evento
    {
        public Ev Tipo; public int A, B, C;
        public override string ToString() => Tipo + "(" + A + "," + B + "," + C + ")";
    }

    /// <summary>Passageiro. Onda, voo e indice saem do Id (Sim.PaxId); Loja e Historia sao funcao do Id e da semente (refeitos no Load).</summary>
    public sealed class Pax
    {
        public int Id;
        /// <summary>Passos que faltam no atendimento atual (0 na fila).</summary>
        public int Resto;
        public bool Loja, Historia;
        public int Onda => Id / 1000;
        public int Voo => Id / 500 % 2;
        public int Indice => Id % 500;
    }

    public sealed class Station
    {
        public Estacao Tipo;
        /// <summary>Niveis de Velocidade (0..VelNiveis) e Capacidade (0..CapNiveis).</summary>
        public int Vel, Cap;
        /// <summary>Fila FIFO (o portao atende o 1o cujo voo ja abriu o embarque).</summary>
        public readonly List<Pax> Fila = new List<Pax>();
        /// <summary>Em atendimento, um por posto.</summary>
        public readonly List<Pax> Atendendo = new List<Pax>();
        /// <summary>Postos (balcoes) = os da planta (Tuning.PostosBase) + o upgrade de capacidade. Cada um atende 1 passageiro.</summary>
        public int Postos;
        /// <summary>Atualizados a cada 0,5 s de jogo (Sim.AtualizaEstados): estado, menor folga (s; +inf sem ninguem) e o voo em risco (-1).</summary>
        public Estado Estado; public double Folga = double.PositiveInfinity; public int VooEmRisco = -1;
        public int Pessoas => Fila.Count + Atendendo.Count;
    }

    public sealed class Funcionario
    {
        /// <summary>Indice na ordem de contratacao (vai no save e no diario).</summary>
        public int Id;
        /// <summary>Estacao onde esta ou para onde esta andando.</summary>
        public Estacao Em, De;
        /// <summary>Passos de caminhada que faltam (0 = presente e atendendo).</summary>
        public int Andando;
    }

    public sealed class Voo
    {
        /// <summary>Id = onda x 2 + indice (estavel, vai no diario).</summary>
        public int Id, Onda, Indice, Total, Criados, Embarcados;
        /// <summary>Passos dentro da onda em que abre o embarque e fecha o portao.</summary>
        public int AbrePasso, FechaPasso;
        /// <summary>Receita base das tarifas deste voo (sem o 2x do anuncio): base do bonus de pontualidade.</summary>
        public long Receita;
        public bool Embarque, Partiu;
        public int Perdidos => Partiu ? Total - Embarcados : 0;
    }

    /// <summary>
    /// Numeros da v0.1 (CONTRATO_V01 §4, todos hipotese; o bot mede e o docs/BALANCE.md registra). So o Tuning muda entre perfis e testes;
    /// as travas de seguranca sao const (OfflineTrava).
    /// </summary>
    public sealed class Tuning
    {
        public static readonly Tuning Padrao = new Tuning();

        /// <summary>Teto do cofre offline como trava do nucleo (GDD §6.3): o Tuning so encurta (OfflineCap).</summary>
        public const int OfflineTrava = 7200;

        public float Onda = 180f;
        public float[] Fecha = { 120f, 170f };
        public float AbreEmbarqueAntes = 60f;
        public int PaxInicial = 10, PaxPorOnda = 4, PaxMax = 36;
        /// <summary>Chegadas espalhadas de 0 ate (fechamento - ChegadaFolga).</summary>
        public float ChegadaFolga = 70f;
        /// <summary>s por passageiro no nivel 0, na ordem de Estacao.</summary>
        public float[] Tempo = { 4f, 5f, 3f, 1.5f };
        public float VelFator = 0.88f;
        public int VelNiveis = 5, VelCusto = 40; public float VelCresce = 1.6f;
        public int CapNiveis = 2, CapCusto = 300; public float CapCresce = 2.5f;
        /// <summary>Balcoes de cada estacao no nivel 0 de capacidade, na ordem de Estacao.</summary>
        public int[] PostosBase = { 2, 2, 1, 1 };
        public float LojaChance = 0.5f, LojaFolgaMin = 40f; public int LojaVenda = 6;
        public int Tarifa = 8, TarifaPorMarco = 2;
        public float Bonus = 0.25f;
        public int[] Contratacao = { 20, 150, 400 };
        /// <summary>Funcionarios no jogo novo (decisao da Fase B, BALANCE.md): 0 = comeca no toque manual.</summary>
        public int FuncionariosIniciais = 0;
        public float Caminhada = 2.5f;
        public int[] Marcos = { 150, 600, 1500 };
        public float AtencaoFolga = 20f;
        /// <summary>s da media movel da renda online (base do cofre offline).</summary>
        public float RendaTau = 180f;
        public float OfflineFator = 0.25f, OfflineTetoUpgrades = 2f;
        public int OfflineMaxSegundos = OfflineTrava;
        public long OfflineCap => Math.Min(OfflineMaxSegundos, OfflineTrava);
        /// <summary>Rewarded 2: 2x renda por BoostSegundos de jogo, BoostPorDia vezes por dia local (virada as 04:00).</summary>
        public int BoostSegundos = 300, BoostPorDia = 3;
        /// <summary>Estresse (-passageiros N): passageiros por voo fixos, acima do PaxMax. 0 = desligado.</summary>
        public int PaxFixo;

        public int Pax(int onda) => PaxFixo > 0 ? PaxFixo : Math.Min(PaxMax, PaxInicial + PaxPorOnda * onda);
    }

    /// <summary>Historias do TA-3 (GDD §5.9). A v0.1 tem 1. Id = indice (vai no album do save): so cresce no fim.</summary>
    public static class Historias
    {
        public static readonly string[] Texto = { "Dona Rosa vai ao casamento da neta" };
    }
}
