#nullable enable
using System;
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
        public static string Of(UiText text, Objective o) => Pick(text, o.Name, o.NameHe);
        public static string Of(UiText text, Legislature l) => Pick(text, l.Body, l.BodyHe);
        /// <summary>A city project kind's name: its draft name_he, pending a native speaker's review (#564, #1122).</summary>
        public static string Of(UiText text, ProjectKind k) => Pick(text, k.Name, k.NameHe);

        /// <summary>The office's title for <paramref name="holder"/> (null: the office itself, masculine): a woman is given
        /// the row's feminine title_he_f, which every office with a Hebrew title has (owner on #861: "Move to content"), so
        /// the UI derives no form; an office with no Hebrew title for her keeps the English.</summary>
        public static string Of(UiText text, Office o, Person? holder) => Pick(text, o.Title, o.TitleHeFor(holder));

        /// <summary>A content text in the UI's language, the one rule the event card and the outcome caption share (#1064,
        /// #1210): in Hebrew the row's Hebrew <paramref name="hebrew"/> with the ministers' Hebrew names in its placeholders;
        /// <paramref name="english"/> (already filled) wherever the row has no Hebrew text, and always in English.</summary>
        public static string Fill(UiText text, Content c, GameState s, DateTime date, string english, string hebrew) =>
            hebrew != "" && text["ui.lang"] == UiLanguage.Hebrew ? Cabinet.Fill(c, s, hebrew, date, p => Of(text, p)) : english;
    }
}
