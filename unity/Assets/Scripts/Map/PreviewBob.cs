#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>A preview prop's gentle bob (PreviewProps): a sine rise and fall with a slight roll and pitch about the rest
    /// pose Init records, driven by Time.time and a fixed phase, so two runs move alike. Display only, never the
    /// simulation. Play mode only: in edit mode (tests, captures) Update does not run, and its guard keeps it so.</summary>
    [DisallowMultipleComponent]
    public sealed class PreviewBob : MonoBehaviour
    {
        /// <summary>Puts the pitch a quarter-turn behind the roll, so the hull rocks rather than tips on one axis.</summary>
        const float PitchLag = Mathf.PI / 2f;

        Vector3 rest; Quaternion restTurn;

        public float AmplitudeM { get; private set; }
        public float PeriodS { get; private set; }
        public float RollDeg { get; private set; }
        /// <summary>Fraction of a period, in [0, 1).</summary>
        public float Phase { get; private set; }

        /// <summary>Records the current local pose as the rest pose; call it after the prop is placed.</summary>
        public void Init(float amplitudeM, float periodS, float rollDeg, float phase)
        {
            AmplitudeM = amplitudeM; PeriodS = periodS; RollDeg = rollDeg; Phase = phase;
            rest = transform.localPosition; restTurn = transform.localRotation;
        }

        void Update()
        {
            if (!Application.isPlaying || PeriodS <= 0f) return;
            float a = 2f * Mathf.PI * (Time.time / PeriodS + Phase);
            transform.localPosition = rest + Vector3.up * (AmplitudeM * Mathf.Sin(a));
            transform.localRotation = restTurn * Quaternion.Euler(RollDeg / 2f * Mathf.Sin(a + PitchLag), 0f, RollDeg * Mathf.Sin(a));
        }
    }
}
