using System;
using System.Collections.Generic;
using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// Telas modais (seguram o relogio do quadro enquanto abertas), todas em folha (Art.NovaFolha): melhorias (efeito de cada upgrade escrito
    /// antes de comprar, GDD §5.6, e as estacoes travadas com o motivo), "mover para" (o botao alternativo ao arrasto, GDD §5.5), cofre offline
    /// (intervalo e calculo, GDD §6.3), anuncio simulado (v0.1 sem SDK: 3 s e so entao o Pagar do nucleo; fechar antes = sem recompensa), pausa e
    /// album das historias (TA-3). Item, Compra e Resumo sao as pecas que estas telas e as da operacao repetem, com as medidas da Equipe.
    /// </summary>
    public sealed partial class Hud
    {
        const float AltTitulo = 92f, AltCompra = 168f;   // cartao de estacao ou servico: faixa do nome e cada linha de compra

        sealed class Modal { public RectTransform Rt; public Text Titulo; public Folha F; }

        /// <summary>Cartao de lista: placa, nome, 2 linhas e, com acao, o botao no canhoto.</summary>
        sealed class Item { public RectTransform Rt; public Image Placa, Glifo; public Text Nome, Linha, Info; public BotaoPreco Btn; }

        /// <summary>Linha de melhoria: nome com o nivel, efeito, barra e preco.</summary>
        sealed class Compra
        {
            public Text Nome, Efeito;
            public BarraNivel Barra;
            public BotaoPreco Btn;

            public void Pinta(string nome, string efeito, int nivel, long custo, long cash)
            {
                Nome.text = nome; Efeito.text = efeito;
                Barra.Pinta(nivel, Art.Compra);
                if (custo < 0) Btn.Maximo(Textos.Maximo); else Btn.Preco(Textos.Melhorar, custo, cash);
            }
        }

        /// <summary>Os 3 numeros do alto da folha: o da tela, Cash e Terminal.</summary>
        sealed class Resumo
        {
            public Numero Tela, Cash, Terminal;

            public void Pinta(Sim s, string tela)
            {
                Tela.Valor.text = tela;
                Cash.Valor.text = Textos.Num(s.Cash);
                Terminal.Valor.text = Textos.NivelNome(s.Marco + 1);
            }
        }

        Modal _melhorias, _mover, _cofre, _anuncio, _pausa, _album;
        readonly List<Modal> _modais = new List<Modal>();
        Resumo _melhoriasResumo, _pausaResumo, _albumResumo;
        readonly Item[] _melhoriaTrancada = new Item[8];   // as 8 estacoes; o despacho so tem este cartao, porque nao tem upgrade
        readonly RectTransform[] _melhoriaCartao = new RectTransform[7];
        readonly Compra[,] _melhoriaCompra = new Compra[7, 2];
        readonly Item[] _moverItem = new Item[8];
        Item _pausaDiario, _anuncioItem;
        Item[] _albumItem;
        Numero _cofreGanho;
        Text _cofreTexto;
        BotaoPreco _coletar, _dobrar;
        BarraNivel _anuncioBarra;
        int _moverQuem = -1;
        float _anuncioFalta;
        Anuncio _anuncioQual;
        string _anuncioTx;

        public bool ModalAberto { get { foreach (Modal m in _modais) if (m.Rt.gameObject.activeSelf) return true; return false; } }

        /// <summary>Modal na linguagem nova (Art.NovaFolha): o conteudo entra em m.F.Corpo; o X e o toque fora fecham.</summary>
        Modal NovaFolhaModal(RectTransform canvas, string nome, string titulo, Sprite icone, float altura)
        {
            var m = new Modal();
            Image veu = Art.Img(canvas, nome, Art.Square(), new Color(0f, 0f, 0f, 0.62f), Vector2.zero, Vector2.one);
            veu.preserveAspect = false; veu.raycastTarget = true;   // segura o toque do jogo atras
            m.Rt = veu.rectTransform;
            m.F = Art.NovaFolha(m.Rt, titulo, icone, altura, () => Fecha(m));
            m.Titulo = m.F.Titulo;
            m.Rt.gameObject.SetActive(false);
            _modais.Add(m);
            return m;
        }

        /// <summary>Resumo de 3 numeros no alto da folha; devolve a lista rolavel que fica embaixo dela.</summary>
        RectTransform NovoResumo(Modal m, string rotulo, Sprite icone, Color cor, out Resumo f)
        {
            RectTransform pe = m.F.Corpo, nums = Art.Node(pe, "Numeros", Vector2.up, Vector2.one);
            nums.offsetMin = new Vector2(24f, -172f); nums.offsetMax = new Vector2(-24f, -16f);
            f = new Resumo
            {
                Tela = Art.NovoNumero(nums, "Tela", icone, cor, rotulo, Vector2.zero, new Vector2(0.34f, 1f)),
                Cash = Art.NovoNumero(nums, "Cash", Art.Moeda(), Color.white, Textos.CashNome, new Vector2(0.34f, 0f), new Vector2(0.67f, 1f)),
                Terminal = Art.NovoNumero(nums, "Terminal", Art.Estrela(), Art.Coin, Textos.TerminalNome, new Vector2(0.67f, 0f), Vector2.one),
            };
            RectTransform lista = Art.NovaLista(pe, Vector2.zero, Vector2.one);
            Art.Margem((RectTransform)lista.parent, 24f, 0f, 24f, 184f);
            return lista;
        }

        static BotaoPreco BotaoDoCartao(RectTransform pai, Action acao)
        {
            BotaoPreco b = Art.NovoBotaoPreco(pai, Vector2.zero, Vector2.zero, acao);
            Art.Fixa((RectTransform)b.B.transform, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(246f, 146f));
            return b;
        }

        /// <summary>Botao de largura cheia ao pe da folha, para a acao que encerra a tela.</summary>
        static BotaoPreco BotaoDoPe(RectTransform corpo, Vector2 anchorMin, Vector2 anchorMax, Action acao)
        {
            BotaoPreco b = Art.NovoBotaoPreco(corpo, anchorMin, anchorMax, acao);
            Art.Margem((RectTransform)b.B.transform, 24f, 24f, 24f, -174f);
            return b;
        }

        /// <summary>altura e topo posicionam o bloco de texto; reserva guarda espaco a direita para uma barra.</summary>
        Item NovoItem(RectTransform lista, string name, Sprite icone, Color cor, Action acao, float altura = 186f, float topo = 20f, float reserva = 0f)
        {
            RectTransform c = Art.NovoCartao(lista, name, altura, acao != null ? 290f : 0f);
            Image placa = Art.NovaPlaca(c, "Placa", icone, cor, Color.white);
            float lado = Mathf.Min(126f, altura - 40f), x = 46f + lado, dir = (acao != null ? 306f : 28f) + reserva;
            Art.Fixa(placa.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(lado, lado));
            var it = new Item
            {
                Rt = c, Placa = placa, Glifo = placa.transform.GetChild(0).GetComponent<Image>(),
                Nome = Art.Escreve(c, "Nome", Tipo.Nome, Vector2.up, Vector2.one),
                Linha = Art.Escreve(c, "Linha", Tipo.Legenda, Vector2.up, Vector2.one),
                Info = Art.Escreve(c, "Info", Tipo.Legenda, Vector2.up, Vector2.one, TextAnchor.UpperLeft),
            };
            it.Nome.rectTransform.offsetMin = new Vector2(x, -topo - 56f); it.Nome.rectTransform.offsetMax = new Vector2(-dir, -topo);
            it.Linha.rectTransform.offsetMin = new Vector2(x, -topo - 100f); it.Linha.rectTransform.offsetMax = new Vector2(-dir, -topo - 52f);
            it.Info.rectTransform.offsetMin = new Vector2(x, 16f - altura); it.Info.rectTransform.offsetMax = new Vector2(-dir, -topo - 98f);
            if (acao != null) it.Btn = BotaoDoCartao(c, acao);
            return it;
        }

        /// <summary>Cartao de estacao ou de servico: placa e nome em cima e, embaixo, as 2 linhas de NovaCompra.</summary>
        static RectTransform NovoCartaoCompra(RectTransform lista, string name, string titulo, Sprite icone, Color cor)
        {
            RectTransform c = Art.NovoCartao(lista, name, AltTitulo + 2f * AltCompra + 10f, 290f);
            Art.Fixa(Art.NovaPlaca(c, "Placa", icone, cor, Color.white).rectTransform, Vector2.up, new Vector2(28f, -16f), new Vector2(72f, 72f));
            Text t = Art.Escreve(c, "Titulo", Tipo.Nome, Vector2.up, Vector2.one);
            t.rectTransform.offsetMin = new Vector2(118f, -90f); t.rectTransform.offsetMax = new Vector2(-306f, -14f);
            t.text = titulo;
            return c;
        }

        static Compra NovaCompra(RectTransform cartao, int linha, Sprite icone, int niveis, Action comprar)
        {
            float y = -AltTitulo - linha * AltCompra;
            RectTransform r = Art.Node(cartao, "Compra" + linha, Vector2.up, Vector2.one);
            r.offsetMin = new Vector2(0f, y - AltCompra); r.offsetMax = new Vector2(0f, y);
            Art.Fixa(Art.Img(r, "Icone", icone, Art.Azul, Vector2.up, Vector2.up).rectTransform, new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(60f, 60f));
            var c = new Compra
            {
                Nome = Art.Escreve(r, "Nome", Tipo.Nome, Vector2.up, Vector2.one),
                Efeito = Art.Escreve(r, "Efeito", Tipo.Corpo, Vector2.up, Vector2.one),
                Barra = Art.NovaBarraNivel(r, "Niveis", niveis, Vector2.up, Vector2.one),
                Btn = BotaoDoCartao(r, comprar),
            };
            c.Nome.rectTransform.offsetMin = new Vector2(118f, -62f); c.Nome.rectTransform.offsetMax = new Vector2(-306f, -6f);
            c.Efeito.rectTransform.offsetMin = new Vector2(118f, -116f); c.Efeito.rectTransform.offsetMax = new Vector2(-306f, -62f);
            var barra = (RectTransform)c.Barra.Pai;
            barra.offsetMin = new Vector2(118f, -146f); barra.offsetMax = new Vector2(-306f, -126f);
            return c;
        }

        void BuildTelas(RectTransform canvas)
        {
            BuildMelhorias(canvas);
            BuildMover(canvas);

            _cofre = NovaFolhaModal(canvas, "Cofre", Textos.CofreTitulo, Art.Estrela(), 830f);
            RectTransform pc = _cofre.F.Corpo, topo = Art.Node(pc, "Ganho", Vector2.up, Vector2.one);
            topo.offsetMin = new Vector2(24f, -204f); topo.offsetMax = new Vector2(-24f, -16f);
            _cofreGanho = Art.NovoNumero(topo, "Ganho", Art.Moeda(), Color.white, Textos.Rendeu, Vector2.zero, Vector2.one);
            RectTransform lc = Art.NovaLista(pc, Vector2.zero, Vector2.one);
            Art.Margem((RectTransform)lc.parent, 24f, 186f, 24f, 212f);
            RectTransform tc = Art.NovoCartao(lc, "Texto", 222f);
            _cofreTexto = Art.Escreve(tc, "Texto", Tipo.Corpo, Vector2.zero, Vector2.one, TextAnchor.MiddleLeft);
            Art.Margem(_cofreTexto.rectTransform, 32f, 18f, 32f, 18f);
            _coletar = BotaoDoPe(pc, Vector2.zero, new Vector2(1f, 0f), () => Fecha(_cofre));
            _coletar.Principal(Textos.Coletar, Art.Compra);
            _dobrar = BotaoDoPe(pc, new Vector2(0.5f, 0f), new Vector2(1f, 0f), () => { Fecha(_cofre); _g.VerAnuncio(Anuncio.Cofre); });

            _anuncio = NovaFolhaModal(canvas, "Anuncio", Textos.AnuncioSimulado, Art.Relogio(), 590f);
            RectTransform la = Art.NovaLista(_anuncio.F.Corpo, Vector2.zero, Vector2.one);
            Art.Margem((RectTransform)la.parent, 24f, 186f, 24f, 16f);
            _anuncioItem = NovoItem(la, "Conta", Art.Relogio(), Art.Video, null, 160f, 18f);
            _anuncioBarra = Art.NovaBarraNivel(_anuncioItem.Rt, "Segundos", 3, Vector2.up, Vector2.one);
            var segundos = (RectTransform)_anuncioBarra.Pai;
            segundos.offsetMin = new Vector2(166f, -118f); segundos.offsetMax = new Vector2(-28f, -94f);
            BotaoDoPe(_anuncio.F.Corpo, Vector2.zero, new Vector2(1f, 0f), () => Fecha(_anuncio)).Secundario(Textos.FecharSemRecompensa);

            _pausa = NovaFolhaModal(canvas, "Pausa", Textos.Pausado, Art.Relogio(), 1120f);
            RectTransform lp = NovoResumo(_pausa, Textos.Embarcados, Art.Pessoas(), Art.Azul, out _pausaResumo);
            Art.Margem((RectTransform)lp.parent, 24f, 186f, 24f, 184f);
            ItemPausa(lp, "Tutorial", Art.Mao(), Art.EstacaoCor[0], Textos.ReverTutorial, Textos.TutorialLegenda, Textos.Rever, it => { Fecha(_pausa); ReverTour(); });
            _pausaDiario = ItemPausa(lp, "Diario", Art.Bilhete(), Art.EstacaoCor[3], Textos.ExportarDiario, Textos.DiarioLegenda, Textos.Exportar, it => it.Linha.text = _g.ExportarDiario());
            ItemPausa(lp, "Album", Art.Estrela(), Art.EstacaoCor[1], Textos.Album, Textos.AlbumLegenda, Textos.Abrir, it => { Fecha(_pausa); AbreTela("album"); });
            BotaoDoPe(_pausa.F.Corpo, Vector2.zero, new Vector2(1f, 0f), () => Fecha(_pausa)).Principal(Textos.Continuar, Art.Compra);

            _album = NovaFolhaModal(canvas, "Album", Textos.Album, Art.Estrela(), 560f);
            RectTransform lh = NovoResumo(_album, Textos.HistoriasNome, Art.Estrela(), Art.Coin, out _albumResumo);
            _albumItem = new Item[Textos.Historia.Length];
            for (int h = 0; h < _albumItem.Length; h++)
            {
                _albumItem[h] = NovoItem(lh, "Historia" + h, Art.Estrela(), Art.Vazio, null, 136f, 18f);
                _albumItem[h].Nome.text = Textos.Historia[h];
            }
        }

        void BuildMelhorias(RectTransform canvas)
        {
            _melhorias = NovaFolhaModal(canvas, "Melhorias", Textos.Melhorias, Art.Raio(), 1600f);
            RectTransform lista = NovoResumo(_melhorias, Textos.Compradas, Art.Raio(), Art.Azul, out _melhoriasResumo);
            for (int i = 0; i < 8; i++)
            {
                var e = (Estacao)i;
                _melhoriaTrancada[i] = NovoItem(lista, "Trancada" + i, Art.IconeEstacao(i), Art.Vazio, () => { }, 186f);
                _melhoriaTrancada[i].Nome.text = Textos.Estacao[i];
                if (i == 7) break;   // o despacho nao tem upgrade
                _melhoriaCartao[i] = NovoCartaoCompra(lista, "Estacao" + i, Textos.Estacao[i], Art.IconeEstacao(i), Art.EstacaoCor[i]);
                _melhoriaCompra[i, 0] = NovaCompra(_melhoriaCartao[i], 0, Art.Raio(), S.T.VelNiveis, () => _g.Buy(e, Melhoria.Velocidade));
                _melhoriaCompra[i, 1] = NovaCompra(_melhoriaCartao[i], 1, Art.Pessoas(), S.T.CapNiveis, () => _g.Buy(e, Melhoria.Capacidade));
            }
        }

        void BuildMover(RectTransform canvas)
        {
            _mover = NovaFolhaModal(canvas, "Mover", "", Art.Pessoa(), 1600f);
            RectTransform lista = Art.NovaLista(_mover.F.Corpo, Vector2.zero, Vector2.one);
            Art.Margem((RectTransform)lista.parent, 24f, 0f, 24f, 16f);
            for (int i = 0; i < 8; i++)
            {
                var e = (Estacao)i;
                _moverItem[i] = NovoItem(lista, "Estacao" + i, Art.IconeEstacao(i), Art.Vazio, () => { int q = _moverQuem; Fecha(_mover); _g.Move(q, e); }, 168f, 34f);
                _moverItem[i].Nome.text = Textos.Estacao[i];
            }
        }

        Item ItemPausa(RectTransform lista, string name, Sprite icone, Color cor, string nome, string legenda, string botao, Action<Item> acao)
        {
            Item it = null;
            it = NovoItem(lista, name, icone, cor, () => acao(it), 168f, 34f);
            it.Nome.text = nome; it.Linha.text = legenda;
            it.Btn.Secundario(botao);
            return it;
        }

        void Fecha(Modal m) => m.Rt.gameObject.SetActive(false);

        static void Abre(Modal m)
        {
            m.F.Ajusta();
            m.Rt.gameObject.SetActive(true);
            m.Rt.SetAsLastSibling();
        }

        /// <summary>Abre uma tela pelo nome (botoes e a flag -tela): melhorias, mover, cofre, anuncio, pausa, album. "jogo" = nenhuma.</summary>
        public void AbreTela(string nome)
        {
            switch (nome)
            {
                case "melhorias": Abre(_melhorias); break;
                case "mover": if (S.Staff.Count > 0) AbreMover(0); break;
                case "pausa": _pausaDiario.Linha.text = Textos.DiarioLegenda; Abre(_pausa); break;
                case "album": Abre(_album); break;
                case "anuncio": _g.VerAnuncio(Anuncio.Receita); break;
                case "equipe": Abre(_equipe); break;
                case "tripulacao": Abre(_trip); break;
                case "patio": Abre(_patio); break;
                case "voo": AbreVoo(0); break;
            }
        }

        internal void AbreMover(int quem)
        {
            _moverQuem = quem;
            _mover.Titulo.text = Textos.Mover((int)S.Staff[quem].Perfil);   // quem e a posicao em Staff: Edu pode vir antes da Dani
            Abre(_mover);
        }

        /// <summary>Game: cofre ao abrir ou ao voltar da pausa (so com >= 60 s fora e Cash > 0).</summary>
        public void AbreCofre(long cash, long seg)
        {
            _cofreGanho.Valor.text = "+" + Textos.Num(cash);
            _cofreTexto.text = Textos.Cofre(cash, seg);
            Abre(_cofre);
        }

        /// <summary>Game: anuncio simulado de `a` com o id de transacao `tx` (Pagar so quando acabar).</summary>
        public void AbreAnuncio(Anuncio a, string tx)
        {
            _anuncioQual = a; _anuncioTx = tx; _anuncioFalta = 3f;
            Abre(_anuncio);
        }

        /// <summary>Estacao ou perfil ainda travado pelo marco: placa apagada, quanto falta embarcar e o nivel do terminal no botao.</summary>
        static void PintaTrancada(Item it, int marco, Sim s)
        {
            it.Placa.color = Art.Vazio; it.Nome.color = Art.Fraco;
            it.Linha.text = Textos.FaltamEmbarques(s.T.Marcos[marco - 1] - s.TotalEmbarcados);
            it.Btn.B.gameObject.SetActive(true);
            it.Btn.Travado(Textos.TerminalNome, Textos.NivelNome(marco + 1));
        }

        void PintaMelhorias(Sim s)
        {
            int compradas = 0;
            for (int i = 0; i < 8; i++)
            {
                var e = (Estacao)i;
                bool aberta = s.Desbloqueada(e), comUpgrade = i < 7;
                Item it = _melhoriaTrancada[i];
                it.Rt.gameObject.SetActive(!aberta || !comUpgrade);
                if (comUpgrade) _melhoriaCartao[i].gameObject.SetActive(aberta);
                if (!aberta) PintaTrancada(it, s.T.Abre[i], s);
                else if (!comUpgrade)
                {
                    it.Placa.color = Art.EstacaoCor[i]; it.Nome.color = Art.Tinta;
                    it.Linha.text = Textos.SemMelhoria;
                    it.Btn.B.gameObject.SetActive(false);
                }
                else compradas += PintaEstacao(s, e);
            }
            _melhoriasResumo.Pinta(s, Textos.DeTotal(compradas, 7 * (s.T.VelNiveis + s.T.CapNiveis)));
        }

        /// <summary>Pinta as 2 linhas da estacao aberta; devolve quantos niveis ja foram comprados nela.</summary>
        int PintaEstacao(Sim s, Estacao e)
        {
            int i = (int)e, nv = s.Nivel(e, Melhoria.Velocidade), nc = s.Nivel(e, Melhoria.Capacidade), postos = s.St(e).Postos;
            long cv = s.Custo(e, Melhoria.Velocidade), cc = s.Custo(e, Melhoria.Capacidade);
            float baseSeg = e == Estacao.Manutencao ? s.T.CheckSeg : s.T.Tempo[i];   // na oficina, a Velocidade encurta a checagem
            string seg = Textos.Seg((float)(baseSeg * Math.Pow(s.T.VelFator, nv)));
            if (cv >= 0) seg = Art.DePara(seg, Textos.Seg((float)(baseSeg * Math.Pow(s.T.VelFator, nv + 1))));
            _melhoriaCompra[i, 0].Pinta(Textos.MelhoriaNome(Melhoria.Velocidade, nv, s.T.VelNiveis),
                e == Estacao.Manutencao ? Textos.PorChecagem(seg) : Textos.PorPassageiro(seg), nv, cv, s.Cash);
            _melhoriaCompra[i, 1].Pinta(Textos.MelhoriaNome(Melhoria.Capacidade, nc, s.T.CapNiveis),
                Textos.BalcoesDe(cc >= 0 ? Art.DePara(postos.ToString(), (postos + 1).ToString()) : postos.ToString()), nc, cc, s.Cash);
            return nv + nc;
        }

        void PintaMover(Sim s)
        {
            for (int i = 0; i < 8; i++)
            {
                var e = (Estacao)i;
                Item it = _moverItem[i];
                if (!s.Desbloqueada(e)) { PintaTrancada(it, s.T.Abre[i], s); continue; }
                it.Placa.color = Art.EstacaoCor[i]; it.Nome.color = Art.Tinta;
                it.Linha.text = Textos.NaEstacao(s.Alocados(e));
                if (s.Staff[_moverQuem].Em == e) it.Btn.Maximo(Textos.Aqui); else it.Btn.Secundario(Textos.MoverCurto);
            }
        }

        void PintaTelas()
        {
            Sim s = S;
            if (_melhorias.Rt.gameObject.activeSelf) PintaMelhorias(s);
            if (_mover.Rt.gameObject.activeSelf && _moverQuem >= 0 && _moverQuem < s.Staff.Count) PintaMover(s);
            if (_pausa.Rt.gameObject.activeSelf) _pausaResumo.Pinta(s, Textos.Num(s.TotalEmbarcados));
            if (_cofre.Rt.gameObject.activeSelf)
            {
                bool pode = s.Anuncios.Pode(Anuncio.Cofre);
                _dobrar.B.gameObject.SetActive(pode);
                ((RectTransform)_coletar.B.transform).anchorMax = new Vector2(pode ? 0.5f : 1f, 0f);
                if (pode) _dobrar.Video(Textos.DobrarAnuncio, s.Anuncios.Previa(Anuncio.Cofre));
            }
            if (_anuncio.Rt.gameObject.activeSelf)
            {
                _anuncioFalta -= Time.unscaledDeltaTime;
                int falta = Mathf.CeilToInt(Mathf.Max(0f, _anuncioFalta));
                _anuncioItem.Nome.text = Textos.AnuncioConta(falta);
                _anuncioBarra.Pinta(3 - falta, Art.Video);
                if (_anuncioFalta <= 0f) { Fecha(_anuncio); _g.AnuncioConcluido(_anuncioQual, _anuncioTx); }
            }
            if (_album.Rt.gameObject.activeSelf)
            {
                int entregues = 0;
                for (int h = 0; h < _albumItem.Length; h++)
                {
                    bool tem = (s.Album & (1 << h)) != 0;
                    if (tem) entregues++;
                    _albumItem[h].Placa.color = tem ? Art.Coin : Art.Vazio;
                    _albumItem[h].Glifo.sprite = tem ? Art.Estrela() : Art.Cadeado();
                    _albumItem[h].Linha.text = Textos.AlbumEstado(tem);
                }
                _albumResumo.Pinta(s, Textos.DeTotal(entregues, _albumItem.Length));
            }
            PintaOperacao();
        }

        /// <summary>
        /// So para as fotos (-shot): menor texto, menor alvo de toque e textos cortados da tela aberta, em px da referencia de 1080. Pisos: 32 e
        /// 144. Mede de novo cada texto porque o que esta fora da area visivel da lista nao passa pelo gerador da tela.
        /// </summary>
        internal string PisoTela()
        {
            foreach (Modal m in _modais)
            {
                if (!m.Rt.gameObject.activeSelf) continue;
                float texto = float.MaxValue, alvo = float.MaxValue;
                int cortes = 0;
                string menor = "", cortado = "";
                var g = new TextGenerator();
                foreach (Text t in m.Rt.GetComponentsInChildren<Text>())
                {
                    if (t.text.Length == 0) continue;
                    g.Populate(t.text, t.GetGenerationSettings(t.rectTransform.rect.size));
                    float px = t.resizeTextForBestFit ? g.fontSizeUsedForBestFit / t.pixelsPerUnit : t.fontSize;
                    if (px < texto) { texto = px; menor = t.text; }
                    if (g.characterCountVisible < t.text.Length) { cortes++; cortado = t.text; }
                }
                foreach (Button b in m.Rt.GetComponentsInChildren<Button>())
                {
                    if (b.name == "ToqueFora") continue;
                    Transform hit = b.transform.Find("AreaDeToque");
                    Rect r = ((RectTransform)(hit != null ? hit : b.transform)).rect;
                    alvo = Mathf.Min(alvo, Mathf.Min(r.width, r.height));
                }
                return "PISO texto=" + texto + " alvo=" + alvo + " cortes=" + cortes + " menor='" + menor + "' cortado='" + cortado + "'";
            }
            return "PISO sem tela aberta";
        }
    }
}
