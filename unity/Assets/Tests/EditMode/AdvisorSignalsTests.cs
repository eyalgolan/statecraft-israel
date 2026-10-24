#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Spec 5 and 6: each signal on built states, and the critical warnings at every help level.</summary>
public class AdvisorSignalsTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static SimBridge Bridge() => new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), En);
    static bool Holds(SimBridge b, string signal) => AdvisorSignals.States[signal](new AdvisorContext(b, null));
    static double Ceiling(SimBridge b) => C.RulesAt(b.State.Date).Mvp!.DebtCeiling!.Value;

    readonly List<string> temp = new List<string>();
    string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-signals-" + Guid.NewGuid().ToString("N"));
        temp.Add(dir);
        return dir;
    }

    [TearDown]
    public void Clean()
    {
        if (temp.Count == 0) return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);   // after the scene test
        foreach (var d in temp) { try { if (Directory.Exists(d)) Directory.Delete(d, true); } catch (Exception) { } }
        temp.Clear();
    }

    /// <summary>The ids the queue would show now, in order, taking each as Got it would (the popup itself untouched).</summary>
    static List<string> Waiting(AdvisorSession session, SimBridge bridge)
    {
        var ids = new List<string>();
        while (session.Queue.Head(bridge.State.Turn, hebrew: false) is AdvisorCatalog.Message m) { ids.Add(m.Id); session.Queue.Done(m, bridge.State.Turn, bridge.State.Date); }
        return ids;
    }

    /// <summary>Review (must-fix 2): a game loaded with Continue already at the debt ceiling has its critical warning as the
    /// start screen closes, not only after the next change.</summary>
    [Test]
    public void AContinuedGameHearsItsWarningsAtOnce()
    {
        var store = new SaveStore(TempDir());
        var played = new SimBridge(GameSession.New(C, 9, Mode.Historical), En);
        played.EndTurn();
        store.WriteAutosave(played.Session.Save.Write());
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        Object.FindFirstObjectByType<MapScene>().Build(inputs.Map);
        var game = Object.FindFirstObjectByType<GameRoot>();
        game.Store = store; game.Settings = new SettingsStore(TempDir()); game.Language = UiLanguage.English;
        game.Begin(inputs, 0, startScreen: true);
        var bridge = game.Bridge!;
        bridge.State.Debt = Ceiling(bridge);                             // as a save at the ceiling would load
        UiTestKit.Press(Object.FindFirstObjectByType<UIDocument>().rootVisualElement.Q<Button>("start-continue"));
        CollectionAssert.Contains(Waiting(game.AdvisorSession!, bridge), "meyerson.debt_ceiling");
    }

    /// <summary>Review (must-fix 3): Submit takes the unsubmitted-budget warning back, shown or waiting, as GameRoot wires the
    /// Budget screen to the advisor.</summary>
    [Test]
    public void SubmittingTheBudgetTakesItsWarningBack()
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        using var rig = new AdvisorPopupTests.Rig(catalog: System.IO.File.ReadAllText(AdvisorCatalog.Path), text: text);
        using var budget = new BudgetPresenter(rig.Ui.Root, rig.Bridge, text);
        budget.UnsubmittedChanged += rig.Advisor.BudgetUnsubmitted;
        rig.Ui.Press("budget-education_minister-up");
        Assert.AreEqual("meyerson.budget_unsubmitted", rig.Advisor.Shown?.Id, "an edit not submitted");
        rig.Ui.Press("budget-submit");
        Assert.AreNotEqual("meyerson.budget_unsubmitted", rig.Advisor.Shown?.Id, "submitted: no longer true");
        CollectionAssert.DoesNotContain(Drain(rig), "meyerson.budget_unsubmitted", "nor waiting");
    }

    /// <summary>Review (should 4): a new help level applies to the popup at once, both ways.</summary>
    [Test]
    public void ANewHelpLevelAppliesAtOnce()
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        using var rig = new AdvisorPopupTests.Rig(catalog: System.IO.File.ReadAllText(AdvisorCatalog.Path), text: text);
        rig.Session.Settings.SetLevel("off");
        rig.Advisor.Notice(AdvisorSignals.BudgetUnsubmitted);
        Assert.IsNull(rig.Advisor.Shown, "Off: no warning");
        rig.Session.Settings.SetLevel("warnings");
        Assert.AreEqual("meyerson.budget_unsubmitted", rig.Advisor.Shown?.Id, "Warnings only: shown at once");
        rig.Session.Settings.SetLevel("off");
        Assert.IsNull(rig.Advisor.Shown, "Off again: gone at once");
    }

    /// <summary>The owner approved these Hebrew lines word for word (24 October 2026: "Hebrew approved" for the critical three,
    /// "approved" for the rest; defeat_economy with the review sheet's grammar fix, תורים); a different text
    /// needs a new approval, so it must also set he_review back to pending.</summary>
    [TestCase("dayan.city_at_risk", "התקפת אויב מכוונת לאחת מערי המדינה. בדקו את מגיני העיר ואת תחזית הקרב לפני ששלב האויב מוכרע.")]
    [TestCase("meyerson.exhausted", "הכסף והמזון אזלו יחד. החזירו אחד מהם לפני שמונה ההפסד יגיע לגבולו.")]
    [TestCase("meyerson.debt_ceiling", "החוב הגיע לתקרה. אם גם מלאי המזון ריק בסוף התור, מונה ההפסד מתחיל או ממשיך לרוץ.")]
    [TestCase("meyerson.food_short", "בתור הזה המדינה נזקקה ליותר מזון ממה שהיה לה. מחסור במזון גם מוריד את הלכידות החברתית.")]
    [TestCase("dayan.defeat_war", "יחידה צבאית של האויב מחזיקה באחת מערי המדינה בסוף התור. המערכה הסתיימה בתבוסה.")]
    [TestCase("ben_gurion.defeat_economy", "שלושה תורים ברציפות הסתיימו כשהחוב שלנו בתקרה או מעליה ומלאי המזון ריק, וזה מסיים את המשחק.")]
    [TestCase("meyerson.budget_unsubmitted", "בתקציב יש שינויים שלא הוגשו. הם לא ייכנסו לתוקף בסיום התור עד שתגישו את התקציב.")]
    [TestCase("dayan.arms_out", "הנשק מצייד את האימון הצבאי ומתכלה בהתקפות. קניית נשק בכסף לפני שהמלאי אוזל שומרת את האפשרויות האלה פתוחות.")]
    public void AnApprovedHebrewLineIsTheTextTheOwnerRead(string id, string hebrew)
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        Assert.AreEqual(AdvisorCatalog.Approved, AdvisorCatalog.Parse(System.IO.File.ReadAllText(AdvisorCatalog.Path)).Find(id)!.HeReview);
        Assert.AreEqual(hebrew, he["advisor." + id + ".text"]);
    }

    [Test]
    public void TheEconomySignalsReadTheShownState()
    {
        var b = Bridge();
        foreach (var s in new[] { "exhausted", "in_debt", "food_short", "defeat_war", "defeat_economy" }) Assert.IsFalse(Holds(b, s), "a new game: " + s);
        b.State.ExhaustedTurns = 1; b.State.Debt = Ceiling(b) * 0.85; b.State.FoodUnmet = 3;
        Assert.IsTrue(Holds(b, "exhausted")); Assert.IsTrue(Holds(b, "in_debt")); Assert.IsTrue(Holds(b, "food_short"));
        Assert.AreEqual(0.85, AdvisorSignals.Measures["debt_ratio_at_least"](new AdvisorContext(b, null)), 1e-9);
        // meyerson.debt_ceiling's bound, read from the shipped row: the ceiling itself (R-END-001: Money is exhausted at or above it).
        var atCeiling = AdvisorCatalog.Parse(System.IO.File.ReadAllText(AdvisorCatalog.Path)).Find("meyerson.debt_ceiling")!.When;
        b.State.Debt = Ceiling(b) * 0.99; Assert.IsFalse(AdvisorSignals.When(new AdvisorContext(b, null), atCeiling), "below the ceiling");
        b.State.Debt = Ceiling(b); Assert.IsTrue(AdvisorSignals.When(new AdvisorContext(b, null), atCeiling), "at the ceiling");
        b.State.Debt = Ceiling(b) * 1.1; Assert.IsTrue(AdvisorSignals.When(new AdvisorContext(b, null), atCeiling), "above the ceiling");
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
        rig.Bridge.State.ExhaustedTurns = 1; rig.Bridge.State.Debt = Ceiling(rig.Bridge); rig.Bridge.State.FoodUnmet = 3;
        rig.Advisor.Notice(AdvisorSignals.TurnEnded);
        var shown = Drain(rig);
        CollectionAssert.IsSubsetOf(new[] { "meyerson.exhausted", "meyerson.debt_ceiling" }, shown);
        CollectionAssert.DoesNotContain(shown, "meyerson.food_short", "Off");
        Assert.IsTrue(shown.All(id => rig.Session.Catalog.Find(id)!.Critical), "Off: nothing but critical");
        rig.Session.Settings.SetLevel("warnings");
        rig.Advisor.Notice(AdvisorSignals.TurnEnded);
        CollectionAssert.Contains(Drain(rig), "meyerson.food_short", "Warnings only");
    }

    /// <summary>The owner approved the critical warnings' Hebrew (24 October 2026, "Hebrew approved"): a Hebrew player sees
    /// them at Off; food_short, approved too, waits for Warnings only.</summary>
    [Test]
    public void TheCriticalWarningsShowInHebrew()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text);
        using var rig = new AdvisorPopupTests.Rig(catalog: System.IO.File.ReadAllText(AdvisorCatalog.Path), text: he);
        rig.Session.Settings.SetLevel("off");
        rig.Bridge.State.ExhaustedTurns = 1; rig.Bridge.State.Debt = Ceiling(rig.Bridge); rig.Bridge.State.FoodUnmet = 3;
        rig.Advisor.Notice(AdvisorSignals.TurnEnded);
        CollectionAssert.IsSubsetOf(new[] { "meyerson.exhausted", "meyerson.debt_ceiling" }, Drain(rig));
        rig.Session.Settings.SetLevel("warnings");
        rig.Advisor.Notice(AdvisorSignals.TurnEnded);
        CollectionAssert.Contains(Drain(rig), "meyerson.food_short", "approved Hebrew, at Warnings only");
        foreach (var id in new[] { "dayan.city_at_risk", "meyerson.exhausted", "meyerson.debt_ceiling" })
            Assert.IsTrue(rig.Session.Catalog.Find(id)!.ApprovedIn(hebrew: true), id);
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

    /// <summary>The owner's 24 October 2026 ruling ("Adopt clip words"): these messages say the recorded clips' words, by
    /// the clips' speakers (talking-v3/lines.csv), so a clip can attach to its message; a text edit needs a new recording.</summary>
    [TestCase("dayan.city_at_risk", "dayan", "An enemy attack is aimed at one of the country's cities. Review its defenders and the combat preview before the enemy phase resolves.")]
    [TestCase("meyerson.exhausted", "meyerson", "Money and Food are exhausted together. Restore either one before the defeat counter reaches its limit.")]
    [TestCase("meyerson.debt_ceiling", "meyerson", "Debt has reached the ceiling. If Food is also empty at turn end, the defeat counter begins or continues.")]
    [TestCase("dayan.defeat_war", "dayan", "An enemy military unit holds one of the country's cities at turn end. The campaign has ended in defeat.")]
    [TestCase("dayan.arms_out", "dayan", "Arms equip military training and are spent in attacks. Buying Arms with Money before the stock runs short keeps those choices available.")]
    public void AClipLineKeepsTheRecordedWordsAndSpeaker(string id, string advisor, string words)
    {
        var m = AdvisorCatalog.Parse(System.IO.File.ReadAllText(AdvisorCatalog.Path)).Find(id);
        Assert.IsNotNull(m, id);
        Assert.AreEqual(advisor, m!.Advisor);
        Assert.AreEqual(words, En["advisor." + id + ".text"]);
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
        Assert.AreEqual(C.IsGdd, Holds(b, "arms_low"), "an empty stock");
        Assert.AreEqual(b.State.Settlements.Values.Any(x => x.State == SettlementControl.AwaitingChoice), Holds(b, "capture_waiting"));
    }
    /// <summary>Owner, 24 October 2026: "Fire when low". Low (an owner-set DESIGN VALUE, "Approve 3 / costliest action") is at or below the Arms the
    /// costliest single Arms action takes under the current rules: a recruit group (mvp.arms_per_recruit_group, R-ARMS-003),
    /// an attack (mvp.arms_per_attack, R-COMBAT-004), or an Israeli type's training_arms or arms_per_fire (R-UNIT-008,
    /// R-COMBAT-007). Today an artillery group's 3. At or below it fires, above it does not, and an empty stock fires.</summary>
    [Test]
    public void ArmsAreLowAtOrBelowTheCostliestSingleAction()
    {
        var b = Bridge();
        var mvp = C.RulesAt(b.State.Date).Mvp!;
        var israeli = C.UnitTypes.Where(t => t.Side == Movement.PlayerSide).ToList();
        int reserve = new[] { mvp.ArmsPerRecruitGroup!.Value, mvp.ArmsPerAttack!.Value }
            .Concat(israeli.Select(t => t.TrainingArms ?? 0)).Concat(israeli.Select(t => t.ArmsPerFire ?? 0)).Max();
        Assert.AreEqual(3, reserve, "today's content: the artillery group's training_arms (unit_types.yaml)");
        Assert.AreEqual(reserve, AdvisorSignals.ArmsReserve(new AdvisorContext(b, null)));
        Assert.IsFalse(Holds(b, "arms_low"), "a new game's 12 Arms");
        b.State.Arms = reserve + 1; Assert.IsFalse(Holds(b, "arms_low"), "one above");
        b.State.Arms = reserve; Assert.IsTrue(Holds(b, "arms_low"), "at the reserve");
        b.State.Arms = reserve - 1; Assert.IsTrue(Holds(b, "arms_low"), "below");
        b.State.Arms = 0; Assert.IsTrue(Holds(b, "arms_low"), "empty");
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
