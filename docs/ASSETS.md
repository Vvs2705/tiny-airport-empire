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

### 2.4 Lote 5 — prédio que muda a cada nível e mapa que cresce (aval geral de 29/10)

O GDD §5.13 pede que "cada upgrade muda o prédio na tela" e que o mapa cresça por terrenos (com heliponto e hangares nas referências, COMPETITIVO §g). As 8 estações ganham um **nível 2** com desenho próprio (mais balcões, mais máquinas, maior), não uma troca de cor. Mais 3 silhuetas de aeronave e as peças de obra dos terrenos bloqueados. Rodadas de imagem com 2 opções (4 créditos).

| # | Peça | Para quê | Créditos |
|---|---|---|---|
| 70 | `balcao_checkin_n2` (4 balcões) | check-in nível 2 | 4 + 55 |
| 71 | `raio_x_n2` (2 pórticos, 2 esteiras) | segurança nível 2 | 4 + 55 |
| 72 | `esteira_bagagem_n2` (carrossel longo) | bagagem nível 2 | 4 + 55 |
| 73 | `portao_embarque_n2` (sala com assentos) | portão nível 2 | 4 + 55 |
| 74 | `loja_n2` (boutique com vitrine) | loja nível 2 | 4 + 55 |
| 75 | `alimentacao_n2` (café com mesas) | alimentação nível 2 | 4 + 55 |
| 76 | `hangar_manutencao_n2` (com ponte rolante) | manutenção nível 2 | 4 + 55 |
| 77 | `torre_controle_n2` (torre alta com radar) | despacho nível 2 | 4 + 55 |
| 78 | `helicoptero` | heliponto (Costa Azul e Aurora) | 4 + 55 |
| 79 | `jato_executivo` | rota executiva, Aurora | 4 + 55 |
| 80 | `hidroaviao` | rota turística, Costa Azul | 4 + 55 |
| 81 | `guindaste_obra` | terreno em obra | 4 + 55 |
| 82 | `barreira_obra` | terreno bloqueado | 4 + 55 |
| 83 | `canteiro_obra` | terreno em obra | 4 + 55 |

Total do Lote 5: 14 × 59 = **826**.

### 2.5 Lote 6 — nível máximo das estações e terminais crescidos (aval geral de 29/10)

Completa a escada de upgrade visual do GDD §5.13: cada estação tem nível 1 (§1), nível 2 (§2.4) e agora o **nível 3**, o mais vistoso; os 3 terminais ganham o **nível 2** (mais andares e pontes de embarque). Desenhos próprios, não troca de cor. Rodadas de imagem com 2 opções (4 créditos).

| # | Peça | Para quê | Créditos |
|---|---|---|---|
| 84 | `balcao_checkin_n3` (ilha com 6 balcões e autoatendimento) | check-in nível 3 | 4 + 55 |
| 85 | `raio_x_n3` (3 scanners corporais e 2 esteiras) | segurança nível 3 | 4 + 55 |
| 86 | `esteira_bagagem_n3` (2 carrosséis) | bagagem nível 3 | 4 + 55 |
| 87 | `portao_embarque_n3` (sala premium com janela para o pátio) | portão nível 3 | 4 + 55 |
| 88 | `loja_n3` (pavilhão de 2 andares) | loja nível 3 | 4 + 55 |
| 89 | `alimentacao_n3` (praça com 3 quiosques) | alimentação nível 3 | 4 + 55 |
| 90 | `hangar_manutencao_n3` (2 hangares e banco de teste) | manutenção nível 3 | 4 + 55 |
| 91 | `torre_controle_n3` (torre alta com prédio de operações) | despacho nível 3 | 4 + 55 |
| 92 | `terminal_vale_verde_n2` | Vale Verde crescido | 4 + 55 |
| 93 | `terminal_costa_azul_n2` | Costa Azul crescido | 4 + 55 |
| 94 | `terminal_aurora_n2` | Aurora Global crescido | 4 + 55 |

Total do Lote 6: 11 × 59 = **649**. Gerado inteiro; 5 dos 11 exportados (os 4 primeiros e o `terminal_aurora_n2`), **6 exports pendentes e bloqueados** (resolvido em 30/10: as 6 foram regeradas na conta nova, `PROVENIENCIA.md` §2.4): faltou RAM no PC naquela hora e, quando o PC liberou, o plano Max já tinha vencido (~17:10) e o plano gratuito não exporta (`PROVENIENCIA.md` §1).

### 2.6 Lote 7 — gerado e exportado em 2026-10-30 na conta nova

Em 29/10 o plano Max venceu (~17:10) com 6 318 créditos sem uso e o lote não saiu. Em 30/10 o Vinicius criou a conta nova (25 200 créditos, Max até 29/11) e autorizou "todas as imagens e todos os materiais" (`PROVENIENCIA.md` §1). As 6 peças presas da conta antiga saíram de novo como **grupo A** (6 × 55 = 330), e o Lote 7 saiu inteiro, na ordem abaixo, com os 82 zips guardados em `arte/tripo/` (§2.4 da proveniência). Todas em HD (4K, 50 000 faces, Privado); imagem com 2 opções, **0 crédito** nesta conta; personagem com rig (+20). Sem texto nem marca de terceiros; uniforme em cor lisa para a máscara.

| Grupo | Peças | Créditos |
|---|---|---|
| 1. Nível 3 dos terminais (3) | `terminal_vale_verde_n3`, `terminal_costa_azul_n3`, `terminal_aurora_n3` | 3 × 59 = 177 |
| 2. Aeronaves (3) | `aviao_jato_regional` (cauda em T), `aviao_bimotor_helice`, `aviao_wide_quad` (jumbo de 4 motores). As 8 variantes do contrato (item 3) **já estão cobertas** pelas 8 aeronaves geradas (regional, narrow, wide, turístico, cargo, helicóptero, jato executivo, hidroavião); estas 3 são silhuetas extras, brancas, para pintar por cor | 3 × 59 = 177 |
| 3. Personagens (16) | `cozinheira`, `cozinheiro`, `garcom` (alimentação); `agente_portao`, `agente_portao_f` (portão); `pilota`, `mecanica`, `seguranca_f`, `bagageira`, `despachante_f`, `atendente_m`, `vendedora` (variantes femininas e masculinas dos 6 perfis); `adolescente`, `passageira_executiva`, `surfista`, `passageira_turista` (passageiros) | 16 × 79 = 1 264 |
| 4. Identidade dos aeroportos (17) | Costa Azul: `farol_costeiro`, `doca_hidroaviao`, `quiosque_praia`, `torre_controle_costa`, `hangar_costa`, `veleiro`, `guarda_sol_espreguicadeiras`, `resort_hotel`. Aurora Global: `torre_controle_aurora`, `hotel_aeroporto`, `garagem_estacionamento`, `monotrilho`, `escultura_dourada`, `lounge_vip`, `terminal_satelite`, `hangar_aurora`, `skyline_torres` | 17 × 59 = 1 003 |
| 5. Incidentes (5) | `ambulancia`, `viatura_seguranca`, `varredora_pista`, `caminhao_degelo` (frente fria, TA-4), `trator_remocao` | 5 × 59 = 295 |
| 6. Interior (14) | `portico_detector`, `prateleiras_loja`, `mesas_alimentacao`, `totem_autoatendimento`, `escada_rolante`, `esteira_rolante`, `maquina_venda`, `balcao_informacoes`, `bandeja_seguranca`, `lixeira_terminal`, `catraca_embarque`, `vitrine_loja`, `vitrine_cafe`, `carrinho_cafe`. Balcão de check-in, raio-x, esteira de bagagem e cadeiras de portão já existem (§1, §2.4, §2.5) e não se repetem | 14 × 59 = 826 |
| 7. O que couber (18) | `hidrante`, `container_lixo`, `arbusto`, `canteiro_flores`, `luz_pista`, `gerador_solo`, `esteira_carga`, `van_translado`, `taxi`, `onibus_cidade`, `ponto_onibus`, `guarita_cancela`, `heliponto`, `tanque_agua`, `turbina_eolica`, `painel_solar`, `carro_passageiro`, `caminhonete_pista` | 18 × 59 = 1 062 |

Total do Lote 7: 76 peças = 60 objetos × 55 + 16 personagens × (55 + 20) = **4 500** créditos (a estimativa de 4 804 contava 4 por imagem; nesta conta a imagem não cobra).

**Auditoria de 30/10** (`docs/ASSETS.md` §1 a §2.5 contra `arte/tripo/`): as 94 peças dos Lotes 1 a 6 têm o zip HD no disco; nenhuma precisou ser regerada. As 8 variantes de aeronave do contrato (item 3) já estavam cobertas desde o Lote 5.

### 2.7 Lote 8 — variedade útil ao jogo (30/10, aval do Vinicius: "todas as imagens, e todos os materiais")

Pedido da sessão coordenadora: depois do Lote 7, gerar o que falta para o lançamento e a variedade útil, sem repetir peça e sem gastar crédito em peça que não entra no jogo. 44 peças (38 objetos × 55 + 6 personagens × 75 = **2 540** créditos), cada uma ligada a uma linha do GDD:

| Grupo | Peças | Para quê |
|---|---|---|
| A. Vale Verde rural (8) | `celeiro`, `silo_graos`, `moinho_vento`, `trator_agricola`, `fardos_feno`, `casa_fazenda`, `estufa_vidro`, `espantalho` | identidade do aeroporto regional (GDD §5.10) |
| B. Pátio e carga (10) | `caminhao_agua_potavel`, `caminhao_lavatorio`, `empilhadeira`, `carreta_dolly`, `paletes_carga`, `caixotes_carga`, `calcos_roda`, `luzes_aproximacao`, `placa_taxiway`, `barreira_jato` | pátio visível em cada voo (§5.12), rotas de carga |
| C. Interior extra (8) | `sofa_lounge`, `mesa_lounge`, `bar_lounge`, `balanca_bagagem`, `caixa_registradora`, `bebedouro`, `cadeira_rodas`, `carrinho_bebe` | lounge da Aurora, check-in, loja, assistência e família |
| D. Terreno e mapa (8) | `colina_gramada`, `rochas_grandes`, `lago_pequeno`, `duna_praia`, `ilha_pequena`, `fonte_praca`, `arco_entrada`, `caminho_pedra` | mapa que cresce, bordas e praça (§5.13) |
| E. Personagens (6) | `passageira_idosa`, `pai_familia`, `menino`, `menina`, `atleta`, `gerente_terminal` | variedade da fila e chefe de terminal |
| F. Economia (4) | `cofre_dinheiro`, `pilha_moedas`, `bau_recompensa`, `caixa_presente` | tela do cofre (offline) e recompensas |

### 2.8 Lote 9 — passageiros de variedade (30/10)

8 personagens com rig (8 × 75 = **600** créditos): `passageiro_hoodie`, `passageira_casaco`, `passageiro_robusto`, `passageira_cacheada`, `estudante`, `passageiro_idoso_b`, `passageira_capa_chuva`, `passageiro_careca`. Variam corpo, cabelo, idade e roupa, para a fila não repetir silhueta.

### 2.9 Lote 10 — incidentes, eventos e fila (30/10)

14 peças (13 objetos × 55 + 1 personagem × 75 = **790** créditos), ligadas ao GDD §5.8 (falha de equipamento, frente fria), ao §6.5 (eventos Air Show e Holiday) e à fila no rush: `placa_fora_de_servico`, `cavalete_manutencao`, `placa_piso_molhado`, `escada_manutencao`, `limpa_neve`, `aviao_acrobatico`, `arquibancada`, `balao_ar_quente`, `bandeirolas`, `tenda_evento`, `arvore_natal`, `boneco_neve`, `cordao_fila`, `garconete`.

Total dos Lotes 8 a 10: **3 930** créditos. Saldo no fim do dia: **16 440** (Max até 29/11). O que cada peça é para a integração está em `docs/INTEGRACAO_ARTE.md`.

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
