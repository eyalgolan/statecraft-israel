#nullable enable
using System;
using System.Linq;
using System.Collections;
using UnityEngine.TestTools;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>#955: the read-only view consumes the real sim helper, never pending/queued answers.</summary>
public class TimelineScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text(bool hebrew = false) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(hebrew ? UiPaths.StringsHebrew : UiPaths.Strings).text);
    static IDisposable Presenter(VisualElement root, SimBridge bridge, UiText text) => new TimelinePresenter(root, bridge, text);
    static GameSession Historical(int turns, Content? content = null)
    {
        var c = content ?? C; var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        for (int i = 0; i < turns; i++) g.Step(GoldenRun.Orders(c, g.State).ToArray());
        return g;
    }
    static VisualElement[] Rows(VisualElement root) => root.Query<VisualElement>(className: "timeline-row").ToList().ToArray();

    static (GameSession game, GameEvent ev, EventOption chosen, EventOption record) ResolvedIdf(Content content, bool alternative)
    {
        var game = GameSession.New(content, GoldenRun.Seed, Mode.Historical);
        for (int i = 0; !game.State.PendingEvents.Contains("idf_founding") && i < 2; i++) game.Step(Array.Empty<Command>());
        Assert.Contains("idf_founding", game.State.PendingEvents, "a real pending event is required");
        var ev = content.Events.Single(e => e.Id == "idf_founding");
        var record = ev.Options.Single(o => o.Historical);
        var chosen = alternative ? ev.Options.First(o => !o.Historical) : record;
        var orders = GoldenRun.Orders(content, game.State)
            .Where(c => !(c is ChooseEventOption choice && choice.EventId == ev.Id)).ToList();
        orders.Add(new ChooseEventOption(ev.Id, chosen.Id));
        game.Step(orders);
        Assert.AreEqual(chosen.Id, game.State.ChosenOptions[ev.Id], "the normal sim path resolved the chosen answer");
        return (game, ev, chosen, record);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ExistingEventNarrativeUsesTheRequestedLanguage(bool hebrew, bool alternative)
    {
        var content = ContentLoader.Load(Paths.Content);
        var (game, ev, chosen, record) = ResolvedIdf(content, alternative);
        Assert.IsNotEmpty(ev.TitleHe); Assert.IsNotEmpty(chosen.TextHe); Assert.IsNotEmpty(record.TextHe);
        Assert.IsNull(Timeline.Variant(content, game.State, ev), "this is the real base event, not an untranslated variant");
        using var ui = new UiTestKit(); var text = Text(hebrew); var bridge = new SimBridge(game, text);
        using var screen = Presenter(ui.Root, bridge, text); using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var row = Rows(ui.Root).Single(r => (string)r.userData == ev.Id);
        Assert.AreEqual(hebrew ? ev.TitleHe : EventSystem.Title(content, game.State, ev), row.Q<Label>("timeline-title").text);
        Assert.AreEqual(hebrew ? chosen.TextHe : chosen.Text, row.Q<Label>("timeline-yours").text);
        Assert.AreEqual(hebrew ? record.TextHe : record.Text, row.Q<Label>("timeline-record").text);
    }

    [TestCase("title")]
    [TestCase("chosen")]
    [TestCase("record")]
    public void MissingHebrewNarrativeFieldsFallBackIndependently(string missing)
    {
        // Test-only absence in an independently loaded content copy; no shipped translation or review flag changes.
        var content = ContentLoader.Load(Paths.Content);
        var (game, ev, chosen, record) = ResolvedIdf(content, true);
        if (missing == "title") ev.TitleHe = "";
        else if (missing == "chosen") chosen.TextHe = "";
        else record.TextHe = "";
        using var ui = new UiTestKit(); var text = Text(true); var bridge = new SimBridge(game, text);
        using var screen = Presenter(ui.Root, bridge, text); using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var row = Rows(ui.Root).Single(r => (string)r.userData == ev.Id);
        Assert.AreEqual(missing == "title" ? EventSystem.Title(content, game.State, ev) : ev.TitleHe, row.Q<Label>("timeline-title").text);
        Assert.AreEqual(missing == "chosen" ? chosen.Text : chosen.TextHe, row.Q<Label>("timeline-yours").text);
        Assert.AreEqual(missing == "record" ? record.Text : record.TextHe, row.Q<Label>("timeline-record").text);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void VariantTitlesKeepTheirExistingEnglishFallback(bool hebrew)
    {
        var content = ContentLoader.Load(Paths.Content);
        var (game, ev, _, _) = ResolvedIdf(content, false);
        ev.Variants.Add(new EventVariant { Title = "Fixture variant title" });
        using var ui = new UiTestKit(); var text = Text(hebrew); var bridge = new SimBridge(game, text);
        using var screen = Presenter(ui.Root, bridge, text); using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var row = Rows(ui.Root).Single(r => (string)r.userData == ev.Id);
        Assert.AreEqual("Fixture variant title", row.Q<Label>("timeline-title").text,
            "a translated base title must not hide the active variant's untranslated title");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NarrativeOfficePlaceholdersUseNamesInTheRequestedLanguage(bool hebrew)
    {
        var content = ContentLoader.Load(Paths.Content);
        var (game, ev, chosen, record) = ResolvedIdf(content, true);
        // Test-only placeholders, not new Hebrew prose or an assertion about the historical event's wording.
        ev.Title = ev.TitleHe = chosen.Text = chosen.TextHe = record.Text = record.TextHe = "{defense_minister}";
        using var ui = new UiTestKit(); var text = Text(hebrew); var bridge = new SimBridge(game, text);
        var expected = Cabinet.Fill(content, game.State, "{defense_minister}", EventSystem.FillDate(ev, game.State.Date),
            person => ContentNames.Of(text, person));
        Assert.AreEqual(hebrew, UiDirection.HasRightToLeft(expected), "existing content supplies the requested name");
        StringAssert.DoesNotContain("{", expected, "the office placeholder must actually be expanded");
        using var screen = Presenter(ui.Root, bridge, text); using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var row = Rows(ui.Root).Single(r => (string)r.userData == ev.Id);
        foreach (var name in new[] { "timeline-title", "timeline-yours", "timeline-record" })
            Assert.AreEqual(expected, row.Q<Label>(name).text, name);
    }

    [Test]
    public void TheHudOpensTheTimelineAndCloseReturnsItsFocus()
    {
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        var b = ui.Q<Button>("nav-timeline");
        UiTestKit.Press(b);
        Assert.AreEqual("timeline", nav.Current);
        Assert.IsTrue(ScreenNav.IsOpen(ui.Q<VisualElement>("screen-timeline")));
        var close = ui.Q<Button>("screen-close-timeline"); close.Focus(); UiTestKit.Press(close);
        Assert.IsNull(nav.Current);
        Assert.AreSame(b, ui.Root.panel.focusController.focusedElement);
    }

    [Test]
    public void OneRowPerResolvedEventAndNoMarkOnTheRecord()
    {
        using var ui = new UiTestKit();
        var g = Historical(6); var text = Text(); var bridge = new SimBridge(g, text);
        using var screen = Presenter(ui.Root, bridge, text);
        using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var entries = Timeline.Entries(C, g.State); Assert.Greater(entries.Count, 0, "real resolved history");
        CollectionAssert.AreEqual(entries.Select(e => e.Event), Rows(ui.Root).Select(r => r.userData));
        Assert.Zero(ui.Root.Query<VisualElement>(className: "timeline-diverged").ToList().Count);
        foreach (var row in Rows(ui.Root))
        {
            Assert.IsNotEmpty(row.Q<Label>("timeline-title").text);
            Assert.AreEqual(row.Q<Label>("timeline-yours").text, row.Q<Label>("timeline-record").text);
            foreach (var marker in new[] { "timeline-attribution", "timeline-design", "timeline-rejoined", "timeline-difference" })
                Assert.IsNull(row.Q<Label>(marker), "the recorded path must not acquire a false marker: " + marker);
        }
    }

    [Test]
    public void ARoadNotTakenIsMarkedBesideTheRecord()
    {
        using var ui = new UiTestKit(); var text = Text();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        // Force only a real pending event's alternative; all other answers use the reference player.
        for (int i = 0; !g.State.PendingEvents.Contains("idf_founding") && i < 2; i++) g.Step(Array.Empty<Command>());
        Assert.Contains("idf_founding", g.State.PendingEvents);
        var e = C.Events.Single(x => x.Id == "idf_founding"); var alternative = e.Options.First(o => !o.Historical);
        var orders = GoldenRun.Orders(C, g.State).Where(x => !(x is ChooseEventOption choice && choice.EventId == e.Id)).ToList();
        orders.Add(new ChooseEventOption(e.Id, alternative.Id)); g.Step(orders);
        var bridge = new SimBridge(g, text); using var screen = Presenter(ui.Root, bridge, text);
        using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var row = Rows(ui.Root).Single(r => (string)r.userData == e.Id);
        Assert.IsTrue(row.ClassListContains("timeline-diverged"));
        Assert.AreEqual("≠", row.Q<Label>("timeline-difference").text, "difference is visible without relying on colour");
        StringAssert.Contains(alternative.Text, row.Q<Label>("timeline-yours").text);
        StringAssert.Contains(e.Options.Single(o => o.Historical).Text, row.Q<Label>("timeline-record").text);
        Assert.AreNotEqual(row.Q<Label>("timeline-yours").text, row.Q<Label>("timeline-record").text);
    }

    [Test]
    public void PendingAndQueuedAnswersAreAbsentAndReadingChangesNoStateOrSave()
    {
        using var ui = new UiTestKit(); var text = Text(); var g = Historical(6);
        for (int i = 0; i < 12 && g.State.PendingEvents.Count == 0; i++) g.Step(GoldenRun.Orders(C, g.State).ToArray());
        Assert.Greater(Timeline.Entries(C, g.State).Count, 0, "readonly checks must exercise resolved rows");
        Assert.Greater(g.State.PendingEvents.Count, 0, "real pending choice needed for the queue guard");
        var e = C.Events.Single(x => x.Id == g.State.PendingEvents.First());
        var bridge = new SimBridge(g, text); bridge.Queue(new ChooseEventOption(e.Id, e.Options.First().Id));
        Assert.AreEqual(1, bridge.Queued.Count);
        var state = g.State.Hash(); var save = g.Save.Write(); var queued = bridge.Queued.Single();
        using var screen = Presenter(ui.Root, bridge, text); using var nav = new ScreenNav(ui.Root);
        nav.Open("timeline"); nav.Close(); nav.Open("timeline");
        Assert.IsFalse(Rows(ui.Root).Any(r => (string)r.userData == e.Id), "pending/queued is not resolved");
        Assert.AreEqual(state, g.State.Hash()); Assert.AreEqual(save, g.Save.Write()); Assert.AreSame(queued, bridge.Queued.Single());
    }

    [Test]
    public void NewTurnsRefreshRowsAndDisposedScreensStopListening()
    {
        using var ui = new UiTestKit(); var text = Text(); var bridge = new SimBridge(Historical(1), text);
        var screen = Presenter(ui.Root, bridge, text); using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var before = Rows(ui.Root).Length;
        for (int i = 0; i < 6; i++) { foreach (var c in GoldenRun.Orders(C, bridge.State)) bridge.Queue(c); bridge.EndTurn(); }
        Assert.Greater(Rows(ui.Root).Length, before);
        CollectionAssert.AreEqual(Timeline.Entries(C, bridge.State).Select(e => e.Event), Rows(ui.Root).Select(r => r.userData));
        screen.Dispose(); var rows = Rows(ui.Root);
        bridge.EndTurn(); nav.Close(); nav.Open("timeline"); Assert.AreSame(rows[0], Rows(ui.Root)[0], "disposed presenter cannot rebuild the tree");
    }

    [Test]
    public void AFixtureRejoinKeepsBothTheOriginalRoadAndTheMissingRecordFallback([Values(false, true)] bool hebrew)
    {
        // UI-only fixture: no consequence card is added to shipped content and no historical attribution is invented.
        var c = ContentLoader.Load(Paths.Content); var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        c.Events.Add(new GameEvent { Id = "fixture_parent", Title = "Fixture parent", From = new DateTime(1948, 5, 15),
            Options = { new EventOption { Id = "record", Text = "Recorded answer", Historical = true }, new EventOption { Id = "road", Text = "Other answer" } } });
        c.Events.Add(new GameEvent { Id = "fixture_followup", Kind = EventKinds.Consequence, Title = "Fixture follow-up", From = new DateTime(1948, 5, 16),
            Options = { new EventOption { Id = "return", Text = "Return answer", Rejoin = "fixture_parent" } } });
        g.State.ChosenOptions["fixture_followup"] = "return"; g.State.ChosenOptions["fixture_parent"] = "road";
        using var ui = new UiTestKit(); var text = Text(hebrew); var bridge = new SimBridge(g, text);
        using var screen = Presenter(ui.Root, bridge, text); using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var rows = Rows(ui.Root); Assert.AreEqual(2, rows.Length);
        Assert.AreEqual("fixture_parent", rows[0].userData); Assert.AreEqual("fixture_followup", rows[1].userData);
        Assert.IsTrue(rows[0].ClassListContains("timeline-diverged"));
        Assert.AreEqual("Other answer", rows[0].Q<Label>("timeline-yours").text);
        Assert.AreEqual("Recorded answer", rows[0].Q<Label>("timeline-record").text);
        Assert.AreEqual(text["timeline.rejoined"], rows[0].Q<Label>("timeline-rejoined").text);
        Assert.AreEqual(text["timeline.no_record"], rows[1].Q<Label>("timeline-record").text);
        Assert.AreEqual(hebrew ? "15 במאי 1948" : "15 May 1948", rows[0].Q<Label>("timeline-date").text);
        Assert.AreEqual(hebrew ? "16 במאי 1948" : "16 May 1948", rows[1].Q<Label>("timeline-date").text, "use each event window, not the current game date");
    }

    [Test]
    public void ReopeningAndQueuingKeepTheSameRowsButReplacingTheSessionRefreshesThem()
    {
        using var ui = new UiTestKit(); var text = Text(); var bridge = new SimBridge(Historical(6), text);
        using var screen = Presenter(ui.Root, bridge, text); using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var first = Rows(ui.Root)[0]; nav.Close(); nav.Open("timeline");
        Assert.AreSame(first, Rows(ui.Root)[0], "reading again must not reset the scroll tree");
        bridge.Queue(new SetMobilization(1.0)); Assert.AreSame(first, Rows(ui.Root)[0], "an unrelated queued order cannot rebuild history");
        bridge.Replace(GameSession.New(C, GoldenRun.Seed, Mode.Historical));
        Assert.IsEmpty(Rows(ui.Root), "a new game cannot retain the previous session's history");
    }

    [Test]
    public void LanguageRebuildUsesNewHeadingsAndTheExistingEventText()
    {
        using var ui = new UiTestKit(); var en = Text(); var he = Text(true); var bridge = new SimBridge(Historical(6), en);
        using var nav = new ScreenNav(ui.Root);
        var old = Presenter(ui.Root, bridge, en); nav.Open("timeline"); old.Dispose();
        using var current = Presenter(ui.Root, bridge, he); nav.Open("timeline");
        Assert.AreEqual(he["timeline.title"], ui.Q<Label>("timeline-screen-title").text);
        Assert.AreEqual(he["timeline.yours"], ui.Q<Label>("timeline-yours-heading").text);
        Assert.AreEqual(he["timeline.record"], ui.Q<Label>("timeline-record-heading").text);
        Assert.IsNotEmpty(Rows(ui.Root)); Assert.IsNotEmpty(Rows(ui.Root)[0].Q<Label>("timeline-yours").text);
    }

    [UnityTest]
    public IEnumerator ThePairedAnswersFitLaptopAndPhoneAndPageKeysReachTheLastRow()
    {
        foreach (var hebrew in new[] { false, true })
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(390, 844) })
            {
                var rt = new RenderTexture(size.x, size.y, 0);
                var ps = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt;
                var go = new GameObject("TimelineLayoutTest"); go.SetActive(false);
                var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps;
                doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml); go.SetActive(true);
                var root = doc.rootVisualElement; root.Q("root").RemoveFromClassList("loading");
                if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
                var text = Text(hebrew); var content = ContentLoader.Load(Paths.Content); var game = Historical(12, content);
                // UI-only long attribution fixture; never added to shipped content or factual review records.
                content.Events.Add(new GameEvent { Id = "fixture_long_attribution", Title = "Fixture attribution wrapping", From = new DateTime(1948, 5, 15),
                    Options = { new EventOption { Id = "record", Text = "Fixture recorded answer", Historical = true }, new EventOption {
                        Id = "road", Text = "Fixture alternative answer", Alternative = new OptionAlternative { Basis = OptionAlternative.Debated,
                            By = string.Join(" ", Enumerable.Repeat("Fixture body with a long name", 8)), Date = new DateTime(1948, 5, 15) } } } });
                game.State.ChosenOptions["fixture_long_attribution"] = "road";
                var bridge = new SimBridge(game, text);
                var hud = new HudPresenter(root, bridge, text);
                var screen = Presenter(root, bridge, text);
                var nav = new ScreenNav(root); nav.Open("timeline");
                var direction = new UiDirection(root, bridge);
                try
                {
                    for (int i = 0; i < 12; i++) yield return null;
                    var panel = root.Q("screen-timeline"); var window = root.worldBound;
                    Assert.Greater(panel.worldBound.width, 0); Assert.Greater(panel.worldBound.height, 0);
                    Assert.GreaterOrEqual(panel.worldBound.xMin, window.xMin); Assert.LessOrEqual(panel.worldBound.xMax, window.xMax);
                    Assert.GreaterOrEqual(panel.worldBound.yMin, window.yMin); Assert.LessOrEqual(panel.worldBound.yMax, window.yMax);
                    var view = root.Q<ScrollView>("timeline-list"); Assert.Greater(view.contentViewport.worldBound.height, 0);
                    foreach (var row in Rows(root))
                    {
                        var yours = row.Q<Label>("timeline-yours"); var record = row.Q<Label>("timeline-record");
                        Assert.AreEqual(hebrew, yours.worldBound.center.x > record.worldBound.center.x, "columns follow the interface language");
                        foreach (var label in row.Query<Label>().ToList())
                        {
                            Assert.IsNotEmpty(label.text, label.name);
                            Assert.Greater(label.contentRect.width, 0, label.name);
                            Assert.GreaterOrEqual(label.worldBound.xMin, view.contentViewport.worldBound.xMin - 1f, label.name);
                            Assert.LessOrEqual(label.worldBound.xMax, view.contentViewport.worldBound.xMax + 1f, label.name);
                            var measured = label.MeasureTextSize(label.text, label.contentRect.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                            Assert.LessOrEqual(measured.y, label.contentRect.height + 1f, label.name + " text must fit rather than clip");
                        }
                    }
                    var close = root.Q<Button>("screen-close-timeline"); close.Focus();
                    Assert.Greater(view.verticalScroller.highValue, 0, "real history must overflow this reading screen");
                    Assert.IsTrue(KeyScroll.Press(root, "timeline", false, KeyScroll.Key.End));
                    Assert.AreEqual(view.verticalScroller.highValue, view.scrollOffset.y, 0.5f);
                    Assert.AreSame(close, root.panel.focusController.focusedElement, "scroll keys never lose the focused Close button");
                    Assert.IsTrue(KeyScroll.Press(root, "timeline", false, KeyScroll.Key.Home));
                    Assert.AreEqual(0f, view.scrollOffset.y, 0.5f);
                }
                finally
                {
                    nav.Dispose(); screen.Dispose(); hud.Dispose(); direction.Dispose();
                    UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(ps); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
                }
            }
    }

    [Test]
    public void AConsideredChosenAlternativeShowsItsOwnAttributionOnlyAfterTheChoice(
        [Values(OptionAlternative.Proposed, OptionAlternative.Debated, OptionAlternative.Design, "none")] string basis,
        [Values(false, true)] bool hebrew)
    {
        // Entirely test-only metadata: "Fixture body" is not a real institution or an assertion about history.
        var c = ContentLoader.Load(Paths.Content); var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        var option = new EventOption { Id = "road", Text = "Fixture answer", Alternative = basis == "none" ? null : new OptionAlternative {
            Basis = basis, By = basis == OptionAlternative.Design ? "" : "Fixture body",
            Date = basis == OptionAlternative.Design ? (DateTime?)null : new DateTime(1948, 5, 16) } };
        c.Events.Add(new GameEvent { Id = "fixture_attribution", Title = "Fixture attribution", From = new DateTime(1948, 5, 15),
            Options = { new EventOption { Id = "record", Text = "Fixture recorded answer", Historical = true }, option } });
        // One separate pending alternative must never disclose an attribution before the player chooses it.
        c.Events.Add(new GameEvent { Id = "fixture_pending", Title = "Fixture pending", From = new DateTime(1948, 5, 15),
            Options = { new EventOption { Id = "record", Text = "Fixture record", Historical = true }, new EventOption {
                Id = "road", Text = "Fixture pending answer", Alternative = new OptionAlternative { Basis = OptionAlternative.Proposed,
                    By = "Pending fixture body", Date = new DateTime(1948, 5, 15) } } } });
        g.State.ChosenOptions["fixture_attribution"] = "road"; g.State.PendingEvents.Add("fixture_pending");
        using var ui = new UiTestKit(); var text = Text(hebrew); var bridge = new SimBridge(g, text);
        using var screen = Presenter(ui.Root, bridge, text); using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        Assert.AreEqual(1, Rows(ui.Root).Length, "pending attribution is not shown as history");
        var label = Rows(ui.Root)[0].Q<Label>("timeline-attribution");
        if (basis == "none")
        {
            Assert.IsNull(label, "no invented attribution for old content with no metadata");
            Assert.IsNull(Rows(ui.Root)[0].Q<Label>("timeline-design"), "no invented design classification for metadata-free content");
            return;
        }
        if (basis == OptionAlternative.Design)
        {
            Assert.IsNull(label, "design cannot be represented as a documented proposal");
            var design = Rows(ui.Root)[0].Q<Label>("timeline-design"); Assert.IsNotNull(design, "GDD195 requires identifying a design assumption");
            Assert.AreEqual(hebrew ? "הנחת תכנון." : "Design assumption.", design.text);
            Assert.IsFalse(design.focusable); return;
        }
        Assert.IsNotNull(label, "the chosen considered alternative needs its recorded attribution (Q-ALT-7)");
        Assert.IsNull(Rows(ui.Root)[0].Q<Label>("timeline-design"), "a considered alternative is not a design-only assumption");
        if (!hebrew) Assert.AreEqual(basis == OptionAlternative.Proposed
            ? "Not adopted. Proposed by Fixture body, 16 May 1948."
            : "Not adopted. Debated by Fixture body, 16 May 1948.", label.text);
        else
        {
            StringAssert.Contains(basis == OptionAlternative.Proposed ? "הוצע בידי" : "נדון בידי", label.text);
            StringAssert.Contains("Fixture body", label.text); StringAssert.Contains("16 במאי 1948", label.text);
        }
        Assert.IsFalse(label.focusable, "attribution adds no keyboard stop");
    }

}
