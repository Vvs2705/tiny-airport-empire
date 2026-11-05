// Sobe o app numa porta livre e devolve post/get e o log capturado.
import { createServer } from 'node:http';
import { criaApp } from '../src/app.js';
import { lojaFalsa } from '../src/falso.js';
import { registroMemoria } from '../src/registro.js';

export async function sobe(opcoes = {}) {
  const logs = [];
  const app = criaApp({
    ambiente: 'falso',
    consulta: async (p, t) => lojaFalsa(p, t),
    registra: registroMemoria(),
    limite: () => true,
    log: (o) => logs.push(JSON.stringify(o)),
    ...opcoes,
  });
  const srv = createServer(app);
  await new Promise((r) => srv.listen(0, '127.0.0.1', r));
  const base = `http://127.0.0.1:${srv.address().port}`;
  const post = async (corpo, headers = { 'Content-Type': 'application/json' }, rota = '/v1/compras/validar') => {
    const r = await fetch(base + rota, { method: 'POST', headers, body: typeof corpo === 'string' ? corpo : JSON.stringify(corpo) });
    return { status: r.status, corpo: await r.json() };
  };
  const get = async (rota) => {
    const r = await fetch(base + rota);
    return { status: r.status, corpo: await r.json() };
  };
  return { base, post, get, logs, fecha: () => new Promise((r) => srv.close(r)) };
}
