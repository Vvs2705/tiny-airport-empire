"""Conta os cruzamentos do patio a partir de um ou mais CSV do -diag (Mundo.Patio.cs, DiagCaixas): caixa de cada peca contra a trajetoria
de cada veiculo, aviao e pessoa, com a altura. Roda fora do jogo: nenhuma fisica de motor, nenhum colisor em tempo de jogo.

Uso: python client/tools/diag_caixas.py arq.csv [arq2.csv ...]     (sai 1 se houver cruzamento)
     python client/tools/diag_caixas.py --selftest

Conta um cruzamento por encontro (2 pecas sobrepostas em amostras seguidas), mais fundo que TOLERANCIA:
  veiculo x peca, veiculo x veiculo, veiculo x aviao, aviao x peca, aviao x aviao e pessoa x veiculo. Pessoa em sala e parede: PlantaTests.
Cobertura: quantas vezes cada peca saiu do lugar e voltou (ida e volta) e por onde passou; o par so vale se a rota rodou.
A caixa de um veiculo e a da malha. As de aviao, garagem e rebocador preso sao as tabelas abaixo, medidas nos cortes das malhas.
"""

import argparse
import csv
import math
import sys
from collections import defaultdict

TOLERANCIA = 0.02  # m: menos que isso e o contorno da malha, nao um cruzamento
PESSOAS = ("pax", "func", "equipe", "ator", "trip", "mec")
PESSOA_LADO, PESSOA_ALTURA = 0.4, 1.2

# partes do aviao: (nome, frente0, frente1, lateral0, lateral1, altura0, altura1), m a partir do pivo, com a frente para +z
AVIAO = {
    "aviao_regional": [
        ("nariz", 2.0, 3.69, -0.72, 0.72, 0.7, 2.2),
        ("fuselagem", -2.6, 2.0, -1.0, 1.0, 0.6, 2.7),
        ("cauda", -3.69, -2.6, -0.6, 0.6, 1.5, 3.71),
        ("profundor", -3.69, -2.7, -1.5, 1.5, 3.0, 3.6),
        ("asa", -0.4, 0.8, -3.8, 3.8, 2.19, 2.6),
        ("motor", -0.4, 1.8, -2.4, 2.4, 1.8, 3.0),
        ("trem", -0.6, 0.6, -1.5, 1.5, 0.0, 0.6),
    ],
    "aviao_narrow": [
        ("nariz", 3.0, 4.5, -0.81, 0.81, 0.6, 2.8),
        ("fuselagem", -3.0, 3.0, -1.0, 1.0, 0.6, 2.85),
        ("cauda", -4.5, -3.0, -0.55, 0.55, 1.5, 4.5),
        ("profundor", -4.2, -3.0, -1.84, 1.84, 1.9, 2.6),
        ("asa", 0.0, 2.13, -3.0, 3.0, 0.53, 1.86),
        ("ponta da asa", -0.57, 0.84, -4.47, 4.47, 1.56, 1.95),
        ("trem", -0.5, 2.0, -2.0, 2.0, 0.0, 0.6),
    ],
}

# garagem oca, na malha em escala 1 e yaw 0: (nome, x0, x1, y0, y1, z0, z1); a boca fica em +z, com 5 vaos entre 6 pilares
GARAGEM = (9.6, 2.943, 4.037)
OCA = {
    "garagem_patio": [
        ("fundo", -4.8, 4.8, 0.0, 2.943, -2.02, -1.15),
        ("verga", -4.8, 4.8, 1.8, 2.943, 1.1, 2.02),
        ("teto", -4.8, 4.8, 2.3, 2.943, -2.02, 2.02),
        ("pilar", 3.9, 4.8, 0.0, 1.85, 1.1, 2.02),
        ("pilar", 2.45, 2.8, 0.0, 1.85, 1.1, 2.02),
        ("pilar", 0.7, 1.0, 0.0, 1.85, 1.1, 2.02),
        ("pilar", -1.0, -0.65, 0.0, 1.85, 1.1, 2.02),
        ("pilar", -2.75, -2.45, 0.0, 1.85, 1.1, 2.02),
        ("pilar", -4.8, -4.2, 0.0, 1.85, 1.1, 2.02),
        ("mureta", 3.95, 4.8, 0.0, 1.05, -0.05, 2.02),
    ],
}

# contato que e o servico: a barra do rebocador no nariz e a ponte na porta (a ponte e da frente ta-embarque)
LIGADOS = (("rebocador", "aviao_regional.nariz"), ("aviao_regional", "ponte_embarque"))
ESCADA = "escada_embarque"  # parada, a gente sobe nela: pessoa x escada so conta com ela andando


class Caixa:
    """Retangulo girado no plano (frente = eixo u, de meia medida hl; lado, de meia medida hw) com a faixa de altura."""

    __slots__ = ("nome", "cx", "cz", "ux", "uz", "hl", "hw", "y0", "y1", "r")

    def __init__(self, nome, cx, cz, yaw, hl, hw, y0, y1):
        self.nome, self.cx, self.cz, self.hl, self.hw, self.y0, self.y1 = (
            nome,
            cx,
            cz,
            hl,
            hw,
            y0,
            y1,
        )
        self.ux, self.uz = math.sin(math.radians(yaw)), math.cos(math.radians(yaw))
        self.r = math.hypot(hl, hw)


def penetra(a, b):
    """Profundidade da sobreposicao de 2 caixas (eixo separador + altura); 0 = nao se tocam."""
    dz = b.cz - a.cz
    dx = b.cx - a.cx
    if dx * dx + dz * dz >= (a.r + b.r) ** 2:
        return 0.0
    menor = min(a.y1, b.y1) - max(a.y0, b.y0)
    if menor <= 0.0:
        return 0.0
    for ex, ez in ((a.ux, a.uz), (a.uz, -a.ux), (b.ux, b.uz), (b.uz, -b.ux)):
        ra = a.hl * abs(ex * a.ux + ez * a.uz) + a.hw * abs(ex * a.uz - ez * a.ux)
        rb = b.hl * abs(ex * b.ux + ez * b.uz) + b.hw * abs(ex * b.uz - ez * b.ux)
        d = ra + rb - abs(dx * ex + dz * ez)
        if d <= 0.0:
            return 0.0
        menor = min(menor, d)
    return menor


def partes_fixas(nome, x, y0, z, yaw, sx, sy, sz):
    """A peca parada: uma caixa ou, se for oca, uma por parte (escala = medida da malha no jogo / medida em escala 1)."""
    if nome not in OCA:
        return [Caixa(nome, x, z, yaw, sz / 2, sx / 2, y0, y0 + sy)]
    ex, ey, ez = sx / GARAGEM[0], sy / GARAGEM[1], sz / GARAGEM[2]
    s, k = math.sin(math.radians(yaw)), math.cos(math.radians(yaw))
    r = []
    for p, x0, x1, a0, a1, z0, z1 in OCA[nome]:
        lx, lz = (x0 + x1) * ex / 2, (z0 + z1) * ez / 2
        r.append(
            Caixa(
                nome + "." + p,
                x + k * lx + s * lz,
                z - s * lx + k * lz,
                yaw,
                (z1 - z0) * ez / 2,
                (x1 - x0) * ex / 2,
                y0 + a0 * ey,
                y0 + a1 * ey,
            )
        )
    return r


def partes_movel(nome, medida, x, y, z, yaw):
    s, k = math.sin(math.radians(yaw)), math.cos(math.radians(yaw))
    if nome in AVIAO:
        r = []
        for p, f0, f1, l0, l1, a0, a1 in AVIAO[nome]:
            f, l = (f0 + f1) / 2, (l0 + l1) / 2
            r.append(
                Caixa(
                    nome + "." + p,
                    x + s * f + k * l,
                    z + k * f - s * l,
                    yaw,
                    (f1 - f0) / 2,
                    (l1 - l0) / 2,
                    y + a0,
                    y + a1,
                )
            )
        return r
    ox, y0, oz, sx, sy, sz = medida
    return [
        Caixa(
            nome,
            x + k * ox + s * oz,
            z - s * ox + k * oz,
            yaw,
            sz / 2,
            sx / 2,
            y + y0,
            y + y0 + sy,
        )
    ]


def ligado(a, b):
    return any(
        (a.startswith(p) and b.startswith(q)) or (b.startswith(p) and a.startswith(q))
        for p, q in LIGADOS
    )


class Conta:
    """Encontros por par: amostras seguidas (buraco de ate 0,35 s) do mesmo par sao um cruzamento so."""

    def __init__(self):
        self.ultimo = {}
        self.pares = defaultdict(
            lambda: [0, 0.0, 0.0]
        )  # par -> cruzamentos, s sobrepostos, mais fundo
        self.onde = {}  # par -> t, x e z do primeiro encontro (para achar na tela)
        self.x = self.z = 0.0

    def anota(self, classe, a, b, chave, t, p, dt):
        rot = (classe, a, b)
        d = self.pares[rot]
        if t - self.ultimo.get(chave, -1e9) > 0.35:
            d[0] += 1
        self.ultimo[chave] = t
        d[1] += dt
        d[2] = max(d[2], p)
        self.onde.setdefault(rot, (t, self.x, self.z))


def mede(arquivo, conta, cobertura):
    fixas, medidas, por_t = [], {}, defaultdict(list)
    with open(arquivo, newline="") as f:
        for r in csv.DictReader(f):
            tipo, _, nome = r["peca"].partition("#")
            if tipo == "fixa":
                vals = [float(r[c]) for c in ("x", "y", "z", "yaw", "sx", "sy", "sz")]
                fixas += partes_fixas(nome, *vals)
            elif tipo == "movel":
                medidas[nome] = [float(r[c]) for c in ("x", "y", "z", "sx", "sy", "sz")]
            elif tipo not in ("balcao", "cadeira"):
                por_t[float(r["t"])].append(r)
    tempos = sorted(por_t)
    casa, longe, em_casa = {}, defaultdict(int), {}
    antes, parada = {}, set()
    for i, t in enumerate(tempos):
        dt = t - tempos[i - 1] if i else 0.1
        moveis, gente = [], []
        for r in por_t[t]:
            peca = r["peca"]
            base = peca.split("#")[0]
            x, y, z, yaw = float(r["x"]), float(r["y"]), float(r["z"]), float(r["yaw"])
            if base in PESSOAS:
                lado = PESSOA_LADO / 2
                gente.append(
                    (peca, Caixa(base, x, z, 0.0, lado, lado, 0.0, PESSOA_ALTURA))
                )
                continue
            if base not in medidas and base not in AVIAO:
                continue
            if base == ESCADA:
                (parada.add if antes.get(peca) == (x, z) else parada.discard)(peca)
                antes[peca] = (x, z)
            moveis.append(
                (peca, base, partes_movel(base, medidas.get(base), x, y, z, yaw))
            )
            if peca not in casa:
                casa[peca], em_casa[peca] = (x, z), True
            perto = math.hypot(x - casa[peca][0], z - casa[peca][1]) < 0.5
            if perto != em_casa[peca]:
                em_casa[peca] = perto
                longe[peca, "volta" if perto else "ida"] += 1
        for j, (pa, ba, ca) in enumerate(moveis):
            aviao = ba in AVIAO
            conta.x, conta.z = ca[0].cx, ca[0].cz
            for c in ca:
                for f in fixas:
                    if ligado(c.nome, f.nome):
                        continue
                    p = penetra(c, f)
                    if p > TOLERANCIA:
                        classe = "aviao x peca" if aviao else "veiculo x peca"
                        conta.anota(
                            classe, c.nome, f.nome, (pa, c.nome, f.nome), t, p, dt
                        )
            for pb, bb, cb in moveis[j + 1 :]:
                outro = bb in AVIAO
                classe = (
                    "aviao x aviao"
                    if aviao and outro
                    else "veiculo x aviao"
                    if aviao or outro
                    else "veiculo x veiculo"
                )
                for c in ca:
                    for o in cb:
                        if ligado(c.nome, o.nome):
                            continue
                        p = penetra(c, o)
                        if p > TOLERANCIA:
                            conta.anota(
                                classe,
                                c.nome,
                                o.nome,
                                (pa, pb, c.nome, o.nome),
                                t,
                                p,
                                dt,
                            )
            if not aviao and pa not in parada:
                for pg, g in gente:
                    p = penetra(ca[0], g)
                    if p > TOLERANCIA:
                        conta.anota(
                            "pessoa x veiculo", g.nome, ca[0].nome, (pa, pg), t, p, dt
                        )
    for peca in casa:
        cobertura[peca.split("#")[0]][0] += longe[peca, "ida"]
        cobertura[peca.split("#")[0]][1] += longe[peca, "volta"]
    return len(tempos), tempos[-1] if tempos else 0.0, len(fixas)


def selftest():
    a = Caixa("a", 0, 0, 0, 1, 0.5, 0, 1)
    assert penetra(a, Caixa("b", 0, 1.9, 0, 1, 0.5, 0, 1)) > 0.05
    assert penetra(a, Caixa("b", 0, 2.1, 0, 1, 0.5, 0, 1)) == 0.0
    assert penetra(a, Caixa("b", 0, 0, 90, 1, 0.5, 2, 3)) == 0.0, "por cima nao cruza"
    assert penetra(a, Caixa("b", 1.7, 0, 45, 1, 0.5, 0, 1)) == 0.0
    assert penetra(a, Caixa("b", 0.9, 0, 45, 1, 0.5, 0, 1)) > 0.0
    cheia = partes_fixas("cone", 0, 0, 0, 0, 1, 1, 1)
    assert len(cheia) == 1
    oca = partes_fixas(
        "garagem_patio", 0, 0, 0, 180, GARAGEM[0], GARAGEM[1], GARAGEM[2]
    )
    fundo = next(c for c in oca if c.nome.endswith("fundo"))
    assert abs(fundo.cz - 1.585) < 1e-3, "yaw 180 leva o fundo para +z"
    carro = Caixa("carro", 0, 0, 0, 1, 0.6, 0, 1.2)
    assert not any(penetra(carro, c) for c in oca if not c.nome.endswith("fundo")), (
        "cabe no vao central"
    )
    pp = partes_movel("aviao_narrow", None, 0, 0, 0, 90)
    assert abs(next(c for c in pp if c.nome.endswith("nariz")).cx - 3.75) < 1e-6, (
        "yaw 90 = frente para +x"
    )
    c = Conta()
    for t, p in ((1.0, 0.1), (1.1, 0.2), (3.0, 0.1)):
        c.anota("v", "a", "b", "k", t, p, 0.1)
    assert c.pares["v", "a", "b"][0] == 2, "2 encontros"
    print("selftest ok")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("csv", nargs="*")
    ap.add_argument("--selftest", action="store_true")
    a = ap.parse_args()
    if a.selftest:
        selftest()
        return 0
    conta, cobertura = Conta(), defaultdict(lambda: [0, 0])
    amostras, segundos, fixas = 0, 0.0, 0
    for arq in a.csv:
        n, s, fixas = mede(arq, conta, cobertura)
        amostras, segundos = amostras + n, segundos + s
    print(
        "CAIXAS %d arquivo(s), %d amostras, %.0f s de jogo, %d caixas fixas"
        % (len(a.csv), amostras, segundos, fixas)
    )
    print(
        "COBERTURA (saidas/voltas): "
        + ", ".join("%s %d/%d" % (k, v[0], v[1]) for k, v in sorted(cobertura.items()))
    )
    por_classe = defaultdict(int)
    for (classe, x, y), d in conta.pares.items():
        por_classe[classe] += d[0]
    print(
        "%-18s %-36s %-34s %5s %7s %5s  %s"
        % ("classe", "peca", "contra", "cruz", "s", "fundo", "primeiro (t, x, z)")
    )
    for (classe, x, y), d in sorted(
        conta.pares.items(), key=lambda kv: (kv[0][0], -kv[1][0], kv[0])
    ):
        print(
            "%-18s %-36s %-34s %5d %7.1f %5.2f  %.1f, %.1f, %.1f"
            % ((classe, x, y, d[0], d[1], d[2]) + conta.onde[classe, x, y])
        )
    total = sum(por_classe.values())
    print(
        "POR CLASSE: "
        + (", ".join("%s = %d" % kv for kv in sorted(por_classe.items())) or "nenhum")
    )
    print("CRUZAMENTOS = %d em %d pares" % (total, len(conta.pares)))
    return 1 if total else 0


if __name__ == "__main__":
    sys.exit(main())
