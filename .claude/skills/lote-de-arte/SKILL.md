---
name: lote-de-arte
description: Fecha um lote de arte do Tiny Airport e confere se cada peça tem zip íntegro, registro com SHA em docs/PROVENIENCIA.md e cópia no backup. Use ao terminar geração ou export de um lote, antes do PR de arte, ou quando pedirem para auditar a arte, conferir o lote ou ver o que falta exportar.
---

# Fechar um lote de arte

**Objetivo:** nenhuma peça paga fica sem registro, sem cópia ou com zip quebrado.

**Quando usar:** fim de geração ou export de um lote, antes do PR de arte e ao retomar um lote parado.

**Entradas:** `docs/ASSETS.md`, `docs/PROVENIENCIA.md`, `arte/tripo/` no checkout principal e `%USERPROFILE%\3D Objects\TinyAirport-BACKUP\`.

## Passos

1. Rode a auditoria, a partir da raiz do repositório:
   ```bash
   python client/tools/auditar_arte.py --sha
   ```
   Leva cerca de 20 s e só lê. Sem `--sha`, leva 2 s e não confere os hashes.
2. Trate cada linha do resultado:
   - `sem zip`: a peça está na lista e não foi exportada. Exporte; não gere de novo, porque isso cobra crédito.
   - `zip quebrado`: refaça o export do mesmo task id.
   - `sem registro`: acrescente a linha em `docs/PROVENIENCIA.md`, com task id, créditos, saldo e os dois SHA-256.
   - `sem copia`: copie o zip para o backup, no mesmo caminho relativo.
   - `sha`: o arquivo mudou depois do registro. Descubra qual está certo antes de mexer em qualquer um.
   - `fora da lista`: acrescente a peça em `docs/ASSETS.md` no mesmo PR.
3. Quando a saída for 0, abra o PR do lote com `docs/ASSETS.md`, `docs/PROVENIENCIA.md` e `CHANGELOG.md`.

**Permissões:** só leitura na arte; escrita nos 3 documentos. A cópia para o backup é local.

**Saídas:** o resumo da auditoria no corpo do PR e os documentos atualizados.

**Critério de aceitação:** `auditar_arte.py --sha` sai 0.

**Testes:** `python client/tools/auditar_arte.py --selftest`, que também roda no CI.

**Reversão:** a ferramenta não escreve na arte; desfaça as edições dos documentos pelo git.

**Proibido sem o aval do Vinicius:** gerar ou regerar fora do saldo autorizado, apagar arte crua e enviar arte para fora do PC. Arte crua, FBX e `.blend` nunca entram no git.
