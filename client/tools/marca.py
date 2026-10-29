"""Gera as versões de marca que entram no git a partir dos originais 4K (arte/marca/, fora do git).

Recorte: o fundo azul-marinho liso é removido por inundação a partir das bordas (só o que está
ligado à borda e perto da cor do fundo vira transparente), então o contorno escuro da logo fica.
uso: python client/tools/marca.py <arte/marca> client/Assets/_TA/Art/Marca <pasta de conferência>
(precisa de numpy e Pillow)
"""

import logging
import sys
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

TOL = 38  # ponytail: distância RGB ao fundo; subir se sobrar halo, descer se comer o contorno


def recorta(im: Image.Image) -> Image.Image:
    a = np.asarray(im.convert("RGB")).astype(np.int16)
    h, w, _ = a.shape
    borda = np.concatenate([a[0], a[-1], a[:, 0], a[:, -1]])
    fundo = np.median(borda, axis=0)
    perto = np.sqrt(((a - fundo) ** 2).sum(axis=2)) < TOL
    fora = np.zeros((h, w), bool)
    fila = deque((y, x) for y in (0, h - 1) for x in range(w) if perto[y, x])
    fila.extend((y, x) for x in (0, w - 1) for y in range(h) if perto[y, x])
    for y, x in fila:
        fora[y, x] = True
    while fila:
        y, x = fila.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and perto[ny, nx] and not fora[ny, nx]:
                fora[ny, nx] = True
                fila.append((ny, nx))
    alfa = np.where(fora, 0, 255).astype(np.uint8)
    # borda suave de 1 px: média 3x3 do alfa
    p = np.pad(alfa.astype(np.float32), 1, mode="edge")
    alfa = sum(p[dy : dy + h, dx : dx + w] for dy in range(3) for dx in range(3)) / 9
    rgba = np.dstack([a.astype(np.uint8), alfa.astype(np.uint8)])
    out = Image.fromarray(rgba, "RGBA")
    return out.crop(out.getbbox())


def main():
    src, dst, conf = (Path(p) for p in sys.argv[1:4])
    dst.mkdir(parents=True, exist_ok=True)
    conf.mkdir(parents=True, exist_ok=True)
    # a inundação é O(px): reduz para 2048 antes de recortar
    for nome, saida, lado in (
        ("logo_b", "logo_little_terminal", 2048),
        ("emblema", "emblema", 1024),
    ):
        im = Image.open(src / f"{nome}_4k.jpeg").resize((2048, 2048), Image.LANCZOS)
        r = recorta(im)
        r.thumbnail((lado, lado), Image.LANCZOS)
        r.save(dst / f"{saida}.png", optimize=True)
        xadrez = Image.new("RGBA", r.size, (255, 255, 255, 255))
        for y in range(0, r.size[1], 32):
            for x in range((y // 32) % 2 * 32, r.size[0], 64):
                xadrez.paste((200, 60, 200, 255), (x, y, x + 32, y + 32))
        xadrez.alpha_composite(r)
        xadrez.convert("RGB").resize((r.size[0] // 4, r.size[1] // 4)).save(
            conf / f"conf_{saida}.png"
        )
        logging.info("%s %s", saida, r.size)
    ic = (
        Image.open(src / "icone_app_a_4k.jpeg")
        .convert("RGB")
        .resize((1024, 1024), Image.LANCZOS)
    )
    ic.save(dst / "icone_app.png", optimize=True)
    sp = Image.open(src / "splash_b_4k.jpeg").convert("RGB")
    sp = sp.resize((1080, round(1080 * sp.size[1] / sp.size[0])), Image.LANCZOS)
    sp.save(dst / "splash.jpg", quality=90, optimize=True)
    logging.info("icone_app %s splash %s", ic.size, sp.size)


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO, format="%(message)s")
    main()
