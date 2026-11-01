using System;
using System.Collections.Generic;

// API do nucleo para a View (TA.Core, C# puro, deterministico, passo fixo de 1/30 s; nunca le o relogio do aparelho).
// Regras e numeros: docs/CONTRATO_V01.md §3 e §4 e docs/CONTRATO_FASE_C.md (Defs.cs, Tuning). Arquivos: Sim.cs (relogio, onda, filas,
// equipe), Sim.Estados.cs (estado das estacoes), Sim.Patio.cs (ciclo do aviao, patio e manutencao), Sim.Tripulacao.cs, Sim.Save.cs,
// Sim.Offline.cs (cofre e dia local), Sim.Metas.cs (campanha), Anuncios.cs (2 rewarded), Bot.cs.
//
//   Abrir:      var sim = Sim.Load(texto) ("" ou null = jogo novo; Sim.Load(texto, tuning, semente) para teste). sim.SaveRefused => save
//               de schema futuro ou corrompido: avise e NUNCA grave por cima (sim.Save devolve null). Depois: sim.Agora(nowUnix, fusoSeg)
//               e sim.ApplyOffline(nowUnix - sim.SavedAt, sim.SavedAt) = o cofre (Offline -> Ev.Offline).
//   Jogar:      a cada quadro sim.Tick(dt) (passos fixos de Sim.Step; dt invalido ou <= 0 nao anda; teto Sim.MaxFrame). Depois leia
//               sim.Events (as intencoes desde o Tick anterior e o que aconteceu neste Tick). Intencoes: sim.Tap(estacao), sim.Move(funcionario, estacao), sim.Buy(estacao, melhoria),
//               sim.Hire(). Todas devolvem bool (false = nada mudou) e emitem o Ev correspondente.
//   Fase C:     sim.HireTripulante(funcao), sim.Escala(voo, tripulante) (troca no quadro: so com o aviao no patio e antes do embarque),
//               sim.HirePatio(), sim.BuyPatio(servico, melhoria), sim.Checagem(aviao) e sim.Hangar(aviao) (so Cash e tempo). O resto e' o
//               padrao do nucleo: escala automatica (1o livre na ordem), servicos do patio, inspecao obrigatoria do aviao pendente.
//   Ler:        sim.Time, sim.Cash, sim.Voos[0..1] (quadro da onda: horario em passos da onda, sim.PassoNaOnda), sim.Stations[e]
//               (fila, atendendo, Estado/Folga/VooEmRisco), sim.Staff (Em, Andando), sim.Presentes(e), sim.Custo(e, m),
//               sim.CustoContratar(), sim.Marco, sim.Tarifa, sim.Album, sim.BoostLeft, sim.Anuncios (Pode/Oferta/Pagar).
//               Fase C: sim.Avioes[f] (o aviao do voo f: Desgaste, NoPatio, Falta/Fazendo por Servico, Check/Inspecao, Hangar, VooId),
//               sim.Pronto(aviao) (o embarque pode abrir), sim.Tripulacao (Nome, Funcao, Trechos, Aviao; sim.Livre, sim.DescansoFalta),
//               sim.EquipePatio, sim.ServicoPassos(s), sim.Veiculos(s), sim.MecanicoLivre, sim.CustoTripulante(f), sim.CustoPatio(),
//               sim.CustoPatio(s, m), sim.TetoTripulacao, sim.PodeChecagem(a), sim.PodeHangar(a), sim.PodeEscala(a) (a tela pergunta antes de
//               oferecer). Ev.VooPartiu = portao fechou; Ev.Decolou = o aviao saiu (patio terminado).
//   Fase C2:    sim.Estacoes[0..7] (as 8; sim.Stations continua com as 4 da v0.1), sim.Desbloqueada(e) (abre pelo marco: Ev.EstacaoAberta),
//               sim.Rende(e) (o especialista presente), Funcionario.Perfil, sim.Contratado(p), sim.CustoContratar(p) e sim.Hire(p) (Dani, Edu
//               e Fatima; sim.Hire() continua contratando os 3 da v0.1), Pax.Total (anel de progresso), Sim.MaxEquipe (6).
//   Fase C3:    tabelas sim.T.Rotas e sim.T.Aeroportos (Id estavel, Nome de trabalho). sim.AeroportoAtual, sim.CustoViagem(a), sim.Viajar(a) (vale
//               na proxima onda); sim.RotaAberta(r), sim.CustoRota(r), sim.AbreRota(r), sim.Prog[vaga], sim.Programa(vaga, r) (vale na proxima
//               onda); Voo.Rota, Voo.Total = 0 = sem voo (o aviao esta fora na rota longa); sim.TrechoSeg(r). Incidentes: sim.Inc (null ate o
//               aviso: Tipo, Vaga, Estacao, Inicio, Fim, Resolvido), sim.PistaFechada, sim.Falhou(e), sim.Reparo; saidas sim.Adianta(),
//               sim.Repara() e sim.Prioriza(); Voo.Grupo (grupo extra, ja somado no Total), Ev.Incidente*, Ev.Rota*, Ev.Viagem, Ev.AeroportoChegada.
//   Fase C4:    sim.Hora, sim.DiaJogo, sim.Noite, sim.Epoca, sim.Previsao (clima da onda seguinte; null = tempo bom), sim.ClimaDe(tipo) (o que o
//               clima faz: FechaPista, ParaPatio, Espera, Degelo), sim.PatioParado, sim.EsperaPista, sim.PistaAte, sim.Degelando(aviao),
//               sim.EmRisco(aviao), Sim.Opcoes(tipo) e Sim.Opcoes(falha). Clima vem em sim.Inc, com os tipos novos. Saidas: sim.Adianta(), sim.Atrasa()
//               e segurar (nao fazer nada). Falhas: Aviao.Pane, SaiEm e Retorno; reparo sim.ReparaAviao(aviao) (so Cash); Ev.Pane, Ev.Rejeicao,
//               Ev.RetornoPedido, Ev.PousoSeguro, Ev.Remarcados.
//   Fase C5:    tabela sim.T.Objetivos[aeroporto][i] (Id, Nome, Meta, Arg, Alvo, Recompensa). sim.ObjetivoAtual(aeroporto) (null = cadeia fechada),
//               sim.ObjetivosFeitos(aeroporto), sim.Progresso(aeroporto), sim.CampanhaCompleta; Ev.ObjetivoConcluido (Cash ja pago).
//   Gravar:     texto = sim.Save(versaoDoJogo, nowUnix): payload chave=valor dentro do SaveEnvelope (SHA-256 + schema). Grava filas e o
//               relogio do quadro: reabrir retoma a onda do ponto gravado. Schema 6; os saves dos schemas 1 a 5 migram no Load.
//
// Nomes acima sao estaveis (so crescem). Mudou a API: atualize este cabecalho e o docs/MAPA_CODIGO.md no mesmo PR.

namespace TA.Core
{
    public sealed partial class Sim
    {
        /// <summary>Passo fixo do nucleo (s): bot, testes e View andam no mesmo ritmo.</summary>
        public const float Step = 1f / 30f;
        public const int Hz = 30;
        /// <summary>Teto do dt de um quadro (s): uma travada longa nao vira um salto de minutos no relogio do quadro de partidas.</summary>
        public const float MaxFrame = 0.25f;
        /// <summary>Versao do formato do save (SaveEnvelope). Sobe a cada mudanca de formato; save de schema maior e' recusado; menor migra
        /// no Load (6 = Fase C5: campanha; 5 = Fase C4: falhas de aviao e portao atrasado; 4 = Fase C3: aeroportos, rotas e incidentes; 3 = Fase C2: 8 estacoes e o perfil de cada funcionario; 2 = Fase C: avioes,
        /// tripulacao e patio; o 1 e' a v0.1).</summary>
        public const int Schema = 6;
        /// <summary>A equipe da v0.1 (Hire()): Ana, Beto e Caio. MaxEquipe = os 6 perfis, com Dani, Edu e Fatima (Hire(perfil)).</summary>
        public const int MaxFuncionarios = 3, MaxEquipe = 6;

        /// <summary>Passos fixos ja simulados (o relogio do quadro; vai no save; nao anda com o jogo fechado).</summary>
        public long Steps;
        /// <summary>A unica moeda da v0.1 (GDD §6.1). Nunca negativa.</summary>
        public long Cash;
        /// <summary>O save lido era de schema futuro ou corrompido: este Sim e' novo e Save devolve null, para nao apagar o save do jogador.</summary>
        public bool SaveRefused { get; private set; }

        public readonly Tuning T;
        /// <summary>Semente das chegadas, da loja e do passageiro com historia (vai no save).</summary>
        public readonly int Seed;
        /// <summary>As 8 estacoes (ordem de Estacao). Stations = as 4 da v0.1 (os mesmos objetos), para quem ainda so desenha essas.</summary>
        public readonly Station[] Estacoes = new Station[8];
        public readonly Station[] Stations = new Station[4];
        public readonly List<Funcionario> Staff = new List<Funcionario>();
        /// <summary>Os 2 voos da onda atual (quadro de partidas). Null antes do 1o passo.</summary>
        public readonly Voo[] Voos = new Voo[2];
        public readonly List<Evento> Events = new List<Evento>();
        public readonly Anuncios Anuncios;

        /// <summary>Onda atual (0 = a 1a; -1 antes do 1o passo).</summary>
        public int Onda = -1;
        public long TotalEmbarcados, TotalPerdidos;
        /// <summary>Historias entregues (bit = id em Historias.Texto).</summary>
        public int Album;
        /// <summary>Passos de 2x renda que faltam (rewarded 2).</summary>
        public int BoostLeft;

        public float Time => Steps * Step;
        public int OndaPassos => (int)Math.Round(T.Onda * Hz);
        /// <summary>Passo dentro da onda atual (o relogio do quadro).</summary>
        public int PassoNaOnda => (int)(Steps % OndaPassos);
        public int Marco { get { int m = 0; foreach (int x in T.Marcos) if (TotalEmbarcados >= x) m++; return m; } }
        public int Tarifa => T.Tarifa + T.TarifaPorMarco * Marco;
        public Station St(Estacao e) => Estacoes[(int)e];

        float _acc;

        public Sim() : this(Tuning.Padrao, 1) { }

        public Sim(Tuning t, int seed)
        {
            T = t ?? Tuning.Padrao;
            Seed = seed;
            _feitos = new int[T.Objetivos.Length];
            _conta = Array.ConvertAll(T.Objetivos, c => new int[c.Length]);
            for (int i = 0; i < Estacoes.Length; i++) Estacoes[i] = new Station { Tipo = (Estacao)i, Postos = Math.Max(1, T.PostosBase[i]) };
            Array.Copy(Estacoes, Stations, Stations.Length);
            Anuncios = new Anuncios(this);
            for (int i = 0; i < Math.Min(MaxFuncionarios, T.FuncionariosIniciais); i++) Staff.Add(new Funcionario { Id = i, Perfil = (Perfil)i, Em = Lugar(), De = Lugar() });
            Avioes = new[] { new Aviao { Id = 0 }, new Aviao { Id = 1, Rota = 1 } };
            Chega(Math.Max(0, Math.Min(T.Aeroportos.Length - 1, T.AeroportoInicial)));
            EquipePatio = T.PatioIniciais;
            for (int f = 0; f < T.TripPorVoo.Length; f++)
                for (int i = 0; i < T.TripIniciais; i++) Tripulacao.Add(NovoTripulante((Funcao)f));
        }

        /// <summary>Anda o relogio. Events passa a ter as intencoes feitas desde o Tick anterior (toque, mover, comprar, cofre, anuncio) e o
        /// que aconteceu neste Tick; o que a View ja leu depois do Tick anterior sai.</summary>
        public void Tick(float dt)
        {
            Events.RemoveRange(0, Math.Min(_vistos, Events.Count));
            if (dt > 0f)   // tambem pega NaN: com Math.Max(NaN, 0) o acumulador virava NaN e o relogio parava para sempre
            {
                _acc += Math.Min(dt, MaxFrame);
                while (_acc >= Step - 1e-6f)
                {
                    _acc -= Step;
                    Passo();
                }
            }
            _vistos = Events.Count;
        }

        int _vistos;

        // ------------------------------------------------------------------ passo fixo

        long _ganhoPasso;

        void Passo()
        {
            int w = (int)(Steps / OndaPassos), loc = PassoNaOnda;
            if (w != Onda) NovaOnda(w);
            PassoIncidente();
            Sala();
            Patio();
            foreach (Voo v in Voos)
            {
                if (v.Partiu) continue;
                while (v.Criados < v.Total && Chegada(v, v.Criados) <= loc) Entra(v, v.Criados++);
                if (!v.Embarque && loc >= v.AbrePasso)
                {
                    // Fase C: o embarque espera o aviao liberado (patio, tripulacao e manutencao: Sim.Patio.cs)
                    if (loc == v.AbrePasso) ContaAtrasoPatio(v);
                    Aviao a = Avioes[v.Indice];
                    if (a.VooId == v.Id && Pronto(a) && !EntrouEmPane(a)) { v.Embarque = true; Emit(Ev.EmbarqueAberto, v.Id); }
                }
            }
            foreach (Funcionario f in Staff)
                if (f.Andando > 0 && --f.Andando == 0) Emit(Ev.Chegou, f.Id, (int)f.Em);
            foreach (Station s in Estacoes) Atende(s, loc);
            foreach (Voo v in Voos) if (!v.Partiu && loc >= v.FechaPasso) Fecha(v);
            Decolagens();
            PassoMetas();
            if (BoostLeft > 0) BoostLeft--;
            if (Steps % (Hz / 2) == 0) AtualizaEstados(true);
            if (Steps % (10 * Hz) == 0) foreach (Station s in Estacoes) if (Desbloqueada(s.Tipo)) Emit(Ev.Fila, (int)s.Tipo, s.Fila.Count, (int)s.Estado);
            RateEma += (_ganhoPasso * (double)Hz - RateEma) * Math.Min(1.0, Step / T.RendaTau);
            _ganhoPasso = 0;
            Steps++;
        }

        void NovaOnda(int w)
        {
            foreach (Voo v in Voos) if (v != null && !v.Partiu) Fecha(v);   // so com save adulterado: a onda anterior sempre fecha antes
            Onda = w;
            if (ProximoAeroporto >= 0)
            {
                Chega(ProximoAeroporto);
                UltimoIncidente = w;
                Emit(Ev.AeroportoChegada, AeroportoAtual);
            }
            for (int f = 0; f < 2; f++) Voos[f] = NovoVoo(w, f, Prog[f]);
            SorteiaIncidente(w);
            if (Previsao is TipoIncidente previsto) Emit(Ev.Previsto, (int)previsto, w + 1);
            foreach (Voo v in Voos) Emit(Ev.VooCriado, v.Id, v.Total, v.FechaPasso);
            PatioNaOnda(w);
        }

        Voo NovoVoo(int w, int f, int rota)
        {
            int fecha = (int)Math.Round(T.Fecha[f] * Hz);
            return new Voo
            {
                Id = w * 2 + f, Onda = w, Indice = f, Rota = rota, Total = w < Avioes[f].VoltaOnda ? 0 : PaxDaRota(w, rota), FechaPasso = fecha,
                AbrePasso = Math.Max(0, fecha - (int)Math.Round(T.AbreEmbarqueAntes * Hz)),
            };
        }

        /// <summary>Passo (na onda) em que chega o i-esimo passageiro do voo: estratificado de 0 ate fechamento - ChegadaFolga, com jitter da
        /// semente; o Pico da rota tira o inicio da janela. O grupo extra de incidente vem depois, 2 por s. Funcao pura (nada a gravar):
        /// retomar no meio da onda refaz as mesmas chegadas.</summary>
        public int Chegada(Voo v, int i)
        {
            if (i >= v.Base) return v.GrupoChega + (i - v.Base) * Hz / 2;
            double span = Math.Max(0.0, T.Fecha[v.Indice] - T.ChegadaFolga) * Hz, ini = span * Efeito(v).Pico;
            return (int)(ini + (span - ini) * (i + U(v.Onda, v.Indice, i, 0)) / v.Base);
        }

        public static int PaxId(int onda, int voo, int i) => onda * 1000 + voo * 500 + i;

        /// <summary>Passageiro pelo Id, com loja e historia da semente (o Load e os testes usam).</summary>
        public Pax NovoPax(int id)
        {
            var p = new Pax { Id = id };
            p.Loja = U(p.Onda, p.Voo, p.Indice, 1) < T.LojaChance;
            p.Mala = U(p.Onda, p.Voo, p.Indice, 2) < T.MalaChance;
            p.Fome = U(p.Onda, p.Voo, p.Indice, 3) < T.FomeChance;
            p.Historia = p.Onda >= 1 && p.Voo == VooDaHistoria(p.Onda) && p.Indice == T.Pax(p.Onda) / 2;
            return p;
        }

        /// <summary>TA-3: a partir da onda 2 (indice 1), 1 passageiro por onda, no voo da sequencia de Weyl (como o VIP do Forge), o do meio da fila.</summary>
        public static int VooDaHistoria(int onda)
        {
            double x = onda * 0.6180339887498949;
            return (int)((x - Math.Floor(x)) * 2);
        }

        /// <summary>Quem vem de conexao ja fez check-in na origem: entra na seguranca.</summary>
        void Entra(Voo v, int i) => St((i >= v.Base ? v.GrupoConexao : Efeito(v).Tipo == TipoRota.Conexao) ? Estacao.Seguranca : Estacao.CheckIn).Fila.Add(NovoPax(PaxId(v.Onda, v.Indice, i)));

        public Voo VooDe(Pax p) => Voos[p.Voo] != null && Voos[p.Voo].Onda == p.Onda ? Voos[p.Voo] : null;

        // ------------------------------------------------------------------ estacoes

        /// <summary>Funcionarios presentes (nao andando) na estacao.</summary>
        public int Presentes(Estacao e)
        {
            int n = 0;
            foreach (Funcionario f in Staff) if (f.Em == e && f.Andando == 0) n++;
            return n;
        }

        /// <summary>Funcionarios na estacao ou a caminho dela.</summary>
        public int Alocados(Estacao e)
        {
            int n = 0;
            foreach (Funcionario f in Staff) if (f.Em == e) n++;
            return n;
        }

        /// <summary>Loja sem funcionario fica fechada: os passageiros passam direto (CONTRATO_V01 §3.4).</summary>
        public bool LojaAberta => Presentes(Estacao.Loja) > 0;

        /// <summary>Passos de atendimento de um passageiro na estacao, com o upgrade de velocidade e o especialista presente (tempo / Rende).
        /// Na loja e na alimentacao o especialista vende mais, nao atende mais rapido.</summary>
        public int TempoPassos(Estacao e) => Vende(e) ? TempoBase(e) : Math.Max(1, (int)Math.Round(T.Tempo[(int)e] * Math.Pow(T.VelFator, St(e).Vel) * Hz / Rende(e)));
        public float TempoSeg(Estacao e) => TempoPassos(e) * Step;
        /// <summary>Sem o especialista: o teto do atendimento gravado (o especialista pode ter saido no meio).</summary>
        int TempoBase(Estacao e) => Math.Max(1, (int)Math.Round(T.Tempo[(int)e] * Math.Pow(T.VelFator, St(e).Vel) * Hz));

        void Atende(Station s, int loc)
        {
            if (s.Tipo == Estacao.Manutencao || s.Tipo == Estacao.Despacho) return;   // sem fila de passageiros
            bool lento = Falhou(s.Tipo) && Steps % T.FalhaFator != 0;   // falha de equipamento: o atendimento anda 1 passo em cada FalhaFator
            for (int i = 0; i < s.Atendendo.Count; i++)
            {
                Pax p = s.Atendendo[i];
                if (lento || --p.Resto > 0) continue;
                s.Atendendo.RemoveAt(i--);
                Avanca(s, p, loc);
            }
            if (Opcional(s.Tipo) && !Funcionando(s.Tipo))
            {
                // parada opcional sem ninguem (a vendedora saiu): a fila segue para a proxima estacao, na ordem
                St(s.Tipo == Estacao.Bagagem ? Estacao.Seguranca : Estacao.Portao).Fila.AddRange(s.Fila);
                s.Fila.Clear();
                return;
            }
            int servidores = Math.Min(s.Postos, Presentes(s.Tipo));
            while (s.Atendendo.Count < servidores && Inicia(s)) { }
        }

        /// <summary>Comeca o atendimento do 1o da fila (no portao, o 1o cujo voo ja abriu o embarque). false = ninguem para atender.</summary>
        bool Inicia(Station s)
        {
            // com grupo priorizado (atraso de conexao), uma 1a passada so por ele
            for (int passada = Voos[0]?.Prioridade == true || Voos[1]?.Prioridade == true ? 0 : 1; passada < 2; passada++)
                for (int i = 0; i < s.Fila.Count; i++)
                {
                    Pax p = s.Fila[i];
                    Voo v = VooDe(p);
                    if (s.Tipo == Estacao.Portao && !PodeEmbarcar(v) || passada == 0 && !(v != null && v.Prioridade && p.Indice >= v.Base)) continue;
                    s.Fila.RemoveAt(i);
                    p.Resto = p.Total = TempoPassos(s.Tipo);
                    s.Atendendo.Add(p);
                    return true;
                }
            return false;
        }

        /// <summary>O portao atende este voo: embarque aberto e, com a pista fechada, so quem adiantou (clima).</summary>
        bool PodeEmbarcar(Voo v) => v != null && v.Embarque && (!PistaFechada || v.Adiantado);

        void Avanca(Station s, Pax p, int loc)
        {
            Conta(Meta.Atendidos, (int)s.Tipo);
            Voo v = VooDe(p);
            switch (s.Tipo)
            {
                case Estacao.CheckIn: St(Para(p.Mala, Estacao.Bagagem, v, loc) ? Estacao.Bagagem : Estacao.Seguranca).Fila.Add(p); break;
                case Estacao.Bagagem:
                    Emit(Ev.Venda, p.Id, (int)Ganha(T.MalaTaxa));
                    St(Estacao.Seguranca).Fila.Add(p);
                    break;
                case Estacao.Seguranca:
                    St(Para(p.Loja, Estacao.Loja, v, loc) ? Estacao.Loja : Para(p.Fome, Estacao.Alimentacao, v, loc) ? Estacao.Alimentacao : Estacao.Portao).Fila.Add(p);
                    break;
                case Estacao.Loja:
                    Emit(Ev.Venda, p.Id, (int)Ganha((long)Math.Round(T.LojaVenda * (double)Rende(Estacao.Loja) * Gasto(v))));
                    St(Para(p.Fome, Estacao.Alimentacao, v, loc) ? Estacao.Alimentacao : Estacao.Portao).Fila.Add(p);
                    break;
                case Estacao.Alimentacao:
                    Emit(Ev.Venda, p.Id, (int)Ganha((long)Math.Round(T.ComidaVenda * (double)Rende(Estacao.Alimentacao) * Gasto(v))));
                    St(Estacao.Portao).Fila.Add(p);
                    break;
                case Estacao.Portao:
                    if (v == null || v.Embarcados >= v.Total) break;   // so com save adulterado
                    int marco = Marco, tarifa = TarifaDe(v, p);
                    v.Embarcados++;
                    v.Receita += tarifa;
                    Emit(Ev.Embarcou, p.Id, (int)Ganha(tarifa));
                    TotalEmbarcados++;
                    if (Marco > marco)
                    {
                        Emit(Ev.Marco, Marco);
                        for (int e = 0; e < Estacoes.Length; e++) if (T.Abre[e] == Marco) Emit(Ev.EstacaoAberta, e);
                    }
                    if (p.Historia)
                    {
                        const int h = 0;   // ponytail: 1 historia na v0.1; com mais, a proxima ainda fora do album
                        bool nova = (Album & (1 << h)) == 0;
                        Album |= 1 << h;
                        Emit(Ev.Historia, h, p.Id, nova ? 1 : 0);
                    }
                    break;
            }
        }

        /// <summary>Renda de jogo (tarifa, loja, bonus): 2x durante o rewarded 2. A media movel (cofre offline) aprende so a base.</summary>
        long Ganha(long baseCash)
        {
            long g = BoostLeft > 0 ? baseCash * 2 : baseCash;
            Cash += g;
            _ganhoPasso += baseCash;
            return g;
        }

        /// <summary>Portao fecha na hora: quem nao embarcou perde o voo e sai. Custa so a receita dos assentos vazios (invariante da Origem
        /// §50): nenhum Cash sai daqui. Bonus de pontualidade = todos a bordo e o patio pronto (decola na hora). O aviao decola em
        /// Sim.Decolagens quando o patio termina.</summary>
        void Fecha(Voo v)
        {
            FechaAviao(v);
            foreach (Station s in Estacoes)
            {
                s.Fila.RemoveAll(p => p.Onda == v.Onda && p.Voo == v.Indice);
                s.Atendendo.RemoveAll(p => p.Onda == v.Onda && p.Voo == v.Indice);
            }
            v.Partiu = true;
            v.Criados = v.Total;
            TotalPerdidos += v.Perdidos;
            // Fase C2: o despacho com gente aumenta o bonus de quem sai na hora (CONTRATO_FASE_C2 §2)
            double bonus = T.Bonus + (Ativa(Estacao.Despacho) ? T.DespachoBonus * Forca(Estacao.Despacho) : 0) + Efeito(v).Bonus;
            if (v.Perdidos == 0 && v.Total > 0 && NoHorario(v))
                Emit(Ev.Bonus, v.Id, (int)Ganha((long)Math.Round(v.Receita * bonus)));
            Emit(Ev.VooPartiu, v.Id, v.Embarcados, v.Total);
            if (v.Perdidos > 0) Emit(Ev.VooPerdido, v.Id, v.Perdidos, v.Total);
        }

        // ------------------------------------------------------------------ intencoes do jogador

        /// <summary>Toque manual: estacao sem funcionario comeca o atendimento do 1o da fila, no tempo da estacao (o toque nao acelera,
        /// substitui o funcionario). So check-in, seguranca e portao: as paradas opcionais sem funcionario estao fechadas: false.</summary>
        public bool Tap(Estacao e)
        {
            if (!Tocavel(e)) return false;
            Station s = St(e);
            if (Presentes(e) > 0 || s.Atendendo.Count >= s.Postos || !Inicia(s)) return false;
            Emit(Ev.Toque, (int)e);
            return true;
        }

        static bool Tocavel(Estacao e) => e == Estacao.CheckIn || e == Estacao.Seguranca || e == Estacao.Portao;

        /// <summary>O toque comecaria um atendimento agora (a View acende a mao; o bot escolhe onde tocar).</summary>
        public bool PodeTocar(Estacao e)
        {
            if (!Tocavel(e)) return false;
            Station s = St(e);
            if (Presentes(e) > 0 || s.Atendendo.Count >= s.Postos) return false;
            foreach (Pax p in s.Fila) if (e != Estacao.Portao || PodeEmbarcar(VooDe(p))) return true;
            return false;
        }

        /// <summary>TA-2: manda o funcionario para outra estacao. Ele anda Tuning.Caminhada s e, nesse tempo, nao atende em nenhuma das duas
        /// (quem ja estava em atendimento termina). Redirecionar no meio do caminho recomeca a caminhada.</summary>
        public bool Move(int funcionario, Estacao destino)
        {
            if (funcionario < 0 || funcionario >= Staff.Count || (int)destino < 0 || (int)destino >= Estacoes.Length || !Desbloqueada(destino)) return false;
            Funcionario f = Staff[funcionario];
            if (f.Em == destino) return false;
            f.De = f.Em;
            f.Em = destino;
            f.Andando = Math.Max(1, (int)Math.Round(T.Caminhada * Hz));
            Emit(Ev.Movido, f.Id, (int)f.De, (int)destino);
            return true;
        }

        /// <summary>Custo da proxima contratacao da v0.1 (-1 = Ana, Beto e Caio ja contratados). Preco fixo: nada sai de sorteio.</summary>
        public long CustoContratar()
        {
            for (int p = 0; p < MaxFuncionarios; p++) if (!Contratado((Perfil)p)) return CustoContratar((Perfil)p);
            return -1;
        }

        /// <summary>Contrata o proximo da v0.1 (Ana, Beto, Caio): aparece no 1o posto vazio na ordem check-in, seguranca, portao, loja (o
        /// tutorial ensina a atendente do check-in). Deterministico, sem sorteio.</summary>
        public bool Hire()
        {
            for (int p = 0; p < MaxFuncionarios; p++) if (!Contratado((Perfil)p)) return Hire((Perfil)p);
            return false;
        }

        /// <summary>Custo do perfil (-1 = ja contratado, antes do marco dele ou equipe cheia). Preco fixo pelo perfil.</summary>
        public long CustoContratar(Perfil p)
        {
            if ((int)p < 0 || (int)p >= MaxEquipe || Contratado(p) || Staff.Count >= MaxEquipe || Marco < T.PerfilMarco[(int)p]) return -1;
            return T.Contratacao[(int)p];
        }

        /// <summary>Contrata o perfil: aparece no 1o posto vazio (Lugar), como na v0.1. O nucleo nao escolhe a estacao pelo perfil: levar a
        /// pessoa para a especialidade ou para uma estacao nova e' a decisao do jogador (CONTRATO_FASE_C2 §2, BALANCE.md D-C2b).</summary>
        public bool Hire(Perfil p)
        {
            long c = CustoContratar(p);
            if (c < 0 || Cash < c) return false;
            Cash -= c;
            var f = new Funcionario { Id = Staff.Count, Perfil = p, Em = Lugar() };
            f.De = f.Em;
            Staff.Add(f);
            Emit(Ev.Contratado, f.Id, (int)c, (int)f.Em);
            return true;
        }

        public bool Contratado(Perfil p)
        {
            foreach (Funcionario f in Staff) if (f.Perfil == p) return true;
            return false;
        }

        Estacao Lugar()
        {
            foreach (Estacao e in new[] { Estacao.CheckIn, Estacao.Seguranca, Estacao.Portao, Estacao.Loja })
                if (Alocados(e) == 0) return e;
            return Estacao.CheckIn;
        }

        public int Nivel(Estacao e, Melhoria m) => m == Melhoria.Velocidade ? St(e).Vel : St(e).Cap;
        public int NivelMax(Melhoria m) => m == Melhoria.Velocidade ? T.VelNiveis : T.CapNiveis;

        /// <summary>Custo do proximo nivel (-1 = no maximo): Velocidade 40 x 1,6^n e Capacidade 300 x 2,5^n, arredondados a 5.</summary>
        public long Custo(Estacao e, Melhoria m)
        {
            // estacao fechada, so visual ou o despacho (sem upgrade): nada a venda
            if ((int)e < 0 || (int)e >= Estacoes.Length || !Ativa(e) || e == Estacao.Despacho) return -1;
            int n = Nivel(e, m);
            if (n >= NivelMax(m)) return -1;
            double c = m == Melhoria.Velocidade ? T.VelCusto * Math.Pow(T.VelCresce, n) : T.CapCusto * Math.Pow(T.CapCresce, n);
            return 5 * (long)Math.Round(c / 5.0, MidpointRounding.AwayFromZero);
        }

        public bool Buy(Estacao e, Melhoria m)
        {
            long c = Custo(e, m);
            if (c < 0 || Cash < c) return false;
            Cash -= c;
            if (m == Melhoria.Velocidade) St(e).Vel++; else { St(e).Cap++; St(e).Postos++; }
            Emit(Ev.Melhorou, (int)e, (int)m, Nivel(e, m));
            return true;
        }

        /// <summary>Menor custo de algo ainda a venda (contratacao ou upgrade das estacoes abertas); tudo comprado = o mais caro de todos (teto
        /// do cofre). Nao olha a Fase C (tripulacao e patio).</summary>
        public long MaisBaratoTravado()
        {
            long best = CustoContratar(), top = T.Contratacao[MaxFuncionarios - 1];
            for (int p = MaxFuncionarios; p < MaxEquipe; p++)
            {
                long c = CustoContratar((Perfil)p);
                if (c >= 0 && (best < 0 || c < best)) best = c;
                if (Marco >= T.PerfilMarco[p]) top = Math.Max(top, T.Contratacao[p]);
            }
            foreach (Station s in Estacoes)
                foreach (Melhoria m in new[] { Melhoria.Velocidade, Melhoria.Capacidade })
                {
                    if (!Ativa(s.Tipo) || s.Tipo == Estacao.Despacho) continue;
                    long c = Custo(s.Tipo, m);
                    if (c >= 0 && (best < 0 || c < best)) best = c;
                    int n = NivelMax(m) - 1;   // custo do ultimo nivel, para o teto com tudo comprado
                    double u = m == Melhoria.Velocidade ? T.VelCusto * Math.Pow(T.VelCresce, n) : T.CapCusto * Math.Pow(T.CapCresce, n);
                    top = Math.Max(top, 5 * (long)Math.Round(u / 5.0, MidpointRounding.AwayFromZero));
                }
            return best >= 0 ? best : top;
        }

        // ------------------------------------------------------------------ dev (flags -onda e -passageiros; so jogo novo)

        /// <summary>-onda K: pula o relogio para o inicio da onda K (0 = a 1a). So num Sim que ainda nao andou.</summary>
        public void IrParaOnda(int k)
        {
            if (Steps != 0 || k <= 0) return;
            Steps = (long)k * OndaPassos;
        }

        // ------------------------------------------------------------------ eventos e acaso

        void Emit(Ev t, int a = 0, int b = 0, int c = 0)
        {
            AnotaMeta(t, a, b, c);
            Events.Add(new Evento { Tipo = t, A = a, B = b, C = c });
        }

        /// <summary>Uniforme [0,1) puro da semente e das coordenadas (SplitMix64): nada a gravar, retomar no meio da onda repete tudo.</summary>
        double U(int onda, int voo, int i, int sal)
        {
            ulong z = (ulong)(uint)Seed * 0x9E3779B97F4A7C15UL ^ (ulong)(uint)onda << 32 ^ (ulong)(uint)voo << 24 ^ (ulong)(uint)i << 8 ^ (ulong)(uint)sal;
            z += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (z >> 11) * (1.0 / (1UL << 53));
        }
    }
}
