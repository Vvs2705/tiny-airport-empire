// Google Play Developer API (purchases.products.get), com o token da conta de servico do Cloud Run pelo servidor de metadados: sem chave
// JSON em lugar nenhum (GDD §7.4, D-06). A conta de servico e' convidada na Play Console com a permissao de ver e gerenciar pedidos.
import { PACOTE } from './catalogo.js';

export class IndisponivelError extends Error {}

const METADADOS = 'http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/default/token';
const ESCOPOS = ['https://www.googleapis.com/auth/androidpublisher', 'https://www.googleapis.com/auth/datastore'];
const PRAZO_MS = 5000;

/** Token de acesso da conta de servico, guardado ate 1 min antes de vencer. fetchFn: troca nos testes. */
export function credencial(fetchFn = fetch, agora = Date.now) {
  let cache = null;
  return async () => {
    if (cache && agora() < cache.vence) return cache.token;
    let r;
    try {
      r = await fetchFn(`${METADADOS}?scopes=${encodeURIComponent(ESCOPOS.join(','))}`, {
        headers: { 'Metadata-Flavor': 'Google' },
        signal: AbortSignal.timeout(PRAZO_MS),
      });
    } catch {
      throw new IndisponivelError('metadados');
    }
    if (!r.ok) throw new IndisponivelError('metadados ' + r.status);
    const j = await r.json();
    cache = { token: j.access_token, vence: agora() + Math.max(0, (j.expires_in ?? 0) - 60) * 1000 };
    return cache.token;
  };
}

/** A compra como a Google ve, ou null se a Google nao conhece o token. Erro de rede, 5xx, 401 e 403 = IndisponivelError (tenta de novo). */
export function playApi(token, fetchFn = fetch) {
  return async (produto, compraToken) => {
    const url =
      'https://androidpublisher.googleapis.com/androidpublisher/v3/applications/' +
      `${PACOTE}/purchases/products/${encodeURIComponent(produto)}/tokens/${encodeURIComponent(compraToken)}`;
    let r;
    try {
      r = await fetchFn(url, { headers: { Authorization: 'Bearer ' + (await token()) }, signal: AbortSignal.timeout(PRAZO_MS) });
    } catch (e) {
      if (e instanceof IndisponivelError) throw e;
      throw new IndisponivelError('play rede');
    }
    if (r.status === 400 || r.status === 404 || r.status === 410) return null;
    if (!r.ok) throw new IndisponivelError('play ' + r.status);
    return await r.json();
  };
}
