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

/// <summary>The raid card's place in a new game (owner, 8 Nov 2026, "Film, raid, card, advisors (Recommended)"): New game
/// only, after the opening and before the turn 1 advisor chain; Continue and a loaded game never show it. Booted as a
/// player boots it (the start screen and a settings file), in the Editor, which shows the draft (RaidCardContent.MayPresent).
/// With STATECRAFT_SHOT_DIR set, the layout test also writes the card's stills for the pull request.</summary>
public class RaidCardRoutingTests
{
    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    readonly List<string> directories = new List<string>();
    readonly List<(PanelSettings panel, RenderTexture texture)> panels = new List<(PanelSettings, RenderTexture)>();
    static VisualElement Ui => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
    static VisualElement Hud => Ui.Q("root");
    static bool RaidOpen => Hud.ClassListContains(RaidCardPresenter.OpenClass);
    static bool OpeningOpen => Hud.ClassListContains(OpeningPresenter.OpenClass);
    string NewDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-raid-routing-" + Guid.NewGuid().ToString("N"));
        directories.Add(dir);
        return dir;
    }

    GameRoot Open(bool saved, bool opening = false, string language = "en", int width = 1280, int height = 720, bool settings = true)
    {
        var store = new SaveStore(NewDirectory());
        if (saved) store.WriteAutosave(GameSession.New(Inputs.Content, 23UL, Mode.Historical).Save.Write());
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var panel = Object.Instantiate(doc.panelSettings);
        var texture = new RenderTexture(width, height, 24);
        panel.targetTexture = texture;
        panels.Add((panel, texture));
        doc.panelSettings = panel;   // real layout/focus in EditMode; never mutate the shared PanelSettings asset
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = store; root.Settings = settings ? new SettingsStore(NewDirectory()) : null; root.Language = language;
        root.OpeningInEditMode = opening; root.RaidCardInEditMode = true;
        root.Begin(Inputs, 0, startScreen: true);
        return root;
    }

    [TearDown]
    public void Clean()
    {
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
    public void NewGameShowsTheCardThenItsOkStartsTheAdvisorChain()
    {
        var root = Open(saved: false);
        Assert.IsFalse(RaidOpen, "not on the start screen");
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(RaidOpen, "New game shows the card");
        Assert.IsFalse(root.Advisor!.IsOpen, "the advisors wait for the card");
        Assert.IsFalse(root.AdvisorSession!.Settings.OpeningSeen, "the chain has not been used up under the card");
        var hash = root.Bridge!.State.Hash();
        UiTestKit.Press(Ui.Q<Button>("raid-ok"));
        Assert.IsFalse(RaidOpen);
        Assert.IsTrue(root.Advisor.InChain, "OK hands over to the turn 1 advisor chain");
        Assert.AreEqual(hash, root.Bridge.State.Hash(), "presentation only: the card changes no game state");
        Assert.AreEqual(0, root.Bridge.State.Turn);
    }

    [Test]
    public void TheOpeningComesFirstThenTheCardThenTheAdvisors()
    {
        var root = Open(saved: false, opening: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(OpeningOpen, "the opening plays first");
        Assert.IsFalse(RaidOpen);
        UiTestKit.Press(Ui.Q<Button>("opening-skip"));
        Assert.IsTrue(RaidOpen, "the card follows the opening");
        Assert.IsFalse(root.Advisor!.IsOpen);
        UiTestKit.Press(Ui.Q<Button>("raid-ok"));
        Assert.IsTrue(root.Advisor.InChain);
    }

    [UnityTest]
    public IEnumerator ASavedGamesNewGameShowsTheCardOnceAndContinueNever()
    {
        var root = Open(saved: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        yield return null;
        Assert.IsFalse(RaidOpen, "one press only asks to confirm");
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        yield return null;
        Assert.IsTrue(RaidOpen, "the confirmed New game shows it");
        UiTestKit.Press(Ui.Q<Button>("raid-ok"));
        Assert.IsFalse(RaidOpen, "one OK closes it: the start screen's close and New game's Started show it once");
        Assert.IsTrue(root.Advisor!.InChain);

        Open(saved: true);
        yield return null;
        UiTestKit.Press(Ui.Q<Button>("start-continue"));
        yield return null;
        Assert.IsFalse(Hud.ClassListContains(StartScreenPresenter.OpenClass), "precondition: Continue closed the start screen");
        Assert.IsFalse(RaidOpen, "Continue never shows it");
    }

    [Test]
    public void TheTopBarsNewGameShowsItAgain()
    {
        var root = Open(saved: false);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        UiTestKit.Press(Ui.Q<Button>("raid-ok"));
        if (root.Advisor!.InChain) root.Advisor.CloseChain();
        Assert.IsFalse(root.Advisor.IsOpen, "precondition: nothing covers the top bar");
        UiTestKit.Press(Ui.Q<Button>("new-game"));
        Assert.IsFalse(RaidOpen, "one press only arms New game");
        UiTestKit.Press(Ui.Q<Button>("new-game"));
        Assert.IsTrue(RaidOpen, "every New game shows it");
    }

    [Test]
    public void AutomatedRunsAndTheToolsBeginNeverShowIt()
    {
        var root = Open(saved: false);
        root.Begin(Inputs, 0);                                           // the tools' Begin: no start screen
        Assert.IsFalse(RaidOpen);
        Assert.AreEqual(DisplayStyle.None, Ui.Q("raid-layer").resolvedStyle.display);
    }

    [Test]
    public void FirstCardInstrumentPressesOkBeforeAwaitingTheCard()
    {
        // No settings file, as under ?firstcard: there the advisors never show by themselves (GameRoot's autoShow), so the
        // turn 1 chain does not collapse the card the instrument waits for.
        var root = Open(saved: false, settings: false);
        var lines = new List<string>();
        double clock = 0;
        var check = GameRoot.FirstCardCheck(Ui, root.Card!, lines.Add, () => clock++);
        int steps = 0;
        while (check.MoveNext()) Assert.Less(++steps, 12, "the existing clock-bound instrument is finite");
        Assert.IsFalse(RaidOpen);
        Assert.IsTrue(lines.Any(line => line.StartsWith("FIRST CARD OK ", StringComparison.Ordinal)), string.Join("\n", lines));
    }

    /// <summary>The card fits a desktop and a phone window in both languages, OK in view; with STATECRAFT_SHOT_DIR set it
    /// writes raid-card-&lt;lang&gt;-&lt;w&gt;x&lt;h&gt;.png there. EditMode decodes no video, so a still frame of the clip
    /// named by STATECRAFT_RAID_FRAME (a PNG) stands in for it in the stills, and only there.</summary>
    [UnityTest]
    public IEnumerator TheCardFitsTheWindowWithOkInView(
        [Values("en", "he")] string language, [Values(1920, 390)] int width)
    {
        int height = width == 1920 ? 1080 : 844;
        LogAssert.ignoreFailingMessages = width == 390;   // main's Hebrew 390x844 logs "Layout update is struggling" (StartScreenLanguageShots)
        Open(saved: false, language: language, width: width, height: height);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var card = Ui.Q("raid-card");
        for (int i = 0; i < 60 && !(card.worldBound.height > 0); i++) yield return null;
        var frame = Environment.GetEnvironmentVariable("STATECRAFT_RAID_FRAME");
        var media = Ui.Q("raid-media");
        if (!string.IsNullOrEmpty(frame) && File.Exists(frame))
        {
            var still = new Texture2D(2, 2);
            still.LoadImage(File.ReadAllBytes(frame));
            media.style.display = DisplayStyle.Flex;
            media.style.backgroundImage = new StyleBackground(still);
            Ui.Q("raid-ai").style.display = DisplayStyle.Flex;
        }
        for (int i = 0; i < 10; i++) yield return null;
        var ok = Ui.Q<Button>("raid-ok");
        var screen = Ui.worldBound;
        Assert.IsTrue(RaidOpen);
        Assert.Greater(ok.worldBound.height, 0f, "OK is laid out");
        Assert.IsTrue(screen.Contains(ok.worldBound.min) && screen.Contains(ok.worldBound.max - Vector2.one), $"OK {ok.worldBound} in {screen}");
        Assert.IsTrue(card.worldBound.xMin >= screen.xMin - 0.5f && card.worldBound.xMax <= screen.xMax + 0.5f, $"card {card.worldBound} in {screen}");
        if (Environment.GetEnvironmentVariable("STATECRAFT_SHOT_DIR") is string dir && dir.Length > 0)
        {
            Directory.CreateDirectory(dir);
            var rt = (RenderTexture)Object.FindFirstObjectByType<UIDocument>().panelSettings.targetTexture;
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(dir, $"raid-card-{language}-{width}x{height}.png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
