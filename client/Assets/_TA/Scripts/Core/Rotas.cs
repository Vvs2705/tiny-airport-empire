namespace TA.Core
{
    /// <summary>Arquetipos do GDD §5.7. Indice nao vai no save (a linha da tabela vai).</summary>
    public enum TipoRota { Curta, Turistica, Conexao, Longa }

    /// <summary>Os 4 incidentes do GDD §5.8. Indice vai no save.</summary>
    public enum TipoIncidente { FrenteFria, Falha, Pico, Conexao }

    /// <summary>
    /// Uma linha da tabela de rotas (CONTRATO_FASE_C3 §3). O indice em Tuning.Rotas vai no save: so cresce no fim. Id e' estavel; Nome e' de
    /// trabalho (D-13 troca so ele). Fatores em 1 = a rota da v0.1.
    /// </summary>
    public sealed class Rota
    {
        public static readonly Rota Neutra = new Rota();

        public string Id = "", Nome = "";
        public TipoRota Tipo;
        /// <summary>Indice em Tuning.Aeroportos, marco do terminal que libera a compra e preco fixo (0 = abre ao chegar no aeroporto).</summary>
        public int Aeroporto, Marco, Custo;
        /// <summary>Distancia: s alem de Tuning.Trecho. Frequencia: voa a cada Cada ondas (com Cada = 2 o aviao fica fora a onda seguinte).</summary>
        public float Extra; public int Cada = 1;
        /// <summary>Fatores de passageiros, tarifa e venda em loja e comida.</summary>
        public float Pax = 1f, Tarifa = 1f, Gasto = 1f;
        /// <summary>Parte do inicio da janela de chegada sem ninguem (0,5 = todos na 2a metade) e o que soma no bonus de pontualidade.</summary>
        public float Pico, Bonus;
        /// <summary>0 a 2: soma na chance de incidente da onda (Tuning.RiscoChance). Aeronave: silhueta (0 regional, 1 narrow, 2 wide), so visual.</summary>
        public int Risco, Aeronave;
        /// <summary>Receita por onda em relacao a rota da v0.1 (o bot ordena por ela).</summary>
        public float Receita => Pax * Tarifa / Cada;
        public Rota Copia() => (Rota)MemberwiseClone();
    }

    /// <summary>Uma linha da tabela de aeroportos (CONTRATO_FASE_C3 §2). Indice vai no save: so cresce no fim. Nome de trabalho (D-13).</summary>
    public sealed class Aeroporto
    {
        public static readonly Aeroporto Neutro = new Aeroporto { Pesos = new int[4] };

        public string Id = "", Nome = "";
        /// <summary>Marco do terminal que libera a viagem e o preco da primeira ida (voltar e' gratis).</summary>
        public int Marco, Custo;
        /// <summary>Chance de incidente por onda, fator de desgaste por trecho e peso de cada TipoIncidente.</summary>
        public float Incidentes, Desgaste = 1f; public int[] Pesos;
        public Aeroporto Copia() => (Aeroporto)MemberwiseClone();
    }

    /// <summary>Incidente em andamento (no maximo 1). Passos absolutos: Aviso &lt;= agora &lt; Fim. A semente decide; nada pago mexe nele.</summary>
    public sealed class Incidente
    {
        public TipoIncidente Tipo;
        /// <summary>Vaga atingida (0 ou 1) e, na falha, a estacao.</summary>
        public int Vaga; public Estacao Estacao;
        public long Aviso, Inicio, Fim;
        /// <summary>A saida foi usada: adiantou, reparou, priorizou; no pico, ninguem perdeu o voo.</summary>
        public bool Resolvido;
    }

    /// <summary>As tabelas do contrato. Numeros sao hipotese: o bot mede e o docs/BALANCE.md §7 registra.</summary>
    public static class Tabelas
    {
        public static readonly Aeroporto[] Aeroportos =
        {
            new Aeroporto { Id = "vv", Nome = "Vale Verde", Pesos = new[] { 1, 1, 1, 0 } },
            new Aeroporto { Id = "ca", Nome = "Costa Azul", Marco = 2, Custo = 1200, Incidentes = 0.4f, Desgaste = 1.5f, Pesos = new[] { 4, 3, 2, 1 } },
            new Aeroporto { Id = "ag", Nome = "Aurora Global", Marco = 3, Custo = 3000, Incidentes = 0.5f, Pesos = new[] { 4, 3, 1, 2 } },
        };

        public static readonly Rota[] Rotas =
        {
            new Rota { Id = "vv_campinas", Nome = "Campinas" },
            new Rota { Id = "vv_ribeirao", Nome = "Ribeirão Preto" },
            new Rota { Id = "vv_curitiba", Nome = "Curitiba", Marco = 1, Custo = 250, Extra = 10f, Pax = 1.3f },
            new Rota { Id = "vv_bh", Nome = "Belo Horizonte", Marco = 2, Custo = 500, Extra = 20f, Pax = 0.9f, Tarifa = 1.5f, Risco = 1 },
            new Rota { Id = "ca_salvador", Nome = "Salvador", Tipo = TipoRota.Turistica, Aeroporto = 1, Marco = 2, Extra = 10f, Pax = 1.3f, Tarifa = 1.1f, Gasto = 1.5f, Pico = 0.5f, Risco = 1, Aeronave = 1 },
            new Rota { Id = "ca_recife", Nome = "Recife", Tipo = TipoRota.Turistica, Aeroporto = 1, Marco = 2, Extra = 20f, Pax = 1.2f, Tarifa = 1.2f, Gasto = 1.5f, Pico = 0.5f, Risco = 1, Aeronave = 1 },
            new Rota { Id = "ca_fortaleza", Nome = "Fortaleza", Tipo = TipoRota.Turistica, Aeroporto = 1, Marco = 2, Custo = 800, Extra = 20f, Pax = 1.5f, Tarifa = 1.2f, Gasto = 2f, Pico = 0.6f, Risco = 1, Aeronave = 1 },
            new Rota { Id = "ca_brasilia", Nome = "Brasília", Tipo = TipoRota.Conexao, Aeroporto = 1, Marco = 3, Custo = 1000, Pax = 1.35f, Tarifa = 1f, Pico = 0.7f, Bonus = 0.25f, Risco = 1, Aeronave = 1 },
            new Rota { Id = "ag_guarulhos", Nome = "Guarulhos", Tipo = TipoRota.Conexao, Aeroporto = 2, Marco = 3, Pax = 1.25f, Tarifa = 1.1f, Pico = 0.7f, Bonus = 0.25f, Risco = 1, Aeronave = 1 },
            new Rota { Id = "ag_galeao", Nome = "Galeão", Tipo = TipoRota.Conexao, Aeroporto = 2, Marco = 3, Extra = 10f, Pax = 1.3f, Tarifa = 1.15f, Pico = 0.7f, Bonus = 0.25f, Risco = 1, Aeronave = 1 },
            new Rota { Id = "ag_manaus", Nome = "Manaus", Tipo = TipoRota.Longa, Aeroporto = 2, Marco = 3, Custo = 1500, Extra = 180f, Cada = 2, Pax = 1.7f, Tarifa = 3f, Gasto = 1.2f, Risco = 2, Aeronave = 2 },
            new Rota { Id = "ag_lisboa", Nome = "Lisboa", Tipo = TipoRota.Longa, Aeroporto = 2, Marco = 3, Custo = 2500, Extra = 180f, Cada = 2, Pax = 2f, Tarifa = 3f, Gasto = 1.2f, Risco = 2, Aeronave = 2 },
        };
    }
}
