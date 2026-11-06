#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map.Kits;
using UnityEditor;
using UnityEngine;

/// <summary>#2428: the town kits' one shared atlas. The kits stay one mesh per town with one material, so the atlas adds no draw;
/// these tests pin what makes it work: the material carries it, each face names its cell, a wall counts whole bays, and the cells
/// tile and carry no painted light. Surfaces are written as their alpha bytes (KitMesh.Facade).</summary>
public class KitAtlasTests
{
    const byte WhiteCity = 17, Stone = 34, Plaster = 102, RoofTile = 153, FlatRoof = 170;
    const string AtlasPath = "Assets/Art/Kits/kit_atlas.png";
    const int Cell = 512;
    static readonly Color32 Grey = new Color32(200, 200, 200, 255);

    /// <summary>The cells that are one tiling surface all over: no windows, so any drift across one would be painted light.</summary>
    static readonly int[] SolidCells = { 5, 6, 7, 8, 9, 12, 13 };

    static (Vector2 min, Vector2 max) Span(KitMesh km, int from, int to)
    {
        Vector2 lo = km.Uvs[from], hi = km.Uvs[from];
        for (int i = from; i < to; i++) { lo = Vector2.Min(lo, km.Uvs[i]); hi = Vector2.Max(hi, km.Uvs[i]); }
        return (lo, hi);
    }

    [Test]
    public void BothKitMaterialsCarryTheOneSharedAtlas()
    {
        var kit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kit.mat");
        var tree = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tree.mat");
        Assert.IsTrue(kit.HasProperty("_KitAtlas"), "Statecraft/Kit has an atlas slot");
        var atlas = kit.GetTexture("_KitAtlas") as Texture2DArray;
        Assert.IsNotNull(atlas, "Kit.mat holds a Texture2DArray");
        Assert.AreEqual(16, atlas!.depth, "the 4 x 4 sheet's cells");
        Assert.AreEqual(Cell, atlas.width);
        Assert.AreEqual(TextureWrapMode.Repeat, atlas.wrapMode, "every cell tiles");
        Assert.Greater(atlas.mipmapCount, 5, "the mipmaps fade the windows to their mean far off");
        Assert.AreSame(atlas, tree.GetTexture("_KitAtlas"), "one texture for both materials");
    }

    [Test]
    public void AWallCountsWholeBaysSoACornerNeverCutsAWindow()
    {
        foreach (var (surface, bay) in new[] { ((Facade)WhiteCity, 3f), ((Facade)Stone, 4f) })
            foreach (float wide in new[] { 13f, 21.7f, 24f })
            {
                var km = new KitMesh();
                km.Box(0f, 0f, 20f, wide * 60f, 10f * 60f, -60f, 0f, 3.3f * 60f * 4f, Grey, Grey, surface, 60f, roofed: true);
                var (min, max) = Span(km, 0, 4);   // the first wall: wide across
                float bays = (max.x - min.x) * 12f / bay;
                Assert.AreEqual(Mathf.Round(bays), bays, 1e-3f, $"{surface} {wide} m: {bays} bays");
                Assert.AreEqual(Mathf.Round(wide / bay), bays, 1e-3f, "the nearest whole count, not a stretch by a bay or more");
                Assert.AreEqual(4f, max.y, 1e-3f, "v counts storeys");
            }
    }

    [Test]
    public void ARoofFaceNamesItsCellAndRunsInRealMetres()
    {
        var box = new KitMesh();
        box.Box(0f, 0f, 0f, 3200f, 1600f, 0f, 0f, 400f, Grey, Grey, (Facade)Stone, 100f);
        var (lo, hi) = Span(box, 16, 20);
        Assert.AreEqual(FlatRoof, box.Colours[16].a, "a flat roof is the limed terrace's cell");
        Assert.AreEqual(2f, hi.x - lo.x, 1e-3f, "32 real m at 16 m a cell");
        Assert.AreEqual(1f, hi.y - lo.y, 1e-3f);

        var gable = new KitMesh();
        gable.Gable(0f, 0f, 0f, 800f, 400f, 300f, 170f, Grey, Grey, 100f);
        Assert.AreEqual(RoofTile, gable.Colours[0].a, "a gable slope is tiled");
        Assert.AreEqual(8f * 1.06f / 4f, gable.Uvs[2].x - gable.Uvs[0].x, 1e-3f, "along the ridge, at 4 real m a cell");
        Assert.Greater(gable.Uvs[1].y - gable.Uvs[0].y, 0.5f, "up the slope, so the rows run with the eaves");
        Assert.AreEqual(Plaster, gable.Colours[8].a, "the gable end is a plain wall");
    }

    /// <summary>The ground storey's doors: a Tel Aviv block's ribbon windows start one storey up, and a parapet is its wall's
    /// plain surface, with no window rows through it.</summary>
    [Test]
    public void AWhiteCityParapetIsPlasterWithNoWindowRows()
    {
        var km = new KitMesh();
        km.Box(0f, 0f, 0f, 1200f, 900f, 0f, 0f, 400f, Grey, Grey, (Facade)WhiteCity, 60f, parapet: 60f, parapetThickness: 20f);
        Assert.AreEqual(Plaster, km.Colours[km.VertexCount - 1].a);
    }

    static Color32[] Sheet()
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
        Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(AtlasPath)), "the atlas decodes");
        Assert.AreEqual(4 * Cell, tex.width); Assert.AreEqual(4 * Cell, tex.height);
        var px = tex.GetPixels32();
        Object.DestroyImmediate(tex);
        return px;
    }

    /// <summary>Layer i of the sheet, as rows bottom up (LoadImage's GetPixels32 runs bottom up; layer 0 is the top left).</summary>
    static float[,] Luminance(Color32[] sheet, int layer)
    {
        int cx = layer % 4, cy = 3 - layer / 4;
        var l = new float[Cell, Cell];
        for (int y = 0; y < Cell; y++)
            for (int x = 0; x < Cell; x++)
            {
                var c = sheet[(cy * Cell + y) * 4 * Cell + cx * Cell + x];
                l[y, x] = (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
            }
        return l;
    }

    /// <summary>No lighting painted in: a sun side would show as one half of a plain cell brighter than the other, either way.</summary>
    [Test]
    public void ASolidCellHasNoPaintedLight()
    {
        var sheet = Sheet();
        foreach (int layer in SolidCells)
        {
            var l = Luminance(sheet, layer);
            float left = 0f, right = 0f, low = 0f, high = 0f;
            for (int y = 0; y < Cell; y++)
                for (int x = 0; x < Cell; x++)
                {
                    if (x < Cell / 2) left += l[y, x]; else right += l[y, x];
                    if (y < Cell / 2) low += l[y, x]; else high += l[y, x];
                }
            Assert.AreEqual(1f, left / right, 0.03f, $"layer {layer}: left against right");
            Assert.AreEqual(1f, low / high, 0.06f, $"layer {layer}: bottom against top");
        }
    }

    /// <summary>A solid cell's opposite edges match about as well as any two neighbouring rows inside it: the seams do not show.</summary>
    [Test]
    public void ASolidCellTilesWithNoSeam()
    {
        var sheet = Sheet();
        foreach (int layer in SolidCells)
        {
            var l = Luminance(sheet, layer);
            float seam = 0f, inside = 0f;
            for (int x = 0; x < Cell; x++)
            {
                seam += Mathf.Abs(l[0, x] - l[Cell - 1, x]) + Mathf.Abs(l[x, 0] - l[x, Cell - 1]);
                inside += Mathf.Abs(l[200, x] - l[201, x]) + Mathf.Abs(l[x, 200] - l[x, 201]);
            }
            Assert.LessOrEqual(seam, 2.5f * inside + 0.02f * 2 * Cell, $"layer {layer}: seam {seam}, inside {inside}");
        }
    }
}
