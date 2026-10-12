using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Statecraft.Game.Prototype
{
    /// <summary>EXPERIMENTAL PLAYTEST. Direct-control toy mission; never reads or mutates the campaign simulation.
    /// Attach only after map/UI creation. Dispose restores UI visibility, camera and input components.</summary>
    public sealed class CommandPrototype : MonoBehaviour, IDisposable
    {
        static readonly Color Ink = new Color(.075f, .105f, .105f), Paper = new Color(.92f, .87f, .72f),
            Brass = new Color(.83f, .66f, .33f), Mint = new Color(.37f, .91f, .72f), Red = new Color(.94f, .36f, .27f);
        public CommandModel Model { get; private set; }
        readonly Dictionary<int, UnitView> views = new Dictionary<int, UnitView>();
        readonly HashSet<int> selected = new HashSet<int>();
        readonly List<(VisualElement element, StyleEnum<DisplayStyle> display)> hidden = new List<(VisualElement, StyleEnum<DisplayStyle>)>();
        readonly List<Object> assets = new List<Object>();
        readonly List<(GameObject obj, bool active)> hiddenMapLayers = new List<(GameObject, bool)>();
        readonly List<FloatText> floats = new List<FloatText>();
        readonly List<(CommandPoint point, Label label)> sites = new List<(CommandPoint, Label)>();
        MapScene map; CameraRig rig; CameraController controller; MapPointer pointer; Camera cam;
        Rect cameraBounds; Func<Vector2, float> ground; float groundTop;
        VisualElement root, hud, top, orders, objectivePanel, briefing, endPanel, mini, dragBox, worldLabels;
        Label resources, clock, objective, selection, report, status, result;
        Button pauseButton, soundButton;
        GameObject sceneRoot; Material olive, cream, black, orange, friendly, danger, lineMaterial;
        Mesh cubeMesh, sphereMesh;
        AudioSource audioSource; AudioClip clickSound, noticeSound;
        bool oldControllerEnabled, oldPointerEnabled, disposed, attached, briefingOpen, sound = true, dragging;
        Vector3 oldLook; float oldDistance, oldYaw, oldInset;
        Vector2 downAt, lastMouse;
        LineRenderer orderRing;
        float ringUntil, refreshAt;
        int lastReport = -1;
        CommandOutcome shownOutcome;
        CommandGallery gallery; IDisposable pointerBlur, navigationGuard;
        sealed class UnitView { public GameObject Body; public LineRenderer Ring, Path; public Label Label; public VisualElement Dot; public float LastHealth; public CommandKind Kind; public CommandArtMotion Motion; }
        sealed class FloatText { public Label Label; public Vector3 At; public float Until; }

        public static bool Requested() => Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);
        public static bool Requested(string url, string[] args, bool webgl)
        {
            if (!webgl) { int i = Array.IndexOf(args ?? Array.Empty<string>(), "-prototype"); return i >= 0 && i + 1 < args.Length && args[i + 1] == "command"; }
            var clean = (url ?? "").Split('#')[0];
            int query = clean.IndexOf('?');
            if (query < 0) return false;
            return clean.Substring(query + 1).Split('&').Any(parameter => parameter == "prototype=command");
        }
        public static CommandPrototype Attach(GameObject host, VisualElement root, MapScene map, CameraRig rig,
            CameraController cameraController, MapPointer pointer)
        {
            var p = host.AddComponent<CommandPrototype>();
            p.Init(root, map, rig, cameraController, pointer); return p;
        }
        void Init(VisualElement uiRoot, MapScene scene, CameraRig cameraRig, CameraController cameraController, MapPointer mapPointer)
        {
            root = uiRoot; map = scene; rig = cameraRig; controller = cameraController; pointer = mapPointer; cam = rig.GetComponent<Camera>();
            oldControllerEnabled = controller.enabled; oldPointerEnabled = pointer.enabled;
            oldLook = rig.LookAt; oldDistance = rig.Distance; oldYaw = rig.Yaw; oldInset = rig.TopInset;
            controller.enabled = false; pointer.enabled = false;
            var terrain = map.Terrain;
            var terrainOrigin = terrain.transform.position; var terrainSize = terrain.terrainData.size;
            cameraBounds = new Rect(terrainOrigin.x, terrainOrigin.z, terrainSize.x, terrainSize.z);
            ground = CameraController.GroundOf(terrain); groundTop = CameraController.GroundTopOf(terrain);
            // An in-session switch can leave the existing campaign token layer on the map. Restore its exact active state.
            foreach (string name in new[] { Statecraft.Game.Map.Units.UnitLayer.RootName,
                Statecraft.Game.Map.Units.UnitShowcase.RootName, MapScene.SelectionName }) {
                var layer = terrain.transform.Find(name);
                if (layer != null) { hiddenMapLayers.Add((layer.gameObject, layer.gameObject.activeSelf)); layer.gameObject.SetActive(false); }
            }
            foreach (var child in root.Children().ToArray()) { hidden.Add((child, child.style.display)); child.style.display = DisplayStyle.None; }
            sceneRoot = new GameObject("CommandPrototype_LocalPlaytest"); sceneRoot.transform.SetParent(map.transform, false);
            olive = Material(new Color(.29f, .38f, .22f)); cream = Material(new Color(.86f, .79f, .57f));
            black = Material(new Color(.07f, .085f, .07f)); orange = Material(new Color(.96f, .5f, .10f));
            friendly = Material(Mint, true); danger = Material(Red, true); lineMaterial = Material(Brass, true);
            audioSource = sceneRoot.AddComponent<AudioSource>(); audioSource.spatialBlend = 0; audioSource.volume = .15f;
            clickSound = Tone("Command acknowledge", 660, .075f); noticeSound = Tone("Command radio", 440, .20f);
            MakeHud(); MakeSites(); attached = true; Restart();
            Debug.Log("COMMAND READY: isolated experimental prototype; paused briefing; no campaign writes");
        }
        public void Restart()
        {
            gallery?.Dispose(); gallery = null;
            if (Model != null) Model.Feedback -= OnFeedback;
            foreach (var view in views.Values) { DestroyOwned(view.Body); view.Label.RemoveFromHierarchy(); view.Dot.RemoveFromHierarchy(); }
            views.Clear(); selected.Clear(); foreach (var f in floats) f.Label.RemoveFromHierarchy(); floats.Clear();
            Model = new CommandModel { Paused = true }; Model.Feedback += OnFeedback;
            gallery = new CommandGallery(hud, Model);
            if (orderRing != null) orderRing.gameObject.SetActive(false);
            selected.Add(0); shownOutcome = CommandOutcome.Playing; lastReport = -1;
            endPanel.style.display = DisplayStyle.None; briefing.style.display = DisplayStyle.Flex; briefingOpen = true;
            Home(); RefreshViews(); RefreshHud();
        }
        Material Material(Color color, bool unlit = false)
        {
            var material = unlit ? new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit")) : new Material(map.MarkerMaterial);
            material.name = "Command procedural material";
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .15f);
            assets.Add(material); return material;
        }
        AudioClip Tone(string name, float hz, float seconds)
        {
            const int rate = 22050; int count = (int)(seconds * rate); var samples = new float[count];
            for (int i = 0; i < count; i++) { float t = (float)i / rate; samples[i] = Mathf.Sin(t * hz * Mathf.PI * 2) * Mathf.Sin(Mathf.PI * i / count) * .45f; }
            var clip = AudioClip.Create(name, count, 1, rate, false); clip.SetData(samples, 0); assets.Add(clip); return clip;
        }
        void Beep(bool notice = false) { if (sound && audioSource != null) audioSource.PlayOneShot(notice ? noticeSound : clickSound); }
        Vector3 World(CommandPoint p)
        {
            var at = map.Frame.ToWorld(34.68 + p.X * .0033, 31.32 + p.Y * .0075, 0);
            at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y + 55; return at;
        }
        CommandPoint Point(Vector3 p) { var geo = map.Frame.ToLonLat(p); return new CommandPoint((float)((geo.lon - 34.68) / .0033), (float)((geo.lat - 31.32) / .0075)); }
        void Home() { rig.LookAt = World(new CommandPoint(46, 53)); rig.Distance = 135000; rig.Yaw = 0; rig.TopInset = .10f; rig.Apply(); }
        void MakeHud()
        {
            hud = new VisualElement { name = "command-prototype", pickingMode = PickingMode.Ignore };
            hud.style.position = Position.Absolute; hud.style.left = hud.style.right = hud.style.top = hud.style.bottom = 0;
            hud.style.color = Paper; hud.style.fontSize = 13; root.Add(hud);
            pointerBlur = new PointerBlur(hud); navigationGuard = new NavigationGuard(hud);
            hud.RegisterCallback<NavigationSubmitEvent>(ReserveSpace, TrickleDown.TrickleDown);
            worldLabels = new VisualElement { pickingMode = PickingMode.Ignore }; Stretch(worldLabels); hud.Add(worldLabels);
            top = Panel(); top.style.left = 14; top.style.right = 14; top.style.top = 12; top.style.height = 72;
            top.style.flexDirection = FlexDirection.Row; top.style.alignItems = Align.Center; top.style.justifyContent = Justify.SpaceBetween;
            var titleBox = new VisualElement(); titleBox.Add(Text("COMMAND  /  SOUTHERN ROUTE", 17, Brass));
            titleBox.Add(Text("EXPERIMENTAL PLAYTEST · COASTAL PLAIN & NEGEV · 1948", 10, Paper)); top.Add(titleBox);
            resources = Text("", 15, Paper); top.Add(resources); clock = Text("", 15, Brass); top.Add(clock);
            pauseButton = Button("SPACE · Begin", TogglePause); top.Add(pauseButton);
            soundButton = Button("♪ On", () => { sound = !sound; soundButton.text = sound ? "♪ On" : "♪ Off"; }); top.Add(soundButton);
            hud.Add(top);
            objectivePanel = Panel(); objectivePanel.style.left = 14; objectivePanel.style.top = 98; objectivePanel.style.width = 270;
            objectivePanel.Add(Text("FIELD ORDERS", 12, Brass)); objective = Text("", 13, Paper); objective.style.whiteSpace = WhiteSpace.Normal; objectivePanel.Add(objective);
            var counters = Text("BRIGADE → patrols\nSCOUT → rescue + speed\nCONVOY → deliveries; needs escort", 11, Mint); counters.style.marginTop = 10; objectivePanel.Add(counters);
            objectivePanel.Add(Button("F · Frame mission", Home)); hud.Add(objectivePanel);
            mini = Panel(); mini.name = "command-minimap"; mini.style.right = 14; mini.style.top = 98; mini.style.width = 174; mini.style.height = 174;
            mini.style.paddingLeft = mini.style.paddingRight = mini.style.paddingTop = mini.style.paddingBottom = 0;
            mini.Add(Text("  SECTOR MAP", 10, Brass));
            mini.RegisterCallback<PointerDownEvent>(evt => {
                if (evt.button != 0) return;
                var local = mini.WorldToLocal(evt.position);
                // Map picture coordinates onto the mission board; these are positions, not displayed percentages.
                var p = new CommandPoint(Mathf.Lerp(0, 100, local.x / mini.resolvedStyle.width),
                    Mathf.Lerp(100, 0, local.y / mini.resolvedStyle.height));
                rig.LookAt = World(p); rig.Apply(); evt.StopPropagation();
            }); hud.Add(mini);
            orders = Panel(); orders.style.left = 14; orders.style.right = 14; orders.style.bottom = 12; orders.style.minHeight = 115;
            selection = Text("", 15, Brass); orders.Add(selection);
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap;
            row.Add(Button("G · Harvest", Harvest)); row.Add(Button("C · Camp  $35k / 20", () => Build(CommandKind.Camp)));
            row.Add(Button("B · Settlement  $80k / 50", () => Build(CommandKind.Settlement)));
            row.Add(Button("R · Brigade  $55k / 25", () => { Model.Recruit(); Beep(); }));
            row.Add(Button("H · Depot", () => OrderSelection(CommandModel.Base))); row.Add(Button("O · Outpost", () => OrderSelection(CommandModel.Outpost)));
            row.Add(Button("Models · 34", () => gallery.Open())); row.Add(Button("? · Briefing", ShowBriefing)); row.Add(Button("Restart", Restart)); orders.Add(row);
            orders.Add(Text("Click / drag: select  ·  Shift: add  ·  Right-click: move / attack  ·  1 brigade  2 scout  3 convoy  4 workers  ·  WASD / arrows: pan  ·  wheel: zoom", 10, Paper));
            hud.Add(orders);
            report = Text("", 13, Paper); report.style.position = Position.Absolute; report.style.left = 24; report.style.right = 24; report.style.bottom = 150;
            report.style.unityTextAlign = TextAnchor.MiddleCenter; report.style.backgroundColor = new Color(.06f,.09f,.09f,.9f); report.style.paddingTop = report.style.paddingBottom = 7;
            report.style.whiteSpace = WhiteSpace.Normal; hud.Add(report);
            status = Text("", 15, Brass); status.style.position = Position.Absolute; status.style.left = 0; status.style.right = 0; status.style.top = 90; status.style.unityTextAlign = TextAnchor.MiddleCenter; status.pickingMode = PickingMode.Ignore; hud.Add(status);
            dragBox = new VisualElement { pickingMode = PickingMode.Ignore }; dragBox.style.position = Position.Absolute; dragBox.style.display = DisplayStyle.None;
            Border(dragBox, Mint, 1); dragBox.style.backgroundColor = new Color(.2f, .8f, .6f, .12f); hud.Add(dragBox);
            briefing = Modal(); var card = Card(540); briefing.Add(card);
            card.Add(Text("OPERATIONS BRIEFING   /   01", 12, Brass)); card.Add(Text("KEEP THE SOUTH CONNECTED", 25, Paper));
            var brief = Text("A short, fictional command exercise on the 1948 map. The coastal depot is your lifeline. Secure the route before eight minutes run out.\n\n1  Select the worker [4], build a camp [C], then harvest citrus [G].\n2  Select scouts [2]. Right-click each immigrant group to rescue it. They walk to your camp and become workers.\n3  Send the brigade [1] south to clear patrols. Escort the convoy [3] to the outpost [O], return to depot [H], and repeat.\n4  Send a worker to the outpost [O], then build a settlement [B].\n\nBrigades defeat patrols. Scouts move fast but lose direct fights. Convoys and workers need protection. Recruit [R] and repair units near the depot.\n\nPause freely with SPACE; issue orders while paused. Click units or drag a selection box. Right-click gives orders. WASD pans; the wheel zooms.", 14, Paper);
            brief.style.whiteSpace = WhiteSpace.Normal; brief.style.marginTop = brief.style.marginBottom = 16;
            var briefScroll = new ScrollView(ScrollViewMode.Vertical); briefScroll.style.flexShrink = 1; briefScroll.Add(brief); card.Add(briefScroll);
            card.Add(Button("BEGIN OPERATION   →", () => { briefingOpen = false; briefing.style.display = DisplayStyle.None; Model.Paused = false; Debug.Log("COMMAND START"); Beep(); }));
            hud.Add(briefing);
            endPanel = Modal(); var ending = Card(470); endPanel.Add(ending); result = Text("", 25, Brass); ending.Add(result);
            var endCopy = Text("", 15, Paper); endCopy.name = "command-result-copy"; endCopy.style.whiteSpace = WhiteSpace.Normal; endCopy.style.marginTop = endCopy.style.marginBottom = 18; ending.Add(endCopy);
            ending.Add(Button("PLAY AGAIN", Restart)); hud.Add(endPanel);
        }
        VisualElement Panel() { var e = new VisualElement(); e.style.position = Position.Absolute; e.style.backgroundColor = new Color(Ink.r, Ink.g, Ink.b, .96f); Border(e, new Color(.42f,.42f,.28f), 1); e.style.paddingLeft = e.style.paddingRight = 12; e.style.paddingTop = e.style.paddingBottom = 10; return e; }
        static void Stretch(VisualElement e) { e.style.position = Position.Absolute; e.style.left = e.style.right = e.style.top = e.style.bottom = 0; }
        static void Border(VisualElement e, Color color, int width) { e.style.borderLeftColor = e.style.borderRightColor = e.style.borderTopColor = e.style.borderBottomColor = color; e.style.borderLeftWidth = e.style.borderRightWidth = e.style.borderTopWidth = e.style.borderBottomWidth = width; }
        static Label Text(string text, int size, Color color) { var label = new Label(text) { pickingMode = PickingMode.Ignore }; label.style.color = color; label.style.fontSize = size; label.style.marginLeft = label.style.marginRight = 0; return label; }
        Button Button(string text, Action action) {
            var b = new Button(() => { action(); RefreshHud(); }) { text = text };
            b.style.backgroundColor = new Color(.18f,.23f,.20f); b.style.color = Paper; b.style.fontSize = 12;
            b.style.paddingLeft = b.style.paddingRight = 10; b.style.paddingTop = b.style.paddingBottom = 7;
            b.style.marginRight = 5; b.style.marginBottom = 5; Border(b, new Color(.49f,.44f,.28f), 1); return b;
        }
        VisualElement Modal() { var e = new VisualElement(); Stretch(e); e.style.backgroundColor = new Color(.025f,.04f,.04f,.78f); e.style.alignItems = Align.Center; e.style.justifyContent = Justify.Center; return e; }
        VisualElement Card(int width) { var e = Panel(); e.style.position = Position.Relative; e.style.width = width; e.style.maxWidth = Length.Percent(94); e.style.maxHeight = Length.Percent(92); e.style.paddingLeft = e.style.paddingRight = 25; e.style.paddingTop = e.style.paddingBottom = 22; Border(e, Brass, 2); return e; }
        void ShowBriefing() { briefingOpen = true; Model.Paused = true; briefing.style.display = DisplayStyle.Flex; }
        void ReserveSpace(NavigationSubmitEvent evt) {
            if (Keyboard.current?.spaceKey.isPressed != true && Keyboard.current?.spaceKey.wasPressedThisFrame != true) return;
            (evt.target as VisualElement)?.focusController?.IgnoreEvent(evt); evt.StopImmediatePropagation();
        }
        void TogglePause() {
            if (Model.Outcome != CommandOutcome.Playing) return;
            if (briefingOpen) { briefingOpen = false; briefing.style.display = DisplayStyle.None; Model.Paused = false; }
            else Model.Paused = !Model.Paused;
            Debug.Log("COMMAND " + (Model.Paused ? "PAUSE" : "START"));
            Beep();
        }
        void Harvest()
        {
            if (!Model.Units.Any(u => selected.Contains(u.Id) && u.Kind == CommandKind.Worker && u.Alive)) SelectKind(CommandKind.Worker);
            if (Model.Harvest(selected) > 0) ShowOrderFeedback(CommandModel.Grove);
            else Model.Announce("No worker crew is available to harvest.");
        }
        void Build(CommandKind kind) { var worker = Model.Units.FirstOrDefault(u => selected.Contains(u.Id) && u.Alive && u.Kind == CommandKind.Worker); Model.Build(worker != null ? worker.Id : -1, kind); Beep(); }
        void SelectKind(CommandKind kind) { selected.Clear(); foreach (var u in Model.Units) if (u.Kind == kind && u.Selectable) selected.Add(u.Id); Beep(); }
        void OrderSelection(CommandPoint at, int target = -1) {
            int i = 0; foreach (int id in selected) { var u = Model.Find(id); if (u == null || !u.Selectable) continue;
                var destination = target >= 0 || selected.Count == 1 ? at : new CommandPoint(at.X + (i % 3 - 1) * 2, at.Y + i / 3 * 2);
                Model.Order(id, destination, target); i++;
            }
            if (i == 0) Model.Announce("Select a unit first: click, drag a box, or press 1–4.");
            else ShowOrderFeedback(at);
        }
        void ShowOrderFeedback(CommandPoint at)
        {
            if (orderRing == null) orderRing = Ring(sceneRoot.transform, lineMaterial, 950, 70);
            orderRing.transform.position = World(at) + Vector3.up * 80; orderRing.gameObject.SetActive(true);
            ringUntil = Time.unscaledTime + 1.1f; Beep();
        }
        bool OverUi(Vector2 p) => UiPointer.IsOverUi(root.panel, p, Screen.height);
        void Update()
        {
            if (!attached || disposed) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f); var mouse = Mouse.current; var keys = Keyboard.current;
            if (gallery != null && gallery.IsOpen) { gallery.Tick(dt); if (keys?.escapeKey.wasPressedThisFrame == true) gallery.Close(); return; }
            if (keys != null) {
                if (keys.spaceKey.wasPressedThisFrame) TogglePause();
                if (!briefingOpen && Model.Outcome == CommandOutcome.Playing) {
                    if (keys.digit1Key.wasPressedThisFrame) SelectKind(CommandKind.Brigade);
                    if (keys.digit2Key.wasPressedThisFrame) SelectKind(CommandKind.Scout);
                    if (keys.digit3Key.wasPressedThisFrame) SelectKind(CommandKind.Convoy);
                    if (keys.digit4Key.wasPressedThisFrame) SelectKind(CommandKind.Worker);
                    if (keys.gKey.wasPressedThisFrame) Harvest();
                    if (keys.cKey.wasPressedThisFrame) Build(CommandKind.Camp);
                    if (keys.bKey.wasPressedThisFrame) Build(CommandKind.Settlement);
                    if (keys.rKey.wasPressedThisFrame) { Model.Recruit(); Beep(); }
                    if (keys.hKey.wasPressedThisFrame) OrderSelection(CommandModel.Base);
                    if (keys.oKey.wasPressedThisFrame) OrderSelection(CommandModel.Outpost);
                    if (keys.fKey.wasPressedThisFrame) Home();
                    if (keys.escapeKey.wasPressedThisFrame) { selected.Clear(); dragging = false; dragBox.style.display = DisplayStyle.None; }
                }
            }
            if (!briefingOpen && Model.Outcome == CommandOutcome.Playing) {
                CameraInput input = default;
                if (keys != null) input.Pan = new Vector2((keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0), (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0));
                if (mouse != null) {
                    var pos = mouse.position.ReadValue(); bool over = OverUi(pos);
                    input.OverUi = over; input.Zoom = Mathf.Clamp(mouse.scroll.ReadValue().y, -3, 3);
                    if (mouse.middleButton.isPressed && !over) input.DragPan = pos - lastMouse;
                    if (mouse.leftButton.wasPressedThisFrame && !over) { downAt = pos; dragging = true; }
                    if (dragging && mouse.leftButton.isPressed && Vector2.Distance(pos, downAt) > 7) ShowDrag(downAt, pos);
                    if (dragging && mouse.leftButton.wasReleasedThisFrame) { if (!over) PickUnits(downAt, pos, keys != null && keys.shiftKey.isPressed); dragging = false; dragBox.style.display = DisplayStyle.None; }
                    if (mouse.rightButton.wasPressedThisFrame && !over && TryGround(pos, out var ground)) {
                        var enemy = NearestUnit(pos, false); OrderSelection(Point(ground), enemy != null && enemy.Kind == CommandKind.Threat ? enemy.Id : -1);
                    }
                    lastMouse = pos;
                }
                CameraController.Step(rig, input, dt, cameraBounds, ground, groundTop);
            }
            Model.Advance(dt); RefreshViews();
            if (Time.unscaledTime >= refreshAt) { refreshAt = Time.unscaledTime + .1f; RefreshHud(); }
            if (orderRing != null && Time.unscaledTime > ringUntil) orderRing.gameObject.SetActive(false);
            if (Model.Outcome != shownOutcome) {
                shownOutcome = Model.Outcome; endPanel.style.display = DisplayStyle.Flex;
                result.text = Model.Outcome == CommandOutcome.Won ? "SOUTHERN ROUTE SECURED" : "OPERATION LOST";
                endPanel.Q<Label>("command-result-copy").text = Model.Report + "\n\n" + Model.Deliveries + " deliveries · " + Model.RescuedGroups + " groups sheltered · " + (int)Model.Elapsed + " seconds"; Beep(true);
            }
        }
        void LateUpdate()
        {
            if (!attached || disposed) return;
            rig.TopInset = .10f; rig.Apply();
            foreach (var u in Model.Units) if (views.TryGetValue(u.Id, out var v)) PlaceLabel(v.Label, World(u.Position) + Vector3.up * 700, 0);
            foreach (var site in sites) PlaceLabel(site.label, World(site.point), 0);
            for (int i = floats.Count - 1; i >= 0; i--) {
                var f = floats[i]; if (Time.unscaledTime >= f.Until) { f.Label.RemoveFromHierarchy(); floats.RemoveAt(i); }
                else { PlaceLabel(f.Label, f.At, -(1.5f-(f.Until-Time.unscaledTime))*25); f.Label.style.opacity = Mathf.Clamp01(f.Until-Time.unscaledTime); }
            }
        }
        void ShowDrag(Vector2 start, Vector2 end) {
            var a = UiPointer.ToPanel(root.panel, start, Screen.height); var b = UiPointer.ToPanel(root.panel, end, Screen.height);
            dragBox.style.display = DisplayStyle.Flex; dragBox.style.left = Mathf.Min(a.x,b.x); dragBox.style.top = Mathf.Min(a.y,b.y);
            dragBox.style.width = Mathf.Abs(a.x-b.x); dragBox.style.height = Mathf.Abs(a.y-b.y);
        }
        void PickUnits(Vector2 start, Vector2 end, bool additive)
        {
            if (!additive) selected.Clear();
            if (Vector2.Distance(start,end) < 8) { var found = NearestUnit(end, true); if (found != null) { if (additive && selected.Contains(found.Id)) selected.Remove(found.Id); else selected.Add(found.Id); } }
            else {
                var rect = Rect.MinMaxRect(Mathf.Min(start.x,end.x),Mathf.Min(start.y,end.y),Mathf.Max(start.x,end.x),Mathf.Max(start.y,end.y));
                foreach (var u in Model.Units) if (u.Selectable) { var p = cam.WorldToScreenPoint(World(u.Position)); if (p.z > 0 && rect.Contains(new Vector2(p.x,p.y))) selected.Add(u.Id); }
            }
            Beep(); RefreshHud();
        }
        CommandUnit NearestUnit(Vector2 screen, bool friendlyOnly)
        {
            CommandUnit best = null; float nearest = 30;
            foreach (var u in Model.Units) {
                if (!u.Alive || (friendlyOnly && !u.Selectable)) continue;
                var projected = cam.WorldToScreenPoint(World(u.Position)); if (projected.z <= 0) continue;
                float d = Vector2.Distance(screen, new Vector2(projected.x,projected.y)); if (d < nearest) { nearest = d; best = u; }
            }
            return best;
        }
        bool TryGround(Vector2 screen, out Vector3 position) {
            var collider = map.Terrain.GetComponent<TerrainCollider>();
            if (collider != null && collider.Raycast(cam.ScreenPointToRay(screen), out var hit, 1000000)) { position = hit.point; return true; }
            position = default; return false;
        }
        void PlaceLabel(Label label, Vector3 at, float rise) {
            if (root.panel == null) return; var p = cam.WorldToScreenPoint(at);
            label.style.display = p.z > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (p.z <= 0) return; var panel = UiPointer.ToPanel(root.panel, new Vector2(p.x,p.y),Screen.height);
            label.style.left = panel.x - label.resolvedStyle.width / 2; label.style.top = panel.y - 17 + rise;
        }
        void RefreshHud()
        {
            if (Model == null) return;
            resources.text = "$" + Model.Money + "k  EXPORTS     ▣ " + Model.Supplies + "  SUPPLY";
            int remaining = Math.Max(0,480-(int)Model.Elapsed); clock.text = (remaining/60).ToString("00")+":"+(remaining%60).ToString("00");
            pauseButton.text = Model.Paused ? "SPACE · Resume" : "SPACE · Pause";
            objective.text = (Model.CampReady ? "✓" : "□") + " Build a transit camp\n" + (Model.RescuedGroups>=2 ? "✓" : "□") + " Shelter immigrant groups   " + Model.RescuedGroups + "/2\n" +
                (Model.Deliveries>=2 ? "✓" : "□") + " Supply the southern outpost   " + Model.Deliveries + "/2\n" + (Model.SettlementReady ? "✓" : "□") + " Found the southern settlement";
            var chosen = Model.Units.Where(u => selected.Contains(u.Id) && u.Selectable).ToList();
            selection.text = chosen.Count == 0 ? "SELECT A UNIT  ·  Click / drag on the map or press 1–4" :
                chosen.Count == 1 ? chosen[0].Name.ToUpperInvariant() + "  ·  " + (int)chosen[0].Health + "/" + (int)chosen[0].MaxHealth + " readiness" + (chosen[0].Kind == CommandKind.Convoy ? "  ·  cargo " + chosen[0].Cargo + "/30" : "") : chosen.Count + " UNITS SELECTED  ·  right-click to move together";
            if (lastReport != Model.ReportVersion) { lastReport = Model.ReportVersion; report.text = "RADIO  /  " + Model.Report; }
            status.text = Model.Paused && !briefingOpen ? "PAUSED · ORDERS STILL AVAILABLE" : "";
        }
        void OnFeedback(CommandPoint at, string text)
        {
            if (floats.Count >= 32) { floats[0].Label.RemoveFromHierarchy(); floats.RemoveAt(0); }
            var label = Text(text, 12, text.StartsWith("−",StringComparison.Ordinal) || text == "Disabled" ? Red : Mint); label.style.position = Position.Absolute;
            label.style.backgroundColor = new Color(.03f,.06f,.04f,.82f); label.style.paddingLeft = label.style.paddingRight = 5;
            worldLabels.Add(label); floats.Add(new FloatText { Label = label, At = World(at)+Vector3.up*900, Until = Time.unscaledTime+1.5f });
        }
        void RefreshViews()
        {
            var mouse = Mouse.current;
            var hovered = mouse != null && !OverUi(mouse.position.ReadValue()) ? NearestUnit(mouse.position.ReadValue(), true) : null;
            foreach (var u in Model.Units) {
                if (views.TryGetValue(u.Id, out var oldView) && oldView.Kind != u.Kind) {
                    DestroyOwned(oldView.Body); oldView.Label.RemoveFromHierarchy(); oldView.Dot.RemoveFromHierarchy(); views.Remove(u.Id);
                }
                if (!views.TryGetValue(u.Id,out var v)) { v = MakeUnit(u); views.Add(u.Id,v); }
                v.Motion?.Update(u.Moving || (u.Kind == CommandKind.Immigrants && u.Rescued), Model.Paused);
                v.Body.SetActive(u.Alive); v.Label.style.visibility = u.Alive ? Visibility.Visible : Visibility.Hidden; v.Dot.style.display = u.Alive ? DisplayStyle.Flex : DisplayStyle.None;
                if (!u.Alive) { selected.Remove(u.Id); continue; }
                var world = World(u.Position); v.Body.transform.position = world;
                var towards = World(u.Destination)-world; towards.y = 0;
                if (u.Moving && towards.sqrMagnitude > 1) v.Body.transform.rotation = Quaternion.LookRotation(towards);
                bool active = selected.Contains(u.Id); v.Ring.gameObject.SetActive(active || u.Kind == CommandKind.Threat);
                // Depot/site banners identify ready buildings; friendly detail appears on selection, hover or damage.
                bool showLabel = active || u == hovered || u.Kind == CommandKind.Threat || u.Kind == CommandKind.Immigrants || u.BuildRemaining > 0 || u.Health < u.MaxHealth;
                v.Label.style.visibility = showLabel ? Visibility.Visible : Visibility.Hidden;
                v.Path.gameObject.SetActive(active && u.Moving);
                if (active && u.Moving) {
                    var pathClearance = new Vector3(0, 100, 0); // Metres above the sampled ground, not a UI scale.
                    v.Path.SetPosition(0,world+pathClearance); v.Path.SetPosition(1,World(u.Destination)+pathClearance);
                }
                v.Label.text = u.Name + (u.Kind == CommandKind.Immigrants ? (u.Rescued ? " · returning" : " · rescue") : " · " + (int)u.Health+"/"+(int)u.MaxHealth) + (u.BuildRemaining>0 ? " · "+(int)u.BuildRemaining+"s" : "");
                v.Label.style.color = u.Kind == CommandKind.Threat ? Red : active ? Mint : Paper;
                v.Dot.style.left = u.Position.X/100*164; v.Dot.style.top = (100-u.Position.Y)/100*164;
                v.LastHealth = u.Health;
            }
        }
        UnitView MakeUnit(CommandUnit u)
        {
            var body = new GameObject("Command "+u.Name); body.transform.SetParent(sceneRoot.transform,false);
            bool hostile = u.Kind == CommandKind.Threat; var paint = hostile ? orange : olive;
            bool imported = CommandArt.TryAttach(u.Kind, body.transform, out var motion);
            if (imported) { /* Imported art is a child; orders, labels and rings remain independent. */ }
            else if (u.Kind == CommandKind.Convoy) {
                Box(body.transform,new Vector3(0,180,0),new Vector3(450,260,1050),olive);
                Box(body.transform,new Vector3(0,430,320),new Vector3(420,350,340),cream);
                Box(body.transform,new Vector3(0,480,-200),new Vector3(420,420,580),cream);
                for (int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2) Sphere(body.transform,new Vector3(x*245,120,z*320),new Vector3(180,220,220),black);
            } else if (u.Kind == CommandKind.Depot || u.Kind == CommandKind.Settlement || u.Kind == CommandKind.Camp) {
                for(int i=0;i<3;i++) { var pos=new Vector3((i-1)*550,200,(i%2)*450); Box(body.transform,pos,new Vector3(460,400,430),cream); Box(body.transform,pos+Vector3.up*220,new Vector3(500,60,470),olive); }
                Box(body.transform,new Vector3(850,450,0),new Vector3(80,900,80),black); Box(body.transform,new Vector3(1070,800,0),new Vector3(430,220,40),friendly);
            } else {
                int people = u.Kind == CommandKind.Brigade || hostile ? 4 : 3;
                for(int i=0;i<people;i++) {
                    var at=new Vector3((i%2-.5f)*300,0,(i/2-.5f)*300);
                    Box(body.transform,at+Vector3.up*240,new Vector3(145,310,130),u.Kind==CommandKind.Worker || u.Kind==CommandKind.Immigrants ? cream : paint);
                    Sphere(body.transform,at+Vector3.up*475,new Vector3(155,155,155),cream);
                    if(u.Kind==CommandKind.Brigade || u.Kind==CommandKind.Scout || hostile) Box(body.transform,at+new Vector3(100,300,180),new Vector3(50,50,390),black);
                    else Box(body.transform,at+new Vector3(130,170,0),new Vector3(150,180,150),orange);
                }
            }
            var ring=Ring(body.transform,hostile?danger:friendly,700,55);
            var path = Line(body.transform,lineMaterial,35,2); path.useWorldSpace=true;
            var label=Text(u.Name,11,hostile?Red:Paper); label.style.position=Position.Absolute; label.style.backgroundColor=new Color(.025f,.04f,.03f,.75f); label.style.paddingLeft=label.style.paddingRight=4; worldLabels.Add(label);
            var dot=new VisualElement {pickingMode=PickingMode.Ignore}; dot.style.position=Position.Absolute; dot.style.width=dot.style.height=6; dot.style.backgroundColor=hostile?Red:u.Kind==CommandKind.Immigrants?Brass:Mint; mini.Add(dot);
            return new UnitView {Body=body,Ring=ring,Path=path,Label=label,Dot=dot,LastHealth=u.Health,Kind=u.Kind,Motion=motion};
        }
        GameObject Shape(bool sphere, Transform parent, Vector3 at, Vector3 scale, Material material)
        {
            var mesh = sphere ? sphereMesh : cubeMesh;
            if (mesh == null) {
                mesh = CreateShapeMesh(sphere); assets.Add(mesh);
                if (sphere) sphereMesh = mesh; else cubeMesh = mesh;
            }
            // CreatePrimitive also creates a collider, which can be stripped from WebGL players. These props never need one.
            var go = new GameObject("Command procedural prop"); go.transform.SetParent(parent, false);
            go.transform.localPosition = at; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material; return go;
        }
        /// <summary>Unit-sized, flat-shaded geometry shared by all prototype props. Caller owns the returned mesh.</summary>
        internal static Mesh CreateShapeMesh(bool sphere)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            void Triangle(Vector3 a, Vector3 b, Vector3 c) {
                int first = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
            }
            if (sphere) {
                const int bands = 8, segments = 12;
                Vector3 Point(int band, int segment) {
                    float latitude = -Mathf.PI / 2 + Mathf.PI * band / bands, longitude = 2 * Mathf.PI * segment / segments;
                    return new Vector3(Mathf.Cos(latitude) * Mathf.Cos(longitude), Mathf.Sin(latitude), Mathf.Cos(latitude) * Mathf.Sin(longitude)) * .5f;
                }
                for (int band = 0; band < bands; band++) for (int segment = 0; segment < segments; segment++) {
                    var a = Point(band, segment); var b = Point(band + 1, segment);
                    var c = Point(band + 1, segment + 1); var d = Point(band, segment + 1);
                    if (band < bands - 1) Triangle(a, b, c);
                    if (band > 0) Triangle(a, c, d);
                }
            } else {
                // Each face gets separate vertices so boxes retain crisp planar normals.
                var corners = new[] {
                    new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f),
                    new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f)
                };
                var faces = new[] { 0,3,2,1, 4,5,6,7, 0,4,7,3, 1,2,6,5, 0,1,5,4, 3,7,6,2 };
                for (int face = 0; face < faces.Length; face += 4) {
                    var a = corners[faces[face]]; var b = corners[faces[face + 1]];
                    var c = corners[faces[face + 2]]; var d = corners[faces[face + 3]];
                    Triangle(a, b, c); Triangle(a, c, d);
                }
            }
            var result = new Mesh { name = sphere ? "Command faceted sphere" : "Command box" };
            result.SetVertices(vertices); result.SetTriangles(triangles, 0); result.RecalculateNormals(); result.RecalculateBounds();
            return result;
        }
        void Box(Transform parent,Vector3 at,Vector3 scale,Material material)=>Shape(false,parent,at,scale,material);
        void Sphere(Transform parent,Vector3 at,Vector3 scale,Material material)=>Shape(true,parent,at,scale,material);
        LineRenderer Line(Transform parent, Material material, float width, int count)
        {
            var go=new GameObject("Command line"); go.transform.SetParent(parent,false); var line=go.AddComponent<LineRenderer>();
            line.sharedMaterial=material; line.startWidth=line.endWidth=width; line.positionCount=count; line.useWorldSpace=false; line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; return line;
        }
        LineRenderer Ring(Transform parent,Material material,float radius,float width)
        {
            var line=Line(parent,material,width,40); line.loop=true;
            for(int i=0;i<40;i++) { float a=i*Mathf.PI*2/40; line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,90,Mathf.Sin(a)*radius)); } return line;
        }
        void MakeSites()
        {
            void Site(CommandPoint point,string title) { var label=Text(title,12,Brass); label.style.position=Position.Absolute; label.style.backgroundColor=new Color(.04f,.06f,.04f,.85f); worldLabels.Add(label); sites.Add((point,label)); }
            Site(new CommandPoint(24,91),"COASTAL DEPOT  /  REPAIR + RELOAD"); Site(new CommandPoint(43,82),"CITRUS GROVE  /  EXPORTS"); Site(new CommandPoint(60,25),"SOUTHERN OUTPOST  /  DELIVER HERE");
            var grove=new GameObject("Command citrus grove"); grove.transform.SetParent(sceneRoot.transform,false); grove.transform.position=World(CommandModel.Grove);
            for(int i=0;i<16;i++) { var at=new Vector3((i%4-1.5f)*550,0,(i/4-1.5f)*550); Box(grove.transform,at+Vector3.up*170,new Vector3(90,340,90),black); Sphere(grove.transform,at+Vector3.up*530,new Vector3(540,600,540),olive); Sphere(grove.transform,at+new Vector3(160,640,-160),new Vector3(100,100,100),orange); }
            var outpost=new GameObject("Command delivery marker"); outpost.transform.SetParent(sceneRoot.transform,false); outpost.transform.position=World(CommandModel.Outpost); Ring(outpost.transform,lineMaterial,1300,95);
            for(int i=0;i<4;i++) Box(outpost.transform,new Vector3(1700,i*160,0),new Vector3(260,140,300),cream);
            var route=Line(sceneRoot.transform,lineMaterial,45,32); route.useWorldSpace=true;
            for(int i=0;i<32;i++) {float t=i/31f; route.SetPosition(i,World(new CommandPoint(Mathf.Lerp(CommandModel.Base.X,CommandModel.Outpost.X,t),Mathf.Lerp(CommandModel.Base.Y,CommandModel.Outpost.Y,t)))+Vector3.up*70);}
        }
        static void DestroyOwned(Object obj) { if(obj==null)return; if(Application.isPlaying) Object.Destroy(obj); else Object.DestroyImmediate(obj); }
        public void Dispose() => Cleanup(true);
        void Cleanup(bool destroyComponent)
        {
            if(disposed)return; disposed=true;
            if(Model!=null)Model.Feedback-=OnFeedback;
            gallery?.Dispose(); gallery = null; pointerBlur?.Dispose(); navigationGuard?.Dispose();
            hud?.UnregisterCallback<NavigationSubmitEvent>(ReserveSpace, TrickleDown.TrickleDown);
            hud?.RemoveFromHierarchy(); foreach(var entry in hidden)entry.element.style.display=entry.display; hidden.Clear();
            foreach (var entry in hiddenMapLayers) if (entry.obj != null) entry.obj.SetActive(entry.active);
            hiddenMapLayers.Clear();
            if(controller!=null)controller.enabled=oldControllerEnabled; if(pointer!=null)pointer.enabled=oldPointerEnabled;
            if(rig!=null) {rig.LookAt=oldLook;rig.Distance=oldDistance;rig.Yaw=oldYaw;rig.TopInset=oldInset;rig.Apply();}
            DestroyOwned(sceneRoot); foreach(var asset in assets)DestroyOwned(asset); assets.Clear(); attached=false;
            if(destroyComponent && this!=null)DestroyOwned(this);
        }
        void OnDestroy() { Cleanup(false); }
        void OnApplicationFocus(bool focus) { if(!focus && Model!=null) {Model.Paused=true; dragging=false; if(dragBox!=null)dragBox.style.display=DisplayStyle.None;} }
    }
}
