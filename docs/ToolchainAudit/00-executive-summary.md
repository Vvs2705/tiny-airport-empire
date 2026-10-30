# 00. Resumo executivo

Auditoria de ferramentas do Tiny Airport Empire ("Little Terminal"), 2026-10-30, a partir de `0ea41f3`. Procedimento: `CLAUDE_GAMEDEV_TOOLCHAIN_AUDIT.md`.

## Conclusão

**O projeto não precisa de ferramenta nova agora.** A base é boa: núcleo com 99,3% de cobertura, 84 testes, bot de balanceamento e portão em um comando. Os gargalos reais se resolveram com scripts em stdlib e com o que já estava instalado. **Nada foi baixado, instalado ou pago.**

O maior gargalo não é de ferramenta: **236 peças de arte geradas, 0 na `main`**. O trabalho está em `feat/view-tripo`, pausado.

## Maiores gargalos

| # | Gargalo | Evidência | Situação |
|---|---|---|---|
| 1 | Arte não chega ao jogo | 236 zips, 32 malhas num branch, 0 na `main` | depende de retomar a frente |
| 2 | Requisitos da Play nunca medidos | nenhum APK no projeto; targetSdk em `Auto` | **resolvido**: APK conferido, targetSdk 36 fixo |
| 3 | Portões que dependiam de lembrar | bot só à mão; enums sem trava; `.meta` faltando | **resolvido**: CI, `EnumsTests`, `checa_repo.py` |
| 4 | Portão quebrado em worktree novo | `verify.sh quick` saía 1 em toda sessão nova | **resolvido** |
| 5 | Ausências que travam a loja | sem áudio, fonte padrão, K3 nunca medido | fila priorizada em `09-next-actions.md` |

## Prioridades

1. Mesclar `feat/view-tripo`.
2. Telas da Fase C e da C2: o núcleo já está na `main`.
3. Áudio por reuso do DC, com o teste do motor de avião.
4. Fonte OFL.
5. Medir K3 no aparelho.

## Bloqueios

| Bloqueio | Quem resolve |
|---|---|
| Patch do Unity para as falhas do Brotli (CVSS 9,2) e da eMudhra está "Pending" para 6000.3. O jogo não usa Brotli e nada foi publicado, mas **o AAB da loja precisa sair de um Editor corrigido** | Unity, depois aval para atualizar |
| Medição em aparelho | "pode testar" e um Android de entrada |
| Git, Node e .NET deste PC abaixo das versões corrigidas do Brotli, falha que a CISA lista como explorada | Vinicius: é instalação global |

## O que a auditoria mudou

Nada na lógica do jogo. Mudanças:

- 3 ferramentas novas em stdlib;
- `verify.sh android`;
- 1 workflow novo e 1 endurecido;
- 1 teste;
- 1 skill;
- 7 `.meta`;
- targetSdk fixo em 36 no `Setup.cs`, com o efeito no `ProjectSettings.asset`.

## Correções de premissa

- **GPU:** o PC tem RTX 3050 de 6 GB. O limite real é a RAM de 7,7 GB.
- **Telemetria do MCP do Blender:** em chat, esta auditoria chegou a dizer que era só opt-in; a leitura completa do código mostrou que um registro anônimo mínimo sai mesmo sem consentimento. Nenhum conteúdo sai. Detalhes em `08-license-security-and-privacy.md`.
- **GameCI:** parecia a maior oportunidade, porque o Unity pesaria no PC. O `verify.sh full` mediu 65 s, e o CI nem veria a arte. Foi adiado.

## O que não se aplica

Multiplayer, backend, NPC com LLM, reinforcement learning, testes web e pixel art. Motivos em `03-capability-gap-matrix.md`.

## Mapa dos relatórios

| Arquivo | Conteúdo |
|---|---|
| `01-environment-inventory.md` | sistema, hardware, motor, ferramentas, Skills e MCPs |
| `02-project-audit.md` | arquitetura, gameplay, arte, testes, desempenho |
| `03-capability-gap-matrix.md` | capacidades, evidências, lacunas |
| `04-tool-recommendations.md` | preço, licença, risco, pontuação e decisão |
| `05-installation-plan.md` | o que foi feito e o passo a passo do que depende do Vinicius |
| `06-skills-and-automation.md` | scripts, CI, Skills e MCPs |
| `07-quality-and-performance-baseline.md` | números medidos |
| `08-license-security-and-privacy.md` | segredos, falhas, dados e licenças |
| `09-next-actions.md` | ações em ordem |
