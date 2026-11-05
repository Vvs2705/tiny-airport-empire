"""Ficha da Google Play do Little Terminal: confere os textos de docs/FICHA_PLAY.md e gera o que sai do repositório publico.

uso (Pillow so nos tres ultimos):
  python client/tools/ficha_play.py check                         limites de tamanho dos textos (titulo 30, curta 80, longa 4000) e palavras proibidas
  python client/tools/ficha_play.py icone                         docs/play/icone_512.png (512x512, de Art/Marca/icone_app.png) e a previa circular
  python client/tools/ficha_play.py grafico                       docs/play/grafico_1024x500.png (1024x500, sem alfa), do splash e da logo
  python client/tools/ficha_play.py capturas <entrada> <saida>    cada captura 1080x1920 da entrada vai reduzida abaixo de uma faixa com a legenda (pt-BR) para a saida
So marca ja publicada entra aqui. As capturas com as malhas do Tripo ficam em client/Builds (fora do git).
"""

import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[2]
FICHA = RAIZ / "docs" / "FICHA_PLAY.md"
MARCA = RAIZ / "client" / "Assets" / "_TA" / "Art" / "Marca"
SAIDA = RAIZ / "docs" / "play"
FONTE = RAIZ / "client" / "Assets" / "_TA" / "Resources" / "Fonts" / "LilitaOne-Regular.ttf"  # a fonte de titulo do jogo (OFL)
FUNDO, AMARELO = (20, 22, 28), (255, 210, 63)  # as cores do quadro de partidas (ART_BIBLE.md)
CAPAS = ["1_geral", "2_equipe", "3_ficha", "4_patio", "5_rotas", "6_metas", "7_melhorias", "8_passe"]
LEGENDAS = ["Gerencie um aeroporto inteiro", "Sua equipe é pouca: mova com cuidado", "Toque e veja o que pesa no humor",
            "Do pouso à decolagem", "3 aeroportos e 12 rotas", "Objetivos, missões e conquistas",
            "Melhore cada sala do terminal", "Prêmios por jogar, sem pressa"]


def check() -> int:
    """Os mesmos limites e palavras do checa_repo (uma regra so), com o tamanho de cada texto."""
    sys.path.insert(0, str(Path(__file__).parent))
    import checa_repo as cr

    md = FICHA.read_text(encoding="utf-8")
    for (tipo, lingua), s in sorted(cr.textos_ficha(md).items()):
        print(f"{tipo}_{lingua}: {len(s)}/{cr.LIMITES_FICHA[tipo]}")
    erros = cr.checa_ficha({cr.FICHA: md.encode("utf-8")})
    for e in erros:
        print("FALHOU", e)
    return len(erros)


def icone() -> int:
    from PIL import Image, ImageDraw

    im = Image.open(MARCA / "icone_app.png").convert("RGBA").resize((512, 512), Image.LANCZOS)
    SAIDA.mkdir(parents=True, exist_ok=True)
    im.save(SAIDA / "icone_512.png", optimize=True)
    # previa: o recorte circular do launcher (zona segura = 66% central), so para conferir a olho
    prev = Image.new("RGBA", (512, 512), (255, 255, 255, 255))
    mask = Image.new("L", (512, 512), 0)
    ImageDraw.Draw(mask).ellipse((0, 0, 511, 511), fill=255)
    prev.paste(im, (0, 0), mask)
    d = ImageDraw.Draw(prev)
    c = 512 * 0.66 / 2
    d.ellipse((256 - c, 256 - c, 256 + c, 256 + c), outline=(255, 255, 255, 200), width=2)
    prev.convert("RGB").save(SAIDA / "previa_icone.png", optimize=True)
    kb = (SAIDA / "icone_512.png").stat().st_size / 1024
    print(f"icone_512.png 512x512, {kb:.0f} KB (limite 1024 KB)")
    return 0 if kb <= 1024 else 1


def grafico() -> int:
    from PIL import Image, ImageFilter

    W, H = 1024, 500
    sp = Image.open(MARCA / "splash.jpg").convert("RGB")
    # o splash 9:16 vira fundo: recorta uma faixa 1024:500 do meio (o ceu e o terminal) e desfoca de leve
    s = W / sp.width
    sp = sp.resize((W, round(sp.height * s)), Image.LANCZOS)
    top = max(0, round(sp.height * 0.52) - H // 2)
    fundo = sp.crop((0, top, W, top + H)).filter(ImageFilter.GaussianBlur(2))
    logo = Image.open(MARCA / "logo_little_terminal.png").convert("RGBA")
    lh = 330
    logo = logo.resize((round(logo.width * lh / logo.height), lh), Image.LANCZOS)
    fundo = fundo.convert("RGBA")
    fundo.paste(logo, (W // 2 - logo.width // 2, (H - lh) // 2), logo)
    SAIDA.mkdir(parents=True, exist_ok=True)
    fundo.convert("RGB").save(SAIDA / "grafico_1024x500.png", optimize=True)
    print(f"grafico_1024x500.png {W}x{H}, {(SAIDA / 'grafico_1024x500.png').stat().st_size / 1024:.0f} KB")
    return 0


def capturas(entrada: str, saida: str) -> int:
    """Cada captura crua (1080x1920) vai inteira, reduzida, abaixo de uma faixa de legenda: nada da HUD fica coberto."""
    from PIL import Image, ImageDraw, ImageFont

    ent, sai = Path(entrada), Path(saida)
    sai.mkdir(parents=True, exist_ok=True)
    W, H, FAIXA = 1080, 1920, 230
    erros = 0
    for nome, leg in zip(CAPAS, LEGENDAS):
        p = ent / f"{nome}.png"
        if not p.exists():
            print(f"FALHOU: falta {p}")
            erros += 1
            continue
        im = Image.open(p).convert("RGB")
        if im.size != (W, H):
            print(f"FALHOU: {p.name} tem {im.size}, tem de ser {W}x{H}")
            erros += 1
            continue
        tela = Image.new("RGB", (W, H), FUNDO)
        d = ImageDraw.Draw(tela)
        tam = 76
        while True:  # a legenda cabe em uma linha, com 48 px de margem de cada lado
            fonte = ImageFont.truetype(str(FONTE), tam)
            if d.textlength(leg, font=fonte) <= W - 96 or tam <= 40:
                break
            tam -= 2
        x0, y0, x1, y1 = d.textbbox((0, 0), leg, font=fonte)
        d.text(((W - (x1 - x0)) / 2 - x0, (FAIXA - (y1 - y0)) / 2 - y0), leg, font=fonte, fill=AMARELO)
        h = H - FAIXA - 30
        w = round(W * h / H)
        tela.paste(im.resize((w, h), Image.LANCZOS), ((W - w) // 2, FAIXA))
        tela.save(sai / f"{nome}.png", optimize=True)
        print(f"OK   {nome}.png {W}x{H}, legenda {tam} px")
    return erros


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "check":
        sys.exit(1 if check() else 0)
    if cmd == "icone":
        sys.exit(icone())
    if cmd == "grafico":
        sys.exit(grafico())
    if cmd == "capturas" and len(sys.argv) == 4:
        sys.exit(1 if capturas(sys.argv[2], sys.argv[3]) else 0)
    print(__doc__)
    sys.exit(2)
