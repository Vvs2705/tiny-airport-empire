# GO/NO-GO do lançamento (rascunho de 2026-11-06)

Base: `CONTRATO_LANCAMENTO.md` §1 e GDD §25. Rodada local na `main` `18e44a7` mais o PR #90 (conta UGS), no PC do Vinicius, com as malhas do Tripo.

**Veredito de hoje: NO-GO.** Nenhum bloqueio é de código grande; os que travam são externos (D-U-N-S e Play Console) ou medições que ainda não fecharam.

## 1. Regressão de 2026-11-06

| Portão | Resultado | Evidência |
|---|---|---|
| `verify.sh quick` | OK, 457 testes do núcleo | PR #90 |
| `verify.sh full` | OK: EditMode 522 de 557 (35 `[Explicit]`, que o CI roda), build Windows, autoplay de 10 ondas (Bom 94,1%, Lento 75,7%, ganho de mover 1,892) | rodada 2 |
| `verify.sh android` | OK: APK de dev, targetSdk 36, ARM64, sem `AD_ID` | rodada 2 |
| K3 no PC (540×960) | p10 58,4 a 1x (74 pessoas) e 58,6 a 3x | rodada 2 |
| `diag_caixas` (5 cenários) | **falha**: 253 cruzamentos a 1x, 376 a 3x, 129 a 3x com `-speed 2`; quase todos de um funcionário parado na parede sul do portão depois do vestiário | rodada 2, devolvido à frente do portão |
| Fotos das 8 salas, geral, fichas, metas e pátio | sem exceção | rodada 2 |
| Conta (UGS) no Editor | compila com `VS_UGS`; falta medir ligar, trocar de conta e excluir (`CONTAS.md` §6.5) | PR #90 |

## 2. O que trava o GO

| Item | Quem | Situação |
|---|---|---|
| Play Console (D-U-N-S, previsão 25/11) | Vinicius | sem ela não há envio, produtos, testadores, Play Games nem Data safety |
| `diag_caixas` em zero | frente do portão | funcionário encostado na parede sul do portão |
| Play Games e Facebook no projeto | Vinicius (download) e Claude | SDKs oficiais baixados à mão; depois `full` e `android` de novo |
| Painéis da UGS e do Facebook | Vinicius (login) e Claude | Authentication, Cloud Save e os 2 provedores |
| Loja (Unity IAP) e anúncio (LevelPlay) no build | Claude, rodada do PC | pacotes fora do manifest (`LOJA_SERVIDOR.md` §5) |
| K2 com 20 pessoas e teste no POCO F4 | Vinicius e amigos | último passo |
| AAB assinado | Claude | `Setup.BuildAndroidRelease` pronto; a chave de upload foi trocada em 06/11 (`NOME.md`) |
| Revisão jurídica da política (D-10) | sem responsável | — |
| Linhas parciais do contrato (A a I, S, P, 1 a 5, 9a, 10, 14, 15, 18, 21, 22, 23) | frentes | cada uma fecha com o critério de 10 itens |

## 3. Como vira GO

Toda linha do contrato **feita** ou **dependendo de alguém** com nome; `full`, `android` e `diag_caixas` verdes na mesma `main`; o AAB assinado aceito na faixa de teste interno da Play; K2 e o teste no aparelho feitos.
