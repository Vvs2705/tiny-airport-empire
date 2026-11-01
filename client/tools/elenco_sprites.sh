#!/bin/bash
# Folhas das pessoas (GDD §10.2, D-18): o .blend leve de cada personagem (elenco_ta.py, uma vez) -> render_sprites.py --preset <ator> --mask
# -> client/Assets/_TA/Resources/Sprites/<ator>/ (entra no git: e render, nao o modelo). Conferencia em client/Builds/sprites_contact/.
# uso: client/tools/elenco_sprites.sh [ator ...]   (sem argumento: o elenco todo, 15 atores). Um Blender por vez (PC de 7,7 GB). Git Bash no Windows.
# Arte crua no checkout principal (arte/tripo/_leve/*.blend, arte/mixamo/ta_pax e ta_func), fora do git.
set -uo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ARTE="$(cd "$(git -C "$ROOT" rev-parse --path-format=absolute --git-common-dir)/.." && pwd)/arte"
BLENDER="${BLENDER_EXE:-/c/Program Files/Blender Foundation/Blender 5.2/blender.exe}"
OUT="$ROOT/client/Assets/_TA/Resources/Sprites"; CONF="$ROOT/client/Builds/sprites_contact"; LOG="$ROOT/client/Builds/sprites_log"
mkdir -p "$OUT" "$CONF" "$LOG"
MALA="$(ls "$ARTE"/tripo/mala_rodinha_hd/*.fbx | head -1)"
w() { cygpath -m "$1"; }

ator() {
  local nome=$1 anims=$2 saida=${SAIDA:-$1}; shift 2
  [ -f "$ARTE/tripo/_leve/$nome.blend" ] || { echo "FALHOU $nome: falta $ARTE/tripo/_leve/$nome.blend (rode elenco_ta.py)"; return 1; }
  rm -rf "$OUT/$saida"
  "$BLENDER" -b --factory-startup --python-exit-code 1 --python "$(w "$ROOT/client/tools/render_sprites.py")" -- \
    --model "$(w "$ARTE/tripo/_leve/$nome.blend")" --anims "$(w "$ARTE/mixamo/$anims")" --preset "$nome" --mask \
    --out "$(w "$OUT/$saida")" --contact "$(w "$CONF/$saida.png")" "$@" > "$LOG/$saida.log" 2>&1
  if grep -q "SPRITES_OK" "$LOG/$saida.log"; then echo "OK   $saida: $(grep -c '^CLIP' "$LOG/$saida.log") clipes $(grep -E '^(AVISO|MASCARA)' "$LOG/$saida.log" | tr '\n' ' ' | cut -c1-200)"
  else echo "FALHOU $saida (ver $LOG/$saida.log)"; grep -E "ERRO|Error|Traceback" "$LOG/$saida.log" | head -5; return 1; fi
}

ASSENTO=0.27   # altura do assento da cadeiras_espera em m (render_sprites.py --senta)
# passageiros: a mala de rodinha na mao direita (no repouso em T o braco direito aponta para -x; a alca vai para a mao) e tingida na cor do voo
mala() { echo "$(w "$MALA"):RightHand:$2,0,0:0,90,0:$1"; }
pax() { ator "$1" ta_pax --attach "$(mala "$2" -0.24)" --mask-attach --senta "$ASSENTO"; }
# <ator>_mao: a mesma pessoa so com a mala de mao, depois do despacho (Mundo.Malas.cs); a cor do voo continua na mao
mao() { SAIDA="$1_mao" ator "$1" ta_pax --attach "$(mala "$2" -0.13)" --mask-attach --senta "$ASSENTO"; }

lista=("$@"); [ ${#lista[@]} -eq 0 ] && lista=(passageiro_a passageira_b passageiro_idoso crianca passageiro_a_mao passageira_b_mao passageiro_idoso_mao crianca_mao atendente seguranca vendedor piloto comissaria comissario despachante bagageiro mecanico agente_portao agente_portao_f)
falhas=0
for a in "${lista[@]}"; do
  case $a in
    crianca_mao) mao crianca 0.16 ;;
    *_mao) mao "${a%_mao}" 0.2 ;;
    crianca) pax "$a" 0.30 ;;
    passageiro_*|passageira_*) pax "$a" 0.40 ;;
    atendente|seguranca|vendedor|bagageiro|mecanico|despachante) ator "$a" ta_func ;;   # o uniforme (elenco_ta.py, material TA_MASK) tinge na cor do setor
    agente_portao*) ator "$a" ta_func ;;   # uniforme fixo, com o clipe de atender
    *) ator "$a" ta_pax ;;   # tripulacao: uniforme fixo da funcao, sem mascara
  esac || falhas=$((falhas + 1))
done
[ $falhas -eq 0 ] && echo "elenco: OK em $OUT" || { echo "elenco: $falhas falhas" >&2; exit 1; }
