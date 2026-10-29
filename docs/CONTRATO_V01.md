# Contrato da v0.1 (greybox jogável no PC) — 2026-10-29

Fonte da verdade: `GDD.md` §4.2 (o que é a v0.1), §5.4–§5.6 (quadro, equipe, estações), §6.3 (offline), §7.2 (rewarded), §13 (arquitetura), §15.1 (eventos) e §16.0 (portão); `GDD_ORIGEM.md` §46 (MVP), §50 (TA-1 a TA-7 e invariantes), §51 (KPIs) e "Onboarding". Aval: D-01 aprovada em 2026-10-29 (Fases A e B).

Este documento fixa **o que a v0.1 entrega, os números iniciais e o critério de pronto**. Todo número aqui é **hipótese**: o bot mede na Fase B e o `docs/BALANCE.md` registra antes e depois. Mudou regra ou número? Atualize aqui e no `BALANCE.md` no mesmo commit.

A v0.1 é o greybox que prova os diferenciais TA-1 (quadro de partidas) e TA-2 (equipe que se move) com o bot e com gente. Ela é um subconjunto do lançamento (§4.1), não um produto à parte. O que é feito em cada fase está no §9.

## 1. Escopo da v0.1

**Entra (GDD §4.2):** aeroporto Vale Verde; 4 estações (check-in, segurança, loja, portão); quadro de partidas com 2 voos por onda; 3 funcionários móveis; 1 moeda (Cash); upgrades de velocidade e capacidade; terminal que cresce em 3 marcos; offline de 2 h com 2× opcional por anúncio; 2× receita por 5 min por anúncio; tutorial com os passos 1–9 da Origem; 1 passageiro com história (TA-3); save versionado com envelope; bot headless com perfis; diário CSV de playtest; arte greybox procedural.

**Fica fora (volta pelo §4.1, nas Fases C a E):** outros aeroportos, rotas e aviões; estações 5–8 (bagagem, alimentação, manutenção, despacho); os 6 perfis de funcionário (na v0.1 os 3 são generalistas); incidentes (frente fria TA-4); campanha, missões e conquistas; temporada; IAP e Gems; SDK de anúncio real (o LevelPlay depende do nome e do appId, D-03: na v0.1 o anúncio é simulado); interstitial (nenhum); aceleração 2× do tempo; idiomas além do pt-BR (D-08); arte do Tripo (Lote 1 só depois do portão da Fase B, D-14).

**Nunca:** energia, item aleatório pago, anúncio obrigatório, perder voo com o jogo fechado.

## 2. Arquitetura (padrão do Forge Street e do Dungeon Courier)

| Camada | Pasta | Regra |
|---|---|---|
| `TA.Core` | `client/Assets/_TA/Scripts/Core/` | C# puro (`noEngineReferences`), determinístico: RNG próprio com semente (xorshift), passo fixo de 1/30 s, nunca lê o relógio do aparelho (recebe `nowUnix` de fora). Regras, conteúdo (defs com ID estável), save, bot e eventos. API documentada no topo do `Sim.cs`. |
| `TA` (View) | `client/Assets/_TA/Scripts/View/` | MonoBehaviours: câmera, toque e arrasto, HUD por código, arte procedural, som, diário CSV e flags de teste. Só lê o núcleo e manda intenção. |
| `TA.EditorTools` | `client/Assets/_TA/Editor/` | `Setup.Apply`, `Setup.BuildWindows`, `Setup.BuildAndroidDev`, `SpriteImport`. |
| `TA.Tests` | `client/Assets/_TA/Tests/EditMode/` | NUnit; rodam também em `dotnet test client/tools/coretests`. |

**Entrada (contrato núcleo ← View):** a cada quadro a View chama `sim.Tick(dt)` e, quando o jogador age, `sim.Tap(estacao)`, `sim.Move(funcionario, estacao)`, `sim.Buy(upgrade)` ou `sim.Hire()`. O núcleo cuida do resto (relógio do quadro, filas, caminhada do funcionário), para que bot e testes usem as mesmas regras. Enum salvo só cresce no fim; o nome de exibição nunca é chave.

## 3. Regras

1. **Onda:** a cada onda, o quadro mostra 2 voos com destino, horário de fechamento do portão, passageiros e receita prevista. O horário corre em tempo de jogo, no passo fixo do núcleo.
2. **Fluxo do passageiro:** chega → check-in → segurança → loja (opcional) → portão → embarca. Filas FIFO.
3. **Portão fecha na hora:** quem não embarcou até o fechamento perde o voo e sai. **Perder um voo custa só a receita dos assentos vazios daquele voo** (invariante da Origem §50): nada de confiscar Cash, nem penalidade oculta, nem reputação.
4. **Atendimento:** estação com funcionário atende sozinha, um passageiro por posto. Estação sem funcionário só atende por toque: cada toque começa o atendimento do passageiro da frente, no tempo base (o toque não acelera; ele substitui o funcionário). A loja sem funcionário fica fechada e os passageiros passam direto.
5. **Mover a equipe (TA-2):** arrastar o funcionário até outra estação. Ele anda 2,5 s e, nesse tempo, não atende em nenhuma das duas. Alvo de toque ≥ 48 dp, limiar separado para tocar e arrastar, destino válido aceso, botão alternativo "mover para…" (§5.5).
6. **Estado da estação (§5.6):** **crítico** = algum passageiro da fila, no ritmo atual, não chega ao portão antes do fechamento do voo dele; **atenção** = folga menor que 20 s; senão, normal. Crítico aparece por cor, ícone e texto (qual voo está em risco), nunca só por cor.
7. **Passageiro com história (TA-3):** a partir da onda 2, 1 passageiro por onda, num voo escolhido por sequência determinística (Weyl, como o VIP do Forge). Embarcado a tempo, a história entra no álbum. A v0.1 tem 1 história: "Dona Rosa vai ao casamento da neta".
8. **Terminal em 3 marcos:** por passageiros embarcados acumulados. Cada marco aumenta a tarifa e o terminal cresce na tela.
9. **Offline (§6.3):** o relógio do quadro não anda fora do jogo; nenhum voo se perde fechado. Ao voltar, o cofre paga a renda offline (regras no §4).

**Conflito resolvido na Fase B (2026-10-29, `BALANCE.md` D-B3):** o GDD §5.5 diz "começa com 3 funcionários", e o passo 6 do tutorial da Origem diz "contrate a atendente do check-in". Decidido: o jogo começa com 0 e o toque manual; a 1ª contratação é guiada pelo tutorial e custa pouco; as outras duas saem por Cash. O teto da v0.1 é 3. O bot mediu o TA-2 igual nos dois começos (1,259 com 0, 1,254 com 3), e com 0 ele também mede o toque e a contratação do tutorial. Reversível: `Tuning.FuncionariosIniciais`.

## 4. Números iniciais (hipóteses)

| Item | Valor inicial |
|---|---|
| Passo do núcleo | 1/30 s; dt por quadro limitado a 0,25 s |
| Onda | 180 s de jogo: voo 1 fecha aos 120 s, voo 2 aos 170 s, a próxima onda começa 10 s depois; o embarque abre 60 s antes do fechamento |
| Passageiros por voo | 10 na onda 1, +4 por onda, até 36 (assentos do avião regional; era +2 e 30: `BALANCE.md` D-B2) |
| Balcões (postos) da planta | check-in 2, segurança 2, loja 1, portão 1 (mais balcões que funcionários: `BALANCE.md` D-B1) |
| Chegada | espalhada de 0 até (fechamento − 70 s), por sequência determinística |
| Tempo de atendimento (nível 0, s por passageiro) | check-in 4,0 · segurança 5,0 · loja 3,0 · portão 1,5 |
| Loja | 50% dos passageiros param, se ela estiver aberta e o voo deles fechar em mais de 40 s; cada compra rende 6 Cash |
| Tarifa | 8 Cash por passageiro embarcado; +2 por marco do terminal (8 → 10 → 12 → 14) |
| Bônus de pontualidade | +25% da receita do voo com 100% a bordo |
| Contratação | 1º funcionário 20 Cash (tutorial), 2º 150, 3º 400; teto 3 |
| Caminhada ao mover | 2,5 s |
| Upgrade Velocidade (por estação) | 5 níveis, −12% do tempo por nível (× 0,88ⁿ); custo 40 × 1,6^nível, arredondado a 5 |
| Upgrade Capacidade (por estação) | +1 posto por nível, 2 níveis (check-in e segurança 2 → 4, loja e portão 1 → 3); custo 300 × 2,5^nível, arredondado a 5 |
| Marcos do terminal | 150, 600 e 1.500 passageiros embarcados |
| Cofre offline | 25% da renda online medida (média móvel), **teto de 2 h como trava no código** (o Tuning só encurta), teto relativo de 2× o upgrade mais barato ainda travado, Δt ≤ 0 paga 0, cada coleta com id de transação (`ApplyOffline` do Forge) |
| Rewarded 1 | 2× a coleta offline, 1 vez por retorno |
| Rewarded 2 | 2× receita por 5 min de jogo, 3 vezes por dia (dia vira às 04:00 locais, como o `Metas.cs` do Forge), sem acumular |
| Anúncio | simulado na v0.1; nunca durante arrasto, tutorial ou pausa; limites decididos no núcleo |

## 5. Save

- Texto `chave=valor` com `schema=`, dentro do envelope do Forge (`SaveEnvelope`: SHA-256 do conteúdo, schema, versão do jogo, data). Um byte trocado = `Corrupt`, nunca estado novo calado.
- Grava: Cash, funcionários (contratados e onde estão), níveis, marcos, onda e relógio do quadro, filas, cofre (último id de coleta), limites de anúncio e o álbum.
- Grava a cada 5 s de jogo e na pausa. Reabrir retoma a onda do ponto gravado (o relógio do quadro ficou parado).
- Schema futuro → `Sim.Load` recusa, devolve estado novo marcado `SaveRefused`, e a View **não grava** por cima.

## 6. Eventos e diário

O núcleo emite eventos por Tick (a lista limpa a cada Tick). A View usa os eventos para som, efeitos e o diário CSV em `persistentDataPath/diario.csv` (exportado só pelo jogador, sem dado pessoal), com os nomes do GDD §15.1: `tutorial_started`, `tutorial_completed`, `tour_step`, `flight_spawned`, `flight_boarding_started`, `flight_departed` (embarcados/total), `flight_missed`, `queue_length_sampled`, `station_upgraded`, `staff_moved`, `staff_hired`, `story_delivered`, `offline_claim`, `rewarded_ad_offered`, `rewarded_ad_completed`, `reward_granted` e `save_loaded`. O `client/tools/diario_report.py` (cópia do DC) calcula o K2 e o embarque médio.

## 7. Bot e flags

- **Bot** (cópia do `Bot.cs` do Forge, perfis do §6.2 do GDD), determinístico com semente, sem render:
  - **Bom:** a cada 1,0 s de jogo, se uma estação está crítica e outra tem folga, move o funcionário da estação com mais folga; senão, toca a estação sem funcionário que tem fila; compra a contratação ou o upgrade mais barato que couber.
  - **Lento (×0,4):** a mesma política, decidindo a cada 2,5 s.
  - **Parado:** como o Bom, mas nunca move funcionário (a base do TA-2).
- **Flags** (todas, menos `-autoplay`, só com `-testsession`, que não lê nem grava save nem diário e usa a semente 1): `-autoplay N` (N ondas sem render; imprime `AUTOPLAY OK|FAIL` com o embarque médio, o ganho de mover a equipe e o tempo até cada marco; sai 0/1), `-bot`, `-speed N`, `-shot arq -shotdelay s`, `-tela <nome>`, `-onda K`, `-passageiros N` (estresse com 60 na tela, para o K3), `-tutorial`, `-record pasta …`. `-lang`, `-dev-desbloqueia` e `-adtestsuite` entram quando houver idiomas, campanha e SDK de anúncio.

## 8. Critério de pronto da v0.1 (portão da Fase B)

A v0.1 está pronta quando **todas** as linhas abaixo estiverem feitas, com evidência no PR:

| # | Critério | Como se mede |
|---|---|---|
| 1 | **TA-1:** embarque médio por voo ≥ 85% com o bot Bom e ≥ 60% com o Lento | `-autoplay` e `BalanceTests`: 200 sementes × 10 ondas |
| 2 | **TA-2:** a receita do bot Bom é ≥ 1,10 × a do Parado (mesmas sementes) | idem. Se não render, o diferencial é falso e o design volta para revisão antes de qualquer arte (§4.2) |
| 3 | **K2 medido com gente:** ≥ 60% agem (movem alguém ou compram upgrade) na estação crítica em até 60 s | diário CSV de ≥ 5 pessoas, `diario_report.py` (o número mínimo de pessoas é proposta) |
| 4 | Invariantes como testes negativos nomeados: perder voo custa só a receita dele; offline nunca perde voo; nenhum funcionário sai de sorteio; toda concessão tem id de transação; Δt ≤ 0 paga 0; teto de 2 h | `ExploitTests` (cópia do Forge) |
| 5 | Save: ida e volta, envelope corrompido, schema futuro recusado, retomada no meio da onda | `SaveTests` |
| 6 | `client/tools/verify.sh full` verde, com `-autoplay 10` e 0 exceções | saída do `verify.sh` no PR |
| 7 | Fotos 540×960 de cada tela, incluindo uma com 60 passageiros na tela, conferidas à mão | `client/Builds/validation_v01/` |
| 8 | Tutorial passos 1–9 jogáveis, puláveis e desligados no `-testsession` | foto de cada passo com `-tutorial -bot` |

O K3 (p10 de FPS ≥ 30 com 60 passageiros num Android de entrada) é medido no aparelho, por último; ele não trava a v0.1, mas trava a arte do Lote 1.

## 9. O que cada fase entrega

| Fase | Entrega | Estado |
|---|---|---|
| A | este contrato, `CONTRATO_LANCAMENTO.md`, projeto Unity 6000.3.27f1, esqueleto vazio (núcleo, Setup, flags `-autoplay`/`-testsession`/`-speed`/`-shot`) e `verify.sh full` verde | em andamento (PR da Fase A) |
| B | os §3–§8 deste contrato | em andamento: núcleo, bot e testes (PR `feat/fase-b-nucleo`); View greybox a seguir |
