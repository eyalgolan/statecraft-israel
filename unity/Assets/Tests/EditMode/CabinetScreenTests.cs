#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CabinetScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>The UI with the Cabinet screen open, as ScreenNav opens it (a hidden screen does not re-render).</summary>
    static UiTestKit Shown()
    {
        var ui = new UiTestKit();
        ui.Q<VisualElement>("screen-cabinet").style.display = DisplayStyle.Flex;
        return ui;
    }

    /// <summary>The historical game on 26 February 1950: the First Government, whose next turn ends before it falls.</summary>
    static SimBridge In1950()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new DateTime(1950, 2, 1)) g.Step(HistoricalAI.Commands(C, g.State));
        return new SimBridge(g, Text);
    }

    [Test]
    public void TheScreenListsTheGovernmentAndRefusesIllegalAppointmentsWithAReason()
    {
        using var ui = Shown();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        Assert.AreEqual("First Government", ui.Q<Label>("cabinet-government").text);
        StringAssert.StartsWith("Coalition: 73 of 120 seats", ui.Q<Label>("cabinet-coalition").text);
        StringAssert.IsMatch(@"stability \d+%$", ui.Q<Label>("cabinet-coalition").text);   // fix round 1, I2: the unit
        // The finance card (promo 04): name, office, party, and competence and loyalty as bars with their values out of 10.
        Assert.AreEqual("Eliezer Kaplan", ui.Q<Label>("seat-finance_minister-holder").text);
        Assert.AreEqual("Minister of Finance", ui.Q<Label>("seat-finance_minister-office").text);
        Assert.AreEqual("Mapai", ui.Q<Label>("seat-finance_minister-party").text);
        Assert.AreEqual("8", ui.Q<Label>("seat-finance_minister-competence-value").text);
        Assert.AreEqual("9", ui.Q<Label>("seat-finance_minister-loyalty-value").text);
        Assert.AreEqual(0.8f, ui.Q<UiBar>("seat-finance_minister-competence").Value, 1e-6f);
        Assert.AreEqual(0.9f, ui.Q<UiBar>("seat-finance_minister-loyalty").Value, 1e-6f);
        UiTestKit.Press(ui.Q<Button>("seat-finance_minister"));
        Assert.AreEqual("finance_minister", cabinet.Selected);
        Assert.IsFalse(ui.Q<Button>("appoint-zisling").enabledSelf);
        Assert.AreEqual("Aharon Zisling's party (Mapam) is not in the coalition", ui.Q<Label>("cand-zisling-reason").text);
        Assert.IsTrue(ui.Q<Button>("appoint-golda_meir").enabledSelf);
    }

    [Test]
    public void AppointingTakesEffectAtEndTurn()
    {
        using var ui = Shown();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        cabinet.Select("finance_minister");
        UiTestKit.Press(ui.Q<Button>("appoint-golda_meir"));
        Assert.AreEqual("Ordered: Golda Meir to Minister of Finance, at End Turn", ui.Q<Label>("cabinet-status").text);
        bridge.EndTurn();
        Assert.AreEqual("golda_meir", Cabinet.Holder(C, bridge.State, "finance_minister")!.Id);
        StringAssert.EndsWith("(your appointee)", ui.Q<Label>("seat-finance_minister-holder").text);
        Assert.IsTrue(bridge.Session.Last!.Why.Any(w => w.Message == "Golda Meir appointed Minister of Finance, replacing Eliezer Kaplan"));
    }

    [Test]
    public void APortraitShowsOnlyWhenTheCatalogHasOne()
    {
        using var ui = Shown();
        var bridge = In1950();
        var catalog = ScriptableObject.CreateInstance<PortraitCatalog>();
        try
        {
            catalog.entries.Add(new PortraitCatalog.Entry { personId = "kaplan", image = Texture2D.whiteTexture });
            new CabinetPresenter(ui.Root, bridge, Text, catalog);
            var kaplan = ui.Q<Button>("seat-finance_minister").Q<VisualElement>(className: PersonCard.PortraitClass);
            Assert.AreEqual(Texture2D.whiteTexture, kaplan.style.backgroundImage.value.texture);
            Assert.AreEqual(DisplayStyle.Flex, kaplan.style.display.value);
            var pm = Cabinet.Holder(C, bridge.State, "prime_minister")!;
            // Every card but Kaplan's; the PM's other offices are listed on the PM card, not given cards of their own.
            foreach (var seat in Cabinet.Lineup(C, bridge.State).Where(x => x.Holder != null && x.Holder.Id != "kaplan" && (x.Holder.Id != pm.Id || x.Office.Id == "prime_minister")))
                Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("seat-" + seat.Office.Id).Q<VisualElement>(className: PersonCard.PortraitClass).style.display.value, "no invented portrait for " + seat.Office.Id);
        }
        finally { UnityEngine.Object.DestroyImmediate(catalog); }
    }

    [Test]
    public void EveryCandidateIsListedAndOnlyCheckDecidesWhoCanBeAppointed()
    {
        using var ui = Shown();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        foreach (var office in new[] { "finance_minister", "chief_of_staff", "president", "prime_minister" })
        {
            cabinet.Select(office);
            var list = Cabinet.Candidates(C, bridge.State, office);
            // #215: a refusal shared by every candidate (the office itself) is said once under the heading.
            var shared = list.All(x => x.reason != null && x.reason == list[0].reason) ? list[0].reason : null;
            Assert.AreEqual(office == "prime_minister" || office == "president", shared != null, office + ": an office-level refusal");
            Assert.AreEqual(shared ?? "", ui.Q<Label>("cabinet-pick-reason").text, office);
            Assert.AreEqual(shared == null ? DisplayStyle.None : DisplayStyle.Flex, ui.Q<Label>("cabinet-pick-reason").style.display.value, office);
            foreach (var (person, reason) in list)
            {
                Assert.AreEqual(Cabinet.Check(C, bridge.State, office, person.Id), reason, office + " " + person.Id);   // Candidates is Check, row by row
                Assert.AreEqual(reason == null, ui.Q<Button>("appoint-" + person.Id).enabledSelf, office + " " + person.Id);
                var row = ui.Q<Label>("cand-" + person.Id + "-reason");
                if (shared != null) Assert.AreEqual(DisplayStyle.None, row.style.display.value, office + " " + person.Id + ": not repeated");
                else if (reason != null) Assert.AreEqual(reason, row.text);
            }
        }
    }

    [Test]
    public void ARefusalAtQueueTimeIsShown()
    {
        using var ui = Shown();
        var bridge = In1950();
        new ToastPresenter(ui.Root, bridge, Text);
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        cabinet.Select("finance_minister");
        Assert.IsNull(bridge.Queue(new SetPolicy(C.Policies[0].Id, 0.5)));
        Assert.IsNull(bridge.Queue(new SetPolicy(C.Policies[1].Id, 0.5)));
        UiTestKit.Press(ui.Q<Button>("appoint-golda_meir"));
        StringAssert.StartsWith("Not done: Not enough attention", ui.Q<Label>("toast").text);
        Assert.AreEqual(2, bridge.Queued.Count);
    }

    [Test]
    public void SelectingNothingClosesTheListAndADisposedScreenHearsNothing()
    {
        using var ui = Shown();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        cabinet.Select("finance_minister");
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<VisualElement>("cabinet-pick").style.display.value);
        Assert.IsTrue(ui.Q<Button>("seat-finance_minister").ClassListContains("seat-selected"));
        cabinet.Select(null);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("cabinet-pick").style.display.value);
        cabinet.Dispose();
        ui.Q<Label>("cabinet-government").text = "untouched";
        bridge.EndTurn();
        Assert.AreEqual("untouched", ui.Q<Label>("cabinet-government").text);
    }

    /// <summary>Steps until the next turn would reach <paramref name="date"/>, so the next End Turn crosses it.</summary>
    static void StepUntilNextTurnCrosses(GameSession g, DateTime date)
    {
        while (SimTime.Advance(g.State.Date, SecuritySystem.TurnLengthFor(g.State.Rung)) < date) g.Step(HistoricalAI.Commands(C, g.State));
    }

    [Test]
    public void ANewGovernmentRefreshesTheScreenAndEndsTheAppointment()
    {
        using var ui = Shown();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        cabinet.Select("finance_minister");
        UiTestKit.Press(ui.Q<Button>("appoint-golda_meir"));
        bridge.EndTurn();
        StringAssert.EndsWith("(your appointee)", ui.Q<Label>("seat-finance_minister-holder").text);
        StepUntilNextTurnCrosses(bridge.Session, new DateTime(1950, 11, 1));
        bridge.EndTurn();
        var gov = Offices.Government(C, bridge.State.Date)!;
        Assert.AreEqual("Second Government", gov.Name);
        Assert.AreEqual(gov.Name, ui.Q<Label>("cabinet-government").text);
        var holder = Cabinet.Holder(C, bridge.State, "finance_minister")!;
        Assert.AreNotEqual("golda_meir", holder.Id);
        Assert.AreEqual(holder.Name, ui.Q<Label>("seat-finance_minister-holder").text);
        StringAssert.DoesNotContain("(your appointee)", ui.Q<Label>("seat-finance_minister-holder").text);
        Assert.IsTrue(bridge.Session.Last!.Why.Any(w => w.System == "cabinet" && w.Target == "appointments"));
    }

    [Test]
    public void ReplacingTheSessionRedrawsTheScreen()
    {
        using var ui = Shown();
        var bridge = In1950();
        new CabinetPresenter(ui.Root, bridge, Text, null);
        Assert.AreEqual("First Government", ui.Q<Label>("cabinet-government").text);
        var fresh = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        bridge.Replace(fresh);
        Assert.AreEqual(Offices.Government(C, fresh.State.Date)!.Name, ui.Q<Label>("cabinet-government").text);
        Assert.AreNotEqual("First Government", ui.Q<Label>("cabinet-government").text);
    }

    [Test]
    public void AVacantSeatSaysSoAndHasNoPortrait()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        Seat? vacant = null;
        for (int i = 0; i < 120 && g.State.GameOver == null && vacant == null; i++)
        {
            vacant = Cabinet.Lineup(C, g.State).FirstOrDefault(x => x.Holder == null);
            if (vacant == null) g.Step(HistoricalAI.Commands(C, g.State));
        }
        Assert.IsNotNull(vacant, "the historical game has a vacant seat to test with");
        using var ui = Shown();
        var catalog = ScriptableObject.CreateInstance<PortraitCatalog>();
        try
        {
            new CabinetPresenter(ui.Root, new SimBridge(g, Text), Text, catalog);
            Assert.AreEqual("(vacant)", ui.Q<Label>("seat-" + vacant!.Office.Id + "-holder").text);
            Assert.AreEqual(vacant.Office.Title, ui.Q<Label>("seat-" + vacant.Office.Id + "-office").text);
            Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("seat-" + vacant.Office.Id).Q<VisualElement>(className: PersonCard.PortraitClass).style.display.value);
            Assert.AreEqual("", ui.Q<Label>("seat-" + vacant.Office.Id + "-initials").text, "a vacancy has no initials either");
        }
        finally { UnityEngine.Object.DestroyImmediate(catalog); }
    }

    [Test]
    public void ASelectedOfficeThatDisappearsIsDeselected()
    {
        using var ui = Shown();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        var ending = C.Offices.Where(o => o.Until.HasValue && o.Until > bridge.State.Date).OrderBy(o => o.Until).First();
        cabinet.Select(ending.Id);
        Assert.AreEqual(ending.Id, cabinet.Selected);
        while (bridge.State.Date < ending.Until!.Value && bridge.State.GameOver == null) bridge.EndTurn();
        Assert.IsNull(cabinet.Selected);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("cabinet-pick").style.display.value);
        Assert.IsNull(ui.Root.Q<Button>("seat-" + ending.Id));
    }

    [Test]
    public void DisposeHidesThePickPanel()
    {
        using var ui = Shown();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        cabinet.Select("finance_minister");
        cabinet.Dispose();
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("cabinet-pick").style.display.value);
    }
}
