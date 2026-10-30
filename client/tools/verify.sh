#!/bin/bash
# Portão do Tiny Airport num comando só (cópia do verify.sh do Dungeon Courier, mesmo nome e uso do Forge Street). Git Bash no Windows.
# uso: client/tools/verify.sh quick | full | android
#   quick  checa_repo + núcleo (dotnet test) + viewcheck da View, sem abrir o Unity
#   full   núcleo + Unity batchmode: EditMode, viewcheck, Setup.BuildWindows, o .exe com -autoplay 10 e checa_repo
#   android  núcleo + Setup.BuildAndroidDev + checa_apk (targetSdk, ABI, AD_ID, 16 KB); pelo drive subst (caminho com espaço quebra o Gradle)
# Imprime só OK ou o erro; os logs desta rodada ficam em client/Builds/verify/. As fotos 540×960 continuam sendo conferidas à mão.
# Regra do PC (7,7 GB): um processo pesado por vez; aborta se o Unity estiver aberto neste projeto ou com emulador, Blender ou Docker ligados.
# Caminho longo (worktree): se o Unity falhar por MAX_PATH, rode por um drive subst (ex.: subst R: <raiz do repo>; R:/client/tools/verify.sh full).
# Variável opcional: UNITY_EXE.
set -uo pipefail
export MSYS_NO_PATHCONV=1 DOTNET_CLI_UI_LANGUAGE=en   # caminhos do Windows passam intactos; saída do dotnet em inglês para o grep

ROOT="$(cd "$(dirname "$0")/../.." && pwd -W)"; CLIENT="$ROOT/client"; OUT="$CLIENT/Builds/verify"
UNITY="${UNITY_EXE:-C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe}"
T0=$SECONDS

ok()   { echo "OK   $* [$((SECONDS - T0)) s]"; }
fail() { echo "FALHOU: $*" >&2; exit 1; }

repo() {
  local log rc
  mkdir -p "$OUT"
  log=$(python "$CLIENT/tools/checa_repo.py" 2>&1); rc=$?
  echo "$log" > "$OUT/checa_repo.log"
  [ $rc -eq 0 ] || { echo "$log" | grep -v '^AVISO' | head -20; fail "checa_repo (ver $OUT/checa_repo.log)"; }
  ok "repo: $(echo "$log" | tail -1)"
}

nucleo() {
  local log rc
  mkdir -p "$OUT"
  log=$(dotnet test "$CLIENT/tools/coretests" -nologo 2>&1); rc=$?
  echo "$log" > "$OUT/coretests.log"
  [ $rc -eq 0 ] || { echo "$log" | grep -E "Failed |error " | head -20; fail "dotnet test do núcleo (ver $OUT/coretests.log)"; }
  ok "núcleo: $(echo "$log" | grep -m1 -oE 'Failed: +[0-9]+, Passed: +[0-9]+, Skipped: +[0-9]+, Total: +[0-9]+' | tr -s ' ')"
}

# a View usa o uGUI: a DLL vem de client/Library/ScriptAssemblies; num worktree novo, de outro worktree deste repo que já abriu no Unity
ugui_dll() {
  local w d="client/Library/ScriptAssemblies/UnityEngine.UI.dll"
  { echo "$ROOT"; git -C "$ROOT" worktree list --porcelain | sed -n 's/^worktree //p'; } | while read -r w; do
    [ -f "$w/$d" ] && { echo "$w/$d"; break; }
  done
}

viewcheck() {
  local log rc dll
  dll=$(ugui_dll)
  [ -n "$dll" ] || fail "nenhum worktree tem client/Library/ScriptAssemblies/UnityEngine.UI.dll: rode o full (abre o Unity) uma vez"
  log=$(dotnet build "$CLIENT/tools/viewcheck/view" -nologo -v q "-p:UguiDll=$dll" 2>&1); rc=$?
  echo "$log" > "$OUT/viewcheck.log"
  [ $rc -eq 0 ] || { echo "$log" | grep -E " error " | head -20; fail "viewcheck (ver $OUT/viewcheck.log)"; }
  ok "viewcheck: $(echo "$log" | grep -oE '[0-9]+ (Warning|Error)\(s\)' | paste -sd, | tr -s ' ')"
}

# antes de abrir o Unity: nada de Editor neste projeto nem emulador ligado
guard() {
  [ -f "$UNITY" ] || fail "Unity não achado em $UNITY (defina UNITY_EXE)"
  local cl me
  cl=$(powershell.exe -NoProfile -Command "(Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\").CommandLine" 2>/dev/null | tr 'A-Z\\' 'a-z/')
  me=$(echo "$CLIENT" | tr 'A-Z' 'a-z')
  echo "$cl" | grep -qF "$me" && fail "o Unity está aberto neste projeto ($CLIENT): feche o Editor antes"
  [ -n "$(echo "$cl" | tr -d '[:space:]')" ] && echo "AVISO: há outro Unity.exe aberto (PC de 7,7 GB)"
  tasklist 2>/dev/null | grep -qiE '^(qemu-system|emulator|blender|Docker Desktop)' && fail "emulador, Blender ou Docker ligado: feche antes (um processo pesado por vez)"
  mkdir -p "$OUT"
}

# Unity em batchmode; o log é apagado antes, então o que estiver nele é desta rodada
run_unity() {
  local log="$OUT/$1"; shift
  rm -f "$log"
  "$UNITY" -batchmode -nographics -projectPath "$CLIENT" -logFile "$log" "$@"
}

# o Setup.Apply regrava settings: avisa se mexeu em arquivo versionado (git checkout -- se o gerador não mudou)
dirty() {
  local p=(client/ProjectSettings client/Assets client/Packages) s
  s=$(git -C "$ROOT" diff --name-only -- "${p[@]}" 2>/dev/null; git -C "$ROOT" ls-files --others --exclude-standard -- "${p[@]}")
  [ -z "$s" ] || printf 'AVISO: o Unity mexeu em arquivos versionados:\n%s\n' "$s"
}

quick() {
  repo
  nucleo
  viewcheck
}

full() {
  nucleo
  guard
  rm -f "$OUT/editmode.xml"
  run_unity editmode.log -runTests -testPlatform EditMode -testResults "$OUT/editmode.xml"   # veredito no XML, não no código de saída
  local run n; run=$(grep -m1 -o '<test-run [^>]*>' "$OUT/editmode.xml" 2>/dev/null) || fail "EditMode sem XML (ver $OUT/editmode.log)"
  n=$(echo "$run" | grep -oE '(total|passed|failed)="[0-9]+"' | paste -sd' ')
  [[ "$run" == *'result="Passed"'* ]] || fail "EditMode: $n (ver $OUT/editmode.xml)"
  ok "EditMode: $n"
  viewcheck
  run_unity build_win.log -quit -executeMethod TA.EditorTools.Setup.BuildWindows; local rc=$? sum
  sum=$(grep -m1 -oE "BuildSummary\(StandaloneWindows64[^)]*\): .*" "$OUT/build_win.log")   # o Setup põe "dev 0.0.1-alpha" nos parênteses
  [ $rc -eq 0 ] && [[ "$sum" == *result=Succeeded* ]] || fail "BuildWindows (saída $rc, '${sum:-sem BuildSummary}', ver $OUT/build_win.log)"
  ok "BuildWindows: $sum"
  rm -f "$OUT/autoplay.log"
  "$CLIENT/Builds/win/TinyAirport.exe" -batchmode -nographics -autoplay 10 -logFile "$OUT/autoplay.log" > /dev/null 2>&1 || fail "autoplay saiu $? (ver $OUT/autoplay.log)"
  local a; a=$(grep -m1 -o 'AUTOPLAY OK.*' "$OUT/autoplay.log") || fail "sem 'AUTOPLAY OK' (ver $OUT/autoplay.log)"
  grep -q "Exception" "$OUT/autoplay.log" && fail "exceção no autoplay (ver $OUT/autoplay.log)"
  ok "autoplay: $a"
  dirty
  repo
}

android() {
  nucleo
  guard
  run_unity build_android.log -buildTarget Android -quit -executeMethod TA.EditorTools.Setup.BuildAndroidDev; local rc=$? sum log
  sum=$(grep -m1 -oE "BuildSummary\(Android[^)]*\): .*" "$OUT/build_android.log")
  [ $rc -eq 0 ] && [[ "$sum" == *result=Succeeded* ]] || fail "BuildAndroidDev (saída $rc, '${sum:-sem BuildSummary}', ver $OUT/build_android.log)"
  ok "BuildAndroid: $sum"
  log=$(python "$CLIENT/tools/checa_apk.py" "$CLIENT/Builds/android/TinyAirport-dev.apk" 2>&1) || { echo "$log"; fail "checa_apk"; }
  ok "apk: $log"
}

case "${1:-}" in
  quick) quick ;;
  full) full ;;
  android) android ;;
  *) sed -n '3,6p' "$0"; exit 2 ;;
esac
echo "verify $1: OK em $((SECONDS - T0)) s"
