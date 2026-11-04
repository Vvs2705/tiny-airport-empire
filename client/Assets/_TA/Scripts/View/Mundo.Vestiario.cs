using System.Collections.Generic;
using TA.Core;
using UnityEngine;

namespace TA
{
    /// <summary>
    /// Vestiario (frente E; GDD §4.1, Direcao §9.3 e §9.6): sala da equipe no canto livre ao sul do despacho e a leste da alimentacao, com a porta
    /// no corredor que liga o portao ao despacho. Ninguem passa pelo despacho (em obra ate o marco 2) nem pela alimentacao: quem vai trocar de
    /// setor sai pela porta do portao (ou do despacho), desce o corredor e entra. Sem fila e sem balcao; armarios na parede de fundo, bancos e a
    /// arara. O boneco segue a fase do nucleo (Sim.Fase): fica no posto enquanto termina o atendimento, vai ao armario, troca a cor do uniforme
    /// so aqui dentro (quando o nucleo diz que a troca acabou) e segue ao posto novo. O relogio da troca fica no posto de destino (Salas3D).
    /// </summary>
    public sealed partial class Mundo
    {
        static readonly Vector2 VestMin = new Vector2(12f, 0f);
        /// <summary>Porta do vestiario e a do portao para o despacho, no meio do corredor (x = 12); a entrada, ja dentro.</summary>
        static readonly Vector2 VestPorta = new Vector2(12f, 2f), CorredorPorta = new Vector2(12f, 6f), VestEntra = new Vector2(12.45f, 2f);

        readonly bool[] _rotaVest = new bool[Sim.MaxEquipe];
        readonly int[] _posto = new int[Sim.MaxEquipe];

        /// <summary>Onde o funcionario k troca de roupa: de frente para os armarios (parede leste), um ao lado do outro.</summary>
        public static Vector2 VestLugar(int k) => new Vector2(15.15f, 0.75f + 0.36f * k);

        public static bool NoVestiario(Vector2 p) => p.x > VestMin.x + 0.1f && p.x < VestMin.x + Lado && p.y > VestMin.y && p.y < VestMin.y + Lado;

        /// <summary>No corredor entre a porta do vestiario e a do portao (o SalaEm daria a alimentacao ou o portao).</summary>
        public static bool NoCorredor(Vector2 p) => Mathf.Abs(p.x - VestPorta.x) < 0.11f && p.y > VestPorta.y - 0.1f && p.y < CorredorPorta.y + 0.1f;

        /// <summary>Lado do corredor por onde se chega a sala `e`: oficina e despacho pela porta do despacho; o resto, pela do portao.</summary>
        static int LadoCorredor(int e) => e == SalaDespacho || e == SalaOficina ? SalaDespacho : Portao;

        /// <summary>De p (sala `de`, corredor ou vestiario) ate o armario k: porta a porta ate o portao (ou o despacho), o corredor e a porta.</summary>
        public static void RotaVestiario(List<Vector2> r, Vector2 p, int de, int k)
        {
            if (!NoVestiario(p))
            {
                if (!NoCorredor(p))
                {
                    int lado = LadoCorredor(de);
                    var portas = new List<Vector2>();   // ponytail: so na hora da transferencia, nao a cada quadro
                    PelasPortas(de, lado, portas);
                    foreach (Vector2 porta in portas) { CruzaLinha(r, ref p, porta); PelaPorta(r, ref p, porta); }
                    Vector2 dentro = CorredorPorta + Vector2.right * (lado == Portao ? -0.45f : 0.45f);
                    if (lado == Portao) RotaSala(r, ref p, dentro); else Eixos(r, ref p, dentro, true);
                    r.Add(p = CorredorPorta);
                }
                Eixos(r, ref p, VestPorta, false);
                Eixos(r, ref p, VestEntra, true);
            }
            Eixos(r, ref p, VestLugar(k), false);   // entre os bancos e depois ao longo dos armarios
        }

        /// <summary>Do vestiario (ou do corredor) ate `alvo` na sala `para`: a porta, o corredor e, do portao ou do despacho, a rota de sempre.</summary>
        public void RotaDoVestiario(List<Vector2> r, Vector2 p, int para, Vector2 alvo)
        {
            if (NoVestiario(p)) { Eixos(r, ref p, VestEntra, true); r.Add(p = VestPorta); }
            Eixos(r, ref p, CorredorPorta, false);
            int lado = LadoCorredor(para);
            r.Add(p = CorredorPorta + Vector2.right * (lado == Portao ? -0.45f : 0.45f));
            RotaPessoa(r, p, lado, para, alvo, false);
        }

        /// <summary>Preparo (0 a 1) de quem vem para o j-esimo posto livre da estacao `e`, na ordem da equipe; -1 = ninguem a caminho.</summary>
        static float Chegando(Sim s, Estacao e, int j)
        {
            foreach (Funcionario f in s.Staff)
                if (f.Em == e && f.Andando > 0 && j-- == 0) return s.Preparo(f);
            return -1f;
        }

        void Vestiario3D()
        {
            Vector2 c = VestMin + Vector2.one * (Lado * 0.5f), m = VestMin + Vector2.one * (Corredor * 0.5f);
            Renderer piso = Bloco("PisoVestiario", PrimitiveType.Cube, new Vector3(c.x, -0.05f, c.y), new Vector3(Interno, 0.1f, Interno), PisoServico);
            if (Material != null) { piso.sharedMaterial = MatPiso(); Cor(piso, PisoServico); }
            // paredes em corte, como as salas (Sala3D): a de oeste tem a porta para o corredor; o rodape e o do despacho, a ala da equipe
            float porta = VestPorta.y - m.y;
            Muro(SalaDespacho, m, Vector2.right, 0f, Interno, AltoFrente);                                  // sul
            Muro(SalaDespacho, m + Vector2.up * Interno, Vector2.right, 0f, Interno, AltoFundo);            // norte
            Muro(SalaDespacho, m, Vector2.up, 0f, porta - PortaLarg * 0.5f, AltoFrente);                     // oeste, ate a porta
            Muro(SalaDespacho, m, Vector2.up, porta + PortaLarg * 0.5f, Interno, AltoFrente);                // oeste, depois da porta
            Muro(SalaDespacho, m + Vector2.right * Interno, Vector2.up, 0f, Interno, AltoFundo);            // leste
            Fixo(PrimitiveType.Cube, V3(VestPorta, 0.002f), new Vector3(Corredor, 0.004f, PortaLarg + 0.1f), CorCapacho);
            Fixo(PrimitiveType.Cube, V3(VestPorta + Vector2.up * 2f, 0.002f), new Vector3(0.12f, 0.004f, 3.6f), CorZona);   // faixa do corredor da equipe
            // armarios de frente para quem troca, 2 bancos fora do caminho (que passa em z = 2) e a arara junto da parede norte
            foreach (float z in new[] { 1f, 1.65f, 2.3f }) Peca("armarios_vestiario", new Vector3(15.55f, 0f, z), Rumo(Vector2.left));
            foreach (float z in new[] { 1.1f, 2.9f }) Peca("banco_vestiario", new Vector3(13.6f, 0f, z), 90f);
            Peca("arara_uniformes", new Vector3(13.2f, 0f, 3.45f), 90f);   // o varao sai do Tripo no eixo da frente: girada, corre ao longo da parede
            PlacaVestiario(new Vector3(c.x, AltoFundo + 0.2f, VestMin.y + Lado - Corredor * 0.5f));
        }

        /// <summary>Letreiro em cima da parede norte, como o das salas (Placa), sem selo de estado: o vestiario nao tem fila.</summary>
        void PlacaVestiario(Vector3 p)
        {
            Quaternion rosto = Quaternion.Euler(20f, 0f, 0f);
            Bloco("PlacaVestiario", PrimitiveType.Cube, p, new Vector3(1.8f, 0.34f, 0.03f), Art.EstacaoCor[SalaDespacho]).transform.rotation = rosto;
            var go = new GameObject("PlacaNomeVestiario");
            go.transform.SetParent(_raiz, false);
            go.transform.SetPositionAndRotation(p + rosto * Vector3.back * 0.02f, rosto);
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = Art.Font();
            tm.fontSize = 72;
            tm.characterSize = 0.03f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = Art.Ink;
            tm.text = Textos.Vestiario;
            go.GetComponent<MeshRenderer>().sharedMaterial = Art.Font().material;
        }
    }
}
