#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Beersheba, 1948: the small Ottoman-planned town in the northern Negev, drawn as a low town hex on desert
    /// ground. The town was founded about 1900 as an Ottoman administrative centre and laid out on a grid
    /// (https://en.wikipedia.org/wiki/Beersheba, checked 2026-10-19: "A plan for the town in the form of a grid was
    /// developed by a Swiss and a German architect and two others").
    /// - The grid is turned -15 degrees (a look choice, unsourced), so the north-west diagonal points at a hex corner where
    ///   the station has room. Streets are painted dirt between 0.26 S lots. The lot at the centre is a small paved square,
    ///   kept open for a unit. Two streets run on as roads to the west and north edges.
    /// - One-storey stone houses (sandbox:kit_negev_2, _3 and sandbox:kit_stone_village_2) stand on the lots in a diamond of up to
    ///   18. All are grid-aligned and turned by quarter turns.
    /// - The Great Mosque, built in 1906 with Bedouin donations, stands on the lot north-west of the square, on the road
    ///   west. Sources: https://en.wikipedia.org/wiki/Beersheba (checked 2026-10-19: "In 1906, during the Ottoman era,
    ///   the Great Mosque of Beersheba was built"), and https://www.hadassahmagazine.org/?p=11283 (checked 2026-10-19:
    ///   a large dome, arched windows and a narrow minaret).
    ///   It is turned 45 degrees off the grid, since it "was built diagonally to the course of the street" to face the
    ///   direction of prayer (https://archive.israel21c.org/once-upon-a-time-in-beersheva/, checked 2026-10-19). It is drawn modest and all stand-ins: a stone block, a low dome
    ///   rising out of it (a Tree stand-in, as JerusalemOldCity draws its dome) and a slim minaret (a Pine stand-in).
    /// - The governor's house (Saraya) faces the square from the west lot, beside the mosque. Sources disagree on the
    ///   name: the Beersheba article says the governor lived in a tent "until the government house (Saraya) was built",
    ///   and the Ottoman Governor's House is now the Negev Museum of Art (both https://en.wikipedia.org/wiki/Beersheba,
    ///   checked 2026-10-19). The Governor's Mansion "was erected in 1906", and the Saraya, the central administration,
    ///   was its neighbour in the government complex (https://archive.israel21c.org/once-upon-a-time-in-beersheva/,
    ///   checked 2026-10-19). One modest two-storey stone block stands for them.
    /// - The Ottoman railway station sits on the north-west diagonal: a stone station building and its water tower.
    ///   Source: https://en.wikipedia.org/wiki/Beersheba_Turkish_railway_station (checked 2026-10-19): opened October
    ///   1915, the complex had a station building and a water tower, and the line closed in July 1927. That the buildings
    ///   still stood in 1948 is inferred from the 2013 renovation of the original complex. No track and no engine.
    /// - The Mandate police station is left out. It is listed among Tegart forts with a caveat
    ///   (https://en.wikipedia.org/wiki/Tegart_fort, checked 2026-10-19), but a 1948 photo caption describes an Ottoman
    ///   original building with a Mandate addition, so proto:tegart-fort-v1 would misdraw it.
    /// - Nahal Be'er Sheva runs along the south-south-west edge: a wadi bed painted as dirt with a pale gravel line.
    ///   Generic wells (low stone rings), acacias and tamarisks stand on its inner bank and in the free corners. Up to
    ///   three dromedaries (stand-ins) rest by a well in the south-east corner as market life.
    ///
    /// Triangle estimate at the maximum counts (FlatSurface refuses nothing):
    /// - 18 houses x 1,400 = 25,200.
    /// - 5 acacias (sandbox:acacia_lite, measured about 1,164) = 5,820.
    /// - Stand-ins: mosque, dome, minaret, Saraya, station, water tower, 4 tamarisks, 3 wells and 3 camels, about 16 x 60 =
    ///   960. The camels are a stand-in (tiles:beersheba_town/camel), not proto:figure-dromedary-camel-1948-v1, which
    ///   measures about 29,450 triangles each.
    /// - Total about 32,000 of 36,000.</summary>
    [HexPrototype("beersheba_town", Kind = "town", Region = "south")]
    public sealed class BeershebaTown : IHexPrototype
    {
        public string Title => "Beersheba, the Ottoman grid town (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "desert";
        // Measured 110,332 triangles worst case with the buildings drawn as the shipped b*-v2 town models at full
        // level (about 6,000 each, HexModels.TownAliases); the budget is that plus 10%. The doc comment's estimate predates them.
        public int TriangleBudget => 125000;

        // Explicit house ids: kit_stone_village, kit_negev (the first variant), kit_jaffa and kit_jerusalem_4 are the
        // mosque-like b17 mesh, so no wildcard that could pick one.
        static readonly string[] Houses = { "sandbox:kit_negev_2", "sandbox:kit_stone_village_2", "sandbox:kit_negev_3" };
        const string Mosque = "tiles:beersheba_town/great_mosque", Dome = "tiles:beersheba_town/great_mosque_dome",
            Minaret = "tiles:beersheba_town/minaret", Saraya = "tiles:beersheba_town/saraya",
            Station = "tiles:beersheba_town/railway_station", StationTower = "tiles:beersheba_town/station_water_tower",
            Well = "tiles:beersheba_town/well", Tamarisk = "tiles:beersheba_town/tamarisk",
            Acacia = "sandbox:acacia_lite", Camel = "tiles:beersheba_town/camel";
        const int MaxHouses = 18, MaxAcacias = 5, MaxTamarisks = 4, MaxWells = 3, MaxCamels = 3;

        static readonly Color Limestone = new Color(0.84f, 0.77f, 0.64f), Sandstone = new Color(0.80f, 0.70f, 0.55f),
            DomeStone = new Color(0.78f, 0.74f, 0.66f), TowerStone = new Color(0.72f, 0.66f, 0.55f),
            WellStone = new Color(0.62f, 0.57f, 0.49f), AcaciaGreen = new Color(0.48f, 0.50f, 0.30f),
            TamariskGreen = new Color(0.50f, 0.56f, 0.42f), CamelTan = new Color(0.72f, 0.58f, 0.40f);

        const float GridYaw = -15f, Cell = 0.26f, HouseSlope = 38f;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            var turn = Quaternion.Euler(0f, GridYaw, 0f);
            // The grid frame: u east along the grid, v north along it, shares of S about the hex centre.
            Vector3 G(float u, float v) => ctx.Centre + turn * new Vector3(u * s, 0f, v * s);

            // Pieces already down, as circles (centre, radius in metres), so the free pieces keep off them.
            var taken = new List<(Vector3 at, float r)>();
            bool Free(Vector3 at, float r)
            {
                foreach (var t in taken) if (Flat(at - t.at).magnitude < r + t.r) return false;
                return true;
            }
            // The half-diagonal of a placed square footprint asked as width w (shares of S), in metres.
            float HalfDiag(float w) => 0.71f * w * HexScale.PieceScale * s;

            // 1. Ground: desert to the rim, the town's trodden ground lighter, the square paved pale.
            ctx.PaintGround("desert", ctx.Centre, 0f, 1.75f * s, 2.0f * s, 0.2f, 0.7f);
            ctx.PaintGround("dirt", ctx.Centre, GridYaw + 45f, 0.95f * s, 0.95f * s, 0.45f, 0.35f);
            ctx.PaintGround("beach", ctx.Centre, GridYaw, 0.30f * s, 0.30f * s, 0.25f, 0.9f);

            // 2. Streets between the lots (at odd multiples of half a cell), then the roads on west and north to the edge.
            foreach (float k in new[] { -1.5f, -0.5f, 0.5f, 1.5f })
            {
                ctx.PaintStroke("dirt", G(k * Cell, -0.62f), G(k * Cell, 0.62f), 0.045f * s, 0.9f);
                ctx.PaintStroke("dirt", G(-0.62f, k * Cell), G(0.62f, k * Cell), 0.045f * s, 0.9f);
            }
            ctx.PaintStroke("dirt", G(-0.62f, 0.5f * Cell), G(-0.98f, 0.5f * Cell), 0.05f * s, 0.9f);
            ctx.PaintStroke("dirt", G(0.5f * Cell, 0.62f), G(0.5f * Cell, 0.98f), 0.05f * s, 0.9f);

            // 3. The landmarks round the square's west side.
            //    The Great Mosque on lot (-1, 1), 45 degrees off the grid; its dome rises out of the block; the minaret stands
            //    just off the south-west side's midpoint, inside the lot.
            var mosque = G(-Cell, Cell);
            float mosqueW = 0.14f;
            if (ctx.PlaceByWidth(Mosque, mosque, GridYaw + 45f, mosqueW * s, StandIn.Box, Limestone, HouseSlope))
            {
                taken.Add((mosque, HalfDiag(mosqueW)));
                ctx.PlaceByHeight(Dome, mosque, GridYaw + 45f, 0.13f * s, StandIn.Tree, DomeStone, 0.5f, HouseSlope);
            }
            var minaret = G(-Cell - 0.088f, Cell - 0.088f);
            if (ctx.PlaceByHeight(Minaret, minaret, GridYaw, 0.17f * s, StandIn.Pine, Limestone, 0.2f, HouseSlope))
                taken.Add((minaret, 0.03f * s));
            //    The governor's house (Saraya) on lot (-1, 0), facing the square.
            var saraya = G(-Cell, 0f);
            if (ctx.PlaceByWidth(Saraya, saraya, GridYaw + 90f, 0.15f * s, StandIn.Box, Sandstone, HouseSlope))
                taken.Add((saraya, HalfDiag(0.15f)));

            // 4. Houses on the other lots: a diamond (|i| + |j| <= 3) inside a 5 x 5 grid, the square and landmarks left out.
            int houses = 0;
            for (int j = 2; j >= -2; j--)
                for (int i = -2; i <= 2; i++)
                {
                    if (Mathf.Abs(i) + Mathf.Abs(j) > 3 || (i == 0 && j == 0) || (i == -1 && (j == 0 || j == 1))) continue;
                    float w = ctx.Range(0.13f, 0.14f);
                    float yaw = GridYaw + 90f * ctx.Rng.Next(4);
                    var at = G(i * Cell, j * Cell);
                    if (houses >= MaxHouses) continue;
                    if (ctx.PlaceByWidth(Houses[(houses + (i & 1)) % Houses.Length], at, yaw, w * s, StandIn.Box, Limestone, HouseSlope))
                    {
                        taken.Add((at, HalfDiag(w))); houses++;
                    }
                }

            // 5. The railway station on the north-west diagonal (a hex corner), with its water tower to the west.
            var station = G(-0.46f, 0.46f);
            if (ctx.PlaceByWidth(Station, station, GridYaw, 0.13f * s, StandIn.Box, Sandstone, HouseSlope))
                taken.Add((station, HalfDiag(0.13f)));
            var tower = G(-0.62f, 0.44f);
            if (ctx.PlaceByHeight(StationTower, tower, GridYaw, 0.14f * s, StandIn.Palm, TowerStone, 0.3f, HouseSlope))
                taken.Add((tower, 0.04f * s));

            // 6. Nahal Be'er Sheva: a meandering wadi bed along the south-south-west edge, dirt banks and a pale gravel line.
            float normalBearing = 195f, offset = 0.78f * s, amp = 0.035f * s, phase = ctx.Range(0f, 6.283f);
            var normal = Flat(ctx.At(normalBearing, 1f) - ctx.Centre).normalized;
            var along = new Vector3(normal.z, 0f, -normal.x);
            var wadi = new List<Vector3>();
            for (int k = 0; k <= 20; k++)
            {
                float t = -1f + k * 0.1f;
                wadi.Add(ctx.Centre + normal * (offset + amp * Mathf.Sin(3.1f * t + phase)) + along * (t * s));
            }
            for (int k = 0; k < wadi.Count - 1; k++)
            {
                ctx.PaintStroke("dirt", wadi[k], wadi[k + 1], 0.07f * s, 0.85f);
                ctx.PaintStroke("beach", wadi[k], wadi[k + 1], 0.025f * s, 0.7f);
            }

            // 7. Trees on the wadi's inner bank, acacias and tamarisks alternating, off the houses.
            int acacias = 0, tamarisks = 0;
            for (int k = 1; k < wadi.Count - 1; k += 2)
            {
                var at = wadi[k] - normal * (ctx.Range(0.07f, 0.09f) * s);
                float h = ctx.Range(0.12f, 0.14f) * s, r = 0.05f * s;
                if (!ctx.Inside(at, r) || ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                bool acacia = (k / 2) % 2 == 0;
                if (acacia && acacias < MaxAcacias &&
                    ctx.PlaceByHeight(Acacia, at, ctx.Range(0f, 360f), h, StandIn.Tree, AcaciaGreen, 0.8f, 40f))
                { taken.Add((at, r)); acacias++; }
                else if (!acacia && tamarisks < MaxTamarisks &&
                    ctx.PlaceByHeight(Tamarisk, at, ctx.Range(0f, 360f), h, StandIn.Tree, TamariskGreen, 0.6f, 40f))
                { taken.Add((at, r)); tamarisks++; }
            }

            // 8. Wells: one by the wadi to the south-west, one in the north-east corner, one in the south-east corner, where
            //    up to three camels rest beside it.
            int wells = 0;
            Vector3[] wellSpots = { G(-0.44f, -0.44f), G(0.44f, 0.44f), G(0.50f, -0.50f) };
            foreach (var at in wellSpots)
            {
                float r = 0.04f * s;
                if (wells >= MaxWells || !Free(at, r)) continue;
                if (ctx.PlaceByWidth(Well, at, ctx.Range(0f, 360f), 0.035f * s, StandIn.Rock, WellStone, 40f)) { taken.Add((at, r)); wells++; }
            }
            int camels = 0;
            Vector3[] camelSpots = { G(0.64f, -0.44f), G(0.44f, -0.64f), G(0.60f, -0.58f) };
            foreach (var at in camelSpots)
            {
                float r = 0.05f * s;
                if (camels >= MaxCamels || !Free(at, r)) continue;
                if (ctx.PlaceByHeight(Camel, at, ctx.Range(0f, 360f), 0.07f * s, StandIn.Figure, CamelTan, 0.8f, 40f)) { taken.Add((at, r)); camels++; }
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
