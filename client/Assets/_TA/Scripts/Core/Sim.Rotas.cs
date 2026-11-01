using System;

namespace TA.Core
{
    /// <summary>
    /// Aeroportos e rotas (CONTRATO_FASE_C3 §2 e §3). A empresa e' uma so: viajar troca o aeroporto em operacao, com Cash, equipe, niveis e
    /// frota. O quadro continua com 2 voos por onda: a rota e' o que cada vaga voa, e o que se programa vale na proxima onda.
    /// Preco, marco e efeito sao da tabela: nada aqui depende da semente. Chave Tuning.Regra* desligada = so o nome muda.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Aeroporto em operacao, os ja pagos (bit por aeroporto), a viagem marcada para a proxima onda (-1 = nenhuma) e as rotas
        /// abertas (bit por rota).</summary>
        public int AeroportoAtual, AeroportosPagos, ProximoAeroporto = -1, RotasAbertas;
        /// <summary>Rota de cada vaga a partir da proxima onda.</summary>
        public readonly int[] Prog = new int[2];

        public Aeroporto Aero => T.RegraAeroportos ? T.Aeroportos[AeroportoAtual] : Aeroporto.Neutro;
        /// <summary>O efeito da rota r (so visual: a rota neutra, igual a da v0.1).</summary>
        Rota Efeito(int r) => T.RegraRotas ? T.Rotas[r] : Rota.Neutra;
        Rota Efeito(Voo v) => Efeito(v.Rota);

        public bool RotaAberta(int r) => r >= 0 && r < T.Rotas.Length && (RotasAbertas >> r & 1) != 0;
        bool Pago(int a) => (AeroportosPagos >> a & 1) != 0;

        /// <summary>Segundos fora do patio em cada trecho da rota r (a distancia).</summary>
        public float TrechoSeg(int r) => T.Trecho + Efeito(r).Extra;

        /// <summary>Chegou ao aeroporto a: as rotas sem preco abrem e entram nas vagas.</summary>
        void Chega(int a)
        {
            AeroportoAtual = a;
            AeroportosPagos |= 1 << a;
            ProximoAeroporto = -1;
            for (int r = 0; r < T.Rotas.Length; r++) if (T.Rotas[r].Aeroporto == a && T.Rotas[r].Custo == 0) RotasAbertas |= 1 << r;
            for (int f = 0; f < Prog.Length; f++) Prog[f] = T.RotaFixa >= 0 ? T.RotaFixa : RotaInicial(a, f);
        }

        /// <summary>A f-esima rota sem preco do aeroporto a (a tabela tem 2 por aeroporto): a padrao da vaga f.</summary>
        int RotaInicial(int a, int f)
        {
            for (int r = 0; r < T.Rotas.Length; r++) if (T.Rotas[r].Aeroporto == a && T.Rotas[r].Custo == 0 && f-- == 0) return r;
            return 0;
        }

        /// <summary>Preco da primeira ida (0 = ja pago; -1 = nao da: e' o atual, antes do marco dele ou ja ha viagem marcada).</summary>
        public long CustoViagem(int a)
        {
            if (a < 0 || a >= T.Aeroportos.Length || a == AeroportoAtual || ProximoAeroporto >= 0 || Marco < T.Aeroportos[a].Marco) return -1;
            return Pago(a) ? 0 : T.Aeroportos[a].Custo;
        }

        /// <summary>Marca a viagem: paga agora e chega na proxima onda (os voos desta onda terminam aqui).</summary>
        public bool Viajar(int a)
        {
            long c = CustoViagem(a);
            if (c < 0 || Cash < c) return false;
            Cash -= c;
            AeroportosPagos |= 1 << a;
            ProximoAeroporto = a;
            Emit(Ev.Viagem, a, (int)c);
            return true;
        }

        /// <summary>Preco da rota r (-1 = ja aberta, aeroporto ainda nao pago ou antes do marco dela). Preco fixo da tabela.</summary>
        public long CustoRota(int r)
        {
            if (r < 0 || r >= T.Rotas.Length || RotaAberta(r) || !Pago(T.Rotas[r].Aeroporto) || Marco < T.Rotas[r].Marco) return -1;
            return T.Rotas[r].Custo;
        }

        public bool AbreRota(int r)
        {
            long c = CustoRota(r);
            if (c < 0 || Cash < c) return false;
            Cash -= c;
            RotasAbertas |= 1 << r;
            Emit(Ev.RotaAberta, r, (int)c);
            return true;
        }

        /// <summary>A vaga (0 ou 1) voa a rota r a partir da proxima onda. So rota aberta do aeroporto atual.</summary>
        public bool Programa(int vaga, int r)
        {
            if (vaga < 0 || vaga >= Prog.Length || !RotaAberta(r) || T.Rotas[r].Aeroporto != AeroportoAtual) return false;
            Prog[vaga] = r;
            Emit(Ev.RotaProgramada, vaga, r);
            return true;
        }

        /// <summary>Passageiros do voo da rota r na onda w, com a demanda da hora e da estacao do ano (sem o grupo de incidente).</summary>
        int PaxDaRota(int w, int r) => (int)Math.Round(T.Pax(w) * (double)Efeito(r).Pax * Demanda(w));

        /// <summary>Tarifa do passageiro no voo v: a do marco com o fator da rota; o grupo de conexao atrasada paga Tuning.ConexaoTarifa x.</summary>
        int TarifaDe(Voo v, Pax p)
        {
            int t = (int)Math.Round(Tarifa * (double)Efeito(v).Tarifa);
            return v.GrupoConexao && p.Indice >= v.Base ? t * T.ConexaoTarifa : t;
        }

        double Gasto(Voo v) => v != null ? Efeito(v).Gasto : 1.0;
    }
}
