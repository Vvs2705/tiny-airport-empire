"""Auditoria da arte fora do git (stdlib): o que a lista pede, o que esta em disco, o que esta registrado e o que tem copia.

uso: python client/tools/auditar_arte.py [--sha] [--arte PASTA] [--backup PASTA] [--saida ARQ]   |   --selftest
Le docs/ASSETS.md e docs/PROVENIENCIA.md do repo, arte/tripo/ do checkout principal e o backup. Sai 1 com qualquer falta:
  sem zip       peca da lista sem <peca>_hd/<peca>_hd.zip
  zip quebrado  zip que nao abre, sem FBX ou sem textura base
  sem registro  zip sem linha com SHA-256 no PROVENIENCIA
  sem copia     zip que falta no backup ou com tamanho diferente
  sha           (--sha, le tudo: ~1 min por 5 GB) SHA-256 do zip diferente do registrado, ou o do backup diferente
Fora da lista (peca em disco que o ASSETS.md nao cita) so aparece no resumo: o lote pode ter sido gerado antes de a lista andar.
Nunca escreve na arte: so le.
"""

import hashlib
import os
import re
import subprocess
import sys
import tempfile
import zipfile

RAIZ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BACKUP = os.path.join(os.path.expanduser("~"), "3D Objects", "TinyAirport-BACKUP")
FIM_DA_LISTA = (
    "## 5."  # depois do §2.x o ASSETS.md fala de marca e receita, nao de pecas
)


def pecas_da_lista(assets_md):
    # ponytail: todo token em crase antes do §5 conta como peca; um token solto vira "sem zip" na hora, e dai se corrige o texto
    return set(
        re.findall(r"`([a-z][a-z0-9_]+)`", assets_md.split(FIM_DA_LISTA)[0])
    )  # tabela ou prosa: os lotes 9 e 10 listam em texto


def registros(prov_md):
    """zip citado no PROVENIENCIA -> SHA-256: o 1o hash de 64 digitos depois do caminho e' o do zip (a coluna seguinte e' o do FBX)."""
    reg = {}
    for linha in prov_md.splitlines():
        m = re.search(r"`([a-z0-9_]+/[a-z0-9_]+\.zip)`", linha)
        h = re.search(r"\b[0-9a-f]{64}\b", linha)
        if m and h:
            reg[m.group(1)] = h.group(0)
    return reg


def sha256(caminho):
    h = hashlib.sha256()
    with open(caminho, "rb") as f:
        for bloco in iter(lambda: f.read(1 << 20), b""):
            h.update(bloco)
    return h.hexdigest()


def checa_zip(caminho, profundo):
    try:
        with zipfile.ZipFile(caminho) as z:
            nomes = [n.lower() for n in z.namelist()]
            if profundo and z.testzip():
                return "conteudo corrompido"
    except Exception as e:  # zlib.error, cifrado, metodo desconhecido: um zip ruim nao pode derrubar o relatorio das outras pecas
        return f"{type(e).__name__}: {e}"
    if not any(n.endswith(".fbx") for n in nomes):
        return "sem FBX"
    if not any(
        "basecolor" in n or "base_color" in n or "albedo" in n or "diffuse" in n
        for n in nomes
    ):
        return "sem textura base"
    return None


def audita(assets_md, prov_md, arte, backup, sha=False):
    lista = pecas_da_lista(assets_md)
    reg = registros(prov_md)
    zips = {}
    for pasta in sorted(os.listdir(arte)):
        caminho = os.path.join(arte, pasta, pasta + ".zip")
        if pasta.endswith("_hd") and os.path.isfile(caminho):
            zips[pasta[: -len("_hd")]] = (pasta + "/" + pasta + ".zip", caminho)
    falta = {
        "sem zip": sorted(lista - set(zips)),
        "zip quebrado": [],
        "sem registro": [],
        "sem copia": [],
        "sha": [],
    }
    for peca, (rel, caminho) in sorted(zips.items()):
        ruim = checa_zip(caminho, sha)
        if ruim:
            falta["zip quebrado"].append(f"{peca}: {ruim}")
        if rel not in reg:
            falta["sem registro"].append(peca)
        copia = os.path.join(backup, "arte", "tripo", *rel.split("/"))
        if not os.path.isfile(copia) or os.path.getsize(copia) != os.path.getsize(
            caminho
        ):
            falta["sem copia"].append(peca)
        if sha:
            h = sha256(caminho)
            if rel in reg and reg[rel] != h:
                falta["sha"].append(f"{peca}: disco difere do PROVENIENCIA")
            if os.path.isfile(copia) and sha256(copia) != h:
                falta["sha"].append(f"{peca}: backup difere do disco")
    return {
        "lista": len(lista),
        "zips": len(zips),
        "registros": len(reg),
        "fora da lista": sorted(set(zips) - lista),
        "falta": falta,
    }


def relatorio(r):
    linhas = [
        f"lista (ASSETS.md): {r['lista']} pecas | zips HD em disco: {r['zips']} | registros com SHA: {r['registros']}",
        f"fora da lista: {len(r['fora da lista'])}"
        + (
            " ("
            + ", ".join(r["fora da lista"][:12])
            + (", ..." if len(r["fora da lista"]) > 12 else "")
            + ")"
            if r["fora da lista"]
            else ""
        ),
    ]
    for tipo, itens in r["falta"].items():
        linhas.append(
            f"{tipo}: {len(itens)}"
            + (
                " ("
                + ", ".join(itens[:20])
                + (", ..." if len(itens) > 20 else "")
                + ")"
                if itens
                else ""
            )
        )
    return "\n".join(linhas)


def selftest():
    with tempfile.TemporaryDirectory() as t:
        arte, backup = os.path.join(t, "tripo"), os.path.join(t, "bk")

        def zipa(destino, nomes):
            os.makedirs(os.path.dirname(destino), exist_ok=True)
            with zipfile.ZipFile(destino, "w") as z:
                for n in nomes:
                    z.writestr(n, n)

        bom = ["m.fbx", "m.fbm/a_basecolor.PNG"]
        for p in ("loja_hd", "torre_hd"):
            for raiz in (arte, os.path.join(backup, "arte", "tripo")):
                zipa(os.path.join(raiz, p, p + ".zip"), bom)
        h = sha256(os.path.join(arte, "loja_hd", "loja_hd.zip"))
        assets = (
            "| 1 | `loja` | x |\n| 2 | `torre` | x |\n## 5. Marca\n| `logo_app` |\n"
        )
        prov = f"| loja | `loja_hd/loja_hd.zip` | `{h}` | `{'0' * 64}` |\n| torre | `torre_hd/torre_hd.zip` | `{h}` |\n"
        r = audita(assets, prov, arte, backup, sha=True)
        assert not any(r["falta"].values()) and r["lista"] == 2 and r["zips"] == 2, r
        assert pecas_da_lista("2 pecas: `loja`, `torre`.\n## 5. Marca\n`logo_app`") == {
            "loja",
            "torre",
        }

        r = audita(assets + "", prov.replace(h, "1" * 64, 1), arte, backup, sha=True)
        assert r["falta"]["sha"] == ["loja: disco difere do PROVENIENCIA"], r
        r = audita("| `loja` |\n| `torre` |\n| `aviao` |\n", prov, arte, backup)
        assert r["falta"]["sem zip"] == ["aviao"], r
        r = audita("| `loja` |\n", prov, arte, backup)
        assert r["fora da lista"] == ["torre"] and not r["falta"]["sem zip"], r
        r = audita(assets, prov.splitlines()[0], arte, backup)
        assert r["falta"]["sem registro"] == ["torre"], r
        os.remove(os.path.join(backup, "arte", "tripo", "torre_hd", "torre_hd.zip"))
        r = audita(assets, prov, arte, backup)
        assert r["falta"]["sem copia"] == ["torre"], r
        zipa(
            os.path.join(arte, "torre_hd", "torre_hd.zip"), ["so_textura_basecolor.png"]
        )
        assert audita(assets, prov, arte, backup)["falta"]["zip quebrado"] == [
            "torre: sem FBX"
        ]
        zipa(os.path.join(arte, "torre_hd", "torre_hd.zip"), ["m.fbx"])
        assert audita(assets, prov, arte, backup)["falta"]["zip quebrado"] == [
            "torre: sem textura base"
        ]
        ruim = os.path.join(arte, "torre_hd", "torre_hd.zip")
        with zipfile.ZipFile(ruim, "w", zipfile.ZIP_DEFLATED) as z:
            z.writestr("m.fbx", os.urandom(1) * 200000)
            z.writestr("a_basecolor.png", "x")
        with open(ruim, "r+b") as f:  # corrompe o miolo e deixa o diretorio central intacto
            f.seek(40)
            f.write(b"\xff" * 40)
        assert checa_zip(ruim, True) is not None and checa_zip(ruim, False) is None
        with open(ruim, "wb") as f:
            f.write(b"nao e zip")
        assert audita(assets, prov, arte, backup)["falta"]["zip quebrado"][
            0
        ].startswith("torre: ")
    print("selftest OK")


def main():
    args = sys.argv[1:]
    if "--selftest" in args:
        selftest()
        return 0

    def opcao(nome, padrao):
        return args[args.index(nome) + 1] if nome in args else padrao

    principal = os.path.dirname(
        os.path.abspath(
            subprocess.run(
                ["git", "rev-parse", "--git-common-dir"],
                cwd=RAIZ,
                capture_output=True,
                text=True,
                check=True,
            ).stdout.strip()
        )
    )
    if not os.path.isabs(principal) or not os.path.isdir(
        os.path.join(principal, "arte")
    ):
        principal = RAIZ
    arte = opcao("--arte", os.path.join(principal, "arte", "tripo"))
    with open(os.path.join(RAIZ, "docs", "ASSETS.md"), encoding="utf-8") as f:
        assets = f.read()
    with open(os.path.join(RAIZ, "docs", "PROVENIENCIA.md"), encoding="utf-8") as f:
        prov = f.read()
    r = audita(assets, prov, arte, opcao("--backup", BACKUP), "--sha" in args)
    texto = relatorio(r)
    print(texto)
    if "--saida" in args:
        with open(opcao("--saida", ""), "w", encoding="utf-8") as f:
            f.write(texto + "\n")
    return 1 if any(r["falta"].values()) else 0


if __name__ == "__main__":
    sys.exit(main())
