// A regra de validacao (GDD §7.4 passo 5, §8.2, §16.3, §16.6), pura: recebe a compra como a Google Play Developer API a devolve
// (purchases.products.get) e diz se o cliente pode creditar. Quem credita e' o nucleo, pelo id da tabela; aqui so se decide.
import { createHash } from 'node:crypto';
import { consumivel } from './catalogo.js';

// Consumivel apresentado mais vezes que isto sem ser consumido = abuso (o cliente legitimo repete so ate gravar e confirmar).
export const MAX_REPETICOES = 5;

export const hash = (s) => createHash('sha256').update(s, 'utf8').digest('hex');

/** Id de transacao que o nucleo credita (Sim.Creditar): o orderId da loja; sem ele (codigo promocional), o hash do token. */
export const txDe = (compra, token) => compra.orderId || 'tok.' + hash(token).slice(0, 32);

/**
 * compra: o ProductPurchase da Google (ou o da loja falsa). aceitaTeste: o ambiente aceita compra de teste (purchaseType 0) e so ela.
 * Devolve { resultado: 'ok'|'pendente'|'recusado', motivo? }, sem olhar o registro (o app.js conta as repeticoes depois).
 */
export function decide(compra, produto, aceitaTeste) {
  if (!compra || typeof compra !== 'object') return { resultado: 'recusado', motivo: 'desconhecido' };
  if (compra.productId && compra.productId !== produto) return { resultado: 'recusado', motivo: 'produto' };
  const teste = compra.purchaseType === 0;
  if (teste !== aceitaTeste) return { resultado: 'recusado', motivo: 'ambiente' };
  if (compra.purchaseState === 2) return { resultado: 'pendente' };
  if (compra.purchaseState !== 0) return { resultado: 'recusado', motivo: 'cancelado' };
  if (consumivel(produto) && compra.consumptionState === 1) return { resultado: 'recusado', motivo: 'consumido' };
  return { resultado: 'ok' };
}
