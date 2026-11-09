#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#2609: one Escape closes one layer, the topmost visible one. Drives GameRoot's own Escape poll (Update) with a key
/// pressed through the Input System test fixture, which is what makes wasPressedThisFrame read true in an EditMode test.</summary>
public class GameRootEscapeLayerTests : SharedCameraActionsFixture
{
    string saveDir = "";
    Keyboard keyboard = null!;
    static VisualElement Ui => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
    static VisualElement HudRoot => Ui.Q("root");

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    GameRoot Open(GameInputs inputs, bool startScreen)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(inputs, 0, startScreen);
        keyboard = InputSystem.AddDevice<Keyboard>();
        return root;
    }

    void Escape(GameRoot root)
    {
        (Ui.focusController?.focusedElement as VisualElement)?.Blur();   // Escape with no focus: only GameRoot's poll takes it
        Press(keyboard.escapeKey);
        Assert.IsTrue(keyboard.escapeKey.wasPressedThisFrame, "precondition: Escape went down this frame");
        typeof(GameRoot).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(root, null);
        Release(keyboard.escapeKey);
    }

    [SetUp]
    public void NewSaveDir() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-escape-" + Guid.NewGuid().ToString("N"));

    /// <summary>The advisor's full-screen media overlay disables the screens under it but leaves ScreenNav.Current as it
    /// was: the Escape that closes the overlay must not close the side screen under it too.</summary>
    [Test]
    public void EscapeClosesTheMediaOverlayAndLeavesTheScreenUnderIt()
    {
        var root = Open(GameInputs.FromDisk(Paths.Content, Paths.StreamingMap), startScreen: false);
        GameSceneTests.HideTheOpeningCard(Ui);                         // the card is a hard cover: no media plays under it
        // A clip with a picture, so it takes the overlay; its label is any shipped key, as the advisor reads it from the game's text.
        var row = new AdvisorCatalog.Media
        {
            Picture = "x.mp4", Narration = new Dictionary<string, string> { ["en"] = "x.en.wav" },
            Cues = new Dictionary<string, List<double>> { ["en"] = new List<double> { 0.0 } }, Label = "report.close",
        };
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text + "\nadvisor.x.cue.1: \"First.\"\n");
        var advisor = root.Advisor!;
        advisor.Media = new AdvisorMedia(text, new AdvisorMediaTests.FakePicture(), null, AdvisorMediaTests.Immediate, new AdvisorMediaTests.FakeOutput());
        advisor.ForceInline = false;
        UiTestKit.Press(Ui.Q<Button>("nav-budget"));
        Assert.AreEqual(DisplayStyle.Flex, Ui.Q("screen-budget").style.display.value, "precondition: a side screen is open");
        advisor.WatchOverlay(row, "advisor.x");
        Assert.IsTrue(HudRoot.ClassListContains(AdvisorPresenter.OverlayOpenClass), "precondition: the overlay is over it");

        Escape(root);
        Assert.IsFalse(HudRoot.ClassListContains(AdvisorPresenter.OverlayOpenClass), "the first Escape closes the overlay");
        Assert.AreEqual(DisplayStyle.Flex, Ui.Q("screen-budget").style.display.value, "and only the overlay: the screen stays");
        // A later frame: the overlay's close counts as this press's only in the frame it happened (OverlayClosedThisFrame),
        // and an EditMode test cannot advance Time.frameCount between two presses.
        typeof(AdvisorPresenter).GetField("overlayClosedAt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(advisor, -1);
        Escape(root);
        Assert.AreNotEqual(DisplayStyle.Flex, Ui.Q("screen-budget").style.display.value, "the next Escape closes the screen");
    }

    /// <summary>The turn report is over everything under it: the Escape that closes it must not close a side screen under it
    /// too. A report opens with the screens closed (TurnReportPresenter.Show), so the test puts the report's class over an
    /// open screen by hand, as SelectThenClickMoveTests does.</summary>
    [Test]
    public void EscapeClosesTheReportAndLeavesTheScreenUnderIt()
    {
        var root = Open(GameInputs.FromDisk(Paths.Content, Paths.StreamingMap), startScreen: false);
        GameSceneTests.HideTheOpeningCard(Ui);
        UiTestKit.Press(Ui.Q<Button>("nav-budget"));
        Assert.AreEqual(DisplayStyle.Flex, Ui.Q("screen-budget").style.display.value, "precondition: a side screen is open");
        HudRoot.AddToClassList(TurnReportPresenter.OpenClass);

        Escape(root);
        Assert.IsFalse(HudRoot.ClassListContains(TurnReportPresenter.OpenClass), "the Escape closes the report");
        Assert.AreEqual(DisplayStyle.Flex, Ui.Q("screen-budget").style.display.value, "and only the report: the screen stays");
    }

    /// <summary>A key press reaches the focused element as the UI Cancel event, which closes the open screen (ScreenNav), before
    /// GameRoot polls the same Escape. That poll finds the screen closed this frame (ScreenNav.ClosedThisFrame), so the press
    /// was the screen's: it closes nothing else and leaves a New Game armed from the top bar armed.</summary>
    [Test]
    public void EscapeAFocusedElementUsedToCloseTheScreenLeavesAnArmedNewGame()
    {
        var root = Open(GameInputs.FromDisk(Paths.Content, Paths.StreamingMap), startScreen: false);
        GameSceneTests.HideTheOpeningCard(Ui);
        // The Hide is not this press's: CollapsedThisFrame would otherwise claim it in the single EditMode frame.
        typeof(EventCardPresenter).GetField("collapsedAt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(root.Card, -1);
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        UiTestKit.Press(Ui.Q<Button>("new-game"));
        Assert.AreEqual(text["newgame.confirm"], Ui.Q<Label>("toast").text, "precondition: the top bar armed the request");
        var newGame = (NewGamePresenter)typeof(GameRoot).GetField("newGame", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(root)!;
        UiTestKit.Press(Ui.Q<Button>("nav-budget"));
        Assert.AreEqual(DisplayStyle.Flex, Ui.Q("screen-budget").style.display.value, "precondition: a side screen is open");

        var at = Ui.Q<Button>("nav-budget");
        using (var e = NavigationCancelEvent.GetPooled()) { e.target = at; at.SendEvent(e); }
        Assert.AreNotEqual(DisplayStyle.Flex, Ui.Q("screen-budget").style.display.value, "precondition: the Cancel closed the screen");
        Press(keyboard.escapeKey);                                     // the same press, as GameRoot's poll reads it
        Assert.IsTrue(keyboard.escapeKey.wasPressedThisFrame, "precondition: Escape went down this frame");
        typeof(GameRoot).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(root, null);
        Release(keyboard.escapeKey);
        Assert.IsTrue(newGame.Armed, "the press was the screen's: the request is still armed");
        Assert.AreEqual(text["newgame.confirm"], Ui.Q<Label>("toast").text, "and its prompt stays on the toast");
    }

    /// <summary>#2202: the Arab forces' moves play on the map, under every screen. A screen the player opened while they play
    /// is the topmost layer: the first Escape closes it and leaves the moves playing, and the next one skips them (#2967
    /// review, S1). The same holds when the focused element's Cancel closed the screen in this frame. GameRoot plays nothing in
    /// EditMode (Application.isPlaying), so the test puts a playback of its own, on a bridge of its own, in GameRoot's field.</summary>
    [Test]
    public void EscapeClosesAScreenOverTheEnemyMovesBeforeSkippingThem([Values(false, true)] bool focusedCancel)
    {
        var root = Open(GameInputs.FromDisk(Paths.Content, Paths.StreamingMap), startScreen: false);
        GameSceneTests.HideTheOpeningCard(Ui);
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        var bridge = new SimBridge(GameSession.New(ContentLoader.Load(Paths.Content), GoldenRun.Seed, Mode.Historical), text,
            EnemyTurnPlaybackTests.Step(EnemyTurnPlaybackTests.Move(901, 0, 0, 1, 0), EnemyTurnPlaybackTests.Move(902, 30, 0, 31, 0)));
        using var playback = new EnemyTurnPlayback(bridge, new EnemyTurnPlaybackTests.FakeMap(), new EnemyTurnPlaybackTests.FakeCamera(),
            () => true, () => false);
        var field = typeof(GameRoot).GetField("playback", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var own = field.GetValue(root);
        field.SetValue(root, playback);
        try
        {
            bridge.EndTurn();
            playback.Tick(0.05f);
            Assert.IsTrue(playback.Active, "precondition: the moves play");
            UiTestKit.Press(Ui.Q<Button>("nav-budget"));
            Assert.AreEqual(DisplayStyle.Flex, Ui.Q("screen-budget").style.display.value, "precondition: a side screen is open over them");

            if (focusedCancel)
            {
                var at = Ui.Q<Button>("nav-budget");
                using (var e = NavigationCancelEvent.GetPooled()) { e.target = at; at.SendEvent(e); }
                Assert.AreNotEqual(DisplayStyle.Flex, Ui.Q("screen-budget").style.display.value, "precondition: the Cancel closed the screen");
                Press(keyboard.escapeKey);                             // the same press, as GameRoot's poll reads it
                typeof(GameRoot).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(root, null);
                Release(keyboard.escapeKey);
            }
            else
            {
                Escape(root);
                Assert.AreNotEqual(DisplayStyle.Flex, Ui.Q("screen-budget").style.display.value, "the first Escape closes the screen");
            }
            Assert.IsTrue(playback.Active, "and only the screen: the moves go on");

            // A later frame: the screen's close counts as this press's only in the frame it happened (ScreenNav.ClosedThisFrame),
            // and an EditMode test cannot advance Time.frameCount between two presses.
            var nav = typeof(GameRoot).GetField("nav", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(root)!;
            typeof(ScreenNav).GetField("closedFrame", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(nav, -1);
            Escape(root);
            Assert.IsFalse(playback.Active || playback.Pending, "the next Escape skips the moves");
        }
        finally { field.SetValue(root, own); }
    }

    /// <summary>#3000: the Escape that skips New game's raid flight shows the raid card in the same frame (the flight's finish).
    /// The press was the flight's: GameRoot's poll of it finds the flight closed this frame (RaidFlightPresenter.ClosedThisFrame)
    /// and leaves the card it just showed open, whether the press reached the flight as the focused layer's Cancel first or only
    /// through the poll. The next Escape closes the card. Booted as RaidFlightRoutingTests boots it, with a flight of its own.</summary>
    [Test]
    public void EscapeThatSkipsTheRaidFlightLeavesTheRaidCardItShowedOpen([Values(false, true)] bool focusedCancel)
    {
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Settings = new SettingsStore(Path.Combine(saveDir, "settings")); root.Language = "en";
        root.RaidCardInEditMode = true; root.RaidFlightInEditMode = true;
        int stops = 0;
        root.RaidFlightForTests = ended => () => stops++;              // flies until skipped: EditMode has no frame clock
        root.Begin(inputs, 0, startScreen: true);
        keyboard = InputSystem.AddDevice<Keyboard>();
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(HudRoot.ClassListContains(RaidFlightPresenter.OpenClass), "precondition: New game flies the raid");
        Assert.IsFalse(HudRoot.ClassListContains(RaidCardPresenter.OpenClass), "precondition: the card waits for the flight");

        if (focusedCancel)
        {
            var layer = Ui.Q("raid-flight-layer");
            using (var e = NavigationCancelEvent.GetPooled()) { e.target = layer; layer.SendEvent(e); }
            Assert.IsFalse(HudRoot.ClassListContains(RaidFlightPresenter.OpenClass), "precondition: the Cancel skipped the flight");
            Press(keyboard.escapeKey);                                 // the same press, as GameRoot's poll reads it
            Assert.IsTrue(keyboard.escapeKey.wasPressedThisFrame, "precondition: Escape went down this frame");
            typeof(GameRoot).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(root, null);
            Release(keyboard.escapeKey);
        }
        else Escape(root);
        Assert.IsFalse(HudRoot.ClassListContains(RaidFlightPresenter.OpenClass), "the Escape skips the flight");
        Assert.AreEqual(1, stops, "the Dakotas are stopped once");
        Assert.IsTrue(HudRoot.ClassListContains(RaidCardPresenter.OpenClass), "and only the flight: the card it showed stays open");

        // A later frame: the flight's close counts as this press's only in the frame it happened (ClosedThisFrame), and an
        // EditMode test cannot advance Time.frameCount between two presses.
        var flight = typeof(GameRoot).GetField("raidFlight", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(root)!;
        typeof(RaidFlightPresenter).GetField("closedFrame", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(flight, -1);
        Escape(root);
        Assert.IsFalse(HudRoot.ClassListContains(RaidCardPresenter.OpenClass), "the next Escape closes the card");
    }

    /// <summary>An ended autosave opens the end screen before the start screen goes up over it. Escape on the start screen
    /// is the start screen's: it withdraws an armed New Game, and never closes the hidden end screen under it.</summary>
    [Test]
    public void EscapeOnTheStartScreenLeavesTheEndScreenUnderIt()
    {
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        var ended = GameSession.New(inputs.Content, 9, Mode.Historical);
        for (int turn = 0; turn < 1000 && ended.State.GameOver == null; turn++) ended.Step(Array.Empty<Command>());
        Assert.IsNotNull(ended.State.GameOver, "precondition: a game left alone ends");
        new SaveStore(saveDir).WriteAutosave(ended.Save.Write());
        var root = Open(inputs, startScreen: true);
        Assert.IsTrue(HudRoot.ClassListContains(StartScreenPresenter.OpenClass), "precondition: the start screen is up");
        Assert.IsTrue(HudRoot.ClassListContains(EndScreenPresenter.OpenClass), "precondition: the ended save's end screen is under it");
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

        Escape(root);                                                  // nothing armed: the start screen has nothing to close
        Assert.IsTrue(HudRoot.ClassListContains(StartScreenPresenter.OpenClass), "the start screen stays");
        Assert.IsTrue(HudRoot.ClassListContains(EndScreenPresenter.OpenClass), "the end screen under it is not closed");

        UiTestKit.Press(Ui.Q<Button>("start-new-game"));               // a resumed game: the first press arms the request
        Assert.AreEqual(text["newgame.confirm"], Ui.Q<Label>("toast").text, "precondition: New Game is armed");
        Escape(root);
        Assert.AreEqual("", Ui.Q<Label>("toast").text, "the Escape withdraws the request");
        Assert.IsTrue(HudRoot.ClassListContains(StartScreenPresenter.OpenClass), "the start screen stays");
        Assert.IsTrue(HudRoot.ClassListContains(EndScreenPresenter.OpenClass), "and only the request: the end screen stays");

        UiTestKit.Press(Ui.Q<Button>("start-continue"));
        Assert.IsTrue(HudRoot.ClassListContains(EndScreenPresenter.OpenClass), "Continue finds the end report");
        Escape(root);
        Assert.IsFalse(HudRoot.ClassListContains(EndScreenPresenter.OpenClass), "now Escape closes the end screen");
    }
}
