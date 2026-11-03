# Contrato da Fase C7: passe de temporada, trilha gratuita, no núcleo (2026-11-02)

Fonte da verdade: `GDD.md` §4.1, §6.1 e §6.4, com uma exceção registrada no §2 (D-C7a). A tabela está em `PASSE.md`. Continua os contratos das Fases C a C6 e D1: nada de lá muda de regra, menos o teto do 2× no save (§5).

Todo número é **hipótese**: o bot mede e o `BALANCE.md` registra.

**Medido em 2026-11-02 (`BALANCE.md` §12):** sem compra, o `LentoCompleto` fecha os 30 níveis em 200 de 200 sementes, no dia 20,0 do jogo em média, 21,8 no p90 e 23,9 no pior; o `Completo` fecha no dia 18,2, e quem nunca olha para as missões (Bom) chega ao nível 28,9. Com o passe desligado, a tabela do bot é a da Fase D1, número a número. Os portões anteriores seguem verdes; TA-2 sobe de 1,431 para 1,824, porque o 2× do passe entra na receita. **A 1ª tabela fechava em 163 de 200** (D-C7d no `BALANCE.md`): o §2 já está na versão final. Mudou regra ou número? Atualize aqui e no `BALANCE.md` no mesmo commit.

## 1. O que entra

- **Uma temporada de 28 dias do jogo**, com **30 níveis** na trilha gratuita (`Tabelas.Temporadas`).
- **Pontos de temporada** saem do que o núcleo já mede: voo que partiu, diária, semanal e objetivo da campanha. São barra, não moeda (GDD §6.1).
- **Prêmio fixo por nível**, de 3 tipos: Cash, tempo de 2× renda ou cosmético. Fora do `Ganha`: o 2× não dobra e o cofre offline não aprende com ele.
- **Ponto de entrada da trilha premium:** a temporada guarda o produto da loja (`season_pass_s1`, já no catálogo da Fase D1). O conteúdo e a regra ficam para a frente da loja.
- **Fora daqui:** tela, textos, trilha premium e campanha sazonal.

## 2. Regras

| Regra | Como funciona |
|---|---|
| Relógio | A temporada corre em `DiaJogo` (8 ondas, 24 min jogados). Abre no dia do jogo em que o jogo novo começa e fecha 28 dias depois. Nada lê a hora do aparelho, nem o `Dia` local |
| Pontos | Voo que partiu: 2, com ou sem gente a bordo. Diária: 25. Semanal: 70. Objetivo da campanha: 20. A barra para em 3 000 |
| Nível | 100 pontos por nível. O nível paga sozinho no fim do passo fixo em que fecha, uma vez, e emite `Ev.PasseNivel`. Não há o que resgatar nem prazo |
| Prêmio | Cash fixo da tabela, minutos de 2× somados ao 2× ligado (teto de 1 h) ou cosmético na posse para sempre (`sim.Tem(id)`). Nada sorteado |
| Virada | No 1º passo depois do 28º dia, a barra e os níveis zeram e abre a temporada seguinte da tabela. Cash, 2× e cosmético ficam |
| Fechado | Sem temporada seguinte, o passe fica fechado: nada conta e o jogo segue. A temporada que chegar numa atualização abre no dia em que chegar |
| Fontes que não contam | Anúncio, compra, Gems, cofre offline e o próprio passe. Nenhum ponto se compra |

**D-C7a. A temporada é do jogo, não do calendário.** O GDD §6.4 pede início e fim em UTC fixo no build. Este contrato troca isso por 28 dias do jogo, pelo pedido da frente: nada de hora de parede dentro da simulação, como na D-C6a das missões. O que se ganha: reproduzível no bot, imune ao relógio do aparelho, e faltar um dia deixa de existir. O que se perde: a temporada não é a mesma para todo mundo, e quem joga 30 min por dia leva cerca de 22 dias de calendário para atravessá-la. A conta do GDD §6.4 (3 diárias por dia de calendário) fica substituída pela medida do §6. Voltar ao calendário é trocar `DiaJogo` por um dia UTC em `Sim.Passe.cs` e medir de novo.

**D-C7b. Prêmio pago na hora.** O GDD recomenda um painel de resgate por 7 dias depois do fim. Com o nível pagando sozinho, não sobra nada a resgatar nem a perder, e o "prazo para resgatar" não existe. A tela mostra o nível e o prêmio quando ele entra.

**D-C7c. O ponto do voo não olha quem embarcou.** Se olhasse, perder o voo custaria também ponto de temporada, e o GDD §5.14 diz que perder o voo custa só a receita dele.

## 3. Núcleo e API

- Determinístico, passo fixo de 1/30 s. Nada sai da semente: a tabela é a mesma em todo jogo.
- Ler: `sim.T.Temporadas` (`Id`, `Produto`, `Dias`, `Gratis[nivel - 1]` com `Premio`, `Valor` e `Id`), `sim.Temporada` (null = fechado), `sim.DiasRestantesPasse`, `sim.PontosPasse`, `sim.NivelPasse`, `sim.T.PassePontosNivel`, `sim.Tem(id)` para o cosmético.
- Evento novo no fim do `Ev`: `PasseNivel` (A = temporada, B = nível, C = Cash, s de 2× ou 1 cosmético) → `season_reward_claimed`.
- `Tuning.RegraPasse = false` = só visual: conta e mostra, sem pagar.
- **Trilha premium (não implementada):** uma 2ª coluna de `Nivel` na `Temporada`, paga em `PassoPasse` com `Tem(Produto)` do nível 1 ao `NivelPasse` (o retroativo do GDD §6.4), com o próprio contador no save. O comentário `ponytail:` em `Sim.Passe.cs` marca o lugar.

## 4. Invariantes (testes negativos no `ExploitTests`)

1. **Prêmio fixo:** a mesma tabela em toda semente. O 2× não dobra e o cofre não aprende com ele.
2. **Paga uma vez:** fechar e abrir o jogo, em qualquer ponto, dá o mesmo passe de nunca fechar.
3. **Faltar um dia não custa nada:** 30 ou 400 dias fora, relógio do aparelho na frente ou para trás: a mesma temporada, os mesmos dias restantes e pontos, nada pago de novo, nenhum Cash a menos. O offline não dá ponto.
4. **Ponto não se compra:** crédito na loja (Gems e o próprio `season_pass_s1`), gasto de Gems, anúncio e cofre não dão ponto nem nível.
5. **Voo perdido não tira ponto:** o ponto do voo vem de ele partir.
6. **A virada não tira nada:** Cash e cosmético ficam. Sem a temporada seguinte, nada quebra.
7. **Sem compra fecha:** o bot mais lento que joga pela campanha e pelas missões fecha os 30 níveis numa temporada (§6).

## 5. Save

Schema 9. `passe=temporada,início,pontos,pagos`: o índice da temporada, o dia do jogo em que abriu (-1 = fechado), os pontos e os níveis já pagos. Tudo limitado: temporada fora da tabela fecha o passe, início no máximo hoje e no mínimo 28 dias atrás, pontos no teto da barra, pagos no máximo o nível alcançado. O cosmético do passe vai na mesma `posse=` das compras.

O schema 8 migra sem conta: chave ausente = a temporada 1 abre no dia do jogo de agora, com 0 pontos e os 28 dias inteiros. Teste com um save real gravado pelo código do schema 8 (commit dd82ecf, semente 7, bot Bom, 2 790 s, dia do jogo 1): com `RegraPasse` desligada, migrar dá o mesmo jogo que jogar até ali com o código novo.

**Teto do 2× no save:** passa de 30 min para 1 h (2 × o booster), para o tempo do passe que chega com o booster ligado não se perder ao reabrir. Os saves dos schemas 1 a 7 continuam migrando pelo mesmo caminho.

## 6. Bot e portões

- **Começo intacto:** com `RegraPasse` desligada, a tabela do bot é a da Fase D1, número a número (`BalanceTests.Antes_FaseD1`).
- **Portão do passe** (`Portao_FaseC7_Passe`, roda no `balanco.yml`; 200 sementes × 224 ondas, a temporada inteira): o `LentoCompleto`, o bot mais lento que joga pela campanha e pelas missões, sem compra, fecha os 30 níveis em 200 de 200 sementes.
- **Os outros bots** (`Medida_FaseC7`): Completo, Missionário, Guiado, Bom, Lento e Parado, sem portão.
- **Os portões anteriores** (TA-1, TA-2, Fases C a C6, D1 e TA-6) medidos de novo com o passe pagando.

## 7. Critério de pronto

| # | Critério | Como se mede |
|---|---|---|
| 1 | Este contrato, `PASSE.md` e a linha 14 da matriz do `CONTRATO_LANCAMENTO.md` | o PR |
| 2 | Regras do §2 com teste de regra e determinismo; tabela igual ao `PASSE.md` | `PasseTests` |
| 3 | Invariantes do §4 | `ExploitTests` e prova de mutação |
| 4 | Save schema 9 com migração do save real do schema 8 | `SaveTests` |
| 5 | Portão do §6 medido, e os anteriores de novo | `BalanceTests` e `BALANCE.md` §12 |
| 6 | `MAPA_CODIGO.md`, cabeçalho do `Sim.cs` e `CHANGELOG.md` | o PR |
| 7 | `client/tools/verify.sh full` verde | saída no PR |

**Fica para a tela:** a barra com os 30 níveis e os prêmios à vista, os dias restantes, o aviso de nível novo, a imagem dos 3 cosméticos e os eventos no diário. **Fica para a loja:** a trilha premium.
