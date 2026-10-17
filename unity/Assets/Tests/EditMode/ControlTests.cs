#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Two-colour borders (owner rulings 2026-10-14, refs #196): who held which areas, 1948-49
/// (content/map/control_hexes_1948_49.yaml, baked by tools/control_bake.py from the true polygons of
/// tools/control_digitise.py): the Israeli side blue, the other side orange-red, contested, no-man's land and the
/// demilitarized zones grey, from 15 May 1948, the game's start (owner decision of 16 October 2026, #674: the 9 July 1948
/// lines, adjusted, until 11 June); nothing before it; the label carries the approved legend in English and Hebrew; the sim
/// never reads the file.</summary>
public class ControlTests
{
    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    static string ControlText => File.ReadAllText(Path.Combine(Paths.Content, "map", Control1948.FileName));
    static string SubText => File.ReadAllText(Path.Combine(Paths.Content, "map", Subdistricts1948.FileName));
    static string ArmText => File.ReadAllText(Path.Combine(Paths.Content, "map", Armistice1949.FileName));
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Folder + "/strings.he.yaml").text);
    static MapRegions Regions() => new MapRegions(Subdistricts1948.Parse(SubText), Armistice1949.Parse(ArmText), Control1948.Parse(ControlText));
    static readonly DateTime Start = new DateTime(1948, 5, 15), Truce1 = new DateTime(1948, 6, 20), TenDays = new DateTime(1948, 7, 12),
        Truce2 = new DateTime(1948, 8, 1), October = new DateTime(1948, 11, 10), Mixed = new DateTime(1949, 4, 20), After = new DateTime(1949, 9, 1);
    string saveDir = "";

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-ctl-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear();
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static (int q, int r) HexOf(double lon, double lat, bool land = true)
    {
        var hexes = JsonUtility.FromJson<HexFile>(Inputs.Map.HexesJson).hexes;
        return hexes.Where(h => h.land == land).OrderBy(h => (h.lon - lon) * (h.lon - lon) + (h.lat - lat) * (h.lat - lat)).Select(h => (h.q, h.r)).First();
    }

    [Test]
    public void TheBakedFileParsesWithContiguousPeriodsFrom15May1948()
    {
        var c = Control1948.Parse(ControlText);
        Assert.AreEqual(Start, c.ShownFrom);
        Assert.IsNull(c.PeriodOn(Start.AddDays(-1)), "nothing before the game's start, 15 May 1948");
        Assert.AreEqual("opening", c.PeriodOn(Start)!.Id);
        Assert.AreEqual(Control1948.Legend.Adjusted, c.PeriodOn(Start)!.Legend);
        Assert.AreEqual("1948-07-09", c.PeriodOn(Start)!.LineDate, "the 9 July 1948 lines");
        Assert.AreEqual("opening", c.PeriodOn(new DateTime(1948, 6, 10))!.Id, "to 10 June inclusive");
        Assert.AreEqual("truce1", c.PeriodOn(new DateTime(1948, 6, 11))!.Id, "the first truce from 11 June");
        Assert.AreEqual("truce1", c.PeriodOn(Truce1)!.Id);
        Assert.AreEqual("ten_days", c.PeriodOn(TenDays)!.Id);
        Assert.AreEqual(Control1948.Legend.Held, c.PeriodOn(October)!.Legend);
        Assert.AreEqual(Control1948.Legend.Mixed, c.PeriodOn(Mixed)!.Legend);
        Assert.AreEqual(Control1948.Legend.Armistice, c.PeriodOn(After)!.Legend);
        Assert.IsNull(c.Periods.Last().Until);
        StringAssert.Contains("nearest hex edge", c.Line);
        StringAssert.Contains("Military control", c.Meaning);
    }

    [TestCase("", "empty")]
    [TestCase("layer: x\nperiods: []\n", "no periods")]
    [TestCase("layer: x\nshown_from: 1948-06-11\nperiods:\n  - { id: a, from: 1948-06-11, until: null, legend: odd, line_date: '1948-07-09', fronts: [] }\nhexes: []\n", "unknown legend")]
    [TestCase("layer: x\nshown_from: 1948-06-11\nperiods:\n  - { id: a, from: 1948-06-11, until: null, legend: held, line_date: '1948-07-09', fronts: [] }\nhexes:\n  - [1, 2, 'IX']\n", "codes for 1 periods")]
    [TestCase("layer: x\nshown_from: 1948-06-11\nperiods:\n  - { id: a, from: 1948-06-11, until: null, legend: held, line_date: '1948-07-09', fronts: [] }\nhexes:\n  - [1, 2, 'X']\n", "unknown code")]
    [TestCase("layer: x\nshown_from: 1948-06-11\nperiods:\n  - { id: a, from: 1948-06-11, until: 1948-07-01, legend: held, line_date: '1948-07-09', fronts: [] }\n  - { id: b, from: 1948-07-02, until: null, legend: held, line_date: '1948-07-09', fronts: [] }\nhexes: []\n", "does not start where")]
    public void MalformedFilesAreRefusedNamingTheFile(string yaml, string why)
    {
        var e = Assert.Throws<InvalidDataException>(() => Control1948.Parse(yaml));
        StringAssert.Contains(Control1948.FileName, e!.Message);
        StringAssert.Contains(why, e.Message);
    }

    [Test]
    public void TheSidesFollowTheSourcesOnTheirDates()
    {
        var m = Regions();
        var telAviv = HexOf(34.78, 32.08); var nablus = HexOf(35.2608, 32.2211); var nazareth = HexOf(35.3033, 32.7019);
        foreach (var d in new[] { Truce1, Truce2, October, Mixed, After })
        {
            var layer = m.LayerOn(d)!;
            Assert.AreEqual(BorderStyle.Primary, layer.StyleOf(layer.RegionOf(telAviv.q, telAviv.r)), $"Tel Aviv on {d:d}");
            Assert.AreEqual(BorderStyle.Secondary, layer.StyleOf(layer.RegionOf(nablus.q, nablus.r)), $"Nablus on {d:d}");
        }
        Assert.AreEqual(Control1948.Side.Arab, m.Control!.At(nazareth.q, nazareth.r, Truce1).side);
        Assert.AreEqual(Control1948.Side.Contested, m.Control.At(nazareth.q, nazareth.r, TenDays).side, "grey for the days an operation ran");
        Assert.AreEqual(Control1948.Side.Israel, m.Control.At(nazareth.q, nazareth.r, Truce2).side);
    }

    // Settlement hexes (content/map/settlement_hexes.yaml, tools/settlement_hexes.py)
    static readonly (int q, int r) AcreHex = (7, 6), JerusalemHex = (0, 23), GazaHex = (-11, 27), YadMordechaiHex = (-9, 26);

    /// <summary>#674 (b): from the game's start the 9 July lines, adjusted: the coast blue, Samaria orange-red, the Acre coast
    /// grey (contested), Jerusalem no-man's land, the south without colour but its settlements blue; nothing before 15 May.</summary>
    [Test]
    public void TheGameOpensOnThe9JulyLinesAdjustedAndNothingIsColouredBefore15May()
    {
        var m = Regions();
        Assert.IsNull(m.LayerOn(Start.AddDays(-1)), "nothing is drawn before 15 May 1948");
        Assert.IsNull(m.ControlLabel(Start.AddDays(-1), k => En[k]));
        var layer = m.LayerOn(Start)!;
        Assert.AreEqual("control_1948_49:opening", layer.Name);
        BorderStyle Style((int q, int r) h) => layer.StyleOf(layer.RegionOf(h.q, h.r));
        Assert.AreEqual(BorderStyle.Primary, Style(HexOf(34.78, 32.08)), "Tel Aviv");
        Assert.AreEqual(BorderStyle.Secondary, Style(HexOf(35.2608, 32.2211)), "Nablus");
        Assert.AreEqual(BorderStyle.Neutral, Style(AcreHex), "the Acre coast: Operation Ben-Ami, Acre taken 17/18 May");
        Assert.AreEqual(BorderStyle.Neutral, Style(JerusalemHex));
        Assert.AreEqual(BorderStyle.Primary, Style(YadMordechaiHex), "a settlement in the uncoloured south (owner ruling Q3)");
        Assert.AreEqual(0, layer.RegionOf(GazaHex.q, GazaHex.r), "the south has no colour: no region, no band");
        var c = m.Control!;
        Assert.AreEqual((Control1948.Side.Contested, (string?)null), c.At(AcreHex.q, AcreHex.r, Start));
        Assert.AreEqual((Control1948.Side.NoMansLand, (string?)null), c.At(JerusalemHex.q, JerusalemHex.r, Start));
        Assert.AreEqual((Control1948.Side.None, (string?)null), c.At(GazaHex.q, GazaHex.r, Start));
        Assert.AreEqual((Control1948.Side.Arab, (string?)null), c.At(HexOf(35.3033, 32.7019).q, HexOf(35.3033, 32.7019).r, Start),
            "Nazareth: the other side, its holder not named before 11 June");
        Assert.AreEqual(Control1948.Side.Israel, c.At(YadMordechaiHex.q, YadMordechaiHex.r, Start).side);
        Assert.AreNotEqual(Control1948.Side.Israel, c.At(YadMordechaiHex.q, YadMordechaiHex.r, Truce1).side, "fallen on 24 May");
    }

    /// <summary>Kept for a side file whose first period starts later: before it the sub-districts are grey, never blue.</summary>
    [Test]
    public void BeforeAFirstPeriodTheSubdistrictsAreGreyNotBlue()
    {
        var later = Control1948.Parse("layer: x\nshown_from: 1948-06-11\nperiods:\n  - { id: a, from: 1948-06-11, until: null, legend: held, line_date: '1948-07-09', fronts: [] }\nhexes: []\n");
        var m = new MapRegions(Subdistricts1948.Parse(SubText), Armistice1949.Parse(ArmText), later);
        var layer = m.LayerOn(Start)!;
        StringAssert.StartsWith("subdistricts_1948", layer.Name);
        var styles = layer.Hexes.Select(h => layer.StyleOf(layer.RegionOf(h.q, h.r))).Distinct().ToList();
        CollectionAssert.AreEquivalent(new[] { BorderStyle.Neutral, BorderStyle.None }, styles, "blue now means the Israeli side");
        Assert.IsNull(m.ControlLabel(Start, k => En[k]), "the sub-district label stands");
    }

    [Test]
    public void TheFrontierIsOutlinedAndNothingIsDrawnBeyondIt()
    {
        var layer = Regions().LayerOn(Truce2)!;
        var beyond = HexOf(35.9, 32.5);   // Transjordan
        Assert.AreEqual(BorderStyle.None, layer.StyleOf(layer.RegionOf(beyond.q, beyond.r)));
        var sea = HexOf(34.3, 32.5, land: false);
        Assert.AreEqual(0, layer.RegionOf(sea.q, sea.r), "the sea: no band along the coast");
    }

    [Test]
    public void TheLabelNamesTheSidesBeforeTheirColoursWithADashedGreyInEnglishAndHebrew()
    {
        var m = Regions();
        string en = m.ControlLabel(Truce2, k => En[k])!, he = m.ControlLabel(Truce2, k => He[k])!;
        StringAssert.StartsWith("Held by Israel: blue · Held by Arab forces: orange-red · Contested or no-man's land: grey, dashed", en);
        StringAssert.DoesNotContain("not held", en, "nothing is ever drawn grey for land held by no one");
        StringAssert.Contains("18 July 1948", en);
        StringAssert.Contains("Israel State Archives", en);
        StringAssert.StartsWith("בשליטת ישראל: כחול · בשליטת כוחות ערביים: כתום-אדום · שטח שבמחלוקת או שטח הפקר: אפור, מקווקו", he);
        StringAssert.Contains("18 ביולי 1948", he, "the date in Hebrew month names (review S5)");
        StringAssert.DoesNotContain("July", he);
        StringAssert.Contains("ארכיון המדינה", he);
        StringAssert.Contains("9 July 1948", m.ControlLabel(Truce1, k => En[k]), "the 11 June snapshot is the line dated 9 July, labelled as such");
        // #674 (b): 15 May - 10 June 1948, "lines of 9 July 1948, adjusted"
        var open = m.ControlLabel(Start, k => En[k])!;
        StringAssert.StartsWith("Held by Israel: blue · Held by Arab forces: orange-red · Contested or no-man's land: grey, dashed", open);
        StringAssert.Contains("Front lines of 9 July 1948, adjusted", open);
        StringAssert.Contains("CIA map of 18 May 1948", open);
        StringAssert.DoesNotContain("approximate", open);
        var openHe = m.ControlLabel(Start, k => He[k])!;
        StringAssert.Contains("קווי החזית של 9 ביולי 1948, מותאמים", openHe);
        StringAssert.DoesNotContain("July", openHe);
        Assert.AreEqual(open, m.ControlLabel(new DateTime(1948, 6, 10), k => En[k]), "the same caption to 10 June");
        StringAssert.Contains("Front lines, 9 July 1948, approximate", m.ControlLabel(new DateTime(1948, 6, 11), k => En[k]));
        var after = m.ControlLabel(After, k => En[k])!;
        StringAssert.StartsWith("Israeli side of the armistice line: blue · Arab side: orange-red · No-man's land or demilitarized zone: grey, dashed", after);
        StringAssert.Contains("Jordan (low accuracy)", after, "D-A ruling 2: the Jordan line is labelled low-accuracy on the map");
        StringAssert.StartsWith("הצד הישראלי של קו שביתת הנשק: כחול · הצד הערבי: כתום-אדום · שטח הפקר או אזור מפורז: אפור, מקווקו", m.ControlLabel(After, k => He[k]));
        var mixed = m.ControlLabel(Mixed, k => En[k])!;
        StringAssert.StartsWith("Israeli side of the armistice line", mixed);
        StringAssert.Contains("Egypt, Jordan (low accuracy), Lebanon", mixed);
        StringAssert.Contains("October 1948", mixed);
        StringAssert.Contains("אוקטובר 1948", m.ControlLabel(Mixed, k => He[k]));
    }

    [Test]
    public void TheInfoPanelSaysWhoHeldTheHexAndClaimsNothingElse()
    {
        var m = Regions();
        var telAviv = HexOf(34.78, 32.08); var gaza = HexOf(34.4597, 31.5075); var nablus = HexOf(35.2608, 32.2211); var nazareth = HexOf(35.3033, 32.7019);
        var march = new DateTime(1949, 3, 1);   // Egypt's agreement in force, Jordan's not yet
        StringAssert.StartsWith("Held by Israel", InfoPanelPresenter.ControlText(m, En, telAviv.q, telAviv.r, Truce2));
        Assert.AreEqual("Held by Arab forces (Egypt)", InfoPanelPresenter.ControlText(m, En, gaza.q, gaza.r, Truce2));
        Assert.AreEqual("Contested", InfoPanelPresenter.ControlText(m, En, nazareth.q, nazareth.r, TenDays));
        Assert.AreEqual("Arab side of the armistice line (Egypt)", InfoPanelPresenter.ControlText(m, En, gaza.q, gaza.r, march), "Egypt has switched");
        StringAssert.StartsWith("Held by Arab forces", InfoPanelPresenter.ControlText(m, En, nablus.q, nablus.r, march), "Jordan has not");
        Assert.AreEqual("Israeli side of the armistice line", InfoPanelPresenter.ControlText(m, En, telAviv.q, telAviv.r, After));
        Assert.AreEqual("Arab side of the armistice line (Jordan)", InfoPanelPresenter.ControlText(m, En, nablus.q, nablus.r, After), "the state, not the army, after the armistice");
        Assert.AreEqual("", InfoPanelPresenter.ControlText(m, En, telAviv.q, telAviv.r, Start.AddDays(-1)), "nothing before 15 May 1948");
        Assert.AreEqual("Held by Israel", InfoPanelPresenter.ControlText(m, En, telAviv.q, telAviv.r, Start));
        Assert.AreEqual("Held by Arab forces", InfoPanelPresenter.ControlText(m, En, nablus.q, nablus.r, Start), "no holder named before 11 June");
        Assert.AreEqual("Contested", InfoPanelPresenter.ControlText(m, En, AcreHex.q, AcreHex.r, Start));
        Assert.AreEqual("No-man's land", InfoPanelPresenter.ControlText(m, En, JerusalemHex.q, JerusalemHex.r, Start));
        Assert.AreEqual("No sourced front line here on this date", InfoPanelPresenter.ControlText(m, En, GazaHex.q, GazaHex.r, Start));
        var amman = HexOf(35.9, 31.95);
        Assert.AreEqual("", InfoPanelPresenter.ControlText(m, En, amman.q, amman.r, After), "beyond the frontier the data says nothing");
        StringAssert.StartsWith("Held by Israel", InfoPanelPresenter.RegionText(m, En, telAviv.q, telAviv.r, Truce2));
        StringAssert.DoesNotContain("Mandate sub-districts", InfoPanelPresenter.RegionText(m, En, telAviv.q, telAviv.r, Truce2), "no sub-district once its bands are gone");
        StringAssert.DoesNotContain("Mandate sub-districts", InfoPanelPresenter.RegionText(m, En, telAviv.q, telAviv.r, Start), "sides from the start (#674)");
        // a demilitarized zone hex: the zone's own line, not a second "Demilitarized zone"
        var dmz = m.Control!.Periods.Count > 0 ? FindHex(m, After, Control1948.Side.Dmz) : default;
        Assert.AreEqual("Demilitarized zone", InfoPanelPresenter.ControlText(m, En, dmz.q, dmz.r, After));
        StringAssert.DoesNotContain("Demilitarized zone", InfoPanelPresenter.RegionText(m, En, dmz.q, dmz.r, After));
        // no-man's land from a minimal file too (the data has it only in the opening period, in the Old City's hex)
        var nml = Control1948.Parse("layer: x\nshown_from: 1948-06-11\nperiods:\n  - { id: a, from: 1948-06-11, until: null, legend: held, line_date: '1948-07-09', fronts: [] }\nhexes:\n  - [1, 2, 'm']\n");
        Assert.AreEqual((Control1948.Side.NoMansLand, (string?)null), nml.At(1, 2, Truce1));
        Assert.AreEqual("No-man's land", InfoPanelPresenter.ControlText(new MapRegions(Subdistricts1948.Parse(SubText), null, nml), En, 1, 2, Truce1));
    }

    static (int q, int r) FindHex(MapRegions m, DateTime d, Control1948.Side side)
    {
        var hexes = JsonUtility.FromJson<HexFile>(Inputs.Map.HexesJson).hexes;
        return hexes.Select(h => (h.q, h.r)).First(h => m.Control!.At(h.q, h.r, d).side == side);
    }

    /// <summary>The game's own wiring (review S10): boot, then move the session's date through the bridge past 11 June
    /// 1948. The map draws the side layer and the label carries the legend; at the start, the 9 July lines, adjusted (#674).</summary>
    [Test]
    public void ANormalGameOpensOnSidesAndSwitchesToTheFirstTruceOn11June1948()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(Inputs, 0);
        var label = Object.FindFirstObjectByType<UIDocument>().rootVisualElement.Q<Label>("map-layer");
        Assert.AreEqual("control_1948_49:opening", MapBorders.Current!.Name, "sides from the game's start");
        StringAssert.StartsWith("Held by Israel: blue", label.text);
        StringAssert.Contains("Front lines of 9 July 1948, adjusted", label.text);
        var g0 = GameSession.New(Inputs.Content, GoldenRun.Seed, Mode.Historical);
        g0.State.Date = new DateTime(1948, 6, 11);
        root.Bridge!.Replace(g0);
        Assert.AreEqual("control_1948_49:truce1", MapBorders.Current!.Name);
        StringAssert.Contains("Front lines, 9 July 1948, approximate", label.text);
        var g = GameSession.New(Inputs.Content, GoldenRun.Seed, Mode.Historical);
        g.State.Date = Truce2;
        root.Bridge!.Replace(g);
        StringAssert.StartsWith("control_1948_49:truce2", MapBorders.Current!.Name);
        StringAssert.StartsWith("Held by Israel: blue", label.text);
        StringAssert.Contains("18 July 1948", label.text);
    }

    [Test]
    public void TheWebGLBuildShipsTheFileAndReadsIt()
    {
        CollectionAssert.Contains(ContentManifest.Collect(Paths.Content), "map/" + Control1948.FileName);
        Assert.AreEqual(ControlText, Inputs.Map.ControlYaml);
    }

    /// <summary>Map-only data: the content hash (saves, goldens) does not move whatever the control files say.</summary>
    [Test]
    public void TheSimIgnoresTheFilesTheContentHashDoesNotMove()
    {
        var copy = Path.Combine(saveDir, "content");
        foreach (var f in Directory.GetFiles(Paths.Content, "*", SearchOption.AllDirectories).Where(f => !f.EndsWith(".meta")))
        {
            var to = Path.Combine(copy, Path.GetRelativePath(Paths.Content, f));
            Directory.CreateDirectory(Path.GetDirectoryName(to)!); File.Copy(f, to);
        }
        var before = ContentLoader.Load(copy).Hash;
        Assert.AreEqual(ContentLoader.Load(Paths.Content).Hash, before);
        File.WriteAllText(Path.Combine(copy, "map", Control1948.FileName), "layer: changed\n");
        File.WriteAllText(Path.Combine(copy, "map", "control_1948_49.geojson"), "{}");
        Assert.AreEqual(before, ContentLoader.Load(copy).Hash);
    }

    /// <summary>The longest legend label (English and Hebrew, a period with some fronts switched: legend and caption) fits
    /// beside the minimap with the real HUD and the longest event card (which hides it while open: owner, 17 October 2026,
    /// #849), as ArmisticeTests.TheLongestMapLabelFitsBesideTheEventCard
    /// checks the older label: on screen, clear of the card, the minimap, the info panel and the top bar, its text not cut, at
    /// most six lines (the approved legend is about three of them in the narrow box).</summary>
    [UnityTest]
    public IEnumerator TheLongestLegendLabelFitsBesideTheEventCard()
    {
        var c = Inputs.Content; var m = Regions();
        var periods = m.Control!.Periods.Select(p => p.From).ToList();
        foreach (var lang in new[] { En, He })
        {
            var text = periods.Select(d => m.ControlLabel(d, k => lang[k])!).OrderByDescending(x => x.Length).First();
            foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (1531, 784), (1366, 657), (2560, 1080), (1920, 900), (1280, 1024),
                                             (960, 600), (1024, 768), (390, 844), (1600, 757) })
            {
                var rt = new RenderTexture(w, h, 0);
                var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
                ps.targetTexture = rt;
                var go = new GameObject("LabelLayout"); go.SetActive(false);
                var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps;
                doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
                go.SetActive(true);
                var root = doc.rootVisualElement;
                var bridge = new SimBridge(GameSession.New(c, GoldenRun.Seed, Mode.Historical), lang);
                var hud = new HudPresenter(root, bridge, lang);
                var card = new EventCardPresenter(root, bridge, lang, null, null);
                try
                {
                    var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
                    var e = c.Events.OrderByDescending(x => x.Text.Length).First();
                    g.State.Date = e.From; g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(e.Id);
                    bridge.Replace(g);
                    var label = root.Q<Label>("map-layer");
                    label.text = text; label.style.display = DisplayStyle.Flex;
                    for (int i = 0; i < 6; i++) yield return null;
                    Rect R(string n) => root.Q(n).worldBound;
                    var lb = label.worldBound; var screen = root.worldBound;
                    string at = $"{lang["ui.lang"]} {w}x{h}";
                    Assert.IsTrue(lb.xMin >= screen.xMin - 0.5f && lb.xMax <= screen.xMax + 0.5f && lb.yMin >= screen.yMin - 0.5f && lb.yMax <= screen.yMax + 0.5f,
                        $"{at}: the label {lb} leaves the screen {screen}");
                    Assert.IsFalse(lb.Overlaps(R("minimap")), $"{at}: the label {lb} overlaps the minimap {R("minimap")}");
                    // Not clear of the card: the owner's decision of 17 October 2026 (#849, issuecomment-6233870812, "Hide
                    // legend under card", on desktop and phone alike) hides the legend while the modal card is open
                    // (DecisionCardTests.TheLegendIsHiddenWhileTheCardIsOpen), so the card never covers it.
                    if (root.Q("info-panel").resolvedStyle.display == DisplayStyle.Flex)
                        Assert.IsFalse(lb.Overlaps(R("info-panel")), $"{at}: the info panel overlaps the label {lb}");
                    Assert.IsFalse(lb.Overlaps(R("top-bar")), $"{at}: the top bar overlaps the label");
                    var size = label.MeasureTextSize(text, lb.width - label.resolvedStyle.paddingLeft - label.resolvedStyle.paddingRight,
                        VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                    Assert.LessOrEqual(size.y, lb.height + 1f, $"{at}: the label's text is cut off");
                    // the approved legend alone is about three lines in the 312 px box; with its caption, at most six
                    Assert.LessOrEqual(size.y, 6 * label.resolvedStyle.fontSize * 1.4f, $"{at}: the label takes more than six lines ({size.y} px)");
                }
                finally { card.Dispose(); hud.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
            }
        }
    }

    /// <summary>Rendered: on a coastal-plain proof edge at gameplay03's zoom, the Primary band measures about the owner's
    /// deeper blue (promo 03's Israeli side, about 89,119,156 on screen), against main's lighter 120,142,192.</summary>
    [UnityTest]
    public IEnumerator ThePrimaryStripeIsTheDeeperBlueOfTheRuling()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        RenderSample.TiltShift(false);
        BorderProof.Show(map);
        var (inner, outer) = BorderTests.ProofEdge(map, PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, "coastal_plain");
        BorderTests.LookAtEdge(map, rig, inner);
        var target = new Color(89 / 255f, 119 / 255f, 156 / 255f); var mainBlue = new Color(120 / 255f, 142 / 255f, 192 / 255f);
        var d = new List<float>(); var dMain = new List<float>();
        foreach (var w in BorderTests.WalkEdge(map, cam, inner, outer, 9, 1920, 1080))
        {
            var p = BorderTests.Read(w, -1, BorderTests.Blueness, 0.45f);
            TestContext.WriteLine($"primary stripe {p.Stripe} ({p.Stripe.r * 255:0},{p.Stripe.g * 255:0},{p.Stripe.b * 255:0})");
            d.Add(RenderSample.DeltaE(p.Stripe, target)); dMain.Add(RenderSample.DeltaE(p.Stripe, mainBlue));
        }
        float md = d.OrderBy(x => x).ElementAt(d.Count / 2), mm = dMain.OrderBy(x => x).ElementAt(dMain.Count / 2);
        TestContext.WriteLine($"median deltaE76 to the ruling's blue {md:0.0}, to main's {mm:0.0}");
        Assert.Less(md, 10f, "about the ruling's deeper blue");
        Assert.Less(md, mm, "nearer the deeper blue than main's lighter one");
    }
    /// <summary>Not a check: the contact sheet's raw renders (run on demand: -testFilter ControlTests.ContactSheetRenders, the
    /// output folder in STATECRAFT_CONTACT_DIR). For each period, a 220 km overview and the gameplay zoom (60 km) over the Ramle
    /// front, the Gaza-Faluja south and the Galilee (kits and trees on, as shipped), and the legend label in English and Hebrew as the UI draws it.</summary>
    [UnityTest, Explicit]
    public IEnumerator ContactSheetRenders()
    {
        var dir = Environment.GetEnvironmentVariable("STATECRAFT_CONTACT_DIR") ?? Path.Combine(Path.GetTempPath(), "statecraft-contact");
        Directory.CreateDirectory(dir);
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        MapShaderGlobals.PinWaterTime(100f);
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        yield return null;
        var m = Regions();
        void Save(RenderSample r, string name)
        {
            var tex = new Texture2D(r.Width, r.Height, TextureFormat.RGB24, false);
            tex.SetPixels(r.Pixels); tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
        var dates = new List<(string id, DateTime d)>();
        dates.AddRange(m.Control!.Periods.Select(p => (p.Id, p.From.AddDays(1))));
        foreach (var (id, d) in dates)
        {
            MapBorders.Use(m.LayerOn(d));
            rig.TopInset = 0f;
            foreach (var (pose, lon, lat, dist) in new[] { ("overview", 35.05, 32.0, 220000f), ("front", 34.95, 31.88, 60000f), ("south", 34.65, 31.55, 60000f), ("north", 35.30, 32.85, 60000f), ("beersheba", 34.95, 31.35, 60000f) })
            {
                rig.LookAt = map.Frame.ToWorld(lon, lat, 0); rig.Distance = dist; rig.Yaw = 0f; rig.Apply();
                Save(RenderSample.Render(cam, 1600, 900), $"{pose}-{id}");
            }
        }
        // the legend as the UI draws it
        foreach (var (lang, tag) in new[] { (En, "en"), (He, "he") })
            foreach (var d in new[] { Start, Truce1, Truce2, Mixed, After })
            {
                var rt = new RenderTexture(1920, 1080, 0);
                var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
                ps.targetTexture = rt;
                var go = new GameObject("Legend"); go.SetActive(false);
                var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps;
                doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
                go.SetActive(true);
                var root = doc.rootVisualElement;
                foreach (var child in root.Children().ToList()) child.style.display = DisplayStyle.None;
                var label = new Label(m.ControlLabel(d, k => lang[k])) { };
                label.AddToClassList("map-layer");
                label.style.display = DisplayStyle.Flex; label.style.position = Position.Absolute; label.style.left = 10; label.style.top = 10;
                label.style.right = StyleKeyword.Auto; label.style.bottom = StyleKeyword.Auto; label.style.maxWidth = 312;
                // Hebrew as the start screen sets it (Statecraft.uss .start-he): Frank Ruhl Libre through the advanced text
                // generator, which orders right-to-left text (the Hebrew UI is off in play until reviewed)
                if (tag == "he") { label.style.unityFontDefinition = FontDefinition.FromFont(AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontHebrew)); label.style.unityTextGenerator = TextGeneratorType.Advanced; }
                root.Add(label);
                for (int i = 0; i < 6; i++) yield return null;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, $"legend-{tag}-{d:yyyy-MM-dd}.png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        MapShaderGlobals.PinWaterTime(null);
    }
}
