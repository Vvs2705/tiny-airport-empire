# Contrato da Fase C3: 3 aeroportos, 12 rotas e 4 incidentes no núcleo (2026-10-30)

Fonte da verdade: `GDD.md` §4.1 (itens 1, 2 e 9), §5.4 (quadro), §5.7 (rotas), §5.8 (incidentes), §5.10 (aeroportos), §5.13 (portão de design) e `CONTRATO_LANCAMENTO.md` linhas 1, 2 e 9. Continua os contratos das Fases C e C2: nada de lá muda de regra.

Todo número é **hipótese**: o bot mede e o `BALANCE.md` §7 registra.

## 1. O que entra

- **Tabelas no `Tuning`:** `Tuning.Rotas` (12 linhas) e `Tuning.Aeroportos` (3 linhas), dados e não código. Cada linha tem um `Id` estável (`vv_campinas`, `ca_salvador`, `ag_manaus`) e um `Nome` de trabalho. O save grava o índice, que só cresce no fim. A D-13 troca só o `Nome`.
- **Aeroportos:** a empresa é uma só. Viajar troca o aeroporto em operação: Cash, equipe, níveis e frota vão junto; mudam as rotas, o ritmo das chegadas, os incidentes e o desgaste. Decisão pela opção mais simples: o GDD não diz se cada aeroporto tem terminal próprio, e níveis por aeroporto exigiriam 3 cópias do estado. O TA-7 (aeroporto antigo vira hub) fica fora: ele depende do fim da campanha.
- **Rotas:** o quadro continua com 2 voos por onda (2 vagas, 2 aviões). A rota é o que a vaga voa. `Sim.Programa(vaga, rota)` vale a partir da próxima onda.
- **Incidentes:** os 4 do GDD §5.8, um por vez, sorteados da semente na virada da onda e sempre com aviso.

## 2. Aeroportos

| Id | Nome de trabalho | Abre | Viagem | Rotas | Desafio |
|---|---|---|---|---|---|
| `vv` | Vale Verde | início | — | 4 curtas | fluxo e equipe móvel; sem incidente, salvo na rota de risco |
| `ca` | Costa Azul | marco 2 | 1.200 | 3 turísticas + 1 conexão | pico de chegada, gasto alto em loja e comida, maresia (desgaste × 1,5), incidentes |
| `ag` | Aurora Global | marco 3 | 3.000 | 2 conexões + 2 longas | conexões sem check-in, longas que tiram o avião por 2 ondas, incidentes mais frequentes |

`Sim.Viajar(a)` paga a viagem uma vez (voltar é grátis) e vale na próxima onda. Na chegada, as 2 rotas sem preço do aeroporto abrem e entram nas vagas.

## 3. Rotas

Campos: `Extra` (s além do `Tuning.Trecho`: a distância), `Cada` (voa a cada N ondas: a frequência), `Pax` e `Tarifa` (fatores de receita), `Gasto` (fator de venda em loja e comida), `Pico` (parte da janela de chegada sem ninguém: 0,5 = todo mundo na 2ª metade), `Bonus` (soma no bônus de pontualidade), `Risco` (0 a 2) e `Aeronave` (silhueta para a View).

| # | Id | Nome | Tipo | Marco | Preço | Extra | Cada | Pax | Tarifa | Gasto | Pico | Bônus | Risco |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | `vv_campinas` | Campinas | curta | 0 | 0 | 0 | 1 | 1,0 | 1,0 | 1 | 0 | 0 | 0 |
| 1 | `vv_ribeirao` | Ribeirão Preto | curta | 0 | 0 | 0 | 1 | 1,0 | 1,0 | 1 | 0 | 0 | 0 |
| 2 | `vv_curitiba` | Curitiba | curta | 1 | 250 | 10 | 1 | 1,3 | 1,0 | 1 | 0 | 0 | 0 |
| 3 | `vv_bh` | Belo Horizonte | curta | 2 | 500 | 20 | 1 | 0,9 | 1,5 | 1 | 0 | 0 | 1 |
| 4 | `ca_salvador` | Salvador | turística | 2 | 0 | 10 | 1 | 1,2 | 1,1 | 1,5 | 0,5 | 0 | 1 |
| 5 | `ca_recife` | Recife | turística | 2 | 0 | 20 | 1 | 1,1 | 1,2 | 1,5 | 0,5 | 0 | 1 |
| 6 | `ca_fortaleza` | Fortaleza | turística | 2 | 800 | 20 | 1 | 1,5 | 1,1 | 2 | 0,6 | 0 | 1 |
| 7 | `ca_brasilia` | Brasília | conexão | 3 | 1.000 | 0 | 1 | 1,2 | 1,4 | 1 | 0,7 | 0,25 | 1 |
| 8 | `ag_guarulhos` | Guarulhos | conexão | 3 | 0 | 0 | 1 | 1,3 | 1,4 | 1 | 0,7 | 0,25 | 1 |
| 9 | `ag_galeao` | Galeão | conexão | 3 | 0 | 10 | 1 | 1,4 | 1,4 | 1 | 0,7 | 0,25 | 1 |
| 10 | `ag_manaus` | Manaus | longa | 3 | 1.500 | 180 | 2 | 2,0 | 2,0 | 1,2 | 0 | 0 | 2 |
| 11 | `ag_lisboa` | Lisboa | longa | 3 | 2.500 | 180 | 2 | 2,2 | 2,2 | 1,2 | 0 | 0 | 2 |

- **Curta:** `Pax` × `Tarifa` é a escolha entre volume e tarifa. A distância come o turnaround: quem volta mais tarde pressiona o pátio.
- **Turística:** chegadas concentradas e gasto maior: a alimentação e a loja pagam mais.
- **Conexão:** o passageiro já fez check-in na origem e entra na segurança, em bloco no fim da janela. O bônus de pontualidade é maior.
- **Longa:** o avião fica fora por 2 ondas (`Extra` = 1 onda). Na onda em que ele está fora, a vaga não tem voo (`Voo.Total` = 0): ninguém chega e nada se perde.
- **Risco:** soma na chance de incidente da onda (§4). É o preço das rotas melhores.

## 4. Incidentes

Na virada de cada onda, com chance = `Aeroporto.Incidentes` + `Tuning.RiscoChance` × (soma do risco das 2 rotas), teto `Tuning.IncidenteTeto`, sai um incidente da semente. Depois de um incidente, `Tuning.IncidenteFolga` ondas sem nenhum. O tipo sai dos pesos do aeroporto. A vaga atingida é a de maior risco que tem voo. Todos dão `Tuning.AvisoSeg` = 30 s de aviso, mostrados em `sim.Inc`, que fica null até a hora do aviso.

| Tipo | O que acontece | Saída | Números iniciais |
|---|---|---|---|
| Frente fria (TA-4) | a pista fecha: ninguém embarca, pousa nem decola; quem fecha com a pista fechada perde o bônus | `Sim.Adianta()` no aviso: o embarque abre já e o portão fecha no início do fechamento | fecha 45 s antes do portão da vaga, por 60 s |
| Falha de equipamento | check-in, segurança ou portão fica 3× mais lento | `Sim.Repara()`: Cash e o mecânico da Fase C por 6 s; ou o Edu presente na estação repara de graça | 60 s, reparo 30 Cash |
| Pico de demanda | grupo extra chega de uma vez ao check-in logo depois das chegadas normais | levar gente ao check-in antes | +35% da onda |
| Atraso de conexão | grupo extra entra na segurança 45 s antes do portão e paga tarifa dobrada | `Sim.Prioriza()`: o grupo fura a fila em todas as estações | +25% da onda |

## 5. Regras de justiça e invariantes (testes negativos no `ExploitTests`)

1. **Voo perdido custa só a própria receita**, também com pista fechada, falha, grupo extra ou rota longa: nenhum Cash sai do saldo.
2. **Offline nunca perde voo** nem anda incidente, reparo ou trecho de rota longa: o relógio do quadro não anda com o jogo fechado.
3. **Nada pago é aleatório:** preço, marco e efeito de rota e aeroporto são os mesmos em qualquer semente. A semente só decide incidente, que não se compra nem se evita pagando.
4. **Reparo só com Cash:** sem Cash não há reparo pago, e anúncio nunca repara nem encurta incidente. Edu presente é trabalho, não pagamento.
5. **Incidente sempre tem aviso prévio** (≥ `AvisoSeg`, medido em 200 sementes) **e nunca derruba o progresso:** Cash só cai na compra, e embarcados, marco, níveis, equipe e rotas abertas nunca recuam.
6. **Sem derrota inevitável:** um incidente por vez, com folga depois; fechar e abrir o app não evita nem renova incidente.

## 6. Save

Schema 4. Grava aeroporto atual, aeroportos pagos, rotas abertas, rota programada por vaga, a rota e os dados de cada voo da onda, a volta de cada avião, o incidente em andamento, o reparo e a última onda com incidente. O schema 3 migra sem conta: chave ausente = Vale Verde, rotas iniciais, nenhum incidente. Teste com um save real gravado pelo código do schema 3 (semente 7, 1.000 s, depois do marco 1): migrar dá o mesmo jogo que jogar até ali com o código novo.

## 7. Bot e portões

- **TA-1 e TA-2** (200 sementes × 10 ondas, desbloqueio pelo marco): Bom ≥ 85%, Lento ≥ 60%, Bom/Parado ≥ 1,10.
- **Começo intacto:** com rotas, aeroportos e incidentes fechados (`BalanceTests.SemFaseC3`), a tabela do bot é a da `main`, número a número.
- **Portão do §5.13 por sistema com escolha** (200 sementes × 20 ondas, tudo aberto desde o começo, um sistema por vez): **rotas** (qual rota cada vaga voa), **aeroportos** (quando viajar) e **incidentes** (reagir ao aviso, uma decisão só: a saída de cada tipo sai na tabela, sem portão próprio, como as estações da C2). Planejador ≥ 1,10 × Automático, nas mesmas sementes. Abaixo disso, o sistema fica **só visual** (`Tuning.RegraRotas`, `RegraAeroportos`, `RegraIncidentes`).
- **TA-6:** nenhuma rota domina: em cada aeroporto, a melhor rota rende ≤ 1,20 × a segunda, nas mesmas sementes.

## 8. Critério de pronto

| # | Critério | Como se mede |
|---|---|---|
| 1 | Este contrato e as linhas 1, 2 e 9 do `CONTRATO_LANCAMENTO.md` | o PR |
| 2 | Regras dos §2 a §4 com testes | `RotasIncidentesTests` |
| 3 | Invariantes do §5 | `ExploitTests` |
| 4 | Save schema 4 com migração do save real do schema 3 | `SaveTests` |
| 5 | TA-1, TA-2, TA-6 e os portões do §7 medidos | `BalanceTests` e `BALANCE.md` §7 |
| 6 | `MAPA_CODIGO.md`, cabeçalho do `Sim.cs` e `CHANGELOG.md` | o PR |
| 7 | `client/tools/verify.sh full` verde | saída no PR |
