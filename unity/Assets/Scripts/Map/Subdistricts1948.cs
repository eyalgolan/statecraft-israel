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
    /// <summary>The sub-districts of Mandatory Palestine in force on 15 May 1948, the game's start, as the map draws them:
    /// content/map/subdistrict_hexes_1948.yaml, which tools/borders_bake.py snaps from the true digitised lines
    /// (content/map/subdistricts_1948.geojson; tools/borders_digitise.py). Each land hex is in one sub-district, or in the
    /// "outside" region (style none: the sub-districts' bands run along the Mandate frontier, nothing beyond it); sea and
    /// lake hexes are in none, so the coast is not outlined. Display only: the sim never reads the file. Owner ruling
    /// (2026-10-11): it shows from the game's start (<see cref="ShownFrom"/>), labelled on screen with <see cref="Label"/>
    /// (GameRoot's "map-layer" label above the minimap, while the layer is drawn). <see cref="Fronts"/>: each front's switch
    /// date, its agreement's, from which <see cref="MapRegions"/> draws that front's armistice features over this layer
    /// (null: no line data, the front never switches); <see cref="ShownUntil"/> is the layer's end as a whole, null while
    /// the armistice lines leave gaps (the layer stays as the base under them).</summary>
    public sealed class Subdistricts1948
    {
        public const string FileName = "subdistrict_hexes_1948.yaml";

        public sealed class Region
        {
            public int Id; public string Key = "", Name = "", District = ""; public BorderStyle Style;
        }

        public readonly RegionLayer Layer;
        /// <summary>What the map shows, and where the true line is (the file's "line").</summary>
        public readonly string Title, Line, Label;
        public readonly DateTime ShownFrom;
        /// <summary>The first date the layer is no longer shown (null: until replaced, no end set yet).</summary>
        public readonly DateTime? ShownUntil;
        /// <summary>Per front (key), the first date that front switches: from then MapRegions draws its armistice features over
        /// this layer, which stays as the base; null: never (no line data). Empty when the file names no fronts.</summary>
        public readonly IReadOnlyDictionary<string, DateTime?> Fronts;
        readonly Dictionary<int, Region> regions;

        Subdistricts1948(RegionLayer layer, string title, string line, string label, DateTime from, DateTime? until, Dictionary<int, Region> regions,
            Dictionary<string, DateTime?> fronts)
        { Layer = layer; Title = title; Line = line; Label = label; ShownFrom = from; ShownUntil = until; this.regions = regions; Fronts = fronts; }

        /// <summary>Whether the front's armistice line has replaced this layer there by <paramref name="date"/>.</summary>
        public bool FrontReplaced(string front, DateTime date) => Fronts.TryGetValue(front, out var u) && u != null && date >= u.Value;

        /// <summary>Whether the map shows the layer on <paramref name="date"/> (the turn's date).</summary>
        public bool ShowsOn(DateTime date) => date >= ShownFrom && (ShownUntil == null || date < ShownUntil.Value);

        public IReadOnlyCollection<Region> Regions => regions.Values;

        /// <summary>The sub-district a hex is drawn in, or null (sea, lake, or outside Mandatory Palestine).</summary>
        public Region? SubdistrictAt(int q, int r) =>
            regions.TryGetValue(Layer.RegionOf(q, r), out var x) && x.Style != BorderStyle.None ? x : null;

        sealed class FileRow { public int Id { get; set; } public string Key { get; set; } = ""; public string Name { get; set; } = ""; public string District { get; set; } = ""; public string Style { get; set; } = ""; }
        sealed class FrontRow { public string Key { get; set; } = ""; public string? Until { get; set; } }
        sealed class FileDoc { public string Layer { get; set; } = ""; public string Title { get; set; } = ""; public string Line { get; set; } = ""; public string Label { get; set; } = ""; public string ShownFrom { get; set; } = ""; public string? ShownUntil { get; set; } public List<FrontRow> Fronts { get; set; } = new List<FrontRow>(); public List<FileRow> Regions { get; set; } = new List<FileRow>(); public List<List<int>> Hexes { get; set; } = new List<List<int>>(); }

        /// <summary>Parses the baked file. Every malformed input throws InvalidDataException naming the file and the entry.</summary>
        public static Subdistricts1948 Parse(string yaml)
        {
            if (string.IsNullOrWhiteSpace(yaml)) throw new InvalidDataException($"{FileName}: empty");
            FileDoc? doc;
            try { doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).WithDuplicateKeyChecking().Build().Deserialize<FileDoc>(yaml); }
            catch (YamlException e) { throw new InvalidDataException($"{FileName}: line {e.Start.Line}: {e.InnerException?.Message ?? e.Message}", e); }
            if (doc == null || string.IsNullOrWhiteSpace(doc.Layer)) throw new InvalidDataException($"{FileName}: no layer");
            if (doc.Regions.Count == 0) throw new InvalidDataException($"{FileName}: no regions");
            if (string.IsNullOrWhiteSpace(doc.Label)) throw new InvalidDataException($"{FileName}: no label (the layer is labelled on screen)");
            var from = Date(doc.ShownFrom, "shown_from") ?? throw new InvalidDataException($"{FileName}: shown_from is required");
            var until = Date(doc.ShownUntil, "shown_until");
            if (until != null && until <= from) throw new InvalidDataException($"{FileName}: shown_until is not after shown_from");
            var fronts = new Dictionary<string, DateTime?>();
            foreach (var f in doc.Fronts)
            {
                if (string.IsNullOrWhiteSpace(f.Key)) throw new InvalidDataException($"{FileName}: a front has no key");
                if (fronts.ContainsKey(f.Key)) throw new InvalidDataException($"{FileName}: front '{f.Key}' twice");
                var u = Date(f.Until, $"front {f.Key} until");
                if (u != null && u <= from) throw new InvalidDataException($"{FileName}: front {f.Key}: until is not after shown_from");
                fronts[f.Key] = u;
            }
            var regions = new Dictionary<int, Region>(); var styles = new Dictionary<int, BorderStyle>();
            foreach (var r in doc.Regions)
            {
                var style = r.Style switch
                {
                    "primary" => BorderStyle.Primary, "secondary" => BorderStyle.Secondary, "none" => BorderStyle.None,
                    _ => throw new InvalidDataException($"{FileName}: region {r.Id} ({r.Key}): unknown style '{r.Style}'"),
                };
                if (string.IsNullOrWhiteSpace(r.Key) || string.IsNullOrWhiteSpace(r.Name)) throw new InvalidDataException($"{FileName}: region {r.Id}: key and name are required");
                if (regions.ContainsKey(r.Id)) throw new InvalidDataException($"{FileName}: region id {r.Id} twice");
                regions[r.Id] = new Region { Id = r.Id, Key = r.Key, Name = r.Name, District = r.District, Style = style };
                styles[r.Id] = style;
            }
            var hexes = new Dictionary<(int q, int r), int>();
            foreach (var h in doc.Hexes)
            {
                if (h == null || h.Count != 3) throw new InvalidDataException($"{FileName}: a hex row is not [q, r, region]");
                if (!regions.ContainsKey(h[2])) throw new InvalidDataException($"{FileName}: hex ({h[0]}, {h[1]}) is in region {h[2]}, which is not listed");
                if (hexes.ContainsKey((h[0], h[1]))) throw new InvalidDataException($"{FileName}: hex ({h[0]}, {h[1]}) twice");
                hexes[(h[0], h[1])] = h[2];
            }
            RegionLayer layer;
            // The label goes in the game's UI (GameRoot, the "map-layer" label), not the 3D banner, which the tilt-shift blurs.
            try { layer = new RegionLayer(doc.Layer, hexes, styles); }
            catch (ArgumentException e) { throw new InvalidDataException($"{FileName}: {e.Message}", e); }
            return new Subdistricts1948(layer, doc.Title, doc.Line, doc.Label, from, until, regions, fronts);
        }

        static DateTime? Date(string? text, string key)
        {
            if (string.IsNullOrWhiteSpace(text) || text == "null") return null;
            if (DateTime.TryParseExact(text, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d)) return d;
            throw new InvalidDataException($"{FileName}: {key} '{text}' is not a yyyy-MM-dd date");
        }

        /// <summary>Parses the file and, when <paramref name="date"/> is in its window, draws it on the built map with its label
        /// (MapBorders.Use).</summary>
        public static Subdistricts1948 Show(string yaml, DateTime date)
        {
            var m = Parse(yaml);
            m.Apply(date);
            return m;
        }

        bool? shown;
        /// <summary>Whether the last <see cref="Apply"/> drew the layer.</summary>
        public bool Shown => shown == true;
        /// <summary>Draws the layer if <paramref name="date"/> is in its window and takes it off once it is not. A call that
        /// changes nothing leaves the map alone (so a test pattern drawn over it in a development build stays), except that a
        /// rebuilt map, which draws no layer at all, gets the layer again.</summary>
        public void Apply(DateTime date)
        {
            bool on = ShowsOn(date);
            if (shown == on && !(on && MapBorders.Current == null)) return;
            shown = on;
            if (on) MapBorders.Use(Layer);
            else if (MapBorders.Current == Layer) MapBorders.Use(null);
        }
    }
}
