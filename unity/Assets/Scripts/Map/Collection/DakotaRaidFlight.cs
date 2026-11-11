#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>The Dakotas' raid on Tel Aviv, flown once on New game after the opening and before the raid card (#3000, PR 2 of
    /// #2968; owner, 8 November 2026, "Film, raid, card, advisors (Recommended)", and the Dakotas by "Reuse Dakotas
    /// (Recommended)"): each Dakota flies in from the south-west along the coast, turns east over the sea onto its loop's
    /// southern pass, crosses Tel Aviv on it dropping its bombs exactly as on the loop (DakotaBombRun, the same releases, flashes,
    /// blasts and smoke), then turns south and flies away. The wing follows the lead <see cref="TrailS"/> later on the same
    /// path. The same models, flags, shadows and effects as the loop (DakotaLoop.PlaceRaid): nothing is forked or added.
    /// Presentation only: a pure function of the map's animation clock (SpitfireLoop.Clock) since <see cref="Start"/>; no sim,
    /// content, save or hash change. While it flies, every DakotaLoop follows it instead of its loop; <see cref="Stop"/> hands
    /// them back to their loop clock.
    ///
    /// Time: an aircraft's raid time u is 0 at the middle of its pass over Tel Aviv, as its loop time is (SeaAirCollection
    /// .DakotaLoopOffset(0) is Tel Aviv's hex centre), and runs at the real clock's pace, so the bombs fall and the smoke rises
    /// at the loop's pace. The path is drawn in path time (<see cref="PathTime"/>): the same as u on the pass, and
    /// <see cref="Speedup"/> times faster on the way in and out, so the flight is not a minute long.</summary>
    public static class DakotaRaidFlight
    {
        /// <summary>DESIGN VALUE: half the pass (s of raid time) on which the raid flies the loop itself, through every release:
        /// the releases over Tel Aviv's hex lie within about 4 s of the pass's middle (its hex is 7.6 km across, about 10 s at
        /// the loop's 760 m/s), so 6 s keeps every bomb under its aircraft (SeaAirCollectionTests).</summary>
        public const float PassS = 6f;

        /// <summary>DESIGN VALUE: the path time (s) of the turn in from the start and of the turn out to the end. At the
        /// loop's speed at each end, the curves turn no tighter than about 6 km (the loop's own ends are 5.4 km).</summary>
        public const float TurnS = 50f;

        /// <summary>DESIGN VALUES: where the raid starts and ends, from Tel Aviv's hex centre (world x east, z north, m), and the
        /// bearing it flies there (degrees clockwise from north). In from about 39 km south-west, out at sea off Ashdod, flying
        /// north-north-east up the coast; out to about 34 km south-south-east, inland, flying due south. Chosen so neither turn
        /// first swings the other way (the heading turns one way only, to within a few degrees).</summary>
        public static readonly Vector2 FromM = new Vector2(-25000f, -30000f), ToM = new Vector2(16000f, -30000f);
        public const float FromBearing = 20f, ToBearing = 180f;

        /// <summary>DESIGN VALUES: off the pass the path runs this many times faster than the real clock, easing in over
        /// <see cref="RampS"/> real seconds from the pass's own pace, so the speed never jumps.</summary>
        public const float Speedup = 8f, RampS = 2f;

        /// <summary>DESIGN VALUE: the wing flies the raid this many seconds after the lead. Two Dakotas never meet while their
        /// centres are more than 2 x 0.54 x 9.8 = 10.6 km apart (SeaAirCollection.DakotaTrailShare); on the pass, the slowest
        /// part of the path, 15 s is about 11.4 km.</summary>
        public const float TrailS = 15f;

        /// <summary>DESIGN VALUE: the share of each turn's path time over which its bank eases from the loop's at the join and
        /// from level at the far end, so the bank never jumps.</summary>
        public const float BankEaseShare = 0.15f;

        /// <summary>Real seconds from the pass's end to the turn's far end (the warp's inverse at <see cref="TurnS"/>).</summary>
        public static float TurnRealS => (TurnS + (Speedup - 1f) * 0.5f * RampS) / Speedup;

        /// <summary>Seconds from <see cref="Start"/> to the middle of the lead's pass over Tel Aviv.</summary>
        public static float ArriveS => PassS + TurnRealS;

        /// <summary>How long the raid lasts (s): until the wing reaches the end of its way out.</summary>
        public static float Seconds => 2f * ArriveS + TrailS;

        /// <summary>Aircraft <paramref name="plane"/>'s raid time (0 the lead, 1 its wing) <paramref name="seconds"/> after the
        /// raid starts.</summary>
        public static float U(float seconds, int plane) => seconds - ArriveS - plane * TrailS;

        /// <summary>The seconds after the raid starts when aircraft <paramref name="plane"/> is at raid time <paramref name="u"/>.</summary>
        public static float SecondsAt(float u, int plane) => u + ArriveS + plane * TrailS;

        /// <summary>The path time at raid time <paramref name="u"/>: u on the pass; beyond it, faster by <see cref="Speedup"/>
        /// after a smooth ramp of <see cref="RampS"/> (the pace's rise is a smoothstep, so this is its integral).</summary>
        public static float PathTime(float u)
        {
            float r = Mathf.Abs(u) - PassS;
            if (r <= 0f) return u;
            float g;
            if (r < RampS) { float y = r / RampS; g = r + (Speedup - 1f) * RampS * (y * y * y - 0.5f * y * y * y * y); }
            else g = r + (Speedup - 1f) * (r - 0.5f * RampS);
            return Mathf.Sign(u) * (PassS + g);
        }

        /// <summary>Where an aircraft is at path time <paramref name="tau"/>, from its loop's centre (world x, z, m), as
        /// SeaAirCollection.DakotaLoopOffset: the loop itself on the pass; a cubic Hermite curve from the start to the pass and
        /// from the pass to the end, at the loop's speed and heading where it joins; a straight line before the start and
        /// after the end.</summary>
        public static Vector2 Offset(float tau)
        {
            if (Mathf.Abs(tau) <= PassS) return SeaAirCollection.DakotaLoopOffset(tau);
            Side(tau, out var at, out _, out _, out _, out _);
            return at;
        }

        /// <summary>The aircraft's turn at path time <paramref name="tau"/>, as SeaAirCollection.DakotaTurn: on the pass, the
        /// loop's own; off it, a yaw to the path's heading (<paramref name="forward"/>) and a bank (<paramref name="bank"/>,
        /// positive for a left turn, its left wing down) from the path's curvature seen from above, as the loop's
        /// (SeaAirCollection.BankFor at the Dakotas' scale), signed by the turn's side and eased in at each end of a turn
        /// (<see cref="BankEaseShare"/>).</summary>
        public static Quaternion Turn(float tau, out Vector3 forward, out float bank)
        {
            if (Mathf.Abs(tau) <= PassS) return SeaAirCollection.DakotaTurn(tau, out forward, out bank);
            Side(tau, out _, out var velocity, out var accel, out float toJoin, out float toFar);
            var heading = velocity.normalized;
            forward = new Vector3(heading.x, 0f, heading.y);
            var yaw = Quaternion.Euler(0f, -Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg, 0f);
            float speed = velocity.magnitude, cross = velocity.x * accel.y - velocity.y * accel.x;
            float curvature = speed > 1f ? cross / (speed * speed * speed) : 0f;
            float curve = Mathf.Sign(curvature) * SeaAirCollection.BankFor(SeaAirCollection.DakotaLoopScale * Mathf.Abs(curvature));
            float join = SeaAirCollection.DakotaBankDegrees(tau < 0f ? -PassS : PassS);
            float far = Mathf.SmoothStep(0f, 1f, toFar / BankEaseShare), near = Mathf.SmoothStep(0f, 1f, toJoin / BankEaseShare);
            bank = Mathf.Lerp(join, curve * far, near);
            return Quaternion.AngleAxis(bank, forward) * yaw;
        }

        /// <summary>The path off the pass at path time <paramref name="tau"/>: where it is, its velocity and acceleration (per
        /// path second), and how far (share of the turn) it is from the pass's join and from the turn's far end.</summary>
        static void Side(float tau, out Vector2 at, out Vector2 velocity, out Vector2 accel, out float toJoin, out float toFar)
        {
            var telAviv = SeaAirCollection.DakotaLoopOffset(0f);
            float speed = LoopVelocity(PassS).magnitude;
            Vector2 p0, v0, p1, v1; float s;
            if (tau < 0f)
            {
                p0 = telAviv + FromM; v0 = Bearing(FromBearing) * speed;
                p1 = SeaAirCollection.DakotaLoopOffset(-PassS); v1 = LoopVelocity(-PassS);
                s = (tau + PassS + TurnS) / TurnS;
                toJoin = 1f - s; toFar = s;
            }
            else
            {
                p0 = SeaAirCollection.DakotaLoopOffset(PassS); v0 = LoopVelocity(PassS);
                p1 = telAviv + ToM; v1 = Bearing(ToBearing) * speed;
                s = (tau - PassS) / TurnS;
                toJoin = s; toFar = 1f - s;
            }
            if (s < 0f) { at = p0 + v0 * (s * TurnS); velocity = v0; accel = Vector2.zero; return; }
            if (s > 1f) { at = p1 + v1 * ((s - 1f) * TurnS); velocity = v1; accel = Vector2.zero; return; }
            float s2 = s * s, s3 = s2 * s, d = TurnS;
            at = (2f * s3 - 3f * s2 + 1f) * p0 + (s3 - 2f * s2 + s) * d * v0 + (-2f * s3 + 3f * s2) * p1 + (s3 - s2) * d * v1;
            velocity = ((6f * s2 - 6f * s) * p0 + (3f * s2 - 4f * s + 1f) * d * v0 + (-6f * s2 + 6f * s) * p1 + (3f * s2 - 2f * s) * d * v1) / d;
            accel = ((12f * s - 6f) * p0 + (6f * s - 4f) * d * v0 + (-12f * s + 6f) * p1 + (6f * s - 2f) * d * v1) / (d * d);
        }

        /// <summary>The loop's velocity (world x, z, m a second) <paramref name="seconds"/> into it: the derivative of
        /// SeaAirCollection.DakotaLoopOffset.</summary>
        static Vector2 LoopVelocity(float seconds)
        {
            float w = 2f * Mathf.PI / SeaAirCollection.DakotaLoopSeconds, a = w * seconds;
            return new Vector2(SeaAirCollection.DakotaLoopHalfLengthM * w * Mathf.Cos(a), SeaAirCollection.DakotaLoopHalfWidthM * w * Mathf.Sin(a));
        }

        /// <summary>A unit direction (world x east, z north) at <paramref name="degrees"/> clockwise from north.</summary>
        static Vector2 Bearing(float degrees) => new Vector2(Mathf.Sin(degrees * Mathf.Deg2Rad), Mathf.Cos(degrees * Mathf.Deg2Rad));

        static float? startedAt;

        /// <summary>True from <see cref="Start"/> until <see cref="Stop"/>.</summary>
        public static bool Flying => startedAt.HasValue;

        /// <summary>The seconds since the raid started, when it is flying.</summary>
        public static bool Since(out float seconds)
        {
            seconds = startedAt.HasValue ? SpitfireLoop.Clock() - startedAt.Value : 0f;
            return startedAt.HasValue;
        }

        /// <summary>True once the raid has flown its <see cref="Seconds"/>, or when it is not flying.</summary>
        public static bool Over => !Since(out float seconds) || seconds >= Seconds;

        /// <summary>Starts the raid now, when a Dakota is in the scene (they stream in, and fly only on their dates,
        /// SeaAirCollection.DakotaFrom); false, and nothing flies, when none is.</summary>
        public static bool Start()
        {
            if (Object.FindAnyObjectByType<DakotaLoop>() == null) return false;
            startedAt = SpitfireLoop.Clock();
            return true;
        }

        /// <summary>Ends the raid: the Dakotas go back to their loop's clock at their next frame. Safe to call at any time.</summary>
        public static void Stop() => startedAt = null;
    }
}
