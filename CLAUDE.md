# Instruções para o Claude — Tiny Airport Empire

Este repositório é **só do Tiny Airport Empire** (GitHub `Vvs2705/tiny-airport-empire`). Cada chat aberto aqui cuida **apenas deste jogo**.

Regras comuns dos jogos do estúdio (escopo, git, guardrails, processo, arte, testes), editadas num lugar só:
@~/.claude/estudio/REGRAS_JOGOS_ESTUDIO.md

Missão e autorizações permanentes deste jogo (o Vinicius mandou seguir em 2026-10-29; vale por cima das regras acima quando disser outra coisa):
@~/.claude/estudio/PROMPT_MESTRE_NOVOS_JOGOS.md

Se os arquivos acima não carregarem (outro PC), o mínimo é: só este jogo; mudança entra por PR na `main`; nada de item aleatório pago; anúncio sem `AD_ID`; um processo pesado por vez; a missão é levar o jogo até "só falta publicar".

## Deste jogo
- Código do jogo em `client/Assets/_TA/` (núcleo em C# puro em `Scripts/Core`, testável fora do Unity). Unity **6000.3.27f1**.
- Portão: `client/tools/verify.sh quick|full|android` (`full` antes de todo PR de jogo); mapa dos arquivos em `docs/MAPA_CODIGO.md`.
- Ao fechar um lote de arte: skill `lote-de-arte`. Ferramentas avaliadas e rejeitadas: `docs/ToolchainAudit/04-tool-recommendations.md` (consulte antes de propor instalar algo).

## Situação
- Estado (2026-11-03): **jogo em desenvolvimento, `main` com 48 PRs** (D-01 aprovada em 2026-10-29 e ampliada pelo prompt mestre até "só falta publicar"; D-14 aprovada). Fase B feita, menos o K2 com gente; núcleo da Fase C inteiro e Fase D1 (Gems) na `main`; arte do Tripo no jogo, com as malhas fora do git. Nome decidido em 2026-11-01 (D-03): **Little Terminal**, appId `br.com.vstack.littleterminal`, busca de marca em `docs/NOME.md`; a chave de upload fica fora do git. Em 2026-11-03 o Vinicius aprovou misturar o GDD de direção (`docs/GDD_DIRECAO_v0.1.md`, só direção): o lançamento ganha as 9 frentes de gestão A a I (GDD §4.1), o resto é roteiro pós-lançamento (§4.4) e valem as seis regras de direção do §0.5. O que está em andamento, o que falta e o que está errado: `docs/ESTADO_DO_PROJETO.md`.
- Comece por: `README.md`, `docs/ESTADO_DO_PROJETO.md` (estado, em andamento, a fazer e a corrigir), `docs/CONTRATO_V01.md` (o que a v0.1 entrega), `docs/CONTRATO_LANCAMENTO.md` (GDD × feito), `docs/GDD.md` (GDD mestre "Little Terminal": auditoria histórica no §0.3, fases no §17, matriz no §21 e decisões no §23.2), `docs/GDD_ORIGEM.md` (produto, revisão competitiva e lições do DC) e `docs/COMPETITIVO.md`.
- Quem fecha um PR que muda uma linha do contrato atualiza, no mesmo PR, `docs/CONTRATO_LANCAMENTO.md` §1, o §21 do GDD e `docs/ESTADO_DO_PROJETO.md`.
