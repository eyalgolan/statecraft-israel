#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

/// <summary>Decision videos, phase 3 (docs/event-videos.md): an outcome clip plays at End Turn only for the historical option
/// the player ordered and the turn applied, captioned as the order; never for a counterfactual option, a delegated answer,
/// a refused order or Deport. It is drawn over the screens, closes an open one, and has its own player, so the decision
/// card cannot take it. The layer takes the keyboard focus: Enter or Space skips one clip, Esc skips them all, a click
/// skips one; the focus then goes back. A sensitive one waits for the owner's review before a shareable build.</summary>
public class OutcomeClipTests
{
    static readonly Content C = BuildGateTests.Checked();   // the starting forces' rows fact-checked: these tests are about the clips (R-UNIT-006)
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(UiPaths.HebrewText(), "strings.he.yaml");
    static string Yaml => File.ReadAllText(EventVideoMapping.Path);

    [Test]
    public void EveryOutcomeIsAHistoricalOrderWithItsClipAndNoDeport()
    {
        var rows = EventVideoMapping.ParseOutcomes(Yaml);
        CollectionAssert.AreEquivalent(new[] { "maabarot_policy", "hula_drainage", "operation_horev", "altalena", "first_new_yishuv", "iraqi_exodus", "austerity_1949", "absorb_immigrants" },
            rows.Select(r => r.Event), "the three of 2026-10-14, the four the owner approved on 2026-10-20 (#1102) and absorb_immigrants (owner, 2026-10-24: \"Add v8, 6 s\")");
        var problems = EventVideoMapping.OutcomeProblems(rows, C, f => File.Exists(Path.Combine(EventVideoMapping.ClipFolder, f)));
        Assert.IsEmpty(problems, string.Join("\n", problems));
        Assert.IsTrue(rows.All(r => !string.IsNullOrWhiteSpace(r.AltHe) && r.AltHeReview == "agent-checked"), "every shipped outcome has agent-checked Hebrew alt text");
        Assert.AreEqual(8, EventVideoMapping.LoadOutcomes().Count, "the game finds them in Resources");
    }

    [Test]
    public void TheMappingRefusesACounterfactualOrderAndDeport()
    {
        var row = EventVideoMapping.ParseOutcomes(Yaml).First(r => r.Event == "maabarot_policy");
        var other = C.Events.First(e => e.Id == "maabarot_policy").Options.First(o => !o.Historical);
        var counter = new EventVideoMapping.Row { Event = row.Event, Option = other.Id, File = row.File, Frames = 84, Source = row.Source, SourceSha256 = row.SourceSha256, Generator = "x", Generated = "x", Alt = row.Alt, AiGenerated = true };
        Assert.IsTrue(EventVideoMapping.OutcomeProblems(new[] { counter }, C, _ => true).Any(p => p.Contains("not the historical option")));
        var c = ContentLoader.Load(Paths.Content);
        c.Events.First(e => e.Id == "maabarot_policy").Options.Add(new EventOption { Id = "deport", Text = "x" });
        Assert.IsTrue(EventVideoMapping.OutcomeProblems(new[] { row }, c, _ => true).Any(p => p.Contains("Deport never gets any video")));
    }

    /// <summary>The game's parts that meet an outcome clip, on a rendered panel: the HUD, the screens (ScreenNav), the
    /// decision card with its own player, and the outcome clips with theirs.</summary>
    sealed class Rig : IDisposable
    {
        public readonly VisualElement Root; public readonly SimBridge Bridge; public readonly ScreenNav Nav;
        public readonly EventCardPresenter Card; public readonly OutcomePresenter Outcomes;
        public readonly CardVideo CardPlayer = new CardVideo(), OutcomePlayer = new CardVideo();
        public int Closed;
        readonly GameObject go; readonly PanelSettings ps; readonly RenderTexture rt; readonly List<IDisposable> owned = new List<IDisposable>();

        public Rig(string[] events, Func<GameSession, IReadOnlyList<Command>, StepResult>? step = null, UiText? text = null,
            Content? content = null, IReadOnlyList<EventVideoMapping.Row>? outcomes = null)
        {
            var t = text ?? Text; var c = content ?? C;
            rt = new RenderTexture(1920, 1080, 0);
            ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            go = new GameObject("OutcomeTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            Root = doc.rootVisualElement;
            Root.Q("root").RemoveFromClassList("loading");
            var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
            var start = g.State.Date;
            g.State.Date = c.Events.First(e => e.Id == events[0]).From;
            ContentWindows.Update(c, g.State, start);     // as a turn does when the date moves (#80)
            g.State.PendingEvents.Clear(); g.State.PendingEvents.AddRange(events);
            Bridge = new SimBridge(g, t, step);
            owned.Add(new HudPresenter(Root, Bridge, t));
            Nav = new ScreenNav(Root); owned.Add(Nav);
            Card = new EventCardPresenter(Root, Bridge, t, null, null, CardPlayer, _ => null); owned.Add(Card);
            Outcomes = new OutcomePresenter(Root, Bridge, t, OutcomePlayer, outcomes ?? EventVideoMapping.ParseOutcomes(Yaml), () => Closed++, () => Nav.Close());
            owned.Add(Outcomes);
            owned.Add(CardPlayer); owned.Add(OutcomePlayer);
        }

        public void Order(string ev, string option) => Assert.IsNull(Bridge.Queue(new ChooseEventOption(ev, option)), $"{ev}/{option} is taken");
        public bool Open => Root.Q("root").ClassListContains(OutcomePresenter.OpenClass);
        public VisualElement Layer => Root.Q("outcome-layer");
        public Focusable? Focused => Root.panel.focusController.focusedElement;
        public void Send(EventBase e, VisualElement target) { e.target = target; target.SendEvent(e); }
        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    [Test]
    public void TheHistoricalOrderPlaysItsClipAtEndTurnCaptionedAsTheOrder()
    {
        LogAssert.ignoreFailingMessages = true;   // the edit-mode VideoPlayer may log
        using var r = new Rig(new[] { "maabarot_policy" });
        r.Order("maabarot_policy", "maabarot");
        Assert.IsFalse(r.Open, "nothing at the click: the order takes effect at End Turn");
        Assert.IsNull(r.OutcomePlayer.Current);
        r.Bridge.EndTurn();
        Assert.AreEqual("maabarot_policy", r.Outcomes.Showing?.Event);
        StringAssert.EndsWith("/video/maabarot_policy-outcome.mp4", r.OutcomePlayer.Current, "its clip is asked of its own player");
        var option = C.Events.First(e => e.Id == "maabarot_policy").Options.First(o => o.Id == "maabarot");
        Assert.AreEqual("Your order: " + option.Text, r.Outcomes.Caption);
        Assert.IsTrue(r.Open);
        Assert.IsFalse(r.Root.Q("top-bar").enabledSelf, "the bar waits while it plays");
        Assert.IsFalse(r.Root.Q("info-panel").enabledSelf);
        Assert.IsFalse(r.Root.Q("event-layer").enabledSelf);
        Assert.AreEqual("AI-generated illustration", r.Root.Q<Label>("outcome-label").text);
    }

    /// <summary>#1210: in Hebrew the caption is the order in the option's text_he, the way the event card shows it; an
    /// option with no Hebrew falls back to its English text (fail open, like the card).</summary>
    [Test]
    public void InHebrewTheCaptionUsesTheOptionsHebrewTextAndFallsBackToEnglish()
    {
        LogAssert.ignoreFailingMessages = true;
        var he = He;
        var option = C.Events.First(e => e.Id == "maabarot_policy").Options.First(o => o.Id == "maabarot");
        Assert.IsNotEmpty(option.TextHe, "precondition: the option has Hebrew text");
        using (var r = new Rig(new[] { "maabarot_policy" }, text: he))
        {
            r.Order("maabarot_policy", "maabarot");
            r.Bridge.EndTurn();
            Assert.AreEqual(he.Format("outcome.order", option.TextHe), r.Outcomes.Caption);
            Assert.AreEqual(r.Outcomes.Showing!.AltHe, r.Root.Q("outcome-media").tooltip);
            StringAssert.DoesNotContain(option.Text, r.Outcomes.Caption, "no English in the Hebrew caption");
        }
        var c = ContentLoader.Load(Paths.Content);
        c.Events.First(e => e.Id == "maabarot_policy").Options.First(o => o.Id == "maabarot").TextHe = "";
        using (var r = new Rig(new[] { "maabarot_policy" }, text: he, content: c))
        {
            r.Order("maabarot_policy", "maabarot");
            r.Bridge.EndTurn();
            Assert.AreEqual(he.Format("outcome.order", option.Text), r.Outcomes.Caption, "no text_he: the English");
        }
    }

    [Test]
    public void InHebrewABlankOutcomeMediaAltFallsBackToEnglish()
    {
        LogAssert.ignoreFailingMessages = true;
        var source = EventVideoMapping.ParseOutcomes(Yaml).First(r => r.Event == "maabarot_policy");
        source.AltHe = "  ";
        using var r = new Rig(new[] { source.Event }, text: He, outcomes: new[] { source });
        r.Order(source.Event, source.Option);
        r.Bridge.EndTurn();
        Assert.AreEqual(source.Alt, r.Root.Q("outcome-media").tooltip);
    }

    /// <summary>Scenario A of the review: a screen is open at End Turn. The clip closes it and is drawn over the screens
    /// (it comes after them in the tree). Scenario B: something changes under it (bridge.Changed); the card re-renders,
    /// but it has its own player, so the outcome clip plays on.</summary>
    [Test]
    public void AnOpenScreenClosesAndTheCardCannotTakeThePlayer()
    {
        LogAssert.ignoreFailingMessages = true;
        using var r = new Rig(new[] { "maabarot_policy", "hula_drainage" });
        r.Order("maabarot_policy", "maabarot");
        UiTestKit.Press(r.Root.Q<Button>("nav-budget"));
        Assert.AreEqual("budget", r.Nav.Current, "precondition: Budget is open");
        r.Bridge.EndTurn();
        Assert.IsNotNull(r.Outcomes.Showing);
        Assert.IsNull(r.Nav.Current, "the open screen closed when the clip started");
        var parent = r.Layer.parent;
        foreach (var s in ScreenNav.Screens)
            Assert.Greater(parent.IndexOf(r.Layer), parent.IndexOf(r.Root.Q("screen-" + s)), $"the clip is drawn over screen-{s}");
        var playing = r.OutcomePlayer.Current;
        r.Bridge.Queue(new ChooseEventOption("hula_drainage", "continue_works"));   // a change while it plays: the card re-renders
        r.Card.Render();
        Assert.AreEqual(playing, r.OutcomePlayer.Current, "the card never stops or replaces the outcome clip");
        Assert.IsNotNull(r.Outcomes.Showing);
    }

    [UnityTest]
    public IEnumerator TheLayerTakesTheFocusEnterSkipsAndTheFocusGoesBack()
    {
        LogAssert.ignoreFailingMessages = true;
        using var r = new Rig(new[] { "maabarot_policy" });
        yield return Frames(3);
        r.Order("maabarot_policy", "maabarot");
        yield return Frames(2);
        var endTurn = r.Root.Q<Button>("end-turn");
        endTurn.Focus();
        Assert.AreSame(endTurn, r.Focused, "precondition: the keyboard is on End Turn");
        r.Bridge.EndTurn();
        for (var until = Time.realtimeSinceStartup + 2f; r.Focused != r.Layer && Time.realtimeSinceStartup < until;) yield return null;
        Assert.IsNotNull(r.Outcomes.Showing);
        Assert.AreSame(r.Layer, r.Focused, $"the clip has the keyboard focus once styles resolve (layer display {r.Layer.resolvedStyle.display}, enabled {r.Layer.enabledInHierarchy}, canGrabFocus {r.Layer.canGrabFocus})");
        using (var e = NavigationSubmitEvent.GetPooled()) r.Send(e, (VisualElement)r.Focused!);   // Enter / Space / Submit, to whatever has the focus
        Assert.IsNull(r.Outcomes.Showing, "Enter skipped it");
        for (var until = Time.realtimeSinceStartup + 1f; Time.realtimeSinceStartup < until;) yield return null;
        if (r.Card.Shown == null) Assert.AreSame(endTurn, r.Focused, "no card is waiting: the focus is back on End Turn");
        else Assert.AreSame(r.Root.Q("event-card"), r.Focused, $"a new card ({r.Card.Shown.Id}) is waiting: it has the focus");
    }

    /// <summary>Two clips in one turn: a click (through the layer's own pointer handler) skips one and the next plays; Esc
    /// skips the rest.</summary>
    [Test]
    public void AClickSkipsOneAndEscapeSkipsThemAll()
    {
        LogAssert.ignoreFailingMessages = true;
        using (var r = new Rig(new[] { "maabarot_policy", "hula_drainage" }))
        {
            r.Order("maabarot_policy", "maabarot");
            r.Order("hula_drainage", "continue_works");
            r.Bridge.EndTurn();
            var first = r.Outcomes.Showing?.Event;
            Assert.IsNotNull(first);
            using (var e = PointerDownEvent.GetPooled()) r.Send(e, r.Layer);
            Assert.IsNotNull(r.Outcomes.Showing, "a click skips one: the second plays");
            Assert.AreNotEqual(first, r.Outcomes.Showing!.Event);
            Assert.IsTrue(r.Outcomes.HoldsThePointer, "the map ignores the pointer meanwhile");
            Assert.AreEqual(0, r.Closed);
        }
        using (var r = new Rig(new[] { "maabarot_policy", "hula_drainage" }))
        {
            r.Order("maabarot_policy", "maabarot");
            r.Order("hula_drainage", "continue_works");
            r.Bridge.EndTurn();
            Assert.IsNotNull(r.Outcomes.Showing);
            using (var e = NavigationCancelEvent.GetPooled()) r.Send(e, r.Layer);
            Assert.IsNull(r.Outcomes.Showing, "Esc skips them all, not just the one on screen");
            Assert.IsFalse(r.Open);
            Assert.AreEqual(1, r.Closed, "the card is told once");
            Assert.IsTrue(r.Root.Q("top-bar").enabledSelf, "the bar is back");
            Assert.IsNull(r.OutcomePlayer.Current);
        }
    }

    [Test]
    public void ACounterfactualADelegatedOrARefusedOrderGetsNoClip()
    {
        using (var r = new Rig(new[] { "maabarot_policy" }))
        {
            var other = C.Events.First(e => e.Id == "maabarot_policy").Options.First(o => !o.Historical);
            r.Order("maabarot_policy", other.Id);
            r.Bridge.EndTurn();
            Assert.IsNull(r.Outcomes.Showing, "counterfactual: no clip");
            Assert.IsFalse(r.Open);
        }
        using (var r = new Rig(new[] { "maabarot_policy" }))
        {
            r.Bridge.EndTurn();   // unanswered: the minister decides; the player gave no order
            Assert.IsTrue(r.Bridge.State.ChosenOptions.ContainsKey("maabarot_policy"), "precondition: it was delegated");
            Assert.IsNull(r.Outcomes.Showing, "delegated: no clip");
        }
        // The turn ran, the option stands in ChosenOptions (as a hard anchor resolves it), but the order is in the
        // rejections: no "Your order" for an order that was refused.
        using (var r = new Rig(new[] { "maabarot_policy" }, (s, cmds) =>
               {
                   var done = s.Step(cmds);
                   return new StepResult(done.State, done.Why, cmds.Select(c => new Rejection(c, "refused")).ToList());
               }))
        {
            r.Order("maabarot_policy", "maabarot");
            r.Bridge.EndTurn();
            Assert.AreEqual("maabarot", r.Bridge.State.ChosenOptions["maabarot_policy"], "precondition: the option stands");
            Assert.IsNull(r.Outcomes.Showing, "refused: no clip");
        }
    }

    /// <summary>GameRoot subscribes the outcome handler before autosave. SimBridge independently isolates TurnEnded handlers,
    /// so this observes production order and final saving without attributing autosave survival only to the outcome catch.</summary>
    [Test]
    public void AThrowInTheOutcomeCodeNeverSkipsTheAutosave()
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-outcome-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SaveStore(dir);
            Assert.IsFalse(CardVideo.ForcedOff(), "precondition: GameRoot constructs outcomes");
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
            Object.FindFirstObjectByType<MapScene>().Build(inputs.Map);
            var game = Object.FindFirstObjectByType<GameRoot>();
            game.Store = store;
            game.Begin(inputs, 0);
            var outcomes = (OutcomePresenter?)typeof(GameRoot)
                .GetField("outcomes", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(game);
            Assert.IsNotNull(outcomes, "production OutcomePresenter was constructed");
            Assert.IsFalse(File.Exists(store.AutosavePath), "precondition: no turn has been saved");
            bool savedBeforeOutcome = false;
            outcomes!.BeforeTurn = () =>
            {
                savedBeforeOutcome = File.Exists(store.AutosavePath);
                throw new InvalidOperationException("outcome failure");
            };
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("outcome failure"));
            game.Bridge!.EndTurn();
            Assert.IsFalse(savedBeforeOutcome, "GameRoot subscribes the outcome before autosave");
            Assert.IsTrue(File.Exists(store.AutosavePath), "the turn was saved");
            Assert.IsNotEmpty(File.ReadAllText(store.AutosavePath));
            Assert.IsNull(outcomes.Showing);
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    /// <summary>The build gate reads the outcome clips from the mapping (Build.GatedClips, used by RefuseUnlessShareable):
    /// the sensitive Horev clip refuses a shareable build until the owner's review. The owner reviewed it on 14 October 2026
    /// (relayed by the coordinator), and the altalena outcome on 20 October 2026 (#1102), so the shipped mapping passes and
    /// the same rows unreviewed refuse.</summary>
    [Test]
    public void TheSensitiveHorevClipWaitsForTheOwnersReview()
    {
        var build = Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor")!;
        var gated = (IReadOnlyList<EventVideoMapping.Row>)build.GetMethod("GatedClips", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object[] { Yaml })!;
        var gate = build.GetMethod("GateRefusal", BindingFlags.Public | BindingFlags.Static)!;
        Assert.IsTrue(gated.Single(r => r.File == "operation_horev-outcome.mp4").SensitivityReviewed, "the owner's review of 2026-10-14");
        Assert.IsTrue(gated.Single(r => r.File == "altalena-outcome.mp4").SensitivityReviewed, "the owner's review of 2026-10-20 (#1102)");
        // Isolate the clip gate from the other content gates (the starting forces' rows); never change the shipped content or shared C.
        StringAssert.DoesNotContain("illustration clips", (string?)gate.Invoke(null,
            new object?[] { new[] { "Unity", "-shareable" }, C, null, false, null, gated, BuildGateTests.ReviewedSettlementControlStrings() }) ?? "");
        var reviewedContent = BuildGateTests.Checked();
        foreach (var e in reviewedContent.Events)
            if (ReleaseGate.SensitiveEvents(reviewedContent).Contains(e.Id)) e.Status = ReleaseGate.ReviewedStatus;
        Assert.IsNull(gate.Invoke(null, new object?[] { new[] { "Unity", "-shareable" }, reviewedContent, null, false, null, gated, BuildGateTests.ReviewedSettlementControlStrings() }), "reviewed: the clips may ship");
        var unreviewed = gated.Select(r => { r.SensitivityReviewed = false; return r; }).ToList();
        var refusal = (string?)gate.Invoke(null, new object?[] { new[] { "Unity", "-shareable" }, reviewedContent, null, false, null, unreviewed, BuildGateTests.ReviewedSettlementControlStrings() });
        StringAssert.Contains("illustration clips: altalena, degania_defence, operation_horev, operation_yoav, reparations_vote, ten_days", refusal ?? "");
    }
}
