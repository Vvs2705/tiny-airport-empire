using System;
using System.Collections.Generic;
using System.Text;

namespace TA.Core
{
    /// <summary>O que um nivel do passe da (CONTRATO_FASE_C7 §2). O indice nao vai no save.</summary>
    public enum Premio { Cash, Tempo, Cosmetico }

    /// <summary>Um nivel da trilha gratuita. Cash = Cash fixo; Tempo = s de 2x renda; Cosmetico = Id do cosmetico (sim.Tem(Id)), Valor = 1.</summary>
    public sealed class Nivel
    {
        public Premio Premio;
        public int Valor;
        public string Id = "";
    }

    /// <summary>Uma temporada do passe (docs/PASSE.md e' esta tabela). Id estavel; Produto = o da trilha premium na loja (sim.Tem(Produto));
    /// Dias = a duracao em dias do jogo (DiaJogo); Gratis = a trilha gratuita, nivel 1 na posicao 0.</summary>
    public sealed class Temporada
    {
        public string Id = "", Produto = "";
        public int Dias;
        public Nivel[] Gratis;
    }

    public static partial class Tabelas
    {
        static Nivel N(int cash) => new Nivel { Premio = Premio.Cash, Valor = cash };
        static Nivel Min(int min) => new Nivel { Premio = Premio.Tempo, Valor = min * 60 };
        static Nivel Cosmetico(string id) => new Nivel { Premio = Premio.Cosmetico, Valor = 1, Id = id };

        /// <summary>As temporadas, na ordem em que abrem. A posicao vai no save: temporada nova so no fim.</summary>
        public static readonly Temporada[] Temporadas =
        {
            new Temporada
            {
                Id = "s1", Produto = "season_pass_s1", Dias = 28,
                Gratis = new[]
                {
                    N(200), Min(5), N(300), N(400), Min(10), N(500), N(600), Min(10), N(800), Cosmetico("terminal_decor_flags_s1"),
                    N(1000), Min(10), N(1200), N(1400), Min(15), N(1600), N(1800), Min(15), N(2000), Cosmetico("aircraft_skin_sunset_s1"),
                    N(2200), Min(15), N(2400), N(2600), Min(15), N(2800), N(3000), Min(15), N(3500), Cosmetico("aircraft_skin_gold_s1"),
                },
            },
        };
    }

    /// <summary>
    /// Passe de temporada, trilha gratuita (CONTRATO_FASE_C7, docs/PASSE.md). Os pontos de temporada saem do que o nucleo ja mede: voo que
    /// partiu, diaria, semanal e objetivo da campanha. Sao barra, nao moeda: nada compra, gasta ou repoe ponto. A temporada corre em dias do
    /// jogo (DiaJogo): com o jogo fechado nada anda, e faltar um dia nao existe. O nivel paga sozinho no passo fixo em que fecha, uma vez:
    /// nao ha o que resgatar nem prazo. Cash fora do Ganha (sem 2x, fora do cofre). Tuning.RegraPasse = false = conta e mostra, sem pagar.
    /// </summary>
    public sealed partial class Sim
    {
        int _temp, _inicio, _pagos;

        /// <summary>Pontos da temporada em curso, de 0 ao ultimo nivel.</summary>
        public int PontosPasse;

        /// <summary>A temporada em curso (null = a tabela acabou: o passe fica fechado ate uma atualizacao trazer a proxima).</summary>
        public Temporada Temporada => _temp < T.Temporadas.Length ? T.Temporadas[_temp] : null;

        public int DiasRestantesPasse => Temporada is Temporada t ? _inicio + t.Dias - DiaJogo : 0;

        /// <summary>Niveis alcancados (= pagos: o nivel paga no passo em que fecha).</summary>
        public int NivelPasse => Temporada is Temporada t ? Math.Min(t.Gratis.Length, PontosPasse / T.PassePontosNivel) : 0;

        /// <summary>Teto do 2x: o booster mais o tempo do passe que chega com ele ligado (o booster so se compra com o 2x parado).</summary>
        int BoostTeto => 2 * Math.Max(T.BoostSegundos, T.BoosterSeg) * Hz;

        /// <summary>Todo evento passa aqui (Emit). Anuncio, compra, Gems e offline nao entram: ponto de temporada nao se compra.</summary>
        void AnotaPasse(Ev t, int a, int c)
        {
            if (Temporada == null) return;
            int p;
            switch (t)
            {
                case Ev.VooPartiu: p = c > 0 ? T.PasseVoo : 0; break;   // embarcados nao contam: voo perdido custa so a receita dele
                case Ev.MissaoConcluida: p = a == 0 ? T.PasseDiaria : T.PasseSemanal; break;
                case Ev.ObjetivoConcluido: p = T.PasseObjetivo; break;
                default: return;
            }
            PontosPasse = Math.Min(Temporada.Gratis.Length * T.PassePontosNivel, PontosPasse + p);
        }

        /// <summary>Fim do passo fixo: paga o que fechou e, no fim dos dias, vira a temporada (os pontos zeram; Cash e cosmetico ficam).</summary>
        void PassoPasse()
        {
            Temporada t = Temporada;
            if (t == null) { _inicio = -1; return; }   // fechado: a temporada que chegar numa atualizacao abre no dia em que chegar
            // ponytail: a trilha premium entra aqui: 2a coluna na Temporada, paga com Tem(t.Produto) do nivel 1 ao NivelPasse (retroativo), com o contador dela no save
            while (_pagos < NivelPasse)
            {
                Nivel n = t.Gratis[_pagos++];
                int v = T.RegraPasse ? n.Valor : 0;
                if (n.Premio == Premio.Cash) SomaCash(v);
                else if (n.Premio == Premio.Tempo) BoostLeft = (int)Math.Min(BoostTeto, BoostLeft + (long)v * Hz);
                else if (v > 0) _posse.Add(n.Id);
                Emit(Ev.PasseNivel, _temp, _pagos, v);
            }
            if (DiaJogo < _inicio + t.Dias) return;
            _temp++; _inicio = DiaJogo; PontosPasse = 0; _pagos = 0;
        }

        /// <summary>Schema 9 (CONTRATO_FASE_C7 §5): temporada, dia do jogo em que abriu (-1 = passe fechado), pontos e niveis pagos.</summary>
        void SalvaPasse(StringBuilder sb) => sb.Append("passe=").Append(_temp).Append(',').Append(_inicio).Append(',').Append(PontosPasse).Append(',').Append(_pagos).Append('\n');

        /// <summary>Le o schema 9, com tudo limitado. O schema 8 nao tem a chave: a 1a temporada abre no dia do jogo de agora, com 0 pontos.</summary>
        void CarregaPasse(Dictionary<string, string> kv)
        {
            _inicio = DiaJogo;
            int[] p = Ints(kv, "passe");
            if (p.Length < 4) return;
            _temp = Math.Max(0, Math.Min(T.Temporadas.Length, p[0]));
            if (Temporada == null) return;
            _inicio = p[1] < 0 ? DiaJogo : Math.Max(DiaJogo - Temporada.Dias, Math.Min(DiaJogo, p[1]));
            PontosPasse = Math.Max(0, Math.Min(Temporada.Gratis.Length * T.PassePontosNivel, p[2]));
            _pagos = Math.Max(0, Math.Min(NivelPasse, p[3]));
        }
    }
}
