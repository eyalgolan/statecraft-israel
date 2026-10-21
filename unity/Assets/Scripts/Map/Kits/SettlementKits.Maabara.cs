#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The ma'abara kit (issue #32): the player's transit camps (MapActions.TransitCamp, from 1950) drawn as the camps
    /// of 1950-51 looked, rows of white and khaki canvas tents (ridge and bell tents) on a regular grid with a few tin or
    /// canvas huts, a long communal hut (showers, kitchen) and, in some, a water tower. Built by PlacementLayer when a camp is
    /// placed, never at map build (the boot budget, #321). Seeded by the camp's id; presentation only, no sim state.</summary>
    public static partial class SettlementKits
    {
        /// <summary>The share of a ma'abara's plots that hold a hut rather than a tent: 1950-51, mostly tents. A parameter so a
        /// staged look (more huts as the camps aged) only has to pass another share.</summary>
        public const float MaabaraHutShare = 0.2f;

        /// <summary>The plot grid (columns along the rows, rows across), its pitch (real m, before the house scale) and the
        /// radius of the lot each plot claims: the largest tent or hut (6.5 x 4.5) with a little to spare. The 1948 Haifa
        /// transit-camp photographs (Nationaal Archief, Van de Poll) show tents 2-3 m apart along a row and 5-6 m lanes
        /// between rows. The lot is the same whatever stands on it, so the hut share changes what a plot holds, never which
        /// plots are used.</summary>
        const int MaabaraCols = 5, MaabaraRows = 4;
        const float MaabaraPitchU = 8f, MaabaraPitchV = 9f, MaabaraPlotR = 4f;

        /// <summary>The fewest plots a ma'abara is drawn with, one of them a hut (owner, 15 October 2026: "Yes, min 6 plots + a
        /// hut"): fewer reads as a few sheds, not a camp, so the search goes on and the camp is its box if nowhere in its hex
        /// has room.</summary>
        public const int MaabaraMinPlots = 6;

        /// <summary>How far (house-scale metres) a ma'abara reaches from its centre: the communal hut past the last column
        /// (30 along, 9 to a side) and the rows' half width, rounded up.</summary>
        public const float MaabaraReach = 36f;

        /// <summary>Test seam (internal; the tests see it through InternalsVisibleTo): false lets a ma'abara's plots stand on
        /// the hex grid's edge marks and on steep ground, so a test can show those rules refuse a plot (review #557).</summary>
        internal static bool MaabaraGroundRules = true;

        /// <summary>Builds one ma'abara per entry of <paramref name="camps"/> (seed id, the centre of its hex in world x, z, the
        /// day it was built), in order, on one lot list so no two overlap: a mesh in world space (null when no free ground was
        /// found in the camp's own hex), the tents and huts it holds (the communal hut and the water tower not counted) and its
        /// centre. A camp keeps off the unit tokens present on or after its own day (KitGround.OnToken with a date; T67).</summary>
        public static List<(Mesh? mesh, int tents, int huts, Vector2 at)> Maabarot(IReadOnlyList<(string id, Vector2 at, System.DateTime from)> camps,
            KitGround ground, float hutShare = MaabaraHutShare)
        {
            var lots = new Lots();
            var result = new List<(Mesh?, int, int, Vector2)>();
            foreach (var (id, at, from) in camps)
            {
                var km = new KitMesh();
                var (tents, huts, c) = Maabara(id, at, from, ground, km, lots, hutShare);
                result.Add((km.VertexCount > 0 ? km.ToMesh(id + "_kit") : null, tents, huts, c));
            }
            return result;
        }

        /// <summary>As Maabarot, also listing everything placed (<see cref="Placed"/>: "tent", "hut", "communal_hut",
        /// "water_tower"), in placement order.</summary>
        public static List<(Mesh? mesh, int tents, int huts, Vector2 at)> Maabarot(IReadOnlyList<(string id, Vector2 at, System.DateTime from)> camps,
            KitGround ground, out List<Placed> placed, float hutShare = MaabaraHutShare)
        {
            census = placed = new List<Placed>();
            try { return Maabarot(camps, ground, hutShare); }
            finally { census = null; }
        }

        /// <summary>As Maabarot, for camps with no date: each keeps off every unit token, dated or not.</summary>
        public static List<(Mesh? mesh, int tents, int huts, Vector2 at)> Maabarot(IReadOnlyList<(string id, Vector2 at)> camps, KitGround ground,
            float hutShare = MaabaraHutShare) => Maabarot(Undated(camps), ground, hutShare);

        /// <summary>As Maabarot with <paramref name="placed"/>, for camps with no date: each keeps off every unit token.</summary>
        public static List<(Mesh? mesh, int tents, int huts, Vector2 at)> Maabarot(IReadOnlyList<(string id, Vector2 at)> camps, KitGround ground,
            out List<Placed> placed, float hutShare = MaabaraHutShare) => Maabarot(Undated(camps), ground, out placed, hutShare);

        static List<(string id, Vector2 at, System.DateTime from)> Undated(IReadOnlyList<(string id, Vector2 at)> camps)
        {
            var list = new List<(string, Vector2, System.DateTime)>(camps.Count);
            foreach (var (id, at) in camps) list.Add((id, at, System.DateTime.MinValue));
            return list;
        }

        static (int tents, int huts, Vector2 at) Maabara(string id, Vector2 hexCentre, System.DateTime from, KitGround ground, KitMesh km, Lots lots,
            float hutShare)
        {
            float m = KitScale.Houses, plotR = MaabaraPlotR * m;
            var rnd = new KitRandom(id, 1);
            float yaw = rnd.Range(0f, 90f), a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            Vector2 Turn(float u, float v) => new Vector2(u * ca - v * sa, u * sa + v * ca);
            // As the Bedouin camps' tents (Encampments): off the routes and the units' clearings, the unit tokens (those present
            // on or after the camp's own date, T67), the hex grid's edge marks and steep ground. Each tent, hut, the communal
            // hut and the water tower's spot stands in the camp's own hex, not only its centre (review #557): the rows reach
            // about 1.6 km from the centre at the house scale, the communal hut about 3 km, against the hex's 4.33 km inradius.
            // The whole disc of radius r, not only its centre (review #557, c9): r is at least the half-width of what stands
            // there, so nothing overhangs the hex's edge (HexGrid.EdgeDistance: the drawn grid's own distance field).
            var hex = ground.Hex(hexCentre.x, hexCentre.y);
            bool InHex(Vector2 p, float r) => ground.Hex(p.x, p.y) == hex
                && HexGrid.EdgeDistance(HexGrid.FromWorld(new Vector3(p.x, 0f, p.y), ground.TerrainOrigin, ground.Length), ground.HexSizeM) >= r;
            bool Free(Vector2 p, float r) => ground.Inside(p.x, p.y) && InHex(p, r) && !ground.Water(p.x, p.y) && OffTowns(p, r, ground)
                && !ground.OnRoute(p, r) && !ground.OnToken(p, r, from)
                && (!MaabaraGroundRules || (!ground.OnGridMark(p, r) && ground.Slope(p.x, p.y, 600f) <= 0.12f)) && lots.Free(p, r, id, 0);
            // Huts at an even spread over the plots (a golden-ratio sequence from a seeded phase), not a coin per plot: the
            // camp shows the share asked for, not a lucky draw of it. Drawn before the camp's spot is chosen; the spot draws
            // nothing, so the stream is the one the camp always had (yaw, then phase).
            float phase = rnd.Value();
            bool HutPlot(int k, float share) => Mathf.Repeat(phase + k * 0.618034f, 1f) < share;
            // A plot's centre round a camp centre: the grid turned by the yaw, pegged by eye (the first two draws of the
            // plot's wear stream, as the build below takes them).
            Vector2 PlotAt(Vector2 centre, int i, int j, int k)
            {
                var w = new KitRandom(id, 100UL + (ulong)k);
                return centre + Turn((i - (MaabaraCols - 1) * 0.5f) * MaabaraPitchU * m, (j - (MaabaraRows - 1) * 0.5f) * MaabaraPitchV * m)
                    + new Vector2(w.Range(-0.4f, 0.4f), w.Range(-0.4f, 0.4f)) * m;
            }
            // Owner, 15 October 2026 ("Yes, min 6 plots + a hut"): a camp is at least MaabaraMinPlots free plots, one of them a
            // hut, or the search goes on, further out in the hex. The hut is counted at the 1950-51 share
            // (MaabaraHutShare), not the share asked for, so the share still changes what a plot holds, never where the
            // camp stands. A camp's own lots never block each other (Lots.Free: the same id and group), so this dry run
            // frees exactly the plots the build below fills.
            bool Roomy(Vector2 centre)
            {
                int free = 0; bool hut = false;
                for (int j = 0, k = 0; j < MaabaraRows; j++)
                    for (int i = 0; i < MaabaraCols; i++, k++)
                        if (Free(PlotAt(centre, i, j, k), plotR)) { free++; hut |= HutPlot(k, MaabaraHutShare); }
                return free >= MaabaraMinPlots && hut;
            }
            if (!MaabaraSpot(hexCentre, MaabaraReach * m, from, ground, lots, Roomy, out var c)) return (0, 0, c);
            Vector2 At(float u, float v) => c + Turn(u, v);
            int tents = 0, huts = 0, plot = 0;
            for (int j = 0; j < MaabaraRows; j++)
                for (int i = 0; i < MaabaraCols; i++, plot++)
                {
                    bool hut = HutPlot(plot, hutShare);
                    // Wear (sag, creases, stains, washing) draws from its own stream per plot, so the layout stream above is
                    // the one the camp always had and the hut share still changes what a plot holds, never which plots.
                    var wear = new KitRandom(id, 100UL + (ulong)plot);
                    var p = At((i - (MaabaraCols - 1) * 0.5f) * MaabaraPitchU * m, (j - (MaabaraRows - 1) * 0.5f) * MaabaraPitchV * m)
                        + new Vector2(wear.Range(-0.4f, 0.4f), wear.Range(-0.4f, 0.4f)) * m;   // pegged by eye: the grid reads, not ruled (PlotAt)
                    float pyaw = yaw + rnd.Range(-3f, 3f);   // pitched by hand: straight rows, not ruled ones
                    if (!Free(p, plotR)) continue;
                    lots.Add(new Lot { c = p, r = plotR, id = id, group = 0 });
                    if (hut)
                    {
                        // Corrugated tin (pale grey) or canvas on a wooden frame, under a low gable.
                        float w = rnd.Range(5.5f, 6.5f) * m, d = rnd.Range(3.8f, 4.5f) * m;
                        bool tin = rnd.Chance(0.6f);
                        var wall = tin ? Jitter(C(0.72f, 0.74f, 0.75f), ref rnd, 0.03f) : Jitter(C(0.70f, 0.64f, 0.50f), ref rnd, 0.05f);
                        var roof = tin ? Jitter(C(0.63f, 0.65f, 0.66f), ref rnd, 0.03f) : Jitter(C(0.58f, 0.53f, 0.42f), ref rnd, 0.05f);
                        if (tin) TinHut(km, p, pyaw, w, d, 2.2f, 0.7f, wall, roof, ground, m, ref wear, false);
                        else RidgeTent(km, p, pyaw, w, d, 2.2f, 0.7f, 0.12f, roof, ground, m, ref wear, false);   // canvas on a frame sags less
                        Record(id, "hut", p, pyaw, w, d, 2.2f * m, wall);
                        huts++;
                        continue;
                    }
                    // Canvas, white, khaki or a weathered grey-olive (the 1948 Haifa photographs' tents are mostly dark): the
                    // army ridge tent (low walls under a peaked roof), or a round bell tent.
                    bool white = rnd.Chance(0.6f);
                    var canvas = white ? Jitter(C(0.88f, 0.86f, 0.79f), ref rnd, 0.04f) : Jitter(C(0.72f, 0.67f, 0.52f), ref rnd, 0.05f);
                    if (!white && wear.Chance(0.5f)) canvas = Jitter(C(0.58f, 0.57f, 0.47f), ref wear, 0.05f);
                    if (rnd.Chance(0.15f))
                    {
                        float r = rnd.Range(2.3f, 2.7f) * m;
                        BellTent(km, p, r, canvas, ground, m, ref wear);
                        float a2 = wear.Range(0f, 2f * Mathf.PI);
                        Belongings(km, p + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * (r + 0.5f * m), a2 * Mathf.Rad2Deg, ground, m, ref wear);
                        Record(id, "tent", p, 0f, 2f * r, 2f * r, 0.9f * m, canvas);
                    }
                    else
                    {
                        float w = rnd.Range(5f, 6f) * m, d = rnd.Range(3.6f, 4.2f) * m;
                        RidgeTent(km, p, pyaw, w, d, 1f, 1.6f, 0.3f, canvas, ground, m, ref wear, wear.Chance(0.45f));
                        float pa = pyaw * Mathf.Deg2Rad;
                        Vector2 Local(float u, float v) => p + new Vector2(u * Mathf.Cos(pa) - v * Mathf.Sin(pa), u * Mathf.Sin(pa) + v * Mathf.Cos(pa));
                        // Bundles, a crate or a jerrycan by the door (the +u end), and a washing line in front of some tents,
                        // strung along the row inside the plot's own lot (Haifa 1948: washing between the tents).
                        if (wear.Chance(0.6f)) Belongings(km, Local(0.5f * w + 0.45f * m, wear.Range(-0.7f, 0.7f) * m), pyaw, ground, m, ref wear);
                        if (wear.Chance(0.5f))
                        {
                            float v = (wear.Chance(0.5f) ? 1f : -1f) * (0.5f * d + 1.1f * m);
                            var a0 = Local(-1.7f * m, v); var b0 = Local(1.7f * m, v);
                            WashingLine(km, new Vector3(a0.x, ground.Y(a0.x, a0.y), a0.y), new Vector3(b0.x, ground.Y(b0.x, b0.y), b0.y), m, ref wear);
                        }
                        Record(id, "tent", p, pyaw, w, d, 1f * m, canvas);
                    }
                    tents++;
                }
            if (tents + huts == 0) return (0, 0, c);
            // The communal hut past the last column, across the rows (a plot of the grid's own lattice: clear of its tents by
            // construction).
            var hall = At(((MaabaraCols - 1) * 0.5f * MaabaraPitchU + 14f) * m, 0f);
            float hallR = 0.5f * Mathf.Sqrt(18f * 18f + 6f * 6f) * m;
            if (Free(hall, hallR))
            {
                lots.Add(new Lot { c = hall, r = hallR, id = id, group = 0 });
                var tin = Jitter(C(0.72f, 0.74f, 0.75f), ref rnd, 0.03f);
                var hallWear = new KitRandom(id, 99);
                TinHut(km, hall, yaw + 90f, 18f * m, 6f * m, 2.6f, 1f, tin, Jitter(C(0.63f, 0.65f, 0.66f), ref rnd, 0.03f), ground, m, ref hallWear, true);
                Record(id, "communal_hut", hall, yaw + 90f, 18f * m, 6f * m, 2.6f * m, tin);
            }
            // A water tower at the other end in some camps (the moshavot's, SettlementKits.Landmarks.cs). Its spot moves up to
            // 20 m (house scale) from the one asked for, so that whole reach must be off the towns. That shared path (Claim)
            // tests water, lots and routes but not slope or grid marks, so the spot its Claim (4 m) will take is found first on
            // a copy of the lots and must be Free as a tent's is (review #557).
            var tower = At(-((MaabaraCols - 1) * 0.5f * MaabaraPitchU + 10f) * m, 0f);
            if (rnd.Chance(0.5f) && OffTowns(tower, 20f * m, ground) && !ground.OnToken(tower, 20f * m, from))
            {
                var probe = new Lots();
                foreach (var l in lots.List) probe.Add(l);
                if (Claim(probe, tower, 4f * m, id, ground, out var spot) && Free(spot, 4f * m)) WaterTower(id, tower, ground, km, lots, model: false);
            }
            return (tents, huts, c);
        }

        // Wear (issue #32, the owner's "creases and breaks, like it really looked"): what the 1948 Haifa transit-camp
        // photographs show and a box cannot. sRGB targets the canvas and tin weather towards: sun-bleach on the roofs, the
        // dust a skirt drags in, rust, and the dark of an open door or a missing sheet. Alpha 0 = Facade.Plain.
        static readonly Color32 Bleach = new Color32(242, 240, 226, 0), Dust = new Color32(128, 107, 82, 0),
            Rust = new Color32(140, 84, 51, 0), Dark = new Color32(46, 41, 36, 0);

        /// <summary>Washing on the lines: white, faded blue, dull red, grey and ochre.</summary>
        static readonly Color32[] Laundry =
        {
            new Color32(235, 232, 222, 0), new Color32(115, 140, 173, 0), new Color32(168, 77, 64, 0), new Color32(153, 153, 148, 0),
            new Color32(204, 173, 102, 0),
        };

        /// <summary>What stands by a door: a wooden crate, a cloth bundle, an olive jerrycan, a blanket roll.</summary>
        static readonly Color32[] Things =
        {
            new Color32(140, 107, 71, 0), new Color32(166, 150, 120, 0), new Color32(89, 97, 64, 0), new Color32(120, 64, 56, 0),
        };

        /// <summary><paramref name="c"/> moved <paramref name="t"/> of the way to <paramref name="toward"/>, times
        /// <paramref name="k"/>, alpha 0 (Facade.Plain).</summary>
        static Color32 Weather(Color32 c, Color32 toward, float t, float k)
        {
            var x = Color32.Lerp(c, toward, t);
            return new Color32((byte)Mathf.Min(255f, x.r * k), (byte)Mathf.Min(255f, x.g * k), (byte)Mathf.Min(255f, x.b * k), 0);
        }

        /// <summary>A quad turned to face away from <paramref name="inside"/>: sagging canvas has no fixed corner order, so the
        /// winding is worked out rather than kept by hand.</summary>
        static void FaceOut(KitMesh km, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside, Color32 colour)
        {
            if (Vector3.Dot(Vector3.Cross(c - a, d - b), a + b + c + d - 4f * inside) < 0f) (b, d) = (d, b);
            colour.a = 0;
            km.Face(a, b, c, d, colour, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        }

        /// <summary>A triangle turned to face away from <paramref name="inside"/>.</summary>
        static void TriOut(KitMesh km, Vector3 a, Vector3 b, Vector3 c, Vector3 inside, Color32 colour)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c - 3f * inside) < 0f) (b, c) = (c, b);
            colour.a = 0;
            km.Triangle(a, b, c, colour);
        }

        /// <summary>A quad seen from both sides (washing, a door flap): two faces back to back. A collapsed edge (d = c) makes
        /// it a triangle; Face takes its normal from the diagonals, so that stays finite.</summary>
        static void TwoSided(KitMesh km, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 colour)
        {
            colour.a = 0;
            km.Face(a, b, c, d, colour, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            km.Face(d, c, b, a, colour, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        }

        /// <summary>A flat lid hidden inside a tent or hut, facing up, a little under its eaves. The sun's shadow pass culls
        /// back faces and pushes every vertex out along its own normal (URP's normal bias, Kit.shader), so the seams between
        /// the many small faces of sagging canvas or ribbed tin open into hairline cracks; through them the sun would
        /// reach the ground behind the shelter as rows of bright dots in its shadow. The lid catches that light.</summary>
        static void ShadowLid(KitMesh km, Vector3[] rim, Color32 colour)
        {
            var c = Vector3.zero;
            foreach (var v in rim) c += v;
            c /= rim.Length;
            colour.a = 0;
            for (int i = 0; i < rim.Length; i++)
            {
                Vector3 a = rim[i], b = rim[(i + 1) % rim.Length];
                if (Vector3.Cross(b - c, a - c).y > 0f) km.Triangle(c, b, a, colour); else km.Triangle(c, a, b, colour);
            }
        }

        /// <summary>A canvas ridge tent (or a canvas hut on a wooden frame): <paramref name="wallM"/> walls under a ridge
        /// <paramref name="riseM"/> higher (real m, times the house scale <paramref name="m"/>), the ridge along w. Canvas is
        /// never taut: the ridge sags <paramref name="sagM"/> between its three poles (which stand out as the peaks), the roof
        /// panels droop mid-span, the walls fold in and out a little per column, the skirt is pegged out unevenly, and some
        /// tents lean. Weathered in the vertex colour: bleached high on the roof, dusty along the skirt, now and then a
        /// patch of other canvas. <paramref name="door"/> pins the +w end's flap open on the dark inside. Flat-shaded, so
        /// every fold catches the sun on its own.</summary>
        static void RidgeTent(KitMesh km, Vector2 p, float yaw, float w, float d, float wallM, float riseM, float sagM, Color32 canvas,
            KitGround ground, float m, ref KitRandom wear, bool door)
        {
            const int N = 8, Span = 4;   // columns along the ridge; a pole every Span columns (both ends and the middle)
            var (bottom, top) = GroundSpan(p, yaw, w, d, ground, m);
            float a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            float lean = wear.Chance(0.4f) ? wear.Range(-0.12f, 0.12f) : 0f;   // across the ridge, per metre of height
            Vector3 P(float u, float v, float y)
            {
                v += lean * Mathf.Max(0f, y - top);
                return new Vector3(p.x + u * ca - v * sa, y, p.y + u * sa + v * ca);
            }
            float eave = top + wallM * m, ridge = eave + riseM * m, knee = top + 0.35f * m;
            var R = new Vector3[N + 1];
            Vector3[,] E = new Vector3[2, N + 1], Mi = new Vector3[2, N + 1], K = new Vector3[2, N + 1], B = new Vector3[2, N + 1];
            for (int i = 0; i <= N; i++)
            {
                float u = (i / (float)N - 0.5f) * w, between = Mathf.Sin(Mathf.PI * (i % Span) / Span), sag = sagM * m * between;
                R[i] = P(u, 0f, ridge - sag);
                for (int s = 0; s < 2; s++)
                {
                    float side = s == 0 ? -1f : 1f, crease = wear.Range(-0.08f, 0.08f) * m, flare = wear.Range(0.06f, 0.24f) * m;
                    float ey = eave - 0.4f * sag;   // the wall tops sag with the ridge, less
                    E[s, i] = P(u, side * (0.5f * d + 0.5f * crease), ey);
                    K[s, i] = P(u, side * (0.5f * d + flare + crease), knee + wear.Range(-0.08f, 0.05f) * m);
                    B[s, i] = P(u, side * (0.5f * d + flare + crease), bottom);
                    Mi[s, i] = P(u, side * (0.25f * d + 0.6f * crease), 0.5f * (ey + ridge - sag) - sagM * m * (0.35f + 0.65f * between));
                }
            }
            var inside = P(0f, 0f, top + 0.3f * wallM * m);
            float lid = eave - 0.5f * sagM * m - 0.05f * m, lu = 0.5f * w - 0.1f * m, lv = 0.5f * d - 0.1f * m;
            ShadowLid(km, new[] { P(-lu, -lv, lid), P(lu, -lv, lid), P(lu, lv, lid), P(-lu, lv, lid) }, Dark);
            // Up to two patches (a roof cell each: side, column, lower or upper half) sewn from the other canvas.
            int patchA = wear.Range(-2 * N, 4 * N), patchB = wear.Range(-2 * N, 4 * N);
            var patch = Weather(canvas, canvas.r > 200 ? new Color32(160, 150, 115, 0) : Bleach, 0.5f, 1f);
            for (int i = 0; i < N; i++)
            {
                float k = 1f + wear.Range(-0.05f, 0.05f);
                for (int s = 0; s < 2; s++)
                {
                    int cell = (s * N + i) * 2;
                    FaceOut(km, B[s, i], K[s, i], K[s, i + 1], B[s, i + 1], inside, Weather(canvas, Dust, 0.45f, 0.82f * k));
                    FaceOut(km, K[s, i], E[s, i], E[s, i + 1], K[s, i + 1], inside, Weather(canvas, Dust, 0.08f, k));
                    FaceOut(km, E[s, i], Mi[s, i], Mi[s, i + 1], E[s, i + 1], inside,
                        cell == patchA || cell == patchB ? patch : Weather(canvas, Bleach, 0.1f, k));
                    FaceOut(km, Mi[s, i], R[i], R[i + 1], Mi[s, i + 1], inside,
                        cell + 1 == patchA || cell + 1 == patchB ? patch : Weather(canvas, Bleach, 0.3f, k));
                }
            }
            for (int e = 0; e < 2; e++)
            {
                int c = e * N;
                FaceOut(km, B[0, c], K[0, c], K[1, c], B[1, c], inside, Weather(canvas, Dust, 0.45f, 0.8f));
                FaceOut(km, K[0, c], E[0, c], E[1, c], K[1, c], inside, Weather(canvas, Dust, 0.08f, 0.97f));
                // The end's gable follows the drooping roof edge: a fan from the middle of its foot.
                var g = 0.5f * (E[0, c] + E[1, c]); var gc = Weather(canvas, Bleach, 0.15f, 0.97f);
                TriOut(km, E[0, c], Mi[0, c], g, inside, gc); TriOut(km, Mi[0, c], R[c], g, inside, gc);
                TriOut(km, R[c], Mi[1, c], g, inside, gc); TriOut(km, Mi[1, c], E[1, c], g, inside, gc);
            }
            if (door)
            {
                // The doorway, dark, and its flap swung out on the doorway's edge.
                float u = 0.5f * w + 0.03f * m, dh = top + 0.7f * (wallM + riseM) * m, half = 0.45f * m;
                TriOut(km, P(u, -half, top), P(u, 0f, dh), P(u, half, top), inside, Dark);
                var flapEnd = P(u + 0.7f * m, half + 0.55f * m, top + 0.15f * m);
                TwoSided(km, P(u, half, top), P(u, 0f, dh), flapEnd, flapEnd, Weather(canvas, Dust, 0.15f, 0.9f));
            }
        }

        /// <summary>A round bell tent of radius <paramref name="r"/>: a short wall under a cone, the hem pulled out at the
        /// pegs (scalloped), the cone sagging between its seams, the pole leaning the apex off centre, a dust band round the
        /// foot, bleached on top; in some the door panel is open on the dark inside.</summary>
        static void BellTent(KitMesh km, Vector2 p, float r, Color32 canvas, KitGround ground, float m, ref KitRandom wear)
        {
            const int S = 10;
            var (bottom, top) = GroundSpan(p, 0f, 2f * r, 2f * r, ground, m);
            float wall = top + 0.9f * m, turn = wear.Range(0f, 2f * Mathf.PI);
            var lean = new Vector2(wear.Range(-0.3f, 0.3f), wear.Range(-0.3f, 0.3f)) * m;
            var apex = new Vector3(p.x + lean.x, wall + 2.2f * m, p.y + lean.y);
            var inside = new Vector3(p.x, top + 0.3f * m, p.y);
            Vector3[] B = new Vector3[S], K = new Vector3[S], W = new Vector3[S], Mi = new Vector3[S];
            for (int i = 0; i < S; i++)
            {
                float th = turn + 2f * Mathf.PI * i / S;
                var dir = new Vector2(Mathf.Cos(th), Mathf.Sin(th));
                var hem = p + dir * r * (1f + (i % 2 == 0 ? 0.1f : 0.02f) + wear.Range(0f, 0.05f));   // pulled out at the pegs
                B[i] = new Vector3(hem.x, bottom, hem.y);
                K[i] = new Vector3(hem.x, top + (0.3f + wear.Range(-0.06f, 0.04f)) * m, hem.y);
                var wv = p + dir * r * (1f + wear.Range(-0.03f, 0.03f)) + lean * 0.25f;
                W[i] = new Vector3(wv.x, wall, wv.y);
                Mi[i] = Vector3.Lerp(W[i], apex, 0.5f) + Vector3.down * ((i % 2 == 1 ? 0.2f : 0.05f) * m);   // seams at the pegs
            }
            var rim = new Vector3[S];
            for (int i = 0; i < S; i++) rim[i] = Vector3.Lerp(new Vector3(p.x, wall, p.y), W[i], 0.92f) + Vector3.down * (0.1f * m);
            ShadowLid(km, rim, Dark);
            int doorAt = wear.Chance(0.4f) ? wear.Range(0, S) : -1, patchAt = wear.Chance(0.5f) ? wear.Range(0, S) : -1;
            for (int i = 0; i < S; i++)
            {
                int j = (i + 1) % S;
                float k = 1f + wear.Range(-0.05f, 0.05f);
                FaceOut(km, B[i], K[i], K[j], B[j], inside, Weather(canvas, Dust, 0.45f, 0.82f * k));
                FaceOut(km, K[i], W[i], W[j], K[j], inside, i == doorAt ? Dark : Weather(canvas, Dust, 0.1f, k));
                FaceOut(km, W[i], Mi[i], Mi[j], W[j], inside, i == patchAt ? Weather(canvas, Dust, 0.35f, 0.9f) : Weather(canvas, Bleach, 0.1f, k));
                TriOut(km, Mi[i], apex, Mi[j], inside, Weather(canvas, Bleach, 0.3f, k));
            }
        }

        /// <summary>One sheet's colour: galvanised tin a shade lighter or darker, one in five rusting, now and then a newer,
        /// brighter one.</summary>
        static Color32 SheetColour(Color32 tin, ref KitRandom wear)
        {
            float k = 1f + wear.Range(-0.04f, 0.04f);
            if (wear.Chance(0.2f)) return Weather(tin, Rust, wear.Range(0.25f, 0.6f), 0.94f * k);
            return Weather(tin, tin, 0f, wear.Chance(0.1f) ? 1.12f : k);
        }

        /// <summary>A corrugated-tin hut (or the communal hut) <paramref name="w"/> x <paramref name="d"/> (w the longer),
        /// walls and roof of separate 0.9 m sheets, each its own shade and some rusting. The roof's sheets carry one
        /// corrugation each (two flanks lit differently: it reads as ribbing at the in-play zoom), and the roof shows its
        /// years: a sheet missing (the dark inside), one lifted by the wind and askew, others laid crooked.
        /// <paramref name="patched"/> (the communal hut) adds a run of boards nailed over the wall and a tar-paper patch
        /// on the roof.</summary>
        static void TinHut(KitMesh km, Vector2 p, float yaw, float w, float d, float wallM, float riseM, Color32 tin, Color32 roof,
            KitGround ground, float m, ref KitRandom wear, bool patched)
        {
            var (bottom, top) = GroundSpan(p, yaw, w, d, ground, m);
            float a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            Vector3 P(float u, float v, float y) => new Vector3(p.x + u * ca - v * sa, y, p.y + u * sa + v * ca);
            float eave = top + wallM * m, ridge = eave + riseM * m, sheet = 0.9f * m;
            var inside = P(0f, 0f, top + 0.5f * wallM * m);
            var wood = new Color32(128, 102, 71, 0);
            Vector2[] corners = { new Vector2(-0.5f * w, -0.5f * d), new Vector2(0.5f * w, -0.5f * d), new Vector2(0.5f * w, 0.5f * d), new Vector2(-0.5f * w, 0.5f * d) };
            int boardsFrom = patched ? wear.Range(2, 8) : -99;
            for (int e = 0; e < 4; e++)
            {
                var c0 = corners[e]; var c1 = corners[(e + 1) % 4];
                int n = Mathf.Max(1, Mathf.RoundToInt((c1 - c0).magnitude / sheet));
                for (int k = 0; k < n; k++)
                {
                    var q0 = Vector2.Lerp(c0, c1, k / (float)n); var q1 = Vector2.Lerp(c0, c1, (k + 1) / (float)n);
                    var col = e == 0 && k >= boardsFrom && k < boardsFrom + 4 ? Weather(wood, wood, 0f, 1f + wear.Range(-0.08f, 0.08f)) : SheetColour(tin, ref wear);
                    FaceOut(km, P(q0.x, q0.y, bottom), P(q0.x, q0.y, eave), P(q1.x, q1.y, eave), P(q1.x, q1.y, bottom), inside, col);
                }
            }
            for (int e = -1; e <= 1; e += 2)
                TriOut(km, P(e * 0.5f * w, -0.5f * d, eave), P(e * 0.5f * w, 0f, ridge), P(e * 0.5f * w, 0.5f * d, eave), inside, Weather(tin, tin, 0f, 0.97f));
            float hu = 0.5f * w - 0.05f * m, hv = 0.5f * d - 0.05f * m, hy = eave - 0.05f * m;
            ShadowLid(km, new[] { P(-hu, -hv, hy), P(hu, -hv, hy), P(hu, hv, hy), P(-hu, hv, hy) }, Dark);
            // The roof: sheets from the eaves (0.2 m overhang) to the ridge, 0.15 m past each gable.
            float over = 0.2f * m, vEdge = 0.5f * d + over, yEdge = eave - riseM / (0.5f * d) * m * over, lo = -0.5f * w - 0.15f * m, len = w + 0.3f * m;
            int ns = Mathf.Max(1, Mathf.RoundToInt(len / sheet));
            float amp = 0.08f * m;
            int tarFrom = patched ? wear.Range(0, Mathf.Max(1, ns - 4)) : -99;
            for (int s = 0; s < 2; s++)
            {
                float side = s == 0 ? -1f : 1f;
                for (int k = 0; k < ns; k++)
                {
                    float u0 = lo + len * k / ns, u1 = lo + len * (k + 1) / ns, uc = 0.5f * (u0 + u1);
                    bool tar = s == 0 && k >= tarFrom && k < tarFrom + 4;
                    if (!tar && wear.Chance(0.05f))   // a sheet gone: the rafters' dark
                    {
                        FaceOut(km, P(u0, side * vEdge, yEdge), P(u0, 0f, ridge), P(u1, 0f, ridge), P(u1, side * vEdge, yEdge), inside, Dark);
                        continue;
                    }
                    float lift0 = 0f, lift1 = 0f, shift = 0f;
                    if (!tar && wear.Chance(0.12f))   // lifted by the wind at its lower edge, one corner more: dark under it
                    {
                        lift0 = wear.Range(0.15f, 0.4f) * m; lift1 = lift0 * wear.Range(0.2f, 0.8f);
                        FaceOut(km, P(u0, side * vEdge, yEdge), P(u0, 0f, ridge), P(u1, 0f, ridge), P(u1, side * vEdge, yEdge), inside, Dark);
                    }
                    else if (wear.Chance(0.2f)) shift = wear.Range(-0.12f, 0.12f) * m;   // laid crooked
                    var col = tar ? Weather(Dark, Dark, 0f, 1.3f + wear.Range(-0.1f, 0.1f)) : SheetColour(roof, ref wear);
                    var up = Vector3.up;
                    var e0 = P(u0 + shift, side * vEdge, yEdge) + up * lift0; var e1 = P(u1 + shift, side * vEdge, yEdge) + up * lift1;
                    var ec = P(uc + shift, side * vEdge, yEdge) + up * (0.5f * (lift0 + lift1) + amp);
                    var t0 = P(u0 + 0.3f * shift, 0f, ridge); var t1 = P(u1 + 0.3f * shift, 0f, ridge); var tc = P(uc + 0.3f * shift, 0f, ridge) + up * amp;
                    FaceOut(km, e0, t0, tc, ec, inside, col);
                    FaceOut(km, ec, tc, t1, e1, inside, Weather(col, col, 0f, 0.88f));   // the rib's shaded flank
                }
            }
        }

        /// <summary>A washing line between two posts (ground points <paramref name="a"/>, <paramref name="b"/>), sagging, with
        /// three to five pieces of washing in mixed colours hung along it.</summary>
        static void WashingLine(KitMesh km, Vector3 a, Vector3 b, float m, ref KitRandom wear)
        {
            var wood = new Color32(110, 88, 62, 0);
            float h = 2f * m, post = 0.06f * m, th = 0.05f * m;
            km.Shaft(a + Vector3.down * (0.5f * m), a + Vector3.up * (h + 0.1f * m), post, post, wood, 4);
            km.Shaft(b + Vector3.down * (0.5f * m), b + Vector3.up * (h + 0.1f * m), post, post, wood, 4);
            Vector3 Line(float t) => Vector3.Lerp(a, b, t) + Vector3.up * (h - 0.25f * m * 4f * t * (1f - t));
            var rope = new Color32(180, 175, 160, 0);
            for (int k = 0; k < 2; k++)
            {
                Vector3 l0 = Line(0.5f * k), l1 = Line(0.5f * (k + 1));
                TwoSided(km, l0, l0 + Vector3.up * th, l1 + Vector3.up * th, l1, rope);
            }
            var along = b - a; along.y = 0f; float length = along.magnitude; along /= Mathf.Max(length, 1e-3f);
            var across = Vector3.Cross(Vector3.up, along);
            int n = wear.Range(3, 6);
            float at = wear.Range(0.06f, 0.15f);
            for (int k = 0; k < n && at < 0.9f; k++)
            {
                float cw = wear.Range(0.5f, 0.95f) * m / length, ch = wear.Range(0.55f, 1f) * m;
                Vector3 top0 = Line(at), top1 = Line(Mathf.Min(at + cw, 0.95f)), sway = across * (wear.Range(-0.05f, 0.05f) * m);
                TwoSided(km, top0 + Vector3.down * ch + sway, top0, top1, top1 + Vector3.down * ch + sway,
                    Weather(Laundry[wear.Range(0, Laundry.Length)], Dust, wear.Range(0f, 0.15f), 1f));
                at += cw + wear.Range(0.03f, 0.12f);
            }
        }

        /// <summary>One to three small things by a door at <paramref name="at"/>: crates, bundles, a jerrycan.</summary>
        static void Belongings(KitMesh km, Vector2 at, float yaw, KitGround ground, float m, ref KitRandom wear)
        {
            int n = wear.Range(1, 4);
            for (int k = 0; k < n; k++)
            {
                var q = at + (k == 0 ? Vector2.zero : new Vector2(wear.Range(-0.35f, 0.35f), wear.Range(-0.35f, 0.35f)) * m);
                float s = wear.Range(0.35f, 0.65f) * m, hgt = wear.Range(0.3f, 0.6f) * m, y = ground.Y(q.x, q.y);
                var col = Weather(Things[wear.Range(0, Things.Length)], Dust, wear.Range(0f, 0.2f), 1f);
                km.Box(q.x, q.y, yaw + wear.Range(-25f, 25f), s, s * wear.Range(0.6f, 1f), y - 0.2f * m, y, hgt, col, Weather(col, Bleach, 0.2f, 1f), Facade.Plain, m);
            }
        }

        /// <summary>The camp's centre: the dry spot nearest <paramref name="want"/> (rings an eighth of a reach apart) in the hex
        /// <paramref name="want"/> stands in, whose inner half stays off every town's plots, that is gentle and whose whole
        /// reach is clear of the unit tokens present on or after <paramref name="from"/> (Encampments' tests; T67) and of the
        /// camps built before it, and that is <paramref name="roomy"/> (the owner's minimum camp). The player picks the hex,
        /// so the camp stands in it, where selection and the info panel point (review #557). False when there is none: the
        /// camp is then drawn as its box.</summary>
        static bool MaabaraSpot(Vector2 want, float reach, System.DateTime from, KitGround ground, Lots lots, System.Func<Vector2, bool> roomy,
            out Vector2 at)
        {
            // From any point of a hex the whole hex lies within two circumradii. The rings are an eighth of a reach apart (450 m
            // at the house scale), not half of one (1.8 km): the ma'abarot stood at the edges of the towns (T67, issue #32), and
            // in the big towns' hexes the free ground at a town's edge fell between the coarse rings, so the camp was its box.
            // The inner half that must stay off the towns is unchanged.
            float step = reach * 0.125f, clear = reach * 0.5f, max = 2f * ground.HexSizeM;
            var hex = ground.Hex(want.x, want.y);
            for (int ring = 0; ring * step <= max; ring++)
            {
                int around = ring == 0 ? 1 : 6 * ring;
                for (int k = 0; k < around; k++)
                {
                    float a = 2f * Mathf.PI * k / around;
                    var p = want + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (ring * step);
                    if (!ground.Inside(p.x, p.y) || ground.Hex(p.x, p.y) != hex || ground.Water(p.x, p.y) || !OffTowns(p, clear, ground) || ground.Slope(p.x, p.y, 600f) > 0.12f
                        || ground.OnToken(p, reach, from) || !lots.Free(p, reach) || !roomy(p)) continue;
                    at = p; return true;
                }
            }
            at = want; return false;
        }

        /// <summary>No kit footprint (a town's, a 1948 locality's or a Bedouin camp's, out to <see cref="PlotReach"/> of its
        /// radius, where a town's plots end) reaches a disc of radius <paramref name="r"/> at <paramref name="p"/>. A linear
        /// scan: some 350 footprints, and only when a camp is placed.</summary>
        static bool OffTowns(Vector2 p, float r, KitGround ground)
        {
            foreach (var f in ground.Footprints)
            {
                float d = f.radius * PlotReach + r;
                if ((p - f.centre).sqrMagnitude < d * d) return false;
            }
            return true;
        }
    }
}
