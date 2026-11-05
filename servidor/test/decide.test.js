import { test } from 'node:test';
import assert from 'node:assert/strict';
import { decide, txDe } from '../src/decide.js';

const compra = (m = {}) => ({ purchaseState: 0, purchaseType: 0, consumptionState: 0, orderId: 'GPA.1', ...m });

test('compra de teste paga vale so onde o ambiente aceita teste', () => {
  assert.deepEqual(decide(compra(), 'gems_pack_small', true), { resultado: 'ok' });
  assert.deepEqual(decide(compra(), 'gems_pack_small', false), { resultado: 'recusado', motivo: 'ambiente' });
  assert.deepEqual(decide(compra({ purchaseType: undefined }), 'gems_pack_small', false), { resultado: 'ok' });
  assert.deepEqual(decide(compra({ purchaseType: undefined }), 'gems_pack_small', true), { resultado: 'recusado', motivo: 'ambiente' });
  assert.deepEqual(decide(compra({ purchaseType: 1 }), 'gems_pack_small', false), { resultado: 'ok' }, 'codigo promocional e real');
});

test('pendente, cancelado/reembolsado, consumido e produto trocado', () => {
  assert.deepEqual(decide(compra({ purchaseState: 2 }), 'gems_pack_small', true), { resultado: 'pendente' });
  assert.deepEqual(decide(compra({ purchaseState: 1 }), 'gems_pack_small', true), { resultado: 'recusado', motivo: 'cancelado' });
  assert.deepEqual(decide(compra({ purchaseState: 7 }), 'gems_pack_small', true), { resultado: 'recusado', motivo: 'cancelado' });
  assert.deepEqual(decide(compra({ consumptionState: 1 }), 'gems_pack_small', true), { resultado: 'recusado', motivo: 'consumido' });
  assert.deepEqual(decide(compra({ productId: 'gems_pack_small' }), 'gems_pack_large', true), { resultado: 'recusado', motivo: 'produto' });
  assert.deepEqual(decide(null, 'gems_pack_small', true), { resultado: 'recusado', motivo: 'desconhecido' });
});

test('nao consumivel ja confirmado volta ok: e a restauracao depois de reinstalar', () => {
  const c = compra({ acknowledgementState: 1, consumptionState: 1 });
  assert.deepEqual(decide(c, 'aircraft_skin_blue_01', true), { resultado: 'ok' });
});

test('tx: o orderId; sem ele, o hash do token (nunca o token)', () => {
  assert.equal(txDe(compra(), 'abc'), 'GPA.1');
  const t = txDe(compra({ orderId: undefined }), 'token-secreto');
  assert.match(t, /^tok\.[0-9a-f]{32}$/);
  assert.ok(!t.includes('token-secreto'));
});
