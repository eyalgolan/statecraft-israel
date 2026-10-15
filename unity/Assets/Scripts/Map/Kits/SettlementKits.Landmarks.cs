#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The settlements' landmarks, placed before any town's houses (docs/visual-v1.md, "Towns round 2", lists each
    /// with its source). Positions keep the real bearing from the settlement's point to the landmark's sourced coordinate;
    /// distances and sizes are symbolic, like the buildings.</summary>
    public static partial class SettlementKits
    {
        /// <summary>A settlement's landmarks: the four cities' own (Jaffa, Jerusalem, Haifa, Tel Aviv) and Ramla's White
        /// Tower, else its archetype's (an old town's or an Arab town's mosque, a moshava's water tower). Returns how many.</summary>
        static int TownLandmarks(Settlement s, Vector2 centre, KitGround ground, KitMesh km, Lots lots, ref KitRandom rnd)
        {
            var arch = ArchetypeOf(s);
            float R = FootprintRadiusM(s, ground.HexSizeM);
            var heart = DryNear(centre, ground, R * 0.3f, ref rnd);
            switch (s.Id)
            {
                case "jaffa": return Jaffa(s, heart, ground, km, lots, ref rnd);
                case "jerusalem": return Jerusalem(s, ground, km, lots, ref rnd);
                case "haifa": return Haifa(s, ground, km, lots, ref rnd);
                case "tel_aviv": return TelAvivPort(s, ground, km, lots, ref rnd);
                case "ramla":
                    return Mosque(s.Id, heart, ground, km, lots, ref rnd, 1f)
                        + WhiteTower(s.Id, heart + Bearing(ground, s, 34.86611, 31.92750) * 18f * KitScale.Landmarks, ground, km, lots);
            }
            switch (arch)
            {
                case Archetype.OldTown: return Mosque(s.Id, heart, ground, km, lots, ref rnd, 1.2f);
                case Archetype.ArabTown: return Mosque(s.Id, heart, ground, km, lots, ref rnd, 1f);
                case Archetype.Moshava: return WaterTower(s.Id, heart + new Vector2(R * 0.35f, -R * 0.2f), ground, km, lots);
                default: return 0;
            }
        }

        static Vector2 World(KitGround g, double lon, double lat) { var w = g.Frame.ToWorld(lon, lat, 0); return new Vector2(w.x, w.z); }

        /// <summary>The unit vector from settlement <paramref name="s"/>'s point towards a sourced coordinate.</summary>
        static Vector2 Bearing(KitGround g, Settlement s, double lon, double lat) => (World(g, lon, lat) - World(g, s.Lon, s.Lat)).normalized;

        /// <summary>The step (m) of the shore and rectangle scans.</summary>
        const float ScanStepM = 125f;

        /// <summary>The first open-water point from <paramref name="from"/> along <paramref name="dir"/> (ScanStepM steps, up to
        /// <paramref name="maxM"/>), or null.</summary>
        static Vector2? ToSea(Vector2 from, Vector2 dir, KitGround ground, float maxM)
        {
            for (float d = 0f; d <= maxM; d += ScanStepM)
            {
                var p = from + dir * d;
                if (ground.Water(p.x, p.y)) return p;
            }
            return null;
        }

        /// <summary>The first dry point from <paramref name="from"/> along <paramref name="dir"/>, or null.</summary>
        static Vector2? ToLand(Vector2 from, Vector2 dir, KitGround ground, float maxM)
        {
            for (float d = 0f; d <= maxM; d += ScanStepM)
            {
                var p = from + dir * d;
                if (ground.Inside(p.x, p.y) && !ground.Water(p.x, p.y)) return p;
            }
            return null;
        }

        // ---- Jaffa ----------------------------------------------------------------------------------------------------

        /// <summary>Jaffa: the Mahmoudiya Mosque (1812) at the old town's north-east corner, the clock tower (1900-03) on the
        /// plaza east of it, St Peter's Church (1888-94) on the hill near the shore, the lighthouse (1865, red and white bands)
        /// on the hilltop above the old port, and the port's breakwater.</summary>
        static int Jaffa(Settlement s, Vector2 heart, KitGround ground, KitMesh km, Lots lots, ref KitRandom rnd)
        {
            float m = KitScale.Landmarks;
            int n = 0;
            var mosqueWant = heart + Bearing(ground, s, 34.75548, 32.054917) * 22f * m;
            n += Mosque(s.Id, mosqueWant, ground, km, lots, ref rnd, 1.2f);
            var stone = C(0.86f, 0.76f, 0.57f);
            // The clock tower stands on the plaza the mosque's east gate faces.
            if (Claim(lots, mosqueWant + new Vector2(20f * m, 2f * m), 4.5f * m, s.Id, ground, out var p))
            {
                float y = ground.Y(p.x, p.y);
                km.Box(p.x, p.y, 15f, 5f * m, 5f * m, y - 2f * m, y, 24f * m, stone, stone, Facade.Stone, m, roofed: true);
                km.Box(p.x, p.y, 15f, 4f * m, 4f * m, y + 24f * m, y + 24f * m, 4f * m, C(0.90f, 0.82f, 0.64f), stone, Facade.Plain, m, roofed: true);
                km.Pyramid(p.x, p.y, 15f, 4.4f * m, 4.4f * m, y + 28f * m, 3.5f * m, C(0.55f, 0.47f, 0.40f));
                Record(s.Id, "clock_tower", p, 15f, 5f * m, 5f * m, 31.5f * m, stone);
                n++;
            }
            if (Church(s.Id, heart + Bearing(ground, s, 34.752083, 32.054778) * 26f * m, ground, km, lots, 1f)) n++;
            // The lighthouse on the hilltop above the old port: the last dry ground towards it, a little inland.
            var dir = Bearing(ground, s, 34.7505056, 32.0534583);
            var shore = ToSea(heart, dir, ground, 12000f);
            if (shore is Vector2 sea)
            {
                var want = sea - dir * 5f * m;
                if (Claim(lots, want, 2.6f * m, s.Id, ground, out var q))
                {
                    float y = ground.Y(q.x, q.y);
                    var white = C(0.93f, 0.92f, 0.88f); var red = C(0.70f, 0.22f, 0.17f);
                    const int bands = 4; float bandH = 12f * m / bands;
                    for (int b = 0; b < bands; b++)
                        km.Shaft(new Vector3(q.x, y - (b == 0 ? 2f * m : 0f) + b * bandH, q.y), new Vector3(q.x, y + (b + 1) * bandH, q.y),
                            Mathf.Lerp(1.9f, 1.4f, b / (float)bands) * m, Mathf.Lerp(1.9f, 1.4f, (b + 1) / (float)bands) * m, b % 2 == 0 ? white : red, 10);
                    km.Shaft(new Vector3(q.x, y + 12f * m, q.y), new Vector3(q.x, y + 14.5f * m, q.y), 1.5f * m, 1.5f * m, C(0.30f, 0.32f, 0.30f), 10);
                    km.Cone(new Vector3(q.x, y + 14.5f * m, q.y), 1.8f * m, 2.2f * m, red, 10);
                    Record(s.Id, "lighthouse", q, 0f, 3.8f * m, 3.8f * m, 16.7f * m, white);
                    n++;
                }
                n += Port(sea, ground, km, ref rnd);
            }
            return n;
        }

        /// <summary>Jaffa's harbour, as promo 03 draws it: a stone breakwater curving out to sea from the shore below the
        /// lighthouse. Its boats are SeaCraft's.</summary>
        static int Port(Vector2 shore, KitGround ground, KitMesh km, ref KitRandom rnd)
        {
            float m = KitScale.Landmarks;
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
                Record("jaffa", "breakwater", mid, yaw, dir.magnitude * 1.08f, 5f * m, 2.2f * m, stone);   // SeaCraft keeps its boats off it
                prev = cur;
            }
            Record("jaffa", "port", c, 0f, r, r, 2.2f * m, stone);
            // The boats are SeaCraft's (#388). The random draws the old boats made here stay, so the stream every later
            // Jaffa house is drawn from is unchanged.
            int boats = 0;
            for (int i = 0; i < 12 && boats < 5; i++)
            {
                var b = c + new Vector2(rnd.Range(-1.6f, 0.2f) * r, rnd.Range(-1.4f, 0.4f) * r);
                if (!ground.Water(b.x, b.y) || ground.Water(b.x + 40f * m, b.y) == false) continue;
                rnd.Range(0f, 360f); rnd.Range(9f, 14f);
                boats++;
            }
            return 1;
        }

        // ---- Jerusalem ------------------------------------------------------------------------------------------------

        /// <summary>The Old City's centre (its sourced coordinate) and the half side (m) of its symbolic walled square: 4 km
        /// across, about 4 times the real Old City (0.9 km^2), so the walls hold the Haram and a quarter of stone houses at
        /// the kits' scale. In metres, not scaled with the kits: the drawn 1948 roads fix where the Haram can stand
        /// (docs/visual-v1.md, "Towns round 2"). Those roads ran round the real walls; at this size they cross inside the
        /// square, through gaps in the walls.</summary>
        static (Vector2 centre, float half) OldCity(KitGround g) => (World(g, 35.23417, 31.77667), 2000f);

        /// <summary>How far inside the walls the Old City's houses keep (and outside them the new city's).</summary>
        const float OldCityInset = 220f;

        /// <summary>Jerusalem: Suleiman's walls (1537-41) round a symbolic square Old City with five gates (Jaffa Gate on the
        /// west, Damascus Gate on the north wall's west side, Lions' Gate on the east, Zion and Dung Gates on the south) and a
        /// gap wherever a 1948 road or the railway crosses the line, the citadel near Jaffa Gate, and the Haram al-Sharif in
        /// the city's east: its platform, the Dome of the Rock (blue and white Ottoman tiles; its dome blackened lead until
        /// the 1959-64 restoration) and the Al-Aqsa Mosque at the platform's south end (its dome lead). The rest of the
        /// footprint is houses (PlaceHouses: the new city's stone blocks to the west, the Arab quarters round the other sides).</summary>
        static int Jerusalem(Settlement s, KitGround ground, KitMesh km, Lots lots, ref KitRandom rnd)
        {
            var (oc, half) = OldCity(ground);
            // Above the Old City's houses (one or two storeys inside the walls: PlaceHouses), so the circuit reads.
            float wallT = 380f, wallH = 900f;
            var stone = C(0.82f, 0.72f, 0.55f); var top = C(0.86f, 0.78f, 0.62f);
            int n = 0;
            // The Haram al-Sharif in the city's south-east (in the real city it fills the east side down to the south wall):
            // its platform, the Dome of the Rock and Al-Aqsa, where the platform keeps clear of the drawn Jericho road. The
            // roads leave no room for the real platform's north-south length, so it is wider than long and Al-Aqsa stands in
            // its south-east corner rather than across its south end (docs/visual-v1.md, "Towns round 2").
            float pw = 1300f, pl = 1100f;
            var pc = oc + new Vector2(350f, -1200f);
            var paving = C(0.86f, 0.82f, 0.72f);
            if (RectClear(pc, pw, pl, ground) && lots.Free(pc, 0.5f * Mathf.Sqrt(pw * pw + pl * pl)))   // first in the Old City
            {
                var (pb, pg) = GroundSpan(pc, 0f, pw, pl, ground, 1f);
                km.Box(pc.x, pc.y, 0f, pw, pl, pb, pg, 80f, C(0.80f, 0.72f, 0.58f), paving, Facade.Plain, 10f);
                // Its lots: discs over the platform, each clear of the routes (RectClear checked the whole platform).
                for (float u = -pw * 0.5f + 150f; u < pw * 0.5f; u += 260f)
                    for (float v = -pl * 0.5f + 150f; v < pl * 0.5f; v += 260f)
                    {
                        var c = pc + new Vector2(u, v);
                        if (!ground.OnRoute(c, 190f)) lots.Add(new Lot { c = c, r = 190f, id = s.Id });
                    }
                Record(s.Id, "haram", pc, 0f, pw, pl, 80f, paving);
                n++;
                float deck = pg + 80f;
                // The Dome of the Rock in the platform's west (18x across: the 52 m octagon about 0.95 km; its walls and drum 30x
                // high, so it rises over the Old City's roofs): marble below, blue and white tiles above, a tiled drum and the
                // lead dome (dark: before the 1959-64 restoration).
                float k = 18f, kv = 30f;
                float rOct = 27f * k;
                var dc = pc + new Vector2(-0.5f * pw + rOct + 30f, 0f);
                var marble = C(0.88f, 0.86f, 0.80f); var tiles = C(0.36f, 0.50f, 0.64f); var lead = C(0.26f, 0.27f, 0.29f);
                km.Shaft(new Vector3(dc.x, deck - 60f, dc.y), new Vector3(dc.x, deck + 5f * kv, dc.y), rOct, rOct, marble, 8);
                km.Shaft(new Vector3(dc.x, deck + 5f * kv, dc.y), new Vector3(dc.x, deck + 11f * kv, dc.y), rOct, rOct, tiles, 8);
                km.Cone(new Vector3(dc.x, deck + 11f * kv, dc.y), rOct, 2.5f * k, C(0.62f, 0.62f, 0.60f), 8);
                km.Shaft(new Vector3(dc.x, deck + 11f * kv, dc.y), new Vector3(dc.x, deck + 19f * kv, dc.y), 11f * k, 11f * k, tiles, 16);
                km.Dome(dc.x, dc.y, deck + 19f * kv, 11f * k, lead, 16, 6);
                Record(s.Id, "dome_of_the_rock", dc, 0f, 2f * rOct, 2f * rOct, 19f * kv + 11f * k, tiles);
                n++;
                // Al-Aqsa in the platform's south-east corner: a hall running north-south (16x) and its lead dome.
                float aw = 230f, al = 480f;
                var ac = pc + new Vector2(0.5f * pw - 0.5f * aw - 40f, -0.5f * pl + 0.5f * al + 40f);
                km.Box(ac.x, ac.y, 0f, aw, al, deck - 60f, deck, 300f, C(0.84f, 0.77f, 0.62f), C(0.70f, 0.70f, 0.68f), Facade.Stone, 16f);
                km.Dome(ac.x, ac.y + 0.3f * al, deck + 280f, 0.45f * aw, C(0.55f, 0.56f, 0.57f), 12, 5);
                Record(s.Id, "al_aqsa", ac, 0f, aw, al, 300f, C(0.84f, 0.77f, 0.62f));
                n++;
            }

            // Gates: (side, position along it as a share of the half side). Sides: 0 north, 1 east, 2 south, 3 west.
            var gates = new (int side, float at)[] { (3, 0.05f), (0, -0.15f), (1, 0.6f), (2, -0.45f), (2, -0.05f) };
            const int pieces = 12;
            float len = 2f * half / pieces, pieceR = 0.5f * Mathf.Sqrt(len * len + wallT * wallT);   // a piece's half-diagonal
            int built = 0;
            for (int side = 0; side < 4; side++)
            {
                // Along each side from one corner to the next, clockwise from the north-west corner.
                Vector2 a0 = side switch { 0 => new Vector2(-half, half), 1 => new Vector2(half, half), 2 => new Vector2(half, -half), _ => new Vector2(-half, -half) };
                Vector2 dir = side switch { 0 => Vector2.right, 1 => Vector2.down, 2 => Vector2.left, _ => Vector2.up };
                float yaw = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                var gap = new bool[pieces];
                for (int i = 0; i < pieces; i++) gap[i] = ground.OnRoute(oc + a0 + dir * (len * (i + 0.5f)), pieceR);   // a road's or the railway's gap
                for (int i = 0; i < pieces; i++)
                {
                    var mid = oc + a0 + dir * (len * (i + 0.5f));
                    if (gap[i]) continue;
                    var (bottom, gy) = GroundSpan(mid, yaw, len, wallT, ground, 1f);
                    km.Box(mid.x, mid.y, yaw, len * 1.01f, wallT, bottom, gy, wallH, stone, top, Facade.Plain, KitScale.Houses);
                    km.Box(mid.x, mid.y, yaw, len * 1.01f, wallT * 0.35f, gy + wallH, gy + wallH, 90f, top, top, Facade.Plain, KitScale.Houses);   // the parapet
                    lots.Add(new Lot { c = mid, r = pieceR, id = s.Id });
                    built++;
                    // A gate tower where a road passes the wall line (a road entered by a gate), else a square tower at every
                    // other joint.
                    for (int end = 0; end < 2; end++)
                    {
                        int next = end == 0 ? i - 1 : i + 1;
                        bool byRoad = next >= 0 && next < pieces && gap[next];
                        if (!byRoad && !(end == 1 && i % 2 == 1 && i + 1 < pieces)) continue;
                        float tw = byRoad ? 600f : 480f, th = wallH + (byRoad ? 400f : 260f);
                        var t = oc + a0 + dir * (len * (i + end) + (byRoad ? (end == 0 ? 1f : -1f) * 0.5f * tw : 0f));
                        if (ground.OnRoute(t, 0.5f * tw + 10f)) continue;   // the tower itself stays off the road
                        var (tb, ty) = GroundSpan(t, yaw, tw, tw, ground, 1f);
                        km.Box(t.x, t.y, yaw, tw, tw, tb, ty, th, stone, top, byRoad ? Facade.Stone : Facade.Plain, KitScale.Houses);
                        if (byRoad) Record(s.Id, "gate", t, yaw, tw, tw, th, stone);
                    }
                }
                foreach (var (gs, at) in gates)
                {
                    if (gs != side) continue;
                    var gp = oc + a0 + dir * (half + at * half);
                    if (ground.OnRoute(gp, 330f)) continue;
                    var (gb, gy) = GroundSpan(gp, yaw, 650f, 420f, ground, 1f);
                    km.Box(gp.x, gp.y, yaw, 650f, 420f, gb, gy, wallH + 400f, stone, top, Facade.Stone, KitScale.Houses);
                    Record(s.Id, "gate", gp, yaw, 650f, 420f, wallH + 400f, stone);
                }
            }
            if (built > 0) { Record(s.Id, "walls", oc, 0f, 2f * half, 2f * half, wallH, stone); n++; }

            // The citadel (the Tower of David) by Jaffa Gate, with its minaret (20x): at its sourced spot (about 0.6 km west of
            // the Old City's centre), moved east off the drawn Hebron road.
            float m = 20f;
            if (Claim(lots, oc + new Vector2(-450f, -150f), 10f * m, s.Id, ground, out var cp))
            {
                float y = ground.Y(cp.x, cp.y);
                km.Box(cp.x, cp.y, 0f, 18f * m, 16f * m, y - 2f * m, y, 12f * m, stone, top, Facade.Stone, m);
                km.Box(cp.x - 5f * m, cp.y + 4f * m, 0f, 7f * m, 7f * m, y + 12f * m - 1f, y + 12f * m, 6f * m, stone, top, Facade.Stone, m);
                km.Shaft(new Vector3(cp.x + 6f * m, y, cp.y - 5f * m), new Vector3(cp.x + 6f * m, y + 30f * m, cp.y - 5f * m), 1.7f * m, 1.4f * m, stone, 8);
                km.Cone(new Vector3(cp.x + 6f * m, y + 30f * m, cp.y - 5f * m), 1.6f * m, 3.5f * m, C(0.62f, 0.62f, 0.58f), 8);
                Record(s.Id, "citadel", cp, 0f, 18f * m, 16f * m, 33.5f * m, stone);
                n++;
            }
            return n;
        }

        /// <summary>True when a north-aligned w x l rectangle at <paramref name="c"/> keeps 60 m off every route corridor
        /// (sampled every ScanStepM, edges included).</summary>
        static bool RectClear(Vector2 c, float w, float l, KitGround ground)
        {
            int nx = Mathf.CeilToInt(w / ScanStepM), nz = Mathf.CeilToInt(l / ScanStepM);
            for (int i = 0; i <= nx; i++)
                for (int j = 0; j <= nz; j++)
                    if (ground.OnRoute(c + new Vector2(-0.5f * w + w * i / nx, -0.5f * l + l * j / nz), 60f)) return false;
            return true;
        }

        // ---- Haifa ----------------------------------------------------------------------------------------------------

        /// <summary>Haifa: the deep-water port (opened 1933) on the bay north of the town, a long main breakwater and a lee
        /// breakwater with warehouse sheds along the quays, and the German Colony (the Templers' colony of 1868) west of the
        /// centre: stone houses with red roofs in two rows along its main street, which runs north-south down to the harbour.</summary>
        static int Haifa(Settlement s, KitGround ground, KitMesh km, Lots lots, ref KitRandom rnd)
        {
            float m = KitScale.Landmarks;
            int n = 0;
            var stone = C(0.74f, 0.70f, 0.62f);
            var portPt = World(ground, 35.00448, 32.820118);
            var shore = ToSea(portPt, Vector2.up, ground, 8000f);
            if (shore is Vector2 sh)
            {
                float sea = ground.Y(sh.x, sh.y);
                int pieces = 0;
                void Arm(Vector2 from, Vector2 to)
                {
                    var d = to - from; int k = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / (8f * m)));
                    float yaw = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    for (int i = 0; i < k; i++)
                    {
                        var mid = from + d * ((i + 0.5f) / k);
                        if (!ground.Water(mid.x, mid.y)) continue;
                        km.Box(mid.x, mid.y, yaw, d.magnitude / k * 1.04f, 5f * m, sea - 2f * m, sea, 2.4f * m, stone, stone, Facade.Plain, m);
                        Record(s.Id, "breakwater", mid, yaw, d.magnitude / k * 1.04f, 5f * m, 2.4f * m, stone);   // SeaCraft keeps its boats off it
                        pieces++;
                    }
                }
                // The main breakwater out from the shore west of the port and along the bay; the lee breakwater from the east.
                var w0 = sh + new Vector2(-40f * m, 0f); var w1 = w0 + new Vector2(0f, 24f * m); var w2 = w1 + new Vector2(96f * m, 0f);
                Arm(w0, w1); Arm(w1, w2);
                var e0 = sh + new Vector2(70f * m, 0f); var e1 = e0 + new Vector2(0f, 16f * m); var e2 = e1 + new Vector2(-20f * m, 0f);
                Arm(e0, e1); Arm(e1, e2);
                if (pieces > 0) { Record(s.Id, "port", sh + new Vector2(30f * m, 12f * m), 0f, 140f * m, 24f * m, 2.4f * m, stone); n++; }
                // Warehouse sheds on the quays: on the first dry ground south of the shore line.
                var shed = C(0.80f, 0.76f, 0.68f);
                for (int i = 0; i < 4; i++)
                {
                    var along = sh + new Vector2((-25f + i * 20f) * m, 0f);
                    if (!(ToLand(along + new Vector2(0f, 10f * m), Vector2.down, ground, 4000f) is Vector2 land)) continue;
                    if (!Claim(lots, land + new Vector2(0f, -6f * m), 8f * m, s.Id, ground, out var q)) continue;
                    var (b, y) = GroundSpan(q, 0f, 15f * m, 6f * m, ground, m);
                    km.Box(q.x, q.y, 0f, 15f * m, 6f * m, b, y, 3.5f * m, shed, C(0.62f, 0.58f, 0.54f), Facade.Plain, m, roofed: true);
                    km.Gable(q.x, q.y, 0f, 15f * m, 6f * m, y + 3.5f * m, 1.6f * m, C(0.58f, 0.55f, 0.52f), shed);
                    Record(s.Id, "warehouse", q, 0f, 15f * m, 6f * m, 5.1f * m, shed);
                    n++;
                }
            }
            // The German Colony: its avenue from the shore southwards, a row of Templer houses on each side.
            var colony = World(ground, 34.99056, 32.82000);
            var top = ToSea(colony, Vector2.up, ground, 8000f) ?? colony;
            float hm = KitScale.Houses;
            int houses = 0;
            for (int i = 0; i < 4; i++)
                for (int side = -1; side <= 1; side += 2)
                {
                    var want = top + new Vector2(side * 8f * hm, -(9f + i * 13f) * hm);
                    var spec = Pick(Style.Templer, 0.5f, ref rnd, hm);
                    if (!Claim(lots, want, spec.lotR, s.Id, ground, out var q)) continue;
                    var drnd = new KitRandom(rnd.Next());
                    Emit(s.Id, Style.Templer, q, 90f, spec, 0.5f, ground, km, ref drnd);
                    houses++;
                }
            if (houses > 0) { Record(s.Id, "german_colony", top + new Vector2(0f, -28f * hm), 90f, 52f * hm, 16f * hm, 0f, C(0.72f, 0.31f, 0.20f)); n += houses; }
            return n;
        }

        // ---- Tel Aviv -------------------------------------------------------------------------------------------------

        /// <summary>Tel Aviv's port in the city's north-west (opened 1936, inaugurated at its full size in 1938; closed 1965):
        /// a mole out to sea and hangar sheds on the shore behind it, at its sourced coordinate.</summary>
        static int TelAvivPort(Settlement s, KitGround ground, KitMesh km, Lots lots, ref KitRandom rnd)
        {
            float m = KitScale.Landmarks;
            var at = World(ground, 34.7732528, 32.0964833);
            // The shore: west from the port's point to the sea, or east from it to land when the point lies in the water.
            Vector2? shoreSea = ground.Water(at.x, at.y) ? at : ToSea(at, Vector2.left, ground, 6000f);
            if (!(shoreSea is Vector2 sea)) return 0;
            var land = ToLand(sea, Vector2.right, ground, 6000f);
            if (!(land is Vector2 l)) return 0;
            int n = 0;
            float y = ground.Y(sea.x, sea.y);
            var stone = C(0.74f, 0.70f, 0.62f);
            int pieces = 0;
            var p0 = sea; var p1 = sea + new Vector2(-24f * m, 0f); var p2 = p1 + new Vector2(0f, 18f * m);
            foreach (var (a, b) in new[] { (p0, p1), (p1, p2) })
            {
                var d = b - a; int k = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / (8f * m))); float yaw = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                for (int i = 0; i < k; i++)
                {
                    var mid = a + d * ((i + 0.5f) / k);
                    if (!ground.Water(mid.x, mid.y)) continue;
                    km.Box(mid.x, mid.y, yaw, d.magnitude / k * 1.04f, 4.5f * m, y - 2f * m, y, 2.2f * m, stone, stone, Facade.Plain, m);
                    Record(s.Id, "breakwater", mid, yaw, d.magnitude / k * 1.04f, 4.5f * m, 2.2f * m, stone);   // SeaCraft keeps its boats off it
                    pieces++;
                }
            }
            if (pieces > 0) { Record(s.Id, "port", sea + new Vector2(-12f * m, 6f * m), 0f, 24f * m, 18f * m, 2.2f * m, stone); n++; }
            var shed = C(0.82f, 0.79f, 0.72f);
            for (int i = 0; i < 3; i++)
            {
                // Behind the beach strip (BeachSetbackM), so the sand stays open in front of them.
                if (!Claim(lots, l + new Vector2(BeachSetbackM + 4f * m, (i - 1) * 17f * m), 8.5f * m, s.Id, ground, out var q)) continue;
                var (bt, gy) = GroundSpan(q, 90f, 15f * m, 6f * m, ground, m);
                km.Box(q.x, q.y, 90f, 15f * m, 6f * m, bt, gy, 3.2f * m, shed, C(0.62f, 0.58f, 0.54f), Facade.Plain, m, roofed: true);
                km.Gable(q.x, q.y, 90f, 15f * m, 6f * m, gy + 3.2f * m, 1.8f * m, C(0.60f, 0.58f, 0.55f), shed);
                Record(s.Id, "warehouse", q, 90f, 15f * m, 6f * m, 5f * m, shed);
                n++;
            }
            return n;
        }

        // ---- Shared landmarks -----------------------------------------------------------------------------------------

        /// <summary>Ramla's White Tower: the five-storey square minaret of the White Mosque (rebuilt after the 1318
        /// earthquake; 27 m), east of the town's heart, at its sourced bearing.</summary>
        static int WhiteTower(string owner, Vector2 want, KitGround ground, KitMesh km, Lots lots)
        {
            float m = KitScale.Landmarks;
            if (!Claim(lots, want, 5f * m, owner, ground, out var p)) return 0;
            float y = ground.Y(p.x, p.y), storey = 27f / 5f * m;
            var stone = C(0.90f, 0.86f, 0.76f);
            for (int i = 0; i < 5; i++)
            {
                float w = Mathf.Lerp(7f, 5.6f, i / 4f) * m;
                km.Box(p.x, p.y, 0f, w, w, i == 0 ? y - 2f * m : y + i * storey - 1f, y + i * storey, storey, stone, stone, Facade.Stone, m);
            }
            km.Box(p.x, p.y, 0f, 3.2f * m, 3.2f * m, y + 27f * m - 1f, y + 27f * m, 2.6f * m, stone, stone, Facade.Plain, m, roofed: true);
            km.Dome(p.x, p.y, y + 29.6f * m, 1.6f * m, C(0.80f, 0.78f, 0.72f));
            Record(owner, "white_tower", p, 0f, 7f * m, 7f * m, 31f * m, stone);
            return 1;
        }

        static int Mosque(string owner, Vector2 want, KitGround ground, KitMesh km, Lots lots, ref KitRandom rnd, float size)
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
            Record(owner, "mosque", p, 0f, 14f * m, 14f * m, 30.5f * m, stone);
            return 1;
        }

        /// <summary>A village mosque (S1): a one-storey stone hall with a flat lime roof and a short, thick square minaret at one
        /// corner, about 14 m tall, inside the hall's footprint, so the lot is the hall's. Village minarets were "chunky" towers
        /// rising over a one-storey mosque (L. Dittmar, "A View from the Minaret", Harvard Divinity Bulletin, 2019, on
        /// Qaysaria; https://bulletin.hds.harvard.edu/a-view-from-the-minaret/), and a village's square minaret stood 10-15 m
        /// (Kawfakha, from Petersen's Gazetteer of Buildings in Muslim Palestine: https://en.wikipedia.org/wiki/Kawfakha;
        /// both checked 2026-10-15). The domed mosque with its tall round minaret (<see cref="Mosque"/>) stays the towns'.</summary>
        static int VillageMosque(string owner, Vector2 want, KitGround ground, KitMesh km, Lots lots, ref KitRandom rnd, float size)
        {
            float m = KitScale.Landmarks * size, w = 12f * m, d = 9f * m;
            if (!Claim(lots, want, 0.5f * Mathf.Sqrt(w * w + d * d), owner, ground, out var p)) return 0;
            float yaw = rnd.Range(0f, 90f);
            var (bottom, top) = GroundSpan(p, yaw, w, d, ground, m);
            var stone = Jitter(C(0.84f, 0.77f, 0.63f), ref rnd, 0.04f);
            var roof = Jitter(LimeRoof, ref rnd, 0.03f);
            km.Box(p.x, p.y, yaw, w, d, bottom, top, 5.5f * m, stone, roof, Facade.Stone, m);
            float a = yaw * Mathf.Deg2Rad, u = 0.5f * w - 1.4f * m, v = -(0.5f * d - 1.4f * m);
            float mx = p.x + u * Mathf.Cos(a) - v * Mathf.Sin(a), mz = p.y + u * Mathf.Sin(a) + v * Mathf.Cos(a);
            km.Box(mx, mz, yaw, 2.8f * m, 2.8f * m, top + 5.5f * m - 0.3f * m, top + 5.5f * m, 7.9f * m, stone, roof, Facade.Plain, m);
            km.Box(mx, mz, yaw, 3.3f * m, 3.3f * m, top + 13.4f * m - 0.2f * m, top + 13.4f * m, 0.6f * m, stone, roof, Facade.Plain, m);   // the balcony's parapet
            Record(owner, "village_mosque", p, yaw, w, d, 14f * m, stone);
            return 1;
        }

        /// <summary>A stone church with a gabled nave and a belfry topped by a pyramid roof; false, and nothing built, when no
        /// free dry spot is near.</summary>
        static bool Church(string owner, Vector2 want, KitGround ground, KitMesh km, Lots lots, float size)
        {
            float m = KitScale.Landmarks * size;
            var stone = C(0.86f, 0.76f, 0.57f);
            if (!Claim(lots, want, 10f * m, owner, ground, out var q)) return false;
            float yq = ground.Y(q.x, q.y);
            km.Box(q.x, q.y, 0f, 9f * m, 18f * m, yq - 2f * m, yq, 12f * m, stone, stone, Facade.Stone, m, roofed: true);
            km.Box(q.x, q.y + 7f * m, 0f, 4.5f * m, 4.5f * m, yq + 12f * m, yq + 12f * m, 14f * m, stone, stone, Facade.Stone, m, roofed: true);
            km.Pyramid(q.x, q.y + 7f * m, 0f, 4.8f * m, 4.8f * m, yq + 26f * m, 6f * m, C(0.60f, 0.52f, 0.44f));
            km.Gable(q.x, q.y, 0f, 9f * m, 18f * m, yq + 12f * m, 4f * m, C(0.66f, 0.34f, 0.24f), stone);
            Record(owner, "church", q, 0f, 9f * m, 18f * m, 32f * m, stone);
            return true;
        }

        static int WaterTower(string owner, Vector2 want, KitGround ground, KitMesh km, Lots lots, float size = 1f)
        {
            float m = KitScale.Houses * size;
            if (!Claim(lots, want, 4f * m, owner, ground, out var p)) return 0;
            float y = ground.Y(p.x, p.y);
            var concrete = C(0.82f, 0.80f, 0.76f);
            Record(owner, "water_tower", p, 0f, 6f * m, 6f * m, 14.7f * m, concrete);
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
}
