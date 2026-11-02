#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>#237: the 1949 armistice lines drawn as their true polylines (content/map/armistice_lines_1949.geojson), a map
/// mode beside the hex-snapped bands. Owner ruling of 30 October 2026: display only, no sim change, the Jordan lines styled
/// as low accuracy, the two Syrian DMZ parts that hold no hex centre included.</summary>
public class ArmisticeLinesTests
{
    static string GeoText => File.ReadAllText(Path.Combine(Paths.Content, "map", ArmisticeLines.FileName));
    /// <summary>The feature ids, read off the file with a pattern rather than the parser under test.</summary>
    static List<string> FileIds => Regex.Matches(GeoText, "\"properties\":\\s*\\{\\s*\"id\":\\s*\"([a-z0-9_]+)\"").Select(m => m.Groups[1].Value).ToList();
    static readonly DateTime After = new DateTime(1951, 1, 1);
    /// <summary>The 11 features the owner signed off on 23 October 2026 (#196, #1444; docs/m3-release.md, "Map lines") and
    /// the five flagged then, by id: a player's map draws exactly the first list.</summary>
    static readonly string[] SignedOff = { "lebanon_adl", "syria_dmz_north", "syria_dmz_central", "syria_dmz_south", "egypt_el_auja_dmz",
        "jordan_adl_iraqi_a_triangle", "jordan_adl_iraqi_b_wadi_ara", "jordan_adl_iraqi_c_east", "jordan_adl_iraqi_c_south",
        "jordan_adl_bc_south", "jordan_adl_d_arava" };
    static readonly string[] Drafts = { "egypt_adl_gaza", "egypt_mv1950_demarcation", "egypt_mv1950_zone_lines", "jordan_adl_a_latrun", "syria_adl" };

    static MapScene? map;
    static MapScene Map => map != null ? map : map = RouteTests.BuildMap();

    [OneTimeTearDown]
    public void DiscardScene()
    {
        map = null;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear();
    }

    static Material Selection => AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSelection.mat");
    static GameObject Overlay(DateTime date) =>
        ArmisticeLines.Build(Map.Terrain, Map.Frame, ArmisticeLines.Parse(GeoText), Selection, date);

    static Dictionary<string, ArmisticeLine> Drawn(GameObject root) =>
        root.GetComponentsInChildren<ArmisticeLine>(true).ToDictionary(l => l.Id);

    [Test]
    public void EveryFeatureOfTheTrueLinesIsDrawnAsAPolyline()
    {
        var ids = FileIds;
        Assert.AreEqual(16, ids.Count, "the committed file's features");
        CollectionAssert.IsSubsetOf(new[] { "syria_dmz_north", "syria_dmz_south" }, ids, "the two DMZ parts with no hex centre");
        var root = Overlay(After);
        try
        {
            var drawn = Drawn(root);
            var features = ArmisticeLines.Parse(GeoText);
            CollectionAssert.AreEquivalent(ids, drawn.Keys, "one drawn polyline per feature");
            foreach (var (id, line) in drawn)
            {
                Assert.IsTrue(line.gameObject.activeInHierarchy, $"{id} is shown in 1951");
                var mesh = line.GetComponent<MeshFilter>().sharedMesh;
                Assert.Greater(mesh.vertexCount, 0, $"{id} has geometry");
                // On the ground where the line is: the mesh's footprint holds the feature's own points.
                var b = mesh.bounds;
                var feature = features.First(f => f.Id == id);
                foreach (var part in feature.Parts)
                {
                    var p = Map.Frame.ToWorld(part[0].lon, part[0].lat, 0); float w = ArmisticeLines.LowAccuracyWidthM;
                    Assert.IsTrue(p.x >= b.min.x - w && p.x <= b.max.x + w && p.z >= b.min.z - w && p.z <= b.max.z + w, $"{id} is drawn where its points are");
                }
            }
            // The two narrow Syrian DMZ parts are closed outlines, so they read as areas, not as lines.
            foreach (var id in new[] { "syria_dmz_north", "syria_dmz_south" })
            {
                var part = features.First(f => f.Id == id).Parts.Single();
                Assert.AreEqual(part[0], part[part.Count - 1], $"{id}'s ring is closed");
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void TheJordanLinesAreDrawnInTheLowAccuracyStyleAndNoOtherIs()
    {
        var features = ArmisticeLines.Parse(GeoText);
        var low = features.Where(f => f.LowAccuracy).Select(f => f.Id).ToList();
        CollectionAssert.AreEquivalent(FileIds.Where(id => id.StartsWith("jordan_")).ToList(), low,
            "exactly the features traced from the low-resolution scan (source_quality: low_res_scan)");
        Assert.AreEqual(7, low.Count, "the seven Jordan features");
        var root = Overlay(After);
        try
        {
            var drawn = Drawn(root);
            var lowColour = drawn[low[0]].GetComponent<MeshRenderer>().sharedMaterial.GetColor("_BaseColor");
            var firmColour = drawn["lebanon_adl"].GetComponent<MeshRenderer>().sharedMaterial.GetColor("_BaseColor");
            Assert.AreNotEqual(firmColour, lowColour, "the low-accuracy style has its own colour");
            foreach (var (id, line) in drawn)
            {
                Assert.AreEqual(low.Contains(id), line.LowAccuracy, id);
                Assert.AreEqual(low.Contains(id) ? lowColour : firmColour, line.GetComponent<MeshRenderer>().sharedMaterial.GetColor("_BaseColor"), id);
            }
        }
        finally { Object.DestroyImmediate(root); }
        // ...and is dashed, a cue that is not colour: the dashes cover DashOn of the line, in pieces of the dash's length.
        var straight = new List<Vector2> { Vector2.zero, new Vector2(10000f, 0f) };
        var dashes = ArmisticeLines.Dashes(straight);
        Assert.Greater(dashes.Count, 4);
        float covered = dashes.Sum(d => (d[d.Count - 1] - d[0]).magnitude);
        Assert.AreEqual(10000f * ArmisticeLines.DashOn, covered, ArmisticeLines.DashM * ArmisticeLines.DashOn);
        Assert.IsTrue(dashes.All(d => (d[d.Count - 1] - d[0]).magnitude <= ArmisticeLines.DashM * ArmisticeLines.DashOn + 1f));
        // A dash runs on round a bend: on an L whose first leg is 100 m shorter than a dash, the first dash keeps the corner
        // and its full length.
        float leg = ArmisticeLines.DashM * ArmisticeLines.DashOn - 100f;
        var bent = ArmisticeLines.Dashes(new List<Vector2> { Vector2.zero, new Vector2(leg, 0f), new Vector2(leg, 2000f) });
        CollectionAssert.Contains(bent[0], new Vector2(leg, 0f), "the dash over the corner keeps the corner");
        float first = 0f; for (int i = 1; i < bent[0].Count; i++) first += (bent[0][i] - bent[0][i - 1]).magnitude;
        Assert.AreEqual(ArmisticeLines.DashM * ArmisticeLines.DashOn, first, 1f, "a full dash, measured along the bend");
    }

    /// <summary>Owner, 2 November 2026 (#2195): "Restyle Jordan lines (Recommended)". The low-accuracy lines are drawn
    /// dotted: each mark no longer than the line is wide, the gaps at least as long, and a mark a quarter or less of the
    /// bands' grey dash for no-man's land and the demilitarized zones (MapBorders.NeutralDashM, NeutralDashOn), so the
    /// legend's "No-man's land or demilitarized zone: grey, dashed" stays true of the dashes alone.</summary>
    [Test]
    public void TheLowAccuracyLinesAreDottedApartFromTheNeutralDash()
    {
        float mark = ArmisticeLines.DashM * ArmisticeLines.DashOn, gap = ArmisticeLines.DashM - mark;
        Assert.LessOrEqual(mark, ArmisticeLines.LowAccuracyWidthM, "a dot: no longer than the line is wide");
        Assert.GreaterOrEqual(gap, mark, "the gaps are at least as long as the dots");
        Assert.LessOrEqual(mark, MapBorders.NeutralDashM * MapBorders.NeutralDashOn / 4f, "a quarter of the neutral dash or less");
    }

    /// <summary>Owner, 2 November 2026 (#2195): "Tie to Borders button (Recommended)". The minimap's Borders button hides
    /// the true lines with the bands, and pressing it again shows them.</summary>
    [Test]
    public void TheMinimapBordersButtonHidesTheLinesAndShowsThemAgain()
    {
        var root = Overlay(After);
        try
        {
            using var ui = new UiTestKit();
            using var mini = new MinimapPresenter(ui.Root, Map.Hexes);
            var lines = root.transform.Find("Lines").gameObject;
            Assert.IsTrue(lines.activeSelf, "precondition: the lines are shown");
            ui.Press("minimap-borders");
            Assert.IsFalse(mini.BordersShown);
            Assert.IsFalse(lines.activeSelf, "Borders off hides the true lines");
            ui.Press("minimap-borders");
            Assert.IsTrue(mini.BordersShown);
            Assert.IsTrue(lines.activeSelf, "Borders on shows them again");
        }
        finally
        {
            Object.DestroyImmediate(root);
            // The presses also work the shared map's bands (MapBorders' switch, which keeps its state): leave them on for
            // the next test however this one ended (review of #2195, C5).
            if (VisualToggles.Names.Contains(MapBorders.ToggleName)) VisualToggles.Set(MapBorders.ToggleName, true);
        }
    }

    [Test]
    public void EachLineIsShownFromItsOwnDate()
    {
        var root = Overlay(new DateTime(1949, 3, 1));
        try
        {
            var drawn = Drawn(root);
            // From the Egypt agreement (24 Feb 1949) only its 1949 features; the 1950 modus vivendi lines and the later fronts wait.
            CollectionAssert.AreEquivalent(new[] { "egypt_adl_gaza", "egypt_el_auja_dmz" },
                drawn.Values.Where(l => l.gameObject.activeSelf).Select(l => l.Id).ToArray());
            ArmisticeLines.ShowAt(root, new DateTime(1949, 7, 20));
            Assert.IsTrue(drawn["syria_dmz_north"].gameObject.activeSelf && drawn["syria_dmz_south"].gameObject.activeSelf);
            Assert.IsFalse(drawn["egypt_mv1950_demarcation"].gameObject.activeSelf);
            ArmisticeLines.ShowAt(root, After);
            Assert.IsTrue(drawn.Values.All(l => l.gameObject.activeSelf));
        }
        finally { Object.DestroyImmediate(root); }
    }

    /// <summary>Display only, and on unless turned off (owner, 1 November 2026: "On by default (Recommended)"): the map
    /// builds the overlay with no flag, "noarmisticelines" turns it off (the old "armisticelines" links still show it),
    /// nothing in the sim reads the file (the content hash), and drawing it leaves the hex-snapped layer and its baked data
    /// as they were.</summary>
    [Test]
    public void TheOverlayIsDisplayOnlyAndOnUnlessTurnedOff()
    {
        Assert.IsNotNull(Map.Terrain.transform.Find(ArmisticeLines.RootName), "the overlay with no flag");
        Assert.IsTrue(ArmisticeLines.Requested("https://x/index.html", new string[0], true));
        Assert.IsTrue(ArmisticeLines.Requested(null, new[] { "game" }, false));
        Assert.IsTrue(ArmisticeLines.Requested("https://x/index.html?armisticelines", new string[0], true), "an old link still shows it");
        Assert.IsTrue(ArmisticeLines.Requested(null, new[] { "game", "-armisticelines" }, false));
        Assert.IsFalse(ArmisticeLines.Requested("https://x/index.html?noarmisticelines", new string[0], true), "turned off");
        Assert.IsFalse(ArmisticeLines.Requested("https://x/index.html?lang=he&noarmisticelines#x", new string[0], true));
        Assert.IsFalse(ArmisticeLines.Requested(null, new[] { "game", "-noarmisticelines" }, false));
        Assert.IsTrue(ArmisticeLines.Requested("https://x/index.html?noarmisticelinesx", new string[0], true), "a whole flag only");

        // The hex bands are drawn from the RegionHexes texture (MapBorders); drawing the overlay leaves every texel as it was
        // (a layer is drawn first, so the texels are not all empty).
        BorderProof.Show(Map);
        var regionTex = (Texture2D)Shader.GetGlobalTexture("_ScRegionTex");
        var texels = regionTex.GetPixels32();
        var root = Overlay(After);
        try
        {
            Assert.AreSame(regionTex, Shader.GetGlobalTexture("_ScRegionTex"));
            CollectionAssert.AreEqual(texels, regionTex.GetPixels32(), "the hex bands are untouched");
            CollectionAssert.Contains(VisualToggles.Names, ArmisticeLines.ToggleName);
            VisualToggles.Set(ArmisticeLines.ToggleName, false);
            Assert.IsFalse(root.transform.GetChild(0).gameObject.activeInHierarchy, "the toggle hides it");
            VisualToggles.Set(ArmisticeLines.ToggleName, true);
        }
        finally { Object.DestroyImmediate(root); MapBorders.Use(null); }

        var copy = Path.Combine(Path.GetTempPath(), "statecraft-armlines-" + Guid.NewGuid().ToString("N"));
        try
        {
            ContentManifest.Copy(Paths.Content, copy);
            File.WriteAllText(Path.Combine(copy, "map", ArmisticeLines.FileName), "{}");
            Assert.AreEqual(ContentLoader.Load(Paths.Content).Hash, ContentLoader.Load(copy).Hash, "the sim never reads the true lines");
        }
        finally { if (Directory.Exists(copy)) Directory.Delete(copy, true); }
    }

    /// <summary>A release build draws only the features the owner has signed off (docs/m3-release.md, "Map lines"); both
    /// Syrian DMZ parts are among them. The editor and development builds draw every feature.</summary>
    [Test]
    public void AReleaseBuildDrawsOnlyTheReviewedFeatures()
    {
        var all = ArmisticeLines.Parse(GeoText);
        Assert.AreEqual(16, ArmisticeLines.ForBuild(all, devBuild: true).Count);
        var release = ArmisticeLines.ForBuild(all, devBuild: false).Select(f => f.Id).ToList();
        int reviewed = Regex.Matches(GeoText, "\"status\":\\s*\"sensitivity-reviewed\"").Count;
        Assert.AreEqual(11, reviewed, "the committed file: 11 of 16 signed off (#1444)");
        Assert.AreEqual(reviewed, release.Count);
        CollectionAssert.IsSubsetOf(new[] { "syria_dmz_north", "syria_dmz_south" }, release);
        CollectionAssert.IsNotSubsetOf(new[] { "syria_adl" }, release, "a draft feature waits for the owner's review");
    }

    /// <summary>Owner review of #2012: a release build's data holds no draft feature, in the copied file and in the bundle
    /// the web boot fetches (ContentSync copies with release for a player without the Development option); a development
    /// copy ships the file as it is.</summary>
    [Test]
    public void AReleaseBuildShipsNoDraftFeature()
    {
        var drafts = ArmisticeLines.Parse(GeoText).Where(f => !f.Reviewed).Select(f => f.Id).ToList();
        Assert.AreEqual(5, drafts.Count, "the five flagged on 23 October 2026");
        var target = Path.Combine(Path.GetTempPath(), "statecraft-armlines-release-" + Guid.NewGuid().ToString("N"));
        try
        {
            ContentManifest.Copy(Paths.Content, target, release: true);
            ContentManifest.Verify(Paths.Content, target, release: true);
            var (_, texts) = ContentManifest.ParseBundle(File.ReadAllText(Path.Combine(target, ContentManifest.BundleFileName)));
            foreach (var shipped in new[] { File.ReadAllText(Path.Combine(target, "map", ArmisticeLines.FileName)), texts["map/" + ArmisticeLines.FileName] })
            {
                foreach (var id in drafts) StringAssert.DoesNotContain($"\"id\":\"{id}\"", shipped, $"{id} is not shipped");
                var kept = ArmisticeLines.Parse(shipped);
                Assert.AreEqual(11, kept.Count, "the signed-off features are all shipped");
                Assert.IsTrue(kept.All(f => f.Reviewed));
            }
            ContentManifest.Copy(Paths.Content, target);
            Assert.AreEqual(GeoText, File.ReadAllText(Path.Combine(target, "map", ArmisticeLines.FileName)), "a development copy is the file as it is");
        }
        finally { if (Directory.Exists(target)) Directory.Delete(target, true); }
    }

    /// <summary>Owner review of #2012: a fixed ground width thins to about a pixel at the full-map zoom, so farther out than
    /// the gameplay zoom the lines are drawn wider, the low-accuracy ones still wider (and still dashed); closer in they
    /// keep their ground width.</summary>
    [Test]
    public void FarOutTheLinesAreDrawnWiderAndCloseInAsBefore()
    {
        Assert.AreEqual(1f, ArmisticeLines.WidthScale(30000f), "close in: the ground width");
        Assert.AreEqual(1f, ArmisticeLines.WidthScale(ArmisticeLines.WidthsSetAtM), "the gameplay zoom: the ground width");
        float far = ArmisticeLines.WidthScale(CameraController.MaxDistance);
        Assert.That(far, Is.InRange(2.5f, 3.2f), "the full-map zoom: about as much wider as the camera is farther (250 / 90 km)");
        Assert.AreEqual(far, ArmisticeLines.WidthScale(float.PositiveInfinity));
        // The dashes lengthen with the width, so the dashed lines keep their look.
        var straight = new List<Vector2> { Vector2.zero, new Vector2(20000f, 0f) };
        var firstDash = ArmisticeLines.Dashes(straight, far)[0];
        Assert.AreEqual(ArmisticeLines.DashM * ArmisticeLines.DashOn * far, firstDash[firstDash.Count - 1].x, 1f);

        var root = Overlay(After);
        try
        {
            var drawn = Drawn(root);
            foreach (var id in new[] { "egypt_el_auja_dmz", "jordan_adl_d_arava" })
            {
                var line = drawn[id];
                var filter = line.GetComponent<MeshFilter>();
                line.ShowFor(30000f);
                var close = filter.sharedMesh; var closeSize = close.bounds.size;
                Assert.AreEqual(1f, line.Scale, id);
                line.ShowFor(CameraController.MaxDistance);
                Assert.AreEqual(far, line.Scale, id);
                var farSize = filter.sharedMesh.bounds.size;
                // A wider ribbon: its footprint grows by about the extra width (at least half of it on the wider side).
                float extra = ((line.LowAccuracy ? ArmisticeLines.LowAccuracyWidthM : ArmisticeLines.WidthM) * (far - 1f)) / 2f;
                Assert.Greater(Mathf.Max(farSize.x - closeSize.x, farSize.z - closeSize.z), extra, $"{id} is wider far out");
                line.ShowFor(30000f);
                Assert.AreSame(close, filter.sharedMesh, $"{id}: back in, the same ground-width mesh");
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    /// <summary>The wiring (review of #2012, optional): with the mode asked for, building the map builds the overlay under the
    /// terrain; the other cases call ArmisticeLines.Build directly.</summary>
    [Test]
    public void TheMapBuildsTheOverlayWhenTheModeIsAsked()
    {
        var was = ArmisticeLines.Wanted;
        ArmisticeLines.Wanted = () => true;
        try
        {
            var built = RouteTests.BuildMap();
            var root = built.Terrain.transform.Find(ArmisticeLines.RootName);
            Assert.IsNotNull(root, "Terrain/ArmisticeLines");
            Assert.AreEqual(16, root.GetComponentsInChildren<ArmisticeLine>(true).Length, "the editor draws every feature");
        }
        finally { ArmisticeLines.Wanted = was; map = null; }
    }

    /// <summary>Owner, 1 November 2026: "On by default (Recommended)", with only the signed-off lines for players. A
    /// release player's map, built with no flag from the file as a release build ships it (ContentManifest's release copy),
    /// draws the 11 signed-off features and none of the five drafts.</summary>
    [Test]
    public void APlayersMapDrawsTheSignedOffLinesByDefaultAndNoDraft()
    {
        var all = ArmisticeLines.Parse(GeoText);
        CollectionAssert.AreEquivalent(SignedOff, all.Where(f => f.Reviewed).Select(f => f.Id).ToList(), "the file's signed-off features");
        CollectionAssert.AreEquivalent(Drafts, all.Where(f => !f.Reviewed).Select(f => f.Id).ToList(), "the file's drafts");
        var signedOff = SignedOff; var drafts = Drafts;
        var target = Path.Combine(Path.GetTempPath(), "statecraft-armlines-player-" + Guid.NewGuid().ToString("N"));
        try
        {
            ContentManifest.Copy(Paths.Content, target, release: true);
            var inputs = GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap);
            inputs.ArmisticeLinesGeojson = File.ReadAllText(Path.Combine(target, "map", ArmisticeLines.FileName));
            var built = RouteTests.BuildMap(inputs);
            var root = built.Terrain.transform.Find(ArmisticeLines.RootName);
            Assert.IsNotNull(root, "the overlay is on with no flag");
            var ids = root.GetComponentsInChildren<ArmisticeLine>(true).Select(l => l.Id).ToList();
            CollectionAssert.AreEquivalent(signedOff, ids, "the signed-off features, each once");
            foreach (var id in drafts) CollectionAssert.DoesNotContain(ids, id, $"{id} stays out of a player's map");
        }
        finally { map = null; if (Directory.Exists(target)) Directory.Delete(target, true); }
    }

    [Test]
    public void TheWebGLBuildShipsTheTrueLinesAndTheMapReadsThem()
    {
        CollectionAssert.Contains(ContentManifest.Collect(Paths.Content), "map/" + ArmisticeLines.FileName);
        Assert.AreEqual(GeoText, GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap).ArmisticeLinesGeojson);
    }

    [TestCase("{\"type\": \"FeatureCollection\", \"features\": []}", "no features")]
    [TestCase("{\"features\": [{\"properties\": {\"id\": \"a\", \"shown_from\": \"1949-01-01\"}, \"geometry\": {\"type\": \"Point\", \"coordinates\": [34.5, 31.5]}}]}", "a: geometry 'Point'")]
    [TestCase("{\"features\": [{\"properties\": {\"id\": \"a\"}, \"geometry\": {\"type\": \"LineString\", \"coordinates\": [[34.5, 31.5], [34.6, 31.6]]}}]}", "a: shown_from")]
    [TestCase("{\"features\": [{\"properties\": {\"id\": \"a\", \"shown_from\": \"1949-01-01\"}, \"geometry\": {\"type\": \"LineString\", \"coordinates\": [[34.5, 31.5]]}}]}", "a: a line needs two points")]
    [TestCase("{\"features\": [{\"properties\": {\"id\": \"a\", \"shown_from\": \"1949-01-01\"}, \"geometry\": {\"type\": \"LineString\", \"coordinates\": [[34.5, 31.5], [34.6]]}}]}", "a: a point is not [lon, lat]")]
    [TestCase("not: [json", "armistice_lines_1949.geojson")]
    public void BadFilesNameTheFileAndTheProblem(string text, string expected)
    {
        var e = Assert.Throws<InvalidDataException>(() => ArmisticeLines.Parse(text));
        StringAssert.StartsWith(ArmisticeLines.FileName + ":", e.Message);
        StringAssert.Contains(expected, e.Message);
    }
}
