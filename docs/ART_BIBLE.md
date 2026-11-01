# Little Terminal — Art Bible (v0.1, 2026-10-29)

**Estado:** em uso desde o dia do Tripo (2026-10-29). Decidida pelo GDD §10 e pelo prompt mestre §2. A lista de peças está em `ASSETS.md`; a origem e o SHA de cada arquivo, em `PROVENIENCIA.md`. O que diz "a medir" se fecha no 1º render de teste, com foto 540×960.

## 1. Estilo

- **Low-poly acolhedor** (GDD §10.1): volumes arredondados e gordos, personagens com ~3 cabeças de altura, mãos e cabeça grandes, cor chapada com 2 tons e contorno de tinta assado no sprite. Nada de PBR espelhado, textura ruidosa nem micro-detalhe que some numa tela de 6,5".
- **Cada papel tem uma cor e uma forma.** O funcionário se lê pela silhueta e pelo colete; o passageiro, pela roupa civil e pela mala. A cor de um papel não se repete em cenário.
- **O avião é a estrela da pista.** Fuselagem branca e lisa (lê bem sobre o asfalto cinza), com faixa de cor da companhia aplicada por troca de cor. Sem logotipo nem forma de fabricante real (D-13).

## 2. Câmera e projeção

- **Rota híbrida (GDD §10.2, D-18):** as **pessoas** viram sprites 2D pré-renderizados (Tripo → Auto Rig Mixamo → Blender → `render_sprites.py`), sem skinning no aparelho e com pooling; **prédios, aviões e veículos** entram no Unity como **malha em tempo real**, decimada no Blender a partir da HD, com material simples e pivô no chão.
- **Uniforme trocável (pedido do Vinicius, 29/10):** o funcionário veste o uniforme do setor onde chega. No Blender, o uniforme (colete, boné ou camisa) vai para um material próprio, e o render sai com uma **máscara do uniforme** separada do corpo; a View tinge a máscara em tempo de execução. Clipes por personagem: andar, parado e atendendo no balcão; passageiros: andar e parado na fila.
- **Ortográfica, azimute 45°, elevação 45° (decidida em 29/10 com a troca do greybox: lê a fila inteira dentro da sala em corte e a lateral do avião).** Antes: elevação a medir com um render de teste de 0 crédito (terminal + fila de 6 passageiros + avião): candidatos **30°** (dimetria 2:1, mostra fachadas e a lateral do avião), **45°** e **60°** (o padrão do DC, mostra o topo e as filas sem oclusão). Vence a que deixa ler a fila inteira e o portão sem esconder ninguém; padrão provisório 45°.
- **Direções:** passageiros e funcionários com 4 direções diagonais + espelho (andam em corredores isométricos). Aviões, veículos, estações e terminais não têm folha: giram como malha no Unity.
- **Luz gravada:** Workbench chapado + cavidade + contorno de tinta de 3 px (`--outline 3`), uma direção de luz para tudo (do alto à esquerda). Sombra de ator como elipse no chão, não sombra dinâmica.
- **Escala:** 1 unidade = 1 m na View; textura a 180 px/m (`--scale 180`, preset TA do `render_sprites.py`); altura normalizada no Blender (`--height`), nunca no Unity. Adulto = 1,0 m no jogo (3 cabeças: a fila de 60 cabe na sala de 4 × 4), criança 0,7 m; os corpos rígidos seguem a mesma régua (balcão 0,75 m, avião regional 7,6 m).

## 3. Leitura em 540×960

- O jogo é em retrato. Na foto de 540×960, o passageiro tem de ocupar **≥ 28 px de altura** e a estação **≥ 90 px de largura**; abaixo disso a silhueta some.
- A fila se lê pela cor da barra de paciência e pelo ícone acima do passageiro, nunca só pela cor (GDD §11.2).
- Sem faixa lisa em tela 20:9 (o pátio/grama preenche), sem texto sobreposto, sem dedo tampando o funcionário arrastado (o sprite sobe acima do dedo, GDD §5.5), sem botão menor que 48 dp.

## 4. Paleta (hex)

| Uso | Base | Sombra | Luz |
|---|---|---|---|
| Vale Verde (regional): grama | `#7FB069` | `#5E8A4C` | `#A3CF85` |
| Vale Verde: telhado/destaque quente | `#E39B4F` | `#B8743A` | `#F2BE7E` |
| Costa Azul (turístico): mar/vidro | `#3FA7D6` | `#2C7EA6` | `#7CCBEB` |
| Costa Azul: coral | `#F07167` | `#C4534B` | `#F7A399` |
| Aurora Global (hub): azul profundo | `#1F3A68` | `#142847` | `#35598F` |
| Aurora Global: dourado | `#E2B04A` | `#B48632` | `#F0CF84` |
| Asfalto da pista | `#5A5F6B` | `#43474F` | faixa `#F4F1E8` |
| Piso do terminal | `#E8E2D4` | `#CFC7B5` | `#F7F3EA` |
| Avião (fuselagem) | `#F4F6F8` | `#C9D0D8` | janelas `#2B3A55` |
| Tinta (contorno, texto escuro) | `#1E2230` | — | — |

**Cores de papel (exclusivas):** atendente azul-marinho `#24407A` com lenço amarelo; segurança cinza-chumbo `#3D4452`; bagageiro e despachante laranja de segurança `#F28C28` (o despachante com faixa amarela); vendedor verde-menta `#5FBF9F`; mecânico macacão azul-petróleo `#2E6F80`. **Estado de fila:** normal `#4CAF6E`, atenção `#F2C14E`, crítico `#E0533D`, sempre com ícone. **Quadro de partidas:** amarelo `#FFD23F` sobre preto `#14161C` (GDD §10.3).

## 5. Interface

- **Fontes OFL**, com a licença junto do arquivo: títulos em **Lilita One**, texto em **Nunito ExtraBold** (as do DC, cobrem Latin-1: "Florianópolis" passa) e horários do quadro em **JetBrains Mono** ou **Space Mono** (mono OFL).
- Botões, painéis e molduras em **9-fatias gerados por código** (como no DC), cantos arredondados, sombra curta.
- **Linguagem das telas (`Art.Kit.cs`):** folha ancorada ao pé com cabeçalho azul `#1F3A68`, fio e placa amarelos `#FFD23F` (pictograma de aeroporto, azul sobre amarelo) e corpo claro; cada item é um cartão de embarque branco, com o canhoto picotado para o botão. Verde compra, azul vídeo, cinza sem dinheiro ou travado, claro para ação grátis. Estado nunca é só cor: sem dinheiro diz quanto falta, travado tem cadeado e motivo. Texto: Título 72, Seção 46, Nome e Valor 44, Corpo 36, Legenda 32 (px da referência de 1080; o piso de 32 dá ~11 sp num aparelho de 360 dp). Alvo de toque de 144 px ou mais (48 dp a 360 dp). A seta `→` não existe nas 2 fontes: use `»`.
- **Ícones e retratos pintados** no gerador de imagem grátis (Nano Banana 2), sobre azul-marinho liso e recortados por script (`ui_assets.py` do DC): ícones das 8 estações, avião, relógio, mala e história; retratos de Lara e Marco; 10–20 rostos dos passageiros com história.
- **Marca (feita em 2026-10-29, a pedido do Vinicius):** logo "LITTLE TERMINAL" em letras gordas amarelo-creme com contorno azul-marinho e um aviãozinho com rastro pontilhado; emblema sem texto (avião + terminal + asas douradas); ícone do app sem texto (avião branco de faixa verde-água decolando sobre o terminal de telhado laranja, céu azul); splash 9:16 ao pôr do sol. O nome é provisório (D-03): a logo se refaz se ele mudar; ícone, emblema e splash não têm texto e ficam.
- **Qualidade antes de peso** (Vinicius, 2026-10-29): modelos canônicos em 4K + 50 000 faces e peças sem presença refeitas; o APK pode crescer, mas cada folha continua com teto de 4096 px.

## 6. Pipeline e regras

- Item sempre separado do corpo e preso ao osso no Blender: mala na mão (`mixamorig:RightHand`), mochila nas costas (`mixamorig:Spine2`), rádio e scanner na mão, bastões nas duas mãos.
- Variantes por troca de cor, sem crédito: 8 pinturas de avião, uniformes, roupas de passageiro, Marco e temas de terminal.
- Animação: clipes Mixamo do disco + clipes por script no Blender (`anim_blender.py`), retarget por delta de rotação sobre o repouso. Aviões sem esqueleto.
- Arte crua (zips do Tripo, FBX, `.blend`) fora do git, em `arte/` do checkout principal e no backup `3D Objects\TinyAirport-BACKUP\`. Sprites finais, ícones e retratos entram no git.
