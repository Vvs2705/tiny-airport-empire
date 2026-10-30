# Balanceamento — medição do bot (Fase B, 2026-10-29)

Doc vivo: toda mudança de número ou de regra de economia entra aqui no mesmo commit, com a tabela de antes e de depois. Os números do jogo moram no `Tuning` (`client/Assets/_TA/Scripts/Core/Defs.cs`), e o contrato (`CONTRATO_V01.md` §4) acompanha.

**Como medir:** `dotnet test client/tools/coretests --filter "FullyQualifiedName~BalanceTests"` (o teste é `[Explicit]`: 200 sementes × 10 ondas por perfil, ~5 s). Ele imprime a tabela abaixo e falha se o portão não passar. O build faz o mesmo com a semente 1: `TinyAirport.exe -batchmode -nographics -autoplay 10`.

**Portão da Fase B (`CONTRATO_V01.md` §8):** TA-1 = embarque médio por voo ≥ 85% com o bot Bom e ≥ 60% com o Lento; TA-2 = receita do Bom ≥ 1,10 × a do Parado, nas mesmas sementes.

**Perfis (`Bot.cs`):** Bom decide a cada 1,0 s; Lento (×0,4) a cada 2,5 s; Parado é o Bom que nunca move funcionário. A política é a do contrato §7, literal: se uma estação está crítica e outra tem folga, move o funcionário da estação com mais folga; senão, toca a estação sem funcionário que tem fila; e compra a contratação ou o upgrade mais barato que couber. Duas leituras que o contrato não dizia: o bot só move para uma estação onde haja posto livre para o funcionário (senão ele ficaria parado lá), e não compra upgrade da loja com a loja fechada.

**Colunas:** receita = Cash ganho em 10 ondas (tarifas, loja e bônus, sem descontar gastos); movimentos e toques por partida; marco = minuto de jogo em que o terminal chegou ao marco (média das sementes que chegaram).

## 1. Antes: os números do contrato (200 sementes × 10 ondas)

| perfil | embarque médio | p10 | receita | movimentos | toques | marco 1 | marco 2 | marco 3 |
|---|---|---|---|---|---|---|---|---|
| Bom | 99.8% | 99.6% | 4293 | 1.0 | 414 | 16.2 min | - | - |
| Lento | 97.5% | 97.0% | 4118 | 4.2 | 403 | 16.5 min | - | - |
| Parado | 99.8% | 99.6% | 4293 | 0.0 | 414 | 16.2 min | - | - |

Ganho de mover (Bom/Parado) = **1,000**. TA-1 passa com sobra; **TA-2 falha**.

**Diagnóstico:** com 1 posto por estação e 3 funcionários para 3 estações essenciais (check-in, segurança, portão), não há para onde mover: um 2º funcionário numa estação de 1 posto fica parado, e o Parado já tem uma pessoa em cada estação. Além disso, com +2 passageiros por onda, a capacidade comprada cresce mais rápido que a demanda e quase nada fica crítico (o Bom moveu 1 vez por partida). O diferencial só existe se a estação tiver **mais balcões que funcionários** e se as ondas apertarem.

## 2. Decisões (cada uma medida; todas reversíveis)

**D-B1. Balcões da planta (novo número, `Tuning.PostosBase`): check-in 2, segurança 2, loja 1, portão 1.** O aeroporto tem 2 balcões de check-in e 2 raios-x, mas só 3 funcionários: mover alguém para o 2º balcão dobra a estação no pico. O upgrade de Capacidade continua +1 balcão por nível (2 níveis). Sozinho (100 sementes), não muda nada (ganho 1,000): falta pressão.

**D-B2. Mais passageiros por onda: +4 por onda (era +2), teto 36 (era 30).** Varredura com D-B1 (100 sementes × 10 ondas; o contrato manda o bot seguir a política literal):

| variante | Bom | Lento | Parado | ganho |
|---|---|---|---|---|
| 14 +3, teto 30 | 99.3% | 76.1% | 96.5% | 1,108 |
| 14 +3, teto 36 | 98.6% | 67.0% | 93.0% | 1,187 |
| 12 +3, teto 36 | 99.3% | 80.2% | 94.7% | 1,150 |
| 16 +3, teto 40 | 97.9% | 41.5% | 90.0% | 1,249 |
| 14 +3, chegadas até fechamento − 80 s | 98.5% | 80.7% | 97.7% | 1,038 |
| 14 +3, bônus de pontualidade 50% | 99.3% | 78.2% | 97.2% | 1,118 |
| 16 +3, portão com 2 balcões | 99.6% | 50.3% | 96.2% | 1,120 |
| 10 +4, teto 30 (200 sementes) | 99.6% | 86.4% | 96.1% | 1,125 |
| **10 +4, teto 36 (200 sementes)** | **99.2%** | **73.6%** | **92.0%** | **1,259** |

Escolhido **10 +4, teto 36**: mantém a onda 1 do contrato (10 por voo, a do tutorial), passa os três portões com folga (TA-2 1,26 contra 1,10; Lento 73,6% contra 60%) e cresce até o avião regional cheio na onda 7. O teto 30 passa, mas com o TA-2 a 1,125 de 1,10, sem margem para o playtest.

**D-B3. O jogo começa com 0 funcionários e o toque manual (a proposta do contrato §3; o GDD §5.5 dizia 3).** Mesmos números de D-B1 e D-B2, 200 sementes × 10 ondas:

| começo | Bom | Lento | Parado | ganho |
|---|---|---|---|---|
| **0 funcionários (escolhido)** | 99.2% | 73.6% | 92.0% | **1,259** |
| 3 funcionários | 99.2% | 92.6% | 92.4% | 1,254 |

O TA-2 sai igual. Com 0, o bot também mede o toque manual e a 1ª contratação, que são os passos 2, 3, 5 e 6 do tutorial; com 3, o Parado nunca toca (0 toques) e o verbo manual some do jogo. O Lento sofre mais com 0 (ele move e não toca rápido o bastante), mas fica 13,6 pontos acima do portão. Reversível: `Tuning.FuncionariosIniciais`.

**D-B4. A política do bot fica literal.** Medida e descartada a variante "não esvaziar estação com fila" (só tirar o funcionário de uma estação vazia ou que fica com outro): com 10 +4, teto 36, ela dá Bom 95,6%, Lento 91,1%, Parado 91,9% e ganho 1,149. Ajuda o Lento, mas encolhe o TA-2 e muda a regra do contrato.

**D-B5. Regras que o contrato não tinha número (medidas junto com o resto):**
- o embarque abre **60 s antes** do fechamento do portão (`Tuning.AbreEmbarqueAntes`): o portão concentra o trabalho no fim de cada voo, e é aí que mover a equipe para o portão vale;
- Velocidade = −12% **multiplicativo** por nível (× 0,88ⁿ; nível 5 = 53% do tempo);
- estado da estação: uma estação sem funcionário conta 1 servidor (o toque do jogador) na estimativa de "no ritmo atual"; sem isso, toda fila de toque ficaria crítica e o vermelho deixaria de dizer qual voo está em risco;
- renda online por média móvel de 180 s (uma onda), base do cofre offline.

## 3. Depois: o padrão do `Tuning` (200 sementes × 10 ondas)

| perfil | embarque médio | p10 | receita | movimentos | toques | marco 1 | marco 2 | marco 3 |
|---|---|---|---|---|---|---|---|---|
| Bom | 99.2% | 98.8% | 5978 | 4.0 | 772 | 13.6 min | - | - |
| Lento | 73.6% | 70.3% | 3523 | 39.3 | 576 | 14.5 min | - | - |
| Parado | 92.0% | 91.7% | 4748 | 0.0 | 433 | 13.8 min | - | - |

Ganho de mover (Bom/Parado) = **1,259**. **TA-1 e TA-2 passam.**

`-autoplay 10` (semente 1): `embarque_bom=99.4% embarque_lento=74.5% embarque_parado=91.8% ganho_mover=1.268 receita_bom=6001 moves_bom=4 marcos_bom=13.5min,-,- retomada=igual`.

Longo prazo (200 sementes × 20 ondas, 60 min de jogo): Bom 99,6%, Lento 74,9%, Parado 87,5%, ganho 1,422; marco 2 aos 34 min (Bom); o marco 3 (1.500 embarcados) fica para depois de 1 h.

## 4. Para medir com gente (o bot é mais rápido que a pessoa)

- K2 (≥ 60% agem na estação crítica em até 60 s) só sai do diário CSV de playtest (`client/tools/diario_report.py`).
- O bot toca uma estação por segundo; uma pessoa toca mais rápido no começo e cansa depois. Se o playtest mostrar a onda 1 pesada demais com 0 funcionários, baixe `PaxInicial` ou o preço da 1ª contratação antes de mexer no TA-2.
- No Forge, a pessoa foi 1,7–3,7× mais lenta que o bot: os minutos dos marcos acima são o limite inferior.

## 5. Fase C: tripulação, pátio e manutenção (2026-10-29, `CONTRATO_FASE_C.md`)

**Como medir:** `dotnet test client/tools/coretests --filter "FullyQualifiedName~BalanceTests"` (todos `[Explicit]`, ~6 min). `Antes_RegrasDaV01` roda as mesmas sementes com as regras novas desligadas (`Tuning.Regra* = false`); `Portao_TA1_TA2` roda com elas ligadas; `Portao_FaseC_PlanejadorContraAutomatico` é o portão do GDD §5.13; `Varredura_FaseC` é a tabela da D-C1.

**Portão do §5.13:** a receita do Planejador (o Bom que escala o tripulante mais descansado, faz a checagem rápida quando ela cabe antes do embarque e manda o avião pendente ao hangar) ≥ 1,10 × a do Automático (o mesmo Bom, com tudo no padrão do núcleo), nas mesmas 200 sementes. **20 ondas (1 h de jogo), não 10:** com 15 de desgaste por trecho, o avião só fica pendente na 7ª volta; em 10 ondas a manutenção quase não aparece.

### D-C1. Varredura (100 sementes × 20 ondas; cada sistema sozinho, com o pátio ligado)

| variante | emb. planejador | emb. automático | receita planejador | receita automático | ganho | sem tripulação p/a | hangar p / inspeção a |
|---|---|---|---|---|---|---|---|
| tripulação N=3 trechos, descanso 180 s (contrato) | 99.7% | 97.2% | 16569 | 16248 | 1,020 | 0/2 | — |
| tripulação N=2, 180 s | 99.7% | 96.8% | 16569 | 16081 | 1,030 | 5/7 | — |
| tripulação N=3, 240 s | 97.3% | 97.2% | 16248 | 16248 | 1,000 | 6/7 | — |
| tripulação N=2, 240 s | 96.9% | 96.9% | 16083 | 16193 | 0,993 | 31/31 | — |
| tripulação N=2, 300 s | 74.2% | 77.0% | 11341 | 11928 | 0,951 | 18/19 | — |
| manutenção W=10/trecho, checagem −5 por 15, inspeção 90 s (contrato) | 94.7% | 95.5% | 15489 | 15537 | 0,997 | — | 2/4 |
| W=10, −5 por 15, inspeção 110 s | 94.7% | 89.7% | 15489 | 14409 | 1,075 | — | 2/4 |
| W=10, −10 por 10, inspeção 110 s | 99.7% | 89.7% | 16569 | 14409 | 1,150 | — | 0/4 |
| **W=15, −10 por 10, inspeção 110 s (escolhida)** | **94.7%** | **84.8%** | **15489** | **13451** | **1,151** | — | **2/6** |
| W=12, −6 por 10, inspeção 110 s | 94.7% | 89.7% | 15489 | 14409 | 1,075 | — | 2/4 |
| W=20, −10 por 10, inspeção 110 s | 88.6% | 79.7% | 14239 | 12429 | 1,146 | — | 4/8 |

**Diagnóstico da manutenção:** com a inspeção de 90 s, o avião pendente ainda embarca nos 10 s que sobram, e o portão com upgrades embarca 30 pessoas nesse tempo: deixar chegar a 100% não custava nada (0,997). A inspeção passou a **segurar o avião o turnaround inteiro (110 s > os 100 s de pátio)**: pendente perde o voo dele (só a receita dele) e sai com metade do desgaste. Com W=15 e a checagem tirando 10, a checagem em todo turnaround só atrasa o desgaste (+5 por trecho), então o hangar ainda é preciso: o Planejador manda o pendente ao hangar (zera) no lugar da inspeção (tira 50), e o Automático volta a ficar pendente a cada 4 trechos. É a decisão do GDD: prevenir no pátio e escolher a hora do hangar. W=10 com −10 dá o mesmo ganho, mas a checagem empata com o desgaste e o hangar nunca é usado.

### D-C2. A tripulação fica só visual (portão do §5.13, por sistema)

A escala automática do contrato ("o 1º livre na ordem do quadro") se corrige sozinha: na 1ª vez que os dois primeiros descansam juntos, um voo fica sem piloto, e depois disso as folgas já ficam desencontradas. Em 20 ondas o Automático perde 1 voo por partida, e escalar bem rende **1,02**. Descanso mais longo (240–300 s) não cria decisão: a tripulação de 3 + 3 deixa de dar conta, e o Planejador e o Automático perdem os mesmos voos. Com as duas regras ligadas o ganho seria 1,180, mas quase todo da manutenção. Pela regra do GDD ("o sistema que não gerar decisão fica só visual"), **`Tuning.RegraTripulacao = false`**: a escala, os trechos, o descanso, a contratação e os eventos continuam no núcleo (a tela mostra), mas a falta de tripulação não segura o embarque. Volta a ter regra, com uma linha, quando a frota crescer (narrow 2+2, mais voos por onda): é a escassez que cria a decisão. Revisão de design: TA-8.

### D-C3. O pátio começa com 1 pessoa

Com 1 pessoa, a bagagem do voo 1 espera o desembarque e a limpeza do voo 2: o voo 1 decola 4 s atrasado na 1ª onda e perde o bônus de pontualidade. O jogador (e o bot) contrata a 2ª pessoa por 40 Cash, e o pátio deixa de atrasar no Vale Verde regional. O pátio pesa de novo com aviões maiores e com os upgrades de veículo. O pátio não entra no portão do §5.13 (o GDD fala só da tripulação e da manutenção).

### Antes e depois do TA-1 e do TA-2 (200 sementes × 10 ondas)

Antes (regras da Fase C desligadas, `Antes_RegrasDaV01`, igual à §3):

| perfil | embarque médio | p10 | receita | movimentos | toques | marco 1 | marco 2 | marco 3 |
|---|---|---|---|---|---|---|---|---|
| Bom | 99.2% | 98.8% | 5978 | 4.0 | 772 | 13.6 min | - | - |
| Lento | 73.6% | 70.3% | 3523 | 39.3 | 576 | 14.5 min | - | - |
| Parado | 92.0% | 91.7% | 4748 | 0.0 | 433 | 13.8 min | - | - |

Ganho de mover = 1,259.

Depois (padrão do `Tuning`: pátio e manutenção com regra, tripulação visual; os três perfis planejam):

| perfil | embarque médio | p10 | receita | movimentos | toques | marco 1 | marco 2 | marco 3 |
|---|---|---|---|---|---|---|---|---|
| Bom | 99.3% | 99.0% | 5969 | 4.7 | 809 | 13.7 min | - | - |
| Lento | 74.5% | 71.6% | 3581 | 32.5 | 578 | 15.8 min | - | - |
| Parado | 92.2% | 92.0% | 4783 | 0.0 | 473 | 13.7 min | - | - |

Ganho de mover = **1,248**. O TA-1 não piorou (Bom 99,2 → 99,3; Lento 73,6 → 74,5). O TA-2 caiu 0,011 (−0,9%) e continua com folga sobre o 1,10. A causa é o pátio, não a equipe: só com o pátio ligado o ganho é 1,248, e só com a manutenção é 1,260. A 2ª pessoa de pátio (40 Cash) e o bônus perdido na 1ª onda mudam a ordem das compras do Parado, que ganha 35 Cash em 10 ondas; o Bom perde 9. Começar com 2 pessoas de pátio devolve o 1,260, mas o Lento cai para 72,0%: as duas saídas mexem ±2 pontos por causa da ordem das compras, e a escolhida mantém o pátio como decisão no começo do jogo.

### Portão do §5.13 (200 sementes × 20 ondas, padrão do `Tuning`)

| perfil | embarque médio | receita | voos sem tripulação | inspeções | hangar | checagens | trocas de escala |
|---|---|---|---|---|---|---|---|
| Planejador | 94.7% | 15488 | 0.0 | 2.0 | 2.0 | 32.7 | 48.0 |
| Automático | 84.8% | 13451 | 0.0 | 6.0 | 0.0 | 0.0 | 0.0 |

Ganho de planejar = **1,151: passa.** Por sistema: só a tripulação 1,020 (fica visual, D-C2); só a manutenção 1,151; as duas com regra 1,180. As 2 inspeções do Planejador são as que o mecânico começa sozinho no pouso e que o hangar substitui no segundo seguinte.

### Prova de mutação (`python client/tools/mutacao.py`)

Cada trava sai do código, o `dotnet test` roda e algum teste tem de cair (mutante que não compila conta como vivo). Resultado em 2026-10-29: **9 de 9 mortos**.

| trava retirada | testes que caíram |
|---|---|
| manutenção pendente segura o embarque | `Invariante_ManutencaoPendente_CustaSoAReceitaDoVoo`, `Manutencao_Desgaste_AvisoEm80_PendenteEm100_InspecaoAutomatica` |
| falta de tripulação segura o embarque (com a regra ligada) | `Invariante_FaltaDeTripulacao_CustaSoAReceitaDoVoo`, `Tripulacao_SemNinguemLivre_EmbarqueEspera_AvisaUmaVez` |
| checagem só com Cash | `Invariante_Reparo_SoCashETempo_NuncaAnuncio`, `Manutencao_ChecagemRapida_...` |
| hangar só com Cash | `Invariante_Reparo_SoCashETempo_NuncaAnuncio`, `Manutencao_Hangar_...` |
| offline não gasta desgaste | `Invariante_Offline_NaoGastaDesgasteTrechoNemDescanso_NemPerdeVoo` |
| nenhum tripulante sai de sorteio | `Invariante_NenhumTripulanteSaiDeSorteio`, `Tripulacao_Contratacao_...` |
| perder voo não cobra Cash | 5, entre eles `Invariante_PerderVoo_CashSoCaiNaCompra` e os dois da Fase C |
| descanso obrigatório depois de N trechos | 4, entre eles `Exploit_FecharEAbrir_NaoRenovaAChecagemNemZeraODescanso` |
| anúncio nunca repara | `Invariante_Reparo_SoCashETempo_NuncaAnuncio` |

### Para medir com gente

- Se o aviso de 80% não fizer ninguém mandar o avião ao hangar antes de ficar pendente, o aviso precisa ser maior na tela, não a regra mais fraca.
- A tripulação visual é o primeiro candidato a voltar com regra quando a Costa Azul trouxer mais voos: medir de novo com `RegraTripulacao = true` antes de ligar.
