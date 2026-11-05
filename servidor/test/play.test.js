import { test } from 'node:test';
import assert from 'node:assert/strict';
import { credencial, playApi, IndisponivelError } from '../src/play.js';
import { registroFirestore, registroMemoria } from '../src/registro.js';
import { limitador } from '../src/limite.js';
import { configura } from '../src/index.js';

const resp = (status, corpo) => ({ ok: status < 300, status, json: async () => corpo });

test('credencial: pede ao servidor de metadados com os escopos e guarda ate vencer', async () => {
  const urls = [];
  let t = 0;
  const pega = credencial(async (u, o) => (urls.push([u, o.headers]), resp(200, { access_token: 'A' + urls.length, expires_in: 3600 })), () => t);
  assert.equal(await pega(), 'A1');
  assert.equal(await pega(), 'A1');
  t = 3600 * 1000;
  assert.equal(await pega(), 'A2');
  assert.match(urls[0][0], /^http:\/\/metadata\.google\.internal\/.*scopes=.*androidpublisher/);
  assert.deepEqual(urls[0][1], { 'Metadata-Flavor': 'Google' });
  await assert.rejects(credencial(async () => resp(500, {}))(), IndisponivelError);
  await assert.rejects(credencial(async () => { throw new Error('rede'); })(), IndisponivelError);
});

test('playApi: url da compra do pacote, 404/410/400 = desconhecido, 5xx/401/403/rede = indisponivel', async () => {
  let url, auth;
  const api = (status) => playApi(async () => 'TK', async (u, o) => ((url = u), (auth = o.headers.Authorization), resp(status, { purchaseState: 0 })));
  assert.deepEqual(await api(200)('gems_pack_small', 'a.b/c'), { purchaseState: 0 });
  assert.equal(url, 'https://androidpublisher.googleapis.com/androidpublisher/v3/applications/br.com.vstack.littleterminal/purchases/products/gems_pack_small/tokens/a.b%2Fc');
  assert.equal(auth, 'Bearer TK');
  for (const s of [400, 404, 410]) assert.equal(await api(s)('gems_pack_small', 't'), null, String(s));
  for (const s of [401, 403, 429, 500, 503]) await assert.rejects(api(s)('gems_pack_small', 't'), IndisponivelError, String(s));
  await assert.rejects(playApi(async () => 'TK', async () => { throw new Error('x'); })('gems_pack_small', 't'), IndisponivelError);
});

test('registro: memoria conta; Firestore faz 1 commit com increment e devolve a contagem', async () => {
  const m = registroMemoria();
  assert.equal(await m('a', {}), 1);
  assert.equal(await m('a', {}), 2);
  assert.equal(await m('b', {}), 1);
  let pedido;
  const f = registroFirestore('meu-proj', async () => 'TK', async (u, o) => ((pedido = [u, JSON.parse(o.body)]), resp(200, { writeResults: [{ transformResults: [{ integerValue: '3' }] }] })));
  assert.equal(await f('abc', { produto: 'gems_pack_small', tx: 'h' }), 3);
  assert.equal(pedido[0], 'https://firestore.googleapis.com/v1/projects/meu-proj/databases/(default)/documents:commit');
  const w = pedido[1].writes[0];
  assert.equal(w.update.name, 'projects/meu-proj/databases/(default)/documents/compras/abc');
  assert.deepEqual(w.updateMask.fieldPaths, ['produto', 'tx']);
  assert.deepEqual(w.updateTransforms[0], { fieldPath: 'vezes', increment: { integerValue: '1' } });
  await assert.rejects(registroFirestore('p', async () => 'TK', async () => resp(500, {}))('a', {}), IndisponivelError);
});

test('limite: N por minuto por IP, reposto com o tempo, IPs separados', () => {
  let t = 0;
  const l = limitador(3, () => t);
  assert.deepEqual([l('a'), l('a'), l('a'), l('a'), l('b')], [true, true, true, false, true]);
  t = 20000;
  assert.equal(l('a'), true);
  assert.equal(l('a'), false);
});

test('configura: ambiente obrigatorio, producao exige Firestore, falso nunca fala com a Google', async () => {
  assert.throws(() => configura({}), /AMBIENTE/);
  assert.throws(() => configura({ AMBIENTE: 'prod' }), /AMBIENTE/);
  assert.throws(() => configura({ AMBIENTE: 'producao' }), /PROJETO/);
  const c = configura({ AMBIENTE: 'falso', DESLIGADO: '1' });
  assert.equal(c.desligado, true);
  assert.equal((await c.consulta('gems_pack_small', 'falso-ok-1')).purchaseState, 0);
  assert.equal(await c.consulta('gems_pack_small', 'GPA.real'), null);
});
