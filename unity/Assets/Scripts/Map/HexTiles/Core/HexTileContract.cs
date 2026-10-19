#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>What the board raises under a tile. Civ-style: the ground follows the hexes, not a real heightmap.</summary>
    public enum HexTerrain { Flat, Hills, Mountain, Water }

    /// <summary>Marks a class as a hex prototype with a unique, stable id (lower_snake_case). The board finds prototypes by
    /// this attribute, so adding one is adding one file: no table or switch elsewhere is edited.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class HexPrototypeAttribute : Attribute
    {
        public string Id { get; }
        public HexPrototypeAttribute(string id) => Id = id;
    }

    /// <summary>One kind of hex tile (a city quarter, a forest, a unit on the march ...). Build places pieces through the
    /// context only, and must be deterministic: the same context seed gives the same placements (tested).</summary>
    public interface IHexPrototype
    {
        /// <summary>A short human title for captures and stats.</summary>
        string Title { get; }
        /// <summary>The ground shape this tile wants when a board does not say otherwise.</summary>
        HexTerrain Terrain { get; }
        /// <summary>Ground texture layer (HexGround.Layers) when a board does not say otherwise.</summary>
        string Ground { get; }
        /// <summary>Most triangles the tile may draw (models after decimation; tested). See HexScale.Budget.</summary>
        int TriangleBudget { get; }
        void Build(HexTileContext ctx);
    }

    /// <summary>The shared scale, in hex units, so every tile reads at the same size (Civ VI as the reference: few, large
    /// pieces). S = HexM is the hex circumradius; a hex is sqrt(3) S across flat to flat. Start values, tuned from captures.</summary>
    public static class HexScale
    {
        public const float HexM = 5000f;
        /// <summary>Keep pieces this share of S inside the hexagon, so tile edges read.</summary>
        public const float InsetShare = 0.07f;
        /// <summary>Every placed piece is drawn this much larger than its prototype asks (Tal, 19 Oct: "the models a bit
        /// bigger"): Civ reads tiles by a few large pieces. Tune here, not in each prototype.</summary>
        public const float PieceScale = 1.35f;
        /// <summary>Buildings: footprint 0.12 to 0.2 S; 12 to 25 per city hex. A hero landmark up to 0.4 S.</summary>
        public const float BuildingMin = 0.12f, BuildingMax = 0.20f, LandmarkMax = 0.40f;
        /// <summary>Trees: height 0.12 to 0.2 S; 25 to 60 in a forest hex, 15 to 40 in an orchard.</summary>
        public const float TreeMin = 0.12f, TreeMax = 0.20f;
        /// <summary>Figures (units, people, animals): height 0.14 to 0.22 S, larger than buildings as Civ draws units;
        /// 3 to 7 to a unit.</summary>
        public const float FigureMin = 0.14f, FigureMax = 0.22f;
        /// <summary>The unit spot: keep tall pieces (buildings, trees, rocks) out of this radius about the centre, where a
        /// unit stands (units are a layer above every tile). Flats (fields, roads) may run under it. ctx.InUnitSpot tests it.</summary>
        public const float UnitSpotShare = 0.3f;
        /// <summary>Default triangle budgets per tile kind (after decimation): city 40k, forest 60k, units 60k, fields 20k.</summary>
        public const int BudgetCity = 40000, BudgetForest = 60000, BudgetUnits = 60000, BudgetFields = 20000;
    }

    /// <summary>All prototypes in loaded assemblies, by id.</summary>
    public static class HexPrototypes
    {
        static Dictionary<string, Type>? all;

        public static IReadOnlyDictionary<string, Type> All => all ??= AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { return e.Types.Where(t => t != null)!; } })
            .Where(t => t != null && !t.IsAbstract && typeof(IHexPrototype).IsAssignableFrom(t))
            .Select(t => (t, attr: (HexPrototypeAttribute?)Attribute.GetCustomAttribute(t, typeof(HexPrototypeAttribute))))
            .Where(p => p.attr != null)
            .ToDictionary(p => p.attr!.Id, p => p.t!, StringComparer.Ordinal);

        public static IHexPrototype Create(string id) =>
            All.TryGetValue(id, out var t) ? (IHexPrototype)Activator.CreateInstance(t)! : throw new KeyNotFoundException($"no hex prototype '{id}' (known: {string.Join(", ", All.Keys)})");
    }

    /// <summary>Pointy-top hex geometry in world x/z (HexGrid's lattice, z = -y): circumradius S, flat sides east and west.</summary>
    public static class HexGeometry
    {
        public const float Sqrt3 = 1.7320508f;

        public static Vector3 Centre(int q, int r, float sizeM)
        {
            var c = Statecraft.Game.Map.HexGrid.Centre(q, r, sizeM);
            return new Vector3(c.x, 0f, -c.y);
        }

        /// <summary>Inside the hexagon about <paramref name="centre"/>, shrunk by <paramref name="insetM"/> on every side.</summary>
        public static bool InHex(Vector3 p, Vector3 centre, float sizeM, float insetM = 0f)
        {
            float a = sizeM * 0.8660254f - insetM, dx = p.x - centre.x, dz = p.z - centre.z;
            return Mathf.Abs(dx) <= a && Mathf.Abs(dx * 0.5f + dz * 0.8660254f) <= a && Mathf.Abs(-dx * 0.5f + dz * 0.8660254f) <= a;
        }

        /// <summary>The axial hex containing p (cube rounding of HexGrid's inverse).</summary>
        public static (int q, int r) Axial(Vector3 p, float sizeM)
        {
            float x = p.x, y = -p.z;
            float qf = (Sqrt3 / 3f * x - y / 3f) / sizeM, rf = (2f / 3f * y) / sizeM, sf = -qf - rf;
            int q = Mathf.RoundToInt(qf), r = Mathf.RoundToInt(rf), s = Mathf.RoundToInt(sf);
            float dq = Mathf.Abs(q - qf), dr = Mathf.Abs(r - rf), ds = Mathf.Abs(s - sf);
            if (dq > dr && dq > ds) q = -r - s; else if (dr > ds) r = -q - s;
            return (q, r);
        }

        public static readonly (int q, int r)[] Neighbours = { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) };

        public static int Distance(int q0, int r0, int q1, int r1)
        {
            int dq = q1 - q0, dr = r1 - r0;
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
        }

        /// <summary>The six corners, starting north, clockwise seen from above.</summary>
        public static Vector3[] Corners(Vector3 centre, float sizeM) =>
            Enumerable.Range(0, 6).Select(i => { float a = Mathf.Deg2Rad * (90f - 60f * i); return centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * sizeM; }).ToArray();
    }
}
