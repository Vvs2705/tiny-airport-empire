using System;
using System.Collections.Generic;
using TA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TA
{
    /// <summary>
    /// Preferencias do jogador (PlayerPrefs ta_vibrar, ta_movimento e ta_texto; os volumes ficam no Audio). Fora do -testsession: le ao abrir
    /// e grava a cada toque. No -testsession nao le nem grava (as fotos saem iguais em qualquer PC); -textomaior e -reduzmovimento ligam.
    /// Vibrar so fica guardado: o jogo ainda nao vibra (Handheld.Vibrate poe a permissao VIBRATE no manifesto, entra com quem usar).
    /// </summary>
    public static class Ajustes
    {
        const string VibrarKey = "ta_vibrar", MovimentoKey = "ta_movimento", TextoKey = "ta_texto", IdiomaKey = "ta_idioma";
        public static bool Vibrar = true, ReduzMovimento, TextoMaior;
        /// <summary>O idioma escolhido na tela de configuracoes; so vale (Idiomas.Atual) na proxima abertura do jogo.</summary>
        public static Lingua Escolhido = Lingua.Pt;
        static bool _grava;

        public static void Carrega(bool grava)
        {
            _grava = grava;
            if (!grava) return;
            Vibrar = PlayerPrefs.GetInt(VibrarKey, 1) == 1;
            ReduzMovimento = PlayerPrefs.GetInt(MovimentoKey, 0) == 1;
            TextoMaior = PlayerPrefs.GetInt(TextoKey, 0) == 1;
            // Sem escolha guardada, o idioma do aparelho (ingles = en, o resto = pt-BR). Tem de rodar antes do primeiro acesso a Textos.
            string guardado = PlayerPrefs.GetString(IdiomaKey, "");
            Idiomas.Atual = Escolhido = guardado.Length == 0 ? Idiomas.DoAparelho(Application.systemLanguage.ToString()) : Idiomas.De(guardado);
        }

        public static void EscolheIdioma(Lingua l)
        {
            Escolhido = l;
            if (!_grava) return;
            PlayerPrefs.SetString(IdiomaKey, Idiomas.Codigo(l));
            PlayerPrefs.Save();
        }

        public static void Grava()
        {
            if (!_grava) return;
            PlayerPrefs.SetInt(VibrarKey, Vibrar ? 1 : 0);
            PlayerPrefs.SetInt(MovimentoKey, ReduzMovimento ? 1 : 0);
            PlayerPrefs.SetInt(TextoKey, TextoMaior ? 1 : 0);
            PlayerPrefs.Save();   // tambem leva os volumes, que o Audio so poe na memoria
        }

        /// <summary>0 com Reduzir movimento: multiplica balanco e tremor.</summary>
        public static float Mov => ReduzMovimento ? 0f : 1f;

        /// <summary>O PingPong de 0 a 1 do que pisca; parado no meio com Reduzir movimento.</summary>
        public static float Vai(float t) => ReduzMovimento ? 0.5f : Mathf.PingPong(t, 1f);
    }

    /// <summary>
    /// Configuracoes (GDD §11.1, contrato linha 21), aberta pela pausa: volume de musica e de efeitos em 5 degraus, vibracao, reduzir movimento,
    /// texto maior, idioma, politica de privacidade (navegador), suporte (e-mail), creditos (docs/CREDITOS.md) com a versao e excluir meus
    /// dados, com confirmacao: apaga save, diario e PlayerPrefs deste aparelho e fecha o jogo (Game.ApagaDados).
    /// </summary>
    public sealed partial class Hud
    {
        const int Degraus = 5;
        const float TextoFator = 1.25f;

        Modal _config, _excluir;
        readonly Item[] _volumeItem = new Item[2];
        readonly BarraNivel[] _volumeBarra = new BarraNivel[2];
        readonly BotaoPreco[] _volumeMenos = new BotaoPreco[2];
        Item _vibrar, _movimento, _texto;
        readonly Dictionary<Text, int> _textoBase = new Dictionary<Text, int>();

        void BuildConfig(RectTransform canvas)
        {
            _config = NovaFolhaModal(canvas, "Config", Textos.Configuracoes, Art.Chave(), 1600f);
            RectTransform lista = Art.NovaLista(_config.F.Corpo, Vector2.zero, Vector2.one);
            Art.Margem((RectTransform)lista.parent, 24f, 0f, 24f, 16f);

            ItemVolume(lista, 0, Textos.Musica, Art.Radio());
            ItemVolume(lista, 1, Textos.Efeitos, Art.Alerta());
            _vibrar = ItemPausa(lista, "Vibrar", Art.Raio(), Art.EstacaoCor[1], Textos.Vibracao, Textos.VibracaoLegenda, "",
                it => { Ajustes.Vibrar = !Ajustes.Vibrar; Ajustes.Grava(); });
            _movimento = ItemPausa(lista, "Movimento", Art.Seta(), Art.EstacaoCor[2], Textos.ReduzirMovimento, Textos.MovimentoLegenda, "",
                it => { Ajustes.ReduzMovimento = !Ajustes.ReduzMovimento; Ajustes.Grava(); });
            _texto = ItemPausa(lista, "Texto", Art.Bilhete(), Art.EstacaoCor[3], Textos.TextoMaior, Textos.TextoMaiorLegenda, "",
                it => { Ajustes.TextoMaior = !Ajustes.TextoMaior; Ajustes.Grava(); AplicaTexto(); });
            ItemPausa(lista, "Idioma", Art.Aviao(), Art.EstacaoCor[4], Textos.Idioma, Textos.IdiomaNome, Textos.NomeDoIdioma(Outro(Ajustes.Escolhido)),
                it =>
                {
                    Lingua nova = Outro(Ajustes.Escolhido);
                    Ajustes.EscolheIdioma(nova);
                    it.Linha.text = Textos.ReabraIdioma;
                    it.Btn.Secundario(Textos.NomeDoIdioma(Outro(nova)));
                });
            ItemConta(lista);   // so com a UGS no build (Hud.Conta.cs)
            ItemPausa(lista, "Privacidade", Art.Escudo(), Art.EstacaoCor[5], Textos.Privacidade, Textos.PrivacidadeLegenda, Textos.Abrir,
                it => Application.OpenURL(Textos.UrlPrivacidade));
            ItemPausa(lista, "Suporte", Art.Balao(), Art.EstacaoCor[0], Textos.Suporte, Textos.EmailSuporte, Textos.Escrever,
                it => Application.OpenURL("mailto:" + Textos.EmailSuporte + "?subject=" + Uri.EscapeDataString("Little Terminal " + Application.version)));
            Item cr = NovoItem(lista, "Creditos", Art.Estrela(), Art.Coin, null, 340f, 20f);
            cr.Nome.text = Textos.Creditos; cr.Linha.text = Textos.Versao(Application.version); cr.Info.text = Textos.CreditosTexto;
            Item ex = ItemPausa(lista, "Excluir", Art.Xis(), Art.Perigo, Textos.ExcluirDados, Textos.ExcluirLegenda, Textos.Excluir, it => Abre(_excluir));
            ex.Btn.Valor.color = Art.Perigo;

            BuildConta(canvas);

            _excluir = NovaFolhaModal(canvas, "Excluir", Textos.ExcluirDados, Art.Alerta(), 1100f);
            RectTransform pe = _excluir.F.Corpo, le = Art.NovaLista(pe, Vector2.zero, Vector2.one);
            Art.Margem((RectTransform)le.parent, 24f, 186f, 24f, 16f);
            Text t = Art.Escreve(Art.NovoCartao(le, "Texto", 600f), "Texto", Tipo.Corpo, Vector2.zero, Vector2.one, TextAnchor.UpperLeft);
            Art.Margem(t.rectTransform, 32f, 24f, 32f, 28f);
            t.text = Textos.ExcluirTexto;
            BotaoDoPe(pe, Vector2.zero, new Vector2(0.5f, 0f), () => Fecha(_excluir)).Secundario(Textos.Cancelar);
            BotaoDoPe(pe, new Vector2(0.5f, 0f), new Vector2(1f, 0f), () => _g.ApagaDados()).Principal(Textos.ApagarTudo, Art.Perigo);
        }

        static Lingua Outro(Lingua l) => l == Lingua.En ? Lingua.Pt : Lingua.En;

        /// <summary>Volume k (0 musica, 1 efeitos): barra de 5 degraus e, no canhoto, - e +.</summary>
        void ItemVolume(RectTransform lista, int k, string nome, Sprite icone)
        {
            Item it = NovoItem(lista, k == 0 ? "Musica" : "Efeitos", icone, Art.Azul, () => Volume(k, 1), 186f, 20f);
            it.Nome.text = nome;
            Art.Fixa((RectTransform)it.Btn.B.transform, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(118f, 146f));
            it.Btn.Secundario("+");
            Art.Toque(it.Btn.B, 14f, 0f);   // 146 px de toque, sem encostar no -
            _volumeMenos[k] = BotaoDoCartao(it.Rt, () => Volume(k, -1));
            Art.Fixa((RectTransform)_volumeMenos[k].B.transform, new Vector2(1f, 0.5f), new Vector2(-170f, 0f), new Vector2(118f, 146f));
            _volumeMenos[k].Secundario("−");
            Art.Toque(_volumeMenos[k].B, 14f, 0f);
            _volumeBarra[k] = Art.NovaBarraNivel(it.Rt, "Volume", Degraus, Vector2.up, Vector2.one);
            var r = (RectTransform)_volumeBarra[k].Pai;
            r.offsetMin = new Vector2(172f, -150f); r.offsetMax = new Vector2(-324f, -126f);
            _volumeItem[k] = it;
        }

        static float VolumeDe(int k) => k == 0 ? Audio.Musica : Audio.Som;

        static void Volume(int k, int passo)
        {
            float v = Mathf.Clamp(Mathf.RoundToInt(VolumeDe(k) * Degraus) + passo, 0, Degraus) / (float)Degraus;
            if (k == 0) Audio.SetVolumes(Audio.Som, v); else Audio.SetVolumes(v, Audio.Musica);
            Ajustes.Grava();
        }

        /// <summary>- e + no limite: cinza e sem toque (o botao nao tem transicao de cor, quem pinta e a tela).</summary>
        static void Pode(BotaoPreco b, bool pode)
        {
            b.B.interactable = pode;
            b.Fundo.color = pode ? Art.Suave : Art.Apagado;
            b.Valor.color = pode ? Art.Azul : Art.Fraco;
        }

        static void Interruptor(Item it, bool ligado)
        {
            if (ligado) it.Btn.Principal(Textos.Ligado, Art.Compra); else it.Btn.Secundario(Textos.Desligado);
        }

        void PintaConfig()
        {
            PintaConta();
            if (!_config.Rt.gameObject.activeSelf) return;
            for (int k = 0; k < 2; k++)
            {
                int n = Mathf.RoundToInt(VolumeDe(k) * Degraus);
                _volumeItem[k].Linha.text = Textos.Porcento(n / (float)Degraus);
                _volumeBarra[k].Pinta(n, Art.Azul);
                Pode(_volumeItem[k].Btn, n < Degraus);
                Pode(_volumeMenos[k], n > 0);
            }
            Interruptor(_vibrar, Ajustes.Vibrar);
            Interruptor(_movimento, Ajustes.ReduzMovimento);
            Interruptor(_texto, Ajustes.TextoMaior);
        }

        /// <summary>
        /// Texto maior: o teto do best fit sobe 25% em todo texto que se ajusta. Cresce o que tem espaco na caixa e nada passa dela (o piso
        /// nao muda, entao nada corta). Os textos de tamanho fixo (valores dos botoes) ficam como estao. Chamar no Build e ao trocar.
        /// </summary>
        void AplicaTexto()
        {
            foreach (Text t in _canvas.GetComponentsInChildren<Text>(true))
            {
                if (!t.resizeTextForBestFit) continue;
                if (!_textoBase.TryGetValue(t, out int b)) _textoBase[t] = b = t.resizeTextMaxSize;
                t.resizeTextMaxSize = Ajustes.TextoMaior ? Mathf.RoundToInt(b * TextoFator) : b;
            }
        }
    }
}
