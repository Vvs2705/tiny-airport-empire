using UnityEngine;
using UnityEngine.UI;
using Vstack.Conta;

namespace TA
{
    /// <summary>
    /// A conta opcional (frente de contas, docs/CONTAS.md), aberta em Configuracoes > Conta: o estado (anonima, ligada, sem conexao), um
    /// cartao por login que o build oferece (Google Play Games, Facebook) com Conectar ou Desconectar, e a promessa do brinde enquanto nao
    /// ligou. Tres folhas de resposta: "Conta ja existente" (o login e' de outra conta: carregar o progresso dela), "Dois progressos" (o
    /// jogador escolhe) e "Progresso carregado" (fecha o jogo). Sem a UGS no build (_g.Conta null), o cartao nem aparece.
    /// </summary>
    public sealed partial class Hud
    {
        Modal _conta, _trocar, _escolher, _carregado;
        Item _contaItem;
        readonly Item[] _provItem = new Item[2];
        static readonly Provedor[] Provedores = { Provedor.GooglePlayGames, Provedor.Facebook };
        Provedor _trocarQual;
        Text _escolherAqui, _escolherNuvem;
        Text _contaId;

        /// <summary>O cartao da conta na lista de Configuracoes (antes da privacidade).</summary>
        void ItemConta(RectTransform lista)
        {
            ContaJogo c = _g.Conta;
            if (c == null) return;
            _contaItem = ItemPausa(lista, "Conta", Art.Chave(), Art.Azul, Textos.ContaTitulo, Textos.ContaAnonima, Textos.Abrir, it => Abre(_conta));
        }

        void BuildConta(RectTransform canvas)
        {
            ContaJogo c = _g.Conta;
            if (c == null) return;
            c.Toast += Toast;
            c.PedeTrocar += p => { _trocarQual = p; Abre(_trocar); };
            c.PedeEscolher += (aqui, nuvem) => { _escolherAqui.text = Textos.EsteAparelho + ": " + Textos.Progresso(aqui); _escolherNuvem.text = Textos.Nuvem + ": " + Textos.Progresso(nuvem); Abre(_escolher); };
            c.Carregado += () => { Fecha(_conta); Fecha(_trocar); Fecha(_escolher); Abre(_carregado); };

            _conta = NovaFolhaModal(canvas, "ContaFolha", Textos.ContaTitulo, Art.Chave(), 900f);
            RectTransform lista = Art.NovaLista(_conta.F.Corpo, Vector2.zero, Vector2.one);
            Art.Margem((RectTransform)lista.parent, 24f, 0f, 24f, 16f);
            for (int i = 0; i < Provedores.Length; i++)
            {
                Provedor p = Provedores[i];
                if (!c.Oferece(p)) continue;
                _provItem[i] = ItemPausa(lista, p.ToString(), p == Provedor.Facebook ? Art.Balao() : Art.Estrela(), Art.EstacaoCor[i + 1],
                    p == Provedor.Facebook ? Textos.Facebook : Textos.GooglePlayGames, "", Textos.Conectar,
                    it => { if (c.Ligado(p)) c.Desligar(p); else c.Ligar(p); });
            }

            _contaId = LinhaTexto(lista, "Id");   // o jogador informa este id num pedido de exclusao por e-mail (docs/privacidade/excluir-conta)
            _contaId.fontSize = 28; _contaId.resizeTextForBestFit = false;

            _trocar = Folha(canvas, "ContaTrocar", Textos.ContaJaExiste, Textos.ContaJaExisteTexto, out _);
            BotaoDoPe(_trocar.F.Corpo, Vector2.zero, new Vector2(0.5f, 0f), () => Fecha(_trocar)).Secundario(Textos.Cancelar);
            BotaoDoPe(_trocar.F.Corpo, new Vector2(0.5f, 0f), new Vector2(1f, 0f), () => { Fecha(_trocar); c.Trocar(_trocarQual); }).Principal(Textos.Carregar, Art.Compra);

            _escolher = Folha(canvas, "ContaEscolher", Textos.DoisProgressos, Textos.DoisProgressosTexto, out RectTransform le);
            _escolherAqui = LinhaTexto(le, "Aqui");
            _escolherNuvem = LinhaTexto(le, "Nuvem");
            BotaoDoPe(_escolher.F.Corpo, Vector2.zero, new Vector2(0.5f, 0f), () => { Fecha(_escolher); c.Escolhe(false); }).Secundario(Textos.EsteAparelho);
            BotaoDoPe(_escolher.F.Corpo, new Vector2(0.5f, 0f), new Vector2(1f, 0f), () => { Fecha(_escolher); c.Escolhe(true); }).Principal(Textos.Nuvem, Art.Compra);

            _carregado = Folha(canvas, "ContaCarregada", Textos.ProgressoCarregado, Textos.ProgressoCarregadoTexto, out _);
            BotaoDoPe(_carregado.F.Corpo, Vector2.zero, new Vector2(1f, 0f), Application.Quit).Principal(Textos.FecharJogo, Art.Compra);
        }

        /// <summary>Folha com um cartao de texto e lugar para 2 botoes no pe.</summary>
        Modal Folha(RectTransform canvas, string nome, string titulo, string texto, out RectTransform lista)
        {
            Modal m = NovaFolhaModal(canvas, nome, titulo, Art.Chave(), 1000f);
            lista = Art.NovaLista(m.F.Corpo, Vector2.zero, Vector2.one);
            Art.Margem((RectTransform)lista.parent, 24f, 186f, 24f, 16f);
            Text t = Art.Escreve(Art.NovoCartao(lista, "Texto", 420f), "Texto", Tipo.Corpo, Vector2.zero, Vector2.one, TextAnchor.UpperLeft);
            Art.Margem(t.rectTransform, 32f, 24f, 32f, 28f);
            t.text = texto;
            return m;
        }

        static Text LinhaTexto(RectTransform lista, string nome)
        {
            Text t = Art.Escreve(Art.NovoCartao(lista, nome, 120f), "Texto", Tipo.Nome, Vector2.zero, Vector2.one, TextAnchor.MiddleLeft);
            Art.Margem(t.rectTransform, 32f, 12f, 32f, 12f);
            return t;
        }

        /// <summary>Com Configuracoes ou Conta abertas: o estado e o botao de cada login.</summary>
        void PintaConta()
        {
            ContaJogo c = _g.Conta;
            if (c == null || _contaItem == null) return;
            EstadoConta e = c.Estado;
            _contaItem.Linha.text = e == EstadoConta.Fora ? Textos.ContaFora : e == EstadoConta.Anonima ? Textos.ContaAnonima
                : c.Sincronizando ? Textos.ContaSincronizando : Textos.ContaLigada;
            if (!_conta.Rt.gameObject.activeSelf) return;
            _contaId.text = string.IsNullOrEmpty(c.Jogador) ? Textos.ContaFora : Textos.IdDaConta + c.Jogador;
            string brinde = S.BrindeConta ? "" : Textos.BrindeConta(S.T.BrindeContaGems, S.T.BrindeContaBoostSeg / 60);
            for (int i = 0; i < Provedores.Length; i++)
            {
                Item it = _provItem[i];
                if (it == null) continue;
                bool ligado = c.Ligado(Provedores[i]);
                it.Linha.text = ligado ? Textos.Conectado : brinde;
                if (ligado) it.Btn.Secundario(Textos.Desconectar); else it.Btn.Principal(Textos.Conectar, Art.Compra);
                it.Btn.B.interactable = !c.Ocupada;
            }
        }
    }
}
