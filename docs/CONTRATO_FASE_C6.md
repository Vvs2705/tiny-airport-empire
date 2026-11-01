# Contrato da Fase C6: missões e conquistas no núcleo (2026-11-01)

Fonte da verdade: `GDD.md` §4.1, §5.13 (portão de design), §6.1 e §6.5, com uma exceção registrada no §2 (D-C6a). As listas estão em `MISSOES.md`. Continua os contratos das Fases C a C5: nada de lá muda de regra.

Todo número é **hipótese**: o bot mede e o `BALANCE.md` registra.

**Medido em 2026-11-01 (`BALANCE.md` §10):** MEDIDA_RESUMO Mudou regra ou número? Atualize aqui e no `BALANCE.md` no mesmo commit.

## 1. O que entra

- **3 diárias e 5 semanais** em curso, escolhidas de 37 e 26 linhas (`Tabelas.Diarias`, `Tabelas.Semanais`). Cada semanal tem 2 partes de sistemas diferentes.
- **20 conquistas permanentes** (`Tabelas.Conquistas`).
- **Reaproveita a campanha:** os mesmos eventos e o mesmo `Conta`. O que a campanha mede, a missão mede igual. Entram 5 metas no fim do `Meta`: `Melhorias` para as missões, e `Passageiros`, `Rotas`, `Campanha` e `Missoes` para as conquistas.
- **Recompensa:** missão paga Cash fixo da tabela, único tipo. Fora do `Ganha`: o 2× não dobra e o cofre offline não aprende com ela. Conquista não paga Cash: a recompensa é o selo.
- **Fora daqui:** tela, textos, passe e campanha sazonal. O passe lê `Ev.MissaoConcluida` para dar os pontos de temporada.

## 2. Regras

| Regra | Como funciona |
|---|---|
| Dia e semana | `DiaJogo` (8 ondas, 24 min jogados) e `SemanaJogo` (7 dias do jogo, 2,8 h jogadas). Saem de `Steps`. Nada lê a hora do aparelho, nem o `Dia` local |
| Virada | No fim do passo fixo em que o dia muda, a lista troca. Missão aberta sai sem custo. Com o jogo fechado nada vira |
| Escolha | A semente dá uma nota por linha e entram as menores que cabem, sem repetir a meta. Mesma semente, mesmo dia e mesmo jogo, mesma lista. A semente nunca muda o valor |
| O que cabe | Só a linha do marco atual e só o que dá para fazer ali: estação aberta, rota aberta no aeroporto atual, melhoria ainda à venda. A lista fica como foi escolhida até a virada |
| Conta | Todo `Conta` da campanha passa também pelas missões, em qualquer aeroporto. Para no alvo |
| Conclusão | Avaliada no fim do passo fixo, paga uma vez e emite `Ev.MissaoConcluida`. A semanal pede as 2 partes |
| Perigo | Diária não pede clima nem incidente: não espera a semente. Semanal pede "qualquer um", nunca um tipo só |
| Sem recuo | Missão não usa `Seguidos` nem meta de estado: o progresso nunca recua e nada conclui sozinho |
| Conquista | Conta em qualquer aeroporto, dura para sempre, e a feita fica feita. `Seguidos` pode zerar antes de fechar; depois de fechada, não desfaz |

**D-C6a. O dia da missão é o do jogo, não o do calendário.** O GDD §6.5 diz que o dia vira às 04:00 no fuso gravado. Este contrato troca isso pelo `DiaJogo`, pelo pedido da frente: nada de hora de parede dentro da simulação. O que se ganha: reproduzível no bot, imune a adiantar o relógio do aparelho e faltar um dia deixa de existir. O que se perde: a missão não chama o jogador de volta no dia seguinte, e quem joga 1 h por dia vê 2,5 listas nesse dia. O passe do GDD §6.4 conta 3 diárias por dia de calendário: a frente do passe precisa refazer a conta com isso. Voltar ao calendário é trocar `DiaJogo` por `Dia` em `Sim.Missoes.cs` e medir de novo.

## 3. Núcleo e API

- Determinístico, passo fixo de 1/30 s. A escolha sai de `U`, puro da semente.
- Ler: `sim.Diarias[0..2]` e `sim.Semanais[0..4]` (`O`, `O.E`, `Conta`, `ContaE`, `Feita`; null antes do 1º passo), `sim.SemanaJogo`, `sim.T.Conquistas[i]`, `sim.ConquistaFeita(i)`, `sim.ProgressoConquista(i)`, `sim.ConquistasFeitas`.
- Eventos novos no fim do `Ev`: `MissaoConcluida` (A = 0 diária, 1 semanal, B = índice na tabela, C = Cash) → `daily_mission_completed`, `weekly_mission_completed`; `ConquistaFeita` (A = índice) → `achievement_unlocked`.
- `Tuning.RegraMissoes = false` = só visual: conta e mostra, sem pagar.

## 4. Invariantes (testes negativos no `ExploitTests`)

1. **Recompensa fixa:** a mesma tabela em toda semente. O 2× não dobra.
2. **Paga uma vez:** fechar e abrir o jogo, em qualquer ponto, nunca paga de novo nem perde o que foi feito.
3. **Faltar um dia não custa nada:** 30 ou 400 dias fora, relógio do aparelho na frente ou para trás: mesma lista, mesmo dia do jogo, nenhum Cash a menos.
4. **A virada não tira nada:** missão aberta sai sem Cash, sem sequência e sem recuo.
5. **Offline nunca anda as missões:** `ApplyOffline` não conta nem conclui.
6. **Nenhuma missão exige anúncio ou compra:** nenhuma meta olha anúncio ou compra digital, e assistir anúncio não anda missão nem conquista.
7. **Voo perdido não tira nada:** Cash e progresso de missão só sobem. Perder o voo custa só a receita dele.
8. **Cofre intacto:** a recompensa não entra na renda média nem no teto do cofre.
9. **Conquista permanente e sem Cash:** nem com `Recompensa` na linha ela paga, e reabrir não desfaz nem repete.
10. **Nenhuma conquista inalcançável:** o bot que joga pela campanha e pelas missões fecha as 20 (§6).

## 5. Save

Schema 7. `miss=dia,semana`; uma chave por vaga, `md.<v>` e `ms.<v>` = `indice,conta,contaE,feita`; `cq=máscara,contadores`. Tudo limitado: índice fora da tabela sai, contador fica entre 0 e o alvo, máscara presa à tabela, dia no máximo o atual.

O schema 6 migra sem conta: chave ausente = lista nova no 1º passo, contador em zero, e o que o estado já prova conquista uma vez. Teste com um save real gravado pelo código do schema 6 (commit 804cfb7, semente 7, bot Bom, 2 790 s, Costa Azul, marco 2): com `RegraMissoes` desligada, migrar dá o mesmo jogo que jogar até ali com o código novo.

Os saves dos schemas 1 a 5 continuam migrando pelo mesmo caminho.

## 6. Bot e portões

- **Começo intacto:** com `RegraMissoes` desligada, a tabela do bot é a da Fase C5, número a número (`BalanceTests.Antes_FaseC6`).
- **TA-1 e TA-2** (200 sementes × 10 ondas): Bom ≥ 85%, Lento ≥ 60%, Bom/Parado ≥ 1,10, com as missões pagando.
- **Portão do §5.13** (`Portao_FaseC6_Missoes`, roda no `balanco.yml`; 200 sementes × 20 ondas, o horizonte da Fase C5): Planejador = o Bom que joga pelas missões (`BotProfile.Missionario`). Automático = o mesmo Bom, que nunca olha as missões e recebe o que cumprir sem querer. O ganho conta receita + campanha + missões. Precisa dar ≥ 1,10, ou as missões ficam só visuais.
- **Horizontes longos** e o Lento: saem na tabela, sem portão (`Medida_FaseC6`).
- **Alcance** (`Alcance_FaseC6`): o `Completo` e o `LentoCompleto`, que jogam pela campanha e pelas missões, fecham as 20 em 200 de 200 sementes, em até 240 ondas.

## 7. Critério de pronto

| # | Critério | Como se mede |
|---|---|---|
| 1 | Este contrato, `MISSOES.md` e as linhas 12 e 13 da matriz do `CONTRATO_LANCAMENTO.md` | o PR |
| 2 | Regras do §2 com teste de regra e determinismo; tabelas iguais ao `MISSOES.md` | `MissoesTests` |
| 3 | Invariantes do §4 | `ExploitTests` e prova de mutação |
| 4 | Save schema 7 com migração do save real do schema 6 | `SaveTests` |
| 5 | TA-1, TA-2 e os portões do §6 medidos | `BalanceTests` e `BALANCE.md` |
| 6 | `MAPA_CODIGO.md`, cabeçalho do `Sim.cs` e `CHANGELOG.md` | o PR |
| 7 | `client/tools/verify.sh full` verde | saída no PR |

**Fica para a tela:** lista das 3 diárias e das 5 semanais, tela de conquistas, aviso de missão feita e os eventos no diário.
