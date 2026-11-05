// A conta do jogador (docs/CONTAS.md): o token da Unity Authentication conferido no servidor, o historico de compras por conta e a exclusao.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { generateKeyPairSync, sign } from 'node:crypto';
import { sobe } from './apoio.js';
import { EMISSOR, JWKS, chavesRemotas, verificador, tokenDe } from '../src/conta.js';
import { historicoFirestore, historicoMemoria } from '../src/historico.js';
import { hash } from '../src/decide.js';
import { IndisponivelError } from '../src/play.js';

const PROJETO = '4a7c824c-0202-45e6-86c4-b6de3ca3ae98';
const AGORA = 1_800_000_000;
const rsa = generateKeyPairSync('rsa', { modulusLength: 2048 });
const ec = generateKeyPairSync('ec', { namedCurve: 'P-256' });
const outra = generateKeyPairSync('rsa', { modulusLength: 2048 });
const b64 = (o) => Buffer.from(JSON.stringify(o)).toString('base64url');

function jwt(corpo = {}, { alg = 'RS256', kid = 'k1', chave = rsa.privateKey } = {}) {
  const c = {
    iss: EMISSOR,
    sub: 'jogador-1',
    aud: ['idd:x', 'envName:production', 'upid:' + PROJETO],
    project_id: PROJETO,
    envName: 'production',
    sign_in_provider: 'google-play-games',
    exp: AGORA + 3600,
    nbf: AGORA - 10,
    ...corpo,
  };
  const dados = b64({ alg, kid, typ: 'JWT' }) + '.' + b64(c);
  const s =
    alg === 'ES256'
      ? sign('sha256', Buffer.from(dados), { key: chave, dsaEncoding: 'ieee-p1363' })
      : sign('RSA-SHA256', Buffer.from(dados), chave);
  return dados + '.' + s.toString('base64url');
}

const chaves = async (kid) =>
  ({ k1: { chave: rsa.publicKey, alg: 'RS256' }, e1: { chave: ec.publicKey, alg: 'ES256' } })[kid] ?? null;
const ver = (o = {}) => verificador({ projeto: PROJETO, chaves, agora: () => AGORA, ...o });

test('token bom da conta: devolve o jogador e o provedor (RS256 e ES256)', async () => {
  assert.deepEqual(await ver()(jwt()), { jogador: 'jogador-1', provedor: 'google-play-games' });
  assert.deepEqual((await ver()(jwt({}, { alg: 'ES256', kid: 'e1', chave: ec.privateKey }))).jogador, 'jogador-1');
  assert.ok(await ver({ ambiente: 'production' })(jwt()));
  assert.ok(await ver()(jwt({ project_id: undefined })), 'o projeto pelo upid do aud');
});

test('token forjado, vencido, de outro projeto, ambiente ou emissor: recusado', async () => {
  const v = ver();
  assert.equal(await v(jwt({}, { chave: outra.privateKey })), null, 'assinado por outra chave');
  assert.equal(await v(jwt({}, { kid: 'nao-existe' })), null);
  assert.equal(await v(jwt({ exp: AGORA - 120 })), null, 'vencido');
  assert.ok(await v(jwt({ exp: AGORA - 30 })), 'folga de 60 s para o relogio');
  assert.equal(await v(jwt({ nbf: AGORA + 600 })), null, 'ainda nao vale');
  assert.equal(await v(jwt({ project_id: 'outro', aud: ['upid:outro'] })), null, 'outro projeto da UGS');
  assert.equal(await v(jwt({ iss: 'https://falso.example' })), null);
  assert.equal(await v(jwt({ sub: '' })), null);
  assert.equal(await ver({ ambiente: 'production' })(jwt({ envName: 'development', aud: ['upid:' + PROJETO] })), null);
  // alg trocado: o cabecalho diz ES256 numa chave RS256 (confusao de algoritmo)
  assert.equal(await v(jwt({}, { alg: 'ES256', kid: 'k1', chave: ec.privateKey })), null);
  const [c, p] = jwt().split('.');
  assert.equal(await v(b64({ alg: 'none', kid: 'k1' }) + '.' + p + '.'), null, 'alg none');
  assert.equal(await v(c + '.' + p), null);
  assert.equal(await v('lixo'), null);
  assert.equal(await v('a.b.c'), null);
  assert.equal(await v('x'.repeat(5000)), null);
  // corpo trocado depois de assinado
  const [h, , s] = jwt().split('.');
  const mexido = b64({ iss: EMISSOR, sub: 'outro', project_id: PROJETO, exp: AGORA + 3600 });
  assert.equal(await v(h + '.' + mexido + '.' + s), null);
  assert.throws(() => verificador({ chaves }), /projeto/);
});

test('chaves remotas: busca o JWKS, guarda 1 h, rebusca kid novo no maximo 1x por minuto e sem rede nunca aceita', async () => {
  const jwk = { ...rsa.publicKey.export({ format: 'jwk' }), kid: 'k1', alg: 'RS256', use: 'sig' };
  let t = 0;
  const urls = [];
  let rede = true;
  const f = async (u) => {
    urls.push(u);
    if (!rede) throw new Error('fora');
    return { ok: true, status: 200, json: async () => ({ keys: [jwk, { kid: 'ruim', kty: 'XYZ' }] }) };
  };
  const c = chavesRemotas(f, () => t);
  assert.ok(await c('k1'));
  assert.equal(urls[0], JWKS);
  assert.ok(await c('k1'));
  assert.equal(urls.length, 1, 'guardou');
  assert.equal(await c('outro'), null);
  assert.equal(urls.length, 1, 'kid novo antes de 1 min nao rebusca');
  t = 61_000;
  assert.equal(await c('outro'), null);
  assert.equal(urls.length, 2);
  rede = false;
  t = 4000_000;
  assert.ok(await c('k1'), 'sem rede, a chave guardada continua valendo');
  await assert.rejects(chavesRemotas(f, () => 0)('k1'), IndisponivelError);
  const v = verificador({ projeto: PROJETO, chaves: chavesRemotas(async () => ({ ok: true, json: async () => ({ keys: [jwk] }) })), agora: () => AGORA });
  assert.equal((await v(jwt())).jogador, 'jogador-1');
});

test('tokenDe: Bearer bem formado, ausente ou mal formado', () => {
  assert.equal(tokenDe({ headers: {} }), undefined);
  assert.equal(tokenDe({ headers: { authorization: 'Bearer abc.def-_g' } }), 'abc.def-_g');
  assert.equal(tokenDe({ headers: { authorization: 'Basic x' } }), null);
  assert.equal(tokenDe({ headers: { authorization: 'Bearer a b' } }), null);
});

const comConta = (extra = {}) => ({ conta: ver(), historico: historicoMemoria(), ...extra });
const auth = (t = jwt()) => ({ 'Content-Type': 'application/json', Authorization: 'Bearer ' + t });
const pede = (token, produto = 'gems_pack_small') => ({ produto, token });

test('compra com conta entra no historico dela; sem conta continua valendo; token ruim = 401', async () => {
  const s = await sobe(comConta());
  assert.equal((await s.post(pede('falso-ok-1'), auth())).corpo.resultado, 'ok');
  assert.equal((await s.post(pede('falso-ok-2', 'aircraft_skin_blue_01'), auth())).corpo.resultado, 'ok');
  assert.equal((await s.post(pede('falso-ok-3'))).corpo.resultado, 'ok', 'sem conta: compra anonima');
  assert.deepEqual((await s.post(pede('falso-ok-4'), auth(jwt({ exp: 1 })))).status, 401);
  assert.deepEqual((await s.post(pede('falso-ok-4'), { 'Content-Type': 'application/json', Authorization: 'x' })).corpo, { erro: 'conta' });
  const r = await fetch(s.base + '/v1/conta/compras', { headers: auth() });
  const h = await r.json();
  assert.equal(r.status, 200);
  assert.deepEqual(h.compras.map((c) => c.produto).sort(), ['aircraft_skin_blue_01', 'gems_pack_small']);
  assert.ok(h.compras.every((c) => c.ambiente === 'falso' && c.quando));
  const outra = await fetch(s.base + '/v1/conta/compras', { headers: auth(jwt({ sub: 'jogador-2' })) });
  assert.deepEqual((await outra.json()).compras, [], 'cada conta ve so as suas');
  assert.ok(!s.logs.join('\n').includes('jogador-1'), 'o id do jogador nao vai para o log');
  assert.ok(!s.logs.join('\n').includes(hash('jogador-1')), 'nem o hash dele');
  await s.fecha();
});

test('exigeConta: compra sem token = 401 e nada consultado na loja', async () => {
  let consultas = 0;
  const s = await sobe(comConta({ exigeConta: true, consulta: async () => (consultas++, null) }));
  assert.equal((await s.post(pede('falso-ok-1'))).status, 401);
  assert.equal(consultas, 0);
  assert.equal((await s.post(pede('falso-ok-1'), auth())).status, 200);
  await s.fecha();
});

test('DELETE /v1/conta apaga o historico da conta; rotas pedem o token e o metodo certo', async () => {
  const s = await sobe(comConta());
  await s.post(pede('falso-ok-1'), auth());
  await s.post(pede('falso-ok-9'), auth(jwt({ sub: 'jogador-2' })));
  assert.equal((await fetch(s.base + '/v1/conta', { method: 'DELETE' })).status, 401);
  assert.equal((await fetch(s.base + '/v1/conta', { method: 'GET', headers: auth() })).status, 405);
  assert.equal((await fetch(s.base + '/v1/conta/compras', { method: 'POST', headers: auth() })).status, 405);
  const d = await fetch(s.base + '/v1/conta', { method: 'DELETE', headers: auth() });
  assert.deepEqual([d.status, await d.json()], [200, { ok: true }]);
  const h = await (await fetch(s.base + '/v1/conta/compras', { headers: auth() })).json();
  assert.deepEqual(h.compras, []);
  const h2 = await (await fetch(s.base + '/v1/conta/compras', { headers: auth(jwt({ sub: 'jogador-2' })) })).json();
  assert.equal(h2.compras.length, 1, 'a outra conta nao foi tocada');
  // apagar a conta nao reabre o token: o registro anti-fraude fica
  assert.equal((await s.post(pede('falso-ok-1'), auth())).corpo.resultado, 'ok');
  await s.fecha();
});

test('servidor sem conta configurada: rotas de conta 404 e o cabecalho e ignorado', async () => {
  const s = await sobe();
  assert.equal((await fetch(s.base + '/v1/conta/compras', { headers: auth() })).status, 404);
  assert.equal((await s.post(pede('falso-ok-1'), auth())).corpo.resultado, 'ok');
  await s.fecha();
});

test('historico no Firestore: anota no caminho do jogador, lista e apaga em lote', async () => {
  const pedidos = [];
  let docs = [{ name: 'projects/p/databases/(default)/documents/jogadores/h/compras/a', fields: { produto: { stringValue: 'gems_pack_small' }, ambiente: { stringValue: 'teste' }, quando: { stringValue: '2026-11-05T00:00:00.000Z' } } }];
  const f = async (u, o) => {
    pedidos.push([u, o.method ?? 'GET', o.body ? JSON.parse(o.body) : null]);
    if (u.endsWith(':commit')) {
      if (JSON.parse(o.body).writes[0].delete) docs = [];
      return { ok: true, status: 200, json: async () => ({}) };
    }
    return { ok: true, status: 200, json: async () => (docs.length ? { documents: docs } : {}) };
  };
  const h = historicoFirestore('p', async () => 'T', f);
  await h.anota('h', 'a', { produto: 'gems_pack_small', ambiente: 'teste', quando: 'x' });
  assert.equal(pedidos[0][2].writes[0].update.name, 'projects/p/databases/(default)/documents/jogadores/h/compras/a');
  assert.deepEqual(await h.lista('h'), [{ produto: 'gems_pack_small', ambiente: 'teste', quando: '2026-11-05T00:00:00.000Z' }]);
  assert.equal(await h.apaga('h'), 1);
  assert.deepEqual(await h.lista('h'), []);
  await assert.rejects(historicoFirestore('p', async () => 'T', async () => ({ ok: false, status: 500 })).lista('h'), IndisponivelError);
  await assert.rejects(historicoFirestore('p', async () => 'T', async () => { throw new Error('x'); }).lista('h'), IndisponivelError);
});
