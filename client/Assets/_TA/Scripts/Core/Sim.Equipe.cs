using System;

namespace TA.Core
{
    /// <summary>
    /// Contratacao com 3 candidatos e demissao (CONTRATO_FASE_C9, frente D). Cada perfil aberto pelo marco e ainda sem ninguem e' uma vaga com 3
    /// candidatos fixos (Experiencia: Junior, Pleno e Senior), os mesmos em todo jogo: nada sai da semente nem de sorteio, nenhuma moeda troca a lista e o
    /// jogador ve o preco, o salario e o rendimento antes de pagar (TA-5). O Pleno e' a pessoa e o preco de antes: Hire(p) e Hire() contratam o Pleno.
    /// Demitir tira a pessoa na hora e sem custo; a folha (Folha), a cobertura do posto (Presentes, Alocados) e a escala (Staff) se atualizam sozinhas.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>% do Pleno numa tabela do nivel (100 com RegraExperiencia desligada: so o nome muda).</summary>
        int Pct(int[] tabela, Experiencia n) => T.RegraExperiencia ? tabela[(int)n] : 100;

        static long DoPct(long v, int pct) => (v * pct + 50) / 100;

        static bool ExpValida(Experiencia n) => n >= Experiencia.Junior && n <= Experiencia.Senior;

        /// <summary>Preco do candidato (-1 = vaga fechada: perfil ja contratado, antes do marco, equipe cheia; ou experiencia invalida).</summary>
        public long CustoContratar(Perfil p, Experiencia n) => CustoContratar(p) < 0 ? -1 : Preco(p, n);

        /// <summary>O preco de tabela do candidato, com a vaga aberta ou nao (quanto custaria trocar quem esta la por ele). -1 = perfil ou experiencia invalidos.</summary>
        public long Preco(Perfil p, Experiencia n) =>
            p < 0 || (int)p >= MaxEquipe || !ExpValida(n) ? -1 : DoPct(T.Contratacao[(int)p], Pct(T.ExpContratacao, n));

        /// <summary>Salario por dia do candidato ou de quem ja esta na equipe: o que entra na Folha(Despesa.Equipe).</summary>
        public long Salario(Perfil p, Experiencia n) => DoPct(T.Salario[(int)p], Pct(T.ExpSalario, n));

        /// <summary>Quanto a pessoa rende, sem o especialista: tempo / x nas filas, venda x na loja e na alimentacao, forca x na oficina e no despacho.</summary>
        public float Rendimento(Experiencia n) => Pct(T.ExpRende, n) / 100f;

        /// <summary>Teto do atendimento gravado: o do candidato mais lento (quem atendia pode ter saido no meio). Nunca menor que TempoBase.</summary>
        int TempoTeto(Estacao e)
        {
            int min = 100;
            if (T.RegraExperiencia) foreach (int x in T.ExpRende) min = Math.Max(1, Math.Min(min, x));
            return TempoBase(e) * 100 / min + 1;
        }

        /// <summary>Contrata o candidato: aparece no 1o posto vazio (Lugar), como na v0.1. Com a conta atrasada (Pausado) nao contrata.</summary>
        public bool Hire(Perfil p, Experiencia n)
        {
            long c = CustoContratar(p, n);
            if (!PodeComprar(c)) return false;
            Cash -= c;
            var f = new Funcionario { Id = Staff.Count, Perfil = p, Experiencia = n, Em = Lugar() };
            f.De = f.Em;
            Staff.Add(f);
            Emit(Ev.Contratado, f.Id, (int)c, (int)f.Em);
            return true;
        }

        /// <summary>Demite: sai na hora, sem custo e sem devolver a contratacao; quem estava em atendimento com ele termina. Quem vinha depois sobe uma
        /// posicao (Id = indice no Staff). A vaga reabre com os mesmos 3 candidatos. Vale com a conta atrasada: e' a saida para baixar a folha. Nunca leva
        /// a derrota: sem ninguem o jogo e' o do comeco, no toque (CONTRATO_FASE_C9 §4).</summary>
        public bool Demite(int funcionario)
        {
            if (funcionario < 0 || funcionario >= Staff.Count) return false;
            Funcionario f = Staff[funcionario];
            Staff.RemoveAt(funcionario);
            for (int i = funcionario; i < Staff.Count; i++) Staff[i].Id = i;
            Emit(Ev.Demitido, funcionario, (int)f.Perfil, (int)f.Experiencia);
            return true;
        }
    }
}
