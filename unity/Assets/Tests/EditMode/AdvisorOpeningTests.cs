#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Opening-chain core lifecycle (spec 9.2-9.3). Scene and layout parity with the guide is in AdvisorOpeningParityTests.</summary>
public class AdvisorOpeningTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    readonly List<string> dirs = new List<string>();
    SettingsStore Store() { var d = Path.Combine(Path.GetTempPath(), "statecraft-opening-" + Guid.NewGuid().ToString("N")); dirs.Add(d); return new SettingsStore(d); }
    [TearDown] public void Clean() { foreach (var d in dirs) if (Directory.Exists(d)) Directory.Delete(d, true); dirs.Clear(); }

    // Approval is simulated ONLY in the test's in-memory copy. Shipped Hebrew stays pending.
    static AdvisorCatalog Pending() => AdvisorCatalog.Parse(File.ReadAllText(AdvisorCatalog.Path).Replace("he_review: approved", "he_review: pending"));
    static AdvisorCatalog Approved() => AdvisorCatalog.Parse(File.ReadAllText(AdvisorCatalog.Path).Replace("he_review: pending", "he_review: approved"));

    sealed class Rig : IDisposable
    {
        public readonly UiTestKit Ui = new UiTestKit();
        public readonly SimBridge Bridge;
        public readonly ScreenNav Nav;
        public readonly EventCardPresenter Card;
        public readonly AdvisorSession Session;
        public readonly AdvisorPresenter Advisor;
        readonly HudPresenter hud;
        public Rig(UiText text, SettingsStore? store, bool approved = false, AdvisorSession? session = null, bool tellMeMore = false)
        {
            Ui.Root.Q("root").RemoveFromClassList("loading");
            Ui.Root.Q("root").EnableInClassList(UiLanguage.HebrewClass, text["ui.lang"] == UiLanguage.Hebrew);
            Bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
            hud = new HudPresenter(Ui.Root, Bridge, text); Nav = new ScreenNav(Ui.Root);
            Card = new EventCardPresenter(Ui.Root, Bridge, text, null);
            Session = session ?? new AdvisorSession(approved ? Approved() : Pending(), new AdvisorSettings(store), true);
            Advisor = new AdvisorPresenter(Ui.Root, Bridge, text, Session, hooks: new AdvisorHooks { Card = Card, CloseScreens = () => Nav.Close(),
                OpenPage = tellMeMore ? p => Encyclopedia?.Open(p) : null });   // as GameRoot wires Tell me more
            Encyclopedia = new EncyclopediaPresenter(Ui.Root, text, Session.Catalog, Nav, Advisor);
        }
        public readonly EncyclopediaPresenter Encyclopedia;
        public void Dispose() { Encyclopedia.Dispose(); Advisor.Dispose(); Card.Dispose(); Nav.Dispose(); hud.Dispose(); Ui.Dispose(); }
    }

    [Test]
    public void HasSevenStepsInOrder()
    {
        CollectionAssert.AreEqual(new[] { "opening.greeting", "opening.attention", "opening.city", "opening.training", "opening.move", "opening.event", "opening.objectives" }, AdvisorPresenter.ChainIds(AdvisorCatalog.Load()));
        Assert.AreEqual(5, AdvisorPresenter.EventStep);
    }

    /// <summary>Tell me more on an opening step (#2095 review): the encyclopedia opens on the step's page over the chain, and
    /// when it closes the chain is still on that step, showing it (RenderChain hides the popup only while a screen covers it).</summary>
    [Test]
    public void TellMeMoreOnAnOpeningStepKeepsTheChainOnItsStep()
    {
        using var rig = new Rig(En, Store(), tellMeMore: true); rig.Advisor.NewGame();
        rig.Advisor.NextStep(); rig.Advisor.NextStep();
        Assert.AreEqual(2, rig.Session.Chain.Step); Assert.AreEqual("opening.city", rig.Advisor.Shown?.Id);
        Assert.AreEqual(DisplayStyle.Flex, rig.Ui.Q<Button>("advisor-more").style.display.value, "the step offers Tell me more");
        rig.Ui.Press("advisor-more");
        Assert.AreEqual("encyclopedia", rig.Nav.Current); Assert.AreEqual("cities", rig.Encyclopedia.ShowingPage);
        Assert.AreEqual(2, rig.Session.Chain.Step, "reading the page does not move the chain");
        rig.Ui.Press("nav-encyclopedia");
        Assert.IsNull(rig.Nav.Current);
        Assert.IsTrue(rig.Advisor.InChain, "the chain is still open");
        Assert.AreEqual(2, rig.Session.Chain.Step, "on the same step");
        Assert.AreEqual("opening.city", rig.Advisor.Shown?.Id);
        Assert.IsTrue(rig.Advisor.IsOpen, "and the step shows again");
    }

    [Test]
    public void ThePendingChainDoesNotOpenInHebrewAndIsNotMarkedSeen()
    {
        using var rig = new Rig(He, Store()); rig.Advisor.NewGame();
        Assert.IsFalse(rig.Advisor.InChain); Assert.IsFalse(rig.Session.Settings.OpeningSeen);
    }

    /// <summary>While every opening line is pending in Hebrew, the encyclopedia has no button that would open nothing, and
    /// asking for the chain leaves the screen the player is on open.</summary>
    [Test]
    public void ThePendingChainHasNoReopenButtonInHebrew()
    {
        using (var en = new Rig(En, Store()))
            Assert.AreEqual(DisplayStyle.Flex, en.Ui.Q<Button>("encyclopedia-opening").style.display.value, "English: the button shows");
        using var rig = new Rig(He, Store());
        Assert.AreEqual(DisplayStyle.None, rig.Ui.Q<Button>("encyclopedia-opening").style.display.value, "Hebrew, all pending: no button");
        rig.Ui.Press("nav-encyclopedia");
        rig.Advisor.OpenChain();
        Assert.IsFalse(rig.Advisor.InChain);
        Assert.AreEqual("encyclopedia", rig.Nav.Current, "the encyclopedia stays open");
    }

    [Test]
    public void ItShowsAutomaticallyOnlyOnceAndNeverWithoutAStore()
    {
        using var rig = new Rig(En, Store()); rig.Advisor.NewGame();
        Assert.IsTrue(rig.Advisor.InChain); Assert.IsTrue(rig.Session.Settings.OpeningSeen);
        rig.Advisor.CloseChain(); rig.Advisor.NewGame(); Assert.IsFalse(rig.Advisor.InChain);
        using var automated = new Rig(En, null); automated.Advisor.NewGame(); Assert.IsFalse(automated.Advisor.InChain);
    }

    /// <summary>The plan's assumption (c): the opening chain is the first game's onboarding, as the guide was, so it ignores
    /// the help level (review of the S2 head).</summary>
    [TestCase("off")] [TestCase("experienced")] [TestCase("warnings")]
    public void AnyHelpLevelGetsTheOpeningChainOnItsFirstGame(string level)
    {
        var store = Store(); store.Write(AdvisorSettings.LevelKey, level);
        using var rig = new Rig(En, store); rig.Advisor.NewGame();
        Assert.IsTrue(rig.Advisor.InChain, "the first new game shows the chain at " + level);
        Assert.IsTrue(rig.Session.Settings.OpeningSeen);
    }

    [Test]
    public void TheOldGuidesSeenMarkPreventsAnAutomaticChain()
    {
        var store = Store(); store.Write(AdvisorSettings.GuideSeenKey, "seen");
        using var rig = new Rig(En, store); rig.Advisor.NewGame(); Assert.IsFalse(rig.Advisor.InChain);
        rig.Advisor.OpenChain(); Assert.IsTrue(rig.Advisor.InChain, "manual reopen remains available");
    }

    [TestCase(false)] [TestCase(true)]
    public void EveryStepCanBeSkippedAndTheChainReopened(bool hebrew)
    {
        using var rig = new Rig(hebrew ? He : En, Store(), approved: true);
        for (int step = 0; step < 7; step++)
        {
            rig.Advisor.OpenChain(); for (int i = 0; i < step; i++) rig.Advisor.NextStep();
            Assert.AreEqual(step, rig.Advisor.ChainStep); Assert.IsTrue(rig.Card.Collapsed);
            rig.Ui.Press("advisor-close"); Assert.IsFalse(rig.Advisor.InChain);
            Assert.IsFalse(rig.Card.Collapsed, "restore the opening card");
        }
    }

    [Test]
    public void DuplicateNewGameNotificationsDoNotResetTheOpenChain()
    {
        using var rig = new Rig(En, Store()); rig.Advisor.NewGame(); rig.Advisor.NextStep(); rig.Advisor.NextStep();
        rig.Advisor.NewGame(); Assert.AreEqual(2, rig.Advisor.ChainStep, "the same session, not another new game");
        rig.Bridge.Replace(GameSession.New(C, GoldenRun.Seed, Mode.Historical)); rig.Advisor.NewGame();
        Assert.IsFalse(rig.Advisor.InChain, "a genuinely new session resets the old chain; its profile has seen it");
    }

    [Test]
    public void TheEncyclopediasButtonOpensTheChainAgain()
    {
        using var rig = new Rig(En, Store()); rig.Ui.Press("nav-encyclopedia"); rig.Ui.Press("encyclopedia-opening");
        Assert.IsTrue(rig.Advisor.InChain); Assert.AreEqual(0, rig.Advisor.ChainStep); Assert.IsNull(rig.Nav.Current);
        Assert.AreEqual("Show the opening advice again", rig.Ui.Q<Button>("encyclopedia-opening").text);
    }

    [Test]
    public void WithNoCardTheEventStepIsLeftOut()
    {
        using var rig = new Rig(En, Store());
        UiTestKit.Press(rig.Ui.Root.Q("event-options").Query<Button>().First());   // One Army
        UiTestKit.Press(rig.Ui.Root.Q("event-options").Query<Button>().First());   // then Degania (R-EVT-006)
        Assert.IsNull(rig.Card.Shown); rig.Advisor.OpenChain(); var seen = new List<string>();
        while (rig.Advisor.InChain) { seen.Add(rig.Advisor.Shown!.Id); Assert.LessOrEqual(seen.Count, 6); rig.Advisor.NextStep(); }
        CollectionAssert.AreEqual(new[] { "opening.greeting", "opening.attention", "opening.city", "opening.training", "opening.move", "opening.objectives" }, seen);
    }

    [Test]
    public void TheEventStepSpeaksBeforeTheCardAndStepsAside()
    {
        using var rig = new Rig(En, Store()); rig.Advisor.OpenChain();
        for (int i = 0; i < AdvisorPresenter.EventStep; i++) rig.Advisor.NextStep();
        Assert.IsTrue(rig.Card.Collapsed); Assert.AreEqual(5, rig.Advisor.ChainStep);
        Assert.AreEqual(AdvisorTargets.DecisionWaiting, rig.Advisor.Shown!.Actions.First(a => a.ShowMe != "").ShowMe, "(1) the ring's target: Decision waiting");
        Assert.IsTrue(rig.Ui.Root.Q("root").ClassListContains(AdvisorPresenter.RingClass), "(1) the ring shows");
        rig.Ui.Press("advisor-show-me"); Assert.IsFalse(rig.Card.Collapsed); Assert.IsFalse(rig.Advisor.IsOpen);
        rig.Card.Collapse(); Assert.IsTrue(rig.Advisor.IsOpen); Assert.AreEqual(5, rig.Advisor.ChainStep);
        rig.Ui.Press("advisor-show-me"); UiTestKit.Press(rig.Ui.Root.Q("event-options").Query<Button>().First());
        Assert.AreEqual("degania_defence", rig.Card.Shown?.Id, "One Army answered, the turn's second card opens (R-EVT-006)");
        Assert.IsFalse(rig.Advisor.IsOpen); Assert.AreEqual(5, rig.Session.Chain.Step, "the advisor waits aside on its event step");
        UiTestKit.Press(rig.Ui.Root.Q("event-options").Query<Button>().First());
        Assert.IsNull(rig.Card.Shown); Assert.AreEqual("opening.objectives", rig.Advisor.Shown!.Id); Assert.IsTrue(rig.Advisor.IsOpen);
    }

    [Test]
    public void ARebuiltPresenterKeepsTheStepAndHidesPendingHebrew()
    {
        var store = Store(); AdvisorSession session;
        using (var en = new Rig(En, store)) { en.Advisor.NewGame(); en.Advisor.NextStep(); en.Advisor.NextStep(); session = en.Session; Assert.AreEqual(2, session.Chain.Step); }
        using (var he = new Rig(He, store, session: session)) { Assert.IsFalse(he.Advisor.IsOpen); Assert.AreEqual(2, session.Chain.Step); }
        using var back = new Rig(En, store, session: session); Assert.IsTrue(back.Advisor.InChain); Assert.AreEqual(2, back.Advisor.ChainStep);
    }

    [Test]
    public void ClosingAndWalkingTheChainChangeNeitherTheSimNorItsSave()
    {
        using var rig = new Rig(En, Store()); var state = rig.Bridge.State.Hash(); var save = rig.Bridge.Session.Save.Write();
        rig.Advisor.OpenChain(); for (int i = 0; i < 7; i++) rig.Advisor.NextStep();
        Assert.IsFalse(rig.Advisor.InChain); Assert.AreEqual(state, rig.Bridge.State.Hash()); Assert.AreEqual(save, rig.Bridge.Session.Save.Write()); Assert.IsEmpty(rig.Bridge.Queued);
    }
}


// R-GOV-002 / #1688: isolated test sessions; no shipped selector is changed.
internal static class AdvisorTour1688Fixture
{
    internal static readonly string[] Visible = { "opening.greeting", "opening.city", "opening.training", "opening.move", "opening.event", "opening.objectives" };
    internal static UiText Text(bool he) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he ? UiPaths.StringsHebrew : UiPaths.Strings).text);
    internal static Content Content(string ruleset = SaveGame.Gdd03Ruleset)
    {
        // Borrow existing topology/rules but replace ALL historical cards before creating any session.
        // None of the gdd03-events exclusion manifest becomes a synthetic 0.3 card.
        var c = ContentLoader.Load(Paths.Content);
        foreach (var era in c.Eras)
        {
            era.Systems.Loop = ruleset == SaveGame.LegacyRuleset ? EraSystems.Legacy : EraSystems.Gdd;
            era.Systems.Ruleset = ruleset;
        }
        c.Events.Clear(); c.Objectives.Clear();
        foreach (var id in new[] { "tour_test_a", "tour_test_b" })
            c.Events.Add(new GameEvent { Id = id, From = c.Rules.Start, Title = "Synthetic decision", Text = "Test only",
                Options = new List<EventOption> { new EventOption { Id = "yes", Text = "Continue" } } });
        return c;
    }
    internal sealed class Rig : IDisposable
    {
        internal readonly UiTestKit Ui = new UiTestKit();
        internal readonly SimBridge Bridge;
        internal readonly EventCardPresenter Card;
        internal readonly AdvisorSession Session;
        internal readonly AdvisorHooks Hooks;
        internal AdvisorPresenter Advisor;
        readonly HudPresenter hud;
        internal string Selection = "player";
        internal Rig(bool he = false, SettingsStore? store = null, string ruleset = SaveGame.Gdd03Ruleset, GameSession? game = null, AdvisorCatalog? catalog = null)
        {
            Ui.Root.Q("root").RemoveFromClassList("loading");
            var text = Text(he);
            Bridge = new SimBridge(game ?? GameSession.New(Content(ruleset), 1, Mode.Historical), text);
            hud = new HudPresenter(Ui.Root, Bridge, text);
            Card = new EventCardPresenter(Ui.Root, Bridge, text, null);
            Session = new AdvisorSession(catalog ?? AdvisorCatalog.Parse(File.ReadAllText(AdvisorCatalog.Path).Replace("he_review: pending", "he_review: approved")), new AdvisorSettings(store), true);
            Hooks = new AdvisorHooks { Card = Card, ShowCity = () => Selection = "city", ShowUnit = () => Selection = "unit",
                KeepSelection = () => { var previous = Selection; return () => Selection = previous; } };
            Advisor = new AdvisorPresenter(Ui.Root, Bridge, text, Session, hooks: Hooks);
        }
        internal void Rebuild(bool he)
        {
            Advisor.Dispose();
            Advisor = new AdvisorPresenter(Ui.Root, Bridge, Text(he), Session, hooks: Hooks);
        }
        public void Dispose() { Advisor.Dispose(); Card.Dispose(); hud.Dispose(); Ui.Dispose(); }
    }
}

public class AdvisorOpeningNoAttentionTests
{
    readonly List<string> dirs = new List<string>();
    SettingsStore Store()
    {
        var path = Path.Combine(Path.GetTempPath(), "statecraft-tour1688-" + Guid.NewGuid().ToString("N"));
        dirs.Add(path); return new SettingsStore(path);
    }
    [TearDown] public void Clean() { foreach (var d in dirs) if (Directory.Exists(d)) Directory.Delete(d, true); dirs.Clear(); }

    // R-GOV-002: traverse requires:[opening.attention] without recording it as watched.
    [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
    public void AutoStartAndManualReplaySkipAttentionWithoutChangingItsPreference(bool he, bool alreadySeen)
    {
        var store = Store(); if (alreadySeen) store.Write(AdvisorSettings.SeenKey, "opening.attention");
        using var rig = new AdvisorTour1688Fixture.Rig(he, store);
        rig.Advisor.NewGame(); Assert.IsTrue(rig.Advisor.InChain);
        var state = rig.Bridge.State.Hash(); var save = rig.Bridge.Session.Save.Write();
        for (int replay = 0; replay < 2; replay++)
        {
            if (replay > 0) rig.Advisor.OpenChain();
            var visited = new List<string>();
            while (rig.Advisor.InChain)
            {
                visited.Add(rig.Advisor.Shown!.Id); Assert.LessOrEqual(visited.Count, 6, "requires must not dead-end or loop");
                Assert.AreEqual(AdvisorTour1688Fixture.Text(he).Format("advisor.step", visited.Count, 6), rig.Ui.Q<Label>("advisor-count").text);
                rig.Advisor.NextStep();
            }
            CollectionAssert.AreEqual(AdvisorTour1688Fixture.Visible, visited);
            Assert.AreEqual(alreadySeen, rig.Session.Settings.Seen("opening.attention"));
            Assert.IsFalse(rig.Session.Queue.History.Any(e => e.Id == "opening.attention"));
        }
        Assert.AreEqual(alreadySeen, new AdvisorSettings(store).Seen("opening.attention"));
        Assert.AreEqual(state, rig.Bridge.State.Hash()); Assert.AreEqual(save, rig.Bridge.Session.Save.Write());
        Assert.IsEmpty(rig.Bridge.Queued);
        rig.Advisor.NewGame(); Assert.IsFalse(rig.Advisor.InChain, "profile already opened the tour");
    }

    // R-GOV-002: closing each visible step restores card/selection; replay starts at greeting.
    [TestCase(false)] [TestCase(true)]
    public void EveryVisibleStepCanCloseAndReopen(bool he)
    {
        using var rig = new AdvisorTour1688Fixture.Rig(he);
        foreach (var id in AdvisorTour1688Fixture.Visible)
        {
            rig.Advisor.OpenChain();
            for (int n = 0; rig.Advisor.Shown?.Id != id && n < 6; n++) rig.Advisor.NextStep();
            Assert.AreEqual(id, rig.Advisor.Shown?.Id);
            rig.Ui.Press("advisor-close"); Assert.IsFalse(rig.Advisor.InChain);
            Assert.AreEqual("player", rig.Selection); Assert.IsFalse(rig.Card.Collapsed);
        }
        Assert.IsFalse(rig.Session.Settings.Seen("opening.attention"));
    }

    // R-GOV-002: new controls; every original old-mode test above is untouched.
    [TestCase(SaveGame.LegacyRuleset)] [TestCase(SaveGame.GddRuleset)]
    public void OlderSessionsStillTraverseAndRememberAttention(string ruleset)
    {
        using var rig = new AdvisorTour1688Fixture.Rig(ruleset: ruleset);
        rig.Advisor.OpenChain(); var ids = new List<string>();
        while (rig.Advisor.InChain) { ids.Add(rig.Advisor.Shown!.Id); Assert.LessOrEqual(ids.Count, 7); rig.Advisor.NextStep(); }
        CollectionAssert.AreEqual(AdvisorPresenter.ChainIds(rig.Session.Catalog), ids);
        Assert.IsTrue(rig.Session.Settings.Seen("opening.attention"));
        Assert.IsTrue(rig.Session.Queue.History.Any(e => e.Id == "opening.attention"));
    }
}


public class AdvisorOpeningNoAttentionPreferenceTests
{
    // R-GOV-002: an already-opened profile still has manual replay, with no Attention seen mark.
    [Test]
    public void AlreadyOpenedProfileReplaysManuallyWithoutAttention()
    {
        var path = Path.Combine(Path.GetTempPath(), "statecraft-tour1688-seen-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SettingsStore(path); store.Write(AdvisorSettings.OpeningKey, "seen");
            using var rig = new AdvisorTour1688Fixture.Rig(store: store);
            rig.Advisor.NewGame(); Assert.IsFalse(rig.Advisor.InChain);
            rig.Advisor.OpenChain(); rig.Advisor.NextStep(); Assert.AreEqual("opening.city", rig.Advisor.Shown?.Id);
            Assert.IsFalse(new AdvisorSettings(store).Seen("opening.attention"));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }
}
