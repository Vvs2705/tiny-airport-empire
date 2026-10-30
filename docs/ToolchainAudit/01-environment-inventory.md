# 01. Inventário do ambiente

Levantado em 2026-10-30 no PC `DESKTOP-4VDN6JG`, com comandos só de leitura. Rótulos: **VERIFICADO** (saída de comando), **PROVÁVEL**, **NÃO VERIFICADO**.

## Hardware e sistema

| Item | Valor | Estado |
|---|---|---|
| Sistema | Windows 10 Pro 22H2, build 19045, 64 bits | VERIFICADO |
| CPU | Intel Core i7-13620H, 10 núcleos, 16 threads | VERIFICADO |
| RAM | 7,71 GB; entre 0,58 e 1,65 GB livres durante a auditoria, com o Chrome do Tripo aberto (1,5 GB) | VERIFICADO |
| GPU | NVIDIA RTX 3050 Laptop, **6 144 MiB** (`nvidia-smi`); Intel UHD integrada | VERIFICADO |
| Disco | C: 476 GB, **36,5 GB livres** | VERIFICADO |

**Correção de premissa:** há GPU dedicada de 6 GB. O limite real para ferramentas locais é a RAM de 7,7 GB. O WMI mostra só 4 GB de VRAM porque esse campo satura; vale o `nvidia-smi`.

## Motor e linguagens

| Item | Valor | Evidência |
|---|---|---|
| Motor | Unity **6000.3.27f1** (LTS), o patch mais recente do 6.3 em 2026-10-30 | `ProjectVersion.txt`; API de releases da Unity |
| Módulos do Unity | AndroidPlayer e Windows; também há o 6000.3.23f1 instalado | pasta do Hub |
| SDK dentro do Unity | plataformas android-34, 35, 36 e **37.0**; build-tools 36.0.0; OpenJDK 17.0.18; `bundletool-all-1.17.2` | pasta `AndroidPlayer` |
| Renderização | Built-in RP, espaço de cor Gamma | `GraphicsSettings.asset`, `ProjectSettings.asset` |
| Linguagens | C# 9 (Unity e dotnet), Python 3.14, Bash | `coretests`, `client/tools` |
| Pacotes Unity | `test-framework` 1.6.0, `ugui` 2.0.0 e 5 módulos: `androidjni`, `imageconversion`, `imgui`, `screencapture`, `ui` | `manifest.json` |

## Linha de comando

| Ferramenta | Versão | Uso aqui |
|---|---|---|
| git / git-lfs | 2.54.0 / 3.7.1 | LFS instalado e sem uso neste repositório |
| gh | 2.91.0, logado como Vvs2705 | sem escopo `workflow` nem `admin:repo_hook` |
| dotnet | 10.0.400 | testes do núcleo e viewcheck |
| Python | 3.14.4 com numpy 2.4.6, scipy 1.18.1, pandas 3.0.3 e Pillow 12.2.0 | ferramentas e arte |
| Node / npm | 24.15.0 / 11.12.1 | só MCPs |
| uv | 0.12.18 | sobe o MCP do Blender |
| ffmpeg | 9.0.1 | vídeo e GIF |
| adb | 1.0.41 | SDK em `%LOCALAPPDATA%\Android\Sdk`, plataformas 35 e 36 |
| Docker | 29.4.0 | disponível; este jogo não usa |
| Blender | **5.2.1 LTS**, abre em modo headless | testado |
| VS Code | instalado | Rider e Visual Studio não encontrados |
| Ausentes | `pre-commit`, `gitleaks`, `java` no PATH (o Unity traz o próprio), `trimesh`, `pygltflib`, `matplotlib` | sem falta real; ver `04-tool-recommendations.md` |

## Agente, Skills e MCPs

| Item | Estado | Evidência |
|---|---|---|
| Claude Code | 2.1.197; o app desktop roda esta sessão; o `claude` do PATH está **sem login** | `claude --version`, `claude -p` |
| Skills globais | 47 em `~/.claude/skills`, mais os plugins `unity:` (Unity), `superpowers`, `ponytail` e `anthropic-skills` | listagem |
| Agentes globais | 17 em `~/.claude/agents` | listagem |
| Skill do projeto | **1, criada nesta auditoria**: `lote-de-arte`. Formato conferido; carregamento numa sessão nova **não verificado** | `.claude/skills/` |
| MCP `blender` | **conectado e testado ao vivo**: `get_addon_status` respondeu com Blender 5.2.1 e `telemetry_consent=false` | ver abaixo |
| MCP `github` | conectado; usado em leitura | ferramentas disponíveis |
| MCP `context7` | conectado | ferramentas disponíveis |
| MCP `shadcn` | configurado; sem uso em jogo Unity | `~/.claude.json` |
| Conectores da conta | Figma, Slack, Supabase, Vercel, Drive, ElevenLabs, Adobe e outros | lista de ferramentas |
| MCP de Unity | não instalado (decisão: não instalar; ver o relatório 04) | — |

### Detalhe do MCP do Blender

- Pacote `mcp-for-blender` (autor Siddharth Ahuja, licença MIT), que vem do repositório `ahujasid/blender-mcp`. Roda por `uvx mcp-for-blender`, **sem versão fixada**: o cache do `uv` tem de 2.1.1 a 2.2.6.
- `BLENDER_MCP_SAFE_MODE=1` está ligado.
- **O addon instalado está atrasado:** protocolo 9, o servidor espera 15. O efeito direto do `uvx` sem versão fixa é este: o servidor anda sozinho e o addon fica para trás.
- Telemetria: ver `08-license-security-and-privacy.md`.

## Serviços

| Serviço | Estado | Observação |
|---|---|---|
| Tripo3D | plano Max até 2026-11-29, ≈ 16 600 créditos (dado da sessão do Tripo; não conferi no painel) | não toquei no Chrome nem no Tripo, como pedido |
| GitHub | repositório **público** | Actions grátis; secret scanning e push protection ligados |
| Unity, LevelPlay, Play Console | fora desta auditoria | Play depende do D-U-N-S, previsto para 2026-11-25 |

## Arte fora do git

| Item | Valor |
|---|---|
| `arte/tripo/` | 244 entradas, 230 zips HD, 9,25 GB (242 zips, 242 FBX, 1 210 imagens) |
| Backup | `3D Objects\TinyAirport-BACKUP`, 5,0 GB, com todos os zips. **Está no mesmo disco C:** |
| Segredos de assinatura | `%USERPROFILE%\.tinyairport\` não existe. Normal nesta fase: ainda não há chave de upload |

## Worktrees

6 worktrees. O checkout principal está em `59f0bb9`, **36 commits atrás** de `origin/main`. O `Library` de cada worktree é pequeno (93 a 123 MB), então o custo é baixo; a limpeza fica para o fim do projeto, como no prompt mestre §9.
