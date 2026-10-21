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
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

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

    /// <summary>In Hebrew the screen shows the content's Hebrew names (#558): people, office titles (in the feminine for a
    /// woman who holds the office, Office.TitleHeFor), parties, the government and the legislature's body.</summary>
    [Test]
    public void InHebrewTheScreenShowsTheContentsHebrewNames()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        using var ui = Shown();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new DateTime(1950, 2, 1)) g.Step(HistoricalAI.Commands(C, g.State));
        var bridge = new SimBridge(g, he);
        var cabinet = new CabinetPresenter(ui.Root, bridge, he, null);
        Assert.AreEqual("ממשלת ישראל הראשונה", ui.Q<Label>("cabinet-government").text);
        StringAssert.Contains(Offices.Legislature(C, g.State.Date)!.BodyHe, ui.Q<Label>("cabinet-legislature").text);
        Assert.AreEqual("אליעזר קפלן", ui.Q<Label>("seat-finance_minister-holder").text);
        Assert.AreEqual("שר האוצר", ui.Q<Label>("seat-finance_minister-office").text);
        Assert.AreEqual("מפא״י", ui.Q<Label>("seat-finance_minister-party").text);
        Assert.AreEqual("גולדה מאיר", ui.Q<Label>("seat-labour_minister-holder").text);
        Assert.AreEqual("שרת העבודה", ui.Q<Label>("seat-labour_minister-office").text, "the feminine title for her");
        var pm = Cabinet.Holder(C, g.State, CabinetPresenter.PrimeMinister)!;
        Assert.AreEqual(pm.NameHe, ui.Q<Label>("seat-" + CabinetPresenter.PrimeMinister + "-holder").text);
        StringAssert.StartsWith("מפא״י ", ui.Q<Label>("party-mapai-label").text);
        StringAssert.Contains("מפא״י", ui.Q<Label>("cabinet-largest").text);
        cabinet.Select("finance_minister");
        Assert.AreEqual("מינוי לתפקיד שר האוצר", ui.Q<Label>("cabinet-pick-title").text);
        StringAssert.StartsWith("גולדה מאיר · מפא״י · ", ui.Q<Button>("appoint-golda_meir").text);
        StringAssert.StartsWith("אהרון ציזלינג · מפ״ם · ", ui.Q<Button>("appoint-zisling").text);
        UiTestKit.Press(ui.Q<Button>("appoint-golda_meir"));
        // She never held Finance; her order reads the office's title_he_f row all the same (owner on #861: "Move to content").
        Assert.AreEqual("שרת האוצר", C.Offices.Single(o => o.Id == "finance_minister").TitleHeF);
        Assert.AreEqual("ניתנה פקודה: גולדה מאיר לתפקיד שרת האוצר, בסיום התור", ui.Q<Label>("cabinet-status").text);
        // A row with no Hebrew form in the content keeps its English one: the rationing office has no title_he yet.
        var rationing = C.Offices.Single(o => o.Id == "rationing_minister");
        Assert.AreEqual("", rationing.TitleHe);
        Assert.AreEqual(rationing.Title, ui.Q<Label>("seat-rationing_minister-office").text);
        // Every other card and party chip is in Hebrew.
        foreach (var t in ui.Q<VisualElement>("screen-cabinet").Query<TextElement>().ToList())
            if (t.name != null && t.name != "seat-rationing_minister-office"
                && (t.name.EndsWith("-holder") || t.name.EndsWith("-office") || t.name.EndsWith("-party") || t.name.StartsWith("party-")))
                Assert.IsFalse((t.text ?? "").Any(ch => ch < 128 && char.IsLetter(ch)), $"'{t.name}': {t.text}");
        cabinet.Dispose();
    }

    /// <summary>A woman's title is the office's title_he_f row, also for an office she never held (owner on #861, 17 October
    /// 2026: "Move to content"; the wording is Beri's, native speaker): a minister's title takes the feminine, the Prime
    /// Minister's stays "ראש הממשלה". The UI derives no form: an office with no title_he_f keeps its English title for her.
    /// A man's title and the English UI are unchanged.</summary>
    [Test]
    public void AWomansMinisterTitleIsFeminineAndThePrimeMinistersIsNot()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        var golda = C.People.Single(p => p.Id == "golda_meir");
        Office O(string id) => C.Offices.Single(o => o.Id == id);
        foreach (var o in C.Offices.Where(o => o.TitleHeF != ""))
            Assert.AreEqual(o.TitleHeF, ContentNames.Of(he, o, golda), o.Id);
        var unsourced = new Office { Title = "Minister of Finance", TitleHe = "שר האוצר" };
        Assert.AreEqual("Minister of Finance", ContentNames.Of(he, unsourced, golda), "no title_he_f: English, no derived form");
        Assert.AreEqual("שרת האוצר", ContentNames.Of(he, O("finance_minister"), golda));
        Assert.AreEqual("השרה לנפגעי מלחמה", ContentNames.Of(he, O("war_victims_minister"), golda));
        Assert.AreEqual("שרה בלי תיק", ContentNames.Of(he, O("minister_without_portfolio"), golda));
        Assert.AreEqual("ראש הממשלה", ContentNames.Of(he, O("prime_minister"), golda));
        Assert.AreEqual("שרת העבודה", ContentNames.Of(he, O("labour_minister"), golda));
        var own = new Office { Title = "Minister of Labour", TitleHe = "שר העבודה", TitleHeF = "שרת העבודה והרווחה" };
        Assert.AreEqual("שרת העבודה והרווחה", ContentNames.Of(he, own, golda), "the content's title_he_f wins");
        Assert.AreEqual("שר האוצר", ContentNames.Of(he, O("finance_minister"), C.People.Single(p => p.Id == "kaplan")));
        Assert.AreEqual("Minister of Finance", ContentNames.Of(Text, O("finance_minister"), golda));
        Assert.AreEqual(O("rationing_minister").Title, ContentNames.Of(he, O("rationing_minister"), golda), "no title_he: English");
    }

    /// <summary>#901: candidate refusal arguments are content ids from the sim; the Hebrew UI resolves and formats them.</summary>
    [Test]
    public void ARefusedCandidateHasAHebrewReasonInTheHebrewUi()
    {
        using var ui = Shown();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new DateTime(1950, 2, 1)) g.Step(HistoricalAI.Commands(C, g.State));
        var cabinet = new CabinetPresenter(ui.Root, new SimBridge(g, He), He, null);
        cabinet.Select("finance_minister");
        Assert.AreEqual("אהרון ציזלינג ממפלגת מפ״ם, שאינה בקואליציה", ui.Q<Label>("cand-zisling-reason").text);
    }

    [TestCase(true, "labour_minister", "golda_meir", "גולדה מאיר כבר בתפקיד שרת העבודה")]
    [TestCase(false, "labour_minister", "golda_meir", "Golda Meir already holds the office of Minister of Labour")]
    [TestCase(true, "finance_minister", "kaplan", "אליעזר קפלן כבר בתפקיד שר האוצר")]
    [TestCase(false, "finance_minister", "kaplan", "Eliezer Kaplan already holds the office of Minister of Finance")]
    public void AlreadyHeldOfficeUsesTheHoldersTitle(bool hebrew, string office, string person, string expected)
    {
        using var ui = Shown();
        var text = hebrew ? He : Text;
        var bridge = new SimBridge(In1950().Session, text);
        using var cabinet = new CabinetPresenter(ui.Root, bridge, text, null);
        cabinet.Select(office);
        Assert.AreEqual("already_holds", Cabinet.CheckReason(C, bridge.State, office, person)!.Kind);
        Assert.IsFalse(ui.Q<Button>("appoint-" + person).enabledSelf);
        Assert.AreEqual(expected, ui.Q<Label>("cand-" + person + "-reason").text);
    }

    [Test]
    public void AppointingTakesEffectAtEndTurn()
    {
        using var ui = Shown();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        cabinet.Select("finance_minister");
        UiTestKit.Press(ui.Q<Button>("appoint-golda_meir"));
        Assert.AreEqual("Ordered: Golda Meir to Minister of Finance, at End turn", ui.Q<Label>("cabinet-status").text);
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
            var shared = list.All(x => x.reason != null && x.reason.Text == list[0].reason!.Text) ? list[0].reason : null;
            Assert.AreEqual(office == "prime_minister" || office == "president", shared != null, office + ": an office-level refusal");
            Assert.AreEqual(shared?.Text ?? "", ui.Q<Label>("cabinet-pick-reason").text, office);
            Assert.AreEqual(shared == null ? DisplayStyle.None : DisplayStyle.Flex, ui.Q<Label>("cabinet-pick-reason").style.display.value, office);
            foreach (var (person, reason) in list)
            {
                Assert.AreEqual(Cabinet.Check(C, bridge.State, office, person.Id), reason?.Text, office + " " + person.Id);   // Candidates is CheckReason, row by row
                Assert.AreEqual(reason == null, ui.Q<Button>("appoint-" + person.Id).enabledSelf, office + " " + person.Id);
                var row = ui.Q<Label>("cand-" + person.Id + "-reason");
                if (shared != null) Assert.AreEqual(DisplayStyle.None, row.style.display.value, office + " " + person.Id + ": not repeated");
                else if (reason != null) Assert.AreEqual(reason.Text, row.text);
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
        // The content's turn lengths (monthly since the GDD cutover), not the legacy table.
        while (SimTime.Advance(g.State.Date, SecuritySystem.TurnLengthFor(C, g.State.Date, g.State.Rung)) < date) g.Step(HistoricalAI.Commands(C, g.State));
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

    /// <summary>#277: Minister without Portfolio had two holders from 24 December 1952 to 15 June 1953 (Yosef and Lavon).
    /// The screen gives each a card, and selecting the office says once why nobody can be appointed to it.</summary>
    [Test]
    public void AnOfficeWithSeveralHoldersGetsACardForEach()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.Date = new DateTime(1953, 3, 1);
        using var ui = Shown();
        var cabinet = new CabinetPresenter(ui.Root, new SimBridge(g, Text), Text, null);
        Assert.AreEqual("Dov Yosef", ui.Q<Label>("seat-minister_without_portfolio-holder").text);
        Assert.AreEqual("Pinhas Lavon", ui.Q<Label>("seat-minister_without_portfolio-2-holder").text);
        Assert.AreEqual("Minister without Portfolio", ui.Q<Label>("seat-minister_without_portfolio-2-office").text);
        UiTestKit.Press(ui.Q<Button>("seat-minister_without_portfolio-2"));
        Assert.AreEqual("minister_without_portfolio", cabinet.Selected);
        Assert.IsTrue(ui.Q<Button>("seat-minister_without_portfolio").ClassListContains("seat-selected"));
        Assert.IsTrue(ui.Q<Button>("seat-minister_without_portfolio-2").ClassListContains("seat-selected"));
        StringAssert.Contains("several holders", ui.Q<Label>("cabinet-pick-reason").text);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("cabinet-pick-reason").style.display.value);

        g.State.Date = new DateTime(1953, 7, 1);   // Yosef has moved to Development: one holder, one card
        cabinet.Render();
        Assert.AreEqual("Pinhas Lavon", ui.Q<Label>("seat-minister_without_portfolio-holder").text);
        Assert.IsNull(ui.Root.Q<Button>("seat-minister_without_portfolio-2"));
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

    /// <summary>eras.yaml systems.legislature (#58): in an assembly era the screen says the body's votes do not bind; in a
    /// state era it is the body the government answers to. Played on a copy of the content with the switch set.</summary>
    [Test]
    public void TheLegislatureLineFollowsTheErasSwitch()
    {
        var c = ContentLoader.Load(Paths.Content);
        using var ui = Shown();
        var cabinet = new CabinetPresenter(ui.Root, new SimBridge(GameSession.New(c, GoldenRun.Seed, Mode.Historical), Text), Text, null);
        var leg = Offices.Legislature(c, c.Rules.Start)!;
        Assert.AreEqual($"Answerable to the {leg.Body} · {leg.Size} members", ui.Q<Label>("cabinet-legislature").text);
        c.Eras[0].Systems.Legislature = EraSystems.Assembly;
        cabinet.Render();
        Assert.AreEqual($"Consults the {leg.Body} · {leg.Size} members · its votes do not bind", ui.Q<Label>("cabinet-legislature").text);
        cabinet.Dispose();
    }

    [Test]
    public void TheHouseColoursTheCoalitionOfTheDay()
    {
        // Agudat Yisrael and Poalei Agudat Yisrael leave the Third Government on 18 September 1952 (#278).
        var leg = Offices.Legislature(C, new DateTime(1952, 9, 18))!;
        var gov = Offices.Government(C, new DateTime(1952, 9, 18))!;
        bool InCoalition(DateTime d, string party) => CabinetPresenter.Blocks(C, leg, gov, d).Single(b => b.Party == party).Coalition;
        Assert.IsTrue(InCoalition(new DateTime(1952, 9, 17), "agudat_yisrael"));
        Assert.IsFalse(InCoalition(new DateTime(1952, 9, 18), "agudat_yisrael"));
        Assert.IsFalse(InCoalition(new DateTime(1952, 9, 18), "poalei_agudat_yisrael"));
        Assert.AreEqual(60, CabinetPresenter.Blocks(C, leg, gov, new DateTime(1952, 9, 18)).Where(b => b.Coalition).Sum(b => b.Seats));
    }
}
