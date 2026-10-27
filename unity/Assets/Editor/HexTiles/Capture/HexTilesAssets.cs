#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Statecraft.HexTiles.Capture
{
    /// <summary>Editor-made assets the hex-tile map needs in a player build: the material templates (tracked, in
    /// Resources/HexTiles/Materials, so their shaders and variants ship; the under-attack look's among them), and a local copy of the ground textures in the
    /// git-ignored prototype Resources (Poly Haven CC0 and our generated crop rows; local until their asset batch).
    /// Unity -batchmode ... -executeMethod Statecraft.HexTiles.Capture.HexTilesAssets.Run</summary>
    public static class HexTilesAssets
    {
        const string Materials = "Assets/Resources/" + HexMaterials.Folder;
        const string GroundSource = "Assets/Art/Settlements/Prototype/HexSandboxGround", GroundLocal = "Assets/Art/Settlements/Prototype/Resources/HexTiles/Ground";

        [MenuItem("Statecraft/Hex Tiles/Make Materials And Local Ground")]
        public static void Run()
        {
            Directory.CreateDirectory(Materials);
            foreach (var name in new[] { HexMaterials.Terrain, HexMaterials.TerrainHoles, HexMaterials.Lit, HexMaterials.Sea, HexMaterials.Edges })
            {
                var path = $"{Materials}/{name}.mat";
                var m = HexMaterials.New(name);   // no template yet: built from the shader with the right keywords
                if (name == HexMaterials.Lit) m.SetFloat("_Smoothness", 0.05f);
                AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(m, path);
            }
            // The under-attack look's (HexUnderAttack): always rebuilt from their shaders, so a settings change lands.
            foreach (var name in HexUnderAttack.Materials)
            {
                var path = $"{Materials}/{name}.mat";
                AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(HexMaterials.FromShader(name), path);
            }
            if (Directory.Exists(GroundSource))
            {
                Directory.CreateDirectory(GroundLocal);
                foreach (var f in Directory.GetFiles(GroundSource, "*.jpg")) File.Copy(f, Path.Combine(GroundLocal, Path.GetFileName(f)), true);
                AssetDatabase.Refresh();
                foreach (var f in Directory.GetFiles(GroundLocal, "*_nor_gl_2k.jpg"))
                    if (AssetImporter.GetAtPath(f) is TextureImporter ti && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("HEX TILES ASSETS materials and local ground ready");
        }
    }
}
