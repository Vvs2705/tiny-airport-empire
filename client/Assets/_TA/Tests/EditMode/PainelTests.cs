using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Painel de operacoes com o relatorio do dia (frente I, Sim.Painel.cs): so le (chamar todo passo da o mesmo jogo, numero a numero),
    /// os alertas saem do critico ao informativo e cada um aponta para o lugar certo, as partidas seguem o voo e o relatorio bate com o da frente A.</summary>
    public class PainelTests
    {
        static long DiaPassos(Sim s) => (long)s.OndaPassos * s.T.DiaOndas;

        /// <summary>Le tudo o que o painel mostra (o que a tela chama a cada pintura).</summary>
        static string LeTudo(Sim s)
        {
            var b = new System.Text.StringBuilder();
            foreach (Alerta a in s.Alertas()) b.Append(a.Tipo).Append(a.Indice).Append(',');
            for (int f = 0; f < 2; f++) { LinhaVoo l = s.Partida(f); b.Append(l.Situacao).Append(l.EmRisco).Append(','); }
            b.Append(s.AvioesEmSolo).Append(s.AvioesChegando).Append(s.ChegantesNoTerminal).Append(s.Congestionada)
                .Append(s.ReceitaHoje).Append(s.DespesaHoje).Append(s.SaldoHoje).Append(s.Ontem?.Lucro).Append(s.OcorrenciasHoje().Count)
                .Append(s.UltimaOcorrencia?.Tipo).Append(s.AtrasosHoje).Append(s.PerdidosHoje).Append(s.ReclamacoesHoje);
            return b.ToString();
        }

        static readonly Func<Tuning>[] Cenarios =
        {
            () => new Tuning(),
            () => RotasIncidentesTests.Com(TipoIncidente.Falha),
            () => ClimaFalhasTests.ComFalha(TipoFalha.Pane),
            () => ClimaFalhasTests.ComClima(TipoIncidente.Temporal),
        };

        [Test]
        public void SoLe_ChamarTodoPasso_DaOMesmoJogo_NumeroANumero()
        {
            foreach (Func<Tuning> cen in Cenarios)
                foreach (BotProfile p in new[] { BotProfile.Bom, BotProfile.Parado })
                {
                    Sim a = new Sim(cen(), 3), b = new Sim(cen(), 3);
                    Bot ba = new Bot(p), bb = new Bot(p);
                    for (int i = 0; i < 9 * a.OndaPassos; i++)
                    {
                        ba.Step(a); bb.Step(b);
                        if (i % 7 == 0) LeTudo(a);
                        if (!a.Events.SequenceEqual(b.Events)) Assert.Fail(p.Name + " passo " + a.Steps);
                    }
                    Assert.AreEqual(b.Save("t", 1), a.Save("t", 1), p.Name);
                }
        }

        [Test]
        public void Alertas_DoCriticoAoInformativo_ESoComMotivoQueExiste()
        {
            var s = new Sim(new Tuning(), 5);
            var bot = new Bot(BotProfile.Parado);
            var vistos = new HashSet<TipoAlerta>();
            for (int i = 0; i < 9 * s.OndaPassos; i++)
            {
                bot.Step(s);
                if (i % 5 != 0) continue;
                List<Alerta> al = s.Alertas();
                for (int k = 1; k < al.Count; k++) Assert.LessOrEqual(al[k - 1].Gravidade, al[k].Gravidade, "passo " + s.Steps);
                foreach (Alerta a in al)
                {
                    vistos.Add(a.Tipo);
                    switch (a.Tipo)
                    {
                        case TipoAlerta.FilaCritica: Assert.AreEqual(Estado.Critico, s.Estacoes[a.Indice].Estado); Assert.AreEqual(Alvo.Estacao, a.Alvo); break;
                        case TipoAlerta.FilaAtencao: Assert.AreEqual(Estado.Atencao, s.Estacoes[a.Indice].Estado); break;
                        case TipoAlerta.ContaAtrasada: Assert.IsTrue(s.Pausado); Assert.AreEqual(s.Atrasada, a.Valor); break;
                        case TipoAlerta.VooAtrasado: Assert.AreEqual(SituacaoVoo.Atrasado, s.Situacao(a.Indice)); Assert.AreEqual(Alvo.Voo, a.Alvo); break;
                    }
                }
            }
            Assert.IsTrue(vistos.Contains(TipoAlerta.FilaCritica), "parado, a fila fica critica: " + string.Join(" ", vistos));
        }

        [Test]
        public void Alertas_CaixaPaneSujeiraBanheiroESatisfacao_ApontamParaOLugar()
        {
            var s = new Sim(new Tuning { TripTeto = new[] { 9, 9, 9, 9 } }, 1) { Cash = 100 };
            if (s.Steps == 0) s.Tick(Sim.Step);
            s.Steps = (s.DiaJogo + 1) * DiaPassos(s) - 1;
            s.Tick(Sim.Step); s.Tick(Sim.Step);
            Assert.IsTrue(s.Pausado);
            Alerta caixa = s.Alertas()[0];
            Assert.AreEqual((TipoAlerta.ContaAtrasada, Gravidade.Critico, Alvo.Caixa, s.Atrasada), (caixa.Tipo, caixa.Gravidade, caixa.Alvo, caixa.Valor));

            int tol = s.T.TolSujeira.Min();
            s.Sujeira[(int)Area.Loja] = tol;
            s.Sujeira[(int)Area.CheckIn] = tol - 1;
            Alerta suja = s.Alertas().Single(a => a.Tipo == TipoAlerta.Sujeira);
            Assert.AreEqual((Gravidade.Informativo, Alvo.Area, (int)Area.Loja), (suja.Gravidade, suja.Alvo, suja.Indice));

            s.Banheiro.Interditado = true;
            s.Sujeira[(int)Area.Banheiro] = Sim.SujeiraMax;
            List<Alerta> al = s.Alertas();
            Assert.AreEqual(1, al.Count(a => a.Tipo == TipoAlerta.BanheiroFechado && a.Alvo == Alvo.Area && a.Indice == (int)Area.Banheiro));
            Assert.IsFalse(al.Any(a => a.Tipo == TipoAlerta.Sujeira && a.Indice == (int)Area.Banheiro), "o banheiro fechado ja diz a sujeira");

            var semLimpeza = new Sim(new Tuning { RegraLimpeza = false }, 1);
            semLimpeza.Sujeira[(int)Area.Loja] = Sim.SujeiraMax;
            Assert.IsFalse(semLimpeza.Alertas().Any(a => a.Tipo == TipoAlerta.Sujeira), "sem a regra, nada de limpeza no painel");

            // a pane so vem acima do aviso de desgaste (ClimaFalhasTests.NoPatio)
            Sim p = EstacoesPerfisTests.Pronta(ClimaFalhasTests.ComFalha(TipoFalha.Pane));
            p.Cash = 1000;
            RegrasTests.Run(p, 21f);
            p.Avioes[0].Desgaste = 85;
            Assert.IsNotNull(ClimaFalhasTests.Espera(p, Ev.Pane));
            Aviao pane = p.Avioes.First(a => a.Pane);
            Assert.IsTrue(p.Alertas().Any(a => a.Tipo == TipoAlerta.Pane && a.Gravidade == Gravidade.Critico && a.Alvo == Alvo.Aviao && a.Indice == pane.Id));
        }

        [Test]
        public void SatisfacaoBaixa_LevaAoMotivoQueMaisPesa()
        {
            var s = new Sim(new Tuning(), 5);   // ninguem atende: a fila chega ao teto e todo mundo perde o voo
            for (int i = 0; i < 12 * s.OndaPassos && !(s.SatAmostras > 0 && s.SatMedia < s.T.SatReclama); i++) s.Tick(Sim.Step);
            Assert.That(s.SatAmostras > 0 && s.SatMedia < s.T.SatReclama, "parado, a media cai");
            Alerta a = s.Alertas().Single(x => x.Tipo == TipoAlerta.SatisfacaoBaixa);
            Fator f = s.SatTop(0);
            Assert.AreEqual(((long)(int)f, s.SatOnde(f)), (a.Valor, a.Indice));
            Assert.AreEqual(f == Fator.Pontualidade ? Alvo.Voo : f == Fator.Limpeza || f == Fator.Banheiro ? Alvo.Area : Alvo.Estacao, a.Alvo);
        }

        [Test]
        public void Partidas_SeguemOVoo_PrevistoEmbarcandoDecolou()
        {
            Sim s = EstacoesPerfisTests.Pronta(BalanceTests.SemFaseC4());
            var bot = new Bot(BotProfile.Bom);
            var vistas = new HashSet<SituacaoVoo>();
            for (int i = 0; i < 3 * s.OndaPassos; i++)
            {
                bot.Step(s);
                for (int f = 0; f < 2; f++)
                {
                    LinhaVoo l = s.Partida(f);
                    Voo v = s.Voos[f];
                    vistas.Add(l.Situacao);
                    if (v == null) continue;
                    Assert.AreEqual((v.Id, v.Total, v.Embarcados, f), (l.Id, l.Total, l.Embarcados, l.Vaga));
                    if (v.Embarque && !v.Partiu) Assert.AreEqual(SituacaoVoo.Embarcando, l.Situacao);
                    if (!v.Embarque && !v.Partiu && s.PassoNaOnda < v.AbrePasso && v.Total > 0) Assert.AreEqual(SituacaoVoo.Previsto, l.Situacao);
                }
                Assert.AreEqual(s.Avioes.Count(a => a.NoPatio), s.AvioesEmSolo);
            }
            CollectionAssert.IsSubsetOf(new[] { SituacaoVoo.Previsto, SituacaoVoo.Embarcando, SituacaoVoo.Decolou }, vistas.ToArray());
        }

        [Test]
        public void Relatorio_BateComOFechamentoDaFrenteA_EContaOsPerdidosUmaVezPorVoo()
        {
            Tuning t = BalanceTests.SemFaseC4();
            t.RegraIncidentes = false;
            var s = new Sim(t, 2);
            var bot = new Bot(BotProfile.Parado);
            Assert.IsNull(s.Ontem);
            while (s.DiaJogo == 0)
            {
                bot.Step(s);
                Assert.AreEqual(s.Hoje.Receita, s.ReceitaHoje);
                Assert.AreEqual(s.Folha(), s.DespesaHoje);
                Assert.AreEqual(s.ReceitaHoje - s.DespesaHoje, s.SaldoHoje);
                if (s.DiaJogo == 0) Assert.AreEqual(s.TotalPerdidos, s.PerdidosHoje, "parado, so o voo perdido entra, 1 por voo");
            }
            s.Tick(Sim.Step);   // o fechamento sai no 1o passo do dia seguinte
            Assert.AreSame(s.Historico.Last(), s.Ontem);
            Assert.IsTrue(s.OcorrenciasHoje().All(o => o.Dia == s.DiaJogo));
            Assert.AreSame(s.Ocorrencias.LastOrDefault(), s.UltimaOcorrencia);
            Assert.AreEqual(Enum.GetValues(typeof(Fator)).Cast<Fator>().Sum(f => s.Reclamacoes(f)), s.ReclamacoesHoje);
        }

        [Test]
        public void Congestionada_APiorFila_SemFilaMenosUm()
        {
            var s = new Sim(new Tuning(), 1);
            Assert.AreEqual(-1, s.Congestionada);
            var bot = new Bot(BotProfile.Parado);
            for (int i = 0; i < 2 * s.OndaPassos; i++)
            {
                bot.Step(s);
                int c = s.Congestionada;
                if (c < 0) { Assert.IsTrue(s.Estacoes.All(x => !s.Desbloqueada(x.Tipo) || x.Fila.Count == 0)); continue; }
                foreach (Station x in s.Estacoes)
                    if (s.Desbloqueada(x.Tipo) && x.Fila.Count > 0) Assert.IsTrue(x.Estado < s.Estacoes[c].Estado || x.Estado == s.Estacoes[c].Estado && x.Fila.Count <= s.Estacoes[c].Fila.Count);
            }
        }
    }
}
