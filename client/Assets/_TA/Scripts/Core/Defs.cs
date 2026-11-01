using System;
using System.Collections.Generic;

// Tipos e numeros do nucleo (docs/CONTRATO_V01.md §3 e §4). Enum salvo por indice: so cresce no FIM; o nome de exibicao nunca e' chave.
namespace TA.Core
{
    /// <summary>As 4 estacoes da v0.1, na ordem do fluxo, e as 4 da Fase C2 (CONTRATO_FASE_C2 §1), que abrem pelo marco. Indice vai no save.
    /// Fluxo: check-in -> [bagagem] -> seguranca -> [loja] -> [alimentacao] -> portao. Oficina e despacho nao tem fila de passageiros.</summary>
    public enum Estacao { CheckIn, Seguranca, Loja, Portao, Bagagem, Alimentacao, Manutencao, Despacho }

    /// <summary>Os 6 perfis do GDD §5.5, um por pessoa, na ordem de contratacao (Ana, Beto, Caio, Dani, Edu, Fatima). Indice vai no save.</summary>
    public enum Perfil { Atendente, Seguranca, Vendedor, Bagageiro, Mecanico, Despachante }

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
        // Fase C (CONTRATO_FASE_C §5): tripulacao, patio e manutencao
        TripulanteContratado, // A = tripulante, B = custo, C = funcao                           -> crew_hired
        Escalado,           // A = tripulante, B = voo, C = 1 se manual (Escala)                 -> crew_assigned
        SemTripulacao,      // A = voo, B = funcao que falta (1x por voo)                         -> crew_missing
        Descanso,           // A = tripulante, B = s de descanso que faltam                       -> crew_rest_started
        Pousou,             // A = aviao, B = voo que vai fazer (-1 = nenhum aberto)              -> aircraft_landed
        ServicoFeito,       // A = aviao, B = servico                                             -> ground_service_done
        TurnaroundFeito,    // A = aviao, B = voo, C = 1 se terminou depois do fechamento        -> turnaround_done
        Decolou,            // A = aviao, B = voo, C = atraso (s) sobre o fechamento do portao   -> flight_takeoff
        PatioContratado,    // A = pessoas de patio, B = custo                                    -> ground_staff_hired
        PatioMelhorou,      // A = servico, B = melhoria, C = nivel novo                          -> ground_vehicle_upgraded
        ManutencaoAviso,    // A = aviao, B = desgaste                                            -> maintenance_warning
        ManutencaoPendente, // A = aviao, B = desgaste                                            -> maintenance_due
        ChecagemIniciada,   // A = aviao, B = custo, C = 1 se inspecao obrigatoria                -> maintenance_check_started
        ChecagemFeita,      // A = aviao, B = desgaste depois                                     -> maintenance_check_done
        HangarIniciado,     // A = aviao, B = custo                                               -> hangar_started
        HangarFeito,        // A = aviao                                                          -> hangar_done
        // Fase C2 (CONTRATO_FASE_C2): estacoes novas pelo marco
        EstacaoAberta,      // A = estacao                                                        -> station_unlocked
        // Fase C3 (CONTRATO_FASE_C3): rotas, aeroportos e incidentes
        RotaAberta,         // A = rota, B = custo                                                -> route_unlocked
        RotaProgramada,     // A = vaga, B = rota (vale na proxima onda)                          -> route_scheduled
        Viagem,             // A = aeroporto, B = custo (chega na proxima onda)                   -> airport_trip
        AeroportoChegada,   // A = aeroporto                                                      -> airport_arrived
        IncidenteAviso,     // A = TipoIncidente, B = vaga, C = s ate comecar                     -> incident_warning
        IncidenteInicio,    // A = TipoIncidente, B = vaga, C = estacao (falha)                   -> incident_started
        IncidenteFim,       // A = TipoIncidente, B = 1 se resolvido                              -> incident_ended
        Adiantado,          // A = voo (frente fria: embarque adiantado)                          -> incident_boarding_advanced
        ReparoIniciado,     // A = estacao, B = custo (0 = mecanico na estacao)                   -> incident_repair_started
        Reparado,           // A = estacao                                                        -> incident_repaired
        Priorizado,         // A = voo (grupo de conexao passa a frente)                          -> incident_group_prioritized
        // Fase C4 (CONTRATO_FASE_C4): clima e falhas de aviao
        Previsto,           // A = TipoIncidente, B = onda (previsao da onda seguinte)            -> weather_forecast
        Atrasado,           // A = voo, B = s de atraso do portao                                 -> gate_delayed
        Degelou,            // A = aviao, B = voo                                                 -> deicing_done
        Pane,               // A = aviao, B = voo                                                 -> aircraft_fault
        PaneReparo,         // A = aviao, B = custo (reparo pago no lugar da inspecao)            -> aircraft_fault_repair_started
        PaneReparada,       // A = aviao                                                          -> aircraft_fault_repaired
        Rejeicao,           // A = aviao, B = voo (abortou a decolagem; a pista bloqueia)         -> takeoff_rejected
        RetornoPedido,      // A = aviao (bombeiros e ambulancia em prontidao)                    -> return_requested
        PousoSeguro,        // A = aviao                                                          -> safe_landing
        Remarcados,         // A = aviao (os passageiros do voo foram remarcados)                 -> pax_rebooked
        // Fase C5 (CONTRATO_FASE_C5): campanha
        ObjetivoConcluido,  // A = aeroporto, B = indice na cadeia, C = Cash                      -> campaign_objective_completed
        // Fase D1 (CONTRATO_FASE_D1): Gems e compras
        CompraCreditada,    // A = produto, B = Gems, C = Cash                                    -> purchase_succeeded_server_verified
        GemsGastas,         // A = item, B = preco, C = Cash do pacote, s do booster ou 1         -> gems_spent
        GemsGanhas,         // A = aeroporto, B = Gems (cadeia fechada)                           -> gems_earned
    }

    /// <summary>Funcoes da tripulacao (GDD §5.11). Indice vai no save.</summary>
    public enum Funcao { Piloto, Comissario }

    /// <summary>Servicos do turnaround, na ordem (GDD §5.12). Indice vai no save.</summary>
    public enum Servico { Desembarque, Limpeza, Combustivel, Bagagem }

    /// <summary>Piloto ou comissario: fixo e nomeado pela ordem de contratacao dentro da funcao (TA-5: nada sorteado).</summary>
    public sealed class Tripulante
    {
        public static readonly string[][] Nomes =
        {
            new[] { "Cmte. Helena", "Cmte. Otavio", "Cmte. Iara", "Cmte. Bruno", "Cmte. Lucia", "Cmte. Davi" },
            new[] { "Marina", "Caio", "Rute", "Tiago", "Nina", "Joel" },
        };
        /// <summary>Id = indice em Sim.Tripulacao (save, diario e Escala); Indice = ordem dentro da funcao (o nome).</summary>
        public int Id, Indice; public Funcao Funcao;
        public string Nome => Nomes[(int)Funcao][Indice % Nomes[(int)Funcao].Length];
        /// <summary>Trechos voados desde o ultimo descanso.</summary>
        public int Trechos;
        /// <summary>Passos seguidos na sala da tripulacao (0 a bordo). Tuning.DescansoSeg na sala = descansado.</summary>
        public int Sala;
        /// <summary>Aviao em que esta a bordo (-1 = na sala).</summary>
        public int Aviao = -1;
    }

    /// <summary>Aviao da frota (CONTRATO_FASE_C §1): o aviao f faz o voo f de cada onda.</summary>
    public sealed class Aviao
    {
        public int Id;
        /// <summary>0..100 (CONTRATO_FASE_C §4).</summary>
        public int Desgaste;
        /// <summary>Passo absoluto em que encosta (ou encostou) no patio (-1 = ainda nao agendado: a 1a onda agenda).</summary>
        public long Chega = -1;
        public bool NoPatio;
        /// <summary>Voo (Id) que este aviao vai fazer (-1 = nenhum) e o passo em que o portao dele fechou com gente a bordo (-1 = nao
        /// fechou; >= 0 = falta so decolar).</summary>
        public int VooId = -1; public long FechouEm = -1;
        /// <summary>Passos que faltam em cada servico do turnaround (0 = feito) e quais estao sendo feitos agora.</summary>
        public readonly int[] Falta = new int[4]; public readonly bool[] Fazendo = new bool[4];
        /// <summary>Passos da checagem em andamento (0 = nenhuma), se e' a inspecao obrigatoria, e se ja houve checagem neste turnaround.</summary>
        public int Check; public bool Inspecao, Checou;
        /// <summary>Passos que faltam no hangar (0 = fora do hangar).</summary>
        public int Hangar;
        /// <summary>Ja avisou falta de tripulacao para o voo atual.</summary>
        public bool AvisouTrip;
        /// <summary>Rota do voo que pegou e a onda em que volta a ter voo (rota longa: fica fora da onda seguinte).</summary>
        public int Rota, VoltaOnda;
        /// <summary>Fase C4: em pane (parado ate o reparo), voo que ja falhou (sem novo sorteio), passo a partir do qual decola depois de rejeitar
        /// e passo em que pede para voltar (0 = nao esta voltando).</summary>
        public bool Pane; public int Falhou = -1; public long SaiEm, Retorno;
        public bool Pendente => Desgaste >= 100;
        public bool PatioPronto { get { foreach (int f in Falta) if (f > 0) return false; return true; } }
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
        /// <summary>Passos que faltam no atendimento atual (0 na fila) e o total dele (o anel de progresso: 1 - Resto/Total).</summary>
        public int Resto, Total;
        /// <summary>Loja, mala despachada e fome saem da semente (nao se compram).</summary>
        public bool Loja, Historia, Mala, Fome;
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
        /// <summary>Perfil fixo (GDD §5.5): a especialidade rende Tuning.Especialista a partir do marco Tuning.EspecialistaMarco.</summary>
        public Perfil Perfil;
        /// <summary>Estacao onde esta ou para onde esta andando.</summary>
        public Estacao Em, De;
        /// <summary>Passos de caminhada que faltam (0 = presente e atendendo).</summary>
        public int Andando;

        /// <summary>Estacoes da especialidade de cada perfil (CONTRATO_FASE_C2 §2).</summary>
        public static bool Especialidade(Perfil p, Estacao e)
        {
            switch (p)
            {
                case Perfil.Atendente: return e == Estacao.CheckIn || e == Estacao.Portao;
                case Perfil.Seguranca: return e == Estacao.Seguranca;
                case Perfil.Vendedor: return e == Estacao.Loja || e == Estacao.Alimentacao;
                case Perfil.Bagageiro: return e == Estacao.Bagagem;
                case Perfil.Mecanico: return e == Estacao.Manutencao;
                default: return e == Estacao.Despacho;
            }
        }

        /// <summary>A estacao principal do perfil: para onde o bot que planeja o leva de volta (o Vendedor vai a alimentacao, que vende mais).</summary>
        public static Estacao Casa(Perfil p) =>
            new[] { Estacao.CheckIn, Estacao.Seguranca, Estacao.Alimentacao, Estacao.Bagagem, Estacao.Manutencao, Estacao.Despacho }[(int)p];
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
        /// <summary>Indice em Tuning.Rotas (Fase C3). Total = 0: o aviao da vaga esta fora na rota longa, nao ha voo nesta onda.</summary>
        public int Rota;
        /// <summary>Grupo extra de incidente, ja somado no Total: os ultimos Grupo passageiros, a partir do passo GrupoChega. GrupoConexao =
        /// entra na seguranca e paga Tuning.ConexaoTarifa x; Prioridade = o jogador priorizou (fura a fila). Adiantado = embarca com a pista
        /// fechada (frente fria).</summary>
        public int Grupo, GrupoChega; public bool GrupoConexao, Prioridade, Adiantado;
        /// <summary>Clima: o jogador atrasou o portao (FechaPasso ja mudou) e o voo nao leva o bonus de pontualidade.</summary>
        public bool Atrasado;
        public int Base => Total - Grupo;
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
        /// <summary>s por passageiro no nivel 0, na ordem de Estacao (oficina e despacho nao atendem passageiro).</summary>
        public float[] Tempo = { 4f, 5f, 3f, 1.5f, 3f, 4f, 0f, 0f };
        public float VelFator = 0.88f;
        public int VelNiveis = 5, VelCusto = 40; public float VelCresce = 1.6f;
        public int CapNiveis = 2, CapCusto = 300; public float CapCresce = 2.5f;
        /// <summary>Balcoes (postos) de cada estacao no nivel 0 de capacidade, na ordem de Estacao.</summary>
        public int[] PostosBase = { 2, 2, 1, 1, 1, 1, 1, 1 };
        /// <summary>LojaFolgaMin vale para as 3 paradas opcionais (loja, bagagem e alimentacao): so vai quem tem essa folga no voo.</summary>
        public float LojaChance = 0.5f, LojaFolgaMin = 40f; public int LojaVenda = 6;
        public int Tarifa = 8, TarifaPorMarco = 2;
        public float Bonus = 0.25f;
        /// <summary>Preco fixo por perfil (Ana, Beto, Caio pela ordem da v0.1; Dani, Edu e Fatima depois do marco deles).</summary>
        public int[] Contratacao = { 20, 150, 400, 300, 450, 450 };

        // ---- Fase C2 (CONTRATO_FASE_C2 §1-§2, hipoteses) ----
        /// <summary>Portao de design do GDD §5.13 por sistema (false = so visual: a estacao abre e a equipe fica la, mas nao rende). A
        /// especializacao ficou so visual: ganho 1,00 e, ligada, derruba o TA-2 para 1,07 (BALANCE.md D-C2a).</summary>
        public bool RegraEspecialista = false, RegraBagagem = true, RegraAlimentacao = true, RegraOficina = true, RegraDespacho = true;
        /// <summary>Marco do terminal que abre cada estacao (ordem de Estacao), que libera cada perfil e a especializacao.</summary>
        public int[] Abre = { 0, 0, 0, 0, 1, 1, 2, 2 };
        public int[] PerfilMarco = { 0, 0, 0, 1, 2, 2 };
        public int EspecialistaMarco = 1;
        /// <summary>O especialista rende isto na estacao dele: tempo / x, venda x, forca x (oficina e despacho).</summary>
        public float Especialista = 1.5f;
        public float MalaChance = 0.35f; public int MalaTaxa = 5;
        public float FomeChance = 0.5f; public int ComidaVenda = 8;
        /// <summary>Despacho com gente: bonus de pontualidade + DespachoBonus x forca dos presentes.</summary>
        public float DespachoBonus = 0.25f;
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

        // ---- Fase C (CONTRATO_FASE_C §1-§4, hipoteses) ----
        /// <summary>Portao de design do GDD §5.13: sistema que nao gerar decisao no bot fica so visual (false = nao segura embarque nem
        /// decolagem; a View continua mostrando). Tudo false = a regra da v0.1. Tripulacao desligada: rendeu 1,02 sozinha (BALANCE.md D-C2).</summary>
        public bool RegraTripulacao = false, RegraPatio = true, RegraManutencao = true;
        /// <summary>Ciclo do aviao (1 por voo da onda): encosta PatioAntes s antes do fechamento; o trecho dura Trecho s.</summary>
        public float PatioAntes = 100f, Trecho = 80f;
        /// <summary>Tripulacao por voo (regional), por Funcao; no comeco, por funcao; teto por funcao no marco 0..3; precos pela ordem.</summary>
        public int[] TripPorVoo = { 1, 1 };
        public int TripIniciais = 3;
        public int[] TripTeto = { 3, 4, 5, 6 };
        public int[][] TripCusto = { new[] { 300, 450, 600 }, new[] { 150, 250, 350 } };
        public int TrechosAteDescanso = 3; public float DescansoSeg = 180f;
        /// <summary>s de cada servico no nivel 0, na ordem de Servico.</summary>
        public float[] ServicoSeg = { 12f, 18f, 20f, 24f };
        public int PatioIniciais = 1; public int[] PatioContratacao = { 40, 150 };
        public int PatioVelNiveis = 3, PatioVelCusto = 30; public float PatioVelCresce = 1.6f;
        public int PatioCapNiveis = 1, PatioCapCusto = 120;
        public int DesgastePorTrecho = 15, DesgasteAviso = 80;
        public float CheckSeg = 8f; public int CheckTira = 10, CheckCusto = 10;
        /// <summary>A inspecao obrigatoria segura o aviao o turnaround inteiro (110 s &gt; os 100 s de patio): o voo dele nao embarca.</summary>
        public float InspecaoSeg = 110f; public int InspecaoTira = 50;
        public float HangarSeg = 180f; public int HangarCusto = 100;

        // ---- Fase C3 (CONTRATO_FASE_C3, hipoteses) ----
        /// <summary>Portao de design do GDD §5.13 por sistema (false = so visual: o nome muda, os numeros nao).</summary>
        public bool RegraRotas = true, RegraAeroportos = true, RegraIncidentes = true;
        /// <summary>As tabelas (Rotas.cs). Aeroporto do jogo novo: 0 = Vale Verde; os testes comecam em outro.</summary>
        public Rota[] Rotas = Tabelas.Rotas;
        public Aeroporto[] Aeroportos = Tabelas.Aeroportos;
        public int AeroportoInicial;
        /// <summary>Medicao do TA-6: as 2 vagas comecam nesta rota (-1 = as iniciais do aeroporto).</summary>
        public int RotaFixa = -1;
        /// <summary>Chance de incidente por ponto de risco das rotas da onda, teto da chance, ondas sem incidente depois de um e s de aviso.</summary>
        public float RiscoChance = 0.1f, IncidenteTeto = 0.8f, AvisoSeg = 30f; public int IncidenteFolga = 1;
        /// <summary>Falha de equipamento: FalhaFator x mais devagar por FalhaSeg s; reparo de ReparoCusto Cash em ReparoSeg s.</summary>
        public int FalhaFator = 5, ReparoCusto = 30; public float FalhaSeg = 60f, ReparoSeg = 6f;
        /// <summary>Grupos extras, em fracao dos passageiros da onda. Atraso de conexao: ConexaoAntes s antes do portao, tarifa ConexaoTarifa x.</summary>
        public float PicoExtra = 0.35f, ConexaoExtra = 0.25f, ConexaoAntes = 30f; public int ConexaoTarifa = 3;

        // ---- Fase C4 (CONTRATO_FASE_C4, hipoteses) ----
        /// <summary>Portao de design do GDD §5.13 por sistema. Tudo false = o jogo da Fase C3 (a frente fria volta aos pesos do aeroporto). A demanda
        /// por estacao do ano ficou so visual: ganho 0,985 (BALANCE.md D-C4a). Os picos do dia nao tem escolha propria: so nao podem quebrar os portoes.</summary>
        public bool RegraClima = true, RegraEpocas = false, RegraPicos = true, RegraFalhas = true;
        /// <summary>Ondas por dia (1 onda = 3 h de jogo), dias por estacao do ano, hora em que a onda 0 comeca e marco que abre clima e demanda.</summary>
        public int DiaOndas = 8, EpocaDias = 1, ClimaMarco = 1; public float HoraInicial = 6f;
        /// <summary>Fator de passageiros de cada onda do dia, a partir das 06h: picos de manha e no fim da tarde.</summary>
        public float[] DemandaHora = { 1.25f, 1f, 0.9f, 1f, 1.25f, 1f, 0.75f, 0.85f };
        /// <summary>As linhas de clima (Rotas.cs) e os s que o portao atrasado fecha depois de reabrir.</summary>
        public Clima[] Climas = Tabelas.Climas; public float AtrasoFolga = 20f;
        /// <summary>Falha por voo com o desgaste no aviso, e os pesos de pane, rejeicao e retorno.</summary>
        public float FalhaAviaoChance = 0.4f; public int[] FalhaAviaoPesos = { 3, 2, 1 };
        public int PaneCusto = 30; public float PaneSeg = 8f;
        /// <summary>Rejeicao: o aviao sai RejeicaoSeg s depois. Retorno: pede RetornoPede s depois de decolar e pousa RetornoVolta s depois disso.
        /// Os dois deixam a pista bloqueada por PistaBloqueioSeg s.</summary>
        public float RejeicaoSeg = 40f, PistaBloqueioSeg = 20f, RetornoPede = 15f, RetornoVolta = 25f;

        // ---- Fase C5 (CONTRATO_FASE_C5, hipoteses) ----
        /// <summary>Portao de design do GDD §5.13 (false = so visual: conta e mostra, sem pagar) e a tabela (Objetivos.cs).</summary>
        public bool RegraCampanha = true;
        public Objetivo[][] Objetivos = Tabelas.Objetivos;
        /// <summary>Voo lotado para a campanha: pelo menos esta parte dos passageiros a bordo, em %.</summary>
        public int LotadoPct = 90;

        // ---- Fase D1 (CONTRATO_FASE_D1, hipoteses) ----
        /// <summary>As tabelas (Compras.cs), o teto do saldo de Gems e o que fechar a cadeia de um aeroporto paga.</summary>
        public Produto[] Produtos = Tabelas.Produtos;
        public ItemGems[] ItensGems = Tabelas.ItensGems;
        public long GemsTeto = 9999999;
        public int GemsPorCadeia = 100;
        /// <summary>Pacote de Cash: PacoteSeg s da renda media atual, entre PacotePiso e PacoteTeto. Ate 3 600 s: acima de 1 h de renda e' recompensa de alto valor (GDD §7.2).</summary>
        public int PacoteSeg = 600, PacotePiso = 200, PacoteTeto = 10000;
        /// <summary>Booster: 2x renda por BoosterSeg s de jogo (so anda com o jogo aberto).</summary>
        public int BoosterSeg = 1800;
    }

    /// <summary>Historias do TA-3 (GDD §5.9). A v0.1 tem 1. Id = indice (vai no album do save): so cresce no fim.</summary>
    public static class Historias
    {
        public static readonly string[] Texto = { "Dona Rosa vai ao casamento da neta" };
    }
}
