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
        /// gives her), and then the grammatically masculine title_he stands until the office gets a title_he_f: still Hebrew, so
        /// the Hebrew line and its bidi stay whole, where an English title would break them.</summary>
        public static string Of(UiText text, Office o, Person? holder)
        {
            var hebrew = o.TitleHeFor(holder);
            return Pick(text, o.Title, hebrew != "" ? hebrew : o.TitleHe);
        }
    }
}
