# Contrato da Fase C8: despesas e relatório do dia no núcleo (2026-11-03)

Fonte da verdade: `GDD.md` §0.5 (regra 6: nenhuma despesa leva a derrota inevitável), §4.1 (frente A), §5.13 (portão de design) e §6.1 a §6.3, e o §12 do GDD de direção v0.1 (`GDD_DIRECAO_v0.1.md`). Continua os contratos das Fases C a D1: nada de lá muda de regra. Fica fora daqui o que o §12 do GDD de direção lista e o jogo ainda não tem: energia, água, limpeza, combustível de veículo, treinamento, contratos, seguros e relatório semanal ou mensal.

Todo número é **hipótese**: o bot mede e o `BALANCE.md` registra.

**Medido em 2026-11-03 (`BALANCE.md` §13):** todos os portões seguem verdes com a conta ligada. TA-1: Bom 94,4%, Lento 79,1%. TA-2: 1,411. Aeroportos, o que mais cai: 1,122. Campanha 1,142, missões 1,164. Sem compra, os 30 objetivos fecham em 200 de 200 sementes; com o passe (Fase C7), a trilha gratuita também. A conta pesa 8% a 11% da receita nas 10 primeiras ondas. **A 1ª tabela era 1,67 × esta e derrubava os aeroportos para 1,078** (D-C8c): o §2 já tem a final. Mudou regra ou número? Atualize aqui e no `BALANCE.md` no mesmo commit.

## 1. O que entra

- **Salário por dia do jogo:** um valor por perfil de funcionário (6), por função da tripulação (2) e por pessoa de pátio.
- **Manutenção por dia do jogo:** um valor por estação ativa (8) e quanto cada nível de melhoria dela soma.
- **A conta sai no fechamento do dia do jogo** (`DiaJogo`, 8 ondas, 24 min jogados), no 1º passo do dia seguinte.
- **Relatório do dia:** receita por fonte, despesa por categoria, o que foi pago e o lucro, do dia em curso e dos 7 últimos fechados, no save. É o que o painel de operações (frente I) lê.
- **Fora daqui:** tela, textos, demissão e o painel. A demissão entra com a frente D (contratação).

## 2. Regras

| Regra | Como funciona |
|---|---|
| Tabela | `Tuning.Salario` (ordem de `Perfil`), `Tuning.SalarioTrip` (ordem de `Funcao`), `Tuning.SalarioPatio`, `Tuning.Manutencao` e `Tuning.ManutencaoNivel` (ordem de `Estacao`) |
| Quem paga | Todo funcionário, tripulante e pessoa de pátio contratado. Estação ativa: aberta pelo marco e com regra; a só visual não cobra. O nível conta velocidade + capacidade |
| Quando | No 1º passo do dia seguinte, com o estado daquela hora. Com o jogo fechado o dia não vira, então offline nunca cobra. Pular dias (save adulterado) cobra só o dia gravado |
| Pagamento | O caixa paga o que tem. O Cash nunca fica negativo |
| Falta de caixa | O que faltou vira `Atrasada`, emite o aviso (`Ev.DiaFechado` com C > 0) e pausa contratações e melhorias (`Hire`, `HireTripulante`, `HirePatio`, `Buy`, `BuyPatio`). O resto do jogo segue: voo, toque, mover, checagem, hangar, reparo, rota e viagem |
| Fim da pausa | Assim que o caixa cobre a `Atrasada` inteira, ela se paga sozinha (`Ev.ContaQuitada`) e a pausa acaba |
| Sem dívida | O fechamento seguinte perdoa o que sobrou e cobra só o dia dele. Nunca há juro, nem atrasada maior que uma conta |
| Relatório | Todo Cash que entra passa por `Recebe(valor, fonte)`. Fontes: tarifas, bagagem, loja, alimentação, bônus, prêmios (campanha e missões), cofre (offline e o anúncio do cofre) e compras (produto e pacote de Gems). Categorias: equipe, tripulação, pátio e manutenção. Lucro = receita − despesa |

**D-C8a. Falta de caixa pausa até o caixa cobrir, e o dia seguinte perdoa.** O guardrail do plano pede despesa sem derrota e sem dívida que trave. Três formas foram pensadas. Perdoar na hora deixava gastar tudo antes da virada e nunca pagar. Pausar até o fechamento seguinte trancava as compras por 24 min, mesmo com caixa. A escolhida cobra assim que o caixa cobre e perdoa no fechamento seguinte: a pausa dura o tempo de juntar uma conta, e quem está no vermelho de verdade não acumula nada.

**D-C8b. A conta é a do estado na hora do fechamento.** Sem rateio por minuto: contratar logo antes da virada paga o dia inteiro, e logo depois paga só no fechamento seguinte. É simples de mostrar na tela ("a conta de hoje") e a diferença é de uma conta.

## 3. Núcleo e API

- Determinístico, passo fixo de 1/30 s. Nada sai de sorteio.
- Ler: `sim.Hoje` (o dia em curso: `Receitas` por `Fonte`), `sim.Historico` (até 7 `DiaConta`: `Dia`, `Receitas`, `Despesas` por `Despesa`, `Pago`, `Receita`, `Despesa`, `Lucro`, `Falta`), `sim.Folha(d)` e `sim.Folha()` (a conta por dia agora), `sim.Atrasada` e `sim.Pausado`.
- Eventos novos no fim do `Ev`: `DiaFechado` (A = dia, B = Cash pago, C = o que faltou) → `day_closed`; `ContaQuitada` (A = dia, B = Cash) → `day_bill_paid_late`.
- `Tuning.RegraDespesas = false` = só o relatório: conta a receita e não cobra nada.

## 4. Invariantes (testes negativos no `DespesasTests`)

1. **Cash nunca negativo**, nem com a conta 1 000 × a receita.
2. **Sem derrota:** com a conta impagável, todo voo sai, o terminal continua embarcando e só contratar e melhorar param.
3. **Sem dívida:** a falta nunca passa de uma conta; o fechamento seguinte perdoa.
4. **Offline sem perda e com teto de 2 h:** `ApplyOffline` não cobra nem vira o dia; o cofre entra inteiro.
5. **A conta não depende de voo:** quem embarca todos e quem perde todos os voos pagam o mesmo pelo mesmo aeroporto.
6. **Cobra uma vez por dia**, no 1º passo do dia seguinte.
7. **O relatório bate com os eventos**, fonte por fonte.
8. **Fechar e abrir no meio da onda dá o mesmo jogo**, com a conta atrasada e o relatório.

## 5. Save

Schema 9. `atrasada=Cash`; `hoje=` e `dc.<i>=` = `dia,pago,8 receitas,4 despesas`. Tudo limitado: dia entre 0 e o dia atual, histórico em ordem e antes do dia em curso, valores entre 0 e 2^50, pago até a conta, atrasada até o que faltou no último dia.

O schema 8 migra sem conta: abrir não cobra nada, o relatório começa vazio, o dia em curso começa a contar no 1º passo e a 1ª conta sai quando ele vira, inteira. Teste com um save real gravado pelo código do schema 8 (commit 49e1597, semente 7, bot Bom, 2 790 s): com `RegraDespesas` desligada, migrar dá o mesmo jogo que jogar até ali com o código novo.

Os saves dos schemas 1 a 7 continuam migrando pelo mesmo caminho.

## 6. Bot e portões

- **Começo intacto:** com `RegraDespesas` desligada, a tabela do bot é a da Fase D1, número a número (`BalanceTests.Antes_FaseD1`).
- **Todos os portões seguem verdes com a conta ligada** (200 sementes): TA-1 e TA-2, Fase C, C2, C3, C4, campanha (C5), missões (C6), compras (D1: os 30 objetivos fecham sem compra), TA-6 e os alcances da campanha e das conquistas. Com o passe (Fase C7): a trilha gratuita fecha sem compra.
- **O bot respeita a pausa** (D-C8d): com a conta atrasada, só olha rota e viagem, como a tela vai deixar.
- **O peso da conta** sai na tabela, sem portão próprio (`Medida_FaseC8`): receita, conta paga, a parte da receita e os dias com falta de caixa, por bot e horizonte.
- **Sem portão do §5.13:** a despesa não é um sistema com escolha nova; ela muda o valor das escolhas que já existem (contratar, melhorar). A pergunta "contratar sem demanda dá prejuízo?" fica para a frente D, que traz a demissão.

## 7. Critério de pronto

| # | Critério | Como se mede |
|---|---|---|
| 1 | Este contrato e a linha da matriz do `CONTRATO_LANCAMENTO.md` | o PR |
| 2 | Regras do §2 com teste de regra e determinismo | `DespesasTests` |
| 3 | Invariantes do §4 | `DespesasTests` e prova de mutação |
| 4 | Save schema 9 com migração do save real do schema 8 | `SaveTests` |
| 5 | Todos os portões medidos de novo, com antes e depois | `BalanceTests` e `BALANCE.md` |
| 6 | `MAPA_CODIGO.md`, cabeçalho do `Sim.cs` e `CHANGELOG.md` | o PR |
| 7 | `client/tools/verify.sh full` verde | saída no PR |

**Fica para a tela:** a conta de hoje no HUD, o aviso de falta de caixa com o valor e o motivo da pausa nos botões de contratar e melhorar, e o relatório no painel de operações (frente I).
