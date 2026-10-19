#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Ramla, the Arab market town of the coastal plain, as it stood in 1948. Drawn as architecture and landscape
    /// only: no ruins, damage or abandonment cues.
    /// - A market street crosses the hex, painted dirt, with a small market square at the centre kept open for a unit.
    /// - Flat-roofed stone houses (kit_stone_village_2, kit_jaffa_2, kit_jaffa_3, kit_jerusalem, kit_jerusalem_2) stand
    ///   in two tight rows each side of the street, the first row facing it.
    /// - A little apart, past the north-east row, the White Tower: the square, five-storey minaret of about 27 m added to
    ///   the White Mosque in 1267 to 1268 (https://en.wikipedia.org/wiki/White_Tower_(Ramla), checked 2026-10-19). Kept
    ///   low-key: a small stone base with a slim spire stand-in on a patch of bare ground. The Great Mosque is not
    ///   modelled.
    /// - Prickly-pear hedges in short runs round the edge of the town, and orchards (citrus, every third tree an olive)
    ///   on tilled soil in a ring beyond them.
    /// - No figures: Ramla changed hands during 1948, and a static tile cannot say who lives there across 1948 to 1952.
    ///
    /// Triangle estimate, at the maximum counts (FlatSurface refuses nothing):
    /// - At most 22 houses (kit_*, up to 1,400 each) = 30,800.
    /// - At most 18 orchard trees (citrus_lite, olive_lite, 1,602 each) = 28,836.
    /// - At most 36 prickly pears, the tower base and spire: about 38 stand-ins x 60 = 2,280.
    /// - Total about 61,900 of 80,000.
    ///
    /// The tower and the prickly pear have no models (tiles:ramla_town/...), so they draw stand-ins. The tower is a Pine
    /// spire on a small Box base, because a Box stand-in cannot be taller than it is wide.</summary>
    [HexPrototype("ramla_town", Kind = "town", Region = "centre")]
    public sealed class RamlaTown : IHexPrototype
    {
        public string Title => "Ramla, market town of the plain (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => 80000;

        // Explicit ids: kit_jaffa, kit_jerusalem_4 and kit_stone_village are the mosque-like b17 mesh.
        static readonly string[] Houses = { "sandbox:kit_stone_village_2", "sandbox:kit_jaffa_2", "sandbox:kit_jerusalem", "sandbox:kit_jaffa_3", "sandbox:kit_jerusalem_2" };
        const string Citrus = "sandbox:citrus_lite", Olive = "sandbox:olive_lite",
            TowerBase = "tiles:ramla_town/white_tower_base", Tower = "tiles:ramla_town/white_tower", PricklyPear = "tiles:ramla_town/prickly_pear";

        static readonly Color Stone = new Color(0.84f, 0.78f, 0.66f), TowerStone = new Color(0.90f, 0.87f, 0.79f),
            CitrusLeaf = new Color(0.16f, 0.34f, 0.14f), OliveLeaf = new Color(0.42f, 0.50f, 0.34f), Cactus = new Color(0.48f, 0.58f, 0.40f);

        const int MaxHouses = 22, MaxTrees = 18, MaxPears = 36;

        /// <summary>How far the inset hexagon reaches from the centre on a bearing, as a share of S (the flat sides face
        /// 30, 90, 150 ... degrees).</summary>
        static float Edge(float bearing) => (HexScale.HexM * 0.8660254f - HexScale.HexM * HexScale.InsetShare) / HexScale.HexM
            / Mathf.Cos(Mathf.Deg2Rad * (Mathf.Repeat(bearing, 60f) - 30f));

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, yaw = 90f + ctx.Range(-25f, 25f);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            // A point in the street's frame, shares of S: x across the street, z along it.
            Vector3 L(float x, float z) => ctx.Centre + rot * new Vector3(x * s, 0f, z * s);
            // Footprints placed so far (centre, radius in metres): there is no collision check in Place, so keep our own.
            var taken = new List<(Vector3 at, float r)>();
            bool Free(Vector3 p, float r) { foreach (var t in taken) if ((t.at - p).magnitude < t.r + r) return false; return true; }

            // 1. Ground: the market street and square, a lighter wash of beaten earth over the town.
            ctx.PaintGround("dirt", ctx.Centre, yaw, 0.9f * s, 1.3f * s, 0.4f, 0.3f);
            ctx.PaintStroke("dirt", L(0f, -0.95f), L(0f, 0.95f), 0.06f * s, 0.95f);
            ctx.PaintGround("dirt", ctx.Centre, yaw, 0.24f * s, 0.24f * s, 0.3f, 0.9f);

            // 2. The White Tower, a little apart past the north-east row, on its own patch of bare ground.
            var tower = L(0.56f, 0.12f);
            ctx.PaintGround("dirt", tower, yaw, 0.16f * s, 0.16f * s, 0.35f, 0.8f);
            ctx.PlaceByWidth(TowerBase, tower, yaw, 0.05f * s, StandIn.Box, TowerStone);
            ctx.PlaceByHeight(Tower, tower, yaw, 0.20f * s, StandIn.Pine, TowerStone, 0.15f);
            taken.Add((tower, 0.07f * s));

            // 3. Houses in two rows each side of the street, 0.19 S apart along it, the first row facing it. The lots on the
            //    square and those past 0.62 S (where the hedges and orchards begin) are skipped.
            int houses = 0;
            foreach (float x in new[] { -0.13f, 0.13f, -0.33f, 0.33f })
                foreach (float z in new[] { -0.57f, -0.38f, -0.19f, 0f, 0.19f, 0.38f, 0.57f })
                {
                    float w = ctx.Range(0.12f, 0.13f), jx = ctx.Range(-0.008f, 0.008f), jz = ctx.Range(-0.008f, 0.008f);
                    if ((Mathf.Abs(x) < 0.2f && z == 0f) || x * x + z * z > 0.62f * 0.62f) continue;
                    var at = L(x + jx, z + jz);
                    float r = 0.5f * w * HexScale.PieceScale * s;   // the rows share one heading, so the half side is enough
                    if (houses >= MaxHouses || !Free(at, r)) continue;
                    float face = x > 0f ? -90f : 90f;
                    if (!ctx.PlaceByWidth(Houses[houses % Houses.Length], at, yaw + face + ctx.Range(-3f, 3f), w * s, StandIn.Box, Stone)) continue;
                    houses++;
                    taken.Add((at, r));
                }

            // 4. Prickly-pear hedges: runs of four plants round the town's edge, between the street ends, kept off the houses.
            float streetA = yaw, streetB = yaw + 180f;
            int pears = 0;
            for (int k = 0; k < 9; k++)
            {
                float b0 = yaw + 20f + 40f * k;
                if (Mathf.Abs(Mathf.DeltaAngle(b0, streetA)) < 18f || Mathf.Abs(Mathf.DeltaAngle(b0, streetB)) < 18f) continue;
                float r = Mathf.Min(0.68f, Edge(b0) - 0.14f);
                for (int i = 0; i < 4; i++)
                {
                    var at = ctx.At(b0 + 3.5f * i, r);
                    if (pears >= MaxPears || !Free(at, 0.03f * s)) continue;
                    if (ctx.PlaceByHeight(PricklyPear, at, ctx.Range(0f, 360f), 0.06f * s, StandIn.Tree, Cactus, 0.9f)) pears++;
                }
            }

            // 5. Orchards: a ring of trees on tilled soil beyond the hedges, every third an olive, off the street ends.
            int trees = 0;
            for (int k = 0; k < 20; k++)
            {
                float b = yaw + 9f + 18f * k;
                if (Mathf.Abs(Mathf.DeltaAngle(b, streetA)) < 9f || Mathf.Abs(Mathf.DeltaAngle(b, streetB)) < 9f) continue;
                float r = Mathf.Min(0.82f, Edge(b) - 0.09f) + ctx.Range(-0.01f, 0.01f);
                var at = ctx.At(b, r);
                if (trees >= MaxTrees || !Free(at, 0.06f * s)) continue;
                ctx.PaintGround("ploughed", at, b, 0.16f * s, 0.12f * s, 0.3f, 0.7f);
                bool placed = k % 3 == 2
                    ? ctx.PlaceByHeight(Olive, at, ctx.Range(0f, 360f), ctx.Range(0.12f, 0.13f) * s, StandIn.Tree, OliveLeaf, 0.5f)
                    : ctx.PlaceByHeight(Citrus, at, ctx.Range(0f, 360f), ctx.Range(0.12f, 0.14f) * s, StandIn.Tree, CitrusLeaf, 0.5f);
                if (placed) trees++;
            }
        }
    }
}
