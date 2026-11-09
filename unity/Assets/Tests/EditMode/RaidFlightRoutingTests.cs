#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>New game's raid flight (#3000, PR 2 of #2968; owner, 8 Nov 2026, "Film, raid, card, advisors (Recommended)"):
/// after the opening and before the raid card, once per New game; Continue, a load and the tools' Begin never fly it, nor do
/// the automated runs (no video, CardVideo.ForcedOff). Any click, or Escape, skips it and the card still follows. Booted as a
/// player boots it, as RaidCardRoutingTests; the flight itself is a fake here (the Dakotas' path and bombs are in
/// DakotaRaidFlightTests and SeaAirCollectionTests): EditMode has no frame clock to fly it by.</summary>
public class RaidFlightRoutingTests
{
    sealed class FakeFlight
    {
        public int Plays, Stops;
        public bool Available = true;
        Action? ended;

        public Action? Start(Action ended)
        {
            if (!Available) return null;
            Plays++; this.ended = ended;
            return () => Stops++;
        }

        /// <summary>The flight ends by itself (the wing has flown away).</summary>
        public void End() => ended?.Invoke();
    }

    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    readonly List<string> directories = new List<string>();
    readonly List<(PanelSettings panel, RenderTexture texture)> panels = new List<(PanelSettings, RenderTexture)>();
    static VisualElement Ui => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
    static VisualElement Hud => Ui.Q("root");
    static bool FlightOpen => Hud.ClassListContains(RaidFlightPresenter.OpenClass);
    static bool RaidOpen => Hud.ClassListContains(RaidCardPresenter.OpenClass);
    static bool OpeningOpen => Hud.ClassListContains(OpeningPresenter.OpenClass);
    static VisualElement Layer => Ui.Q("raid-flight-layer");

    string NewDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-raid-flight-" + Guid.NewGuid().ToString("N"));
        directories.Add(dir);
        return dir;
    }

    GameRoot Open(FakeFlight flight, bool saved = false, bool opening = false, bool settings = true)
    {
        var store = new SaveStore(NewDirectory());
        if (saved) store.WriteAutosave(GameSession.New(Inputs.Content, 23UL, Mode.Historical).Save.Write());
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var panel = Object.Instantiate(doc.panelSettings);
        var texture = new RenderTexture(1280, 720, 24);
        panel.targetTexture = texture;
        panels.Add((panel, texture));
        doc.panelSettings = panel;   // real layout/focus in EditMode; never mutate the shared PanelSettings asset
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = store; root.Settings = settings ? new SettingsStore(NewDirectory()) : null; root.Language = "en";
        root.OpeningInEditMode = opening; root.RaidCardInEditMode = true;
        root.RaidFlightInEditMode = true; root.RaidFlightForTests = flight.Start;
        root.Begin(Inputs, 0, startScreen: true);
        return root;
    }

    [TearDown]
    public void Clean()
    {
        CardVideo.ForceOffForTests = false;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (var dir in directories) if (Directory.Exists(dir)) Directory.Delete(dir, true);
        directories.Clear();
        foreach (var (panel, texture) in panels)
        {
            if (panel != null) Object.DestroyImmediate(panel);
            if (texture != null) { texture.Release(); Object.DestroyImmediate(texture); }
        }
        panels.Clear();
    }

    [Test]
    public void NewGameFliesTheRaidThenShowsTheCardThenTheAdvisors()
    {
        var flight = new FakeFlight();
        var root = Open(flight);
        Assert.IsFalse(FlightOpen, "not on the start screen");
        Assert.AreEqual(0, flight.Plays);
        var hash = root.Bridge!.State.Hash();
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.AreEqual(1, flight.Plays, "New game flies it once");
        Assert.IsTrue(FlightOpen);
        Assert.IsFalse(RaidOpen, "the card waits for the flight");
        Assert.IsFalse(root.Advisor!.IsOpen, "the advisors wait too");
        Assert.AreEqual(DisplayStyle.Flex, Layer.style.display.value, "its layer takes the clicks over the map");
        flight.End();
        Assert.IsFalse(FlightOpen);
        Assert.AreEqual(1, flight.Stops, "stopped once it ended");
        Assert.IsTrue(RaidOpen, "the card follows the flight");
        Assert.IsFalse(root.Advisor.IsOpen);
        UiTestKit.Press(Ui.Q<Button>("raid-ok"));
        Assert.IsTrue(root.Advisor.InChain, "the card's OK starts the turn 1 advisor chain, as before");
        Assert.AreEqual(1, flight.Plays, "once per New game");
        Assert.AreEqual(hash, root.Bridge.State.Hash(), "presentation only: the flight changes no game state");
        Assert.AreEqual(0, root.Bridge.State.Turn);
    }

    [Test]
    public void TheOpeningComesFirstThenTheFlightThenTheCard()
    {
        var flight = new FakeFlight();
        Open(flight, opening: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(OpeningOpen, "the opening plays first");
        Assert.AreEqual(0, flight.Plays, "nothing flies under the opening");
        UiTestKit.Press(Ui.Q<Button>("opening-skip"));
        Assert.AreEqual(1, flight.Plays, "the flight follows the opening");
        Assert.IsTrue(FlightOpen); Assert.IsFalse(RaidOpen);
        flight.End();
        Assert.IsTrue(RaidOpen, "then the card");
    }

    [UnityTest]
    public IEnumerator ASavedGamesNewGameFliesItOnceAndContinueNever()
    {
        var flight = new FakeFlight();
        Open(flight, saved: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        yield return null;
        Assert.AreEqual(0, flight.Plays, "one press only asks to confirm");
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        yield return null;
        Assert.AreEqual(1, flight.Plays, "the confirmed New game flies it once: the start screen's close and New game's Started are one game");
        Assert.IsTrue(FlightOpen);
        flight.End();
        Assert.IsTrue(RaidOpen);
        UiTestKit.Press(Ui.Q<Button>("raid-ok"));
        Assert.AreEqual(1, flight.Plays, "not again after the card");

        var again = new FakeFlight();
        Open(again, saved: true);
        yield return null;
        UiTestKit.Press(Ui.Q<Button>("start-continue"));
        yield return null;
        Assert.IsFalse(Hud.ClassListContains(StartScreenPresenter.OpenClass), "precondition: Continue closed the start screen");
        Assert.AreEqual(0, again.Plays, "Continue never flies it");
        Assert.IsFalse(FlightOpen); Assert.IsFalse(RaidOpen);
    }

    [Test]
    public void AClickSkipsTheFlightAndTheCardStillFollows()
    {
        var flight = new FakeFlight();
        Open(flight);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(FlightOpen);
        using (var down = PointerDownEvent.GetPooled()) { down.target = Layer; Layer.SendEvent(down); }
        Assert.IsFalse(FlightOpen, "a click skips it");
        Assert.AreEqual(1, flight.Stops, "the Dakotas go back to their loop");
        Assert.IsTrue(RaidOpen, "the card still follows");
        flight.End();                                                    // the skipped flight's own end, late
        Assert.AreEqual(1, flight.Stops, "a late end of a skipped flight does nothing");
        Assert.IsTrue(RaidOpen, "and leaves the card up");
        Assert.AreEqual(1, flight.Plays);
    }

    [Test]
    public void EscapeSkipsTheFlightAndTheCardStillFollows()
    {
        var flight = new FakeFlight();
        var root = Open(flight);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(FlightOpen);
        using (var cancel = NavigationCancelEvent.GetPooled()) { cancel.target = Layer; Layer.SendEvent(cancel); }
        Assert.IsFalse(FlightOpen, "Escape (the UI's Cancel) skips it");
        Assert.AreEqual(1, flight.Stops);
        Assert.IsTrue(RaidOpen, "the card still follows");
        Assert.IsFalse(root.Advisor!.IsOpen);
    }

    [Test]
    public void WithoutVideoAsInTheAutomatedRunsTheCardShowsWithoutTheFlight()
    {
        CardVideo.ForceOffForTests = true;                               // as the smoke, golden and shot runs boot (reset in Clean)
        var flight = new FakeFlight();
        Open(flight);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.AreEqual(0, flight.Plays, "no flight with video off");
        Assert.IsFalse(FlightOpen);
        Assert.AreEqual(DisplayStyle.None, Layer.resolvedStyle.display);
    }

    [Test]
    public void TheToolsBeginNeverFliesIt()
    {
        var flight = new FakeFlight();
        var root = Open(flight);
        root.Begin(Inputs, 0);                                           // the tools' Begin: no start screen
        Assert.AreEqual(0, flight.Plays);
        Assert.IsFalse(FlightOpen);
        Assert.AreEqual(DisplayStyle.None, Layer.resolvedStyle.display);
    }

    [Test]
    public void TheTopBarsNewGameFliesItAgain()
    {
        var flight = new FakeFlight();
        var root = Open(flight);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        flight.End();
        UiTestKit.Press(Ui.Q<Button>("raid-ok"));
        if (root.Advisor!.InChain) root.Advisor.CloseChain();
        Assert.IsFalse(root.Advisor.IsOpen, "precondition: nothing covers the top bar");
        UiTestKit.Press(Ui.Q<Button>("new-game"));
        Assert.AreEqual(1, flight.Plays, "one press only arms New game");
        UiTestKit.Press(Ui.Q<Button>("new-game"));
        Assert.AreEqual(2, flight.Plays, "every New game flies it");
        Assert.IsTrue(FlightOpen);
    }

    [Test]
    public void WithNoDakotaToFlyTheCardShowsAtOnce()
    {
        var flight = new FakeFlight { Available = false };              // the Dakotas not streamed in yet
        Open(flight);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsFalse(FlightOpen);
        Assert.IsTrue(RaidOpen, "the card at once");
        Assert.AreEqual(DisplayStyle.None, Layer.style.display.value, "its layer gone again");
    }

    [Test]
    public void FirstCardInstrumentSkipsTheFlightThenPressesOk()
    {
        // No settings file, as under ?firstcard (RaidCardRoutingTests.FirstCardInstrumentPressesOkBeforeAwaitingTheCard).
        var flight = new FakeFlight();
        var root = Open(flight, settings: false);
        var lines = new List<string>();
        double clock = 0;
        var check = GameRoot.FirstCardCheck(Ui, root.Card!, lines.Add, () => clock++);
        int steps = 0;
        while (check.MoveNext()) Assert.Less(++steps, 12, "the existing clock-bound instrument is finite");
        Assert.AreEqual(1, flight.Plays, "the instrument met the flight");
        Assert.IsFalse(FlightOpen); Assert.IsFalse(RaidOpen);
        Assert.IsTrue(lines.Any(line => line.StartsWith("FIRST CARD OK ", StringComparison.Ordinal)), string.Join("\n", lines));
    }
}
