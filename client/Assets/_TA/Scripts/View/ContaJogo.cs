using System;
using System.Threading.Tasks;
using TA.Core;
using UnityEngine;
using Vstack.Conta;
using Vstack.Conta.Unity;

namespace TA
{
    /// <summary>
    /// A conta opcional no Little Terminal (docs/CONTAS.md): a cola entre o Game, a tela (Hud.Conta.cs) e o modulo reaproveitavel
    /// (client/Assets/Vstack/Conta). Sem a UGS no build (Fabrica.Cria devolve null), o Game nao cria esta classe e o jogo e' o de antes.
    /// O jogo abre anonimo; ligar a conta da o brinde 1x (Sim.DaBrindeConta) e liga o save na nuvem. Carregar um save da nuvem grava o
    /// arquivo e fecha o jogo (como o Excluir meus dados): reabrir monta tudo com ele, sem trocar o Sim de um mundo ja montado.
    /// Nada daqui vai ao diario alem do provedor (indice) e do resultado: nunca o id do jogador.
    /// </summary>
    public sealed class ContaJogo
    {
        readonly Game _g;
        readonly Contas _c;
        readonly Func<string, Task<Resultado>> _servidor;
        bool _ocupada, _bloqueada;

        /// <summary>A tela abre: Trocar (o login e' de outra conta), Escolher (dois progressos), Carregado (fechar e reabrir), Toast (aviso curto).</summary>
        public event Action<Provedor> PedeTrocar;
        public event Action<long, long> PedeEscolher;
        public event Action Carregado;
        public event Action<string> Toast;

        ContaJogo(Game g, Contas c, Func<string, Task<Resultado>> servidor)
        {
            _g = g;
            _c = c;
            _servidor = servidor;
        }

        /// <summary>null = build sem a UGS. servidor: o https do servidor de validacao (vazio = sem servidor, nada a apagar la).</summary>
        public static ContaJogo Cria(Game g, string servidor)
        {
            Contas c = Fabrica.Cria(Debug.isDebugBuild ? "development" : "production", Meta);
            return c == null ? null : new ContaJogo(g, c, Fabrica.Servidor(servidor));
        }

        public EstadoConta Estado => _c.Estado;
        public bool Ligado(Provedor p) { foreach (Provedor x in _c.Ligados) if (x == p) return true; return false; }
        public bool Oferece(Provedor p) => _c.Oferece(p);
        public bool Sincronizando => _c.Pendente;
        public bool Ocupada => _ocupada;
        /// <summary>O token da conta para o servidor de validacao (LojaIap): null sem conta.</summary>
        public string Token => _c.Token;
        /// <summary>O id da conta, para o jogador informar num pedido de exclusao por e-mail (nunca vai ao diario).</summary>
        public string Jogador => _c.Jogador;

        /// <summary>
        /// O que o modulo sabe de um save do jogo: o progresso e' TotalEmbarcados (so cresce) e a data, o writtenAt do envelope. Save de
        /// schema mais novo (outro aparelho com o jogo atualizado) ganha sempre, para nunca ser apagado por um build velho; quem abre e' o
        /// Game, que recusa e pede para atualizar.
        /// </summary>
        public static MetaSave Meta(string texto)
        {
            SaveEnvelope.Result r = SaveEnvelope.Unwrap(texto);
            if (r.Status == SaveEnvelope.Status.NewerSchema) return new MetaSave { Progresso = long.MaxValue, GravadoEm = long.MaxValue };
            if (r.Status != SaveEnvelope.Status.Ok) return new MetaSave { Vazio = true };
            long emb = 0;
            foreach (string linha in r.Payload.Split('\n'))
                if (linha.StartsWith("emb=", StringComparison.Ordinal)) { long.TryParse(linha.Substring(4), out emb); break; }
            return new MetaSave { Vazio = emb <= 0, Progresso = Math.Max(0, emb), GravadoEm = r.WrittenAt };
        }

        static long Embarcados(string texto) => Meta(texto).Progresso;

        public async void Abre(string local) => Trata(await Tenta(() => _c.Abrir(local)), null);

        public async void Ligar(Provedor p)
        {
            if (_ocupada) return;
            Passo s = await Tenta(() => _c.Ligar(p, _g.SaveAtual()));
            if (s.Resultado == Resultado.JaLigadaAOutra) { PedeTrocar?.Invoke(p); return; }
            Trata(s, p);
        }

        /// <summary>O jogador aceitou carregar o progresso da conta que ja tem esse login.</summary>
        public async void Trocar(Provedor p) => Trata(await Tenta(() => _c.TrocarPara(p, _g.SaveAtual())), null);

        public async void Desligar(Provedor p)
        {
            if (_ocupada) return;
            _ocupada = true;
            Resultado r;
            try { r = await _c.Desligar(p); }
            finally { _ocupada = false; }
            if (r != Resultado.Ok) Toast?.Invoke(Aviso(r));
        }

        /// <summary>Depois de "Dois progressos": true = o da nuvem.</summary>
        public async void Escolhe(bool nuvem) => Trata(await Tenta(() => _c.Resolve(nuvem, _g.SaveAtual())), null);

        /// <summary>O Game gravou o save: sobe no proximo Tick (so com a conta ligada).</summary>
        public void Gravou(string texto)
        {
            if (!_bloqueada) _c.Gravou(texto);
        }

        public async void Tick(double agora, bool pausa = false)
        {
            if (_bloqueada) return;
            try { await _c.Tick(agora, pausa); }
            catch (Exception e) { Debug.LogWarning("conta: " + e.GetType().Name); }
        }

        /// <summary>
        /// Excluir meus dados com a conta: apaga no servidor, na nuvem e a conta, e so entao o Game apaga o local (apagaLocal). Com a conta
        /// ligada e sem rede, nao apaga nada e avisa (o jogador tenta de novo). Conta anonima sem rede: apaga o local mesmo assim; a conta
        /// anonima que sobra na UGS nao aponta para ninguem.
        /// </summary>
        public async void Excluir(Action apagaLocal)
        {
            if (_ocupada) return;
            bool ligada = Estado == EstadoConta.Ligada;
            _ocupada = true;
            Resultado r;
            try { r = await _c.Excluir(_servidor); }
            catch (Exception e) { Debug.LogWarning("conta: " + e.GetType().Name); r = Resultado.Falhou; }
            finally { _ocupada = false; }
            if (r == Resultado.Ok || !ligada)
            {
                if (r == Resultado.Ok) _g.Diario("account_deleted");
                _bloqueada = true;
                apagaLocal();
                return;
            }
            Toast?.Invoke(Aviso(r));
        }

        async Task<Passo> Tenta(Func<Task<Passo>> f)
        {
            _ocupada = true;
            try { return await f(); }
            catch (Exception e) { Debug.LogWarning("conta: " + e.GetType().Name); return new Passo { Resultado = Resultado.Falhou }; }
            finally { _ocupada = false; }
        }

        /// <summary>p: o login que acabou de ser ligado (null quando nao foi um Ligar).</summary>
        void Trata(Passo s, Provedor? p)
        {
            if (s.Resultado != Resultado.Ok)
            {
                if (p != null && s.Resultado != Resultado.Cancelado) Toast?.Invoke(Aviso(s.Resultado));
                return;
            }
            if (p != null)
            {
                _g.Diario("account_linked", (int)p.Value, s.PrimeiraLigacao ? 1 : 0);
                bool brinde = s.PrimeiraLigacao && _g.Sim.DaBrindeConta();
                if (brinde) _g.GravaAgora();
                Toast?.Invoke(brinde ? Textos.ContaGanhou : Textos.ContaLigou);
            }
            switch (s.Escolha)
            {
                case Escolha.UsaNuvem:
                    if (_g.CarregaSave(s.Nuvem)) { _bloqueada = true; Carregado?.Invoke(); }
                    else { _bloqueada = true; Toast?.Invoke(Textos.ContaNova); }   // schema novo: nada sobe por cima ate atualizar o jogo
                    break;
                case Escolha.Pergunta:
                    _g.Diario("cloud_save_conflict");
                    PedeEscolher?.Invoke(Embarcados(_g.SaveAtual()), Embarcados(s.Nuvem));
                    break;
            }
        }

        static string Aviso(Resultado r) => r == Resultado.SemRede ? Textos.ContaSemRede : Textos.ContaFalhou;
    }
}
