#!/bin/bash
# As 8 capturas cruas da ficha da Google Play (docs/FICHA_PLAY.md §5), em 1080x1920: janela 540x960 com -shotescala 2. Todas no -testsession.
# uso: client/tools/fotos_play.sh <pasta de saida>   Git Bash no Windows, depois do verify.sh full e com as malhas do Tripo no lugar.
# Depois: python client/tools/ficha_play.py capturas <pasta> <pasta>/final. A saida mostra o Tripo: fica fora do git (client/Builds/).
set -uo pipefail
export MSYS_NO_PATHCONV=1
ROOT="$(cd "$(dirname "$0")/../.." && pwd -W)"; EXE="$ROOT/client/Builds/win/TinyAirport.exe"
OUT="${1:?pasta de saida}"
[ -f "$EXE" ] || { echo "FALHOU: falta $EXE" >&2; exit 1; }
[ -d "$ROOT/client/Assets/_TA/ArtLocal" ] || echo "AVISO: sem client/Assets/_TA/ArtLocal (malhas do Tripo): as capturas saem com primitivas e NAO valem para a loja" >&2
mkdir -p "$OUT"
falhas=0

foto() {
  local nome=$1; shift
  "$EXE" -testsession -screen-fullscreen 0 -screen-width 540 -screen-height 960 -shotescala 2 -logFile "$OUT/$nome.log" -shot "$OUT/$nome.png" "$@" > /dev/null 2>&1
  if [ -f "$OUT/$nome.png" ] && ! grep -q "Exception" "$OUT/$nome.log"; then echo "OK   $nome"; else echo "FALHOU $nome (ver $OUT/$nome.log)"; falhas=$((falhas + 1)); fi
}

foto 1_geral     -onda 3 -bot -speed 6 -shotdelay 14 -cenario salas -vista geral
foto 2_equipe    -onda 3 -bot -speed 6 -shotdelay 12 -tela equipe
foto 3_ficha     -onda 3 -bot -speed 6 -shotdelay 12 -tela fichapax
foto 4_patio     -onda 3 -bot -speed 6 -shotdelay 12 -vista patio
foto 5_rotas     -onda 3 -bot -speed 6 -shotdelay 12 -tela rotas
foto 6_metas     -onda 3 -bot -speed 6 -shotdelay 12 -tela metas
foto 7_melhorias -onda 3 -bot -speed 6 -shotdelay 12 -tela melhorias
foto 8_passe     -onda 3 -bot -speed 6 -shotdelay 12 -tela passe
[ $falhas -eq 0 ] && echo "fotos da Play: OK em $OUT" || { echo "fotos da Play: $falhas falhas" >&2; exit 1; }
