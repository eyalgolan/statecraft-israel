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
        Assert.IsNull(catalog.Get("lavon"));                          // no public-domain portrait recorded
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
}
