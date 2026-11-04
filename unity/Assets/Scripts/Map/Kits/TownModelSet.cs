#nullable enable
using System.IO;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The Editor/test town-model list: each model's full original mesh and material (the near tier, #1273; mid is an
    /// alias of the same mesh, kept for the hex-tile sandbox's "@mid" ids) and its far-off copy with that copy's material (the
    /// far tier, #2378: FarModelLevels). In play, TownModelLayer assembles an equivalent list from individually streamed
    /// prefabs; Map.unity has no reference to the asset, so these are outside the initial download.</summary>
    public sealed class TownModelSet : ScriptableObject
    {
        public Mesh[] meshes = System.Array.Empty<Mesh>();
        public Material[] materials = System.Array.Empty<Material>();
        /// <summary>An alias of <see cref="meshes"/>: no baked lighter level near (#1273).</summary>
        public Mesh[] mid = System.Array.Empty<Mesh>();
        /// <summary>The far-off copies and their materials (owner, 3 November 2026: "Ask Tal; allow them far off only").</summary>
        public Mesh[] far = System.Array.Empty<Mesh>();
        public Material[] farMaterials = System.Array.Empty<Material>();

        public Mesh Mesh(TownModel m) => meshes[(int)m];
        public Material Material(TownModel m) => materials[(int)m];
        public Mesh FarMesh(TownModel m) => far[(int)m];
        public Material FarMaterial(TownModel m) => farMaterials[(int)m];

        /// <summary>Why the list is incomplete (a model without its mesh, its material, its mid alias, its far-off copy or
        /// that copy's material), or null when every model has all of them.</summary>
        public string? Missing()
        {
            int n = System.Enum.GetValues(typeof(TownModel)).Length;
            if (meshes.Length != n || materials.Length != n || mid.Length != n || far.Length != n || farMaterials.Length != n)
                return $"{meshes.Length} meshes, {materials.Length} materials, {mid.Length} mid levels and {far.Length}/{farMaterials.Length} far copies/materials, want {n} of each";
            for (int i = 0; i < n; i++)
            {
                string? missing = meshes[i] == null ? "mesh" : materials[i] == null ? "material" : mid[i] == null ? "mid level"
                    : far[i] == null ? "far mesh" : farMaterials[i] == null ? "far material" : null;
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
