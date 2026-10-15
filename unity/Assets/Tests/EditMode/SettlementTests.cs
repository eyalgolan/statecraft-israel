using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

public class SettlementTests
{
    static readonly HeightmapMeta Meta = HeightmapLoader.Load(Paths.StreamingMap).meta;
    static (double, double, double, double) Bounds => (Meta.lon0, Meta.lon1, Meta.lat0, Meta.lat1);
    static string RealYamlPath => Path.Combine(Paths.Content, "map", "settlements_1948.yaml");

    const string Src = "sources: [{url: u, checked: x}]";

    static string Entry(string id, string extra = "lon: 35, lat: 32, type: town, founded: 1900, " + Src) =>
        $"- {{id: {id}, name: N, {extra}}}\n";

    static InvalidDataException Rejects(string entries) =>
        Assert.Throws<InvalidDataException>(() => SettlementPlacer.Parse("settlements:\n" + entries, Bounds));

    static void AssertNames(InvalidDataException ex, string id, string field)
    {
        StringAssert.Contains(SettlementPlacer.DefaultFileName, ex.Message);
        StringAssert.Contains($"'{id}'", ex.Message); StringAssert.Contains(field, ex.Message);
    }

    [Test]
    public void ParsesYamlAndRejectsUnknownType()
    {
        var ok = SettlementPlacer.Parse("settlements:\n- {id: jlm, name: Jerusalem, lon: 35.2256, lat: 31.7789, type: city, founded: null, pre_modern: true, sources: [{url: u, checked: '2026-10-09'}]}\n");
        Assert.AreEqual("Jerusalem", ok[0].Name); Assert.IsNull(ok[0].Founded); Assert.IsTrue(ok[0].PreModern);
        Assert.Throws<InvalidDataException>(() => SettlementPlacer.Parse("settlements:\n- {id: q, name: Q, lon: 35, lat: 32, type: castle, founded: 1900, sources: [{url: u, checked: x}]}\n"));
    }

    [Test] public void UnknownTypeNamesIdAndField() =>
        AssertNames(Rejects(Entry("q", "lon: 35, lat: 32, type: castle, founded: 1900, " + Src)), "q", "type");

    [Test] public void DuplicateIdIsRejected() => AssertNames(Rejects(Entry("a") + Entry("a")), "a", "id");

    [Test] public void NonFiniteCoordinateIsRejected() =>
        AssertNames(Rejects(Entry("n", "lon: .nan, lat: 32, type: town, founded: 1900, " + Src)), "n", "lon");

    [Test]
    public void CoordinateOutsideHeightmapBoundsIsRejected()
    {
        AssertNames(Rejects(Entry("cy", "lon: 33.36, lat: 35.17, type: town, founded: 1900, " + Src)), "cy", "lon"); // Nicosia
        AssertNames(Rejects(Entry("s", "lon: 35, lat: 29.40, type: town, founded: 1900, " + Src)), "s", "lat");
    }

    [Test] public void MissingLonIsRejected() => AssertNames(Rejects(Entry("m", "lat: 32, type: town, founded: 1900, " + Src)), "m", "lon");
    [Test] public void MissingLatIsRejected() => AssertNames(Rejects(Entry("m", "lon: 35, type: town, founded: 1900, " + Src)), "m", "lat");

    [Test] public void EmptySourcesIsRejected() =>
        AssertNames(Rejects(Entry("e", "lon: 35, lat: 32, type: town, founded: 1900, sources: []")), "e", "sources");

    [Test] public void EmptySourceUrlIsRejected() =>
        AssertNames(Rejects(Entry("u", "lon: 35, lat: 32, type: town, founded: 1900, sources: [{url: '', checked: x}]")), "u", "url");

    [Test] public void EmptySourceCheckedIsRejected() =>
        AssertNames(Rejects(Entry("c", "lon: 35, lat: 32, type: town, founded: 1900, sources: [{url: u, checked: ''}]")), "c", "checked");

    [Test] public void NullFoundedNeedsPreModern() =>
        AssertNames(Rejects(Entry("f", "lon: 35, lat: 32, type: town, founded: null, " + Src)), "f", "founded");

    [Test]
    public void EmptyIdIsRejected()
    {
        var ex = Rejects("- {id: '', name: N, lon: 35, lat: 32, type: town, founded: 1900, " + Src + "}\n");
        StringAssert.Contains(SettlementPlacer.DefaultFileName, ex.Message); StringAssert.Contains("id", ex.Message);
    }

    [Test] public void EmptyNameIsRejected() =>
        AssertNames(Rejects("- {id: nn, name: '', lon: 35, lat: 32, type: town, founded: 1900, " + Src + "}\n"), "nn", "name");

    [Test]
    public void UnknownKeyAndBadScalarBecomeInvalidDataNamingTheFile()
    {
        StringAssert.Contains(SettlementPlacer.DefaultFileName, Rejects(Entry("k", "lon: 35, lat: 32, type: town, founded: 1900, colour: red, " + Src)).Message);
        StringAssert.Contains(SettlementPlacer.DefaultFileName, Rejects(Entry("b", "lon: east, lat: 32, type: town, founded: 1900, " + Src)).Message);
    }

    // Final fix 3, M5: duplicate keys and malformed ids name the file, the id and the field.
    [Test]
    public void DuplicateKeyIsRejectedNamingIdAndField()
    {
        var ex = Rejects(Entry("a") + Entry("d", "lon: 35, lon: 35.5, lat: 32, type: town, founded: 1900, " + Src));
        AssertNames(ex, "d", "lon"); StringAssert.Contains("duplicate", ex.Message);
    }

    [Test]
    public void DuplicateKeyInBlockStyleIsRejectedNamingIdAndField()
    {
        var ex = Rejects("- id: b\n  name: N\n  lon: 35\n  lat: 32\n  type: town\n  type: city\n  founded: 1900\n  " + Src + "\n");
        AssertNames(ex, "b", "type"); StringAssert.Contains("duplicate", ex.Message);
    }

    [TestCase("Tel_Aviv")]
    [TestCase("tel-aviv")]
    [TestCase("9a")]
    [TestCase("_a")]
    [TestCase("'a b'")]
    public void IdThatIsNotSnakeCaseIsRejected(string id)
    {
        var ex = Rejects(Entry(id));
        AssertNames(ex, id.Trim('\''), "id");
    }

    [Test]
    public void RealSettlementFileParses()
    {
        var list = SettlementPlacer.Parse(File.ReadAllText(RealYamlPath), Bounds);
        Assert.AreEqual(25, list.Count);
        // #558: every game settlement has its sourced 1948 Hebrew name (presentation only; the UI wiring comes later).
        foreach (var s in list) { Assert.IsNotEmpty(s.NameHe, s.Id); Assert.IsNotEmpty(s.NameHeSources, s.Id); }
        Assert.AreEqual("תל אביב", list.Find(s => s.Id == "tel_aviv").NameHe);
    }

    /// <summary>#558: name_he reads with its own sources; a Hebrew name without sources, or sources without the name, is an error
    /// naming the id and the field.</summary>
    [Test]
    public void HebrewNameReadsWithItsSources()
    {
        var ok = SettlementPlacer.Parse("settlements:\n" + Entry("h", "lon: 35, lat: 32, type: town, founded: 1900, name_he: \"\u05D0\", name_he_sources: [{url: u, checked: x}], " + Src), Bounds);
        Assert.AreEqual("\u05D0", ok[0].NameHe);
        Assert.AreEqual("u", ok[0].NameHeSources[0].Url);
        Assert.AreEqual("", SettlementPlacer.Parse("settlements:\n" + Entry("e"), Bounds)[0].NameHe, "absent: the English stays");
        AssertNames(Rejects(Entry("h", "lon: 35, lat: 32, type: town, founded: 1900, name_he: \"\u05D0\", " + Src)), "h", "name_he_sources");
        AssertNames(Rejects(Entry("h", "lon: 35, lat: 32, type: town, founded: 1900, name_he_sources: [{url: u, checked: x}], " + Src)), "h", "name_he");
        AssertNames(Rejects(Entry("h", "lon: 35, lat: 32, type: town, founded: 1900, name_he: \"\u05D0\", name_he_sources: [{url: u}], " + Src)), "h", "name_he_sources[0].checked");
        // The review flag (#564): agent-checked until a native speaker reads the name, then left out.
        var flagged = SettlementPlacer.Parse("settlements:\n" + Entry("h", "lon: 35, lat: 32, type: town, founded: 1900, name_he: \"\u05D0\", name_he_sources: [{url: u, checked: x}], name_he_review: agent-checked, " + Src), Bounds);
        Assert.AreEqual("agent-checked", flagged[0].NameHeReview);
        AssertNames(Rejects(Entry("h", "lon: 35, lat: 32, type: town, founded: 1900, name_he: \"\u05D0\", name_he_sources: [{url: u, checked: x}], name_he_review: done, " + Src)), "h", "name_he_review");
        AssertNames(Rejects(Entry("h", "lon: 35, lat: 32, type: town, founded: 1900, name_he_review: agent-checked, " + Src)), "h", "name_he_review");
        // A blank or empty flag is not a review: the key is either agent-checked or gone (second review of #610).
        foreach (var blank in new[] { "''", "~", "\"\"" })
            AssertNames(Rejects(Entry("h", $"lon: 35, lat: 32, type: town, founded: 1900, name_he: \"\u05D0\", name_he_sources: [{{url: u, checked: x}}], name_he_review: {blank}, " + Src)), "h", "name_he_review");
    }

}

/// <summary>MapScene, built in edit mode with the editor-made material assets. Each test runs in a fresh empty scene so it
/// never touches the runner's own camera (R4). The scene replaces the runner's untitled one (NewSceneMode.Single):
/// Unity refuses an additive new scene while an untitled scene is unsaved, which is the state batch test runs start in.</summary>
public class MapSceneTests
{
    static readonly HeightmapMeta Meta = HeightmapLoader.Load(Paths.StreamingMap).meta;
    static (double, double, double, double) Bounds => (Meta.lon0, Meta.lon1, Meta.lat0, Meta.lat1);
    static string RealYamlPath => Path.Combine(Paths.Content, "map", "settlements_1948.yaml");

    const string TerrainMatPath = "Assets/Materials/Terrain.mat",
        MarkerMatPath = "Assets/Materials/SettlementMarker.mat", SelectionMatPath = "Assets/Materials/HexSelection.mat", SeaMatPath = "Assets/Materials/Sea.mat", FrameMatPath = "Assets/Materials/MapFrame.mat";

    [SetUp] public void FreshScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    // Replacing the scene destroys everything the test built (MapRuntimeAssets frees the runtime assets with it).
    [TearDown] public void DiscardScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    static Material Load(string path) { var m = AssetDatabase.LoadAssetAtPath<Material>(path); Assert.IsNotNull(m, path); return m; }

    static MapScene NewMap(bool withCamera = true)
    {
        if (withCamera) new GameObject("TestMainCamera") { tag = "MainCamera" }.AddComponent<Camera>();
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load(TerrainMatPath), Load(MarkerMatPath), Load(SelectionMatPath), Load(SeaMatPath), Load(FrameMatPath));
        return map;
    }

    static int CountNamed(string name)
    {
        int n = 0; foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (t.name == name) n++;
        return n;
    }

    static void AssertOneOfEach()
    {
        Assert.AreEqual(1, Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Length, "terrains");
        Assert.AreEqual(1, Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Length, "volumes");
        Assert.AreEqual(1, CountNamed("Settlements"), "Settlements roots");
        Assert.AreEqual(1, Object.FindObjectsByType<CameraRig>(FindObjectsSortMode.None).Length, "camera rigs");
    }

    /// <summary>Playtest 1, item 9: beyond the terrain the view shows sea, not a grey void. The diorama pitch never shows
    /// the sky, so the camera clears to the sea biome colour.</summary>
    [Test]
    public void TheCameraClearsToTheSeaColour()
    {
        var map = NewMap(); map.Build();
        var cam = Camera.main;
        Assert.AreEqual(CameraClearFlags.SolidColor, cam.clearFlags);
        Assert.AreEqual(MapBuilder.BiomeColours["sea"], cam.backgroundColor);
        // Fix round 1 (review M5): the top-bar inset shifts the frustum up, so check the worst case the rig allows: the
        // top ray rises to atan((1 + inset) tan(Fov/2)) above the axis. Analytically, and on the camera's own rays.
        var worst = Mathf.Rad2Deg * Mathf.Atan((1 + CameraRig.MaxTopInset) * Mathf.Tan(Mathf.Deg2Rad * CameraRig.Fov / 2));
        Assert.Greater(CameraRig.Pitch - worst, 0f, "the top of the view looks down at the largest inset: no sky to show");
        var rig = cam.GetComponent<CameraRig>();
        rig.TopInset = 1f; rig.Apply();                                    // clamped to MaxTopInset
        foreach (var x in new[] { 0f, 0.5f, 1f })
            Assert.Less(cam.ViewportPointToRay(new Vector3(x, 1f, 0)).direction.y, 0f, $"top edge at x={x} looks down");
    }

    /// <summary>Issue #36 (it replaces fix round 2's skirt, R2-1 and N1): the map is a board in a wooden frame on a table.
    /// The frame's mesh lies entirely outside the terrain's footprint (nothing covers land below sea level), its inner edge
    /// meets the terrain's edge at every heightmap sample (no sliver), and all of it lies between the terrain's lowest
    /// (world y 0) and highest points, where the camera's fit keeps it off the screen whenever the view fits the map (#174).
    /// The bevel is cut earth where the edge stands above the frame (the Jordanian plateau) and the frame's lip where it lies
    /// below (the sea); the table reaches TableReachM past the frame on every side. Every face is seen from above or, for
    /// the frame's outer face, from outside.</summary>
    [Test]
    public void TheFrameRimsTheMapAndTheTableLiesBeyond()
    {
        var map = NewMap(); map.Build();
        Assert.IsNull(map.Terrain.transform.Find("SeaPlane"), "no plane over the terrain (fix round 2, N1)");
        var frame = map.Terrain.transform.Find(MapScene.FrameName);
        Assert.IsNotNull(frame, "the frame exists");
        var mr = frame.GetComponent<MeshRenderer>();
        CollectionAssert.AreEqual(new[] { Load(FrameMatPath) }, mr.sharedMaterials, "one editor-made material");
        Assert.AreEqual("Statecraft/MapFrame", Load(FrameMatPath).shader.name);
        Assert.IsFalse(ShaderUtil.ShaderHasError(Load(FrameMatPath).shader));
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(Load(FrameMatPath).shader), "compiles for WebGL 2");
        Assert.AreEqual(ShadowCastingMode.Off, mr.shadowCastingMode);
        Assert.IsNull(frame.GetComponent<Collider>(), "clicks pick the terrain only");
        var mesh = frame.GetComponent<MeshFilter>().sharedMesh;
        Assert.AreEqual(1, mesh.subMeshCount);
        var verts = mesh.vertices; var colours = mesh.colors; var tris = mesh.triangles;
        var size = map.Terrain.terrainData.size;
        // Nothing of the frame lies over the terrain (N1): every triangle is outside the footprint.
        for (int k = 0; k < tris.Length; k += 3)
        {
            var c = (verts[tris[k]] + verts[tris[k + 1]] + verts[tris[k + 2]]) / 3;
            Assert.IsFalse(c.x > 1f && c.x < size.x - 1f && c.z > 1f && c.z < size.z - 1f, $"frame triangle over the terrain at {c}");
        }
        // Nor any vertex (review S3: a bevel inset 100 m into the map keeps every centroid outside the 1 m margin).
        foreach (var v in verts)
            Assert.IsFalse(v.x > 0.5f && v.x < size.x - 0.5f && v.z > 0.5f && v.z < size.z - 0.5f, $"frame vertex over the terrain at {v}");
        // Inside the camera's fit (#174): between the terrain's lowest and highest points.
        float top = CameraController.GroundTopOf(map.Terrain);
        foreach (var v in verts)
        {
            var w = frame.TransformPoint(v);
            Assert.That(w.y, Is.InRange(ShadowFit.GroundY - 0.01f, top), $"frame vertex {w} within the ground's height range");
        }
        // The table reaches TableReachM past the frame on every side.
        var b = mesh.bounds; float reach = MapBuilder.FrameBevelM + MapBuilder.FrameWidthM + MapBuilder.TableReachM - 1f;
        Assert.LessOrEqual(b.min.x, -reach); Assert.GreaterOrEqual(b.max.x, size.x + reach);
        Assert.LessOrEqual(b.min.z, -reach); Assert.GreaterOrEqual(b.max.z, size.z + reach);
        // Every face is seen from above, or (the frame's outer face, vertical) from outside: the winding Unity culls by.
        var centre = new Vector3(size.x / 2, 0, size.z / 2);
        for (int k = 0; k < tris.Length; k += 3)
        {
            Vector3 p0 = verts[tris[k]], p1 = verts[tris[k + 1]], p2 = verts[tris[k + 2]];
            var n = Vector3.Cross(p1 - p0, p2 - p0);
            var c = (p0 + p1 + p2) / 3 - centre;
            bool up = n.y > 1e-3f * n.magnitude, outward = Mathf.Abs(n.y) <= 1e-3f * n.magnitude && n.x * c.x + n.z * c.z > 0;
            Assert.IsTrue(up || outward, $"triangle {k / 3} at {c + centre} faces {n}");
        }
        // Cut earth above the frame's top (east, at Amman's latitude: the plateau), the frame's lip below it (west, the sea).
        float KindAt(double lat, double lon)
        {
            var p = map.Frame.ToWorld(lon, lat, 0); int best = -1;
            for (int i = 0; i < verts.Length; i++)
                if (colours[i].a < 0.75f && (best < 0 || (new Vector2(verts[i].x - p.x, verts[i].z - p.z)).sqrMagnitude < (new Vector2(verts[best].x - p.x, verts[best].z - p.z)).sqrMagnitude)) best = i;
            return colours[best].a;
        }
        Assert.AreEqual(MapBuilder.FrameKind.Earth, KindAt(31.95, 35.9), "the plateau's edge: cut earth");
        Assert.AreEqual(MapBuilder.FrameKind.Wood, KindAt(32.0, 34.2), "the sea's edge: the frame's lip");
        // Fix round 3 (m-4), kept: on all four edges, at every heightmap sample (the terrain's own vertices), the frame's
        // inner edge is at the terrain's height: no sliver between them. Its height between its inner vertices is linear.
        var data = map.Terrain.terrainData;
        int res = data.heightmapResolution;
        var ring0 = System.Array.FindAll(verts, v => v.x <= 0.5f || v.x >= size.x - 0.5f || v.z <= 0.5f || v.z >= size.z - 0.5f);
        foreach (var (name, onEdge, along, at) in new (string, System.Func<Vector3, bool>, System.Func<Vector3, float>, System.Func<float, Vector2>)[] {
            ("south", v => Mathf.Abs(v.z) < 0.5f, v => v.x, t => new Vector2(t, 0)),
            ("north", v => Mathf.Abs(v.z - size.z) < 0.5f, v => v.x, t => new Vector2(t, size.z)),
            ("west", v => Mathf.Abs(v.x) < 0.5f, v => v.z, t => new Vector2(0, t)),
            ("east", v => Mathf.Abs(v.x - size.x) < 0.5f, v => v.z, t => new Vector2(size.x, t)) })
        {
            var line = System.Array.FindAll(ring0, v => onEdge(v)); System.Array.Sort(line, (p, q) => along(p).CompareTo(along(q)));
            // Review S3: with no vertex on the edge line the loop below checked nothing and passed. One inner vertex per
            // heightmap sample, and every sample but the last bracketed by two of them.
            Assert.GreaterOrEqual(line.Length, res, $"{name} edge: the frame's inner vertices lie on the map's edge");
            float worst = 0; float worstAt = 0; int checkedSamples = 0;
            for (int i = 0; i < res; i++)
            {
                float t = (name == "south" || name == "north" ? size.x : size.z) * i / (res - 1);
                int k = System.Array.FindLastIndex(line, v => along(v) <= t + 0.01f);
                if (k < 0 || k >= line.Length - 1) continue;
                var a0 = line[k]; var a1 = line[k + 1];
                float f = (t - along(a0)) / Mathf.Max(1e-3f, along(a1) - along(a0));
                var p2 = at(t);
                float terrain = data.GetInterpolatedHeight(p2.x / size.x, p2.y / size.z);
                float d = Mathf.Abs(Mathf.Lerp(a0.y, a1.y, f) - terrain);
                if (d > worst) { worst = d; worstAt = t; }
                checkedSamples++;
            }
            Assert.GreaterOrEqual(checkedSamples, res - 1, $"{name} edge: samples checked");
            TestContext.WriteLine($"{name} edge: largest gap to the terrain {worst:0.00} m (at {worstAt:0} m)");
            Assert.Less(worst, 0.5f, $"{name} edge: a sliver of {worst:0.00} m at {worstAt:0} m along it");
        }
    }

    /// <summary>Renders the real scene (Map.unity's light, rig and post-processing) looking at <paramref name="look"/> and
    /// returns the pixel colour of each world point: with <paramref name="radius"/> over 0, the darkest pixel of the
    /// (2 radius + 1)-square around it when <paramref name="darkest"/>, else that square's mean.</summary>
    static System.Collections.IEnumerator RenderAt(Vector3 look, float distance, Vector3[] points, Color[] colours, int radius = 0, bool darkest = false)
    {
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = look; rig.Distance = distance; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var rt = new RenderTexture(1600, 900, 24);
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        try
        {
            cam.targetTexture = rt;
            yield return null;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            for (int i = 0; i < points.Length; i++)
            {
                var p = cam.WorldToScreenPoint(points[i]);   // while the camera still renders into the texture
                var best = tex.GetPixel((int)p.x, (int)p.y); var sum = Color.clear;
                for (int dx = -radius; dx <= radius; dx++)
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        var c = tex.GetPixel((int)p.x + dx, (int)p.y + dy);
                        if (darkest && c.r + c.g + c.b < best.r + best.g + best.b) best = c;
                        sum += c;
                    }
                colours[i] = darkest ? best : sum / ((2 * radius + 1) * (2 * radius + 1));
            }
        }
        finally
        {
            RenderTexture.active = null; cam.targetTexture = null;
            rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        }
    }

    static bool LandColour(Color c) => c.r > c.b + 0.05f;
    static bool SeaColour(Color c) => c.b > c.r + 0.15f;

    /// <summary>Issue #36, rendered (it replaces fix round 2's land-or-sea backdrop): 15 km past each edge the table shows,
    /// dark warm wood (red over green over blue, darker than the board's sand next to it), and the frame's top between them is
    /// lighter than the table.</summary>
    [UnityTest]
    public System.Collections.IEnumerator TheTableLiesBeyondTheFrameOnEverySide()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        // The middle of the frame's crown (its rounded top's flat, 0.3-0.7 of its width: MapBuilder.BuildFrame's profile), at
        // its own height (sea level in world y, plus the frame's rise and crown: review S2), and a point on the table 15 km
        // out, at world y 0. Each is the mean of a 5 x 5 pixel patch (about 0.4 km at 90 km, well inside the crown's 2.4 km).
        float seaY = (0f - Meta.elev_min_m) * MapBuilder.VerticalExaggeration;
        float crown = MapBuilder.FrameBevelM + 0.5f * MapBuilder.FrameWidthM, past = 15000f;
        float topY = seaY + MapBuilder.FrameRiseM + MapBuilder.FrameCrownM;
        foreach (var (name, lat, lon, outward) in new[] { ("east", 31.95, 35.9, Vector3.right), ("south", 29.45, 34.6, Vector3.back), ("west", 32.0, 34.2, Vector3.left), ("north (the sea off Tyre)", 33.35, 34.9, Vector3.forward) })
        {
            var edge = map.Frame.ToWorld(lon, lat, 0); edge.y = map.Terrain.SampleHeight(edge);
            var table = edge + outward * past; table.y = 0f;
            var top = edge + outward * crown; top.y = topY;
            var colours = new Color[2];
            yield return RenderAt(edge, 90000f, new[] { table, top }, colours, 2);
            Color t = colours[0], f = colours[1];
            TestContext.WriteLine($"{name}: table {t}, frame {f}");
            Assert.IsTrue(t.r > t.g && t.g > t.b, $"{name}: warm wood, rendered {t}");
            Assert.Less(RenderSample.Luma(t), 0.3f, $"{name}: a dark table, rendered {t}");
            Assert.Greater(RenderSample.Luma(f), RenderSample.Luma(t) + 0.03f, $"{name}: the frame ({f}) reads against the table ({t})");
        }
    }

    /// <summary>Fix round 2 (N1), rendered: the land below sea level (the Jordan valley, Beit She'an, the Dead Sea's shores
    /// and the Arava) shows its biome colour, its hex lines and its settlement markers; nothing covers it.</summary>
    [UnityTest]
    public System.Collections.IEnumerator TheRiftBelowSeaLevelIsVisible()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        float size = map.Hexes.hex_size_km * 1000f;
        // The lowest land hex in each band: Beit She'an valley, the Jordan valley by Jericho, the Arava north of Eilat.
        foreach (var (name, lat0, lat1) in new[] { ("Beit She'an valley", 32.35, 32.65), ("Jordan valley (Jericho)", 31.75, 32.05), ("Dead Sea shore / northern Arava", 30.6, 31.2) })
        {
            HexJson low = null;
            foreach (var h in map.Hexes.hexes)
                if (h.land && h.lat >= lat0 && h.lat <= lat1 && h.biome != "lake" && (low == null || h.elev_m < low.elev_m)) low = h;
            Assert.IsNotNull(low, name);
            Assert.Less(low.elev_m, -10f, $"{name}: a hex below the old plane's -10 m");
            var centre = map.Frame.ToWorld(low.lon, low.lat, 0); centre.y = map.Terrain.SampleHeight(centre);
            var corner2 = HexMath.Corner(new Vector2(centre.x, centre.z), size, 0);
            var corner = new Vector3(corner2.x, 0, corner2.y); corner.y = map.Terrain.SampleHeight(corner);
            var colours = new Color[1];
            yield return RenderAt(centre, 40000f, new[] { centre }, colours);
            // Visual v1 Task 8: the terrain shader draws the grid, light and faint as in the promo; the line shows if the
            // corner's neighbourhood changes when the grid is switched off (tilt-shift off: a colour test, task-3 carry m6).
            var cam = Camera.main; var px = new Vector2[1];
            RenderSample.TiltShift(false);
            var on = RenderSample.Render(cam, 1600, 900, new[] { corner }, px);
            VisualToggles.Set("grid", false);
            var off = RenderSample.Render(cam, 1600, 900, new[] { corner }, px);
            VisualToggles.Set("grid", true);
            RenderSample.TiltShift(true);
            float change = 0f;
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    change = Mathf.Max(change, Mathf.Abs(RenderSample.Luma(on.At((int)px[0].x + dx, (int)px[0].y + dy)) - RenderSample.Luma(off.At((int)px[0].x + dx, (int)px[0].y + dy))));
            TestContext.WriteLine($"{name} ({low.q}, {low.r}) {low.elev_m:0} m: centre {colours[0]}, line change {change:0.000}");
            Assert.IsTrue(LandColour(colours[0]), $"{name}: the {low.biome} colour, rendered {colours[0]}");
            // 0.012 (3/255 in sRGB luma, above 8-bit quantisation; grid off renders are identical, so any change is the
            // line). The plan's 0.03 holds in the green rift valleys (about 0.06), but light lines on the bright Arava
            // desert by the Dead Sea change luma only about 0.016: faint, as the promo's land lines are (pre-grade).
            Assert.Greater(change, 0.012f, $"{name}: the hex line shows");
        }
        // Settlement markers in the rift (below -10 m) are drawn: the pixel at the marker's top is not the sea colour.
        var root = GameObject.Find("Settlements").transform;
        int seen = 0;
        foreach (Transform box in root)
        {
            var ground = map.Terrain.SampleHeight(box.position);
            var realElevation = ground / MapBuilder.VerticalExaggeration + HeightmapLoader.Load(Paths.StreamingMap).meta.elev_min_m;
            if (realElevation > -10f) continue;
            var top = box.position + Vector3.up * box.lossyScale.y / 2;
            var colours = new Color[1];
            yield return RenderAt(box.position, 40000f, new[] { top }, colours);
            TestContext.WriteLine($"marker {box.name} at {realElevation:0} m: {colours[0]}");
            Assert.IsFalse(SeaColour(colours[0]), $"marker {box.name} is hidden ({colours[0]})");
            seen++;
        }
        Assert.Greater(seen, 0, "the rift has settlement markers to check");
    }

    /// <summary>Playtest 1, item 3: with the top bar covering the top <c>TopInset</c> of the screen, the look-at point is
    /// framed in the middle of the map area below the bar, not in the middle of the whole screen.</summary>
    [Test]
    public void TheLookAtPointIsCentredBelowTheTopBar()
    {
        var map = NewMap(); map.Build();
        var cam = Camera.main;
        var rig = cam.GetComponent<CameraRig>();
        Assert.AreEqual(0.5f, cam.WorldToViewportPoint(rig.LookAt).y, 1e-3f, "no inset: the screen's centre");
        rig.TopInset = 0.1f; rig.Apply();
        var v = cam.WorldToViewportPoint(rig.LookAt);
        Assert.AreEqual(0.45f, v.y, 1e-3f, "the middle of the 90% below the bar (viewport y is up)");
        Assert.AreEqual(0.5f, v.x, 1e-3f);
        rig.TopInset = 0f; rig.Apply();
        Assert.AreEqual(0.5f, cam.WorldToViewportPoint(rig.LookAt).y, 1e-3f, "the inset is undone");
    }

    /// <summary>Playtest 1, item 10: the selected hex gets an outline in the highlight colour, draped on the terrain
    /// (HexMath corners), drawn with its own editor-made material (no runtime material).</summary>
    [Test]
    public void SelectingAHexOutlinesItOnTheTerrain()
    {
        var map = NewMap(); map.Build();
        var selection = Load(SelectionMatPath);
        // Fix round 1, M2: its own editor-made material, whose shader pulls the band towards the camera in depth (Offset), so
        // it cannot z-fight with the ground at long range.
        Assert.AreEqual("Statecraft/HexSelection", selection.shader.name);
        StringAssert.IsMatch(@"(?m)^\s*Offset\s+-\d+\s*,\s*-\d+", File.ReadAllText(AssetDatabase.GetAssetPath(selection.shader)));
        Assert.Greater(selection.renderQueue, 2000, "drawn after the opaque terrain (queue Geometry+10)");
        // Fix round 3 (m-2): every pass declares the same UnityPerMaterial CBUFFER, as the SRP Batcher requires.
        var source = File.ReadAllText(AssetDatabase.GetAssetPath(selection.shader));
        var buffers = System.Text.RegularExpressions.Regex.Matches(source, @"CBUFFER_START\(UnityPerMaterial\)(.*?)CBUFFER_END", System.Text.RegularExpressions.RegexOptions.Singleline);
        var passes = System.Text.RegularExpressions.Regex.Matches(source, @"HLSLPROGRAM").Count;
        Assert.AreEqual(passes, buffers.Count, "a UnityPerMaterial CBUFFER in every pass");
        foreach (System.Text.RegularExpressions.Match b in buffers)
            Assert.AreEqual(System.Text.RegularExpressions.Regex.Replace(buffers[0].Groups[1].Value, @"\s+", " "), System.Text.RegularExpressions.Regex.Replace(b.Groups[1].Value, @"\s+", " "), "the same layout in every pass");
        // Fix round 2 (N6): the band is in the depth prepass / camera depth texture too, so depth of field sees its depth.
        Assert.GreaterOrEqual(selection.FindPass("DepthOnly"), 0, "a DepthOnly pass");
        Assert.AreEqual("DepthOnly", selection.shader.FindPassTagValue(selection.FindPass("DepthOnly"), new UnityEngine.Rendering.ShaderTagId("LightMode")).name);
        float size = map.Hexes.hex_size_km * 1000f;
        bool Inland(HexJson h)   // whole hex on the terrain, so no corner is clamped to the map's edge
        {
            var w = map.Frame.ToWorld(h.lon, h.lat, 0);
            return w.x > size && w.x < map.Frame.WidthM - size && w.z > size && w.z < map.Frame.LengthM - size;
        }
        // The roughest inland hexes: the band must clear the ground across its whole width there, not just at its edges.
        var land = System.Array.FindAll(map.Hexes.hexes, h => h.land && (h.biome == "mountains" || h.biome == "hills") && Inland(h));
        Assert.Greater(land.Length, 2);
        foreach (var h in land)
        {
            map.Select(h.q, h.r);
            var sel = map.Terrain.transform.Find(MapScene.SelectionName);
            Assert.IsNotNull(sel, "the selection outline exists");
            var mr = sel.GetComponent<MeshRenderer>();
            Assert.IsTrue(mr.enabled, "the outline is shown");
            Assert.AreSame(selection, mr.sharedMaterial, "the editor-made selection material, not an instance");
            var verts = sel.GetComponent<MeshFilter>().sharedMesh.vertices;
            int n = MapBuilder.RingVertices;
            Assert.AreEqual(2 * n, verts.Length);
            var centre = map.Frame.ToWorld(h.lon, h.lat, 0);
            var c2 = new Vector2(centre.x, centre.z);
            for (int i = 0; i < 6; i++)
            {
                var corner = HexMath.Corner(c2, size, i);
                Assert.IsTrue(System.Array.Exists(verts, v => (new Vector2(v.x, v.z) - corner).magnitude < 1f), $"corner {i} of ({h.q}, {h.r})");
            }
            for (int j = 0; j < n; j++)
            {
                Assert.LessOrEqual((new Vector2(verts[j].x, verts[j].z) - c2).magnitude, size + 1f, "inside the hex");
                var mid = (verts[j] + verts[n + j]) / 2;    // the middle of the band, between its outer and inner edge
                Assert.Greater(mid.y, map.Terrain.SampleHeight(mid) + MapScene.SelectionClearanceM - 1f,
                    $"({h.q}, {h.r}) vertex {j}: the band clears the ground across its width");
            }
        }
        map.Select(-9999, -9999);
        Assert.IsFalse(map.Terrain.transform.Find(MapScene.SelectionName).GetComponent<MeshRenderer>().enabled, "no hex there: no outline");
    }

    [Test]
    public void BuildWithoutMaterialsNamesTheMissingField()
    {
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load(TerrainMatPath), null, Load(SelectionMatPath), Load(SeaMatPath), Load(FrameMatPath));
        var ex = Assert.Throws<System.InvalidOperationException>(() => map.Build());
        StringAssert.Contains("markerMaterial", ex.Message);
        map.Configure(Load(TerrainMatPath), Load(MarkerMatPath), null, Load(SeaMatPath), Load(FrameMatPath));
        ex = Assert.Throws<System.InvalidOperationException>(() => map.Build());
        StringAssert.Contains("selectionMaterial", ex.Message);
        map.Configure(Load(TerrainMatPath), Load(MarkerMatPath), Load(SelectionMatPath), null, Load(FrameMatPath));
        ex = Assert.Throws<System.InvalidOperationException>(() => map.Build());
        StringAssert.Contains("seaMaterial", ex.Message);
        map.Configure(Load(TerrainMatPath), Load(MarkerMatPath), Load(SelectionMatPath), Load(SeaMatPath), null);
        ex = Assert.Throws<System.InvalidOperationException>(() => map.Build());
        StringAssert.Contains("frameMaterial", ex.Message);
    }

    [Test]
    public void BuildWithoutMainCameraThrows()
    {
        var map = NewMap(withCamera: false);
        Assert.IsNull(Camera.main);
        var ex = Assert.Throws<System.InvalidOperationException>(() => map.Build());
        Assert.AreEqual("MapScene needs a camera tagged MainCamera", ex.Message);
    }

    [Test]
    public void BuildTwiceLeavesOneTerrainOneSettlementsRootAndOneVolume()
    {
        var map = NewMap();
        map.Build(); map.Build();
        AssertOneOfEach();
    }

    [Test]
    public void RebuildByAFreshComponentStillClearsTheOldMap()
    {
        // After a domain reload a MapScene has lost any non-serialized references to what it built (R2).
        var map = NewMap(); map.Build();
        var go = map.gameObject; Object.DestroyImmediate(map);
        var fresh = go.AddComponent<MapScene>();
        fresh.Configure(Load(TerrainMatPath), Load(MarkerMatPath), Load(SelectionMatPath), Load(SeaMatPath), Load(FrameMatPath));
        fresh.Build();
        AssertOneOfEach();
    }

    [Test]
    public void RebuildingDoesNotLeakVolumeComponents()
    {
        var map = NewMap(); map.Build();
        int after1 = Resources.FindObjectsOfTypeAll<VolumeComponent>().Length;
        int profiles1 = Resources.FindObjectsOfTypeAll<VolumeProfile>().Length;   // review m1: Init makes one per build
        map.Build(); map.Build();
        Assert.AreEqual(after1, Resources.FindObjectsOfTypeAll<VolumeComponent>().Length);
        Assert.AreEqual(profiles1, Resources.FindObjectsOfTypeAll<VolumeProfile>().Length, "the runtime profile is owned by the map");
    }

    [Test]
    public void EveryMarkerSitsOnItsSourcedPositionWithTheMarkerMaterial()
    {
        var map = NewMap(); map.Build();
        var marker = Load(MarkerMatPath);
        Assert.AreEqual("Universal Render Pipeline/Lit", marker.shader.name);
        Assert.AreEqual(0f, marker.GetFloat("_Smoothness"), "matte marker");

        // The frame comes from the heightmap meta, independently of the code under test (R5).
        Assert.AreEqual(Meta.lon0, map.Frame.Lon0); Assert.AreEqual(Meta.lon1, map.Frame.Lon1);
        Assert.AreEqual(Meta.lat0, map.Frame.Lat0); Assert.AreEqual(Meta.lat1, map.Frame.Lat1);
        Assert.AreEqual(Meta.width_km * 1000f, map.Frame.WidthM, 1f); Assert.AreEqual(Meta.length_km * 1000f, map.Frame.LengthM, 1f);

        var root = GameObject.Find("Settlements").transform;
        foreach (var s in SettlementPlacer.Parse(File.ReadAllText(RealYamlPath), Bounds))
        {
            var box = root.Find(s.Id); Assert.IsNotNull(box, s.Id);
            var want = map.Frame.ToWorld(s.Lon, s.Lat, 0);
            Assert.AreEqual(want.x, box.position.x, 1f, s.Id + " x"); Assert.AreEqual(want.z, box.position.z, 1f, s.Id + " z");
            float baseY = box.position.y - box.lossyScale.y / 2;
            Assert.AreEqual(map.Terrain.SampleHeight(box.position), baseY, 1f, s.Id + " base y");
            Assert.AreSame(marker, box.GetComponent<Renderer>().sharedMaterial, s.Id + " material");
            Assert.IsNull(box.GetComponent<Collider>(), s.Id + " has a collider (R1)");
        }
        var jerusalem = root.Find("jerusalem").position;
        Assert.AreEqual((35.2256 - Meta.lon0) / (Meta.lon1 - Meta.lon0) * Meta.width_km * 1000.0, jerusalem.x, 1.0, "Jerusalem x, absolute");
        Assert.AreEqual((31.7789 - Meta.lat0) / (Meta.lat1 - Meta.lat0) * Meta.length_km * 1000.0, jerusalem.z, 1.0, "Jerusalem z, absolute");
        var telAviv = root.Find("tel_aviv").position; var haifa = root.Find("haifa").position;
        Assert.Less(telAviv.x, jerusalem.x, "Tel Aviv west of Jerusalem");
        Assert.Greater(haifa.z, telAviv.z, "Haifa north of Tel Aviv"); Assert.Greater(haifa.z, jerusalem.z, "Haifa north of Jerusalem");
    }

    [Test]
    public void CaptureOfASceneWithoutMapSceneNamesTheSceneAndTheFix()
    {
        // MapCapture lives in the predefined editor assembly, which an asmdef cannot reference, so reach it by reflection.
        var capture = System.Type.GetType("MapCapture, Assembly-CSharp-Editor");
        Assert.IsNotNull(capture, "MapCapture type");
        var require = capture.GetMethod("RequireMapScene"); Assert.IsNotNull(require, "MapCapture.RequireMapScene");
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => require.Invoke(null, null));
        Assert.IsInstanceOf<System.InvalidOperationException>(ex.InnerException);
        StringAssert.Contains("Assets/Map.unity", ex.InnerException.Message);
        StringAssert.Contains("MakeScene", ex.InnerException.Message);
    }
}
