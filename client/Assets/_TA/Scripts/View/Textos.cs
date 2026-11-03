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
        public static readonly string[] Servico = { L("Desembarque"), L("Limpeza e lanche"), L("Combustível"), L("Bagagem") };
        public static readonly string[] Funcao = { L("PILOTOS"), L("COMISSÁRIOS") };
        public static readonly string[] AviaoNome = { L("Regional"), L("Jato") };
        /// <summary>Os nomes da Tabelas.Rotas, na mesma ordem: o destino tem um nome so, no quadro, na campanha e na tela do voo.</summary>
        public static readonly string[] Destino = System.Array.ConvertAll(Tabelas.Rotas, r => L(r.Nome));
        public static readonly string[] Historia = System.Array.ConvertAll(Historias.Texto, L);
        public static readonly string[] Melhoria = { L("Velocidade"), L("Capacidade") };

        public static string Num(long n) => n.ToString("#,0", Br);
        public static string Seg(float s) => s.ToString("0.0", Br) + " s";
        public static string Voo(int id) => "LT " + (101 + id).ToString(CultureInfo.InvariantCulture);
        public static string DestinoDe(int id) => Destino[id % Destino.Length];
        /// <summary>O destino de verdade do voo (Voo.Rota). O quadro ainda chama DestinoDe(int): trocar quando a frente do HUD mexer ali.</summary>
        public static string DestinoDe(Voo v) => Destino[v.Rota % Destino.Length];
        /// <summary>Hora do quadro: 1 s de jogo = 1 min, o dia comeca as 16:00 (o 1o voo, para Campinas, fecha as 18:00 como diz a Lara).</summary>
        public static string Hora(double segJogo)
        {
            long m = (long)System.Math.Floor(segJogo) + 16 * 60;
            return ((m / 60) % 24).ToString("00", CultureInfo.InvariantCulture) + ":" + (m % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        public static string Onda(int n) => L("Onda ") + n;
        /// <summary>O botao de velocidade: 1x, 2x, 3x.</summary>
        public static string Vezes(int v) => v.ToString(CultureInfo.InvariantCulture) + L("x");
        public static string Contratar(long custo) => custo < 0 ? L("Equipe completa") : L("Contratar\n") + Num(custo);
        public static readonly string Melhorias = L("Melhorias"), Pausa = L("II"), Continuar = L("Continuar"),
            Pausado = L("Pausado"), ReverTutorial = L("Rever tutorial"), ExportarDiario = L("Exportar diário"), Album = L("Álbum"),
            Quadro = L("PARTIDAS"), Toque = L("TOQUE"), FechadaCurto = L("(fechada)"), Entendi = L("Entendi"), Pular = L("Pular tutorial"),
            Coletar = L("Coletar"), SoltaFora = L("Solte o funcionário sobre uma estação"),
            AnuncioSimulado = L("Anúncio (simulado na v0.1)"), FecharSemRecompensa = L("Fechar sem recompensa"),
            SaveRecusado = L("Este jogo salvo é de outra versão: não será apagado nem sobrescrito."),
            DiarioVazio = L("Ainda não há diário para exportar."), DiarioPasta = L("Diário aberto na pasta."), DiarioErro = L("Não deu para exportar o diário."),
            HistoriaEntregue = L("História entregue! Ela foi para o álbum."),
            Equipe = L("Equipe"), CofreTitulo = L("Bem-vindo de volta"), Tripulacao = L("Tripulação"), Patio = L("Pátio"), MoverCurto = L("Mover"),
            Escala = L("ESCALA DO VOO"), Reparo = L("Reparo só com Cash e tempo. Nenhum avião se perde."),
            EscalaFechada = L("A escala fecha quando o embarque abre."), EscalaSemVoo = L("O avião ainda não encostou para este voo."),
            AvisoHangar = L("No hangar, este voo não embarca: você perde só o Cash dele."),
            TripulacaoSala = L("Tripulação na sala");

        public static string Balcoes(int n, float seg) => n + (n == 1 ? L(" balcão · ") : L(" balcões · ")) + Seg(seg);
        public static string EstadoTexto(Estado e, int voo) => e == Estado.Critico ? L("CRÍTICO: ") + Voo(voo) + L(" em risco")
            : e == Estado.Atencao ? L("ATENÇÃO: ") + Voo(voo) : L("Normal");
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
            if (a.Pane) return L("EM PANE");
            return a.Pendente || a.Check > 0 ? L("MANUTENÇÃO") : L("PÁTIO");
        }
        public static string ABordo(Voo v) => v.Embarcados + "/" + v.Total + L(" a bordo");
        public static string Fecha(double seg) => L("fecha ") + Hora(seg);
        public static string Partiu(Voo v) => Voo(v.Id) + L(" partiu: ") + v.Embarcados + "/" + v.Total + L(" a bordo");
        public static string Bonus(long c) => L("Voo cheio! +") + Num(c) + L(" Cash de pontualidade");
        public static string Marco(int m) => L("O terminal cresceu! Nível ") + m + L(": tarifa maior");
        public static string Terminal(int m) => L("Terminal Vale Verde · nível ") + m;
        public static string Cofre(long cash, long seg) => L("Enquanto você esteve fora (") + (seg / 60) + L("\u00A0min, limite de 2\u00A0h), o aeroporto rendeu +") + Num(cash)
            + L(" Cash. Nenhum voo foi perdido: o quadro esperou você.");
        public static string Receita2x(int hoje, int max) => L("5 min · ") + (max - hoje) + L(" hoje");
        public static string FechaEm(int min) => L("Fecha em ") + min + L("\u00A0min");
        public static readonly string Dobro = L("2x"), ColunaFecha = L("FECHA"), ColunaSituacao = L("SITUAÇÃO");
        public static string AnuncioConta(int s) => L("O anúncio termina em ") + s + L(" s");

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
        /// <summary>Por que a checagem nao esta na mesa agora (na ordem das travas de Sim.PodeChecagem); "" = esta.</summary>
        public static string PorQueNaoChecar(Aviao a, bool embarcando, bool mecanicoLivre, long cash, int custo)
        {
            if (a.Check > 0) return L("O mecânico já está neste avião.");
            if (!a.NoPatio) return L("Só com o avião no pátio.");
            if (embarcando) return L("Embarque aberto: fica para o próximo pouso.");
            if (a.Pendente) return L("Com 100%, a inspeção é grátis e segura o voo. Ou hangar.");
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
        public static string ManutencaoAviso(int d) => L("Avião com ") + d + L("% de desgaste: toque no voo para decidir.");
        public static readonly string ManutencaoPendente = L("Manutenção pendente: inspeção ou hangar.");
        public static readonly string HangarFeito = L("Revisão pronta: desgaste zerado.");
        public static string EstacaoAberta(int e) => L("Nova estação: ") + Estacao[e].ToLowerInvariant() + L("! Veja a Equipe.");
        public static readonly string[] EstacaoNo = { L("no check-in"), L("na segurança"), L("na loja"), L("no portão"), L("na bagagem"), L("na alimentação"), L("na oficina"), L("no despacho") };
        public static readonly string[] EstacaoPara = { L("para o check-in"), L("para a segurança"), L("para a loja"), L("para o portão"), L("para a bagagem"), L("para a alimentação"), L("para a oficina"), L("para o despacho") };
        public static string EmEstacao(int e, bool andando) => andando ? L("Indo ") + EstacaoPara[e] : L("Trabalhando ") + EstacaoNo[e];
        public static string PerfilInfo(int p) => L("Melhor em ") + PerfilCasa[p];
        public static readonly string ContratarCurto = L("Contratar"), ContrateAntes = L("Contrate antes"), Contratados = L("Contratados"), CashNome = L("Cash"), TerminalNome = L("Terminal");
        public static string Faltam(long c) => (c == 1 ? L("Falta ") : L("Faltam ")) + Num(c);
        public static string DeTotal(int a, int b) => a + L(" de ") + b;
        public static string NivelNome(int nivel) => L("Nível ") + nivel;
        public static string FaltamEmbarques(long n) => L("Embarque mais ") + Num(n) + (n == 1 ? L(" passageiro") : L(" passageiros"));

        // ---------- telas modais: rotulos curtos dos cartoes e dos botoes ----------
        public static readonly string Maximo = L("Máximo"), Melhorar = L("Melhorar"), Completa = L("Completa"), Compradas = L("Compradas"), Aqui = L("Aqui"),
            Embarcados = L("Embarcados"), HistoriasNome = L("Histórias"), SemMelhoria = L("Esta estação não tem melhoria"),
            Rendeu = L("Rendeu enquanto você esteve fora"), DobrarAnuncio = L("Dobrar com anúncio"),
            TutorialLegenda = L("Os passos da Lara, de novo"), DiarioLegenda = L("Salva o diário em um arquivo"), AlbumLegenda = L("As histórias que você entregou"),
            Rever = L("Rever"), Exportar = L("Exportar"), Abrir = L("Abrir"),
            EquipePatioNome = L("Equipe de pátio"), UmServico = L("Cada pessoa faz um serviço por vez"), VerVoo = L("Ver voo"), LimiteSobe = L("O limite sobe com o terminal"),
            DesgasteNome = L("Desgaste"), ChecagemNome = L("Checagem rápida"), HangarNome = L("Revisão no hangar"), Checar = L("Checar"), Revisar = L("Revisar"),
            AgoraNao = L("Agora não"), Escalar = L("Escalar"), ABordoCurto = L("A bordo"), DescansaCurto = L("Descansa");
        public static string MelhoriaNome(Melhoria m, int nivel, int max) => Melhoria[(int)m] + " " + nivel + "/" + max;
        public static string VeiculosNome(int nivel, int max) => L("Veículos ") + nivel + "/" + max;
        public static string PorPassageiro(string seg) => seg + L(" por passageiro");
        public static string PorChecagem(string seg) => seg + L(" por checagem");
        public static string PorAviao(string seg) => seg + L(" por avião");
        public static string BalcoesDe(string n) => n + L(" balcões");
        public static string VeiculosDe(string n) => n + L(" veículos");
        public static string NaEstacao(int n) => n == 0 ? L("Ninguém aqui") : n == 1 ? L("1 pessoa aqui") : n + L(" pessoas aqui");
        public static string ChecagemEfeito(int tira, float seg) => "-" + tira + "% · " + Seg(seg);
        public static string HangarEfeito(float seg) => L("Zera o desgaste em ") + Tempo(seg);
        public static string AlbumEstado(bool tem) => tem ? L("Entregue") : L("Ainda não");

        // ---------- avisos de perigo (GDD §5.8: causa, duracao e opcoes; bombeiros so de rotina, sem fogo nem acidente) ----------
        /// <summary>Na ordem de TipoIncidente.</summary>
        public static readonly string[] Perigo = { L("Frente fria"), L("Falha"), L("Pico de passageiros"), L("Conexão atrasada"), L("Temporal"), L("Vento"), L("Neblina"), L("Neve") };
        static string SegInt(float s) => (int)s + L("\u00A0s");
        public static string PerigoEm(string nome, int seg) => nome + L(" em ") + SegInt(seg);
        public static string PerigoResta(string nome, int seg) => nome + ": " + SegInt(seg);
        public static string Previsao(TipoIncidente t) => L("Previsão: ") + Perigo[(int)t];
        public static string FalhaEm(int e) => L("Falha ") + EstacaoNo[e];
        public static string CausaClima(Clima c) => c.ParaPatio ? L("Pista e pátio param por ") + SegInt(c.Seg) : c.FechaPista ? L("Pista fechada por ") + SegInt(c.Seg)
            : c.Espera > 0f ? L("Pousos e decolagens esperam ") + SegInt(c.Espera) : L("Degelo de ") + SegInt(c.Degelo) + L(" antes de cada voo");
        public static string CausaFalha(int fator, float seg) => L("Atende ") + fator + L("x mais devagar por ") + SegInt(seg);
        public static string CausaGrupo(int voo) => L("Grupo extra no ") + Voo(voo);
        public static string FazerAdiantar(int voo) => L("Adiante o embarque do ") + Voo(voo);
        public static string FazerAtrasar(int voo) => L("Atrase o portão do ") + Voo(voo);
        public static string FazerReforcar(int e) => L("Mova a equipe ") + EstacaoPara[e];
        public static string EmPane(string aviao) => aviao + L(" em pane");
        public static string Voltando(string aviao, int seg) => aviao + L(" voltando: ") + SegInt(seg);
        public static string Abortou(string aviao) => aviao + L(" abortou a decolagem");
        public static string SaiEm(int seg) => L("Sai de novo em ") + SegInt(seg);
        public static string RiscoFalha(int d) => Desgaste(d) + L(": pode falhar");
        public static readonly string FazerReparar = L("Repare ou leve o mecânico"), FazerPriorizar = L("Priorize e ele fura as filas"), JaDecidiu = L("Decidido: agora é esperar"),
            CausaPane = L("Embarque parado até o reparo"), FazerPane = L("Repare agora ou espere a inspeção"), Consertando = L("Mecânico trabalhando"),
            PousoPrioridade = L("Pouso seguro, com prioridade"), Prontidao = L("Bombeiros em prontidão, de rotina"), NadaAFazer = L("Não precisa fazer nada"),
            Adiantar = L("Adiantar"), Atrasar = L("Atrasar"), Priorizar = L("Priorizar"), Reparar = L("Reparar"),
            AtrasoNao = L("Não dá para atrasar: o clima passa da virada da onda."), MecanicoOcupado = L("O mecânico está ocupado."), PousoSeguro = L("Pouso seguro. Passageiros remarcados.");

        // ---------- metas: campanha, missoes e conquistas (docs/CAMPANHA.md e docs/MISSOES.md; o nome e' o da tabela do nucleo) ----------
        public static readonly string[] AeroportoNome = System.Array.ConvertAll(Tabelas.Aeroportos, a => L(a.Nome));
        public static readonly string Metas = L("Metas"), Campanha = L("Campanha"), Missoes = L("Missões"), Conquistas = L("Conquistas"),
            Diarias = L("DIÁRIAS"), Semanais = L("SEMANAIS"), Feito = L("Feito"), Feita = L("Feita"), Premio = L("Prêmio"), Selo = L("Selo");
        public static string NomeMeta(Objetivo o) => L(o.Nome);
        public static string Conta(long a, long b) => Num(a) + "/" + Num(b);
        public static string NovasEm(int ondas) => L("Novas em ") + ondas + (ondas == 1 ? L(" onda") : L(" ondas"));
        static string Cash(long c) => c > 0 ? " · +" + Num(c) + L(" Cash") : "";
        public static string ObjetivoFeito(string nome, long cash) => L("Objetivo feito: ") + nome + Cash(cash);
        public static string MissaoFeita(string nome, long cash) => L("Missão feita: ") + nome + Cash(cash);
        public static string ConquistaNova(string nome) => L("Conquista: ") + nome + "!";
        static string Plural(int n, string um, string varios) => Num(n) + " " + (n == 1 ? um : varios);

        // ---------- passe de temporada (docs/PASSE.md e CONTRATO_FASE_C7; os numeros sao os do nucleo) ----------
        public static readonly string Passe = L("Passe"), PasseTitulo = L("Passe de temporada"), NivelRotulo = L("Nível"), DiasRotulo = L("Dias do jogo"),
            ComoFunciona = L("Como funciona"), Gratis = L("GRÁTIS"), Premium = L("PREMIUM"), Recebido = L("Recebido"), TrilhaCompleta = L("Trilha completa"),
            PremiumTitulo = L("Trilha premium"), PremiumLiberadaTitulo = L("Trilha premium liberada"),
            PremiumTrancada = L("Vem com a loja, numa atualização. Por enquanto não tem preço nem compra."),
            PremiumLiberada = L("Ela já é sua. Os prêmios dela chegam com a loja."),
            PremiumNaoPrecisa = L("A campanha e a trilha grátis fecham sem ela."), ComALoja = L("Com a loja"), Liberada = L("Liberada"),
            PasseFechado = L("A próxima temporada chega numa atualização. O que você ganhou continua seu.");
        /// <summary>As regras visiveis (GDD §6.4): de onde vem o ponto, quando o premio entra, o que expira e quanto dura.</summary>
        public static string[] RegrasPasse(Tuning t, int dias) => new[]
        {
            L("Ponto vem do jogo: voo que partiu ") + t.PasseVoo + L(", diária ") + t.PasseDiaria + L(", semanal ") + t.PasseSemanal
                + L(", objetivo da campanha ") + t.PasseObjetivo + L(". Ponto não se compra."),
            L("O prêmio entra na hora em que o nível fecha. Coletar só marca como visto: não há prazo para resgatar, nem depois do fim."),
            L("Nada expira: Cash, 2× e cosmético ficam com você. A temporada dura ") + dias + L(" dias do jogo e só anda com o jogo aberto."),
        };
        public static string CosmeticoNome(string id)
        {
            switch (id)
            {
                case "terminal_decor_flags_s1": return L("Bandeirolas no saguão");
                case "aircraft_skin_sunset_s1": return L("Pintura pôr do sol");
                case "aircraft_skin_gold_s1": return L("Pintura dourada");
                default: return id;
            }
        }
        public static string PremioNome(Nivel n) => n.Premio == TA.Core.Premio.Cash ? "+" + Num(n.Valor) + L(" Cash")   // Premio aqui e' o texto "Prêmio"
            : n.Premio == TA.Core.Premio.Tempo ?(n.Valor / 60) + L(" min de 2×") : CosmeticoNome(n.Id);
        public static string NivelCurto(int n) => L("Nível ") + n;
        public static string ParaNivel(int n) => L("Para o nível ") + n;
        public static string FaltamPontos(int n) => (n == 1 ? L("Falta ") : L("Faltam ")) + Plural(n, L("ponto"), L("pontos"));
        public static string AbreCom(int pontos) => L("Abre com ") + Num(pontos) + L(" pontos");
        public static string PasseNivelFeito(int n, Nivel premio) => L("Passe, nível ") + n + ": " + PremioNome(premio);

        /// <summary>O que a meta pede, em uma frase: a coluna Condição do CAMPANHA.md e do MISSOES.md, montada de Meta, Arg e Alvo.</summary>
        public static string MetaTexto(Objetivo o)
        {
            int n = o.Alvo, a = o.Arg;
            switch (o.Meta)
            {
                case Meta.Embarcados: return L("Embarque ") + Plural(n, L("passageiro"), L("passageiros"));
                case Meta.Equipe: return n == 1 ? L("Contrate 1 pessoa") : L("Tenha ") + n + L(" pessoas na equipe");
                case Meta.Movidos: return L("Mova alguém ") + Plural(n, L("vez"), L("vezes")) + L(" para uma fila em alerta");
                case Meta.Velocidade: return L("Velocidade ") + n + " " + EstacaoNo[a];
                case Meta.Capacidade: return L("Capacidade ") + n + " " + EstacaoNo[a];
                case Meta.Historias: return L("Embarque ") + Plural(n, L("passageiro"), L("passageiros")) + L(" com história");
                case Meta.Atendidos: return L("Atenda ") + Plural(n, L("passageiro"), L("passageiros")) + " " + EstacaoNo[a];
                case Meta.Climas: return L("Resolva ") + Plural(n, L("clima"), L("climas"));
                case Meta.Incidentes: return L("Resolva ") + Plural(n, L("incidente"), L("incidentes"));
                case Meta.Cheios: return L("Faça ") + Plural(n, L("voo lotado"), L("voos lotados")) + (a >= 0 ? L(" para ") + Destino[a] : "");
                case Meta.Voos: return L("Faça ") + Plural(n, L("voo"), L("voos")) + (a >= 0 ? L(" para ") + Destino[a] : "");
                case Meta.Seguidos: return L("Faça ") + n + L(" voos lotados seguidos");
                case Meta.Pontuais: return L("Ganhe ") + Num(n) + L(" bônus de pontualidade");
                case Meta.Checagens: return L("Faça ") + Plural(n, L("checagem rápida"), L("checagens rápidas"));
                case Meta.Hangar: return L("Faça ") + Plural(n, L("revisão no hangar"), L("revisões no hangar"));
                case Meta.Marco: return L("Leve o terminal ao nível ") + (n + 1);
                case Meta.Chegou: return L("Viaje para ") + AeroportoNome[a];
                case Meta.Melhorias: return L("Compre ") + Plural(n, L("melhoria"), L("melhorias"));
                case Meta.Passageiros: return L("Embarque ") + Num(n) + L(" passageiros no total");
                case Meta.Rotas: return L("Abra ") + Plural(n, L("rota"), L("rotas"));
                case Meta.Campanha: return L("Feche ") + Plural(n, L("objetivo"), L("objetivos")) + L(" da campanha");
                default: return L("Feche ") + Plural(n, L("missão"), L("missões"));   // Missoes
            }
        }

        // ---------- ficha ao tocar (GDD de direcao §3.4 e §8.4): quem e, o que faz agora e por que esta assim ----------
        /// <summary>Etapa do passageiro na fila e no atendimento, na ordem de Estacao (oficina e despacho nao tem passageiro).</summary>
        public static readonly string[] PaxNaFila = { L("Na fila do check-in"), L("Na fila da segurança"), L("Na fila da loja"), L("Esperando o embarque"), L("Na fila da bagagem"), L("Na fila da alimentação"), "", "" },
            PaxAtendido = { L("Fazendo o check-in"), L("Na inspeção da segurança"), L("Comprando na loja"), L("Embarcando"), L("Despachando a mala"), L("Pedindo um lanche"), "", "" };
        /// <summary>Na ordem de TA.Humor: o nome (a linha do topo) e o motivo (a ficha).</summary>
        public static readonly string[] Humor = { L("Tranquilo"), L("Impaciente"), L("Preocupado"), L("Chateado"), L("Irritado"), L("Furioso"), L("Contente") },
            HumorPorque = { L("tudo no horário"), L("esperando há muito tempo"), L("o portão fecha logo"), L("o voo atrasou"), L("no ritmo da fila, perde o voo"), L("perdeu o voo"), L("embarcou a tempo") };
        public static readonly string NaFilaDoEmbarque = L("Na fila do embarque"), IndoAoAviao = L("Indo para o avião"), IndoEmbora = L("Perdeu o voo e vai embora"),
            ABordoLogo = L("A bordo em instantes"), PortaoFechou = L("O portão fechou antes do embarque"), ComHistoria = L(" · com história"),
            AtendendoUm = L("Atendendo um passageiro"), ChamandoProximo = L("Chamando o próximo da fila"), EsperandoPax = L("Esperando passageiro"),
            NaChecagem = L("Ajudando na checagem do avião"), ProntoChecagem = L("Pronto para a próxima checagem"), NoBonus = L("Garantindo o bônus de pontualidade"),
            NaEspecialidade = L("Está na especialidade"), ACaminho = L("A caminho"), Atendendo = L("Atendendo"), Trabalhando = L("Trabalhando"), Livre = L("Livre"),
            VoltaSala = L("Voltando para a sala da tripulação"), EscalaNaTela = L("A escala fica na tela Tripulação"),
            GargaloFalha = L("Gargalo: em falha, atende mais devagar"), GargaloToque = L("Sem equipe: só anda com o seu toque"), GargaloFechada = L("Fechada: ninguém da equipe aqui"),
            EquipeVindo = L("A equipe está a caminho"), SemGargalo = L("Fluindo: sem gargalo"), Trancada = L("Abre quando o terminal crescer");
        public static string HumorLinha(int h) => Humor[h] + ": " + HumorPorque[h];
        public static string NoAeroporto(float seg, float fecha, bool atrasado) => L("No aeroporto há ") + Tempo(seg)
            + (fecha <= 0f ? "" : (atrasado ? L(" · portão atrasado: fecha em ") : L(" · portão fecha em ")) + Tempo(fecha));
        public static string ChegaEm(float seg) => L("Chega em ") + Tempo(seg);
        public static string NaFila(int n) => n + L(" na fila");
        public static string FuncaoDe(Tripulante t) => t.Funcao == TA.Core.Funcao.Piloto ? L("Piloto") : t.Indice % 2 == 0 ? L("Comissária") : L("Comissário");   // a folha da comissaria vai nos indices pares
        public static string IndoPara(int voo) => voo >= 0 ? L("Indo para o ") + Voo(voo) : IndoAoAviao;
        public static string Trechos(int n, int limite) => n + "/" + limite + L(" trechos desde o descanso");
        public static string GargaloRisco(int voo) => L("Gargalo: a fila não dá conta do ") + Voo(voo);
        public static string GargaloFolga(int voo) => L("Atenção: folga curta para o ") + Voo(voo);

        // ---------- tutorial (GDD_ORIGEM "Onboarding", passos 1-12) ----------
        // Titulo = o que fazer; texto = por que, em uma ou duas frases. Seta: use o guillemet U+00BB; a seta U+2192 nao existe nas fontes. Passos 10 a 12 = evento.
        public static readonly string Lara = L("Lara, Chefe de Terminal");
        public static readonly string[] TourTitulo =
        {
            L("Bem-vindo ao Vale Verde"), L("Toque no check-in"), L("Toque na segurança"), L("Este é o quadro de partidas"), L("Embarque no portão"),
            L("Contrate a atendente"), L("Arraste alguém para a segurança"), L("Vermelho é gargalo"), L("Acelere a segurança"),
            L("Passageiro com história"), L("O terminal cresceu"), L("O aeroporto rendeu sem você"),
        };
        public static readonly string[] TourTexto =
        {
            L("Este aeroporto estava abandonado, mas hoje sai o primeiro voo: às 18:00, para Campinas. Tem gente que não pode perder esse avião. Vamos abrir o check-in?"),
            L("Sem funcionário no balcão, cada toque atende um passageiro. Quando a equipe chegar, ela faz isso sozinha."),
            L("O passageiro passa por uma estação de cada vez. Depois do check-in vem a segurança: cada toque leva um pelo raio-x."),
            L("Cada voo fecha o portão na hora marcada. Quem não embarcar fica, e você perde só o Cash daquele assento: nada mais."),
            L("O embarque abre 60\u00A0min antes da partida. Toque no portão: cada passageiro que embarca vira Cash."),
            L("Toque em Contratar. A Ana assume o check-in e atende sozinha, sem o seu toque."),
            L("Segure uma funcionária e solte sobre a segurança. Ela leva 2,5\u00A0s para chegar e não atende ninguém nesse tempo. Ou toque nela e escolha Mover."),
            L("A estação vermelha não vai dar conta do voo marcado. Duas saídas: mover alguém para lá ou comprar uma melhoria."),
            L("Toque em Melhorias e compre Velocidade na segurança: a fila anda mais rápido. Capacidade põe mais gente atendendo ao mesmo tempo."),
            L("O coração vermelho marca um passageiro com história. Se ele embarcar a tempo, a história vai para o Álbum, no menu de pausa."),
            L("Cada passageiro embarcado faz o terminal subir de nível. Nível maior paga mais por passageiro e abre estações: veja a Equipe."),
            L("Esse Cash veio do tempo em que você esteve fora. O limite é 2\u00A0h: o que passar disso não conta, então vale voltar antes."),
        };
        public static string TourConta(int n, int total) => n + "/" + total;

        // Configuracoes, privacidade e suporte (GDD §9.3 e §11.1, D-12). A politica e a pagina do idioma: o en aponta para /privacy/.
        public const string EmailSuporte = "suporte@vstack-solutions.com.br";
        public static readonly string UrlPrivacidade = L("https://vvs2705.github.io/tiny-airport-empire/privacidade/");
        public static readonly string Configuracoes = L("Configurações"), ConfigLegenda = L("Som, acessibilidade e privacidade"),
            Musica = L("Música"), Efeitos = L("Efeitos sonoros"), Vibracao = L("Vibração"), VibracaoLegenda = L("Liga ou desliga a vibração"),
            ReduzirMovimento = L("Reduzir movimento"), MovimentoLegenda = L("Sem balanço, tremor nem pulso"),
            TextoMaior = L("Texto maior"), TextoMaiorLegenda = L("Letras maiores onde couber"), Ligado = L("Ligado"), Desligado = L("Desligado"),
            Idioma = L("Idioma"), IdiomaNome = L("Português (Brasil)"), EmUso = L("Em uso"),
            Privacidade = L("Política de privacidade"), PrivacidadeLegenda = L("O que o jogo guarda e por quê"),
            Suporte = L("Suporte"), Escrever = L("Escrever"), Creditos = L("Créditos"),
            CreditosTexto = L("Fontes Lilita One (© 2011 Juan Montoreano, com o Nome de Fonte Reservado Lilita) e Nunito (© 2014 The Nunito Project Authors), na SIL Open Font License 1.1."),
            ExcluirDados = L("Excluir meus dados"), ExcluirLegenda = L("Apaga o progresso deste aparelho"), Excluir = L("Excluir"),
            Cancelar = L("Cancelar"), ApagarTudo = L("Apagar tudo"),
            ExcluirTexto = L("Apaga deste aparelho o seu progresso, as configurações, o tutorial já visto e o diário de jogo. Não dá para desfazer: o jogo fecha e, ao abrir de novo, começa do zero.\n\nO jogo não guarda seus dados em servidor nosso. Cópias do diário que você exportou ficam onde você as salvou. Para qualquer pedido sobre seus dados, escreva para ") + EmailSuporte + ".";
        public static string Porcento(float v) => (int)System.Math.Round(v * 100f) + "%";
        public static string Versao(string v) => L("Little Terminal, da V-STACK · versão ") + v;
    }
}
