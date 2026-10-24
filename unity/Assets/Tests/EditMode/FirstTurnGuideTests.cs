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
    static List<UnitEntry> UnitRows => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));

    /// <summary>The info panel and the guide as GameRoot wires them, without the map: the guide's city (CityHex) and its unit
    /// (UnitToShow, among the tokens a new game draws) shown as a click would show them.</summary>
    static (InfoPanelPresenter info, FirstTurnGuidePresenter guide) PanelAndGuide(VisualElement root, SimBridge bridge, UiText text,
        SettingsStore? settings, EventCardPresenter card, Action closeScreens)
    {
        var tokens = UnitRoster.FromState(UnitRows, bridge.Content, bridge.State, _ => true, (row, a, q, r) => a);
        PlacedToken? shown = null;
        var info = new InfoPanelPresenter(root, bridge, text, Names(), selectedUnit: () => shown);
        var guide = new FirstTurnGuidePresenter(root, text, settings, card,
            () => { if (FirstTurnGuidePresenter.CityHex(bridge.Content, bridge.State) is (int q, int r)) info.Show(q, r); }, closeScreens,
            showUnit: () =>
            {
                if (!(FirstTurnGuidePresenter.UnitToShow(bridge.Content, bridge.State, tokens) is string id)) return;
                shown = new PlacedToken(tokens.First(t => t.Unit.Id == id), new AnchorSpot(0, 0, 0, 0, false));
                info.ShowUnit();
            });
        return (info, guide);
    }

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
            Own(new HowToPlayPresenter(Ui.Root, text));
            var (info, guide) = PanelAndGuide(Ui.Root, Bridge, text, settings, Card, () => Nav.Close());
            Own(info);
            Guide = Own(guide);
        }

        T Own<T>(T d) where T : IDisposable { owned.Add(d); return d; }

        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Ui.Dispose();
        }
    }

    /// <summary>The six steps, in the order a player meets them: the move step (#1141, owner, 21 October 2026) after
    /// training and before the event card, which keeps its own step's number.</summary>
    [Test]
    public void HasSixStepsInOrder()
    {
        CollectionAssert.AreEqual(new[] { "attention", "city", "training", "move", "event", "objectives" }, FirstTurnGuidePresenter.Steps);
        Assert.AreEqual(Array.IndexOf(FirstTurnGuidePresenter.Steps, "event"), FirstTurnGuidePresenter.EventStep);
        foreach (var text in new[] { En, He })
            foreach (var key in FirstTurnGuidePresenter.Steps)
                foreach (var part in new[] { ".title", ".text" })
                    Assert.IsFalse(string.IsNullOrWhiteSpace(text["guide." + key + part]), $"{text["ui.lang"]}: guide.{key}{part}");
    }

    /// <summary>The unit the move step shows: the first drawn unit (units.yaml order) that the sim lets move now (a hex in
    /// Movement.Reachable), so its card has the Move button; none in the legacy loop, which has no starting forces.</summary>
    [Test]
    public void TheMoveStepsUnitCanMove()
    {
        var s = GameSession.New(C, GoldenRun.Seed, Mode.Historical).State;
        var rows = UnitRoster.FromState(UnitRows, C, s, _ => true, (row, a, q, r) => a).Select(t => t.Unit).ToList();
        var id = FirstTurnGuidePresenter.UnitToShow(C, s, rows);
        Assert.IsNotNull(id, "a new game has a unit that can move");
        var row = rows.Single(u => u.Id == id);
        Assert.AreEqual(UnitData.Israel, row.Side);
        Assert.IsNotEmpty(Movement.Reachable(C, s, UnitCard.SimUnit(C, s, row)!.Value), "the sim has a hex for it");
        var first = rows.First(u => UnitCard.SimUnit(C, s, u) is int i && Movement.Reachable(C, s, i).Count > 0);
        Assert.AreEqual(first.Id, id, "the first such row");
        Assert.IsNull(FirstTurnGuidePresenter.UnitToShow(SimBridgeGddTests.Legacy, s, rows), "the legacy loop: no unit to show");
    }

    /// <summary>Skip at any step closes it; Next walks the six steps and Done closes it; Escape skips; How to play's button
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
            Assert.AreEqual(text.Format("guide.step", step + 1, 6), rig.Ui.Q<Label>("guide-count").text);
            Assert.AreEqual(text["guide." + key + ".title"], rig.Ui.Q<Label>("guide-title").text);
            Assert.AreEqual(text["guide." + key + ".text"], rig.Ui.Q<Label>("guide-text").text);
            Assert.AreEqual(text[step == 5 ? "guide.done" : "guide.next"], rig.Ui.Q<Button>("guide-next").text);
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

    /// <summary>In the game as a player has it, the move step shows the guide's unit (UnitToShow) as a click on it would: its
    /// ring and its card with the Move button. Skip puts back the selection the guide found (none), as on the city's steps.</summary>
    [Test]
    public void TheMoveStepShowsAUnitsCardWithMove()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var guide = game.Guide!; var info = game.Info!; var units = game.Units!; var bridge = game.Bridge!;
        Assert.IsTrue(guide.IsOpen && info.Selected == null && !info.ShowsUnit, "precondition: a new game, nothing selected");
        var move = Array.IndexOf(FirstTurnGuidePresenter.Steps, "move");
        for (int i = 0; i < move; i++) UiTestKit.Press(Ui.Q<Button>("guide-next"));
        Assert.AreEqual(move, guide.Step);
        var id = FirstTurnGuidePresenter.UnitToShow(bridge.Content, bridge.State, units.Tokens.Select(t => t.Token));
        Assert.IsNotNull(id, "precondition: a drawn unit can move");
        Assert.AreEqual(id, units.Selected, "the guide's unit, with its ring");
        Assert.IsTrue(info.ShowsUnit, "its card in the info panel");
        Assert.AreEqual(DisplayStyle.Flex, Ui.Q<Button>("info-move").style.display.value, "with the Move button");
        UiTestKit.Press(Ui.Q<Button>("guide-skip"));
        Assert.IsNull(units.Selected, "Skip: no unit selected again");
        Assert.IsFalse(info.ShowsUnit);
        Assert.IsNull(info.Selected, "and no hex");
    }

    /// <summary>Issue #1392: the move step brings UnitToShow's hex into the map view, clear of the info panel and the guide's
    /// card, with at least one of Movement.Reachable's lit destinations visible too. The real scene, camera projection and HUD
    /// layout are used at the guide's supported desktop, itch embed and phone sizes, in both UI directions.</summary>
    [UnityTest]
    public IEnumerator TheMoveStepShowsTheUnitAndAReachableHexOnTheMap()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (390, 844) })
            {
                // The scene first: opening it in Single mode destroys textures made before it.
                var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
                var rt = new RenderTexture(w, h, 0);
                var camRt = new RenderTexture(w, h, 24);
                var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
                ps.targetTexture = rt;
                var camera = Camera.main;
                try
                {
                    game.Language = hebrew ? UiLanguage.Hebrew : UiLanguage.English;
                    Object.FindFirstObjectByType<UIDocument>().panelSettings = ps;
                    camera.targetTexture = camRt;
                    game.Begin(Inputs, 0, startScreen: true);
                    UiTestKit.Press(Ui.Q<Button>("start-new-game"));
                    var guide = game.Guide!; var bridge = game.Bridge!; var units = game.Units!;
                    var map = Object.FindFirstObjectByType<MapScene>();
                    var move = Array.IndexOf(FirstTurnGuidePresenter.Steps, "move");
                    for (int step = 0; step < move; step++) UiTestKit.Press(Ui.Q<Button>("guide-next"));
                    Assert.AreEqual(move, guide.Step, "precondition: the guide is on its move step");
                    for (int frame = 0; frame < 5; frame++) yield return null;

                    var id = FirstTurnGuidePresenter.UnitToShow(bridge.Content, bridge.State, units.Tokens.Select(t => t.Token));
                    Assert.IsNotNull(id, "precondition: a drawn unit can move");
                    var token = units.Tokens.Single(t => t.Token.Unit.Id == id);
                    var simUnit = UnitCard.SimUnit(bridge.Content, bridge.State, token.Token);
                    Assert.IsNotNull(simUnit, "precondition: the guide's token has a sim unit");
                    var reachable = Movement.Reachable(bridge.Content, bridge.State, simUnit!.Value).Keys.ToList();
                    Assert.IsNotEmpty(reachable, "precondition: the guide's unit has a destination");
                    UiTestKit.Press(Ui.Q<Button>("info-move"));
                    Assert.IsTrue(map.Terrain.transform.Find(MapScene.ReachableName)?.GetComponent<MeshRenderer>().enabled == true,
                        "precondition: Move lit the reachable hexes");

                    var root = Ui;
                    var info = root.Q("info-panel").worldBound;
                    var card = root.Q("guide-panel").worldBound;   // the guide's own card is opaque too
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                    (bool visible, bool clear, Vector3 viewport, Vector2 panel) Project(int q, int r)
                    {
                        var hex = bridge.Content.HexAt(q, r)!;
                        var ground = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                        ground.y = map.Terrain.SampleHeight(ground) + map.Terrain.transform.position.y;
                        var viewport = camera.WorldToViewportPoint(ground);
                        var panel = MapLabelsPresenter.ToPanel(viewport, camera.rect, root.panel.visualTree.layout.size, Vector2.zero);
                        return (viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f,
                            !info.Contains(panel) && !card.Contains(panel), viewport, panel);
                    }

                    var unit = Project(token.Q, token.R);
                    if (!unit.visible) problems.Add($"{at}: unit ({token.Q},{token.R}) is outside the viewport at {unit.viewport}");
                    else if (!unit.clear) problems.Add($"{at}: unit ({token.Q},{token.R}) at {unit.panel} is under the info panel {info} or the guide {card}");
                    if (!reachable.Select(p => (at: p, projection: Project(p.Q, p.R))).Any(p => p.projection.visible && p.projection.clear))
                        problems.Add($"{at}: no reachable hex is visible and clear of the info panel and the guide");
                }
                finally
                {
                    camera.targetTexture = null;
                    Object.FindFirstObjectByType<UIDocument>().panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel);
                    Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); camRt.Release(); Object.DestroyImmediate(camRt);
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
            }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    // --- Each step with what it explains (owner's report on v0.1.4) ---------------------------------------------------

    /// <summary>The first card's first option, pressed as a click would.</summary>
    static void AnswerTheCard() => UiTestKit.Press(Ui.Q("event-options").Query<Button>().First());

    /// <summary>The opening turn's two cards answered, One Army then the defence of Degania (R-EVT-006; owner, 24 October
    /// 2026, #1568: "One Army first").</summary>
    static void AnswerTheOpeningCards() { AnswerTheCard(); AnswerTheCard(); }

    /// <summary>The owner's report on v0.1.4 ("they open for a split second after the user closes the screen they were
    /// supposed to explain"), path A: the player answers the card while the event step explains it. The card closes, so the
    /// guide goes on to the next step at once, with Next focused, instead of explaining a card that is gone.</summary>
    [Test]
    public void AnsweringTheCardOnItsStepGoesOnToTheNext()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var guide = game.Guide!; var card = game.Card!;
        for (int i = 0; i < FirstTurnGuidePresenter.EventStep; i++) UiTestKit.Press(Ui.Q<Button>("guide-next"));
        Assert.AreEqual(FirstTurnGuidePresenter.EventStep, guide.Step, "precondition: the event step");
        Assert.IsTrue(card.Shown != null && !card.Collapsed, "precondition: its card open");
        AnswerTheCard();
        Assert.AreEqual("degania_defence", card.Shown?.Id, "One Army answered, the turn's second card opens");
        Assert.AreEqual(FirstTurnGuidePresenter.EventStep, guide.Step, "and the event step stays, explaining it");
        // The owner's report was a mouse player's: the second card, which took the focus as it opened, answered with the
        // pointer (a keyboard Submit inside the card hands the focus to End Turn instead, EventCardPresenter.Choose).
        DecisionCardTests.PressWithPointer(Ui.Q("event-options").Query<Button>().First());
        Assert.IsNull(card.Shown, "precondition: the opening turn's two cards, now answered");
        Assert.IsTrue(guide.IsOpen);
        Assert.AreEqual(Array.IndexOf(FirstTurnGuidePresenter.Steps, "objectives"), guide.Step, "the guide goes on to Objectives");
        Assert.AreEqual(En.Format("guide.step", 6, 6), Ui.Q<Label>("guide-count").text, "the count the player was shown stays");
        Assert.AreEqual(En["guide.objectives.title"], Ui.Q<Label>("guide-title").text);
        Assert.AreSame(Ui.Q<Button>("guide-next"), Ui.Q("root").focusController.focusedElement, "Next has the focus again");
    }

    /// <summary>Path B: the card was answered before the guide reaches its event step (a returning player opening it from How
    /// to play, or one who answered it under the guide): there is no card to explain, so the step is left out and the
    /// guide counts five steps.</summary>
    [Test]
    public void WithNoCardTheEventStepIsLeftOut()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var guide = game.Guide!; var card = game.Card!;
        guide.Close();
        AnswerTheOpeningCards();
        Assert.IsNull(card.Shown, "precondition: no card left this turn");
        UiTestKit.Press(Ui.Q<Button>("nav-how-to-play")); UiTestKit.Press(Ui.Q<Button>("how-to-play-guide"));
        Assert.IsTrue(guide.IsOpen);
        var seen = new List<string>();
        for (int shown = 1; guide.IsOpen; shown++)
        {
            seen.Add(FirstTurnGuidePresenter.Steps[guide.Step]);
            Assert.AreEqual(En.Format("guide.step", shown, 5), Ui.Q<Label>("guide-count").text, $"step {shown} of five");
            Assert.AreEqual(En[shown == 5 ? "guide.done" : "guide.next"], Ui.Q<Button>("guide-next").text);
            Assert.LessOrEqual(shown, 5, "five steps, then Done");
            UiTestKit.Press(Ui.Q<Button>("guide-next"));
        }
        CollectionAssert.AreEqual(new[] { "attention", "city", "training", "move", "objectives" }, seen);
    }

    /// <summary>The city or unit the guide shows for steps 2 to 4 is put away when the guide moves on to a step that does not
    /// explain it (the event card, Objectives), with or without a card, so no stale panel stays under the later steps.</summary>
    [Test]
    public void LeavingTheMoveStepPutsItsUnitCardAway([Values(false, true)] bool cardAnswered)
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var guide = game.Guide!; var info = game.Info!; var units = game.Units!;
        if (cardAnswered) { guide.Close(); AnswerTheOpeningCards(); guide.Open(); }
        var move = Array.IndexOf(FirstTurnGuidePresenter.Steps, "move");
        for (int i = 0; i < move; i++) UiTestKit.Press(Ui.Q<Button>("guide-next"));
        Assert.IsTrue(info.ShowsUnit && units.Selected != null, "precondition: the move step shows a unit's card");
        UiTestKit.Press(Ui.Q<Button>("guide-next"));
        Assert.AreEqual(cardAnswered ? "objectives" : "event", FirstTurnGuidePresenter.Steps[guide.Step]);
        Assert.IsFalse(info.ShowsUnit, "the unit's card is put away");
        Assert.IsNull(units.Selected, "and its unit unselected, as the guide found it");
        Assert.IsNull(info.Selected, "with no hex shown either");
    }

    /// <summary>A closed or disposed guide takes no notice of the game changing (it listens to SimBridge.Changed only while it
    /// lives, and acts on it only while it is open).</summary>
    [Test]
    public void AClosedGuideIgnoresTheGameChanging()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var guide = game.Guide!;
        for (int i = 0; i < FirstTurnGuidePresenter.EventStep; i++) UiTestKit.Press(Ui.Q<Button>("guide-next"));
        UiTestKit.Press(Ui.Q<Button>("guide-skip"));
        AnswerTheOpeningCards();
        Assert.IsFalse(guide.IsOpen, "closed, it stays closed");
        Assert.AreEqual(-1, guide.Step);
        Assert.IsFalse(Ui.Q("root").ClassListContains(FirstTurnGuidePresenter.OpenClass));

        using var rig = new Rig(En);
        var bridge = rig.Bridge;
        var lone = new FirstTurnGuidePresenter(rig.Ui.Root, En, null, rig.Card, bridge: bridge);
        lone.Dispose();
        rig.Guide.Open();                                               // another guide open on the same HUD (the root's class)
        for (int i = 0; i < FirstTurnGuidePresenter.EventStep; i++) rig.Guide.Next();
        UiTestKit.Press(rig.Ui.Root.Q("event-options").Query<Button>().First());   // One Army
        UiTestKit.Press(rig.Ui.Root.Q("event-options").Query<Button>().First());   // then Degania (R-EVT-006)
        Assert.IsNull(rig.Card.Shown, "precondition: the opening turn's two cards answered");
        Assert.AreEqual(-1, lone.Step, "the disposed guide was not called");
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
                    if (FirstTurnGuidePresenter.Steps[step] == "move" &&
                        (targets.Count != 1 || targets[0].name != "info-move" || ((Button)targets[0]).text != (hebrew ? He : En)["unit.order.move"]))
                        problems.Add($"{at}: the move step must point at the unit card's Move button");
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
                    // The ring too (its gap and border round the target), so the larger text (player feedback, 21 October
                    // 2026) never pushes the panel onto the gold box it points at.
                    if (ring.resolvedStyle.display == DisplayStyle.Flex && panel.Overlaps(ring.worldBound))
                        problems.Add($"{at}: the panel {panel} covers the ring {ring.worldBound}");
                    foreach (var name in new[] { "guide-title", "guide-text", "guide-skip", "guide-next" })
                        if (!Inside(root.Q(name).worldBound, panel)) problems.Add($"{at}: {name} {root.Q(name).worldBound} leaves the panel {panel}");
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>Player feedback relayed by the owner (21 October 2026: the guide's text was too small; "make it slightly
    /// bigger"): at 1280x720 (the itch embed) and 1920x1080, in English and Hebrew, the guide's text is at least
    /// <see cref="GuideTextOnScreenPx"/> on screen and its smallest line, the step count, at least the owner's text floor
    /// (HudPresenter.TextFloorPx). Before the change they were 12.7 and 10.7 at 1280x720. On screen px are the resolved size
    /// times the panel's scale (EditMode draws at one pixel per CSS px). Every size and step is logged, the phone (390x844)
    /// too, which is held to the floor alone (about 0.49 scale; <see cref="TheGuideKeepsTheTextFloorOnEveryWindow"/>).</summary>
    [UnityTest]
    public IEnumerator TheGuideReadsAtTheItchEmbedSize()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (390, 844) })
                yield return EachStep(w, h, hebrew, (root, guide, step) =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h} step {step + 1}";
                    float scale = root.panel.scaledPixelsPerPoint;
                    float OnScreen(string name) => root.Q(name).resolvedStyle.fontSize * scale;
                    var panel = root.Q("guide-panel").worldBound;
                    Debug.Log($"GUIDE SIZE {at}: scale {scale:0.###}, text {OnScreen("guide-text"):0.#} px, title {OnScreen("guide-title"):0.#} px, " +
                              $"count {OnScreen("guide-count"):0.#} px, buttons {OnScreen("guide-next"):0.#} px on screen; panel {panel.width * scale:0}x{panel.height * scale:0} px");
                    if (w == 390) return;
                    if (OnScreen("guide-text") < GuideTextOnScreenPx - 0.01f)
                        problems.Add($"{at}: the guide's text is {OnScreen("guide-text"):0.##} px on screen (under {GuideTextOnScreenPx})");
                    if (OnScreen("guide-count") < HudPresenter.TextFloorPx - 0.01f)
                        problems.Add($"{at}: the step count is {OnScreen("guide-count"):0.##} px on screen (under the {HudPresenter.TextFloorPx} px floor)");
                    if (OnScreen("guide-title") <= OnScreen("guide-text"))
                        problems.Add($"{at}: the title ({OnScreen("guide-title"):0.#} px) is not larger than the text ({OnScreen("guide-text"):0.#} px)");
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>The owner's text floor on the guide too (21 October 2026, after #1283): at every window, the phone held upright
    /// (390x844, about 0.49 scale) included, in English and Hebrew, the step count, the title, the text and both buttons
    /// are at least HudPresenter.TextFloorPx on screen, the top bar's rule (HudPresenter.SmallTextSize); where the scale
    /// keeps them over it (1920x1080, 1280x720) they are the stylesheet's sizes from #1283, unchanged. Before the change
    /// the phone's text was about 10.8 px on screen.</summary>
    [UnityTest]
    public IEnumerator TheGuideKeepsTheTextFloorOnEveryWindow()
    {
        var problems = new List<string>();
        // Statecraft.uss: .guide-count, .guide-title, .guide-text, Button (Skip and Next).
        var stylesheet = new Dictionary<string, float> { ["guide-count"] = 18f, ["guide-title"] = 29f, ["guide-text"] = 22f, ["guide-skip"] = 19f, ["guide-next"] = 19f };
        CollectionAssert.AreEqual(new[] { 18f, 29f, 22f, 19f },
            new[] { FirstTurnGuidePresenter.CountPx, FirstTurnGuidePresenter.TitlePx, FirstTurnGuidePresenter.TextPx, FirstTurnGuidePresenter.ButtonPx },
            "the presenter's sizes mirror the stylesheet's");
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (1024, 768), (390, 844) })
                yield return EachStep(w, h, hebrew, (root, guide, step) =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h} step {step + 1}";
                    float scale = root.panel.scaledPixelsPerPoint;
                    foreach (var (name, px) in stylesheet)
                    {
                        float size = root.Q(name).resolvedStyle.fontSize;
                        if (size * scale < HudPresenter.TextFloorPx - 0.01f)
                            problems.Add($"{at}: {name} is {size * scale:0.##} px on screen ({size} px at {scale:0.###}; under the {HudPresenter.TextFloorPx} px floor)");
                        if (w >= 1280 && Mathf.Abs(size - px) > 0.01f)
                            problems.Add($"{at}: {name} is {size} px, not the stylesheet's {px} px (the desktop sizes stay)");
                    }
                    if (root.Q("guide-title").resolvedStyle.fontSize <= root.Q("guide-text").resolvedStyle.fontSize)
                        problems.Add($"{at}: the title is not larger than the text");
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>The guide's text on screen at 1280x720 and larger, in px (22 panel px at 0.67 scale is 14.7).</summary>
    const float GuideTextOnScreenPx = 14f;

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
        var (info, guide) = PanelAndGuide(root, bridge, text, null, card, () => nav.Close());
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
