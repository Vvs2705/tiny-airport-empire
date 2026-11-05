// Loja falsa (AMBIENTE=falso): para o PC, o Editor e o K2 antes da Play Console existir. O token diz o que a loja responderia:
//   falso-ok-<qualquer>  falso-pendente-<..>  falso-cancelado-<..>  falso-consumido-<..>  falso-real-<..> (compra que nao e' de teste)
//   falso-erro-<..>      a loja fora do ar (o cliente tenta de novo)
// Qualquer outro token = desconhecido na loja. Toda compra falsa e' "de teste" (purchaseType 0), como a do testador de licenca.
import { hash } from './decide.js';
import { IndisponivelError } from './play.js';

export function lojaFalsa(produto, token) {
  const m = /^falso-(ok|pendente|cancelado|consumido|real|erro)-[A-Za-z0-9._:-]{1,64}$/.exec(token);
  if (!m) return null;
  if (m[1] === 'erro') throw new IndisponivelError('loja falsa fora do ar');
  return {
    productId: produto,
    orderId: 'FALSO.' + hash(token).slice(0, 16),
    purchaseType: m[1] === 'real' ? undefined : 0,
    purchaseState: { pendente: 2, cancelado: 1 }[m[1]] ?? 0,
    consumptionState: m[1] === 'consumido' ? 1 : 0,
    acknowledgementState: 0,
  };
}
