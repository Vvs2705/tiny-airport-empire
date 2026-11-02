# Tiny Airport Empire

**Estilo:** idle/tycoon de gestão de aeroporto, retrato.

**O que é:** você começa numa pista rural e constrói uma rede global de aeroportos.

**Como funciona:** o avião pousa, os passageiros passam por check-in, raio-x, bagagem e lojas, embarcam e o avião decola com a receita. Um quadro de partidas com horário cria a hora do rush; você move a equipe entre as estações, resolve filas, automatiza com funcionários, constrói terminais, hangares e lojas e abre novos aeroportos.

**Como vai ser jogar:** começar fazendo tudo à mão e terminar comandando um império que funciona sozinho.

## Situação
Em desenvolvimento. O jogo se chama **Little Terminal** (nome decidido em 2026-11-01, appId `br.com.vstack.littleterminal`, busca de marca em [`docs/NOME.md`](docs/NOME.md)); o repositório e os arquivos internos seguem Tiny Airport Empire. A `main` tem 44 PRs: o núcleo das Fases B e C e as Gems (Fase D1) estão prontos e testados, e o jogo roda em 3D com a arte do Tripo, cujas malhas ficam fora do git por licença. Faltam as telas de aeroportos, rotas, metas, passe e loja, o servidor de compras, os anúncios reais, as 20 histórias, o inglês, o áudio ligado e a medida em aparelho. O envio depende da Play Console (previsão 2026-11-25). Cerca de 57% do contrato de lançamento está cumprido em 2026-11-02. Ferramentas e lacunas: [`docs/ToolchainAudit/`](docs/ToolchainAudit/00-executive-summary.md).

- **Estado do projeto, para quem chega agora:** [`docs/ESTADO_DO_PROJETO.md`](docs/ESTADO_DO_PROJETO.md): o que é, como compilar e testar, o que está feito, em andamento, a fazer e a corrigir, e o que depende do dono.
- **GDD mestre (Little Terminal, v1.4):** [`docs/GDD.md`](docs/GDD.md): produção, segurança e publicação; decisões no §23.2 e divergências entre código e texto no §26.
- **Contrato de lançamento:** [`docs/CONTRATO_LANCAMENTO.md`](docs/CONTRATO_LANCAMENTO.md): cada item do lançamento × o que está feito, com o PR que prova.
- **GDD de origem:** [`docs/GDD_ORIGEM.md`](docs/GDD_ORIGEM.md) (produto, revisão competitiva de 2026-10-09 e lições do Dungeon Courier).
- **Comparativo com jogos similares e diferenciais:** [`docs/COMPETITIVO.md`](docs/COMPETITIVO.md).

Compilar e testar (Windows, Git Bash, Unity 6000.3.27f1): `client/tools/verify.sh quick` no dia a dia e `full` antes de um PR de jogo; detalhes no estado do projeto, seção 2.

## Estrutura do repositório
| Pasta | Para quê |
|---|---|
| `docs/` | GDD, estado do projeto, contrato de lançamento, contratos de cada fase, balanço, comparativo e proveniência da arte |
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
