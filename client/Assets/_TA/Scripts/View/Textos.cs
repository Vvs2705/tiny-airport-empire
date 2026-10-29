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

        public static readonly string[] Estacao = { L("CHECK-IN"), L("SEGURANÇA"), L("LOJA"), L("PORTÃO") };
        public static readonly string[] Funcionario = { L("Ana"), L("Beto"), L("Caio") };
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
        public static string Contratar(long custo) => custo < 0 ? L("Equipe completa") : L("Contratar ") + Num(custo);
        public static readonly string Melhorias = L("Melhorias"), Pausa = L("II"), Fechar = L("Fechar"), Continuar = L("Continuar"),
            Pausado = L("Pausado"), ReverTutorial = L("Rever tutorial"), ExportarDiario = L("Exportar diário"), Album = L("Álbum"),
            Quadro = L("PARTIDAS"), Toque = L("TOQUE"), Fechada = L("fechada: sem funcionário"), Entendi = L("ENTENDI"), Pular = L("Pular tutorial"),
            Cancelar = L("Cancelar"), Coletar = L("Coletar"), SoltaFora = L("Solte o funcionário em cima de uma estação"),
            AnuncioSimulado = L("Anúncio (simulado na v0.1)"), FecharSemRecompensa = L("Fechar sem recompensa"),
            SaveRecusado = L("Este save é de outra versão do jogo: ele não será apagado nem gravado por cima."),
            DiarioVazio = L("Ainda não há diário para exportar."), DiarioPasta = L("Diário aberto na pasta."), DiarioErro = L("Não deu para exportar o diário."),
            HistoriaEntregue = L("História entregue! Ela foi para o álbum.");

        public static string Balcoes(int n, float seg) => n + (n == 1 ? L(" balcão · ") : L(" balcões · ")) + Seg(seg);
        public static string EstadoTexto(Estado e, int voo) => e == Estado.Critico ? L("CRÍTICO: ") + Voo(voo) + L(" em risco")
            : e == Estado.Atencao ? L("Atenção: ") + Voo(voo) : L("Normal");
        public static string Mover(int f) => L("Mover ") + Funcionario[f] + L(" para:");
        public static string Andando(int f) => Funcionario[f] + L(" a caminho");
        public static string Status(Voo v) => v.Partiu ? (v.Perdidos == 0 ? L("PARTIU") : L("PARTIU · ") + v.Perdidos + L(" ficaram"))
            : v.Embarque ? L("EMBARCANDO") : L("CHECK-IN");
        public static string ABordo(Voo v) => v.Embarcados + "/" + v.Total + L(" a bordo");
        public static string Fecha(double seg) => L("fecha ") + Hora(seg);
        public static string Partiu(Voo v) => Voo(v.Id) + L(" partiu: ") + v.Embarcados + "/" + v.Total + L(" a bordo");
        public static string Bonus(long c) => L("Voo cheio! +") + Num(c) + L(" de pontualidade");
        public static string Marco(int m) => L("O terminal cresceu! Nível ") + m + L(": tarifa maior");
        public static string Terminal(int m) => L("Terminal Vale Verde · nível ") + m;
        public static string Cofre(long cash, long seg) => L("Enquanto você esteve fora (") + (seg / 60) + L(" min, teto de 2 h), o aeroporto rendeu +") + Num(cash)
            + L(" Cash. Nenhum voo foi perdido: o quadro esperou você.");
        public static string Dobrar(long c) => L("Dobrar (+") + Num(c) + L(") com anúncio");
        public static string Receita2x(int hoje, int max) => L("2x renda 5 min (") + (max - hoje) + L(" hoje)");
        public static string Receita2xAtiva(int seg) => L("2x renda: ") + (seg / 60) + ":" + (seg % 60).ToString("00", CultureInfo.InvariantCulture);
        public static string AnuncioConta(int s) => L("O anúncio termina em ") + s + L(" s");
        public static string MelhoriaLinha(Melhoria m, int nivel, int max, string efeito) => Melhoria[(int)m] + " " + nivel + "/" + max + "\n" + efeito;
        public static string Custo(long c) => c < 0 ? L("máximo") : Num(c);
        public static string AlbumLinha(int h, bool tem) => (tem ? L("Entregue: ") : L("Ainda não: ")) + Historia[h];

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
