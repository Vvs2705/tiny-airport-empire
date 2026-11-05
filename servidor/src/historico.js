// Historico de compras por conta (docs/CONTAS.md): jogadores/{hash do id do jogador}/compras/{hash do token}. So o produto, o ambiente e
// quando: nem o id do jogador nem o token ficam guardados. Apagar a conta (DELETE /v1/conta) apaga a colecao inteira do jogador; o registro
// anti-fraude (compras/{hash do token}, registro.js) fica, porque nao aponta para ninguem.
//
// anota(jogador, id, dados)  lista(jogador) -> [{ produto, ambiente, quando }]  apaga(jogador) -> quantas apagou
import { IndisponivelError } from './play.js';

export const MAX_LISTA = 300;

export function historicoMemoria() {
  const m = new Map();
  return {
    anota: async (jogador, id, dados) => {
      const h = m.get(jogador) ?? new Map();
      h.set(id, { ...dados });
      m.set(jogador, h);
    },
    lista: async (jogador) => [...(m.get(jogador)?.values() ?? [])].slice(0, MAX_LISTA),
    apaga: async (jogador) => {
      const n = m.get(jogador)?.size ?? 0;
      m.delete(jogador);
      return n;
    },
  };
}

export function historicoFirestore(projeto, token, fetchFn = fetch) {
  const raiz = `projects/${projeto}/databases/(default)/documents`;
  const base = `https://firestore.googleapis.com/v1/${raiz}`;
  async function pede(url, opcoes = {}) {
    let r;
    try {
      r = await fetchFn(url, {
        ...opcoes,
        headers: { Authorization: 'Bearer ' + (await token()), 'Content-Type': 'application/json' },
        signal: AbortSignal.timeout(5000),
      });
    } catch (e) {
      if (e instanceof IndisponivelError) throw e;
      throw new IndisponivelError('firestore rede');
    }
    if (r.status === 404) return {};
    if (!r.ok) throw new IndisponivelError('firestore ' + r.status);
    return r.json();
  }
  const commit = (writes) => pede(`${base}:commit`, { method: 'POST', body: JSON.stringify({ writes }) });
  const docs = async (jogador) =>
    (await pede(`${base}/jogadores/${jogador}/compras?pageSize=${MAX_LISTA}`)).documents ?? [];
  return {
    // "quando" e' a hora da compra na loja (purchaseTimeMillis): restaurar regrava o mesmo valor
    anota: (jogador, id, dados) =>
      commit([
        {
          update: {
            name: `${raiz}/jogadores/${jogador}/compras/${id}`,
            fields: Object.fromEntries(Object.entries(dados).map(([k, v]) => [k, { stringValue: String(v) }])),
          },
          updateMask: { fieldPaths: Object.keys(dados) },
        },
      ]),
    lista: async (jogador) =>
      (await docs(jogador)).map((d) => ({
        produto: d.fields?.produto?.stringValue ?? '',
        ambiente: d.fields?.ambiente?.stringValue ?? '',
        quando: d.fields?.quando?.stringValue ?? '',
      })),
    apaga: async (jogador) => {
      let n = 0;
      for (;;) {
        const lote = await docs(jogador);
        if (lote.length === 0) return n;
        await commit(lote.map((d) => ({ delete: d.name })));
        n += lote.length;
        if (lote.length < MAX_LISTA) return n;
      }
    },
  };
}
