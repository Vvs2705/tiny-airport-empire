using System;
using System.Linq;
using NUnit.Framework;
using TA.Core;

namespace TA.Tests
{
    /// <summary>
    /// O save e o diario guardam estes enums pelo indice: so crescem no FIM (CONTRIBUTING, Saves). Item novo no fim passa aqui sem mexer no
    /// teste; trocar a ordem, renomear ou tirar um item quebra o save de quem ja joga, e cai aqui.
    /// </summary>
    public class EnumsTests
    {
        static void SoCresceNoFim<T>(params string[] salvos) where T : Enum =>
            CollectionAssert.AreEqual(salvos, Enum.GetNames(typeof(T)).Take(salvos.Length).ToArray(), typeof(T).Name + " mudou no meio");

        [Test]
        public void EnumsSalvos_SoCrescemNoFim()
        {
            SoCresceNoFim<Estacao>("CheckIn", "Seguranca", "Loja", "Portao");
            SoCresceNoFim<Melhoria>("Velocidade", "Capacidade");
            SoCresceNoFim<Estado>("Normal", "Atencao", "Critico");
            SoCresceNoFim<Funcao>("Piloto", "Comissario");
            SoCresceNoFim<Servico>("Desembarque", "Limpeza", "Combustivel", "Bagagem");
            SoCresceNoFim<Ev>("VooCriado", "EmbarqueAberto", "VooPartiu", "VooPerdido", "Embarcou", "Venda", "Toque", "Movido", "Chegou", "Contratado", "Melhorou", "Marco",
                "Historia", "Offline", "Recompensa", "Fila", "Bonus", "TripulanteContratado", "Escalado", "SemTripulacao", "Descanso", "Pousou", "ServicoFeito",
                "TurnaroundFeito", "Decolou", "PatioContratado", "PatioMelhorou", "ManutencaoAviso", "ManutencaoPendente", "ChecagemIniciada", "ChecagemFeita", "HangarIniciado",
                "HangarFeito");
        }
    }
}
