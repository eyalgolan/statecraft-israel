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

/// <summary>Opening-chain core lifecycle (spec9.2–9.3). Scene/layout parity with the guide is a separate group below.</summary>
public class AdvisorOpeningTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    readonly List<string> dirs = new List<string>();
    SettingsStore Store() { var d = Path.Combine(Path.GetTempPath(), "statecraft-opening-" + Guid.NewGuid().ToString("N")); dirs.Add(d); return new SettingsStore(d); }
    [TearDown] public void Clean() { foreach (var d in dirs) if (Directory.Exists(d)) Directory.Delete(d, true); dirs.Clear(); }

    // Approval is simulated ONLY in the test's in-memory copy. Shipped Hebrew stays pending.
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
        public Rig(UiText text, SettingsStore? store, bool approved = false, AdvisorSession? session = null)
        {
            Ui.Root.Q("root").RemoveFromClassList("loading");
            Ui.Root.Q("root").EnableInClassList(UiLanguage.HebrewClass, text["ui.lang"] == UiLanguage.Hebrew);
            Bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
            hud = new HudPresenter(Ui.Root, Bridge, text); Nav = new ScreenNav(Ui.Root);
            Card = new EventCardPresenter(Ui.Root, Bridge, text, null);
            Session = session ?? new AdvisorSession(approved ? Approved() : AdvisorCatalog.Load(), new AdvisorSettings(store), true);
            Advisor = new AdvisorPresenter(Ui.Root, Bridge, text, Session, hooks: new AdvisorHooks { Card = Card });
        }
        public void Dispose() { Advisor.Dispose(); Card.Dispose(); Nav.Dispose(); hud.Dispose(); Ui.Dispose(); }
    }

    [Test]
    public void HasSevenStepsInOrder()
    {
        CollectionAssert.AreEqual(new[] { "opening.greeting", "opening.attention", "opening.city", "opening.training", "opening.move", "opening.event", "opening.objectives" }, AdvisorPresenter.ChainIds(AdvisorCatalog.Load()));
        Assert.AreEqual(5, AdvisorPresenter.EventStep);
    }

    [Test]
    public void ThePendingChainDoesNotOpenInHebrewAndIsNotMarkedSeen()
    {
        using var rig = new Rig(He, Store()); rig.Advisor.NewGame();
        Assert.IsFalse(rig.Advisor.InChain); Assert.IsFalse(rig.Session.Settings.OpeningSeen);
    }

    [Test]
    public void ItShowsAutomaticallyOnlyOnceAndNeverWithoutAStore()
    {
        using var rig = new Rig(En, Store()); rig.Advisor.NewGame();
        Assert.IsTrue(rig.Advisor.InChain); Assert.IsTrue(rig.Session.Settings.OpeningSeen);
        rig.Advisor.CloseChain(); rig.Advisor.NewGame(); Assert.IsFalse(rig.Advisor.InChain);
        using var automated = new Rig(En, null); automated.Advisor.NewGame(); Assert.IsFalse(automated.Advisor.InChain);
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
    public void WithNoCardTheEventStepIsLeftOut()
    {
        using var rig = new Rig(En, Store()); UiTestKit.Press(rig.Ui.Root.Q("event-options").Query<Button>().First());
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
        rig.Ui.Press("advisor-show-me"); Assert.IsFalse(rig.Card.Collapsed); Assert.IsFalse(rig.Advisor.IsOpen);
        rig.Card.Collapse(); Assert.IsTrue(rig.Advisor.IsOpen); Assert.AreEqual(5, rig.Advisor.ChainStep);
        rig.Ui.Press("advisor-show-me"); UiTestKit.Press(rig.Ui.Root.Q("event-options").Query<Button>().First());
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
