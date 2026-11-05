using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Vstack.Conta.Tests
{
    /// <summary>O modulo de conta com um servico e uma nuvem falsos (tudo sincrono: GetResult nunca espera).</summary>
    public sealed class ContaTests
    {
        sealed class Falso : IServicoConta, INuvem
        {
            public bool Rede = true, SubirFalha, ApagarFalha, Cancela;
            public readonly List<Provedor> L = new List<Provedor>();
            /// <summary>Logins que ja sao de outra conta, com o save que a nuvem dessa conta tem.</summary>
            public readonly Dictionary<Provedor, string> DeOutra = new Dictionary<Provedor, string>();
            public string Nuvem, Id;
            public int Subidas, Contas;
            public bool Entrou { get; private set; }
            public string Jogador => Id;
            public string Token => Entrou ? "tok-" + Id : null;
            public IReadOnlyCollection<Provedor> Ligados => L;
            public bool Oferece(Provedor p) => p != Provedor.Apple;

            Task<Resultado> R(Resultado r) => Task.FromResult(Rede ? r : Resultado.SemRede);

            public Task<Resultado> Entrar()
            {
                if (!Rede) return R(Resultado.SemRede);
                if (!Entrou) { Entrou = true; Id = Id ?? "j" + ++Contas; }
                return R(Resultado.Ok);
            }

            public Task<Resultado> Ligar(Provedor p)
            {
                if (Cancela) return R(Resultado.Cancelado);
                if (DeOutra.ContainsKey(p)) return R(Resultado.JaLigadaAOutra);
                if (Rede && !L.Contains(p)) L.Add(p);
                return R(Resultado.Ok);
            }

            public Task<Resultado> TrocarPara(Provedor p)
            {
                if (!Rede) return R(Resultado.SemRede);
                Id = "outra"; L.Clear(); L.Add(p);
                Nuvem = DeOutra[p];
                return R(Resultado.Ok);
            }

            public Task<Resultado> Desligar(Provedor p) { L.Remove(p); return R(Resultado.Ok); }

            public Task<Resultado> Excluir()
            {
                if (!Rede) return R(Resultado.SemRede);
                Entrou = false; Id = null; L.Clear();
                return R(Resultado.Ok);
            }

            public Task<Baixado> Baixar() => Task.FromResult(new Baixado { Resultado = Rede ? Resultado.Ok : Resultado.SemRede, Texto = Rede ? Nuvem : null });

            public Task<Resultado> Subir(string texto)
            {
                if (!Rede || SubirFalha) return R(Resultado.Falhou);
                Nuvem = texto; Subidas++;
                return R(Resultado.Ok);
            }

            public Task<Resultado> Apagar()
            {
                if (ApagarFalha) return R(Resultado.Falhou);
                Nuvem = null;
                return R(Resultado.Ok);
            }
        }

        /// <summary>Save de teste: "progresso,gravadoEm". "" ou lixo = vazio.</summary>
        static MetaSave Le(string t)
        {
            string[] p = (t ?? "").Split(',');
            if (p.Length != 2 || !long.TryParse(p[0], out long pr) || !long.TryParse(p[1], out long em)) return new MetaSave { Vazio = true };
            return new MetaSave { Progresso = pr, GravadoEm = em, Vazio = pr == 0 };
        }

        static T G<T>(Task<T> t) => t.GetAwaiter().GetResult();
        static void G(Task t) => t.GetAwaiter().GetResult();

        static (Falso, Contas) Novo()
        {
            var f = new Falso();
            return (f, new Contas(f, f, Le));
        }

        static MetaSave M(long progresso, long em) => new MetaSave { Progresso = progresso, GravadoEm = em };

        [Test]
        public void Conflito_VazioPerde_IguaisNada_FrenteNosDoisFica_DivergentePergunta()
        {
            var vazio = new MetaSave { Vazio = true };
            Assert.AreEqual(Escolha.Nada, Conflito.Decide(vazio, vazio));
            Assert.AreEqual(Escolha.SobeLocal, Conflito.Decide(M(5, 10), vazio));
            Assert.AreEqual(Escolha.UsaNuvem, Conflito.Decide(vazio, M(5, 10)));
            Assert.AreEqual(Escolha.Nada, Conflito.Decide(M(5, 10), M(5, 10)));
            Assert.AreEqual(Escolha.SobeLocal, Conflito.Decide(M(6, 11), M(5, 10)));
            Assert.AreEqual(Escolha.UsaNuvem, Conflito.Decide(M(5, 10), M(6, 11)));
            Assert.AreEqual(Escolha.SobeLocal, Conflito.Decide(M(5, 11), M(5, 10)), "mesmo progresso, mais novo");
            // jogou em dois aparelhos sem rede: mais progresso de um lado, mais novo do outro
            Assert.AreEqual(Escolha.Pergunta, Conflito.Decide(M(9, 10), M(5, 20)));
            Assert.AreEqual(Escolha.Pergunta, Conflito.Decide(M(5, 20), M(9, 10)));
        }

        [Test]
        public void Abrir_SemRede_SegueSoComOLocal()
        {
            var (f, c) = Novo();
            f.Rede = false;
            Assert.AreEqual(Resultado.SemRede, G(c.Abrir("3,1")).Resultado);
            Assert.AreEqual(EstadoConta.Fora, c.Estado);
            Assert.IsNull(c.Token);
        }

        [Test]
        public void Abrir_Anonima_NaoMexeNaNuvem()
        {
            var (f, c) = Novo();
            Passo p = G(c.Abrir("3,1"));
            Assert.AreEqual(Resultado.Ok, p.Resultado);
            Assert.AreEqual(EstadoConta.Anonima, c.Estado);
            Assert.AreEqual(Escolha.Nada, p.Escolha);
            c.Gravou("4,2");
            G(c.Tick(100));
            Assert.AreEqual(0, f.Subidas, "conta anonima nao sobe save");
        }

        [Test]
        public void Ligar_PrimeiraVez_SobeOLocalEMarcaOBrinde_SegundaNao()
        {
            var (f, c) = Novo();
            G(c.Abrir("3,1"));
            Passo p = G(c.Ligar(Provedor.GooglePlayGames, "3,1"));
            Assert.AreEqual(Resultado.Ok, p.Resultado);
            Assert.IsTrue(p.PrimeiraLigacao);
            Assert.AreEqual(EstadoConta.Ligada, c.Estado);
            Assert.AreEqual("3,1", f.Nuvem, "o progresso do aparelho vai para a conta, nada se perde");
            p = G(c.Ligar(Provedor.Facebook, "3,1"));
            Assert.AreEqual(Resultado.Ok, p.Resultado);
            Assert.IsFalse(p.PrimeiraLigacao, "o segundo login nao e' conta nova");
        }

        [Test]
        public void Ligar_Cancelado_NadaMuda()
        {
            var (f, c) = Novo();
            G(c.Abrir("3,1"));
            f.Cancela = true;
            Passo p = G(c.Ligar(Provedor.Facebook, "3,1"));
            Assert.AreEqual(Resultado.Cancelado, p.Resultado);
            Assert.IsFalse(p.PrimeiraLigacao);
            Assert.AreEqual(EstadoConta.Anonima, c.Estado);
            Assert.IsNull(f.Nuvem);
        }

        [Test]
        public void Ligar_LoginDeOutraConta_TrocarCarregaONuvemSemBrinde()
        {
            var (f, c) = Novo();
            f.DeOutra[Provedor.GooglePlayGames] = "50,9";
            G(c.Abrir("3,1"));
            Passo p = G(c.Ligar(Provedor.GooglePlayGames, "3,1"));
            Assert.AreEqual(Resultado.JaLigadaAOutra, p.Resultado);
            Assert.IsFalse(p.PrimeiraLigacao);
            p = G(c.TrocarPara(Provedor.GooglePlayGames, "3,1"));
            Assert.AreEqual(Escolha.UsaNuvem, p.Escolha);
            Assert.AreEqual("50,9", p.Nuvem);
            Assert.IsFalse(p.PrimeiraLigacao, "conta que ja existia nao ganha brinde de novo");
            Assert.AreEqual(0, f.Subidas, "o local nao sobe por cima do save da conta");
        }

        [Test]
        public void Trocar_ContaSemSaveNaNuvem_FicaComOLocal()
        {
            var (f, c) = Novo();
            f.DeOutra[Provedor.Facebook] = null;
            G(c.Abrir("3,1"));
            Passo p = G(c.TrocarPara(Provedor.Facebook, "3,1"));
            Assert.AreEqual(Escolha.SobeLocal, p.Escolha);
            Assert.AreEqual("3,1", f.Nuvem);
        }

        [Test]
        public void Abrir_Ligada_ComparaComANuvem()
        {
            var (f, c) = Novo();
            G(c.Abrir("3,1"));
            G(c.Ligar(Provedor.GooglePlayGames, "3,1"));
            f.Nuvem = "8,5";   // jogou em outro aparelho
            Passo p = G(c.Abrir("3,1"));
            Assert.AreEqual(Escolha.UsaNuvem, p.Escolha);
            Assert.AreEqual("8,5", p.Nuvem);
            p = G(c.Abrir("9,6"));
            Assert.AreEqual(Escolha.SobeLocal, p.Escolha);
            Assert.AreEqual("9,6", f.Nuvem);
        }

        [Test]
        public void Pergunta_NadaSobeAteEscolher_DepoisValeAEscolha()
        {
            var (f, c) = Novo();
            G(c.Abrir("3,1"));
            G(c.Ligar(Provedor.GooglePlayGames, "3,1"));
            f.Nuvem = "2,50";
            int antes = f.Subidas;
            Passo p = G(c.Abrir("9,10"));
            Assert.AreEqual(Escolha.Pergunta, p.Escolha);
            c.Gravou("9,11");
            G(c.Tick(1000, pausa: true));
            Assert.AreEqual(antes, f.Subidas, "o jogo gravou, mas o save da nuvem espera a escolha");
            Assert.AreEqual("2,50", f.Nuvem);
            p = G(c.Resolve(false, "9,11"));
            Assert.AreEqual(Escolha.SobeLocal, p.Escolha);
            Assert.AreEqual("9,11", f.Nuvem);

            f.Nuvem = "2,90";
            Assert.AreEqual(Escolha.Pergunta, G(c.Abrir("9,11")).Escolha);
            p = G(c.Resolve(true, "9,11"));
            Assert.AreEqual(Escolha.UsaNuvem, p.Escolha);
            Assert.AreEqual("2,90", p.Nuvem);
        }

        [Test]
        public void Tick_SobeNoRitmo_PausaSobeJa_FalhaEsperaCrescente()
        {
            var (f, c) = Novo();
            G(c.Abrir("3,1"));
            G(c.Ligar(Provedor.GooglePlayGames, "3,1"));
            int n = f.Subidas;
            c.Gravou("4,2");
            G(c.Tick(0));
            Assert.AreEqual(n + 1, f.Subidas);
            c.Gravou("5,3");
            G(c.Tick(10));
            Assert.AreEqual(n + 1, f.Subidas, "antes do intervalo nao sobe");
            Assert.IsTrue(c.Pendente);
            G(c.Tick(10, pausa: true));
            Assert.AreEqual(n + 2, f.Subidas, "ao pausar sobe na hora");
            Assert.IsFalse(c.Pendente);

            f.SubirFalha = true;
            c.Gravou("6,4");
            G(c.Tick(200, pausa: true));
            Assert.IsTrue(c.Pendente, "falhou: guarda para tentar de novo");
            G(c.Tick(201));
            f.SubirFalha = false;
            G(c.Tick(201.5));
            Assert.AreEqual(n + 2, f.Subidas, "espera 2 s depois da 1a falha");
            G(c.Tick(202));
            Assert.AreEqual(n + 3, f.Subidas);
            Assert.AreEqual("6,4", f.Nuvem);
        }

        [Test]
        public void Gravou_SaveGrandeDemais_NaoSobe()
        {
            var (f, c) = Novo();
            G(c.Abrir("3,1"));
            G(c.Ligar(Provedor.GooglePlayGames, "3,1"));
            c.Gravou(new string('x', Contas.MaxSave + 1));
            Assert.IsFalse(c.Pendente);
        }

        [Test]
        public void Excluir_NaOrdem_ServidorNuvemConta_FalhaParaTudo()
        {
            var (f, c) = Novo();
            G(c.Abrir("3,1"));
            G(c.Ligar(Provedor.GooglePlayGames, "3,1"));
            var passos = new List<string>();
            Func<string, Task<Resultado>> servidor = tok => { passos.Add("servidor:" + tok); return Task.FromResult(Resultado.Ok); };

            f.ApagarFalha = true;
            Assert.AreEqual(Resultado.Falhou, G(c.Excluir(servidor)));
            Assert.AreEqual(EstadoConta.Ligada, c.Estado, "a nuvem falhou: a conta continua");
            f.ApagarFalha = false;

            Func<string, Task<Resultado>> fora = tok => Task.FromResult(Resultado.SemRede);
            Assert.AreEqual(Resultado.SemRede, G(c.Excluir(fora)));
            Assert.AreEqual("3,1", f.Nuvem, "o servidor falhou: nada mais foi apagado");

            Assert.AreEqual(Resultado.Ok, G(c.Excluir(servidor)));
            Assert.AreEqual("servidor:tok-j1", passos[passos.Count - 1], "o servidor recebe o token antes de a conta sumir");
            Assert.IsNull(f.Nuvem);
            Assert.AreEqual(EstadoConta.Fora, c.Estado);
            G(c.Abrir(""));
            Assert.AreEqual(EstadoConta.Anonima, c.Estado, "depois de excluir, a proxima abertura e' uma conta nova");
            Assert.AreEqual("j2", f.Jogador);
        }

        [Test]
        public void Excluir_SemServidor_ApagaNuvemEConta()
        {
            var (f, c) = Novo();
            G(c.Abrir("3,1"));
            Assert.AreEqual(Resultado.Ok, G(c.Excluir(null)));
            Assert.AreEqual(EstadoConta.Fora, c.Estado);
        }

        [Test]
        public void Desligar_UltimoLogin_VoltaAAnonimaENaoSobeMais()
        {
            var (f, c) = Novo();
            G(c.Abrir("3,1"));
            G(c.Ligar(Provedor.Facebook, "3,1"));
            int n = f.Subidas;
            Assert.AreEqual(Resultado.Ok, G(c.Desligar(Provedor.Facebook)));
            Assert.AreEqual(EstadoConta.Anonima, c.Estado);
            c.Gravou("4,2");
            G(c.Tick(500, pausa: true));
            Assert.AreEqual(n, f.Subidas);
        }

        [Test]
        public void Oferece_SoOQueOBuildTem()
        {
            var (f, c) = Novo();
            Assert.IsTrue(c.Oferece(Provedor.GooglePlayGames));
            Assert.IsFalse(c.Oferece(Provedor.Apple));
        }
    }
}
