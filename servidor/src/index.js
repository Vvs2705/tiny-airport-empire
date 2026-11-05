// Sobe o servico. Variaveis (todas sem segredo; a credencial vem da conta de servico do Cloud Run):
//   AMBIENTE      falso | teste | producao (obrigatoria). falso = loja falsa e registro na memoria, nunca fala com a Google.
//   PROJETO       id do projeto GCP para o Firestore (obrigatoria em producao; em teste, sem ela o registro fica na memoria)
//   DESLIGADO=1   kill switch (GDD §15.3): toda validacao responde 503 e nada e' concedido
//   POR_MINUTO    limite por IP (padrao 20)        PORT    porta (o Cloud Run define)
import { createServer } from 'node:http';
import { criaApp } from './app.js';
import { lojaFalsa } from './falso.js';
import { credencial, playApi } from './play.js';
import { registroFirestore, registroMemoria } from './registro.js';
import { limitador } from './limite.js';

export function configura(env) {
  const ambiente = env.AMBIENTE;
  if (!['falso', 'teste', 'producao'].includes(ambiente)) throw new Error('AMBIENTE deve ser falso, teste ou producao');
  if (ambiente === 'producao' && !env.PROJETO) throw new Error('producao precisa do PROJETO (registro no Firestore)');
  const token = credencial();
  return {
    ambiente,
    consulta: ambiente === 'falso' ? async (p, t) => lojaFalsa(p, t) : playApi(token),
    registra: ambiente !== 'falso' && env.PROJETO ? registroFirestore(env.PROJETO, token) : registroMemoria(),
    limite: limitador(Number(env.POR_MINUTO) > 0 ? Number(env.POR_MINUTO) : 20),
    desligado: env.DESLIGADO === '1',
    log: (o) => console.log(JSON.stringify(o)),
  };
}

if (import.meta.url === `file://${process.argv[1]}`) {
  const app = criaApp(configura(process.env));
  const porta = Number(process.env.PORT) || 8080;
  createServer(app).listen(porta, () => console.log(JSON.stringify({ severity: 'INFO', msg: 'no ar', porta, ambiente: process.env.AMBIENTE })));
}
