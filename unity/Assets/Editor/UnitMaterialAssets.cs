using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Creates the unit materials in Resources (UnitMaterials). Idempotent.
    /// Run headless: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.UnitMaterialAssets.Create</summary>
    public static class UnitMaterialAssets
    {
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(UnitMaterials.Folder)) AssetDatabase.CreateFolder("Assets/Resources", "Units");
            // Painted miniatures: URP Lit, nearly matte (a satin varnish's faint sheen); the base map is set at runtime.
            var paint = Make(UnitMaterials.MiniatureResource, "Universal Render Pipeline/Lit", Color.white);
            paint.SetFloat("_Smoothness", 0.18f); paint.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(paint);
            Make(UnitMaterials.RingResource, "Statecraft/HexSelection", MapScene.SelectionColour);
            AssetDatabase.SaveAssets();
        }

        static Material Make(string resource, string shaderName, Color colour)
        {
            var path = "Assets/Resources/" + resource + ".mat";
            var shader = Shader.Find(shaderName) ?? throw new System.InvalidOperationException($"shader not found: {shaderName}"); // editor only
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            else mat.shader = shader;
            mat.enableInstancing = true;
            mat.color = colour;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
