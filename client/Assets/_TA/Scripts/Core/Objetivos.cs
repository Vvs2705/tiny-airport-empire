namespace TA.Core
{
    /// <summary>O que um objetivo mede (CONTRATO_FASE_C5 §2). O indice nao vai no save. Nenhuma meta olha anuncio ou compra digital.</summary>
    public enum Meta { Embarcados, Equipe, Movidos, Velocidade, Capacidade, Historias, Atendidos, Climas, Incidentes, Cheios, Voos, Seguidos, Pontuais, Checagens, Hangar, Marco, Chegou, Melhorias, Passageiros, Rotas, Campanha, Missoes }

    /// <summary>
    /// Uma linha da campanha (docs/CAMPANHA.md), das missoes ou das conquistas (docs/MISSOES.md). A posicao na tabela vai no save: linha nova so
    /// no fim. Id e' estavel; Nome e' de trabalho. Arg = estacao, rota, aeroporto ou TipoIncidente, conforme a meta (-1 = qualquer um). Recompensa = Cash fixo.
    /// So nas missoes: Marco = o marco do terminal em que a linha entra no sorteio e E = a segunda parte da semanal (a mesma classe, sem Recompensa).
    /// </summary>
    public sealed class Objetivo
    {
        public string Id = "", Nome = "";
        public Meta Meta;
        public int Arg = -1, Alvo = 1, Recompensa, Marco;
        public Objetivo E;
    }

    public static partial class Tabelas
    {
        /// <summary>As 3 cadeias, na ordem de Tuning.Aeroportos: a mesma tabela para o jogo e para o bot. docs/CAMPANHA.md e' esta tabela.</summary>
        public static readonly Objetivo[][] Objetivos =
        {
            new[]
            {
                new Objetivo { Id = "vv_embarque", Nome = "Primeiro embarque", Meta = Meta.Embarcados, Alvo = 10, Recompensa = 20 },
                new Objetivo { Id = "vv_ana", Nome = "Alguém no balcão", Meta = Meta.Equipe, Alvo = 1, Recompensa = 30 },
                new Objetivo { Id = "vv_equipe", Nome = "Turno completo", Meta = Meta.Equipe, Alvo = 3, Recompensa = 100 },
                new Objetivo { Id = "vv_mover", Nome = "Onde a fila aperta", Meta = Meta.Movidos, Alvo = 3, Recompensa = 60 },
                new Objetivo { Id = "vv_balcao", Nome = "Balcão mais rápido", Meta = Meta.Velocidade, Arg = (int)Estacao.CheckIn, Alvo = 2, Recompensa = 80 },
                new Objetivo { Id = "vv_rosa", Nome = "Dona Rosa", Meta = Meta.Historias, Alvo = 1, Recompensa = 100 },
                new Objetivo { Id = "vv_loja", Nome = "Loja aberta", Meta = Meta.Atendidos, Arg = (int)Estacao.Loja, Alvo = 15, Recompensa = 150 },
                new Objetivo { Id = "vv_ceu", Nome = "Céu fechado", Meta = Meta.Climas, Alvo = 1, Recompensa = 150 },
                new Objetivo { Id = "vv_curitiba", Nome = "Rumo a Curitiba", Meta = Meta.Cheios, Arg = 2, Alvo = 3, Recompensa = 250 },
                new Objetivo { Id = "vv_terminal", Nome = "Terminal novo", Meta = Meta.Marco, Alvo = 2, Recompensa = 400 },
            },
            new[]
            {
                new Objetivo { Id = "ca_chegada", Nome = "Chegada à praia", Meta = Meta.Chegou, Arg = 1, Recompensa = 300 },
                new Objetivo { Id = "ca_salvador", Nome = "Turista em cima da hora", Meta = Meta.Cheios, Arg = 4, Alvo = 3, Recompensa = 300 },
                new Objetivo { Id = "ca_comida", Nome = "Fome de praia", Meta = Meta.Atendidos, Arg = (int)Estacao.Alimentacao, Alvo = 40, Recompensa = 400 },
                new Objetivo { Id = "ca_bagagem", Nome = "Mala e prancha", Meta = Meta.Atendidos, Arg = (int)Estacao.Bagagem, Alvo = 40, Recompensa = 400 },
                new Objetivo { Id = "ca_checagem", Nome = "Maresia", Meta = Meta.Checagens, Alvo = 3, Recompensa = 500 },
                new Objetivo { Id = "ca_incidente", Nome = "Imprevisto", Meta = Meta.Incidentes, Alvo = 1, Recompensa = 500 },
                new Objetivo { Id = "ca_vento", Nome = "Rajada", Meta = Meta.Climas, Alvo = 1, Recompensa = 600 },
                new Objetivo { Id = "ca_hangar", Nome = "Revisão geral", Meta = Meta.Hangar, Alvo = 1, Recompensa = 600 },
                new Objetivo { Id = "ca_fortaleza", Nome = "Fortaleza lotada", Meta = Meta.Cheios, Arg = 6, Alvo = 3, Recompensa = 800 },
                new Objetivo { Id = "ca_terminal", Nome = "Terminal internacional", Meta = Meta.Marco, Alvo = 3, Recompensa = 1200 },
            },
            new[]
            {
                new Objetivo { Id = "ag_chegada", Nome = "Bem-vindo ao hub", Meta = Meta.Chegou, Arg = 2, Recompensa = 800 },
                new Objetivo { Id = "ag_guarulhos", Nome = "Conexão em Guarulhos", Meta = Meta.Cheios, Arg = 8, Alvo = 3, Recompensa = 800 },
                new Objetivo { Id = "ag_conexao", Nome = "Grupo atrasado", Meta = Meta.Incidentes, Alvo = 2, Recompensa = 800 },
                new Objetivo { Id = "ag_pontual", Nome = "Relógio suíço", Meta = Meta.Pontuais, Alvo = 10, Recompensa = 800 },
                new Objetivo { Id = "ag_neve", Nome = "Neve no pátio", Meta = Meta.Climas, Alvo = 1, Recompensa = 1000 },
                new Objetivo { Id = "ag_manaus", Nome = "Rumo a Manaus", Meta = Meta.Voos, Arg = 10, Alvo = 3, Recompensa = 1200 },
                new Objetivo { Id = "ag_portao", Nome = "Portão duplo", Meta = Meta.Capacidade, Arg = (int)Estacao.Portao, Alvo = 2, Recompensa = 1000 },
                new Objetivo { Id = "ag_lisboa", Nome = "Travessia", Meta = Meta.Voos, Arg = 11, Alvo = 3, Recompensa = 1500 },
                new Objetivo { Id = "ag_sequencia", Nome = "Dia perfeito", Meta = Meta.Seguidos, Alvo = 8, Recompensa = 2000 },
                new Objetivo { Id = "ag_hub", Nome = "Hub internacional", Meta = Meta.Embarcados, Alvo = 2000, Recompensa = 3000 },
            },
        };
    }
}
