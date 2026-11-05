using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vstack.Conta
{
    /// <summary>Com quem a conta anonima pode ser ligada. Ordem fixa: o indice pode ir para o diario do jogo. Novo provedor so no fim.</summary>
    public enum Provedor { GooglePlayGames, Facebook, Apple }

    /// <summary>O que o servico respondeu. JaLigadaAOutra = esse login do Google/Facebook ja e' de outra conta (o jogador escolhe trocar).</summary>
    public enum Resultado { Ok, Cancelado, JaLigadaAOutra, SemRede, Falhou }

    /// <summary>Fora = ainda nao entrou (sem rede ao abrir: o jogo segue so com o save local). Anonima = conta so deste aparelho.</summary>
    public enum EstadoConta { Fora, Anonima, Ligada }

    /// <summary>O que fazer com o save depois de comparar o do aparelho com o da nuvem.</summary>
    public enum Escolha { Nada, SobeLocal, UsaNuvem, Pergunta }

    /// <summary>
    /// A conta de jogador (Unity Authentication no jogo; um falso nos testes). Toda chamada devolve Resultado e nunca joga excecao: o
    /// adaptador converte. Entrar() reabre a sessao guardada no aparelho ou cria uma conta anonima nova.
    /// </summary>
    public interface IServicoConta
    {
        Task<Resultado> Entrar();
        Task<Resultado> Ligar(Provedor p);
        /// <summary>Sai da conta deste aparelho e entra na conta que ja tem esse login (o save da nuvem dela substitui o local).</summary>
        Task<Resultado> TrocarPara(Provedor p);
        Task<Resultado> Desligar(Provedor p);
        /// <summary>Apaga a conta no servico (exigencia da Play). Depois disso, Entrar() cria uma conta anonima nova.</summary>
        Task<Resultado> Excluir();
        /// <summary>O provedor esta instalado e configurado neste build (sem o SDK do Facebook, o botao nem aparece).</summary>
        bool Oferece(Provedor p);
        bool Entrou { get; }
        /// <summary>Id do jogador no servico (nunca vai para o diario nem para a telemetria).</summary>
        string Jogador { get; }
        /// <summary>Token de acesso, para o servidor do jogo saber de quem e' a compra (Authorization: Bearer).</summary>
        string Token { get; }
        IReadOnlyCollection<Provedor> Ligados { get; }
    }

    /// <summary>Um texto baixado da nuvem. Texto null = a conta ainda nao tem save la.</summary>
    public struct Baixado
    {
        public Resultado Resultado;
        public string Texto;
    }

    /// <summary>O save na nuvem, por conta (Cloud Save no jogo). Uma chave so: o texto inteiro do save, como o jogo grava no disco.</summary>
    public interface INuvem
    {
        Task<Baixado> Baixar();
        Task<Resultado> Subir(string texto);
        Task<Resultado> Apagar();
    }

    /// <summary>O que o modulo precisa saber de um save para comparar dois, sem conhecer o formato do jogo.</summary>
    public struct MetaSave
    {
        /// <summary>Sem save, ou um jogo que acabou de comecar (nada a perder).</summary>
        public bool Vazio;
        /// <summary>Quando foi gravado (unix, do relogio de quem gravou: so desempata).</summary>
        public long GravadoEm;
        /// <summary>Um numero que so cresce com o jogo (no Little Terminal: passageiros embarcados). E' o que decide.</summary>
        public long Progresso;
    }

    public static class Conflito
    {
        /// <summary>
        /// Qual save fica. Vazio perde sempre. Iguais: nada. Um na frente nos dois (progresso e data): ele fica. Um tem mais progresso e o
        /// outro e' mais novo (jogou em dois aparelhos sem rede): o jogador escolhe. Nunca escolhe calado contra o progresso maior.
        /// </summary>
        public static Escolha Decide(MetaSave local, MetaSave nuvem)
        {
            if (nuvem.Vazio) return local.Vazio ? Escolha.Nada : Escolha.SobeLocal;
            if (local.Vazio) return Escolha.UsaNuvem;
            if (local.Progresso == nuvem.Progresso && local.GravadoEm == nuvem.GravadoEm) return Escolha.Nada;
            if (local.Progresso >= nuvem.Progresso && local.GravadoEm >= nuvem.GravadoEm) return Escolha.SobeLocal;
            if (nuvem.Progresso >= local.Progresso && nuvem.GravadoEm >= local.GravadoEm) return Escolha.UsaNuvem;
            return Escolha.Pergunta;
        }
    }

    /// <summary>O fim de um passo da conta. Escolha UsaNuvem: o jogo carrega Nuvem no lugar do save local. Pergunta: mostra os dois e chama Resolve.</summary>
    public struct Passo
    {
        public Resultado Resultado;
        public Escolha Escolha;
        public string Nuvem;
        /// <summary>Ligar: e' a primeira vez que esta conta ganha um login (o jogo da o brinde, uma vez, pelo save).</summary>
        public bool PrimeiraLigacao;
    }

    /// <summary>
    /// Conta opcional, reaproveitavel entre os jogos do estudio (README.md desta pasta). O jogo comeca anonimo e liga a conta quando o jogador
    /// quiser, sem perder o progresso. Com a conta ligada, o save sobe para a nuvem depois de gravar (no maximo 1 vez por IntervaloSeg, e na
    /// hora ao pausar) e e' comparado com o da nuvem ao abrir. Nada aqui conhece a Unity, o SDK, a rede ou o relogio: `agora` e' o tempo real
    /// desde o inicio do app, em segundos, so para o ritmo e a espera crescente. Sem rede, nada se perde: o save local continua valendo.
    /// </summary>
    public sealed class Contas
    {
        /// <summary>Intervalo minimo entre duas subidas com o jogo aberto (s). Espera depois de falha: EsperaMin dobrando ate EsperaMax.</summary>
        public const double IntervaloSeg = 60, EsperaMin = 2, EsperaMax = 300;
        /// <summary>Teto do save que sobe (bytes do texto). Maior que isso e' defeito: nao sobe e o jogo continua com o local.</summary>
        public const int MaxSave = 512 * 1024;

        readonly IServicoConta _s;
        readonly INuvem _n;
        readonly Func<string, MetaSave> _le;
        string _pendente, _nuvemPendente;
        bool _subindo;
        double _proxima;
        int _falhas;

        /// <summary>le: o MetaSave de um texto de save do jogo (null ou lixo = Vazio).</summary>
        public Contas(IServicoConta servico, INuvem nuvem, Func<string, MetaSave> le)
        {
            _s = servico;
            _n = nuvem;
            _le = le;
        }

        public EstadoConta Estado => !_s.Entrou ? EstadoConta.Fora : _s.Ligados.Count > 0 ? EstadoConta.Ligada : EstadoConta.Anonima;
        public IReadOnlyCollection<Provedor> Ligados => _s.Ligados;
        public bool Oferece(Provedor p) => _s.Oferece(p);
        public string Token => _s.Entrou ? _s.Token : null;
        /// <summary>O id do jogador, so para a tela mostrar ao proprio jogador (pedido de exclusao por e-mail). Nunca para log ou diario.</summary>
        public string Jogador => _s.Entrou ? _s.Jogador : null;
        /// <summary>Ha um save esperando para subir (para a tela dizer "sincronizando").</summary>
        public bool Pendente => _pendente != null || _subindo;

        /// <summary>Ao abrir o jogo: entra (sessao guardada ou anonima) e, com a conta ligada, compara o save local com o da nuvem.</summary>
        public async Task<Passo> Abrir(string local)
        {
            Resultado r = await _s.Entrar();
            if (r != Resultado.Ok) return new Passo { Resultado = r };
            return Estado == EstadoConta.Ligada ? await Sincroniza(local) : new Passo { Resultado = Resultado.Ok };
        }

        /// <summary>Liga o login p a conta deste aparelho. JaLigadaAOutra: a tela oferece TrocarPara(p) (com o aviso de que o progresso
        /// deste aparelho sai) ou cancelar. Ok: o save local sobe na hora.</summary>
        public async Task<Passo> Ligar(Provedor p, string local)
        {
            if (!_s.Entrou && await _s.Entrar() != Resultado.Ok) return new Passo { Resultado = Resultado.SemRede };
            bool primeira = _s.Ligados.Count == 0;
            Resultado r = await _s.Ligar(p);
            if (r != Resultado.Ok) return new Passo { Resultado = r };
            Passo s = await Sincroniza(local);
            s.PrimeiraLigacao = primeira;
            return s;
        }

        /// <summary>Entra na conta que ja tem o login p. O save dela substitui o local (a tela avisou); conta sem save na nuvem fica com o local.</summary>
        public async Task<Passo> TrocarPara(Provedor p, string local)
        {
            Resultado r = await _s.TrocarPara(p);
            if (r != Resultado.Ok) return new Passo { Resultado = r };
            _pendente = null;
            Baixado b = await _n.Baixar();
            if (b.Resultado != Resultado.Ok) return new Passo { Resultado = b.Resultado };
            if (b.Texto == null || _le(b.Texto).Vazio) return await Sobe(local, Resultado.Ok, Escolha.SobeLocal);
            return new Passo { Resultado = Resultado.Ok, Escolha = Escolha.UsaNuvem, Nuvem = b.Texto };
        }

        /// <summary>Tira o login p. Sem nenhum, a conta volta a ser so deste aparelho (o progresso fica aqui).</summary>
        public Task<Resultado> Desligar(Provedor p) => _s.Desligar(p);

        /// <summary>Depois da Pergunta: true = o da nuvem (o jogo carrega Passo.Nuvem), false = o deste aparelho (sobe por cima).</summary>
        public async Task<Passo> Resolve(bool usaNuvem, string local)
        {
            string nuvem = _nuvemPendente;
            _nuvemPendente = null;
            if (usaNuvem && nuvem != null) return new Passo { Resultado = Resultado.Ok, Escolha = Escolha.UsaNuvem, Nuvem = nuvem };
            return await Sobe(local, Resultado.Ok, Escolha.SobeLocal);
        }

        /// <summary>O jogo gravou o save: guarda para subir no proximo Tick. So o ultimo conta.</summary>
        public void Gravou(string texto)
        {
            if (Estado == EstadoConta.Ligada && !string.IsNullOrEmpty(texto) && texto.Length <= MaxSave) _pendente = texto;
        }

        /// <summary>Chamar todo quadro (ou a cada segundo). agora = tempo real desde o inicio do app. pausa = o app vai para o fundo: sobe ja.</summary>
        public async Task Tick(double agora, bool pausa = false)
        {
            // com a Pergunta aberta nada sobe: subir o local por cima apagaria o da nuvem antes do jogador escolher
            if (_pendente == null || _subindo || _nuvemPendente != null || Estado != EstadoConta.Ligada || (!pausa && agora < _proxima)) return;
            string texto = _pendente;
            _subindo = true;
            Resultado r;
            try { r = await _n.Subir(texto); }
            finally { _subindo = false; }
            if (r == Resultado.Ok)
            {
                if (ReferenceEquals(_pendente, texto)) _pendente = null;
                _falhas = 0;
                _proxima = agora + IntervaloSeg;
            }
            else
            {
                _falhas++;
                _proxima = agora + Math.Min(EsperaMax, EsperaMin * Math.Pow(2, Math.Min(16, _falhas - 1)));
            }
        }

        /// <summary>
        /// Excluir a conta (Play: exclusao dentro do app). Ordem: o servidor do jogo apaga o historico de compras (precisa do token, entao vem
        /// antes), a nuvem apaga o save e o servico apaga a conta. Qualquer passo que falha para tudo e devolve o erro: nada fica pela metade
        /// calado. Ok: o jogo apaga o save local e recomeca. apagaServidor: null = o jogo nao tem servidor.
        /// </summary>
        public async Task<Resultado> Excluir(Func<string, Task<Resultado>> apagaServidor)
        {
            if (!_s.Entrou && await _s.Entrar() != Resultado.Ok) return Resultado.SemRede;
            if (apagaServidor != null)
            {
                Resultado a = await apagaServidor(_s.Token);
                if (a != Resultado.Ok) return a;
            }
            Resultado n = await _n.Apagar();
            if (n != Resultado.Ok) return n;
            Resultado e = await _s.Excluir();
            if (e == Resultado.Ok) { _pendente = _nuvemPendente = null; _falhas = 0; _proxima = 0; }
            return e;
        }

        async Task<Passo> Sincroniza(string local)
        {
            Baixado b = await _n.Baixar();
            if (b.Resultado != Resultado.Ok) return new Passo { Resultado = b.Resultado };
            Escolha e = Conflito.Decide(_le(local), _le(b.Texto));
            switch (e)
            {
                case Escolha.SobeLocal: return await Sobe(local, Resultado.Ok, e);
                case Escolha.UsaNuvem: return new Passo { Resultado = Resultado.Ok, Escolha = e, Nuvem = b.Texto };
                case Escolha.Pergunta:
                    _nuvemPendente = b.Texto;
                    return new Passo { Resultado = Resultado.Ok, Escolha = e, Nuvem = b.Texto };
                default: return new Passo { Resultado = Resultado.Ok };
            }
        }

        async Task<Passo> Sobe(string local, Resultado r, Escolha e)
        {
            if (string.IsNullOrEmpty(local) || local.Length > MaxSave) return new Passo { Resultado = r, Escolha = Escolha.Nada };
            Resultado s = await _n.Subir(local);
            if (s != Resultado.Ok) _pendente = local;   // tenta de novo no Tick
            return new Passo { Resultado = r, Escolha = e };
        }
    }
}
