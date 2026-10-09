# TINY AIRPORT EMPIRE
## Game Design Document — Versão de Produto

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
