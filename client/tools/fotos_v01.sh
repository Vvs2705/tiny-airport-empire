#!/bin/bash
# Fotos 540x960 da v0.1 (CONTRATO_V01 §8 linhas 7 e 8): cada tela, o estresse com 60 passageiros (K3) e cada passo do tutorial, mais uma
# rodada -tutorial -bot que loga os passos concluidos (TOUR ... no log). Todas no -testsession (sem save, sem diario, semente 1).
# uso: client/tools/fotos_v01.sh   (depois do verify.sh full, que gera client/Builds/win/TinyAirport.exe). Git Bash no Windows.
# Saida: client/Builds/validation_v01/<nome>.png e <nome>.log (fora do git). As fotos sao conferidas a mao.
set -uo pipefail
export MSYS_NO_PATHCONV=1
ROOT="$(cd "$(dirname "$0")/../.." && pwd -W)"; EXE="$ROOT/client/Builds/win/TinyAirport.exe"; OUT="$ROOT/client/Builds/validation_v01"
[ -f "$EXE" ] || { echo "FALHOU: falta $EXE (rode client/tools/verify.sh full)" >&2; exit 1; }
mkdir -p "$OUT"; rm -f "$OUT"/*.png "$OUT"/*.log
falhas=0

foto() {
  local nome=$1; shift
  "$EXE" -testsession -screen-fullscreen 0 -screen-width 540 -screen-height 960 -logFile "$OUT/$nome.log" -shot "$OUT/$nome.png" "$@" > /dev/null 2>&1
  if [ -f "$OUT/$nome.png" ] && ! grep -q "Exception" "$OUT/$nome.log"; then echo "OK   $nome"; else echo "FALHOU $nome (ver $OUT/$nome.log)"; falhas=$((falhas + 1)); fi
}

foto jogo_onda1       -bot -speed 4 -shotdelay 6
foto jogo_onda3_bot   -onda 3 -bot -speed 8 -shotdelay 12
foto critico_onda4    -onda 4 -speed 6 -shotdelay 12
foto estresse_60      -passageiros 60 -speed 10 -shotdelay 11
foto estresse_60_bot  -passageiros 60 -bot -speed 6 -shotdelay 12
foto melhorias        -onda 3 -bot -speed 8 -shotdelay 10 -tela melhorias
foto mover            -onda 2 -bot -speed 8 -shotdelay 10 -tela mover
foto cofre            -shotdelay 2 -tela cofre
foto anuncio          -onda 2 -shotdelay 2 -tela anuncio
foto pausa            -bot -speed 4 -shotdelay 4 -tela pausa
foto album            -shotdelay 2 -tela album
for n in 1 2 3 4 5 6 7 8 9; do foto "passo$n" -tutorial -bot -speed 8 -shotdelay 12 -tela "passo$n"; done

# tutorial jogavel: o bot joga com o tutorial ligado (ENTENDI sozinho em 2 s); o log diz quais passos apareceram e terminaram
foto tutorial_bot -tutorial -bot -speed 10 -shotdelay 120
grep -o "TOUR .*" "$OUT/tutorial_bot.log" | tr -d '\r' > "$OUT/tutorial_bot.txt"
cat "$OUT/tutorial_bot.txt"

# -record: a Ana sai do check-in, anda pelo corredor e chega na loja com o colete trocado; folha de contato e GIF para o PR
rm -rf "$OUT/record_mover"
"$EXE" -testsession -screen-fullscreen 0 -screen-width 540 -screen-height 960 -logFile "$OUT/record_mover.log" -demo -record "$OUT/record_mover" -recordseg 6 > /dev/null 2>&1
python "$ROOT/client/tools/montagem.py" "$OUT/record_mover" --saida "$OUT/mover" || falhas=$((falhas + 1))
[ $falhas -eq 0 ] && echo "fotos: OK em $OUT" || { echo "fotos: $falhas falhas" >&2; exit 1; }
