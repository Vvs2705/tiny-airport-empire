using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// A segurança com etapas na tela (só apresentação: o tempo continua sendo o do núcleo, Pax.Resto/Total). Cada posto é uma linha: o
    /// passageiro põe a mala na bandeja, a bandeja passa no raio-x, ele atravessa o pórtico (a cabine de corpo inteiro no nível mais alto da
    /// Velocidade) e pega a mala do outro lado. Um em cada 4 passa pela vistoria com o scanner de mão, se houver agente no posto (fixo pelo
    /// id do passageiro: a tela não sorteia nada).
    /// </summary>
    public sealed partial class Mundo
    {
        /// <summary>Linha de cada posto ao longo da parede (a linha cheia cabe 4 na sala) e a escala do raio-x e do pórtico.</summary>
        static readonly float[] SegA = { -0.43f, 0.43f, -1.26f, 1.26f };
        const float EscalaSeg = 0.5f, PassoX = -0.22f, BeltX = 0.2f, SegMaquinaD = 0.72f, SegAntesD = 1.18f, SegDepoisD = 0.36f, SegMalaD = 0.28f, SegAgenteD = 0.16f;
        /// <summary>Troca por nome de arquivo: sem a malha do nível, fica a de baixo. Nível 2 = Velocidade 4 (NivelMalha).</summary>
        static readonly string[] PorticoMalha = { "portico_detector", "portico_detector", "cabine_scanner_corpo" };
        readonly GameObject[,] _portico = new GameObject[4, 3];
        readonly Transform[] _bandeja = new Transform[4], _scanner = new Transform[4];
        readonly SpriteRenderer[] _malaSeg = new SpriteRenderer[4];

        static float Posto(int e, int i) => e == (int)Estacao.Seguranca ? SegA[i] : Desloc[i];

        /// <summary>Pórtico, bandeja, mala e scanner de um posto (Sala3D). O raio-x é o balcão da estação.</summary>
        void Seguranca3D(int i)
        {
            const int e = (int)Estacao.Seguranca;
            Vector2 p = Em(e, SegMaquinaD, SegA[i] + PassoX);
            Transform ant = null;
            for (int n = 0; n < PorticoMalha.Length; n++)
            {
                Transform t = Peca(PorticoMalha[n], V3(p), 0f, n == 2 ? 0.55f : EscalaSeg);
                if (t == null) t = ant;
                else t.gameObject.SetActive(false);
                ant = t;
                _portico[i, n] = t != null ? t.gameObject : null;
            }
            _bandeja[i] = Peca("bandeja_seguranca", V3(Em(e, SegAntesD, SegA[i] + BeltX), 0.2f), 0f, 0.5f);
            if (_bandeja[i] == null) _bandeja[i] = Bloco("Bandeja", PrimitiveType.Cube, V3(Em(e, SegAntesD, SegA[i] + BeltX), 0.2f), new Vector3(0.15f, 0.03f, 0.15f), Art.Hex(0x8A94A6)).transform;
            _bandeja[i].gameObject.SetActive(false);
            _malaSeg[i] = Billboard("MalaSeg" + i, Art.Mala(), Color.white, 0.12f);
            _scanner[i] = Peca("scanner_mao", Vector3.zero, 0f, 0.6f);
            if (_scanner[i] != null) _scanner[i].gameObject.SetActive(false);
        }

        void PorticoNivel(Sim s)
        {
            int nivel = NivelDe(s.St(Estacao.Seguranca).Vel), postos = s.St(Estacao.Seguranca).Postos;
            for (int i = 0; i < 4; i++)
                for (int n = 0; n < PorticoMalha.Length; n++)
                    if (_portico[i, n] != null && _portico[i, nivel] != _portico[i, n]) _portico[i, n].SetActive(false);
            for (int i = 0; i < 4; i++)
                if (_portico[i, nivel] != null) _portico[i, nivel].SetActive(i < postos);
        }

        static bool Vistoria(Pax p) => p.Id % 4 == 1;

        /// <summary>Onde o passageiro está no posto `i` com o atendimento na fração `f`.</summary>
        static Vector2 PontoSeguranca(Pax p, int i, float f, bool agente)
        {
            const int e = (int)Estacao.Seguranca;
            if (f < 0.28f) return Em(e, SegAntesD, SegA[i] + PassoX);   // põe a mala na bandeja
            if (f < 0.62f || (Vistoria(p) && agente && f < 0.88f)) return Em(e, SegDepoisD, SegA[i] + PassoX);   // atravessou o pórtico; vistoria
            return Em(e, SegMalaD, SegA[i] + BeltX);   // pega a mala
        }

        /// <summary>A bandeja anda pelo raio-x no tempo do atendimento; a mala em cima, na cor do voo; o scanner sobe e desce na vistoria.</summary>
        void BandejaPasso(int i, Pax p, float f, bool agente, Vector2 pos)
        {
            const int e = (int)Estacao.Seguranca;
            bool ve = p != null && f >= 0.14f;
            _bandeja[i].gameObject.SetActive(ve);
            _malaSeg[i].enabled = ve && f < 0.95f;
            bool scan = p != null && agente && Vistoria(p) && f >= 0.62f && f < 0.88f;
            if (_scanner[i] != null) _scanner[i].gameObject.SetActive(scan);
            if (!ve) return;
            Vector3 b = V3(Em(e, Mathf.Lerp(SegAntesD, SegMalaD, Mathf.InverseLerp(0.22f, 0.82f, f)), SegA[i] + BeltX), 0.2f);
            _bandeja[i].position = b;
            _malaSeg[i].transform.SetPositionAndRotation(b + Vector3.up * 0.08f, _cam.transform.rotation);
            _malaSeg[i].color = Art.VooCor[p.Voo];
            _malaSeg[i].sortingOrder = Ordem(b) + 6;
            if (scan) _scanner[i].position = V3(pos + new Vector2(0.08f, -0.05f), 0.15f + 0.2f * Mathf.PingPong(Time.time * 1.6f, 1f));
        }
    }
}
