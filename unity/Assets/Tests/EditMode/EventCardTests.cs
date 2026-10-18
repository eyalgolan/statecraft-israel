using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class EventCardTests
{
    const string CatalogPath = "Assets/UI/Portraits/PortraitCatalog.asset";
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>The historical game on the turn the Egyptian armistice first waits for a decision (February 1949).</summary>
    static SimBridge AtArmistice()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        return new SimBridge(g, Text);
    }

    [Test]
    public void ThePortraitCatalogHoldsEveryPublicDomainPortrait()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
        Assert.IsNotNull(catalog, CatalogPath + " (run PortraitImport.Run)");
        var expected = C.People.Where(p => p.Portrait != null && p.Portrait.File != "").Select(p => p.Id).OrderBy(x => x).ToArray();
        Assert.AreEqual(expected, catalog.entries.Select(e => e.personId).OrderBy(x => x).ToArray());
        Assert.IsNotNull(catalog.Get("ben_gurion"));
        Assert.IsNotNull(catalog.Get("golda_meir"));
        Assert.IsNull(catalog.Get("geri"));                           // no portrait recorded
    }

    [Test]
    public void TheCardShowsThePendingEventWithTheMinisterAndOptions()
    {
        using var ui = new UiTestKit();
        var bridge = AtArmistice();
        var catalog = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
        var card = new EventCardPresenter(ui.Root, bridge, Text, catalog);
        Assert.AreEqual("armistice_egypt", card.Shown!.Id);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<VisualElement>("event-card").style.display.value);
        Assert.AreEqual("Armistice with Egypt", ui.Q<Label>("event-title").text);
        Assert.AreEqual("Prime Minister: David Ben-Gurion", ui.Q<Label>("event-office").text);
        StringAssert.Contains("David Ben-Gurion must decide whether Israel signs.", ui.Q<Label>("event-text").text);
        Assert.AreEqual(catalog.Get("ben_gurion"), ui.Q<VisualElement>("event-image").style.backgroundImage.value.texture);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("event-draft").style.display.value);
        Assert.AreEqual("Sign the armistice", ui.Q<Button>("option-sign").text);
    }

    [Test]
    public void InHebrewTheOfficeLineUsesTheFeminineTitleAndHebrewName()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        var c = ContentLoader.Load(Paths.Content);
        var e = c.Events.First(x => x.Id == "armistice_egypt");
        e.Office = "labour_minister";
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        g.State.Date = new System.DateTime(1950, 2, 1);
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(e.Id);
        using var ui = new UiTestKit();
        new EventCardPresenter(ui.Root, new SimBridge(g, he), he, null);
        Assert.AreEqual("שרת העבודה: גולדה מאיר", ui.Q<Label>("event-office").text);
    }

    [Test]
    public void AMinisterWithoutAPortraitShowsNoImage()
    {
        using var ui = new UiTestKit();
        var bridge = AtArmistice();
        var real = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
        var catalog = Object.Instantiate(real);                       // the real catalog; its entries are edited between renders
        try
        {
            var card = new EventCardPresenter(ui.Root, bridge, Text, catalog);
            var image = ui.Q<VisualElement>("event-image");
            Assert.IsNotNull(catalog.Get("ben_gurion"));
            Assert.AreEqual(catalog.Get("ben_gurion"), image.style.backgroundImage.value.texture, "with the portrait, the texture is set");
            Assert.AreEqual(DisplayStyle.Flex, image.style.display.value);

            catalog.entries.RemoveAll(e => e.personId == "ben_gurion");
            Assert.Greater(catalog.entries.Count, 0);
            card.Render();
            // Unity reads a background set to StyleKeyword.None back as Null (verified in a probe), so Null plus no texture is the observable "cleared".
            Assert.IsNull(image.style.backgroundImage.value.texture, "the texture is gone");
            Assert.AreEqual(StyleKeyword.Null, image.style.backgroundImage.keyword);
            Assert.AreEqual(DisplayStyle.None, image.style.display.value);
        }
        finally { Object.DestroyImmediate(catalog); }
    }

    [UnityTest]
    public System.Collections.IEnumerator TheTitleIsCentred()
    {
        using var ui = new UiTestKit();
        var bridge = AtArmistice();
        new EventCardPresenter(ui.Root, bridge, Text, null);
        for (int i = 0; i < 10 && ui.Q<Label>("event-title").resolvedStyle.unityTextAlign == TextAnchor.UpperLeft; i++) yield return null;
        Assert.AreEqual(TextAnchor.MiddleCenter, ui.Q<Label>("event-title").resolvedStyle.unityTextAlign);
    }

    [Test]
    public void DraftContentIsStamped()
    {
        using var ui = new UiTestKit();
        var bridge = AtArmistice();
        var ev = C.Events.First(e => e.Id == "armistice_egypt");
        var old = ev.Status;
        try
        {
            ev.Status = "draft";
            new EventCardPresenter(ui.Root, bridge, Text, null);
            Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("event-draft").style.display.value);
            Assert.AreEqual("DRAFT", ui.Q<Label>("event-draft").text);
        }
        finally { ev.Status = old; }
    }

    [Test]
    public void ADisposedCardHearsNothing()
    {
        using var ui = new UiTestKit();
        var bridge = AtArmistice();
        var card = new EventCardPresenter(ui.Root, bridge, Text, null);
        Assert.IsNotNull(card.Shown);
        card.Dispose();
        ui.Q<Label>("event-title").text = "untouched";
        bridge.Queue(new SecurityOrder(-1));
        bridge.EndTurn();
        Assert.AreEqual("untouched", ui.Q<Label>("event-title").text);
        Assert.IsNull(ui.Root.Q<Button>("option-sign"), "option buttons are removed with the card");
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-card").style.display.value, "a disposed card is hidden");
    }

    [Test]
    public void ChoosingQueuesTheAnswerAndTheTurnAppliesIt()
    {
        using var ui = new UiTestKit();
        var bridge = AtArmistice();
        var card = new EventCardPresenter(ui.Root, bridge, Text, null);
        UiTestKit.Press(ui.Q<Button>("option-sign"));
        var queued = (ChooseEventOption)bridge.Queued.Single();
        Assert.AreEqual(("armistice_egypt", "sign"), (queued.EventId, queued.OptionId));
        Assert.AreNotEqual("armistice_egypt", card.Shown?.Id);           // the card moves on
        bridge.EndTurn();
        Assert.AreEqual(2, bridge.State.Rung);                            // signing ends full war
        Assert.IsTrue(bridge.Session.Last!.Why.Any(w => w.Message == "Chosen: Sign the armistice"));
    }

    // Fix round 1, I3: the 1949 elections are a one-option card; it renders one button, and pressing it answers the event.
    [Test]
    public void AOneOptionEventShowsOneButton()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("elections_1949")) g.Step(new Command[0]);
        // On monthly turns elections_1949 comes on 15 January 1949 with sinai_withdrawal and rhodes_talks; this test is about the one-option card.
        g.State.PendingEvents.RemoveAll(id => id != "elections_1949");
        var bridge = new SimBridge(g, Text);
        var card = new EventCardPresenter(ui.Root, bridge, Text, null);
        Assert.AreEqual("elections_1949", card.Shown!.Id);
        var buttons = ui.Q<VisualElement>("event-options").Query<Button>().ToList();
        Assert.AreEqual(1, buttons.Count);
        Assert.AreEqual("option-hold", buttons[0].name);
        UiTestKit.Press(buttons[0]);
        var queued = (ChooseEventOption)bridge.Queued.Single();
        Assert.AreEqual(("elections_1949", "hold"), (queued.EventId, queued.OptionId));
    }

    // Owner ruling 2026-10-11: the promo's header picture is a real archival photograph with its credit line under it.
    const string PhotosPath = "Assets/Resources/EventImageCatalog.asset";

    /// <summary>The historical game with only this event pending, on its first day (as UiLayoutTests does).</summary>
    static SimBridge Showing(string eventId)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.Date = C.Events.First(e => e.Id == eventId).From;
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(eventId);
        return new SimBridge(g, Text);
    }

    [Test]
    public void AnEventWithAPhotographShowsItWithItsCreditAndAltText()
    {
        using var ui = new UiTestKit();
        var photos = AssetDatabase.LoadAssetAtPath<EventImageCatalog>(PhotosPath);
        var portraits = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
        var card = new EventCardPresenter(ui.Root, AtArmistice(), Text, portraits, photos);
        var entry = photos.Find("armistice_egypt")!.Value;
        var band = ui.Q<VisualElement>("event-photo");
        Assert.AreEqual(entry.image, band.style.backgroundImage.value.texture);
        Assert.AreEqual(DisplayStyle.Flex, band.style.display.value);
        Assert.AreEqual(entry.alt, band.tooltip, "the alt text");
        Assert.AreEqual(entry.alt, card.PhotoAlt);
        var credit = ui.Q<Label>("event-photo-credit");
        Assert.AreEqual(DisplayStyle.Flex, credit.style.display.value);
        Assert.AreEqual(entry.credit, credit.text);
        StringAssert.Contains("Benno Rothenberg", credit.text);
        StringAssert.Contains("CC BY 4.0", credit.text);
        StringAssert.Contains("graded and cropped", credit.text);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-image").style.display.value, "the photograph replaces the portrait");
        Assert.AreEqual("Prime Minister: David Ben-Gurion", ui.Q<Label>("event-office").text, "the minister is still named");
    }

    [Test]
    public void AnEventWithoutAPhotographFallsBackToTheMinistersPortrait()
    {
        using var ui = new UiTestKit();
        var photos = AssetDatabase.LoadAssetAtPath<EventImageCatalog>(PhotosPath);
        var portraits = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
        Assert.IsNull(photos.Find("armistice_lebanon"), "no free photograph was found for it");
        var card = new EventCardPresenter(ui.Root, Showing("armistice_lebanon"), Text, portraits, photos);
        Assert.AreEqual("armistice_lebanon", card.Shown!.Id);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-photo").style.display.value);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("event-photo-credit").style.display.value);
        Assert.AreEqual("", ui.Q<Label>("event-photo-credit").text);
        Assert.IsNull(card.PhotoAlt);
        Assert.AreEqual(portraits.Get("ben_gurion"), ui.Q<VisualElement>("event-image").style.backgroundImage.value.texture);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<VisualElement>("event-image").style.display.value);
    }

    [Test]
    public void ThePhotographGoesWhenTheNextEventHasNone()
    {
        using var ui = new UiTestKit();
        var real = AssetDatabase.LoadAssetAtPath<EventImageCatalog>(PhotosPath);
        var photos = Object.Instantiate(real);                          // the real catalog; its entries are edited between renders
        try
        {
            var portraits = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
            var card = new EventCardPresenter(ui.Root, AtArmistice(), Text, portraits, photos);
            Assert.AreEqual(DisplayStyle.Flex, ui.Q<VisualElement>("event-photo").style.display.value);
            photos.entries.RemoveAll(e => e.eventId == "armistice_egypt");
            card.Render();
            Assert.IsNull(ui.Q<VisualElement>("event-photo").style.backgroundImage.value.texture, "the texture is gone");
            Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-photo").style.display.value);
            Assert.AreEqual("", ui.Q<VisualElement>("event-photo").tooltip);
            Assert.AreEqual(portraits.Get("ben_gurion"), ui.Q<VisualElement>("event-image").style.backgroundImage.value.texture, "the portrait is back");
        }
        finally { Object.DestroyImmediate(photos); }
    }

    [Test]
    public void WithNoPhotoCatalogTheCardKeepsThePortrait()
    {
        using var ui = new UiTestKit();
        var portraits = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
        new EventCardPresenter(ui.Root, AtArmistice(), Text, portraits);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-photo").style.display.value);
        Assert.AreEqual(portraits.Get("ben_gurion"), ui.Q<VisualElement>("event-image").style.backgroundImage.value.texture);
    }
    /// <summary>A screenshot run (?shot, -shot) captures the map, so the opening card (One Army, waiting on every new game
    /// since the GDD cutover) must not cover it, as the units are left out (UnitLayer.HiddenInCaptures). A smoke run
    /// presses the card's options, so it keeps the card even with shot; every other run, normal play first, shows it.</summary>
    [Test]
    public void AScreenshotRunHidesTheCardAndOnlyIt()
    {
        Assert.IsTrue(EventCardPresenter.HiddenInCaptures("https://x/?shot", new string[0], true));
        Assert.IsTrue(EventCardPresenter.HiddenInCaptures("https://x/?golden&shot", new string[0], true), "tools/art_checkpoint.sh's query");
        Assert.IsTrue(EventCardPresenter.HiddenInCaptures("https://x/?golden&shot&units", new string[0], true), "the unit slice's shots too");
        Assert.IsTrue(EventCardPresenter.HiddenInCaptures(null, new[] { "player", "-shot" }, false));
        Assert.IsFalse(EventCardPresenter.HiddenInCaptures("https://x/", new string[0], true), "normal play shows the card");
        Assert.IsFalse(EventCardPresenter.HiddenInCaptures(null, new string[0], true), "no URL: normal play");
        Assert.IsFalse(EventCardPresenter.HiddenInCaptures(null, new[] { "player" }, false));
        Assert.IsFalse(EventCardPresenter.HiddenInCaptures("https://x/?golden", new string[0], true));
        Assert.IsFalse(EventCardPresenter.HiddenInCaptures("https://x/?vprofile", new string[0], true), "the frame profile prices the card");
        Assert.IsFalse(EventCardPresenter.HiddenInCaptures("https://x/?screenshot=1", new string[0], true), "a whole parameter only");
        Assert.IsFalse(EventCardPresenter.HiddenInCaptures("https://x/?smoke=5&shot", new string[0], true), "a smoke run presses the card");
        Assert.IsFalse(EventCardPresenter.HiddenInCaptures(null, new[] { "player", "-smoke", "5", "-shot" }, false));
    }

    [Test]
    public void AHiddenCardLeavesTheMapClearAndANormalOneCoversIt()
    {
        foreach (var hidden in new[] { true, false })
        {
            using var ui = new UiTestKit();
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            var card = new EventCardPresenter(ui.Root, bridge, Text, null, hiddenInCapture: hidden);
            var hud = ui.Root.Q("root") ?? ui.Root;
            Assert.AreEqual("idf_founding", card.Shown?.Id, "a new game opens on One Army either way: the sim is untouched");
            Assert.AreEqual(hidden ? DisplayStyle.None : DisplayStyle.Flex, ui.Q<VisualElement>("event-card").style.display.value, $"hidden {hidden}");
            Assert.AreEqual(!hidden, hud.ClassListContains(EventCardPresenter.OpenClass), $"the scrim, hidden {hidden}");
            Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("event-reopen").style.display.value, "no Decision waiting button");
            card.Render();                                                // a later render (any bridge change) keeps it so
            Assert.AreEqual(hidden ? DisplayStyle.None : DisplayStyle.Flex, ui.Q<VisualElement>("event-card").style.display.value, $"re-rendered, hidden {hidden}");
            Assert.AreEqual(!hidden, hud.ClassListContains(EventCardPresenter.OpenClass));
            card.Dispose();
        }
    }

    // ---- The objective card (#380): its line and its photograph ----

    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    /// <summary>The first new yishuv's card, recorded as completed on <paramref name="done"/> (R-OBJ-001), pending.</summary>
    static SimBridge ObjectiveDone(System.DateTime done, UiText text)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.Date = done;
        g.State.Objectives["first_new_yishuv"] = done;
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add("first_new_yishuv");
        return new SimBridge(g, text);
    }

    /// <summary>The objective card's line under its text, in the owner's wording (#379, 14 October 2026), against Mefalsim's
    /// reference date of 12 June 1949 (objectives.yaml), by calendar months (Objectives.MonthsFromHistory): early with both
    /// dates and the months, on time in the same month, later with the months; one month in the singular.</summary>
    [Test]
    public void TheObjectiveCardSaysWhenItWasCompletedAgainstHistory()
    {
        Assert.AreEqual(new System.DateTime(1949, 6, 12), C.Objectives.Single(o => o.Id == "first_new_yishuv").ReferenceDate, "precondition");
        foreach (var (done, line) in new[]
        {
            (new System.DateTime(1949, 3, 15), "Completed on 15 March 1949; historically 12 June 1949; 3 months early."),
            (new System.DateTime(1949, 5, 31), "Completed on 31 May 1949; historically 12 June 1949; 1 month early."),
            (new System.DateTime(1949, 6, 1), "Completed on time, as in history."),
            (new System.DateTime(1949, 6, 30), "Completed on time, as in history."),
            (new System.DateTime(1949, 7, 1), "Completed 1 month later than in history."),
            (new System.DateTime(1950, 2, 15), "Completed 8 months later than in history."),
        })
        {
            using var ui = new UiTestKit();
            using var card = new EventCardPresenter(ui.Root, ObjectiveDone(done, Text), Text, null);
            Assert.AreEqual("first_new_yishuv", card.Shown!.Id);
            var label = ui.Q<Label>("event-objective");
            Assert.AreEqual(line, label.text, done.ToString("yyyy-MM-dd"));
            Assert.AreEqual(DisplayStyle.Flex, label.style.display.value, done.ToString("yyyy-MM-dd"));
        }
    }

    /// <summary>The same line in Hebrew: Hebrew months (date.month_*), the values between left-to-right marks (#543).</summary>
    [Test]
    public void TheObjectiveCardsLineIsInHebrewInTheHebrewUi()
    {
        foreach (var (done, line) in new[]
        {
            (new System.DateTime(1949, 3, 15), "הושלם ב־15 במרץ 1949; בהיסטוריה ב־12 ביוני 1949; מוקדם ב־3 חודשים."),
            (new System.DateTime(1949, 5, 31), "הושלם ב־31 במאי 1949; בהיסטוריה ב־12 ביוני 1949; מוקדם בחודש אחד."),
            (new System.DateTime(1949, 6, 30), "הושלם בזמן, כמו בהיסטוריה."),
            (new System.DateTime(1949, 7, 1), "הושלם באיחור של חודש אחד לעומת ההיסטוריה."),
            (new System.DateTime(1950, 2, 15), "הושלם באיחור של 8 חודשים לעומת ההיסטוריה."),
        })
        {
            using var ui = new UiTestKit();
            using var card = new EventCardPresenter(ui.Root, ObjectiveDone(done, He), He, null);
            var shown = ui.Q<Label>("event-objective").text;
            Assert.AreEqual(line, shown.Replace("\u200E", ""), done.ToString("yyyy-MM-dd"));
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(shown, "[A-Za-z]"), $"English in the Hebrew line: {shown}");
        }
    }

    /// <summary>Every other card has no objective line, and takes no room for one.</summary>
    [Test]
    public void ACardThatIsNoObjectiveHasNoObjectiveLine()
    {
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, AtArmistice(), Text, null);
        Assert.AreEqual("armistice_egypt", card.Shown!.Id);
        Assert.AreEqual("", ui.Q<Label>("event-objective").text);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("event-objective").style.display.value);
    }

    /// <summary>The objective card shows Mefalsim's photograph (Boris Carmi, June 1949, CC BY 4.0) with its credit, in place
    /// of a portrait (the event has no office).</summary>
    [Test]
    public void TheObjectiveCardShowsItsPhotographWithItsCredit()
    {
        using var ui = new UiTestKit();
        var photos = AssetDatabase.LoadAssetAtPath<EventImageCatalog>(PhotosPath);
        var entry = photos.Find("first_new_yishuv");
        Assert.IsNotNull(entry, "the catalog has the card's photograph (EventImageImport.Run)");
        using var card = new EventCardPresenter(ui.Root, ObjectiveDone(new System.DateTime(1949, 3, 15), Text), Text, null, photos);
        var band = ui.Q<VisualElement>("event-photo");
        Assert.AreEqual(entry!.Value.image, band.style.backgroundImage.value.texture);
        Assert.AreEqual(DisplayStyle.Flex, band.style.display.value);
        Assert.AreEqual(entry.Value.alt, card.PhotoAlt);
        var credit = ui.Q<Label>("event-photo-credit").text;
        StringAssert.StartsWith("Boris Carmi / Meitar Collection", credit);
        StringAssert.EndsWith("CC BY 4.0 · graded and cropped", credit);
    }
}
