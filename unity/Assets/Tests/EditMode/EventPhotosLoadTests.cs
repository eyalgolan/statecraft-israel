#nullable enable
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

/// <summary>Issue #321 (T59): the event photo catalog (EventImageCatalog in Resources, with its 24 crunched textures) cost
/// about 101 ms of GameRoot.Begin at cpu 4, for a card that no turn-0 game shows. It now loads off the boot path
/// (EventPhotos): GameRoot starts an async load a moment after the game starts, while the start screen is up, and the
/// card asks for the catalog only when it shows an event. A load still in flight is waited for, and with none started
/// the card loads it then, so the first card always has its photograph. New Game reuses the loaded catalog.</summary>
public class EventPhotosLoadTests
{
    const string PhotosPath = "Assets/Resources/EventImageCatalog.asset";
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static EventImageCatalog Shipped => AssetDatabase.LoadAssetAtPath<EventImageCatalog>(PhotosPath);

    string saveDir = "";
    int loadsNow, starts;

    [SetUp]
    public void SetUp()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-photos-" + System.Guid.NewGuid().ToString("N"));
        EventPhotos.Reset();
        loadsNow = starts = 0;
        EventPhotos.LoadNow = () => { loadsNow++; return Shipped; };
    }

    [TearDown]
    public void TearDown()
    {
        EventPhotos.Reset();
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    /// <summary>The historical game with only this event pending, on its first day (as EventCardTests does).</summary>
    static SimBridge Showing(string eventId)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.Date = C.Events.First(e => e.Id == eventId).From;
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(eventId);
        return new SimBridge(g, Text);
    }

    static SimBridge NoEvent()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.PendingEvents.Clear();
        return new SimBridge(g, Text);
    }

    static void AssertShowsPhoto(UiTestKit ui, string eventId)
    {
        var entry = Shipped.Find(eventId)!.Value;
        var band = ui.Q<VisualElement>("event-photo");
        Assert.AreEqual(DisplayStyle.Flex, band.style.display.value, eventId + ": the photograph shows");
        Assert.AreEqual(entry.image, band.style.backgroundImage.value.texture, eventId + ": its own photograph");
        Assert.AreEqual(entry.credit, ui.Q<Label>("event-photo-credit").text);
        Assert.AreEqual(entry.alt, band.tooltip);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-image").style.display.value, "no portrait in its place");
    }

    [Test]
    public void ACardWithNoEventLoadsNoCatalog()
    {
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, NoEvent(), Text, null, null, photosLater: EventPhotos.Get);
        Assert.IsNull(card.Shown);
        Assert.AreEqual(0, loadsNow);
        Assert.IsFalse(EventPhotos.Loaded);
    }

    [Test]
    public void TheFirstCardShowsItsPhotographWhenItLoadsTheCatalogItself()
    {
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, Showing("armistice_egypt"), Text, null, null, photosLater: EventPhotos.Get);
        AssertShowsPhoto(ui, "armistice_egypt");
        Assert.AreEqual(1, loadsNow, "loaded on the first card");
        Assert.AreEqual(1, EventPhotos.Loads);
        card.Render(); card.Render();
        Assert.AreEqual(1, EventPhotos.Loads, "once");
    }

    [Test]
    public void TheFirstCardShowsItsPhotographAfterThePrefetch()
    {
        EventPhotos.StartLoad = () => { starts++; return () => Shipped; };
        EventPhotos.Prefetch();
        EventPhotos.Prefetch();
        Assert.AreEqual(1, starts, "one load");
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, Showing("armistice_egypt"), Text, null, null, photosLater: EventPhotos.Get);
        AssertShowsPhoto(ui, "armistice_egypt");
        Assert.AreEqual(0, loadsNow, "the prefetch's load, not a second one");
        Assert.AreEqual(1, EventPhotos.Loads);
    }

    [Test]
    public void ALoadStillInFlightIsWaitedForAndTheCardNeverShowsThePortraitFirst()
    {
        bool done = false, waited = false;
        EventPhotos.StartLoad = () => { starts++; return () => { waited = !done; done = true; return Shipped; }; };
        EventPhotos.Prefetch();
        // With the portraits passed in, a card that rendered before the photograph arrived would show the minister.
        var portraits = AssetDatabase.LoadAssetAtPath<PortraitCatalog>("Assets/UI/Portraits/PortraitCatalog.asset");
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, Showing("armistice_egypt"), Text, portraits, null, photosLater: EventPhotos.Get);
        Assert.IsTrue(waited, "the card waited for the load in flight");
        AssertShowsPhoto(ui, "armistice_egypt");
        Assert.IsNull(ui.Q<VisualElement>("event-image").style.backgroundImage.value.texture, "no portrait set");
        Assert.AreEqual(1, starts);
        Assert.AreEqual(0, loadsNow);
    }

    [Test]
    public void NewGameReusesTheLoadedCatalogAndShowsTheRightPhotographs()
    {
        using (var ui = new UiTestKit())
        using (new EventCardPresenter(ui.Root, Showing("armistice_egypt"), Text, null, null, photosLater: EventPhotos.Get))
            AssertShowsPhoto(ui, "armistice_egypt");
        using (var ui = new UiTestKit())                                   // New Game: a new presenter over a new session
        using (new EventCardPresenter(ui.Root, Showing("first_truce"), Text, null, null, photosLater: EventPhotos.Get))
            AssertShowsPhoto(ui, "first_truce");
        Assert.AreEqual(1, loadsNow, "no second load");
        Assert.AreEqual(1, EventPhotos.Loads);
    }

    [Test]
    public void ThePrefetchLoadsTheShippedCatalogFromResources()
    {
        EventPhotos.Reset();                                               // the real loads
        EventPhotos.Prefetch();
        var loaded = EventPhotos.Get();
        Assert.IsNotNull(loaded);
        Assert.AreSame(Resources.Load<EventImageCatalog>(EventImageCatalog.ResourcePath), loaded);
        Assert.AreSame(loaded, EventPhotos.Get());
        Assert.AreEqual(1, EventPhotos.Loads);
    }

    [Test]
    public void GameRootBeginLoadsNoPhotoCatalog([Values(false, true)] bool startScreen)
    {
        EventPhotos.Reset();                                               // the real loads, counted by EventImageCatalog
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        try
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            Object.FindFirstObjectByType<MapScene>().Build(inputs.Map);
            var root = Object.FindFirstObjectByType<GameRoot>();
            root.Store = new SaveStore(saveDir);
            var before = EventImageCatalog.ResourceLoads;
            root.Begin(inputs, 0, startScreen);
            root.Begin(inputs, 0, startScreen);                            // New Game
            Assert.AreEqual(before, EventImageCatalog.ResourceLoads, "no catalog load in Begin (the prefetch waits for the idle time)");
            Assert.IsFalse(EventPhotos.Loaded);
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }
    }
}
