using System;
using System.Collections.Generic;

namespace TA.Core
{
    /// <summary>
    /// Tripulacao (CONTRATO_FASE_C §2): pilotos e comissarios por Cash, fixos e nomeados; escala automatica (o 1o livre na ordem do quadro)
    /// com troca manual; trecho conta na decolagem; DescansoSeg na sala = descansado; depois de TrechosAteDescanso trechos, so voa
    /// descansado. Sem tripulacao completa o embarque nao abre: custa so a receita dos assentos vazios.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Pilotos e comissarios na ordem de contratacao (Id = indice).</summary>
        public readonly List<Tripulante> Tripulacao = new List<Tripulante>();

        int DescansoPassos => Passos(T.DescansoSeg);

        /// <summary>Na sala e apto a voar: abaixo do limite de trechos ou ja descansado.</summary>
        public bool Livre(Tripulante t) => t.Aviao < 0 && (t.Trechos < T.TrechosAteDescanso || t.Sala >= DescansoPassos);

        /// <summary>s que faltam para o tripulante na sala ficar descansado (0 = descansado ou a bordo).</summary>
        public float DescansoFalta(Tripulante t) => t.Aviao >= 0 ? 0f : Math.Max(0, DescansoPassos - t.Sala) * Step;

        public int ABordo(int aviao, Funcao f)
        {
            int n = 0;
            foreach (Tripulante t in Tripulacao) if (t.Aviao == aviao && t.Funcao == f) n++;
            return n;
        }

        bool TripCompleta(Aviao a)
        {
            for (int f = 0; f < T.TripPorVoo.Length; f++) if (ABordo(a.Id, (Funcao)f) < T.TripPorVoo[f]) return false;
            return true;
        }

        public int Quantos(Funcao f)
        {
            int n = 0;
            foreach (Tripulante t in Tripulacao) if (t.Funcao == f) n++;
            return n;
        }

        Tripulante NovoTripulante(Funcao f) => new Tripulante { Id = Tripulacao.Count, Indice = Quantos(f), Funcao = f, Sala = DescansoPassos };

        /// <summary>Um passo na sala: o tempo de descanso anda; descansado zera os trechos.</summary>
        void Sala()
        {
            foreach (Tripulante t in Tripulacao)
                if (t.Aviao < 0 && t.Sala < DescansoPassos && ++t.Sala >= DescansoPassos) t.Trechos = 0;
        }

        /// <summary>A tripulacao desce para a sala. Voou (pouso): o descanso recomeca e quem bateu o limite de trechos avisa. Nao voou (hangar
        /// antes do embarque): volta com o descanso que tinha.</summary>
        void Desembarca(Aviao a, bool voou)
        {
            foreach (Tripulante t in Tripulacao)
            {
                if (t.Aviao != a.Id) continue;
                t.Aviao = -1;
                if (!voou) continue;
                t.Sala = 0;
                if (t.Trechos >= T.TrechosAteDescanso) Emit(Ev.Descanso, t.Id, (DescansoPassos + Hz - 1) / Hz);
            }
        }

        /// <summary>O padrao: aviao no patio, antes do embarque, com vaga vazia pega o 1o livre na ordem do quadro da tripulacao. Sem ninguem
        /// livre, avisa uma vez por voo (Ev.SemTripulacao, o aviso antecipado do §5.4).</summary>
        void EscalaAuto()
        {
            foreach (Aviao a in Avioes)
            {
                if (!a.NoPatio || a.VooId < 0 || a.FechouEm >= 0 || (VooDo(a)?.Embarque ?? false)) continue;
                for (int f = 0; f < T.TripPorVoo.Length; f++)
                    for (int n = ABordo(a.Id, (Funcao)f); n < T.TripPorVoo[f]; n++)
                    {
                        Tripulante t = Tripulacao.Find(x => x.Funcao == (Funcao)f && Livre(x));
                        if (t == null)
                        {
                            if (!a.AvisouTrip && T.RegraTripulacao) { a.AvisouTrip = true; Emit(Ev.SemTripulacao, a.VooId, f); }
                            break;
                        }
                        t.Aviao = a.Id;
                        Emit(Ev.Escalado, t.Id, a.VooId, 0);
                    }
            }
        }

        /// <summary>Troca manual no quadro de partidas: poe o tripulante (livre) no voo, no lugar do primeiro da mesma funcao (que volta para a
        /// sala sem perder o descanso que tinha). So com o aviao no patio e antes de o embarque abrir.</summary>
        public bool Escala(int voo, int tripulante)
        {
            if (tripulante < 0 || tripulante >= Tripulacao.Count) return false;
            Tripulante t = Tripulacao[tripulante];
            if (!Livre(t)) return false;
            Aviao a = null;
            foreach (Aviao x in Avioes) if (x.VooId == voo && voo >= 0) a = x;
            if (a == null || AntesDoEmbarque(a.Id) == null) return false;
            if (ABordo(a.Id, t.Funcao) >= T.TripPorVoo[(int)t.Funcao])
                Tripulacao.Find(x => x.Aviao == a.Id && x.Funcao == t.Funcao).Aviao = -1;
            t.Aviao = a.Id;
            Emit(Ev.Escalado, t.Id, voo, 1);
            return true;
        }

        /// <summary>Teto de cada funcao no marco atual do terminal.</summary>
        public int TetoTripulacao => T.TripTeto[Math.Min(Marco, T.TripTeto.Length - 1)];

        /// <summary>Custo do proximo tripulante da funcao (-1 = no teto). Preco fixo pela ordem: nada sai de sorteio.</summary>
        public long CustoTripulante(Funcao f)
        {
            if ((int)f < 0 || (int)f >= T.TripCusto.Length) return -1;
            int n = Quantos(f), i = n - T.TripIniciais;
            return n >= TetoTripulacao || i < 0 || i >= T.TripCusto[(int)f].Length ? -1 : T.TripCusto[(int)f][i];
        }

        /// <summary>Contrata piloto ou comissario: entra descansado na sala, com o proximo nome da funcao.</summary>
        public bool HireTripulante(Funcao f)
        {
            long c = CustoTripulante(f);
            if (c < 0 || Cash < c) return false;
            Cash -= c;
            Tripulante t = NovoTripulante(f);
            Tripulacao.Add(t);
            Emit(Ev.TripulanteContratado, t.Id, (int)c, (int)f);
            return true;
        }
    }
}
