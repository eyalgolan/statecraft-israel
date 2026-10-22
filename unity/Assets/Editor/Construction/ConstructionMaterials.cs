#nullable enable
using Statecraft.Game.Map.Construction;
using UnityEditor;
using UnityEngine;

namespace Statecraft.Editor.Construction
{
    /// <summary>Gives the construction reveal its Dust and Stone template materials in the editor. They live under
    /// Assets/Editor, so player builds carry none of them until a gameplay caller loads them behind the opt-in.</summary>
    [InitializeOnLoad]
    static class ConstructionMaterials
    {
        public const string Folder = "Assets/Editor/Construction/Materials/";
        static ConstructionMaterials() =>
            ConstructionReveal.Templates = name => AssetDatabase.LoadAssetAtPath<Material>(Folder + name + ".mat");
    }
}
