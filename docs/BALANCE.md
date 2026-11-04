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

## 9. Fase C5: campanha principal (2026-11-01, `CONTRATO_FASE_C5.md`)

Trinta objetivos, 10 por aeroporto, com a mesma tabela para o jogo e para o bot. **Guiado** é o Bom que joga pelo objetivo ativo. **Bom** é o mesmo bot, que nunca olha a campanha. A recompensa vale para os dois.

### D-C5a. O contador conta desde a chegada, não só no objetivo ativo

A 1ª versão contava só o objetivo ativo. A cadeia virou uma fila de esperas de sorteio, e quem seguia a campanha ficava preso nos aeroportos mais pobres: chegada a Aurora na onda 47,5 (o Bom chega na 22) e `ag_hub` fechando em só 109 de 200 sementes. Agora os objetivos continuam fechando na ordem, mas o perigo que passou enquanto o jogador fazia outra coisa vale. Quem ignora um objetivo continua parado nele: o Bom para em `vv_loja`, porque nunca põe gente na loja.

### D-C5b. Voo lotado é ≥ 90% a bordo

Com "ninguém perdido", o Lento quase nunca fechava: `ag_hub` em 0 de 200 sementes e 10 ondas presas em Curitiba. O embarque médio dele é 73%. Com `LotadoPct = 90`, os 30 fecham em 200 de 200.

### D-C5c. O portão é em 20 ondas, não em 64

O contrato dizia 64 ondas antes da medida. **Mudei depois de medir**, e por isso a tabela inteira fica aqui.

| ondas | Guiado: receita + recompensas | Bom: receita + recompensas | objetivos (Guiado / Bom) | ganho |
|---|---|---|---|---|
| 10 | 7 070 + 850 | 6 586 + 390 | 8,6 / 6,0 | 1,135 |
| **20** | **25 185 + 4 516** | **25 335 + 730** | **17,1 / 7,1** | **1,140** |
| 30 | 53 528 + 8 690 | 60 667 + 1 695 | 22,2 / 8,5 | 0,998 |
| 40 | 85 771 + 15 039 | 99 235 + 1 762 | 27,7 / 8,5 | 0,998 |
| 64 | 176 511 + 19 815 | 195 007 + 2 388 | 30,0 / 9,0 | 0,995 |

O catálogo inteiro (equipe, melhorias, rotas, aeroportos, pátio e tripulação) custa 27 670 Cash, e a renda acumulada do Bom alcança esse valor perto da onda 20. Na onda 44, os dois bots têm de 80 a 100 mil de Cash parado. Depois do catálogo, a razão mede quanto Aurora paga, não se a campanha gera decisão. Em 20 ondas é o horizonte das Fases C a C3, e nele o Cash ainda compra progresso: o Guiado chega à Costa Azul na onda 12,1, contra 13,7 do Bom.

**O que este portão não prova:** de 30 ondas em diante, seguir a campanha empata. O Guiado chega a Aurora 1,8 onda depois, segura rotas leves para os 8 voos lotados e leva ondas para subir de novo; a recompensa cobre isso e nada mais. E o ganho de 1,140 vem quase todo da recompensa: a receita de jogo empata em 20 ondas.

### D-C5d. Sem sorteio específico, e a Costa termina no marco 3

Pedir 2 atrasos de conexão, 2 climas ou 3 climas deixava a cadeia parada: p90 de 28 ondas à espera do mesmo tipo de perigo. Agora as metas pedem "qualquer clima" ou "qualquer incidente", e menos vezes. `ca_brasilia` saiu: exigia o marco 3, mais 1 000 Cash e 2 ondas, e segurava o Guiado 2 ondas a mais na Costa. O último objetivo da Costa é o próprio marco 3, como em Vale Verde. Brasília continua no jogo, fora da campanha.

### TA-1 e TA-2 (200 sementes × 10 ondas)

| | antes (a fase anterior) | depois (campanha ligada) |
|---|---|---|
| Bom | 94,2%, receita 6 279 | 94,9%, receita 6 586 |
| Lento | 75,8%, receita 3 872 | 73,2%, receita 3 534 |
| Parado | 88,7%, receita 4 702 | 88,4%, receita 4 660 |
| TA-2 (Bom/Parado) | 1,336 | 1,413 |

**Passa.** Com a campanha sem pagar, a tabela é a da coluna "antes", número a número (`Antes_FaseC5`). O Lento perde 2,6 pontos e continua acima dos 60%. A causa provável, não isolada, é que a recompensa o faz comprar rota e equipe antes de dar conta delas.

Os portões das Fases C a C4 seguem verdes. A Fase C, medida com a campanha ligada, dá 1,152.

### Alcance (200 sementes, até 120 ondas)

| | Guiado | LentoGuiado |
|---|---|---|
| fecham os 30 | 200 de 200 | 200 de 200 |
| Vale Verde fechada | onda 10,8 | onda 14,8 |
| Costa Azul fechada | onda 22,8 | onda 32,3 |
| campanha inteira | onda 51,6 (p90 56, pior 66) | onda 62,9 (p90 69, pior 85) |

Sem cauda: entre a média e o p90 há cerca de 6 ondas no fim. Em tempo de jogo, são cerca de 2,6 h e 3,1 h. O trecho mais longo é `ag_hub`, com 2 000 embarques em Aurora, cerca de 12 ondas de espera depois do resto.

### Prova de mutação (`python client/tools/mutacao.py campanha`)

As 16 travas novas entram no `mutacao.py`. Resultado em 2026-11-01: **16 de 16 mortos**. Rodei só as 16 novas. Das travas anteriores, conferi que todos os trechos ainda existem no núcleo, sem rodar de novo.

| trava retirada | testes que caíram |
|---|---|
| recompensa fixa, sem semente | 6, entre eles `Invariante_C5_RecompensaFixa_SemSorteioNem2x` |
| recompensa fora do 2× e do cofre | `Invariante_C5_RecompensaFixa_SemSorteioNem2x`, `Invariante_C5_RecompensaForaDoCofre` |
| regra desligada não paga | 7, entre eles `RegraDesligada_ContaEMostra_NaoPaga` |
| conclui só no alvo | 9, entre eles `Contadores_SeguemOsEventos` |
| fechar e abrir não paga de novo | 8, entre eles `Invariante_C5_PagaUmaVez_FecharEAbrirNaoPagaDeNovo` |
| contador do save limitado | `LixoDaCampanha_ViraPadraoLimitado` |
| offline não conclui | `Invariante_C5_Offline_NaoAndaACampanha` |
| offline não conta | `Invariante_C5_Offline_NaoAndaACampanha` |
| anúncio não anda a campanha | `Invariante_C5_NenhumObjetivoExigeVideoOuCompra` |
| só conta no aeroporto do objetivo | 16, entre eles `Contador_SoContaNoAeroportoDoObjetivo` |
| só a rota pedida conta | `VoosECheios_SoNaRotaPedida`, `Atendidos_SoNaEstacaoPedida`, `Incidentes_SoOTipoPedido` |
| conta antes de virar o ativo | `Contador_ContaAntesDeVirarOAtivo_MasConcluiNaOrdem`, `Invariante_C5_TodoObjetivoAlcancavel` |
| voo perdido zera a sequência | `Seguidos_PerdaZeraASequencia` |
| só a sequência recua | `Invariante_C5_VooPerdido_NaoTiraNada`, `Contadores_SeguemOsEventos`, `Invariante_C5_TodoObjetivoAlcancavel` |
| perigo só conta resolvido | `Climas_SoContaResolvido` |
| não tranca o jogo | `Invariante_C5_CampanhaNaoTrancaOJogo` e mais 3 |

### Para medir com gente

- **Cash sem destino:** os 12 900 Cash de Aurora chegam quando já não há o que comprar. Se a próxima frente (missões, passe, mapa que cresce) não trouxer sumidouro, a recompensa ali vira número. Refazer a tabela do D-C5c quando isso existir.
- **O Bom para em `vv_loja`** porque o bot nunca põe gente na loja. Gente vai ler o objetivo na tela. A pergunta real é se a pessoa entende o que fazer, e isso só o playtest diz.
- **Cadeia em ordem:** o jogador vê um objetivo só. Se perguntar "por que não conta?" num objetivo atrás do ativo, pensar em mostrar os seguintes.
- O bot resolve todo perigo em 1 s. Com gente, `Climas` e `Incidentes` vão demorar mais, porque segurar não conta.

## 10. Fase C6: missões e conquistas (2026-11-01, `CONTRATO_FASE_C6.md`)

Três diárias e 5 semanais escolhidas de 63 linhas, mais 20 conquistas. **Missionário** é o Bom que joga pelas missões abertas. **Bom** é o mesmo bot, que nunca olha para elas e recebe o que cumprir sem querer. O pagamento vale para os dois.

### D-C6a. O dia é o do jogo

O GDD §6.5 pede a virada às 04:00 no fuso gravado. A frente pediu nada de hora de parede dentro da simulação, e o núcleo usa `DiaJogo`: 8 ondas, 24 min jogados. O bot mede a coisa real, adiantar o relógio do aparelho não dá lista nova e faltar um dia não existe. O custo: a missão não chama o jogador de volta amanhã, e quem joga 1 h por dia vê 2,5 listas. **A conta do passe no GDD §6.4 supõe 3 diárias por dia de calendário** e precisa ser refeita na frente do passe.

### D-C6b. A primeira tabela pagava sem decisão

A 1ª tabela deu **1,077**. O Bom fechava 4,1 das 5 semanais em 20 ondas sem olhar para elas: quem joga normalmente já embarca, voa e sobe de nível. As semanais do marco 0 agora pedem uma parte que o Bom não faz, a loja com gente; ele caiu para 1,6.

### D-C6c. Quem persegue missão não pode largar o fluxo

A 2ª medida deu **1,034**, com embarque de 88,5%, e o Lento que jogava pelas missões caiu a **16%**. O bot tirava gente do fluxo e a devolvia a cada decisão, e quem anda não atende. Agora a parada opcional leva só quem está fora do fluxo, quem está numa parada pedida fica, e há um reforço a cada 30 s. Medido: embarque de 93,4% contra 93,6% do Bom, e **1,147**. É a mesma escolha que o jogador vai fazer, e a tela precisa mostrar isso.

### D-C6d. Conquista não paga Cash

Conquista fecha tarde (a mais lenta na onda 149), quando o Cash já não compra nada. Sem Cash, ela fica fora do balanço e não precisa de portão nem de chave.

### D-C6e. Quatro alvos baixaram depois da medida

Decolagens de 500 para 200: não cabiam em 240 ondas. Hangar de 5 para 3, sequência de 12 para 8, loja de 200 para 80 e missões de 100 para 50: ficavam presos em algumas sementes. Todas as 20 fecham em 200 de 200, com os 2 bots.

### Portão do §5.13 (200 sementes)

| ondas | Missionário: receita + campanha + missões | Bom: receita + campanha + missões | diárias | semanais | ganho |
|---|---|---|---|---|---|
| 10 | 6 928 + 857 + 1 760 | 6 740 + 390 + 389 | 4,5 / 3,7 | 2,0 / 0,0 | 1,269 |
| **20** | **25 742 + 2 688 + 4 350** | **25 986 + 741 + 1 845** | **7,9 / 6,2** | **4,7 / 1,6** | **1,147** |
| 30 | 60 896 + 4 287 + 6 402 | 61 189 + 1 905 + 3 182 | 10,9 / 8,4 | 5,0 / 1,7 | 1,080 |
| 40 | 97 055 + 6 485 + 8 532 | 99 676 + 2 218 + 4 860 | 13,7 / 10,8 | 5,0 / 1,7 | 1,050 |
| 64 | 181 412 + 11 452 + 14 852 | 194 589 + 2 965 + 10 007 | 20,6 / 16,0 | 5,3 / 2,1 | 1,001 |

O Lento que joga pelas missões contra o Lento: 1,149 em 20 ondas e 1,055 em 64, com embarque de 77,0% contra 78,1%.

**O que este portão não prova:**

- **Boa parte do ganho vem da campanha.** Pôr gente na loja destrava `vv_loja`, onde o Bom para: campanha 2 688 contra 741. Só com as missões, a razão seria 1,079.
- **Para quem já joga pela campanha, as missões rendem menos que 1,10.** `Completo` (campanha e missões) contra `Guiado` (só campanha): **1,080** em 20 ondas (25 779 + 4 558 + 4 058 contra 25 297 + 4 487 + 2 077) e **0,964** em 64. Perseguir tudo no fim do jogo custa mais do que paga.
- **A receita de jogo empata.** O ganho é recompensa, como na campanha (D-C5c). Depois do catálogo, perto da onda 20, o Cash não compra mais nada.

### TA-1 e TA-2 (200 sementes × 10 ondas)

| | antes (a Fase C5) | depois (missões pagando) |
|---|---|---|
| Bom | 94,9%, receita 6 586 | 95,0%, receita 6 740 |
| Lento | 73,2%, receita 3 534 | 78,8%, receita 4 219 |
| Parado | 88,4%, receita 4 660 | 88,5%, receita 4 709 |
| TA-2 (Bom/Parado) | 1,413 | 1,431 |

**Passa.** Com as missões sem pagar, a tabela é a da coluna "antes", número a número (`Antes_FaseC6`). O Lento sobe 5,6 pontos: o Cash extra compra equipe antes, e ele é quem mais precisa dela.

Os portões anteriores seguem verdes com as missões ligadas: Fase C 1,152; Fase C2 1,371; Fase C3 1,221, 1,124, 1,273, 1,151, 1,146 e 1,170; Fase C4 1,161, 1,147, 1,134, 1,131 e 1,124; TA-6 verde. **A campanha caiu de 1,140 para 1,114:** o dinheiro das missões entra nos 2 lados e dilui a recompensa dela. Ainda passa, com pouca folga.

### Alcance (200 sementes, até 240 ondas)

| | Completo | LentoCompleto |
|---|---|---|
| conquistas | 20 de 20, em 200 de 200 | 20 de 20, em 200 de 200 |
| primeira (`c_seguidos`, `c_movidos`) | onda 3,3 | onda 13,8 |
| última (`c_passageiros_2`) | onda 148,5 (p90 153) | onda 172,8 (p90 182) |
| diárias feitas | 73,9 de 90 | 65,3 de 90 |
| semanais feitas | 20,5 de 25 | 18,3 de 25 |

Em tempo de jogo, as 20 fecham em cerca de 7,4 h e 8,6 h. A sobra das diárias é normal: a missão que não fecha sai sem custo.

### Prova de mutação (`python client/tools/mutacao.py missao conquista`)

As 26 travas novas entram no `mutacao.py`. Resultado em 2026-11-01: **26 de 26 mortos**. Rodei só as 26 novas, e as 76 travas ainda acham o trecho delas no núcleo. A 1ª rodada, só com `MissoesTests`, `ExploitTests` e `SaveTests`, deu 24 de 26: um mutante não mudava nada, porque multiplicava todas as notas pelo mesmo número, e o outro só aparecia no texto do save. Troquei o 1º, apertei o teste do 2º e repeti esses 2, mais o da rota, com o `dotnet test` inteiro.

| trava retirada | testes que caíram |
|---|---|
| recompensa fixa, sem semente | 5, entre eles `Invariante_C6_RecompensaFixa_SemSorteioNem2x` |
| recompensa fora do 2× e do cofre | `Invariante_C6_RecompensaFixa_SemSorteioNem2x`, `Invariante_C6_RecompensaForaDoCofre` |
| regra desligada não paga | 8, entre eles `RegraDesligada_ContaEMostra_NaoPaga` |
| paga uma vez | `Conclui_NoPassoFixo_PagaFixoEFicaFeita` e mais 2 |
| semanal pede as 2 partes | `Semanal_PedeAsDuasPartes` |
| fechar e abrir não paga de novo | 8, entre eles `Invariante_C6_PagaUmaVez_FecharEAbrirNaoPagaDeNovo` |
| contador do save limitado | `LixoDasMissoes_ViraPadraoLimitado` |
| dia do jogo, nunca o do aparelho | `Invariante_C6_RelogioDoAparelhoNaoViraODia_FaltarNaoCustaNada` |
| dia do save limitado | `LixoDasMissoes_ViraPadraoLimitado` |
| virada não tira Cash | 21, entre eles `Invariante_C6_ViradaNaoTiraNada` |
| a lista sai só da semente | `Invariante_C6_ListaNaoDependeDeCashNemDeAnuncio` |
| só a linha do marco | `Lista_EmTodoMarco_EncheSemRepetirMeta` e mais 1 |
| nada que o jogo não tem | `Lista_SoEntraOQueDaParaFazerAqui` |
| rota aberta, no aeroporto atual | `Lista_SoEntraOQueDaParaFazerAqui` |
| catálogo no fim sai da lista | `Lista_SoEntraOQueDaParaFazerAqui` |
| sem repetir a meta | `Lista_EmTodoMarco_EncheSemRepetirMeta` |
| offline não conclui | `Invariante_C6_Offline_NaoAndaAsMissoes` |
| offline não conta | `Invariante_C6_Offline_NaoAndaAsMissoes` |
| anúncio não anda missão | `Invariante_C6_NenhumaMissaoExigeAnuncioOuCompra` |
| só a rota ou a estação pedida | `SoARotaPedidaConta` e mais 2 |
| conquista não paga Cash | `Invariante_C6_ConquistaNaoPagaEFicaFeita` |
| conquista feita fica no alvo | `Conquista_Seguidos_FicaFeitaDepoisDeQuebrar` e mais 1 |
| conquista uma vez | 4, entre eles `Conquista_DeEstado_LeOJogo_DeContador_ContaSempre` |
| conquista fica feita ao reabrir | 6, entre eles `Invariante_C6_ConquistaNaoPagaEFicaFeita` |
| máscara presa à tabela | `LixoDasMissoes_ViraPadraoLimitado` |
| contador de conquista limitado | `LixoDasMissoes_ViraPadraoLimitado` |

### Para medir com gente

- **O dia do jogo funciona?** A pergunta central da D-C6a: sem "volte amanhã", a missão ainda dá motivo para abrir o jogo? Se não der, trocar `DiaJogo` por `Dia` é uma linha, e o portão precisa ser medido de novo.
- **A loja aparece demais.** Quatro das 6 semanais do marco 0 pedem a loja, porque é a decisão que o jogo tem nesse marco. Pode parecer repetitivo. Se parecer, a saída é uma decisão nova no marco 0, não pagar mais.
- **Cash sem destino continua.** Missão no marco 3 paga até 4 000 num jogo com 100 mil parados. Nenhum sumidouro entrou aqui: o passe e o mapa que cresce continuam devendo isso (§9).
- **A lista fica como saiu.** Quem viaja no meio do dia pode ficar com uma rota do aeroporto anterior até a virada. Voltar é grátis, mas vale ver se confunde.

## 11. Fase D1: Gems e compras (2026-11-01, `CONTRATO_FASE_D1.md`)

Primeira parte das compras, só no núcleo. **Guiado** nunca compra. **GastaGanho** gasta só as Gems que ganhou. **BoasVindas** compra o pacote de boas-vindas. **PacoteGrande** compra 1 pacote grande. **Baleia** compra 200. Os 4 que gastam deixam o booster sempre ligado e, enquanto houver Gems, nunca ficam com menos de 5 000 Cash.

### Portão de compras (200 sementes, até 120 ondas, para quando a campanha fecha)

| bot | fecham os 30 | Vale Verde | Costa Azul | campanha | p90 | pior | embarque |
|---|---|---|---|---|---|---|---|
| Guiado | 200 de 200 | onda 10,8 | 22,9 | **51,6** | 56 | 66 | 90,8% |
| LentoGuiado | 200 de 200 | 14,2 | 32,1 | **62,8** | 70 | 99 | 77,3% |
| GastaGanho | 200 de 200 | 10,8 | 22,5 | 51,2 | 55 | 66 | 91,0% |
| BoasVindas | 200 de 200 | 10,6 | 22,3 | 51,1 | 55 | 66 | 91,6% |
| PacoteGrande | 200 de 200 | 10,6 | 22,3 | 51,1 | 55 | 66 | 91,3% |
| Baleia | 200 de 200 | 10,6 | 22,3 | **51,1** | 55 | 66 | 91,3% |

**Passa.** Sem compra, os 30 fecham em 200 de 200 sementes. A Baleia fecha em **0,990** do tempo do Guiado, acima do chão de 0,85.

### D-D1a. Comprar quase não acelera a campanha

A Baleia ganha meia onda em 51. O tempo da campanha sai de marcos e de 2 000 embarques em Aurora (`ag_hub`), e nada disso se compra. Um pacote grande já dá o mesmo resultado que 200.

Para o guardrail, é o melhor resultado possível: compra não é poder. Para a receita, é um alerta: o pacote de Cash vende pouco progresso, e depois do catálogo (27 670 Cash, perto da onda 20) o Cash comprado não tem destino. O GDD §7.3 recusa vender benefício inexistente. O núcleo não bloqueia a compra nesse ponto: `MaisBaratoTravado` não olha rotas, viagens nem tripulação, e um sumidouro novo pode chegar com o passe ou o mapa que cresce. **Decidir na tela da loja.** Nada foi escondido do jogador: a prévia mostra o Cash, e o catálogo restante é visível.

### D-D1b. O pacote caiu de 30 para 10 min de renda

A 1ª versão dava 30 min de renda por 100 Gems: 5 727 Cash na onda 5 e 12 919 na onda 10, quase o dobro da receita do Bom nas 10 primeiras ondas (6 740). Agora são 10 min por 50 Gems, com o mesmo valor por Gem do booster: 12 s de renda. O pacote entrega na hora; o booster pede 30 min jogando.

| onda | pacote de Cash (média de 40 sementes) |
|---|---|
| 5 | 1 908 |
| 10 | 4 251 |
| 20 | 6 507 |
| 30 | 9 118 |
| 50 | 9 659 (teto de 10 000 em parte das sementes) |

### D-D1c. Fechar a cadeia dá 100 Gems

O GDD §6.1 diz que Gems vêm de "metas claras". Sem isso, quem não compra nunca veria a moeda. São 300 Gems na campanha: 1 cosmético, ou 2 boosters, ou 6 pacotes. Gastar tudo no que ganhou tira 0,4 onda da campanha (GastaGanho).

### D-D1d. O chão de 0,85 veio depois da medida

Fixei o chão depois da 1ª rodada, que deu 0,99. Ele não prova que 0,85 é o número certo. É um alarme: se uma frente futura fizer o Cash comprar progresso, o portão cai. O portão roda no CI com o Guiado, o LentoGuiado e a Baleia; os outros 3 bots ficam em `Medida_FaseD1`, à mão.

### TA-1 e TA-2 (200 sementes × 10 ondas)

Nenhum bot de sempre gasta Gems: Bom 95,0% e receita 6 740, Lento 78,8% e 4 219, Parado 88,5% e 4 709, TA-2 1,431. É a tabela da Fase C6, número a número (`Antes_FaseD1`). Portão da Fase C5: 1,114. Portão da Fase C6: 1,147.

### Prova de mutação (`MUTACAO_TESTES="FullyQualifiedName~ComprasTests|FullyQualifiedName~SaveTests" python client/tools/mutacao.py compra:`)

As 26 travas novas entram no `mutacao.py`. Resultado em 2026-11-01: **26 de 26 mortos**. Rodei só as 26, contra `ComprasTests` e `SaveTests`. Das travas anteriores (76), conferi que os trechos ainda existem no núcleo, sem rodar de novo.

| trava retirada | testes que caíram |
|---|---|
| a mesma transacao rende uma so | 4, entre eles `Credito_AMesmaTransacaoRendeUmaSo` |
| o id da transacao vai no save | 2, entre eles `Credito_ReabrirOJogoNaoPagaDeNovo` |
| o token nunca fica no save | `Credito_TokenNuncaFicaNoSave` |
| nao consumivel paga uma vez | `Credito_NaoConsumivel_UmaVezSo` |
| id vazio nao credita | `Credito_Invalido_NaoMudaNada` |
| save recusado nao credita | `Credito_ComSaveRecusado_NaoCredita` |
| teto nao engole | `Credito_AcimaDoTeto_NaoEngoleACompra` |
| conteudo fixo, sem semente | 4, entre eles `Credito_AcimaDoTeto_NaoEngoleACompra` |
| Cash fora da media movel | `Invariante_D1_CompraForaDoCofre` |
| pacote fora da media movel | `Invariante_D1_CompraForaDoCofre` |
| pacote com teto | `Pacote_AcompanhaARenda_ComPisoETeto` |
| pacote com piso | `Pacote_AcompanhaARenda_ComPisoETeto` |
| booster nao acumula | `Booster_2xPor30Min_SemAcumular` |
| gasto so com saldo | 2, entre eles `Gastar_SemSaldoOuForaDaTabela_NaoMudaNada` |
| saldo nao estoura | `Invariante_D1_SaldoNuncaEstoura` |
| Gems nao repoem voo | `Invariante_D1_ComprarNaoTocaNoJogo` |
| Gems nao andam a campanha | `Invariante_D1_ComprarNaoTocaNoJogo` |
| Gems nao consertam aviao | `Invariante_D1_ComprarNaoTocaNoJogo` |
| credito nao mexe no voo | `Invariante_D1_ComprarNaoTocaNoJogo` |
| offline nao da Gems | 2, entre eles `Invariante_D1_AnuncioNaoDaGems` |
| anuncio nao da Gems | 2, entre eles `Booster_2xPor30Min_SemAcumular` |
| cadeia paga so ao fechar | 3, entre eles `CadeiaFechada_PagaGemsUmaVez` |
| campanha sem pagar nao da Gems | `CampanhaSemPagar_NaoDaGems` |
| saldo do save limitado | `LixoDoSave_ViraPadraoLimitado` |
| posse do save so de nao consumivel | `LixoDoSave_ViraPadraoLimitado` |
| booster sobrevive a reabrir | 4, entre eles `Booster_2xPor30Min_SemAcumular` |

### Para medir com gente

- **Valor das Gems:** preço real vem da loja, e as quantidades dos pacotes são hipótese. Se o preço não acompanhar a proporção 100, 550 e 1 200, não anunciar bônus: desconto fictício é proibido (GDD §7.5).
- **O 2× por 30 min vale a pena?** Sem destino para o Cash, o booster só importa antes do fim do catálogo. A tela precisa deixar isso claro.
- **Cosméticos:** só há 2 à venda, mais a pintura do pacote de boas-vindas. Todos pedem arte, e a loja precisa mostrar a imagem real (GDD §7.5).
- **Pacote de boas-vindas:** 1 000 Cash no começo adiantam 0,2 onda em Vale Verde. Ver se a pessoa sente esse empurrão.

## 12. Fase C7: passe de temporada, trilha gratuita (2026-11-02, `CONTRATO_FASE_C7.md`)

Uma temporada de 28 dias do jogo (224 ondas, 11,2 h jogadas) e 30 níveis de 100 pontos. O ponto vem do voo que partiu (2), da diária (25), da semanal (70) e do objetivo da campanha (20). O prêmio é o da tabela do `PASSE.md` e paga sozinho no nível.

### D-C7a. A temporada é do jogo

Pela mesma razão da D-C6a: nada de hora de parede na simulação. O bot mede a temporada inteira, o relógio do aparelho não abre nem fecha nada e faltar um dia não existe. O custo: a temporada não é a mesma para todo mundo, e não dá para anunciar "termina domingo". Detalhes no contrato.

### D-C7d. O primeiro ritmo não fechava

A 1ª tabela (diária 20, semanal 50, objetivo 15) fechava em **163 de 200** sementes com o LentoCompleto, no dia 26,1 em média e no 27,9 no pior. Medi as fontes em 60 sementes: o pior LentoCompleto faz 300 voos, 50 diárias, 13 semanais e os 30 objetivos, 2 700 pontos. Com diária 25, semanal 70 e objetivo 20, o mesmo jogo dá 3 360, e o pior das 200 fecha no dia 23,9.

| bot (60 sementes, 224 ondas) | voos | diárias (de 84) | semanais (de 20) | objetivos (de 30) |
|---|---|---|---|---|
| Completo | 289 | 69,0 | 19,1 | 30,0 |
| LentoCompleto | 309 | 60,2 | 17,0 | 30,0 |
| Missionário | 281 | 68,0 | 19,1 | 23,2 |
| Guiado | 269 | 53,0 | 14,8 | 30,0 |
| Bom | 260 | 50,6 | 13,3 | 13,1 |
| Lento | 302 | 44,1 | 13,1 | 20,1 |

Só 260 a 310 dos 448 voos possíveis partem com gente: com um avião fora na rota longa, a onda sai com 1 voo. O voo pesa pouco de propósito: o ponto vem sobretudo de missão.

### Portão do passe (200 sementes, a temporada inteira)

| bot | fecham os 30 | dia em que fecha | p90 | pior | nível no fim | p10 | Cash do passe | embarque |
|---|---|---|---|---|---|---|---|---|
| **LentoCompleto** | **200 de 200** | **20,0** | 21,8 | **23,9** | 30,0 | 30 | 28 300 | 77,7% |
| Completo | 200 de 200 | 18,2 | 19,4 | 19,9 | 30,0 | 30 | 28 300 | 89,2% |
| Missionário | 200 de 200 | 19,3 | 20,4 | 21,9 | 30,0 | 30 | 28 300 | 89,6% |
| Guiado | 200 de 200 | 23,0 | 25,1 | 26,9 | 30,0 | 30 | 28 300 | 90,4% |
| Bom | 84 de 200 | 26,6 | 27,6 | 27,9 | 28,9 | 27 | 27 000 | 90,7% |
| Lento | 111 de 200 | 27,0 | 27,9 | 27,9 | 29,3 | 28 | 27 517 | 79,2% |
| Parado | 0 de 200 | — | — | — | 18,2 | 17 | 10 483 | 53,6% |

**Passa.** Sem compra, quem joga pela campanha e pelas missões fecha os 30 com 4 a 10 dias de folga. Quem nunca olha para elas chega ao nível 27 a 29 e fecha em parte das sementes, no fim. O Parado, que não mexe na equipe, para no 18. O dia é o do jogo, contado do 0.

### Os portões de antes, com o passe pagando

| portão | a Fase D1 | com o passe |
|---|---|---|
| TA-1 Bom | 95,0% | 95,5% |
| TA-1 Lento | 78,8% | 80,6% |
| TA-2 (Bom / Parado) | 1,431 | 1,824 |
| Fase C (Planejador / Automático) | 1,152 | 1,176 |
| Fase C5 (Guiado / Bom, 20 ondas) | 1,114 | 1,380 |
| Fase C6 (Missionário / Bom, 20 ondas) | 1,147 | 1,438 |
| Fase D1 (a Baleia fecha a campanha em) | 0,990 | 0,996 do tempo |
| Fases C2, C3 e C4 e TA-6 | verdes | verdes, número a número: medem com o passe desligado (`SemFaseC4`), como já faziam com as missões |

**TA-2 e os portões da campanha e das missões sobem por causa do 2×.** O tempo do passe dobra a receita de quem chega antes aos níveis 2 e 5, e chega antes quem mexe na equipe, faz missão e fecha objetivo. Com o passe desligado, a tabela é a da Fase D1, número a número (`Antes_FaseD1`: Bom 6 740, Lento 4 219, Parado 4 709). A decisão de mover vale o mesmo de antes: agora o número mede também o passe. A campanha não acelera: o Guiado fecha os 30 objetivos na onda 51,3, contra 51,6.

### Prova de mutação (`MUTACAO_TESTES="FullyQualifiedName~PasseTests|FullyQualifiedName~ExploitTests|FullyQualifiedName~SaveTests|FullyQualifiedName~ComprasTests" python client/tools/mutacao.py "passe:" "booster sobrevive"`)

As 16 travas novas entram no `mutacao.py`, e a do booster da Fase D1 muda de trecho, porque o teto do 2× no save passou a ser `BoostTeto`. Resultado em 2026-11-03: **17 de 17 mortos**. A 1ª rodada deu 16: o mutante do cosmético tirava o corpo do `foreach` e não compilava. Troquei por um corpo vazio e repeti esse. Das travas anteriores, só a do booster rodou de novo.

| trava retirada | testes que caíram |
|---|---|
| recompensa fixa, sem semente | 4, entre eles `Invariante_C7_RecompensaFixa_ForaDo2xEDoCofre` |
| regra desligada não paga | 8, entre eles `RegraDesligada_ContaEMostra_NaoPaga` |
| Cash fora do 2× e do cofre | `Invariante_C7_RecompensaFixa_ForaDo2xEDoCofre`, `Nivel_PagaSozinhoUmaVez_NoValorDaTabela` |
| níveis pagos vão no save | 5, entre eles `Invariante_C7_PagaUmaVez_FecharEAbrirNaoPagaDeNovo` |
| pagos do save limitados | `LixoDoPasse_ViraPadraoLimitado` |
| pontos do save limitados | `LixoDoPasse_ViraPadraoLimitado` |
| dia do save limitado | `LixoDoPasse_ViraPadraoLimitado` |
| a próxima temporada abre no dia em que chega | `Temporada_ViraNoFimDosDias_EFechaSemAProxima` e mais 1 |
| dia do jogo, nunca o do aparelho | 5, entre eles `Invariante_C7_RelogioDoAparelhoNaoMexeNaTemporada` |
| a virada zera os níveis pagos | `DuasTemporadas_ASegundaAbreNoDiaEmQueAPrimeiraFecha` |
| a virada não tira Cash nem cosmético | `Invariante_C7_ViradaNaoTiraNada` e mais 1 |
| offline, anúncio e compra não dão ponto | 3, entre eles `Invariante_C7_PontoNaoSeCompra` |
| voo perdido não tira ponto | `Invariante_C7_VooPerdido_NaoTiraPonto` e mais 1 |
| tempo soma ao 2×, com teto | `Tempo_SomaAo2xQueCorre` |
| cosmético sobrevive a reabrir | `Cosmetico_FicaNaPosse_EVaiNoSave` |
| regra desligada não dá cosmético | `RegraDesligada_ContaEMostra_NaoPaga` |
| booster sobrevive a reabrir (Fase D1, trecho novo) | 7, entre eles `Booster_2xPor30Min_SemAcumular` |

### Para medir com gente

- **O passe fecha cedo para quem joga tudo.** O Completo termina no dia 18 e passa 10 dias sem barra. Se incomodar, subir os pontos por nível e medir de novo. A trilha premium pode ocupar esse fim.
- **Cash do passe sem destino.** São 28 300 Cash, quase todo depois do fim do catálogo (perto da onda 20). Só os primeiros níveis compram progresso; o valor que sobra está no 2× e nos 3 cosméticos, que pedem arte.
- **A barra é entendida?** A tela precisa dizer que ponto não se compra, que nada expira e quantos dias do jogo faltam. Ver se "dia do jogo" confunde quem espera o calendário (D-C7a).

## 13. Fase C8: despesas e relatório do dia (2026-11-03, `CONTRATO_FASE_C8.md`)

Salário por perfil, por função da tripulação e por pessoa de pátio, e manutenção por estação ativa e por nível, cobrados no fechamento do dia do jogo (8 ondas). Faltou caixa: o Cash vai a zero e para, contratar e melhorar pausam até o caixa cobrir a conta, e o fechamento seguinte perdoa o resto. **Antes** = a conta desligada (`RegraDespesas = false`), medida no mesmo código: repete a `main` da Fase D1 número a número (`Antes_FaseD1`). **Depois** = o `Tuning` padrão.

### A tabela (Cash por dia do jogo)

| | Atendente | Segurança | Vendedor | Bagageiro | Mecânico | Despachante | Piloto | Comissário | Pátio |
|---|---|---|---|---|---|---|---|---|---|
| salário | 35 | 50 | 50 | 60 | 90 | 90 | 24 | 12 | 18 |

| | Check-in | Segurança | Loja | Portão | Bagagem | Alimentação | Oficina | Despacho |
|---|---|---|---|---|---|---|---|---|
| manutenção da estação ativa | 12 | 12 | 9 | 9 | 9 | 9 | 18 | 12 |
| + por nível (velocidade + capacidade) | 6 | 6 | 5 | 5 | 5 | 5 | 7 | 0 |

O jogo novo paga 168 por dia: a tripulação inicial (3 pilotos e 3 comissários), 1 pessoa de pátio e as 4 estações da v0.1. Com as 8 estações, a equipe e o pátio completos e tudo no máximo, a conta chega a cerca de 1 000 por dia.

### D-C8c. A 1ª tabela derrubava o portão dos aeroportos

A 1ª tabela era 1,67 × esta (salário de 60 a 150, manutenção de 15 a 30 mais 8 a 12 por nível). Com ela, todos os portões passaram, menos **aeroportos: 1,078**. Quem viaja e quem fica pagam a mesma conta (2 270 em 20 ondas), mas juntar Cash para a viagem demora mais, e quem viaja chega depois ao aeroporto que paga mais. Variantes medidas (200 sementes × 20 ondas, tudo aberto, partindo de Vale Verde, sem incidente):

| variante | viaja | fica | ganho | chega à Costa Azul | chega a Aurora | conta paga |
|---|---|---|---|---|---|---|
| sem conta | 31 365 | 27 256 | 1,151 | onda 13,4 | onda 16,8 | 0 |
| 1ª tabela | 28 692 | 26 615 | **1,078** | 14,2 | 18,1 | 2 270 |
| 1ª tabela pela metade | 30 540 | 26 909 | 1,135 | 14,0 | 17,3 | 1 131 |
| 1ª tabela, só salário | 29 954 | 26 835 | 1,116 | 14,0 | 17,5 | 1 370 |
| 1ª tabela, só manutenção | 30 830 | 26 945 | 1,144 | 13,9 | 17,1 | 900 |
| bot respeita a pausa (D-C8d), 1ª tabela | 29 056 | 26 707 | 1,088 | 14,0 | 18,0 | 2 271 |
| bot respeita a pausa, 75% | 29 251 | 26 790 | 1,092 | 14,0 | 17,8 | 1 709 |
| **bot respeita a pausa, 60% (escolhida, arredondada)** | **30 165** | **26 890** | **1,122** | 14,0 | 17,5 | 1 367 |
| bot respeita a pausa, salário pela metade | 29 417 | 26 803 | 1,097 | 14,0 | 17,7 | 1 585 |

O ganho cai quase em linha com o tamanho da conta. Escolhi 60%, a maior conta que deixa o portão com folga parecida com a dos outros (o menor deles é 1,118).

### D-C8d. O bot respeita a pausa

Com a conta atrasada, o bot continuava escolhendo o item mais barato. Se era uma melhoria pausada, não comprava nada, nem a rota que poderia abrir. Agora, em pausa, ele só olha rota e viagem, que é o que a tela vai deixar comprar. Sem pausa, nada muda: com a conta desligada, a tabela é a da Fase D1, número a número.

### D-C8e. Quase todo bot fica sem caixa no 1º fechamento

O bot compra o que couber assim que pode, e no fim do dia 0 o caixa está perto de zero em quase todas as sementes (sem o passe, em todas). A conta fica atrasada por alguns minutos, e só o LentoGuiado repete no dia 1. Uma pessoa que vê "a conta de hoje" pode guardar a reserva. A tela precisa mostrar a conta antes da virada.

### O peso da conta (`Medida_FaseC8`, 200 sementes, com o passe)

| bot | ondas | embarque | receita | conta paga | conta ÷ receita | dias sem caixa no fechamento |
|---|---|---|---|---|---|---|
| Bom | 10 | 95,5% | 8 883 | 561 | 6,3% | 0,92 |
| Lento | 10 | 79,6% | 5 221 | 496 | 9,5% | 1,00 |
| Parado | 10 | 89,2% | 4 819 | 474 | 9,8% | 1,00 |
| Guiado | 20 | 92,0% | 35 536 | 1 499 | 4,2% | 0,89 |
| LentoGuiado | 20 | 74,4% | 17 501 | 1 431 | 8,2% | 1,82 |
| Guiado | 64 | 91,1% | 212 798 | 5 968 | 2,8% | 0,89 |
| LentoGuiado | 64 | 76,7% | 124 377 | 5 861 | 4,7% | 1,82 |

Sem o passe, a conta era 7,7% a 11,0% da receita em 10 ondas e 3,3% a 5,4% em 64: o 2× do passe aumenta a receita, e a conta fica igual.

### Os portões com o passe, antes e depois (200 sementes, a `main` com o PR #49)

| portão | antes | depois |
|---|---|---|
| TA-1 Bom (10 ondas) | 95,5%, receita 8 913 | 95,5%, receita 8 883 |
| TA-1 Lento | 80,6%, receita 5 401 | 79,6%, receita 5 221 |
| TA-1 Parado | 89,2%, receita 4 887 | 89,2%, receita 4 819 |
| TA-2 (Bom / Parado) | 1,824 | 1,843 |
| Fase C (Planejador / Automático, 20 ondas) | 1,176 | 1,182 |
| Fase C2, as 4 estações | 1,366 | 1,343 |
| Fase C5, campanha (Guiado / Bom, 20 ondas) | 1,380 | 1,395 |
| Fase C6, missões (Missionário / Bom, 20 ondas) | 1,438 | 1,451 |
| Fase C7, passe: o LentoCompleto fecha os 30 níveis | **200 de 200**, dia 20,0 (p90 21,8, pior 23,9) | **200 de 200**, dia 19,9 (p90 21,5, pior 23,4) |
| Fase D1, sem compra fecham os 30 | 200 de 200: Guiado na onda 51,3, LentoGuiado 62,9 (pior 85) | 200 de 200: Guiado 51,2, LentoGuiado 62,4 (pior 81) |
| Fase D1, a Baleia fecha a campanha em | 0,996 do tempo | 0,998 |
| Alcance da campanha e das conquistas | — | os 30 e as 20 em 200 de 200; LentoGuiado fecha a campanha na onda 62,4 (p90 69); última conquista na onda 148,7 e 172,6 |

As Fases C3 e C4 e o TA-6 medem com o passe desligado (`SemFaseC4`) e dão a tabela de baixo. Quem nunca olha para as missões fecha o passe em menos sementes com a conta: Bom 79 de 200 (eram 84) e Lento 99 de 200 (eram 111). O passe não promete nada a eles.

### Os portões sem o passe, antes e depois (200 sementes, medidos antes de o PR #49 entrar)

| portão | antes | depois |
|---|---|---|
| TA-1 Bom (10 ondas) | 95,0%, receita 6 740 | 94,4%, receita 6 529 |
| TA-1 Lento | 78,8%, receita 4 219 | 79,1%, receita 4 264 |
| TA-1 Parado | 88,5%, receita 4 709 | 88,5%, receita 4 627 |
| TA-2 (Bom / Parado) | 1,431 | **1,411** |
| Fase C (Planejador / Automático, 20 ondas) | 1,152 | 1,157 |
| Fase C2, as 4 estações | 1,371 | 1,353 |
| Fase C3, rotas em Vale Verde, Costa Azul e Aurora | 1,221, 1,124 e 1,273 | 1,213, 1,118 e 1,255 |
| Fase C3, aeroportos | 1,151 | **1,122** |
| Fase C3, incidentes na Costa Azul e em Aurora | 1,146 e 1,170 | 1,162 e 1,203 |
| Fase C4, clima na Costa Azul e em Aurora | 1,161 e 1,147 | 1,166 e 1,140 |
| Fase C4, falhas em Vale Verde, Costa Azul e Aurora | 1,134, 1,131 e 1,124 | 1,139, 1,139 e 1,135 |
| Fase C5, campanha (Guiado / Bom, 20 ondas) | 1,114 | 1,142 |
| Fase C6, missões (Missionário / Bom, 20 ondas) | 1,147 | 1,164 |
| Fase D1, sem compra fecham os 30 | 200 de 200: Guiado na onda 51,6, LentoGuiado 62,8 (pior 99) | 200 de 200: Guiado 51,6, LentoGuiado 63,3 (pior 99) |
| Fase D1, a Baleia fecha a campanha em | 0,990 do tempo | 0,991 |
| TA-6, melhor ÷ segunda rota (Vale Verde, Costa Azul, Aurora) | 1,020, 1,047 e 1,161 | 1,066, 1,055 e 1,161 |
| Alcance da campanha (`Alcance_FaseC5`, até 120 ondas) | os 30 em 200 de 200 | os 30 em 200 de 200; Guiado 51,6 (p90 56), LentoGuiado 63,3 (p90 71) |
| Alcance das conquistas (`Alcance_FaseC6`, até 240 ondas) | as 20 em 200 de 200; última na onda 148,5 e 172,8 | as 20 em 200 de 200; última na onda 148,5 e 172,7 |

**Passa, com e sem o passe.** Todos os portões seguem verdes. O que mais cai é o dos aeroportos, pelo motivo da D-C8c. A campanha e as missões sobem porque a receita do Bom, que não persegue nada, cai mais que a de quem recebe as recompensas (sem o passe, −533 contra +90 em 20 ondas).

### Prova de mutação (`MUTACAO_TESTES="FullyQualifiedName~DespesasTests|FullyQualifiedName~SaveTests|FullyQualifiedName~ExploitTests" python client/tools/mutacao.py despesa`)

As 14 travas novas entram no `mutacao.py`. Resultado em 2026-11-03: **14 de 14 mortos**. Das travas anteriores, conferi que os trechos ainda existem no núcleo, sem rodar de novo.

| trava retirada | testes que caíram |
|---|---|
| o caixa paga só o que tem | 9, entre eles `FaltaDeCaixa_CashNuncaNegativo_PausaEQuitaSozinho` |
| a falta não acumula | `Falta_PerdoadaNoFechamentoSeguinte_NuncaViraDivida` e mais 2 |
| a atrasada só sai inteira | 8, entre eles `FaltaDeCaixa_CashNuncaNegativo_PausaEQuitaSozinho` |
| a falta pausa contratar e melhorar | `FaltaDeCaixa_CashNuncaNegativo_PausaEQuitaSozinho` |
| cobra só na virada do dia | 31, entre eles `Fechamento_CobraNoPrimeiroPassoDoDiaSeguinte_UmaVez` |
| regra desligada não cobra | 6, entre eles `RegraDesligada_SoORelatorio` |
| só a estação ativa cobra | `Folha_QuemEstaContratado_EAsEstacoesAtivasPeloNivel` |
| o nível da estação cobra | `Folha_QuemEstaContratado_EAsEstacoesAtivasPeloNivel` |
| offline não cobra | 4, entre eles `Invariante_C8_Offline_NaoCobraNemViraODia` |
| o relatório guarda a fonte | `Relatorio_ReceitaPorFonte_BateComOsEventos` e mais 1 |
| a conta atrasada vai no save | 5, entre eles `Save_ReabrirNoMeioDaOnda_DaOMesmoJogo` |
| atrasada do save limitada | `LixoDoRelatorio_ViraPadraoLimitado` |
| dia do save nunca no futuro | `LixoDoRelatorio_ViraPadraoLimitado` |
| pago do save até a conta | `LixoDoRelatorio_ViraPadraoLimitado` |

### Para medir com gente

- **A conta é pequena.** Com o passe, pesa 6% a 10% da receita nas 10 primeiras ondas e 3% a 5% em 64 ondas. Não resolve o Cash sem destino (§9 e §10): para isso faltam o mapa que cresce e a construção. Com mais sumidouro, dá para subir a tabela de novo, medindo os aeroportos.
- **Contratar sem demanda ainda não dá prejuízo.** O pior salário (90) é menos de 1% da receita de um dia a partir do dia 2. O GDD de direção §12 pede que dê; isso precisa da demissão (frente D), para o jogador poder corrigir.
- **O 1º fechamento sempre aperta** (D-C8e). Ver se a pessoa entende a pausa e o que fazer: mostrar o valor que falta e a hora do fechamento.

## 14. Registro de ocorrências (2026-11-04, frente `ta-historico`)

Não tem número de balanço: só observa. A prova tem duas partes.

- **No mesmo código:** `Invariante_SoObserva_DesligadoDaOMesmoJogo_NumeroANumero` joga 9 cenários (o padrão, os 4 incidentes, um clima e as 3 falhas de avião), com o Bom e o Parado, 10 ondas cada, com o registro ligado e desligado (`Tuning.RegraHistorico`). Os eventos saem iguais passo a passo, e o save, igual fora da linha `hist=`.
- **Contra a `main`:** os portões do §5.13, da campanha, das missões e do passe têm de sair iguais no CI do PR. Medi a `main` de 03/11 (`0b76529`, antes da frente A) e o registro entrou depois da frente A, então a base é a coluna "depois" do §13 (a `main` com a conta e o passe):

| portão | `main` de 03/11 | `main` com a frente A (§13) |
|---|---|---|
| §5.13 (Planejador / Automático, 20 ondas) | 1,176: receita 21 447 contra 18 231; só tripulação 1,030, só manutenção 1,176, as duas 1,194 | 1,182 |
| campanha (Guiado / Bom, 20 ondas) | 1,380: receita 35 568 contra 28 361 | 1,395 |
| missões (Missionário / Bom, 20 ondas) | 1,438: receita 37 415 contra 28 361 | 1,451 |
| passe (LentoCompleto) | 200 de 200, dia 20,0, p90 21,8, pior 23,9, 28 300 de Cash, embarque 77,7% | 200 de 200, dia 19,9, p90 21,5, pior 23,4 |

### Prova de mutação (`MUTACAO_TESTES="FullyQualifiedName~HistoricoTests" python client/tools/mutacao.py historico:`)

As 19 travas novas entram no `mutacao.py`. Resultado em 2026-11-04, depois da junção com a frente A: **19 de 19 mortos**. A 1ª rodada deu 16: "travou onde ficou a maioria" sobreviveu, porque em todo teste quem perdia o voo estava numa estação só. Entrou `VooPerdido_ExplicaOndeFicouAMaioria` e esse mutante foi repetido.

| trava retirada | testes que caíram |
|---|---|
| só observa: não mexe no voo | `Invariante_SoObserva_...` e a migração do schema 10 |
| só observa: não mexe no avião | os 2 testes da pane |
| desligado não anota (eventos e portão) | `Invariante_SoObserva_...` |
| retenção de 3 dias do jogo | `Retencao_TresDiasDoJogo_ODiaQueViraSaiSemEvento` |
| teto do dia, ao anotar e no save | `TetoDoDia_FicamAsMaisNovas` |
| passo e perdidos do save limitados | `LixoDoHistorico_ViraPadraoLimitado` |
| o registro vai no save | `IdaEVolta_ComOcorrencias_TextoEstavel`, `Retomada_ReabrirNoMeio_DaOMesmoRegistro` |
| o custo do reparo pago | `Pane_ReparoPago_GuardaOCusto` |
| explica a estação sem ninguém | `VooPerdido_SegurancaSemNinguem_ExplicaSemEquipe` |
| travou onde ficou a maioria | `VooPerdido_ExplicaOndeFicouAMaioria` |
| o atraso da decolagem | os testes da rejeição e do pátio lento |
| a pane fecha quando o avião volta ao normal | a pane sem pagar e o retorno |
| a falha de avião guarda o desgaste | a pane sem pagar |
| a rejeição não fecha o incidente do mesmo índice (o `Pico` e a `Rejeicao` valem 2) | `Incidente_EntraNoAviso_FechaComOResultadoDoVoo` |
| a falta de caixa entra no fechamento (frente A) | `FaltaDeCaixa_EntraNoFechamento_FechaQuandoOCaixaQuita` |
| a falta fecha quando o caixa quita | `FaltaDeCaixa_EntraNoFechamento_FechaQuandoOCaixaQuita` |

## 15. Frente E: transferência pelo vestiário (2026-11-04, `GDD.md` §4.1, linha E)

Mover alguém de estação passa pelo vestiário (`Sim.Vestiario.cs`). A pessoa termina o atendimento em curso (o encerramento, quando os outros presentes não cobrem os atendimentos), anda ao vestiário, troca o uniforme e anda ao posto novo. Em todo esse tempo ela não atende em nenhuma das duas estações. Voltar ao setor do uniforme que ela ainda veste é só a caminhada. Nada novo no save: a fase sai do `Andando`, e o `De` guarda o uniforme até o fim da troca.

### D-Ea. A transferência não pode durar mais que a caminhada da v0.1

O primeiro desenho somava 2,5 s de ida, 3 a 5 s de troca por estação e 2,5 s de volta. O TA-2 passava com folga, mas o LentoGuiado parava no objetivo 18 ("Fortaleza lotada", 3 voos com 90% na rota de Fortaleza) e não fechava a campanha. Medi o tempo total (sem o encerramento), com 40 sementes para o TA-2 e as sementes 1 a 4 para a campanha:

| transferência | TA-2 | TA-1 Lento | LentoGuiado: objetivos em 120 ondas |
|---|---|---|---|
| 2,5 s (a v0.1, sem vestiário) | 1,823 | 80,6% | 30, 30, 30, 30 |
| 2,5 s com o vestiário e o encerramento | 1,800 | 80,8% | 30, 30, 30, 30 |
| 3,5 s | 1,789 | 80,4% | 30, 30, 30, 30 |
| 4 s | 1,764 | 79,0% | 27, 29, 23, 30 |
| 5 s | 1,758 | 79,0% | 18, 18, 18, 18 |
| 8 s (troca de 3 s) | 1,709 | 76,5% | 18, 18, 11, 11 |

Com 200 sementes, os 3,5 s deram **131 de 200** no alcance da campanha e derrubaram o `Alcance_FaseC5` e o `Portao_FaseD1_Compras`. Quem trava é o voo cheio: um segundo a mais na mudança custa os últimos passageiros do voo, e o bot lento, que move 4 a 5 vezes por onda, não chega aos 90%. Um bot que reage antes, pelo tempo a mais da transferência, não resolveu: com 8 s, 18/18/18/11, e o TA-2 caiu para 1,664. Ficou de fora.

**Decisão:** ida, troca de 1 s (`Tuning.Troca`) e volta somam os 2,5 s da caminhada da v0.1. O custo novo de mover é o encerramento. Se o playtest pedir uma troca mais longa, o objetivo de voo cheio da campanha precisa mudar antes (decisão do Vinicius).

### Os portões, com o vestiário (200 sementes, a `main` com a despesas, PR #56)

| portão | antes (a `main`, §13) | com o vestiário |
|---|---|---|
| TA-1 Bom (10 ondas) | 95,5%, receita 8 883 | 94,8%, receita 8 740 |
| TA-1 Lento | 79,6%, receita 5 221 | 79,9%, receita 5 222 |
| TA-1 Parado | 89,2%, receita 4 819 | 89,2%, receita 4 819 |
| TA-2 (Bom / Parado) | 1,843 | **1,814** |
| Fase C (Planejador / Automático, 20 ondas) | 1,182 | 1,182 |
| Fase C2, as 4 estações | 1,343 | 1,340 |
| Fase C5, campanha (Guiado / Bom, 20 ondas) | 1,395 | 1,411 |
| Fase C6, missões (Missionário / Bom, 20 ondas) | 1,451 | 1,461 |
| Fase C7, passe: o LentoCompleto fecha os 30 níveis | 200 de 200, dia 19,9 (pior 23,4) | 200 de 200, dia 19,9 (pior 23,5) |
| Fase D1 e alcance da campanha, sem compra | 200 de 200: Guiado 51,2, LentoGuiado 62,4 (pior 81) | 200 de 200: Guiado 51,5, **LentoGuiado 69,5 (p90 80, pior 103)**; a Baleia fecha em 0,994 do tempo |
| Alcance das missões e conquistas | 200 de 200 | 200 de 200 |
| Fases C3 e C4 e TA-6 | verdes | iguais: medem com o vestiário desligado (`SemFaseC4`) |

Antes da despesas entrar, a mesma medida deu TA-2 1,787 (era 1,824) e LentoGuiado na onda 69,8 (pior 106), também com todos os portões verdes.

**Ressalva:** o LentoGuiado leva 7 ondas a mais para fechar a campanha, e o pior caso fica a 17 ondas do teto de 120. O encerramento pesa para quem move muito. As tabelas "antes" (`SemC8` do `BalanceTests`) e as migrações dos saves reais dos schemas 2 a 9 jogam com o vestiário desligado, como o jogo que as gravou.

### Prova de mutação (`MUTACAO_TESTES="FullyQualifiedName~VestiarioTests|FullyQualifiedName~RegrasTests|FullyQualifiedName~SaveTests" python client/tools/mutacao.py vestiario`)

Resultado em 2026-11-04: **6 de 6 mortos**.

| trava retirada | testes que caíram |
|---|---|
| voltar ao setor do uniforme não troca | `VoltarAoSetorDoUniforme_SoACaminhada_DepoisDaTroca_VestiarioDeNovo` |
| quem sai termina o atendimento | `Encerrando_TerminaOAtendimentoNoPostoAntesDeSair` |
| o outro no posto cobre o atendimento | `ComOutroNoPosto_SaiSemEsperar` |
| o uniforme só muda no fim da troca | 5, entre eles `Invariantes_NoBotLento_TresOndas` e `MigracaoDaV01_AbreComTudoEOVooDaFilaEmbarca` |
| a fase sai do `Andando` | 4, entre eles `Transferencia_EncerraVaiTrocaEChega_NaOrdem_UniformeSoNoVestiario` |
| o save guarda a transferência inteira | `Encerrando_TerminaOAtendimentoNoPostoAntesDeSair` |

## 16. Fase C9: os 3 candidatos e a demissão (2026-11-03, `CONTRATO_FASE_C9.md`)

Cada vaga tem um Júnior, um Pleno e um Sênior fixos. O Pleno é a pessoa e o preço de antes; os outros dois rendem, custam e ganham em % dele (o `CONTRATO_FASE_C9.md` §2 tem a tabela). Os bots de sempre contratam o Pleno e nunca demitem: o jogo deles é o de antes, número a número. A migração do save real do schema 10 prova isso (migrar = jogar até ali com o código novo).

### A sonda: quanto a experiência pesa (40 sementes × 20 ondas, todos os perfis iguais)

| equipe | Bom: lucro | Lento: lucro | Guiado: lucro |
|---|---|---|---|
| toda Pleno (o jogo de antes) | 26 374 | 14 128 | 33 208 |
| toda Sênior (125%, contratação 200%, salário 150%) | 29 754 (1,128) | 17 911 (1,268) | 40 707 (1,226) |
| toda Júnior (80%, contratação 50%, salário 70%) | 23 050 (0,874) | 8 584 (0,608) | 31 495 (0,948) |
| Sênior de graça (125%, preço do Pleno) | 32 943 | 19 695 | 35 827 |
| Júnior de graça (80%, preço do Pleno) | 22 811 | 8 053 | 30 121 |

Lucro = receita − conta paga. **Ao contrário do especialista** (só visual, ganho 1,00, D-C2a), a experiência pesa muito: o especialista só vale na casa dele, e a experiência vale em toda estação onde a pessoa estiver. O salário é pequeno perto do que cada pessoa rende, por isso o Sênior compensa mesmo pagando o dobro.

### D-C9c. A decisão é de caixa, não de tipo

Com estes números o Sênior quase sempre é o melhor. O Júnior põe gente no posto cedo, com pouco Cash, e a demissão serve para trocá-lo por alguém melhor depois. É o que o bot `Contratador` faz. Se o playtest achar o Sênior óbvio demais, o ajuste é no preço (`ExpContratacao`), não na lista.

### O portão do §5.13 (`Portao_FaseC9_Candidatos`)

O `Contratador` (e o `LentoContratador`) compara os 3, contrata o melhor que o caixa paga e troca pelo Sênior, pesando a troca pela metade do preço (D-C9d). O portão compara o lucro dele com o do Bom (e do Lento), que contrata sempre o Pleno, com as mesmas sementes, 200 × 20 ondas.

| bot | embarque | receita | conta paga | lucro | lucro do Pleno | ganho |
|---|---|---|---|---|---|---|
| Contratador | 96,2% | 30 957 | 1 652 | 29 305 | 26 213 | **1,118** |
| LentoContratador | 88,0% | 20 461 | 1 581 | 18 881 | 13 356 | **1,414** |

Com o vestiário da frente E (PR #63), que entrou depois: antes dele, 1,118 e 1,405.

### D-C9d. O bot que mede a escolha troca pelo Sênior com prioridade

A 1ª versão do `Contratador` pesava a troca pelo preço inteiro: deu 1,120 com 40 sementes e **1,099 com 200** no CI (PR #61), um milésimo abaixo do piso. As variantes, todas com 200 sementes (Bom / Lento):

| variante | Bom | Lento |
|---|---|---|
| vaga pelo preço do Júnior, troca pelo preço inteiro (a 1ª) | 1,099 | 1,323 |
| vaga pelo preço do Sênior: espera o caixa e contrata o Sênior | 1,030 | 0,324 |
| vaga pelo preço do Pleno, troca pelo preço inteiro | 1,094 | 1,282 |
| vaga pelo preço do Pleno, sem troca | 1,027 | 1,057 |
| vaga pelo preço do Júnior, sem troca | 0,936 | 0,854 |
| **vaga pelo preço do Júnior, troca pesando metade do preço (a final)** | **1,118** | **1,405** |
| vaga pelo preço do Júnior, troca pesando um quarto | 1,121 | 1,398 |

Esperar o Sênior para contratar atrasa demais, e o Lento desaba (44% de embarque). O que rende é pôr gente no posto cedo e trocar pelo Sênior logo. A troca pesa metade porque o Sênior rende 25% na estação toda, cerca do dobro de um nível de velocidade (12%). Nenhum número do jogo mudou: só o bot que mede.

### Prova de mutação (`MUTACAO_TESTES="FullyQualifiedName~EquipeTests|FullyQualifiedName~SaveTests.Lixo|FullyQualifiedName~SaveTests.MigracaoDoSchema10|FullyQualifiedName~DespesasTests" python client/tools/mutacao.py candidato demitir`)

As 13 travas novas entram no `mutacao.py`. Resultado em 2026-11-03: **13 de 13 mortos**.

| trava retirada | testes que caíram |
|---|---|
| cada um custa o preço dele | `Hire_Recusa_SemCaixa_NivelInvalido_AntesDoMarco` e mais 1 |
| salário pela experiência | `Tabela_TresCandidatos_OPlenoEOPrecoDeAntes_NenhumDomina` |
| a folha é a da experiência | `Hire_CadaNivel_CobraOPrecoDele_EEntraNaFolha` e mais 1 |
| rende pela experiência | `Rende_PeloNivel_NoTempoNaVendaENaForca` |
| regra desligada iguala os 3 | `Rende_PeloNivel_NoTempoNaVendaENaForca` |
| vaga fechada não contrata | `Hire_CadaNivel_CobraOPrecoDele_EEntraNaFolha` e mais 1 |
| experiência inválida não contrata | `Hire_Recusa_SemCaixa_NivelInvalido_AntesDoMarco` |
| demitir não devolve Cash | `Invariante_C9_ContratarEDemitirNuncaDaCash` e mais 1 |
| quem vinha depois sobe | `Demite_AtualizaEscalaFolhaECobertura_EReabreAVaga` e mais 1 |
| demitir vale com a conta atrasada | `Invariante_C9_ComContaAtrasada_DemitirTiraDaPausa` |
| a experiência vai no save | `MigracaoDoSchema10_SaveReal_TodosPleno` e mais 1 |
| lixo no save vira o Pleno | `LixoNoNivel_ViraOPleno` |
| teto do atendimento é o do mais lento | `Lixo_ViraPadraoLimitado` e mais 1 |

### Para medir com gente

- **O Sênior parece óbvio?** Ver se a pessoa compara os 3 ou vai direto no mais caro, e se usa o Júnior para começar.
- **Demitir sem custo** (D-C9b). Ver se alguém demite e recontrata para fugir da conta; a conta é do estado na hora do fechamento (D-C8b), então demitir antes e recontratar depois economiza no máximo um salário, pagando a contratação de novo.

## 17. Frente F: limpeza e banheiro (2026-11-04, `GDD.md` §4.1, linha F)

Banheiro, sujeira e equipe de limpeza no núcleo (`Sim.Limpeza.cs`, save schema 14) e na tela (`Mundo.Limpeza.cs`). **Antes** = `RegraLimpeza = false`, que dá o jogo de antes número a número: sem banheiro no caminho, sem sujeira, sem equipe de limpeza e sem o salário dela. As tabelas "antes" (`SemC8` e `SemFaseC4` do `BalanceTests`) e as migrações dos saves reais jogam com a regra desligada, como o jogo que as gravou.

| | valor |
|---|---|
| passageiro que precisa do banheiro (semente) | 35% |
| cabines, uso, caminhada do portão à porta (cada perna) | 2, 6 s, 8 s |
| folga que sobra antes de o embarque abrir, com a volta | 10 s |
| sujeira por passageiro atendido (check-in, segurança, loja, portão, bagagem, alimentação; banheiro por uso) | 4, 4, 6, 3, 4, 12; 20 (de 1 000) |
| gatilho da escala automática, limpeza por s | 250, 60 |
| equipe: no jogo novo, contratações, salário por dia | 1; 60 e 150; 15 |

### D-Fa. O banheiro não pode fazer perder voo

Só vai ao banheiro quem volta antes de o embarque abrir, com 10 s de sobra (`AoPortao`): a viagem acontece enquanto a pessoa esperaria sentada no portão. Quem não tem folga vai direto. Se mesmo assim alguém estiver lá quando o portão fecha (portão adiantado pelo clima, save editado), o voo leva, como numa fila (`FechaOVoo_LevaQuemFicouNoBanheiro`). Com 5 sementes × 4 ondas do Bom e do Lento, nenhum voo fechou com gente no banheiro (`Invariante_NinguemFicaNoBanheiroQuandoOVooFecha`).

### D-Fb. A sujeira ainda não mexe no Cash

A sujeira e a fila do banheiro ficam prontas para a satisfação (frente G) ler (`CONTRATO_LANCAMENTO.md` §5). Hoje o efeito de jogo é o do banheiro: interdita na sujeira máxima, a fila volta ao portão (`Negados`) e só reabre limpo. Com 1 zelador ele quase nunca interdita (0 em 200 sementes), então contratar o 2º e o 3º só limpa mais rápido. O portão do §5.13 dessa escolha (contratar limpeza rende ≥ 1,10) só faz sentido quando a G der peso à sujeira; até lá, a contratação existe no núcleo sem tela.

### O portão da F (`Portao_FrenteF_Limpeza`, 200 sementes, medido no PC com `--filter`)

| bot | ondas | embarque sem / com | receita sem / com | usos do banheiro | maior fila | negados | interdições | limpezas | sujeira média (de 1 000) |
|---|---|---|---|---|---|---|---|---|---|
| Bom | 10 | 94,8% / 94,9% | 8 740 / 8 745 | 52,2 | 5 | 0 | 0 | 27,4 | 77 |
| Lento | 10 | 79,9% / 80,0% | 5 222 / 5 234 | 42,9 | 2 | 0 | 0 | 21,0 | 76 |
| Guiado | 20 | 91,6% / 91,6% | 35 621 / 35 643 | 61,0 | 6 | 0 | 0 | 74,3 | 106 |

O portão exige embarque com a regra ≥ sem a regra − 0,5 ponto e receita ≥ 98%. O salário (15 por dia) quase não pesa. A receita com a regra sai um pouco maior; a diferença (0,1% a 0,2%) é do tamanho do ruído das decisões do bot, que lê o portão com menos gente enquanto alguns estão no banheiro. Os portões de antes rodam no CI deste PR, com a regra ligada.

### Prova de mutação (`MUTACAO_TESTES="FullyQualifiedName~LimpezaTests|FullyQualifiedName~DespesasTests|FullyQualifiedName~SaveTests|FullyQualifiedName~EquipeTests" python client/tools/mutacao.py limpeza:`)

Resultado em 2026-11-04: **9 de 9 mortos** (o das cabines sobreviveu na 1ª rodada e ganhou o `Cabines_NoTeto_OResto_EsperaNaFilaNaOrdem`).

| trava retirada | testes que caíram |
|---|---|
| só vai ao banheiro quem volta antes do embarque | `Invariante_NinguemFicaNoBanheiroQuandoOVooFecha` |
| as cabines no teto | `Cabines_NoTeto_OResto_EsperaNaFilaNaOrdem` |
| o banheiro interditado não recebe | `Interdicao_NaSujeiraMaxima_AFilaVolta_OZeladorVaiPrimeiro_EReabreLimpo` |
| só limpa depois de chegar | 3, entre eles `Escala_VaiAMaisSujaAcimaDoGatilho_SoLimpaDepoisDeChegar_ELiberaNoZero` e `Invariantes_NoBotLento_TresOndas` |
| a ordem do jogador antes da escala | `Ordem_DoJogador_VemAntesDaEscala_EsperaUmLivre_ERecusaOQueNaoFazSentido` |
| o salário na folha da equipe | 7, entre eles `Folha_QuemEstaContratado_EAsEstacoesAtivasPeloNivel` |
| contratar pausa com a conta atrasada | `Contratacao_PrecoFixo_TetoDe3_PausaComAContaAtrasada_FolhaSobe` |
| o save guarda quem está na cabine | 3, entre eles `Save_NoMeioDoBanheiroEDaLimpeza_IdaEVoltaEstavel_EOMesmoJogo` |
| o voo que fecha leva quem ficou no banheiro | `FechaOVoo_LevaQuemFicouNoBanheiro` |

### Para medir com gente

- **O banheiro se nota?** Com 2 cabines a fila passa de 5 pessoas só no pico. Ver se a pessoa repara no banheiro e na fila sem a satisfação (G) dizer o motivo.
- **Mandar limpar.** A ordem existe no núcleo, sem tela ainda. Quando a tela entrar, ver se a pessoa manda limpar a área suja ou deixa a escala automática.


## 18. Frente G: satisfação com motivo (2026-11-04, `GDD.md` §4.1, linha G)

Satisfação de cada passageiro e do aeroporto no núcleo (`Sim.Satisfacao.cs`, save schema 17) e na tela (ficha do passageiro e selo do HUD, `Hud.Satisfacao.cs`). Só lê o que os outros sistemas já fazem; com `RegraSatBonus = false`, o padrão, o jogo é o mesmo número a número (`SoLe_SemAGorjeta_OJogoEOMesmoNumeroANumero_ComAGorjetaNinguemContenteNaoGanhaNada`).

| | valor |
|---|---|
| nota de partida; o que cada fator soma quando vai bem (fila, limpeza, banheiro, pontualidade, conforto) | 75; 5, 5, 3, 5, 3 |
| o pior que cada fator tira | 45, 20, 15, 25, 15 |
| tolerância de fila em s (executivo, família, conexão, viagem longa) | 45, 70, 60, 90 |
| tolerância de sujeira (de 1 000) | 600, 400, 600, 500 |
| tolerância de embarque atrasado e de espera em pé, em s | 20, 40, 15, 40; 30, 20, 30, 20 |
| fila do banheiro tolerada; quem achou fechado | 10 s; -15 |
| cadeiras do portão (as da planta, `Mundo.Espera`) | 48 |
| carinha contente, brava, reclama | ≥ 70, < 50, < 40 |
| média do aeroporto | os últimos ~100 que saíram |
| gorjeta (desligada) | 10% da tarifa de quem embarca com ≥ 80 |

Cada fator anda numa reta pela tolerância: soma o máximo com 0, fica em 0 na metade e tira o máximo na tolerância, até o teto. Atraso e espera em pé só tiram. O perfil é o tipo da rota do voo (curta = executivo, turística = família, conexão, longa = viagem longa); o grupo extra de conexão do incidente é conexão.

### D-Ga. A 1ª tabela deixava todo executivo bravo

Com 20 s de fila tolerados pelo executivo e sujeira 400, o Bom ficava com 57 de média e o Parado com 58: a nota não separava quem joga bem de quem não joga. Com as tolerâncias acima (fila somada na viagem toda) e **quem perde o voo na fila sai com a fila no teto**, o Bom fica em 67, o Lento em 65 e o Parado em 54 (40 sementes × 20 ondas; com 200, no portão abaixo, Bom 68,0 e Lento 64,1). Quem perde o voo sai com 75 - 45 + 5 = 35: bravo, e reclama da fila.

### D-Gb. A gorjeta fica desligada: a satisfação é só leitura

O portão do §5.13 pergunta se reagir aos motivos rende ≥ 1,10 × não reagir. O bot `Satisfeito` olha os 3 motivos que mais pesam contra e reage: manda limpar onde suja, contrata zelador e compra velocidade na estação da fila. Com a gorjeta ligada nos dois, o ganho é 1,000 (Bom) e 0,996 (Lento): a gorjeta paga quase igual a quem reage e a quem não reage, porque o motivo que pesa é a fila, e o bot de sempre já move gente para ela. Gorjeta maior viraria moeda, o que o §6.1 proíbe. Ela fica no núcleo, desligada (`RegraSatBonus = false`), e a satisfação diz o porquê sem mexer no Cash.

### O portão da G (`Portao_FrenteG_Satisfacao`, 200 sementes × 20 ondas, medido no PC com `--filter`)

| bot | embarque sem / com gorjeta | receita sem / com | gorjetas (Cash) | satisfação média | reclamações | lucro reagindo | lucro sem reagir | ganho |
|---|---|---|---|---|---|---|---|---|
| Bom | 93,1% / 93,4% | 27 678 / 28 325 | 381 | 68,0 | 143,6 | 26 867 | 26 866 | 1,000 |
| Lento | 77,2% / 78,1% | 14 726 / 15 231 | 259 | 64,1 | 341,4 | 13 802 | 13 854 | 0,996 |

O portão exige que a gorjeta não puna (embarque com ela ≥ sem ela - 0,5 ponto e receita ≥ 98%) e só deixa ligar com ganho ≥ 1,10. As reclamações são quase todas de quem perdeu o voo na fila. A gorjeta ligada daria 1,4% (Bom) e 1,7% (Lento) a mais de receita a qualquer um.

### Prova de mutação (`MUTACAO_TESTES="FullyQualifiedName~SatisfacaoTests|FullyQualifiedName~SaveTests|FullyQualifiedName~LimpezaTests" python client/tools/mutacao.py satisfacao:`)

Resultado em 2026-11-04: **14 de 14 mortos** na 1ª rodada.

| trava retirada | testes que caíram |
|---|---|
| sem a regra não paga gorjeta | 15, entre eles `Gorjeta_SoComARegra_SoDeQuemEmbarcaContente_SatBonusDaTarifa` e as migrações dos saves reais |
| gorjeta só de quem está contente | `SoLe_SemAGorjeta_...` |
| no portão, a fila só com o embarque aberto; as cadeiras no teto; o atraso só depois da hora de abrir | `Portao_SentaAteAcabarACadeira_AtrasoSoDepoisDaHoraDeAbrir_EAFilaSomaEspera` (os 3) |
| reclama 1x; as reclamações são do dia | `Reclama_UmaVez_DoMotivoPrincipal_EZeraNoDiaSeguinte` (os 2) |
| quem perdeu o voo entra na média | 2, entre eles `Aeroporto_TodoQueSaiEntraNaMedia_OTopEOrdenado_ENadaForaDaFaixa` |
| perdeu na fila, a fila vai ao teto | `PerdeuOVoo_NaFila_AEsperaVaiAoTeto_ESaiNaMedia` |
| o perfil sai da rota | `Perfil_SaiDaRotaDoVoo_OGrupoDeConexaoEConexao` e `Tolerancia_...` |
| a nota de 0 a 100 | `SoLe_SemAGorjeta_...` |
| o save guarda quem está no aeroporto | 3, entre eles `Save_NoMeioDaOnda_IdaEVoltaEstavel_EOMesmoJogo` |
| o save limita o lixo | `Lixo_NoSave_ViraOPadraoLimitado` |
| achou o banheiro fechado | `Banheiro_SoPesaEmQuemPrecisava_FilaENegadoContra_UsarSemFilaAjuda` |

### Para medir com gente

- **O motivo se lê?** Ver se a pessoa toca no selo e entende "Fila · CHECK-IN: -8" sem explicação, e se vai à sala que o toque abre.
- **O executivo e a família se notam?** O perfil aparece só no topo da ficha. Ver se a pessoa repara que a rota turística reclama de sujeira antes.
