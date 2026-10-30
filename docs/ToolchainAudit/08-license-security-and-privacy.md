# 08. Licenças, segurança e privacidade

Levantado em 2026-10-30. Este arquivo não traz valor de nenhum segredo.

## Segredos

| Verificação | Resultado |
|---|---|
| Busca no histórico inteiro (`git log -p --all`) por tokens do GitHub, chaves da Anthropic, Google, AWS e chave privada | **0 ocorrências** |
| Nomes sensíveis no histórico (`.keystore`, `.jks`, `.p12`, `.pem`, `.env`, `google-services`) | **0** |
| Alertas do GitHub Secret Scanning | **0** |
| Secret scanning e push protection no repositório | **ligados** |
| Padrões genéricos e validity checks | desligados (ajuste opcional) |
| Dependabot | **desligado** (`05-installation-plan.md`, item B) |
| Proteção da `main` | **nenhuma** (mesmo item) |
| `checa_repo.py` nos arquivos atuais | 0 erros |

## Falhas de segurança no motor

Lidas nas páginas oficiais em 2026-10-30.

| Aviso | Gravidade | 6000.3 | Efeito neste projeto |
|---|---|---|---|
| [Brotli Decoder, CVE-2026-111497](https://unity.com/security/unity-brotli-decoder-vulnerability) | CVSS 9,2 | 0a1 a 27f1 afetadas; patch **pendente** | O jogo só seria exposto passando dado externo a `BrotliStream`, `BrotliDecoder` ou `HttpClient` com Brotli. **Busca no código: 0 usos.** O risco volta com SDKs: conferir LevelPlay e IAP antes de entrarem. O decodificador vai dentro do app, então o build final precisa sair de um Editor corrigido. No Editor, evitar scoped registries de terceiros até o patch |
| [eMudhra CA comprometida](https://unity.com/security/unity-emudhra-compromise) | certificados de CA intermediária | 0a1 a 27f1 afetadas; patch **pendente** | Android protegido pelo Google Play Services de outubro. Editor e OpenJDK do build ficam expostos a interceptação em rede; atualizar quando sair |
| [CVE-2025-59489](https://unity.com/security/sept-2025-01) | CVSS 8,4 | corrigida desde 0b4 | sem ação |

**Nada foi publicado**, então não há jogador exposto. A condição é: **o AAB da loja sai de um Editor com o patch.**

A falha do Brotli também foi corrigida em Node.js e .NET ([Field Effect](https://fieldeffect.com/blog/brotli-vulnerability-affects-multiple-software-ecosystems), [CSO Online](https://www.csoonline.com/article/4241027/node2shell-puts-microsoft-and-node-js-on-brotli-patch-duty.html)). Node 24.15.0 e .NET SDK 10.0.400 aqui são só ferramentas locais; não verifiquei se já estão nas versões corrigidas. Atualizar é simples e não afeta o jogo.

## Dados enviados para fora

| Fluxo | O que sai | Estado |
|---|---|---|
| MCP do Blender | **Correção:** a telemetria **não é só opt-in**. Com `telemetry_consent=false`, que é o estado verificado aqui, o pacote ainda envia a um Supabase do autor um registro mínimo: ID aleatório da instalação, sessão, nome da ferramenta, sucesso, duração, versões e sistema. Prompts, código, screenshots e cena só saem com consentimento | **Confirmado no código** (`telemetry.py`, 2.2.6): nada da arte sai, e o registro mínimo continua. Inclusive 1 evento do meu teste de conexão. `DISABLE_TELEMETRY=true` corta tudo (item A do plano) |
| Tripo | Prompts e imagens, na conta paga e com "Privado" marcado | previsto; ver licenças |
| Pesquisa desta auditoria | Só buscas e leitura de páginas públicas | nenhum dado do projeto enviado |
| Jogo | Nenhum dado sai: sem SDK de rede; o diário CSV só sai quando o jogador exporta | verificado no código: 0 usos de rede em `Scripts` |

## Guardrails do estúdio, agora com trava

| Regra | Como é cobrada |
|---|---|
| Sem interstitial, sem item aleatório pago | `checa_repo.py` (`interstitial`, `gacha`, `loot box` em `Scripts`) e CI |
| Sem `AD_ID` | `checa_repo.py` confere o `Setup.cs`; `checa_apk.py` confere o APK |
| Arte crua e chaves fora do git | `checa_repo.py` e `.gitignore` |
| Offline com teto de 2 h, perder voo custa só a receita | `ExploitTests` |

**Limite:** a busca por palavras pega o óbvio e não substitui revisão. Uma mecânica de sorteio paga com outro nome passaria por ela.

## Licenças do que entra no jogo

| Item | Licença | Estado |
|---|---|---|
| Código e documentos | Todos os direitos reservados, repositório público para acompanhamento | `README.md` |
| Unity Personal | Termos da Unity | limite de receita a conferir ao lançar |
| Modelos do Tripo | Plano pago: uso comercial; "Privado" marcado | **PROVÁVEL**: a página oficial deu 403. Direitos depois do vencimento: **NÃO VERIFICADO** |
| Marca (imagens do gerador do Tripo) | Mesmos termos, segundo o `PROVENIENCIA.md` | a confirmar junto |
| Malhas decimadas | Fora do git, por decisão registrada no GDD §10.2 | coerente com o repositório público |
| Mixamo | Sem royalties em jogos; proibido distribuir arquivos brutos; sem treino de IA | **PROVÁVEL**; nenhum clipe baixado neste projeto até agora |
| Fontes | `LegacyRuntime.ttf`, do Unity | trocar por OFL antes da loja |
| Áudio | Nenhum | a rota por síntese não cria dependência de licença |
| Nome "Little Terminal" | Sem busca no INPI, USPTO, EUIPO e nas lojas | D-03 pendente |

## Privacidade

- Sem coleta de dados pessoais hoje; `/playtest/` está no `.gitignore`.
- LGPD, política de privacidade, D-09, D-10 e D-12 seguem pendentes (Fase D).
- O ECA Digital (Lei 15.211/2025) pede atenção a menores. A classificação definitiva depende da D-10.
- Não há backend, então Play Integrity e App Attest não se aplicam agora (GDD §8, D-06).

## Riscos em aberto

1. Tripo: direitos depois de 2026-11-29 não confirmados.
2. Arte paga com backup no mesmo disco.
3. MCP do Blender sem versão fixa: cada abertura pode executar código novo do PyPI.
4. `main` sem proteção.
