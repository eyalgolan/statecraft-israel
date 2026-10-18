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

/// <summary>The turn report (#1037): after End Turn, each top-bar number that changed, with the change the bar shows and its
/// main causes, which are the sim's own cause messages (StepResult.Why). The fixed turn is the opening one: GoldenRun.Seed,
/// Historical, no orders, so One Army goes to its minister.</summary>
public class TurnReportTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    /// <summary>The HUD, the screens' navigation, the Why screen, the decision card and the report on Game.uxml.</summary>
    sealed class Kit : System.IDisposable
    {
        public readonly UiTestKit Ui = new UiTestKit();
        public readonly SimBridge Bridge; public readonly ScreenNav Nav; public readonly EventCardPresenter Card; public readonly TurnReportPresenter Report;
        readonly List<System.IDisposable> owned = new List<System.IDisposable>();

        public Kit(UiText text, System.Func<GameSession, IReadOnlyList<Command>, StepResult>? step = null)
        {
            Bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text, step);
            owned.Add(new HudPresenter(Ui.Root, Bridge, text));
            owned.Add(Nav = new ScreenNav(Ui.Root));
            owned.Add(new WhyPresenter(Ui.Root, Bridge, text));
            owned.Add(Card = new EventCardPresenter(Ui.Root, Bridge, text, null));
            owned.Add(Report = new TurnReportPresenter(Ui.Root, Bridge, text, Nav));
        }

        public VisualElement Q(string name) => Ui.Q<VisualElement>(name);

        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Ui.Dispose();
        }
    }

    [Test]
    public void TheFixedTurnReportsEachChangedNumberWithTheSimsOwnCauses()
    {
        using var k = new Kit(En);
        Assert.IsFalse(k.Report.IsOpen, "no report before a turn");
        var turn = k.Bridge.EndTurn();
        Assert.IsTrue(k.Report.IsOpen, "End Turn opens the report");
        Assert.AreEqual(DisplayStyle.Flex, k.Q("turn-report").style.display.value);

        var m = HudModel.From(k.Bridge, En);
        var changed = HudModel.MeterIds.Where(id => m.For(id).Delta.Length > 0).ToArray();
        Assert.GreaterOrEqual(changed.Length, 3, "the opening turn moves at least three numbers in the bar");
        var rows = k.Ui.Q<ScrollView>("turn-report-rows").Children().ToList();
        CollectionAssert.AreEqual(changed.Select(id => "turn-report-" + id), rows.Select(r => r.name), "every changed number, in the bar's order, and no other");
        var messages = new HashSet<string>(turn.Why.Select(w => w.Message));
        foreach (var id in changed)
        {
            // The heading is the bar's caption and the very change the bar shows next to the number.
            var heading = k.Ui.Q<Label>("turn-report-heading-" + id).text;
            Assert.AreEqual(En.Format("report.line", En["hud.caption." + id], k.Ui.Q<Label>(id + "-delta").text), heading, id);
            var causes = k.Ui.Root.Q<Label>("turn-report-causes-" + id);
            var expected = HudModel.Causes(turn, id);
            if (expected.Count == 0) { Assert.IsNull(causes, id + ": no cause line without a cause"); continue; }
            var lines = causes!.text.Split('\n');
            CollectionAssert.AreEqual(expected, lines, id);
            Assert.LessOrEqual(lines.Length, HudModel.MaxCauses, id);
            foreach (var line in lines) Assert.IsTrue(messages.Contains(line), $"{id}: '{line}' is a cause of StepResult.Why, word for word");
        }
        // The fixed turn's own causes: Security moved by One Army, then by the border villages' effect, the larger first.
        var security = k.Ui.Q<Label>("turn-report-causes-security").text.Split('\n');
        Assert.AreEqual("Security +1.5% (One Army)", security[0]);
        StringAssert.EndsWith("(Border villages)", security[1]);
        StringAssert.StartsWith("Money ", k.Ui.Q<Label>("turn-report-causes-treasury").text, "Money's main cause is the economy's line");
        Assert.IsNull(k.Ui.Root.Q("turn-report-causes-support"), "no cause names Public support (an average of the indicators)");
    }

    [Test]
    public void ItCoversTheHudUntilClosedAndThenTheCardIsBack()
    {
        using var k = new Kit(En);
        k.Bridge.EndTurn();   // the second turn opens with The First Truce and The Altalena
        Assert.IsTrue(k.Report.IsOpen);
        Assert.IsFalse(k.Q("top-bar").enabledSelf, "the top bar is covered");
        Assert.IsFalse(k.Q("left-column").enabledSelf, "the screens' buttons are covered");
        Assert.IsFalse(k.Q("event-layer").enabledSelf, "the decision card is covered");
        Assert.IsTrue(k.Q("turn-report").enabledSelf, "the report itself takes input");
        UiTestKit.Press(k.Ui.Q<Button>("turn-report-close"));
        Assert.IsFalse(k.Report.IsOpen);
        Assert.AreEqual(DisplayStyle.None, k.Q("turn-report").style.display.value);
        Assert.IsTrue(k.Q("top-bar").enabledSelf && k.Q("left-column").enabledSelf && k.Q("event-layer").enabledSelf, "everything is back");
        Assert.IsFalse(k.Q("turn-report").enabledSelf, "a closed report takes no input");
        Assert.IsFalse(k.Report.Close(), "closing it again does nothing");
    }

    [Test]
    public void CancelClosesItAndSeeAllOpensTheWhyScreen()
    {
        using var k = new Kit(En);
        k.Bridge.EndTurn();
        var close = k.Ui.Q<Button>("turn-report-close");
        using (var e = NavigationCancelEvent.GetPooled()) { e.target = close; close.SendEvent(e); }
        Assert.IsFalse(k.Report.IsOpen, "Esc or a gamepad's Cancel closes it");
        var turn = k.Bridge.EndTurn();
        Assert.IsTrue(k.Report.IsOpen, "each turn has its own report");
        UiTestKit.Press(k.Ui.Q<Button>("turn-report-all"));
        Assert.IsFalse(k.Report.IsOpen);
        Assert.AreEqual("why", k.Nav.Current, "See all opens the Why screen");
        Assert.AreEqual(turn.Why.Count, k.Ui.Root.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList().Count, "with every cause of the turn");
    }

    [Test]
    public void AnOutcomeClipCoversTheReportAndHandsItBack()
    {
        using var k = new Kit(En);
        k.Bridge.EndTurn();
        var root = k.Q("root");
        root.AddToClassList(OutcomePresenter.OpenClass);
        ScreenNav.DisableCovered(root);
        Assert.IsFalse(k.Q("turn-report").enabledSelf, "under a clip the report takes no input");
        root.RemoveFromClassList(OutcomePresenter.OpenClass);
        ScreenNav.DisableCovered(root);
        Assert.IsTrue(k.Q("turn-report").enabledSelf, "and takes it again when the clip ends");
        Assert.IsFalse(k.Q("event-layer").enabledSelf, "the card stays covered by the report");
    }

    [Test]
    public void ARefusedOrderStillReportsTheTurnAndARefusedTurnDoesNot()
    {
        // A turn that ran with one order refused: the turn happened, so it is reported.
        using (var k = new Kit(En, (g, cmds) =>
        {
            var r = g.Step(cmds);
            return new StepResult(r.State, r.Why, new[] { new Rejection(new SetMobilization(0.5), "refused for the test") });
        }))
        {
            k.Bridge.EndTurn();
            Assert.IsTrue(k.Report.IsOpen, "a refused order does not hide the turn's report");
        }
        // A batch the sim refused outright (SimBridge.EndTurn's failure): nothing happened, so there is nothing to report.
        using (var k = new Kit(En, (g, cmds) => throw new System.ArgumentException("refused for the test")))
        {
            LogAssert.Expect(LogType.Warning, "End Turn refused: refused for the test");
            k.Bridge.EndTurn();
            Assert.IsFalse(k.Report.IsOpen, "a refused turn has no report");
        }
    }

    [Test]
    public void HebrewShowsTheHebrewWordsAndCaptions()
    {
        using var k = new Kit(He);
        k.Bridge.EndTurn();
        Assert.AreEqual(He["report.title"], k.Ui.Q<Label>("turn-report-title").text);
        Assert.AreEqual(He["report.intro"], k.Ui.Q<Label>("turn-report-intro").text);
        Assert.AreEqual(He["report.all"], k.Ui.Q<Button>("turn-report-all").text);
        Assert.AreEqual(He["report.close"], k.Ui.Q<Button>("turn-report-close").text);
        var heading = k.Ui.Q<Label>("turn-report-heading-security").text;
        StringAssert.StartsWith(He["hud.caption.security"] + ":", heading);
        // In a right-to-left line the change keeps its sign in front ("+2%", not "2%+"): UiText.Format marks it (#543).
        StringAssert.Contains(UiText.LeftToRightMark + k.Ui.Q<Label>("security-delta").text + UiText.LeftToRightMark, heading);
        CollectionAssert.AreEqual(HudModel.Causes(k.Bridge.Session.Last, "security"), k.Ui.Q<Label>("turn-report-causes-security").text.Split('\n'),
            "the causes are the sim's messages, as on the Why screen");
    }

    /// <summary>The smoke run plays as a player does: it closes each turn's report before the next turn (GameRoot.Smoke).</summary>
    [Test]
    public void TheSmokeRunClosesTheReportEachTurn()
    {
        using var k = new Kit(En);
        var log = new List<string>();
        var run = GameRoot.Smoke(k.Ui.Root, k.Bridge, k.Card, 3, log.Add);
        while (run.MoveNext()) { }
        StringAssert.StartsWith("SMOKE OK 3 turns", log[log.Count - 1], string.Join("\n", log));
        Assert.IsFalse(k.Report.IsOpen);
    }

    /// <summary>At a laptop's 1280x720 and a phone's 390x844, in English and Hebrew: the report lies in the window, every
    /// line within its panel, its two buttons shown, and the keyboard focus on Close.</summary>
    [UnityTest]
    public IEnumerator ItFitsLaptopAndPhoneInBothLanguages()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in new[] { (1280, 720), (390, 844) })
            {
                string at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                var rt = new RenderTexture(w, h, 0);
                var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
                ps.targetTexture = rt;
                var go = new GameObject("TurnReportLayout"); go.SetActive(false);
                var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps;
                doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
                go.SetActive(true);
                var root = doc.rootVisualElement;
                root.Q("root").RemoveFromClassList("loading");
                if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
                var text = hebrew ? He : En;
                var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
                var owned = new List<System.IDisposable>();
                owned.Add(new HudPresenter(root, bridge, text));
                var nav = new ScreenNav(root); owned.Add(nav);
                owned.Add(new EventCardPresenter(root, bridge, text, null));
                var report = new TurnReportPresenter(root, bridge, text, nav); owned.Add(report);
                try
                {
                    bridge.EndTurn();
                    for (int i = 0; i < 30 && !(root.Q("turn-report-panel").layout.width > 0); i++) yield return null;
                    for (int i = 0; i < 3; i++) yield return null;
                    var window = root.worldBound; var panel = root.Q("turn-report-panel").worldBound;
                    if (!(panel.width > 0 && panel.height > 0)) problems.Add($"{at}: the report is not shown");
                    if (panel.xMin < window.xMin - 0.5f || panel.yMin < window.yMin - 0.5f || panel.xMax > window.xMax + 0.5f || panel.yMax > window.yMax + 0.5f)
                        problems.Add($"{at}: the report {panel} leaves the window {window}");
                    foreach (var label in root.Q("turn-report-panel").Query<Label>().ToList().Where(l => !string.IsNullOrEmpty(l.text)))
                        if (label.worldBound.xMin < panel.xMin - 1 || label.worldBound.xMax > panel.xMax + 1)
                            problems.Add($"{at}: '{label.name}' {label.worldBound} is wider than the report {panel}");
                    foreach (var name in new[] { "turn-report-all", "turn-report-close" })
                    {
                        var b = root.Q<Button>(name).worldBound;
                        if (!(b.height > 0) || b.yMax > panel.yMax + 1 || b.xMin < panel.xMin - 1 || b.xMax > panel.xMax + 1) problems.Add($"{at}: {name} {b} is not inside the report {panel}");
                    }
                    if (root.focusController?.focusedElement != root.Q<Button>("turn-report-close")) problems.Add($"{at}: the focus is on {(root.focusController?.focusedElement as VisualElement)?.name ?? "nothing"}, not Close");
                }
                finally
                {
                    for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
                    Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
                }
            }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }
}
