# Contrato da Fase C: tripulação, pátio e manutenção no núcleo (2026-10-29)

Fonte da verdade: `GDD.md` §5.4 (perder voo custa só a receita dele), §5.8 (reparo só com Cash), §5.11 (tripulação), §5.12 (pátio e manutenção), §5.13 (portão de design), §14.2 (migração), §15.1 (eventos) e §17.2 (Fase C). Aval: D-19 (recomendação "sim, na Fase C, com o portão do §5.13") e o prompt mestre do estúdio.

Este documento fixa **as regras, os números e o critério de pronto** da tripulação, do pátio e da manutenção no `TA.Core`. Todo número é **hipótese**: o bot mede e o `docs/BALANCE.md` §5 registra o antes e o depois.

**Medido em 2026-10-29 (`BALANCE.md` §5):** o portão do §8 passou com 1,151 (1,114 depois da Fase C2, `BALANCE.md` §6). A manutenção mudou de número (D-C1: desgaste 15, checagem −10 por 10 Cash, inspeção de 110 s). A tripulação não gerou decisão sozinha (1,02) e ficou **só visual** (`Tuning.RegraTripulacao = false`, D-C2): as regras do §2 estão no núcleo e testadas com a chave ligada, mas a falta de tripulação não segura o embarque. O pátio começa com 1 pessoa (D-C3). Mudou regra ou número? Atualize aqui e no `BALANCE.md` no mesmo commit. A tela (View) liga esses sistemas na próxima frente; aqui é só o núcleo, os testes e o bot.

## 1. O ciclo do avião (a peça que liga os três sistemas)

Cada voo da onda tem o seu avião (voo 1 → avião 0, voo 2 → avião 1; Vale Verde tem 2 regionais, sem reserva). O ciclo, em tempo de jogo e no passo fixo:

1. **Pousa e encosta no pátio** `PatioAntes` s antes do fechamento do portão (voo 1 aos 20 s da onda, voo 2 aos 70 s).
2. **Turnaround:** a equipe de pátio faz os serviços (§3). A escala automática põe a tripulação a bordo (§2).
3. **Embarque:** abre na hora do quadro (60 s antes do fechamento) **só com o avião liberado**: desembarque e limpeza feitos, tripulação completa a bordo e manutenção em dia (§4). Se não estiver liberado, o embarque espera e o quadro avisa.
4. **Portão fecha na hora**, como na v0.1: quem não embarcou perde o voo. É o único jeito de um sistema novo custar dinheiro, e custa **só a receita dos assentos vazios daquele voo**. Nenhum Cash sai do saldo.
5. **Decola quando embarcou e o pátio terminou** (combustível e bagagem). Pátio atrasado = decolagem atrasada: o voo perde o bônus de pontualidade e o avião volta mais tarde.
6. **Voa o trecho** (`Trecho` s) e volta para o voo da onda seguinte. Atraso na volta atrasa o próximo turnaround.

Voo cujo embarque nunca abriu (sem tripulação, manutenção pendente ou avião no hangar) não decola: o avião fica no pátio, sem trecho e sem desgaste, pronto para o próximo voo.

## 2. Tripulação (§5.11)

| Regra | Hipótese |
|---|---|
| Por voo (regional) | 1 piloto + 1 comissário (`Tuning.TripPorVoo`; narrow 2+2 e wide 2+4 entram com as aeronaves) |
| Começo | 3 pilotos e 3 comissários (uma tripulação de reserva) |
| Contratação | por Cash, preço fixo pela ordem; teto por função sobe com os marcos do terminal (3 → 4 → 5 → 6). Fixos e nomeados (nome pela ordem, `Tripulacao.Nomes`), sem sorteio nem pacote pago |
| Escala automática | com o avião no pátio, cada vaga vazia pega **o primeiro livre na ordem do quadro da tripulação** (ordem de contratação) |
| Troca manual | `sim.Escala(voo, tripulante)`: põe um tripulante livre no voo, no lugar do da mesma função (que volta para a sala). Só com o avião no pátio e antes de o embarque abrir |
| Trecho | conta na decolagem; a tripulação volta com o avião |
| Descanso | quem está na sala há `DescansoSeg` (180 s) está descansado (trechos voltam a 0). Depois de `TrechosAteDescanso` (3) trechos, o tripulante **só volta a voar** depois desse descanso |
| Falta de tripulação | com a regra ligada, o embarque espera; o voo avisa (`crew_missing`) assim que a vaga não pode ser preenchida. **Padrão: regra desligada (só visual), D-C2** |
| Papéis | piloto não vira atendente e vice-versa; a tripulação não troca uniforme |

A decisão do jogador seria a escala: quem voa agora e quem fica na sala para descansar antes de bater o limite. O bot mediu que a escala automática quase não erra com 3 + 3 para 2 voos por onda (1,02), então a regra volta quando a frota e os voos crescerem.

## 3. Pátio (§5.12)

| Regra | Hipótese |
|---|---|
| Serviços do turnaround | desembarque 12 s, limpeza e catering 18 s, combustível 20 s, bagagem 24 s (`Tuning.ServicoSeg`); com 1 pessoa, a bagagem do voo 1 espera o voo 2 e ele decola 4 s atrasado (D-C3) |
| Ordem | limpeza e bagagem depois do desembarque; combustível a qualquer hora |
| Antes do embarque | desembarque e limpeza |
| Antes da decolagem | os quatro |
| Equipe de pátio | cada pessoa faz um serviço por vez; começa com 1; contratar 40 e 150 Cash; teto 3 |
| Veículos | 1 de cada serviço; cada serviço usa um veículo do seu tipo |
| Upgrades por serviço | Velocidade: −12% do tempo por nível, 3 níveis, 30 × 1,6^nível; Capacidade: +1 veículo, 1 nível, 120 Cash |
| Prioridade | primeiro os serviços antes do embarque, por ordem de chegada dos aviões; depois os outros |

## 4. Manutenção (§5.12)

| Regra | Hipótese |
|---|---|
| Desgaste | +15 por trecho, de 0 a 100 (era 10: D-C1) |
| Aviso | a partir de 80 (`maintenance_warning`) |
| Pendente | em 100 (`maintenance_due`): o embarque não abre até a checagem |
| Mecânico | 1, na oficina; faz uma checagem por vez |
| Checagem rápida | `sim.Checagem(aviao)`: no pátio, antes de o embarque abrir, 1 por turnaround; 8 s, −10 de desgaste, 10 Cash (era −20 por 15: com ela empatando ou passando o desgaste, o hangar nunca seria usado) |
| Inspeção obrigatória | se o avião chega pendente, o mecânico a faz sozinho (padrão): 110 s, −50 de desgaste, sem Cash (custa tempo). Os 110 s passam dos 100 s de pátio: o voo daquele avião não embarca (era 60 s: com o portão rápido, ficar pendente não custava nada, D-C1) |
| Revisão no hangar | `sim.Hangar(aviao)`: no pátio, antes de o embarque abrir; 100 Cash; o avião sai por 180 s (uma onda) e volta com desgaste 0, limpo e abastecido. O voo dele daquela onda não embarca (custa só a receita dele) |
| Justiça | reparo e revisão só com Cash e tempo, nunca com anúncio; nenhum avião é destruído nem perdido; desgaste não anda com o jogo fechado |

## 5. Núcleo e API

- Determinístico, passo fixo de 1/30 s, sem sorteio nos sistemas novos (a semente continua só nas chegadas e na loja).
- A View só manda intenções novas e lê o estado (cabeçalho do `Sim.cs`): `HireTripulante(funcao)`, `Escala(voo, tripulante)`, `HirePatio()`, `BuyPatio(servico, melhoria)`, `Checagem(aviao)`, `Hangar(aviao)`; leitura em `sim.Avioes`, `sim.Tripulacao`, `sim.EquipePatio`, `sim.Pronto(aviao)` e nos custos.
- Eventos com os nomes do GDD §15.1 mais os novos: `crew_hired`, `crew_assigned`, `crew_missing`, `crew_rest_started`, `aircraft_landed`, `ground_service_done`, `turnaround_done`, `flight_takeoff`, `ground_staff_hired`, `ground_vehicle_upgraded`, `maintenance_warning`, `maintenance_due`, `maintenance_check_started`, `maintenance_check_done`, `hangar_started`, `hangar_done`. `flight_departed` continua sendo o portão que fecha.
- O teto do cofre offline continua olhando só a contratação e os upgrades do terminal.

## 6. Save

- Schema 2. O save da v0.1 (schema 1) abre com tudo o que tinha (Cash, relógio, filas, equipe, voos, cofre, anúncios) e ganha os sistemas novos no padrão: os dois aviões no pátio, prontos, com desgaste 0, a tripulação inicial e 1 pessoa de pátio. A escala automática põe a tripulação a bordo no passo seguinte, então o voo que estava na fila embarca como antes.
- Grava aviões (desgaste, onde está, serviços, checagem, hangar), tripulação (trechos, tempo na sala, a bordo de qual avião), equipe e veículos de pátio e o mecânico.
- Ida e volta continua estável (`Load(Save(x)).Save == Save(x)`); lixo vira padrão limitado; schema futuro continua recusado.

## 7. Invariantes (testes negativos nomeados no `ExploitTests`)

1. Falta de tripulação nunca custa mais que a receita do voo, e nenhum Cash sai do saldo por isso.
2. Manutenção pendente nunca custa mais que a receita do voo, e nenhum Cash sai do saldo por isso.
3. Offline nunca perde voo nem gasta desgaste, trecho ou descanso.
4. Nenhum tripulante sai de sorteio: mesma ordem, nome e preço em qualquer semente.
5. Reparo e revisão só com Cash e tempo: sem Cash não acontece, o 2× do anúncio não acelera, e nenhum ID de anúncio paga reparo.

**Prova de mutação:** tirar cada trava acima do código tem de derrubar pelo menos um teste (registro no `BALANCE.md`).

## 8. Bot e portão de design (§5.13)

- **Planejador:** o Bom de sempre que também escala (põe no voo o tripulante mais descansado e deixa na sala quem precisa descansar), faz a checagem rápida quando ela cabe antes do embarque e manda ao hangar na hora certa: o avião que pousa pendente vai ao hangar (zera) no lugar da inspeção (tira metade).
- **Automático:** o mesmo Bom, mas deixa a escala, a checagem e o hangar no padrão.
- Os dois compram igual: o que o Bom já comprava, mais a próxima pessoa de pátio quando o pátio atrasou um embarque ou uma decolagem.
- **Portão:** receita do Planejador ≥ 1,10 × a do Automático, com as mesmas sementes (200 × 20 ondas: o ciclo da manutenção não cabe em 10). **Medido: 1,151** (só a manutenção 1,151; só a tripulação 1,020; as duas 1,180). Se não render, o `BALANCE.md` registra, os números se ajustam e, se ainda não der, o sistema que não gerou decisão fica só visual (regra desligada no `Tuning`), como manda o GDD.
- **TA-1 e TA-2 não pioram:** a tabela do Bom, do Lento e do Parado de antes e de depois vai para o `BALANCE.md`. **Medido:** Bom 99,2 → 99,3%, Lento 73,6 → 74,5%, TA-2 1,259 → 1,248 (−0,9%, efeito do pátio na ordem das compras do Parado; explicado no `BALANCE.md` §5).

## 9. Critério de pronto

| # | Critério | Como se mede |
|---|---|---|
| 1 | Este contrato e a matriz do `CONTRATO_LANCAMENTO.md` atualizados | o PR |
| 2 | Regras dos §1–§4 com testes de regra | `RegrasTests` |
| 3 | Invariantes do §7 e a prova de mutação | `ExploitTests` e o registro no `BALANCE.md` |
| 4 | Save schema 2 com migração do save da v0.1 (texto real gravado pela v0.1) e ida e volta estável | `SaveTests` |
| 5 | Portão do §8 medido, com o antes e o depois do TA-1 e do TA-2 | `BalanceTests` e `BALANCE.md` |
| 6 | `MAPA_CODIGO.md` e o cabeçalho do `Sim.cs` atualizados | o PR |
| 7 | `client/tools/verify.sh full` verde | saída no PR |

**Estado em 2026-10-29:** linhas 1 a 6 feitas neste PR; a 7 fica com a saída do `verify.sh full` no PR.

**Tela (2026-10-30):** quadro da tripulação e troca da escala ao tocar no voo, ícone do serviço e anel de progresso em cima do avião, veículo por serviço com vaga e mão única, mecânico saindo da oficina, hangar, aviso de 80%, decisão entre checagem e hangar e os eventos novos no diário CSV. Os aviões seguem `sim.Avioes`: cada um no seu estande, com o pouso começando antes da hora para encostar quando o núcleo encosta. Só o desembarque no estande G é sem veículo (pela ponte). `PodeChecagem`, `PodeHangar` e `PodeEscala` são a única adição ao núcleo: as mesmas travas das intenções, para a tela perguntar antes de oferecer.
