#!/bin/bash
# Fotos 540x960 das 8 salas, de perto e de longe, e o estresse de 60 passageiros (depois do verify.sh full). Todas no -testsession.
# uso: client/tools/fotos_salas.sh <pasta de saida> [prefixo]   Git Bash no Windows. As fotos sao conferidas a mao; a saida fica fora do git.
set -uo pipefail
export MSYS_NO_PATHCONV=1
ROOT="$(cd "$(dirname "$0")/../.." && pwd -W)"; EXE="$ROOT/client/Builds/win/TinyAirport.exe"
OUT="${1:?pasta de saida}"; PRE="${2:-}"
[ -f "$EXE" ] || { echo "FALHOU: falta $EXE" >&2; exit 1; }
mkdir -p "$OUT"
falhas=0

foto() {
  local nome=$PRE$1; shift
  "$EXE" -testsession -screen-fullscreen 0 -screen-width 540 -screen-height 960 -logFile "$OUT/$nome.log" -shot "$OUT/$nome.png" "$@" > /dev/null 2>&1
  if [ -f "$OUT/$nome.png" ] && ! grep -q "Exception" "$OUT/$nome.log"; then echo "OK   $nome"; else echo "FALHOU $nome (ver $OUT/$nome.log)"; falhas=$((falhas + 1)); fi
}

for e in 0 1 2 3 4 5 6 7; do foto "sala$e" -onda 3 -bot -speed 6 -shotdelay 12 -cenario salas -vista "sala$e"; done
foto salas_jogo  -onda 3 -bot -speed 6 -shotdelay 12 -cenario salas
foto salas_longe -onda 3 -bot -speed 6 -shotdelay 12 -cenario salas -vista todas
foto inicio      -onda 1 -bot -speed 4 -shotdelay 6
foto geral       -onda 3 -bot -speed 6 -shotdelay 14 -cenario salas -vista geral
foto patio       -bot -speed 6 -shotdelay 12 -vista patio
foto estresse_60 -passageiros 60 -speed 10 -shotdelay 11 -vista sala0
foto estresse_60_longe -passageiros 60 -speed 10 -shotdelay 11
[ $falhas -eq 0 ] && echo "fotos: OK em $OUT" || { echo "fotos: $falhas falhas" >&2; exit 1; }
