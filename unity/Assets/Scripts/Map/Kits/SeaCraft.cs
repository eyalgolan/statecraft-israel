#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The boats on the Mediterranean, as promo 03 draws them (refs #388): white-sailed craft under way off the coast
    /// and craft moored in and off the harbours of Jaffa, Tel Aviv and Haifa. Craft of the period: lateen-rigged and
    /// gaff-rigged sailing and fishing boats, and a small coastal steamer at anchor off Haifa's breakwater; no motor
    /// launches or modern craft. Like the buildings, sizes are symbolic: a felucca is drawn about a quarter of a hex across
    /// (2 km at the default scale), as the promo's boats are, so it reads at the gameplay zoom. One static mesh under the
    /// kits' root, drawn with Kit.mat, so the "kits" toggle covers it. Seeded: the same map draws the same boats.</summary>
    public static class SeaCraft
    {
        public enum Kind { Felucca, Cutter, Steamer, Moored }

        /// <summary>One boat: its kind, centre (world x, z), heading (degrees, from +x towards +z), length (world m) and the
        /// place it belongs to (a port's settlement id, or the coast it sails off).</summary>
        public readonly struct Craft
        {
            public readonly Kind Kind; public readonly Vector2 At; public readonly float Yaw, Length; public readonly string Place;
            public Craft(Kind kind, Vector2 at, float yaw, float length, string place) { Kind = kind; At = at; Yaw = yaw; Length = length; Place = place; }
            public Vector2 Forward { get { float a = Yaw * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(a), Mathf.Sin(a)); } }
        }

        /// <summary>Lengths in the kits' landmark metres (x KitScale.Landmarks): a felucca 34, a cutter 30, the steamer 46, a
        /// moored boat 16; a felucca is about a quarter of a hex across at the default scale.</summary>
        static float LengthOf(Kind k) => k switch { Kind.Felucca => 34f, Kind.Cutter => 30f, Kind.Steamer => 46f, _ => 16f } * KitScale.Landmarks;

        /// <summary>The coasts the boats under way sail off: a point on the shore (lon, lat), the direction out to sea (world
        /// x, z) and how many boats. Jaffa's, Tel Aviv's and Haifa's are the points the settlement kits build those ports at
        /// (docs/visual-v1.md, "Towns round 2"); Bat Yam's, Acre's and Gaza's are approximate points on their shores, chosen
        /// for placement only.</summary>
        static readonly (string place, double lon, double lat, Vector2 seaward, int boats)[] Coasts =
        {
            ("jaffa", 34.7505056, 32.0534583, Vector2.left, 3),
            ("tel_aviv", 34.7732528, 32.0964833, Vector2.left, 2),
            ("bat_yam", 34.745, 32.020, Vector2.left, 1),
            ("haifa", 35.00448, 32.820118, Vector2.up, 2),
            ("acre", 35.0675, 32.9205, Vector2.left, 1),
            ("gaza", 34.44, 31.52, Vector2.left, 1),
        };

        /// <summary>Where the boats are: the moored craft in and off each harbour the settlement kits recorded
        /// (<paramref name="placed"/>'s "port" footprints), then those under way off <see cref="Coasts"/>, between
        /// <see cref="OffshoreNearM"/> and <see cref="OffshoreM"/> out from the shore. Every hull stands in open sea, clear of
        /// the harbour works (the "breakwater" pieces the kits record) and of every other boat. <paramref name="salt"/>
        /// varies the draw (the tests sweep it); the map uses 0.</summary>
        public static List<Craft> Place(KitGround ground, IReadOnlyList<SettlementKits.Placed> placed, ulong salt = 0)
        {
            var list = new List<Craft>();
            var rnd = new KitRandom("sea_craft", salt);
            float m = KitScale.Landmarks / 60f;
            var ports = new List<SettlementKits.Placed>(); var works = new List<SettlementKits.Placed>();
            foreach (var p in placed)
            {
                if (p.Kind == "port") ports.Add(p);
                else if (p.Kind == "breakwater") works.Add(p);
            }
            foreach (var p in ports) Moor(p, ground, works, list, ref rnd);
            foreach (var (place, lon, lat, seaward, boats) in Coasts)
            {
                var w = ground.Frame.ToWorld(lon, lat, 0); var at = new Vector2(w.x, w.z);
                var shore = ground.Water(at.x, at.y) ? at : FirstWater(at, seaward, ground, 20000f);
                if (!(shore is Vector2 s)) continue;
                var along = new Vector2(-seaward.y, seaward.x);
                int put = 0;
                for (int tries = 0; tries < 60 && put < boats; tries++)
                {
                    var kind = rnd.Value() < 0.6f ? Kind.Felucca : Kind.Cutter;
                    float L = LengthOf(kind);
                    var c = s + seaward * rnd.Range(OffshoreNearM, OffshoreM) * m + along * rnd.Range(-2.2f, 2.2f) * L;
                    var craft = new Craft(kind, c, Heading(ref rnd, 35f), L, place);
                    if (!Clear(craft, ground, works, list, 0.5f * L) || NearPort(c, ports, 0.8f * L)) continue;
                    list.Add(craft); put++;
                }
            }
            return list;
        }

        /// <summary>East or west, give or take <paramref name="spread"/> degrees: the gameplay camera looks north, so the
        /// boats show their sails side-on, as the promo's do. On this coast, running north-south, that is reaching out to sea
        /// or back in, and moored craft lie stern-to the quays.</summary>
        static float Heading(ref KitRandom rnd, float spread) => (rnd.Value() < 0.5f ? 0f : 180f) + rnd.Range(-spread, spread);

        /// <summary>How far out the boats under way sail (world m at the default landmark scale, 60): from past the surf to the
        /// edge of the gameplay pose's view of the coast.</summary>
        const float OffshoreNearM = 2400f, OffshoreM = 7500f;

        /// <summary>A harbour's water as the settlement kits record it: Jaffa's a circle (its breakwater's centre and radius,
        /// SettlementKits.Port), Tel Aviv's and Haifa's a box (centre, width east-west, depth north-south). Returns how far
        /// <paramref name="q"/> lies outside it (0 inside).</summary>
        static float PortDistance(SettlementKits.Placed port, Vector2 q)
        {
            if (port.Id == "jaffa") return Mathf.Max(0f, (q - port.C).magnitude - port.W);
            var d = new Vector2(Mathf.Max(0f, Mathf.Abs(q.x - port.C.x) - 0.5f * port.W), Mathf.Max(0f, Mathf.Abs(q.y - port.C.y) - 0.5f * port.D));
            return d.magnitude;
        }

        static bool NearPort(Vector2 q, List<SettlementKits.Placed> ports, float margin)
        {
            foreach (var p in ports) if (PortDistance(p, q) < margin) return true;
            return false;
        }

        /// <summary>Moored craft in a recorded harbour: in Jaffa's basin inside its curved breakwater, in Tel Aviv's lee of its
        /// mole and along Haifa's quays, never touching a breakwater piece. Where the basin has no room at the boats' size,
        /// they lie at anchor just off the harbour on its seaward side (Tel Aviv's south of its mole, clear of Jaffa's label
        /// to the north-west); Haifa's coastal steamer always does.</summary>
        static void Moor(SettlementKits.Placed port, KitGround ground, List<SettlementKits.Placed> works, List<Craft> list, ref KitRandom rnd)
        {
            int want = port.Id == "haifa" ? 3 : 2;
            var seaward = port.Id == "haifa" ? Vector2.up : Vector2.left;
            var along = new Vector2(-seaward.y, seaward.x);
            float extent = port.Id == "jaffa" ? port.W : 0.5f * (seaward.x != 0f ? port.W : port.D);
            int put = 0;
            for (int tries = 0; tries < 120 && put < want; tries++)
            {
                var kind = port.Id == "haifa" && put == 0 ? Kind.Steamer : Kind.Moored;
                float L = LengthOf(kind);
                Vector2 c;
                if (tries < 80 && kind == Kind.Moored)
                {
                    if (port.Id == "jaffa")
                    {
                        // The basin: the breakwater's quarter circle runs from the shore south of the centre out west, so the
                        // water inside it lies south-west of the centre, within its radius.
                        float a = rnd.Range(190f, 260f) * Mathf.Deg2Rad, r = rnd.Range(0.25f, 0.75f) * port.W;
                        c = port.C + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    }
                    else c = port.C + new Vector2(rnd.Range(-0.4f, 0.4f) * port.W, rnd.Range(-0.25f, 0.25f) * port.D);
                }
                else
                {
                    // At anchor off the harbour. Tel Aviv's lie south of its mole (along is south there): at the gameplay pose
                    // the labels put Jaffa's name north-west of the mole, over the water there.
                    float t = port.Id == "tel_aviv" ? rnd.Range(0.3f, 1f) : rnd.Range(-1f, 1f);
                    c = port.C + seaward * (extent + rnd.Range(0.7f, 1.6f) * L) + along * t * extent;
                }
                var craft = new Craft(kind, c, Heading(ref rnd, 15f), L, port.Id);
                if (!Clear(craft, ground, works, list, 0.1f * L)) continue;
                list.Add(craft); put++;
            }
        }

        static Vector2? FirstWater(Vector2 from, Vector2 dir, KitGround ground, float maxM)
        {
            for (float d = 0f; d <= maxM; d += 125f)
            {
                var p = from + dir * d;
                if (ground.Water(p.x, p.y)) return p;
            }
            return null;
        }

        /// <summary>The hull's rectangle (its length by <see cref="Beam"/> of it, both grown by <paramref name="margin"/>):
        /// its samples all lie in open sea (not a lake: the rift lakes are below sea level), it meets no harbour work
        /// (<paramref name="works"/>, each piece's own rectangle), and its centre keeps clear of every boat already
        /// placed.</summary>
        static bool Clear(Craft c, KitGround ground, List<SettlementKits.Placed> works, List<Craft> list, float margin)
        {
            var f = c.Forward; var side = new Vector2(-f.y, f.x);
            float half = 0.5f * c.Length + margin, beam = 0.5f * Beam * c.Length + margin;
            for (int i = -2; i <= 2; i++)
                for (int j = -1; j <= 1; j++)
                {
                    var p = c.At + f * (half * i / 2f) + side * (beam * j);
                    if (!ground.Inside(p.x, p.y) || !ground.Water(p.x, p.y) || ground.ElevM(p.x, p.y) < -50f) return false;
                }
            foreach (var w in works)
                if (Overlap(c.At, c.Yaw, half, beam, w.C, w.Yaw, 0.5f * w.W, 0.5f * w.D)) return false;
            foreach (var o in list)
                if ((o.At - c.At).magnitude < 0.6f * (o.Length + c.Length)) return false;
            return true;
        }

        /// <summary>Whether two rectangles meet (centre, yaw in degrees, half-length along the yaw, half-width across it):
        /// the separating-axis test on their four edge normals.</summary>
        public static bool Overlap(Vector2 ca, float yawA, float ha, float wa, Vector2 cb, float yawB, float hb, float wb)
        {
            float a = yawA * Mathf.Deg2Rad, b = yawB * Mathf.Deg2Rad;
            var ax = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var ay = new Vector2(-ax.y, ax.x);
            var bx = new Vector2(Mathf.Cos(b), Mathf.Sin(b)); var by = new Vector2(-bx.y, bx.x);
            var d = cb - ca;
            foreach (var n in new[] { ax, ay, bx, by })
            {
                float ra = ha * Mathf.Abs(Vector2.Dot(ax, n)) + wa * Mathf.Abs(Vector2.Dot(ay, n));
                float rb = hb * Mathf.Abs(Vector2.Dot(bx, n)) + wb * Mathf.Abs(Vector2.Dot(by, n));
                if (Mathf.Abs(Vector2.Dot(d, n)) > ra + rb) return false;
            }
            return true;
        }

        // ---- The models ------------------------------------------------------------------------------------------------

        /// <summary>Beam and freeboard as shares of the length.</summary>
        const float Beam = 0.30f, Freeboard = 0.10f;

        static Color32 C(float r, float g, float b) => new Color32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), (byte)Facade.Plain);

        static readonly Color32 Sail = C(0.96f, 0.94f, 0.88f), Spar = C(0.30f, 0.23f, 0.16f), DeckColour = C(0.66f, 0.55f, 0.40f);
        static readonly Color32[] HullColours = { C(0.30f, 0.20f, 0.13f), C(0.36f, 0.24f, 0.15f), C(0.16f, 0.26f, 0.30f), C(0.42f, 0.18f, 0.12f) };
        static readonly Color32 Band = C(0.90f, 0.87f, 0.78f);

        /// <summary>Draws every boat of <paramref name="crafts"/> into <paramref name="km"/>, its waterline at the sea's surface
        /// under it.</summary>
        public static void Draw(IReadOnlyList<Craft> crafts, KitGround ground, KitMesh km)
        {
            var rnd = new KitRandom("sea_craft_models");
            foreach (var c in crafts)
            {
                float sea = ground.Y(c.At.x, c.At.y);
                var hull = c.Kind == Kind.Steamer ? C(0.14f, 0.14f, 0.15f) : HullColours[(int)(rnd.Value() * HullColours.Length) % HullColours.Length];
                float slim = c.Kind == Kind.Steamer ? 0.8f : 1f;
                Hull(km, c, sea, hull, slim);
                switch (c.Kind)
                {
                    case Kind.Felucca: Felucca(km, c, sea, ref rnd); break;
                    case Kind.Cutter: Cutter(km, c, sea, ref rnd); break;
                    case Kind.Steamer: Steamer(km, c, sea); break;
                    default: Moored(km, c, sea); break;
                }
            }
        }

        /// <summary>A point in the boat's frame: <paramref name="x"/> along it (forward), <paramref name="z"/> across (to
        /// port), <paramref name="y"/> up from the waterline; all in shares of its length.</summary>
        static Vector3 P(Craft c, float sea, float x, float y, float z)
        {
            var f = c.Forward; var s = new Vector2(-f.y, f.x);
            var q = c.At + f * (x * c.Length) + s * (z * c.Length);
            return new Vector3(q.x, sea + y * c.Length, q.y);
        }

        /// <summary>A quad facing <paramref name="outward"/> (its winding chosen so the face's normal does).</summary>
        static void Quad(KitMesh km, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 colour, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(c - a, d - b), outward) >= 0f) km.Face(a, b, c, d, colour, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            else km.Face(d, c, b, a, colour, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        }

        static void Tri(KitMesh km, Vector3 a, Vector3 b, Vector3 c, Color32 colour, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) >= 0f) km.Triangle(a, b, c, colour); else km.Triangle(a, c, b, colour);
        }

        /// <summary>A sail: both faces, each lit as if turned mostly to the sky (its own normal tipped up by
        /// <see cref="SailLift"/>), so the cloth reads white from either side, as the promo's sunlit sails do; as a flat plate,
        /// the face the camera saw was often the one turned from the sun, and read grey.</summary>
        static void SailTri(KitMesh km, Vector3 a, Vector3 b, Vector3 c)
        {
            var n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-6f) return;
            n.Normalize();
            km.Triangle(a, b, c, Sail, n * (1f - SailLift) + Vector3.up * SailLift);
            km.Triangle(a, c, b, Sail, -n * (1f - SailLift) + Vector3.up * SailLift);
        }

        const float SailLift = 0.7f;

        /// <summary>The deck's half-outline from stern to bow: (along, half-width), shares of the length and beam.</summary>
        static readonly Vector2[] Outline = { new(-0.5f, 0.40f), new(-0.28f, 0.50f), new(0.05f, 0.50f), new(0.30f, 0.36f), new(0.5f, 0f) };

        /// <summary>A pointed hull: the deck at the freeboard, sides sloping in to a narrower, shorter waterline (a sheer band
        /// of pale paint under the gunwale, the rest the hull's colour), a transom at the stern.</summary>
        static void Hull(KitMesh km, Craft c, float sea, Color32 hull, float slim)
        {
            float B = Beam * slim, F = Freeboard * (c.Kind == Kind.Steamer ? 0.9f : 1f), band = F * 0.72f, below = -0.02f;
            int n = Outline.Length;
            Vector3 Deck(int i, int side, float y) => P(c, sea, Outline[i].x, y, side * Outline[i].y * B * 0.5f);
            Vector3 Water(int i, int side) => P(c, sea, Outline[i].x * 0.9f, below, side * Outline[i].y * B * 0.5f * 0.7f);
            var centre = P(c, sea, 0f, F, 0f);
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < n - 1; i++)
                {
                    var mid = (P(c, sea, Outline[i].x, F, 0f) + P(c, sea, Outline[i + 1].x, F, 0f)) * 0.5f;
                    var outward = (Deck(i, side, F) + Deck(i + 1, side, F)) * 0.5f - mid; outward.y = 0f;
                    if (i == n - 2) outward += (P(c, sea, 1f, F, 0f) - centre).normalized * outward.magnitude;
                    // The pale sheer band, then the hull down to the waterline.
                    var a0 = Deck(i, side, F); var a1 = Deck(i + 1, side, F);
                    var b0 = Vector3.Lerp(Water(i, side), a0, band / (F - below)); var b1 = Vector3.Lerp(Water(i + 1, side), a1, band / (F - below));
                    Quad(km, a0, a1, b1, b0, Band, outward);
                    Quad(km, b0, b1, Water(i + 1, side), Water(i, side), hull, outward);
                    Tri(km, centre, Deck(i, side, F), Deck(i + 1, side, F), DeckColour, Vector3.up);
                }
            // The transom.
            Quad(km, Deck(0, -1, F), Deck(0, 1, F), Water(0, 1), Water(0, -1), hull, P(c, sea, -1f, F, 0f) - centre);
        }

        static void Mast(KitMesh km, Vector3 foot, float h, float r) => km.Shaft(foot, foot + Vector3.up * h, r, r * 0.7f, Spar, 5);

        /// <summary>A felucca: one mast stepped forward, a long yard slanting up aft, and the lateen sail under it, sheeted
        /// out to one side (so the top-down camera sees its face, as the promo's do).</summary>
        static void Felucca(KitMesh km, Craft c, float sea, ref KitRandom rnd)
        {
            float L = c.Length, side = rnd.Value() < 0.5f ? -1f : 1f, sheet = 0.16f * side;
            Mast(km, P(c, sea, 0.14f, Freeboard, 0f), 0.62f * L, 0.010f * L);
            var tack = P(c, sea, 0.42f, Freeboard + 0.05f, 0f);
            var peak = P(c, sea, -0.40f, 1.00f, sheet * 0.6f);
            var clew = P(c, sea, -0.30f, Freeboard + 0.08f, sheet);
            km.Shaft(tack, peak, 0.006f * L, 0.004f * L, Spar, 4);
            SailTri(km, tack, peak, clew);
        }

        /// <summary>A gaff cutter: a mast amidships, a four-sided mainsail on its gaff and boom aft of it and a jib forward,
        /// both sheeted out to one side.</summary>
        static void Cutter(KitMesh km, Craft c, float sea, ref KitRandom rnd)
        {
            float L = c.Length, side = rnd.Value() < 0.5f ? -1f : 1f, sheet = 0.14f * side;
            var foot = P(c, sea, 0.06f, Freeboard, 0f);
            Mast(km, foot, 0.95f * L, 0.011f * L);
            var throat = P(c, sea, 0.06f, 0.78f, 0f); var tack = P(c, sea, 0.06f, Freeboard + 0.06f, 0f);
            var peakG = P(c, sea, -0.30f, 0.98f, sheet * 0.8f); var clew = P(c, sea, -0.40f, Freeboard + 0.07f, sheet);
            SailTri(km, tack, throat, peakG); SailTri(km, tack, peakG, clew);
            var head = P(c, sea, 0.06f, 0.88f, 0f); var bow = P(c, sea, 0.52f, Freeboard + 0.04f, 0f);
            var jibClew = P(c, sea, 0.10f, Freeboard + 0.06f, sheet * 0.7f);
            SailTri(km, head, bow, jibClew);
        }

        /// <summary>A small coastal steamer: a white deckhouse amidships, a black funnel with a red band, and two short masts.</summary>
        static void Steamer(KitMesh km, Craft c, float sea)
        {
            float L = c.Length, F = Freeboard * 0.9f;
            var white = C(0.92f, 0.91f, 0.86f);
            var mid = P(c, sea, -0.06f, F, 0f);
            float yaw = c.Yaw;
            km.Box(mid.x, mid.z, yaw, 0.34f * L, 0.17f * L, mid.y, mid.y, 0.07f * L, white, white, Facade.Plain, 1f);
            var bridge = P(c, sea, 0.04f, F + 0.07f, 0f);
            km.Box(bridge.x, bridge.z, yaw, 0.08f * L, 0.14f * L, bridge.y, bridge.y, 0.05f * L, white, white, Facade.Plain, 1f);
            var funnel = P(c, sea, -0.10f, F + 0.07f, 0f);
            km.Shaft(funnel, funnel + Vector3.up * 0.10f * L, 0.03f * L, 0.03f * L, C(0.12f, 0.11f, 0.11f), 8);
            km.Shaft(funnel + Vector3.up * 0.10f * L, funnel + Vector3.up * 0.13f * L, 0.031f * L, 0.031f * L, C(0.62f, 0.16f, 0.12f), 8);
            Mast(km, P(c, sea, 0.30f, F, 0f), 0.26f * L, 0.006f * L);
            Mast(km, P(c, sea, -0.34f, F, 0f), 0.22f * L, 0.006f * L);
        }

        /// <summary>A moored fishing boat: its mast bare, the sail furled along the boom.</summary>
        static void Moored(KitMesh km, Craft c, float sea)
        {
            float L = c.Length;
            Mast(km, P(c, sea, 0.10f, Freeboard, 0f), 0.70f * L, 0.012f * L);
            var boom = P(c, sea, -0.13f, Freeboard + 0.07f, 0f);
            km.Box(boom.x, boom.z, c.Yaw, 0.46f * L, 0.045f * L, boom.y, boom.y, 0.04f * L, Sail, Sail, Facade.Plain, 1f);
        }
    }
}
