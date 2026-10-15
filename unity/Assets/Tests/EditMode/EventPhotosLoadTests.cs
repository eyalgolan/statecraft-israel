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
    public void APrefetchAfterTheCardLoadedTheCatalogItselfLoadsNothingMore()
    {
        EventPhotos.StartLoad = () => { starts++; return () => Shipped; };
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, Showing("armistice_egypt"), Text, null, null, photosLater: EventPhotos.Get);
        var first = EventPhotos.Get();
        EventPhotos.Prefetch();                                            // the scheduled prefetch, a second later
        Assert.AreEqual(0, starts, "no second load");
        Assert.AreSame(first, EventPhotos.Get());
        Assert.AreEqual(1, EventPhotos.Loads);
        card.Render();
        AssertShowsPhoto(ui, "armistice_egypt");
    }

    [Test]
    public void ALoadThatGivesNothingLeavesThePortraitAndIsNotRetried([Values(false, true)] bool prefetched)
    {
        EventPhotos.LoadNow = () => { loadsNow++; return null; };
        EventPhotos.StartLoad = () => { starts++; return () => null; };
        if (prefetched) EventPhotos.Prefetch();
        var portraits = AssetDatabase.LoadAssetAtPath<PortraitCatalog>("Assets/UI/Portraits/PortraitCatalog.asset");
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, Showing("armistice_egypt"), Text, portraits, null, photosLater: EventPhotos.Get);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-photo").style.display.value, "no photograph");
        Assert.AreEqual("", ui.Q<Label>("event-photo-credit").text);
        Assert.AreEqual(portraits.Get("ben_gurion"), ui.Q<VisualElement>("event-image").style.backgroundImage.value.texture, "the portrait instead");
        card.Render();
        EventPhotos.Prefetch();
        Assert.IsNull(EventPhotos.Get());
        Assert.IsTrue(EventPhotos.Loaded);
        Assert.AreEqual(1, loadsNow + starts, "one load for the page, no retry");
    }

    [Test]
    public void BeginSchedulesThePrefetchASecondLater([Values(false, true)] bool startScreen)
    {
        var scheduled = new System.Collections.Generic.List<(VisualElement root, System.Action action, long delay)>();
        EventPhotos.Schedule = (root, action, delay) => scheduled.Add((root, action, delay));
        EventPhotos.StartLoad = () => { starts++; return () => Shipped; };
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        try
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            Object.FindFirstObjectByType<MapScene>().Build(inputs.Map);
            var root = Object.FindFirstObjectByType<GameRoot>();
            root.Store = new SaveStore(saveDir);
            root.Begin(inputs, 0, startScreen);
            root.Begin(inputs, 0, startScreen);                            // New Game
            Assert.AreEqual(2, scheduled.Count, "one schedule a Begin");
            var document = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            foreach (var (on, action, delay) in scheduled)
            {
                Assert.AreSame(document, on, "on the game's UI root");
                Assert.AreEqual(1000, delay, "a second after Begin");
                Assert.AreEqual(EventPhotos.PrefetchDelayMs, delay);
            }
            if (!startScreen)
            {
                // No start screen (the automated runs): since the GDD cutover turn 0 shows a card (One Army, 15 May 1948), and
                // a visible card takes its photograph at once; New Game reuses it, and the prefetch then has nothing to do.
                Assert.AreEqual(1, loadsNow, "the visible turn-0 card loads the catalog once");
                foreach (var (_, action, _) in scheduled) action();
                Assert.AreEqual(0, starts, "the prefetch finds it loaded");
                return;
            }
            Assert.AreEqual(0, starts, "nothing loads until the scheduler runs it");
            foreach (var (_, action, _) in scheduled) action();
            Assert.AreEqual(1, starts, "the action is the async prefetch, and once");
            Assert.AreEqual(0, loadsNow, "never the synchronous load: the turn-0 card waits under the start screen (#592)");
            Assert.IsFalse(EventPhotos.Loaded, "the catalog waits for the first card");
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }
    }

    /// <summary>An autosave whose turn has a decision waiting, with a photograph, as End Turn writes it (golden seed;
    /// about 40% of autosaves, issue #592). Returns that event's id.</summary>
    string SaveWithAPhotographedDecisionWaiting()
    {
        var played = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        for (int turn = 0; turn < 120; turn++)
        {
            played.EndTurn();
            var id = played.State.PendingEvents.FirstOrDefault();
            if (id != null && Shipped.Find(id) != null)
            {
                new SaveStore(saveDir).WriteAutosave(played.Session.Save.Write());
                return id;
            }
        }
        Assert.Fail("no autosave with a photographed decision waiting in 120 turns");
        return "";
    }

    static void AssertShowsPhoto(VisualElement ui, string eventId)
    {
        var entry = Shipped.Find(eventId)!.Value;
        var band = ui.Q<VisualElement>("event-photo");
        Assert.AreEqual(DisplayStyle.Flex, band.style.display.value, eventId + ": the photograph shows");
        Assert.AreEqual(entry.image, band.style.backgroundImage.value.texture, eventId + ": its own photograph");
        Assert.AreEqual(entry.credit, ui.Q<Label>("event-photo-credit").text);
        Assert.AreEqual(entry.alt, band.tooltip);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-image").style.display.value, "no portrait in its place");
    }

    /// <summary>Issue #592: Continue with a decision waiting. Begin builds the card under the start screen and loads no
    /// catalog (before, the card's first Render loaded it synchronously in Begin). The card shows its photograph, and never
    /// the portrait, when Continue closes the screen: after the prefetch, from it; before it, loaded then.</summary>
    [Test]
    public void AContinueWithADecisionWaitingLoadsNoCatalogInBeginAndShowsThePhotographOnContinue([Values(true, false)] bool prefetchFirst)
    {
        var eventId = SaveWithAPhotographedDecisionWaiting();
        var scheduled = new System.Collections.Generic.List<System.Action>();
        EventPhotos.Schedule = (_, action, _) => scheduled.Add(action);
        EventPhotos.StartLoad = () => { starts++; return () => Shipped; };
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        try
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            Object.FindFirstObjectByType<MapScene>().Build(inputs.Map);
            var root = Object.FindFirstObjectByType<GameRoot>();
            root.Store = new SaveStore(saveDir);
            root.Begin(inputs, 0, startScreen: true);
            Assert.AreEqual(eventId, root.Bridge!.State.PendingEvents.FirstOrDefault(), "resumed with the decision waiting");
            var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            Assert.IsTrue(ui.Q("root").ClassListContains(StartScreenPresenter.OpenClass), "under the start screen");
            Assert.AreEqual(0, loadsNow, "no synchronous catalog load in Begin");
            Assert.AreEqual(0, starts);
            Assert.IsFalse(EventPhotos.Loaded);
            Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-image").style.display.value, "no portrait under the screen either");
            Assert.AreEqual(1, scheduled.Count);
            if (prefetchFirst) scheduled[0]();                             // the idle-time prefetch, a second after Begin
            UiTestKit.Press(ui.Q<Button>("start-continue"));
            Assert.IsFalse(ui.Q("root").ClassListContains(StartScreenPresenter.OpenClass));
            AssertShowsPhoto(ui, eventId);
            Assert.AreEqual(prefetchFirst ? 0 : 1, loadsNow, prefetchFirst ? "the prefetch's catalog" : "loaded when the screen closed");
            Assert.AreEqual(prefetchFirst ? 1 : 0, starts);
            Assert.AreEqual(1, EventPhotos.Loads, "one load for the page");
            if (!prefetchFirst) { scheduled[0](); Assert.AreEqual(0, starts, "the late prefetch loads nothing more"); }
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }
    }

    /// <summary>Issue #592 review: the card waits for its photograph exactly when Begin shows the start screen (one value
    /// in GameRoot decides both; the ?mvp=1 slice went with the cutover). A card waiting under no screen would never be uncovered and
    /// never show its photograph.</summary>
    [Test]
    public void TheCardWaitsForThePhotographExactlyWhenTheStartScreenShows([Values(false, true)] bool startScreen, [Values(0, 5)] int smoke)
    {
        SaveWithAPhotographedDecisionWaiting();
        EventPhotos.Schedule = (_, _, _) => { };
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        try
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            Object.FindFirstObjectByType<MapScene>().Build(inputs.Map);
            var root = Object.FindFirstObjectByType<GameRoot>();
            root.Store = new SaveStore(saveDir);
            if (smoke > 0) UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, "SMOKE FAIL the smoke run needs play mode or a player");
            root.Begin(inputs, smoke, startScreen);
            var open = Object.FindFirstObjectByType<UIDocument>().rootVisualElement.Q("root").ClassListContains(StartScreenPresenter.OpenClass);
            Assert.AreEqual(startScreen && smoke == 0, open, "the start screen shows only outside a smoke run");
            Assert.AreEqual(open, root.Card!.PhotoWaits, "the card waits for the photograph exactly when the screen covers it");
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }
    }

    /// <summary>Issue #592 with the real loads: a Continue with a decision waiting adds no Resources load to Begin.</summary>
    [Test]
    public void GameRootBeginOnAContinueWithADecisionWaitingLoadsNoPhotoCatalog()
    {
        SaveWithAPhotographedDecisionWaiting();
        EventPhotos.Reset();                                               // the real loads, counted by EventImageCatalog
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        try
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            Object.FindFirstObjectByType<MapScene>().Build(inputs.Map);
            var root = Object.FindFirstObjectByType<GameRoot>();
            root.Store = new SaveStore(saveDir);
            var before = EventImageCatalog.ResourceLoads;
            root.Begin(inputs, 0, startScreen: true);
            Assert.IsNotEmpty(root.Bridge!.State.PendingEvents);
            Assert.AreEqual(before, EventImageCatalog.ResourceLoads, "no catalog load in Begin");
            Assert.IsFalse(EventPhotos.Loaded);
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }
    }

    /// <summary>#592: the turn-0 card (One Army, since the GDD cutover) under the start screen asks for no photograph; when
    /// the screen closes the card takes it, so it never opens with the minister's portrait.</summary>
    [Test]
    public void TheOpeningCardTakesItsPhotographWhenTheStartScreenCloses()
    {
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        try
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            Object.FindFirstObjectByType<MapScene>().Build(inputs.Map);
            var root = Object.FindFirstObjectByType<GameRoot>();
            root.Store = new SaveStore(saveDir);
            root.Begin(inputs, 0, startScreen: true);
            var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            Assert.IsTrue(ui.Q("root")!.ClassListContains(StartScreenPresenter.OpenClass), "the start screen is up");
            Assert.AreEqual(0, loadsNow, "no catalog under the start screen");
            Assert.IsFalse(EventPhotos.Loaded);
            UiTestKit.Press(ui.Q<Button>("start-new-game"));               // a new game: the first press closes the screen
            Assert.IsFalse(ui.Q("root")!.ClassListContains(StartScreenPresenter.OpenClass), "the start screen closed");
            Assert.AreEqual(1, loadsNow, "the card loads the catalog when it comes out from under the screen");
            var entry = Shipped.Find("idf_founding")!.Value;
            var band = ui.Q<VisualElement>("event-photo");
            Assert.AreEqual(DisplayStyle.Flex, band.style.display.value, "the photograph shows");
            Assert.AreEqual(entry.image, band.style.backgroundImage.value.texture);
            Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-image").style.display.value, "no portrait in its place");
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }
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
            // Since the GDD cutover turn 0 shows a card. Under the start screen it waits, so Begin loads nothing (#592), and
            // New Game's Begin does not load it for the closing game either (GameRoot.Shutdown disposes the card first); with
            // no start screen (the automated runs) the card is visible and loads the catalog once, and New Game reuses it.
            Assert.AreEqual(before + (startScreen ? 0 : 1), EventImageCatalog.ResourceLoads, "catalog loads in Begin");
            Assert.AreEqual(!startScreen, EventPhotos.Loaded);
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }
    }
}
