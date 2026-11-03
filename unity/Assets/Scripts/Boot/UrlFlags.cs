#nullable enable
using System.Globalization;
using System.Text.RegularExpressions;

namespace Statecraft.Game.Boot
{
    /// <summary>Flags in the WebGL page URL's query: "?golden", "&amp;smoke=5", "?profile", "?camlog". A flag is a whole query
    /// parameter: it starts at '?' or '&amp;' and ends at '=', '&amp;' or the end of the query (the rule GameRoot's smoke
    /// flag uses), so neither a path such as ".../golden-project/..." nor "?profile-x" or "?golden.y" turns one on. Only the
    /// part before the first '#' is read: a flag in the fragment is not a query parameter.</summary>
    public static class UrlFlags
    {
        public static bool Has(string? url, string name) => Regex.IsMatch(BeforeFragment(url), @"[?&]" + Regex.Escape(name) + @"(?=[=&]|$)");

        /// <summary>The page address without its fragment: everything before the first '#'. The other URL parsers cut with it too (#2265).</summary>
        internal static string BeforeFragment(string? url)
        {
            url ??= "";
            var hash = url.IndexOf('#');
            return hash < 0 ? url : url.Substring(0, hash);
        }

        /// <summary>The flag's integer value ("smoke=5" -> 5), or 0 when absent.</summary>
        public static int Int(string? url, string name)
        {
            var m = Regex.Match(BeforeFragment(url), @"[?&]" + Regex.Escape(name) + @"=(\d+)");
            return m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }
    }
}
