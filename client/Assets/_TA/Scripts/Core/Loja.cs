using System;
using System.Collections.Generic;

namespace TA.Core
{
    /// <summary>O que o servidor de validacao respondeu (servidor/src/app.js). Indisponivel = rede, 429 ou 503: tenta de novo mais tarde.</summary>
    public enum Veredito { Ok, Pendente, Recusado, Indisponivel }

    /// <summary>Onde a compra esta. Validando -> (Gravar -> Confirmar) | Esperando | Recusada | Falhou. So Confirmar vai ao acknowledge da loja.</summary>
    public enum EstadoCompra { Validando, Esperando, Gravar, Confirmar, Recusada, Falhou }

    /// <summary>
    /// Compra em 2 passos (GDD §7.4, CONTRATO_FASE_D1 §4.10): a loja entrega o token, o servidor valida, o nucleo credita pelo tx do servidor,
    /// a View grava o save e so entao confirma na loja. A View e' so a cola: Recebida (callback da loja), ProximoParaValidar + Resposta (o
    /// servidor), Gravado (depois do save gravado) e ParaConfirmar + Confirmada (acknowledge). Nada aqui conhece SDK, rede ou relogio do
    /// aparelho: `agora` e' o tempo real desde o inicio do app, so para a espera crescente quando o servidor cai. Sem save: a loja reentrega
    /// toda compra sem acknowledge ao abrir o app, e o crédito e' idempotente pelo tx (Sim.Creditar).
    /// </summary>
    public sealed class Loja
    {
        public sealed class Pedido
        {
            public string Token = "", Produto = "", Tx;
            public EstadoCompra Estado;
            /// <summary>Ja confirmada na loja antes (restaurar depois de reinstalar): credita, mas nao confirma de novo.</summary>
            public bool Restaurada;
            /// <summary>O ultimo crédito do nucleo e o motivo da recusa do servidor (para a tela dizer o que houve).</summary>
            public Credito Credito;
            public string Motivo;
            public int Tentativas;
            public double Proxima;
            public bool NoServidor;
        }

        /// <summary>Espera antes de tentar de novo: EsperaMin, dobrando a cada falha, ate EsperaMax (segundos).</summary>
        public const double EsperaMin = 2, EsperaMax = 300;
        /// <summary>Teto de pedidos abertos: callback da loja em loop nao enche a memoria.</summary>
        public const int MaxPedidos = 32;
        public const int MaxToken = 1024;

        readonly Func<Sim> _sim;
        readonly List<Pedido> _p = new List<Pedido>();

        /// <summary>sim: o jogo atual (o Game troca de Sim ao carregar ou excluir os dados).</summary>
        public Loja(Func<Sim> sim) { _sim = sim; }

        public IReadOnlyList<Pedido> Pedidos => _p;

        public Pedido Do(string token) => _p.Find(x => x.Token == token);

        /// <summary>A loja entregou uma compra: nova, reentregue ao abrir o app ou restaurada. pagamentoPendente = a loja ainda espera o
        /// pagamento (boleto, por exemplo): nada vai ao servidor ate ela entregar de novo sem pendencia. null = ignorada (produto fora do
        /// catalogo, token vazio ou grande demais, fila cheia): sem acknowledge, a loja devolve o dinheiro.</summary>
        public Pedido Recebida(string token, string produto, bool pagamentoPendente, bool restaurada, double agora)
        {
            if (string.IsNullOrEmpty(token) || token.Length > MaxToken || Array.Find(Tabelas.Produtos, x => x.Id == produto) == null) return null;
            Pedido p = Do(token);
            if (p != null && p.Produto != produto) return null;
            if (p == null)
            {
                if (_p.Count >= MaxPedidos) return null;
                p = new Pedido { Token = token, Produto = produto, Restaurada = restaurada };
                _p.Add(p);
            }
            if (p.Estado == EstadoCompra.Validando || p.Estado == EstadoCompra.Esperando || p.Estado == EstadoCompra.Falhou)
            {
                p.Estado = pagamentoPendente ? EstadoCompra.Esperando : EstadoCompra.Validando;
                if (!pagamentoPendente && !p.NoServidor) p.Proxima = Math.Min(p.Proxima, agora);
            }
            return p;
        }

        /// <summary>O proximo pedido a mandar ao servidor agora, um por vez (null = nenhum). Fica marcado ate a Resposta.</summary>
        public Pedido ProximoParaValidar(double agora)
        {
            Pedido p = _p.Find(x => x.Estado == EstadoCompra.Validando && !x.NoServidor && x.Proxima <= agora);
            if (p != null) p.NoServidor = true;
            return p;
        }

        /// <summary>O servidor respondeu sobre `token`. Ok credita no nucleo pelo tx do servidor; o resto nunca credita.</summary>
        public void Resposta(string token, Veredito v, string tx, string motivo, double agora)
        {
            Pedido p = Do(token);
            if (p == null || !p.NoServidor) return;
            p.NoServidor = false;
            p.Motivo = motivo;
            switch (v)
            {
                case Veredito.Ok:
                    if (string.IsNullOrWhiteSpace(tx)) goto case Veredito.Indisponivel;
                    p.Tx = tx;
                    p.Credito = _sim().Creditar(tx, p.Produto);
                    bool vale = p.Credito == Credito.Creditado || p.Credito == Credito.Repetido || p.Credito == Credito.JaTem;
                    p.Estado = vale ? EstadoCompra.Gravar : EstadoCompra.Falhou;
                    break;
                case Veredito.Pendente:
                    p.Estado = EstadoCompra.Esperando;
                    break;
                case Veredito.Recusado:
                    p.Estado = EstadoCompra.Recusada;
                    break;
                case Veredito.Indisponivel:
                default:
                    p.Tentativas++;
                    p.Proxima = agora + Math.Min(EsperaMax, EsperaMin * Math.Pow(2, Math.Min(20, p.Tentativas - 1)));
                    break;
            }
        }

        /// <summary>A View gravou o save depois dos creditos: agora (e so agora) eles podem ser confirmados na loja. Restaurada sai sem
        /// confirmar, porque a loja ja tinha confirmado.</summary>
        public void Gravado()
        {
            _p.RemoveAll(x => x.Estado == EstadoCompra.Gravar && x.Restaurada);
            foreach (Pedido p in _p)
                if (p.Estado == EstadoCompra.Gravar) p.Estado = EstadoCompra.Confirmar;
        }

        /// <summary>O que a View confirma (acknowledge/consume) na loja agora.</summary>
        public IEnumerable<Pedido> ParaConfirmar() => _p.FindAll(x => x.Estado == EstadoCompra.Confirmar);

        /// <summary>A loja confirmou: o pedido acabou.</summary>
        public void Confirmada(string token) => _p.RemoveAll(x => x.Token == token && x.Estado == EstadoCompra.Confirmar);

        /// <summary>A tela ja mostrou a recusa ou a falha: some da lista. Sem acknowledge, a loja devolve o dinheiro sozinha.</summary>
        public void Esquecer(string token) =>
            _p.RemoveAll(x => x.Token == token && (x.Estado == EstadoCompra.Recusada || x.Estado == EstadoCompra.Falhou));

        /// <summary>Falhou por teto ou save recusado: tenta de novo (o jogador gastou Gems ou o save voltou).</summary>
        public void TentarDeNovo(string token, double agora)
        {
            Pedido p = Do(token);
            if (p == null || p.Estado != EstadoCompra.Falhou) return;
            p.Estado = EstadoCompra.Validando;
            p.Proxima = agora;
        }
    }
}
