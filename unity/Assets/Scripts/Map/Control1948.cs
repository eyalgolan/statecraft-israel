#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.Map
{
    /// <summary>Who held which areas, 1948-49, as the map draws them (owner rulings 2026-10-14): content/map/control_hexes_1948_49.yaml,
    /// which tools/control_bake.py snaps from the true polygons (content/map/control_1948_49.geojson; tools/control_digitise.py:
    /// the front lines of Israel State Archives map 6158/1 and, from each agreement's date, the sides of the 1949 armistice
    /// lines). Per period (a dated span), every land hex of Mandatory Palestine has one code: the Israeli side, the other side
    /// (with its holder, for the info panel only), contested, no-man's land, a demilitarized zone, or no colour (no sourced
    /// line). The colours claim military control on a date, nothing else. Display only: the sim never reads the file.</summary>
    public sealed class Control1948
    {
        public const string FileName = "control_hexes_1948_49.yaml";

        /// <summary>The baked runtime form of the file the player reads at boot (tools/regions_bake.py; issue #321).</summary>
        public const string RuntimeFileName = "control_hexes_1948_49.tsv";

        /// <summary>Parses the runtime file made from <paramref name="yaml"/> with the same checks as <see cref="Parse"/>;
        /// throws InvalidDataException when it is of another version, made from another YAML, cut short or malformed.</summary>
        public static Control1948 ParseRuntime(string tsv, string yaml)
        {
            var d = RegionsRuntime.Read(tsv, yaml, RuntimeFileName);
            var doc = FromRuntime(d);
            d.Done();
            return Build(doc);
        }

        /// <summary>The boot read: the runtime file when it is for this YAML, else (one warning) the YAML.</summary>
        public static Control1948 ReadRuntimeOrYaml(string? tsv, string yaml, System.Action<string> warn) =>
            RegionsRuntime.ReadOrYaml(tsv, yaml, RuntimeFileName, d => { var doc = FromRuntime(d); d.Done(); return doc; }, Build, Parse, warn);

        static FileDoc FromRuntime(RegionsRuntime.Doc d) => new FileDoc
        {
            Layer = d.Str("layer", "")!, Title = d.Str("title", "")!, Meaning = d.Str("meaning", "")!, Line = d.Str("line", "")!,
            ShownFrom = d.Str("shown_from", "")!, Codes = d.Str("codes", "")!,
            Periods = d.Items("periods", r => new PeriodRow
            {
                Id = r.Str("id", "")!, From = r.Str("from", "")!, Until = r.Str("until", null), Legend = r.Str("legend", "")!,
                LineDate = r.Str("line_date", "")!, Fronts = r.List("fronts"),
            }),
            Hexes = d.Strings("hexes"),
        };

        /// <summary>What a hex is on a date. Holder: the other side's holder when the sources name one, else null.</summary>
        /// <summary>None: a hex of Mandatory Palestine with no sourced line on the date (no colour); OffData: a hex outside the
        /// data (beyond the frontier, the sea).</summary>
        public enum Side { None, Israel, Arab, Contested, NoMansLand, Dmz, OffData }

        /// <summary>Held: a 1948 front line; Adjusted: 15 May - 10 June 1948, the 9 July line with the changes before 11 June
        /// in grey (owner decision of 16 October 2026, #674); Mixed: some fronts' armistice lines; Armistice: every front's.</summary>
        public enum Legend { Held, Adjusted, Mixed, Armistice }

        public sealed class Period
        {
            public string Id = ""; public DateTime From; public DateTime? Until; public Legend Legend;
            /// <summary>The date the source line shows ("1948-07-09", or a month "1948-10"), and the fronts whose armistice line
            /// is in force.</summary>
            public string LineDate = ""; public IReadOnlyList<string> Fronts = Array.Empty<string>();
            public bool Covers(DateTime d) => d >= From && (Until == null || d < Until.Value);
        }

        public const int IsraelId = 1, ArabId = 2, NeutralId = 3, OutsideId = 4;
        static readonly Dictionary<char, (Side side, string? holder)> CodeOf = new Dictionary<char, (Side, string?)>
        {
            ['I'] = (Side.Israel, null), ['E'] = (Side.Arab, "egypt"), ['J'] = (Side.Arab, "jordan"),
            ['S'] = (Side.Arab, "syria"), ['V'] = (Side.Arab, "ala"), ['M'] = (Side.Arab, null),
            ['A'] = (Side.Arab, null), ['c'] = (Side.Contested, null), ['m'] = (Side.NoMansLand, null), ['z'] = (Side.Dmz, null),
            ['-'] = (Side.None, null),
        };

        public readonly string Title, Line, Meaning;
        public readonly DateTime ShownFrom;
        public readonly IReadOnlyList<Period> Periods;
        readonly Dictionary<(int q, int r), string> codes;
        /// <summary>Each hex's codes as read (a test aid: the runtime file must give the YAML's, issue #321).</summary>
        internal IReadOnlyDictionary<(int q, int r), string> Codes => codes;
        readonly RegionLayer?[] layers;
        IReadOnlyCollection<(int q, int r)> outside = Array.Empty<(int, int)>();

        Control1948(string title, string line, string meaning, DateTime from, List<Period> periods, Dictionary<(int q, int r), string> codes)
        { Title = title; Line = line; Meaning = meaning; ShownFrom = from; Periods = periods; this.codes = codes; layers = new RegionLayer?[periods.Count]; }

        /// <summary>The period covering <paramref name="date"/>, or null (before the first: the sub-district layer).</summary>
        public Period? PeriodOn(DateTime date) => Periods.FirstOrDefault(p => p.Covers(date));

        /// <summary>The hex's side and holder on <paramref name="date"/> (Side.OffData off the data, Side.None outside its
        /// periods).</summary>
        public (Side side, string? holder) At(int q, int r, DateTime date)
        {
            int i = Index(date);
            if (i < 0) return (Side.None, null);
            if (!codes.TryGetValue((q, r), out var c)) return (Side.OffData, null);
            return CodeOf[c[i]];
        }

        int Index(DateTime date)
        {
            for (int i = 0; i < Periods.Count; i++) if (Periods[i].Covers(date)) return i;
            return -1;
        }

        /// <summary>The hexes outside Mandatory Palestine (the sub-district layer's "outside" region): a region of style none,
        /// so the bands run along the frontier and nothing is drawn beyond it, as for the sub-districts.</summary>
        public void SetOutside(IEnumerable<(int q, int r)> hexes) { outside = hexes.ToList(); Array.Clear(layers, 0, layers.Length); }

        /// <summary>The layer drawn on <paramref name="date"/> (null before the first period), built once per period: Israel
        /// primary, the other side secondary, contested, no-man's land and demilitarized zones neutral, no colour left out (no
        /// band, and it stops its neighbours' bands: the edge of the sourced data is not a front line).</summary>
        public RegionLayer? LayerOn(DateTime date)
        {
            int i = Index(date);
            if (i < 0) return null;
            if (layers[i] != null) return layers[i];
            var hexes = new Dictionary<(int q, int r), int>();
            foreach (var kv in codes)
            {
                var side = CodeOf[kv.Value[i]].side;
                int id = side switch { Side.Israel => IsraelId, Side.Arab => ArabId, Side.Contested => NeutralId, Side.NoMansLand => NeutralId, Side.Dmz => NeutralId, _ => 0 };
                if (id != 0) hexes[kv.Key] = id;
            }
            foreach (var h in outside) if (!hexes.ContainsKey(h)) hexes[h] = OutsideId;
            var styles = new Dictionary<int, BorderStyle> { [IsraelId] = BorderStyle.Primary, [ArabId] = BorderStyle.Secondary, [NeutralId] = BorderStyle.Neutral, [OutsideId] = BorderStyle.None };
            return layers[i] = new RegionLayer("control_1948_49:" + Periods[i].Id, hexes, styles);
        }

        sealed class PeriodRow { public string Id { get; set; } = ""; public string From { get; set; } = ""; public string? Until { get; set; } public string Legend { get; set; } = ""; public string LineDate { get; set; } = ""; public List<string> Fronts { get; set; } = new List<string>(); }
        sealed class FileDoc { public string Layer { get; set; } = ""; public string Title { get; set; } = ""; public string Meaning { get; set; } = ""; public string Line { get; set; } = ""; public string ShownFrom { get; set; } = ""; public string Codes { get; set; } = ""; public List<PeriodRow> Periods { get; set; } = new List<PeriodRow>(); public List<List<string>> Hexes { get; set; } = new List<List<string>>(); }

        /// <summary>Parses the baked file. Every malformed input throws InvalidDataException naming the file and the entry.</summary>
        public static Control1948 Parse(string yaml)
        {
            if (string.IsNullOrWhiteSpace(yaml)) throw new InvalidDataException($"{FileName}: empty");
            FileDoc? doc;
            try { doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).WithDuplicateKeyChecking().Build().Deserialize<FileDoc>(yaml); }
            catch (YamlException e) { throw new InvalidDataException($"{FileName}: line {e.Start.Line}: {e.InnerException?.Message ?? e.Message}", e); }
            return Build(doc);
        }

        /// <summary>The checks and the object, from the file's rows (the YAML's or the runtime file's: RegionsRuntime).</summary>
        static Control1948 Build(FileDoc? doc)
        {
            if (doc == null || string.IsNullOrWhiteSpace(doc.Layer)) throw new InvalidDataException($"{FileName}: no layer");
            if (doc.Periods.Count == 0) throw new InvalidDataException($"{FileName}: no periods");
            var periods = new List<Period>();
            foreach (var p in doc.Periods)
            {
                var from = Date(p.From, $"period {p.Id} from") ?? throw new InvalidDataException($"{FileName}: period {p.Id}: from is required");
                var until = Date(p.Until, $"period {p.Id} until");
                if (until != null && until <= from) throw new InvalidDataException($"{FileName}: period {p.Id}: until is not after from");
                if (periods.Count > 0 && periods[periods.Count - 1].Until != from) throw new InvalidDataException($"{FileName}: period {p.Id} does not start where {periods[periods.Count - 1].Id} ends");
                var legend = p.Legend switch { "held" => Legend.Held, "adjusted" => Legend.Adjusted, "mixed" => Legend.Mixed, "armistice" => Legend.Armistice,
                    _ => throw new InvalidDataException($"{FileName}: period {p.Id}: unknown legend '{p.Legend}'") };
                periods.Add(new Period { Id = p.Id, From = from, Until = until, Legend = legend, LineDate = p.LineDate, Fronts = p.Fronts.ToList() });
            }
            if (periods[periods.Count - 1].Until != null) throw new InvalidDataException($"{FileName}: the last period must run on (until: null)");
            var codes = new Dictionary<(int q, int r), string>();
            foreach (var h in doc.Hexes)
            {
                if (h == null || h.Count != 3) throw new InvalidDataException($"{FileName}: a hex row is not [q, r, codes]");
                if (!int.TryParse(h[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var q) || !int.TryParse(h[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r))
                    throw new InvalidDataException($"{FileName}: a hex row's q, r are not integers");
                if (h[2].Length != periods.Count) throw new InvalidDataException($"{FileName}: hex ({q}, {r}) has {h[2].Length} codes for {periods.Count} periods");
                foreach (var c in h[2]) if (!CodeOf.ContainsKey(c)) throw new InvalidDataException($"{FileName}: hex ({q}, {r}): unknown code '{c}'");
                if (codes.ContainsKey((q, r))) throw new InvalidDataException($"{FileName}: hex ({q}, {r}) twice");
                codes[(q, r)] = h[2];
            }
            var shown = Date(doc.ShownFrom, "shown_from") ?? throw new InvalidDataException($"{FileName}: shown_from is required");
            if (shown != periods[0].From) throw new InvalidDataException($"{FileName}: shown_from is not the first period's start");
            return new Control1948(doc.Title, doc.Line, doc.Meaning, shown, periods, codes);
        }

        static DateTime? Date(string? text, string key)
        {
            if (string.IsNullOrWhiteSpace(text) || text == "null") return null;
            if (DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d;
            throw new InvalidDataException($"{FileName}: {key} '{text}' is not a yyyy-MM-dd date");
        }

        /// <summary>The source line's date as players read it, in the UI's language through <paramref name="text"/>
        /// (date.day_month_year, date.month_year, date.month_1..12): "9 July 1948" / "9 ביולי 1948", or "October 1948" /
        /// "אוקטובר 1948" for a month.</summary>
        public static string LineDateText(string lineDate, Func<string, string> text)
        {
            string F(string key, params object[] a) => string.Format(CultureInfo.InvariantCulture, text(key), a);
            if (DateTime.TryParseExact(lineDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return DateText(d, text);
            if (DateTime.TryParseExact(lineDate, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var m))
                return F("date.month_year", text("date.month_" + m.Month), m.Year);
            return lineDate;
        }

        /// <summary>A day as players read it, in the UI's language through <paramref name="text"/> (date.day_month_year,
        /// date.month_1..12): "15 May 1948" / "15 במאי 1948". The map's line date and the top bar's date plate (#687).</summary>
        public static string DateText(DateTime d, Func<string, string> text) =>
            string.Format(CultureInfo.InvariantCulture, text("date.day_month_year"), d.Day, text("date.month_" + d.Month), d.Year);
    }
}
