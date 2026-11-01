using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// A segurança com etapas na tela (só apresentação: o tempo continua sendo o do núcleo, Pax.Resto/Total). Cada posto é uma linha com 2 raias,
    /// da fila até a parede de saída: na da mala, mesa e raio-x, por onde corre a bandeja; na da pessoa, o pórtico detector de metal e a cabine de
    /// corpo inteiro. O passageiro põe a mala na bandeja, passa pelo pórtico, para na cabine enquanto o feixe corre, sai diante do agente e pega a
    /// mala no fim da mesa. Um em cada 4 passa pela vistoria com o scanner de mão, se houver agente no posto (fixo pelo id do passageiro: a tela não
    /// sorteia nada). As linhas são espelhadas no meio da sala, com a raia da pessoa para fora, e quem sai anda atrás das máquinas até a porta.
    /// </summary>
    public sealed partial class Mundo
    {
        const int SalaSeg = (int)Estacao.Seguranca, Etapas = 4;
        /// <summary>Linha de cada posto ao longo da parede (a linha cheia cabe 4 na sala) e a escala do raio-x e do pórtico.</summary>
        static readonly float[] SegA = { -0.43f, 0.43f, -1.26f, 1.26f };
        const float EscalaSeg = 0.5f, PassoX = -0.22f, BeltX = 0.2f, EscalaCabine = EscalaSeg * 0.85f;
        /// <summary>Distâncias da parede de saída: parada diante da mesa, pórtico, cabine, raio-x, parada atrás das máquinas, agente e as pontas da
        /// esteira (centro do rolo: ela passa disso meia largura). Altura e largura da mesa.</summary>
        const float EntraD = 1.22f, PorticoD = 1.06f, CabineD = 0.6f, RaioXD = 0.66f, SaiD = 0.2f, AgenteD = 0.09f, MesaIniD = 1.08f, MesaFimD = 0.4f, AltMesa = 0.2f, LargMesa = 0.24f;
        static readonly Color CorLuz = Art.Hex(0x7FD8A0), CorFeixe = new Color(0.35f, 0.95f, 1f, 0.85f);

        readonly GameObject[] _linha = new GameObject[4];
        readonly Transform[] _portico = new Transform[4], _cabine = new Transform[4], _bandeja = new Transform[4], _scanner = new Transform[4];
        readonly Renderer[] _luz = new Renderer[4];
        readonly SpriteRenderer[] _feixe = new SpriteRenderer[4];
        readonly Mala[] _malaSeg = new Mala[4];

        static float Posto(int e, int i) => e == SalaSeg ? SegA[i] : e == SalaCheckIn ? ChkA[i] : e == Portao ? PodioA[i] : Desloc[i];
        static Vector2 RaiaPessoa(int i, float d) => Em(SalaSeg, d, SegA[i] - Mathf.Sign(SegA[i]) * PassoX);
        static Vector2 RaiaMala(int i, float d) => Em(SalaSeg, d, SegA[i] - Mathf.Sign(SegA[i]) * BeltX);
        /// <summary>O centro do que fica no lugar do balcão (na segurança, o raio-x), para a planta e o -diag.</summary>
        static Vector2 MesaEm(int e, int i) => e == SalaSeg ? RaiaMala(i, RaioXD) : Balcao(e, i);
        public static Vector2 Agente(int i) => RaiaPessoa(i, AgenteD);

        /// <summary>As paradas de quem passa pela linha `i`, em ordem: põe a mala na bandeja, fica na cabine, sai diante do agente e pega a mala.</summary>
        public static Vector2 Parada(int i, int k) => k == 0 ? RaiaPessoa(i, EntraD) : k == 1 ? RaiaPessoa(i, CabineD) : k == 2 ? RaiaPessoa(i, SaiD) : RaiaMala(i, SaiD);

        /// <summary>O que ninguém atravessa na linha `i`, no chão: mesa, raio-x, as colunas do pórtico e os painéis da cabine (PostosTests).</summary>
        public static Rect[] CaixasLinha(int i)
        {
            float lado = 0.21f;
            Vector2 p = RaiaPessoa(i, PorticoD), c0 = RaiaPessoa(i, CabineD + 0.2f), c1 = RaiaPessoa(i, CabineD - 0.2f), x = Vector2.right * lado;
            return new[]
            {
                Caixa(RaiaMala(i, MesaIniD), RaiaMala(i, MesaFimD), LargMesa * 0.5f), Caixa(RaiaMala(i, RaioXD + 0.08f), RaiaMala(i, RaioXD - 0.08f), 0.2f),
                Caixa(p - x, p - x, 0.04f), Caixa(p + x, p + x, 0.04f), Caixa(c0 - x, c1 - x, 0.02f), Caixa(c0 + x, c1 + x, 0.02f),
            };
        }

        static Rect Caixa(Vector2 a, Vector2 b, float meia) => Rect.MinMaxRect(Mathf.Min(a.x, b.x) - meia, Mathf.Min(a.y, b.y) - meia, Mathf.Max(a.x, b.x) + meia, Mathf.Max(a.y, b.y) + meia);

        /// <summary>Atrás das máquinas, do lado da parede de saída.</summary>
        static bool AtrasDaLinha(Vector2 p) => Vector2.Dot(p - Parada(0, 0), Salas[SalaSeg].N) > 0.02f;

        /// <summary>Pórtico, cabine, esteira, bandeja, mala e scanner de um posto (Sala3D), num grupo que aparece com o posto comprado. O raio-x é o
        /// balcão da estação.</summary>
        void Seguranca3D(int i)
        {
            Transform sala = _cont;
            _cont = new GameObject("Linha" + i).transform;
            _cont.SetParent(sala, false);
            _linha[i] = _cont.gameObject;
            float alt = AltMesa;
            _portico[i] = Peca("portico_detector", V3(RaiaPessoa(i, PorticoD)), 0f, EscalaSeg);
            _cabine[i] = Peca("cabine_scanner_corpo", V3(RaiaPessoa(i, CabineD)), 0f, EscalaCabine);
            _luz[i] = Bloco("Luz", PrimitiveType.Cube, V3(RaiaPessoa(i, PorticoD), 0.68f), Vector3.one * 0.05f, CorLuz);
            Esteira(RaiaMala(i, MesaIniD), RaiaMala(i, MesaFimD), alt, LargMesa);
            for (int n = 0; n < 3; n++) Peca("bandeja_seguranca", V3(RaiaMala(i, MesaIniD), alt + n * 0.035f), 0f, EscalaSeg);   // a pilha de bandejas vazias
            _bandeja[i] = Peca("bandeja_seguranca", V3(RaiaMala(i, MesaIniD), alt), 0f, EscalaSeg);
            if (_bandeja[i] == null) _bandeja[i] = Bloco("Bandeja", PrimitiveType.Cube, V3(RaiaMala(i, MesaIniD), alt), new Vector3(0.3f, 0.04f, 0.3f), Art.Hex(0x8A94A6)).transform;
            _bandeja[i].gameObject.SetActive(false);
            _malaSeg[i] = NovaMala("MalaSeg" + i);
            _scanner[i] = Peca("scanner_mao", Vector3.zero, 0f, 1.2f * EscalaFunc);
            if (_scanner[i] != null) _scanner[i].gameObject.SetActive(false);
            _cont = sala;
            _feixe[i] = Billboard("Feixe" + i, Art.Square(), CorFeixe, 1f);
        }

        void LinhasAtivas(Sim s)
        {
            for (int i = 0; i < 4; i++) _linha[i].SetActive(i < s.St(Estacao.Seguranca).Postos);
        }

        static bool Vistoria(Pax p) => p.Id % 4 == 1;

        static int EtapaDe(Pax p, float f, bool agente) => f < 0.22f ? 0 : f < 0.52f ? 1 : f < (agente && Vistoria(p) ? 0.9f : 0.74f) ? 2 : 3;

        /// <summary>A parada do passageiro no posto `i` com o atendimento na fração `f`. A rota acumula: atrasado, ele passa por todas, na ordem, e nunca
        /// corta caminho pelas máquinas.</summary>
        Vector2 ParadaDe(Pax p, int i, float f, bool agente)
        {
            if (!_pax.TryGetValue(p.Id, out Boneco b) || !b.NoBalcao) return Parada(i, 0);
            for (int quer = EtapaDe(p, f, agente); b.Etapa < quer;) b.Caminho.Add(Parada(i, ++b.Etapa));
            return Parada(i, b.Etapa);
        }

        /// <summary>Liga, antes de mudar de sala. O núcleo não espera a pessoa: quem terminou no meio da linha faz o resto das paradas e só então segue.
        /// False = não está na linha.</summary>
        bool FimDaLinha(Boneco b, ref Vector2 p)
        {
            if (b.Zona != SalaSeg || !b.NoBalcao) return false;
            int i = b.Vaga - VagaPosto;
            while (b.Etapa < Etapas - 1) b.Caminho.Add(Parada(i, ++b.Etapa));
            p = Parada(i, Etapas - 1);
            return true;
        }

        /// <summary>Sai(): quem perdeu o voo no meio da linha volta por ela até a frente das máquinas.</summary>
        void VoltaDaLinha(Boneco b, ref Vector2 p)
        {
            if (b.Zona != SalaSeg || !b.NoBalcao) return;
            for (int k = b.Etapa - 1; k >= 0; k--) b.Caminho.Add(p = Parada(b.Vaga - VagaPosto, k));
        }

        /// <summary>RotaPessoa: as máquinas fecham a sala de parede a parede, e quem vai de um lado delas para o outro (o agente) usa a raia da pessoa do
        /// posto mais perto.</summary>
        public static void CruzaLinha(List<Vector2> r, ref Vector2 p, Vector2 alvo)
        {
            bool atras = AtrasDaLinha(p);
            if (SalaEm(p) != SalaSeg || atras == AtrasDaLinha(alvo)) return;
            float x = (atras ? p : alvo).x;
            int i = 0;
            for (int k = 1; k < 4; k++) if (Mathf.Abs(Parada(k, 2).x - x) < Mathf.Abs(Parada(i, 2).x - x)) i = k;
            Eixos(r, ref p, Parada(i, atras ? 2 : 0), false);
            p = Parada(i, atras ? 0 : 2);
            r.Add(p);
        }

        /// <summary>A bandeja anda pelo raio-x no tempo do atendimento; a mala deitada em cima, na cor do voo, e a pessoa só com a mala de mão até pegá-la
        /// de volta; a luz do pórtico acende quando a pessoa passa; o feixe corre o corpo na cabine; o scanner sobe e desce na vistoria.</summary>
        void BandejaPasso(int i, Pax p, float f, bool agente)
        {
            Boneco dono = p != null ? _pax[p.Id] : null;
            bool parou = dono != null && dono.Caminho.Count == 0;
            bool ve = p != null && f >= 0.12f, naBandeja = ve && f < 0.95f, naCabine = parou && dono.Etapa == 1, scan = parou && dono.Etapa == 2 && agente && Vistoria(p);
            _bandeja[i].gameObject.SetActive(ve);
            _malaSeg[i].T.gameObject.SetActive(naBandeja);
            if (_scanner[i] != null) _scanner[i].gameObject.SetActive(scan);
            _feixe[i].enabled = naCabine;
            bool noArco = dono != null && Vector2.Distance(dono.Pos, RaiaPessoa(i, PorticoD)) < 0.16f;
            Cor(_luz[i], noArco ? (Vistoria(p) ? Art.Bad : Art.Good) : agente ? CorLuz : Apagado);
            if (dono == null) return;
            if (parou && dono.Etapa == 0) { Olha(dono, Salas[SalaSeg].T * -Mathf.Sign(SegA[i])); Gesto(dono, dono.PoeMala, Mathf.InverseLerp(0f, 0.2f, f)); }   // poe a mala na bandeja
            if (parou && dono.Etapa == 3) { Olha(dono, -Salas[SalaSeg].N); Gesto(dono, dono.PoeMala, Mathf.InverseLerp(1f, 0.82f, f)); }   // e pega de volta
            if (naCabine)
            {
                Vector3 c = V3(dono.Pos) + _cam.transform.up * (EscalaFunc * (0.08f + 0.8f * Mathf.PingPong(Time.time * 1.5f, 1f))) - _cam.transform.forward * 1.2f * EscalaFunc;
                _feixe[i].transform.SetPositionAndRotation(c, _cam.transform.rotation);
                _feixe[i].transform.localScale = new Vector3(0.42f * EscalaFunc, 0.04f * EscalaFunc, 1f);
                _feixe[i].sortingOrder = Ordem(c) + 9;
            }
            if (scan && _scanner[i] != null) _scanner[i].position = V3(dono.Pos + new Vector2(0.14f, 0.08f) * EscalaFunc, EscalaFunc * (0.3f + 0.45f * Mathf.PingPong(Time.time * 1.6f, 1f)));
            if (!ve) return;
            SoMao(dono, p, naBandeja || dono.Despachou);
            Vector3 b = V3(RaiaMala(i, Mathf.Lerp(MesaIniD, MesaFimD, Mathf.InverseLerp(0.22f, 0.8f, f))), AltMesa);
            _bandeja[i].position = b;
            Deita(_malaSeg[i], b + Vector3.up * 0.14f, Rumo(Salas[SalaSeg].T), 0.56f * (dono.Despachou ? 0.6f : 1f));
            Cor(_malaSeg[i].R, Art.VooCor[p.Voo]);
        }
    }
}
