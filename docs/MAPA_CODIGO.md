# Mapa do código

Uma página: cada arquivo, para que serve e as funções-chave. Atualize no PR que cria, move ou divide um arquivo (regra do estúdio). Arquivo que passar de ~600 linhas vira `partial` por assunto.

Convenção de origem: `fs:` = Forge Street (`Vvs2705/forge-street`), `dc:` = Dungeon Courier (`Vvs2705/dungeon-courier`). Estado: Fase B, v0.1 jogável com a arte do Tripo (malha nos corpos rígidos, sprite nas pessoas): núcleo com bot e testes, View com quadro, arrasto, tutorial, diário e pátio vivo (`docs/CONTRATO_V01.md` §9).

## Núcleo — `client/Assets/_TA/Scripts/Core/` (`TA.Core`, C# puro, sem UnityEngine)

| Arquivo | Para quê | Funções-chave | Origem |
|---|---|---|---|
| `Sim.cs` | fachada do núcleo; API para a View documentada no topo. Relógio do quadro (passo fixo de 1/30 s), onda com 2 voos, chegadas (função pura da semente), filas FIFO, atendimento por posto, portão que fecha na hora, renda, marcos, história | `Tick(dt)`; `Tap`, `Move`, `Buy`, `Hire`; `PodeTocar`, `Presentes`, `Alocados`, `Custo`, `CustoContratar`, `TempoPassos`; `Chegada`, `NovoPax`, `VooDaHistoria`; `IrParaOnda` (flag `-onda`) | passo fixo de `dc:Scripts/Core/Run.cs`; sequência de Weyl do VIP de `fs:Scripts/Core/Sim.cs` |
| `Sim.Estados.cs` | estado de cada estação (normal, atenção, crítico) e o voo em risco, a cada 0,5 s | `AtualizaEstados(emitir)` | — |
| `Sim.Save.cs` | save chave=valor no envelope: filas, quem está em atendimento, equipe andando, voos da onda; lixo vira padrão limitado | `Save(versao, nowUnix)` (SavedAt nunca recua), `Sim.Load(texto[, tuning, semente])` | `fs:Scripts/Core/Sim.Save.cs` |
| `Sim.Offline.cs` | cofre offline e dia local | `ApplyOffline(dt, claimId)` (trava de 2 h, Δt ≤ 0 = 0, teto relativo, id de coleta), `Agora(nowUnix, fuso)`, `DiaLocal` | `ApplyOffline` de `fs:Scripts/Core/Sim.Consultas.cs`; dia de `fs:Scripts/Core/Metas.cs` |
| `Anuncios.cs` | regras dos 2 rewarded (Cofre 2x; Receita 2x por 5 min, 3 por dia), sem SDK | `Pode`, `Previa`, `Oferta` (id de transação), `Pagar`, `Veto(arrasto, tutorial, pausa)` | `fs:Scripts/Core/Anuncios.cs`, enxuto |
| `Defs.cs` | enums salvos (`Estacao`, `Melhoria`, `Estado`, `Ev`), `Pax`, `Station`, `Funcionario`, `Voo`, `Tuning` (os números do contrato §4) e `Historias` | `Tuning.Padrao`, `Tuning.OfflineTrava`, `Tuning.Pax(onda)` | `fs:Scripts/Core/Defs.cs` |
| `Bot.cs` | bot headless com perfis Bom, Lento e Parado; medição e a linha do `-autoplay` | `Bot.Step`, `Bot.Pensa(sim, dt)` (a View no `-bot`), `Bot.Medir(perfil, semente, ondas)`, `Bot.Autoplay(n, out ok)` | `fs:Scripts/Core/Bot.cs` (perfil como dado) |
| `SaveEnvelope.cs` | cabeçalho do save: SHA-256, schema, versão do jogo e data | `Wrap`, `Unwrap` → `Missing`, `Ok`, `Legacy`, `Corrupt`, `NewerSchema` | cópia de `fs:Scripts/Core/SaveEnvelope.cs` |
| `TA.Core.asmdef` | assembly do núcleo com `noEngineReferences` | — | `fs:FS.Core.asmdef` |

## View — `client/Assets/_TA/Scripts/View/` (`TA`)

| Arquivo | Para quê | Funções-chave | Origem |
|---|---|---|---|
| `Game.cs` | o MonoBehaviour de fluxo: nasce sozinho na cena vazia, chama `sim.Tick` (parado com tela modal ou explicação do tutorial), grava o save (5 s, pausa, saída; escrita atômica; nunca por cima de save recusado), paga o cofre ao abrir e ao voltar, escreve o diário CSV e lê as flags do contrato §7 | `Boot`, `Arg`/`ArgF`; `-autoplay N` (`Bot.Autoplay`: Bom, Lento e Parado com a semente 1 e retomada no meio da onda; `AUTOPLAY OK\|FAIL` com embarque e ganho de mover, sai 0/1); com `-testsession`: `-bot`, `-speed N`, `-tela nome`, `-onda K`, `-passageiros N`, `-tutorial`, `-shot arq -shotdelay s`, `-record pasta`; `Diario(evento, a, b, c)`; `VerAnuncio`/`AnuncioConcluido` |
| `Hud.cs` | HUD em uGUI por código (referência 1080×1920, área segura), em faixas finas por cima do mundo: barra de cima, quadro de partidas (voo, destino, hora de fechamento, estado, a bordo, barra do tempo, chip da mala na cor do voo), terminal, barra de baixo; camada de rótulos do mundo | `Build`, `Toast`, `Bloqueia`, `MundoAceita`, `M` (o Mundo) | `dc:Scripts/View/Hud.cs` |
| `Mundo.cs` | o aeroporto em **3D em tempo real** (GDD §5.13, D-18): câmera ortográfica a 45°/45° (o ângulo das folhas), de perto nas salas, com pan e zoom; planta em escada (entrada, check-in, segurança, loja, portão, ponte); salas 4×4 em corte com fila entre fitas; **malhas do Tripo** (`Peca`: `Resources/Malhas` da `ArtLocal`, fora do git; sem elas, as primitivas da v0.1); prédio do terminal, torre e hangar; placa no chão de cada sala e selo de estado só em atenção/crítico; toque na sala, arrasto do funcionário e "mover para"; `-vista` | `Atualiza(dt, regiao)`, `Evento`, `ZonaTela`, `FuncTela`, `Arrastando`, `Vista`, `Contagem` | — |
| `Mundo.Pessoas.cs` | pessoas em **sprite pré-renderizado** num quad virado para a câmera (4 diagonais com espelho): 4 corpos de passageiro com a mala na cor do voo, Ana/Beto/Caio com o uniforme tingido na cor do setor (assenta em 0,35 s ao chegar; "atende" no balcão), balão de irritação nos 3 da frente da fila crítica, piloto, despachante, bagageiro e mecânico no pátio; pool de bonecos que trocam de folha | `SincronizaPax`, `Funcionarios`, `Pinta`, `Veste`, `Olha`, `GentePatio`, `AndaPatio` | — |
| `Mundo.Patio.cs` | pátio vivo, só visual: 2 aviões no ciclo pouso → taxi → estande → pushback (rebocador no nariz) → taxi → decolagem por trajeto com curvas arredondadas; estande da ponte e remoto; 6 veículos em volta do avião estacionado; sombra em elipse | `Patio3D`, `Caminho`, `Pousa`, `Parte`, `ParaPonte`, `AviaoPasso`, `VeiculoPasso`, `EventoPatio` | — |
| `SpriteSheet.cs` | folha de uma pessoa (`Resources/Sprites/<ator>/meta.json` + 1 PNG por clipe, cor em cima e máscara embaixo); `Material` com o shader `TA/SpriteTinta` | `Get`, `Pick`, `FrameAt`, `Frame` | cópia enxuta de `dc:Scripts/View/SpriteSheet.cs` |
| `Hud.Telas.cs` | telas modais que seguram o relógio: melhorias (efeito escrito antes de comprar), mover para (alternativa ao arrasto), cofre offline, anúncio simulado (3 s, só então o `Pagar`), pausa, álbum | `AbreTela(nome)`, `AbreCofre`, `AbreAnuncio` | — |
| `Tour.cs` | tutorial dos passos 1–9 da Origem (véu com buraco, contorno, seta, cartão da Lara, dedo arrastando no passo 7), um por vez, pulável, gravado no PlayerPrefs `ta_tour` | `MostraPasso(n)` (flag `-tela passoN`), `ReverTour`, `PulaTour` | `dc:Scripts/View/Tour.cs` |
| `Art.cs` | arte procedural (formas com antialias em cache) e helpers de uGUI (`Node`, `Img`, `NewText`, `Panel`, `NewButton`, `Toque`), `Aperta`, `Dp` e `AreaSegura` | `Art.*`, `Dp.Px`, `AreaSegura.AncorarDentro` | `dc:Scripts/View/Art.cs`, `Dp.cs`, `AreaSegura.cs` |
| `Textos.cs` | todo texto de tela em pt-BR numa tabela `L(pt)`, pronta para as colunas en/es/id da D-08 | `Textos.*` | `dc:Scripts/View/Textos.cs` |
| `Exportar.cs` | exportar o diário só quando o jogador manda (Android: MediaStore/ACTION_SEND; Windows: abre a pasta) | `Exportar.Diario` | cópia de `dc:Scripts/View/Exportar.cs` | leitura de flags de `dc:Scripts/View/Game.cs` |
| `TA.asmdef` | assembly da View (só referencia o `TA.Core`) | — | `dc:DC.asmdef` |

## Editor — `client/Assets/_TA/Editor/` (`TA.EditorTools`)

| Arquivo | Para quê | Funções-chave | Origem |
|---|---|---|---|
| `Setup.cs` | settings, cena gerada e builds por script, idempotente | `Apply` (appId e nome **provisórios** de dev, D-03; retrato; Android ARM64 IL2CPP API 26+; janela 540×960), `BuildWindows` → `Builds/win/TinyAirport.exe`, `BuildAndroidDev`, `VersionCode`; `SemIdDePublicidade` tira o `AD_ID` do manifesto Android | cópia de `dc:Editor/Setup.cs`, sem release, AAB, chave de upload e ícones |
| `MalhaImport.cs` | import das malhas da `ArtLocal`: só a malha (sem material, animação, câmera, luz ou colisor), compressão média | `OnPreprocessModel` | — |
| `SpriteImport.cs` | import das folhas (`Resources/Sprites`) e da arte de interface (`Resources/UI`): DXT5 no PC, ASTC no Android | `OnPreprocessTexture` | cópia de `dc:Editor/SpriteImport.cs` |

## Testes — `client/Assets/_TA/Tests/EditMode/` (`TA.Tests`)

| Arquivo | Cobre |
|---|---|
| `CoreTests.cs` | passo fixo, dt inválido (NaN, 0, negativo), teto do quadro; save em ida e volta, vazio, corrompido, schema futuro, sem cabeçalho, Cash negativo e cabeçalho do envelope |
| `RegrasTests.cs` | onda e horários, chegadas, portão que fecha na hora, fluxo FIFO, toque, embarque só depois de abrir, mover (2,5 s sem atender), contratação, upgrades, estado crítico com o voo em risco, marcos, história, loja, bônus, Receita 2x, determinismo |
| `SaveTests.cs` | ida e volta estável, retomada no meio da onda igual, lixo vira padrão limitado, onda que não bate com o relógio, schema futuro; a linha do `-autoplay` |
| `ExploitTests.cs` | invariantes da Origem §50 como testes negativos nomeados (perder voo, offline, sorteio, id de transação) e os ataques de relógio, teto de 2 h, teto relativo e anúncio |
| `BalanceTests.cs` | `[Explicit]`: portão TA-1/TA-2 com 200 sementes × 10 ondas e a tabela do `docs/BALANCE.md` |

## Cena e pacotes

- `client/Assets/_TA/Scenes/Main.unity`: gerada pelo `Setup.Apply`, vazia.
- `client/Assets/_TA/Shaders/SpriteTinta.shader`: as folhas das pessoas; onde a máscara (metade de baixo) acende, 2 × cinza × a cor do vértice (o `SpriteRenderer.color`), sem MaterialPropertyBlock, para o lote não quebrar.
- `client/Assets/_TA/Resources/Sprites/<ator>/`: folhas do elenco (`elenco_sprites.sh`), no git. `client/Assets/_TA/ArtLocal/`: malhas decimadas (`malhas_ta.py`), **fora do git** (licença do Tripo, GDD §10.2).
- `client/Packages/manifest.json`: `com.unity.test-framework`, `com.unity.ugui` (a HUD) e os módulos de UI, IMGUI, captura de tela e conversão de imagem. A entrada é a do Input Manager antigo com o `StandaloneInputModule` do uGUI; Input System, IAP e LevelPlay entram com o sistema que os usa.

## Ferramentas — `client/tools/`

| Arquivo | Para quê | Origem |
|---|---|---|
| `verify.sh quick\|full` | o portão: núcleo, viewcheck, EditMode, build Windows e `-autoplay 10` com 0 exceções; logs em `client/Builds/verify/` | `dc:client/tools/verify.sh` |
| `coretests/TACore.Tests.csproj` | `dotnet test`: núcleo e testes EditMode sem abrir o Unity | `dc:client/tools/coretests` |
| `viewcheck/` (`unity_refs.props`, `core/`, `view/`) | `dotnet build`: compila a View contra as DLLs do Unity 6000.3.27f1 e a `UnityEngine.UI.dll` de `client/Library/ScriptAssemblies`, sem abrir o Unity | `dc:client/tools/viewcheck` |
| `fotos_v01.sh` | fotos 540×960 de cada tela, do estresse com 60 passageiros, de perto, do pátio (pouso e turnaround) e geral, de cada passo do tutorial, a rodada `-tutorial -bot`, o K3 (`-perf`) e o `-record` do avião chegando com a Ana trocando de uniforme; saída em `client/Builds/validation_v01/` | — |
| `malhas_ta.py` | Blender headless: corpo rígido HD do Tripo → malha decimada (teto de triângulos por peça), medida de projeto, pivô no chão, textura reduzida, FBX + PNG na `ArtLocal`; folha de conferência no ângulo do jogo | — |
| `elenco_ta.py` | Blender headless: personagem HD (5 texturas 4K) → `.blend` leve (só a base, 1024 px) com o uniforme no material `TA_MASK` (cor da textura e faixa de altura); `--remask` refaz só a máscara | — |
| `render_sprites.py` | Blender headless: `.blend` rigado + clipes → folhas; o TA acrescenta `--diag` (4 diagonais), `--mask` (máscara empilhada embaixo, uniforme em cinza neutro) e `--mask-attach` (a mala) | cópia de `dc:client/tools/render_sprites.py` |
| `elenco_sprites.sh` | roda o `render_sprites.py` no elenco (4 passageiros com a mala, 3 funcionários, 4 do pátio) e põe as folhas em `Resources/Sprites` | — |
| `montagem.py` | folha de contato e GIF de um `-record` (Pillow) | — |
| `diario_report.py` | relatório do playtest a partir dos diários CSV exportados: K2 (pessoas e episódios), embarque médio, uso de offline, anúncio, histórias e upgrades; `--autoteste` com diários sintéticos | `dc:client/tools/diario_report.py` |
