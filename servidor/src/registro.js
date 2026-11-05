// Registro das compras ja vistas (GDD §7.4 passo 6): 1 documento por hash do token, nunca o token. registra(id, dados) devolve quantas
// vezes o token ja foi apresentado, contando esta. Memoria (falso, teste local) ou Firestore pela API REST (teste e producao no Cloud Run).
import { IndisponivelError } from './play.js';

export function registroMemoria() {
  const m = new Map();
  return async (id, dados) => {
    const v = (m.get(id)?.vezes ?? 0) + 1;
    m.set(id, { ...dados, vezes: v });
    return v;
  };
}

/** Uma escrita so (commit com increment): sem ler antes, sem corrida entre duas instancias. */
export function registroFirestore(projeto, token, fetchFn = fetch) {
  const base = `https://firestore.googleapis.com/v1/projects/${projeto}/databases/(default)/documents`;
  return async (id, dados) => {
    const nome = `projects/${projeto}/databases/(default)/documents/compras/${id}`;
    const fields = Object.fromEntries(Object.entries(dados).map(([k, v]) => [k, { stringValue: String(v) }]));
    const corpo = {
      writes: [
        {
          update: { name: nome, fields },
          updateMask: { fieldPaths: Object.keys(dados) },
          updateTransforms: [
            { fieldPath: 'vezes', increment: { integerValue: '1' } },
            { fieldPath: 'ultima', setToServerValue: 'REQUEST_TIME' },
          ],
        },
      ],
    };
    let r;
    try {
      r = await fetchFn(`${base}:commit`, {
        method: 'POST',
        headers: { Authorization: 'Bearer ' + (await token()), 'Content-Type': 'application/json' },
        body: JSON.stringify(corpo),
        signal: AbortSignal.timeout(5000),
      });
    } catch (e) {
      if (e instanceof IndisponivelError) throw e;
      throw new IndisponivelError('firestore rede');
    }
    if (!r.ok) throw new IndisponivelError('firestore ' + r.status);
    const j = await r.json();
    return Number(j.writeResults?.[0]?.transformResults?.[0]?.integerValue ?? NaN);
  };
}
