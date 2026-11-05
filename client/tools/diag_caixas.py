"""Conta os cruzamentos do patio a partir de um ou mais CSV do -diag (Mundo.Patio.cs, DiagCaixas): caixa de cada peca contra a trajetoria
de cada veiculo, aviao e pessoa, com a altura. Roda fora do jogo: nenhuma fisica de motor, nenhum colisor em tempo de jogo.

Uso: python client/tools/diag_caixas.py arq.csv [arq2.csv ...]     (sai 1 se houver cruzamento)
     python client/tools/diag_caixas.py --selftest

Conta um cruzamento por encontro (2 pecas sobrepostas em amostras seguidas), mais fundo que TOLERANCIA:
  veiculo x peca, veiculo x veiculo, veiculo x aviao, aviao x peca, aviao x aviao e pessoa x veiculo.
Nas salas: pessoa x parede e pessoa x movel, por sala e por peca, com o corpo do adulto (circulo de CORPO_RAIO) contra as pecas ligadas naquele
momento (liga#). Nao conta quem esta sentado, contra a cadeira, nem quem esta parado no posto e so encosta: la, so conta com o centro dentro da peca (no vao de um balcao OCAS, o funcionario nunca conta: e o lugar de trabalho dele).
Cobertura: quantas vezes cada peca saiu do lugar e voltou (ida e volta) e por onde passou; o par so vale se a rota rodou.
A caixa de um veiculo e a da malha. As de aviao, garagem e rebocador preso sao as tabelas abaixo, medidas nos cortes das malhas.
"""

import argparse
import csv
import math
import sys
from collections import defaultdict

TOLERANCIA = 0.02  # m: menos que isso e o contorno da malha, nao um cruzamento
PESSOAS = ("pax", "func", "equipe", "ator", "trip", "mec", "limp")
PESSOA_LADO, PESSOA_ALTURA = 0.4, 1.2
CORPO_RAIO, CORPO_ALTURA = 0.1, 0.5  # adulto de 0,5 m e 0,2 m de ombros (Mundo.Gente)
CADEIRAS = ("cadeiras_espera", "Cadeira")
OCAS = ("bar_lounge", "bancada_oficina", "vitrine_loja")  # balcao com prateleira ou painel atras e o quiosque da loja (nivel 3): quem trabalha fica no vao, dentro da caixa
FINAS = ("cordao_fila",)  # a caixa e a da diagonal da malha, mais larga que o corredor da fila: encostar nao conta, so o centro dentro (atravessou)
# a gente passa por dentro (portico, cabine) ou a peca anda com ela (Mundo.Seguranca, Mundo.Malas): nao e obstaculo
SEM_CORPO = ("portico_detector", "cabine_scanner_corpo", "bandeja_seguranca", "Bandeja", "scanner_mao", "mala_rodinha", "Mala", "MalaSeg", "Pilha")

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
    # as outras 6 variantes (Mundo.Patio.cs, Modelos), na medida do jogo: geradas dos voxels de 0,1 m da malha (colunas de 0,2 m com as faixas
    # de altura, trem e asa separados, juntas ate 16 caixas sem que a caixa juntada toque um veiculo parado na vaga). O que fica a menos de
    # 1,7 m da ponta e nariz.
    "aviao_bimotor_helice": [
        ("nariz1", 2.81, 3.58, -0.72, 0.72, 0.71, 1.81),
        ("nariz2", 0.2, 2.82, -1.23, 1.23, 0.54, 2.49),
        ("nariz3", 1.63, 2.4, -0.39, 0.39, 0.0, 0.72),
        ("nariz4", 1.63, 1.98, -1.9, 1.9, 1.88, 2.49),
        ("parte1", 1.38, 1.65, -2.4, 2.4, 1.46, 2.82),
        ("parte2", 0.62, 1.39, -2.23, 2.23, 1.55, 2.57),
        ("parte3", -0.05, 1.23, -1.98, 1.98, 1.55, 1.81),
        ("parte4", -0.39, 0.97, -2.23, 2.23, 2.39, 2.65),
        ("parte5", -0.55, 0.89, -3.83, 3.83, 2.05, 2.49),
        ("parte6", -0.39, 0.64, -2.23, 2.23, 1.46, 2.15),
        ("parte7", -0.39, 0.39, -1.31, 1.31, 0.0, 1.81),
        ("parte8", -2.23, -0.2, -1.06, 1.06, 0.54, 2.49),
        ("parte9", -0.55, -0.37, -2.82, 2.82, 1.71, 2.49),
        ("parte10", -3.16, -1.97, -1.65, 1.65, 1.8, 2.32),
        ("parte11", -3.58, -2.22, -0.39, 0.39, 1.46, 3.07),
        ("parte12", -3.24, -2.22, -0.81, 0.81, 0.79, 2.57),
    ],
    "aviao_jato_regional": [
        ("nariz1", 0.61, 3.74, -0.87, 0.87, 0.52, 2.35),
        ("nariz2", 2.41, 3.0, -0.38, 0.38, 0.0, 0.79),
        ("nariz3", -1.77, 2.43, -0.62, 0.62, 2.0, 2.43),
        ("parte1", -1.03, 1.44, -0.79, 0.79, 1.51, 2.1),
        ("parte2", 0.36, 0.79, -1.03, 1.03, 0.52, 1.77),
        ("parte3", -1.03, 0.62, -2.43, 2.43, 0.77, 1.28),
        ("parte4", -0.38, 0.38, -0.79, 0.79, 0.44, 0.87),
        ("parte5", -0.62, 0.38, -1.03, 1.03, 1.18, 1.77),
        ("parte6", -1.53, -0.2, -3.74, 3.74, 0.93, 1.28),
        ("parte7", -1.03, -0.36, -1.28, 1.28, 0.0, 1.77),
        ("parte8", -1.77, -1.02, -1.03, 1.03, 0.52, 2.1),
        ("parte9", -2.18, -1.1, -1.53, 1.53, 1.34, 2.1),
        ("parte10", -2.18, -1.43, -0.38, 0.38, 2.16, 2.59),
        ("parte11", -3.17, -1.59, -0.79, 0.79, 0.61, 2.35),
        ("parte12", -3.74, -2.16, -0.21, 0.21, 1.43, 3.49),
        ("parte13", -3.74, -2.66, -1.12, 1.12, 2.98, 3.33),
    ],
    "jato_executivo": [
        ("nariz1", 2.82, 3.74, -0.71, 0.71, 0.93, 2.26),
        ("nariz2", 2.0, 2.84, -0.46, 0.46, 0.0, 1.12),
        ("nariz3", 1.18, 2.84, -0.95, 0.95, 0.85, 2.76),
        ("nariz4", 0.2, 2.02, -1.03, 1.03, 0.77, 1.36),
        ("parte1", -0.79, 1.77, -0.62, 0.62, 2.41, 2.76),
        ("parte2", -0.62, 1.2, -0.79, 0.79, 2.0, 2.59),
        ("parte3", 0.77, 1.2, -1.12, 1.12, 1.1, 2.35),
        ("parte4", -0.71, 1.03, -3.74, 3.74, 1.1, 1.61),
        ("parte5", -0.38, 0.79, -1.03, 1.03, 1.43, 2.35),
        ("parte6", -0.54, 0.3, -1.94, 1.94, 0.0, 1.53),
        ("parte7", -1.2, -0.36, -1.03, 1.03, 0.77, 2.67),
        ("parte8", -2.35, -0.61, -1.69, 1.69, 1.67, 2.59),
        ("parte9", -3.74, -1.02, -0.21, 0.21, 1.84, 3.99),
        ("parte10", -2.02, -1.18, -0.79, 0.79, 0.93, 2.43),
        ("parte11", -3.08, -2.0, -0.62, 0.62, 1.26, 2.51),
        ("parte12", -3.58, -2.33, -1.53, 1.53, 3.39, 3.74),
    ],
    "aviao_wide": [
        ("nariz1", 3.15, 4.55, -0.65, 0.65, 0.95, 2.55),
        ("nariz2", 2.95, 3.65, -0.35, 0.35, 0.0, 1.15),
        ("nariz3", 1.05, 3.45, -0.75, 0.75, 0.95, 2.15),
        ("nariz4", -2.05, 3.15, -0.65, 0.65, 1.75, 2.65),
        ("parte1", 1.05, 1.85, -2.65, 2.65, 0.55, 1.75),
        ("parte2", -0.25, 1.05, -0.75, 0.75, 1.65, 2.15),
        ("parte3", -0.05, 1.05, -2.65, 2.65, 0.65, 1.95),
        ("parte4", -1.25, 0.85, -4.55, 4.55, 1.55, 2.05),
        ("parte5", -0.85, 0.35, -1.65, 1.65, 0.0, 0.55),
        ("parte6", -0.25, 0.15, -1.45, 1.45, 0.0, 1.85),
        ("parte7", -0.65, -0.05, -2.45, 2.45, 1.25, 1.95),
        ("parte8", -2.65, -0.25, -0.75, 0.75, 0.85, 2.15),
        ("parte9", -4.55, -0.65, -0.15, 0.15, 2.35, 4.45),
        ("parte10", -1.05, -0.65, -3.45, 3.45, 1.45, 1.95),
        ("parte11", -4.55, -2.05, -0.65, 0.65, 1.05, 2.65),
        ("parte12", -4.55, -3.15, -1.75, 1.75, 2.05, 2.35),
    ],
    "aviao_wide_quad": [
        ("nariz1", 3.05, 4.35, -0.85, 0.85, 0.45, 2.35),
        ("nariz2", 1.45, 3.15, -0.65, 0.65, 0.0, 0.95),
        ("nariz3", -2.15, 3.15, -0.75, 0.75, 1.65, 2.45),
        ("nariz4", 1.85, 3.05, -0.85, 0.85, 0.75, 2.05),
        ("parte1", 1.25, 1.85, -1.15, 1.15, 0.65, 1.85),
        ("parte2", 0.65, 1.45, -2.15, 2.15, 0.15, 1.25),
        ("parte3", -0.65, 1.25, -1.05, 1.05, 1.05, 1.75),
        ("parte4", -1.25, 0.75, -3.65, 3.65, 0.25, 1.25),
        ("parte5", -0.45, 0.65, -1.05, 1.05, 0.0, 0.75),
        ("parte6", 0.35, 0.65, -1.85, 1.85, 0.15, 0.45),
        ("parte7", -1.75, -0.15, -4.55, 4.55, 0.95, 1.25),
        ("parte8", -2.85, -0.65, -0.35, 0.35, 0.35, 0.85),
        ("parte9", -2.15, -0.65, -1.05, 1.05, 0.45, 1.85),
        ("parte10", -3.95, -2.15, -0.75, 0.75, 0.65, 2.45),
        ("parte11", -4.25, -2.15, -2.05, 2.05, 1.25, 1.65),
        ("parte12", -4.35, -2.45, -0.35, 0.35, 1.15, 3.55),
    ],
    "aviao_cargo": [
        ("nariz1", 0.75, 3.95, -1.05, 1.05, 0.55, 2.85),
        ("nariz2", 2.15, 2.95, -0.65, 0.65, 0.0, 0.95),
        ("parte1", 1.55, 1.65, -2.05, 2.05, 1.65, 2.45),
        ("parte2", 1.55, 1.65, -2.15, 2.15, 2.15, 2.45),
        ("parte3", 1.55, 1.65, -2.15, 2.15, 1.65, 1.95),
        ("parte4", 1.55, 1.65, -2.35, 2.35, 1.75, 2.35),
        ("parte5", 0.75, 1.55, -2.45, 2.45, 1.65, 2.85),
        ("parte6", -0.55, 1.05, -1.35, 1.35, 0.0, 1.55),
        ("parte7", -0.45, 0.95, -4.55, 4.55, 2.35, 2.85),
        ("parte8", -0.25, 0.75, -2.45, 2.45, 1.35, 2.45),
        ("parte9", -1.65, -0.25, -1.25, 1.25, 0.55, 2.85),
        ("parte10", -3.45, -0.45, -0.75, 0.75, 2.45, 2.85),
        ("parte11", -3.85, -1.25, -0.15, 0.15, 2.15, 4.35),
        ("parte12", -2.85, -1.65, -0.95, 0.95, 0.05, 2.65),
        ("parte13", -3.95, -2.65, -1.05, 1.05, 1.45, 2.85),
        ("parte14", -3.55, -2.65, -1.95, 1.95, 2.35, 2.65),
    ],
}
# a vaga de cada variante: G (ponte, rebocador no nariz) e R (escada, passarela e porta do jato)
VAGA_G = ("aviao_regional", "aviao_bimotor_helice", "aviao_jato_regional", "jato_executivo")
VAGA_R = ("aviao_narrow", "aviao_wide", "aviao_wide_quad", "aviao_cargo")

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

# contato que e o servico: a barra do rebocador no nariz, a ponte na porta e, no remoto, a passarela e a porta (so aparecem com a escada encostada)
LIGADOS = (
    tuple(("rebocador", g + ".nariz") for g in VAGA_G)
    + tuple((g, "ponte_embarque") for g in VAGA_G)
    + tuple((r, p) for r in VAGA_R for p in ("PortaJato", "Passarela"))
    + (("escada_embarque", "Passarela"),)
)
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


def dentro(c, x, z):
    """Quanto o ponto esta dentro da caixa, no plano, em m: negativo = fora, a essa distancia."""
    dx, dz = x - c.cx, z - c.cz
    ex = abs(dx * c.ux + dz * c.uz) - c.hl
    ez = abs(dx * c.uz - dz * c.ux) - c.hw
    return -math.hypot(ex, ez) if ex > 0.0 and ez > 0.0 else -max(ex, ez)


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


def sala_de(salas, x, z):
    for e, (x0, z0, lado) in salas.items():
        if x0 <= x <= x0 + lado and z0 <= z <= z0 + lado:
            return e
    return -1


def corpo(conta, salas, pecas, nome, x, z, sentado, posto, t, dt):
    """Uma amostra de uma pessoa contra as pecas ligadas da sala em que ela esta (pecas: sala -> [(id, Caixa)])."""
    e = sala_de(salas, x, z)
    if e < 0:
        return
    conta.x, conta.z = x, z
    base = nome.split("#")[0]
    for i, c in pecas.get(e, ()):
        d = dentro(c, x, z)
        p = d if posto or c.nome in FINAS else d + CORPO_RAIO
        if p <= TOLERANCIA or (sentado and c.nome in CADEIRAS) or (base == "func" and c.nome in OCAS):
            continue
        classe = "pessoa x parede" if c.nome == "Parede" else "pessoa x movel"
        conta.anota(classe, "sala%d %s" % (e, base), c.nome, (nome, i), t, p, dt)


def mede(arquivo, conta, cobertura):
    fixas, medidas, por_t = [], {}, defaultdict(list)
    salas, por_id, liga = {}, {}, []
    with open(arquivo, newline="") as f:
        for r in csv.DictReader(f):
            tipo, _, nome = r["peca"].partition("#")
            if tipo == "fixa":
                vals = [float(r[c]) for c in ("x", "y", "z", "yaw", "sx", "sy", "sz")]
                por_id[int(r["id"])] = partes_fixas(nome, *vals)
                fixas += por_id[int(r["id"])]
            elif tipo == "sala":
                salas[int(nome)] = (float(r["x"]), float(r["z"]), float(r["sx"]))
            elif tipo == "liga":
                liga.append((float(r["t"]), int(nome), r["sx"] == "1"))
            elif tipo == "movel":
                medidas[nome] = [float(r[c]) for c in ("x", "y", "z", "sx", "sy", "sz")]
            elif tipo not in ("balcao", "cadeira"):
                por_t[float(r["t"])].append(r)
    tempos = sorted(por_t)
    casa, longe, em_casa = {}, defaultdict(int), {}
    antes, parada = {}, set()
    pecas = defaultdict(list)  # sala -> pecas ligadas que barram gente
    acesas = set()  # as caixas ligadas agora: cone de incidente, nivel de predio e sala fechada nao batem em ninguem
    obstaculos = {
        i: [c for c in cs if c.y0 < CORPO_ALTURA and c.y1 > TOLERANCIA and c.nome.split(".")[0] not in SEM_CORPO]
        for i, cs in por_id.items()
    }
    prox = 0
    for i, t in enumerate(tempos):
        dt = t - tempos[i - 1] if i else 0.1
        moveis, gente = [], []
        while prox < len(liga) and liga[prox][0] <= t:
            _, ident, ligou = liga[prox]
            prox += 1
            (acesas.update if ligou else acesas.difference_update)(por_id.get(ident, ()))
            for c in obstaculos.get(ident, ()):
                for e, (x0, z0, lado) in salas.items():
                    if x0 - c.r <= c.cx <= x0 + lado + c.r and z0 - c.r <= c.cz <= z0 + lado + c.r:
                        par = (ident, c)
                        if ligou:
                            pecas[e].append(par)
                        elif par in pecas[e]:
                            pecas[e].remove(par)
        for r in por_t[t]:
            peca = r["peca"]
            base = peca.split("#")[0]
            x, y, z, yaw = float(r["x"]), float(r["y"]), float(r["z"]), float(r["yaw"])
            if base in PESSOAS:
                lado = PESSOA_LADO / 2
                gente.append(
                    (peca, Caixa(base, x, z, 0.0, lado, lado, 0.0, PESSOA_ALTURA))
                )
                if salas:
                    corpo(conta, salas, pecas, peca, x, z, r["sx"] == "1", r["sy"] == "1", t, dt)
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
                for f in acesas:
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
    assert set(VAGA_G + VAGA_R) == set(AVIAO), "toda variante do patio tem as caixas"
    assert ligado("rebocador", "aviao_jato_regional.nariz2") and not ligado("rebocador", "aviao_wide.nariz1")
    assert abs(dentro(a, 0, 0) - 0.5) < 1e-9, "no meio: 0,5 da borda mais perto"
    assert abs(dentro(a, 0, 1.5) + 0.5) < 1e-9, "fora, de frente"
    assert abs(dentro(a, 1.5, 2.0) + math.hypot(1.0, 1.0)) < 1e-9, "fora, pela quina"
    salas, parede = {0: (0.0, 0.0, 4.0)}, Caixa("Parede", 1, 1, 0, 1, 0.04, 0, 0.34)
    c = Conta()
    corpo(c, salas, {0: [(1, parede)]}, "pax#1", 1.1, 1.0, False, False, 1.0, 0.1)
    corpo(c, salas, {0: [(1, parede)]}, "func#1", 1.1, 1.0, False, True, 1.0, 0.1)
    assert list(c.pares) == [("pessoa x parede", "sala0 pax", "Parede")], "parado no posto so encosta"
    c = Conta()
    corpo(c, salas, {0: [(1, Caixa("cadeiras_espera", 1, 1, 0, 0.4, 0.1, 0, 0.3))]}, "pax#1", 1.0, 1.0, True, False, 1.0, 0.1)
    corpo(c, salas, {0: [(1, parede)]}, "pax#1", 9.0, 9.0, False, False, 1.0, 0.1)
    assert not c.pares, "sentado na cadeira e fora das salas nao contam"
    c = Conta()
    bar = Caixa("bar_lounge", 1, 1, 0, 0.47, 0.34, 0, 0.64)
    corpo(c, salas, {0: [(1, bar)]}, "func#1", 1.2, 1.0, False, True, 1.0, 0.1)
    corpo(c, salas, {0: [(1, bar)]}, "pax#1", 1.2, 1.0, False, False, 1.0, 0.1)
    assert list(c.pares) == [("pessoa x movel", "sala0 pax", "bar_lounge")], "no vao do proprio balcao so o funcionario no posto"
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
