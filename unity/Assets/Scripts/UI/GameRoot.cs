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
    /// committed order and turn, and wires the camera and the map pointer to the game panel (UiPointer.IsOverUi), map clicks to the info panel,
    /// the minimap to the camera, and State.Placements with the built city projects to the placement layer. Disabling it disposes all of that.
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
        /// <summary>Maximum time after closing the start screen for the normal-media browser check's first card to become usable.
        /// This proposed budget needs maintainer sign-off (#909).</summary>
        internal const double FirstCardBudgetSeconds = 10;

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
        CaptureDialogPresenter? capture;
        /// <summary>Tests: the capture dialog of the last Begin.</summary>
        internal CaptureDialogPresenter? Capture => capture;
        /// <summary>Tests: the settlement name pills of the last Begin.</summary>
        internal MapLabelsPresenter? Labels => labels;
        string? hoveredCity;   // the city HoverCity lit (#1063)
        OutcomePresenter? outcomes;
        TurnReportPresenter? report;
        EndScreenPresenter? end;
        /// <summary>Set by SwitchLanguage for its Begin: the player had closed the end screen, so the rebuilt one stays closed.</summary>
        bool endClosed;
        FirstTurnGuidePresenter? guide;
        /// <summary>Tests: the first-turn guide of the last Begin.</summary>
        internal FirstTurnGuidePresenter? Guide => guide;
        FeedbackPresenter? feedback;
        SharePresenter? share;
        InfoPanelPresenter? info;
        /// <summary>Tests: the info panel of the last Begin.</summary>
        internal InfoPanelPresenter? Info => info;
        CameraRig? rig;
        List<(string, int, int, DateTime)>? drawn;
        /// <summary>The designated sites on the map (#381), made on first use beside the placements; and their states as drawn.</summary>
        SiteLayer? siteLayer; List<string>? drawnSites;
        /// <summary>Whether the last Begin was a smoke run: the language switch then leaves the UI as it is, so the golden-seed
        /// game never gets the autosave and New Game a normal Begin gives (SwitchLanguage).</summary>
        bool smokeRun;

        public SimBridge? Bridge { get; private set; }
        /// <summary>Where the autosave lives; null (the default) = SaveStore.ForPlatform(). Tests set a temporary folder.</summary>
        internal SaveStore? Store { get; set; }
        /// <summary>The UI's language; null (the default) = UiLanguage.AtStart (a flag, else the choice kept in Settings).
        /// Tests set it, and so does Settings' language switch.</summary>
        internal string? Language { get; set; }
        /// <summary>Where the Settings options are kept; null (the default) = SettingsStore.ForPlatform() in Play mode and
        /// players, none outside Play mode (EditMode tests: the developer's own settings must not leak into a test). Tests set
        /// a temporary folder.</summary>
        internal SettingsStore? Settings { get; set; }
        SettingsStore? SettingsFile() => Settings ?? (Application.isPlaying ? SettingsStore.ForPlatform() : null);

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

        /// <summary>A boot failure replaces the loading line with a short reason in the player's language; the loading state
        /// stays, so no half-built panel shows (M3 final review I5). <paramref name="reason"/> is an exception's English text
        /// (a file path, a content error): it stays in GameBoot.Error and the log, never on screen (#637).</summary>
        internal void OnBootFailed(string reason)
        {
            var text = ShowLoading();
            var line = text == null ? null : document.rootVisualElement.Q<Label>("loading");
            if (line != null) Say(line, text!.Format("boot.failed", text["boot.reason_other"]), text["ui.lang"] == UiLanguage.Hebrew);
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

        /// <summary>The strings of the UI's language (UiLanguage): strings.he.yaml when Hebrew is asked for or chosen, else English.</summary>
        UiText Strings() => (Language ?? UiLanguage.AtStart(SettingsFile()?.Read(SettingsPresenter.LanguageKey))) == UiLanguage.Hebrew
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

        /// <summary>"?smoke-orders" (WebGL) or "-smoke-orders" (desktop): the smoke-orders golden's one-turn run (SmokeOrders, #754)
        /// instead of the smoke turns. A whole query parameter, so the "smoke" flag never matches it, nor it "?smoke-orders-x".</summary>
        internal static bool SmokeOrdersRequested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "smoke-orders") : Array.IndexOf(args, "-smoke-orders") >= 0;

        /// <summary>GameBoot.Ready: starts the game, smoke-testing when the URL or command line asks, with the start screen
        /// unless an automated run skips it.</summary>
        public void Begin(GameInputs inputs)
        {
            bool orders = SmokeOrdersRequested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);
            Begin(inputs, orders ? 1 : SmokeTurns(), Application.isPlaying && !StartScreenPresenter.Skipped(), smokeOrders: orders);
        }

        /// <summary>Starts (or restarts) the game over the built map. <paramref name="smoke"/> &gt; 0 plays that many turns
        /// (with <paramref name="smokeOrders"/>, the smoke-orders run's one turn instead);
        /// <paramref name="startScreen"/> shows the start screen first (never in a smoke run, which has no New Game).
        /// <paramref name="keep"/> (Settings' language switch) goes on with that bridge's game and its queued orders instead
        /// of loading the autosave.</summary>
        internal void Begin(GameInputs inputs, int smoke, bool startScreen = false, SimBridge? keep = null, bool smokeOrders = false)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            foreach (var (value, field) in new (UnityEngine.Object?, string)[] { (document, nameof(document)), (strings, nameof(strings)), (stringsHe, nameof(stringsHe)),
                (map, nameof(map)), (placements, nameof(placements)), (pointer, nameof(pointer)), (cameraController, nameof(cameraController)) })
                if (value == null) throw new InvalidOperationException($"GameRoot: {field} is not assigned (run MapCapture.MakeScene)");
            if (map.Terrain == null) throw new InvalidOperationException("GameRoot: the map is not built (GameBoot builds it before Ready)");
            Shutdown();
            smokeRun = smoke > 0;

            var text = Strings();
            var store = Store ?? SaveStore.ForPlatform();
            var notes = new List<string>();
            // A smoke run is reproducible: a fresh game with the golden seed, never the player's autosave.
            bool resumed = true;
            var session = keep?.Session ?? (smoke > 0
                ? GameStart.Begin(inputs.Content, store, () => GoldenRun.Seed, notes, text, out resumed, fresh: true)
                : GameStart.Begin(inputs.Content, store, GameStart.NewSeed, notes, text, out resumed));
            DiagLog.BootPhase($"session started (turn {session.State.Turn}, replayed)");
            var bridge = Bridge = new SimBridge(session, text);
            if (keep != null) foreach (var order in keep.Queued) bridge.Queue(order);   // in their order: each was within the attention left
            if (Statecraft.HexTiles.HexTilesLook.Requested())   // opt-in hex-tile map (docs/hex-tiles/README.md); presentation only
            {
                Statecraft.HexTiles.HexTilesLook.Apply(map, true);
                owned.Add(new Undo(() => Statecraft.HexTiles.HexTilesLook.Apply(map, false)));
            }
            var root = document.rootVisualElement;
            bool hebrew = text["ui.lang"] == UiLanguage.Hebrew;
            // Hebrew: the Hebrew face, the advanced text generator and the mirrored rows (.lang-he); each text's paragraph
            // direction comes from its own text (UiDirection, made below once the presenters have rendered).
            (root.Q("root") ?? root).EnableInClassList(UiLanguage.HebrewClass, hebrew);   // on .root, whose face it replaces
            var loading = root.Q<Label>("loading");
            if (loading != null) Say(loading, text["boot.loading"], hebrew);   // hidden from here on; set for a restart in the other language

            // The toast first: the autosave (below) must report a failed write after the toast has shown the turn's refusals.
            var toast = Own(new ToastPresenter(root, bridge, text));
            Own(new HudPresenter(root, bridge, text, selectUnit: _ => FocusNextUnit()));
            nav = Own(new ScreenNav(root));                                      // every screen starts hidden (USS); ScreenNav opens one at a time
            Own(new PointerBlur(root));                                    // a mouse click leaves no focus fill behind (keyboard keeps it)
            Own(new FocusScroll(root));                                    // keyboard focus scrolls its ScrollView to it (a11y H1)
            FocusScroll.NoScrollBarIsATabStop(root);                       // no scroll bar takes a Tab stop (#41 K3, widened from the Cabinet's two)
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
            // A smoke run has no New Game, so it shows no start screen. Since the GDD cutover every new game opens on a card
            // (One Army, 15 May 1948), so a new game's card waits under the screen too.
            bool showsStart = startScreen && smoke == 0;
            EventPhotos.ScheduleOn(root);
            card = Own(new EventCardPresenter(root, bridge, text, portraits, null, video,
                clips == null ? null : id => clips.TryGetValue(id, out var r) ? r : null, EventPhotos.Get,
                underStartScreen: showsStart,
                // A screenshot run (?shot) captures the map: the opening card would cover it (UnitLayer.HiddenInCaptures' rule).
                hiddenInCapture: EventCardPresenter.HiddenInCaptures(Application.absoluteURL, Environment.GetCommandLineArgs(),
                    Application.platform == RuntimePlatform.WebGLPlayer)));
            // The sound effects (#39): End Turn, a refused order, a card coming into view; the Settings screen sets the volume.
            // After the card: a card already open when Begin rebuilds the UI (New Game, a language switch) makes no sound.
            var audio = Own(new GameAudio(bridge, card));
            // The End Turn outcome clips (decision videos, phase 3): none in the automated runs (no video there). Their own
            // player, so the card's loop never takes it; an open screen closes when one starts.
            outcomes = video == null ? null : Own(new OutcomePresenter(root, bridge, text, Own(new CardVideo()), mapping.outcomes,
                () => card?.Render(), () => nav?.Close()));
            Own(new DefencePresenter(root, bridge, text, portraits));
            Own(new BudgetPresenter(root, bridge, text));
            var why = Own(new WhyPresenter(root, bridge, text));
            Own(new TimelinePresenter(root, bridge, text));
            Own(new HowToPlayPresenter(root, text));
            Own(new ObjectivesPresenter(root, bridge, text));
            // Presentation only (#40): never the sim or the save. Outside Play mode (EditMode tests) no file: the developer's
            // own settings must not leak into a test. Each Begin (New Game too) reads the file again.
            // The language switch (#1036) builds every screen again in the new language over this game (SwitchLanguage).
            Own(new SettingsPresenter(root, text, SettingsFile(), language => SwitchLanguage(inputs, language), panel: document.panelSettings, audio: audio));
            // "Report a bug or idea" (#1169), from Settings and the start screen: the version line names the content GameBoot logged.
            feedback = Own(new FeedbackPresenter(root, text, ReportLinks.Version(Application.version, inputs.Content.Hash)));
            var feedbackPanel = feedback;
            Own(new CabinetPresenter(root, bridge, text, portraits));
            // After End Turn, what changed in the top bar and the sim's causes (#1037); under an outcome clip, which plays first.
            report = Own(new TurnReportPresenter(root, bridge, text, nav!));
            // "Share" (#1170), from Settings and the turn report: the frame is captured at its end, before the panel shows.
            var sharePanel = share = Own(new SharePresenter(root, bridge, text, done => StartCoroutine(ShareShot.Capture(done))));
            // Star, Report and Share as round icons at the map screen's bottom left (#1236): the same actions as Settings' buttons.
            Own(new CommunityIconsPresenter(root, text, feedbackPanel, sharePanel));
            // Both panels in one hook: the same Escape never closes the screen under the panel too.
            nav!.TakesEscape = () => feedbackPanel.Escape() || sharePanel.Escape();
            var names = map.SettlementsFor(inputs.Map.SettlementsYaml).ToDictionary(s => s.Id, s => s.Name);   // the map's parse (issue #321)
            // The capture dialog (GDD Phase 6 PR 10, #802): in the decision layer, after the card, which goes first; the
            // places' Hebrew names from the same parse.
            capture = Own(new CaptureDialogPresenter(root, bridge, text, card,
                map.SettlementsFor(inputs.Map.SettlementsYaml).ToDictionary(s => s.Id, s => s.NameHe)));
            // The 1948 sub-districts, outlined on the map, and each front's 1949 armistice line from its agreement's date
            // where the line exists (display only: the sim never reads them; docs/visual-v1.md, owner rulings 2026-10-11).
            // Since the owner's rulings of 2026-10-14 the areas are coloured by side (control_hexes_1948_49.yaml), from 15 May 1948
            // since #674 (the 9 July 1948 lines, adjusted),
            // with the legend in the label.
            // From their runtime forms when they match the YAML (issue #321: boot time), the sub-districts as the map read them.
            var regions = new MapRegions(map.SubdistrictsFor(inputs.Map.RegionsTsv, inputs.Map.RegionsYaml, Debug.LogWarning),
                string.IsNullOrWhiteSpace(inputs.Map.ArmisticeYaml) ? null : Armistice1949.ReadRuntimeOrYaml(inputs.Map.ArmisticeTsv, inputs.Map.ArmisticeYaml, Debug.LogWarning),
                map.Control);   // the map's read, with the map editor's control overrides (#1009); the road vehicles use it too (#725)
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
            why.Units = unitData;
            report.Units = unitData;   // the turn report names the enemy's units by these rows (#1293)
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
            Action showUnits = () =>
            {
                unitLayer.Show(bridge.Content, bridge.State);
                unitLayer.Dim(unitLayer.Tokens.Where(t => t.Token.Unit.Side == Statecraft.Game.Map.Units.UnitData.Israel)
                    .Where(t => UnitCard.SimUnit(bridge.Content, bridge.State, t.Token) is int id
                        && Movement.PointsLeft(bridge.Content, bridge.State, id, bridge.Spent) == 0)
                    .Select(t => t.Token.Unit.Id).ToList());
            };
            bridge.Changed += showUnits;
            owned.Add(new Undo(() => bridge.Changed -= showUnits));
            showUnits();
            DiagLog.BootPhase("units layer built");
            var localitiesAt = LocalitiesAt(map);
            var townNamesHe = map.SettlementsFor(inputs.Map.SettlementsYaml).ToDictionary(s => s.Id, s => s.NameHe);
            var info = this.info = Own(new InfoPanelPresenter(root, bridge, text, names, nav: nav,
                selectedUnit: () => unitLayer.Selected is string id ? unitLayer.Tokens.First(p => p.Token.Unit.Id == id) : (Statecraft.Game.Map.Units.PlacedToken?)null,
                regions: regions, localitiesAt: localitiesAt,
                townHex: InfoPanelPresenter.TownHex(map.SettlementsFor(inputs.Map.SettlementsYaml), map.Hexes),
                townNamesHe: townNamesHe,
                showReachable: map.ShowReachable,
                unitName: id => unitLayer.Tokens.Where(p => UnitCard.SimUnit(bridge.Content, bridge.State, p.Token) == id)
                    .Select(p => UnitCard.For(p, bridge.State, text, bridge.Content.IsGdd, bridge.Content, townNamesHe).Title).FirstOrDefault(),
                unitDone: FocusNextUnit, showRoute: map.ShowRoute));
            toast.Units = unitData; toast.TownNamesHe = townNamesHe; why.TownNamesHe = townNamesHe;
            // A smoke session never touches the player's save: no autosave, and no New Game (which deletes the autosave).
            // Leaving New Game out is safer than handing it a throwaway store: no file is ever deleted, no path has to be
            // right on every platform, and the scripted run never presses it.
            NewGamePresenter? newGame = null;
            if (smoke == 0)
            {
                newGame = Own(new NewGamePresenter(root, bridge, store, toast, text, GameStart.NewSeed));
                Own(GameStart.Autosave(bridge, store, text, toast.Append));   // appends: the turn's refusals stay visible
            }
            // Once the game is over, the sim's end report (#812); after the autosave, so the last turn is saved first. New Game
            // is its two presses; a smoke run has none.
            end = Own(new EndScreenPresenter(root, bridge, text, newGame == null ? null : newGame.Press, show: !endClosed));
            endClosed = false;

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
            Action<int, int> onHex = (q, r) => OnHexClicked(map, unitLayer, info, q, r,
                UnityEngine.InputSystem.Pointer.current is UnityEngine.InputSystem.Touchscreen);
            pointer.HexClicked += onHex;
            // #1063: a click on a city's name pill is a click on the city's hex (its settlement hex, where the city panel opens).
            Func<Vector2, (int q, int r)?> pillHex = p =>
            {
                if (labels?.PillAt(p) is not string id || !bridge.State.Cities.ContainsKey(id)) return null;
                foreach (var x in bridge.Content.SettlementHexes) if (x.Id == id) return (x.Q, x.R);
                return null;
            };
            pointer.PillHex = pillHex;
            var camRig = rig = cameraController.GetComponent<CameraRig>();
            Own(new TopBarInset(root, camRig));                            // the map is framed below the top bar
            Own(new SafeAreaFit(root));                                    // the HUD keeps clear of the phone's notch
            // the map's keyboard stop (#41 B1b): Enter or Space picks the hex under the look-at point, as a click there would
            Own(new MapFocusPresenter(root, cameraController.GetComponent<Camera>(), camRig, at => pointer.PickAt(at)));
            var mini = minimap = Own(new MinimapPresenter(root, map.Hexes, text));
            var labelCamera = cameraController.GetComponent<Camera>();
            labels = Own(new MapLabelsPresenter(root, map, labelCamera, null,
                p => MapLabelsPresenter.TipOf(p, n => text.Format("map.today", n), text["map.mixed"], hebrew),
                () =>   // the names keep clear of the unit tokens (issue #197), their badges and flags as this camera will draw them,
                {       // and of the 1948 Collection's ships and C-54 (#669)
                    IReadOnlyList<Bounds[]>? tokens = null;
                    if (unitLayer != null) { unitLayer.FaceCamera(labelCamera); tokens = unitLayer.KeepClear; }
                    var craft = seaAir != null ? seaAir.KeepClear : null;
                    if (craft == null || craft.Count == 0) return tokens;
                    if (tokens == null || tokens.Count == 0) return craft;
                    // One merged list per pair of lists, so the labels see a new list only when either changes (they refresh then).
                    if (!ReferenceEquals(tokens, keptTokens) || !ReferenceEquals(craft, keptCraft))
                    {
                        keptTokens = tokens; keptCraft = craft;
                        var merged = new List<Bounds[]>(tokens); merged.AddRange(craft); keptBoth = merged;
                    }
                    return keptBoth;
                },
                // Issue #710: the static 1948 tier from cities.yaml's people, not the sim's live count (owner, 15 October 2026).
                bridge.Content.Cities.Where(c => c.People != null).ToDictionary(c => c.Id, c => c.People!.Value),
                (tier, people) => text.Format("map.tier", tier, MapLabelsPresenter.TierCount, people.ToString("N0", CultureInfo.InvariantCulture)),
                hebrew, bridge.Content.MapLocations));   // #1008: the GDD v0.2 map locations are named too
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
                if (pointer.PillHex == pillHex) pointer.PillHex = null;
                if (pointer.IsOverUi == overUi) pointer.IsOverUi = null;
                if (cameraController.IsOverUi == overUi) cameraController.IsOverUi = null;
                if (pointer.Map == map) pointer.Map = null;
                cameraController.Bounds = oldBounds;
                cameraController.Ground = oldGround; cameraController.GroundTop = oldTop;
            }));
            drawn = null; drawnSites = null;
            RenderPlacementsIfChanged();
            SyncMinimap();
            // The first-turn guide (#42) opens when the start screen closes on a new game (not on Continue). It is made after
            // the screen, so Shutdown disposes it first and the screen's closing then opens nothing.
            FirstTurnGuidePresenter? firstTurn = null;
            // The start screen goes up in the same frame the loading state ends, so the HUD never flashes before it. While it
            // shows, the camera keys are off (keyboard pan would otherwise move the hidden map; plan amendment A4).
            if (showsStart)                                                // newGame is made above whenever smoke == 0
            {
                var cam = cameraController;
                var recording = Own(new DeclarationRecording(root, text, toast.Show));   // never built in automated runs (Skipped)
                start = Own(new StartScreenPresenter(root, newGame!, resumed, shown =>
                {
                    if (cam != null) cam.enabled = !shown;
                    if (!shown && start?.Resumed == false) firstTurn?.NewGame();
                }, recording, text));
                start.Show();
            }
            // It shows by itself only where the start screen does (never in the automated runs, nor under ?firstcard, which
            // times the first card) and a settings file remembers that it has; How to play opens it again anywhere. Steps 2
            // and 3 show its city as a click there would, without selecting a unit on the city's hex; step 4 shows a unit
            // that can move, its ring and its card, as a click on it would (OnHexClicked), and centres the camera on its hex.
            Action showCity = () =>
            {
                if (FirstTurnGuidePresenter.CityHex(bridge.Content, bridge.State) is not (int q, int r)) return;
                unitLayer.Select(null); map.Select(q, r); info.Show(q, r);
            };
            Action showUnit = () =>
            {
                if (FirstTurnGuidePresenter.UnitToShow(bridge.Content, bridge.State, unitLayer.Tokens.Select(t => t.Token)) is not string id) return;
                camRig.Distance = Mathf.Min(camRig.Distance, FirstTurnGuidePresenter.MoveStepDistanceM);
                FocusUnit(id);
            };
            // When it closes, the guide puts back the selection it found (a hex, a unit, or none), unless the player has
            // picked something else meanwhile (the HUD stays usable under it).
            Func<Action> keepSelection = () =>
            {
                var hex = info.ShowsUnit ? null : info.Selected;
                var unit = info.ShowsUnit ? unitLayer.Selected : null;
                var city = FirstTurnGuidePresenter.CityHex(bridge.Content, bridge.State);
                var guideUnit = FirstTurnGuidePresenter.UnitToShow(bridge.Content, bridge.State, unitLayer.Tokens.Select(t => t.Token));
                return () =>
                {
                    // Under a screen, or the player picked another than the guide's city or unit: kept.
                    if (info.Covered || (info.ShowsUnit ? unitLayer.Selected != guideUnit : info.Selected != city)) return;
                    unitLayer.Select(unit);
                    if (unitLayer.Selected != null) { map.ClearSelection(); info.ShowUnit(); }
                    else if (hex is (int q, int r)) { map.Select(q, r); info.Show(q, r); }
                    else { map.ClearSelection(); info.Clear(); }
                };
            };
            guide = firstTurn = Own(new FirstTurnGuidePresenter(root, text, showsStart && !FirstCardCheckRequested() ? SettingsFile() : null,
                card, showCity, () => nav?.Close(), bridge, keepSelection, showUnit));
            if (newGame != null) newGame.Started += firstTurn.NewGame;      // New Game in the top bar too; both die with this Begin
            if (hebrew) Own(new UiDirection(root, bridge));
            root.Q("root")?.RemoveFromClassList("loading");                // ready: the bar, panels and screens may show (P2-1)
            DiagLog.BootPhase("screens ready");

            if (notes.Count > 0) toast.Show(notes);
            Debug.Log(FormattableString.Invariant($"GAME OK {bridge.State.Date:yyyy-MM-dd} turn {bridge.State.Turn} hash {bridge.State.Hash()}"));
            // Unlike the other browser instruments, ?firstcard keeps the normal start screen, clip mapping and video players.
            // It closes the screen through its real button immediately, then gives tools/web_check.py a bounded result to
            // wait for. Console exceptions are collected by that browser-side check (#910).
            if (Application.isPlaying && FirstCardCheckRequested())
                StartCoroutine(FirstCardCheck(root, card!, Debug.Log));
            if (smoke > 0)
            {
                if (Application.isPlaying) StartCoroutine(smokeOrders ? SmokeOrders(root, bridge, unitLayer, onHex, Debug.Log) : Smoke(root, bridge, card!, smoke, Debug.Log));
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

        /// <summary>Settings' language switch (#1036): every screen again in <paramref name="language"/> at once, no reload,
        /// over the same game with this turn's orders (Begin's keep), and Settings open again with the focus on the choice
        /// just made (for a mouse press too: the rebuilt UI has no focus to keep). Not in a smoke run (smokeRun).</summary>
        void SwitchLanguage(GameInputs inputs, string language)
        {
            if (Bridge == null || smokeRun) return;
            Language = language;
            endClosed = end != null && !end.IsOpen;   // read before Begin disposes it
            Begin(inputs, 0, keep: Bridge);
            nav?.Open("settings");
            document.rootVisualElement.Q<Button>("settings-language-" + language)?.Focus();
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
        /// never hides the hex's map actions). While the card's Move or Attack pick is open (#754), a target click gives the
        /// order and keeps the unit's card; any other click ends the pick first and then selects as usual.</summary>
        internal static void OnHexClicked(MapScene map, Statecraft.Game.Map.Units.UnitLayer units, InfoPanelPresenter info, int q, int r, bool touch = false)
        {
            if (info.Covered)
            {
                // A screen covers the panel: the click moves neither the ring nor the card (#210), and MapPointer's outline
                // goes back to the hex the panel shows (or away, while it shows a unit).
                if (info.Selected is (int sq, int sr) && !info.ShowsUnit) map.Select(sq, sr); else map.ClearSelection();
                return;
            }
            if (info.PickTarget(q, r, touch)) { map.ClearSelection(); return; }   // the unit keeps its ring, the one highlight
            var id = units.UnitToSelectAt(q, r);
            units.Select(id);
            if (id != null) { map.ClearSelection(); info.ShowUnit(); }
            else info.Show(q, r);
        }

        /// <summary>Selects a drawn unit row, opens its card and centres the camera on its current hex.</summary>
        internal void FocusUnit(string rowId)
        {
            if (units == null || Bridge == null || info == null || rig == null) return;
            units.Select(rowId);
            if (units.Selected == null) return;
            map.ClearSelection(); info.ShowUnit();
            var token = units.Tokens.First(t => t.Token.Unit.Id == rowId);
            var hex = Bridge.Content.HexAt(token.Q, token.R);
            if (hex == null) return;
            rig.LookAt = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
            cameraController.Settle(); SyncMinimap(); labels?.Refresh();
        }

        void SelectUnit(int simId)
        {
            if (units == null || Bridge == null) return;
            var row = units.Tokens.FirstOrDefault(t => UnitCard.SimUnit(Bridge.Content, Bridge.State, t.Token) == simId).Token?.Unit.Id;
            if (row != null) FocusUnit(row);
        }

        void FocusNextUnit()
        {
            if (Bridge == null || units == null || Bridge.NeedsOrders.Count == 0) return;
            // An open event card is modal and disables the unit card (ScreenNav.DisableCovered), so the unit End turn selects
            // could take no order: the card waits behind "Decision waiting", as its Hide button does.
            card?.Collapse(takeFocus: false);
            int? current = units.Selected is string row
                ? units.Tokens.Where(t => t.Token.Unit.Id == row).Select(t => UnitCard.SimUnit(Bridge.Content, Bridge.State, t.Token)).FirstOrDefault()
                : null;
            var next = current is int id
                ? Bridge.NeedsOrders.Where(n => n > id).Select(n => (int?)n).FirstOrDefault() ?? Bridge.NeedsOrders[0]
                : Bridge.NeedsOrders[0];
            SelectUnit(next);
        }

        /// <summary>#1063 (player report: a city is hard to tell from the land round it and hard to click): the city under a
        /// hovering pointer, on its hexes or its name pill, lights its pill and turns the browser's pointer into a hand, so the
        /// player sees it can be clicked. Null (and nothing lit) over the UI, off the map, off every city, or with no hovering
        /// pointer (a touchscreen). Returns that city's id; called each frame (LateUpdate), it changes the UI only when the city does.</summary>
        internal string? HoverCity(Vector2? screenYUp)
        {
            var city = screenYUp is Vector2 p && Bridge != null && pointer.HexAt(p) is (int q, int r) ? CityAt(Bridge.Content, Bridge.State, q, r) : null;
            if (city == hoveredCity) return city;
            hoveredCity = city;
            labels?.Highlight(city);
            HandCursor(city != null);
            return city;
        }

        /// <summary>The city with a settlement hex at (q, r): one in State.Cities, whose hex opens the city panel (GddHexPanel); or null.</summary>
        internal static string? CityAt(Content c, GameState s, int q, int r)
        {
            foreach (var x in c.SettlementHexes) if (x.Q == q && x.R == r && s.Cities.ContainsKey(x.Id)) return x.Id;
            return null;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void StatecraftHandCursor(int on);   // HandCursor.jslib
        static void HandCursor(bool on) => StatecraftHandCursor(on ? 1 : 0);
#else
        static void HandCursor(bool on) { }   // the game ships as the browser build; the editor keeps its own cursor
#endif

        /// <summary>Disposes every presenter, the autosave and the camera/pointer hooks (newest first). Safe to call twice.</summary>
        void Shutdown()
        {
            StopAllCoroutines();
            // The card first: disposing the start screen uncovers the decision layer, and a card still waiting for it would
            // then render and load the photo catalog for a game that is going away (#592; since the GDD cutover a new game's
            // card waits under the screen, so New Game's Begin would load it).
            if (card != null) { owned.Remove(card); card.Dispose(); }
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            owned.Clear();
            if (hoveredCity != null) { hoveredCity = null; HandCursor(false); }   // the next Begin's pills start unlit
            minimap = null; labels = null; nav = null; start = null; card = null; outcomes = null; report = null; end = null; guide = null; feedback = null; share = null; info = null; rig = null; drawn = null; drawnSites = null; units = null; Regions = null; logCamera = false;
            // A ?profile run cut short (a stopped coroutine never reaches its restore line) must not leave the terrain at the
            // profile's pixel error (Task 24 review M1). Nothing else changes it, so restoring is always right.
            if (map != null && map.Terrain != null) map.Terrain.heightmapPixelError = MapBuilder.TerrainPixelError;
        }

        /// <summary>Draws the placements only when State.Placements with the built city projects (PlacementLayer.Drawn, #1123)
        /// differs from what was drawn (every order and every turn raises Changed; rebuilding the markers each time would be
        /// wasted work). A project finishing redraws though State.Placements did not change.</summary>
        Statecraft.Game.Map.Collection.SeaAirCollection? seaAir;   // the 1948 Collection's craft (#669), for the labels to keep clear of
        IReadOnlyList<Bounds[]>? keptTokens, keptCraft, keptBoth;

        void RenderPlacementsIfChanged()
        {
            if (Bridge == null) return;
            // Dated routes (the Burma Road from June 1948); in the GDD loop the corridors by their built segments (#792).
            Statecraft.Game.Map.Routes.RouteLayer.ShowAt(map, Bridge.State.Date,
                Bridge.Content.IsGdd ? Statecraft.Game.Map.Routes.RouteLayer.BuiltSegments(Bridge.Content, Bridge.State) : null);
            // Tal Heuberger's whole-town models on their towns far off (#663, "Whole model far, buildings near"), streamed after the first frame; the ma'abara from its date.
            // The camps and housing built as city projects bring theirs too, as the placement layer draws them (#1275).
            Statecraft.Game.Map.Collection.TownLandmarks.ShowAt(map, Bridge.State.Date,
                PlacementLayer.Drawn(Bridge.State.Placements, Bridge.State.CityProjects.Values), Bridge.Content);
            // Tal Heuberger's ibex and striped hyena, sparse in their sourced terrain zones and visible only at close zoom (#670).
            Statecraft.Game.Map.Collection.WildlifeLayer.Show(map);
            // Tal Heuberger's civilian figures at towns, villages, transit camps and building sites, ambience only and streamed
            // after the first frame (#666, #708, #886): the residents follow the settlement state, the builders the projects.
            Statecraft.Game.Map.Collection.CivilianPeople.ShowAt(map, Bridge.State.Date, Bridge.State.Placements, Bridge.Content,
                Bridge.State.Settlements, Bridge.State.CityProjects.Values);
            // Tal Heuberger's period-generic farm and village scenery, streamed after the first frame (#667); never game units.
            Statecraft.Game.Map.Collection.RuralLife.Show(map);
            // Tal Heuberger's Tegart fort, barracks and factory at their sourced places and dates, close up only, streamed (#707).
            Statecraft.Game.Map.Collection.SpecialBuildingModels.ShowAt(map, Bridge.State.Date);
            // Tal Heuberger's ships off Haifa, Jaffa, Acre and (on the Altalena's turn) Tel Aviv, and the C-54 at Lod, streamed after the first frame (#669).
            seaAir = Statecraft.Game.Map.Collection.SeaAirCollection.ShowAt(map, Bridge.State.Date, Bridge.State.Placements, Bridge.Content);
            RenderSitesIfChanged();
            var shown = PlacementLayer.Drawn(Bridge.State.Placements, Bridge.State.CityProjects.Values);
            var now = shown.Select(p => (p.Action, p.Q, p.R, p.Date)).ToList();
            if (drawn != null && drawn.SequenceEqual(now)) return;
            drawn = now;
            placements.Render(shown, Bridge.Content, map);
        }

        /// <summary>Draws the designated sites (#381) only when a site's state differs from what was drawn: being built, or
        /// built on its date.</summary>
        void RenderSitesIfChanged()
        {
            if (Bridge == null) return;
            var now = Bridge.State.Sites.Values.Select(w => w.Site + ":" + (w.BuiltOn?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "building")).ToList();
            if (drawnSites != null && drawnSites.SequenceEqual(now)) return;
            drawnSites = now;
            if (siteLayer == null)
            {
                siteLayer = new GameObject("Sites").AddComponent<SiteLayer>();
                siteLayer.transform.SetParent(placements.transform.parent, false);
            }
            siteLayer.Render(Bridge.State.Sites.Values, Bridge.Content, map);
            // A built site's name is a map pill like a town's (#381), against its kit's columns.
            labels?.SetSites(siteLayer.Places.Select(p => (p, siteLayer.Columns.TryGetValue(p.Id, out var c) ? c : (Bounds[]?)null)).ToList());
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

        /// <summary>"firstcard" asks a player build to exercise the first card through the normal media path. It deliberately
        /// is not one of StartScreenPresenter.Skipped or CardVideo.ForcedOff's automation flags.</summary>
        internal static bool FirstCardCheckRequested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "firstcard") : Array.IndexOf(args, "-firstcard") >= 0;

        internal static bool FirstCardCheckRequested() =>
            FirstCardCheckRequested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        /// <summary>Closes a fresh game's start screen at once through New Game and waits at most
        /// <see cref="FirstCardBudgetSeconds"/> seconds for an enabled option plus the archival photograph or documented
        /// minister-portrait fallback. The browser check treats FIRST CARD FAIL and console exceptions as failures.</summary>
        internal static IEnumerator FirstCardCheck(VisualElement root, EventCardPresenter card, Action<string> log,
            Func<double>? now = null)
        {
            now ??= () => Time.realtimeSinceStartupAsDouble;
            var began = now();
            var hud = root.Q("root") ?? root;
            if (!hud.ClassListContains(StartScreenPresenter.OpenClass))
            {
                log("FIRST CARD FAIL start screen is not open");
                yield break;
            }
            if (!Press(root.Q<Button>("start-new-game"), s => log(s.Replace("SMOKE FAIL", "FIRST CARD FAIL")))) yield break;
            while (now() - began <= FirstCardBudgetSeconds)
            {
                var option = root.Q<VisualElement>("event-options")?.Q<Button>();
                bool photo = root.Q<VisualElement>("event-photo")?.style.display.value == DisplayStyle.Flex;
                bool portrait = root.Q<VisualElement>("event-image")?.style.display.value == DisplayStyle.Flex;
                if (!hud.ClassListContains(StartScreenPresenter.OpenClass) && card.Shown != null && !card.Collapsed &&
                    !card.PhotoWaits && option != null && option.enabledInHierarchy && (photo || portrait))
                {
                    var wait = (now() - began) * 1000;
                    log(FormattableString.Invariant($"FIRST CARD OK event={card.Shown.Id} media={(photo ? "photo" : "portrait fallback")} wait={wait:0} ms budget={FirstCardBudgetSeconds * 1000:0} ms"));
                    yield break;
                }
                yield return null;
            }
            log(FormattableString.Invariant($"FIRST CARD FAIL not interactive after {FirstCardBudgetSeconds * 1000:0} ms"));
        }

        /// <summary>After every Update (CameraController moves the camera in its Update), so the pills never trail the camera.</summary>
        void LateUpdate()
        {
            if (labels == null) return;
            labels.Refresh();
            // Today's name under a 1948 locality's pill: while a mouse (or pen) is over it (owner ruling 2026-10-11), or after a
            // tap on it (#245). A touchscreen has no hover: its last touch position stays after the finger lifts.
            var hover = MapLabelsPresenter.HoverPosition(UnityEngine.InputSystem.Pointer.current);
            labels.Hover(hover);
            HoverCity(hover);
            if (hover is Vector2 at && pointer.HexAt(at) is (int q, int r)) info?.ShowRouteTo(q, r);
            else if (UnityEngine.InputSystem.Pointer.current is UnityEngine.InputSystem.Mouse) info?.HideRoute();
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) labels.Tap(touch.primaryTouch.position.ReadValue());
        }

        void Update()
        {
            // Escape closes the open screen even when no element has the keyboard focus (ScreenNav also takes the UI
            // Cancel event from a focused element; closing twice is harmless). On the start screen it withdraws an open New
            // Game request (StartScreenPresenter.Escape; idempotent too).
            // With no screen to close, Escape skips the first-turn guide (FirstTurnGuidePresenter.Escape).
            // With nothing else to close, Escape on the open decision card is Hide (EventCardPresenter.Escape; the card itself
            // takes the Cancel event when it has the focus), and so on the open capture dialog (CaptureDialogPresenter.Escape),
            // which shows only while the card does not.
            // The report panel (#1169) and the share panel (#1170) are over everything: their Escape goes no further.
            if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true && feedback?.Escape() != true && share?.Escape() != true) { bool done = outcomes?.SkipAll() == true || outcomes?.ClosedThisFrame == true; if (!done) done = end?.Close() == true || end?.ClosedThisFrame == true; if (!done) done = report?.Close() == true || report?.ClosedThisFrame == true; done |= start?.Escape() == true; done |= nav?.Close() == true; if (!done) done = guide?.Escape() == true; if (!done && document.rootVisualElement.focusController?.focusedElement == null && capture?.Escape() != true) card?.Escape(); }
            // PageUp, PageDown, Home and End, with no modifier, scroll the open screen or the open card's text, with or without
            // focus (KeyScroll; accessibility audit H3), but not under an End Turn outcome clip, which covers them. The arrows
            // stay with the camera (NavigationGuard, B6).
            var keys = UnityEngine.InputSystem.Keyboard.current;
            if (keys != null && outcomes?.Showing == null && report?.IsOpen != true && end?.IsOpen != true && !(keys.ctrlKey.isPressed || keys.altKey.isPressed || keys.shiftKey.isPressed || keys.leftMetaKey.isPressed || keys.rightMetaKey.isPressed))
            {
                KeyScroll.Key? page = keys.pageUpKey.wasPressedThisFrame ? KeyScroll.Key.PageUp : keys.pageDownKey.wasPressedThisFrame ? KeyScroll.Key.PageDown
                    : keys.homeKey.wasPressedThisFrame ? KeyScroll.Key.Home : keys.endKey.wasPressedThisFrame ? KeyScroll.Key.End : (KeyScroll.Key?)null;
                if (page != null) KeyScroll.Press(document.rootVisualElement, nav?.Current, card?.Shown != null && !card.Collapsed, page.Value);
            }
            SyncMinimap();
            if (!logCamera || rig == null || (rig.Distance == loggedDistance && rig.Yaw == loggedYaw)) return;
            loggedDistance = rig.Distance; loggedYaw = rig.Yaw;
            Debug.Log(CameraLine(rig.Distance, rig.Yaw));
        }

        /// <summary>Plays turns the way a player does: answers each open event by pressing the card's first option button
        /// while the turn's Attention pays for an answer, then presses End Turn and closes the turn report it brings (#1037); a card left unanswered goes to its minister
        /// (since the GDD cutover, 15 July 1948 brings three cards at full war, where an answer costs 2 of the 4). Logs "SMOKE turn &lt;n&gt; &lt;date&gt; refused &lt;k&gt;" per turn through <paramref name="log"/>,
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
                    if (bridge.AttentionLeft + 1e-9 < AttentionSystem.Cost(bridge.Content, bridge.State, new ChooseEventOption(shown.Id, "").Kind)) break;
                    var option = root.Q<VisualElement>("event-options")?.Q<Button>();
                    if (option == null || guard >= 50) { log(FormattableString.Invariant($"SMOKE FAIL event {shown.Id} has no option to press")); yield break; }
                    if (!Press(option, log)) yield break;
                    if (card.Shown == shown) { log(FormattableString.Invariant($"SMOKE FAIL the option '{option.text}' for event {shown.Id} was not taken")); yield break; }
                    yield return null;
                }
                var turn = bridge.State.Turn;
                for (int guard = 0; bridge.NeedsOrders.Count > 0; guard++)
                {
                    if (guard >= 200) { log("SMOKE FAIL unit skip loop reached 200"); yield break; }
                    if (!Press(root.Q<Button>("end-turn"), log)) yield break;
                    if (!Press(root.Q<Button>("info-skip"), log)) yield break;
                    yield return null;
                }
                int refused = 0;
                Action<IReadOnlyList<Rejection>> count = r => refused = r.Count;
                bridge.TurnEnded += count;
                bool pressed;
                try { pressed = Press(root.Q<Button>("end-turn"), log); }
                finally { bridge.TurnEnded -= count; }
                if (!pressed) yield break;
                if (bridge.State.Turn != turn + 1) { log(FormattableString.Invariant($"SMOKE FAIL End Turn did not end turn {turn}")); yield break; }
                // The turn report (#1037) covers the HUD after End Turn: a player closes it before the next turn.
                var hud = root.Q("root") ?? root;
                if (hud.ClassListContains(TurnReportPresenter.OpenClass))
                {
                    if (!Press(root.Q<Button>("turn-report-close"), log)) yield break;
                    if (hud.ClassListContains(TurnReportPresenter.OpenClass)) { log(FormattableString.Invariant($"SMOKE FAIL the turn report after turn {turn} did not close")); yield break; }
                }
                var s = bridge.State;
                log(FormattableString.Invariant($"SMOKE turn {s.Turn} {s.Date:yyyy-MM-dd} refused {refused}"));
            }
            log(FormattableString.Invariant($"SMOKE OK {turns} turns {bridge.TurnStart.Date:yyyy-MM-dd} {bridge.TurnStart.Hash()}"));
        }

        /// <summary>The smoke-orders golden (#754, docs/determinism.md): turn 1 with one unit order given as a player gives it.
        /// An open event card takes the panels under it out of reach, so it first presses the card's Hide (the turn's events
        /// go to their ministers). It clicks the hex of the unit GoldenRun.SmokeMove names (<paramref name="clickHex"/>: what
        /// a map click raises), presses the unit card's Move button, clicks the order's lit hex, then presses End Turn and
        /// closes the turn report. Logs "SMOKE ORDERS move unit &lt;id&gt; to &lt;q&gt;,&lt;r&gt;", then
        /// "SMOKE ORDERS OK &lt;date&gt; &lt;hash&gt;", or "SMOKE FAIL &lt;reason&gt;" when a click or press does not take effect.
        /// Static, so an edit-mode test can drive it.</summary>
        public static IEnumerator SmokeOrders(VisualElement root, SimBridge bridge, Statecraft.Game.Map.Units.UnitLayer units,
            Action<int, int> clickHex, Action<string> log)
        {
            yield return null;
            var hud = root.Q("root") ?? root;
            if (hud.ClassListContains(EventCardPresenter.OpenClass))
            {
                if (!Press(root.Q<Button>("event-hide"), log)) yield break;
                if (hud.ClassListContains(EventCardPresenter.OpenClass)) { log("SMOKE FAIL the event card did not hide"); yield break; }
            }
            var move = GoldenRun.SmokeMove(bridge.Content, bridge.State);
            var token = units.Tokens.Where(p => UnitCard.SimUnit(bridge.Content, bridge.State, p.Token) == move.Unit)
                .Select(p => (Statecraft.Game.Map.Units.PlacedToken?)p).FirstOrDefault();
            if (!(token is { } t)) { log(FormattableString.Invariant($"SMOKE FAIL unit {move.Unit} has no token on the map")); yield break; }
            log(FormattableString.Invariant($"SMOKE ORDERS move unit {move.Unit} to {move.Q},{move.R}"));
            clickHex(t.Q, t.R);
            if (units.Selected != t.Token.Unit.Id) { log(FormattableString.Invariant($"SMOKE FAIL the click on {t.Q},{t.R} did not select unit {move.Unit}")); yield break; }
            if (!Press(root.Q<Button>("info-move"), log)) yield break;
            clickHex(move.Q, move.R);
            if (!bridge.Session.Committed.OfType<Move>().Any(m => m.Unit == move.Unit && m.Q == move.Q && m.R == move.R))
            { log(FormattableString.Invariant($"SMOKE FAIL the click on {move.Q},{move.R} committed no Move for unit {move.Unit}")); yield break; }
            yield return null;
            var turn = bridge.State.Turn;
            for (int guard = 0; bridge.NeedsOrders.Count > 0; guard++)
            {
                if (guard >= 200) { log("SMOKE FAIL unit skip loop reached 200"); yield break; }
                if (!Press(root.Q<Button>("end-turn"), log)) yield break;
                if (!Press(root.Q<Button>("info-skip"), log)) yield break;
                yield return null;
            }
            if (!Press(root.Q<Button>("end-turn"), log)) yield break;
            if (bridge.State.Turn != turn + 1) { log(FormattableString.Invariant($"SMOKE FAIL End Turn did not end turn {turn}")); yield break; }
            if (hud.ClassListContains(TurnReportPresenter.OpenClass))
            {
                if (!Press(root.Q<Button>("turn-report-close"), log)) yield break;
                if (hud.ClassListContains(TurnReportPresenter.OpenClass)) { log(FormattableString.Invariant($"SMOKE FAIL the turn report after turn {turn} did not close")); yield break; }
            }
            log(FormattableString.Invariant($"SMOKE ORDERS OK {bridge.TurnStart.Date:yyyy-MM-dd} {bridge.TurnStart.Hash()}"));
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
