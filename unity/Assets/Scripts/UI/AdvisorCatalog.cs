#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.UI
{
    /// <summary>Reads and checks Assets/Resources/Advisors/advisor-messages.yaml (spec 4.1): the three advisors, their
    /// messages (a signal, a condition, a help level, a priority, a repeat rule, up to two actions) and the encyclopedia's
    /// pages. A Unity-side file, never part of the hashed game content.</summary>
    public sealed class AdvisorCatalog
    {
        public const string ResourcePath = "Advisors/advisor-messages";
        public const string Path = "Assets/Resources/Advisors/advisor-messages.yaml";
        public static readonly string[] AdvisorIds = { "ben_gurion", "meyerson", "dayan" };
        /// <summary>The lowest help level that shows a message, from the chattiest (spec 6); "critical" shows at every level.</summary>
        public static readonly string[] Levels = { "new", "experienced", "warnings", "critical" };
        public const string Once = "once", Every = "every", Approved = "approved", Pending = "pending";
        public const int MaxWords = 35;
        static readonly Regex IdPattern = new Regex("^[a-z0-9_.]+$", RegexOptions.CultureInvariant);

        public sealed class Advisor { public string Id { get; set; } = ""; public string Person { get; set; } = ""; }

        /// <summary>One button: Show me's named target, a side screen, an encyclopedia page, or the decision card.</summary>
        public sealed class Act
        {
            public string ShowMe { get; set; } = "";
            public string OpenScreen { get; set; } = "";
            public string Encyclopedia { get; set; } = "";
            public bool OpenCard { get; set; }
        }

        public sealed class Message
        {
            public string Id { get; set; } = "";
            public string Advisor { get; set; } = "";
            public string Signal { get; set; } = "";
            public Dictionary<string, double> When { get; set; } = new Dictionary<string, double>();
            public List<string> Requires { get; set; } = new List<string>();
            public string Level { get; set; } = "";
            public int Priority { get; set; } = 1;
            public string Repeat { get; set; } = Once;
            public int CooldownTurns { get; set; }
            public List<Act> Actions { get; set; } = new List<Act>();
            public string Quote { get; set; } = "";
            public string DisagreesWith { get; set; } = "";
            public string HeReview { get; set; } = Pending;

            /// <summary>Teaching (help levels new and experienced) as against advice and warnings (spec 7.1's button sets).</summary>
            public bool Teaching => Level == "new" || Level == "experienced";
            public bool Critical => Level == "critical";
            /// <summary>Its text may show in this language: English always, Hebrew once approved (OQ-4).</summary>
            public bool ApprovedIn(bool hebrew) => !hebrew || HeReview == Approved;
        }

        public sealed class Page
        {
            public string Id { get; set; } = "";
            public string Advisor { get; set; } = "";
            public string ShowMe { get; set; } = "";
            public List<string> Rules { get; set; } = new List<string>();
            public string HeReview { get; set; } = Pending;
            public bool ApprovedIn(bool hebrew) => !hebrew || HeReview == Approved;
        }

        /// <summary>What the checks accept: the game's signals, conditions, Show me targets, side screens, verified quotes
        /// and the files in StreamingAssets/advisors (Shipped), or a test's own lists.</summary>
        public sealed class Known
        {
            public IReadOnlyCollection<string> Signals = Array.Empty<string>(), Conditions = Array.Empty<string>(),
                Targets = Array.Empty<string>(), Screens = Array.Empty<string>(), Quotes = Array.Empty<string>();
            public Func<string, bool> StreamingFile = _ => false;
        }

        sealed class Doc
        {
            public List<Advisor> Advisors { get; set; } = new List<Advisor>();
            public List<Message> Messages { get; set; } = new List<Message>();
            public List<Page> Pages { get; set; } = new List<Page>();
        }

        public IReadOnlyList<Advisor> Advisors { get; }
        public IReadOnlyList<Message> Messages { get; }
        public IReadOnlyList<Page> Pages { get; }
        readonly Dictionary<string, Message> byId;
        readonly Dictionary<string, Page> pageById;

        AdvisorCatalog(Doc d)
        {
            Advisors = d.Advisors; Messages = d.Messages; Pages = d.Pages;
            byId = new Dictionary<string, Message>(StringComparer.Ordinal);
            foreach (var m in d.Messages) byId[m.Id] = m;   // a repeated id is a problem (Problems), the last one wins here
            pageById = new Dictionary<string, Page>(StringComparer.Ordinal);
            foreach (var p in d.Pages) pageById[p.Id] = p;
        }

        public Message? Find(string id) => byId.TryGetValue(id, out var m) ? m : null;
        public Page? FindPage(string id) => pageById.TryGetValue(id, out var p) ? p : null;
        public Advisor AdvisorOf(string id) => Advisors.First(a => a.Id == id);

        /// <summary>Parses the file. Throws InvalidOperationException naming it on malformed YAML or an unknown key.</summary>
        public static AdvisorCatalog Parse(string yaml)
        {
            try
            {
                return new AdvisorCatalog(new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .WithDuplicateKeyChecking().Build().Deserialize<Doc>(yaml ?? "") ?? new Doc());
            }
            catch (YamlDotNet.Core.YamlException e) { throw new InvalidOperationException($"{Path}: {e.Message}", e); }
        }

        /// <summary>The shipped file. A missing file is a build error, not a quiet game without advisors.</summary>
        public static AdvisorCatalog Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) throw new InvalidOperationException($"{Path} is missing");
            return Parse(asset.text);
        }

        /// <summary>The game's own lists, for the load test. Later tasks fill Targets (Task 8), Quotes (Task 19) and
        /// StreamingFile (Task 23).</summary>
        public static Known Shipped() => new Known
        {
            Signals = AdvisorSignals.Names.ToList(), Conditions = AdvisorSignals.Measures.Keys.ToList(),
            Screens = ScreenNav.Screens, Targets = AdvisorTargets.Names, Quotes = AdvisorQuotes.Ids,
        };

        /// <summary>Every problem, one line each (spec 4.1).</summary>
        public List<string> Problems(UiText en, UiText he, Known known)
        {
            var p = new List<string>();
            foreach (var id in AdvisorIds)
                if (Advisors.Count(a => a.Id == id) != 1) p.Add($"advisors: '{id}' must be listed once");
            foreach (var a in Advisors)
            {
                if (!AdvisorIds.Contains(a.Id)) p.Add($"advisors: unknown advisor '{a.Id}'");
                if (a.Person.Trim() == "") p.Add($"advisors: '{a.Id}' names no person");
            }
            foreach (var g in Messages.GroupBy(m => m.Id).Where(g => g.Count() > 1)) p.Add($"{g.Key}: listed more than once");
            foreach (var m in Messages)
            {
                var at = m.Id;
                if (!IdPattern.IsMatch(m.Id)) p.Add($"{at}: an id is lower-case letters, digits, '_' and '.'");
                if (!AdvisorIds.Contains(m.Advisor)) p.Add($"{at}: unknown advisor '{m.Advisor}'");
                if (!known.Signals.Contains(m.Signal)) p.Add($"{at}: unknown signal '{m.Signal}'");
                foreach (var k in m.When.Keys) if (!known.Conditions.Contains(k)) p.Add($"{at}: unknown condition '{k}'");
                foreach (var r in m.Requires) if (!byId.ContainsKey(r)) p.Add($"{at}: requires unknown message '{r}'");
                if (!Levels.Contains(m.Level)) p.Add($"{at}: level must be one of {string.Join(", ", Levels)}");
                if (m.Priority < 1 || m.Priority > 3) p.Add($"{at}: priority must be 1 to 3");
                if (m.Repeat == Once && m.CooldownTurns != 0) p.Add($"{at}: once has no cooldown_turns");
                else if (m.Repeat == Every && m.CooldownTurns < 1) p.Add($"{at}: every needs cooldown_turns of at least 1");
                else if (m.Repeat != Once && m.Repeat != Every) p.Add($"{at}: repeat must be once or every");
                if (m.Actions.Count > 2) p.Add($"{at}: at most two actions");
                foreach (var a in m.Actions) CheckAct(p, at, a, known);
                if (m.Quote != "" && !known.Quotes.Contains(m.Quote)) p.Add($"{at}: quote '{m.Quote}' is not on the verified list (spec 3.3)");
                if (m.Quote != "")
                {
                    Text(p, en, he, at, "advisor.quote." + m.Quote + ".text", 0);
                    Text(p, en, he, at, "advisor.quote." + m.Quote + ".source", 0);
                }
                if (m.DisagreesWith != "")
                {
                    var o = Find(m.DisagreesWith);
                    if (o == null || o.Advisor == m.Advisor || o.DisagreesWith != m.Id)
                        p.Add($"{at}: disagrees_with must name another advisor's message that names this one back");
                }
                if (m.HeReview != Pending && m.HeReview != Approved) p.Add($"{at}: he_review must be pending or approved");
                Text(p, en, he, at, "advisor." + m.Id + ".text", MaxWords);
            }
            if (Cycle() is string c) p.Add("requires has a cycle: " + c);
            foreach (var g in Pages.GroupBy(x => x.Id).Where(g => g.Count() > 1)) p.Add($"page {g.Key}: listed more than once");
            foreach (var page in Pages)
            {
                var at = "page " + page.Id;
                if (!IdPattern.IsMatch(page.Id)) p.Add($"{at}: an id is lower-case letters, digits, '_' and '.'");
                if (!AdvisorIds.Contains(page.Advisor)) p.Add($"{at}: unknown advisor '{page.Advisor}'");
                if (page.ShowMe != "" && !known.Targets.Contains(page.ShowMe)) p.Add($"{at}: unknown show_me target '{page.ShowMe}'");
                if (page.HeReview != Pending && page.HeReview != Approved) p.Add($"{at}: he_review must be pending or approved");
                Text(p, en, he, at, "advisor.page." + page.Id + ".title", 0);
                Text(p, en, he, at, "advisor.page." + page.Id + ".text", 0);
            }
            return p;
        }

        void CheckAct(List<string> p, string at, Act a, Known known)
        {
            int set = (a.ShowMe != "" ? 1 : 0) + (a.OpenScreen != "" ? 1 : 0) + (a.Encyclopedia != "" ? 1 : 0) + (a.OpenCard ? 1 : 0);
            if (set != 1) p.Add($"{at}: each action names exactly one of show_me, open_screen, encyclopedia, open_card");
            if (a.ShowMe != "" && !known.Targets.Contains(a.ShowMe)) p.Add($"{at}: unknown show_me target '{a.ShowMe}'");
            if (a.OpenScreen != "" && !known.Screens.Contains(a.OpenScreen)) p.Add($"{at}: unknown screen '{a.OpenScreen}'");
            if (a.Encyclopedia != "" && !pageById.ContainsKey(a.Encyclopedia)) p.Add($"{at}: unknown page '{a.Encyclopedia}'");
        }

        static void Text(List<string> p, UiText en, UiText he, string at, string key, int maxWords)
        {
            foreach (var (t, lang) in new[] { (en, "en"), (he, "he") })
            {
                if (!t.Has(key) || string.IsNullOrWhiteSpace(t[key])) { p.Add($"{at}: no {key} in strings.{lang}.yaml"); continue; }
                if (t[key].IndexOf('\u2014') >= 0) p.Add($"{at}: {key} in strings.{lang}.yaml has an em dash");
            }
            if (maxWords > 0 && en.Has(key) && en[key].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length > maxWords)
                p.Add($"{at}: {key} is over {maxWords} words");
        }

        /// <summary>The first requires cycle found, as "a -> b -> a", or null.</summary>
        string? Cycle()
        {
            var state = new Dictionary<string, int>(StringComparer.Ordinal);   // 1 visiting, 2 done
            var path = new List<string>();
            string? Visit(string id)
            {
                if (state.TryGetValue(id, out var s)) return s == 1 ? string.Join(" -> ", path.SkipWhile(x => x != id).Append(id)) : null;
                state[id] = 1; path.Add(id);
                foreach (var r in Find(id)?.Requires ?? new List<string>()) if (byId.ContainsKey(r) && Visit(r) is string c) return c;
                path.RemoveAt(path.Count - 1); state[id] = 2;
                return null;
            }
            foreach (var m in Messages) if (Visit(m.Id) is string c) return c;
            return null;
        }
    }
}
