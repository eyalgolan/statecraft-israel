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
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The invasion card's place in a new game (#3142; owner, 11 Nov 2026: "Replace the 2 advisor steps (Recommended)",
/// "Info only, turn 1 only (Recommended)"): after the raid card (or New game, without one) and before the turn 1 advisor
/// chain, which its Continue starts. Each front's step selects that army's token and centres the camera on it; closing the
/// card clears the selection and puts the camera back. Booted as a player boots it, in the Editor, which shows the draft
/// (InvasionCardContent.MayPresent). An Arab force's token waits for its Collection figure, which edit mode never streams, so
/// each test answers at once with a stand-in (UnitBadgePipsTests' way) but the one that checks a front without its token.
/// With STATECRAFT_SHOT_DIR set, the layout test also writes the card's stills.</summary>
public class InvasionCardRoutingTests
{
    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    readonly List<string> directories = new List<string>();
    readonly List<(PanelSettings panel, RenderTexture texture)> panels = new List<(PanelSettings, RenderTexture)>();
    GameObject? standIn;
    static VisualElement Ui => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
    static VisualElement Hud => Ui.Q("root");
    static bool InvasionOpen => Hud.ClassListContains(InvasionCardPresenter.OpenClass);
    static bool RaidOpen => Hud.ClassListContains(RaidCardPresenter.OpenClass);
    static CameraRig Rig => Object.FindFirstObjectByType<CameraRig>();
    string NewDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-invasion-routing-" + Guid.NewGuid().ToString("N"));
        directories.Add(dir);
        return dir;
    }

    GameRoot Open(bool raid = true, string language = "en", int width = 1280, int height = 720, bool settings = true)
    {
        var store = new SaveStore(NewDirectory());
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var panel = Object.Instantiate(doc.panelSettings);
        var texture = new RenderTexture(width, height, 24);
        panel.targetTexture = texture; panel.clearColor = true; panel.colorClearValue = Color.black;   // each still shows one step, not the last ones under it
        panels.Add((panel, texture));
        doc.panelSettings = panel;   // real layout/focus in EditMode; never mutate the shared PanelSettings asset
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = store; root.Settings = settings ? new SettingsStore(NewDirectory()) : null; root.Language = language;
        root.RaidCardInEditMode = raid; root.InvasionCardInEditMode = true;
        root.Begin(Inputs, 0, startScreen: true);
        return root;
    }

    [SetUp]
    public void FiguresAnswerAtOnce()
    {
        standIn = GameObject.CreatePrimitive(PrimitiveType.Cube); standIn.transform.position = Vector3.one * -100000f;
        UnitFigures.Source = (_, __, done) => done(standIn); UnitFigures.Forget();
    }

    /// <summary>Draws the Arab forces' tokens now that their figures have answered, as the next render's redraw would.</summary>
    static void DrawTheArabTokens(GameRoot root)
    {
        root.Units!.Show(root.Bridge!.Content, root.Bridge.State);
        Assert.IsTrue(root.Units.Tokens.Any(t => t.Token.Unit.Id == "egyptian_expeditionary_force"), "precondition: the enemy tokens are drawn");
    }

    [TearDown]
    public void Clean()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        UnitFigures.Source = null; UnitFigures.Forget();
        if (standIn != null) Object.DestroyImmediate(standIn);
        standIn = null;
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
    public void TheRaidCardsOkShowsTheInvasionCardThenItsContinueStartsTheAdvisorChain()
    {
        var root = Open();
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(RaidOpen, "the raid card first");
        Assert.IsFalse(InvasionOpen);
        var hash = root.Bridge!.State.Hash();
        UiTestKit.Press(Ui.Q<Button>("raid-ok"));
        DrawTheArabTokens(root);
        Assert.IsTrue(InvasionOpen, "the raid card's OK shows the invasion card");
        Assert.IsFalse(root.Advisor!.IsOpen, "the advisors wait for the card");
        Assert.IsFalse(root.AdvisorSession!.Settings.OpeningSeen, "the chain has not been used up under the card");
        var start = (Rig.LookAt, Rig.Distance);
        var data = InvasionCardContent.Load()!;
        var looks = new List<Vector3>();
        Assert.IsNull(root.Units!.Selected, "the intro selects nothing");
        for (int i = 1; i < data.Steps.Count; i++)
        {
            UiTestKit.Press(Ui.Q<Button>("invasion-next"));
            Assert.AreEqual(data.Steps[i].Focus, root.Units.Selected, "step " + i + " selects that army's token");
            Assert.LessOrEqual(Rig.Distance, AdvisorTargets.MoveStepDistanceM, "close enough to see it");
            looks.Add(Rig.LookAt);
        }
        Assert.AreEqual(looks.Count, looks.Distinct().Count(), "the camera moves to each front: " + string.Join(" ", looks));
        Assert.IsTrue(InvasionOpen, "open until Continue");
        UiTestKit.Press(Ui.Q<Button>("invasion-next"));
        Assert.IsFalse(InvasionOpen);
        Assert.IsNull(root.Units.Selected, "closing clears the tour's selection");
        Assert.AreEqual(start.LookAt, Rig.LookAt, "the camera goes back where the game started");
        Assert.AreEqual(start.Distance, Rig.Distance);
        Assert.IsTrue(root.Advisor.InChain, "Continue hands over to the turn 1 advisor chain");
        Assert.AreEqual(0, root.Advisor.ChainStep);
        Assert.AreEqual(hash, root.Bridge.State.Hash(), "presentation only: the card changes no game state");
        Assert.AreEqual(0, root.Bridge.State.Turn);
    }

    [Test]
    public void AFrontWhoseTokenIsNotDrawnYetStillMovesTheCameraToItsStartingHex()
    {
        UnitFigures.Source = null; UnitFigures.Forget();   // edit mode's own way: the Arab figures never arrive
        var root = Open(raid: false);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var data = InvasionCardContent.Load()!;
        var content = root.Bridge!.Content;
        var frame = Object.FindFirstObjectByType<MapScene>().Frame;
        for (int i = 1; i < data.Steps.Count; i++)
        {
            UiTestKit.Press(Ui.Q<Button>("invasion-next"));
            Assert.IsFalse(root.Units!.Tokens.Any(t => t.Token.Unit.Id == data.Steps[i].Focus), "precondition: no token for " + data.Steps[i].Focus);
            Assert.IsNull(root.Units.Selected);
            var force = content.StartingForces.Single(f => f.Row == data.Steps[i].Focus);
            var hex = content.HexAt(force.Q, force.R)!;
            var want = frame.ToWorld(hex.Lon, hex.Lat, 0);
            Assert.Less(Vector2.Distance(new Vector2(want.x, want.z), new Vector2(Rig.LookAt.x, Rig.LookAt.z)), 1f, $"step {i}: the camera on {force.Row}'s starting hex {want}, not {Rig.LookAt}");
            Assert.LessOrEqual(Rig.Distance, AdvisorTargets.MoveStepDistanceM);
        }
    }

    [Test]
    public void WithoutTheRaidCardNewGameShowsTheInvasionCardFirst()
    {
        var root = Open(raid: false);
        Assert.IsFalse(InvasionOpen, "not on the start screen");
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(InvasionOpen);
        Assert.IsFalse(root.Advisor!.IsOpen);
    }

    /// <summary>Escape as GameRoot's Escape chain sends it (invasion.Escape(), as RaidCardTests drives the raid card's): edit
    /// mode's Time.frameCount does not move, so the Cancel event's same-frame guard would hold it.</summary>
    [Test]
    public void EscapeOnAFrontClosesTheCardRestoresTheCameraAndStartsTheAdvisors()
    {
        var root = Open(raid: false);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var start = (Rig.LookAt, Rig.Distance);
        UiTestKit.Press(Ui.Q<Button>("invasion-next"));
        Assert.AreNotEqual(start.LookAt, Rig.LookAt, "precondition: on a front");
        Assert.IsTrue(root.Invasion!.Escape(), "the open card takes Escape");
        Assert.IsFalse(InvasionOpen, "Escape closes the card");
        Assert.IsNull(root.Units!.Selected);
        Assert.AreEqual(start.LookAt, Rig.LookAt); Assert.AreEqual(start.Distance, Rig.Distance);
        Assert.IsTrue(root.Advisor!.InChain);
    }

    [Test]
    public void AutomatedRunsAndTheToolsBeginNeverShowIt()
    {
        var root = Open();
        root.Begin(Inputs, 0);                                           // the tools' Begin: no start screen
        Assert.IsFalse(InvasionOpen);
        Assert.AreEqual(DisplayStyle.None, Ui.Q("invasion-layer").resolvedStyle.display);
    }

    [Test]
    public void FirstCardInstrumentPassesThroughTheInvasionCard()
    {
        // No settings file, as under ?firstcard (RaidCardRoutingTests): the advisors never show by themselves.
        var root = Open(settings: false);
        var lines = new List<string>();
        double clock = 0;
        var check = GameRoot.FirstCardCheck(Ui, root.Card!, lines.Add, () => clock++);
        int steps = 0;
        while (check.MoveNext()) Assert.Less(++steps, 12, "the existing clock-bound instrument is finite");
        Assert.IsFalse(RaidOpen); Assert.IsFalse(InvasionOpen);
        Assert.IsTrue(lines.Any(line => line.StartsWith("FIRST CARD OK ", StringComparison.Ordinal)), string.Join("\n", lines));
    }

    /// <summary>The card fits a desktop and a phone window in both languages, Next in view, on each step; with
    /// STATECRAFT_SHOT_DIR set it writes invasion-card-&lt;lang&gt;-&lt;w&gt;x&lt;h&gt;-&lt;step&gt;.png there (the UI panel
    /// only: EditMode renders the panel to a texture, not the map's camera).</summary>
    [UnityTest]
    public IEnumerator TheCardFitsTheWindowWithNextInView(
        [Values("en", "he")] string language, [Values(1920, 390)] int width)
    {
        int height = width == 1920 ? 1080 : 844;
        LogAssert.ignoreFailingMessages = width == 390;   // main's Hebrew 390x844 logs "Layout update is struggling" (StartScreenLanguageShots)
        Open(raid: false, language: language, width: width, height: height);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var card = Ui.Q("invasion-card");
        int count = InvasionCardContent.Load()!.Steps.Count;
        for (int step = 0; step < count; step++)
        {
            for (int i = 0; i < 60 && !(card.worldBound.height > 0); i++) yield return null;
            for (int i = 0; i < 10; i++) yield return null;
            var next = Ui.Q<Button>("invasion-next");
            var screen = Ui.worldBound;
            Assert.IsTrue(InvasionOpen);
            Assert.Greater(next.worldBound.height, 0f, "Next is laid out");
            Assert.IsTrue(screen.Contains(next.worldBound.min) && screen.Contains(next.worldBound.max - Vector2.one), $"step {step}: Next {next.worldBound} in {screen}");
            Assert.IsTrue(card.worldBound.xMin >= screen.xMin - 0.5f && card.worldBound.xMax <= screen.xMax + 0.5f, $"step {step}: card {card.worldBound} in {screen}");
            if (Environment.GetEnvironmentVariable("STATECRAFT_SHOT_DIR") is string dir && dir.Length > 0)
            {
                Directory.CreateDirectory(dir);
                var rt = (RenderTexture)Object.FindFirstObjectByType<UIDocument>().panelSettings.targetTexture;
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, $"invasion-card-{language}-{width}x{height}-{step + 1}.png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            UiTestKit.Press(next);
        }
        Assert.IsFalse(InvasionOpen);
    }
}
