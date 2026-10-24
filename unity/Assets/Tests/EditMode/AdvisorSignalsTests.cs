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
        b.State.GameOver = GameOverCodes.DefeatWar;
        Assert.IsTrue(Holds(b, "defeat_war")); Assert.IsFalse(Holds(b, "defeat_economy"));
    }

    [Test]
    public void TheCityAtRiskSignalIsTheSimsHelper()
    {
        var b = Bridge();
        Assert.AreEqual(Threats.CitiesAtRisk(C, b.State).Count > 0, Holds(b, "city_at_risk"));
    }

    /// <summary>Spec 6: "Off" keeps every critical warning, and two in one turn both show, one after the other.</summary>
    [Test]
    public void OffStillShowsEveryCriticalWarning()
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        using var rig = new AdvisorPopupTests.Rig(catalog: System.IO.File.ReadAllText(AdvisorCatalog.Path), text: text);
        rig.Session.Settings.SetLevel("off");
        rig.Bridge.State.ExhaustedTurns = 1; rig.Bridge.State.Debt = Ceiling(rig.Bridge) * 0.9;
        rig.Advisor.Notice(AdvisorSignals.TurnEnded);
        var shown = new System.Collections.Generic.List<string>();
        while (rig.Advisor.Shown is AdvisorCatalog.Message m) { shown.Add(m.Id); rig.Ui.Press("advisor-got-it"); }
        CollectionAssert.IsSubsetOf(new[] { "ben_gurion.exhausted", "meyerson.debt_ceiling" }, shown);
        Assert.IsTrue(shown.All(id => rig.Session.Catalog.Find(id)!.Critical), "Off: nothing but critical");
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
        b.State.UnlockedProjects.Add("any"); b.State.ActiveSituations.Add("any");
        Assert.IsTrue(Holds(b, "project_unlocked")); Assert.IsTrue(Holds(b, "situation_active"));
    }
    [Test]
    public void DayansSignalsReadTheShownStateAndTheLastTurn()
    {
        var b = Bridge();
        Assert.AreEqual(b.State.Units.Values.Any(u => u.Side == Movement.PlayerSide && u.Order != ""), Holds(b, "standing_order"));
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
        Assert.AreEqual(b.State.Cities.Values.Sum(x => x.Refugees),
            AdvisorSignals.Measures["refugees_at_least"](new AdvisorContext(b, null)));
    }

    [Test]
    public void TruceSignalUsesThePublicSimHelper()
    {
        var b = Bridge();
        Assert.AreEqual(Combat.TruceUntil(b.State) != null, Holds(b, "truce_active"));
    }
}
