"""Relatório do playtest do Tiny Airport (v0.1) a partir de 1..N diários exportados: embarque médio por voo e K2.

Uso (da raiz do repositório; os dados ficam fora do git):
  python client/tools/diario_report.py                          # lê o diário do build Windows deste PC
  python client/tools/diario_report.py playtest/rodada1         # pasta (todos os *.csv e *.txt) ou arquivos
  python client/tools/diario_report.py playtest/rodada1 --saida playtest/rodada1/relatorio.md
  python client/tools/diario_report.py --autoteste              # diários sintéticos: cálculos, faixas e casos de borda
Opção: --versao 0.1.0-alpha (só essa versão). Saída: 0 ok; 1 diário padrão não existe; 2 caminho dado não existe.

Diário (docs/CONTRATO_V01.md §6), uma linha por evento, vírgula crua, sem aspas; a 1ª linha é o cabeçalho:
  utc,instalacao,versao,sessao,t_sessao,t_jogo,evento,a,b,c,onda
  utc = ISO 8601 UTC; instalacao = GUID local (sem dado pessoal); sessao = 8 hex por abertura do app;
  t_sessao = s reais desde a abertura; t_jogo = s de jogo do relógio do quadro (atravessa sessões; não anda com o
  jogo fechado nem pausado); onda = número da onda (1 = a 1ª). a, b, c são inteiros (vazios se o evento não usa).
Eventos (a/b/c):
  session_start | save_loaded a=0 novo,1 ok,2 recusado | tutorial_started | tutorial_completed a=1 se pulou |
  tour_step a=passo(1..9) b=1 feito,0 pulado | flight_spawned a=voo b=passageiros c=s de fechamento na onda |
  flight_boarding_started a=voo | flight_departed a=voo b=embarcados c=total | flight_missed a=voo b=perdidos c=total |
  queue_length_sampled a=estação(0 check-in,1 segurança,2 loja,3 portão) b=fila c=estado(0 normal,1 atenção,2 crítico) |
  station_upgraded a=estação b=melhoria(0 velocidade,1 capacidade) c=nível novo | staff_moved a=funcionário b=de c=para |
  staff_hired a=funcionário b=custo c=estação | story_delivered a=história b=passageiro c=1 se nova no álbum |
  offline_claim a=cash b=segundos | rewarded_ad_offered/_completed a=anúncio(0 cofre 2x,1 receita 2x) |
  reward_granted a=anúncio b=valor | diary_export.
  Fase C (a/b/c, contados no total, sem conta própria): crew_hired tripulante/custo/função | crew_assigned tripulante/voo/1 manual |
  crew_missing voo/função | crew_rest_started tripulante/s | aircraft_landed avião/voo | ground_service_done avião/serviço |
  turnaround_done avião/voo/1 depois do fechamento | flight_takeoff avião/voo/atraso s | ground_staff_hired pessoas/custo |
  ground_vehicle_upgraded serviço/melhoria/nível | maintenance_warning e maintenance_due avião/desgaste |
  maintenance_check_started avião/custo/1 inspeção | maintenance_check_done avião/desgaste | hangar_started avião/custo |
  hangar_done avião | station_unlocked estação.
Um arquivo cujo cabeçalho não é o acima (diário de outro jogo ou de outra versão do formato) é contado e ignorado.
"""

import argparse
import glob
import math
import os
import re
import statistics
import sys
import tempfile
from collections import Counter, defaultdict, namedtuple
from datetime import datetime, timedelta

PADRAO = os.path.join(
    os.environ.get("USERPROFILE", ""),
    "AppData",
    "LocalLow",
    "V-STACK",
    "Tiny Airport",
    "diario.csv",
)
CAB = "utc,instalacao,versao,sessao,t_sessao,t_jogo,evento,a,b,c,onda"
ESTACOES = ["check-in", "segurança", "loja", "portão"]
MELHORIAS = ["velocidade", "capacidade"]
ANUNCIOS = ["cofre 2x", "receita 2x"]
CRITICO = 2  # c do queue_length_sampled
JANELA = 60.0  # K2: s de jogo, depois do início do episódio crítico, para mover alguém ou comprar upgrade na estação
MIN_PESSOAS = 5  # K2: amostra mínima (proposta do CONTRATO_V01 §7)
EMBARQUE = (80, 65)  # Origem: >= 80 continuar, 65-80 iterar, < 65 cancelar
K2 = (60, 45)  # Origem: >= 60 continuar, 45-60 iterar, < 45 cancelar

Linha = namedtuple("Linha", "inst versao sessao utc ts tj ev a b c onda ordem")


# ------------------------------------------------------------------ leitura


def _num(s, tipo):
    """'' -> None (campo que o evento não usa); texto que não vira número (ou nan/inf) -> ValueError."""
    if s == "":
        return None
    v = tipo(s)
    if tipo is float and not math.isfinite(v):
        raise ValueError(s)
    return v


def parse(bruta):
    """Linha -> tupla de Linha sem a ordem, ou None. O jogo grava best-effort: linha cortada, campo a mais/menos ou
    número podre é lixo contado, nunca exceção."""
    r = bruta.split(",")
    if len(r) != 11 or not r[1] or not r[3] or not re.fullmatch(r"[a-z_]+", r[6]):
        return None
    try:
        utc = datetime.strptime(r[0].removesuffix("Z"), "%Y-%m-%dT%H:%M:%S")
        ts, tj = _num(r[4], float), _num(r[5], float)
        a, b, c, onda = (_num(x, int) for x in r[7:11])
    except ValueError:
        return None
    return (
        None
        if ts is None or tj is None
        else (r[1], r[2], r[3], utc, ts, tj, r[6], a, b, c, onda)
    )


def ler(caminhos):
    """-> (linhas, qualidade). Linhas na ordem cronológica: o diário é append-only, então a ordem é (arquivo, linha) com
    os arquivos pela 1ª data. Duas exportações do mesmo aparelho se sobrepõem: deduplica por (linha inteira, n-ésima
    repetição dela no arquivo), porque a linha já leva instalação e sessão."""
    q, arquivos = Counter(), []
    for c in caminhos:
        arquivos += (
            sorted(
                glob.glob(os.path.join(glob.escape(c), "*.csv"))
                + glob.glob(os.path.join(glob.escape(c), "*.txt"))
            )
            if os.path.isdir(c)
            else [c]
        )
    lidos = []
    for arq in arquivos:
        q["arquivos"] += 1
        with open(arq, encoding="utf-8-sig", errors="replace") as f:
            texto = f.read().replace(
                "\0", "\n"
            )  # escrita cortada pode deixar NUL: vira quebra, a linha seguinte se salva
        brutas = [x for x in (y.strip() for y in texto.splitlines()) if x]
        if not brutas:
            q["arquivos vazios"] += 1
        elif brutas[0] != CAB and brutas[0].startswith("utc,"):
            q["arquivos com outro cabeçalho"] += (
                1  # colunas desconhecidas: não dá para confiar em nenhuma linha
            )
        else:
            ps, oc = [], Counter()
            for bruta in brutas:
                if bruta == CAB:  # cabeçalho de arquivo colado em outro
                    continue
                p = parse(bruta)
                if p is None:
                    q["linhas ruins"] += 1
                else:
                    oc[bruta] += 1
                    ps.append(((bruta, oc[bruta]), p))
            lidos.append((min((p[3] for _, p in ps), default=datetime.max), ps))
    vistas, linhas = set(), []
    for n, (_, ps) in enumerate(sorted(lidos, key=lambda x: x[0])):
        for i, (chave, p) in enumerate(ps):
            if chave in vistas:
                q["linhas duplicadas"] += 1
            else:
                vistas.add(chave)
                linhas.append(Linha(*p, (n, i)))
    q["linhas válidas"] = len(linhas)
    return linhas, q


# ------------------------------------------------------------------ análise


def episodio(ls, i):
    """Episódio crítico que começa em ls[i] -> (i, estação, s até a 1ª ação em até JANELA, ou None). Ação = staff_moved com
    c = a estação OU station_upgraded com a = a estação. ponytail: varre as linhas seguintes (O(n) por episódio); um
    diário de horas tem milhares de linhas e poucas dezenas de episódios."""
    ini = ls[i]
    est = ini.a
    dts = [
        x.tj - ini.tj
        for x in ls[i + 1 :]
        if 0 <= x.tj - ini.tj <= JANELA + 1e-6
        and (
            x.ev == "staff_moved"
            and x.c == est
            or x.ev == "station_upgraded"
            and x.a == est
        )
    ]
    return i, est, min(dts, default=None)


def pessoa(inst, ls):
    """Tudo de uma instalação. ls = suas linhas na ordem do diário."""
    n = Counter(l.ev for l in ls)
    fim = next((i for i, l in enumerate(ls) if l.ev == "tutorial_completed"), None)
    passos = [l.a for l in ls if l.ev == "tour_step" and l.a is not None]
    tut = (
        ("pulado" if ls[fim].a == 1 else "concluído")
        if fim is not None
        else "não concluiu"
        if n["tutorial_started"] or passos
        else "-"
    )
    ant, eps = (
        {},
        [],
    )  # estado anterior por estação: um episódio crítico não reabre enquanto a estação segue crítica
    for i, l in enumerate(ls):
        if l.ev == "queue_length_sampled" and l.a is not None and l.c is not None:
            if l.c == CRITICO and ant.get(l.a) != CRITICO:
                eps.append(episodio(ls, i))
            ant[l.a] = l.c
    primeiro = next(
        (e for e in eps if fim is None or e[0] > fim), None
    )  # 1º episódio depois do tutorial
    return {
        "id": inst[:8],
        "sessoes": len({l.sessao for l in ls}),
        "tj": max(l.tj for l in ls),
        "ondas": max((l.onda or 0 for l in ls), default=0),
        "tut": tut,
        "passo": passos[-1] if tut == "não concluiu" and passos else None,
        "voos": [
            l.b / l.c
            for l in ls
            if l.ev == "flight_departed" and l.b is not None and l.c
        ],
        "eps": eps,
        "k2": None if primeiro is None else primeiro[2] is not None,
    }


def filtrar(linhas, versao):
    return [l for l in linhas if l.versao == versao] if versao else linhas


def analisar(linhas, versao=None):
    """-> (pessoas, resumo). O resumo traz os números dos critérios da Origem (percentuais em 0-100)."""
    por = defaultdict(list)
    for l in filtrar(linhas, versao):
        por[l.inst].append(l)
    ps = [pessoa(i, ls) for i, ls in sorted(por.items())]
    voos = [v for P in ps for v in P["voos"]]
    emb = [statistics.mean(P["voos"]) * 100 for P in ps if P["voos"]]
    eps = [e for P in ps for e in P["eps"]]
    agiu = [e[2] for e in eps if e[2] is not None]
    k2 = [P["k2"] for P in ps if P["k2"] is not None]
    return ps, {
        "emb_pessoas": statistics.mean(emb) if emb else None,
        "n_emb": len(emb),
        "emb_voos": statistics.mean(voos) * 100 if voos else None,
        "n_voos": len(voos),
        "k2_n": len(k2),
        "k2_agiram": sum(k2),
        "k2_pessoas": pc(sum(k2), len(k2)),
        "ep_n": len(eps),
        "ep_agiu": len(agiu),
        "k2_eps": pc(len(agiu), len(eps)),
        "t_mediano": statistics.median(agiu) if agiu else None,
    }


# ------------------------------------------------------------------ formatação e faixas


def pc(n, d):
    return None if not d else 100.0 * n / d


def fp(v):
    return "-" if v is None else f"{v:.1f}%"


def mmss(s):
    return f"{int(s) // 60}:{int(s) % 60:02d}"


def faixa(v, alvos):
    """alvos = (continuar, iterar): >= continuar CONTINUAR; >= iterar ITERAR; abaixo CANCELAR."""
    return (
        "SEM DADOS"
        if v is None
        else "CONTINUAR"
        if v >= alvos[0]
        else "ITERAR"
        if v >= alvos[1]
        else "CANCELAR"
    )


def faixa_k2(R):
    if R["k2_n"] < MIN_PESSOAS:
        return f"amostra insuficiente ({R['k2_n']} pessoas, mínimo {MIN_PESSOAS})"
    return faixa(R["k2_pessoas"], K2)


def tabela(cab, linhas):
    out = ["| " + " | ".join(cab) + " |", "|" + "|".join("---" for _ in cab) + "|"]
    return out + ["| " + " | ".join(str(c) for c in l) + " |" for l in linhas]


def nome(lista, i, pref):
    return lista[i] if i is not None and 0 <= i < len(lista) else f"{pref} {i}"


# ------------------------------------------------------------------ relatório (Markdown)


def relatorio(linhas, q, versao=None):
    linhas = filtrar(linhas, versao)
    ps, R = analisar(linhas)
    s = [
        "# Tiny Airport - relatório do diário de playtest",
        "",
        f"Pessoas (instalações): **{len(ps)}** | sessões: {sum(P['sessoes'] for P in ps)} | "
        f"minutos de jogo: {sum(P['tj'] for P in ps) / 60:.1f} | filtro de versão: {versao or 'todas'}",
        "",
        "> Amostra de playtest: com poucas pessoas leia a faixa como sinal e confira com a observação. "
        "O K2 só conta pessoas que tiveram episódio crítico.",
        "",
    ]
    vs = Counter(l.versao for l in linhas)
    if len(vs) > 1:
        s += [
            f"> **ATENÇÃO: versões misturadas** ({', '.join(f'{k}: {n} linhas' for k, n in vs.most_common())}). "
            "Populações diferentes não se somam: rode de novo com `--versao`.",
            "",
        ]

    s += ["## Critérios da Origem", ""]
    t = R["t_mediano"]
    s += tabela(
        ["critério", "resultado", "faixa"],
        [
            (
                "Embarque médio por voo, média das pessoas (>= 80% continuar, 65-80% iterar, < 65% cancelar)",
                f"{fp(R['emb_pessoas'])} ({R['n_emb']} pessoas)",
                f"**{faixa(R['emb_pessoas'], EMBARQUE)}**",
            ),
            (
                "Embarque médio por voo, média de todos os voos",
                f"{fp(R['emb_voos'])} ({R['n_voos']} voos)",
                f"**{faixa(R['emb_voos'], EMBARQUE)}**",
            ),
            (
                f"K2 pessoas: agiram em até {JANELA:g} s no 1º episódio crítico depois do tutorial "
                "(>= 60% continuar, 45-60% iterar, < 45% cancelar; mínimo 5 pessoas)",
                f"{fp(R['k2_pessoas'])} ({R['k2_agiram']}/{R['k2_n']} pessoas)",
                f"**{faixa_k2(R)}**",
            ),
            (
                f"K2 episódios: com ação em até {JANELA:g} s",
                f"{fp(R['k2_eps'])} ({R['ep_agiu']}/{R['ep_n']}); mediana até a ação: {'-' if t is None else f'{t:.1f} s'}",
                "informativo",
            ),
        ],
    )
    s += [
        "",
        "Ação = mover um funcionário PARA a estação crítica ou comprar upgrade nela. Episódio = a estação passa a "
        "crítico vindo de outro estado (ou no 1º registro dela); continuar crítico não abre outro.",
        "",
    ]

    s += ["## Por pessoa", ""]
    nt = Counter(P["tut"] for P in ps)
    s += tabela(
        [
            "instalação",
            "sessões",
            "min de jogo",
            "ondas",
            "tutorial",
            "parou após o passo",
            "voos",
            "embarque",
            "K2",
        ],
        [
            (
                P["id"],
                P["sessoes"],
                f"{P['tj'] / 60:.1f}",
                P["ondas"],
                P["tut"],
                "-" if P["passo"] is None else P["passo"],
                len(P["voos"]),
                fp(statistics.mean(P["voos"]) * 100) if P["voos"] else "-",
                "-" if P["k2"] is None else "agiu" if P["k2"] else "não agiu",
            )
            for P in ps
        ]
        + [
            (
                "**total**",
                sum(P["sessoes"] for P in ps),
                f"{sum(P['tj'] for P in ps) / 60:.1f}",
                sum(P["ondas"] for P in ps),
                f"{nt['concluído']} concl. / {nt['pulado']} pul. / {nt['não concluiu']} não concl.",
                "-",
                R["n_voos"],
                f"{fp(R['emb_pessoas'])} pessoas / {fp(R['emb_voos'])} voos",
                f"{R['k2_agiram']}/{R['k2_n']}",
            )
        ],
    )

    s += ["", "## K2 por estação", ""]
    por_est = defaultdict(list)
    for P in ps:
        for _, est, dt in P["eps"]:
            por_est[est].append(dt)
    s += tabela(
        ["estação", "episódios", f"com ação em até {JANELA:g} s", "%"],
        [
            (
                nome(ESTACOES, est, "estação"),
                len(v),
                sum(x is not None for x in v),
                fp(pc(sum(x is not None for x in v), len(v))),
            )
            for est, v in sorted(por_est.items(), key=lambda kv: (kv[0] is None, kv[0]))
        ]
        or [("-", 0, 0, "-")],
    )

    # uso: contagens e somas por evento (n[(ev,)]) e por evento+anúncio/estação (n[(ev, a)]); quem[...] = instalações
    n, quem, soma, up = Counter(), defaultdict(set), Counter(), Counter()
    for l in linhas:
        for k in ((l.ev,), (l.ev, l.a)):
            n[k] += 1
            quem[k].add(l.inst)
            soma[k + ("a",)] += l.a or 0
            soma[k + ("b",)] += l.b or 0
        if l.ev == "station_upgraded":
            up[(l.a, l.b)] += 1
    s += ["", "## Uso", ""]
    off = ("offline_claim",)
    s += [
        f"- Ganho offline: {n[off]} coletas por {len(quem[off])} pessoas | Cash {soma[off + ('a',)]} | "
        f"tempo {mmss(soma[off + ('b',)])}",
        f"- Voos: {n[('flight_departed',)]} partiram, {n[('flight_missed',)]} perdidos (eventos flight_missed)",
        f"- Histórias entregues: {n[('story_delivered',)]} por {len(quem[('story_delivered',)])} pessoas; "
        f"novas no álbum: {sum(1 for l in linhas if l.ev == 'story_delivered' and l.c == 1)}",
        f"- Contratações: {n[('staff_hired',)]} por {len(quem[('staff_hired',)])} pessoas | custo total {soma[('staff_hired', 'b')]}",
        f"- Mudanças de equipe (staff_moved): {n[('staff_moved',)]} por {len(quem[('staff_moved',)])} pessoas",
        "",
        "Rewarded (opt-in = pessoas que concluíram >= 1 / pessoas a quem foi oferecido):",
        "",
    ]
    ofe, con, dar = "rewarded_ad_offered", "rewarded_ad_completed", "reward_granted"
    ads = sorted(
        {
            k[1]
            for k in n
            if len(k) == 2 and k[0] in (ofe, con, dar) and k[1] is not None
        }
    )

    def linha_ad(rotulo, k):
        o, c = quem[(ofe,) + k], quem[(con,) + k] & quem[(ofe,) + k]
        return (
            rotulo,
            n[(ofe,) + k],
            n[(con,) + k],
            n[(dar,) + k],
            soma[(dar,) + k + ("b",)],
            len(o),
            len(c),
            fp(pc(len(c), len(o))),
        )

    s += tabela(
        [
            "anúncio",
            "oferecido",
            "concluído",
            "recompensas",
            "valor (soma de b)",
            "pessoas a quem foi oferecido",
            "pessoas que concluíram",
            "opt-in",
        ],
        [linha_ad(nome(ANUNCIOS, a, "anúncio"), (a,)) for a in ads]
        + [linha_ad("**todos**", ())],
    )
    s += ["", "Upgrades (station_upgraded):", ""]
    s += tabela(
        ["estação"] + MELHORIAS + ["total"],
        [
            (
                nome(ESTACOES, est, "estação"),
                *(up[(est, m)] for m in range(len(MELHORIAS))),
                sum(v for (e, _), v in up.items() if e == est),
            )
            for est in sorted({e for e, _ in up}, key=lambda e: (e is None, e))
        ]
        or [("-", 0, 0, 0)],
    )

    s += ["", "## Dados lidos", ""]
    s += tabela(
        ["item", "quantidade"],
        [
            (k, q[k])
            for k in (
                "arquivos",
                "arquivos vazios",
                "arquivos com outro cabeçalho",
                "linhas válidas",
                "linhas duplicadas",
                "linhas ruins",
            )
        ],
    )
    s += [
        "",
        "- Versões (linhas): "
        + (", ".join(f"{k} {v}" for k, v in vs.most_common()) or "-"),
        "",
    ]
    return "\n".join(s)


# ------------------------------------------------------------------ autoteste


class Diario:
    """Escreve um diário sintético no formato do jogo, com t_jogo explícito por evento."""

    def __init__(self, n, versao="0.1.0-alpha"):
        self.inst, self.versao, self.l, self.ts, self.tj = (
            f"{n:08x}-1111-4222-8333-444455556666",
            versao,
            [],
            0.0,
            0.0,
        )

    def sessao(self, sid, utc="2026-10-29T18:00:00"):
        self.sid, self.t0, self.ts = (
            sid,
            datetime.strptime(utc, "%Y-%m-%dT%H:%M:%S"),
            0.0,
        )
        return self.ln("session_start")

    def ln(self, ev, a="", b="", c="", tj=None, onda=1):
        self.tj = self.tj if tj is None else float(tj)
        utc = self.t0 + timedelta(seconds=self.ts)
        self.l.append(
            f"{utc:%Y-%m-%dT%H:%M:%SZ},{self.inst},{self.versao},{self.sid},{self.ts:.1f},{self.tj:.1f},{ev},{a},{b},{c},{onda}"
        )
        self.ts += 1
        return self

    def tutorial(self, pulou=0):
        self.ln("tutorial_started", tj=0)
        for p in range(1, 10):
            self.ln("tour_step", p, 1, tj=p)
        return self.ln("tutorial_completed", pulou, tj=20)


def jogador(n, dt=None, est=1, embarque=(10, 10)):
    """Termina o tutorial, voa uma vez, a estação `est` fica crítica em t_jogo=100 e a pessoa move alguém para ela dt s depois."""
    d = Diario(n)
    d.sessao("aaaa0001").tutorial()
    d.ln("flight_departed", 1, embarque[0], embarque[1], tj=60)
    d.ln("queue_length_sampled", est, 4, 0, tj=90)
    d.ln("queue_length_sampled", est, 9, 2, tj=100)
    if dt is not None:
        d.ln("staff_moved", 1, 0, est, tj=100 + dt)
    return d


def bordas():
    """7 pessoas com os casos de borda do K2 e do tutorial; os números esperados estão no autoteste."""
    ds = []
    d = Diario(1)  # p1: ação aos 59 s (conta); 2 voos: 100% e 80%
    d.sessao("aaaa0001").tutorial()
    d.ln("flight_departed", 1, 10, 10, tj=60).ln("queue_length_sampled", 1, 3, 0, tj=90)
    d.ln("queue_length_sampled", 1, 9, 2, tj=100, onda=3).ln(
        "flight_departed", 2, 8, 10, tj=120, onda=3
    )
    d.ln("staff_moved", 2, 0, 1, tj=159, onda=3)
    ds.append(d)
    d = Diario(2)  # p2: ação aos 61 s (não conta); voo 50%
    d.sessao("aaaa0002").tutorial()
    d.ln("queue_length_sampled", 2, 4, 1, tj=90).ln(
        "queue_length_sampled", 2, 9, 2, tj=100
    )
    d.ln("flight_departed", 1, 5, 10, tj=130).ln("station_upgraded", 2, 0, 1, tj=161)
    ds.append(d)
    d = Diario(3)  # p3: ação só em OUTRA estação (não conta); voo 70%
    d.sessao("aaaa0003").tutorial()
    d.ln("queue_length_sampled", 0, 9, 2, tj=100).ln("staff_moved", 1, 0, 3, tj=110).ln(
        "station_upgraded", 1, 0, 1, tj=120
    )
    d.ln("flight_departed", 1, 7, 10, tj=130)
    ds.append(d)
    d = Diario(4)  # p4: segue crítico em 130 e 160: um episódio só; ação aos 65 s
    d.sessao("aaaa0004").tutorial()
    d.ln("queue_length_sampled", 3, 4, 1, tj=90)
    for t in (100, 130, 160):
        d.ln("queue_length_sampled", 3, 9, 2, tj=t)
    d.ln("staff_moved", 1, 0, 3, tj=165).ln("flight_departed", 1, 9, 10, tj=170)
    ds.append(d)
    d = Diario(5)  # p5: 2 episódios; o 1º sem ação, o 2º com ação aos 10 s; sem voos
    d.sessao("aaaa0005").tutorial()
    d.ln("queue_length_sampled", 0, 4, 0, tj=90).ln(
        "queue_length_sampled", 0, 9, 2, tj=100
    )
    d.ln("queue_length_sampled", 0, 2, 0, tj=200).ln(
        "queue_length_sampled", 0, 9, 2, tj=300
    ).ln("staff_moved", 1, 2, 0, tj=310)
    ds.append(d)
    d = Diario(6)  # p6: 1º episódio é ANTES do tutorial (não vale); depois age aos 30 s
    d.sessao("aaaa0006")
    d.ln("tutorial_started", tj=0).ln("queue_length_sampled", 1, 9, 2, tj=10).ln(
        "tutorial_completed", 1, tj=40
    )
    d.ln("queue_length_sampled", 1, 2, 0, tj=50).ln(
        "queue_length_sampled", 2, 9, 2, tj=100
    )
    d.ln("station_upgraded", 2, 1, 2, tj=130).ln("flight_departed", 1, 6, 10, tj=140)
    ds.append(d)
    d = Diario(7)  # p7: abandonou o tutorial depois do passo 3; sem episódio
    d.sessao("aaaa0007").ln("tutorial_started", tj=0)
    for p in (1, 2, 3):
        d.ln("tour_step", p, 1, tj=p * 5)
    ds.append(d)
    return ds


def autoteste():
    def linha(texto, trecho):
        return next(x for x in texto.splitlines() if trecho in x)

    with tempfile.TemporaryDirectory() as tmp:

        def rodar(arquivos, versao=None):
            """arquivos = listas de linhas (cada lista vira um .csv com cabeçalho) -> (pessoas, resumo, texto, qualidade)."""
            for f in glob.glob(os.path.join(tmp, "*")):
                os.remove(f)
            for i, ls in enumerate(arquivos):
                with open(
                    os.path.join(tmp, f"j{i:02d}.csv"),
                    "w",
                    encoding="utf-8",
                    newline="\n",
                ) as f:
                    f.write(
                        "\n".join(
                            ([CAB] if ls and not ls[0].startswith("utc,") else []) + ls
                        )
                        + ("\n" if ls else "")
                    )
            linhas, q = ler([tmp])
            ps, R = analisar(linhas, versao)
            return ps, R, relatorio(linhas, q, versao), q

        # --- casos de borda: 7 pessoas (p5 sem voos, p7 sem episódio)
        ps, R, texto, q = rodar([d.l for d in bordas()])
        assert (
            len(ps) == 7 and q["linhas ruins"] == 0 and q["linhas duplicadas"] == 0
        ), (len(ps), q)
        # embarque: pessoas 90, 50, 70, 90, 60 -> 72,0 (iterar); voos 100, 80, 50, 70, 90, 60 -> 75,0 (iterar)
        assert (
            abs(R["emb_pessoas"] - 72.0) < 1e-9
            and R["n_emb"] == 5
            and abs(R["emb_voos"] - 75.0) < 1e-9
            and R["n_voos"] == 6
        ), R
        # K2 episódios: p1 1, p2 1, p3 1, p4 1 (não reabre), p5 2, p6 2 = 8; com ação: p1 (59 s), p5 (10 s), p6 (30 s) = 3
        assert (R["ep_n"], R["ep_agiu"], R["k2_eps"], R["t_mediano"]) == (
            8,
            3,
            37.5,
            30.0,
        ), R
        # K2 pessoas: 1º episódio depois do tutorial: p1 sim, p2 não (61 s), p3 não (outra estação), p4 não (65 s),
        # p5 não (1º sem ação; o 2º não vale), p6 sim (o de antes do tutorial não vale); p7 sem episódio fica fora
        assert (R["k2_n"], R["k2_agiram"]) == (6, 2) and abs(
            R["k2_pessoas"] - 100 * 2 / 6
        ) < 1e-9, R
        assert [P["k2"] for P in ps] == [
            True,
            False,
            False,
            False,
            False,
            True,
            None,
        ], [P["k2"] for P in ps]
        assert [len(P["eps"]) for P in ps] == [1, 1, 1, 1, 2, 2, 0]
        assert (
            [P["tut"] for P in ps] == ["concluído"] * 5 + ["pulado", "não concluiu"]
            and ps[6]["passo"] == 3
            and ps[0]["passo"] is None
        )
        assert (
            [P["sessoes"] for P in ps] == [1] * 7
            and ps[0]["ondas"] == 3
            and ps[1]["ondas"] == 1
            and abs(ps[0]["tj"] - 159) < 1e-9
        )
        assert (
            faixa_k2(R) == "CANCELAR" and faixa(R["emb_pessoas"], EMBARQUE) == "ITERAR"
        )
        assert (
            linha(texto, "K2 pessoas").endswith("**CANCELAR** |")
            and "33.3% (2/6 pessoas)" in texto
        )
        assert (
            "37.5% (3/8); mediana até a ação: 30.0 s" in texto
            and "72.0% (5 pessoas)" in texto
            and "75.0% (6 voos)" in texto
        )
        # K2 por estação: check-in p3 + p5 (2) = 3 episódios, 1 com ação; segurança p1 + p6 (antes do tutorial) = 2, 1;
        # loja p2 + p6 = 2, 1; portão p4 = 1, 0
        assert (
            "| check-in | 3 | 1 | 33.3% |" in texto
            and "| segurança | 2 | 1 | 50.0% |" in texto
            and "| loja | 2 | 1 | 50.0% |" in texto
            and "| portão | 1 | 0 | 0.0% |" in texto
        ), texto
        assert "| 00000007 | 1 | " in texto and "| não concluiu | 3 |" in texto
        assert (
            "| **total** | 7 | " in texto
            and "5 concl. / 1 pul. / 1 não concl." in texto
        )

        # --- portão do K2: 5 pessoas, 3 agem = 60,0% (fronteira: continuar); 10 pessoas, 5 agem = 50,0% (iterar); 4 de 10 = 40,0% (cancelar)
        for dts, esperado in (
            ([10, 10, 10, None, 70], "CONTINUAR"),
            ([10] * 5 + [None] * 5, "ITERAR"),
            ([10] * 4 + [None] * 6, "CANCELAR"),
            ([10] * 7 + [None] * 5, "ITERAR"),  # 58,3%: abaixo de 60
            ([10] * 9 + [None] * 11, "ITERAR"),  # 45,0% exato
            ([10] * 4 + [None] * 5, "CANCELAR"),  # 44,4%
        ):
            _, R, texto, _ = rodar([jogador(k + 1, dt).l for k, dt in enumerate(dts)])
            assert faixa_k2(R) == esperado and linha(texto, "K2 pessoas").endswith(
                f"**{esperado}** |"
            ), (dts, R)
        # ação exatamente em 60 s conta (em até 60 s); 60,5 s não
        assert (
            rodar([jogador(1, 60).l])[1]["ep_agiu"] == 1
            and rodar([jogador(1, 60.5).l])[1]["ep_agiu"] == 0
        )
        # amostra < 5 pessoas: não decide, mesmo com 100%
        _, R, texto, _ = rodar([jogador(k + 1, 10).l for k in range(4)])
        assert (
            R["k2_pessoas"] == 100.0
            and faixa_k2(R) == "amostra insuficiente (4 pessoas, mínimo 5)"
        )
        assert linha(texto, "K2 pessoas").endswith(
            "**amostra insuficiente (4 pessoas, mínimo 5)** |"
        )

        # --- embarque: 8/10 = 80,0 (continuar), 65/100 = 65,0 (iterar), 64/100 = 64,0 (cancelar)
        for emb, esperado, v in (
            ((8, 10), "CONTINUAR", 80.0),
            ((65, 100), "ITERAR", 65.0),
            ((64, 100), "CANCELAR", 64.0),
        ):
            _, R, texto, _ = rodar([jogador(1, embarque=emb).l])
            assert abs(R["emb_pessoas"] - v) < 1e-9 and linha(
                texto, "média das pessoas"
            ).endswith(f"**{esperado}** |"), (emb, R)

        # --- linhas inválidas, cabeçalho errado, exportação repetida, arquivo vazio
        bom = jogador(1, 10).l
        ruins = [
            "2026-10-29T18:00:00Z,x,0.1.0,abc",  # colunas a menos
            "2026-10-29T18:00:00Z,x,0.1.0,aaaa0001,1.0,abc,flight_departed,1,5,10,1",  # t_jogo não é número
            "2026-10-29T18:00:00Z,x,0.1.0,aaaa0001,1.0,5.0,flight_departed,um,5,10,1",  # a não é inteiro
            "2026-10-29T18:00:00Z,x,0.1.0,aaaa0001,1.0,nan,flight_departed,1,5,10,1",
        ]  # t_jogo nan
        dc = [
            "utc,instalacao,versao,sessao,t_sessao,t_contrato,evento,a,b,contrato,sala",
            bom[1],
        ]  # cabeçalho do Dungeon Courier
        _, R, texto, q = rodar([bom + ruins[:2] + [CAB] + ruins[2:], dc, bom, []])
        assert (
            q["linhas ruins"] == 4
            and q["arquivos com outro cabeçalho"] == 1
            and q["arquivos vazios"] == 1
        ), q
        assert (
            q["linhas duplicadas"] == len(bom)
            and q["linhas válidas"] == len(bom)
            and q["arquivos"] == 4
        ), q
        assert (R["k2_n"], R["k2_agiram"], R["n_voos"]) == (
            1,
            1,
            1,
        )  # as inválidas e o DC não entram
        assert (
            "| linhas ruins | 4 |" in texto
            and "| arquivos com outro cabeçalho | 1 |" in texto
        )

        # --- uso: offline, rewarded (opt-in), histórias, contratações, upgrades; versões misturadas e --versao
        a, b = Diario(1), Diario(2, "0.2.0")
        a.sessao("aaaa0001").ln("offline_claim", 500, 3600).ln(
            "offline_claim", 200, 600
        )
        a.ln("rewarded_ad_offered", 0).ln("rewarded_ad_completed", 0).ln(
            "reward_granted", 0, 1000
        ).ln("rewarded_ad_offered", 0)
        a.ln("rewarded_ad_offered", 1).ln("story_delivered", 1, 7, 1).ln(
            "story_delivered", 2, 7, 0
        ).ln("staff_hired", 4, 300, 2)
        a.ln("station_upgraded", 0, 0, 2).ln("station_upgraded", 0, 1, 2).ln(
            "station_upgraded", 3, 0, 5
        )
        b.sessao("bbbb0001").ln("rewarded_ad_offered", 0).ln("rewarded_ad_offered", 1)
        ps, R, texto, _ = rodar([a.l, b.l])
        assert (
            "Ganho offline: 2 coletas por 1 pessoas | Cash 700 | tempo 70:00" in texto
            and "versões misturadas" in texto
        )
        assert (
            "| cofre 2x | 3 | 1 | 1 | 1000 | 2 | 1 | 50.0% |" in texto
            and "| receita 2x | 2 | 0 | 0 | 0 | 2 | 0 | 0.0% |" in texto
        )
        assert "| **todos** | 5 | 1 | 1 | 1000 | 2 | 1 | 50.0% |" in texto
        assert (
            "Histórias entregues: 2 por 1 pessoas; novas no álbum: 1" in texto
            and "Contratações: 1 por 1 pessoas | custo total 300" in texto
        )
        assert "| check-in | 1 | 1 | 2 |" in texto and "| portão | 1 | 0 | 1 |" in texto
        ps, R, texto, _ = rodar([a.l, b.l], versao="0.1.0-alpha")
        assert (
            len(ps) == 1
            and "versões misturadas" not in texto
            and "| cofre 2x | 2 | 1 | 1 | 1000 | 1 | 1 | 100.0% |" in texto
        )

        # --- vazio: nunca exceção
        ps, R, texto, _ = rodar([[]])
        assert (
            ps == []
            and R["k2_pessoas"] is None
            and "SEM DADOS" in texto
            and "amostra insuficiente (0 pessoas" in texto
        )
    print("autoteste OK")


if __name__ == "__main__":
    # UTF-8 também em pipe (console real do Windows ignora isso); o Markdown e o --saida têm o mesmo encoding
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser(
        description="Relatório do diário de playtest do Tiny Airport (v0.1)."
    )
    ap.add_argument(
        "caminhos",
        nargs="*",
        help="arquivos .csv/.txt ou pastas; padrão: o diário do build Windows deste PC",
    )
    ap.add_argument("--autoteste", action="store_true")
    ap.add_argument("--versao", help="só as linhas dessa versão do jogo")
    ap.add_argument(
        "--saida", help="grava o Markdown neste arquivo (UTF-8) em vez de imprimir"
    )
    o = ap.parse_args()
    if o.autoteste:
        autoteste()
        sys.exit(0)
    caminhos = o.caminhos or [PADRAO]
    faltando = [c for c in caminhos if not os.path.exists(c)]
    if faltando:
        print("não encontrei: " + ", ".join(faltando), file=sys.stderr)
        sys.exit(2 if o.caminhos else 1)
    md = relatorio(*ler(caminhos), o.versao)
    if o.saida:
        with open(o.saida, "w", encoding="utf-8") as f:
            f.write(md + "\n")
        print("relatório gravado em", o.saida)
    else:
        print(md)
