"""Confere o APK contra o que a Play exige (GDD §3: "reconferidos no dia do envio"). Stdlib + aapt2 e zipalign do SDK que vem no Unity.

uso: python client/tools/checa_apk.py <app.apk>   |   --selftest
Sai 1 com qualquer falta: targetSdk abaixo de 36; ABI diferente de so arm64-v8a; AD_ID no manifesto; .so com segmento alinhado a menos
de 16 KB; zip nao alinhado a 16 KB. Imprime uma linha com o que achou.
ponytail: so APK; o AAB entra com o Setup.BuildAndroidBundle (o manifesto dele pede o bundletool-all que vem no Unity).
"""

import os
import re
import struct
import subprocess
import sys
import zipfile

ANDROID = r"C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Data\PlaybackEngines\AndroidPlayer"
BUILD_TOOLS = os.path.join(
    os.environ.get("UNITY_ANDROID", ANDROID), "SDK", "build-tools", "36.0.0"
)
TARGET_MIN = 36
PAGINA = 16 * 1024
PT_LOAD = 1


def alinhamento_elf(b):
    """Menor p_align entre os PT_LOAD de um ELF de 64 bits, little-endian; None se nao for um."""
    if b[:6] != b"\x7fELF\x02\x01":
        return None
    phoff = struct.unpack_from("<Q", b, 32)[0]
    phentsize, phnum = struct.unpack_from("<HH", b, 54)
    aligns = []
    for i in range(phnum):
        p = phoff + i * phentsize
        if struct.unpack_from("<I", b, p)[0] == PT_LOAD:
            aligns.append(struct.unpack_from("<Q", b, p + 48)[0])
    return min(aligns) if aligns else None


def veredito(badging, libs, zip_alinhado):
    erros = []
    m = re.search(r"targetSdkVersion:'(\d+)'", badging)
    alvo = int(m.group(1)) if m else 0
    if alvo < TARGET_MIN:
        erros.append(f"targetSdk {alvo}: a Play exige {TARGET_MIN} ou mais")
    abis = sorted({n.split("/")[1] for n in libs})
    if abis != ["arm64-v8a"]:
        erros.append(f"ABI {abis}: so arm64-v8a")
    if "AD_ID" in badging:
        erros.append("AD_ID no manifesto")
    for nome, a in sorted(libs.items()):
        if a is None or a < PAGINA:
            erros.append(f"{nome}: alinhado a {a}, abaixo de 16 KB")
    if not zip_alinhado:
        erros.append("zip nao alinhado a 16 KB (zipalign -c -P 16 4)")
    return alvo, abis, erros


def selftest():
    def elf(aligns):
        h = bytearray(64 + 56 * len(aligns))
        h[:6] = b"\x7fELF\x02\x01"
        struct.pack_into("<Q", h, 32, 64)
        struct.pack_into("<HH", h, 54, 56, len(aligns))
        for i, a in enumerate(aligns):
            struct.pack_into("<I", h, 64 + 56 * i, PT_LOAD)
            struct.pack_into("<Q", h, 64 + 56 * i + 48, a)
        return bytes(h)

    assert alinhamento_elf(elf([16384, 65536])) == 16384
    assert alinhamento_elf(elf([4096, 65536])) == 4096
    assert alinhamento_elf(b"nao e elf") is None
    bom = "package: name='x'\ntargetSdkVersion:'36'\n"
    so = {"lib/arm64-v8a/libil2cpp.so": 16384}
    assert veredito(bom, so, True)[2] == []
    casos = [
        ("targetSdk 35", "targetSdkVersion:'35'", so, True),
        ("targetSdk 0", "sem nada", so, True),
        ("ABI", bom, {"lib/armeabi-v7a/libil2cpp.so": 16384, **so}, True),
        (
            "AD_ID",
            bom + "uses-permission: name='com.google.android.gms.permission.AD_ID'\n",
            so,
            True,
        ),
        (
            "lib/arm64-v8a/libil2cpp.so: alinhado a 4096",
            bom,
            {"lib/arm64-v8a/libil2cpp.so": 4096},
            True,
        ),
        ("zip nao alinhado", bom, so, False),
    ]
    for esperado, badging, libs, zipa in casos:
        erros = veredito(badging, libs, zipa)[2]
        assert any(e.startswith(esperado) for e in erros), (esperado, erros)
    print(f"selftest OK: {len(casos)} quebras pegas")


def main():
    if "--selftest" in sys.argv:
        selftest()
        return 0
    apk = sys.argv[1]
    badging = subprocess.run(
        [os.path.join(BUILD_TOOLS, "aapt2.exe"), "dump", "badging", apk],
        capture_output=True,
        encoding="utf-8",
        errors="replace",
        check=True,
    ).stdout
    with zipfile.ZipFile(apk) as z:
        libs = {
            i.filename: alinhamento_elf(z.read(i.filename))
            for i in z.infolist()
            if i.filename.startswith("lib/") and i.filename.endswith(".so")
        }
    zip_ok = (
        subprocess.run(
            [os.path.join(BUILD_TOOLS, "zipalign.exe"), "-c", "-P", "16", "4", apk],
            capture_output=True,
        ).returncode
        == 0
    )
    alvo, abis, erros = veredito(badging, libs, zip_ok)
    for e in erros:
        print(e)
    print(
        f"targetSdk={alvo} abi={','.join(abis)} libs={len(libs)} tamanho={os.path.getsize(apk) / 2**20:.1f} MiB erros={len(erros)}"
    )
    return 1 if erros else 0


if __name__ == "__main__":
    sys.exit(main())
