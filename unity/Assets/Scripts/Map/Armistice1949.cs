#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.Map
{
    /// <summary>The 1949 armistice lines as the map shows them: content/map/armistice_hexes_1949.yaml, which
    /// tools/armistice_bake.py snaps from the true lines (content/map/armistice_lines_1949.geojson;
    /// tools/armistice_digitise.py). Per front (Egypt, Lebanon, Jordan, Syria), the agreement and the date its line is shown
    /// from (null for a front without line data, which keeps the Mandate layer and is labelled so), and whether it is
    /// low-accuracy (Jordan, traced from a low-resolution scan); zones (the Demilitarized Zones),
    /// drawn over the sub-districts from their date (<see cref="MapRegions"/>); lines, named in the info panel on the hexes
    /// they pass through. Display only: the sim never reads the file.</summary>
    public sealed class Armistice1949
    {
        public const string FileName = "armistice_hexes_1949.yaml";

        /// <summary>The baked runtime form of the file the player reads at boot (tools/regions_bake.py; issue #321).</summary>
        public const string RuntimeFileName = "armistice_hexes_1949.tsv";

        /// <summary>Parses the runtime file made from <paramref name="yaml"/> with the same checks as <see cref="Parse"/>;
        /// throws InvalidDataException when it is of another version, made from another YAML, cut short or malformed.</summary>
        public static Armistice1949 ParseRuntime(string tsv, string yaml)
        {
            var d = RegionsRuntime.Read(tsv, yaml, RuntimeFileName);
            var doc = FromRuntime(d);
            d.Done();
            return Build(doc);
        }

        /// <summary>The boot read: the runtime file when it is for this YAML, else (one warning) the YAML.</summary>
        public static Armistice1949 ReadRuntimeOrYaml(string? tsv, string yaml, System.Action<string> warn) =>
            RegionsRuntime.ReadOrYaml(tsv, yaml, RuntimeFileName, d => { var doc = FromRuntime(d); d.Done(); return doc; }, Build, Parse, warn);

        static FileDoc FromRuntime(RegionsRuntime.Doc d)
        {
            var absent = new ItemRow();   // the YAML path's defaults for a field the row leaves out (review of #601)
            ItemRow Item(RegionsRuntime.Row r) => new ItemRow
            {
                Id = r.Int("id", 0), Key = r.Str("key", "")!, Front = r.Str("front", "")!, Name = r.Str("name", "")!,
                ShownFrom = r.Str("shown_from", absent.ShownFrom)!, Style = r.Str("style", absent.Style)!, Agreement = r.Str("agreement", absent.Agreement),
                Signed = r.Str("signed", null), LabelNote = r.Str("label_note", "")!, LowAccuracy = r.Bool("low_accuracy", false),
            };
            return new FileDoc
            {
                Layer = d.Str("layer", "")!, Title = d.Str("title", "")!, Line = d.Str("line", "")!, Label = d.Str("label", "")!,
                Fronts = d.Items("fronts", r => new FrontRow
                {
                    Key = r.Str("key", "")!, Name = r.Str("name", "")!, Agreement = r.Str("agreement", "")!, Signed = r.Str("signed", "")!,
                    ShownFrom = r.Str("shown_from", null), LowAccuracy = r.Bool("low_accuracy", false), Note = r.Str("note", "")!,
                }),
                Zones = d.Items("zones", Item), Lines = d.Items("lines", Item), NoHex = d.List("no_hex"),
                ZoneHexes = d.Ints("zone_hexes"), LineHexes = d.Ints("line_hexes"),
            };
        }

        public sealed class Front
        {
            public string Key = "", Name = "", Agreement = "", Note = ""; public DateTime Signed; public DateTime? ShownFrom;
            /// <summary>Traced from a low-resolution scan (Jordan, owner ruling 2026-10-11): said on the map label.</summary>
            public bool LowAccuracy;
            /// <summary>Whether the front's line is shown on <paramref name="date"/> (never, without line data).</summary>
            public bool InForce(DateTime date) => ShownFrom != null && date >= ShownFrom.Value;
        }

        public sealed class Item
        {
            public int Id; public string Key = "", Name = ""; public Front Front = null!; public DateTime ShownFrom; public BorderStyle Style;
            /// <summary>The item's own agreement and date (the 1950 modus vivendi is not its front's 1949 agreement), a note
            /// for the map label ("" for none), and whether it is low-accuracy.</summary>
            public string Agreement = "", LabelNote = ""; public DateTime Signed; public bool LowAccuracy;
            public bool InForce(DateTime date) => date >= ShownFrom;
        }

        public readonly string Title, Line, Label;
        public readonly IReadOnlyList<Front> Fronts;
        public readonly IReadOnlyList<Item> Zones, Lines;
        readonly Dictionary<(int q, int r), Item> zoneAt;
        readonly Dictionary<(int q, int r), List<Item>> linesAt;
        /// <summary>The zone and line hexes as read (a test aid: the runtime file must give the YAML's, issue #321).</summary>
        internal IReadOnlyDictionary<(int q, int r), Item> ZoneHexes => zoneAt;
        internal IReadOnlyDictionary<(int q, int r), List<Item>> LineHexes => linesAt;

        Armistice1949(string title, string line, string label, List<Front> fronts, List<Item> zones, List<Item> lines,
            Dictionary<(int q, int r), Item> zoneAt, Dictionary<(int q, int r), List<Item>> linesAt)
        {
            Title = title; Line = line; Label = label; Fronts = fronts; Zones = zones; Lines = lines; this.zoneAt = zoneAt; this.linesAt = linesAt;
        }

        public Front? FrontOf(string key) => Fronts.FirstOrDefault(f => f.Key == key);

        /// <summary>The zone drawn on the hex on <paramref name="date"/>, or null.</summary>
        public Item? ZoneAt(int q, int r, DateTime date) => zoneAt.TryGetValue((q, r), out var z) && z.InForce(date) ? z : null;

        /// <summary>The lines in force on <paramref name="date"/> that pass through the hex.</summary>
        public IEnumerable<Item> LinesAt(int q, int r, DateTime date) =>
            linesAt.TryGetValue((q, r), out var l) ? l.Where(x => x.InForce(date)) : Enumerable.Empty<Item>();

        /// <summary>The hexes of a zone (for composing the drawn layer).</summary>
        public IEnumerable<(int q, int r)> HexesOf(Item zone) => zoneAt.Where(kv => kv.Value == zone).Select(kv => kv.Key);

        sealed class FrontRow { public string Key { get; set; } = ""; public string Name { get; set; } = ""; public string Agreement { get; set; } = ""; public string Signed { get; set; } = ""; public string? ShownFrom { get; set; } public bool LowAccuracy { get; set; } public string Note { get; set; } = ""; }
        sealed class ItemRow { public int Id { get; set; } public string Key { get; set; } = ""; public string Front { get; set; } = ""; public string Name { get; set; } = ""; public string ShownFrom { get; set; } = ""; public string Style { get; set; } = "secondary"; public string? Agreement { get; set; } public string? Signed { get; set; } public string LabelNote { get; set; } = ""; public bool LowAccuracy { get; set; } }
        sealed class FileDoc
        {
            public string Layer { get; set; } = ""; public string Title { get; set; } = ""; public string Line { get; set; } = ""; public string Label { get; set; } = "";
            public List<FrontRow> Fronts { get; set; } = new List<FrontRow>(); public List<ItemRow> Zones { get; set; } = new List<ItemRow>(); public List<ItemRow> Lines { get; set; } = new List<ItemRow>();
            public List<string> NoHex { get; set; } = new List<string>();
            public List<List<int>> ZoneHexes { get; set; } = new List<List<int>>(); public List<List<int>> LineHexes { get; set; } = new List<List<int>>();
        }

        /// <summary>Parses the baked file. Every malformed input throws InvalidDataException naming the file and the entry.</summary>
        public static Armistice1949 Parse(string yaml)
        {
            if (string.IsNullOrWhiteSpace(yaml)) throw new InvalidDataException($"{FileName}: empty");
            FileDoc? doc;
            try { doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).WithDuplicateKeyChecking().Build().Deserialize<FileDoc>(yaml); }
            catch (YamlException e) { throw new InvalidDataException($"{FileName}: line {e.Start.Line}: {e.InnerException?.Message ?? e.Message}", e); }
            return Build(doc);
        }

        /// <summary>The checks and the object, from the file's rows (the YAML's or the runtime file's: RegionsRuntime).</summary>
        static Armistice1949 Build(FileDoc? doc)
        {
            if (doc == null || string.IsNullOrWhiteSpace(doc.Layer)) throw new InvalidDataException($"{FileName}: no layer");
            if (string.IsNullOrWhiteSpace(doc.Label)) throw new InvalidDataException($"{FileName}: no label (the layer is labelled on screen)");
            if (doc.Fronts.Count == 0) throw new InvalidDataException($"{FileName}: no fronts");
            var fronts = new List<Front>();
            foreach (var f in doc.Fronts)
            {
                if (string.IsNullOrWhiteSpace(f.Key) || string.IsNullOrWhiteSpace(f.Name) || string.IsNullOrWhiteSpace(f.Agreement))
                    throw new InvalidDataException($"{FileName}: front '{f.Key}': key, name and agreement are required");
                if (fronts.Any(x => x.Key == f.Key)) throw new InvalidDataException($"{FileName}: front '{f.Key}' twice");
                var signed = Date(f.Signed, $"front {f.Key} signed") ?? throw new InvalidDataException($"{FileName}: front {f.Key}: signed is required");
                var from = Date(f.ShownFrom, $"front {f.Key} shown_from");
                if (from == null && string.IsNullOrWhiteSpace(f.Note)) throw new InvalidDataException($"{FileName}: front {f.Key} has no line data and no note saying so");
                if (from != null && from < signed) throw new InvalidDataException($"{FileName}: front {f.Key}: shown_from is before the agreement was signed");
                fronts.Add(new Front { Key = f.Key, Name = f.Name, Agreement = f.Agreement, Signed = signed, ShownFrom = from, Note = f.Note, LowAccuracy = f.LowAccuracy });
            }
            List<Item> Items(List<ItemRow> rows, string kind)
            {
                var items = new List<Item>();
                foreach (var z in rows)
                {
                    var front = fronts.FirstOrDefault(f => f.Key == z.Front) ?? throw new InvalidDataException($"{FileName}: {kind} {z.Id} ({z.Key}): unknown front '{z.Front}'");
                    if (front.ShownFrom == null) throw new InvalidDataException($"{FileName}: {kind} {z.Id} ({z.Key}): its front {front.Key} has no line data");
                    if (string.IsNullOrWhiteSpace(z.Key) || string.IsNullOrWhiteSpace(z.Name)) throw new InvalidDataException($"{FileName}: {kind} {z.Id}: key and name are required");
                    if (items.Any(x => x.Id == z.Id)) throw new InvalidDataException($"{FileName}: {kind} id {z.Id} twice");
                    if (z.Id < 1 || z.Id > 999) throw new InvalidDataException($"{FileName}: {kind} id {z.Id} outside 1..999");
                    var from = Date(z.ShownFrom, $"{kind} {z.Key} shown_from") ?? throw new InvalidDataException($"{FileName}: {kind} {z.Key}: shown_from is required");
                    var style = z.Style switch
                    {
                        "primary" => BorderStyle.Primary, "secondary" => BorderStyle.Secondary,
                        _ => throw new InvalidDataException($"{FileName}: {kind} {z.Id} ({z.Key}): unknown style '{z.Style}'"),
                    };
                    var signed = Date(z.Signed, $"{kind} {z.Key} signed") ?? front.Signed;
                    if (from < signed) throw new InvalidDataException($"{FileName}: {kind} {z.Key}: shown_from is before its agreement was signed");
                    items.Add(new Item { Id = z.Id, Key = z.Key, Name = z.Name, Front = front, ShownFrom = from, Style = style,
                        Agreement = string.IsNullOrWhiteSpace(z.Agreement) ? front.Agreement : z.Agreement!, Signed = signed,
                        LabelNote = z.LabelNote, LowAccuracy = z.LowAccuracy || front.LowAccuracy });
                }
                return items;
            }
            var zones = Items(doc.Zones, "zone");
            var lines = Items(doc.Lines, "line");
            var zoneAt = new Dictionary<(int q, int r), Item>();
            foreach (var h in doc.ZoneHexes)
            {
                if (h == null || h.Count != 3) throw new InvalidDataException($"{FileName}: a zone hex row is not [q, r, zone]");
                var z = zones.FirstOrDefault(x => x.Id == h[2]) ?? throw new InvalidDataException($"{FileName}: hex ({h[0]}, {h[1]}) is in zone {h[2]}, which is not listed");
                if (zoneAt.ContainsKey((h[0], h[1]))) throw new InvalidDataException($"{FileName}: hex ({h[0]}, {h[1]}) is in two zones");
                zoneAt[(h[0], h[1])] = z;
            }
            var linesAt = new Dictionary<(int q, int r), List<Item>>();
            foreach (var h in doc.LineHexes)
            {
                if (h == null || h.Count != 3) throw new InvalidDataException($"{FileName}: a line hex row is not [q, r, line]");
                var l = lines.FirstOrDefault(x => x.Id == h[2]) ?? throw new InvalidDataException($"{FileName}: hex ({h[0]}, {h[1]}) is on line {h[2]}, which is not listed");
                if (!linesAt.TryGetValue((h[0], h[1]), out var list)) linesAt[(h[0], h[1])] = list = new List<Item>();
                if (!list.Contains(l)) list.Add(l);
            }
            return new Armistice1949(doc.Title, doc.Line, doc.Label, fronts, zones, lines, zoneAt, linesAt);
        }

        static DateTime? Date(string? text, string key)
        {
            if (string.IsNullOrWhiteSpace(text) || text == "null") return null;
            if (DateTime.TryParseExact(text, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d)) return d;
            throw new InvalidDataException($"{FileName}: {key} '{text}' is not a yyyy-MM-dd date");
        }
    }
}
