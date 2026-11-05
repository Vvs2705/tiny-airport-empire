#!/bin/bash
# K3 automático (GDD §5.13, P-13): p10 >= 30 FPS com 60 pessoas, 2 aviões e 6 veículos de pátio, medido pelo -perf do build de
# desenvolvimento com o bot jogando a 3x (-vel 3) no enquadramento geral. Sem K3 com gente desde 2026-11-05: a medida é de máquina.
# uso: client/tools/k3.sh pc [seg]       o .exe do verify.sh full em 540x960 (o PC prende em 60 qps: confere a cena e pega queda grande)
#      client/tools/k3.sh android [seg]  o APK do verify.sh android no aparelho do adb (qualquer Android; o POCO F4 entra no último passo)
# seg = segundos medidos depois de 5 s de aquecimento (padrão 30). Imprime a linha "K3 OK|FAIL ..." e sai 0 só se passou.
# O log fica em client/Builds/verify/k3_<alvo>.log. Variáveis opcionais: K3_PERFMIN (piso do p10, padrão 30), ADB, ADB_SERIAL.
# Regra do PC (7,7 GB): um processo pesado por vez; não rode com o Unity, o Blender ou o emulador abertos.
# A parte do núcleo (custo e lixo por quadro na mesma cena) roda no quick: K3Tests.
set -uo pipefail
export MSYS_NO_PATHCONV=1

ROOT="$(cd "$(dirname "$0")/../.." && (pwd -W 2>/dev/null || pwd))"; CLIENT="$ROOT/client"; OUT="$CLIENT/Builds/verify"
ALVO="${1:-}"; SEG="${2:-30}"; MIN="${K3_PERFMIN:-30}"
ARGS="-testsession -passageiros 60 -bot -vel 3 -vista geral -perf $SEG -perfmin $MIN"
PKG=br.com.vstack.littleterminal
mkdir -p "$OUT"; LOG="$OUT/k3_$ALVO.log"

fail() { echo "FALHOU: $*" >&2; exit 1; }
veredito() {
  local k; k=$(grep -m1 -oE 'K3 (OK|FAIL).*' "$LOG" | tr -d '\r') || fail "sem linha K3 no log (ver $LOG)"
  grep -m1 -oE 'PERF .*' "$LOG" | tr -d '\r'
  echo "$k"
  [[ "$k" == "K3 OK"* ]]
}

pc() {
  local exe="$CLIENT/Builds/win/TinyAirport.exe"
  [ -f "$exe" ] || fail "sem $exe: rode client/tools/verify.sh full antes"
  rm -f "$LOG"
  timeout -k 10 $((SEG + 120)) "$exe" $ARGS -screen-fullscreen 0 -screen-width 540 -screen-height 960 -logFile "$LOG" > /dev/null 2>&1
  veredito
}

android() {
  local adb="${ADB:-adb}" apk="$CLIENT/Builds/android/TinyAirport-dev.apk" act fim
  command -v "$adb" > /dev/null || fail "adb não achado (defina ADB)"
  [ -n "${ADB_SERIAL:-}" ] && adb="$adb -s $ADB_SERIAL"
  [ "$($adb get-state 2>/dev/null | tr -d '\r')" = device ] || fail "nenhum aparelho no adb (cabo, depuração USB ligada e a chave aceita no aparelho)"
  [ -f "$apk" ] || fail "sem $apk: rode client/tools/verify.sh android antes"
  $adb install -r "$apk" > /dev/null || fail "adb install"
  act=$($adb shell cmd package resolve-activity --brief "$PKG" | tail -1 | tr -d '\r')
  [[ "$act" == */* ]] || fail "atividade de abertura de $PKG não achada ($act)"
  $adb shell am force-stop "$PKG"; $adb logcat -c
  # o extra "unity" vira a linha de comando do jogo; o Game.Arg só lê em build de desenvolvimento
  $adb shell am start -W -n "$act" -e unity "\"$ARGS\"" > /dev/null || fail "am start"
  fim=$((SECONDS + SEG + 120)); rm -f "$LOG"
  while [ $SECONDS -lt $fim ]; do
    $adb logcat -d -s Unity > "$LOG" 2>/dev/null
    grep -qE 'K3 (OK|FAIL)' "$LOG" && break
    sleep 3
  done
  { echo "aparelho: $($adb shell getprop ro.product.model | tr -d '\r'), Android $($adb shell getprop ro.build.version.release | tr -d '\r')"; } >> "$LOG"
  $adb shell am force-stop "$PKG"
  veredito
}

case "$ALVO" in
  pc) pc ;;
  android) android ;;
  *) sed -n '4,5p' "$0"; exit 2 ;;
esac
