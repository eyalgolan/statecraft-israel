#nullable enable
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>The names of content rows in the UI's language (#558): in Hebrew the row's sourced Hebrew form (#582, #661),
    /// and the English one wherever the content has no Hebrew form yet; in English always the English one.</summary>
    public static class ContentNames
    {
        static string Pick(UiText text, string english, string hebrew) =>
            hebrew != "" && text["ui.lang"] == UiLanguage.Hebrew ? hebrew : english;

        public static string Of(UiText text, Person p) => Pick(text, p.Name, p.NameHe);
        public static string Of(UiText text, Party p) => Pick(text, p.Name, p.NameHe);
        public static string Of(UiText text, Government g) => Pick(text, g.Name, g.NameHe);
        public static string Of(UiText text, Legislature l) => Pick(text, l.Body, l.BodyHe);

        /// <summary>The office's title for <paramref name="holder"/> (null: the office itself, masculine). A woman is given
        /// the feminine title_he_f; an office she never held may have none (the loader checks only the offices people.yaml
        /// gives her), and then a minister's title takes the feminine form of title_he (Feminine) rather than stay masculine.</summary>
        public static string Of(UiText text, Office o, Person? holder)
        {
            var hebrew = o.TitleHeFor(holder);
            if (hebrew == "") hebrew = holder?.TitleHeForm == TitleHeForms.Feminine ? Feminine(o.TitleHe) : o.TitleHe;
            return Pick(text, o.Title, hebrew);
        }

        /// <summary>A minister's title for a woman (Beri, native speaker, 17 October 2026): "שר האוצר" is "שרת האוצר", "השר
        /// לנפגעי מלחמה" is "השרה לנפגעי מלחמה", and "שר בלי תיק" (no construct) is "שרה בלי תיק". Any other title stays as it is:
        /// the Prime Minister is "ראש הממשלה" for a woman too, and so are the Chief of Staff's and the President's.</summary>
        static string Feminine(string title)
        {
            if (title.StartsWith("השר ", System.StringComparison.Ordinal)) return "השרה " + title.Substring(4);
            if (!title.StartsWith("שר ", System.StringComparison.Ordinal)) return title;
            var rest = title.Substring(3);
            return (rest.StartsWith("ה", System.StringComparison.Ordinal) ? "שרת " : "שרה ") + rest;
        }
    }
}
