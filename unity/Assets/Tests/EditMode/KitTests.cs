#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The art spike's procedural settlement kits and trees (issues #31 #32 #34 #37): seeded, sized by population, on
/// land, drawn for WebGL 2, and with shadows that start at the walls.</summary>
public class KitTests
{
    [TearDown]
    public void Restore()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ShadowFit.Restore();
        VisualToggles.Clear();
        KitScale.Reset();
    }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        return map;
    }

    static KitGround Ground(MapScene map) =>
        new KitGround(map.Terrain, map.Frame, map.Hexes, HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta.elev_min_m);

    [Test]
    public void TheSeedIsTheKeyNotTheClock()
    {
        var a = new KitRandom("tel_aviv", 1); var b = new KitRandom("tel_aviv", 1); var c = new KitRandom("jaffa", 1);
        var sa = Enumerable.Range(0, 64).Select(_ => a.Next()).ToArray();
        CollectionAssert.AreEqual(sa, Enumerable.Range(0, 64).Select(_ => b.Next()).ToArray());
        CollectionAssert.AreNotEqual(sa, Enumerable.Range(0, 64).Select(_ => c.Next()).ToArray());
        var r = new KitRandom("x");
        for (int i = 0; i < 1000; i++) { var v = r.Value(); Assert.That(v, Is.InRange(0f, 0.99999994f)); }
    }

    [Test]
    public void EveryBuildDrawsTheSameTownsAndTrees()
    {
        var map = OpenMap();
        var list = map.Settlements;
        var first = SettlementKits.Build(list, Ground(map)); var second = SettlementKits.Build(list, Ground(map));
        Assert.AreEqual(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.AreEqual(first[i].s.Id, second[i].s.Id);
            Assert.AreEqual(first[i].buildings, second[i].buildings, first[i].s.Id);
            Assert.AreEqual(first[i].mesh.vertexCount, second[i].mesh.vertexCount, first[i].s.Id);
            Assert.AreEqual(first[i].mesh.bounds, second[i].mesh.bounds, first[i].s.Id);
        }
        foreach (var k in first.Concat(second)) Object.DestroyImmediate(k.mesh);
        var t1 = Vegetation.Plant(list, Ground(map)); var t2 = Vegetation.Plant(list, Ground(map));
        Assert.AreEqual(t1.Count, t2.Count);
        for (int i = 0; i < t1.Count; i++) { Assert.AreEqual(t1[i].kind, t2[i].kind); Assert.AreEqual(t1[i].m, t2[i].m); }
        TestContext.WriteLine(map.KitStats);
    }

    [Test]
    public void TownsSpreadInProportionToTheirPeople()
    {
        var map = OpenMap();
        float hex = map.Hexes.hex_size_km * 1000f;
        var byId = map.Settlements.ToDictionary(s => s.Id);
        float R(string id) => SettlementKits.FootprintRadiusM(byId[id], hex);
        float hexArea = 1.5f * Mathf.Sqrt(3f) * hex * hex;
        // Area in hexes = people / PeoplePerHex (Tel Aviv 166,660 in 1945: about 2.8 hexes), floored for the smallest places.
        Assert.AreEqual(166660f / SettlementKits.PeoplePerHex, Mathf.PI * R("tel_aviv") * R("tel_aviv") / hexArea, 0.01f);
        Assert.AreEqual(94310f / 166660f, (R("jaffa") * R("jaffa")) / (R("tel_aviv") * R("tel_aviv")), 0.001f, "area ratio = population ratio");
        Assert.Greater(R("jaffa"), R("lod")); Assert.Greater(R("lod"), R("rishon_lezion"));
        Assert.AreEqual(R("hanita"), R("revivim"), 1e-3f, "the floor");
        // The kits root: one merged mesh per settlement with Kit.mat, and more buildings in bigger towns.
        var root = map.transform.Find(KitLayer.RootName); Assert.IsNotNull(root);
        var kit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kit.mat");
        foreach (var s in map.Settlements)
        {
            var t = root.Find(s.Id); Assert.IsNotNull(t, s.Id);
            var r = t.GetComponent<MeshRenderer>(); Assert.AreSame(kit, r.sharedMaterial, s.Id);
            Assert.Greater(t.GetComponent<MeshFilter>().sharedMesh.vertexCount, 0, s.Id);
            Assert.IsFalse(map.transform.Find("Settlements").Find(s.Id).GetComponent<MeshRenderer>().enabled, s.Id + ": the marker box is hidden");
        }
        int V(string id) => root.Find(id).GetComponent<MeshFilter>().sharedMesh.vertexCount;
        Assert.Greater(V("tel_aviv"), V("petah_tikva")); Assert.Greater(V("jaffa"), V("lod")); Assert.Greater(V("petah_tikva"), V("hanita"));
        TestContext.WriteLine(map.KitStats);
    }

    /// <summary>Review N1: with the kits on, a map without its population file refuses to build in the editor (a player
    /// would warn and give every town the smallest footprint).</summary>
    [Test]
    public void AMissingPopulationFileIsAnErrorInTheEditor()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        var inputs = Statecraft.Game.Boot.GameInputs.MapFromDisk(Statecraft.Game.Paths.Content, Statecraft.Game.Paths.StreamingMap);
        inputs.PopulationYaml = "";
        var ex = Assert.Throws<System.IO.InvalidDataException>(() => map.Build(inputs));
        StringAssert.Contains(KitPopulation.FileName, ex.Message);
        map.ConfigureKits(null, null);   // without the kits the file is not needed (the scene is not saved)
        Assert.DoesNotThrow(() => map.Build(inputs));
    }

    /// <summary>Review N2: no two towns' lots overlap, landmarks (mosques, water towers, Jaffa's clock tower and church)
    /// included: a landmark looks for a free spot near its town's heart and is left out when there is none.</summary>
    [Test]
    public void NoTownsBuildingsOrLandmarksOverlapAnotherTowns()
    {
        var map = OpenMap();
        var built = SettlementKits.Build(map.Settlements, Ground(map), out var lots);
        int overlaps = 0;
        for (int i = 0; i < lots.Count; i++)
            for (int j = i + 1; j < lots.Count; j++)
                if (lots[i].id != lots[j].id && (lots[i].centre - lots[j].centre).magnitude < lots[i].radius + lots[j].radius - 1f) overlaps++;
        foreach (var k in built) Object.DestroyImmediate(k.mesh);
        TestContext.WriteLine($"{lots.Count} lots over {lots.Select(l => l.id).Distinct().Count()} towns");
        Assert.AreEqual(0, overlaps, "lots of different towns overlap");
        Assert.AreEqual(map.Settlements.Count, lots.Select(l => l.id).Distinct().Count(), "every town claimed ground");
    }

    /// <summary>Integration bug B9: palms grew out of Tel Aviv's and Jaffa's roofs and grove rows ran through blocks. No tree
    /// instance stands inside, or reaches with its crown into, any building's or landmark's lot.</summary>
    [Test]
    public void NoTreeGrowsThroughABuildingOrLandmark()
    {
        var map = OpenMap();
        var g = Ground(map);
        var built = SettlementKits.Build(map.Settlements, g, out var lots);
        foreach (var k in built) Object.DestroyImmediate(k.mesh);
        foreach (var l in lots) g.Lots.Add((l.centre, l.radius));
        var trees = Vegetation.Plant(map.Settlements, g);
        int inside = 0, touching = 0;
        foreach (var (kind, m) in trees)
        {
            Vector3 p = m.GetColumn(3); float scale = ((Vector3)m.GetColumn(0)).magnitude;
            foreach (var l in lots)
            {
                float d = (new Vector2(p.x, p.z) - l.centre).magnitude;
                if (d < l.radius) inside++;
                else if (d < l.radius + Vegetation.CrownRadiusM(kind) * scale) touching++;
            }
        }
        TestContext.WriteLine($"{trees.Count} trees against {lots.Count} lots: {inside} inside a lot, {touching} crowns reaching one");
        Assert.AreEqual(0, inside, "trees standing inside a building's or landmark's lot");
        Assert.AreEqual(0, touching, "crowns reaching into a lot");
        Assert.Greater(trees.Count, 3000, "the groves and hills still have their trees");
    }

    /// <summary>Review M6: switching the kits off shows the marker boxes again, so a settlement is never left undrawn.</summary>
    [Test]
    public void TheKitsToggleSwapsKitsAndMarkers()
    {
        var map = OpenMap();
        var kits = map.transform.Find(KitLayer.RootName).GetComponentsInChildren<MeshRenderer>();
        var markers = map.transform.Find("Settlements").GetComponentsInChildren<MeshRenderer>();
        Assert.IsTrue(kits.All(r => r.enabled)); Assert.IsTrue(markers.All(r => !r.enabled));
        VisualToggles.Set("kits", false);
        Assert.IsTrue(kits.All(r => !r.enabled)); Assert.IsTrue(markers.All(r => r.enabled), "the markers come back");
        VisualToggles.Set("kits", true);
        Assert.IsTrue(kits.All(r => r.enabled)); Assert.IsTrue(markers.All(r => !r.enabled));
    }

    /// <summary>Review I2: the populations come from content/map/settlement_population_1945.yaml through the map inputs.</summary>
    [Test]
    public void ThePopulationFileLoadsAndRejectsBadRows()
    {
        var inputs = Statecraft.Game.Boot.GameInputs.MapFromDisk(Statecraft.Game.Paths.Content, Statecraft.Game.Paths.StreamingMap);
        var table = KitPopulation.Parse(inputs.PopulationYaml);
        Assert.AreEqual(166660, table["tel_aviv"]); Assert.AreEqual(94310, table["jaffa"]); Assert.AreEqual(5570, table["beersheba"]);
        Assert.IsFalse(table.ContainsKey("revivim"), "a place the source does not list has no figure");
        Assert.AreEqual(23, table.Count);
        OpenMap();
        Assert.AreEqual(166660, KitPopulation.Of("tel_aviv"), "MapScene.Build hands the table to the kits");
        Assert.AreEqual(0, KitPopulation.Of("revivim"));
        const string src = "  sources:\n  - url: https://example.org/x\n    checked: '2026-10-11'\n";
        void Bad(string yaml, string expect)
        {
            var ex = Assert.Throws<System.IO.InvalidDataException>(() => KitPopulation.Parse(yaml, "pop.yaml"));
            StringAssert.Contains("pop.yaml", ex.Message); StringAssert.Contains(expect, ex.Message);
        }
        Bad("settlements:\n- id: a\n  population: -1\n  status: owner-verify\n" + src, "negative");
        Bad("settlements:\n- id: a\n  population: 1\n  status: maybe\n" + src, "status");
        Bad("settlements:\n- id: a\n  population: 1\n  status: owner-verify\n  sources: []\n", "sources");
        Bad("settlements:\n- id: a\n  population: 1\n  status: owner-verify\n" + src + "- id: a\n  population: 2\n  status: owner-verify\n" + src, "duplicate");
        Bad("settlements:\n- id: A\n  population: 1\n  status: owner-verify\n" + src, "snake_case");
        Bad("settlements:\n- id: a\n  people: 1\n", "people");
    }

    [Test]
    public void KitsAndTreesStandOnLand()
    {
        var map = OpenMap();
        var g = Ground(map);
        var root = map.transform.Find(KitLayer.RootName);
        foreach (var s in map.Settlements)
        {
            var b = root.Find(s.Id).GetComponent<MeshRenderer>().bounds;
            Assert.Less(b.size.x, 2.6f * SettlementKits.FootprintRadiusM(s, g.HexSizeM) + 2000f, s.Id + ": the town stays within its footprint");
        }
        var trees = Vegetation.Plant(map.Settlements, g);
        Assert.Greater(trees.Count(t => t.kind == TreeKind.Citrus) + 4 * trees.Count(t => t.kind == TreeKind.CitrusPatch), 1000); Assert.Greater(trees.Count(t => t.kind == TreeKind.Olive), 1000);
        Assert.Greater(trees.Count(t => t.kind == TreeKind.Palm), 50); Assert.Greater(trees.Count(t => t.kind == TreeKind.Cypress), 50);
        int wet = 0, inTown = 0;
        foreach (var (kind, m) in trees)
        {
            Vector3 p = m.GetColumn(3);
            if (g.Water(p.x, p.z)) wet++;
            Assert.AreEqual(g.Y(p.x, p.z), p.y, 0.5f, "rooted on the ground");
            if (kind != TreeKind.CitrusPatch) continue;
            // Every tree of a 2x2 patch, not only its centre, stands on land and outside the towns (review M2).
            float d = Vegetation.GroveStepM * 0.5f;
            foreach (var local in new[] { new Vector3(-d, 0, -d), new Vector3(d, 0, -d), new Vector3(-d, 0, d), new Vector3(d, 0, d) })
            {
                var t = m.MultiplyPoint3x4(local);
                if (g.Water(t.x, t.z)) wet++;
                if (g.InTown(t.x, t.z, 0.85f)) inTown++;
            }
        }
        Assert.AreEqual(0, wet, "trees in the water");
        Assert.AreEqual(0, inTown, "patch trees inside a town");
    }

    /// <summary>The WebGL checkpoint drew every palm black: a frond's tip quad collapsed to a point, its edge-based normal was
    /// zero and normalize(0) is NaN on WebGL 2 (the tilt-shift then smeared it into black blocks). No part may have one.</summary>
    [Test]
    public void EveryPartHasAUnitNormal()
    {
        foreach (TreeKind k in System.Enum.GetValues(typeof(TreeKind))) Assert.AreEqual(0, Vegetation.Build(k).BadNormals, k.ToString());
        var km = new KitMesh(); var rnd = new KitRandom("parts");
        km.Box(0, 0, 17f, 500f, 300f, -50f, 0f, 400f, new Color32(255, 255, 255, 255), new Color32(200, 200, 200, 255), Facade.WhiteCity, 25f);
        km.Gable(0, 0, 17f, 500f, 300f, 400f, 120f, new Color32(200, 80, 50, 255), new Color32(255, 255, 255, 255));
        km.Pyramid(0, 0, 0f, 100f, 100f, 0f, 80f, new Color32(255, 255, 255, 255));
        km.Dome(0, 0, 0f, 100f, new Color32(255, 255, 255, 255));
        km.Shaft(Vector3.zero, Vector3.up * 300f, 30f, 20f, new Color32(255, 255, 255, 255), 8);
        km.Cone(Vector3.zero, 30f, 40f, new Color32(255, 255, 255, 255), 8);
        km.Crown(Vector3.zero, Vector3.one * 50f, new Color32(40, 80, 30, 255), ref rnd);
        km.Frond(Vector3.zero, Vector3.right, 100f, 25f, 0.7f, new Color32(40, 80, 30, 255));
        Assert.AreEqual(0, km.BadNormals);
    }

    [Test]
    public void TheKitShaderRunsOnWebGL2()
    {
        var shader = Shader.Find("Statecraft/Kit"); Assert.IsNotNull(shader);
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(shader));
        var tree = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tree.mat");
        Assert.AreSame(shader, tree.shader); Assert.IsTrue(tree.enableInstancing, "the trees are GPU-instanced");
    }

    [UnityTest]
    public IEnumerator TheTreesAreDrawnAtTheGameplayPoseAndThinnedAtTheOverview()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var veg = map.GetComponentInChildren<VegetationLayer>(); Assert.IsNotNull(veg);
        PromoPose.Gameplay03.Apply(rig, map.Frame); RenderSample.Render(cam, 320, 180);
        int near = veg.LastDrawn;
        PromoPose.Diorama02.Apply(rig, map.Frame); RenderSample.Render(cam, 320, 180);
        int far = veg.LastDrawn;
        TestContext.WriteLine($"trees {veg.Count}: drawn at gameplay03 {near}, at diorama02 {far}");
        Assert.Greater(near, 1000); Assert.Less(far, veg.Count * 0.6f, "the overview thins the far tiles");
        VisualToggles.Set("trees", false); RenderSample.Render(cam, 320, 180);
        Assert.AreEqual(far, veg.LastDrawn, "the trees toggle stops the draws (no new submission)");
    }

    /// <summary>Review M5: the hex grid stays readable through the towns and groves at gameplay03. Midpoints of the land
    /// hex edges in view; at each, the line shows when switching the grid off changes the 3x3 neighbourhood's luma by more
    /// than 3/255 (HexGridTests' threshold). With kits and trees on, at least <see cref="GridReadableShare"/> of the
    /// midpoints whose line shows on bare ground must still show it (the rest are covered by a building or a crown).</summary>
    public const float GridReadableShare = 0.75f;   // measured 84% (76 of 90) at the 2026-10-11 defaults

    [UnityTest]
    public IEnumerator TheGridStaysReadableThroughTownsAndGroves()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        RenderSample.TiltShift(false);
        float size = map.Hexes.hex_size_km * 1000f;
        var origin = map.Terrain.transform.position; var g = Ground(map);
        var look = PromoPose.Gameplay03; var c = map.Frame.ToWorld(look.Lon, look.Lat, 0);
        var mids = new List<Vector3>(); var seen = new HashSet<(int, int)>();
        foreach (var h in map.Hexes.hexes)
        {
            if (!h.land) continue;
            var hc = HexGrid.Centre(h.q, h.r, size);
            for (int i = 0; i < 6; i++)
            {
                var m2 = (HexMath.Corner(hc, size, i) + HexMath.Corner(hc, size, (i + 1) % 6)) * 0.5f;
                var w = HexGrid.ToWorld(m2, origin, map.Frame.LengthM);
                if (Mathf.Abs(w.x - c.x) > 30000f || Mathf.Abs(w.z - c.z) > 30000f || g.Water(w.x, w.z)) continue;
                if (!seen.Add((Mathf.RoundToInt(w.x / 10f), Mathf.RoundToInt(w.z / 10f)))) continue;
                w.y = g.Y(w.x, w.z); mids.Add(w);
            }
        }
        var px = new Vector2[mids.Count];
        bool[] Shows(bool kits)
        {
            VisualToggles.Set("kits", kits); VisualToggles.Set("trees", kits);
            var on = RenderSample.Render(cam, 1920, 1080, mids.ToArray(), px);
            VisualToggles.Set("grid", false);
            var off = RenderSample.Render(cam, 1920, 1080, mids.ToArray(), px);
            VisualToggles.Set("grid", true);
            var shows = new bool[mids.Count];
            for (int i = 0; i < mids.Count; i++)
            {
                int x = (int)px[i].x, y = (int)px[i].y;
                if (x < 2 || y < 2 || x > 1917 || y > 1077) continue;
                float change = 0f;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        change = Mathf.Max(change, Mathf.Abs(RenderSample.Luma(on.At(x + dx, y + dy)) - RenderSample.Luma(off.At(x + dx, y + dy))));
                shows[i] = change > 0.012f;
            }
            return shows;
        }
        var bare = Shows(false); var built = Shows(true);
        int baseCount = bare.Count(b => b), kept = Enumerable.Range(0, mids.Count).Count(i => bare[i] && built[i]);
        float share = kept / (float)Mathf.Max(1, baseCount);
        TestContext.WriteLine($"gameplay03: {mids.Count} land edge midpoints in range, line shows at {baseCount} on bare ground, {kept} of them with kits and trees on ({share:P0})");
        Assert.Greater(baseCount, 50, "enough edges in view");
        Assert.GreaterOrEqual(share, GridReadableShare, "the grid is buried under the kits");
    }

    /// <summary>Issue #37 (review I3): with the sun's own URP bias, unchanged, a kit's shadow starts at its walls and its lit
    /// faces show no acne. A lone kit box on the open coastal plain at the hero (12 km), gameplay (60 km) and overview
    /// (250 km) zooms, on both tiers. Walking out from the shadow-side wall along the sun's travel, from 1 m in 1 m steps
    /// to 20 m and then in steps of a twentieth of a shadow texel, the gap is the distance to the first ground sample that
    /// is clearly in shadow (darker, shadows on against off, by 60% of the deep shadow's darkening); it must stay within one
    /// screen pixel, so no lit strip can show. The sunlit roof must not darken with shadows on (acne). The old
    /// marker material (URP Lit) is measured alongside for the record.</summary>
    [UnityTest]
    public IEnumerator AKitShadowStartsAtItsWalls([Values(0, 1)] int level, [Values(12000f, 60000f, 250000f)] float distance)
    {
        int previous = QualitySettings.GetQualityLevel();
        QualitySettings.SetQualityLevel(level, false);   // 0 = the web tier (1 cascade, bias 1 / 1), 1 = desktop
        try { yield return Measure(level, distance); }
        finally { QualitySettings.SetQualityLevel(previous, false); }
    }

    IEnumerator Measure(int level, float distance)
    {
        var map = OpenMap();
        yield return null;
        foreach (var n in VisualToggles.Names.Where(n => n != "shadows")) VisualToggles.Set(n, false);
        var g = Ground(map);
        var at = map.Frame.ToWorld(34.70, 31.70, 0);
        float w = distance > 100000f ? 1800f : distance > 30000f ? 600f : 300f, h = w;
        var km = new KitMesh();
        float y = g.Y(at.x, at.z);
        km.Box(at.x, at.z, 0f, w, w, y - 30f, y, h, new Color32(235, 230, 220, 255), new Color32(200, 200, 190, 255), Facade.Plain, 25f);
        var mesh = km.ToMesh("ShadowProbe");
        var kit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kit.mat");
        var marker = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SettlementMarker.mat");
        var go = new GameObject("ShadowProbe"); go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = kit;
        try
        {
            var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
            rig.LookAt = at; rig.Distance = distance; rig.Yaw = 0f; rig.TopInset = 0f; rig.Apply();
            var sun = MapLighting.FindSun()!; var fall = sun.transform.forward; fall.y = 0f; fall.Normalize();
            // Distance from the centre to the wall along the fall (the fall crosses the wall facing it).
            float toWall = 0.5f * w / Mathf.Max(Mathf.Abs(fall.x), Mathf.Abs(fall.z));
            float texel = 2f * ShadowFit.Distance(cam, UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset!.cascadeBorder) / 2048f;
            var offsets = new List<float>();
            for (int i = 1; i <= 20; i++) offsets.Add(i);
            for (int i = 1; i <= 40; i++) offsets.Add(20f + texel * i / 20f);
            var pts = new List<Vector3>();
            foreach (var o in offsets) { var p = at + fall * (toWall + o); p.y = g.Y(p.x, p.z); pts.Add(p); }
            var deep = at + fall * (toWall + h * 1.2f); deep.y = g.Y(deep.x, deep.z); pts.Add(deep);
            // Two roof samples (the sunlit face the camera sees from this pose; the west wall is edge-on to it): centre and
            // near the sunward edge, where self-shadow acne starts first.
            var roof = new Vector3(at.x, y + h, at.z); pts.Add(roof);
            var roofEdge = new Vector3(at.x, y + h, at.z) - fall * (toWall * 0.85f); pts.Add(roofEdge);
            int deepI = offsets.Count, roofI = deepI + 1, wallI = deepI + 2;   // wallI: the roof's sunward edge
            var px = new Vector2[pts.Count];
            float Gap(Material m, out float deepDark, out float wallDark, out float roofDark)
            {
                mr.sharedMaterial = m;
                VisualToggles.Set("shadows", true); var on = RenderSample.Render(cam, 1920, 1080, pts.ToArray(), px);
                VisualToggles.Set("shadows", false); var off = RenderSample.Render(cam, 1920, 1080, pts.ToArray(), px);
                VisualToggles.Set("shadows", true);
                float Dark(int i) => 1f - RenderSample.Luma(on.At((int)px[i].x, (int)px[i].y)) / Mathf.Max(1e-4f, RenderSample.Luma(off.At((int)px[i].x, (int)px[i].y)));
                deepDark = Dark(deepI); wallDark = Dark(wallI); roofDark = Dark(roofI);
                for (int i = 0; i < offsets.Count; i++) if (Dark(i) > 0.6f * deepDark) return offsets[i];
                return float.PositiveInfinity;
            }
            float kitGap = Gap(kit, out var kitDeep, out var kitWall, out var kitRoof);
            float markerGap = Gap(marker, out var markerDeep, out _, out _);
            float metresPerPixel = 2f * distance * Mathf.Tan(CameraRig.Fov * 0.5f * Mathf.Deg2Rad) / 1080f;
            TestContext.WriteLine($"tier {QualitySettings.names[level]}, {distance / 1000f:0} km, {w:0} m box, shadow texel {texel:0} m, pixel {metresPerPixel:0.0} m: " +
                $"first shadowed sample {kitGap:0} m from the wall with Kit.mat (sun's bias), {markerGap:0} m with the marker material; deep shadow darker by " +
                $"{kitDeep:P0} / {markerDeep:P0}; with shadows on the roof darkens {kitRoof:P1} at its centre and {kitWall:P1} by its sunward edge");
            Assert.Greater(kitDeep, 0.15f, "the box casts a shadow at all");
            Assert.LessOrEqual(kitGap, metresPerPixel + 1f, "no lit strip between the wall and its shadow");
            Assert.Less(kitRoof, 0.05f, "acne on the roof"); Assert.Less(kitWall, 0.05f, "acne by the roof's sunward edge");
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(mesh); }
    }
}
