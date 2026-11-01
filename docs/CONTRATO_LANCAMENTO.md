# Contrato de lançamento — o §4.1 do GDD inteiro (2026-10-29)

Base: `GDD.md` §4.1 (conteúdo obrigatório), §17 (fases e portões), §21 (matriz P-01 a P-18) e §23.2 (decisões). A v0.1 é um subconjunto deste contrato e está em `CONTRATO_V01.md`.

**Aval em vigor:** D-01 aprovada pelo Vinicius em 2026-10-29, só para as **Fases A e B**. O lançamento completo (Fases C a E) se decide no portão da Fase B (§17.3). Até lá, as linhas das Fases C a E ficam planejadas, sem código.

**Regra:** o jogo só está pronto com toda linha **feita** (com evidência no PR que a fechou) ou **dependendo de alguém** (com o nome de quem e do quê). "Quase pronto" não existe (§21).

**Guardrails que não mudam:** 1 moeda (Cash) na v0.1, e Gems só com D-05 e D-07; offline com teto de 2 h no código; nada aleatório pago (ECA Digital, Lei 15.211/2025); nenhum interstitial; anúncio não personalizado, sem `AD_ID`; perder voo custa só a receita dele; criativos gravados do jogo real; nenhum dado pessoal no diário.

## 1. Matriz §4.1 × feito (estado em 2026-10-31: Fase B feita, menos o K2; Fase C no núcleo: tripulação, pátio, manutenção, 8 estações, 6 perfis, 3 aeroportos, 12 rotas, 4 incidentes, clima e falhas de avião)

Estados: **feito** (com evidência) · **parcial** · **falta** · **depende** (de decisão, conta ou recurso externo).

| §4.1 | Item | v0.1 | Estado | Onde fecha | Matriz §21 |
|---|---|---|---|---|---|
| — | Projeto Unity 6000.3.27f1 que abre e compila; portão `verify.sh full` | sim | **feito**: `verify.sh full` verde (núcleo, EditMode, viewcheck, build Windows e `-autoplay 10`) nos PRs #5 e da View | Fase A | P-01 |
| 1 | 3 aeroportos com identidade e desafios diferentes | Vale Verde | **parcial**: núcleo feito (`CONTRATO_FASE_C3.md`): Vale Verde, Costa Azul (marco 2) e Aurora Global (marco 3), com rotas, ritmo de chegada, desgaste e incidentes próprios; a empresa é uma só (Cash, equipe e níveis viajam); portão do §5.13 **passou: 1,151** (`BALANCE.md` §7); nomes de trabalho dependem de D-13; falta a tela, e os objetivos por aeroporto ficam na linha 11 | B (Vale Verde), C (núcleo); View na próxima frente | P-05 |
| 2 | 12 rotas desbloqueáveis (distância, frequência, receita, risco) | — | **parcial**: núcleo feito: 12 linhas na tabela `Tuning.Rotas` (id estável), 4 + 4 + 4 por aeroporto, abertas por marco e preço fixo, programadas por vaga; portão do §5.13 **passou: 1,221, 1,124 e 1,273**; TA-6 **passou** (melhor ÷ segunda ≤ 1,161); nomes de trabalho dependem de D-13; falta a tela | C (núcleo); View na próxima frente | P-06 |
| 3 | 8 variantes visuais de aeronaves | — | depende | C; arte depende de D-14 (Lote 2) | P-06, P-12 |
| 4 | 8 estações (check-in, segurança, bagagem, portão, loja, alimentação, manutenção, despacho) | 4 | **parcial**: núcleo feito, com regra (`CONTRATO_FASE_C2.md`): bagagem e alimentação abrem no marco 1, oficina e despacho no marco 2; testes e invariantes; portão do §5.13 **passou: 1,366** com as 4 juntas (sozinhas 1,02–1,06, `BALANCE.md` D-C2c); **tela feita**: as 8 salas no mapa, fechadas até o marco, com malha por nível nas estações, no hangar e na torre; falta o K2 com gente | C | P-02 |
| 5 | 6 perfis de funcionários com especialização | 3 generalistas | **parcial**: núcleo feito (6 perfis fixos, um por pessoa, ordem e preço fixos, os 3 novos por marco); o bônus do especialista está **desligado (só visual)**: portão 1,00 e, ligado, derruba o TA-2 (`BALANCE.md` D-C2a); **tela feita**: tela da Equipe com ícone, marco e contratação, e os 6 com o uniforme tingido ao chegar | C; bônus volta com mais voos | P-04 |
| 6 | Equipe limitada e reposicionável entre estações (TA-2) | sim | **feito** na v0.1, menos o K2: bot com ganho de mover 1,408 (`BALANCE.md` §6); arrastar e "mover para" no mundo com bonecos que andam pelo corredor e vestem o uniforme do setor; K2 com gente **pendente (externo)** | B; portão: bot ≥ +10% e K2 com gente | P-04 |
| 6a | Tripulação contratada e escalada: pilotos e comissários (§5.11) | — | **parcial**: núcleo feito (contratação com teto por marco, nomes fixos, escala automática e troca manual, trechos e descanso, testes e invariantes; `CONTRATO_FASE_C.md`); a regra está **desligada (só visual)** porque o portão do §5.13 mediu 1,02 com ela sozinha (`BALANCE.md` D-C2); **tela feita**: quadro da tripulação, contratação, troca da escala no voo e tripulação andando da sala ao avião | C; regra volta com mais voos e a narrow 2+2 | P-04 |
| 6b | Pátio visível em cada voo e manutenção com desgaste, checagem e hangar (§5.12) | avião e veículos só visuais | **parcial**: núcleo feito, com regra: turnaround (4 serviços, equipe e veículos de pátio, upgrades), "decola quando embarcou e o pátio terminou", desgaste, aviso de 80%, inspeção obrigatória, checagem rápida e hangar; portão do §5.13 **passou: 1,151** (`BALANCE.md` §5); **tela feita**: aviões e veículos seguem o núcleo (`Mundo.Patio.cs`, `Mundo.Veiculos.cs`), com garagem, faixa por veículo, ícone e anel do serviço, mecânico, hangar, aviso de 80% e decisão entre checagem e hangar | C | P-02 |
| 7 | Quadro de partidas com horários e estados (TA-1) | 2 voos por onda | **feito** na v0.1: quadro com horário, estado, a bordo e barra; bot 99,7% (Bom) e 75,4% (Lento) | B; portão: bot ≥ 85% / ≥ 60% | P-03 |
| 8 | Fluxo de passageiros, filas, embarque, partida e perda de receita por voo perdido | sim | **feito** na v0.1: núcleo testado e bonecos andando pela fila, embarque pela ponte e saída de quem perdeu o voo | B | P-02 |
| 9 | 4 incidentes com aviso prévio | — | **parcial**: núcleo feito: frente fria, falha de equipamento, pico de demanda e atraso de conexão, da semente, um por vez, com 30 s de aviso e saída, em testes e invariantes; portão do §5.13 **passou: 1,146 e 1,170** (como um sistema; ressalva na D-C3c do `BALANCE.md`); falta a tela | C (núcleo); View na próxima frente | P-06 |
| 9a | Dia a dia: dia e noite, picos, estações do ano, clima com previsão e falhas de avião (§5.14) | — | **parcial**: núcleo feito (`CONTRATO_FASE_C4.md`): relógio do jogo, 5 climas da semente com previsão e aviso, 3 falhas acima do aviso de 80%, pouso sempre seguro; portão do §5.13 **passou**: clima 1,161 e 1,147, falhas 1,134, 1,131 e 1,124; demanda por estação só visual (D-C4a); falta a tela | C (núcleo); View na próxima frente | P-06 |
| 10 | 20 micro-histórias de passageiros (TA-3) | 1 | **parcial**: 1 história feita (coração no boneco e no quadro, álbum no save); as 20 na C | B (1), C (20) | P-06 |
| 11 | Campanha principal: 30 objetivos, 10 por aeroporto | — | falta | C; `docs/CAMPANHA.md` | P-05 |
| 12 | Missões diárias e semanais | — | falta | C (`Metas.cs` do Forge) | P-05 |
| 13 | 20 conquistas permanentes | — | falta | C | P-05 |
| 14 | Passe de temporada: trilhas gratuita e premium, 30 níveis | — | falta (premium **depende** de D-05) | C (gratuita), D (premium) | P-07 |
| 15 | Loja de compras digitais com catálogo fixo e preços da loja | — | depende | D; D-05, D-06 e Play Console (D-U-N-S, previsão 2026-11-25) | P-08 |
| 16 | Anúncios recompensados opcionais | simulado | depende: regras dos 2 rewarded no núcleo (`Anuncios.cs`, testes de id de transação e teto diário); LevelPlay real depende de D-03 | B (regras e simulador no núcleo), D (LevelPlay real) | P-09 |
| 17 | Sem interstitial forçado | sim | **feito** no núcleo: não existe código de interstitial; `Anuncios` só tem os 2 rewarded | B | P-09 |
| 18 | Save local, migração de schema, restauração de compras, nuvem se configurada | save + envelope | **parcial**: save em disco com escrita atômica, autosave de 5 s e na pausa, retomada no meio da onda, schema futuro recusado; migração feita nos schemas 2 (Fase C), 3 (Fase C2) e 4 (Fase C3), testadas com saves reais gravados pelo código anterior; falta restaurar compras (D) | B (save), C (migração), D (restaurar compras), C/D (nuvem, se houver) | P-10 |
| 19 | Progressão offline limitada a 2 h | sim | **feito**: `ApplyOffline` com as travas (`ExploitTests`) e a tela do cofre com o tempo e o cálculo, ao abrir e ao voltar da pausa | B | P-10 |
| 20 | Tutorial contextual (12 passos da Origem) | passos 1–9 | **parcial**: passos 1–9 feitos (fotos de cada passo e rodada `-tutorial -bot`); 10–12 na C | B (1–9), C (10–12) | P-02 |
| 21 | Configurações, áudio, acessibilidade básica, política de privacidade, suporte e exclusão de dados | — | depende | C (telas e áudio); política e suporte dependem de D-09, D-10 e D-12 | P-11 |
| 22 | Telemetria de eventos, crash reporting e painel mínimo de saúde | diário CSV | **parcial**: diário CSV com os eventos do §15.1 e `diario_report.py` (K2 e embarque médio); produção **depende** de D-11 | B (diário), D (SDK de D-11); crash pelo Android vitals | P-16 |
| 23 | Arte final, UI, ícone, screenshots e materiais de loja sem placeholder | greybox | depende | C e E; Tripo depende de D-14; logo e ícone dependem de D-03 | P-12, P-15 |
| — | Build Android assinado (AAB) e iOS | — | depende | E; AAB depende de D-03 (a chave de upload leva o nome) e da Play Console; iOS depende de macOS e conta Apple (D-04) | P-14 |
| — | Segurança e anti-fraude (§8), privacidade e LGPD (§9) | — | falta | D | P-17, P-11 |
| — | Regressão final e GO/NO-GO (§25) | — | falta | E | P-18 |

## 2. Depende do Vinicius (preparado do nosso lado, sem travar a Fase B)

| Decisão | Trava |
|---|---|
| D-03 nome e appId | o `Setup.cs` usa `br.com.vstack.tinyairport.dev` só para build de desenvolvimento; trava app no LevelPlay, chave de upload, logo, ícone e loja |
| D-05/D-06/D-07 compras, backend e Gems | linha 15 e o premium da linha 14 |
| D-08 idiomas | textos já entram numa tabela (padrão do `Textos.cs` do DC); a tradução espera a decisão |
| D-13 nomes de companhias e aeroportos | arte de marca e textos das rotas |
| D-14 lotes do Tripo | toda a arte além do greybox |
| D-02 nova data-alvo | decidida no portão da Fase B, com a velocidade real |

## 3. Como esta matriz anda

Cada PR que fecha ou avança uma linha atualiza o estado aqui e no §21 do GDD, com a evidência (saída do `verify.sh`, teste, foto ou relatório do bot). Uma linha nova no §4.1 do GDD ganha uma linha aqui no mesmo PR.
