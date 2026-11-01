# Nome do jogo e nomes de companhias (D-03 e D-13)

Decidido em 2026-11-01, com a autorização do Vinicius para pesquisar o nome e renomear sozinho o que colide. Este documento é a fonte dos dois: GDD §0.4 e §23.2 só apontam para cá.

**Aviso.** Pesquisa em base pública de marcas não é parecer jurídico. Ela mostra o que já está publicado nessas bases na data indicada; não garante que o INPI ou outro escritório aceite o registro, nem cobre marca ainda não publicada, nome em uso sem registro ou semelhança que só um examinador enxerga. O INPI faz uma busca própria ao examinar o pedido. A busca abaixo é por nome, **sem filtro de classe**: se não há marca idêntica em nenhuma classe, não há nas classes 9 (software) e 41 (jogos e entretenimento). Pedir o registro (classes 9 e 41, no INPI) é decisão e custo do Vinicius; não foi feito.

## 1. D-03: "Little Terminal"

**Resultado: busca limpa. O nome fica "Little Terminal".** Nenhuma marca, jogo ou app com o nome idêntico nem com a grafia colada.

- Nome do produto: `Little Terminal` (`Setup.ProductName` e `ProjectSettings.asset`).
- appId: `br.com.vstack.littleterminal` (Android e Standalone). Irreversível depois de publicado.
- Nome do repositório, `TinyAirport.exe` e `TinyAirport-dev.apk` não mudam: são nomes internos. O que o jogador vê (logo, ícone, loja) já é "Little Terminal" (`ART_BIBLE.md` §5, `ASSETS.md`).

### Registro das buscas (2026-11-01)

Cada busca por controle: antes de aceitar um "zero", a mesma ferramenta foi testada com um nome que existe.

| # | Base | Termo e modo | Resultado | Link |
|---|---|---|---|---|
| 1 | Google Play, Brasil, pt-BR | `"little terminal"`, apps e jogos | "Nenhum resultado" | https://play.google.com/store/search?q=%22little%20terminal%22&c=apps&hl=pt_BR&gl=BR |
| 2 | Google Play, EUA, en | `little terminal` | cerca de 30 resultados, nenhum com esse nome: emuladores de terminal (Termux, Termius), "Game Terminal: Mini Games Hub" (Astrolab Builder), "Terminal Master - Bus Tycoon" (Supercent) | https://play.google.com/store/search?q=little%20terminal&c=apps&hl=en&gl=US |
| 3 | Google Play, EUA, en | `little terminal airport` | nenhum "Little Terminal"; só jogos de aeroporto de outros nomes (Idle Airport Empire, Airport Empire - Idle Tycoon, Airport Manager, Idle Airport Tycoon) | https://play.google.com/store/search?q=little%20terminal%20airport&c=apps&hl=en&gl=US |
| 4 | App Store, API pública de busca (iTunes Search), BR | `little terminal`, `littleterminal` | 5 resultados e 0 resultados; nenhum com esse nome (Zara, Shein, Bershka, ASOS e um jogo infantil de aeroporto) | https://itunes.apple.com/search?term=little+terminal&entity=software&country=br&limit=50 |
| 5 | App Store, mesma API, EUA | `little terminal` (50), `little terminal airport` (92) | as 50 lidas por inteiro, as 92 filtradas por expressão regular; nenhum nome nem vendedor com "little terminal" (só clientes SSH e emuladores) | https://itunes.apple.com/search?term=little+terminal&entity=software&country=us&limit=50 |
| 6 | App Store, mesma API, Reino Unido | `little terminal` (89) | nenhum nome nem vendedor com "little terminal" (filtro por expressão regular) | https://itunes.apple.com/search?term=little+terminal&entity=software&country=gb&limit=100 |
| 7 | TMview (EUIPO; reúne EUIPO, INPI Brasil, USPTO, UKIPO e mais de 70 escritórios) | nome da marca **contém** `little terminal` | **0 linhas** | https://www.tmdn.org/tmview/#/tmview/results?page=1&pageSize=30&criteria=C&basicSearch=little%20terminal |
| 8 | TMview | nome da marca contém `littleterminal` | **0 linhas** | https://www.tmdn.org/tmview/#/tmview/results?page=1&pageSize=30&criteria=C&basicSearch=littleterminal |
| 9 | TMview | modo parecido, `little terminal` | 80.693 linhas de ruído (qualquer marca com "terminal"); as primeiras são TERMINAL, TERMINAL+ (NetJets), BLOOMBERG TERMINAL; nenhuma "LITTLE TERMINAL" | https://www.tmdn.org/tmview/#/tmview/results?page=1&pageSize=30&criteria=F&basicSearch=little%20terminal |
| 10 | INPI Brasil, pePI (marcas, sessão anônima), tipo **radical** | `little terminal` | "Nenhum resultado foi encontrado" (base atualizada até 27/10/2026) | https://busca.inpi.gov.br/pePI/jsp/marcas/Pesquisa_classe_basica.jsp |
| 11 | INPI Brasil, mesmo formulário | `littleterminal` | "Nenhum resultado foi encontrado" | idem |
| 12 | WIPO Global Brand Database (89 fontes, 77 milhões de registros), expressão **exata** | `little terminal` | "No results found" | https://branddb.wipo.int/en/similarname |
| 13 | WIPO Global Brand Database, expressão exata | `littleterminal` | "No results found" | idem |
| 14 | USPTO Trademark Search, direto | `little terminal` | **inconclusivo** (ver abaixo) | https://tmsearch.uspto.gov |
| 15 | Web aberta (buscador), jogo e app | `"Little Terminal" game`; `"Little Terminal" airport tycoon OR idle OR simulator app` | nenhum jogo ou app com esse nome; só jogos de terminal de texto no itch.io e jogos de aeroporto de outros nomes | — |

**Controles (provam que a ferramenta devolve resultado quando existe):** TMview contém `bloomberg terminal` = 90 linhas; TMview contém `petrobras` = 1.753 linhas, com linhas do INPI Brasil; INPI radical `petrobras` = 355 processos; WIPO exata `bloomberg terminal` = 94 resultados.

**Limites que ficam abertos:**
- **USPTO direto (#14).** O campo de palavra do tmsearch tratou `little terminal` como só `terminal` (7.341 marcas vivas, nenhuma com "LITTLE") e o link direto com a busca não roda (o controle `bloomberg terminal` também deu "No results"). Não conto esse resultado como zero. A cobertura do USPTO vem do TMview e da WIPO, que devolvem linhas do USPTO (ex.: TIRMINAL, TERMINIL) e deram zero. Conferência manual no tmsearch, pelo campo "Field tag and Search builder", fica para o Vinicius ou o advogado, junto do pedido de registro.
- **EUIPO eSearch direto** não foi aberto: o TMview é o agregador do próprio EUIPO e cobre a mesma base.
- Semelhança sonora e visual (ex.: "Litle Terminal", "Little Terminals") só foi coberta pelo modo "contém" e pelo modo parecido do TMview; análise fonética completa é trabalho de advogado.
- Observação sem peso: no itch.io existe um jogo de nome "Tiny Terminal" (nome diferente, outro gênero).

### Chave de upload (fora do git)

- Arquivo: `%USERPROFILE%\.tiny-airport\littleterminal-upload.jks` (alias `upload`, RSA 2048, SHA384withRSA, validade 10.000 dias, `CN=Little Terminal Upload, O=V-STACK, C=BR`).
- Senha: gerada por script (32 caracteres aleatórios) e guardada só em `%USERPROFILE%\.tiny-airport\littleterminal-upload.pass`, ambos com permissão só para o usuário. Não está no repositório nem em log, e não foi mostrada na conversa.
- Impressão digital SHA-256 do certificado (pública; é o que a Play Console pede para conferir o envio): `83:09:78:3B:71:E5:56:36:A0:D2:F6:5C:5F:99:77:A9:CC:7A:CF:56:54:77:C2:20:FA:54:41:A0:AE:F7:AF:66`.
- **Faça cópia dos dois arquivos num lugar seguro e separado do PC.** Sem a chave de upload só se recupera o acesso pedindo reset à Google Play. Com a Play App Signing, a chave que assina o app para o jogador é da Google; esta é só a chave de envio.
- O `Setup.cs` ainda gera só APK de desenvolvimento (chave de debug do Unity). A assinatura de release com esta chave entra junto do build AAB.

## 2. D-13: companhias aéreas e aeroportos

### Companhias (GDD de origem §13)

As companhias não aparecem no código: o jogo ainda não mostra nome de companhia, só aeroporto e destino. Por isso a troca é só de nome, sem mexer em `Scripts/Core` nem em `id` de rota. Quando a arte pintar a faixa de cada uma e o texto citar a companhia, usar os nomes da coluna "Novo".

| Origem §13 | Por que sai | Novo | Faixa sugerida | WIPO, expressão exata |
|---|---|---|---|---|
| SkyFox | `SKYFOX` já é marca: 28 resultados, entre eles classe 9 registrada em Singapura (2025) e classe 42 | **Arara Air** | vermelho | 0 resultados |
| BlueWing | lembra a Blue Wing Airlines (Suriname), listada no GDD §0.4 | **Gaivota Air** | azul-claro | 0 resultados |
| Aurora Air | lembra a Aurora (companhia russa) e repetia o aeroporto Aurora Global | **Alvorada Air** | dourado | 0 resultados |
| SunJet | não estava na lista do §0.4 e não foi pesquisada isolada; trocada junto para a família ficar coerente | **Coral Jet** | coral (`#F07167`, paleta da Costa Azul) | 0 resultados |
| Atlas Cargo | lembra a Atlas Air (cargueira dos EUA) | **Capivara Cargo** | marrom-terra | 0 resultados |

Todos funcionam em pt-BR e em inglês (arara, gaivota, alvorada, coral e capivara são palavras conhecidas nos dois idiomas) e seguem o tom colorido do jogo. A checagem é a mesma de §1: WIPO Global Brand Database, expressão exata, 2026-11-01 (links no padrão `https://branddb.wipo.int/en/similarname`). É uma checagem por nome, não por classe, e não é parecer jurídico.

### Aeroportos (tabela `Tabelas.Aeroportos`)

Ficam como estão: **Vale Verde** (`vv`), **Costa Azul** (`ca`) e **Aurora Global** (`ag`).

- Nenhum é nome de aeroporto comercial com código IATA (busca na web, 2026-11-01). Existem pistas pequenas com nome parecido (Costa Azul, no Equador; Fazenda Vale Verde, no Brasil), que são nomes geográficos comuns e não marca de companhia.
- Dentro do jogo são nome de lugar, não nome de produto nem de companhia, então não passam pela busca de marca. Aurora Global só repetia o nome de "Aurora Air", que saiu.
- Se o Vinicius preferir tirar "Aurora Global", troca-se só `Nome` em `Rotas.cs` (o `id` `ag` não muda, porque save e testes dependem dele) e as menções em `ART_BIBLE.md`, `ASSETS.md` e `BALANCE.md`.
- As rotas usam nomes de cidades reais (Campinas, Salvador, Lisboa…): nome de lugar, sem marca.
