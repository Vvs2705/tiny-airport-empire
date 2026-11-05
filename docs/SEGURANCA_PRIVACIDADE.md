# Segurança e privacidade: auditoria e respostas da Play Console

**Retrato de 2026-11-05** (frente "segurança e privacidade" da fila C). Este documento junta o que o jogo coleta, o que ele protege, as respostas prontas do formulário **Segurança dos dados** (Data safety) da Play Console e o que cada frente futura tem de mudar junto. Ele não substitui a revisão jurídica (D-10), que continua com o Vinicius. Onde ele e o código discordarem, vale o código, e os portões abaixo existem para que isso não aconteça calado.

## 1. Resumo

- **O build de hoje não coleta dado nenhum.** Não há conta, login, servidor, análise de uso, SDK de anúncio ou de compra, nem código de rede no jogo (`checa_repo` confere). O progresso, as preferências e o diário ficam no aparelho. O diário só sai se o jogador tocar em Exportar diário e escolher o app.
- **O ID de publicidade não entra no APK:** o `Setup.SemIdDePublicidade` tira o `AD_ID` e as APIs do Privacy Sandbox do manifesto; o `checa_apk` derruba o APK que tiver `AD_ID`.
- **Nenhum segredo no git**, nem no histórico: varri os 181 mil linhas de `git log --all -p` com os padrões do `checa_repo` e os novos (Stripe, Slack, OAuth do Google, licença `.ulf` da Unity, senha de keystore). Nada achado. Nenhum `.jks`, `.keystore`, `.ulf`, `.env` ou `upload.properties` jamais entrou num commit.
- **Achado corrigido neste PR:** as flags de teste (`-onda`, `-bot`, `-cenario`, `-passageiros`...) valiam em qualquer build. No Android, qualquer app pode abrir a atividade do jogo com o extra `unity` e passar essas flags, o que viraria trapaça num build da loja. Agora `Game.Arg` só lê flags com `Debug.isDebugBuild` (build de desenvolvimento e Editor), como o GDD §8.3 manda, e o `checa_repo` derruba código que leia a linha de comando sem essa trava.
- **Falta, e não é desta frente:** subir o servidor de validação (pronto em `servidor/`), ligar os pacotes do IAP e do LevelPlay com a política nova (passo a passo e texto da política em `docs/LOJA_SERVIDOR.md`) e a revisão jurídica.

## 2. Auditoria (o que foi conferido)

| Item (GDD) | Situação | Evidência |
|---|---|---|
| Dados coletados (§9.1) | Nenhum sai do aparelho. Guardados no aparelho: save, `PlayerPrefs` (preferências, tutorial, `ta_instalacao`) e `diario.csv` | `Game.cs` (diário com GUID local aleatório, sem nome, e-mail, aparelho ou local); nenhum `InputField` no jogo, então não há texto livre do jogador |
| Telemetria (§15.1, D-11) | Só no diário local: eventos do catálogo `Telemetria.Eventos` com 3 inteiros; desempenho como `performance_sample` (FPS) e `device_class` (RAM em GB e núcleos, com teto), sem modelo, nome ou ID do aparelho | `Telemetria.cs`, `TelemetriaTests`; `checa_repo` (telemetria) derruba evento fora do catálogo e `deviceModel`, `deviceName`, `deviceUniqueIdentifier`, local e ID de publicidade no código |
| ID de publicidade (§7.2, §8.7) | Retirado do manifesto; o jogo não o lê | `Setup.cs` `SemIdDePublicidade`; `checa_apk` e `checa_repo` (guardrails) |
| Permissões Android | Só as da lista curta; nenhuma perigosa | `checa_apk` agora derruba permissão fora de `PERMISSOES` (INTERNET, ACCESS_NETWORK_STATE, VIBRATE e BILLING) |
| Serviços da Unity | Analytics, Crash/Cloud Diagnostics, Performance Reporting, Ads e IAP desligados | `ProjectSettings/UnityConnectSettings.asset` (`m_Enabled: 0` em todos) |
| Pacotes | Nenhum pacote que colete dados no `manifest.json` | `checa_repo` (privacidade) exige que a política cite cada pacote que coleta e cada API de rede |
| Exportar diário | Só por ação do jogador; API 29+ copia para Downloads/TinyAirport pelo MediaStore, sem permissão | `Exportar.cs` |
| Excluir meus dados | Apaga save, diário e todo o `PlayerPrefs` (inclusive o código da instalação), com confirmação | `Game.cs` (Excluir), PR #38 |
| Política (§9.3) | pt-BR e inglês no GitHub Pages, com e-mail de suporte, linkada nas Configurações | `docs/privacidade/`, `docs/privacy/`, `Textos.UrlPrivacidade` |
| Flags de teste no build público (§8.3) | **Corrigido aqui:** só em build de desenvolvimento | `Game.Arg`; `checa_repo` (guardrails) |
| App depurável em release (§8.3) | Conferido quando houver release: `checa_apk --release` derruba `android:debuggable` | `checa_apk.py` |
| Save (§8.6) | Envelope versionado com SHA-256, escrita atômica, migração, valores fora do limite recusados; teto offline de 2 h | `SaveEnvelope.cs`, `SaveTests`, `ExploitTests` |
| Compras (§7.4, §8.2) | Crédito idempotente e sem token no disco; servidor de validação e compra em 2 passos prontos em modo de teste | `ComprasTests` (PR #43); `servidor/` e `LojaTests` (`docs/LOJA_SERVIDOR.md`) |
| Segredos no repo público (§8.4) | Nenhum no histórico; travas no `.gitignore` e no `checa_repo` | seção 1; `.gitignore` ganhou `*.jks`, `*.keystore`, `*.ulf`, `upload.properties` |
| CI no repo público (§8.4) | `nucleo` e `balanco` sem segredo e com `permissions: contents: read` | `checa_repo` (ci) agora derruba `pull_request_target`, `workflow_run`, workflow sem `permissions:` e segredo impresso em `run:` |

## 3. Formulário "Segurança dos dados" da Play Console

O formulário descreve **o build que vai para a loja**, com todos os SDKs dele. Preencha a versão que bater com o AAB enviado; se o AAB mudar de coluna, o formulário muda antes do envio.

### 3.1. Build sem anúncio real, sem compra e sem servidor (o de hoje)

| Pergunta | Resposta |
|---|---|
| O app coleta ou compartilha algum dos tipos de dados exigidos? | **Não** |
| (as perguntas seguintes somem com o "Não") | — |

Condição para responder "Não": `checa_apk --release` sem `INTERNET` na lista de permissões, ou com `INTERNET` e nenhum SDK que envie dados. Os links da política (navegador) e do suporte (app de e-mail) são apps do próprio jogador e não contam como coleta do jogo. O diário exportado pelo jogador, para quem ele escolher, também não: o desenvolvedor não recebe nada.

### 3.2. Build de lançamento (anúncio com recompensa do LevelPlay, Unity IAP e servidor de validação)

Referência do LevelPlay: [Google Data Safety Questionnaire](https://docs.unity.com/en-us/grow/levelplay/platform/legal-resources/google-data-safety-questionnaire) (conferido em 2026-11-05; a frente de anúncio reconfere no dia, junto com o questionário de cada rede mediada).

| Categoria › tipo | Coletado | Compartilhado | Finalidade | Opcional? | Quem |
|---|---|---|---|---|---|
| Local › local aproximado | Sim | Sim | Publicidade, análise, prevenção de fraude | Obrigatório | LevelPlay (pelo IP) |
| Atividade no app › outras ações (interação com anúncio) | Sim | Sim | Publicidade, análise, prevenção de fraude | Obrigatório | LevelPlay |
| Informações e desempenho do app › diagnóstico | Sim | Não | Análise, prevenção de fraude | Obrigatório | LevelPlay |
| IDs de dispositivo ou outros | Sim | Sim | Publicidade, análise, prevenção de fraude | Obrigatório | LevelPlay (sem `AD_ID`: ID do app/instalação) |
| Informações financeiras › histórico de compras | Sim | Não | Funcionalidade do app (entregar o item), prevenção de fraude | Obrigatório para quem compra | nosso servidor de validação (token e produto, sem dado de cartão) |

Respostas gerais para essa coluna: dados **criptografados em trânsito: sim** (HTTPS); **o usuário pode pedir exclusão: sim** (e-mail de suporte, e o caminho de cada SDK); **todos os dados são obrigatórios**? Não declarar como "opcional" o que o SDK manda sempre. O pagamento em si (cartão) fica com a Google Play e não entra no nosso formulário.

### 3.3. Outros formulários do "Conteúdo do app"

| Formulário | Resposta |
|---|---|
| Política de privacidade | `https://vvs2705.github.io/tiny-airport-empire/privacidade/` (a mesma do jogo) |
| Anúncios | "Contém anúncios": **não** enquanto o anúncio for simulado; **sim** quando o LevelPlay entrar |
| Acesso ao app | Todo o app fica disponível sem login nem código |
| Público-alvo | 13 anos ou mais; fora do programa Famílias (política, D-10). Se a Google classificar como "atraente para crianças", rever com a revisão jurídica |
| Classificação (IARC) | Sem violência, sem jogo de azar, **sem compra de item aleatório**; marcar "compras no app" quando a loja entrar; sem chat nem interação entre usuários |
| ID de publicidade | "O app usa ID de publicidade?": **não** (a permissão é retirada) |
| Exclusão de conta | Não há conta; a exclusão dos dados do aparelho está no jogo |
| Apps governamentais, financeiros, de saúde, VPN | Não |

## 4. O que cada frente futura muda junto

Uma frente que acrescente qualquer item abaixo atualiza, **no mesmo PR**, as duas políticas (`docs/privacidade/`, `docs/privacy/`, com a data do topo), a seção 3 deste documento e a linha "Segurança e anti-fraude" do `CONTRATO_LANCAMENTO.md`. O `checa_repo` pega pacote novo e API de rede sem citação; o `checa_apk` pega permissão nova.

- **Anúncio com recompensa (LevelPlay):** não personalizado para todos, configurado **antes** do `Init` (sem consentimento de personalização, "do not sell" e o equivalente de cada rede mediada); nenhuma rede que exija `AD_ID`; recompensa com ID único e limite diário (§8.7); anúncio de teste no QA e configuração de produção só no release. Lançar só em BR, PH e ID (D-09) evita a CMP do EEE e do Reino Unido.
- **Compra (Unity IAP) e servidor (`ta-servidor`):** o servidor é a autoridade (§8.2): valida o token na API da Google Play com conta de serviço de menor privilégio, guarda o token por hash, recusa repetido, cancelado e reembolsado, responde sem stack trace, com limite de taxa e alerta de orçamento. A conta de serviço e qualquer chave ficam no secret manager, nunca no app nem no git.
- **Telemetria (FPS e p10 no diário):** feita no diário local (05/11), sem modelo de aparelho (só RAM em GB e núcleos, com teto), e só sai pelo Exportar diário; a seção 3.1 continua "Não". **Quando o SDK do Unity Analytics (D-11) entrar:** manda só os eventos do catálogo, sem o código da instalação do diário; vira "Informações e desempenho do app › diagnóstico" e "Atividade no app › outras ações" (coletado, não compartilhado, análise) na seção 3.2; as duas políticas citam o pacote antes (o `checa_repo` cobra); e o envio só começa depois do aviso.
- **Notificação, câmera, arquivos ou qualquer permissão nova:** só com linha nova em `PERMISSOES` do `checa_apk`, explicação contextual na hora de pedir (§9.2) e política atualizada.

## 5. CI e segredos no repositório público

- **Gatilhos.** Workflow que usa segredo roda só em `push` na `main`, `workflow_dispatch` ou `pull_request` (PR de fork não recebe segredo). `pull_request_target` e `workflow_run` são proibidos pelo `checa_repo`.
- **Artefatos são públicos.** Num repositório público, qualquer pessoa logada no GitHub baixa os artefatos das Actions. O rascunho do `unity.yml` (branch `ci/unity-nuvem`) sobe o AAB por 14 dias: tudo bem para AAB de teste com primitivas, mas **um AAB assinado com a chave de upload não deve virar artefato público**. Recomendação para a frente `ta-unity-ci`: AAB de release só no PC (`%USERPROFILE%\.tiny-airport\upload.jks`) ou, se for no CI, sem `upload-artifact` e com `environment` protegido.
- **Chave de upload no CI** (`ANDROID_KEYSTORE_BASE64`): é decisão do Vinicius. Se perdida, a Play App Signing permite trocar a chave de upload, mas o pedido leva dias. Mantê-la só no PC e no backup é o mais seguro.
- **Versões das actions** estão por tag (`@v4`, `@v7`). Fixar por SHA é o ideal para workflows com segredo; fica como melhoria da `ta-unity-ci`.
- **Log da licença da Unity:** o GitHub mascara os segredos nos logs; nunca use `echo`/`cat` com `secrets.*` (o `checa_repo` derruba).

## 6. Com o Vinicius (conta, senha e configuração)

1. **Verificação em duas etapas** no GitHub, na conta Google da Play Console, na Unity e no Tripo.
2. **Secret scanning e push protection** do GitHub, grátis em repositório público: [Settings › Code security](https://github.com/Vvs2705/tiny-airport-empire/settings/security_analysis), ligar "Secret Protection" e "Push protection".
3. **Play Console** (quando sair o D-U-N-S): ligar Play App Signing, preencher o "Conteúdo do app" com a seção 3 e conferir o e-mail de suporte público.
4. **Revisão jurídica** da política (D-10), com esta seção 3 em mãos.

## 7. Portões que guardam isto

| Portão | O que derruba |
|---|---|
| `checa_repo.py` (CI `nucleo`, `verify.sh quick`) | segredo em arquivo, arquivo de chave ou licença no git, `.gitignore` sem as travas, interstitial ou sorteio pago, `Setup` sem tirar o `AD_ID`, flag de linha de comando sem `Debug.isDebugBuild`, workflow com `pull_request_target`/`workflow_run`/sem `permissions:`/imprimindo segredo, política que não cita pacote que coleta ou API de rede (arquivo atrás de `#if TA_X` conta quando o pacote entra), `LevelPlay.Init` antes de `SetConsent(false)` e `do_not_sell` |
| `checa_apk.py` (`verify.sh android`; `--selftest` no CI `nucleo`) | targetSdk < 36, ABI, `AD_ID`, permissão fora da lista, 16 KB; com `--release`, app depurável |
| `node --test` em `servidor/` (CI `nucleo`, job `servidor`) | validação de compra: ambiente, pendente, cancelado, consumido, repetido, entrada mal formada, limite, kill switch, token no log, 503 sem detalhe (`docs/LOJA_SERVIDOR.md` §7) |
