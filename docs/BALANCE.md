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

## 6. Fase C2: as 8 estações e os 6 perfis (2026-10-30, `CONTRATO_FASE_C2.md`)

**Como medir:** `dotnet test client/tools/coretests --filter "FullyQualifiedName~BalanceTests.Portao_|FullyQualifiedName~BalanceTests.Antes_FaseC2" --logger "console;verbosity=detailed"` (~10 min). No filtro entra só o `BalanceTests`: com um teste não `[Explicit]` junto, o NUnit pula os explícitos. `Antes_FaseC2` roda com as estações e os perfis novos nunca abrindo; `Portao_FaseC2_PorSistema` mede cada sistema sozinho, com tudo aberto desde o começo.

**Bots:** EquipePlanejada = o Bom que leva cada um para a estação principal do perfil quando a atual está normal, manda gente à oficina quando um avião está pendente e só tira alguém de uma estação nova em último caso. EquipeAutomatica = o mesmo Bom, com a equipe no padrão do núcleo. Os dois compram igual. A Fase C (escala, checagem e hangar) fica ligada nos dois.

### D-C2a. A especialização fica só visual

Com o especialista rendendo mais, o Parado deixa de sofrer: ele já tem a Ana no check-in e o Beto na segurança, na estação certa e de graça. O TA-2 cai abaixo do portão com qualquer fator, e a decisão de "quem vai para onde" não rende nada. Varredura (100 sementes × 10 ondas no TA, × 20 no ganho):

| fator do especialista | contratado | Bom | Lento | Parado | TA-2 | ganho da especialização |
|---|---|---|---|---|---|---|
| 1,15 | na estação do perfil | 98,7% | 81,2% | 96,6% | 1,079 | 1,195 |
| 1,25 | na estação do perfil | 98,7% | 82,4% | 98,4% | 1,049 | 1,140 |
| 1,5 | na estação do perfil | 99,9% | 84,5% | 99,6% | 1,069 | 1,222 |
| 1,15 | no 1º posto vazio | 98,7% | 81,2% | 96,6% | 1,098 | 1,051 |
| 1,25 | no 1º posto vazio | 98,7% | 82,4% | 98,7% | 1,055 | 1,005 |
| 1,5 | no 1º posto vazio | 99,9% | 84,5% | 99,6% | 1,069 | 1,000 |
| **desligada (escolhida)** | **no 1º posto vazio** | **99,7%** | **75,4%** | **92,2%** | **1,408** | — |

O ganho alto da 2ª a 4ª linha vinha só de abrir a loja com o Vendedor, não da especialização. **`Tuning.RegraEspecialista = false`**: os 6 perfis continuam fixos, nomeados e com a estação principal, mas não rendem mais nela. Volta a ter regra quando houver mais voos do que a equipe dá conta: é a mesma escassez que falta à tripulação.

### D-C2b. O contratado aparece no 1º posto vazio

O contrato dizia "na estação do perfil". Com isso, o padrão do núcleo já fazia o certo e nenhuma estação nova gerava decisão (bagagem 1,025, alimentação 1,000, oficina 1,061, despacho 1,026). Voltou a regra da v0.1: o contratado aparece no 1º posto vazio, e levar a pessoa para uma estação nova é a escolha do jogador. Na mesma medida, a alimentação passou a vender mais que a loja (50% com fome, refeição 8) e virou a estação principal do Vendedor.

### D-C2c. As 4 estações novas contam como um sistema

Cada estação sozinha não passa, mas a decisão é uma só: tirar gente do fluxo para uma estação nova. Com as 4 ligadas, oficina, despacho e paradas se somam, porque o voo que a oficina salva também paga o bônus do despacho e as vendas. **Ressalva:** isto é uma leitura do "por sistema" do GDD. Se o corte tiver de ser por estação, as 4 ficam só visuais (`RegraBagagem`, `RegraAlimentacao`, `RegraOficina` e `RegraDespacho` = false) e o TA-2 volta perto do 1,248.

| sistema (200 sementes × 20 ondas, tudo aberto) | emb. planejada | emb. automática | receita planejada | receita automática | ganho | regra |
|---|---|---|---|---|---|---|
| especialização | 95,0% | 95,0% | 18310 | 18309 | 1,000 | só visual |
| **as 4 estações juntas** | **99,7%** | **94,1%** | **22324** | **16342** | **1,366** | **ligada** |
| só bagagem | 94,4% | 94,1% | 16665 | 16342 | 1,020 | parte das 4 |
| só alimentação | 93,5% | 94,1% | 17258 | 16342 | 1,056 | parte das 4 |
| só oficina | 98,3% | 94,1% | 16936 | 16342 | 1,036 | parte das 4 |
| só despacho | 93,9% | 94,1% | 17145 | 16342 | 1,049 | parte das 4 |

### Antes e depois do TA-1 e do TA-2 (200 sementes × 10 ondas)

Antes (estações e perfis novos nunca abrem, `Antes_FaseC2`): **igual à tabela "depois" da §5, número a número**. É a prova de que o começo da v0.1 não mudou para o bot.

| perfil | embarque médio | p10 | receita | movimentos | toques | marco 1 | marco 2 | marco 3 |
|---|---|---|---|---|---|---|---|---|
| Bom | 99.3% | 99.0% | 5969 | 4.7 | 809 | 13.7 min | - | - |
| Lento | 74.5% | 71.6% | 3581 | 32.5 | 578 | 15.8 min | - | - |
| Parado | 92.2% | 92.0% | 4783 | 0.0 | 473 | 13.7 min | - | - |

Depois (padrão do `Tuning`: bagagem e alimentação abrem no marco 1, oficina e despacho no marco 2, que o Bom só alcança depois das 10 ondas):

| perfil | embarque médio | p10 | receita | movimentos | toques | marco 1 | marco 2 | marco 3 |
|---|---|---|---|---|---|---|---|---|
| Bom | 99.7% | 99.4% | 6913 | 4.7 | 981 | 13.7 min | - | - |
| Lento | 75.4% | 71.0% | 3838 | 38.0 | 569 | 15.8 min | - | - |
| Parado | 92.2% | 92.0% | 4911 | 0.0 | 372 | 13.7 min | - | - |

Ganho de mover = **1,408** (era 1,248). **TA-1 e TA-2 passam.** O Parado fica no mesmo embarque, porque nunca leva ninguém às estações novas; a Dani, que cai na loja pelo 1º posto vazio, é o que ele ganha.

`-autoplay 10` no build (semente 1): `embarque_bom=99.4% embarque_lento=73.7% embarque_parado=92.0% ganho_mover=1.438 receita_bom=6999 moves_bom=5 marcos_bom=13.7min,-,- retomada=igual`.

### Portão da Fase C, de novo (200 sementes × 20 ondas)

| perfil | embarque médio | receita | inspeções | hangar | checagens | trocas de escala |
|---|---|---|---|---|---|---|
| Planejador | 99.7% | 22293 | 2.0 | 0.0 | 35.0 | 44.0 |
| Automático | 89.8% | 20020 | 6.0 | 0.0 | 0.0 | 0.0 |

Ganho de planejar = **1,114** (era 1,151): continua passando, mais perto do limite. A oficina tira parte do valor do hangar: quem tem gente lá salva o voo do pendente sem ir ao hangar, e o Automático também usa a equipe. Só a manutenção 1,114; só a tripulação 1,030 (segue visual); as duas 1,149.

### Prova de mutação (`python client/tools/mutacao.py`)

As 4 travas novas entram no `mutacao.py`, que continua pegando as 9 da Fase C. Resultado em 2026-10-30: **13 de 13 mortos**.

| trava retirada | testes que caíram |
|---|---|
| perfil novo só pelo marco | 3, entre eles `Desbloqueio_PeloMarco_AntesDoMarco1EAV01` |
| preço fixo por perfil | 6, entre eles `Perfis_SeisFixos_TodosNoPrimeiroPostoVazio` e `Invariante_C2_NadaPagoEAleatorio_PerfilOrdemEPreco` |
| offline não anda as estações novas | `Invariante_C2_Offline_NuncaPerdeVoo_NemAndaAsEstacoesNovas` |
| passageiro na parada opcional não fica para trás quando o voo fecha | `Invariante_C2_VooPerdido_CustaSoAReceita_ComGenteNaBagagemENaAlimentacao` |

### Para medir com gente

- As estações novas só rendem se a pessoa levar alguém para lá: se o playtest mostrar que ninguém descobre isso, o contratado pode voltar a nascer na estação do perfil, e aí o portão precisa ser medido de novo.
- Os números do Bom depois do marco 2 só aparecem em partidas de mais de 34 min: medir a oficina e o despacho com gente exige partida longa.

## 7. Fase C3: 3 aeroportos, 12 rotas e 4 incidentes (2026-10-30, `CONTRATO_FASE_C3.md`)

**Como medir:** `dotnet test client/tools/coretests --filter "FullyQualifiedName~BalanceTests.Portao_|FullyQualifiedName~BalanceTests.Antes_|FullyQualifiedName~BalanceTests.Longo_" --logger "console;verbosity=detailed"` (~4 min: as sementes rodam em paralelo, com a soma na ordem de sempre; em série eram 17 min, acima do teto de 20 min do job `portoes` no CI). `Antes_FaseC3` roda com rotas e aeroportos novos fechados; `Portao_FaseC3_PorSistema` mede cada sistema, com tudo aberto desde o começo; `Portao_TA6_NenhumaRotaDomina` prende as 2 vagas em cada rota; `Longo_FaseC3` é a partida de 40 ondas.

**Bots:** o perfil ganhou 3 chaves: `Rotas` (abre rota e programa as vagas), `Viaja` (vai ao próximo aeroporto) e `Incidentes` (reage ao aviso). Bom, Lento e Parado têm as 3. Em cada portão, o Planejador e o Automático são o mesmo Bom, com uma chave de diferença. Rotas: depois de cada voo, a vaga sobe para a rota de receita maior se ele embarcou ≥ 95% e desce se embarcou < 85%; o Automático fica nas 2 rotas iniciais. Aeroportos: viaja quando pode pagar e o último voo embarcou ≥ 90%; o Automático fica em Vale Verde. Incidentes: usa a saída assim que o aviso aparece; o Automático ignora o aviso e só tem o mover da crise. Os perfis das Fases C e C2 ficam sem as chaves, e por isso os portões delas não mudaram (1,114 e 1,366).

### D-C3a. A frente fria: quem adianta continua embarcando

A 1ª regra fazia o `Adianta()` fechar o portão antes da pista. O embarque ganhava só 10 s (abre no pouso + pátio, não 60 s antes), e o portão fechava mais cedo: reagir rendia **1,033**. A regra virou a do TA-4 ao pé da letra, "quem adianta o embarque não perde o voo": sem adiantar, o portão para enquanto a pista está fechada; com `Adianta()`, o embarque abre já e continua até a hora, e só a decolagem espera a pista. Sozinha, a frente fria passou a render **1,335**. A pista fecha 48 s antes do portão para não pegar o pouso da vaga 2 nem a decolagem da vaga 1.

### D-C3b. A tabela de rotas, de novo

Na 1ª medição (200 sementes × 20 ondas), a conexão dominava e a longa não pagava: Brasília 42,6 mil contra Fortaleza 31,4 mil; Galeão 50,8 mil contra Lisboa 23,9 mil. Quem vem de conexão não ocupa o check-in e ainda tem bônus maior, e a longa perde o bônus no primeiro passageiro que fica. A conexão baixou para Pax × Tarifa de 1,35 a 1,5, e a longa subiu para tarifa × 3, com volume menor (1,7 e 2,0). O TA-6 passou, e o bot deixou de subir para uma rota pior.

### D-C3c. Os 4 incidentes contam como um sistema

A decisão é uma só: reagir ao aviso em vez de ignorar. Com a mesma leitura da D-C2c, os tipos saem na tabela, sem portão próprio. **Ressalva:** só a frente fria passa sozinha. Se o corte tiver de ser por tipo, falha, pico e conexão saem dos `Aeroporto.Pesos` (ou ficam só visuais) e o ganho do sistema fica o da frente fria.

### D-C3d. O estado das estações entrou no save

Em Aurora, com fila cheia, a retomada dava um jogo diferente: o bot decide no passo em que o jogo reabre, e `Estado`, `Folga` e `VooEmRisco` voltavam zerados. Em jogo só mudaria a cor por 0,5 s, mas `retomada=igual` é portão do build. O save grava as linhas `stt.` e a View deixa de emitir uma troca de estado falsa ao abrir. `Aviao.Inspecao` também passou a limpar no fim da checagem: ficava ligado sem checagem nenhuma e quebrava a ida e volta do save.

### D-C3e. Decisões de desenho

- **A empresa é uma só:** viajar leva Cash, equipe, níveis e frota. O GDD não diz se cada aeroporto tem terminal próprio, e essa é a opção mais simples. Se o playtest pedir níveis por aeroporto, são 3 cópias do `Station` no save.
- **Incidente só onde o GDD pede:** Vale Verde não tem chance própria, e o único risco lá é a rota de Belo Horizonte (risco 1), que é escolha do jogador. Costa Azul e Aurora têm 0,4 e 0,5 por onda; com o risco das rotas e a folga de 1 onda, dá 1 incidente a cada 2,5 ondas (7 a 8 min).

### Antes e depois do TA-1 e do TA-2 (200 sementes × 10 ondas)

Antes (rotas e aeroportos novos fechados, `Antes_FaseC3`): **igual à tabela "depois" da §6, número a número**. É a prova de que o começo não mudou para o bot.

| perfil | embarque médio | p10 | receita | movimentos | toques | marco 1 | marco 2 | marco 3 |
|---|---|---|---|---|---|---|---|---|
| Bom | 99.7% | 99.4% | 6913 | 4.7 | 981 | 13.7 min | - | - |
| Lento | 75.4% | 71.0% | 3838 | 38.0 | 569 | 15.8 min | - | - |
| Parado | 92.2% | 92.0% | 4911 | 0.0 | 372 | 13.7 min | - | - |

Depois (padrão do `Tuning`: Curitiba abre no marco 1; o resto só depois das 10 ondas):

| perfil | embarque médio | p10 | receita | movimentos | toques | marco 1 | marco 2 | marco 3 |
|---|---|---|---|---|---|---|---|---|
| Bom | 96.1% | 95.3% | 6827 | 32.7 | 972 | 13.7 min | - | - |
| Lento | 73.0% | 71.2% | 3629 | 38.7 | 573 | 15.8 min | - | - |
| Parado | 89.7% | 89.4% | 4788 | 0.0 | 404 | 13.7 min | - | - |

Ganho de mover = **1,426** (era 1,408). **TA-1 e TA-2 passam.** A receita das 10 ondas cai 1% a 5%: a rota é paga no meio da janela, e o retorno vem depois (rotas em Vale Verde: +22% em 20 ondas).

`-autoplay 10` no build (semente 1): `embarque_bom=95.5% embarque_lento=73.2% embarque_parado=89.5% ganho_mover=1.423 receita_bom=6795 moves_bom=30 marcos_bom=13.7min,-,- retomada=igual`.

### Portão do §5.13 (200 sementes × 20 ondas, tudo aberto, um sistema por vez)

| sistema | emb. planejador | emb. automático | receita planejador | receita automático | ganho | incidentes por partida | regra |
|---|---|---|---|---|---|---|---|
| rotas em Vale Verde | 97,8% | 99,7% | 27256 | 22324 | **1,221** | 1,4 | ligada |
| rotas em Costa Azul | 95,9% | 96,0% | 31870 | 28364 | **1,124** | 7,6 | ligada |
| rotas em Aurora Global | 98,8% | 100,0% | 46861 | 36799 | **1,273** | 8,4 | ligada |
| aeroportos | 96,6% | 97,8% | 31365 | 27256 | **1,151** | 1,7 | ligada |
| incidentes em Costa Azul | 91,6% | 82,7% | 28728 | 25061 | **1,146** | 7,6 | ligada |
| incidentes em Aurora Global | 93,5% | 86,0% | 44565 | 38089 | **1,170** | 8,2 | ligada |
| só frente fria (Aurora) | 89,5% | 75,4% | 38182 | 28605 | 1,335 | 8,4 | parte dos incidentes |
| só falha (Aurora) | 98,7% | 94,5% | 46626 | 44767 | 1,042 | 8,4 | parte dos incidentes |
| só pico (Aurora) | 98,1% | 97,3% | 50945 | 47402 | 1,075 | 8,3 | parte dos incidentes |
| só conexão (Aurora) | 97,8% | 97,1% | 55706 | 53447 | 1,042 | 8,2 | parte dos incidentes |

As linhas de incidente mostram o Bom com as rotas ligadas, reagindo e sem reagir. O embarque do Automático em incidentes (83–86%) também mostra o TA-1 em Costa Azul e Aurora.

### TA-6: nenhuma rota domina (200 sementes × 20 ondas, sem incidente, as 2 vagas presas na rota)

| aeroporto | rota | embarque | receita |
|---|---|---|---|
| Vale Verde | Campinas | 99,7% | 22324 |
| Vale Verde | Ribeirão Preto | 99,7% | 22324 |
| Vale Verde | Curitiba | 96,0% | 28648 |
| Vale Verde | Belo Horizonte | 100,0% | 29216 |
| Costa Azul | Salvador | 93,5% | 27390 |
| Costa Azul | Recife | 97,6% | 30377 |
| Costa Azul | Fortaleza | 90,2% | 33775 |
| Costa Azul | Brasília | 100,0% | 35363 |
| Aurora Global | Guarulhos | 100,0% | 35018 |
| Aurora Global | Galeão | 100,0% | 38439 |
| Aurora Global | Manaus | 87,9% | 37666 |
| Aurora Global | Lisboa | 87,8% | 44639 |

Melhor ÷ segunda: Vale Verde 1,020; Costa Azul 1,047; Aurora 1,161. **Passa** (teto 1,20). A escolha muda com a equipe: Curitiba e Fortaleza pedem volume, Belo Horizonte traz risco, Lisboa fica com o avião fora uma onda. As iniciais rendem menos que as pagas, e é o que dá sentido a abrir rota.

### Partida longa (200 sementes × 40 ondas, 2 h de jogo, desbloqueio normal)

| perfil | embarque médio | receita | incidentes | reações | chega à Costa Azul | chega a Aurora |
|---|---|---|---|---|---|---|
| Bom | 93,7% | 104821 | 9,5 | 8,9 | onda 13,9 (200/200) | onda 21,2 (200/200) |
| Lento | 78,1% | 58249 | 6,4 | 6,4 | onda 21,2 (200/200) | onda 28,7 (200/200) |
| Parado | 63,9% | 26135 | 7,4 | 5,4 | onda 18,9 (200/200) | nunca |

O Parado é a referência do TA-2, não um jogador: sem mover ninguém, a Costa Azul o derruba, e ele nunca volta a Vale Verde, embora voltar seja grátis.

### Prova de mutação (`python client/tools/mutacao.py`)

As 9 travas novas entram no `mutacao.py`, que continua pegando as 13 das Fases C e C2. Resultado em 2026-10-30: **22 de 22 mortos**.

| trava retirada | testes que caíram |
|---|---|
| incidente sempre tem aviso prévio | 4, entre eles `Invariante_C3_IncidenteTemAviso_ENuncaDerrubaOProgresso` |
| incidente não tira Cash | `Invariante_C3_IncidenteTemAviso_ENuncaDerrubaOProgresso`, `Invariante_C3_VooPerdido_CustaSoAReceita_ComIncidenteERotaLonga` |
| um incidente por vez, com folga depois | `Incidente_UmPorVez_ComFolgaDepois` |
| reparo só com Cash | `Invariante_C3_Reparo_SoComCash_NuncaAnuncio`, `RetomadaComFaseC3_ContinuaIgual` |
| offline não anda incidente | `Invariante_C3_Offline_NuncaPerdeVoo_NemAndaIncidente` |
| preço fixo de rota | `Invariante_C3_NadaPagoEAleatorio_PrecoDeRotaEViagem`, `Rota_AbrePeloMarcoEPrecoFixo_EProgramaValeNaProximaOnda` |
| rota só depois do marco | `Rota_AbrePeloMarcoEPrecoFixo_EProgramaValeNaProximaOnda` |
| rota longa sem voo na onda fora | `Longa_AviaoFicaFora_AVagaFicaSemVoo_ENadaSePerde` |
| frente fria segura o portão | `FrenteFria_SemSaida_NinguemEmbarcaNemDecola_EOVooPerdeOBonus` |

### Para medir com gente

- O portão do Automático supõe que o jogador ignora o aviso. Se quase todo mundo reagir, a frente fria vira rotina, e o que importa é se ela é entendida na 1ª vez. O TA-4 pede ≥ 50% de reação ao aviso e nenhum "injusto" no playtest.
- Perder quase um voo inteiro por ignorar a frente fria é o incidente mais duro. Se a reclamação vier, baixe a `FrenteAntes` (mais embarque antes de a pista fechar) antes de mexer na frequência.
- Um incidente a cada 7–8 min é hipótese do bot, que reage em 1 s. Com gente, medir se 0,4 e 0,5 por onda cansam.
- A tripulação visual (D-C2) não voltou com regra: a Fase C3 não trouxe mais voos por onda, só voos maiores.

## 8. Fase C4: dia a dia, estações do ano, clima e falhas de avião (2026-10-31, `CONTRATO_FASE_C4.md`)

**Como medir:** `dotnet test client/tools/coretests --filter "FullyQualifiedName~BalanceTests.Portao_|FullyQualifiedName~BalanceTests.Antes_FaseC4|FullyQualifiedName~BalanceTests.Medida_FaseC4" --logger "console;verbosity=detailed"`. As sementes rodam em 4 threads (`BalanceTests.Threads`), com a soma na ordem de sempre. `CoreTests.Bot_em_paralelo_da_o_mesmo_que_uma_semente_por_vez` prova que dá o mesmo número de rodar uma por vez. Em 200 sementes, as tabelas saíram idênticas às da rodada em série, com 19,6 min em série e 5,4 min em paralelo. `Medida_FaseC4` não roda no CI: leva 11 min.

**Bots:** o perfil ganhou as chaves `Clima` (adianta no aviso e, se perdeu o aviso, atrasa), `Falhas` (paga o reparo da pane) e `Segue` (muda de aeroporto com a estação). Bom, Lento e Parado têm `Clima` e `Falhas`. Em cada portão, o Planejador e o Automático são o mesmo Bom, com rotas e reação a incidente, e a diferença é a do sistema medido.

### D-C4a. A demanda por estação do ano fica só visual

O Planejador que segue a estação rendeu **1,000** em 200 sementes × 2 anos: pela conta do bot, Aurora Global é o melhor aeroporto em todas as estações, e ele nunca sai de lá. Com estações de 2 dias, ele chegou a viajar e rendeu 0,985 em 60 sementes. A chave `Tuning.RegraEpocas` fica desligada, e os fatores continuam na tabela `Aeroporto.Demanda`. As estações do ano seguem mudando a chance de cada clima, que está dentro do sistema de clima. **Para voltar:** a demanda por estação pede um motivo para mudar de aeroporto, como rota de temporada ou aeroporto antigo que rende (TA-7).

### D-C4b. No temporal, quem adiantou continua embarcando

Na 1ª regra o embarque parava para todos e a janela reabria 8 s antes do portão: reagir rendia **1,010**. No fim do jogo, com o portão rápido, 8 s bastam para embarcar todo mundo. Agora o temporal dura 55 s, o portão fecha com ele em curso, e quem adiantou segue pela ponte, como na frente fria (D-C3a). Rende **1,218** sozinho.

### D-C4c. O vento fecha a pista

Na 1ª regra o vento só fazia cada pouso e decolagem esperar 25 s. Com 2 aviões, que operam a cada 50 s, a espera quase não pesa: Costa Azul ficou em 0,960 e depois 1,026. Rajada acima do limite da pista fecha a operação, o que é mais perto do real e mais fácil de explicar. Rende **1,218** sozinho.

### D-C4d. Adiantar também pega a vez na pista

Na 1ª regra a saída da neblina e do vento era atrasar o portão, e atrasar rendia menos que segurar: 0,977. O atraso custa o bônus e devolve o avião mais tarde. Agora quem adianta já pegou a vez na pista e sai sem esperar, como quem degela antes na neve. Atrasar ficou para quem perdeu o aviso. A neblina rende **1,130** sozinha.

### D-C4e. Pane sem desconto da oficina, e chance de 0,4

Com chance de 0,3, as falhas ficaram em 1,026, 1,074 e 1,097. No fim do jogo, com gente e upgrades na oficina, a inspeção cai para cerca de 30 s, e a pane deixava de custar o voo. A inspeção de pane agora leva os 110 s inteiros e a oficina acelera só o reparo pago. Com chance de 0,4 o portão passa. **Leitura honesta:** o reparo pago sozinho rende de 1,019 a 1,034. O ganho vem de cuidar do avião antes do aviso (checagem) e de pagar quando falha, juntos.

### D-C4f. Vale Verde fica fora do portão do clima

Vale Verde é o aeroporto que ensina: tem menos clima de propósito e rende **1,086**. Pela proporção medida, passar de 1,10 pediria clima em quase 1 de cada 2 ondas. O portão do clima vale em Costa Azul e Aurora Global, como os incidentes da C3, e a tabela mostra Vale Verde e a partida inteira (**1,184**).

### D-C4g. Decisões de desenho

- **Estação do ano de 1 dia (8 ondas, 24 min):** com 2 dias o ano teria 64 ondas e o job `portoes` passaria do teto de 20 min do CI. Além disso, o jogador vê a estação mudar numa sessão.
- **Atrasar não passa da virada da onda:** o quadro vira a cada onda. Clima que termina depois da virada não dá para atrasar: restam adiantar e segurar.
- **Segurar é não fazer nada:** não tem botão. Custa no máximo a receita de quem não embarcou.
- **Remarcar não mexe no Cash:** o voo já pagou no embarque, e devolver tiraria Cash do saldo. O custo do retorno é o tempo do avião.
- **Sem Cash, a inspeção resolve a pane de graça:** senão, 2 aviões em pane e Cash zerado seriam derrota.
- **Os picos do dia não têm portão próprio:** não têm escolha própria. Ficam ligados porque TA-1 e TA-2 passam.

### Antes e depois do TA-1 e do TA-2 (200 sementes × 10 ondas)

Antes (`Antes_FaseC4`, tudo desligado): **igual à tabela "depois" da §7, número a número**.

| perfil | embarque médio | p10 | receita | movimentos | toques | marco 1 | marco 2 | marco 3 |
|---|---|---|---|---|---|---|---|---|
| Bom | 96.1% | 95.3% | 6827 | 32.7 | 972 | 13.7 min | - | - |
| Lento | 73.0% | 71.2% | 3629 | 38.7 | 573 | 15.8 min | - | - |
| Parado | 89.7% | 89.4% | 4788 | 0.0 | 404 | 13.7 min | - | - |

Depois (padrão do `Tuning`):

| perfil | embarque médio | p10 | receita | movimentos | toques | marco 1 | marco 2 | marco 3 |
|---|---|---|---|---|---|---|---|---|
| Bom | 94.2% | 92.4% | 6279 | 25.7 | 915 | 13.7 min | - | - |
| Lento | 75.8% | 71.5% | 3872 | 29.3 | 569 | 15.8 min | - | - |
| Parado | 88.7% | 87.1% | 4702 | 0.0 | 408 | 13.7 min | - | - |

Ganho de mover = **1,336** (era 1,426). **TA-1 e TA-2 passam.** O Lento sobe de 73,0% para 75,8%; a causa provável é a demanda menor em parte das ondas, mas não isolei isso.

### Portão do §5.13 (200 sementes × 1 ano = 32 ondas, tudo aberto, um sistema por vez)

| sistema | emb. planejador | emb. automático | receita planejador | receita automático | ganho | regra |
|---|---|---|---|---|---|---|
| clima em Costa Azul | 92,2% | 82,4% | 59014 | 50812 | **1,161** | ligada |
| clima em Aurora Global | 95,3% | 88,1% | 94973 | 82772 | **1,147** | ligada |
| falhas em Vale Verde | 98,0% | 90,6% | 53347 | 47044 | **1,134** | ligada |
| falhas em Costa Azul | 90,2% | 82,8% | 56479 | 49949 | **1,131** | ligada |
| falhas em Aurora Global | 90,6% | 84,9% | 87441 | 77797 | **1,124** | ligada |
| estações do ano (2 anos, `Medida_FaseC4`) | 89,4% | 89,4% | 446999 | 446999 | 1,000 | só visual |

Sem portão próprio (`Medida_FaseC4`):

| linha | emb. planejador | emb. automático | receita planejador | receita automático | ganho |
|---|---|---|---|---|---|
| clima em Vale Verde | 97,6% | 90,9% | 51870 | 47768 | 1,086 |
| clima na partida inteira (2 anos) | 94,9% | 87,7% | 211194 | 178429 | 1,184 |
| só frente fria (Aurora, 20 ondas) | 93,2% | 84,9% | 43809 | 37155 | 1,179 |
| só temporal | 96,3% | 85,2% | 45263 | 37164 | 1,218 |
| só vento | 97,8% | 90,0% | 48349 | 39692 | 1,218 |
| só neblina | 97,6% | 93,7% | 48068 | 42526 | 1,130 |
| só neve | 98,1% | 94,1% | 48712 | 43247 | 1,126 |
| só o reparo pago em Vale Verde | 98,0% | 95,6% | 53347 | 51605 | 1,034 |
| só o reparo pago em Costa Azul | 90,2% | 87,3% | 56479 | 55053 | 1,026 |
| só o reparo pago em Aurora Global | 90,6% | 89,2% | 87441 | 85788 | 1,019 |
| manutenção sem falhas em Vale Verde | 98,6% | 98,4% | 54254 | 53556 | 1,013 |
| manutenção sem falhas em Costa Azul | 91,6% | 90,4% | 57823 | 56241 | 1,028 |
| manutenção sem falhas em Aurora Global | 91,7% | 91,4% | 89865 | 86927 | 1,034 |

As linhas de manutenção mostram quanto as falhas acrescentam: sem elas, cuidar do avião rende de 1,013 a 1,034 em 1 ano. No fim do jogo a oficina resolve quase tudo.

Portão da Fase C com tudo ligado (200 sementes × 20 ondas): **1,159** (era 1,114).

### Partida longa (200 sementes × 40 ondas, 2 h de jogo, desbloqueio normal)

| perfil | embarque médio | receita | chega à Costa Azul | chega a Aurora |
|---|---|---|---|---|
| Bom | 92,5% | 98364 | onda 14,2 (200/200) | onda 22,2 (200/200) |
| Lento | 77,3% | 52282 | onda 21,1 (200/200) | onda 29,6 (200/200) |
| Parado | 66,3% | 27260 | onda 18,9 (200/200) | onda 31,5 (176/200) |

### Prova de mutação (`python client/tools/mutacao.py`)

As 12 travas novas entram no `mutacao.py`, que continua pegando as 22 das Fases C, C2 e C3. Resultado em 2026-11-01, sobre o código final: **34 de 34 mortos**.

| trava retirada | testes que caíram |
|---|---|
| falha de avião só acima do aviso | 10, entre eles `Checagem_TiraDaFaixaDeRisco_EEvitaAFalha` |
| uma falha por voo | 4, entre eles `Rejeicao_AbortaNaPista_BloqueiaPoucoTempo_ESaiDepois` |
| reparo da pane só com Cash | `Invariante_C4_ReparoSoComCash_AnuncioNaoRepara` |
| clima tem o aviso inteiro | 5, entre eles `Adianta_ValeParaOs5Climas_ForaDoAvisoNao` |
| Cash não muda o clima | 4, entre eles `Determinismo_MesmaSemente_MesmosEventos_ComTodosOsEventosDaC4` |
| anúncio não evita falha | `Invariante_C4_NadaPagoMudaOPerigo`, `Invariante_C4_ReparoSoComCash_AnuncioNaoRepara` |
| pane não tira Cash | `Invariante_C4_VooPerdido_CustaSoAReceita_ComClimaEFalha` |
| todo retorno pousa seguro | 5, entre eles `Determinismo_MesmaSemente_MesmosEventos_ComTodosOsEventosDaC4` |
| retorno tem prioridade na pista | `Retorno_PassaNaFrenteDaPistaFechada` |
| sem Cash a pane não trava o jogo | 3, entre eles `Pane_SeguraOEmbarque_ReparoPagoEmSegundos` |
| com clima, a frente fria sai dos pesos | `Previsao_AcertaSempre_EAvisaAntes` |
| atrasar custa o bônus | `Atrasa_ProPortaoDepoisDeReabrir_SemBonus` |

A 1ª rodada deu 29 de 34 e mostrou duas falhas de teste. `Invariante_C2_Offline_NuncaPerdeVoo_NemAndaAsEstacoesNovas` media um instante sem ninguém em atendimento nas estações novas, e o teste do atraso não garantia que todos tinham embarcado. Os dois agora exigem esse estado antes de medir. Os outros 3 eram padrões vencidos no `mutacao.py`.

### Para medir com gente

- O bot adianta sempre, e adiantar não custa nada. Se todo mundo fizer isso, o aviso vira rotina e a decisão some. Vale medir se o jogador usa o atrasar e, se não usar, dar um custo pequeno ao adiantar.
- Chance de 0,4 por voo acima do aviso é alta. O bot só chega lá quando ignora a checagem, mas gente pode não entender o aviso amarelo. O playtest precisa medir se a falha parece justa.
- A inspeção de pane de 110 s perde o voo inteiro. Se a reclamação vier, baixe o tempo antes de mexer na chance.
- Vale Verde tem clima em 1 de cada 4 ondas depois do marco 1. Medir se assusta quem está aprendendo.
- O tom depende da tela: bombeiros e ambulância precisam parecer rotina de aeroporto, nunca desastre.
