# Campanha principal: 30 objetivos, 10 por aeroporto

Fonte: `GDD.md` §5.10 e §6.5. Regras no `CONTRATO_FASE_C5.md`. A tabela do código é `Tabelas.Objetivos` (`Objetivos.cs`); um teste compara esta página com ela linha a linha, então mudou aqui, muda lá no mesmo commit.

Como ler:

- **id** é estável: não muda nem com a D-13 (nome do jogo). O nome é de trabalho.
- **Meta** é a condição medida pelo núcleo. `Arg` entre colchetes: índice da estação (`Estacao`), da rota (`Tabelas.Rotas`), do aeroporto ou do tipo de incidente (`TipoIncidente`).
- **Pré-requisito** de todo objetivo: o anterior da cadeia. A coluna *Pede antes* diz só o que o jogo cobra além disso.
- **Cash** é fixo. Nenhum objetivo exige vídeo ou compra, e nenhum depende de um clima raro.
- Os objetivos fecham na ordem. Metas de contador contam com o jogo naquele aeroporto, desde a chegada, também antes de o objetivo virar o ativo.
- **Voo lotado** é voo com pelo menos 90% dos passageiros a bordo (`Tuning.LotadoPct`).

## Vale Verde (regional)

Ensina fluxo, equipe móvel, quadro de partidas e lojas.

| # | id | Nome | Condição | Meta | Cash | Pede antes | Contexto | Ensina |
|---|---|---|---|---|---|---|---|---|
| 1 | vv_embarque | Primeiro embarque | Embarque 10 passageiros | `Embarcados ≥ 10` | 20 | nada | A Lara abre o balcão. O primeiro voo sai em dois minutos. | tocar no check-in, na segurança e no portão |
| 2 | vv_ana | Alguém no balcão | Contrate 1 pessoa | `Equipe ≥ 1` | 30 | 20 Cash | A Ana procura emprego e já conhece o balcão. | contratar |
| 3 | vv_equipe | Turno completo | Tenha 3 pessoas na equipe | `Equipe ≥ 3` | 100 | 550 Cash no total | Com o Beto e o Caio, o toque vira exceção. | uma pessoa por estação do fluxo |
| 4 | vv_mover | Onde a fila aperta | Mova alguém 3 vezes para uma estação em atenção ou crítica | `Movidos ≥ 3` | 60 | equipe | A fila cresce onde ninguém está olhando. | mover a equipe vale mais que comprar (TA-2) |
| 5 | vv_balcao | Balcão mais rápido | Leve o check-in à velocidade 2 | `Velocidade[0] ≥ 2` | 80 | 105 Cash | A Ana pede um leitor de cartão novo. | melhoria e o efeito dela |
| 6 | vv_rosa | Dona Rosa | Embarque um passageiro com história | `Historias ≥ 1` | 100 | nada | Dona Rosa vai ao casamento da neta e não pode perder este voo. | o coração na fila e o álbum (TA-3) |
| 7 | vv_loja | Loja aberta | Atenda 15 passageiros na loja | `Atendidos[2] ≥ 15` | 150 | alguém na loja | A loja só abre as portas com alguém no caixa. | renda fora da tarifa, e o custo de tirar gente da fila |
| 8 | vv_ceu | Céu fechado | Resolva 1 clima, adiantando ou atrasando | `Climas ≥ 1` | 150 | marco 1 | A torre avisa que o tempo vai virar. | aviso, previsão e as 3 saídas |
| 9 | vv_curitiba | Rumo a Curitiba | Faça 3 voos lotados para Curitiba | `Cheios[2] ≥ 3` | 250 | marco 1 e 250 Cash | Curitiba quer um voo maior, e ele só compensa se sair cheio. | abrir e programar rota |
| 10 | vv_terminal | Terminal novo | Chegue ao marco 2 | `Marco ≥ 2` | 400 | 600 embarques | Com 600 embarques, a Costa Azul manda o convite. | o marco abre oficina, despacho e o próximo aeroporto |

## Costa Azul (turístico)

Picos de demanda, alimentação, manutenção e incidentes, com rotas turísticas.

| # | id | Nome | Condição | Meta | Cash | Pede antes | Contexto | Ensina |
|---|---|---|---|---|---|---|---|---|
| 1 | ca_chegada | Chegada à praia | Chegue à Costa Azul | `Chegou[1] ≥ 1` | 300 | marco 2 e 1 200 Cash | Alta temporada: o avião já desce cheio de turistas. | viajar; voltar é grátis |
| 2 | ca_salvador | Turista em cima da hora | Faça 3 voos lotados para Salvador | `Cheios[4] ≥ 3` | 300 | nada | Em Salvador, todo mundo chega na segunda metade da janela. | a chegada concentrada da rota turística |
| 3 | ca_comida | Fome de praia | Atenda 40 passageiros na alimentação | `Atendidos[5] ≥ 40` | 400 | alguém na alimentação | Quem vai à praia já embarca com fome. | a parada opcional que mais paga |
| 4 | ca_bagagem | Mala e prancha | Atenda 40 passageiros na bagagem | `Atendidos[4] ≥ 40` | 400 | alguém na bagagem | Prancha não cabe no bagageiro de mão. | a taxa da mala e o tempo extra que ela custa |
| 5 | ca_checagem | Maresia | Faça 3 checagens rápidas | `Checagens ≥ 3` | 500 | oficina | O sal gasta o avião 1,5 vez mais rápido. | checar durante o turnaround |
| 6 | ca_incidente | Imprevisto | Resolva 1 incidente | `Incidentes ≥ 1` | 500 | nada | Máquina parada, grupo extra, conexão atrasada: aqui tudo acontece. | ler o aviso e escolher a saída |
| 7 | ca_vento | Rajada | Resolva 1 clima | `Climas ≥ 1` | 600 | nada | O vento fecha a pista, mas o pátio continua. | adiantar ou atrasar pela previsão |
| 8 | ca_hangar | Revisão geral | Faça 1 revisão no hangar | `Hangar ≥ 1` | 600 | 100 Cash | Um avião cansado precisa de uma onda inteira fora. | o hangar zera o desgaste e tira o avião da onda |
| 9 | ca_fortaleza | Fortaleza lotada | Faça 3 voos lotados para Fortaleza | `Cheios[6] ≥ 3` | 800 | 800 Cash | A rota mais cheia da costa, e a que mais gasta na loja. | pagar por uma rota difícil |
| 10 | ca_terminal | Terminal internacional | Chegue ao marco 3 | `Marco ≥ 3` | 1200 | 1 500 embarques | Com 1 500 embarques, Aurora Global abre as portas. | o marco fecha a costa e abre o hub |

## Aurora Global (hub internacional)

Conexões, ondas de passageiros, rotas longas e gestão avançada. Fecha a campanha.

| # | id | Nome | Condição | Meta | Cash | Pede antes | Contexto | Ensina |
|---|---|---|---|---|---|---|---|---|
| 1 | ag_chegada | Bem-vindo ao hub | Chegue a Aurora Global | `Chegou[2] ≥ 1` | 800 | marco 3 e 3 000 Cash | Aurora nunca dorme. | a mesma empresa em escala maior |
| 2 | ag_guarulhos | Conexão em Guarulhos | Faça 3 voos lotados para Guarulhos | `Cheios[8] ≥ 3` | 800 | nada | Quem desembarca aqui já vem com o check-in feito. | hub vive de conexão |
| 3 | ag_conexao | Grupo atrasado | Resolva 2 incidentes | `Incidentes ≥ 2` | 800 | nada | Em Aurora, sempre tem um grupo correndo pelo terminal. | priorizar o grupo de conexão, que paga 3× |
| 4 | ag_pontual | Relógio suíço | Ganhe 10 bônus de pontualidade | `Pontuais ≥ 10` | 800 | despacho com gente ajuda | Um hub se mede pela hora em que sai. | despacho e pontualidade |
| 5 | ag_neve | Neve no pátio | Resolva 1 clima | `Climas ≥ 1` | 1000 | nada | No inverno, ninguém decola sem degelo. | planejar o embarque pela previsão |
| 6 | ag_manaus | Rumo a Manaus | Faça 3 voos para Manaus | `Voos[10] ≥ 3` | 1200 | 1 500 Cash | A primeira rota longa: o avião some por uma onda. | rota longa e o quadro com a vaga vazia |
| 7 | ag_portao | Portão duplo | Leve o portão à capacidade 2 | `Capacidade[3] ≥ 2` | 1000 | 1 050 Cash | Avião grande pede dois portões. | capacidade onde o gargalo está |
| 8 | ag_lisboa | Travessia | Faça 3 voos para Lisboa | `Voos[11] ≥ 3` | 1500 | 2 500 Cash | O primeiro voo a cruzar o oceano. | pagar pela rota mais cara do jogo |
| 9 | ag_sequencia | Dia perfeito | Faça 8 voos lotados seguidos | `Seguidos ≥ 8` | 2000 | nada | Oito voos seguidos saindo cheios. | escolher a rota que a equipe aguenta |
| 10 | ag_hub | Hub internacional | Embarque 2 000 passageiros em Aurora | `Embarcados ≥ 2000` | 3000 | nada | O hub está pronto. Daqui para a frente, o aeroporto é seu. | fecha a campanha |

Total: 1 340 + 6 200 + 12 900 = 20 440 Cash.
