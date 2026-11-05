// Os testes de seguranca e de monetizacao do GDD §16.3 e §16.6 que cabem no servidor.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { sobe } from './apoio.js';
import { IndisponivelError } from '../src/play.js';
import { MAX_REPETICOES } from '../src/decide.js';

const pede = (token, produto = 'gems_pack_small') => ({ produto, token });

test('compra ok devolve o tx e o produto; o mesmo token de novo devolve o mesmo tx (callback repetido)', async () => {
  const s = await sobe();
  const a = await s.post(pede('falso-ok-1'));
  assert.equal(a.status, 200);
  assert.equal(a.corpo.resultado, 'ok');
  assert.equal(a.corpo.produto, 'gems_pack_small');
  assert.match(a.corpo.tx, /^FALSO\.[0-9a-f]{16}$/);
  const b = await s.post(pede('falso-ok-1'));
  assert.deepEqual(b.corpo, a.corpo);
  await s.fecha();
});

test('consumivel reapresentado alem do limite e recusado; nao consumivel continua (restaurar)', async () => {
  const s = await sobe();
  for (let i = 0; i < MAX_REPETICOES; i++) assert.equal((await s.post(pede('falso-ok-x'))).corpo.resultado, 'ok');
  assert.deepEqual((await s.post(pede('falso-ok-x'))).corpo, { resultado: 'recusado', motivo: 'repetido' });
  for (let i = 0; i < MAX_REPETICOES + 3; i++)
    assert.equal((await s.post(pede('falso-ok-y', 'aircraft_skin_blue_01'))).corpo.resultado, 'ok');
  await s.fecha();
});

test('pendente, cancelado, consumido, compra real no ambiente de teste e token que a loja nao conhece', async () => {
  const s = await sobe();
  assert.deepEqual((await s.post(pede('falso-pendente-1'))).corpo, { resultado: 'pendente' });
  assert.deepEqual((await s.post(pede('falso-cancelado-1'))).corpo, { resultado: 'recusado', motivo: 'cancelado' });
  assert.deepEqual((await s.post(pede('falso-consumido-1'))).corpo, { resultado: 'recusado', motivo: 'consumido' });
  assert.deepEqual((await s.post(pede('falso-real-1'))).corpo, { resultado: 'recusado', motivo: 'ambiente' });
  assert.deepEqual((await s.post(pede('GPA-forjado'))).corpo, { resultado: 'recusado', motivo: 'desconhecido' });
  await s.fecha();
});

test('producao recusa compra de teste (nenhuma compra de QA vira credito no build da loja)', async () => {
  const s = await sobe({ ambiente: 'producao' });
  assert.deepEqual((await s.post(pede('falso-ok-1'))).corpo, { resultado: 'recusado', motivo: 'ambiente' });
  assert.equal((await s.post(pede('falso-real-1'))).corpo.resultado, 'ok');
  await s.fecha();
});

test('pedido mal formado: produto desconhecido, token invalido, tipos errados, JSON quebrado, corpo grande, content-type', async () => {
  const s = await sobe();
  assert.deepEqual(await s.post(pede('falso-ok-1', 'gems_pack_giant')), { status: 400, corpo: { erro: 'produto' } });
  assert.deepEqual(await s.post(pede('falso-ok-1', '__proto__')), { status: 400, corpo: { erro: 'produto' } });
  assert.deepEqual(await s.post(pede('')), { status: 400, corpo: { erro: 'token' } });
  assert.deepEqual(await s.post(pede('a b')), { status: 400, corpo: { erro: 'token' } });
  assert.deepEqual(await s.post(pede('x'.repeat(513))), { status: 400, corpo: { erro: 'token' } });
  assert.deepEqual(await s.post({ produto: 'gems_pack_small', token: -1 }), { status: 400, corpo: { erro: 'token' } });
  assert.deepEqual(await s.post({ produto: 'gems_pack_small', token: 'falso-ok-1', quantidade: -5 }).then((r) => r.status), 200, 'campo extra ignorado: quantidade nunca vem do cliente');
  assert.deepEqual(await s.post('null'), { status: 400, corpo: { erro: 'produto' } });
  assert.deepEqual(await s.post('{"produto":'), { status: 400, corpo: { erro: 'json' } });
  assert.deepEqual(await s.post('x'.repeat(5000)), { status: 413, corpo: { erro: 'tamanho' } });
  assert.deepEqual(await s.post(pede('falso-ok-1'), { 'Content-Type': 'text/plain' }), { status: 415, corpo: { erro: 'tipo' } });
  await s.fecha();
});

test('rotas e metodos: so o validar e a saude; nada de painel', async () => {
  const s = await sobe();
  assert.deepEqual(await s.get('/saude'), { status: 200, corpo: { ok: true, ambiente: 'falso' } });
  assert.deepEqual(await s.get('/v1/compras/validar'), { status: 405, corpo: { erro: 'metodo' } });
  for (const r of ['/', '/admin', '/v1/compras', '/v1/compras/validar/../admin'])
    assert.equal((await s.get(r)).status, 404, r);
  assert.equal((await s.post(pede('falso-ok-1'), undefined, '/saude')).status, 405);
  await s.fecha();
});

test('limite de taxa responde 429 sem consultar a loja', async () => {
  let consultas = 0;
  const s = await sobe({ limite: () => false, consulta: async () => (consultas++, null) });
  assert.deepEqual(await s.post(pede('falso-ok-1')), { status: 429, corpo: { erro: 'limite' } });
  assert.equal(consultas, 0);
  await s.fecha();
});

test('kill switch: desligado nao concede nada', async () => {
  const s = await sobe({ desligado: true });
  assert.deepEqual(await s.post(pede('falso-ok-1')), { status: 503, corpo: { erro: 'desligado' } });
  await s.fecha();
});

test('loja ou registro fora do ar: 503 sem detalhe interno, e o cliente tenta de novo', async () => {
  const s = await sobe();
  assert.deepEqual(await s.post(pede('falso-erro-1')), { status: 503, corpo: { erro: 'indisponivel' } });
  const t = await sobe({ registra: async () => { throw new IndisponivelError('firestore 500'); } });
  assert.deepEqual(await t.post(pede('falso-ok-1')), { status: 503, corpo: { erro: 'indisponivel' } });
  const u = await sobe({ consulta: async () => { throw new TypeError('segredo interno em /srv/app.js:12'); } });
  const r = await u.post(pede('falso-ok-1'));
  assert.deepEqual(r, { status: 503, corpo: { erro: 'indisponivel' } });
  assert.ok(!u.logs.join('\n').includes('segredo interno'), 'nem no log');
  await Promise.all([s.fecha(), t.fecha(), u.fecha()]);
});

test('o log nunca tem o token nem o tx', async () => {
  const s = await sobe();
  const token = 'falso-ok-token-que-nao-pode-vazar';
  const r = await s.post(pede(token));
  const log = s.logs.join('\n');
  assert.ok(!log.includes(token));
  assert.ok(!log.includes(r.corpo.tx));
  assert.match(log, /"tok":"[0-9a-f]{12}"/);
  await s.fecha();
});
