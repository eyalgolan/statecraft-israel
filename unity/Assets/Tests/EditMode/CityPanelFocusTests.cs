#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#41, the keyboard-only turn's "Still open" list (owner, 23 October 2026), item 2: a city panel button that is
/// gone or disabled after the press still drops the focus. #1330's K2 fix gives the focus to the rebuilt button of the
/// same name, but the Assign stepper's minus disables itself at 0 (`GddHexPanel`, "city-workers-less",
/// SetEnabled(target > 0)), so after the press that reaches 0 there is no button able to take it and the focus is lost:
/// the next Tab starts again at Cabinet, which is the H2 symptom K2 was meant to end. Same harness as
/// KeyboardTurnTests.</summary>
public class CityPanelFocusTests
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-citypanel-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    SimBridge Bridge => root.Bridge!;
    Focusable? Focused => ui.panel?.focusController?.focusedElement;
    VisualElement At => Focused as VisualElement ?? throw new AssertionException("nothing has the focus");
    string FocusedName => (Focused as VisualElement)?.name is string n && n.Length > 0 ? n : Focused == null ? "none" : Focused.GetType().Name;

    IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    IEnumerator Tab(bool back = false)
    {
        var at = At;
        using (var e = NavigationMoveEvent.GetPooled(back ? NavigationMoveEvent.Direction.Previous : NavigationMoveEvent.Direction.Next)) { e.target = at; at.SendEvent(e); }
        yield return null;
    }

    IEnumerator Enter()
    {
        var at = At;
        using (var k = KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None)) { k.target = at; at.SendEvent(k); }
        using (var s = NavigationSubmitEvent.GetPooled()) { s.target = at; at.SendEvent(s); }
        yield return null;
    }

    IEnumerator TabTo(string name, int max = 80)
    {
        for (int i = 0; i < max && FocusedName != name; i++) yield return Tab();
        Assert.AreEqual(name, FocusedName, $"Tabbed to {name}");
    }

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
    }

    static void LookAtTelAviv()
    {
        var map = Object.FindFirstObjectByType<MapScene>();
        var c = MapFocusTests.Inputs.Content;
        var tlv = c.SettlementHexes.Single(s => s.Id == "tel_aviv");
        var hex = c.HexAt(tlv.Q, tlv.R)!;
        var ground = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        ground.y = map.Terrain.SampleHeight(ground) + map.Terrain.transform.position.y;
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = ground; rig.Distance = 30000f;
        rig.Apply();
    }

    /// <summary>Opens Tel Aviv's city panel, as ATrainingOrderByThePointerLeavesNoFocus does. The attention is raised for
    /// the same reason KeyboardTurnTests raises it: what is checked here is the keyboard's path, not the attention rule,
    /// and the opening turn's 4 attention does not pay for stepping the Assign count down to zero.</summary>
    IEnumerator OpenTelAviv()
    {
        yield return Boot();
        Bridge.State.Attention = 100;
        GameSceneTests.HideTheOpeningCard(ui);
        LookAtTelAviv();
        var stop = ui.Q("map-focus");
        for (int i = 0; i < 10 && Focused != stop; i++) { yield return null; stop.Focus(); }
        yield return Enter();
        Assert.AreEqual("Tel Aviv", ui.Q<Label>("info-title").text, "precondition: Tel Aviv's city panel");
    }

    [UnityTest]
    public IEnumerator TheAssignMinusKeepsTheFocusWhenItDisablesItselfAtZero()
    {
        yield return OpenTelAviv();

        var less = ui.Q<Button>("city-workers-less");
        Assert.IsNotNull(less, "precondition: the Assign stepper's minus is in the panel");
        Assert.IsTrue(less.enabledSelf, "precondition: it starts enabled, so some workers are assigned");

        // Tel Aviv starts with 86 groups in work, so stepping down one press at a time would take 86 presses. Order one
        // group directly, which is what the stepper itself queues, so a single press then reaches 0 and disables the
        // button. That is the press this test is about.
        Bridge.Queue(new AssignWorkers("tel_aviv", "job", 1));
        yield return Frames(5);
        var one = ui.Q<Button>("city-workers-less");
        Assert.IsNotNull(one, "precondition: the minus is still in the panel at 1");
        Assert.IsTrue(one.enabledSelf, $"precondition: the minus is enabled at 1; the count reads {ui.Q<Label>("city-workers")?.text}");
        one.Focus();
        yield return null;
        Assert.AreEqual("city-workers-less", FocusedName, "precondition: the minus has the focus before the press");

        yield return Enter();
        yield return Frames(5);

        var after = ui.Q<Button>("city-workers-less");
        Assert.IsTrue(after == null || !after.enabledSelf,
            $"precondition: the minus is gone or disabled after the press that reaches 0, but it is still enabled; the count reads {ui.Q<Label>("city-workers")?.text}");

        Assert.IsNotNull(Focused, "the focus is not dropped when the pressed button cannot take it back");
        Assert.IsTrue(ui.Q("info-panel")?.Contains(At) ?? false,
            $"the focus stays in the city panel, but it is on {FocusedName}");
        Assert.AreEqual("city-workers-more", FocusedName, "and on the stepper's plus, the nearest control that can take it");
    }
}
