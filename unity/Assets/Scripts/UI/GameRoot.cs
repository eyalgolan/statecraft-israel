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
    /// "SMOKE OK ..." (or "SMOKE FAIL ..."). In a player, unless an automated run skips it (StartScreenPresenter.Skipped),
    /// the start screen shows over the started game before its first turn, with the camera keys stopped until it closes.
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
        [SerializeField] TextAsset stringsHe = null!;
        [SerializeField] PortraitCatalog portraits = null!;
        [SerializeField] MapScene map = null!;
        [SerializeField] PlacementLayer placements = null!;
        [SerializeField] MapPointer pointer = null!;
        [SerializeField] CameraController cameraController = null!;

        /// <summary>Everything Begin created that must be undone, disposed in reverse order.</summary>
        readonly List<IDisposable> owned = new List<IDisposable>();
        MinimapPresenter? minimap;
        Statecraft.Game.Map.Units.UnitLayer? units;
        /// <summary>The units in play (null before Begin).</summary>
        internal Statecraft.Game.Map.Units.UnitLayer? Units => units;
        /// <summary>The map's region layers (sub-districts, armistice, control; null before Begin): a test aid.</summary>
        internal MapRegions? Regions { get; private set; }
        MapLabelsPresenter? labels;
        ScreenNav? nav;
        StartScreenPresenter? start;
        EventCardPresenter? card;
        /// <summary>Tests: the decision card of the last Begin.</summary>
        internal EventCardPresenter? Card => card;
        OutcomePresenter? outcomes;
        CameraRig? rig;
        List<(string, int, int, DateTime)>? drawn;

        public SimBridge? Bridge { get; private set; }
        /// <summary>Where the autosave lives; null (the default) = SaveStore.ForPlatform(). Tests set a temporary folder.</summary>
        internal SaveStore? Store { get; set; }
        /// <summary>The UI's language; null (the default) = UiLanguage.Requested(). Tests set it.</summary>
        internal string? Language { get; set; }

        public void Configure(GameBoot boot, UIDocument document, TextAsset strings, TextAsset stringsHe, PortraitCatalog portraits,
            MapScene map, PlacementLayer placements, MapPointer pointer, CameraController cameraController)
        {
            var oldBoot = this.boot;
            this.boot = boot; this.document = document; this.strings = strings; this.stringsHe = stringsHe; this.portraits = portraits; this.map = map;
            this.placements = placements; this.pointer = pointer; this.cameraController = cameraController;
            // An enabled GameRoot is already subscribed to the old boot's Ready (OnEnable): move the subscription.
            if (!isActiveAndEnabled) return;
            if (oldBoot != null) { oldBoot.Ready -= Begin; oldBoot.Failed -= OnBootFailed; }
            if (this.boot != null) { this.boot.Ready += Begin; this.boot.Failed += OnBootFailed; }
        }

        void OnEnable() { if (boot != null) { boot.Ready += Begin; boot.Failed += OnBootFailed; } }

        /// <summary>The loading line in the UI's language from the first frame (Start runs after every OnEnable, so the
        /// document's tree exists).</summary>
        void Start() { if (Application.isPlaying) Started(); }

        /// <summary>Start's work: the loading line in the UI's language (ShowLoading), unless the boot has already failed or
        /// the game has already begun. GameBoot and GameRoot set no execution order, and on the desktop the load reads from
        /// disk, so a failure can land inside GameBoot.Start before this runs: its reason must stay (M3 final review I5).</summary>
        internal void Started()
        {
            if ((boot == null || boot.Error == null) && Bridge == null) ShowLoading();
        }

        /// <summary>A boot failure replaces the loading line with the reason; the loading state stays, so no half-built panel
        /// shows (M3 final review I5).</summary>
        internal void OnBootFailed(string reason)
        {
            var text = ShowLoading();
            var line = text == null ? null : document.rootVisualElement.Q<Label>("loading");
            if (line != null) Say(line, text!.Format("boot.failed", GameBoot.ForPlayer(reason).TrimEnd('.')), text["ui.lang"] == UiLanguage.Hebrew);
        }

        /// <summary>Puts the loading line in the UI's language (boot.loading, #193). In Hebrew the root also gets its mark
        /// (UiLanguage.HebrewClass) now, before Begin, so the line and a boot failure are set in the Hebrew face. Returns
        /// the strings, or null before the scene's fields are set.</summary>
        internal UiText? ShowLoading()
        {
            if (document == null || strings == null || stringsHe == null || document.rootVisualElement == null) return null;
            var text = Strings();
            var root = document.rootVisualElement;
            bool hebrew = text["ui.lang"] == UiLanguage.Hebrew;
            (root.Q("root") ?? root).EnableInClassList(UiLanguage.HebrewClass, hebrew);
            var line = root.Q<Label>("loading");
            if (line != null) Say(line, text["boot.loading"], hebrew);
            return text;
        }

        /// <summary>A loading-line text, with its own direction in Hebrew (UiDirection is not running yet) and none in English.</summary>
        static void Say(Label line, string words, bool hebrew)
        {
            line.text = words;
            line.languageDirection = !hebrew ? LanguageDirection.Inherit : UiDirection.RightToLeft(words) ? LanguageDirection.RTL : LanguageDirection.LTR;
        }

        /// <summary>The strings of the UI's language (UiLanguage): strings.he.yaml when Hebrew is asked for, else English.</summary>
        UiText Strings() => (Language ?? UiLanguage.Requested()) == UiLanguage.Hebrew
            ? UiText.Parse(stringsHe.text, "strings.he.yaml") : UiText.Parse(strings.text);

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

        /// <summary>GameBoot.Ready: starts the game, smoke-testing when the URL or command line asks, with the start screen
        /// unless an automated run skips it.</summary>
        public void Begin(GameInputs inputs) => Begin(inputs, SmokeTurns(), Application.isPlaying && !StartScreenPresenter.Skipped());

        /// <summary>Starts (or restarts) the game over the built map. <paramref name="smoke"/> &gt; 0 plays that many turns;
        /// <paramref name="startScreen"/> shows the start screen first (never in a smoke run, which has no New Game).</summary>
        internal void Begin(GameInputs inputs, int smoke, bool startScreen = false)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            foreach (var (value, field) in new (UnityEngine.Object?, string)[] { (document, nameof(document)), (strings, nameof(strings)), (stringsHe, nameof(stringsHe)),
                (map, nameof(map)), (placements, nameof(placements)), (pointer, nameof(pointer)), (cameraController, nameof(cameraController)) })
                if (value == null) throw new InvalidOperationException($"GameRoot: {field} is not assigned (run MapCapture.MakeScene)");
            if (map.Terrain == null) throw new InvalidOperationException("GameRoot: the map is not built (GameBoot builds it before Ready)");
            Shutdown();

            var text = Strings();
            var store = Store ?? SaveStore.ForPlatform();
            var notes = new List<string>();
            // A smoke run is reproducible: a fresh game with the golden seed, never the player's autosave. A slice game (?mvp=1,
            // #380) is fresh too: the autosave was made with other rules, so it is neither read nor set aside.
            bool resumed;
            var session = smoke > 0
                ? GameStart.Begin(inputs.Content, store, () => GoldenRun.Seed, notes, text, out resumed, fresh: true)
                : GameStart.Begin(inputs.Content, store, GameStart.NewSeed, notes, text, out resumed, fresh: inputs.Slice);
            DiagLog.BootPhase($"session started (turn {session.State.Turn}, replayed)");
            var bridge = Bridge = new SimBridge(session, text);
            var root = document.rootVisualElement;
            bool hebrew = text["ui.lang"] == UiLanguage.Hebrew;
            // Hebrew: the Hebrew face, the advanced text generator and the mirrored rows (.lang-he); each text's paragraph
            // direction comes from its own text (UiDirection, made below once the presenters have rendered).
            (root.Q("root") ?? root).EnableInClassList(UiLanguage.HebrewClass, hebrew);   // on .root, whose face it replaces
            var loading = root.Q<Label>("loading");
            if (loading != null) Say(loading, text["boot.loading"], hebrew);   // hidden from here on; set for a restart in the other language

            // The toast first: the autosave (below) must report a failed write after the toast has shown the turn's refusals.
            var toast = Own(new ToastPresenter(root, bridge, text));
            Own(new HudPresenter(root, bridge, text));
            nav = Own(new ScreenNav(root));                                      // every screen starts hidden (USS); ScreenNav opens one at a time
            Own(new PointerBlur(root));                                    // a mouse click leaves no focus fill behind (keyboard keeps it)
            Own(new FocusScroll(root));                                    // keyboard focus scrolls its ScrollView to it (a11y H1)
            Own(new NavigationGuard(root));                                // the camera keys (WASD, arrows) never move UI focus (B6)
            if (DiagLog.Nav) Own(new NavEventLog(root));                   // ?navlog: every UI pointer/submit event (playtest 2, P2-4)
            // The Illustration switch's silent loops (event-videos.yaml, Resources): none in the automated runs (CardVideo.ForcedOff).
            var video = CardVideo.ForcedOff() ? null : Own(new CardVideo());
            var mapping = video == null ? default : EventVideoMapping.LoadBoth();
            var clips = video == null ? null : mapping.clips.ToDictionary(r => r.Event);
            // Event photos: Resources, no scene field, loaded off the boot path (issue #321): in the start screen's idle time,
            // or by the first card if it comes sooner. A Continue with a decision waiting builds the card under the start
            // screen, which asks for the photograph only when the screen closes (issue #592). One value says whether the
            // screen goes up (below) and whether the card waits for it: a card waiting under no screen would never show its photo.
            // A smoke run has no New Game, and a slice game's (?mvp=1, #380) would be dead: neither shows the start screen.
            bool showsStart = startScreen && smoke == 0 && !inputs.Slice;
            EventPhotos.ScheduleOn(root);
            card = Own(new EventCardPresenter(root, bridge, text, portraits, null, video,
                clips == null ? null : id => clips.TryGetValue(id, out var r) ? r : null, EventPhotos.Get,
                underStartScreen: showsStart));
            // The End Turn outcome clips (decision videos, phase 3): none in the automated runs (no video there). Their own
            // player, so the card's loop never takes it; an open screen closes when one starts.
            outcomes = video == null ? null : Own(new OutcomePresenter(root, bridge, text, Own(new CardVideo()), mapping.outcomes,
                () => card?.Render(), () => nav?.Close()));
            Own(new DefencePresenter(root, bridge, text, portraits));
            Own(new BudgetPresenter(root, bridge, text));
            Own(new WhyPresenter(root, bridge, text));
            Own(new CabinetPresenter(root, bridge, text, portraits));
            var names = map.SettlementsFor(inputs.Map.SettlementsYaml).ToDictionary(s => s.Id, s => s.Name);   // the map's parse (issue #321)
            // The 1948 sub-districts, outlined on the map, and each front's 1949 armistice line from its agreement's date
            // where the line exists (display only: the sim never reads them; docs/visual-v1.md, owner rulings 2026-10-11).
            // Since the owner's rulings of 2026-10-14 the areas are coloured by side from 11 June 1948 (control_hexes_1948_49.yaml),
            // with the legend in the label.
            // From their runtime forms when they match the YAML (issue #321: boot time), the sub-districts as the map read them.
            var regions = new MapRegions(map.SubdistrictsFor(inputs.Map.RegionsTsv, inputs.Map.RegionsYaml, Debug.LogWarning),
                string.IsNullOrWhiteSpace(inputs.Map.ArmisticeYaml) ? null : Armistice1949.ReadRuntimeOrYaml(inputs.Map.ArmisticeTsv, inputs.Map.ArmisticeYaml, Debug.LogWarning),
                string.IsNullOrWhiteSpace(inputs.Map.ControlYaml) ? null : Control1948.ReadRuntimeOrYaml(inputs.Map.ControlTsv, inputs.Map.ControlYaml, Debug.LogWarning));
            Regions = regions;
            var layerLabel = root.Q<Label>("map-layer");
            Action onDate = () =>
            {
                regions.Apply(bridge.State.Date);
                layerLabel.text = regions.ControlLabel(bridge.State.Date, k => text[k])
                    ?? regions.Label(bridge.State.Date, text.Format);
                ShowLayerLabel();
            };
            // The caption names the borders while they are drawn: by date, and not while the minimap's button hides them.
            void ShowLayerLabel() => layerLabel.style.display = regions.Shown && minimap?.BordersShown != false ? DisplayStyle.Flex : DisplayStyle.None;
            onDate();
            bridge.Changed += onDate;
            owned.Add(new Undo(() => bridge.Changed -= onDate));
            // The units in play (docs/units.md): drawn from the state and the sourced unit data, before the info panel so
            // the layer has followed a change before the panel renders the selected unit's card.
            DiagLog.BootPhase("screens wired");   // issue #321: the splits of the units layer
            // The map's own parse of the unit file when it read this text (issue #321: it was parsed twice at boot).
            var unitData = map.UnitEntriesFor(inputs.Map.UnitsYaml, Debug.LogWarning);
            DiagLog.BootPhase("units layer data read");
            var unitLayer = units = Statecraft.Game.Map.Units.UnitLayer.Create(map,
                unitData, inputs.Content.Hexes,
                Statecraft.Game.Map.Units.UnitShowcase.StillRequested(Application.absoluteURL, Environment.GetCommandLineArgs(),
                    Application.platform == RuntimePlatform.WebGLPlayer));
            // A screenshot run (?shot) leaves the units out unless it also asks for them (&units), so the art log's plain
            // checkpoints stay comparable across tracks (review I5; tools/art_checkpoint.sh --units).
            if (Statecraft.Game.Map.Units.UnitLayer.HiddenInCaptures(Application.absoluteURL, Environment.GetCommandLineArgs(),
                    Application.platform == RuntimePlatform.WebGLPlayer))
                VisualToggles.Set(Statecraft.Game.Map.Units.UnitLayer.ToggleName, false);
            Action showUnits = () => unitLayer.Show(bridge.Content, bridge.State);
            bridge.Changed += showUnits;
            owned.Add(new Undo(() => bridge.Changed -= showUnits));
            showUnits();
            DiagLog.BootPhase("units layer built");
            var localitiesAt = LocalitiesAt(map);
            var info = Own(new InfoPanelPresenter(root, bridge, text, names, nav: nav,
                selectedUnit: () => unitLayer.Selected is string id ? unitLayer.Tokens.First(p => p.Token.Unit.Id == id) : (Statecraft.Game.Map.Units.PlacedToken?)null,
                regions: regions, localitiesAt: localitiesAt));
            // A smoke or slice session never touches the player's save: no autosave, and no New Game (which deletes the autosave).
            // A smoke run gets no New Game at all: no file is ever deleted, no path has to be right on every platform, and the
            // scripted run never presses it. A slice game (?mvp=1) hides the button and takes it out of the Tab order, and its
            // presenter refuses every press (slice: true), so even a press that reaches it deletes nothing.
            NewGamePresenter? newGame = null;
            var newGameButton = root.Q<Button>("new-game");
            newGameButton.style.display = inputs.Slice ? DisplayStyle.None : StyleKeyword.Null;
            newGameButton.focusable = !inputs.Slice;
            if (smoke == 0)
            {
                newGame = Own(new NewGamePresenter(root, bridge, store, toast, text, GameStart.NewSeed, slice: inputs.Slice));
                if (!inputs.Slice) Own(GameStart.Autosave(bridge, store, text, toast.Append));   // appends: the turn's refusals stay visible
            }

            // An outcome clip holds the pointer while it shows and in the frame it closes (the skipping click selects no hex).
            Func<Vector2, bool> overUi = p => outcomes?.HoldsThePointer == true || UiPointer.IsOverUi(root.panel, p, Screen.height);
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
            Action<int, int> onHex = (q, r) => OnHexClicked(map, unitLayer, info, q, r);
            pointer.HexClicked += onHex;
            var camRig = rig = cameraController.GetComponent<CameraRig>();
            Own(new TopBarInset(root, camRig));                            // the map is framed below the top bar
            var mini = minimap = Own(new MinimapPresenter(root, map.Hexes, text));
            var labelCamera = cameraController.GetComponent<Camera>();
            labels = Own(new MapLabelsPresenter(root, map, labelCamera, null,
                p => MapLabelsPresenter.TipOf(p, n => text.Format("map.today", n), text["map.mixed"], hebrew),
                () =>   // the names keep clear of the unit tokens (issue #197), their badges and flags as this camera will draw them
                {
                    if (unitLayer == null) return null;
                    unitLayer.FaceCamera(labelCamera);
                    return unitLayer.KeepClear;
                }));
            mini.Clicked += (lon, lat) => { camRig.LookAt = map.Frame.ToWorld(lon, lat, 0); cameraController.Settle(); SyncMinimap(); labels?.Refresh(); };   // the click may come after LateUpdate (review M6)
            // The minimap's whole-map button: the camera's own farthest zoom, over the middle of the map (no new rule).
            mini.Overview += () =>
            {
                var (lon, lat) = mini.Centre;
                camRig.LookAt = map.Frame.ToWorld(lon, lat, 0); camRig.Distance = CameraController.MaxDistance;
                cameraController.Settle(); SyncMinimap(); labels?.Refresh();
            };
            mini.BordersChanged += ShowLayerLabel;
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
            // The start screen goes up in the same frame the loading state ends, so the HUD never flashes before it. While it
            // shows, the camera keys are off (keyboard pan would otherwise move the hidden map; plan amendment A4).
            if (showsStart)                                                // newGame is made above whenever smoke == 0
            {
                var cam = cameraController;
                var recording = Own(new DeclarationRecording(root, text, toast.Show));   // never built in automated runs (Skipped)
                start = Own(new StartScreenPresenter(root, newGame!, resumed, shown => { if (cam != null) cam.enabled = !shown; }, recording, text));
                start.Show();
            }
            if (hebrew) Own(new UiDirection(root, bridge));
            root.Q("root")?.RemoveFromClassList("loading");                // ready: the bar, panels and screens may show (P2-1)
            DiagLog.BootPhase("screens ready");

            if (notes.Count > 0) toast.Show(notes);
            Debug.Log(FormattableString.Invariant($"GAME OK {bridge.State.Date:yyyy-MM-dd} turn {bridge.State.Turn} hash {bridge.State.Hash()}"));
            if (smoke > 0)
            {
                if (Application.isPlaying) StartCoroutine(Smoke(root, bridge, card!, smoke, Debug.Log));
                else Debug.LogWarning("SMOKE FAIL the smoke run needs play mode or a player");
            }
            if (Application.isPlaying && PixelErrorProfile.Requested()) StartCoroutine(PixelErrorProfile.Run(map.Terrain));
            // The unit art slice at gameplay03 (a showcase, never in normal play; war mode's units are issue #104). Before the
            // instruments, so the frame profile lists its "unitshowcase" toggle (the profile reads the toggles when it starts).
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

        /// <summary>The 1948 localities in a hex (by the hex their point lies in, as the map picks hexes), for the info panel.</summary>
        internal static Func<int, int, IReadOnlyList<Statecraft.Game.Map.Kits.Localities.Place>> LocalitiesAt(MapScene map)
        {
            var byHex = Statecraft.Game.Map.Kits.Localities.ByHex(map.Localities, p => HexPicker.FromLonLat(p.Settlement.Lon, p.Settlement.Lat, map.Hexes.bounds, map.Hexes.hex_size_km));
            IReadOnlyList<Statecraft.Game.Map.Kits.Localities.Place> none = Array.Empty<Statecraft.Game.Map.Kits.Localities.Place>();
            return (q, r) => byHex.TryGetValue((q, r), out var l) ? l : none;
        }

        T Own<T>(T d) where T : IDisposable { owned.Add(d); return d; }

        /// <summary>A map click: on a unit's hex it selects the unit (the ring and its card; the hex outline is cleared, so
        /// the unit has the promo's one highlight); a second click on it, or a click anywhere else, shows the hex (so a token
        /// never hides the hex's map actions).</summary>
        internal static void OnHexClicked(MapScene map, Statecraft.Game.Map.Units.UnitLayer units, InfoPanelPresenter info, int q, int r)
        {
            if (info.Covered)
            {
                // A screen covers the panel: the click moves neither the ring nor the card (#210), and MapPointer's outline
                // goes back to the hex the panel shows (or away, while it shows a unit).
                if (info.Selected is (int sq, int sr) && !info.ShowsUnit) map.Select(sq, sr); else map.ClearSelection();
                return;
            }
            var id = units.UnitToSelectAt(q, r);
            units.Select(id);
            if (id != null) { map.ClearSelection(); info.ShowUnit(); }
            else info.Show(q, r);
        }

        /// <summary>Disposes every presenter, the autosave and the camera/pointer hooks (newest first). Safe to call twice.</summary>
        void Shutdown()
        {
            StopAllCoroutines();
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            owned.Clear();
            minimap = null; labels = null; nav = null; start = null; card = null; outcomes = null; rig = null; drawn = null; units = null; Regions = null; logCamera = false;
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
        void LateUpdate()
        {
            if (labels == null) return;
            labels.Refresh();
            // Today's name under a 1948 locality's pill: while a mouse (or pen) is over it (owner ruling 2026-10-11), or after a
            // tap on it (#245). A touchscreen has no hover: its last touch position stays after the finger lifts.
            labels.Hover(MapLabelsPresenter.HoverPosition(UnityEngine.InputSystem.Pointer.current));
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) labels.Tap(touch.primaryTouch.position.ReadValue());
        }

        void Update()
        {
            // Escape closes the open screen even when no element has the keyboard focus (ScreenNav also takes the UI
            // Cancel event from a focused element; closing twice is harmless). On the start screen it withdraws an open New
            // Game request (StartScreenPresenter.Escape; idempotent too).
            // With nothing else to close, Escape on the open decision card is Hide (EventCardPresenter.Escape; the card itself
            // takes the Cancel event when it has the focus).
            if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true) { bool done = outcomes?.SkipAll() == true || outcomes?.ClosedThisFrame == true; done |= start?.Escape() == true; done |= nav?.Close() == true; if (!done && document.rootVisualElement.focusController?.focusedElement == null) card?.Escape(); }
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
