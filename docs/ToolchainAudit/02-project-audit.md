# 02. Auditoria do projeto

Estado de `origin/main` em `0ea41f3`, 2026-10-30. Os branches pausados foram só lidos.

## O jogo

| Item | Situação | Fonte |
|---|---|---|
| Gênero | tycoon/idle de gestão de aeroporto, retrato | `README.md` |
| Plataformas | Android primeiro; iOS depende de macOS e conta Apple | `GDD.md` §3 |
| Modo | single-player, sem backend; D-06 prevê Cloud Run só se houver IAP | `GDD.md` |
| Loop | avião pousa, passageiros passam pelas estações, embarcam e o avião decola com a receita; o quadro de partidas cria a hora do rush | `README.md` |
| Fase | Fase B feita, menos o K2 com gente; Fase C com o núcleo mesclado e sem tela | `CONTRATO_LANCAMENTO.md` |
| Monetização | 2 rewarded simulados; nada de interstitial nem item aleatório pago | `Anuncios.cs` |
| Prazo | D-02 em aberto; a Play depende do D-U-N-S, previsto para 2026-11-25 | `GDD.md` |

## Arquitetura

Núcleo em C# puro separado da View, com passo fixo e RNG com semente. É uma arquitetura boa para este gênero e **não há motivo para mexer nela**.

| Assunto | Estado | Evidência |
|---|---|---|
| Núcleo sem Unity | **bom**: `noEngineReferences` ligado e nenhum `using Unity` | `checa_repo.py` |
| Duplicação e dívida | **boa**: 0 `TODO`/`FIXME` em código e ferramentas | `git grep` |
| Dados hardcoded | **parcial, aceitável**: os números ficam em `Tuning`, objeto injetado na `Sim`; é o que permite as varreduras do bot | `Defs.cs` |
| Textos | **bom**: 0 strings acentuadas na View fora de `Textos.cs` | `git grep` |
| Save | **bom**: envelope com SHA-256, schema, migração e escrita atômica | `SaveTests.cs` |
| Enums salvos | **corrigido nesta auditoria**: a regra "só crescem no fim" não tinha teste; agora tem `EnumsTests` | prova vermelha no relatório 07 |
| Tamanho dos arquivos | `Mundo.cs` tem 815 linhas, acima das ~600 da regra do estúdio; o `feat/view-tripo` já divide em 3 | `checa_repo.py` (aviso) |
| Módulos do Unity | `manifest.json` não tem `audio`, `animation`, `particlesystem` nem `physics`. O `viewcheck` compila contra todos os módulos, e o Unity não: o `TA.rsp` real tem 0 referências a esses módulos. Código com `AudioSource` passava no `quick` e quebrava no `full` | `Library/Bee/.../TA.rsp`; agora o `checa_repo.py` pega |

## Sistemas de gameplay

| Sistema | Estado |
|---|---|
| Fluxo de passageiros, filas, embarque, portão que fecha | feito e testado |
| Quadro de partidas com estado e barra | feito |
| Equipe limitada e reposicionável | feito; K2 com gente pendente |
| Pátio, turnaround, manutenção, hangar | núcleo feito, sem tela |
| Tripulação | núcleo feito, regra desligada (só visual) pelo portão §5.13 |
| 8 estações e 6 perfis | em `claude/fase-c-estacoes-perfis-ada843`, sem PR |
| Offline com teto de 2 h, cofre | feito, com ataques de relógio testados |
| Tutorial | passos 1 a 9 de 12 |
| Save e migração | feito |
| Áudio | **ausente**: sem módulo, sem clipe e sem `Audio.cs` |
| Campanha, metas, conquistas, passe | falta |

## Pipeline de arte

O funil é o maior gargalo do produto. Os números abaixo são contagens reais de 2026-10-30:

| Etapa | Peças |
|---|---|
| Zips HD gerados e exportados | 230 |
| Registradas com SHA no `PROVENIENCIA.md` | 182 registros (170 peças na lista) |
| Malhas decimadas para o Unity (`ArtLocal`, fora do git) | 32, só no branch `feat/view-tripo` |
| Na `main` | **0 malhas**; o jogo ainda é greybox |

Faltam ferramentas? Não. `malhas_ta.py`, `render_sprites.py` e `elenco_ta.py` já existem, sem PR. **O que falta é mesclar o `feat/view-tripo`**, e o caminho está escrito no `docs/CONTINUAR_ARTE.md`.

Validação do §7 do documento de auditoria:

| Critério | Estado |
|---|---|
| Escala, pivô, teto de triângulos, textura | tratados por peça no `malhas_ta.py` (branch), com `malhas.json` |
| Integridade do zip, registro, cópia | `auditar_arte.py` (novo) |
| LOD, instancing | não medidos; dependem da View com arte |
| Aparência na cena real | pendente: a primeira foto das malhas no Unity ainda não foi vista (`CONTINUAR_ARTE.md`) |

## Testes e automação

| Camada | Estado | Evidência |
|---|---|---|
| Núcleo no `dotnet test` | 70 testes verdes | relatório 07 |
| Cobertura do núcleo | **99,3% das linhas** | relatório 07 |
| EditMode no Unity | 70 de 74, com 4 `[Explicit]` fora | relatório 07 |
| Mutação | 9 de 9 travas (`mutacao.py`, medido na Fase C) | `fase-c-nucleo.md` |
| Bot de balanceamento | 200 sementes, antes só à mão; **agora no CI** | `balanco.yml` |
| Autoplay no build | `-autoplay 10` com 0 exceções | relatório 07 |
| PlayMode | ausente; `-bot`, `-tela` e `-shot` cobrem o fluxo | `Game.cs` |
| Testes da View | 0 automáticos além do autoplay; as fotos são à mão | `fotos_v01.sh` |

## Desempenho

Meta do GDD: p10 ≥ 30 FPS em Android de entrada, com 60 pessoas, 2 aviões e 6 veículos (K3 ampliado), sem crescimento de memória em 20 min.

**Nada disso foi medido ainda.** A medição exige a View com arte (`-perf`, no branch) e um aparelho. `medir_aparelho.sh`, `soak_emulador.sh` e `soak_relatorio.py` já existem no Forge Street e podem ser copiados.

## Segurança

Detalhes em `08-license-security-and-privacy.md`. Resumo: 0 segredos no histórico, 0 alertas, push protection ligado.

## Documentação

| Achado | Ação |
|---|---|
| `README.md` dizia "Ainda não há código" | corrigido nesta auditoria |
| `CLAUDE.md` dizia "Fase A: esqueleto vazio" | corrigido nesta auditoria |
| `client/tools/marca.py` fora do `MAPA_CODIGO.md` | corrigido; o `checa_repo.py` passa a cobrar |
