#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Jerusalem's walled Old City on its hill (1948), drawn Civ-style as a city-centre hex.
    /// - The wall is a square-ish crenellated ring, half-side 0.5 S, turned a few degrees. It is built from many short
    ///   stand-in blocks of two alternating sizes, with larger towers at the corners and beside four gates.
    /// - The gates sit on the axes: Damascus (north), Lions' (east), Dung (south) and Jaffa (west). Four streets painted
    ///   as dirt cross the town from them to a small paved plaza at the centre, 0.25 S across and kept open for a unit.
    /// - North of the plaza, 0.3 S out, stands the first-batch Jerusalem stone-city cluster. Its covered mass stands for
    ///   the souk on the Damascus Gate street.
    /// - To the south-east is the Haram al-Sharif: a raised paved esplanade carrying the Dome of the Rock and the
    ///   al-Aqsa block. The Dome is two crossed blocks in blue faience (an octagonal drum) under a gold dome.
    /// - Stone houses fill every other lot inside the walls, on a tight grid. About one lot in seven is a garden
    ///   courtyard with a cypress or an olive.
    /// - The ground inside the walls is painted dirt at half strength. Outside, the bare hill carries olives on the
    ///   Mount of Olives (east) and in the valleys (south), and cypresses to the north and west.
    ///
    /// Triangle estimate, at the maximum counts (FlatSurface refuses nothing):
    /// - At most 30 houses (sandbox:kit_jerusalem*, up to 1,400 each) = 42,000.
    /// - At most 6 courtyard trees, 2 on the esplanade and 11 outside, so 19 x 1,602 = 30,438.
    /// - The jerusalem-stone-city-v1 cluster: about 6,000 (the 6,000-triangle Tripo export; unmeasured here).
    /// - Wall blocks, gate towers and corner towers: 84 Box stand-ins x 12 = 1,008.
    /// - Dome drum, al-Aqsa block and gold dome: about 90. Plaza and esplanade flats: 24.
    /// - Total about 79,600 of 90,000 (about 83,600 even if the cluster were the 10,000-triangle export).
    ///
    /// The wall, towers, drum, dome and al-Aqsa have no models (tiles:jerusalem_old_city/...), so they always draw stand-ins.
    /// The gold dome is a Tree stand-in whose crown rises out of the drum.</summary>
    [HexPrototype("jerusalem_old_city", Kind = "city")]
    public sealed class JerusalemOldCity : IHexPrototype
    {
        public string Title => "Jerusalem, the walled Old City (1948)";
        public HexTerrain Terrain => HexTerrain.Hills;
        public string Ground => "hills";
        public int TriangleBudget => 240000;   // the shipped b*-v2 buildings at full detail (Tal, 19 Oct 2026): measured 217k on the israel board and the test hex

        const string Cluster = "proto:jerusalem-stone-city-v1", House = "sandbox:kit_jerusalem*", Cypress = "sandbox:cypress_lite",
            Olive = "sandbox:olive_lite", Wall = "tiles:jerusalem_old_city/wall", Tower = "tiles:jerusalem_old_city/tower",
            Drum = "tiles:jerusalem_old_city/dome_drum", Dome = "tiles:jerusalem_old_city/golden_dome", Aqsa = "tiles:jerusalem_old_city/aqsa";

        static readonly Color Limestone = new Color(0.86f, 0.80f, 0.68f), WallStone = new Color(0.83f, 0.76f, 0.62f),
            TowerStone = new Color(0.77f, 0.70f, 0.56f), Paving = new Color(0.80f, 0.75f, 0.63f), Esplanade = new Color(0.87f, 0.83f, 0.72f),
            Faience = new Color(0.30f, 0.47f, 0.62f), Gold = new Color(0.90f, 0.71f, 0.22f),
            OliveLeaf = new Color(0.42f, 0.50f, 0.34f), CypressLeaf = new Color(0.18f, 0.30f, 0.16f);

        /// <summary>The wall's half-side, as a share of S. At 0.5 the corner towers pass the inset-hexagon test at any of the
        /// small turns the wall takes, so no segment is refused and no accidental gap opens.</summary>
        const float Half = 0.5f, Slope = 40f;
        const int MaxHouses = 30, MaxYards = 6;

        /// <summary>House lot centres on each axis, as shares of S. The wider gap about 0 leaves the cross streets open.</summary>
        static readonly float[] Lots = { -0.415f, -0.305f, -0.195f, -0.085f, 0.085f, 0.195f, 0.305f, 0.415f };

        /// <summary>Where houses may not go, in the wall's frame (centre and half-sizes, shares of S): the plaza, the cluster
        /// and the esplanade.</summary>
        static readonly (float x, float z, float hx, float hz)[] KeepOut =
        {
            (0f, 0f, 0.125f, 0.125f), (0f, 0.30f, 0.15f, 0.15f), (0.30f, -0.20f, 0.13f, 0.15f),
        };

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, yaw = ctx.Range(-8f, 8f);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            // A point in the wall's frame: x along the north and south walls (east), z along the east and west walls (north),
            // both shares of S.
            Vector3 P(float x, float z) => ctx.Centre + rot * new Vector3(x * s, 0f, z * s);
            bool Free(float x, float z, float half)
            {
                foreach (var k in KeepOut) if (Mathf.Abs(x - k.x) < half + k.hx && Mathf.Abs(z - k.z) < half + k.hz) return false;
                return true;
            }

            // 1. Ground. Inside the walls it is beaten earth: dirt at half strength over the hill rock, kept just inside the
            //    wall. Ploughed olive terraces lie on the Mount of Olives to the east. The four streets cross the town and
            //    run on out through the gates.
            ctx.PaintGround("dirt", ctx.Centre, yaw, 0.92f * s, 0.92f * s, 0.04f, 0.5f);
            ctx.PaintGround("ploughed", P(0.66f, 0f), yaw, 0.16f * s, 0.80f * s, 0.3f, 0.45f);
            ctx.PaintStroke("dirt", P(0f, 0.125f), P(0f, 0.85f), 0.06f * s, 0.9f);
            ctx.PaintStroke("dirt", P(0.125f, 0f), P(0.85f, 0f), 0.06f * s, 0.9f);
            ctx.PaintStroke("dirt", P(0f, -0.125f), P(0f, -0.85f), 0.06f * s, 0.9f);
            ctx.PaintStroke("dirt", P(-0.125f, 0f), P(-0.85f, 0f), 0.06f * s, 0.9f);

            // 2. The wall: on each side, short blocks 0.04 S apart, alternately 0.045 and 0.034 S. The sizes alternate so
            //    neighbouring blocks never share a face plane, and the skyline reads as crenellation. A gap is left at each
            //    gate, flanked by two gate towers, and a larger tower stands at each corner.
            var sides = new (bool alongX, float at, float gate)[] { (true, Half, 0f), (false, Half, 0f), (true, -Half, 0f), (false, -Half, 0f) };
            foreach (var side in sides)
            {
                Vector3 OnSide(float t) => side.alongX ? P(t, side.at) : P(side.at, t);
                for (int i = 0; i <= 21; i++)
                {
                    float t = -0.42f + 0.04f * i;
                    if (Mathf.Abs(t - side.gate) < 0.09f) continue;
                    ctx.PlaceByWidth(Wall, OnSide(t), yaw, (i % 2 == 0 ? 0.045f : 0.034f) * s, StandIn.Box, WallStone, Slope);
                }
                foreach (float d in new[] { -0.065f, 0.065f })
                    ctx.PlaceByWidth(Tower, OnSide(side.gate + d), yaw, 0.055f * s, StandIn.Box, TowerStone, Slope);
            }
            foreach (float cx in new[] { -Half, Half })
                foreach (float cz in new[] { -Half, Half })
                    ctx.PlaceByWidth(Tower, P(cx, cz), yaw, 0.075f * s, StandIn.Box, TowerStone, Slope);

            // 3. The open plaza at the centre, for a unit. North of it, 0.3 S out, the stone-city cluster faces south, toward
            //    the plaza and the camera.
            ctx.PlaceFlat(ctx.Centre, yaw, 0.25f * s, 0.25f * s, Paving, 150f);
            ctx.PlaceByWidth(Cluster, P(0f, 0.30f), yaw + 180f, 0.30f * s, StandIn.Box, Limestone, Slope);

            // 4. The Haram al-Sharif, to the south-east. Its raised esplanade is a genuine paved platform, so a flat. On it:
            //    - the Dome of the Rock: two blocks at yaw 0 and 45 (0.09 and 0.08 S, so their tops never share a plane) form a stepped octagonal faience drum, and a gold Tree stand-in at
            //      the same spot rises out of it as the dome;
            //    - al-Aqsa at the south end;
            //    - two cypresses.
            ctx.PlaceFlat(P(0.30f, -0.20f), yaw, 0.26f * s, 0.30f * s, Esplanade, 400f);
            var dome = P(0.28f, -0.15f);
            ctx.PlaceByWidth(Drum, dome, yaw, 0.09f * s, StandIn.Box, Faience, Slope);
            ctx.PlaceByWidth(Drum, dome, yaw + 45f, 0.08f * s, StandIn.Box, Faience, Slope);
            ctx.PlaceByHeight(Dome, dome, yaw, 0.13f * s, StandIn.Tree, Gold, footprintShare: 0.7f, maxSlopeDeg: Slope);
            ctx.PlaceByWidth(Aqsa, P(0.30f, -0.30f), yaw + 180f, 0.07f * s, StandIn.Box, TowerStone, Slope);
            ctx.PlaceByHeight(Cypress, P(0.40f, -0.08f), ctx.Range(0f, 360f), 0.12f * s, StandIn.Pine, CypressLeaf, footprintShare: 0.3f, maxSlopeDeg: Slope);
            ctx.PlaceByHeight(Cypress, P(0.20f, -0.32f), ctx.Range(0f, 360f), 0.13f * s, StandIn.Pine, CypressLeaf, footprintShare: 0.3f, maxSlopeDeg: Slope);

            // 5. Houses on a tight, lightly jittered grid of lots, 0.11 S apart. Lots in the plaza, under the cluster or on
            //    the esplanade are skipped. About one lot in seven becomes a garden courtyard with a cypress or an olive.
            int houses = 0, yards = 0;
            foreach (float lx in Lots)
                foreach (float lz in Lots)
                {
                    float x = lx + ctx.Range(-0.006f, 0.006f), z = lz + ctx.Range(-0.006f, 0.006f), w = ctx.Range(0.10f, 0.108f);
                    int roll = ctx.Rng.Next(100), turn = ctx.Rng.Next(4);
                    if (!Free(x, z, 0.5f * w)) continue;
                    var at = P(x, z);
                    if (roll < 15 && yards < MaxYards)
                    {
                        ctx.PaintGround("grass", at, yaw, 0.09f * s, 0.09f * s, 0.3f, 0.55f);
                        bool placed = roll % 2 == 0
                            ? ctx.PlaceByHeight(Cypress, at, ctx.Range(0f, 360f), ctx.Range(0.13f, 0.15f) * s, StandIn.Pine, CypressLeaf, footprintShare: 0.3f, maxSlopeDeg: Slope)
                            : ctx.PlaceByHeight(Olive, at, ctx.Range(0f, 360f), ctx.Range(0.11f, 0.12f) * s, StandIn.Tree, OliveLeaf, footprintShare: 0.5f, maxSlopeDeg: Slope);
                        if (placed) yards++;
                        continue;
                    }
                    if (houses < MaxHouses && ctx.PlaceByWidth(House, at, yaw + 90f * turn + ctx.Range(-3f, 3f), w * s, StandIn.Box, Limestone, Slope)) houses++;
                }

            // 6. Outside the walls, kept off the four roads:
            //    - olives on the Mount of Olives (east) and in the valleys (south);
            //    - cypresses to the north and west.
            foreach (float z in new[] { -0.36f, -0.18f, 0.18f, 0.36f })
                ctx.PlaceByHeight(Olive, P(0.66f, z), ctx.Range(0f, 360f), ctx.Range(0.12f, 0.13f) * s, StandIn.Tree, OliveLeaf, footprintShare: 0.5f, maxSlopeDeg: Slope);
            foreach (float x in new[] { -0.30f, -0.14f, 0.24f })
                ctx.PlaceByHeight(Olive, P(x, -0.64f), ctx.Range(0f, 360f), ctx.Range(0.12f, 0.13f) * s, StandIn.Tree, OliveLeaf, footprintShare: 0.5f, maxSlopeDeg: Slope);
            foreach (float x in new[] { -0.24f, 0.24f })
                ctx.PlaceByHeight(Cypress, P(x, 0.64f), ctx.Range(0f, 360f), ctx.Range(0.14f, 0.16f) * s, StandIn.Pine, CypressLeaf, footprintShare: 0.3f, maxSlopeDeg: Slope);
            foreach (float z in new[] { -0.24f, 0.24f })
                ctx.PlaceByHeight(Cypress, P(-0.66f, z), ctx.Range(0f, 360f), ctx.Range(0.14f, 0.16f) * s, StandIn.Pine, CypressLeaf, footprintShare: 0.3f, maxSlopeDeg: Slope);
        }
    }
}
