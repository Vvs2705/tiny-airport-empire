"""Portao estatico do repositorio (stdlib, sem Unity): o que os testes do nucleo e o viewcheck nao enxergam.

uso (da raiz do repo): python client/tools/checa_repo.py [--selftest]
Sai 1 com a lista de erros; --selftest quebra um repo de mentira de cada jeito e exige o erro certo (prova vermelha, padrao do
conteudo_check.py do Forge). Olha os arquivos versionados e os novos ainda sem commit:
  meta        todo asset de client/Assets tem .meta, nenhum .meta orfao, nenhum GUID repetido
  nucleo      TA.Core sem UnityEngine/UnityEditor e com noEngineReferences
  modulos     API usada no codigo tem o modulo no manifest.json (o viewcheck compila contra todos os modulos e nao pega)
  guardrails  sem interstitial nem sorteio pago no codigo; o Setup tira o AD_ID do manifesto Android; flag de linha de comando so com
              Debug.isDebugBuild (no Android qualquer app passa o extra "unity")
  proibidos   nada de chave, build ou arte crua no git; nada acima de 10 MB; .gitignore com as travas
  segredos    padroes de token e de chave privada em qualquer arquivo (mostra so o arquivo e o tipo, nunca o valor)
  mapa        todo .cs, .py e .sh citado no docs/MAPA_CODIGO.md
  ingles      todo texto de tela (L("...") do Textos.cs e Nome = "..." das tabelas do nucleo) com o par em ingles em Core/Ingles.cs, sem
              par vazio (D-08: pt-BR e en no lancamento)
  ci          workflows do repo publico: nunca pull_request_target nem workflow_run (dariam os segredos a PR de fork), sempre com
              permissions: explicito e sem segredo impresso em run: (GDD §8.4)
  ficha       os textos da ficha da Play (docs/FICHA_PLAY.md) cabem nos limites (titulo 30, curta 80, longa 4000) e nao prometem o
              que a regra da ficha proibe ("sem anuncios", "para criancas", "melhor jogo"...)
  privacidade as 2 politicas (docs/privacidade, docs/privacy) com o e-mail de suporte e citando cada pacote que pode coletar dados e
              cada API de rede do jogo (a politica tem de bater com o codigo, GDD §9.3)
  telemetria  todo Diario("...") do jogo usa um nome do catalogo Telemetria.Eventos (Core/Telemetria.cs), todo evento do GDD §15.1 esta
              no catalogo, e nenhum script le modelo, nome ou ID do aparelho, local ou ID de publicidade (Data safety, SEGURANCA §3.1)
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
TELEMETRIA = CORE + "Telemetria.cs"
GDD = "docs/GDD.md"
# dado do aparelho ou da pessoa que o diario e a telemetria nunca leem (o build da loja responde "Nao coleta" no Data safety)
PESSOAL = r"\b(deviceUniqueIdentifier|deviceModel|deviceName|RequestAdvertisingIdentifierAsync|LocationService|Input\.location|AdvertisingIdClient)\b"
POLITICAS = ["docs/privacidade/index.html", "docs/privacy/index.html"]
EMAIL_SUPORTE = "suporte@vstack-solutions.com.br"
FICHA = "docs/FICHA_PLAY.md"
LIMITES_FICHA = {"titulo": 30, "curta": 80, "longa": 4000}
PROIBIDAS_FICHA = ("sem anúncios", "no ads", "melhor jogo", "best game", "nº 1", "#1", "crianças", "kids", "grátis", "free to play")
REDE = r"\b(UnityWebRequest|HttpClient|WebClient|TcpClient|UdpClient|ClientWebSocket)\b"
TEXTOS = "client/Assets/_TA/Scripts/View/Textos.cs"
INGLES = CORE + "Ingles.cs"
LITERAL = r'"((?:[^"\\\n]|\\.)*)"'
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
    r"(\.(keystore|jks|p12|pfx|pem|ulf|alf|apk|aab|obb|fbx|blend|blend1|zip|ttf\.bak)$)|(^|/)(\.env(\..+)?|upload\.properties|keystore\.properties|google-services\.json|GoogleService-Info\.plist)$",
    re.I,
)
TRAVAS_GITIGNORE = [
    "arte/tripo/",
    "arte/mixamo/",
    "/playtest/",
    ".env",
    "*.apk",
    "*.aab",
    "*.jks",
    "*.keystore",
    "*.ulf",
    "upload.properties",
]
WORKFLOWS = ".github/workflows/"
CI_PROIBIDO = re.compile(r"^\s*(pull_request_target|workflow_run)\s*:", re.M)
CI_ECO = re.compile(r"\b(echo|printf|cat)\b[^\n]*\$\{\{\s*secrets\.", re.I)
SEGREDOS = {
    "token do GitHub": rb"\b(ghp|gho|ghs|ghu)_[0-9A-Za-z]{36}\b|\bgithub_pat_[0-9A-Za-z_]{40,}",
    "chave da Anthropic": rb"\bsk-ant-[0-9A-Za-z_-]{20,}",
    "chave do Google": rb"\bAIza[0-9A-Za-z_-]{35}(?![0-9A-Za-z_-])",
    "chave da AWS": rb"\bAKIA[0-9A-Z]{16}\b",
    "chave privada": rb"-----BEGIN [A-Z ]*PRIVATE KEY-----",
    "chave da Stripe": rb"\b(sk|rk)_live_[0-9A-Za-z]{20,}",
    "token do Slack": rb"\bxox[abprs]-[0-9A-Za-z-]{10,}",
    "segredo OAuth do Google": rb"\bGOCSPX-[0-9A-Za-z_-]{20,}",
    "licenca da Unity": rb"<DeveloperData Value=\"[A-Za-z0-9+/=]{20,}",
    "JWT": rb"\beyJ[0-9A-Za-z_-]{20,}\.eyJ[0-9A-Za-z_-]{20,}\.[0-9A-Za-z_-]{10,}",
}


def texto(dados):
    return dados.decode("utf-8-sig", "replace").replace(chr(13) + chr(10), chr(10))  # BOM do Visual Studio; CRLF do checkout no Windows (autocrlf)


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
        cs = sem_comentario(texto(repo[p]))
        if "GetCommandLineArgs" in cs and "isDebugBuild" not in cs:
            erros.append(
                f"guardrails: {p} le flags de linha de comando sem Debug.isDebugBuild (flag de teste so em build de dev, GDD §8.3)"
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


def checa_ci(repo):
    erros = []
    for p in sorted(x for x in repo if x.startswith(WORKFLOWS) and x.endswith((".yml", ".yaml"))):
        y = texto(repo[p])
        m = CI_PROIBIDO.search(y)
        if m:
            erros.append(f"ci: {p} usa {m.group(1)} (repo publico: PR de fork rodaria com os segredos)")
        if not re.search(r"^permissions\s*:", y, re.M):
            erros.append(f"ci: {p} sem permissions: no topo (o GITHUB_TOKEN fica com o padrao amplo)")
        if CI_ECO.search(y):
            erros.append(f"ci: {p} imprime um segredo num run:")
    return erros


def checa_mapa(repo):
    mapa = texto(repo[MAPA])
    erros = []
    for p in sorted(repo):
        codigo = p.endswith(".cs") and p.startswith("client/Assets/_TA/")
        ferramenta = p.startswith("client/tools/") and p.endswith((".py", ".sh"))
        if (codigo or ferramenta) and not re.search(
            r"(?<![\w.])" + re.escape(os.path.basename(p)), mapa
        ):
            erros.append(f"mapa: {p} nao esta no {MAPA}")
    return erros


def pacote_sem_dados(pacote):
    return pacote in ("com.unity.test-framework", "com.unity.ugui") or (
        pacote.startswith("com.unity.modules.")
        and not re.search(r"analytics|webrequest|unityconnect", pacote)
    )


def checa_privacidade(repo):
    erros = [
        f"privacidade: falta {p} (politica no GitHub Pages)"
        for p in POLITICAS
        if p not in repo
    ]
    if erros:
        return erros
    citar = {
        d
        for d in json.loads(texto(repo[MANIFESTO]))["dependencies"]
        if not pacote_sem_dados(d)
    }
    for p in (
        x
        for x in repo
        if x.startswith("client/Assets/_TA/Scripts/") and x.endswith(".cs")
    ):
        citar.update(re.findall(REDE, sem_comentario(texto(repo[p]))))
    for p in POLITICAS:
        html = texto(repo[p])
        if EMAIL_SUPORTE not in html:
            erros.append(f"privacidade: {p} sem o e-mail de suporte {EMAIL_SUPORTE}")
        erros += [
            f"privacidade: {p} nao cita {c}, que o jogo usa: diga o que ele coleta, para que e com quem"
            for c in sorted(citar)
            if c not in html
        ]
    return erros


def checa_telemetria(repo):
    if TELEMETRIA not in repo:
        return [f"telemetria: falta {TELEMETRIA} (catalogo dos eventos do diario)"]
    m = re.search(r"Eventos\s*=\s*new[^{]*\{(.*?)\};", sem_comentario(texto(repo[TELEMETRIA])), re.S)
    catalogo = set(re.findall(r'"([^"]*)"', m.group(1))) if m else set()
    if not catalogo:
        return [f"telemetria: {TELEMETRIA} sem o HashSet Eventos"]
    erros = []
    if GDD in repo:
        sec = re.search(r"^## 15\.1\.(.*?)^## ", texto(repo[GDD]), re.S | re.M)
        for ev in re.findall(r"^- `([a-z_]+)`", sec.group(1) if sec else "", re.M):
            if ev not in catalogo:
                erros.append(f"telemetria: {ev} (GDD §15.1) fora do catalogo de {TELEMETRIA}")
    for p in sorted(x for x in repo if x.startswith("client/Assets/_TA/Scripts/") and x.endswith(".cs")):
        cs = sem_comentario(texto(repo[p]))
        for chamada in re.findall(r"\bDiario\(([^;]*)", cs):
            erros += [
                f"telemetria: {p} grava '{ev}' no diario, fora do catalogo de {TELEMETRIA}"
                for ev in re.findall(r'"([^"]*)"', chamada)
                if ev not in catalogo
            ]
        a = re.search(PESSOAL, cs)
        if a:
            erros.append(f"telemetria: {p} usa {a.group(1)} (dado do aparelho ou da pessoa; o Data safety diz que nada e coletado)")
    return erros


def textos_ficha(md):
    """{(tipo, lingua): texto} dos blocos marcados com (`titulo_pt`), (`curta_en`)... no docs/FICHA_PLAY.md."""
    return {
        (m.group(1), m.group(2)): m.group(3)
        for m in re.finditer(r"\(`(titulo|curta|longa)_(pt|en)`\)\s+```\n(.*?)\n```", md, re.S)
    }


def checa_ficha(repo):
    if FICHA not in repo:
        return []
    ts, erros = textos_ficha(texto(repo[FICHA])), []
    for tipo, limite in LIMITES_FICHA.items():
        for lingua in ("pt", "en"):
            s = ts.get((tipo, lingua))
            if s is None:
                erros.append(f"ficha: falta o bloco {tipo}_{lingua} em {FICHA}")
                continue
            if len(s) > limite:
                erros.append(f"ficha: {tipo}_{lingua} tem {len(s)} caracteres (limite da Play: {limite})")
            erros += [
                f"ficha: {tipo}_{lingua} tem '{p}' (regra da ficha em {FICHA})"
                for p in PROIBIDAS_FICHA
                if p in s.lower()
            ]
    return erros


def avisos_tamanho(repo):
    for p in sorted(repo):
        if p.startswith("client/Assets/_TA/") and p.endswith(".cs"):
            n = texto(repo[p]).count("\n")
            if n > MAX_LINHAS:
                yield f"AVISO: {p} tem {n} linhas (acima de ~{MAX_LINHAS} vira partial)"


def desescapa(lit):
    """O literal C# como o jogo o enxerga: \\u00A0, \\n, \\" e \\\\ viram o caractere."""
    return re.sub(r"\\(u00A0|n|\"|\\)", lambda m: {"u00A0": "\u00a0", "n": "\n"}.get(m.group(1), m.group(1)), lit)


def textos_de_tela(repo):
    """Os textos pt-BR que passam por Idiomas.Traduz: os L("...") do Textos.cs e os Nome = "..." dos arquivos do nucleo."""
    achados = set(re.findall(r"\bL\(" + LITERAL + r"\)", texto(repo[TEXTOS])))
    for p in repo:
        if p.startswith(CORE) and p.endswith(".cs") and p != INGLES:
            achados.update(re.findall(r"\bNome = " + LITERAL, texto(repo[p])))
    return {desescapa(a) for a in achados if a}


def checa_ingles(repo):
    if TEXTOS not in repo or INGLES not in repo:
        return []
    pares = re.findall(r"^\s*\[" + LITERAL + r"\]\s*=\s*" + LITERAL + ",?$", texto(repo[INGLES]), re.M)
    en = {desescapa(pt): desescapa(val) for pt, val in pares}
    erros = [f"ingles: sem par em {INGLES}: {pt!r}" for pt in sorted(textos_de_tela(repo)) if pt not in en]
    erros += [f"ingles: par vazio para {pt!r}" for pt, val in sorted(en.items()) if not val.strip()]
    # quem tem espaco (ou quebra) no comeco ou no fim e' pedaco de frase: o par tem de ter o mesmo molde
    erros += [
        f"ingles: {pt!r} e {val!r} diferem no espaco do comeco ou do fim"
        for pt, val in sorted(en.items())
        if val.strip() and (pt[:1].isspace() != val[:1].isspace() or pt[-1:].isspace() != val[-1:].isspace())
    ]
    return erros


def checa(repo):
    return (
        checa_meta(repo)
        + checa_nucleo(repo)
        + checa_modulos(repo)
        + checa_guardrails(repo)
        + checa_proibidos(repo)
        + checa_segredos(repo)
        + checa_ci(repo)
        + checa_mapa(repo)
        + checa_privacidade(repo)
        + checa_telemetria(repo)
        + checa_ingles(repo)
        + checa_ficha(repo)
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
        MAPA: b"Sim.cs Setup.cs verify.sh Telemetria.cs",
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
        POLITICAS[0]: EMAIL_SUPORTE.encode(),
        POLITICAS[1]: EMAIL_SUPORTE.encode(),
        WORKFLOWS + "nucleo.yml": b"on:\n  pull_request:\npermissions:\n  contents: read\n",
        TELEMETRIA: b'class Telemetria { HashSet<string> Eventos = new HashSet<string> { "session_start", "flight_spawned" }; }',
        CORE + "Telemetria.cs.meta": b"guid: " + b"5" * 32,
        GDD: b"## 15.1. Eventos\n\n- `flight_spawned`\n\n## 15.2. Indicadores\n- `fora_da_lista`\n",
    }


TEXTOS_BOM = b'static string L(string pt) => pt; var x = L("Ola ") + L("Mundo");'
INGLES_BOM = b'            ["Ola "] = "Hi ",\n            ["Mundo"] = "World",\n'


def selftest():
    base = repo_bom()
    assert checa(base) == [], checa(base)
    ficha_boa = "".join(
        f"(`{t}_{l}`)\n```\n{'Little Terminal' if t == 'titulo' else 'Run it.'}\n```\n"
        for t in LIMITES_FICHA
        for l in ("pt", "en")
    ).encode()
    base[FICHA] = ficha_boa
    assert checa(base) == [], checa(base)
    falso = {  # montados aqui para o proprio arquivo nao ter um segredo inteiro
        "gh": b"gh" + b"p_" + b"a" * 36,
        "pem": b"-----BEGIN " + b"RSA PRIVATE KEY-----",
        "goo": b"AI" + b"za" + b"b" * 35,
        "stripe": b"sk" + b"_live_" + b"a" * 24,
        "ulf": b"<Developer" + b"Data Value=\"" + b"QUJD" * 8,
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
        (
            "guardrails: ",
            lambda r: r.update(
                {CORE + "Sim.cs": b"class Sim { string[] a = Environment.GetCommandLineArgs(); }\n"}
            ),
        ),
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
        ("segredos: chave da Stripe", lambda r: r.update({"docs/nota.md": falso["stripe"]})),
        ("segredos: licenca", lambda r: r.update({"docs/nota.md": falso["ulf"]})),
        ("proibidos: ", lambda r: r.update({"Unity_v6000.ulf": b"x"})),
        ("proibidos: ", lambda r: r.update({"client/keystore.properties": b"x"})),
        (
            "ci: ",
            lambda r: r.update(
                {WORKFLOWS + "u.yml": b"on:\n  pull_request_target:\npermissions: {}\n"}
            ),
        ),
        ("ci: ", lambda r: r.update({WORKFLOWS + "u.yml": b"on:\n  push:\n"})),
        (
            "ci: ",
            lambda r: r.update(
                {
                    WORKFLOWS
                    + "u.yml": b"permissions: {}\njobs:\n  a:\n    steps:\n      - run: echo ${{ secrets.UNITY_PASSWORD }}\n"
                }
            ),
        ),
        ("telemetria: falta", lambda r: r.pop(TELEMETRIA)),
        (
            "telemetria: ",
            lambda r: r.update({CORE + "Sim.cs": b'class Sim { void F() { Diario("nome_do_jogador"); } }\n'}),
        ),
        (
            "telemetria: ",
            lambda r: r.update({GDD: b"## 15.1. Eventos\n\n- `flight_spawned`\n- `app_crash`\n\n## 15.2.\n"}),
        ),
        (
            "telemetria: ",
            lambda r: r.update({CORE + "Sim.cs": b"class Sim { string m = SystemInfo.deviceModel; }\n"}),
        ),
        ("mapa: ", lambda r: r.update({"client/tools/novo.py": b"x"})),
        (
            "mapa: ",
            lambda r: r.update({MAPA: b"AirportSim.cs Setup.cs verify.sh Telemetria.cs"}),
        ),  # Sim.cs nao pode passar por AirportSim.cs
        (
            "segredos: chave do Google",
            lambda r: r.update({"docs/nota.md": falso["goo"][:-1] + b"- "}),
        ),
        ("privacidade: falta", lambda r: r.pop(POLITICAS[1])),
        ("ficha: falta", lambda r: r.update({FICHA: ficha_boa.replace(b"curta_en", b"curta_xx")})),
        ("ficha: titulo_pt", lambda r: r.update({FICHA: ficha_boa.replace(b"Little Terminal\n", b"Little Terminal: o maior aeroporto\n", 1)})),
        ("ficha: longa_en", lambda r: r.update({FICHA: ficha_boa.replace(b"Run it.", b"Run it. No ads.")})),
        ("privacidade: ", lambda r: r.update({POLITICAS[0]: b"<p>sem contato</p>"})),
        (
            "privacidade: ",
            lambda r: r.update(
                {
                    MANIFESTO: b'{"dependencies": {"com.unity.services.analytics": "6.0.0"}}'
                }
            ),
        ),
        (
            "privacidade: ",
            lambda r: r.update(
                {CORE + "Sim.cs": b"class Sim { UnityWebRequest w; }\n"}
            ),
        ),
        ("ingles: sem par", lambda r: r.update({TEXTOS: TEXTOS_BOM, INGLES: INGLES_BOM, CORE + "Rotas.cs": b'new Rota { Nome = "Lisboa" };'})),
        ("ingles: par vazio", lambda r: r.update({TEXTOS: TEXTOS_BOM, INGLES: b'[\"Ola\"] = \" \",\n'.replace(b"\\", b"")})),
        ("ingles: ", lambda r: r.update({TEXTOS: TEXTOS_BOM, INGLES: b'            ["Ola "] = "Hi",\n'})),
    ]
    for esperado, quebra in casos:
        r = dict(base)
        quebra(r)
        erros = checa(r)
        assert any(e.startswith(esperado) for e in erros), (esperado, erros)
    r = dict(base)
    r.update({TEXTOS: TEXTOS_BOM, INGLES: INGLES_BOM})
    assert checa_ingles(r) == [], checa_ingles(r)
    # comentario nao conta, e o erro de segredo nunca traz o valor
    r = dict(base)
    r[CORE + "Sim.cs"] = b"// sem interstitial, AudioSource so na View, sem deviceModel\nclass Sim { void F() { Diario(\"session_start\", 1); } }\n"
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
