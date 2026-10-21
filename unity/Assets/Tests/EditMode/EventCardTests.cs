using System.Collections.Generic;
using System.IO;
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
        Assert.AreEqual("Sign the armistice (costs 2.0 Attention)", ui.Q<Button>("option-sign").text);   // its price shows (#1034)
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

    /// <summary>The Altalena card pending on 15 June 1948 (its window opens), shown in <paramref name="text"/>'s language.</summary>
    static (UiTestKit ui, GameEvent e) Altalena(UiText text, System.Action<GameEvent> edit = null, System.Action<GameState> state = null)
    {
        var c = ContentLoader.Load(Paths.Content);
        var e = c.Events.First(x => x.Id == "altalena");
        edit?.Invoke(e);
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        g.State.Date = new System.DateTime(1948, 6, 15);
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(e.Id);
        state?.Invoke(g.State);
        var ui = new UiTestKit();
        new EventCardPresenter(ui.Root, new SimBridge(g, text), text, null);
        return (ui, e);
    }

    /// <summary>#1064: in Hebrew the card shows the event's title_he, text_he and options' text_he, with the minister's
    /// Hebrew name in the placeholder.</summary>
    [Test]
    public void InHebrewTheCardShowsTheEventsHebrewText()
    {
        var (ui, e) = Altalena(He);
        using (ui)
        {
            Assert.AreEqual("אלטלנה", ui.Q<Label>("event-title").text);
            Assert.AreEqual(e.TextHe.Replace("{defense_minister}", "דוד בן-גוריון"), ui.Q<Label>("event-text").text);
            foreach (var o in e.Options) StringAssert.Contains(o.TextHe, ui.Q<Button>("option-" + o.Id).text);
        }
    }

    /// <summary>#1064: a Hebrew field that is empty falls back to the English, field by field (fail open); the English UI
    /// never shows the Hebrew.</summary>
    [Test]
    public void AMissingHebrewFieldFallsBackToEnglishAndTheEnglishUiKeepsEnglish()
    {
        var (ui, e) = Altalena(He, x => { x.TitleHe = ""; x.Options.First(o => o.Id == "compromise").TextHe = ""; });
        using (ui)
        {
            Assert.AreEqual("The Altalena", ui.Q<Label>("event-title").text);
            StringAssert.StartsWith("אלטלנה, ", ui.Q<Label>("event-text").text);
            StringAssert.Contains("Grant both requests, arming Irgun battalions inside the IDF", ui.Q<Button>("option-compromise").text);
            StringAssert.Contains(e.Options.First(o => o.Id == "enforce").TextHe, ui.Q<Button>("option-enforce").text);
        }
        var (en, _) = Altalena(Text);
        using (en)
        {
            Assert.AreEqual("The Altalena", en.Q<Label>("event-title").text);
            StringAssert.EndsWith("David Ben-Gurion must decide.", en.Q<Label>("event-text").text);
        }
    }

    /// <summary>#1064: a variant (R-ALT-010) has no Hebrew, so on a road not taken the card keeps the variant's English
    /// text; a variant with no title of its own keeps the event's title, so that stays Hebrew.</summary>
    [Test]
    public void InHebrewAVariantKeepsItsEnglishText()
    {
        var when = new EventRequirement { Event = "probe", Option = "other" };
        var (ui, _) = Altalena(He, x => x.Variants.Add(new EventVariant { When = { when }, Text = "The course differs." }),
            s => s.ChosenOptions["probe"] = "other");
        using (ui)
        {
            Assert.AreEqual("אלטלנה", ui.Q<Label>("event-title").text);
            Assert.AreEqual("The course differs.", ui.Q<Label>("event-text").text);
        }
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

    // ---- Too little Attention to answer (#1034, #1043): the card says End Turn hands it to the minister ----

    /// <summary>The players' report (#1034, #1043): The Second Truce on 15 July 1948 with the turn's Attention spent. Each
    /// option shows its price and is greyed out, the card names the minister End Turn hands it to (the one
    /// EventSystem.Delegate names), and End Turn is highlighted; with Attention back, all of that goes away.</summary>
    [TestCase("en", "You don't have enough Attention left to answer this turn. End the turn and the decision goes to David Ben-Gurion.", "Comply with the cease-fire (costs 2.0 Attention)")]
    [TestCase("he", "לא נשארה לך מספיק תשומת לב לענות בתור הזה. אפשר לסיים את התור, וההחלטה תעבור לידי דוד בן-גוריון.", "לציית לצו הפסקת האש (עולה \u200e2.0\u200e תשומת לב)")]
    public void WithTooLittleAttentionTheCardSaysEndTurnHandsItToTheMinister(string lang, string line, string option)
    {
        var text = lang == "he" ? He : Text;
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.Date = C.Events.First(e => e.Id == "second_truce").From;
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add("second_truce");
        var bridge = new SimBridge(g, text);
        using var ui = new UiTestKit();
        var card = new EventCardPresenter(ui.Root, bridge, text, null);
        var delegated = ui.Q<Label>("event-delegate");
        var endTurn = ui.Q<Button>("end-turn");
        var options = ui.Q<VisualElement>("event-options").Query<Button>().ToList();
        Assert.AreEqual(DisplayStyle.None, delegated.style.display.value, "with Attention to pay, no line");
        Assert.IsTrue(options.All(b => b.enabledSelf), "with Attention to pay, every option can be pressed");
        Assert.IsFalse(endTurn.ClassListContains(EventCardPresenter.EndTurnWaitingClass));

        g.State.Attention = 0;
        card.Render();
        options = ui.Q<VisualElement>("event-options").Query<Button>().ToList();
        Assert.AreEqual(DisplayStyle.Flex, delegated.style.display.value);
        Assert.AreEqual(line, delegated.text);
        Assert.AreEqual(option, ui.Q<Button>("option-comply").text, "the option shows its price");
        Assert.IsTrue(options.All(b => !b.enabledSelf), "no option can be pressed");
        Assert.IsTrue(endTurn.ClassListContains(EventCardPresenter.EndTurnWaitingClass), "End Turn is highlighted");

        g.State.Attention = 4;
        card.Render();
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("event-delegate").style.display.value);
        Assert.IsTrue(ui.Q<VisualElement>("event-options").Query<Button>().ToList().All(b => b.enabledSelf));
        Assert.IsFalse(endTurn.ClassListContains(EventCardPresenter.EndTurnWaitingClass));

        // The line names the minister End Turn really hands the card to.
        g.State.Attention = 0;
        var turn = bridge.EndTurn();
        Assert.IsTrue(turn.Why.Any(w => w.Message.StartsWith("Delegated to David Ben-Gurion:")), "EventSystem.Delegate names the same minister");
        Assert.IsFalse(endTurn.ClassListContains(EventCardPresenter.EndTurnWaitingClass), "no card waits after End Turn");
    }

    /// <summary>#1034 review: Step runs the queued orders, Appoint included, before EventSystem.Delegate. With an
    /// appointment queued this turn (One Army, turn 0), the card names the appointee and End Turn hands it to them.</summary>
    [Test]
    public void WithTooLittleAttentionTheCardNamesTheMinisterAppointedThisTurn()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add("idf_founding");
        var bridge = new SimBridge(g, Text);
        using var ui = new UiTestKit();
        var card = new EventCardPresenter(ui.Root, bridge, Text, null);
        var appoint = new Appoint("defense_minister", "zisling");
        Assert.IsNull(bridge.Queue(appoint));
        g.State.Attention = bridge.Price(appoint);   // the rest of the turn's Attention is spent
        card.Render();
        Assert.AreEqual("You don't have enough Attention left to answer this turn. End the turn and the decision goes to Aharon Zisling.", ui.Q<Label>("event-delegate").text);
        var turn = bridge.EndTurn();
        Assert.IsTrue(turn.Why.Any(w => w.Message.StartsWith("Delegated to Aharon Zisling:")), "End Turn hands it to the appointee");
    }

    /// <summary>A card with no office goes to the cabinet at End Turn (EventSystem.Delegate's "the cabinet"): the line says so.</summary>
    [TestCase("en", "You don't have enough Attention left to answer this turn. End the turn and the decision goes to the cabinet.")]
    [TestCase("he", "לא נשארה לך מספיק תשומת לב לענות בתור הזה. אפשר לסיים את התור, וההחלטה תעבור לידי הממשלה.")]
    public void WithTooLittleAttentionACardWithNoOfficeGoesToTheCabinet(string lang, string line)
    {
        var text = lang == "he" ? He : Text;
        var c = ContentLoader.Load(Paths.Content);
        var e = c.Events.First(x => x.Id == "second_truce");
        e.Office = ""; e.Text = "No office.";
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        g.State.Date = e.From;
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(e.Id);
        g.State.Attention = 0;
        using var ui = new UiTestKit();
        new EventCardPresenter(ui.Root, new SimBridge(g, text), text, null);
        Assert.AreEqual(line, ui.Q<Label>("event-delegate").text);
    }

    // ---- The consequence card's banner (#954; plan docs/superpowers/plans/2026-10-17-alternate-timelines.md, A.4.7) ----

    /// <summary>A test-only road, never shipped (the example.org sources say so): a hard card on the game's first day whose
    /// second option was proposed; a consequence card that follows that road, with an option that rejoins the record; and a
    /// card chained on the consequence card, which has no historical option of its own. Schema-valid
    /// (content/schema/event.schema.json); loaded beside the real content through the loader's own checks.</summary>
    static readonly Dictionary<string, string> RoadFixture = new Dictionary<string, string>
    {
        ["events/zz_banner_parent.yaml"] = @"id: banner_parent
title: 'The road of {prime_minister}'
title_he: 'הדרך של {prime_minister}'
text: 'A decision with a record.'
text_he: 'החלטה שיש לה תיעוד.'
text_he_review: agent-checked
anchor: hard
from: 1948-05-15
to: 1948-06-11
options:
  - id: record
    text: What happened
    text_he: מה שקרה
    historical: true
  - id: road
    text: The road not taken
    text_he: הדרך שלא נבחרה
    alternative:
      basis: proposed
      by: the opposition benches
      date: 1948-05-20
      sources: [{ url: ""https://example.org/minutes"", checked: 2026-10-17 }]
sources: [{ url: ""https://example.org/record"", checked: 2026-10-17 }]
",
        ["events/zz_banner_after.yaml"] = @"id: banner_after
kind: consequence
title: After the road
text: 'What was feared.'
from: 1948-06-15
to: 1948-08-15
requires: [{ event: banner_parent, option: road }]
options:
  - id: hold
    text: Hold the course
    alternative: { basis: debated, by: the cabinet, date: 1948-06-20, sources: [{ url: ""https://example.org/cabinet"", checked: 2026-10-17 }] }
  - id: back
    text: Go back
    rejoin: banner_parent
    alternative: { basis: design }
sources: [{ url: ""https://example.org/record"", checked: 2026-10-17 }]
",
        ["events/zz_banner_second.yaml"] = @"id: banner_second
kind: consequence
title: Further along
text: 'Later still.'
from: 1948-07-15
to: 1948-08-15
requires: [{ event: banner_after, option: hold }]
options:
  - id: go_on
    text: Go on
    alternative: { basis: design }
sources: [{ url: ""https://example.org/record"", checked: 2026-10-17 }]
",
    };

    static Content WithRoad()
    {
        string Read(string rel) => RoadFixture.TryGetValue(rel, out var yaml) ? yaml
            : File.Exists(Path.Combine(Paths.Content, rel)) ? File.ReadAllText(Path.Combine(Paths.Content, rel)) : null;
        var files = Directory.GetFiles(Path.Combine(Paths.Content, "events"), "*.yaml").Select(f => "events/" + Path.GetFileName(f));
        return ContentLoader.Load(Read, files.Concat(RoadFixture.Keys));
    }

    /// <summary>The historical game with the fixture's road taken (banner_parent: road, banner_after: hold, ahead of the
    /// reference player's orders), played until <paramref name="until"/> waits for a decision.</summary>
    static SimBridge OnTheRoad(Content c, string until, UiText text)
    {
        var answers = new Dictionary<string, string> { ["banner_parent"] = "road", ["banner_after"] = "hold" };
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        for (int turn = 0; turn < 6 && !g.State.PendingEvents.Contains(until); turn++)
        {
            var mine = answers.Where(a => g.State.PendingEvents.Contains(a.Key)).Select(a => (Command)new ChooseEventOption(a.Key, a.Value));
            var rest = GoldenRun.Orders(c, g.State).Where(o => !(o is ChooseEventOption ch && answers.ContainsKey(ch.EventId)));
            g.Step(mine.Concat(rest).ToArray());
        }
        Assert.Contains(until, g.State.PendingEvents.ToList(), "precondition: the road's card waits");
        return new SimBridge(g, text);
    }

    [Test]
    public void AConsequenceCardSaysWhichRoadItFollows()
    {
        var c = WithRoad();
        var bridge = OnTheRoad(c, "banner_after", Text);
        using var ui = new UiTestKit();
        var card = new EventCardPresenter(ui.Root, bridge, Text, null);
        Assert.AreEqual("banner_after", card.Shown!.Id);
        var banner = ui.Q<Label>("event-alt-banner");
        Assert.AreEqual(DisplayStyle.Flex, banner.style.display.value);
        // The option the player took and its card's title, then the record's option on that card (Timeline.Origin, HistoricalOption).
        Assert.AreEqual("Alternative course. It follows the government's decision: The road not taken (The road of David Ben-Gurion). In the record: What happened.", banner.text);
        // The card itself is as before: its title, text and priced options.
        Assert.AreEqual("After the road", ui.Q<Label>("event-title").text);
        Assert.AreEqual("What was feared.", ui.Q<Label>("event-text").text);
        Assert.AreEqual(new[] { "option-hold", "option-back" }, ui.Q<VisualElement>("event-options").Query<Button>().ToList().Select(b => b.name).ToArray());
        StringAssert.EndsWith("(costs 2.0 Attention)", ui.Q<Button>("option-hold").text);
        Assert.IsTrue(ui.Q<VisualElement>("event-options").Query<Button>().ToList().All(b => b.enabledSelf));
    }

    [Test]
    public void AChainedCardOmitsTheRecordItDoesNotHave()
    {
        var c = WithRoad();
        var bridge = OnTheRoad(c, "banner_second", Text);
        using var ui = new UiTestKit();
        var card = new EventCardPresenter(ui.Root, bridge, Text, null);
        Assert.AreEqual("banner_second", card.Shown!.Id);
        Assert.IsNull(Timeline.HistoricalOption(c.Events.First(e => e.Id == "banner_after")), "precondition: the card it follows has no record");
        var banner = ui.Q<Label>("event-alt-banner");
        Assert.AreEqual(DisplayStyle.Flex, banner.style.display.value);
        Assert.AreEqual("Alternative course. It follows the government's decision: Hold the course (After the road).", banner.text);
    }

    [Test]
    public void AnOrdinaryCardHasNoBanner()
    {
        using var ui = new UiTestKit();
        new EventCardPresenter(ui.Root, AtArmistice(), Text, null);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("event-alt-banner").style.display.value);
        Assert.AreEqual("", ui.Q<Label>("event-alt-banner").text);
        Assert.AreEqual(0, ui.Q<VisualElement>("event-options").Query<Label>(className: EventCardPresenter.RejoinClass).ToList().Count);
    }

    [Test]
    public void ARejoinOptionSaysItReturnsToTheRecord()
    {
        var bridge = OnTheRoad(WithRoad(), "banner_after", Text);
        using var ui = new UiTestKit();
        new EventCardPresenter(ui.Root, bridge, Text, null);
        var lines = ui.Q<VisualElement>("event-options").Query<Label>(className: EventCardPresenter.RejoinClass).ToList();
        Assert.AreEqual(1, lines.Count, "only the option with `rejoin` has the line");
        Assert.AreEqual("Returns to the recorded course.", lines[0].text);
        var children = ui.Q<VisualElement>("event-options").Children().ToList();
        Assert.AreEqual(children.IndexOf(ui.Q<Button>("option-back")) + 1, children.IndexOf(lines[0]), "the line sits under its option");
        Assert.IsFalse(lines[0].focusable, "Tab still reaches only the options");
    }

    /// <summary>The banner belongs to the card it is on: Hide and "Decision waiting" bring it back unchanged, and once the
    /// consequence card is answered the next, ordinary card shows none.</summary>
    [Test]
    public void TheBannerFollowsItsCardThroughHideAndAnAnswer()
    {
        var bridge = OnTheRoad(WithRoad(), "banner_after", Text);
        using var ui = new UiTestKit();
        var card = new EventCardPresenter(ui.Root, bridge, Text, null);
        var banner = ui.Q<Label>("event-alt-banner");
        var shown = banner.text;
        card.Collapse();
        Assert.IsTrue(card.Collapsed);
        card.Reopen();
        Assert.AreEqual("banner_after", card.Shown!.Id);
        Assert.AreEqual(shown, banner.text);
        Assert.AreEqual(DisplayStyle.Flex, banner.style.display.value);
        UiTestKit.Press(ui.Q<Button>("option-hold"));
        Assert.AreNotEqual("banner_after", card.Shown!.Id, "the answer is queued and the next card shows");
        Assert.AreEqual(DisplayStyle.None, banner.style.display.value);
        Assert.AreEqual(0, ui.Q<VisualElement>("event-options").Query<Label>(className: EventCardPresenter.RejoinClass).ToList().Count);
    }

    /// <summary>A save from older content can name an option the road card no longer has: the card shows, with no banner.</summary>
    [Test]
    public void AStaleOptionOnTheRoadShowsNoBanner()
    {
        var bridge = OnTheRoad(WithRoad(), "banner_after", Text);
        bridge.State.ChosenOptions["banner_parent"] = "gone";
        using var ui = new UiTestKit();
        var card = new EventCardPresenter(ui.Root, bridge, Text, null);
        Assert.AreEqual("banner_after", card.Shown!.Id);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("event-alt-banner").style.display.value);
    }

    [Test]
    public void InHebrewTheBannerUsesTheRoadCardsHebrewFieldsAndTheRejoinLineUsesHebrew()
    {
        var c = WithRoad();
        var bridge = OnTheRoad(c, "banner_after", He);
        using var ui = new UiTestKit();
        new EventCardPresenter(ui.Root, bridge, He, null);
        Assert.AreEqual(He.Format("event.alt_origin", "הדרך שלא נבחרה", "הדרך של דוד בן-גוריון", "מה שקרה"), ui.Q<Label>("event-alt-banner").text);
        StringAssert.StartsWith("מסלול חלופי.", ui.Q<Label>("event-alt-banner").text);
        Assert.AreEqual(He["event.rejoin"], ui.Q<VisualElement>("event-options").Q<Label>(className: EventCardPresenter.RejoinClass).text);
        var chained = OnTheRoad(c, "banner_second", He);
        using var ui2 = new UiTestKit();
        new EventCardPresenter(ui2.Root, chained, He, null);
        Assert.AreEqual(He.Format("event.alt_origin_no_record", "Hold the course", "After the road"), ui2.Q<Label>("event-alt-banner").text);
    }

    [Test]
    public void InHebrewTheBannerFallsBackPerFieldAndKeepsAnEnglishVariantTitle()
    {
        var c = WithRoad();
        var bridge = OnTheRoad(c, "banner_after", He);
        var parent = c.Events.First(e => e.Id == "banner_parent");
        var taken = parent.Options.First(o => o.Id == "road");
        using var ui = new UiTestKit();
        var card = new EventCardPresenter(ui.Root, bridge, He, null);

        taken.TextHe = "";
        card.Render();
        Assert.AreEqual(He.Format("event.alt_origin", "The road not taken", "הדרך של דוד בן-גוריון", "מה שקרה"), ui.Q<Label>("event-alt-banner").text);

        taken.TextHe = "הדרך שלא נבחרה";
        parent.Variants.Add(new EventVariant
        {
            When = { new EventRequirement { Event = "banner_parent", Option = "road" } },
            Title = "A different road title",
            Text = "A different road text"
        });
        card.Render();
        Assert.AreEqual(He.Format("event.alt_origin", "הדרך שלא נבחרה", "A different road title", "מה שקרה"), ui.Q<Label>("event-alt-banner").text);
    }
}
