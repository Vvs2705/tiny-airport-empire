# Contrato da Fase C9: contratação com 3 candidatos e demissão (2026-11-03)

Fonte da verdade: `GDD.md` §0.5 (regras de direção), §4.1 (frente D), §5.5 (os 6 perfis fixos, TA-5), §5.13 (portão de design) e §7.1 (nada aleatório pago), e o §9.2 do GDD de direção v0.1 (`GDD_DIRECAO_v0.1.md`): "o jogador consulta candidatos, compara custo e qualificações, contrata e aloca; desligamentos atualizam escalas, custos e cobertura de postos". Continua o `CONTRATO_FASE_C8.md` (despesas): a folha de lá passa a ser a do candidato contratado. Fica fora daqui o que o §9 do GDD de direção traz e o lançamento não tem: qualificações e treinamento, turnos, fadiga e supervisores (roteiro do GDD §4.4).

Todo número é **hipótese**: o bot mede e o `BALANCE.md` registra.

## 1. O que entra

- **3 candidatos por vaga.** Vaga = perfil aberto pelo marco e ainda sem ninguém. Os candidatos são o **Júnior**, o **Pleno** e o **Sênior** daquele perfil, com nome próprio.
- **Contratar um**, vendo antes o nome, o perfil, o salário por dia, o rendimento e onde trabalha melhor.
- **Demitir**, com confirmação. A pessoa sai na hora e a vaga reabre com os mesmos 3.
- **A escala, a folha e a cobertura do posto se atualizam sozinhas**, porque `Folha`, `Presentes`, `Alocados` e `Move` leem o `Staff`.

## 2. Regras

**D-C9a. Os 3 candidatos convivem com os 6 perfis fixos assim:** o perfil continua sendo a vaga, um por pessoa, aberto pelo marco (TA-5). Dentro de cada vaga há 3 pessoas fixas, que diferem só na experiência. O Pleno é a pessoa de antes (Ana, Beto, Cida, Dani, Edu e Fátima), com o preço e o salário de antes. A lista é uma tabela: não sai da semente, do relógio nem de sorteio, e nenhuma moeda, Gems ou compra a troca ou renova (§7.1). Por isso a semente ficou de fora: tirar a experiência da semente seria um traço sorteado, e o TA-5 proíbe.

| Experiência | Rende | Contratação | Salário por dia |
|---|---|---|---|
| Júnior | 80% | 50% | 70% |
| Pleno | 100% | 100% | 100% |
| Sênior | 125% | 200% | 150% |

Em % do Pleno (`Tuning.ExpRende`, `ExpContratacao`, `ExpSalario`), sobre `Tuning.Contratacao` e `Tuning.Salario` do perfil. A conta é em inteiro, com o meio arredondado para cima: o preço não depende de float.

| Perfil | Júnior | Pleno | Sênior | Contratação J / P / S | Salário J / P / S |
|---|---|---|---|---|---|
| Atendente | Lia | Ana | Vânia | 10 / 20 / 40 | 25 / 35 / 53 |
| Agente de segurança | Téo | Beto | Hélio | 75 / 150 / 300 | 35 / 50 / 75 |
| Vendedora | Bia | Cida | Vera | 200 / 400 / 800 | 35 / 50 / 75 |
| Bagageira | Duda | Dani | Neide | 150 / 300 / 600 | 42 / 60 / 90 |
| Mecânico | Igor | Edu | Paulo | 225 / 450 / 900 | 63 / 90 / 135 |
| Despachante | Júlia | Fátima | Sônia | 225 / 450 / 900 | 63 / 90 / 135 |

| Regra | Como funciona |
|---|---|
| Rendimento | Multiplica o da pessoa como o especialista já fazia: tempo de atendimento ÷ x nas filas, venda × x na loja e na alimentação, força × x na oficina e no despacho. A estação rende pelo melhor presente; sem ninguém, o toque rende 1 |
| Ordem | As vagas abertas contratam em qualquer ordem. O botão de contratar do HUD (`Hire()`, o tutorial) continua chamando o Pleno, na ordem da v0.1 |
| Conta atrasada | Contratar pausa com a conta atrasada (`PodeComprar`, Fase C8). **Demitir não pausa**: é a saída para baixar a folha |
| Demitir | Sai na hora, sem custo e sem devolver a contratação. Quem estava sendo atendido por ele termina. Quem vinha depois no `Staff` sobe uma posição (`Id` = índice). A vaga reabre com os mesmos 3 |
| Regra desligada | `Tuning.RegraExperiencia = false`: só o nome muda, e os 3 rendem e custam como o Pleno |

**D-C9b. Demitir é de graça.** Uma multa de rescisão faria o jogador no vermelho pagar para sair do vermelho, e a regra 6 do §0.5 manda o contrário. O custo de demitir já existe: a contratação paga não volta, e trocar alguém por um Sênior paga o Sênior inteiro. Contratar e demitir em laço só gasta Cash (invariante 2).

**D-C9c. O Sênior quase sempre compensa; a decisão é de caixa.** A sonda com 40 sementes (`BALANCE.md` §16) mostrou que o rendimento pesa muito mais que o salário: equipe toda Sênior, pagando o dobro e 1,5× por dia, lucra 13% a mais com o Bom e 27% com o Lento; toda Júnior lucra até 39% a menos. O Júnior serve para pôr gente no posto cedo, com pouco caixa; a demissão é a troca por alguém melhor quando sobra. Se o playtest achar o Sênior óbvio demais, o ajuste é no preço (`ExpContratacao`), não na lista.

## 3. Núcleo e API

- Determinístico, passo fixo de 1/30 s. Nada sai de sorteio.
- Ler: `sim.CustoContratar(p, x)` (o preço; -1 = vaga fechada), `sim.Preco(p, x)` (o preço de tabela, com a vaga aberta ou não), `sim.Salario(p, x)`, `sim.Rendimento(x)`, `Funcionario.Experiencia`.
- Agir: `sim.Hire(p, x)` e `sim.Demite(k)`. `Hire(p)` e `Hire()` contratam o Pleno.
- Evento novo no fim do `Ev`: `Demitido` (A = a posição que tinha no `Staff`, B = perfil, C = experiência) → `staff_fired` no diário.

## 4. Invariantes (testes no `EquipeTests`)

1. **A lista é fixa:** igual com qualquer semente, depois do relógio andar e depois de creditar e gastar Gems.
2. **Contratar e demitir nunca dá Cash:** 50 voltas custam a soma das contratações.
3. **Sem derrota inevitável:** demitir todo mundo no meio do jogo volta ao começo, no toque; todo voo sai, o terminal continua embarcando e o bot volta a contratar.
4. **Demitir tira da pausa:** com a conta atrasada, demitir vale, a folha cai e o fechamento seguinte, sem a equipe, cabe.
5. **Fechar e abrir no meio da onda dá o mesmo jogo,** com 3 experiências e uma demissão.
6. **Nenhum candidato domina na tabela:** quem rende mais custa mais, na contratação e por dia.

## 5. Save

**Schema 12** (combinado com o coordenador em 04/11: o 11 fica para o histórico, PR #60). `func.<i>` ganha o 5º campo, a experiência: `em,de,andando,perfil,experiencia`. Os schemas 10 e 11 não têm o campo: todos viram o Pleno, com a folha de antes. Lixo (fora de 0 a 2) também vira o Pleno. O teto do atendimento gravado passou a ser o do candidato mais lento (`TempoTeto`), porque um Júnior pode ter saído no meio.

A cadeia 10 → 11 → 12 tem teste nos dois elos, cada um com um save real (semente 7, bot Bom, 2 790 s, os 6 perfis contratados): o do schema 10 gravado pelo commit 03c40bd, e o do schema 11 pelo 96a6e40 (a `main` com o registro de ocorrências). Nos dois, todos viram o Pleno, a folha é a de antes e migrar dá o mesmo jogo que jogar até ali com o código novo, número a número (sem o vestiário da frente E, como o jogo que esses schemas gravavam); o do 11 traz o registro de volta. Os saves dos schemas 1 a 9 continuam migrando pelo mesmo caminho.

## 6. Bot e portão

- **Começo intacto:** os bots de sempre contratam o Pleno e nunca demitem, então todos os portões anteriores repetem a tabela deles (o Pleno é o caminho de antes, e a migração do schema 10 prova).
- **Bot `Contratador` e `LentoContratador`:** as 6 vagas entram na conta do mais barato pelo preço do Júnior, em qualquer ordem; ao contratar, levam o melhor que o caixa paga (o Sênior direto quando cobre); e trocam quem não é Sênior pelo Sênior (demite e contrata), com a troca pesando metade do preço na conta do mais barato, porque o Sênior rende 25% na estação toda, cerca do dobro de um nível de velocidade (D-C9d, `BALANCE.md` §16).
- **Portão do §5.13 (`Portao_FaseC9_Candidatos`):** o lucro (receita − conta paga) do `Contratador` ≥ 1,10 × o do Bom, e o do `LentoContratador` ≥ 1,10 × o do Lento, 200 sementes × 20 ondas. Abaixo disso a experiência fica só visual (`RegraExperiencia = false`). Com 200 sementes e o vestiário da frente E: **1,118 e 1,414**. O CI roda as 200 na parte "resto".

## 7. Tela

- **Equipe:** cada vaga aberta mostra o perfil, onde trabalha melhor e o botão **Candidatos**. Quem foi contratado mostra o nome, a experiência, o perfil, o salário por dia e onde está, com o botão **Mover**. Sumiu o "contrate antes": as vagas abrem em qualquer ordem.
- **Vaga** (folha nova, `Hud.Operacao.cs`): o total da equipe por dia no alto e os 3 candidatos, cada um com nome, experiência, salário por dia, rendimento ("Rende 20% menos", "Rende o normal", "Rende 25% mais") e onde trabalha melhor, com o preço no botão. Com a conta atrasada, o botão mostra o cadeado e quanto falta.
- **Demitir** (cartão no fim do Mover): nome, perfil, experiência e salário. O 1º toque arma, o cartão explica o que acontece e o botão vira **Confirmar**, em vermelho. Ao demitir, aparece o aviso "Fulana saiu da equipe".
- **Mundo:** cada pessoa usa o boneco do perfil dela, também contratando fora de ordem e depois de uma demissão, e o nome em cima é o do candidato. Júnior, Pleno e Sênior do mesmo perfil usam o mesmo boneco.
- Flags para as fotos: `-tela vaga` (a 1ª vaga aberta) e `-tela demitir` (o Mover do 1º da equipe, com a confirmação armada).

## 8. Critério de pronto em 10 itens (GDD §0.5)

| # | Critério | Estado |
|---|---|---|
| 1 | Regra de gameplay implementada | feito: `Sim.Equipe.cs` |
| 2 | Tela de consulta ou de controle | feito: Equipe, Vaga e o cartão Demitir |
| 3 | Representação visual coerente | parcial: boneco e nome certos por pessoa; os 3 candidatos do perfil usam o mesmo boneco (arte própria fica para depois) |
| 4 | Animação ou retorno visual | parcial: o contratado aparece de uniforme no posto e a demissão mostra o aviso; quem sai some na hora, sem andar até a porta |
| 5 | Requisitos e custos definidos | feito: §2 |
| 6 | Integração com as estações e os departamentos | feito: rendimento nas 8 estações, folha da frente A, cobertura do posto, escala e Mover |
| 7 | Estado salvo e restaurado | feito: schema 12, migração dos saves reais dos schemas 10 e 11 |
| 8 | Testes das regras principais | feito: `EquipeTests` (11), `SaveTests` (migração do 10 e do 11, lixo), prova de mutação 13 de 13 |
| 9 | Nenhum bloqueio de navegação conhecido | a conferir na rodada da leva: Equipe → Vaga → Contratar volta à Equipe; Mover → Demitir → Confirmar fecha a folha |
| 10 | Validação em build e no Android | na rodada de integração da leva, com o Unity do coordenador; o aparelho fica para o último passo (D-24) |
