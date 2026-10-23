#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#754's keyboard part (the plan's PR 15 item "Keyboard access follows the a11y plan", left free in the
/// owner's 22 October 2026 comment): a Move and an Attack given with the keyboard alone. Map.unity as the player gets
/// it, at 1280x720, a new GDD game: answer the opening card, Tab to the map stop (MapFocusPresenter, a11y plan B1b)
/// over Golani's hex and press Enter to select it, Tab to the card's Move or Attack and press Enter, Tab round to the
/// map stop again over a lit hex and press Enter: the sim's order is queued. Keys reach the UI as the UI Toolkit events
/// the Input System's UI provider makes of them (Tab: NavigationMoveEvent Next; Enter: KeyDownEvent then
/// NavigationSubmitEvent), as in MapFocusTests: an edit-mode panel gets no device input. The pan under the ring moves
/// the camera's look-at point as the arrow keys would; that the arrows still pan with the stop focused is
/// MapFocusCameraKeysTests'.</summary>
public class KeyboardOrdersTests
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-kborders-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    /// <summary>Map.unity on a 1280x720 panel, a new GDD game begun as GameBoot.Ready does with no start screen, and the
    /// opening event card answered from the keyboard (which hands the focus to End Turn).</summary>
    IEnumerator Boot()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(1280, 720, 0);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(MapFocusTests.Inputs.Map);
        root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(MapFocusTests.Inputs, 0);
        ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(5);
        Assert.IsTrue(Bridge.Content.IsGdd, "precondition: the GDD loop");
        Assert.AreEqual("event-card", FocusedName, "precondition: the open card has the focus");
        yield return TabTo(v => v.ClassListContains(EventCardPresenter.OptionClass), "an option");
        yield return Enter();
        Assert.AreEqual("end-turn", FocusedName, "the answered card hands the focus to End Turn");
    }

    SimBridge Bridge => root.Bridge!;
    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
    Focusable? Focused => ui.panel.focusController.focusedElement;
    string FocusedName => (Focused as VisualElement)?.name is string n && n.Length > 0 ? n : Focused == null ? "none" : Focused.GetType().Name;
    VisualElement At => Focused as VisualElement ?? throw new AssertionException("nothing has the focus");

    IEnumerator Tab()
    {
        var at = At;
        using (var e = NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next)) { e.target = at; at.SendEvent(e); }
        yield return null;
    }

    IEnumerator Enter()
    {
        var at = At;
        using (var e = KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None)) { e.target = at; at.SendEvent(e); }
        using (var e = NavigationSubmitEvent.GetPooled()) { e.target = at; at.SendEvent(e); }
        yield return Frames(3);
    }

    /// <summary>Tabs until <paramref name="want"/> has the focus, at most <paramref name="max"/> times; fails with the path
    /// taken.</summary>
    IEnumerator TabTo(Func<VisualElement, bool> want, string what, int max = 60)
    {
        var seen = new List<string> { FocusedName };
        for (int i = 0; i < max && !want(At); i++) { yield return Tab(); seen.Add(FocusedName); }
        Assert.IsTrue(want(At), $"Tab reaches {what}: {string.Join(" > ", seen)}");
    }

    IEnumerator TabTo(string name) => TabTo(v => v.name == name, name);

    /// <summary>Tab to the map stop, pan the hex (q, r) under its ring, Enter.</summary>
    IEnumerator EnterOnTheMapAt(int q, int r)
    {
        yield return TabTo("map-focus");
        var map = Object.FindFirstObjectByType<MapScene>();
        var hex = Bridge.Content.HexAt(q, r)!;
        var ground = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        ground.y = map.Terrain.SampleHeight(ground) + map.Terrain.transform.position.y;
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = ground; rig.Distance = 30000f;   // as a pan with the arrows would (MapFocusTests)
        rig.Apply();
        yield return Frames(3);
        yield return Enter();
        Assert.AreEqual("map-focus", FocusedName, "the focus stays on the map stop");
    }

    /// <summary>Selects Golani from the keyboard (Enter on the map stop over its hex) and hands back its sim id.</summary>
    IEnumerator SelectGolani(Action<int> id)
    {
        var layer = Object.FindFirstObjectByType<UnitLayer>();
        var golani = layer.Tokens.Single(p => p.Token.Unit.Id == "golani");
        yield return EnterOnTheMapAt(golani.Q, golani.R);
        Assert.AreEqual("golani", layer.Selected, "Enter on the map stop over Golani selects it");
        id(UnitCard.SimUnit(Bridge.Content, Bridge.State, golani.Token.Unit)!.Value);
    }

    /// <summary>Select Golani, Tab to Move, Enter (its reachable hexes light), Tab round to the map stop over a lit hex,
    /// Enter: the sim's Move is queued and Golani's card stays.</summary>
    [UnityTest]
    public IEnumerator AMoveIsGivenWithTheKeyboardAlone()
    {
        yield return Boot();
        var id = -1;
        yield return SelectGolani(i => id = i);
        yield return TabTo("info-move");
        yield return Enter();
        var reach = Movement.Reachable(Bridge.Content, Bridge.State, id).Keys.ToList();
        Assert.IsNotEmpty(reach, "precondition: Golani can move");
        Assert.IsTrue(Object.FindFirstObjectByType<MapScene>().Terrain.transform.Find(MapScene.ReachableName).GetComponent<MeshRenderer>().enabled,
            "Enter on Move lights the reachable hexes");
        var (q, r) = reach.Last();
        yield return EnterOnTheMapAt(q, r);
        var queued = Bridge.Queued.OfType<Move>().Single();
        Assert.AreEqual((id, q, r), (queued.Unit, queued.Q, queued.R), "Enter on a lit hex queues the Move");
        Assert.AreEqual("golani", Object.FindFirstObjectByType<UnitLayer>().Selected, "Golani stays selected");
    }

    /// <summary>With an enemy in Golani's reach and the Arms for it: select Golani, Tab to Attack, Enter (the hexes the
    /// sim's Combat.Check takes light), Tab round to the map stop over one, Enter: the sim's attack is queued.</summary>
    [UnityTest]
    public IEnumerator AnAttackIsGivenWithTheKeyboardAlone()
    {
        yield return Boot();
        var expected = UnitOrdersTests.EnemyInGolanisReach(Bridge.State);
        var id = -1;
        yield return SelectGolani(i => id = i);
        Assert.AreEqual(expected, id);
        yield return TabTo("info-attack");
        yield return Enter();
        var s = Bridge.State;
        var targets = s.Units.Values.Select(u => (u.Q, u.R)).Distinct().Where(h => Combat.Check(Bridge.Content, s, id, h.Item1, h.Item2) == null).ToList();
        Assert.IsNotEmpty(targets, "precondition: Golani has a hex to attack");
        Assert.IsTrue(Object.FindFirstObjectByType<MapScene>().Terrain.transform.Find(MapScene.ReachableName).GetComponent<MeshRenderer>().enabled,
            "Enter on Attack lights the hexes it can name");
        var (q, r) = targets[0];
        yield return EnterOnTheMapAt(q, r);
        var queued = Bridge.Queued.OfType<UnitOrder>().Single();
        Assert.AreEqual((id, Combat.Attack, (int?)q, (int?)r), (queued.Unit, queued.Order, queued.Q, queued.R), "Enter on a lit hex queues the attack");
    }
}
