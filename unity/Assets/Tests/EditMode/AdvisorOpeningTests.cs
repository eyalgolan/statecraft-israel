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
    public void HasEightStepsInOrder()
    {
        // #3142: the invasion card on New game took opening.invasion's and opening.units' place.
        CollectionAssert.AreEqual(new[] { "opening.greeting", "opening.cities", "opening.attention", "opening.move_how", "opening.move", "opening.orders", "opening.event", "opening.end_turn" }, AdvisorPresenter.ChainIds(AdvisorCatalog.Load()));
        Assert.AreEqual(6, AdvisorPresenter.EventStep);
    }

    /// <summary>Tell me more on an opening step (#2095 review): the encyclopedia opens on the step's page over the chain, and
    /// when it closes the chain is still on that step, showing it (RenderChain hides the popup only while a screen covers it).</summary>
    [Test]
    public void TellMeMoreOnAnOpeningStepKeepsTheChainOnItsStep()
    {
        using var rig = new Rig(En, Store(), tellMeMore: true); rig.Advisor.NewGame();
        rig.Advisor.NextStep();
        Assert.AreEqual(1, rig.Session.Chain.Step); Assert.AreEqual("opening.cities", rig.Advisor.Shown?.Id);
        Assert.AreEqual(DisplayStyle.Flex, rig.Ui.Q<Button>("advisor-more").style.display.value, "the step offers Tell me more");
        rig.Ui.Press("advisor-more");
        Assert.AreEqual("encyclopedia", rig.Nav.Current); Assert.AreEqual("cities", rig.Encyclopedia.ShowingPage);
        Assert.AreEqual(1, rig.Session.Chain.Step, "reading the page does not move the chain");
        rig.Ui.Press("nav-encyclopedia");
        Assert.IsNull(rig.Nav.Current);
        Assert.IsTrue(rig.Advisor.InChain, "the chain is still open");
        Assert.AreEqual(1, rig.Session.Chain.Step, "on the same step");
        Assert.AreEqual("opening.cities", rig.Advisor.Shown?.Id);
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

    /// <summary>Owner ruling on #2942, replacing the plan's assumption (c): "Off silences it". At help off, experienced or
    /// warnings the chain does not open by itself and counts as seen, so it never appears later, even at the new level; the
    /// course does not open either. A chain reopened by hand from the encyclopedia still shows.</summary>
    [TestCase("off")] [TestCase("experienced")] [TestCase("warnings")]
    public void AHelpLevelAboveNewSilencesTheOpeningChainForGood(string level)
    {
        var store = Store(); store.Write(AdvisorSettings.LevelKey, level);
        using (var rig = new Rig(En, store))
        {
            rig.Advisor.NewGame();
            Assert.IsFalse(rig.Advisor.InChain, "no chain by itself at " + level);
            Assert.IsTrue(rig.Session.Settings.OpeningSeen, "it counts as seen");
            Assert.AreEqual("seen", store.Read(AdvisorSettings.OpeningKey));
            Assert.IsFalse(rig.Session.Settings.CourseOpen, "and opens no course");
            Assert.IsNull(store.Read(AdvisorSettings.CourseKey));
            rig.Session.Settings.SetLevel("new");
            rig.Advisor.NewGame();
            Assert.IsFalse(rig.Advisor.InChain, "nor on a later game at the new level");
            rig.Advisor.OpenChain();
            Assert.IsTrue(rig.Advisor.InChain, "reopened by hand it shows");
            Assert.IsFalse(rig.Session.Settings.CourseOpen);
        }
        using var later = new Rig(En, store); later.Advisor.NewGame();
        Assert.IsFalse(later.Advisor.InChain, "nor after a restart");
        Assert.IsFalse(later.Session.Settings.CourseOpen); Assert.IsNull(store.Read(AdvisorSettings.CourseKey));
    }

    /// <summary>At the new level, stored or the default, the first game still gets the chain and the course.</summary>
    [TestCase("new")] [TestCase("")]
    public void TheNewHelpLevelGetsTheOpeningChainOnItsFirstGame(string level)
    {
        var store = Store(); if (level != "") store.Write(AdvisorSettings.LevelKey, level);   // "": no line, the default
        using var rig = new Rig(En, store); rig.Advisor.NewGame();
        Assert.IsTrue(rig.Advisor.InChain, "the first new game shows the chain at " + (level == "" ? "the default" : level));
        Assert.IsTrue(rig.Session.Settings.OpeningSeen); Assert.IsTrue(rig.Session.Settings.CourseOpen);
    }

    /// <summary>#2933: the course opens with the chain's automatic opening, so only on a first game.</summary>
    [Test]
    public void TheAutomaticChainOpensTheCourse()
    {
        var store = Store();
        using var rig = new Rig(En, store); rig.Advisor.NewGame();
        Assert.IsTrue(rig.Advisor.InChain); Assert.IsTrue(rig.Session.Settings.CourseOpen);
        Assert.AreEqual("open", store.Read(AdvisorSettings.CourseKey));
    }

    [Test]
    public void ReturningPlayersAndAReopenedChainOpenNoCourse()
    {
        var store = Store(); store.Write(AdvisorSettings.OpeningKey, "seen");
        using var rig = new Rig(En, store); rig.Advisor.NewGame();
        Assert.IsFalse(rig.Advisor.InChain);
        rig.Advisor.OpenChain();
        Assert.IsTrue(rig.Advisor.InChain, "reopened by hand");
        Assert.IsFalse(rig.Session.Settings.CourseOpen); Assert.IsNull(store.Read(AdvisorSettings.CourseKey));
    }

    [Test]
    public void NoAutomaticChainNoCourse()
    {
        using var automated = new Rig(En, null); automated.Advisor.NewGame();
        Assert.IsFalse(automated.Session.Settings.CourseOpen, "an automated run");
        using var pending = new Rig(He, Store()); pending.Advisor.NewGame();
        Assert.IsFalse(pending.Session.Settings.CourseOpen, "Hebrew with every step pending shows no chain");
    }

    /// <summary>#2933, the plan's quit and X case: after X on the first step, the fallback step shows as a queue row, comes back
    /// from under a layer (its own signal, opening, never holds), and on a Continue it is offered from the settings line
    /// alone, with no chain. Since #3142 that step is opening.end_turn, the chain's one fallback row.</summary>
    [Test]
    public void AFallbackStepSurvivesXALayerAndAContinue()
    {
        var catalog = AdvisorCatalog.Load();
        var store = Store();
        using (var rig = new Rig(En, store, session: new AdvisorSession(catalog, new AdvisorSettings(store), true)))
        {
            rig.Advisor.NewGame();
            Assert.AreEqual("opening.greeting", rig.Advisor.Shown?.Id);
            rig.Advisor.CloseChain();
            rig.Card.Collapse(takeFocus: false);
            Assert.AreEqual("opening.end_turn", rig.Advisor.Shown?.Id, "X on the first step");
            Assert.IsTrue(rig.Advisor.IsOpen);
            rig.Card.Reopen();
            Assert.IsFalse(rig.Advisor.IsOpen, "under the card");
            rig.Card.Collapse(takeFocus: false);
            Assert.AreEqual("opening.end_turn", rig.Advisor.Shown?.Id, "back from under the card");
            Assert.IsTrue(rig.Advisor.IsOpen);
            Assert.IsFalse(rig.Session.Queue.History.Any(e => e.Outcome == AdvisorQueue.Outcome.Missed));
        }
        using var continued = new Rig(En, store, session: new AdvisorSession(catalog, new AdvisorSettings(store), true));
        continued.Card.Collapse(takeFocus: false);
        continued.Advisor.Resume();
        Assert.IsFalse(continued.Advisor.InChain);
        Assert.AreEqual("opening.end_turn", continued.Advisor.Shown?.Id, "a Continue, not seen yet");
    }

    /// <summary>#2933: a player who quits on step 2 finds the fallback step on a Continue (since #3142 the one, End turn's),
    /// with its Show me, as a queue row.</summary>
    [Test]
    public void QuittingOnStepTwoLeavesTheFallbackStepForAContinue()
    {
        var store = Store();
        using (var rig = new Rig(En, store, session: new AdvisorSession(AdvisorCatalog.Load(), new AdvisorSettings(store), true)))
        {
            rig.Advisor.NewGame(); rig.Advisor.NextStep();
            Assert.AreEqual("opening.cities", rig.Advisor.Shown?.Id);
        }
        using var continued = new Rig(En, store, session: new AdvisorSession(AdvisorCatalog.Load(), new AdvisorSettings(store), true));
        continued.Card.Collapse(takeFocus: false);
        continued.Advisor.Resume();
        Assert.IsFalse(continued.Advisor.InChain);
        Assert.AreEqual("opening.end_turn", continued.Advisor.Shown?.Id);
        Assert.AreEqual(DisplayStyle.Flex, continued.Ui.Q<Button>("advisor-show-me").style.display.value);
        continued.Ui.Press("advisor-show-me");
        Assert.IsTrue(continued.Ui.Root.Q("root").ClassListContains(AdvisorPresenter.RingClass), "a queue row's Show me rings End turn");
    }

    /// <summary>#2933: X on step 2. The greeting took the first turn's teaching line, so the fallback step (since #3142 the
    /// one, End turn's) comes back on turn 2.</summary>
    [Test]
    public void XOnStepTwoBringsItBackNextTurn()
    {
        var store = Store();
        using var rig = new Rig(En, store, session: new AdvisorSession(AdvisorCatalog.Load(), new AdvisorSettings(store), true));
        rig.Advisor.NewGame(); rig.Advisor.NextStep();
        rig.Ui.Press("advisor-close");
        rig.Card.Collapse(takeFocus: false);
        Assert.IsNull(rig.Advisor.Shown, "one teaching line a turn: the greeting was it");
        rig.Bridge.State.Turn = 1; rig.Advisor.Notice(AdvisorSignals.TurnEnded);
        Assert.AreEqual("opening.end_turn", rig.Advisor.Shown?.Id);
    }

    /// <summary>#2933: End turn pressed on a step that would come back as a queue row: the player has read it (above all step
    /// 10, whose own lesson is to press the button), so it counts as seen, on the turn it was shown. Since #3142 End turn's is
    /// the one fallback step.</summary>
    [Test]
    public void EndTurnOnTheFallbackStepCountsAsSeen()
    {
        const string id = "opening.end_turn";
        var store = Store();
        using var rig = new Rig(En, store, session: new AdvisorSession(AdvisorCatalog.Load(), new AdvisorSettings(store), true));
        rig.Advisor.NewGame();
        for (int i = 0; rig.Advisor.Shown?.Id != id && i < 10; i++) rig.Advisor.NextStep();
        Assert.AreEqual(id, rig.Advisor.Shown?.Id, "precondition");
        foreach (var unit in rig.Bridge.NeedsOrders.ToList()) rig.Bridge.Skip(unit);
        rig.Bridge.EndTurn();
        Assert.AreEqual(1, rig.Bridge.State.Turn, "precondition: the turn ran");
        Assert.IsFalse(rig.Advisor.InChain);
        Assert.IsTrue(rig.Session.Settings.Seen(id));
        Assert.AreEqual(0, rig.Session.Queue.History.Single(e => e.Id == id).Turn, "the turn it was shown");
    }

    /// <summary>#2933: a language switch is the one way to stand on a step that is pending in Hebrew. The chain goes on to the
    /// next approved step instead of hiding until the turn ends.</summary>
    [Test]
    public void ASwitchToHebrewOnAPendingStepGoesOnToTheNextApprovedStep()
    {
        var store = Store();
        var catalog = AdvisorCatalog.Parse(File.ReadAllText(AdvisorCatalog.Path).Replace("{ id: opening.cities, advisor: meyerson, signal: opening, level: new, he_review: approved", "{ id: opening.cities, advisor: meyerson, signal: opening, level: new, he_review: pending"));
        var ids = AdvisorPresenter.ChainIds(catalog).ToList();
        AdvisorSession session;
        using (var en = new Rig(En, store, session: new AdvisorSession(catalog, new AdvisorSettings(store), true)))
        {
            en.Advisor.NewGame(); en.Advisor.NextStep(); session = en.Session;
            Assert.AreEqual("opening.cities", en.Advisor.Shown?.Id, "precondition");
        }
        Assert.IsFalse(catalog.Messages.Single(m => m.Id == "opening.cities").ApprovedIn(true), "precondition: pending");
        var next = ids.Skip(2).First(id => catalog.Messages.Single(m => m.Id == id).ApprovedIn(true));
        using var he = new Rig(He, store, session: session);
        Assert.IsTrue(he.Advisor.InChain); Assert.IsTrue(he.Advisor.IsOpen, "not hidden");
        Assert.AreEqual(next, he.Advisor.Shown?.Id);
        Assert.AreEqual(ids.IndexOf(next), he.Advisor.ChainStep);
        Assert.IsTrue(he.Card.Collapsed);
    }

    /// <summary>#2933: until the owner approves a new step's Hebrew, a Hebrew player walks only the approved ones, counted as
    /// such, and the pending fallback steps never show; they lapse as missed, and leave no Hebrew anywhere.</summary>
    [Test]
    public void HebrewWalksOnlyApprovedStepsAndPendingFallbacksLapse()
    {
        var store = Store(); var catalog = AdvisorCatalog.Load();
        var approved = AdvisorPresenter.ChainIds(catalog).Where(id => catalog.Messages.Single(m => m.Id == id).ApprovedIn(true)).ToList();
        using var rig = new Rig(He, store, session: new AdvisorSession(catalog, new AdvisorSettings(store), true));
        rig.Advisor.NewGame();
        Assert.IsTrue(rig.Advisor.InChain); Assert.IsTrue(rig.Session.Settings.CourseOpen);
        var seen = new List<string>();
        while (rig.Advisor.InChain)
        {
            seen.Add(rig.Advisor.Shown!.Id); Assert.LessOrEqual(seen.Count, approved.Count);
            Assert.AreEqual(He.Format("advisor.step", seen.Count, approved.Count), rig.Ui.Q<Label>("advisor-count").text);
            rig.Advisor.NextStep();
        }
        CollectionAssert.AreEqual(approved, seen);
        rig.Card.Collapse(takeFocus: false);
        foreach (var id in new[] { "opening.end_turn" })   // the one fallback step since #3142
        {
            if (catalog.Messages.Single(m => m.Id == id).ApprovedIn(true)) continue;   // the owner approved it: nothing to lapse
            for (int turn = 1; turn <= 3; turn++) { rig.Bridge.State.Turn = turn; rig.Advisor.Notice(AdvisorSignals.TurnEnded); Assert.AreNotEqual(id, rig.Advisor.Shown?.Id); }
            Assert.IsTrue(rig.Session.Queue.History.Any(e => e.Id == id && e.Outcome == AdvisorQueue.Outcome.Missed), id);
            Assert.IsFalse(rig.Session.Settings.Seen(id));
        }
    }

    [TestCase("opening.objectives", 3, 6)] [TestCase("opening.city", 4, 7)] [TestCase("opening.training", 5, 8)]
    public void AMovedRowLeftTheChainWithItsApprovedHebrew(string id, int floor, int last)
    {
        var catalog = AdvisorCatalog.Load(); var row = catalog.Messages.Single(m => m.Id == id);
        CollectionAssert.DoesNotContain(AdvisorPresenter.ChainIds(catalog), id);
        Assert.AreEqual(AdvisorSignals.CourseOpen, row.Signal); Assert.IsEmpty(row.Requires);
        Assert.AreEqual(floor, row.When["turn_at_least"]); Assert.AreEqual(last, row.LapseAfter);
        Assert.AreEqual(AdvisorCatalog.Approved, row.HeReview);
    }

    /// <summary>#2933 section 3: the later lessons, each a teaching row from its turn floor to its last turn, outside the chain,
    /// in the issue's words, under the word cap, and its Hebrew approved by the owner.</summary>
    [TestCase("lesson.report", "course_open", 2, 4, 3, "That report shows what moved and what was fought. If the truce was accepted, your brigades cannot attack until it ends.")]
    [TestCase("lesson.attack", "course_open", 6, 8, 3, "The truce period is over. Select a brigade next to an enemy to see Attack and its odds. An attack can force the enemy back.")]
    [TestCase("lesson.stakes", "course_open", 7, 10, 1, "Your score counts objectives, how early you reach them, and prisoners returned. Battles wear brigades down. If Money and Food stay exhausted, the game ends.")]
    [TestCase("lesson.combat", "combat_seen", 2, 8, 2, "When fighting breaks out, the report shows losses and who fell back. Select a brigade next to an enemy to see Attack and its odds.")]
    public void ALaterLessonIsATeachingRowFromItsFloorToItsLastTurn(string id, string signal, int floor, int last, int priority, string english)
    {
        var catalog = AdvisorCatalog.Load(); var row = catalog.Messages.Single(m => m.Id == id);
        CollectionAssert.DoesNotContain(AdvisorPresenter.ChainIds(catalog), id);
        Assert.AreEqual(signal, row.Signal); Assert.AreEqual("", row.Fallback); Assert.IsEmpty(row.Requires);
        Assert.AreEqual("new", row.Level); Assert.IsTrue(row.Teaching); Assert.AreEqual(AdvisorCatalog.Once, row.Repeat);
        CollectionAssert.AreEquivalent(new[] { "turn_at_least" }, row.When.Keys);
        Assert.AreEqual(floor, row.When["turn_at_least"]); Assert.AreEqual(last, row.LapseAfter); Assert.AreEqual(priority, row.Priority);
        Assert.AreEqual(AdvisorCatalog.Approved, row.HeReview, "the owner approved the four, 9 Nov 2026 (\"Approve all four (Recommended)\")");
        Assert.IsTrue(row.ApprovedIn(true), "so it shows in Hebrew too");
        Assert.AreEqual(english, En["advisor." + id + ".text"]);
        Assert.LessOrEqual(english.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length, AdvisorCatalog.MaxWords);
        Assert.AreNotEqual("advisor." + id + ".text", He["advisor." + id + ".text"], "its Hebrew is there");
    }

    /// <summary>#2933: "Priority 3 rows go first, fallback rows included". Without it, lesson.report (priority 3) would take
    /// turn 2 from a fallback step that X put off, and opening.end_turn would lapse unseen on turn 3. Since #3142 it is the one
    /// fallback row.</summary>
    [Test]
    public void TheFallbackStepGoesFirstAmongTheCourseRows()
    {
        var catalog = AdvisorCatalog.Load();
        CollectionAssert.AreEqual(new[] { "opening.end_turn" }, catalog.Messages.Where(m => m.Fallback != "").Select(m => m.Id));
        var row = catalog.Messages.Single(m => m.Id == "opening.end_turn");
        Assert.AreEqual(AdvisorSignals.CourseOpen, row.Fallback); Assert.AreEqual(3, row.Priority);
    }

    /// <summary>#2933: X on step 2, then a turn at a time: the fallback step comes back first (since #3142 the one, End turn's),
    /// then each course row on its own turn, a bumped row waiting while it can, and nothing is missed.</summary>
    [Test]
    public void XOnStepTwoThenTheWholeCourseOneRowATurn()
    {
        var store = Store();
        using var rig = new Rig(En, store, session: new AdvisorSession(AdvisorCatalog.Load(), new AdvisorSettings(store), true));
        rig.Advisor.NewGame(); rig.Advisor.NextStep();
        rig.Ui.Press("advisor-close");
        rig.Card.Collapse(takeFocus: false);
        Assert.IsNull(rig.Advisor.Shown, "one teaching line a turn: the greeting was it");
        var expected = new[] { "opening.end_turn", "lesson.report", "opening.objectives", "opening.city", "lesson.attack", "opening.training", "lesson.stakes", null };
        for (int turn = 2; turn < 2 + expected.Length; turn++)
        {
            rig.Bridge.State.Turn = turn - 1; rig.Advisor.Notice(AdvisorSignals.TurnEnded);
            Assert.AreEqual(expected[turn - 2], rig.Advisor.Shown?.Id, "game turn " + turn);
            if (rig.Advisor.Shown != null) rig.Ui.Press("advisor-got-it");
        }
        Assert.IsFalse(rig.Session.Queue.History.Any(e => e.Outcome == AdvisorQueue.Outcome.Missed));
    }

    /// <summary>#2933: lesson.combat waits for the first fight's report, and goes ahead of the priority 1 rows the turn after
    /// lesson.report; a first fight after its last turn (8) brings nothing. The queue alone, as the presenter drives it.</summary>
    [TestCase(1, new[] { "lesson.report", "lesson.combat", "opening.objectives", "opening.city", "lesson.attack", "opening.training", "lesson.stakes", null, null })]
    [TestCase(8, new[] { "lesson.report", "opening.objectives", "opening.city", "opening.training", "lesson.attack", "lesson.stakes", null, null, null })]
    public void TheCombatLessonFollowsTheFirstFight(int firstFight, string?[] fromTurnTwo)
    {
        var settings = new AdvisorSettings(null);
        settings.MarkSeen("opening.end_turn");   // the chain walked
        var queue = new AdvisorQueue(AdvisorCatalog.Load(), settings);
        var shown = new List<string?>();
        for (int turn = 1; turn <= 9; turn++)   // State.Turn; game turn 2 to 10
        {
            var t = turn;
            var holding = t >= firstFight ? new[] { AdvisorSignals.CourseOpen, "combat_seen" } : new[] { AdvisorSignals.CourseOpen };
            bool When(IReadOnlyDictionary<string, double> w) { CollectionAssert.IsSubsetOf(w.Keys, new[] { "turn_at_least" }); return !w.TryGetValue("turn_at_least", out var v) || t + 1 >= v; }
            queue.Offer(holding, When, t); queue.Prune(holding, When, t, DateTime.MinValue);
            var head = queue.Head(t, false); shown.Add(head?.Id);
            if (head != null) queue.Done(head, t, DateTime.MinValue);
        }
        CollectionAssert.AreEqual(fromTurnTwo, shown);
        Assert.AreEqual(firstFight <= 7, settings.Seen("lesson.combat"));
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
        for (int step = 0; step < AdvisorPresenter.ChainIds(AdvisorCatalog.Load()).Count; step++)
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

    /// <summary>#3028 (spec 16: the popup "gives it back with FocusReturn"): closing the tour gives the focus back to the
    /// control that opened it. From the Encyclopedia that is its rail button: the screen's own button is in a screen that
    /// closed, and ScreenNav.Close has already handed the focus to the rail button before the tour opens.</summary>
    [TestCase(false)] [TestCase(true)]
    public void ClosingTheTourOpenedFromTheEncyclopediaGivesTheFocusBackToItsButton(bool hebrew)
    {
        using var rig = new Rig(hebrew ? He : En, Store(), approved: true);
        rig.Card.Collapse(takeFocus: false);                         // a new game opens on a card: out of the way
        Focusable? Focused() => rig.Ui.Root.focusController.focusedElement;
        var rail = rig.Ui.Q<Button>("nav-encyclopedia");
        rail.Focus(); UiTestKit.Press(rail);                         // Tab to the Encyclopedia and Enter
        rig.Ui.Q<Button>("encyclopedia-opening").Focus(); rig.Ui.Press("encyclopedia-opening");
        Assert.IsTrue(rig.Advisor.InChain, "precondition: the tour is open");
        Assert.IsNull(rig.Nav.Current, "precondition: the Encyclopedia closed");
        Assert.AreSame(rig.Ui.Q<Button>("advisor-got-it"), Focused(), "precondition: Next has the focus");
        Assert.IsTrue(rig.Advisor.Escape());
        Assert.IsFalse(rig.Advisor.InChain);
        Assert.AreSame(rail, Focused(), "back to the Encyclopedia's button, not End turn");
    }

    /// <summary>#3028: a tour opened with the focus on any control gives it back there when it closes, by Escape or by the
    /// X alike, and each tour remembers its own opener.</summary>
    [TestCase(false)] [TestCase(true)]
    public void ClosingATourOpenedByHandGivesTheFocusBackToTheOpener(bool hebrew)
    {
        using var rig = new Rig(hebrew ? He : En, Store(), approved: true);
        rig.Card.Collapse(takeFocus: false);
        Focusable? Focused() => rig.Ui.Root.focusController.focusedElement;
        var next = rig.Ui.Q<Button>("advisor-got-it");
        var cabinet = rig.Ui.Q<Button>("nav-cabinet"); var budget = rig.Ui.Q<Button>("nav-budget");
        cabinet.Focus(); rig.Advisor.OpenChain();
        Assert.IsTrue(rig.Advisor.InChain, "precondition: the tour is open");
        Assert.AreSame(next, Focused(), "precondition: Next has the focus");
        Assert.IsTrue(rig.Advisor.Escape());
        Assert.AreSame(cabinet, Focused(), "Escape: back to the opener, not End turn");
        budget.Focus(); rig.Advisor.OpenChain();
        Assert.AreSame(next, Focused(), "precondition: Next has the focus again");
        rig.Ui.Press("advisor-close");                               // Press moves no focus: it is still on Next
        Assert.IsFalse(rig.Advisor.InChain);
        Assert.AreSame(budget, Focused(), "the X: back to this tour's opener");
    }

    /// <summary>#3028 guard: an opener that cannot take the focus when the tour closes (disabled, or gone from the panel)
    /// leaves it on End turn, never nowhere.</summary>
    [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
    public void AnOpenerThatCannotTakeTheFocusFallsBackToEndTurn(bool hebrew, bool detached)
    {
        using var rig = new Rig(hebrew ? He : En, Store(), approved: true);
        rig.Card.Collapse(takeFocus: false);
        Focusable? Focused() => rig.Ui.Root.focusController.focusedElement;
        var opener = rig.Ui.Q<Button>("nav-cabinet");
        if (detached) { opener = new Button { name = "test-opener" }; rig.Ui.Root.Q("root").Add(opener); }
        opener.Focus();
        Assert.AreSame(opener, Focused(), "precondition: the opener has the focus");
        rig.Advisor.OpenChain();
        Assert.AreSame(rig.Ui.Q<Button>("advisor-got-it"), Focused(), "precondition: Next has the focus");
        if (detached) opener.RemoveFromHierarchy(); else opener.SetEnabled(false);
        Assert.IsTrue(rig.Advisor.Escape());
        Assert.AreSame(rig.Ui.Q<Button>("end-turn"), Focused(), detached ? "the opener left the panel" : "the opener is disabled");
    }

    [Test]
    public void WithNoCardTheEventStepIsLeftOut()
    {
        using var rig = new Rig(En, Store());
        UiTestKit.Press(rig.Ui.Root.Q("event-options").Query<Button>().First());   // One Army
        UiTestKit.Press(rig.Ui.Root.Q("event-options").Query<Button>().First());   // then Degania (R-EVT-006)
        Assert.IsNull(rig.Card.Shown); rig.Advisor.OpenChain(); var seen = new List<string>();
        while (rig.Advisor.InChain) { seen.Add(rig.Advisor.Shown!.Id); Assert.LessOrEqual(seen.Count, 7); rig.Advisor.NextStep(); }
        CollectionAssert.AreEqual(new[] { "opening.greeting", "opening.cities", "opening.attention", "opening.move_how", "opening.move", "opening.orders", "opening.end_turn" }, seen);
    }

    [Test]
    public void TheEventStepSpeaksBeforeTheCardAndStepsAside()
    {
        using var rig = new Rig(En, Store()); rig.Advisor.OpenChain();
        for (int i = 0; i < AdvisorPresenter.EventStep; i++) rig.Advisor.NextStep();
        Assert.IsTrue(rig.Card.Collapsed); Assert.AreEqual(AdvisorPresenter.EventStep, rig.Advisor.ChainStep);
        Assert.AreEqual(AdvisorTargets.DecisionWaiting, rig.Advisor.Shown!.Actions.First(a => a.ShowMe != "").ShowMe, "(1) the ring's target: Decision waiting");
        Assert.IsTrue(rig.Ui.Root.Q("root").ClassListContains(AdvisorPresenter.RingClass), "(1) the ring shows");
        rig.Ui.Press("advisor-show-me"); Assert.IsFalse(rig.Card.Collapsed); Assert.IsFalse(rig.Advisor.IsOpen);
        rig.Card.Collapse(); Assert.IsTrue(rig.Advisor.IsOpen); Assert.AreEqual(AdvisorPresenter.EventStep, rig.Advisor.ChainStep);
        rig.Ui.Press("advisor-show-me"); UiTestKit.Press(rig.Ui.Root.Q("event-options").Query<Button>().First());
        Assert.AreEqual("degania_defence", rig.Card.Shown?.Id, "One Army answered, the turn's second card opens (R-EVT-006)");
        Assert.IsFalse(rig.Advisor.IsOpen); Assert.AreEqual(AdvisorPresenter.EventStep, rig.Session.Chain.Step, "the advisor waits aside on its event step");
        UiTestKit.Press(rig.Ui.Root.Q("event-options").Query<Button>().First());
        Assert.IsNull(rig.Card.Shown); Assert.AreEqual("opening.end_turn", rig.Advisor.Shown!.Id); Assert.IsTrue(rig.Advisor.IsOpen);
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
        rig.Advisor.OpenChain(); for (int i = 0; i < 10; i++) rig.Advisor.NextStep();
        Assert.IsFalse(rig.Advisor.InChain); Assert.AreEqual(state, rig.Bridge.State.Hash()); Assert.AreEqual(save, rig.Bridge.Session.Save.Write()); Assert.IsEmpty(rig.Bridge.Queued);
    }
}


// R-GOV-002 / #1688: isolated test sessions; no shipped selector is changed.
internal static class AdvisorTour1688Fixture
{
    internal static readonly string[] Visible = { "opening.greeting", "opening.cities", "opening.move_how", "opening.move", "opening.orders", "opening.event", "opening.end_turn" };
    /// <summary>From the greeting to the step after cities: Attention, or under gdd-0.3 the step past it.</summary>
    internal const int PastCities = 2;
    /// <summary>No shipped step shows a city since #2933, while Show me still does: the tests of that rule give opening.cities one, in memory.</summary>
    internal static AdvisorCatalog WithACityStep()
    {
        const string Cities = "actions: [{ encyclopedia: cities }]";
        var yaml = File.ReadAllText(AdvisorCatalog.Path).Replace("he_review: pending", "he_review: approved");
        StringAssert.Contains(Cities, yaml);
        return AdvisorCatalog.Parse(yaml.Replace(Cities, "actions: [{ show_me: city_housing }, { encyclopedia: cities }]"));
    }
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
                visited.Add(rig.Advisor.Shown!.Id); Assert.LessOrEqual(visited.Count, AdvisorTour1688Fixture.Visible.Length, "requires must not dead-end or loop");
                Assert.AreEqual(AdvisorTour1688Fixture.Text(he).Format("advisor.step", visited.Count, AdvisorTour1688Fixture.Visible.Length), rig.Ui.Q<Label>("advisor-count").text);
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
            for (int n = 0; rig.Advisor.Shown?.Id != id && n < AdvisorTour1688Fixture.Visible.Length; n++) rig.Advisor.NextStep();
            Assert.AreEqual(id, rig.Advisor.Shown?.Id);
            rig.Ui.Press("advisor-close"); Assert.IsFalse(rig.Advisor.InChain);
            Assert.AreEqual("player", rig.Selection); Assert.IsFalse(rig.Card.Collapsed);
        }
        Assert.IsFalse(rig.Session.Settings.Seen("opening.attention"));
    }

    /// <summary>#2933: the Show me names of the unit steps show a unit as a click would, and closing puts the selection back.</summary>
    [TestCase("opening.move_how")] [TestCase("opening.orders")]
    public void AUnitStepShowsItsUnitAndPutsTheSelectionBack(string id)
    {
        using var rig = new AdvisorTour1688Fixture.Rig();
        rig.Advisor.OpenChain();
        for (int n = 0; rig.Advisor.Shown?.Id != id && n < AdvisorTour1688Fixture.Visible.Length; n++) rig.Advisor.NextStep();
        Assert.AreEqual(id, rig.Advisor.Shown?.Id);
        Assert.AreEqual("unit", rig.Selection);
        rig.Ui.Press("advisor-close");
        Assert.AreEqual("player", rig.Selection);
    }

    /// <summary>#2933: the course rows reach a gdd-0.3 player though opening.attention is never marked there, one a turn from
    /// their floors; the walked chain's fallback steps do not come back. Every course row's Hebrew is approved (the later
    /// lessons' on 9 Nov 2026, "Approve all four (Recommended)"), so a Hebrew player gets the same course, nothing missed.</summary>
    [TestCase(false)] [TestCase(true)]
    public void CourseRowsShowFromTheirFloorsAfterTheChain(bool he)
    {
        var store = Store();
        using var rig = new AdvisorTour1688Fixture.Rig(he, store, catalog: AdvisorCatalog.Load());
        rig.Advisor.NewGame(); Assert.IsTrue(rig.Advisor.InChain);
        for (int n = 0; rig.Advisor.InChain && n < 10; n++) rig.Advisor.NextStep();
        Assert.IsFalse(rig.Advisor.InChain);
        rig.Card.Collapse(takeFocus: false);
        var expected = new[] { "lesson.report", "opening.objectives", "opening.city", "opening.training", "lesson.attack", "lesson.stakes", null, null, null, null };
        for (int turn = 2; turn < 2 + expected.Length; turn++)   // to game turn 11, past lesson.stakes' last (10)
        {
            rig.Bridge.State.Turn = turn - 1; rig.Advisor.Notice(AdvisorSignals.TurnEnded);
            Assert.AreEqual(expected[turn - 2], rig.Advisor.Shown?.Id, "game turn " + turn);
            if (rig.Advisor.Shown != null) rig.Ui.Press("advisor-got-it");
        }
        foreach (var id in new[] { "lesson.report", "lesson.attack", "lesson.stakes" }) Assert.IsTrue(rig.Session.Settings.Seen(id), id);
        Assert.IsFalse(rig.Session.Queue.History.Any(e => e.Outcome == AdvisorQueue.Outcome.Missed), "nothing missed");
        Assert.IsFalse(rig.Session.Queue.History.Any(e => e.Id == "lesson.combat"), "no fight, no combat lesson");
        Assert.IsFalse(rig.Session.Settings.Seen("opening.attention"));
    }

    /// <summary>#2933: a course row not reached by its last turn is gone, the others keep their own days; a priority 3 lesson
    /// goes ahead of a moved row on its last turn.</summary>
    [Test]
    public void AMovedRowLapsesAfterItsLastTurn()
    {
        var store = Store();
        using var rig = new AdvisorTour1688Fixture.Rig(false, store, catalog: AdvisorCatalog.Load());
        rig.Advisor.NewGame();
        for (int n = 0; rig.Advisor.InChain && n < 10; n++) rig.Advisor.NextStep();
        rig.Card.Collapse(takeFocus: false);
        rig.Bridge.State.Turn = 6; rig.Advisor.Notice(AdvisorSignals.TurnEnded);   // game turn 7: objectives' last was 6, lesson.report's 4
        Assert.AreEqual("lesson.attack", rig.Advisor.Shown?.Id, "priority 3, ahead of opening.city on its last turn");
        rig.Ui.Press("advisor-got-it");
        rig.Bridge.State.Turn = 7; rig.Advisor.Notice(AdvisorSignals.TurnEnded);
        Assert.AreEqual("opening.training", rig.Advisor.Shown?.Id, "its last turn");
        rig.Ui.Press("advisor-got-it");
        rig.Bridge.State.Turn = 8; rig.Advisor.Notice(AdvisorSignals.TurnEnded);
        Assert.AreEqual("lesson.stakes", rig.Advisor.Shown?.Id);
        rig.Ui.Press("advisor-got-it");
        rig.Bridge.State.Turn = 9; rig.Advisor.Notice(AdvisorSignals.TurnEnded);
        Assert.IsNull(rig.Advisor.Shown);
        Assert.IsFalse(rig.Session.Settings.Seen("opening.objectives")); Assert.IsFalse(rig.Session.Settings.Seen("lesson.report"));
        Assert.IsFalse(rig.Session.Settings.Seen("opening.city"));
        Assert.IsTrue(rig.Session.Queue.History.Any(e => e.Id == "opening.city" && e.Outcome == AdvisorQueue.Outcome.Missed), "lapsed, recorded as missed");
    }

    // R-GOV-002: new controls; every original old-mode test above is untouched.
    [TestCase(SaveGame.LegacyRuleset)] [TestCase(SaveGame.GddRuleset)]
    public void OlderSessionsStillTraverseAndRememberAttention(string ruleset)
    {
        using var rig = new AdvisorTour1688Fixture.Rig(ruleset: ruleset);
        rig.Advisor.OpenChain(); var ids = new List<string>();
        while (rig.Advisor.InChain) { ids.Add(rig.Advisor.Shown!.Id); Assert.LessOrEqual(ids.Count, 10); rig.Advisor.NextStep(); }
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
            rig.Advisor.OpenChain(); for (int i = 0; i < AdvisorTour1688Fixture.PastCities; i++) rig.Advisor.NextStep();
            Assert.AreEqual("opening.move_how", rig.Advisor.Shown?.Id);
            Assert.IsFalse(new AdvisorSettings(store).Seen("opening.attention"));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }
}
