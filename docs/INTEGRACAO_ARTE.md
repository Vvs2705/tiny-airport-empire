# Little Terminal — Arte para integrar no jogo (2026-10-30)

**Estado:** 236 peças HD no disco (4K, ~50 000 faces, Privado, licença `tripo_pago`), todas com a origem e o SHA em `PROVENIENCIA.md` e a lista de pedido em `ASSETS.md`. Esta página é o roteiro da etapa de integração; nenhum modelo foi convertido ainda.

## Regras de integração

- **Onde está:** `arte/tripo/<peça>_hd/<peça>_hd.zip` (FBX + texturas 4K) no checkout principal, fora do git (o repositório é público e modelo pago não vai para ele (decisão do estúdio)), com cópia em `C:\Users\VINICIUS\3D Objects\TinyAirport-BACKUP\arte\tripo\`. O que entra no git são os sprites e as malhas leves já decimadas, nunca o zip.
- **Rota (GDD §10.2, D-18):** pessoas viram **sprites** (Blender → `render_sprites.py`; rig Mixamo, já feito, com `Export Skeleton`); prédios, aviões, veículos e props entram como **malha em tempo real**, decimada no Blender (low-poly, material simples, pivô no chão, 1 unidade = 1 m).
- **Uniforme:** nos funcionários e na tripulação, o uniforme é uma cor lisa; no Blender vai para um material próprio e a View tinge a máscara por setor (§5.5).
- **Aviões:** brancos e lisos, sem logotipo; a pintura é troca de cor no material. 12 modelos cobrem as 8 variantes do contrato (item 3).
- **Níveis:** estações e terminais têm 3 níveis com desenho próprio; a View troca a malha ao subir de nível.
- **Marca e texto:** nada tem texto nem marca de terceiros (`ART_BIBLE.md`). Placas de pista, sinais e quadros saem em branco; o texto entra pela interface.
- **Antes de integrar:** conferir cada malha no Blender (escala, pivô, normais) e a contagem de triângulos por tela; o teto de 4096 px por textura vale para as folhas de sprite.

## 1. Estações (8 × 3 níveis; malha em tempo real, troca por nível)

| Peça | Pasta (`arte/tripo/`) | Uso no jogo | Rota |
|---|---|---|---|
| `balcao_checkin` | `balcao_checkin_hd/` | estação check-in, nível 1 | malha |
| `balcao_checkin_n2` | `balcao_checkin_n2_hd/` | estação check-in, nível 2 | malha |
| `balcao_checkin_n3` | `balcao_checkin_n3_hd/` | estação check-in, nível 3 | malha |
| `raio_x` | `raio_x_hd/` | estação segurança, nível 1 | malha |
| `raio_x_n2` | `raio_x_n2_hd/` | estação segurança, nível 2 | malha |
| `raio_x_n3` | `raio_x_n3_hd/` | estação segurança, nível 3 | malha |
| `esteira_bagagem` | `esteira_bagagem_hd/` | estação bagagem, nível 1 | malha |
| `esteira_bagagem_n2` | `esteira_bagagem_n2_hd/` | estação bagagem, nível 2 | malha |
| `esteira_bagagem_n3` | `esteira_bagagem_n3_hd/` | estação bagagem, nível 3 | malha |
| `portao_embarque` | `portao_embarque_hd/` | estação portão, nível 1 | malha |
| `portao_embarque_n2` | `portao_embarque_n2_hd/` | estação portão, nível 2 | malha |
| `portao_embarque_n3` | `portao_embarque_n3_hd/` | estação portão, nível 3 | malha |
| `loja` | `loja_hd/` | estação loja, nível 1 | malha |
| `loja_n2` | `loja_n2_hd/` | estação loja, nível 2 | malha |
| `loja_n3` | `loja_n3_hd/` | estação loja, nível 3 | malha |
| `alimentacao` | `alimentacao_hd/` | estação alimentação, nível 1 | malha |
| `alimentacao_n2` | `alimentacao_n2_hd/` | estação alimentação, nível 2 | malha |
| `alimentacao_n3` | `alimentacao_n3_hd/` | estação alimentação, nível 3 | malha |
| `hangar_manutencao` | `hangar_manutencao_hd/` | estação manutenção, nível 1 | malha |
| `hangar_manutencao_n2` | `hangar_manutencao_n2_hd/` | estação manutenção, nível 2 | malha |
| `hangar_manutencao_n3` | `hangar_manutencao_n3_hd/` | estação manutenção, nível 3 | malha |
| `torre_controle` | `torre_controle_hd/` | estação despacho, nível 1 | malha |
| `torre_controle_n2` | `torre_controle_n2_hd/` | estação despacho, nível 2 | malha |
| `torre_controle_n3` | `torre_controle_n3_hd/` | estação despacho, nível 3 | malha |

## 2. Terminais e prédios de operação (malha)

| Peça | Pasta (`arte/tripo/`) | Uso no jogo | Rota |
|---|---|---|---|
| `terminal_vale_verde` | `terminal_vale_verde_hd/` | Vale Verde, nível 1 | malha |
| `terminal_vale_verde_n2` | `terminal_vale_verde_n2_hd/` | Vale Verde, nível 2 | malha |
| `terminal_vale_verde_n3` | `terminal_vale_verde_n3_hd/` | Vale Verde, nível 3 | malha |
| `terminal_costa_azul` | `terminal_costa_azul_hd/` | Costa Azul, nível 1 | malha |
| `terminal_costa_azul_n2` | `terminal_costa_azul_n2_hd/` | Costa Azul, nível 2 | malha |
| `terminal_costa_azul_n3` | `terminal_costa_azul_n3_hd/` | Costa Azul, nível 3 | malha |
| `terminal_aurora` | `terminal_aurora_hd/` | Aurora Global, nível 1 | malha |
| `terminal_aurora_n2` | `terminal_aurora_n2_hd/` | Aurora Global, nível 2 | malha |
| `terminal_aurora_n3` | `terminal_aurora_n3_hd/` | Aurora Global, nível 3 | malha |
| `ala_terminal` | `ala_terminal_hd/` | módulo de expansão do terminal | malha |
| `terminal_satelite` | `terminal_satelite_hd/` | ala satélite do hub (Aurora) | malha |
| `terminal_carga` | `terminal_carga_hd/` | rotas de carga | malha |
| `ponte_embarque` | `ponte_embarque_hd/` | ponte de embarque (finger) | malha |
| `sala_tripulacao` | `sala_tripulacao_hd/` | sala da tripulação (§5.11) | malha |
| `oficina_mecanico` | `oficina_mecanico_hd/` | oficina de manutenção (§5.12) | malha |
| `posto_combustivel` | `posto_combustivel_hd/` | tanques de combustível do pátio | malha |

## 3. Aeronaves (malha branca lisa; pintura por troca de cor, sem logotipo)

| Peça | Pasta (`arte/tripo/`) | Uso no jogo | Rota |
|---|---|---|---|
| `aviao_regional` | `aviao_regional_hd/` | turboélice de asa alta (regional) | malha |
| `aviao_narrow` | `aviao_narrow_hd/` | jato de corredor único | malha |
| `aviao_wide` | `aviao_wide_hd/` | jato de dois corredores | malha |
| `aviao_turistico` | `aviao_turistico_hd/` | monomotor pequeno (Costa Azul) | malha |
| `aviao_cargo` | `aviao_cargo_hd/` | cargueiro de nariz alto | malha |
| `helicoptero` | `helicoptero_hd/` | heliponto (Costa Azul e Aurora) | malha |
| `jato_executivo` | `jato_executivo_hd/` | rota executiva (Aurora) | malha |
| `hidroaviao` | `hidroaviao_hd/` | rota turística (Costa Azul) | malha |
| `aviao_jato_regional` | `aviao_jato_regional_hd/` | jato regional de cauda em T | malha |
| `aviao_bimotor_helice` | `aviao_bimotor_helice_hd/` | bimotor de hélice (commuter) | malha |
| `aviao_wide_quad` | `aviao_wide_quad_hd/` | jumbo de 4 motores | malha |
| `aviao_acrobatico` | `aviao_acrobatico_hd/` | biplano do evento Air Show | malha |

## 4. Veículos de pátio, terra e incidentes (malha)

| Peça | Pasta (`arte/tripo/`) | Uso no jogo | Rota |
|---|---|---|---|
| `rebocador` | `rebocador_hd/` | pushback | malha |
| `carrinho_bagagem` | `carrinho_bagagem_hd/` | trator com carretas | malha |
| `caminhao_combustivel` | `caminhao_combustivel_hd/` | abastecimento | malha |
| `escada_embarque` | `escada_embarque_hd/` | caminhão-escada | malha |
| `onibus_patio` | `onibus_patio_hd/` | ônibus de pátio | malha |
| `caminhao_catering` | `caminhao_catering_hd/` | serviço de bordo | malha |
| `caminhao_bombeiro` | `caminhao_bombeiro_hd/` | incidentes | malha |
| `carro_follow_me` | `carro_follow_me_hd/` | guia dos aviões | malha |
| `caminhao_agua_potavel` | `caminhao_agua_potavel_hd/` | serviço de água | malha |
| `caminhao_lavatorio` | `caminhao_lavatorio_hd/` | serviço de lavatório | malha |
| `empilhadeira` | `empilhadeira_hd/` | carga | malha |
| `carreta_dolly` | `carreta_dolly_hd/` | carreta de bagagem | malha |
| `esteira_carga` | `esteira_carga_hd/` | carregador de bagagem | malha |
| `gerador_solo` | `gerador_solo_hd/` | energia em solo | malha |
| `ambulancia` | `ambulancia_hd/` | incidente médico | malha |
| `viatura_seguranca` | `viatura_seguranca_hd/` | segurança do pátio | malha |
| `varredora_pista` | `varredora_pista_hd/` | limpeza de pista | malha |
| `caminhao_degelo` | `caminhao_degelo_hd/` | frente fria (TA-4) | malha |
| `trator_remocao` | `trator_remocao_hd/` | remoção de aeronave | malha |
| `limpa_neve` | `limpa_neve_hd/` | frente fria (TA-4) | malha |
| `van_translado` | `van_translado_hd/` | translado | malha |
| `taxi` | `taxi_hd/` | tráfego da cidade | malha |
| `onibus_cidade` | `onibus_cidade_hd/` | tráfego da cidade | malha |
| `carro_passageiro` | `carro_passageiro_hd/` | tráfego da cidade | malha |
| `caminhonete_pista` | `caminhonete_pista_hd/` | operações de pista | malha |
| `trator_agricola` | `trator_agricola_hd/` | Vale Verde | malha |

## 5. Funcionários e tripulação (rig Mixamo; rota de sprites com máscara de uniforme)

| Peça | Pasta (`arte/tripo/`) | Uso no jogo | Rota |
|---|---|---|---|
| `atendente` | `atendente_hd/` | check-in / portão (azul-marinho) | sprite (rig Mixamo) |
| `atendente_m` | `atendente_m_hd/` | check-in (variante masculina) | sprite (rig Mixamo) |
| `agente_portao` | `agente_portao_hd/` | portão (masculino) | sprite (rig Mixamo) |
| `agente_portao_f` | `agente_portao_f_hd/` | portão (feminino) | sprite (rig Mixamo) |
| `seguranca` | `seguranca_hd/` | segurança | sprite (rig Mixamo) |
| `seguranca_f` | `seguranca_f_hd/` | segurança (variante feminina) | sprite (rig Mixamo) |
| `bagageiro` | `bagageiro_hd/` | bagagem | sprite (rig Mixamo) |
| `bagageira` | `bagageira_hd/` | bagagem (variante feminina) | sprite (rig Mixamo) |
| `vendedor` | `vendedor_hd/` | loja | sprite (rig Mixamo) |
| `vendedora` | `vendedora_hd/` | loja (variante feminina) | sprite (rig Mixamo) |
| `mecanico` | `mecanico_hd/` | manutenção | sprite (rig Mixamo) |
| `mecanica` | `mecanica_hd/` | manutenção (variante feminina) | sprite (rig Mixamo) |
| `despachante` | `despachante_hd/` | despacho | sprite (rig Mixamo) |
| `despachante_f` | `despachante_f_hd/` | despacho (variante feminina) | sprite (rig Mixamo) |
| `cozinheira` | `cozinheira_hd/` | alimentação | sprite (rig Mixamo) |
| `cozinheiro` | `cozinheiro_hd/` | alimentação | sprite (rig Mixamo) |
| `garcom` | `garcom_hd/` | alimentação | sprite (rig Mixamo) |
| `garconete` | `garconete_hd/` | alimentação | sprite (rig Mixamo) |
| `faxineiro` | `faxineiro_hd/` | limpeza | sprite (rig Mixamo) |
| `gerente_terminal` | `gerente_terminal_hd/` | chefe de terminal / tutorial | sprite (rig Mixamo) |
| `lara` | `lara_hd/` | Chefe de Terminal (tutorial) | sprite (rig Mixamo) |
| `piloto` | `piloto_hd/` | tripulação | sprite (rig Mixamo) |
| `pilota` | `pilota_hd/` | tripulação (variante feminina) | sprite (rig Mixamo) |
| `comissaria` | `comissaria_hd/` | tripulação de cabine | sprite (rig Mixamo) |
| `comissario` | `comissario_hd/` | tripulação de cabine | sprite (rig Mixamo) |

## 6. Passageiros (rig Mixamo; rota de sprites; a roupa varia por cor)

| Peça | Pasta (`arte/tripo/`) | Uso no jogo | Rota |
|---|---|---|---|
| `passageiro_a` | `passageiro_a_hd/` | fila | sprite (rig Mixamo) |
| `passageira_b` | `passageira_b_hd/` | fila | sprite (rig Mixamo) |
| `passageiro_idoso` | `passageiro_idoso_hd/` | fila | sprite (rig Mixamo) |
| `crianca` | `crianca_hd/` | fila / família | sprite (rig Mixamo) |
| `executivo` | `executivo_hd/` | fila | sprite (rig Mixamo) |
| `turista` | `turista_hd/` | Costa Azul | sprite (rig Mixamo) |
| `mochileiro` | `mochileiro_hd/` | fila | sprite (rig Mixamo) |
| `mae` | `mae_hd/` | família | sprite (rig Mixamo) |
| `adolescente` | `adolescente_hd/` | fila | sprite (rig Mixamo) |
| `passageira_executiva` | `passageira_executiva_hd/` | fila | sprite (rig Mixamo) |
| `surfista` | `surfista_hd/` | Costa Azul | sprite (rig Mixamo) |
| `passageira_turista` | `passageira_turista_hd/` | Costa Azul | sprite (rig Mixamo) |
| `passageira_idosa` | `passageira_idosa_hd/` | fila | sprite (rig Mixamo) |
| `pai_familia` | `pai_familia_hd/` | família | sprite (rig Mixamo) |
| `menino` | `menino_hd/` | família | sprite (rig Mixamo) |
| `menina` | `menina_hd/` | família | sprite (rig Mixamo) |
| `atleta` | `atleta_hd/` | fila | sprite (rig Mixamo) |
| `passageiro_hoodie` | `passageiro_hoodie_hd/` | variedade da fila | sprite (rig Mixamo) |
| `passageira_casaco` | `passageira_casaco_hd/` | variedade da fila | sprite (rig Mixamo) |
| `passageiro_robusto` | `passageiro_robusto_hd/` | variedade da fila | sprite (rig Mixamo) |
| `passageira_cacheada` | `passageira_cacheada_hd/` | variedade da fila | sprite (rig Mixamo) |
| `estudante` | `estudante_hd/` | variedade da fila | sprite (rig Mixamo) |
| `passageiro_idoso_b` | `passageiro_idoso_b_hd/` | variedade da fila | sprite (rig Mixamo) |
| `passageira_capa_chuva` | `passageira_capa_chuva_hd/` | variedade (chuva) | sprite (rig Mixamo) |
| `passageiro_careca` | `passageiro_careca_hd/` | variedade da fila | sprite (rig Mixamo) |

## 7. Itens presos ao osso (no Blender) e acessórios

| Peça | Pasta (`arte/tripo/`) | Uso no jogo | Rota |
|---|---|---|---|
| `mala_rodinha` | `mala_rodinha_hd/` | mão do passageiro | malha |
| `mochila` | `mochila_hd/` | costas | malha |
| `radio` | `radio_hd/` | mão do despachante | malha |
| `scanner_mao` | `scanner_mao_hd/` | mão da segurança | malha |
| `bastoes_sinalizacao` | `bastoes_sinalizacao_hd/` | mãos do despachante | malha |
| `caixa_ferramentas` | `caixa_ferramentas_hd/` | mão do mecânico | malha |
| `carrinho_limpeza` | `carrinho_limpeza_hd/` | faxineiro | malha |
| `carrinho_mao` | `carrinho_mao_hd/` | saguão | malha |
| `conteiner_bagagem` | `conteiner_bagagem_hd/` | carga | malha |
| `paletes_carga` | `paletes_carga_hd/` | carga | malha |
| `caixotes_carga` | `caixotes_carga_hd/` | carga | malha |
| `calcos_roda` | `calcos_roda_hd/` | pátio | malha |

## 8. Interior do terminal (malha)

| Peça | Pasta (`arte/tripo/`) | Uso no jogo | Rota |
|---|---|---|---|
| `cadeiras_espera` | `cadeiras_espera_hd/` | portão | malha |
| `vaso_planta` | `vaso_planta_hd/` | decoração | malha |
| `portico_detector` | `portico_detector_hd/` | segurança | malha |
| `prateleiras_loja` | `prateleiras_loja_hd/` | loja | malha |
| `mesas_alimentacao` | `mesas_alimentacao_hd/` | alimentação | malha |
| `totem_autoatendimento` | `totem_autoatendimento_hd/` | check-in | malha |
| `escada_rolante` | `escada_rolante_hd/` | terminal | malha |
| `esteira_rolante` | `esteira_rolante_hd/` | terminal | malha |
| `maquina_venda` | `maquina_venda_hd/` | terminal | malha |
| `balcao_informacoes` | `balcao_informacoes_hd/` | terminal | malha |
| `bandeja_seguranca` | `bandeja_seguranca_hd/` | segurança | malha |
| `lixeira_terminal` | `lixeira_terminal_hd/` | terminal | malha |
| `catraca_embarque` | `catraca_embarque_hd/` | portão | malha |
| `vitrine_loja` | `vitrine_loja_hd/` | loja | malha |
| `vitrine_cafe` | `vitrine_cafe_hd/` | alimentação | malha |
| `carrinho_cafe` | `carrinho_cafe_hd/` | alimentação | malha |
| `sofa_lounge` | `sofa_lounge_hd/` | lounge VIP (Aurora) | malha |
| `mesa_lounge` | `mesa_lounge_hd/` | lounge VIP | malha |
| `bar_lounge` | `bar_lounge_hd/` | lounge VIP | malha |
| `balanca_bagagem` | `balanca_bagagem_hd/` | check-in | malha |
| `caixa_registradora` | `caixa_registradora_hd/` | loja | malha |
| `bebedouro` | `bebedouro_hd/` | terminal | malha |
| `cadeira_rodas` | `cadeira_rodas_hd/` | assistência | malha |
| `carrinho_bebe` | `carrinho_bebe_hd/` | família | malha |
| `cordao_fila` | `cordao_fila_hd/` | fila (rush hour) | malha |

## 9. Props de mapa e pátio (malha)

| Peça | Pasta (`arte/tripo/`) | Uso no jogo | Rota |
|---|---|---|---|
| `cone` | `cone_hd/` | pátio | malha |
| `cerca` | `cerca_hd/` | perímetro | malha |
| `arvore` | `arvore_hd/` | Vale Verde | malha |
| `palmeira` | `palmeira_hd/` | Costa Azul | malha |
| `poste` | `poste_hd/` | iluminação | malha |
| `placa_direcao` | `placa_direcao_hd/` | setas sem texto | malha |
| `banco` | `banco_hd/` | área externa | malha |
| `biruta` | `biruta_hd/` | frente fria | malha |
| `radar` | `radar_hd/` | torre | malha |
| `hidrante` | `hidrante_hd/` | pátio | malha |
| `container_lixo` | `container_lixo_hd/` | pátio | malha |
| `arbusto` | `arbusto_hd/` | jardim | malha |
| `canteiro_flores` | `canteiro_flores_hd/` | jardim | malha |
| `luz_pista` | `luz_pista_hd/` | pista | malha |
| `luzes_aproximacao` | `luzes_aproximacao_hd/` | pista | malha |
| `placa_taxiway` | `placa_taxiway_hd/` | pista (sem texto) | malha |
| `barreira_jato` | `barreira_jato_hd/` | pista | malha |
| `ponto_onibus` | `ponto_onibus_hd/` | cidade | malha |
| `guarita_cancela` | `guarita_cancela_hd/` | entrada | malha |
| `heliponto` | `heliponto_hd/` | heliponto | malha |
| `tanque_agua` | `tanque_agua_hd/` | pátio | malha |
| `turbina_eolica` | `turbina_eolica_hd/` | energia | malha |
| `painel_solar` | `painel_solar_hd/` | energia | malha |
| `arco_entrada` | `arco_entrada_hd/` | entrada do aeroporto | malha |
| `fonte_praca` | `fonte_praca_hd/` | praça | malha |
| `caminho_pedra` | `caminho_pedra_hd/` | calçada | malha |

## 10. Terreno, obra e mapa que cresce (malha)

| Peça | Pasta (`arte/tripo/`) | Uso no jogo | Rota |
|---|---|---|---|
| `guindaste_obra` | `guindaste_obra_hd/` | terreno em obra | malha |
| `barreira_obra` | `barreira_obra_hd/` | terreno bloqueado | malha |
| `canteiro_obra` | `canteiro_obra_hd/` | terreno em obra | malha |
| `colina_gramada` | `colina_gramada_hd/` | borda do mapa | malha |
| `rochas_grandes` | `rochas_grandes_hd/` | borda do mapa | malha |
| `lago_pequeno` | `lago_pequeno_hd/` | terreno | malha |
| `duna_praia` | `duna_praia_hd/` | Costa Azul | malha |
| `ilha_pequena` | `ilha_pequena_hd/` | Costa Azul | malha |

## 11. Identidade dos aeroportos (malha)

| Peça | Pasta (`arte/tripo/`) | Uso no jogo | Rota |
|---|---|---|---|
| `celeiro` | `celeiro_hd/` | Vale Verde | malha |
| `silo_graos` | `silo_graos_hd/` | Vale Verde | malha |
| `moinho_vento` | `moinho_vento_hd/` | Vale Verde | malha |
| `fardos_feno` | `fardos_feno_hd/` | Vale Verde | malha |
| `casa_fazenda` | `casa_fazenda_hd/` | Vale Verde | malha |
| `estufa_vidro` | `estufa_vidro_hd/` | Vale Verde | malha |
| `espantalho` | `espantalho_hd/` | Vale Verde | malha |
| `farol_costeiro` | `farol_costeiro_hd/` | Costa Azul | malha |
| `doca_hidroaviao` | `doca_hidroaviao_hd/` | Costa Azul | malha |
| `quiosque_praia` | `quiosque_praia_hd/` | Costa Azul | malha |
| `torre_controle_costa` | `torre_controle_costa_hd/` | Costa Azul | malha |
| `hangar_costa` | `hangar_costa_hd/` | Costa Azul | malha |
| `veleiro` | `veleiro_hd/` | Costa Azul | malha |
| `guarda_sol_espreguicadeiras` | `guarda_sol_espreguicadeiras_hd/` | Costa Azul | malha |
| `resort_hotel` | `resort_hotel_hd/` | Costa Azul | malha |
| `torre_controle_aurora` | `torre_controle_aurora_hd/` | Aurora Global | malha |
| `hotel_aeroporto` | `hotel_aeroporto_hd/` | Aurora Global | malha |
| `garagem_estacionamento` | `garagem_estacionamento_hd/` | Aurora Global | malha |
| `monotrilho` | `monotrilho_hd/` | Aurora Global | malha |
| `escultura_dourada` | `escultura_dourada_hd/` | Aurora Global | malha |
| `lounge_vip` | `lounge_vip_hd/` | Aurora Global | malha |
| `hangar_aurora` | `hangar_aurora_hd/` | Aurora Global | malha |
| `skyline_torres` | `skyline_torres_hd/` | Aurora Global | malha |

## 12. Incidentes, eventos e economia (malha)

| Peça | Pasta (`arte/tripo/`) | Uso no jogo | Rota |
|---|---|---|---|
| `placa_fora_de_servico` | `placa_fora_de_servico_hd/` | falha de equipamento | malha |
| `cavalete_manutencao` | `cavalete_manutencao_hd/` | falha de equipamento | malha |
| `placa_piso_molhado` | `placa_piso_molhado_hd/` | falha de equipamento | malha |
| `escada_manutencao` | `escada_manutencao_hd/` | falha de equipamento | malha |
| `arquibancada` | `arquibancada_hd/` | Air Show | malha |
| `balao_ar_quente` | `balao_ar_quente_hd/` | Air Show | malha |
| `bandeirolas` | `bandeirolas_hd/` | Air Show / festa | malha |
| `tenda_evento` | `tenda_evento_hd/` | Air Show / festa | malha |
| `arvore_natal` | `arvore_natal_hd/` | Holiday | malha |
| `boneco_neve` | `boneco_neve_hd/` | Holiday | malha |
| `caixa_presente` | `caixa_presente_hd/` | recompensa | malha |
| `cofre_dinheiro` | `cofre_dinheiro_hd/` | tela do cofre (offline) | malha |
| `pilha_moedas` | `pilha_moedas_hd/` | recompensa | malha |
| `bau_recompensa` | `bau_recompensa_hd/` | recompensa | malha |

## Ordem sugerida

1. **Estações e terminais** (seções 1 e 2): o que o jogador mais vê; começar pelos níveis 1 de Vale Verde.
2. **Pessoas** (seções 5 e 6): folhas de sprite com a máscara de uniforme; passageiros por cor.
3. **Aeronaves e veículos** (seções 3 e 4), com a pintura por troca de cor.
4. **Interior, props e identidade dos aeroportos** (seções 8, 9 e 11), por aeroporto.
5. **Incidentes, eventos e terreno** (seções 10 e 12), depois que o núcleo da Fase C tiver os incidentes.
