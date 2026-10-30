# 07. Linha de base de qualidade e desempenho

Medido em 2026-10-30 neste PC, com o Chrome do Tripo aberto e entre 1 e 2 GB de RAM livres. Logs em `client/Builds/verify/`, fora do git.

## Portão

| Medida | Antes da auditoria | Depois | Evidência |
|---|---|---|---|
| `verify.sh quick` em worktree novo | **falhava**, saída 1, em 3 s | **passa em 5 s** | "falta client/Library/..." → `verify quick: OK em 5 s` |
| `verify.sh full` | não medido nesta rodada | **65 s** em worktree novo, **37 s** com cache | ver abaixo |
| `verify.sh android` | não existia; **nenhum APK** no projeto | **passa em 147 s** | ver abaixo |
| Portões do bot | só à mão | no CI, em todo PR do núcleo | `balanco.yml` |
| `.meta` faltando | 6 | 0 | `checa_repo.py` |
| Arquivos fora do `MAPA_CODIGO.md` | 1 | 0 | `checa_repo.py` |

## Testes

| Camada | Resultado |
|---|---|
| `dotnet test` | **70 aprovados**, 0 falhas (eram 69; +`EnumsTests`) |
| Cobertura do núcleo | **99,3%** das linhas (1 037 de 1 044); 95,6% de linhas e 93,2% de ramos no total, incluindo os testes |
| EditMode no Unity | total 74, aprovados 70, falhas 0; os 4 restantes são `[Explicit]` |
| Viewcheck | 0 avisos, 0 erros |
| Selftests | `checa_repo` 23 quebras, `checa_apk` 6, `auditar_arte` 8 cenários |
| Prova vermelha do `EnumsTests` | trocar `Loja` e `Portao` derruba o teste; `Defs.cs` restaurado; a Fase C2 só cresce no fim e não quebra |
| Mutação (`mutacao.py`) | **9 de 9 mutantes mortos**, em 25 s, sobre o estado final |

A cobertura foi medida sem instalar nada: o `Microsoft.CodeCoverage` já vem com o `Microsoft.NET.Test.Sdk`.

```bash
dotnet test client/tools/coretests --collect:"Code Coverage;Format=cobertura"
```

## Portões do bot

Local, `FullyQualifiedName~BalanceTests`, 200 sementes:

| Portão | Meta | Medido | Tempo |
|---|---|---|---|
| TA-1 Bom | ≥ 85% | **99,3%** | 16 s |
| TA-1 Lento | ≥ 60% | **74,5%** | junto |
| TA-2 Bom / Parado | ≥ 1,10 | **1,248** | junto |
| §5.13 Planejador / Automático | ≥ 1,10 | **1,151** | 1 min 47 s |
| Varredura e "antes" (medições, fora do CI) | — | 2 min 45 s | — |

## Build Windows

| Medida | Valor |
|---|---|
| Resultado | Succeeded, 0 erros |
| Tamanho | 147 MB (140 MiB) |
| `-autoplay 10` | OK, 0 exceções; embarque Bom 99,0%, Lento 74,1%; ganho de mover 1,249; retomada igual |

## Build Android (primeiro do projeto)

Build de desenvolvimento, greybox, sem arte nem áudio. **Não representa o tamanho final.**

| Medida | Valor | Meta |
|---|---|---|
| `targetSdk` | **36** | ≥ 36 desde 2026-08-31 |
| ABI | só `arm64-v8a` | 64 bits |
| `AD_ID` | ausente | ausente |
| 6 `.so` | PT_LOAD alinhados a ≥ 16 KB | exigido a partir de 2027-02-01 |
| Zip | alinhado a 16 KB (`zipalign -c -P 16 4`) | idem |
| APK | **19,4 MiB** | abaixo do limite da Play |
| Tempo | 147 s | — |
| Permissões | `INTERNET`, mais a de receptor gerada pelo Android | revisar ao fechar os SDKs |

O APK não foi instalado em aparelho. Não medi o que o `Auto` daria: fixei em 36 para não depender do SDK instalado.

## Arte

`auditar_arte.py --sha`, cerca de 20 s para os dois conjuntos de zips, já com os Lotes 8 a 10 na `main`:

| Medida | Valor |
|---|---|
| Peças na lista | 236 |
| Zips HD em disco | 236 |
| Registros com SHA | 248 |
| Sem zip / zip quebrado / sem registro / sem cópia / SHA divergente | **0 / 0 / 0 / 0 / 0** |
| Fora da lista | 0 |

Na primeira rodada, a ferramenta acusou 60 zips sem registro. Eram os Lotes 8 a 10, cujos documentos entraram na `main` pelo PR #17 durante esta auditoria; com eles, o número foi a 0. A primeira rodada também mostrou uma limitação do parser, que só lia tabelas: os Lotes 9 e 10 listam as peças em prosa. Foi corrigido, com caso no selftest.

## Análise estática

Analisadores do SDK sobre `coretests`, em uma rodada sem alterar o repositório: 366 CA1051 e 146 CA1707, quase tudo convenção de nome e campo público, que é o estilo do núcleo. Sem achado de defeito, então não foi adotado no portão.

## Bugs conhecidos

| Item | Estado |
|---|---|
| `Art/Marca` sem `.meta` | corrigido |
| Mecânico e despachante sem folha (`elenco_ta.py` morreu duas vezes sem log) | aberto em `feat/view-tripo`; sintoma compatível com falta de RAM (hipótese) |
| Git local com `garbage found` e `.idx` sem `.pack` em `.git` | sem impacto: `git fsck --connectivity-only` limpo |

## Não medido

| Medida | Motivo | Rótulo |
|---|---|---|
| FPS no aparelho (K3) | sem View com arte; sem "pode testar" | NÃO EXECUTADO |
| PSS e 20 minutos sem vazamento | idem | NÃO EXECUTADO |
| FPS no PC | sem valor para a meta mobile | NÃO EXECUTADO |
| Android de entrada | não há aparelho | BLOQUEADO |
| AAB | `Setup.BuildAndroidBundle` ainda não existe | NÃO EXECUTADO |
| Fotos 540×960 | esta auditoria não mudou a tela | NÃO EXECUTADO |
| K2 com pessoas | playtest externo | BLOQUEADO |
