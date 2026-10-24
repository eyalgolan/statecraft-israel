using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class DefenceScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    static (UiTestKit ui, SimBridge bridge, DefencePresenter defence) Open()
    {
        var ui = new UiTestKit();
        ui.Q<VisualElement>("screen-defence").style.display = DisplayStyle.Flex;   // open, as ScreenNav opens it (a hidden screen does not re-render)
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        new ToastPresenter(ui.Root, bridge, Text);
        return (ui, bridge, new DefencePresenter(ui.Root, bridge, Text));
    }

    /// <summary>The same screen on the legacy loop (the real content without the loop switch): the call-up and the
    /// security order live there now (R-WAR-002), so their tests play a legacy session.</summary>
    static (UiTestKit ui, SimBridge bridge, DefencePresenter defence) OpenLegacy()
    {
        var ui = new UiTestKit();
        ui.Q<VisualElement>("screen-defence").style.display = DisplayStyle.Flex;
        var bridge = new SimBridge(GameSession.New(SimBridgeGddTests.Legacy, GoldenRun.Seed, Mode.Historical), Text);
        new ToastPresenter(ui.Root, bridge, Text);
        return (ui, bridge, new DefencePresenter(ui.Root, bridge, Text));
    }

    /// <summary>In Hebrew the command line and the leaders' cards show the content's Hebrew names and titles (#558).</summary>
    [Test]
    public void InHebrewTheLeadersAreNamedInHebrew()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        using var ui = new UiTestKit();
        ui.Q<VisualElement>("screen-defence").style.display = DisplayStyle.Flex;
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), he);
        new DefencePresenter(ui.Root, bridge, he);
        var minister = Cabinet.Holder(C, bridge.State, "defense_minister")!;
        var chief = Cabinet.Holder(C, bridge.State, "chief_of_staff")!;
        Assert.AreEqual($"שר הביטחון {minister.NameHe} · הרמטכ״ל {chief.NameHe}", ui.Q<Label>("defence-command").text);
        Assert.AreEqual(chief.NameHe, ui.Q<Label>("lead-chief_of_staff-holder").text);
        Assert.AreEqual(C.Offices.Single(o => o.Id == "chief_of_staff").TitleHe, ui.Q<Label>("lead-chief_of_staff-office").text);
        // Ben-Gurion's card lists both his offices, one a line, each in Hebrew.
        Assert.AreEqual(string.Join("\n", C.Offices.Single(o => o.Id == "prime_minister").TitleHe, C.Offices.Single(o => o.Id == "defense_minister").TitleHe),
            ui.Q<Label>("lead-prime_minister-office").text);
    }

    [Test]
    public void TheLadderShowsFullWarWithTheCeasefireClock()
    {
        var (ui, _, _) = Open();
        using (ui)
        {
            Assert.IsTrue(ui.Q<VisualElement>("rung-5").ClassListContains("rung-current"));
            Assert.IsFalse(ui.Q<VisualElement>("rung-2").ClassListContains("rung-current"));
            for (int rung = 1; rung <= 5; rung++)   // the GDD game plays monthly turns on every rung (R-TURN-001)
                Assert.AreEqual("turns of a month", ui.Q<Label>($"rung-{rung}-turn").text, $"rung {rung}");
            Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("ceasefire").style.display.value);   // no clock in the GDD loop (R-WAR-003)
            Assert.AreEqual("Minister of Defense David Ben-Gurion · Chief of Staff Yaakov Dori", ui.Q<Label>("defence-command").text);
            Assert.AreEqual("80% of reserves called up (level 5 needs at least 80%)", ui.Q<Label>("mob-level").text);
            Assert.AreEqual("Costs IL 48.0m a year in pay and lost work", ui.Q<Label>("mob-cost").text);
            Assert.IsFalse(ui.Q<Button>("escalate").enabledSelf);
        }
    }

    [Test]
    public void OrdersAreQueuedAndTheTurnCarriesThemOut()
    {
        var (ui, bridge, defence) = OpenLegacy();
        using (ui)
        {
            ui.Press("de-escalate");
            Assert.AreEqual("Ordered: level 5 to 4", ui.Q<Label>("order-pending").text);
            Assert.AreEqual("80% of reserves called up (level 4 needs at least 40%)", ui.Q<Label>("mob-level").text);
            ui.Press("mob-down");
            Assert.AreEqual(0.75, defence.ProposedMobilization, 1e-9);
            bridge.EndTurn();
            Assert.AreEqual(4, bridge.State.Rung);
            Assert.AreEqual(0.75, bridge.State.Mobilization, 1e-9);
            Assert.IsTrue(ui.Q<VisualElement>("rung-4").ClassListContains("rung-current"));
            Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("ceasefire").style.display.value);
        }
    }

    [Test]
    public void PressingBackToTodaysLevelWithdrawsTheOrder()
    {
        var (ui, bridge, _) = OpenLegacy();
        using (ui)
        {
            ui.Press("de-escalate");
            ui.Press("escalate");
            Assert.AreEqual(0, bridge.Queued.Count);
            Assert.AreEqual("", ui.Q<Label>("order-pending").text);
        }
    }

    [Test]
    public void RaisingTheLevelShowsTheCallUpTheSimWillForce()
    {
        var (ui, bridge, defence) = OpenLegacy();
        using (ui)
        {
            ui.Press("de-escalate");
            for (int i = 0; i < 8; i++) ui.Press("mob-down");                              // 80% down to level 4's floor of 40%
            Assert.AreEqual(0.4, defence.ProposedMobilization, 1e-9);
            ui.Press("mob-down");                                                          // cannot go below the floor
            Assert.AreEqual(0.4, defence.ProposedMobilization, 1e-9);
            bridge.EndTurn();
            Assert.AreEqual(0.4, bridge.State.Mobilization, 1e-9);
            ui.Press("escalate");                                                          // back to full war needs 80% again
            StringAssert.StartsWith("80% of reserves called up (level 5 needs at least 80%)", ui.Q<Label>("mob-level").text);
            Assert.AreEqual("Costs IL 48.0m a year in pay and lost work", ui.Q<Label>("mob-cost").text);
            bridge.EndTurn();
            Assert.AreEqual(0.8, bridge.State.Mobilization, 1e-9);
            Assert.AreEqual(5, bridge.State.Rung);
        }
    }

    [Test]
    public void AnOrderTheAttentionCannotPayForIsRefusedWithTheReason()
    {
        var (ui, bridge, _) = OpenLegacy();
        using (ui)
        {
            Assert.IsNull(bridge.Queue(new SetPolicy(C.Policies[0].Id, 0.5)));
            Assert.IsNull(bridge.Queue(new SetPolicy(C.Policies[1].Id, 0.5)));
            ui.Press("de-escalate");
            Assert.AreEqual("Not done: Not enough attention (need 2.0, have 0.0)", ui.Q<Label>("toast").text);
            Assert.AreEqual("", ui.Q<Label>("order-pending").text);
        }
    }

    /// <summary>R-WAR-002 and R-WAR-003 (#750): the GDD loop refuses the call-up and the security order and has no
    /// ceasefire clock, so the Defence screen hides the four lever controls and the ceasefire line there. The legacy
    /// loop keeps all of them, with the clock's 52 weeks.</summary>
    [Test]
    public void TheGddLoopHidesTheRetiredLevers()
    {
        var levers = new[] { "escalate", "de-escalate", "mob-up", "mob-down" };
        var (ui, _, _) = Open();
        using (ui)
        {
            foreach (var id in levers)
                Assert.AreEqual(DisplayStyle.None, ui.Q<Button>(id).style.display.value, id);
            Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("ceasefire").style.display.value);
        }
        var (legacy, _, _) = OpenLegacy();
        using (legacy)
        {
            foreach (var id in levers)
                Assert.AreEqual(DisplayStyle.Flex, legacy.Q<Button>(id).style.display.value, id);
            Assert.AreEqual(DisplayStyle.Flex, legacy.Q<Label>("ceasefire").style.display.value);
            Assert.AreEqual("If full war goes on, the UN imposes a ceasefire in 52 weeks", legacy.Q<Label>("ceasefire").text);
        }
    }

    /// <summary>#755 (plan PR 16): every rung plays a month in the GDD loop (R-TURN-001), so the per-rung turn-length
    /// legend says nothing there and is hidden. The legacy loop, whose rungs set the turn length, keeps it.</summary>
    [Test]
    public void TheGddLoopDropsTheTurnLengthLegend()
    {
        var (ui, _, _) = Open();
        using (ui)
            for (int rung = 1; rung <= 5; rung++)
                Assert.AreEqual(DisplayStyle.None, ui.Q<Label>($"rung-{rung}-turn").style.display.value, $"rung {rung}");
        var (legacy, _, _) = OpenLegacy();
        using (legacy)
            for (int rung = 1; rung <= 5; rung++)
                Assert.AreEqual(DisplayStyle.Flex, legacy.Q<Label>($"rung-{rung}-turn").style.display.value, $"rung {rung}");
    }

    /// <summary>#755, Q-WAR-1: in the GDD loop the level is the war state the events set (R-WAR-002), and a line says so
    /// with today's level. The legacy loop, where the player orders the level, has no such line.</summary>
    [Test]
    public void TheWarStateLineNamesTheLevelTheEventsSet()
    {
        var (ui, bridge, defence) = Open();
        using (ui)
        {
            Assert.AreEqual("War state: level 5, set by the events of the war", ui.Q<Label>("war-state").text);
            bridge.State.Rung = 3;
            defence.Render();
            Assert.AreEqual("War state: level 3, set by the events of the war", ui.Q<Label>("war-state").text);
            Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("war-state").style.display.value);
        }
        var (legacy, _, _) = OpenLegacy();
        using (legacy) Assert.AreEqual(DisplayStyle.None, legacy.Q<Label>("war-state").style.display.value);
    }

    /// <summary>#218's case (11 June to 8 July 1948) under Q-WAR-2 as decided: the GDD loop has no UN ceasefire clock
    /// (R-WAR-003), so the screen no longer promises a ceasefire "in 49 weeks" during the First Truce; while the truce the
    /// player accepted is in force (R-COMBAT-009, read from the sim) a line says so and until when. It goes once the
    /// window ends, and a refused card shows none.</summary>
    [Test]
    public void TheFirstTruceShowsTheTruceLineAndNoCeasefireClock()
    {
        var (ui, bridge, defence) = Open();
        using (ui)
        {
            var truce = ui.Q<Label>("truce");
            Assert.AreEqual(DisplayStyle.None, truce.style.display.value, "no truce at the start");
            bridge.State.Date = new System.DateTime(1948, 6, 15);
            bridge.State.ChosenOptions["first_truce"] = "accept";
            defence.Render();
            Assert.AreEqual(DisplayStyle.Flex, truce.style.display.value);
            Assert.AreEqual("A truce is in force: our units cannot attack until 8 July 1948", truce.text);
            Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("ceasefire").style.display.value, "no ceasefire clock (R-WAR-003)");
            bridge.State.Date = new System.DateTime(1948, 7, 15);
            defence.Render();
            Assert.AreEqual(DisplayStyle.None, truce.style.display.value, "the window has ended");
            bridge.State.Date = new System.DateTime(1948, 6, 15);
            bridge.State.ChosenOptions["first_truce"] = "refuse";
            defence.Render();
            Assert.AreEqual(DisplayStyle.None, truce.style.display.value, "a refused card binds nothing");
        }
        var (legacy, legacyBridge, legacyDefence) = OpenLegacy();
        using (legacy)
        {
            legacyBridge.State.Date = new System.DateTime(1948, 6, 15);
            legacyBridge.State.ChosenOptions["first_truce"] = "accept";
            legacyDefence.Render();
            Assert.AreEqual(DisplayStyle.None, legacy.Q<Label>("truce").style.display.value, "the legacy loop has no truce line");
        }
    }

    /// <summary>#218's case in Hebrew: the truce line names its end date in the Hebrew calendar words.</summary>
    [Test]
    public void InHebrewTheTruceLineNamesItsEnd()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        using var ui = new UiTestKit();
        ui.Q<VisualElement>("screen-defence").style.display = DisplayStyle.Flex;
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), he);
        bridge.State.Date = new System.DateTime(1948, 6, 15);
        bridge.State.ChosenOptions["first_truce"] = "accept";
        using var defence = new DefencePresenter(ui.Root, bridge, he);
        Assert.AreEqual("הפוגה בתוקף: היחידות שלנו אינן יכולות לתקוף עד 8 ביולי 1948", ui.Q<Label>("truce").text);
    }

    /// <summary>#755, Q-POW-4: the prisoner lines read GameState's counts (R-POW-001, R-POW-004, R-POW-005), summed over
    /// the sides, in people (people_per_unit, 1,000). Plain counts: nothing celebrates a capture (impact plan section 4.3
    /// item 4). The legacy loop takes no prisoners, so the panel is hidden there.</summary>
    [Test]
    public void ThePrisonerLinesReadTheirState()
    {
        var (ui, bridge, defence) = Open();
        using (ui)
        {
            Assert.AreEqual(DisplayStyle.Flex, ui.Q<VisualElement>("defence-prisoners").style.display.value);
            Assert.AreEqual("Enemy prisoners held: 0", ui.Q<Label>("prisoners-held").text);
            var s = bridge.State;
            s.PrisonersHeld["egypt"] = 2; s.PrisonersHeld["jordan"] = 1;
            s.PrisonersReturned["lebanon"] = 4;
            s.SoldiersHeld["egypt"] = 1;
            defence.Render();
            Assert.AreEqual("Enemy prisoners held: 3,000", ui.Q<Label>("prisoners-held").text);
            Assert.AreEqual("Prisoners returned: 4,000", ui.Q<Label>("prisoners-returned").text);
            Assert.AreEqual("Our soldiers held by the enemy: 1,000", ui.Q<Label>("soldiers-held").text);
        }
        var (legacy, _, _) = OpenLegacy();
        using (legacy) Assert.AreEqual(DisplayStyle.None, legacy.Q<VisualElement>("defence-prisoners").style.display.value);
    }

    [Test]
    public void ADisposedScreenHearsNothing()
    {
        var (ui, bridge, defence) = Open();
        using (ui)
        {
            defence.Dispose();
            ui.Q<Label>("defence-command").text = "untouched";
            ui.Press("de-escalate");
            Assert.AreEqual(0, bridge.Queued.Count);
            bridge.EndTurn();
            Assert.AreEqual("untouched", ui.Q<Label>("defence-command").text);
        }
    }

    [Test]
    public void CallUpCanBeRaisedButNotPast100Percent()
    {
        var (ui, bridge, defence) = OpenLegacy();
        using (ui)
        {
            for (int i = 0; i < 6; i++) ui.Press("mob-up");
            Assert.AreEqual(1.0, defence.ProposedMobilization, 1e-9);
            StringAssert.StartsWith("100% of reserves called up", ui.Q<Label>("mob-level").text);
            bridge.EndTurn();
            Assert.AreEqual(1.0, bridge.State.Mobilization, 1e-9);
        }
    }

    [Test]
    public void TheCeasefireClockCountsDownEachTurn()
    {
        var (ui, bridge, _) = OpenLegacy();
        using (ui)
        {
            bridge.EndTurn();   // a monthly turn counts 4 weeks (SecuritySystem.Weeks) since the GDD cutover
            Assert.AreEqual("If full war goes on, the UN imposes a ceasefire in 48 weeks", ui.Q<Label>("ceasefire").text);
            Assert.AreEqual(48, bridge.State.CeasefireWeeksLeft);
        }
    }

    [Test]
    public void AQueuedDropFromFullWarSaysTheClockStops()
    {
        var (ui, bridge, _) = OpenLegacy();
        using (ui)
        {
            ui.Press("de-escalate");
            Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("ceasefire").style.display.value);
            Assert.AreEqual("The UN ceasefire clock stops if the level is lowered", ui.Q<Label>("ceasefire").text);
            ui.Press("escalate");
            Assert.AreEqual("If full war goes on, the UN imposes a ceasefire in 52 weeks", ui.Q<Label>("ceasefire").text);
        }
    }

    [Test]
    public void AQueuedRaiseToFullWarShowsTheClockItWillStart()
    {
        var (ui, bridge, _) = OpenLegacy();
        using (ui)
        {
            ui.Press("de-escalate");
            bridge.EndTurn();
            Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("ceasefire").style.display.value);
            ui.Press("escalate");
            Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("ceasefire").style.display.value);
            Assert.AreEqual($"UN ceasefire: {C.Rules.CeasefireWeeks} weeks once level 5 is reached", ui.Q<Label>("ceasefire").text);
            bridge.EndTurn();
            Assert.AreEqual(5, bridge.State.Rung);
            Assert.AreEqual(C.Rules.CeasefireWeeks - 4, bridge.State.CeasefireWeeksLeft);   // the clock started and ran one monthly turn (4 weeks)
        }
    }

    [Test]
    public void ACallUpOrderThatBecomesANoOpIsWithdrawn()
    {
        var (ui, bridge, defence) = Open();
        using (ui)
        {
            ui.Press("de-escalate");
            ui.Press("mob-down");
            Assert.AreEqual(2, bridge.Queued.Count);
            ui.Press("escalate");                                   // back at level 5, where 75% is raised to 80% anyway
            Assert.AreEqual(0, bridge.Queued.Count);
            Assert.AreEqual(bridge.State.Mobilization, defence.ProposedMobilization, 1e-9);
        }
    }

    [Test]
    public void EveryTurnLengthHasAStringAndAnUnknownOneFailsLoudly()
    {
        var text = Text;
        foreach (System.Enum v in System.Enum.GetValues(typeof(TurnLength)))
            Assert.DoesNotThrow(() => { var _ = text[DefencePresenter.TurnKey((TurnLength)v)]; }, v.ToString());
        Assert.Throws<System.ArgumentOutOfRangeException>(() => DefencePresenter.TurnKey((TurnLength)99));
    }

    /// <summary>Final review M5: the call-up cost comes from the sim's one rule, and a vacant office reads "(vacant)" as on
    /// the Cabinet and Budget screens.</summary>
    [Test]
    public void TheCostComesFromTheSimAndAVacancySaysSo()
    {
        var c = ContentLoader.Load(Paths.Content);
        foreach (var p in c.People) p.Offices.RemoveAll(t => t.Office == "defense_minister");   // no Minister of Defense
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(c, GoldenRun.Seed, Mode.Historical), Text);
        new DefencePresenter(ui.Root, bridge, Text);
        StringAssert.StartsWith("Minister of Defense (vacant) · Chief of Staff ", ui.Q<Label>("defence-command").text);
        Assert.AreEqual(Text.Format("defence.mob_cost", PlayerText.Money(c, "treasury", EconomySystem.MobilizationCost(c, bridge.State.Date, bridge.State.Mobilization))),
            ui.Q<Label>("mob-cost").text);
    }
}
