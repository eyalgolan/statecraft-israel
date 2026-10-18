#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>What a kibbutz's plot holds (#32): a member's house or a farm shed (None: not a kibbutz building).</summary>
    enum KibbutzPart : byte { None, House, Shed }

    /// <summary>The kibbutz kit (issue #32): a kibbutz no longer shares the moshava's houses, so a kibbutz and a moshav (Nahalal's
    /// ring) or a moshava read as different places. The basis is Richard Kauffmann's plans as the Hebrew Wikipedia's "גן הנוף
    /// הקיבוצי" sums them up (https://he.wikipedia.org/wiki/גן_הנוף_הקיבוצי, checked 2026-10-16): the farm area kept apart from
    /// the living area ("ההפרדה בין אזור המשק לאזור המגורים"), the farm about a third of the kibbutz's ground, the dining hall at
    /// the centre ("חדר האוכל, במרכזו של הקיבוץ"), and in the early kibbutzim the members' houses built in rows, one storey
    /// ("הוקמו מבני המגורים בשורות", "בני קומה אחת"). The sizes are modelling choices that fit the moshava's plot lattice, on
    /// which a kibbutz is still laid out, so its plots are where they were.</summary>
    public static partial class SettlementKits
    {
        /// <summary>The dining hall's wall height in storeys: taller than the one-storey houses round it (a hall's high room).</summary>
        const float KibbutzHallStoreys = 1.6f;

        /// <summary>The dining hall's footprint (real m, at the house scale) and the spots it tries in turn (shares of the radius
        /// from the heart): across the heart from the water tower (which stands at +0.35, -0.2), each far enough from it that
        /// their lots do not meet.</summary>
        const float KibbutzHallW = 8.4f, KibbutzHallD = 6.4f;
        static readonly Vector2[] KibbutzHallAt = { new Vector2(-0.35f, 0.25f), new Vector2(-0.38f, -0.05f), new Vector2(0f, 0.45f) };

        /// <summary>The farm yard: plots farther than this share of the radius from the heart, within 60 degrees either side of
        /// the farm's bearing: a third of the directions, about a quarter of the ring's area (the source gives the farm about a
        /// third of the ground; a modelling choice that keeps most plots for houses in a kibbutz of a few plots).</summary>
        const float FarmYardFromT = 0.45f;

        /// <summary>Running counts for <see cref="Result.KibbutzHalls"/> and <see cref="Result.FarmSheds"/> during a Build.</summary>
        [System.ThreadStatic] static int kibbutzHalls, farmSheds;

        /// <summary>Whether a kibbutz plot <paramref name="offset"/> from the heart (t: distance over the radius) lies in its farm
        /// yard. The farm's bearing is the kibbutz's grid angle, so the yard is one side of the place, not a ring.</summary>
        static bool FarmYard(Settlement s, Vector2 offset, float t)
        {
            if (t <= FarmYardFromT) return false;
            float a = GridYaw(s.Id) * Mathf.Deg2Rad;
            return Vector2.Dot(offset.normalized, new Vector2(Mathf.Cos(a), Mathf.Sin(a))) > 0.5f;
        }

        /// <summary>A kibbutz building of <paramref name="part"/>, <paramref name="u"/> and <paramref name="v"/> (0-1) picking its
        /// size, at scale <paramref name="m"/>; its lot is the half diagonal times <paramref name="lotK"/>. Real sizes: a
        /// member's house 7.5-9 x 4.6-5.4 m (a long, narrow row house), a farm shed 7.8-9.4 x 6.2-7.2 m, one storey. Both fit
        /// the moshava's 11.5 x 9.8 m plot with its jitter and yaw.</summary>
        static Spec KibbutzSpec(KibbutzPart part, float u, float v, float m, float lotK)
        {
            bool shed = part == KibbutzPart.Shed;
            float w = shed ? Mathf.Lerp(7.8f, 9.4f, u) : Mathf.Lerp(7.5f, 9f, u), d = shed ? Mathf.Lerp(6.2f, 7.2f, v) : Mathf.Lerp(4.6f, 5.4f, v);
            return new Spec { w = w * m, d = d * m, h = 3.3f * m, storeys = 1, part = part, lotR = 0.5f * Mathf.Sqrt(w * w + d * d) * m * lotK };
        }

        /// <summary>What stands on a kibbutz plot: a shed in the farm yard, else the member's house <paramref name="house"/> (its
        /// size picks the shed's within the shed's range, so no extra draw moves the place's stream).</summary>
        static Spec KibbutzPlot(Settlement s, Vector2 offset, float t, Spec house, float m, float lotK)
        {
            if (!FarmYard(s, offset, t)) return house;
            float u = Mathf.InverseLerp(7.5f * m, 9f * m, house.w), v = Mathf.InverseLerp(4.6f * m, 5.4f * m, house.d);
            return KibbutzSpec(KibbutzPart.Shed, u, v, m, lotK);
        }

        /// <summary>A kibbutz's dining hall (a landmark, placed with the water tower before any house): white, a flat roof and
        /// a raised clerestory, 1.6 storeys, at the house scale, on a free spot near one of
        /// <see cref="KibbutzHallAt"/> round <paramref name="heart"/> (radius <paramref name="R"/>), or none. Returns how many were built (0 or 1).</summary>
        static int DiningHall(string owner, Vector2 heart, float R, KitGround ground, KitMesh km, Lots lots)
        {
            float m = KitScale.Houses, w = KitScale.Houses * KibbutzHallW, d = KitScale.Houses * KibbutzHallD, h = KibbutzHallStoreys * 3.3f * m;
            // One ring of the search only (half a lot from the spot): a hall pushed to the edge would no longer be the heart, and
            // would take the ground of the few houses a small kibbutz has (Yad Mordechai, with no free ground near its heart).
            var p = Vector2.zero; bool found = false;
            foreach (var at in KibbutzHallAt)
                if (found = Claim(lots, heart + at * R, 0.5f * Mathf.Sqrt(w * w + d * d), owner, ground, out p, rings: 1)) break;
            if (!found) return 0;
            float yaw = GridYaw(owner);
            var (bottom, top) = GroundSpan(p, yaw, w, d, ground, m);
            var wall = WhitePlaster; var roof = C(0.86f, 0.85f, 0.82f);
            // Issue #659: up close the hall is MATKOT's b07 dining hall, the clerestory's height included; far off, the kit's.
            AddModel(owner, TownModel.B07DiningHall, p, yaw, w, d, h + 1.2f * m, bottom, top, km, m, exactHeight: false, flip: false);
            var cut = km.CutStart();
            km.Box(p.x, p.y, yaw, w, d, bottom, top, h, wall, roof, Facade.Plain, m);
            km.Box(p.x, p.y, yaw, w * 0.8f, d * 0.4f, top + h - 0.3f * m, top + h, 1.2f * m, wall, roof, Facade.Plain, m);
            km.CutEnd(cut);
            Record(owner, "dining_hall", p, yaw, w, d, h, wall);
            kibbutzHalls++;
            return 1;
        }

        /// <summary>Draws a kibbutz building: a member's house whitewashed under a red tiled gable, a farm shed with weathered
        /// walls under a low grey roof (corrugated sheet). Exactly two colour jitters whatever the part (a 1948 locality's
        /// stream relies on it). Returns the wall colour.</summary>
        static Color32 KibbutzBuilding(KitMesh km, Vector2 p, float yaw, Spec b, float bottom, float top, float m, ref KitRandom rnd)
        {
            switch (b.part)
            {
                case KibbutzPart.Shed:
                {
                    var wall = Jitter(C(0.80f, 0.76f, 0.68f), ref rnd, 0.04f);
                    var roof = Jitter(C(0.47f, 0.48f, 0.49f), ref rnd, 0.05f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Plain, m, roofed: true);
                    km.Gable(p.x, p.y, yaw, b.w, b.d, top + b.h, Mathf.Min(b.w, b.d) * 0.3f, roof, wall);
                    farmSheds++;
                    return wall;
                }
                default:
                {
                    var wall = Jitter(C(0.93f, 0.92f, 0.87f), ref rnd, 0.03f);
                    var roof = Jitter(C(0.74f, 0.31f, 0.20f), ref rnd, 0.08f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Village, m, roofed: true);
                    km.Gable(p.x, p.y, yaw, b.w, b.d, top + b.h, Mathf.Min(b.w, b.d) * 0.42f, roof, wall);
                    return wall;
                }
            }
        }
    }
}
