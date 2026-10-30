# 03. Matriz de capacidades e lacunas

Estado em 2026-10-30, depois das integrações desta auditoria. Estados: DISPONÍVEL, PARCIAL, AUSENTE, DESCONHECIDO, NÃO APLICÁVEL, BLOQUEADO. Custo e licença detalhados em `04-tool-recommendations.md`.

## Núcleo, testes e portão

| Capacidade | Estado | Evidência | Impacto | Candidata | Custo/licença | Compatibilidade | Risco | Ação | Sucesso |
|---|---|---|---|---|---|---|---|---|---|
| Testes do núcleo sem Unity | DISPONÍVEL | 70 verdes, 99,3% de linhas | crítico | manter | grátis | dotnet 10 | baixo | manter | CI verde |
| Portão local em um comando | DISPONÍVEL (corrigido) | `quick` falhava em worktree novo; agora OK em 5 s | alto | `verify.sh` | grátis | Git Bash | baixo | **feito** | `quick` sai 0 em worktree novo |
| Portão de balanceamento automático | DISPONÍVEL (novo) | `[Explicit]` só à mão; agora `balanco.yml` | alto | GitHub Actions | grátis em repo público | ubuntu-latest | baixo | **feito** | PR do núcleo mostra TA-1, TA-2 e §5.13 |
| Trava de ordem dos enums salvos | DISPONÍVEL (novo) | a regra não tinha teste; agora `EnumsTests`, com prova vermelha | alto | NUnit | grátis | Unity e dotnet | baixo | **feito** | trocar a ordem derruba o teste |
| Integridade do projeto Unity (`.meta`, GUID, módulos) | DISPONÍVEL (novo) | 6 `.meta` faltavam em `Art/Marca`; `TA.rsp` sem módulos de áudio | alto | `checa_repo.py` | grátis, stdlib | Python 3 | baixo | **feito** | `checa_repo` sai 0 no CI |
| Mutação | PARCIAL | `mutacao.py`: 9 travas | médio | Stryker.NET | grátis, Apache-2.0 | núcleo .NET | baixo | adiar | só se o núcleo crescer sem teste |
| CI com Unity | AUSENTE | CI só roda `dotnet test` | médio | GameCI | grátis; 3 segredos | imagem de 8,75 GB em SSD de 14 GB | alto: senha e `.ulf` | **adiar**: `full` leva 65 s no PC | só se o PC não der conta |
| Testes de View | PARCIAL | autoplay e fotos à mão | médio | `-bot`, `-tela` | grátis | — | — | manter | fotos conferidas |

## Android e publicação

| Capacidade | Estado | Evidência | Impacto | Candidata | Custo/licença | Compatibilidade | Risco | Ação | Sucesso |
|---|---|---|---|---|---|---|---|---|---|
| APK com requisitos da Play conferidos | DISPONÍVEL (novo) | nunca houve APK; `checa_apk.py` e `verify.sh android` | crítico | stdlib + `aapt2` do Unity | grátis | Unity 6000.3.27f1 | baixo | **feito** | relatório 07 |
| targetSdk determinístico | DISPONÍVEL (corrigido) | era `Auto`, com android-37.0 instalado; agora 36 fixo | crítico | `Setup.cs` | grátis | Play ≥ 36, Unity 35 e 36 | baixo | **feito** | `targetSdk=36` no APK |
| Motor sem falha conhecida | BLOQUEADO | 2 avisos da Unity com patch "Pending" para 6000.3 | alto | próximo patch 6000.3 | grátis | mesma linha LTS | médio | **esperar** a Unity | build final com patch |
| Medição em aparelho (K3, PSS, 20 min) | BLOQUEADO | depende da View com arte e de "pode testar" | crítico | `medir_aparelho.sh` e `soak_*` do Forge | grátis, reuso | adb 1.0.41 | baixo | adiar | p10 ≥ 30 FPS |
| Aparelho de entrada | AUSENTE | só o POCO F4, que não é de entrada | alto | aparelho barato | fora do orçamento de ferramenta | — | — | decisão do Vinicius | K3 medido num Android de entrada |
| AAB, assinatura, ficha da loja | AUSENTE | Fase E; `loja_*` e `criativos.py` no DC | alto | reuso do DC | grátis | — | baixo | adiar | só falta publicar |
| Telemetria e crash | PARCIAL | diário CSV; D-11 aberta | médio | Android vitals, Crashlytics, Sentry | grátis no início | SDK a conferir | médio: dados | adiar até D-11 | D-11 decidida |

## Arte

| Capacidade | Estado | Evidência | Impacto | Candidata | Custo/licença | Compatibilidade | Risco | Ação | Sucesso |
|---|---|---|---|---|---|---|---|---|---|
| Geração 3D | DISPONÍVEL | 230 zips, ≈ 16 600 créditos | crítico | Tripo Max | já pago | Chrome | direitos após vencer: NÃO VERIFICADO | manter | ler os termos |
| Auditoria de lote | DISPONÍVEL (novo) | era feita à mão a cada lote | alto | `auditar_arte.py` | grátis, stdlib | — | baixo | **feito** | `--sha` sai 0 |
| Decimação e sprites | PARCIAL | 32 malhas e 11 atores só no branch | crítico | `malhas_ta.py`, `render_sprites.py` | grátis | Blender 5.2.1 | RAM | mesclar `feat/view-tripo` | malhas na `main` |
| Automação do Blender | DISPONÍVEL | MCP ao vivo; addon no protocolo 9 de 15 | médio | atualizar e fixar | grátis, MIT | Blender 5.2.1 | telemetria mínima | **pendente do Vinicius** | versão fixa |
| Geradores locais de imagem e 3D | NÃO APLICÁVEL | Tripo cobre; RAM de 7,7 GB | baixo | ComfyUI, TRELLIS.2, Hunyuan3D, TripoSR | grátis | não cabem ou no limite | RAM, licença | **rejeitar** | — |
| Fonte final | AUSENTE | `LegacyRuntime.ttf` | alto | Lilita One e Nunito do DC, mono OFL | grátis, OFL | uGUI | baixo | reuso na Fase C | sem fonte padrão na loja |
| Backup fora do disco | AUSENTE | backup no mesmo C: | alto | drive privado | a escolher | — | envio de arte paga | **pendente do Vinicius** | cópia fora do PC |

## Áudio

| Capacidade | Estado | Evidência | Impacto | Candidata | Custo/licença | Compatibilidade | Risco | Ação | Sucesso |
|---|---|---|---|---|---|---|---|---|---|
| Efeitos e música | AUSENTE | nenhum clipe, sem módulo de áudio | alto | `audio_gen.py` e `Audio.cs` do DC | grátis, numpy e scipy instalados | requer `com.unity.modules.audio` | baixo | reuso na Fase C | GDD §12 |
| Motor de avião | DESCONHECIDO | GDD §12 pede teste cedo | médio | síntese; depois Kenney CC0 ou ElevenLabs pago | grátis; ElevenLabs a partir de US$ 1 a 6 | — | ElevenLabs: Termos × página | testar na frente de áudio | som aprovado |

## Fora do escopo do jogo

| Capacidade | Estado | Motivo |
|---|---|---|
| Multiplayer, Nakama, Agones, Netcode | NÃO APLICÁVEL | single-player |
| NPC com LLM, ML-Agents, Ollama, Convai, Inworld | NÃO APLICÁVEL | passageiros por regra determinística; o bot cobre o balanceamento |
| Playwright | NÃO APLICÁVEL | não há jogo web |
| Rigging com Rigify, Cascadeur, DeepMotion | NÃO APLICÁVEL | D-18: Auto Rig do Tripo e clipes do Mixamo |
| Pixel art | NÃO APLICÁVEL | estilo low-poly |
