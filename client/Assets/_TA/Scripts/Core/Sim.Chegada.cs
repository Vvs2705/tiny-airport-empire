using System;
using System.Collections.Generic;
using System.Text;

namespace TA.Core
{
    /// <summary>Onde esta quem chega (Direcao §8.3, frente H): no aviao, andando ate a restituicao, esperando a mala em volta da esteira ou
    /// tirando a mala dela. Indice vai no save: so cresce no fim.</summary>
    public enum EstadoChegada { NoAviao, Andando, Esperando, Pegando }

    /// <summary>Passageiro que chega. Id = pouso x 64 + ordem (unico entre os que estao chegando); Mala sai da semente (refeita no Load).</summary>
    public sealed class Chegante
    {
        public int Id, Aviao;
        public EstadoChegada Estado;
        /// <summary>Passos que faltam andando ou pegando a mala (0 nos outros estados).</summary>
        public int Resto;
        /// <summary>Lugar em volta da esteira, reservado ao descer (so quem tem mala; -1 = nenhum): Vaga % EsteiraCap = a posicao da esteira em
        /// frente, Vaga / EsteiraCap = a fileira (0 colada na esteira).</summary>
        public int Vaga = -1;
        /// <summary>Mala despachada na origem e se o servico de bagagem do patio ja a tirou do aviao.</summary>
        public bool Mala, MalaSolta;
    }

    /// <summary>
    /// Chegada e restituicao de bagagem (frente H; GDD §4.1 linha H, CONTRATO_LANCAMENTO §1). Tudo sai do relogio do quadro e do patio, sem
    /// sorteio pago e sem Cash: quem chega ja pagou na origem, entao a chegada nao mexe na economia (o TA-1 e o TA-2 ficam iguais, teste
    /// ChegadaTests). O efeito na nota entra com a satisfacao (frente G).
    ///  - Pouso (Encosta, fora o do hangar e o de retorno): o aviao traz ChegadaFator x os passageiros da onda (teto ChegadaMax); cada um
    ///    tem mala despachada com ChegadaMalaChance.
    ///  - Desce na proporcao do servico de desembarque do patio e anda ChegadaAndar s ate a restituicao (o estande remoto e mais longe).
    ///    Quem tem mala reserva um lugar em volta da esteira; quem nao tem sai direto.
    ///  - As malas saem do aviao na proporcao do servico de bagagem e entram no Carrinho, a fila da esteira, na ordem.
    ///  - Esteira: EsteiraCap lugares numa volta que anda um lugar a cada EsteiraSeg s. A cada passo da volta, quem espera tira a mala dele
    ///    se ela esta em frente ao seu lugar (ordem da esteira) e a boca poe a 1a do carrinho no lugar vazio que passa por ela. Esteira cheia
    ///    de malas de quem ainda anda = o carrinho espera (fila de verdade, §0.5 regra 1). Tirou a mala (PegarSeg s): sai pela calcada.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Quem esta chegando, na ordem do pouso.</summary>
        public readonly List<Chegante> Chegantes = new List<Chegante>();
        /// <summary>Malas fora do aviao esperando lugar na esteira, na ordem (Id do dono).</summary>
        public readonly List<int> Carrinho = new List<int>();
        /// <summary>A esteira: em cada lugar, o Id do dono da mala (-1 = vazio). O lugar j esta em frente a posicao (j + Giro) % EsteiraCap; a
        /// boca fica na posicao 0.</summary>
        public int[] Esteira => _esteira ?? (_esteira = Vazia(Math.Max(1, T.EsteiraCap)));
        /// <summary>Passos de um lugar da esteira e quantos lugares ela ja andou (a tela desenha a volta continua com Steps / EsteiraPassos).</summary>
        public int EsteiraPassos => Passos(T.EsteiraSeg);
        public long Giro => Steps / EsteiraPassos;

        int[] _esteira;
        /// <summary>Por aviao, do ultimo pouso: quantos chegam, quantos ja desceram, quantas malas e quantas ja sairam do aviao.</summary>
        readonly int[] _chegam = new int[2], _desceram = new int[2], _malas = new int[2], _malasFora = new int[2];

        static int[] Vazia(int n)
        {
            var e = new int[n];
            for (int i = 0; i < n; i++) e[i] = -1;
            return e;
        }

        bool MalaDe(int id) => U(id, 0, 0, 7) < T.ChegadaMalaChance;

        /// <summary>Encosta (Sim.Patio.cs): o pouso com passageiros. O que sobrou do pouso anterior desce e sai do aviao antes.</summary>
        void Desembarque(Aviao a)
        {
            Solta(a, int.MaxValue, false);
            Solta(a, int.MaxValue, true);
            int n = Math.Max(0, Math.Min(Math.Min(63, T.ChegadaMax), (int)Math.Round(T.Pax(Math.Max(0, Onda)) * (double)T.ChegadaFator)));
            int pouso = (int)((Steps * 2 + a.Id) & 0xFFFFFF) * 64;   // o Id so precisa ser unico entre quem chega agora: a volta em 2^24 pousos nao cruza
            _chegam[a.Id] = n; _desceram[a.Id] = 0; _malas[a.Id] = 0; _malasFora[a.Id] = 0;
            for (int i = 0; i < n; i++)
            {
                var c = new Chegante { Id = pouso + i, Aviao = a.Id, Mala = MalaDe(pouso + i) };
                if (c.Mala) _malas[a.Id]++;
                Chegantes.Add(c);
            }
        }

        /// <summary>Quantos ja deviam ter saido do aviao pelo servico `s`: na proporcao do que ele andou; feito (ou zerado no hangar e na decolagem) = todos.</summary>
        int Feito(Aviao a, Servico s, int total)
        {
            int f = a.Falta[(int)s];
            if (f <= 0) return total;
            return (int)(total * Math.Max(0.0, 1.0 - f / (double)ServicoPassos(s)));
        }

        /// <summary>Desce (mala = false) ou tira do aviao a mala (true) ate `alvo` do aviao, na ordem do pouso. So cresce.</summary>
        void Solta(Aviao a, int alvo, bool mala)
        {
            int[] feitos = mala ? _malasFora : _desceram;
            alvo = Math.Min(alvo, mala ? _malas[a.Id] : _chegam[a.Id]);
            if (feitos[a.Id] >= alvo) return;
            foreach (Chegante c in Chegantes)
            {
                if (feitos[a.Id] >= alvo) break;
                if (c.Aviao != a.Id) continue;
                if (mala)
                {
                    if (!c.Mala || c.MalaSolta) continue;
                    c.MalaSolta = true;
                    Carrinho.Add(c.Id);
                }
                else
                {
                    if (c.Estado != EstadoChegada.NoAviao) continue;
                    c.Estado = EstadoChegada.Andando;
                    c.Resto = AndarPassos(c.Aviao);
                    if (c.Mala) c.Vaga = VagaLivre();
                }
                feitos[a.Id]++;
            }
            feitos[a.Id] = alvo;   // faltou quem soltar so com save adulterado: nao procura de novo a cada passo
        }

        int ContaSoltos(int aviao, bool mala)
        {
            int n = mala ? _malas[aviao] : _chegam[aviao];
            foreach (Chegante c in Chegantes)
                if (c.Aviao == aviao && (mala ? c.Mala && !c.MalaSolta : c.Estado == EstadoChegada.NoAviao)) n--;
            return Math.Max(0, n);
        }

        int AndarPassos(int aviao) => Passos(T.ChegadaAndar[Math.Max(0, Math.Min(T.ChegadaAndar.Length - 1, aviao))]);
        int PegarPassos => Passos(T.PegarSeg);

        /// <summary>O menor lugar em volta da esteira sem ninguem (as fileiras de tras so enchem com a da frente cheia).</summary>
        int VagaLivre()
        {
            for (int k = 0; ; k++)
            {
                bool livre = true;
                foreach (Chegante c in Chegantes) if (c.Vaga == k) { livre = false; break; }
                if (livre) return k;
            }
        }

        Chegante Chegando(int id)
        {
            foreach (Chegante c in Chegantes) if (c.Id == id) return c;
            return null;
        }

        /// <summary>Um passo da chegada, depois do patio (Passo): desce, sai do aviao, anda, espera, pega e sai.</summary>
        void Chegada()
        {
            foreach (Aviao a in Avioes)
            {
                Solta(a, Feito(a, Servico.Desembarque, _chegam[a.Id]), false);
                Solta(a, Feito(a, Servico.Bagagem, _malas[a.Id]), true);
            }
            for (int i = 0; i < Chegantes.Count; i++)
            {
                Chegante c = Chegantes[i];
                if (c.Estado == EstadoChegada.Andando && --c.Resto <= 0)
                {
                    c.Resto = 0;
                    if (c.Mala) c.Estado = EstadoChegada.Esperando;
                    else Chegantes.RemoveAt(i--);   // sem mala: passa pela restituicao e sai
                }
                else if (c.Estado == EstadoChegada.Pegando && --c.Resto <= 0) Chegantes.RemoveAt(i--);
            }
            if (Steps % EsteiraPassos != 0) return;
            int[] e = Esteira;
            int n = e.Length;
            long giro = Giro;
            for (int j = 0; j < n; j++)
            {
                Chegante c = e[j] >= 0 ? Chegando(e[j]) : null;
                if (c == null || c.Estado != EstadoChegada.Esperando || c.Vaga % n != (int)((j + giro) % n)) continue;
                c.Estado = EstadoChegada.Pegando;
                c.Resto = PegarPassos;
                e[j] = -1;
            }
            int boca = (int)((n - giro % n) % n);
            if (e[boca] < 0 && Carrinho.Count > 0)
            {
                e[boca] = Carrinho[0];
                Carrinho.RemoveAt(0);
            }
        }

        // ------------------------------------------------------------------ save (schema 13)

        /// <summary>Schema 13: quem esta chegando, a esteira, o carrinho e as contas do aviao que ainda tem gente ou mala a bordo. Nada em andamento =
        /// nenhuma chave (as contas de um pouso ja todo fora nao mudam mais nada: o proximo pouso as refaz).</summary>
        void SalvaChegada(StringBuilder sb)
        {
            for (int a = 0; a < 2; a++)
                if (_desceram[a] < _chegam[a] || _malasFora[a] < _malas[a]) sb.Append("chav.").Append(a).Append('=').Append(_chegam[a]).Append(',').Append(_desceram[a]).Append(',').Append(_malas[a]).Append(',').Append(_malasFora[a]).Append('\n');
            if (Chegantes.Count > 0)
                sb.Append("cheg=").Append(string.Join(";", Chegantes.ConvertAll(c => c.Id.ToString(Ci) + ":" + c.Aviao + ":" + (int)c.Estado + ":" + c.Resto + ":" + c.Vaga + ":" + (c.MalaSolta ? 1 : 0)))).Append('\n');
            if (Array.Exists(Esteira, x => x >= 0)) sb.Append("esteira=").Append(string.Join(",", Esteira)).Append('\n');
            if (Carrinho.Count > 0) sb.Append("carrinho=").Append(string.Join(",", Carrinho)).Append('\n');
        }

        /// <summary>Le o schema 13, com tudo limitado; o schema 12 e anteriores nao tem as chaves: ninguem chegando. Mala que o save perdeu
        /// (nem na esteira nem no carrinho) volta para o fim do carrinho: ninguem fica esperando para sempre.</summary>
        void CarregaChegada(Dictionary<string, string> kv)
        {
            var vagas = new HashSet<int>();
            if (kv.TryGetValue("cheg", out string ch))
                foreach (string tok in ch.Split(';'))
                {
                    string[] p = tok.Split(':');
                    if (p.Length < 6) continue;
                    long id = L(p[0], -1), av = L(p[1], -1), est = L(p[2], -1);
                    if (id < 0 || id > int.MaxValue || av < 0 || av > 1 || est < 0 || est > (int)EstadoChegada.Pegando || Chegando((int)id) != null) continue;
                    var c = new Chegante { Id = (int)id, Aviao = (int)av, Estado = (EstadoChegada)est, Mala = MalaDe((int)id) };
                    c.MalaSolta = c.Mala && L(p[5], 0) != 0;
                    if (!c.Mala && c.Estado >= EstadoChegada.Esperando) continue;   // sem mala nao espera nem pega
                    if (c.Estado == EstadoChegada.Pegando && !c.MalaSolta) c.Estado = EstadoChegada.Esperando;
                    int teto = c.Estado == EstadoChegada.Andando ? AndarPassos(c.Aviao) : c.Estado == EstadoChegada.Pegando ? PegarPassos : 0;
                    c.Resto = teto > 0 ? (int)Math.Max(1, Math.Min(teto, L(p[3], 1))) : 0;
                    long vaga = L(p[4], -1);
                    c.Vaga = c.Mala && c.Estado != EstadoChegada.NoAviao && vaga >= 0 && vaga < 1024 && vagas.Add((int)vaga) ? (int)vaga : -1;
                    Chegantes.Add(c);
                }
            foreach (Chegante c in Chegantes)   // vaga repetida ou perdida: a menor livre
                if (c.Mala && c.Estado != EstadoChegada.NoAviao && c.Vaga < 0) c.Vaga = VagaLivre();
            var malas = new HashSet<int>();
            bool Valida(long id)
            {
                Chegante c = id >= 0 && id <= int.MaxValue ? Chegando((int)id) : null;
                return c != null && c.MalaSolta && c.Estado != EstadoChegada.Pegando && malas.Add(c.Id);
            }
            int[] e = Esteira;
            long[] est2 = Longs(kv, "esteira");
            for (int j = 0; j < e.Length && j < est2.Length; j++) e[j] = Valida(est2[j]) ? (int)est2[j] : -1;
            foreach (long id in Longs(kv, "carrinho")) if (Valida(id)) Carrinho.Add((int)id);
            foreach (Chegante c in Chegantes) if (c.MalaSolta && c.Estado != EstadoChegada.Pegando && malas.Add(c.Id)) Carrinho.Add(c.Id);
            for (int a = 0; a < 2; a++)
            {
                int[] x = Ints(kv, "chav." + a);
                if (x.Length < 4) continue;
                _chegam[a] = Math.Max(0, Math.Min(63, x[0]));
                _malas[a] = Math.Max(0, Math.Min(_chegam[a], x[2]));
                _desceram[a] = Math.Max(ContaSoltos(a, false), Math.Min(_chegam[a], x[1]));
                _malasFora[a] = Math.Max(ContaSoltos(a, true), Math.Min(_malas[a], x[3]));
            }
        }
    }
}
