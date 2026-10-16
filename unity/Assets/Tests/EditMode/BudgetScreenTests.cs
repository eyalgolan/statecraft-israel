using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class BudgetScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [Test]
    public void AdjustKeepsTheTotalAtExactlyOne()
    {
        var shares = new Dictionary<string, double> { ["a"] = 0.45, ["b"] = 0.35, ["c"] = 0.2 };
        var up = BudgetPresenter.Adjust(shares, "c", 0.05);
        Assert.AreEqual(0.25, up["c"], 1e-12);
        Assert.AreEqual(1.0, up.Values.Sum(), 1e-12);
        Assert.Greater(up["a"] / up["b"], 1.28);                      // the others keep their proportions
        var floor = BudgetPresenter.Adjust(shares, "c", -0.5);
        Assert.AreEqual(0.0, floor["c"]);
        Assert.AreEqual(1.0, floor.Values.Sum(), 1e-12);
    }

    [Test]
    public void TheImmigrationLineNamesTheOwnerOfTheDay()
    {
        // rules.yaml budget_owners (#108): the Minister of Immigration until 7 October 1951, the Prime Minister from 8 October.
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var budget = new BudgetPresenter(ui.Root, bridge, Text);
        bridge.State.Date = new System.DateTime(1951, 10, 7);
        budget.Render();
        Assert.AreEqual("Minister of Immigration (Haim-Moshe Shapira)", ui.Q<Label>("budget-prime_minister-name").text);
        bridge.State.Date = new System.DateTime(1951, 10, 8);
        budget.Render();
        Assert.AreEqual("Prime Minister (David Ben-Gurion)", ui.Q<Label>("budget-prime_minister-name").text);
        Assert.AreEqual("15%", ui.Q<Label>("budget-prime_minister-share").text);       // the line keeps its id; only the owner changes
    }

    [Test]
    public void TheScreenListsTheMinistriesAndSubmitsTheDraft()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var budget = new BudgetPresenter(ui.Root, bridge, Text);
        Assert.AreEqual("45%", ui.Q<Label>("budget-defense_minister-share").text);
        Assert.AreEqual("Minister of Defense (David Ben-Gurion)", ui.Q<Label>("budget-defense_minister-name").text);
        Assert.AreEqual("spending effect 150%", ui.Q<Label>("budget-defense_minister-factor").text);   // capped at 1.5 (playtest 2, P2-6)
        var (seats, size) = PoliticsSystem.CoalitionSeats(C, bridge.State.Date);   // the council approves the budget (#3)
        Assert.AreEqual(System.FormattableString.Invariant($"The Provisional State Council must approve it: the coalition holds {seats} of {size} seats"), ui.Q<Label>("budget-vote").text);
        Assert.AreEqual("Money IL 20.0m · debt IL 0.0m · foreign currency $40.0m", ui.Q<Label>("budget-money").text);
        UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
        Assert.AreEqual("11%", ui.Q<Label>("budget-education_minister-share").text);
        Assert.AreEqual("Changed: submit to put it to the vote", ui.Q<Label>("budget-status").text);
        ui.Press("budget-submit");
        var queued = (AllocateBudget)bridge.Queued.Single();
        Assert.AreEqual(1.0, queued.Shares.Values.Sum(), 1e-9);
        Assert.AreEqual("Submitted: takes effect at End turn", ui.Q<Label>("budget-status").text);
        bridge.EndTurn();
        Assert.AreEqual(budget.Draft["education_minister"], bridge.State.Budget["education_minister"], 1e-12);
        Assert.IsEmpty(bridge.Session.Last!.Rejected);
    }

    [Test]
    public void AdjustHandlesEdges()
    {
        var one = BudgetPresenter.Adjust(new Dictionary<string, double> { ["a"] = 1.0, ["b"] = 0.0 }, "a", -0.05);
        Assert.AreEqual(0.95, one["a"], 1e-12); Assert.AreEqual(0.05, one["b"], 1e-12);     // nothing to rescale: split evenly
        var cap = BudgetPresenter.Adjust(new Dictionary<string, double> { ["a"] = 0.9, ["b"] = 0.1 }, "a", 0.5);
        Assert.AreEqual(1.0, cap["a"]); Assert.AreEqual(0.0, cap["b"], 1e-12);
        var lone = BudgetPresenter.Adjust(new Dictionary<string, double> { ["a"] = 1.0 }, "a", -0.5);
        Assert.AreEqual(0.5, lone["a"], 1e-12);                                           // a single line cannot leave 1: the sim would refuse a sum of 0.5
    }

    [Test]
    public void FactorsAndFlowsComeFromTheSim()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var budget = new BudgetPresenter(ui.Root, bridge, Text);
        foreach (var office in C.Rules.BudgetStart.Keys)
            Assert.AreEqual("spending effect " + PlayerText.Percent(EffectsSystem.BudgetFactor(budget.Draft, office)), ui.Q<Label>($"budget-{office}-factor").text, office);
        var f = EconomySystem.Flows(C, bridge.State, 1.0);
        Assert.AreEqual(System.FormattableString.Invariant($"Taxes IL {f.Tax:0.0}m, spending IL {f.Spending:0.0}m and reserve call-up IL {f.Mobilization:0.0}m a year"), ui.Q<Label>("budget-flow").text);
        UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
        Assert.AreEqual("spending effect " + PlayerText.Percent(EffectsSystem.BudgetFactor(budget.Draft, "education_minister")), ui.Q<Label>("budget-education_minister-factor").text);
    }

    [Test]
    public void TheDraftSurvivesOtherChangesAndResetsToTheBudget()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var budget = new BudgetPresenter(ui.Root, bridge, Text);
        UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
        bridge.Queue(new SecurityOrder(-1));                                  // another order changes the bridge; the draft stays
        Assert.AreEqual("11%", ui.Q<Label>("budget-education_minister-share").text);
        ui.Press("budget-reset");
        Assert.AreEqual("6%", ui.Q<Label>("budget-education_minister-share").text);
        Assert.AreEqual("", ui.Q<Label>("budget-status").text);
    }

    [Test]
    public void ALegislatureThatMustApproveIsNamed()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!(Offices.Legislature(C, g.State.Date)?.BudgetApproval ?? false)) g.Step(new Command[0]);
        var bridge = new SimBridge(g, Text);
        new BudgetPresenter(ui.Root, bridge, Text);
        var leg = Offices.Legislature(C, g.State.Date)!;
        var (seats, size) = PoliticsSystem.CoalitionSeats(C, g.State.Date);
        Assert.AreEqual(System.FormattableString.Invariant($"The {leg.Body} must approve it: the coalition holds {seats} of {size} seats"), ui.Q<Label>("budget-vote").text);
    }

    [Test]
    public void ADisposedScreenHearsNothing()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var budget = new BudgetPresenter(ui.Root, bridge, Text);
        budget.Dispose();
        ui.Q<Label>("budget-money").text = "untouched";
        ui.Press("budget-submit");
        Assert.AreEqual(0, bridge.Queued.Count);
        bridge.EndTurn();
        Assert.AreEqual("untouched", ui.Q<Label>("budget-money").text);
    }

    static (UiTestKit ui, SimBridge bridge, BudgetPresenter budget) Open()
    {
        var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        return (ui, bridge, new BudgetPresenter(ui.Root, bridge, Text));
    }

    [Test]
    public void ALoadedGameNeverInheritsADirtyDraft()
    {
        var (ui, bridge, budget) = Open();
        using (ui)
        {
            UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
            var other = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            bridge.Replace(other);
            foreach (var kv in other.State.Budget) Assert.AreEqual(kv.Value, budget.Draft[kv.Key], 1e-12, kv.Key);
            Assert.AreEqual("", ui.Q<Label>("budget-status").text);
            Assert.AreEqual("6%", ui.Q<Label>("budget-education_minister-share").text);
        }
    }

    [Test]
    public void AnUnsubmittedDraftSurvivesEndTurnAndIsNeverAppliedSilently()
    {
        var (ui, bridge, budget) = Open();
        using (ui)
        {
            var before = bridge.State.Budget["education_minister"];
            UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
            bridge.EndTurn();
            Assert.AreEqual(before, bridge.State.Budget["education_minister"], 1e-12, "never submitted, so never applied");
            Assert.AreEqual(before + 0.05, budget.Draft["education_minister"], 1e-9, "the draft is kept");
            Assert.AreEqual("Changed: submit to put it to the vote", ui.Q<Label>("budget-status").text);
        }
    }

    [Test]
    public void TheStatusFollowsWhatIsActuallyQueued()
    {
        var (ui, bridge, budget) = Open();
        using (ui)
        {
            UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
            ui.Press("budget-submit");
            Assert.AreEqual("Submitted: takes effect at End turn", ui.Q<Label>("budget-status").text);
            UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
            Assert.AreEqual("Changed: submit to put it to the vote", ui.Q<Label>("budget-status").text);
            ui.Press("budget-submit");
            Assert.AreEqual("Submitted: takes effect at End turn", ui.Q<Label>("budget-status").text);
            var queued = (AllocateBudget)bridge.Queued.Single();
            foreach (var kv in budget.Draft) Assert.AreEqual(kv.Value, queued.Shares[kv.Key], 1e-12);
        }
    }

    [Test]
    public void SubmittingAnUnchangedBudgetSpendsNothing()
    {
        var (ui, bridge, _) = Open();
        using (ui)
        {
            ui.Press("budget-submit");
            Assert.AreEqual(0, bridge.Queued.Count);
            Assert.AreEqual(bridge.State.Attention, bridge.AttentionLeft, 1e-12);
            Assert.IsFalse(ui.Q<Button>("budget-submit").enabledSelf);
            UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
            Assert.IsTrue(ui.Q<Button>("budget-submit").enabledSelf);
            ui.Press("budget-submit");
            ui.Press("budget-reset");                                 // back to today's budget: submitting now withdraws the order
            Assert.IsTrue(ui.Q<Button>("budget-submit").enabledSelf);
            ui.Press("budget-submit");
            Assert.AreEqual(0, bridge.Queued.Count);
        }
    }

    [Test]
    public void AdjustAndChangeSurviveAMissingOffice()
    {
        var shares = new Dictionary<string, double> { ["a"] = 0.5, ["b"] = 0.5 };
        var same = BudgetPresenter.Adjust(shares, "zzz", 0.05);
        Assert.AreEqual(0.5, same["a"]); Assert.IsFalse(same.ContainsKey("zzz"));
        foreach (var v in BudgetPresenter.Adjust(new Dictionary<string, double> { ["a"] = 0.3, ["b"] = 0.3, ["c"] = 0.4 }, "a", 0.7).Values) Assert.GreaterOrEqual(v, 0);
        var (ui, bridge, budget) = Open();
        using (ui)
        {
            Assert.DoesNotThrow(() => budget.Change("zzz", 0.05));
            foreach (var kv in bridge.State.Budget) Assert.AreEqual(kv.Value, budget.Draft[kv.Key], 1e-12);
        }
    }

    /// <summary>#217: Education has a budget line from 1948, but the ministry dates from 10 March 1949. Its row says so
    /// instead of "vacant", and the Cabinet does not list it.</summary>
    [Test]
    public void AMinistryNotYetEstablishedSaysWhenItBegins()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        using var ui = new UiTestKit();
        new BudgetPresenter(ui.Root, new SimBridge(g, Text), Text);
        Assert.AreEqual("Minister of Education and Culture (no ministry until 10 March 1949)", ui.Q<Label>("budget-education_minister-name").text);
        Assert.IsFalse(Cabinet.Lineup(C, g.State).Any(x => x.Office.Id == "education_minister"));
    }

    /// <summary>Playtest 1, item 6: a vacant ministry reads "(vacant)", as on the Cabinet screen, never "(-)".</summary>
    [Test]
    public void AVacantMinistryReadsVacant()
    {
        // No budgeted ministry falls vacant in the historical run before the founding era ends (1 January 1957); the first
        // is Agriculture in May 1961. So this test plays on past the end, on its own copy of the content.
        var c = ContentLoader.Load(Paths.Content);
        c.Eras[c.Eras.Count - 1].To = new System.DateTime(1990, 1, 1);
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        Seat vacant = null;
        for (int i = 0; i < 400 && g.State.GameOver == null && vacant == null; i++)
        {
            vacant = Cabinet.Lineup(c, g.State).FirstOrDefault(x => x.Holder == null && g.State.Budget.ContainsKey(x.Office.Id));
            if (vacant == null) g.Step(HistoricalAI.Commands(c, g.State));
        }
        Assert.IsNotNull(vacant, "the historical game has a vacant budgeted ministry to test with");
        using var ui = new UiTestKit();
        new BudgetPresenter(ui.Root, new SimBridge(g, Text), Text);
        Assert.AreEqual(vacant.Office.Title + " (vacant)", ui.Q<Label>("budget-" + vacant.Office.Id + "-name").text);
    }

    /// <summary>eras.yaml systems.legislature (#58): where the body votes on the budget, an assembly era says its vote does
    /// not bind and a state era says it must approve. Played on a copy of the content with the switch set.</summary>
    [Test]
    public void TheBudgetVoteLineFollowsTheErasSwitch()
    {
        var c = ContentLoader.Load(Paths.Content);
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        while (!(Offices.Legislature(c, g.State.Date)?.BudgetApproval ?? false)) g.Step(new Command[0]);
        var leg = Offices.Legislature(c, g.State.Date)!;
        var (seats, size) = PoliticsSystem.CoalitionSeats(c, g.State.Date);
        using (var ui = new UiTestKit())
        {
            c.Eras[0].Systems.Legislature = EraSystems.Assembly;
            new BudgetPresenter(ui.Root, new SimBridge(g, Text), Text);
            Assert.AreEqual(System.FormattableString.Invariant($"The {leg.Body} votes on it, but its vote does not bind: the coalition holds {seats} of {size} seats"), ui.Q<Label>("budget-vote").text);
        }
        using (var ui = new UiTestKit())
        {
            c.Eras[0].Systems.Legislature = EraSystems.State;
            new BudgetPresenter(ui.Root, new SimBridge(g, Text), Text);
            Assert.AreEqual(System.FormattableString.Invariant($"The {leg.Body} must approve it: the coalition holds {seats} of {size} seats"), ui.Q<Label>("budget-vote").text);
        }
    }
}
