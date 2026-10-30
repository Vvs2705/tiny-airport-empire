# 04. Recomendações de ferramentas

Pesquisa feita em 2026-10-30, só em leitura. Preço e licença: **V** = lido na fonte oficial, **P** = fonte secundária ou trecho, **N** = não verificado. Teto do orçamento: US$ 10. Nada aqui autoriza gasto.

**Conclusão:** o projeto não precisa instalar nenhuma ferramenta nova agora. Os gargalos se resolveram com scripts em stdlib e com o que já está no PC. Nada foi baixado.

## Integrado nesta auditoria, sem instalar nada

| Solução | Lacuna | Preço | Licença | Risco |
|---|---|---|---|---|
| `client/tools/checa_repo.py` | `.meta`, GUID, módulos, guardrails, segredos, mapa | US$ 0 | do projeto | baixo |
| `client/tools/auditar_arte.py` | lote sem registro, sem cópia ou corrompido | US$ 0 | do projeto | baixo; só lê |
| `client/tools/checa_apk.py` | requisitos da Play nunca medidos | US$ 0 | do projeto | baixo |
| `.github/workflows/balanco.yml` | portões do bot só à mão | US$ 0: runner grátis em repo público (V) | do projeto | baixo |
| `EnumsTests` | regra de save sem trava | US$ 0 | do projeto | baixo |

## Pontuação

De 0 a 5: relevância, ganho de tempo, qualidade, compatibilidade, manutenção, segurança, custo, integração e remoção. Total sobre 45.

| Candidata | Rel | Tem | Qua | Com | Man | Seg | Cus | Int | Rem | Total | Decisão |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Scripts stdlib desta auditoria | 5 | 4 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | **44** | **feito** |
| Fixar e atualizar `mcp-for-blender` | 4 | 2 | 3 | 5 | 4 | 4 | 5 | 5 | 5 | **37** | recomendar |
| GitHub ruleset e Dependabot alerts | 3 | 2 | 4 | 5 | 5 | 5 | 5 | 5 | 5 | **39** | recomendar |
| `audio_gen.py` do DC (reuso) | 5 | 4 | 3 | 5 | 4 | 5 | 5 | 4 | 5 | **40** | reuso na Fase C |
| Ferramentas do Forge (`medir_aparelho`, `soak_*`, `placeholder_gate`) | 4 | 4 | 4 | 4 | 4 | 5 | 5 | 4 | 5 | **39** | reuso com a View |
| Unity Profile Analyzer | 2 | 2 | 3 | 4 | 4 | 5 | 5 | 4 | 5 | 34 | adiar |
| RenderDoc | 2 | 1 | 3 | 4 | 5 | 4 | 5 | 2 | 5 | 31 | adiar |
| Stryker.NET | 2 | 1 | 3 | 4 | 5 | 5 | 5 | 3 | 5 | 33 | adiar |
| GameCI | 2 | 1 | 3 | 2 | 4 | 2 | 5 | 2 | 4 | 25 | adiar |
| Coplay unity-mcp | 2 | 2 | 2 | 3 | 4 | 1 | 5 | 2 | 4 | 25 | rejeitar |
| IvanMurzak Unity-MCP | 1 | 1 | 2 | 0 | 4 | 0 | 5 | 1 | 3 | 17 | rejeitar |
| ComfyUI / Krita AI | 1 | 1 | 2 | 1 | 4 | 3 | 5 | 1 | 4 | 22 | rejeitar |
| TRELLIS.2 / Hunyuan3D-2.1 | 0 | 0 | 2 | 0 | 2 | 3 | 5 | 0 | 4 | 16 | rejeitar |
| Claude-Code-Game-Studios | 1 | 1 | 1 | 2 | 3 | 1 | 5 | 1 | 2 | 17 | rejeitar |
| gitleaks + pre-commit | 1 | 1 | 2 | 5 | 5 | 5 | 5 | 3 | 5 | 32 | rejeitar: redundante |

## Recomendadas

### 1. Fixar a versão do MCP do Blender, atualizar o addon e desligar a telemetria

- **Por quê:** o `uvx` sem versão pega a mais nova a cada abertura, e isso já deixou o addon no protocolo 9 de 15. Também desliga o registro mínimo que hoje sai.
- **Link:** https://github.com/ahujasid/blender-mcp
- **Preço e licença:** US$ 0; MIT (V).
- **Compatibilidade:** Blender 5.2.1, testado ao vivo.
- **Risco:** baixo. Mexe na configuração global do Claude, que vale para todos os projetos; por isso depende do Vinicius.

### 2. GitHub: ruleset na `main` e Dependabot alerts

- **Por quê:** hoje a `main` não tem proteção; "merge com CI verde" depende só de disciplina. Dependabot está desligado.
- **Link:** https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets
- **Preço:** US$ 0 em repo público pela página de preços (V). A página "About rulesets" ainda diz Team/Enterprise: contradição da própria GitHub, que só se confirma ao abrir a tela. Alertas do Dependabot grátis: P.
- **Risco:** baixo. Em conta pessoal não há lista de bypass (P).

### 3. Áudio: reuso, antes de qualquer gerador

- **Por quê:** o GDD §12 define a rota: `audio_gen.py` do DC, 1 852 linhas, com numpy e scipy, que já estão instalados.
- **Plano B para o motor de avião:** Kenney, CC0 (V), https://kenney.nl/support. Plano C: ElevenLabs.
- **ElevenLabs:** https://elevenlabs.io/pricing. Starter a US$ 1 ou US$ 6 por mês (a própria página se contradiz; V). Efeitos: usar plano pago, porque os Termos dizem que o grátis é não comercial e a página de SFX diz o contrário (V). **Música gerada: não usar por enquanto**, porque os planos self-serve excluem "Studio Games", termo sem definição na página (V/N).
- **Rejeitado:** AudioCraft, com pesos CC-BY-NC, não comerciais (V); ACE-Step 1.5, com licença dos pesos não verificada (N).

### 4. Fonte

Lilita One e Nunito do DC, mais uma mono OFL para o quadro de partidas (GDD §10.3). Custo zero; licença OFL a registrar junto da fonte, como manda o prompt mestre §7.

## Adiadas, com o gatilho

| Ferramenta | Link | Preço e licença | Gatilho |
|---|---|---|---|
| GameCI | https://game.ci/docs/github/activation | US$ 0, MIT (V). Exige `UNITY_LICENSE`, `UNITY_EMAIL` e `UNITY_PASSWORD`. Ativação Personal no 6000.3 em 2026: N | O PC não conseguir rodar o `full`. Hoje ele leva 65 s, e o CI não veria a arte, que fica fora do git. A imagem Android de 8,75 GB aperta o SSD de 14 GB do runner |
| Stryker.NET | https://github.com/stryker-mutator/stryker-net | US$ 0, Apache-2.0 (V) | Núcleo crescer e a cobertura de 99,3% esconder teste fraco |
| Unity Profile Analyzer | https://docs.unity3d.com/Packages/com.unity.performance.profile-analyzer@1.2/manual/index.html | US$ 0 (V) | K3 falhar no aparelho |
| RenderDoc | https://github.com/baldurk/renderdoc | US$ 0, MIT (V) | Problema de GPU no Android |
| Android vitals, Crashlytics, Sentry | https://sentry.io/pricing/ | Vitals: no Play Console. Crashlytics: "No-cost" (V). Sentry Developer: US$ 0, 5 mil erros, 1 usuário (V) | Decisão D-11. Vitals primeiro, sem SDK |
| EL4CTEO/blender-skills | https://github.com/EL4CTEO/blender-skills | US$ 0, MIT (V) | Os scripts headless deixarem de bastar. Grava 22 skills no global |

## Rejeitadas

| Ferramenta | Motivo |
|---|---|
| Coplay unity-mcp | A cena é gerada por código e `Main.unity` é vazia, então sobra pouco para editar. Exige Editor aberto (P) num PC de 7,7 GB, tem `execute_code`, telemetria padrão ligada, e entra por Git ou OpenUPM enquanto a Unity pede evitar scoped registries até sair o patch do Brotli (V) |
| IvanMurzak Unity-MCP | "Project path cannot contain spaces", e este caminho tem. Exige Docker; autenticação padrão `none`; roda C# por reflection (V) |
| ComfyUI, Krita AI, InvokeAI | O gerador de imagem do Tripo não cobra nesta conta. ComfyUI pede 8 GB de RAM, mais do que o PC tem |
| TRELLIS.2 | ≥ 24 GB de VRAM, testado em Linux (V) |
| Hunyuan3D-2.1 | ~29 GB de VRAM; licença exclui UE, Reino Unido e Coreia do Sul (V) |
| TripoSR | ~6 GB de VRAM, no limite; o Tripo pago já cobre |
| gitleaks, pre-commit | Secret scanning e push protection já ligados (VERIFICADO no repositório), mais o `checa_repo.py` |
| Git LFS | Instalado e sem uso; 12,3 MB de sprites não justificam. Cota: 10 GiB + 10 GiB/mês (V) |
| meshoptimizer, glTFast | O pipeline é FBX pelo Blender |
| Firebase Test Lab | **Depreciado**: desliga em 2027-09-30 (V) |
| Aseprite | US$ 19,99, acima do teto, e o jogo não é pixel art (V) |
| PixelLab | Preço N; não é pixel art |
| Cascadeur | Indie a US$ 19/mês, acima do teto (V); D-18 usa sprites |
| FMOD, Wwise | Áudio do Unity basta; preços N |
| Claude-Code-Game-Studios | 49 agentes, 74 skills e 15 hooks, com `.claude/settings.json` próprio e `update-check.sh` com rede não documentada (V). Colide com os hooks e o CLAUDE.md atuais |
| wshobson/agents | 202 agentes, nenhum de jogo (V). Mais contexto, nenhum ganho |
| Roo Code | **Arquivado** desde 2026-05-15 (V) |
| Aider, Cline, OpenCode, Gemini CLI, Codex | O Claude Code resolve; outro agente só dividiria o trabalho. Aider sem release desde 2025-08 (V). Gemini CLI: o uso individual grátis migrou para outro produto (V, com contradição interna) |
| ML-Agents, Ollama, LangGraph, Gymnasium, PettingZoo, Convai, Inworld | Bot determinístico testado; LLM em NPC só traria custo e falha |
| Nakama, Colyseus, FishNet, Mirror, Netcode, Agones, Redis, PlayFab | Single-player |
| Penpot, Material Maker | HUD por código. Poly Haven e ambientCG são CC0 (V) e ficam à mão, sem instalar nada, se o terreno pedir |

## O que não foi possível verificar

- **Tripo:** preços, termos e direitos depois que o plano vence. As páginas deram HTTP 403. Pelos trechos da ajuda oficial, plano pago tem direitos comerciais plenos e saídas fora de treino (P).
- **Mixamo:** texto vigente da Adobe (403). Uso comercial em jogo sem royalties e proibição de distribuir arquivos brutos vêm do FAQ (P).
- **Google Play:** prazo de API 37 em 2027, ainda não publicado.
- FMOD e Wwise: valores só por fontes secundárias.
- Compatibilidade explícita de Sentry, Firebase Unity SDK e Memory Profiler com Unity 6.3.
