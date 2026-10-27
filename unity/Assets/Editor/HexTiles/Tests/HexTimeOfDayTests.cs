#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.HexTiles;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>The hex-tile map's time of day (HexTimeOfDay): which preset the URL or the command line asks for, and that
/// each preset's undo puts back exactly what it changed (the grade, the sun, the ambient and the board's lit windows).</summary>
public class HexTimeOfDayTests
{
    VolumeProfile profile = null!;
    Light sun = null!;
    readonly List<Object> made = new List<Object>();

    [SetUp]
    public void Scene()
    {
        // RenderSettings belong to the active scene: a new one per test, lit the way the board is (map light, then HexLook).
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        made.Add(sun.gameObject); made.Add(profile);
    }

    [TearDown] public void Clean() { foreach (var o in made) if (o != null) Object.DestroyImmediate(o); made.Clear(); }

    static IEnumerable<HexTime> All() => (HexTime[])Enum.GetValues(typeof(HexTime));
    static IEnumerable<HexTime> Presets() => All().Where(t => t != HexTime.Day);

    // --- Which preset is asked for ---

    [TestCase("https://x/play?hex-tiles&hex-time=dusk", HexTime.Dusk)]
    [TestCase("https://x/play?hex-time=night&hex-tiles", HexTime.Night)]
    [TestCase("https://x/play?hex-tiles&hex-time=night#top", HexTime.Night)]
    [TestCase("https://x/play?hex-tiles&hex-time=day", HexTime.Day)]
    [TestCase("https://x/play?hex-tiles#hex-time=night", HexTime.Day)]          // after '#': not a query parameter
    [TestCase("https://x/play#?hex-time=night", HexTime.Day)]
    [TestCase("https://x/play?hex-tiles&hex-time", HexTime.Day)]                // the flag alone names no preset
    [TestCase("https://x/play?hex-tiles&hex-time=", HexTime.Day)]
    [TestCase("https://x/play?hex-tiles&hex-time=noon", HexTime.Day)]           // a bad value
    [TestCase("https://x/play?hex-tiles&hex-time=Night", HexTime.Day)]          // ids are lower case
    [TestCase("https://x/play?hex-tiles&hex-time=nightly", HexTime.Day)]
    [TestCase("https://x/play?hex-tiles&old-hex-time=night", HexTime.Day)]      // another parameter's tail
    [TestCase("https://x/play?hex-tiles", HexTime.Day)]
    [TestCase("https://x/play", HexTime.Day)]
    [TestCase("", HexTime.Day)]
    [TestCase(null, HexTime.Day)]
    public void TheWebReadsATopLevelQueryParameter(string? url, HexTime expected) =>
        Assert.That(HexTimeOfDay.Requested(url, new[] { "-hex-time", "night" }, webgl: true), Is.EqualTo(expected));

    [TestCase(new[] { "unity", "-hex-tiles", "-hex-time", "dusk" }, HexTime.Dusk)]
    [TestCase(new[] { "unity", "-hex-time", "night", "-hex-tiles" }, HexTime.Night)]
    [TestCase(new[] { "unity", "-hex-time", "day" }, HexTime.Day)]
    [TestCase(new[] { "unity", "-hex-tiles", "-hex-time" }, HexTime.Day)]           // the flag alone
    [TestCase(new[] { "unity", "-hex-time", "-hex-tiles" }, HexTime.Day)]           // the next argument is another flag
    [TestCase(new[] { "unity", "-hex-time", "dawn" }, HexTime.Day)]                 // a bad value
    [TestCase(new[] { "unity", "-hex-time=night" }, HexTime.Day)]
    [TestCase(new[] { "unity", "-hex-tiles" }, HexTime.Day)]
    [TestCase(new string[0], HexTime.Day)]
    public void TheDesktopReadsTheArgumentAfterTheFlag(string[] args, HexTime expected) =>
        Assert.That(HexTimeOfDay.Requested("https://x/play?hex-time=night", args, webgl: false), Is.EqualTo(expected));

    [Test]
    public void IdsRoundTrip()
    {
        foreach (var t in All())
        {
            Assert.That(HexTimeOfDay.TryParse(HexTimeOfDay.Id(t), out var back), Is.True);
            Assert.That(back, Is.EqualTo(t));
        }
        Assert.That(HexTimeOfDay.TryParse("evening", out var none), Is.False);
        Assert.That(none, Is.EqualTo(HexTime.Day));
    }

    // --- What a preset changes, and its undo ---

    /// <summary>Everything a preset may touch: every volume component and parameter (value and override state), the
    /// sun's light and rotation, the scene's ambient, fog and skybox.</summary>
    string Snapshot(GameObject? root = null)
    {
        var lines = new List<string>();
        foreach (var c in profile.components)
        {
            lines.Add($"{c.GetType().Name} active={c.active}");
            var ps = c.parameters;
            for (int i = 0; i < ps.Count; i++)
            {
                var value = ps[i].GetType().GetProperty("value")?.GetValue(ps[i]);
                lines.Add($"  {i}: {Show(value)} override={ps[i].overrideState}");
            }
        }
        var r = sun.transform.localRotation;
        lines.Add($"sun {sun.intensity:R} {Show(sun.color)} rot=({r.x:R},{r.y:R},{r.z:R},{r.w:R}) shadows={sun.shadows} strength={sun.shadowStrength:R} enabled={sun.enabled}");
        lines.Add($"ambient {RenderSettings.ambientMode} {Show(RenderSettings.ambientSkyColor)} {Show(RenderSettings.ambientEquatorColor)} {Show(RenderSettings.ambientGroundColor)} {Show(RenderSettings.ambientLight)} {RenderSettings.ambientIntensity:R}");
        lines.Add($"fog {RenderSettings.fog} {Show(RenderSettings.fogColor)} {RenderSettings.fogMode} {RenderSettings.fogDensity:R} {RenderSettings.fogStartDistance:R} {RenderSettings.fogEndDistance:R} sky={RenderSettings.skybox} rsun={RenderSettings.sun}");
        if (root != null) lines.AddRange(root.GetComponentsInChildren<Transform>(true).Select(t => "node " + t.name + " " + t.childCount));
        return string.Join("\n", lines);
    }

    static string Show(object? v) => v switch
    {
        null => "null",
        Color c => $"({c.r:R},{c.g:R},{c.b:R},{c.a:R})",
        float f => f.ToString("R"),
        _ => v.ToString(),
    };

    void LightTheBoard(bool grade)
    {
        MapLighting.Apply(sun);
        if (grade) MapGrade.Apply(profile);
        HexLook.Apply(profile, sun);
    }

    [TestCaseSource(nameof(All))]
    public void TheUndoRestoresEverythingOnAFreshProfileAndLight(HexTime time)
    {
        var before = Snapshot();
        var undo = HexTimeOfDay.Apply(time, profile, sun, null);
        undo();
        Assert.That(Snapshot(), Is.EqualTo(before), HexTimeOfDay.Id(time));
        Assert.That(profile.components, Is.Empty, "components the preset added are removed");
    }

    [TestCaseSource(nameof(All))]
    public void TheUndoRestoresEverythingOverTheBoardsLook(HexTime time)
    {
        LightTheBoard(grade: true);
        var before = Snapshot();
        HexTimeOfDay.Apply(time, profile, sun, null)();
        Assert.That(Snapshot(), Is.EqualTo(before), HexTimeOfDay.Id(time));
    }

    [Test]
    public void TheUndoKeepsHexLooksOwnComponents()
    {
        // HexLook adds a WhiteBalance to a profile without one; the time undo, which runs first, must leave it there.
        HexLook.Apply(profile, sun);
        var before = Snapshot();
        HexTimeOfDay.Apply(HexTime.Night, profile, sun, null)();
        Assert.That(Snapshot(), Is.EqualTo(before));
    }

    [TestCaseSource(nameof(Presets))]
    public void APresetChangesTheLook(HexTime time)
    {
        LightTheBoard(grade: true);
        var before = Snapshot();
        var undo = HexTimeOfDay.Apply(time, profile, sun, null);
        Assert.That(Snapshot(), Is.Not.EqualTo(before), HexTimeOfDay.Id(time));
        undo();
    }

    [Test]
    public void DayChangesNothing()
    {
        LightTheBoard(grade: true);
        var root = new GameObject("board"); made.Add(root);
        var before = Snapshot(root);
        var undo = HexTimeOfDay.Apply(HexTime.Day, profile, sun, root, CityBoard());
        Assert.That(Snapshot(root), Is.EqualTo(before));
        undo();
        Assert.That(Snapshot(root), Is.EqualTo(before));
    }

    [Test]
    public void ThePresetsAreDistinct()
    {
        var looks = new List<string>();
        LightTheBoard(grade: true);
        foreach (var t in All())
        {
            var undo = HexTimeOfDay.Apply(t, profile, sun, null);
            looks.Add(Snapshot());
            undo();
        }
        Assert.That(looks.Distinct().Count(), Is.EqualTo(looks.Count));
    }

    [Test]
    public void ThePresetsKeepTheirIdea()
    {
        // The presets keep their idea: night's light is dimmer than dusk's, and bluer than red; dusk's sun is low and warm.
        var dusk = HexTimeOfDay.PresetOf(HexTime.Dusk)!; var night = HexTimeOfDay.PresetOf(HexTime.Night)!;
        Assert.That(HexTimeOfDay.PresetOf(HexTime.Day), Is.Null);
        Assert.That(night.SunIntensity, Is.LessThan(dusk.SunIntensity));
        Assert.That(night.SunColour.b, Is.GreaterThan(night.SunColour.r));
        Assert.That(dusk.SunColour.r, Is.GreaterThan(dusk.SunColour.b));
        Assert.That(dusk.SunElevation, Is.LessThan(MapLighting.SunElevation));
        Assert.That(night.PostExposure, Is.LessThan(HexLook.PostExposure));
        Assert.That(dusk.AmbientSky.b, Is.GreaterThan(dusk.AmbientSky.r), "dusk's ambient is cooler than its sun");
    }

    [Test]
    public void NullsChangeNothingAndDoNotThrow()
    {
        foreach (var t in All())
        {
            var undo = HexTimeOfDay.Apply(t, null, null, null);
            undo();
            undo();   // a second run does no harm
        }
    }

    // --- The board's lit windows ---

    static HexBoard CityBoard(string prototype = "tel_aviv_city") => new HexBoard(new HexBoardFile
    {
        id = "t", hexes = new[] { new BoardHex { q = 0, r = 0, prototype = prototype }, new BoardHex { q = 1, r = 0, prototype = "wheat_fields" } },
    });

    [Test]
    public void NightLightsWindowsInACityAndTheUndoRemovesThem()
    {
        LightTheBoard(grade: false);
        var root = new GameObject("board"); made.Add(root);
        new GameObject("pieces").transform.SetParent(root.transform, false);
        var before = Snapshot(root);
        var undo = HexTimeOfDay.Apply(HexTime.Night, profile, sun, root, CityBoard());
        var windows = root.transform.Find(HexTimeOfDay.WindowsName);
        Assert.That(windows, Is.Not.Null);
        var mesh = windows.GetComponent<MeshFilter>().sharedMesh; var material = windows.GetComponent<MeshRenderer>().sharedMaterial;
        Assert.That(mesh.vertexCount, Is.GreaterThan(0));
        undo();
        Assert.That(Snapshot(root), Is.EqualTo(before));
        Assert.That(mesh == null && material == null, "the windows' mesh and material are destroyed with them");
    }

    [Test]
    public void TheWindowsAreTheSameEachTime()
    {
        var root = new GameObject("board"); made.Add(root);
        Vector3[] Windows()
        {
            var undo = HexTimeOfDay.Apply(HexTime.Night, profile, sun, root, CityBoard());
            var v = root.transform.Find(HexTimeOfDay.WindowsName).GetComponent<MeshFilter>().sharedMesh.vertices;
            undo();
            return v;
        }
        Assert.That(Windows(), Is.EqualTo(Windows()));
    }

    [TestCase(HexTime.Dusk, "tel_aviv_city")]
    [TestCase(HexTime.Night, "wheat_fields")]
    public void NoWindowsAtDuskOrOutsideTowns(HexTime time, string prototype)
    {
        var root = new GameObject("board"); made.Add(root);
        var undo = HexTimeOfDay.Apply(time, profile, sun, root, CityBoard(prototype));
        Assert.That(root.transform.Find(HexTimeOfDay.WindowsName), Is.Null);
        undo();
    }

    [Test]
    public void OnlyLightsTheOneTileACaptureBuilds()
    {
        var root = new GameObject("board"); made.Add(root);
        var undo = HexTimeOfDay.Apply(HexTime.Night, profile, sun, root, CityBoard(), only: "kibbutz");
        Assert.That(root.transform.Find(HexTimeOfDay.WindowsName), Is.Null);
        undo();
    }

    [Test]
    public void ABoardRootWithNoBoardGetsNoWindows()
    {
        var root = new GameObject("board"); made.Add(root);
        var before = Snapshot(root);
        var undo = HexTimeOfDay.Apply(HexTime.Night, profile, sun, root);
        Assert.That(root.transform.Find(HexTimeOfDay.WindowsName), Is.Null);
        undo();
        Assert.That(Snapshot(root), Is.EqualTo(before));
    }
}
