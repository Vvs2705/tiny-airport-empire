# 09. Próximas ações

Em ordem de impacto. As frentes de jogo ficam acima das de ferramenta: nenhuma ferramenta nova destrava mais do que mesclar o que já existe.

## Frentes do jogo

| # | Ação | Depende de | Pronto quando |
|---|---|---|---|
| 1 | **Mesclar `feat/view-tripo`** (`docs/CONTINUAR_ARTE.md`): é o maior gargalo; 230 peças geradas e 0 na `main` | retomar a frente; RAM livre para Blender e Unity | malhas e sprites na `main`, `verify.sh full` verde, fotos conferidas |
| 2 | **Fechar a Fase C2** (8 estações e 6 perfis), hoje em branch | item 1 não bloqueia; é independente | PR mesclado e `balanco.yml` verde no PR |
| 3 | **Frente de áudio**: copiar `audio_gen.py` e `Audio.cs` do DC, adicionar `com.unity.modules.audio`, testar cedo o motor de avião (GDD §12) | nenhuma | efeitos e música no jogo, registro no `PROVENIENCIA.md` |
| 4 | **Fonte final** OFL, com a licença junto | nenhuma | sem `LegacyRuntime.ttf` |
| 5 | **Medir K3 no aparelho**: copiar `medir_aparelho.sh` do Forge, com `-perf` | itens 1 e 2; "pode testar" do Vinicius | p10 ≥ 30 FPS medido, ou plano de corte |

## Depende do Vinicius

Passo a passo em `05-installation-plan.md`.

| # | Ação | Por que importa | Tempo estimado |
|---|---|---|---|
| A | Fixar a versão do MCP do Blender, atualizar o addon e desligar a telemetria | addon no protocolo 9 de 15; registro mínimo ainda sai | 5 min |
| B | Ruleset na `main` e Dependabot alerts | a `main` não tem proteção | 5 min |
| D | **Ler os termos do Tripo logado, antes de 2026-11-29** | direitos depois do vencimento não confirmados; o prazo é real | 15 min |
| E | Cópia da arte fora do PC | 5 GB de arte paga num só disco | 30 min |
| C | Atualizar o Unity quando o patch sair | condição para o AAB da loja | quando a Unity publicar |
| F | Decidir como medir em Android de entrada | o POCO F4 não representa a meta do K3 | decisão |
| G | Desligar conectores sem uso neste repositório | contexto de cada sessão | 5 min |

## Antes do envio à loja

Conferir no dia:

1. Unity com o patch do Brotli e da eMudhra; `verify.sh full` e `android`.
2. `checa_apk.py` estendido para AAB, junto com o `Setup.BuildAndroidBundle`.
3. Rever a Play: API 36 vale desde 2026-08-31; a partir de 2027-02-01, atualização sem 16 KB não pode ser publicada; prazo de API 37 ainda não publicado.
4. SDKs novos (LevelPlay, IAP): procurar Brotli e `HttpClient` neles.
5. `placeholder_gate.py` do Forge adaptado.
6. Permissão `INTERNET` no APK: hoje o jogo não usa rede; revisar quando os SDKs de anúncio entrarem.

## Quando reavaliar

| Ferramenta | Gatilho |
|---|---|
| GameCI | `verify.sh full` deixar de caber no PC |
| Unity MCP | cena deixar de ser gerada por código, e o patch do Brotli sair |
| Stryker.NET | teste fraco escapar, apesar da cobertura |
| Sentry ou Crashlytics | decisão D-11 |
| Git LFS | algum arquivo passar de 10 MB (o `checa_repo.py` avisa) |
| ElevenLabs | motor de avião sintetizado soar pobre |
