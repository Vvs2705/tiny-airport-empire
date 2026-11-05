# Loja, servidor de validação e anúncio em modo de teste

**Retrato de 2026-11-05** (frente "loja e servidor em modo de teste" da fila C). Segue o GDD §7.2 a §7.4, §8.2, §8.5, §8.7, §16.3 e §16.6, o `CONTRATO_FASE_D1.md` e o `docs/SEGURANCA_PRIVACIDADE.md`. Contas, senhas, pagamento, chaves e publicação ficam com o Vinicius: este documento diz **os nomes e onde colocar**, nunca os valores.

## 1. O que está pronto

| Peça | Onde | Prova |
|---|---|---|
| Servidor de validação, sem dependência de terceiros, pronto para o Cloud Run | `servidor/` (Node 22) | `node --test` em `servidor/` (CI `nucleo`, job `servidor`): 21 testes com os casos de §16.3 e §16.6 |
| Loja falsa no servidor, para o PC, o Editor e o K2 antes da Play Console | `servidor/src/falso.js`, `AMBIENTE=falso` | `app.test.js` |
| Compra em 2 passos no núcleo: validar, creditar, gravar e só então confirmar na loja | `Core/Loja.cs` | `LojaTests` (11 testes) |
| Cola com o Unity IAP 5 e o `UnityWebRequest` | `View/LojaIap.cs`, atrás de `#if TA_IAP` | compila na rodada do PC (§5) |
| Rewarded do LevelPlay, não personalizado, test suite só em teste | `View/AnunciosLevelPlay.cs`, atrás de `#if TA_LEVELPLAY` | `checa_repo` (anúncio): `Init` só depois de `SetConsent(false)` e `do_not_sell`; compila na rodada do PC |
| Configuração fora do git | `Resources/loja_config.json` (no `.gitignore`), lida por `View/LojaConfig.cs` | §4 |

**Por que os adaptadores estão atrás de `#if`:** a nuvem não tem Unity, e um pacote novo no `manifest.json` só se resolve abrindo o projeto. Sem os pacotes, os dois arquivos não existem para o compilador, o jogo continua com o anúncio simulado e sem loja, e o `checa_repo` não cobra módulo nem política por eles. Quando o pacote entra e o `TA.asmdef` liga o define, o `checa_repo` passa a cobrar o módulo `unitywebrequest` e a citação na política, sozinho.

## 2. Como a compra anda (GDD §7.4)

1. O jogador toca em comprar; o Unity IAP abre a Google Play (`LojaIap.Compra`).
2. A loja entrega a compra (`OnPurchasePending`) com o `purchaseToken`. A View só repassa: `Loja.Recebida(token, produto, ...)`. Pagamento pendente (boleto) fica em **Esperando** e não vai ao servidor.
3. A View manda um pedido por vez ao servidor: `POST /v1/compras/validar` com `{"produto", "token"}`.
4. O servidor consulta a Google (`purchases.products.get`) com a conta de serviço do Cloud Run, sem chave JSON, e decide: ambiente certo, comprado (não pendente, não cancelado, não reembolsado), consumível ainda não consumido, produto igual ao pedido. Registra o token por SHA-256 no Firestore e conta as vezes.
5. `ok` traz o `tx` (o orderId). O núcleo credita **pelo tx e pela tabela** (`Sim.Creditar`, 1x por tx); nenhuma quantidade vem do cliente nem do servidor.
6. A View grava o save. Só então `Loja.Gravado()` libera o acknowledge, e a loja confirma (`ConfirmPurchase`; o consumível é consumido).
7. Servidor fora, 429 ou 503: espera crescente de 2 s, dobrando até 5 min, sem crédito. Ao voltar, credita. Fechar o app antes do acknowledge: a loja reentrega ao abrir, o servidor diz `ok` de novo com o mesmo tx, o núcleo responde `Repetido` e a compra é confirmada sem pagar 2x.
8. Recusado (token forjado, cancelado, reembolsado, consumido, compra de teste em produção): nada creditado nem confirmado; sem acknowledge, a Google devolve o dinheiro em 3 dias.
9. Restaurar compras (reinstalou): a loja devolve os não consumíveis já confirmados, o servidor diz `ok` e o núcleo devolve a posse (e o pacote de boas-vindas volta inteiro com o save novo, o que é restauração e não duplicação, GDD §7.3). Não confirma de novo.

**Com a conta opcional** (`docs/CONTAS.md`, 2026-11-05): o `LojaIap` manda o token da conta (`Authorization: Bearer`), o servidor confere pela chave pública da Unity e anota a compra no histórico da conta; 401 nunca vira recusa no cliente (tenta de novo). Variáveis novas: `PROJETO_UGS`, `AMBIENTE_UGS`, `EXIGE_CONTA`.

**Limite honesto (GDD §7.4):** sem login, o servidor impede compra forjada e token reaproveitado depois de consumido, mas não impede quem edita o próprio save. Um consumível apresentado mais de 5 vezes sem ser consumido é recusado (`repetido`).

## 3. Ambientes e modo de teste

| `AMBIENTE` | Loja | Registro | Aceita | Para quê |
|---|---|---|---|---|
| `falso` | a loja falsa: o token diz o estado (`falso-ok-1`, `falso-pendente-1`, `falso-cancelado-1`, `falso-consumido-1`, `falso-real-1`, `falso-erro-1`) | memória | só "compra de teste" | PC, Editor, K2 antes da Play Console; nunca fala com a Google |
| `teste` | Google Play (testadores de licença) | Firestore se houver `PROJETO`, senão memória | **só** compra de teste (`purchaseType` 0) | QA com a Play Console: nenhuma compra real vira crédito |
| `producao` | Google Play | Firestore (obrigatório) | só compra real | o build da loja; compra de teste é recusada (`ambiente`) |

O cliente escolhe o servidor pelo `loja_config.json`; build de desenvolvimento é sempre "teste" (servidor de teste, anúncio de teste e test suite). Um serviço por ambiente no Cloud Run: `lt-validacao-teste` e, só no lançamento, `lt-validacao`.

Rodar no PC: `cd servidor && set AMBIENTE=falso && node src/index.js` (porta 8080; no Git Bash, `AMBIENTE=falso node src/index.js`).

## 4. `loja_config.json` (fora do git)

Em `client/Assets/_TA/Resources/loja_config.json`, só no PC que gera o build:

```json
{ "servidor": "https://<url do Cloud Run>", "levelplayApp": "<app key>", "levelplayRecompensado": "<ad unit do rewarded>", "teste": true }
```

Nenhum desses valores é segredo (todos vão dentro do APK), mas são da conta da V-STACK, então ficam fora do repositório público. `"teste": false` só no build de release que vai para a loja. Sem o arquivo, o jogo fica como hoje.

## 5. O que falta, e quem faz

### Com o Vinicius (conta, pagamento, chave)

1. **Projeto GCP do jogo** (D-06): criar `little-terminal` (ou outro id) em [console.cloud.google.com/projectcreate](https://console.cloud.google.com/projectcreate), ligado à conta de faturamento da Vstack, com **alerta de orçamento** em [Faturamento › Orçamentos e alertas](https://console.cloud.google.com/billing/budgets) (sugestão: R$ 10/mês, alerta a 50%, 90% e 100%).
2. **Subir o servidor de teste** (depois do item 1; o Claude roda pelo PC se ele quiser): no PC, com o `gcloud` logado na conta dele,
   ```
   gcloud config set project <id do projeto>
   gcloud services enable run.googleapis.com firestore.googleapis.com androidpublisher.googleapis.com cloudbuild.googleapis.com
   gcloud firestore databases create --location=southamerica-east1
   gcloud iam service-accounts create lt-validacao --display-name "Little Terminal validacao"
   gcloud projects add-iam-policy-binding <id do projeto> --member serviceAccount:lt-validacao@<id do projeto>.iam.gserviceaccount.com --role roles/datastore.user
   gcloud run deploy lt-validacao-teste --source servidor --region southamerica-east1 --service-account lt-validacao@<id do projeto>.iam.gserviceaccount.com --allow-unauthenticated --max-instances 2 --concurrency 20 --memory 256Mi --set-env-vars AMBIENTE=falso,PROJETO=<id do projeto>
   ```
   Começa em `AMBIENTE=falso` (o K2 já pode usar). Quando a Play Console existir, troca para `AMBIENTE=teste` com `gcloud run services update lt-validacao-teste --region southamerica-east1 --update-env-vars AMBIENTE=teste`.
3. **Play Console** (depois do D-U-N-S): criar os 7 produtos com os ids exatos do `Tabelas.Produtos` (consultar antes; id criado não se reaproveita), os testadores de licença, e convidar a conta de serviço `lt-validacao@<id do projeto>.iam.gserviceaccount.com` em Usuários e permissões, só com "Ver informações financeiras" e "Gerenciar pedidos" deste app.
4. **LevelPlay:** criar o app `br.com.vstack.littleterminal` na conta da V-STACK, com um ad unit **rewarded**, e marcar "não direcionado a crianças". O app key e o id do ad unit vão no `loja_config.json` do PC (§4), não no chat nem no git. Conferir no painel que nenhuma rede mediada exige `AD_ID`.
5. **Kill switch:** `--update-env-vars DESLIGADO=1` desliga toda validação (nada é concedido); `DESLIGADO=0` religa.

### Rodada do PC (Unity), uma vez

1. `client/Packages/manifest.json`: `com.unity.purchasing` (5.x, a do DC), `com.unity.services.levelplay` (a do DC) e `com.unity.modules.unitywebrequest`.
2. `client/Assets/_TA/Scripts/View/TA.asmdef`: em `references`, os assemblies dos dois pacotes (os nomes aparecem no asmdef de cada pacote, em `Library/PackageCache`); em `versionDefines`:
   ```json
   [{ "name": "com.unity.purchasing", "expression": "5.0.0", "define": "TA_IAP" },
    { "name": "com.unity.services.levelplay", "expression": "8.0.0", "define": "TA_LEVELPLAY" }]
   ```
3. Ligar no `Game.cs`: `new Loja(() => Sim)`, `LojaIap` com `Grava` devolvendo se gravou, `Tick()` no `Update`; o anúncio real no lugar do simulado quando `AnunciosLevelPlay.Pronto` (o simulado continua sem config). A tela da loja é da frente `ta-loja`.
4. `verify.sh full` e `verify.sh android`: o `checa_apk` tem de continuar sem `AD_ID` (o LevelPlay e o Google Mobile Ads tentam pôr; o `Setup.SemIdDePublicidade` tira) e só com as permissões da lista.
5. **No mesmo PR**: as duas políticas (`docs/privacidade/`, `docs/privacy/`) com o texto do §6, o `docs/SEGURANCA_PRIVACIDADE.md` §3.2 como a coluna do build, e o `CONTRATO_LANCAMENTO.md`. O `checa_repo` derruba o PR se a política não citar os pacotes e o `UnityWebRequest`.
6. Testes de §16.6 no aparelho, com testadores de licença e o servidor em `teste`: comprar, cancelar, pendente (cartão de teste "aprova depois"), servidor fora (modo avião) e de volta, reabrir antes do acknowledge, reinstalar e restaurar, reembolso pela Play Console. Anúncio: só a test suite e anúncios de teste (`-adtestsuite`); nunca clicar em anúncio de produção.

## 6. Texto da política para quando os pacotes entrarem (rascunho, sujeito à revisão jurídica D-10; o prazo de 5 anos é sugestão)

**pt-BR:**

> **Compras no app.** Quando você compra algo no jogo, a Google Play processa o pagamento; nós não recebemos dados do cartão. O jogo envia ao nosso servidor de validação (Google Cloud Run, região São Paulo), pela biblioteca `UnityWebRequest` e o Unity IAP (`com.unity.purchasing`), apenas o identificador do produto e o comprovante da compra, para conferir com a Google que ela é verdadeira e entregar o item uma vez. Guardamos um resumo criptográfico (SHA-256) do comprovante, o produto e a data, para impedir que a mesma compra seja usada duas vezes, por até 5 anos ou até você pedir a exclusão pelo e-mail de suporte. Não usamos isso para publicidade.
>
> **Anúncios com recompensa.** Se você escolher assistir a um vídeo para ganhar uma recompensa, o LevelPlay da Unity (`com.unity.services.levelplay`) e as redes de anúncio dele recebem dados técnicos do aparelho, o endereço IP (local aproximado) e a interação com o anúncio, para mostrar o anúncio, medir e prevenir fraude. Os anúncios são **não personalizados para todos**, e o jogo não usa o ID de publicidade do aparelho.

**English:**

> **In-app purchases.** When you buy something in the game, Google Play processes the payment; we never receive card details. The game sends our validation server (Google Cloud Run, São Paulo region), through `UnityWebRequest` and Unity IAP (`com.unity.purchasing`), only the product id and the purchase receipt, to check with Google that the purchase is genuine and deliver the item once. We keep a cryptographic digest (SHA-256) of the receipt, the product and the date, to stop the same purchase from being used twice, for up to 5 years or until you ask for deletion through the support e-mail. We do not use this for advertising.
>
> **Rewarded ads.** If you choose to watch a video for a reward, Unity LevelPlay (`com.unity.services.levelplay`) and its ad networks receive technical device data, your IP address (approximate location) and your interaction with the ad, to serve the ad, measure it and prevent fraud. Ads are **non-personalized for everyone**, and the game does not use the device advertising ID.

## 7. Testes do GDD §16.3 e §16.6 que já têm prova

| Caso | Onde |
|---|---|
| Repetir o mesmo identificador de compra / callback repetido | `LojaTests.CallbackRepetido_E_Reentrega_CreditamUmaVez`, `app.test.js` (mesmo token, mesmo tx), `ComprasTests` |
| Compra pendente | `LojaTests.PagamentoPendente_EsperaALoja`, `decide.test.js`, `app.test.js` |
| Cancelada, reembolsada (revogada), consumida | `decide.test.js`, `app.test.js`, `LojaTests.Recusada_NaoCreditaNemConfirma` |
| Backend indisponível e depois reconectado | `LojaTests.ServidorFora_EsperaCrescente_E_CreditaAoVoltar`, `app.test.js` (503 sem detalhe) |
| Restaurar depois de reinstalar | `LojaTests.Restaurar_DevolveAPosse_SemConfirmarDeNovo`, `decide.test.js` |
| Produto desconhecido, id inválido, payload grande, campo de quantidade | `app.test.js`, `LojaTests.Entrada_Invalida_Ignorada` |
| Nenhuma compra de produção durante o QA / compra de teste em produção | `decide.test.js`, `app.test.js` (ambiente) |
| Endpoints administrativos não existem | `app.test.js` (rotas) |
| Nada de token nem tx no log | `app.test.js` |
| Limite de taxa e kill switch | `app.test.js`, `play.test.js` |
| Anúncio não personalizado antes do Init | `checa_repo --selftest` (anúncio) |
| Recompensa de anúncio 1x, não duplicada ao reabrir, sem recompensa ao fechar antes | `Anuncios.cs` + `ExploitTests`/`CoreTests` (já existiam); `AnunciosLevelPlay` paga só no `OnAdRewarded` |
| Preço e moeda localizados, passe no meio da temporada, aparelho | rodada do PC e Play Console (§5) |
