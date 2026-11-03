#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The C06 point-fire pose source (#2084; owner, 2 November 2026: "Yes, for firing units (Recommended)"): the root
    /// of the prefab that streams in Tal Heuberger's point-fire clip, unchanged from their export. Its rig's bones have the
    /// same names and the same rest mesh as the C06 young recruit's, but its exporter gave them other local axes, so a bone
    /// sampled from the clip is turned into the C06 rig's frame by <see cref="Correction"/>: for each bone, the export's
    /// bind pose times its mesh's world-from-local, times the C06 mesh's local-from-world and its inverse bind pose
    /// (CollectionImport.MakeFigurePosePrefab writes them from the two meshes). The prefab holds no mesh or texture.</summary>
    public sealed class UnitFigurePoseSource : MonoBehaviour
    {
        /// <summary>Tal Heuberger's 97-frame C06_POINT_FIRE_PROVISIONAL_REVIEW_97F stack, as imported.</summary>
        public AnimationClip? Clip;
        /// <summary>The held aim: a frame inside the clip's still aim (frames 25 to 65 of Tal's contact sheet), in seconds.</summary>
        public float AimTime;
        /// <summary>The model the corrections are for (the C06 young recruit).</summary>
        public string Figure = "";
        /// <summary>The bones (names under the figure's Armature, parents before children) and each one's correction.</summary>
        public string[] Bones = System.Array.Empty<string>();
        public Matrix4x4[] Correction = System.Array.Empty<Matrix4x4>();
    }
}
