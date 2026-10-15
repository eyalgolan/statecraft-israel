#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Tel Aviv's White City (visual gap S2, docs/visual-v1.md "Tel Aviv's White City"): International Style
    /// blocks of four to eight storeys on the Geddes plan's lattice, with continuous balconies, ribbon windows, flat roof
    /// terraces behind a parapet, pilotis on some, and palms along the streets. Everything here goes into Tel Aviv's own
    /// kit mesh, so a model that replaces the town (#507) hides it whole.</summary>
    public static partial class SettlementKits
    {
        // ---- Tel Aviv's lattice and blocks (real metres, before the 60x city-block scale) ------------------------------

        // Owner ruling on #652 (2026-10-15): "Lower floor to ~30, wider blocks": blocks about 1.6-2 times main's width, more
        // height variety, open plots for palms. Sizes chosen by a sweep (docs/visual-v1.md, "Tel Aviv's White City"). Main drew
        // 12-16 x 9-11.5 m blocks on an 18.5 x 13.5 m pitch.

        /// <summary>Plot pitch along and across the street (real m).</summary>
        const float TelAvivPitchU = 27f, TelAvivPitchV = 19f;
        /// <summary>A block's width (along the street) and depth (real m): median 1.67 times main's.</summary>
        const float TelAvivWMin = 21f, TelAvivWMax = 24f, TelAvivDMin = 13f, TelAvivDMax = 15f;
        /// <summary>The share of plots built at the footprint's edge (the core builds every plot); the open ones take palms.</summary>
        const float TelAvivEdge = 0.9f;
        /// <summary>The storey tiers' shares: tall blocks (seven or eight storeys) from 60% in the core to 35% at the edge,
        /// low blocks (four) 36%, the rest five or six.</summary>
        const float TelAvivTallCore = 0.6f, TelAvivTallEdge = 0.35f, TelAvivLow = 0.36f;
        /// <summary>Promo 03's Tel Aviv: four to eight storeys, the tallest in the core (owner ruling 2026-10-10: where the
        /// promo departs from the era's three or four storeys, the promo wins).</summary>
        public const int TelAvivMinStoreys = 4, TelAvivMaxStoreys = 8;
        /// <summary>Share of Tel Aviv's blocks raised on pilotis: an open, recessed ground storey under the block.</summary>
        const float PilotisShare = 0.25f;

        /// <summary>A Tel Aviv block: four to eight storeys, taller near the heart.</summary>
        static Spec PickTelAviv(float t, ref KitRandom rnd, float m)
        {
            float w = rnd.Range(TelAvivWMin, TelAvivWMax), d = rnd.Range(TelAvivDMin, TelAvivDMax);
            // Three tiers, as promo 03's skyline: tall blocks of seven or eight storeys (most in the core), low blocks of four,
            // and a few of five or six between them.
            float u = rnd.Value(), tall = Mathf.Lerp(TelAvivTallCore, TelAvivTallEdge, Mathf.Clamp01(t));
            int storeys = u < tall ? (rnd.Chance(0.85f) ? 8 : 7) : u < tall + TelAvivLow ? 4 : rnd.Range(5, 7);
            return new Spec { w = w * m, d = d * m, h = storeys * 3.3f * m, storeys = storeys, lotR = 0.5f * Mathf.Sqrt(w * w + d * d) * m };
        }

        /// <summary>Draws one Tel Aviv block and records it in the census: lime-white plaster (a few cream), ribbon windows
        /// along both long sides (Ribbons), a balcony band on every upper storey (WhiteBlockDetail), some on pilotis, some stepped, and a
        /// flat roof terrace behind a parapet with its stair head.</summary>
        static void TelAvivBlock(string id, Vector2 p, float yaw, Spec b, float bottom, float top, KitMesh km, ref KitRandom rnd)
        {
            float m = KitScale.CityBlocks, storey = 3.3f * m, a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            Vector2 At(float u, float v) => p + new Vector2(u * ca - v * sa, u * sa + v * ca);
            var wall = rnd.Chance(0.08f) ? Jitter(C(0.93f, 0.90f, 0.83f), ref rnd, 0.03f) : Jitter(WhitePlaster, ref rnd, 0.025f);
            // The roof terrace's paving, a touch warmer and darker than the parapet, so the parapet reads as a rim.
            var terrace = Jitter(C(0.84f, 0.81f, 0.76f), ref rnd, 0.03f);
            bool pilotis = rnd.Chance(PilotisShare);
            bool stepped = !pilotis && rnd.Chance(0.3f);
            // The main block (c, w x d, h tall) and, on a stepped block, a lower wing at one end.
            var c = p; float w = b.w, h = b.h, du = 0f;
            if (stepped)
            {
                float share = rnd.Range(0.6f, 0.72f), side = rnd.Chance(0.5f) ? 1f : -1f;
                float w2 = b.w * (1f - share), d2 = b.d * rnd.Range(0.75f, 1f), h2 = b.h - storey * rnd.Range(1, 3);
                w = b.w * share; du = side * (b.w - w) * 0.5f; c = At(du, 0f);
                var c2 = At(-side * (b.w - w2) * 0.5f, (b.d - d2) * 0.5f);
                km.Box(c2.x, c2.y, yaw, w2 * 1.01f, d2, bottom, top, h2, wall, terrace, Facade.WhiteCity, m);
                Ribbons(km, c2, yaw, w2, d2, top, h2, m);
                RoofParapet(km, c2, yaw, w2, d2, top + h2, wall, m);
                Record(id, "wing", c2, yaw, w2, d2, h2, wall);
            }
            if (pilotis)
            {
                // The ground storey stands back under the block, in the block's shadow; the block above starts one storey up.
                var shade = C(wall.r / 255f * 0.62f, wall.g / 255f * 0.62f, wall.b / 255f * 0.64f);
                km.Box(c.x, c.y, yaw, w * 0.72f, b.d * 0.6f, bottom, top, storey, shade, shade, Facade.Plain, m, roofed: true);
                km.Box(c.x, c.y, yaw, w, b.d, top + storey - 0.3f * m, top + storey, h - storey, wall, terrace, Facade.WhiteCity, m);
                Ribbons(km, c, yaw, w, b.d, top + storey, h - storey, m);
            }
            else
            {
                km.Box(c.x, c.y, yaw, w, b.d, bottom, top, h, wall, terrace, Facade.WhiteCity, m);
                Ribbons(km, c, yaw, w, b.d, top, h, m);
            }
            WhiteBlockDetail(km, c, du, b.lotR, yaw, w, b.d, top, h, wall, terrace, m, ref rnd);
            RoofParapet(km, c, yaw, w, b.d, top + h, wall, m);
            // The stair head and the shared laundry room on the terrace.
            float rw = w * rnd.Range(0.2f, 0.3f), rd = b.d * rnd.Range(0.3f, 0.42f);
            var r1 = At(du + rnd.Range(-0.2f, 0.2f) * w, rnd.Range(-0.15f, 0.15f) * b.d);
            km.Box(r1.x, r1.y, yaw, rw, rd, top + h - 2f, top + h, 2.8f * m, wall, terrace, Facade.Plain, m);
            if (rnd.Chance(0.4f))
            {
                float lw = w * rnd.Range(0.14f, 0.2f), ld = b.d * rnd.Range(0.22f, 0.3f);
                var r2 = At(du + (rnd.Chance(0.5f) ? 1f : -1f) * (0.5f * w - 0.6f * lw), rnd.Range(-0.2f, 0.2f) * b.d);
                km.Box(r2.x, r2.y, yaw, lw, ld, top + h - 2f, top + h, 2.4f * m, wall, terrace, Facade.Plain, m);
            }
            Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
        }

        /// <summary>Glass of the ribbon windows: near black, as promo 03's deep-set windows read.</summary>
        static readonly Color32 RibbonGlass = new Color32(38, 42, 48, 0);

        /// <summary>Tel Aviv's ribbon windows (the International Style's horizontal strip window): one unbroken dark strip per
        /// storey along each long side of a w x d block whose walls rise <paramref name="h"/> from <paramref name="y0"/>, set
        /// a hair proud of the wall (no z-fight), 1.4-2.6 m above each floor, so it shows above the balcony's parapet. Geometry,
        /// not the facade shader (Kit.shader is left alone): at the gameplay zoom a 2 m window bay is under a pixel and the
        /// shader's discrete windows fade to their mean, while a strip stays a band. Two triangles per storey and side.</summary>
        static void Ribbons(KitMesh km, Vector2 c, float yaw, float w, float d, float y0, float h, float m)
        {
            float storey = 3.3f * m, a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            float hu = 0.5f * w - 0.6f * m, off = 0.06f * m;
            if (hu <= 0f) return;
            Vector3 P(float u, float v, float y) => new Vector3(c.x + u * ca - v * sa, y, c.y + u * sa + v * ca);
            int storeys = Mathf.RoundToInt(h / storey);
            for (int k = 0; k < storeys; k++)
            {
                float y1 = y0 + k * storey + 1.4f * m, y2 = y0 + k * storey + 2.6f * m;
                for (int side = -1; side <= 1; side += 2)
                {
                    float v = side * (0.5f * d + off), u0 = side * hu, u1 = -side * hu;   // clockwise seen from outside, as Box's walls
                    km.Face(P(u0, v, y1), P(u0, v, y2), P(u1, v, y2), P(u1, v, y1), RibbonGlass, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                }
            }
        }

        /// <summary>A flat roof's parapet: a low plastered rim round the terrace, inside the block's footprint.</summary>
        static void RoofParapet(KitMesh km, Vector2 c, float yaw, float w, float d, float roofY, Color32 wall, float m)
        {
            float t = 0.35f * m, hgt = 1.0f * m, a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            for (int side = -1; side <= 1; side += 2)
            {
                float v = side * 0.5f * (d - t), u = side * 0.5f * (w - t);
                km.Box(c.x - v * sa, c.y + v * ca, yaw, w, t, roofY - 0.1f * m, roofY, hgt, wall, wall, Facade.Plain, m);
                km.Box(c.x + u * ca, c.y + u * sa, yaw, t, d - 2f * t, roofY - 0.1f * m, roofY, hgt, wall, wall, Facade.Plain, m);
            }
        }

        // ---- Street palms ------------------------------------------------------------------------------------------------

        /// <summary>The most street palms Tel Aviv draws (a palm is about 110 triangles in the town's one mesh).</summary>
        const int MaxStreetPalms = 260;

        /// <summary>Palms along Tel Aviv's streets, as promo 03 shows them between the blocks: at the street crossings, the
        /// streets' middles and in the plots left open, wherever the crown clears every block (its balconies included), every
        /// other place's lots, the routes, the water and the beach strip; each as large as its spot allows, from
        /// <see cref="MinPalmScale"/> to <see cref="MaxPalmScale"/> of the city blocks' scale (a small palm in a narrow street, a
        /// tall one in an open plot). Build runs them after every place's houses and the camps, so they take only ground left free and move nothing;
        /// each claims a lot, so no tree grows through one. Seeded by
        /// its own stream: no block moves. Returns how many it drew.</summary>
        static int StreetPalms(Settlement s, Vector2 centre, float R, float yawDeg, List<(Vector2 c, float hw, float hd)> blocks, KitGround ground,
            KitMesh km, Lots lots, System.Func<Vector2, bool> mine, bool shore)
        {
            var rnd = new KitRandom(s.Id, 23);
            float m = KitScale.CityBlocks, pu = TelAvivPitchU * m, pv = TelAvivPitchV * m;
            float a = yawDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            int nu = Mathf.CeilToInt(R / pu) + 1, nv = Mathf.CeilToInt(R / pv) + 1;
            var spots = new List<(float d, int order, Vector2 uv)>();
            for (int j = -nv; j <= nv; j++)
                for (int i = -nu; i <= nu; i++)
                {
                    // The crossing (half a pitch off each way), the long street's middle, the gap between two blocks of a row,
                    // and a 3 x 3 grid over a plot left open (a garden or a square: a grove of up to nine).
                    foreach (var (u, v) in new[] { ((i + 0.5f) * pu, (j + 0.5f) * pv), (i * pu, (j + 0.5f) * pv), ((i + 0.5f) * pu, j * pv),
                                 ((i - 1f / 3f) * pu, (j - 1f / 3f) * pv), (i * pu, (j - 1f / 3f) * pv), ((i + 1f / 3f) * pu, (j - 1f / 3f) * pv),
                                 ((i - 1f / 3f) * pu, j * pv), (i * pu, j * pv), ((i + 1f / 3f) * pu, j * pv),
                                 ((i - 1f / 3f) * pu, (j + 1f / 3f) * pv), (i * pu, (j + 1f / 3f) * pv), ((i + 1f / 3f) * pu, (j + 1f / 3f) * pv) })
                    {
                        float d = Mathf.Sqrt(u * u + v * v);
                        if (d <= R) spots.Add((d, spots.Count, new Vector2(u, v)));
                    }
                }
            spots.Sort((x, y) => x.d != y.d ? x.d.CompareTo(y.d) : x.order.CompareTo(y.order));
            int drawn = 0;
            foreach (var (_, _, uv) in spots)
            {
                if (drawn >= MaxStreetPalms) break;
                // Every spot draws its numbers whether or not a palm stands there.
                float size = rnd.Range(0.85f, 1.05f), spin = rnd.Range(0f, 360f), lean = rnd.Range(-0.08f, 0.08f);
                ulong seed = rnd.Next();
                var p = centre + new Vector2(uv.x * ca - uv.y * sa, uv.x * sa + uv.y * ca);
                if (!ground.Inside(p.x, p.y) || ground.Water(p.x, p.y) || !mine(p)) continue;
                // The largest palm whose crown (but for its drooping tips) clears every block, within the scale range; 2%
                // short of touching, so float rounding never puts a crown a hair inside a wall.
                float scale = Mathf.Min(MaxPalmScale * size, 0.98f * Clearance(uv, blocks) / (PalmClear * PalmCrownM * m));
                if (scale < MinPalmScale * size) continue;
                float crown = PalmCrownM * scale * m;
                if (!lots.Free(p, crown, s.Id, 0) || ground.OnRoute(p, crown)) continue;
                if (shore && NearSea(p, yawDeg, 2f * (crown + BeachSetbackM), 2f * (crown + BeachSetbackM), ground)) continue;
                lots.Add(new Lot { c = p, r = crown, id = s.Id });
                Record(s.Id, "palm", p, 0f, 2f * crown * PalmClear, 2f * crown * PalmClear, 11.5f * scale * m, C(0.48f, 0.40f, 0.30f));
                var prnd = new KitRandom(seed);
                Palm(km, new Vector3(p.x, ground.Y(p.x, p.y), p.y), scale * m, spin, lean, ref prnd);
                drawn++;
            }
            return drawn;
        }

        /// <summary>A palm's crown radius in real metres (Vegetation's palm: CrownRadiusM).</summary>
        const float PalmCrownM = 4.6f;
        /// <summary>A street palm's size as a share of the city blocks' scale: at least 0.7 (about 42x: two and a half
        /// storeys beside a block), at most 1.6 (about 96x: as tall as a five- or six-storey block, as promo 03's palms stand); and the
        /// share of its crown that must clear every block (the drooping frond tips may reach over a balcony).</summary>
        const float MinPalmScale = 0.7f, MaxPalmScale = 1.6f, PalmClear = 0.85f;

        /// <summary>The distance from lattice point <paramref name="uv"/> to the nearest block (centre and half extents in the
        /// lattice's frame: Tel Aviv's blocks all stand square to its streets).</summary>
        static float Clearance(Vector2 uv, List<(Vector2 c, float hw, float hd)> blocks)
        {
            float best = float.MaxValue;
            foreach (var (c, hw, hd) in blocks)
            {
                float dx = Mathf.Max(0f, Mathf.Abs(uv.x - c.x) - hw), dy = Mathf.Max(0f, Mathf.Abs(uv.y - c.y) - hd);
                best = Mathf.Min(best, dx * dx + dy * dy);
            }
            return Mathf.Sqrt(best);
        }

        /// <summary>A date palm in the town's mesh, as Vegetation's (a two-part trunk, nine drooping fronds), at
        /// <paramref name="scale"/> world metres per real metre: about 110 triangles.</summary>
        static void Palm(KitMesh km, Vector3 at, float scale, float spinDeg, float lean, ref KitRandom rnd)
        {
            var trunk = C(0.48f, 0.40f, 0.30f);
            float sp = spinDeg * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(sp), 0f, Mathf.Sin(sp));
            var mid = at + (Vector3.up * 6f + dir * (0.35f + lean * 6f)) * scale;
            var top = at + (Vector3.up * 11.5f + dir * (0.9f + lean * 11.5f)) * scale;
            km.Shaft(at + Vector3.down * 0.6f * scale, mid, 0.30f * scale, 0.24f * scale, trunk, 5);
            km.Shaft(mid, top, 0.24f * scale, 0.21f * scale, trunk, 5);
            for (int i = 0; i < 7; i++)
            {
                float fa = sp + i * Mathf.PI * 2f / 7f + rnd.Range(-0.15f, 0.15f);
                km.Frond(top, new Vector3(Mathf.Cos(fa), 0, Mathf.Sin(fa)), rnd.Range(3.8f, 4.6f) * scale, 1.1f * scale, rnd.Range(0.55f, 0.85f),
                    i % 3 == 0 ? C(0.40f, 0.44f, 0.20f) : C(0.33f, 0.38f, 0.17f));
            }
        }
    }
}
