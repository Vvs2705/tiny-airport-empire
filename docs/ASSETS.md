# Little Terminal — Lista fechada de assets 3D (dia do Tripo, 2026-10-29)

**Estado:** lista fechada antes de gerar (prompt mestre §3.1). Aval: D-14 aprovada em 2026-10-29 (GDD §23.2), que manda gerar os Lotes 1 e 2 hoje, por esta lista, porque os créditos do Tripo acabam hoje. A geração peça a peça, com saldo e SHA, fica em `PROVENIENCIA.md`. Estilo, câmera e paleta estão em `ART_BIBLE.md`.

**Fontes:** GDD §4.1 (8 estações, 6 perfis, 8 variantes de avião, 3 aeroportos), §5.5, §5.7, §10.2 e §10.3; GDD_ORIGEM, "Arte e animação" (Lote 1 ≈ 400); modelos do Dungeon Courier (`historico/ASSETS.md` §0).

## 0. Regras desta lista

- **Custo** (medido no DC e no Forge): modelo HD = **45**, Auto Rig = **20**. Personagem = 65, objeto = 45. Imagem de referência no Nano Banana 2 = **0**. Export e download = 0.
- **Prioridade:** 1 = sem ele não há jogo (v0.1); 2 = lançamento (GDD §4.1); 3 = acabamento e variedade.
- **Item separado do corpo, sempre:** mala, mochila, rádio, scanner, bastões e caixa de ferramentas saem como objeto próprio e são presos ao osso no Blender (`render_sprites.py --attach`). O personagem é gerado de mãos vazias.
- **Avião genérico:** sem logotipo, sem texto, sem pintura de companhia e sem forma de fabricante real (D-13 pendente). Fuselagem branca lisa, que recebe as pinturas por troca de cor.
- **Logo e ícone do app:** o brief do dia dizia "só depois do D-03", mas o Vinicius pediu os dois no mesmo dia (§5). Saem com o nome provisório "Little Terminal" e se refazem se o nome mudar. Retratos e ícones de interface saem depois, pintados no gerador.
- **Variantes sem crédito** (troca de cor no Blender ou na folha renderizada, `roupas_cor.py`/`roupas_2d.py` do DC): as 8 pinturas de avião, as cores de uniforme, as roupas dos passageiros, o Marco (Chefe de Bagagem = bagageiro com outra cor) e os temas de terminal.

## 1. Lista

| # | Peça (pasta em `arte/tripo/`) | Família | Prior. | Como | Créditos |
|---|---|---|---|---|---|
| 1 | `passageiro_a` (adulto, roupa casual) | personagem | 1 | HD + rig | 65 |
| 2 | `atendente` (uniforme de balcão, blazer) | funcionário | 1 | HD + rig | 65 |
| 3 | `aviao_regional` (turboélice de asa alta) | avião | 1 | HD | 45 |
| 4 | `balcao_checkin` | estação | 1 | HD | 45 |
| 5 | `raio_x` (pórtico detector + esteira do raio-x) | estação | 1 | HD | 45 |
| 6 | `portao_embarque` (balcão do portão + painel) | estação | 1 | HD | 45 |
| 7 | `loja` (quiosque de lembranças) | estação | 1 | HD | 45 |
| 8 | `terminal_vale_verde` (terminal regional pequeno) | terminal | 1 | HD | 45 |
| 9 | `mala_rodinha` | item | 1 | HD | 45 |
| 10 | `passageira_b` (adulta, roupa casual) | personagem | 2 | HD + rig | 65 |
| 11 | `seguranca` (agente de segurança) | funcionário | 2 | HD + rig | 65 |
| 12 | `bagageiro` (colete refletivo, protetor auricular) | funcionário | 2 | HD + rig | 65 |
| 13 | `vendedor` (avental de loja/café) | funcionário | 2 | HD + rig | 65 |
| 14 | `mecanico` (macacão) | funcionário | 2 | HD + rig | 65 |
| 15 | `despachante` (colete, fone de rádio) | funcionário | 2 | HD + rig | 65 |
| 16 | `aviao_narrow` (jato bimotor de corredor único) | avião | 2 | HD | 45 |
| 17 | `aviao_wide` (jato bimotor de dois corredores) | avião | 2 | HD | 45 |
| 18 | `esteira_bagagem` (carrossel de bagagem) | estação | 2 | HD | 45 |
| 19 | `alimentacao` (balcão de café/lanche) | estação | 2 | HD | 45 |
| 20 | `hangar_manutencao` | estação | 2 | HD | 45 |
| 21 | `torre_controle` (despacho) | estação | 2 | HD | 45 |
| 22 | `terminal_costa_azul` (terminal turístico) | terminal | 2 | HD | 45 |
| 23 | `terminal_aurora` (hub internacional) | terminal | 2 | HD | 45 |
| 24 | `rebocador` (trator de pushback) | veículo | 2 | HD | 45 |
| 25 | `carrinho_bagagem` (trator com carretas) | veículo | 2 | HD | 45 |
| 26 | `caminhao_combustivel` | veículo | 2 | HD | 45 |
| 27 | `mochila` | item | 2 | HD | 45 |
| 28 | `radio` (rádio de mão) | item | 2 | HD | 45 |
| 29 | `scanner_mao` (detector de metal de mão) | item | 2 | HD | 45 |
| 30 | `passageiro_idoso` | personagem | 3 | HD + rig | 65 |
| 31 | `crianca` (passageira criança) | personagem | 3 | HD + rig | 65 |
| 32 | `lara` (Chefe de Terminal, tutorial) | personagem | 3 | HD + rig | 65 |
| 33 | `piloto` | personagem | 3 | HD + rig | 65 |
| 34 | `aviao_turistico` (monomotor pequeno, Costa Azul) | avião | 3 | HD | 45 |
| 35 | `aviao_cargo` (cargueiro de nariz alto) | avião | 3 | HD | 45 |
| 36 | `escada_embarque` (caminhão-escada) | veículo | 3 | HD | 45 |
| 37 | `onibus_patio` | veículo | 3 | HD | 45 |
| 38 | `ponte_embarque` (finger) | terminal | 3 | HD | 45 |
| 39 | `cadeiras_espera` (fileira de assentos) | decoração | 3 | HD | 45 |
| 40 | `vaso_planta` | decoração | 3 | HD | 45 |
| 41 | `bastoes_sinalizacao` (par de bastões laranja) | item | 3 | HD | 45 |
| 42 | `caixa_ferramentas` | item | 3 | HD | 45 |

## 2. Totais

| Prioridade | Peças | Personagens (65) | Objetos (45) | Créditos |
|---|---|---|---|---|
| 1 | 9 | 2 | 7 | 445 |
| 2 | 20 | 6 | 14 | 1 020 |
| 3 | 13 | 4 | 9 | 665 |
| **Total** | **42** | **12** | **30** | **2 130** |

**Saldo lido no site antes de começar (2026-10-29):** 14 595 créditos. Cobre a lista inteira com folga; nenhuma peça foi cortada por saldo.

### 2.1 Rodada HD (pedido do Vinicius no mesmo dia)

No meio do lote, o Vinicius pediu: "não faça artes fracas sem presença e valor no jogo, mesmo que isso deixe mais pesado; crie a logo também, a arte para quando o app for instalado". Por isso:

- **As 42 peças foram geradas de novo em HD:** textura 4K e 50 000 faces (botão "Generate 55"), a partir da mesma imagem de referência. Os personagens HD recebem rig de novo (+20). A versão HD é a **canônica** (pasta `arte/tripo/<peça>_hd/`); a 1ª versão (2K, 8 000 faces) fica como reserva.
- **7 peças sem presença ganharam imagem nova, mais rica, antes do HD:** `raio_x`, `balcao_checkin`, `terminal_aurora`, `bastoes_sinalizacao` (saiu como 2 cápsulas), `cadeiras_espera` (saiu 1 assento só), `esteira_bagagem` (saiu um disco cinza liso) e `portao_embarque` (cinza, sem cor).
- Custo da rodada: 42 × 55 + 12 × 20 = **2 550**. Total do dia previsto: 2 130 + 2 550 + imagens e marca (§5) ≈ **4 900**, dentro do saldo.

### 2.2 Cobertura do elenco e Lote 3 (D-20)

Papéis pedidos pelo Vinicius em 29/10 ("aeromoças, pilotos, manutenção, contratações"): piloto, mecânico, equipe de pátio (bagageiro e despachante, colete refletivo), funcionários do terminal (atendente de check-in e portão, segurança, vendedor) e 4 passageiros já estão gerados (§1). **Falta a tripulação de cabine:**

| # | Peça | Família | Prior. | Como | Créditos |
|---|---|---|---|---|---|
| 43 | `comissaria` (uniforme verde-petróleo, lenço coral) | personagem | 2 | 1 rodada de imagem + HD + rig | 8 + 55 + 20 = 83 |
| 44 | `comissario` (uniforme verde-petróleo, gravata coral) | personagem | 2 | 1 rodada de imagem + HD + rig | 8 + 55 + 20 = 83 |

Total: **166** créditos. Aval: "Aprovo o Lote 3" (Vinicius, 29/10, D-20) e, depois, o aval geral do dia: "pode gerar todos e qualquer arte enquanto houver saldo" (Vinicius, 29/10). Gerado em HD (4K, 50 000 faces) com rig; a 1ª tentativa tinha sido barrada pelo classificador de permissões e foi refeita depois do aval geral. Mala de bordo, se precisar, sai como item à parte.

### 2.3 Lote 4 — nível das referências do mercado (aval geral de 29/10)

Pedido: chegar perto do nível dos líderes do gênero (GDD v1.2 §5.13, COMPETITIVO §g), sem copiar modelo, arte ou marca de ninguém. Uma peça por objeto: as variantes saem por cor no Blender (GDD §10). Todas em HD (4K, 50 000 faces); personagens com rig. Custo: 1 rodada de imagem (8) + HD (55), + rig (20) nos personagens.

| # | Peça | Família | Para quê | Créditos |
|---|---|---|---|---|
| 45 | `executivo` (terno) | passageiro | variedade da fila | 83 |
| 46 | `turista` (camisa florida, chapéu de palha) | passageiro | Costa Azul | 83 |
| 47 | `mochileiro` (mochila é o item 27) | passageiro | variedade | 83 |
| 48 | `mae` (faz par com a `crianca`) | passageiro | família na fila | 83 |
| 49 | `faxineiro` (colete verde-limão) | funcionário | equipe de limpeza | 83 |
| 50 | `carrinho_limpeza` | item | anda com o faxineiro | 63 |
| 51 | `caminhao_catering` | veículo | serviço de bordo | 63 |
| 52 | `posto_combustivel` (tanques) | prédio | pátio | 63 |
| 53 | `terminal_carga` | prédio | rotas de carga | 63 |
| 54 | `cone` | prop de mapa | pátio | 63 |
| 55 | `cerca` (1 segmento) | prop de mapa | perímetro | 63 |
| 56 | `arvore` | prop de mapa | Vale Verde | 63 |
| 57 | `palmeira` | prop de mapa | Costa Azul | 63 |
| 58 | `poste` | prop de mapa | pátio e estacionamento | 63 |
| 59 | `placa_direcao` (setas sem texto) | prop de mapa | terminal | 63 |
| 60 | `banco` | prop de mapa | área externa | 63 |
| 61 | `carrinho_mao` (carrinho de bagagem do passageiro) | prop | saguão | 63 |
| 62 | `ala_terminal` (módulo de expansão) | terminal | crescimento do aeroporto | 63 |
| 63 | `sala_tripulacao` | prédio | tripulação | 63 |
| 64 | `oficina_mecanico` | prédio | manutenção | 63 |
| 65 | `caminhao_bombeiro` | veículo | incidentes | 63 |
| 66 | `carro_follow_me` | veículo | táxi dos aviões | 63 |
| 67 | `conteiner_bagagem` (ULD) | prop | bagagem e carga | 63 |
| 68 | `biruta` | prop de mapa | frente fria (TA-4) | 63 |
| 69 | `radar` | prop de mapa | torre e despacho | 63 |

Total do Lote 4: 5 × 83 + 20 × 63 = **1 675**. A taxiway com marcação sai por tile/textura, sem modelo.

## 5. Marca e arte de instalação (pedido do Vinicius, 2026-10-29)

Feitas no gerador de imagem do Tripo (Nano Banana 2, 1024 px) e ampliadas para 4K (10 créditos cada). Entram no git (interface, não modelo pago), em `client/Assets/_TA/Art/Marca/`.

| Peça | Uso | Observação |
|---|---|---|
| `logo_little_terminal` | logo do título, splash, loja | com o nome provisório "Little Terminal": se o D-03 mudar o nome, refazer (1 rodada de imagem) |
| `emblema` | selo sem texto (avião + terminal + asas douradas) | serve com qualquer nome |
| `icone_app` | ícone do launcher (arte de instalação) | sem texto; o adaptativo sai por recorte (personagem até 0,64 da camada, prompt mestre §8) |
| `splash` | tela de abertura 9:16 | céu livre no terço de cima para a logo |

O que **não** se gera (0 crédito): as 8 pinturas de avião (3 silhuetas da prioridade 2 + 2 da 3, cor no Blender), uniformes por cor, roupas de passageiro por cor, o Marco, a hélice girando (disco borrado por script) e o trem de pouso (escondido no voo por script), pista, pátio e grama (tiles pintados ou textura grátis do Tripo), quadro de partidas (interface).

## 3. Receita no Tripo (a do DC, conferida na 1ª peça)

1. Imagem de referência no gerador embutido (Nano Banana 2, 1:1, 4 opções), com assunto + sufixos abaixo. Era grátis até a cota de imagens grátis acabar no meio do dia; depois, **8 créditos por rodada de 4**. Escolha pela leitura no ângulo da câmera do jogo.
2. Modelo HD (H3.1), conferido a cada peça: 1ª versão com botão "Generate 45" (textura 2K, ~8 000 faces); **versão canônica com "Generate 55" (textura 4K, 50 000 faces)**. Triângulos, Ultra, PBR, remover iluminação ligado, 8K desligado, Gerar em Partes desligado, **Privado**.
3. Personagem: Auto Rig humanoide **Mixamo** (+20) pela URL `workspace/rigging/<task>`; export FBX **Mixamo 2K com esqueleto**. Objeto: export FBX **Blender 2K**. A predefinição "lembra" a anterior: confira a cada export.
4. Download para `arte/tripo/<peça>/` do checkout principal (fora do git), com cópia em `C:\Users\VINICIUS\3D Objects\TinyAirport-BACKUP\arte\tripo\`.

### Sufixos de prompt (inglês, depois do assunto)

- `[S]` personagem: "stylized cozy low-poly mobile game character, chunky rounded proportions about 3 heads tall, big head and hands, flat colors with soft two-tone shading, clean readable silhouette, single character, centered, no background, no text, no logo"
- `[T]` pose: "T-pose, arms straight out horizontal, legs slightly apart, open empty hands, nothing held in the hands, neutral friendly face, symmetrical, fitted clothes on the legs"
- `[I]` imagem de personagem: "full body front view, T-pose arms horizontal, soft even studio light, plain light grey background, 3D render look, no ground shadow"
- `[P]` objeto: "stylized cozy low-poly mobile game prop, chunky rounded beveled shapes, flat colors with soft two-tone shading, single object, centered, no background, no text, no logo"
- `[IP]` imagem de objeto: "3/4 front view product shot, plain light grey background, soft even light, no ground shadow"
- `[A]` avião: "generic fictional airplane, plain white fuselage, light grey belly, dark blue windows, no airline livery, no logo, no text, not a real aircraft model, toy-like rounded proportions, landing gear down"

## 4. Animação (0 crédito)

Os clipes vêm do disco (GDD_ORIGEM, tabela "Clipes reaproveitados"): andar, idle de fila, paciência baixa, carregar bagagem, correr, guarda, entregar cartão, arremessar mala, chamar, comemorar, acenar e dar de ombros. Puxar a mala de rodinha não existe no disco: clipe novo no `anim_blender.py`. Aviões não têm esqueleto: pouso, táxi e decolagem por tween no Unity, a partir de 8 direções do modelo parado.
