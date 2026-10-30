# 06. Skills e automação

Critério do documento de auditoria: criar só o que resolve necessidade recorrente confirmada. Cada item abaixo tem a necessidade e o teste.

## Scripts novos

Todos em Python stdlib, com `--selftest` que quebra uma entrada de cada jeito e exige o erro certo (padrão do `conteudo_check.py` do Forge Street).

| Script | Necessidade confirmada | O que cobre | Selftest |
|---|---|---|---|
| `client/tools/checa_repo.py` | `Art/Marca` entrou sem `.meta`; o prompt mestre pede busca por segredo antes de cada PR; a regra do `MAPA_CODIGO.md` era só de boa vontade; o `viewcheck` não enxerga módulos fora do manifesto | `.meta` e GUID, núcleo sem Unity, módulos, guardrails, arquivos proibidos, segredos, mapa | 23 quebras |
| `client/tools/auditar_arte.py` | conferência à mão em todo lote do Tripo (10 lotes até agora) | lista × zip × registro × backup, com SHA-256 | 8 cenários |
| `client/tools/checa_apk.py` | GDD §3: "reconferidos no dia do envio"; o projeto nunca teve APK | targetSdk, ABI, `AD_ID`, 16 KB em `.so` e zip | 6 quebras + leitura de ELF |

**Mudança em ferramenta existente:** `client/tools/verify.sh`, com `quick` sem exigir `Library`, trava para Blender e Docker, e o novo `android`.

## CI

| Workflow | Quando | O que faz |
|---|---|---|
| `nucleo.yml` | todo push e PR | selftests, `checa_repo.py`, `dotnet test`; agora com `permissions: contents: read`, `timeout-minutes` e actions novas (`checkout@v7`, `setup-dotnet@v6`), que tiram o aviso de Node 20 descontinuado |
| `balanco.yml` | PR que toca no núcleo, nos testes ou no próprio workflow; ou à mão | `BalanceTests.Portao*`: TA-1, TA-2 e §5.13, com a tabela no resumo do job |

`Varredura_FaseC` e `Antes_RegrasDaV01` ficam de fora de propósito: são medições, não portões, e somam 2 min 45 s.

## Skills

| Skill | Necessidade | Decisão |
|---|---|---|
| `lote-de-arte` | fechar lote do Tripo, tarefa repetida 10 vezes e com vários passos | **criada** em `.claude/skills/`. Frontmatter conferido contra uma skill que já carrega; os comandos dela foram executados. A listagem numa sessão nova ficou **NÃO VERIFICADA**: a lista é montada no início da sessão, e o `claude` do PATH está sem login |
| `game-project-audit` | este relatório fica no repositório e `verify.sh` cobre o dia a dia | não criar |
| `qa-regression`, `release-check` | `verify.sh quick\|full\|android` já é o comando único | não criar |
| `asset-validation` | coberta pelo `lote-de-arte` | não criar |
| `npc-pipeline`, `ability-pipeline`, `animation-review` | o jogo não tem essas necessidades | não criar |
| `performance-audit` | sem medição ainda; criar depois do primeiro K3 real | adiar |

## MCPs

| MCP | Estado | Permissão mínima | Como desligar |
|---|---|---|---|
| `blender` | conectado; `execute_blender_code` roda Python no Blender, com `BLENDER_MCP_SAFE_MODE=1` | safe mode ligado; generators e bibliotecas `off` | remover `mcpServers.blender` |
| `github` | conectado | escopo da conta | conectores do app |
| `context7` | conectado | leitura de documentação | conectores do app |
| `shadcn` | configurado, sem uso em jogo | — | remover de `mcpServers` |

**Economia de contexto:** esta sessão recebeu centenas de nomes de ferramentas de conectores sem relação com o jogo (Vercel, Supabase, Slack, Windsor e outros). Desligar os que este repositório não usa reduz o custo de cada sessão, e cabe ao Vinicius, no painel de conectores.

## Reuso pronto, ainda sem copiar

Copiar agora seria código sem uso. Cada um entra junto com a frente que precisa dele:

| Origem | Arquivo | Quando |
|---|---|---|
| Dungeon Courier | `audio_gen.py`, `Scripts/View/Audio.cs` | frente de áudio |
| Dungeon Courier | `loja_assets.py`, `loja_fotos.sh`, `criativos.py`, `icone_app.py` | Fase E |
| Forge Street | `medir_aparelho.sh` | primeira medida no POCO F4 |
| Forge Street | `soak_emulador.sh`, `soak_relatorio.py` | teste de 20 minutos. A Unity não suporta emulador Android (V), então conferir antes de confiar nele |
| Forge Street | `placeholder_gate.py` | antes da Fase E, para provar "sem placeholder" |
