#nullable enable
using System.Collections.Generic;

namespace Statecraft.Game.Map
{
    /// <summary>Fixed camera poses that frame the same ground as the approved promo images, so every art checkpoint compares
    /// like with like. Yaw 0 looks north (the sea on the left), as both promo images do.</summary>
    public readonly struct PromoPose
    {
        public readonly string Name; public readonly double Lon, Lat; public readonly float Distance, Yaw;

        public PromoPose(string name, double lon, double lat, float distance, float yaw)
        { Name = name; Lon = lon; Lat = lat; Distance = distance; Yaw = yaw; }

        /// <summary>promo/final/03-gameplay-1936.png: Jaffa and Tel Aviv at the left, Lydda and Ramla to the right, the sea
        /// top-left, Qalqilya top-right. Matched on hex count (about nine across, so about 60 km), not on geography: the
        /// promo's Jaffa-to-Ramla spread (about 17 km over 60% of the frame) would need a 25-30 km view, but under the
        /// owner's symbolic-scale ruling (2026-10-11) settlements will sprawl over neighbouring hexes, so the hex count is
        /// the scale the art track will match. The 6 km hero zoom stays available for close looks.</summary>
        public static readonly PromoPose Gameplay03 = new PromoPose("gameplay03", 34.86, 32.00, 60000f, 10f);

        /// <summary>promo/final/02-map-diorama.png: the country from the Negev northwards, the sea on the left, the Dead Sea
        /// right of centre; the camera's farthest zoom.</summary>
        public static readonly PromoPose Diorama02 = new PromoPose("diorama02", 35.05, 31.55, CameraController.MaxDistance, 0f);

        public static IReadOnlyList<PromoPose> All { get; } = new[] { Gameplay03, Diorama02 };

        /// <summary>Close checks for the art checkpoint, not promo matches (Task 6 review A3): the 6 km hero zoom over the
        /// Judean slopes and over the Negev plateau, where texture steps in the baked shading would show. VisualShots shoots
        /// them after All, with the haze off.</summary>
        public static IReadOnlyList<PromoPose> CloseChecks { get; } = new[]
        {
            new PromoPose("hero06judea", 35.15, 31.78, 6000f, 0f), new PromoPose("hero06negev", 34.90, 30.60, 6000f, 0f),
        };

        /// <summary>Issue #659 (owner ruling on #700: "Models up close only"): the close views where MATKOT's building models
        /// draw instead of the kits (TownModelLayer.CloseEnterM): Tel Aviv at 30 km and Jaffa's old town at 12 km. Not promo
        /// matches: VisualShots shoots them after All (everything on), and FrameProfile prices them under "vclose".</summary>
        public static IReadOnlyList<PromoPose> CloseUps { get; } = new[]
        {
            new PromoPose("tel_aviv30", 34.78, 32.08, 30000f, 10f), new PromoPose("jaffa12", 34.755, 32.052, 12000f, 10f),
        };

        /// <summary>Issue #659: the other town types up close, for the main / branch captures the issue's done-when asks for:
        /// Jerusalem and Haifa at 12 km, the moshava Rishon LeZion (its b23 winery) and the Arab town Nazareth at 8 km, the
        /// kibbutz Degania Alef and the moshav Nahalal at 6 km, each over its settlements_1948.yaml point; and start120, the
        /// 120 km starting distance over the centre, where #917's whole-town landmarks draw and the kits do. Capture poses
        /// only: VisualShots shoots them after CloseUps; FrameProfile and the model tests keep to CloseUps (a kibbutz at 6 km
        /// holds fewer models than their floors).</summary>
        public static IReadOnlyList<PromoPose> TownChecks { get; } = new[]
        {
            new PromoPose("jerusalem12", 35.2256, 31.7789, 12000f, 10f), new PromoPose("haifa12", 34.9992, 32.8192, 12000f, 10f),
            new PromoPose("rishon08", 34.80, 31.95, 8000f, 10f), new PromoPose("degania06", 35.5747, 32.7081, 6000f, 10f),
            new PromoPose("nazareth08", 35.3033, 32.7019, 8000f, 10f), new PromoPose("nahalal06", 35.1967, 32.69, 6000f, 10f),
            new PromoPose("start120", 35.00, 32.20, 120000f, 0f),
        };

        public void Apply(CameraRig rig, GeoFrame frame)
        {
            rig.LookAt = frame.ToWorld(Lon, Lat, 0); rig.Distance = Distance; rig.Yaw = Yaw; rig.Apply();
        }
    }
}
