// O servico HTTP (GDD §7.4, §8.5): POST /v1/compras/validar e GET /saude. Sem estado de jogador, sem login, sem stack trace na resposta.
//
// Pedido:   {"produto": "<id do catalogo>", "token": "<purchaseToken da loja>"}       (JSON, ate 4 KB)
// Resposta: 200 {"resultado": "ok", "tx": "<id que o nucleo credita>", "produto": ...}
//           200 {"resultado": "pendente"}                                      pagamento ainda nao aprovado na loja
//           200 {"resultado": "recusado", "motivo": "cancelado|consumido|ambiente|produto|desconhecido|repetido"}
//           400 / 404 / 405 / 413 / 415 {"erro": ...}                          pedido mal formado (o cliente nao tenta de novo)
//           429 / 503 {"erro": ...}                                            tente de novo mais tarde (com espera crescente)
import { PRODUTOS, consumivel } from './catalogo.js';
import { decide, hash, txDe, MAX_REPETICOES } from './decide.js';
import { IndisponivelError } from './play.js';

export const MAX_CORPO = 4096;
const TOKEN = /^[A-Za-z0-9._:-]{1,512}$/;

/**
 * ambiente: 'falso' | 'teste' | 'producao'. consulta(produto, token) -> compra da loja ou null. registra(id, dados) -> vezes.
 * limite(ip) -> bool. desligado: o kill switch do GDD §15.3 (so desliga, nunca concede). log(obj): uma linha JSON por pedido.
 */
export function criaApp({ ambiente, consulta, registra, limite, desligado = false, log = () => {} }) {
  const aceitaTeste = ambiente !== 'producao';

  async function valida(corpo) {
    const { produto, token } = corpo ?? {};
    if (typeof produto !== 'string' || !Object.hasOwn(PRODUTOS, produto)) return [400, { erro: 'produto' }];
    if (typeof token !== 'string' || !TOKEN.test(token)) return [400, { erro: 'token' }];
    const id = hash(token);
    const base = { tok: id.slice(0, 12), produto };
    if (desligado) return [503, { erro: 'desligado' }, base];
    const compra = await consulta(produto, token);
    if (!compra) return [200, { resultado: 'recusado', motivo: 'desconhecido' }, base];
    const d = decide(compra, produto, aceitaTeste);
    if (d.resultado !== 'ok') return [200, d, base];
    const tx = txDe(compra, token);
    const vezes = await registra(id, { produto, tx: hash(tx).slice(0, 32), ambiente });
    if (consumivel(produto) && vezes > MAX_REPETICOES) return [200, { resultado: 'recusado', motivo: 'repetido' }, { ...base, vezes }];
    return [200, { resultado: 'ok', tx, produto }, { ...base, vezes }];
  }

  return async function (req, res) {
    const responde = (status, corpo, extra) => {
      log({ severity: status >= 500 ? 'WARNING' : 'INFO', status, ambiente, ...extra, ...corpo, tx: undefined });
      res.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store' });
      res.end(JSON.stringify(corpo));
    };
    const url = (req.url ?? '').split('?')[0];
    if (url === '/saude') return req.method === 'GET' ? responde(200, { ok: true, ambiente }) : responde(405, { erro: 'metodo' });
    if (url !== '/v1/compras/validar') return responde(404, { erro: 'rota' });
    if (req.method !== 'POST') return responde(405, { erro: 'metodo' });
    const ip = String(req.headers['x-forwarded-for'] ?? req.socket?.remoteAddress ?? '').split(',')[0].trim();
    if (!limite(ip)) return responde(429, { erro: 'limite' });
    if (!/^application\/json\b/i.test(req.headers['content-type'] ?? '')) return responde(415, { erro: 'tipo' });

    const partes = [];
    let tamanho = 0;
    try {
      for await (const p of req) {
        tamanho += p.length;
        if (tamanho > MAX_CORPO) return responde(413, { erro: 'tamanho' });
        partes.push(p);
      }
    } catch {
      return responde(400, { erro: 'corpo' });
    }
    let corpo;
    try {
      corpo = JSON.parse(Buffer.concat(partes).toString('utf8'));
    } catch {
      return responde(400, { erro: 'json' });
    }
    try {
      const [status, resposta, extra] = await valida(corpo);
      return responde(status, resposta, extra);
    } catch (e) {
      // nunca o stack trace nem a mensagem da loja na resposta; no log so o tipo
      const motivo = e instanceof IndisponivelError ? e.message : 'interno';
      log({ severity: 'ERROR', motivo });
      return responde(503, { erro: 'indisponivel' });
    }
  };
}
