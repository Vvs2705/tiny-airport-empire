using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Fluxo do jogo, o unico MonoBehaviour de fluxo (nasce sozinho na cena vazia por RuntimeInitializeOnLoadMethod; casca do
    /// dc:Scripts/View/Game.cs). Chama sim.Tick com o dt do quadro (parado com tela modal ou explicacao do tutorial), transforma os eventos
    /// do nucleo em diario CSV e avisos, grava o save (a cada 5 s de jogo, na pausa e ao sair, escrita atomica; nunca por cima de um save
    /// recusado) e paga o cofre offline ao abrir e ao voltar da pausa. O relogio do quadro nunca anda com o jogo fechado.
    /// Diario (CONTRATO_V01 §6): persistentDataPath/diario.csv, id anonimo da instalacao (GUID local), nada sai do aparelho sem o jogador
    /// tocar em Exportar diario (Exportar.cs). Colunas: utc,instalacao,versao,sessao,t_sessao,t_jogo,evento,a,b,c,onda
    /// (client/tools/diario_report.py le e calcula o K2 e o embarque medio).
    /// Flags (CONTRATO_V01 §7; todas, menos -autoplay, so com -testsession, que nao le nem grava save nem diario e usa a semente 1):
    ///   -autoplay N   sem render: o bot joga N ondas (Bom, Lento, Parado; retomada no meio da onda), loga AUTOPLAY OK|FAIL e sai 0/1.
    ///                 Usar com -batchmode -nographics (client/tools/verify.sh full).
    ///   -bot          o bot Bom joga na tela.        -speed N   multiplica o dt (0,1..20).
    ///   -tela nome    jogo | melhorias | mover | cofre | anuncio | pausa | album | equipe | tripulacao | patio | voo | passoN (tutorial, passo N).
    ///   -onda K       comeca na onda K (1 = a 1a).   -passageiros N   estresse: N passageiros por onda (60 = o K3).
    ///   -tutorial     liga o tutorial no -testsession (sem gravar; com -bot o ENTENDI se aperta sozinho).
    ///   -shot f.png [-shotdelay s] [-shotescala n] [-rola f]   espera s s reais (padrao 1), abre a -tela, fotografa em n x a janela (loga PISO: menor texto e menor alvo da tela; -rola 0 = fim da lista, 1 = topo) e sai 0.
    ///   -record pasta [-recordseg s]  grava s s (padrao 10) de quadros PNG a 30 qps na pasta e sai 0. Com -recordini r [-recordspeed v]:
    ///                 anda r s reais na -speed e so entao grava, na velocidade v (padrao 1): o pushback e a decolagem no GIF.
    ///                 Com -recordinc, os r s contam do aviso do incidente ou da pane.
    ///   -demo         contrata os 3 e, 1,5 s depois, manda a Ana do check-in para a loja (o -record da caminhada e da troca de uniforme).
    ///   -cenario nome postos = 4 postos no check-in e na seguranca, 1 e 2 deles com gente; oficina = abre tudo, cada um na sua sala (salas = o mesmo, para medir; frota = o mesmo, com o +1 veiculo); desgaste = o aviao do 1o voo pousa com 85%; hangar = o mesmo, e o manda ao
    ///                 hangar assim que encostar (o -record da manutencao).
    ///   -vista nome   perto | patio | geral: enquadramento das fotos e do -record (Mundo.Vista).
    ///   -perigo k     o perigo k em toda onda (PoePerigo), para fotografar o aviso.
    ///   -diag arq.csv [-diagseg s]  grava a pose dos avioes e veiculos (client/tools/diag_patio.py mede o escorregamento) e sai 0.
    ///   -perf s       K3 (GDD §5.13): depois de 5 s de aquecimento, mede s s de quadros, loga PERF p10=.. media=.. com as pessoas, os
    ///                 avioes e os veiculos na tela e sai 0. Usar com -passageiros 60.
    /// </summary>
    public sealed class Game : MonoBehaviour
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        const string Cabecalho = "utc,instalacao,versao,sessao,t_sessao,t_jogo,evento,a,b,c,onda";

        public Sim Sim { get; private set; }
        /// <summary>s de jogo do ultimo quadro (0 com o relogio parado): o Mundo anda os bonecos no tempo do jogo.</summary>
        public float DtJogo { get; private set; }
        Hud _hud;
        Bot _bot;
        float _speed = 1f;
        bool _headless, _test;
        string _save, _diario, _instalacao, _sessao;
        long _ultimoSave;
        float _ultimoFlush;
        readonly StringBuilder _buf = new StringBuilder();
        string _ofertaLogada;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        }

        void Awake()
        {
            // vSyncCount 0: senao a qualidade Medium (vSyncCount 1) prende o quadro ao vsync de 120 Hz (POCO F4) e o targetFrameRate nao vale
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            _headless = Arg("-autoplay") != null;
            if (_headless) return;   // o Start roda o autoplay sem montar camera

            _test = Arg("-testsession") != null;
            if (_test) Application.runInBackground = true;   // fotos e gravacao nao param quando a janela perde o foco
            _speed = _test ? Mathf.Clamp(ArgF("-speed", 1f), 0.1f, 20f) : 1f;
            string dir = Application.persistentDataPath;
            _save = Path.Combine(dir, "save.txt");
            _diario = Path.Combine(dir, "diario.csv");
            _instalacao = PlayerPrefs.GetString("ta_instalacao", "");
            if (_instalacao.Length == 0) { _instalacao = Guid.NewGuid().ToString("N"); PlayerPrefs.SetString("ta_instalacao", _instalacao); PlayerPrefs.Save(); }
            _sessao = Guid.NewGuid().ToString("N").Substring(0, 8);

            int status;
            if (_test)
            {
                var t = new Tuning();
                if (int.TryParse(Arg("-passageiros"), NumberStyles.Integer, Inv, out int n) && n > 0) t.PaxFixo = Mathf.Max(1, n / 2);
                if (int.TryParse(Arg("-perigo"), NumberStyles.Integer, Inv, out int perigo)) PoePerigo(t, perigo);
                Sim = new Sim(t, 1);
                if (int.TryParse(Arg("-onda"), NumberStyles.Integer, Inv, out int k) && k > 1) Sim.IrParaOnda(k - 1);
                if (Arg("-bot") != null) _bot = new Bot(BotProfile.Bom);
                status = 0;
            }
            else
            {
                string texto = null;
                try { if (File.Exists(_save)) texto = File.ReadAllText(_save); } catch (Exception e) { Debug.LogWarning("TA save: " + e.Message); }
                Sim = Sim.Load(texto);
                status = Sim.SaveRefused ? 2 : string.IsNullOrEmpty(texto) ? 0 : 1;
            }
            _ultimoSave = Sim.Steps;

            Camera cam = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Art.Bg;
            _hud = gameObject.AddComponent<Hud>();
            _hud.Build(this);
            _hud.TourGrava = !_test;
            _hud.TourLigado = !_test || Arg("-tutorial") != null;
            _hud.TourAuto = _bot != null;
            Diario("session_start");
            Diario("save_loaded", status);
            if (!_test) Retorno();
        }

        void Start()
        {
            if (_headless) { Autoplay(); return; }
            if (!_test) return;
            string shot = Arg("-shot"), rec = Arg("-record"), tela = Arg("-tela");
            if (!string.IsNullOrEmpty(shot)) StartCoroutine(Shot(shot, tela));
            else if (!string.IsNullOrEmpty(tela)) AbreTelaDev(tela);
            if (!string.IsNullOrEmpty(rec)) StartCoroutine(Record(rec));
            if (Arg("-demo") != null) StartCoroutine(Demo());
            if (!string.IsNullOrEmpty(Arg("-cenario"))) StartCoroutine(Cenario(Arg("-cenario")));
            if (!string.IsNullOrEmpty(Arg("-vista"))) _hud.M.Vista(Arg("-vista"));
            if (Arg("-perf") != null) StartCoroutine(Perf(ArgF("-perf", 20f)));
            if (!string.IsNullOrEmpty(Arg("-diag"))) StartCoroutine(DiagPatio(Arg("-diag"), ArgF("-diagseg", 60f)));
        }

        void Update()
        {
            if (_headless) return;
            float dt = Time.deltaTime * _speed;
            bool para = _hud.Bloqueia;
            if (!para) _bot?.Pensa(Sim, dt);
            DtJogo = para ? 0f : dt;
            Sim.Tick(DtJogo);
            foreach (Evento e in Sim.Events) { Trata(e); _hud.M.Evento(e); }
            string oferta = Sim.Anuncios.Oferta(Anuncio.Receita);
            if (oferta != null && oferta != _ofertaLogada) { _ofertaLogada = oferta; Diario("rewarded_ad_offered", (int)Anuncio.Receita); }
            if (Sim.Steps - _ultimoSave >= 5 * Sim.Hz) Grava();
            if (Time.unscaledTime - _ultimoFlush > 5f) Flush();
        }

        void OnApplicationPause(bool pausado)
        {
            if (_headless || Sim == null) return;
            if (pausado) { Grava(); Flush(); }
            else Retorno();
        }

        void OnApplicationQuit()
        {
            if (_headless || Sim == null) return;
            Grava();
            Flush();
        }

        // ------------------------------------------------------------------ intencoes da tela (o Hud chama)

        public void Tap(Estacao e) => Sim.Tap(e);
        public bool Move(int f, Estacao e) => Sim.Move(f, e);
        public void Buy(Estacao e, Melhoria m) => Sim.Buy(e, m);
        public void Hire() => Sim.Hire();
        public void Hire(Perfil p) => Sim.Hire(p);
        public void HireTripulante(Funcao f) => Sim.HireTripulante(f);
        public void Escala(int voo, int tripulante) => Sim.Escala(voo, tripulante);
        public void HirePatio() => Sim.HirePatio();
        public void BuyPatio(Servico s, Melhoria m) => Sim.BuyPatio(s, m);
        public void Checagem(int aviao) => Sim.Checagem(aviao);
        public void Hangar(int aviao) => Sim.Hangar(aviao);
        public bool Adianta() => Sim.Adianta();
        public bool Atrasa() => Sim.Atrasa();
        public bool Repara() => Sim.Repara();
        public bool Prioriza() => Sim.Prioriza();
        public bool ReparaAviao(int aviao) => Sim.ReparaAviao(aviao);

        /// <summary>Botao de rewarded: pede o id de transacao ao nucleo e abre o anuncio simulado. Nunca durante arrasto (o botao some no
        /// tutorial e nas telas modais, Hud).</summary>
        public void VerAnuncio(Anuncio a)
        {
            if (Anuncios.Veto(_hud.Arrastando, false, false) != null) return;
            string tx = Sim.Anuncios.Oferta(a);
            if (tx == null) return;
            if (a == Anuncio.Cofre) Diario("rewarded_ad_offered", (int)a);
            _hud.AbreAnuncio(a, tx);
        }

        /// <summary>O anuncio simulado chegou ao fim: so agora o nucleo paga (1x por id).</summary>
        public void AnuncioConcluido(Anuncio a, string tx)
        {
            Diario("rewarded_ad_completed", (int)a);
            Sim.Anuncios.Pagar(tx);
            Grava();
        }

        public string ExportarDiario()
        {
            Diario("diary_export");
            Flush();
            switch (Exportar.Diario(_test ? null : _diario, Textos.ExportarDiario))
            {
                case Exportar.Status.Vazio: return Textos.DiarioVazio;
                case Exportar.Status.Pasta: return Textos.DiarioPasta;
                case Exportar.Status.Erro: return Textos.DiarioErro;
                default: return "";
            }
        }

        // ------------------------------------------------------------------ eventos do nucleo -> diario e avisos

        void Trata(Evento e)
        {
            switch (e.Tipo)
            {
                case Ev.VooCriado: Diario("flight_spawned", e.A, e.B, e.C / Sim.Hz); break;
                case Ev.EmbarqueAberto: Diario("flight_boarding_started", e.A); break;
                case Ev.VooPartiu:
                    Diario("flight_departed", e.A, e.B, e.C);
                    Voo v = Sim.Voos[e.A % 2];
                    if (v != null && v.Id == e.A) _hud.Toast(Textos.Partiu(v));
                    break;
                case Ev.VooPerdido: Diario("flight_missed", e.A, e.B, e.C); break;
                case Ev.Fila: Diario("queue_length_sampled", e.A, e.B, e.C); break;
                case Ev.Melhorou: Diario("station_upgraded", e.A, e.B, e.C); break;
                case Ev.Movido: Diario("staff_moved", e.A, e.B, e.C); break;
                case Ev.Contratado: Diario("staff_hired", e.A, e.B, e.C); break;
                case Ev.Historia: Diario("story_delivered", e.A, e.B, e.C); _hud.Toast(Textos.HistoriaEntregue); break;
                case Ev.Offline: Diario("offline_claim", e.A, e.B); break;
                case Ev.Recompensa: Diario("reward_granted", e.A, e.B); break;
                case Ev.Bonus: _hud.Toast(Textos.Bonus(e.B)); break;
                case Ev.Marco: _hud.Toast(Textos.Marco(e.A + 1)); break;
                // Fase C e C2 (GDD §15.1, CONTRATO_FASE_C §5)
                case Ev.TripulanteContratado: Diario("crew_hired", e.A, e.B, e.C); break;
                case Ev.Escalado: Diario("crew_assigned", e.A, e.B, e.C); break;
                case Ev.SemTripulacao: Diario("crew_missing", e.A, e.B); break;
                case Ev.Descanso: Diario("crew_rest_started", e.A, e.B); break;
                case Ev.Pousou: Diario("aircraft_landed", e.A, e.B); break;
                case Ev.ServicoFeito: Diario("ground_service_done", e.A, e.B); break;
                case Ev.TurnaroundFeito: Diario("turnaround_done", e.A, e.B, e.C); break;
                case Ev.Decolou: Diario("flight_takeoff", e.A, e.B, e.C); break;
                case Ev.PatioContratado: Diario("ground_staff_hired", e.A, e.B); break;
                case Ev.PatioMelhorou: Diario("ground_vehicle_upgraded", e.A, e.B, e.C); break;
                case Ev.ManutencaoAviso: Diario("maintenance_warning", e.A, e.B); _hud.Toast(Textos.ManutencaoAviso(e.B)); break;
                case Ev.ManutencaoPendente: Diario("maintenance_due", e.A, e.B); _hud.Toast(Textos.ManutencaoPendente); break;
                case Ev.ChecagemIniciada: Diario("maintenance_check_started", e.A, e.B, e.C); break;
                case Ev.ChecagemFeita: Diario("maintenance_check_done", e.A, e.B); break;
                case Ev.HangarIniciado: Diario("hangar_started", e.A, e.B); break;
                case Ev.HangarFeito: Diario("hangar_done", e.A); _hud.Toast(Textos.HangarFeito); break;
                case Ev.EstacaoAberta: Diario("station_unlocked", e.A); _hud.Toast(Textos.EstacaoAberta(e.A)); break;
                case Ev.PousoSeguro: Diario("safe_landing", e.A); _hud.Toast(Textos.PousoSeguro); break;
            }
        }

        /// <summary>Uma linha do diario (bufferizada; Flush a cada 5 s, na pausa e ao sair). -testsession nao escreve.</summary>
        public void Diario(string evento, long a = 0, long b = 0, long c = 0)
        {
            if (Sim == null) return;
            if (_test)
            {
                // -testsession nao grava diario; o tutorial vai para o log (prova dos passos jogaveis com -tutorial -bot)
                if (evento.StartsWith("tour", StringComparison.Ordinal) || evento.StartsWith("tutorial", StringComparison.Ordinal))
                    Debug.Log(string.Format(Inv, "TOUR {0} {1} {2} t={3:0.0}", evento, a, b, Sim.Time));
                return;
            }
            _buf.Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", Inv)).Append(',').Append(_instalacao).Append(',').Append(Application.version)
                .Append(',').Append(_sessao).Append(',').Append(Time.realtimeSinceStartup.ToString("0.0", Inv)).Append(',').Append(Sim.Time.ToString("0.0", Inv))
                .Append(',').Append(evento).Append(',').Append(a.ToString(Inv)).Append(',').Append(b.ToString(Inv)).Append(',').Append(c.ToString(Inv))
                .Append(',').Append(Math.Max(0, Sim.Onda) + 1).Append('\n');
        }

        void Flush()
        {
            _ultimoFlush = Time.unscaledTime;
            if (_buf.Length == 0) return;
            try
            {
                bool novo = !File.Exists(_diario) || new FileInfo(_diario).Length == 0;
                File.AppendAllText(_diario, (novo ? Cabecalho + "\n" : "") + _buf);
                _buf.Clear();
            }
            catch (Exception) { }   // ponytail: diario e best-effort; disco cheio nunca derruba o jogo
        }

        // ------------------------------------------------------------------ save e cofre

        static long Agora => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Grava o save (escrita atomica: .tmp e depois troca). Save recusado ou -testsession: nunca grava.</summary>
        void Grava()
        {
            _ultimoSave = Sim.Steps;
            if (_test || Sim.SaveRefused) return;
            string texto = Sim.Save(Application.version, Agora);
            if (texto == null) return;
            if (Sim.ClockWentBack > 0) Diario("clock_back", Sim.ClockWentBack);
            try
            {
                string tmp = _save + ".tmp";
                File.WriteAllText(tmp, texto);
                if (File.Exists(_save)) File.Replace(tmp, _save, null);
                else File.Move(tmp, _save);
            }
            catch (Exception e) { Debug.LogWarning("TA save: " + e.Message); }
        }

        /// <summary>Ao abrir e ao voltar da pausa: anda o dia local e paga o cofre pelo tempo fora (id = o SavedAt). Nenhum voo andou.</summary>
        void Retorno()
        {
            long agora = Agora;
            Sim.Agora(agora, (long)TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalSeconds);
            if (Sim.SavedAt <= 0 || Sim.SaveRefused) return;
            long fora = agora - Sim.SavedAt;
            long cash = Sim.ApplyOffline(fora, Sim.SavedAt);
            if (cash > 0 && fora >= 60) _hud.AbreCofre(cash, Math.Min(fora, Sim.T.OfflineCap));
        }

        // ------------------------------------------------------------------ dev: autoplay, foto, gravacao

        /// <summary>-autoplay N (portao do build, sem render): o Bot.Autoplay do nucleo (Bom, Lento e Parado com a semente 1, e o Bom fechando
        /// e reabrindo no meio de cada onda). Loga AUTOPLAY OK|FAIL com o embarque medio, o ganho de mover e os marcos; sai 0/1.</summary>
        void Autoplay()
        {
            int n = int.TryParse(Arg("-autoplay"), NumberStyles.Integer, Inv, out int v) && v > 0 ? v : 8;
            bool ok;
            string linha;
            try { linha = Bot.Autoplay(n, out ok); }
            catch (Exception ex) { ok = false; linha = "AUTOPLAY FAIL excecao"; Debug.LogError("AUTOPLAY excecao: " + ex); }
            Debug.Log(linha);
            Application.Quit(ok ? 0 : 1);
        }

        void AbreTelaDev(string tela)
        {
            if (tela.StartsWith("passo", StringComparison.Ordinal) && int.TryParse(tela.Substring(5), out int p)) { _hud.MostraPasso(p); return; }
            if (tela == "cofre")
            {
                Sim.RateEma = Math.Max(Sim.RateEma, 2.0);
                long cash = Sim.ApplyOffline(5400, Sim.LastClaim + 1);
                _hud.AbreCofre(cash, 5400);
                return;
            }
            _hud.AbreTela(tela);
        }

        /// <summary>-perigo k (so no -testsession, para as fotos): o TipoIncidente k em toda onda; 8, 9 e 10 = pane, rejeicao e retorno em todo voo no aviso de desgaste.</summary>
        static void PoePerigo(Tuning t, int k)
        {
            t.ClimaMarco = 0; t.IncidenteFolga = 0; t.IncidenteTeto = 1f;
            if (k >= 8)
            {
                t.RegraClima = t.RegraIncidentes = false; t.FalhaAviaoChance = 1f; t.DesgasteAviso = 0;
                t.FalhaAviaoPesos = new int[3]; t.FalhaAviaoPesos[k - 8] = 1;
                return;
            }
            Aeroporto a = t.Aeroportos[0].Copia();
            int col = Array.FindIndex(Tabelas.Climas, c => c.Tipo == (TipoIncidente)k);
            if (col >= 0)
            {
                a.Clima = new float[4][];
                for (int e = 0; e < 4; e++) { a.Clima[e] = new float[Tabelas.Climas.Length]; a.Clima[e][col] = 1f; }
            }
            else { t.RegraClima = false; a.Incidentes = 1f; a.Pesos = new int[4]; a.Pesos[k] = 1; }
            t.Aeroportos = (Aeroporto[])t.Aeroportos.Clone();
            t.Aeroportos[0] = a;
        }

        IEnumerator Shot(string path, string tela)
        {
            yield return new WaitForSecondsRealtime(ArgF("-shotdelay", 1f));
            if (!string.IsNullOrEmpty(tela)) AbreTelaDev(tela);
            yield return null;
            yield return null;
            if (!string.IsNullOrEmpty(Arg("-rola")))
                foreach (UnityEngine.UI.ScrollRect r in FindObjectsByType<UnityEngine.UI.ScrollRect>(FindObjectsSortMode.None)) r.verticalNormalizedPosition = ArgF("-rola", 1f);
            yield return new WaitForEndOfFrame();
            Debug.Log(string.Format(Inv, "SHOT {0} t={1:0.0} onda={2} {3}x{4}", path, Sim.Time, Sim.Onda + 1, Screen.width, Screen.height));
            Debug.Log(_hud.PisoTela());
            ScreenCapture.CaptureScreenshot(path, Mathf.Max(1, Mathf.RoundToInt(ArgF("-shotescala", 1f))));
            yield return null;
            yield return null;
            Application.Quit(0);
        }

        IEnumerator Demo()
        {
            Sim.Cash += 570;
            while (Sim.Hire()) { }
            yield return new WaitForSeconds(1.5f);
            Sim.Move(0, Estacao.Loja);   // do check-in (azul) para a loja (verde): a troca de colete e a loja abrindo
        }

        /// <summary>-cenario (so no -testsession): deixa na mesa o que as fotos e o -record da manutencao e das salas novas precisam mostrar.</summary>
        IEnumerator Cenario(string nome)
        {
            Sim.Cash += 5000;
            if (nome == "frota") for (int v = 0; v < Sim.PatioCap.Length; v++) Sim.PatioCap[v] = 1;
            if (nome == "oficina" || nome == "salas" || nome == "frota")
            {
                Sim.TotalEmbarcados = Math.Max(Sim.TotalEmbarcados, Sim.T.Marcos[1]);
                while (Sim.Hire()) { }
                for (int p = Sim.MaxFuncionarios; p < Sim.MaxEquipe; p++) Sim.Hire((Perfil)p);
                yield return null;
                for (int i = 0; i < Sim.Staff.Count; i++) Sim.Move(i, Funcionario.Casa(Sim.Staff[i].Perfil));
                yield break;
            }
            if (nome == "postos")
            {
                foreach (Estacao e in new[] { Estacao.CheckIn, Estacao.Seguranca })
                    while (Sim.Buy(e, Melhoria.Capacidade)) { }
                while (Sim.Hire()) { }
                Sim.Move(2, Estacao.Seguranca);
                yield break;
            }
            Sim.Avioes[0].Desgaste = 85;
            if (nome != "hangar") yield break;
            while (!Sim.Avioes[0].NoPatio) yield return null;
            yield return new WaitForSeconds(ArgF("-hangarespera", 6f));
            Sim.Hangar(0);
        }

        /// <summary>K3: p10 do FPS (o quadro mais lento dos 90% mais rapidos) e a media, em tempo real, sem o aquecimento.</summary>
        IEnumerator Perf(float seg)
        {
            yield return new WaitForSecondsRealtime(5f);
            var dts = new List<float>();
            float fim = Time.realtimeSinceStartup + seg;
            while (Time.realtimeSinceStartup < fim) { yield return null; dts.Add(Time.unscaledDeltaTime); }
            dts.Sort();
            float p90 = dts[Mathf.Clamp((int)(dts.Count * 0.9f), 0, dts.Count - 1)], soma = 0f;
            foreach (float d in dts) soma += d;
            Debug.Log(string.Format(Inv, "PERF p10={0:0.0} media={1:0.0} quadros={2} {3} {4}x{5}", 1f / p90, dts.Count / soma, dts.Count, _hud.M.Contagem(), Screen.width, Screen.height));
            Application.Quit(0);
        }

        IEnumerator DiagPatio(string arq, float seg)
        {
            _hud.M.Diag(arq);
            yield return new WaitForSecondsRealtime(seg);
            _hud.M.DiagFim();
            Application.Quit(0);
        }

        IEnumerator Record(string pasta)
        {
            Directory.CreateDirectory(pasta);
            if (Arg("-recordinc") != null) while (Sim.Inc == null && !Sim.Avioes[0].Pane && !Sim.Avioes[1].Pane) yield return null;   // o -cenario de incidente: conta do aviso
            float ini = ArgF("-recordini", 0f);
            if (ini > 0f) { yield return new WaitForSecondsRealtime(ini); _speed = ArgF("-recordspeed", 1f); }
            Time.captureFramerate = 30;
            int total = Mathf.RoundToInt(ArgF("-recordseg", 10f) * 30f);
            for (int i = 0; i < total; i++)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(pasta, "f_" + i.ToString("00000", Inv) + ".png"));
            }
            Application.Quit(0);
        }

        static float ArgF(string name, float def) => float.TryParse(Arg(name), NumberStyles.Float, Inv, out float v) ? v : def;

        /// <summary>Valor da flag (Environment.GetCommandLineArgs): null = ausente; "" = presente sem valor.</summary>
        static string Arg(string name)
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, name);
            if (i < 0) return null;
            return i + 1 < a.Length && !a[i + 1].StartsWith("-", StringComparison.Ordinal) ? a[i + 1] : "";
        }
    }
}
