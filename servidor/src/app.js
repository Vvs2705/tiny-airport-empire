// O servico HTTP (GDD §7.4, §8.5, docs/CONTAS.md). Sem stack trace na resposta; o id do jogador nunca vai para o log nem para o disco (so o hash).
//
// POST /v1/compras/validar   {"produto": "<id do catalogo>", "token": "<purchaseToken da loja>"}       (JSON, ate 4 KB)
//   Authorization: Bearer <token da Unity Authentication>  opcional; obrigatorio com exigeConta. Com conta, a compra entra no historico dela.
//   200 {"resultado": "ok", "tx": "<id que o nucleo credita>", "produto": ...}
//   200 {"resultado": "pendente"}                                      pagamento ainda nao aprovado na loja
//   200 {"resultado": "recusado", "motivo": "cancelado|consumido|ambiente|produto|desconhecido|repetido"}
//   400 / 404 / 405 / 413 / 415 {"erro": ...}                          pedido mal formado (o cliente nao tenta de novo)
//   401 {"erro": "conta"}                                              token da conta ausente (com exigeConta), vencido ou forjado
//   429 / 503 {"erro": ...}                                            tente de novo mais tarde (com espera crescente)
// GET    /v1/conta/compras   (Authorization obrigatorio)  200 {"compras": [{"produto", "ambiente", "quando"}]}
// DELETE /v1/conta           (Authorization obrigatorio)  200 {"ok": true}: apaga o historico de compras da conta (exclusao de conta)
// GET    /saude
import { PRODUTOS, consumivel } from './catalogo.js';
import { decide, hash, txDe, MAX_REPETICOES } from './decide.js';
import { IndisponivelError } from './play.js';
import { tokenDe } from './conta.js';

export const MAX_CORPO = 4096;
const TOKEN = /^[A-Za-z0-9._:-]{1,512}$/;

/**
 * ambiente: 'falso' | 'teste' | 'producao'. consulta(produto, token) -> compra da loja ou null. registra(id, dados) -> vezes.
 * limite(ip) -> bool. desligado: o kill switch do GDD §15.3 (so desliga, nunca concede). log(obj): uma linha JSON por pedido.
 * conta(token) -> { jogador, provedor } | null (verificador do conta.js), ou null sem conta configurada. exigeConta: compra sem conta = 401.
 * historico: { anota, lista, apaga } (historico.js).
 */
export function criaApp({ ambiente, consulta, registra, limite, desligado = false, log = () => {}, conta = null, exigeConta = false, historico = null }) {
  const aceitaTeste = ambiente !== 'producao';

  /** O hash do jogador do pedido; null = sem conta (permitido); false = recusar com 401. */
  async function jogadorDe(req, obrigatorio) {
    if (!conta) return obrigatorio ? false : null;   // servidor sem conta configurada: o cabecalho e' ignorado
    const t = tokenDe(req);
    if (t === undefined) return obrigatorio ? false : null;
    if (t === null) return false;
    const j = await conta(t);
    return j ? hash(j.jogador) : false;
  }

  async function valida(corpo, jogador) {
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
    if (jogador && historico) {
      const ms = Number(compra.purchaseTimeMillis);
      const quando = Number.isFinite(ms) && ms > 0 ? new Date(ms).toISOString() : new Date().toISOString();
      await historico.anota(jogador, id, { produto, ambiente, quando });
    }
    return [200, { resultado: 'ok', tx, produto }, { ...base, vezes, conta: !!jogador }];
  }

  async function rotaConta(req, url) {
    if (!conta || !historico) return [404, { erro: 'rota' }];
    const lista = url === '/v1/conta/compras';
    if (lista ? req.method !== 'GET' : req.method !== 'DELETE') return [405, { erro: 'metodo' }];
    const jogador = await jogadorDe(req, true);
    if (!jogador) return [401, { erro: 'conta' }];
    if (lista) {
      const compras = (await historico.lista(jogador)).sort((a, b) => (a.quando < b.quando ? 1 : -1));
      return [200, { compras }, { n: compras.length }];
    }
    const n = await historico.apaga(jogador);
    return [200, { ok: true }, { apagou: n }];
  }

  return async function (req, res) {
    const responde = (status, corpo, extra) => {
      log({ severity: status >= 500 ? 'WARNING' : 'INFO', status, ambiente, ...extra, ...corpo, tx: undefined });
      res.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store' });
      res.end(JSON.stringify(corpo));
    };
    const url = (req.url ?? '').split('?')[0];
    if (url === '/saude') return req.method === 'GET' ? responde(200, { ok: true, ambiente }) : responde(405, { erro: 'metodo' });
    const ehConta = url === '/v1/conta' || url === '/v1/conta/compras';
    if (url !== '/v1/compras/validar' && !ehConta) return responde(404, { erro: 'rota' });
    if (!ehConta && req.method !== 'POST') return responde(405, { erro: 'metodo' });
    const ip = String(req.headers['x-forwarded-for'] ?? req.socket?.remoteAddress ?? '').split(',')[0].trim();
    if (!limite(ip)) return responde(429, { erro: 'limite' });
    if (ehConta) {
      try {
        const [status, resposta, extra] = await rotaConta(req, url);
        return responde(status, resposta, extra);
      } catch (e) {
        log({ severity: 'ERROR', motivo: e instanceof IndisponivelError ? e.message : 'interno' });
        return responde(503, { erro: 'indisponivel' });
      }
    }
    let jogador;
    try {
      jogador = await jogadorDe(req, exigeConta);
    } catch (e) {
      log({ severity: 'ERROR', motivo: e instanceof IndisponivelError ? e.message : 'interno' });
      return responde(503, { erro: 'indisponivel' });
    }
    if (jogador === false) return responde(401, { erro: 'conta' });
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
      const [status, resposta, extra] = await valida(corpo, jogador);
      return responde(status, resposta, extra);
    } catch (e) {
      // nunca o stack trace nem a mensagem da loja na resposta; no log so o tipo
      const motivo = e instanceof IndisponivelError ? e.message : 'interno';
      log({ severity: 'ERROR', motivo });
      return responde(503, { erro: 'indisponivel' });
    }
  };
}
