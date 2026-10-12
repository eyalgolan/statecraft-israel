using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Statecraft.Sim
{
    /// <summary>Changed lines between two <see cref="GameState.Describe"/> texts, grouped by section. Used by the golden
    /// tests (#270) and the writer preview (#295).</summary>
    public static class StateDiff
    {
        /// <summary>Changed lines grouped by section (the label before '{' or '[' or '='), at most 40 removed and 40 added per section.</summary>
        public static string Lines(string before, string after)
        {
            List<(string section, string line)> Parse(string text)
            {
                var res = new List<(string, string)>();
                string section = "header";
                foreach (var raw in text.Split('\n'))
                {
                    if (raw.Length == 0) continue;
                    if (!raw.StartsWith("  "))
                    {
                        var end = raw.IndexOfAny(new[] { '{', '[', '=' });
                        section = end > 0 && raw.IndexOf(';') is var semi && (semi < 0 || end < semi) ? raw.Substring(0, end) : "header";
                    }
                    res.Add((section, raw));
                }
                return res;
            }
            var a = Parse(before); var b = Parse(after);
            var aSet = new HashSet<(string, string)>(a); var bSet = new HashSet<(string, string)>(b);
            var sb = new StringBuilder();
            foreach (var g in a.Concat(b).Select(x => x.section).Distinct())
            {
                var removed = a.Where(x => x.section == g && !bSet.Contains(x)).Select(x => "  - " + x.line.TrimStart()).ToList();
                var added = b.Where(x => x.section == g && !aSet.Contains(x)).Select(x => "  + " + x.line.TrimStart()).ToList();
                if (removed.Count + added.Count == 0) continue;
                sb.AppendLine($"[{g}]");
                foreach (var l in removed.Take(40)) sb.AppendLine(l);
                foreach (var l in added.Take(40)) sb.AppendLine(l);
            }
            return sb.Length == 0 ? "(the descriptions are identical)\n" : sb.ToString();
        }
    }
}
