"""Mede o andar das pessoas e as caixas dos veiculos a partir do CSV do -diag (Mundo.Patio.cs, DiagBoneco).

Uso: python client/tools/diag_pessoas.py arq.csv
Pessoas (pax#id e func#k; a coluna y = 1 com o boneco andando, a coluna yaw = o rumo da folha, um eixo do mundo):
  lado%     amostras andando com mais de 30 graus entre o rumo da folha e a velocidade (andar de lado)
  viradas   trocas de rumo da folha por 10 m andados (zigue-zague)
  tele      saltos acima de 6 m/s (teletransporte); a pessoa que entra ou sai de cena nao conta
  vMax      velocidade maxima (m/s)
  pe%       amostras andando fora de 0,39 a 2,53 m/s: a cadencia da folha satura e o pe desliza (PassoNatural 1,1 m/s, Pinta)
  balcao%   amostras andando a menos de 0,3 m do centro de um balcao (balcao#e.i, gravado no comeco): atravessou
  choque    pessoa andando a menos de 0,15 m de outra, por minuto de pessoa andando
Veiculos: sobrepos = amostras com 2 veiculos com as caixas (comprimento x largura da malha) sobrepostas, parados ou nao; o rebocador
preso ao aviao nao conta. Criterio do Vinicius (30/10): nenhuma sobreposicao; sem andar de lado, sem atravessar, sem teletransporte.
"""

import argparse
import csv
import math
from collections import defaultdict

# comprimento, largura e eixo (m do pivo ao centro da caixa) por malha, de malhas.json e do NovoVeiculo
CAIXA = {
    "caminhao_catering": (2.8, 1.28, 0.75),
    "caminhao_combustivel": (2.6, 1.13, 0.75),
    "carrinho_bagagem": (2.6, 0.78, 0.6),
    "escada_embarque": (2.3, 1.17, 0.7),
    "onibus_patio": (3.2, 1.47, 0.9),
    "rebocador": (1.5, 0.76, 0.35),
}


def ang(a):
    return (a + 180.0) % 360.0 - 180.0


def cantos(x, z, yaw, c, l, e):
    s, k = math.sin(math.radians(yaw)), math.cos(math.radians(yaw))
    cx, cz = x + s * e, z + k * e
    return [
        (cx + s * a * c / 2 + k * b * l / 2, cz + k * a * c / 2 - s * b * l / 2)
        for a, b in ((1, 1), (1, -1), (-1, -1), (-1, 1))
    ]


def separa(a, b):
    """Eixo separador entre 2 retangulos (SAT): True = nao se tocam."""
    for p in (a, b):
        for i in range(2):
            ex, ez = p[i + 1][0] - p[i][0], p[i + 1][1] - p[i][1]
            n = (-ez, ex)
            pa = [q[0] * n[0] + q[1] * n[1] for q in a]
            pb = [q[0] * n[0] + q[1] * n[1] for q in b]
            if max(pa) <= min(pb) or max(pb) <= min(pa):
                return True
    return False


def pessoas(linhas, balcoes):
    serie = defaultdict(list)
    por_t = defaultdict(list)
    for r in linhas:
        t, x, z, anda, yaw = (
            float(r["t"]),
            float(r["x"]),
            float(r["z"]),
            float(r["y"]),
            float(r["yaw"]),
        )
        serie[r["peca"]].append((t, x, z, anda, yaw))
        por_t[t].append((r["peca"], x, z, anda))
    tot = defaultdict(float)
    for nome, p in serie.items():
        for a, b in zip(p, p[1:]):
            dt = b[0] - a[0]
            if dt <= 0 or dt > 0.6:
                continue
            d = math.hypot(b[1] - a[1], b[2] - a[2])
            v = d / dt
            if v > 6.0:
                tot["tele"] += 1
                continue
            if v < 0.2:
                continue
            tot["n"] += 1
            tot["dist"] += d
            tot["tempo"] += dt
            tot["vmax"] = max(tot["vmax"], v)
            rumo = math.degrees(math.atan2(b[1] - a[1], b[2] - a[2]))
            if abs(ang(rumo - b[4])) > 30.0:
                tot["lado"] += 1
            if abs(ang(b[4] - a[4])) > 1.0:
                tot["vira"] += 1
            if v < 0.39 or v > 2.53:
                tot["pe"] += 1
            if any(math.hypot(b[1] - q[0], b[2] - q[1]) < 0.3 for q in balcoes):
                tot["balcao"] += 1
    for t, gente in por_t.items():
        for i, g in enumerate(gente):
            if g[3] < 0.5:
                continue
            if any(
                j != i and math.hypot(g[1] - o[1], g[2] - o[2]) < 0.15
                for j, o in enumerate(gente)
            ):
                tot["choque"] += 1
    n = max(tot["n"], 1)
    print("PESSOAS amostras=%d dist=%.0f m" % (tot["n"], tot["dist"]))
    print(
        "  lado%%=%.1f viradas=%.2f/10m tele=%d vMax=%.2f pe%%=%.1f balcao%%=%.1f choque=%.1f/min"
        % (
            100 * tot["lado"] / n,
            10 * tot["vira"] / max(tot["dist"], 1),
            tot["tele"],
            tot["vmax"],
            100 * tot["pe"] / n,
            100 * tot["balcao"] / n,
            60 * tot["choque"] * 0.1 / max(tot["tempo"], 1),
        )
    )


def veiculos(linhas):
    por_t = defaultdict(list)
    for r in linhas:
        por_t[r["t"]].append(r)
    amostras, ruins = 0, 0
    pares = defaultdict(int)
    for t, vs in por_t.items():
        amostras += 1
        caixas = []
        for r in vs:
            nome = r["peca"].split("#")[0]
            c, l, e = CAIXA[nome]
            caixas.append(
                (
                    r["peca"],
                    cantos(float(r["x"]), float(r["z"]), float(r["yaw"]), c, l, e),
                )
            )
        ruim = False
        for i, a in enumerate(caixas):
            for b in caixas[i + 1 :]:
                if not separa(a[1], b[1]):
                    ruim = True
                    pares[a[0] + " x " + b[0]] += 1
        ruins += ruim
    print(
        "VEICULOS amostras=%d sobrepos=%d %s"
        % (amostras, ruins, dict(sorted(pares.items(), key=lambda kv: -kv[1])[:4]))
    )


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("csv")
    a = ap.parse_args()
    gente, carros, balcoes = [], [], []
    with open(a.csv, newline="") as f:
        for r in csv.DictReader(f):
            nome = r["peca"].split("#")[0]
            if nome == "balcao":
                balcoes.append((float(r["x"]), float(r["z"])))
            elif nome in ("pax", "func"):
                gente.append(r)
            elif nome in CAIXA:
                carros.append(r)
    pessoas(gente, balcoes)
    veiculos(carros)


if __name__ == "__main__":
    main()
