#nullable enable
using System.IO;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The town models in <see cref="TownModel"/> order: each one's full mesh (MATKOT's), its material, and three
    /// lighter levels of detail baked from the full mesh by the editor (TownModelAssets, with Unity's Mesh LOD generator) and
    /// kept as sub-assets of this list. They are separate meshes, not Mesh LOD ranges of one mesh: a WebGL build drew the
    /// full mesh whatever RenderParams.forceMeshLod asked for (measured 16 October 2026, issue #659). The map scene's
    /// MapScene references this list, so the models load with the map and never through a Resources folder (owner,
    /// 16 October 2026: "No, load with the map").</summary>
    public sealed class TownModelSet : ScriptableObject
    {
        public Mesh[] meshes = System.Array.Empty<Mesh>();
        public Material[] materials = System.Array.Empty<Material>();
        /// <summary>The baked levels of detail: about <see cref="MidTriangles"/>, <see cref="FarTriangles"/> and the
        /// generator's last level (about 64-140 triangles).</summary>
        public Mesh[] mid = System.Array.Empty<Mesh>(), far = System.Array.Empty<Mesh>(), farthest = System.Array.Empty<Mesh>();

        public const int MidTriangles = 800, FarTriangles = 200;

        public Mesh Mesh(TownModel m) => meshes[(int)m];
        public Material Material(TownModel m) => materials[(int)m];

        /// <summary>The mesh for detail level <paramref name="level"/> (TownModelLayer.LodFor: each level halves the size on
        /// screen): the full mesh at levels 0-1, mid at 2-3, far at 4-5, farthest beyond.</summary>
        public Mesh Mesh(TownModel m, int level) =>
            level <= 1 ? meshes[(int)m] : level <= 3 ? mid[(int)m] : level <= 5 ? far[(int)m] : farthest[(int)m];

        /// <summary>The deepest level <see cref="Mesh(TownModel, int)"/> tells apart.</summary>
        public const int Levels = 7;

        /// <summary>Why the list is incomplete (a model without its mesh, its material or one of its three baked levels), or
        /// null when every model has all of them.</summary>
        public string? Missing()
        {
            int n = System.Enum.GetValues(typeof(TownModel)).Length;
            if (meshes.Length != n || materials.Length != n || mid.Length != n || far.Length != n || farthest.Length != n)
                return $"{meshes.Length} meshes, {materials.Length} materials and {mid.Length}/{far.Length}/{farthest.Length} baked levels, want {n} of each";
            for (int i = 0; i < n; i++)
            {
                string? missing = meshes[i] == null ? "mesh" : materials[i] == null ? "material" : mid[i] == null ? "mid level"
                    : far[i] == null ? "far level" : farthest[i] == null ? "farthest level" : null;
                if (missing != null) return $"{(TownModel)i} has no {missing}";
            }
            return null;
        }

        /// <summary>Throws unless the list is complete (<see cref="Missing"/>): the editor's bake refuses to save an
        /// incomplete list.</summary>
        public void Validate()
        {
            if (Missing() is string why) throw new InvalidDataException("town models: " + why);
        }
    }
}
