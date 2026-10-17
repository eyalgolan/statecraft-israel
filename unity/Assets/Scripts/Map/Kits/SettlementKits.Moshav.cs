#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The moshav kit (issue #32): Nahalal, the map's one moshav, already stands on a ring (<see cref="Allows"/>); this
    /// gives the ring its plan. The basis is Richard Kauffmann's 1921 layout as the English Wikipedia's "Nahalal" gives it
    /// (https://en.wikipedia.org/wiki/Nahalal, checked 2026-10-17): "the public buildings (school, administration offices,
    /// services, and warehouses) in the centre", "then a ring street with the farmers' homesteads bordering on it on the
    /// outside"; the Hebrew article agrees ("ובחלק הפנימי של המעגל נבנו מוסדות הציבור", https://he.wikipedia.org/wiki/נהלל).
    /// So the hub gets a public building, near the heart across from the water tower, and each farmstead is turned with its long side along the
    /// ring. The building's size and colours are modelling choices.</summary>
    public static partial class SettlementKits
    {
        /// <summary>The public building's footprint (real m, at the house scale) and wall height in storeys: longer than a
        /// farmstead (a school's row of classrooms), one storey with a high room.</summary>
        const float MoshavHallW = 10.5f, MoshavHallD = 6f, MoshavHallStoreys = 1.3f;

        /// <summary>The yaw (degrees) that sets a building's long side (its own x) along the ring through
        /// <paramref name="offset"/> from the hub: at right angles to the line from the hub.</summary>
        static float RingYaw(Vector2 offset) => Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg + 90f;

        /// <summary>A moshav's public building (a landmark, placed after the water tower and before any house): plastered walls under
        /// a red tiled gable, on a free spot near one of the dining hall's spots round <paramref name="heart"/> (radius
        /// <paramref name="R"/>; they keep clear of the water tower), long side along the ring; or none. Returns how many were
        /// built (0 or 1).</summary>
        static int PublicBuilding(string owner, Vector2 heart, float R, KitGround ground, KitMesh km, Lots lots)
        {
            float m = KitScale.Houses, w = m * MoshavHallW, d = m * MoshavHallD, h = MoshavHallStoreys * 3.3f * m;
            // One ring of the search only, as the dining hall's: pushed to the edge it would no longer be the hub.
            var p = Vector2.zero; bool found = false;
            foreach (var at in KibbutzHallAt)
                if (found = Claim(lots, heart + at * R, 0.5f * Mathf.Sqrt(w * w + d * d), owner, ground, out p, rings: 1)) break;
            if (!found) return 0;
            float yaw = RingYaw(p - heart);
            var (bottom, top) = GroundSpan(p, yaw, w, d, ground, m);
            var wall = C(0.91f, 0.86f, 0.75f); var roof = C(0.70f, 0.30f, 0.20f);
            km.Box(p.x, p.y, yaw, w, d, bottom, top, h, wall, roof, Facade.Village, m, roofed: true);
            km.Gable(p.x, p.y, yaw, w, d, top + h, d * 0.42f, roof, wall);
            Record(owner, "public_building", p, yaw, w, d, h, wall);
            return 1;
        }
    }
}
