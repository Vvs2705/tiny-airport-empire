using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>D-08: pt-BR e en no lancamento. O idioma, a troca por codigo e a tabela Core/Ingles.cs (a cobertura de Textos fica no checa_repo.py).</summary>
    public class IdiomasTests
    {
        Lingua _antes;

        [SetUp] public void Guarda() { _antes = Idiomas.Atual; }
        [TearDown] public void Devolve() { Idiomas.Atual = _antes; }

        [Test]
        public void Pt_devolve_o_proprio_texto()
        {
            Idiomas.Atual = Lingua.Pt;
            Assert.AreEqual("Equipe", Idiomas.Traduz("Equipe"));
            Assert.AreEqual("texto que nao existe", Idiomas.Traduz("texto que nao existe"));
        }

        [Test]
        public void En_traduz_e_o_que_nao_tem_par_fica_em_pt()
        {
            Idiomas.Atual = Lingua.En;
            Assert.AreEqual("Team", Idiomas.Traduz("Equipe"));
            Assert.AreEqual("Settings", Idiomas.Traduz("Configurações"));
            Assert.AreEqual("texto que nao existe", Idiomas.Traduz("texto que nao existe"));
        }

        [Test]
        public void Codigo_e_idioma_do_aparelho()
        {
            Assert.AreEqual("en", Idiomas.Codigo(Lingua.En));
            Assert.AreEqual("pt", Idiomas.Codigo(Lingua.Pt));
            Assert.AreEqual(Lingua.En, Idiomas.De("en"));
            Assert.AreEqual(Lingua.Pt, Idiomas.De("pt"));
            Assert.AreEqual(Lingua.Pt, Idiomas.De("lixo"), "codigo desconhecido volta para o texto de origem");
            Assert.AreEqual(Lingua.En, Idiomas.DoAparelho("English"));
            Assert.AreEqual(Lingua.Pt, Idiomas.DoAparelho("Portuguese"));
            Assert.AreEqual(Lingua.Pt, Idiomas.DoAparelho("Spanish"), "so o ingles tem traducao no lancamento");
        }

        [Test]
        public void Tabela_ingles_tem_pares_completos_e_sem_portugues_sobrando()
        {
            Assert.GreaterOrEqual(Idiomas.Pares, 700);
            // Nomes proprios que levam acento nos dois idiomas; qualquer outro valor com letra acentuada e' texto que ficou em pt.
            string[] proprios = { "Fátima", "Vânia", "Téo", "Hélio", "Júlia", "Sônia", "Léo", "Pipoca", "Zé", "Abílio", "Brasília", "Galeão", "Ribeirão", "Mariana" };
            foreach (var par in Idiomas.Todos)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(par.Value), par.Key);
                string sem = proprios.Aggregate(par.Value, (t, n) => t.Replace(n, ""));
                Assert.IsFalse(sem.Any(c => "ãõçâêôáéíóúà".Contains(c)), "ficou portugues em: " + par.Value);
            }
        }
    }
}
