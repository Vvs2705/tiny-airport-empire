"""Banco de audio do Tiny Airport (GDD §12): todo som e toda musica sintetizados aqui, sem asset de terceiros nem servico externo.
So numpy + scipy. Deterministico: cada som usa uma semente fixa (crc32 do nome). Copia adaptada de dc:client/tools/audio_gen.py.

Uso:  python client/tools/audio_gen.py          gera tudo e confere (pico, DC, laco)
      python client/tools/audio_gen.py --md     idem e imprime as linhas do PROVENIENCIA §3.1

Saida (client/Assets/_TA/Resources/Audio/):
  sfx/<nome>_<n>.wav    44,1 kHz mono 16 bit, silencio inicial cortado, loudness por categoria (LUFS momentaneo maximo, aprox.)
  musica/<faixa>.wav    22,05 kHz mono 16 bit, laco exato (as caudas dao a volta no buffer), -22 LUFS integrado (BS.1770)
  *.meta                import do Unity (SFX: Decompress On Load + Vorbis; musica: Streaming + Vorbis), criado so se faltar.
ponytail: musica em WAV e nao OGG (sem encoder Vorbis na stdlib/scipy); o Unity comprime em Vorbis no import.
"""

import hashlib
import sys
import wave
import zlib
from pathlib import Path

import numpy as np
from scipy import signal

OUT = Path(__file__).resolve().parents[1] / "Assets" / "_TA" / "Resources" / "Audio"
SR, MSR = 44100, 22050
PEAK_DB = -1.5  # teto de pico verdadeiro (4x), abaixo do -1 dBFS pedido
MUSIC_LUFS = -22.0


# ------------------------------------------------------------------ blocos de sintese


def midi(m):
    return 440.0 * 2.0 ** ((m - 69) / 12.0)


def n_of(d, sr=SR):
    return max(1, int(round(d * sr)))


def ph(freq, sr=SR):
    return 2.0 * np.pi * np.cumsum(freq) / sr


def osc(freq, kind="sine", sr=SR, duty=0.5, hmax=40):
    """Oscilador limitado em banda (aditivo): sine, tri, square, saw, pulse. freq: escalar ou array por amostra."""
    freq = np.asarray(freq, float)
    p = ph(freq, sr)
    if kind == "sine":
        return np.sin(p)
    out = np.zeros_like(p)
    for k in range(1, hmax + 1):
        if kind in ("tri", "square") and k % 2 == 0:
            continue
        if kind == "tri":
            a, w = (8 / np.pi**2) * (-1) ** ((k - 1) // 2) / k**2, np.sin(k * p)
        elif kind == "square":
            a, w = (4 / np.pi) / k, np.sin(k * p)
        elif kind == "saw":
            a, w = (2 / np.pi) / k, np.sin(k * p)
        else:  # pulse sem DC
            a, w = (2 / np.pi) * np.sin(np.pi * k * duty) / k, np.cos(k * p)
        mask = k * freq < 0.45 * sr
        if not mask.any():
            break
        out += a * w * mask
    return out


def _sos(kind, f, sr, order=2):
    return signal.butter(order, f, kind, fs=sr, output="sos")


def lp(x, f, sr=SR, order=2):
    return signal.sosfilt(_sos("lowpass", min(f, 0.45 * sr), sr, order), x)


def hp(x, f, sr=SR, order=2):
    return signal.sosfilt(_sos("highpass", f, sr, order), x)


def bp(x, lo, hi, sr=SR, order=2):
    return signal.sosfilt(_sos("bandpass", [lo, min(hi, 0.45 * sr)], sr, order), x)


def svf(x, fc, q=0.7, mode="bp", sr=SR):
    """Filtro de estado variavel (Chamberlin) com corte variando por amostra (ronco do motor, afastamento do aviao)."""
    fc = np.broadcast_to(np.asarray(fc, float), x.shape)
    f = (2.0 * np.sin(np.pi * np.minimum(fc, sr / 6.5) / sr)).tolist()
    damp, low, band, out = 1.0 / q, 0.0, 0.0, []
    for xi, fi in zip(x.tolist(), f):
        high = xi - low - damp * band
        band += fi * high
        low += fi * band
        out.append(band if mode == "bp" else low)
    return np.array(out)


def unit(x):
    """Pico 1: deixa explicita a proporcao entre ruidos filtrados (svf/bp mudam muito o nivel)."""
    return x / (np.max(np.abs(x)) + 1e-12)


# ------------------------------------------------------------------ motor de aviao (o risco do GDD §12)


def lento(rng, n, f):
    """Ruido lento em torno de 0, pico 1: o que tira o motor do tom de sintetizador (giro e ar nunca ficam parados)."""
    return unit(lp(rng.standard_normal(n), f))


def chao(x, atraso):
    """Eco no chao com atraso variando (s): o filtro de pente que varre o som de todo aviao que passa."""
    i = np.arange(len(x))
    return x + 0.7 * np.interp(i - atraso * SR, i, x, left=0.0)


def afasta(x, de, doppler):
    """Aviao indo embora a partir de `de` s: a altura cai `doppler`, o ar come os agudos e o volume desce."""
    n = len(x)
    u = np.clip((np.arange(n) / SR - de) / (n / SR - de), 0, 1)
    u = u * u * (3 - 2 * u)
    y = np.interp(np.cumsum(1 - doppler * u), np.arange(n), x)
    return svf(y, 1100 + 5600 * (1 - u) ** 2, q=0.7, mode="lp") * (1 - u) ** 1.4


def s_jato(rng, v):
    """Decolagem de jato: ronco do escapamento, rasgo das pas da ventoinha e apito da turbina subindo com a rotacao."""
    p, n = (1.0, 0.94)[v], n_of(3.4)
    t = np.arange(n) / SR
    rot = (0.35 + 0.65 * (1 - np.exp(-t / 0.7))) * (1 + 0.006 * lento(rng, n, 3))
    ar = rng.standard_normal(n)
    ronco = unit(svf(ar, (250 + 1300 * rot**2) * p, q=0.6, mode="lp"))
    corpo = unit(bp(ar, 120, 900))
    serra = unit(
        bp(
            osc((55 + 70 * rot) * p * (1 + 0.003 * lento(rng, n, 25)), "saw", hmax=60),
            400,
            2600,
        )
    )
    fan = (900 + 2000 * rot) * p * (1 + 0.002 * lento(rng, n, 40))
    apito = np.sin(ph(fan)) + 0.35 * np.sin(ph(2 * fan)) + 0.12 * np.sin(ph(3.03 * fan))
    x = (
        (0.9 * ronco + 0.7 * corpo) * rot**2 * (1 + 0.18 * lento(rng, n, 14))
        + 0.35 * serra * rot**3
        + 0.16 * apito * rot**2
    )
    return afasta(chao(np.tanh(1.4 * unit(x)), 0.0014 - 0.0011 * t / t[-1]), 1.4, 0.10)


def s_helice(rng, v):
    """Decolagem de turboelice (o aviao regional): pas batendo no ar, duas helices fora de fase (o "uau-uau") e turbina fina."""
    p, n = (1.0, 0.95)[v], n_of(3.4)
    t = np.arange(n) / SR
    rot = (0.6 + 0.4 * (1 - np.exp(-t / 0.5))) * (1 + 0.005 * lento(rng, n, 4))

    def pas(desvio):
        return unit(osc(120 * rot * p * desvio, "pulse", duty=0.18, hmax=26))

    helices = pas(1.0) + 0.65 * pas(
        1.008
    )  # a 2a mais fraca: o batimento pulsa sem zerar o som
    ar = rng.standard_normal(n)
    chop = 0.6 + 0.4 * np.sin(ph(120 * rot * p))
    sopro = unit(bp(ar, 300, 2600)) * chop * (1 + 0.2 * lento(rng, n, 11))
    turbina = np.sin(ph((2600 + 1100 * rot) * p * (1 + 0.002 * lento(rng, n, 30))))
    x = (
        unit(hp(bp(helices, 280, 2600), 300, order=4)) + 0.9 * sopro
    ) * rot**2 + 0.02 * turbina * rot**2  # acima de 300 Hz: celular
    return afasta(chao(np.tanh(1.3 * unit(x)), 0.0013 - 0.001 * t / t[-1]), 1.4, 0.08)


# ------------------------------------------------------------------ SFX (um por momento do GDD §12)


def sweep(f0, f1, n):
    """Glide exponencial f0 -> f1 em n amostras."""
    return f0 * (f1 / f0) ** np.linspace(0.0, 1.0, n)


def ad(n, a, tau, sr=SR):
    """Ataque linear de `a` s e queda exponencial com constante `tau` s."""
    t = np.arange(n) / sr
    return np.minimum(1.0, t / max(a, 1e-5)) * np.exp(-np.maximum(t - a, 0.0) / tau)


def adsr(gate, a, d, s, r, sr):
    ng = n_of(gate, sr)
    t = np.arange(ng) / sr
    e = np.where(t < a, t / max(a, 1e-5), s + (1 - s) * np.exp(-(t - a) / d))
    tail = e[-1] * np.exp(-np.arange(n_of(5 * r, sr)) / (r * sr))
    return np.concatenate([e, tail])


def modes(n, freqs, taus, amps=None, sr=SR):
    """Soma de senoides amortecidas: madeira, metal, sino, moeda."""
    t = np.arange(n) / sr
    amps = amps or [1.0] * len(freqs)
    return sum(
        a * np.sin(2 * np.pi * f * t) * np.exp(-t / tau)
        for f, tau, a in zip(freqs, taus, amps)
    )


def pluck(f, dur, rng, damp=0.996, sr=SR):
    """Karplus-Strong afinado: o laco tem period - 0,5 amostras e o resto do erro sai reamostrando."""
    n, period = n_of(dur, sr), max(2, int(round(sr / f + 0.5)))
    buf, out, j = rng.uniform(-1, 1, period).tolist(), [], 0
    for _ in range(n):
        v = buf[j]
        out.append(v)
        buf[j] = damp * 0.5 * (v + buf[(j + 1) % period])
        j = (j + 1) % period
    return np.interp(np.arange(n) * f * (period - 0.5) / sr, np.arange(n), out)


def mix(*xs):
    out = np.zeros(max(len(x) for x in xs))
    for x in xs:
        out[: len(x)] += x
    return out


def place(buf, x, at):
    at = int(at)
    m = min(len(x), len(buf) - at)
    if m > 0:
        buf[at : at + m] += x[:m]


def bell(f, dur, sr=SR):
    n = n_of(dur, sr)
    return modes(
        n,
        [f, 2 * f, 3.01 * f, 5.4 * f],
        [0.5 * dur, 0.22 * dur, 0.1 * dur, 0.03],
        [1, 0.3, 0.1, 0.08],
        sr,
    ) * np.minimum(1, np.arange(n) / (0.002 * sr))


def notes(seq, sr, voice, gain=1.0):
    """[(t, midi, dur)] -> buffer com a voz `voice(f, dur)` em cada nota."""
    end = max(t + d for t, _, d in seq) + 1.5
    buf = np.zeros(n_of(end, sr))
    for t, m, d in seq:
        place(buf, voice(midi(m), d) * gain, t * sr)
    return buf


def lead(f, d, sr=SR):
    """Pulso 25% suave com vibrato atrasado (fanfarra da missao)."""
    e = adsr(d * 0.9, 0.005, 0.12, 0.6, 0.05, sr)
    t = np.arange(len(e)) / sr
    vib = 1 + 0.005 * np.sin(2 * np.pi * 5.5 * t) * np.clip((t - 0.18) / 0.2, 0, 1)
    return lp(osc(f * vib, "pulse", sr, duty=0.25, hmax=14), 3000, sr) * e


P3 = (1.0, 0.91, 1.1)  # fator de altura por variacao


def s_toque(rng, v):
    """Toque na tela: o som mais frequente do jogo, curto e macio."""
    f, n = (1180, 1040)[v], n_of(0.06)
    return np.sin(ph(sweep(f, f * 0.72, n))) * ad(n, 0.001, 0.012) + 0.12 * lp(
        rng.standard_normal(n), 2500
    ) * ad(n, 0.0003, 0.003)


def s_melhoria(rng, v):
    """Melhoria comprada: batida de obra e tres sininhos subindo."""
    x = np.zeros(n_of(0.95))
    place(x, modes(n_of(0.1), [420, 980], [0.03, 0.015], [1, 0.5]), 0)
    for k, nota in enumerate((76, 81, 88)):
        place(x, 0.6 * bell(midi(nota), 0.6), (0.06 + 0.09 * k) * SR)
    return x


def s_embarque(rng, v):
    """Embarque aberto: o "ding-dong" do alto-falante do aeroporto, lento, com a oitava de baixo dando corpo."""
    x = np.zeros(n_of(1.5))
    for at, nota in ((0.0, 79), (0.38, 75)):
        place(x, bell(midi(nota), 1.1) + 0.5 * bell(midi(nota - 12), 1.1), at * SR)
    return x


def s_recompensa(rng, v):
    """Recompensa (cofre, meta): moedas caindo e um brilho no fim."""
    p, x, at = P3[v], np.zeros(n_of(0.75)), 0.0
    for k in range(4):
        f = rng.uniform(1900, 2400) * p
        place(
            x,
            modes(
                n_of(0.25),
                [f, f * 2.32, f * 3.89, f * 5.4],
                [0.09, 0.05, 0.03, 0.02],
                [1, 0.6, 0.4, 0.2],
            )
            * (1 - 0.2 * k),
            at,
        )
        at += rng.uniform(0.035, 0.07) * SR
    place(x, 0.5 * bell(midi(91) * p, 0.5), 0.16 * SR)
    return x


def s_alerta(rng, v):
    """Estacao critica (GDD §12: distinguivel sem ser agressivo). Tres batidas curtas de madeira no medio: o ritmo "ta-ta-tum" e
    o timbre nao existem em outro som do banco, e sem ataque seco nem agudo em 3 kHz, onde o ouvido doi."""
    x, m = np.zeros(n_of(0.95)), n_of(0.45)
    t = np.arange(m) / SR
    for at, nota in ((0.0, 76), (0.16, 76), (0.32, 72)):
        f = midi(nota)
        e = np.minimum(1, t / 0.008) * np.exp(-t / 0.11)
        place(
            x,
            (
                np.sin(2 * np.pi * f * t)
                + 0.3 * np.sin(4 * np.pi * f * t)
                + 0.08 * np.sin(6 * np.pi * f * t)
            )
            * e,
            at * SR,
        )
    return x


def s_missao(rng, v):
    """Missao concluida: subida curta e acorde que abre, com sino dobrando a melodia."""
    melo = [
        (0.0, 67, 0.11),
        (0.11, 72, 0.11),
        (0.22, 76, 0.11),
        (0.33, 79, 0.11),
        (0.46, 84, 0.7),
    ]
    return mix(
        notes(melo, SR, lead, 0.7),
        notes(melo, SR, lambda f, d: bell(f, 0.8), 0.5),
        notes([(0.46, 76, 0.7), (0.46, 79, 0.7)], SR, lead, 0.22),
    )


# nome -> (receita, variacoes, LUFS alvo aprox.). Toque mais baixo (frequente); eventos grandes mais altos; a musica fica em -22.
SFX = {
    "toque": (s_toque, 2, -24),
    "melhoria": (s_melhoria, 1, -17),
    "embarque": (s_embarque, 1, -19),
    "decolagem_helice": (s_helice, 2, -17),
    "decolagem_jato": (s_jato, 2, -17),
    "recompensa": (s_recompensa, 3, -17),
    "alerta": (s_alerta, 1, -18),
    "missao": (s_missao, 1, -15),
}


# ------------------------------------------------------------------ musica

NOTE = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}
QUAL = {"": (0, 4, 7), "7": (0, 4, 7, 10), "M7": (0, 4, 7, 11), "m7": (0, 3, 7, 10)}


def m_of(s):
    k, i = NOTE[s[0]], 1
    while s[i] in "#b":
        k += 1 if s[i] == "#" else -1
        i += 1
    return k + 12 * (int(s[i:]) + 1)


def melody(text, start=0.0):
    """'E5:2 C5:1 r:1' -> [(beat, midi, beats)], fim."""
    out, b = [], start
    for tok in text.split():
        name, d = tok.split(":")
        if name != "r":
            out.append((b, m_of(name), float(d)))
        b += float(d)
    return out, b


def chord(name):
    """'Bb' -> (classe de altura da raiz, intervalos)."""
    k, i = NOTE[name[0]], 1
    if len(name) > 1 and name[1] in "#b":
        k += 1 if name[1] == "#" else -1
        i = 2
    return k % 12, QUAL[name[i:]]


class Song:
    def __init__(self, bpm, beats, seed):
        self.spb = 60.0 / bpm
        self.L = int(round(beats * self.spb * MSR))
        self.tracks = {}
        self.rng = np.random.default_rng(seed)

    def add(self, track, beat, x, gain=1.0):
        buf = self.tracks.setdefault(track, np.zeros(self.L + n_of(4.0, MSR)))
        place(
            buf,
            x * gain * self.rng.uniform(0.9, 1.0),
            int(round(beat * self.spb * MSR)),
        )

    def loop(self, track):
        """Buffer em laco: o que passa do fim (caudas) soma no comeco, como no laco real."""
        buf = self.tracks[track]
        x = buf[: self.L].copy()
        tail = buf[self.L :]
        x[: len(tail)] += tail
        return x


def roll_echo(x, delay, gains):
    """Eco circular (np.roll): o laco continua sem emenda."""
    y = x.copy()
    for k, g in enumerate(gains, 1):
        y += g * np.roll(x, k * delay)
    return y


def room(x):
    """Saguao curto: taps circulares de um sinal passa-baixas (filtrado em 3 voltas para nao ter emenda)."""
    L = len(x)
    w = lp(np.tile(x, 3), 2500, MSR)[L : 2 * L]
    y = x.copy()
    for d, g in zip(
        (0.031, 0.047, 0.067, 0.089, 0.113, 0.149), (0.16, 0.14, 0.11, 0.09, 0.07, 0.05)
    ):
        y += g * np.roll(w, n_of(d, MSR))
    return y


# vozes da musica (MSR): o que muda de aeroporto para aeroporto
def v_violao(s, f):
    x = lp(
        pluck(f, 1.5, s.rng, 0.999), 3000
    )  # a 44,1 kHz: a 22 kHz a nota aguda morre em 0,15 s
    return 2.2 * unit(x)[::2]


def v_vibra(s, f):
    n = n_of(1.8, MSR)
    t = np.arange(n) / MSR
    return (
        modes(n, [f, 4 * f], [0.7, 0.12], [1, 0.12], MSR)
        * (1 + 0.22 * np.sin(2 * np.pi * 4.5 * t))
        * np.minimum(1, t / 0.004)
    )


def v_rhodes(s, f):
    """Piano eletrico por FM: o saguao do hub."""
    n = n_of(1.8, MSR)
    t = np.arange(n) / MSR
    return (
        np.sin(2 * np.pi * f * t + 1.4 * np.exp(-t / 0.25) * np.sin(2 * np.pi * f * t))
        * np.exp(-t / 0.75)
        * np.minimum(1, t / 0.003)
    )


def v_pad(f, d):
    e = adsr(d, 0.5, 1.0, 0.8, 0.5, MSR)
    n = len(e)
    return (
        lp(
            osc(np.full(n, f), "tri", MSR, hmax=7)
            + osc(np.full(n, f * 1.004), "tri", MSR, hmax=7),
            1800,
            MSR,
        )
        * e
    )


def v_baixo(f, d):
    e = adsr(d * 0.9, 0.006, 0.35, 0.0, 0.05, MSR)
    return osc(np.full(len(e), f), "tri", MSR, hmax=9) * e


def perto_a2(pc):
    return 45 + (pc - 9) % 12 - (12 if (pc - 9) % 12 > 6 else 0)


# A mesma composicao nos 3 aeroportos (GDD §12: "variacoes sutis"): 20 compassos em 4/4, escrita em Do; cada um muda tom, andamento e voz.
MELODIA = (
    "E5:3 G5:1 D5:4 C5:3 E5:1 A4:4 E5:3 G5:1 A5:2 G5:2 F5:3 E5:1 D5:4 "
    "A5:3 G5:1 E5:4 C5:2 D5:2 E5:4 D5:3 E5:1 G5:2 D5:2 C5:4 r:4 "
    "F5:2 E5:2 D5:4 C5:8"
)
HARMONIA = "C Em7 Am7 FM7 C Em7 FM7 G7 FM7 C Am7 Em7 Dm7 G7 C G7 FM7 G7 CM7 CM7".split()


def ambiente(voz, semitons, bpm, seed):
    s = Song(bpm, 80, seed)
    for b0, m, d in melody(MELODIA)[0]:
        s.add("lead", b0, voz(s, midi(m + semitons)), 0.42)
    n = n_of(0.12, MSR)
    for bar, c in enumerate(HARMONIA):
        root, iv = chord(c)
        pc, b0 = (root + semitons) % 12, bar * 4
        for i in iv:
            s.add("pad", b0, v_pad(midi(57 + (pc + i - 9) % 12), 4 * s.spb), 0.048)
        s.add("bass", b0, v_baixo(midi(perto_a2(pc)), 2.5 * s.spb), 0.42)
        s.add(
            "bass",
            b0 + 2.5,
            v_baixo(midi(perto_a2(pc) + iv[2] - 12 * (iv[2] > 6)), 1.5 * s.spb),
            0.28,
        )
        if bar >= 8:  # a 2a metade ganha um dedilhado e um chocalho, bem baixos
            for k, i in enumerate((iv[0], iv[1], iv[2], iv[1]) * 2):
                s.add("arp", b0 + 0.5 * k, voz(s, midi(69 + (pc + i - 9) % 12)), 0.09)
            for k in (1, 3):
                s.add(
                    "shaker",
                    b0 + k,
                    bp(s.rng.standard_normal(n), 3000, 8000, MSR)
                    * ad(n, 0.012, 0.03, MSR),
                    0.1,
                )
    x = (
        roll_echo(s.loop("lead"), int(round(0.75 * s.spb * MSR)), (0.22, 0.07))
        + s.loop("pad")
        + s.loop("bass")
        + s.loop("arp")
        + s.loop("shaker")
    )
    return room(x)


MUSIC = {
    "vale_verde": lambda: ambiente(v_violao, 0, 84, 901),
    "costa_azul": lambda: ambiente(v_vibra, 2, 88, 902),
    "aurora_global": lambda: ambiente(v_rhodes, -3, 80, 903),
}


# ------------------------------------------------------------------ medida, normalizacao, escrita


def kweight(x, sr):
    """Pre-filtro K da BS.1770 (prateleira +4 dB em 1,68 kHz + passa-altas 38 Hz), formulas RBJ."""
    out = x
    for kind, fc, q, g in (
        ("shelf", 1681.974450955533, 0.7071752369554196, 3.999843853973347),
        ("hp", 38.13547087602444, 0.5003270373238773, 0),
    ):
        w0 = 2 * np.pi * fc / sr
        alpha, cw = np.sin(w0) / (2 * q), np.cos(w0)
        if kind == "shelf":
            A = 10 ** (g / 40)
            sa = 2 * np.sqrt(A) * alpha
            b = [
                A * ((A + 1) + (A - 1) * cw + sa),
                -2 * A * ((A - 1) + (A + 1) * cw),
                A * ((A + 1) + (A - 1) * cw - sa),
            ]
            a = [
                (A + 1) - (A - 1) * cw + sa,
                2 * ((A - 1) - (A + 1) * cw),
                (A + 1) - (A - 1) * cw - sa,
            ]
        else:
            b = [(1 + cw) / 2, -(1 + cw), (1 + cw) / 2]
            a = [1 + alpha, -2 * cw, 1 - alpha]
        out = signal.lfilter(b, a, out)
    return out


def lufs(x, sr, gated):
    """gated: integrado da BS.1770 (musica). Senao: maior loudness momentanea (janela de 400 ms, ou o clipe inteiro se for
    mais curto), que e o que se ouve num SFX sem a cauda puxar a media para baixo."""
    y = kweight(x, sr) ** 2
    blk, hop = min(int(0.4 * sr), len(y)), int(0.01 * sr if not gated else 0.1 * sr)
    c = np.concatenate([[0.0], np.cumsum(y)])
    starts = np.arange(0, len(y) - blk + 1, hop)
    ms = (c[starts + blk] - c[starts]) / blk
    if not gated:
        return -0.691 + 10 * np.log10(np.max(ms) + 1e-20)
    ms = ms[-0.691 + 10 * np.log10(ms + 1e-20) > -70]
    rel = -0.691 + 10 * np.log10(np.mean(ms)) - 10
    ms = ms[-0.691 + 10 * np.log10(ms) > rel]
    return -0.691 + 10 * np.log10(np.mean(ms))


def true_peak_db(x):
    return 20 * np.log10(np.max(np.abs(signal.resample_poly(x, 4, 1))) + 1e-20)


def normalize(x, sr, target, gated):
    g = min(
        10 ** ((target - lufs(x, sr, gated)) / 20),
        10 ** ((PEAK_DB - true_peak_db(x)) / 20),
    )
    return x * g


def finish_sfx(x, target):
    x = hp(
        np.asarray(x, float), 40
    )  # sem DC nem sub que o alto-falante do celular nao toca
    # transiente forte demais para chegar ao alvo sem passar do pico: limitador suave (tanh), no maximo 6 dB
    short = (target - lufs(x, SR, False)) - (PEAK_DB - true_peak_db(x))
    if short > 1:
        x = hp(
            np.tanh(10 ** (min(short, 6) / 20) * x / np.max(np.abs(x))), 10
        )  # o tanh assimetrico cria DC: tira so o DC
    thr = np.max(np.abs(x)) * 10 ** (
        -50 / 20
    )  # corta silencio do comeco e do fim (-50 dB do pico)
    loud = np.abs(x) > thr
    x = x[np.argmax(loud) : len(x) - np.argmax(loud[::-1])].copy()
    fi, fo = min(32, len(x) // 4), min(n_of(0.005), len(x) // 4)
    x[:fi] *= np.linspace(0, 1, fi)
    x[-fo:] *= np.linspace(1, 0, fo)
    return normalize(x, SR, target, gated=False)


def write_wav(path, x, sr):
    pcm = np.clip(np.round(x * 32767), -32768, 32767).astype("<i2")
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(pcm.tobytes())
    return pcm.astype(float) / 32768


def guid(rel):
    return hashlib.md5(("ta-audio:" + rel).encode()).hexdigest()


def write_meta(path, rel, music):
    """Import do Unity: SFX curto = Decompress On Load; musica = Streaming + Load In Background. So se faltar."""
    meta = path.with_name(path.name + ".meta")
    if meta.exists():
        return
    if music is None:
        body = "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
    else:
        lt, q, pre, bg = (2, 0.5, 0, 1) if music else (0, 0.7, 1, 0)
        body = (
            "AudioImporter:\n  externalObjects: {}\n  serializedVersion: 7\n  defaultSettings:\n    serializedVersion: 2\n"
            f"    loadType: {lt}\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: 1\n"
            f"    quality: {q}\n    conversionMode: 0\n    preloadAudioData: {pre}\n  platformSettingOverrides: {{}}\n"
            f"  forceToMono: 0\n  normalize: 0\n  preloadAudioData: {pre}\n  loadInBackground: {bg}\n  ambisonic: 0\n  3D: 0\n"
        )
    meta.write_text(
        f"fileFormatVersion: 2\nguid: {guid(rel)}\n{body}  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
        encoding="utf-8",
        newline="\n",
    )


def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    rows, problems = [], []
    for d in ("", "sfx", "musica"):
        p = OUT / d if d else OUT
        p.mkdir(parents=True, exist_ok=True)
        write_meta(p, "folder:" + d, None)
    print(f"{'arquivo':30} {'dur s':>6} {'pico dBTP':>9} {'LUFS':>6} {'DC':>8}")
    for name, (fn, nvar, target) in SFX.items():
        for v in range(nvar):
            rng = np.random.default_rng(zlib.crc32(f"{name}#{v}".encode()))
            rel = f"sfx/{name}_{v + 1}.wav"
            y = write_wav(OUT / rel, finish_sfx(fn(rng, v), target), SR)
            write_meta(OUT / rel, rel, False)
            rows.append(check(rel, y, SR, False, problems))
    for name, fn in MUSIC.items():
        x = fn()
        x = normalize(x - x.mean(), MSR, MUSIC_LUFS, gated=True)
        rel = f"musica/{name}.wav"
        y = write_wav(OUT / rel, x, MSR)
        write_meta(OUT / rel, rel, True)
        rows.append(check(rel, y, MSR, True, problems))
    size = {
        d: sum((OUT / r).stat().st_size for r, *_ in rows if r.startswith(d)) / 1e6
        for d in ("sfx", "musica")
    }
    print(
        f"{len(rows)} arquivos, {sum(size.values()):.2f} MB (sfx {size['sfx']:.2f} MB, musica {size['musica']:.2f} MB)"
    )
    if problems:
        sys.exit("FALHOU:\n" + "\n".join(problems))
    print(
        "OK: pico < -1 dBTP, sem DC, sem silencio inicial, lacos sem emenda, alerta macio"
    )
    if "--md" in sys.argv:
        s = hashlib.sha256(
            Path(__file__).read_bytes().replace(b"\r\n", b"\n")
        ).hexdigest()  # mesmo hash com checkout CRLF
        print(f"\naudio_gen.py SHA-256 {s}\n")
        for rel, *_ in rows:
            print(
                f"| `Audio/{rel}` | `client/tools/audio_gen.py` (numpy/scipy) | `{s[:8]}…{s[-4:]}` | `{sha(OUT / rel)}` | própria (sintetizado) |"
            )


def check(rel, y, sr, music, problems):
    dur, tp, dc = len(y) / sr, true_peak_db(y), float(np.mean(y))
    lu = lufs(y, sr, gated=music)
    print(f"{rel:30} {dur:6.2f} {tp:9.2f} {lu:6.1f} {dc:8.5f}")
    if tp >= -1.0:
        problems.append(f"{rel}: pico {tp:.2f} dBTP")
    if abs(dc) > 1e-3:
        problems.append(f"{rel}: DC {dc:.5f}")
    if music:
        jump = abs(y[0] - y[-1])
        typical = np.percentile(np.abs(np.diff(y)), 99.9)
        if not 45 <= dur <= 90 or jump > typical:
            problems.append(
                f"{rel}: dur {dur:.1f} s, emenda {jump:.4f} > p99,9 {typical:.4f}"
            )
    elif abs(y[0]) > 0.01 or np.max(np.abs(y[: n_of(0.01)])) < 0.01:
        problems.append(f"{rel}: comeco com silencio ou estalo")
    if rel.startswith("sfx/alerta"):  # GDD §12: distinguivel sem ser agressivo
        f, p = signal.welch(y, sr, nperseg=2048)
        centro = np.sum(f * p) / np.sum(p)
        if centro > 1500 or np.max(np.abs(y[: n_of(0.004)])) > 0.7 * np.max(np.abs(y)):
            problems.append(
                f"{rel}: agressivo (centroide {centro:.0f} Hz ou ataque seco)"
            )
    return rel, dur, tp, lu, dc


if __name__ == "__main__":
    main()
