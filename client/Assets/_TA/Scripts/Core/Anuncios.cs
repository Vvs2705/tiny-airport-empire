using System;
using System.Globalization;
using System.Text;

namespace TA.Core
{
    /// <summary>Os 2 rewarded da v0.1 (GDD §7.2). Salvos pelo Id: placement novo entra no FIM.</summary>
    public enum Anuncio { Cofre, Receita }

    /// <summary>
    /// Regras dos rewarded no nucleo (copia enxuta do Anuncios.cs do Forge; na v0.1 o anuncio e' simulado, sem SDK). Fluxo da View: Pode(a)
    /// mostra o botao com Previa(a) (o que da, ANTES de assistir); o toque pede Oferta(a) = ID de transacao "id:chave"; so o fim do anuncio
    /// chama Pagar(tx). Pagar aplica 1x: a chave so anda para frente e a ultima paga vai no save, entao o mesmo ID nunca paga de novo
    /// (callback repetido, save/load, reabrir). Cofre 2x: 1 vez por retorno (chave = o claim do cofre). Receita 2x: 2x renda por 5 min de
    /// jogo, BoostPorDia por dia local (Sim.Dia, virada as 04:00), sem acumular (so com o boost parado). Nunca durante arrasto, tutorial ou
    /// pausa (Veto). Anuncio nunca e' necessario: o bot nao assiste nenhum.
    /// </summary>
    public sealed class Anuncios
    {
        public static readonly string[] Ids = { "cofre2x", "receita2x" };
        public static readonly int N = Ids.Length;
        readonly Sim _sim;
        /// <summary>Por placement: ultima chave paga e vistos no HojeDia.</summary>
        public readonly long[] Pago = new long[N];
        public readonly int[] Hoje = new int[N];
        public long HojeDia;

        public Anuncios(Sim sim) { _sim = sim; }

        long Chave(Anuncio a) => a == Anuncio.Cofre ? (_sim.LastClaimCash > 0 ? _sim.LastClaim : 0) : Pago[(int)a] + 1;

        public int HojeDe(Anuncio a) => _sim.Dia > HojeDia ? 0 : Hoje[(int)a];

        /// <summary>O botao do placement aparece agora (sem contar se o anuncio carregou: isso e' da View).</summary>
        public bool Pode(Anuncio a)
        {
            if (Chave(a) <= Pago[(int)a]) return false;
            return a == Anuncio.Receita ? _sim.BoostLeft == 0 && HojeDe(a) < _sim.T.BoostPorDia : Previa(a) > 0;
        }

        /// <summary>Cofre = Cash a mais; Receita = o multiplicador (2).</summary>
        public long Previa(Anuncio a) => a == Anuncio.Cofre ? _sim.LastClaimCash : 2;

        /// <summary>ID de transacao da oferta de agora ("id:chave"), ou null se o placement nao pode.</summary>
        public string Oferta(Anuncio a) => Pode(a) ? Ids[(int)a] + ":" + Chave(a).ToString(CultureInfo.InvariantCulture) : null;

        /// <summary>Aplica a recompensa da oferta `tx`, 1x. Devolve o que deu ou -1 (ID ja pago, oferta que mudou, regra que nao deixa).</summary>
        public long Pagar(string tx)
        {
            if (tx == null) return -1;
            for (int i = 0; i < N; i++)
            {
                var a = (Anuncio)i;
                if (Oferta(a) != tx) continue;
                long k = Chave(a), v = Previa(a);
                if (a == Anuncio.Cofre) _sim.Cash += v;   // como o cofre: fora da media movel
                else _sim.BoostLeft = _sim.T.BoostSegundos * Sim.Hz;
                Pago[i] = k;
                if (_sim.Dia > HojeDia) { Array.Clear(Hoje, 0, N); HojeDia = _sim.Dia; }
                Hoje[i]++;
                _sim.Recompensa(a, v);
                return v;
            }
            return -1;
        }

        /// <summary>Por que NAO oferecer anuncio agora (null = pode): nunca durante arrasto, tutorial ou pausa (GDD §7.2).</summary>
        public static string Veto(bool arrastando, bool tutorial, bool pausado) =>
            arrastando ? "arrasto" : tutorial ? "tutorial" : pausado ? "pausa" : null;

        public void Save(StringBuilder sb)
        {
            var ci = CultureInfo.InvariantCulture;
            for (int i = 0; i < N; i++)
                if (Pago[i] != 0 || Hoje[i] != 0)
                    sb.Append("ad.").Append(Ids[i]).Append('=').Append(Pago[i].ToString(ci)).Append(',').Append(Hoje[i].ToString(ci)).Append('\n');
            if (HojeDia != 0) sb.Append("ad.dia=").Append(HojeDia.ToString(ci)).Append('\n');
        }

        /// <summary>Uma linha ad.* (lixo = padrao; Id que sumiu = ignorado).</summary>
        public void Load(string key, string val)
        {
            if (key == "dia") { HojeDia = Sim.L(val, 0); return; }
            int i = Array.IndexOf(Ids, key);
            string[] p = val.Split(',');
            if (i < 0 || p.Length < 2) return;
            Pago[i] = Math.Max(0, Sim.L(p[0], 0));
            Hoje[i] = (int)Math.Max(0, Math.Min(int.MaxValue, Sim.L(p[1], 0)));
        }
    }

    public sealed partial class Sim
    {
        internal void Recompensa(Anuncio a, long v) => Emit(Ev.Recompensa, (int)a, (int)Math.Min(int.MaxValue, v));
    }
}
