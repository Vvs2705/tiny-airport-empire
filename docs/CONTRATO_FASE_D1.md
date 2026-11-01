# Contrato da Fase D1: Gems e compras no núcleo (2026-11-01)

Fonte da verdade: `GDD.md` §6.1, §7.1, §7.3, §7.4, §8 e §16.6, com as decisões D-05, D-06 e D-07 do §23.2. É a primeira parte da Fase D: **sem loja real, sem servidor e sem tela.** Continua os contratos das Fases C a C6: nada de lá muda de regra, com uma adição registrada no §1 (a cadeia fechada dá Gems).

Todo número é **hipótese**: o bot mede e o `BALANCE.md` registra.

**Medido em 2026-11-01 (`BALANCE.md` §11):** sem compra, os 30 objetivos fecham em 200 de 200 sementes, com o Guiado e o LentoGuiado. Dinheiro sem limite fecha a campanha em **0,990** do tempo de quem não compra. Quem não gasta Gems joga a tabela da fase anterior, número a número. Mudou regra ou número? Atualize aqui e no `BALANCE.md` no mesmo commit.

## 1. O que entra

- **Gems:** segunda moeda, no save, entre 0 e `Tuning.GemsTeto`. Não expira.
- **De onde vêm:** compra e **uma meta clara** (GDD §6.1): fechar a cadeia de objetivos de um aeroporto paga 100 Gems, 300 na campanha inteira. O offline nunca dá Gems, e anúncio também não. Esta é a adição ao contrato da Fase C5: o objetivo continua pagando só Cash, e a Gem vem do fechamento da cadeia, fora da tabela de objetivos.
- **Catálogo fixo** (§2), com o id do GDD §7.3 e sem preço real.
- **Crédito por id de transação** (§6): a mesma transação creditada duas vezes rende uma só.
- **O que as Gems compram** (§3): os 3 sumidouros do GDD §6.1, e nada além deles.
- **Fora daqui:** tela da loja (`ta-telas`), textos, Unity IAP, servidor, restauração pela loja, reembolso e a trilha premium do passe. O servidor é o PR seguinte.

## 2. Catálogo (`Tabelas.Produtos`)

Id = o da loja: decide-se uma vez, nunca muda nem se reaproveita (GDD §7.3). Preço, moeda e localização vêm da loja (GDD §7.1): o catálogo não tem campo de preço, e um teste garante isso. Produto novo entra só no fim, com id novo.

| Id | Tipo | Cash | Gems | Posse |
|---|---|---|---|---|
| `welcome_bundle_01` | não consumível | 1 000 | 200 | a pintura de boas-vindas |
| `gems_pack_small` | consumível | | 100 | |
| `gems_pack_medium` | consumível | | 550 | |
| `gems_pack_large` | consumível | | 1 200 | |
| `season_pass_s1` | não consumível | | | a trilha premium (ainda sem regra) |
| `aircraft_skin_blue_01` | não consumível | | | a pintura |
| `terminal_decor_01` | não consumível | | | a decoração |

## 3. O que as Gems compram (`Tabelas.ItensGems`)

| Id | O que dá | Gems | Regra |
|---|---|---|---|
| `cash_pack` | 10 min da renda média atual (`RateEma`), entre 200 e 10 000 Cash | 50 | a prévia mostra o valor exato antes da compra; fora da renda média e do cofre |
| `boost_30min` | 2× receita por 30 min de jogo | 150 | não acumula com o 2× do anúncio, nos dois sentidos (GDD §7.2); só anda com o jogo aberto |
| `aircraft_skin_blue_01` | a pintura | 300 | uma vez; a mesma posse da compra direta |
| `terminal_decor_01` | a decoração | 300 | uma vez; a mesma posse da compra direta |

**O que Gems nunca compram:** repor voo perdido, pular a perda de um voo, passageiro a bordo, objetivo, marco, nível, rota, aeroporto, contratação, tripulação, reparo, hangar, tempo de jogo e qualquer conteúdo sorteado. Cash comprado paga o que o Cash já paga, pelo preço de sempre. Tipo novo de item só com mudança no GDD e neste contrato: o enum `TipoItem` tem teste que quebra.

## 4. Invariantes (testes negativos no `ComprasTests`)

1. **Nada aleatório pago** (ECA Digital, Lei 15.211/2025): conteúdo igual em toda semente. Sem caixa, sem roleta, sem probabilidade.
2. **Uma transação, um crédito:** callback repetido, reabrir o jogo ou pedir outro produto com o mesmo id não paga de novo.
3. **Nenhuma quantidade vem de fora:** `Creditar` recebe só a transação e o id do produto. O conteúdo sai da tabela (GDD §8.2).
4. **Comprar não toca no jogo:** depois de creditar e gastar, todo o save fica igual, menos Cash, Gems, boost, posse e transações. Voo perdido continua perdido.
5. **Sem compra termina a campanha:** nenhuma meta olha compra, e os 30 fecham no portão do §7.
6. **Offline continua em 2 h:** comprar não muda o teto, o offline não dá Gems, e o booster não corre com o jogo fechado.
7. **Cofre intacto:** Cash comprado fica fora da renda média. O pacote não se alimenta dele mesmo.
8. **Anúncio nunca dá Gems.**
9. **Saldo limitado:** Gems entre 0 e o teto, mesmo com save editado, e Cash nunca passa do limite de `long` (GDD §8.6).
10. **Compra não some calada:** com o save recusado ou acima do teto, `Creditar` recusa e não anota o id. Sem o acknowledge, a loja devolve o dinheiro, e a transação pode voltar depois.
11. **O token não vai para o disco:** o save guarda só o SHA-256 do id (GDD §8.5).

## 5. Save

Schema 8, com 3 chaves: `gems=N`, `posse=id,id` (só os não consumíveis) e `tx=sha256,sha256` (uma entrada por compra). Tudo limitado: Gems entre 0 e o teto; posse de id que sumiu ou de consumível é ignorada; transação nunca sai da lista, porque tirar a mais antiga reabriria o crédito dobrado dela.

O booster sobe o teto do `bst` no `Load` para 30 min. A tela já mostra o tempo que falta, sem barra de fração.

O schema 7 migra sem conta: chave ausente = saldo 0 e nada comprado. Cadeia já fechada não paga Gems retroativas, e o jogo ainda não tem saves de jogador. Teste com um save real gravado pelo código do schema 7 (semente 7, bot Bom, 2 790 s): migrar dá o mesmo jogo que jogar até ali com o código novo. Os saves dos schemas 1 a 6 continuam migrando pelo mesmo caminho.

## 6. Crédito: o ponto do servidor

`sim.Creditar(tx, produto)` devolve `Credito`:

| Resultado | Quando | A loja confirma? |
|---|---|---|
| `Creditado` | pagou | sim, depois de gravar o save |
| `Repetido` | id já creditado | sim: é retry |
| `JaTem` | não consumível já na posse, com outro id | sim: nada a dar; o id fica anotado |
| `Invalido` | id vazio ou produto fora do catálogo | não |
| `Teto` | passaria do teto de Gems | não |
| `SaveRecusado` | o save não abriu, então nada gravaria | não |

Ordem da compra em 2 passos (GDD §7.4), para o PR do servidor: o servidor valida na loja, o cliente chama `Creditar`, grava o save e só então confirma. Se o app cair entre a gravação e a confirmação, o callback volta e sai `Repetido`.

**Limite honesto** (GDD §7.4 e §8.2): sem login, o save é local. A trava impede crédito dobrado, não save editado. A posse no save é cache: a autoridade é a loja e o servidor. Não chamar isto de seguro contra hack.

| GDD §16.6 | Onde está |
|---|---|
| Produto indisponível | `Credito_Invalido_NaoMudaNada` |
| Repetição de callback | `Credito_AMesmaTransacaoRendeUmaSo` |
| Recompensa não duplicada ao reabrir o app | `Credito_ReabrirOJogoNaoPagaDeNovo` |
| Restore após reinstalação | `Credito_Restaurar_NoSaveNovo_Volta` (o conteúdo volta com o save novo, GDD §7.3) |
| Passe já comprado | `Credito_NaoConsumivel_UmaVezSo` |
| Compra cancelada, pendente, backend indisponível ou que confirma depois | nada chama `Creditar` até o servidor responder; o id repetido cobre a volta. Fluxo fica no PR do servidor |
| Sandbox, reembolso, preço localizado, passe no meio da temporada | dependem da Play Console e do servidor |

**Reembolso:** fica para o PR do servidor. O hash do id continua no save, então a mesma transação não credita de novo depois de revogada; o servidor informa o produto.

## 7. Bot e portões

- **Sem Gems gastas, a tabela da fase anterior:** `BalanceTests.Antes_FaseD1`, 200 sementes × 10 ondas, número a número.
- **Portão de compras:** `Portao_FaseD1_Compras`, no CI, com 200 sementes. O Guiado, o LentoGuiado e a Baleia (200 pacotes grandes) fecham os 30, e a Baleia fecha em pelo menos **0,85** do tempo do Guiado.
- **Os outros bots:** `Medida_FaseD1`, à mão, com GastaGanho (gasta as Gems ganhas), BoasVindas e PacoteGrande, mais o tamanho do pacote de Cash ao longo da campanha.
- **Baleia é o teto do que a compra acelera:** booster sempre ligado e, enquanto houver Gems, nunca menos de 5 000 Cash. O bot faz o papel do servidor no 1º passo.
- Pacote de Cash acima de 1 h de renda seria recompensa de alto valor (GDD §7.2): `PacoteSeg` fica em 600 s, e um teste trava em 3 600 s.

## 8. Critério de pronto

| # | Critério | Como se mede |
|---|---|---|
| 1 | Este contrato e as linhas 14, 15 e 18 da matriz do `CONTRATO_LANCAMENTO.md` | o PR |
| 2 | Catálogo igual ao §2, sem preço real, e só os 3 tipos do §3 | `ComprasTests` |
| 3 | Invariantes do §4 | `ComprasTests` e prova de mutação |
| 4 | Save schema 8 com migração do save real do schema 7 | `SaveTests` |
| 5 | Portões do §7 medidos | `BalanceTests` e `BALANCE.md` §11 |
| 6 | `MAPA_CODIGO.md`, cabeçalho do `Sim.cs` e `CHANGELOG.md` | o PR |
| 7 | `client/tools/verify.sh full` verde | saída no PR |

**Fica para a tela:** loja com prévia e conteúdo antes da compra, saldo de Gems na HUD, os eventos `gems_spent` e `gems_earned` no diário e o estado de compra pendente, falha e cancelamento.
