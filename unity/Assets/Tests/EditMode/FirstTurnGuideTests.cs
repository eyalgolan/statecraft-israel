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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The first-turn guide (#42; Q-ONB-1, owner, 17 October 2026, #784): it shows by itself on the first new game only,
/// never on a Continue or a second game; it can be skipped and opened again from How to play; every step's target is on
/// screen at 1280x720 and 390x844 in English and Hebrew, with the panel clear of it; and it changes neither the sim nor the
/// save.</summary>
public class FirstTurnGuideTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    /// <summary>The settlements' names for the info panel's title (InfoPanelLayoutTests' list).</summary>
    static Dictionary<string, string> Names() => SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName)))
        .ToDictionary(t => t.Id, t => t.Name);

    readonly List<string> temp = new List<string>();
    string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-guide-" + Guid.NewGuid().ToString("N"));
        temp.Add(dir);
        return dir;
    }

    [TearDown]
    public void Clean()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);   // after the scene tests
        foreach (var d in temp) { try { if (Directory.Exists(d)) Directory.Delete(d, true); } catch (Exception) { } }
        temp.Clear();
    }

    // --- When it shows ----------------------------------------------------------------------------------------------

    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    static VisualElement Ui => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;

    static GameRoot OpenScene(SaveStore store, SettingsStore settings)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = store; root.Settings = settings;
        return root;
    }

    /// <summary>Map.unity as a player boots it: the guide waits for the start screen, opens when New Game closes it (at its
    /// first step, the card hidden behind "Decision waiting") and notes in the settings file that it has shown; the next new
    /// game, from the start screen or the top bar, does not show it again.</summary>
    [Test]
    public void ShowsOnTheFirstNewGameOnly()
    {
        var settings = new SettingsStore(TempDir());
        var game = OpenScene(new SaveStore(TempDir()), settings);
        game.Begin(Inputs, 0, startScreen: true);                       // a fresh profile: no save
        Assert.IsFalse(game.Guide!.IsOpen, "not under the start screen");
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(game.Guide!.IsOpen, "New Game closes the screen and the guide opens");
        Assert.AreEqual(0, game.Guide.Step);
        Assert.IsTrue(game.Card!.Shown != null && game.Card.Collapsed, "the opening card waits behind Decision waiting");
        Assert.AreEqual("seen", settings.Read(FirstTurnGuidePresenter.SeenKey));
        UiTestKit.Press(Ui.Q<Button>("guide-skip"));
        Assert.IsFalse(game.Guide.IsOpen);
        Assert.IsFalse(game.Card.Collapsed, "skipping puts the card back");

        game.Begin(Inputs, 0, startScreen: true);                       // a reload with no save: a new game again
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsFalse(game.Guide!.IsOpen, "a second new game: not again");
        UiTestKit.Press(Ui.Q<Button>("new-game")); UiTestKit.Press(Ui.Q<Button>("new-game"));
        Assert.IsFalse(game.Guide!.IsOpen, "nor from the top bar's New Game");
    }

    /// <summary>A Continue (a loaded save) never shows it, even before it was ever seen; a new game from the top bar then
    /// does. Automated runs never show it: no start screen (Begin(inputs, n)) or no settings file.</summary>
    [Test]
    public void NeverShowsOnAContinueOrAnAutomatedRun()
    {
        var settings = new SettingsStore(TempDir());
        var store = new SaveStore(TempDir());
        var played = new SimBridge(GameSession.New(C, 9, Mode.Historical), En);
        played.EndTurn(); played.EndTurn();
        store.WriteAutosave(played.Session.Save.Write());
        var game = OpenScene(store, settings);
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-continue"));
        Assert.IsFalse(game.Guide!.IsOpen, "Continue");
        Assert.IsNull(settings.Read(FirstTurnGuidePresenter.SeenKey), "and still unseen");

        game.Begin(Inputs, 0);                                          // the tests' and tools' Begin: no start screen
        UiTestKit.Press(Ui.Q<Button>("new-game")); UiTestKit.Press(Ui.Q<Button>("new-game"));
        Assert.IsFalse(game.Guide!.IsOpen, "an automated run");
        game.Settings = null;                                           // EditMode with no settings file
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsFalse(game.Guide!.IsOpen, "nowhere to remember it: never by itself");

        game.Settings = settings;
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(game.Guide!.IsOpen, "the first new game in this profile");
    }

    // --- Skip, Escape and How to play -------------------------------------------------------------------------------

    sealed class Rig : IDisposable
    {
        public readonly UiTestKit Ui = new UiTestKit();
        public readonly SimBridge Bridge;
        public readonly ScreenNav Nav;
        public readonly EventCardPresenter Card;
        public readonly FirstTurnGuidePresenter Guide;
        readonly List<IDisposable> owned = new List<IDisposable>();

        public Rig(UiText text, SettingsStore? settings = null)
        {
            Ui.Root.Q("root").RemoveFromClassList("loading");
            Bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
            owned.Add(new HudPresenter(Ui.Root, Bridge, text));
            Nav = Own(new ScreenNav(Ui.Root));
            Card = Own(new EventCardPresenter(Ui.Root, Bridge, text, null));
            var info = Own(new InfoPanelPresenter(Ui.Root, Bridge, text, Names()));
            Own(new HowToPlayPresenter(Ui.Root, text));
            Guide = Own(new FirstTurnGuidePresenter(Ui.Root, text, settings, Card,
                () => { if (FirstTurnGuidePresenter.CityHex(Bridge.Content, Bridge.State) is (int q, int r)) info.Show(q, r); }, () => Nav.Close()));
        }

        T Own<T>(T d) where T : IDisposable { owned.Add(d); return d; }

        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Ui.Dispose();
        }
    }

    /// <summary>Skip at any step closes it; Next walks the five steps and Done closes it; Escape skips; How to play's button
    /// opens it again at its first step, closing that screen; each step shows its own strings.</summary>
    [Test]
    public void CanBeSkippedAtAnyStepAndReopened([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        using var rig = new Rig(text, new SettingsStore(TempDir()));
        using var objectives = new ObjectivesPresenter(rig.Ui.Root, rig.Bridge, text);
        rig.Guide.NewGame();
        Assert.IsTrue(rig.Guide.IsOpen);
        for (int step = 0; step < FirstTurnGuidePresenter.Steps.Length; step++)
        {
            var key = FirstTurnGuidePresenter.Steps[step];
            Assert.AreEqual(step, rig.Guide.Step);
            Assert.AreEqual(text.Format("guide.step", step + 1, 5), rig.Ui.Q<Label>("guide-count").text);
            Assert.AreEqual(text["guide." + key + ".title"], rig.Ui.Q<Label>("guide-title").text);
            Assert.AreEqual(text["guide." + key + ".text"], rig.Ui.Q<Label>("guide-text").text);
            Assert.AreEqual(text[step == 4 ? "guide.done" : "guide.next"], rig.Ui.Q<Button>("guide-next").text);
            Assert.AreEqual(step != FirstTurnGuidePresenter.EventStep, rig.Card.Collapsed, $"step {step + 1}: the card shows on its own step only");
            if (key == "objectives")
            {
                // #791: from the guide's last step the real Objectives button opens the new parts and the standalone goal.
                CollectionAssert.AreEquivalent(new[] { "railway", "burma_road", "yishuvim", "absorb_immigrants" },
                    C.Objectives.Select(o => o.Id).ToArray(), "the current objectives, not the retired standalone first yishuv");
                rig.Ui.Press("nav-objectives");
                Assert.AreEqual("objectives", rig.Nav.Current, "the guide's Objectives destination opens");
                var rows = rig.Ui.Q<ScrollView>("objectives-rows").Query(className: "objectives-row").ToList();
                CollectionAssert.AreEqual(C.Objectives.Select(o => "objectives-row-" + o.Id).ToArray(),
                    rows.Select(row => row.name).ToArray(), "every current objective in the helper's order");
                for (int i = 0; i < C.Objectives.Count; i++)
                {
                    var expectedName = hebrew ? C.Objectives[i].NameHe : C.Objectives[i].Name;
                    Assert.IsFalse(string.IsNullOrWhiteSpace(expectedName), "the objective has a name in this language");
                    Assert.AreEqual(expectedName, rows[i].Q<Label>(className: "objectives-name").text,
                        C.Objectives[i].Id + ": the displayed name matches content in this language");
                }
                rig.Nav.Close();
            }
            rig.Ui.Press("guide-next");
        }
        Assert.IsFalse(rig.Guide.IsOpen, "Done closes it");
        Assert.IsFalse(rig.Card.Collapsed, "and the card is as it was");

        for (int at = 0; at < FirstTurnGuidePresenter.Steps.Length; at++)
        {
            rig.Ui.Press("nav-how-to-play");
            Assert.AreEqual("how-to-play", rig.Nav.Current);
            Assert.AreEqual(text["how_to_play.guide"], rig.Ui.Q<Button>("how-to-play-guide").text);
            rig.Ui.Press("how-to-play-guide");
            Assert.IsTrue(rig.Guide.IsOpen, "How to play opens it again");
            Assert.IsNull(rig.Nav.Current, "and closes itself");
            Assert.AreEqual(0, rig.Guide.Step);
            for (int i = 0; i < at; i++) rig.Ui.Press("guide-next");
            rig.Ui.Press("guide-skip");
            Assert.IsFalse(rig.Guide.IsOpen, $"Skip at step {at + 1}");
            Assert.IsFalse(rig.Card.Collapsed, $"the card is back after a Skip at step {at + 1}");
        }

        rig.Guide.Open();
        Assert.IsTrue(rig.Guide.Escape(), "Escape skips it");
        Assert.IsFalse(rig.Guide.IsOpen);
        Assert.IsFalse(rig.Guide.Escape(), "and does nothing when it is closed");

        rig.Card.Collapse();                                           // the player hid the card before opening the guide
        rig.Guide.Open();
        for (int i = 0; i < FirstTurnGuidePresenter.EventStep; i++) rig.Ui.Press("guide-next");
        Assert.IsFalse(rig.Card.Collapsed, "the event step shows the card");
        rig.Ui.Press("guide-skip");
        Assert.IsTrue(rig.Card.Collapsed, "and Skip hides it again, as the player left it");
    }

    /// <summary>End turn pressed with the guide open (it takes no pointer outside its panel, so the HUD stays usable): the
    /// guide closes as the turn advances, at any step, and the new turn's card is left to its own rule (a new event opens),
    /// not hidden or shown by what the guide found on the old card (review of #1209, follow-up 1).</summary>
    [Test]
    public void EndTurnClosesTheGuide([Values(0, 1, FirstTurnGuidePresenter.EventStep)] int step, [Values(false, true)] bool cardHidden)
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(game.Guide!.IsOpen, "precondition: the first new game opens the guide");
        game.Guide.Close();
        var card = game.Card!;
        Assert.IsNotNull(card.Shown, "precondition: a new game opens on a card");
        var first = card.Shown!.Id;
        if (cardHidden) card.Collapse();
        game.Guide.Open();
        for (int i = 0; i < step; i++) UiTestKit.Press(Ui.Q<Button>("guide-next"));
        var turn = game.Bridge!.State.Turn;
        UiTestKit.Press(Ui.Q<Button>("end-turn"));
        Assert.AreEqual(turn + 1, game.Bridge.State.Turn, "precondition: End turn ran the turn");
        Assert.IsFalse(game.Guide.IsOpen, "the guide closes when the turn advances");
        Assert.AreEqual(-1, game.Guide.Step);
        Assert.IsFalse(Ui.Q("root").ClassListContains(FirstTurnGuidePresenter.OpenClass));
        if (card.Shown != null && card.Shown.Id != first)
            Assert.IsFalse(card.Collapsed, $"the new turn's card ({card.Shown.Id}) opens by its own rule");
        else if (card.Shown != null)
            Assert.AreEqual(cardHidden, card.Collapsed, "the same card is put back as the guide found it");
        Assert.AreEqual(card.Shown != null && !card.Collapsed, Ui.Q("root").ClassListContains(EventCardPresenter.OpenClass),
            "the backdrop shows with an open card only");
    }

    /// <summary>The guide shows its city on steps 2 and 3; Done, Skip or Escape puts back the selection it found (none, a
    /// hex or a unit) with the info panel and the map's outline; a selection the player made meanwhile is kept (review of
    /// #1209, follow-up 3).</summary>
    [Test]
    public void ClosingPutsBackTheSelectionItFound()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var guide = game.Guide!; var info = game.Info!; var units = game.Units!; var bridge = game.Bridge!;
        var map = Object.FindFirstObjectByType<MapScene>();
        var city = FirstTurnGuidePresenter.CityHex(bridge.Content, bridge.State)!.Value;
        bool Outlined() { var t = map.Terrain.transform.Find(MapScene.SelectionName); return t != null && t.GetComponent<MeshRenderer>().enabled; }
        void ToCity() { UiTestKit.Press(Ui.Q<Button>("guide-next")); Assert.AreEqual(city, info.Selected, "step 2 shows the guide's city"); }
        void Click(int q, int r) => GameRoot.OnHexClicked(map, units, info, q, r);

        Assert.IsTrue(guide.IsOpen && info.Selected == null && !info.ShowsUnit, "precondition: a new game, nothing selected");
        ToCity();
        UiTestKit.Press(Ui.Q<Button>("guide-skip"));
        Assert.IsNull(info.Selected, "Skip: nothing selected again");
        Assert.IsFalse(info.ShowsUnit);
        Assert.IsNull(units.Selected);
        Assert.IsFalse(Outlined(), "and no outline on the map");

        var hex = bridge.Content.Hexes.Where(h => h.Land).Select(h => (h.Q, h.R)).First(h => h != city && units.UnitToSelectAt(h.Item1, h.Item2) == null);
        Click(hex.Item1, hex.Item2);
        Assert.AreEqual(hex, info.Selected, "precondition: a hex selected");
        guide.Open(); ToCity();
        for (int i = 1; i < FirstTurnGuidePresenter.Steps.Length; i++) UiTestKit.Press(Ui.Q<Button>("guide-next"));
        Assert.IsFalse(guide.IsOpen, "Done");
        Assert.AreEqual(hex, info.Selected, "Done: the hex the guide found");
        Assert.IsTrue(Outlined(), "outlined again");

        Assert.IsNotEmpty(units.Tokens, "precondition: units in play");
        var token = units.Tokens[0];
        Click(token.Q, token.R);
        var unit = units.Selected;
        Assert.IsTrue(unit != null && info.ShowsUnit, "precondition: a unit selected");
        guide.Open(); ToCity();
        Assert.IsTrue(guide.Escape());
        Assert.AreEqual(unit, units.Selected, "Escape: the unit the guide found");
        Assert.IsTrue(info.ShowsUnit, "its card in the info panel");
        Assert.IsFalse(Outlined(), "with its ring, not a hex outline");

        Click(hex.Item1, hex.Item2);
        guide.Open(); ToCity();
        var other = bridge.Content.Hexes.Where(h => h.Land).Select(h => (h.Q, h.R)).First(h => h != city && h != hex && units.UnitToSelectAt(h.Item1, h.Item2) == null);
        Click(other.Item1, other.Item2);                                // the player picks a hex under the open guide
        UiTestKit.Press(Ui.Q<Button>("guide-skip"));
        Assert.AreEqual(other, info.Selected, "a selection the player made meanwhile stays");
    }

    // --- Presentation only ------------------------------------------------------------------------------------------

    /// <summary>The whole guide, by itself, through How to play, through every step, Skip and Escape: the state hash, the
    /// session, the queued orders and the save file are what they were.</summary>
    [Test]
    public void ChangesNeitherTheSimNorTheSave()
    {
        var store = new SaveStore(TempDir());
        var settings = new SettingsStore(TempDir());
        var game = OpenScene(store, settings);
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(game.Guide!.IsOpen);
        var bridge = game.Bridge!;
        bridge.EndTurn();                                               // a turn played, so there is a save to compare
        if (Ui.Q("root").ClassListContains(TurnReportPresenter.OpenClass)) UiTestKit.Press(Ui.Q<Button>("turn-report-close"));
        var session = bridge.Session;
        var hash = bridge.State.Hash();
        var save = store.ReadAutosave();
        Assert.IsNotNull(save, "precondition: the turn was saved");
        var queued = bridge.Queued.Count;

        void Next(int times) { for (int i = 0; i < times; i++) UiTestKit.Press(Ui.Q<Button>("guide-next")); }
        void FromHowToPlay() { UiTestKit.Press(Ui.Q<Button>("nav-how-to-play")); UiTestKit.Press(Ui.Q<Button>("how-to-play-guide")); }
        Next(FirstTurnGuidePresenter.Steps.Length);                     // the guide that opened by itself, to Done
        Assert.IsFalse(game.Guide.IsOpen);
        FromHowToPlay(); Next(2); UiTestKit.Press(Ui.Q<Button>("guide-skip"));
        FromHowToPlay(); Next(3); Assert.IsTrue(game.Guide.Escape());
        Assert.IsFalse(game.Guide.IsOpen);
        Assert.AreSame(session, bridge.Session, "the same game");
        Assert.AreEqual(hash, bridge.State.Hash(), "the state hash");
        Assert.AreEqual(queued, bridge.Queued.Count, "no order queued");
        Assert.AreEqual(save, store.ReadAutosave(), "the save file");
    }

    // --- Where each step points --------------------------------------------------------------------------------------

    /// <summary>Every step's target is on screen, in the window, with the gold ring round it (inside the window) and the panel
    /// inside the window and clear of it, at 1920x1080 (the one-row bar), 1280x720 and 390x844 (the phone the three-row bar
    /// is tuned for), in English and Hebrew, on a new game's first turn (the opening card open). Step 1's ring keeps clear of
    /// End turn's border, so the two read as two boxes (review of #1209, follow-up 2).</summary>
    [UnityTest]
    public IEnumerator EveryStepPointsAtAVisibleElement()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (390, 844) })
                yield return EachStep(w, h, hebrew, (root, guide, step) =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h} step {step + 1} ({FirstTurnGuidePresenter.Steps[step]})";
                    var window = root.worldBound;
                    var targets = guide.Targets(step);
                    if (FirstTurnGuidePresenter.Steps[step] == "objectives" &&
                        (targets.Count != 1 || targets[0].name != "nav-objectives"))
                        problems.Add($"{at}: the last step must point at the Objectives button");
                    if (targets.Count == 0) { problems.Add($"{at}: no target shows"); return; }
                    var area = guide.TargetArea()!.Value;
                    if (!Inside(area, window)) problems.Add($"{at}: the target {area} leaves the window {window}");
                    var ring = root.Q("guide-ring");
                    if (ring.resolvedStyle.display != DisplayStyle.Flex) problems.Add($"{at}: no ring");
                    else
                    {
                        if (!Inside(area, ring.worldBound)) problems.Add($"{at}: the ring {ring.worldBound} is not round the target {area}");
                        if (!Inside(ring.worldBound, window)) problems.Add($"{at}: the ring {ring.worldBound} leaves the window {window}");
                        // Step 1's ring round Attention must read as its own box, not one with End turn's gold border under it.
                        var endTurn = root.Q("end-turn").worldBound;
                        if (step == 0 && Gap(ring.worldBound, endTurn) < RingClearPx)
                            problems.Add($"{at}: the ring {ring.worldBound} is {Gap(ring.worldBound, endTurn):0.#} px from End turn {endTurn} (under {RingClearPx})");
                    }
                    var panel = root.Q("guide-panel").worldBound;
                    if (!(panel.width > 0) || !Inside(panel, window)) problems.Add($"{at}: the panel {panel} leaves the window {window}");
                    if (panel.Overlaps(area)) problems.Add($"{at}: the panel {panel} covers the target {area}");
                    foreach (var name in new[] { "guide-title", "guide-text", "guide-skip", "guide-next" })
                        if (!Inside(root.Q(name).worldBound, panel)) problems.Add($"{at}: {name} {root.Q(name).worldBound} leaves the panel {panel}");
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>The least clear space between the ring and End turn for them to read as two boxes, in panel px.</summary>
    const float RingClearPx = 4f;

    /// <summary>The clear space between two rects (the larger of the horizontal and vertical gaps; negative when they overlap).</summary>
    static float Gap(Rect a, Rect b) =>
        Mathf.Max(Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax), Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));

    static bool Inside(Rect inner, Rect outer) =>
        inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;

    /// <summary>The real Game.uxml on the real PanelSettings at w x h with the HUD, the opening card and the info panel on a
    /// new game; the guide opened, and <paramref name="check"/> run at each step once the layout has settled.</summary>
    static IEnumerator EachStep(int w, int h, bool hebrew, Action<VisualElement, FirstTurnGuidePresenter, int> check)
    {
        var text = hebrew ? He : En;
        var rt = new RenderTexture(w, h, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("FirstTurnGuideTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        root.Q("root").RemoveFromClassList("loading");
        if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
        var hud = new HudPresenter(root, bridge, text);
        var nav = new ScreenNav(root);
        var card = new EventCardPresenter(root, bridge, text, null);
        var info = new InfoPanelPresenter(root, bridge, text, Names());
        var guide = new FirstTurnGuidePresenter(root, text, null, card,
            () => { if (FirstTurnGuidePresenter.CityHex(bridge.Content, bridge.State) is (int q, int r)) info.Show(q, r); }, () => nav.Close());
        try
        {
            Assert.IsNotNull(card.Shown, "precondition: a new game opens on a card");
            guide.Open();
            for (int step = 0; step < FirstTurnGuidePresenter.Steps.Length; step++)
            {
                if (step > 0) guide.Next();
                for (int i = 0; i < 20 && guide.Targets(step).Count == 0; i++) yield return null;
                for (int i = 0; i < 3; i++) yield return null;   // the layout settles
                guide.Place();
                for (int i = 0; i < 2; i++) yield return null;   // the panel's dock follows the ring's side
                check(root, guide, step);
            }
        }
        finally
        {
            guide.Dispose(); info.Dispose(); card.Dispose(); nav.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }
}
