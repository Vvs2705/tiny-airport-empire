namespace TA.Core
{
    public static partial class Tabelas
    {
        const int Loja = (int)Estacao.Loja, Mala = (int)Estacao.Bagagem, Comida = (int)Estacao.Alimentacao;

        static Objetivo E(Meta m, int alvo, int arg = -1) => new Objetivo { Meta = m, Alvo = alvo, Arg = arg };

        /// <summary>As diarias (docs/MISSOES.md e' esta tabela). Marco = entra no sorteio naquele marco. A posicao vai no save: linha nova so no fim.</summary>
        public static readonly Objetivo[] Diarias =
        {
            new Objetivo { Id = "d0_embarque", Nome = "Portas abertas", Meta = Meta.Embarcados, Alvo = 100, Recompensa = 60 },
            new Objetivo { Id = "d0_voos", Nome = "Quadro em dia", Meta = Meta.Voos, Alvo = 6, Recompensa = 50 },
            new Objetivo { Id = "d0_pontual", Nome = "Na hora certa", Meta = Meta.Pontuais, Alvo = 2, Recompensa = 80 },
            new Objetivo { Id = "d0_melhora", Nome = "Terminal em obras", Meta = Meta.Melhorias, Alvo = 2, Recompensa = 60 },
            new Objetivo { Id = "d0_loja", Nome = "Vitrine acesa", Meta = Meta.Atendidos, Arg = Loja, Alvo = 15, Recompensa = 150 },
            new Objetivo { Id = "d0_historia", Nome = "Boas histórias", Meta = Meta.Historias, Alvo = 2, Recompensa = 60 },

            new Objetivo { Id = "d1_embarque", Nome = "Terminal movimentado", Marco = 1, Meta = Meta.Embarcados, Alvo = 300, Recompensa = 150 },
            new Objetivo { Id = "d1_cheios", Nome = "Casa cheia", Marco = 1, Meta = Meta.Cheios, Alvo = 6, Recompensa = 180 },
            new Objetivo { Id = "d1_pontual", Nome = "Relógio afinado", Marco = 1, Meta = Meta.Pontuais, Alvo = 4, Recompensa = 200 },
            new Objetivo { Id = "d1_melhora", Nome = "Reforma do dia", Marco = 1, Meta = Meta.Melhorias, Alvo = 3, Recompensa = 150 },
            new Objetivo { Id = "d1_mover", Nome = "Olho na fila", Marco = 1, Meta = Meta.Movidos, Alvo = 3, Recompensa = 200 },
            new Objetivo { Id = "d1_loja", Nome = "Dia de vendas", Marco = 1, Meta = Meta.Atendidos, Arg = Loja, Alvo = 30, Recompensa = 300 },
            new Objetivo { Id = "d1_comida", Nome = "Hora do almoço", Marco = 1, Meta = Meta.Atendidos, Arg = Comida, Alvo = 30, Recompensa = 200 },
            new Objetivo { Id = "d1_mala", Nome = "Esteira rodando", Marco = 1, Meta = Meta.Atendidos, Arg = Mala, Alvo = 30, Recompensa = 200 },
            new Objetivo { Id = "d1_curitiba", Nome = "Ponte aérea", Marco = 1, Meta = Meta.Cheios, Arg = 2, Alvo = 4, Recompensa = 250 },

            new Objetivo { Id = "d2_embarque", Nome = "Fluxo constante", Marco = 2, Meta = Meta.Embarcados, Alvo = 400, Recompensa = 300 },
            new Objetivo { Id = "d2_cheios", Nome = "Sem poltrona vazia", Marco = 2, Meta = Meta.Cheios, Alvo = 8, Recompensa = 350 },
            new Objetivo { Id = "d2_pontual", Nome = "Pontual o dia todo", Marco = 2, Meta = Meta.Pontuais, Alvo = 6, Recompensa = 400 },
            new Objetivo { Id = "d2_melhora", Nome = "Obra no terminal", Marco = 2, Meta = Meta.Melhorias, Alvo = 2, Recompensa = 300 },
            new Objetivo { Id = "d2_mover", Nome = "Gerente de piso", Marco = 2, Meta = Meta.Movidos, Alvo = 4, Recompensa = 350 },
            new Objetivo { Id = "d2_checagem", Nome = "Avião em dia", Marco = 2, Meta = Meta.Checagens, Alvo = 3, Recompensa = 350 },
            new Objetivo { Id = "d2_comida", Nome = "Praça cheia", Marco = 2, Meta = Meta.Atendidos, Arg = Comida, Alvo = 50, Recompensa = 400 },
            new Objetivo { Id = "d2_loja", Nome = "Corredor de compras", Marco = 2, Meta = Meta.Atendidos, Arg = Loja, Alvo = 40, Recompensa = 500 },
            new Objetivo { Id = "d2_salvador", Nome = "Rota do sol", Marco = 2, Meta = Meta.Voos, Arg = 4, Alvo = 5, Recompensa = 400 },
            new Objetivo { Id = "d2_bh", Nome = "Rota das montanhas", Marco = 2, Meta = Meta.Voos, Arg = 3, Alvo = 5, Recompensa = 400 },

            new Objetivo { Id = "d3_embarque", Nome = "Hub lotado", Marco = 3, Meta = Meta.Embarcados, Alvo = 500, Recompensa = 600 },
            new Objetivo { Id = "d3_cheios", Nome = "Lotação esgotada", Marco = 3, Meta = Meta.Cheios, Alvo = 8, Recompensa = 700 },
            new Objetivo { Id = "d3_pontual", Nome = "Painel verde", Marco = 3, Meta = Meta.Pontuais, Alvo = 6, Recompensa = 800 },
            new Objetivo { Id = "d3_mover", Nome = "Chefe de turno", Marco = 3, Meta = Meta.Movidos, Alvo = 5, Recompensa = 600 },
            new Objetivo { Id = "d3_checagem", Nome = "Revisão em dia", Marco = 3, Meta = Meta.Checagens, Alvo = 4, Recompensa = 600 },
            new Objetivo { Id = "d3_comida", Nome = "Banquete", Marco = 3, Meta = Meta.Atendidos, Arg = Comida, Alvo = 60, Recompensa = 700 },
            new Objetivo { Id = "d3_mala", Nome = "Porão cheio", Marco = 3, Meta = Meta.Atendidos, Arg = Mala, Alvo = 60, Recompensa = 700 },
            new Objetivo { Id = "d3_loja", Nome = "Duty free", Marco = 3, Meta = Meta.Atendidos, Arg = Loja, Alvo = 50, Recompensa = 900 },
            new Objetivo { Id = "d3_historias", Nome = "Álbum de viagem", Marco = 3, Meta = Meta.Historias, Alvo = 5, Recompensa = 500 },
            new Objetivo { Id = "d3_guarulhos", Nome = "Conexão perfeita", Marco = 3, Meta = Meta.Cheios, Arg = 8, Alvo = 5, Recompensa = 800 },
            new Objetivo { Id = "d3_manaus", Nome = "Rumo ao norte", Marco = 3, Meta = Meta.Voos, Arg = 10, Alvo = 3, Recompensa = 1000 },
            new Objetivo { Id = "d3_lisboa", Nome = "Outro lado do mar", Marco = 3, Meta = Meta.Voos, Arg = 11, Alvo = 3, Recompensa = 1200 },
        };

        /// <summary>As semanais: duas partes de sistemas diferentes (GDD §6.5). Mesmas regras das diarias.</summary>
        public static readonly Objetivo[] Semanais =
        {
            new Objetivo { Id = "s0_fluxo", Nome = "Semana de estreia", Meta = Meta.Embarcados, Alvo = 1000, E = E(Meta.Atendidos, 30, Loja), Recompensa = 500 },
            new Objetivo { Id = "s0_loja", Nome = "Vitrine e obra", Meta = Meta.Atendidos, Arg = Loja, Alvo = 60, E = E(Meta.Melhorias, 5), Recompensa = 600 },
            new Objetivo { Id = "s0_pontual", Nome = "Boa impressão", Meta = Meta.Pontuais, Alvo = 10, E = E(Meta.Atendidos, 40, Loja), Recompensa = 700 },
            new Objetivo { Id = "s0_cheios", Nome = "Fama de lotado", Meta = Meta.Cheios, Alvo = 20, E = E(Meta.Atendidos, 50, Loja), Recompensa = 700 },
            new Objetivo { Id = "s0_voos", Nome = "Céu aberto", Meta = Meta.Voos, Alvo = 40, E = E(Meta.Climas, 1), Recompensa = 500 },
            new Objetivo { Id = "s0_mover", Nome = "Equipe em forma", Meta = Meta.Movidos, Alvo = 30, E = E(Meta.Historias, 6), Recompensa = 500 },

            new Objetivo { Id = "s1_fluxo", Nome = "Semana cheia", Marco = 1, Meta = Meta.Embarcados, Alvo = 1500, E = E(Meta.Movidos, 8), Recompensa = 800 },
            new Objetivo { Id = "s1_loja", Nome = "Compras e malas", Marco = 1, Meta = Meta.Atendidos, Arg = Loja, Alvo = 80, E = E(Meta.Atendidos, 60, Mala), Recompensa = 800 },
            new Objetivo { Id = "s1_pontual", Nome = "Fama de pontual", Marco = 1, Meta = Meta.Pontuais, Alvo = 15, E = E(Meta.Climas, 1), Recompensa = 1000 },
            new Objetivo { Id = "s1_cheios", Nome = "Sempre lotado", Marco = 1, Meta = Meta.Cheios, Alvo = 25, E = E(Meta.Historias, 5), Recompensa = 1000 },
            new Objetivo { Id = "s1_voos", Nome = "Malha aérea", Marco = 1, Meta = Meta.Voos, Alvo = 50, E = E(Meta.Melhorias, 4), Recompensa = 1000 },
            new Objetivo { Id = "s1_mover", Nome = "Time afiado", Marco = 1, Meta = Meta.Movidos, Alvo = 12, E = E(Meta.Atendidos, 60, Comida), Recompensa = 1000 },

            new Objetivo { Id = "s2_fluxo", Nome = "Grande semana", Marco = 2, Meta = Meta.Embarcados, Alvo = 2000, E = E(Meta.Checagens, 6), Recompensa = 1500 },
            new Objetivo { Id = "s2_comida", Nome = "Mesa e esteira", Marco = 2, Meta = Meta.Atendidos, Arg = Comida, Alvo = 120, E = E(Meta.Atendidos, 120, Mala), Recompensa = 1500 },
            new Objetivo { Id = "s2_pontual", Nome = "Bom tempo, boa hora", Marco = 2, Meta = Meta.Pontuais, Alvo = 20, E = E(Meta.Climas, 2), Recompensa = 2000 },
            new Objetivo { Id = "s2_cheios", Nome = "Frota em dia", Marco = 2, Meta = Meta.Cheios, Alvo = 30, E = E(Meta.Hangar, 1), Recompensa = 2000 },
            new Objetivo { Id = "s2_voos", Nome = "Imprevisto resolvido", Marco = 2, Meta = Meta.Voos, Alvo = 60, E = E(Meta.Incidentes, 2), Recompensa = 2000 },
            new Objetivo { Id = "s2_mover", Nome = "Terminal na mão", Marco = 2, Meta = Meta.Movidos, Alvo = 15, E = E(Meta.Historias, 8), Recompensa = 2000 },

            new Objetivo { Id = "s3_fluxo", Nome = "Semana do hub", Marco = 3, Meta = Meta.Embarcados, Alvo = 3000, E = E(Meta.Checagens, 8), Recompensa = 3000 },
            new Objetivo { Id = "s3_comida", Nome = "Serviço de bordo", Marco = 3, Meta = Meta.Atendidos, Arg = Comida, Alvo = 160, E = E(Meta.Atendidos, 160, Mala), Recompensa = 3000 },
            new Objetivo { Id = "s3_pontual", Nome = "Hub pontual", Marco = 3, Meta = Meta.Pontuais, Alvo = 25, E = E(Meta.Climas, 3), Recompensa = 4000 },
            new Objetivo { Id = "s3_cheios", Nome = "Frota impecável", Marco = 3, Meta = Meta.Cheios, Alvo = 40, E = E(Meta.Hangar, 2), Recompensa = 4000 },
            new Objetivo { Id = "s3_voos", Nome = "Sala de crise", Marco = 3, Meta = Meta.Voos, Alvo = 70, E = E(Meta.Incidentes, 3), Recompensa = 4000 },
            new Objetivo { Id = "s3_mover", Nome = "Maestro do terminal", Marco = 3, Meta = Meta.Movidos, Alvo = 20, E = E(Meta.Historias, 10), Recompensa = 4000 },
            new Objetivo { Id = "s3_manaus", Nome = "Longo curso", Marco = 3, Meta = Meta.Voos, Arg = 10, Alvo = 10, E = E(Meta.Atendidos, 120, Comida), Recompensa = 4000 },
            new Objetivo { Id = "s3_lisboa", Nome = "Ponte transatlântica", Marco = 3, Meta = Meta.Voos, Arg = 11, Alvo = 10, E = E(Meta.Atendidos, 120, Mala), Recompensa = 4000 },
        };

        /// <summary>As 20 conquistas, permanentes: valem em qualquer aeroporto e nao pagam Cash. A posicao vai no save (bit e contador): linha nova so no fim.</summary>
        public static readonly Objetivo[] Conquistas =
        {
            new Objetivo { Id = "c_passageiros_1", Nome = "Mil a bordo", Meta = Meta.Passageiros, Alvo = 1000 },
            new Objetivo { Id = "c_passageiros_2", Nome = "Dez mil a bordo", Meta = Meta.Passageiros, Alvo = 10000 },
            new Objetivo { Id = "c_voos_1", Nome = "Cem decolagens", Meta = Meta.Voos, Alvo = 100 },
            new Objetivo { Id = "c_voos_2", Nome = "Duzentas decolagens", Meta = Meta.Voos, Alvo = 200 },
            new Objetivo { Id = "c_cheios", Nome = "Cinquenta lotados", Meta = Meta.Cheios, Alvo = 50 },
            new Objetivo { Id = "c_seguidos", Nome = "Oito seguidos", Meta = Meta.Seguidos, Alvo = 8 },
            new Objetivo { Id = "c_pontuais", Nome = "Cinquenta na hora", Meta = Meta.Pontuais, Alvo = 50 },
            new Objetivo { Id = "c_movidos", Nome = "Cem reforços", Meta = Meta.Movidos, Alvo = 100 },
            new Objetivo { Id = "c_loja", Nome = "Freguês da loja", Meta = Meta.Atendidos, Arg = Loja, Alvo = 80 },
            new Objetivo { Id = "c_comida", Nome = "Cozinha famosa", Meta = Meta.Atendidos, Arg = Comida, Alvo = 500 },
            new Objetivo { Id = "c_mala", Nome = "Nenhuma mala fora", Meta = Meta.Atendidos, Arg = Mala, Alvo = 500 },
            new Objetivo { Id = "c_checagens", Nome = "Olho na frota", Meta = Meta.Checagens, Alvo = 25 },
            new Objetivo { Id = "c_hangar", Nome = "Hangar de confiança", Meta = Meta.Hangar, Alvo = 3 },
            new Objetivo { Id = "c_climas", Nome = "Dono do tempo", Meta = Meta.Climas, Alvo = 10 },
            new Objetivo { Id = "c_incidentes", Nome = "Sangue-frio", Meta = Meta.Incidentes, Alvo = 10 },
            new Objetivo { Id = "c_equipe", Nome = "Time completo", Meta = Meta.Equipe, Alvo = 6 },
            new Objetivo { Id = "c_aurora", Nome = "Três aeroportos", Meta = Meta.Chegou, Arg = 2 },
            new Objetivo { Id = "c_rotas", Nome = "Mapa completo", Meta = Meta.Rotas, Alvo = 12 },
            new Objetivo { Id = "c_campanha", Nome = "Hub internacional", Meta = Meta.Campanha, Alvo = 30 },
            new Objetivo { Id = "c_missoes", Nome = "Cinquenta missões", Meta = Meta.Missoes, Alvo = 50 },
        };
    }
}
