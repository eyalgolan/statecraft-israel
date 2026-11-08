#nullable enable
using Statecraft.Game.UI;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>#2202 (plan docs/superpowers/plans/2026-11-02-enemy-move-playback.md, Task 4): the camera's eased glide to a
    /// replay's beat, as pure functions over the rig. It moves the look-at point only, never the yaw or the distance (owner,
    /// 2 November 2026, Q5: "Keep the player's zoom"; the GDD camera never orbits). The caller sets LookAt from
    /// <see cref="At"/> and lets CameraController.Settle hold the whole view on the map, as it holds a pan, so a beat by the
    /// map's edge stops where the player's own pan would. A player's pan or zoom ends the glide (<see cref="TookOver"/>).</summary>
    public static class CameraGlide
    {
        /// <summary>A pan this far (metres) or less is no pan: TookOver's tolerance for the look-at point.</summary>
        public const float StillM = 1f;

        /// <summary>The look-at point <paramref name="t"/> (clamped to 0..1) of the way from <paramref name="from"/> to
        /// <paramref name="to"/>, eased in and out (smoothstep) in x and z. The height is <paramref name="from"/>'s:
        /// CameraController.Step lifts the point over the ground again.</summary>
        public static Vector3 At(Vector3 from, Vector3 to, float t)
        {
            t = Mathf.Clamp01(t);
            float s = t * t * (3f - 2f * t);
            return new Vector3(from.x + (to.x - from.x) * s, from.y, from.z + (to.z - from.z) * s);
        }

        /// <summary>How long a glide of <paramref name="metres"/> takes on hexes <paramref name="hexMetres"/> across: the
        /// replay's pan pacing (UnitBeats.PanSeconds, owner Q3), so the planner and the glide share one clamp.</summary>
        public static float Seconds(float metres, float hexMetres) => UnitBeats.PanSeconds(hexMetres > 0f ? metres / hexMetres : 0f);

        /// <summary>Whether the camera, as the rig was last applied, shows <paramref name="point"/> inside the map area (below
        /// the UI's top band, CameraRig.TopInset) with <paramref name="margin"/> of its width and height to spare on each
        /// side: a beat there needs no pan. Projected through the camera itself, so the view's narrower near edge counts.</summary>
        public static bool InView(CameraRig rig, Vector3 point, float margin = 0.15f)
        {
            var cam = rig.GetComponent<Camera>();
            if (cam == null) return false;
            var v = cam.WorldToViewportPoint(point);
            float top = 1f - Mathf.Clamp(rig.TopInset, 0f, CameraRig.MaxTopInset);
            return v.z > 0f && v.x >= margin && v.x <= 1f - margin && v.y >= margin * top && v.y <= (1f - margin) * top;
        }

        /// <summary>Whether someone other than the glide moved the camera since it last set <paramref name="lastLookAt"/> and
        /// <paramref name="lastDistance"/> (as Settle left them): the look-at point moved more than <see cref="StillM"/> in x or
        /// z, or the distance changed. The height is Step's own (the lift over the ground), so it is not compared.</summary>
        public static bool TookOver(CameraRig rig, Vector3 lastLookAt, float lastDistance)
        {
            float dx = rig.LookAt.x - lastLookAt.x, dz = rig.LookAt.z - lastLookAt.z;
            return dx * dx + dz * dz > StillM * StillM || rig.Distance != lastDistance;
        }
    }
}
