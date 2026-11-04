using System;
using System.Collections.Generic;
using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// Passe de temporada (CONTRATO_FASE_C7, docs/PASSE.md): uma folha com o resumo (nivel, pontos que faltam e dias do jogo), as regras (de onde
    /// vem o ponto, quando o premio entra, nada expira), a trilha premium e os 30 niveis, cada um com o premio gratis e, no canhoto, o lugar do
    /// premium. O nucleo paga o nivel sozinho no passo em que fecha (D-C7b): o Coletar so da baixa no que o jogador ainda nao viu, como nas
    /// missoes. A trilha premium aparece trancada ate a loja existir, sem preco e sem botao de compra; com a posse do produto da temporada (save
    /// de teste) ela aparece liberada. O atalho fica a direita da faixa da meta, com o selo do que ha para coletar. So le o Sim.
    /// </summary>
    public sealed partial class Hud
    {
        public enum EstadoNivel { Trancado, Proximo, Coletar, Recebido }

        const float AltNivel = 176f, CanhotoPremium = 300f, AltRegra = 84f, AltPremiumCartao = 228f;
        /// <summary>Largura do premio e do estado no cartao do nivel (lista de 1032 px: disco e placa a esquerda, canhoto premium a direita), de
        /// cada regra, do texto do canhoto e do nivel no atalho; o teste confere que cabe.</summary>
        public const float LargPremio = 468f, LargRegra = 896f, LargCanhoto = 260f, LargNivelAtalho = 188f;

        sealed class CartaoNivel
        {
            public RectTransform Rt;
            public Image Disco, Placa, Glifo, PremiumPlaca, PremiumGlifo, Destaque;
            public Text Num, Premio, Estado, PremiumTexto;
            public Button Coletar;
        }

        Modal _passe;
        CartaoNivel[] _nivel;
        Numero _passeNivel, _passePontos, _passeDias;
        Image _passeBarra, _premiumPlaca, _premiumGlifo;
        Text _passeConta, _premiumNome, _premiumLinha, _premiumInfo, _passeFechado;
        RectTransform _passeLista, _premiumCartao, _passeColunas;
        Text[] _regra;
        RectTransform _regrasLinhas;
        /// <summary>Niveis pagos nesta sessao que o jogador ainda nao viu (o Cash, o 2x e o cosmetico ja entraram). Temporada nova limpa.</summary>
        readonly HashSet<int> _passeAColetar = new HashSet<int>();
        int _passeTemp = -1;

        RectTransform _passeAtalho;
        Image _atalhoBarra, _seloPasse;
        Text _atalhoNivel, _seloPasseN;

        /// <summary>O nivel n (1..30) do passe: alcancado e ainda nao visto = Coletar; o seguinte ao alcancado = Proximo.</summary>
        public static EstadoNivel EstadoDoNivel(Sim s, int n, ICollection<int> aColetar) =>
            n <= s.NivelPasse ? (aColetar.Contains(n) ? EstadoNivel.Coletar : EstadoNivel.Recebido) : n == s.NivelPasse + 1 ? EstadoNivel.Proximo : EstadoNivel.Trancado;

        /// <summary>A trilha premium so abre com a posse do produto da temporada. A loja vem depois: aqui nada se vende.</summary>
        public static bool PremiumAberto(Sim s) => s.Temporada is Temporada t && s.Tem(t.Produto);

        /// <summary>Glifo, fundo e cor do glifo do premio. Os 3 cosmeticos ainda nao tem arte: a placa mostra o que eles sao.</summary>
        static (Sprite glifo, Color fundo, Color cor) IconePremio(Nivel n)
        {
            if (n.Premio == TA.Core.Premio.Cash) return (Art.Moeda(), Art.Suave, Color.white);   // Premio aqui e' o metodo da tela de metas
            if (n.Premio == TA.Core.Premio.Tempo) return (Art.Relogio(), Art.Video, Color.white);
            switch (n.Id)
            {
                case "terminal_decor_flags_s1": return (Art.Triangle(), Art.EstacaoCor[5], Art.Placa);   // bandeirola: o triangulo de ponta para baixo
                case "aircraft_skin_sunset_s1": return (Art.Aviao(), Art.Azul, Art.VooCor[1]);
                case "aircraft_skin_gold_s1": return (Art.Aviao(), Art.Azul, Art.Gold);
                default: return (Art.Estrela(), Art.Azul, Art.Placa);
            }
        }

        void BuildPasse(RectTransform canvas)
        {
            _passe = NovaFolhaModal(canvas, "Passe", Textos.PasseTitulo, Art.Estrela(), 1600f);
            RectTransform corpo = _passe.F.Corpo, nums = Art.Node(corpo, "Numeros", Vector2.up, Vector2.one);
            nums.offsetMin = new Vector2(24f, -172f); nums.offsetMax = new Vector2(-24f, -16f);
            _passeNivel = Art.NovoNumero(nums, "Nivel", Art.Estrela(), Art.Coin, Textos.NivelRotulo, Vector2.zero, new Vector2(0.34f, 1f));
            _passePontos = Art.NovoNumero(nums, "Pontos", Art.Raio(), Art.Compra, "", new Vector2(0.34f, 0f), new Vector2(0.67f, 1f));
            _passeDias = Art.NovoNumero(nums, "Dias", Art.Relogio(), Art.Azul, Textos.DiasRotulo, new Vector2(0.67f, 0f), Vector2.one);
            RectTransform trilho = Art.Panel(corpo, "Trilho", Art.Vazio, Vector2.up, Vector2.one).rectTransform;
            NaFaixa(trilho, 32f, 196f, 256f, 24f);
            _passeBarra = Art.Panel(trilho, "Barra", Art.Coin, Vector2.zero, Vector2.one);
            _passeConta = EscreveMeta(corpo, "Conta", Tipo.Legenda, TextAnchor.MiddleRight);
            Art.Fixa(_passeConta.rectTransform, Vector2.one, new Vector2(-24f, -182f), new Vector2(216f, 52f));
            _passeConta.color = Art.Tinta;

            _passeLista = Art.NovaLista(corpo, Vector2.zero, Vector2.one);
            Art.Margem((RectTransform)_passeLista.parent, 24f, 16f, 24f, 240f);
            BuildRegras();
            BuildPremium();

            _passeColunas = Art.Node(_passeLista, "Colunas", Vector2.zero, Vector2.one);
            LayoutElement e = _passeColunas.gameObject.AddComponent<LayoutElement>();
            e.minHeight = e.preferredHeight = 76f;
            Text gratis = EscreveMeta(_passeColunas, "Gratis", Tipo.Secao);
            NaFaixa(gratis.rectTransform, 132f, 14f, CanhotoPremium, 60f);
            gratis.text = Textos.Gratis; gratis.color = Art.Azul;
            Text premium = Art.Escreve(_passeColunas, "Premium", Tipo.Secao, Vector2.one, Vector2.one, TextAnchor.MiddleCenter);
            Art.Fixa(premium.rectTransform, Vector2.one, new Vector2(0f, -14f), new Vector2(CanhotoPremium, 60f));
            premium.text = Textos.Premium; premium.color = Art.Azul; premium.resizeTextMinSize = MenorTexto;   // o dourado fica na placa: no fundo claro ele nao le

            int niveis = 0;
            foreach (Temporada t in S.T.Temporadas) niveis = Math.Max(niveis, t.Gratis.Length);
            _nivel = new CartaoNivel[niveis];
            for (int n = 1; n <= niveis; n++) _nivel[n - 1] = NovoCartaoNivel(n);
            BuildPasseAtalho();
        }

        /// <summary>Como funciona: tres linhas com icone, os numeros saem do Tuning e da temporada.</summary>
        void BuildRegras()
        {
            RectTransform c = Art.NovoCartao(_passeLista, "Regras", 84f + 3 * AltRegra + 12f);
            Text titulo = EscreveMeta(c, "Titulo", Tipo.Nome);
            NaFaixa(titulo.rectTransform, 28f, 16f, 28f, 56f);
            titulo.text = Textos.ComoFunciona;
            _regrasLinhas = Art.Node(c, "Linhas", Vector2.zero, Vector2.one);
            Sprite[] icone = { Art.Raio(), Art.Check(), Art.Relogio() };
            Color[] cor = { Art.Compra, Art.Video, Art.Azul };
            _regra = new Text[icone.Length];
            for (int i = 0; i < icone.Length; i++)
            {
                Image p = Art.NovaPlaca(_regrasLinhas, "Placa" + i, icone[i], cor[i], Color.white);
                Art.Fixa(p.rectTransform, Vector2.up, new Vector2(28f, -86f - i * AltRegra), new Vector2(64f, 64f));
                _regra[i] = EscreveMeta(_regrasLinhas, "Regra" + i, Tipo.Legenda, TextAnchor.MiddleLeft);
                NaFaixa(_regra[i].rectTransform, 108f, 78f + i * AltRegra, 28f, 80f);
                _regra[i].color = Art.Tinta;
            }
            _passeFechado = EscreveMeta(c, "Fechado", Tipo.Legenda, TextAnchor.UpperLeft);
            NaFaixa(_passeFechado.rectTransform, 28f, 78f, 28f, 3 * AltRegra);
            _passeFechado.text = Textos.PasseFechado; _passeFechado.color = Art.Tinta;
        }

        /// <summary>A trilha premium: trancada e com o porque, ou liberada pela posse. Sem preco e sem botao de compra.</summary>
        void BuildPremium()
        {
            _premiumCartao = Art.NovoCartao(_passeLista, "Premium", AltPremiumCartao);
            _premiumPlaca = Art.NovaPlaca(_premiumCartao, "Placa", Art.Cadeado(), Art.Apagado, Art.Fraco);
            Art.Fixa(_premiumPlaca.rectTransform, Vector2.up, new Vector2(28f, -22f), new Vector2(104f, 104f));
            _premiumGlifo = _premiumPlaca.transform.GetChild(0).GetComponent<Image>();
            _premiumNome = EscreveMeta(_premiumCartao, "Nome", Tipo.Nome);
            NaFaixa(_premiumNome.rectTransform, 152f, 16f, 28f, 56f);
            _premiumLinha = EscreveMeta(_premiumCartao, "Linha", Tipo.Legenda, TextAnchor.UpperLeft);
            NaFaixa(_premiumLinha.rectTransform, 152f, 76f, 28f, 88f);
            _premiumLinha.color = Art.Tinta;
            _premiumInfo = EscreveMeta(_premiumCartao, "Info", Tipo.Legenda);
            NaFaixa(_premiumInfo.rectTransform, 152f, 166f, 28f, 44f);
            _premiumInfo.text = Textos.PremiumNaoPrecisa;
        }

        CartaoNivel NovoCartaoNivel(int n)
        {
            var c = new CartaoNivel { Rt = Art.NovoCartao(_passeLista, "Nivel" + n, AltNivel, CanhotoPremium) };
            c.Disco = Art.Img(c.Rt, "Disco", Art.Disc(), Art.Vazio, Vector2.up, Vector2.up);
            Art.Fixa(c.Disco.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(96f, 96f));
            c.Num = Art.Escreve(c.Disco.transform, "Numero", Tipo.Valor, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            c.Num.text = n.ToString(); c.Num.resizeTextMinSize = MenorTexto;
            c.Placa = Art.NovaPlaca(c.Rt, "Placa", Art.Moeda(), Art.Suave, Color.white);
            Art.Fixa(c.Placa.rectTransform, new Vector2(0f, 0.5f), new Vector2(132f, 0f), new Vector2(96f, 96f));
            c.Glifo = c.Placa.transform.GetChild(0).GetComponent<Image>();
            c.Premio = EscreveMeta(c.Rt, "Premio", Tipo.Nome);
            NaFaixa(c.Premio.rectTransform, 244f, 24f, CanhotoPremium + 20f, 60f);
            c.Estado = EscreveMeta(c.Rt, "Estado", Tipo.Legenda);
            NaFaixa(c.Estado.rectTransform, 244f, 94f, CanhotoPremium + 20f, 56f);
            c.Coletar = Art.NewButton(c.Rt, Textos.Coletar, 36, Art.Compra, Vector2.up, Vector2.up, () => _passeAColetar.Remove(n));
            Art.Fixa((RectTransform)c.Coletar.transform, Vector2.up, new Vector2(244f, -88f), new Vector2(220f, 76f));
            c.PremiumPlaca = Art.NovaPlaca(c.Rt, "PremiumPlaca", Art.Cadeado(), Art.Apagado, Art.Fraco);
            Art.Fixa(c.PremiumPlaca.rectTransform, Vector2.one, new Vector2(-CanhotoPremium / 2f + 40f, -22f), new Vector2(80f, 80f));
            c.PremiumGlifo = c.PremiumPlaca.transform.GetChild(0).GetComponent<Image>();
            c.PremiumTexto = Art.Escreve(c.Rt, "PremiumTexto", Tipo.Legenda, Vector2.right, Vector2.right, TextAnchor.MiddleCenter);
            Art.Fixa(c.PremiumTexto.rectTransform, Vector2.right, new Vector2(-(CanhotoPremium - LargCanhoto) / 2f, 18f), new Vector2(LargCanhoto, 52f));
            c.PremiumTexto.resizeTextMinSize = MenorTexto;
            c.Destaque = Art.Img(c.Rt, "Destaque", Art.Contorno(), Art.Placa, Vector2.zero, Vector2.one);
            c.Destaque.type = Image.Type.Sliced; c.Destaque.preserveAspect = false;
            return c;
        }

        /// <summary>A direita da faixa da meta: estrela, nivel e barra do nivel. Toque abre o passe.</summary>
        void BuildPasseAtalho()
        {
            float x = 16f + LargFaixaMeta + 12f;
            _passeAtalho = Art.NovoCartao(_root, "PasseAtalho", AltFaixaMeta);
            Art.Fixa(_passeAtalho, Vector2.zero, new Vector2(x, AltBase + 14f), new Vector2(RefW - 16f - x, AltFaixaMeta));
            Image fundo = _passeAtalho.GetComponent<Image>();
            fundo.raycastTarget = true;   // o toque no atalho nao vai para o mundo
            Button b = _passeAtalho.gameObject.AddComponent<Button>();
            b.targetGraphic = fundo;
            b.onClick.AddListener(() => AbreTela("passe"));
            _passeAtalho.gameObject.AddComponent<Aperta>();
            Image placa = Art.NovaPlaca(_passeAtalho, "Placa", Art.Estrela(), Art.Coin, Color.white);
            Art.Fixa(placa.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(88f, 88f));
            Text nome = EscreveMeta(_passeAtalho, "Nome", Tipo.Nome);
            NaFaixa(nome.rectTransform, 114f, 12f, 14f, 56f);
            nome.text = Textos.Passe;
            _atalhoNivel = EscreveMeta(_passeAtalho, "Nivel", Tipo.Legenda);
            NaFaixa(_atalhoNivel.rectTransform, 114f, 64f, 14f, 40f);
            _atalhoNivel.color = Art.Tinta;
            RectTransform trilho = Art.Panel(_passeAtalho, "Trilho", Art.Vazio, Vector2.up, Vector2.one).rectTransform;
            NaFaixa(trilho, 114f, 110f, 20f, 16f);
            _atalhoBarra = Art.Panel(trilho, "Barra", Art.Coin, Vector2.zero, Vector2.one);
            _seloPasse = NovoSelo(_passeAtalho, out _seloPasseN);
        }

        void AbrePasse()
        {
            Abre(_passe);
            PintaPasseTela(S);
            CartaoNivel alvo = _nivel[Mathf.Clamp(S.NivelPasse, 0, _nivel.Length - 1)];   // abre no proximo nivel
            RectTransform conteudo = _passeLista, vista = (RectTransform)conteudo.parent;
            LayoutRebuilder.ForceRebuildLayoutImmediate(conteudo);
            float topo = -alvo.Rt.anchoredPosition.y - alvo.Rt.rect.height * (1f - alvo.Rt.pivot.y);
            conteudo.anchoredPosition = new Vector2(0f, Mathf.Clamp(topo - 2f * AltNivel, 0f, Mathf.Max(0f, conteudo.rect.height - vista.rect.height)));
        }

        /// <summary>Game.Trata: Ev.PasseNivel. O nucleo ja pagou; a tela avisa e guarda para o Coletar.</summary>
        public void PasseNivel(int temporada, int nivel)
        {
            if (temporada < 0 || temporada >= S.T.Temporadas.Length || nivel < 1 || nivel > S.T.Temporadas[temporada].Gratis.Length) return;
            if (temporada != _passeTemp) { _passeAColetar.Clear(); _passeTemp = temporada; }
            _passeAColetar.Add(nivel);
            Toast(Textos.PasseNivelFeito(nivel, S.T.Temporadas[temporada].Gratis[nivel - 1]));
        }

        // ------------------------------------------------------------------ quadro a quadro

        void PintaPasse(Sim s)
        {
            Temporada t = s.Temporada;
            bool mostra = t != null && !TourAberto && !Arrastando;
            _passeAtalho.gameObject.SetActive(mostra);
            if (mostra)
            {
                int nivel = s.NivelPasse, total = t.Gratis.Length, pn = s.T.PassePontosNivel;
                _atalhoNivel.text = Textos.NivelCurto(nivel);
                Barra(_atalhoBarra, nivel >= total ? pn : s.PontosPasse - nivel * pn, pn);
                _seloPasse.gameObject.SetActive(_passeAColetar.Count > 0);
                _seloPasseN.text = _passeAColetar.Count.ToString();
                _seloPasse.rectTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f));
            }
            if (_passe.Rt.gameObject.activeSelf) PintaPasseTela(s);
        }

        void PintaPasseTela(Sim s)
        {
            Temporada t = s.Temporada;
            bool aberto = t != null;
            int total = aberto ? t.Gratis.Length : 0, nivel = s.NivelPasse, pn = s.T.PassePontosNivel;
            bool completo = aberto && nivel >= total;
            _passeNivel.Valor.text = Textos.DeTotal(nivel, total);
            _passePontos.Rotulo.text = completo || !aberto ? Textos.TrilhaCompleta : Textos.ParaNivel(nivel + 1);
            _passePontos.Valor.text = Textos.Num(completo || !aberto ? s.PontosPasse : (nivel + 1) * pn - s.PontosPasse);   // completa: os pontos da barra
            _passeDias.Valor.text = Textos.Num(s.DiasRestantesPasse);
            _passeBarra.transform.parent.gameObject.SetActive(aberto);
            _passeConta.gameObject.SetActive(aberto);
            int naBarra = completo ? pn : s.PontosPasse - nivel * pn;
            Barra(_passeBarra, naBarra, pn);
            _passeConta.text = Textos.Conta(naBarra, pn);

            string[] regras = Textos.RegrasPasse(s.T, aberto ? t.Dias : 0);
            for (int i = 0; i < _regra.Length; i++) _regra[i].text = regras[i];
            _regrasLinhas.gameObject.SetActive(aberto);
            _passeFechado.gameObject.SetActive(!aberto);
            _premiumCartao.gameObject.SetActive(aberto);
            _passeColunas.gameObject.SetActive(aberto);

            bool premium = PremiumAberto(s);
            _premiumPlaca.color = premium ? Art.Gold : Art.Apagado;
            _premiumGlifo.sprite = premium ? Art.Estrela() : Art.Cadeado();
            _premiumGlifo.color = premium ? Color.white : Art.Fraco;
            _premiumNome.text = premium ? Textos.PremiumLiberadaTitulo : Textos.PremiumTitulo;
            _premiumLinha.text = premium ? Textos.PremiumLiberada : Textos.PremiumTrancada;

            for (int n = 1; n <= _nivel.Length; n++)
            {
                CartaoNivel c = _nivel[n - 1];
                c.Rt.gameObject.SetActive(n <= total);
                if (n > total) continue;
                PintaNivel(c, t.Gratis[n - 1], EstadoDoNivel(s, n, _passeAColetar), n * pn, s.PontosPasse, premium);
            }
        }

        /// <summary>alvo = os pontos que fecham este nivel; pontos = os da barra.</summary>
        static void PintaNivel(CartaoNivel c, Nivel premio, EstadoNivel e, int alvo, int pontos, bool premium)
        {
            bool alcancado = e == EstadoNivel.Coletar || e == EstadoNivel.Recebido;
            c.Disco.color = alcancado ? Art.Placa : e == EstadoNivel.Proximo ? Art.Azul : Art.Vazio;
            c.Num.color = alcancado ? Art.Tinta : e == EstadoNivel.Proximo ? Color.white : Art.Fraco;
            var (glifo, fundo, cor) = IconePremio(premio);
            c.Placa.color = fundo;
            c.Glifo.sprite = glifo; c.Glifo.color = cor;
            c.Glifo.rectTransform.localEulerAngles = new Vector3(0f, 0f, glifo == Art.Triangle() ? 180f : 0f);   // bandeirola de ponta para baixo
            c.Premio.text = Textos.PremioNome(premio);
            c.Premio.color = e == EstadoNivel.Trancado ? Art.Fraco : Art.Tinta;
            c.Destaque.enabled = e == EstadoNivel.Proximo;
            c.Coletar.gameObject.SetActive(e == EstadoNivel.Coletar);
            c.Estado.gameObject.SetActive(e != EstadoNivel.Coletar);
            c.Estado.text = e == EstadoNivel.Recebido ? Textos.Recebido : e == EstadoNivel.Proximo ? Textos.FaltamPontos(alvo - pontos) : Textos.AbreCom(alvo);
            c.Estado.color = e == EstadoNivel.Recebido ? Art.FeitoTinta : e == EstadoNivel.Proximo ? Art.Tinta : Art.Fraco;
            c.PremiumPlaca.color = premium ? Art.Gold : Art.Apagado;
            c.PremiumGlifo.sprite = premium ? Art.Estrela() : Art.Cadeado();
            c.PremiumGlifo.color = premium ? Color.white : Art.Fraco;
            c.PremiumTexto.text = PremiumNoNivel(premium);
            c.PremiumTexto.color = premium ? Art.FeitoTinta : Art.Fraco;
        }

        /// <summary>O canhoto premium de cada nivel: sem conteudo no nucleo ainda, diz so de onde ele vem.</summary>
        public static string PremiumNoNivel(bool aberto) => aberto ? Textos.Liberada : Textos.ComALoja;
    }
}
