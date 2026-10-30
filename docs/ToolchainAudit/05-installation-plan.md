# 05. Plano de instalação

**Nada foi baixado nem instalado nesta auditoria.** Tudo que entrou é arquivo do repositório, usando Python, dotnet, Unity e Blender já presentes. O `dotnet` só restaurou pacotes que o projeto já usava.

## Feito, na ordem

| # | Mudança | Comando de teste | Como desfazer |
|---|---|---|---|
| 1 | `checa_repo.py` no `verify.sh` e no CI | `python client/tools/checa_repo.py --selftest` | apagar o arquivo e a função `repo` |
| 2 | `auditar_arte.py` | `python client/tools/auditar_arte.py --selftest` | apagar o arquivo |
| 3 | `viewcheck` acha o uGUI em outro worktree | `bash client/tools/verify.sh quick` | reverter `verify.sh` e `unity_refs.props` |
| 4 | Trava de Blender e Docker no `verify.sh` | `verify.sh full` com o Blender aberto | reverter a linha do `guard` |
| 5 | `EnumsTests` | `dotnet test client/tools/coretests` | apagar o teste e o `.meta` |
| 6 | `balanco.yml` e endurecimento do `nucleo.yml` | PR que toca no núcleo | reverter os YAML |
| 7 | 6 `.meta` de `Art/Marca`, gerados pelo Unity | `checa_repo.py` | apagar os `.meta` |
| 8 | targetSdk 36 fixo no `Setup.cs` | `verify.sh android` | voltar para `AndroidApiLevelAuto` |
| 9 | `checa_apk.py` e `verify.sh android` | `python client/tools/checa_apk.py --selftest` | apagar o arquivo e a função `android` |
| 10 | Skill `lote-de-arte` | sessão nova do Claude Code | apagar `.claude/skills/lote-de-arte/` |

## Pendente, depende do Vinicius

### A. MCP do Blender: versão fixa, addon novo, telemetria desligada

**Por que depende de você:** mexe no `~/.claude.json`, que vale para todos os projetos. **Custo:** zero. **Download:** pacote `mcp-for-blender` 2.2.6 do PyPI, já no cache do `uv` deste PC.

1. Atualize o addon, com o Blender fechado:
   ```bash
   uvx mcp-for-blender==2.2.6 install-addon
   ```
2. Em `~/.claude.json`, em `mcpServers.blender`, troque `"args": ["mcp-for-blender"]` por `"args": ["mcp-for-blender==2.2.6"]`, e acrescente em `env` a linha `"DISABLE_TELEMETRY": "true"`.
3. Reinicie o Claude e confira com `get_addon_status`.

**Desfazer:** voltar os dois campos. **Não testado:** estes passos foram lidos do próprio pacote e da mensagem do servidor, mas não executados.

### B. GitHub: proteger a `main` e ligar Dependabot alerts

**Por que depende de você:** é configuração de segurança do repositório, fora do aval de commit, PR e merge. **Custo:** zero em repo público.

1. Em *Settings → Rules → Rulesets → New branch ruleset*, escolha o branch `main`, ligue **Require status checks to pass** e adicione `coretests`.
2. Não ligue *Require a pull request approval*: o estúdio mescla sem revisão.
3. Em *Settings → Advanced Security*, ligue **Dependabot alerts**.

**Risco:** conta pessoal não tem lista de bypass. Com o CI fora do ar, nada entra na `main` até ele voltar. Se a tela pedir plano pago, pare: a documentação da GitHub se contradiz nesse ponto.

### C. Atualizar o Unity quando sair o patch

**Por que depende de você:** atualizar a engine exige autorização, e é um download grande (tamanho não verificado).

Quando https://unity.com/security/unity-brotli-decoder-vulnerability mostrar a versão corrigida para 6000.3, instale pelo Hub com os módulos Android e Windows. A versão aparece em quatro lugares: `ProjectVersion.txt`, `verify.sh`, `unity_refs.props` e `checa_apk.py`. Rode `verify.sh full` e `verify.sh android`.

### D. Tripo: ler os termos logado

**Por que depende de você:** as páginas deram 403 fora da conta. **Pergunta:** o que acontece com os direitos dos modelos depois que o plano vence em 2026-11-29? Salve o PDF dos termos no backup com a data, antes do vencimento.

### E. Cópia da arte fora do PC

**Por que depende de você:** enviar arte paga a um serviço externo exige aval. **Escolha:** drive privado da V-STACK ou HD externo. **Tamanho:** 5,0 GB (só os zips). O backup atual está no mesmo disco C:.

## Não instalar

Ver `04-tool-recommendations.md`. Em especial: nenhum MCP de Unity, nenhum gerador local, nenhum agente adicional e nenhum pacote de agentes de terceiros.
