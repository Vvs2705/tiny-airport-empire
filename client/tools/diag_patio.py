"""Mede o movimento do patio a partir do CSV do -diag (Mundo.Patio.cs): escorregamento, velocidade, aceleracao, giro.

Uso: python client/tools/diag_patio.py arq.csv [--frente nome=graus ...]
  --frente  yaw da frente real da malha em relacao ao eixo z do pivo (para medir o "antes", quando a View ainda nao a aplicava).
Saida por peca, sempre medida no pivo (eixo traseiro, o ponto que nao escorrega num veiculo de verdade):
  esc50/95/max  angulo entre o rumo e a velocidade, em graus, com a peca a mais de 0,3 m/s; andar de re conta como alinhado
  vMax          velocidade maxima (m/s)
  acel/freio    aceleracao e freada maximas (m/s2)
  giro          taxa de giro maxima (graus/s)
  giroParado    taxa de giro com a peca a menos de 0,05 m/s (giro no lugar: tem de ser 0)
  tranco        degraus de velocidade acima de 2 m/s em 0,2 s (partida ou parada instantanea, teletransporte)
Teletransporte de verdade (aviao que some no fim da decolagem e reaparece no pouso) quebra a serie: nao entra na conta.
Criterio do Vinicius (30/10): esc95 <= 5, giroParado = 0, aceleracao e freada visiveis.
"""

import argparse
import csv
import math
import sys
from collections import defaultdict


def ang(a):
    return (a + 180.0) % 360.0 - 180.0


def series(p):
    """Quebra a serie onde a peca some e reaparece (buraco no tempo ou salto de mais de 3 m numa amostra)."""
    atual = [p[0]]
    for a, b in zip(p, p[1:]):
        if b[0] - a[0] > 0.6 or math.hypot(b[1] - a[1], b[3] - a[3]) > 3.0:
            yield atual
            atual = []
        atual.append(b)
    yield atual


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("csv")
    ap.add_argument("--frente", nargs="*", default=[])
    a = ap.parse_args()
    frente = {k: float(v) for k, v in (x.split("=") for x in a.frente)}
    dados = defaultdict(list)
    with open(a.csv) as f:
        for r in csv.DictReader(f):
            dados[r["peca"]].append(
                (
                    float(r["t"]),
                    float(r["x"]),
                    float(r["y"]),
                    float(r["z"]),
                    float(r["yaw"]),
                )
            )
    print(
        "%-24s %7s %6s %6s %6s %6s %6s %6s %7s %10s %6s"
        % (
            "peca",
            "dist_m",
            "esc50",
            "esc95",
            "escMax",
            "vMax",
            "acel",
            "freio",
            "giro",
            "giroParado",
            "tranco",
        )
    )
    pior, giro_pior = 0.0, 0.0
    for nome in sorted(dados):
        off = frente.get(nome.split("#")[0], 0.0)
        esc, vs, acs, gs = [], [], [], []
        dist, giro_parado, tranco = 0.0, 0.0, 0
        for p in series(dados[nome]):
            vant = None
            for i in range(2, len(p) - 2):
                t0, x0, _, z0, yaw0 = p[i - 2]
                t1, x1, _, z1, yaw1 = p[i + 2]
                dt = t1 - t0
                if dt <= 0:
                    continue
                dx, dz = x1 - x0, z1 - z0
                v = math.hypot(dx, dz) / dt
                giro = abs(ang(yaw1 - yaw0)) / dt
                dist += math.hypot(p[i][1] - p[i - 1][1], p[i][3] - p[i - 1][3])
                if v > 0.3:
                    e = abs(ang(p[i][4] + off - math.degrees(math.atan2(dx, dz))))
                    esc.append(
                        180.0 - e if e > 90.0 else e
                    )  # de re esta certo: mede o alinhamento com o eixo da peca
                if v < 0.05:
                    giro_parado = max(giro_parado, giro)
                vs.append(v)
                gs.append(giro)
                if vant is not None:
                    ac = (v - vant[0]) / max(p[i][0] - vant[1], 1e-3)
                    acs.append(ac)
                    if abs(ac) * 0.2 > 2.0:
                        tranco += 1
                vant = (v, p[i][0])
        if not vs:
            continue
        esc.sort()
        q = lambda f: esc[min(len(esc) - 1, int(f * len(esc)))] if esc else 0.0
        pior, giro_pior = max(pior, q(0.95)), max(giro_pior, giro_parado)
        print(
            "%-24s %7.1f %6.1f %6.1f %6.1f %6.2f %6.2f %6.2f %7.1f %10.1f %6d"
            % (
                nome,
                dist,
                q(0.5),
                q(0.95),
                max(esc or [0.0]),
                max(vs),
                max(acs or [0.0]),
                -min(acs or [0.0]),
                max(gs),
                giro_parado,
                tranco,
            )
        )
    print("PIOR esc95 = %.1f graus; giro parado max = %.1f graus/s" % (pior, giro_pior))
    return 0


sys.exit(main())
