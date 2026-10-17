#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>One camera move of the scripted trailer capture (issue #45, MapCapture.Trailer): from one promo pose to another
    /// over <see cref="Seconds"/> at <see cref="Fps"/>, eased in and out. Pure arithmetic on the frame number, so the same
    /// frame always gets the same camera, whatever the machine's speed.</summary>
    public readonly struct TrailerShot
    {
        /// <summary>Steam takes 30 or 60 fps (docs/RELEASING.md); 60 keeps the slow moves smooth.</summary>
        public const int Fps = 60;

        public readonly string Name; public readonly PromoPose From, To; public readonly float Seconds;

        public TrailerShot(string name, PromoPose from, PromoPose to, float seconds)
        { Name = name; From = from; To = to; Seconds = seconds; }

        public int Frames => Mathf.RoundToInt(Seconds * Fps);

        /// <summary>The camera on frame <paramref name="frame"/> (0 to Frames - 1): the first frame is From, the last is To.
        /// Smoothstep easing, so a move neither starts nor stops with a jolt. The distance follows the eased curve in log
        /// terms (it falls evenly as a ratio), because a straight lerp from 250 km to 60 km would race through the high part
        /// and crawl at the end.</summary>
        public PromoPose PoseAt(int frame)
        {
            float t = Frames <= 1 ? 1f : Mathf.Clamp01(frame / (float)(Frames - 1));
            float e = t * t * (3f - 2f * t);
            return new PromoPose($"{Name}-{frame:D4}",
                From.Lon + (To.Lon - From.Lon) * e, From.Lat + (To.Lat - From.Lat) * e,
                (float)(From.Distance * System.Math.Pow((double)To.Distance / From.Distance, e)), Mathf.Lerp(From.Yaw, To.Yaw, e));
        }

        /// <summary>The default flight, from framings the art checkpoint already compares (PromoPose): the whole country held
        /// for two seconds, down to the Jaffa-Tel Aviv-Lydda ground of promo 03, and back up to the whole country for the
        /// title. 14 s, 840 frames. Each move eases in and out on its own, so the camera settles on every promo pose (one
        /// frame repeats at each join) before the next move: a beat for the cut, on purpose. Not the 6 km close checks: they
        /// are ground checks shot with the trees off, and in the first capture (2026-10-16) the move down to the Judean one ended
        /// with hillside and tree crowns filling the frame. A placeholder order for the owner to change: the trailer's shots are
        /// the owner's call (#45).</summary>
        public static IReadOnlyList<TrailerShot> Default { get; } = new[]
        {
            new TrailerShot("country", PromoPose.Diorama02, PromoPose.Diorama02, 2f),
            new TrailerShot("descent", PromoPose.Diorama02, PromoPose.Gameplay03, 6f),
            new TrailerShot("rise", PromoPose.Gameplay03, PromoPose.Diorama02, 6f),
        };
    }
}
