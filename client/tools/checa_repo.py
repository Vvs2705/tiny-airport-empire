"""Portao estatico do repositorio (stdlib, sem Unity): o que os testes do nucleo e o viewcheck nao enxergam.

uso (da raiz do repo): python client/tools/checa_repo.py [--selftest]
Sai 1 com a lista de erros; --selftest quebra um repo de mentira de cada jeito e exige o erro certo (prova vermelha, padrao do
conteudo_check.py do Forge). Olha os arquivos versionados e os novos ainda sem commit:
  meta        todo asset de client/Assets tem .meta, nenhum .meta orfao, nenhum GUID repetido
  nucleo      TA.Core sem UnityEngine/UnityEditor e com noEngineReferences
  modulos     API usada no codigo tem o modulo no manifest.json (o viewcheck compila contra todos os modulos e nao pega)
  guardrails  sem interstitial nem sorteio pago no codigo; o Setup tira o AD_ID do manifesto Android
  proibidos   nada de chave, build ou arte crua no git; nada acima de 10 MB; .gitignore com as travas
  segredos    padroes de token e de chave privada em qualquer arquivo (mostra so o arquivo e o tipo, nunca o valor)
  mapa        todo .cs, .py e .sh citado no docs/MAPA_CODIGO.md
Aviso, sem falhar: arquivo .cs acima de ~600 linhas (regra do estudio: vira partial).
"""

import json
import os
import re
import subprocess
import sys

RAIZ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ASSETS = "client/Assets/"
CORE = "client/Assets/_TA/Scripts/Core/"
ASMDEF_CORE = CORE + "TA.Core.asmdef"
SETUP = "client/Assets/_TA/Editor/Setup.cs"
MANIFESTO = "client/Packages/manifest.json"
MAPA = "docs/MAPA_CODIGO.md"
MAX_BYTES = 10 * 1024 * 1024
MAX_LINHAS = 600

# ponytail: lista curta das APIs que o jogo pode pedir; modulo novo no jogo = linha nova aqui
MODULOS = {
    "audio": r"\b(AudioSource|AudioClip|AudioListener|AudioMixer)\b",
    "animation": r"\b(Animator|AnimationClip)\b",
    "particlesystem": r"\bParticleSystem\b",
    "physics": r"\b(Physics|Rigidbody|Collider|BoxCollider|SphereCollider)\b",
    "unitywebrequest": r"\bUnityWebRequest\b",
    "video": r"\bVideoPlayer\b",
}
PROIBIDOS = re.compile(
    r"(\.(keystore|jks|p12|pfx|pem|apk|aab|obb|fbx|blend|blend1|zip|ttf\.bak)$)|(^|/)(\.env(\..+)?|upload\.properties|google-services\.json|GoogleService-Info\.plist)$",
    re.I,
)
TRAVAS_GITIGNORE = [
    "arte/tripo/",
    "arte/mixamo/",
    "/playtest/",
    ".env",
    "*.apk",
    "*.aab",
]
SEGREDOS = {
    "token do GitHub": rb"\b(ghp|gho|ghs|ghu)_[0-9A-Za-z]{36}\b|\bgithub_pat_[0-9A-Za-z_]{40,}",
    "chave da Anthropic": rb"\bsk-ant-[0-9A-Za-z_-]{20,}",
    "chave do Google": rb"\bAIza[0-9A-Za-z_-]{35}(?![0-9A-Za-z_-])",
    "chave da AWS": rb"\bAKIA[0-9A-Z]{16}\b",
    "chave privada": rb"-----BEGIN [A-Z ]*PRIVATE KEY-----",
    "JWT": rb"\beyJ[0-9A-Za-z_-]{20,}\.eyJ[0-9A-Za-z_-]{20,}\.[0-9A-Za-z_-]{10,}",
}


def texto(dados):
    return dados.decode("utf-8-sig", "replace")  # o Visual Studio grava com BOM


def sem_comentario(cs):
    return "\n".join(linha.split("//")[0] for linha in cs.splitlines())


def checa_meta(repo):
    erros = []
    no_assets = [p for p in repo if p.startswith(ASSETS)]
    itens = set()  # o que o Unity importa: arquivo (dotfile e ~ ele ignora) e as pastas que levam a ele
    pastas = set()
    for p in no_assets:
        d = os.path.dirname(p)
        while len(d) + 1 > len(ASSETS):
            pastas.add(d)
            d = os.path.dirname(d)
        nome = os.path.basename(p)
        if (
            not p.endswith(".meta")
            and not nome.startswith(".")
            and not nome.endswith("~")
        ):
            itens.add(p)
    for p in sorted(itens | pastas):
        if p + ".meta" not in repo:
            erros.append(
                f"meta: falta {p}.meta (o Unity gera um GUID novo em cada clone)"
            )
    guids = {}
    for p in sorted(x for x in no_assets if x.endswith(".meta")):
        alvo = p[: -len(".meta")]
        if alvo not in itens and alvo not in pastas:
            erros.append(f"meta: orfao {p}")
        m = re.search(r"^guid: ([0-9a-f]{32})", texto(repo[p]), re.M)
        if not m:
            erros.append(f"meta: sem guid em {p}")
        elif m.group(1) in guids:
            erros.append(f"meta: GUID repetido em {p} e {guids[m.group(1)]}")
        else:
            guids[m.group(1)] = p
    return erros


def checa_nucleo(repo):
    erros = [
        f"nucleo: {p} usa a Unity"
        for p in repo
        if p.startswith(CORE)
        and p.endswith(".cs")
        and re.search(r"^\s*using\s+Unity", texto(repo[p]), re.M)
    ]
    if not json.loads(texto(repo[ASMDEF_CORE])).get("noEngineReferences"):
        erros.append(f"nucleo: {ASMDEF_CORE} sem noEngineReferences")
    return erros


def checa_modulos(repo):
    tem = json.loads(texto(repo[MANIFESTO]))["dependencies"]
    erros = []
    for modulo, api in MODULOS.items():
        if "com.unity.modules." + modulo in tem:
            continue
        for p in sorted(x for x in repo if x.startswith(ASSETS) and x.endswith(".cs")):
            if re.search(api, sem_comentario(texto(repo[p]))):
                erros.append(
                    f"modulos: {p} usa a API de {modulo}, fora do {MANIFESTO}: adicione com.unity.modules.{modulo}"
                )
    return erros


def checa_guardrails(repo):
    erros = []
    for p in sorted(
        x
        for x in repo
        if x.startswith("client/Assets/_TA/Scripts/") and x.endswith(".cs")
    ):
        m = re.search(
            r"interstitial|gacha|loot ?box", sem_comentario(texto(repo[p])), re.I
        )
        if m:
            erros.append(
                f"guardrails: {p} tem '{m.group(0)}' (nenhum interstitial, nada aleatorio pago)"
            )
    setup = texto(repo[SETUP])
    if "SemIdDePublicidade" not in setup or "permission.AD_ID" not in setup:
        erros.append(f"guardrails: {SETUP} nao tira o AD_ID do manifesto Android")
    return erros


def checa_proibidos(repo):
    erros = [
        f"proibidos: {p} nao vai para o git"
        for p in sorted(repo)
        if PROIBIDOS.search(p)
    ]
    erros += [
        f"proibidos: {p} tem {len(d) // (1024 * 1024)} MB (teto {MAX_BYTES // (1024 * 1024)} MB)"
        for p, d in sorted(repo.items())
        if len(d) > MAX_BYTES
    ]
    linhas = texto(repo[".gitignore"]).splitlines()
    erros += [
        f"proibidos: .gitignore perdeu a trava {t}"
        for t in TRAVAS_GITIGNORE
        if t not in linhas
    ]
    return erros


def checa_segredos(repo):
    return [
        f"segredos: {nome} em {p}"
        for p, d in sorted(repo.items())
        for nome, rx in SEGREDOS.items()
        if re.search(rx, d)
    ]


def checa_mapa(repo):
    mapa = texto(repo[MAPA])
    erros = []
    for p in sorted(repo):
        codigo = p.endswith(".cs") and p.startswith("client/Assets/_TA/")
        ferramenta = p.startswith("client/tools/") and p.endswith((".py", ".sh"))
        if (codigo or ferramenta) and not re.search(r"(?<![\w.])" + re.escape(os.path.basename(p)), mapa):
            erros.append(f"mapa: {p} nao esta no {MAPA}")
    return erros


def avisos_tamanho(repo):
    for p in sorted(repo):
        if p.startswith("client/Assets/_TA/") and p.endswith(".cs"):
            n = texto(repo[p]).count("\n")
            if n > MAX_LINHAS:
                yield f"AVISO: {p} tem {n} linhas (acima de ~{MAX_LINHAS} vira partial)"


def checa(repo):
    return (
        checa_meta(repo)
        + checa_nucleo(repo)
        + checa_modulos(repo)
        + checa_guardrails(repo)
        + checa_proibidos(repo)
        + checa_segredos(repo)
        + checa_mapa(repo)
    )


def le_repo():
    saida = subprocess.run(
        ["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard"],
        cwd=RAIZ,
        capture_output=True,
        check=True,
    ).stdout
    repo = {}
    for p in texto(saida).split("\0"):
        cheio = os.path.join(RAIZ, p)
        if p and os.path.isfile(
            cheio
        ):  # apagado no disco e ainda nao commitado fica de fora
            with open(cheio, "rb") as f:
                repo[p] = f.read()
    return repo


def repo_bom():
    a = ASSETS + "_TA"
    return {
        ".gitignore": "\n".join(TRAVAS_GITIGNORE).encode(),
        MAPA: b"Sim.cs Setup.cs verify.sh",
        MANIFESTO: b'{"dependencies": {}}',
        ASMDEF_CORE: b'{"noEngineReferences": true}',
        SETUP: b"class Setup { SemIdDePublicidade(); /* com.google.android.gms.permission.AD_ID */ }",
        CORE + "Sim.cs": b"using System;\nclass Sim {}\n",
        "client/tools/verify.sh": b"#!/bin/bash\n",
        ASSETS + "_TA.meta": b"guid: " + b"a" * 32,
        a + "/Editor.meta": b"guid: " + b"b" * 32,
        a + "/Editor/Setup.cs.meta": b"guid: " + b"c" * 32,
        a + "/Scripts.meta": b"guid: " + b"d" * 32,
        a + "/Scripts/Core.meta": b"guid: " + b"e" * 32,
        CORE + "Sim.cs.meta": b"guid: " + b"f" * 32,
        CORE + "TA.Core.asmdef.meta": b"guid: " + b"1" * 32,
        a + "/Resources.meta": b"guid: " + b"2" * 32,
        a + "/Resources/.gitkeep": b"",  # dotfile nao pede .meta, mas a pasta sim
    }


def selftest():
    base = repo_bom()
    assert checa(base) == [], checa(base)
    falso = {  # montados aqui para o proprio arquivo nao ter um segredo inteiro
        "gh": b"gh" + b"p_" + b"a" * 36,
        "pem": b"-----BEGIN " + b"RSA PRIVATE KEY-----",
        "goo": b"AI" + b"za" + b"b" * 35,
        "jwt": b"ey" + b"J" + b"a" * 20 + b".ey" + b"J" + b"a" * 20 + b"." + b"c" * 12,
    }
    ceu = CORE + "Ceu.cs"
    casos = [
        ("meta: falta", lambda r: r.update({ceu: b"class Ceu {}"})),
        (
            "meta: falta",
            lambda r: r.update(
                {
                    ASSETS + "_TA/Art/Marca/logo.png": b"x",
                    ASSETS + "_TA/Art/Marca/logo.png.meta": b"guid: " + b"3" * 32,
                }
            ),
        ),
        ("meta: orfao", lambda r: r.update({ceu + ".meta": b"guid: " + b"3" * 32})),
        (
            "meta: GUID repetido",
            lambda r: r.update(
                {ceu: b"class Ceu {}", ceu + ".meta": b"guid: " + b"f" * 32}
            ),
        ),
        (
            "meta: sem guid",
            lambda r: r.update({ceu: b"class Ceu {}", ceu + ".meta": b"nada"}),
        ),
        ("nucleo: ", lambda r: r.update({CORE + "Sim.cs": b"using UnityEngine;\n"})),
        ("nucleo: ", lambda r: r.update({ASMDEF_CORE: b"{}"})),
        (
            "modulos: ",
            lambda r: r.update(
                {
                    ASSETS + "_TA/Scripts/View/Som.cs": b"class Som { AudioSource a; }",
                    ASSETS + "_TA/Scripts/View/Som.cs.meta": b"guid: " + b"4" * 32,
                }
            ),
        ),
        (
            "guardrails: ",
            lambda r: r.update(
                {CORE + "Sim.cs": b"class Sim { void ShowInterstitial() {} }\n"}
            ),
        ),
        (
            "guardrails: ",
            lambda r: r.update({CORE + "Sim.cs": b"class Sim { Gacha g; }\n"}),
        ),
        ("guardrails: ", lambda r: r.update({SETUP: b"class Setup {}"})),
        ("proibidos: ", lambda r: r.update({"arte/tripo/x_hd/x.zip": b"x"})),
        ("proibidos: ", lambda r: r.update({"upload.properties": b"x"})),
        ("proibidos: ", lambda r: r.update({".env.local": b"x"})),
        (
            "proibidos: ",
            lambda r: r.update({"docs/grande.gif": b"x" * (MAX_BYTES + 1)}),
        ),
        ("proibidos: ", lambda r: r.update({".gitignore": b"*.apk\n"})),
        ("segredos: token", lambda r: r.update({"docs/nota.md": falso["gh"]})),
        ("segredos: chave privada", lambda r: r.update({"docs/nota.md": falso["pem"]})),
        (
            "segredos: chave do Google",
            lambda r: r.update({"docs/nota.md": falso["goo"]}),
        ),
        ("segredos: JWT", lambda r: r.update({"docs/nota.md": falso["jwt"]})),
        ("mapa: ", lambda r: r.update({"client/tools/novo.py": b"x"})),
        ("mapa: ", lambda r: r.update({MAPA: b"AirportSim.cs Setup.cs verify.sh"})),  # Sim.cs nao pode passar por AirportSim.cs
        ("segredos: chave do Google", lambda r: r.update({"docs/nota.md": falso["goo"][:-1] + b"- "})),
    ]
    for esperado, quebra in casos:
        r = dict(base)
        quebra(r)
        erros = checa(r)
        assert any(e.startswith(esperado) for e in erros), (esperado, erros)
    # comentario nao conta, e o erro de segredo nunca traz o valor
    r = dict(base)
    r[CORE + "Sim.cs"] = b"// sem interstitial, AudioSource so na View\nclass Sim {}\n"
    assert checa(r) == [], checa(r)
    r = dict(base)
    r["docs/nota.md"] = falso["gh"]
    assert all(texto(falso["gh"]) not in e for e in checa(r))
    r = dict(base)
    r[MANIFESTO] = b"\xef\xbb\xbf" + r[MANIFESTO]  # BOM nao derruba o json
    assert checa(r) == [], checa(r)
    print(f"selftest OK: {len(casos)} quebras pegas")


def main():
    if "--selftest" in sys.argv:
        selftest()
        return 0
    repo = le_repo()
    for aviso in avisos_tamanho(repo):
        print(aviso)
    erros = checa(repo)
    for e in erros:
        print(e)
    print(f"{len(repo)} arquivos, {len(erros)} erro(s)")
    return 1 if erros else 0


if __name__ == "__main__":
    sys.exit(main())
