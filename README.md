# Tiny Airport Empire

**Estilo:** idle/tycoon de gestão de aeroporto, retrato.

**O que é:** você começa numa pista rural e constrói uma rede global de aeroportos.

**Como funciona:** o avião pousa, os passageiros passam por check-in, raio-x, bagagem e lojas, embarcam e o avião decola com a receita. Um quadro de partidas com horário cria a hora do rush; você move a equipe entre as estações, resolve filas, automatiza com funcionários, constrói terminais, hangares e lojas e abre novos aeroportos.

**Como vai ser jogar:** começar fazendo tudo à mão e terminar comandando um império que funciona sozinho.

## Situação
Fora desta rodada; o nome colide com outros apps (sugestão da revisão: "Little Terminal", falta checar marca). Ainda não há código: este repositório guarda o GDD e a estrutura do projeto, pronta para o desenvolvimento começar. A auditoria de 2026-10-29 deu **NO-GO** para o prazo de 29/10 (GDD §0.3).

- **GDD mestre (Little Terminal, v1.1):** [`docs/GDD.md`](docs/GDD.md): produção, segurança e publicação; decisões pendentes no §23.2.
- **GDD de origem:** [`docs/GDD_ORIGEM.md`](docs/GDD_ORIGEM.md) (produto, revisão competitiva de 2026-10-09 e lições do Dungeon Courier).
- **Comparativo com jogos similares e diferenciais:** [`docs/COMPETITIVO.md`](docs/COMPETITIVO.md).

## Estrutura do repositório
| Pasta | Para quê |
|---|---|
| `docs/` | GDD, comparativo e, depois, balance, validações e contratos de cada fase |
| `client/` | projeto Unity 6000.3.27f1 LTS, Android primeiro, retrato; código do jogo em `client/Assets/_TA/` |
| `client/Assets/_TA/Scripts/Core/` | núcleo em C# puro (regras, simulação, save), testável fora do Unity |
| `client/Assets/_TA/Scripts/View/` | MonoBehaviours, UI, câmera, entrada |
| `client/Assets/_TA/Editor/` | setup do projeto e builds por linha de comando |
| `client/Assets/_TA/Tests/EditMode/` | testes NUnit do núcleo |
| `client/Assets/_TA/Resources/` | sprites e dados carregados em tempo de execução |
| `client/tools/` | ferramentas fora do Unity (testes `dotnet`, scripts de arte, relatório de playtest) |
| `arte/` | arte-fonte; `arte/tripo/` e `arte/mixamo/` ficam fora do git |

Como contribuir: [`CONTRIBUTING.md`](CONTRIBUTING.md). Histórico: [`CHANGELOG.md`](CHANGELOG.md).

## Repositório e licença
© V-STACK / Vinicius Souza. **Todos os direitos reservados.** Código, documentos e arte visíveis para acompanhamento, sem licença de uso, cópia ou redistribuição.
