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

/// <summary>Map.unity as the player gets it: GameRoot wired to the boot, the map, the camera, the pointer, the placement
/// layer and the UI document, every presenter created and disposed with it, and the scripted smoke run (plan Task 23 and
/// the carry list to Task 23).</summary>
public class GameSceneTests
{
    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    string saveDir = "";

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-scene-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
        else if (File.Exists(saveDir)) File.Delete(saveDir);
    }

    /// <summary>Opens Map.unity, builds the map from the repository inputs and starts the game the way GameBoot.Ready does,
    /// with the saves in a temporary folder (never the player's real autosave).</summary>
    GameRoot Boot(SaveStore? store = null)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = store ?? new SaveStore(saveDir);
        root.Begin(Inputs, 0);
        return root;
    }

    static VisualElement Ui => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;

    [Test]
    public void MapUnityIsTheOnlyBuildSceneAndIsFullyWired()
    {
        Assert.AreEqual(new[] { "Assets/Map.unity" }, Array.ConvertAll(EditorBuildSettings.scenes, s => s.path));
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var root = Object.FindFirstObjectByType<GameRoot>();
        Assert.IsNotNull(root, "GameRoot");
        var so = new SerializedObject(root);
        foreach (var field in new[] { "boot", "document", "strings", "stringsHe", "portraits", "map", "placements", "pointer", "cameraController" })
            Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, field);
        var boot = Object.FindFirstObjectByType<GameBoot>();
        Assert.IsNotNull(boot);
        Assert.AreSame(Object.FindFirstObjectByType<MapScene>(), new SerializedObject(boot).FindProperty("map").objectReferenceValue, "GameBoot.map");
        var map = new SerializedObject(Object.FindFirstObjectByType<MapScene>());
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain.mat"), map.FindProperty("terrainMaterial").objectReferenceValue);
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SettlementMarker.mat"), map.FindProperty("markerMaterial").objectReferenceValue);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel), doc.panelSettings);
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml), doc.visualTreeAsset);
        Assert.IsNotNull(Camera.main.GetComponent<CameraController>());
        Assert.IsNotNull(Camera.main.GetComponent<CameraRig>());
        Assert.IsNotNull(Object.FindFirstObjectByType<PlacementLayer>());
        Assert.IsNotNull(Object.FindFirstObjectByType<MapPointer>());
        Assert.IsNull(GameObject.Find("EventSystem"), "UI Toolkit reads the Input System directly; an EventSystem would take over");
    }

    /// <summary>The game as automated runs boot it (no start screen): no DeclarationRecording is built, so the
    /// "Play the recording" row keeps the stylesheet's display: none and its button is never set up.</summary>
    [Test]
    public void WithoutTheStartScreenThereIsNoRecording()
    {
        Boot();
        Assert.AreEqual(StyleKeyword.Null, Ui.Q("start-recording").style.display.keyword, "nothing shows the row");
        Assert.AreEqual("", Ui.Q<Button>("start-listen").text, "the button was never set up");
        Assert.AreEqual(StyleKeyword.Null, Ui.Q("start-caption").style.display.keyword);
    }

    /// <summary>The booted game (video on in an edit-mode run): the End Turn outcome clips have their own player, so the
    /// decision card's Render can never stop or replace one (review of #466, finding 1).</summary>
    [Test]
    public void TheOutcomeClipsHaveTheirOwnPlayer()
    {
        var root = Boot();
        Assert.IsFalse(CardVideo.ForcedOff(), "precondition: an edit-mode run has video");
        object? Field(object o, string name) => o.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(o);
        var outcomes = Field(root, "outcomes"); var card = Field(root, "card");
        Assert.IsNotNull(outcomes); Assert.IsNotNull(card);
        var outcomePlayer = Field(outcomes!, "video"); var cardPlayer = Field(card!, "video");
        Assert.IsNotNull(outcomePlayer); Assert.IsNotNull(cardPlayer);
        Assert.AreNotSame(cardPlayer, outcomePlayer);
    }

    [Test]
    public void TheGameStartsInTheSceneOnceTheInputsAreLoaded()
    {
        var root = Boot();
        Assert.IsNotNull(root.Bridge);
        Assert.AreEqual(root.Bridge!.State.Date.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), Ui.Q<Label>("year").text);
        Assert.AreEqual("Click the map to inspect a hex", Ui.Q<Label>("info-title").text);
        Assert.AreEqual(new DateTime(1948, 5, 15), root.Bridge.State.Date, "no autosave: a new game");
        var pointer = Object.FindFirstObjectByType<MapPointer>();
        Assert.AreSame(Object.FindFirstObjectByType<MapScene>(), pointer.Map);
        Assert.IsNotNull(pointer.IsOverUi);
        Assert.IsNotNull(Camera.main.GetComponent<CameraController>().IsOverUi);
        var size = Object.FindFirstObjectByType<MapScene>().Terrain.terrainData.size;
        Assert.AreEqual(size.x, Camera.main.GetComponent<CameraController>().Bounds.width, 1f);
        Assert.AreEqual(size.z, Camera.main.GetComponent<CameraController>().Bounds.height, 1f);
    }

    /// <summary>Review of #602 (must-fix): with a decision card open the map is blocked everywhere but the left icon column's
    /// own buttons. Probes in the column's gutter (left of the discs, between two discs, below the last) reach the map while
    /// no card is open (a hex click picks a hex there), and are UI while the card is open: the map pointer refuses the hex
    /// click and the camera refuses the drag there. Before the fix the backdrop started at the column's right edge and the
    /// column takes no pointer, so the strip leaked through to the map.</summary>
    [UnityTest]
    public IEnumerator WithACardOpenTheColumnsGutterBlocksTheMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var rt = new RenderTexture(1920, 1080, 0);
        var ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
        try
        {
            doc.panelSettings = ps;
            Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
            var root = Object.FindFirstObjectByType<GameRoot>();
            root.Store = new SaveStore(saveDir);
            root.Begin(Inputs, 0);
            var ui = doc.rootVisualElement;
            for (int i = 0; i < 30 && !(ui.Q("nav-why").worldBound.width > 0); i++) yield return null;
            var panel = ui.panel;
            float h = Screen.height;
            Vector2 o = UiPointer.ToPanel(panel, Vector2.zero, h), ex = UiPointer.ToPanel(panel, Vector2.right * 100, h) - o, ey = UiPointer.ToPanel(panel, Vector2.up * 100, h) - o;
            Vector2 ScreenFor(Vector2 p) => new Vector2((p - o).x / ex.x * 100, (p - o).y / ey.y * 100);
            Rect a = ui.Q("nav-cabinet").worldBound, b = ui.Q("nav-budget").worldBound, last = ui.Q("nav-why").worldBound;
            var probes = new (string where, Vector2 p)[]
            {
                ("left of a disc", new Vector2(a.xMin - 6f, a.center.y)),
                ("between two discs", new Vector2(a.center.x, (a.yMax + b.yMin) / 2)),
                ("right of a disc, inside the column's strip", new Vector2(a.xMax + 4f, a.center.y)),
                ("below the last disc", new Vector2(last.center.x, last.yMax + 80f)),
            };
            var pointer = Object.FindFirstObjectByType<MapPointer>();
            var camera = Camera.main.GetComponent<CameraController>();
            var bridge = root.Bridge!;
            // Since the GDD cutover the game opens on a card (One Army, 15 May 1948): take it away for the "no card" half.
            var none = GameSession.New(Inputs.Content, GoldenRun.Seed, Mode.Historical);
            none.State.PendingEvents.Clear();
            bridge.Replace(none);
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsFalse(ui.Q("root").ClassListContains(EventCardPresenter.ModalClass), "precondition: no card");
            foreach (var (where, p) in probes)
            {
                Assert.IsFalse(pointer.IsOverUi!(ScreenFor(p)), $"no card: {where} {p} is map");
                Assert.IsTrue(pointer.HandleClick(ScreenFor(p)), $"no card: a hex click {where} {p} picks a hex (the probe is on the map)");
            }
            var g = GameSession.New(Inputs.Content, GoldenRun.Seed, Mode.Historical);
            while (g.State.PendingEvents.Count == 0) g.Step(new Command[0]);
            bridge.Replace(g);
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsTrue(ui.Q("root").ClassListContains(EventCardPresenter.ModalClass), "precondition: the card is open");
            foreach (var (where, p) in probes)
            {
                Assert.IsTrue(pointer.IsOverUi!(ScreenFor(p)), $"card open: {where} {p} blocks map clicks");
                Assert.IsFalse(pointer.HandleClick(ScreenFor(p)), $"card open: a hex click {where} {p} picks no hex");
                Assert.IsTrue(camera.IsOverUi!(ScreenFor(p)), $"card open: {where} {p} blocks the drag and the wheel");
            }
            UiTestKit.Press(ui.Q<Button>("nav-why"));
            Assert.AreEqual(DisplayStyle.Flex, ui.Q("screen-why").style.display.value, "card open: the column's own buttons still work");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (ps != null) Object.DestroyImmediate(ps);
            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        }
    }

    // Carry 2 and 8: ScreenNav hides every screen at start; the empty middle of the screen reaches the map while the
    // panels do not (the camera and the map pointer ask the game panel); a minimap click moves the camera.
    [UnityTest]
    public IEnumerator AfterBootNoScreenIsOpenThePanelsBlockTheMapAndTheMinimapMovesTheCamera()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var rt = new RenderTexture(1920, 1080, 0);
        var ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
        try
        {
            doc.panelSettings = ps;
            Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
            var root = Object.FindFirstObjectByType<GameRoot>();
            root.Store = new SaveStore(saveDir);
            root.Begin(Inputs, 0);
            var ui = doc.rootVisualElement;
            for (int i = 0; i < 30 && !(ui.Q("minimap-image").worldBound.width > 0); i++) yield return null;
            Assert.IsTrue(ui.Q("minimap-image").worldBound.width > 0, "the panel was laid out");

            foreach (var s in ScreenNav.Screens)
                Assert.AreEqual(DisplayStyle.None, ui.Q("screen-" + s).resolvedStyle.display, s + " is hidden at start");
            HideTheOpeningCard(ui);
            yield return null;
            var panel = ui.panel;
            Assert.IsNull(panel.Pick(ui.worldBound.center), "the middle of the screen is map, not UI");

            // A screen point for a panel point: ScreenToPanel is affine, so two probes invert it.
            float h = Screen.height;
            Vector2 o = UiPointer.ToPanel(panel, Vector2.zero, h), ex = UiPointer.ToPanel(panel, Vector2.right * 100, h) - o, ey = UiPointer.ToPanel(panel, Vector2.up * 100, h) - o;
            Vector2 ScreenFor(Vector2 p) => new Vector2((p - o).x / ex.x * 100, (p - o).y / ey.y * 100);
            var pointer = Object.FindFirstObjectByType<MapPointer>();
            var camera = Camera.main.GetComponent<CameraController>();
            foreach (var name in new[] { "top-bar", "info-panel", "minimap" })
            {
                var at = ScreenFor(ui.Q(name).worldBound.center);
                Assert.IsTrue(pointer.IsOverUi!(at), name + " blocks map clicks");
                Assert.IsTrue(camera.IsOverUi!(at), name + " blocks the wheel and the drag");
            }
            var middle = ScreenFor(ui.worldBound.center);
            Assert.IsFalse(pointer.IsOverUi!(middle));
            Assert.IsFalse(camera.IsOverUi!(middle));

            UiTestKit.Press(ui.Q<Button>("nav-cabinet"));
            Assert.AreEqual(DisplayStyle.Flex, ui.Q("screen-cabinet").style.display.value, "the top-bar buttons open screens");

            var image = ui.Q("minimap-image");
            using (var e = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = image.worldBound.center }))
            { e.target = image; image.SendEvent(e); }
            var map = Object.FindFirstObjectByType<MapScene>();
            var rig = Camera.main.GetComponent<CameraRig>();
            var b = map.Hexes.bounds;
            var expected = map.Frame.ToWorld((b.lon0 + b.lon1) / 2, (b.lat0 + b.lat1) / 2, 0);
            Assert.AreEqual(expected.x, rig.LookAt.x, map.Frame.WidthM * 0.02f, "the camera moved to the clicked point");
            Assert.AreEqual(expected.z, rig.LookAt.z, map.Frame.LengthM * 0.02f);
            Assert.AreEqual(rig.LookAt - rig.transform.forward * rig.Distance, rig.transform.position, "the rig was applied");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene unloads unreferenced assets, which may already have destroyed these
            if (ps != null) Object.DestroyImmediate(ps);
            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        }
    }

    /// <summary>Fix round 1, M4: the real path. GameRoot.Begin creates TopBarInset before the panel's first layout (the
    /// inset starts at 0); the layout then sets it to the bar's share of the window, and a resize to a 21:9 window
    /// updates it. The camera's projection follows (the look-at point sits in the middle of the area below the bar).</summary>
    [UnityTest]
    public IEnumerator TheCameraFollowsTheTopBarFromTheFirstLayoutThroughAResize()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var rt = new RenderTexture(1920, 1080, 0);
        var ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
        try
        {
            doc.panelSettings = ps;
            Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
            var root = Object.FindFirstObjectByType<GameRoot>();
            root.Store = new SaveStore(saveDir);
            var rig = Camera.main.GetComponent<CameraRig>();
            root.Begin(Inputs, 0);
            var ui = doc.rootVisualElement;
            float Expected() => ui.Q("top-bar").worldBound.height / ui.panel.visualTree.worldBound.height;
            for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
            yield return null;
            Assert.Greater(rig.TopInset, 0f, "the first layout set the inset");
            Assert.AreEqual(Expected(), rig.TopInset, 1e-3f, "1920x1080: the bar's share of the window");
            var at1080 = rig.TopInset;
            var cam = Camera.main;
            Assert.AreEqual((1 - rig.TopInset) / 2, cam.WorldToViewportPoint(rig.LookAt).y, 1e-3f, "framed below the bar");

            var before = ui.panel.visualTree.worldBound;
            rt.Release(); rt.width = 2560; rt.Create();              // the window becomes 21:9 (same target, new size)
            for (int i = 0; i < 30 && ui.panel.visualTree.worldBound == before; i++) yield return null;
            yield return null;
            Assert.AreNotEqual(before, ui.panel.visualTree.worldBound, "the panel followed the new size");
            Assert.AreEqual(Expected(), rig.TopInset, 1e-3f, "2560x1080: the new share");
            Assert.Greater(Mathf.Abs(at1080 - rig.TopInset), 1e-3f, "the resize changed it (the bar is 96 of a shorter logical window)");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (ps != null) Object.DestroyImmediate(ps);
            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        }
    }

    // Carry 8: a real raycast on the built terrain picks the hex under the camera and fills the info panel.
    [Test]
    public void AClickOnTheTerrainFillsTheInfoPanel()
    {
        Boot();
        HideTheOpeningCard(Ui);
        var pointer = Object.FindFirstObjectByType<MapPointer>();
        Assert.IsTrue(pointer.HandleClick(AimAtTelAviv()), "the ray hit the terrain");
        Assert.AreEqual("Tel Aviv", Ui.Q<Label>("info-title").text);
        // Playtest 1, item 10: the map marks the hex the info panel describes.
        var map = Object.FindFirstObjectByType<MapScene>();
        var sel = map.Terrain.transform.Find(MapScene.SelectionName);
        Assert.IsNotNull(sel, "the clicked hex is outlined");
        Assert.IsTrue(sel.GetComponent<MeshRenderer>().enabled);
        var tlv = Inputs.Content.SettlementHexes.Single(s => s.Id == "tel_aviv");
        var hex = Inputs.Content.HexAt(tlv.Q, tlv.R)!;
        var ground = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        var b = sel.GetComponent<MeshFilter>().sharedMesh.bounds;
        Assert.AreEqual(ground.x, b.center.x, 100f, "the outline is on the clicked hex");
        Assert.AreEqual(ground.z, b.center.z, 100f);
    }

    /// <summary>Since the GDD cutover the game opens on its first card, One Army (idf_founding, window from 15 May 1948 per
    /// #567: the order it asks about was signed on 26 May, before the next monthly turn), and its photograph covers the map. A
    /// player hides it to use the map; so do these tests.</summary>
    public static void HideTheOpeningCard(VisualElement ui)
    {
        var hide = ui.Q<Button>("event-hide");
        Assert.IsNotNull(hide, "the opening card has its Hide button");
        UiTestKit.Press(hide);
    }

    /// <summary>Points the main camera straight down at Tel Aviv and returns Tel Aviv's screen position.</summary>
    static Vector2 AimAtTelAviv()
    {
        var map = Object.FindFirstObjectByType<MapScene>();
        var c = Inputs.Content;
        var tlv = c.SettlementHexes.Single(s => s.Id == "tel_aviv");
        var hex = c.HexAt(tlv.Q, tlv.R)!;
        var ground = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        var cam = Camera.main!;
        cam.transform.position = new Vector3(ground.x, 50_000f, ground.z);
        cam.transform.rotation = Quaternion.Euler(90, 0, 0);
        Ui.panel?.Pick(Vector2.zero);   // settle the layout first: the top bar's height sets the camera's inset (TopBarInset)
        var p = cam.WorldToScreenPoint(new Vector3(ground.x, map.Terrain.SampleHeight(ground), ground.z));
        return new Vector2(p.x, p.y);   // projected, not the screen centre: the frame below the top bar is off-centre (TopBarInset)
    }

    // One label each presenter writes on every change: HUD, toast, info panel, Why, Budget, Defence, Cabinet, event card
    // (the card writes its title while an event is open, so the test opens one first).
    static readonly string[] Watched = { "year", "toast", "info-title", "why-turn", "budget-money", "mob-level", "cabinet-coalition", "event-title" };

    // Carry 1, 3-7, 9: disabling GameRoot disposes every presenter, the autosave and its own hooks.
    [Test]
    public void ADisabledGameRootHearsNothing()
    {
        var root = Boot();
        var bridge = root.Bridge!;
        var c = Inputs.Content;
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        bridge.Replace(g);
        Assert.AreEqual(DisplayStyle.Flex, Ui.Q("event-card").style.display.value, "an event is open before the disable");
        var pointer = Object.FindFirstObjectByType<MapPointer>();
        var camera = Camera.main.GetComponent<CameraController>();
        var image = Ui.Q("minimap-image");
        var texture = image.style.backgroundImage.value.texture;
        Assert.IsTrue(texture != null, "the minimap has its texture");
        root.enabled = false;
        Assert.IsNull(pointer.IsOverUi, "the pointer no longer asks a disposed panel");
        Assert.IsNull(camera.IsOverUi);
        Assert.IsNull(pointer.Map, "the pointer no longer picks on this map");
        Assert.AreEqual(float.MaxValue, camera.Bounds.width, "the camera bounds are restored");
        Assert.IsTrue(texture != null, "the minimap's picture is the shared baked asset: let go of, not destroyed");
        Assert.AreEqual(StyleKeyword.Null, image.style.backgroundImage.keyword, "nothing draws the minimap's picture");
        foreach (var label in Watched)
            Ui.Q<Label>(label).text = "untouched";
        var turn = bridge.State.Turn;
        UiTestKit.Press(Ui.Q<Button>("end-turn"));
        Assert.AreEqual(turn, bridge.State.Turn, "End Turn is no longer wired (HudPresenter)");
        UiTestKit.Press(Ui.Q<Button>("nav-budget"));
        Assert.AreNotEqual(DisplayStyle.Flex, Ui.Q("screen-budget").style.display.value, "ScreenNav");
        var session = bridge.Session;
        UiTestKit.Press(Ui.Q<Button>("new-game")); UiTestKit.Press(Ui.Q<Button>("new-game"));
        Assert.AreSame(session, bridge.Session, "NewGamePresenter");
        Assert.AreEqual(DisplayStyle.None, Ui.Q("event-card").style.display.value, "EventCardPresenter hid its card");
        Assert.IsFalse(pointer.HandleClick(AimAtTelAviv()), "a map click no longer reaches the info panel");
        bridge.Queue(new SetMobilization(0.1));
        bridge.EndTurn();
        foreach (var label in Watched)
            Assert.AreEqual("untouched", Ui.Q<Label>(label).text, label);
        Assert.IsFalse(File.Exists(Path.Combine(saveDir, SaveStore.AutosaveName)), "the autosave was disposed");
    }

    // Carry 9: the start goes through GameStart.Begin (an unreadable autosave is set aside, with a note in the toast);
    // each turn is saved; the game continues from that save on the next start.
    [Test]
    public void TheStartNotesShowInTheToastAndEachTurnIsSaved()
    {
        Directory.CreateDirectory(saveDir);
        File.WriteAllText(Path.Combine(saveDir, SaveStore.AutosaveName), "not a save");
        var root = Boot();
        StringAssert.StartsWith("Your saved game could not be loaded", Ui.Q<Label>("toast").text);
        Assert.IsTrue(File.Exists(Path.Combine(saveDir, "autosave.bad.txt")));
        root.Bridge!.EndTurn();
        Assert.IsTrue(File.Exists(Path.Combine(saveDir, SaveStore.AutosaveName)), "the turn was autosaved");
        var hash = root.Bridge.State.Hash();
        root.enabled = false;
        root.enabled = true;
        root.Begin(Inputs, 0);
        Assert.AreEqual(hash, root.Bridge!.State.Hash(), "the next start continues the autosave");
    }

    // Carry 11 and review M4: the autosave subscribes after the toast and appends, so a save failure is shown next to
    // the turn's refusals, not cleared by them and not replacing them.
    [Test]
    public void ASaveFailureIsShownAlongsideTheTurnsRefusals()
    {
        File.WriteAllText(saveDir, "a file where the save folder should be");
        var root = Boot(new SaveStore(saveDir));
        var tlv = Inputs.Content.SettlementHexes.Single(s => s.Id == "tel_aviv");
        Assert.IsNull(root.Bridge!.Queue(new MapAction(MapActions.TransitCamp, tlv.Q, tlv.R)), "queued; the sim refuses it in 1948");
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("^Autosave could not be written"));
        root.Bridge.EndTurn();
        var lines = Ui.Q<Label>("toast").text.Split('\n');
        Assert.AreEqual(2, lines.Length, Ui.Q<Label>("toast").text);
        StringAssert.StartsWith("Not done: ", lines[0]);
        Assert.AreEqual(Text["save.write_failed"], lines[1]);
        Assert.AreEqual(DisplayStyle.Flex, Ui.Q("toast").style.display.value);
    }

    [Test]
    public void AppendAddsALineOrShowsItAlone()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(Inputs.Content, GoldenRun.Seed, Mode.Historical), Text);
        using var toast = new ToastPresenter(ui.Root, bridge, Text);
        toast.Append("one");
        Assert.AreEqual("one", ui.Q<Label>("toast").text);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("toast").style.display.value);
        toast.Append("two");
        Assert.AreEqual("one\ntwo", ui.Q<Label>("toast").text);
        toast.Clear();
        toast.Append("three");
        Assert.AreEqual("three", ui.Q<Label>("toast").text);
    }

    // Review I1: a smoke run never touches the player's save, not even through New Game.
    [Test]
    public void ASmokeBootNeverTouchesThePlayersSave()
    {
        var player = new SimBridge(GameSession.New(Inputs.Content, 9, Mode.Historical), Text);
        player.EndTurn();
        Directory.CreateDirectory(saveDir);
        var path = Path.Combine(saveDir, SaveStore.AutosaveName);
        var saved = player.Session.Save.Write();
        File.WriteAllText(path, saved);
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        LogAssert.Expect(LogType.Warning, "SMOKE FAIL the smoke run needs play mode or a player");
        root.Begin(Inputs, 5);
        Assert.AreEqual(0, root.Bridge!.State.Turn, "a fresh game, not the player's");
        var session = root.Bridge.Session;
        UiTestKit.Press(Ui.Q<Button>("new-game")); UiTestKit.Press(Ui.Q<Button>("new-game"));
        Assert.AreSame(session, root.Bridge.Session, "New Game does nothing in a smoke run");
        root.Bridge.EndTurn();
        Assert.AreEqual(saved, File.ReadAllText(path), "the player's autosave is untouched");
        Assert.AreEqual(new[] { path }, Directory.GetFiles(saveDir));
    }

    // Review M6: Configure on an enabled GameRoot moves the Ready subscription to the new boot.
    [Test]
    public void ConfigureMovesTheReadySubscription()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var root = Object.FindFirstObjectByType<GameRoot>();
        var old = Object.FindFirstObjectByType<GameBoot>();
        var other = new GameObject("OtherBoot").AddComponent<GameBoot>();
        static int Listeners(GameBoot b) =>
            (typeof(GameBoot).GetField("Ready", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(b) as Delegate)?.GetInvocationList().Length ?? 0;
        Assert.AreEqual(1, Listeners(old), "subscribed while enabled");
        var so = new SerializedObject(root);
        Object R(string f) => so.FindProperty(f).objectReferenceValue;
        root.Configure(other, (UIDocument)R("document"), (TextAsset)R("strings"), (TextAsset)R("stringsHe"), (PortraitCatalog)R("portraits"),
            (MapScene)R("map"), (PlacementLayer)R("placements"), (MapPointer)R("pointer"), (CameraController)R("cameraController"));
        Assert.AreEqual(0, Listeners(old));
        Assert.AreEqual(1, Listeners(other));
        root.enabled = false;
        Assert.AreEqual(0, Listeners(other));
    }

    // Carry 10: placements are drawn at start and redrawn only when State.Placements changes.
    [Test]
    public void PlacementsAreRedrawnOnlyWhenTheyChange()
    {
        var root = Boot();
        var c = Inputs.Content;
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new DateTime(1950, 8, 1)) g.Step(HistoricalAI.Commands(c, g.State));
        var bridge = root.Bridge!;
        bridge.Replace(g);
        var layer = Object.FindFirstObjectByType<PlacementLayer>().transform;
        Assert.AreEqual(g.State.Placements.Count, layer.childCount);
        var tlv = c.SettlementHexes.Single(s => s.Id == "tel_aviv");
        Assert.IsNull(bridge.Queue(new MapAction(MapActions.TransitCamp, tlv.Q, tlv.R)));
        Assert.AreEqual(g.State.Placements.Count, layer.childCount, "a queued order draws nothing");
        bridge.EndTurn();
        Assert.AreEqual(bridge.State.Placements.Count, layer.childCount);
        var camp = layer.Cast<Transform>().Single(t => t.name == $"{MapActions.TransitCamp}_{tlv.Q}_{tlv.R}").gameObject;
        bridge.Queue(new SetMobilization(bridge.State.Mobilization));
        bridge.EndTurn();
        Assert.IsTrue(camp != null, "an unchanged set of placements is not redrawn");
        Assert.IsNull(bridge.Queue(new MapAction(MapActions.Housing, tlv.Q, tlv.R)));
        bridge.EndTurn();
        Assert.IsTrue(camp == null, "housing replacing the camp is redrawn");
        Assert.IsNotNull(layer.Cast<Transform>().SingleOrDefault(t => t.name == $"{MapActions.Housing}_{tlv.Q}_{tlv.R}"));
    }

    // Amendment A2: the smoke run answers a pending event by pressing the card's option button.
    [Test]
    public void TheSmokeRunAnswersAPendingEventThroughTheCard()
    {
        using var ui = new UiTestKit();
        var c = Inputs.Content;
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        var text = Text;
        var bridge = new SimBridge(g, text);
        using var toast = new ToastPresenter(ui.Root, bridge, text);
        using var hud = new HudPresenter(ui.Root, bridge, text);          // wires the End Turn button
        using var card = new EventCardPresenter(ui.Root, bridge, text, null);
        var turn = g.State.Turn;
        var log = new List<string>();
        var run = GameRoot.Smoke(ui.Root, bridge, card, 1, log.Add);
        while (run.MoveNext()) { }
        StringAssert.StartsWith("SMOKE OK 1 turns", log[log.Count - 1], string.Join("\n", log));
        StringAssert.StartsWith($"SMOKE turn {turn + 1} ", log[log.Count - 2]);
        Assert.AreEqual(turn + 1, bridge.State.Turn);
        Assert.IsTrue(bridge.Session.Last!.Why.Any(w => w.Message == "Chosen: Sign the armistice"), "the card's option was pressed, not delegated");
        Assert.IsFalse(bridge.Session.Last.Why.Any(w => w.Target == "armistice_egypt" && w.Message.StartsWith("Delegated")));
    }

    /// <summary>Since the GDD cutover, 15 July 1948 brings three cards at full war, where an answer costs 2 of the 4
    /// Attention: the smoke answers the two it can pay for and leaves the third to its minister, and the run still passes.</summary>
    [Test]
    public void TheSmokeRunLeavesACardItCannotPayForToItsMinister()
    {
        using var ui = new UiTestKit();
        var c = Inputs.Content;
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new System.DateTime(1948, 7, 15)) g.Step(new Command[0]);
        Assert.AreEqual(3, g.State.PendingEvents.Count, string.Join(", ", g.State.PendingEvents));
        var text = Text;
        var bridge = new SimBridge(g, text);
        using var toast = new ToastPresenter(ui.Root, bridge, text);
        using var hud = new HudPresenter(ui.Root, bridge, text);
        using var card = new EventCardPresenter(ui.Root, bridge, text, null);
        var log = new List<string>();
        var run = GameRoot.Smoke(ui.Root, bridge, card, 1, log.Add);
        while (run.MoveNext()) { }
        StringAssert.StartsWith("SMOKE OK 1 turns 1948-08-15", log[log.Count - 1], string.Join("\n", log));
        Assert.AreEqual(2, bridge.Session.Last!.Why.Count(w => w.System == "events" && w.Message.StartsWith("Chosen")));
        Assert.AreEqual(1, bridge.Session.Last.Why.Count(w => w.System == "events" && w.Message.StartsWith("Delegated")));
    }

    [Test]
    public void ASmokeRunWhoseEndTurnDoesNothingFails()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(Inputs.Content, GoldenRun.Seed, Mode.Historical), Text);
        using var card = new EventCardPresenter(ui.Root, bridge, Text, null);   // no HudPresenter: End Turn is not wired
        var log = new List<string>();
        var run = GameRoot.Smoke(ui.Root, bridge, card, 2, log.Add);
        while (run.MoveNext()) { }
        StringAssert.StartsWith("SMOKE FAIL", log[log.Count - 1]);
        Assert.IsFalse(log.Any(l => l.StartsWith("SMOKE OK")));
    }

    // Amendment A3 and review M5: "smoke" must be a whole query parameter (bare, or =N); =0 means off; a bare flag
    // (URL or command line) runs DefaultSmokeTurns.
    [Test]
    public void SmokeTurnsComeFromTheUrlOrTheCommandLine()
    {
        var none = new string[0];
        int Web(string url) => GameRoot.SmokeTurns(url, none, true);
        int Desk(params string[] args) => GameRoot.SmokeTurns(null, args, false);
        Assert.AreEqual(5, Web("http://localhost:8000/?golden&smoke=5"));
        Assert.AreEqual(3, Web("http://localhost:8000/?smoke=3"));
        Assert.AreEqual(3, Web("http://localhost:8000/?smoke=3&golden"));
        Assert.AreEqual(3, Web("http://localhost:8000/?smoke=3#top"));
        Assert.AreEqual(GameRoot.DefaultSmokeTurns, Web("http://localhost:8000/?smoke"));
        Assert.AreEqual(GameRoot.DefaultSmokeTurns, Web("http://localhost:8000/?smoke&golden"));
        Assert.AreEqual(GameRoot.DefaultSmokeTurns, Web("http://localhost:8000/?golden&smoke="));
        Assert.AreEqual(0, Web("http://localhost:8000/?smoke=0"), "explicitly off");
        Assert.AreEqual(0, Web("http://localhost:8000/smoke-test/index.html"));
        Assert.AreEqual(0, Web("http://localhost:8000/?smoker=4"));
        Assert.AreEqual(0, Web("http://localhost:8000/?smoke-x"));
        Assert.AreEqual(0, Web("http://localhost:8000/?smoke=five"));
        Assert.AreEqual(0, Web("http://localhost:8000/?nosmoke=5"));
        Assert.AreEqual(0, Web("http://localhost:8000/"));
        Assert.AreEqual(0, Web(null!));
        Assert.AreEqual(4, Desk("Statecraft", "-smoke", "4"));
        Assert.AreEqual(0, Desk("Statecraft", "-smoke", "0"), "explicitly off");
        Assert.AreEqual(GameRoot.DefaultSmokeTurns, Desk("Statecraft", "-smoke"));
        Assert.AreEqual(GameRoot.DefaultSmokeTurns, Desk("Statecraft", "-smoke", "-golden"));
        Assert.AreEqual(0, Desk("Statecraft", "-smoke", "99999999999"), "unparseable (too big) is off, as on the web");
        Assert.AreEqual(0, Desk("Statecraft", "-smoke", "-1"), "negative is off");
        Assert.AreEqual(0, Desk("Statecraft", "-smoke", "five"), "not a number is off, as ?smoke=five");
        Assert.AreEqual(0, Web("http://localhost:8000/?smoke=99999999999"), "the web agrees");
        Assert.AreEqual(0, Desk("Statecraft"));
        Assert.AreEqual(0, GameRoot.SmokeTurns("http://localhost/?smoke=5", new[] { "Statecraft" }, false), "desktop reads only the command line");
    }

    // Task 24 (persistence gate, zoom check): the start and every turn are logged with the state hash, and ?camlog logs the
    // camera distance, so a browser run can be checked from the console alone.
    [Test]
    public void TheStartAndEachTurnAreLoggedWithTheStateHash()
    {
        var lines = new List<string>();
        Application.LogCallback capture = (m, _, __) => lines.Add(m);
        Application.logMessageReceived += capture;
        try
        {
            var root = Boot();
            var start = root.Bridge!.State;
            root.Bridge.EndTurn();
            var s = root.Bridge.State;
            CollectionAssert.Contains(lines, FormattableString.Invariant($"GAME OK {start.Date:yyyy-MM-dd} turn {start.Turn} hash {start.Hash()}"));
            CollectionAssert.Contains(lines, FormattableString.Invariant($"TURN {s.Turn} {s.Date:yyyy-MM-dd} hash {s.Hash()}"));
            root.enabled = false;
            lines.Clear();
            root.Bridge.EndTurn();
            Assert.IsFalse(lines.Any(l => l.StartsWith("TURN ")), "a disabled GameRoot logs nothing");
        }
        finally { Application.logMessageReceived -= capture; }
    }

    [Test]
    public void TheCameraLogIsAWholeQueryParameter()
    {
        Assert.IsTrue(GameRoot.CameraLogRequested("http://localhost/?camlog", new string[0], true));
        Assert.IsTrue(GameRoot.CameraLogRequested("http://localhost/?golden&camlog", new string[0], true));
        Assert.IsFalse(GameRoot.CameraLogRequested("http://localhost/?camlogs", new string[0], true));
        Assert.IsFalse(GameRoot.CameraLogRequested("http://localhost/camlog/", new string[0], true));
        Assert.IsFalse(GameRoot.CameraLogRequested("http://localhost/?camlog-x", new string[0], true));
        Assert.IsTrue(GameRoot.CameraLogRequested(null, new[] { "x", "-camlog" }, false));
        Assert.IsFalse(GameRoot.CameraLogRequested(null, new[] { "x" }, false));
        Assert.AreEqual("CAMERA distance=120000 yaw=0.0", GameRoot.CameraLine(120000f, 0f));
    }

    /// <summary>Task 24 review M1: a profile cut short (GameRoot disabled or restarted while ?profile runs; a stopped
    /// coroutine never reaches its restore line) does not leave the terrain at the profile's pixel error.</summary>
    [Test]
    public void ShuttingDownRestoresTheTerrainPixelError()
    {
        var root = Boot();
        var terrain = Object.FindFirstObjectByType<MapScene>().Terrain;
        terrain.heightmapPixelError = 5f;                               // where an interrupted profile leaves it
        root.enabled = false;
        Assert.AreEqual(MapBuilder.TerrainPixelError, terrain.heightmapPixelError);
    }

    /// <summary>Playtest 2, P2-1: once GameRoot has started the game, the loading state is gone.</summary>
    [Test]
    public void TheGameLeavesTheLoadingState()
    {
        Boot();
        Assert.IsFalse(Ui.Q("root").ClassListContains("loading"), "GameRoot clears the loading state");
    }

    /// <summary>Final review M1: once ready, the root is transparent again, so the map shows.</summary>
    [UnityTest]
    public IEnumerator TheLoadingBackgroundGoesWhenTheGameIsReady()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var rt = new RenderTexture(1280, 720, 0);
        var ps = Object.Instantiate(doc.panelSettings); ps.targetTexture = rt;
        try
        {
            doc.panelSettings = ps;
            Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
            var root = Object.FindFirstObjectByType<GameRoot>();
            root.Store = new SaveStore(saveDir);
            root.Begin(Inputs, 0);
            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(0f, doc.rootVisualElement.Q("root").resolvedStyle.backgroundColor.a, 1e-3f);
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (ps != null) Object.DestroyImmediate(ps);
            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        }
    }

    /// <summary>Final review I5: a boot failure (a failed StreamingAssets fetch, a content error) is shown on the loading
    /// line, naming the reason, instead of "Loading the map of 1948..." forever; no panel appears.</summary>
    [UnityTest]
    public IEnumerator ABootFailureIsShownOnTheLoadingLine()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var rt = new RenderTexture(1280, 720, 0);
        var ps = Object.Instantiate(doc.panelSettings); ps.targetTexture = rt;
        try
        {
            doc.panelSettings = ps;
            var boot = Object.FindFirstObjectByType<GameBoot>();
            LogAssert.Expect(LogType.Error, "BOOT FAIL people.yaml: people[x].name: required");
            boot.Fail("people.yaml: people[x].name: required");
            for (int i = 0; i < 5; i++) yield return null;
            var ui = doc.rootVisualElement;
            // #637: the player reads a short localised reason; the content error stays in Error and the log.
            Assert.AreEqual("Statecraft could not start: its files failed to load. Reload the page.", ui.Q<Label>("loading").text);
            Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("loading").resolvedStyle.display);
            Assert.IsTrue(ui.Q("root").ClassListContains("loading"), "the panels stay hidden");
            Assert.AreEqual(Visibility.Hidden, ui.Q("info-panel").resolvedStyle.visibility);
            Assert.AreEqual("people.yaml: people[x].name: required", boot.Error);
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (ps != null) Object.DestroyImmediate(ps);
            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        }
    }

    /// <summary>Final review M4: every settlement that owns a hex has a display name (the info panel looks names up strictly).</summary>
    [Test]
    public void EverySettlementHexHasAName()
    {
        var names = SettlementPlacer.Parse(Inputs.Map.SettlementsYaml).Select(s => s.Id).ToHashSet();
        foreach (var sh in Inputs.Content.SettlementHexes) Assert.IsTrue(names.Contains(sh.Id), sh.Id);
    }

    /// <summary>Rereview: the real failure path. GameBoot's boot (what Start runs) catches a failing load, and the player
    /// sees the reason with no absolute local path (desktop players read files from disk), only the file name.</summary>
    [UnityTest]
    public IEnumerator AFailingLoadIsCaughtAndShownWithoutLocalPaths()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var rt = new RenderTexture(1280, 720, 0);
        var ps = Object.Instantiate(doc.panelSettings); ps.targetTexture = rt;
        try
        {
            doc.panelSettings = ps;
            var boot = Object.FindFirstObjectByType<GameBoot>();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("BOOT FAIL Could not find file"));
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("Could not find file"));
            _ = boot.BootAsync(() => throw new IOException("Could not find file '/Users/someone/Statecraft Israel.app/Contents/Resources/Data/StreamingAssets/map/heightmap.json'."));
            for (int i = 0; i < 3; i++) yield return null;
            var line = doc.rootVisualElement.Q<Label>("loading").text;
            Assert.AreEqual("Statecraft could not start: its files failed to load. Reload the page.", line);   // #637: no exception text
            StringAssert.DoesNotContain("/Users/", line);
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (ps != null) Object.DestroyImmediate(ps);
            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        }
    }
}
