#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Statecraft.HexTiles.Painter
{
    /// <summary>One palette entry: a prototype or a unit type, by its registered id.</summary>
    public sealed class HexPaletteEntry
    {
        public string Id = "", Title = "", Kind = "", Region = "";
        public bool IsUnit;
        public override string ToString() => $"{Id} ({Title})";
    }

    /// <summary>A palette group: a heading ("farm, north", "characters") and its entries in order.</summary>
    public sealed class HexPaletteGroup
    {
        public string Name = "";
        public List<HexPaletteEntry> Entries = new List<HexPaletteEntry>();
    }

    /// <summary>The Hex Painter's palette and brushes, apart from its window so tests can drive them. The groups come from
    /// what is registered (HexPrototype's Kind and Region, a unit type's members), never from a list kept here, so a new
    /// prototype or unit file shows up in the palette by itself.</summary>
    public static class HexPalette
    {
        /// <summary>The region group of a prototype drawn for anywhere (Region "").</summary>
        public const string AnyRegion = "any";
        public const string Characters = "characters", Vehicles = "vehicles", OtherUnits = "other units";

        /// <summary>Every registered prototype, grouped by Kind (HexPrototypes.Kinds order; none or unknown is "other"),
        /// then by Region (HexPrototypes.Regions order, then "any"), ids in ordinal order inside a group.</summary>
        public static List<HexPaletteGroup> Tiles() => GroupTiles(HexPrototypes.All.Keys.Select(id =>
        {
            var info = HexPrototypes.Info(id);
            return new HexPaletteEntry { Id = id, Title = HexPrototypes.Create(id).Title, Kind = info.Kind, Region = info.Region };
        }));

        /// <summary>The grouping of <see cref="Tiles"/>, for any entries.</summary>
        public static List<HexPaletteGroup> GroupTiles(IEnumerable<HexPaletteEntry> entries)
        {
            string Kind(HexPaletteEntry e) => HexPrototypes.Kinds.Contains(e.Kind) ? e.Kind : "other";
            string Region(HexPaletteEntry e) => HexPrototypes.Regions.Contains(e.Region) ? e.Region : AnyRegion;
            int RegionOrder(string region) => region == AnyRegion ? HexPrototypes.Regions.Length : Array.IndexOf(HexPrototypes.Regions, region);
            return entries.GroupBy(e => (kind: Kind(e), region: Region(e)))
                .OrderBy(g => Array.IndexOf(HexPrototypes.Kinds, g.Key.kind)).ThenBy(g => RegionOrder(g.Key.region))
                .Select(g => new HexPaletteGroup
                {
                    Name = g.Key.region == AnyRegion ? g.Key.kind : $"{g.Key.kind}, {g.Key.region}",
                    Entries = g.OrderBy(e => e.Id, StringComparer.Ordinal).ToList(),
                }).ToList();
        }

        /// <summary>Every registered unit type: characters (a squad of HexUnitScale.Characters figures), then vehicles
        /// (one vehicle each), told apart by the members its Create() returns. Anything else is "other units", so the
        /// palette still lists every type once. Call it once per window, not per frame: it creates each unit.</summary>
        public static List<HexPaletteGroup> Units() => GroupUnits(HexUnitTypes.All.Keys.Select(id =>
        {
            var type = HexUnitTypes.Create(id);
            return (new HexPaletteEntry { Id = id, Title = type.Title, IsUnit = true }, type.Create());
        }));

        /// <summary>The grouping of <see cref="Units"/>, for any (entry, unit) pairs; empty groups are left out.</summary>
        public static List<HexPaletteGroup> GroupUnits(IEnumerable<(HexPaletteEntry entry, HexUnit unit)> units)
        {
            var order = new[] { Characters, Vehicles, OtherUnits };
            var grouped = units.Select(u => { u.entry.Kind = UnitGroup(u.unit); return u.entry; }).ToList();
            return grouped.GroupBy(e => e.Kind).OrderBy(g => Array.IndexOf(order, g.Key))
                .Select(g => new HexPaletteGroup { Name = g.Key, Entries = g.OrderBy(e => e.Id, StringComparer.Ordinal).ToList() })
                .ToList();
        }

        /// <summary>Characters: exactly HexUnitScale.Characters figures. Vehicles: one member that is not a figure.</summary>
        public static string UnitGroup(HexUnit unit) =>
            unit.Members.Count == HexUnitScale.Characters && unit.Members.All(m => m.StandIn == StandIn.Figure) ? Characters
            : unit.Members.Count == 1 && unit.Members[0].StandIn != StandIn.Figure ? Vehicles
            : OtherUnits;

        // --- Facing ---

        /// <summary>The six hex directions as facings (degrees from north, clockwise seen from above, as HexUnitMotion
        /// reads them), in HexGeometry.Neighbours order, each with a compass label: the heading from a hex's centre to
        /// that neighbour's.</summary>
        public static IReadOnlyList<(string label, float degrees)> Facings { get; } = HexGeometry.Neighbours.Select(n =>
        {
            var a = HexGeometry.Centre(0, 0, HexScale.HexM); var b = HexGeometry.Centre(n.q, n.r, HexScale.HexM);
            float deg = Mathf.Repeat(Mathf.Round(Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg), 360f);
            return (Compass(deg), deg);
        }).OrderBy(f => f.deg).ToList();

        static string Compass(float deg) => new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }[Mathf.RoundToInt(deg / 45f) % 8];

        public static string Degrees(float facing) => facing.ToString("R", CultureInfo.InvariantCulture);

        // --- Brushes (each edit goes through HexPainter, so it is written to the overrides and rebuilds the hex) ---

        /// <summary>Paints prototype <paramref name="id"/> ("" for none) on each hex, through HexPainter.Change: each is
        /// rebuilt and recorded in the overrides at <paramref name="overridesPath"/>. Returns the hexes painted.</summary>
        public static List<(int q, int r)> Paint(GameObject root, string overridesPath, IEnumerable<(int q, int r)> hexes, string id)
        {
            var done = new List<(int q, int r)>();
            foreach (var (q, r) in hexes.Distinct())
            {
                HexPainter.Change(root, overridesPath, q, r, "prototype", id);
                done.Add((q, r));
            }
            return done;
        }

        /// <summary>Stands unit <paramref name="unit"/> on hex (q, r) facing <paramref name="facing"/>: both fields are
        /// recorded in the overrides, and the hex is rebuilt once.</summary>
        public static HexTileReport? PlaceUnit(GameObject root, string overridesPath, int q, int r, string unit, float facing)
        {
            if (unit == "") throw new ArgumentException("no unit: use ClearUnit");
            var state = root.GetComponent<HexBoardState>() ?? throw new InvalidOperationException("load the board with the Hex Painter first");
            HexPainter.Validate("unit", unit);
            HexPainter.Write(state.Board, overridesPath, q, r, "facing", Degrees(facing));
            return HexPainter.Change(root, overridesPath, q, r, "unit", unit);
        }

        /// <summary>Removes the unit from hex (q, r) (recorded as an empty unit, so it stays off after regenerating).</summary>
        public static HexTileReport? ClearUnit(GameObject root, string overridesPath, int q, int r) =>
            HexPainter.Change(root, overridesPath, q, r, "unit", "");

        /// <summary>Turns the unit on hex (q, r) to <paramref name="facing"/>.</summary>
        public static HexTileReport? Face(GameObject root, string overridesPath, int q, int r, float facing) =>
            HexPainter.Change(root, overridesPath, q, r, "facing", Degrees(facing));

        // --- Thumbnails ---

        /// <summary>The git-ignored thumbnail folder, from the repo root.</summary>
        public const string ThumbsFolder = "asset-staging/ui-local/hex-tiles/thumbs";

        public static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        public static string ThumbsDir => Path.Combine(RepoRoot, ThumbsFolder);

        /// <summary>&lt;dir&gt;/&lt;id&gt;.png for a prototype, &lt;dir&gt;/unit-&lt;id&gt;.png for a unit.</summary>
        public static string ThumbPath(string dir, HexPaletteEntry e) => Path.Combine(dir, (e.IsUnit ? "unit-" : "") + e.Id + ".png");

        /// <summary>The thumbnail of <paramref name="e"/>, or null when none was made (the palette then shows its text).
        /// The caller owns the texture.</summary>
        public static Texture2D? LoadThumb(string dir, HexPaletteEntry e)
        {
            var path = ThumbPath(dir, e);
            if (!File.Exists(path)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave, name = Path.GetFileName(path) };
            if (tex.LoadImage(File.ReadAllBytes(path))) return tex;
            UnityEngine.Object.DestroyImmediate(tex);
            return null;
        }
    }
}
