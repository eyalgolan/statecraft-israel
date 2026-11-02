#nullable enable
using System;
using UnityEngine;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Flies Tal Heuberger's Egyptian Spitfire round its loop (SeaAirCollection.LoopOffset, owner, 2 November 2026:
    /// "Fly a slow loop"), level, nose along the path. Presentation only: it reads the map's animation clock (Time.time, as
    /// the steam train and the showcase flight do), sets one transform per frame and allocates nothing. It lives on the
    /// aircraft's own object, so it stops when the layer drops the aircraft (its dates end, or the map is torn down) and
    /// does not run while the layer is inactive. The model is one fused mesh with no separate propeller, so nothing spins.
    /// Editor renders and screenshots (`?shot` / `-shot`) hold the loop's start, so they repeat exactly, as the train does
    /// (RouteLayer.ModeFor).</summary>
    public sealed class SpitfireLoop : MonoBehaviour
    {
        Vector3 centre, offset; Quaternion level; bool still;

        /// <summary>STUB (red stage).</summary>
        public const float PropDegreesPerSecond = 1500f, ShadowLiftM = 30f;
        public static readonly Vector3 SunRay = Quaternion.Euler(MapLighting.SunElevation, MapLighting.SunAzimuth - 180f, 0f) * Vector3.forward;
        internal void Attach(Transform? prop, Transform? shadow, Transform? flag, Terrain? ground, Vector2 shadowHalf) { }

        /// <summary>The animation clock, Time.time. Test seam (internal; the tests see it through InternalsVisibleTo): an
        /// EditMode test has no frame clock.</summary>
        internal static Func<float> Clock = () => Time.time;

        /// <summary>True when the aircraft holds the loop's start (edit mode, screenshots).</summary>
        public bool Still => still;

        /// <summary><paramref name="centre"/>: the loop's centre in the air. <paramref name="level"/>: the model's rotation
        /// with its nose east. <paramref name="offset"/>: its drawn centre from its pivot at that rotation.</summary>
        internal void Configure(Vector3 centre, Quaternion level, Vector3 offset, bool still)
        {
            this.centre = centre; this.level = level; this.offset = offset; this.still = still;
            // Where the clock puts it now: a component added mid-frame gets no Update that frame (review k2 C1).
            Place(still ? 0f : Clock());
        }

        void Update()
        {
            if (!still) Place(Clock());
        }

        /// <summary>Puts the aircraft where it is <paramref name="seconds"/> into its loop, its drawn centre on the path and
        /// its nose along the path's tangent.</summary>
        public void Place(float seconds)
        {
            var at = SeaAirCollection.LoopOffset(seconds); var heading = SeaAirCollection.LoopHeading(seconds);
            // A turn about the vertical from east (+x) to the heading; Unity's yaw turns +x toward -z.
            var yaw = Quaternion.Euler(0f, -Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg, 0f);
            transform.localRotation = yaw * level;
            transform.position = centre + new Vector3(at.x, 0f, at.y) - yaw * offset;
        }
    }
}
