// Sobe o servico. Variaveis (todas sem segredo; a credencial vem da conta de servico do Cloud Run):
//   AMBIENTE      falso | teste | producao (obrigatoria). falso = loja falsa e registro na memoria, nunca fala com a Google.
//   PROJETO       id do projeto GCP para o Firestore (obrigatoria em producao; em teste, sem ela o registro fica na memoria)
//   DESLIGADO=1   kill switch (GDD §15.3): toda validacao responde 503 e nada e' concedido
//   POR_MINUTO    limite por IP (padrao 20)        PORT    porta (o Cloud Run define)
//   PROJETO_UGS   id do projeto da Unity Gaming Services: liga a conta (docs/CONTAS.md). Sem ela, o servidor nao conhece conta nenhuma.
//   AMBIENTE_UGS  ambiente da UGS exigido no token (ex.: production); vazio = qualquer um do projeto
//   EXIGE_CONTA=1 compra sem o token da conta e' recusada com 401 (ligar quando o build com a conta estiver na loja)
import { createServer } from 'node:http';
import { criaApp } from './app.js';
import { lojaFalsa } from './falso.js';
import { credencial, playApi } from './play.js';
import { registroFirestore, registroMemoria } from './registro.js';
import { limitador } from './limite.js';
import { chavesRemotas, verificador } from './conta.js';
import { historicoFirestore, historicoMemoria } from './historico.js';

export function configura(env) {
  const ambiente = env.AMBIENTE;
  if (!['falso', 'teste', 'producao'].includes(ambiente)) throw new Error('AMBIENTE deve ser falso, teste ou producao');
  if (ambiente === 'producao' && !env.PROJETO) throw new Error('producao precisa do PROJETO (registro no Firestore)');
  if (env.EXIGE_CONTA === '1' && !env.PROJETO_UGS) throw new Error('EXIGE_CONTA precisa do PROJETO_UGS');
  const token = credencial();
  const firestore = ambiente !== 'falso' && env.PROJETO;
  return {
    ambiente,
    consulta: ambiente === 'falso' ? async (p, t) => lojaFalsa(p, t) : playApi(token),
    registra: firestore ? registroFirestore(env.PROJETO, token) : registroMemoria(),
    conta: env.PROJETO_UGS ? verificador({ projeto: env.PROJETO_UGS, ambiente: env.AMBIENTE_UGS ?? '', chaves: chavesRemotas() }) : null,
    exigeConta: env.EXIGE_CONTA === '1',
    historico: firestore ? historicoFirestore(env.PROJETO, token) : historicoMemoria(),
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
