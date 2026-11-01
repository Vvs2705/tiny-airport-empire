using System.Globalization;
using TA.Core;

namespace TA
{
    /// <summary>
    /// Todo texto de tela num lugar so (padrao do dc:Scripts/View/Textos.cs). A v0.1 so tem pt-BR; os idiomas esperam a D-08: cada texto
    /// passa por L(pt) e ganha as colunas en/es/id aqui, sem tocar nas telas. Nada de texto solto no Hud ou no Tour.
    /// </summary>
    public static class Textos
    {
        static string L(string pt) => pt;
        static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

        public static readonly string[] Estacao = { L("CHECK-IN"), L("SEGURANÇA"), L("LOJA"), L("PORTÃO"), L("BAGAGEM"), L("ALIMENTAÇÃO"), L("OFICINA"), L("DESPACHO") };
        public static readonly string[] Funcionario = { L("Ana"), L("Beto"), L("Cida"), L("Dani"), L("Edu"), L("Fátima") };
        public static readonly string[] Perfil = { L("Atendente"), L("Agente de segurança"), L("Vendedora"), L("Bagageira"), L("Mecânico"), L("Despachante") };
        /// <summary>Onde cada perfil trabalha melhor (GDD §5.5). A especializacao e so visual (D-C2a): o texto diz a casa, sem prometer bonus.</summary>
        public static readonly string[] PerfilCasa = { L("check-in e portão"), L("segurança"), L("loja e alimentação"), L("bagagem"), L("oficina"), L("despacho") };
        public static readonly string[] Servico = { L("Desembarque"), L("Limpeza e catering"), L("Combustível"), L("Bagagem") };
        public static readonly string[] Funcao = { L("PILOTOS"), L("COMISSÁRIOS") };
        public static readonly string[] AviaoNome = { L("Regional"), L("Jato") };
        public static readonly string[] Destino = { L("Campinas"), L("Recife"), L("Curitiba"), L("Salvador"), L("Belo Horizonte"), L("Porto Alegre"),
            L("Florianópolis"), L("Fortaleza"), L("Brasília"), L("Manaus") };
        public static readonly string[] Historia = { L("Dona Rosa vai ao casamento da neta") };
        public static readonly string[] Melhoria = { L("Velocidade"), L("Capacidade") };

        public static string Num(long n) => n.ToString("#,0", Br);
        public static string Seg(float s) => s.ToString("0.0", Br) + " s";
        public static string Voo(int id) => "LT " + (101 + id).ToString(CultureInfo.InvariantCulture);
        public static string DestinoDe(int id) => Destino[id % Destino.Length];
        /// <summary>Hora do quadro: 1 s de jogo = 1 min, o dia comeca as 16:00 (o 1o voo, para Campinas, fecha as 18:00 como diz a Lara).</summary>
        public static string Hora(double segJogo)
        {
            long m = (long)System.Math.Floor(segJogo) + 16 * 60;
            return ((m / 60) % 24).ToString("00", CultureInfo.InvariantCulture) + ":" + (m % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        public static string Onda(int n) => L("Onda ") + n;
        public static string Contratar(long custo) => custo < 0 ? L("Equipe completa") : L("Contratar\n") + Num(custo);
        public static readonly string Melhorias = L("Melhorias"), Pausa = L("II"), Fechar = L("Fechar"), Continuar = L("Continuar"),
            Pausado = L("Pausado"), ReverTutorial = L("Rever tutorial"), ExportarDiario = L("Exportar diário"), Album = L("Álbum"),
            Quadro = L("PARTIDAS"), Toque = L("TOQUE"), FechadaCurto = L("(fechada)"), Entendi = L("ENTENDI"), Pular = L("Pular tutorial"),
            Cancelar = L("Cancelar"), Coletar = L("Coletar"), SoltaFora = L("Solte o funcionário em cima de uma estação"),
            AnuncioSimulado = L("Anúncio (simulado na v0.1)"), FecharSemRecompensa = L("Fechar sem recompensa"),
            SaveRecusado = L("Este save é de outra versão do jogo: ele não será apagado nem gravado por cima."),
            DiarioVazio = L("Ainda não há diário para exportar."), DiarioPasta = L("Diário aberto na pasta."), DiarioErro = L("Não deu para exportar o diário."),
            HistoriaEntregue = L("História entregue! Ela foi para o álbum."),
            Equipe = L("Equipe"), Tripulacao = L("Tripulação"), Patio = L("Pátio"), MoverCurto = L("Mover"),
            Escala = L("ESCALA DO VOO"), Reparo = L("Reparo só com Cash e tempo. Nenhum avião se perde."),
            EscalaFechada = L("A escala fecha quando o embarque abre."), EscalaSemVoo = L("O avião ainda não encostou para este voo."),
            AvisoHangar = L("No hangar, este voo não embarca e perde só a receita dele."),
            TripulacaoSala = L("Tripulação na sala");

        public static string Balcoes(int n, float seg) => n + (n == 1 ? L(" balcão · ") : L(" balcões · ")) + Seg(seg);
        public static string EstadoTexto(Estado e, int voo) => e == Estado.Critico ? L("CRÍTICO: ") + Voo(voo) + L(" em risco")
            : e == Estado.Atencao ? L("Atenção: ") + Voo(voo) : L("Normal");
        public static string Mover(int f) => L("Mover ") + Funcionario[f] + L(" para:");
        public static string Andando(int f) => Funcionario[f] + L(" a caminho");
        /// <summary>O voo no quadro. Passou da hora do embarque sem abrir: diz o que esta segurando (CONTRATO_FASE_C §1: "o quadro avisa").</summary>
        public static string Status(Voo v, Aviao a, int passoNaOnda)
        {
            if (v.Partiu) return v.Perdidos == 0 ? L("PARTIU") : L("PARTIU · ") + v.Perdidos + L(" ficaram");
            if (v.Embarque) return L("EMBARCANDO");
            if (passoNaOnda < v.AbrePasso) return L("CHECK-IN");
            if (a.Hangar > 0) return L("NO HANGAR");
            if (!a.NoPatio) return L("A CAMINHO");
            return a.Pendente || a.Check > 0 ? L("MANUTENÇÃO") : L("PÁTIO");
        }
        public static string ABordo(Voo v) => v.Embarcados + "/" + v.Total + L(" a bordo");
        public static string Fecha(double seg) => L("fecha ") + Hora(seg);
        public static string Partiu(Voo v) => Voo(v.Id) + L(" partiu: ") + v.Embarcados + "/" + v.Total + L(" a bordo");
        public static string Bonus(long c) => L("Voo cheio! +") + Num(c) + L(" de pontualidade");
        public static string Marco(int m) => L("O terminal cresceu! Nível ") + m + L(": tarifa maior");
        public static string Terminal(int m) => L("Terminal Vale Verde · nível ") + m;
        public static string Cofre(long cash, long seg) => L("Enquanto você esteve fora (") + (seg / 60) + L(" min, teto de 2 h), o aeroporto rendeu +") + Num(cash)
            + L(" Cash. Nenhum voo foi perdido: o quadro esperou você.");
        public static string Dobrar(long c) => L("Dobrar (+") + Num(c) + L(") com anúncio");
        public static string Receita2x(int hoje, int max) => L("2x renda 5 min\n(") + (max - hoje) + L(" hoje)");
        public static string Receita2xAtiva(int seg) => L("2x renda\n") + (seg / 60) + ":" + (seg % 60).ToString("00", CultureInfo.InvariantCulture);
        public static string AnuncioConta(int s) => L("O anúncio termina em ") + s + L(" s");
        public static string MelhoriaLinha(Melhoria m, int nivel, int max, string efeito) => Melhoria[(int)m] + " " + nivel + "/" + max + "\n" + efeito;
        public static string Custo(long c) => c < 0 ? L("máximo") : Num(c);
        public static string AlbumLinha(int h, bool tem) => (tem ? L("Entregue: ") : L("Ainda não: ")) + Historia[h];

        // ---------- Fase C: tripulacao, patio, manutencao e equipe ----------
        public static string Nivel(int marco) => L("nível ") + (marco + 1);
        public static string PlacaTrancada(int e, int marco) => Estacao[e] + " · " + Nivel(marco);
        public static string Tempo(float seg) { int s = (int)System.Math.Ceiling(seg); return (s / 60) + ":" + (s % 60).ToString("00", CultureInfo.InvariantCulture); }
        public static string Porcento(int d) => d + "%";
        public static string VooTitulo(int id) => Voo(id) + " · " + DestinoDe(id);
        public static string Desgaste(int d) => L("Desgaste ") + d + "%";
        public static string Equipagem(string piloto, string comissario) => piloto.Length == 0 && comissario.Length == 0 ? TripulacaoSala : piloto + " · " + comissario;
        public static string AviaoEstado(int aviao, Aviao a, float falta)
        {
            string n = AviaoNome[aviao] + " · ";
            if (a.Hangar > 0) return n + L("no hangar, volta em ") + Tempo(falta);
            if (!a.NoPatio) return n + L("em voo");
            return n + (a.Check > 0 ? (a.Inspecao ? L("em inspeção") : L("em checagem")) : L("no pátio"));
        }
        public static string ChecagemBotao(int tira, float seg, long c) => L("Checagem rápida\n-") + tira + "% · " + Seg(seg) + " · " + Num(c);
        public static string HangarBotao(float seg, long c) => L("Revisão no hangar\nzera · ") + Tempo(seg) + " · " + Num(c);
        /// <summary>Por que a checagem nao esta na mesa agora (na ordem das travas de Sim.PodeChecagem); "" = esta.</summary>
        public static string PorQueNaoChecar(Aviao a, bool embarcando, bool mecanicoLivre, long cash, int custo)
        {
            if (a.Check > 0) return L("O mecânico já está neste avião.");
            if (!a.NoPatio) return L("Só com o avião no pátio.");
            if (embarcando) return L("Embarque aberto: fica para o próximo pouso.");
            if (a.Pendente) return L("Em 100%, a inspeção é de graça, mas segura o voo. Ou hangar.");
            if (a.Checou) return L("Já checado neste pouso.");
            if (a.Desgaste <= 0) return L("Sem desgaste para tirar.");
            if (!mecanicoLivre) return L("O mecânico está no outro avião.");
            return cash < custo ? L("Falta Cash para a checagem.") : "";
        }
        public static string TripStatus(Tripulante t, int vooId, float descansoFalta, int limite)
        {
            if (t.Aviao >= 0) return vooId >= 0 ? L("a bordo do ") + Voo(vooId) : L("a bordo");
            if (t.Trechos >= limite && descansoFalta > 0f) return L("descansa ") + Tempo(descansoFalta);
            return L("na sala · ") + t.Trechos + "/" + limite + L(" trechos");
        }
        public static string TripTroca(Tripulante t, int limite) => t.Nome + "\n" + t.Trechos + "/" + limite + L(" trechos");
        public static string TripContratar(Funcao f, long c, int teto) => c < 0 ? L("Teto de ") + teto + L("\nsobe com o terminal")
            : (f == TA.Core.Funcao.Piloto ? L("Contratar piloto\n") : L("Contratar comissário\n")) + Num(c);
        public static string PatioEquipe(int n) => L("Equipe de pátio: ") + n;
        public static string PatioContratar(long c) => c < 0 ? L("Equipe completa") : L("Contratar\n") + Num(c);
        public static string AviaoLinha(int vooId, int d) => (vooId >= 0 ? Voo(vooId) + "\n" : "") + Desgaste(d);
        public static string VeiculosLinha(int n, int max, long c) => L("Veículos ") + n + "/" + max + "\n" + Custo(c);
        public static string ServicoVel(int nivel, int max, float seg, float prox, long c) => Melhoria[0] + " " + nivel + "/" + max + "\n" + Seg(seg) + (c < 0 ? "" : " » " + Seg(prox) + " · " + Num(c));
        public static string ManutencaoAviso(int d) => L("Avião com ") + d + L("% de desgaste: toque no voo para decidir.");
        public static readonly string ManutencaoPendente = L("Manutenção pendente: inspeção ou hangar.");
        public static readonly string HangarFeito = L("Revisão pronta: desgaste zerado.");
        public static string EstacaoAberta(int e) => L("Nova estação: ") + Estacao[e] + L("! Veja a Equipe.");
        public static readonly string[] EstacaoNo = { L("no check-in"), L("na segurança"), L("na loja"), L("no portão"), L("na bagagem"), L("na alimentação"), L("na oficina"), L("no despacho") };
        public static readonly string[] EstacaoPara = { L("para o check-in"), L("para a segurança"), L("para a loja"), L("para o portão"), L("para a bagagem"), L("para a alimentação"), L("para a oficina"), L("para o despacho") };
        public static string EmEstacao(int e, bool andando) => andando ? L("Indo ") + EstacaoPara[e] : L("Trabalhando ") + EstacaoNo[e];
        public static string PerfilInfo(int p) => L("Melhor em ") + PerfilCasa[p];
        public static readonly string ContratarCurto = L("Contratar"), ContrateAntes = L("Contrate antes"), Contratados = L("Contratados"), CashNome = L("Cash"), TerminalNome = L("Terminal");
        public static string Faltam(long c) => L("Faltam ") + Num(c);
        public static string DeTotal(int a, int b) => a + L(" de ") + b;
        public static string NivelNome(int nivel) => L("Nível ") + nivel;
        public static string FaltamEmbarques(long n) => L("Embarque mais ") + Num(n) + (n == 1 ? L(" passageiro") : L(" passageiros"));

        // ---------- tutorial (GDD_ORIGEM "Onboarding", passos 1-9) ----------
        public static readonly string Lara = L("Lara, Chefe de Terminal");
        public static readonly string[] TourTitulo =
        {
            L("Bem-vindo ao Vale Verde"), L("Toque no check-in"), L("Toque na segurança"), L("Este é o quadro de partidas"), L("Embarque no portão"),
            L("Contrate a atendente"), L("Arraste a funcionária para a segurança"), L("Vermelho é gargalo"), L("Melhore a velocidade da segurança"),
        };
        public static readonly string[] TourTexto =
        {
            L("Este aeroporto estava abandonado, mas hoje sai o primeiro voo: 18h, para Campinas. Tem gente que não pode perder esse avião. Vamos abrir o check-in?"),
            L("Sem funcionário, cada toque atende um passageiro. O toque não acelera: ele faz o trabalho de quem ainda não está lá."),
            L("Depois do check-in vem o raio-x. Uma estação de cada vez."),
            L("Cada voo fecha o portão na hora marcada. Quem não embarcar fica, e você perde só a tarifa daquele assento: nada mais."),
            L("O embarque abre 60 min antes da partida. Toque no portão para embarcar."),
            L("Com Cash você contrata gente: a estação dela atende sozinha."),
            L("Arraste a Ana até a segurança. Ela anda 2,5 s e, nesse tempo, não atende em nenhuma das duas. Se preferir, toque nela e escolha \"mover para\"."),
            L("Estação vermelha não vai dar conta do voo marcado. Duas respostas: mover alguém para lá ou comprar melhoria."),
            L("Melhorias deixam a estação mais rápida ou com mais balcões. Comece pela segurança."),
        };
        public static string TourConta(int n, int total) => n + "/" + total;
    }
}
