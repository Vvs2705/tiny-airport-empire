using System;
using System.Collections.Generic;

namespace TA.Core
{
    /// <summary>GDD §7.3. O indice nao vai no save (a posse vai pelo Id).</summary>
    public enum TipoProduto { Consumivel, NaoConsumivel }

    /// <summary>O que as Gems compram (GDD §6.1, CONTRATO_FASE_D1 §3). So estes 3: tipo novo so com mudanca no GDD e no contrato. O indice nao vai no save.</summary>
    public enum TipoItem { PacoteCash, Booster, Cosmetico }

    /// <summary>Por que a compra foi ou nao creditada. Creditado e Repetido = a loja pode confirmar (acknowledge); o resto nao.</summary>
    public enum Credito { Creditado, Repetido, JaTem, Invalido, Teto, SaveRecusado }

    /// <summary>
    /// Uma linha do catalogo (GDD §7.3). Id = o da loja: decide-se uma vez, nunca muda nem se reaproveita. Sem preco: preco, moeda e
    /// localizacao vem da loja (GDD §7.1). Conteudo fixo; a posse de um NaoConsumivel e' o cosmetico, o passe ou a pintura do pacote.
    /// </summary>
    public sealed class Produto
    {
        public string Id = "";
        public TipoProduto Tipo;
        public long Cash;
        public int Gems;
    }

    /// <summary>Uma linha do que as Gems compram. Preco em Gems, fixo. Cosmetico: Id = o Produto que ele entrega (a posse e' a mesma da compra direta).</summary>
    public sealed class ItemGems
    {
        public string Id = "";
        public TipoItem Tipo;
        public int Preco;
    }

    public static partial class Tabelas
    {
        /// <summary>O catalogo, igual ao CONTRATO_FASE_D1 §2. Produto novo so no fim, com Id novo.</summary>
        public static readonly Produto[] Produtos =
        {
            new Produto { Id = "welcome_bundle_01", Tipo = TipoProduto.NaoConsumivel, Cash = 1000, Gems = 200 },
            new Produto { Id = "gems_pack_small", Tipo = TipoProduto.Consumivel, Gems = 100 },
            new Produto { Id = "gems_pack_medium", Tipo = TipoProduto.Consumivel, Gems = 550 },
            new Produto { Id = "gems_pack_large", Tipo = TipoProduto.Consumivel, Gems = 1200 },
            new Produto { Id = "season_pass_s1", Tipo = TipoProduto.NaoConsumivel },
            new Produto { Id = "aircraft_skin_blue_01", Tipo = TipoProduto.NaoConsumivel },
            new Produto { Id = "terminal_decor_01", Tipo = TipoProduto.NaoConsumivel },
        };

        public static readonly ItemGems[] ItensGems =
        {
            new ItemGems { Id = "cash_pack", Tipo = TipoItem.PacoteCash, Preco = 50 },
            new ItemGems { Id = "boost_30min", Tipo = TipoItem.Booster, Preco = 150 },
            new ItemGems { Id = "aircraft_skin_blue_01", Tipo = TipoItem.Cosmetico, Preco = 300 },
            new ItemGems { Id = "terminal_decor_01", Tipo = TipoItem.Cosmetico, Preco = 300 },
        };
    }

    /// <summary>
    /// Segunda moeda e compras (CONTRATO_FASE_D1). Creditar(tx, produto) e' o ponto que o servidor chama depois de validar na loja: aplica 1x
    /// por id de transacao, com o conteudo da tabela (nenhuma quantidade vem de fora). A View confirma na loja so depois de gravar o save. O id
    /// vai no save como SHA-256: o token de compra nunca fica em disco. Gems ficam fora do Ganha e da media movel: o cofre nao aprende com
    /// elas; o offline e os anuncios nunca dao Gems. Nada aqui e' sorteado nem toca em voo, passageiro, marco ou campanha.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Moeda premium (GDD §6.1): nao expira, entre 0 e Tuning.GemsTeto.</summary>
        public long Gems;

        // ponytail: 1 linha por compra e nunca sai: tirar a mais antiga reabriria o credito dobrado dela
        readonly SortedSet<string> _tx = new SortedSet<string>(StringComparer.Ordinal);
        readonly SortedSet<string> _posse = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>Tem o Produto nao consumivel (pela compra direta ou pelas Gems).</summary>
        public bool Tem(string produto) => _posse.Contains(produto);

        /// <summary>Credita a transacao `tx` do produto: Repetido = ja creditada (mesmo com outro produto no pedido). Teto e SaveRecusado nao
        /// anotam a transacao: sem o acknowledge a loja devolve, e o jogador nao perde o dinheiro calado.</summary>
        public Credito Creditar(string tx, string produto)
        {
            Produto p = Array.Find(T.Produtos, x => x.Id == produto);
            if (string.IsNullOrWhiteSpace(tx) || p == null) return Credito.Invalido;
            if (SaveRefused) return Credito.SaveRecusado;
            string id = SaveEnvelope.Hash(tx);
            if (_tx.Contains(id)) return Credito.Repetido;
            bool unico = p.Tipo == TipoProduto.NaoConsumivel;
            if (unico && Tem(p.Id)) { _tx.Add(id); return Credito.JaTem; }
            if (p.Gems > T.GemsTeto - Gems) return Credito.Teto;
            _tx.Add(id);
            if (unico) _posse.Add(p.Id);
            Gems += p.Gems;
            SomaCash(p.Cash);
            Emit(Ev.CompraCreditada, Array.IndexOf(T.Produtos, p), p.Gems, (int)Math.Min(int.MaxValue, p.Cash));
            return Credito.Creditado;
        }

        /// <summary>O item pode ser comprado agora: saldo, booster sem acumular com outro 2x (GDD §7.2), cosmetico uma vez.</summary>
        public bool PodeGastar(int item)
        {
            if (item < 0 || item >= T.ItensGems.Length || Gems < T.ItensGems[item].Preco) return false;
            ItemGems g = T.ItensGems[item];
            return g.Tipo == TipoItem.PacoteCash || (g.Tipo == TipoItem.Booster ? BoostLeft == 0 : !Tem(g.Id));
        }

        /// <summary>O que o item da AGORA, para mostrar antes da compra: Cash do pacote, s de 2x do booster, 1 cosmetico.</summary>
        public long PreviaGasto(int item)
        {
            switch (T.ItensGems[item].Tipo)
            {
                case TipoItem.PacoteCash: return (long)Math.Max(T.PacotePiso, Math.Min(T.PacoteTeto, Math.Floor(RateEma * T.PacoteSeg)));
                case TipoItem.Booster: return T.BoosterSeg;
                default: return 1;
            }
        }

        /// <summary>Compra o item com Gems. false = nada mudou.</summary>
        public bool Gastar(int item)
        {
            if (!PodeGastar(item)) return false;
            ItemGems g = T.ItensGems[item];
            long v = PreviaGasto(item);
            Gems -= g.Preco;
            if (g.Tipo == TipoItem.PacoteCash) SomaCash(v);   // como o cofre: fora da media movel
            else if (g.Tipo == TipoItem.Booster) BoostLeft = (int)v * Hz;
            else _posse.Add(g.Id);
            Emit(Ev.GemsGastas, item, g.Preco, (int)Math.Min(int.MaxValue, v));
            return true;
        }

        /// <summary>Fechar a cadeia de um aeroporto da Gems: a meta clara do GDD §6.1.</summary>
        void GanhaGems(int aeroporto)
        {
            Gems = Math.Min(T.GemsTeto, Gems + T.GemsPorCadeia);
            Emit(Ev.GemsGanhas, aeroporto, T.GemsPorCadeia);
        }

        /// <summary>Soma que nao vira negativo: o Cash do save e' limitado so por baixo (GDD §8.6).</summary>
        void SomaCash(long v) => Cash = Cash > long.MaxValue - v ? long.MaxValue : Cash + v;
    }
}
