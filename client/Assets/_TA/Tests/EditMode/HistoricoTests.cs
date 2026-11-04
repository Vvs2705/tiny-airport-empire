using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Registro de ocorrencias (frente ta-historico, Sim.Historico.cs): cada tipo com causa, resposta, resultado e a chave da
    /// explicacao; so observa (desligado = o mesmo jogo, numero a numero); retencao, teto do dia, lixo, retomada, falta de caixa e migracao dos schemas 9 e 10.</summary>
    public class HistoricoTests
    {
        static Sim Pronta(Tuning t) => EstacoesPerfisTests.Pronta(t);

        /// <summary>Sem incidente, clima nem falha: so o voo perdido ou atrasado entra.</summary>
        static Tuning Calmo(Tuning t = null)
        {
            t = BalanceTests.SemFaseC4(t);
            t.RegraIncidentes = false;
            return t;
        }

        /// <summary>Anda ate a ocorrencia que `achou` aceitar (null = nao veio em `seg` s).</summary>
        static Ocorrencia Ate(Sim s, Func<Ocorrencia, bool> achou, float seg = 400f)
        {
            for (int i = 0; i < seg * Sim.Hz; i++)
            {
                Ocorrencia o = s.Ocorrencias.FirstOrDefault(achou);
                if (o != null) return o;
                s.Tick(Sim.Step);
            }
            return null;
        }

        // ponytail: a media movel (ema) do save real foi gravada pelo dotnet; o Mono do Unity refaz a conta com diferenca na 8a casa (rodada da
        // leva 2, 04/11), entao a comparacao arredonda o ema em 6 casas. O resto do save continua comparado caractere a caractere.
        static string SemHist(string save) => string.Join("\n", save.Split('\n').Where(l => !l.StartsWith("hist=") && !l.StartsWith("sha256="))
            .Select(l => l.StartsWith("ema=") ? "ema=" + double.Parse(l.Substring(4), System.Globalization.CultureInfo.InvariantCulture).ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture) : l));

        static string Hist(string save) => save.Split('\n').Single(l => l.StartsWith("hist="));

        static string Campos(Ocorrencia o) => string.Join(",", o.Passo, o.Dia, o.Tipo, o.Sub, o.Causa, o.Voo, o.Total, o.Perdidos, o.Atraso, o.Resposta, o.Auto,
            o.Custo, o.Motivo, o.Estacao, o.Espera, o.Postos, o.Aberta);

        // ------------------------------------------------------------------ so observa

        /// <summary>Os cenarios que exercitam cada caminho do registro: o padrao, os 4 incidentes, um clima e as 3 falhas de aviao.</summary>
        static readonly Func<Tuning>[] Cenarios =
        {
            () => new Tuning(),
            () => RotasIncidentesTests.Com(TipoIncidente.Pico), () => RotasIncidentesTests.Com(TipoIncidente.Conexao),
            () => RotasIncidentesTests.Com(TipoIncidente.Falha), () => RotasIncidentesTests.Com(TipoIncidente.FrenteFria),
            () => ClimaFalhasTests.ComClima(TipoIncidente.Temporal),
            () => ClimaFalhasTests.ComFalha(TipoFalha.Pane), () => ClimaFalhasTests.ComFalha(TipoFalha.Rejeicao), () => ClimaFalhasTests.ComFalha(TipoFalha.Retorno),
        };

        /// <summary>A prova de que so observa: com o registro desligado, os mesmos eventos, passo a passo, e o mesmo save sem a linha dele.</summary>
        [Test]
        public void Invariante_SoObserva_DesligadoDaOMesmoJogo_NumeroANumero()
        {
            foreach (Func<Tuning> cen in Cenarios)
                foreach (BotProfile p in new[] { BotProfile.Bom, BotProfile.Parado })
                {
                    Tuning liga = cen(), desliga = cen();
                    desliga.RegraHistorico = false;
                    Sim a = new Sim(liga, 3), b = new Sim(desliga, 3);
                    Bot ba = new Bot(p), bb = new Bot(p);
                    for (int i = 0; i < 10 * a.OndaPassos; i++)
                    {
                        ba.Step(a); bb.Step(b);
                        if (a.Events.Count != b.Events.Count || !a.Events.SequenceEqual(b.Events)) Assert.Fail(p.Name + " passo " + a.Steps + ": " + string.Join(" ", a.Events));
                    }
                    Assert.AreEqual(SemHist(b.Save("t", 1)), SemHist(a.Save("t", 1)), p.Name);
                    Assert.IsNotEmpty(a.Ocorrencias, p.Name + ": o cenario anota");
                    Assert.IsEmpty(b.Ocorrencias, "desligado nao anota");
                    StringAssert.DoesNotContain("\nhist=", b.Save("t", 1), "sem nenhuma, a chave fica de fora");
                }
        }

        // ------------------------------------------------------------------ incidentes e clima

        [Test]
        public void Incidente_EntraNoAviso_FechaComOResultadoDoVoo()
        {
            Sim s = Pronta(RotasIncidentesTests.Com(TipoIncidente.Pico));
            Incidente i = RotasIncidentesTests.AteOAviso(s);
            Voo v = s.Voos[i.Vaga];
            s.Tick(Sim.Step);   // o Ev.IncidenteAviso sai no passo seguinte ao que o Inc aparece
            Ocorrencia o = s.Ocorrencias.Single();
            Assert.AreEqual((TipoOcorrencia.Incidente, (int)TipoIncidente.Pico, v.Id, i.Inicio, true, Opcao.Segurar), (o.Tipo, o.Sub, o.Voo, o.Passo, o.Aberta, o.Resposta),
                "entra no aviso, em andamento, com a hora em que comeca");
            Assert.AreEqual(s.HoraDe(i.Inicio), s.HoraDe(o.Passo));
            RotasIncidentesTests.Ate(s, i.Fim + 1);
            Assert.IsTrue(v.Partiu);
            Assert.AreEqual((false, v.Total, v.Perdidos, v.Perdidos == 0), (o.Aberta, o.Total, o.Perdidos, o.Eficaz));
            Assert.AreEqual(v.Perdidos == 0, o.Motivo == Motivo.Nenhum, "com perdidos, diz onde travou");
            Assert.IsFalse(s.Ocorrencias.Any(x => x != o && x.Voo == v.Id), "o voo do incidente nao ganha outro registro");
        }

        [Test]
        public void Falha_RepararNoAviso_GuardaEstacaoECusto_OMecanicoNaEstacaoEAutomatico()
        {
            Sim s = Pronta(RotasIncidentesTests.Com(TipoIncidente.Falha));
            Incidente i = RotasIncidentesTests.AteOAviso(s);
            Assert.IsTrue(s.Repara());
            Ocorrencia o = s.Ocorrencias.Single();
            Assert.AreEqual(((int)i.Estacao, Opcao.Reparar, false, (long)s.T.ReparoCusto), (o.Causa, o.Resposta, o.Auto, o.Custo));

            Sim m = EstacoesPerfisTests.Pronta(RotasIncidentesTests.Com(TipoIncidente.Falha), Perfil.Mecanico);
            Incidente j = RotasIncidentesTests.AteOAviso(m);
            Assert.IsTrue(m.Move(3, j.Estacao));
            m.Staff[3].Andando = 0;
            RegrasTests.Run(m, m.T.ReparoSeg + 0.1f);
            Ocorrencia a = m.Ocorrencias.Single();
            Assert.AreEqual((Opcao.Reparar, true, 0L), (a.Resposta, a.Auto, a.Custo), "o mecanico repara sozinho e de graca");
        }

        [Test]
        public void Clima_Adiantar_EficazEComOTipoDoClima_SemResposta_OPortaoParadoExplica()
        {
            Sim s = Pronta(RotasIncidentesTests.Com(TipoIncidente.FrenteFria));
            Incidente i = RotasIncidentesTests.AteOAviso(s);
            Assert.IsTrue(s.Adianta());
            RotasIncidentesTests.Ate(s, i.Fim + 1);
            Ocorrencia o = s.Ocorrencias.Single(x => x.Sub == (int)TipoIncidente.FrenteFria);
            Assert.AreEqual((TipoOcorrencia.Clima, Opcao.Adiantar, 0, Motivo.Nenhum), (o.Tipo, o.Resposta, o.Perdidos, o.Motivo));

            Sim n = Pronta(RotasIncidentesTests.Com(TipoIncidente.FrenteFria));
            Incidente j = RotasIncidentesTests.AteOAviso(n);
            Voo v = n.Voos[j.Vaga];
            Ocorrencia p = Ate(n, x => x.Sub == (int)TipoIncidente.FrenteFria && !x.Aberta);
            Assert.AreEqual((Opcao.Segurar, v.Perdidos), (p.Resposta, p.Perdidos));
            Assert.Greater(p.Perdidos, 0, "segurar na frente fria perde quem nao embarcou");
            Assert.AreEqual((Motivo.Clima, Estacao.Portao), (p.Motivo, p.Estacao), "travou no portao com a pista fechada");
        }

        // ------------------------------------------------------------------ voo perdido e atrasado

        [Test]
        public void VooPerdido_SegurancaSemNinguem_ExplicaSemEquipe()
        {
            Sim s = Pronta(Calmo());
            Assert.IsTrue(s.Move(1, Estacao.Loja));
            Ocorrencia o = Ate(s, x => x.Tipo == TipoOcorrencia.VooPerdido);
            Voo v = s.Voos[o.Voo % 2];
            Assert.AreEqual((v.Id, v.Total, v.Perdidos, false), (o.Voo, o.Total, o.Perdidos, o.Aberta));
            Assert.AreEqual((Motivo.SemEquipe, Estacao.Seguranca, 0), (o.Motivo, o.Estacao, o.Postos));
            Assert.IsFalse(o.Eficaz);
        }

        /// <summary>Quem perdeu o voo em duas estacoes: a explicacao fala da que segurou mais gente, nao da ultima.</summary>
        [Test]
        public void VooPerdido_ExplicaOndeFicouAMaioria()
        {
            Sim s = Pronta(Calmo());
            Assert.IsTrue(s.Move(0, Estacao.Loja) && s.Move(2, Estacao.Loja), "check-in e portao sem ninguem");
            s.Tick(Sim.Step);
            for (int i = 400; i < 402; i++) s.St(Estacao.Portao).Fila.Add(s.NovoPax(Sim.PaxId(0, 0, i)));
            Ocorrencia o = Ate(s, x => x.Tipo == TipoOcorrencia.VooPerdido);
            Assert.AreEqual((0, Estacao.CheckIn, Motivo.SemEquipe), (o.Voo, o.Estacao, o.Motivo), "a maioria ficou no check-in, 2 no portao");
        }

        /// <summary>O exemplo da frente: "voo perdido: fila da seguranca acima de 6 min com 1 posto aberto" (1 s de jogo = 1 min no quadro).</summary>
        [Test]
        public void VooPerdido_FilaLenta_ExplicaComEsperaEPostosAbertos()
        {
            Tuning t = Calmo();
            t.Tempo = (float[])t.Tempo.Clone();
            t.Tempo[(int)Estacao.Seguranca] = 20f;
            Sim s = Pronta(t);
            Ocorrencia o = Ate(s, x => x.Tipo == TipoOcorrencia.VooPerdido);
            Assert.AreEqual((Motivo.Fila, Estacao.Seguranca, 1), (o.Motivo, o.Estacao, o.Postos), "2 balcoes, 1 agente");
            Assert.Greater(o.Espera, 6);
        }

        [Test]
        public void VooAtrasado_PatioLento_ExplicaEGuardaOAtrasoNaDecolagem()
        {
            Tuning t = Calmo();
            t.ServicoSeg = new[] { 12f, 18f, 130f, 24f };
            Sim s = Pronta(t);
            Ocorrencia o = Ate(s, x => x.Tipo == TipoOcorrencia.VooAtrasado);
            Assert.AreEqual((Motivo.Patio, true, 0), (o.Motivo, o.Aberta, o.Perdidos), "entra no portao, espera a decolagem");
            Evento? d;
            do d = ClimaFalhasTests.Espera(s, Ev.Decolou); while (d.Value.B != o.Voo);
            Assert.AreEqual((d.Value.C, false, false), (o.Atraso, o.Aberta, o.Eficaz));
            Assert.Greater(o.Atraso, 20);
        }

        // ------------------------------------------------------------------ falhas de aviao

        static Sim NoPatio(TipoFalha f)
        {
            Sim s = Pronta(ClimaFalhasTests.ComFalha(f));
            s.Cash = 1000;
            RegrasTests.Run(s, 21f);
            s.Avioes[0].Desgaste = 85;
            return s;
        }

        [Test]
        public void Pane_SemPagar_InspecaoAutomatica_EOVooPerdidoPelaPane()
        {
            Sim s = NoPatio(TipoFalha.Pane);
            s.Cash = 0;
            Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.Pane));
            Voo v = s.Voos[0];
            Ocorrencia o = s.Ocorrencias.Single();
            Assert.AreEqual((TipoOcorrencia.FalhaAviao, (int)TipoFalha.Pane, 85, v.Id, true), (o.Tipo, o.Sub, o.Causa, o.Voo, o.Aberta));
            RegrasTests.Run(s, 111f);
            Assert.AreEqual((Opcao.Reparar, true, 0L), (o.Resposta, o.Auto, o.Custo), "a inspecao obrigatoria resolve sozinha");
            Assert.AreEqual((v.Total, Motivo.Pane, Estacao.Portao, false), (o.Perdidos, o.Motivo, o.Estacao, o.Aberta), "fecha quando a inspecao acaba");
            Assert.IsFalse(s.Ocorrencias.Any(x => x != o && x.Voo == v.Id), "o voo da pane nao ganha outro registro");
        }

        [Test]
        public void Pane_ReparoPago_GuardaOCusto()
        {
            Sim s = NoPatio(TipoFalha.Pane);
            Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.Pane));
            RegrasTests.Run(s, 0.1f);
            Assert.IsTrue(s.ReparaAviao(0));
            Ocorrencia o = s.Ocorrencias.Single();
            Assert.AreEqual((Opcao.Reparar, false, (long)s.T.PaneCusto), (o.Resposta, o.Auto, o.Custo));
        }

        [Test]
        public void Rejeicao_FechaNaDecolagemComOAtraso()
        {
            Sim s = NoPatio(TipoFalha.Rejeicao);
            Evento? r = ClimaFalhasTests.Espera(s, Ev.Rejeicao);
            Ocorrencia o = s.Ocorrencias.Single(x => x.Tipo == TipoOcorrencia.FalhaAviao);
            Assert.AreEqual(((int)TipoFalha.Rejeicao, r.Value.B, true), (o.Sub, o.Voo, o.Aberta));
            Evento? d = ClimaFalhasTests.Espera(s, Ev.Decolou, s.T.RejeicaoSeg + 2f);
            Assert.AreEqual((0, o.Voo), (d.Value.A, d.Value.B));
            Assert.AreEqual((d.Value.C, false), (o.Atraso, o.Aberta));
            Assert.GreaterOrEqual(o.Atraso, (int)s.T.RejeicaoSeg - 1);
        }

        [Test]
        public void Retorno_GuardaOsRemarcados_EFicaAbertoAteAPaneDoPousoAcabar()
        {
            Sim s = NoPatio(TipoFalha.Retorno);
            Evento? d = ClimaFalhasTests.Espera(s, Ev.Decolou);
            Voo v = s.Voos[0];
            Ocorrencia o = s.Ocorrencias.Single(x => x.Tipo == TipoOcorrencia.FalhaAviao);
            Assert.AreEqual(((int)TipoFalha.Retorno, d.Value.B, v.Embarcados, 0, true, 85), (o.Sub, o.Voo, o.Total, o.Perdidos, o.Aberta, o.Causa));
            Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.PousoSeguro, s.T.RetornoPede + s.T.RetornoVolta + 1f));
            RegrasTests.Run(s, 0.1f);
            Assert.AreEqual((Opcao.Reparar, true, true), (o.Resposta, o.Auto, o.Aberta), "a pane do pouso cai na inspecao obrigatoria");
            Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.PaneReparada, s.T.InspecaoSeg + 1f));
            Assert.IsFalse(o.Aberta, "o aviao voltou ao normal");
        }

        // ------------------------------------------------------------------ retencao, teto, save

        static string Reg(long passo, int voo = 0) => passo + ",3,-1,0," + voo + ",10,4,0,0,0,0,1,1,30,1,0";

        [Test]
        public void Retencao_TresDiasDoJogo_ODiaQueViraSaiSemEvento()
        {
            Sim s = Pronta(Calmo());
            long dia = s.OndaPassos * (long)s.T.DiaOndas;
            Assert.IsTrue(s.Move(1, Estacao.Loja));   // perde voos todo dia
            RotasIncidentesTests.Ate(s, 3 * dia + 10);
            Assert.AreEqual(3, s.DiaJogo);
            Assert.IsTrue(s.Ocorrencias.All(o => o.Dia >= 1 && o.Dia == (int)(o.Passo / dia)), "o dia 0 saiu");
            Assert.IsTrue(s.Ocorrencias.Any(o => o.Dia == 1));
            foreach (var g in s.Ocorrencias.GroupBy(o => o.Dia)) Assert.LessOrEqual(g.Count(), Sim.OcorrenciasPorDia);

            Sim r = Sim.Load(SaveEnvelope.Wrap("t=" + (3 * dia - 1) + "\nhist=" + Reg(5) + ";" + Reg(dia + 5) + "\n", "x", 1));
            Assert.AreEqual(2, r.Ocorrencias.Count);
            r.Steps++;   // o dia vira sem ocorrencia nova: quem le e quem grava podam
            StringAssert.DoesNotContain("hist=" + Reg(5), r.Save("t", 1));
            Assert.AreEqual(new[] { dia + 5 }, r.Ocorrencias.Select(o => o.Passo).ToArray(), "o dia 0 saiu");
        }

        [Test]
        public void TetoDoDia_FicamAsMaisNovas()
        {
            string hist = string.Join(";", Enumerable.Range(0, 30).Select(i => Reg(i, i % 2)));
            Sim s = Sim.Load(SaveEnvelope.Wrap("t=100\nhist=" + hist + "\n", "x", 1));
            Assert.AreEqual(Enumerable.Range(6, Sim.OcorrenciasPorDia).Select(i => (long)i), s.Ocorrencias.Select(o => o.Passo), "o teto fica com as mais novas");

            Sim p = Pronta(Calmo());
            Assert.IsTrue(p.Move(1, Estacao.Loja));
            string cheio = string.Join(";", Enumerable.Range(0, Sim.OcorrenciasPorDia).Select(i => Reg(i, -1)));
            Sim q = Sim.Load(SaveEnvelope.Wrap(SaveEnvelope.Unwrap(p.Save("t", 1)).Payload + "hist=" + cheio + "\n", "t", 1), Calmo(), 1);
            Assert.IsFalse(q.SaveRefused);
            Ocorrencia o = Ate(q, x => x.Tipo == TipoOcorrencia.VooPerdido && x.Voo >= 0);
            Assert.AreEqual(Sim.OcorrenciasPorDia, q.Ocorrencias.Count);
            Assert.AreEqual((1L, o), (q.Ocorrencias[0].Passo, q.Ocorrencias.Last()), "a mais antiga do dia saiu para a nova entrar");
        }

        [Test]
        public void LixoDoHistorico_ViraPadraoLimitado()
        {
            const string lixo = "-5,3,-1,0,0,10,4,0,0,0,0,1,1,30,1,0;99999999,3,-1,0,0,10,4,0,0,0,0,1,1,30,1,0;x,3,-1,0,0,10,4,0,0,0,0,1,1,30,1,0;7,3,-1;" +
                "8,99,99,999,99999,99999,99999,99999,99,1,-50,99,99,99999,99,1;9,-3,-9,-1,-9,-1,-1,-1,-1,0,9999999999999,-1,-1,-1,-1,0";
            Sim s = Sim.Load(SaveEnvelope.Wrap("t=90\nonda=0\nhist=" + lixo + "\n", "x", 1));
            Assert.AreEqual(new[] { 8L, 9L }, s.Ocorrencias.Select(o => o.Passo).ToArray(), "negativo, longe no futuro, torto e curto saem");
            Ocorrencia a = s.Ocorrencias[0], b = s.Ocorrencias[1];
            Assert.AreEqual((TipoOcorrencia.FaltaCaixa, 7, 100, 1, 499, 499, 3600, Opcao.Hangar, 0L, Motivo.Suprimento, Estacao.Despacho, 3600, 8),
                (a.Tipo, a.Sub, a.Causa, a.Voo, a.Total, a.Perdidos, a.Atraso, a.Resposta, a.Custo, a.Motivo, a.Estacao, a.Espera, a.Postos));
            Assert.AreEqual((TipoOcorrencia.Incidente, -1, 0, -1, 0, 0, 0, Opcao.Segurar, (long)int.MaxValue, Motivo.Nenhum, Estacao.CheckIn, 0, 0),
                (b.Tipo, b.Sub, b.Causa, b.Voo, b.Total, b.Perdidos, b.Atraso, b.Resposta, b.Custo, b.Motivo, b.Estacao, b.Espera, b.Postos));
        }

        /// <summary>Fechar e reabrir no meio de tudo (no aviso, no incidente, com a pane esperando resposta) da o mesmo registro de nao fechar.</summary>
        [Test]
        public void Retomada_ReabrirNoMeio_DaOMesmoRegistro()
        {
            foreach (Func<Tuning> cen in Cenarios)
            {
                Sim a = new Sim(cen(), 5), b = new Sim(cen(), 5);
                Bot ba = new Bot(BotProfile.Bom), bb = new Bot(BotProfile.Bom);
                bool abertaNoSave = false;
                for (int i = 0; i < 8 * a.OndaPassos; i++)
                {
                    if (b.Steps % (b.OndaPassos / 7) == 0)
                    {
                        abertaNoSave |= b.Ocorrencias.Any(o => o.Aberta);
                        b = Sim.Load(b.Save("t", 1000 + b.Steps), cen(), 5);
                    }
                    ba.Step(a); bb.Step(b);
                }
                Assert.IsNotEmpty(a.Ocorrencias);
                Assert.AreEqual(a.Ocorrencias.Select(Campos), b.Ocorrencias.Select(Campos));
                Assert.AreEqual(Hist(a.Save("t", 9)), Hist(b.Save("t", 9)), "o resto do save e' dos SaveTests (o ema anda diferente ao reabrir)");
                if (cen != Cenarios[0]) Assert.IsTrue(abertaNoSave, "o cenario grava com ocorrencia em andamento");
            }
        }

        [Test]
        public void IdaEVolta_ComOcorrencias_TextoEstavel()
        {
            Sim s = new Sim(RotasIncidentesTests.Com(TipoIncidente.Pico), 2);
            var b = new Bot(BotProfile.Parado);
            for (int i = 0; i < 4 * s.OndaPassos + s.OndaPassos / 2; i++) b.Step(s);   // no meio da onda (na virada, o save da main ja nao e' estavel)
            string a = s.Save("t", 1);
            StringAssert.Contains("\nhist=", a);
            Assert.AreEqual(a, Sim.Load(a, s.T, 2).Save("t", 1));
        }

        // ------------------------------------------------------------------ falta de caixa (frente A)

        /// <summary>O fechamento sem caixa entra com o que faltou e a explicacao; fecha quando o caixa quita sozinho (automatico).</summary>
        [Test]
        public void FaltaDeCaixa_EntraNoFechamento_FechaQuandoOCaixaQuita()
        {
            Sim s = Pronta(Calmo());
            long dia = s.OndaPassos * (long)s.T.DiaOndas;
            RotasIncidentesTests.Ate(s, dia);
            s.Cash = 0;
            Evento? f = ClimaFalhasTests.Espera(s, Ev.DiaFechado, 1f);
            Assert.Greater(f.Value.C, 0, "o caixa vazio nao cobre a conta");
            Ocorrencia o = s.Ocorrencias.Single(x => x.Tipo == TipoOcorrencia.FaltaCaixa);
            Assert.AreEqual(((long)f.Value.C, Motivo.Caixa, true, false, 1), (o.Custo, o.Motivo, o.Aberta, o.Eficaz, o.Dia));
            s.Cash = 100000;
            Assert.IsNotNull(ClimaFalhasTests.Espera(s, Ev.ContaQuitada, 1f));
            Assert.AreEqual((false, true, Opcao.Segurar), (o.Aberta, o.Auto, o.Resposta), "o caixa quitou sozinho");
        }

        // ------------------------------------------------------------------ migracao do schema 10

        /// <summary>Save real do schema 10 (a main de 04/11 com a frente A, semente 7, 2 790 s do Bom).</summary>
        const string V10 =
            "sha256=d6ace12d0ccc0f0937b0681c95f044f5b14231ff596271299943cdf8d8753862\nschema=10\ncontent=0.10.0\nat=1800000000\nt=83700\ncash=4424\nseed=" +
            "7\nonda=15\nemb=996\nperd=47\nalbum=1\nbst=0\nema=9.359414870706443\nsaved=1800000000\nclaim=0\nclaimcash=0\ndia=0\nest.0=5,2\nfila.0=15501;" +
            "15502;15503;15504;15505;15506;15507;15508;15509;15510;15511;15512;15513;15514;15515;15516;15517;15518;15519;15520;15521;15522;15523;15524;15" +
            "525;15526;15527;15528;15529;15530;15531;15532;15533;15534\nat.0=15044:2;15045:32;15500:62\nest.1=5,2\nat.1=15042:40;15043:70\nest.2=0,0\nest" +
            ".3=5,2\nfila.3=15011;15016;15015;15017;15014;15020;15019;15018;15022;15023;15021;15024;15025;15026;15027;15028;15029;15031;15030;15033;15032" +
            ";15035;15036;15034;15037;15038;15039;15040;15041\nest.4=5,2\nest.5=5,2\nest.6=5,2\nest.7=0,0\nfunc.0=1,0,0,0\nfunc.1=1,0,0,1\nfunc.2=4,0,0,3" +
            "\nfunc.3=5,1,0,2\nfunc.4=6,0,0,4\nfunc.5=7,1,0,5\nvoo.0=46,13,182,1,0\nvoo.1=35,0,0,0,0\naviao.0=50,82200,1,30,-1,0,0,1,0,0\nsrv.0=0:0,0:0,0" +
            ":0,0:0\naviao.1=100,83700,0,-1,-1,0,0,1,0,0\nsrv.1=0:0,0:0,0:0,0:0\ntrip.0=0,1,3872,0\ntrip.1=0,1,5400,1\ntrip.2=0,3,1499,-1\ntrip.3=1,1,384" +
            "2,0\ntrip.4=1,1,5400,1\ntrip.5=1,3,1499,-1\npatio=3\npatioatraso=7\naero=1,3,-1\nrotas=127\nprog=6,6\nvr.0=6,46,0,0,0,0,1800,0\nvr.1=6,46,0," +
            "0,0,0,3300,0\nav.0=6,15\nav.1=6,15\nstt.0=1,31,5.666665818542242\nstt.1=0,-1,27.900001425296068\nstt.3=1,30,8.099999587982893\nreparo=0\ninc" +
            "w=14\ncamp.0=6,10,0,0,3,0,1,0,1,3,0\ncamp.1=1,0,1,40,40,3,0,1,0,2,0\ncamp.2=0,0,0,0,0,0,0,0,0,0,0\nmiss=1,0\nmd.0=14,3,0,0\nmd.1=12,30,0,1\n" +
            "md.2=6,300,0,1\nms.0=4,30,1,0\nms.1=2,10,0,0\nms.2=3,20,0,0\nms.3=1,0,5,0\nms.4=0,996,0,0\ncq=34848,0,0,30,30,25,4,21,93,0,208,241,25,0,2,0," +
            "0,0,0,0,5\ngems=0\npasse=0,0,325,3\natrasada=0\nhoje=1,0,7336,960,0,1728,1772,950,0,0,0,0,0,0\ndc.0=0,585,4570,445,0,528,1000,790,0,0,195,10" +
            "8,54,228\n";

        /// <summary>O schema 10 nao tem a chave: o registro comeca vazio, e o jogo e' o mesmo de jogar ate ali com o codigo novo, antes e depois.</summary>
        [Test]
        public void MigracaoDoSchema10_SaveReal_RegistroVazio_EOMesmoJogo()
        {
            const long U = 1800000000;
            var t = new Tuning { RegraVestiario = false, RegraLimpeza = false, RegraSuprimentos = false };   // o jogo que o schema 10 gravava: sem o vestiario (frente E), a limpeza e os suprimentos
            Sim m = Sim.Load(V10, t, 7);
            Assert.IsFalse(m.SaveRefused, "schema 10 migra, nao e' recusado");
            Assert.AreEqual((4424L, 83700L), (m.Cash, m.Steps));
            Assert.IsEmpty(m.Ocorrencias);

            Sim n = new Sim(t, 7);
            var bn = new Bot(BotProfile.Bom);
            for (int i = 0; i < 2790 * Sim.Hz; i++) bn.Step(n);
            Assert.IsNotEmpty(n.Ocorrencias, "o codigo novo ja anotou ate ali");
            string texto = m.Save("0.10.0", U);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", texto);
            Assert.AreEqual(SemHist(n.Save("0.10.0", U)), SemHist(texto), "migrar = jogar ate ali com o codigo novo, fora o registro");

            var bm = new Bot(BotProfile.Bom);
            for (int i = 0; i < 1800 * Sim.Hz; i++) { bm.Step(m); bn.Step(n); }
            Assert.IsNotEmpty(m.Ocorrencias, "anota dali em diante");
            Assert.AreEqual(SemHist(n.Save("0.10.0", U + 1)), SemHist(m.Save("0.10.0", U + 1)));
            string depois = m.Save("0.10.0", U + 1);
            Assert.AreEqual(depois, Sim.Load(depois, t, 7).Save("0.10.0", U + 1), "ida e volta estavel no schema 11");
        }

        /// <summary>Save real do schema 9 (a main de 03/11, antes da frente A, semente 7, 2 790 s do Bom).</summary>
        const string V09 =
            "sha256=e66c9074640868e36d4e7aa5599d866b1d93ee058b6f5f4d7e1a84c442c21485\nschema=9\ncontent=0.9.0\nat=1800000000\nt=83700\ncash=5137\nseed=7" +
            "\nonda=15\nemb=996\nperd=47\nalbum=1\nbst=0\nema=9.360288600661304\nsaved=1800000000\nclaim=0\nclaimcash=0\ndia=0\nest.0=5,2\nfila.0=15501;1" +
            "5502;15503;15504;15505;15506;15507;15508;15509;15510;15511;15512;15513;15514;15515;15516;15517;15518;15519;15520;15521;15522;15523;15524;155" +
            "25;15526;15527;15528;15529;15530;15531;15532;15533;15534\nat.0=15044:2;15045:32;15500:62\nest.1=5,2\nat.1=15042:40;15043:70\nest.2=0,0\nest." +
            "3=5,2\nfila.3=15011;15016;15015;15017;15014;15020;15019;15018;15022;15023;15021;15024;15025;15026;15027;15028;15029;15031;15030;15033;15032;" +
            "15035;15036;15034;15037;15038;15039;15040;15041\nest.4=5,2\nest.5=5,2\nest.6=5,2\nest.7=0,0\nfunc.0=1,0,0,0\nfunc.1=1,0,0,1\nfunc.2=4,0,0,3" +
            "\nfunc.3=5,1,0,2\nfunc.4=6,0,0,4\nfunc.5=7,1,0,5\nvoo.0=46,13,182,1,0\nvoo.1=35,0,0,0,0\naviao.0=50,82200,1,30,-1,0,0,1,0,0\nsrv.0=0:0,0:0,0" +
            ":0,0:0\naviao.1=100,83700,0,-1,-1,0,0,1,0,0\nsrv.1=0:0,0:0,0:0,0:0\ntrip.0=0,1,3872,0\ntrip.1=0,1,5400,1\ntrip.2=0,3,1499,-1\ntrip.3=1,1,384" +
            "2,0\ntrip.4=1,1,5400,1\ntrip.5=1,3,1499,-1\npatio=3\npatioatraso=7\naero=1,3,-1\nrotas=127\nprog=6,6\nvr.0=6,46,0,0,0,0,1800,0\nvr.1=6,46,0," +
            "0,0,0,3300,0\nav.0=6,15\nav.1=6,15\nstt.0=1,31,5.666665818542242\nstt.1=0,-1,27.900001425296068\nstt.3=1,30,8.099999587982893\nreparo=0\ninc" +
            "w=14\ncamp.0=6,10,0,0,3,0,1,0,1,3,0\ncamp.1=1,0,1,40,40,3,0,1,0,2,0\ncamp.2=0,0,0,0,0,0,0,0,0,0,0\nmiss=1,0\nmd.0=14,3,0,0\nmd.1=12,30,0,1\n" +
            "md.2=6,300,0,1\nms.0=4,30,1,0\nms.1=2,10,0,0\nms.2=3,20,0,0\nms.3=1,0,5,0\nms.4=0,996,0,0\ncq=34848,0,0,30,30,25,4,22,90,0,209,236,25,0,2,0," +
            "0,0,0,0,5\ngems=0\npasse=0,0,325,3\n";

        /// <summary>A cadeia 9 -> 10 -> 11: o relatorio da frente A comeca no dia de agora e o registro comeca vazio; o resto do jogo fica como estava.</summary>
        [Test]
        public void MigracaoDoSchema9_PelaCadeia_RegistroVazio()
        {
            const long U = 1800000000;
            Sim m = Sim.Load(V09, null, 7);
            Assert.IsFalse(m.SaveRefused, "schema 9 migra, nao e' recusado");
            Assert.AreEqual((5137L, 83700L, 996L), (m.Cash, m.Steps, m.TotalEmbarcados));
            Assert.IsEmpty(m.Ocorrencias);
            string texto = m.Save("0.9.0", U);
            StringAssert.Contains("\nschema=" + Sim.Schema + "\n", texto);
            StringAssert.DoesNotContain("\nhist=", texto);
            Assert.AreEqual(texto, Sim.Load(texto, null, 7).Save("0.9.0", U), "ida e volta estavel depois de migrar");

            var b = new Bot(BotProfile.Bom);
            for (int i = 0; i < 1800 * Sim.Hz; i++) b.Step(m);
            Assert.IsNotEmpty(m.Ocorrencias, "anota dali em diante");
        }

        /// <summary>O save guarda estes enums pelo indice: so crescem no fim.</summary>
        [Test]
        public void EnumsDoHistorico_SoCrescemNoFim()
        {
            CollectionAssert.AreEqual(new[] { "Incidente", "Clima", "FalhaAviao", "VooPerdido", "VooAtrasado", "FaltaCaixa" }, Enum.GetNames(typeof(TipoOcorrencia)).Take(6));
            CollectionAssert.AreEqual(new[] { "Nenhum", "Fila", "SemEquipe", "Equipamento", "Clima", "Pista", "Patio", "Tripulacao", "Manutencao", "Pane", "Hangar", "Pouso", "Caixa" },
                Enum.GetNames(typeof(Motivo)).Take(13));
            CollectionAssert.AreEqual(new[] { "Segurar", "Adiantar", "Atrasar", "Reparar", "Priorizar", "Reforcar", "Hangar" }, Enum.GetNames(typeof(Opcao)).Take(7));
            CollectionAssert.AreEqual(new[] { "Nenhuma", "Pane", "Rejeicao", "Retorno" }, Enum.GetNames(typeof(TipoFalha)).Take(4));
        }
    }
}
