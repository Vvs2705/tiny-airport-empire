# Vstack.Conta: conta opcional para os jogos do estúdio

Módulo reaproveitável de conta de jogador. O jogo começa com uma **conta anônima**, e o jogador liga a conta ao **Google Play Games** ou ao **Facebook** quando quiser, sem perder o progresso. Com a conta ligada, o save vai para a nuvem e volta em outro aparelho. A exclusão de conta, que a Play exige, já vem pronta. Nada aqui é do Little Terminal: o jogo entra só com o formato do save dele (`MetaSave`) e o endereço do servidor.

O desenho e as decisões estão em `docs/CONTAS.md` do Little Terminal.

## O que tem

| Pasta | Assembly | O que é | Testes |
|---|---|---|---|
| `Core/` | `Vstack.Conta.Core` (C# puro, `noEngineReferences`) | `Contas` (abrir, ligar, trocar de conta, nuvem com ritmo e espera crescente, excluir), `Conflito.Decide` e as interfaces `IServicoConta` e `INuvem` | `Tests/ContaTests.cs` (dotnet test, sem Unity) |
| `Unity/` | `Vstack.Conta.Unity` | `ContaUgs`, a cola com a Unity Authentication e o Cloud Save, atrás de `#if VS_UGS`, e a `Fabrica` (sem `#if`), que devolve `null` sem a UGS | compila na rodada do PC |
| `Tests/` | `Vstack.Conta.Tests` (EditMode) | os testes do `Core/` | |

O servidor tem o par em `servidor/src/conta.js` (confere o token da Unity sem dependência de terceiros) e `servidor/src/historico.js` (histórico de compras por conta e exclusão). Os dois também servem para outro jogo sem mudança.

## Como levar para outro jogo

1. Copie `client/Assets/Vstack/Conta/` inteira, com os `.meta`. O GUID repetido entre projetos diferentes não é problema.
2. No asmdef da View do jogo, em `references`, ponha `Vstack.Conta.Core` e `Vstack.Conta.Unity`. Nos testes de núcleo fora do Unity (`dotnet test`), inclua `Core/**/*.cs` e `Tests/**/*.cs`.
3. No PC, com o Unity:
   - `com.unity.services.authentication` e `com.unity.services.cloudsave` no `manifest.json`. O `VS_UGS` liga sozinho, pelo `versionDefines` do `Vstack.Conta.Unity.asmdef`.
   - Google Play Games: o pacote `com.google.play.games` (tgz do GitHub `playgameservices/play-games-plugin-for-unity`) liga o `VS_GPGS`. Ponha o assembly dele em `references` do `Vstack.Conta.Unity.asmdef`.
   - Facebook: importe o SDK e ponha `VS_FACEBOOK` em Player Settings > Scripting Define Symbols (Android). Em `FacebookSettings`, desligue **Auto Logging App Events** e **Advertiser ID Collection**. O `ContaUgs` também desliga isso no código, e o `checa_repo` trava o `FB.Init` sem isso. Depois do build, o `checa_apk` precisa continuar sem `AD_ID`.
   - Se o Unity reclamar de assembly faltando, confira os nomes em `Library/PackageCache/<pacote>/Runtime/*.asmdef`.
4. No jogo, crie com `Fabrica.Cria(ambienteUgs, meta)`. `null` quer dizer build sem UGS: esconda a tela. Chame `Abrir` ao abrir, `Gravou` depois de cada save, `Tick` no Update e `Tick(agora, pausa: true)` ao pausar. Quando a tela pede, chame `Ligar`, `TrocarPara`, `Resolve` e `Excluir`. O exemplo completo está em `client/Assets/_TA/Scripts/View/ContaJogo.cs`.
5. Brinde: `Passo.PrimeiraLigacao` diz que a conta ganhou o primeiro login. O jogo dá o brinde dele **uma vez por save**, porque o save é o da conta e viaja com ela.
6. Painéis, feitos pelo Vinicius no navegador (o Claude abre as páginas):
   - UGS: Authentication > Identity providers > Google Play Games e Facebook.
   - Facebook for Developers: o app do jogo, com a URL da política e a URL de exclusão de dados.
   - Play Console: Play Games Services, com a credencial OAuth do jogo.
7. Servidor: `PROJETO_UGS` (e `AMBIENTE_UGS=production` no serviço de produção). Ligue `EXIGE_CONTA=1` quando o build com conta estiver na loja.
8. Privacidade: a política e o Data safety passam a declarar o **ID do usuário** (conta do jogo) e o login do Google/Facebook. Use o modelo de `docs/SEGURANCA_PRIVACIDADE.md` §3.3 e a página de exclusão `docs/privacidade/excluir-conta/`.

## Regras do módulo

- O id do jogador nunca vai para o diário, para a telemetria ou para o log do servidor. O servidor guarda só o SHA-256 dele.
- Só se pede o perfil público ao Facebook, nunca e-mail ou amigos. O Play Games não entrega e-mail.
- Sem rede, nada se perde: o save local continua valendo, e a subida tenta de novo com espera de 2 s dobrando até 5 min.
- Conflito: o save vazio sempre perde. Se um save está à frente em progresso e em data, ele fica. Se um tem mais progresso e o outro é mais novo, o jogador escolhe. A escolha nunca é feita calada contra o progresso maior, e nada sobe enquanto a pergunta está aberta.
- Excluir segue a ordem servidor, nuvem e conta. Se um passo falha, o resto para e o erro volta para o jogo.
