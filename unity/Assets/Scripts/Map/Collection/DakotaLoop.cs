#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Flies Tal Heuberger's Egyptian Dakota round its loop off Tel Aviv (SeaAirCollection.DakotaLoopOffset; owner, 2
    /// November 2026: "A slow loop over Tel Aviv on those turns, like the Spitfire's loop, with the Egyptian flag; purely
    /// visual"), level, nose along the path, Egypt's flag floating over its tail (owner on #2260, "Smaller flag, no pole").
    /// Presentation only: it reads the map's animation clock (SpitfireLoop.Clock, Time.time), sets two transforms a frame and
    /// allocates nothing. It lives on the aircraft's own object, so it stops when the layer drops the aircraft. Editor renders
    /// and screenshots (`?shot` / `-shot`) hold the loop's start, as the train and the Spitfire do (RouteLayer.ModeFor).</summary>
    public sealed class DakotaLoop : MonoBehaviour
    {
        /// <summary>DESIGN VALUE: the gap (m) between the aircraft's box top and the flag's lower edge, as #2260's
        /// SpitfireLoop.FlagClearM.</summary>
        public const float FlagClearM = 150f;

        Vector3 centre, offset, half; Quaternion level; bool still; Transform? flag; float flagHalf;

        /// <summary>True when the aircraft holds the loop's start (edit mode, screenshots).</summary>
        public bool Still => still;

        /// <summary>The flag that floats over the tail, or null.</summary>
        public Transform? Flag => flag;

        /// <summary><paramref name="centre"/>: the loop's centre in the air. <paramref name="level"/>: the model's rotation with
        /// its nose east. <paramref name="offset"/>: its drawn centre from its pivot at that rotation. <paramref name="half"/>: half
        /// its box at that rotation (half its length, height and span).</summary>
        internal void Configure(Vector3 centre, Quaternion level, Vector3 offset, Vector3 half, Transform? flag, bool still)
        {
            this.centre = centre; this.level = level; this.offset = offset; this.half = half; this.flag = flag; this.still = still;
            flagHalf = flag != null ? flag.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y * flag.localScale.y : 0f;
            // Where the clock puts it now: a component added mid-frame gets no Update that frame.
            Place(still ? 0f : SpitfireLoop.Clock());
        }

        void Update()
        {
            if (!still) Place(SpitfireLoop.Clock());
        }

        /// <summary>Puts the aircraft where it is <paramref name="seconds"/> into its loop: its drawn centre on the path, its nose
        /// along the path's tangent; the flag over its tail, its lower edge <see cref="FlagClearM"/> over the aircraft's top.</summary>
        public void Place(float seconds)
        {
            var at = SeaAirCollection.DakotaLoopOffset(seconds); var heading = SeaAirCollection.DakotaLoopHeading(seconds);
            // A turn about the vertical from east (+x) to the heading; Unity's yaw turns +x toward -z.
            var yaw = Quaternion.Euler(0f, -Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg, 0f);
            transform.localRotation = yaw * level;
            var drawn = centre + new Vector3(at.x, 0f, at.y);
            transform.position = drawn - yaw * offset;
            if (flag != null)
                flag.position = drawn - new Vector3(heading.x, 0f, heading.y) * half.x + Vector3.up * (half.y + FlagClearM + flagHalf);
        }
    }
}
