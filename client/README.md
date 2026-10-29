# client/ — projeto Unity de Tiny Airport Empire

Projeto Unity **6000.3.27f1** (a versão do Dungeon Courier; GDD §3.2). `client/` é a raiz do projeto: `Assets/`, `Packages/`, `ProjectSettings/`.

- Código só em `Assets/_TA/`: núcleo em C# puro em `Scripts/Core/` (`TA.Core`), View em `Scripts/View/` (`TA`), `Editor/` (`TA.EditorTools`) e testes em `Tests/EditMode/`, que rodam também fora do Unity por `tools/coretests`. Mapa de cada arquivo: `docs/MAPA_CODIGO.md`.
- `Editor/Setup.cs` aplica os PlayerSettings e expõe `Apply`, `BuildWindows` e `BuildAndroidDev` por linha de comando. O appId é provisório, só de desenvolvimento, até o D-03.
- Portão antes de todo PR de jogo, da raiz do repositório:

```bash
client/tools/verify.sh full
```

O `quick` roda só o núcleo e o viewcheck, sem abrir o Unity. Num worktree de caminho longo, rode por um drive `subst` se o Unity falhar por MAX_PATH.
