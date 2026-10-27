#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The camp kit (GDD Phase 4 PR 8, #792): a built training camp (a sites.yaml site of kind military_camp,
    /// SiteLayer) is drawn as a camp, not as a kibbutz: a fenced square yard with its gate, rows of long one-storey barracks
    /// and an open parade ground by the gate. No source was read for the layout; the plan, sizes and colours are modelling
    /// choices, kept as plain as the Negev outpost's (SettlementKits.Outpost.cs), whose yard pattern this follows.</summary>
    public static partial class SettlementKits
    {
        /// <summary>The settlement type a camp's kit is built from (SiteLayer.KitOf). No settlements_1948.yaml row has it.</summary>
        public const string CampType = "military_camp";

        static bool MilitaryCamp(Settlement s) => s.Type == CampType;

        /// <summary>The yard's side and the fence's height and thickness (real m, at the house scale).</summary>
        const float CampSide = 30f, CampFenceH = 1.8f, CampFenceT = 0.4f;

        /// <summary>A camp at <paramref name="heart"/>, on the settlement's grid angle: the fenced yard (the +v side broken by
        /// the gate), two columns of three barracks on the -v two thirds, the parade ground left open on the gate side. One lot
        /// for the whole yard; nothing is built when no free dry spot takes it. Returns how many things were built (the yard
        /// and its barracks, or 0).</summary>
        static int MilitaryCampKit(Settlement s, Vector2 heart, KitGround ground, KitMesh km, Lots lots)
        {
            float m = KitScale.Houses, half = 0.5f * CampSide * m;
            if (!Claim(lots, heart, half * 1.4142f, s.Id, ground, out var c)) return 0;
            float yaw = GridYaw(s.Id), a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            Vector2 At(float u, float v) => c + new Vector2(u * ca - v * sa, u * sa + v * ca) * m;
            var fence = C(0.55f, 0.52f, 0.46f); var timber = C(0.74f, 0.66f, 0.52f); var tin = C(0.58f, 0.60f, 0.58f);
            // The fence: four sides, the +v one broken by a 4 m gate in its middle.
            float h = 0.5f * CampSide, t = CampFenceT;
            var pieces = new (float u, float v, float w, float d)[]
            {
                (0f, -h + 0.5f * t, CampSide, t), (-h + 0.5f * t, 0f, t, CampSide), (h - 0.5f * t, 0f, t, CampSide),
                (-8.5f, h - 0.5f * t, 13f, t), (8.5f, h - 0.5f * t, 13f, t),
            };
            foreach (var (u, v, w, d) in pieces)
            {
                var p = At(u, v);
                var (bottom, top) = GroundSpan(p, yaw, w * m, d * m, ground, m);
                km.Box(p.x, p.y, yaw, w * m, d * m, bottom, top, CampFenceH * m, fence, fence, Facade.Plain, m);
            }
            Record(s.Id, "camp_yard", c, yaw, 2f * half, 2f * half, CampFenceH * m, fence);
            // The barracks: 10 x 3.6 m, timber walls under tin roofs, in two columns (u = -7, 7) of three rows (v = -10, -4,
            // 2), inside the fence and clear of each other; the yard's +v third, by the gate, is the parade ground.
            int n = 1;
            foreach (var u in new[] { -7f, 7f })
                foreach (var v in new[] { -10f, -4f, 2f })
                {
                    var p = At(u, v); float w = 10f * m, d = 3.6f * m, bh = 3f * m;
                    var (bottom, top) = GroundSpan(p, yaw, w, d, ground, m);
                    km.Box(p.x, p.y, yaw, w, d, bottom, top, bh, timber, tin, Facade.Plain, m);
                    Record(s.Id, "barracks", p, yaw, w, d, bh, timber);
                    n++;
                }
            return n;
        }
    }
}
