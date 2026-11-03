# Passe de temporada

Fonte: `GDD.md` §6.1 e §6.4. Regras no `CONTRATO_FASE_C7.md`. A tabela do código é `Tabelas.Temporadas` (`Sim.Passe.cs`). Um teste compara esta página com ela linha a linha: se mudar aqui, muda lá no mesmo commit.

Como ler:

- **A temporada corre em dias do jogo:** 28 dias de 8 ondas, ou 11,2 h jogadas. Com o jogo fechado nada anda, e faltar um dia não existe (D-C7a do contrato).
- **Ponto de temporada é barra, não moeda.** Não se compra nem se gasta, e não vem de anúncio, de Gems, de compra nem do cofre offline.
- **De onde vem o ponto:** voo que partiu, 2 pontos, com ou sem gente a bordo, porque perder o voo custa só a receita dele. Diária feita, 25. Semanal feita, 70. Objetivo da campanha, 20.
- **100 pontos por nível**, 3 000 para fechar os 30.
- **O nível paga sozinho** no passo em que fecha. Não há o que resgatar nem prazo, e o que foi pago fica depois do fim da temporada.
- **Tempo** são minutos de 2× renda, somados ao 2× que já estiver ligado.
- **Cosmético** fica para sempre (`sim.Tem(id)`) e não se vende na loja.
- **Trilha premium:** produto `season_pass_s1`, ainda sem conteúdo. Ela depende da loja (linha 14 do `CONTRATO_LANCAMENTO.md`).

## Temporada 1 (`s1`): trilha gratuita

| Nível | Prêmio | Pontos | O que é |
|---|---|---|---|
| 1 | 200 Cash | 100 | |
| 2 | 5 min de 2× | 200 | |
| 3 | 300 Cash | 300 | |
| 4 | 400 Cash | 400 | |
| 5 | 10 min de 2× | 500 | |
| 6 | 500 Cash | 600 | |
| 7 | 600 Cash | 700 | |
| 8 | 10 min de 2× | 800 | |
| 9 | 800 Cash | 900 | |
| 10 | `terminal_decor_flags_s1` | 1000 | bandeirolas da temporada no saguão |
| 11 | 1000 Cash | 1100 | |
| 12 | 10 min de 2× | 1200 | |
| 13 | 1200 Cash | 1300 | |
| 14 | 1400 Cash | 1400 | |
| 15 | 15 min de 2× | 1500 | |
| 16 | 1600 Cash | 1600 | |
| 17 | 1800 Cash | 1700 | |
| 18 | 15 min de 2× | 1800 | |
| 19 | 2000 Cash | 1900 | |
| 20 | `aircraft_skin_sunset_s1` | 2000 | pintura de avião "pôr do sol" |
| 21 | 2200 Cash | 2100 | |
| 22 | 15 min de 2× | 2200 | |
| 23 | 2400 Cash | 2300 | |
| 24 | 2600 Cash | 2400 | |
| 25 | 15 min de 2× | 2500 | |
| 26 | 2800 Cash | 2600 | |
| 27 | 3000 Cash | 2700 | |
| 28 | 15 min de 2× | 2800 | |
| 29 | 3500 Cash | 2900 | |
| 30 | `aircraft_skin_gold_s1` | 3000 | pintura de avião dourada da temporada 1 |

**Total:** 28 300 Cash em 18 níveis, 110 min de 2× em 9 e 3 cosméticos. Os 3 cosméticos pedem arte. A tela mostra a imagem real antes de o jogador chegar ao nível, como a loja (GDD §7.5).

## Fim da temporada

- No 1º passo do dia 29 do jogo a barra zera e a temporada seguinte da tabela abre, com os 28 dias inteiros.
- Sem temporada seguinte na tabela, o passe fica fechado e o jogo segue igual. A temporada que chegar numa atualização abre no dia em que chegar, nunca no passado.
- Um save de antes do passe (schema 8) abre a temporada 1 no dia do jogo em que foi aberto, com 0 pontos.
