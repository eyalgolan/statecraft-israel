#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Flies its object round a horizontal circle, banked into the turn, nose along the path; turns the propeller;
    /// lays its soft shadow on the ground, when it has one, as the Egyptian Spitfire's (SpitfireLoop.LayShadow, from
    /// SpitfireLoop.ShadowHeightShare of its height). With a still phase it holds that point (and the propeller its angle), so
    /// screenshots repeat exactly.</summary>
    public sealed class ShowcaseFlight : MonoBehaviour
    {
        Vector3 centre; float radius, altitude, seconds, bank; Transform? propeller; float? still;
        Transform? shadow; Terrain? ground; float seaY; Vector2 shadowHalf;

        /// <summary><paramref name="shadow"/>, a child of this object, is laid on <paramref name="ground"/> or the sea's surface
        /// <paramref name="seaY"/>, <paramref name="shadowHalf"/> across and along the heading (m), when both are given.</summary>
        public void Configure(Vector3 centre, float radius, float altitude, float seconds, float bank, Transform? propeller, float? stillPhase,
            Transform? shadow = null, Terrain? ground = null, float seaY = float.NegativeInfinity, Vector2 shadowHalf = default)
        {
            this.centre = centre; this.radius = radius; this.altitude = altitude; this.seconds = seconds; this.bank = bank;
            this.propeller = propeller; still = stillPhase;
            this.shadow = shadow; this.ground = ground; this.seaY = seaY; this.shadowHalf = shadowHalf;
            Place(still ?? 0f);
        }

        public float? StillPhase => still;

        /// <summary>The propeller the flight turns (null: none).</summary>
        internal Transform? Propeller => propeller;

        /// <summary>Turns <paramref name="replacement"/> from now on in place of the configured propeller (StreamedAircraft: the
        /// streamed model's own, once it stands), at the angle the old one had, so a still frame keeps it.</summary>
        internal void TurnPropeller(Transform? replacement)
        {
            if (replacement != null && propeller != null) replacement.localRotation = propeller.localRotation;
            propeller = replacement;
        }

        /// <summary>The animation clock, Time.time. Test seam, as SpitfireLoop.Clock: an EditMode test has no frame clock.</summary>
        internal static System.Func<float> Clock = () => Time.time;

        void Update()
        {
            if (still.HasValue) return;
            float now = Clock();
            Place(now / seconds % 1f);
            if (propeller != null) propeller.localRotation = Quaternion.Euler(0, 0, -Mathf.Repeat(now * 1500f, 360f));
        }

        /// <summary>Puts the aircraft at phase <paramref name="t"/> (0..1) of the loop, flying anticlockwise seen from above.</summary>
        public void Place(float t)
        {
            float a = 2f * Mathf.PI * t;
            var pos = centre + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius; pos.y = altitude;
            var forward = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0, 0, bank));
            if (shadow != null && ground != null)
            {
                // Converging: the 101 Squadron's coast rises inland (SpitfireLoop.LayShadow). The shadow is this object's child,
                // and this object's scale is uniform, so its world size is LayShadow's over that scale.
                Collection.SpitfireLoop.LayShadow(shadow, ground, seaY, shadowHalf, pos, forward, bank, converge: true,
                    heightShare: Collection.SpitfireLoop.ShadowHeightShare);
                shadow.localScale /= transform.lossyScale.x;
            }
        }
    }
}
