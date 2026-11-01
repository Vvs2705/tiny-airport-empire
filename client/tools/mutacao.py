"""Prova de mutacao das travas da Fase C (CONTRATO_FASE_C §7): tira cada trava do nucleo, roda o dotnet test e exige que algum teste caia.

uso (da raiz do repo): python client/tools/mutacao.py [trecho do nome]   (com o trecho, so os mutantes que o contem, ex.: campanha)
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
        "&& MecanicoLivre && Cash >= T.CheckCusto;",
        "&& MecanicoLivre;",
    ),
    (
        "hangar so com Cash",
        "Sim.Patio.cs",
        "!= null && T.RegraManutencao && Cash >= T.HangarCusto;",
        "!= null && T.RegraManutencao;",
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
    (
        "perfil novo so pelo marco",
        "Sim.cs",
        "|| Staff.Count >= MaxEquipe || Marco < T.PerfilMarco[(int)p]) return -1;",
        "|| Staff.Count >= MaxEquipe) return -1;",
    ),
    (
        "preco fixo por perfil",
        "Sim.cs",
        "return T.Contratacao[(int)p];",
        "return T.Contratacao[(int)p] + Math.Abs(Seed) % 7;",
    ),
    (
        "offline nao anda as estacoes novas",
        "Sim.Offline.cs",
        "LastClaim = claimId;",
        "LastClaim = claimId; foreach (Station e in Estacoes) if ((int)e.Tipo >= 4) e.Atendendo.Clear();",
    ),
    (
        "incidente sempre tem aviso previo",
        "Sim.Incidentes.cs",
        "i.Aviso = i.Inicio - Passos(T.AvisoSeg);",
        "i.Aviso = i.Inicio;",
    ),
    (
        "incidente nao tira Cash",
        "Sim.Incidentes.cs",
        "if (Steps == i.Inicio) Emit(Ev.IncidenteInicio, (int)i.Tipo, i.Vaga, (int)i.Estacao);",
        "if (Steps == i.Inicio) { Cash = Math.Max(0, Cash - 10); Emit(Ev.IncidenteInicio, (int)i.Tipo, i.Vaga, (int)i.Estacao); }",
    ),
    (
        "um incidente por vez, com folga depois",
        "Sim.Incidentes.cs",
        "if (_inc != null || w - UltimoIncidente <= T.IncidenteFolga) return;",
        "if (_inc != null) return;",
    ),
    (
        "reparo so com Cash",
        "Sim.Incidentes.cs",
        "|| !MecanicoLivre || Cash < T.ReparoCusto) return false;",
        "|| !MecanicoLivre) return false;",
    ),
    (
        "offline nao anda incidente",
        "Sim.Offline.cs",
        "LastClaim = claimId;",
        "LastClaim = claimId; if (_inc != null) _inc = null;",
    ),
    (
        "preco fixo de rota",
        "Sim.Rotas.cs",
        "return T.Rotas[r].Custo;",
        "return T.Rotas[r].Custo + Math.Abs(Seed) % 7;",
    ),
    (
        "rota so depois do marco",
        "Sim.Rotas.cs",
        "|| !Pago(T.Rotas[r].Aeroporto) || Marco < T.Rotas[r].Marco) return -1;",
        "|| !Pago(T.Rotas[r].Aeroporto)) return -1;",
    ),
    (
        "rota longa sem voo na onda fora",
        "Sim.cs",
        "Total = w < Avioes[f].VoltaOnda ? 0 : PaxDaRota(w, rota),",
        "Total = PaxDaRota(w, rota),",
    ),
    (
        "frente fria segura o portao",
        "Sim.cs",
        "v.Embarque && (!PistaFechada || v.Adiantado);",
        "v.Embarque;",
    ),
    (
        "passageiro na parada opcional nao fica para tras",
        "Sim.cs",
        "s.Fila.RemoveAll(p => p.Onda == v.Onda && p.Voo == v.Indice);",
        "if ((int)s.Tipo < 4) s.Fila.RemoveAll(p => p.Onda == v.Onda && p.Voo == v.Indice);",
    ),
    (
        "falha de aviao so acima do aviso",
        "Sim.Clima.cs",
        " && Avioes[aviao].Desgaste >= T.DesgasteAviso;",
        ";",
    ),
    (
        "uma falha por voo",
        "Sim.Clima.cs",
        "if (a.VooId < 0 || a.VooId == a.Falhou || !EmRisco(a.Id))",
        "if (a.VooId < 0 || !EmRisco(a.Id))",
    ),
    (
        "reparo da pane so com Cash",
        "Sim.Clima.cs",
        "if (aviao < 0 || aviao >= Avioes.Length || Cash < T.PaneCusto || Reparo > 0) return false;",
        "if (aviao < 0 || aviao >= Avioes.Length || Reparo > 0) return false;",
    ),
    (
        "clima tem o aviso inteiro",
        "Sim.Clima.cs",
        "Aviso = inicio - Passos(T.AvisoSeg) };",
        "Aviso = inicio };",
    ),
    (
        "Cash nao muda o clima",
        "Sim.Clima.cs",
        "double x = U(w, 0, 0, 30);",
        "double x = U(w, 0, 0, 30) + (Cash > 0 ? 0.5 : 0);",
    ),
    (
        "anuncio nao evita falha",
        "Sim.Clima.cs",
        "if (U(w, a.Id, 0, 20) >= T.FalhaAviaoChance) return TipoFalha.Nenhuma;",
        "if (BoostLeft > 0 || U(w, a.Id, 0, 20) >= T.FalhaAviaoChance) return TipoFalha.Nenhuma;",
    ),
    (
        "pane nao tira Cash",
        "Sim.Clima.cs",
        "a.Falhou = a.VooId; a.Pane = true;",
        "a.Falhou = a.VooId; a.Pane = true; Cash = Math.Max(0, Cash - 5);",
    ),
    (
        "todo retorno pousa seguro",
        "Sim.Clima.cs",
        "Emit(Ev.PousoSeguro, a.Id);",
        "",
    ),
    (
        "retorno tem prioridade na pista",
        "Sim.Patio.cs",
        "(a.Retorno > 0 ? Steps >= a.Chega : Steps >= a.Chega + EsperaPista && !PistaFechada && Steps >= PistaAte)",
        "Steps >= a.Chega + EsperaPista && !PistaFechada && Steps >= PistaAte",
    ),
    (
        "sem Cash a pane nao trava o jogo",
        "Sim.Patio.cs",
        "if (a.NoPatio && (a.Pendente || a.Pane) && a.Check == 0 && a.FechouEm < 0 && MecanicoLivre)",
        "if (a.NoPatio && a.Pendente && a.Check == 0 && a.FechouEm < 0 && MecanicoLivre)",
    ),
    (
        "com clima a frente fria sai dos pesos",
        "Sim.Incidentes.cs",
        "if (T.RegraClima) pesos[(int)TipoIncidente.FrenteFria] = 0;",
        "",
    ),
    (
        "atrasar custa o bonus",
        "Sim.Patio.cs",
        "return !PistaFechada && !v.Atrasado && EsperaDecolagem(a) == 0 &&",
        "return !PistaFechada && EsperaDecolagem(a) == 0 &&",
    ),
    (
        "campanha: recompensa fixa, sem semente",
        "Sim.Metas.cs",
        "int pago = T.RegraCampanha ? o.Recompensa : 0;",
        "int pago = T.RegraCampanha ? o.Recompensa + System.Math.Abs(Seed) % 7 : 0;",
    ),
    (
        "campanha: recompensa fora do 2x e do cofre",
        "Sim.Metas.cs",
        "Cash += pago;",
        "Ganha(pago);",
    ),
    (
        "campanha: regra desligada nao paga",
        "Sim.Metas.cs",
        "int pago = T.RegraCampanha ? o.Recompensa : 0;",
        "int pago = o.Recompensa;",
    ),
    (
        "campanha: conclui so no alvo",
        "Sim.Metas.cs",
        "if (o == null || Progresso(a) < o.Alvo) continue;",
        "if (o == null || Progresso(a) < 1) continue;",
    ),
    (
        "campanha: fechar e abrir nao paga de novo",
        "Sim.Save.cs",
        "_feitos[a] = Math.Max(0, Math.Min(T.Objetivos[a].Length, c[0]));",
        "_feitos[a] = 0;",
    ),
    (
        "campanha: contador do save limitado",
        "Sim.Save.cs",
        "_conta[a][i] = Math.Max(0, Math.Min(T.Objetivos[a][i].Alvo, c[i + 1]));",
        "_conta[a][i] = c[i + 1];",
    ),
    (
        "campanha: offline nao conclui",
        "Sim.Offline.cs",
        "LastClaim = claimId;",
        "LastClaim = claimId; PassoMetas();",
    ),
    (
        "campanha: offline nao conta",
        "Sim.Offline.cs",
        "LastClaim = claimId;",
        "LastClaim = claimId; Conta(Meta.Embarcados);",
    ),
    (
        "campanha: anuncio nao anda a campanha",
        "Sim.Metas.cs",
        "case Ev.Historia: Conta(Meta.Historias); break;",
        "case Ev.Historia: Conta(Meta.Historias); break; case Ev.Recompensa: Conta(Meta.Embarcados); break;",
    ),
    (
        "campanha: so conta no aeroporto do objetivo",
        "Sim.Metas.cs",
        "int a = AeroportoAtual;",
        "int a = 1;",
    ),
    (
        "campanha: so a rota pedida conta",
        "Sim.Metas.cs",
        "(c[i].Arg < 0 || c[i].Arg == arg)",
        "true",
    ),
    (
        "campanha: conta antes de virar o ativo",
        "Sim.Metas.cs",
        "for (int i = _feitos[a]; i < c.Length; i++)",
        "for (int i = _feitos[a]; i < c.Length && i == _feitos[a]; i++)",
    ),
    (
        "campanha: voo perdido zera a sequencia",
        "Sim.Metas.cs",
        "else Conta(Meta.Seguidos, -1, true);",
        "",
    ),
    (
        "campanha: so a sequencia recua",
        "Sim.Metas.cs",
        "else Conta(Meta.Seguidos, -1, true);",
        "else { Conta(Meta.Seguidos, -1, true); Conta(Meta.Embarcados, -1, true); Conta(Meta.Voos, -1, true); }",
    ),
    (
        "campanha: perigo so conta resolvido",
        "Sim.Metas.cs",
        "case Ev.IncidenteFim: if (b == 1) Conta(",
        "case Ev.IncidenteFim: Conta(",
    ),
    (
        "campanha: nao tranca o jogo",
        "Sim.Rotas.cs",
        "|| ProximoAeroporto >= 0 || Marco < T.Aeroportos[a].Marco) return -1;",
        "|| ProximoAeroporto >= 0 || Marco < T.Aeroportos[a].Marco || ObjetivoAtual(AeroportoAtual) != null) return -1;",
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
    filtro = sys.argv[1] if len(sys.argv) > 1 else ""
    lista = [m for m in MUTANTES if filtro in m[0]]
    for nome, arq, de, para in lista:
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
    print("mutacao: %d de %d mutantes mortos" % (len(lista) - vivos, len(lista)))
    sys.exit(1 if vivos else 0)


if __name__ == "__main__":
    main()
