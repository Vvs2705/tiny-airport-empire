// Conta do jogador (docs/CONTAS.md): o token de acesso da Unity Authentication (JWT RS256/ES256), conferido com a chave publica da Unity,
// sem dependencia de terceiros. Reaproveitavel: nada aqui e' do Little Terminal; o jogo passa o id do projeto da UGS.
//
// O que confere: assinatura pela chave do JWKS (kid), alg RS256 ou ES256, emissor, projeto (project_id ou upid:<id> no aud), exp e nbf
// com 60 s de folga, e o ambiente da UGS quando configurado. Devolve { jogador, provedor } ou null. O id do jogador nunca vai para o log:
// quem chama guarda so o hash (hash() do decide.js).
import { createPublicKey, verify } from 'node:crypto';
import { IndisponivelError } from './play.js';

export const EMISSOR = 'https://player-auth.services.api.unity.com';
export const JWKS = `${EMISSOR}/.well-known/jwks.json`;
const FOLGA = 60;
const MAX_TOKEN = 4096;

const b64 = (s) => Buffer.from(s, 'base64url');
const json = (s) => JSON.parse(b64(s).toString('utf8'));

/**
 * chaves(kid) -> KeyObject | null. Busca o JWKS 1x por hora; kid desconhecido busca de novo, no maximo 1x por minuto (a Unity girou a
 * chave). Rede fora com o cache vazio: IndisponivelError (o cliente tenta de novo; nunca aceita sem conferir).
 */
export function chavesRemotas(fetchFn = fetch, agora = () => Date.now()) {
  let cache = new Map();
  let buscouEm = -Infinity;
  async function busca() {
    buscouEm = agora();
    let r;
    try {
      r = await fetchFn(JWKS, { signal: AbortSignal.timeout(5000) });
    } catch {
      throw new IndisponivelError('jwks rede');
    }
    if (!r.ok) throw new IndisponivelError('jwks ' + r.status);
    const { keys } = await r.json();
    const novo = new Map();
    for (const k of keys ?? []) {
      try {
        novo.set(k.kid, { chave: createPublicKey({ key: k, format: 'jwk' }), alg: k.alg });
      } catch {
        // chave que o node nao entende: ignorada
      }
    }
    cache = novo;
  }
  return async (kid) => {
    const velho = agora() - buscouEm;
    if (velho > 3600_000 || (!cache.has(kid) && velho > 60_000)) {
      try {
        await busca();
      } catch (e) {
        if (cache.size === 0) throw e;
      }
    }
    return cache.get(kid) ?? null;
  };
}

/**
 * projeto: o id do projeto da UGS (obrigatorio). ambiente: o nome do ambiente da UGS exigido (ex.: 'production'), ou vazio para nao conferir.
 * chaves: chavesRemotas() ou um falso nos testes. agora: segundos unix.
 */
export function verificador({ projeto, ambiente = '', chaves, emissor = EMISSOR, agora = () => Date.now() / 1000 }) {
  if (!projeto) throw new Error('verificador sem o projeto da UGS');
  return async (token) => {
    if (typeof token !== 'string' || token.length > MAX_TOKEN) return null;
    const partes = token.split('.');
    if (partes.length !== 3) return null;
    let cab, corpo;
    try {
      cab = json(partes[0]);
      corpo = json(partes[1]);
    } catch {
      return null;
    }
    if (cab.alg !== 'RS256' && cab.alg !== 'ES256') return null;
    const k = await chaves(cab.kid);
    if (!k || (k.alg && k.alg !== cab.alg)) return null;
    const dados = Buffer.from(partes[0] + '.' + partes[1]);
    const assinatura = b64(partes[2]);
    const ok =
      cab.alg === 'RS256'
        ? verify('RSA-SHA256', dados, k.chave, assinatura)
        : verify('sha256', dados, { key: k.chave, dsaEncoding: 'ieee-p1363' }, assinatura);
    if (!ok) return null;
    const t = agora();
    if (corpo.iss !== emissor) return null;
    if (typeof corpo.exp !== 'number' || corpo.exp + FOLGA < t) return null;
    if (typeof corpo.nbf === 'number' && corpo.nbf - FOLGA > t) return null;
    const aud = Array.isArray(corpo.aud) ? corpo.aud : [corpo.aud];
    if (corpo.project_id !== projeto && !aud.includes('upid:' + projeto)) return null;
    if (ambiente && corpo.envName !== ambiente && !aud.includes('envName:' + ambiente)) return null;
    if (typeof corpo.sub !== 'string' || !corpo.sub) return null;
    return { jogador: corpo.sub, provedor: typeof corpo.sign_in_provider === 'string' ? corpo.sign_in_provider : '' };
  };
}

/** O token do cabecalho Authorization: Bearer. undefined = sem cabecalho; null = cabecalho mal formado. */
export function tokenDe(req) {
  const h = req.headers?.authorization;
  if (h === undefined) return undefined;
  const m = /^Bearer ([A-Za-z0-9._-]{1,4096})$/.exec(String(h));
  return m ? m[1] : null;
}
