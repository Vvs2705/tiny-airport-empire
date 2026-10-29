# Instruções para o Claude — Tiny Airport Empire

Este repositório é **só do Tiny Airport Empire** (GitHub `Vvs2705/tiny-airport-empire`). Cada chat aberto aqui cuida **apenas deste jogo**.

Regras comuns dos jogos do estúdio (escopo, git, guardrails, processo, arte, testes), editadas num lugar só:
@~/.claude/estudio/REGRAS_JOGOS_ESTUDIO.md

Missão e autorizações permanentes deste jogo (o Vinicius mandou seguir em 2026-10-29; vale por cima das regras acima quando disser outra coisa):
@~/.claude/estudio/PROMPT_MESTRE_NOVOS_JOGOS.md

Se os arquivos acima não carregarem (outro PC), o mínimo é: só este jogo; mudança entra por PR na `main`; nada de item aleatório pago; anúncio sem `AD_ID`; um processo pesado por vez; a missão é levar o jogo até "só falta publicar".

## Deste jogo
- Código do jogo em `client/Assets/_TA/` (núcleo em C# puro em `Scripts/Core`, testável fora do Unity). Unity **6000.3.27f1**.
- Portão: `client/tools/verify.sh quick|full` (`full` antes de todo PR de jogo); mapa dos arquivos em `docs/MAPA_CODIGO.md`.

## Situação
- Estado: **projeto Unity autorizado** (D-01 aprovada em 2026-10-29 e ampliada pelo prompt mestre até "só falta publicar"; D-14 aprovada: Tripo no mesmo dia). Fase A: esqueleto vazio copiado do Forge Street e do Dungeon Courier, com o portão verde. Nome e appId (D-03) seguem pendentes: o `Setup.cs` usa um appId provisório só de desenvolvimento.
- Comece por: `README.md`, `docs/CONTRATO_V01.md` (o que a v0.1 entrega), `docs/CONTRATO_LANCAMENTO.md` (GDD × feito), `docs/GDD.md` (GDD mestre "Little Terminal": auditoria no §0.3, fases no §17 e decisões pendentes no §23.2), `docs/GDD_ORIGEM.md` (produto, revisão competitiva e lições do DC) e `docs/COMPETITIVO.md`.
