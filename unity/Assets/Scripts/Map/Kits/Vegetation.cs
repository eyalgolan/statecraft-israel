#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>CitrusPatch is four citrus trees in a 2 x 2 block of a grove's rows, drawn as one instance: the trees' cost on
    /// WebGL is per instance (the matrices uploaded each frame), not per triangle, so the dense groves are instanced in fours.</summary>
    public enum TreeKind { Citrus, Olive, Palm, Cypress, CitrusPatch }

    /// <summary>Procedural trees at the symbolic scale (issue #34): citrus groves (dense rows of round dark olive-brown trees with
    /// fruit specks) on the coastal plain round the towns, olives scattered over the hills, palms by the shore of the coastal
    /// towns, cypress windbreaks by villages. There is no land-use layer yet (Deferred row "Land-use texture"), so where
    /// each grows is a seeded rule over the biome, elevation, slope and the settlements; every draw is seeded (no
    /// System.Random), so every load of a build grows the same trees.</summary>
    public static class Vegetation
    {
        /// <summary>One tree's mesh in real metres (the instance matrix applies KitScale.Trees).</summary>
        public static Mesh BuildMesh(TreeKind kind) => Build(kind).ToMesh("Tree_" + kind);

        /// <summary>The tree's parts before upload (tests read them).</summary>
        public static KitMesh Build(TreeKind kind)
        {
            var km = new KitMesh();
            var rnd = new KitRandom("tree:" + kind, 3);
            Color32 C(float r, float g, float b) => new Color32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), 0);
            switch (kind)
            {
                case TreeKind.Citrus:
                    CitrusTree(km, Vector3.zero, 1f, ref rnd);
                    break;
                case TreeKind.CitrusPatch:
                    // A grove's row spacing (GroveStepM) apart, each tree a little off its spot and its own size.
                    for (int i = 0; i < 4; i++)
                    {
                        var at = new Vector3(((i & 1) - 0.5f) * GroveStepM + rnd.Range(-0.3f, 0.3f), 0f, ((i >> 1) - 0.5f) * GroveStepM + rnd.Range(-0.3f, 0.3f));
                        CitrusTree(km, at, rnd.Range(0.88f, 1.06f), ref rnd, rnd.Range(0.88f, 1.1f));   // each its own size and shade
                    }
                    break;
                case TreeKind.Olive:
                    km.Shaft(new Vector3(0, -0.6f, 0), new Vector3(0.15f, 1.7f, 0.1f), 0.32f, 0.22f, C(0.36f, 0.31f, 0.25f), 4);
                    km.Crown(new Vector3(0.5f, 2.9f, 0.2f), new Vector3(2.0f, 1.5f, 1.9f), C(0.55f, 0.53f, 0.39f), ref rnd, 0.16f, 7, 4);
                    km.Crown(new Vector3(-0.8f, 2.6f, -0.4f), new Vector3(1.6f, 1.3f, 1.5f), C(0.52f, 0.50f, 0.37f), ref rnd, 0.18f, 6, 4);
                    km.Crown(new Vector3(-0.1f, 3.5f, 0.6f), new Vector3(1.3f, 1.0f, 1.3f), C(0.60f, 0.58f, 0.44f), ref rnd, 0.15f, 6, 3);
                    break;
                case TreeKind.Palm:
                {
                    var trunk = C(0.48f, 0.40f, 0.30f);
                    var mid = new Vector3(0.35f, 6f, 0.1f); var top = new Vector3(0.9f, 11.5f, 0.25f);
                    km.Shaft(new Vector3(0, -0.6f, 0), mid, 0.30f, 0.24f, trunk, 6);
                    km.Shaft(mid, top, 0.24f, 0.21f, trunk, 6);
                    for (int i = 0; i < 9; i++)
                    {
                        float a = i * Mathf.PI * 2f / 9f + rnd.Range(-0.15f, 0.15f);
                        km.Frond(top, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), rnd.Range(3.8f, 4.6f), 1.1f, rnd.Range(0.55f, 0.85f),
                            i % 3 == 0 ? C(0.40f, 0.44f, 0.20f) : C(0.33f, 0.38f, 0.17f));
                    }
                    break;
                }
                default:
                    km.Shaft(new Vector3(0, -0.6f, 0), new Vector3(0, 0.8f, 0), 0.2f, 0.18f, C(0.30f, 0.24f, 0.18f), 5);
                    km.Crown(new Vector3(0, 4.6f, 0), new Vector3(1.05f, 4.4f, 1.05f), C(0.20f, 0.25f, 0.13f), ref rnd, 0.08f, 7, 7, 0f, default, 0.9f);
                    break;
            }
            return km;
        }

        /// <summary>A tree's crown radius in real metres before the symbolic scale (a citrus patch: its four trees' extent).</summary>
        public static float CrownRadiusM(TreeKind k) => k switch
        {
            TreeKind.Citrus => 2.4f, TreeKind.CitrusPatch => 0.5f * GroveStepM * 1.42f + 2.4f, TreeKind.Olive => 2.8f,
            TreeKind.Palm => 4.6f, _ => 1.2f,
        };

        /// <summary>The hills' scatter (Plant): one try per cell this wide (m), kept at about this share (a broad noise
        /// takes it from 0.4 to 1.6 times), this share of it low scrub (a small, squat olive), the rest single olives.</summary>
        public const float ScatterCellM = 800f, ScatterShare = 0.85f, ScrubShare = 0.35f;
        /// <summary>Visual gap L1 (issue #34; owner, 15 Oct 2026: "~1.7x bigger, fewer trees", the plan's option "hill olives
        /// only"): the hill olives (the clumps' trees on the hills and mountains and the scatter's single olives, not its
        /// scrub; the plain's and the valleys' olives are unchanged) are this many times the size, to
        /// promo 03's crowns as measured on screen at gameplay03, and only this share of them is kept, so the hills keep
        /// ground between the crowns and the instance count falls. CrownRadiusM, the mesh and every other tree are unchanged;
        /// the keep-outs (lots, routes, footprints, groves, grid marks) and the tree holes under units read the size.</summary>
        public const float HillOliveSize = 1.7f, HillOliveShare = 0.6f;
        /// <summary>The widest crown (m) the grove index is asked about (its query reach past a grove's disc).</summary>
        const float ScatterKeepM = 1500f;

        /// <summary>Real metres between the trees of a grove's rows.</summary>
        public const float GroveStepM = 4.9f;

        /// <summary>Promo 03's orange tree: a dark, lumpy crown of three clusters, heavy with fruit.</summary>
        static void CitrusTree(KitMesh km, Vector3 at, float size, ref KitRandom rnd, float shade = 1f)
        {
            Color32 C(float r, float g, float b) => new Color32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), 0);
            var leaf = C(0.32f * shade, 0.28f * shade, 0.18f * shade); var fruit = C(0.92f, 0.50f, 0.09f);
            km.Shaft(at + new Vector3(0, -0.6f, 0), at + new Vector3(0, 1.0f, 0) * size, 0.18f * size, 0.14f * size, C(0.33f, 0.25f, 0.17f), 4);
            km.Crown(at + new Vector3(0, 2.3f, 0) * size, new Vector3(2.1f, 1.8f, 2.1f) * size, leaf, ref rnd, 0.10f, 7, 5, 0.13f, fruit, 0.55f);
            km.Crown(at + new Vector3(0.9f, 2.9f, 0.5f) * size, new Vector3(1.3f, 1.1f, 1.3f) * size, leaf, ref rnd, 0.12f, 6, 4, 0.16f, fruit, 0.5f);
            km.Crown(at + new Vector3(-0.8f, 2.8f, -0.6f) * size, new Vector3(1.2f, 1.0f, 1.2f) * size, leaf, ref rnd, 0.12f, 6, 4, 0.16f, fruit, 0.5f);
        }

        /// <summary>Where each tree stands: (kind, world matrix) for the whole map, seeded.</summary>
        public static List<(TreeKind kind, Matrix4x4 m)> Plant(IReadOnlyList<Settlement> settlements, KitGround ground)
        {
            var trees = new List<(TreeKind, Matrix4x4)>();
            float s = KitScale.Trees;
            // Counts fall with the square of the tree scale (k = 1 at the default), so a larger tree covers the same ground
            // with fewer instances instead of burying the towns.
            float ks = s / KitScale.CountReference;
            void Add(TreeKind k, float x, float z, ref KitRandom rnd, float size = 1f, float yawDeg = float.NaN, float squash = 1f)
            {
                // The tree's own numbers first, whether it stands or not: a tree left out for a lot or a route then never
                // shifts the trees drawn after it (towns round 2: a town's new lots no longer reshuffle every grove).
                var scale = s * size * rnd.Range(0.85f, 1.15f);
                float spin = rnd.Range(0f, 360f), stretch = rnd.Range(0.9f, 1.1f);
                // No crown reaches into a building's or landmark's lot (integration bug B9: palms through Tel Aviv's roofs,
                // grove rows through blocks).
                // Nor into a railway's or road's corridor (the routes run through the groves otherwise).
                float crown = CrownRadiusM(k) * s * size * 1.15f;
                if (ground.OnLot(x, z, crown) || ground.OnRoute(new Vector2(x, z), crown)) return;
                float y = ground.Y(x, z);
                float yaw = float.IsNaN(yawDeg) ? spin : yawDeg;
                trees.Add((k, Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), new Vector3(scale, scale * stretch * squash, scale))));
            }

            // Citrus groves round the coastal-plain towns: rectangular blocks of rows, each grove on its own angle. Each grove's
            // disc (centre; half its diagonal plus a patch's crown) is kept, so the hills' scatter below stays out of it.
            var groveDiscs = new DiscIndex(1f, ScatterKeepM);
            foreach (var st in settlements)
            {
                var c3 = ground.Frame.ToWorld(st.Lon, st.Lat, 0); var c = new Vector2(c3.x, c3.z);
                var biome = ground.Biome(c.x, c.y);
                if (biome != "coastal_plain" && biome != "lowland" && biome != "valley" && biome != "sea") continue;
                if (ground.ElevM(c.x, c.y) > 300f) continue;
                var rnd = new KitRandom(st.Id, 11);
                float R = SettlementKits.FootprintRadiusM(st, ground.HexSizeM);
                int groves = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(KitPopulation.Of(st.Id), 400)) / 9f / ks), 1, 28);
                for (int g = 0, tries = 0; g < groves && tries < groves * 12; tries++)
                {
                    float a = rnd.Range(0f, 2f * Mathf.PI), r = R * 0.95f + rnd.Range(0f, 8500f);
                    var gc = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    if (!ground.Inside(gc.x, gc.y) || ground.Water(gc.x, gc.y) || ground.InTown(gc.x, gc.y, 0.9f)) continue;
                    var gb = ground.Biome(gc.x, gc.y);
                    if (gb != "coastal_plain" && gb != "lowland" && gb != "valley" || ground.ElevM(gc.x, gc.y) > 250f || ground.Slope(gc.x, gc.y, 800f) > 0.06f) continue;
                    g++;
                    float yaw = rnd.Range(0f, 90f) * Mathf.Deg2Rad, ca = Mathf.Cos(yaw), sa = Mathf.Sin(yaw);
                    // Grove sizes grow with the square root of the tree scale, so a bigger tree means fewer, not only larger, rows.
                    float grow = Mathf.Sqrt(s / KitScale.CountReference);
                    float w = rnd.Range(1300f, 3000f) * grow, d = rnd.Range(900f, 2000f) * grow, step = 2f * GroveStepM * s;   // one 2 x 2 patch per step
                    groveDiscs.Add((gc, 0.5f * Mathf.Sqrt(w * w + d * d) + CrownRadiusM(TreeKind.CitrusPatch) * s * 1.15f));   // to its edge rows' crowns
                    // A ragged block: each row stops at its own length, and some groves lose a corner (an L shape).
                    bool notch = rnd.Chance(0.3f); float nu = rnd.Range(0.1f, 0.3f) * w, nv = rnd.Range(0.1f, 0.3f) * d;
                    for (float v = -d / 2; v <= d / 2; v += step)
                    {
                        float rowEnd = w / 2 - rnd.Range(0f, 0.25f) * w;
                        for (float u = -w / 2; u <= rowEnd; u += step)
                        {
                            if (notch && u > nu && v > nv) continue;
                            if (rnd.Chance(0.05f)) continue;   // a gap: a well, a packing shed's yard
                            float x = gc.x + u * ca - v * sa, z = gc.y + u * sa + v * ca;
                            if (ground.Water(x, z) || ground.InTown(x, z, 0.85f)) continue;
                            // The patch's rows follow the grove's own angle (yaw is measured clockwise from north in Unity).
                            float patchYaw = -yaw * Mathf.Rad2Deg + (rnd.Chance(0.5f) ? 0f : 90f);
                            // All four of a patch's trees must stand on dry land outside the towns, or the spot gets one tree.
                            float hd = 0.5f * GroveStepM * s * 1.15f;   // half the patch's tree spacing, with its scale jitter
                            bool whole = true;
                            for (int k = 0; k < 4 && whole; k++)
                            {
                                float du = (k & 1) == 0 ? -hd : hd, dv = (k & 2) == 0 ? -hd : hd;
                                float tx = x + du * ca - dv * sa, tz = z + du * sa + dv * ca;
                                whole = !ground.Water(tx, tz) && !ground.InTown(tx, tz, 0.85f);
                            }
                            if (!whole || (u + step > rowEnd && rnd.Chance(0.5f))) Add(TreeKind.Citrus, x, z, ref rnd, rnd.Range(0.9f, 1.05f));   // an edge, or a row thinning out
                            else Add(TreeKind.CitrusPatch, x, z, ref rnd, 1f, patchYaw);
                        }
                    }
                }
            }

            // Olives over the hills and mountains: one jittered cell per 1.2 km, in clumps (Perlin), denser by the old towns; the
            // trees on the hills and mountains HillOliveSize times the size, HillOliveShare of them kept.
            {
                var rnd = new KitRandom("olives", 13);
                // About 2.6 crowns apart where they grow (promo 03's hills), but never more than one cell per 1.1 km: at small
                // tree scales the hills would need hundreds of thousands of trees (the WebGL budget, docs/visual-v1.md).
                float cell = Mathf.Max(1100f, 2.6f * 4.5f * s);
                for (float z = cell / 2; z < ground.Length; z += cell)
                    for (float x = cell / 2; x < ground.Width; x += cell)
                    {
                        float px = x + rnd.Range(-0.45f, 0.45f) * cell, pz = z + rnd.Range(-0.45f, 0.45f) * cell;
                        float clump = Mathf.PerlinNoise(px / 7000f + 13.1f, pz / 7000f + 7.7f);
                        float p = Mathf.Clamp01((clump - 0.30f) * 1.8f) * 0.8f;
                        if (p <= 0f && rnd.Value() > 0.03f) continue;
                        if (!rnd.Chance(Mathf.Max(p, 0.03f))) continue;
                        var b = ground.Biome(px, pz);
                        bool plain = b == "coastal_plain" || b == "lowland";
                        bool hill = b == "hills" || b == "mountains";
                        float size = hill ? HillOliveSize : 1f;
                        if (!plain && !hill && b != "valley") continue;
                        if (plain && !rnd.Chance(0.35f)) continue;   // lone trees between the fields
                        float e = ground.ElevM(px, pz);
                        if (e < (plain ? 5f : 40f) || e > 950f || ground.Water(px, pz) || ground.InTown(px, pz, 0.9f) || ground.Slope(px, pz, 600f) > 0.35f) continue;
                        int n = rnd.Range(1, 4);
                        for (int i = 0; i < n; i++)
                        {
                            float tx = px + rnd.Range(-0.5f, 0.5f) * cell, tz = pz + rnd.Range(-0.5f, 0.5f) * cell;
                            if (hill && !rnd.Chance(HillOliveShare)) continue;
                            // Off the hex-edge midpoints, as the scatter below (the grid guard, L1's G4).
                            if (ground.OnGridMark(new Vector2(tx, tz), CrownRadiusM(TreeKind.Olive) * s * size * 1.15f)) continue;
                            if (!ground.Water(tx, tz) && !ground.InTown(tx, tz, 0.85f)) Add(TreeKind.Olive, tx, tz, ref rnd, size);
                        }
                    }
            }

            // The hills' loose scatter between the olive clumps (promo 03's hills, and the demo video at 23 s): single olives
            // and low scrub over the wet hill country, where the land-use bake paints garrigue, terraces and scrub
            // (tools/land_bake.py: wild ground above 80 m, none on the steep faces the shader draws as rock). One try per
            // ScatterCellM cell, anywhere in it, kept at a share that a broad noise makes uneven (no clumps, no rows). Each
            // cell draws from its own stream, seeded by its place in the grid, so the olives above never move, a tree left out
            // for a town or a route never shifts another, and only the cells over the hill country are visited.
            {
                ulong seed = KitRandom.Fnv1a("hill-scatter");
                var hills = ground.HillCountryBounds;
                int i0 = Mathf.Max(0, Mathf.FloorToInt(hills.xMin / ScatterCellM)), i1 = Mathf.Min(Mathf.CeilToInt(ground.Width / ScatterCellM), Mathf.CeilToInt(hills.xMax / ScatterCellM));
                int j0 = Mathf.Max(0, Mathf.FloorToInt(hills.yMin / ScatterCellM)), j1 = Mathf.Min(Mathf.CeilToInt(ground.Length / ScatterCellM), Mathf.CeilToInt(hills.yMax / ScatterCellM));
                for (int j = j0; j < j1; j++)
                    for (int i = i0; i < i1; i++)
                    {
                        var rnd = new KitRandom(new KitRandom(seed ^ ((ulong)j << 32 | (uint)i)).Next());
                        float x = (i + 0.5f) * ScatterCellM, z = (j + 0.5f) * ScatterCellM;
                        float px = x + rnd.Range(-0.5f, 0.5f) * ScatterCellM, pz = z + rnd.Range(-0.5f, 0.5f) * ScatterCellM;
                        float uneven = 0.4f + 1.2f * Mathf.PerlinNoise(px / 23000f + 3.7f, pz / 23000f + 1.9f);
                        if (!rnd.Chance(ScatterShare * uneven)) continue;
                        bool scrub = rnd.Chance(ScrubShare);
                        float size = scrub ? rnd.Range(0.5f, 0.7f) : rnd.Range(0.9f, 1.2f) * HillOliveSize;
                        if (!scrub && !rnd.Chance(HillOliveShare)) continue;
                        float e = ground.ElevM(px, pz);
                        if (e < 120f || e > 950f || !ground.HillCountry(px, pz) || ground.Water(px, pz) || ground.Slope(px, pz, 600f) > 0.37f) continue;
                        var p = new Vector2(px, pz); float crown = CrownRadiusM(TreeKind.Olive) * s * size * 1.15f;
                        // No crown reaches into a town's or village's footprint (the footprint index finds a disc up to 1.7 of its
                        // radius away: a crown wider than 0.7 of a footprint's radius is still kept off its lots by Add) or a grove.
                        if (Reaches(ground.Footprints, p, crown) || Reaches(groveDiscs, p, crown) || ground.OnGridMark(p, crown)) continue;
                        Add(TreeKind.Olive, px, pz, ref rnd, size, float.NaN, scrub ? 0.7f : 1f);
                    }
                static bool Reaches(DiscIndex discs, Vector2 p, float r)
                {
                    var near = discs.Near(p);
                    for (int k = 0; k < near.Count; k++) { var (c, dr, _) = discs[near[k]]; if ((p - c).sqrMagnitude < (dr + r) * (dr + r)) return true; }
                    return false;
                }
            }

            // Palms by the shore of the coastal towns and in their streets; cypress windbreaks by villages and moshavot.
            foreach (var st in settlements)
            {
                var c3 = ground.Frame.ToWorld(st.Lon, st.Lat, 0); var c = new Vector2(c3.x, c3.z);
                var rnd = new KitRandom(st.Id, 17);
                float R = SettlementKits.FootprintRadiusM(st, ground.HexSizeM);
                var arch = SettlementKits.ArchetypeOf(st);
                bool coastal = ground.SeaDistance(c.x, c.y, 6000f) < 6000f;
                float beachKeep = 6.5f * s;   // about a palm crown's radius plus the sand strip, in metres
                if (coastal)
                {
                    int n = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(KitPopulation.Of(st.Id), 400)) * 0.9f / (ks * ks)), 4, 260);
                    for (int i = 0, tries = 0; i < n && tries < n * 10; tries++)
                    {
                        float a = rnd.Range(0f, 2f * Mathf.PI), r = (R + 2500f) * Mathf.Sqrt(rnd.Value());
                        float x = c.x + Mathf.Cos(a) * r, z = c.y + Mathf.Sin(a) * r;
                        if (ground.Water(x, z)) continue;
                        bool shore = ground.SeaDistance(x, z, 1600f) < 1600f;
                        if (!shore && !rnd.Chance(0.35f)) continue;   // mostly by the shore, some in the streets
                        i++;
                        int clump = rnd.Range(1, 4);
                        for (int k = 0; k < clump; k++)
                        {
                            float px = x + rnd.Range(-1f, 1f) * 6f * s, pz = z + rnd.Range(-1f, 1f) * 6f * s;
                            // Back from the beach: a crown never overhangs promo-sea's sand strip (review N3).
                            if (!ground.Water(px, pz) && ground.SeaDistance(px, pz, beachKeep) >= beachKeep) Add(TreeKind.Palm, px, pz, ref rnd);
                        }
                    }
                }
                if (arch == Archetype.Moshava || arch == Archetype.ArabTown)
                {
                    int rows = Mathf.Max(1, Mathf.RoundToInt((arch == Archetype.Moshava ? 4 : 3) / ks));
                    for (int row = 0; row < rows; row++)
                    {
                        float a = rnd.Range(0f, 2f * Mathf.PI), r = R * rnd.Range(0.5f, 1.15f);
                        var o = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        var dir = new Vector2(-Mathf.Sin(a), Mathf.Cos(a));
                        int n = rnd.Range(5, 11);
                        for (int k = 0; k < n; k++)
                        {
                            var p = o + dir * ((k - n / 2f) * 3.2f * s);
                            if (!ground.Water(p.x, p.y)) Add(TreeKind.Cypress, p.x, p.y, ref rnd, rnd.Range(0.9f, 1.1f));
                        }
                    }
                }
            }
            return trees;
        }
    }
}
