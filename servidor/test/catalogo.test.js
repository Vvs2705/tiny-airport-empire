// O catalogo do servidor e o do nucleo (Tabelas.Produtos em Compras.cs) sao o mesmo: id, ordem e tipo.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { PRODUTOS } from '../src/catalogo.js';

test('catalogo igual ao Tabelas.Produtos do nucleo', () => {
  const cs = readFileSync(new URL('../../client/Assets/_TA/Scripts/Core/Compras.cs', import.meta.url), 'utf8');
  const nucleo = [...cs.matchAll(/new Produto \{ Id = "([^"]+)", Tipo = TipoProduto\.(\w+)/g)].map((m) => [m[1], m[2]]);
  assert.ok(nucleo.length >= 7, 'achou o catalogo no Compras.cs');
  assert.deepEqual(Object.entries(PRODUTOS), nucleo);
});
