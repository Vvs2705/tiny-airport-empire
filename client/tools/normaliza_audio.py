"""Normaliza o volume dos efeitos e da musica (Resources/Audio) com o ffmpeg, para nenhum som do jogo sair muito mais alto ou baixo que os outros.

Alvo: efeitos em -18 LUFS, musica em -22 LUFS, pico nunca acima de -1 dBFS. Som curto demais para medir LUFS (os toques, < 0,4 s)
vai pelo pico, a -4 dBFS. Ganho so por fator (sem compressor): o timbre fica igual. Regrava no mesmo formato, so se o ganho passar de 0,5 dB.

Uso: python client/tools/normaliza_audio.py [--medir]      (precisa do ffmpeg no PATH)
"""

import os, re, subprocess, sys, tempfile

RAIZ = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..",
    "Assets",
    "_TA",
    "Resources",
    "Audio",
)
ALVO = {"sfx": -18.0, "musica": -22.0}
TETO, PICO_CURTO, FOLGA = -1.0, -4.0, 0.5


def medir(arq):
    """(LUFS integrado ou None se curto demais, pico verdadeiro em dBFS)."""
    err = subprocess.run(
        [
            "ffmpeg",
            "-hide_banner",
            "-nostats",
            "-i",
            arq,
            "-af",
            "ebur128=peak=true",
            "-f",
            "null",
            "-",
        ],
        capture_output=True,
        text=True,
    ).stderr
    i = float(re.findall(r"I:\s+(-?[\d.]+) LUFS", err)[-1])
    p = float(re.findall(r"Peak:\s+(-?[\d.inf]+) dBFS", err)[-1])
    return (None if i <= -69.0 else i), p


def ganho(lufs, pico, alvo):
    g = PICO_CURTO - pico if lufs is None else alvo - lufs
    return min(g, TETO - pico)  # nunca estoura o teto de pico


def regrava(arq, g):
    ext = os.path.splitext(arq)[1]
    cod = (
        ["-c:a", "libmp3lame", "-q:a", "2"] if ext == ".mp3" else ["-c:a", "pcm_s16le"]
    )
    fd, tmp = tempfile.mkstemp(suffix=ext)
    os.close(fd)
    subprocess.run(
        [
            "ffmpeg",
            "-y",
            "-hide_banner",
            "-loglevel",
            "error",
            "-i",
            arq,
            "-af",
            f"volume={g:.2f}dB",
            "-map_metadata",
            "-1",
            *cod,
            tmp,
        ],
        check=True,
    )
    os.replace(tmp, arq)


def main():
    so_medir = "--medir" in sys.argv
    for pasta, alvo in ALVO.items():
        for nome in sorted(os.listdir(os.path.join(RAIZ, pasta))):
            if nome.endswith(".meta"):
                continue
            arq = os.path.join(RAIZ, pasta, nome)
            lufs, pico = medir(arq)
            g = ganho(lufs, pico, alvo)
            mexe = not so_medir and abs(g) > FOLGA
            if mexe:
                regrava(arq, g)
            print(
                f"{pasta}/{nome}: {'curto' if lufs is None else f'{lufs:.1f} LUFS'}, pico {pico:.1f} dBFS, ganho {g:+.1f} dB{' (aplicado)' if mexe else ''}"
            )


if __name__ == "__main__":
    assert (
        ganho(-30.0, -20.0, -18.0) == 12.0
        and ganho(-15.0, -0.5, -18.0) == -3.0
        and ganho(None, -13.0, -18.0) == 9.0
    )
    assert ganho(-30.0, -5.0, -18.0) == 4.0  # o pico segura o ganho
    main()
