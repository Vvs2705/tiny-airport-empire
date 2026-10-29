# TINY AIRPORT EMPIRE
## Game Design Document — Versão de Produto

> **GDD de origem (até 2026-10-29 era o `docs/GDD.md`).** A fonte de verdade agora é o GDD mestre [`GDD.md`](GDD.md) (Little Terminal, v1.1), que cita esta página como "Origem §N". Onde os dois divergem, vale o mestre; o que ele não cobre (tutorial, rota de arte, clipes, flags de teste) continua valendo daqui.

> **Revisão 2026-10-09 (comparativo de mercado):** comparativo com 8 idle e tycoons de aeroporto (Airport Empire da HYPERCELL, Idle Airport Empire, Airport Inc., Idle Airport Tycoon, Idle Airplane Inc., Airport Simulator: Plane City, World of Airports, Airport City) e o My Perfect Hotel como referência de fluxo, em [`COMPETITIVO.md`](COMPETITIVO.md). O que mudou, sem renumerar nem apagar nada:
> - **Status:** continua **NO-GO nesta rodada** (veredito de 2026-10-06). Este documento passa a dizer o que o tornaria mais que um reskin do Forge Street se o slot idle validar.
> - **Nome:** "Tiny Airport Empire" não tem app com o nome exato, mas "Airport Empire" tem colisão forte (5+ apps, um com 5M+). Três alternativas checadas no novo **§52**; a recomendada é "Little Terminal".
> - **KPIs numéricos e kill criteria** que faltavam (novo **§51**): D1 ≥ 26%, D7 ≥ 5%, coorte de 1,5–2k installs, CPI ≤ US$0,70, cancelar só depois de 2 iterações.
> - **O core ganha tensão** (§7): "fila legível" é padrão do gênero, e numa cadeia linear o gargalo é sempre óbvio. Propostas: voo com horário de partida e equipe que você move.
> - **Guardrails:** 1 moeda no MVP (§8); gerentes sem sorteio e sem paywall (§10, §11); offline que não degrada satisfação e não se vende (§19, §25); interstitial desligado na v0.1 (§26); booster fora de ranking (§27); MVP em sprites com pooling (§39).
> - **Novo §50 (Diferenciais competitivos)**, com os 3 primeiros: quadro de partidas com horário, equipe que você move e "nada atrás de vídeo nem de sorteio". **Novo §53:** reuso do núcleo do Forge Street.

### 1. VISÃO DO PRODUTO

**Gênero:** Idle / Tycoon / Management

**Plataforma:** Android / iOS

**Orientação:** Portrait

**Modelo:** Free-to-Play

**Sessão:** 3–12 minutos.

**Fantasia:**

Começar administrando uma pequena pista rural e construir progressivamente uma rede global de aeroportos.

O jogador não simplesmente aumenta números.

Ele vê literalmente seu aeroporto se transformar.

> **Revisão 2026-10-09 (nome).** "Tiny Airport Empire" é **nome provisório**. "Airport Empire" colide com pelo menos 5 apps (Idle Airport Empire, 5M+; Airport Empire – Idle Tycoon; Airport Empire Idle; Idle Airport Empire Tycoon; Airport Idle), e "Tiny" lembra Tiny Tower/Pocket Planes, da NimbleBit. Alternativas no §52. appId e nome comercial são irreversíveis depois de publicados e são decisão do Vinicius.

---

# 2. PITCH

Construa pistas, terminais, lojas e hangares; automatize operações; gerencie passageiros e transforme um pequeno aeroporto em um império internacional.

---

# 3. PILARES

## Crescimento visual

Toda melhoria relevante precisa aparecer no mundo.

## Automação

Começamos fazendo tarefas manualmente.

Terminamos comandando sistemas automáticos.

## Otimização

O jogador identifica gargalos.

## Expansão

Cada aeroporto introduz novos desafios.

## Coleção

Aviões, companhias, funcionários e destinos.

---

# 4. CORE LOOP

Avião chega

→

passageiros desembarcam

→

check-in

→

segurança

→

bagagem

→

serviços

→

embarque

→

avião decola

→

receita

→

upgrade

→

capacidade aumenta

→

mais voos.

---

# 5. GAMEPLAY

Inicialmente o jogador interage diretamente.

Exemplo:

tocar para liberar check-in.

Depois contrata funcionário.

O processo torna-se automático.

Com o tempo, o jogo passa de:

**ação manual**

para

**gerenciamento de sistema.**

---

# 6. MAPA

Visão isométrica estilizada.

Aeroporto pode ser dividido em zonas:

pistas;

gates;

terminal;

check-in;

segurança;

bagagem;

lojas;

alimentação;

estacionamento;

hangar;

controle aéreo.

---

# 7. SISTEMA DE FLUXO

O diferencial deverá ser tornar filas visualmente legíveis.

Exemplo:

muitos passageiros no check-in.

Poucos balcões.

Jogador identifica gargalo.

Upgrade:

mais velocidade;

mais funcionários;

mais balcões.

Fila diminui.

Isso gera sensação imediata de eficiência.

> **Revisão 2026-10-09 (tensão do core).** Fila legível é o **padrão** do gênero (My Perfect Hotel, Airport Simulator: Plane City), não diferencial. Numa cadeia linear (check-in → segurança → portão) o gargalo é sempre a estação vermelha, e a decisão é trivial (raia B da validação). Para haver decisão, o upgrade precisa competir com o tempo e com a equipe (detalhe no §50):
> - **quadro de partidas com horário (TA-1):** cada voo fecha o portão numa hora marcada; quem não embarcou fica (receita perdida, sem outra punição). Agora a pergunta é "qual voo eu salvo?", não só "qual estação está vermelha?";
> - **equipe que você move (TA-2):** os funcionários são um grupo limitado que o jogador arrasta entre estações durante a onda; mover agora compete com comprar upgrade;
> - **frente fria (TA-4):** a pista fecha 60 s com aviso; quem adianta o embarque antes ganha.

---

# 8. RECURSOS

### Cash

Moeda principal.

### Gems

Premium.

### Reputation

Desbloqueia estruturas.

### Research Points

Progressão permanente.

### Event Currency

Temporária.

> **Revisão 2026-10-09 (guardrails do veredito).** **No MVP: 1 moeda (Cash).** Reputation, Research Points, Gems e Event Currency entram uma por vez, só depois de existir ralo para cada uma no mapa de fontes e ralos. O offline nunca concede gem nem moeda de evento.

---

# 9. ESTAÇÕES

Cada estação possui:

Level;

Speed;

Capacity;

Revenue;

Staff.

Exemplo:

Check-in Nv.1.

Atendimento:

1 passageiro a cada X segundos.

Upgrade:

Nv.2

Nv.3

...

Marcos especiais:

nível 10;

25;

50;

100.

Esses níveis oferecem grandes bônus.

---

# 10. FUNCIONÁRIOS

Tipos:

atendente;

segurança;

bagageiro;

mecânico;

piloto;

gerente.

Raridades:

Comum;

Raro;

Épico;

Lendário.

Funcionários podem possuir bônus.

Exemplo:

+10% check-in speed.

+20% receita de restaurantes.

Evitar transformar isso em gacha agressivo inicialmente.

> **Revisão 2026-10-09 (ECA Digital).** "Inicialmente" sinaliza gacha no futuro (raia B). Regra: **nunca há sorteio pago** de funcionário ou gerente (Lei 15.211/2025, em vigor desde 17/03/2026; HYPERCELL, Airport City e outro concorrente já têm selo de loot box na App Store). Raridade vira **marco de desbloqueio**: o Raro abre no 2º terminal, o Épico com a 1ª certificação. Todo pacote pago tem conteúdo fixo e visível.

---

# 11. MANAGERS

Managers automatizam e melhoram setores.

Exemplo:

**Lara — Chefe de Terminal**

+15% velocidade.

**Marco — Chefe de Bagagem**

+20% capacidade.

Managers criam:

coleção;

progressão;

eventos;

monetização.

> **Revisão 2026-10-09.** A reclamação mais concreta contra o concorrente que mais cresce (HYPERCELL) é **faltar gerente para quem não paga**: 30–40 vídeos ou US$5 por gerente (`COMPETITIVO.md` c). Regra (TA-5): gerentes têm nome e história curta, **saem por marco de jogo**, e o anúncio só adianta alguns minutos. O "Manager Pack" do §26 vende conveniência (adiantar um gerente que você já ia ganhar), nunca um gerente exclusivo.

---

# 12. AVIÕES

Classes:

regional;

narrow-body;

wide-body;

cargo;

VIP;

especial.

Cada modelo possui:

capacidade;

tempo de turnaround;

receita;

raridade.

---

# 13. COMPANHIAS AÉREAS

Companhias fictícias evitam dependência de licenciamento.

Exemplo:

SkyFox.

BlueWing.

Aurora Air.

SunJet.

Atlas Cargo.

Relacionamento com companhia aumenta.

Quanto maior:

mais voos;

aviões melhores;

contratos especiais.

---

# 14. ROTAS

Destinos desbloqueiam progressivamente.

Cada rota possui:

distância;

demanda;

classe;

receita.

Rotas especiais:

VIP;

cargo;

turismo;

evento.

---

# 15. AEROPORTOS

A progressão macro será baseada em vários aeroportos.

## Aeroporto 1

Campo Verde.

Introduz operações básicas.

## Aeroporto 2

Costa Azul.

Introduz turismo e múltiplos gates.

## Aeroporto 3

Metro City.

Introduz grande volume.

## Aeroporto 4

Deserto Dourado.

Introduz desafios climáticos.

## Aeroporto 5

Ilhas Boreais.

Introduz neve.

## Aeroporto 6+

Novos desafios continuam indefinidamente.

---

# 16. PRESTIGE

Ao atingir determinado estágio, jogador pode:

continuar aeroporto

ou

iniciar expansão global.

Não queremos reset tradicional frustrante.

Usaremos:

**Airport Certification.**

Completar aeroporto oferece permanentemente:

Research Points.

Esses pontos melhoram todos os aeroportos.

> **Revisão 2026-10-09.** Sem reset, a certificação é só mais um tier (raia B). Os Research Points precisam de ralo (a árvore do §17), senão inflam. Proposta (TA-7, §50): ao certificar, o aeroporto antigo **vira hub** e manda passageiros de conexão para o novo, então o progresso viaja (a falta disso é reclamação no Plane City). Prestige com reset só entra se o bot mostrar que o hub não basta. Fica fora do MVP.

---

# 17. RESEARCH TREE

Categorias:

Operações.

Atendimento.

Comércio.

Aviação.

Automação.

Exemplos:

+5% velocidade global.

+10% receita de lojas.

+1 capacidade de fila.

+5% lucro offline.

Research cria progressão permanente.

---

# 18. PASSAGEIROS

Tipos:

regular;

business;

VIP;

família;

turista;

celebridade.

Diferentes tipos possuem necessidades.

VIP:

maior receita.

mais exigência.

Famílias:

grupos.

Turistas:

consomem mais lojas.

---

# 19. SATISFAÇÃO

Passenger Satisfaction:

0–100%.

Afetada por:

filas;

limpeza;

serviços;

alimentação;

tempo.

Satisfação aumenta:

receita;

reputação;

bônus.

> **Revisão 2026-10-09.** A satisfação **não cai com o jogo fechado**: o offline nunca degrada satisfação (raia B). Fica fora do MVP.

---

# 20. SERVIÇOS

Restaurantes.

Cafés.

Duty Free.

Lojas.

Lounges.

Hotel.

Parking.

Esses sistemas aumentam monetização interna e profundidade de gerenciamento.

---

# 21. MISSÕES

Exemplos:

atenda 500 passageiros;

realize 20 voos;

ganhe X dinheiro;

melhore segurança;

atinja satisfação 90%.

Daily Missions.

Weekly Missions.

Career Missions.

---

# 22. EVENTOS

### Rush Hour

Grande volume de passageiros.

### VIP Arrival

Personagem especial.

### Air Show

Evento temporário.

### Cargo Week

Foco em carga.

### Holiday Travel

Fluxo elevado.

### Storm Operations

Eficiência reduzida.

Jogador precisa adaptar aeroporto.

> **Revisão 2026-10-09.** O "Rush Hour" deixa de ser evento e vira o **ritmo normal** do aeroporto, com ondas de voo no quadro de partidas (TA-1). O "Storm Operations" vira a **frente fria** (TA-4): 60 s de pista fechada, com aviso, só com o jogo aberto.

---

# 23. EVENT AIRPORT

Eventos maiores podem usar aeroporto independente.

Exemplo:

**Arctic Expedition Airport**

Duração:

7 dias.

Progressão específica.

Recompensas permanentes.

---

# 24. DAILY LOOP

Login.

Coleta lucro offline.

Gerencia gargalos.

Melhora estações.

Completa voos.

Realiza missões.

Coleta evento.

Investe research.

Sai.

---

# 25. OFFLINE REVENUE

Jogo calcula progresso durante ausência.

Base:

por exemplo, até 2 horas inicialmente.

Melhorias aumentam limite.

Rewarded Ad:

multiplica coleta.

IAP:

Manager pode ampliar.

Evitar transformar offline earning em principal gameplay.

> **Revisão 2026-10-09 (regra do MVP, igual ao cofre do Forge Street, já testada).**
> - ganho = taxa online medida × **25–50%** × min(tempo fora, **2 h**); com upgrades, no máximo 8 h;
> - teto relativo de 2× o upgrade mais barato ainda travado; Δt ≤ 0 paga 0; o cofre abre com ≥ 60 s fora; id de transação por claim;
> - rewarded 2× no claim, **opcional**;
> - **sem "manager" pago que amplia o offline:** o Airport Inc. vende um por US$8,99 e isso é reclamação. A ampliação sai por research/marco;
> - sem backend não há anti-cheat de relógio; aceitamos a trapaça local com teto.

---

# 26. MONETIZAÇÃO

Modelo:

Ads + IAP híbrido.

## Rewarded

2× offline revenue.

boost global.

VIP flight.

instant construction.

extra event reward.

## Interstitial

Poucos.

Preferencialmente em transições naturais.

Nunca durante gerenciamento ativo.

## IAP

Remove Ads.

Starter Pack.

Gems.

Manager Pack.

Airport Pack.

Event Pass.

Season Pass.

> **Revisão 2026-10-09.**
> - **Interstitial desligado na v0.1** (veredito). A proposta TA-5 (§50) é mantê-lo desligado como promessa de marca: os reviews dos concorrentes reclamam de 6–7 anúncios seguidos para um 2× (Airport Inc.) e de anúncio que trava tudo (HYPERCELL).
> - **v0.1 só com rewarded em modo teste, sem IAP** (o modelo de negócio é decisão do Vinicius). Gate de dinheiro da fase B = IAA de rewarded (opt-in, impressões/DAU, ARPDAU). Pagante só a partir de 5k installs.
> - Manager Pack e Airport Pack com conteúdo fixo e visível; nada aleatório pago.

---

# 27. BOOSTERS

Exemplos:

2× Revenue por 10 minutos.

2× Speed.

VIP Rush.

Instant Build.

Podem ser conquistados gratuitamente e comprados.

> **Revisão 2026-10-09.** Booster pago fica **desligado** em qualquer ranking, leaderboard ou desafio comunitário (§33, §37).

---

# 28. COLEÇÃO DE AVIÕES

Aircraft Album.

Objetivo de longo prazo.

Jogador desbloqueia variantes.

Categorias:

Regional.

Classic.

Modern.

Luxury.

Cargo.

Experimental.

Coleções completas concedem pequenos bônus permanentes.

---

# 29. ACHIEVEMENTS

Exemplos:

1 milhão de passageiros.

100 mil voos.

100% satisfação.

primeiro aeroporto concluído.

10 aeroportos.

Achievement Points podem liberar cosméticos.

---

# 30. D1

Jogador:

cria primeira pista;

opera primeiro gate;

abre check-in;

contrata primeiro funcionário;

automatiza primeira estação;

desbloqueia restaurante;

realiza primeiros upgrades.

---

# 31. D7

Jogador:

possui aeroporto muito maior;

começa segunda localidade ou prepara expansão;

usa Research;

participa de eventos;

coleciona aviões/managers.

---

# 32. D30

Jogador:

administra vários aeroportos;

possui progressão global;

participa de Season;

busca coleção;

otimiza aeroportos.

---

# 33. D90

O foco migra para:

coleções;

eventos;

novos aeroportos;

research avançado;

leaderboards opcionais;

season progression.

---

# 34. LIVE OPS

Semanas alternam:

Evento operacional.

Event Airport.

Coleção.

Desafio comunitário.

Temporadas mensais.

Grandes updates trimestrais.

---

# 35. SEASON

Tema exemplo:

**Golden Age of Aviation.**

Recompensas:

skins;

aviões;

managers;

boosters;

gems.

---

# 36. COSMÉTICOS

Terminal themes.

Runway skins.

Vehicle skins.

Aircraft liveries.

Decoração.

Monetização cosmética pode crescer após público estabelecido.

---

# 37. SOCIAL

Versão posterior.

Airline Alliance.

Objetivos cooperativos.

Jogadores contribuem passageiros/voos.

Sem multiplayer em tempo real.

---

# 38. CONTEÚDO DE LANÇAMENTO

3 aeroportos.

15–20 estações/serviços.

20+ aeronaves.

15 managers.

50+ missões.

Daily.

Weekly.

3 eventos.

Research Tree.

Collections.

Economia offline.

Soft launch pode ocorrer com menos conteúdo.

---

# 39. ARTE

3D low-poly estilizado de boa qualidade.

Câmera isométrica.

Personagens pequenos, mas legíveis.

Animações fundamentais:

andar;

fila;

bagagem;

avião pousando;

avião decolando;

veículos.

A grande recompensa visual vem do movimento constante do aeroporto.

> **Revisão 2026-10-09 (custo de arte).** É o tycoon mais caro de arte do portfólio: NPCs em massa e aviões em 3D isométrico (raias B e C). **MVP em sprites pré-renderizados**, pela rota que o Forge Street já usa (Tripo → Mixamo → Blender → folhas de sprite; zero skinning no aparelho), com pooling e caminhos predefinidos. Meta de desempenho: p10 ≥ 30 FPS com 60 passageiros na tela num Android de entrada (K3, §51).

---

# 40. ÁUDIO

Motores.

Chamadas de aeroporto.

Scanner.

Bagagem.

Caixa.

Upgrade.

Avião.

Música ambiente relaxante.

Áudio deve transmitir que o aeroporto está vivo sem ficar irritante.

---

# 41. ARQUITETURA UNITY

GameManager

AirportManager

StationManager

PassengerSystem

QueueSystem

AircraftManager

FlightManager

RouteManager

StaffManager

ManagerCollection

EconomyManager

UpgradeSystem

ResearchManager

MissionManager

EventManager

OfflineProgressManager

SaveManager

AnalyticsManager

AdsManager

IAPManager

RemoteConfigManager

AudioManager.

NPCs não precisam usar NavMesh pesado caso paths predefinidos resolvam o problema.

Performance em aparelhos Android intermediários é prioridade.

> **Revisão 2026-10-09.** Para o MVP vale o padrão do Forge Street (§53): **núcleo em C# puro** (simulação por tick com estações, filas e paciência, testável sem abrir o Unity) + **bot de balance** + **diário de playtest**, e uma View fina por cima. A lista acima fica como mapa de longo prazo.

---

# 42. DATA-DRIVEN DESIGN

AirportData.

StationData.

UpgradeCurve.

AircraftData.

ManagerData.

RouteData.

PassengerData.

EventData.

Isso permite adicionar conteúdo sem reprogramar sistemas.

---

# 43. ANALYTICS

station_upgrade

airport_unlock

aircraft_unlock

flight_complete

queue_length

passenger_lost

satisfaction_change

offline_claim

research_unlock

mission_complete

event_progress

rewarded_watch

iap_purchase.

Telemetria de filas será extremamente importante.

---

# 44. KPIs

Tutorial completion.

D1.

D3.

D7.

D30.

Airport progression speed.

Sessions/day.

Session length.

Rewarded opt-in.

Offline claim rate.

Event participation.

Payer conversion.

ARPDAU.

CPI.

LTV.

> **Revisão 2026-10-09.** Os números, a amostra e os kill criteria estão no **§51**.

---

# 45. MARKETABILITY

Criativos:

“Conserte este aeroporto caótico.”

Fila gigantesca.

Bagagens acumuladas.

Avião esperando.

Jogador melhora gargalo.

Outro:

“Você consegue administrar 1.000 passageiros?”

Outro:

“De uma pista abandonada para aeroporto internacional.”

Crescimento visual será um dos principais elementos de UA.

> **Revisão 2026-10-09 (criativos dos diferenciais, §50).**
> - "O voo das 18h vai sair! Você embarca todo mundo?" (TA-1, família *fail/challenge*);
> - arrastar os funcionários para a segurança e ver a fila sumir (TA-2, família *satisfying*);
> - "Ela vai perder o casamento?" (TA-3, passageiro com história).

---

# 46. ROADMAP

## MVP

Uma pista.

Um avião.

4 estações.

passageiros.

economia.

upgrade.

offline income.

> **Revisão 2026-10-09 (MVP de 2–3 semanas, raia B, só se o slot idle validar):**
> - **Entra:** 1 pista, 1 avião e 4 estações (check-in, segurança, portão, loja); 1 tipo de passageiro, em sprites com pooling; upgrades de velocidade e capacidade e 1 funcionário automatizando cada estação; terminal que cresce em 3 marcos; offline de 2 h; rewarded 2× offline e 2× receita por 5 min; **quadro de partidas com 2 voos e horário (TA-1)**.
> - **Corta:** rotas, companhias, tipos de passageiro, satisfação, research, managers raros, álbum, eventos, event airport, season, alliance e 2º aeroporto.

## Alpha

1 aeroporto inteiro.

missions.

managers.

ads.

IAP.

## Soft Launch

2–3 aeroportos.

research.

events.

analytics completos.

## Global

collections.

season.

live ops.

conteúdo contínuo.

---

# 47. ANO 1

Q1:

lançamento e balanceamento.

Q2:

novos aeroportos e primeiro Event Airport.

Q3:

Alliance.

coleção expandida.

Q4:

sistema de companhias aéreas mais profundo.

eventos globais.

---

# 48. DIFERENCIAL DE LONGO PRAZO

Tiny Airport Empire não deve ser apenas:

“ganhe dinheiro → compre upgrade”.

A profundidade vem da interação entre:

fluxo;

gargalos;

staff;

aviões;

rotas;

companhias;

passageiros;

satisfação;

research;

coleção.

O objetivo é parecer simples nos primeiros minutos e progressivamente revelar um jogo de gerenciamento bastante maior.

> **Revisão 2026-10-09.** A lista acima (fluxo, staff, aviões, rotas, companhias, satisfação, research, coleção) é a mesma dos concorrentes: coleção de aviões e mapa com cidades reais já existem no Airport Inc., no Idle Airplane Inc. e no World of Airports. O que ninguém tem é **decisão sob horário** (TA-1, TA-2) e **nada atrás de vídeo** (TA-5). O §50 trata disso.

---

# 49. VISÃO DE LONGO PRAZO

Após dois anos, poderíamos ter:

20+ aeroportos.

100+ aeronaves.

centenas de upgrades.

dezenas de managers.

eventos sazonais.

alliances.

coleções.

novas mecânicas de operação.

O core não precisa mudar.

Apenas continuamos construindo sobre a mesma fundação.

---

# 50. DIFERENCIAIS COMPETITIVOS (revisão 2026-10-09)

Resumo. O detalhe de cada um (por que, como, custo, risco e validação) e as fontes estão em [`COMPETITIVO.md`](COMPETITIVO.md) d–e. Status de todos: **PROPOSTO**. Custo para o nosso time: P ≤ 1 semana · M 2–4 semanas · G ≥ 1 mês.

| # | Diferencial | O que os similares fazem | Custo | Prova barata |
|---|---|---|---|---|
| TA-1 | **Quadro de partidas com horário:** cada voo fecha o portão numa hora marcada; quem não embarcou fica (receita perdida, sem outra punição). As ondas de voo criam a hora do rush | fluxo contínuo; só o Plane City (mid-core) tem agenda de 24 h | M | bot: ≥ 85% de embarque com jogo bom e ≥ 60% com bot lento; criativo "o voo vai sair!" |
| TA-2 | **Equipe que você move:** um grupo limitado de funcionários que o jogador arrasta entre estações durante a onda | estação vermelha → upgrade (decisão óbvia) | M | ≥ 60% movem alguém na 1ª onda sem tutorial pesado; bot: mover rende ≥ 10% mais que não mover |
| TA-3 | **Passageiro com história:** 1 por voo tem um motivo em uma linha (casamento, show, 1ª viagem); embarcá-lo a tempo entra no álbum "histórias entregues" | nenhum encontrado | P | criativo "ela vai perder o casamento?"; ≥ 30% abrem o álbum |
| TA-4 | **Frente fria:** a pista fecha 60 s com aviso de 30 s; quem adianta o embarque não perde o voo. Só com o jogo aberto | clima só no World of Airports (mid-core) | M | ≥ 50% reagem ao aviso; nenhum review "injusto" no playtest |
| TA-5 | **Nada atrás de vídeo nem de sorteio:** gerentes com nome saem por marco; offline sem anúncio obrigatório e sem "manager" pago; zero interstitial; zero loot box | gerentes pagos, 2× por 6–7 anúncios, selo de loot box | P | página da loja com e sem a promessa; opt-in ≥ 40% mesmo sem obrigação |
| TA-6 | **Rotas do Brasil com escolha:** abrir rota curta e frequente (ex.: Campinas) × longa e rara (ex.: Manaus); cada escolha muda o ritmo das ondas | rota é só desbloqueio | M | bot: nenhuma rota domina; página da loja BR × genérica |
| TA-7 | **O aeroporto antigo vira hub:** ao certificar, ele manda passageiros de conexão para o novo, e o progresso viaja | "o dinheiro não passa de um aeroporto para outro" (Plane City) | G | bot: o 2º aeroporto chega ≥ 20% mais rápido com o hub; sem prestige com reset |

**Ordem recomendada:** TA-1 → TA-2 → TA-5. Depois TA-3 (barato, e é o criativo mais emocional), TA-6 e TA-4. TA-7 só com o 2º aeroporto.

**Invariantes de design** (cada uma vira teste negativo no núcleo):
- Perder um voo nunca custa mais que a receita daquele voo; offline nunca perde voo nem satisfação.
- Nenhum gerente, funcionário ou avião sai de sorteio pago.
- Booster pago não vale em ranking.
- Toda concessão tem id de transação.

# 51. KPIs NUMÉRICOS E KILL CRITERIA (revisão 2026-10-09)

A validação apontou que este GDD não tinha número, estimativa nem critério de corte. Valores do veredito de 2026-10-06 e da raia B. Todos são **HIPÓTESE**: o jogo é **NO-GO nesta rodada** e só entra se o slot idle validar.

**Metas (coorte paga de 1,5–2k installs, BR/PH/ID, comparando pago com pago):**

| Métrica | Continuar | Iterar | Cancelar (só depois de 2 iterações) |
|---|---|---|---|
| D1 | ≥ 26% | 22–26% | < 22% |
| D7 | ≥ 5% | 4–5% | < 4% |
| CPI (Android, BR/PH/ID) | ≤ US$0,70 | 0,70–1,10 | > 2× o alvo em 8 criativos |
| Opt-in de rewarded | ≥ 40% | 30–40% | < 30% |
| Upgrade ou ação na estação-gargalo em até 60 s depois de ela ficar vermelha | ≥ 60% | 45–60% | < 45% |
| Embarque médio por voo (com TA-1) | ≥ 80% | 65–80% | < 65% |
| p10 de FPS com 60 passageiros (Android de entrada) | ≥ 30 | 25–30 | < 25 |

- Pagante só se mede a partir de 5k installs. Antes disso, o gate de dinheiro é IAA de rewarded.
- Amostra: com 1k installs, a margem do D7 é ±1,7 pp; com 2k, ±1,2 pp.
- O CPI-alvo é o mesmo do Forge Street (§15 do GDD 03): os dois disputam o mesmo slot e o mesmo público.

**Kill criteria (raia B, ajustados ao veredito):**
- **K1, retenção:** D1 < 22% ou D7 < 4% (coorte ≥ 1,5k, 2 iterações) → cancelar.
- **K2, diferencial:** < 60% fazem upgrade ou movem equipe na estação-gargalo em até 60 s depois de ela ficar vermelha, mesmo depois de 1 iteração de UI (telemetria `queue_length` + `station_upgrade` + `staff_move`).
- **K3, marketability e performance:** CPI > 2× o alvo em 8 criativos distintos, **ou** p10 < 30 FPS com 60 passageiros na tela num aparelho de entrada.

# 52. NOME: COLISÃO E ALTERNATIVAS (revisão 2026-10-09)

Busca nas lojas e na web em 09/10/2026 (detalhe e links em `COMPETITIVO.md` f). **Busca em loja não prova que a marca está livre:** antes de decidir, consultar INPI, USPTO e EUIPO (classes 9 e 41) e fazer a busca exata nas duas lojas.

| Nome | O que achamos | Veredito |
|---|---|---|
| Tiny Airport Empire (atual) | nenhum app com o nome exato; "Airport Empire" em 5+ apps (um com 5M+); "Tiny" lembra Tiny Tower/Pocket Planes | **colisão forte** para ASO |
| **Little Terminal** (recomendado) | nada na Play nem na web (a busca exata da Play deu erro 500; existem "Airport Terminal" e "Terminal Master") | livre, com ressalva |
| **Jetway Tycoon** | nenhum app com "Jetway" nos resultados visíveis da Play (lista truncada) | livre, com ressalva |
| **Boarding Call** | nada na Play nem no iOS via web; o mais perto é "Boarding Match"; expressão genérica, difícil de registrar como marca | livre / parcial leve |
| Runway Tycoon (reserva) | sem nome exato; existe "Rogue Runways: Idle Jet Tycoon" | colisão parcial |

Por que "Little Terminal": é curto, descreve o jogo, mantém o "pequeno que cresce" do "Tiny" sem colar na NimbleBit e foge do "Empire", que está saturado. O subtítulo de loja carrega a palavra de busca: "Little Terminal: Airport Idle". **Decisão do Vinicius** (appId e nome são irreversíveis depois de publicados).

# 53. REUSO DO NÚCLEO DO FORGE STREET E RELAÇÃO COM O SLOT IDLE (revisão 2026-10-09)

O laço do aeroporto (cliente chega → fila com paciência → estação atende → paga) é o mesmo do Forge Street, com passageiro no lugar de cliente. O Forge (repo `Vvs2705/forge-street`, v0.6, 60 fps no POCO F4) já tem testado:

| Peça do Forge (`FS.Core` e ferramentas) | Uso no aeroporto |
|---|---|
| Simulação por tick com estações, filas, paciência por cliente e métricas de gargalo ("travada"/"fome") | check-in, segurança e portão; a fila legível sai pronta |
| Balcão evolutivo 4 → 8 vagas e pontos de atendimento | balcões de check-in que crescem |
| VIP determinístico (sequência de Weyl) e "Chamar VIP" por anúncio | VIP flight e passageiro com história (TA-3) sem sorteio |
| Encomendas dimensionadas pela renda real (`RateEma`) | missões do §21 que crescem com o aeroporto |
| Bot de balance + `coretests` (~1 s sem Unity) | prova de TA-1, TA-2 e TA-6 antes de qualquer arte |
| Cofre offline (teto de tempo, teto relativo, Δt ≤ 0 = 0, id de transação) | §25 do jeito que está |
| Diário de playtest + `diario_report.py` com PORTÕES | KPIs do §51 no playtest Camada 0 |
| Rota de arte por sprites pré-renderizados (Tripo → Mixamo → Blender), `-record` de criativos 9:16, `medir_aparelho.sh`, LevelPlay com anúncio simulado | o maior custo deste jogo (NPCs em massa) com zero skinning no aparelho |

Estimativa não medida: de 5–7 semanas até o APK (raia C) para 3–4.

**Relação com o slot idle.** O Forge Street está em desenvolvimento e o Underground é a alternativa. Este jogo só entra se o formato idle validar. Com os diferenciais do §50, ele deixa de ser "reskin do Forge" e passa a ser o idle do portfólio com **pressão de horário**, que nem o Forge nem o Underground têm. Se o Forge validar, os criativos de TA-1 e TA-3 podem entrar no mesmo teste de criativos, gravados de um greybox sobre o núcleo do Forge, para medir o tema aeroporto antes de qualquer arte.

---

## Lições do estúdio aplicadas a Tiny Airport Empire (do Dungeon Courier, 2026-10-16)

> **Status: PROPOSTO.** Nada desta seção foi implementado nem testado. O jogo continua **NO-GO nesta rodada** (§51): estas lições valem para quando, e se, ele entrar. "Tiny Airport Empire" é nome provisório (§52).

O Dungeon Courier (DC) chegou à 1.4.1-beta em 2026-10-16 com o GDD inteiro feito, arte v2, tutorial guiado, anúncios e compras integrados e AAB assinado. Cada lição abaixo foi paga lá com tempo, crédito ou retrabalho. A versão completa está no playbook [`LICOES_PARA_O_ESTUDIO.md`](https://github.com/Vvs2705/dungeon-courier/blob/main/docs/LICOES_PARA_O_ESTUDIO.md), com cópia na memória do estúdio (`C:\Users\VINICIUS\Videos\MEUS PROJETOS\Agentes\_memoria\PLAYBOOK_ESTUDIO_DUNGEON_COURIER.md`). Esta seção traduz o playbook para o aeroporto e complementa o §53: o Forge Street dá o laço de fila e a economia, e o DC dá o processo, a rota de arte para NPCs em massa, o tutorial, a interface e o caminho até a loja. Convenção: `dc:` = repo `Vvs2705/dungeon-courier` (pasta `JOGOS NOVOS/06_DUNGEON_COURIER`); `fs:` = repo `Vvs2705/forge-street` (pasta `JOGOS NOVOS/03_FORGE_STREET`). Os clipes Mixamo ficam **fora do git** nos dois jogos: o reuso é cópia do disco, registrada em `docs/PROVENIENCIA.md`. O aval do Vinicius de 2026-10-09 cobre reusar, como cópia, os clipes Mixamo que já estão no disco (`dc:docs/PROVENIENCIA.md` §3).

### Processo
- **Nome antes de tudo o que grava o nome.** **Fazer:** decidir o §52 (com busca no INPI, USPTO e EUIPO) antes do `Setup.cs`. Dependem do nome: o appId `br.com.vstack.<nome>`, o logo, o ícone, o app no LevelPlay, a pasta da chave de upload, a política de privacidade e as capturas e criativos em 4 idiomas. **Por quê:** no DC, cada troca de arte obrigou a refazer loja e criativos (8 capturas × 4 idiomas e 13 vídeos × 4); com o nome, o retrabalho é o mesmo, mais o appId, que não muda depois de publicado.
- **Contrato da v0.1 antes de codar.** **Fazer:** `docs/CONTRATO_V01.md` com o MVP do §46: 1 pista, 1 avião, check-in, segurança, portão e loja; 1 tipo de passageiro em sprites com pooling; velocidade, capacidade e 1 funcionário automatizando cada estação; terminal em 3 marcos; offline de 2 h; rewarded 2× no offline e 2× receita por 5 min; quadro de partidas com 2 voos (TA-1). Também os números iniciais e o critério de pronto (KPIs do §51). Em `docs/CONTRATO_LANCAMENTO.md`, a matriz "§ do GDD × feito". **Por quê:** no DC, o contrato deixou núcleo e View andarem em paralelo sem quebrar a API, e a matriz impediu dar o jogo por pronto antes da hora. **Reaproveitar:** `dc:docs/CONTRATO_V01.md` e `dc:docs/CONTRATO_LANCAMENTO.md` §1.
- **Memória de retomada.** **Fazer:** criar `_memoria/STATUS_tiny-airport-empire.md`, com um bloco "CONTINUAR DAQUI" a cada pausa. **Por quê:** o Vinicius retoma dizendo só "vamos continuar". **Reaproveitar:** `_memoria/STATUS_dungeon-courier-v01.md` como modelo.
- **Fotos antes do PR e um processo pesado por vez.** **Fazer:** fotos 540×960 de cada tela antes do PR, incluindo uma com 60 passageiros na tela (a cena do K3). Unity, Blender e emulador nunca rodam juntos (PC de 7,7 GB). **Por quê:** no DC, as fotos acharam texto sobreposto e um controle tampando o personagem, e nenhum dos 169 testes verdes pegou isso.
- **Arte crua fora do git, com backup.** **Fazer:** zips do Tripo, FBX e `.blend` com modelo pago ficam no **checkout principal** (o worktree some quando é apagado) e num drive privado. Logo, ícones e retratos pintados entram no git. **Por quê:** o repositório é público e a licença dos modelos não permite redistribuir. **Reaproveitar:** `dc:docs/PROVENIENCIA.md`.

### Código e testes
- **Esqueleto do projeto: copiar, não reescrever.** **Fazer:** o laço do aeroporto é o do Forge (cliente chega → fila com paciência → estação atende → paga). O núcleo vem de `fs:client/Assets/_FS/Scripts/Core/`: `Sim.cs` (estações, fila, `Patience` por cliente, balcão 4 → 8, VIP determinístico por Weyl para o TA-3), `Equipe.cs` (worker fixo e nomeado, sem traço sorteado: Lara e Marco do §11, e o TA-5), `Metas.cs` (diárias e semanais do §21, com o dia virando às 04:00 no fuso gravado), `Encomendas.cs` (missões dimensionadas pela renda real), `SaveEnvelope.cs` e `Anuncios.cs`. A casca vem do DC, em `dc:client/Assets/_DC/`: `Editor/Setup.cs`, `Editor/SpriteImport.cs` e, em `Scripts/View/`, `Art.cs`, `Tour.cs`, `Textos.cs`, `Exportar.cs`, `AreaSegura.cs`, `Dp.cs`, `SpriteSheet.cs` e `Joystick.cs` (a lógica de arrastar e de toque rápido serve ao TA-2). Mais `client/tools/coretests` e `viewcheck`. Namespace `TA` e `TA.Core`, com a API documentada no topo do `Sim.cs`. **Por quê:** o DC roda 169 testes em 2–3 min sem abrir o Unity, e o Forge mediu esse laço num POCO F4 (60 fps, 0 crash).
- **Relógio do quadro em tempo de jogo.** **Fazer:** o horário de cada voo (TA-1) e a frente fria (TA-4) correm no passo fixo do núcleo, com semente. O núcleo nunca lê o relógio do aparelho: recebe `nowUnix` de fora, como o `Metas.cs` do Forge. **Por quê:** assim o voo perdido é reproduzível no teste, no bot e no `-record` do criativo "o voo das 18h vai sair!".
- **Bot, perfis e `-autoplay`.** **Fazer:** o `-autoplay N` imprime o embarque médio por voo (TA-1: ≥ 85% no bot bom, ≥ 60% no lento ×0,4), o ganho de mover a equipe (TA-2: ≥ 10%; se não render, o diferencial é falso), a rota que domina (TA-6) e o tempo até cada marco do terminal. **Por quê:** no DC, o bot fez o balance e o portão. No Forge, a pessoa real foi 1,7–3,7× mais lenta que o bot, então a duração se mede com gente (diário). **Reaproveitar:** `fs:client/Assets/_FS/Scripts/Core/Bot.cs` (perfis F2P, Slow, AdWatcher) e `fs:client/tools/econsim`.
- **Invariantes do §50 viram testes negativos nomeados.** **Fazer:** perder um voo nunca custa mais que a receita daquele voo; offline nunca perde voo nem satisfação; nenhum gerente, funcionário ou avião sai de sorteio pago; booster pago não vale em ranking; toda concessão tem id de transação. **Por quê:** regra que mora só em prosa não sobrevive ao código. **Reaproveitar:** `fs:client/Assets/_FS/Tests/EditMode/ExploitTests.cs`.
- **Flags de teste**, só com `-testsession` (não lê nem grava save, semente 1): `-tela <nome>`, `-onda K` (começa na onda de voos K), `-passageiros N` (estresse com 60 na tela, para o p10 de FPS do K3), `-bot`, `-dev-desbloqueia`, `-lang pt|en|es|id`, `-shot arq -shotdelay s`, `-record pasta …`, `-tutorial` e `-adtestsuite`. **Por quê:** no DC, fotos da loja, criativos e QA saíram do jogo real sem tocar no save. **Reaproveitar:** a leitura de argumentos do `dc:Scripts/View/Game.cs`.
- **Diário CSV de playtest.** **Fazer:** eventos `flight_depart` (embarcados/total), `flight_missed`, `staff_move`, `queue_length`, `station_upgrade`, `story_delivered`, `offline_claim` e `tour_step`, sem dado pessoal. O relatório calcula o K2 (≥ 60% agem na estação vermelha em até 60 s) e o embarque médio. **Reaproveitar:** `dc:Scripts/View/Exportar.cs` + `dc:client/tools/diario_report.py`.
- **Textos em 4 idiomas, com tabela.** **Fazer:** as 100+ histórias do TA-3 e todo texto fixo vão em `L(pt, en, es, id)`: frase inteira por idioma, nome e horário por `{0}`, só Latin-1 ("Florianópolis" passa). Escrita com IA e revisão humana, escolhida por sequência determinística e sem repetir na semana. Tipos de passageiro, classes de avião e estações ficam indexados por enum com reserva procedural; enum salvo só cresce no fim. **Reaproveitar:** `dc:Scripts/View/Textos.cs`.
- **Áudio sintetizado.** **Fazer:** a chamada de aeroporto (3 notas), o bipe do scanner, a esteira, a caixa, o upgrade e a música ambiente saem de script. O motor de avião sintetizado é o risco: testar cedo e, se soar pobre, registrar uma alternativa com licença em `docs/PROVENIENCIA.md`. **Reaproveitar:** `dc:client/tools/audio_gen.py`.
- **Portão antes de todo PR.** `dotnet test client/tools/coretests` · `dotnet build client/tools/viewcheck/view` · Unity EditMode · build Windows · `-autoplay 30` com 0 exceções · fotos 540×960. Armadilhas já pagas: NUnit 3.14 no dotnet e 3.5 no Unity; DLLs de `Library/ScriptAssemblies` no viewcheck de worktree novo; `Packages/manifest.json` reescrito pelo Unity ao criar o projeto; `PlayerPrefs` fora de inicializador de campo. **Reaproveitar:** `fs:client/tools/verify.sh`.

### Arte e animação
- **Rota: 3D modelado → sprites isométricos pré-renderizados** (a revisão do §39). **Fazer:** Tripo (imagem grátis → modelo HD → Auto Rig Mixamo) → Blender → `render_sprites.py`, com câmera de isometria (`--elev` a medir: o DC usa 60°), `--dirs 4` ou `8` com `--mirror` e luz chapada com contorno. Zero skinning no aparelho, com pooling. Pista, pátio e grama saem pintados no gerador grátis ou em tiles procedurais. **Por quê:** é a rota que já entregou o elenco do DC e os clientes do Forge. **Reaproveitar:** `dc:client/tools/render_sprites.py`, `render_sprites.ps1`, `folhas.py` e `dc:docs/RENDER_SPRITES.md`.
- **O que gerar no Tripo: Lote 1 do MVP, ≈ 400 créditos (teto ≈ 530).** Créditos só com aval por lote.

  | Peça | Como | Créditos |
  |---|---|---|
  | Passageiro base (adulto, roupa civil) | modelo HD 45 + rig 20 | 65 |
  | 2º corpo de passageiro | só se a fila parecer clonada mesmo com troca de cor | +65 |
  | Funcionário base de uniforme | HD + rig; atendente, segurança e bagageiro saem do mesmo corpo com colete de outra cor | 65 |
  | Lara, Chefe de Terminal (§11) | no MVP, retrato pintado + o corpo do funcionário com outra cor | 0 (+65 com modelo próprio, quando os gerentes entrarem) |
  | Avião regional genérico | sem rig, sem logotipo e sem forma de fabricante real | 45 |
  | Check-in, pórtico do raio-x com esteira, portão, loja | sem rig, 45 cada | 180 |
  | Terminal | 1 modelo; os 3 marcos saem por edição no Blender (alongar, 2º andar) | 45 |
  | Mala de rodinha e mochila | por script no Blender, presas ao osso (regra do Vinicius: item separado do personagem) | 0 |

  Depois do MVP: piloto e mecânico (troca de cor), aviões narrow-body, wide-body e cargo, veículos de pátio e os serviços do §20, a 45 cada. Nunca gerar o passageiro com a mala junto: o Tripo funde item e mão, e o rig arrasta o item errado. Confira o saldo antes e depois de cada geração e registre em `docs/PROVENIENCIA.md`; na 1ª peça de cada sessão, confira a receita do painel (o Tripo já voltou com 8K e 2 M polígonos).
- **Itens presos ao osso.** Mala na mão (`--attach mala.fbx:mixamorig:RightHand:…`), mochila nas costas (`mixamorig:Spine2`, como a `ferreiro_mochila` do Forge), rádio e scanner de mão da segurança. **Reaproveitar:** `fs:client/tools/props_blender.py` e o registro da mochila em `fs:docs/PROVENIENCIA.md`.
- **Clipes reaproveitados:** todos já estão no disco, no esqueleto `mixamorig:` (0 download, 0 crédito). Os do `_blender/` estão em `06_DUNGEON_COURIER/arte/mixamo/_blender/`.

  | Uso | Clipe | Detalhe |
  |---|---|---|
  | Passageiro andando | `03_FORGE_STREET/arte/mixamo/Walking.fbx` ou ARKANA `mixamo-loc-andar-frente.fbx` | In Place; no do ARKANA, o render tira o root motion |
  | Na fila | Forge `Idle.fbx` (Breathing Idle) | janela 199–289, preset `BREATH` |
  | Paciência baixa | Forge `LookingAround.fbx` e uma janela curta do `Angry.fbx` | Standing Angrily: o DC descartou para rugido, mas aqui é exatamente "parado com raiva" |
  | Carregando bagagem | `06_DUNGEON_COURIER/arte/mixamo/Hold.fbx` + o andar | com `--hold`, os clipes Carry* herdam os braços dele |
  | Puxando mala de rodinha | não existe no disco | clipe novo no `dc:client/tools/anim_blender.py`, 0 crédito |
  | Funcionário movido (TA-2) | ARKANA `mixamo-loc-correr-frente.fbx` | corre 2–3 s; o render tira o root motion e grava o `stride_mps` |
  | Segurança no raio-x | `_blender/GuardIdle.fbx` | guarda em pé, laço de 60 quadros |
  | Atendente entregando o cartão de embarque | `_blender/Grab.fbx` | 0,5 s |
  | Bagageiro jogando a mala na esteira | `_blender/KoboldToss.fbx` | arremesso por baixo; soltura no quadro 7 de 12 |
  | Lara avisando da frente fria (TA-4) | `_blender/Call.fbx` | mão na boca |
  | Embarcou a tempo, história entregue (TA-3) | Forge `Cheering.fbx`, `Thankful.fbx` | — |
  | Tchau no portão | Forge `Waving.fbx` | — |
  | Perdeu o voo | `_blender/Shrug.fbx` | dá de ombros de frente: a punição leve do TA-1 fica legível sem texto |

  O que faltar se anima no Blender, por keyframe em script e com retarget por delta de rotação sobre o repouso. A origem e o SHA de cada clipe estão em `dc:docs/PROVENIENCIA.md` §3.
- **Aviões sem esqueleto.** **Fazer:** pouso, táxi e decolagem por caminho e tween no Unity, a partir de 8 direções renderizadas do modelo parado. Hélice e trem de pouso são peças separadas, animadas no Blender por script. **Por quê:** no DC, os 7 clipes do dragão saíram por keyframe em script, sem Mixamo. **Reaproveitar:** `dc:client/tools/dragao_blender.py` (o método) e `anim_blender.py`.
- **No Blender, sem crédito.** **Fazer:** por troca de cor:
  - as liveries das companhias fictícias do §13 (SkyFox, BlueWing, Aurora Air, SunJet, Atlas Cargo);
  - as variantes do álbum do §28;
  - os uniformes por função;
  - dezenas de combinações de roupa de passageiro a partir de 1 corpo;
  - os temas de terminal do §36.

  Com luz chapada, a troca é feita direto na folha renderizada. Também: `.blend` salvo depois do 1º import (o FBX do Tripo leva mais de 10 min) e `timeout` em todo render. **Por quê:** assim o álbum e os cosméticos custam 0 crédito de arte. **Reaproveitar:** `dc:client/tools/roupas_cor.py`, `roupas_2d.py` e `roupas_courier.py`.
- **Nitidez e memória.** **Fazer:** os personagens são "pequenos, mas legíveis" (§39): use `--hd K` e `--escala E`, com K = min(1,5; 4096 / (quadros × célula)), e um teto de quadros por clipe (`--max-frames`). Meça o PSS no aparelho com 60 passageiros. Hipótese não testada no DC: trocar a cor da roupa em tempo real (tinta por máscara) em vez de gravar uma folha por variante, para economizar APK e memória. **Por quê:** com as folhas HD, o APK do DC foi de 67,7 para 83 MiB, e aqui há variantes em massa. **Reaproveitar:** `dc:client/tools/elenco_hd.sh`.
- **Arte de interface pintada no gerador grátis.** **Fazer:** sobre azul-marinho liso e recortados por script: logo (com o nome decidido); ícones das estações, avião, relógio, mala e história; retratos de Lara e Marco (§11); 10–20 rostos dos passageiros com história (TA-3) num lote só; fundo do menu. **Por quê:** no DC, o retrato pintado ficou muito mais vistoso que o render 3D recortado. **Reaproveitar:** `dc:client/tools/ui_assets.py`.

### Interface e marca
- **O quadro de partidas (TA-1) é a peça central da UI.** **Fazer:** um painel amarelo sobre preto, gerado por código em 9 fatias (`Moldura` + `Letreiro`), com o horário numa fonte mono OFL (ex.: Space Mono) e o resto em Lilita One e Nunito ExtraBold, como no DC. Também: `Balao` para a fala do passageiro com história, `Contorno` no tutorial e `Aperta` (escala 0,95 no toque) em todo botão. **Reaproveitar:** `dc:Scripts/View/Art.cs`, com as fontes e a licença OFL em `Resources/Fonts`.
- **Alvo de toque maior que o boneco.** **Fazer:** o funcionário que se arrasta (TA-2) tem alvo de pelo menos 48 dp em volta do sprite (`Dp`), arrastar e tocar têm limiar separado, e o boneco arrastado aparece acima do dedo, sem tampar a estação de destino. **Por quê:** no DC, o joystick desenhado no dedo tampava o courier, e a correção foi o mesmo controle sem desenho.
- **Retrato e 20:9.** CanvasScaler com referência 1080×1920 casando a largura e `AreaSegura` para o notch. Nas telas 20:9, o pátio e a grama, escurecidos, continuam até a borda: faixa lisa parece bug.
- **Ícone adaptativo.** Fundo + frente, com o avião ou a torre a no máximo 0,64 da camada, conferido nas máscaras círculo, squircle e quadrado; o ícone da loja (512) é o mesmo. **Reaproveitar:** `dc:client/tools/icone_app.py` e `Setup.Icones`.

### Onboarding (tutorial guiado)
A tela congela e só o alvo fica aceso (véu com buraco, contorno dourado e seta). Cada passo aparece uma vez, com "Pular tutorial" e "REVER TUTORIAL" nas configurações, e fica desligado no `-testsession`. Os gatilhos leem o estado do núcleo, e passo de botão se cancela se a tela mudar (no DC, um passo preso sem alvo congelava o jogo). **Reaproveitar:** `dc:Scripts/View/Tour.cs`; troque só a lista de passos e os textos. O campo `Arrastar` já mostra o dedo arrastando, que é o gesto do TA-2, e o `Fala` põe retrato e balão.

**Cartão de contexto no lugar de cutscene.** Antes do 1º toque, aparece o retrato pintado da **Lara, Chefe de Terminal** (o GDD já tem nome e cargo para ela, §11), com o balão: *"Este aeroclube estava abandonado, mas hoje sai o primeiro voo: 18h, para Campinas. Tem gente que não pode perder esse avião. Vamos abrir o check-in?"* No 1º voo, um passageiro tem história (TA-3, PROPOSTO: "Dona Rosa vai ao casamento da neta"). O ciclo fecha na 1ª decolagem, com a Lara de novo: *"Todo mundo a bordo! Amanhã a gente cresce."* **Por quê:** no DC, uma cutscene atrasaria o 1º toque e somaria com o tutorial; o cartão do cliente resolveu com uma fala e um retrato, e a cena de entrega fechou o ciclo.

| # | Passo (tipo) | Quando | Por quê |
|---|---|---|---|
| 1 | Cartão da Lara (explicação) | 1ª abertura | contexto e o horário do 1º voo numa frase |
| 2 | "Toque no check-in" (ação) | 1º passageiro na fila | o verbo manual do §5, antes de qualquer escolha |
| 3 | "Toque na segurança" (ação: raio-x) | passageiro no raio-x | a cadeia, uma estação por vez |
| 4 | "Este é o quadro de partidas" (explicação: o painel) | quando o 1º passageiro passa da segurança | a regra que cria a decisão (TA-1): o portão fecha na hora, e quem fica custa só a receita do assento |
| 5 | "Embarque no portão" (ação) → 1ª decolagem e 1ª receita | o passageiro chega ao portão | fecha o 1º ciclo no 1º minuto |
| 6 | "Contrate a atendente do check-in" (ação) | 1º Cash suficiente | pilar Automação |
| 7 | "Arraste a funcionária da loja para a segurança" (ação, dedo arrastando) | 1ª onda com a segurança vermelha | ensinar o verbo do TA-2 antes de "qual voo eu salvo?"; é o que o K2 mede |
| 8 | "Vermelho é gargalo: mover ou melhorar" (explicação) | logo depois do 7 | as duas respostas existem e competem (§7 revisado) |
| 9 | "Melhore a velocidade da segurança" (ação) | Cash suficiente | ensina o menu de upgrade |
| 10 | "Este passageiro tem uma história" (explicação: balão do TA-3) | 1º passageiro com história depois do tutorial | o álbum: embarcá-lo a tempo guarda a história |
| 11 | "O terminal cresceu" (explicação) | 1º marco do terminal | pilar Crescimento visual (§3) |
| 12 | "O aeroporto rendeu enquanto você estava fora" (explicação: cofre) | 1ª volta com ≥ 60 s fora | teto de 2 h, nenhum voo perdido no offline, 2× opcional |

- **Ritmo:** os passos 1–5 cabem no 1º minuto; os outros nascem dos gatilhos, espaçados, para não virar parede de texto. Se a frente fria (TA-4) entrar, ganha um passo no 1º aviso.
- **Controle:** sem joystick. O gesto central é arrastar a equipe, o mesmo desenho do "arrastar com a carga → soltar" do DC, sem nada desenhado no dedo.

### Monetização e compliance
- **1 moeda (Cash) na v0.1** (§8 já revisado). O offline nunca dá gem nem moeda de evento.
- **Nada aleatório pago.** Gerentes por marco (§10, §11, TA-5) e pacotes de conteúdo fixo (§26) já estão alinhados. Falta alinhar aviões e temporada (ver os conflitos).
- **Rewarded sempre opcional:** 2× offline, 2× receita por 5 min e "VIP flight" com resultado fixo, como o "Chamar VIP" do Forge.
- **Anúncio não personalizado para todos, sem `AD_ID`.** `LevelPlayPrivacySettings` com GDPR sem consentimento, CCPA opt-out e COPPA desligado, antes do `Init`; `Setup.SemIdDePublicidade` (pós-geração do Gradle) tira o `AD_ID` e as 3 permissões do Privacy Sandbox do manifesto; na Play, a declaração de ID de publicidade é **não**. O app do jogo nasce na conta **V-STACK** do LevelPlay (o Forge tinha ficado em outra conta). **Reaproveitar:** `dc:Scripts/View/Anuncios.cs`, `AnunciosLevelPlay.cs` e `dc:docs/MONETIZACAO.md` §4.
- **Interstitial desligado na v0.1** (§26). Se entrar, a regra de quando mostrar mora no núcleo, nunca no SDK (no DC: `sim.InterstitialDue`).
- **Offline com teto de 2 h como trava no código.** O Tuning só encurta (como o `Balance.OfflineCapSeconds` do Forge). Também valem o teto relativo, Δt ≤ 0 pagando 0 e um id de transação por claim. **Reaproveitar:** `fs:Scripts/Core/Sim.cs` (`ApplyOffline`).
- **IAP, só quando o Vinicius aprovar.** Unity IAP 5.4.3 atrás de uma interface, com loja falsa no PC; compra em 2 passos (o núcleo concede e só então confirma); restaurar compras. **Reaproveitar:** `dc:Scripts/View/Loja.cs` e `LojaIap.cs`.
- **Licença e privacidade.** Companhias fictícias (§13) e aviões genéricos, sem forma nem pintura de fabricante ou companhia real (prompt do Tripo registrado em `docs/PROVENIENCIA.md`). A telemetria de filas do §43 tem só eventos de jogo, sem dado pessoal. A política de privacidade fica pública no GitHub Pages (4 idiomas) e cita exatamente as redes usadas.

### Play e lançamento
- **Conta de organização.** A Play Console da V-STACK espera o D-U-N-S grátis pedido no DC (previsão 2026-11-25), e a mesma conta publica este jogo. Não abra conta pessoal. A lição do DC é que a burocracia de conta leva ~30 dias úteis: começa no início do projeto, nunca no fim.
- **Chave de upload fora do git**, com backup fora do PC: `%USERPROFILE%/.<nome>/upload.jks` + `upload.properties`, como o DC faz em `.dungeoncourier/`.
- **AAB assinado** por `Setup.BuildAndroidBundle`. No DC, isso foi conferido: targetSdk 36, bibliotecas nativas alinhadas a 16 KB e Google Play Billing 9.
- **Loja e criativos por script, do jogo real:** capturas × 4 idiomas do build de release, ícone 512, feature graphic e os vídeos 9:16 do §45 ("o voo das 18h vai sair!", arrastar a equipe e ver a fila sumir, "ela vai perder o casamento?"), gravados com `-record`. O "conserte este aeroporto caótico" só vale se o caos existir no build (a 1ª onda com a segurança vermelha): a regra do estúdio é criativo só de gameplay real. **Reaproveitar:** `dc:client/tools/loja_fotos.sh`, `loja_assets.py`, `criativos.py`, `dc:docs/LOJA.md` e `CRIATIVOS.md`.
- **Pré-release no GitHub** a cada versão, com APK, AAB, zip Windows e o pacote da loja.

### Android
- **Taxa de quadros:** `QualitySettings.vSyncCount = 0` e `Application.targetFrameRate = 60` logo no início (no DC, o POCO F4 foi de 28–36 para 55 fps). Com 60 passageiros, a meta do K3 é p10 ≥ 30 FPS num aparelho **de entrada**: o POCO de 4 anos se limita depois de 1 min e não serve de régua. Se a temperatura subir, um teto de 30 fps é decisão medida, não chute. **Reaproveitar:** `fs:client/tools/medir_aparelho.sh`.
- **APK de worktree:** gere via `subst R: <client>`, porque caminho longo com espaço quebra o CMake do Gradle. O worktree vai em caminho curto, nunca no scratchpad (estoura o MAX_PATH).
- **adb no Git Bash:** `adb shell "am start -n <pkg>/<act> -e unity '-a -b'"`, com as aspas por dentro.
- **Aparelho:** mantenha só o APK atual. Pergunte ao Vinicius antes de usar o celular dele e fotografe só com o jogo em primeiro plano.

### Conflitos com o GDD atual
Os conflitos ficam só apontados aqui; as outras seções do GDD não foram editadas. Todos pedem decisão do Vinicius.
1. **§25, "melhorias aumentam limite" e "com upgrades, no máximo 8 h", contra o guardrail de 2 h.** **Recomendação:** 2 h como trava no código na v0.1. O research do §17 (+5% lucro offline) sobe a **taxa**, não o teto.
2. **§26 (interstitial "poucos, em transições naturais") contra o TA-5 ("zero interstitial e zero banner") e a v0.1 sem interstitial.** **Recomendação:** decidir se o TA-5 é promessa pública (vai na página da loja e não volta atrás) antes de escrever a loja. Se o interstitial entrar, que seja como no DC: um ponto só, nunca nos 3 primeiros do dia, no máximo 1 a cada 4 min, decidido pelo núcleo, sumindo com Remove Ads.
3. **§12 (avião com "raridade"), §28 ("jogador desbloqueia variantes"), §35 (temporada com aviões, managers e gems) e §26 (Gems à venda).** Juntos, abrem caminho para avião raro por gem comprada, que é aleatório pago e segunda moeda premium. **Recomendação:** avião e variante saem por marco, rota ou companhia, ou em pacote de conteúdo fixo; o premium da temporada fica em cosmético e conteúdo listado; gems só com IAP aprovado e com ralo.
4. **§30 (D1 com "desbloqueia restaurante") contra o §46 (MVP com loja, sem restaurante).** O tutorial acima segue o MVP. **Recomendação:** alinhar o §30 no contrato da v0.1.
5. **§45 ("conserte este aeroporto caótico") contra o guardrail "criativos gravados do jogo real".** **Recomendação:** o caos tem que existir no build (onda forte sem equipe movida) e ser gravado com `-record`; nada de cena montada que o jogo não entrega.
6. **§39 ("personagens pequenos, mas legíveis") contra o alvo de 48 dp do TA-2.** **Recomendação:** alvo de toque invisível maior que o sprite e o boneco arrastado acima do dedo (ver Interface).
7. **§52 (nome em aberto) contra tudo o que grava o nome.** **Recomendação:** decidir o nome antes do `Setup.cs` e de qualquer arte de marca.

### Primeiros passos recomendados
1. Enquanto o jogo é NO-GO: nada de projeto Unity, arte ou crédito. Só as decisões baratas: o nome (§52, com busca de marca) e os conflitos 1–3.
2. Se o slot idle validar e o Vinicius pedir o projeto neste grupo: escrever `docs/CONTRATO_V01.md` e a matriz `docs/CONTRATO_LANCAMENTO.md`, e abrir `_memoria/STATUS_tiny-airport-empire.md`.
3. Montar o esqueleto (núcleo do Forge + casca do DC, namespace `TA`) com o portão verde ainda vazio.
4. Fazer o greybox procedural sobre o núcleo do Forge e provar, só com o bot, o TA-1 (≥ 85% / ≥ 60% de embarque) e o TA-2 (≥ 10% de ganho), antes de qualquer arte.
5. Pôr o tutorial (passos 1–9), o cartão da Lara e o diário no greybox, e medir o K2 com gente.
6. Gravar os criativos do TA-1 e do TA-3 do greybox com `-record`, para entrar no mesmo teste de criativos do slot idle (§53).
7. Só com o jogo validado: Lote 1 de arte (≈ 400–530 créditos, com aval), UI pintada grátis (retratos de Lara e Marco), ícone adaptativo, appId e chave de upload com backup.
