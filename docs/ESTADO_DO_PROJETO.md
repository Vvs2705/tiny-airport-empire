# Estado do projeto: Little Terminal (repositório Tiny Airport Empire)

**Data deste retrato: 2026-11-03.** A `main` está em `49e1597` (merge do PR #48, de 2026-11-03), com 48 PRs mesclados e 1 aberto (o #49, do passe de temporada). Este documento é para quem **nunca viu o projeto**: diz o que o jogo é, como rodar, o que está pronto, o que está sendo feito agora, o que falta e o que está errado. Os detalhes de cada regra ficam nos outros documentos, citados ao longo do texto.

> **Como ler as marcações.** "Na `main`" = mesclado e com o portão verde. "Em andamento" = trabalho que existe só num branch ou numa sessão aberta; **não conta como pronto**. Onde este documento e o código discordam, vale o código, e a divergência está registrada no `GDD.md` §26 (v1.5).

## 1. O que é o jogo

**Little Terminal** é um jogo de celular (Android, retrato) de gestão de aeroporto, no estilo idle/tycoon. O repositório se chama Tiny Airport Empire por motivo histórico; o nome do produto, decidido em 2026-11-01, é Little Terminal, com appId `br.com.vstack.littleterminal` (`docs/NOME.md`).

- Você começa numa pista rural (Vale Verde) e chega a um hub internacional (Aurora Global), passando por Costa Azul: 3 aeroportos, 12 rotas.
- O avião pousa, os passageiros passam por check-in, segurança, bagagem, lojas e portão, embarcam, e o avião decola com a receita. Um **quadro de partidas com horário** cria a hora do rush; perder o voo custa só a receita dele.
- A **equipe é limitada e você a move** entre as 8 estações (check-in, segurança, bagagem, portão, loja, alimentação, oficina, despacho). Pilotos e comissários têm escala e descanso; o avião tem desgaste, checagem no pátio e hangar.
- O dia tem hora, clima com previsão (5 tipos), estações do ano e falhas de avião que sempre terminam em pouso seguro. Sem acidente, morte, fogo ou queda.
- Monetização: anúncio **recompensado e opcional** (nenhum interstitial) e compras com **Gems** validadas em servidor. Nada aleatório pago (ECA Digital, Lei 15.211/2025). Offline limitado a 2 h, como trava no código.
- Quem é o dono: V-STACK (Vinicius Souza). Repositório público, **todos os direitos reservados** (`README.md`).
- **Direção (2026-11-03).** O Vinicius trouxe um GDD de direção (`docs/GDD_DIRECAO_v0.1.md`), um simulador de construção, gestão e operação bem maior que este tycoon de 8 estações fixas, e aprovou misturar os dois. O lançamento ganha **9 frentes de gestão**, nenhuma existente no código ainda: despesas e salários (A), velocidades 1x, 2x e 3x (B), ficha ao tocar (C), contratação com 3 candidatos e demissão (D), transferência pelo vestiário (E), limpeza e banheiro (F), satisfação com motivo (G), esteira de restituição (H) e painel de operações com o relatório do dia (I) (`GDD.md` §4.1). O resto do documento dele (construção livre, contratos com companhias, turnos de 24 h, polícia, K9, imigração e o mais) é **roteiro pós-lançamento** (`GDD.md` §4.4), não escopo. Ficam como estavam: retrato, pessoas em sprite (D-18), ondas com dia e noite e compras opção B. Valem desde já seis regras de direção, entre elas o critério de pronto em 10 itens e o "nenhuma despesa leva a derrota inevitável" (`GDD.md` §0.5).

Onde ler mais: `docs/GDD.md` (o produto inteiro: regras, segurança, publicação, decisões), `docs/GDD_DIRECAO_v0.1.md` (o GDD de direção do dono: só direção, não é o escopo de lançamento), `docs/CONTRATO_LANCAMENTO.md` (cada item do lançamento × o que está feito, com evidência), `docs/CONTRATO_V01.md` (a v0.1, primeiro corte jogável) e os `docs/CONTRATO_FASE_*.md` (um por fase de conteúdo).

## 2. Como compilar e testar

**Precisa de:** Windows 10 com Git Bash, Unity **6000.3.27f1** (instalação padrão do Unity Hub; a variável `UNITY_EXE` troca o caminho), .NET SDK 10 (os testes do núcleo são `net10.0`), Python 3 e, só para arte, Blender e Pillow/numpy. O PC de desenvolvimento tem 7,7 GB de RAM: **um processo pesado por vez** (Unity, emulador, Blender ou Docker). Em worktree com caminho longo, o build estoura o limite de caminho do Windows e roda por um drive `subst` (`R:`).

O portão é um comando só, em `client/tools/verify.sh`:

```bash
client/tools/verify.sh quick     # sem abrir o Unity: checa_repo, núcleo (dotnet test) e compilação da View
client/tools/verify.sh full      # Unity em batchmode: EditMode, build Windows e o .exe jogando 10 ondas sozinho
client/tools/verify.sh android   # núcleo, APK de desenvolvimento e checa_apk (targetSdk, ABI, sem AD_ID, 16 KB)
```

- `quick` é o do dia a dia (cerca de 3 minutos, medido em 2026-11-02). `full` roda uma vez antes de todo PR que mexe no jogo.
- O `checa_repo.py` confere `.meta` e GUID, núcleo sem Unity, guardrails (nenhum interstitial, nada aleatório pago, sem `AD_ID`), arquivos proibidos no git, segredos e se todo arquivo novo está em `docs/MAPA_CODIGO.md`.
- O CI (`.github/workflows/`) repete o `checa_repo` e o `dotnet test` em cada PR (`nucleo.yml`; a `main` exige esse check verde) e roda os portões do bot de balanço (`balanco.yml`, cerca de 15 min).
- Os logs de cada rodada ficam em `client/Builds/verify/`. Fotos 540×960 de cada tela são conferidas à mão.

**Balanço do jogo.** Não há planilha: um bot headless (`Scripts/Core/Bot.cs`) joga 200 sementes × 10 ou 20 ondas e mede o que o GDD chama de portões (TA-1, TA-2 e o portão de design do §5.13). Toda mudança de número vem com antes e depois em `docs/BALANCE.md`.

## 3. Por que as malhas do Tripo não estão no git

A arte 3D (prédios, aviões, veículos, móveis) foi gerada no **Tripo3D**, na conta paga da V-STACK. A licença do Tripo não deixa redistribuir o modelo, e este repositório é público. Por isso:

- **Fora do git:** a arte crua (`arte/tripo/`, zips, FBX, `.blend`) e a malha decimada que o jogo carrega (`client/Assets/_TA/ArtLocal/`, ignorada no `.gitignore`). O build de loja sai do PC do dono, que tem as duas pastas e um backup privado.
- **Dentro do git:** o código que gera a malha a partir da arte crua (`client/tools/malhas_ta.py`), os sprites das pessoas (são *renders* do rig, não o modelo), áudio sintetizado, fontes com licença OFL e a arte de interface (logo, ícone, splash).
- **O que acontece sem as malhas:** o jogo compila e roda, mas mostra as primitivas (caixas e cilindros) da v0.1, sem aviso nenhum. Foto e build feitos assim não valem.
- **Para ter acesso à arte:** peça ao dono. Cada peça tem prompt, data, créditos gastos e SHA-256 em `docs/PROVENIENCIA.md`; a lista de uso está em `docs/INTEGRACAO_ARTE.md` e a de peças em `docs/ASSETS.md`.
- **Termos do Tripo.** `docs/PROVENIENCIA.md` §4 resume o que foi lido logado em 2026-10-30. Há um ponto de risco em aberto (seção 7 deste documento).

## 4. Arquitetura: `Scripts/Core` × `Scripts/View`

O código do jogo está em `client/Assets/_TA/`. O mapa de todos os arquivos, um a um, é `docs/MAPA_CODIGO.md`.

| Parte | O que é | Regra |
|---|---|---|
| `Scripts/Core/` (`TA.Core`) | **O núcleo**: C# puro, sem `UnityEngine`. Simulação em passo fixo de 1/30 s, filas, equipe, pátio, tripulação, rotas, incidentes, clima, campanha, missões, Gems, save e o bot. | Roda e é testado fora do Unity (`dotnet test`). Determinístico: tudo vem da semente e do relógio do jogo, nunca do aparelho. É a **única** parte que muda regra. |
| `Scripts/View/` (`TA`) | **A tela**: MonoBehaviours, câmera, HUD em uGUI, mundo 3D isométrico (`Mundo.*.cs`), pessoas em sprite, tutorial, áudio, entrada. | Só apresenta o que o núcleo decide. Frente de tela não muda número do núcleo (o bot repete a tabela da `main` número a número). |
| `Editor/` | Setup do projeto (appId, nome, retrato, ARM64) e builds por linha de comando. | Idempotente. |
| `Tests/EditMode/` | 247 testes NUnit do núcleo, que rodam no `dotnet test` sem abrir o Unity. `Tests/EditModeView/` só roda no Unity. | Invariantes viram teste negativo nomeado (`ExploitTests`). |
| `client/tools/` | `verify.sh`, `checa_repo.py`, bot de balanço, medidores de movimento (`diag_*.py`), scripts de arte (Blender headless). | Stdlib sempre que possível. |

Duas regras de arquitetura que quebram fácil: **enum salvo só cresce no fim** (trocar a ordem corrompe save, e `EnumsTests` falha) e **o save é versionado** (hoje no schema 8; cada schema novo vem com migração testada em save real do schema anterior).

## 5. O que está feito (na `main`)

Link de cada PR: `https://github.com/Vvs2705/tiny-airport-empire/pull/<n>`.

| Área | O que existe | PRs |
|---|---|---|
| **Documentos e decisões** | GDD mestre (v1.1 a v1.4), GDD de origem, comparativo com concorrentes (dez jogos jogados no celular), decisões D-01 a D-24 respondidas em 2026-11-01, estado do projeto de 2026-11-02 | [#1](https://github.com/Vvs2705/tiny-airport-empire/pull/1), [#2](https://github.com/Vvs2705/tiny-airport-empire/pull/2), [#3](https://github.com/Vvs2705/tiny-airport-empire/pull/3), [#6](https://github.com/Vvs2705/tiny-airport-empire/pull/6), [#25](https://github.com/Vvs2705/tiny-airport-empire/pull/25), [#38](https://github.com/Vvs2705/tiny-airport-empire/pull/38), [#45](https://github.com/Vvs2705/tiny-airport-empire/pull/45) |
| **Esqueleto e portão** | Projeto Unity, `verify.sh`, `checa_repo`, `checa_apk`, CI do núcleo e do balanço, auditoria de ferramentas, teto de 90 min por chamada do Unity no `verify.sh` | [#4](https://github.com/Vvs2705/tiny-airport-empire/pull/4), [#16](https://github.com/Vvs2705/tiny-airport-empire/pull/16), [#47](https://github.com/Vvs2705/tiny-airport-empire/pull/47) |
| **Núcleo da v0.1** | Onda de 2 voos, chegadas, filas, atendimento, equipe móvel, melhorias, cofre offline (2 h), 2 anúncios recompensados simulados, história, save, bot. Com o conteúdo das Fases C a C3 ligado, o bot mede TA-1: Bom 96,1% e Lento 73,0% de embarque (portão: 85% e 60%); TA-2: 1,426× a receita de quem não move a equipe (portão: 1,10×) | [#5](https://github.com/Vvs2705/tiny-airport-empire/pull/5) |
| **Núcleo, Fase C** | Tripulação, pátio e manutenção (portão do §5.13: 1,151) | [#12](https://github.com/Vvs2705/tiny-airport-empire/pull/12) |
| **Núcleo, Fase C2** | 8 estações e 6 perfis, por marco | [#18](https://github.com/Vvs2705/tiny-airport-empire/pull/18) |
| **Núcleo, Fase C3** | 3 aeroportos, 12 rotas, 4 incidentes com aviso de 30 s | [#23](https://github.com/Vvs2705/tiny-airport-empire/pull/23) |
| **Núcleo, Fase C4** | Hora do jogo, estações do ano, 5 climas com previsão, 3 falhas de avião | [#28](https://github.com/Vvs2705/tiny-airport-empire/pull/28) |
| **Núcleo, Fase C5** | Campanha: 30 objetivos, 10 por aeroporto; fecham em 200 de 200 sementes | [#30](https://github.com/Vvs2705/tiny-airport-empire/pull/30) |
| **Núcleo, Fase C6** | Missões (3 diárias e 5 semanais, de 63 linhas) e 20 conquistas | [#40](https://github.com/Vvs2705/tiny-airport-empire/pull/40) |
| **Núcleo, Fase D1** | Gems no save (schema 8), catálogo de 7 produtos sem preço, 3 sumidouros, crédito idempotente por id de transação | [#43](https://github.com/Vvs2705/tiny-airport-empire/pull/43) |
| **Núcleo, registro de ocorrências** | Os 4 incidentes, o clima, as falhas de avião e o voo perdido ou atrasado, com causa, resposta (do jogador ou automática), resultado e a chave da explicação; 3 dias do jogo, até 24 por dia, no save (schema 12). Só observa: desligado, dá o mesmo jogo, número a número (`HistoricoTests`). Base das frentes G e I, ainda sem tela | frente `ta-historico` |
| **Mundo 3D e pessoas** | v0.1 jogável em greybox; troca pela arte do Tripo (malha nos corpos rígidos, sprite nas pessoas); tela das Fases C e C2; dia a dia (garagem, postos, segurança) | [#7](https://github.com/Vvs2705/tiny-airport-empire/pull/7), [#21](https://github.com/Vvs2705/tiny-airport-empire/pull/21), [#24](https://github.com/Vvs2705/tiny-airport-empire/pull/24), [#26](https://github.com/Vvs2705/tiny-airport-empire/pull/26) |
| **Cenário vivo** | Embarque pela ponte (a pessoa some no túnel), sala de espera do portão com cadeiras e fila, escala de adulto de 0,5 m com corredores, física do pátio (469 cruzamentos de peças para 0 em 5 cenários), mala de verdade do balcão ao avião, postos com gente e gestos de atendimento, clima e incidentes no cenário | [#29](https://github.com/Vvs2705/tiny-airport-empire/pull/29), [#31](https://github.com/Vvs2705/tiny-airport-empire/pull/31), [#32](https://github.com/Vvs2705/tiny-airport-empire/pull/32), [#33](https://github.com/Vvs2705/tiny-airport-empire/pull/33), [#37](https://github.com/Vvs2705/tiny-airport-empire/pull/37), [#42](https://github.com/Vvs2705/tiny-airport-empire/pull/42), [#44](https://github.com/Vvs2705/tiny-airport-empire/pull/44) |
| **HUD, telas e textos** | Fontes e linguagem visual (Lilita One e Nunito, licença OFL), HUD fixo com quadro de partidas de aeroporto, avisos de perigo, telas modais (Melhorias, Mover, Cofre, Voo, Tripulação, Pátio...), revisão dos textos em pt-BR, tutorial de 12 passos, tela de metas (campanha, missões e conquistas, com a faixa da meta da vez no HUD) e tela de configurações, com a política de privacidade em pt-BR e inglês, o suporte e "excluir meus dados"; velocidade do relógio (pausa operacional, 1x, 2x e 3x; frente `ta-velocidade`) | [#27](https://github.com/Vvs2705/tiny-airport-empire/pull/27), [#34](https://github.com/Vvs2705/tiny-airport-empire/pull/34), [#35](https://github.com/Vvs2705/tiny-airport-empire/pull/35), [#36](https://github.com/Vvs2705/tiny-airport-empire/pull/36), [#41](https://github.com/Vvs2705/tiny-airport-empire/pull/41), [#46](https://github.com/Vvs2705/tiny-airport-empire/pull/46), [#48](https://github.com/Vvs2705/tiny-airport-empire/pull/48) |
| **Arte do Tripo** | Lotes 1 a 11 gerados e exportados, com proveniência: tripulação, passageiros, pátio, terminais em 3 níveis, aeronaves, 3 aeroportos, incidentes, cabine de raio-x, garagem | [#8](https://github.com/Vvs2705/tiny-airport-empire/pull/8), [#9](https://github.com/Vvs2705/tiny-airport-empire/pull/9), [#10](https://github.com/Vvs2705/tiny-airport-empire/pull/10), [#11](https://github.com/Vvs2705/tiny-airport-empire/pull/11), [#13](https://github.com/Vvs2705/tiny-airport-empire/pull/13), [#14](https://github.com/Vvs2705/tiny-airport-empire/pull/14), [#15](https://github.com/Vvs2705/tiny-airport-empire/pull/15), [#17](https://github.com/Vvs2705/tiny-airport-empire/pull/17), [#20](https://github.com/Vvs2705/tiny-airport-empire/pull/20), [#22](https://github.com/Vvs2705/tiny-airport-empire/pull/22) |
| **Áudio** | 8 sons em 13 arquivos e 3 músicas, sintetizados por script (sem licença); a classe `Audio.cs` existe, **sem nenhuma chamada no jogo** | [#19](https://github.com/Vvs2705/tiny-airport-empire/pull/19) |
| **Nome e marca** | "Little Terminal", appId, chave de upload (fora do git), 5 companhias renomeadas, aeroportos mantidos | [#39](https://github.com/Vvs2705/tiny-airport-empire/pull/39) |

Medidas que existem: K3 no **PC** (p10 de 58,7 FPS, média de 59,5, trava de 60, com 56 pessoas, 2 aviões e 6 veículos em 540×960; `docs/CONTRATO_V01.md`). O resto das medidas está no `docs/BALANCE.md`.

## 6. Em andamento (não está na `main`)

Estado conferido em 2026-11-03. Cada frente termina num PR próprio, pequeno; o coordenador mescla. Das cinco frentes abertas em 2026-11-02, **duas já entraram**: `ta-metas-tela` (PR #46) e `ta-config` (PR #48). As 9 frentes de gestão da v1.5 (`GDD.md` §4.1) **ainda não começaram**: estão no §8.

| Frente | Branch | O que é | O que falta |
|---|---|---|---|
| `ta-planta-fim` | `feat/planta-salas` (enviado) | PR 2 da organização das 8 salas: ladrilho, tapete, paredes de fundo, letreiro, mobília e três postos refeitos na régua de 0,5 m (balcão da loja, bancada da oficina, mesa do despacho) | Rodada final do `verify.sh full`, fotos de antes e depois, abrir o PR. Entra **antes** da `ta-caminhos`. A arte nova que a frente pediu ao Tripo (5 peças) não foi gerada; o PR usa as peças que já existem |
| `ta-caminhos` | `claude/practical-sanderson-e254c9` (enviado como `wip`) | Pessoa não atravessar parede, móvel nem equipamento. Medido em 5 cenários: **1.862 encontros caíram para 237** em 2026-11-01 (cadeira já em zero) | Zerar o que sobrou, conferir encontrão e viradas, rodar o `full` e abrir o PR. Há correções novas sem medir |
| `ta-passe` | `claude/frosty-jepsen-605169` (PR #49, aberto) | Passe de temporada, **trilha gratuita de 30 níveis no núcleo** (`Sim.Passe.cs`, `docs/CONTRATO_FASE_C7.md`, `docs/PASSE.md`; save com mais uma versão). Só núcleo, sem tela | O coordenador mescla: o PR estava com conflito com a `main` (estado `DIRTY` em 2026-11-03). A trilha premium e a tela ficam para depois |

`ta-planta-fim` e `ta-caminhos` seguem sem PR aberto em 2026-11-03; o estado das duas é o de 2026-11-02, acima. Da `ta-config` sobra a **revisão jurídica** do texto da política, que é do dono: o GitHub Pages já está ligado (status `built` em 2026-11-03).

Parada: a geração da peça `painel_ferramentas_parede` no Tripo (o navegador com a conta estava ambíguo; 0 crédito gasto).

## 7. A corrigir (problemas conhecidos)

Do mais importante para o menos.

1. **Pessoa atravessando peça.** Em 2026-11-01 eram 237 encontros de pessoa com parede ou móvel nos 5 cenários de medida (153 de parede e 84 de móvel); a meta é **zero**. Medidores: `client/tools/diag_caixas.py` e `client/tools/diag_pessoas.py`. Frente: `ta-caminhos`.
2. **Desempenho não medido no aparelho.** O K3 (p10 de FPS ≥ 30 num Android de entrada, com 60 pessoas) só foi medido no PC. Por decisão do dono, o celular de teste só entra no último passo de tudo; antes disso, o APK vai para amigos dele (D-24).
3. **K2 sem gente.** O portão da v0.1 pede teste com pelo menos 20 pessoas reais; hoje só o bot jogou. O relatório sai de `client/tools/diario_report.py`.
4. **`painel_ferramentas_parede` sem gerar** (peça de parede da oficina). Parada por problema de navegador, sem crédito gasto.
5. **Aviso do firewall do Windows no build de desenvolvimento.** O `.exe` que o `verify.sh full` e as fotos abrem pede liberação do firewall; só o dono aprova esse tipo de janela (UAC e firewall ficam com ele), e há um comando preparado para ele. Enquanto não for liberado, o aviso aparece em cada máquina nova.
6. **Busca de marca do USPTO inconclusiva.** A busca direta no USPTO tratou "little terminal" como "terminal" e não valeu como zero; a cobertura vem do TMview e da WIPO, que deram zero (`docs/NOME.md`, linha 14 e "Limites"). Nenhuma busca vale como parecer jurídico.
7. **Risco de automação nos termos do Tripo.** O texto de 2025-07-11 (§3.2) proíbe extrair conteúdo do serviço com "automated tools", e os Lotes 3 a 10 foram gerados com filas de script de página (`docs/PROVENIENCIA.md` §4). Desde o Lote 11, peça nova é manual, pela interface. Precisa de leitura jurídica antes de publicar. Além disso, **o plano pago do Tripo vale até 2026-11-29**; sem plano, o export fica bloqueado (já custou 6.318 créditos em 2026-10-29).
8. **Regra desligada por medida** (não é bug; é decisão do portão do §5.13): a tripulação (`BALANCE.md` D-C2), o bônus do especialista (D-C2a) e a demanda por estação do ano (D-C4a) estão só visuais. Voltam quando houver mais voos para equilibrar.
9. **Lacunas visíveis:** a estação do ano não aparece na tela; só 2 das 12 aeronaves da lista de integração (`aviao_regional` e `aviao_narrow`; `docs/INTEGRACAO_ARTE.md` §3) estão no pátio; só 1 das 20 histórias de passageiros existe; o texto está só em pt-BR; o áudio não toca; o ícone e o splash existem em `Art/Marca/` mas o build ainda não os usa; o `Setup.cs` gera só APK de desenvolvimento.
10. **Documentação:** o GDD cita `docs/TEMPORADA_S1.md` e `client/tools/econsim`, que não existem (a tabela da temporada vai para o contrato da `ta-passe`, e o balanço sai do bot); a correção está no GDD v1.4. O `docs/NOME.md` cita o caminho da pasta da chave de upload, o que não deveria estar num repositório público: trocar por texto genérico (a chave em si nunca esteve no git).

## 8. A fazer, em ordem

Ordem de trabalho de 2026-11-03, depois da mistura com o GDD de direção (`GDD.md` §4.1 e §4.4) e das frentes abertas do §6. Cada item é uma frente com PR próprio, e toda frente de gestão só fecha com o critério de pronto em 10 itens (`GDD.md` §0.5).

**Gestão do aeroporto (frentes A a I do `GDD.md` §4.1; só a B e a C existem, parciais):**

1. **A, despesas e salários,** com fechamento do dia e sem derrota inevitável.
2. **B, velocidades** 1x, 2x e 3x: no PR da `ta-velocidade` (parcial; falta a validação em build na rodada do Unity da leva).
3. **C, ficha ao tocar** em pessoa, equipamento e sala: parcial (`ta-ficha`: pessoa e sala); faltam o equipamento e as necessidades.
4. **H, esteira de restituição** (traz junto o desembarque mínimo de quem chega).
5. **D, contratação** com 3 candidatos e demissão.
6. **E, transferência pelo vestiário** (o TA-2 tem de passar de novo com o tempo de troca).
7. **F, limpeza e banheiro.**
8. **G, satisfação com motivo.** O porquê de cada voo perdido ou atrasado já está no registro de ocorrências do núcleo.
9. **I, painel de operações** com o relatório do dia. A última ocorrência e as do dia saem do mesmo registro.

**Depois, a loja e o servidor, e então a lista de antes:**

10. **`ta-loja`:** Unity IAP em modo de teste, tela da loja e Gems no HUD (usa o núcleo da D1).
11. **`ta-servidor`:** validação de compra no Cloud Run (projeto próprio, sem chave JSON). Se não passar nos testes do GDD §16.6, o jogo lança só com anúncio recompensado (opção A de D-05).
12. **`ta-rotas-tela`:** tela de aeroportos e de rotas (`Hud.Rotas.cs`): PR aberto em 03/11 com o `quick` verde; falta a rodada da leva (EditMode `RotasTests` e fotos `-tela rotas`, `rotas1` e `rotas2`).
13. **`ta-avioes`:** 8 variantes visuais de aeronave por rota ou aeroporto (só visual; as peças do Tripo já existem).
14. **`ta-historias`:** as 20 micro-histórias de passageiros (hoje 1).
15. **`ta-audio`:** ligar o áudio em `Game.cs`, `Mundo.cs` e na pausa; o dono ouve antes de valer.
16. **`ta-passe-tela`:** tela do passe de temporada, depois do núcleo (PR #49).
17. **`ta-incidente5`:** o 5º incidente (volta ao portão por aviso técnico); só entra se passar no portão do §5.13.
18. **`ta-estetica`:** auditoria visual contra os concorrentes; refazer o que estiver abaixo.
19. **`ta-ingles`:** textos em inglês (D-08: pt-BR e en no lançamento; es e id depois, com revisão humana).
20. **`ta-telemetria`:** Unity Analytics sem dado pessoal (D-11).
21. **`ta-lgpd`:** consentimento, Data safety, exclusão de dados ponta a ponta.
22. **`ta-playtest`:** K2 com 20 pessoas; K3 nos aparelhos dos amigos do dono.
23. **`ta-loja-assets`:** ícone aplicado no build, screenshots, descrição, criativos gravados do jogo real.
24. **Fim:** atualizações de ferramentas com a fila vazia, AAB assinado, regressão final, GO/NO-GO (GDD §25) e, por último, o celular de teste.

Fora do lançamento por decisão do dono (D-04): iOS (exige macOS e conta Apple) e o 5º incidente antes do teste.

**Roteiro pós-lançamento** (`GDD.md` §4.4, aprovado como direção em 2026-11-03; nada daqui conta no contrato): construção livre, início com hangar, contratos com companhias, totens, turnos de 24 h e fadiga, supervisores, qualificações e treinamento, polícia, K9, bombeiros e saúde (só prontidão, sem incêndio, morte ou queda), carga, estacionamento, imigração, alfândega, conexões internacionais e múltiplos terminais.

## 9. O que depende do dono

Nada disso o time de código consegue resolver sozinho.

| O quê | Por que trava | Quando |
|---|---|---|
| **Play Console** (conta de organização) | Sem ela não há AAB para enviar, compra de teste, produtos nem formulário de segurança de dados. Espera o D-U-N-S grátis | Previsão **2026-11-25** |
| **D-U-N-S** | Pré-requisito da Play Console de organização; o número grátis está em espera | Previsão 2026-11-25 (a mesma da Play Console) |
| **Registro da marca "Little Terminal"** | A busca de 2026-11-01 não achou conflito, mas só o INPI registra | A fazer |
| **Revisão jurídica** | Política de privacidade, ECA Digital (público 13+), LGPD, termos do Tripo; nenhum responsável definido (D-10) | Antes do envio |
| **Cópia segura da chave de upload** | Sem ela, recuperar o acesso exige pedir reset à Google Play | Antes do 1º AAB |
| **App e chaves no LevelPlay** | O appId já existe; falta criar o app na conta da V-STACK para os anúncios recompensados reais | Depois da Play Console |
| **Liberar o firewall** e demais janelas de segurança do Windows | Só ele aprova | Quando aparecer |
| **K2 com 20 pessoas e K3 com amigos** | Medida com gente real; APK enviado por ele | Fim das Fases C e D |
| **Renovar o plano do Tripo** (se ainda houver arte a gerar) | Vence em 2026-11-29 | Antes de 2026-11-29 |
| **Aprovar o texto do GDD v1.5** | A mistura foi aprovada em 2026-11-03 ("pode seguir desta forma", D-25), mas ele ainda não leu o texto do GDD v1.5 nem o roteiro do §4.4 | Quando ler |
| **Autorização final de envio** (D-17) | Publicar é irreversível | No fim |

## 10. Porcentagem estimada e método

**Aproximadamente 50% do contrato de lançamento** (19,5 de 39 pontos), em 2026-11-03: os 49% da `main` em `49e1597` mais a linha B (velocidades) em `parcial`, com a frente `ta-velocidade`.

**Por que caiu de 57% (2026-11-02).** Não foi regressão: em 2026-11-03 o contrato ganhou **9 linhas**, as frentes de gestão A a I, todas em `falta`. Sobre as 30 linhas de antes, a conta **subiu** de 57% para 63% (19,0 de 30), por três mudanças: a tela de configurações (linha 21, de `falta` para `parcial`, PR #48) e a tela de metas, que fechou as linhas 11, 12 e 13 (de `parcial` para `feito`, PR #46, com `verify.sh full` verde e foto). Em pontos: 17,0 + 0,5 + 3 × 0,5 = 19,0.

**Método (o mesmo de antes).** `docs/CONTRATO_LANCAMENTO.md` §1 tem 39 linhas (cada item do §4.1 do GDD, mais o projeto Unity, o AAB, a segurança, a regressão final e as 9 frentes A a I). Cada linha vale 1 ponto e conta assim: **feito 100%**, **parcial 50%**, **depende 25%** (de conta, decisão ou recurso do dono), **falta 0%**. Trabalho em andamento e não mesclado **não conta** (o PR #49, do passe, está aberto e fica de fora). O K2 com gente fica fora da conta de cada linha e aparece na seção 9.

| Estado | Linhas | Pontos |
|---|---|---|
| Feito | 12 | 12,0 |
| Parcial | 14 | 7,0 |
| Depende | 2 | 0,5 |
| Falta | 11 | 0,0 |
| **Total** | **39** | **19,5 → 50%** |

- **Feito (12):** projeto Unity e portão; equipe móvel; pátio e manutenção; quadro de partidas; fluxo de passageiros; 4 incidentes; campanha de 30 objetivos; missões; 20 conquistas; sem interstitial; offline de 2 h; tutorial de 12 passos.
- **Falta (10):** passe de temporada; segurança e privacidade ponta a ponta; regressão final; e 7 das 9 frentes de gestão da v1.5 (A despesas e salários, D contratação e demissão, E vestiário, F limpeza e banheiro, G satisfação, H esteira de restituição, I painel de operações).
- **Depende (2):** anúncios recompensados reais; AAB assinado e enviado.
- **Parcial (15):** aeroportos; rotas; aeronaves; estações; perfis; tripulação; dia a dia; histórias; loja; save; configurações, política e suporte; telemetria; arte final; velocidades (B); ficha ao tocar (C). Uma a uma, estão em `docs/CONTRATO_LANCAMENTO.md` §1, com a evidência de cada PR e o que falta.

**O que o número não mostra.** A porcentagem mede **cobertura do contrato**, não esforço restante nem risco. A maior parte do que falta é tela (aeroportos, rotas, passe e loja), as 9 frentes de gestão (nenhuma começada, e cada uma só fecha com o critério de pronto em 10 itens, `GDD.md` §0.5), integração (IAP, servidor, anúncios, telemetria) e medida em aparelho, e quase toda a parte que depende do dono está na janela de 2026-11-25 em diante. As frentes A a I valem 9 linhas do mesmo peso das demais, mas variam muito de tamanho: a conta não diz isso.

## 11. Regras que não mudam

Valem por cima de qualquer tarefa (GDD §0.4 e `CLAUDE.md`):

- Uma moeda (Cash) na v0.1; Gems só no lançamento, com validação em servidor.
- **Nada aleatório pago**, **nenhum interstitial**, anúncio não personalizado e **sem `AD_ID`**.
- Perder voo custa só a receita dele. Offline com teto de 2 h no código.
- Nenhum dado pessoal no diário nem na telemetria. Criativos gravados do jogo real.
- Print ou arte de jogo de terceiro **nunca** entra no git. Segredo, chave e senha nunca entram no git nem em chat.
- Toda mudança entra por PR na `main`, com `verify.sh` verde e, se mexe em número, com antes e depois no `docs/BALANCE.md`.
- Nenhuma despesa leva a uma derrota inevitável; perder voo custa só a receita dele (`GDD.md` §0.5, regra 6).
- O que parece operacional tem função ou é decoração; o que for novo segue o critério de pronto em 10 itens; a animação segue o estado; ninguém atravessa nada; a loja vem depois do ciclo principal (`GDD.md` §0.5).

## 12. Como manter este documento

Quem fecha um PR que muda uma linha do contrato atualiza, no mesmo PR: a linha em `docs/CONTRATO_LANCAMENTO.md`, o estado no §21 do GDD e este documento (seções 5, 6, 7 e 10). Troque a data do topo e o hash da `main`. Para recalcular a porcentagem, conte os estados das linhas do contrato (39 em 2026-11-03) e use a tabela da seção 10; se a ordem de trabalho mudar, ajuste também a seção 8.
