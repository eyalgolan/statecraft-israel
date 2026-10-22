#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.Map
{
    [Serializable]
    public sealed class MapEditorTerrainEdit
    {
        public int q, r;
        public string value = "";
    }

    [Serializable]
    public sealed class MapEditorControlEdit
    {
        public int q, r;
        public string period = "", value = "";
    }

    [Serializable]
    public sealed class MapEditorLocationEdit
    {
        public string id = "";
        public int q, r;
    }

    public sealed class MapEditorLocation
    {
        public readonly string Id, Name, Kind;
        public readonly int BaseQ, BaseR;

        public MapEditorLocation(string id, string name, string kind, int q, int r)
        { Id = id; Name = name; Kind = kind; BaseQ = q; BaseR = r; }
    }

    public sealed class MapEditorDependencies
    {
        public readonly IReadOnlyList<string> Cities, Sites, Corridors;

        public MapEditorDependencies(IEnumerable<string> cities, IEnumerable<string> sites, IEnumerable<string> corridors)
        {
            Cities = cities.OrderBy(x => x, StringComparer.Ordinal).ToList();
            Sites = sites.OrderBy(x => x, StringComparer.Ordinal).ToList();
            Corridors = corridors.OrderBy(x => x, StringComparer.Ordinal).ToList();
        }
    }

    /// <summary>The undoable data behind the Unity map editor. It edits only content/map/overrides.yaml; generated map
    /// inputs and historical evidence stay read-only. Unity serializes the three edit lists, so Undo restores every
    /// terrain, one-period control and location-position edit. The content root is serialized too, so after a domain
    /// reload <see cref="Restore"/> rebuilds the map around the surviving edits.</summary>
    public sealed class MapOverrideEditorDocument : ScriptableObject
    {
        [SerializeField] List<MapEditorTerrainEdit> terrain = new List<MapEditorTerrainEdit>();
        [SerializeField] List<MapEditorControlEdit> control = new List<MapEditorControlEdit>();
        [SerializeField] List<MapEditorLocationEdit> locations = new List<MapEditorLocationEdit>();

        [SerializeField] string contentRoot = "";
        string path = "", savedCanonical = "";
        readonly Dictionary<(int q, int r), MapHex> baseHexes = new Dictionary<(int, int), MapHex>();
        readonly Dictionary<string, MapEditorLocation> baseLocations = new Dictionary<string, MapEditorLocation>(StringComparer.Ordinal);
        readonly List<string> landBiomes = new List<string>();
        Content baseContent = null!;
        Control1948 controlFile = null!;

        public IReadOnlyCollection<MapHex> Hexes => baseHexes.Values;
        public IReadOnlyCollection<MapEditorLocation> Locations => baseLocations.Values;
        public IReadOnlyList<string> LandBiomes => landBiomes;
        public IReadOnlyList<Control1948.Period> Periods => controlFile.Periods;
        public IReadOnlyList<string> ControlValues => Control1948.EditorCodes;
        public bool HasUnsavedChanges => Canonical() != savedCanonical;
        public string OverridePath => path;
        /// <summary>False after a domain reload, which keeps the serialized edits but not the map read from disk.</summary>
        public bool IsLoaded => controlFile != null;

        public static MapOverrideEditorDocument Load(string root)
        {
            var result = CreateInstance<MapOverrideEditorDocument>();
            result.hideFlags = HideFlags.HideAndDontSave;
            result.Reload(root);
            return result;
        }

        public void Reload() => Reload(contentRoot);

        /// <summary>Reads the map and the saved override file again and keeps the current edits (after a domain reload).</summary>
        public void Restore() => ReadSaved(contentRoot);

        void Reload(string root) => SetFrom(ReadSaved(root));

        MapOverrides ReadSaved(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("The content directory is required", nameof(root));
            contentRoot = Path.GetFullPath(root);
            path = Path.Combine(contentRoot, MapOverrides.FileName);
            var eventRoot = Path.Combine(contentRoot, "events");
            var events = Directory.Exists(eventRoot)
                ? Directory.GetFiles(eventRoot, "*.yaml").Select(x => "events/" + Path.GetFileName(x)).ToList()
                : new List<string>();
            baseContent = ContentLoader.Load(relative =>
            {
                if (relative == MapOverrides.FileName) return null;
                var file = Path.Combine(contentRoot, relative);
                return File.Exists(file) ? File.ReadAllText(file) : null;
            }, events);
            controlFile = Control1948.Parse(File.ReadAllText(Path.Combine(contentRoot, "map", Control1948.FileName)));

            baseHexes.Clear();
            foreach (var hex in baseContent.Hexes) baseHexes[(hex.Q, hex.R)] = hex;
            landBiomes.Clear();
            landBiomes.AddRange(baseContent.Hexes.Where(x => x.Land).Select(x => x.Biome).Distinct().OrderBy(x => x, StringComparer.Ordinal));
            baseLocations.Clear();
            foreach (var item in baseContent.SettlementHexes)
                baseLocations[item.Id] = new MapEditorLocation(item.Id, baseContent.SettlementNames[item.Id], "settlement", item.Q, item.R);
            foreach (var item in baseContent.MapLocations)
                baseLocations[item.Id] = new MapEditorLocation(item.Id, item.Name, item.Kind, item.Q, item.R);

            var source = File.Exists(path) ? File.ReadAllText(path) : "";
            var parsed = MapOverrides.Parse(source);
            parsed.ValidateMap(baseContent.Hexes, baseLocations.Keys);
            parsed.ValidateControl(controlFile.EditorHexes, controlFile.Periods.Select(x => x.Id), Control1948.EditorCodes);
            savedCanonical = MapOverrides.Serialize(parsed);
            name = "Map overrides";
            return parsed;
        }

        public string TerrainAt(int q, int r)
        {
            if (!baseHexes.TryGetValue((q, r), out var hex)) throw new ArgumentException(FormattableString.Invariant($"unknown hex q={q} r={r}"));
            return terrain.FirstOrDefault(x => x.q == q && x.r == r)?.value ?? hex.Biome;
        }

        public string ControlAt(int q, int r, string period) =>
            control.FirstOrDefault(x => x.q == q && x.r == r && x.period == period)?.value
            ?? controlFile.EditorCodeAt(q, r, period);

        public (int q, int r) LocationAt(string id)
        {
            if (!baseLocations.TryGetValue(id, out var item)) throw new ArgumentException($"unknown location id '{id}'", nameof(id));
            var edit = locations.FirstOrDefault(x => x.id == id);
            return edit == null ? (item.BaseQ, item.BaseR) : (edit.q, edit.r);
        }

        public bool IsLand(int q, int r) => baseHexes.TryGetValue((q, r), out var hex) && hex.Land;

        public void SetTerrain(int q, int r, string value)
        {
            if (!baseHexes.TryGetValue((q, r), out var hex)) throw new ArgumentException(FormattableString.Invariant($"unknown hex q={q} r={r}"));
            if (!hex.Land) throw new ArgumentException(FormattableString.Invariant($"hex q={q} r={r} is not a land hex"));
            if (!landBiomes.Contains(value)) throw new ArgumentException($"unknown land terrain '{value}'", nameof(value));
            terrain.RemoveAll(x => x.q == q && x.r == r);
            if (value != hex.Biome) terrain.Add(new MapEditorTerrainEdit { q = q, r = r, value = value });
        }

        public void SetControl(int q, int r, string period, string value)
        {
            var original = controlFile.EditorCodeAt(q, r, period);
            if (!Control1948.EditorCodes.Contains(value)) throw new ArgumentException($"unknown control value '{value}'", nameof(value));
            control.RemoveAll(x => x.q == q && x.r == r && x.period == period);
            if (value != original) control.Add(new MapEditorControlEdit { q = q, r = r, period = period, value = value });
        }

        public void SetLocation(string id, int q, int r)
        {
            if (!baseLocations.TryGetValue(id, out var item)) throw new ArgumentException($"unknown location id '{id}'", nameof(id));
            if (!baseHexes.TryGetValue((q, r), out var hex)) throw new ArgumentException(FormattableString.Invariant($"unknown hex q={q} r={r}"));
            if (!hex.Land) throw new ArgumentException(FormattableString.Invariant($"hex q={q} r={r} is not a land hex"));
            locations.RemoveAll(x => x.id == id);
            if (q != item.BaseQ || r != item.BaseR) locations.Add(new MapEditorLocationEdit { id = id, q = q, r = r });
        }

        public MapEditorDependencies Dependencies(string id)
        {
            if (!baseLocations.TryGetValue(id, out var item)) throw new ArgumentException($"unknown location id '{id}'", nameof(id));
            var current = LocationAt(id);
            var cities = baseContent.Cities.Where(x => x.Id == id).Select(x => $"city '{x.Id}' uses this location identity");
            var sites = baseContent.Sites.Where(x => x.Id == id || x.City == id
                    || (x.Q == item.BaseQ && x.R == item.BaseR) || (x.Q == current.q && x.R == current.r))
                .Select(x => $"site '{x.Id}'" + (x.City == id ? $" is bound to city '{id}'" : $" shares hex q={x.Q} r={x.R}"));
            var corridors = baseContent.Corridors.SelectMany(x => x.Segments.Select((segment, i) => (corridor: x, segment, number: i + 1)))
                .Where(x => x.segment.Hexes.Any(h => (h[0] == item.BaseQ && h[1] == item.BaseR) || (h[0] == current.q && h[1] == current.r)))
                .Select(x => $"corridor '{x.corridor.Id}' segment {x.number} (to {x.segment.To}) crosses this location's hex; its hexes stay as traced");
            return new MapEditorDependencies(cities, sites, corridors);
        }

        public bool Save(out string error)
        {
            try
            {
                var value = ToOverrides();
                value.ValidateMap(baseContent.Hexes, baseLocations.Keys);
                value.ValidateControl(controlFile.EditorHexes, controlFile.Periods.Select(x => x.Id), Control1948.EditorCodes);
                var canonical = MapOverrides.Serialize(value);
                if (canonical == savedCanonical) { error = ""; return true; }
                // Validate the candidate through the game's full content loader before replacing the saved map: a valid
                // land-hex move can still break a dependent record (for example a settlement moved onto a site).
                var eventRoot = Path.Combine(contentRoot, "events");
                var events = Directory.Exists(eventRoot)
                    ? Directory.GetFiles(eventRoot, "*.yaml").Select(x => "events/" + Path.GetFileName(x)).ToList()
                    : new List<string>();
                ContentLoader.Load(relative =>
                {
                    if (relative == MapOverrides.FileName) return canonical;
                    var file = Path.Combine(contentRoot, relative);
                    return File.Exists(file) ? File.ReadAllText(file) : null;
                }, events);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
                try
                {
                    File.WriteAllText(temporary, canonical);
                    var check = MapOverrides.Parse(File.ReadAllText(temporary));
                    check.ValidateMap(baseContent.Hexes, baseLocations.Keys);
                    check.ValidateControl(controlFile.EditorHexes, controlFile.Periods.Select(x => x.Id), Control1948.EditorCodes);
                    if (File.Exists(path)) File.Replace(temporary, path, null);
                    else File.Move(temporary, path);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
                savedCanonical = canonical;
                error = "";
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                || exception is ArgumentException || exception is ContentException)
            {
                error = exception.Message;
                return false;
            }
        }

        public MapOverrides ToOverrides() => new MapOverrides
        {
            Terrain = terrain.Select(x => new TerrainOverride { Hex = new MapHexRef { Q = x.q, R = x.r }, Value = x.value }).ToList(),
            Control = control.Select(x => new ControlOverride { Hex = new MapHexRef { Q = x.q, R = x.r }, Period = x.period, Value = x.value }).ToList(),
            Locations = locations.Select(x => new LocationOverride { Id = x.id, Hex = new MapHexRef { Q = x.q, R = x.r } }).ToList(),
        };

        string Canonical() => MapOverrides.Serialize(ToOverrides());

        void SetFrom(MapOverrides value)
        {
            terrain = value.Terrain.Select(x => new MapEditorTerrainEdit { q = x.Hex.Q, r = x.Hex.R, value = x.Value }).ToList();
            control = value.Control.Select(x => new MapEditorControlEdit { q = x.Hex.Q, r = x.Hex.R, period = x.Period, value = x.Value }).ToList();
            locations = value.Locations.Select(x => new MapEditorLocationEdit { id = x.Id, q = x.Hex.Q, r = x.Hex.R }).ToList();
        }
    }
}
#endif
