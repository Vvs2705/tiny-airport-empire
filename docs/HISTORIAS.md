# Histórias de passageiros (TA-3) — as 20 do lançamento (2026-11-04)

Base: `GDD.md` §5.9 e TA-3 (Origem §50); linha 10 do `CONTRATO_LANCAMENTO.md`. Núcleo em `Sim.Historias.cs` (tabela `Tabelas.Historias`), texto em `Textos.Historia` (a frente de inglês traduz de lá). Save schema 16.

## 1. Regra

- **Uma por onda, a partir da 2ª**, no voo da sequência de Weyl (o outro, se este estiver vazio ou, sendo de conexão, não passar pelo balcão), do meio da fila para trás. Nas paradas opcionais (loja, bagagem, alimentação) ela vai para o primeiro que já tinha o motivo da parada (`Pax.Loja`, `Mala`, `Fome`): o fluxo de passageiros não muda.
- **Qual história:** entre as do aeroporto atual com o marco já alcançado e a estação já aberta. A Dona Rosa abre o álbum; depois vêm as inéditas, depois as que ainda faltam entregar e, com o álbum do aeroporto cheio, qualquer uma de novo. A escolha dentro do grupo sai da semente (`U(onda, 0, 0, 40)`). **Nada se compra:** o sorteio, o pedido e o álbum não leem Cash, Gems nem anúncio (`HistoriasTests.NenhumaDependeDeCompraOuAnuncio`).
- **O pedido** (o efeito do §5.9: muda uma decisão ou uma prioridade):
  - *embarcar a tempo*: entra no álbum ao embarcar;
  - *passar por uma parada*: a parada tem de estar funcionando quando ele passa (alguém nela e 40 s de folga), ou seja, mantenha a loja, a bagagem ou a alimentação aberta;
  - *perfil numa estação*: ao terminar o atendimento na estação pedida, o perfil pedido está lá (leve a atendente ao portão, a vendedora à loja);
  - *todos a bordo*: ele embarca e o voo dele parte sem perder ninguém; fecha no fechamento do portão.
- Não cumpriu: embarca normalmente e a história volta numa onda seguinte. Entregue de novo: conta para as missões (`Meta.Historias`), sem entrar outra vez no álbum.
- Sem prêmio em Cash: a recompensa é o álbum e as metas que já contam histórias (`vv_rosa`, `d0_historia`, `d3_historias` e as semanais). Assim a regra não mexe no balanço da campanha.
- `Tuning.RegraHistorias = false` volta à v0.1 (só a Dona Rosa, do meio da fila, entregue ao embarcar); os testes que comparam com o jogo de antes usam isso.

## 2. As 20

| # | Id | Texto (pt-BR) | Aeroporto | Nível do terminal | Pede |
|---|---|---|---|---|---|
| 0 | rosa_casamento | Dona Rosa vai ao casamento da neta | Vale Verde | 1 | embarcar a tempo |
| 1 | lia_entrevista | Lia vai à entrevista de emprego | Vale Verde | 1 | atendente no check-in |
| 2 | pipoca_gato | Pipoca, o gato, viaja na caixinha | Vale Verde | 1 | agente na segurança |
| 3 | tonico_presente | Vô Tonico leva um presente ao neto | Vale Verde | 1 | passar na loja |
| 4 | familia_souza | A família Souza visita a bisavó | Vale Verde | 2 | o voo sair sem perder ninguém |
| 5 | ze_violao | Seu Zé leva o violão da serenata | Vale Verde | 2 | passar na bagagem |
| 6 | gabi_reencontro | Gabi reencontra a amiga de escola | Vale Verde | 2 | passar na alimentação |
| 7 | theo_primeiro_voo | Theo, 8 anos, voa pela 1ª vez | Vale Verde | 2 | atendente no portão |
| 8 | lua_de_mel | Léo e Rafa saem em lua de mel | Costa Azul | 3 | vendedora na loja |
| 9 | time_volei | O time de vôlei vai à final | Costa Azul | 3 | o voo sair sem perder ninguém |
| 10 | lurdes_mar | Dona Lurdes vai ver o mar | Costa Azul | 3 | embarcar a tempo |
| 11 | nina_prancha | Nina leva a prancha de surfe | Costa Azul | 3 | bagageira na bagagem |
| 12 | bolinha_cao | Bolinha, o cão, sai de férias | Costa Azul | 4 | agente na segurança |
| 13 | abilio_terra | Seu Abílio volta à terra natal | Costa Azul | 4 | passar na alimentação |
| 14 | kenji_conexao | Kenji faz conexão para o Japão | Aurora Global | 4 | embarcar a tempo |
| 15 | coral_lisboa | O coral da escola canta em Lisboa | Aurora Global | 4 | o voo sair sem perder ninguém |
| 16 | iara_pesquisa | Dra. Iara apresenta sua pesquisa | Aurora Global | 4 | vendedora na loja |
| 17 | mariana_intercambio | Mariana parte para o intercâmbio | Aurora Global | 4 | passar na alimentação |
| 18 | hiro_neta | Vô Hiro vai conhecer a netinha | Aurora Global | 4 | atendente no portão |
| 19 | banda_festival | A banda Sol Maior vai ao festival | Aurora Global | 4 | agente na segurança |

Em Aurora Global as rotas grátis são de conexão (o passageiro entra pela segurança): nenhuma história de lá pede check-in ou bagagem. Temas: família (0, 4, 13, 18), trabalho e estudo (1, 16, 17), férias (8, 10, 11, 12), pet na caixa de transporte (2, 12), primeira viagem (7, 10), reencontro (6, 13), esporte e música (5, 9, 15, 19), conexão internacional (14). Sem drama pesado; público 13+.

## 3. Tela

- **Boneco:** o coração sobre quem tem a história (`Pax.Historia`, `Mundo.Pessoas.cs`).
- **Quadro de partidas:** na linha do voo dele, a nota com o coração alterna o título e o pedido a cada 3 s enquanto o pedido falta (`Textos.HistoriaNota`).
- **Ficha ao tocar:** o título da história no lugar do voo e o pedido no lugar do humor (`Hud.Ficha.cs`).
- **Álbum** (Pausa → Álbum, `-tela album`): as 20, com estrela e "Entregue: …" para as entregues, coração e "Pede: …" para as que já apareceram e cadeado, "História ainda não vista" e onde aparece (aeroporto e nível) para as outras.

## 4. Alcance (portão `BalanceTests.Portao_Historias`, pelo CI)

Os bots `Contador` e `LentoContador` (o Completo e o LentoCompleto que levam o perfil pedido à estação antes de o passageiro passar e só trocam de aeroporto com o álbum dele cheio) fecham as 20 em 200 de 200 sementes, em até `OndasAlcance` (120) ondas. Nenhum compra nem assiste anúncio. Números no corpo do PR.

## 5. Critério de pronto em 10 itens (GDD §0.5)

| # | Critério | Estado |
|---|---|---|
| 1 | Regra de gameplay implementada | feito: `Sim.Historias.cs` |
| 2 | Tela de consulta ou de controle | feito: álbum com as 20, ficha do passageiro e nota no quadro |
| 3 | Representação visual coerente | feito: coração no boneco certo e na linha do voo dele; estrela, coração e cadeado no álbum |
| 4 | Animação ou retorno visual | parcial: aviso "História entregue!" e o coração some com quem embarcou; sem animação própria de entrega |
| 5 | Requisitos e custos definidos | feito: §1 e §2 (sem custo; nada pago) |
| 6 | Integração com as estações e os departamentos | feito: paradas opcionais, perfis por estação, voo e portão; campanha e missões contam as entregas |
| 7 | Estado salvo e restaurado | feito: schema 16 (`hp=` história da onda, passageiro, passo e as que já saíram), migração do save real do schema 12 |
| 8 | Testes das regras principais | feito: `HistoriasTests` (13), `Portao_Historias` (200 sementes, pelo CI), prova de mutação local 11 de 11 |
| 9 | Nenhum bloqueio de navegação conhecido | a conferir na rodada da leva: Pausa → Álbum → fechar volta à Pausa |
| 10 | Validação em build e no Android | na rodada de integração da leva, com o Unity do coordenador; o aparelho fica para o último passo (D-24) |
