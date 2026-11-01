# Little Terminal — Proveniência e licença de arte e áudio

## Regras

- **Sem linha, sem build:** cada asset gerado ou de terceiros entra aqui (ferramenta, data, task id, créditos, arquivo, SHA-256, licença). Sem essa linha, o asset não entra em build comercial. Formato herdado do Dungeon Courier (`06_DUNGEON_COURIER/docs/PROVENIENCIA.md`).
- **Saldo lido no site** antes e depois de cada geração (o painel mostra o saldo no topo; o script do dia leu o número antes e depois de cada clique em "Generate").
- **Aval:** D-14 aprovada em 2026-10-29 (GDD §23.2): os créditos do Tripo acabam hoje, então a lista fechada do `ASSETS.md` sai hoje, sem aval peça por peça. No mesmo dia o Vinicius pediu arte com mais presença, logo e arte de instalação (`ASSETS.md` §2.1 e §5).
- **Hash:** `sha256sum` no arquivo baixado (zip) e no FBX extraído.
- **Arquivos crus fora do git:** `arte/tripo/<peça>/` e `arte/marca/` no checkout principal (`09_TINY_AIRPORT_EMPIRE/`, ignorados pelo `.gitignore`), com cópia dos zips e dos originais da marca em `C:\Users\VINICIUS\3D Objects\TinyAirport-BACKUP\arte\`. O repositório é público e modelo pago não vai para ele (decisão do estúdio; ver §4).

## 1. Livro-razão de créditos (Tripo, conta da V-STACK)

| Data/hora (2026-10-29) | Evento | Saldo visto no site | Gasto | Obs |
|---|---|---|---|---|
| ~11:35 | Abertura do dia | **14 595** | 0 | Receita conferida no painel na 1ª peça: "Generate 45", H3.1 (Best Quality), Ultra, textura ligada, 2K, PBR, remover iluminação, triângulos, 8 000 faces, 8K desligado, Gerar em Partes desligado, **Privado**. Imagens: Nano Banana 2, 1:1, 4 por rodada |
| 11:37–11:51 | 42 modelos, 1ª versão (2K, 8 000 faces), um por peça do `ASSETS.md` | 12 673 | 1 890 | 42 × 45; o saldo caiu exatamente 45 a cada geração (§2) |
| 11:37–11:41 | Débito sem modelo entre o passageiro e o atendente | (14 550 → 14 518) | 32 | Não explicado na hora. Bate com 4 rodadas de imagem a 8 créditos, preço que o painel passou a mostrar quando a cota de imagens grátis acabou (§1, linha das 12:18) |
| 11:55–12:16 | 12 Auto Rig humanoide, predefinição Mixamo, nos personagens da 1ª versão (URL `workspace/rigging/<task>`) | 12 433 | 240 | 12 × 20. Dois cliques saíram sem cobrança (navegação rápida) e foram refeitos depois, com 1 cobrança cada |
| 11:55–12:20 | 12 exports FBX "Mixamo, 2K, Export Skeleton" dos personagens da 1ª versão | 12 433 | 0 | export não cobra |
| ~12:18 | Pedido do Vinicius: arte com presença, logo e arte de instalação. 12 rodadas de imagem: 7 peças refeitas com imagem mais rica, logo, emblema, 2 ícones de app e 1 splash 9:16 | 12 337 | 96 | 12 × 8: a cota de imagens grátis tinha acabado |
| 12:20–12:22 | 7 ampliações para 4K ("4K10") nas imagens da marca: 2 logos, emblema, 2 ícones, 2 splashes | 12 267 | 70 | 7 × 10 |
| 12:22–12:29 | 42 modelos **HD** (textura 4K, 50 000 faces, "Generate 55"), canônicos | 9 957 | 2 310 | 42 × 55; mesmas imagens da 1ª versão, menos 7 peças com imagem nova (§2, imagem "v2") |
| 12:31–13:05 | 12 Auto Rig humanoide Mixamo nos personagens HD | 9 717 | 240 | 12 × 20 |
| 12:31–13:25 | 42 exports HD: personagens "Mixamo, 4K, Export Skeleton"; objetos "Blender, 4K" | 9 717 | 0 | a resolução do export veio em 4K sozinha (a do modelo) e foi conferida a cada peça |
| 13:26 | **Fim dos Lotes 1 e 2: 4 878 créditos** | **9 717** | 4 878 | 1 890 + 32 + 240 + 96 + 70 + 2 310 + 240 |
| ~13:40 | Lote 3: 1ª tentativa de imagem barrada pelo classificador de permissões (aval repassado por outra sessão) | 9 717 | 0 | nada gerado |
| ~14:15 | Aval geral do Vinicius para o saldo do dia ("pode gerar todos e qualquer arte enquanto houver saldo"). Chrome conferido na conta da V-STACK | 9 717 | 0 | — |
| 14:16–14:18 | 27 rodadas de imagem: Lote 3 (2) e Lote 4 (25) | 9 501 | 216 | 27 × 8 |
| 14:21–14:23 | 27 modelos HD (4K, 50 000 faces) dos Lotes 3 e 4 | 8 016 | 1 485 | 27 × 55 |
| 14:24–14:35 | 7 Auto Rig humanoide Mixamo (comissária, comissário, executivo, turista, mochileiro, mãe, faxineiro) | 7 848 | 140 + 28 | 7 × 20 = 140. Os 28 a mais não foram explicados: a sessão caiu no meio do rig do faxineiro e o Chrome foi reaberto; o rig saiu uma vez só |
| 14:24–15:25 | 27 exports HD (personagens "Mixamo, 4K, Export Skeleton"; objetos "Blender, 4K") | 7 848 | 0 | — |
| 15:25 | **Fim dos Lotes 3 e 4: 1 869 créditos** | **7 848** | 1 869 | 216 + 1 485 + 168 |
| ~15:15 | Lote 5: 14 rodadas de imagem com 2 opções cada (a opção "Number of Images" estava em 2; outra sessão usa a mesma conta) | 7 792 | 56 | 14 × 4 |
| 15:18–15:19 | 14 modelos HD do Lote 5 (níveis 2 das estações, 3 aeronaves, obra) | 7 022 | 770 | 14 × 55 |
| 15:20–16:05 | 14 exports HD "Blender 4K" | 7 022 | 0 | — |
| 16:05 | **Fim do Lote 5: 826 créditos** | **7 022** | 826 | 56 + 770 |
| ~15:30 | Lote 6: 11 rodadas de imagem com 2 opções | 6 978 | 44 | 11 × 4 |
| 15:34–15:35 | 11 modelos HD do Lote 6 (nível 3 das estações e nível 2 dos terminais) | 6 373 | 605 | 11 × 55 |
| 15:40–16:10 | 4 exports HD do Lote 6; os outros 7 ficaram pendentes (o PC caiu para 0,2 GB de RAM livre e o Chrome travou no export). Export não cobra: dá para baixar depois, com o saldo vencido | 6 373 | 0 | — |
| 16:25 | **Fim do Lote 6: 649 créditos. Total do dia: 8 222** | **6 373** | 649 | 4 878 + 1 869 + 826 + 649 |
| 16:33 | 1 modelo HD de imagem única fora da lista do `ASSETS.md`: o 3D do emblema (`emblema_tripo_v1`), da sessão da logo | 6 318 | 55 | Lido no histórico de uso da conta, linha `-55` às 16:33:52. Total do dia: **8 277** |
| 16:55 | Painel de plano ainda mostrava **Max, "Expirar 2026-10-29"** (sem hora) e 6 318 créditos | 6 318 | 0 | Sessão do Lote 7 lê o saldo e prepara a lista (`ASSETS.md` §2.6) |
| 16:58 | 1 export FBX "Blender, 4K" (`terminal_aurora_n2`) | 6 318 | 0 | zip de 13,2 MiB guardado (§2.3) |
| ~17:03 | **A conta deslogou no Chrome** ("Registrar/Entrar", ativos vazios) | — | — | Parado, como manda a regra; o Vinicius entrou de novo às ~17:10 |
| ~17:10 | **Plano Max venceu: plano Gratuito, saldo 0.** 6 318 créditos perdidos | **0** | 6 318 (vencidos) | O Lote 7 (76 peças, ≈ 4 800 créditos) **não foi gerado**. A conta guarda os modelos, mas o **export no plano gratuito pede assinatura** (tela de upgrade: Starter R$ 10/mês, Pro R$ 44/mês); nada foi assinado nem comprado. Também aparece "limite de armazenamento atingido" (333 de 10) |
| 2026-10-30 ~08:35 | **Conta nova** ("vinicius", plano Max até 2026-11-29; +25 000 e +200 em 29/10, 17:10 e 17:24; os 200 vencem em 2026-11-01). Aval do Vinicius em 30/10: "eu quero que vc produza todas as imagens, e todos os materias, sem falta, esta autorizado o uso, sem problema" | **25 200** | 0 | A conta de 29/10 caiu (~17:10). Receita conferida no painel: H3.1, Ultra, textura 4K, PBR, remover iluminação, triângulos, 50 000 faces, 8K desligado, Gerar em Partes desligado, **Privado** (o painel abria em "Sharing Only" e 8K ligado, "Generate 65"; corrigido para "Generate 55"). Imagens: Nano Banana, 1:1, 2 por rodada, **0 crédito** (o saldo não mexeu em 82 rodadas) |
| 08:40–08:42 | Grupo A: as 6 peças presas na conta antiga, regeradas (§2.4) | 24 870 | 330 | 6 × 55 |
| 08:45–08:46 | 22 modelos HD do Lote 7: 3 terminais n3, 3 aeronaves, 16 personagens | 23 660 | 1 210 | 22 × 55 |
| 08:53–08:57 | 54 modelos HD do Lote 7 (Costa Azul, Aurora, incidentes, interior, extras) | 20 690 | 2 970 | 54 × 55 |
| 08:59–09:46 | 16 Auto Rig humanoide Mixamo nos personagens do Lote 7 | 20 370 | 320 | 16 × 20 |
| 08:40–10:10 | Exports HD do grupo A e do Lote 7 (82 peças; personagens "Mixamo, 4K, Export Skeleton"; objetos "Blender, 4K"), em 2 abas do Chrome; uma aba travou por falta de RAM e a fila foi refeita numa aba nova | 20 370 | 0 | export não cobra |
| 09:49–09:52 | 44 modelos HD do Lote 8 (Vale Verde rural, pátio e carga, interior extra, terreno, 6 personagens, cofre e recompensas) | 17 950 | 2 420 | 44 × 55 |
| 09:53–10:00 | 6 Auto Rig humanoide Mixamo nos personagens do Lote 8 | 17 830 | 120 | 6 × 20 |
| 10:16 | 8 modelos HD do Lote 9 (passageiros de variedade) | 17 390 | 440 | 8 × 55 |
| 10:20 | 14 modelos HD do Lote 10 (incidentes de equipamento, frente fria, Air Show, Holiday, fila e a garçonete) | 16 620 | 770 | 14 × 55 |
| 10:19–10:32 | 9 Auto Rig (8 do Lote 9 e a garçonete; o rig do `passageiro_hoodie` foi refeito porque a 1ª tentativa começou com o modelo ainda gerando e não cobrou) | 16 440 | 180 | 9 × 20 |
| 09:54–11:11 | Exports HD dos Lotes 8 a 10 (66 peças), em 2 abas do Chrome; as abas travaram duas vezes por falta de RAM e o grupo de abas caiu no fim, então a última fila (6 objetos) foi refeita numa aba nova | 16 440 | 0 | export não cobra |
| 11:11 | **Fim do dia 30/10: 8 760 créditos gastos** (4 830 do grupo A e Lote 7 + 3 930 dos Lotes 8 a 10); **saldo 16 440**, Max até 2026-11-29 | **16 440** | 8 760 | 25 200 − 8 760 |
| 12:35–12:40 | 2 rodadas de imagem (Nano Banana, 1:1, 2 opções): cabine de scanner de corpo inteiro e garagem de pátio (pedido do Vinicius) | 16 440 | 0 | imagem grátis; checagem prévia em `ASSETS.md` e `arte/tripo/` não achou peça equivalente |
| 12:36 | 1 modelo HD (4K, 50 000 faces, "Generate 55"): `cabine_scanner_corpo` | 16 385 | 55 | clique normal na interface, sem script (§4, Automação) |
| 12:42 | 1 modelo HD: `garagem_patio` | 16 330 | 55 | idem |
| 12:40–12:45 | 2 exports FBX "Blender, 4K" | 16 330 | 0 | export não cobra |
| 12:45 | **Fim do Lote 11: 110 créditos gastos; saldo 16 330**, Max até 2026-11-29 | **16 330** | 110 | 16 440 − 110 |

## 2. Registro por peça (Tripo)

Ferramenta: imagem no gerador do Tripo (Nano Banana 2) → modelo H3.1 → Auto Rig humanoide Mixamo (personagens) → export FBX. "Imagem" é a opção escolhida entre as 4 da rodada; "v2" marca as 7 peças refeitas com imagem mais rica. Os prompts estão no `ASSETS.md` §3 (sufixos) e, completos, no histórico de imagens da conta. Licença de todas as linhas: `tripo_pago` (§4). Visibilidade: **Privado**.

| Peça | Versão | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|---|
| passageiro_a | 1ª (2K, 8 000) | 11:37 | #3 | `99e7f483-f7e4-4797-9ad9-661a1da77eb8` | 45 + 20 rig | 14550 | `passageiro_a/passageiro_a.zip` | 20.4 MiB | `1bc3b6c8003a236fb273bc9c3e68cda8c2fbee77759cb8ab2a551342c0f0a25b` | `7be04a672facb350e12bc831374e31a2785bfdbcf6f4b7c2c1651dfd5400c620` |
| atendente | 1ª (2K, 8 000) | 11:41 | #3 | `6d849054-56be-4891-a34d-18b269373e95` | 45 + 20 rig | 14473 | `atendente/atendente.zip` | 18.9 MiB | `a7ef12dfabb8f076874e11f6c8a03300bf1ac761dd7d95cf25171240a3a93aae` | `931ba23ca076d2ae6e41b500b617f65e2e79f1df4be61897add8f7b9d005f4ab` |
| aviao_regional | 1ª (2K, 8 000) | 11:43 | #1 | `d60fecf5-ba77-427d-9b90-9de42e814a3e` | 45 | 14428 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| balcao_checkin | 1ª (2K, 8 000) | 11:45 | #2 | `d8d484b7-7e1f-43e6-80bf-02c10e5021de` | 45 | 14383 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| raio_x | 1ª (2K, 8 000) | 11:46 | #2 | `85bc812a-9788-49da-b84f-44a9f174a90e` | 45 | 14338 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| portao_embarque | 1ª (2K, 8 000) | 11:47 | #1 | `bc96a399-1a02-47a0-bec2-de18916e0510` | 45 | 14293 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| loja | 1ª (2K, 8 000) | 11:44 | #4 | `d2802469-def1-41e8-9359-be4afcd5ee52` | 45 | 14248 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| terminal_vale_verde | 1ª (2K, 8 000) | 11:44 | #2 | `7d871135-4ffc-4cf5-81e8-0989e4ba7276` | 45 | 14203 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| mala_rodinha | 1ª (2K, 8 000) | 11:44 | #2 | `5fe54ef2-d8c2-489c-95a3-dc8e01d99805` | 45 | 14158 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| passageira_b | 1ª (2K, 8 000) | 11:44 | #2 | `e33e629b-c8df-4bc8-bdae-1a2ab8c7122c` | 45 + 20 rig | 14113 | `passageira_b/passageira_b.zip` | 19.1 MiB | `c97899ca57cc1c7c62bccbba0ef37ba80fac091d5fe2190ba948aa817eafb765` | `1df539575c15bda70434ba13659cfce0523cbb010c7ae4f5e5b47e23b41e5989` |
| seguranca | 1ª (2K, 8 000) | 11:45 | #2 | `840f61ca-b27d-46e3-a0e3-a372ba719cd7` | 45 + 20 rig | 14068 | `seguranca/seguranca.zip` | 19.8 MiB | `5f28792bb2e5abc28e8d04c9fcb6cdfcf845039b00d4c9b1d0c9c22bc66a957a` | `46d63ea0cd57a88193ddd0cca832dbee3ca8db1a65510157a197067703151357` |
| bagageiro | 1ª (2K, 8 000) | 11:45 | #2 | `1a41d277-fc33-4cab-adbb-ad0581a84561` | 45 + 20 rig | 14023 | `bagageiro/bagageiro.zip` | 27.1 MiB | `61ab0fca2c52db03a826980417aa9f83c9e33bae92179e6b436fd044434f4c77` | `91a4892fdb83f4d65ebfcdbf8f905d032fca485d0adc6df032777d8cd36a7a5c` |
| vendedor | 1ª (2K, 8 000) | 11:45 | #2 | `1be7cb0e-f795-4e70-9d7f-2eb5d1590c9b` | 45 + 20 rig | 13978 | `vendedor/vendedor.zip` | 18.8 MiB | `4254e29f6f665c2553939d5390a4deff18e14df5fe2f12480247e3ab4dbf6a75` | `2e695449d1bfce5c6160b0c261a679611ae76744c586ff337a2af96c82096af3` |
| mecanico | 1ª (2K, 8 000) | 11:46 | #2 | `4e7f9bd6-ebe3-419d-8c73-b71708ac16a5` | 45 + 20 rig | 13933 | `mecanico/mecanico.zip` | 21.2 MiB | `54bce8177da24607c557b499bf777a4a70666421e8e9debe1e6cd1f5a1f3e392` | `a74c5c77ed822db4a313774a7d0e6186d8496ca1d72225efa8a07a8d3ac48bbd` |
| despachante | 1ª (2K, 8 000) | 11:46 | #4 | `1fc4f680-0f6a-47d2-8e93-c5ccceccaaf5` | 45 + 20 rig | 13888 | `despachante/despachante.zip` | 20.8 MiB | `7db60cd8b451d8730bf37175263fac8d7005805f383e60f5221bb97ff8f508d3` | `f2372d348254b16e795b257c1ee320d2d76ec8d600aae65c88d0927d55b6999f` |
| aviao_narrow | 1ª (2K, 8 000) | 11:46 | #1 | `7d037649-d76a-48fb-8c0c-a216f3291a57` | 45 | 13843 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| aviao_wide | 1ª (2K, 8 000) | 11:46 | #1 | `ea5e8461-3691-43eb-8bd5-839b2d9f019b` | 45 | 13798 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| esteira_bagagem | 1ª (2K, 8 000) | 11:46 | #2 | `93277763-394d-4ad2-a0cd-6019986ea397` | 45 | 13753 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| alimentacao | 1ª (2K, 8 000) | 11:47 | #2 | `215d3f25-b6a2-4bb9-8cd1-4fead6c8946d` | 45 | 13708 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| hangar_manutencao | 1ª (2K, 8 000) | 11:47 | #2 | `964de790-3641-484c-af27-e2bcb0bf8a39` | 45 | 13663 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| torre_controle | 1ª (2K, 8 000) | 11:47 | #4 | `bae9c1c0-4ff4-4559-9923-9e901ae48ded` | 45 | 13618 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| terminal_costa_azul | 1ª (2K, 8 000) | 11:47 | #2 | `eee56a04-35f5-4516-860d-c887935c74d6` | 45 | 13573 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| terminal_aurora | 1ª (2K, 8 000) | 11:47 | #2 | `f7842e33-bcd6-4a91-93dc-d0f489c04305` | 45 | 13528 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| rebocador | 1ª (2K, 8 000) | 11:47 | #2 | `68923af1-28dd-40bf-b575-667387c3d82f` | 45 | 13483 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| carrinho_bagagem | 1ª (2K, 8 000) | 11:48 | #2 | `26f70a3b-aa53-4cb7-9853-e6b228f2d871` | 45 | 13438 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| caminhao_combustivel | 1ª (2K, 8 000) | 11:49 | #1 | `02e2d4d1-348b-45f6-99fb-c38d8961da5f` | 45 | 13393 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| mochila | 1ª (2K, 8 000) | 11:49 | #1 | `9036ff40-f5ea-4a70-a8fd-a980a772e127` | 45 | 13348 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| radio | 1ª (2K, 8 000) | 11:49 | #1 | `c14139ab-5619-43ec-a611-5b8f7053e33e` | 45 | 13303 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| scanner_mao | 1ª (2K, 8 000) | 11:49 | #4 | `df814016-cff8-4a4b-bcc9-73cbd267f4f5` | 45 | 13258 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| passageiro_idoso | 1ª (2K, 8 000) | 11:49 | #3 | `d6fc641a-6b60-4fd8-b429-b2d5c28ccdbd` | 45 + 20 rig | 13213 | `passageiro_idoso/passageiro_idoso.zip` | 21.4 MiB | `8f6de0701db7ecd6a72c5480be802b06c72f39f7a715603f72f53521e088c2ff` | `78db7caffbfca587c4c2df1a4b7697084669e70e529b6a70e95df73ab8064df1` |
| crianca | 1ª (2K, 8 000) | 11:49 | #3 | `308f86f3-8da2-4c71-92c2-c7d46d8993e5` | 45 + 20 rig | 13168 | `crianca/crianca.zip` | 19.3 MiB | `ce16c9296d90c3d5929851126c7c9ba9f67d9a17bf7f980d607f9ea70ac8754f` | `20dd02b8640fa14b2a3aef17bfb310e9358a4c9d9ccd5ec77d28772e3926c996` |
| lara | 1ª (2K, 8 000) | 11:49 | #3 | `37912676-e703-42a7-aa2d-e89a647b7a02` | 45 + 20 rig | 13123 | `lara/lara.zip` | 18.4 MiB | `f9828c95c95aa1df3a718a840ad75d5063486b3373757ce613f1588b842ea9ac` | `b0c27c15029377ad77dc873e3a1284ebecea774df5f5be12cfa0f311e8688dbc` |
| piloto | 1ª (2K, 8 000) | 11:50 | #3 | `be78f87b-355b-4a55-a6f0-a888c0734ea1` | 45 + 20 rig | 13078 | `piloto/piloto.zip` | 18.9 MiB | `cea089961615ee7ef3df86dfa094228cd5e8d65caa9a5cce57f245929cbb61c5` | `a0694a0246ac05f11c2728d0faa76e4cada1f0ca854c01b1e331f0be6f13b95e` |
| aviao_turistico | 1ª (2K, 8 000) | 11:50 | #2 | `5a8d3c3f-e64b-4e90-b8af-cc8ef30d9096` | 45 | 13033 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| aviao_cargo | 1ª (2K, 8 000) | 11:50 | #2 | `f62dac35-a45e-47d9-bf8f-83e7564e7dd9` | 45 | 12988 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| escada_embarque | 1ª (2K, 8 000) | 11:50 | #3 | `eac9b130-5c34-4f6b-b8a3-20de55de11ae` | 45 | 12943 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| onibus_patio | 1ª (2K, 8 000) | 11:50 | #2 | `293e21d9-485c-4ce8-9389-4ebe83b76deb` | 45 | 12898 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| ponte_embarque | 1ª (2K, 8 000) | 11:50 | #1 | `0e36a7be-d6a3-4214-969c-8d9e7a28a0af` | 45 | 12853 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| cadeiras_espera | 1ª (2K, 8 000) | 11:50 | #2 | `986c0c7a-ca8f-4414-9479-4a7361901e97` | 45 | 12808 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| vaso_planta | 1ª (2K, 8 000) | 11:51 | #1 | `546fda10-c9b3-48e2-8f17-a44c238c0f74` | 45 | 12763 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| bastoes_sinalizacao | 1ª (2K, 8 000) | 11:51 | #1 | `21039dc8-c4ba-4c35-84d3-41ab53c624f6` | 45 | 12718 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| caixa_ferramentas | 1ª (2K, 8 000) | 11:51 | #2 | `dfb1a5a4-2f6c-46d5-819c-6e457dc68b50` | 45 | 12673 | não exportado (fica no Tripo; a HD é a canônica) | — | — | — |
| passageiro_a | HD (4K, 50 000) | 12:22 | #3 | `ba823a7f-72bc-4bd6-83d4-0d21a5b85bca` | 55 + 20 rig | 12212 | `passageiro_a_hd/passageiro_a_hd.zip` | 62.7 MiB | `8b60a0ccbf829edad3622a0f851943082cac19f5950a64f84c8e4d5811daa330` | `ede2b8aadc880c975ee425a1098cb539656c21bef682b509c4440ac5daf6c97d` |
| atendente | HD (4K, 50 000) | 12:22 | #3 | `6e957a20-e32c-4e53-87f2-0616eae4cdad` | 55 + 20 rig | 12157 | `atendente_hd/atendente_hd.zip` | 62.0 MiB | `cab1dfdc796c8bb16e9bc079a4f7d04884573c501bac84d6f30a4d7b689db5bb` | `f33e566db2e71f6540ff585ee36c90c677c6c7b0addf3697be0cc7c86544688b` |
| passageira_b | HD (4K, 50 000) | 12:23 | #2 | `989310d6-e66c-4f01-b011-a53af4be0c18` | 55 + 20 rig | 12102 | `passageira_b_hd/passageira_b_hd.zip` | 60.4 MiB | `981a2ac0ecaf0e4cba27dfbe42b93626026d969b9c8790d0469f0abf1b346c5b` | `d773f3544f202fb9a85e16e1137adecf3fd60ec7c0df592dbb0f256f20a92a10` |
| seguranca | HD (4K, 50 000) | 12:23 | #2 | `9d12fe12-0da9-4644-9d17-9f3191e038a3` | 55 + 20 rig | 12047 | `seguranca_hd/seguranca_hd.zip` | 64.5 MiB | `b82d3dd63a4ac173a9e63b772d7e9591a82696f7a9a075cd1607ffec81952d41` | `c266c8e4b316359404fe9c7d7f56d0a6b1d7c56fa5bb0e492e0818b9a2062ccf` |
| bagageiro | HD (4K, 50 000) | 12:23 | #2 | `84d56c8e-50fc-4d14-8a27-e2fde203b3a8` | 55 + 20 rig | 11992 | `bagageiro_hd/bagageiro_hd.zip` | 80.7 MiB | `2abb2c2e20dc8aeeb47c8517f1555b8022811e9ff3ca304c6da96c9585dff997` | `cb06595f7fa2c42bac80620ac691f951b858ae31555d27808ca8d726a81ac12e` |
| vendedor | HD (4K, 50 000) | 12:23 | #2 | `28e7a208-7b8d-47a2-86d0-477c7bedefe6` | 55 + 20 rig | 11937 | `vendedor_hd/vendedor_hd.zip` | 59.6 MiB | `8cb6d38ea9861cd87e7e243006d2a30ae76a35ecefe78525e3621064acb89331` | `83a0fbd6824712d19315d94b459d1ddaee9c5012e9f3270bad7be5eafaa6e048` |
| mecanico | HD (4K, 50 000) | 12:24 | #2 | `143327aa-87ba-4971-8d3d-0ff708605cf5` | 55 + 20 rig | 11882 | `mecanico_hd/mecanico_hd.zip` | 67.1 MiB | `f7bfad7f617c25d3f6918c15b6d470c0a88be91bfc468628124b0b0f05c4a3e5` | `3341d07c96cedb3204cc97b77a1dd546d2540af79708cc840a7a722dd4384835` |
| despachante | HD (4K, 50 000) | 12:24 | #4 | `0807e1bb-330d-4bfe-8ce4-619967ffc8b3` | 55 + 20 rig | 11827 | `despachante_hd/despachante_hd.zip` | 65.2 MiB | `60a7636894c0128b54133215007b99f20a06498803bd5bb90a6a3d03ca37bf08` | `0c9b69e6187c62ac4632db0baedb41466fb2ca0e51dd9660458d88b244d70aff` |
| passageiro_idoso | HD (4K, 50 000) | 12:24 | #3 | `cd91e3f8-48b5-413e-95db-a021dd7857ba` | 55 + 20 rig | 11772 | `passageiro_idoso_hd/passageiro_idoso_hd.zip` | 76.5 MiB | `ccc8071b42f262f5e7809d1285e65e173d84e03f719f683f3540fffccae17bd1` | `30946dbce1e53b4966d63586c458848befe5912688d8f61b6fcc80150bc8c430` |
| crianca | HD (4K, 50 000) | 12:24 | #3 | `c83c4ed4-8967-4905-8fea-28eafecf407f` | 55 + 20 rig | 11717 | `crianca_hd/crianca_hd.zip` | 60.7 MiB | `3d333f9a9be90e7d153dcef178bc5630b85ce537e0c2472bfa8947aaf135e7fb` | `86d238c177b4a084eec1e193d6e46d925d1e008bb93603a0e8011e2b0b3371e7` |
| lara | HD (4K, 50 000) | 12:24 | #3 | `6b329152-1d75-4709-9475-e1711e1b6afd` | 55 + 20 rig | 11662 | `lara_hd/lara_hd.zip` | 57.2 MiB | `2b7222ced3c6b3ab6e01b63099dbf0fe8ca15bb1ab88f3cc5a2b75bbfa35ee5c` | `12ed12e5aa1888e3eafe54d68ec889bcd964c3009eca2e2bbaddd8eb9b484edb` |
| piloto | HD (4K, 50 000) | 12:24 | #3 | `6014c836-83a7-4a8c-acf4-f0f2464e4475` | 55 + 20 rig | 11607 | `piloto_hd/piloto_hd.zip` | 58.2 MiB | `f20a2623cbe290f753983c9fb16dffb26e5bc9b7ae609c9ce79a0993181e66b6` | `efb582f72f2d604e7a16277c2769ae5688ba4b08fab9f0ba97781631e68fc869` |
| raio_x | HD (4K, 50 000) | 12:25 | #v2 1 | `887a38ec-f290-4b8d-92c6-8ce670ba1a98` | 55 | 11552 | `raio_x_hd/raio_x_hd.zip` | 10.6 MiB | `68daca953bafe3c22cb9df601cfbbb8b137a6fa6d20ed6693dadf6a2c246c211` | `8ff34b7d221fcbe138bec849c9f5877b54088d19e2715bac9bf03ec2ddf03638` |
| balcao_checkin | HD (4K, 50 000) | 12:25 | #v2 1 | `91910b2d-9d3a-48ac-ac83-eb813bf99082` | 55 | 11497 | `balcao_checkin_hd/balcao_checkin_hd.zip` | 9.9 MiB | `286438d499ddf5be81870d1e126181fdec09b4d4249857f17f864cf7548defc0` | `2dfa73a70d2eb588d30e1d39e33d55625210a451d5a62318062deb34ea79735e` |
| terminal_aurora | HD (4K, 50 000) | 12:25 | #v2 1 | `389db93b-7dda-4389-9351-e632b45abd75` | 55 | 11442 | `terminal_aurora_hd/terminal_aurora_hd.zip` | 12.8 MiB | `c9699b2a2c057ce785fba95e0f1f49d51cb4bdad454910d43b8e11821864ac49` | `bd97658ff290193059ccbd52a1d5a918801221dc7da682b6c3a5b6bd1cfd343b` |
| bastoes_sinalizacao | HD (4K, 50 000) | 12:25 | #v2 1 | `21878e2b-1019-4106-8f0a-2ad75f88b28d` | 55 | 11387 | `bastoes_sinalizacao_hd/bastoes_sinalizacao_hd.zip` | 7.4 MiB | `7f8493eee2143bd113486b31dfb21ec3845428eae2457aed489ab4961a633095` | `0fea5a25b99dfb34710ebb0fd4520e630ca505b1719ff5cb9b6b48f57e61e3fc` |
| cadeiras_espera | HD (4K, 50 000) | 12:25 | #v2 1 | `8d6a4f49-80d7-415f-8240-1564b7928498` | 55 | 11332 | `cadeiras_espera_hd/cadeiras_espera_hd.zip` | 10.5 MiB | `db27f3802e84a790e3fdf7638872c5e652e8c8e4a163f9703a5d26a605479a34` | `1a694b7a2df471cfd6ded70e1fda56ab05a927bd246b38454b9f6e4281694402` |
| esteira_bagagem | HD (4K, 50 000) | 12:25 | #v2 1 | `a6b363c9-b97f-4ae9-af69-f4c6070cf071` | 55 | 11277 | `esteira_bagagem_hd/esteira_bagagem_hd.zip` | 10.0 MiB | `a30abab2dcf9b51c81b98186b17801124a9a212da95cea2a88f16de285789d5a` | `2c3021a0ddb3ce1039d3e008bf8f0c8cbe5bfbe948c57dbc54bb7df38328bff3` |
| portao_embarque | HD (4K, 50 000) | 12:25 | #v2 4 | `5ac859b7-1842-4a10-b146-2a8010e2b35d` | 55 | 11222 | `portao_embarque_hd/portao_embarque_hd.zip` | 8.8 MiB | `891805022402bf5c59dd616b13b89fa1ba5f7f53e5aa9e09e6a5c0ea1e4e525f` | `58983b39bad1fb88038c1dd597378ca677f463ff1393f464a3df7344e774302d` |
| aviao_regional | HD (4K, 50 000) | 12:25 | #1 | `686b3647-d942-429f-9361-bf67bf59248f` | 55 | 11167 | `aviao_regional_hd/aviao_regional_hd.zip` | 8.2 MiB | `7dc243e70173c0be8a67b908fbf87fa46194b0f5ba498d3ee823689ab5258c6a` | `2997626652716008f93409d99cba47788c5cfa55ac22294fed6bbad5c11e1355` |
| loja | HD (4K, 50 000) | 12:26 | #4 | `9de05e81-7e5e-4326-835b-a8da42c2aa81` | 55 | 11112 | `loja_hd/loja_hd.zip` | 9.0 MiB | `f734628696ebaa45cbca11f3459ba4700af1e3d2a509e548e69bd13f31f866b4` | `9381133d6dd01237415b31d6a289a6f0f088223bce0358dffb36b1f4c7ee8ee5` |
| terminal_vale_verde | HD (4K, 50 000) | 12:26 | #2 | `f4d01a13-c1ad-4a17-a0c7-4e9e5ce02c48` | 55 | 11057 | `terminal_vale_verde_hd/terminal_vale_verde_hd.zip` | 9.0 MiB | `e8da437f3e10e9384d0c49eb576a841a72c436d8246965d809cd9564e608836b` | `3dd74a702c7fe9db84e7131bc22a64e316992adfedf72720a39b2b81195d3413` |
| mala_rodinha | HD (4K, 50 000) | 12:26 | #2 | `2e3d5e47-d82d-42a2-9403-9319d1938d72` | 55 | 11002 | `mala_rodinha_hd/mala_rodinha_hd.zip` | 9.4 MiB | `ba27a1d55fddbd95fb2cdb4c941937aa14113f1ce2a946d206518fa0dacec707` | `ae18662861ea8ed202d5c218786bea6c7334887c248eb8e90db808b25858ff97` |
| aviao_narrow | HD (4K, 50 000) | 12:26 | #1 | `7da13821-7633-460a-b317-233367c8a4a7` | 55 | 10947 | `aviao_narrow_hd/aviao_narrow_hd.zip` | 9.1 MiB | `deaa4a34f7ec0592cf2334b307367ca3325bc8732331da3123a54b435ebb865b` | `5977d26dfe165c219952b07f72c1376060bea330bb5de08e1e258a4eace3c7c0` |
| aviao_wide | HD (4K, 50 000) | 12:26 | #1 | `5c1a59d3-a435-4631-969c-5878d8f8b28f` | 55 | 10892 | `aviao_wide_hd/aviao_wide_hd.zip` | 8.6 MiB | `c0c5202adc8be2109188c0b3252bc12dd423106ad493715e350dabcd78e9a3a4` | `796e043f5ee5afa9f7a3f801cf6a289c24cb01a6de3aa918904426d17921aa4a` |
| alimentacao | HD (4K, 50 000) | 12:26 | #2 | `bc9f2672-da15-4ba7-b1da-aadfcaf363a0` | 55 | 10837 | `alimentacao_hd/alimentacao_hd.zip` | 8.8 MiB | `b9a403a5127b9027c24835a6d8e3d7cc2d2590c365a6d279cd89ec28b30dc9f6` | `e21f40927fc38e10630ec41ef77a8c84019f9534410eab57255d49ef32a8fd0d` |
| hangar_manutencao | HD (4K, 50 000) | 12:26 | #2 | `005f7fb7-95e5-44de-946f-fe0dbb247a73` | 55 | 10782 | `hangar_manutencao_hd/hangar_manutencao_hd.zip` | 11.1 MiB | `530ea1218d806cef17cf9fb472f525ad717c3af8a3eea102fd3971606eec2944` | `e0431c98d9779969117c2452674d07f3895d540bb5a81a4bd42854c943829c64` |
| torre_controle | HD (4K, 50 000) | 12:26 | #4 | `6fe4fbef-ed3c-417d-9276-dab51da61c95` | 55 | 10727 | `torre_controle_hd/torre_controle_hd.zip` | 8.8 MiB | `596dbc98effaf63e9f1a47b013e4ea47602ef02707924131966e7ac3a110e625` | `2d336f84ea4fdf92746100cbefae6e2629dc6bc32d96ab135c00851bbbaffcc6` |
| terminal_costa_azul | HD (4K, 50 000) | 12:27 | #2 | `c71e6f97-f655-417d-92b7-4e8671b8c517` | 55 | 10672 | `terminal_costa_azul_hd/terminal_costa_azul_hd.zip` | 11.6 MiB | `0c025ad050ed963e77c247f116f24546e9beee0595fdd25bc05cd8a8954a027c` | `ce5b9dada7aeb6a25505282a27abce19cceb3d5c9e2421dccf55496d467788ac` |
| rebocador | HD (4K, 50 000) | 12:27 | #2 | `d419a9ad-9cde-4e6d-9352-88c87f519041` | 55 | 10617 | `rebocador_hd/rebocador_hd.zip` | 8.8 MiB | `9acf5ee49d058afc596781472ebdc4bd5b070d804b34f52184e3eb889d6629e6` | `8335171e0663b9b805f386047e933b38632a9ca0b5d68814d58e0c38e99b74f6` |
| carrinho_bagagem | HD (4K, 50 000) | 12:27 | #2 | `26966e4a-eb3a-48c2-88e9-4b677d6aa7e8` | 55 | 10562 | `carrinho_bagagem_hd/carrinho_bagagem_hd.zip` | 10.7 MiB | `a63e5b73cc77d12bf6ab0f412791b4446b718b93fe1219b95ab072a06a1766f1` | `9ccd795e8e4b7215c1996f2b7fc554c21c41a4346643c24948f3e220bbc1496b` |
| caminhao_combustivel | HD (4K, 50 000) | 12:27 | #1 | `70acb161-190b-4178-91c8-20271e0109aa` | 55 | 10507 | `caminhao_combustivel_hd/caminhao_combustivel_hd.zip` | 9.0 MiB | `fe96979ba946c680aed0bd0cfc4af293d3e5010da2614495805d249ea5af1b20` | `9f659fefb883505cdd0fe642d4c6f241bd09d3ff593c2699d9040259da037b7e` |
| mochila | HD (4K, 50 000) | 12:28 | #1 | `9497c3ae-c85f-4083-bd3c-93ad75db0ed5` | 55 | 10452 | `mochila_hd/mochila_hd.zip` | 8.2 MiB | `96eb9207488f5ce19b874acc80f826fc4a39bcc347a9dd2d6adfda090763a649` | `619c8b12cc83fdcd8a3eed1a654511e0e8570792c303308af14573ed95975ab8` |
| radio | HD (4K, 50 000) | 12:28 | #1 | `637bf027-2754-4e7f-a3e7-25a52ef0dfb4` | 55 | 10397 | `radio_hd/radio_hd.zip` | 9.1 MiB | `690ec84d2419fedb00c0ed17104c2bc810eed26a28ce0f91bd9641de52e2b220` | `b0caceaf135942ab2885a112223e01fb441c57531e2fd47ea44b28a96b277fb0` |
| scanner_mao | HD (4K, 50 000) | 12:28 | #4 | `d8d421d4-e24d-48ea-9b19-0512b6d7b590` | 55 | 10342 | `scanner_mao_hd/scanner_mao_hd.zip` | 6.8 MiB | `61c6f50b17ef5d68ba6bbf364c727666a4c5ee44fddb07baea2d4f7af0e8ad4f` | `12e1479e4b5465da13ceb6651ddfaa9f7eac2846ff54bc934f6c19636d32b737` |
| aviao_turistico | HD (4K, 50 000) | 12:28 | #2 | `ac1db492-88d9-4671-bffa-7bf97ded747b` | 55 | 10287 | `aviao_turistico_hd/aviao_turistico_hd.zip` | 8.5 MiB | `49d14ddbb9da83c4302e2a332f65c57d5eb697dd647cd8ce63eb0a653859796f` | `509ccdc054f620f9207a9d1e9708b44ebbb2e9fd7239c6f24c5d01d76e1564fa` |
| aviao_cargo | HD (4K, 50 000) | 12:28 | #2 | `0a959aa4-bac0-44d7-b3c1-e3c7d6e14675` | 55 | 10232 | `aviao_cargo_hd/aviao_cargo_hd.zip` | 7.5 MiB | `d517b5e93bacb7ee0aed68cf3127452112a64bf1a2f1467fc7e4ace5fe0525f6` | `15c1c43d98d31a5c7f53df6db34351f26baf2d3330f3d2e86ef6f65f013fb99b` |
| escada_embarque | HD (4K, 50 000) | 12:28 | #3 | `cca12ad5-cbbe-4845-a4b9-234840155f4a` | 55 | 10177 | `escada_embarque_hd/escada_embarque_hd.zip` | 9.1 MiB | `0bde2f57f28123b506b001be5819e574d7a08c2d0cf3cbfb3f3f8b944cae8e60` | `7cafed239039fb77425c460f895609ca9a3528c13601588527ab4951ef72696e` |
| onibus_patio | HD (4K, 50 000) | 12:28 | #2 | `1c12fd6b-af00-45d4-80d1-d20deee0c15d` | 55 | 10122 | `onibus_patio_hd/onibus_patio_hd.zip` | 9.5 MiB | `d565bfe27a1abbb2050b88a2d2f2aec147f6108ca8908aa9e48f40e3e05a1117` | `286be54a4edf9e741d913373099f4e4e697a132d18c7aa8d7e92a391b7be5db8` |
| ponte_embarque | HD (4K, 50 000) | 12:28 | #1 | `4fd9592c-ecaf-4f84-8328-320e705df821` | 55 | 10067 | `ponte_embarque_hd/ponte_embarque_hd.zip` | 9.3 MiB | `c27191d6192cdff544521cb9e70ee25776d4c0b247b5d781cde073faa61739bb` | `b097b86927873fba92c61e5b0d5baf87e854fcb62a6f5df3c6c10e5719ee2bd3` |
| vaso_planta | HD (4K, 50 000) | 12:29 | #1 | `ffe74a22-76b6-4d81-8a3b-e454b3ea14f8` | 55 | 10012 | `vaso_planta_hd/vaso_planta_hd.zip` | 7.6 MiB | `e13cb164849a5eac86ce323443c35e98acd74e97213020ed7c91b9948bcf09ad` | `ae22a20a50fd89a0dd4fe4a53d01f00ef807e3d3f9edf94f80db3b7c9000f683` |
| caixa_ferramentas | HD (4K, 50 000) | 12:29 | #2 | `5c5c102e-5144-413c-a1da-6cf25818e389` | 55 | 9957 | `caixa_ferramentas_hd/caixa_ferramentas_hd.zip` | 7.2 MiB | `5220ec9d97546b03e38e024aa8b1c3a48d04f57d26f5ce39710fcb75a50b2f76` | `f5256e8f17041348ec43cf7a0f72e9cf32a914be3c3d36f0688da3350e922ca8` |

Conferência visual feita no visualizador do Tripo antes do rig: os 12 personagens em pose T, de mãos vazias, sem mala nem mochila. Os aviões saíram brancos, sem logotipo, sem texto e sem pintura de companhia (D-13). Peças da 1ª versão sem presença, refeitas na HD com imagem nova: `bastoes_sinalizacao` (2 cápsulas), `cadeiras_espera` (1 assento), `esteira_bagagem` (disco cinza liso), `portao_embarque` (cinza), `balcao_checkin`, `raio_x` e `terminal_aurora`.

### 2.1 Lotes 3 e 4 (2026-10-29, à tarde)

Só HD (4K, 50 000 faces). Mesma receita e mesmos sufixos de prompt do `ASSETS.md` §3; objetos com o sufixo "premium" da rodada HD. Os personagens saíram em pose T, de mãos vazias; a comissária e o comissário com uniforme verde-petróleo de cor chapada, para a máscara de uniforme. Licença `tripo_pago`, Privado.

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| comissaria | 14:21 | #2 | `9807aecc-3a6f-4184-9b8d-ae2f8ef9d124` | 8 img + 55 + 20 rig | 9446 | `comissaria_hd/comissaria_hd.zip` | 8.3 MiB | `a39ac9587fcc3853d12531e4096cc4ed5a8cece504dd75521c3208e89a418215` | `953f847a3efd62750c5962def8fe792e26dc761c6cf1f316f753812451f8cfc7` |
| comissario | 14:21 | #2 | `75d0c6ae-76d9-48d7-ad46-10c8abeeaf88` | 8 img + 55 + 20 rig | 9391 | `comissario_hd/comissario_hd.zip` | 8.5 MiB | `a58dba9b5e285bb2f0d83d19a8e0c664ee97b561cdeaed20c2efeda8417ccfad` | `c4ae76b2ad2294f3bc630d5da4e099796a16e3f9bd5624768be4c59855ad1e78` |
| executivo | 14:21 | #3 | `2d83cc8b-973b-446b-9d27-512cd35f4388` | 8 img + 55 + 20 rig | 9336 | `executivo_hd/executivo_hd.zip` | 9.2 MiB | `e802623e4f515dc07c73247432f188e89cf52001baf0f9454ecf220dca23fe7a` | `b5b09e918885273af6b8e1d25e0a61b2ac4bef5cbe5beaa4a6afd07ddb596915` |
| turista | 14:21 | #3 | `9aade010-0dd2-4b6c-9061-b694f7441bfd` | 8 img + 55 + 20 rig | 9281 | `turista_hd/turista_hd.zip` | 12.4 MiB | `b5036bdb2dadadf4afd400466c6e9724c4c2a45db137f3fcdd0453875b71c316` | `446cd610c3d46e001d074177a5333d3e2d5d3ee1628ef2e61747ef9bc4b20168` |
| mochileiro | 14:21 | #3 | `3816d249-079f-48ea-9dc4-652a92dcd7d9` | 8 img + 55 + 20 rig | 9226 | `mochileiro_hd/mochileiro_hd.zip` | 9.6 MiB | `6db402ab1047b934e161781f2b960d458439b08bf88c61808b706b4b05e44eac` | `b87cf9d853905d9b128b8179b711800584069c60d372244670adfdc9924b61fa` |
| mae | 14:21 | #1 | `80b06fb6-10cf-4c98-9364-9b094d62e5e8` | 8 img + 55 + 20 rig | 9171 | `mae_hd/mae_hd.zip` | 9.9 MiB | `901a439b76523f39f08b5b8600a72907033593d3035fa29340b9b66fda29f542` | `c96e0ca6ce9147b88da0a9360d9334616fc8d5e995007d7caedc45abbda2fb1a` |
| faxineiro | 14:21 | #3 | `af9ae9c4-65df-4b65-8198-2ed7d1528127` | 8 img + 55 + 20 rig | 9116 | `faxineiro_hd/faxineiro_hd.zip` | 9.2 MiB | `4e65941634a67a7a1dffc8d4f187280300996b9b33144297256545ebbe31e75e` | `66a0df45f876a09d28c3da5299d8cdf64ee36f167c78deb1c96c2bd8ab8a010a` |
| carrinho_limpeza | 14:21 | #2 | `bea688fb-9c71-49e7-b167-e71ed82428d5` | 8 img + 55 | 9061 | `carrinho_limpeza_hd/carrinho_limpeza_hd.zip` | 8.7 MiB | `d25ebaa809a894625d73274f2788525d814be1fff210255e66b92dd1cb48fe35` | `d0175058ec66701203903d4aab73626f98471a48c2913324c1d67f4828067cb4` |
| caminhao_catering | 14:21 | #4 | `081502f7-190f-462a-aeda-3648fc62fc69` | 8 img + 55 | 9006 | `caminhao_catering_hd/caminhao_catering_hd.zip` | 8.3 MiB | `130b0a9594615e0e68d7e186ee0d7a1b8a95dfe6e2816aabde1a41bac1fb8a03` | `17eae8a48cb59d71973b29d61b343e4d7c16e9d796ad102ca38e6c1c300e6315` |
| posto_combustivel | 14:21 | #4 | `02e2da90-be4b-4921-a206-d22020665c96` | 8 img + 55 | 8951 | `posto_combustivel_hd/posto_combustivel_hd.zip` | 9.0 MiB | `1d097b397e7e8914974106083e879603935729a96f22db0eef1f6272861d58af` | `1c0c6a8c61698a3e468932d0d2bae27f91a7f86c91b2d1046630f948fa106c9c` |
| terminal_carga | 14:21 | #3 | `f9cda329-9220-4515-82f4-3bf44769348d` | 8 img + 55 | 8896 | `terminal_carga_hd/terminal_carga_hd.zip` | 12.8 MiB | `be8cabcc812189987f3a9addfe0d4f7cf7e10f6d944702739e351d48aa7e9f1c` | `c70dd123b304c854f87904c375a6f3906bea505a70b3a2526c0f0dc642058b86` |
| cone | 14:21 | #2 | `0300731f-8788-4381-9817-cf78b626ffda` | 8 img + 55 | 8841 | `cone_hd/cone_hd.zip` | 6.0 MiB | `844ba82f4771597223e6580d9e55281b42f8257754aaec0acc9c723a23e3c911` | `011159d4631224774ea82650b840092ba82514a99cb03178251af395333a421d` |
| cerca | 14:21 | #1 | `3bfd96e7-9ab2-40ce-8461-0aedbff8e68b` | 8 img + 55 | 8786 | `cerca_hd/cerca_hd.zip` | 11.5 MiB | `535ac25b9abab038ed8c9b0f4ccf90a203bd6668175d51863a87122668beedc3` | `623edbf4b101a63306bff89f8513fdc55dab2b23235ab78de9d8b9af4cbf74b6` |
| arvore | 14:22 | #2 | `bc81e965-110f-407c-8c79-fe11c7cb0669` | 8 img + 55 | 8731 | `arvore_hd/arvore_hd.zip` | 10.4 MiB | `fcbca248c137db70003b259e9c8adb5279f979c293c95bf545129eda83cf8bb4` | `497d3bf43a100efcde21045e5b9fb89e6090bada620d5f36e5b037ad5ca6cb4c` |
| palmeira | 14:22 | #2 | `f3de1d40-19e7-461e-b900-353c5e1b8012` | 8 img + 55 | 8676 | `palmeira_hd/palmeira_hd.zip` | 10.1 MiB | `ba48efd0c8fdd996f4de6b7300f25ff7110d16b8df70aa0bd7470bf3fa56895a` | `a36b900af1df8a6f59208a3950bedcc9ba86f5d53d4ca937728a3c515142a347` |
| poste | 14:22 | #4 | `93cde14b-1a0f-43f3-935d-31b23110d486` | 8 img + 55 | 8621 | `poste_hd/poste_hd.zip` | 7.9 MiB | `9268c081dd7294f34be130cb45780b021c99d9b2b1fee1a98d78e5d057587e0e` | `8ec429e4063b3051ae815a988e9c867c438d3fa804b797b1512e7329d853a5a4` |
| placa_direcao | 14:22 | #2 | `711e5b93-21f3-420e-9496-3b8f589b7690` | 8 img + 55 | 8566 | `placa_direcao_hd/placa_direcao_hd.zip` | 8.6 MiB | `cc894537e5a7f8881e0b8f22638627d1f1e8d27f20a388201149c9839c2b7599` | `ef74bcf886481c8d5c52d3084dff564c5a209658be288412c2f0019b1bcc1809` |
| banco | 14:22 | #1 | `288b31fb-e610-4321-bd19-59bfabf8a641` | 8 img + 55 | 8511 | `banco_hd/banco_hd.zip` | 10.5 MiB | `705e34ab23c1203006fed750efba3092abbcf7eee58451df97916d01ae3eded9` | `8433b063ee75304af9922a782c96997936a65cb351c3fc07bd349817b596642b` |
| carrinho_mao | 14:22 | #4 | `3142de88-08a3-4030-b680-436e5e8c2636` | 8 img + 55 | 8456 | `carrinho_mao_hd/carrinho_mao_hd.zip` | 11.5 MiB | `6d6a31ff93f03624c88d76fc4c18761b8d334b7ff9632882d877f91830e151a2` | `8258fad0f2fe792af482e77421a5ec341287393bc056392d850eaadff359813f` |
| ala_terminal | 14:22 | #1 | `4e84958e-0c2e-4ba2-9a86-5ceca8f20215` | 8 img + 55 | 8401 | `ala_terminal_hd/ala_terminal_hd.zip` | 13.6 MiB | `d581de9618d9816c3ce1f8bc182687a92d3b4d018732197415249940ca6573bb` | `4665f6dfdae400afe23cb8c31c001ca95ecb1f3c78b0346ef0c9324aaeac55c6` |
| sala_tripulacao | 14:22 | #1 | `331a814b-cd32-474b-96be-b3cdcce9c49a` | 8 img + 55 | 8346 | `sala_tripulacao_hd/sala_tripulacao_hd.zip` | 10.3 MiB | `1041975b722e37bacd738ee64e37ef6ec1556e374c7e8b24a4de1a09d24bd9ac` | `81e74e182fd0ea020c20fd4f6eef61073743f6b68d7d426ee0760b5a4b35923d` |
| oficina_mecanico | 14:22 | #1 | `494ec8bf-2a5b-4d23-bc51-b29a4afde1ed` | 8 img + 55 | 8291 | `oficina_mecanico_hd/oficina_mecanico_hd.zip` | 10.8 MiB | `7e5f2c96c2c0dcd06b05961818e08b0c98726a65452209e33a1688babbb0182b` | `436b3c38a605a32c6313c5518924651ed34e6f8e901f57ae961fe74e39d8a69f` |
| caminhao_bombeiro | 14:22 | #1 | `cf8eeeb1-edf1-4e73-a07a-90d5ce95363b` | 8 img + 55 | 8236 | `caminhao_bombeiro_hd/caminhao_bombeiro_hd.zip` | 11.9 MiB | `7512b5534432d2d4a338f89b529224f91b6af7e576e68ac99cf503eab5954ad5` | `a5ef8f528bb0f8eae4bce3a9a88d3eb1c27acaeb7619abb740ef55070115ce57` |
| carro_follow_me | 14:22 | #2 | `2e552be9-0574-45da-8bc0-c160a27519c6` | 8 img + 55 | 8181 | `carro_follow_me_hd/carro_follow_me_hd.zip` | 9.4 MiB | `ca61c8d746c50ca881ccaabd125d3800d0c68a7df40c0cbf49a3bd94bba5c747` | `d05cb1cdbae6a37a79828fdc8a35d3c68429c8b2299a1e674daa548a7af22fbe` |
| conteiner_bagagem | 14:22 | #2 | `84d1b884-f7db-443d-86a4-5d67c3df4598` | 8 img + 55 | 8126 | `conteiner_bagagem_hd/conteiner_bagagem_hd.zip` | 13.3 MiB | `71297d24b04946e1845eacce25094ed2132a07fb22baffbe2efefe31c5a36bb1` | `f9be4ab3654b18c50ea2dca67340ea40176e7c43d68a715b888e438dcf664420` |
| biruta | 14:22 | #1 | `0286d656-8fc8-449d-b1a6-422df100ad01` | 8 img + 55 | 8071 | `biruta_hd/biruta_hd.zip` | 9.2 MiB | `17c308c896545f280a4ba6ba3f040334e859ba70d2e73c69f8f67d7d238b381c` | `e892b8fe9ef569e3ae4d45b9c4776c3bfb9faca614a0df53982a047a1f65be7a` |
| radar | 14:23 | #3 | `e6652446-32cd-481a-a5dc-439524994c24` | 8 img + 55 | 8016 | `radar_hd/radar_hd.zip` | 10.3 MiB | `babb326730501f4e923c216242326f743889e160e1f5d1c2d0aef0071b15973a` | `2ba95b8aa86333f173a71dc796698b20e7221b52cd567588bcb9cbec8b481ea6` |

### 2.2 Lote 5 (2026-10-29, fim da tarde)

Só HD (4K, 50 000 faces), objetos em FBX "Blender 4K". Imagem escolhida entre 2 opções. Licença `tripo_pago`, Privado.

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| balcao_checkin_n2 | 15:18 | #1 de 2 | `40508539-0b10-4163-acf8-b7ca36ac2402` | 4 img + 55 | 7737 | `balcao_checkin_n2_hd/balcao_checkin_n2_hd.zip` | 10.9 MiB | `2909befae91e98cf40941f2144979ae378795e1a955bbde1610f13e560ae7d67` | `9e90f625f32e95d199d612f2776ebbdab631f734cbb7cab264f3b23c53b183cf` |
| raio_x_n2 | 15:18 | #1 de 2 | `829d6880-36fc-4258-93bb-562091d55e79` | 4 img + 55 | 7682 | `raio_x_n2_hd/raio_x_n2_hd.zip` | 12.3 MiB | `d006c8312249ceeed35aba84e1bd89b7a4e361807e2a19aeb5f3d64bc49fe6df` | `fefd67b1692a310f62fd2ca8f19af45c6c2f1c19badea9debc3fa2f68e31ecbb` |
| esteira_bagagem_n2 | 15:18 | #1 de 2 | `ac6ae58f-f573-4884-bc3e-24c0ab4eb695` | 4 img + 55 | 7627 | `esteira_bagagem_n2_hd/esteira_bagagem_n2_hd.zip` | 13.0 MiB | `6160cc6c92af1705b13b26a80910247b5eb2b8c7b3f826ceafa832922eeac481` | `b8858aa1fd4054b54d91fc1a3742637833dc7b2f59c3ed9ef43c9aecfdff5ecc` |
| portao_embarque_n2 | 15:18 | #2 de 2 | `6a23ad62-7c11-4466-8d91-2a3f49d5439d` | 4 img + 55 | 7572 | `portao_embarque_n2_hd/portao_embarque_n2_hd.zip` | 9.1 MiB | `db2c554f4603adfd29ccdbbb16b468666f024a4f1c0982635b453ad7365958ac` | `4760e289d4afa682184176b19fa4e0151b218212296fd91a6c1ba0ba7d4c9f06` |
| loja_n2 | 15:18 | #2 de 2 | `537bfb29-d3a7-47db-b778-b907674c1074` | 4 img + 55 | 7517 | `loja_n2_hd/loja_n2_hd.zip` | 9.4 MiB | `37b3160cd76d2780e9793401ed029e5f1024c5356655fc0efa45df3d6f2a91ab` | `48054aec915ca36527ef33a7c0170a43228c3d392db6230c338a30cd56662869` |
| alimentacao_n2 | 15:19 | #1 de 2 | `d3508372-dd38-41ca-bb54-7560a4e93501` | 4 img + 55 | 7462 | `alimentacao_n2_hd/alimentacao_n2_hd.zip` | 10.3 MiB | `c385be27c4af8a9b05cad28f596ea8a765797695813a7e5d53fdeb3955bdc3cc` | `94970af2a8c946811911b4d50bcef4123505527e7696bc87132d9bdca0488778` |
| hangar_manutencao_n2 | 15:19 | #2 de 2 | `6c89bd01-19ae-4a36-9848-a3d8659e60a8` | 4 img + 55 | 7407 | `hangar_manutencao_n2_hd/hangar_manutencao_n2_hd.zip` | 14.6 MiB | `c9fa6e3c3136f77393b9598332546ebada0a8e4b722a71a943c54de8a745932b` | `8d2373e420049febbf1618c7142ae3e6928d052debf4dd9245bf0c865de39f33` |
| torre_controle_n2 | 15:19 | #2 de 2 | `a33f5d4c-e558-4733-b71d-39c6a9eb576f` | 4 img + 55 | 7352 | `torre_controle_n2_hd/torre_controle_n2_hd.zip` | 11.7 MiB | `e5c4f32fafa57115139093bbfddb3110277b2c5bfbaa104ff7f7b7372af3d1d7` | `b456055a32d452a5db55453af41f32c805688623c05b176aa57310d225fad33e` |
| helicoptero | 15:19 | #2 de 2 | `1536fde7-4eaf-4436-a97b-70e6eeaee94f` | 4 img + 55 | 7297 | `helicoptero_hd/helicoptero_hd.zip` | 8.8 MiB | `dcd85014821c94f9746131f192fed6a69f2b1a97d1715347f3ba72209083aeef` | `7c5c0426e1c3f07c891416e5a4f49d5396c00f41aea6860f358253036619a38f` |
| jato_executivo | 15:19 | #1 de 2 | `851e2c5c-b7e9-4e09-894c-96d4c095d6fd` | 4 img + 55 | 7242 | `jato_executivo_hd/jato_executivo_hd.zip` | 8.6 MiB | `3a1ca6e9f2dc76d249065a1bc09ab4467ba1535ad53b491f879b40195f044c9b` | `4db1b981aeffcd430c4f16205a10b3819ebf52d55db3e2b97d054966a90c0682` |
| hidroaviao | 15:19 | #1 de 2 | `4b55c279-a7dd-4072-a31d-beebbca43bca` | 4 img + 55 | 7187 | `hidroaviao_hd/hidroaviao_hd.zip` | 8.7 MiB | `489207d86db3ed51f921ee241dd67699eb31931c6205d15fb4a075c85b6959f7` | `7c37701ddf5cb72b9b178109f785263539f4a61cce6f75aab42365204378accc` |
| guindaste_obra | 15:19 | #2 de 2 | `78d6cc83-9c79-4d6b-aee2-ee1d79699fc1` | 4 img + 55 | 7132 | `guindaste_obra_hd/guindaste_obra_hd.zip` | 10.7 MiB | `0feec43ab98436924f649a719a2116cecda44ce557811301749e2dea6b4afe97` | `8821e52ba1629f0b99e6428f96d275f5a65723dcd8e34b89aba3b29e9ad350ee` |
| barreira_obra | 15:19 | #2 de 2 | `5825b80a-8ad1-44c1-abd3-a85213ab98f3` | 4 img + 55 | 7077 | `barreira_obra_hd/barreira_obra_hd.zip` | 8.2 MiB | `6ae915f87e670d3ad38df80ba6408e35a6031b5b04f91ad7364fb228b188ed68` | `3883513887ac618f13bb836f87c86fc388ceb3cfbbcc9fae793a3a4abea20e2f` |
| canteiro_obra | 15:19 | #1 de 2 | `b2ebdb92-d4ee-4f64-babd-260fde0db01f` | 4 img + 55 | 7022 | `canteiro_obra_hd/canteiro_obra_hd.zip` | 9.7 MiB | `c569b68a04eef1b01d817d810f11c7b5209cba066d460c52908d23cbda05b0e4` | `4485973f3f9059e7d10d7e80bb3ab6118dc5c0013c4e070725b3ff83c0115f54` |

### 2.3 Lote 6 (2026-10-29, fim da tarde)

Só HD (4K, 50 000 faces), objetos em FBX "Blender 4K". Imagem escolhida entre 2 opções. Licença `tripo_pago`, Privado. **6 exports pendentes e bloqueados** (resolvido em 30/10: as 6 peças foram regeradas na conta nova, §2.4): os modelos estavam pagos e guardados na conta antiga, mas o plano venceu às ~17:10 (§1) e o plano gratuito não exporta (pede assinatura). Só o `terminal_aurora_n2` saiu a tempo, às 16:58. O task id do `hangar_manutencao_n3` não foi lido na geração e sai do painel de ativos (6º cartão da grade, ordem do mais novo para o mais antigo) quando houver export.

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| balcao_checkin_n3 | 15:34 | #1 de 2 | `6d83b097-9f74-47d3-a84f-37483fd7bd61` | 4 img + 55 | 6923 | `balcao_checkin_n3_hd/balcao_checkin_n3_hd.zip` | 11.1 MiB | `5a2bdc1e43bca6485517f78447dc2d5e94fb053f0947281f15f5cd8421e62e0e` | `20ef1bf1fcd4541e385633cbbd73372851921a54f53e79ccd62f0d6c14021ffe` |
| raio_x_n3 | 15:34 | #2 de 2 | `ee87d05d-49fe-490d-8c1d-0711091c8cf7` | 4 img + 55 | 6868 | `raio_x_n3_hd/raio_x_n3_hd.zip` | 9.4 MiB | `cf06660ebb7b4a37c4bf2d0e835400fe96c17c96c07885263637704cfa8cf431` | `83f5584f0388f0398daccea5ba1f6b3fbb91e727e093bebe04c853a93dd09d71` |
| esteira_bagagem_n3 | 15:34 | #2 de 2 | `e272641d-b872-4ccb-884d-80820c367537` | 4 img + 55 | 6813 | `esteira_bagagem_n3_hd/esteira_bagagem_n3_hd.zip` | 10.8 MiB | `79ddb68701d1e846048975b5be840d33966aaf2d307a2f0f104bf715d91a0ef9` | `fbbeb3f1ff2fbb6c70bc051333723df3fc45ef931e46d1b0aa6ea4f706714f64` |
| portao_embarque_n3 | 15:34 | #1 de 2 | `de15e212-3495-48c2-a826-be96ca883fd9` | 4 img + 55 | 6758 | `portao_embarque_n3_hd/portao_embarque_n3_hd.zip` | 10.7 MiB | `66bcd05b3fca1f0847e8886cdcf2f5feba2b084c53273c21ace26802b5ce3f5d` | `f3859d1aad488ab287c86aac786fb7ae2ba4118a23c127a51320a1781299c11e` |
| loja_n3 | 15:35 | #1 de 2 | `4b2ae957-e9b2-4b39-8b3a-e2562fb38fbc` | 4 img + 55 | 6703 | **regerado na conta nova (§2.4)** | — | — | — |
| alimentacao_n3 | 15:35 | #1 de 2 | `c79b346f-b6d5-4c02-b296-ab80281261d1` | 4 img + 55 | 6648 | **regerado na conta nova (§2.4)** | — | — | — |
| hangar_manutencao_n3 | 15:35 | #2 de 2 | ler no painel de ativos no export | 4 img + 55 | 6593 | **regerado na conta nova (§2.4)** | — | — | — |
| torre_controle_n3 | 15:35 | #2 de 2 | `5ab297ca-b111-4ecf-9d02-59e828b52616` | 4 img + 55 | 6538 | **regerado na conta nova (§2.4)** | — | — | — |
| terminal_vale_verde_n2 | 15:35 | #1 de 2 | `d8ae9ef4-5eb3-4d57-9e93-a1b4847accf3` | 4 img + 55 | 6483 | **regerado na conta nova (§2.4)** | — | — | — |
| terminal_costa_azul_n2 | 15:35 | #2 de 2 | `a8963395-6f79-4fa9-9009-af39bdaf1bee` | 4 img + 55 | 6428 | **regerado na conta nova (§2.4)** | — | — | — |
| terminal_aurora_n2 | 15:35 | #1 de 2 | `7af1f624-a33a-44c2-8813-a0eadb8bb229` | 4 img + 55 | 6373 | `terminal_aurora_n2_hd/terminal_aurora_n2_hd.zip` (export 16:58) | 13.2 MiB | `5d8466cf313e45ac81f2e54760ba8ca844f4ac6f80b5bbfe52ffa2a39249935d` | `75baf5ed866496195717af4c3ec1adc2313ee05d4abc05b9b96ad8c9ce710686` |

### 2.4 Conta nova, 2026-10-30: grupo A e Lote 7

Só HD (4K, 50 000 faces, "Generate 55", **Privado**), objetos em FBX "Blender 4K", personagens com Auto Rig Mixamo (+20) em FBX "Mixamo 4K, Export Skeleton". Imagem escolhida entre 2 opções (as 2 ficam no histórico de imagens da conta). Licença `tripo_pago`. Conta "vinicius", plano Max (§1). "Saldo depois" é o de depois do modelo; os 16 rigs (+20 cada) saíram depois de todas as gerações (§1). Os nomes seguem o `ASSETS.md` §2.6.

**Grupo A: as 6 peças presas na conta antiga** (imagem e 3D refeitos; os modelos da conta antiga nunca foram baixados):

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| loja_n3 | 08:40 | #1 de 2 | `c6c24873-ffbe-4377-8700-929e8cab2815` | 55 | 25145 | `loja_n3_hd/loja_n3_hd.zip` | 9.9 MiB | `44fc05a064535254e80baa1c555fd3c301b851190f2a7041efb84f540cbf3f68` | `0e6431cfa832424705c7334a82d8eb0c8694c3da7448ecbe1289be0c7a452eaa` |
| alimentacao_n3 | 08:41 | #2 de 2 | `029c4ded-6063-47bb-86ae-3e40002bc9a0` | 55 | 25090 | `alimentacao_n3_hd/alimentacao_n3_hd.zip` | 13.0 MiB | `a4592e05d343a1e169a7d943a027fecc9c05d1c1880bd8db0a5754321fdf9e09` | `eb57ded0e4ff7446cf45aa037e7c7f8c63763d5fe02d547dc2dad982a74dd604` |
| hangar_manutencao_n3 | 08:41 | #1 de 2 | `4476f9e9-f1e5-4bc2-aad2-1f74c32b0da3` | 55 | 25035 | `hangar_manutencao_n3_hd/hangar_manutencao_n3_hd.zip` | 11.0 MiB | `631487f470daad51da72f062eb789e2a311bab59b3bc0e6be6710c64703a730e` | `064352a524617ac6b0ec7fe1e21d8ebcf61ef9e11de6651c9e4bc642646e9e22` |
| torre_controle_n3 | 08:42 | #1 de 2 | `09f69435-59d8-4bb4-80b3-5c6d36812322` | 55 | 24980 | `torre_controle_n3_hd/torre_controle_n3_hd.zip` | 8.5 MiB | `66bd874b2c474b872ab2db385ea60e99ac00de3227062a42ff9c82a55405b3f1` | `004b2ce7a5cb7d5a3d9216c2fff8d6376b4746cf2c7da3b49d7db261abf8fdd9` |
| terminal_vale_verde_n2 | 08:42 | #1 de 2 | `b9b0f3ab-6a64-4d1b-ab94-95e8e41958d1` | 55 | 24925 | `terminal_vale_verde_n2_hd/terminal_vale_verde_n2_hd.zip` | 10.9 MiB | `16f68e69a99909c1db1089259bcb0fc6dfae471dfc800cf7b5f7a7671140a675` | `ebfd54fe36a8ec51f197c825abd3b5b5d15c8b2915af814e2831ff73f7c0fbac` |
| terminal_costa_azul_n2 | 08:42 | #2 de 2 | `aceaa088-b8eb-4c86-8d2e-5e72e446d8db` | 55 | 24870 | `terminal_costa_azul_n2_hd/terminal_costa_azul_n2_hd.zip` | 12.6 MiB | `e5bb3a381b95cfeaa22b58d51bde203874a388469a8525dc1f9ad2c459e5fa82` | `63f5294780e7c25b5540052bb6e101039b5e4d00c96f9b7af0b17846116c9f51` |

**Lote 7, grupo 1: terminais nível 3**

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| terminal_vale_verde_n3 | 08:45 | #2 de 2 | `6423d174-b3f0-49a7-9042-a0dd75a081e2` | 55 | 24815 | `terminal_vale_verde_n3_hd/terminal_vale_verde_n3_hd.zip` | 12.7 MiB | `d57540822eec6f5ff4ee418763188d76394342ad9634e68710a012cfa352e7cc` | `b6f2a483245e323d1d0f34f05e1322c91c46f01d389284c95a94cdf4f9db62a6` |
| terminal_costa_azul_n3 | 08:45 | #2 de 2 | `145497c7-302c-467a-9bba-e7ebf31a0da7` | 55 | 24760 | `terminal_costa_azul_n3_hd/terminal_costa_azul_n3_hd.zip` | 11.1 MiB | `925436e7ed79e5664c91054aec0e101702375209988d398902befac61a550bbf` | `a31c4ddfb13b9d6506a590e7ef19ff0548fba50943de02df14c38dff9eb971da` |
| terminal_aurora_n3 | 08:45 | #2 de 2 | `b6b507ee-4b2d-43d5-8f29-608203712985` | 55 | 24705 | `terminal_aurora_n3_hd/terminal_aurora_n3_hd.zip` | 12.5 MiB | `1bf82b50d2589f2afafa9ac0678f09d05b2013fb69e3440f4fc3b37f34ee1ab2` | `ea8d9f5fd70f4c83537ab96c62f59f97518939b434aacb7ab18cd9d7e226f019` |

**Lote 7, grupo 2: aeronaves extras** (brancas, para pintar por cor)

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| aviao_jato_regional | 08:45 | #1 de 2 | `8bff4027-2724-4fa9-8146-3e52e8496378` | 55 | 24650 | `aviao_jato_regional_hd/aviao_jato_regional_hd.zip` | 8.3 MiB | `825cd098eb7dbc18c6f2963592df01c52301551ae285ddd86ff759bda57190cf` | `cbd6b778c0dcc298597a1a18b325075fd3013bdb598cd6d88f3f4b5205cdf0a8` |
| aviao_bimotor_helice | 08:45 | #1 de 2 | `1607cfad-6122-4b53-b158-b939b1431ee0` | 55 | 24595 | `aviao_bimotor_helice_hd/aviao_bimotor_helice_hd.zip` | 8.2 MiB | `488601fa66b84a24b9a60a5064048482017d1cc4128f2aca4e483c14498887e1` | `95f6feff101be223fd0360c0c975d6d4095a752b14d0fcd532f1211fc3b0a237` |
| aviao_wide_quad | 08:45 | #1 de 2 | `87c39cec-2eaa-4a23-9b9b-ee6590cdd206` | 55 | 24540 | `aviao_wide_quad_hd/aviao_wide_quad_hd.zip` | 8.7 MiB | `51965203236f7edcb1bdb00bcd9406875167558d5a5e33567fa25c710ee02992` | `7f79dcd3241695770c24536b0e618a759b917a3a9cd619f48c4aff570b380b99` |

**Lote 7, grupo 3: personagens** (T-pose, mãos vazias, uniforme em cor lisa para a máscara)

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| cozinheira | 08:45 | #2 de 2 | `4e045de5-46e7-4439-bff6-45516376a845` | 55 + 20 rig | 24485 | `cozinheira_hd/cozinheira_hd.zip` | 57.8 MiB | `411f6b27e2c34598cf2ed18d288343850c1b298ee6de0a8195450659d03f2be3` | `61298fc1eef1bbac3dda4dac5183843cd112b6672616c8963086b2d91007c480` |
| garcom | 08:45 | #1 de 2 | `4b29a943-229d-4627-8935-2e1ac2bc48a1` | 55 + 20 rig | 24430 | `garcom_hd/garcom_hd.zip` | 59.4 MiB | `f8745943f77929c4cd77a45f1302afbe618879e98131a62247874036740c2e75` | `85180a6c920b9efa105b9f5cac923e4cebb70ad376f4d10dae40e34a6e15eade` |
| agente_portao | 08:45 | #1 de 2 | `41696193-febf-40ba-8721-5f3722867b9e` | 55 + 20 rig | 24375 | `agente_portao_hd/agente_portao_hd.zip` | 56.7 MiB | `be6b990364f49ce63546d5f5f8975f052fdbbd175d9bd2c40dc8c382ff8a9683` | `e6cc91c2efb5a19bc55975e59ab979a5b855f2de44383994f841dbc9a0ce62f5` |
| agente_portao_f | 08:45 | #2 de 2 | `cd22fa73-c3e3-480b-b926-66cb6cccefa3` | 55 + 20 rig | 24320 | `agente_portao_f_hd/agente_portao_f_hd.zip` | 55.7 MiB | `b0116b2892a8d5fcb70fc557c8a85a4487c3de3bf749e4cce3e5ea3758739ce3` | `b625aefc764d2603dabcf0b85fb4915bd55758984636b106145a550f9c652f23` |
| pilota | 08:45 | #2 de 2 | `44f2a360-8467-4a4a-82d2-d057c304e715` | 55 + 20 rig | 24265 | `pilota_hd/pilota_hd.zip` | 9.4 MiB | `96e853d8bea00f5c81fadd89db91c77fcb669ee7a60e2cdf727a3a325aa56c40` | `53737ba35127719732742b55c79c1ab8232c34d7874e52f1b2ab6e996451262f` |
| mecanica | 08:45 | #2 de 2 | `c7b050b3-289f-4b32-841d-0f81c7a3ee09` | 55 + 20 rig | 24210 | `mecanica_hd/mecanica_hd.zip` | 8.2 MiB | `f6c098a24355ea4eb6d396a3d163d128b33341bbed9e07dacfb0e401eb0db5de` | `572a7a0d971dc9b420bddf85eefbdb3759eb85424f23dd5889102f6eb7303af0` |
| seguranca_f | 08:45 | #1 de 2 | `2f0605c7-f0f1-4c41-b48b-4a020a16d24d` | 55 + 20 rig | 24155 | `seguranca_f_hd/seguranca_f_hd.zip` | 8.9 MiB | `fae94fe60bfe6f63b9c30e59e857e2f5279f1bddc2999a02316be5b82ffe4f14` | `5f84f756e26ca2343caf21b5a818f4a695d06de7d7dd61c932c26813ef6f3299` |
| bagageira | 08:45 | #1 de 2 | `e624742c-ba2b-4bce-a720-8e6ead8ec278` | 55 + 20 rig | 24100 | `bagageira_hd/bagageira_hd.zip` | 8.6 MiB | `41256233481a93803c39a9e7bef89b23ea5cfc1c3582f6f6f6d834d8f9135a93` | `8c6589fb81b742ea9626e4ae958ea1e9d3d7a20de10a93fe10bc9b221425b4bf` |
| despachante_f | 08:46 | #1 de 2 | `93b615bf-f326-4dbc-8f59-1e8fcfe76022` | 55 + 20 rig | 24045 | `despachante_f_hd/despachante_f_hd.zip` | 61.0 MiB | `9dd5fd135aae3461989c92bb2378339117ac9609d6923c21d1ffc82039602469` | `e7e2bb6d3d37856e33e393c6b337aeb1b2ae3bf4fcb47b5c1746c8e0a4bab912` |
| atendente_m | 08:46 | #1 de 2 | `57e0241d-7321-4664-93f9-a3f0d3869d24` | 55 + 20 rig | 23990 | `atendente_m_hd/atendente_m_hd.zip` | 60.8 MiB | `0b65f87a438d79de7e158ed47748cfa2f541fcf69021046dabdf8eeb82daebad` | `ed963c7ae7ed647ef67cdf53ecab1360493bdedd6038e625a1dc9b780d067fac` |
| vendedora | 08:46 | #1 de 2 | `4d442888-9007-405a-ae1b-58482fedfafa` | 55 + 20 rig | 23935 | `vendedora_hd/vendedora_hd.zip` | 66.7 MiB | `38dd5fc0f180d884160fc409fbef63f84e8d740807e20dff6f044f440d12bd37` | `3017b1b731293e05a7dfaadbaf1943aef528cf74f0c3be615c63d8ae897e4cca` |
| cozinheiro | 08:46 | #1 de 2 | `eee26f57-6d59-4e96-8b48-f9b6300aa490` | 55 + 20 rig | 23880 | `cozinheiro_hd/cozinheiro_hd.zip` | 56.2 MiB | `d780249837252cac484fcd4d28f408d7f084a15b0ffbea27eaea7b815ca0371e` | `b50cc945c1cb34438dc2f639b80bb9138984d4ec5c5af871c2b669a2107439d5` |
| adolescente | 08:46 | #1 de 2 | `1800f171-9893-4026-82b0-1e08a40fc2fb` | 55 + 20 rig | 23825 | `adolescente_hd/adolescente_hd.zip` | 70.1 MiB | `8c596314da3c8aa06d7239f37d3090c85d72d038bf6eaf746a85100fe5dd6980` | `f84bc69ee3e0068eea3dd645791203a7e2585e1442b6cefb1d99f58e46b32160` |
| passageira_executiva | 08:46 | #2 de 2 | `369cead6-c9f7-49a8-a510-5fcfa25c4017` | 55 + 20 rig | 23770 | `passageira_executiva_hd/passageira_executiva_hd.zip` | 59.5 MiB | `4c9d2576cedf5935f4a367903f3577e94c912083f559b000b5cf87f1c89e46c6` | `e204433b8790643dd2aeeb1fa2576e5a4008d90cd4beb9e00379baf34fdc3737` |
| surfista | 08:46 | #1 de 2 | `7b2595e8-f44f-4eb0-9ea8-93ae3295eaa8` | 55 + 20 rig | 23715 | `surfista_hd/surfista_hd.zip` | 74.9 MiB | `1bf982b43d163917efe38e638c999b9ec655e60f3cf1a7eda9cd0cc4cb08953e` | `7e859d55c174bd4efa69673f2c644db562d223a876b340c6ad174abc11af4e36` |
| passageira_turista | 08:46 | #1 de 2 | `99fe025d-c046-4ccb-a451-1a3edd696e18` | 55 + 20 rig | 23660 | `passageira_turista_hd/passageira_turista_hd.zip` | 75.5 MiB | `9b152ec1fa316e73b8f6b5994a58b4d89c116215621d756e20d58285f9f65a60` | `d9496cb448d5441f95bb6ce134712f54eadc9017a29f875a0c1eebdf9074c083` |

**Lote 7, grupo 4: identidade da Costa Azul** (8) **e da Aurora Global** (9)

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| farol_costeiro | 08:53 | #2 de 2 | `696cc964-06c7-49f8-86a8-63fa7353f379` | 55 | 23605 | `farol_costeiro_hd/farol_costeiro_hd.zip` | 9.8 MiB | `54c79b62217bbad50e08a659be82608652f81b756662cf609f1d446f98f3c727` | `20a90447151af0dff3051a6e45c3113c3d38f139f65746a45ac3f5d7a8bd8c6a` |
| doca_hidroaviao | 08:53 | #2 de 2 | `26f46a0a-9f76-40dc-bd28-8b85021daedb` | 55 | 23550 | `doca_hidroaviao_hd/doca_hidroaviao_hd.zip` | 11.4 MiB | `b4395c1153b5bad5db83f9717d1272da64798940521ac06832bb601982dbd481` | `5af7aed980c44aef8ad2cc4bf3d2569ba164f80a3fe655be426e72258a797812` |
| quiosque_praia | 08:53 | #2 de 2 | `5a8cf3e0-de64-4162-842d-88aced1afa2c` | 55 | 23495 | `quiosque_praia_hd/quiosque_praia_hd.zip` | 9.5 MiB | `283b98a8f7516df51ff710d06ff07ddd98ed0ceff3fd2f935df73968eb855f29` | `ee225e267b171aba5848ef479b66b453b7315b0b592455326bd3ea9d4d2e7950` |
| torre_controle_costa | 08:53 | #1 de 2 | `620ddca8-f799-4224-9509-b73accd6575d` | 55 | 23440 | `torre_controle_costa_hd/torre_controle_costa_hd.zip` | 63.5 MiB | `d724fba8cd6de20d4e445850ecfc952698a8619d4f98d56e7d247f87cb13e3f1` | `f9618883b1198de8a0e311eb2eedf488d03f9136f56d65ce3f730e348bd9ba99` |
| hangar_costa | 08:53 | #1 de 2 | `3eb1c74a-49bd-45af-917d-2ed9abda504f` | 55 | 23385 | `hangar_costa_hd/hangar_costa_hd.zip` | 9.8 MiB | `de11060d4b90673c4f3c62a99b3ef69d7551ac8c0f455453f639923f72288b13` | `da4b03c0b5b41cbccc3fd73b7e525104bd75e94de676688a60b7c3cab3c1e011` |
| veleiro | 08:53 | #1 de 2 | `ba22244a-54d0-491c-8140-36746bb11b69` | 55 | 23330 | `veleiro_hd/veleiro_hd.zip` | 8.8 MiB | `8cbe2577a871a23a298db9808fd9765e852a12234185a931148b630cc7d9ca77` | `cd6235e7d6e161eea261e82447a71bef9aceabcee814da26e0d85f0c18c9e20b` |
| guarda_sol_espreguicadeiras | 08:53 | #1 de 2 | `313ae3f1-bd46-4a6e-a85f-a46b4873b343` | 55 | 23275 | `guarda_sol_espreguicadeiras_hd/guarda_sol_espreguicadeiras_hd.zip` | 8.7 MiB | `b0e85aa32a419423362e784bf1f99bca5ece170205af79db1e976e54f099939d` | `f66bc6f7a22b438cf6f614c9c491d8f32c8e9ee868137a1de7b13dbc8fd43e7f` |
| resort_hotel | 08:53 | #1 de 2 | `851e6984-8511-4f86-8e0e-fe765d94017e` | 55 | 23220 | `resort_hotel_hd/resort_hotel_hd.zip` | 11.4 MiB | `23db0740d1e4718c00e678d0910498bb91a00b7f30782a17e99524e7d495bd7e` | `65be81937d68004157ca1c3575ae439de1e773801584468cf3e118b6d3c80980` |
| torre_controle_aurora | 08:54 | #1 de 2 | `a27c6b98-87a3-4252-8b33-3d7673a550f4` | 55 | 23165 | `torre_controle_aurora_hd/torre_controle_aurora_hd.zip` | 9.2 MiB | `763a968356791ec62dd56337b78e06071de3277a023ed4910d38f41d1bfe961c` | `fdc1b3a093af1257982d7dc97b187eda31462e3a71f9a15b94d3c197720d4af0` |
| hotel_aeroporto | 08:54 | #1 de 2 | `00a99639-82ca-4887-9214-4c6d166fcb71` | 55 | 23110 | `hotel_aeroporto_hd/hotel_aeroporto_hd.zip` | 9.4 MiB | `868e381e7020609de7b1016753b715db423fdd46e012bb04411ac6164ee9fc46` | `8a19d1a4b64dd93a82863c75124bcf2119227ae8803a4778324f59daa3b77ea2` |
| garagem_estacionamento | 08:54 | #2 de 2 | `97c915a7-88aa-4b40-83a6-9c5f3d4bde66` | 55 | 23055 | `garagem_estacionamento_hd/garagem_estacionamento_hd.zip` | 11.8 MiB | `96c25d978b34d144b511eb8b8deacfc7425ee9b41e349de7a9b59ad3d7b7d076` | `a140f215ce73e16d329c483dc73069bb4e6b1e794ecf4e69d30acd9797e8678b` |
| monotrilho | 08:54 | #1 de 2 | `32af8d09-3607-4524-a037-853ac66c030c` | 55 | 23000 | `monotrilho_hd/monotrilho_hd.zip` | 11.1 MiB | `16667b9aa216d6d8c30444c7adbf69941bc500fe0f08946e1603f500363d48eb` | `d0775d15aa13f8aeb325d183581a828518d4ed71b5a2d8c9afc8a3adf060098c` |
| escultura_dourada | 08:54 | #2 de 2 | `e83b8c89-c2eb-4c2c-b87e-385a0f0dd25e` | 55 | 22945 | `escultura_dourada_hd/escultura_dourada_hd.zip` | 9.4 MiB | `887a906b8e0a47fb16d8d15b44835ce75e16599c13e9c8250104cc4253423a8a` | `bbd5c5f114582c4b2e84014d9189d1b874e761e2ff20160efcc434cf41df9f6d` |
| lounge_vip | 08:54 | #2 de 2 | `26c27348-5c28-4cff-abac-df11262be3b3` | 55 | 22890 | `lounge_vip_hd/lounge_vip_hd.zip` | 9.6 MiB | `9967c46f965524f90fba127f1ab10e9dbd55b841138d717a7737bf659efc65ee` | `bee64f2d92f84719433cc868f36ec0bd6cf911971fad74a8e28db9a95b932c30` |
| terminal_satelite | 08:54 | #1 de 2 | `c7d7046c-2e63-445c-bbd8-6902b891e818` | 55 | 22835 | `terminal_satelite_hd/terminal_satelite_hd.zip` | 11.6 MiB | `2996bc6bc91ed8c81baab03679b6b37044946d461731fc5761b14e72ff497e2d` | `89acb744826e58fdce21e59b798542fb578ff366507bbba6191ef1b02f7bbd10` |
| hangar_aurora | 08:54 | #1 de 2 | `2d31e359-5747-4df0-a911-718eb9e91630` | 55 | 22780 | `hangar_aurora_hd/hangar_aurora_hd.zip` | 10.3 MiB | `fbb2d2e1d1f33e2f344a656ef25d2f9958b1d4ce1ec1904385bb7b0ab4888810` | `45ce593b5c5681f0695c48c274ce608a76dfc7aee02b5faa60013802083c3c66` |
| skyline_torres | 08:54 | #2 de 2 | `6960d70f-a64c-46fd-a21e-922831530442` | 55 | 22725 | `skyline_torres_hd/skyline_torres_hd.zip` | 11.5 MiB | `05bc4992d07d25cfb7e184906012e53bd8c707f917decc73e69a72c6245db1c8` | `520a6574d0e8db07e1a0b41cb0b12710fbcf39dc3175afb8b3679f47c99f1339` |

**Lote 7, grupo 5: incidentes**

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| ambulancia | 08:54 | #1 de 2 | `2c97d42c-d6fa-4333-9055-ad4b2f1010fa` | 55 | 22670 | `ambulancia_hd/ambulancia_hd.zip` | 9.9 MiB | `70f1a1238c7a0c87e06abc53c6b711d71232d759b4bd3ad248495a02ecf60d09` | `7dbab1b7681b5db3711db64ecad0af52b05fbf7beafe86d3f871bd9c3f254237` |
| viatura_seguranca | 08:54 | #1 de 2 | `4d1fdd7f-e137-4710-9551-b12fd89d8843` | 55 | 22615 | `viatura_seguranca_hd/viatura_seguranca_hd.zip` | 8.8 MiB | `2f206b00d07356b6cd1c74727fbe4bf69d3a67707cf6aa411ab35b221d10632e` | `6361960abc6781b6b8e0dccf7262ebf372a431330b1381a3b4eaa4080865c54a` |
| varredora_pista | 08:54 | #1 de 2 | `f7e6f6af-28d5-4a23-b75e-0c8001dd7377` | 55 | 22560 | `varredora_pista_hd/varredora_pista_hd.zip` | 9.7 MiB | `7593f8227a0ce95587c816031278b71ca4efe5c0747315c62fc45798dd35d6eb` | `354bc0431343a82b44d8efeb3479d3c701d797544a1e50cb7d8041b9ec1a3050` |
| caminhao_degelo | 08:54 | #1 de 2 | `5a9825c2-3d70-4f05-844f-e5a1fa54f6b4` | 55 | 22505 | `caminhao_degelo_hd/caminhao_degelo_hd.zip` | 8.9 MiB | `85bedf526b4c653951d07b020d587389ae0a6fccebd8bde85ecb4a168d76f188` | `db2a1eb82a806033e0c172279a51d41165768138cf3a63526364cc1d05d4bde2` |
| trator_remocao | 08:55 | #1 de 2 | `5448d111-01a5-4960-acb7-96c3eff8fd7e` | 55 | 22450 | `trator_remocao_hd/trator_remocao_hd.zip` | 11.0 MiB | `611302700f1a365255b39d13c6c7abf8c5cdb570bd2cd67f07b2add5ab2adf2c` | `658411f97a0b4175f583fe720d0eebe36d60b5b598087e80b03370b3d51ab8f5` |

**Lote 7, grupo 6: interior**

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| portico_detector | 08:55 | #1 de 2 | `7528db3b-af26-4172-b5e5-55222e2112bf` | 55 | 22395 | `portico_detector_hd/portico_detector_hd.zip` | 10.7 MiB | `fb1db848d7f5c272107b0b98129125004976ef8a3b1192694efb53929d231910` | `4d8ba84a96f523b45d1fb0669b6344b559790928f1967ddaca8232bb29b22cb2` |
| prateleiras_loja | 08:55 | #1 de 2 | `14db5b29-4ebb-468c-9a49-d76cfd2a38e0` | 55 | 22340 | `prateleiras_loja_hd/prateleiras_loja_hd.zip` | 10.2 MiB | `26fe186517c97c61eb054a0e331915a9407f3e65a0ac5cfdb2860f9fed5098fc` | `60125621fbd18f9585ad48cd60c16f16b91716beb92dee9ea85d5ffb9be93ca9` |
| mesas_alimentacao | 08:55 | #2 de 2 | `0cb20d5b-7594-4787-8628-f651a6b76943` | 55 | 22285 | `mesas_alimentacao_hd/mesas_alimentacao_hd.zip` | 9.2 MiB | `fd4c45dfd206f8aed06cb81ea6064256f47087f6d4ffc4251432cd0265c930c6` | `4bbde03a460ae60cdb120d7095f94f641f73573f208664bb98dcc8fcb65a0654` |
| totem_autoatendimento | 08:55 | #1 de 2 | `2c85e805-6265-470b-9fc5-0029d45540f4` | 55 | 22230 | `totem_autoatendimento_hd/totem_autoatendimento_hd.zip` | 9.0 MiB | `33fe75d9f528da9d24e9de66f30b53c877af7b4d99ae01a9d16fb529fe8e1ca8` | `ba997013ba6f1508009562231b59043fb12d57f2554d64b603dbb0ca842e7150` |
| escada_rolante | 08:55 | #1 de 2 | `788a867b-b160-41d7-9de4-73ab3a208e2c` | 55 | 22175 | `escada_rolante_hd/escada_rolante_hd.zip` | 10.6 MiB | `4bdc4b561b4d78635a29d37122344f42760b1c2a49ebf817bbf2eaef6030f349` | `ef0bb3549d7b6a21bb72e7a1010fb018169999d1f2c6ed2256fd6946907c2e01` |
| esteira_rolante | 08:55 | #1 de 2 | `14f28e4e-f2e7-4be0-a9a1-a4e57b03eed9` | 55 | 22120 | `esteira_rolante_hd/esteira_rolante_hd.zip` | 10.1 MiB | `751cb08f892f854bf486ea02b974d982b74eb68288c5f8864a5f107e9179e645` | `09602d5b5e516ae8405ee7d2382cfedb474fecea7aa831b0d86bc4620b5362a9` |
| maquina_venda | 08:55 | #2 de 2 | `483f590f-f7ab-445e-80cd-f40c47473ae4` | 55 | 22065 | `maquina_venda_hd/maquina_venda_hd.zip` | 10.1 MiB | `aec0393c00d5ed08ffceafbfcc9463953b486f01774ff90759dd1e9ac25a66a7` | `701dad199f2b56bc56edbe754ee501841bfd95d025a51b672803a1aa1433aef4` |
| balcao_informacoes | 08:55 | #1 de 2 | `388c5be0-0925-40f9-920b-b4ae0a4f4e21` | 55 | 22010 | `balcao_informacoes_hd/balcao_informacoes_hd.zip` | 7.8 MiB | `22ad169f0f4bc8e59ed0f18724ec5d7f8d8792a1108dcaa49e04ea7043d562c3` | `6817acfa8e11f5d4a214fd54ac3231094fb121008d6791e22c36a927a3808a24` |
| bandeja_seguranca | 08:55 | #1 de 2 | `1aa5b90c-cf23-464e-a0c4-1b0cf2ac5b94` | 55 | 21955 | `bandeja_seguranca_hd/bandeja_seguranca_hd.zip` | 8.6 MiB | `14beb194e344b7a5e660214e6a21613d078eec072f64828cab807c7c449e674d` | `99fc216a95d8b6e444ce7fb39bf547a45eb4fd41417f8164bb87da4cb42a972f` |
| lixeira_terminal | 08:55 | #1 de 2 | `4fadf337-2f8f-4f90-9af9-7ce00e28ea72` | 55 | 21900 | `lixeira_terminal_hd/lixeira_terminal_hd.zip` | 8.8 MiB | `772ebbc3386b72f5ecbdeb4ba8ae7706df6d02fd5dd9174794bd729b2c9e3538` | `202529d66a0406d90fc7b698cd8427feebaf3e2ea3d4b6cf05074567085ed371` |
| catraca_embarque | 08:55 | #1 de 2 | `3a3f3001-03e8-4490-9619-e02da32152a4` | 55 | 21845 | `catraca_embarque_hd/catraca_embarque_hd.zip` | 8.5 MiB | `24566175f80e899b65328b6dd4082b79397cf539198d8bfa892af46c32305d0b` | `f1e5b3163fc5488a576c85256a1dfb06424832460c9e06e207ebced005252ac1` |
| vitrine_loja | 08:55 | #1 de 2 | `d94b1708-393c-4eb7-849d-d2b76b2ffc90` | 55 | 21790 | `vitrine_loja_hd/vitrine_loja_hd.zip` | 9.1 MiB | `ca9b56dce35db1fa77355ac0a9c9767e14932f53743d86bc2594d313db1c9829` | `82934cfb33ea7b530d8952c7db2f68aa799f1c286c99dcbaf8bdddbe3b9f9de7` |
| vitrine_cafe | 08:56 | #1 de 2 | `28901cee-4a62-4710-9cfa-416ff0684325` | 55 | 21735 | `vitrine_cafe_hd/vitrine_cafe_hd.zip` | 9.2 MiB | `635343b1fafd4456aedf36f6a772ee87f9166a2c7d7ecf94e1bfc0a6aedd43db` | `40a19931abc8f044a018f38f0a32504247548a30366fd4e1549eda243eb462aa` |
| carrinho_cafe | 08:56 | #1 de 2 | `179d4c30-3961-4e36-b592-276a668870d3` | 55 | 21680 | `carrinho_cafe_hd/carrinho_cafe_hd.zip` | 11.2 MiB | `62457af0c4262bceb5ac7822f3b1b5508ad811b50cb7ef091e1487177acb92dc` | `db56fccdac3d0b4b8cd990f18a46046a712b5609d8e113fa639d6ef5ada035db` |

**Lote 7, grupo 7: o que couber**

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| hidrante | 08:56 | #1 de 2 | `c2b7a371-2b16-4fda-b74c-0da6992d4dd6` | 55 | 21625 | `hidrante_hd/hidrante_hd.zip` | 9.0 MiB | `28a6e0f4f494daf913df23c305f183254a5905930406897b77f584bc8fe02e0c` | `b49d3c235be73a212e60a21195c106a9cf94d43183918ac1347d8b085f60d812` |
| container_lixo | 08:56 | #1 de 2 | `d1b6bdc2-2e3c-42ce-a82c-e1501e50f184` | 55 | 21570 | `container_lixo_hd/container_lixo_hd.zip` | 10.5 MiB | `9b86bc557ecf806ac9e1d4662cb1df4d9196e5324a36d319e137e032ccdaea9a` | `3599744cbcb07b57e1306bfd5bc815bbb3ef49ec3e1d4aa3774681335b8a9a01` |
| arbusto | 08:56 | #2 de 2 | `04c1b32d-2c2b-4612-964a-489aaad539df` | 55 | 21515 | `arbusto_hd/arbusto_hd.zip` | 9.1 MiB | `c1ca013977ce5f02b023e87f8f9a5b436027d143b65799417385c728bf6de0a1` | `7d1992fe06d95ec3ee7d51ad9726f4d525172b355013fdce5d6aace6180b4df9` |
| canteiro_flores | 08:56 | #2 de 2 | `6e535b1d-1bdd-4687-bdf1-5ae552edd1b1` | 55 | 21460 | `canteiro_flores_hd/canteiro_flores_hd.zip` | 10.1 MiB | `35265568613920941a9e4b1ca4b994a8745be995587e6a012163877a3e317a52` | `56ea64834c9bf8b601804c875b676a36a394a64a247788ac77d2f73cb653af13` |
| luz_pista | 08:56 | #2 de 2 | `35bd106e-3f3f-4216-a346-df91f810d8a4` | 55 | 21405 | `luz_pista_hd/luz_pista_hd.zip` | 8.2 MiB | `3b3f472e9e88c6482c09a359819febe83f285d4d99935c8e93f157241162bc33` | `c2374e62825adf07efbbc621605327e9f7034a48c595d95007703361c39123de` |
| gerador_solo | 08:56 | #1 de 2 | `7563131d-5c40-4075-9fde-2849399196ee` | 55 | 21350 | `gerador_solo_hd/gerador_solo_hd.zip` | 10.0 MiB | `e662a5ba0e7bf46fab546d774503e5cfb8c03b88fa86a2226d513cbc1709975d` | `699b700131ed04f03b1ca83b3ac55c7d1a506a96ae1d23a409b854e8190a973f` |
| esteira_carga | 08:56 | #2 de 2 | `6aa5a983-3f53-46a0-a37b-df34f89d5ba0` | 55 | 21295 | `esteira_carga_hd/esteira_carga_hd.zip` | 9.1 MiB | `cc59f277f0bfaac4c1fe31d4c5c63f2435910b005ef204291d6d3e860e5b4b9c` | `0e50d699b7de59e7201eee2527360678fc70b86c9c0ae526a00f9904e1ad841d` |
| van_translado | 08:56 | #2 de 2 | `73830a76-a5ca-4b00-82d6-9c3aae924722` | 55 | 21240 | `van_translado_hd/van_translado_hd.zip` | 9.9 MiB | `13421697d162c09e05ace9fe3eec69f5f856bcf87b86468c0467210b4fca720c` | `4e7bc5e23951ff8ab5c8837f08e955006e31bb45f7e6f704e99d1a2e3768777b` |
| taxi | 08:56 | #1 de 2 | `d2470e3a-32f2-45e3-8fdf-ce31c413ee26` | 55 | 21185 | `taxi_hd/taxi_hd.zip` | 8.9 MiB | `5e68e849740bafce8124e31e44fcf5e8da0e67f2fc5b745def16ca40410eec5e` | `b015852c7bd0348491252b0f3f33338cf7bc0b58a7b285a4f0d3c190c45f0ac2` |
| onibus_cidade | 08:56 | #2 de 2 | `eb23e0bb-21d4-448e-b466-b3f7c00e89a2` | 55 | 21130 | `onibus_cidade_hd/onibus_cidade_hd.zip` | 10.4 MiB | `3ea26c7079e4d0d587ee62977e6baeefaaa831c9f913e8bb7fe3edf1bb883d9f` | `0f314455d37cea978d3c7a7d2a1aa68cd467bef61b15f9668e38e2c2550a79ea` |
| ponto_onibus | 08:56 | #1 de 2 | `f336f856-8376-445d-8238-4a76915b8858` | 55 | 21075 | `ponto_onibus_hd/ponto_onibus_hd.zip` | 8.5 MiB | `9874158529c363344071f880158e8910bf7e4b9209467e1fa8690ab2bcfc8db7` | `c846c371720022c27996e78dff341f7d5cb640e7bd6057b0da4a784b0d80df8d` |
| guarita_cancela | 08:57 | #2 de 2 | `fa577f9d-3aea-44f4-8886-e20d5cc25bc4` | 55 | 21020 | `guarita_cancela_hd/guarita_cancela_hd.zip` | 9.5 MiB | `3a2d9424a792342e3cd4b916d34dc935a28d34cce9f7ce2946606387e1528f30` | `d4cc6781f6e47fb6836524d509e0a9a5f8aa52620e0ef4d50b44a2a075d968ad` |
| heliponto | 08:57 | #1 de 2 | `3961ac60-d6d1-4c79-b94e-17ed6215c4bf` | 55 | 20965 | `heliponto_hd/heliponto_hd.zip` | 7.4 MiB | `503b31967af30e506f92b8f28833ea8e37d15167a6d2cff09b018ddd860fbfec` | `3fa5cd254504eb490a4f7b006e15f014c488724d049698f660e4b709582ad1e6` |
| tanque_agua | 08:57 | #1 de 2 | `7976c1a4-1228-4bd0-8101-1889f57856cf` | 55 | 20910 | `tanque_agua_hd/tanque_agua_hd.zip` | 8.3 MiB | `69140f7fa032fbc8f6a2141c6d9c1744125a625e0984a37071064dc913045e70` | `19917ecf95694c3ca862c6cbebe2e6fe90ed27195f0e4875eec42e73c57b7188` |
| turbina_eolica | 08:57 | #2 de 2 | `f6c17936-07a9-4a21-8d82-2e4a5f87e937` | 55 | 20855 | `turbina_eolica_hd/turbina_eolica_hd.zip` | 7.2 MiB | `a75c3e766bea78ea46a89c017b63ac0d2d2fe7069c4cfd755f6966f3e08937b8` | `5dcf9852e03289d03e409903f58097633484b616c70e0ef076c603cd87f5e792` |
| painel_solar | 08:57 | #1 de 2 | `70d24c3c-882f-4976-90a1-da41b13f5fab` | 55 | 20800 | `painel_solar_hd/painel_solar_hd.zip` | 9.6 MiB | `928399ec7c3b2c19afa8d497c67652a5e2a80751f601fcee9223526b6c937ccf` | `67ede9fc14cf3ab0d476608158bbc92bce3e3f340bfcdf85287be5739c3bace1` |
| carro_passageiro | 08:57 | #2 de 2 | `c2c8d1c6-48f1-4d5e-bc88-2e0785d17186` | 55 | 20745 | `carro_passageiro_hd/carro_passageiro_hd.zip` | 8.7 MiB | `f1b118234d61df1c98d37dc9ec866bc3b1e374e8ecbada4db66799d5c2145929` | `c5ae72333e6d166884c6e0765bb693f542825d80967af41b2f2f4ecffa45a001` |
| caminhonete_pista | 08:57 | #1 de 2 | `656038a2-f1c6-4162-b576-69845137f424` | 55 | 20690 | `caminhonete_pista_hd/caminhonete_pista_hd.zip` | 10.6 MiB | `b891e44251905e23542643d89b944b69228b8e68ecc3f489f82ce2b5a9d34530` | `3513e9da70237811db436c44a7102e8d9043f7978ac0e828aae30d486c8c926e` |

### 2.5 Conta nova, 2026-10-30: Lotes 8, 9 e 10

Mesma receita do §2.4 (HD 4K, 50 000 faces, Privado; objetos em "Blender 4K"; personagens com Auto Rig Mixamo em "Mixamo 4K, Export Skeleton"). Imagem escolhida entre 2 opções, 0 crédito.

**Lote 8, A: identidade rural do Vale Verde**

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| celeiro | 09:50 | #2 de 2 | `210b54ee-ab91-4f2b-8ed8-2d3d3d793fbb` | 55 | 20315 | `celeiro_hd/celeiro_hd.zip` | 11.7 MiB | `72313094cddb7ed7ab09423778fd29fdf59d030a6e5441670be4ad26c2bd7f1f` | `a262b0b76f3342b1d9206033e6e27a4fdd98f3d2b4ac7727e02596cfe217b823` |
| silo_graos | 09:50 | #2 de 2 | `e704e644-dded-4b0b-897e-bc0e235d7270` | 55 | 20260 | `silo_graos_hd/silo_graos_hd.zip` | 10.1 MiB | `3bc42dd50b44554ba3fecd090e471b2203f7f45ece02d6abb5e723621c24bca8` | `4383e1fba6a46b20462f78ec492039293e5916716b4220ddaef69704b1b129b9` |
| moinho_vento | 09:50 | #1 de 2 | `16e204e8-f7d5-4618-885e-c65e1b5feaee` | 55 | 20205 | `moinho_vento_hd/moinho_vento_hd.zip` | 10.6 MiB | `6ee84a94b4d82565e5566bd6e60ded0eabe90177d1b8e2fb781e21f6d38b8ac4` | `a8825ed39df95e87cb56deb74126f79fd819238530e4fe62b7b657baa0325918` |
| trator_agricola | 09:50 | #2 de 2 | `4b7348ac-d8c5-4371-8720-40cbb5bb91c8` | 55 | 20150 | `trator_agricola_hd/trator_agricola_hd.zip` | 7.9 MiB | `ba69a0b246c99e6023a7a43df020fb005410676bb21ca4d56603afc0d322e42e` | `8ea4b881c8bdd4486fff07da3a9dd412459b8782335bd981e5d9394792e5695b` |
| fardos_feno | 09:50 | #2 de 2 | `e61026b1-5e80-42a5-b5af-7a242d7d56d3` | 55 | 20095 | `fardos_feno_hd/fardos_feno_hd.zip` | 11.7 MiB | `d28cf4e42f599646089e48463cea96270790a5586da2c3b2b426b3a4e94401d6` | `d4e999e19f05df22ea7555446f3f95ce786dc6d793f065c4827da55ddcfe4a3e` |
| casa_fazenda | 09:50 | #2 de 2 | `0e264a6a-07b0-4a27-bafd-ccb10104f1ff` | 55 | 20040 | `casa_fazenda_hd/casa_fazenda_hd.zip` | 11.2 MiB | `ecb9003e1500c7c6d15ff1f33b505292d6e0312c4f55ff968503a78f14a757ed` | `2b64f7c4ede5cdf3976ef9ad9856833109d082bf668a04c3bb909d7dfdbadd7d` |
| estufa_vidro | 09:50 | #2 de 2 | `883fd748-bfbd-4713-898d-599a42922574` | 55 | 19985 | `estufa_vidro_hd/estufa_vidro_hd.zip` | 9.5 MiB | `93efecd9191c4072722e4d63637bac17a1dc6ae66563b3d7b29f51754a093a95` | `537b145849456c6c1ddd2d1d9368234dca75ed09006ff2869dac8d3ff07456bb` |
| espantalho | 09:50 | #1 de 2 | `c97db7b9-0b17-48bc-8a03-40ab3c3720a0` | 55 | 19930 | `espantalho_hd/espantalho_hd.zip` | 8.8 MiB | `71a4e966c74810ccffb6cbb365704fc430d2939f1f2c3182b6ac249f9131feed` | `6e7d32806ab7812350625e67c240e2df5eb9e21daadfd892847783c6d3d88430` |

**Lote 8, B: pátio e carga**

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| caminhao_agua_potavel | 09:50 | #2 de 2 | `fd5698e8-cbaf-4c67-ae0b-45b4bdd5c6c1` | 55 | 19875 | `caminhao_agua_potavel_hd/caminhao_agua_potavel_hd.zip` | 9.6 MiB | `b80a90cdf97cfed4b430b2f6560404e591840fa619f02d4c256319a76a866dc4` | `4613897b77a9dfcdc40d40d189733a15b16d57ebf4acbc19294ed5d6607ce08b` |
| caminhao_lavatorio | 09:50 | #1 de 2 | `7cce80e9-4838-460c-99d9-8926ad959c1b` | 55 | 19820 | `caminhao_lavatorio_hd/caminhao_lavatorio_hd.zip` | 8.9 MiB | `6b1147540b1088c905d81c80618055f784985758b1d5ce2b4820a88490e94a15` | `bee4d1dde32a0e274b6b9fb50d31ec09b2b2589490fb91e4fc08365ec0a22763` |
| empilhadeira | 09:50 | #1 de 2 | `1a5c6af9-7506-496e-808f-da23a4099aa4` | 55 | 19765 | `empilhadeira_hd/empilhadeira_hd.zip` | 9.1 MiB | `db77d57665d7c0d228a547f66c259fd2bc474e920fa95c65f16f98572470dfcc` | `15552df4d79871fe79bcb3b797ced11cddbb4c0c00302187bb444f0e6eef0568` |
| carreta_dolly | 09:50 | #1 de 2 | `b5c33e21-98aa-4df0-9340-5878232cd1b0` | 55 | 19710 | `carreta_dolly_hd/carreta_dolly_hd.zip` | 9.8 MiB | `3a4186281ca2629846e66d8ad9ba388610e5e7751433c6daeb4e7aa3b828aec9` | `8de937c2ef37a60cdfc7a0e725d2ea8dedfba50908452bc18daa6319184faeaa` |
| paletes_carga | 09:50 | #1 de 2 | `559c44f9-e3af-4ed7-915c-da6f6d442543` | 55 | 19655 | `paletes_carga_hd/paletes_carga_hd.zip` | 11.3 MiB | `676c61d531b4ef6d3d47522059579305068824ec41847ba9f24755c455979c10` | `845d9607b8ff8c737bb6915e156fd32d0fe571efa0cfd71d31a3b2693163e430` |
| caixotes_carga | 09:50 | #1 de 2 | `d3ee8c16-2779-4fbf-8502-850043e1870d` | 55 | 19600 | `caixotes_carga_hd/caixotes_carga_hd.zip` | 10.6 MiB | `5d43884e01afa762784c64a5abde4d0055b9235f0a6295e22390a7575a2ab9ff` | `575e0f63883e00e3c3d74b8f81eae66bda504f4e2a153356216337e8911c16bc` |
| calcos_roda | 09:50 | #1 de 2 | `02e32a4c-98a5-4349-a3ed-17f902f5f1eb` | 55 | 19545 | `calcos_roda_hd/calcos_roda_hd.zip` | 8.8 MiB | `499273ecd502c6fe1939b3a215febe4880fb46dc04a2418b2666d92367938a3b` | `da47ae0475be4ef47258daa6c02c26a05ea90a723c9d5a7ccb4aadfbca331701` |
| luzes_aproximacao | 09:50 | #1 de 2 | `429bc623-1326-4334-8894-b1eafbca4a1d` | 55 | 19490 | `luzes_aproximacao_hd/luzes_aproximacao_hd.zip` | 7.7 MiB | `cefa18e266da7ca929f6d947a9848e706151608d788542b66ac75304b9ad026c` | `2353f6fbf105201ccbd850730e2e734b661c16a4c480b31ab636320f43d8a2d3` |
| placa_taxiway | 09:50 | #1 de 2 | `b9ba3e7a-5071-4754-97b1-013e2fd0d17a` | 55 | 19435 | `placa_taxiway_hd/placa_taxiway_hd.zip` | 7.1 MiB | `24a3125202000f9ce22bc48608909e4766ffbb88c144d7a7ad8fcacaf3bbb162` | `78e46edb9b657715f4b5e2fb7b6acbf38b50efb6dad56a68085767c18f715844` |
| barreira_jato | 09:50 | #1 de 2 | `406c698b-b33d-4e54-b26d-26ee1c7237fa` | 55 | 19380 | `barreira_jato_hd/barreira_jato_hd.zip` | 9.3 MiB | `e3285c5ca900b1ec37a5ccd6d955f4671ef6a990a907b0e8b6d5aef0269602e7` | `1c5dc288020ef8d744f1fe22f0c92411b884eb6362605950247700d9d846d11e` |

**Lote 8, C: interior extra**

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| sofa_lounge | 09:50 | #1 de 2 | `7e3f0894-0511-4e08-9041-0a7c784dc9e4` | 55 | 19325 | `sofa_lounge_hd/sofa_lounge_hd.zip` | 7.6 MiB | `0a260d1ceb32163732bba2b230c5405c37abb2cf7680d4b9b47456c0dc71eade` | `17ca203788577f04b1cad88c2acc20b492d6f91d2886868ae06180248c7b967f` |
| mesa_lounge | 09:50 | #2 de 2 | `3f812f95-dc14-41bb-99c8-0b5f2ff3ee3e` | 55 | 19270 | `mesa_lounge_hd/mesa_lounge_hd.zip` | 8.8 MiB | `379e2d91fa07f1491aa4a7fd0114b2734f4c7728a9d1017d96466abb556cef58` | `c3dead6afbf636f4addf93e7f34ec1ae82e3574f986647dbfa6fd96c63d21aab` |
| bar_lounge | 09:50 | #2 de 2 | `fb67e1d0-cac0-459f-a788-901ccabe49de` | 55 | 19215 | `bar_lounge_hd/bar_lounge_hd.zip` | 9.2 MiB | `c9ef2fbc029157facf2ebe98a29281fb91b3843a913f7326edbe8bee7036c4f5` | `e00e8b254005b3a7621f7d7fad4e03befdf4d7bf2e9e24c94dadbbdd893ce285` |
| balanca_bagagem | 09:50 | #2 de 2 | `45d00b1a-06f7-4f0d-991c-d8be574538d6` | 55 | 19160 | `balanca_bagagem_hd/balanca_bagagem_hd.zip` | 9.8 MiB | `2a70c7a8e2130db94f88cc2917ab195943de90f9c2777319f5f514f4eb6b110b` | `53877b1811bde147412e36d6a878ba490cb59bead713e736f308e36309721fc1` |
| caixa_registradora | 09:50 | #2 de 2 | `fac6d554-6fd5-4d0e-b24a-f73faacbb46d` | 55 | 19105 | `caixa_registradora_hd/caixa_registradora_hd.zip` | 9.1 MiB | `4358221e5436d7986d35a93294d16d83f95da9b3959ab07b4b03aaa908e998bc` | `72410a79163d7140ecd062cba170ba3cd60d6c6777766acd18c2598487d7ea27` |
| bebedouro | 09:50 | #2 de 2 | `4dcb3ad9-191e-49a0-aab2-ced1ecce3e36` | 55 | 19050 | `bebedouro_hd/bebedouro_hd.zip` | 9.2 MiB | `f79c18577ab68ab10c3dd5828ac388731fcefbbae3beab882d1a816844a04ce3` | `8d9be6a5f902967168a16818b5ddd3fff7fa8d0c6d97ab451fe48aa9eb5b3b5e` |
| cadeira_rodas | 09:50 | #1 de 2 | `ae72d4f2-6a5c-4cbe-9db5-faf60715bbf1` | 55 | 18995 | `cadeira_rodas_hd/cadeira_rodas_hd.zip` | 10.3 MiB | `5dfe45ec6f0a30e06f41ef4dac573bd3a78d7575e85f2bb83866b83222b3285b` | `ba93d9d770a2c4cf12ad86a9f8a08a90eb621e05cd24da045253ebe850bfecd4` |
| carrinho_bebe | 09:50 | #2 de 2 | `465c3130-69b2-445a-b8b0-da77e0e09383` | 55 | 18940 | `carrinho_bebe_hd/carrinho_bebe_hd.zip` | 9.6 MiB | `4e49dbc302b0265f0ccdfef8ab9e335604df4d3846346cde81c14a2e3eb2cae7` | `69398d5e1e86d6cc045f23b25446d30dc92e582c33baf238a775eb9f99a52781` |

**Lote 8, D: terreno e mapa**

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| colina_gramada | 09:50 | #2 de 2 | `7a2a8e27-1856-4172-aaaf-c15425c7c973` | 55 | 18885 | `colina_gramada_hd/colina_gramada_hd.zip` | 9.1 MiB | `45bcf6612e2bca5b6f846938f2166482014481015ebfaad32330bf0bf97b06c4` | `b973bfae1e24bcf0b7cf43a793a26b3b17f2881bfbefd5c961127e183e02ea94` |
| rochas_grandes | 09:50 | #1 de 2 | `4d7778df-5327-4be7-94b3-713d5034b8ce` | 55 | 18830 | `rochas_grandes_hd/rochas_grandes_hd.zip` | 9.7 MiB | `68ffd7b06e7b8990389d7620e10991e0ee83978c9675168cf1f238098aeeb96b` | `e0ad6f67b78264da30f5b8137c48e510634c0552ba1926ddaa56ae65f961c3a0` |
| lago_pequeno | 09:50 | #1 de 2 | `9c2c0852-2df8-4242-ba10-0ffd98a479d8` | 55 | 18775 | `lago_pequeno_hd/lago_pequeno_hd.zip` | 9.8 MiB | `4dd43e33699912d74a4d7d9789966d643277e52b18255e7aeb5f050d7dea02e6` | `e1b807b99b4fda1c03b78e31d5293c381b8d160b38dc17fcf3b8d85f63b28e37` |
| duna_praia | 09:50 | #2 de 2 | `4022528c-38de-4eb0-b417-ed53f6956668` | 55 | 18720 | `duna_praia_hd/duna_praia_hd.zip` | 9.3 MiB | `28c60eb671ebba7fba9928e98fa696d6ea8018a32de5e4067e6a02e00bb37202` | `775bf572757f9dfedc57e261577ffcc1e56316c34df00994b229be63f4999a39` |
| ilha_pequena | 09:50 | #1 de 2 | `b5304e86-560e-4442-b04d-6c53e1378214` | 55 | 18665 | `ilha_pequena_hd/ilha_pequena_hd.zip` | 7.5 MiB | `c214613148307f4fa26f179f1270ee91ae9d230a294ae367d3ad3f75f1f48ed5` | `6d00d9e141be7a9aadeb0a80329359e271a0a8a07d2bd85f7a10ae18b4a9f933` |
| fonte_praca | 09:50 | #1 de 2 | `02695abc-2549-486f-bef2-5db63721ceb0` | 55 | 18610 | `fonte_praca_hd/fonte_praca_hd.zip` | 10.1 MiB | `06ea4487db6eb77fd1a90d36bcf5fa6709201b9021fee20618ada553f85e21c3` | `f0b97eb380784bcc6a7c3c88d8cb5cd0f197082cb4e28d3a5f03e3a28b8cdaaa` |
| arco_entrada | 09:50 | #1 de 2 | `7659329a-cbbd-4e60-92ff-448a777ca2d2` | 55 | 18555 | `arco_entrada_hd/arco_entrada_hd.zip` | 9.4 MiB | `7ddddd290f8ae240a1e8710144d61799715f0882291bf293e5caca640d42bb57` | `aa611d5a4758586bec0d55da88bf9757cd4323ee5c4ef2ac5c9adde11940400a` |
| caminho_pedra | 09:50 | #2 de 2 | `ff17cdf9-b464-48ea-813a-38a50333b728` | 55 | 18500 | `caminho_pedra_hd/caminho_pedra_hd.zip` | 9.1 MiB | `8eb071aae4508ff12095370e029335237f8ccb328414396308e3561f471fdcaa` | `6c287cbb2997ddd519e0c87141d4098cf42f77e006885fd580b4764b5bfcfe54` |

**Lote 8, E: personagens**

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| passageira_idosa | 09:50 | #2 de 2 | `e7fbe8da-8a79-4dba-a335-f9c4c06426e7` | 55 + 20 rig | 18445 | `passageira_idosa_hd/passageira_idosa_hd.zip` | 82.2 MiB | `ef7415c1edd2764596ec2b5510ba92678be3ed0c42ef4bd364e750ad16f61ed2` | `f1d871587f52d37b965dde93cfa7d9a92ff3fdb734febdc014a5bd0d003825cf` |
| pai_familia | 09:50 | #1 de 2 | `e176d0b5-4c6f-4834-b660-89b4ed2149c9` | 55 + 20 rig | 18390 | `pai_familia_hd/pai_familia_hd.zip` | 65.5 MiB | `61744a19aea258b3c7351d3dd519f699088436f96f88368c2bedc948e41d7018` | `d3f8116ae8dffde3633ff7bbc7adb448c806e258d71285e86da64b2505ddc9c8` |
| menino | 09:50 | #1 de 2 | `9cba17f4-03e1-4145-a8ff-63ce80ddcb2b` | 55 + 20 rig | 18335 | `menino_hd/menino_hd.zip` | 76.8 MiB | `12329f8455c10daf72b8bc4c126b6f0b5178646da52afe9f5bf6062873c7cd5e` | `3a1afcd07b46ba373aa5a1c0f5c1f2ace6be15b72e06bdf7474094636510e538` |
| menina | 09:50 | #1 de 2 | `f601264f-671a-4306-96c5-b516c137016a` | 55 + 20 rig | 18280 | `menina_hd/menina_hd.zip` | 65.5 MiB | `2633d644d53ebfa00a4191800e5cdb380b3f725b091818d9904480ebaf1ea2c5` | `17ce335e4a1c49fc6bde9ef0d9d70ca911a6855fbcee6d4b18114983e4b3851a` |
| atleta | 09:50 | #1 de 2 | `d099ecff-2d35-4597-b05d-42cd038c28c6` | 55 + 20 rig | 18225 | `atleta_hd/atleta_hd.zip` | 65.2 MiB | `70176b7afbf389fe46e4150e32098e7e0246b8e6b3c08d3cebb39aed3d812043` | `c3f8871da0116f8c57b03f6f4cd18cac065c93f3b4341ccf26a7ac1e57922481` |
| gerente_terminal | 09:50 | #2 de 2 | `fa925b44-eb59-4bac-9c5a-5fe4fb171c73` | 55 + 20 rig | 18170 | `gerente_terminal_hd/gerente_terminal_hd.zip` | 55.6 MiB | `89ef306536e98333c847c9829b9c625df1a3528c91b506e30690fb647ac577a0` | `5c2c2f9b2149af0cb66a04e3b9deecd7ec20aae68abefc2120dddaaaa8747bfb` |

**Lote 8, F: economia (cofre e recompensas)**

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| cofre_dinheiro | 09:50 | #1 de 2 | `10cb140a-ed32-4334-a048-d8fea30e76ef` | 55 | 18115 | `cofre_dinheiro_hd/cofre_dinheiro_hd.zip` | 10.2 MiB | `a681a789d37aae96363e23bca8fa2a7927319bd41f2c440db5934f6bebc4cccc` | `15128fa8b4e0676ea5a133cc599718aa6b1743c83a563b4f6c052f5c0ec85ac1` |
| pilha_moedas | 09:50 | #1 de 2 | `c32873bb-6815-4abe-8daa-dd347afd79bd` | 55 | 18060 | `pilha_moedas_hd/pilha_moedas_hd.zip` | 10.4 MiB | `dac6b142e689184a2df873a259332f57c249492616a35a82d03fab02a10be53f` | `9db7486164856deba05aec604f7af75b8118cd91c2577d5ad3ff1a4f79310fcb` |
| bau_recompensa | 09:50 | #1 de 2 | `9c70c76a-2b0a-437e-8de6-a0b5fcc9dfe5` | 55 | 18005 | `bau_recompensa_hd/bau_recompensa_hd.zip` | 10.0 MiB | `a62b48805f063d9dbf013db2172386d780beaada665af706ebd76f95def4a50d` | `8db935a035b085eb0dc19519aba7240b0a6513b4a72ace1f9fe0733d25878452` |
| caixa_presente | 09:50 | #1 de 2 | `5e08b7f6-3df7-4f93-b993-afdd17a5cd0b` | 55 | 17950 | `caixa_presente_hd/caixa_presente_hd.zip` | 9.3 MiB | `907f03ab80025596dd508fd8e75f81a66591ac4458a1923898f174022cdab8b3` | `7a79d7cc313d1b127bddf715b29d5729d4d56852dc4b68dd8bc06bffd298f140` |

**Lote 9: passageiros de variedade**

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| passageiro_hoodie | 10:16 | #1 de 2 | `4488e3ff-8e53-4a0d-b61d-a62c362f8050` | 55 + 20 rig | 17775 | `passageiro_hoodie_hd/passageiro_hoodie_hd.zip` | 60.4 MiB | `0d1aab0ccfeebc858d1c900b005856c2f782885a891be0e53874703a2a7385df` | `6eefb00fa44ce3dcf57cc652361ee1161fcc971ba54e0ceb193d1b7ca41c3740` |
| passageira_casaco | 10:16 | #1 de 2 | `4bd9f186-7a70-4a04-88e6-4e9188a351a7` | 55 + 20 rig | 17720 | `passageira_casaco_hd/passageira_casaco_hd.zip` | 74.6 MiB | `6a2a9d049cf1cdebb48c04647aaadc3059b3ca8505445e4d62af0f173767d9a6` | `46c7847a5293981e08efd6ee4aac406d3f8946b158c99e74d3e553612dfa0919` |
| passageiro_robusto | 10:16 | #1 de 2 | `904ae808-9c77-4836-a9d0-223c8e4e9fca` | 55 + 20 rig | 17665 | `passageiro_robusto_hd/passageiro_robusto_hd.zip` | 71.9 MiB | `882987c5274b0078dd321d2f21bc37ec190380a0bcc446f262cbbe016c06b690` | `ef1f37268751a83296d64a488578dc712372b0825bed86cf281af45315b65117` |
| passageira_cacheada | 10:16 | #1 de 2 | `8149e59f-22bd-4493-9c24-71b86e1637aa` | 55 + 20 rig | 17610 | `passageira_cacheada_hd/passageira_cacheada_hd.zip` | 59.0 MiB | `9514197d9f31cef42a1b637e0302d4349b87c94dd3b45d61611e719d89558bb7` | `fca7749e5c86e492d055b75b233b4edfc8ed37633a332e572a8040aadf16575e` |
| estudante | 10:16 | #1 de 2 | `dd9c12f3-8530-400f-9ebf-b353da33c474` | 55 + 20 rig | 17555 | `estudante_hd/estudante_hd.zip` | 70.1 MiB | `dc26f9e207dd523ac08ce8a4838d87a83a0f0d8f16eb01bef1f16b652db4df93` | `9bb5720845f3963414b77c70d8f8b513544902ecc31afd48f708ed317c45e437` |
| passageiro_idoso_b | 10:16 | #1 de 2 | `45f347ba-2060-4428-b02f-76720eda19ca` | 55 + 20 rig | 17500 | `passageiro_idoso_b_hd/passageiro_idoso_b_hd.zip` | 77.7 MiB | `8ff5169ef0fc3dbe51eba3137f7c442d471f4d93be4a72a2cee894a953a02fa0` | `17b8d3c7c6bcf17ca86bef6f10a78cd1d53b37d08b59084edb0b40e74b8fb4d1` |
| passageira_capa_chuva | 10:16 | #2 de 2 | `f2abbb7e-b4d0-409a-9007-90b9226c8046` | 55 + 20 rig | 17445 | `passageira_capa_chuva_hd/passageira_capa_chuva_hd.zip` | 56.3 MiB | `6d57437dd0fb194a3322118543005b0d7a06b44bad24f9b4e5ee90c37d3cbdcd` | `479b9c4c8147c579ab48c3c4dae2492f23e2edf020169e15a9932f278da3dbc5` |
| passageiro_careca | 10:16 | #1 de 2 | `dcc01fa5-b1b2-4443-873c-830bd8960460` | 55 + 20 rig | 17390 | `passageiro_careca_hd/passageiro_careca_hd.zip` | 62.3 MiB | `8ed65549d62feefde86c396947442e6af363c0870b29c4242f292de8cbd053db` | `f77ebf5970355649d5336e9b614c78249334fd59a4312a6667040bffc03fa59a` |

**Lote 10: incidentes de equipamento, frente fria, eventos, fila e a garçonete**

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| placa_fora_de_servico | 10:20 | #1 de 2 | `72b6d43e-39b5-43df-8fba-bbf216de7d01` | 55 | 17335 | `placa_fora_de_servico_hd/placa_fora_de_servico_hd.zip` | 73.3 MiB | `b72f9f430277262520804c90bedc2aeec52e4ffc1607f4f0fc3ff951623f9e0a` | `8794bc3625710912c64bd6c44b06abf5923b89041fe205e285eb79b5b6043bb9` |
| cavalete_manutencao | 10:20 | #2 de 2 | `4070c70c-9489-4940-a9b5-3007b62bd708` | 55 | 17280 | `cavalete_manutencao_hd/cavalete_manutencao_hd.zip` | 9.7 MiB | `7d6a17d4e1ae6a0d1935991efc63b41b5c3c4b43ffc1eabfdb5058c0b1628cfc` | `41faedced92facd6b8593b3c06723140931f0b4a8837a62d8639f5515fce9b9d` |
| placa_piso_molhado | 10:20 | #1 de 2 | `3e5006e1-9277-432e-acd0-182ef4e44aa6` | 55 | 17225 | `placa_piso_molhado_hd/placa_piso_molhado_hd.zip` | 10.2 MiB | `20d178d6c46dabeffaa65331f2b9f15e7b7b7dfc33f0d628cf949d996b2a5aef` | `cd27c799eb7d456f0a5b7fc8d249b25959b8b0078cd2080bae8d977f2b4db433` |
| escada_manutencao | 10:20 | #1 de 2 | `7553ead5-fcf4-4c8f-8d75-d73b2dde579e` | 55 | 17170 | `escada_manutencao_hd/escada_manutencao_hd.zip` | 10.4 MiB | `149bd36c0e0004b1711550ee6423c582394d8ff86813ee37328e21cb01906f21` | `ecacc49324652c8d4c6da66076441061ec009c8f95938b79244da45833f2cdd0` |
| limpa_neve | 10:20 | #1 de 2 | `715c8ac0-9065-40d7-ada7-b4ee0c76ef4b` | 55 | 17115 | `limpa_neve_hd/limpa_neve_hd.zip` | 9.5 MiB | `db7743bab200fa201547a0bf5f88037b45d7df079b510d9383bbb724a41c5243` | `5e499e25a27fc0711111b51d353e76446e5053988371b0a3087e94c0bdb5dd39` |
| aviao_acrobatico | 10:20 | #1 de 2 | `c112a376-f196-4223-bcf8-b7f273b3d86a` | 55 | 17060 | `aviao_acrobatico_hd/aviao_acrobatico_hd.zip` | 8.0 MiB | `89a68ba505b986be19067e0939bbe33e9931e0d3554f16331a0b74990435dc1c` | `5004e6a9d66a0fe90d07069841e1e1b7190fd67f8d5630086b65cc0aef218670` |
| arquibancada | 10:20 | #1 de 2 | `e109a1f7-bccc-4f98-92b0-9e43bd223e05` | 55 | 17005 | `arquibancada_hd/arquibancada_hd.zip` | 7.6 MiB | `bc041ad8d0e8ab56f6324dee172f58a8d61d18cbb78eac2da147deaada0d9906` | `e3ae4c10688f30faa3dd5dc3371389139890a948d6585ca94ae9343786dccf02` |
| balao_ar_quente | 10:20 | #1 de 2 | `b0286c84-d8f5-4112-a2db-9fe7063d3377` | 55 | 16950 | `balao_ar_quente_hd/balao_ar_quente_hd.zip` | 9.8 MiB | `9e502e97438e1fd93748ae56806adfccf4d863044837393585b9b693800b0ade` | `427d130540c921ec82b1e909830247e750aebbcf0ff490cfa5ab48f381e3ceda` |
| bandeirolas | 10:20 | #1 de 2 | `f3ddcdce-247f-417c-9491-0b2b4b76b7db` | 55 | 16895 | `bandeirolas_hd/bandeirolas_hd.zip` | 8.5 MiB | `970bd0e441748cf7b221825c12a680b9ea470964de8faf0af9cb45a0ac26ae39` | `53eb161410723845a710be0f4712d5d9d2faed320ed660c6347a5da0a77dc7f2` |
| tenda_evento | 10:20 | #1 de 2 | `be174d56-278f-4c55-acf8-02860313d25f` | 55 | 16840 | `tenda_evento_hd/tenda_evento_hd.zip` | 8.6 MiB | `eb9d155095cbafa8f9297d534150b66a97b98c98a1be056ed0ad58d0c89d186c` | `ab96ee3811293264142c19a6074366fea1566245cd130e44b2523cb54d526ddd` |
| arvore_natal | 10:20 | #1 de 2 | `d3320534-00ba-4059-9910-6ac48781f2ce` | 55 | 16785 | `arvore_natal_hd/arvore_natal_hd.zip` | 8.9 MiB | `5283c8bb5fa8fabe09c2773d82c90433420969333041d8207e12afcd60290f41` | `3316292c343ab48ee52b210c9cfeefe923aff65a02ec6a6725efdcd75752d936` |
| boneco_neve | 10:20 | #1 de 2 | `d1f677b3-83f8-43f8-a834-bcb125898dcc` | 55 | 16730 | `boneco_neve_hd/boneco_neve_hd.zip` | 7.9 MiB | `46cca3582e7b98639e76581f476644d2b4f953babcb32b6daebc23ca4b0d634b` | `a146dc445792d0eeceed4188868fcce741471046a7fa2a84dfa41758a4de257e` |
| cordao_fila | 10:20 | #1 de 2 | `f587c9f0-5868-4c4d-93b9-bb7c96fffaea` | 55 | 16675 | `cordao_fila_hd/cordao_fila_hd.zip` | 8.1 MiB | `26da3fa43d5dc2878d92362946ea8f700ff14c47fcf6c889be8010f6335ff359` | `50ccee4067b9a467c513f3f9787c0484ddeed75897499acefa27e356ee1b14d0` |
| garconete | 10:20 | #2 de 2 | `273f65e8-3939-46c3-a0f9-0719966c4915` | 55 + 20 rig | 16620 | `garconete_hd/garconete_hd.zip` | 61.6 MiB | `4436d64b4f8d9fee94d4b3533deb9f38b7cfb7d680209803b8e8ab15b047e26e` | `cfa0a3692573af45bc644e45dde2afdf099adf0031c8ea3a5b6e9e5052308f2b` |

### 2.6 Conta nova, 2026-10-30, tarde: Lote 11 (cabine de corpo inteiro e garagem de pátio)

Mesma receita do §2.4 (HD 4K, 50 000 faces, Privado; objeto em "Blender 4K"). Gerado e exportado **peça a peça pela interface, com cliques normais**, sem fila nem script de página (§4, Automação). Imagem escolhida entre 2 opções, 0 crédito. O zip foi guardado em `arte/tripo/` e copiado para o backup (mesmo SHA nas duas cópias); também foi extraído na mesma pasta (FBX + `.fbm` com as texturas), como nas outras peças. Faces lidas no visualizador: cabine 41 712, garagem 38 624.

| Peça | Hora | Imagem | Task id | Créditos | Saldo depois | Arquivo (`arte/tripo/`) | Tamanho | SHA-256 do zip | SHA-256 do FBX |
|---|---|---|---|---|---|---|---|---|---|
| cabine_scanner_corpo | 12:36 | #1 de 2 | `2b8bc1df-eb43-4d28-9a35-70b8ca61c87c` | 55 | 16385 | `cabine_scanner_corpo_hd/cabine_scanner_corpo_hd.zip` | 9.7 MiB | `35948a330e6132986d176f4c2ffa7553728ffd7ecd6fcc4956cdc1875570a2a3` | `581945e6c8f8ea92b87d8e789b56de2b7ae9fd410df19676dad793497f41a138` |
| garagem_patio | 12:42 | #1 de 2 | `25d555f9-4d96-4538-bf14-6d72c5aad68c` | 55 | 16330 | `garagem_patio_hd/garagem_patio_hd.zip` | 10.7 MiB | `fc4c637ee21746a7c6b3a269666234ff0e55c6a43e8f96f6d98f5ca837a790b7` | `af6d3350029dea6ab5bbd33230f57e3cbacce391439b373fc28af8ed0c8fa0c7` |

## 3. Marca e arte de instalação (imagem, sem modelo 3D)

Geradas no gerador de imagem do Tripo (Nano Banana 2, 1024 px; splash 9:16 em 768 × 1376) e ampliadas para 4K pelo botão "4K" (10 créditos cada, §1). Os originais 4K ficam em `arte/marca/` (fora do git, com backup). As versões do jogo, em `client/Assets/_TA/Art/Marca/`, saem do script `client/tools/marca.py` (determinístico; recorte do fundo azul-marinho por inundação a partir da borda, tolerância 38; conferido sobre xadrez).

| Arquivo no repo | Original 4K (`arte/marca/`) | SHA-256 do original | SHA-256 do arquivo no repo | Prompt (resumo) |
|---|---|---|---|---|
| `logo_little_terminal.png` (1643 × 1212, fundo transparente) | `logo_b_4k.jpeg` | `0048cee6d0036962f8486778a99538f2827171c22d19b3da889529cdc425adac` | `ed88f89a2eca3e141587242122f6f9c00982b7c99ec50733e4f53a8d2da20837` | logo que diz "LITTLE TERMINAL" em 2 linhas, letras gordas amarelo-creme com contorno azul-marinho, aviãozinho branco de faixa verde-água com rastro pontilhado, torre de controle ao lado, fundo azul-marinho liso |
| `emblema.png` (1024 × 969, transparente) | `emblema_4k.jpeg` | `c98004551769024b778fb264e4d76d3f1ab6e9974d51b2e150377602b0b76cc8` | `84e2acdbf376de449ecb3dec1364cd62ad133d463a7bb1e8a1c798407130454f` | selo redondo sem texto: avião decolando sobre o terminal de telhado laranja com torre, asas douradas em volta |
| `icone_app.png` (1024 × 1024) | `icone_app_a_4k.jpeg` | `04751ff81d864f67aa880ee075d9caed634c3935f9493ed60b5f52487c26fbd4` | `461f6e51f24d76ec6badd4bf0a2842d506afa2ae30e32dec2065594b5d349a3a` | ícone sem texto, sangrado: avião branco de faixa verde-água decolando na diagonal sobre o terminal de telhado laranja e a torre branca, céu azul com nuvens |
| `splash.jpg` (1080 × 1947) | `splash_b_4k.jpeg` | `597f25cde1654245b820b6f6fba915a3f2e0847b31f3df6b2982798a02c7077f` | `78c2b76c565135d4aa2f3bc8ffda0655e04505d124f5fe76c89b4e0ad60e048f` | aeroporto regional ao pôr do sol em ângulo isométrico, terminal de telhado laranja, torre, avião decolando, passageiros com malas, céu livre no terço de cima |

**Reservas (só em `arte/marca/`, fora do jogo):** `logo_a_4k.jpeg` (`e8bec7c95400847eca6ec1ad59171f4a77cd71cbfd6c326eaeaf3e7e1ad9bdcb`), outra composição da mesma logo. **Descartadas:** `splash_a_4k.jpeg` (`d16c066fe0a78eb2fc53a5e9eeeb5cdf9d2fdc58d9610a8966d2ca247fb83b88`), porque escreveu "AIRPORT" no prédio, e `icone_app_b_4k.jpeg` (`7434ce86daadd76e58ef7def9f278f6fcd4f6a8395ae81e7096e21a261bd6333`), um avião com olhos no para-brisa e boca, convenção visual muito associada a uma franquia de animação conhecida (GDD §10.5: não imitar identidade de terceiros).

O nome "Little Terminal" foi decidido em 2026-11-01 (D-03, busca em `docs/NOME.md`): se mudar um dia, só a logo se refaz (1 rodada de imagem + 1 ampliação). Ícone, emblema e splash não têm texto.

## 3.1. Áudio sintetizado (0 crédito, sem asset de terceiros)

Gerado por `client/tools/audio_gen.py` (Python 3.14.4, numpy 2.4.6, scipy 1.18.1), sem rede, sem serviço externo e sem amostra gravada: osciladores, ruído, filtros e Karplus-Strong escritos no script. Blocos de síntese, medição de loudness e escrita vêm de `dc:client/tools/audio_gen.py`; o banco de sons e a música são novos. Semente fixa por arquivo (crc32 do nome; música: semente fixa por faixa): rodar de novo dá o mesmo SHA.

- 2026-10-30: 16 arquivos, 9,28 MB: os 7 momentos do GDD §12 em 8 sons e 13 WAV (a decolagem tem hélice e jato) e 3 laços de música, a mesma composição com tom, andamento e voz próprios de cada aeroporto.
- `audio_gen.py`: SHA-256 `0f4a82f4118136c5858ab53132e918c207b73d5764aaa5885865b0a20cfcb3d8` (calculado com fim de linha LF, o mesmo com checkout CRLF).
- Comando: `python client/tools/audio_gen.py --md` (gera os arquivos, confere pico, DC, laço e alerta macio e imprime estas linhas).
- Licença: própria (sintetizado). Os `.meta` de import do Unity saem do mesmo script e não entram na tabela.
- Motor de avião, o risco do GDD §12, testado antes do resto e aprovado **por medida**: ninguém ouviu ainda, nem em celular. Com um filtro de 300 Hz a 8 kHz imitando o alto-falante do celular, sobram 90% da energia da hélice e 74% da do jato. A hélice mostra as harmônicas das pás e o batimento das duas hélices (2,7 Hz); o jato mostra o apito da turbina 16 dB acima do fundo, subindo com a rotação e caindo com o Doppler. Plano B do GDD (faixa licenciada) não acionado; se soar pobre ao ouvido, a troca é só destes 4 WAV.

| Arquivo (`Resources/`) | Ferramenta | SHA-256 do script | SHA-256 do WAV | Licença |
|---|---|---|---|---|
| `Audio/sfx/toque_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `49b4fdcc8d8b7b2da4f6354a47b0108b144edb3780ffc5cf928b4cf7805a7552` | própria (sintetizado) |
| `Audio/sfx/toque_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `d86c102c586cf2b47ab56dcffd9bd435449274eb513843d5c2367f24248c2560` | própria (sintetizado) |
| `Audio/sfx/melhoria_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `fecddb12d3bf1fd295ddec449b0e43d16e8d6e3f64ae4fb235fa9f2c7191c0a3` | própria (sintetizado) |
| `Audio/sfx/embarque_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `04bbde5c9b6df6b9283dc3cef30d6b2fb7b28d24879c07d00a2cd5d4408887c7` | própria (sintetizado) |
| `Audio/sfx/decolagem_helice_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `8be19abbc6b530352a2c7813d7f98271f966bc22d689a3f607750b7c7b68776e` | própria (sintetizado) |
| `Audio/sfx/decolagem_helice_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `a71f247ee68ae977fd7e8f96c55a32ec67757357a587728c75a259ea5cb8cb13` | própria (sintetizado) |
| `Audio/sfx/decolagem_jato_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `8ada0ce70ae57f621012c83f75eacf81aa471417f6dba751b69291f9cf13e132` | própria (sintetizado) |
| `Audio/sfx/decolagem_jato_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `6ea59450cd9467060a8a2cb958881b34e9a5dba4c0fed091a024cd52ca6660fc` | própria (sintetizado) |
| `Audio/sfx/recompensa_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `02945cca07be5005b5637a8ae9fa8b935e98212ebe095cd72539a94faa2224d2` | própria (sintetizado) |
| `Audio/sfx/recompensa_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `f0b0bf86a3fe5cee7ce1504575981f696c7b1cb0a2499a5d59f1a7660d5b4ab2` | própria (sintetizado) |
| `Audio/sfx/recompensa_3.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `37bb4eab77045a6c35fb5f8be57ba34244371be004ed8bd3abc41a69960a2dd6` | própria (sintetizado) |
| `Audio/sfx/alerta_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `da839a123edf0ea118c2bc96d6155d67632760730b1ec720fb21ae4a6f1c4ed6` | própria (sintetizado) |
| `Audio/sfx/missao_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `5c33328787b60281453b54eb475bfe82cae02ae9ec64afa0acc565bc8ca15774` | própria (sintetizado) |
| `Audio/musica/vale_verde.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `23bc1d8c3c1771297cf7ad716f1b8cf5b2aff5048064b5d2cd04f326fb9fa5a9` | própria (sintetizado) |
| `Audio/musica/costa_azul.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `c0d8479b1f4566d27f04d77fade814a48bd23c719faba153db363141c86f94aa` | própria (sintetizado) |
| `Audio/musica/aurora_global.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `0f4a82f4…b3d8` | `62b5ef05d5bbb031e711fdb74be46716e25510229865cd38e41f9d7e355985d5` | própria (sintetizado) |

## 3.2. Fontes (SIL OFL 1.1, sem crédito)

Cópias do Dungeon Courier (`client/Assets/_DC/Resources/Fonts/`), sem download nesta rodada. As duas entram no build em `client/Assets/_TA/Resources/Fonts/`, com a licença ao lado. O nome de cada uma está em `docs/CREDITOS.md`.

| Arquivo | Origem | Uso | SHA-256 | Licença |
|---|---|---|---|---|
| `LilitaOne-Regular.ttf` (28092 B; licença em `OFL-LilitaOne.txt`) | Google Fonts (github.com/google/fonts, `ofl/lilitaone`) | títulos, botões e números | `f5b641c45c69d772ee4eda687bc9fda411d5cad6b0b45371491da4580cbc8d59` | SIL OFL 1.1 |
| `Nunito-ExtraBold.ttf` (132252 B; licença em `OFL-Nunito.txt`) | Google Fonts (`ofl/nunito/Nunito[wght].ttf`, 276932 B, SHA-256 `bb55a5ca5c2042335b3991af27c4d0705d0ef41cac6164ac737fd8f2a1e85207`), instância estática wght=800 pelo `fontTools.varLib.instancer` (a licença da Nunito não tem Nome de Fonte Reservado, então a instância pode manter o nome) | texto, rótulos do mundo e placas de chão | `82494be2918c50a4e52c2dd2e2bf351a26a4eb38093cdebd91190681bac37b83` | SIL OFL 1.1 |

## 4. Licenças e termos

- **Tripo:** modelos e imagens gerados na conta paga da V-STACK, com "Privado" marcado antes de gerar. Só o plano pago permite uso comercial; o gratuito é "Non-Commercial Use". Termos: https://www.tripo3d.ai/terms (reler a versão vigente antes do lançamento). Zips, FBX e `.blend` ficam fora do repositório público por decisão do estúdio.
- **Termos do Tripo lidos logado em 2026-10-30** (versão de 2025-07-11; PDF e SHA-256 em `docs/ToolchainAudit/08-license-security-and-privacy.md`). É leitura do texto, não parecer jurídico.
  - **Conta paga (§5.2.2):** o usuário pago tem, em geral, todos os direitos sobre entradas e saídas, inclusive distribuir e ganhar dinheiro, e dá à empresa uma licença para usar e exibir o necessário à prestação do serviço. A empresa diz que não usa entradas e saídas para treinar IA.
  - **Conta gratuita (§5.2.1):** a Tripo fica com os direitos do que o usuário gratuito gera. Nada do jogo pode sair de plano gratuito.
  - **Depois do vencimento:** o texto não trata do fim do plano pago. O §10.5 diz que as cláusulas de propriedade sobrevivem ao encerramento, então a leitura provável é que as peças geradas com plano pago continuam nossas.
  - **Conta encerrada (§10.5):** arquivos e saídas da conta podem ser apagados, sem responsabilidade da empresa. A cópia local é a única garantia.
  - **Automação (§3.2):** proíbe extrair dados ou conteúdo do serviço sem autorização, citando raspagem e "automated tools". As filas de script de página usadas nos Lotes 3 a 10 são risco a considerar: geramos e baixamos peças próprias, mas o texto não abre exceção.
- **Imagens do Nano Banana 2 dentro do Tripo:** mesmos termos do Tripo para o conteúdo gerado na conta. Logo, emblema, ícone e splash entram no git como arte de interface do jogo (prompt mestre §3.8).
- **Mixamo:** nenhum clipe baixado hoje. Os clipes que virão do disco seguem o registro de origem de cada jogo (prompt mestre §4.1).
- **Marca registrada:** a busca de "Little Terminal" no INPI, USPTO, EUIPO e nas lojas deu limpa em 2026-11-01 (`docs/NOME.md`, com os limites). O pedido de registro (classes 9 e 41) ainda não foi feito, e a busca não é parecer jurídico.

## 5. Derivados para o jogo (troca do greybox, 2026-10-29)

Nada aqui gasta crédito: tudo sai dos arquivos HD do §2 por script, no Blender 5.2, neste PC.

### 5.1 Malhas decimadas (fora do git)

`client/tools/malhas_ta.py` decima cada corpo rígido HD (teto de triângulos por peça), normaliza a medida de projeto, põe o pivô no chão e reduz a textura base. A saída fica em `client/Assets/_TA/ArtLocal/Resources/Malhas/`, pasta ignorada pelo git: é o modelo do Tripo, que a licença não deixa redistribuir (GDD §10.2, decisão A). O registro completo de cada peça (triângulos antes e depois, medidas, SHA-256 do FBX de origem, do FBX e do PNG gerados) está no `malhas.json` dessa pasta; a tabela abaixo é o resumo.

| Peça | Triângulos (HD → jogo) | Medidas (m) | Textura | SHA-256 do FBX decimado |
|---|---|---|---|---|
| `arvore` | 49236 → 1500 | 3.05 × 2.62 × 3.20 | 512 px | `17ea0652f99dc2524d154f4e2aa370bd8211d530059864fd2c11342cac44ca00` |
| `aviao_narrow` | 48228 → 5000 | 9.00 × 8.95 × 4.50 | 1024 px | `5449dc9f6a5c64328363e0282cbd4b47f1c3418fc23e7f6c807555029a79d3d0` |
| `aviao_regional` | 48774 → 5000 | 7.60 × 7.38 × 3.71 | 1024 px | `9150080594aff50bdb9577cc824becbbd3ab8c7596b599a09afe3da017c0a062` |
| `balcao_checkin` | 42014 → 2000 | 0.75 × 0.71 × 0.46 | 512 px | `aafcec3ef6cac9ab547e9872583176210a19b2254bfebf8fd6d10cc8062d806d` |
| `balcao_checkin_n2` | 42665 → 2000 | 0.75 × 0.47 × 0.37 | 512 px | `d3ecbb21566a03984a9b297fa720d68d3ecaf0ca9bf49d0a7630adcd5db2e6c4` |
| `balcao_checkin_n3` | 44609 → 2000 | 0.75 × 0.75 × 0.39 | 512 px | `026a41faa0234d9a59653ff95b6b7c8f9639537a4b626eba59805f438345df18` |
| `biruta` | 48921 → 998 | 1.46 × 1.03 × 2.60 | 256 px | `a70cde5d20429228f37672af19ae3a5946dedc515e4818fa953005de26de7034` |
| `cadeiras_espera` | 48419 → 1500 | 1.60 × 0.42 × 0.60 | 512 px | `59cac73a64cbfec5901fc2547cb5a5cc7257a74763701fc23cf03db667803e2e` |
| `caminhao_catering` | 45746 → 2500 | 2.80 × 1.28 × 2.67 | 512 px | `02d320134e1fae7317a5cc29b4041ac21f34a6d32409fdf8ed12f864f4873c38` |
| `caminhao_combustivel` | 46077 → 2500 | 2.60 × 1.13 × 1.17 | 512 px | `ac2aa37aa3d698f7315a1915151e933a7d8ed1327fbd361d83dd9c69510741a9` |
| `carrinho_bagagem` | 44193 → 2500 | 2.60 × 0.78 × 0.89 | 512 px | `b575ec51c24222f6f4124981383239faef9772d7c815da4561c02d102b9a12f3` |
| `cone` | 46502 → 300 | 0.25 × 0.25 × 0.35 | 128 px | `d9f387b8f852dd64f9d3326d6924e40a63cabbddfc4e55e6c67892465c998d8c` |
| `conteiner_bagagem` | 45570 → 1200 | 1.30 × 0.98 × 1.35 | 256 px | `8394597cc8914ee7c58a15f82796edb770c728cfe9786406a907ecc83a13f15b` |
| `escada_embarque` | 45628 → 2000 | 1.17 × 2.30 × 1.78 | 512 px | `5fe94b04af154a3b9dc9a4af3f843ba3bd399cba9b08d907c3704d8956882be0` |
| `hangar_manutencao` | 42370 → 4000 | 7.00 × 6.38 × 4.65 | 1024 px | `e1db8b4ba62ed7bb3e0563ce900cae48bdac5bb16da6938fbad8201e697f70f6` |
| `loja` | 44868 → 2500 | 1.40 × 1.21 × 1.51 | 512 px | `378652bf695b96e9f8fa8bdc63309c2d0e99979a800df5f4cd982f03ade65c68` |
| `loja_n2` | 44066 → 2499 | 1.40 × 1.17 × 0.99 | 512 px | `43ce27912c150027e4f086ef02724038cf6f072797cf0d0794c3a79b76f97439` |
| `loja_n3` | 41970 → 2500 | 1.31 × 1.40 × 1.80 | 512 px | `7b3bcb40e25f72301dcf4371b436953324bf374b8cac3149701f01927dc62416` |
| `onibus_patio` | 43737 → 2500 | 1.47 × 3.20 × 1.40 | 512 px | `458bb34fa56a39d2f380f6f30e0c332dd80cc59bef15243a7b398dc57865a8de` |
| `ponte_embarque` | 44185 → 2499 | 1.08 × 3.40 × 2.09 | 512 px | `7d4c4dc5cff0f3ed567421143342a37d017c5a735fc773a4d2e393ccc32247c9` |
| `portao_embarque` | 45758 → 2000 | 0.75 × 0.56 × 0.53 | 512 px | `59054ac09343df15984d2dc0a22f4a010364c3e09547745fbba1231a76bf686a` |
| `portao_embarque_n2` | 42813 → 1999 | 0.68 × 0.75 × 0.42 | 512 px | `7bfa72d5fc2dde20142afd80a486181395b1c815835b69be1341248a92a2a387` |
| `portao_embarque_n3` | 40701 → 1998 | 0.75 × 0.75 × 0.45 | 512 px | `8a82d8aa1d181291f2120c95b6763177e1dc45107858a725d1ba7c0cf3aca836` |
| `poste` | 46942 → 800 | 1.73 × 0.93 × 4.00 | 256 px | `be5cdbc82b8ec29642077953c79c2e1fddbacca6022f59f39c7f9664dd4e02f3` |
| `radar` | 46548 → 1500 | 0.86 × 0.86 × 2.40 | 256 px | `5076160c400aa9247ffbe90c40daaa9f2832a5f247b54ea54ce430ce3ed1f181` |
| `raio_x` | 45511 → 2500 | 0.93 × 1.15 × 0.74 | 512 px | `243d50952e12a9e38e5ddf3d252fce7df90f413d25c6c765f61355b2f283c2ff` |
| `raio_x_n2` | 41413 → 2499 | 1.11 × 1.15 × 0.58 | 512 px | `9e47f13aae842ba31beed02abab3b6f8f844f81af26d29abc101bf49d74ebe5e` |
| `raio_x_n3` | 42501 → 2500 | 1.15 × 1.15 × 0.50 | 512 px | `96d285c9a30b7d97df99be36c8e27831330e36a76b5ebea2d712236dcefdad52` |
| `rebocador` | 43078 → 2000 | 1.50 × 0.76 × 0.79 | 512 px | `5941014382cd1cdd35ceaaa3c9abc2839e64394b1a9fc2ab4300925ea9caea4f` |
| `terminal_vale_verde` | 43119 → 5999 | 9.00 × 7.17 × 6.71 | 1024 px | `4e5a6f2aee8620d8a0a8fba87fdc849323cd2f6c5e46d81e5769435f3ee4a7fe` |
| `torre_controle` | 42898 → 3000 | 2.18 × 2.20 × 5.43 | 512 px | `7f1bd1163cd61974528253ce82c32cf16c7fc58e8a9d48dd3a7b9c92447fc27a` |
| `vaso_planta` | 47330 → 800 | 0.50 × 0.47 × 0.60 | 256 px | `a2d5c2cc7572ec013bc73e89e2822d331160d6f97dc39247e367eba9d3d009f1` |

### 5.2 Clipes de animação (fora do git, `arte/mixamo/ta_pax` e `ta_func`)

| Clipe | Origem | SHA-256 | Licença |
|---|---|---|---|
| `Walk.fbx` | `Walking.fbx` do Forge Street (Mixamo, In Place), backup `3D Objects\ForgeStreet-BACKUP\arte-fonte\mixamo\` | `87c18ad47c01e126faf31921d316cf0714e7867c3ab29fbedc404ca79d528585` | Mixamo (Adobe): uso em jogo comercial permitido, sem redistribuir o arquivo |
| `Idle.fbx` | `Idle.fbx` do Forge Street (Mixamo Breathing Idle; janela 199–289) | `befaa0fa9c5684a4e82dd229b04710588b1f32bc026704cc1856cae9c80f4220` | Mixamo (Adobe), idem |
| `Atende.fbx` | `_blender/Grab.fbx` do Dungeon Courier (keyframe por script no `anim_blender.py`, entregar o cartão) | `0c431dbde330dba2aa06f8c3bc8a456316bea18f37c9003492379f3cf22684b5` | do estúdio (gerado por script) |

### 5.3 Folhas das pessoas (no git)

`client/tools/elenco_ta.py` faz o `.blend` leve de cada personagem (só a textura base, 1024 px, e o uniforme no material `TA_MASK`), fora do git, em `arte/tripo/_leve/`. O `client/tools/elenco_sprites.sh` renderiza com o `render_sprites.py` (preset TA: 45°/45°, 180 px/m, 4 diagonais com espelho) e grava em `client/Assets/_TA/Resources/Sprites/<ator>/`. A folha é render, não o modelo, e entra no git, como no Dungeon Courier.

| Ator | Clipes | Folha | SHA-256 do `Walk.png` |
|---|---|---|---|
| `atendente` | Atende, Idle, Walk | 2496 × 832 px | `b0e8198536c65e96aa720ab9235e2896220648a7e054fafd3efe4ed7d8f9cebb` |
| `bagageiro` | Idle, Walk | 2496 × 832 px | `040dbedfed9f17735eb75cd73e181b8ca25dff9216e8a5fcc7f04ed90c628459` |
| `comissaria` | Idle, Walk | 2496 × 832 px | `cb4f71a7ce46c4a4b5fb754c5fc821a7b5cd96ca144ac3070b4e0d1d2d340ebc` |
| `comissario` | Idle, Walk | 2496 × 832 px | `0d2f99879edb38d31920401e58fcd3d17431837959af9ec1429041603b669dda` |
| `crianca` | Idle, Walk | 1920 × 640 px | `94e8f97e1da7697b167f6c9a6f657e8630f5faee74ae576d6ef343e641788f9b` |
| `despachante` | Idle, Walk | 2496 × 832 px | `2c01da1d114b40011445d4d3ec9a1ace8422e0aed930f28ce35f4ccffd222ca5` |
| `mecanico` | Idle, Walk | 2496 × 832 px | `9279ce65e8c9d460714af5a0a52be1312a13b3b9249ee39c9d17462fad173b88` |
| `passageira_b` | Idle, Walk | 2496 × 832 px | `887c4572a6d8cdb60dbbf563a6622333852ce3a270fa7942f5a111a8189bf784` |
| `passageiro_a` | Idle, Walk | 2496 × 832 px | `c59e5a6bf1c7034da52e414c63576e6fa8d67b0238eeec5f4148d717eaf88687` |
| `passageiro_idoso` | Idle, Walk | 2496 × 832 px | `72a0633080c2c59091268b9b9e0547884f681c4949099380eb53e25b3c054b83` |
| `piloto` | Idle, Walk | 2496 × 832 px | `cf1cfef362d90ecc48b8c8f9ce274810a10864eaaabbf50096f8a3c4d079a57c` |
| `seguranca` | Atende, Idle, Walk | 2496 × 832 px | `635cfec09fea7c46213f0a18fff8d3d9b00c9fb1a1d03eba135e3e0aa4ee9cf7` |
| `vendedor` | Atende, Idle, Walk | 2496 × 832 px | `3cbaae40b9824d7f46f840f7bbfb6efe0a8fa741183558d7ad2b64d210f0bb3e` |

Total das folhas: 14.4 MB em PNG. Máscara: `elenco_ta.py` (uniforme de Ana, Beto e Cida por cor e faixa de altura) e `--mask-attach` (a mala dos passageiros). Tripulação e técnicos ficam com a cor do modelo, sem máscara.
