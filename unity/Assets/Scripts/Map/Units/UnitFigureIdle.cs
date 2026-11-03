#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>C05's idle, in code (#2374, part of #2007; owner, 3 November 2026: "do everything with the 0$ and make it as
    /// high quality as possible"). C05, the pioneer veteran, is the one token figure whose only idle take is a walk, so the
    /// token draws Tal Heuberger's original held still at the walk's first frame (OriginalModelPose; owner, 18 October 2026,
    /// #665: "Keep its still model"). Close up, where the other figures play their idle clips, this moves that held pose by
    /// small, slow turns, as the rigged idles do: the chest rises and sinks (breathing), the whole figure leans a little from
    /// side to side from its feet (a shift of weight, with the feet planted) and the head stays nearer level. Calmer than
    /// Tal's idle clips (their heads travel 3 to 4 percent of the figure's height; this about 2). Each figure starts at its own point in the
    /// cycle, from a hash of its token and place (<see cref="Phase"/>), so a trio never breathes in step, and the same figure
    /// gets the same offset after every rebuild. No clip, no file and no Animator: the Animator stays off, the bones' rest
    /// turns are kept and put back exactly when the figure turns far or is hidden. Sampling allocates nothing.</summary>
    [ExecuteAlways]
    public sealed class UnitFigureIdle : MonoBehaviour
    {
        /// <summary>The figure that idles this way: C05 only.</summary>
        public static bool Wanted(string? id) => id == UnitFigures.PioneerVeteran;

        /// <summary>A breath's period and a weight shift's, incommensurate so the two never line up for long.</summary>
        public const float BreathS = 3.7f, SwayS = 6.3f;
        /// <summary>The chest's pitch, the figure's lean and the head's counter-lean, in degrees, at the most.</summary>
        public const float BreathDeg = 2.5f, SwayDeg = 1.5f, HeadDeg = 0.6f;
        internal const string ArmatureNode = "Armature", ChestBone = "Chest", NeckBone = "Neck";

        Transform? chest, neck, frame;
        Transform? chestParent, neckParent;
        Quaternion rest, chestRest, neckRest;   // the model root's turn under its holder, and the bones' under their parents
        float phase;

        /// <summary>Where in the cycle this figure starts, 0 to 1.</summary>
        public float Offset => phase;

        /// <summary>The key a figure's offset is hashed from: its token and its place on it ("givati/Figure2").</summary>
        internal static string KeyOf(Transform piece) => piece.parent != null ? piece.parent.name + "/" + piece.name : piece.name;

        /// <summary>A figure's point in the cycle, 0 to 1, from its key (FNV-1a over the characters): the same key gives the
        /// same offset on every platform and after every rebuild.</summary>
        public static float Phase(string key)
        {
            uint hash = 2166136261u;
            foreach (char c in key) { hash ^= c; hash *= 16777619u; }
            return (hash % 10007u) / 10007f;
        }

        /// <summary>Gives <paramref name="model"/> (C05's streamed original, posed and under its holder) the idle, keyed by
        /// <paramref name="key"/>, off until the zoom switch turns it on. Null when the model lacks the chest or the neck: the
        /// figure then stands still, as before.</summary>
        internal static UnitFigureIdle? Equip(Transform model, string key)
        {
            if (model == null || model.parent == null || model.TryGetComponent<UnitFigureIdle>(out _)) return null;
            var armature = UnitWeapons.FindDeep(model, ArmatureNode);
            if (armature == null) return null;
            var chest = UnitWeapons.FindDeep(armature, ChestBone);
            var neck = UnitWeapons.FindDeep(armature, NeckBone);
            if (chest == null || neck == null) return null;
            var idle = model.gameObject.AddComponent<UnitFigureIdle>();
            idle.enabled = false;
            idle.chest = chest; idle.neck = neck; idle.frame = model.parent;
            idle.chestParent = chest.parent; idle.neckParent = neck.parent;
            idle.rest = model.localRotation; idle.chestRest = chest.localRotation; idle.neckRest = neck.localRotation;
            idle.phase = Phase(key);
            return idle;
        }

        /// <summary>The turns at <paramref name="t"/> seconds for a figure at <paramref name="phase"/>: the chest's pitch
        /// (breathing) and the figure's lean (weight) in degrees; the head leans back by <see cref="HeadDeg"/> over <see cref="SwayDeg"/>
        /// of the lean.</summary>
        public static void Evaluate(float t, float phase, out float breathDeg, out float swayDeg)
        {
            breathDeg = BreathDeg * Mathf.Sin(2f * Mathf.PI * (t / BreathS + phase));
            swayDeg = SwayDeg * Mathf.Sin(2f * Mathf.PI * (t / SwayS + phase * 3.3f + 0.37f));
        }

        /// <summary>Poses the figure as it stands <paramref name="t"/> seconds into the session: the same t gives the same
        /// pose. The lean is about the holder's forward axis through the model's root, which stands on the figure's feet
        /// (UnitFigures.Swap), so the feet stay planted and the held rifle leans with him; the chest's and the neck's turns
        /// are about the figure's own axes (its holder's right and forward), whatever the bones' frames. No allocation. A
        /// retired model (its bones gone) is left alone.</summary>
        public void Sample(float t)
        {
            if (chest == null || neck == null || frame == null) return;
            Evaluate(t, phase, out float breath, out float sway);
            transform.localRotation = Quaternion.AngleAxis(sway, Vector3.forward) * rest;
            var right = frame.right; var forward = frame.forward;
            chest.localRotation = Turn(chestParent!, right, breath) * chestRest;
            neck.localRotation = Turn(neckParent!, forward, -HeadDeg / SwayDeg * sway) * neckRest;
        }

        /// <summary>A turn of <paramref name="deg"/> about world axis <paramref name="axis"/>, as a bone under <paramref name="parent"/> applies it.</summary>
        static Quaternion Turn(Transform parent, Vector3 axis, float deg) => Quaternion.AngleAxis(deg, parent.InverseTransformDirection(axis));

        /// <summary>The bones back at rest, exactly.</summary>
        void Rest()
        {
            if (frame != null) transform.localRotation = rest;   // equipped: the rest turns were read
            if (chest != null) chest.localRotation = chestRest;
            if (neck != null) neck.localRotation = neckRest;
        }

        void LateUpdate() { if (Application.isPlaying) Sample(Time.time); }

        /// <summary>Turned off (far again, or the units hidden): the figure stands as it was held.</summary>
        void OnDisable() => Rest();
    }
}
