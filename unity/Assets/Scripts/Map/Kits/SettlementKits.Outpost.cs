#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The Negev outpost kit (issue #32): a lookout of 1943 is drawn as the walled yard it was, not as a moshava. The
    /// basis is the Hebrew Wikipedia's "שלושת המצפים" (https://he.wikipedia.org/wiki/שלושת_המצפים, checked 2026-10-16): the
    /// three lookouts (Gvulot, Revivim, Beit Eshel) were built to one plan, "חצר מרובעת מוקפת חומה, ובפינתה מגדל תצפית. בצלע
    /// אחת של הריבוע מבני המגורים, ובצלע השנייה מבני השירות" (a square yard closed by a wall, a lookout tower in its corner,
    /// the living quarters along one side of the square and the service buildings along another). Its "רביבים" article
    /// (https://he.wikipedia.org/wiki/רביבים, same day) gives Revivim's 25 settlers one fortified building, "טירה שכללה חדרים,
    /// מטבח וחדר אוכל" (rooms, a kitchen and a dining hall), so the living side includes the dining room: the larger living
    /// building is recorded as Revivim's dining hall, the one a kibbutz has (#820). The sizes and colours are modelling choices.</summary>
    public static partial class SettlementKits
    {
        /// <summary>Whether <paramref name="s"/> is drawn as a Negev lookout. Revivim is the only one on the map: Gvulot and Beit
        /// Eshel, and the eleven points of 1946, have no row in content/map/.</summary>
        static bool NegevOutpost(Settlement s) => s.Id == "revivim";

        /// <summary>The yard's side and its wall's height and thickness (real m, at the house scale).</summary>
        const float OutpostSide = 22f, OutpostWallH = 2.6f, OutpostWallT = 0.7f;

        /// <summary>A Negev lookout at <paramref name="heart"/>, on the settlement's grid angle: the walled yard with its gate,
        /// the lookout tower in one corner, the living quarters along the opposite side and the service buildings along a
        /// third, all flat-roofed. One lot for the whole yard; nothing is built when no free dry spot takes it. Returns how
        /// many things were built (the yard, the tower and the four buildings, or 0). Up close (issue #1032) the yard and its
        /// tower are copied as one landmark at the models' scale, and (issue #659) each building is one of Tal Heuberger's
        /// models at its place in that copy, so they keep their places in the yard.</summary>
        static int NegevOutpostKit(Settlement s, Vector2 heart, KitGround ground, KitMesh km, Lots lots)
        {
            float m = KitScale.Houses, side = OutpostSide * m, half = 0.5f * side;
            if (!Claim(lots, heart, half * 1.4142f, s.Id, ground, out var c)) return 0;
            float yaw = GridYaw(s.Id), a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            Vector2 At(float u, float v) => c + new Vector2(u * ca - v * sa, u * sa + v * ca) * m;
            var stone = C(0.80f, 0.74f, 0.63f); var concrete = C(0.86f, 0.84f, 0.79f); var roof = C(0.72f, 0.70f, 0.66f);
            var start = km.CutStart();
            // The wall: four sides, the +v one broken by the gate (3 m wide, on the -u side of the middle).
            float h = 0.5f * OutpostSide, t = OutpostWallT;
            var pieces = new (float u, float v, float w, float d)[]
            {
                (0f, -h + 0.5f * t, OutpostSide, t), (-h + 0.5f * t, 0f, t, OutpostSide), (h - 0.5f * t, 0f, t, OutpostSide),
                (-7.5f, h - 0.5f * t, 7f, t), (5f, h - 0.5f * t, 12f, t),
            };
            foreach (var (u, v, w, d) in pieces)
            {
                var p = At(u, v);
                var (bottom, top) = GroundSpan(p, yaw, w * m, d * m, ground, m);
                km.Box(p.x, p.y, yaw, w * m, d * m, bottom, top, OutpostWallH * m, stone, stone, Facade.Plain, m);
            }
            Record(s.Id, "outpost_yard", c, yaw, side, side, OutpostWallH * m, stone);
            // The lookout tower in the yard's (+u, +v) corner: three storeys under a slightly wider cap.
            {
                var p = At(h - 2f, h - 2f); float w = 4f * m, th = 9.5f * m;
                var (bottom, top) = GroundSpan(p, yaw, w, w, ground, m);
                km.Box(p.x, p.y, yaw, w, w, bottom, top, th, stone, roof, Facade.Stone, m);
                km.Box(p.x, p.y, yaw, w * 1.08f, w * 1.08f, top + th, top + th, 1f * m, stone, roof, Facade.Plain, m);
                Record(s.Id, "lookout_tower", p, yaw, w, w, th + 1f * m, stone);
            }
            AtWorldScale(s.Id, "outpost_yard", km, start, c, ground.Y(c.x, c.y), m);   // #1032: the wall and the tower, as one landmark
            // Issue #659: the copy above is shrunk about the yard's centre by this, so each building's model stands there.
            float k = Mathf.Min(1f, ModelScale / m);
            // The living quarters along the -v side, the service buildings along the -u side: one storey, flat
            // roofs, the living rooms' small windows. Inside the wall, clear of each other and of the tower.
            float inner = h - t;
            var rooms = new (float u, float v, float w, float d, bool living)[]
            {
                (-1.1f, -inner + 2.4f, 9f, 4.8f, true), (6.9f, -inner + 2.4f, 6f, 4.8f, true),
                (-inner + 2.1f, -5.2f, 4.2f, 6f, false), (-inner + 2.1f, 1.6f, 4.2f, 6f, false),
            };
            for (int i = 0; i < rooms.Length; i++)
            {
                var (u, v, w, d, living) = rooms[i];
                var p = At(u, v); float bh = 3.3f * m;
                var (bottom, top) = GroundSpan(p, yaw, w * m, d * m, ground, m);
                // Issue #659 (owner, 18 Oct 2026: "Use b09a and b07"): up close the dining hall is Tal Heuberger's b07 and
                // the other buildings his b09a communal building, at their places in the yard's copy; far off, the kit's.
                var q = c + (p - c) * k;
                var (qBottom, qTop) = GroundSpan(q, yaw, w * m * k, d * m * k, ground, m);
                AddModel(s.Id, i == 0 ? TownModel.B07DiningHall : TownModel.B09aCommunal, q, yaw, w * m, d * m, bh, qBottom, qTop, km, m,
                    exactHeight: false, flip: false);
                var cut = km.CutStart();
                km.Box(p.x, p.y, yaw, w * m, d * m, bottom, top, bh, concrete, roof, living ? Facade.Village : Facade.Plain, m);
                km.CutEnd(cut);
                // The first, larger living building holds the fortress's "חדר אוכל" (the source above): Revivim's one dining hall.
                Record(s.Id, i == 0 ? "dining_hall" : "building", p, yaw, w * m, d * m, bh, concrete);
                if (i == 0) kibbutzHalls++;
            }
            return 2 + rooms.Length;
        }
    }
}
