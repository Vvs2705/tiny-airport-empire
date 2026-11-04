using System;
using System.Text;

// TA-3, as 20 micro-historias de passageiros (GDD §5.9; docs/HISTORIAS.md). Uma por onda a partir da 2a, sorteada pela semente entre as
// do aeroporto e do marco atuais (as ineditas primeiro, depois as que ainda nao foram entregues); nada se compra. Cada uma pede uma coisa
// que o nucleo confere: embarcar a tempo, passar por uma parada, ser atendida por um perfil numa estacao ou o voo partir sem perder
// ninguem. Cumprida, entra no album. O texto fica na View (Textos.Historia[indice]).
namespace TA.Core
{
    /// <summary>Uma historia: onde aparece e o que a fecha.</summary>
    public sealed class Historia
    {
        /// <summary>Id estavel. O indice na tabela e' o bit do album no save: a tabela so cresce no fim.</summary>
        public string Id;
        /// <summary>Aeroporto em que aparece e o marco minimo do terminal.</summary>
        public int Aeroporto, Marco;
        /// <summary>Estacao em que o passageiro tem de ser atendido (null = so embarcar). Loja, bagagem e alimentacao sao paradas opcionais: a
        /// historia vai para quem ja tem o motivo dela (Pax.Loja, Mala, Fome), e a parada tem de estar funcionando quando ele passa.</summary>
        public Estacao? Onde;
        /// <summary>Perfil que tem de estar presente em Onde quando ele e' atendido (null = qualquer um).</summary>
        public Perfil? Quem;
        /// <summary>O voo dele parte sem perder ninguem: a historia fecha quando o portao fecha.</summary>
        public bool Todos;
    }

    public static partial class Tabelas
    {
        /// <summary>As 20 do lancamento: 8 em Vale Verde, 6 em Costa Azul, 6 em Aurora Global.</summary>
        public static readonly Historia[] Historias =
        {
            new Historia { Id = "rosa_casamento" },
            new Historia { Id = "lia_entrevista", Onde = Estacao.CheckIn, Quem = Perfil.Atendente },
            new Historia { Id = "pipoca_gato", Onde = Estacao.Seguranca, Quem = Perfil.Seguranca },
            new Historia { Id = "tonico_presente", Onde = Estacao.Loja },
            new Historia { Id = "familia_souza", Marco = 1, Todos = true },
            new Historia { Id = "ze_violao", Marco = 1, Onde = Estacao.Bagagem },
            new Historia { Id = "gabi_reencontro", Marco = 1, Onde = Estacao.Alimentacao },
            new Historia { Id = "theo_primeiro_voo", Marco = 1, Onde = Estacao.Portao, Quem = Perfil.Atendente },
            new Historia { Id = "lua_de_mel", Aeroporto = 1, Marco = 2, Onde = Estacao.Loja, Quem = Perfil.Vendedor },
            new Historia { Id = "time_volei", Aeroporto = 1, Marco = 2, Todos = true },
            new Historia { Id = "lurdes_mar", Aeroporto = 1, Marco = 2 },
            new Historia { Id = "nina_prancha", Aeroporto = 1, Marco = 2, Onde = Estacao.Bagagem, Quem = Perfil.Bagageiro },
            new Historia { Id = "bolinha_cao", Aeroporto = 1, Marco = 3, Onde = Estacao.Seguranca, Quem = Perfil.Seguranca },
            new Historia { Id = "abilio_terra", Aeroporto = 1, Marco = 3, Onde = Estacao.Alimentacao },
            new Historia { Id = "kenji_conexao", Aeroporto = 2, Marco = 3 },
            new Historia { Id = "coral_lisboa", Aeroporto = 2, Marco = 3, Todos = true },
            new Historia { Id = "iara_pesquisa", Aeroporto = 2, Marco = 3, Onde = Estacao.Loja, Quem = Perfil.Vendedor },
            new Historia { Id = "mariana_intercambio", Aeroporto = 2, Marco = 3, Onde = Estacao.Alimentacao },
            new Historia { Id = "hiro_neta", Aeroporto = 2, Marco = 3, Onde = Estacao.Portao, Quem = Perfil.Atendente },
            new Historia { Id = "banda_festival", Aeroporto = 2, Marco = 3, Onde = Estacao.Seguranca, Quem = Perfil.Seguranca },
        };
    }

    public sealed partial class Sim
    {
        /// <summary>A historia da onda (indice em T.Historias; -1 = nenhuma) e o passageiro dela (Pax.Id; -1).</summary>
        public int HistoriaOnda = -1, HistoriaPax = -1;
        /// <summary>0 = falta o pedido; 1 = o pedido foi cumprido (Onde e Quem); 2 = embarcou e espera o voo partir sem perder ninguem (Todos).</summary>
        public int HistoriaPasso;
        /// <summary>As que ja apareceram (bit = indice; o album e' parte delas): o sorteio prefere as ineditas e o album so mostra o titulo delas.</summary>
        public int HistoriasSaiu;
        public Historia HistoriaAtual => HistoriaOnda >= 0 ? T.Historias[HistoriaOnda] : null;
        /// <summary>Todos os bits da tabela. ponytail: int, ate 31 historias; long quando a tabela passar disso.</summary>
        public int TodasHistorias => (1 << T.Historias.Length) - 1;

        /// <summary>No comeco da onda: entre as que cabem nela (PaxDaHistoria), a Dona Rosa abre o album; depois as ineditas, as que ainda faltam
        /// entregar e, com o album do aeroporto cheio, qualquer uma de novo. Qual delas sai da semente.</summary>
        void SorteiaHistoria(int w)
        {
            HistoriaOnda = HistoriaPax = -1;
            HistoriaPasso = 0;
            if (!T.RegraHistorias || w < 1) return;
            int cabe = 0;
            for (int h = 0; h < T.Historias.Length; h++) if (PaxDaHistoria(h) >= 0) cabe |= 1 << h;
            int nova = cabe & ~HistoriasSaiu, falta = cabe & ~Album;
            int pool = (nova & 1) != 0 ? 1 : nova != 0 ? nova : falta != 0 ? falta : cabe;
            int n = 0;
            for (int x = pool; x != 0; x &= x - 1) n++;
            if (n == 0) return;
            int k = (int)(U(w, 0, 0, 40) * n);
            for (int h = 0; h < T.Historias.Length; h++)
                if ((pool >> h & 1) != 0 && k-- == 0) { HistoriaOnda = h; break; }
            HistoriaPax = PaxDaHistoria(HistoriaOnda);
            HistoriasSaiu |= 1 << HistoriaOnda;
        }

        /// <summary>O passageiro da historia h nesta onda (Pax.Id) ou -1 se ela nao cabe: aeroporto e marco, a estacao ja aberta, o voo da
        /// sequencia de Weyl (o outro se este nao tem gente ou, sendo conexao, nao passa pelo check-in e pela bagagem) e, do meio da fila, o
        /// 1o com o motivo da parada.</summary>
        int PaxDaHistoria(int h)
        {
            Historia x = T.Historias[h];
            if (x.Aeroporto != AeroportoAtual || x.Marco > Marco || x.Onde is Estacao e && !Ativa(e)) return -1;
            bool balcao = x.Onde == Estacao.CheckIn || x.Onde == Estacao.Bagagem;
            int f0 = VooDaHistoria(Onda);
            for (int k = 0; k < 2; k++)
            {
                Voo v = Voos[(f0 + k) % 2];
                if (v == null || v.Base <= 0 || balcao && Efeito(v).Tipo == TipoRota.Conexao) continue;
                for (int j = 0; j < v.Base; j++)
                {
                    Pax p = NovoPax(PaxId(v.Onda, v.Indice, (v.Base / 2 + j) % v.Base));
                    if (x.Onde == Estacao.Loja ? p.Loja : x.Onde == Estacao.Bagagem ? p.Mala : x.Onde == Estacao.Alimentacao ? p.Fome : true) return p.Id;
                }
            }
            return -1;
        }

        /// <summary>O passageiro da historia terminou o atendimento em `e`: cumpre o pedido se e' a estacao dele, com o perfil presente.</summary>
        void CumpreHistoria(Estacao e)
        {
            Historia x = HistoriaAtual;
            if (x == null || x.Onde != e) return;
            if (x.Quem is Perfil q && !Staff.Exists(f => f.Perfil == q && f.Em == e && f.Andando == 0)) return;
            HistoriaPasso = 1;
        }

        /// <summary>Embarcou: com o pedido cumprido, entra no album (Todos: espera o portao fechar sem perder ninguem). Sem a regra, a v0.1.</summary>
        void EmbarcaHistoria(Pax p)
        {
            if (!T.RegraHistorias) { Entrega(0, p.Id); return; }
            Historia x = HistoriaAtual;
            if (x == null || x.Onde != null && HistoriaPasso != 1) return;   // nao cumpriu: volta numa proxima onda
            if (x.Todos) HistoriaPasso = 2; else Entrega(HistoriaOnda, p.Id);
        }

        /// <summary>No fechamento do portao: a historia Todos fecha se o voo dela partiu sem perder ninguem.</summary>
        void FechaHistoria(Voo v)
        {
            if (HistoriaPasso != 2 || HistoriaPax / 1000 != v.Onda || HistoriaPax / 500 % 2 != v.Indice) return;
            HistoriaPasso = 0;
            if (v.Perdidos == 0) Entrega(HistoriaOnda, HistoriaPax);
        }

        void Entrega(int h, int pax)
        {
            bool nova = (Album >> h & 1) == 0;
            Album |= 1 << h;
            HistoriasSaiu |= 1 << h;   // a entregue ja saiu, tambem sem a regra (a Dona Rosa da v0.1)
            Emit(Ev.Historia, h, pax, nova ? 1 : 0);
        }

        /// <summary>Schema 16: hp = historia da onda, passageiro, passo, as que ja sairam.</summary>
        void SalvaHistorias(StringBuilder sb) =>
            sb.Append("hp=").Append(HistoriaOnda).Append(',').Append(HistoriaPax).Append(',').Append(HistoriaPasso).Append(',').Append(HistoriasSaiu).Append('\n');

        /// <summary>A historia da onda gravada, com o passageiro num voo desta onda; save de antes do 16 ou lixo sorteia de novo, como no
        /// comeco da onda. Nao refaz o sorteio para conferir: o grupo extra de um incidente muda o meio da fila no meio da onda. Chamado com os
        /// voos da onda prontos e antes das filas (NovoPax le HistoriaPax).</summary>
        void CarregaHistoria(int[] hp)
        {
            if (!T.RegraHistorias) return;
            bool nenhuma = hp.Length >= 4 && hp[0] == -1 && hp[1] == -1;
            if (hp.Length >= 4 && (nenhuma || hp[0] >= 0 && hp[0] < T.Historias.Length && hp[1] >= 0 && hp[1] / 1000 == Onda && hp[1] % 500 < Voos[hp[1] / 500 % 2].Total))
            {
                HistoriaOnda = hp[0]; HistoriaPax = hp[1];
                HistoriaPasso = nenhuma ? 0 : Math.Max(0, Math.Min(2, hp[2]));
                if (!nenhuma) HistoriasSaiu |= 1 << HistoriaOnda;
            }
            else SorteiaHistoria(Onda);
        }
    }
}
