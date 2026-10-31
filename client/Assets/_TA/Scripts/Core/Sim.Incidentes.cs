using System;

namespace TA.Core
{
    /// <summary>
    /// Os 4 incidentes do GDD §5.8 (CONTRATO_FASE_C3 §4): um por vez, sorteados da semente na virada da onda (U puro: nada a gravar alem do
    /// incidente em si), sempre com Tuning.AvisoSeg de aviso. Nunca tiram Cash, marco, nivel, equipe ou rota: custam no maximo a receita do
    /// voo. So andam com o relogio do quadro: com o jogo fechado nada acontece. Tuning.RegraIncidentes = false = so visual.
    /// </summary>
    public sealed partial class Sim
    {
        Incidente _inc;
        /// <summary>O incidente depois do aviso (null antes dele: nem a tela nem o bot veem o que vem).</summary>
        public Incidente Inc => _inc != null && Steps >= _inc.Aviso ? _inc : null;
        /// <summary>Passos que faltam no reparo em andamento (ocupa o mecanico da Fase C) e a ultima onda com incidente ou chegada a aeroporto.</summary>
        public int Reparo, UltimoIncidente = -99;

        /// <summary>Frente fria: ninguem pousa nem decola, e so embarca quem adiantou.</summary>
        public bool PistaFechada => T.RegraIncidentes && _inc != null && _inc.Tipo == TipoIncidente.FrenteFria && Steps >= _inc.Inicio && Steps < _inc.Fim;

        /// <summary>A estacao esta com falha de equipamento agora (atende Tuning.FalhaFator x mais devagar).</summary>
        public bool Falhou(Estacao e) => T.RegraIncidentes && _inc != null && _inc.Tipo == TipoIncidente.Falha && _inc.Estacao == e && !_inc.Resolvido && Steps >= _inc.Inicio && Steps < _inc.Fim;

        /// <summary>Onde a falha cai; quem voa conexao nao usa o check-in, que fica de fora (a primeira da lista).</summary>
        static readonly Estacao[] Falham = { Estacao.CheckIn, Estacao.Seguranca, Estacao.Portao };

        int RiscoDe(Voo v) => v.Total > 0 ? Efeito(v).Risco : 0;

        /// <summary>Virada da onda, com os 2 voos criados e antes de anuncia-los: a semente decide se ha incidente, qual e em que vaga.</summary>
        void SorteiaIncidente(int w)
        {
            if (_inc != null || w - UltimoIncidente <= T.IncidenteFolga || Voos[0].Total == 0 && Voos[1].Total == 0) return;
            double chance = Math.Min(T.IncidenteTeto, Aero.Incidentes + T.RiscoChance * (RiscoDe(Voos[0]) + RiscoDe(Voos[1])));
            if (U(w, 0, 0, 9) >= chance) return;
            UltimoIncidente = w;
            int[] pesos = T.Aeroportos[AeroportoAtual].Pesos;
            int soma = 0, tipo = 0;
            foreach (int p in pesos) soma += p;
            for (int x = (int)(U(w, 0, 0, 10) * soma); tipo < pesos.Length - 1 && x >= pesos[tipo]; tipo++) x -= pesos[tipo];
            // a vaga de maior risco com voo; no empate, a semente
            int vaga = Voos[0].Total == 0 ? 1 : Voos[1].Total == 0 ? 0 : RiscoDe(Voos[1]) != RiscoDe(Voos[0]) ? (RiscoDe(Voos[1]) > RiscoDe(Voos[0]) ? 1 : 0) : U(w, 0, 0, 11) < 0.5 ? 0 : 1;
            Voo v = Voos[vaga];
            long t0 = (long)w * OndaPassos;
            var i = new Incidente { Tipo = (TipoIncidente)tipo, Vaga = vaga, Fim = t0 + v.FechaPasso + 1 };
            switch (i.Tipo)
            {
                case TipoIncidente.FrenteFria:
                    i.Inicio = t0 + v.FechaPasso - Passos(T.FrenteAntes);
                    i.Fim = i.Inicio + Passos(T.FrenteSeg);
                    break;
                case TipoIncidente.Falha:
                    int de = Efeito(v).Tipo == TipoRota.Conexao ? 1 : 0;
                    i.Estacao = Falham[de + (int)(U(w, 0, 0, 12) * (Falham.Length - de))];
                    i.Inicio = t0 + v.FechaPasso - Passos(T.ChegadaFolga);
                    i.Fim = i.Inicio + Passos(T.FalhaSeg);
                    break;
                case TipoIncidente.Pico:
                    i.Inicio = t0 + Grupo(v, T.PicoExtra, T.ChegadaFolga, false);
                    break;
                default:
                    i.Inicio = t0 + Grupo(v, T.ConexaoExtra, T.ConexaoAntes, true);
                    break;
            }
            i.Aviso = i.Inicio - Passos(T.AvisoSeg);
            _inc = i;
        }

        /// <summary>Soma o grupo extra ao voo: `antes` s antes do portao, logo depois das chegadas normais. Devolve o passo (na onda) da chegada.</summary>
        int Grupo(Voo v, float fator, float antes, bool conexao)
        {
            int chega = v.FechaPasso - Passos(antes);
            if (!T.RegraIncidentes) return chega;
            v.Grupo = (int)Math.Round(T.Pax(v.Onda) * (double)fator);
            v.Total += v.Grupo;
            v.GrupoChega = chega; v.GrupoConexao = conexao;
            return chega;
        }

        /// <summary>Um passo do incidente, antes do patio: aviso, inicio, reparo e fim. A falha com o Edu presente na estacao repara de graca.</summary>
        void PassoIncidente()
        {
            Incidente i = _inc;
            if (i == null) return;
            if (Steps == i.Aviso) Emit(Ev.IncidenteAviso, (int)i.Tipo, i.Vaga, (int)((i.Inicio - i.Aviso) / Hz));
            if (Steps == i.Inicio) Emit(Ev.IncidenteInicio, (int)i.Tipo, i.Vaga, (int)i.Estacao);
            if (i.Tipo == TipoIncidente.Falha && Steps >= i.Aviso && !i.Resolvido)
            {
                if (T.RegraIncidentes && Reparo == 0 && MecanicoNa(i.Estacao))
                {
                    Reparo = Passos(T.ReparoSeg);
                    Emit(Ev.ReparoIniciado, (int)i.Estacao, 0);
                }
                if (Reparo > 0 && --Reparo == 0)
                {
                    i.Resolvido = true;
                    Emit(Ev.Reparado, (int)i.Estacao);
                }
            }
            if (Steps < i.Fim) return;
            if (i.Tipo == TipoIncidente.Pico) i.Resolvido = Voos[i.Vaga].Perdidos == 0;
            Emit(Ev.IncidenteFim, (int)i.Tipo, i.Resolvido ? 1 : 0);
            _inc = null;
            Reparo = 0;
        }

        bool MecanicoNa(Estacao e)
        {
            foreach (Funcionario f in Staff) if (f.Perfil == Perfil.Mecanico && f.Em == e && f.Andando == 0) return true;
            return false;
        }

        // ------------------------------------------------------------------ intencoes do jogador

        /// <summary>Frente fria, durante o aviso: o embarque do voo atingido abre agora e continua com a pista fechada (TA-4: quem adianta nao
        /// perde o voo). A decolagem espera a pista abrir. Sem adiantar, o portao para ate a pista abrir e quem ficou perde so a propria receita.</summary>
        public bool Adianta()
        {
            Incidente i = Inc;
            if (i == null || i.Tipo != TipoIncidente.FrenteFria || i.Resolvido || Steps >= i.Inicio || !T.RegraIncidentes) return false;
            Voo v = Voos[i.Vaga];
            if (v.Partiu) return false;
            i.Resolvido = v.Adiantado = true;
            v.AbrePasso = Math.Min(v.AbrePasso, PassoNaOnda);
            Emit(Ev.Adiantado, v.Id);
            return true;
        }

        /// <summary>Falha de equipamento, a partir do aviso: Tuning.ReparoCusto Cash e o mecanico da Fase C por Tuning.ReparoSeg s. So Cash e
        /// tempo: nunca anuncio. Reparar no aviso evita a falha.</summary>
        public bool Repara()
        {
            Incidente i = Inc;
            if (i == null || i.Tipo != TipoIncidente.Falha || i.Resolvido || Reparo > 0 || !T.RegraIncidentes || !MecanicoLivre || Cash < T.ReparoCusto) return false;
            Cash -= T.ReparoCusto;
            Reparo = Passos(T.ReparoSeg);
            Emit(Ev.ReparoIniciado, (int)i.Estacao, T.ReparoCusto);
            return true;
        }

        /// <summary>Atraso de conexao, a partir do aviso: o grupo passa a frente em toda fila.</summary>
        public bool Prioriza()
        {
            Incidente i = Inc;
            if (i == null || i.Tipo != TipoIncidente.Conexao || i.Resolvido || !T.RegraIncidentes) return false;
            Voo v = Voos[i.Vaga];
            if (v.Partiu) return false;
            i.Resolvido = v.Prioridade = true;
            Emit(Ev.Priorizado, v.Id);
            return true;
        }
    }
}
