using System;
using System.Threading.Tasks;

namespace Vstack.Conta.Unity
{
    /// <summary>
    /// Ponto de entrada do jogo, sem #if do lado dele: com a UGS instalada (VS_UGS) devolve Contas sobre a Unity Authentication e o Cloud
    /// Save; sem ela, null, e o jogo esconde a tela de conta e segue so com o save local (como antes do modulo).
    /// </summary>
    public static class Fabrica
    {
        /// <summary>ambiente: o da UGS ("production" no build da loja). le: o MetaSave de um texto de save do jogo.</summary>
        public static Contas Cria(string ambiente, Func<string, MetaSave> le)
        {
#if VS_UGS
            var u = new ContaUgs(ambiente);
            return new Contas(u, u, le);
#else
            return null;
#endif
        }

        /// <summary>O passo do servidor do jogo em Contas.Excluir (DELETE /v1/conta). servidor vazio = nada a apagar.</summary>
        public static Func<string, Task<Resultado>> Servidor(string servidor)
        {
#if VS_UGS
            return token => ContaUgs.ApagaNoServidor(servidor, token);
#else
            return null;
#endif
        }
    }
}
