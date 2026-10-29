using System;
using System.Globalization;
using System.Text;

// API do nucleo para a View (TA.Core, C# puro, deterministico, passo fixo de 1/30 s; nunca le o relogio do aparelho).
// Fase A (docs/CONTRATO_V01.md §9): esqueleto vazio. So o relogio de jogo, o Cash e o save; quadro, filas e equipe chegam na Fase B.
//
//   Abrir:      var sim = Sim.Load(texto) ("" ou null = jogo novo). sim.SaveRefused => save de schema futuro ou corrompido: avise e
//               NUNCA grave por cima (sim.Save devolve null).
//   Jogar:      a cada quadro sim.Tick(dt); o nucleo avanca em passos fixos de Sim.Step (dt invalido ou <= 0 nao anda; dt por quadro
//               limitado a Sim.MaxFrame). Ler sim.Steps, sim.Time (s de jogo), sim.Cash.
//   Gravar:     texto = sim.Save(versaoDoJogo, nowUnix): payload chave=valor dentro do SaveEnvelope (SHA-256 + schema).
//
// Nomes acima sao estaveis (so crescem). Mudou a API: atualize este cabecalho e o docs/MAPA_CODIGO.md no mesmo PR.

namespace TA.Core
{
    public sealed class Sim
    {
        /// <summary>Passo fixo do nucleo (s): bot, testes e View andam no mesmo ritmo.</summary>
        public const float Step = 1f / 30f;
        /// <summary>Teto do dt de um quadro (s): uma travada longa nao vira um salto de minutos no relogio do quadro de partidas.</summary>
        public const float MaxFrame = 0.25f;
        /// <summary>Versao do formato do save (SaveEnvelope). Sobe a cada mudanca de formato; save de schema maior e' recusado.</summary>
        public const int Schema = 1;

        /// <summary>Passos fixos ja simulados (o relogio de jogo; vai no save).</summary>
        public long Steps;
        /// <summary>A unica moeda da v0.1 (GDD §6.1). Nunca negativa.</summary>
        public long Cash;
        /// <summary>O save lido era de schema futuro ou corrompido: este Sim e' novo e Save devolve null, para nao apagar o save do jogador.</summary>
        public bool SaveRefused { get; private set; }

        public float Time => Steps * Step;

        float _acc;

        public void Tick(float dt)
        {
            if (!(dt > 0f)) return;   // tambem pega NaN: com Math.Max(NaN, 0) o acumulador virava NaN e o relogio parava para sempre
            _acc += Math.Min(dt, MaxFrame);
            while (_acc >= Step - 1e-6f)
            {
                _acc -= Step;
                Steps++;
            }
        }

        // ------------------------------------------------------------------ save

        public string Save(string content, long nowUnix)
        {
            if (SaveRefused) return null;
            var sb = new StringBuilder();
            sb.Append("t=").Append(Steps.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("cash=").Append(Cash.ToString(CultureInfo.InvariantCulture)).Append('\n');
            return SaveEnvelope.Wrap(sb.ToString(), content, nowUnix);
        }

        /// <summary>Missing = jogo novo; Ok = carrega (chave desconhecida e' ignorada); Corrupt, Legacy (sem cabecalho: nunca houve save
        /// assim neste jogo) e NewerSchema = jogo novo com SaveRefused.</summary>
        public static Sim Load(string text)
        {
            SaveEnvelope.Result r = SaveEnvelope.Unwrap(text);
            if (r.Status == SaveEnvelope.Status.Missing) return new Sim();
            if (r.Status != SaveEnvelope.Status.Ok) return new Sim { SaveRefused = true };
            var s = new Sim();
            foreach (string line in r.Payload.Split('\n'))
            {
                int eq = line.IndexOf('=');
                if (eq <= 0 || !long.TryParse(line.Substring(eq + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out long v)) continue;
                switch (line.Substring(0, eq))
                {
                    case "t": s.Steps = Math.Max(0, v); break;
                    case "cash": s.Cash = Math.Max(0, v); break;   // GDD §6.2: saldo nunca negativo, nem de save editado
                }
            }
            return s;
        }
    }
}
