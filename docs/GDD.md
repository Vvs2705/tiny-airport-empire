# LITTLE TERMINAL
## GDD Mestre de Produção, Tecnologia, Segurança e Publicação

**Versão:** 1.1, revisão de 29/10/2026 sobre a 1.0 de 26/10/2026. O que mudou e por quê está no §26.
**Projeto:** Tiny Airport Empire (repositório `Vvs2705/tiny-airport-empire`).
**GDD de origem:** [`GDD_ORIGEM.md`](GDD_ORIGEM.md), que reúne o produto, a revisão competitiva de 09/10/2026 e as lições do Dungeon Courier de 16/10/2026. Neste documento ele aparece citado como "Origem §N".
**Nome comercial recomendado:** Little Terminal, provisório até a busca de marca no INPI, USPTO e EUIPO e a busca exata nas duas lojas (Origem §52).
**Motor:** Unity **6000.3.27f1 LTS**, a mesma versão do Dungeon Courier desde 22/10/2026. Fica fixa quando o projeto for criado.
**Plataformas:** Android primeiro. O iOS depende de macOS e de conta Apple (§0.3).
**Orientação:** retrato.
**Modelo comercial:** Free-to-Play, com anúncios recompensados opcionais. Compras digitais só entram com validação no servidor (§7.4).
**Prazo operacional da v1.0:** 29/10/2026. **É inviável:** a auditoria do §0.3 deu **NO-GO**. A nova data é a decisão D-02 (§23.2).
**Status:** especificação de produto. D-01 aprovada em 29/10/2026: Fases A e B autorizadas (§23.2). A Fase A cria só o esqueleto vazio em `client/` (§17.2); **nenhuma funcionalidade do jogo está implementada**, e não há arte nem conta de loja para este jogo.

> **Regra de verdade:** este GDD descreve o produto que deve existir. Uma funcionalidade só pode ser marcada como concluída depois de estar implementada, testada no projeto real e registrada com evidência. Este documento, sozinho, não comprova que qualquer funcionalidade já exista.

> **Precedência:** as regras do estúdio (`~/.claude/estudio/REGRAS_JOGOS_ESTUDIO.md`) e o `CLAUDE.md` do repositório valem por cima deste documento. Este documento vale por cima do GDD de origem. O que ele não cobre continua valendo da Origem: tutorial passo a passo, rota de arte, clipes de animação, flags de teste e invariantes. Os conflitos entre os três estão resolvidos ou apontados no §0.4.

---

# 0. DECISÃO EXECUTIVA E REGRA DO PRAZO

O objetivo é entregar um **jogo completo, coerente e pronto para submissão**, não um protótipo disfarçado de lançamento.

O produto deve ter loop principal, progressão, aeroportos, rotas, campanhas, passe de temporada, anúncios, compras, artes, áudio, salvamento, privacidade, segurança, telemetria, testes e materiais de publicação.

Existe, porém, uma restrição objetiva: entre 26/10/2026 e 29/10/2026 há aproximadamente 72 horas. Isso só é plausível como prazo de *release candidate* se uma parte substancial do projeto Unity já existir e estiver funcional, e se contas, certificados, IDs de produtos, SDKs, backend e páginas das lojas já estiverem configurados.

Se esses pré-requisitos não existirem, o agente deve reportar o bloqueio com evidências. Não pode fingir que foram resolvidos nem remover segurança para cumprir a data. **A auditoria de 29/10 (§0.3) mostrou que eles não existem.**

## 0.1. O que significa "pronto"

1. Build de release reproduzível, sem erros conhecidos bloqueadores.
2. Testes obrigatórios executados e relatório anexado.
3. Compras e anúncios testados em ambiente de teste, sem transações reais de teste.
4. Segurança, política de privacidade e declarações das lojas conferidas.
5. Artes, ícone, screenshots, descrição, peças de marketing e links legais preparados.
6. Pacote Android AAB assinado e pronto para submissão.
7. Build iOS arquivada e pronta para envio, caso certificados, assinatura, conta e ambiente de build estejam válidos. **O ambiente exige macOS com Xcode, e este PC roda Windows 10 (§0.3).**
8. Lista explícita de dependências externas que impeçam a publicação imediata.

**Pronto para submeter não significa aprovado e disponível ao público.** Google Play e App Store podem revisar o envio, solicitar alterações ou atrasar a publicação. Não podemos garantir a aprovação das lojas em uma data específica.

## 0.2. Não reduzir o escopo silenciosamente

O jogo completo continua sendo o objetivo. Se uma funcionalidade obrigatória não estiver pronta, registrar:

- Estado: `BLOQUEADO` (depende de algo externo) ou `NÃO INICIADO` (auditado, ainda não existe).
- Motivo.
- Evidência.
- Impacto no lançamento.
- Dependências.
- Plano de correção.
- Decisão necessária do responsável pelo produto.

Não remover testes, validação de compras, proteção de dados ou controles de segurança para aparentar conclusão.

O que a Origem previa e este GDD **não** põe no lançamento está listado no §4.3, com o motivo. Nada sai do escopo sem aparecer ali.

## 0.3. Auditoria de 29/10/2026: resultado NO-GO para 29/10

Primeira auditoria feita pelo Claude no commit `59f0bb9` da `main`, conforme o §1.

| Pré-requisito do §0 | Achado | Evidência |
|---|---|---|
| Projeto Unity funcional | **Não existe.** `client/` só tem pastas vazias com `.gitkeep` | `git ls-files` no `59f0bb9`; `client/README.md` diz "Ainda vazio" |
| Código, testes, cenas, assets | **Nenhum** | idem |
| Autorização para começar o projeto | **Pendente.** O `CLAUDE.md` diz para não começar o projeto Unity sem o Vinicius pedir neste grupo | `CLAUDE.md`, seção Situação |
| Jogo na rodada de produção | **NO-GO nesta rodada** (veredito de 2026-10-06): o jogo só entra se o slot idle validar | Origem, topo e §51 |
| Conta Google Play | A Play Console de organização da V-STACK espera o D-U-N-S grátis, com **previsão para 2026-11-25**. A mesma conta vai publicar este jogo | memória do estúdio, STATUS do Dungeon Courier; Origem, "Play e lançamento" |
| Build e conta iOS | Não há macOS neste PC (Windows 10, 7,7 GB). A conta Apple não foi verificada; conta de organização também exige D-U-N-S | ambiente do PC; `CLAUDE.md` global |
| Nome e appId | **Não decididos.** A busca de marca não foi feita | Origem §52 |
| Backend de validação de compras | **Não existe** para este jogo | — |
| Rede de anúncios | A conta LevelPlay da V-STACK está pronta; o app deste jogo ainda não existe (depende do nome) | STATUS do Dungeon Courier |
| Arte | Nenhum lote do Tripo aprovado para este jogo | Origem, "Arte e animação" |

**Veredito: `NO-GO` para 29/10/2026.** O próprio §17.3 manda tornar isso visível no primeiro relatório e pedir decisão sobre o prazo. A matriz do §21 tem o estado de cada entrega. O plano realista, com portões em vez de datas, está no §17.3.

Mesmo com o código pronto, o envio Android só é possível depois de a Play Console existir (previsão 2026-11-25), e o iOS só com macOS e conta Apple.

## 0.4. Reconciliação com as regras do estúdio e com o GDD de origem

| Tema | v1.0 colada | Regra do estúdio ou Origem | Como fica nesta versão |
|---|---|---|---|
| Moedas | Cash + Gems + XP + pontos de temporada | 1 moeda no MVP (estúdio; Origem §8) | **v0.1 só com Cash.** Gems só no lançamento e só se D-05 aprovar IAP com validação (§6.1). XP e pontos de temporada são barras de progresso, não moedas: não se compram nem se gastam |
| Rede de anúncios | AdMob, UMP, ATT, SSV | LevelPlay na conta V-STACK; anúncio não personalizado para todos, sem `AD_ID` (estúdio; Origem, Monetização) | **LevelPlay, não personalizado, sem `AD_ID`.** Sem rastreamento, não há pedido de ATT. A recompensa por anúncio nunca dá Gems nem direito pago, então o callback local basta; SSV só quando houver backend (§7.2) |
| Interstitial | nenhum no lançamento | desligado na v0.1 (estúdio) | **Nenhum.** Resolve o conflito 2 da Origem: o §26 da Origem ("poucos, em transições") deixa de valer |
| Item aleatório pago | proibido | proibido (estúdio; ECA Digital, Lei 15.211/2025, art. 20) | igual; a base legal foi acrescentada (§7.1, §9.2) |
| Offline | 2 h | 2 h (estúdio). A Origem §25 ainda falava em até 8 h com upgrades | **2 h como trava no código;** upgrade sobe a taxa, não o teto. Resolve o conflito 1 da Origem |
| Avião raro, temporada com gems | não tem raridade | conflito 3 da Origem | **Resolvido:** avião e variante saem por marco, rota ou companhia; o premium da temporada é uma lista fixa e visível |
| Perder um voo | perde receita e, "quando aplicável", reputação | invariante da Origem §50: nunca custa mais que a receita daquele voo | Na progressão principal, só a receita do voo. Reputação só cai em desafio que anuncia essa regra antes de começar (§5.4) |
| Histórias de passageiros | 20, cada uma com efeito | TA-3: 1 por voo, álbum, "100+" | **20 no lançamento**, cada uma com efeito; mais histórias por temporada (§5.9) |
| Idiomas | pt-BR; inglês opcional | 4 idiomas no DC; coorte paga BR/PH/ID (Origem §51) | **Decisão D-08.** A coorte BR/PH/ID precisa no mínimo de inglês |
| Companhias fictícias | sem marca real | Origem §13: SkyFox, BlueWing, Aurora Air, SunJet, Atlas Cargo | **Checar antes de usar.** Possíveis colisões com companhias reais: Atlas Cargo × Atlas Air (cargueira dos EUA), Aurora Air × Aurora (russa), BlueWing × Blue Wing Airlines (Suriname). "Aurora Air" também repete o nome do aeroporto Aurora Global (D-13) |
| Arquitetura | 20 serviços com nome | núcleo em C# puro (`TA.Core`), View separada, cópia do Forge e do DC | Os serviços viram arquivos do padrão do estúdio (tabela do §13); cada um nasce quando for usado |
| Testes | unidade + PlayMode | `coretests` (dotnet), bot `-autoplay`, EditMode, `verify.sh` | Mesmos casos, rodando no portão do estúdio (§16.0) |
| Créditos do Tripo | "usar os créditos existentes" | só com aval explícito do Vinicius, por lote | **Aval por lote,** com saldo, prompt e SHA em `docs/PROVENIENCIA.md` (§10.2) |
| Plano de datas | 26/10 a 29/10 | — | substituído por fases com portões (§17.3) |

---

# 1. COMO USAR ESTE DOCUMENTO

Este GDD é a fonte central de verdade do produto.

O Claude deve:

1. Auditar o repositório, a cena inicial, pacotes, versão do Unity, configurações de build, serviços, assets, testes e estado de publicação.
2. Comparar o que existe com este GDD e gerar uma matriz `REQUISITO → ESTADO → ARQUIVO/COMPONENTE → TESTE → EVIDÊNCIA`. A matriz detalhada mora em `docs/CONTRATO_LANCAMENTO.md`, como manda o processo do estúdio; o resumo fica no §21.
3. Preservar funcionalidades existentes que estejam corretas; não reescrever o projeto inteiro por preferência pessoal.
4. Implementar em incrementos pequenos, executando testes após cada grupo de mudanças.
5. Usar as CLIs realmente disponíveis. Descobrir comandos com `--help` e documentação local; não inventar comandos, parâmetros ou nomes de serviços.
6. Usar o Tripo3D em lotes planejados, **cada lote com aval explícito do Vinicius**, priorizando assets necessários ao lançamento e evitando gerar várias versões quase iguais sem decisão de arte.
7. Não registrar, imprimir, copiar para logs ou commitar chaves, tokens, certificados privados, tokens de compra reais, dados de usuários ou segredos. **O repositório é público.**
8. Manter um diário de execução com mudança, arquivos afetados, comando executado, resultado, falhas e próximo passo. O diário é o STATUS do jogo na memória do estúdio, com "CONTINUAR DAQUI" a cada pausa.
9. Nunca afirmar "testado", "seguro", "publicado" ou "pronto" sem evidência verificável.
10. Ao fim de cada fase, produzir um resumo com `CONCLUÍDO`, `FALHOU` e `BLOQUEADO`.
11. Seguir o processo do estúdio, nesta ordem: decisões fechadas (§23.2), contrato da versão (`docs/CONTRATO_V01.md`), núcleo em C# puro com testes e bot, portão `client/tools/verify.sh quick|full` e `docs/MAPA_CODIGO.md`.

## 1.1. Autoridade e ações irreversíveis

O agente pode usar os acessos técnicos já autorizados para inspecionar o projeto, gerar assets, implementar, executar testes e criar builds.

Antes de uma ação irreversível ou que possa gerar cobrança, deve:

- confirmar o impacto;
- verificar se há backup;
- evitar gastos de créditos além do saldo/limite autorizado;
- não publicar uma versão de produção, apagar dados de produção, trocar credenciais, alterar titularidade de contas ou mudar configurações financeiras sem aprovação explícita do responsável.

Mudança entra por PR na `main`, nunca por push direto. Merge, tags e Releases só com aval do Vinicius. Senha, token e dado de pagamento são sempre digitados por ele.

---

# 2. VISÃO DO PRODUTO

## 2.1. Pitch

**Você não administra apenas um aeroporto. Você precisa fazer cada voo decolar a tempo.**

Mova sua equipe, resolva gargalos, escolha quais voos salvar, construa rotas lucrativas e transforme uma pequena pista em um grande hub internacional.

## 2.2. Fantasia do jogador

O jogador começa com um aeroporto regional simples e o transforma em uma rede de hubs.

Ele deve sentir que suas decisões melhoram o fluxo operacional: funcionários mudam de estação, filas diminuem, passageiros embarcam, aviões decolam, novas rotas são abertas e o terminal evolui visualmente.

## 2.3. Posicionamento

O jogo é um **tycoon operacional acessível com decisões táticas**, não apenas um idle game de clicar em upgrades.

A diferenciação vem da combinação abaixo. Cada item corresponde a um diferencial da Origem §50, onde estão o custo, a prova barata e o risco:

| Diferencial | Origem |
|---|---|
| quadro de partidas com prazos visíveis | TA-1 |
| equipe limitada que o jogador reposiciona durante as ondas | TA-2 |
| histórias curtas de passageiros que afetam prioridades | TA-3 |
| incidentes anunciados com antecedência | TA-4 (frente fria) |
| monetização transparente, sem loot boxes pagas e sem anúncios intersticiais forçados | TA-5 |
| rotas com perfis e trade-offs | TA-6 |
| hub que acelera o próximo aeroporto | TA-7 |
| progressão visual dos aeroportos; campanhas e desafios que alteram decisões | Origem §3 |

## 2.4. Público-alvo

- Pessoas que gostam de tycoon, gestão, idle e otimização.
- Jogadores mobile que preferem sessões de 3–12 minutos, mas querem metas de longo prazo.
- Público casual e intermediário.
- Jogadores que gostam de observar sistemas, resolver gargalos e otimizar recursos.

A classificação etária e o público-alvo definitivos devem ser escolhidos após revisar o conteúdo e as políticas das lojas (D-10). Não declarar que o jogo é feito para crianças apenas por ter estética colorida. **Mas também não ignorar o ECA Digital:** um jogo colorido de gestão é "de acesso provável" por crianças e adolescentes, e a lei veda loot boxes nesse caso (art. 20). As regras do estúdio (nada aleatório pago, anúncio não personalizado, sem chat) já atendem a esse ponto; o resto do texto da lei precisa de revisão jurídica.

## 2.5. Pilares de design

1. **Decisão legível:** o jogador entende o problema, as opções e a consequência.
2. **Crescimento visível:** melhorias aparecem no aeroporto, nos aviões, na equipe e no fluxo.
3. **Automação conquistada:** contratar e melhorar a equipe reduz microtarefas, mas não remove todas as decisões.
4. **Tensão justa:** horários e incidentes criam urgência; nunca punir sem aviso claro.
5. **Progresso honesto:** compras aceleram ou personalizam, mas não escondem conteúdo essencial atrás de sorteio pago.
6. **Sessões respeitosas:** o jogador pode sair, voltar e receber progresso offline limitado.
7. **Performance mobile:** sprites, pooling e sistemas simples; evitar simulação física desnecessária de dezenas de passageiros.

---

# 3. PLATAFORMAS, IDIOMA E LINGUAGEM DE IMPLEMENTAÇÃO

## 3.1. Plataformas

- Android: plataforma prioritária para a primeira submissão. AAB por `Setup.BuildAndroidBundle`, IL2CPP ARM64. No DC, em 10/2026, foram conferidos targetSdk 36, bibliotecas nativas alinhadas a 16 KB e Google Play Billing 9; esses três itens precisam ser reconferidos no dia do envio.
- iOS: deve compartilhar o mesmo design, economia, conteúdo e regras. O Unity gera um projeto Xcode, e arquivar e enviar exige macOS. Caminhos possíveis: um Mac, um runner macOS de CI ou o Unity Build Automation, além da conta Apple Developer (D-04). Até isso existir, o iOS fica `BLOQUEADO`.
- Orientação retrato.
- Controles por toque, com áreas interativas confortáveis.
- Compatibilidade com dispositivos de desempenho baixo e intermediário. A régua é um aparelho Android **de entrada**: o POCO F4 do Vinicius se limita depois de 1 minuto e não serve de referência (Origem, Android).

Antes do envio, confirmar na documentação oficial e no console de cada loja o nível de API e os requisitos de build exigidos naquele momento. Não presumir que configurações antigas continuam válidas.

## 3.2. Linguagem e stack

**Linguagem principal do jogo: C# no Unity.**

Não introduzir uma segunda linguagem para a lógica de gameplay.

- **C#:** sistemas de jogo, UI, economia, progressão, dados, integração com SDKs, testes automatizados e ferramentas do Unity Editor.
- **Núcleo em C# puro** (`TA.Core`, em `client/Assets/_TA/Scripts/Core/`), sem `UnityEngine`. É testável por `dotnet test` em segundos e copiado do Forge Street (Origem §53).
- **Dados de conteúdo:** tabelas em C# no núcleo (`Defs`/`Tuning`, como no Forge e no DC) ou JSON versionado. ScriptableObject só onde o Editor precisar. O núcleo não pode depender de asset do Unity.
- **Unity Editor scripting em C#:** importação, validação de assets, geração de catálogos e verificações de build (`Editor/Setup.cs`, `Editor/SpriteImport.cs` do DC).
- **Python 3.x ou scripts de shell/PowerShell:** somente para automação externa já disponível, como validar nomes, dimensões, arquivos, hashes, manifestos e pipeline (as ferramentas do DC em `client/tools/`). Não usar Python como runtime do jogo.
- **SDKs:** Unity IAP 5.4.3 (o mesmo do DC) e LevelPlay (Ads Mediation). Não há Unity Gaming Services ativo; analytics é a decisão D-11.
- **Backend:** não existe para este jogo. Para validar compras, a opção recomendada é um serviço pequeno no Cloud Run, em projeto próprio, na região southamerica-east1, com a conta de serviço autorizada na Play Console e sem chave JSON (o modelo de deploy do FiscWise). A alternativa é uma Supabase Edge Function. A escolha é a decisão D-06.
- Se os serviços não estiverem configurados, não introduzir uma infraestrutura complexa sem avaliar prazo, custo, dependências e riscos.
- **Versão do Unity:** 6000.3.27f1 LTS, fixa. Não atualizar editor ou pacotes principais no prazo final sem necessidade comprovada, backup e testes de regressão.
- Fixar versões de pacotes e remover SDKs não utilizados.

## 3.3. Regras de arquitetura

- Separar lógica de jogo, UI, dados, persistência, anúncios, compras e analytics.
- O núcleo nunca lê o relógio do aparelho: recebe `nowUnix` de fora, e o quadro de partidas corre no passo fixo, com semente (Origem, Código e testes).
- UI não altera saldo diretamente: solicita uma ação a um serviço de domínio.
- Nenhuma compra concede conteúdo apenas porque o botão foi tocado ou porque o cliente enviou `success=true`.
- Nenhuma recompensa de anúncio é concedida apenas por callback local quando houver suporte a verificação server-side **e a recompensa for de alto valor** (definição no §7.2).
- Configurações de equilíbrio ficam fora da lógica rígida sempre que isso não aumentar o risco.
- Todas as ações econômicas relevantes geram um evento de auditoria sem dados pessoais desnecessários.
- Toda operação econômica deve ser idempotente: processar duas vezes o mesmo identificador não duplica a recompensa. Toda concessão tem id de transação (invariante da Origem §50).
- Evitar dependências circulares e singletons globais difíceis de testar.
- Salvar dados com versão de schema e migração explícita.

---

# 4. ESCOPO DE LANÇAMENTO: JOGO COMPLETO

Este é o conteúdo planejado para a versão de lançamento. Não é uma lista de ideias futuras.

## 4.1. Conteúdo obrigatório

- 3 aeroportos com identidade visual e desafios diferentes.
- 12 rotas desbloqueáveis, distribuídas por distância, frequência, receita e risco operacional.
- 8 variantes visuais de aeronaves, reutilizando famílias de assets quando adequado.
- 8 estações operacionais:
  1. Check-in.
  2. Segurança.
  3. Bagagem.
  4. Portão/embarque.
  5. Loja.
  6. Alimentação.
  7. Manutenção.
  8. Despacho/controle de pista.
- 6 perfis de funcionários, com especializações claras.
- Equipe limitada e reposicionável entre estações.
- Quadro de partidas com horários e estados.
- Fluxo de passageiros, filas, embarque, partida e perda de receita por voo perdido.
- 4 incidentes operacionais com aviso prévio e solução compreensível.
- 20 micro-histórias de passageiros integradas a objetivos ou escolhas.
- Campanha principal com 30 objetivos, 10 por aeroporto.
- Missões diárias e semanais.
- 20 conquistas permanentes.
- Passe de temporada com trilha gratuita e premium, 30 níveis e recompensas definidas antes da publicação. A trilha premium depende de IAP (D-05).
- Loja de compras digitais com catálogo fixo e preços fornecidos pela loja da plataforma (D-05).
- Anúncios recompensados opcionais.
- Sem anúncios intersticiais forçados na versão inicial.
- Salvamento local, migração de schema, restauração de compras e sincronização em nuvem se o serviço estiver configurado.
- Progressão offline limitada a 2 horas.
- Tutorial contextual (os 12 passos da Origem, "Onboarding").
- Configurações, áudio, acessibilidade básica, política de privacidade, suporte e opção de exclusão de conta/dados quando aplicável.
- Telemetria de eventos essenciais, crash reporting e painel mínimo de saúde.
- Arte final, UI, ícone, screenshots e materiais de loja sem placeholders.

**Regra de execução:** a lista define o produto, mas o prazo não substitui a auditoria. O agente deve medir o que existe, reutilizar o que estiver pronto e apontar os itens impossíveis de concluir com evidência, em vez de marcá-los como concluídos sem implementação.

## 4.2. Versões: a v0.1 vem antes do lançamento

O processo do estúdio exige um contrato da v0.1 antes do código. A v0.1 é o greybox que prova os diferenciais com o bot e com gente. Ela é um subconjunto do §4.1, não um produto à parte.

| | v0.1 (`docs/CONTRATO_V01.md`) | Lançamento (§4.1) |
|---|---|---|
| Aeroportos | Vale Verde | 3 |
| Estações | check-in, segurança, portão, loja | 8 |
| Quadro de partidas | 2 voos por onda | completo |
| Equipe | 3 funcionários móveis | 6 perfis |
| Moeda | Cash | Cash (+ Gems se D-05/D-07 aprovarem) |
| Offline | 2 h, 2× opcional por anúncio | igual |
| Rewarded | 2× offline, 2× receita por 5 min | igual (§7.2) |
| Compras | nenhuma | se D-05 aprovar |
| Conteúdo | tutorial passos 1–9, 1 história | completo |
| Arte | greybox procedural; depois Lote 1 | Lotes 1 e 2 |
| Portão de saída | TA-1: ≥ 85% de embarque com o bot bom e ≥ 60% com o lento; TA-2: mover rende ≥ 10%; K2 medido com gente (Origem §51) | §25 |

Se o TA-2 não render ≥ 10% no bot, o diferencial é falso, e o design volta para revisão antes de qualquer arte.

## 4.3. O que a Origem previa e fica fora do lançamento

Nada aqui foi cortado em silêncio. Cada item volta como atualização, se a decisão mudar.

| Origem | Item | Por que fica fora |
|---|---|---|
| §15 | aeroportos 4, 5 e 6+ | o lançamento tem 3 aeroportos (§5.10) |
| §16 | Prestige com reset | substituído pelo hub do TA-7: o progresso viaja sem reset |
| §17 | árvore de Research Points | segunda moeda e mais um sistema; os upgrades de estação cobrem a progressão |
| §8 | Reputation como moeda e Event Currency | o estúdio exige 1 moeda; reputação vira barra de progresso (§6.1) |
| §11 | gerentes por raridade | viram os 6 perfis fixos do §5.5, sem sorteio (TA-5); a Lara é a personagem do tutorial |
| §12, §28 | raridade de avião e álbum de coleção | ficam as 8 variantes por marco ou rota, sem raridade (conflito 3) |
| §22, §23 | eventos (Rush Hour, Air Show, Cargo Week, Holiday) e Event Airport | o lançamento tem os 4 incidentes do §5.8 e a campanha sazonal do §6.5 |
| §27 | boosters pagos | só o bônus por anúncio do §7.2 e, se houver Gems, o booster do §6.1 |
| §37 | social e ranking | sem chat e sem ranking no lançamento (§9.1) |

---

# 5. LOOP DE JOGO E SISTEMAS PRINCIPAIS

## 5.1. Loop tático — 1 a 3 minutos

1. Uma onda de passageiros chega.
2. O quadro de partidas mostra voos, horário limite, destino, valor estimado e estado.
3. O jogador observa filas e escolhe prioridades.
4. O jogador move um ou mais funcionários para estações críticas.
5. Passageiros avançam pelas estações; o jogador pode comprar uma melhoria ou realocar a equipe.
6. Incidentes anunciados alteram temporariamente a operação.
7. O portão fecha; passageiros embarcam ou perdem o voo.
8. O avião decola e o jogo mostra receita, eficiência, atrasos e uma recomendação de melhoria.

## 5.2. Loop de gestão — 3 a 10 minutos

- Investir em capacidade, velocidade, automação e serviços.
- Contratar ou especializar funcionários.
- Escolher rotas, frequência e aeronaves adequadas.
- Resolver gargalos sem transformar tudo em uma sequência de upgrades automáticos.
- Completar objetivos da campanha, missões e conquistas.

## 5.3. Loop de longo prazo

- Desbloquear aeroportos e rotas.
- Melhorar reputação operacional.
- Completar a campanha e os conjuntos de histórias.
- Ganhar recompensas de temporada.
- Construir um hub que acelera a expansão do próximo aeroporto (TA-7: o aeroporto certificado manda passageiros de conexão para o novo; meta do bot: o 2º aeroporto chega ≥ 20% mais rápido).
- Personalizar aeronaves, equipe e terminal com cosméticos fixos.

## 5.4. Quadro de partidas

Cada voo contém:

- ID estável;
- origem/destino;
- hora de abertura e fechamento do portão;
- tamanho da onda de passageiros;
- capacidade de assentos;
- receita base e bônus de pontualidade;
- perfil de risco;
- estado: previsto, check-in, segurança, embarque, embarcado, decolado, perdido ou cancelado por incidente.

Regras:

- O horário precisa ser legível e acompanhado por uma barra ou indicador visual.
- O horário corre em tempo de jogo, no passo fixo do núcleo e com semente, nunca no relógio do aparelho. Assim o voo perdido é reproduzível no teste, no bot e no `-record`.
- Avisos críticos começam com antecedência suficiente para o jogador reagir.
- Perder um voo reduz a receita prevista daquele voo e nada mais (invariante da Origem §50). Não confiscar moedas existentes nem impor penalidades ocultas. Perda de reputação só existe em desafio que mostra essa regra antes de começar.
- Voo nenhum se perde enquanto o jogo está fechado ou em segundo plano.
- O primeiro tutorial ensina a salvar um voo sem depender de texto longo.
- Deve haver velocidade normal e aceleração controlada; a aceleração não pode quebrar temporizadores, economia ou anúncios.

## 5.5. Equipe móvel

Cada funcionário tem:

- velocidade base;
- capacidade/eficiência;
- especialidade principal;
- custo de contratação/manutenção;
- nível de melhoria;
- feedback visual e ícone da especialidade.

O jogador começa com 3 funcionários. Pode arrastar funcionários entre estações durante a onda. Ao mover alguém, a estação de origem perde capacidade temporariamente e a de destino ganha. O boneco anda até lá em 2–3 s.

Os 6 perfis são fixos e nomeados, sem traço sorteado (TA-5). Saem por marco da campanha, nunca por sorteio nem só por compra. Proposta inicial, a validar no contrato:

| Perfil | Especialidade |
|---|---|
| Atendente | check-in e portão mais rápidos |
| Agente de segurança | segurança |
| Bagageiro | bagagem |
| Vendedor | loja e alimentação (aumenta o gasto por passageiro) |
| Mecânico | manutenção; resolve a falha de equipamento (§5.8) |
| Despachante | despacho; reduz o efeito da frente fria |

Requisitos:

- arrastar sem bloquear a visão do problema;
- área de toque de pelo menos 48 dp em volta do sprite, maior que o boneco;
- sprite/indicador elevado acima do dedo durante o arrasto;
- destino válido destacado;
- feedback de sucesso ou falha;
- limiar separado para tocar e para arrastar;
- tutorial curto e pulável após demonstrar a ação;
- botão alternativo acessível para escolher estação, caso o arrasto seja difícil;
- não obrigar o jogador a mover funcionários a cada segundo.

## 5.6. Estações e filas

Cada estação contém:

- capacidade máxima;
- tempo de processamento;
- número de funcionários;
- nível de melhoria;
- fila atual;
- estado visual: normal, atenção ou crítico;
- efeito do upgrade claramente explicado.

A fila deve ser legível. Não basta pintar uma estação de vermelho: o jogo deve mostrar qual voo está em risco e por que determinada decisão é útil. Estado crítico também se mostra por ícone e texto, nunca só por cor (§11.2).

## 5.7. Rotas e aeronaves

As rotas variam por:

- distância/tempo;
- volume e frequência de passageiros;
- receita;
- necessidade de serviços;
- risco operacional;
- compatibilidade com aeronaves.

Arquétipos:

- **Curta e frequente:** menor receita por voo, recuperação rápida e demanda previsível.
- **Longa e lucrativa:** mais receita, maior volume e janela operacional mais exigente.
- **Turística:** picos sazonais e passageiros com maior gasto em lojas.
- **Conexão de hub:** recompensa por fluxo eficiente e pontualidade.

Distribuição proposta das 12 rotas: Vale Verde com 4 curtas; Costa Azul com 3 turísticas e 1 conexão; Aurora Global com 2 longas e 2 conexões. Os destinos podem ser cidades reais do Brasil (TA-6, por exemplo Campinas × Manaus): nome de cidade não é marca. Meta do bot: nenhuma rota domina.

Aeronaves: 3 silhuetas (regional, narrow-body, wide-body) e 8 variantes por pintura, sem forma nem pintura de fabricante ou companhia real.

Não usar marcas, logotipos, pinturas ou nomes de companhias reais sem licença. As companhias do jogo serão fictícias, e cada nome passa por busca antes de entrar no jogo (§0.4, D-13).

## 5.8. Incidentes

Quatro tipos no lançamento:

1. **Frente fria:** aviso prévio; pista reduzida ou fechada por tempo definido. Número inicial do TA-4: pista fechada por 60 s, com aviso de 30 s.
2. **Falha de equipamento:** estação perde eficiência; enviar funcionário especializado ou pagar reparo **com Cash** (nunca com Gems nem com anúncio).
3. **Pico de demanda:** chegada concentrada; reposicionar equipe.
4. **Atraso de conexão:** grupo de passageiros precisa de prioridade dentro de uma janela.

Regras de justiça:

- Todo incidente comunica causa, duração aproximada e opções.
- Nada pode apagar uma compra ou saldo.
- Incidentes aleatórios não podem criar uma derrota inevitável. O sorteio usa a semente do núcleo, então todo incidente é reproduzível.
- Não iniciar incidentes que exigem reação enquanto o jogo está em segundo plano.
- Cada incidente precisa de teste de determinismo e regressão.

## 5.9. Histórias de passageiros

Criar 20 histórias curtas, com uma ou duas frases, usando arte e ícones reutilizáveis (retratos pintados, Origem "Arte e animação").

Exemplos de temas:

- casamento;
- entrevista de emprego;
- visita familiar;
- competição esportiva;
- conexão internacional.

As histórias devem mudar uma decisão, uma prioridade, uma recompensa ou uma missão. Não podem ser apenas texto colecionável sem efeito. Embarcar o passageiro a tempo guarda a história no álbum "histórias entregues" (TA-3).

A escolha da história é por sequência determinística, sem repetir na semana. O texto fica na tabela de idiomas (§11.3), com nome e horário por `{0}`.

Não usar sofrimento real de forma manipuladora para incentivar compras ou anúncios. Nenhuma história oferece anúncio ou compra para "salvar" o passageiro.

## 5.10. Progressão dos três aeroportos

### Aeroporto 1 — Vale Verde (regional)

- Ensina fluxo, equipe móvel, quadro de partidas e lojas.
- 10 objetivos.
- Rotas curtas.
- Primeira melhoria visual do terminal.

### Aeroporto 2 — Costa Azul (turístico)

- Picos de demanda, alimentação, manutenção e incidentes.
- 10 objetivos.
- Rotas turísticas e conexões.
- Maior variedade de aeronaves e funcionários.

### Aeroporto 3 — Aurora Global (hub internacional)

- Conexões, ondas de passageiros, rotas longas e gestão avançada.
- 10 objetivos.
- Exige combinar equipe, upgrades, rotas e calendário de voos.
- Conclusão da campanha principal e desbloqueio de metas de maestria.

Cada aeroporto tem mapa e paleta próprios, mas reutiliza módulos, materiais e animações para manter consistência e desempenho. Os três nomes também passam pela busca de marca do D-13.

---

# 6. ECONOMIA, RECOMPENSAS E EQUILÍBRIO

## 6.1. Moedas e progresso

- **Cash:** moeda principal, obtida por voos, serviços e missões; usada em melhorias e contratação. É a única moeda da v0.1.
- **Gems:** moeda premium, obtida por metas claras e comprada por IAP; não expira. **Só existe no lançamento se D-05 aprovar IAP com validação e D-07 aprovar a moeda.** O offline nunca dá Gems, e anúncio também não.
  - Para que servem as Gems (a v1.0 não dizia). Proposta: cosméticos da loja, pacote de Cash dimensionado pela renda atual e booster de 2× receita por 30 min. Nenhum desses é necessário para a campanha, e não há ranking em que o booster conte.
- **XP de gestão/reputação:** barra de progresso da conta e desbloqueio de conteúdo; não é uma moeda comprável nem gastável.
- **Pontos de temporada:** barra de progresso obtida por missões da temporada; reinicia conforme as regras exibidas; não se compra.

Não criar uma moeda para cada sistema. Toda moeda precisa ter função visível e um sumidouro de economia.

## 6.2. Regras de equilíbrio

- Nenhuma rota deve dominar todas as outras; cada uma precisa de trade-off.
- Nenhuma melhoria pode ser obrigatória para vencer uma missão normal se existir alternativa razoável.
- Não vender poder exclusivo necessário para concluir a campanha.
- Não permitir que uma compra faça a economia entrar em overflow, valores negativos ou `NaN`.
- Limitar valores e validar todas as operações de saldo.
- Usar inteiros ou valores decimais controlados para recompensas; não depender de comparações frágeis de float para validar propriedade de item.
- Alterações de preço e balanceamento precisam ser versionadas e testadas em saves antigos.
- O balanceamento sai do bot (`-autoplay` e `client/tools/econsim`, perfis F2P, Slow e AdWatcher) e é registrado em `docs/BALANCE.md`. A duração real se mede com gente: no Forge, a pessoa foi 1,7–3,7× mais lenta que o bot.

## 6.3. Progresso offline

- Limite padrão: **2 horas** de rendimento offline, como **trava no código**. O Tuning só pode encurtar esse teto.
- Mostrar o intervalo considerado e o cálculo da recompensa.
- Recompensa offline não deve degradar a satisfação por ausência. Offline nunca perde voo nem satisfação.
- Um anúncio opcional pode dobrar a recompensa daquela coleta uma vez por retorno, se estiver disponível e o jogador consentir.
- Upgrades podem melhorar a taxa dentro do limite, não criar punição por não abrir o app.
- Tratar o relógio do dispositivo como dado não confiável. Aplicar limites e detecção; nunca permitir valores negativos, ganhos ilimitados ou repetição infinita de recompensa. Concretamente: Δt ≤ 0 paga 0, há um teto relativo à renda e cada coleta tem um id de transação (`ApplyOffline` do Forge).

## 6.4. Passe de temporada

- Uma temporada inicial completa de **28 dias**.
- 30 níveis de progresso.
- Trilha gratuita com recompensas regulares.
- Trilha premium desbloqueada por compra específica da temporada; não é assinatura com renovação automática. **Sem IAP aprovado (D-05), a temporada sai só com a trilha gratuita.**
- Recompensas fixas e exibidas antes da compra.
- Nenhuma caixa aleatória paga.
- O jogador pode visualizar todas as recompensas e os pontos necessários antes de comprar.
- Quem compra o premium no meio da temporada recebe na hora todas as recompensas premium dos níveis que já alcançou.
- A tela de compra mostra os dias restantes e quantos níveis ainda dá para alcançar no ritmo de quem joga todo dia. Proposta: a venda do premium fecha nas últimas 72 h, para ninguém comprar algo que não dá mais tempo de aproveitar.
- O passe premium não pode ser vendido como necessário para concluir a campanha principal.
- Regras de expiração, prazo para resgatar recompensas e conteúdo mantido após a temporada devem aparecer no jogo e na página de compra.
- Recomendação: manter o painel de recompensas resgatáveis por 7 dias após o fim da temporada; depois, as recompensas não resgatadas seguem a regra mostrada antes da compra.
- Produto da loja com ID exclusivo e estável por temporada.
- Testar compra pendente, cancelada, concluída, restaurada, reembolso e reabertura do app durante a transação.
- Início e fim em horário UTC fixo no build, mostrados no fuso do jogador. O relógio do aparelho não é confiável: voltar o relógio não reabre a temporada nem repete recompensa.

**Ritmo proposto (HIPÓTESE, a validar no bot):** 150 pontos por nível, 4 500 no total. Fontes em 28 dias: 3 diárias × 40 pontos, 5 semanais × 150 e 10 objetivos sazonais × 100, num máximo de 7 360 pontos. Quem joga 5 dias por semana chega a cerca de 5 400 e fecha o passe; quem joga 3 dias por semana chega perto do nível 19. A tabela completa de recompensas por nível fica em `docs/TEMPORADA_S1.md`, pronta antes da Fase C terminar.

## 6.5. Missões e campanhas

**Campanha principal:** 30 objetivos, 10 por aeroporto, listados em `docs/CAMPANHA.md` antes da Fase C.

Cada objetivo tem:

- contexto narrativo curto;
- condições de conclusão verificáveis;
- recompensa definida;
- pré-requisitos;
- tela de sucesso;
- teste automatizado da regra.

**Missões diárias:** 3 por dia, escolhidas de um conjunto balanceado. Exemplos: concluir voos pontualmente, realocar equipe, melhorar uma estação, operar uma rota. O dia vira às 04:00 no fuso gravado (`Metas.cs` do Forge).

**Missões semanais:** 5 por semana, exigindo combinações de sistemas, sem obrigar compra ou visualização de anúncio.

**Conquistas:** 20 permanentes. Não devem depender de uma oferta paga ou de um anúncio específico.

**Campanha sazonal:** objetivos adicionais ligados ao passe e ao evento, sem substituir o progresso principal. Deve ter início, fim, recompensas e comportamento de retorno documentados.

---

# 7. MONETIZAÇÃO: ANÚNCIOS E COMPRAS

## 7.1. Princípios comerciais

- Compra voluntária, preço transparente e conteúdo entregue exatamente como descrito.
- Sem loot boxes pagas, roletas pagas ou probabilidades ocultas. É regra do estúdio e, no Brasil, lei: o ECA Digital (Lei 15.211/2025, em vigor desde 17/03/2026) veda caixas de recompensa em jogos direcionados a crianças e adolescentes ou de acesso provável por eles (art. 20).
- Sem intersticiais forçados na versão inicial.
- Sem banners em áreas de toque frequente.
- Sem anúncios que pareçam botões de gameplay.
- Nenhum anúncio obrigatório para concluir a campanha principal.
- Não criar falsas urgências, contadores reiniciados artificialmente ou descontos fictícios.
- O jogo deve continuar utilizável se a rede, a loja ou a rede de anúncios estiver indisponível.
- Preços finais configurados nas lojas e exibidos pelo SDK, com moeda e localização fornecidas pela plataforma. Não hardcode valores regionais como se fossem universais.

## 7.2. Anúncios recompensados

Rede: **LevelPlay**, com o app criado na conta V-STACK. Anúncio **não personalizado para todos** e **sem `AD_ID`**: `LevelPlayPrivacySettings` antes do `Init` e `Setup.SemIdDePublicidade` tirando o `AD_ID` e as permissões do Privacy Sandbox do manifesto (código do DC em `Anuncios.cs` e `AnunciosLevelPlay.cs`).

Formatos de lançamento:

1. **Dobrar coleta offline:** uma vez por retorno, opcional.
2. **Bônus temporário de receita:** 2× receita operacional por 5 minutos, com limite diário configurável. Valor inicial sugerido: 3 vezes por dia, sem acumular um bônus com outro.
3. **Bônus de recuperação de voo:** **fora do lançamento.** Ele transforma perder voos em gatilho de anúncio, o que contraria o TA-5 e a "tensão justa" do §2.5. Só volta por decisão D-15.

**O que é recompensa de alto valor:** qualquer recompensa que dê Gems, direito pago (passe, cosmético) ou mais que 1 h de renda. Anúncio só dá recompensa de baixo valor, em Cash ou tempo, então o callback local do SDK basta. Se um dia houver recompensa de alto valor por anúncio, ela exige a verificação no servidor (callback servidor a servidor do LevelPlay) e o backend do §7.4.

### Requisitos técnicos

- Usar o modo de teste do LevelPlay (test suite, flag `-adtestsuite`) e aparelhos de teste durante o desenvolvimento; nunca clicar nos próprios anúncios de produção para testar.
- Separar a configuração de anúncio de desenvolvimento, staging e produção.
- Carregar o anúncio antes de oferecer o botão; desabilitar o botão enquanto não estiver pronto.
- Se o anúncio não carregar ou for fechado antes de concluir, não conceder recompensa.
- Conceder no máximo uma recompensa por evento/nonce.
- O callback local de "ad completed" não deve ser suficiente para recompensas de alto valor (definição acima).
- Aplicar limites por dia e intervalo, decididos no núcleo, nunca no SDK; monitorar padrões anormais.
- Destruir/recriar objetos de anúncio conforme o ciclo de vida do SDK, evitando vazamentos.
- Não exibir anúncios durante compra, onboarding crítico, diálogo de permissão, pausa ou quando o usuário estiver arrastando um funcionário.
- Não prometer um anúncio que o SDK não conseguiu carregar.

## 7.3. Catálogo inicial de compras

**Só existe se D-05 aprovar IAP.** Os IDs abaixo são propostas de nomenclatura, não IDs já criados. O agente deve consultar o projeto e as lojas antes de registrar produtos. Não criar duplicatas se já houver IDs estáveis. Na Google Play, um ID de produto criado não pode ser reaproveitado, então os IDs se decidem uma vez, aqui.

| Produto | ID sugerido | Tipo | Conteúdo |
|---|---|---|---|
| Pacote de Boas-vindas | `welcome_bundle_01` | Não consumível (a loja garante 1 por conta Google/Apple) | Cash + gems + pintura cosmética fixa |
| Gems pequeno | `gems_pack_small` | Consumível | Quantidade fixa de gems (D-07) |
| Gems médio | `gems_pack_medium` | Consumível | Quantidade fixa de gems (D-07) |
| Gems grande | `gems_pack_large` | Consumível | Quantidade fixa de gems (D-07) |
| Passe Temporada 1 | `season_pass_s1` | Não consumível, um por temporada | Trilha premium da temporada 1 |
| Pintura de aeronave | `aircraft_skin_blue_01` | Não consumível | Cosmético fixo |
| Decoração de terminal | `terminal_decor_01` | Não consumível | Cosmético fixo |

Regras:

- Conteúdo e quantidade exatos devem ser apresentados antes da compra.
- Gems compradas não expiram.
- Compras não podem ser concedidas apenas por salvar localmente uma flag.
- O pacote de boas-vindas deve ser comprado no máximo uma vez por conta/entitlement, mesmo após reinstalação ou troca de dispositivo. Por ser não consumível, a loja impede a 2ª compra. A parte em Cash e Gems é concedida uma vez por id de transação; numa reinstalação sem save na nuvem ela volta junto com o save novo, o que é restauração e não duplicação.
- A loja mostra "Restaurar compras" onde aplicável, estado de carregamento, falha, cancelamento e compra pendente.
- Não oferecer "remover anúncios" enquanto o jogo não tiver anúncios forçados; isso venderia um benefício inexistente.
- Não adicionar assinatura recorrente no lançamento sem necessidade comprovada e sem implementar cancelamento, estado de renovação, recuperação e comunicação claros.

## 7.4. Fluxo seguro de compra

1. Cliente solicita compra pelo Unity IAP (5.4.3, atrás de uma interface, com loja falsa no PC, como no DC).
2. Plataforma processa o pagamento.
3. Cliente recebe os dados de transação, mas não decide sozinho a titularidade.
4. Enviar `purchaseToken` do Google Play ou o dado de transação apropriado da Apple para o backend de validação.
5. Backend valida a transação diretamente com a plataforma, verifica produto, status, ambiente, usuário e se o identificador já foi processado. Google: Google Play Developer API (`purchases.products.get`). Apple: App Store Server API.
6. Registrar a transação com chave idempotente única (`purchaseToken`/`orderId` no Google; `transactionId` na Apple).
7. Conceder a entitlement/recompensa somente uma vez.
8. Confirmar/acknowledge a compra conforme o fluxo do SDK, só depois do OK do backend. É a compra em 2 passos do DC: o núcleo concede e só então confirma.
9. Atualizar o cliente a partir do resultado confirmado pelo backend.
10. Tratar reembolso, revogação, compra pendente e restauração sem duplicar recompensas. Google: Voided Purchases API. Apple: App Store Server Notifications.

**Bloqueador de lançamento:** se não houver validação confiável para compras que concedem gems, passe ou itens digitais, não ativar compras reais em produção. Não substituir validação por uma checagem local facilmente adulterável.

**Limite honesto do backend:** sem login, ele impede compra falsa (recibo forjado ou reaproveitado), mas não impede que alguém edite o próprio save local para ganhar Gems. Num jogo solo, sem ranking e sem troca entre jogadores, esse risco é de receita, não de outros jogadores, e o §8.2 manda não chamar a progressão local de "segura contra hack".

**Backend recomendado (D-06):** um serviço pequeno no Cloud Run, em projeto GCP próprio do jogo, região southamerica-east1, usando a cota grátis da conta de faturamento da Vstack (dividida com os outros produtos). A conta de serviço é autorizada na Play Console, sem chave JSON, e a chave da App Store Server API fica no Secret Manager. Alternativa: Supabase Edge Function em sa-east-1, que ocupa a última vaga de projeto grátis da organização e exige guardar a chave da conta de serviço como segredo.

## 7.5. Campanhas e ofertas comerciais

- Oferta de boas-vindas com conteúdo fixo e preço visível.
- Ofertas sazonais associadas ao calendário do jogo.
- Cosméticos com imagem real do item no jogo.
- Sem temporizador falso, "última chance" repetida ou desconto de preço que nunca existiu.
- Não bloquear o botão de fechar nem colocar compra no lugar do botão de continuar.
- Ofertas podem ser controladas por configuração remota, mas o cliente não deve receber segredos nem autoridade para conceder compras.
- Toda campanha precisa ter data/hora de início e fim, fuso horário, público elegível, ID de oferta, conteúdo, limites, regra de encerramento e fallback.
- Configuração remota inválida deve retornar ao catálogo seguro local, nunca quebrar o jogo.
- No lançamento, o catálogo é local e fixo no build. A única configuração remota é o kill switch do §15.3, que só desliga e nunca concede.

---

# 8. SEGURANÇA, ANTI-FRAUDE E PROTEÇÃO CONTRA INVASÕES

**Meta realista:** reduzir a superfície de ataque, detectar abuso e proteger compras e dados. Nenhum jogo cliente consegue impedir todo tipo de modificação. A segurança depende de defesa em camadas, monitoramento e capacidade de corrigir.

## 8.1. Modelo de ameaça

Considerar:

- save local editado;
- relógio do dispositivo manipulado;
- APK/IPA modificado;
- memória e velocidade do jogo alteradas;
- chamadas de API forjadas;
- recompensa de anúncio falsificada ou repetida;
- purchase token reutilizado;
- compras canceladas ou reembolsadas;
- duplicação de recompensa por retry;
- credenciais ou chaves incluídas no app;
- abuso de endpoints, spam de requisições e enumeração de IDs;
- SDK de terceiros vulnerável ou coletando dados além do necessário;
- falhas de permissão no console, repositório ou painel de backend;
- segredo commitado num repositório **público**.

## 8.2. Autoridade do cliente e do servidor

- Cliente pode apresentar UI e simular operações locais não competitivas.
- Backend/loja deve ser autoridade para titularidade de compras, gems compradas, passe premium e recompensas sensíveis.
- Não confiar em valores enviados pelo cliente para preço, saldo, produto, quantidade, identidade de compra ou status de anúncio.
- Para economia offline, validar limites, plausibilidade, tempo e repetição; nunca aceitar valores arbitrários do cliente.
- Se não houver backend de jogo, manter a progressão local casual limitada e não declarar que ela é "anti-hack segura". Compras ainda exigem validação da plataforma/servidor conforme o tipo de item.

## 8.3. Proteção do app e integridade

- Para Android, avaliar Play Integrity API e recursos de integridade disponíveis na conta; o veredito só vale verificado no backend. Sem backend, não se aplica.
- Não bloquear automaticamente todos os dispositivos legítimos por um sinal ambíguo.
- Usar Play App Signing e assinatura de release conforme a loja. A chave de upload fica fora do git, em `%USERPROFILE%\.<nome>\upload.jks` + `upload.properties`, com backup fora do PC.
- IL2CPP ARM64 e stripping, conforme compatibilidade e desempenho, sem tratar ofuscação como segurança suficiente.
- Se o Play Console oferecer proteção automática de integridade para a conta, avaliar habilitação e testar antes de promover produção.
- Não incluir "admin mode", menu de debug, cheats ou endpoints de teste no build público. As flags de teste (`-testsession`, `-dev-desbloqueia` e as outras da Origem) só funcionam em build de desenvolvimento.
- Desativar logs verbosos e comandos de debug em release.
- Separar ambiente de desenvolvimento, staging e produção.
- Verificar assinatura, package/bundle ID, versão e hash do artefato entregue.

## 8.4. Segredos e credenciais

- Nunca embutir chaves privadas, service account keys, tokens de backend, segredos de anúncios ou credenciais de loja no cliente.
- Segredos do backend ficam em secret manager ou variáveis protegidas do ambiente.
- Não imprimir segredos no terminal nem em relatórios.
- Não commitar keystores, certificados privados, `.env`, tokens, arquivos de service account ou credenciais de API. Rodar um scanner de segredos (gitleaks) antes de cada PR.
- Se um segredo for encontrado no repositório ou histórico, interromper o uso, notificar o responsável e recomendar rotação. Apagar o arquivo atual não remove o segredo do histórico.
- Princípio do menor privilégio: cada integração recebe apenas as permissões necessárias.
- Contas de loja e backend devem usar MFA quando disponível; limitar administradores.

## 8.5. APIs e backend

Se houver API própria (D-06):

- HTTPS obrigatório.
- Autenticação por identidade/credencial de sessão válida; não confiar em `userId` enviado sem validação.
- Validar esquema, tipo, tamanho e limites de todos os campos.
- Rate limiting por usuário, sessão, IP e endpoint conforme o contexto.
- Limites de valor e frequência para recompensas.
- Idempotency keys para compras e grants.
- Proteção contra replay, nonce/timestamp e verificação server-side.
- Logs de auditoria sem tokens completos ou dados pessoais desnecessários.
- Respostas de erro não devem revelar segredos, stack traces ou detalhes internos.
- CORS, regras de acesso e permissões devem ser mínimas se houver painel web.
- Backups e restauração testada para dados importantes.
- Monitoramento de erros, latência, taxa de rejeição e abuso.
- Alerta de orçamento no projeto GCP do jogo, como o que já existe para o FiscWise.
- Não criar endpoints de administração acessíveis anonimamente.
- Nunca permitir que o cliente escolha o usuário-alvo de uma recompensa ou altere diretamente o saldo de outro jogador.

## 8.6. Salvamento e integridade econômica

- Save versionado e com migração testada (`SaveEnvelope.cs` do Forge).
- Tratar arquivo ausente, truncado, inválido, antigo e parcialmente gravado.
- Escrita atômica quando possível: arquivo temporário seguido de substituição segura.
- Checksums locais ajudam a detectar corrupção acidental, mas não impedem um atacante que controla o cliente.
- Cloud Save deve resolver conflito com regra explícita, não escolher cegamente o maior saldo.
- Operações de economia devem ser transacionais ou idempotentes no backend.
- Nunca confiar em save local como comprovante de compra.
- Não permitir números negativos, `NaN`, infinito, overflow ou quantidade de item fora dos limites.
- Validar saldo antes e depois de cada operação econômica.

## 8.7. Proteção contra abuso de anúncios

- Cada recompensa de anúncio tem um identificador único.
- A mesma impressão não pode gerar duas recompensas.
- Validar SSV quando houver recompensa de alto valor (§7.2); rejeitar callbacks repetidos, malformados ou de ambiente incorreto.
- Aplicar limite diário e cooldown.
- Registrar métricas agregadas de falha e anomalia.
- Não armazenar identificadores de anúncio além do necessário para o propósito documentado. Sem `AD_ID`, o jogo não lê o ID de publicidade.
- Usar anúncios de teste em QA e confirmar que a configuração de produção só entra na build de release.

## 8.8. Resposta a incidentes

1. Detectar e classificar: compra, economia, conta, privacidade, disponibilidade ou código.
2. Preservar logs sem expor dados pessoais.
3. Desativar temporariamente a oferta ou endpoint comprometido pelo kill switch (§15.3) ou pelo backend, se isso for seguro.
4. Não apagar evidências nem banir automaticamente com base em um único sinal fraco.
5. Corrigir, testar em staging e registrar a mudança.
6. Avaliar obrigações de notificação e comunicação conforme a legislação aplicável e a política da plataforma. Na LGPD, incidente com risco relevante aos titulares é comunicado à ANPD e aos titulares.
7. Rotacionar credenciais comprometidas e revisar acessos.
8. Publicar hotfix apenas após testes críticos.

---

# 9. PRIVACIDADE, LGPD E DADOS DE JOGADORES

Este GDD não substitui análise jurídica. A pessoa/controladora responsável deve aprovar a política de privacidade, a base legal, a retenção e as declarações das lojas.

## 9.1. Minimização de dados

Coletar somente o necessário para:

- funcionamento do jogo;
- salvamento e restauração;
- compras e suporte;
- estabilidade/crash reporting;
- analytics de gameplay;
- anúncios, de acordo com consentimento e regras aplicáveis.

Evitar:

- nome real, data de nascimento completa, contatos, localização precisa e acesso a fotos/microfone sem função central que justifique;
- chat livre, mensagens privadas ou conteúdo gerado pelo usuário no lançamento, salvo se houver moderação e controles completos;
- compartilhar dados de analytics com SDKs sem revisar documentação e política.

O jogo não tem conta nem login no lançamento.

## 9.2. Consentimento e anúncios

- Identificar SDKs de anúncios/analytics e os dados que coletam. No lançamento: LevelPlay e as redes que ele mediar, Unity IAP e o SDK de analytics, se D-11 aprovar.
- O anúncio é não personalizado para todos, configurado antes do `Init`. Com isso, o fluxo de consentimento fica mínimo nos países do 1º lançamento.
- Se o jogo for distribuído no Espaço Econômico Europeu ou no Reino Unido, confirmar se a mediação exige uma plataforma de consentimento (CMP) certificada. A recomendação D-09 é lançar primeiro só em BR/PH/ID, a coorte do §15.2, e tratar o EEE e o Reino Unido depois.
- No iOS, não há pedido de App Tracking Transparency, porque o jogo não rastreia nem lê o IDFA. Se isso mudar, o ATT entra junto.
- Não pedir permissão sem necessidade real e sem explicação contextual.
- Se o público incluir crianças ou idade desconhecida, revisar políticas específicas de famílias, SDKs certificados e anúncios adequados.
- Não declarar público infantil por padrão nem ignorar essa possibilidade. Pelo ECA Digital, o jogo deve ser tratado como de acesso provável por crianças e adolescentes: configurações protetivas por padrão, nada aleatório pago e anúncio não personalizado (D-10, com revisão jurídica).
- Consentimento não deve ser pré-marcado nem inferido de inação.
- Se o usuário recusar personalização, o jogo deve continuar funcional e o SDK deve receber a configuração correspondente.

## 9.3. Política de privacidade e direitos

Antes da submissão, publicar uma política acessível por URL HTTPS (GitHub Pages, nos idiomas do jogo) e linká-la dentro do jogo e nas lojas. Ela cita exatamente as redes e SDKs do build final.

Ela deve descrever:

- dados coletados;
- finalidade e base aplicável;
- SDKs e terceiros relevantes;
- compartilhamento e retenção;
- segurança;
- como contatar o responsável;
- como exercer direitos e solicitar exclusão;
- como revogar consentimento quando aplicável;
- tratamento de dados de crianças/adolescentes, caso o produto os alcance.

Se houver criação de conta, oferecer processo de exclusão de conta dentro do app quando exigido pela plataforma e um caminho externo aplicável. Não adicionar login obrigatório se o jogo não precisar de conta para funcionar. Sem conta, basta um caminho para pedir a exclusão dos dados de analytics e de compras (e-mail de suporte, D-12).

## 9.4. Formulários das lojas

- **Google Play:** preencher Data safety de acordo com o comportamento real do app e de todos os SDKs incluídos; declarar anúncios ("Contém anúncios"), ID de publicidade = **não usa**, público-alvo, conteúdo e permissões.
- **Apple:** preencher App Privacy, incluir política de privacidade e garantir que SDKs, compras e backend estejam funcionando para revisão.
- Nunca copiar uma declaração genérica sem comparar com o build final.
- Se o comportamento de coleta mudar, atualizar política e formulários antes de lançar a nova versão.

---

# 10. ARTE, DIREÇÃO VISUAL E PIPELINE TRIPO3D

## 10.1. Direção de arte

- Estilo: low-poly acolhedor, formas arredondadas, leitura imediata e cores por função.
- Câmera: isométrica/ortográfica consistente.
- Luz: uma direção principal consistente, sombras suaves e contraste suficiente para filas e estações.
- Paleta: regional quente/verde; aeroporto turístico azul/coral; hub internacional azul profundo/dourado.
- Personagens e veículos com silhuetas distintas.
- UI com tipografia legível, hierarquia forte e botões confortáveis.
- Evitar realismo excessivo, texturas ruidosas e detalhes que desapareçam em escala mobile.

## 10.2. Pipeline de assets

O pipeline é o do estúdio (Origem, "Arte e animação"): Tripo → Auto Rig/Mixamo → Blender → `render_sprites.py` → sprites isométricos pré-renderizados, sem skinning no aparelho.

1. Auditar os assets atuais e a lista de lacunas antes de gerar qualquer coisa.
2. Criar uma folha de estilo com 3–5 assets aprovados como referência.
3. Gerar no Tripo3D os modelos que faltam em lotes por família: edifícios, aeronaves, veículos de serviço, mobiliário, personagens e decoração. **Cada lote só com aval do Vinicius**, conferindo o saldo antes e depois.
4. Preferir modelos modulares e reutilizáveis a dezenas de objetos únicos. Itens (mala, mochila, scanner) são gerados separados e presos ao osso no Blender.
5. Renderizar sprites no Blender com câmera, iluminação, escala, ângulo e resolução padronizados (`render_sprites.py` do DC).
6. Exportar PNG transparente/spritesheets.
7. Importar no Unity com compressão, atlas, pivôs e filtros corretos (`Editor/SpriteImport.cs`).
8. Confirmar transparência, bordas, escala, pivô, direção da luz, legibilidade e ausência de artefatos, por métrica em script e por folha de contato em baixa resolução.
9. Registrar arquivo de origem, prompt/versão da geração, licença/termos aplicáveis e asset final em `docs/PROVENIENCIA.md`, com o SHA.
10. Fazer revisão visual dentro da cena real, não apenas no preview isolado: fotos 540×960 de cada tela.

Arte crua paga (zips do Tripo, FBX, `.blend` com modelo) fica fora do git, no checkout principal, com backup privado. A licença dos modelos não permite redistribuir, e o repositório é público.

Interface pintada (logo, ícones, retratos) sai do gerador de imagens grátis, sobre fundo liso e recortada por script.

**Créditos estimados (HIPÓTESE, preço da Origem: modelo HD 45, rig 20):**

| Lote | Conteúdo | Créditos |
|---|---|---|
| 1 (v0.1, Origem) | passageiro, funcionário, avião regional, check-in, raio-x, portão, loja, terminal | ≈ 400 (teto ≈ 530) |
| 2 (lançamento) | bagagem, alimentação, hangar de manutenção, torre de despacho (4 × 45); narrow-body e wide-body (2 × 45); terminais de Costa Azul e Aurora Global (2 × 45); rebocador, carrinho de bagagem e caminhão de combustível (3 × 45) | ≈ 495 |
| 2, opcional | 2º corpo de funcionário (macacão do mecânico) | + 65 |

As 8 variantes de aeronave, os 6 uniformes e as roupas dos passageiros saem por troca de cor no Blender, sem crédito.

## 10.3. Lista prioritária de arte

**Ambientes:** 3 aeroportos, pistas, terminal modular, portões, check-in, segurança, bagagem, lojas, alimentação, manutenção, torre/controle e áreas de serviço.

**Aeronaves:** 8 variantes visuais, com pelo menos 3 silhuetas distintas. Variações de pintura podem reutilizar a mesma malha se a leitura for boa.

**Funcionários:** 6 perfis visuais/especializações; animações simples de idle, andar e trabalhar, a partir dos clipes Mixamo que já estão no disco (tabela da Origem).

**Passageiros:** conjunto modular de corpos, roupas e cores para criar variedade sem exigir centenas de modelos. Histórias podem reutilizar personagens.

**Ícones:** moedas, rotas, incidentes, status de voo, especialidades, melhorias, missões, conquistas, passe e compras.

**UI:** painéis, botões, badges de estado, barras de progresso, ícones de loja, molduras de recompensa, tutorial e configurações. O quadro de partidas é a peça central (painel amarelo sobre preto, horário em fonte mono OFL).

**Marketing:** ícone de app (adaptativo, conferido nas máscaras), feature graphic, screenshots de gameplay, arte promocional da temporada e criativos de vídeo.

## 10.4. Otimização visual

- Pooling de passageiros, aeronaves, efeitos e elementos de fila.
- Evitar instanciar/destruir centenas de objetos continuamente.
- Atlas de sprites e compressão apropriada.
- Evitar texturas gigantes sem necessidade.
- Evitar sombras dinâmicas caras se sprites pré-renderizados entregarem a mesma leitura.
- Não usar modelos 3D animados pesados em tempo real se sprites atenderem ao visual e à performance.
- Garantir legibilidade em dispositivos de tela pequena e densidade baixa.
- Medir o PSS no aparelho com 60 passageiros; no DC, as folhas HD levaram o APK de 67,7 para 83 MiB.
- Assets não utilizados só devem ser removidos após comprovar que não são referenciados; criar backup antes de limpeza em lote.

## 10.5. Direitos autorais e marca

- Não usar logos, pinturas, marcas, personagens ou fotos de terceiros sem licença.
- Não imitar a identidade visual de concorrentes.
- Nome comercial, ícone e materiais devem passar por verificação de colisão e direitos.
- Guardar registros de origem/licença dos assets gerados e de bibliotecas de terceiros.
- Não usar nomes de companhias aéreas reais no lançamento sem autorização. Os nomes fictícios da Origem §13 precisam de busca antes (§0.4).

---

# 11. UI/UX E ACESSIBILIDADE

## 11.1. Telas obrigatórias

- Splash/loading com falha de rede tratada.
- Tutorial inicial.
- Aeroporto principal.
- Quadro de partidas.
- Painel de estação e melhoria.
- Painel de funcionários.
- Painel de rotas e aeronaves.
- Campanha/objetivos.
- Missões diárias/semanais.
- Conquistas.
- Passe de temporada.
- Loja de compras.
- Painel de recompensas offline.
- Configurações: som, música, idioma se disponível, consentimento/privacidade, suporte, créditos, versão e "Rever tutorial".
- Restaurar compras.
- Erro de compra/rede com opção de tentar novamente.
- Política de privacidade e exclusão de dados/conta quando aplicável.

## 11.2. Interação

- Alvos principais de toque com dimensão mínima de referência de 48 dp quando aplicável ao layout.
- Evitar texto essencial dentro de imagens.
- Nunca depender só de cor para comunicar status: usar ícone, forma ou texto.
- Botões de compra separados dos botões de fechar/voltar.
- Não mostrar anúncios no ponto em que o jogador costuma tocar para mover funcionários.
- Confirmação para ações irreversíveis dentro do jogo, como gastar grande quantidade de gems.
- Feedback para compra pendente, restauração e perda de conexão.
- Tutoriais curtos, contextuais e não repetidos após conclusão, exceto se o jogador os reabrir. O tutorial é o `Tour.cs` do DC com os 12 passos da Origem; passo de botão se cancela se a tela mudar.
- Retrato com CanvasScaler de referência 1080×1920 e área segura para o notch. Em 20:9, o cenário continua até a borda.

## 11.3. Localização

- Idioma inicial: português do Brasil. Os outros idiomas são a decisão D-08; a coorte BR/PH/ID do §15.2 precisa no mínimo de inglês.
- Estruturar strings para localização, sem texto hardcoded em scripts. Tabela `L(pt, en, es, id)` com frase inteira por idioma, como o `Textos.cs` do DC.
- Inglês pode ser preparado como segundo idioma se todas as strings e materiais estiverem revisados; não publicar tradução automática incompleta.
- Formatar números, moedas, datas e pluralização conforme cultura.
- Testar texto longo, acentos, caracteres especiais e escala de fonte (as fontes do DC cobrem Latin-1: "Florianópolis" passa).

---

# 12. ÁUDIO

- Música ambiente calma, com variações sutis por aeroporto.
- SFX curtos para toque, melhoria, embarque, decolagem, recompensa, alerta e conclusão de missão.
- Alertas críticos distinguíveis sem serem agressivos.
- Controles separados de música e efeitos.
- Não reproduzir áudio alto automaticamente após voltar do background.
- Licenças e origem de todas as faixas/efeitos devem ser registradas em `docs/PROVENIENCIA.md`.
- Áudio não pode bloquear o loop de gameplay se arquivos falharem; usar fallback silencioso.
- Primeira opção: áudio sintetizado por script (`audio_gen.py` do DC), sem custo de licença. O motor de avião é o risco: testar cedo e, se soar pobre, usar uma faixa licenciada e registrada.

---

# 13. ARQUITETURA LÓGICA SUGERIDA

Adaptar aos padrões que já existirem no projeto. Não duplicar sistemas funcionais sem necessidade. O padrão do estúdio é núcleo em C# puro (`TA.Core`) com uma API documentada no topo do `Sim.cs`, mais a View em MonoBehaviours, e as peças vêm por cópia do Forge (`fs:`) e do DC (`dc:`). Cada serviço da v1.0 vira um arquivo desse padrão e só nasce quando a fase que o usa começar:

| Serviço da v1.0 | Onde mora | Origem da cópia |
|---|---|---|
| `GameBootstrap` | `View/Game.cs` (leitura de flags, injeção de `nowUnix`) | `dc:Scripts/View/Game.cs` |
| `AirportStateController`, `FlightScheduleService`, `PassengerFlowSystem`, `StationService`, `IncidentService`, `RouteService` | `Core/Sim.cs`, depois `partial` por assunto quando passar de ~600 linhas | `fs:Scripts/Core/Sim.cs` |
| `StaffAssignmentService` | `Core/Equipe.cs` | `fs:Scripts/Core/Equipe.cs` |
| `EconomyService`, `ProgressionService` | `Core/Sim.cs` + `Core/Tuning.cs` | Forge |
| `CampaignService` (diárias, semanais, campanha) | `Core/Metas.cs` + `Core/Encomendas.cs` | Forge |
| `SeasonPassService` | `Core/Temporada.cs` (novo) | — |
| `SaveService` | `Core/SaveEnvelope.cs` | Forge |
| `PurchaseService` | `View/Loja.cs` + `View/LojaIap.cs`, atrás de interface | DC |
| `AdsService` | `Core/Anuncios.cs` (regras e limites) + `View/AnunciosLevelPlay.cs` | DC |
| `ConsentService` | dentro do `AnunciosLevelPlay.cs` (privacidade antes do `Init`) | DC |
| `AnalyticsService` | `View/Exportar.cs` (diário de playtest) + o SDK de D-11 | DC |
| `AudioService` | `View/Audio.cs` | DC |
| `UIFlowController` | `View/Tour.cs` + telas | DC |
| `RemoteConfigService` | só o kill switch do §15.3 | — |

Dados configuráveis devem ter IDs estáveis e validação automatizada. Não usar o nome de exibição como chave primária. Enum salvo só cresce no fim. Não deixar sistemas de gameplay dependerem diretamente de objetos de UI. `docs/MAPA_CODIGO.md` lista cada arquivo e é atualizado no PR que o cria ou move.

---

# 14. DADOS, SALVAMENTO E MIGRAÇÕES

## 14.1. Save local

Salvar:

- versão de schema;
- progresso dos aeroportos;
- cash e gems disponíveis;
- upgrades;
- funcionários;
- rotas desbloqueadas;
- campanhas e conquistas;
- progresso de temporada;
- estado de áudio/configuração;
- ids de transação já concedidos (compras, anúncios, offline);
- timestamps necessários para offline, tratados como não confiáveis.

Não salvar dados de cartão, credenciais, segredos, recibos completos sem necessidade ou tokens de autenticação em texto simples.

## 14.2. Migração

- Cada alteração de schema tem migração testada de versão anterior.
- Testar save antigo, save ausente, save inválido, arquivo parcial e atualização interrompida.
- Nunca sobrescrever o único save válido com um save corrompido.
- Se migração falhar, recuperar backup e informar o jogador de forma compreensível.
- Não limpar progresso do usuário silenciosamente.

## 14.3. Nuvem

- Não há Cloud Save no lançamento. Se um dia for configurado, testar primeiro e documentar a regra de conflito.
- Não criar login obrigatório somente para usar nuvem.
- Não considerar a nuvem como substituta de backups operacionais.
- Testar troca de dispositivo, sessão sem rede, duas versões do app e conflito de save.
- Se a nuvem não estiver configurada, o jogo precisa continuar funcionando localmente. Compras ainda devem ter restauração segura via loja e validação.

---

# 15. ANALYTICS, MÉTRICAS E TELEMETRIA

## 15.1. Eventos mínimos

Estes são os nomes oficiais. O diário de playtest da Origem e os kill criteria da Origem §51 usavam nomes curtos, que passam a ser estes: `flight_depart` → `flight_departed`; `queue_length` → `queue_length_sampled`; `station_upgrade` → `station_upgraded`; `staff_move` → `staff_moved`. Ficam também `story_delivered`, `offline_claim` e `tour_step` da Origem.

- `tutorial_started`
- `tutorial_completed`
- `tutorial_step_failed`
- `tour_step`
- `flight_spawned`
- `flight_boarding_started`
- `flight_departed`
- `flight_missed`
- `queue_length_sampled`
- `station_upgraded`
- `staff_moved`
- `staff_hired`
- `story_delivered`
- `route_unlocked`
- `airport_unlocked`
- `campaign_objective_completed`
- `daily_mission_completed`
- `weekly_mission_completed`
- `season_xp_gained`
- `season_reward_claimed`
- `season_pass_purchased`
- `offline_claim`
- `rewarded_ad_offered`
- `rewarded_ad_started`
- `rewarded_ad_completed`
- `rewarded_ad_failed`
- `reward_granted`
- `purchase_started`
- `purchase_pending`
- `purchase_succeeded_server_verified`
- `purchase_failed`
- `purchase_restored`
- `purchase_refunded`
- `save_loaded`
- `save_migration_failed`
- `cloud_save_conflict`
- `app_crash`
- `performance_sample`
- `network_error`

Não enviar nome real, e-mail, texto livre, token de compra completo ou identificadores pessoais para analytics.

Onde os eventos chegam: no playtest, no diário CSV exportável (`Exportar.cs` + `diario_report.py`, que já calcula o K2 e o embarque médio). Em produção, no SDK escolhido em D-11. Crash e ANR vêm do Android vitals da Play Console, sem SDK.

## 15.2. Indicadores de produto

As metas iniciais herdadas do GDD original são hipóteses, não garantias universais. Os números de continuar, iterar e cancelar estão na Origem §51:

- D1: 26% como piso de alerta; a meta de melhoria deve ser avaliada por coorte.
- D7: 5% como piso de alerta.
- Opt-in de anúncio recompensado: meta inicial de pelo menos 40% dos usuários elegíveis, sem pressionar o jogador.
- Pelo menos 60% dos jogadores de teste devem reagir a um gargalo/voo em risco nos primeiros 60 segundos após aprender a mecânica (K2).
- Com a equipe móvel, a eficiência média deve melhorar de forma mensurável contra um teste com equipe fixa (bot: ≥ 10%).
- Medir tutorial, crashes, desempenho, abandono, falhas de compra, anúncios não carregados e reembolsos.
- Segmentar por versão, plataforma, país/coorte e origem de aquisição quando permitido e com volume suficiente.
- Não interpretar métricas com amostra pequena como prova definitiva. A coorte é paga, de 1,5–2k installs em BR/PH/ID; com 1k installs, a margem do D7 é ±1,7 pp.

## 15.3. Kill switches

Uma configuração remota pode desligar com segurança:

- oferta comercial com erro;
- anúncio recompensado problemático;
- temporada com configuração inválida;
- recompensa suspeita;
- rota ou evento com bug.

Proposta sem backend: um JSON estático por HTTPS, no mesmo GitHub Pages da política de privacidade, com a lista do que está desligado. O cliente lê esse arquivo com timeout curto, e ele **só desliga, nunca concede**: um JSON adulterado, no pior caso, desliga uma oferta. Se houver backend (D-06), o kill switch pode morar nele.

Se a configuração remota falhar, usar valores locais seguros e continuar o jogo.

---

# 16. TESTES OBRIGATÓRIOS E CRITÉRIOS DE ACEITE

**Nenhuma build pode ser declarada pronta sem um relatório real.** Os testes precisam ser repetíveis e registrar versão, dispositivo/ambiente, comando, resultado e falha.

## 16.0. Onde cada teste roda (portão do estúdio)

| Camada | Comando | Cobre |
|---|---|---|
| Núcleo | `dotnet test client/tools/coretests` (sem Unity, segundos) | §16.1 e a lógica do §16.3 |
| Compilação da View | `dotnet build client/tools/viewcheck/view` | erros de C# da View sem abrir o Unity |
| Unity | EditMode (NUnit) | integração com o Unity |
| Jogo real | build Windows + `-autoplay N` com 0 exceções, `-testsession` e flags da Origem | §16.2 (o papel do PlayMode) e os números do bot |
| Visual | fotos 540×960 de cada tela, conferidas à mão | texto sobreposto, controle tampando, placeholder |
| Aparelho | APK, por último (§16.4) | §16.4 e §16.5 |

`client/tools/verify.sh quick|full` roda essas camadas num comando, com log em `client/Builds/verify/`, e imprime só OK ou o erro. O `full` roda antes de todo PR de jogo.

## 16.1. Testes automatizados de unidade

- Cálculo de receita e custo.
- Transição de estados do voo.
- Deadline e fechamento de portão.
- Capacidade/tempo de cada estação.
- Movimentação de funcionários e efeito na origem/destino.
- Regras de incidentes.
- Desbloqueio de rotas e aeroportos.
- Campanhas e condições de conclusão.
- Pontos e níveis do passe.
- Limites de recompensa offline.
- Regras de limite diário/cooldown de anúncio.
- Idempotência de compra e recompensa.
- Validação de quantidades e overflow.
- Migração de save.
- Formatação de valores e tempo.
- As invariantes da Origem §50 como testes negativos nomeados: perder um voo nunca custa mais que a receita dele; offline nunca perde voo nem satisfação; nada sai de sorteio pago; toda concessão tem id de transação.

## 16.2. Testes de integração/PlayMode

- Partida completa de um voo, do spawn à decolagem.
- Voo perdido sem travar a simulação.
- Dois ou mais voos simultâneos e filas concorrentes.
- Realocação de equipe durante a onda.
- Incidente iniciado e concluído.
- Compra mockada e atualização de entitlement.
- Recompensa de anúncio simulada com callback válido, inválido, repetido e cancelado.
- Save/quit/reload.
- Offline, retorno, recompensa e limite de 2 horas.
- Campanha, missão diária, missão semanal e temporada.
- Mudança de aeroporto sem perder estado.
- Falha de rede e reconexão.
- UI sem modal preso e sem botão sem ação.

## 16.3. Testes de segurança/abuso

- Alterar o relógio para trás e para frente.
- Editar save local e inserir números absurdos.
- Repetir a mesma requisição de recompensa.
- Repetir o mesmo identificador de compra.
- Simular compra pendente, cancelada, reembolsada e restaurada.
- Enviar produto desconhecido, quantidade negativa, ID inválido e payload grande.
- Repetir callback de anúncio.
- Rodar com rede desligada e durante timeout.
- Confirmar que nenhuma chave secreta aparece no build, logs ou repositório (gitleaks no repositório; busca de strings no APK).
- Confirmar que endpoints administrativos não estão públicos.
- Confirmar que debug/cheats não existem no build de release.
- Confirmar que o cliente não consegue conceder gems ou passe premium apenas alterando o save **no backend** (o save local editado continua editável; ver o limite honesto do §7.4).
- Confirmar que o manifesto do AAB não tem `AD_ID`.

## 16.4. Testes de dispositivo

Executar primeiro no editor/PC e em emuladores. Concentrar os testes físicos no fim para não desperdiçar tempo do dispositivo pessoal. O celular do Vinicius só depois de ele dizer "pode testar", com print só com o jogo em primeiro plano. Unity, emulador e Blender nunca rodam juntos (PC de 7,7 GB).

- Android: tela pequena, dispositivo de entrada, resolução/densidade comum, retomada do background, pausa e falta de memória.
- iOS: se a build e assinatura estiverem configuradas, testar instalação, compra sandbox, retomada, restauração e layout.
- Orientação retrato e mudanças de safe area.
- Áudio com mute e retorno do background.
- Rede lenta/intermitente.
- Reinstalação e restauração de compra.
- Não testar anúncios reais; usar o modo de teste.

## 16.5. Performance

- Meta de lançamento: estabilidade de 30 FPS em Android de entrada suportado, com alvo de 60 FPS em dispositivos mais fortes quando possível (`vSyncCount = 0` e `targetFrameRate = 60` logo no início).
- Testar 60 passageiros visíveis e filas cheias (`-passageiros 60`). Critério K3 da Origem: p10 ≥ 30 FPS.
- Pooling ativo para objetos repetidos.
- Sem crescimento contínuo de memória durante sessão de 20 minutos.
- Sem travamento de UI durante carregamento de loja, anúncio ou save.
- Registrar FPS baixo, memória, tempo de carregamento e crashes (`medir_aparelho.sh` do Forge).
- Não esconder falha de desempenho reduzindo silenciosamente a simulação ou removendo efeitos sem registrar a alteração.

## 16.6. Testes de monetização

- Sandbox/test environment para todas as compras.
- Produto indisponível na loja.
- Compra cancelada pelo usuário.
- Compra pendente.
- Compra confirmada e backend indisponível.
- Backend confirma depois de reconectar.
- Repetição de callback.
- Restore/entitlement após reinstalação.
- Reembolso/revogação quando o ambiente permitir simular.
- Preço e moeda localizados.
- Passe comprado no meio da temporada (recebe o retroativo).
- Passe já comprado.
- Recompensa de anúncio não concedida após falha/cancelamento.
- Recompensa não duplicada ao reabrir o app.
- Nenhuma compra de produção durante QA.

## 16.7. Testes de regressão

Executar o conjunto crítico (`verify.sh full`) depois de:

- atualizar Unity ou pacotes;
- alterar save/economia;
- alterar compras/anúncios;
- alterar cenas e assets;
- alterar backend/configuração remota;
- gerar build final.

## 16.8. Critérios de aceite de gameplay

- Um jogador novo consegue concluir o primeiro voo com tutorial curto (passos 1–5 da Origem no 1º minuto).
- Jogadores de teste entendem por que um voo está em risco.
- Pelo menos 60% do grupo de teste tenta mover um funcionário sem instrução extensa.
- O teste interno com equipe móvel demonstra ganho mensurável contra equipe fixa.
- Nenhum voo fica preso indefinidamente em estado intermediário.
- Todas as 30 missões principais são concluíveis e testadas.
- Todos os produtos da loja têm descrição, conteúdo e comportamento correspondentes.
- Todos os níveis e recompensas da temporada têm conteúdo definido; não há placeholders.
- Nenhuma tela essencial contém texto temporário, botões mortos ou assets de teste.

## 16.9. Critérios bloqueadores

Bloqueia a submissão:

- crash no fluxo principal;
- perda ou corrupção de save sem recuperação;
- compra concedida sem validação;
- recompensa duplicável;
- vazamento de segredo;
- anúncio de teste em produção ou configuração de produção usada em QA;
- declaração de privacidade inconsistente;
- asset sem direito de uso confirmado;
- botão de compra/restauração quebrado;
- app não compila ou não assina;
- política obrigatória da loja incompleta;
- erro que permite saldo negativo, gems ilimitadas ou conteúdo pago duplicado;
- placeholders ou conteúdo promocional que não existe no jogo;
- `AD_ID` no manifesto ou item aleatório pago.

---

# 17. PROCESSO DE DESENVOLVIMENTO ACELERADO

## 17.1. Regra de execução

- Trabalhar em branch `feat/`, `fix/`, `docs/` ou `art/`; commits em português com escopo (`CONTRIBUTING.md`); entra por PR na `main`.
- Registrar `git status` antes de editar.
- Criar checkpoint/commit após cada fase estável.
- Não sobrescrever arquivos desconhecidos em lote.
- Não atualizar o Unity nem reinstalar pacotes sem necessidade.
- Rodar testes após cada sistema, não deixar tudo para o fim.
- Gerar assets em lotes com lista e nome padronizado.
- Não usar `TODO`, mock ou dados falsos em fluxo de compra de produção.
- Não remover testes para fazer a pipeline ficar verde.
- Se um teste falhar, corrigir ou registrar bloqueio; não apagar o teste.
- Um chat por entrega (um PR). Ao fechar: STATUS com "CONTINUAR DAQUI", commit e PR.
- Log de build e de teste vai para arquivo; no relatório entra só o resumo.

## 17.2. Fases

### Fase 0 — Decisões

- As decisões D-01 a D-14 do §23.2 que travam código: começar o projeto, nome e appId, monetização, idiomas e países.

### Fase A — Auditoria e estabilização

- Descobrir versão do Unity, plataforma, branch, cena inicial, scripts, pacotes, serviços, build settings, assets e testes. **Feito em 29/10 (§0.3): não há projeto.**
- Gerar matriz de requisitos (`docs/CONTRATO_LANCAMENTO.md`) e contrato da v0.1 (`docs/CONTRATO_V01.md`).
- Criar o esqueleto por cópia (núcleo do Forge + casca do DC, namespace `TA`) com o portão verde ainda vazio.
- Identificar segredos, IDs duplicados, SDKs desatualizados, scripts sem referência e erros de compilação.
- Criar backup verificável.
- Produzir inventário de assets que faltam.

### Fase B — Core jogável

- Quadro de partidas.
- Fluxo de passageiros/estações.
- Equipe móvel.
- Rotas e economia.
- Incidentes e tutorial.
- Save/migração.
- Testes automatizados do core.
- **Portão:** os números do §4.2 no bot e o K2 com gente. Se o slot idle ainda estiver em validação, os criativos do TA-1 e do TA-3 gravados do greybox entram no mesmo teste de criativos (Origem §53).

### Fase C — Conteúdo e progressão

- Três aeroportos.
- Rotas, aeronaves e funcionários.
- Campanha de 30 objetivos.
- Histórias, conquistas e missões.
- Passe de temporada completo.
- Arte e áudio integrados.

### Fase D — Monetização e segurança

- Unity IAP configurado.
- Validação server-side.
- Catálogo de produtos nas lojas.
- Anúncios recompensados em modo de teste.
- Consentimento/privacidade.
- Rate limits, idempotência, logs e proteções.
- Testes de compra, restauração, anúncios e abuso.

### Fase E — Release candidate

- Build de release.
- Testes de regressão.
- Performance.
- Ícone, screenshots, descrição, política e formulários das lojas.
- Verificação de package/bundle ID, versão, assinatura e hashes.
- Relatório final GO/NO-GO.

## 17.3. Plano por portões

O plano de 26/10 a 29/10 da v1.0 foi substituído. A auditoria (§0.3) mostrou que o projeto não existe, e o próprio GDD manda, nesse caso, não usar 72 horas como estimativa e pedir decisão sobre o prazo.

| Fase | Portão de saída | Depende de | Duração (ordem de grandeza, **não medida**) |
|---|---|---|---|
| 0 | D-01 a D-14 respondidas | Vinicius | — |
| A | esqueleto copiado, `verify.sh full` verde, contratos escritos | D-01, D-03 | 2–4 dias |
| B | §4.2 cumprido (bot + K2 com gente) | A | 1–2 semanas |
| C | 30 objetivos, 3 aeroportos, temporada e arte integrados, `verify.sh full` verde | B, Lotes 1 e 2 aprovados | 3–5 semanas |
| D | §16.3 e §16.6 passando em sandbox | C; D-05, D-06; Play Console para os produtos | 1–2 semanas |
| E | §25 atendido | D; Play Console; para iOS, macOS e conta Apple | 1 semana |

Somando, a ordem de grandeza é de 7 a 12 semanas até o pacote Android, contando com o reuso do Forge e do DC. A Origem §53 estimava 3–4 semanas só para o APK do MVP. A estimativa se refaz no fim da Fase B, com a velocidade real. A Play Console (previsão 2026-11-25) só trava as Fases D e E.

---

# 18. CHECKLIST DE BUILD E PUBLICAÇÃO

## 18.1. Identidade e versão

- [ ] Nome final aprovado após verificação de colisão (INPI, USPTO, EUIPO e lojas).
- [ ] Package ID Android (`br.com.vstack.<nome>`) e Bundle ID iOS aprovados e estáveis.
- [ ] Ícone final conforme os requisitos da loja.
- [ ] Version name e build number incrementados.
- [ ] Orientação retrato configurada.
- [ ] Splash e tela inicial sem placeholders.
- [ ] Links de suporte e privacidade ativos em HTTPS.
- [ ] Créditos e licenças de terceiros incluídos quando exigido (fontes OFL, áudio, modelos).

## 18.2. Android / Google Play

- [ ] Unity/Android SDK compatíveis com o nível de API exigido no dia do envio.
- [ ] Bibliotecas nativas alinhadas a 16 KB e versão do Billing aceita pela Play.
- [ ] AAB de release assinado.
- [ ] Play App Signing configurado.
- [ ] Permissões mínimas e justificadas; `AD_ID` e permissões do Privacy Sandbox fora do manifesto.
- [ ] Requisitos de arquitetura e 64-bit verificados conforme build e SDKs.
- [ ] Configuração de anúncios de produção correta; modo de teste ausente do artefato final.
- [ ] IAPs criados, preços, países e estados revisados.
- [ ] Data safety preenchido de acordo com todos os SDKs.
- [ ] Declaração de anúncios; ID de publicidade = não usa.
- [ ] Público-alvo, classificação de conteúdo e questionários preenchidos corretamente.
- [ ] Política de privacidade e exclusão de conta/dados conforme aplicável.
- [ ] Screenshots reais de gameplay, ícone, feature graphic e descrição.
- [ ] Instruções de acesso/revisão e backend disponível.
- [ ] Teste de instalação/atualização e fluxo de compra em trilha de teste.
- [ ] Play Integrity/proteção de integridade avaliada, se disponível.

## 18.3. iOS / App Store

- [ ] macOS com Xcode disponível (Mac, runner de CI ou Unity Build Automation).
- [ ] Certificados, provisioning profiles e signing válidos.
- [ ] Bundle ID correto.
- [ ] Arquivo de release arquivado e validado.
- [ ] Produtos IAP configurados e visíveis para revisão.
- [ ] Restauração de compras testada quando aplicável.
- [ ] App Privacy e política de privacidade consistentes com SDKs.
- [ ] ATT somente se houver necessidade real de tracking (hoje não há).
- [ ] Screenshots e metadados por dispositivo exigido.
- [ ] Notas para revisão explicando passe, anúncios recompensados e compras.
- [ ] Backend/serviços ativos durante a revisão.
- [ ] Teste em dispositivo físico quando possível.
- [ ] Nenhum placeholder, botão morto ou funcionalidade escondida.

## 18.4. Pacote de evidências

Entregar em pasta de release (e como pré-release no GitHub, com APK, AAB, zip Windows e pacote da loja):

- `release_manifest.md`
- versão do Unity e versões de SDKs;
- commit/hash do código;
- hash do AAB/IPA;
- logs de build;
- relatório de testes;
- lista de produtos e IDs, sem segredos;
- configuração de anúncios por ambiente, sem chaves privadas;
- checklist de privacidade;
- inventário de licenças de assets (`docs/PROVENIENCIA.md`);
- screenshots e textos de loja;
- lista de bloqueios residuais, mesmo que vazia.

---

# 19. PÁGINA DA LOJA E MATERIAIS DE MARKETING

## 19.1. Mensagem principal

**Headline sugerida:** "Faça cada voo decolar no horário."

**Subtítulo:** "Construa seu aeroporto, mova sua equipe, resolva crises e transforme uma pequena pista em um hub internacional."

Subtítulo de loja com a palavra de busca: "Little Terminal: Airport Idle" (Origem §52).

Não prometer multiplayer, centenas de aeroportos, companhias reais ou funcionalidades que não estejam no build enviado.

## 19.2. Descrição curta sugerida

"Gerencie filas, mova funcionários, escolha rotas e faça cada voo decolar a tempo. Construa aeroportos, desbloqueie aeronaves e supere desafios em um tycoon de gestão feito para sessões rápidas e decisões inteligentes."

## 19.3. Screenshots obrigatórias

1. Aeroporto inicial em funcionamento.
2. Quadro de partidas com voo em risco.
3. Jogador movendo equipe entre estações.
4. Filas e melhorias de terminal.
5. Rotas e aeronaves.
6. Segundo/terceiro aeroporto.
7. Campanha e recompensas.
8. Passe de temporada, se estiver pronto e o conteúdo mostrado for real.

Cada imagem deve refletir o produto. Não simular uma UI que não existe nem inserir recursos falsos em arte promocional. As capturas saem do build de release por script, em cada idioma do jogo (`loja_fotos.sh` do DC).

## 19.4. Vídeos e peças de aquisição

Criar 4 variações de criativo de 15–25 segundos, gravadas do jogo real com `-record`:

- **Criativo A — Caos controlável:** fila crítica → funcionário movido → voo embarca. O caos precisa existir no build (1ª onda com a segurança vermelha).
- **Criativo B — Decisão:** dois voos, uma equipe limitada → escolha do jogador → consequência.
- **Criativo C — Crescimento:** pista pequena → melhorias → hub internacional.
- **Criativo D — História:** "ela vai perder o casamento?" (TA-3). É o mais barato e, segundo a Origem, o mais emocional.

O vídeo deve usar captura real do jogo. Se usar mockup de edição, não apresentar como gameplay real. Guardar arquivo fonte e versões.

## 19.5. Plano de campanha de lançamento

- Preparar campanhas pagas somente depois de validar instalação, tutorial, retenção inicial e estabilidade.
- Começar com orçamento pequeno e limite diário explícito, aprovado pelo Vinicius.
- Testar os quatro criativos, registrando as hipóteses.
- Medir CPI, conclusão do tutorial, D1/D7, custo por jogador retido, crashes, opt-in de anúncios e conversão de compras. Alvo de CPI: ≤ US$0,70 no Android em BR/PH/ID (Origem §51).
- Pausar campanhas se a build tiver crash relevante, compra quebrada, métricas suspeitas ou eventos de analytics ausentes.
- Não comprar avaliações, não incentivar avaliações positivas em troca de recompensa e não usar tráfego fraudulento.
- Marketing não pode afirmar que a loja aprovou o app antes da aprovação.
- Não ampliar orçamento com base apenas em downloads baratos; observar retenção e qualidade da coorte.

---

# 20. SUPORTE AO JOGADOR E OPERAÇÕES APÓS LANÇAMENTO

- Criar e-mail/URL de suporte real, monitorado e aprovado pelo responsável (D-12).
- Criar FAQ para progresso perdido, compras não entregues, restaurar compra, anúncio sem recompensa, reembolso e exclusão de dados.
- Registrar versão do app, plataforma e ID técnico de transação de forma segura; nunca pedir senha, dados completos de cartão ou credenciais de loja.
- Fluxo de compra não entregue: consultar transação, verificar backend e entitlement, reconciliar idempotentemente e responder.
- Processo de hotfix: reproduzir, corrigir, executar testes de regressão, gerar build versionada e fazer release gradual quando disponível.
- Backup antes de mudanças de schema/configuração.
- Monitorar crash rate, erros de compra, falhas de anúncios, latência de backend e eventos de fraude.
- Preparar procedimento para desligar uma oferta defeituosa sem derrubar o jogo inteiro (kill switch do §15.3).
- Manter calendário de temporada e plano para término do evento; não deixar o passe preso em estado impossível.

---

# 21. MATRIZ DE PRIORIDADE E ESTADO

Estado auditado em 29/10/2026 no commit `59f0bb9` (§0.3). A evidência comum a todas as linhas é `git ls-files`: o repositório só tem documentos e pastas vazias.

| ID | Entrega | Prioridade | Evidência exigida | Estado em 29/10 | Depende de |
|---|---|---|---|---|---|
| P-01 | Projeto abre e compila na versão Unity fixada | Bloqueador | Log de build | `EM ANDAMENTO` (D-01 aprovada em 29/10; Fase A: esqueleto e `verify.sh full`) | D-01 aprovada; o PR da Fase A (`docs/CONTRATO_LANCAMENTO.md`) |
| P-02 | Loop completo de voo | Bloqueador | Teste PlayMode + vídeo/captura | `NÃO INICIADO` | P-01 |
| P-03 | Quadro de partidas e deadlines | Bloqueador | Testes de estado/tempo | `NÃO INICIADO` | P-01 |
| P-04 | Equipe móvel | Bloqueador | Teste funcional e de usabilidade | `NÃO INICIADO` | P-01; playtest com gente |
| P-05 | Três aeroportos e campanha | Obrigatório | Checklist de conteúdo e testes | `NÃO INICIADO` | Fase B; `docs/CAMPANHA.md` |
| P-06 | Rotas, aeronaves, incidentes e histórias | Obrigatório | Catálogo validado e testes | `NÃO INICIADO` | Fase B; D-13 |
| P-07 | Passe de temporada completo | Obrigatório | Catálogo, progressão e sandbox | `NÃO INICIADO` | `docs/TEMPORADA_S1.md`; trilha premium depende de D-05 |
| P-08 | Compras seguras e restore | Bloqueador | Validação sandbox/server-side | `BLOQUEADO` | D-05, D-06, Play Console (D-U-N-S, previsão 2026-11-25) |
| P-09 | Anúncios recompensados e limites | Bloqueador se monetização ativa | Modo de teste + teste de callback | `BLOQUEADO` | app no LevelPlay, que depende do nome e do appId (D-03) |
| P-10 | Save/migração/offline | Bloqueador | Testes de corrupção, tempo e reload | `NÃO INICIADO` | P-01 |
| P-11 | Privacidade e declarações de loja | Bloqueador | URLs e formulários revisados | `BLOQUEADO` | D-03, D-09, D-10, D-11, D-12; Play Console |
| P-12 | Artes finais e áudio | Obrigatório | Revisão dentro do jogo | `BLOQUEADO` | D-14 (aval dos lotes do Tripo); D-03 para logo e ícone |
| P-13 | Performance mobile | Bloqueador | Relatório de profiling | `NÃO INICIADO` | P-02; aparelho de entrada |
| P-14 | AAB/IPA assinados | Bloqueador | Artefatos e hashes | `BLOQUEADO` | AAB: P-01 e D-03 (a chave de upload leva o nome); envio: Play Console. IPA: macOS e conta Apple (D-04) |
| P-15 | Página de loja e criativos | Obrigatório | Pacote final | `BLOQUEADO` | D-03; build real para gravar |
| P-16 | Telemetria e crash reporting | Obrigatório | Eventos verificados em ambiente de teste | `NÃO INICIADO` | D-11 |
| P-17 | Proteção anti-fraude e segredos | Bloqueador | Checklist de segurança | `NÃO INICIADO` | P-08 para a parte de compras |
| P-18 | Teste de regressão final | Bloqueador | Relatório com todos os testes críticos | `NÃO INICIADO` | todas as anteriores |

Estados permitidos:

- `NÃO AUDITADO`
- `NÃO INICIADO`: auditado, ainda não existe, e nada externo impede começar.
- `EM ANDAMENTO`
- `CONCLUÍDO COM EVIDÊNCIA`
- `FALHOU`
- `BLOQUEADO`: depende de decisão, conta ou recurso externo.

Nunca usar apenas "quase pronto".

---

# 22. PROMPT OPERACIONAL PARA O CLAUDE

O texto abaixo deve ser utilizado como instrução operacional para o Claude, junto com este GDD e o acesso ao projeto.

> Você é o agente técnico responsável por levar este projeto Unity ao release candidate conforme este GDD. Comece auditando o repositório real; não suponha que as funcionalidades descritas já estejam implementadas.
>
> 1. Leia o `CLAUDE.md`, as regras do estúdio, este GDD e o GDD de origem (`docs/GDD_ORIGEM.md`). Leia o STATUS do jogo na memória do estúdio, se existir.
> 2. Inspecione o estado do repositório, versão do Unity, cena inicial, packages, build settings, scripts, assets, testes, serviços, configuração de anúncios/IAP e publicação.
> 3. Faça backup verificável e registre o estado do Git antes de editar.
> 4. Gere uma matriz de requisitos com status real, caminhos de arquivo, dependências, testes e evidências (`docs/CONTRATO_LANCAMENTO.md` e §21).
> 5. Descubra os comandos disponíveis nas CLIs autorizadas com ajuda/documentação real; não invente comandos.
> 6. Use o Tripo3D só com aval do Vinicius por lote, priorizando assets ausentes que realmente serão usados no lançamento. Não gere duplicatas sem motivo. Registre saldo, prompt e SHA em `docs/PROVENIENCIA.md`.
> 7. Preserve o projeto que funciona. Não faça reescrita total nem upgrade de Unity sem necessidade comprovada. Copie do Forge Street e do Dungeon Courier antes de escrever do zero.
> 8. Implemente em pequenos incrementos, execute `verify.sh` após cada mudança e corrija regressões.
> 9. Nunca exponha segredos, nunca comite credenciais, nunca conceda compra sem validação confiável e nunca use configuração de anúncio de produção durante QA.
> 10. Não marque como concluído algo sem teste/evidência. Não remova testes para obter uma pipeline verde.
> 11. Não reduza silenciosamente o escopo. Se o prazo for incompatível com o estado real do projeto, mostre os bloqueios e as evidências imediatamente.
> 12. No fim, produza `release_manifest.md`, relatório de testes, lista de produtos, lista de assets, checklist de loja, hashes dos artefatos e decisão `GO` ou `NO-GO`.
>
> A prioridade é entregar um jogo funcional, seguro e honesto, não apenas uma build que abre. Se validação de compras, privacidade, integridade de save ou qualquer teste bloqueador falhar, não habilite vendas reais nem declare o jogo pronto para publicação.

---

# 23. RISCOS E DECISÕES ABERTAS

## 23.1. Riscos

- **Prazo curto:** maior risco. A auditoria de 29/10 confirmou: não há projeto, e 29/10 é inviável (§0.3).
- **Contas das lojas:** a Play Console da V-STACK depende do D-U-N-S (previsão 2026-11-25); a burocracia de conta leva ~30 dias úteis. O iOS depende de macOS e de conta Apple, e este PC é Windows.
- **Validação do formato:** o jogo está NO-GO nesta rodada e só entra se o slot idle validar (Origem §51 e §53). Gastar o escopo completo antes da prova do bot (TA-1, TA-2) arrisca construir um diferencial que não funciona.
- **Assets gerados:** modelos podem ter artefatos, escala inconsistente ou termos de uso incompatíveis; revisar antes de integrar.
- **Compra sem backend:** risco de fraude; não ativar compras reais sem validação adequada.
- **SDKs:** dependências podem exigir versões específicas do Unity/Android/iOS; não atualizar às cegas.
- **Revisão das lojas:** depende das plataformas; submissão não garante aprovação na mesma data.
- **Nome:** "Little Terminal" é recomendação provisória; pesquisar colisões, marcas, package ID e domínio antes de fixar.
- **Nomes de companhias e aeroportos:** os nomes fictícios podem colidir com companhias reais (§0.4).
- **Temporada e ofertas:** datas erradas ou recompensas não definidas geram reclamações e problemas de revisão.
- **Dados pessoais:** SDKs de anúncios e analytics podem coletar dados além do esperado; revisar cada SDK.
- **ECA Digital:** em vigor desde 17/03/2026; o texto completo precisa de revisão jurídica para o público do jogo.
- **Anti-cheat:** sem autoridade server-side não existe proteção completa contra manipulação do cliente.
- **PC de 7,7 GB:** um processo pesado por vez (Unity, emulador, Blender ou Docker); isso alonga as Fases C e E.
- **Escopo versus prazo:** não confundir documento completo com implementação pronta.

## 23.2. Decisões que precisam ser confirmadas pelo responsável

As de D-01 a D-14 travam o início ou uma fase; as outras podem esperar.

| ID | Decisão | Recomendação | Trava |
|---|---|---|---|
| D-01 | Tirar o jogo do NO-GO da rodada e autorizar o projeto Unity neste repositório | autorizar só as Fases A e B; decidir o lançamento completo no portão da Fase B. **Aprovada pelo Vinicius em 2026-10-29: Fases A e B.** Ampliada no mesmo dia pelo prompt mestre do estúdio (`~/.claude/estudio/PROMPT_MESTRE_NOVOS_JOGOS.md`): a missão é levar o jogo até "só falta publicar", sem parar no portão da Fase B | P-01 e tudo depois |
| D-02 | Nova data-alvo no lugar de 29/10/2026 | definir depois do portão da Fase B, com a velocidade real; a Play Console não sai antes de ~25/11 | plano |
| D-03 | Nome comercial final e appId (`br.com.vstack.<nome>`), irreversíveis depois de publicados | "Little Terminal", depois da busca no INPI, USPTO, EUIPO e nas duas lojas | Setup, LevelPlay, chave de upload, ícone, loja |
| D-04 | Plataformas com conta e ambiente prontos para envio | Android no lançamento; iOS quando houver macOS e conta Apple | P-14 (IPA) |
| D-05 | Compras no lançamento: (a) só rewarded, IAP numa atualização; (b) IAP com validação no servidor | (b); se o backend não passar no §16.6, lançar como (a) | P-07 premium, P-08 |
| D-06 | Backend de validação | Cloud Run em projeto próprio, southamerica-east1, sem chave JSON | P-08 |
| D-07 | Gems no lançamento e o que elas compram | só com D-05 (b); sumidouros do §6.1 | §6.1, §7.3 |
| D-08 | Idiomas no lançamento | pt-BR + en no mínimo; es e id só com revisão humana | textos, loja, criativos |
| D-09 | Países do 1º lançamento | BR/PH/ID (a coorte do §15.2); EEE, Reino Unido e EUA depois | consentimento, loja |
| D-10 | Público-alvo e classificação etária | 13+, fora do programa Famílias, tratado como de acesso provável por adolescentes (ECA Digital), com revisão jurídica | Data safety, classificação |
| D-11 | Analytics em produção | Unity Analytics (UGS) com os eventos do §15.1, sem dado pessoal, e o Data safety atualizado; crash pelo Android vitals | P-16, K2 em produção |
| D-12 | E-mail de suporte e URL da política de privacidade | um endereço da V-STACK monitorado; política no GitHub Pages | P-11 |
| D-13 | Nomes das companhias aéreas e dos aeroportos | renomear os que colidem (§0.4) antes da arte de marca | arte e textos |
| D-14 | Aval dos lotes do Tripo | Lote 1 (≈ 400, teto 530) depois do portão da Fase B; Lote 2 (≈ 495) na Fase C. **Aprovada em 2026-10-29 pelo prompt mestre do estúdio (§3): os créditos acabam hoje, então os Lotes 1 e 2 saem hoje, pela lista fechada do `docs/ASSETS.md`** | P-12 |
| D-15 | Bônus de recuperação de voo por anúncio | fora do lançamento (§7.2) | — |
| D-16 | Publicação gradual ou imediata após aprovação | gradual (porcentagem pequena primeiro) | Fase E |
| D-17 | Autorização final de submissão/publicação em produção | — | envio |

---

# 24. REFERÊNCIAS OFICIAIS DE IMPLEMENTAÇÃO E POLÍTICAS

As páginas abaixo são referências para o agente validar os detalhes atuais antes de configurar SDKs ou preencher os consoles. As políticas mudam; não usar este documento como substituto de consulta à versão vigente.

1. **Apple — App Review Guidelines**
   https://developer.apple.com/app-store/review/guidelines/

2. **Unity — Unity In-App Purchasing**
   https://docs.unity.com/en-us/iap

3. **Unity — Receipt validation**
   https://docs.unity.com/en-us/iap/receipt-validation

4. **Android Developers — Segurança do Google Play Billing**
   https://developer.android.com/google/play/billing/security?hl=pt-BR

5. **Android Developers — Play Integrity API**
   https://developer.android.com/google/play/integrity/overview?hl=pt-br

6. **Google Play Console — Nível de API de destino**
   https://support.google.com/googleplay/android-developer/answer/11926878?hl=pt-br

7. **Google Play Console — Segurança dos dados**
   https://support.google.com/googleplay/android-developer/answer/10787469?hl=pt-BR

8. **Google Play — Política de pagamentos**
   https://support.google.com/googleplay/android-developer/answer/9858738?hl=pt-br

9. **Google — Anúncios recompensados no Unity/AdMob** (referência de boas práticas; a rede do jogo é o LevelPlay, cuja integração está em `dc:docs/MONETIZACAO.md`)
   https://developers.google.com/admob/unity/rewarded?hl=pt-BR

10. **Google Play — Políticas para famílias**
    https://support.google.com/googleplay/android-developer/answer/9893335?hl=pt-BR

11. **Brasil — Lei Geral de Proteção de Dados (LGPD), texto compilado**
    https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709compilado.htm

12. **Brasil — ECA Digital (Lei 15.211/2025)**, art. 20 (caixas de recompensa); em vigor desde 17/03/2026
    https://www.planalto.gov.br/ccivil_03/_ato2023-2026/2025/lei/L15211.htm

13. **Google Play Developer API — verificação de compra de produto**
    https://developers.google.com/android-publisher/api-ref/rest/v3/purchases.products/get

14. **Apple — App Store Server API**
    https://developer.apple.com/documentation/appstoreserverapi

A política e a implementação devem ser revisadas pela pessoa responsável, considerando o tratamento real de dados do jogo.

---

# 25. DEFINIÇÃO FINAL DE GO/NO-GO

O projeto recebe **`GO PARA SUBMISSÃO`** somente quando:

- todos os bloqueadores da matriz estão `CONCLUÍDO COM EVIDÊNCIA`;
- a build corresponde ao commit testado;
- as compras reais estão validadas ou, se não houver validação, permanecem desativadas;
- anúncios usam a configuração correta e recompensas não são duplicáveis;
- save, migração e offline passaram nos testes;
- dados e SDKs foram revisados para privacidade;
- materiais da loja refletem o jogo real;
- a build é assinada e reproduzível;
- não existem crashes conhecidos no fluxo principal;
- o responsável aprovou o envio.

Caso contrário, o estado é `NO-GO`, com lista explícita de bloqueios e plano de correção.

**Estado em 29/10/2026: `NO-GO`.** Os bloqueios estão no §0.3 e no §21; o plano, no §17.3; as decisões, no §23.2.

**Não confundir urgência com qualidade, e não transformar segurança em item opcional.**

---

# 26. REGISTRO DE ALTERAÇÕES

Toda mudança de escopo, monetização, privacidade, arquitetura ou segurança registra data, motivo, impacto, testes necessários e aprovação do responsável.

## v1.0 — 26/10/2026

Documento mestre escrito pelo Vinicius e colado no chat em 29/10/2026. Passou a ser o `docs/GDD.md`; o GDD anterior virou `docs/GDD_ORIGEM.md`, sem mudança de conteúdo.

## v1.1 — 29/10/2026 (Claude; aprovação do Vinicius: **pendente**)

Nenhuma mudança exige teste agora: ainda não há código. Cada uma vira caso de teste quando o sistema correspondente existir.

| # | Seção | Mudança | Motivo |
|---|---|---|---|
| 1 | cabeçalho, §0.3, §21, §25 | resultado da auditoria: NO-GO para 29/10, com evidências e matriz preenchida | o §17.3 da v1.0 pede o bloqueio visível no primeiro relatório |
| 2 | cabeçalho, §3.2 | Unity fixado em 6000.3.27f1 LTS | versão do DC; a v1.0 deixava em aberto |
| 3 | §0.4 | tabela de reconciliação com as regras do estúdio e a Origem | as regras do estúdio valem por cima; havia 14 pontos divergentes |
| 4 | §0.1, §3.1, §18.3 | o iOS exige macOS | o PC é Windows 10; a v1.0 previa IPA em 72 h |
| 5 | §2.3 | diferenciais ligados aos TA-1 a TA-7 | o custo e a prova de cada um já estavam na Origem |
| 6 | §2.4, §7.1, §9.2, §24 | ECA Digital (Lei 15.211/2025, art. 20) | base legal da regra contra loot box; a v1.0 só citava a LGPD |
| 7 | §4.2 | v0.1 como subconjunto do lançamento, com portão do bot | o processo do estúdio exige o contrato da v0.1 antes do código |
| 8 | §4.3 | lista do que a Origem previa e fica fora do lançamento | o §0.2 proíbe reduzir escopo em silêncio |
| 9 | §5.4 | perder voo custa só a receita dele na progressão principal; horário em tempo de jogo | invariante da Origem §50 e reprodutibilidade no bot |
| 10 | §5.5 | 3 funcionários no início; 6 perfis propostos, sem sorteio | a v1.0 não listava os perfis |
| 11 | §5.7, §0.4, D-13 | distribuição das 12 rotas; checagem dos nomes de companhias | Atlas, Aurora e BlueWing lembram companhias reais |
| 12 | §5.8 | reparo só com Cash; frente fria com 60 s e aviso de 30 s | evitar "pagar para resolver" e usar o número do TA-4 |
| 13 | §6.1 | Gems condicionadas a IAP validado; sumidouros definidos; XP e pontos de temporada não são moedas | regra de 1 moeda no MVP; a v1.0 não dizia o que as Gems compram |
| 14 | §6.3 | trava de 2 h no código, Δt ≤ 0 paga 0, teto relativo, id de transação | detalhe da Origem; resolve o conflito 1 dela |
| 15 | §6.4 | premium retroativo, venda fechada nas últimas 72 h, ritmo de pontos proposto | a v1.0 pedia "pontos necessários" sem números |
| 16 | §7.2 | LevelPlay sem `AD_ID` no lugar de AdMob/UMP; definição de "alto valor"; recuperação de voo fora | regra do estúdio; a v1.0 deixava o SSV sem critério |
| 17 | §7.3 | welcome bundle não consumível; ID da Play não se reaproveita | garante 1 por conta sem login |
| 18 | §7.4 | APIs de validação e de reembolso nomeadas; limite honesto do backend sem login; Cloud Run recomendado | a v1.0 pedia backend sem dizer qual nem o que ele protege |
| 19 | §7.5, §15.3 | catálogo local; kill switch por JSON que só desliga | sem backend, a v1.0 não tinha como desligar uma oferta |
| 20 | §8, §9 | repositório público, gitleaks, flags de teste fora do release, sem ATT, países BR/PH/ID | ajustes ao ambiente real |
| 21 | §10.2 | pipeline do estúdio, aval por lote, estimativa do Lote 2 | regra do estúdio e regra global de créditos |
| 22 | §13 | serviços da v1.0 mapeados para os arquivos do Forge e do DC | evitar criar 20 serviços do zero |
| 23 | §15.1 | nomes de eventos unificados com o diário da Origem | havia dois nomes para o mesmo evento |
| 24 | §16.0 | onde cada teste roda no portão do estúdio | a v1.0 usava PlayMode; o estúdio usa `-autoplay` e `verify.sh` |
| 25 | §17 | Fase 0 de decisões; plano de datas trocado por portões | 29/10 é inviável |
| 26 | §19.4 | criativo D (história) | o mais barato e emocional, segundo a Origem |
| 27 | §21 | estado `NÃO INICIADO` | separar "não existe" de "bloqueado por terceiro" |
| 28 | §23.2 | decisões numeradas D-01 a D-17, com recomendação | a v1.0 tinha uma lista sem recomendação nem ordem |
| 29 | §21, §23.2, `CLAUDE.md` | D-01 aprovada (aval do Vinicius em 29/10/2026: "D-01 approved: open Phase A (v0.1 contract and skeleton copied from Forge + DC) in a new chat"). Fases A e B autorizadas; P-01 passa a `EM ANDAMENTO`. Impacto: começa o projeto Unity em `client/` e os contratos `docs/CONTRATO_V01.md` e `docs/CONTRATO_LANCAMENTO.md`. Testes: `client/tools/verify.sh full` verde no PR da Fase A | o §17.3 exigia D-01 para abrir a Fase A |
| 30 | §23.2 (D-01, D-14), `CLAUDE.md` | O Vinicius mandou seguir o prompt mestre do estúdio (29/10/2026: "leia e pode seguir as orientações"). D-01 ampliada até "só falta publicar"; D-14 aprovada (Tripo hoje, os créditos acabam hoje). O `CLAUDE.md` importa o prompt. Impacto: arte antes do portão da Fase B (risco do §23.1: se o TA-2 falhar no bot, parte da arte pode sobrar). Testes: os do `CONTRATO_V01.md` §8 seguem valendo | o crédito do Tripo vence hoje |

---

**FIM DO GDD MESTRE**

Este documento deve ser atualizado quando uma decisão for alterada. Toda mudança de escopo, monetização, privacidade, arquitetura ou segurança deve registrar data, motivo, impacto, testes necessários e aprovação do responsável (§26).
