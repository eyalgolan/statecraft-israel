#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Collection;
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The C06 point-fire aim on an attacking token (#2084; owner, 2 November 2026: "Yes, for firing units
    /// (Recommended)": "Show the aiming pose on a unit token when it attacks"). Tal Heuberger's point-fire export is one
    /// figure, the C06 young recruit, so only a C06 figure takes it; every other figure keeps its own fire take. While its
    /// token's attack plays (UnitTokenMotion), each model the figure shows (far and near) holds one still frame of the clip,
    /// <see cref="UnitFigurePoseSource.AimTime"/>, with its Animator paused; when the attack ends, every bone it moved is put
    /// back exactly and the Animator resumes. The pose is computed once per figure model and then only copied, so holding
    /// it allocates nothing; it is the same frame in edit mode, in captures and in a player. It fails open: until the
    /// pose source arrives, and for good when its bundle is missing or damaged, the figure plays its fire take as before.</summary>
    [ExecuteAlways]
    public sealed class UnitFigurePointFire : MonoBehaviour
    {
        /// <summary>The pose source of figure <paramref name="figure"/>, or null: only C06 has one.</summary>
        public static string? PoseFor(string? figure) => figure == UnitFigures.YoungRecruit ? CollectionCatalog.C06PointFire : null;

        sealed class Held
        {
            public Transform Model = null!;
            public Transform[] Nodes = Array.Empty<Transform>();
            public Vector3[] Position = Array.Empty<Vector3>(), Scale = Array.Empty<Vector3>();
            public Quaternion[] Rotation = Array.Empty<Quaternion>();
            public Animator[] Animators = Array.Empty<Animator>();
            public bool[] WasEnabled = Array.Empty<bool>();
            public Renderer? Shown;   // a near model's first renderer (UnitFigureDetail shows it while the camera is close)
            public bool Near, On;
        }

        /// <summary>A figure model's held pose: its nodes' local positions, rotations and scales, by node order.</summary>
        sealed class Pose { public Vector3[] Position = null!, Scale = null!; public Quaternion[] Rotation = null!; }
        static readonly Dictionary<string, Pose> poses = new Dictionary<string, Pose>(StringComparer.Ordinal);

        string figure = "", source = "";
        UnitFigurePoseSource? pose;
        IDisposable? lease;
        float fireLength = -1f;
        bool holding;
        readonly List<Held> held = new List<Held>();

        /// <summary>True when the pose source has arrived and the figure shows a model: the attack can hold the aim.</summary>
        public bool Ready => pose != null && pose.Clip != null && transform.Find(UnitFigures.ModelName) != null;
        public bool Holding => holding;

        /// <summary>Gives figure <paramref name="piece"/> (dressed as <paramref name="figure"/>) the point-fire aim, if its
        /// model has a pose source.</summary>
        internal static void Equip(Transform piece, string? figure)
        {
            var id = PoseFor(figure);
            if (id == null || piece == null || piece.TryGetComponent<UnitFigurePointFire>(out _)) return;
            var fire = piece.gameObject.AddComponent<UnitFigurePointFire>();
            fire.figure = figure!; fire.source = id;
            void Answer(GameObject? arrived)
            {
                if (fire == null) return;   // the token was torn down meanwhile
                fire.pose = arrived != null ? arrived.GetComponent<UnitFigurePoseSource>() : null;   // null: fail open
                if (fire.pose != null && fire.pose.Figure != fire.figure) fire.pose = null;
                var motion = fire.GetComponentInParent<UnitTokenMotion>();
                if (motion != null && motion.Attacking) motion.RefreshPose();   // an attack under way takes the aim at once
            }
            fire.lease = UnitWeapons.Request(id, Answer);
        }

        /// <summary>How long the aim holds: the figure's own fire take (an attack lasts as long as its longest take). Read once.</summary>
        internal float FireLength
        {
            get
            {
                if (fireLength >= 0f) return fireLength;
                var holder = transform.Find(UnitFigures.ModelName);
                var animator = holder != null ? holder.GetComponentInChildren<Animator>(true) : null;
                if (animator == null || animator.runtimeAnimatorController == null) return 0f;
                return fireLength = UnitTokenMotion.FireLength(animator);
            }
        }

        /// <summary>Holds the aim on every model the figure shows now (again after a model joins or the zoom switch woke an Animator).</summary>
        internal void Hold()
        {
            if (!Ready) return;
            holding = true;
            for (int i = held.Count - 1; i >= 0; i--) if (held[i].Model == null) held.RemoveAt(i);   // a model retired meanwhile
            var holder = transform.Find(UnitFigures.ModelName);
            for (int i = 0; i < holder.childCount; i++) HoldOn(holder.GetChild(i));
        }

        /// <summary>Puts every held model back as it was, bone for bone, and lets its Animator run again.</summary>
        internal void Release()
        {
            holding = false;
            foreach (var h in held)
            {
                if (!h.On || h.Model == null) continue;
                h.On = false;
                for (int i = 0; i < h.Nodes.Length; i++)
                {
                    var node = h.Nodes[i];
                    if (node == null) continue;
                    node.localPosition = h.Position[i]; node.localRotation = h.Rotation[i]; node.localScale = h.Scale[i];
                }
                // A near model's Animator runs while the camera is close (UnitFigureDetail shows its renderers then); a far
                // model's stays as it was.
                bool shown = h.Shown != null && !h.Shown.forceRenderingOff;
                for (int i = 0; i < h.Animators.Length; i++)
                    if (h.Animators[i] != null) h.Animators[i].enabled = h.Near ? shown : h.WasEnabled[i];
            }
        }

        /// <summary>A model that joins the figure (UnitFigures.Swap, UnitFigureDetail.Attach) takes a held aim at once.</summary>
        internal static void Refresh(Transform piece)
        {
            if (piece != null && piece.TryGetComponent<UnitFigurePointFire>(out var fire) && fire.holding) fire.Hold();
        }

        void HoldOn(Transform model)
        {
            Held? h = null;
            foreach (var x in held) if (x.Model == model) { h = x; break; }   // no lambda: holding allocates nothing
            if (h == null)
            {
                h = Track(model);
                if (h == null) return;
                held.Add(h);
            }
            if (!h.On)
            {
                for (int i = 0; i < h.Nodes.Length; i++)
                { h.Position[i] = h.Nodes[i].localPosition; h.Rotation[i] = h.Nodes[i].localRotation; h.Scale[i] = h.Nodes[i].localScale; }
                for (int i = 0; i < h.Animators.Length; i++) h.WasEnabled[i] = h.Animators[i].enabled;
                h.On = true;
            }
            foreach (var animator in h.Animators) animator.enabled = false;   // a running Animator would write over the aim
            if (!poses.TryGetValue(figure, out var p)) poses[figure] = p = Compute(h);
            for (int i = 0; i < h.Nodes.Length; i++)
            { h.Nodes[i].localPosition = p.Position[i]; h.Nodes[i].localRotation = p.Rotation[i]; h.Nodes[i].localScale = p.Scale[i]; }
        }

        /// <summary>The model's Armature and the source's bones, found once; null when the model lacks one of them.</summary>
        Held? Track(Transform model)
        {
            var armature = UnitWeapons.FindDeep(model, "Armature");
            if (armature == null) return null;
            var nodes = new Transform[pose!.Bones.Length + 1];
            nodes[0] = armature;
            for (int i = 0; i < pose.Bones.Length; i++)
                if ((nodes[i + 1] = UnitWeapons.FindDeep(armature, pose.Bones[i])!) == null) return null;
            var animators = model.GetComponentsInChildren<Animator>(true);
            return new Held
            {
                Model = model, Nodes = nodes, Animators = animators, WasEnabled = new bool[animators.Length],
                Near = model.name == UnitFigureDetail.NearName, Shown = model.GetComponentInChildren<Renderer>(true),
                Position = new Vector3[nodes.Length], Rotation = new Quaternion[nodes.Length], Scale = new Vector3[nodes.Length],
            };
        }

        /// <summary>The aim on a model of this figure, once: Tal's clip sampled at the aim, then each bone turned into the
        /// figure's own rig (Bc = Bp x correction, parents first), read back as local values. The model is left in the aim.</summary>
        Pose Compute(Held h)
        {
            pose!.Clip!.SampleAnimation(h.Model.gameObject, pose.AimTime);
            var target = new Matrix4x4[pose.Bones.Length];
            for (int i = 0; i < target.Length; i++) target[i] = h.Nodes[i + 1].localToWorldMatrix * pose.Correction[i];
            for (int i = 0; i < target.Length; i++)
            {
                var node = h.Nodes[i + 1];
                var local = node.parent.worldToLocalMatrix * target[i];
                node.localPosition = local.GetColumn(3); node.localRotation = local.rotation; node.localScale = local.lossyScale;
            }
            var p = new Pose { Position = new Vector3[h.Nodes.Length], Rotation = new Quaternion[h.Nodes.Length], Scale = new Vector3[h.Nodes.Length] };
            for (int i = 0; i < h.Nodes.Length; i++)
            { p.Position[i] = h.Nodes[i].localPosition; p.Rotation[i] = h.Nodes[i].localRotation; p.Scale[i] = h.Nodes[i].localScale; }
            return p;
        }

        /// <summary>Forgets the computed poses (tests).</summary>
        internal static void Forget() => poses.Clear();

        void OnDestroy() { lease?.Dispose(); lease = null; }
    }
}
