#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>Spec 5 and 6: each signal on built states, and the critical warnings at every help level.</summary>
public class AdvisorSignalsTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static SimBridge Bridge() => new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), En);
    static bool Holds(SimBridge b, string signal) => AdvisorSignals.States[signal](new AdvisorContext(b, null));
    static double Ceiling(SimBridge b) => C.RulesAt(b.State.Date).Mvp!.DebtCeiling!.Value;

    [Test]
    public void TheEconomySignalsReadTheShownState()
    {
        var b = Bridge();
        foreach (var s in new[] { "exhausted", "in_debt", "food_short", "defeat_war", "defeat_economy" }) Assert.IsFalse(Holds(b, s), "a new game: " + s);
        b.State.ExhaustedTurns = 1; b.State.Debt = Ceiling(b) * 0.85; b.State.FoodUnmet = 3;
        Assert.IsTrue(Holds(b, "exhausted")); Assert.IsTrue(Holds(b, "in_debt")); Assert.IsTrue(Holds(b, "food_short"));
        Assert.AreEqual(0.85, AdvisorSignals.Measures["debt_ratio_at_least"](new AdvisorContext(b, null)), 1e-9);
        // meyerson.debt_ceiling's bound: "close to the ceiling" starts at 0.8 of it, not below.
        var close = new System.Collections.Generic.Dictionary<string, double> { ["debt_ratio_at_least"] = 0.8 };
        b.State.Debt = Ceiling(b) * 0.79; Assert.IsFalse(AdvisorSignals.When(new AdvisorContext(b, null), close), "0.79");
        b.State.Debt = Ceiling(b) * 0.8; Assert.IsTrue(AdvisorSignals.When(new AdvisorContext(b, null), close), "0.8");
        b.State.GameOver = GameOverCodes.DefeatWar;
        Assert.IsTrue(Holds(b, "defeat_war")); Assert.IsFalse(Holds(b, "defeat_economy"));
    }

    /// <summary>An enemy unit on one of our cities' hexes is a city at risk (Threats.CitiesAtRisk); taken away, the signal
    /// reads the helper again.</summary>
    [Test]
    public void TheCityAtRiskSignalIsTheSimsHelper()
    {
        var b = Bridge();
        bool before = Threats.CitiesAtRisk(C, b.State).Count > 0;
        Assert.AreEqual(before, Holds(b, "city_at_risk"));
        var city = C.SettlementHexes.First(h => b.State.Cities.ContainsKey(h.Id));
        int id = b.State.Units.Keys.Max() + 1;
        b.State.Units[id] = new Unit(id, "egyptian_army", city.Q, city.R, side: "egypt", strength: 10);
        CollectionAssert.Contains(Threats.CitiesAtRisk(C, b.State).ToList(), city.Id);
        Assert.IsTrue(Holds(b, "city_at_risk"), "an enemy unit stands on " + city.Id);
        b.State.Units.Remove(id);
        Assert.AreEqual(before, Holds(b, "city_at_risk"));
    }

    /// <summary>Spec 6: "Off" keeps every critical warning, and two in one turn both show, one after the other; a
    /// warnings-level message that holds at the same time (food_short) waits for "Warnings only" or chattier.</summary>
    [Test]
    public void OffStillShowsEveryCriticalWarning()
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        using var rig = new AdvisorPopupTests.Rig(catalog: System.IO.File.ReadAllText(AdvisorCatalog.Path), text: text);
        rig.Session.Settings.SetLevel("off");
        rig.Bridge.State.ExhaustedTurns = 1; rig.Bridge.State.Debt = Ceiling(rig.Bridge) * 0.9; rig.Bridge.State.FoodUnmet = 3;
        rig.Advisor.Notice(AdvisorSignals.TurnEnded);
        var shown = Drain(rig);
        CollectionAssert.IsSubsetOf(new[] { "ben_gurion.exhausted", "meyerson.debt_ceiling" }, shown);
        CollectionAssert.DoesNotContain(shown, "meyerson.food_short", "Off");
        Assert.IsTrue(shown.All(id => rig.Session.Catalog.Find(id)!.Critical), "Off: nothing but critical");
        rig.Session.Settings.SetLevel("warnings");
        rig.Advisor.Notice(AdvisorSignals.TurnEnded);
        CollectionAssert.Contains(Drain(rig), "meyerson.food_short", "Warnings only");
    }

    /// <summary>Task 18: an unsubmitted budget change (BudgetPresenter.UnsubmittedChanged, wired by GameRoot) is a warning.</summary>
    [Test]
    public void AnUnsubmittedBudgetIsAWarning()
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        using var rig = new AdvisorPopupTests.Rig(catalog: System.IO.File.ReadAllText(AdvisorCatalog.Path), text: text);
        rig.Session.Settings.SetLevel("off");
        rig.Advisor.Notice(AdvisorSignals.BudgetUnsubmitted);
        CollectionAssert.DoesNotContain(Drain(rig), "meyerson.budget_unsubmitted", "Off");
        rig.Session.Settings.SetLevel("warnings");
        rig.Advisor.Notice(AdvisorSignals.BudgetUnsubmitted);
        CollectionAssert.Contains(Drain(rig), "meyerson.budget_unsubmitted", "Warnings only");
    }

    static System.Collections.Generic.List<string> Drain(AdvisorPopupTests.Rig rig)
    {
        var shown = new System.Collections.Generic.List<string>();
        while (rig.Advisor.Shown is AdvisorCatalog.Message m) { shown.Add(m.Id); rig.Ui.Press("advisor-got-it"); }
        return shown;
    }

    [Test]
    public void TheEndScreenNamesTheWarLoss()
    {
        foreach (var file in new[] { UiPaths.Strings, UiPaths.StringsHebrew })
            Assert.IsTrue(UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(file).text).Has("end.outcome.defeat_war"), file);
        Assert.AreEqual("The enemy holds a city.", En["end.outcome.defeat_war"], "Q-DEF-4 (b)'s line (PlayerText.cs:27)");
    }

    [Test]
    public void AnOrderRefusedForAttentionRaisesItsOwnEvent()
    {
        var b = Bridge();
        var ev = C.Events.First(e => e.Id == b.State.PendingEvents[0]);
        var order = new ChooseEventOption(ev.Id, ev.Options[0].Id);
        Assume.That(b.Price(order), Is.GreaterThan(0), "the opening card's answer costs Attention");
        b.State.Attention = 0;
        int raised = 0; b.RefusedForAttention += () => raised++;
        Assert.IsNotNull(b.Queue(order));
        Assert.AreEqual(1, raised);
        Assert.IsNotNull(b.Queue(new SetPolicy("no_such_policy", 1)), "a refusal for another reason");
        Assert.AreEqual(1, raised, "raises nothing");
    }

    [Test]
    public void AnObjectiveDoneHolds()
    {
        var b = Bridge();
        Assert.IsFalse(Holds(b, "objective_done"));
        b.State.Objectives[C.Objectives[0].Id] = b.State.Date;
        Assert.IsTrue(Holds(b, "objective_done"));
    }
    [Test]
    public void MeyersonsSignalsReadTheShownState()
    {
        var b = Bridge();
        var c = new AdvisorContext(b, null);
        Assert.AreEqual(b.State.UnlockedProjects.Count > 0, Holds(b, "project_unlocked"));
        Assert.AreEqual(b.State.Corridors.Count > 0, Holds(b, "corridor_started"));
        Assert.AreEqual(b.Vacancies.Count > 0, Holds(b, "cabinet_vacancy"));
        Assert.AreEqual(b.State.ActiveSituations.Count > 0, Holds(b, "situation_active"));
        var idle = b.State.Units.Values.Any(u => u.Side == "" && Projects.IsPioneer(u.Type) && !Projects.IsBusy(b.State, u.Id));
        Assert.AreEqual(idle, Holds(b, "idle_pioneer"));
        int id = b.State.Units.Keys.Max() + 1;
        b.State.Units[id] = new Unit(id, "halutz", 0, 0);
        Assert.IsTrue(Holds(b, "idle_pioneer"), "a halutz with no project");
        b.State.UnlockedProjects.Add("any"); b.State.ActiveSituations.Add("any");
        Assert.IsTrue(Holds(b, "project_unlocked")); Assert.IsTrue(Holds(b, "situation_active"));
    }
    [Test]
    public void DayansSignalsReadTheShownStateAndTheLastTurn()
    {
        var b = Bridge();
        bool ordered = b.State.Units.Values.Any(u => u.Side == Movement.PlayerSide && u.Order != "");
        Assert.AreEqual(ordered, Holds(b, "standing_order"));
        int id = b.State.Units.Keys.Max() + 1;
        b.State.Units[id] = new Unit(id, "recruit", 0, 0, side: Movement.PlayerSide, strength: 1, order: "hold");
        Assert.IsTrue(Holds(b, "standing_order"), "a unit on hold");
        b.State.Units.Remove(id);
        Assert.AreEqual(ordered, Holds(b, "standing_order"));
        Assert.IsFalse(Holds(b, "combat_seen"), "no turn yet");
        Assert.IsFalse(Holds(b, "enemy_moved"), "no turn yet");
        b.EndTurn();
        var why = b.Session.Last!.Why;
        Assert.AreEqual(why.Any(w => w.Key == WhyKeys.Engagement), Holds(b, "combat_seen"));
        Assert.AreEqual(why.Any(w => w.EnemyOrder != ""), Holds(b, "enemy_moved"));
        b.State.Arms = 0;
        Assert.AreEqual(C.IsGdd, Holds(b, "arms_out"));
        Assert.AreEqual(b.State.Settlements.Values.Any(x => x.State == SettlementControl.AwaitingChoice), Holds(b, "capture_waiting"));
    }
    [Test]
    public void RefugeesAreCountedFromTheShownCities()
    {
        var b = Bridge();
        double before = b.State.Cities.Values.Sum(x => x.Refugees);
        Assert.AreEqual(before, AdvisorSignals.Measures["refugees_at_least"](new AdvisorContext(b, null)));
        var city = b.State.Cities.Values.First();
        b.State.Cities[city.Id] = city.WithCounts(city.Civilians, city.Employed, city.Refugees + 7);
        Assert.AreEqual(before + 7, AdvisorSignals.Measures["refugees_at_least"](new AdvisorContext(b, null)));
    }

    [Test]
    public void TruceSignalUsesThePublicSimHelper()
    {
        var b = Bridge();
        Assert.IsFalse(Holds(b, "truce_active"), "15 May 1948: no truce accepted");
        b.State.ChosenOptions["first_truce"] = "accept";                // R-COMBAT-009: in force until 8 July 1948
        Assert.IsTrue(Holds(b, "truce_active"));
    }
}
