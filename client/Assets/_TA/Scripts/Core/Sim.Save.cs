using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TA.Core
{
    public sealed partial class Sim
    {
        static readonly CultureInfo Ci = CultureInfo.InvariantCulture;

        /// <summary>
        /// Save chave=valor dentro do SaveEnvelope (CONTRATO_V01 §5): Cash, relogio do quadro (t e onda), voos da onda, filas e quem esta em
        /// atendimento, equipe (onde esta e quanto falta andar), niveis, marcos (pelos embarcados), album, cofre, anuncios e o dia local.
        /// SavedAt = o maior instante ja visto: relogio que voltou nao regrava o SavedAt para tras (adiantar -> resgatar -> voltar -> adiantar
        /// nao gera claim novo). Devolve null com SaveRefused (nunca grava por cima de um save que nao abriu).
        /// </summary>
        public string Save(string content, long nowUnix)
        {
            if (SaveRefused) return null;
            long back = SavedAt - nowUnix;
            ClockWentBack = back > 0 && !_clockBehind ? back : 0;
            _clockBehind = back > 0;
            SavedAt = Math.Max(SavedAt, nowUnix);
            var sb = new StringBuilder();
            Kv(sb, "t", Steps); Kv(sb, "cash", Cash); Kv(sb, "seed", Seed); Kv(sb, "onda", Onda);
            Kv(sb, "emb", TotalEmbarcados); Kv(sb, "perd", TotalPerdidos); Kv(sb, "album", Album); Kv(sb, "bst", BoostLeft);
            sb.Append("ema=").Append(RateEma.ToString("R", Ci)).Append('\n');
            Kv(sb, "saved", SavedAt); Kv(sb, "claim", LastClaim); Kv(sb, "claimcash", LastClaimCash); Kv(sb, "dia", Dia);
            if (FusoGravado) Kv(sb, "fuso", Fuso);
            foreach (Station s in Estacoes)
            {
                int i = (int)s.Tipo;
                sb.Append("est.").Append(i).Append('=').Append(s.Vel).Append(',').Append(s.Cap).Append('\n');
                if (s.Fila.Count > 0) sb.Append("fila.").Append(i).Append('=').Append(string.Join(";", s.Fila.ConvertAll(p => p.Id.ToString(Ci)))).Append('\n');
                if (s.Atendendo.Count > 0)
                    sb.Append("at.").Append(i).Append('=').Append(string.Join(";", s.Atendendo.ConvertAll(p => p.Id.ToString(Ci) + ":" + p.Resto.ToString(Ci)))).Append('\n');
            }
            foreach (Funcionario f in Staff)
                sb.Append("func.").Append(f.Id).Append('=').Append((int)f.Em).Append(',').Append((int)f.De).Append(',').Append(f.Andando).Append(',').Append((int)f.Perfil).Append('\n');
            foreach (Voo v in Voos)
                if (v != null)
                    sb.Append("voo.").Append(v.Indice).Append('=').Append(v.Criados).Append(',').Append(v.Embarcados).Append(',').Append(v.Receita)
                      .Append(',').Append(v.Embarque ? 1 : 0).Append(',').Append(v.Partiu ? 1 : 0).Append('\n');
            Anuncios.Save(sb);
            SalvaFaseC(sb);
            SalvaFaseC3(sb);
            SalvaFaseC4(sb);
            SalvaCampanha(sb);
            return SaveEnvelope.Wrap(sb.ToString(), content, nowUnix);
        }

        /// <summary>Schema 2 (CONTRATO_FASE_C §6): avioes (desgaste, onde esta, voo, checagem, hangar), servicos do turnaround, tripulacao
        /// (trechos, tempo na sala, a bordo de qual aviao), equipe e veiculos de patio.</summary>
        void SalvaFaseC(StringBuilder sb)
        {
            foreach (Aviao a in Avioes)
            {
                sb.Append("aviao.").Append(a.Id).Append('=').Append(a.Desgaste).Append(',').Append(a.Chega.ToString(Ci)).Append(',').Append(a.NoPatio ? 1 : 0)
                  .Append(',').Append(a.VooId).Append(',').Append(a.FechouEm.ToString(Ci)).Append(',').Append(a.Check).Append(',').Append(a.Inspecao ? 1 : 0)
                  .Append(',').Append(a.Checou ? 1 : 0).Append(',').Append(a.Hangar).Append(',').Append(a.AvisouTrip ? 1 : 0).Append('\n');
                sb.Append("srv.").Append(a.Id).Append('=');
                for (int s = 0; s < 4; s++) sb.Append(a.Falta[s]).Append(a.Fazendo[s] ? ":1" : ":0").Append(s < 3 ? "," : "\n");
            }
            foreach (Tripulante t in Tripulacao)
                sb.Append("trip.").Append(t.Id).Append('=').Append((int)t.Funcao).Append(',').Append(t.Trechos).Append(',').Append(t.Sala).Append(',').Append(t.Aviao).Append('\n');
            Kv(sb, "patio", EquipePatio); Kv(sb, "patioatraso", AtrasosPatio);
            for (int s = 0; s < 4; s++) if (PatioVel[s] != 0 || PatioCap[s] != 0) sb.Append("pv.").Append(s).Append('=').Append(PatioVel[s]).Append(',').Append(PatioCap[s]).Append('\n');
        }

        /// <summary>Schema 4 (CONTRATO_FASE_C3 §6): aeroportos, rotas abertas e programadas, a rota e o grupo de cada voo da onda, a volta de
        /// cada aviao e o incidente em andamento.</summary>
        void SalvaFaseC3(StringBuilder sb)
        {
            sb.Append("aero=").Append(AeroportoAtual).Append(',').Append(AeroportosPagos).Append(',').Append(ProximoAeroporto).Append('\n');
            Kv(sb, "rotas", RotasAbertas);
            sb.Append("prog=").Append(Prog[0]).Append(',').Append(Prog[1]).Append('\n');
            foreach (Voo v in Voos)
                if (v != null)
                    sb.Append("vr.").Append(v.Indice).Append('=').Append(v.Rota).Append(',').Append(v.Total).Append(',').Append(v.Grupo).Append(',').Append(v.GrupoChega)
                      .Append(',').Append(v.GrupoConexao ? 1 : 0).Append(',').Append(v.Prioridade ? 1 : 0).Append(',').Append(v.AbrePasso).Append(',').Append(v.Adiantado ? 1 : 0).Append('\n');
            // o padrao fica de fora: rota igual a da vaga da v0.1 e sem onda inteira fora
            foreach (Aviao a in Avioes)
                if (a.Rota != a.Id || a.VoltaOnda > Onda + 1) sb.Append("av.").Append(a.Id).Append('=').Append(a.Rota).Append(',').Append(a.VoltaOnda).Append('\n');
            // o estado que a tela e o bot leem: sem isto, reabrir com fila cheia daria uma decisao diferente
            foreach (Station st in Estacoes)
                if (st.Estado != Estado.Normal || !double.IsInfinity(st.Folga))
                    sb.Append("stt.").Append((int)st.Tipo).Append('=').Append((int)st.Estado).Append(',').Append(st.VooEmRisco).Append(',').Append(st.Folga.ToString("R", Ci)).Append('\n');
            if (_inc != null)
                sb.Append("inc=").Append((int)_inc.Tipo).Append(',').Append(_inc.Vaga).Append(',').Append((int)_inc.Estacao).Append(',').Append(_inc.Aviso.ToString(Ci))
                  .Append(',').Append(_inc.Inicio.ToString(Ci)).Append(',').Append(_inc.Fim.ToString(Ci)).Append(',').Append(_inc.Resolvido ? 1 : 0).Append('\n');
            Kv(sb, "reparo", Reparo); Kv(sb, "incw", UltimoIncidente);
        }

        /// <summary>Schema 5 (CONTRATO_FASE_C4 §7): o que esta em andamento. Clima e falha saem da semente: so grava o que ja aconteceu. O portao
        /// atrasado vai por voo; o padrao fica de fora.</summary>
        void SalvaFaseC4(StringBuilder sb)
        {
            foreach (Voo v in Voos)
                if (v != null && v.Atrasado) sb.Append("va.").Append(v.Indice).Append('=').Append(v.FechaPasso).Append('\n');
            foreach (Aviao a in Avioes)
                if (a.Pane || a.Falhou >= 0 || a.SaiEm > 0 || a.Retorno > 0)
                    sb.Append("falha.").Append(a.Id).Append('=').Append(a.Pane ? 1 : 0).Append(',').Append(a.Falhou).Append(',').Append(a.SaiEm.ToString(Ci))
                      .Append(',').Append(a.Retorno.ToString(Ci)).Append('\n');
            if (PistaAte > Steps) Kv(sb, "pista", PistaAte);
        }

        /// <summary>Schema 6 (CONTRATO_FASE_C5 §5): por aeroporto, os objetivos feitos e o contador de cada um.</summary>
        void SalvaCampanha(StringBuilder sb)
        {
            for (int a = 0; a < _feitos.Length; a++)
                sb.Append("camp.").Append(a).Append('=').Append(_feitos[a]).Append(',').Append(string.Join(",", _conta[a])).Append('\n');
        }

        /// <summary>Le o schema 6, com tudo limitado. O schema 5 nao tem a chave: nada feito, e o que o estado ja prova conclui nos primeiros passos.</summary>
        void CarregaCampanha(Dictionary<string, string> kv)
        {
            for (int a = 0; a < _feitos.Length; a++)
            {
                int[] c = Ints(kv, "camp." + a);
                if (c.Length == 0) continue;
                _feitos[a] = Math.Max(0, Math.Min(T.Objetivos[a].Length, c[0]));
                for (int i = 0; i < _conta[a].Length && i + 1 < c.Length; i++) _conta[a][i] = Math.Max(0, Math.Min(T.Objetivos[a][i].Alvo, c[i + 1]));
            }
        }

        /// <summary>Le o schema 5 depois dos avioes, com tudo limitado. O schema 4 nao tem estas chaves: nada em andamento.</summary>
        void CarregaFaseC4(Dictionary<string, string> kv)
        {
            foreach (Voo v in Voos)
                if (v != null && Ints(kv, "va." + v.Indice) is int[] x && x.Length >= 1) { v.FechaPasso = Math.Max(v.FechaPasso, Math.Min(OndaPassos - 1, x[0])); v.Atrasado = true; }
            foreach (Aviao a in Avioes)
            {
                long[] n = Longs(kv, "falha." + a.Id);
                if (n.Length < 4) continue;
                a.Pane = n[0] != 0 && a.NoPatio;
                a.Falhou = (int)Math.Max(-1, Math.Min(int.MaxValue, n[1]));
                a.SaiEm = a.FechouEm >= 0 ? Math.Max(0, Math.Min(Steps + Passos(T.RejeicaoSeg), n[2])) : 0;
                a.Retorno = !a.NoPatio && a.Hangar == 0 ? Math.Max(0, Math.Min(Steps + Passos(T.RetornoPede), n[3])) : 0;
            }
            PistaAte = Math.Max(0, Math.Min(Steps + Passos(T.PistaBloqueioSeg), Get(kv, "pista", 0)));
        }

        /// <summary>Le o schema 4 antes da onda (a falha pesa no teto do atendimento), com tudo limitado. O schema 3 nao tem estas chaves: fica
        /// o jogo novo, Vale Verde com as rotas iniciais e nenhum incidente.</summary>
        void CarregaFaseC3(Dictionary<string, string> kv)
        {
            int[] a = Ints(kv, "aero");
            int n = T.Aeroportos.Length;
            if (a.Length >= 3)
            {
                for (int i = 0; i < n; i++) if ((a[1] >> i & 1) != 0 && Marco >= T.Aeroportos[i].Marco) AeroportosPagos |= 1 << i;
                if (a[0] >= 0 && a[0] < n && Pago(a[0])) Chega(a[0]);
                ProximoAeroporto = a[2] >= 0 && a[2] < n && a[2] != AeroportoAtual && Pago(a[2]) ? a[2] : -1;
            }
            long abertas = Get(kv, "rotas", 0);
            for (int r = 0; r < T.Rotas.Length; r++)
                if ((abertas >> r & 1) != 0 && Pago(T.Rotas[r].Aeroporto) && Marco >= T.Rotas[r].Marco) RotasAbertas |= 1 << r;
            int[] prog = Ints(kv, "prog");
            for (int f = 0; f < Math.Min(prog.Length, Prog.Length); f++)
                if (RotaAberta(prog[f]) && T.Rotas[prog[f]].Aeroporto == AeroportoAtual) Prog[f] = prog[f];
            foreach (Aviao av in Avioes)
            {
                int[] x = Ints(kv, "av." + av.Id);
                if (x.Length < 2) continue;
                av.Rota = x[0] >= 0 && x[0] < T.Rotas.Length ? x[0] : 0;
                av.VoltaOnda = Math.Max(0, Math.Min(Onda + 2, x[1]));
            }
            foreach (Station st in Estacoes)
            {
                string[] e = kv.TryGetValue("stt." + (int)st.Tipo, out string x) ? x.Split(',') : new string[0];
                if (e.Length < 3 || !double.TryParse(e[2], NumberStyles.Float, Ci, out double folga) || double.IsNaN(folga)) continue;
                st.Estado = (Estado)Math.Max(0, Math.Min(2, L(e[0], 0)));
                st.VooEmRisco = (int)Math.Max(-1, Math.Min(int.MaxValue, L(e[1], -1)));
                st.Folga = folga;
            }
            long[] c = Longs(kv, "inc");
            // incidente coerente com o relogio; lixo = nenhum
            if (c.Length >= 7 && c[0] >= 0 && c[0] <= (long)TipoIncidente.Neve && c[3] <= c[4] && c[4] <= c[5] && c[5] >= Steps && c[5] <= Steps + 2L * OndaPassos)
                _inc = new Incidente { Tipo = (TipoIncidente)c[0], Vaga = c[1] == 1 ? 1 : 0, Estacao = Est((int)c[2]), Aviso = c[3], Inicio = c[4], Fim = c[5], Resolvido = c[6] != 0 };
            Reparo = _inc != null ? (int)Math.Max(0, Math.Min(Passos(T.ReparoSeg), Get(kv, "reparo", 0))) : 0;
            UltimoIncidente = (int)Math.Max(-99, Math.Min(Onda, Get(kv, "incw", -99)));
        }

        /// <summary>Le o schema 2, com tudo limitado (lixo vira o padrao). Schema 1 (a v0.1) nao tem estas chaves: Migra1 poe o padrao.</summary>
        void CarregaFaseC(Dictionary<string, string> kv, int schema)
        {
            if (schema < 2) { Migra1(); return; }
            EquipePatio = (int)Math.Max(T.PatioIniciais, Math.Min(T.PatioIniciais + T.PatioContratacao.Length, Get(kv, "patio", T.PatioIniciais)));
            AtrasosPatio = (int)Math.Max(0, Math.Min(int.MaxValue, Get(kv, "patioatraso", 0)));
            for (int s = 0; s < 4; s++)
            {
                int[] n = Ints(kv, "pv." + s);
                if (n.Length >= 2) { PatioVel[s] = Math.Max(0, Math.Min(T.PatioVelNiveis, n[0])); PatioCap[s] = Math.Max(0, Math.Min(T.PatioCapNiveis, n[1])); }
            }
            int maxCheck = Math.Max(Passos(T.CheckSeg), Passos(T.InspecaoSeg)), fazendo = 0;
            foreach (Aviao a in Avioes)
            {
                long[] n = Longs(kv, "aviao." + a.Id);
                if (n.Length < 10) { Migra1(a); continue; }
                a.Desgaste = (int)Math.Max(0, Math.Min(100, n[0]));
                a.Hangar = (int)Math.Max(0, Math.Min(Passos(T.HangarSeg), n[8]));
                a.NoPatio = n[2] != 0 && a.Hangar == 0;
                a.Chega = Math.Max(-1, Math.Min(Steps + Passos(TrechoSeg(a.Rota)), n[1]));   // volta de trecho no maximo um trecho a frente
                a.VooId = (int)Math.Max(-1, Math.Min(int.MaxValue, n[3]));
                a.FechouEm = a.NoPatio && a.VooId >= 0 ? Math.Max(-1, Math.Min(Steps, n[4])) : -1;
                // voo valido: o desta onda (fechado so se ja partiu) ou, esperando decolar, o da onda anterior
                Voo v = Voos[a.Id];
                bool atual = v != null && v.Id == a.VooId, anterior = v != null && v.Id - 2 == a.VooId;
                if (a.FechouEm >= 0 && !(atual && v.Partiu || anterior)) a.FechouEm = -1;
                if (!a.NoPatio || a.FechouEm < 0 && !atual) a.VooId = -1;
                a.Check = a.NoPatio ? (int)Math.Max(0, Math.Min(maxCheck, n[5])) : 0;
                a.Inspecao = n[6] != 0 && a.Check > 0; a.Checou = n[7] != 0; a.AvisouTrip = n[9] != 0;
                string[] sv = kv.TryGetValue("srv." + a.Id, out string x) ? x.Split(',') : new string[0];
                for (int s = 0; s < 4; s++)
                {
                    string[] p = s < sv.Length ? sv[s].Split(':') : new string[0];
                    a.Falta[s] = a.NoPatio && p.Length == 2 ? (int)Math.Max(0, Math.Min(ServicoPassos((Servico)s), L(p[0], 0))) : 0;
                    a.Fazendo[s] = a.Falta[s] > 0 && L(p[1], 0) != 0 && fazendo++ < EquipePatio;
                }
                if (a.NoPatio && a.VooId < 0 && a.FechouEm < 0 && Voos[a.Id] != null && !Voos[a.Id].Partiu) Liga(a);
                if (!a.NoPatio && a.Hangar == 0 && a.Chega < 0 && Onda >= 0) a.Chega = ChegadaNaOnda(Onda, a.Id);
            }
            var lista = new List<Tripulante>();
            for (int i = 0; ; i++)
            {
                int[] t = Ints(kv, "trip." + i);
                if (t.Length < 4) break;
                var f = (Funcao)Math.Max(0, Math.Min(T.TripPorVoo.Length - 1, t[0]));
                int quantos = 0;
                foreach (Tripulante o in lista) if (o.Funcao == f) quantos++;
                if (quantos >= T.TripIniciais + T.TripCusto[(int)f].Length) continue;   // acima do teto: sai
                int av = t[3] >= 0 && t[3] < Avioes.Length && (Avioes[t[3]].NoPatio || Avioes[t[3]].Hangar == 0) ? t[3] : -1;
                lista.Add(new Tripulante { Id = lista.Count, Indice = quantos, Funcao = f, Trechos = Math.Max(0, Math.Min(T.TrechosAteDescanso, t[1])),
                    Sala = Math.Max(0, Math.Min(DescansoPassos, t[2])), Aviao = av });
            }
            if (lista.Count == 0) return;   // sem tripulacao no save: fica a inicial
            Tripulacao.Clear();
            foreach (Tripulante t in lista)
            {
                if (t.Aviao >= 0 && ABordo(t.Aviao, t.Funcao) >= T.TripPorVoo[(int)t.Funcao]) t.Aviao = -1;   // vaga ja cheia
                Tripulacao.Add(t);
            }
        }

        /// <summary>Migracao do schema 1 (save da v0.1): os dois avioes no patio, prontos (servicos feitos), desgaste 0, cada um no voo dele
        /// da onda gravada e com a tripulacao inicial a bordo; 1 pessoa de patio. O voo que estava na fila embarca como antes.</summary>
        void Migra1()
        {
            foreach (Aviao a in Avioes) Migra1(a);
        }

        void Migra1(Aviao a)
        {
            a.NoPatio = true; a.Chega = Steps; a.Hangar = 0; a.Check = 0; a.Desgaste = 0;
            for (int s = 0; s < 4; s++) { a.Falta[s] = 0; a.Fazendo[s] = false; }
            Liga(a);
            if (a.VooId < 0) return;
            for (int f = 0; f < T.TripPorVoo.Length; f++)
                for (int n = ABordo(a.Id, (Funcao)f); n < T.TripPorVoo[f]; n++)
                {
                    Tripulante t = Tripulacao.Find(x => x.Funcao == (Funcao)f && Livre(x));
                    if (t != null) t.Aviao = a.Id;
                }
        }

        static void Kv(StringBuilder sb, string k, long v) => sb.Append(k).Append('=').Append(v.ToString(Ci)).Append('\n');

        public static Sim Load(string text) => Load(text, null, 1);

        /// <summary>Missing = jogo novo; Ok = carrega (chave desconhecida e' ignorada; lixo vira o padrao; tudo limitado: Cash e niveis nunca
        /// negativos, equipe no teto, passageiro que nao e' da onda gravada sai); Corrupt, Legacy (sem cabecalho: nunca houve save assim neste
        /// jogo) e NewerSchema = jogo novo com SaveRefused. `seed` so vale para o jogo novo (o save grava a dele).</summary>
        public static Sim Load(string text, Tuning t, int seed)
        {
            SaveEnvelope.Result r = SaveEnvelope.Unwrap(text);
            if (r.Status == SaveEnvelope.Status.Missing) return new Sim(t, seed);
            if (r.Status != SaveEnvelope.Status.Ok) return new Sim(t, seed) { SaveRefused = true };
            var kv = new Dictionary<string, string>();
            foreach (string line in r.Payload.Split('\n'))
            {
                int eq = line.IndexOf('=');
                if (eq > 0) kv[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
            var s = new Sim(t, (int)Get(kv, "seed", seed));
            s.Staff.Clear();
            s.Steps = Math.Max(0, Get(kv, "t", 0));
            s.Cash = Math.Max(0, Get(kv, "cash", 0));   // GDD §6.2: saldo nunca negativo, nem de save editado
            s.TotalEmbarcados = Math.Max(0, Get(kv, "emb", 0));
            s.TotalPerdidos = Math.Max(0, Get(kv, "perd", 0));
            s.Album = (int)Math.Max(0, Math.Min(int.MaxValue, Get(kv, "album", 0)));
            s.BoostLeft = (int)Math.Max(0, Math.Min(s.T.BoostSegundos * Hz, Get(kv, "bst", 0)));
            s.RateEma = kv.TryGetValue("ema", out string ema) && double.TryParse(ema, NumberStyles.Float, Ci, out double e) && e > 0 && !double.IsInfinity(e) ? e : 0;
            s.SavedAt = Get(kv, "saved", 0); s.LastClaim = Get(kv, "claim", 0); s.LastClaimCash = Math.Max(0, Get(kv, "claimcash", 0));
            s.Dia = Get(kv, "dia", 0);
            if (kv.TryGetValue("fuso", out string fz)) { s.Fuso = L(fz, 0); s.FusoGravado = true; }
            foreach (Station st in s.Estacoes)
            {
                int[] n = Ints(kv, "est." + (int)st.Tipo);
                if (n.Length >= 2 && s.Desbloqueada(st.Tipo)) { st.Vel = Math.Max(0, Math.Min(s.T.VelNiveis, n[0])); st.Cap = Math.Max(0, Math.Min(s.T.CapNiveis, n[1])); st.Postos += st.Cap; }
            }
            for (int i = 0; i < MaxEquipe; i++)
            {
                int[] f = Ints(kv, "func." + i);
                if (f.Length < 3 || s.Staff.Count != i) break;   // contratados em ordem: um buraco encerra a lista
                // schema 2 nao tem o perfil: os 3 generalistas viram Atendente, Agente e Vendedor pela ordem (CONTRATO_FASE_C2 §4)
                var p = (Perfil)Math.Max(0, Math.Min(MaxEquipe - 1, f.Length >= 4 ? f[3] : i));
                if (s.Contratado(p) || f.Length < 4 && i >= MaxFuncionarios) break;   // perfil repetido ou sem perfil acima da v0.1: lixo
                Estacao em = s.Est(f[0]), de = s.Est(f[1]);
                s.Staff.Add(new Funcionario { Id = i, Perfil = p, Em = em, De = de, Andando = Math.Max(0, Math.Min((int)Math.Round(s.T.Caminhada * Hz), f[2])) });
            }
            int onda = (int)Get(kv, "onda", -1);
            bool bate = onda >= 0 && onda == (int)(s.Steps / s.OndaPassos);   // onda que nao bate com o relogio: comeca a dele
            if (bate) s.Onda = onda;
            s.CarregaFaseC3(kv);
            if (bate) s.CarregaOnda(onda, kv);
            foreach (KeyValuePair<string, string> p in kv)
                if (p.Key.StartsWith("ad.", StringComparison.Ordinal)) s.Anuncios.Load(p.Key.Substring(3), p.Value);
            s.CarregaFaseC(kv, r.Schema);
            s.CarregaFaseC4(kv);
            s.CarregaCampanha(kv);
            return s;
        }

        void CarregaOnda(int onda, Dictionary<string, string> kv)
        {
            Onda = onda;
            for (int f = 0; f < 2; f++)
            {
                Voo v = Voos[f] = NovoVoo(onda, f, Prog[f]);
                int[] x = Ints(kv, "vr." + f);
                if (x.Length >= 8)
                {
                    v.Rota = x[0] >= 0 && x[0] < T.Rotas.Length ? x[0] : Prog[f];
                    v.Grupo = Math.Max(0, Math.Min(T.Pax(onda), x[2]));
                    v.Total = Math.Max(v.Grupo, Math.Min(499, x[1]));
                    v.GrupoChega = Math.Max(0, Math.Min(OndaPassos, x[3]));
                    v.GrupoConexao = x[4] != 0; v.Prioridade = x[5] != 0;
                    v.AbrePasso = Math.Max(0, Math.Min(v.AbrePasso, x[6]));
                    v.Adiantado = x[7] != 0;
                }
                int[] n = Ints(kv, "voo." + f);
                if (n.Length < 5) continue;
                v.Criados = Math.Max(0, Math.Min(v.Total, n[0]));
                v.Embarcados = Math.Max(0, Math.Min(v.Criados, n[1]));
                v.Receita = Math.Max(0, n[2]);
                v.Embarque = n[3] != 0; v.Partiu = n[4] != 0;
                if (v.Partiu) v.Criados = v.Total;
            }
            var vistos = new HashSet<int>();
            foreach (Station st in Estacoes)
            {
                int i = (int)st.Tipo;
                if (!Desbloqueada(st.Tipo) || st.Tipo == Estacao.Manutencao || st.Tipo == Estacao.Despacho) continue;   // fechada ou sem passageiro
                if (kv.TryGetValue("at." + i, out string at))
                    foreach (string tok in at.Split(';'))
                    {
                        string[] a = tok.Split(':');
                        Pax p = a.Length == 2 ? Valido(L(a[0], -1), vistos) : null;
                        if (p == null) continue;
                        p.Resto = (int)Math.Max(1, Math.Min(TempoBase(st.Tipo), L(a[1], 1)));
                        p.Total = Math.Max(p.Resto, TempoPassos(st.Tipo));
                        if (st.Atendendo.Count < st.Postos) st.Atendendo.Add(p); else st.Fila.Add(p);
                    }
                if (kv.TryGetValue("fila." + i, out string fila))
                    foreach (string tok in fila.Split(';'))
                    {
                        Pax p = Valido(L(tok, -1), vistos);
                        if (p != null) st.Fila.Add(p);
                    }
            }
        }

        /// <summary>Passageiro do save: da onda gravada, de voo que nao partiu, ja chegado e ainda nao visto. Senao null.</summary>
        Pax Valido(long id, HashSet<int> vistos)
        {
            if (id < 0 || id > int.MaxValue) return null;
            Pax p = NovoPax((int)id);
            Voo v = p.Onda == Onda ? Voos[p.Voo] : null;
            if (v == null || v.Partiu || p.Indice >= v.Criados || !vistos.Add(p.Id)) return null;
            return p;
        }

        /// <summary>Estacao do save, limitada: fora da faixa vai para a ponta; ainda fechada pelo marco cai nas 4 da v0.1.</summary>
        Estacao Est(int i)
        {
            var e = (Estacao)Math.Max(0, Math.Min(Estacoes.Length - 1, i));
            return Desbloqueada(e) ? e : (Estacao)Math.Min(3, (int)e);
        }

        static long Get(Dictionary<string, string> kv, string k, long def) => kv.TryGetValue(k, out string v) ? L(v, def) : def;

        static long[] Longs(Dictionary<string, string> kv, string k)
        {
            if (!kv.TryGetValue(k, out string v)) return new long[0];
            string[] p = v.Split(',');
            var r = new long[p.Length];
            for (int i = 0; i < p.Length; i++) r[i] = L(p[i], 0);
            return r;
        }

        static int[] Ints(Dictionary<string, string> kv, string k)
        {
            if (!kv.TryGetValue(k, out string v)) return new int[0];
            string[] p = v.Split(',');
            var r = new int[p.Length];
            for (int i = 0; i < p.Length; i++) r[i] = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, L(p[i], 0)));
            return r;
        }

        internal static long L(string s, long d) => long.TryParse(s, NumberStyles.Integer, Ci, out long v) ? v : d;
    }
}
