"""Prova de mutacao das travas da Fase C (CONTRATO_FASE_C §7): tira cada trava do nucleo, roda o dotnet test e exige que algum teste caia.

uso (da raiz do repo): python client/tools/mutacao.py [trecho do nome da trava ...]
MUTACAO_TESTES = filtro do dotnet test, para rodar so os testes da trava (ex.: FullyQualifiedName~ComprasTests).
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
        "&& a.Desgaste > 0 && MecanicoLivre && Cash >= T.CheckCusto;",
        "&& a.Desgaste > 0 && MecanicoLivre;",
    ),
    (
        "hangar so com Cash",
        "Sim.Patio.cs",
        "AntesDoEmbarque(aviao) != null && T.RegraManutencao && Cash >= T.HangarCusto;",
        "AntesDoEmbarque(aviao) != null && T.RegraManutencao;",
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
    (
        "missao: recompensa fixa, sem semente",
        "Sim.Missoes.cs",
        "int pago = T.RegraMissoes ? m.O.Recompensa : 0;",
        "int pago = T.RegraMissoes ? m.O.Recompensa + System.Math.Abs(Seed) % 7 : 0;",
    ),
    (
        "missao: recompensa fora do 2x e do cofre",
        "Sim.Missoes.cs",
        "Cash += pago;",
        "Ganha(pago);",
    ),
    (
        "missao: regra desligada nao paga",
        "Sim.Missoes.cs",
        "int pago = T.RegraMissoes ? m.O.Recompensa : 0;",
        "int pago = m.O.Recompensa;",
    ),
    (
        "missao: paga uma vez",
        "Sim.Missoes.cs",
        "if (m == null || m.Feita || !m.Cumpriu) continue;",
        "if (m == null || !m.Cumpriu) continue;",
    ),
    (
        "missao: semanal pede as duas partes",
        "Sim.Missoes.cs",
        "public bool Cumpriu => Conta >= O.Alvo && (O.E == null || ContaE >= O.E.Alvo);",
        "public bool Cumpriu => Conta >= O.Alvo;",
    ),
    (
        "missao: fechar e abrir nao paga de novo",
        "Sim.Missoes.cs",
        "Feita = x[3] != 0 };",
        "Feita = false };",
    ),
    (
        "missao: contador do save limitado",
        "Sim.Missoes.cs",
        "Conta = Math.Max(0, Math.Min(o.Alvo, x[1]))",
        "Conta = x[1]",
    ),
    (
        "missao: dia do jogo, nunca o do aparelho",
        "Sim.Missoes.cs",
        "if (DiaJogo != _dia) { _dia = DiaJogo; Sorteia(Diarias, T.Diarias, _dia, 40); }",
        "if (DiaJogo + (int)Dia != _dia) { _dia = DiaJogo + (int)Dia; Sorteia(Diarias, T.Diarias, _dia, 40); }",
    ),
    (
        "missao: dia do jogo no save limitado",
        "Sim.Missoes.cs",
        "_dia = Math.Max(-1, Math.Min(DiaJogo, p[0]));",
        "_dia = p[0];",
    ),
    (
        "missao: virada nao tira Cash",
        "Sim.Missoes.cs",
        "Array.Clear(vagas, 0, vagas.Length);",
        "Array.Clear(vagas, 0, vagas.Length); Cash = Math.Max(0, Cash - 10);",
    ),
    (
        "missao: sorteio so da semente",
        "Sim.Missoes.cs",
        "double nota = U(periodo, 0, i, sal);",
        "double nota = U(periodo, 0, i, sal) + (Cash > 50 && i % 2 == 0 ? 0.5 : 0);",
    ),
    (
        "missao: so a linha do marco",
        "Sim.Missoes.cs",
        "bool Elegivel(Objetivo o) => o.Marco == Marco && Cabe(o)",
        "bool Elegivel(Objetivo o) => o.Marco <= Marco && Cabe(o)",
    ),
    (
        "missao: nada que o jogo nao tem",
        "Sim.Missoes.cs",
        "case Meta.Atendidos: return Ativa((Estacao)o.Arg);",
        "case Meta.Atendidos: return true;",
    ),
    (
        "missao: rota aberta, no aeroporto atual",
        "Sim.Missoes.cs",
        "return o.Arg < 0 || RotaAberta(o.Arg) && T.Rotas[o.Arg].Aeroporto == AeroportoAtual;",
        "return true;",
    ),
    (
        "missao: catalogo no fim sai do sorteio",
        "Sim.Missoes.cs",
        "case Meta.Melhorias: return AVenda() >= o.Alvo;",
        "case Meta.Melhorias: return true;",
    ),
    (
        "missao: sem repetir a meta",
        "Sim.Missoes.cs",
        "&& !Repete(vagas, tabela[i])",
        "&& Array.Find(vagas, x => x != null && x.O == tabela[i]) == null",
    ),
    (
        "missao: offline nao conclui",
        "Sim.Offline.cs",
        "LastClaim = claimId;",
        "LastClaim = claimId; PassoMissoes();",
    ),
    (
        "missao: offline nao conta",
        "Sim.Offline.cs",
        "LastClaim = claimId;",
        "LastClaim = claimId; ContaMissoes(Meta.Embarcados, -1, false);",
    ),
    (
        "missao: anuncio nao anda missao",
        "Sim.Metas.cs",
        "case Ev.Melhorou: Conta(Meta.Melhorias); break;",
        "case Ev.Melhorou: Conta(Meta.Melhorias); break; case Ev.Recompensa: ContaMissoes(Meta.Embarcados, -1, false); break;",
    ),
    (
        "missao: so a rota ou a estacao pedida",
        "Sim.Missoes.cs",
        "static bool Casa(Objetivo o, Meta m, int arg) => o.Meta == m && (o.Arg < 0 || o.Arg == arg);",
        "static bool Casa(Objetivo o, Meta m, int arg) => o.Meta == m;",
    ),
    (
        "conquista: nao paga Cash",
        "Sim.Missoes.cs",
        "_conquistas |= 1L << i;",
        "_conquistas |= 1L << i; Cash += T.Conquistas[i].Recompensa;",
    ),
    (
        "conquista: feita fica no alvo",
        "Sim.Missoes.cs",
        "if (ConquistaFeita(i)) return o.Alvo;",
        "",
    ),
    (
        "conquista: uma vez",
        "Sim.Missoes.cs",
        "if (ConquistaFeita(i) || ProgressoConquista(i) < T.Conquistas[i].Alvo) continue;",
        "if (ProgressoConquista(i) < T.Conquistas[i].Alvo) continue;",
    ),
    (
        "conquista: fica feita ao reabrir",
        "Sim.Missoes.cs",
        "_conquistas = c[0] & (1L << _marca.Length) - 1;",
        "_conquistas = 0;",
    ),
    (
        "conquista: mascara limitada a tabela",
        "Sim.Missoes.cs",
        "_conquistas = c[0] & (1L << _marca.Length) - 1;",
        "_conquistas = c[0];",
    ),
    (
        "conquista: contador do save limitado",
        "Sim.Missoes.cs",
        "_marca[i] = (int)Math.Max(0, Math.Min(T.Conquistas[i].Alvo, c[i + 1]));",
        "_marca[i] = (int)c[i + 1];",
    ),
    (
        "compra: a mesma transacao rende uma so",
        "Compras.cs",
        "if (_tx.Contains(id)) return Credito.Repetido;",
        "",
    ),
    (
        "compra: o id da transacao vai no save",
        "Sim.Save.cs",
        'if (_tx.Count > 0) sb.Append("tx=").Append(string.Join(",", _tx)).Append(\'\\n\');',
        "",
    ),
    (
        "compra: o token nunca fica no save",
        "Compras.cs",
        "string id = SaveEnvelope.Hash(tx);",
        "string id = tx;",
    ),
    (
        "compra: nao consumivel paga uma vez",
        "Compras.cs",
        "if (unico && Tem(p.Id)) { _tx.Add(id); return Credito.JaTem; }",
        "",
    ),
    (
        "compra: id vazio nao credita",
        "Compras.cs",
        "if (string.IsNullOrWhiteSpace(tx) || p == null) return Credito.Invalido;",
        'if (p == null) return Credito.Invalido; tx = tx ?? "";',
    ),
    (
        "compra: save recusado nao credita",
        "Compras.cs",
        "if (SaveRefused) return Credito.SaveRecusado;",
        "",
    ),
    (
        "compra: teto nao engole",
        "Compras.cs",
        "if (p.Gems > T.GemsTeto - Gems) return Credito.Teto;",
        "if (p.Gems > T.GemsTeto - Gems) { _tx.Add(id); return Credito.Teto; }",
    ),
    (
        "compra: conteudo fixo, sem semente",
        "Compras.cs",
        "Gems += p.Gems;",
        "Gems += p.Gems + Seed % 2;",
    ),
    (
        "compra: Cash fora da media movel",
        "Compras.cs",
        "SomaCash(p.Cash);",
        "SomaCash(p.Cash); _ganhoPasso += p.Cash;",
    ),
    (
        "compra: pacote fora da media movel",
        "Compras.cs",
        "if (g.Tipo == TipoItem.PacoteCash) SomaCash(v);",
        "if (g.Tipo == TipoItem.PacoteCash) { SomaCash(v); _ganhoPasso += v; }",
    ),
    (
        "compra: pacote com teto",
        "Compras.cs",
        "Math.Min(T.PacoteTeto, Math.Floor(RateEma * T.PacoteSeg))",
        "Math.Floor(RateEma * T.PacoteSeg)",
    ),
    (
        "compra: pacote com piso",
        "Compras.cs",
        "(long)Math.Max(T.PacotePiso, Math.Min(T.PacoteTeto, Math.Floor(RateEma * T.PacoteSeg)))",
        "(long)Math.Min(T.PacoteTeto, Math.Floor(RateEma * T.PacoteSeg))",
    ),
    (
        "compra: booster nao acumula",
        "Compras.cs",
        "(g.Tipo == TipoItem.Booster ? BoostLeft == 0 : !Tem(g.Id))",
        "(g.Tipo == TipoItem.Booster || !Tem(g.Id))",
    ),
    (
        "compra: gasto so com saldo",
        "Compras.cs",
        "|| Gems < T.ItensGems[item].Preco) return false;",
        ") return false;",
    ),
    (
        "compra: saldo nao estoura",
        "Compras.cs",
        "Cash = Cash > long.MaxValue - v ? long.MaxValue : Cash + v;",
        "Cash += v;",
    ),
    (
        "compra: Gems nao repoem voo",
        "Compras.cs",
        "Gems -= g.Preco;",
        "Gems -= g.Preco; TotalPerdidos = 0;",
    ),
    (
        "compra: Gems nao andam a campanha",
        "Compras.cs",
        "Gems -= g.Preco;",
        "Gems -= g.Preco; if (_feitos[0] < 10) _feitos[0]++;",
    ),
    (
        "compra: Gems nao consertam aviao",
        "Compras.cs",
        "Gems -= g.Preco;",
        "Gems -= g.Preco; foreach (Aviao av in Avioes) av.Desgaste = 0;",
    ),
    (
        "compra: credito nao mexe no voo",
        "Compras.cs",
        "if (unico) _posse.Add(p.Id);",
        "if (unico) _posse.Add(p.Id); TotalPerdidos = 0;",
    ),
    (
        "compra: offline nao da Gems",
        "Sim.Offline.cs",
        "LastClaimCash = Math.Max(0, cash);",
        "LastClaimCash = Math.Max(0, cash); Gems += 1;",
    ),
    (
        "compra: anuncio nao da Gems",
        "Anuncios.cs",
        "_sim.Recompensa(a, v);",
        "_sim.Gems += 1; _sim.Recompensa(a, v);",
    ),
    (
        "compra: cadeia paga so ao fechar",
        "Sim.Metas.cs",
        "if (T.RegraCampanha && ObjetivoAtual(a) == null) GanhaGems(a);",
        "if (T.RegraCampanha) GanhaGems(a);",
    ),
    (
        "compra: campanha sem pagar nao da Gems",
        "Sim.Metas.cs",
        "if (T.RegraCampanha && ObjetivoAtual(a) == null) GanhaGems(a);",
        "if (ObjetivoAtual(a) == null) GanhaGems(a);",
    ),
    (
        "compra: saldo do save limitado",
        "Sim.Save.cs",
        'Gems = Math.Max(0, Math.Min(T.GemsTeto, Get(kv, "gems", 0)));',
        'Gems = Get(kv, "gems", 0);',
    ),
    (
        "compra: posse do save so de nao consumivel",
        "Sim.Save.cs",
        "if (p.Tipo == TipoProduto.NaoConsumivel && Array.IndexOf(posse, p.Id) >= 0) _posse.Add(p.Id);",
        "if (Array.IndexOf(posse, p.Id) >= 0) _posse.Add(p.Id);",
    ),
    (
        "compra: booster sobrevive a reabrir",
        "Sim.Save.cs",
        "Math.Min(s.BoostTeto,",
        "Math.Min(s.T.BoostSegundos * Hz,",
    ),
    (
        "passe: recompensa fixa, sem semente",
        "Sim.Passe.cs",
        "int v = T.RegraPasse ? n.Valor : 0;",
        "int v = T.RegraPasse ? n.Valor + Seed % 7 : 0;",
    ),
    (
        "passe: regra desligada nao paga",
        "Sim.Passe.cs",
        "int v = T.RegraPasse ? n.Valor : 0;",
        "int v = n.Valor;",
    ),
    (
        "passe: Cash fora do 2x e do cofre",
        "Sim.Passe.cs",
        "if (n.Premio == Premio.Cash) SomaCash(v);",
        "if (n.Premio == Premio.Cash) Ganha(v);",
    ),
    (
        "passe: niveis pagos vao no save",
        "Sim.Passe.cs",
        ".Append(PontosPasse).Append(',').Append(_pagos).Append",
        ".Append(PontosPasse).Append(',').Append(0).Append",
    ),
    (
        "passe: pagos do save limitado",
        "Sim.Passe.cs",
        "_pagos = Math.Max(0, Math.Min(NivelPasse, p[3]));",
        "_pagos = p[3];",
    ),
    (
        "passe: pontos do save limitados",
        "Sim.Passe.cs",
        "PontosPasse = Math.Max(0, Math.Min(Temporada.Gratis.Length * T.PassePontosNivel, p[2]));",
        "PontosPasse = p[2];",
    ),
    (
        "passe: dia do save limitado",
        "Sim.Passe.cs",
        "Math.Max(DiaJogo - Temporada.Dias, Math.Min(DiaJogo, p[1]));",
        "p[1];",
    ),
    (
        "passe: a proxima temporada abre no dia em que chega",
        "Sim.Passe.cs",
        "_inicio = p[1] < 0 ? DiaJogo : ",
        "_inicio = ",
    ),
    (
        "passe: dia do jogo, nunca o do aparelho",
        "Sim.Passe.cs",
        "if (DiaJogo < _inicio + t.Dias) return;",
        "if (Dia < _inicio + t.Dias) return;",
    ),
    (
        "passe: a virada zera os niveis pagos",
        "Sim.Passe.cs",
        "PontosPasse = 0; _pagos = 0;",
        "PontosPasse = 0;",
    ),
    (
        "passe: a virada nao tira Cash nem cosmetico",
        "Sim.Passe.cs",
        "_temp++; _inicio = DiaJogo;",
        "_temp++; _inicio = DiaJogo; Cash = 0; _posse.Clear();",
    ),
    (
        "passe: offline, anuncio e compra nao dao ponto",
        "Sim.Passe.cs",
        "case Ev.ObjetivoConcluido: p = T.PasseObjetivo; break;",
        "case Ev.ObjetivoConcluido: case Ev.Offline: case Ev.Recompensa: case Ev.CompraCreditada: case Ev.GemsGastas: p = T.PasseObjetivo; break;",
    ),
    (
        "passe: voo perdido nao tira ponto",
        "Sim.cs",
        "AnotaPasse(t, a, c);",
        "AnotaPasse(t, a, t == Ev.VooPartiu && b < c ? 0 : c);",
    ),
    (
        "passe: tempo soma ao 2x, com teto",
        "Sim.Passe.cs",
        "BoostLeft = (int)Math.Min(BoostTeto, BoostLeft + (long)v * Hz);",
        "BoostLeft = v * Hz;",
    ),
    (
        "passe: cosmetico sobrevive a reabrir",
        "Sim.Save.cs",
        "if (n.Premio == Premio.Cosmetico && Array.IndexOf(posse, n.Id) >= 0) _posse.Add(n.Id);",
        "if (n.Premio == Premio.Cosmetico && Array.IndexOf(posse, n.Id) >= 0) { }",
    ),
    (
        "passe: regra desligada nao da cosmetico",
        "Sim.Passe.cs",
        "else if (v > 0) _posse.Add(n.Id);",
        "else _posse.Add(n.Id);",
        "despesa: o caixa paga so o que tem",
        "Sim.Despesas.cs",
        "Hoje.Pago = Math.Min(conta, Cash);",
        "Hoje.Pago = conta;",
    ),
    (
        "despesa: a falta nao acumula",
        "Sim.Despesas.cs",
        "Atrasada = conta - Hoje.Pago;",
        "Atrasada += conta - Hoje.Pago;",
    ),
    (
        "despesa: a atrasada so sai inteira",
        "Sim.Despesas.cs",
        "if (Atrasada > 0 && Cash >= Atrasada)",
        "if (Atrasada > 0)",
    ),
    (
        "despesa: a falta pausa contratar e melhorar",
        "Sim.Despesas.cs",
        "=> c >= 0 && Cash >= c && !Pausado;",
        "=> c >= 0 && Cash >= c;",
    ),
    (
        "despesa: cobra so na virada do dia",
        "Sim.Despesas.cs",
        "if (Hoje.Dia == DiaJogo) return;",
        "if (Hoje.Dia == DiaJogo && Steps % 2 == 0) return;",
    ),
    (
        "despesa: regra desligada nao cobra",
        "Sim.Despesas.cs",
        "if (!T.RegraDespesas) return 0;",
        "",
    ),
    (
        "despesa: so a estacao ativa cobra",
        "Sim.Despesas.cs",
        "foreach (Station s in Estacoes) if (Ativa(s.Tipo))",
        "foreach (Station s in Estacoes) if (true)",
    ),
    (
        "despesa: o nivel da estacao cobra",
        "Sim.Despesas.cs",
        "(long)T.ManutencaoNivel[(int)s.Tipo] * (s.Vel + s.Cap);",
        "(long)T.ManutencaoNivel[(int)s.Tipo] * s.Vel;",
    ),
    (
        "despesa: offline nao cobra",
        "Sim.Offline.cs",
        "Recebe(cash, Fonte.Cofre);",
        "Recebe(cash, Fonte.Cofre); Cash -= Math.Min(Cash, Folha());",
    ),
    (
        "despesa: o relatorio guarda a fonte",
        "Sim.Despesas.cs",
        "Hoje.Receitas[(int)f] += v;",
        "Hoje.Receitas[0] += v;",
    ),
    (
        "despesa: a conta atrasada vai no save",
        "Sim.Despesas.cs",
        'Kv(sb, "atrasada", Atrasada);',
        "",
    ),
    (
        "despesa: atrasada do save limitada",
        "Sim.Despesas.cs",
        'Math.Min(Historico[Historico.Count - 1].Falta, Get(kv, "atrasada", 0))',
        'Get(kv, "atrasada", 0)',
    ),
    (
        "despesa: dia do save nunca no futuro",
        "Sim.Despesas.cs",
        "|| x[0] > DiaJogo) return null;",
        ") return null;",
    ),
    (
        "despesa: pago do save ate a conta",
        "Sim.Despesas.cs",
        "c.Pago = Math.Max(0, Math.Min(c.Despesa, x[1]));",
        "c.Pago = x[1];",
    ),
    (
        "chegada: a boca so poe no lugar vazio",
        "Sim.Chegada.cs",
        "if (e[boca] < 0 && Carrinho.Count > 0)",
        "if (Carrinho.Count > 0)",
    ),
    (
        "chegada: pega so com a mala em frente",
        "Sim.Chegada.cs",
        " || c.Vaga % n != (int)((j + giro) % n)) continue;",
        ") continue;",
    ),
    (
        "chegada: so pega quem esta esperando",
        "Sim.Chegada.cs",
        "c.Estado != EstadoChegada.Esperando || ",
        "",
    ),
    (
        "chegada: o carrinho entra na ordem",
        "Sim.Chegada.cs",
        "e[boca] = Carrinho[0];",
        "e[boca] = Carrinho[Carrinho.Count - 1]; Carrinho.RemoveAt(Carrinho.Count - 1); Carrinho.Insert(0, -9);",
    ),
    (
        "chegada: desce com o desembarque do patio",
        "Sim.Chegada.cs",
        "Solta(a, Feito(a, Servico.Desembarque, _chegam[a.Id]), false);",
        "Solta(a, _chegam[a.Id], false);",
    ),
    (
        "chegada: mala sai com o servico de bagagem",
        "Sim.Chegada.cs",
        "Solta(a, Feito(a, Servico.Bagagem, _malas[a.Id]), true);",
        "Solta(a, _malas[a.Id], true);",
    ),
    (
        "chegada: nao mexe no Cash",
        "Sim.Chegada.cs",
        "else if (c.Estado == EstadoChegada.Pegando && --c.Resto <= 0) Chegantes.RemoveAt(i--);",
        "else if (c.Estado == EstadoChegada.Pegando && --c.Resto <= 0) { Chegantes.RemoveAt(i--); Cash++; }",
    ),
    (
        "chegada: mala que o save perdeu volta ao carrinho",
        "Sim.Chegada.cs",
        "foreach (Chegante c in Chegantes) if (c.MalaSolta && c.Estado != EstadoChegada.Pegando && malas.Add(c.Id)) Carrinho.Add(c.Id);",
        "",
    ),
    (
        "candidato: cada um custa o preco dele",
        "Sim.Equipe.cs",
        "DoPct(T.Contratacao[(int)p], Pct(T.ExpContratacao, n));",
        "T.Contratacao[(int)p];",
    ),
    (
        "candidato: salario pela experiencia",
        "Sim.Equipe.cs",
        "DoPct(T.Salario[(int)p], Pct(T.ExpSalario, n));",
        "T.Salario[(int)p];",
    ),
    (
        "candidato: a folha e' a da experiencia",
        "Sim.Despesas.cs",
        "n += Salario(f.Perfil, f.Experiencia);",
        "n += T.Salario[(int)f.Perfil];",
    ),
    (
        "candidato: rende pela experiencia",
        "Sim.Estacoes.cs",
        "* Rendimento(f.Experiencia);",
        ";",
    ),
    (
        "candidato: regra desligada iguala os 3",
        "Sim.Equipe.cs",
        "T.RegraExperiencia ? tabela[(int)n] : 100;",
        "tabela[(int)n];",
    ),
    (
        "candidato: vaga fechada nao contrata",
        "Sim.Equipe.cs",
        "CustoContratar(p) < 0 ? -1 : Preco(p, n);",
        "Preco(p, n);",
    ),
    (
        "candidato: experiencia invalida nao contrata",
        "Sim.Equipe.cs",
        "|| !ExpValida(n) ? -1 :",
        "? -1 :",
    ),
    (
        "demitir: nao devolve Cash",
        "Sim.Equipe.cs",
        "Staff.RemoveAt(funcionario);",
        "Staff.RemoveAt(funcionario); Cash += T.Contratacao[(int)f.Perfil];",
    ),
    (
        "demitir: quem vinha depois sobe",
        "Sim.Equipe.cs",
        "for (int i = funcionario; i < Staff.Count; i++) Staff[i].Id = i;",
        "",
    ),
    (
        "demitir: vale com a conta atrasada",
        "Sim.Equipe.cs",
        "if (funcionario < 0 || funcionario >= Staff.Count) return false;",
        "if (funcionario < 0 || funcionario >= Staff.Count || Pausado) return false;",
    ),
    (
        "candidato: a experiencia vai no save",
        "Sim.Save.cs",
        ".Append(',').Append((int)f.Experiencia)",
        "",
    ),
    (
        "candidato: lixo no save vira o Pleno",
        "Sim.Save.cs",
        "f.Length >= 5 && f[4] >= (int)Experiencia.Junior && f[4] <= (int)Experiencia.Senior ?",
        "f.Length >= 5 ?",
    ),
    (
        "candidato: teto do atendimento e' o do mais lento",
        "Sim.Save.cs",
        "Math.Min(TempoTeto(st.Tipo), L(a[1], 1))",
        "Math.Min(TempoBase(st.Tipo), L(a[1], 1))",
    ),
    (
        "vestiario: voltar ao setor do uniforme nao troca",
        "Sim.cs",
        "f.De = veste;",
        "f.De = f.Em;",
    ),
    (
        "vestiario: quem sai termina o atendimento",
        "Sim.Vestiario.cs",
        "if (!T.RegraVestiario || s.Atendendo.Count < Presentes(e)) return 0;",
        "if (true) return 0;",
    ),
    (
        "vestiario: o outro no posto cobre o atendimento",
        "Sim.Vestiario.cs",
        "if (!T.RegraVestiario || s.Atendendo.Count < Presentes(e)) return 0;",
        "if (!T.RegraVestiario || s.Atendendo.Count == 0) return 0;",
    ),
    (
        "vestiario: o uniforme so muda no fim da troca",
        "Sim.Vestiario.cs",
        "return t == Transferencia.Nenhuma || t == Transferencia.IndoPosto ? f.Em : f.De;",
        "return f.Em;",
    ),
    (
        "vestiario: a fase sai do Andando",
        "Sim.Vestiario.cs",
        "return f.Andando <= Volta + TrocaPassos ? Transferencia.Trocando : f.Andando <= TransferePassos ? Transferencia.IndoVestiario : Transferencia.Encerrando;",
        "return Transferencia.Trocando;",
    ),
    (
        "vestiario: o save guarda a transferencia inteira",
        "Sim.Vestiario.cs",
        "int AndandoMax(Estacao em, Estacao de) => de == em ? TransferePassos : TransferePassos + TempoBase(de);",
        "int AndandoMax(Estacao em, Estacao de) => TransferePassos;",
    ),
    (
        "historico: so observa (nao mexe no voo)",
        "Sim.Historico.cs",
        "int perdidos = v.Total - v.Embarcados;",
        "int perdidos = v.Total - v.Embarcados; v.Receita = 0;",
    ),
    (
        "historico: so observa (nao mexe no aviao)",
        "Sim.Historico.cs",
        "o.Causa = Avioes[a].Desgaste; o.Total",
        "o.Causa = Avioes[a].Desgaste; Avioes[a].Pane = false; o.Total",
    ),
    (
        "historico: regra desligada nao anota (eventos)",
        "Sim.Historico.cs",
        "if (!T.RegraHistorico) return;\n            Ocorrencia o;",
        "Ocorrencia o;",
    ),
    (
        "historico: regra desligada nao anota (portao)",
        "Sim.Historico.cs",
        "if (!T.RegraHistorico) return;\n            int perdidos",
        "int perdidos",
    ),
    (
        "historico: retencao de 3 dias do jogo",
        "Sim.Historico.cs",
        "void Poda() => _hist.RemoveAll(r => r.Dia <= DiaJogo - OcorrenciasDias);",
        "void Poda() { }",
    ),
    (
        "historico: teto do dia",
        "Sim.Historico.cs",
        "if (doDia >= OcorrenciasPorDia) _hist.RemoveAt(_hist.FindIndex(r => r.Dia == dia));",
        "",
    ),
    (
        "historico: teto do dia no save",
        "Sim.Historico.cs",
        "if (doDia >= OcorrenciasPorDia) _hist.RemoveAt(k);",
        "",
    ),
    (
        "historico: passo do save limitado",
        "Sim.Historico.cs",
        "|| n[0] > Steps + 2L * OndaPassos) continue;",
        ") continue;",
    ),
    (
        "historico: perdidos do save ate o total",
        "Sim.Historico.cs",
        "o.Perdidos = (int)Lim(n[6], 0, o.Total);",
        "o.Perdidos = (int)n[6];",
    ),
    (
        "historico: o registro vai no save",
        "Sim.Historico.cs",
        "Poda();\n            if (_hist.Count == 0) return;",
        "return;",
    ),
    (
        "historico: o custo do reparo pago vai no registro",
        "Sim.Historico.cs",
        "case Ev.PaneReparo: Responde(DaFalha(a), Opcao.Reparar, b, false); break;",
        "case Ev.PaneReparo: break;",
    ),
    (
        "historico: explica a estacao sem ninguem",
        "Sim.Historico.cs",
        ": Presentes(e) == 0 ? Motivo.SemEquipe",
        ": Presentes(e) < 0 ? Motivo.SemEquipe",
    ),
    (
        "historico: travou onde ficou a maioria",
        "Sim.Historico.cs",
        "if (n > max) { max = n; o.Estacao = s.Tipo; }",
        "if (n > 0) { max = n; o.Estacao = s.Tipo; }",
    ),
    (
        "historico: o atraso da decolagem vai no registro",
        "Sim.Historico.cs",
        "o.Atraso = atraso;\n                if (o.Tipo",
        "o.Atraso = 0;\n                if (o.Tipo",
    ),
    (
        "historico: a pane fecha quando o aviao volta ao normal",
        "Sim.Historico.cs",
        "case Ev.PaneReparada: if ((o = DaFalha(a)) != null) o.Aberta = false; break;",
        "case Ev.PaneReparada: break;",
    ),
    (
        "historico: a falha de aviao guarda o desgaste",
        "Sim.Historico.cs",
        "o.Causa = Avioes[a].Desgaste; o.Total",
        "o.Causa = 0; o.Total",
    ),
    (
        "historico: a rejeicao nao fecha o incidente do mesmo indice",
        "Sim.Historico.cs",
        "o.Tipo == TipoOcorrencia.FalhaAviao && o.Sub == (int)TipoFalha.Rejeicao) o.Aberta = false;",
        "o.Sub == (int)TipoFalha.Rejeicao) o.Aberta = false;",
    ),
    (
        "historico: a falta de caixa entra no fechamento",
        "Sim.Historico.cs",
        "if (c > 0) { o = Nova(TipoOcorrencia.FaltaCaixa",
        "if (c < 0) { o = Nova(TipoOcorrencia.FaltaCaixa",
    ),
    (
        "historico: a falta fecha quando o caixa quita",
        "Sim.Historico.cs",
        "{ o.Aberta = false; o.Auto = true; } break;",
        "{ } break;",
    ),
    # frente F (Sim.Limpeza.cs): limpeza e banheiro
    (
        "limpeza: so vai ao banheiro quem volta antes do embarque",
        "Sim.Limpeza.cs",
        "v != null && v.AbrePasso - loc > 2 * BanheiroAndarPassos + UsoPassos + Passos(T.BanheiroFolga)",
        "v != null",
    ),
    (
        "limpeza: as cabines no teto",
        "Sim.Limpeza.cs",
        "while (b.Usando.Count < b.Cabines && b.Fila.Count > 0)",
        "while (b.Fila.Count > 0)",
    ),
    (
        "limpeza: o banheiro interditado nao recebe",
        "Sim.Limpeza.cs",
        "if (b.Interditado)",
        "if (false)",
    ),
    (
        "limpeza: so limpa depois de chegar",
        "Sim.Limpeza.cs",
        "else if (z.Fase == Faxina.Limpando)",
        "if (z.Fase != Faxina.Livre)",
    ),
    (
        "limpeza: a ordem do jogador antes da escala",
        "Sim.Limpeza.cs",
        "while (Ordens.Count > 0)",
        "while (false)",
    ),
    (
        "limpeza: o salario na folha da equipe",
        "Sim.Despesas.cs",
        "if (T.RegraLimpeza) n += (long)Limpeza.Count * T.SalarioLimpeza;",
        "",
    ),
    (
        "limpeza: contratar pausa com a conta atrasada",
        "Sim.Limpeza.cs",
        "if (!PodeComprar(c)) return false;",
        "if (c < 0 || Cash < c) return false;",
    ),
    (
        "limpeza: o save guarda quem esta na cabine",
        "Sim.Limpeza.cs",
        'Lista("bh.uso", Banheiro.Usando, true);',
        "",
    ),
    (
        "limpeza: o voo que fecha leva quem ficou no banheiro",
        "Sim.cs",
        "TiraDoBanheiro(v);",
        "",
    ),
    (
        "suprimento: o embarque nao espera a carga",
        "Sim.Patio.cs",
        "a.Falta[(int)Servico.Limpeza] == 0)",
        "a.Falta[(int)Servico.Limpeza] == 0 && a.Carga[1] == 0)",
    ),
    (
        "suprimento: a decolagem espera a carga",
        "Defs.cs",
        "return Carga[0] == 0 && Carga[1] == 0; } }",
        "return true; } }",
    ),
    (
        "suprimento: sem carga no deposito, nao tira",
        "Sim.Suprimentos.cs",
        "if (Estoque[i] < g)",
        "if (Estoque[i] < 0)",
    ),
    (
        "suprimento: pede no ponto",
        "Sim.Suprimentos.cs",
        "Estoque[i] <= T.SupPonto[i]",
        "Estoque[i] < T.SupPonto[i]",
    ),
    (
        "suprimento: a entrega chega no prazo",
        "Sim.Suprimentos.cs",
        "if (Pedido[i] > 0 && Steps >= Entrega[i])",
        "if (Pedido[i] > 0 && Steps > Entrega[i])",
    ),
    (
        "suprimento: a entrega enche o tanque de agora",
        "Sim.Suprimentos.cs",
        "int n = Tanque((Suprimento)i) - Estoque[i];   // enche",
        "int n = T.SupTanque[i] - Estoque[i];   // enche",
    ),
    (
        "suprimento: o pedido e' o que falta no tanque",
        "Sim.Suprimentos.cs",
        "int i = (int)s, n = Tanque(s) - Estoque[i];",
        "int i = (int)s, n = T.SupTanque[i] - Estoque[i];",
    ),
    (
        "suprimento: a entrega vai para a conta",
        "Sim.Suprimentos.cs",
        "Gasta(T.SupCusto[i], Despesa.Suprimentos);",
        "Gasta(0, Despesa.Suprimentos);",
    ),
    (
        "suprimento: a conta atrasada nao segura a entrega",
        "Sim.Suprimentos.cs",
        "if (n <= 0) return;",
        "if (n <= 0 || Pausado) return;",
    ),
    (
        "suprimento: a conta de hoje mostra a entrega",
        "Sim.Despesas.cs",
        "case Despesa.Suprimentos: n = Hoje.Despesas[(int)d]; break;",
        "case Despesa.Suprimentos: break;",
    ),
    (
        "suprimento: o aviso conta quem pousa antes da entrega",
        "Sim.Suprimentos.cs",
        "(Pedido[i] == 0 || a.Chega < Entrega[i])",
        "Pedido[i] == 0",
    ),
    (
        "suprimento: do hangar volta abastecido",
        "Sim.Patio.cs",
        "a.Carga[i] = doHangar || !T.RegraSuprimentos ? 0 : CargaPede;",
        "a.Carga[i] = !T.RegraSuprimentos ? 0 : CargaPede;",
    ),
    (
        "suprimento: o hangar leva a carga devida",
        "Sim.Patio.cs",
        "Array.Clear(a.Carga, 0, a.Carga.Length);",
        ";",
    ),
    (
        "suprimento: regra desligada nao anda",
        "Sim.Suprimentos.cs",
        "if (!T.RegraSuprimentos) return;",
        ";",
    ),
    (
        "suprimento: o pedido lido cabe no tanque",
        "Sim.Suprimentos.cs",
        "Math.Min(tanque - Estoque[i], x[1])",
        "x[1]",
    ),
    (
        "suprimento: a carga vai no save",
        "Sim.Suprimentos.cs",
        "if (a.Carga[0] != 0 || a.Carga[1] != 0) sb.Append",
        "if (false) sb.Append",
    ),
    (
        "suprimento: o registro diz suprimento",
        "Sim.Historico.cs",
        "? Motivo.Suprimento : Motivo.Patio)",
        "? Motivo.Patio : Motivo.Patio)",
    ),
    (
        "satisfacao: sem a regra nao paga gorjeta",
        "Sim.Satisfacao.cs",
        "if (!embarcou || !T.RegraSatBonus || nota < T.SatGorjeta) return;",
        "if (!embarcou || nota < T.SatGorjeta) return;",
    ),
    (
        "satisfacao: gorjeta so de quem esta contente",
        "Sim.Satisfacao.cs",
        "if (!embarcou || !T.RegraSatBonus || nota < T.SatGorjeta) return;",
        "if (!embarcou || !T.RegraSatBonus) return;",
    ),
    (
        "satisfacao: no portao, espera so com o embarque aberto",
        "Sim.Satisfacao.cs",
        "if (!portao || PodeEmbarcar(v)) { p.Espera += SatPasso;",
        "if (true) { p.Espera += SatPasso;",
    ),
    (
        "satisfacao: as cadeiras no teto",
        "Sim.Satisfacao.cs",
        "if (sentados < T.CadeirasPortao)",
        "if (true)",
    ),
    (
        "satisfacao: atraso so depois da hora de abrir",
        "Sim.Satisfacao.cs",
        "if (v != null && loc >= v.AbrePasso)",
        "if (v != null)",
    ),
    (
        "satisfacao: reclama 1x",
        "Sim.Satisfacao.cs",
        "if (p.Reclamou || Nota(p) >= T.SatReclama) return;",
        "if (Nota(p) >= T.SatReclama) return;",
    ),
    (
        "satisfacao: as reclamacoes sao do dia",
        "Sim.Satisfacao.cs",
        "if (DiaJogo != _satDia) { Array.Clear(_reclama, 0, Fatores); _satDia = DiaJogo; }",
        "",
    ),
    (
        "satisfacao: quem perdeu o voo entra na media",
        "Sim.Satisfacao.cs",
        "Saiu(p, false, 0);",
        "",
    ),
    (
        "satisfacao: perdeu na fila, a espera vai ao teto",
        "Sim.Satisfacao.cs",
        "p.Espera = Math.Max(p.Espera, 10 * OndaPassos);",
        "",
    ),
    (
        "satisfacao: o perfil sai da rota",
        "Sim.Satisfacao.cs",
        "(PerfilPax)(int)Efeito(v).Tipo;",
        "PerfilPax.Executivo;",
    ),
    (
        "satisfacao: a nota de 0 a 100",
        "Sim.Satisfacao.cs",
        "return Math.Max(0, Math.Min(100, n));",
        "return n;",
    ),
    (
        "satisfacao: o save guarda quem esta no aeroporto",
        "Sim.Satisfacao.cs",
        'if (ps.Count > 0) sb.Append("satp=")',
        'if (false) sb.Append("satp=")',
    ),
    (
        "satisfacao: o save limita o lixo",
        "Sim.Satisfacao.cs",
        "p.Sujo = Math.Min(SujeiraMax, x[2]);",
        "p.Sujo = x[2];",
    ),
    (
        "satisfacao: achou o banheiro fechado",
        "Sim.Limpeza.cs",
        "{ p.Negado = true; VoltaAoPortao(p); }",
        "{ VoltaAoPortao(p); }",
    ),
    (
        "historias: a parada pedida precisa ter sido cumprida",
        "Sim.Historias.cs",
        "if (x == null || x.Onde != null && HistoriaPasso != 1) return;",
        "if (x == null) return;",
    ),
    (
        "historias: o perfil pedido tem de estar na estacao",
        "Sim.Historias.cs",
        "if (x.Quem is Perfil q && !Staff.Exists(f => f.Perfil == q && f.Em == e && f.Andando == 0)) return;",
        "",
    ),
    (
        "historias: todos a bordo so sem perdidos",
        "Sim.Historias.cs",
        "if (v.Perdidos == 0) EntregaHistoria(HistoriaOnda, HistoriaPax);",
        "EntregaHistoria(HistoriaOnda, HistoriaPax);",
    ),
    (
        "historias: todos a bordo espera o portao",
        "Sim.Historias.cs",
        "if (x.Todos) HistoriaPasso = 2; else EntregaHistoria(HistoriaOnda, p.Id);",
        "EntregaHistoria(HistoriaOnda, p.Id);",
    ),
    (
        "historias: a parada vai para quem tem o motivo",
        "Sim.Historias.cs",
        "x.Onde == Estacao.Loja ? p.Loja :",
        "x.Onde == Estacao.Loja ? true :",
    ),
    (
        "historias: ineditas primeiro",
        "Sim.Historias.cs",
        ": nova != 0 ? nova :",
        ":",
    ),
    (
        "historias: a Dona Rosa abre o album",
        "Sim.Historias.cs",
        "(nova & 1) != 0 ? 1 :",
        "",
    ),
    (
        "historias: so do aeroporto atual",
        "Sim.Historias.cs",
        "x.Aeroporto != AeroportoAtual ||",
        "",
    ),
    (
        "historias: so do marco alcancado",
        "Sim.Historias.cs",
        "|| x.Marco > Marco ||",
        "||",
    ),
    (
        "historias: o save volta a historia da onda",
        "Sim.Historias.cs",
        "HistoriaOnda = hp[0]; HistoriaPax = hp[1];",
        "SorteiaHistoria(Onda); return;",
    ),
    (
        "historias: as que sairam ficam na tabela",
        "Sim.Save.cs",
        "(hp.Length >= 4 ? hp[3] & s.TodasHistorias : 0)",
        "(hp.Length >= 4 ? hp[3] : 0)",
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
        ]
        + (
            ["--filter", os.environ["MUTACAO_TESTES"]]
            if "MUTACAO_TESTES" in os.environ
            else []
        ),
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
    filtro = sys.argv[1:]
    lista = [m for m in MUTANTES if not filtro or any(f in m[0] for f in filtro)]
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
