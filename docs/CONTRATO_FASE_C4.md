# Contrato da Fase C4: dia a dia, estações do ano, clima e falhas de avião no núcleo (2026-10-30)

Fonte da verdade: `GDD.md` §5.8 (incidentes), §5.12 (manutenção), §5.13 (portão de design), §5.14 (clima e falhas) e §14.2 (migração). Continua os contratos das Fases C, C2 e C3: nada de lá muda de regra, salvo o que o §1 diz da frente fria.

Pedido do Vinicius (2026-10-30): "aviões que podem quebrar, ou ter falhas ao decolar e precisar pousar. a ideia e realmente simular um dia a dia de um aeroporto, claro vai melhorando, mudando de estação tendo perigos de temporais entre outros".

Todo número é **hipótese**: o bot mede e o `BALANCE.md` §8 registra.

**Medido em 2026-10-31 (`BALANCE.md` §8):** o clima passou (Costa Azul 1,161, Aurora 1,147) e as falhas passaram (1,134, 1,131 e 1,124). A **demanda por estação do ano ficou só visual** (`Tuning.RegraEpocas = false`, D-C4a): ganho 1,000. As estações continuam mudando a chance de cada clima. Os picos do dia ficam ligados: não têm escolha própria e não quebram os portões. TA-1: Bom 94,2%, Lento 75,8%. TA-2: 1,336. Com tudo desligado, a tabela do bot é a da `main`, número a número. Mudou regra ou número? Atualize aqui e no `BALANCE.md` no mesmo commit.

## 1. O que entra

- **Relógio do jogo:** dia, hora e estação do ano saem de `Steps`, que é o relógio do quadro. Nunca do aparelho. Com o jogo fechado nada anda.
- **Dia a dia:** 1 onda = 3 horas de jogo, 1 dia = 8 ondas. A onda 0 começa às 06:00. A demanda sobe de manhã e no fim da tarde e cai de madrugada.
- **Estações do ano (enum `Epoca`):** verão, outono, inverno e primavera, 1 dia cada (8 ondas, 24 min de jogo; o ano tem 32 ondas, 1,6 h). O nome é `Epoca` porque `Estacao` já é a estação de trabalho do terminal.
- **Clima:** 5 tipos. A frente fria da C3 passa a ser um deles: com `Tuning.RegraClima` ligada ela sai dos pesos de incidente e vem da tabela de clima, com os mesmos números. Clima usa a vaga de incidente da C3: um perigo por vez, com a mesma folga, o mesmo aviso de 30 s e o mesmo `sim.Inc`.
- **Previsão:** `sim.Previsao` mostra no quadro o clima da onda seguinte. É a semente que decide, então a previsão acerta sempre. Só muda se o jogador marcar viagem, porque a chegada não tem perigo.
- **Falhas de avião:** pane no solo, rejeição de decolagem e retorno ao aeroporto. Só com o desgaste no aviso de 80% do §5.12.
- **Por desbloqueio:** clima e picos abrem no marco 1 (`Tuning.ClimaMarco`). Antes dele o jogo é o da C3, como o começo da C2.
- **Sem acidente, morte, fogo ou queda de avião:** falha sempre termina em pouso seguro e passageiros remarcados. Bombeiros e ambulância ficam em prontidão e voltam sem usar nada. O tom é aconchegante e a classificação indicativa continua baixa.

## 2. Dia e estação do ano

| Hora da onda | 06h | 09h | 12h | 15h | 18h | 21h | 00h | 03h |
|---|---|---|---|---|---|---|---|---|
| Demanda (`Tuning.DemandaHora`) | 1,25 | 1,0 | 0,9 | 1,0 | 1,25 | 1,0 | 0,75 | 0,85 |

A média é 1. O fator multiplica os passageiros do voo na criação da onda, junto com o fator da rota.

| Aeroporto | Perigo | Demanda (só visual, D-C4a): verão, outono, inverno, primavera |
|---|---|---|
| Vale Verde | temporal de verão | 1,0 · 1,0 · 1,0 · 1,0 |
| Costa Azul | vento forte e ressaca | 1,3 · 0,9 · 0,8 · 1,0 |
| Aurora Global | neve e degelo | 1,0 · 1,0 · 1,2 · 0,8 |

Chance de clima por onda (`Aeroporto.Clima`; a soma é a chance de a onda ter clima):

| Aeroporto | Verão | Outono | Inverno | Primavera |
|---|---|---|---|---|
| Vale Verde | temporal 0,35 | frente fria 0,15; neblina 0,10 | frente fria 0,20; neblina 0,10 | temporal 0,20 |
| Costa Azul | temporal 0,20; vento 0,25 | vento 0,30 | frente fria 0,20; vento 0,25 | temporal 0,10; vento 0,20 |
| Aurora Global | temporal 0,20 | frente fria 0,10; neblina 0,20 | neblina 0,10; neve 0,35 | frente fria 0,15; neblina 0,10 |

## 3. Clima

O sorteio é puro: `U(onda, 0, 0, sal)` da semente, da onda e da tabela da estação. Depois de um perigo vale a folga da C3 (`Tuning.IncidenteFolga`), e a chegada a um aeroporto conta como perigo. Na mesma onda o clima tem prioridade sobre o incidente sorteado. A vaga atingida é a de maior risco que tem voo, como na C3.

| Tipo | Causa | Efeito | Janela: s antes do portão, duração |
|---|---|---|---|
| Frente fria | frente fria na região | a pista fecha: ninguém pousa nem decola, e o embarque para | 48, 60 s |
| Temporal | raio perto do aeroporto | a pista fecha e o pátio para: serviços e embarque ficam parados, como na regra real de raio | 48, 55 s |
| Vento | rajada acima do limite da pista | a pista fecha, com o pátio funcionando | 48, 45 s |
| Neblina | teto baixo | pista com capacidade reduzida: cada pouso e cada decolagem espera 35 s | 60, 90 s |
| Neve | neve no pátio | o avião precisa de 25 s de degelo depois do portão, e só decola depois dele | 60, 90 s |

**O jogador escolhe, durante o aviso (e pela previsão, na onda anterior):**

| Saída | Como | Custa |
|---|---|---|
| Adiantar | `sim.Adianta()`: o embarque abre já. Com pista fechada, quem adiantou continua embarcando pela ponte (D-C3a). Com neblina ou neve, já pegou a vez na pista e já degelou | nada; com o avião ainda sem liberar, ajuda menos |
| Atrasar | `sim.Atrasa()`: o portão fecha `AtrasoFolga` (20 s) depois do fim do clima, no máximo na virada da onda. Clima que termina depois da virada não dá para atrasar (D-C4g) | o bônus de pontualidade do voo, e o avião volta mais tarde |
| Segurar | não fazer nada: o avião espera no solo e o portão fecha na hora | quem não embarcou perde o voo: só a receita dele |

Com clima de espera ou degelo no fechamento, o voo sai atrasado e não leva o bônus. Reabriu, tudo volta sozinho.

## 4. Falhas de avião

**Risco:** um voo só pode falhar com `Desgaste >= DesgasteAviso` (80). Esse aviso é o aviso prévio: voar assim é decisão do jogador, que tem a checagem rápida e o hangar para sair da faixa. O sorteio é puro, `U(onda, avião, 0, sal)`: chance `Tuning.FalhaAviaoChance` (0,4) por voo e tipo pelos pesos `Tuning.FalhaAviaoPesos` (3, 2, 1). Uma falha por voo.

| Falha | Quando | O que acontece | Saída |
|---|---|---|---|
| Pane no solo | na hora de abrir o embarque | o avião fica no portão e o embarque não abre até o reparo | `sim.ReparaAviao(a)`: 30 Cash e o mecânico por 8 s. Sem pagar, a inspeção obrigatória da Fase C resolve em 110 s, de graça, sem o desconto da oficina (D-C4e) |
| Rejeição de decolagem | na hora de decolar | o avião aborta na pista, a pista fica bloqueada por 20 s, o avião volta e sai 40 s depois | esperar: não há o que pagar |
| Retorno ao aeroporto | na decolagem | sobe, pede para voltar 15 s depois e pousa 25 s depois disso, com prioridade: passa na frente de pista fechada e de espera. Bombeiros e ambulância ficam em prontidão, os passageiros são remarcados e o avião entra em pane | o mesmo reparo da pane |

- O voo que falha depois de embarcar **já pagou**: a receita fica. Remarcar é história e evento, não Cash (`Ev.Remarcados`). O custo do retorno é o tempo: o avião perde parte do voo seguinte.
- O voo da pane que não embarca custa só a receita dele.
- Hangar também resolve a pane, do jeito da Fase C.
- Sem Cash o jogo não trava: a inspeção obrigatória é de graça e o outro avião continua voando.

## 5. Núcleo e API

- Determinístico, passo fixo de 1/30 s. Todo sorteio novo é `U` puro, então não há nada a gravar além do que está em andamento.
- Ler: `sim.Hora`, `sim.DiaJogo`, `sim.Noite`, `sim.Epoca`, `sim.Previsao`, `sim.Inc` (com os tipos novos no fim de `TipoIncidente`), `sim.ClimaDe(tipo)`, `sim.PistaFechada`, `sim.PatioParado`, `sim.EsperaPista`, `sim.PistaAte`, `sim.Degelando(a)`, `sim.EmRisco(a)`, `sim.DemandaDe(a, onda)`, `Sim.Opcoes(tipo)`, `Sim.Opcoes(falha)`; no avião, `Pane`, `SaiEm`, `Retorno`; no voo, `Atrasado`.
- Intenções novas: `sim.Atrasa()`, `sim.ReparaAviao(a)`; `sim.Adianta()` passa a valer para os 5 climas.
- Eventos novos no fim do `Ev`: `Previsto`, `Atrasado`, `Degelou`, `Pane`, `PaneReparo`, `PaneReparada`, `Rejeicao`, `RetornoPedido`, `PousoSeguro`, `Remarcados`.
- O teto do cofre offline não muda.

## 6. Invariantes (testes negativos no `ExploitTests`)

1. **Voo perdido custa só a própria receita**, com clima, pista fechada, pátio parado, atraso ou pane: nenhum Cash sai do saldo.
2. **Offline nunca perde voo** e nada que exija reação começa em segundo plano: clima, previsão, falha e reparo não andam com o jogo fechado.
3. **Nada pago é aleatório:** sem pagar para evitar ou sortear falha. Anúncio, 2× e Cash não mudam clima nem falha: as mesmas sementes dão os mesmos perigos.
4. **Reparo só com Cash:** sem Cash não há reparo pago, e anúncio nunca repara nem encurta perigo.
5. **Todo perigo tem aviso prévio, causa, duração aproximada e opções:** aviso ≥ `AvisoSeg` no clima, medido em 200 sementes; falha só depois do aviso de 80%; `Sim.Opcoes` nunca vazio.
6. **Nenhum evento cria derrota inevitável nem apaga saldo ou compra:** Cash só cai por escolha do jogador; embarcados, marco, níveis, equipe e rotas nunca recuam; nenhum avião some.
7. **Sem acidente:** todo avião que sai volta. Todo retorno termina em `PousoSeguro`, e não existe evento de queda, fogo ou morte no `Ev`.
8. **Reproduzível:** mesma semente, mesmas decisões, mesma lista de eventos, com e sem fechar e abrir o jogo.

## 7. Save

Schema 5. Grava o que está em andamento: `Pane`, `Falhou`, `SaiEm` e `Retorno` de cada avião, o bloqueio da pista e o horário do portão atrasado. O incidente em andamento continua no slot da C3, com os tipos novos. O schema 4 migra sem conta: chave ausente = nada em andamento. Teste com um save real gravado pelo código do schema 4 (semente 7, 700 s, antes do marco 1): migrar dá o mesmo jogo que jogar até ali com o código novo, e os dois seguem iguais depois do marco 1, com o clima valendo.

## 8. Bot e portões

- **TA-1 e TA-2** (200 sementes × 10 ondas, desbloqueio pelo marco): Bom ≥ 85%, Lento ≥ 60%, Bom/Parado ≥ 1,10.
- **Começo intacto:** com `RegraClima`, `RegraEpocas`, `RegraPicos` e `RegraFalhas` desligadas, a tabela do bot é a da `main`, número a número (`BalanceTests.Antes_FaseC4`).
- **Portão do §5.13 por sistema com escolha** (200 sementes, 1 ano de jogo, tudo aberto desde o começo, um sistema por vez). Planejador ≥ 1,10 × Automático, nas mesmas sementes. Abaixo disso, o sistema fica **só visual**.
  - **Clima** (Costa Azul e Aurora Global; Vale Verde e a partida inteira saem na tabela, D-C4f): o Planejador reage ao aviso; o Automático segura sempre.
  - **Falhas:** o Planejador faz a checagem e paga o reparo; o Automático voa no aviso e espera a inspeção.
  - **Estações do ano** (`BalanceTests.Medida_FaseC4`): o Planejador muda de aeroporto com a estação; o Automático fica onde está.
- Os picos do dia não têm escolha própria: só não podem quebrar os portões.

## 9. Critério de pronto

| # | Critério | Como se mede |
|---|---|---|
| 1 | Este contrato, GDD v1.3 e a matriz do `CONTRATO_LANCAMENTO.md` | o PR |
| 2 | Regras dos §2 a §4 com teste de regra, determinismo e regressão para cada evento | `ClimaFalhasTests` |
| 3 | Invariantes do §6 | `ExploitTests` e prova de mutação |
| 4 | Save schema 5 com migração do save real do schema 4 | `SaveTests` |
| 5 | TA-1, TA-2 e os portões do §8 medidos | `BalanceTests` e `BALANCE.md` §8 |
| 6 | `MAPA_CODIGO.md`, cabeçalho do `Sim.cs` e `CHANGELOG.md` | o PR |
| 7 | `client/tools/verify.sh full` verde | saída no PR |

**Fica para a View:** céu, chuva, neve e luz do dia, previsão no quadro, os veículos de emergência e de degelo e os avisos. A lista completa vai no corpo do PR.
