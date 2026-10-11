using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Creates the map's material assets (M0 correction 1: Shader.Find returns null in players, so materials are
    /// editor-made assets passed to MapBuilder.Build through serialized fields). Idempotent.
    /// Run headless: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.MapMaterials.Create</summary>
    public static class MapMaterials
    {
        public const string Folder = "Assets/Materials";
        public const string TerrainPath = Folder + "/Terrain.mat";
        public const string MarkerPath = Folder + "/SettlementMarker.mat";
        public const string SelectionPath = Folder + "/HexSelection.mat";
        public const string SeaPath = Folder + "/Sea.mat";
        public const string BorderLandPath = Folder + "/BorderLand.mat";
        public const string KitPath = Folder + "/Kit.mat";
        public const string TreePath = Folder + "/Tree.mat";

        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Materials");
            MapTextures.Configure();
            // The Statecraft terrain graph (tools/terrain_graph.py + StatecraftTerrain.hlsl) with the baked textures.
            var terrain = Make(TerrainPath, "Statecraft/StatecraftTerrain", null);
            terrain.SetTexture("_ScBakeTex", Tex(Statecraft.Game.Map.MapTexturePaths.Bake));
            terrain.SetTexture("_ScCoastTex", Tex(Statecraft.Game.Map.MapTexturePaths.Coast));
            terrain.SetTexture("_ScAoTex", Tex(Statecraft.Game.Map.MapTexturePaths.Ao));
            terrain.SetTexture("_ScLandTex", Tex(Statecraft.Game.Map.MapTexturePaths.Land));   // land use (tools/land_bake.py)
            EditorUtility.SetDirty(terrain);
            // Settlement boxes: matte URP Lit; SettlementPlacer sets the per-type _BaseColor through a MaterialPropertyBlock.
            var marker = Make(MarkerPath, "Universal Render Pipeline/Lit", Color.white);
            marker.SetFloat("_Smoothness", 0f); marker.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(marker);
            // The selected hex's outline: its own shader (Assets/Shaders/HexSelection.shader) with a depth offset (fix round 1, M2).
            Make(SelectionPath, "Statecraft/HexSelection", Statecraft.Game.Map.MapScene.SelectionColour);
            // The skirt's sea past the map's edge: the terrain's own water shading (StatecraftMap.hlsl ScWaterShade), so no seam.
            var sea = Make(SeaPath, "Statecraft/Sea", null);
            sea.SetTexture("_ScCoastTex", Tex(Statecraft.Game.Map.MapTexturePaths.Coast));
            EditorUtility.SetDirty(sea);
            // Neighbouring land past the map's edge (Sinai, Jordan, Lebanon): one muted land tone, lit and matte like the sea
            // (fix round 2, R2-1).
            var land = Make(BorderLandPath, "Universal Render Pipeline/Lit", Statecraft.Game.Map.MapBuilder.BorderLandColour);
            land.SetFloat("_Smoothness", 0f); land.SetFloat("_Metallic", 0f);
            land.SetFloat("_SpecularHighlights", 1f); land.SetFloat("_EnvironmentReflections", 1f);
            EditorUtility.SetDirty(land);
            // Settlement kits and trees (art spike): Assets/Shaders/Kit.shader. Buildings are merged static meshes (no tint
            // jitter: each building's colour is in its vertices); trees are GPU-instanced and get a per-instance tint.
            var kit = Make(KitPath, "Statecraft/Kit", Color.white);
            kit.SetFloat("_Jitter", 0f);
            EditorUtility.SetDirty(kit);
            var tree = Make(TreePath, "Statecraft/Kit", Color.white);
            tree.SetFloat("_Jitter", 0.16f); tree.SetFloat("_WindowDark", 0f);
            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
        }

        static Material Make(string path, string shaderName, Color? colour)
        {
            var shader = Shader.Find(shaderName); // editor only: fine here
            if (shader == null) throw new System.InvalidOperationException($"shader not found: {shaderName}");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            else mat.shader = shader;
            mat.enableInstancing = true;
            if (colour.HasValue) mat.color = colour.Value;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Texture2D Tex(string path) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(path) ?? throw new System.IO.FileNotFoundException("texture missing (tools/terrain_bake.py)", path);
    }
}
