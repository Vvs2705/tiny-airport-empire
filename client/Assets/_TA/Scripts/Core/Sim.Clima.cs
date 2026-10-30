using System;

namespace TA.Core
{
    /// <summary>
    /// Dia a dia, estacoes do ano, clima e falhas de aviao (CONTRATO_FASE_C4). Tudo sai do relogio do quadro e da semente (U puro): nada a
    /// gravar alem do que esta em andamento, e com o jogo fechado nada anda. O clima usa a vaga de incidente da Fase C3: um perigo por vez,
    /// com o mesmo aviso e a mesma folga. Falha so existe acima do aviso de desgaste e termina sempre em pouso seguro: nenhum aviao se perde.
    /// Tuning.RegraClima, RegraEpocas e RegraFalhas desligadas = o jogo da Fase C3.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Passo ate o qual a pista fica bloqueada (rejeicao de decolagem e pouso de retorno).</summary>
        public long PistaAte;

        // ------------------------------------------------------------------ relogio do jogo (nunca o do aparelho)

        long DiaPassos => (long)OndaPassos * T.DiaOndas;
        public int DiaJogo => (int)(Steps / DiaPassos);
        /// <summary>Hora do dia, de 0 a 24: a onda 0 comeca as Tuning.HoraInicial.</summary>
        public float Hora => (T.HoraInicial + 24f * (Steps % DiaPassos) / DiaPassos) % 24f;
        public bool Noite => Hora < 6f || Hora >= 18f;
        /// <summary>Estacao do ano. Chama Epoca porque Estacao ja e' o posto de trabalho do terminal.</summary>
        public Epoca Epoca => EpocaDe(Math.Max(0, Onda));

        Epoca EpocaDe(int w) => (Epoca)(w / (T.DiaOndas * T.EpocaDias) % 4);

        /// <summary>Fator de passageiros da onda w: hora do dia x estacao do ano no aeroporto. Abre no marco Tuning.ClimaMarco.</summary>
        double Demanda(int w) => T.RegraEpocas && Marco >= T.ClimaMarco ? T.DemandaHora[w % T.DiaOndas] * (double)T.Aeroportos[AeroportoAtual].Demanda[(int)EpocaDe(w)] : 1.0;

        // ------------------------------------------------------------------ clima

        /// <summary>A linha da tabela de clima do tipo t (null = nao e' clima).</summary>
        public Clima ClimaDe(TipoIncidente t)
        {
            foreach (Clima c in T.Climas) if (c.Tipo == t) return c;
            return null;
        }

        /// <summary>O clima em curso, entre Inicio e Fim (null = nenhum).</summary>
        Clima ClimaAgora => T.RegraIncidentes && _inc != null && Steps >= _inc.Inicio && Steps < _inc.Fim ? ClimaDe(_inc.Tipo) : null;

        /// <summary>Raio perto: servicos e embarque param por tempo definido.</summary>
        public bool PatioParado => ClimaAgora?.ParaPatio == true;

        /// <summary>Passos que cada pouso e decolagem espera agora (vento e neblina: capacidade da pista reduzida).</summary>
        public int EsperaPista { get { Clima c = ClimaAgora; return c != null && c.Espera > 0 ? Passos(c.Espera) : 0; } }

        /// <summary>Clima da onda seguinte, para o quadro de partidas. E' a semente que decide, entao acerta sempre. null = tempo bom.</summary>
        public TipoIncidente? Previsao => ClimaDaOnda(Onda + 1);

        /// <summary>O clima da onda w: tabela do aeroporto na estacao do ano, com a mesma folga dos incidentes. Viagem marcada = chegada na
        /// proxima onda, que nao tem perigo (CONTRATO_FASE_C3 §4).</summary>
        TipoIncidente? ClimaDaOnda(int w)
        {
            float[][] tabela = T.Aeroportos[AeroportoAtual].Clima;
            if (!T.RegraClima || tabela == null || ProximoAeroporto >= 0 || Marco < T.ClimaMarco || w - UltimoIncidente <= T.IncidenteFolga) return null;
            float[] chance = tabela[(int)EpocaDe(w)];
            double x = U(w, 0, 0, 30);
            for (int i = 0; i < chance.Length; i++) if ((x -= chance[i]) < 0) return T.Climas[i].Tipo;
            return null;
        }

        /// <summary>Clima no lugar do incidente da onda, na vaga de maior risco (Sim.Incidentes.cs).</summary>
        void NovoClima(int w, TipoIncidente tipo)
        {
            UltimoIncidente = w;
            Clima c = ClimaDe(tipo);
            int vaga = VagaDoPerigo(w);
            long inicio = (long)w * OndaPassos + Voos[vaga].FechaPasso - Passos(c.Antes);
            _inc = new Incidente { Tipo = tipo, Vaga = vaga, Inicio = inicio, Fim = inicio + Passos(c.Seg), Aviso = inicio - Passos(T.AvisoSeg) };
        }

        /// <summary>Passos que a pista ou o degelo seguram o aviao depois do portao. Quem adiantou o embarque na neve ja degelou.</summary>
        int EsperaDecolagem(Aviao a)
        {
            Clima c = ClimaAgora;
            if (c == null) return 0;
            return c.Espera > 0 ? Passos(c.Espera) : c.Degelo > 0 && VooDo(a)?.Adiantado != true ? Passos(c.Degelo) : 0;
        }

        /// <summary>O caminhao de degelo esta no aviao agora.</summary>
        public bool Degelando(int aviao)
        {
            if (aviao < 0 || aviao >= Avioes.Length) return false;
            Aviao a = Avioes[aviao];
            return ClimaAgora?.Degelo > 0 && a.NoPatio && a.FechouEm >= 0 && Steps < a.FechouEm + EsperaDecolagem(a);
        }

        /// <summary>Durante o clima: o portao do voo atingido fecha Tuning.AtrasoFolga s depois de reabrir, sem passar da virada da onda. Custa
        /// o bonus de pontualidade. Clima que termina depois da virada nao da para atrasar.</summary>
        public bool Atrasa()
        {
            Incidente i = Inc;
            if (i == null || i.Resolvido || ClimaDe(i.Tipo) == null || !T.RegraIncidentes) return false;
            Voo v = Voos[i.Vaga];
            long fim = i.Fim - (long)Onda * OndaPassos;
            if (v.Partiu || fim >= OndaPassos - 1) return false;
            int novo = (int)Math.Min(OndaPassos - 1, fim + Passos(T.AtrasoFolga)), antes = v.FechaPasso;
            if (novo <= antes) return false;
            v.FechaPasso = novo;
            i.Resolvido = v.Atrasado = true;
            Emit(Ev.Atrasado, v.Id, (novo - antes) / Hz);
            return true;
        }

        // ------------------------------------------------------------------ falhas de aviao

        /// <summary>Voar assim e' escolha do jogador: o aviso de desgaste e' o aviso previo da falha.</summary>
        public bool EmRisco(int aviao) => T.RegraFalhas && aviao >= 0 && aviao < Avioes.Length && Avioes[aviao].Desgaste >= T.DesgasteAviso;

        /// <summary>A falha do voo atual do aviao: semente, onda e aviao decidem, e nada pago entra na conta. Uma por voo.</summary>
        TipoFalha FalhaDe(Aviao a)
        {
            if (a.VooId < 0 || a.VooId == a.Falhou || !EmRisco(a.Id)) return TipoFalha.Nenhuma;
            int w = a.VooId / 2, soma = 0;
            if (U(w, a.Id, 0, 20) >= T.FalhaAviaoChance) return TipoFalha.Nenhuma;
            foreach (int p in T.FalhaAviaoPesos) soma += p;
            double x = U(w, a.Id, 0, 21) * soma;
            for (int i = 0; i < T.FalhaAviaoPesos.Length; i++) if ((x -= T.FalhaAviaoPesos[i]) < 0) return (TipoFalha)(i + 1);
            return TipoFalha.Nenhuma;
        }

        /// <summary>Hora de abrir o embarque: a pane segura o aviao no portao ate o reparo. A inspecao obrigatoria da Fase C resolve de graca,
        /// entao falta de Cash nunca trava o jogo.</summary>
        bool EntrouEmPane(Aviao a)
        {
            if (FalhaDe(a) != TipoFalha.Pane) return false;
            a.Falhou = a.VooId; a.Pane = true;
            Emit(Ev.Pane, a.Id, a.VooId);
            return true;
        }

        /// <summary>Hora de decolar: o aviao aborta na pista, que fica bloqueada, e sai depois, sem novo sorteio. O voo ja pagou: nada muda no Cash.</summary>
        bool RejeitouDecolagem(Aviao a)
        {
            if (FalhaDe(a) != TipoFalha.Rejeicao) return false;
            a.Falhou = a.VooId; a.SaiEm = Steps + Passos(T.RejeicaoSeg);
            PistaAte = Steps + Passos(T.PistaBloqueioSeg);
            Emit(Ev.Rejeicao, a.Id, a.VooId);
            return true;
        }

        /// <summary>Acabou de decolar: pede para voltar e pousa com prioridade (Sim.Patio).</summary>
        void DecolouComRetorno(Aviao a)
        {
            a.Retorno = Steps + Passos(T.RetornoPede);
            a.Chega = a.Retorno + Passos(T.RetornoVolta);
        }

        /// <summary>Pouso de retorno: sempre seguro. Bombeiros e ambulancia em prontidao (a View le Ev.RetornoPedido), passageiros remarcados, a
        /// pista bloqueada por pouco tempo e o aviao em pane.</summary>
        void PousoDeRetorno(Aviao a)
        {
            a.Retorno = 0; a.Pane = true;
            PistaAte = Steps + Passos(T.PistaBloqueioSeg);
            Emit(Ev.PousoSeguro, a.Id);
            Emit(Ev.Remarcados, a.Id);
        }

        /// <summary>Reparo da pane no portao: Tuning.PaneCusto Cash e o mecanico por Tuning.PaneSeg s, no lugar da inspecao. So Cash e tempo:
        /// nunca anuncio. Mecanico ocupado com outro aviao ou com a falha de equipamento: nao da.</summary>
        public bool ReparaAviao(int aviao)
        {
            if (aviao < 0 || aviao >= Avioes.Length || Cash < T.PaneCusto || Reparo > 0) return false;
            Aviao a = Avioes[aviao];
            if (!a.Pane || !a.NoPatio || a.Check > 0 && !a.Inspecao) return false;
            foreach (Aviao o in Avioes) if (o != a && o.Check > 0) return false;
            Cash -= T.PaneCusto;
            a.Check = PassosOficina(T.PaneSeg); a.Inspecao = false; a.Checou = true;
            Emit(Ev.PaneReparo, a.Id, T.PaneCusto);
            return true;
        }

        // ------------------------------------------------------------------ o que a tela oferece (GDD §5.8: causa, duracao e opcoes)

        public static Opcao[] Opcoes(TipoIncidente t)
        {
            switch (t)
            {
                case TipoIncidente.Falha: return new[] { Opcao.Reparar, Opcao.Segurar };
                case TipoIncidente.Pico: return new[] { Opcao.Reforcar, Opcao.Segurar };
                case TipoIncidente.Conexao: return new[] { Opcao.Priorizar, Opcao.Segurar };
                default: return new[] { Opcao.Adiantar, Opcao.Atrasar, Opcao.Segurar };
            }
        }

        public static Opcao[] Opcoes(TipoFalha f)
        {
            switch (f)
            {
                case TipoFalha.Pane: return new[] { Opcao.Reparar, Opcao.Hangar, Opcao.Segurar };
                case TipoFalha.Retorno: return new[] { Opcao.Reparar, Opcao.Segurar };
                default: return new[] { Opcao.Segurar };
            }
        }
    }
}
