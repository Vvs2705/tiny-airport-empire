using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>O que o toque selecionou no mundo (GDD de direcao §3.4).</summary>
    public enum Sel { Nada, Pax, Func, Trip, Estacao }

    /// <summary>Humor do passageiro, na ordem de Textos.Humor: os 6 de quem esta no aeroporto e o de quem embarcou.</summary>
    public enum Humor { Tranquilo, Impaciente, Preocupado, Chateado, Irritado, Furioso, Contente }

    /// <summary>Uma ficha: a linha do topo da pessoa, o nome e 3 linhas; a ultima e o humor ou o gargalo, na cor dele.</summary>
    public sealed class FichaDados
    {
        public string Topo = "", Nome = "", Linha = "", Info = "", Nota = "";
        public Color Cor = Art.Fraco, Placa = Art.Azul, Tinta = Color.white;
        public Sprite Glifo;
        public bool Mover;
    }

    /// <summary>
    /// Ficha ao tocar (GDD de direcao §3.4 e §8.4), sem segurar o relogio: cartao logo acima da barra de baixo (por cima da faixa da meta) e uma
    /// linha no topo de quem esta selecionado. Passageiro: voo e destino, etapa, tempo no aeroporto e humor com o motivo; funcionario: perfil,
    /// estacao, tarefa e o Mover; tripulante: funcao, para onde vai e trechos; estacao: niveis, balcoes, fila e gargalo. Le so o que o nucleo ja
    /// sabe; tocar no vazio fecha. Quem escolhe o alvo e o Mundo (Mundo.Toca).
    /// </summary>
    public sealed partial class Hud
    {
        const float AltFicha = 256f, MargemFicha = 172f;
        /// <summary>Largura do texto da ficha (px da referencia), sem e com o Mover a direita: o teste confere que cada linha cabe.</summary>
        public const float LargFicha = RefW - 32f - MargemFicha - 28f, LargFichaMover = RefW - 32f - MargemFicha - 306f;
        /// <summary>ponytail: regua da tela para "espera longa" (s no aeroporto); a frente G (satisfacao com motivo) leva a de verdade ao nucleo.</summary>
        public const float EsperaLonga = 60f;
        /// <summary>A nota no cartao branco: o Good e o Warn da paleta nao leem sobre ele.</summary>
        static readonly Color NotaBem = Art.FeitoTinta, NotaAlerta = Art.Hex(0x8A5300), NotaMal = Art.Perigo;

        RectTransform _ficha, _fichaTopo;
        Image _fichaPlaca, _fichaGlifo;
        Text _fichaNome, _fichaLinha, _fichaInfo, _fichaNota, _fichaTopoTexto;
        Text[] _fichaTextos;
        readonly Vector3[] _cantos = new Vector3[4];
        BotaoPreco _fichaMover;
        Sel _sel;
        int _selId;

        void BuildFicha()
        {
            _ficha = Art.NovoCartao(_root, "Ficha", AltFicha);
            Art.Fixa(_ficha, Vector2.zero, new Vector2(16f, AltBase + 14f), new Vector2(RefW - 32f, AltFicha));
            _ficha.GetComponent<Image>().raycastTarget = true;   // o toque na ficha nao vai para o mundo
            _fichaPlaca = Art.NovaPlaca(_ficha, "Placa", Art.Pessoa(), Art.Azul, Color.white);
            Art.Fixa(_fichaPlaca.rectTransform, Vector2.up, new Vector2(28f, -28f), new Vector2(116f, 116f));
            _fichaGlifo = _fichaPlaca.transform.GetChild(0).GetComponent<Image>();
            _fichaNome = TextoFicha("Nome", Tipo.Nome, 18f, 56f);
            _fichaLinha = TextoFicha("Linha", Tipo.Corpo, 78f, 48f);
            _fichaInfo = TextoFicha("Info", Tipo.Legenda, 128f, 46f);
            _fichaNota = TextoFicha("Nota", Tipo.Corpo, 180f, 50f);
            _fichaTextos = new[] { _fichaNome, _fichaLinha, _fichaInfo, _fichaNota };
            _fichaMover = BotaoDoCartao(_ficha, () => { if (_sel == Sel.Func) AbreMover(_selId); });
            _fichaMover.Secundario(Textos.MoverCurto);
            _ficha.gameObject.SetActive(false);

            Image topo = Art.Panel(M.Rotulos, "Selecao", Art.A(Art.Tinta, 0.92f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            topo.raycastTarget = false;   // a linha nao segura o toque do mundo
            _fichaTopo = topo.rectTransform;
            _fichaTopoTexto = Art.Escreve(_fichaTopo, "Texto", Tipo.Legenda, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            _fichaTopoTexto.color = Art.Placa; _fichaTopoTexto.resizeTextMinSize = MenorTexto;
            _fichaTopo.gameObject.SetActive(false);
        }

        Text TextoFicha(string nome, Tipo tipo, float y, float alt)
        {
            Text t = Art.Escreve(_ficha, nome, tipo, Vector2.up, Vector2.one);
            t.rectTransform.offsetMin = new Vector2(MargemFicha, -y - alt); t.rectTransform.offsetMax = new Vector2(-28f, -y);
            t.resizeTextMinSize = MenorTexto;
            return t;
        }

        /// <summary>Mundo: o toque escolheu um alvo (Sel.Nada = tocou no vazio, fecha).</summary>
        internal void Seleciona(Sel tipo, int id) { _sel = tipo; _selId = id; }

        void PintaFicha(Sim s)
        {
            if (TourAberto || Arrastando) _sel = Sel.Nada;
            FichaDados f = _sel == Sel.Nada ? null : DadosDaFicha(s);
            if (f == null) _sel = Sel.Nada;
            _ficha.gameObject.SetActive(f != null);
            if (f == null) { _fichaTopo.gameObject.SetActive(false); return; }
            _fichaPlaca.color = f.Placa; _fichaGlifo.sprite = f.Glifo; _fichaGlifo.color = f.Tinta;
            _fichaNome.text = f.Nome; _fichaLinha.text = f.Linha; _fichaInfo.text = f.Info;
            _fichaNota.text = f.Nota; _fichaNota.color = f.Cor;
            _fichaMover.B.gameObject.SetActive(f.Mover);
            float dir = f.Mover ? 306f : 28f;
            foreach (Text t in _fichaTextos) t.rectTransform.offsetMax = new Vector2(-dir, t.rectTransform.offsetMax.y);
            _fichaTopoTexto.text = f.Topo;
            _fichaTopo.sizeDelta = new Vector2(_fichaTopoTexto.preferredWidth + 36f, 56f);
            _ficha.GetWorldCorners(_cantos);   // canvas overlay: px da tela
            _fichaTopo.gameObject.SetActive(M.PoeTopo(_fichaTopo, _sel, _selId, _cantos[1].y));
        }

        FichaDados DadosDaFicha(Sim s)
        {
            switch (_sel)
            {
                case Sel.Pax: bool naTela = M.PaxNaTela(_selId, out bool embarcou); return FichaPax(s, _selId, naTela, embarcou);
                case Sel.Func: return _selId < s.Staff.Count ? FichaFunc(s, _selId, M.Atende(_selId)) : null;
                case Sel.Trip: return _selId < s.Tripulacao.Count ? FichaTrip(s, s.Tripulacao[_selId]) : null;
                default: return FichaEstacao(s, (Estacao)_selId);
            }
        }

        /// <summary>Do pior motivo para o melhor, so com o que o nucleo ja sabe: voo perdido, fila que nao chega a tempo, voo atrasado, folga
        /// curta e espera longa. Esperar o embarque abrir no portao e o combinado, nao conta como espera.</summary>
        public static Humor HumorDe(Station st, Voo v, float espera, bool saiu, bool embarcou)
        {
            if (saiu) return embarcou ? Humor.Contente : Humor.Furioso;
            bool risco = st != null && v != null && st.VooEmRisco == v.Id;
            if (risco && st.Estado == Estado.Critico) return Humor.Irritado;
            if (v != null && v.Atrasado) return Humor.Chateado;
            if (risco && st.Estado == Estado.Atencao) return Humor.Preocupado;
            bool combinado = st != null && st.Tipo == Estacao.Portao && v != null && !v.Embarque;
            return espera > EsperaLonga && !combinado ? Humor.Impaciente : Humor.Tranquilo;
        }

        static Color CorDo(Humor h) => h == Humor.Tranquilo || h == Humor.Contente ? NotaBem : h >= Humor.Irritado ? NotaMal : NotaAlerta;

        /// <summary>Passageiro pelo id: no nucleo (fila ou atendimento) ou ainda na tela depois de sair dele (embarcou ou perdeu o voo). Null = sumiu.</summary>
        public static FichaDados FichaPax(Sim s, int id, bool naTela, bool embarcou)
        {
            Pax p = null;
            Station st = null;
            bool atende = false;
            foreach (Station x in s.Estacoes)
            {
                foreach (Pax q in x.Fila) if (q.Id == id) { p = q; st = x; }
                foreach (Pax q in x.Atendendo) if (q.Id == id) { p = q; st = x; atende = true; }
            }
            if (p == null && !naTela) return null;
            int onda = id / 1000, f = id / 500 % 2, vooId = onda * 2 + f;   // Pax.Onda, Pax.Voo e Voo.Id: quem saiu nao esta mais no nucleo
            Voo v = s.Voos[f] != null && s.Voos[f].Onda == onda ? s.Voos[f] : null;
            float espera = v != null && p != null ? (s.PassoNaOnda - s.Chegada(v, id % 500)) * Sim.Step : 0f;
            Humor h = HumorDe(st, v, espera, p == null, embarcou);
            var d = new FichaDados
            {
                Topo = Textos.Voo(vooId) + " · " + Textos.Humor[(int)h],
                Nome = Textos.Voo(vooId) + " · " + (v != null ? Textos.DestinoDe(v) : Textos.DestinoDe(vooId)),
                Nota = Textos.HumorLinha((int)h), Cor = CorDo(h), Glifo = Art.Mala(), Tinta = Art.VooCor[f],
            };
            if (p == null)
            {
                d.Linha = embarcou ? Textos.IndoAoAviao : Textos.IndoEmbora;
                d.Info = embarcou ? Textos.ABordoLogo : Textos.PortaoFechou;
                return d;
            }
            int e = (int)st.Tipo;
            d.Linha = (atende ? Textos.PaxAtendido[e] : st.Tipo == Estacao.Portao && v != null && v.Embarque ? Textos.NaFilaDoEmbarque : Textos.PaxNaFila[e])
                + (p.Historia ? Textos.ComHistoria : "");
            d.Info = Textos.NoAeroporto(espera, v != null && !v.Partiu ? (v.FechaPasso - s.PassoNaOnda) * Sim.Step : 0f, v != null && v.Atrasado);
            if (p.Historia && s.HistoriaAtual != null) { d.Nome = Textos.Historia[s.HistoriaOnda]; d.Nota = Textos.HistoriaPede(s.HistoriaAtual); }   // TA-3: a historia dele
            return d;
        }

        /// <summary>Funcionario k (posicao em Staff, como no Mover); atende = o boneco esta com passageiro no posto.</summary>
        public static FichaDados FichaFunc(Sim s, int k, bool atende)
        {
            Funcionario f = s.Staff[k];
            int p = (int)f.Perfil, e = (int)f.Em;
            bool andando = f.Andando > 0, casa = Funcionario.Especialidade(f.Perfil, f.Em), semFila = e >= (int)Estacao.Manutencao;
            string tarefa = andando ? Textos.Preparo(s.Preparo(f), f.Andando * Sim.Step)
                : f.Em == Estacao.Manutencao ? (Checando(s) ? Textos.NaChecagem : Textos.ProntoChecagem)
                : f.Em == Estacao.Despacho ? Textos.NoBonus
                : atende ? Textos.AtendendoUm : s.St(f.Em).Fila.Count > 0 ? Textos.ChamandoProximo : Textos.EsperandoPax;
            return new FichaDados
            {
                Topo = Textos.Nome(f) + " · " + (andando ? Textos.FaseTransf[(int)s.Fase(f)] : atende ? Textos.Atendendo : semFila ? Textos.Trabalhando : Textos.Livre),
                Nome = Textos.Nome(f) + " · " + Textos.Contratado(f),
                Linha = Textos.EmEstacao(e, andando), Info = tarefa,
                Nota = casa ? Textos.NaEspecialidade : Textos.PerfilInfo(p), Cor = casa ? NotaBem : Art.Fraco,
                Placa = Art.EstacaoCor[e], Glifo = Art.Pessoa(), Mover = true,
            };
        }

        static bool Checando(Sim s)
        {
            foreach (TA.Core.Aviao a in s.Avioes) if (a.Check > 0) return true;
            return false;
        }

        public static FichaDados FichaTrip(Sim s, Tripulante t)
        {
            Voo v = t.Aviao >= 0 ? s.Voos[t.Aviao] : null;
            return new FichaDados
            {
                Topo = t.Nome, Nome = t.Nome + " · " + Textos.FuncaoDe(t),
                Linha = t.Aviao >= 0 ? Textos.IndoPara(v != null ? v.Id : -1) : Textos.VoltaSala,
                Info = Textos.Trechos(t.Trechos, s.T.TrechosAteDescanso), Nota = Textos.EscalaNaTela, Glifo = Art.Aviao(),
            };
        }

        public static FichaDados FichaEstacao(Sim s, Estacao e)
        {
            int i = (int)e;
            Station st = s.St(e);
            var d = new FichaDados { Topo = Textos.Estacao[i], Nome = Textos.Estacao[i], Placa = Art.EstacaoCor[i], Glifo = Art.IconeEstacao(i) };
            if (!s.Desbloqueada(e))
            {
                d.Placa = Art.Vazio;
                d.Linha = Textos.FaltamEmbarques(s.T.Marcos[s.T.Abre[i] - 1] - s.TotalEmbarcados);
                d.Info = Textos.TerminalNome + " · " + Textos.NivelNome(s.T.Abre[i] + 1);
                d.Nota = Textos.Trancada;
                return d;
            }
            bool pax = e != Estacao.Manutencao && e != Estacao.Despacho;
            d.Linha = e == Estacao.Despacho ? Textos.SemMelhoria
                : Textos.MelhoriaNome(Melhoria.Velocidade, st.Vel, s.T.VelNiveis) + " · " + Textos.MelhoriaNome(Melhoria.Capacidade, st.Cap, s.T.CapNiveis);
            d.Info = (pax ? Textos.Balcoes(st.Postos, s.TempoSeg(e)) + " · " + Textos.NaFila(st.Fila.Count) + " · " : "") + Textos.NaEstacao(s.Alocados(e));
            if (pax) d.Topo += " · " + Textos.NaFila(st.Fila.Count);
            if (s.Falhou(e)) { d.Nota = Textos.GargaloFalha; d.Cor = NotaMal; }
            else if (st.Estado == Estado.Critico) { d.Nota = Textos.GargaloRisco(st.VooEmRisco); d.Cor = NotaMal; }
            else if (st.Estado == Estado.Atencao) { d.Nota = Textos.GargaloFolga(st.VooEmRisco); d.Cor = NotaAlerta; }
            else if (s.Presentes(e) > 0) { d.Nota = Textos.SemGargalo; d.Cor = NotaBem; }
            else
            {
                d.Nota = s.Alocados(e) > 0 ? Textos.EquipeVindo : e == Estacao.CheckIn || e == Estacao.Seguranca || e == Estacao.Portao ? Textos.GargaloToque : Textos.GargaloFechada;
                d.Cor = NotaAlerta;
            }
            return d;
        }

        /// <summary>-tela fichapax | fichafunc | fichaestacao | fichatrip, para as fotos: o 3o da maior fila, a Ana, essa sala ou o 1o tripulante.</summary>
        void FichaDev(string nome)
        {
            Sim s = S;
            Station cheia = s.Estacoes[0];
            foreach (Station st in s.Estacoes) if (st.Fila.Count > cheia.Fila.Count) cheia = st;
            switch (nome)
            {
                case "fichafunc": if (s.Staff.Count > 0) Seleciona(Sel.Func, 0); break;
                case "fichatrip": if (s.Tripulacao.Count > 0) Seleciona(Sel.Trip, 0); break;
                case "fichaestacao": Seleciona(Sel.Estacao, (int)cheia.Tipo); break;
                default: if (cheia.Fila.Count > 0) Seleciona(Sel.Pax, cheia.Fila[Mathf.Min(2, cheia.Fila.Count - 1)].Id); break;
            }
        }
    }
}
