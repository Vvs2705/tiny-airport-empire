# Conta de jogador opcional (Google Play Games e Facebook) e save na nuvem

**Retrato de 2026-11-05** (frente de contas). Pedido do Vinicius no mesmo dia: contas com Google, Facebook e outros, por segurança, para salvar tudo, ter o histórico das compras e, no futuro, cooperação entre jogadores. Ele quer o mesmo padrão nos próximos jogos. Aprovou o login **opcional** ("pode seguir desta forma") e pediu um **brinde ao ligar a conta** (D-26 no GDD §23.2). Contas, senhas, painéis de terceiros e publicação ficam com ele. Este documento diz **os nomes e onde colocar**, nunca os valores.

## 1. Como funciona

1. **O jogo abre anônimo.** A Unity Authentication cria uma conta anônima, ou reabre a sessão guardada no aparelho. Sem rede, o jogo segue só com o save local, como antes.
2. **Ligar a conta** (Pausa › Configurações › Conta): o jogador toca em Conectar no Google Play Games ou no Facebook. O login vai para a **mesma** conta, e o progresso do aparelho sobe para a nuvem na hora. Na primeira ligação vem o **brinde** (§3).
3. **Login que já tem outra conta** (reinstalou ou trocou de celular): a tela "Conta já existente" oferece carregar o progresso dessa conta. O progresso deste aparelho é substituído, o jogo grava o save da nuvem e fecha. Ao abrir de novo, o jogo continua de onde o jogador parou. Recarregar sem fechar trocaria o `Sim` de um mundo já montado, e isso não foi medido no Unity.
4. **Com a conta ligada,** o save sobe depois de gravar (no máximo 1 vez por minuto, e na hora ao pausar). Ao abrir, ele é comparado com o da nuvem (§2).
5. **Excluir meus dados,** com a conta: o servidor apaga o histórico de compras, a nuvem apaga o save e a Unity apaga a conta. Só então o aparelho é apagado. Com a conta ligada e sem rede, nada é apagado e o jogo pede para tentar de novo. Sem conta ligada, o aparelho é apagado mesmo sem rede: a conta anônima que sobra na UGS não aponta para ninguém. Pela web: `docs/privacidade/excluir-conta/`, exigido pela Play e pelo Facebook.
6. **Compras:** com a conta, o `LojaIap` manda o token dela ao servidor (`Authorization: Bearer`). O servidor confere o token e anota a compra no histórico da conta. `GET /v1/conta/compras` devolve esse histórico. O crédito continua igual (`docs/LOJA_SERVIDOR.md`): pelo tx, 1x, pela tabela.

## 2. Conflito de save (GDD §14.3)

`Conflito.Decide`, com o progresso igual a `TotalEmbarcados` (só cresce) e a data do envelope:

| Aparelho × nuvem | Fica |
|---|---|
| um dos dois vazio (sem save ou 0 embarcados) | o outro |
| iguais | nada muda |
| um à frente em progresso **e** em data | ele |
| um com mais progresso, o outro mais novo (jogou em 2 aparelhos sem rede) | **o jogador escolhe** ("Dois progressos", com os embarcados de cada um); nada sobe enquanto a pergunta está aberta |
| a nuvem tem schema mais novo (outro aparelho já atualizou o jogo) | a nuvem, que este build não abre: aviso "Atualize o jogo" e nada sobe por cima |

Nunca escolhe calado contra o progresso maior, e nunca escolhe pelo maior saldo (GDD §14.3).

## 3. Brinde de ligar a conta (BALANCE.md §D-CT)

**100 Gems e 30 min de 2x de renda,** fixos, sem sorteio e sem nada pago. É **uma vez por save**, e o save é o da conta, porque viaja com ela. Desligar e ligar de novo, ligar o segundo login ou trocar para uma conta que já existia não paga outra vez. O booster não passa do teto do passe nem encurta um que já está ligado. `Sim.DaBrindeConta()`, save **schema 19** (`brc=1`), `ContaBrindeTests` (7 testes, com a migração do 18).

## 4. Arquitetura

| Peça | Onde | Prova |
|---|---|---|
| Módulo reaproveitável (núcleo) | `client/Assets/Vstack/Conta/Core/Conta.cs` (`Vstack.Conta.Core`, C# puro): `Contas`, `Conflito`, `IServicoConta`, `INuvem` | `Vstack/Conta/Tests/ContaTests.cs`: 15 testes no `dotnet test` |
| Cola com a UGS | `Vstack/Conta/Unity/ContaUgs.cs`, atrás de `#if VS_UGS` (Google com `VS_GPGS`, Facebook com `VS_FACEBOOK`); `Fabrica.cs` sem `#if` | compila na rodada do PC (§6) |
| Cola com o jogo | `View/ContaJogo.cs` (eventos para a tela, brinde, carregar e excluir), `View/Hud.Conta.cs` (cartão em Configurações e as 3 folhas) e os ganchos no `Game.cs` | viewcheck; tela na rodada do PC |
| Brinde | `Core/Sim.Conta.cs` | `ContaBrindeTests` |
| Servidor | `servidor/src/conta.js` (JWT RS256/ES256 com o JWKS da Unity, sem dependência), `servidor/src/historico.js` (Firestore `jogadores/{hash}/compras/{hash do token}`), rotas no `app.js` | `test/conta.test.js`: 9 testes (token forjado, vencido, outro projeto, alg trocado, `alg: none`, corpo mexido; histórico por conta; exclusão; id fora do log) |
| Portão | `checa_repo`: o `Core/` do módulo sem Unity, rede atrás de `VS_UGS` cobrada na política quando o pacote entra, `FB.Init` sem desligar eventos e id de publicidade derrubado | `checa_repo --selftest` |

Como levar para outro jogo: `client/Assets/Vstack/Conta/README.md`.

**Firebase Auth foi avaliado e descartado.** Ele é grátis e o token também se confere no Cloud Run, mas o SDK Unity é pesado (EDM4U, dependências do Play Services que brigam com as do LevelPlay, APK maior) e não traz save na nuvem sem o Firestore no cliente. A UGS já tem o projeto criado (`4a7c824c-0202-45e6-86c4-b6de3ca3ae98`), está no mesmo ecossistema do Unity IAP e do LevelPlay, e o Cloud Save vem junto.

**Limites honestos:** o save continua no aparelho e pode ser editado; a nuvem guarda o que o aparelho mandou. A autoridade de Gems no servidor (Cloud Code) fica para quando houver cooperação, ranking ou troca entre jogadores. Para isso a conta já está pronta: o servidor sabe de quem é cada pedido.

## 5. Dados e privacidade

- **Coletado:** o ID do jogador na UGS (gerado por ela) e o save do jogo (progresso), na nuvem da Unity. Com o Google Play Games, a Unity recebe o código do login. Com o Facebook, recebe o token do login, e o jogo pede **só o perfil público**: nem e-mail, nem amigos. Nada disso vai para o diário, para a telemetria ou para o log do servidor. O servidor guarda só o SHA-256 do id.
- **Data safety:** `docs/SEGURANCA_PRIVACIDADE.md` §3.4. **Política:** o texto pronto está no mesmo §3.4. Ele entra nas duas políticas no PR que ligar o pacote da UGS, e o `checa_repo` cobra.
- **SDK do Facebook:** sem eventos automáticos e sem o id de publicidade (no `FacebookSettings` e no código). O `AD_ID` continua fora do manifesto, e o `checa_apk` derruba se ele voltar.

## 6. O que falta, e quem faz

### Rodada do PC (Unity), uma vez

1. `manifest.json`: `com.unity.services.authentication`, `com.unity.services.cloudsave` e `com.unity.modules.unitywebrequest` (este já vem com a loja). O `VS_UGS` liga sozinho.
2. Google Play Games: o pacote `com.google.play.games` (tgz oficial) e o assembly dele no `Vstack.Conta.Unity.asmdef`. O id do jogo no Play Games vai no menu Window › Google Play Games › Setup, e esse arquivo fica fora do git.
3. Facebook: o SDK do Facebook para Unity, o `VS_FACEBOOK` nas Player Settings (Android), App ID e Client Token no `FacebookSettings`, que fica fora do git, e Auto Logging e Advertiser ID desligados.
4. As 2 políticas com o texto do `SEGURANCA_PRIVACIDADE.md` §3.4 (o `checa_repo` só passa com isso). Depois, `verify.sh full` e `verify.sh android`; o `checa_apk` tem de continuar sem `AD_ID`.
5. Medir no Editor: ligar, trocar de conta, a pergunta de 2 progressos e excluir. No celular, isso fica para o teste do POCO F4.

### Com o Vinicius (navegador do PC; o Claude abre as páginas)

1. **UGS** ([cloud.unity.com](https://cloud.unity.com), projeto "Little Terminal"): ligar Authentication e Cloud Save; em Authentication › Identity providers, adicionar Google Play Games (com o client ID e o client secret do OAuth, que ficam só no painel) e Facebook (App ID e App Secret, só no painel).
2. **Facebook for Developers** ([developers.facebook.com/apps](https://developers.facebook.com/apps)): criar o app "Little Terminal" (tipo Consumidor, caso de uso Login do Facebook), plataforma Android com o pacote `br.com.vstack.littleterminal` e o hash da chave, política `https://vvs2705.github.io/tiny-airport-empire/privacidade/` e exclusão de dados `https://vvs2705.github.io/tiny-airport-empire/privacidade/excluir-conta/`. Só o `public_profile` (sem revisão do app).
3. **Play Console** (depois do D-U-N-S, por volta de 25/11): Play Games Services › configurar, com a credencial OAuth do tipo "servidor de jogos" para a UGS; em "Conteúdo do app", a exclusão de conta com a URL acima e o Data safety do §3.4.
4. **Servidor:** `gcloud run services update lt-validacao-teste --region southamerica-east1 --update-env-vars PROJETO_UGS=4a7c824c-0202-45e6-86c4-b6de3ca3ae98`. No serviço de produção, também `AMBIENTE_UGS=production` e, quando o build com conta estiver na loja, `EXIGE_CONTA=1`.

## 7. Impacto no prazo

O código fica pronto na nuvem, dentro da meta de 8 a 10/11. A rodada do PC leva cerca de 2 h. A parte do Vinicius leva cerca de 2 h, quase toda depois do D-U-N-S: o Play Games depende da Play Console, e o Facebook pode ser feito antes. O lançamento não atrasa, porque o jogo funciona inteiro sem conta e cada provedor só aparece quando está configurado.
