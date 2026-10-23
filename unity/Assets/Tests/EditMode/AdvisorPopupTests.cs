#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Spec 4.2 and 7.1: one message at a time in the side popup, with the buttons of its kind; hidden at once under
/// every modal layer and back when it closes if its signal still holds; no focus taken; never by itself without a
/// settings file.</summary>
public class AdvisorPopupTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    internal static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static readonly DateTime Day = new DateTime(1948, 5, 15);

    internal const string Fixture = "advisors:\n  - { id: ben_gurion, person: ben_gurion }\n  - { id: meyerson, person: golda_meir }\n  - { id: dayan, person: dayan }\n" +
        "messages:\n" +
        "  - { id: t.teach, advisor: meyerson, signal: new_game, level: new, actions: [{ open_screen: budget }] }\n" +
        "  - { id: t.warn, advisor: dayan, signal: turn_ended, level: warnings, actions: [{ open_screen: defence }] }\n" +
        "  - { id: t.crit, advisor: ben_gurion, signal: turn_ended, level: critical }\n";

    /// <summary>The real English strings plus the fixture's three texts.</summary>
    internal static UiText Text() => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text +
        "\nadvisor.t.teach.text: \"Teach.\"\nadvisor.t.warn.text: \"Warn.\"\nadvisor.t.crit.text: \"Critical.\"\n");

    internal sealed class Rig : IDisposable
    {
        public readonly UiTestKit Ui = new UiTestKit();
        public readonly SimBridge Bridge;
        public readonly ScreenNav Nav;
        public readonly EventCardPresenter Card;
        public readonly AdvisorSession Session;
        public readonly AdvisorPresenter Advisor;
        readonly List<IDisposable> owned = new List<IDisposable>();

        public Rig(bool autoShow = true, string catalog = Fixture, UiText? text = null, AdvisorHooks? hooks = null)
        {
            text ??= Text();
            Ui.Root.Q("root").RemoveFromClassList("loading");
            Bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
            owned.Add(new HudPresenter(Ui.Root, Bridge, text));
            Nav = Own(new ScreenNav(Ui.Root));
            Card = Own(new EventCardPresenter(Ui.Root, Bridge, text, null));
            Card.Collapse(takeFocus: false);                            // a new game opens on a card: out of the way
            Session = new AdvisorSession(AdvisorCatalog.Parse(catalog), new AdvisorSettings(null), autoShow);
            hooks ??= new AdvisorHooks();
            hooks.Card = Card;
            Advisor = Own(new AdvisorPresenter(Ui.Root, Bridge, text, Session, hooks: hooks));
        }

        T Own<T>(T d) where T : IDisposable { owned.Add(d); return d; }
        public VisualElement HudRoot => Ui.Root.Q("root");
        /// <summary>End turn, then the new turn's card (if one opens) put behind "Decision waiting", as the opening one is.</summary>
        public void EndTurn()
        {
            Bridge.EndTurn();
            if (Card.Shown != null && !Card.Collapsed) Card.Collapse(takeFocus: false);
        }
        public void Dispose() { for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose(); Ui.Dispose(); }
    }

    static bool Displayed(Button b) => b.style.display.value != DisplayStyle.None;

    [Test]
    public void ATeachingMessageShowsItsAdvisorTextAndButtons()
    {
        using var rig = new Rig();
        rig.Advisor.NewGame();
        Assert.IsTrue(rig.Advisor.IsOpen);
        Assert.AreEqual("t.teach", rig.Advisor.Shown!.Id);
        var text = Text();
        Assert.AreEqual(text["advisor.meyerson.name_first"], rig.Ui.Q<Label>("advisor-name").text, "her first appearance");
        Assert.AreEqual(text["advisor.meyerson.role"], rig.Ui.Q<Label>("advisor-role").text);
        Assert.AreEqual("Teach.", rig.Ui.Q<Label>("advisor-text").text);
        Assert.AreEqual(text["advisor.show_me"], rig.Ui.Q<Button>("advisor-show-me").text);
        Assert.IsTrue(Displayed(rig.Ui.Q<Button>("advisor-show-me")), "it has an action");
        Assert.IsFalse(Displayed(rig.Ui.Q<Button>("advisor-not-now")), "teaching: Tell me more, not Not now");
        Assert.IsFalse(Displayed(rig.Ui.Q<Button>("advisor-more")), "no page to open yet (S3)");
        Assert.IsFalse(rig.HudRoot.ClassListContains(AdvisorPresenter.CriticalClass));
    }

    [Test]
    public void AWarningHasNotNowAndACriticalOneHasNotAndShowsFirst()
    {
        using var rig = new Rig();
        rig.Advisor.NewGame();
        rig.Ui.Press("advisor-got-it");
        rig.EndTurn();
        Assert.AreEqual("t.crit", rig.Advisor.Shown!.Id, "critical first");
        Assert.IsTrue(rig.Ui.Q<VisualElement>("advisor-popup").ClassListContains(AdvisorPresenter.CriticalClass));
        Assert.IsFalse(Displayed(rig.Ui.Q<Button>("advisor-not-now")));
        rig.Ui.Press("advisor-close");                                  // the X is Got it
        Assert.AreEqual("t.warn", rig.Advisor.Shown!.Id);
        Assert.IsTrue(Displayed(rig.Ui.Q<Button>("advisor-not-now")));
        Assert.AreEqual(Text().Format("advisor.count", 2, 2), rig.Ui.Q<Label>("advisor-count").text);
        rig.Ui.Press("advisor-not-now");
        Assert.IsFalse(rig.Advisor.IsOpen);
        CollectionAssert.AreEqual(new[] { AdvisorQueue.Outcome.Shown, AdvisorQueue.Outcome.Shown, AdvisorQueue.Outcome.PutOff },
            rig.Session.Queue.History.Select(h => h.Outcome).ToArray());
    }

    static readonly string[] Layers =
    {
        StartScreenPresenter.OpenClass, EventCardPresenter.OpenClass, CaptureDialogPresenter.OpenClass,
        TurnReportPresenter.OpenClass, OutcomePresenter.OpenClass, EndScreenPresenter.OpenClass, "two-row-screen",
    };

    /// <summary>Spec 4.2: each modal layer hides it at once, through ScreenNav.DisableCovered, which every one of them calls;
    /// when the layer closes it comes back, as its signal still holds this turn.</summary>
    [Test]
    public void HidesUnderEveryModalLayerAndComesBack([ValueSource(nameof(Layers))] string layer)
    {
        using var rig = new Rig();
        rig.Advisor.NewGame();
        Assert.IsTrue(rig.Advisor.IsOpen, "precondition");
        void Set(bool on)
        {
            if (layer == "two-row-screen")
            {
                rig.HudRoot.EnableInClassList(HudPresenter.TwoRowClass, on);
                rig.HudRoot.EnableInClassList(ScreenNav.ScreenOpenClass, on);
            }
            else rig.HudRoot.EnableInClassList(layer, on);
            ScreenNav.DisableCovered(rig.Ui.Root);
        }
        Set(true);
        Assert.IsFalse(rig.Advisor.IsOpen, layer + ": hidden");
        Assert.IsFalse(rig.HudRoot.ClassListContains(AdvisorPresenter.OpenClass));
        Set(false);
        Assert.IsTrue(rig.Advisor.IsOpen, layer + ": back, its signal holds this turn");
        Assert.AreEqual("t.teach", rig.Advisor.Shown!.Id);
    }

    /// <summary>Spec 4.2: a message whose signal no longer holds when the layer closes goes to the history unseen.</summary>
    [Test]
    public void AMessageWhoseSignalEndedUnderALayerGoesToTheHistory()
    {
        using var rig = new Rig(catalog: Fixture.Replace("level: warnings", "level: critical"));
        rig.Advisor.NewGame();
        rig.HudRoot.AddToClassList(TurnReportPresenter.OpenClass); ScreenNav.DisableCovered(rig.Ui.Root);
        rig.EndTurn();                                                   // new_game held on turn 1 only
        rig.HudRoot.RemoveFromClassList(TurnReportPresenter.OpenClass); ScreenNav.DisableCovered(rig.Ui.Root);
        Assert.AreEqual(AdvisorQueue.Outcome.Missed, rig.Session.Queue.History.First().Outcome);
        Assert.AreEqual("t.teach", rig.Session.Queue.History.First().Id);
        Assert.IsFalse(rig.Session.Settings.Seen("t.teach"));
        Assert.IsTrue(rig.Advisor.Shown!.Critical, "the turn's own (critical) message shows");
    }

    [Test]
    public void TakesNoFocusAndEscapeClosesItOnlyFromInside()
    {
        using var rig = new Rig();
        var endTurn = rig.Ui.Q<Button>("end-turn");
        endTurn.Focus();
        rig.Advisor.NewGame();
        Assert.AreSame(endTurn, rig.Ui.Root.focusController.focusedElement, "spec 16: not modal, steals no focus");
        Assert.IsFalse(rig.Advisor.Escape(), "Escape with the focus outside: not its key");
        Assert.IsTrue(rig.Advisor.IsOpen);
        rig.Ui.Q<Button>("advisor-got-it").Focus();
        Assert.IsTrue(rig.Advisor.Escape());
        Assert.IsFalse(rig.Advisor.IsOpen);
        Assert.IsTrue(rig.Session.Settings.Seen("t.teach"), "Escape is the X");
    }

    [Test]
    public void NeverShowsByItselfWithoutASettingsFile()
    {
        using var rig = new Rig(autoShow: false);
        rig.Advisor.NewGame();
        rig.EndTurn();
        Assert.IsFalse(rig.Advisor.IsOpen);
        Assert.IsEmpty(rig.Session.Queue.History);
    }

    [Test]
    public void NoPortraitHidesTheFrame()
    {
        using var rig = new Rig();                                        // no PortraitCatalog: none for anyone
        rig.Advisor.NewGame();
        Assert.IsTrue(rig.Ui.Q<VisualElement>("advisor-popup").ClassListContains(AdvisorPresenter.NoPortraitClass));
    }

    [Test]
    public void ChangesNeitherTheSimNorTheQueue()
    {
        using var rig = new Rig();
        var hash = rig.Bridge.State.Hash(); var queued = rig.Bridge.Queued.Count;
        rig.Advisor.NewGame(); rig.Ui.Press("advisor-show-me"); rig.Nav.Close(); rig.Ui.Press("advisor-got-it");
        Assert.AreEqual(hash, rig.Bridge.State.Hash());
        Assert.AreEqual(queued, rig.Bridge.Queued.Count, "it issues no command");
    }
}
