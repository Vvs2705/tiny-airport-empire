#if TA_IAP
// Compila so com o Unity IAP 5 no projeto: o TA.asmdef liga TA_IAP por versionDefines quando o pacote com.unity.purchasing entra
// (docs/LOJA_SERVIDOR.md §5, rodada do PC). Sem o pacote, este arquivo nao existe para o compilador e o jogo segue sem loja.
using System.Collections.Generic;
using System.Text;
using TA.Core;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Purchasing;

namespace TA
{
    /// <summary>
    /// Cola entre o Unity IAP, o servidor de validacao (servidor/) e o nucleo (Loja.cs), GDD §7.4. A View so repassa: o callback da loja vira
    /// Loja.Recebida, o servidor vira Loja.Resposta, o save gravado vira Loja.Gravado e so entao a loja confirma (ConfirmPurchase). O token
    /// nunca vai para o disco nem para o log. Chamar Tick() no Update do Game.
    /// </summary>
    public sealed class LojaIap
    {
        readonly Loja _loja;
        readonly LojaConfig _cfg;
        readonly System.Func<bool> _grava;
        readonly StoreController _store;
        readonly Dictionary<string, PendingOrder> _ordens = new Dictionary<string, PendingOrder>();
        UnityWebRequest _web;
        Loja.Pedido _noServidor;

        public bool Pronta { get; private set; }
        public readonly Dictionary<string, Product> Produtos = new Dictionary<string, Product>();
        /// <summary>A tela mostra: cancelada pelo jogador, produto indisponivel, falha da loja.</summary>
        public event System.Action<string, string> Falhou;

        /// <summary>grava: grava o save e diz se gravou (Game.Grava). Sem gravar, nada e' confirmado.</summary>
        public LojaIap(Loja loja, LojaConfig cfg, System.Func<bool> grava)
        {
            _loja = loja;
            _cfg = cfg;
            _grava = grava;
            _store = UnityIAPServices.StoreController();
            _store.OnProductsFetched += ps => { foreach (Product p in ps) Produtos[p.definition.id] = p; Pronta = true; _store.FetchPurchases(); };
            _store.OnProductsFetchFailed += f => Pronta = false;
            _store.OnPurchasePending += o => Recebe(o, false);
            _store.OnPurchaseDeferred += o => Pendente(o);
            _store.OnPurchaseFailed += f => Falhou?.Invoke(Id(f), f.FailureReason.ToString());
            _store.OnPurchasesFetched += Restaura;
        }

        /// <summary>Conecta e busca o catalogo fixo do nucleo (preco, moeda e texto vem da loja, GDD §7.1).</summary>
        public async void Conecta()
        {
            await _store.Connect();
            var defs = new List<ProductDefinition>();
            foreach (Produto p in Tabelas.Produtos)
                defs.Add(new ProductDefinition(p.Id, p.Tipo == TipoProduto.Consumivel ? ProductType.Consumable : ProductType.NonConsumable));
            _store.FetchProducts(defs);
        }

        public void Compra(string produto)
        {
            if (Pronta && Produtos.TryGetValue(produto, out Product p)) _store.PurchaseProduct(p);
            else Falhou?.Invoke(produto, "indisponivel");
        }

        /// <summary>Restaurar compras (GDD §7.3): a loja reentrega o que o jogador tem.</summary>
        public void Restaurar() => _store.FetchPurchases();

        static string Id(Order o) => o.CartOrdered.Items()[0].Product.definition.id;
        static string Id(FailedOrder o) => o.CartOrdered.Items()[0].Product.definition.id;

        void Recebe(PendingOrder o, bool restaurada)
        {
            string token = o.Info.TransactionID;
            if (_loja.Recebida(token, Id(o), false, restaurada, Time.realtimeSinceStartupAsDouble) != null) _ordens[token] = o;
        }

        void Pendente(DeferredOrder o) => _loja.Recebida(o.Info.TransactionID, Id(o), true, false, Time.realtimeSinceStartupAsDouble);

        /// <summary>Ao conectar e no Restaurar: nao consumivel que a conta tem e o save nao (reinstalou) volta pelo servidor, sem confirmar de novo.</summary>
        void Restaura(Orders os)
        {
            foreach (PendingOrder o in os.PendingOrders) Recebe(o, false);
            foreach (ConfirmedOrder o in os.ConfirmedOrders)
            {
                Produto p = System.Array.Find(Tabelas.Produtos, x => x.Id == Id(o));
                if (p != null && p.Tipo == TipoProduto.NaoConsumivel)
                    _loja.Recebida(o.Info.TransactionID, p.Id, false, true, Time.realtimeSinceStartupAsDouble);
            }
        }

        public void Tick()
        {
            double agora = Time.realtimeSinceStartupAsDouble;
            if (_web != null && _web.isDone) Responde(agora);
            if (_web == null && _cfg.TemServidor)
            {
                _noServidor = _loja.ProximoParaValidar(agora);
                if (_noServidor != null) Envia(_noServidor);
            }
            bool gravar = false;
            foreach (Loja.Pedido p in _loja.Pedidos) gravar |= p.Estado == EstadoCompra.Gravar;
            if (gravar && _grava()) _loja.Gravado();
            foreach (Loja.Pedido p in _loja.ParaConfirmar())
            {
                if (_ordens.TryGetValue(p.Token, out PendingOrder o)) { _store.ConfirmPurchase(o); _ordens.Remove(p.Token); }
                _loja.Confirmada(p.Token);
            }
        }

        [System.Serializable] sealed class Pedido { public string produto, token; }
        [System.Serializable] sealed class Resposta { public string resultado = "", tx = "", motivo = ""; }

        void Envia(Loja.Pedido p)
        {
            byte[] corpo = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Pedido { produto = p.Produto, token = p.Token }));
            _web = new UnityWebRequest(_cfg.servidor.TrimEnd('/') + "/v1/compras/validar", "POST")
            {
                uploadHandler = new UploadHandlerRaw(corpo),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 15,
            };
            _web.SetRequestHeader("Content-Type", "application/json");
            _web.SendWebRequest();
        }

        /// <summary>200 com resultado = o veredito; 4xx de pedido mal formado = recusado; rede, 429 e 5xx = tenta de novo depois.</summary>
        void Responde(double agora)
        {
            UnityWebRequest w = _web;
            _web = null;
            string token = _noServidor.Token;
            long st = w.responseCode;
            Resposta r = null;
            if (w.result == UnityWebRequest.Result.Success && st == 200)
                try { r = JsonUtility.FromJson<Resposta>(w.downloadHandler.text); } catch (System.ArgumentException) { }
            w.Dispose();
            Veredito v = r == null ? (st >= 400 && st < 500 && st != 429 ? Veredito.Recusado : Veredito.Indisponivel)
                : r.resultado == "ok" ? Veredito.Ok : r.resultado == "pendente" ? Veredito.Pendente : r.resultado == "recusado" ? Veredito.Recusado : Veredito.Indisponivel;
            _loja.Resposta(token, v, r?.tx, r?.motivo, agora);
        }
    }
}
#endif
