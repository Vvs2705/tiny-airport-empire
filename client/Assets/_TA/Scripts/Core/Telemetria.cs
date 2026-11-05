using System;
using System.Collections.Generic;

namespace TA.Core
{
    /// <summary>
    /// Telemetria sem dado pessoal (GDD §15.1, D-11; docs/SEGURANCA_PRIVACIDADE.md §4). Hoje os eventos so vao para o diario local
    /// (Game.Diario), que so sai do aparelho pelo Exportar diario; o SDK de D-11, quando entrar, passa pelo mesmo portao.
    /// Regras: o evento tem de estar em Eventos (nome fixo, sem texto do jogador) e leva so 3 inteiros (a, b, c). Nada de nome, e-mail,
    /// texto livre, token de compra, modelo ou ID do aparelho, local ou ID de publicidade: o checa_repo (privacidade) derruba essas APIs no
    /// codigo do jogo e confere que todo Diario("...") usa um nome daqui e que todo nome do GDD §15.1 esta aqui.
    /// </summary>
    public static class Telemetria
    {
        /// <summary>Os nomes do GDD §15.1 (mesmo os que ainda nao disparam) e os do diario de playtest que o §15.1 nao lista.</summary>
        public static readonly HashSet<string> Eventos = new HashSet<string>(StringComparer.Ordinal)
        {
            // GDD §15.1
            "tutorial_started", "tutorial_completed", "tutorial_step_failed", "tour_step",
            "flight_spawned", "flight_boarding_started", "flight_departed", "flight_missed", "flight_takeoff",
            "aircraft_landed", "ground_service_done", "turnaround_done", "ground_staff_hired", "ground_vehicle_upgraded",
            "crew_hired", "crew_assigned", "crew_missing", "crew_rest_started",
            "maintenance_warning", "maintenance_due", "maintenance_check_started", "maintenance_check_done", "hangar_started", "hangar_done",
            "station_unlocked", "queue_length_sampled", "station_upgraded", "staff_moved", "staff_hired", "story_delivered",
            "route_unlocked", "airport_unlocked", "campaign_objective_completed", "daily_mission_completed", "weekly_mission_completed",
            "season_xp_gained", "season_reward_claimed", "season_pass_purchased",
            "weather_forecast", "gate_delayed", "deicing_done", "aircraft_fault", "aircraft_fault_repaired", "takeoff_rejected",
            "return_requested", "safe_landing", "pax_rebooked", "offline_claim",
            "rewarded_ad_offered", "rewarded_ad_started", "rewarded_ad_completed", "rewarded_ad_failed", "reward_granted",
            "purchase_started", "purchase_pending", "purchase_succeeded_server_verified", "purchase_failed", "purchase_restored",
            "purchase_refunded", "gems_earned", "gems_spent", "save_loaded", "save_migration_failed", "cloud_save_conflict",
            "app_crash", "performance_sample", "network_error",
            // do diario, fora do §15.1
            "session_start", "diary_export", "device_class", "staff_fired", "achievement_unlocked", "clock_back",
            "supply_ordered", "supply_delivered", "supply_shortage", "supply_upgraded",
            "route_scheduled", "airport_trip", "airport_arrived", "policy_changed", "policy_action",
        };

        public static bool Valido(string evento) => evento != null && Eventos.Contains(evento);

        /// <summary>device_class a: memoria do aparelho em GB inteiros, com teto (aparelho de entrada x forte, sem impressao digital).</summary>
        public static int MemoriaGb(int megabytes) => Math.Max(0, Math.Min(16, (megabytes + 512) / 1024));

        /// <summary>device_class b: nucleos do processador, com teto.</summary>
        public static int Nucleos(int n) => Math.Max(0, Math.Min(16, n));
    }

    /// <summary>Uma amostra do desempenho: FPS x10 (inteiros do diario) de uma janela de quadros reais.</summary>
    public readonly struct AmostraFps
    {
        public readonly int P10x10, Mediax10, Quadros;
        public AmostraFps(int p10x10, int mediax10, int quadros) { P10x10 = p10x10; Mediax10 = mediax10; Quadros = quadros; }
        public float P10 => P10x10 / 10f;
        public float Media => Mediax10 / 10f;
    }

    /// <summary>
    /// K3 em campo (GDD §5.13, §13): a cada Janela s reais de jogo aberto, o p10 do FPS (o quadro mais lento dos 90% mais rapidos, a mesma
    /// conta do -perf) e a media. O Game da um Quadro(dt) por Update com o Time.unscaledDeltaTime e grava performance_sample no diario.
    /// Os Aquecimento s depois de abrir ou voltar do segundo plano nao contam (carga e GC do inicio). Um quadro acima de Buraco s e o app
    /// parado (segundo plano, carregando, depurador): a janela recomeca do zero, sem contar o buraco como quadro lento.
    /// </summary>
    public sealed class Desempenho
    {
        public const float Janela = 60f, Aquecimento = 5f, Buraco = 2f;

        readonly List<float> _dts = new List<float>(4096);
        float _soma, _aquecido;

        public Desempenho() { Reinicia(); }

        /// <summary>Volta ao aquecimento e descarta a janela (abrir, voltar do segundo plano).</summary>
        public void Reinicia() { _dts.Clear(); _soma = 0f; _aquecido = 0f; }

        /// <summary>Um quadro de dt s reais. True quando a janela fechou: a amostra sai e a proxima janela comeca.</summary>
        public bool Quadro(float dt, out AmostraFps amostra)
        {
            amostra = default;
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0f) return false;
            if (dt > Buraco) { Reinicia(); return false; }
            if (_aquecido < Aquecimento) { _aquecido += dt; return false; }
            _dts.Add(dt);
            _soma += dt;
            if (_soma < Janela) return false;
            amostra = Calcula(_dts);
            _dts.Clear();
            _soma = 0f;
            return true;
        }

        /// <summary>p10 e media de uma lista de dt (a lista e ordenada no lugar). Vazia: zeros.</summary>
        public static AmostraFps Calcula(List<float> dts)
        {
            if (dts.Count == 0) return default;
            double soma = 0;
            foreach (float d in dts) soma += d;
            dts.Sort();
            // so inteiro: (int)(n * 0.9f) da 89 no Mono do Unity (conta em double) e 90 no dotnet
            float p90 = dts[Math.Min(dts.Count - 1, dts.Count * 9 / 10)];
            return new AmostraFps(X10(1.0 / p90), X10(dts.Count / soma), dts.Count);
        }

        static int X10(double fps) => (int)Math.Round(Math.Min(fps, 999.0) * 10.0, MidpointRounding.AwayFromZero);
    }
}
