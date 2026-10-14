using System.Collections;
using System.IO;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Issue #174 (integration bug B11): panning past the terrain's edge filled the view with flat fills and a hard
/// seam. Two causes: the look-at clamp let the view run off the map onto the border skirt (a flat slab; since #36 the frame), and at the high
/// north-east corner (the ground about 6 km up at 3x relief) the hero-zoom camera, 4.6 km above the look-at plane y = 0,
/// sat inside the hills and showed only haze. Rendered at the worst pan pose: the hero zoom pushed into each corner of the
/// map, through the same CameraController.Step the game runs every frame, with the bounds and ground GameRoot gives it.</summary>
public class MapEdgeTests
{
    const int W = 1600, H = 900;

    [SetUp] public void FreshScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    [TearDown] public void DiscardScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    /// <summary>The camera bounds as GameRoot sets them: the terrain's footprint (and CameraController.GroundOf and
    /// GroundTopOf its ground).</summary>
    static Rect Bounds(MapScene map)
    {
        var t = map.Terrain; var size = t.terrainData.size;
        return new Rect(t.transform.position.x, t.transform.position.z, size.x, size.z);
    }

    /// <summary>Where the renders are written (the before/after images for the issue): the project's Logs folder (git-ignored;
    /// Unity empties Temp when it quits).</summary>
    static string ShotDir => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "map-edge");

    [UnityTest]
    public IEnumerator PanningIntoACornerShowsNoSkirtSeamOrFlatFill()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var frame = map.Terrain.transform.Find(MapScene.FrameName).GetComponent<MeshRenderer>();
        var bounds = Bounds(map); var ground = CameraController.GroundOf(map.Terrain); var top = CameraController.GroundTopOf(map.Terrain);
        var rt = new RenderTexture(W, H, 24);
        Directory.CreateDirectory(ShotDir);
        RenderSample.TiltShift(false);   // a colour test: the blur spreads pixels across the frame
        // The water moves between the renders compared below (waves, breakers and, since sea round 2, the far sea's ripple),
        // which changed a few pixels of the north-west corner's coast at random (0-14 a run, skirt or not): its clock is
        // pinned, so the comparison sees only the frame.
        MapShaderGlobals.PinWaterTime(100f);
        try
        {
            cam.targetTexture = rt;      // the clamp measures the view at the render's aspect
            yield return null;
            var failures = new System.Collections.Generic.List<string>();
            foreach (var (name, corner, push) in new[]
            {
                ("ne", new Vector2(bounds.xMax, bounds.yMax), new Vector2(1, 1)), ("nw", new Vector2(bounds.xMin, bounds.yMax), new Vector2(-1, 1)),
                ("se", new Vector2(bounds.xMax, bounds.yMin), new Vector2(1, -1)), ("sw", new Vector2(bounds.xMin, bounds.yMin), new Vector2(-1, -1)),
            })
            {
                rig.LookAt = new Vector3(corner.x, 0, corner.y); rig.Distance = CameraController.MinDistance; rig.Yaw = 0; rig.TopInset = 0;
                for (int i = 0; i < 20; i++) CameraController.Step(rig, new CameraInput { Pan = push }, CameraController.MaxFrameSeconds, bounds, ground, top);
                var with = RenderSample.Render(cam, W, H);
                frame.enabled = false;
                var without = RenderSample.Render(cam, W, H);
                frame.enabled = true;
                map.Terrain.enabled = false;
                var noTerrain = RenderSample.Render(cam, W, H);
                map.Terrain.enabled = true;
                File.WriteAllBytes(Path.Combine(ShotDir, $"edge-{name}.png"), Png(with));

                // 1. The frame (and the table) draws no pixel of the view: hiding it changes nothing.
                // 2. No flat fill: the terrain draws (nearly) every pixel, so hiding it changes them. Before the fix, at the
                //    north-east corner the camera sat inside the hills and nothing drew: 0% (haze over the clear colour). Kits
                //    and trees standing in front of the ground are the only pixels allowed to stay.
                int framePixels = 0, terrainPixels = 0;
                for (int i = 0; i < with.Pixels.Length; i++)
                {
                    if (Differs(with.Pixels[i], without.Pixels[i])) framePixels++;
                    if (Differs(with.Pixels[i], noTerrain.Pixels[i])) terrainPixels++;
                }
                float terrainShare = (float)terrainPixels / with.Pixels.Length;
                TestContext.WriteLine($"{name}: look-at ({rig.LookAt.x:0}, {rig.LookAt.y:0}, {rig.LookAt.z:0}) of ({bounds.xMax:0}, {bounds.yMax:0}); " +
                    $"camera {rig.transform.position.y:0} over ground {ground(new Vector2(rig.transform.position.x, rig.transform.position.z)):0}; " +
                    $"frame pixels {framePixels}; terrain {100f * terrainShare:0.00}% of the view");
                if (framePixels > 0) failures.Add($"{name} corner: the frame shows in {framePixels} pixels");
                if (terrainShare <= MinTerrainShare) failures.Add($"{name} corner: the terrain draws only {100f * terrainShare:0.0}% of the view (a flat fill)");
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));   // every corner checked and written before failing
        }
        finally
        {
            RenderSample.TiltShift(true); MapShaderGlobals.PinWaterTime(null);
            frame.enabled = true; map.Terrain.enabled = true;
            cam.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    /// <summary>Issue #36: the frame and table show at the overview, where the view is wider than the map (diorama02, fitted
    /// by the same CameraController.Step the game runs), on both sides of the board; at gameplay03 they draw no pixel. The
    /// render is 16:9 (W x H), which the split at 100 km assumes: wider windows show the frame from nearer (32:9 from about
    /// 53 km). What each half covers: the overview half fails if the frame does not draw (renderer off, nothing written);
    /// the gameplay half only shows that the frame stays out of gameplay03's view, about 17 km off every edge, so a frame
    /// raised above the ground's top or reaching into the map is caught by PanningIntoACornerShowsNoSkirtSeamOrFlatFill and
    /// MapSceneTests.TheFrameRimsTheMapAndTheTableLiesBeyond instead.</summary>
    [UnityTest]
    public IEnumerator TheFrameShowsAtTheOverviewOnly()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var frame = map.Terrain.transform.Find(MapScene.FrameName).GetComponent<MeshRenderer>();
        var bounds = Bounds(map); var ground = CameraController.GroundOf(map.Terrain); var top = CameraController.GroundTopOf(map.Terrain);
        var rt = new RenderTexture(W, H, 24);
        RenderSample.TiltShift(false);
        MapShaderGlobals.PinWaterTime(100f);
        try
        {
            cam.targetTexture = rt;
            yield return null;
            foreach (var pose in PromoPose.All)
            {
                pose.Apply(rig, map.Frame); rig.TopInset = 0;
                CameraController.Step(rig, new CameraInput(), 0f, bounds, ground, top);
                var with = RenderSample.Render(cam, W, H);
                frame.enabled = false;
                var without = RenderSample.Render(cam, W, H);
                frame.enabled = true;
                int left = 0, right = 0;
                for (int i = 0; i < with.Pixels.Length; i++)
                    if (Differs(with.Pixels[i], without.Pixels[i])) { if (i % W < W / 2) left++; else right++; }
                float share = (float)(left + right) / with.Pixels.Length;
                TestContext.WriteLine($"{pose.Name}: the frame draws {100f * share:0.00}% of the view ({left} px left, {right} px right)");
                if (pose.Distance < 100000f) Assert.AreEqual(0, left + right, $"{pose.Name}: the frame stays off the screen");
                else
                {
                    Assert.Greater(left, with.Pixels.Length / 50, $"{pose.Name}: the frame shows west of the board");
                    Assert.Greater(right, with.Pixels.Length / 50, $"{pose.Name}: the frame shows east of the board");
                }
            }
        }
        finally
        {
            RenderSample.TiltShift(true); MapShaderGlobals.PinWaterTime(null);
            frame.enabled = true;
            cam.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    /// <summary>The least share of the view the terrain must draw at the corner poses (kits and trees may cover the rest).</summary>
    const float MinTerrainShare = 0.95f;

    static bool Differs(Color a, Color b) => Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)) > 2.5f / 255f;

    static byte[] Png(RenderSample s)
    {
        var tex = new Texture2D(s.Width, s.Height, TextureFormat.RGB24, false);
        try { tex.SetPixels(s.Pixels); tex.Apply(); return tex.EncodeToPNG(); }
        finally { Object.DestroyImmediate(tex); }
    }

    /// <summary>The promo and close-check poses already frame only the map where the view fits it, so the clamp leaves
    /// them where they are (the shots in the WebGL player run through Step every frame); diorama02, wider than the map,
    /// is centred on it east to west, at most a few hundred metres from its pose (accepted: controller ruling, owner-delegated).</summary>
    [Test]
    public void ThePromoPosesStayPut()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        cam.aspect = 16f / 9f;
        try
        {
            foreach (var pose in new System.Collections.Generic.List<PromoPose>(PromoPose.All) { PromoPose.CloseChecks[0], PromoPose.CloseChecks[1] })
            {
                pose.Apply(rig, map.Frame); rig.TopInset = 0;
                var before = rig.LookAt;
                CameraController.Step(rig, new CameraInput(), 0f, Bounds(map), CameraController.GroundOf(map.Terrain), CameraController.GroundTopOf(map.Terrain));
                float moved = (rig.LookAt - before).magnitude;
                TestContext.WriteLine($"{pose.Name}: moved {moved:0} m");
                Assert.Less(moved, pose.Distance > 100000f ? 1000f : 1f, pose.Name);
            }
        }
        finally { cam.ResetAspect(); }
    }

    /// <summary>Issue #174: at the hero zoom, anywhere on the map and at any yaw, the camera keeps MinClearanceShare of its
    /// height above the ground beneath it, so it never sits inside the hills (before, about 6% of the map put it underground).</summary>
    [Test]
    public void TheCameraNeverSinksIntoTheGround()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        cam.aspect = 16f / 9f;
        var bounds = Bounds(map); var ground = CameraController.GroundOf(map.Terrain); var top = CameraController.GroundTopOf(map.Terrain);
        try
        {
            int lifted = 0, n = 0; float least = float.MaxValue;
            for (int i = 0; i <= 40; i++)
                for (int j = 0; j <= 40; j++)
                    foreach (var yaw in new[] { 0f, 120f, 240f })
                    {
                        rig.LookAt = new Vector3(bounds.xMin + bounds.width * i / 40f, 0, bounds.yMin + bounds.height * j / 40f);
                        rig.Distance = CameraController.MinDistance; rig.Yaw = yaw; rig.TopInset = 0;
                        CameraController.Step(rig, new CameraInput(), 0f, bounds, ground, top);
                        var c = rig.transform.position;
                        Assert.LessOrEqual(ground(new Vector2(rig.LookAt.x, rig.LookAt.z)), top + 1f, "GroundTopOf bounds the ground");
                        float height = rig.Distance * Mathf.Sin(CameraRig.Pitch * Mathf.Deg2Rad);
                        float share = (c.y - ground(new Vector2(c.x, c.z))) / height;
                        least = Mathf.Min(least, share); n++;
                        if (rig.LookAt.y > 0f) lifted++;
                    }
            TestContext.WriteLine($"{n} poses, {lifted} lifted, least clearance {least:0.000} of the camera's height");
            Assert.GreaterOrEqual(least, CameraController.MinClearanceShare - 1e-3f);
            Assert.Greater(lifted, 0, "the rule took effect somewhere (the Golan, the Galilee, the Jordanian plateau)");
        }
        finally { cam.ResetAspect(); }
    }

    /// <summary>Review B-I1: the clamp must not hide the map's own edge rows at gameplay zoom. Bounding the near side by the
    /// point below the camera kept the southern 19-20 km (Umm Rashrash/Eilat, the last four hex rows) off screen at 60 km;
    /// panning to the edge must bring Eilat on screen looking north, and Metula looking south.</summary>
    [TestCase("Eilat", 34.95, 29.55, 0f)]
    [TestCase("Metula", 35.578, 33.28, 180f)]
    public void PanningToAnEdgeBringsItsTownsOnScreen(string town, double lon, double lat, float yaw)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        cam.aspect = 16f / 9f;
        var bounds = Bounds(map); var ground = CameraController.GroundOf(map.Terrain); var top = CameraController.GroundTopOf(map.Terrain);
        try
        {
            var at = map.Frame.ToWorld(lon, lat, 0); at.y = ground(new Vector2(at.x, at.z));
            foreach (var inset in new[] { 0f, 0.09f })
            {
                rig.LookAt = new Vector3(at.x, 0, bounds.center.y); rig.Distance = PromoPose.Gameplay03.Distance; rig.Yaw = yaw; rig.TopInset = inset;
                for (int i = 0; i < 100; i++) CameraController.Step(rig, new CameraInput { Pan = Vector2.down }, CameraController.MaxFrameSeconds, bounds, ground, top);
                var v = cam.WorldToViewportPoint(at);
                TestContext.WriteLine($"{town}, yaw {yaw}, inset {inset}: look-at {map.Frame.ToLonLat(rig.LookAt)}, viewport {v}");
                Assert.Greater(v.z, 0f, town);
                Assert.That(v.x, Is.InRange(0f, 1f), $"{town} across the view (inset {inset})");
                Assert.That(v.y, Is.InRange(0f, 1f - inset), $"{town} on screen below the bar (inset {inset})");
            }
        }
        finally { cam.ResetAspect(); }
    }
}
