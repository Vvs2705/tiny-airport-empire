# Ficha da loja na Google Play: Little Terminal

**Retrato de 2026-11-05** (frente "ícone e telas da Play", fila C). Este documento tem tudo o que a ficha da loja pede, pronto para colar na Play Console quando ela existir (previsão 2026-11-25): textos em pt-BR e em inglês, especificação do ícone e do gráfico de recursos, roteiro das capturas de tela e o que ainda falta. **Publicar, preencher o console e aceitar termos é com o Vinicius.** Os limites de tamanho abaixo são conferidos por `client/tools/ficha_play.py check` (e pelo `checa_repo`).

> **Regra da ficha:** a descrição só promete o que está no jogo na versão enviada. Cada frase marcada com `[condicional]` sai do texto se a frente dela não entrar no AAB do lançamento. Nada de "sem anúncios" (o anúncio recompensado e opcional existe), nada de "para crianças" (público 13+, `SEGURANCA_PRIVACIDADE.md` §3.3), nenhuma marca de terceiro e nenhuma promessa de ganho.

## 1. Dados da ficha

| Campo | Valor |
|---|---|
| Nome do app | Little Terminal (appId `br.com.vstack.littleterminal`, `NOME.md`) |
| Tipo e categoria | Jogo, categoria **Simulação** (tags sugeridas: simulação, estratégia, ocioso/tycoon, casual) |
| Idioma padrão | Português (Brasil); tradução em inglês (Estados Unidos) |
| Países do lançamento | Brasil, Filipinas e Indonésia (D-09) |
| Preço | Grátis, com anúncio recompensado opcional; compras no app só quando a frente da loja entrar (`CONTRATO_FASE_D1.md`) |
| E-mail de suporte | suporte@vstack-solutions.com.br |
| Política de privacidade | https://vvs2705.github.io/tiny-airport-empire/privacidade/ (inglês: `/privacy/`) |
| Site | não há; deixar em branco |
| Formulários do "Conteúdo do app" | respostas prontas em `SEGURANCA_PRIVACIDADE.md` §3 |

## 2. Textos (limites da Play: título 30, descrição curta 80, descrição completa 4.000 caracteres)

### 2.1 Português (Brasil)

**Título** (`titulo_pt`)

```
Little Terminal: Aeroporto
```

**Descrição curta** (`curta_pt`)

```
Gerencie um aeroporto: mova a equipe, cumpra horários e faça o terminal crescer.
```

**Descrição completa** (`longa_pt`)

```
Comece com uma pista de grama e um balcão. Termine com um hub internacional.

Little Terminal é um jogo de gestão de aeroporto, no estilo ocioso (idle tycoon), feito para jogar em retrato. O avião pousa, os passageiros passam pelo check-in, pela segurança, pela bagagem, pelas lojas e pelo portão, embarcam, e o avião decola com a receita. O quadro de partidas tem horário de verdade: na hora do rush, quem organiza a equipe melhor leva mais passageiros.

SUA EQUIPE É POUCA, E VOCÊ MANDA NELA
Você nunca tem gente sobrando. Mova atendentes, seguranças, bagageiros, vendedores e mecânicos entre as 8 estações conforme a fila aperta. Contrate entre 3 candidatos (Júnior, Pleno e Sênior) e acompanhe a conta do dia.

TOQUE EM QUALQUER PESSOA
Toque num passageiro para ver o voo, o destino, o tempo de espera e o que está pesando no humor dele: fila, limpeza, banheiro, pontualidade ou conforto. Toque num funcionário, num tripulante ou numa sala para ver o que está acontecendo ali.

UM AEROPORTO VIVO
Pilotos e comissários têm escala e descanso. O avião se desgasta e precisa de checagem no pátio ou de hangar. O dia tem hora, estações do ano e clima com previsão. Falhas e imprevistos sempre terminam em pouso seguro: aqui ninguém se machuca.

CRESÇA DE VERDADE
Passe por 3 aeroportos, do Vale Verde à Costa Azul e ao hub Aurora Global, com 12 rotas. Cumpra 30 objetivos, missões diárias e semanais, 20 conquistas e as histórias de 20 passageiros que pedem um favor.

JOGUE SEM PRESSÃO
- Sem conta e sem login.
- Anúncio só se você quiser: um vídeo opcional em troca de um bônus. Nenhum anúncio obrigatório, nenhum na tela de jogo.
- Nada aleatório se paga: sem caixa-surpresa, sem sorteio.
- Enquanto você está fora, o aeroporto rende por até 2 horas. Ninguém te cobra por voltar.
- [condicional] Compras opcionais com Gems, sempre com o preço mostrado pela Google Play. Nada que a campanha exija fica atrás de pagamento.

Dúvidas ou sugestões: suporte@vstack-solutions.com.br
```

### 2.2 English (United States)

**Title** (`titulo_en`)

```
Little Terminal: Airport
```

**Short description** (`curta_en`)

```
Run a cozy airport: move staff, beat the departure board, grow your terminal.
```

**Full description** (`longa_en`)

```
Start with a grass runway and one counter. End with an international hub.

Little Terminal is an idle airport management game made for portrait play. The plane lands, passengers go through check-in, security, baggage, shops and the gate, board, and the plane takes off with your income. The departure board runs on a real schedule: at rush hour, the player who organizes the staff best carries the most passengers.

YOUR STAFF IS SMALL, AND YOU CALL THE SHOTS
You never have people to spare. Move agents, security officers, baggage handlers, shop clerks and mechanics between 8 stations as the queues grow. Hire from 3 candidates (Junior, Mid and Senior) and keep an eye on the day's bill.

TAP ANYONE
Tap a passenger to see the flight, the destination, the waiting time and what is weighing on their mood: queues, cleanliness, restrooms, punctuality or comfort. Tap an employee, a crew member or a room to see what is going on there.

A LIVING AIRPORT
Pilots and flight attendants have schedules and rest. Planes wear down and need a check on the apron or a stay in the hangar. The day has a clock, seasons and a weather forecast. Faults and surprises always end in a safe landing: nobody gets hurt here.

REAL PROGRESS
Move through 3 airports, from Vale Verde to Costa Azul and the Aurora Global hub, with 12 routes. Complete 30 goals, daily and weekly missions, 20 achievements and the stories of 20 passengers who each ask for a favor.

NO PRESSURE
- No account and no login.
- Ads only if you want them: an optional video in exchange for a bonus. No forced ads, none on the game screen.
- Nothing random is ever sold: no loot boxes, no gacha.
- While you are away, the airport keeps earning for up to 2 hours. Nobody charges you for coming back.
- [conditional] Optional purchases with Gems, always priced by Google Play. Nothing the campaign needs sits behind a payment.

Questions or ideas: suporte@vstack-solutions.com.br
```

> **Conferir antes de colar** (`GDD.md` e `CONTRATO_LANCAMENTO.md` são a fonte): os nomes dos aeroportos ficam em português nos dois textos (são nomes próprios do jogo); se o PR do inglês traduzir esses nomes, o texto em inglês acompanha. Se um número mudar (aeroportos, rotas, objetivos, conquistas, histórias), o texto muda no mesmo PR. A revisão humana do inglês do jogo é pendência do Vinicius e vale também para este texto.

## 3. Ícone do app (512 × 512)

| Item | Especificação da Play | Nosso arquivo |
|---|---|---|
| Tamanho | PNG de 32 bits, **512 × 512 px**, até 1.024 KB | `docs/play/icone_512.png`, gerado de `Art/Marca/icone_app.png` (1024 px, sem texto) por `ficha_play.py icone` |
| Forma | quadrado cheio; a Play aplica o canto arredondado e a sombra sozinha, então **sem canto arredondado nem sombra no arquivo** | já é quadrado cheio |
| Conteúdo | o avião branco de faixa verde-água decolando sobre o terminal de telhado laranja, céu azul; **sem texto** (`ART_BIBLE.md` §5) | igual ao ícone do launcher |
| Zona segura | o motivo principal dentro do círculo central de 66% (o launcher recorta em círculo) | prévia em `docs/play/previa_icone.png` (círculo branco = 66%): o nariz e a cauda do avião passam da zona segura. Na Play (quadrado de canto arredondado) não corta nada; no ícone **adaptativo** do launcher, a camada de frente precisa de um recorte próprio com o avião menor |
| Ícone adaptativo do launcher | a camada de frente com o motivo até 0,64 da área (prompt mestre §8) | **falta aplicar ao build** (`Setup.cs` ainda não usa o ícone; `CONTRATO_LANCAMENTO.md` linha 23) |

A versão do ícone **com as malhas do Tripo** (um avião renderizado em 3D) só sai na rodada local no PC: a arte do Tripo não vai ao repositório público (`ESTADO_DO_PROJETO.md` §3). O ícone atual é arte de marca já publicada no repositório, e basta para a Play.

## 4. Gráfico de recursos (feature graphic, 1024 × 500)

| Item | Especificação |
|---|---|
| Tamanho | JPEG ou PNG de 24 bits (sem alfa), **1024 × 500 px**, até 15 MB |
| Arquivo | `docs/play/grafico_1024x500.png`, montado por `ficha_play.py grafico` com a vista aérea do splash, desfocada de leve, e a logo no centro |
| Composição | logo "LITTLE TERMINAL" no centro, longe das bordas (a Play corta as beiradas em alguns aparelhos e põe o botão de vídeo no meio quando há vídeo de promoção; sem vídeo, o centro fica livre); ao fundo, o terminal, a pista e o hangar do splash |
| Texto | só a logo (o nome do jogo). Nenhuma frase de promoção ("grátis", "melhor", "nº 1"), nenhum selo de loja, nenhum preço |
| Para a rodada local | uma versão com o terminal em 3D renderizado (vista `geral` do `fotos_play.sh`) no lugar da arte do splash; a pasta de saída fica fora do git |

## 5. Capturas de tela

**Regras da Play:** de 2 a 8 capturas de celular (mínimo 2 para a ficha aparecer); PNG ou JPEG de 24 bits, sem alfa; cada lado entre 320 e 3.840 px e o lado maior no máximo o dobro do menor; proporção 9:16 em retrato. Fazemos **1080 × 1920**. Nada de moldura de aparelho, nada de texto que prometa o que o jogo não tem.

**Como se gera** (no PC, depois do `verify.sh full`, com as malhas do Tripo no lugar):

```bash
client/tools/fotos_play.sh client/Builds/play        # 8 capturas cruas em 1080x1920 (o -testsession, o -shotescala 2)
python client/tools/ficha_play.py capturas client/Builds/play client/Builds/play/final   # cada captura vai inteira, reduzida, sob uma faixa com a legenda pt-BR; confere o tamanho
```

As capturas **mostram o Tripo**, então ficam em `client/Builds/` (fora do git) e vão direto para a Play Console. Quem confere: o dono, a olho, uma a uma.

### Roteiro (ordem em que aparecem na ficha)

| # | Tela | Como sai (`fotos_play.sh`) | Legenda pt-BR | Legenda en |
|---|---|---|---|---|
| 1 | Terminal em ação, vista geral, com fila e quadro de partidas | `-onda 3 -bot -cenario salas -vista geral` | Gerencie um aeroporto inteiro | Run a whole airport |
| 2 | A equipe: contratar, escolher entre 3 candidatos | `-tela equipe` | Sua equipe é pouca: mova com cuidado | Your staff is small: move them wisely |
| 3 | Ficha de um passageiro com o humor e o motivo | `-tela fichapax` | Toque e veja o que pesa no humor | Tap to see what drives their mood |
| 4 | Pátio com avião, escada e veículos | `-vista patio` | Do pouso à decolagem | From landing to takeoff |
| 5 | Mapa de aeroportos e rotas | `-tela rotas` | 3 aeroportos e 12 rotas | 3 airports and 12 routes |
| 6 | Metas da campanha | `-tela metas` | Objetivos, missões e conquistas | Goals, missions and achievements |
| 7 | Melhorias de uma sala | `-tela melhorias` | Melhore cada sala do terminal | Upgrade every room |
| 8 | Passe de temporada, trilha gratuita | `-tela passe` | Prêmios por jogar, sem pressa | Rewards for playing, no rush |

**Regras de conteúdo das capturas:** sem número de versão de desenvolvimento nem faixa de depuração na tela; sem preço (a Play mostra o preço); sem anúncio de terceiro; a tela 8 só entra se a trilha gratuita estiver no AAB; se a loja de Gems não entrar, nenhuma captura a mostra. Trocar a ordem ou tirar uma captura é permitido, mas **ficam as tela 1 a 3** nas três primeiras posições (a Play mostra só elas antes de o jogador rolar).

## 6. O que falta (e de quem é)

| Item | Quem | Quando |
|---|---|---|
| Rodar `fotos_play.sh` com as malhas do Tripo e conferir as 8 capturas | PC do dono, uma vez por leva (regra do Unity) | depois da leva de telas e do Lote 12 |
| Aplicar o ícone ao build (`Setup.cs`, ícone adaptativo) | frente de Unity | antes do AAB |
| Revisão humana do inglês (jogo e ficha) | Vinicius | antes de enviar |
| Criar a ficha no console, subir as imagens e preencher "Conteúdo do app" | Vinicius | quando a Play Console sair (previsão 2026-11-25) |
| Classificação indicativa (IARC) e declaração do público-alvo | Vinicius, com `SEGURANCA_PRIVACIDADE.md` §3.3 | no console |
| Vídeo de promoção (opcional, YouTube) | fora do escopo do lançamento | pós-lançamento |
