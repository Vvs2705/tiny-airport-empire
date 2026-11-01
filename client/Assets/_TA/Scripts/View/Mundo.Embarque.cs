using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Embarque e desembarque na tela, so apresentacao (o nucleo nao sabe da ponte). Quem entra ou sai de um aviao passa por um lugar so,
    /// Trilha + NoTrilho: passageiro, tripulacao, subindo ou descendo.
    ///  - Estande G: rampa da porta norte do portao ate a boca do tunel (a ponta em arco da malha, com a ponte em leve subida) e tunel ate o
    ///    lado da fuselagem. A pessoa ganha altura na rampa, entra na boca e fica escondida ate a porta do aviao.
    ///  - Estande R: cruza as ruas por um ponto so, segue a calcada ate o caminhao-escada e sobe degraus, plataforma, passarela e a porta aberta
    ///    do jato. Sem o caminhao estacionado, ninguem entra na calcada das faixas (Segura): espera no comeco dela, fora do caminho dos veiculos.
    ///  - O sprite e um quad de frente para a camera, inclinado 45 graus: parado no chao da rampa, a cabeca dele ja esta dentro da boca e
    ///    corta em diagonal. Por isso cada ponto do trilho traz, alem da altura, quanto o quad vem para a frente (`Frente`: a camera e
    ///    ortografica, a imagem nao muda de lugar, so a profundidade) e o alfa: a pessoa nunca fura o telhado nem o piso e some no escuro da boca.
    ///  - A tela segura o aviao ate o ultimo do trilho entrar (GenteNoTrilho, em Decide e Precisa): sem pessoa subindo em escada que ja saiu.
    /// </summary>
    public sealed partial class Mundo
    {
        /// <summary>Ponto do trilho: chao (P), altura dos pes, recuo do quad para a camera e alfa. Entre 2 pontos, linear.</summary>
        readonly struct No
        {
            public readonly Vector2 P;
            public readonly float Alt, Frente, Alfa;
            public No(float x, float z, float alt, float frente, float alfa) { P = new Vector2(x, z); Alt = alt; Frente = frente; Alfa = alfa; }
        }

        /// <summary>Do chao ate a porta do aviao, por estande. G: pe da rampa, boca, porta. R: comeco da calcada, pe da escada, 1o degrau, topo, plataforma,
        /// passarela, porta. A escada para com o bico antes do motor do jato, dai a porta logo atras da cabine (frente 2,95 m; o casco la tem 0,84 m de meia largura).</summary>
        static readonly No[][] Trilho =
        {
            new[] { new No(10f, 8.45f, 0f, 0.15f, 1f), new No(10f, 9.65f, 0.78f, 0.7f, 1f), new No(10f, 9.95f, 0.98f, 1.1f, 0f), new No(10f, 12.75f, 1.24f, 0f, 0f) },
            new[] { new No(12.6f, CalcadaZ, 0f, 0f, 1f), new No(20.8f, CalcadaZ, 0f, 0.15f, 1f), new No(20.8f, 12.85f, 0.55f, 0.2f, 1f), new No(20.8f, 14.05f, 1.4f, 0.2f, 1f), new No(20.8f, 14.35f, 1.4f, 0.5f, 1f), new No(21.25f, 14.35f, 1.4f, 1.1f, 1f), new No(21.65f, 14.35f, 1.4f, 1.3f, 0f) },
        };
        /// <summary>Chegadas: quem desceu do aviao segue pelo passeio e some no arco de vidro do terminal, com o quad a frente para a fachada nao cortar a cabeca.</summary>
        static readonly No[] Terminal = { new No(4f, 8.2f, 0f, 0f, 1f), new No(4f, 9.9f, 0f, 0f, 1f), new No(4f, 10.5f, 0f, 1f, 1f), new No(4f, 10.9f, 0f, 1.2f, 0f) };
        const float PasseioZ = 8.2f;   // ao longo da parede norte das salas, por tras do pe da rampa
        static readonly Vector2 Chegadas = new Vector2(4f, 10.9f);

        const float PonteGraus = 5f;
        public static readonly Vector3 PonteEm = new Vector3(9.95f, 0f, 11.55f), PonteEscala = new Vector3(1.15f, 1.1f, 0.9f);
        static readonly Color PisoRampa = Art.Hex(0x9AA6BC), Corrimao = Art.Hex(0xF2C14E), Escuro = Art.Hex(0x1E2533), Metal = Art.Hex(0x6E7A90), PisoPasseio = Art.Hex(0xB8B2A6);
        /// <summary>Passageiros que descem por aviao na tela (o nucleo nao conta quem chega) e o intervalo em s de jogo.</summary>
        const int Descem = 8;
        const float DesceCada = 1.1f, TripCada = 0.7f;

        GameObject _passarela, _portaJato;
        bool _escada;
        readonly int[] _faltaDescer = new int[2];
        readonly float[] _proxDescer = new float[2];
        readonly bool[] _descia = new bool[2];

        // ------------------------------------------------------------------ construcao

        void Ponte3D()
        {
            // girada 180 graus: a ponta em arco fica de frente para a camera (a outra, fechada, encosta na fuselagem) e a ponte sobe para o aviao
            Transform t = Peca("ponte_embarque", PonteEm, 180f);
            if (t != null)
            {
                t.localScale = PonteEscala;
                t.localRotation = Quaternion.Euler(-PonteGraus, 0f, 0f) * t.localRotation;
                Bloco("Boca", PrimitiveType.Cube, new Vector3(0f, 1.42f, 1.69f), new Vector3(0.5f, 0.76f, 0.04f), Escuro, t);
                Transform sombra = SombraPlana(t);
                if (sombra != null)
                {
                    sombra.localScale = new Vector3(PonteEscala.x, 0.002f, PonteEscala.z);
                    sombra.SetPositionAndRotation(PonteEm + new Vector3(0.35f, 0.015f, 0.3f), Quaternion.Euler(0f, 180f, 0f));
                }
            }
            else
            {
                Bloco("Ponte", PrimitiveType.Cube, new Vector3(9.95f, 1.6f, 11.45f), new Vector3(0.9f, 1.1f, 3.05f), PisoRampa).transform.localRotation = Quaternion.Euler(-PonteGraus, 0f, 0f);
                Bloco("Boca", PrimitiveType.Cube, new Vector3(9.95f, 1.45f, 9.92f), new Vector3(0.5f, 0.76f, 0.04f), Escuro);
            }

            No pe = Trilho[0][0], boca = Trilho[0][2];
            float parede = Salas[3].Out.y - Corredor * 0.5f;
            Bloco("Soleira", PrimitiveType.Cube, new Vector3(10f, 0.005f, (parede + pe.P.y) * 0.5f), new Vector3(Corredor, 0.03f, pe.P.y - parede), PisoRampa);
            Viga("Rampa", V3(pe.P, -0.04f), V3(boca.P, boca.Alt - 0.04f), 0.86f, 0.08f, PisoRampa);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var d = new Vector3(lado * 0.43f, 0f, 0f);
                Viga("Corrimao", V3(pe.P, 0.2f) + d, V3(boca.P, boca.Alt + 0.2f) + d, 0.04f, 0.04f, Corrimao);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 c = Vector3.Lerp(V3(pe.P, 0f), V3(boca.P, boca.Alt), (i + 0.3f) / 3.3f) + d;
                    Bloco("Balaustre", PrimitiveType.Cube, c + Vector3.up * 0.1f, new Vector3(0.03f, 0.2f, 0.03f), Metal);
                }
                Bloco("Pilar", PrimitiveType.Cylinder, new Vector3(10f + d.x, boca.Alt * 0.5f, boca.P.y - 0.1f), new Vector3(0.1f, boca.Alt * 0.5f, 0.1f), Metal);
            }

            // o passeio de quem anda a pe do lado de fora: a parede norte das salas e o gramado ate o terminal
            Chao("PasseioNorte", new Vector3(6.85f, -0.04f, PasseioZ), new Vector2(6.3f, 0.6f), PisoPasseio);
            Chao("PasseioTerminal", new Vector3(4f, -0.04f, 9.6f), new Vector2(0.7f, 2.8f), PisoPasseio);

            // no remoto: passarela da plataforma do caminhao ate a fuselagem e a porta aberta do jato (aparecem com o caminhao parado)
            _passarela = Bloco("Passarela", PrimitiveType.Cube, new Vector3(21.42f, 1.37f, 14.35f), new Vector3(0.5f, 0.05f, 0.66f), Metal).gameObject;
            _portaJato = Bloco("PortaJato", PrimitiveType.Cube, new Vector3(21.66f, 1.82f, 14.35f), new Vector3(0.06f, 0.86f, 0.5f), Escuro).gameObject;
            _passarela.SetActive(false);
            _portaJato.SetActive(false);
        }

        /// <summary>Bloco de `a` a `b`, com a medida `larg` x `alt` de secao.</summary>
        void Viga(string nome, Vector3 a, Vector3 b, float larg, float alt, Color cor) =>
            Bloco(nome, PrimitiveType.Cube, (a + b) * 0.5f, new Vector3(larg, alt, Vector3.Distance(a, b)), cor).transform.localRotation = Quaternion.LookRotation(b - a);

        // ------------------------------------------------------------------ caminho

        /// <summary>Altura dos pes no ponto `p` do trilho do estande `k` (0 fora dele).</summary>
        public static float AlturaEmbarque(int k, Vector2 p) { Le(Trilho[k], p, out float alt, out _, out _); return alt; }

        /// <summary>Alfa do boneco no ponto `p` do trilho do estande `k` (0 = escondido no tunel ou dentro do aviao).</summary>
        public static float AlfaEmbarque(int k, Vector2 p) { Le(Trilho[k], p, out _, out _, out float alfa); return alfa; }

        /// <summary>Caminho do lado de fora da parede norte ate a porta do aviao `k`. O mesmo nos 2 sentidos: quem desce inverte a lista.</summary>
        public static void RotaAviao(List<Vector2> r, ref Vector2 p, int k)
        {
            float porta = k == 0 ? Salas[3].Out.x : RemotoPorta.x;
            if (p.x < porta - 0.1f) Eixos(r, ref p, new Vector2(porta, PasseioZ), true);
            if (k == 1) Eixos(r, ref p, Trilho[1][0].P, false);   // ao longo do corredor e depois reto para o norte, cruzando as ruas
            foreach (No n in Trilho[k]) { r.Add(n.P); p = n.P; }
        }

        /// <summary>O passageiro do portao vai ao aviao: sai pela porta do estande e segue a mesma rota da tripulacao.</summary>
        void Embarca(Boneco b, ref Vector2 p, int k)
        {
            PelaPorta(b.Caminho, ref p, k == 0 ? Salas[3].Out : RemotoPorta);
            RotaAviao(b.Caminho, ref p, k);
            b.Estande = k;
        }

        /// <summary>Quem nao veio do portao, entre `casa` e o aviao `k`, em qualquer sentido (a tripulacao e os passageiros que descem).</summary>
        void Trilha(Boneco b, Vector2 casa, int k, bool desce)
        {
            var r = new List<Vector2> { casa };
            Vector2 p = casa;
            RotaAviao(r, ref p, k);
            if (desce) r.Reverse();
            b.Pos = r[0];
            b.Estande = k;
            b.Caminho.Clear();
            for (int i = 1; i < r.Count; i++) b.Caminho.Add(r[i]);
        }

        /// <summary>Boneco de passagem, que volta ao pool quando o caminho acaba; aparece depois de `atraso` s de jogo (nao nascem um em cima do outro).</summary>
        Boneco NovoAndante(string ator, Color tinta, float atraso)
        {
            Boneco b;
            if (_livres.Count > 0) { b = _livres.Pop(); Veste(b, ator); } else b = NovoBoneco(ator);
            b.Tinta = tinta;
            b.Coracao.enabled = b.Humor.enabled = false;
            b.Saindo = false; b.Zona = -1; b.VelAtual = 0f;
            b.T.gameObject.SetActive(atraso <= 0f);
            _andantes.Add(new Andante { B = b, Atraso = atraso });
            return b;
        }

        // ------------------------------------------------------------------ quadro a quadro

        /// <summary>Le o trilho no ponto `p`: falso = fora dele, no chao.</summary>
        static bool Le(No[] t, Vector2 p, out float alt, out float frente, out float alfa)
        {
            for (int i = 1; i < t.Length; i++)
            {
                No a = t[i - 1], b = t[i];
                Vector2 d = b.P - a.P;
                float f = Mathf.Clamp01(Vector2.Dot(p - a.P, d) / d.sqrMagnitude);
                if ((a.P + d * f - p).sqrMagnitude > 1e-3f) continue;
                alt = Mathf.Lerp(a.Alt, b.Alt, f); frente = Mathf.Lerp(a.Frente, b.Frente, f); alfa = Mathf.Lerp(a.Alfa, b.Alfa, f);
                return true;
            }
            alt = frente = 0f; alfa = 1f;
            return false;
        }

        /// <summary>Ponto 3D do quad de quem esta no trilho e o alfa dele.</summary>
        Vector3 NoTrilho(Boneco b, out float alfa)
        {
            if (!Le(Trilho[b.Estande], b.Pos, out float alt, out float frente, out alfa)) Le(Terminal, b.Pos, out alt, out frente, out alfa);
            return V3(b.Pos, alt) - _cam.transform.forward * frente;
        }

        /// <summary>Segura quem chegou ao trilho do R sem o caminhao parado (no comeco da calcada, ou escondido na porta, descendo). Sem o aviao ou com ele indo ao hangar, a escada nao vem: acaba ali.</summary>
        bool Segura(Boneco b)
        {
            if (b.Estande != 1 || _escada || b.Caminho.Count == 0) return false;
            Vector2 c = b.Caminho[0];
            if (c != Trilho[1][1].P && c != Trilho[1][5].P) return false;
            if (_avioes[1].Fase != Fase.NoPatio || S.Avioes[1].Hangar > 0) b.Caminho.Clear();
            b.VelAtual = 0f;
            return true;
        }

        /// <summary>Alguem indo ao aviao `k` ou ainda descendo do trilho dele: o aviao e a escada esperam. Quem ja desceu segue a vida.</summary>
        bool GenteNoTrilho(int k)
        {
            foreach (Boneco b in _pax.Values) if (NoCaminho(b, k)) return true;
            foreach (Andante a in _andantes) if (NoCaminho(a.B, k)) return true;
            return false;
        }

        static bool NoCaminho(Boneco b, int k)
        {
            if (b.Estande != k || b.Caminho.Count == 0) return false;
            return b.Caminho[b.Caminho.Count - 1] == Trilho[k][Trilho[k].Length - 1].P || Le(Trilho[k], b.Pos, out _, out _, out _);
        }

        /// <summary>O que muda no quadro: caminhao-escada no lugar (passarela), porta do jato e os passageiros que descem, um a um, a partir do servico de desembarque.</summary>
        void EmbarquePasso(Sim s, float dt)
        {
            _escada = false;
            foreach (Veiculo v in _veiculos) if (v.Tipo?.Malha == "escada_embarque" && v.Aviao == 1 && v.Lida == Lida.Servindo) _escada = true;
            _passarela.SetActive(_escada);
            _portaJato.SetActive(_escada);
            for (int k = 0; k < 2; k++)
            {
                bool desce = s.Avioes[k].Fazendo[(int)Servico.Desembarque];
                if (desce && !_descia[k]) _faltaDescer[k] = Descem;
                _descia[k] = desce;
                if (_faltaDescer[k] == 0 || _avioes[k].Fase != Fase.NoPatio || (k == 1 && !_escada)) continue;
                _proxDescer[k] -= dt;
                if (_proxDescer[k] > 0f) continue;
                _proxDescer[k] = DesceCada;
                _faltaDescer[k]--;
                Trilha(NovoAndante(PaxAtor[_faltaDescer[k] % PaxAtor.Length], Art.VooCor[k], 0f), Chegadas, k, true);
            }
        }
    }
}
