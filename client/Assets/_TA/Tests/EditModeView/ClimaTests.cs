using System;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>Clima no cenario (Mundo.Clima): nada aparece antes do aviso, todo clima tem como aparecer e cada incidente cai na sala certa.</summary>
    public class ClimaTests
    {
        [Test]
        public void Ceu_FechaNoAviso_EFicaForteQuandoComeca()
        {
            var i = new Incidente { Tipo = TipoIncidente.Temporal, Aviso = 100, Inicio = 200, Fim = 300 };
            Assert.AreEqual(0f, Mundo.Forca(null, 150));
            Assert.AreEqual(Mundo.ForcaAviso, Mundo.Forca(i, 150));
            Assert.AreEqual(1f, Mundo.Forca(i, 200));
        }

        [TestCase(TipoIncidente.Falha)]
        [TestCase(TipoIncidente.Pico)]
        [TestCase(TipoIncidente.Conexao)]
        public void Incidente_DeSala_NaoMudaOCeu(TipoIncidente t)
        {
            Assert.AreEqual(0f, Mundo.Forca(new Incidente { Tipo = t, Inicio = 0 }, 10));
        }

        [Test]
        public void TodoClimaDaTabela_TemComoAparecer()
        {
            foreach (Clima c in Tabelas.Climas) Assert.IsNotNull(Mundo.TempoDe(c.Tipo), c.Tipo.ToString());
        }

        [TestCase(TipoIncidente.FrenteFria, Estacao.CheckIn, (int)Estacao.Portao)]
        [TestCase(TipoIncidente.Temporal, Estacao.CheckIn, (int)Estacao.Portao)]
        [TestCase(TipoIncidente.Vento, Estacao.CheckIn, (int)Estacao.Portao)]
        [TestCase(TipoIncidente.Pico, Estacao.Portao, (int)Estacao.CheckIn)]
        [TestCase(TipoIncidente.Conexao, Estacao.CheckIn, (int)Estacao.Seguranca)]
        [TestCase(TipoIncidente.Falha, Estacao.Seguranca, (int)Estacao.Seguranca)]
        [TestCase(TipoIncidente.Falha, Estacao.Portao, (int)Estacao.Portao)]
        [TestCase(TipoIncidente.Neblina, Estacao.CheckIn, -1)]
        [TestCase(TipoIncidente.Neve, Estacao.CheckIn, -1)]
        public void Incidente_CaiNaSalaOndeAcontece(TipoIncidente t, Estacao e, int sala)
        {
            Assert.AreEqual(sala, Mundo.SalaDo(new Incidente { Tipo = t, Estacao = e }));
        }

        [Test]
        public void TodoTipoDeIncidente_TemTesteDeSala()
        {
            Assert.AreEqual(8, Enum.GetValues(typeof(TipoIncidente)).Length, "tipo novo: decida a sala em Mundo.SalaDo e acrescente o caso aqui");
        }
    }
}
