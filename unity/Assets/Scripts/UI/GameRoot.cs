#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Starts the game once GameBoot has loaded the inputs and built the map (GameBoot.Ready): continues the
    /// autosave or starts a new game (GameStart.Begin, with its notes in the toast), creates every screen, autosaves each
    /// turn, and wires the camera and the map pointer to the game panel (UiPointer.IsOverUi), map clicks to the info panel,
    /// the minimap to the camera, and State.Placements to the placement layer. Disabling it disposes all of that.
    /// With "smoke" in the page URL (WebGL, e.g. ?smoke=5) or -smoke N (desktop) it starts a fresh game with the golden
    /// seed (the player's autosave is neither read nor written), plays N turns through the UI's own buttons and logs
    /// "SMOKE OK ..." (or "SMOKE FAIL ...").
    /// ExecuteAlways only so that disabling it disposes the presenters in edit mode too (tests); GameBoot raises Ready
    /// in play mode and players only.</summary>
    [ExecuteAlways]
    public sealed class GameRoot : MonoBehaviour
    {
        /// <summary>Turns a bare smoke flag ("?smoke", "-smoke") plays.</summary>
        internal const int DefaultSmokeTurns = 5;

        [SerializeField] GameBoot boot = null!;
        [SerializeField] UIDocument document = null!;
        [SerializeField] TextAsset strings = null!;
        [SerializeField] PortraitCatalog portraits = null!;
        [SerializeField] MapScene map = null!;
        [SerializeField] PlacementLayer placements = null!;
        [SerializeField] MapPointer pointer = null!;
        [SerializeField] CameraController cameraController = null!;

        /// <summary>Everything Begin created that must be undone, disposed in reverse order.</summary>
        readonly List<IDisposable> owned = new List<IDisposable>();
        MinimapPresenter? minimap;
        MapLabelsPresenter? labels;
        ScreenNav? nav;
        CameraRig? rig;
        List<(string, int, int, DateTime)>? drawn;

        public SimBridge? Bridge { get; private set; }
        /// <summary>Where the autosave lives; null (the default) = SaveStore.ForPlatform(). Tests set a temporary folder.</summary>
        internal SaveStore? Store { get; set; }

        public void Configure(GameBoot boot, UIDocument document, TextAsset strings, PortraitCatalog portraits, MapScene map,
            PlacementLayer placements, MapPointer pointer, CameraController cameraController)
        {
            var oldBoot = this.boot;
            this.boot = boot; this.document = document; this.strings = strings; this.portraits = portraits; this.map = map;
            this.placements = placements; this.pointer = pointer; this.cameraController = cameraController;
            // An enabled GameRoot is already subscribed to the old boot's Ready (OnEnable): move the subscription.
            if (!isActiveAndEnabled) return;
            if (oldBoot != null) { oldBoot.Ready -= Begin; oldBoot.Failed -= OnBootFailed; }
            if (this.boot != null) { this.boot.Ready += Begin; this.boot.Failed += OnBootFailed; }
        }

        void OnEnable() { if (boot != null) { boot.Ready += Begin; boot.Failed += OnBootFailed; } }

        /// <summary>A boot failure replaces the loading line with the reason; the loading state stays, so no half-built panel
        /// shows (M3 final review I5).</summary>
        internal void OnBootFailed(string reason)
        {
            if (document == null || strings == null) return;
            var line = document.rootVisualElement.Q<Label>("loading");
            if (line != null) line.text = UiText.Parse(strings.text).Format("boot.failed", GameBoot.ForPlayer(reason).TrimEnd('.'));
        }

        void OnDisable()
        {
            if (boot != null) { boot.Ready -= Begin; boot.Failed -= OnBootFailed; }
            Shutdown();
        }

        // "smoke" as a whole query parameter: bare ("?smoke", "&smoke=") or with a number; it ends at '&', '#' or the end, so
        // "?smoke-x", "?smoker" and "?smoke=five" are not the flag: UrlFlags' whole-parameter rule, plus digits only (amendment A3).
        static readonly Regex SmokeQuery = new Regex(@"[?&]smoke(?:=(\d*))?(?=&|#|$)", RegexOptions.CultureInvariant);

        /// <summary>Smoke-test turn count: from the page URL on WebGL, else from the command line. "?smoke=N" / "-smoke N"
        /// play N turns (0 = off); a bare "?smoke" / "-smoke" (or "-smoke" followed by another flag) plays DefaultSmokeTurns;
        /// a value that is not a non-negative int (too big, negative, not a number) is off on both. 0 when not asked.</summary>
        internal static int SmokeTurns(string? url, string[] args, bool webgl)
        {
            if (webgl)
            {
                var m = SmokeQuery.Match(url ?? "");
                if (!m.Success) return 0;
                var digits = m.Groups[1].Value;
                return digits.Length == 0 ? DefaultSmokeTurns : int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : 0;
            }
            var i = Array.IndexOf(args, "-smoke");
            if (i < 0) return 0;
            if (i + 1 >= args.Length) return DefaultSmokeTurns;
            var value = args[i + 1];
            if (value.StartsWith("-", StringComparison.Ordinal) && !Regex.IsMatch(value, @"^-\d")) return DefaultSmokeTurns;   // the next flag
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var t) ? t : 0;
        }

        public static int SmokeTurns() =>
            SmokeTurns(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        /// <summary>GameBoot.Ready: starts the game, smoke-testing when the URL or command line asks.</summary>
        public void Begin(GameInputs inputs) => Begin(inputs, SmokeTurns());

        /// <summary>Starts (or restarts) the game over the built map. <paramref name="smoke"/> &gt; 0 plays that many turns.</summary>
        internal void Begin(GameInputs inputs, int smoke)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            foreach (var (value, field) in new (UnityEngine.Object?, string)[] { (document, nameof(document)), (strings, nameof(strings)),
                (map, nameof(map)), (placements, nameof(placements)), (pointer, nameof(pointer)), (cameraController, nameof(cameraController)) })
                if (value == null) throw new InvalidOperationException($"GameRoot: {field} is not assigned (run MapCapture.MakeScene)");
            if (map.Terrain == null) throw new InvalidOperationException("GameRoot: the map is not built (GameBoot builds it before Ready)");
            Shutdown();

            var text = UiText.Parse(strings.text);
            var store = Store ?? SaveStore.ForPlatform();
            var notes = new List<string>();
            // A smoke run is reproducible: a fresh game with the golden seed, never the player's autosave.
            var session = smoke > 0
                ? GameStart.Begin(inputs.Content, store, () => GoldenRun.Seed, notes, text, fresh: true)
                : GameStart.Begin(inputs.Content, store, GameStart.NewSeed, notes, text);
            DiagLog.BootPhase($"session started (turn {session.State.Turn}, replayed)");
            var bridge = Bridge = new SimBridge(session, text);
            var root = document.rootVisualElement;

            // The toast first: the autosave (below) must report a failed write after the toast has shown the turn's refusals.
            var toast = Own(new ToastPresenter(root, bridge, text));
            Own(new HudPresenter(root, bridge, text));
            nav = Own(new ScreenNav(root));                                      // every screen starts hidden (USS); ScreenNav opens one at a time
            Own(new PointerBlur(root));                                    // a mouse click leaves no focus fill behind (keyboard keeps it)
            Own(new NavigationGuard(root));                                // the camera keys (WASD, arrows) never move UI focus (B6)
            if (DiagLog.Nav) Own(new NavEventLog(root));                   // ?navlog: every UI pointer/submit event (playtest 2, P2-4)
            var card = Own(new EventCardPresenter(root, bridge, text, portraits, EventImageCatalog.Load()));   // event photos: Resources, no scene field
            Own(new DefencePresenter(root, bridge, text, portraits));
            Own(new BudgetPresenter(root, bridge, text));
            Own(new WhyPresenter(root, bridge, text));
            Own(new CabinetPresenter(root, bridge, text, portraits));
            var names = SettlementPlacer.Parse(inputs.Map.SettlementsYaml).ToDictionary(s => s.Id, s => s.Name);
            // The 1948 sub-districts, outlined on the map (display only: the sim never reads them; docs/visual-v1.md).
            var subdistricts = Subdistricts1948.Show(inputs.Map.RegionsYaml, bridge.State.Date);
            var layerLabel = root.Q<Label>("map-layer");
            layerLabel.text = subdistricts.Label;
            // Shown from the start until replaced (owner ruling 2026-10-11), with its label while drawn.
            Action onDate = () =>
            {
                subdistricts.Apply(bridge.State.Date);
                layerLabel.style.display = subdistricts.Shown ? DisplayStyle.Flex : DisplayStyle.None;
            };
            onDate();
            bridge.Changed += onDate;
            owned.Add(new Undo(() => bridge.Changed -= onDate));
            var info = Own(new InfoPanelPresenter(root, bridge, text, names, subdistricts));
            // A smoke session never touches the player's save: no autosave, and no New Game (which deletes the autosave).
            // Leaving New Game out is safer than handing it a throwaway store: no file is ever deleted, no path has to be
            // right on every platform, and the scripted run never presses it.
            if (smoke == 0)
            {
                Own(new NewGamePresenter(root, bridge, store, toast, text, GameStart.NewSeed));
                Own(GameStart.Autosave(bridge, store, text, toast.Append));   // appends: the turn's refusals stay visible
            }

            Func<Vector2, bool> overUi = p => UiPointer.IsOverUi(root.panel, p, Screen.height);
            var terrain = map.Terrain;
            var size = terrain.terrainData.size;
            var oldBounds = cameraController.Bounds;
            var oldGround = cameraController.Ground; var oldTop = cameraController.GroundTop;
            cameraController.IsOverUi = overUi;
            cameraController.Bounds = new Rect(terrain.transform.position.x, terrain.transform.position.z, size.x, size.z);
            cameraController.Ground = CameraController.GroundOf(terrain);
            cameraController.GroundTop = CameraController.GroundTopOf(terrain);
            pointer.Map = map;
            pointer.IsOverUi = overUi;
            Action<int, int> onHex = info.Show;
            pointer.HexClicked += onHex;
            var camRig = rig = cameraController.GetComponent<CameraRig>();
            Own(new TopBarInset(root, camRig));                            // the map is framed below the top bar
            var mini = minimap = Own(new MinimapPresenter(root, map.Hexes));
            labels = Own(new MapLabelsPresenter(root, map, cameraController.GetComponent<Camera>()));
            mini.Clicked += (lon, lat) => { camRig.LookAt = map.Frame.ToWorld(lon, lat, 0); cameraController.Settle(); SyncMinimap(); labels?.Refresh(); };   // the click may come after LateUpdate (review M6)
            Action onChanged = RenderPlacementsIfChanged;
            bridge.Changed += onChanged;
            // One console line per turn, so a browser run (reload, Continue) can be checked against the hash.
            Action<IReadOnlyList<Rejection>> onTurn = _ =>
                Debug.Log(FormattableString.Invariant($"TURN {bridge.State.Turn} {bridge.State.Date:yyyy-MM-dd} hash {bridge.State.Hash()}"));
            bridge.TurnEnded += onTurn;
            logCamera = CameraLogRequested();
            loggedDistance = loggedYaw = float.NaN;
            owned.Add(new Undo(() =>
            {
                bridge.Changed -= onChanged;
                bridge.TurnEnded -= onTurn;
                pointer.HexClicked -= onHex;
                if (pointer.IsOverUi == overUi) pointer.IsOverUi = null;
                if (cameraController.IsOverUi == overUi) cameraController.IsOverUi = null;
                if (pointer.Map == map) pointer.Map = null;
                cameraController.Bounds = oldBounds;
                cameraController.Ground = oldGround; cameraController.GroundTop = oldTop;
            }));
            drawn = null;
            RenderPlacementsIfChanged();
            SyncMinimap();
            root.Q("root")?.RemoveFromClassList("loading");                // ready: the bar, panels and screens may show (P2-1)
            DiagLog.BootPhase("screens ready");

            if (notes.Count > 0) toast.Show(notes);
            Debug.Log(FormattableString.Invariant($"GAME OK {bridge.State.Date:yyyy-MM-dd} turn {bridge.State.Turn} hash {bridge.State.Hash()}"));
            if (smoke > 0)
            {
                if (Application.isPlaying) StartCoroutine(Smoke(root, bridge, card, smoke, Debug.Log));
                else Debug.LogWarning("SMOKE FAIL the smoke run needs play mode or a player");
            }
            if (Application.isPlaying && PixelErrorProfile.Requested()) StartCoroutine(PixelErrorProfile.Run(map.Terrain));
            // The unit art slice at gameplay03 (a showcase, never in normal play; war mode's units are issue #104). Before the
            // instruments, so the frame profile lists its "units" toggle (the profile reads the toggles when it starts).
            if (Application.isPlaying && Statecraft.Game.Map.Units.UnitShowcase.Requested())
                Statecraft.Game.Map.Units.UnitShowcase.Spawn(map, Statecraft.Game.Map.Units.UnitShowcase.StillRequested(
                    Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer));
            // The border renderer's synthetic, labelled proof (?borderproof / -borderproof): development builds only.
            if (Application.isPlaying && BorderProof.Requested()) BorderProof.Show(map);
            // The visual track's instruments (docs/visual-v1.md): both move the camera, so one per run.
            if (Application.isPlaying && FrameProfile.Requested())
                StartCoroutine(FrameProfile.Run(camRig, map.Frame, Debug.Log, Application.platform != RuntimePlatform.WebGLPlayer));
            else if (Application.isPlaying && VisualShots.Requested()) StartCoroutine(VisualShots.Run(camRig, map.Frame, Debug.Log));
        }

        T Own<T>(T d) where T : IDisposable { owned.Add(d); return d; }

        /// <summary>Disposes every presenter, the autosave and the camera/pointer hooks (newest first). Safe to call twice.</summary>
        void Shutdown()
        {
            StopAllCoroutines();
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            owned.Clear();
            minimap = null; labels = null; nav = null; rig = null; drawn = null; logCamera = false;
            // A ?profile run cut short (a stopped coroutine never reaches its restore line) must not leave the terrain at the
            // profile's pixel error (Task 24 review M1). Nothing else changes it, so restoring is always right.
            if (map != null && map.Terrain != null) map.Terrain.heightmapPixelError = MapBuilder.TerrainPixelError;
        }

        /// <summary>Draws the placements only when State.Placements differs from what was drawn (every order and every turn
        /// raises Changed; rebuilding the markers each time would be wasted work).</summary>
        void RenderPlacementsIfChanged()
        {
            if (Bridge == null) return;
            Statecraft.Game.Map.Routes.RouteLayer.ShowAt(map, Bridge.State.Date);   // dated routes (the Burma Road from June 1948)
            var now = Bridge.State.Placements.Select(p => (p.Action, p.Q, p.R, p.Date)).ToList();
            if (drawn != null && drawn.SequenceEqual(now)) return;
            drawn = now;
            placements.Render(Bridge.State.Placements, Bridge.Content, map);
        }

        void SyncMinimap()
        {
            if (minimap == null || rig == null) return;
            minimap.SetView(map.Frame, rig.LookAt, rig.Distance);
        }

        bool logCamera;
        float loggedDistance = float.NaN, loggedYaw = float.NaN;

        /// <summary>"camlog" in the WebGL page URL (a whole query parameter) or -camlog on desktop: log the camera distance and
        /// yaw whenever they change (checks the wheel-zoom step in a real browser).</summary>
        internal static bool CameraLogRequested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "camlog") : Array.IndexOf(args, "-camlog") >= 0;

        static bool CameraLogRequested() =>
            CameraLogRequested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        internal static string CameraLine(float distance, float yaw) => FormattableString.Invariant($"CAMERA distance={distance:0} yaw={yaw:0.0}");

        /// <summary>After every Update (CameraController moves the camera in its Update), so the pills never trail the camera.</summary>
        void LateUpdate() => labels?.Refresh();

        void Update()
        {
            // Escape closes the open screen even when no element has the keyboard focus (ScreenNav also takes the UI
            // Cancel event from a focused element; closing twice is harmless).
            if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true) nav?.Close();
            SyncMinimap();
            if (!logCamera || rig == null || (rig.Distance == loggedDistance && rig.Yaw == loggedYaw)) return;
            loggedDistance = rig.Distance; loggedYaw = rig.Yaw;
            Debug.Log(CameraLine(rig.Distance, rig.Yaw));
        }

        /// <summary>Plays turns the way a player does: answers each open event by pressing the card's first option button,
        /// then presses End Turn. Logs "SMOKE turn &lt;n&gt; &lt;date&gt; refused &lt;k&gt;" per turn through <paramref name="log"/>,
        /// then "SMOKE OK &lt;n&gt; turns &lt;date&gt; &lt;hash&gt;", or "SMOKE FAIL &lt;reason&gt;" when a press does not take effect.
        /// Static, so an edit-mode test can drive it step by step.</summary>
        public static IEnumerator Smoke(VisualElement root, SimBridge bridge, EventCardPresenter card, int turns, Action<string> log)
        {
            for (int t = 0; t < turns; t++)
            {
                yield return null;
                for (int guard = 0; card.Shown != null; guard++)
                {
                    var shown = card.Shown;
                    var option = root.Q<VisualElement>("event-options")?.Q<Button>();
                    if (option == null || guard >= 50) { log(FormattableString.Invariant($"SMOKE FAIL event {shown.Id} has no option to press")); yield break; }
                    if (!Press(option, log)) yield break;
                    if (card.Shown == shown) { log(FormattableString.Invariant($"SMOKE FAIL the option '{option.text}' for event {shown.Id} was not taken")); yield break; }
                    yield return null;
                }
                var turn = bridge.State.Turn;
                int refused = 0;
                Action<IReadOnlyList<Rejection>> count = r => refused = r.Count;
                bridge.TurnEnded += count;
                bool pressed;
                try { pressed = Press(root.Q<Button>("end-turn"), log); }
                finally { bridge.TurnEnded -= count; }
                if (!pressed) yield break;
                if (bridge.State.Turn != turn + 1) { log(FormattableString.Invariant($"SMOKE FAIL End Turn did not end turn {turn}")); yield break; }
                var s = bridge.State;
                log(FormattableString.Invariant($"SMOKE turn {s.Turn} {s.Date:yyyy-MM-dd} refused {refused}"));
            }
            log(FormattableString.Invariant($"SMOKE OK {turns} turns {bridge.State.Date:yyyy-MM-dd} {bridge.State.Hash()}"));
        }

        static bool Press(Button? b, Action<string> log)
        {
            if (b == null) { log("SMOKE FAIL button missing"); return false; }
            try
            {
                using (var e = NavigationSubmitEvent.GetPooled()) { e.target = b; b.SendEvent(e); }
                return true;
            }
            catch (Exception e) { log("SMOKE FAIL " + e.Message); return false; }
        }

        sealed class Undo : IDisposable
        {
            readonly Action undo;
            public Undo(Action undo) { this.undo = undo; }
            public void Dispose() => undo();
        }
    }
}
