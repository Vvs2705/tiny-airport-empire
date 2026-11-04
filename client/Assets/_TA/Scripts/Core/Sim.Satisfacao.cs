using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TA.Core
{
    /// <summary>Os 5 motivos da satisfacao (frente G), na ordem da tela. Indice vai no save: so cresce no fim.</summary>
    public enum Fator { Espera, Limpeza, Banheiro, Pontualidade, Conforto }

    /// <summary>Perfil do passageiro: sai do tipo da rota do voo (os arquetipos da campanha, GDD §5.7; mesma ordem de TipoRota) e muda so a tolerancia.
    /// O grupo extra de conexao (incidente) e' Conexao. Nao vai no save: sai do voo.</summary>
    public enum PerfilPax { Executivo, Familia, Conexao, Viajante }

    public enum Carinha { Contente, Neutra, Brava }

    /// <summary>
    /// Frente G: satisfacao com motivo (GDD §4.1 linha G, Direcao §8.4). So le o que os outros sistemas ja fazem; nada sorteado, nada pago.
    ///  - Passageiro: nota de 0 a 100 = SatBase + o peso de cada Fator, pela tolerancia do PerfilPax dele:
    ///    Espera = s nas filas (no portao, so a fila do embarque aberto); Limpeza = a pior sujeira das salas em que foi atendido (e do banheiro);
    ///    Banheiro = quem precisava: fila na porta ou achou fechado (Sim.Limpeza.cs); Pontualidade = s no portao depois da hora de abrir o
    ///    embarque sem ele abrir (patio, tripulacao, clima); Conforto = s em pe no portao, sem cadeira livre (Tuning.CadeirasPortao).
    ///    Cada fator vai bem (+SatMais) ou pesa contra, ate o teto dele (SatTeto). Abaixo de SatReclama o passageiro reclama, 1x, do motivo principal.
    ///  - Aeroporto: media dos ultimos ~SatJanela que sairam (embarcou ou perdeu o voo), com o peso medio de cada Fator, as reclamacoes do dia do
    ///    jogo e onde cada motivo pesa (SatOnde), para o toque levar a sala ou ao voo.
    ///  - Satisfacao e' leitura e bonus, nao e' moeda (GDD §6.1): nunca tira Cash nem passageiro (§5.4). O bonus e' a gorjeta de quem embarca
    ///    contente, so com RegraSatBonus (portao do §5.13). Sem ela, o jogo e' o mesmo numero a numero.
    /// </summary>
    public sealed partial class Sim
    {
        public const int Fatores = 5;
        /// <summary>Passos entre as contagens (0,5 s, como Sim.AtualizaEstados): as filas somam aos pedacos.</summary>
        const int SatPasso = Hz / 2;
        /// <summary>O "onde" de cada motivo esquece pela metade a cada minuto de jogo.</summary>
        static readonly double SatEsquece = Math.Pow(0.5, SatPasso / (double)Hz / 60.0);

        double _satMedia = -1;
        long _satN;
        int _satDia;
        readonly double[] _satFator = new double[Fatores], _esperaEm = new double[8], _atrasoEm = new double[2];
        readonly int[] _reclama = new int[Fatores];

        /// <summary>Nota media do aeroporto (0 a 100; -1 = ninguem saiu ainda) e quantos ja entraram nela.</summary>
        public double SatMedia => _satMedia;
        public long SatAmostras => _satN;
        /// <summary>Peso medio do fator na nota de quem saiu (negativo = pesa contra).</summary>
        public double SatFator(Fator f) => _satFator[(int)f];
        /// <summary>Reclamacoes do dia do jogo por motivo.</summary>
        public int Reclamacoes(Fator f) => _reclama[(int)f];

        public Carinha CarinhaDe(int nota) => nota >= T.SatContente ? Carinha.Contente : nota < T.SatBrava ? Carinha.Brava : Carinha.Neutra;

        public PerfilPax PerfilDe(Pax p)
        {
            Voo v = VooDe(p);
            if (v == null) return PerfilPax.Executivo;
            return p.Indice >= v.Base && v.GrupoConexao ? PerfilPax.Conexao : (PerfilPax)(int)Efeito(v).Tipo;
        }

        /// <summary>O que o fator soma (ou tira) da nota do passageiro agora.</summary>
        public int Peso(Pax p, Fator f)
        {
            int k = (int)PerfilDe(p), i = (int)f, mais = T.SatMais[i];
            Voo v;
            switch (f)
            {
                case Fator.Espera: return Curva(i, p.Espera * Step / T.TolEspera[k]);
                case Fator.Limpeza: return Curva(i, p.Sujo / (double)T.TolSujeira[k]);
                case Fator.Banheiro:
                    if (p.Negado) return -Math.Min(T.SatTeto[i], T.SatNegado);
                    if (p.FilaBanheiro > 0) return Curva(i, p.FilaBanheiro * Step / T.TolBanheiro);
                    return p.UsouBanheiro ? mais : 0;
                case Fator.Pontualidade:
                    if (p.Atraso > 0) return Contra(i, p.Atraso * Step / T.TolAtraso[k]);
                    return (v = VooDe(p)) != null && v.Embarque ? mais : 0;
                default:
                    if (p.EmPe > 0) return Contra(i, p.EmPe * Step / T.TolEmPe[k]);
                    return p.Sentou ? mais : 0;
            }
        }

        /// <summary>x = quanto da tolerancia ja foi: +SatMais em 0, 0 na metade, -SatMais na tolerancia e dai em linha ate o teto.</summary>
        int Curva(int i, double x) => (int)Math.Max(-T.SatTeto[i], Math.Round(T.SatMais[i] * (1 - 2 * x), MidpointRounding.AwayFromZero));

        /// <summary>So pesa contra (atraso e ficar em pe nao tem lado bom): -2 x SatMais na tolerancia.</summary>
        int Contra(int i, double x) => -(int)Math.Min(T.SatTeto[i], Math.Round(2 * T.SatMais[i] * x, MidpointRounding.AwayFromZero));

        public int Nota(Pax p)
        {
            int n = T.SatBase;
            for (int f = 0; f < Fatores; f++) n += Peso(p, (Fator)f);
            return Math.Max(0, Math.Min(100, n));
        }

        /// <summary>O motivo principal: o que mais pesa contra; sem nenhum contra, o que mais ajuda (empate: a ordem de Fator).</summary>
        public Fator Principal(Pax p)
        {
            int pior = 0, melhor = 0; Fator fp = Fator.Espera, fm = Fator.Espera;
            for (int f = 0; f < Fatores; f++)
            {
                int w = Peso(p, (Fator)f);
                if (w < pior) { pior = w; fp = (Fator)f; }
                if (w > melhor) { melhor = w; fm = (Fator)f; }
            }
            return pior < 0 ? fp : fm;
        }

        /// <summary>O k-esimo motivo que mais pesa no aeroporto (|peso medio|; empate: a ordem de Fator). Sem alocar: o HUD chama todo quadro.</summary>
        public Fator SatTop(int k)
        {
            for (int f = 0; f < Fatores; f++)
            {
                int acima = 0;
                for (int g = 0; g < Fatores; g++)
                    if (Math.Abs(_satFator[g]) > Math.Abs(_satFator[f]) || Math.Abs(_satFator[g]) == Math.Abs(_satFator[f]) && g < f) acima++;
                if (acima == k) return (Fator)f;
            }
            return Fator.Espera;
        }

        /// <summary>Onde o motivo pesa: Espera = a Estacao com mais espera no ultimo minuto (ou a maior fila agora); Limpeza = a Area mais suja;
        /// Banheiro = Area.Banheiro; Pontualidade = a vaga (indice em Voos) do voo com mais atraso no ultimo minuto; Conforto = Estacao.Portao.</summary>
        public int SatOnde(Fator f)
        {
            switch (f)
            {
                case Fator.Espera:
                    int e = 0;
                    for (int i = 1; i < 6; i++)
                        if (_esperaEm[i] > _esperaEm[e] || _esperaEm[i] == _esperaEm[e] && Estacoes[i].Fila.Count > Estacoes[e].Fila.Count) e = i;
                    return e;
                case Fator.Limpeza:
                    int a = 0;
                    for (int i = 1; i < Areas; i++) if (Sujeira[i] > Sujeira[a]) a = i;
                    return a;
                case Fator.Banheiro: return (int)Area.Banheiro;
                case Fator.Pontualidade: return _atrasoEm[1] > _atrasoEm[0] ? 1 : 0;
                default: return (int)Estacao.Portao;
            }
        }

        // ------------------------------------------------------------------ passo e ganchos

        /// <summary>Sim.Passo, depois do banheiro: a cada 0,5 s conta a espera de quem esta nas filas, a cadeira e o atraso no portao e a fila do banheiro.</summary>
        void PassoSatisfacao(int loc)
        {
            if (DiaJogo != _satDia) { Array.Clear(_reclama, 0, Fatores); _satDia = DiaJogo; }
            if (Steps % SatPasso != 0) return;
            for (int i = 0; i < _esperaEm.Length; i++) _esperaEm[i] *= SatEsquece;
            for (int i = 0; i < _atrasoEm.Length; i++) _atrasoEm[i] *= SatEsquece;
            foreach (Station s in Estacoes)
            {
                if (s.Tipo == Estacao.Manutencao || s.Tipo == Estacao.Despacho) continue;
                bool portao = s.Tipo == Estacao.Portao;
                int sentados = 0;
                foreach (Pax p in s.Fila)
                {
                    Voo v = VooDe(p);
                    if (!portao || PodeEmbarcar(v)) { p.Espera += SatPasso; _esperaEm[(int)s.Tipo]++; }
                    else
                    {
                        // esperando o embarque abrir: senta na ordem de chegada, enquanto houver cadeira (Mundo.Espera mostra o mesmo)
                        if (sentados < T.CadeirasPortao) { sentados++; p.Sentou = true; } else p.EmPe += SatPasso;
                        if (v != null && loc >= v.AbrePasso) { p.Atraso += SatPasso; _atrasoEm[v.Indice]++; }
                    }
                    Confere(p);
                }
            }
            foreach (Pax p in Banheiro.Fila) { p.FilaBanheiro += SatPasso; Confere(p); }
        }

        /// <summary>Sim.Avanca (antes de sujar) e a entrada na cabine: a sujeira da sala em que o passageiro foi atendido.</summary>
        void Ve(Pax p, int area)
        {
            if (area < 0 || !T.RegraLimpeza) return;
            p.Sujo = Math.Max(p.Sujo, Sujeira[area]);
            Confere(p);
        }

        void UsaBanheiro(Pax p)
        {
            p.UsouBanheiro = true;
            Ve(p, (int)Area.Banheiro);
        }

        /// <summary>Abaixo de SatReclama, reclama 1x do motivo principal (a tela mostra; nada muda no jogo).</summary>
        void Confere(Pax p)
        {
            if (p.Reclamou || Nota(p) >= T.SatReclama) return;
            p.Reclamou = true;
            Fator f = Principal(p);
            _reclama[(int)f]++;
            Emit(Ev.Reclamou, p.Id, (int)f);
        }

        /// <summary>Embarcou (Sim.Avanca) ou perdeu o voo (Sim.Fecha): a nota entra na media do aeroporto. Gorjeta so com RegraSatBonus, e so soma.</summary>
        void Saiu(Pax p, bool embarcou, int tarifa)
        {
            Confere(p);
            int nota = Nota(p);
            _satN++;
            double a = 1.0 / Math.Min(_satN, Math.Max(1, T.SatJanela));   // media simples ate a janela encher, depois movel
            _satMedia += (nota - _satMedia) * a;
            for (int f = 0; f < Fatores; f++) _satFator[f] += (Peso(p, (Fator)f) - _satFator[f]) * a;
            if (!embarcou || !T.RegraSatBonus || nota < T.SatGorjeta) return;
            long g = (long)Math.Round(tarifa * (double)T.SatBonus, MidpointRounding.AwayFromZero);
            if (g > 0) Emit(Ev.Gorjeta, p.Id, (int)Ganha(g, Fonte.Bonus));
        }

        /// <summary>Sim.Fecha, antes de tirar das filas: quem ficou perdeu o voo. Ficou na fila (ou no banheiro) quando o portao fechou: a espera
        /// vai ao teto, e sai com essa nota.</summary>
        void PerdeuOVoo(Voo v)
        {
            foreach (Pax p in Presentes())
                if (p.Onda == v.Onda && p.Voo == v.Indice)
                {
                    p.Espera = Math.Max(p.Espera, 10 * OndaPassos);   // ponytail: 10 ondas de fila passam do teto de qualquer perfil
                    Saiu(p, false, 0);
                }
        }

        /// <summary>Todo passageiro de partida no aeroporto agora: filas, atendimento e banheiro.</summary>
        IEnumerable<Pax> Presentes()
        {
            foreach (Station s in Estacoes)
            {
                foreach (Pax p in s.Fila) yield return p;
                foreach (Pax p in s.Atendendo) yield return p;
            }
            foreach (List<Pax> l in new[] { Banheiro.Indo, Banheiro.Fila, Banheiro.Usando, Banheiro.Voltando })
                foreach (Pax p in l) yield return p;
        }

        // ------------------------------------------------------------------ save (schema 17)

        /// <summary>Schema 17: a media do aeroporto (sat: amostras, dia, reclamacoes; satm: media, fatores e o "onde") e quem esta no aeroporto (satp).</summary>
        void SalvaSatisfacao(StringBuilder sb)
        {
            sb.Append("sat=").Append(_satN).Append(',').Append(_satDia);
            foreach (int r in _reclama) sb.Append(',').Append(r);
            sb.Append("\nsatm=").Append(_satMedia.ToString("R", Ci));
            foreach (double[] l in new[] { _satFator, _esperaEm, _atrasoEm }) foreach (double x in l) sb.Append(',').Append(x.ToString("R", Ci));
            sb.Append('\n');
            var ps = new List<string>();
            foreach (Pax p in Presentes())
            {
                int b = (p.Negado ? 1 : 0) | (p.Sentou ? 2 : 0) | (p.Reclamou ? 4 : 0) | (p.UsouBanheiro ? 8 : 0);
                if (b != 0 || p.Espera + p.Sujo + p.FilaBanheiro + p.Atraso + p.EmPe > 0)
                    ps.Add(string.Join(",", p.Id, p.Espera, p.Sujo, p.FilaBanheiro, p.Atraso, p.EmPe, b));
            }
            if (ps.Count > 0) sb.Append("satp=").Append(string.Join(";", ps)).Append('\n');
        }

        /// <summary>Le o schema 17, com tudo limitado (lixo vira o padrao). Os schemas 1 a 16 nao tem as chaves: ninguem saiu ainda e todos chegam zerados.</summary>
        void CarregaSatisfacao(Dictionary<string, string> kv)
        {
            int[] n = Ints(kv, "sat");
            string[] m = kv.TryGetValue("satm", out string sm) ? sm.Split(',') : new string[0];
            double D(int i, double lo, double hi, double def) =>
                i < m.Length && double.TryParse(m[i], NumberStyles.Float, Ci, out double x) && !double.IsNaN(x) ? Math.Max(lo, Math.Min(hi, x)) : def;
            if (n.Length >= 2 + Fatores)
            {
                _satN = Math.Max(0, n[0]);
                _satDia = n[1];
                for (int f = 0; f < Fatores; f++) _reclama[f] = Math.Max(0, n[2 + f]);   // de outro dia: o 1o passo zera
                _satMedia = _satN > 0 ? D(0, 0, 100, T.SatBase) : -1;
                for (int f = 0; f < Fatores; f++) _satFator[f] = _satN > 0 ? D(1 + f, -T.SatTeto[f], T.SatMais[f], 0) : 0;
            }
            for (int i = 0; i < _esperaEm.Length; i++) _esperaEm[i] = D(1 + Fatores + i, 0, 1e9, 0);
            for (int i = 0; i < _atrasoEm.Length; i++) _atrasoEm[i] = D(1 + Fatores + _esperaEm.Length + i, 0, 1e9, 0);
            if (!kv.TryGetValue("satp", out string sp)) return;
            var dados = new Dictionary<int, int[]>();
            foreach (string tok in sp.Split(';'))
            {
                string[] a = tok.Split(',');
                if (a.Length != 7) continue;
                var x = new int[7];
                for (int i = 0; i < 7; i++) x[i] = (int)Math.Max(0, Math.Min(int.MaxValue, L(a[i], 0)));
                dados[x[0]] = x;
            }
            foreach (Pax p in Presentes())
            {
                if (!dados.TryGetValue(p.Id, out int[] x)) continue;
                int teto = OndaPassos;   // ninguem fica mais de uma onda no aeroporto
                p.Espera = Math.Min(teto, x[1]); p.Sujo = Math.Min(SujeiraMax, x[2]); p.FilaBanheiro = Math.Min(teto, x[3]);
                p.Atraso = Math.Min(teto, x[4]); p.EmPe = Math.Min(teto, x[5]);
                p.Negado = (x[6] & 1) != 0 && p.Banheiro; p.Sentou = (x[6] & 2) != 0; p.Reclamou = (x[6] & 4) != 0; p.UsouBanheiro = (x[6] & 8) != 0 && p.Banheiro;
            }
        }
    }
}
