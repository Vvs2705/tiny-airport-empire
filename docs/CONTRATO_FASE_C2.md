# Contrato da Fase C2: as 8 estações e os 6 perfis no núcleo (2026-10-29)

> **CONTINUAR DAQUI (2026-10-29, parada pelo limite de uso; branch `claude/fase-c-estacoes-perfis-ada843`, commit WIP sem PR)**
> - Feito no núcleo: 8 estações (enum + `Sim.Estacoes.cs`), 6 perfis (`Hire(perfil)`), desbloqueio pelo marco, save schema 3 com migração do save real do schema 2, bot com `Equipe`, testes (83 verdes: `EstacoesPerfisTests`, 4 invariantes C2 no `ExploitTests`, migração no `SaveTests`). `Stations` continua com as 4 da v0.1 para a View.
> - Medido (100 sementes): especialização ligada derruba o TA-2 (1,05–1,07) e o portão dela dá 1,00 → `RegraEspecialista = false`. Desligada: Bom 99,7%, Lento 75,5%, TA-2 1,408. Por estação: bagagem 1,02, alimentação 1,06, oficina 1,04, despacho 1,05; as 4 juntas ainda sem medida (o `Portao_FaseC2_PorSistema` foi interrompido).
> - Próximo comando: `dotnet test client/tools/coretests --filter "FullyQualifiedName~BalanceTests.Portao_|FullyQualifiedName~BalanceTests.Antes_FaseC2" --logger "console;verbosity=detailed"` (~10 min). Se "estacoes" der < 1,10, desligue as 4 chaves `Regra*` das estações e meça de novo o TA-2.
> - Depois: `BALANCE.md` §6 (tabelas), linhas 4 e 5 do `CONTRATO_LANCAMENTO.md`, `CHANGELOG.md`, `verify.sh full` (pedir à sessão da View para fechar o Blender), `sync_with_base_branch`, PR com "Para a View" (ler `sim.Estacoes`, arrays por `Sim.MaxEquipe`, nomes de Dani/Edu/Fátima, `Pax.Total` no anel, `Hire(perfil)`) e merge.

Fonte da verdade: `GDD.md` §4.1 (8 estações e 6 perfis), §5.5 (perfis), §5.6 (estações), §5.12 (manutenção), §5.13 (portão de design), §14.2 (migração) e `CONTRATO_LANCAMENTO.md` linhas 4 e 5. Continua o `CONTRATO_FASE_C.md`: a tripulação, o pátio e a manutenção de lá não mudam de regra.

Todo número é **hipótese**: o bot mede e o `BALANCE.md` §6 registra.

**Medido em 2026-10-29 (`BALANCE.md` §6):** as 4 estações novas passaram no portão do §5.13 como um sistema (§5) e ficam com regra; a **especialização ficou só visual** (`Tuning.RegraEspecialista = false`, D-C2a): ganho 1,00 e, ligada, derruba o TA-2. O contratado aparece no 1º posto vazio, como na v0.1 (D-C2b). Mudou regra ou número? Atualize aqui e no `BALANCE.md` no mesmo commit.

## 1. O que entra

- **Estações (enum `Estacao`, só cresce no fim):** as 4 da v0.1 (check-in, segurança, loja, portão) + **bagagem, alimentação, manutenção (oficina) e despacho**. O GDD lista a loja entre as 4 novas, mas ela já estava na v0.1: as 4 novas são as que faltavam.
- **Perfis (enum `Perfil`):** 6 pessoas fixas, uma por perfil, na ordem de contratação. As 3 da v0.1 (Ana, Beto e Caio) viram Atendente, Agente de segurança e Vendedor; as novas são Dani (Bagageira), Edu (Mecânico) e Fátima (Despachante).
- **Por desbloqueio (marcos do terminal, sem sorteio nem compra):** antes do marco 1 o jogo é o da v0.1 com a Fase C, igual para o bot e para a View.

| Marco | Desbloqueia |
|---|---|
| 1 (150 embarcados) | especialização dos perfis; estações de bagagem e alimentação; contratar a Dani |
| 2 (600 embarcados) | oficina e despacho; contratar o Edu e a Fátima |

## 2. Regras

| Estação | O que faz | Números iniciais |
|---|---|---|
| Bagagem | entre o check-in e a segurança; vai quem tem mala despachada (semente) e folga no voo; paga a taxa da mala | 35% com mala, 3 s, taxa 5, folga ≥ 40 s, 1 balcão |
| Alimentação | entre a loja e o portão; vai quem tem fome (semente) e folga; paga a refeição | 50% com fome, 4 s, refeição 8, folga ≥ 40 s, 1 balcão |
| Manutenção (oficina) | quem está lá ajuda o mecânico da Fase C: a checagem e a inspeção ficam mais rápidas | tempo ÷ (1 + força dos presentes), 1 posto |
| Despacho | com gente, o voo que sai na hora paga bônus de pontualidade maior | +25% × força dos presentes, 1 posto, sem upgrades |

- Bagagem e alimentação são como a loja da v0.1: sem ninguém, ficam fechadas e o passageiro passa direto (a fila segue). Não aceitam toque.
- Oficina e despacho não têm fila de passageiros: o estado delas é sempre normal.
- **Especialista** (a partir do marco 1; **só visual no padrão**, D-C2a) rende **1,5×** na estação dele: tempo ÷ 1,5 (check-in e portão: Atendente; segurança: Agente; bagagem: Bagageira); venda × 1,5 (loja e alimentação: Vendedor; GDD "aumenta o gasto por passageiro"); força 1,5 na oficina (Mecânico) e no despacho (Despachante). Os outros rendem 1. Quem trabalha em qualquer estação continua podendo trabalhar em todas (a equipe é móvel, TA-2).
- **Contratação:** `Hire()` continua contratando os 3 da v0.1 na ordem (20, 150, 400). `Hire(perfil)` contrata Dani, Edu e Fátima depois do marco deles, por preço fixo (300, 450, 450). Contratado aparece no 1º posto vazio (check-in, segurança, portão, loja), como na v0.1: o núcleo não escolhe a estação pelo perfil, e levar a pessoa para uma estação nova é a decisão do jogador (D-C2b). A estação principal de cada perfil (`Funcionario.Casa`) é a do quadro acima; a do Vendedor é a alimentação, que vende mais que a loja.
- **Upgrades:** Velocidade e Capacidade nas estações novas como nas velhas (só abertas); na oficina, Velocidade encurta a checagem e a inspeção; o despacho não tem upgrade.
- **Conversa com a Fase C (sem regra duplicada):** a oficina é o mesmo mecânico da Fase C (`Sim.Patio.cs`): continua fazendo uma checagem por vez e a inspeção obrigatória sozinho; a equipe na oficina só divide o tempo. O despacho lê o "saiu na hora" da Fase C (todos a bordo e pátio pronto). A tripulação não muda.

## 3. Invariantes (testes negativos no `ExploitTests`)

1. Voo perdido custa só a própria receita, também com passageiro na bagagem ou na alimentação: nenhum Cash sai do saldo.
2. Offline nunca perde voo nem anda as estações novas.
3. Nada pago é aleatório: perfil, nome, ordem e preço da contratação são os mesmos em qualquer semente; a semente só decide quem tem mala ou fome, que não se compra.
4. Reparo só com Cash e tempo: a oficina com gente acelera, mas a checagem continua custando Cash, sem Cash não acontece, e anúncio nunca repara nem acelera.

## 4. Save

Schema 3. Grava as 8 estações (níveis, fila e atendimento) e o perfil de cada funcionário. O schema 2 migra: os funcionários ganham o perfil pela ordem (Atendente, Agente, Vendedor) e o resto abre como estava. Teste com um save real gravado pelo código do schema 2 (semente 7, 700 s): migrar dá o mesmo jogo que jogar até ali com o código novo.

## 5. Bot e portões

- **TA-1 e TA-2** (200 sementes × 10 ondas, desbloqueio pelo marco): Bom ≥ 85%, Lento ≥ 60%, Bom/Parado ≥ 1,10.
- **Portão do §5.13 por sistema com escolha** (200 sementes × 20 ondas, tudo desbloqueado desde o começo para o sistema aparecer na medida, um sistema ligado por vez). São 2 sistemas: a **especialização** (item 5: quem vai para onde) e as **4 estações novas** (item 4: uma decisão só, tirar gente do fluxo para uma estação nova). Cada estação sozinha também sai na tabela, sem portão próprio (D-C2c). **Planejador** = o Bom que leva cada um para a estação principal do perfil quando a atual está normal, manda gente à oficina quando um avião está pendente e só tira alguém de uma estação nova para cobrir crise em último caso; **Automático** = o mesmo Bom, com a equipe no padrão do núcleo (contratado no 1º posto vazio, depois só o mover para a estação crítica). Ganho < 1,10 = o sistema fica **só visual** (chave `Tuning.Regra*`), como a tripulação.
- O resultado medido vai para o `BALANCE.md` §6 e para a tabela abaixo.

## 6. Critério de pronto

| # | Critério | Como se mede |
|---|---|---|
| 1 | Este contrato e as linhas 4 e 5 do `CONTRATO_LANCAMENTO.md` | o PR |
| 2 | Regras do §2 com testes | `EstacoesPerfisTests` |
| 3 | Invariantes do §3 | `ExploitTests` |
| 4 | Save schema 3 com migração do save real do schema 2 | `SaveTests` |
| 5 | TA-1, TA-2 e os portões do §5 medidos | `BalanceTests` e `BALANCE.md` §6 |
| 6 | `MAPA_CODIGO.md`, cabeçalho do `Sim.cs` e `CHANGELOG.md` | o PR |
| 7 | `client/tools/verify.sh full` verde | saída no PR |

**Fica para a View:** desenhar as 4 salas novas, os 3 bonecos novos e o ícone do perfil; ler `sim.Estacoes` (as 8) no lugar de `sim.Stations` (as 4 da v0.1).
