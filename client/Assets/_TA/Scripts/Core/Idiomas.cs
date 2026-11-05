using System.Collections.Generic;

namespace TA.Core
{
    public enum Lingua { Pt = 0, En = 1 }

    /// <summary>
    /// Idioma do texto (D-08: pt-BR e en no lancamento). Atual vale desde o primeiro acesso a Textos: a View le a preferencia antes disso
    /// (Ajustes.Carrega), entao trocar de idioma pede reabrir o jogo. Traduz(pt) devolve o par em ingles de Ingles.cs, ou o proprio pt
    /// quando o idioma e pt ou o texto nao tem par (o checa_repo.py garante que todo texto de tela tem).
    /// </summary>
    public static partial class Idiomas
    {
        public static Lingua Atual = Lingua.Pt;

        public static string Traduz(string pt) => Atual == Lingua.En && En.TryGetValue(pt, out string en) ? en : pt;

        /// <summary>"pt" ou "en": o codigo guardado nas preferencias.</summary>
        public static string Codigo(Lingua l) => l == Lingua.En ? "en" : "pt";

        /// <summary>O codigo de volta; qualquer coisa que nao seja "en" e pt.</summary>
        public static Lingua De(string codigo) => codigo == "en" ? Lingua.En : Lingua.Pt;

        /// <summary>Do idioma do aparelho (SystemLanguage.ToString() do Unity): so o ingles vira en; o resto fica em pt-BR, o texto de origem.</summary>
        public static Lingua DoAparelho(string sistema) => sistema == "English" ? Lingua.En : Lingua.Pt;

        public static int Pares => En.Count;
        public static IEnumerable<KeyValuePair<string, string>> Todos => En;
    }
}
