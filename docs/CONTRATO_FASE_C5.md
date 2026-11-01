# Contrato da Fase C5: campanha principal no núcleo (2026-11-01)

Fonte da verdade: `GDD.md` §4.1, §5.10, §5.13 (portão de design), §6.1, §6.5 e §14.2 (migração), sem mudar nenhum deles. A lista dos 30 objetivos está em `CAMPANHA.md`. Continua os contratos das Fases C a C4: nada de lá muda de regra.

Todo número é **hipótese**: o bot mede e o `BALANCE.md` registra.

**Medido em 2026-11-01 (`BALANCE.md` §9, D-C5a a D-C5d):** os 30 fecham em 200 de 200 sementes, com o Bom e com o Lento que jogam pelo objetivo. Portão do §5.13 em 20 ondas: **1,140**. TA-1: Bom 94,9%, Lento 73,2%. TA-2: 1,413. Com a campanha sem pagar, a tabela do bot é a da fase anterior, número a número. O desenho mudou com a medida: **a primeira versão deste contrato contava só o objetivo ativo e media o portão em 64 ondas**. O §2 e o §6 já estão na versão final, e o BALANCE conta por quê. Mudou regra ou número? Atualize aqui e no `BALANCE.md` no mesmo commit.

## 1. O que entra

- **30 objetivos, 10 por aeroporto**, em 3 cadeias (`Tabelas.Objetivos`). A tabela é uma só: o jogo e o bot leem a mesma.
- **Cadeia em ordem:** cada aeroporto tem 1 objetivo ativo, o que a tela mostra, e os objetivos fecham na ordem. O pré-requisito de um objetivo é o anterior da cadeia, mais o que o jogo já pede (marco, Cash para a rota, a viagem).
- **A campanha guia, não tranca:** nenhum objetivo abre aeroporto, rota, estação ou perfil. Tudo isso continua pelo marco e pelo Cash. Quem viajar antes de fechar a cadeia pode voltar de graça e terminar.
- **Recompensa:** Cash fixo da tabela, único tipo (GDD §6.1: uma moeda, com sumidouro). Sem Gems, sem sorteio, sem bônus de anúncio. Fora do `Ganha`: o 2× não dobra e o cofre offline não aprende com ela.
- **Fora daqui:** tela (`ta-telas`), textos (`ta-textos`), missões, conquistas, passe e metas de maestria. O núcleo só expõe `CampanhaCompleta`.

## 2. Regras

| Regra | Como funciona |
|---|---|
| Meta de estado | Lê o que já existe: `Marco`, `Equipe`, `Velocidade`, `Capacidade`, `Chegou`. Vale em qualquer aeroporto. Se já estava cumprida quando virou a ativa, conclui no passo seguinte |
| Meta de contador | Conta eventos **só com o jogo no aeroporto do objetivo**, desde a chegada, também antes de ele virar o ativo. Assim a cadeia não espera a semente repetir um perigo que já passou. Para no alvo |
| Conclusão | Avaliada no fim de cada passo fixo: uma por cadeia e por passo. Paga o Cash e emite `Ev.ObjetivoConcluido` |
| `Seguidos` | Voos lotados seguidos: um voo abaixo de 90% zera a sequência. É a única meta que recua |
| `Climas` e `Incidentes` | Contam perigo **resolvido** (`Ev.IncidenteFim` com B = 1): adiantou, atrasou, reparou, priorizou, ou segurou o pico sem perder ninguém. Segurar o clima sem fazer nada não conta |
| `Cheios` e `Voos` | Voo com passageiros: `Cheios` com pelo menos `Tuning.LotadoPct` (90%) a bordo, `Voos` com alguém a bordo. Com `Arg`, só naquela rota |

Metas de sorteio pedem "qualquer clima" ou "qualquer incidente", nunca um tipo só: a cadeia não pode ficar parada à espera da semente.

## 3. Núcleo e API

- Determinístico, passo fixo de 1/30 s. Nada novo é sorteado. Nada lê o relógio do aparelho.
- Ler: `sim.T.Objetivos[a][i]` (`Id`, `Nome`, `Meta`, `Arg`, `Alvo`, `Recompensa`), `sim.ObjetivoAtual(a)` (null = cadeia fechada), `sim.ObjetivosFeitos(a)`, `sim.Progresso(a)`, `sim.CampanhaCompleta`.
- Evento novo no fim do `Ev`: `ObjetivoConcluido` (A = aeroporto, B = índice na cadeia, C = Cash) → `campaign_objective_completed`.
- Tudo passa por `Emit` e por um contador só (`Sim.Metas.cs`). A exceção é `Atendidos`, que conta em `Avanca`, porque `Ev.Venda` não diz a estação.
- `Tuning.RegraCampanha = false` = só visual: conta e mostra, sem pagar.

## 4. Invariantes (testes negativos no `ExploitTests`)

1. **Recompensa fixa:** a mesma tabela em toda semente. Nada sai de sorteio.
2. **Paga uma vez:** fechar e abrir o jogo, em qualquer ponto, nunca paga de novo nem perde o que foi feito.
3. **Nenhum objetivo exige vídeo ou compra:** nenhuma meta olha anúncio ou compra digital. Assistir anúncio não avança progresso, e a recompensa não dobra com o 2×.
4. **Offline nunca anda a campanha:** `ApplyOffline` não conta, não conclui, não perde voo. Sem `Passo`, nada muda.
5. **A campanha nunca tira nada:** nenhum objetivo tira Cash, marco, nível ou rota. Voo perdido custa só a própria receita, e o único progresso que recua é a sequência de `Seguidos`.
6. **Nenhum objetivo inalcançável:** a tabela é coerente com as rotas, os aeroportos e os tetos do jogo, e o bot fecha os 30 (§6).
7. **Cofre intacto:** a recompensa não entra na renda média (`RateEma`) nem no teto do cofre.
8. **Reproduzível:** mesma semente, mesmas decisões, mesma sequência de objetivos, com e sem fechar e abrir o jogo.

## 5. Save

Schema 6. Uma chave por aeroporto: `camp.<a>=feitos,conta0,...,conta9`, com tudo limitado (`feitos` entre 0 e o tamanho da cadeia, cada `conta` entre 0 e o alvo do objetivo).

O schema 5 migra sem conta: chave ausente = nada feito. O que o estado já prova (metas de estado) conclui em cascata nos primeiros passos e paga. Contador começa em zero. Teste com um save real gravado pelo código do schema 5 (semente 7, bot Bom, 2 790 s, Costa Azul, marco 2): com `RegraCampanha` desligada, migrar dá o mesmo jogo que jogar até ali com o código novo; com ela ligada, as cadeias andam até a primeira meta de contador e nada mais muda.

Os saves dos schemas 1 a 4 continuam migrando pelo mesmo caminho.

## 6. Bot e portões

- **TA-1 e TA-2** (200 sementes × 10 ondas, desbloqueio pelo marco): Bom ≥ 85%, Lento ≥ 60%, Bom/Parado ≥ 1,10, com a campanha ligada.
- **Começo intacto:** com `RegraCampanha` desligada, a tabela do bot é a da fase anterior, número a número (`BalanceTests.Antes_FaseC5`).
- **Portão do §5.13** (200 sementes × 20 ondas, desbloqueio normal, o horizonte das Fases C a C3). Planejador = o Bom que joga pelo objetivo ativo (`BotProfile.Guiado`). Automático = o mesmo Bom, que nunca olha a campanha. A recompensa vale para os dois, e o ganho conta receita + recompensas. Precisa dar ≥ 1,10, ou a campanha fica só visual.
- **Horizontes longos** (30, 40 e 64 ondas): saem na tabela, sem portão. Em 20 ondas a renda acumulada alcança o preço do catálogo inteiro (27 670 Cash). Depois disso o Cash não compra mais nada, e a razão mede quanto Aurora paga, não a decisão (D-C5c).
- **Alcançável:** o Bom e o Lento que jogam pelo objetivo fecham os 30 em 200 de 200 sementes, em até 120 ondas. O BALANCE registra a onda média e o p90 de cada objetivo, para mostrar onde a cadeia espera.

## 7. Critério de pronto

| # | Critério | Como se mede |
|---|---|---|
| 1 | Este contrato, `CAMPANHA.md` e a linha 11 da matriz do `CONTRATO_LANCAMENTO.md` | o PR |
| 2 | Regras do §2 com teste de regra e determinismo; tabela igual ao `CAMPANHA.md` | `CampanhaTests` |
| 3 | Invariantes do §4 | `ExploitTests` e prova de mutação |
| 4 | Save schema 6 com migração do save real do schema 5 | `SaveTests` |
| 5 | TA-1, TA-2 e os portões do §6 medidos | `BalanceTests` e `BALANCE.md` |
| 6 | `MAPA_CODIGO.md`, cabeçalho do `Sim.cs` e `CHANGELOG.md` | o PR |
| 7 | `client/tools/verify.sh full` verde | saída no PR |

**Fica para a tela:** lista de objetivos por aeroporto, objetivo ativo na HUD, tela de sucesso e o evento `campaign_objective_completed` no diário.
