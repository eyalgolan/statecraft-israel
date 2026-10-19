#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Context tile: the Sharon plain, 1948. Long, low ridges of kurkar (calcareous sandstone) run parallel to the
    /// coast as lines of ochre rock; red hamra soil fills the troughs between them, with citrus groves and a few lone
    /// sycamore figs on it, and a farm track down the trough. Where the hex meets the sea (ctx.IsWater finds a shore), a
    /// band of pale dunes runs along it. No settlement. Clearly not citrus_orchard: the ridges and the red troughs carry
    /// the tile, and the groves are only two small blocks.
    ///
    /// Sources (checked 2026-10-19):
    /// - Kurkar ridges run in parallel lines along Israel's coast, from the Tel Aviv area northwards; the coastal dunes
    ///   are kurkar interspersed with hamra, a red sandy loam: https://en.wikipedia.org/wiki/Kurkar
    /// - The red sandy soils (hamra) of the Sharon were particularly suited to citrus; the first orchard was planted at
    ///   Hadera in 1894: https://www.britannica.com/place/Plain-of-Sharon
    /// - Sycamore figs grow in Israel mainly in the coastal plain and the Shephelah:
    ///   https://he.wikipedia.org/wiki/פיקוס_השקמה
    /// The kurkar rock and the sycamore have no model and draw tinted stand-ins (tiles:sharon_kurkar/kurkar, /sycamore).
    ///
    /// Layout: the ridges, the troughs and the track are anchored to the world with a fixed orientation (ridges along
    /// bearing 10, the coast's trend; one ridge every 0.9 S across it), not to the hex centre, so the ridge lines of two
    /// like hexes continue across their shared edge. Ridge rocks are spaced on world-anchored steps along the line, a few
    /// left out so the line reads ragged. Rocks, trees and groves keep out of ctx.InUnitSpot and off the dune band.
    ///
    /// Triangle budget by arithmetic (hard caps below; FlatSurface refuses nothing): 18 citrus x 1,600 = 28,800;
    /// 50 kurkar + 4 sycamore = 54 stand-ins x under 100 = under 5,400. Worst case under 34,200 of 38,000.</summary>
    [HexPrototype("sharon_kurkar", Kind = "nature", Region = "centre")]
    public sealed class SharonKurkar : IHexPrototype
    {
        public string Title => "Sharon plain: kurkar ridges, hamra soil, citrus (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => 38000;

        const string Citrus = "sandbox:citrus_lite", Kurkar = "tiles:sharon_kurkar/kurkar", Sycamore = "tiles:sharon_kurkar/sycamore";
        const int MaxCitrus = 18, MaxKurkar = 50, MaxSycamores = 4;
        const float RidgeBearing = 10f, RidgePitch = 0.90f, RockStep = 0.085f, Scale = HexScale.PieceScale;

        static readonly Color KurkarOchre = new Color(0.80f, 0.64f, 0.42f), CitrusGreen = new Color(0.20f, 0.38f, 0.18f),
            SycamoreGreen = new Color(0.24f, 0.36f, 0.19f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, pitch = RidgePitch * s;
            Vector3 c = ctx.Centre;
            Vector3 u = Dir(RidgeBearing), n = Dir(RidgeBearing + 90f);   // along the ridges; across them (inland)
            float cw = Vector3.Dot(Flat(c), n), cu = Vector3.Dot(Flat(c), u);
            Vector3 Point(float alongM, float acrossM) => u * alongM + n * acrossM + new Vector3(0f, c.y, 0f);

            // 1. The shore, if any: bearings where water lies inside the hex, and how far out it starts.
            var shore = new List<(float bearing, float r)>();
            for (float b = 0f; b < 360f; b += 10f)
                for (float r = 0.30f; r <= 0.98f; r += 0.03f)
                    if (ctx.IsWater(ctx.At(b, r))) { shore.Add((b, r)); break; }
            Vector3 seaDir = Vector3.zero;
            foreach (var sh in shore) seaDir += Dir(sh.bearing);
            bool coastal = shore.Count > 0 && seaDir.sqrMagnitude > 1e-6f;
            if (coastal) seaDir.Normalize();
            bool InDunes(Vector3 p) => coastal && (ctx.IsWater(p + seaDir * (0.12f * s)) || ctx.IsWater(p + seaDir * (0.24f * s)));

            // 2. The ridge lines and the troughs between them, on the world lattice across the ridges.
            int m0 = Mathf.FloorToInt((cw - 1.1f * s) / pitch), m1 = Mathf.CeilToInt((cw + 1.1f * s) / pitch);
            float reach = 1.1f * s;
            var ridges = new List<float>();
            for (int m = m0; m <= m1; m++)
            {
                float w = m * pitch, wt = w + 0.5f * pitch;
                ridges.Add(w);
                // Hamra in the trough: ploughed red soil at partial strength, then the ridge band of sandy rock.
                ctx.PaintGround("ploughed", Point(cu, wt), RidgeBearing, 0.62f * s, 2.4f * s, 0.35f, 0.5f);
                ctx.PaintStroke("desert", Point(cu - reach, w), Point(cu + reach, w), 0.13f * s, 0.6f);
                ctx.PaintStroke("hills", Point(cu - reach, w), Point(cu + reach, w), 0.06f * s, 0.75f);
                // The farm track down the middle of the trough.
                ctx.PaintStroke("dirt", Point(cu - reach, wt + 0.12f * s), Point(cu + reach, wt + 0.12f * s), 0.025f * s, 0.85f);
            }
            float DistToRidge(Vector3 p)
            {
                float w = Vector3.Dot(Flat(p), n), best = float.MaxValue;
                foreach (float r in ridges) best = Mathf.Min(best, Mathf.Abs(w - r));
                return best;
            }
            float DistToTrack(Vector3 p)
            {
                float w = Vector3.Dot(Flat(p), n), best = float.MaxValue;
                foreach (float r in ridges) best = Mathf.Min(best, Mathf.Abs(w - (r + 0.5f * pitch + 0.12f * s)));
                return best;
            }

            // 3. Ridge rocks: world-anchored steps along each line, about one in four left out, low and ochre.
            int kurkar = 0;
            foreach (float w in ridges)
            {
                int k0 = Mathf.FloorToInt((cu - reach) / (RockStep * s)), k1 = Mathf.CeilToInt((cu + reach) / (RockStep * s));
                for (int k = k0; k <= k1 && kurkar < MaxKurkar; k++)
                {
                    var at = Point(k * RockStep * s, w + ctx.Range(-0.015f, 0.015f) * s);
                    float width = ctx.Range(0.055f, 0.08f) * s, yaw = RidgeBearing + ctx.Range(-20f, 20f);
                    if (ctx.Rng.Next(4) == 0) continue;
                    if (!ctx.Inside(at, 0.02f * s) || ctx.InUnitSpot(at, 0.71f * Scale * width) || InDunes(at)) continue;
                    if (ctx.PlaceByWidth(Kurkar, at, yaw, width, StandIn.Rock, KurkarOchre)) kurkar++;
                }
            }

            // 4. White dunes along the shore: two soft patches of beach sand inland of each water bearing.
            foreach (var sh in shore)
            {
                ctx.PaintGround("beach", ctx.At(sh.bearing, sh.r - 0.04f), sh.bearing, 0.16f * s, 0.10f * s, 0.35f, 0.95f);
                if (sh.r - 0.15f > 0.05f)
                    ctx.PaintGround("beach", ctx.At(sh.bearing, sh.r - 0.15f), sh.bearing, 0.18f * s, 0.14f * s, 0.45f, 0.8f);
            }

            // 5. Citrus groves: up to two 3 x 3 blocks in the troughs, along the trough either side of the hex middle.
            var groves = new List<Vector3>();
            int citrus = 0;
            foreach (float w in ridges)
                foreach (float du in new[] { -0.50f, 0.50f })
                {
                    if (citrus >= MaxCitrus) break;
                    var g = Point(cu + du * s, w + 0.5f * pitch - 0.10f * s);   // clear of the ridge and the track by the grove's half-diagonal
                    if (!ctx.Inside(g, 0.14f * s) || ctx.InUnitSpot(g, 0.19f * s) || InDunes(g)) continue;
                    groves.Add(g);
                    ctx.PaintGround("green_crop", g, RidgeBearing, 0.30f * s, 0.30f * s, 0.25f, 0.55f);
                    for (int a = -1; a <= 1; a++)
                        for (int b = -1; b <= 1; b++)
                        {
                            if (citrus >= MaxCitrus) break;
                            var at = g + u * (a * 0.10f * s) + n * (b * 0.10f * s);
                            if (ctx.PlaceByHeight(Citrus, at, ctx.Range(0f, 360f), ctx.Range(0.11f, 0.12f) * s, StandIn.Tree, CitrusGreen, 0.5f)) citrus++;
                        }
                }

            // 6. Lone sycamore figs in the troughs: off the ridges, the groves, the dunes and the unit spot.
            var lone = new List<Vector3>();
            for (int i = 0; i < 40 && lone.Count < MaxSycamores; i++)
            {
                var at = ctx.RandomPoint(0.08f * s);
                if (ctx.InUnitSpot(at, 0.08f * s) || DistToRidge(at) < 0.16f * s || DistToTrack(at) < 0.09f * s || InDunes(at)) continue;
                bool clear = true;
                foreach (var g in groves) if (Flat(at - g).magnitude < 0.30f * s) clear = false;
                foreach (var t in lone) if (Flat(at - t).magnitude < 0.25f * s) clear = false;
                if (!clear) continue;
                if (ctx.PlaceByHeight(Sycamore, at, ctx.Range(0f, 360f), ctx.Range(0.15f, 0.17f) * s, StandIn.Tree, SycamoreGreen, 0.65f)) lone.Add(at);
            }
        }

        static Vector3 Dir(float bearingDeg)
        {
            float a = Mathf.Deg2Rad * bearingDeg;
            return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
