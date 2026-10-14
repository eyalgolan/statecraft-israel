#nullable enable
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Statecraft.Game.UI
{
    /// <summary>The UI's language (#193): English unless Hebrew is asked for by hand, "?lang=he" in the page URL on WebGL or
    /// "-lang he" on the desktop command line. There is no control in the game yet: the content text (events, names) is
    /// English only until #55, so the Hebrew UI is a preview. Any other value, or a bare flag, is English.</summary>
    public static class UiLanguage
    {
        public const string Flag = "lang";
        public const string English = "en", Hebrew = "he";
        /// <summary>On the UI root while the UI is Hebrew: the Hebrew face, the advanced text generator and the mirrored rows
        /// (Statecraft.uss).</summary>
        public const string HebrewClass = "lang-he";

        // A whole query parameter with a whole value (UrlFlags' rule): "?lang=hex" and "?slang=he" are not the flag.
        static readonly Regex HebrewQuery = new Regex(@"[?&]" + Flag + "=" + Hebrew + @"(?=&|#|$)", RegexOptions.CultureInvariant);

        public static string Requested(string? url, string[] args, bool webgl)
        {
            if (webgl) return HebrewQuery.IsMatch(url ?? "") ? Hebrew : English;
            var i = Array.IndexOf(args, "-" + Flag);
            return i >= 0 && i + 1 < args.Length && args[i + 1] == Hebrew ? Hebrew : English;
        }

        /// <summary>Whether <paramref name="e"/> is inside the Hebrew UI (an ancestor carries <see cref="HebrewClass"/>): code
        /// that paints a sign at one side of an element puts it on the other side there.</summary>
        public static bool Within(UnityEngine.UIElements.VisualElement? e)
        {
            for (; e != null; e = e.parent) if (e.ClassListContains(HebrewClass)) return true;
            return false;
        }

        public static string Requested() =>
            Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);
    }
}
