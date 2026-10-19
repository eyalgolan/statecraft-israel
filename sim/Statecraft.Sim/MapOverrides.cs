using System;
using System.Collections.Generic;
using System.Linq;
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
}
