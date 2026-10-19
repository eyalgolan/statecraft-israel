#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Jerusalem in 1948 as one mixed hill hex: the New City on the west two-thirds, the walled Old City on the
    /// east third. The board puts the West Jerusalem and Old City markers on the same hex (0,23), so this is an
    /// alternative to jerusalem_old_city for it. Drawn as architecture only: no line, barrier, flag or damage.
    /// - Jaffa Road, the commercial street of the Mandate-era New City (https://en.wikipedia.org/wiki/Jaffa_Road,
    ///   checked 2026-10-19), is a painted avenue from the Old City's west gate, through a small paved square at the hex
    ///   centre (kept open for a unit), out to the north-west edge. Stone blocks (kit_jerusalem, _2, _3) stand on a grid
    ///   turned to the road, the first row lining it, with pine and cypress garden lots between the quarters.
    /// - A second street runs south from the square. On it, facing each other:
    ///   - the King David Hotel, opened 1931 in local limestone on King David Street
    ///     (https://en.wikipedia.org/wiki/King_David_Hotel, checked 2026-10-19): a long stone block of three boxes, the
    ///     middle one taller, on the east side;
    ///   - the YMCA, opened 18 April 1933, with its tower (https://en.wikipedia.org/wiki/Jerusalem_International_YMCA;
    ///     the tower is 152 ft per https://jerusalemstory.com/en/article/ymca-cherished-social-hub-during-mandate-years,
    ///     both checked 2026-10-19): a stone block with a tall spire stand-in rising from it, on the west side.
    /// - The Old City: a small crenellated square of wall stand-ins (the Ottoman walls of about 1537 to 1541,
    ///   https://en.wikipedia.org/wiki/Walls_of_Jerusalem, checked 2026-10-19) with corner towers, a gate gap on the west
    ///   (toward Jaffa Road) and on the north, seven dense stone houses inside round a cypress court, and olives just
    ///   outside. No named holy site is modelled.
    ///
    /// Triangle estimate, at the maximum counts (FlatSurface refuses nothing):
    /// - At most 22 New City blocks and 7 Old City houses (kit_jerusalem*, up to 1,400 each) = 40,600.
    /// - At most 16 trees (aleppo_pine_lite, cypress_lite, olive_lite, 1,602 each) = 25,632.
    /// - 2 townspeople (v2 figures, up to 4,000 each) = 8,000.
    /// - 42 wall and tower blocks, 3 hotel boxes, the YMCA block and its tower: about 47 stand-ins x 20 = 940.
    /// - Total about 75,200 of 85,000.
    ///
    /// The hotel, the YMCA, its tower and the wall pieces have no models (tiles:jerusalem_new_city/...), so they draw
    /// stand-ins. The YMCA tower is a Pine spire because a Box stand-in cannot be taller than it is wide.</summary>
    [HexPrototype("jerusalem_new_city", Kind = "city", Region = "centre")]
    public sealed class JerusalemNewCity : IHexPrototype
    {
        public string Title => "Jerusalem, the New City and the Old City (1948)";
        public HexTerrain Terrain => HexTerrain.Hills;
        public string Ground => "hills";
        public int TriangleBudget => 85000;

        // Explicit ids: kit_jerusalem_4 is the mosque-like b17 mesh.
        static readonly string[] Houses = { "sandbox:kit_jerusalem", "sandbox:kit_jerusalem_2", "sandbox:kit_jerusalem_3" };
        static readonly string[] People = { "proto:figure-townsman-c02-1948-v2", "proto:figure-woman-blue-dress-c10-1948-v2" };
        const string Pine = "sandbox:aleppo_pine_lite", Cypress = "sandbox:cypress_lite", Olive = "sandbox:olive_lite",
            Hotel = "tiles:jerusalem_new_city/king_david_hotel", Ymca = "tiles:jerusalem_new_city/ymca",
            YmcaTower = "tiles:jerusalem_new_city/ymca_tower", Wall = "tiles:jerusalem_new_city/old_city_wall",
            WallTower = "tiles:jerusalem_new_city/old_city_tower";

        static readonly Color Limestone = new Color(0.86f, 0.80f, 0.68f), PinkStone = new Color(0.88f, 0.76f, 0.66f),
            WallStone = new Color(0.82f, 0.75f, 0.61f), TowerStone = new Color(0.76f, 0.69f, 0.55f),
            PineLeaf = new Color(0.20f, 0.36f, 0.18f), CypressLeaf = new Color(0.14f, 0.28f, 0.15f), OliveLeaf = new Color(0.42f, 0.50f, 0.34f);

        const float Slope = 40f;
        /// <summary>The Old City square: centre x and half-side, shares of S in the tile frame (x east, z north).</summary>
        const float OldX = 0.50f, Half = 0.19f;
        const int MaxBlocks = 22, MaxOldHouses = 7, MaxTrees = 16, MaxGardens = 4;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, yaw = ctx.Range(-5f, 5f);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 P(float x, float z) => ctx.Centre + rot * new Vector3(x * s, 0f, z * s);
            int trees = 0;
            bool Tree(string model, float x, float z, float h, StandIn standIn, Color leaf, float share)
            {
                if (trees >= MaxTrees || !ctx.PlaceByHeight(model, P(x, z), ctx.Range(0f, 360f), h * s, standIn, leaf, share, Slope)) return false;
                trees++;
                return true;
            }

            // Jaffa Road runs from the Old City's west gate west to the square, then north-west to the edge. In the tile
            // frame its outer leg has direction d; n is the side across it.
            float dx = -0.894f, dz = 0.447f, nx = 0.447f, nz = 0.894f;
            float roadYaw = yaw + Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;

            // 1. Ground: a light wash of beaten earth over the New City, a stronger one inside the walls, the streets, and
            //    the paved square at the centre (painted: PlaceFlat refuses on a hill).
            ctx.PaintGround("dirt", P(-0.22f, 0f), yaw, 1.0f * s, 1.6f * s, 0.35f, 0.3f);
            ctx.PaintGround("dirt", P(OldX, 0f), yaw, 0.36f * s, 0.36f * s, 0.1f, 0.55f);
            ctx.PaintStroke("dirt", P(OldX - Half, 0f), P(0f, 0f), 0.06f * s, 0.9f);
            ctx.PaintStroke("dirt", P(0f, 0f), P(-0.72f, 0.36f), 0.06f * s, 0.9f);
            ctx.PaintStroke("dirt", P(0.03f, -0.12f), P(0.05f, -0.85f), 0.05f * s, 0.85f);
            ctx.PaintStroke("dirt", P(OldX - Half, 0f), P(OldX, 0f), 0.04f * s, 0.8f);
            ctx.PaintStroke("dirt", P(OldX, Half), P(OldX, 0f), 0.04f * s, 0.8f);
            ctx.PaintGround("beach", ctx.Centre, yaw, 0.26f * s, 0.26f * s, 0.3f, 0.7f);
            ctx.PaintGround("beach", ctx.Centre, yaw + 45f, 0.26f * s, 0.26f * s, 0.3f, 0.7f);

            // 2. The two landmarks on the south street. The hotel is three boxes in a north-south row (the taller middle
            //    one slightly overlapping the wings, so no faces meet); the YMCA a block with its tower rising from it.
            float[] wing = { -0.125f, 0f, 0.125f }, wingW = { 0.10f, 0.112f, 0.10f };
            for (int i = 0; i < 3; i++)
                ctx.PlaceByWidth(Hotel, P(0.19f, -0.42f + wing[i]), yaw - 90f, wingW[i] * s, StandIn.Box, PinkStone, Slope);
            ctx.PlaceByWidth(Ymca, P(-0.13f, -0.42f), yaw + 90f, 0.14f * s, StandIn.Box, Limestone, Slope);
            ctx.PlaceByHeight(YmcaTower, P(-0.09f, -0.42f), yaw + 90f, 0.20f * s, StandIn.Pine, Limestone, 0.15f, Slope);
            Tree(Pine, 0.33f, -0.48f, 0.14f, StandIn.Pine, PineLeaf, 0.4f);
            Tree(Cypress, -0.02f, -0.62f, 0.15f, StandIn.Pine, CypressLeaf, 0.3f);

            // 3. The New City: lots on a grid turned to Jaffa Road, 0.2 S apart, the first row each side lining it. Lots on
            //    the square, the Old City side, the south street or a landmark are skipped. About one lot in six, up to
            //    four, is a garden with a pine or a cypress.
            float[] us = { -0.56f, -0.36f, -0.16f, 0.04f, 0.24f, 0.44f, 0.64f, 0.84f }, vs = { -0.93f, -0.73f, -0.53f, -0.33f, -0.13f, 0.13f, 0.33f, 0.53f, 0.73f, 0.93f };
            int blocks = 0, gardens = 0;
            foreach (float u in us)
                foreach (float v in vs)
                {
                    float x = u * dx + v * nx + ctx.Range(-0.008f, 0.008f), z = u * dz + v * nz + ctx.Range(-0.008f, 0.008f);
                    float w = ctx.Range(0.12f, 0.13f);
                    int roll = ctx.Rng.Next(100), turn = ctx.Rng.Next(4);
                    if (x * x + z * z < 0.25f * 0.25f) continue;                                // the square
                    if (x > 0.30f || (x > 0.15f && Mathf.Abs(z) < 0.45f)) continue;           // the Old City and its olives
                    if (z < -0.08f && Mathf.Abs(x - 0.04f) < 0.13f) continue;                    // the south street
                    if (Mathf.Abs(x - 0.19f) < 0.21f && Mathf.Abs(z + 0.42f) < 0.33f) continue;  // the hotel
                    if (Mathf.Abs(x + 0.13f) < 0.22f && Mathf.Abs(z + 0.42f) < 0.22f) continue;  // the YMCA
                    if (roll < 18 && gardens < MaxGardens)
                    {
                        gardens++;
                        ctx.PaintGround("grass", P(x, z), roadYaw, 0.14f * s, 0.14f * s, 0.3f, 0.6f);
                        if (roll % 2 == 0) Tree(Pine, x, z, ctx.Range(0.14f, 0.16f), StandIn.Pine, PineLeaf, 0.4f);
                        else Tree(Cypress, x, z, ctx.Range(0.15f, 0.17f), StandIn.Pine, CypressLeaf, 0.3f);
                        continue;
                    }
                    if (blocks < MaxBlocks && ctx.PlaceByWidth(Houses[blocks % Houses.Length], P(x, z), roadYaw + 90f * turn + ctx.Range(-3f, 3f), w * s, StandIn.Box, Limestone, Slope))
                        blocks++;
                }

            // 4. The Old City wall: on each side, short blocks 0.032 S apart, alternately 0.030 and 0.024 S so no two share
            //    a face plane and the top reads as crenellation. A gate gap on the west and north sides, each between two
            //    gate towers, and a larger tower at each corner.
            var sides = new (bool alongX, float at, bool gate)[] { (true, Half, true), (false, OldX + Half, false), (true, -Half, false), (false, OldX - Half, true) };
            foreach (var side in sides)
            {
                Vector3 On(float t) => side.alongX ? P(OldX + t, side.at) : P(side.at, t);
                for (int i = 0; i <= 10; i++)
                {
                    float t = -0.16f + 0.032f * i;
                    if (side.gate && Mathf.Abs(t) < 0.08f) continue;
                    ctx.PlaceByWidth(Wall, On(t), yaw, (i % 2 == 0 ? 0.030f : 0.024f) * s, StandIn.Box, WallStone, Slope);
                }
                if (side.gate)
                    foreach (float t in new[] { -0.065f, 0.065f })
                        ctx.PlaceByWidth(WallTower, On(t), yaw, 0.042f * s, StandIn.Box, TowerStone, Slope);
            }
            foreach (float cx in new[] { OldX - Half, OldX + Half })
                foreach (float cz in new[] { -Half, Half })
                    ctx.PlaceByWidth(WallTower, P(cx, cz), yaw, 0.05f * s, StandIn.Box, TowerStone, Slope);

            // 5. Inside: a 3 x 3 block of dense lots 0.105 S apart. The centre lot is a court with a cypress; the west
            //    middle lot is left open for the street in from the gate.
            int old = 0;
            foreach (float ox in new[] { -0.105f, 0f, 0.105f })
                foreach (float oz in new[] { -0.105f, 0f, 0.105f })
                {
                    int turn = ctx.Rng.Next(4);
                    if (ox == 0f && oz == 0f)
                    {
                        ctx.PaintGround("grass", P(OldX, 0f), yaw, 0.08f * s, 0.08f * s, 0.3f, 0.5f);
                        Tree(Cypress, OldX, 0f, 0.13f, StandIn.Pine, CypressLeaf, 0.3f);
                        continue;
                    }
                    if (ox < 0f && oz == 0f) continue;
                    if (old < MaxOldHouses && ctx.PlaceByWidth(Houses[(old + 1) % Houses.Length], P(OldX + ox, oz), yaw + 90f * turn + ctx.Range(-3f, 3f), 0.078f * s, StandIn.Box, Limestone, Slope))
                        old++;
                }

            // 6. Olives just outside the north and south walls.
            foreach (float x in new[] { OldX - 0.10f, OldX + 0.10f })
                foreach (float z in new[] { -0.32f, 0.32f })
                    Tree(Olive, x, z, ctx.Range(0.12f, 0.13f), StandIn.Tree, OliveLeaf, 0.5f);

            // 7. Life: two townspeople on the square.
            for (int k = 0; k < People.Length; k++)
            {
                float b = yaw + 200f + 40f * k;
                ctx.PlaceByHeight(People[k], ctx.At(b, 0.08f), b + 160f, 0.075f * s, StandIn.Figure, null, 0.4f, Slope);
            }
        }
    }
}
