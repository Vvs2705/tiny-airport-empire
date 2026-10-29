# Mapa do código

Uma página: cada arquivo, para que serve e as funções-chave. Atualize no PR que cria, move ou divide um arquivo (regra do estúdio). Arquivo que passar de ~600 linhas vira `partial` por assunto.

Convenção de origem: `fs:` = Forge Street (`Vvs2705/forge-street`), `dc:` = Dungeon Courier (`Vvs2705/dungeon-courier`). Estado: Fase B, núcleo da v0.1 com bot e testes; View greybox a seguir (`docs/CONTRATO_V01.md` §9).

## Núcleo — `client/Assets/_TA/Scripts/Core/` (`TA.Core`, C# puro, sem UnityEngine)

| Arquivo | Para quê | Funções-chave | Origem |
|---|---|---|---|
| `Sim.cs` | fachada do núcleo; API para a View documentada no topo. Relógio do quadro (passo fixo de 1/30 s), onda com 2 voos, chegadas (função pura da semente), filas FIFO, atendimento por posto, portão que fecha na hora, renda, marcos, história | `Tick(dt)`; `Tap`, `Move`, `Buy`, `Hire`; `PodeTocar`, `Presentes`, `Alocados`, `Custo`, `CustoContratar`, `TempoPassos`; `Chegada`, `NovoPax`, `VooDaHistoria`; `IrParaOnda` (flag `-onda`) | passo fixo de `dc:Scripts/Core/Run.cs`; sequência de Weyl do VIP de `fs:Scripts/Core/Sim.cs` |
| `Sim.Estados.cs` | estado de cada estação (normal, atenção, crítico) e o voo em risco, a cada 0,5 s | `AtualizaEstados(emitir)` | — |
| `Sim.Save.cs` | save chave=valor no envelope: filas, quem está em atendimento, equipe andando, voos da onda; lixo vira padrão limitado | `Save(versao, nowUnix)` (SavedAt nunca recua), `Sim.Load(texto[, tuning, semente])` | `fs:Scripts/Core/Sim.Save.cs` |
| `Sim.Offline.cs` | cofre offline e dia local | `ApplyOffline(dt, claimId)` (trava de 2 h, Δt ≤ 0 = 0, teto relativo, id de coleta), `Agora(nowUnix, fuso)`, `DiaLocal` | `ApplyOffline` de `fs:Scripts/Core/Sim.Consultas.cs`; dia de `fs:Scripts/Core/Metas.cs` |
| `Anuncios.cs` | regras dos 2 rewarded (Cofre 2x; Receita 2x por 5 min, 3 por dia), sem SDK | `Pode`, `Previa`, `Oferta` (id de transação), `Pagar`, `Veto(arrasto, tutorial, pausa)` | `fs:Scripts/Core/Anuncios.cs`, enxuto |
| `Defs.cs` | enums salvos (`Estacao`, `Melhoria`, `Estado`, `Ev`), `Pax`, `Station`, `Funcionario`, `Voo`, `Tuning` (os números do contrato §4) e `Historias` | `Tuning.Padrao`, `Tuning.OfflineTrava`, `Tuning.Pax(onda)` | `fs:Scripts/Core/Defs.cs` |
| `Bot.cs` | bot headless com perfis Bom, Lento e Parado; medição e a linha do `-autoplay` | `Bot.Step`, `Bot.Medir(perfil, semente, ondas)`, `Bot.Autoplay(n, out ok)` | `fs:Scripts/Core/Bot.cs` (perfil como dado) |
| `SaveEnvelope.cs` | cabeçalho do save: SHA-256, schema, versão do jogo e data | `Wrap`, `Unwrap` → `Missing`, `Ok`, `Legacy`, `Corrupt`, `NewerSchema` | cópia de `fs:Scripts/Core/SaveEnvelope.cs` |
| `TA.Core.asmdef` | assembly do núcleo com `noEngineReferences` | — | `fs:FS.Core.asmdef` |

## View — `client/Assets/_TA/Scripts/View/` (`TA`)

| Arquivo | Para quê | Funções-chave | Origem |
|---|---|---|---|
| `Game.cs` | o único MonoBehaviour: nasce sozinho na cena vazia, chama `sim.Tick`, lê as flags | `Boot`, `Arg`/`ArgF`; `-autoplay N` (`Bot.Autoplay`: Bom, Lento e Parado com a semente 1 e retomada no meio da onda; `AUTOPLAY OK\|FAIL` com embarque e ganho de mover, sai 0/1); `-speed N`; `-shot arq -shotdelay s` | leitura de flags de `dc:Scripts/View/Game.cs` |
| `TA.asmdef` | assembly da View (só referencia o `TA.Core`) | — | `dc:DC.asmdef` |

## Editor — `client/Assets/_TA/Editor/` (`TA.EditorTools`)

| Arquivo | Para quê | Funções-chave | Origem |
|---|---|---|---|
| `Setup.cs` | settings, cena gerada e builds por script, idempotente | `Apply` (appId e nome **provisórios** de dev, D-03; retrato; Android ARM64 IL2CPP API 26+; janela 540×960), `BuildWindows` → `Builds/win/TinyAirport.exe`, `BuildAndroidDev`, `VersionCode`; `SemIdDePublicidade` tira o `AD_ID` do manifesto Android | cópia de `dc:Editor/Setup.cs`, sem release, AAB, chave de upload e ícones |
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
- `client/Packages/manifest.json`: `com.unity.test-framework` e os módulos de IMGUI, captura de tela e conversão de imagem. uGUI, Input System, IAP e LevelPlay entram com o sistema que os usa.

## Ferramentas — `client/tools/`

| Arquivo | Para quê | Origem |
|---|---|---|
| `verify.sh quick\|full` | o portão: núcleo, viewcheck, EditMode, build Windows e `-autoplay 10` com 0 exceções; logs em `client/Builds/verify/` | `dc:client/tools/verify.sh` |
| `coretests/TACore.Tests.csproj` | `dotnet test`: núcleo e testes EditMode sem abrir o Unity | `dc:client/tools/coretests` |
| `viewcheck/` (`unity_refs.props`, `core/`, `view/`) | `dotnet build`: compila a View contra as DLLs do Unity 6000.3.27f1, sem abrir o Unity | `dc:client/tools/viewcheck` |
