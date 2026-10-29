"""Montagem de um -record do Tiny Airport: folha de contato (PNG) e GIF curto a partir dos quadros f_00000.png ... da pasta.

Uso (da raiz do repositorio):
  python client/tools/montagem.py client/Builds/validation_v01/record_mover --saida client/Builds/validation_v01/mover
  -> mover_folha.png (12 quadros espalhados, numerados com o tempo) e mover.gif (15 qps, metade do tamanho)
Opcoes: --quadros 12 (folha) | --qps 15 (GIF) | --escala 0.5 (GIF). So o Pillow.
"""

import argparse
import glob
import os
import sys

from PIL import Image, ImageDraw


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("pasta")
    ap.add_argument("--saida", required=True, help="prefixo dos arquivos gerados")
    ap.add_argument("--quadros", type=int, default=12)
    ap.add_argument("--qps", type=int, default=15)
    ap.add_argument("--escala", type=float, default=0.5)
    a = ap.parse_args()

    arqs = sorted(glob.glob(os.path.join(a.pasta, "f_*.png")))
    if not arqs:
        sys.stderr.write("nenhum quadro f_*.png em " + a.pasta + "\n")
        return 1
    n = max(1, min(a.quadros, len(arqs)))
    escolhidos = [arqs[round(i * (len(arqs) - 1) / max(1, n - 1))] for i in range(n)]
    primeiro = Image.open(escolhidos[0])
    w, h = int(primeiro.width * 0.5), int(primeiro.height * 0.5)
    cols = 4 if n > 6 else n
    linhas = (n + cols - 1) // cols
    folha = Image.new("RGB", (cols * w, linhas * h), (14, 21, 38))
    for i, arq in enumerate(escolhidos):
        q = Image.open(arq).convert("RGB").resize((w, h))
        d = ImageDraw.Draw(q)
        t = int(os.path.basename(arq)[2:7]) / 30.0
        d.rectangle([0, 0, 92, 26], fill=(0, 0, 0))
        d.text((6, 6), f"{t:4.1f} s", fill=(255, 213, 74))
        folha.paste(q, ((i % cols) * w, (i // cols) * h))
    folha.save(a.saida + "_folha.png", optimize=True)

    passo = max(1, round(30 / a.qps))
    gw, gh = int(primeiro.width * a.escala), int(primeiro.height * a.escala)
    gif = [
        Image.open(f).convert("RGB").resize((gw, gh)).quantize(colors=64)
        for f in arqs[::passo]
    ]
    gif[0].save(
        a.saida + ".gif",
        save_all=True,
        append_images=gif[1:],
        duration=int(1000 / a.qps),
        loop=0,
        optimize=True,
    )
    sys.stdout.write(
        f"{a.saida}_folha.png ({n} quadros) e {a.saida}.gif ({len(gif)} quadros)\n"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
