using Statecraft.Game.Map.Routes;
using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Creates the route materials in Resources (RouteLayer): Rail.mat, Road.mat and Path.mat, all Statecraft/Route.
    /// The rail draws first (queue 3000, roads 3001, paths 3002), so with the shader's stencil bit a railway stays on top
    /// of a road it crosses, and a road on top of a path (#2917). Idempotent.
    /// Run headless: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.RouteMaterialAssets.Create</summary>
    public static class RouteMaterialAssets
    {
        public const int RailQueue = 3000, RoadQueue = 3001, PathQueue = 3002;
        /// <summary>The roads' opacity: packed dirt over the ground, a little of the ground's colour showing through.</summary>
        public const float RoadOpacity = 0.9f;
        /// <summary>The paths' opacity (#2917): trodden dust, more of the ground showing through than on a road.</summary>
        public const float PathOpacity = 0.75f;

        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(RouteLayer.Folder)) AssetDatabase.CreateFolder("Assets/Resources", "Routes");
            Make(RouteLayer.RailMaterialResource, kind: 1f, opacity: 1f, RailQueue);
            Make(RouteLayer.RoadMaterialResource, kind: 0f, opacity: RoadOpacity, RoadQueue);
            Make(RouteLayer.PathMaterialResource, kind: 2f, opacity: PathOpacity, PathQueue, widthM: RouteLayer.PathWidthM);
            AssetDatabase.SaveAssets();
        }

        static void Make(string resource, float kind, float opacity, int queue, float widthM = 0f)
        {
            var path = "Assets/Resources/" + resource + ".mat";
            var shader = Shader.Find("Statecraft/Route") ?? throw new System.InvalidOperationException("shader not found: Statecraft/Route"); // editor only
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            else mat.shader = shader;
            mat.SetFloat("_Kind", kind);
            mat.SetFloat("_Opacity", opacity);
            if (widthM > 0f) mat.SetFloat("_WidthM", widthM);   // a path's far fade measures its pixels across the ribbon
            mat.renderQueue = queue;
            EditorUtility.SetDirty(mat);
        }
    }
}
