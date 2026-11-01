# Missões e conquistas

Fonte: `GDD.md` §6.5. Regras no `CONTRATO_FASE_C6.md`. As tabelas do código são `Tabelas.Diarias`, `Tabelas.Semanais` e `Tabelas.Conquistas` (`Missoes.cs`); um teste compara esta página com elas linha a linha, então mudou aqui, muda lá no mesmo commit.

Como ler:

- **id** é estável. O nome é de trabalho.
- **Marco** é o marco do terminal em que a linha entra na lista. Sem linha velha em marco novo: a lista acompanha o aeroporto.
- **Meta** é a condição medida pelo núcleo, com os contadores da campanha (`CAMPANHA.md`). `Arg` entre colchetes: índice da estação (2 loja, 4 bagagem, 5 alimentação), da rota (`Tabelas.Rotas`) ou do aeroporto.
- **Cash** é fixo. A semente escolhe quais linhas entram, nunca o valor.
- **O dia é o do jogo:** 8 ondas, ou 24 minutos jogados. A semana são 7 dias do jogo, 2,8 h jogadas. Com o jogo fechado nada vira, e a missão espera onde parou.
- Nenhuma missão exige vídeo ou compra, nenhuma recua, e a que ficar aberta na virada sai sem custo.

## Diárias: 3 por dia

Uma linha por meta: as 3 do dia nunca repetem a mesma meta. Clima e incidente ficam de fora, porque a diária não espera a semente.

| id | Nome | Marco | Meta | Cash |
|---|---|---|---|---|
| d0_embarque | Portas abertas | 0 | `Embarcados ≥ 100` | 60 |
| d0_voos | Quadro em dia | 0 | `Voos ≥ 6` | 50 |
| d0_pontual | Na hora certa | 0 | `Pontuais ≥ 2` | 80 |
| d0_melhora | Terminal em obras | 0 | `Melhorias ≥ 2` | 60 |
| d0_loja | Vitrine acesa | 0 | `Atendidos[2] ≥ 15` | 150 |
| d0_historia | Boas histórias | 0 | `Historias ≥ 2` | 60 |
| d1_embarque | Terminal movimentado | 1 | `Embarcados ≥ 300` | 150 |
| d1_cheios | Casa cheia | 1 | `Cheios ≥ 6` | 180 |
| d1_pontual | Relógio afinado | 1 | `Pontuais ≥ 4` | 200 |
| d1_melhora | Reforma do dia | 1 | `Melhorias ≥ 3` | 150 |
| d1_mover | Olho na fila | 1 | `Movidos ≥ 3` | 200 |
| d1_loja | Dia de vendas | 1 | `Atendidos[2] ≥ 30` | 300 |
| d1_comida | Hora do almoço | 1 | `Atendidos[5] ≥ 30` | 200 |
| d1_mala | Esteira rodando | 1 | `Atendidos[4] ≥ 30` | 200 |
| d1_curitiba | Ponte aérea | 1 | `Cheios[2] ≥ 4` | 250 |
| d2_embarque | Fluxo constante | 2 | `Embarcados ≥ 400` | 300 |
| d2_cheios | Sem poltrona vazia | 2 | `Cheios ≥ 8` | 350 |
| d2_pontual | Pontual o dia todo | 2 | `Pontuais ≥ 6` | 400 |
| d2_melhora | Obra no terminal | 2 | `Melhorias ≥ 2` | 300 |
| d2_mover | Gerente de piso | 2 | `Movidos ≥ 4` | 350 |
| d2_checagem | Avião em dia | 2 | `Checagens ≥ 3` | 350 |
| d2_comida | Praça cheia | 2 | `Atendidos[5] ≥ 50` | 400 |
| d2_loja | Corredor de compras | 2 | `Atendidos[2] ≥ 40` | 500 |
| d2_salvador | Rota do sol | 2 | `Voos[4] ≥ 5` | 400 |
| d2_bh | Rota das montanhas | 2 | `Voos[3] ≥ 5` | 400 |
| d3_embarque | Hub lotado | 3 | `Embarcados ≥ 500` | 600 |
| d3_cheios | Lotação esgotada | 3 | `Cheios ≥ 8` | 700 |
| d3_pontual | Painel verde | 3 | `Pontuais ≥ 6` | 800 |
| d3_mover | Chefe de turno | 3 | `Movidos ≥ 5` | 600 |
| d3_checagem | Revisão em dia | 3 | `Checagens ≥ 4` | 600 |
| d3_comida | Banquete | 3 | `Atendidos[5] ≥ 60` | 700 |
| d3_mala | Porão cheio | 3 | `Atendidos[4] ≥ 60` | 700 |
| d3_loja | Duty free | 3 | `Atendidos[2] ≥ 50` | 900 |
| d3_historias | Álbum de viagem | 3 | `Historias ≥ 5` | 500 |
| d3_guarulhos | Conexão perfeita | 3 | `Cheios[8] ≥ 5` | 800 |
| d3_manaus | Rumo ao norte | 3 | `Voos[10] ≥ 3` | 1000 |
| d3_lisboa | Outro lado do mar | 3 | `Voos[11] ≥ 3` | 1200 |

## Semanais: 5 por semana

Duas partes de sistemas diferentes (GDD §6.5). Perigo sempre "qualquer um", nunca um tipo só.

| id | Nome | Marco | Meta | Cash |
|---|---|---|---|---|
| s0_fluxo | Semana de estreia | 0 | `Embarcados ≥ 1000` e `Atendidos[2] ≥ 30` | 500 |
| s0_loja | Vitrine e obra | 0 | `Atendidos[2] ≥ 60` e `Melhorias ≥ 5` | 600 |
| s0_pontual | Boa impressão | 0 | `Pontuais ≥ 10` e `Atendidos[2] ≥ 40` | 700 |
| s0_cheios | Fama de lotado | 0 | `Cheios ≥ 20` e `Atendidos[2] ≥ 50` | 700 |
| s0_voos | Céu aberto | 0 | `Voos ≥ 40` e `Climas ≥ 1` | 500 |
| s0_mover | Equipe em forma | 0 | `Movidos ≥ 30` e `Historias ≥ 6` | 500 |
| s1_fluxo | Semana cheia | 1 | `Embarcados ≥ 1500` e `Movidos ≥ 8` | 800 |
| s1_loja | Compras e malas | 1 | `Atendidos[2] ≥ 80` e `Atendidos[4] ≥ 60` | 800 |
| s1_pontual | Fama de pontual | 1 | `Pontuais ≥ 15` e `Climas ≥ 1` | 1000 |
| s1_cheios | Sempre lotado | 1 | `Cheios ≥ 25` e `Historias ≥ 5` | 1000 |
| s1_voos | Malha aérea | 1 | `Voos ≥ 50` e `Melhorias ≥ 4` | 1000 |
| s1_mover | Time afiado | 1 | `Movidos ≥ 12` e `Atendidos[5] ≥ 60` | 1000 |
| s2_fluxo | Grande semana | 2 | `Embarcados ≥ 2000` e `Checagens ≥ 6` | 1500 |
| s2_comida | Mesa e esteira | 2 | `Atendidos[5] ≥ 120` e `Atendidos[4] ≥ 120` | 1500 |
| s2_pontual | Bom tempo, boa hora | 2 | `Pontuais ≥ 20` e `Climas ≥ 2` | 2000 |
| s2_cheios | Frota em dia | 2 | `Cheios ≥ 30` e `Hangar ≥ 1` | 2000 |
| s2_voos | Imprevisto resolvido | 2 | `Voos ≥ 60` e `Incidentes ≥ 2` | 2000 |
| s2_mover | Terminal na mão | 2 | `Movidos ≥ 15` e `Historias ≥ 8` | 2000 |
| s3_fluxo | Semana do hub | 3 | `Embarcados ≥ 3000` e `Checagens ≥ 8` | 3000 |
| s3_comida | Serviço de bordo | 3 | `Atendidos[5] ≥ 160` e `Atendidos[4] ≥ 160` | 3000 |
| s3_pontual | Hub pontual | 3 | `Pontuais ≥ 25` e `Climas ≥ 3` | 4000 |
| s3_cheios | Frota impecável | 3 | `Cheios ≥ 40` e `Hangar ≥ 2` | 4000 |
| s3_voos | Sala de crise | 3 | `Voos ≥ 70` e `Incidentes ≥ 3` | 4000 |
| s3_mover | Maestro do terminal | 3 | `Movidos ≥ 20` e `Historias ≥ 10` | 4000 |
| s3_manaus | Longo curso | 3 | `Voos[10] ≥ 10` e `Atendidos[5] ≥ 120` | 4000 |
| s3_lisboa | Ponte transatlântica | 3 | `Voos[11] ≥ 10` e `Atendidos[4] ≥ 120` | 4000 |

## Conquistas: 20 permanentes

Valem em qualquer aeroporto e não pagam Cash: a recompensa é o selo. Feita uma vez, feita para sempre, mesmo a sequência que quebrar depois.

| id | Nome | Meta |
|---|---|---|
| c_passageiros_1 | Mil a bordo | `Passageiros ≥ 1000` |
| c_passageiros_2 | Dez mil a bordo | `Passageiros ≥ 10000` |
| c_voos_1 | Cem decolagens | `Voos ≥ 100` |
| c_voos_2 | Duzentas decolagens | `Voos ≥ 200` |
| c_cheios | Cinquenta lotados | `Cheios ≥ 50` |
| c_seguidos | Oito seguidos | `Seguidos ≥ 8` |
| c_pontuais | Cinquenta na hora | `Pontuais ≥ 50` |
| c_movidos | Cem reforços | `Movidos ≥ 100` |
| c_loja | Freguês da loja | `Atendidos[2] ≥ 80` |
| c_comida | Cozinha famosa | `Atendidos[5] ≥ 500` |
| c_mala | Nenhuma mala fora | `Atendidos[4] ≥ 500` |
| c_checagens | Olho na frota | `Checagens ≥ 25` |
| c_hangar | Hangar de confiança | `Hangar ≥ 3` |
| c_climas | Dono do tempo | `Climas ≥ 10` |
| c_incidentes | Sangue-frio | `Incidentes ≥ 10` |
| c_equipe | Time completo | `Equipe ≥ 6` |
| c_aurora | Três aeroportos | `Chegou[2] ≥ 1` |
| c_rotas | Mapa completo | `Rotas ≥ 12` |
| c_campanha | Hub internacional | `Campanha ≥ 30` |
| c_missoes | Cinquenta missões | `Missoes ≥ 50` |
