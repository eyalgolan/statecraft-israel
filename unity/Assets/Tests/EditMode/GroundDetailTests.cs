#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>#2878: the ground detail pass over the terrain and the low 3D scrub (GroundDetail, GroundDetail.shader,
/// GroundScrub.shader, StatecraftGroundDetail.hlsl): where they draw, that they follow the map's data, and what they keep off.</summary>
public class GroundDetailTests
{
    [TearDown]
    public void Reset()
    {
        RenderSample.TiltShift(true);
        VisualToggles.SetAll(true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear();
    }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);   // the scene is not saved
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        return map;
    }

    static RenderSample At(MapScene map, Camera cam, double lon, double lat, float distance)
    {
        var rig = cam.GetComponent<CameraRig>();
        var at = map.Frame.ToWorld(lon, lat, 0);
        at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
        rig.LookAt = at; rig.Distance = distance; rig.Apply();
        return RenderSample.Render(cam, 960, 540);
    }

    [Test]
    public void ThePassDrawsRightAfterTheTerrainAndCastsNoShadow()
    {
        var map = OpenMap();
        var detail = map.GetComponentInChildren<GroundDetail>(true);
        Assert.IsNotNull(detail, "the map builds the ground detail");
        var renderer = detail.GetComponent<MeshRenderer>();
        Assert.Greater(renderer.sharedMaterial.renderQueue, map.Terrain.materialTemplate.renderQueue);
        Assert.Less(renderer.sharedMaterial.renderQueue, (int)UnityEngine.Rendering.RenderQueue.Geometry, "before the kits, trees and units");
        Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, renderer.shadowCastingMode);
        Assert.Contains(GroundDetail.Toggle, VisualToggles.Names.ToList());
        Assert.Contains(GroundDetail.ScrubToggle, VisualToggles.Names.ToList());
    }

    [Test]
    public void BothShadersCompileForWebGL2()
    {
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(Shader.Find("Statecraft/GroundDetail")));
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(Shader.Find("Statecraft/GroundScrub")));
    }

    [UnityTest]
    public IEnumerator TheHillsGainDetailAndTheSeaAndTheOverviewDoNot()
    {
        var map = OpenMap();
        RenderSample.TiltShift(false);
        yield return null;
        var cam = Camera.main;
        foreach (var n in new[] { "kits", "trees" }) if (VisualToggles.Names.Contains(n)) VisualToggles.Set(n, false);
        var hills = At(map, cam, 35.05, 31.85, 31000f);
        var sea = At(map, cam, 34.45, 32.40, 31000f);
        var overview = At(map, cam, PromoPose.Diorama02.Lon, PromoPose.Diorama02.Lat, PromoPose.Diorama02.Distance);
        VisualToggles.Set(GroundDetail.Toggle, false); VisualToggles.Set(GroundDetail.ScrubToggle, false);
        var bareHills = At(map, cam, 35.05, 31.85, 31000f);
        var bareSea = At(map, cam, 34.45, 32.40, 31000f);
        var bareOverview = At(map, cam, PromoPose.Diorama02.Lon, PromoPose.Diorama02.Lat, PromoPose.Diorama02.Distance);
        TestContext.WriteLine($"gradient {bareHills.GradientEnergy(0.25f, 0.75f):0.0000} -> {hills.GradientEnergy(0.25f, 0.75f):0.0000}");
        Assert.Greater(hills.GradientEnergy(0.25f, 0.75f), 1.3f * bareHills.GradientEnergy(0.25f, 0.75f), "the land between the towns gains texture at the opening zoom");
        Assert.AreEqual(bareHills.MeanSaturation(), hills.MeanSaturation(), 0.03f, "detail, not a new colour");
        Assert.AreEqual(bareSea.MeanLuma(0.3f, 0.3f, 0.7f, 0.7f), sea.MeanLuma(0.3f, 0.3f, 0.7f, 0.7f), 0.01f, "not on the open sea");
        Assert.AreEqual(bareOverview.MeanSaturation(), overview.MeanSaturation(), 0.002f, "gone by the overview");
    }

    /// <summary>Scrub draws for the camera at the opening zoom, and not under the opt-in hex-tile board, past the gameplay
    /// zoom, or with the terrain off.</summary>
    [UnityTest]
    public IEnumerator TheScrubDrawsOnlyWhereItCanRead()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var detail = map.GetComponentInChildren<GroundDetail>(true);
        At(map, cam, 35.05, 31.85, 31000f);
        Assert.Greater(detail.LastTiles, 0);
        Assert.Less(detail.LastTiles, 40, "a few tile instances, never a bush each");
        At(map, cam, PromoPose.Diorama02.Lon, PromoPose.Diorama02.Lat, PromoPose.Diorama02.Distance);
        Assert.AreEqual(0, detail.LastTiles, "not at the overview");
        var board = new GameObject(Statecraft.HexTiles.HexTilesLook.RootName);
        board.transform.SetParent(map.transform, false);
        At(map, cam, 35.05, 31.85, 31000f);
        Assert.AreEqual(0, detail.LastTiles, "not on the hex-tile board");
        Object.DestroyImmediate(board);
        map.Terrain.enabled = false;
        At(map, cam, 35.05, 31.85, 31000f);
        map.Terrain.enabled = true;
        Assert.AreEqual(0, detail.LastTiles, "not with the terrain off");
    }

    [Test]
    public void TheScrubFollowsTheBiomes()
    {
        Assert.Greater(GroundDetail.ClassOf("hills").WetScrub, GroundDetail.ClassOf("coastal_plain").WetScrub);
        Assert.Greater(GroundDetail.ClassOf("mountains").WetScrub, GroundDetail.ClassOf("valley").WetScrub);
        foreach (var water in new[] { "sea", "lake" })
        {
            var c = GroundDetail.ClassOf(water);
            Assert.AreEqual(0f, c.WetScrub + c.OpenScrub + c.Grain + c.Stones, water);
        }
        var desert = GroundDetail.ClassOf("desert");
        Assert.AreEqual(0f, desert.WetScrub + desert.OpenScrub, "no scrub on the desert");
        Assert.Greater(desert.Grain, 0f);
        var map = OpenMap();
        var px = GroundDetail.FillClasses(map.Frame, map.Hexes, out int nx, out int nz);
        Color32 At2(double lon, double lat)
        {
            var w = map.Frame.ToWorld(lon, lat, 0);
            return px[Mathf.RoundToInt(w.z / map.Frame.LengthM * (nz - 1)) * nx + Mathf.RoundToInt(w.x / map.Frame.WidthM * (nx - 1))];
        }
        Assert.Greater(At2(35.15, 31.80).r, At2(34.80, 32.00).r, "the Judean hills against the coastal plain");
        Assert.AreEqual(0, At2(34.40, 32.60).b, "nothing in the open sea");
        Assert.AreEqual(0, At2(34.90, 30.60).r, "nothing in the Negev");
    }

    [Test]
    public void TheScrubKeepsOffTheDiscsAndCorridors()
    {
        var map = OpenMap();
        var c = new Vector2(40000f, 90000f);
        var line = new List<Vector2> { new Vector2(60000f, 150000f), new Vector2(70000f, 150000f) };
        var px = GroundDetail.FillKeepOut(map.Frame, new[] { (c, 2000f) }, new[] { (line, 400f) });
        byte At(Vector2 p) => px[Mathf.FloorToInt(p.y / map.Frame.LengthM * GroundDetail.KeepOutHeight) * GroundDetail.KeepOutWidth
            + Mathf.FloorToInt(p.x / map.Frame.WidthM * GroundDetail.KeepOutWidth)];
        Assert.AreEqual(255, At(c)); Assert.AreEqual(255, At(c + Vector2.right * 1800f));
        Assert.AreEqual(0, At(c + Vector2.right * 2600f));
        Assert.AreEqual(255, At(new Vector2(65000f, 150000f)), "along a route");
        Assert.AreEqual(0, At(new Vector2(65000f, 151500f)));
    }

    [Test]
    public void ATownAndARouteAreInTheMapsKeepOut()
    {
        var map = OpenMap();
        var s = map.Settlements.First(x => x.Id == "tel_aviv");
        var w = map.Frame.ToWorld(s.Lon, s.Lat, 0);
        var line = map.KitRouteCorridors.First();
        var px = GroundDetail.FillKeepOut(map.Frame, map.SettlementFootprints, map.KitRouteCorridors);
        byte At(Vector2 p) => px[Mathf.FloorToInt(p.y / map.Frame.LengthM * GroundDetail.KeepOutHeight) * GroundDetail.KeepOutWidth
            + Mathf.FloorToInt(p.x / map.Frame.WidthM * GroundDetail.KeepOutWidth)];
        Assert.AreEqual(255, At(new Vector2(w.x, w.z)));
        Assert.AreEqual(255, At(line.line[line.line.Count / 2]));
    }

    /// <summary>The smallest unit base is a compact token's, its radius built as UnitLayer builds every Army base (the full
    /// trio at DrawSpread times the hex, times UnitGround.CompactScale); no bush's top reaches that base's lip.</summary>
    [Test]
    public void NoBushStandsThroughAUnitBase()
    {
        var map = OpenMap();
        float hex = map.Hexes.hex_size_km * 1000f;
        float smallestBase = UnitPlinth.Radius(UnitPlinth.TrioWidth(UnitShowcase.FigureOffsets(3), UnitShowcase.DrawSpread * hex * UnitGround.CompactScale));
        Assert.Less(GroundDetail.BushTopM, UnitPlinth.LipShare * smallestBase);
    }

    [Test]
    public void TheDetailTileIsSeededAndCarriesEveryChannel()
    {
        var a = GroundDetail.FillTexels(); var b = GroundDetail.FillTexels();
        CollectionAssert.AreEqual(a, b);
        var mean = new float[4];
        for (int i = 0; i < a.Length; i++) mean[i % 4] += a[i];
        for (int k = 0; k < 4; k++) mean[k] /= a.Length / 4f * 255f;
        Assert.AreEqual(0.5f, mean[0], 0.06f, "the grain darkens as much as it lightens");
        for (int k = 1; k < 4; k++) Assert.Greater(mean[k], 0.01f, "channel " + k);
    }

    [Test]
    public void TheScrubTileIsOneSmallMesh()
    {
        var mesh = GroundDetail.BuildScrubMesh();
        Assert.Less(mesh.vertexCount, 30000);
        Object.DestroyImmediate(mesh);
    }
}
