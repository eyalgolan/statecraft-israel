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

/// <summary>The first-turn advisor (#42; Q-ONB-1, owner, 17 October 2026, #784): it shows by itself on the first new game only,
/// never on a Continue or a second game; it can be skipped and opened again from How to play; every step's target is on
/// screen at 1280x720 and 390x844 in English and Hebrew, with the panel clear of it; and it changes neither the sim nor the
/// save.</summary>
public class AdvisorOpeningParityTests
{
    // Approval is a test-only in-memory copy; no shipped review field is changed.
    static AdvisorCatalog Approved() => AdvisorCatalog.Parse(File.ReadAllText(AdvisorCatalog.Path).Replace("he_review: pending", "he_review: approved"));
    static string[] Steps => AdvisorPresenter.ChainIds(AdvisorCatalog.Load()).ToArray();
    static void Begin(GameRoot game, GameInputs inputs, int smoke, bool startScreen = false)
    {
        game.Begin(inputs, smoke, startScreen: startScreen);
        // Task9 temporarily retains the legacy presenter. Test the replacement alone; Task11 removes this disposal.
        game.Guide?.Dispose();
    }
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    /// <summary>The settlements' names for the info panel's title (InfoPanelLayoutTests' list).</summary>
    static Dictionary<string, string> Names() => SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName)))
        .ToDictionary(t => t.Id, t => t.Name);
    static List<UnitEntry> UnitRows => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));

    /// <summary>The info panel and the advisor as GameRoot wires them, without the map: the advisor's city (CityHex) and its unit
    /// (UnitToShow, among the tokens a new game draws) shown as a click would show them.</summary>
    static (InfoPanelPresenter info, AdvisorPresenter advisor) PanelAndAdvisor(VisualElement root, SimBridge bridge, UiText text,
        SettingsStore? settings, EventCardPresenter card, Action closeScreens)
    {
        var tokens = UnitRoster.FromState(UnitRows, bridge.Content, bridge.State, _ => true, (row, a, q, r) => a);
        PlacedToken? shown = null;
        var info = new InfoPanelPresenter(root, bridge, text, Names(), selectedUnit: () => shown);
        var session = new AdvisorSession(Approved(), new AdvisorSettings(settings), autoShow: true);
        var advisor = new AdvisorPresenter(root, bridge, text, session, hooks: new AdvisorHooks
        {
            Card = card, CloseScreens = closeScreens,
            ShowCity = () => { if (AdvisorTargets.CityHex(bridge.Content, bridge.State) is (int q, int r)) info.Show(q, r); },
            ShowUnit = () =>
            {
                if (AdvisorTargets.UnitToShow(bridge.Content, bridge.State, tokens) is not string id) return;
                shown = new PlacedToken(tokens.First(t => t.Unit.Id == id), new AnchorSpot(0, 0, 0, 0, false)); info.ShowUnit();
            },
        });
        return (info, advisor);
    }

    readonly List<string> temp = new List<string>();
    string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-advisor-" + Guid.NewGuid().ToString("N"));
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
        root.Store = store; root.Settings = settings; root.Language = UiLanguage.English;
        root.AdvisorCatalogForTests = Approved();
        return root;
    }

    /// <summary>Map.unity as a player boots it: the advisor waits for the start screen, opens when New Game closes it (at its
    /// first step, the card hidden behind "Decision waiting") and notes in the settings file that it has shown; the next new
    /// game, from the start screen or the top bar, does not show it again.</summary>
    [Test]
    public void ShowsOnTheFirstNewGameOnly()
    {
        var settings = new SettingsStore(TempDir());
        var game = OpenScene(new SaveStore(TempDir()), settings);
        Begin(game, Inputs, 0, startScreen: true);                       // a fresh profile: no save
        Assert.IsFalse(game.Advisor!.InChain, "not under the start screen");
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(game.Advisor!.InChain, "New Game closes the screen and the advisor opens");
        Assert.AreEqual(0, game.Advisor.ChainStep);
        Assert.IsTrue(game.Card!.Shown != null && game.Card.Collapsed, "the opening card waits behind Decision waiting");
        Assert.AreEqual("seen", settings.Read(AdvisorSettings.OpeningKey));
        UiTestKit.Press(Ui.Q<Button>("advisor-close"));
        Assert.IsFalse(game.Advisor.InChain);
        Assert.IsFalse(game.Card.Collapsed, "skipping puts the card back");

        Begin(game, Inputs, 0, startScreen: true);                       // a reload with no save: a new game again
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsFalse(game.Advisor!.InChain, "a second new game: not again");
        UiTestKit.Press(Ui.Q<Button>("new-game")); UiTestKit.Press(Ui.Q<Button>("new-game"));
        Assert.IsFalse(game.Advisor!.InChain, "nor from the top bar's New Game");
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
        Begin(game, Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-continue"));
        Assert.IsFalse(game.Advisor!.InChain, "Continue");
        Assert.IsNull(settings.Read(AdvisorSettings.OpeningKey), "and still unseen");

        Begin(game, Inputs, 0);                                          // the tests' and tools' Begin: no start screen
        UiTestKit.Press(Ui.Q<Button>("new-game")); UiTestKit.Press(Ui.Q<Button>("new-game"));
        Assert.IsFalse(game.Advisor!.InChain, "an automated run");
        game.Settings = null;                                           // EditMode with no settings file
        Begin(game, Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsFalse(game.Advisor!.InChain, "nowhere to remember it: never by itself");

        game.Settings = settings;
        Begin(game, Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(game.Advisor!.InChain, "the first new game in this profile");
    }

    /// <summary>Actual Task7 callback path: a saved start screen closes from NewGamePresenter.Started, and both notify advisors.</summary>
    [Test]
    public void ANewGameFromASavedStartScreenKeepsItsOpeningChain()
    {
        var store = new SaveStore(TempDir()); var settings = new SettingsStore(TempDir());
        var played = new SimBridge(GameSession.New(C, 9, Mode.Historical), En); played.EndTurn(); played.EndTurn();
        store.WriteAutosave(played.Session.Save.Write());
        var game = OpenScene(store, settings); Begin(game, Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game")); UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(game.Advisor!.InChain); Assert.AreEqual(0, game.Advisor.ChainStep);
        Assert.AreEqual("seen", settings.Read(AdvisorSettings.OpeningKey));
    }

    /// <summary>Review focus3: actual Settings rebuilds preserve the step, hide pending Hebrew and resume English.</summary>
    [Test]
    public void ALanguageSwitchMidChainKeepsTheStep()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        game.AdvisorCatalogForTests = AdvisorCatalog.Parse(File.ReadAllText(AdvisorCatalog.Path).Replace("he_review: approved", "he_review: pending"));
        Begin(game, Inputs, 0, startScreen: true); UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        game.Advisor!.NextStep(); game.Advisor.NextStep(); var session = game.AdvisorSession;
        Assert.AreEqual(2, game.Advisor.ChainStep);
        UiTestKit.Press(Ui.Q<Button>("nav-settings")); UiTestKit.Press(Ui.Q<Button>("settings-language-he"));
        game.Guide?.Dispose(); // the temporary legacy presenter is not part of this replacement test
        Assert.AreSame(session, game.AdvisorSession); Assert.IsFalse(game.Advisor!.IsOpen); Assert.AreEqual(2, session!.Chain.Step);
        if (!Ui.Q("root").ClassListContains(ScreenNav.ScreenOpenClass)) UiTestKit.Press(Ui.Q<Button>("nav-settings"));
        UiTestKit.Press(Ui.Q<Button>("settings-language-en")); game.Guide?.Dispose();
        if (Ui.Q("root").ClassListContains(ScreenNav.ScreenOpenClass)) UiTestKit.Press(Ui.Q<Button>("nav-settings"));
        Assert.AreSame(session, game.AdvisorSession); Assert.IsTrue(game.Advisor!.InChain); Assert.AreEqual(2, game.Advisor.ChainStep);
    }

    // --- Skip, Escape and How to play -------------------------------------------------------------------------------

    sealed class Rig : IDisposable
    {
        public readonly UiTestKit Ui = new UiTestKit();
        public readonly SimBridge Bridge;
        public readonly ScreenNav Nav;
        public readonly EventCardPresenter Card;
        public readonly AdvisorPresenter Advisor;
        public readonly InfoPanelPresenter Info;
        readonly List<IDisposable> owned = new List<IDisposable>();

        public Rig(UiText text, SettingsStore? settings = null)
        {
            Ui.Root.Q("root").RemoveFromClassList("loading");
            Bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
            owned.Add(new HudPresenter(Ui.Root, Bridge, text));
            Nav = Own(new ScreenNav(Ui.Root));
            Card = Own(new EventCardPresenter(Ui.Root, Bridge, text, null));
            Own(new HowToPlayPresenter(Ui.Root, text));
            var (info, advisor) = PanelAndAdvisor(Ui.Root, Bridge, text, settings, Card, () => Nav.Close());
            Info = Own(info);
            Advisor = Own(advisor);
        }

        T Own<T>(T d) where T : IDisposable { owned.Add(d); return d; }

        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Ui.Dispose();
        }
    }

    /// <summary>The seven steps, in the order a player meets them (spec 9.2): the greeting first, then the guide's six with
    /// the guide's targets; the move step (#1141, owner, 21 October 2026) after training and before the event card.</summary>
    [Test]
    public void HasSevenStepsInOrder()
    {
        CollectionAssert.AreEqual(new[] { "opening.greeting", "opening.attention", "opening.city", "opening.training", "opening.move", "opening.event", "opening.objectives" }, Steps);
        Assert.AreEqual(5, AdvisorPresenter.EventStep);
        var targets = AdvisorCatalog.Load().Messages.Where(m => m.Signal == AdvisorSignals.Opening)
            .Select(m => m.Actions.FirstOrDefault(a => a.ShowMe != "")?.ShowMe ?? "").ToArray();
        CollectionAssert.AreEqual(new[] { "", "attention", "city_housing", "city_training", "unit_move", "decision_waiting", "nav_objectives" }, targets,
            "the guide's targets, in its order");
        foreach (var text in new[] { En, He }) foreach (var key in Steps)
            Assert.IsFalse(string.IsNullOrWhiteSpace(text["advisor." + key + ".text"]), key);
    }

    /// <summary>The unit the move step shows: the first drawn unit (units.yaml order) that the sim lets move now (a hex in
    /// Movement.Reachable), so its card has the Move button; none in the legacy loop, which has no starting forces.</summary>
    [Test]
    public void TheMoveStepsUnitCanMove()
    {
        var s = GameSession.New(C, GoldenRun.Seed, Mode.Historical).State;
        var rows = UnitRoster.FromState(UnitRows, C, s, _ => true, (row, a, q, r) => a).Select(t => t.Unit).ToList();
        var id = AdvisorTargets.UnitToShow(C, s, rows);
        Assert.IsNotNull(id, "a new game has a unit that can move");
        var row = rows.Single(u => u.Id == id);
        Assert.AreEqual(UnitData.Israel, row.Side);
        Assert.IsNotEmpty(Movement.Reachable(C, s, UnitCard.SimUnit(C, s, row)!.Value), "the sim has a hex for it");
        var first = rows.First(u => UnitCard.SimUnit(C, s, u) is int i && Movement.Reachable(C, s, i).Count > 0);
        Assert.AreEqual(first.Id, id, "the first such row");
        Assert.IsNull(AdvisorTargets.UnitToShow(SimBridgeGddTests.Legacy, s, rows), "the legacy loop: no unit to show");
    }

    /// <summary>Skip at any step closes it; Next walks the seven steps and Done closes it; Escape skips; How to play's button
    /// opens it again at its first step, closing that screen; each step shows its own strings.</summary>
    [UnityTest] public IEnumerator CanBeSkippedInEnglish() { yield return CanBeSkippedAtAnyStepAndReopened(false); }
    [UnityTest] public IEnumerator CanBeSkippedInHebrew() { yield return CanBeSkippedAtAnyStepAndReopened(true); }
    IEnumerator CanBeSkippedAtAnyStepAndReopened(bool hebrew)
    {
        var text = hebrew ? He : En;
        using var rig = new Rig(text, new SettingsStore(TempDir()));
        using var objectives = new ObjectivesPresenter(rig.Ui.Root, rig.Bridge, text);
        rig.Advisor.NewGame();
        Assert.IsTrue(rig.Advisor.InChain);
        for (int step = 0; step < Steps.Length; step++)
        {
            var key = Steps[step];
            Assert.AreEqual(step, rig.Advisor.ChainStep);
            Assert.AreEqual(text.Format("advisor.step", step + 1, 7), rig.Ui.Q<Label>("advisor-count").text);
            var speaker = AdvisorCatalog.Load().Messages.Single(m => m.Id == key).Advisor;
            Assert.AreEqual(text["advisor." + speaker + (step == 2 ? ".name_first" : ".name")], rig.Ui.Q<Label>("advisor-name").text);
            Assert.AreEqual(text["advisor." + key + ".text"], rig.Ui.Q<Label>("advisor-text").text);
            Assert.AreEqual(text[step == 6 ? "advisor.done" : "advisor.next"], rig.Ui.Q<Button>("advisor-got-it").text);
            Assert.IsTrue(rig.Card.Collapsed, "the chain speaks before the card opens");
            if (key == "opening.objectives")
            {
                // #791: from the advisor's last step the real Objectives button opens the new parts and the standalone goal.
                CollectionAssert.AreEquivalent(new[] { "railway", "burma_road", "yishuvim", "absorb_immigrants" },
                    C.Objectives.Select(o => o.Id).ToArray(), "the current objectives, not the retired standalone first yishuv");
                rig.Ui.Press("nav-objectives");
                Assert.AreEqual("objectives", rig.Nav.Current, "the advisor's Objectives destination opens");
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
            rig.Ui.Press("advisor-got-it");
        }
        Assert.IsFalse(rig.Advisor.InChain, "Done closes it");
        Assert.IsFalse(rig.Card.Collapsed, "and the card is as it was");

        for (int at = 0; at < Steps.Length; at++)
        {
            rig.Ui.Press("nav-how-to-play");
            Assert.AreEqual("how-to-play", rig.Nav.Current);
            Assert.AreEqual(text["how_to_play.guide"], rig.Ui.Q<Button>("how-to-play-guide").text);
            rig.Ui.Press("how-to-play-guide");
            Assert.IsTrue(rig.Advisor.InChain, "How to play opens it again");
            Assert.IsNull(rig.Nav.Current, "and closes itself");
            Assert.AreEqual(0, rig.Advisor.ChainStep);
            for (int i = 0; i < at; i++) rig.Ui.Press("advisor-got-it");
            rig.Ui.Press("advisor-close");
            Assert.IsFalse(rig.Advisor.InChain, $"Skip at step {at + 1}");
            Assert.IsFalse(rig.Card.Collapsed, $"the card is back after a Skip at step {at + 1}");
        }

        rig.Advisor.OpenChain();
        yield return null; rig.Ui.Q<Button>("advisor-got-it").Focus();
        Assert.IsTrue(rig.Advisor.Escape(), "Escape skips it");
        Assert.IsFalse(rig.Advisor.InChain);
        // The guide's "Escape does nothing when closed" cannot be asked again here: Time.frameCount does not move in an
        // edit-mode test, so Escape keeps answering "used this frame" (AdvisorPopupTests.OneEscapeInThePopupLeavesTheScreenUnderItOpen).
        Assert.IsFalse(rig.Ui.Root.Q("root").ClassListContains(AdvisorPresenter.OpenClass), "and it stays closed");

        rig.Card.Collapse();                                           // the player hid the card before opening the advisor
        rig.Advisor.OpenChain();
        for (int i = 0; i < AdvisorPresenter.EventStep; i++) rig.Ui.Press("advisor-got-it");
        rig.Ui.Press("advisor-show-me");
        Assert.IsFalse(rig.Card.Collapsed, "Show me opens the card");
        rig.Card.Collapse();
        rig.Ui.Press("advisor-close");
        Assert.IsTrue(rig.Card.Collapsed, "and Skip hides it again, as the player left it");
    }

    /// <summary>End turn pressed with the advisor open (it takes no pointer outside its panel, so the HUD stays usable): the
    /// advisor closes as the turn advances, at any step, and the new turn's card is left to its own rule (a new event opens),
    /// not hidden or shown by what the advisor found on the old card (review of #1209, follow-up 1).</summary>
    [Test]
    public void EndTurnClosesTheGuide([Values(0, 1, 2, AdvisorPresenter.EventStep)] int step, [Values(false, true)] bool cardHidden)
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        Begin(game, Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(game.Advisor!.InChain, "precondition: the first new game opens the advisor");
        game.Advisor.CloseChain();
        var card = game.Card!;
        Assert.IsNotNull(card.Shown, "precondition: a new game opens on a card");
        var first = card.Shown!.Id;
        if (cardHidden) card.Collapse();
        game.Advisor.OpenChain();
        for (int i = 0; i < step; i++) UiTestKit.Press(Ui.Q<Button>("advisor-got-it"));
        var turn = game.Bridge!.State.Turn;
        UiTestKit.Press(Ui.Q<Button>("end-turn"));
        Assert.AreEqual(turn + 1, game.Bridge.State.Turn, "precondition: End turn ran the turn");
        Assert.IsFalse(game.Advisor.InChain, "the advisor closes when the turn advances");
        Assert.AreEqual(-1, game.Advisor.ChainStep);
        Assert.IsFalse(Ui.Q("root").ClassListContains(AdvisorPresenter.OpenClass));
        if (card.Shown != null && card.Shown.Id != first)
            Assert.IsFalse(card.Collapsed, $"the new turn's card ({card.Shown.Id}) opens by its own rule");
        else if (card.Shown != null)
            Assert.AreEqual(cardHidden, card.Collapsed, "the same card is put back as the advisor found it");
        Assert.AreEqual(card.Shown != null && !card.Collapsed, Ui.Q("root").ClassListContains(EventCardPresenter.OpenClass),
            "the backdrop shows with an open card only");
    }

    /// <summary>The advisor shows its city on steps 2 and 3; Done, Skip or Escape puts back the selection it found (none, a
    /// hex or a unit) with the info panel and the map's outline; a selection the player made meanwhile is kept (review of
    /// #1209, follow-up 3).</summary>
    [UnityTest]
    public IEnumerator ClosingPutsBackTheSelectionItFound()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        Begin(game, Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var advisor = game.Advisor!; var info = game.Info!; var units = game.Units!; var bridge = game.Bridge!;
        var map = Object.FindFirstObjectByType<MapScene>();
        var city = AdvisorTargets.CityHex(bridge.Content, bridge.State)!.Value;
        bool Outlined() { var t = map.Terrain.transform.Find(MapScene.SelectionName); return t != null && t.GetComponent<MeshRenderer>().enabled; }
        void ToCity()
        {
            for (int i = 0; i < 2; i++) UiTestKit.Press(Ui.Q<Button>("advisor-got-it"));   // past the greeting and Attention
            Assert.AreEqual(city, info.Selected, "step 3 shows the advisor's city");
        }
        void Click(int q, int r) => GameRoot.OnHexClicked(map, units, info, q, r);

        Assert.IsTrue(advisor.InChain && info.Selected == null && !info.ShowsUnit, "precondition: a new game, nothing selected");
        ToCity();
        UiTestKit.Press(Ui.Q<Button>("advisor-close"));
        Assert.IsNull(info.Selected, "Skip: nothing selected again");
        Assert.IsFalse(info.ShowsUnit);
        Assert.IsNull(units.Selected);
        Assert.IsFalse(Outlined(), "and no outline on the map");

        var hex = bridge.Content.Hexes.Where(h => h.Land).Select(h => (h.Q, h.R)).First(h => h != city && units.UnitToSelectAt(h.Item1, h.Item2) == null);
        Click(hex.Item1, hex.Item2);
        Assert.AreEqual(hex, info.Selected, "precondition: a hex selected");
        advisor.OpenChain(); ToCity();
        for (int i = 2; i < Steps.Length; i++) UiTestKit.Press(Ui.Q<Button>("advisor-got-it"));
        Assert.IsFalse(advisor.InChain, "Done");
        Assert.AreEqual(hex, info.Selected, "Done: the hex the advisor found");
        Assert.IsTrue(Outlined(), "outlined again");

        Assert.IsNotEmpty(units.Tokens, "precondition: units in play");
        var token = units.Tokens[0];
        Click(token.Q, token.R);
        var unit = units.Selected;
        Assert.IsTrue(unit != null && info.ShowsUnit, "precondition: a unit selected");
        advisor.OpenChain(); ToCity();
        yield return null; Ui.Q<Button>("advisor-got-it").Focus(); Assert.IsTrue(advisor.Escape());
        Assert.AreEqual(unit, units.Selected, "Escape: the unit the advisor found");
        Assert.IsTrue(info.ShowsUnit, "its card in the info panel");
        Assert.IsFalse(Outlined(), "with its ring, not a hex outline");

        Click(hex.Item1, hex.Item2);
        advisor.OpenChain(); ToCity();
        var other = bridge.Content.Hexes.Where(h => h.Land).Select(h => (h.Q, h.R)).First(h => h != city && h != hex && units.UnitToSelectAt(h.Item1, h.Item2) == null);
        Click(other.Item1, other.Item2);                                // the player picks a hex under the open advisor
        UiTestKit.Press(Ui.Q<Button>("advisor-close"));
        Assert.AreEqual(other, info.Selected, "a selection the player made meanwhile stays");
    }

    /// <summary>In the game as a player has it, the move step shows the advisor's unit (UnitToShow) as a click on it would: its
    /// ring and its card with the Move button. Skip puts back the selection the advisor found (none), as on the city's steps.</summary>
    [Test]
    public void TheMoveStepShowsAUnitsCardWithMove()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        Begin(game, Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var advisor = game.Advisor!; var info = game.Info!; var units = game.Units!; var bridge = game.Bridge!;
        Assert.IsTrue(advisor.InChain && info.Selected == null && !info.ShowsUnit, "precondition: a new game, nothing selected");
        var move = Array.IndexOf(Steps, "opening.move");
        for (int i = 0; i < move; i++) UiTestKit.Press(Ui.Q<Button>("advisor-got-it"));
        Assert.AreEqual(move, advisor.ChainStep);
        var id = AdvisorTargets.UnitToShow(bridge.Content, bridge.State, units.Tokens.Select(t => t.Token));
        Assert.IsNotNull(id, "precondition: a drawn unit can move");
        Assert.AreEqual(id, units.Selected, "the advisor's unit, with its ring");
        Assert.IsTrue(info.ShowsUnit, "its card in the info panel");
        Assert.AreEqual(DisplayStyle.Flex, Ui.Q<Button>("info-move").style.display.value, "with the Move button");
        UiTestKit.Press(Ui.Q<Button>("advisor-close"));
        Assert.IsNull(units.Selected, "Skip: no unit selected again");
        Assert.IsFalse(info.ShowsUnit);
        Assert.IsNull(info.Selected, "and no hex");
    }

    /// <summary>Issue #1392: the move step brings UnitToShow's hex into the map view, clear of the info panel and the advisor's
    /// card, with at least one of Movement.Reachable's lit destinations visible too. The real scene, camera projection and HUD
    /// layout are used at the advisor's supported desktop, itch embed and phone sizes, in both UI directions.</summary>
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
                    Begin(game, Inputs, 0, startScreen: true);
                    UiTestKit.Press(Ui.Q<Button>("start-new-game"));
                    var advisor = game.Advisor!; var bridge = game.Bridge!; var units = game.Units!;
                    var map = Object.FindFirstObjectByType<MapScene>();
                    var move = Array.IndexOf(Steps, "opening.move");
                    for (int step = 0; step < move; step++) UiTestKit.Press(Ui.Q<Button>("advisor-got-it"));
                    Assert.AreEqual(move, advisor.ChainStep, "precondition: the advisor is on its move step");
                    for (int frame = 0; frame < 5; frame++) yield return null;

                    var id = AdvisorTargets.UnitToShow(bridge.Content, bridge.State, units.Tokens.Select(t => t.Token));
                    Assert.IsNotNull(id, "precondition: a drawn unit can move");
                    var token = units.Tokens.Single(t => t.Token.Unit.Id == id);
                    var simUnit = UnitCard.SimUnit(bridge.Content, bridge.State, token.Token);
                    Assert.IsNotNull(simUnit, "precondition: the advisor's token has a sim unit");
                    var reachable = Movement.Reachable(bridge.Content, bridge.State, simUnit!.Value).Keys.ToList();
                    Assert.IsNotEmpty(reachable, "precondition: the advisor's unit has a destination");
                    UiTestKit.Press(Ui.Q<Button>("info-move"));
                    Assert.IsTrue(map.Terrain.transform.Find(MapScene.ReachableName)?.GetComponent<MeshRenderer>().enabled == true,
                        "precondition: Move lit the reachable hexes");

                    var root = Ui;
                    var info = root.Q("info-panel").worldBound;
                    var card = root.Q("advisor-popup").worldBound;   // the advisor's own card is opaque too
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
                    else if (!unit.clear) problems.Add($"{at}: unit ({token.Q},{token.R}) at {unit.panel} is under the info panel {info} or the advisor {card}");
                    if (!reachable.Select(p => (at: p, projection: Project(p.Q, p.R))).Any(p => p.projection.visible && p.projection.clear))
                        problems.Add($"{at}: no reachable hex is visible and clear of the info panel and the advisor");
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

    /// <summary>The owner's report on v0.1.4 ("they open for a split second after the user closes the screen they were
    /// supposed to explain"), path A: the player answers the card while the event step explains it. The card closes, so the
    /// advisor goes on to the next step at once, with Next focused, instead of explaining a card that is gone.</summary>
    [Test]
    public void AnsweringTheCardOnItsStepGoesOnToTheNext()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        Begin(game, Inputs, 0, startScreen: true); UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var advisor = game.Advisor!; var card = game.Card!;
        for (int i = 0; i < AdvisorPresenter.EventStep; i++) advisor.NextStep();
        Assert.IsTrue(card.Shown != null && card.Collapsed);
        UiTestKit.Press(Ui.Q<Button>("advisor-show-me")); Assert.IsFalse(advisor.IsOpen);
        card.Collapse(); Assert.AreEqual(5, advisor.ChainStep); Assert.IsTrue(advisor.IsOpen);
        UiTestKit.Press(Ui.Q<Button>("advisor-show-me")); AnswerTheCard();
        Assert.IsNull(card.Shown); Assert.IsTrue(advisor.InChain); Assert.AreEqual(6, advisor.ChainStep);
        Assert.AreEqual(En.Format("advisor.step", 7, 7), Ui.Q<Label>("advisor-count").text, "the count the player was shown stays");
        Assert.AreEqual(En["advisor.opening.objectives.text"], Ui.Q<Label>("advisor-text").text, "on to Objectives");
        Assert.AreSame(Ui.Q<Button>("advisor-got-it"), Ui.Q("root").focusController.focusedElement, "Next has the focus again (#1323)");
    }

    /// <summary>Path B: the card was answered before the advisor reaches its event step (a returning player opening it from How
    /// to play, or one who answered it under the advisor): there is no card to explain, so the step is left out and the
    /// advisor counts five steps.</summary>
    [Test]
    public void WithNoCardTheEventStepIsLeftOut()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        Begin(game, Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var advisor = game.Advisor!; var card = game.Card!;
        advisor.CloseChain();
        AnswerTheCard();
        Assert.IsNull(card.Shown, "precondition: no card left this turn");
        UiTestKit.Press(Ui.Q<Button>("nav-how-to-play")); UiTestKit.Press(Ui.Q<Button>("how-to-play-guide"));
        Assert.IsTrue(advisor.InChain);
        var seen = new List<string>();
        for (int shown = 1; advisor.InChain; shown++)
        {
            seen.Add(Steps[advisor.ChainStep]);
            Assert.AreEqual(En.Format("advisor.step", shown, 6), Ui.Q<Label>("advisor-count").text, $"step {shown} of six");
            Assert.AreEqual(En[shown == 6 ? "advisor.done" : "advisor.next"], Ui.Q<Button>("advisor-got-it").text);
            Assert.LessOrEqual(shown, 6, "six steps, then Done");
            UiTestKit.Press(Ui.Q<Button>("advisor-got-it"));
        }
        CollectionAssert.AreEqual(new[] { "opening.greeting", "opening.attention", "opening.city", "opening.training", "opening.move", "opening.objectives" }, seen);
    }

    /// <summary>The city or unit the advisor shows for steps 2 to 4 is put away when the advisor moves on to a step that does not
    /// explain it (the event card, Objectives), with or without a card, so no stale panel stays under the later steps.</summary>
    [Test]
    public void LeavingTheMoveStepPutsItsUnitCardAway([Values(false, true)] bool cardAnswered)
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        Begin(game, Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var advisor = game.Advisor!; var info = game.Info!; var units = game.Units!;
        if (cardAnswered) { advisor.CloseChain(); AnswerTheCard(); advisor.OpenChain(); }
        var move = Array.IndexOf(Steps, "opening.move");
        for (int i = 0; i < move; i++) UiTestKit.Press(Ui.Q<Button>("advisor-got-it"));
        Assert.IsTrue(info.ShowsUnit && units.Selected != null, "precondition: the move step shows a unit's card");
        UiTestKit.Press(Ui.Q<Button>("advisor-got-it"));
        Assert.AreEqual(cardAnswered ? "opening.objectives" : "opening.event", Steps[advisor.ChainStep]);
        Assert.IsFalse(info.ShowsUnit, "the unit's card is put away");
        Assert.IsNull(units.Selected, "and its unit unselected, as the advisor found it");
        Assert.IsNull(info.Selected, "with no hex shown either");
    }

    /// <summary>A closed or disposed advisor takes no notice of the game changing (it listens to SimBridge.Changed only while it
    /// lives, and acts on it only while it is open).</summary>
    [Test]
    public void AClosedGuideIgnoresTheGameChanging()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        Begin(game, Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var advisor = game.Advisor!;
        for (int i = 0; i < AdvisorPresenter.EventStep; i++) UiTestKit.Press(Ui.Q<Button>("advisor-got-it"));
        UiTestKit.Press(Ui.Q<Button>("advisor-close"));
        AnswerTheCard();
        Assert.IsFalse(advisor.InChain, "closed, it stays closed");
        Assert.AreEqual(-1, advisor.ChainStep);
        Assert.IsFalse(Ui.Q("root").ClassListContains(AdvisorPresenter.OpenClass));

        using var rig = new Rig(En);
        var bridge = rig.Bridge;
        var lone = new AdvisorPresenter(rig.Ui.Root, bridge, En, new AdvisorSession(Approved(), new AdvisorSettings(null), false), hooks: new AdvisorHooks { Card = rig.Card });
        lone.Dispose();
        rig.Advisor.OpenChain();                                               // another advisor open on the same HUD (the root's class)
        for (int i = 0; i < AdvisorPresenter.EventStep; i++) rig.Advisor.NextStep();
        rig.Card.Reopen();                                                     // the chain's event step waits for the card
        UiTestKit.Press(rig.Ui.Root.Q("event-options").Query<Button>().First());
        Assert.IsNull(rig.Card.Shown, "precondition: the card answered");
        Assert.AreEqual(-1, lone.ChainStep, "the disposed advisor was not called");
    }

    /// <summary>The game changing under an open step (a + or − on the city's workers raises SimBridge.Changed) draws the step
    /// again without showing its city again: a hex the player picked meanwhile stays selected (review of the S2 draft).</summary>
    [Test]
    public void TheGameChangingKeepsThePlayersSelectionOnAStep()
    {
        using var rig = new Rig(En, new SettingsStore(TempDir()));
        rig.Advisor.OpenChain(); rig.Advisor.NextStep(); rig.Advisor.NextStep();
        Assert.AreEqual("opening.city", Steps[rig.Advisor.ChainStep], "precondition: the city step");
        var city = AdvisorTargets.CityHex(rig.Bridge.Content, rig.Bridge.State)!.Value;
        Assert.AreEqual(city, rig.Info.Selected, "precondition: it shows its city");
        var other = rig.Bridge.Content.Hexes.Where(h => h.Land).Select(h => (h.Q, h.R)).First(h => h != city);
        rig.Info.Show(other.Item1, other.Item2);                        // the player picks another hex under the open advisor
        int changed = 0; rig.Bridge.Changed += () => changed++;
        Assert.IsNull(rig.Bridge.Queue(new AssignWorkers("tel_aviv", "job", 1)), "precondition: the order is queued");
        Assert.AreEqual(1, changed, "precondition: the game changed");
        Assert.AreEqual(other, rig.Info.Selected, "the player's hex stays");
        Assert.IsTrue(rig.Advisor.InChain); Assert.AreEqual("opening.city", Steps[rig.Advisor.ChainStep], "on the same step");
        rig.Ui.Press("advisor-close");
        Assert.AreEqual(other, rig.Info.Selected, "and Skip keeps the player's choice");
    }

    // --- Presentation only ------------------------------------------------------------------------------------------

    /// <summary>The whole advisor, by itself, through How to play, through every step, Skip and Escape: the state hash, the
    /// session, the queued orders and the save file are what they were.</summary>
    [UnityTest]
    public IEnumerator ChangesNeitherTheSimNorTheSave()
    {
        var store = new SaveStore(TempDir());
        var settings = new SettingsStore(TempDir());
        var game = OpenScene(store, settings);
        Begin(game, Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsTrue(game.Advisor!.InChain);
        var bridge = game.Bridge!;
        bridge.EndTurn();                                               // a turn played, so there is a save to compare
        if (Ui.Q("root").ClassListContains(TurnReportPresenter.OpenClass)) UiTestKit.Press(Ui.Q<Button>("turn-report-close"));
        var session = bridge.Session;
        var hash = bridge.State.Hash();
        var save = store.ReadAutosave();
        Assert.IsNotNull(save, "precondition: the turn was saved");
        var queued = bridge.Queued.Count;

        void Next(int times) { for (int i = 0; i < times; i++) UiTestKit.Press(Ui.Q<Button>("advisor-got-it")); }
        void FromHowToPlay() { UiTestKit.Press(Ui.Q<Button>("nav-how-to-play")); UiTestKit.Press(Ui.Q<Button>("how-to-play-guide")); }
        Next(Steps.Length);                     // the advisor that opened by itself, to Done
        Assert.IsFalse(game.Advisor.InChain);
        FromHowToPlay(); Next(2); UiTestKit.Press(Ui.Q<Button>("advisor-close"));
        FromHowToPlay(); Next(3); yield return null; Ui.Q<Button>("advisor-got-it").Focus(); Assert.IsTrue(game.Advisor.Escape());
        Assert.IsFalse(game.Advisor.InChain);
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
                yield return EachStep(w, h, hebrew, (root, advisor, step, eventCard) =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h} step {step + 1} ({Steps[step]})";
                    var window = root.worldBound;
                    var targets = Targets(root, advisor, eventCard, step);
                    if (Steps[step] == "opening.objectives" &&
                        (targets.Count != 1 || targets[0].name != "nav-objectives"))
                        problems.Add($"{at}: the last step must point at the Objectives button");
                    if (Steps[step] == "opening.move" &&
                        (targets.Count != 1 || targets[0].name != "info-move" || ((Button)targets[0]).text != (hebrew ? He : En)["unit.order.move"]))
                        problems.Add($"{at}: the move step must point at the unit card's Move button");
                    if (step == 0)
                    {
                        if (targets.Count != 0 || root.Q("advisor-ring").resolvedStyle.display == DisplayStyle.Flex) problems.Add($"{at}: greeting has a ring");
                        if (!Inside(root.Q("advisor-popup").worldBound, window)) problems.Add($"{at}: greeting popup outside window");
                        return;
                    }
                    if (targets.Count == 0) { problems.Add($"{at}: no target shows"); return; }
                    var area = AdvisorTargets.Area(targets)!.Value;
                    if (!Inside(area, window)) problems.Add($"{at}: the target {area} leaves the window {window}");
                    var ring = root.Q("advisor-ring");
                    if (ring.resolvedStyle.display != DisplayStyle.Flex) problems.Add($"{at}: no ring");
                    else
                    {
                        if (!Inside(area, ring.worldBound)) problems.Add($"{at}: the ring {ring.worldBound} is not round the target {area}");
                        if (!Inside(ring.worldBound, window)) problems.Add($"{at}: the ring {ring.worldBound} leaves the window {window}");
                        // Step 1's ring round Attention must read as its own box, not one with End turn's gold border under it.
                        var endTurn = root.Q("end-turn").worldBound;
                        if (step == 1 && Gap(ring.worldBound, endTurn) < RingClearPx)
                            problems.Add($"{at}: the ring {ring.worldBound} is {Gap(ring.worldBound, endTurn):0.#} px from End turn {endTurn} (under {RingClearPx})");
                    }
                    var panel = root.Q("advisor-popup").worldBound;
                    if (!(panel.width > 0) || !Inside(panel, window)) problems.Add($"{at}: the panel {panel} leaves the window {window}");
                    if (panel.Overlaps(area)) problems.Add($"{at}: the panel {panel} covers the target {area}");
                    // The ring too (its gap and border round the target), so the larger text (player feedback, 21 October
                    // 2026) never pushes the panel onto the gold box it points at.
                    if (ring.resolvedStyle.display == DisplayStyle.Flex && panel.Overlaps(ring.worldBound))
                        problems.Add($"{at}: the panel {panel} covers the ring {ring.worldBound}");
                    foreach (var name in new[] { "advisor-name", "advisor-text", "advisor-close", "advisor-got-it" })
                        if (!Inside(root.Q(name).worldBound, panel)) problems.Add($"{at}: {name} {root.Q(name).worldBound} leaves the panel {panel}");
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>Player feedback relayed by the owner (21 October 2026: the advisor's text was too small; "make it slightly
    /// bigger"): at 1280x720 (the itch embed) and 1920x1080, in English and Hebrew, the advisor's text is at least
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
                yield return EachStep(w, h, hebrew, (root, advisor, step, eventCard) =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h} step {step + 1}";
                    float scale = root.panel.scaledPixelsPerPoint;
                    float OnScreen(string name) => root.Q(name).resolvedStyle.fontSize * scale;
                    var panel = root.Q("advisor-popup").worldBound;
                    Debug.Log($"GUIDE SIZE {at}: scale {scale:0.###}, text {OnScreen("advisor-text"):0.#} px, title {OnScreen("advisor-name"):0.#} px, " +
                              $"count {OnScreen("advisor-count"):0.#} px, buttons {OnScreen("advisor-got-it"):0.#} px on screen; panel {panel.width * scale:0}x{panel.height * scale:0} px");
                    if (w == 390) return;
                    if (OnScreen("advisor-text") < GuideTextOnScreenPx - 0.01f)
                        problems.Add($"{at}: the advisor's text is {OnScreen("advisor-text"):0.##} px on screen (under {GuideTextOnScreenPx})");
                    if (OnScreen("advisor-count") < HudPresenter.TextFloorPx - 0.01f)
                        problems.Add($"{at}: the step count is {OnScreen("advisor-count"):0.##} px on screen (under the {HudPresenter.TextFloorPx} px floor)");
                    if (OnScreen("advisor-name") <= OnScreen("advisor-text"))
                        problems.Add($"{at}: the title ({OnScreen("advisor-name"):0.#} px) is not larger than the text ({OnScreen("advisor-text"):0.#} px)");
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>The owner's text floor on the advisor too (21 October 2026, after #1283): at every window, the phone held upright
    /// (390x844, about 0.49 scale) included, in English and Hebrew, the step count, the title, the text and both buttons
    /// are at least HudPresenter.TextFloorPx on screen, the top bar's rule (HudPresenter.SmallTextSize); where the scale
    /// keeps them over it (1920x1080, 1280x720) they are the stylesheet's sizes from #1283, unchanged. Before the change
    /// the phone's text was about 10.8 px on screen.</summary>
    [UnityTest]
    public IEnumerator TheGuideKeepsTheTextFloorOnEveryWindow()
    {
        var problems = new List<string>();
        // Statecraft.uss: .advisor-count, .advisor-name, .advisor-text, Button (Skip and Next).
        var stylesheet = new Dictionary<string, float> { ["advisor-count"] = 15f, ["advisor-name"] = 24f, ["advisor-text"] = 22f, ["advisor-close"] = 19f, ["advisor-got-it"] = 19f };
        CollectionAssert.AreEqual(new[] { 15f, 24f, 22f, 19f },
            new[] { AdvisorPresenter.CountPx, AdvisorPresenter.NamePx, AdvisorPresenter.TextPx, AdvisorPresenter.ButtonPx },
            "the presenter's sizes mirror the stylesheet's");
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (1024, 768), (390, 844) })
                yield return EachStep(w, h, hebrew, (root, advisor, step, eventCard) =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h} step {step + 1}";
                    float scale = root.panel.scaledPixelsPerPoint;
                    foreach (var (name, px) in stylesheet)
                    {
                        float size = root.Q(name).resolvedStyle.fontSize;
                        if (size * scale < HudPresenter.TextFloorPx - 0.01f)
                            problems.Add($"{at}: {name} is {size * scale:0.##} px on screen ({size} px at {scale:0.###}; under the {HudPresenter.TextFloorPx} px floor)");
                        if (Mathf.Abs(size - HudPresenter.FlooredTextSize(px, HudPresenter.ScreenPxPerPanelPx(root.panel))) > 0.01f)
                            problems.Add($"{at}: {name} is {size} px, not the stylesheet's {px} px (the desktop sizes stay)");
                    }
                    // Where the floor lifts both (the phone), the name (24 px) and the text (22 px) meet at the floor; it is
                    // never smaller. TheGuideReadsAtTheItchEmbedSize holds it larger where neither is floored.
                    if (root.Q("advisor-name").resolvedStyle.fontSize < root.Q("advisor-text").resolvedStyle.fontSize)
                        problems.Add($"{at}: the title is smaller than the text");
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>The advisor's text on screen at 1280x720 and larger, in px (22 panel px at 0.67 scale is 14.7).</summary>
    const float GuideTextOnScreenPx = 14f;

    /// <summary>The least clear space between the ring and End turn for them to read as two boxes, in panel px.</summary>
    const float RingClearPx = 4f;

    /// <summary>The clear space between two rects (the larger of the horizontal and vertical gaps; negative when they overlap).</summary>
    static float Gap(Rect a, Rect b) =>
        Mathf.Max(Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax), Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));

    static bool Inside(Rect inner, Rect outer) =>
        inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;

    /// <summary>The real Game.uxml on the real PanelSettings at w x h with the HUD, the opening card and the info panel on a
    /// new game; the advisor opened, and <paramref name="check"/> run at each step once the layout has settled.</summary>
    static IReadOnlyList<VisualElement> Targets(VisualElement root, AdvisorPresenter advisor, EventCardPresenter card, int step)
    {
        var action = advisor.Chain[step].Actions.FirstOrDefault(a => a.ShowMe != "");
        return action == null ? Array.Empty<VisualElement>() : AdvisorTargets.Find(root.Q("root"), action.ShowMe, card);
    }

    static IEnumerator EachStep(int w, int h, bool hebrew, Action<VisualElement, AdvisorPresenter, int, EventCardPresenter> check)
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
        var (info, advisor) = PanelAndAdvisor(root, bridge, text, null, card, () => nav.Close());
        try
        {
            Assert.IsNotNull(card.Shown, "precondition: a new game opens on a card");
            advisor.OpenChain();
            for (int step = 0; step < Steps.Length; step++)
            {
                if (step > 0) advisor.NextStep();
                for (int i = 0; i < 20 && step > 0 && Targets(root, advisor, card, step).Count == 0; i++) yield return null;
                for (int i = 0; i < 3; i++) yield return null;   // the layout settles
                advisor.Place();
                for (int i = 0; i < 2; i++) yield return null;   // the panel's dock follows the ring's side
                check(root, advisor, step, card);
            }
        }
        finally
        {
            advisor.Dispose(); info.Dispose(); card.Dispose(); nav.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }
}
