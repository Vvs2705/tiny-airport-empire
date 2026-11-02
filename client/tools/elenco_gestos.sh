#!/bin/bash
# Gestos do atendimento nas folhas que ja existem (render_sprites.py --gestos): passageiros entregam o documento e poem a mala na balanca; a equipe, que
# muda de estacao, tem os gestos de todas. Depois deste script, abrir o Unity gera os .png.meta. Blender: pela fila. Git Bash no Windows.
set -uo pipefail
T="$(dirname "$0")/elenco_sprites.sh"
GESTOS=Entrega,PoeMala "$T" passageiro_a passageira_b passageiro_idoso crianca passageiro_a_mao passageira_b_mao passageiro_idoso_mao crianca_mao
GESTOS=Digita,Pega,Etiqueta,Bastao,Tela "$T" atendente seguranca vendedor bagageiro mecanico despachante
GESTOS=Digita,Pega "$T" agente_portao agente_portao_f
