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

/// <summary>Writes the before and after stills for #41's "Still open" items 1 and 2, in English and Hebrew, to
/// `.superpowers/a11y-keyboard-41/` as the 14 October audit does (local, not committed). Both "before" states are the old
/// code paths called directly rather than a mock: item 1 puts the scrollers back in the Tab order, which is what they were
/// before `BarsAreNotTabStops`; item 2 calls `FocusReturn.To` on the button the press disabled, which is what
/// `InfoPanelPresenter` did before `ToOrWithin`. Explicit so this does not run in CI with the rest.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class A11yKeyboardShots
{
    string saveDir = "";
    string shotDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp]
    public void TempSaves()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-shots-" + Guid.NewGuid().ToString("N"));
        shotDir = Path.Combine(Directory.GetCurrentDirectory(), ".superpowers", "a11y-keyboard-41");
        Directory.CreateDirectory(shotDir);
    }

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

    IEnumerator Boot(bool hebrew)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(1280, 720, 24);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(MapFocusTests.Inputs.Map);
        root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        if (hebrew) root.Language = UiLanguage.Hebrew;
        root.Begin(MapFocusTests.Inputs, 0);
        ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(5);
    }

    void Shot(string name)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt!.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(Path.Combine(shotDir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log($"SHOT {name}: focus is on {FocusedName}");
    }

    static void PutScrollBarsBackInTheOrder(params ScrollView?[] views)
    {
        foreach (var v in views)
        {
            if (v == null) continue;
            foreach (var s in new[] { v.verticalScroller, v.horizontalScroller })
            {
                if (s == null) continue;
                s.focusable = true;
                if (s.slider != null) s.slider.focusable = true;
            }
        }
    }

    IEnumerator Shots(bool hebrew)
    {
        var lang = hebrew ? "he" : "en";
        yield return Boot(hebrew);
        Bridge.State.Attention = 100;
        GameSceneTests.HideTheOpeningCard(ui);
        yield return Frames(3);

        // ---- item 1: the Cabinet's scroll bars ----
        var nav = ui.Q<Button>("nav-cabinet");
        nav.Focus(); yield return null;
        yield return Enter();
        yield return Frames(5);

        var seats = ui.Q<ScrollView>("cabinet-seats");
        var cands = ui.Q<ScrollView>("cabinet-candidates");

        // before: the scrollers back in the order, Tab from the last seat lands on the bar
        PutScrollBarsBackInTheOrder(seats, cands);
        var last = ui.Q<Button>("seat-chief_of_staff");
        if (last != null) { last.Focus(); yield return null; yield return Tab(); yield return Frames(2); }
        Shot($"item1-before-{lang}");

        // after: the fix as it ships; the same Tab goes on to the next control
        FocusScroll.BarsAreNotTabStops(seats, cands);
        if (last != null) { last.Focus(); yield return null; yield return Tab(); yield return Frames(2); }
        Shot($"item1-after-{lang}");

        // ---- item 2: the city panel's Assign minus at 0 ----
        ui.Q<Button>("screen-close-cabinet")?.Focus(); yield return null;
        yield return Enter();
        yield return Frames(3);
        LookAtTelAviv();
        var stop = ui.Q("map-focus");
        for (int i = 0; i < 10 && Focused != stop; i++) { yield return null; stop.Focus(); }
        yield return Enter();
        yield return Frames(3);
        Bridge.State.Attention = 100;
        Bridge.Queue(new AssignWorkers("tel_aviv", "job", 1));
        yield return Frames(5);

        var less = ui.Q<Button>("city-workers-less");
        if (less != null)
        {
            less.Focus(); yield return null;
            yield return Enter();                 // reaches 0; the button disables itself
            yield return Frames(5);
            Shot($"item2-after-{lang}");          // the fix: the focus stays in the panel

            // before: what InfoPanelPresenter did, FocusReturn.To onto the disabled button
            var dead = ui.Q<Button>("city-workers-less");
            ui.panel?.focusController?.focusedElement?.Blur();
            yield return null;
            FocusReturn.To(dead);
            yield return Frames(3);
            Shot($"item2-before-{lang}");
        }
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

    [UnityTest] public IEnumerator English() { yield return Shots(false); }
    [UnityTest] public IEnumerator Hebrew() { yield return Shots(true); }
}
