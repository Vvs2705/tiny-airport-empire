using System;
using System.Collections.Generic;
using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// Metas (CONTRATO_FASE_C5 e C6; docs/CAMPANHA.md e docs/MISSOES.md): uma folha com 3 abas, a campanha (as 3 cadeias de 10, por aeroporto),
    /// as missoes (3 diarias e 5 semanais) e as 20 conquistas, cada linha com a condicao, o progresso e a recompensa. No HUD, a faixa da meta da
    /// vez fica logo acima da barra de baixo (toque = abre a folha), com o selo vermelho do que ha para coletar; concluir avisa pelo toast.
    /// O nucleo ja paga ao concluir (Cash fixo, fora do Ganha): o Coletar so da baixa no que o jogador ainda nao viu, como o Coletar do cofre.
    /// So le o Sim.
    /// </summary>
    public sealed partial class Hud
    {
        const float AltMeta = 220f, AltParte = 132f, AltFaixaMeta = 144f, LargFaixaMeta = 720f;
        /// <summary>Largura da condicao no cartao (lista de 1032 px: placa a esquerda, canhoto a direita) e do nome na faixa; o teste confere que cabe.</summary>
        public const float LargCondicao = 574f, LargNomeFaixa = 394f;
        /// <summary>Cor de cada aeroporto, na ordem de Tabelas.Aeroportos: verde do vale, azul da costa, roxo do hub.</summary>
        static readonly Color[] AeroCor = { Art.EstacaoCor[2], Art.EstacaoCor[4], Art.EstacaoCor[1] };

        sealed class Parte { public Text Condicao, Conta; public RectTransform Trilho; public Image Barra; }

        /// <summary>Cartao de meta: placa, nome, 1 ou 2 partes (condicao, barra e conta) e, no canhoto, a recompensa.</summary>
        sealed class CartaoMeta
        {
            public RectTransform Rt;
            public Image Placa, Glifo, Destaque;
            public Text Nome;
            public Parte[] Partes;
            public BotaoPreco Btn;
        }

        Modal _metas;
        int _metasAba;
        readonly Button[] _abaBtn = new Button[3];
        readonly RectTransform[] _abaLista = new RectTransform[3];
        CartaoMeta[][] _objetivo;
        Text[] _aeroConta;
        BarraNivel[] _aeroBarra;
        readonly CartaoMeta[] _diaria = new CartaoMeta[3], _semanal = new CartaoMeta[5];
        CartaoMeta[] _conquista;
        Text _diariasNovas, _semanaisNovas, _conquistasConta;
        Image _abaSelo;
        Text _abaSeloN;
        /// <summary>Missoes concluidas nesta sessao que o jogador ainda nao coletou (o Cash ja entrou).</summary>
        readonly HashSet<Missao> _aColetar = new HashSet<Missao>();

        RectTransform _faixaMeta;
        Image _faixaPlaca, _faixaGlifo, _faixaBarra, _faixaMoeda, _seloHud;
        Text _faixaNome, _faixaConta, _faixaPremio, _seloHudN;

        static Text EscreveMeta(Transform pai, string nome, Tipo tipo, TextAnchor alinha = TextAnchor.MiddleLeft)
        {
            Text t = Art.Escreve(pai, nome, tipo, Vector2.up, Vector2.one, alinha);
            t.resizeTextMinSize = MenorTexto;
            return t;
        }

        /// <summary>Esticado na largura do pai, preso ao topo: margens esquerda e direita, distancia do topo e altura, em px.</summary>
        static void NaFaixa(RectTransform rt, float esq, float topo, float dir, float alt)
        {
            rt.anchorMin = Vector2.up; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(esq, -topo - alt); rt.offsetMax = new Vector2(-dir, -topo);
        }

        /// <summary>Selo vermelho com a conta, no canto de cima a direita do pai.</summary>
        static Image NovoSelo(Transform pai, out Text n)
        {
            Image aro = Art.Img(pai, "Selo", Art.Disc(), Color.white, Vector2.one, Vector2.one);
            Art.Fixa(aro.rectTransform, Vector2.one, new Vector2(-18f, -18f), new Vector2(76f, 76f));
            aro.rectTransform.pivot = new Vector2(0.5f, 0.5f);   // centro no canto do pai: pulsa sem sair do lugar
            Image d = Art.Img(aro.transform, "Fundo", Art.Disc(), Art.Bad, new Vector2(0.09f, 0.09f), new Vector2(0.91f, 0.91f));
            n = Art.Escreve(d.transform, "Conta", Tipo.Valor, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            n.color = Color.white; n.resizeTextMinSize = MenorTexto;
            return aro;
        }

        static Parte NovaParte(RectTransform c, int p)
        {
            float y = 74f + p * AltParte;
            var pt = new Parte { Condicao = EscreveMeta(c, "Condicao" + p, Tipo.Legenda, TextAnchor.UpperLeft), Conta = EscreveMeta(c, "Conta" + p, Tipo.Legenda, TextAnchor.MiddleRight) };
            pt.Condicao.color = Art.Fraco;
            NaFaixa(pt.Condicao.rectTransform, 152f, y, 306f, 90f);   // 2 linhas de 32 px
            pt.Trilho = Art.Panel(c, "Trilho" + p, Art.Vazio, Vector2.up, Vector2.one).rectTransform;
            NaFaixa(pt.Trilho, 152f, y + 98f, 306f + 196f, 20f);
            pt.Barra = Art.Panel(pt.Trilho, "Barra", Art.Coin, Vector2.zero, Vector2.one);
            Art.Fixa(pt.Conta.rectTransform, Vector2.one, new Vector2(-306f, -y - 86f), new Vector2(184f, 44f));
            pt.Conta.color = Art.Tinta;
            return pt;
        }

        static CartaoMeta NovoCartaoMeta(RectTransform lista, string name, int partes, Action acao)
        {
            RectTransform c = Art.NovoCartao(lista, name, AltMeta + (partes - 1) * AltParte, 290f);
            var m = new CartaoMeta { Rt = c, Partes = new Parte[partes], Placa = Art.NovaPlaca(c, "Placa", Art.Estrela(), Art.Vazio, Color.white) };
            Art.Fixa(m.Placa.rectTransform, Vector2.up, new Vector2(28f, -22f), new Vector2(104f, 104f));
            m.Glifo = m.Placa.transform.GetChild(0).GetComponent<Image>();
            m.Nome = EscreveMeta(c, "Nome", Tipo.Nome);
            NaFaixa(m.Nome.rectTransform, 152f, 14f, 306f, 56f);
            for (int p = 0; p < partes; p++) m.Partes[p] = NovaParte(c, p);
            m.Btn = BotaoDoCartao(c, acao);
            m.Destaque = Art.Img(c, "Destaque", Art.Contorno(), Art.Placa, Vector2.zero, Vector2.one);
            m.Destaque.type = Image.Type.Sliced; m.Destaque.preserveAspect = false;
            return m;
        }

        /// <summary>Titulo de secao sobre o fundo da folha, com o numero a direita.</summary>
        static Text NovoCabecalho(RectTransform lista, string titulo, float altura)
        {
            RectTransform h = Art.Node(lista, "Cabecalho", Vector2.zero, Vector2.one);
            LayoutElement e = h.gameObject.AddComponent<LayoutElement>();
            e.minHeight = e.preferredHeight = altura;
            Text t = EscreveMeta(h, "Titulo", Tipo.Secao);
            NaFaixa(t.rectTransform, 8f, 18f, 8f, 60f);
            t.text = titulo; t.color = Art.Azul;
            Text d = EscreveMeta(h, "Conta", Tipo.Legenda, TextAnchor.MiddleRight);
            NaFaixa(d.rectTransform, 8f, 18f, 8f, 60f);
            return d;
        }

        void BuildMetas(RectTransform canvas)
        {
            _metas = NovaFolhaModal(canvas, "Metas", Textos.Metas, Art.Estrela(), 1600f);
            RectTransform corpo = _metas.F.Corpo;
            string[] abas = { Textos.Campanha, Textos.Missoes, Textos.Conquistas };
            for (int a = 0; a < 3; a++)
            {
                int aa = a;
                _abaBtn[a] = Art.NewButton(corpo, abas[a], 40, Art.Suave, new Vector2(a / 3f, 1f), new Vector2((a + 1) / 3f, 1f), () => MostraAba(aa));
                var r = (RectTransform)_abaBtn[a].transform;
                r.offsetMin = new Vector2(a == 0 ? 24f : 6f, -160f); r.offsetMax = new Vector2(a == 2 ? -24f : -6f, -16f);
                Art.Rotulo(_abaBtn[a]).resizeTextMinSize = MenorTexto;
                _abaLista[a] = Art.NovaLista(corpo, Vector2.zero, Vector2.one);
                Art.Margem((RectTransform)_abaLista[a].parent, 24f, 16f, 24f, 176f);
            }
            _abaSelo = NovoSelo(_abaBtn[1].transform, out _abaSeloN);
            _abaSelo.gameObject.SetActive(false);

            Objetivo[][] cadeias = S.T.Objetivos;
            _objetivo = new CartaoMeta[cadeias.Length][];
            _aeroConta = new Text[cadeias.Length];
            _aeroBarra = new BarraNivel[cadeias.Length];
            for (int a = 0; a < cadeias.Length; a++)
            {
                _aeroConta[a] = NovoCabecalho(_abaLista[0], Textos.AeroportoNome[a % Textos.AeroportoNome.Length].ToUpperInvariant(), 124f);
                _aeroBarra[a] = Art.NovaBarraNivel(_aeroConta[a].transform.parent, "Feitos", cadeias[a].Length, Vector2.up, Vector2.one);
                NaFaixa((RectTransform)_aeroBarra[a].Pai, 8f, 88f, 2f, 18f);
                _objetivo[a] = new CartaoMeta[cadeias[a].Length];
                for (int i = 0; i < cadeias[a].Length; i++) _objetivo[a][i] = NovoCartaoMeta(_abaLista[0], "Objetivo" + a + "." + i, 1, () => { });
            }

            _diariasNovas = NovoCabecalho(_abaLista[1], Textos.Diarias, 96f);
            for (int v = 0; v < _diaria.Length; v++) { int vv = v; _diaria[v] = NovoCartaoMeta(_abaLista[1], "Diaria" + v, 1, () => _aColetar.Remove(S.Diarias[vv])); }
            _semanaisNovas = NovoCabecalho(_abaLista[1], Textos.Semanais, 96f);
            for (int v = 0; v < _semanal.Length; v++) { int vv = v; _semanal[v] = NovoCartaoMeta(_abaLista[1], "Semanal" + v, 2, () => _aColetar.Remove(S.Semanais[vv])); }

            _conquistasConta = NovoCabecalho(_abaLista[2], Textos.Conquistas.ToUpperInvariant(), 96f);
            _conquista = new CartaoMeta[S.T.Conquistas.Length];
            for (int i = 0; i < _conquista.Length; i++) _conquista[i] = NovoCartaoMeta(_abaLista[2], "Conquista" + i, 1, () => { });

            BuildFaixaMeta();
            BuildPasse(canvas);   // o atalho do passe fica ao lado da faixa (Hud.Passe.cs)
        }

        /// <summary>A meta da vez, logo acima da barra de baixo: placa, nome, recompensa e barra. Toque abre as Metas.</summary>
        void BuildFaixaMeta()
        {
            _faixaMeta = Art.NovoCartao(_root, "Meta", AltFaixaMeta);
            Art.Fixa(_faixaMeta, Vector2.zero, new Vector2(16f, AltBase + 14f), new Vector2(LargFaixaMeta, AltFaixaMeta));
            Image fundo = _faixaMeta.GetComponent<Image>();
            fundo.raycastTarget = true;   // o toque na faixa nao vai para o mundo
            Button b = _faixaMeta.gameObject.AddComponent<Button>();
            b.targetGraphic = fundo;
            b.onClick.AddListener(() => AbreTela(AColetar(S) > 0 || S.ObjetivoAtual(S.AeroportoAtual) == null ? "missoes" : "metas"));
            _faixaMeta.gameObject.AddComponent<Aperta>();
            _faixaPlaca = Art.NovaPlaca(_faixaMeta, "Placa", Art.Estrela(), Art.Azul, Color.white);
            Art.Fixa(_faixaPlaca.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(104f, 104f));
            _faixaGlifo = _faixaPlaca.transform.GetChild(0).GetComponent<Image>();
            _faixaNome = EscreveMeta(_faixaMeta, "Nome", Tipo.Nome);
            NaFaixa(_faixaNome.rectTransform, 136f, 12f, 190f, 56f);
            _faixaMoeda = Art.Img(_faixaMeta, "Moeda", Art.Moeda(), Color.white, Vector2.one, Vector2.one);
            Art.Fixa(_faixaMoeda.rectTransform, Vector2.one, new Vector2(-138f, -18f), new Vector2(44f, 44f));
            _faixaPremio = EscreveMeta(_faixaMeta, "Premio", Tipo.Valor);
            Art.Fixa(_faixaPremio.rectTransform, Vector2.one, new Vector2(-16f, -12f), new Vector2(118f, 56f));
            RectTransform trilho = Art.Panel(_faixaMeta, "Trilho", Art.Vazio, Vector2.up, Vector2.one).rectTransform;
            NaFaixa(trilho, 136f, 92f, 224f, 20f);
            _faixaBarra = Art.Panel(trilho, "Barra", Art.Coin, Vector2.zero, Vector2.one);
            _faixaConta = EscreveMeta(_faixaMeta, "Conta", Tipo.Legenda, TextAnchor.MiddleRight);
            Art.Fixa(_faixaConta.rectTransform, Vector2.one, new Vector2(-20f, -76f), new Vector2(196f, 52f));
            _faixaConta.color = Art.Tinta;
            _seloHud = NovoSelo(_faixaMeta, out _seloHudN);
        }

        void MostraAba(int aba)
        {
            _metasAba = aba;
            for (int a = 0; a < 3; a++)
            {
                _abaLista[a].parent.gameObject.SetActive(a == aba);
                ((Image)_abaBtn[a].targetGraphic).color = a == aba ? Art.Azul : Art.Suave;
                Text t = Art.Rotulo(_abaBtn[a]);
                t.color = a == aba ? Color.white : Art.Azul;
                t.GetComponent<Outline>().enabled = a == aba;
            }
            PintaMetasTela(S);
            if (aba == 0) RolaParaAtivo();
        }

        /// <summary>-tela e a faixa: metas (campanha), missoes, conquistas.</summary>
        void AbreMetas(string nome)
        {
            Abre(_metas);
            MostraAba(nome == "missoes" ? 1 : nome == "conquistas" ? 2 : 0);
        }

        /// <summary>A lista da campanha abre no objetivo ativo do aeroporto em que o jogador esta.</summary>
        void RolaParaAtivo()
        {
            Sim s = S;
            CartaoMeta[] cadeia = _objetivo[Mathf.Clamp(s.AeroportoAtual, 0, _objetivo.Length - 1)];
            RectTransform alvo = cadeia[Mathf.Min(s.ObjetivosFeitos(s.AeroportoAtual), cadeia.Length - 1)].Rt, conteudo = _abaLista[0], vista = (RectTransform)conteudo.parent;
            LayoutRebuilder.ForceRebuildLayoutImmediate(conteudo);
            float topo = -alvo.anchoredPosition.y - alvo.rect.height * (1f - alvo.pivot.y);
            conteudo.anchoredPosition = new Vector2(0f, Mathf.Clamp(topo - 140f, 0f, Mathf.Max(0f, conteudo.rect.height - vista.rect.height)));
        }

        // ------------------------------------------------------------------ eventos (Game.Trata)

        public void ObjetivoFeito(int aeroporto, int indice, int cash) => Toast(Textos.ObjetivoFeito(Textos.NomeMeta(S.T.Objetivos[aeroporto][indice]), cash));

        public void MissaoFeita(int tipo, int indice, int cash)
        {
            Missao[] vagas = tipo == 0 ? S.Diarias : S.Semanais;
            _aColetar.RemoveWhere(m => Array.IndexOf(S.Diarias, m) < 0 && Array.IndexOf(S.Semanais, m) < 0);   // as da lista que ja virou
            foreach (Missao m in vagas) if (m != null && m.Feita && m.Indice == indice) _aColetar.Add(m);
            Toast(Textos.MissaoFeita(Textos.NomeMeta((tipo == 0 ? S.T.Diarias : S.T.Semanais)[indice]), cash));
        }

        public void ConquistaNova(int indice) => Toast(Textos.ConquistaNova(Textos.NomeMeta(S.T.Conquistas[indice])));

        // ------------------------------------------------------------------ quadro a quadro

        int AColetar(Sim s)
        {
            int n = 0;
            foreach (Missao m in s.Diarias) if (m != null && _aColetar.Contains(m)) n++;
            foreach (Missao m in s.Semanais) if (m != null && _aColetar.Contains(m)) n++;
            return n;
        }

        /// <summary>Ondas ate a lista virar, num periodo de `ondas` ondas (o dia do jogo ou a semana).</summary>
        static int OndasAte(Sim s, int ondas)
        {
            long p = (long)s.OndaPassos * ondas;
            return (int)((p - s.Steps % p + s.OndaPassos - 1) / s.OndaPassos);
        }

        void PintaMetas(Sim s)
        {
            int coletar = AColetar(s);
            PintaFaixaMeta(s, coletar);
            PintaPasse(s);
            if (!_metas.Rt.gameObject.activeSelf) return;
            _abaSelo.gameObject.SetActive(coletar > 0);
            _abaSeloN.text = coletar.ToString();
            PintaMetasTela(s);
        }

        void PintaFaixaMeta(Sim s, int coletar)
        {
            bool mostra = !TourAberto && !Arrastando;   // no arrasto, a faixa nao tapa a estacao de destino
            _faixaMeta.gameObject.SetActive(mostra);
            if (!mostra) return;
            int a = s.AeroportoAtual, conta = 0;
            Objetivo o = s.ObjetivoAtual(a);
            Color cor = AeroCor[Mathf.Clamp(a, 0, AeroCor.Length - 1)];
            if (o != null) conta = s.Progresso(a);
            else
            {
                cor = Art.Video;
                foreach (Missao m in s.Diarias) if (m != null && !m.Feita) { o = m.O; conta = m.Conta; break; }
            }
            _faixaPlaca.color = o != null ? cor : Art.Compra;
            _faixaGlifo.sprite = o != null ? IconeMeta(o) : Art.Check();
            _faixaNome.text = o != null ? Textos.NomeMeta(o) : Textos.Metas;
            _faixaMoeda.gameObject.SetActive(o != null);
            _faixaPremio.text = o != null ? "+" + Textos.Num(o.Recompensa) : "";
            _faixaBarra.transform.parent.gameObject.SetActive(o != null);
            if (o != null) Barra(_faixaBarra, conta, o.Alvo);
            _faixaConta.text = o != null ? Textos.Conta(Math.Min(conta, o.Alvo), o.Alvo) : Textos.NovasEm(OndasAte(s, s.T.DiaOndas));
            _seloHud.gameObject.SetActive(coletar > 0);
            _seloHudN.text = coletar.ToString();
            _seloHud.rectTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f));   // pulsa: tem coisa para pegar
        }

        static void Barra(Image barra, int conta, int alvo)
        {
            float f = alvo > 0 ? Mathf.Clamp01((float)conta / alvo) : 1f;
            barra.rectTransform.anchorMax = new Vector2(f, 1f);
            barra.color = f >= 1f ? Art.Compra : Art.Coin;
            barra.enabled = f > 0f;
        }

        void PintaMetasTela(Sim s)
        {
            if (_metasAba == 0) PintaCampanha(s);
            else if (_metasAba == 1)
            {
                _diariasNovas.text = Textos.NovasEm(OndasAte(s, s.T.DiaOndas));
                _semanaisNovas.text = Textos.NovasEm(OndasAte(s, s.T.DiaOndas * Sim.SemanaDias));
                for (int v = 0; v < _diaria.Length; v++) PintaMissao(_diaria[v], s.Diarias[v], Art.Video);
                for (int v = 0; v < _semanal.Length; v++) PintaMissao(_semanal[v], s.Semanais[v], Art.EstacaoCor[3]);
            }
            else PintaConquistas(s);
        }

        void PintaCampanha(Sim s)
        {
            for (int a = 0; a < _objetivo.Length; a++)
            {
                Objetivo[] cadeia = s.T.Objetivos[a];
                int feitos = s.ObjetivosFeitos(a);
                _aeroConta[a].text = Textos.DeTotal(feitos, cadeia.Length);
                _aeroBarra[a].Pinta(feitos, AeroCor[a % AeroCor.Length]);
                for (int i = 0; i < cadeia.Length; i++)
                {
                    CartaoMeta c = _objetivo[a][i];
                    Objetivo o = cadeia[i];
                    bool feito = i < feitos, ativo = i == feitos;
                    PintaPlaca(c, feito ? Art.Check() : ativo ? IconeMeta(o) : Art.Cadeado(), feito ? Art.Compra : ativo ? AeroCor[a % AeroCor.Length] : Art.Vazio);
                    c.Nome.text = Textos.NomeMeta(o);
                    c.Nome.color = feito || ativo ? Art.Tinta : Art.Fraco;
                    c.Destaque.enabled = ativo;
                    PintaParte(c.Partes[0], o, feito ? o.Alvo : ativo ? s.Progresso(a) : -1);
                    if (feito) c.Btn.Maximo(Textos.Feito); else Premio(c.Btn, o.Recompensa);
                }
            }
        }

        void PintaMissao(CartaoMeta c, Missao m, Color cor)
        {
            c.Rt.gameObject.SetActive(m != null);
            if (m == null) return;
            PintaPlaca(c, m.Feita ? Art.Check() : IconeMeta(m.O), m.Feita ? Art.Compra : cor);
            c.Destaque.enabled = _aColetar.Contains(m);
            c.Nome.text = Textos.NomeMeta(m.O);
            PintaParte(c.Partes[0], m.O, m.Conta);
            if (c.Partes.Length > 1) PintaParte(c.Partes[1], m.O.E, m.ContaE);
            if (!m.Feita) Premio(c.Btn, m.O.Recompensa);
            else if (_aColetar.Contains(m)) { c.Btn.Video(Textos.Coletar, m.O.Recompensa); c.Btn.Fundo.color = Art.Compra; }
            else c.Btn.Maximo(Textos.Feita);
        }

        void PintaConquistas(Sim s)
        {
            _conquistasConta.text = Textos.DeTotal(s.ConquistasFeitas, _conquista.Length);
            for (int i = 0; i < _conquista.Length; i++)
            {
                CartaoMeta c = _conquista[i];
                Objetivo o = s.T.Conquistas[i];
                bool feita = s.ConquistaFeita(i);
                c.Destaque.enabled = false;
                PintaPlaca(c, feita ? Art.Estrela() : IconeMeta(o), feita ? Art.Coin : Art.AzulLuz);
                c.Nome.text = Textos.NomeMeta(o);
                PintaParte(c.Partes[0], o, s.ProgressoConquista(i));
                if (feita) c.Btn.Maximo(Textos.Feita);
                else Premio(c.Btn, -1);
                c.Btn.Icone.gameObject.SetActive(true);   // a recompensa da conquista e o selo: estrela apagada ate fechar
                c.Btn.Icone.sprite = Art.Estrela(); c.Btn.Icone.color = feita ? Art.Coin : Art.Vazio;
            }
        }

        static void PintaPlaca(CartaoMeta c, Sprite glifo, Color cor)
        {
            c.Placa.color = cor;
            c.Glifo.sprite = glifo;
        }

        /// <summary>Condicao, barra e conta. conta &lt; 0 = ainda nao e a vez: so a condicao. Sem objetivo (semanal de uma parte so), some.</summary>
        static void PintaParte(Parte p, Objetivo o, int conta)
        {
            p.Condicao.text = o != null ? Textos.MetaTexto(o) : "";
            bool barra = o != null && conta >= 0;
            p.Trilho.gameObject.SetActive(barra);
            p.Conta.gameObject.SetActive(barra);
            if (!barra) return;
            Barra(p.Barra, conta, o.Alvo);
            p.Conta.text = Textos.Conta(Math.Min(conta, o.Alvo), o.Alvo);
        }

        /// <summary>A recompensa que ainda nao saiu: cinza, sem toque. cash &lt; 0 = conquista (o selo, sem Cash).</summary>
        static void Premio(BotaoPreco b, long cash)
        {
            b.Video(cash < 0 ? Textos.Selo : Textos.Premio, Math.Max(0, cash));
            if (cash < 0) b.Valor.text = "";
            b.Fundo.color = Art.Suave;
            b.Rotulo.color = Art.Fraco; b.Valor.color = Art.Tinta;
            b.ContornoRotulo.enabled = b.ContornoValor.enabled = false;
            b.B.interactable = false;
        }

        /// <summary>O icone do que a meta pede, com os da barra de baixo e das estacoes.</summary>
        static Sprite IconeMeta(Objetivo o)
        {
            switch (o.Meta)
            {
                case Meta.Equipe: case Meta.Capacidade: case Meta.Embarcados: case Meta.Passageiros: return Art.Pessoas();
                case Meta.Movidos: return Art.Mao();
                case Meta.Velocidade: case Meta.Melhorias: return Art.Raio();
                case Meta.Historias: return Art.Heart();
                case Meta.Atendidos: return Art.IconeEstacao(o.Arg);
                case Meta.Climas: return Art.Gota();
                case Meta.Incidentes: return Art.Alerta();
                case Meta.Pontuais: return Art.Relogio();
                case Meta.Checagens: case Meta.Hangar: return Art.Chave();
                case Meta.Marco: return Art.Estrela();
                case Meta.Rotas: return Art.Bilhete();
                case Meta.Campanha: case Meta.Missoes: return Art.Check();
                default: return Art.Aviao();   // Cheios, Voos, Seguidos, Chegou
            }
        }
    }
}
