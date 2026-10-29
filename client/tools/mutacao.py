"""Prova de mutacao das travas da Fase C (CONTRATO_FASE_C §7): tira cada trava do nucleo, roda o dotnet test e exige que algum teste caia.

uso (da raiz do repo): python client/tools/mutacao.py
Cada mutante e' aplicado, testado e desfeito (o arquivo volta byte a byte, mesmo com erro). Sai 1 se algum mutante sobreviver.
"""

import os
import re
import subprocess
import sys

RAIZ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CORE = os.path.join(RAIZ, "client", "Assets", "_TA", "Scripts", "Core")

# (trava, arquivo, trecho original, trecho mutante)
MUTANTES = [
    (
        "manutencao pendente segura o embarque",
        "Sim.Patio.cs",
        "&& (!T.RegraManutencao || !a.Pendente && a.Check == 0)",
        "&& true",
    ),
    (
        "falta de tripulacao segura o embarque",
        "Sim.Patio.cs",
        "&& (!T.RegraTripulacao || TripCompleta(a))",
        "&& true",
    ),
    (
        "checagem so com Cash",
        "Sim.Patio.cs",
        "|| !MecanicoLivre || Cash < T.CheckCusto) return false;",
        "|| !MecanicoLivre) return false;",
    ),
    (
        "hangar so com Cash",
        "Sim.Patio.cs",
        "if (a == null || !T.RegraManutencao || Cash < T.HangarCusto) return false;",
        "if (a == null || !T.RegraManutencao) return false;",
    ),
    (
        "offline nao gasta desgaste",
        "Sim.Offline.cs",
        "LastClaim = claimId;",
        "LastClaim = claimId; foreach (Aviao av in Avioes) av.Desgaste = Math.Min(100, av.Desgaste + T.DesgastePorTrecho);",
    ),
    (
        "nenhum tripulante sai de sorteio",
        "Sim.Tripulacao.cs",
        "Indice = Quantos(f),",
        "Indice = (Quantos(f) + Seed) % 6,",
    ),
    (
        "perder voo nao cobra Cash",
        "Sim.cs",
        "v.Partiu = true;",
        "v.Partiu = true; Cash = Math.Max(0, Cash - v.Perdidos);",
    ),
    (
        "descanso obrigatorio depois de N trechos",
        "Sim.Tripulacao.cs",
        "(t.Trechos < T.TrechosAteDescanso || t.Sala >= DescansoPassos)",
        "true",
    ),
    (
        "anuncio nunca repara",
        "Anuncios.cs",
        "_sim.Recompensa(a, v);",
        "foreach (Aviao av in _sim.Avioes) av.Desgaste = 0; _sim.Recompensa(a, v);",
    ),
]


def testa():
    env = dict(os.environ, DOTNET_CLI_UI_LANGUAGE="en")
    r = subprocess.run(
        [
            "dotnet",
            "test",
            os.path.join(RAIZ, "client", "tools", "coretests"),
            "-nologo",
        ],
        capture_output=True,
        text=True,
        env=env,
    )
    m = re.search(r"Failed:\s+(\d+), Passed:\s+(\d+)", r.stdout)
    if m:
        return int(m.group(1)), re.findall(r"^\s+Failed (\S+)", r.stdout, re.M)
    return -1, [l for l in r.stdout.splitlines() if " error " in l][
        :3
    ]  # nao compilou: o mutante esta errado, nao prova nada


def main():
    vivos = 0
    for nome, arq, de, para in MUTANTES:
        p = os.path.join(CORE, arq)
        with open(p, "rb") as f:
            original = f.read()
        texto = original.decode("utf-8")
        if texto.count(de) != 1:
            print(
                "ERRO   %s: o trecho original nao esta (ou repete) em %s" % (nome, arq)
            )
            vivos += 1
            continue
        try:
            with open(p, "wb") as f:
                f.write(texto.replace(de, para).encode("utf-8"))
            caiu, quais = testa()
        finally:
            with open(p, "wb") as f:
                f.write(original)
        if caiu <= 0:
            vivos += 1
            print(
                "VIVO   %s (%s)"
                % (
                    nome,
                    "nenhum teste caiu"
                    if caiu == 0
                    else "o mutante nao compila: %s" % quais,
                )
            )
        else:
            print("MORTO  %s: %d teste(s): %s" % (nome, caiu, ", ".join(quais[:4])))
    print("mutacao: %d de %d mutantes mortos" % (len(MUTANTES) - vivos, len(MUTANTES)))
    sys.exit(1 if vivos else 0)


if __name__ == "__main__":
    main()
