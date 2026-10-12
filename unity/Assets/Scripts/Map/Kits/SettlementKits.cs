#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The four procedural archetypes of the settlement art spike (issues #31, #32).</summary>
    public enum Archetype { WhiteCity, OldTown, ArabTown, Moshava }

    /// <summary>Procedural settlement kits at the owner's symbolic scale (ruling 2026-10-11): buildings about
    /// <see cref="KitScale"/> times their real size (per class), a town's footprint spread over neighbouring hexes in
    /// proportion to its population, geography real (towns stay on land, at their sourced coordinates). Built at load time
    /// from the settlement id's seed (<see cref="KitRandom"/>), so every load of a build draws the same towns; nothing here reads or
    /// writes the sim.</summary>
    public static class SettlementKits
    {
        /// <summary>Settled population per hex of footprint: a town of this many people covers one hex (about 65 km^2).</summary>
        public const float PeoplePerHex = 60000f;
        /// <summary>The smallest footprint, in hexes, so a kibbutz of 240 still reads as a place at the gameplay zoom.</summary>
        public const float MinHexes = 0.08f;

        public static Archetype ArchetypeOf(Settlement s)
        {
            switch (s.Id)
            {
                case "tel_aviv": case "haifa": return Archetype.WhiteCity;
                case "jaffa": case "acre": case "gaza": case "jerusalem": return Archetype.OldTown;
            }
            switch (s.Type)
            {
                case "city": return Archetype.WhiteCity;
                case "arab_town": case "village": return Archetype.ArabTown;
                case "town": return s.Id == "tiberias" || s.Id == "safed" ? Archetype.ArabTown : Archetype.Moshava;
                default: return Archetype.Moshava;   // kibbutz, moshav
            }
        }

        /// <summary>Footprint radius (m) of a settlement: the radius of a disc of (population / PeoplePerHex) hexes.</summary>
        public static float FootprintRadiusM(Settlement s, float hexSizeM)
        {
            float hexArea = 1.5f * 1.7320508f * hexSizeM * hexSizeM;   // a pointy hex of circumradius hexSizeM
            float hexes = Mathf.Max(MinHexes, KitPopulation.Of(s.Id) / PeoplePerHex);
            return Mathf.Sqrt(hexes * hexArea / Mathf.PI);
        }

        /// <summary>Claims every settlement's footprint on <paramref name="ground"/> (largest first, so a big town's disc is
        /// whole and its small neighbour fits around it) and returns one mesh per settlement, keyed by id.</summary>
        public static List<(Settlement s, Mesh mesh, int buildings)> Build(IReadOnlyList<Settlement> list, KitGround ground) =>
            Build(list, ground, out _);

        /// <summary>As Build, also returning every claimed lot (centre, radius, owning settlement id): buildings and landmarks
        /// of all towns, none overlapping another (tests); and, when <paramref name="columns"/> is given, filling it with each
        /// non-empty kit's columns by settlement id (KitMesh.Columns, read before the mesh's CPU copy is dropped).</summary>
        public static List<(Settlement s, Mesh mesh, int buildings)> Build(IReadOnlyList<Settlement> list, KitGround ground,
            out List<(Vector2 centre, float radius, string id)> claimed, Dictionary<string, Bounds[]>? columns = null)
        {
            var order = new List<Settlement>(list);
            order.Sort((a, b) => KitPopulation.Of(b.Id).CompareTo(KitPopulation.Of(a.Id)) != 0
                ? KitPopulation.Of(b.Id).CompareTo(KitPopulation.Of(a.Id)) : string.CompareOrdinal(a.Id, b.Id));
            var centres = new Dictionary<string, Vector2>();
            foreach (var s in order)
            {
                var w = ground.Frame.ToWorld(s.Lon, s.Lat, 0);
                centres[s.Id] = new Vector2(w.x, w.z);
                ground.Footprints.Add((centres[s.Id], FootprintRadiusM(s, ground.HexSizeM), s.Id));
            }
            var result = new List<(Settlement, Mesh, int)>();
            // One lot list for every town: a big town's buildings are placed first, and a neighbour's never overlap them.
            var lots = new List<Lot>();
            foreach (var s in order)
            {
                var km = new KitMesh();
                int n = BuildOne(s, centres[s.Id], ground, km, lots);
                if (columns != null && km.VertexCount > 0) columns[s.Id] = km.Columns();
                result.Add((s, km.ToMesh(s.Id + "_kit"), n));
            }
            claimed = lots.ConvertAll(l => (centre: l.c, radius: l.r, id: l.id));
            return result;
        }

        /// <summary>Tints the ground of each settlement's footprint in the baked biome colour texels (sRGB, row 0 = south):
        /// pale paving and sand in the towns, as in promo 03, fading out over the outer half of the footprint.</summary>
        public static void PaintGround(Color32[] px, int res, GeoFrame frame, IReadOnlyList<Settlement> list, float hexSizeM)
        {
            foreach (var s in list)
            {
                var arch = ArchetypeOf(s);
                var (sand, strength) = arch switch
                {
                    Archetype.WhiteCity => (new Color(0.80f, 0.75f, 0.64f), 0.62f),
                    Archetype.OldTown => (new Color(0.78f, 0.68f, 0.52f), 0.62f),
                    Archetype.ArabTown => (new Color(0.74f, 0.64f, 0.48f), 0.5f),
                    _ => (new Color(0.70f, 0.63f, 0.47f), 0.32f),
                };
                var c = frame.ToWorld(s.Lon, s.Lat, 0);
                float R = FootprintRadiusM(s, hexSizeM) * 1.1f;
                int x0 = Mathf.Max(0, Mathf.FloorToInt((c.x - R) / frame.WidthM * res)), x1 = Mathf.Min(res - 1, Mathf.CeilToInt((c.x + R) / frame.WidthM * res));
                int y0 = Mathf.Max(0, Mathf.FloorToInt((c.z - R) / frame.LengthM * res)), y1 = Mathf.Min(res - 1, Mathf.CeilToInt((c.z + R) / frame.LengthM * res));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float wx = (x + 0.5f) / res * frame.WidthM - c.x, wz = (y + 0.5f) / res * frame.LengthM - c.z;
                        float d = Mathf.Sqrt(wx * wx + wz * wz) / R;
                        if (d >= 1f) continue;
                        float w = strength * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, d)));
                        int i = y * res + x;
                        px[i] = Color32.Lerp(px[i], sand, w);
                    }
            }
        }

        sealed class Lot { public Vector2 c; public float r; public string id = ""; }

        static bool FreeAt(List<Lot> lots, Vector2 p, float r)
        {
            foreach (var l in lots) if ((l.c - p).sqrMagnitude < (l.r + r) * (l.r + r)) return false;
            return true;
        }

        /// <summary>A landmark's spot (review N2): the dry spot nearest <paramref name="want"/> (rings out to four radii) that no
        /// lot of any town already holds; claimed for <paramref name="owner"/>. False, and nothing claimed, when there is none:
        /// the landmark is then left out rather than overlapping a bigger neighbour.</summary>
        static bool Claim(List<Lot> lots, Vector2 want, float r, string owner, KitGround ground, out Vector2 at)
        {
            for (int ring = 0; ring <= 8; ring++)
            {
                int around = ring == 0 ? 1 : 8 * ring;
                for (int k = 0; k < around; k++)
                {
                    float a = 2f * Mathf.PI * k / around, d = ring * r * 0.5f;
                    var p = want + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                    if (!ground.Inside(p.x, p.y) || ground.Water(p.x, p.y) || !FreeAt(lots, p, r) || ground.OnRoute(p, r)) continue;
                    lots.Add(new Lot { c = p, r = r, id = owner }); at = p; return true;
                }
            }
            at = want; return false;
        }

        /// <summary>Places one settlement's buildings into <paramref name="km"/>; returns how many.</summary>
        static int BuildOne(Settlement s, Vector2 centre, KitGround ground, KitMesh km, List<Lot> lots)
        {
            var arch = ArchetypeOf(s);
            var rnd = new KitRandom(s.Id, 1);
            float R = FootprintRadiusM(s, ground.HexSizeM);
            float m = KitScale.For(arch);
            int placed = 0;

            // This settlement's cell: a point belongs to the settlement whose footprint it is deepest inside (distance over
            // radius), so Jaffa and Tel Aviv, or Lod and Ramla, meet along a seam instead of overlapping.
            bool Mine(Vector2 p)
            {
                float mine = (p - centre).magnitude / R;
                foreach (var f in ground.Footprints)
                    if (f.id != s.Id && (p - f.centre).magnitude / f.radius < mine) return false;
                return true;
            }
            bool Free(Vector2 p, float r) => FreeAt(lots, p, r);

            // Landmarks first, at the centre (or the nearest dry spot to it).
            var heart = DryNear(centre, ground, R * 0.3f, ref rnd);
            switch (arch)
            {
                case Archetype.OldTown: placed += OldTownLandmarks(s, heart, ground, km, lots, ref rnd); break;
                case Archetype.ArabTown: placed += Mosque(s.Id, heart, ground, km, lots, ref rnd, 1f); break;
                case Archetype.Moshava: placed += WaterTower(s.Id, heart + new Vector2(R * 0.35f, -R * 0.2f), ground, km, lots); break;
            }

            int target = TargetCount(arch, R);
            int attempts = target * 30;
            bool ring = s.Type == "moshav";   // Nahalal's ring of farmsteads round the public buildings
            for (int i = 0; i < attempts && placed < target; i++)
            {
                // The disc grows slowly when the sea or a neighbour eats into it, so the count stays tied to the population.
                float reach = R * (1f + 0.6f * i / attempts);
                float u = rnd.Value(), a = rnd.Range(0f, 2f * Mathf.PI);
                float rr = ring ? reach * rnd.Range(0.55f, 0.75f) : reach * Mathf.Pow(u, arch == Archetype.Moshava ? 0.5f : 0.62f);
                var p = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr;
                if (!ground.Inside(p.x, p.y) || ground.Water(p.x, p.y) || !Mine(p)) continue;
                float t = rr / R;   // 0 at the heart, 1 at the edge
                var b = Pick(arch, t, ref rnd, m);
                if (!Free(p, b.lotR) || ground.OnRoute(p, b.lotR)) continue;   // off the railways and roads too
                if (ground.Slope(p.x, p.y, b.w) > (arch == Archetype.ArabTown || arch == Archetype.OldTown ? 0.6f : 0.35f)) continue;
                // Corner and seaward checks: the whole footprint is dry.
                if (ground.Water(p.x + b.w * 0.5f, p.y) || ground.Water(p.x - b.w * 0.5f, p.y) || ground.Water(p.x, p.y + b.d * 0.5f) || ground.Water(p.x, p.y - b.d * 0.5f)) continue;
                float yaw = arch == Archetype.WhiteCity || arch == Archetype.Moshava
                    ? GridYaw(s.Id) + (rnd.Chance(0.15f) ? 90f : 0f) + rnd.Range(-4f, 4f)
                    : rnd.Range(0f, 90f);
                lots.Add(new Lot { c = p, r = b.lotR, id = s.Id });
                Emit(arch, p, yaw, b, ground, km, ref rnd);
                placed++;
            }
            return placed;
        }

        /// <summary>Buildings per settlement: the footprint area times the archetype's built-up share over its mean lot.</summary>
        static int TargetCount(Archetype arch, float R)
        {
            float area = Mathf.PI * R * R, m = KitScale.For(arch);
            float lot = arch switch
            {
                Archetype.WhiteCity => 21f * 15f, Archetype.OldTown => 11f * 10f, Archetype.ArabTown => 10f * 9f, _ => 10f * 8f,
            } * m * m;
            float share = arch switch { Archetype.WhiteCity => 0.36f, Archetype.OldTown => 0.46f, Archetype.ArabTown => 0.28f, _ => 0.09f };
            return Mathf.Clamp(Mathf.RoundToInt(area * share / lot), 6, 900);
        }

        /// <summary>The street grid's angle per town (Tel Aviv's Geddes plan runs about 20 degrees off north, parallel to the
        /// shore; the others get a seeded angle).</summary>
        static float GridYaw(string id) => id == "tel_aviv" ? 20f : id == "haifa" ? 35f : new KitRandom(id, 7).Range(0f, 90f);

        struct Spec { public float w, d, h, lotR; public int storeys; }

        static Spec Pick(Archetype arch, float t, ref KitRandom rnd, float m)
        {
            float w, d; int storeys;
            switch (arch)
            {
                case Archetype.WhiteCity:
                    w = rnd.Range(15f, 26f); d = rnd.Range(11f, 18f);
                    // Taller near the centre; a few landmarks taller still (the promo's skyline, owner ruling: promo wins).
                    storeys = Mathf.RoundToInt(Mathf.Lerp(5.2f, 2.2f, t) + rnd.Range(-1.2f, 1.2f));
                    if (t < 0.45f && rnd.Chance(0.07f)) storeys = rnd.Range(7, 10);
                    storeys = Mathf.Clamp(storeys, 2, 9);
                    break;
                case Archetype.OldTown:
                    w = rnd.Range(7f, 14f); d = rnd.Range(7f, 12f);
                    storeys = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(3.2f, 1.4f, t) + rnd.Range(-0.8f, 0.8f)), 1, 4);
                    break;
                case Archetype.ArabTown:
                    w = rnd.Range(6.5f, 12f); d = rnd.Range(6f, 10f);
                    storeys = rnd.Chance(Mathf.Lerp(0.45f, 0.1f, t)) ? 2 : 1;
                    break;
                default:
                    w = rnd.Range(8f, 12f); d = rnd.Range(6.5f, 9f);
                    storeys = rnd.Chance(0.15f) ? 2 : 1;
                    if (rnd.Chance(0.06f)) { w = rnd.Range(18f, 28f); d = rnd.Range(8f, 10f); }   // a barn, a dining hall, a packing house
                    break;
            }
            float storeyM = 3.3f;
            float gap = arch switch { Archetype.WhiteCity => 1.25f, Archetype.OldTown => 0.92f, Archetype.ArabTown => 1.05f, _ => 2.4f };
            return new Spec
            {
                w = w * m, d = d * m, h = storeys * storeyM * m, storeys = storeys,
                lotR = 0.5f * Mathf.Sqrt(w * w + d * d) * m * gap * (arch == Archetype.OldTown ? 0.8f : 0.72f),
            };
        }

        // sRGB palettes (promo 03: white and cream city, honey stone Jaffa, tan villages, terracotta roofs).
        static Color32 C(float r, float g, float b) => new Color32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), 255);
        static Color32 Jitter(Color32 c, ref KitRandom rnd, float amount)
        {
            float k = 1f + rnd.Range(-amount, amount), warm = rnd.Range(-amount, amount) * 0.5f;
            return C(Mathf.Clamp01(c.r / 255f * k * (1 + warm)), Mathf.Clamp01(c.g / 255f * k), Mathf.Clamp01(c.b / 255f * k * (1 - warm)));
        }

        static void Emit(Archetype arch, Vector2 p, float yaw, Spec b, KitGround ground, KitMesh km, ref KitRandom rnd)
        {
            float m = KitScale.For(arch);
            var (bottom, top) = GroundSpan(p, yaw, b.w, b.d, ground, m);
            switch (arch)
            {
                case Archetype.WhiteCity:
                {
                    var wall = rnd.Chance(0.12f) ? Jitter(C(0.90f, 0.84f, 0.72f), ref rnd, 0.04f) : Jitter(C(0.94f, 0.92f, 0.87f), ref rnd, 0.035f);
                    var roof = Jitter(C(0.80f, 0.78f, 0.74f), ref rnd, 0.05f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.WhiteCity, m);
                    // Some blocks are L-shaped: a lower wing at one end, back from the street.
                    if (rnd.Chance(0.3f))
                    {
                        float a = yaw * Mathf.Deg2Rad, side = rnd.Chance(0.5f) ? 1f : -1f;
                        float ww = b.w * 0.42f, wd = b.d * 0.8f;
                        float u = side * (b.w - ww) * 0.5f, v = (b.d + wd) * 0.5f * 0.92f;
                        var wc = new Vector2(p.x + u * Mathf.Cos(a) - v * Mathf.Sin(a), p.y + u * Mathf.Sin(a) + v * Mathf.Cos(a));
                        if (!ground.Water(wc.x, wc.y))
                            km.Box(wc.x, wc.y, yaw, ww, wd, bottom, top, Mathf.Max(2f * 3.3f * m, b.h - 3.3f * m), wall, roof, Facade.WhiteCity, m);
                    }
                    // A stair tower or roof room on many Bauhaus blocks, and a parapet's shadow line.
                    if (rnd.Chance(0.55f))
                    {
                        float rw = b.w * rnd.Range(0.25f, 0.4f), rd = b.d * rnd.Range(0.3f, 0.45f);
                        km.Box(p.x + b.w * rnd.Range(-0.2f, 0.2f), p.y + b.d * rnd.Range(-0.15f, 0.15f), yaw, rw, rd, top + b.h - 2f, top + b.h, 2.8f * m, wall, roof, Facade.Plain, m);
                    }
                    break;
                }
                case Archetype.OldTown:
                {
                    var wall = Jitter(C(0.83f, 0.72f, 0.54f), ref rnd, 0.06f);
                    var roof = Jitter(C(0.86f, 0.77f, 0.60f), ref rnd, 0.05f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Stone, m);
                    if (rnd.Chance(0.22f)) km.Dome(p.x, p.y, top + b.h - 1f, Mathf.Min(b.w, b.d) * 0.33f, Jitter(C(0.88f, 0.84f, 0.76f), ref rnd, 0.05f));
                    else if (rnd.Chance(0.03f)) km.Pyramid(p.x, p.y, yaw, b.w * 1.04f, b.d * 1.04f, top + b.h, 2.2f * m, Jitter(C(0.70f, 0.33f, 0.22f), ref rnd, 0.06f));
                    break;
                }
                case Archetype.ArabTown:
                {
                    var wall = Jitter(C(0.80f, 0.70f, 0.53f), ref rnd, 0.07f);
                    var roof = Jitter(C(0.84f, 0.75f, 0.59f), ref rnd, 0.06f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Village, m);
                    if (rnd.Chance(0.10f)) km.Dome(p.x, p.y, top + b.h - 1f, Mathf.Min(b.w, b.d) * 0.3f, roof);
                    break;
                }
                default:
                {
                    var wall = Jitter(C(0.92f, 0.89f, 0.82f), ref rnd, 0.04f);
                    var roof = Jitter(C(0.74f, 0.31f, 0.20f), ref rnd, 0.08f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Village, m, roofed: true);
                    km.Gable(p.x, p.y, yaw, b.w, b.d, top + b.h, Mathf.Min(b.w, b.d) * 0.42f, roof, wall);
                    break;
                }
            }
        }

        /// <summary>Wall bottom (sunk 2 m below the lowest corner, so a house on a slope stands on a plinth, not on air) and the
        /// ground level the walls rise from (the highest corner, so the slope never buries a storey).</summary>
        static (float bottom, float ground) GroundSpan(Vector2 p, float yawDeg, float w, float d, KitGround ground, float m)
        {
            float a = yawDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < 5; i++)
            {
                float u = i == 4 ? 0 : ((i & 1) == 0 ? -0.5f : 0.5f) * w, v = i == 4 ? 0 : ((i & 2) == 0 ? -0.5f : 0.5f) * d;
                float y = ground.Y(p.x + u * ca - v * sa, p.y + u * sa + v * ca);
                lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y);
            }
            return (lo - 2f * m, hi);
        }

        static Vector2 DryNear(Vector2 c, KitGround ground, float within, ref KitRandom rnd)
        {
            if (ground.Inside(c.x, c.y) && !ground.Water(c.x, c.y)) return c;
            for (int i = 0; i < 64; i++)
            {
                float a = rnd.Range(0f, 2f * Mathf.PI), r = within * (i + 1) / 64f;
                var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (ground.Inside(p.x, p.y) && !ground.Water(p.x, p.y)) return p;
            }
            return c;
        }

        /// <summary>Jaffa: the clock tower (1903-06) by the old town's gate, the Mahmoudiya mosque's minaret and dome, and a
        /// church belfry above the harbour; the other old towns get a mosque.</summary>
        static int OldTownLandmarks(Settlement s, Vector2 heart, KitGround ground, KitMesh km, List<Lot> lots, ref KitRandom rnd)
        {
            float m = KitScale.Landmarks;
            int n = Mosque(s.Id, heart + new Vector2(-5f * m, 6f * m), ground, km, lots, ref rnd, 1.2f);
            if (s.Id == "jaffa")
            {
                var stone = C(0.86f, 0.76f, 0.57f);
                if (!Claim(lots, heart + new Vector2(14f * m, 4f * m), 4.5f * m, s.Id, ground, out var p)) return n;
                float y = ground.Y(p.x, p.y);
                km.Box(p.x, p.y, 15f, 5f * m, 5f * m, y - 2f * m, y, 24f * m, stone, stone, Facade.Stone, m, roofed: true);
                km.Box(p.x, p.y, 15f, 4f * m, 4f * m, y + 24f * m, y + 24f * m, 4f * m, C(0.90f, 0.82f, 0.64f), stone, Facade.Plain, m, roofed: true);
                km.Pyramid(p.x, p.y, 15f, 4.4f * m, 4.4f * m, y + 28f * m, 3.5f * m, C(0.55f, 0.47f, 0.40f));
                n++;
                if (!Claim(lots, heart + new Vector2(-10f * m, -12f * m), 10f * m, s.Id, ground, out var q)) return n;
                float yq = ground.Y(q.x, q.y);
                km.Box(q.x, q.y, 0f, 9f * m, 18f * m, yq - 2f * m, yq, 12f * m, stone, stone, Facade.Stone, m, roofed: true);
                km.Box(q.x, q.y + 7f * m, 0f, 4.5f * m, 4.5f * m, yq + 12f * m, yq + 12f * m, 14f * m, stone, stone, Facade.Stone, m, roofed: true);
                km.Pyramid(q.x, q.y + 7f * m, 0f, 4.8f * m, 4.8f * m, yq + 26f * m, 6f * m, C(0.60f, 0.52f, 0.44f));
                km.Gable(q.x, q.y, 0f, 9f * m, 18f * m, yq + 12f * m, 4f * m, C(0.66f, 0.34f, 0.24f), stone);
                n += 1 + Port(heart, ground, km, ref rnd);
            }
            return n;
        }

        /// <summary>Jaffa's harbour, as promo 03 draws it: a stone breakwater curving out to sea from the shore west of the
        /// old town, a lighthouse at its head, and a few sailing boats inside it.</summary>
        static int Port(Vector2 heart, KitGround ground, KitMesh km, ref KitRandom rnd)
        {
            float m = KitScale.Landmarks;
            var shore = heart;
            for (int i = 0; i < 120 && !ground.Water(shore.x, shore.y); i++) shore.x -= 50f;
            if (!ground.Water(shore.x, shore.y)) return 0;
            float sea = ground.Y(shore.x, shore.y);
            var stone = C(0.74f, 0.70f, 0.62f);
            // A quarter-circle arm: out to the west, then north.
            var c = shore + new Vector2(0f, 60f * m);
            float r = 60f * m;
            Vector2 prev = c + new Vector2(0f, -r);
            for (int k = 1; k <= 9; k++)
            {
                float a = -Mathf.PI * 0.5f - k * (Mathf.PI * 0.5f / 9f);
                var cur = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                var mid = (prev + cur) * 0.5f; var dir = cur - prev;
                float yaw = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                km.Box(mid.x, mid.y, yaw, dir.magnitude * 1.08f, 5f * m, sea - 2f * m, sea, 2.2f * m, stone, stone, Facade.Plain, m);
                prev = cur;
            }
            // The lighthouse at the arm's head.
            var white = C(0.93f, 0.92f, 0.88f);
            km.Shaft(new Vector3(prev.x, sea, prev.y), new Vector3(prev.x, sea + 16f * m, prev.y), 2.0f * m, 1.5f * m, white, 10);
            km.Shaft(new Vector3(prev.x, sea + 16f * m, prev.y), new Vector3(prev.x, sea + 19f * m, prev.y), 1.6f * m, 1.6f * m, C(0.30f, 0.32f, 0.30f), 10);
            km.Cone(new Vector3(prev.x, sea + 19f * m, prev.y), 1.9f * m, 2.4f * m, C(0.68f, 0.24f, 0.18f), 10);
            // Sailing boats in and off the harbour.
            int boats = 0;
            for (int i = 0; i < 12 && boats < 5; i++)
            {
                var b = c + new Vector2(rnd.Range(-1.6f, 0.2f) * r, rnd.Range(-1.4f, 0.4f) * r);
                if (!ground.Water(b.x, b.y) || ground.Water(b.x + 40f * m, b.y) == false) continue;
                float yaw = rnd.Range(0f, 360f), L = rnd.Range(9f, 14f) * m;
                km.Box(b.x, b.y, yaw, L, L * 0.32f, sea - 1f * m, sea, 1.6f * m, C(0.42f, 0.30f, 0.20f), C(0.55f, 0.42f, 0.30f), Facade.Plain, m);
                float a = yaw * Mathf.Deg2Rad; var fwd = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var mast = new Vector3(b.x, sea + 1.6f * m, b.y);
                km.Shaft(mast, mast + Vector3.up * L * 0.9f, 0.18f * m, 0.12f * m, C(0.35f, 0.28f, 0.20f), 4);
                var sail = C(0.95f, 0.93f, 0.86f);
                var s0 = mast + Vector3.up * L * 0.85f; var s1 = mast + Vector3.up * 0.4f * m; var s2 = mast + fwd * (-L * 0.45f) + Vector3.up * 0.4f * m;
                km.Triangle(s0, s1, s2, sail); km.Triangle(s0, s2, s1, sail);
                boats++;
            }
            return 1;
        }

        static int Mosque(string owner, Vector2 want, KitGround ground, KitMesh km, List<Lot> lots, ref KitRandom rnd, float size)
        {
            float m = KitScale.Landmarks * size;
            if (!Claim(lots, want, 11f * m, owner, ground, out var p)) return 0;   // the lot covers the minaret too
            float y = ground.Y(p.x, p.y);
            var stone = Jitter(C(0.87f, 0.80f, 0.66f), ref rnd, 0.03f);
            km.Box(p.x, p.y, 0f, 14f * m, 14f * m, y - 2f * m, y, 7f * m, stone, stone, Facade.Stone, KitScale.Landmarks);
            km.Dome(p.x, p.y, y + 7f * m - 0.5f * m, 5.5f * m, C(0.80f, 0.80f, 0.76f));
            float mx = p.x + 8.5f * m, mz = p.y - 6f * m;
            float my = ground.Y(mx, mz);
            km.Shaft(new Vector3(mx, my - 2f * m, mz), new Vector3(mx, my + 26f * m, mz), 1.6f * m, 1.35f * m, stone, 8);
            km.Shaft(new Vector3(mx, my + 20f * m, mz), new Vector3(mx, my + 21f * m, mz), 2.1f * m, 2.1f * m, stone, 8);   // the balcony
            km.Cone(new Vector3(mx, my + 26f * m, mz), 1.5f * m, 4.5f * m, C(0.62f, 0.62f, 0.58f), 8);
            return 1;
        }

        static int WaterTower(string owner, Vector2 want, KitGround ground, KitMesh km, List<Lot> lots)
        {
            float m = KitScale.Houses;
            if (!Claim(lots, want, 4f * m, owner, ground, out var p)) return 0;
            float y = ground.Y(p.x, p.y);
            var concrete = C(0.82f, 0.80f, 0.76f);
            for (int i = 0; i < 4; i++)
            {
                float a = Mathf.PI * 0.5f * i + 0.6f;
                var b = new Vector3(p.x + Mathf.Cos(a) * 2.6f * m, y - 2f * m, p.y + Mathf.Sin(a) * 2.6f * m);
                km.Shaft(b, new Vector3(p.x + Mathf.Cos(a) * 1.8f * m, y + 9f * m, p.y + Mathf.Sin(a) * 1.8f * m), 0.35f * m, 0.3f * m, concrete, 4);
            }
            km.Shaft(new Vector3(p.x, y + 9f * m, p.y), new Vector3(p.x, y + 13.5f * m, p.y), 3f * m, 3f * m, concrete, 10);
            km.Cone(new Vector3(p.x, y + 13.5f * m, p.y), 3.1f * m, 1.2f * m, C(0.66f, 0.64f, 0.60f), 10);
            return 1;
        }
    }

    /// <summary>The symbolic scale per class (owner ruling 2026-10-11, superseding the 10-25x ruling: match promo 03's
    /// measured scale per class): city blocks about 60x their real size, houses and trees about 100x; geography stays real.
    /// Landmarks (a clock tower, a mosque, a harbour) take the city-block scale. Fields, not constants, so the capture tool
    /// and the player's ?kitscale / ?housescale / ?treescale flags (clamped to <see cref="Min"/>..<see cref="Max"/>) can show
    /// another scale. Counts are tuned at <see cref="CountReference"/> and fall with the square of the scale.</summary>
    public static class KitScale
    {
        public const float DefaultCityBlocks = 60f, DefaultHouses = 100f, DefaultTrees = 100f;
        public const float Min = 5f, Max = 200f;
        /// <summary>The scale the building and tree counts were tuned at: a class drawn at k times this has 1/k^2 as many.</summary>
        public const float CountReference = 25f;
        public static float CityBlocks = DefaultCityBlocks;
        public static float Houses = DefaultHouses;
        public static float Trees = DefaultTrees;

        public static float Landmarks => CityBlocks;
        public static float For(Archetype a) => a == Archetype.WhiteCity ? CityBlocks : Houses;

        public static float Clamp(float v) => Mathf.Clamp(v, Min, Max);

        public static void Reset() { CityBlocks = DefaultCityBlocks; Houses = DefaultHouses; Trees = DefaultTrees; }
    }
}
