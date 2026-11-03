using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Sim
{
    public sealed class MapHexRef
    {
        public int Q { get; set; }
        public int R { get; set; }
    }

    public sealed class TerrainOverride
    {
        public MapHexRef Hex { get; set; } = null!;
        public string Value { get; set; } = "";
    }

    public sealed class ControlOverride
    {
        public MapHexRef Hex { get; set; } = null!;
        public string Period { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public sealed class LocationOverride
    {
        public string Id { get; set; } = "";
        public MapHexRef Hex { get; set; } = null!;
    }

    /// <summary>content/map/overrides.yaml: the map editor's authored changes (#1009; owner rulings of 18 October 2026),
    /// applied after every generated map input so that rerunning a generator keeps them. A terrain override sets a land
    /// hex's biome; a control override replaces one period's code of one hex in the map's control file (Control1948) and nothing
    /// else ("one period at a time"); a location override moves a game settlement (settlements_1948.yaml) or a map
    /// location (locations_1948.yaml) to another land hex, keeping its id, names, sourced coordinates and sources. The sim
    /// checks and applies terrain and locations (ContentLoader); the map, which alone reads the control file, checks and
    /// applies control (Unity's MapScene).</summary>
    public sealed class MapOverrides
    {
        public const string FileName = "map/overrides.yaml";
        public List<TerrainOverride> Terrain { get; set; } = null!;
        public List<ControlOverride> Control { get; set; } = null!;
        public List<LocationOverride> Locations { get; set; } = null!;

        static readonly IDeserializer Yaml = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithDuplicateKeyChecking().Build();

        /// <summary>The map editor's one canonical representation: schema header, fixed sections and stable ordinal
        /// ordering. Parse is the only reader and this is the only writer, so the editor cannot drift from the format the
        /// sim and map consume. The input is not mutated.</summary>
        public static string Serialize(MapOverrides value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var copy = Parse(SerializeUnvalidated(value));
            var text = new StringBuilder("# yaml-language-server: $schema=../schema/map-overrides.schema.json\n");
            Section(text, "terrain", copy.Terrain.OrderBy(x => x.Hex.R).ThenBy(x => x.Hex.Q), x =>
                FormattableString.Invariant($"{{ hex: {{ q: {x.Hex.Q}, r: {x.Hex.R} }}, value: {Quote(x.Value)} }}"));
            Section(text, "control", copy.Control.OrderBy(x => x.Hex.R).ThenBy(x => x.Hex.Q).ThenBy(x => x.Period, StringComparer.Ordinal), x =>
                FormattableString.Invariant($"{{ hex: {{ q: {x.Hex.Q}, r: {x.Hex.R} }}, period: {Quote(x.Period)}, value: {Quote(x.Value)} }}"));
            Section(text, "locations", copy.Locations.OrderBy(x => x.Id, StringComparer.Ordinal), x =>
                FormattableString.Invariant($"{{ id: {Quote(x.Id)}, hex: {{ q: {x.Hex.Q}, r: {x.Hex.R} }} }}"));
            return text.ToString();
        }

        static string SerializeUnvalidated(MapOverrides value)
        {
            if (value.Terrain == null) throw new ContentException(FileName, "terrain", "must be a list");
            if (value.Control == null) throw new ContentException(FileName, "control", "must be a list");
            if (value.Locations == null) throw new ContentException(FileName, "locations", "must be a list");
            for (int i = 0; i < value.Terrain.Count; i++) Serializable(value.Terrain[i], value.Terrain[i]?.Hex, $"terrain[{i}]");
            for (int i = 0; i < value.Control.Count; i++) Serializable(value.Control[i], value.Control[i]?.Hex, $"control[{i}]");
            for (int i = 0; i < value.Locations.Count; i++) Serializable(value.Locations[i], value.Locations[i]?.Hex, $"locations[{i}]");
            var text = new StringBuilder();
            Section(text, "terrain", value.Terrain, x =>
                FormattableString.Invariant($"{{ hex: {{ q: {x.Hex.Q}, r: {x.Hex.R} }}, value: {Quote(x.Value ?? "")} }}"));
            Section(text, "control", value.Control, x =>
                FormattableString.Invariant($"{{ hex: {{ q: {x.Hex.Q}, r: {x.Hex.R} }}, period: {Quote(x.Period ?? "")}, value: {Quote(x.Value ?? "")} }}"));
            Section(text, "locations", value.Locations, x =>
                FormattableString.Invariant($"{{ id: {Quote(x.Id ?? "")}, hex: {{ q: {x.Hex.Q}, r: {x.Hex.R} }} }}"));
            return text.ToString();
        }

        static void Serializable(object? row, MapHexRef? hex, string at)
        {
            if (row == null) throw new ContentException(FileName, at, "must be an object");
            if (hex == null) throw new ContentException(FileName, at + ".hex", "required");
        }

        static void Section<T>(StringBuilder text, string name, IEnumerable<T> rows, Func<T, string> row)
        {
            var list = rows.ToList();
            if (list.Count == 0) { text.Append(name).Append(": []\n"); return; }
            text.Append(name).Append(":\n");
            foreach (var item in list) text.Append("  - ").Append(row(item)).Append('\n');
        }

        static string Quote(string value) => "'" + value.Replace("'", "''") + "'";

        /// <summary>Parses the file and checks its shape: the three lists, a hex on every entry and no target twice. A
        /// missing or blank file (null, "") has no overrides. Throws ContentException naming the file and field.</summary>
        public static MapOverrides Parse(string? yaml, string file = FileName)
        {
            if (string.IsNullOrWhiteSpace(yaml)) return new MapOverrides
            {
                Terrain = new List<TerrainOverride>(), Control = new List<ControlOverride>(), Locations = new List<LocationOverride>()
            };
            MapOverrides result;
            try { result = Yaml.Deserialize<MapOverrides>(yaml) ?? throw new ContentException(file, "(file)", "empty file"); }
            catch (YamlException e) { throw new ContentException(file, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
            if (result.Terrain == null) throw new ContentException(file, "terrain", "must be a list");
            if (result.Control == null) throw new ContentException(file, "control", "must be a list");
            if (result.Locations == null) throw new ContentException(file, "locations", "must be a list");

            var terrain = new HashSet<(int, int)>();
            for (int i = 0; i < result.Terrain.Count; i++)
            {
                var at = FormattableString.Invariant($"terrain[{i}]");
                var x = result.Terrain[i] ?? throw new ContentException(file, at, "must be an object");
                Required(file, at, x.Hex);
                if (x.Value == null) x.Value = "";
                if (!terrain.Add((x.Hex.Q, x.Hex.R))) throw new ContentException(file, at + ".hex", "a second override of this hex's terrain");
            }
            var control = new HashSet<(int, int, string)>();
            for (int i = 0; i < result.Control.Count; i++)
            {
                var at = FormattableString.Invariant($"control[{i}]");
                var x = result.Control[i] ?? throw new ContentException(file, at, "must be an object");
                Required(file, at, x.Hex);
                if (x.Period == null) x.Period = "";
                if (x.Value == null) x.Value = "";
                if (!control.Add((x.Hex.Q, x.Hex.R, x.Period))) throw new ContentException(file, at + ".period", "a second override of this hex's control in this period");
            }
            var locations = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < result.Locations.Count; i++)
            {
                var at = FormattableString.Invariant($"locations[{i}]");
                var x = result.Locations[i] ?? throw new ContentException(file, at, "must be an object");
                Required(file, at, x.Hex);
                if (x.Id == null) x.Id = "";
                if (!locations.Add(x.Id)) throw new ContentException(file, at + ".id", "a second override of this location");
            }
            return result;
        }

        static void Required(string file, string at, MapHexRef? hex)
        {
            if (hex == null) throw new ContentException(file, at + ".hex", "required");
        }

        /// <summary>Checks terrain and location overrides against the map: a terrain override names a land hex of
        /// <paramref name="hexes"/> and one of its land biomes; a location override names a known location id and a land
        /// hex. With no <paramref name="locationIds"/> (the map, which has no roster of the map locations) the ids are not
        /// checked; the content loader checks them. Throws ContentException naming the file and field.</summary>
        public void ValidateMap(IReadOnlyCollection<MapHex> hexes, IEnumerable<string>? locationIds, string file = FileName)
        {
            var byHex = new Dictionary<(int, int), MapHex>();
            foreach (var h in hexes) byHex[(h.Q, h.R)] = h;
            var landBiomes = new SortedSet<string>(hexes.Where(h => h.Land).Select(h => h.Biome), StringComparer.Ordinal);
            var known = locationIds == null ? null : new HashSet<string>(locationIds, StringComparer.Ordinal);
            for (int i = 0; i < Terrain.Count; i++)
            {
                var x = Terrain[i]; var at = FormattableString.Invariant($"terrain[{i}]");
                LandHex(file, at + ".hex", x.Hex, byHex);
                if (!landBiomes.Contains(x.Value))
                    throw new ContentException(file, at + ".value", $"unknown terrain value '{x.Value}' ({string.Join(", ", landBiomes)})");
            }
            for (int i = 0; i < Locations.Count; i++)
            {
                var x = Locations[i]; var at = FormattableString.Invariant($"locations[{i}]");
                if (known != null && !known.Contains(x.Id)) throw new ContentException(file, at + ".id", $"unknown location id '{x.Id}'");
                LandHex(file, at + ".hex", x.Hex, byHex);
            }
        }

        static void LandHex(string file, string field, MapHexRef hex, Dictionary<(int, int), MapHex> byHex)
        {
            if (!byHex.TryGetValue((hex.Q, hex.R), out var h))
                throw new ContentException(file, field, FormattableString.Invariant($"unknown hex q={hex.Q} r={hex.R}"));
            if (!h.Land) throw new ContentException(file, field, FormattableString.Invariant($"hex q={hex.Q} r={hex.R} is not a land hex"));
        }

        /// <summary>Checks control overrides against the control file: a hex it has, one of its period ids and one of its
        /// codes. Throws ContentException naming the file and field.</summary>
        public void ValidateControl(IEnumerable<(int q, int r)> controlHexes, IEnumerable<string> periods, IEnumerable<string> codes,
            string file = FileName)
        {
            var hexes = new HashSet<(int, int)>(controlHexes);
            var knownPeriods = new HashSet<string>(periods, StringComparer.Ordinal);
            var knownCodes = new HashSet<string>(codes, StringComparer.Ordinal);
            for (int i = 0; i < Control.Count; i++)
            {
                var x = Control[i]; var at = FormattableString.Invariant($"control[{i}]");
                if (!hexes.Contains((x.Hex.Q, x.Hex.R)))
                    throw new ContentException(file, at + ".hex", FormattableString.Invariant($"unknown hex q={x.Hex.Q} r={x.Hex.R} (not in the control file)"));
                if (!knownPeriods.Contains(x.Period)) throw new ContentException(file, at + ".period", $"unknown period '{x.Period}'");
                if (!knownCodes.Contains(x.Value)) throw new ContentException(file, at + ".value", $"unknown control value '{x.Value}'");
            }
        }

        /// <summary>The overridden hex of location <paramref name="id"/>, if it has one.</summary>
        public bool TryLocation(string id, out int q, out int r)
        {
            var x = Locations.FirstOrDefault(v => v.Id == id);
            q = x?.Hex.Q ?? 0; r = x?.Hex.R ?? 0;
            return x != null;
        }

        /// <summary>Hex (<paramref name="q"/>, <paramref name="r"/>)'s control codes, one per period of
        /// <paramref name="periodIds"/>, with this file's control overrides for that hex: each replaces the one code of its
        /// period and nothing else (owner, 18 October 2026: "One period at a time"). After ValidateControl.</summary>
        public string ApplyControl(int q, int r, IReadOnlyList<string> periodIds, string codes)
        {
            if (codes.Length != periodIds.Count)
                throw new ArgumentException(FormattableString.Invariant($"{codes.Length} codes for {periodIds.Count} periods"), nameof(codes));
            var chars = codes.ToCharArray();
            foreach (var x in Control)
                if (x.Hex.Q == q && x.Hex.R == r)
                {
                    int i = -1;
                    for (int k = 0; k < periodIds.Count; k++) if (periodIds[k] == x.Period) { i = k; break; }
                    if (i < 0) throw new ArgumentException($"unknown period '{x.Period}' (ValidateControl first)", nameof(periodIds));
                    chars[i] = x.Value[0];
                }
            return new string(chars);
        }

        /// <summary>Applies the terrain and location overrides to loaded content (after ValidateMap): the hexes' biomes, the
        /// game settlements' hexes and the map locations' hexes. Sourced coordinates are left as they are.</summary>
        public void ApplyTo(Content c)
        {
            foreach (var x in Terrain) c.HexAt(x.Hex.Q, x.Hex.R)!.Biome = x.Value;
            foreach (var s in c.SettlementHexes)
                if (TryLocation(s.Id, out var q, out var r)) { s.Q = q; s.R = r; }
            foreach (var l in c.MapLocations)
                if (TryLocation(l.Id, out var q, out var r)) { l.Q = q; l.R = r; }
        }
    }
    /// <summary>Authoring-only envelope. No loader or runtime caller consumes this format.
    /// Region, field and facility semantics remain with their owning issues. No evidence or campaign state lives here.</summary>
    public sealed class MapEditorOverlay
    {
        // Strict end assertion also rejects a final LF (unlike the .NET $ anchor).
        public const string TokenPattern = @"^[^\s\u0000-\u001f\u007f]+(?![\s\S])";
        public const string VersionId = "map-editor-0.3";
        public string Version { get; set; } = "";
        public string Ruleset { get; set; } = "";
        public bool? Draft { get; set; }
        public string BaseRevision { get; set; } = "";
        public string BaseDigest { get; set; } = "";
        public MapOverrides Overlay { get; set; } = null!;
        public List<MapEditorWorkable> Workable { get; set; } = null!;
        public List<MapEditorRegion> Regions { get; set; } = null!;
        public List<MapEditorInfrastructure> Infrastructure { get; set; } = null!;
        public List<MapEditorOffset> Offsets { get; set; } = null!;

        /// <summary>The hex an offset must stay inside: content/map/hexes.json's pointy-top hex, whose `hex_size_km` (4.4,
        /// since #1796) is the circumradius, centre to corner. A test pins this to that file.</summary>
        public const double OffsetHexCircumradiusM = 4400;

        // R-MEDIT-003: no receipt or export until accepted dependency adapters and base-manifest contract exist.
        [YamlIgnore] public bool CanExport => false;

        static readonly IDeserializer Reader = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance).WithDuplicateKeyChecking().Build();

        public static MapEditorOverlay Parse(string source, string selectedRuleset)
        {
            if (selectedRuleset != SaveGame.Gdd03Ruleset)
                throw Error("MAPEDIT_RULESET", "ruleset", "select an explicit gdd-0.3 authoring context");
            MapEditorOverlay value;
            try
            {
                CheckPayloadCoordinates(source);
                value = Reader.Deserialize<MapEditorOverlay>(source)
                    ?? throw Error("MAPEDIT_SHAPE", "(file)", "an envelope is required");
            }
            catch (YamlException e)
            {
                throw Error("MAPEDIT_SHAPE", FormattableString.Invariant($"line {e.Start.Line}"), e.InnerException?.Message ?? e.Message);
            }
            Validate(value);
            return value;
        }

        static void CheckPayloadCoordinates(string source)
        {
            var stream = new YamlDotNet.RepresentationModel.YamlStream();
            stream.Load(new System.IO.StringReader(source));
            if (stream.Documents.Count != 1) throw Error("MAPEDIT_SHAPE", "(file)", "one YAML document is required");
            if (!(stream.Documents[0].RootNode is YamlDotNet.RepresentationModel.YamlMappingNode root)) return;
            if (!root.Children.TryGetValue(new YamlDotNet.RepresentationModel.YamlScalarNode("overlay"), out var payload)
                || !(payload is YamlDotNet.RepresentationModel.YamlMappingNode overlay)) return;
            foreach (var section in overlay.Children)
            {
                if (!(section.Value is YamlDotNet.RepresentationModel.YamlSequenceNode rows)) continue;
                foreach (var row in rows.Children)
                {
                    if (!(row is YamlDotNet.RepresentationModel.YamlMappingNode mapping)) continue;
                    if (!mapping.Children.TryGetValue(new YamlDotNet.RepresentationModel.YamlScalarNode("hex"), out var hex)
                        || !(hex is YamlDotNet.RepresentationModel.YamlMappingNode coordinates)
                        || !HasInteger(coordinates, "q") || !HasInteger(coordinates, "r"))
                        throw Error("MAPEDIT_SHAPE", "overlay." + section.Key + ".hex", "both axial coordinates are required");
                }
            }
        }

        static bool HasInteger(YamlDotNet.RepresentationModel.YamlMappingNode map, string key) =>
            map.Children.TryGetValue(new YamlDotNet.RepresentationModel.YamlScalarNode(key), out var node)
            && node is YamlDotNet.RepresentationModel.YamlScalarNode scalar
            && int.TryParse(scalar.Value, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out _);

        static void Validate(MapEditorOverlay value)
        {
            if (value.Version != VersionId) throw Error("MAPEDIT_VERSION", "version", "unsupported editor envelope version");
            if (value.Ruleset != SaveGame.Gdd03Ruleset) throw Error("MAPEDIT_RULESET", "ruleset", "expected gdd-0.3");
            if (value.Draft == null) throw Error("MAPEDIT_SHAPE", "draft", "an explicit boolean is required");
            if (!IsToken(value.BaseRevision) || value.BaseDigest == null
                || value.BaseDigest.Length != 64 || value.BaseDigest.Any(c => !(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')))
                throw Error("MAPEDIT_BASE", "base", "a single-line revision token and lowercase SHA-256 base-input digest are required");
            if (value.Overlay == null || value.Workable == null || value.Regions == null || value.Infrastructure == null
                || value.Offsets == null)
                throw Error("MAPEDIT_SHAPE", "(file)", "overlay and all edit lists are required");
            // Reuse legacy duplicate/shape checks without changing their behavior or serialization.
            try { MapOverrides.Serialize(value.Overlay); }
            catch (ContentException e) { throw Error("MAPEDIT_SHAPE", "overlay." + e.Field, e.Message); }
            foreach (var row in value.Overlay.Terrain) Nonempty(row.Value, "overlay.terrain.value");
            foreach (var row in value.Overlay.Control)
            { Nonempty(row.Period, "overlay.control.period"); Nonempty(row.Value, "overlay.control.value"); }
            foreach (var row in value.Overlay.Locations)
                if (row.Id == null || !System.Text.RegularExpressions.Regex.IsMatch(row.Id, "^[a-z][a-z0-9_]*$"))
                    throw Error("MAPEDIT_SHAPE", "overlay.locations.id", "a stable location id is required");
            CheckRows(value.Workable, "workable", x => HexKey(x.Hex));
            foreach (var row in value.Workable)
                if (row.Value == null) throw Error("MAPEDIT_SHAPE", "workable.value", "an explicit boolean is required");
            CheckRows(value.Regions, "regions", x => HexKey(x.Hex));
            foreach (var row in value.Regions) Token(row.RegionId, "regions.region_id");
            CheckRows(value.Infrastructure, "infrastructure", x => x.Id);
            foreach (var row in value.Infrastructure)
            {
                Token(row.Id, "infrastructure.id"); Token(row.Kind, "infrastructure.kind");
                Token(row.ParentId, "infrastructure.parent_id");
            }
            CheckRows(value.Offsets, "offsets", x => x.Id + "/" + x.Kind);
            foreach (var row in value.Offsets)
            {
                if (row.Id == null || !System.Text.RegularExpressions.Regex.IsMatch(row.Id, "^[a-z][a-z0-9_]*$"))
                    throw Error("MAPEDIT_SHAPE", "offsets.id", "a stable location id is required");
                if (row.Kind != "visual" && row.Kind != "label")
                    throw Error("MAPEDIT_SHAPE", "offsets.kind", "visual or label is required");
                if (row.EastM == null || row.NorthM == null)
                    throw Error("MAPEDIT_SHAPE", "offsets", "whole metres east_m and north_m are required");
            }
        }

        static ContentException Error(string code, string field, string message) =>
            new ContentException(MapOverrides.FileName, field, code + ": " + message);

        static void Nonempty(string value, string field)
        {
            if (string.IsNullOrEmpty(value)) throw Error("MAPEDIT_SHAPE", field, "a nonempty value is required");
        }

        static bool IsToken(string value) => value != null
            && System.Text.RegularExpressions.Regex.IsMatch(value, TokenPattern);

        static void Token(string value, string field)
        {
            if (!IsToken(value)) throw Error("MAPEDIT_SHAPE", field, "a single-line owning-registry token without whitespace or controls is required");
        }

        static string HexKey(MapEditorHex hex) => FormattableString.Invariant($"{hex.Q},{hex.R}");

        static void CheckRows<T>(List<T> rows, string field, Func<T, string> key) where T : MapEditorHexEdit
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Count; i++)
            {
                var at = FormattableString.Invariant($"{field}[{i}]");
                var row = rows[i];
                if (row == null || row.Hex == null || row.Hex.Q == null || row.Hex.R == null)
                    throw Error("MAPEDIT_SHAPE", at + ".hex", "an axial hex with both integer coordinates is required");
                if (!seen.Add(key(row))) throw Error("MAPEDIT_DUPLICATE", at, "a second override of the same identity");
            }
        }

        static string Quote(string value) => "'" + value.Replace("'", "''") + "'";
        static string Hex(MapEditorHex h) => FormattableString.Invariant($"{{ q: {h.Q}, r: {h.R} }}");
        static void Rows<T>(StringBuilder text, string field, IEnumerable<T> rows, Func<T, string> format)
        {
            var list = rows.ToList();
            if (list.Count == 0) { text.Append(field).Append(": []\n"); return; }
            text.Append(field).Append(":\n");
            foreach (var row in list) text.Append("  - ").Append(format(row)).Append('\n');
        }

        /// <summary>Stable authoring text, not a validation receipt. Never mutates or reorders caller-owned lists.</summary>
        public static string Serialize(MapEditorOverlay value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            // Validate object callers too; never use a culture-sensitive intermediate serializer.
            Validate(value);
            var copy = value;
            var text = new StringBuilder("# yaml-language-server: $schema=../schema/map-editor-overlay.schema.json\n");
            text.Append("version: ").Append(Quote(copy.Version)).Append("\nruleset: 'gdd-0.3'\ndraft: ")
                .Append(copy.Draft == true ? "true" : "false").Append("\nbase_revision: ").Append(Quote(copy.BaseRevision))
                .Append("\nbase_digest: ").Append(Quote(copy.BaseDigest)).Append("\noverlay:\n");
            foreach (var line in MapOverrides.Serialize(copy.Overlay).Split('\n').Skip(1))
                if (line.Length != 0) text.Append("  ").Append(line).Append('\n');
            Rows(text, "workable", copy.Workable.OrderBy(x => x.Hex.R).ThenBy(x => x.Hex.Q),
                x => "{ hex: " + Hex(x.Hex) + ", value: " + (x.Value == true ? "true" : "false") + " }");
            Rows(text, "regions", copy.Regions.OrderBy(x => x.Hex.R).ThenBy(x => x.Hex.Q),
                x => "{ hex: " + Hex(x.Hex) + ", region_id: " + Quote(x.RegionId) + " }");
            Rows(text, "infrastructure", copy.Infrastructure.OrderBy(x => x.Id, StringComparer.Ordinal),
                x => "{ id: " + Quote(x.Id) + ", hex: " + Hex(x.Hex) + ", kind: " + Quote(x.Kind) + ", parent_id: " + Quote(x.ParentId) + " }");
            Rows(text, "offsets", copy.Offsets.OrderBy(x => x.Id, StringComparer.Ordinal).ThenBy(x => x.Kind, StringComparer.Ordinal),
                x => "{ id: " + Quote(x.Id) + ", kind: " + Quote(x.Kind) + ", hex: " + Hex(x.Hex)
                    + FormattableString.Invariant($", east_m: {x.EastM}, north_m: {x.NorthM} }}"));
            return text.ToString();
        }

        /// <summary>Infrastructure diagnostics. Caller supplies current base identity; this is not Content.Hash.
        /// Missing domain adapters are explicit errors, even on an empty candidate. With the loaded <paramref name="content"/>
        /// the corridor, force-anchor, target and objective bindings are checked, and each broken one is a blocking row naming
        /// its record (Q58, #1721 slice A). This overlay's location moves apply to the target and objective-site checks only;
        /// corridors and anchors are checked against the map alone, since no overlay edit changes them. No automatic repair is
        /// performed: no corridor, anchor, target or site is moved or rerouted.</summary>
        /// <param name="content">The base content loaded with no editor overlay applied. Omitted, the four adapters stay
        /// unavailable.</param>
        public IReadOnlyList<MapEditorDiagnostic> Diagnostics(string currentRevision, string currentDigest, Content? content = null)
        {
            var result = new List<MapEditorDiagnostic>();
            if (BaseRevision != currentRevision || BaseDigest != currentDigest)
                result.Add(new MapEditorDiagnostic("MAPEDIT_BASE_CONFLICT", "base",
                    "overlay base " + BaseRevision + "/" + BaseDigest + "; current base " + currentRevision + "/" + currentDigest,
                    "Compare base inputs and deliberately rebase; retain the current edits."));
            if (Draft == true)
                result.Add(new MapEditorDiagnostic("MAPEDIT_DRAFT", "draft", "draft input cannot be canonical content",
                    "Resolve all diagnostics before requesting canonical export."));
            var unavailable = content == null
                ? new[] { "regions", "fields", "facilities", "corridors", "anchors", "targets", "objectives", "base_manifest" }
                : new[] { "regions", "fields", "facilities", "base_manifest" };   // post-MVP adapters and the manifest contract
            if (content != null) Dependencies(content, result);
            // Q58/Q59: an offset that leaves its own hex only warns; the authored point is kept as written.
            foreach (var o in Offsets.Where(o => !InsideHex(o.EastM!.Value, o.NorthM!.Value)))
                result.Add(new MapEditorDiagnostic("MAPEDIT_OFFSET_OUTSIDE_HEX", $"offsets[{o.Id}/{o.Kind}]",
                    FormattableString.Invariant($"{o.Kind} offset east_m={o.EastM} north_m={o.NorthM} leaves its hex q={o.Hex.Q} r={o.Hex.R}"),
                    "Bring the offset back inside its hex; the location and its sourced point stay where they are.", blocking: false));
            foreach (var field in unavailable)
                result.Add(new MapEditorDiagnostic("MAPEDIT_DEPENDENCY_UNAVAILABLE", field, "accepted validation adapter is not installed",
                    "Integrate the owning contract before canonical save or export."));
            return result.OrderBy(x => x.Field, StringComparer.Ordinal).ThenBy(x => x.Code, StringComparer.Ordinal).ToList().AsReadOnly();
        }

        /// <summary>Pointy-top hex centred on the origin, north along a vertex: inside when within the flat east/west sides
        /// (the inradius) and the four slanted sides. The boundary counts as inside.</summary>
        static bool InsideHex(double east, double north)
        {
            double inradius = Math.Sqrt(3) / 2 * OffsetHexCircumradiusM, x = Math.Abs(east), y = Math.Abs(north);
            return x <= inradius && x / 2 + y * Math.Sqrt(3) / 2 <= inradius;
        }

        /// <summary>The bindings the sim's loader would refuse (ContentLoader ValidateCorridors, StartingForces.Read,
        /// ValidateSettlementTargets, ValidateObjectiveConditions and ValidateSites' settlement rule), re-checked with this
        /// overlay's location moves applied to the settlements. Reads <paramref name="c"/> only.</summary>
        void Dependencies(Content c, List<MapEditorDiagnostic> result)
        {
            void Broken(string field, string message) => result.Add(new MapEditorDiagnostic("MAPEDIT_DEPENDENCY_BROKEN", field, message,
                "Resolve the binding explicitly; the editor never reroutes a sourced line or moves an anchor, target or site."));
            bool Land(int q, int r) => c.HexAt(q, r)?.Land == true;
            var settlement = new Dictionary<string, (int Q, int R)>(StringComparer.Ordinal);
            foreach (var h in c.SettlementHexes)
                settlement[h.Id] = Overlay.TryLocation(h.Id, out var q, out var r) ? (q, r) : (h.Q, h.R);

            foreach (var x in c.Corridors)
            {
                List<int>? prev = null;
                for (int i = 0; i < x.Segments.Count; i++)
                    foreach (var h in x.Segments[i].Hexes)
                    {
                        var at = FormattableString.Invariant($"segment {i + 1} (to {x.Segments[i].To}) hex q={h[0]} r={h[1]}");
                        if (!Land(h[0], h[1])) Broken($"corridors[{x.Id}]", at + " is not a land hex of the map");
                        else if (prev != null && MapActions.Distance(prev[0], prev[1], h[0], h[1]) > 1)
                            Broken($"corridors[{x.Id}]", at + FormattableString.Invariant($" is not next to q={prev[0]} r={prev[1]}"));
                        prev = h;
                    }
            }
            foreach (var f in c.StartingForces)
            {
                if (!Land(f.Q, f.R))
                    Broken($"anchors[{f.Row}]", FormattableString.Invariant($"anchor {f.Anchor} hex q={f.Q} r={f.R} is not a land hex of the map"));
                if (f.Script is (int sq, int sr) && !Land(sq, sr))
                    Broken($"anchors[{f.Row}]", FormattableString.Invariant($"scripted anchor hex q={sq} r={sr} is not a land hex of the map"));
            }
            foreach (var t in c.SettlementTargets)
            {
                var at = $"targets[{t.Id}]";
                if (!Land(t.Q, t.R)) Broken(at, FormattableString.Invariant($"target hex q={t.Q} r={t.R} is not a land hex of the map"));
                if (!settlement.TryGetValue(t.Id, out var s)) { Broken(at, $"settlement '{t.Id}' has no hex"); continue; }
                var bound = ContentLoader.TargetOnANeighbour.Contains(t.Id)
                    ? Movement.Neighbours.Any(d => s.Q + d.Q == t.Q && s.R + d.R == t.R)
                    : s.Q == t.Q && s.R == t.R;
                if (!bound)
                    Broken(at, FormattableString.Invariant($"target hex q={t.Q} r={t.R} no longer matches settlement {t.Id} at q={s.Q} r={s.R}"));
            }
            foreach (var o in c.Objectives)
            {
                var w = o.CompletesWhen;
                if (w == null) continue;
                var at = $"objectives[{o.Id}]";
                if (w.CorridorComplete != "" && c.Corridors.All(x => x.Id != w.CorridorComplete))
                    Broken(at, $"unknown corridor '{w.CorridorComplete}'");
                foreach (var id in w.SitesBuilt.Prepend(w.SiteBuilt).Where(x => x != ""))
                {
                    var site = c.Sites.FirstOrDefault(x => x.Id == id);
                    if (site == null) { Broken(at, $"unknown site '{id}'"); continue; }
                    if (!Projects.IsYishuvSite(site)) Broken(at, $"site '{id}' cannot be built as a yishuv");
                    foreach (var kv in settlement.Where(kv => kv.Value == (site.Q, site.R)).OrderBy(kv => kv.Key, StringComparer.Ordinal))
                        Broken(at, FormattableString.Invariant($"site '{id}' hex q={site.Q} r={site.R} now holds the settlement {kv.Key}"));
                }
            }
        }
    }

    public sealed class MapEditorHex
    {
        public int? Q { get; set; }
        public int? R { get; set; }
    }
    public abstract class MapEditorHexEdit
    {
        public MapEditorHex Hex { get; set; } = null!;
    }
    public sealed class MapEditorWorkable : MapEditorHexEdit
    {
        public bool? Value { get; set; }
    }
    public sealed class MapEditorRegion : MapEditorHexEdit
    {
        public string RegionId { get; set; } = "";
    }
    public sealed class MapEditorInfrastructure : MapEditorHexEdit
    {
        public string Id { get; set; } = "";
        public string Kind { get; set; } = "";
        public string ParentId { get; set; } = "";
    }
    /// <summary>A visual or label offset (Q59): the location's axial hex plus whole metres east and north of its centre.</summary>
    public sealed class MapEditorOffset : MapEditorHexEdit
    {
        public string Id { get; set; } = "";
        public string Kind { get; set; } = "";
        public int? EastM { get; set; }
        public int? NorthM { get; set; }
    }
    public sealed class MapEditorDiagnostic
    {
        public string Code { get; }
        public string File => MapOverrides.FileName;
        public string Field { get; }
        public string Message { get; }
        public string Action { get; }
        /// <summary>False only for a warning, which does not block export (Q58: visual and label offsets only warn).</summary>
        public bool Blocking { get; }
        public MapEditorDiagnostic(string code, string field, string message, string action, bool blocking = true)
        { Code = code; Field = field; Message = message; Action = action; Blocking = blocking; }
    }

}
