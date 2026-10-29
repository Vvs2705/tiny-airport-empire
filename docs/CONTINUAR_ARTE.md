# CONTINUAR DAQUI — troca do greybox pela arte do Tripo (branch `feat/view-tripo`, 2026-10-29)

- **Feito:** View em 3 partes (`Mundo.cs`, `Mundo.Pessoas.cs`, `Mundo.Patio.cs`), `SpriteSheet.cs` + shader `TA/SpriteTinta`, `MalhaImport.cs`, `-vista` e `-perf`; 32 malhas decimadas em `client/Assets/_TA/ArtLocal/` (fora do git, decisão A); folhas de 11 atores em `Resources/Sprites` (no git); docs GDD §10.2, MAPA, ART_BIBLE, CONTRATO_V01 §1/§7 e PROVENIENCIA §5.
- **Falta 1:** mecânico e despachante (o `elenco_ta.py` saiu sem log nem `.blend` duas vezes; tentar de novo sozinho, com RAM livre). A View já pula o ator do pátio sem folha.
- **Falta 2:** abrir o Unity uma vez (gera os `.meta` novos e os de `Art/Marca`; commitar todos), `client/tools/verify.sh full`, conferir a 1ª foto (o giro das malhas no Unity ainda não foi visto: ajustar o yaw em `Peca` se a frente vier trocada).
- **Falta 3:** `client/tools/fotos_v01.sh` (fotos 540×960, `perf_60.txt` do K3 e o GIF `mover.gif` do avião chegando com a Ana trocando de uniforme), mandar o GIF ao Vinicius, pôr o K3 e o estado no CONTRATO_V01 §8 e a tabela de SHA das folhas no PROVENIENCIA §5.3.
- **Falta 4:** tirar o "wip" do commit, abrir o PR na `main` e mesclar com o CI verde. Depois de mesclar o #12, ligar o pátio em `sim.Avioes`, `Ev.Pousou` e `Ev.Decolou` (follow-up).
- **Próximo comando** (sem outro Blender ou Unity aberto): `bash client/tools/verify.sh full && bash client/tools/fotos_v01.sh`
