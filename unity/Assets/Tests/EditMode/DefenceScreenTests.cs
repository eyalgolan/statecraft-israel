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
            Assert.AreEqual("If full war goes on, the UN imposes a ceasefire in 52 weeks", ui.Q<Label>("ceasefire").text);
            Assert.AreEqual("Minister of Defense David Ben-Gurion · Chief of Staff Yaakov Dori", ui.Q<Label>("defence-command").text);
            Assert.AreEqual("80% of reserves called up (level 5 needs at least 80%)", ui.Q<Label>("mob-level").text);
            Assert.AreEqual("Costs IL 48.0m a year in pay and lost work", ui.Q<Label>("mob-cost").text);
            Assert.IsFalse(ui.Q<Button>("escalate").enabledSelf);
        }
    }

    [Test]
    public void OrdersAreQueuedAndTheTurnCarriesThemOut()
    {
        var (ui, bridge, defence) = Open();
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
        var (ui, bridge, _) = Open();
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
        var (ui, bridge, defence) = Open();
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
        var (ui, bridge, _) = Open();
        using (ui)
        {
            Assert.IsNull(bridge.Queue(new SetPolicy(C.Policies[0].Id, 0.5)));
            Assert.IsNull(bridge.Queue(new SetPolicy(C.Policies[1].Id, 0.5)));
            ui.Press("de-escalate");
            Assert.AreEqual("Not done: Not enough attention (need 2.0, have 0.0)", ui.Q<Label>("toast").text);
            Assert.AreEqual("", ui.Q<Label>("order-pending").text);
        }
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
        var (ui, bridge, defence) = Open();
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
        var (ui, bridge, _) = Open();
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
        var (ui, bridge, _) = Open();
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
        var (ui, bridge, _) = Open();
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
