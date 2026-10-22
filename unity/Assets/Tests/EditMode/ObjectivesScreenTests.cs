#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The Objectives screen (#1189): a side-menu screen like How to play, one row per objective from the sim's
/// Objectives.Status, each with its name, whether it is done and both dates, and no score; both languages, and the two
/// required layouts.</summary>
public class ObjectivesScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    static string Date(UiText t, System.DateTime d) => t.Format("date.day_month_year", d.Day, t["date.month_" + d.Month], d.Year);

    [Test]
    public void TheSideMenuOpensAndClosesTheScreenInBothLanguages([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
        using var screenPresenter = new ObjectivesPresenter(ui.Root, bridge, text);
        var screen = ui.Q<VisualElement>("screen-objectives");

        Assert.IsFalse(ScreenNav.IsOpen(screen), "hidden at start");
        ui.Press("nav-objectives");
        Assert.IsTrue(ScreenNav.IsOpen(screen), "the side-menu button opens it");
        Assert.AreEqual("objectives", nav.Current);
        Assert.AreEqual(text["objectives.title"], ui.Q<Label>("objectives-title").text);
        ui.Press("nav-objectives");
        Assert.IsFalse(ScreenNav.IsOpen(screen), "pressing the same button closes it");
        Assert.IsNull(nav.Current);
    }

    /// <summary>The rows are the helper's, in its order: not done yet, then done with the player's date once the sim records
    /// it (the screen follows the bridge). The two patterns have no score in them (the owner's ruling).</summary>
    [Test]
    public void EveryRowShowsTheHelpersNameStatusAndDates([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
        using var presenter = new ObjectivesPresenter(ui.Root, bridge, text);

        void Check(string when)
        {
            var status = Objectives.Status(bridge.Content, bridge.State);
            var rows = ui.Q<ScrollView>("objectives-rows").Query(className: "objectives-row").ToList();
            Assert.AreEqual(status.Count, rows.Count, when + ": one row per objective");
            Assert.AreEqual(C.Objectives.Count, rows.Count, when + ": every objective in objectives.yaml");
            for (int i = 0; i < status.Count; i++)
            {
                var o = status[i];
                // The name is objectives.yaml's own (#1208): name_he in Hebrew, name in English; no copy in the strings files.
                var objective = C.Objectives.Single(x => x.Id == o.Id);
                Assert.IsNotEmpty(objective.NameHe, o.Id + " has a Hebrew name in objectives.yaml");
                Assert.IsFalse(text.Has("objectives.name." + o.Id), when + ": " + o.Id + " has no second copy of the name in the strings file");
                Assert.AreEqual("objectives-row-" + o.Id, rows[i].name, when + ": the helper's order");
                Assert.AreEqual(hebrew ? objective.NameHe : objective.Name, rows[i].Q<Label>(className: "objectives-name").text, when + ": " + o.Id + " name");
                var line = rows[i].Q<Label>(className: "objectives-status").text;
                Assert.AreEqual(o.Completed is System.DateTime done
                    ? text.Format("objectives.done", Date(text, done), Date(text, o.Historical))
                    : text.Format("objectives.open", Date(text, o.Historical)), line, when + ": " + o.Id + " status and dates");
            }
        }

        Assert.IsTrue(Objectives.Status(bridge.Content, bridge.State).All(o => !o.Done), "precondition: nothing done at the start");
        Check("at the start");
        var first = C.Objectives.Single(x => x.Id == "absorb_immigrants");   // by id: the first in id order, as before R-OBJ-007
        Assert.IsTrue(Objectives.Record(C, bridge.State, C.RulesAt(bridge.State.Date).Mvp!, first.Id, first.ReferenceDate.AddMonths(-1), new List<Cause>()));
        presenter.Render();
        Assert.IsTrue(Objectives.Status(bridge.Content, bridge.State).Single(x => x.Id == first.Id).Done, "precondition: the first is done");
        Check("after the first is done");
    }

    [UnityTest]
    public IEnumerator TheScreenFitsLaptopAndPhoneInBothLanguages()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(390, 844) })
                yield return Rendered(size.x, size.y, hebrew, root =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {size.x}x{size.y}";
                    var screen = root.Q("screen-objectives").worldBound;
                    var window = root.worldBound;
                    if (!(screen.width > 0 && screen.height > 0)) problems.Add($"{at}: screen is not visible");
                    if (screen.xMin < window.xMin || screen.yMin < window.yMin || screen.xMax > window.xMax || screen.yMax > window.yMax)
                        problems.Add($"{at}: screen {screen} leaves window {window}");
                    var body = root.Q<ScrollView>("objectives-rows").worldBound;
                    var labels = root.Q<ScrollView>("objectives-rows").Query<Label>().ToList();
                    if (labels.Count != 2 * C.Objectives.Count) problems.Add($"{at}: {labels.Count} labels, not two per objective");
                    foreach (var line in labels)
                    {
                        if (string.IsNullOrWhiteSpace(line.text)) problems.Add($"{at}: {line.name} is empty");
                        if (line.worldBound.xMin < body.xMin - 1 || line.worldBound.xMax > body.xMax + 1)
                            problems.Add($"{at}: {line.name} {line.worldBound} overflows body {body}");
                    }
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    static IEnumerator Rendered(int width, int height, bool hebrew, System.Action<VisualElement> check)
    {
        var rt = new RenderTexture(width, height, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("ObjectivesLayoutTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        root.Q("root").RemoveFromClassList("loading");
        if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
        var text = hebrew ? He : En;
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
        var hud = new HudPresenter(root, bridge, text);
        var screen = new ObjectivesPresenter(root, bridge, text);
        var nav = new ScreenNav(root);
        nav.Open("objectives");
        try
        {
            for (int i = 0; i < 30 && !(root.Q("screen-objectives").layout.width > 0); i++) yield return null;
            for (int i = 0; i < 3; i++) yield return null;
            check(root);
        }
        finally
        {
            nav.Dispose(); screen.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }
}
