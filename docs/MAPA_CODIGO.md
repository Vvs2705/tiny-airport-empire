# Mapa do código

Uma página: cada arquivo, para que serve e as funções-chave. Atualize no PR que cria, move ou divide um arquivo (regra do estúdio). Arquivo que passar de ~600 linhas vira `partial` por assunto.

Convenção de origem: `fs:` = Forge Street (`Vvs2705/forge-street`), `dc:` = Dungeon Courier (`Vvs2705/dungeon-courier`). Estado: Fase A, esqueleto vazio (`docs/CONTRATO_V01.md` §9).

## Núcleo — `client/Assets/_TA/Scripts/Core/` (`TA.Core`, C# puro, sem UnityEngine)

| Arquivo | Para quê | Funções-chave | Origem |
|---|---|---|---|
| `Sim.cs` | fachada do núcleo; API para a View documentada no topo | `Tick(dt)` em passos fixos de `Step` (1/30 s, dt limitado a `MaxFrame`, NaN e ≤ 0 não andam); `Steps`, `Time`, `Cash`; `Save(versao, nowUnix)`; `Sim.Load(texto)` com `SaveRefused` | passo fixo de `dc:Scripts/Core/Run.cs`; save de `fs:Scripts/Core/Sim.Save.cs` |
| `SaveEnvelope.cs` | cabeçalho do save: SHA-256, schema, versão do jogo e data | `Wrap`, `Unwrap` → `Missing`, `Ok`, `Legacy`, `Corrupt`, `NewerSchema` | cópia de `fs:Scripts/Core/SaveEnvelope.cs` |
| `TA.Core.asmdef` | assembly do núcleo com `noEngineReferences` | — | `fs:FS.Core.asmdef` |

## View — `client/Assets/_TA/Scripts/View/` (`TA`)

| Arquivo | Para quê | Funções-chave | Origem |
|---|---|---|---|
| `Game.cs` | o único MonoBehaviour: nasce sozinho na cena vazia, chama `sim.Tick`, lê as flags | `Boot`, `Arg`/`ArgF`; `-autoplay N` (N ondas de 180 s sem render, save em ida e volta, `AUTOPLAY OK\|FAIL`, sai 0/1); `-speed N`; `-shot arq -shotdelay s` | leitura de flags de `dc:Scripts/View/Game.cs` |
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

## Cena e pacotes

- `client/Assets/_TA/Scenes/Main.unity`: gerada pelo `Setup.Apply`, vazia.
- `client/Packages/manifest.json`: `com.unity.test-framework` e os módulos de IMGUI, captura de tela e conversão de imagem. uGUI, Input System, IAP e LevelPlay entram com o sistema que os usa.

## Ferramentas — `client/tools/`

| Arquivo | Para quê | Origem |
|---|---|---|
| `verify.sh quick\|full` | o portão: núcleo, viewcheck, EditMode, build Windows e `-autoplay 10` com 0 exceções; logs em `client/Builds/verify/` | `dc:client/tools/verify.sh` |
| `coretests/TACore.Tests.csproj` | `dotnet test`: núcleo e testes EditMode sem abrir o Unity | `dc:client/tools/coretests` |
| `viewcheck/` (`unity_refs.props`, `core/`, `view/`) | `dotnet build`: compila a View contra as DLLs do Unity 6000.3.27f1, sem abrir o Unity | `dc:client/tools/viewcheck` |
