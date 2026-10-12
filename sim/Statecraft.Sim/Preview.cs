using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Statecraft.Sim
{
    /// <summary>Writer preview (#295): plays the historical run on the real content and on the same content with some
    /// rules.yaml values changed in memory, then prints what differs. Nothing is written to content/.</summary>
    public static class Preview
    {
        /// <summary>Replaces the single-line top-level value of <paramref name="key"/> in rules.yaml text. Throws
        /// ArgumentException when the key is not a top-level single value. The loader still checks the new value with the strict Rules type.</summary>
        public static string SetRule(string rulesYaml, string key, string value)
        {
            if (!Regex.IsMatch(key, @"^[a-z][a-z0-9_]*$")) throw new ArgumentException($"'{key}' is not a rules.yaml key (use the snake_case name, for example tax_per_capita_per_year)");
            var m = Regex.Match(rulesYaml, @"(?m)^" + key + @":[ \t]*([^\r\n]*)(?=\r?$)");   // a CRLF checkout keeps its line ends
            if (!m.Success) throw new ArgumentException($"rules.yaml has no top-level key '{key}'");
            if (m.Groups[1].Value.Trim().Length == 0 || m.Groups[1].Value.TrimStart().StartsWith("#"))
                throw new ArgumentException($"'{key}' is a block, not a single value; edit it in rules.yaml");
            if (value.IndexOfAny(new[] { '\r', '\n' }) >= 0) throw new ArgumentException("a value must be one line");
            if (value.Trim().Length == 0) throw new ArgumentException($"'{key}' needs a value (an empty one would read as 0)");
            return rulesYaml.Substring(0, m.Index) + key + ": " + value + rulesYaml.Substring(m.Index + m.Length);
        }

        /// <summary>The content at <paramref name="contentRoot"/> with each "key=value" in <paramref name="sets"/> applied to rules.yaml in memory.</summary>
        public static Content LoadWith(string contentRoot, IEnumerable<string> sets)
        {
            var edits = new List<(string key, string value)>();
            foreach (var s in sets)
            {
                var eq = s.IndexOf('=');
                if (eq <= 0) throw new ArgumentException($"--set wants key=value, got '{s}'");
                edits.Add((s.Substring(0, eq).Trim(), s.Substring(eq + 1).Trim()));
            }
            var eventsDir = Path.Combine(contentRoot, "events");
            var eventFiles = Directory.Exists(eventsDir)
                ? Directory.GetFiles(eventsDir, "*.yaml").Select(f => "events/" + Path.GetFileName(f)).ToList()
                : new List<string>();
            return ContentLoader.Load(rel =>
            {
                var path = Path.Combine(contentRoot, rel);
                if (!File.Exists(path)) return null;
                var text = File.ReadAllText(path);
                if (rel == "rules.yaml") foreach (var (k, v) in edits) text = SetRule(text, k, v);
                return text;
            }, eventFiles);
        }

        /// <summary>The states after each <paramref name="every"/>th turn and the final one, playing HistoricalAI from the golden start until <paramref name="until"/>.</summary>
        public static List<GameState> Play(Content c, ulong seed, DateTime until, int every)
        {
            var s = Simulation.NewGame(c, seed, Mode.Historical);
            var states = new List<GameState> { s };
            for (int turn = 0; turn < 1000 && s.Date < until && s.GameOver == null; turn++)   // the same stop as GoldenRun.Play
            {
                s = Simulation.Step(c, s, HistoricalAI.Commands(c, s)).State;
                if ((turn + 1) % every == 0 || s.Date >= until || s.GameOver != null) states.Add(s);
            }
            return states;
        }

        static string Value(GameState s, string what)
        {
            if (what == "treasury") return s.Treasury.ToString("0.0", CultureInfo.InvariantCulture);
            if (what == "debt") return s.Debt.ToString("0.0", CultureInfo.InvariantCulture);
            if (what == "fx") return s.FxReserves.ToString("0.0", CultureInfo.InvariantCulture);
            if (what == "population") return s.TotalPopulation.ToString("N0", CultureInfo.InvariantCulture);
            if (what == "stability") return s.CoalitionStability.ToString("0.00", CultureInfo.InvariantCulture);
            return s.Indicators.TryGetValue(what, out var v) ? v.ToString("0.000", CultureInfo.InvariantCulture) : "-";
        }

        /// <summary>The preview text: the changed-state diff at the end date, then a table of <paramref name="show"/> (indicator ids, or treasury, debt, fx, population, stability) as "before to after".</summary>
        public static string Run(string contentRoot, DateTime until, ulong seed, IReadOnlyList<string> sets, IReadOnlyList<string> show, int every)
        {
            if (every < 1) throw new ArgumentException("--every must be at least 1");
            var baseContent = ContentLoader.Load(contentRoot);
            var changed = LoadWith(contentRoot, sets);
            foreach (var w in show)
                if (!baseContent.Indicators.Any(i => i.Id == w) && Array.IndexOf(new[] { "treasury", "debt", "fx", "population", "stability" }, w) < 0)
                    throw new ArgumentException($"--show: '{w}' is not an indicator or one of treasury, debt, fx, population, stability");
            var a = Play(baseContent, seed, until, every);
            var b = Play(changed, seed, until, every);
            var sb = new StringBuilder();
            sb.AppendLine($"Preview: seed {seed}, until {until:yyyy-MM-dd}, changes: {(sets.Count == 0 ? "(none)" : string.Join(", ", sets))}");
            sb.AppendLine("Nothing is written; content/ is untouched.");
            sb.AppendLine();
            sb.AppendLine($"Final state, base {a[a.Count - 1].Date:yyyy-MM-dd} hash {a[a.Count - 1].Hash().Substring(0, 8)} -> changed {b[b.Count - 1].Date:yyyy-MM-dd} hash {b[b.Count - 1].Hash().Substring(0, 8)}");
            sb.Append(StateDiff.Lines(a[a.Count - 1].Describe(), b[b.Count - 1].Describe()));
            if (show.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("date        " + string.Join("  ", show.Select(w => w.PadRight(22))));
                for (int i = 0; i < Math.Max(a.Count, b.Count); i++)
                {
                    var x = a[Math.Min(i, a.Count - 1)]; var y = b[Math.Min(i, b.Count - 1)];
                    var date = (i < b.Count ? y : x).Date;
                    sb.AppendLine(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "  " + string.Join("  ", show.Select(w => (Value(x, w) + " -> " + Value(y, w)).PadRight(22))));
                }
            }
            return sb.ToString();
        }
    }
}
