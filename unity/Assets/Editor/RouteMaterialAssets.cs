using Statecraft.Game.Map.Routes;
using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Creates the route materials in Resources (RouteLayer): Rail.mat and Road.mat, both Statecraft/Route. The
    /// rail draws first (queue 3000, roads 3001), so with the shader's stencil bit a railway stays on top of a road it
    /// crosses. Idempotent.
    /// Run headless: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.RouteMaterialAssets.Create</summary>
    public static class RouteMaterialAssets
    {
        public const int RailQueue = 3000, RoadQueue = 3001;
        /// <summary>The roads' opacity: packed dirt over the ground, a little of the ground's colour showing through.</summary>
        public const float RoadOpacity = 0.9f;

        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(RouteLayer.Folder)) AssetDatabase.CreateFolder("Assets/Resources", "Routes");
            Make(RouteLayer.RailMaterialResource, kind: 1f, opacity: 1f, RailQueue);
            Make(RouteLayer.RoadMaterialResource, kind: 0f, opacity: RoadOpacity, RoadQueue);
            AssetDatabase.SaveAssets();
        }

        static void Make(string resource, float kind, float opacity, int queue)
        {
            var path = "Assets/Resources/" + resource + ".mat";
            var shader = Shader.Find("Statecraft/Route") ?? throw new System.InvalidOperationException("shader not found: Statecraft/Route"); // editor only
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            else mat.shader = shader;
            mat.SetFloat("_Kind", kind);
            mat.SetFloat("_Opacity", opacity);
            mat.renderQueue = queue;
            EditorUtility.SetDirty(mat);
        }
    }
}
