#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Flies its object round a horizontal circle, banked into the turn, nose along the path; turns the propeller.
    /// With a still phase it holds that point (and the propeller its angle), so screenshots repeat exactly.</summary>
    public sealed class ShowcaseFlight : MonoBehaviour
    {
        Vector3 centre; float radius, altitude, seconds, bank; Transform? propeller; float? still;

        public void Configure(Vector3 centre, float radius, float altitude, float seconds, float bank, Transform? propeller, float? stillPhase)
        {
            this.centre = centre; this.radius = radius; this.altitude = altitude; this.seconds = seconds; this.bank = bank;
            this.propeller = propeller; still = stillPhase;
            Place(still ?? 0f);
        }

        public float? StillPhase => still;

        void Update()
        {
            if (still.HasValue) return;
            Place(Time.time / seconds % 1f);
            if (propeller != null) propeller.localRotation = Quaternion.Euler(0, 0, -Mathf.Repeat(Time.time * 1500f, 360f));
        }

        /// <summary>Puts the aircraft at phase <paramref name="t"/> (0..1) of the loop, flying anticlockwise seen from above.</summary>
        public void Place(float t)
        {
            float a = 2f * Mathf.PI * t;
            var pos = centre + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius; pos.y = altitude;
            var forward = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0, 0, bank));
        }
    }
}
