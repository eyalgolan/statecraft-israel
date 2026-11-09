#nullable enable
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Writes the before and after stills for #1722 (the gdd-0.3 city panel's project list and housing readout), in
/// English and Hebrew, at 1024x768 and 1366x768, to `.superpowers/shots-1722/&lt;SHOTS_TAG&gt;/` at the repository root (local,
/// not committed): Assets/Map.unity with its UI drawn through a copy of its PanelSettings into a transparent texture laid over
/// the main camera's frame, as PanelPreviewShots does. Each game is a golden-seed Historical one on fresh inputs, and every
/// state is a fixture (TEST-ONLY SYNTHETIC values), set before the root draws the game; the info panel then shows Haifa's
/// own hex. The gdd-0.3 games are the real content loaded under gdd-0.3 (WorkshopPanelTests.Load03), so the loader keeps
/// that ruleset's project kinds (housing_repair, content/projects.yaml). Each still asserts the panel lines it is meant to
/// show (after writing it, so the run fails instead of passing silently on a wrong still).
///
/// Shots (`{lang}` is `en` or `he`, `{w}x{h}` the window), each once at the top of the info panel and once scrolled to its
/// last line (`-end`):
/// - `projects-{lang}-{w}x{h}`: gdd-0.3, Haifa with no homes for its 10 people and a housing condition of 22% (a surcharge,
///   the overflow and the cohesion loss), a housing project being built on its own hex with a builder (2 turns left), a
///   housing repair being built with none, and an industry project built on another hex.
/// - `housed-{lang}-{w}x{h}`: gdd-0.3, Haifa well housed (100000 homes at a condition of 60%, above the 50% surcharge
///   threshold), with no project: "Housing surcharge now: none", no overflow or cohesion line.
/// - `gdd01-{lang}-{w}x{h}`: the shipped gdd-0.1 rules with the first state's housing project, which #1722 leaves unchanged.
///
/// Run (Unity closed on the project; the same file, unchanged, on both trees):
///   AFTER, on the PR head:  SHOTS_TAG=after  &lt;Unity&gt; -batchmode -projectPath unity -runTests -testPlatform EditMode -testFilter CityPanelShots
///   BEFORE, with this file and its .meta copied onto main: SHOTS_TAG=before (the same command).
/// Uses only APIs that exist on main as well. On main the list and readout do not exist, so the gdd-0.3 tests fail there by
/// design after writing their stills; the gdd-0.1 tests pass on both trees. Explicit, so CI and the gates never run it.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class CityPanelShots
{
    const string City = "haifa";
    string saveDir = "", shotDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp]
    public void Prepare()
    {
        var tag = Environment.GetEnvironmentVariable("SHOTS_TAG");
        shotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "shots-1722",
            string.IsNullOrEmpty(tag) ? "untagged" : tag));
        Directory.CreateDirectory(shotDir);
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-city-panel-shots-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        rt = null; ps = null;
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
    static UiText Text(bool he) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he ? UiPaths.StringsHebrew : UiPaths.Strings).text);

    /// <summary>Boots the scene at <paramref name="w"/> by <paramref name="h"/> on a golden-seed game of fresh inputs (under
    /// gdd-0.3 when <paramref name="gdd03"/>), shaped by <paramref name="shape"/> before the root draws it, and shows Haifa's
    /// own hex in the info panel.</summary>
    IEnumerator Boot(bool hebrew, bool gdd03, int w, int h, Action<Content, GameState> shape)
    {
        var inputs = Statecraft.Game.Boot.GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        // The ruleset at load time: set after the load, the loader would already have dropped gdd-0.3's kinds (housing_repair).
        if (gdd03) inputs.Content = WorkshopPanelTests.Load03();
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        ShotCamera.Configure(Camera.main, w, h);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (ps != null) Object.DestroyImmediate(ps);
        rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = Color.clear;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(inputs.Map);
        root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        if (hebrew) root.Language = UiLanguage.Hebrew;
        var g = GameSession.New(inputs.Content, GoldenRun.Seed, Mode.Historical);
        g.State.PendingEvents.Clear();   // no event card over the panel
        shape(inputs.Content, g.State);
        root.Begin(inputs, 0, keep: new SimBridge(g, Text(hebrew)));
        ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(5);
        var hide = ui.Q<Button>("event-hide");
        if (hide != null && hide.resolvedStyle.display != DisplayStyle.None && hide.enabledInHierarchy) UiTestKit.Press(hide);
        root.Card?.Collapse(takeFocus: false);
        if (root.Advisor != null && root.Advisor.IsOpen) UiTestKit.Press(ui.Q<Button>("advisor-close"));
        Assert.AreEqual(gdd03 ? SaveGame.Gdd03Ruleset : root.Bridge!.Content.Ruleset, root.Bridge!.Content.Ruleset);
        var home = inputs.Content.SettlementHexes.Single(x => x.Id == City);
        ShotCamera.Configure(Camera.main, w, h);
        root.Info!.Show(home.Q, home.R);
        yield return Frames(8);
    }

    /// <summary>The first state: no homes at 22%, a housing project on Haifa's own hex with a builder, a repair with none and a
    /// built industry project on another hex Haifa reaches (gdd-0.1 has no repair kind: only the projects its content has).</summary>
    static void Projects3(Content c, GameState s)
    {
        var x = s.Cities[City];
        s.Cities[City] = new City(x.Id, 0, x.JobSlots, 10, x.Employed, x.Refugees, x.Recruits, x.Condition is double ? 0.22 : x.Condition, x.Craftsmen, x.JobsDefault, x.Qualified);
        var home = c.SettlementHexes.Single(h => h.Id == City);
        var id = s.NextId++;
        s.CityProjects.Add(id, new CityProject(id, "housing", City, home.Q, home.R, 2).WithBuilder(c.Ruleset == SaveGame.Gdd03Ruleset));
        if (c.ProjectKinds.Any(k => k.Id == Projects.HousingRepair))
        {
            id = s.NextId++;
            s.CityProjects.Add(id, new CityProject(id, Projects.HousingRepair, City, home.Q, home.R, 1));
        }
        var other = c.Hexes.Where(h => h.Land && (h.Q, h.R) != (home.Q, home.R) && Projects.CitiesFor(c, s, h.Q, h.R).Contains(City)).OrderBy(h => h.Q).ThenBy(h => h.R).First();
        id = s.NextId++;
        s.CityProjects.Add(id, new CityProject(id, "industry", City, other.Q, other.R, 0).Built(s.Date));
    }

    /// <summary>The well-housed state (as WorkshopPanelTests.AWellHousedCityHasNoSurchargeAndGdd01HasNoReadout_RHouse005):
    /// Haifa in 100000 homes at a housing condition of 0.60, above the surcharge threshold; its other counts as they are.</summary>
    static void Well(Content c, GameState s)
    {
        var x = s.Cities[City];
        s.Cities[City] = new City(x.Id, 100000, x.JobSlots, x.Civilians, x.Employed, x.Refugees, x.Recruits, 0.60, x.Craftsmen, x.JobsDefault, x.Qualified);
    }

    /// <summary>The projects still's lines: the list header, a row per project of Haifa's, Cancel on the two being built and
    /// none on the built one, and the three housing terms.</summary>
    void ProjectLines()
    {
        var s = root.Bridge!.State;
        var mine = s.CityProjects.Values.Where(p => p.City == City).OrderBy(p => p.Id).ToList();
        Assert.AreEqual(3, mine.Count, "precondition: the housing project, the repair and the built industry project");
        Assert.IsTrue(mine.Any(p => p.Kind == Projects.HousingRepair), "precondition: the repair, a gdd-0.3 kind at load time");
        Assert.AreEqual(2, mine.Count(p => p.BuiltOn == null), "precondition: two projects being built");
        Assert.IsNotNull(ui.Q("city-list-header"), "the list header");
        foreach (var p in mine)
        {
            var name = "city-list-project-" + p.Id.ToString(CultureInfo.InvariantCulture);
            Assert.IsNotNull(ui.Q(name), $"the row of project {p.Id} ({p.Kind})");
            if (p.BuiltOn is DateTime) Assert.IsNull(ui.Q(name + "-cancel"), $"no Cancel on the built project {p.Id} ({p.Kind})");
            else Assert.IsNotNull(ui.Q<Button>(name + "-cancel"), $"Cancel on project {p.Id} ({p.Kind}), being built");
        }
        Assert.IsNotNull(ui.Q("city-housing"), "the housing line");
        Assert.IsNotNull(ui.Q("city-surcharge"), "the surcharge line");
        Assert.IsNotNull(ui.Q("city-housing-falls"), "the overflow line");
        Assert.IsNotNull(ui.Q("city-housing-cohesion"), "the cohesion line");
    }

    /// <summary>The housed still's lines: "no surcharge", no overflow or cohesion line, and no list (no project).</summary>
    void HousedLines()
    {
        var s = root.Bridge!.State;
        Assert.IsFalse(s.CityProjects.Values.Any(p => p.City == City), "precondition: Haifa has no project");
        Assert.IsNotNull(ui.Q("city-housing"), "the housing line");
        Assert.IsNotNull(ui.Q("city-surcharge"), "the no-surcharge line");
        Assert.IsNull(ui.Q("city-housing-falls"), "no overflow line");
        Assert.IsNull(ui.Q("city-housing-cohesion"), "no cohesion line");
        Assert.IsNull(ui.Q("city-list-header"), "no list");
    }

    /// <summary>The gdd-0.1 still's lines: the city's block, with no list and no housing readout, as on main.</summary>
    void Gdd01Lines()
    {
        Assert.IsNotNull(ui.Q("city-housing"), "precondition: the city's block shows");
        Assert.IsNull(ui.Q("city-list-header"), "gdd-0.1 has no list");
        Assert.IsNull(ui.Q("city-surcharge"), "gdd-0.1 has no surcharge");
    }

    static Color[] Read(RenderTexture from)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = from;
        var tex = new Texture2D(from.width, from.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, from.width, from.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        var px = tex.GetPixels();
        Object.DestroyImmediate(tex);
        return px;
    }

    /// <summary>PanelPreviewShots.Shot at this boot's size: the map camera's frame with the UI texture over it (the UI's
    /// pixels are premultiplied by their alpha).</summary>
    void Shot(string name)
    {
        int w = rt!.width, h = rt.height;
        var cam = Camera.main;
        ShotCamera.Require(cam, w, h);
        var frame = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        var before = cam.targetTexture;
        cam.targetTexture = frame; cam.Render(); cam.targetTexture = before;
        var map = Read(frame); var over = Read(rt);
        frame.Release(); Object.DestroyImmediate(frame);
        var outPx = new Color[map.Length];
        for (int i = 0; i < map.Length; i++)
        {
            var u = over[i]; var m = map[i];
            outPx[i] = new Color(u.r + m.r * (1f - u.a), u.g + m.g * (1f - u.a), u.b + m.b * (1f - u.a), 1f);
        }
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.SetPixels(outPx); tex.Apply();
        File.WriteAllBytes(Path.Combine(shotDir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        var labels = ui.Q("info-panel")?.Query<Label>().ToList().Where(l => !string.IsNullOrEmpty(l.name) && (l.name.StartsWith("city-", StringComparison.Ordinal))).Select(l => l.name) ?? Enumerable.Empty<string>();
        Debug.Log($"SHOT {name} -> {shotDir}; city labels: {string.Join(", ", labels)}");
    }

    /// <summary>A shot at the top of the info panel, and one scrolled to its last line.</summary>
    IEnumerator Both(string name)
    {
        var scroll = ui.Q<ScrollView>("info-panel");
        if (scroll != null) scroll.scrollOffset = Vector2.zero;
        yield return Frames(3);
        Shot(name);
        var last = scroll?.contentContainer.Query<VisualElement>().ToList().LastOrDefault(e => e.resolvedStyle.display != DisplayStyle.None && e.worldBound.height > 0);
        if (last != null) scroll!.ScrollTo(last);
        yield return Frames(3);
        Shot(name + "-end");
    }

    IEnumerator Run(string state, bool gdd03, Action<Content, GameState> shape, Action check, bool hebrew)
    {
        var lang = hebrew ? "he" : "en";
        foreach (var (w, h) in new[] { (1024, 768), (1366, 768) })
        {
            yield return Boot(hebrew, gdd03, w, h, shape);
            yield return Both($"{state}-{lang}-{w}x{h}");
            check();   // after the stills, so a "before" run on main still writes them (its gdd-0.3 checks then fail by design)
        }
    }

    [UnityTest] public IEnumerator ProjectsEnglish() { yield return Run("projects", true, Projects3, ProjectLines, false); }
    [UnityTest] public IEnumerator ProjectsHebrew() { yield return Run("projects", true, Projects3, ProjectLines, true); }
    [UnityTest] public IEnumerator HousedEnglish() { yield return Run("housed", true, Well, HousedLines, false); }
    [UnityTest] public IEnumerator HousedHebrew() { yield return Run("housed", true, Well, HousedLines, true); }
    [UnityTest] public IEnumerator Gdd01English() { yield return Run("gdd01", false, Projects3, Gdd01Lines, false); }
    [UnityTest] public IEnumerator Gdd01Hebrew() { yield return Run("gdd01", false, Projects3, Gdd01Lines, true); }
}
