#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The info panel (bottom-left): the selected hex's land, height and the settlements it owns, what the player
    /// built there, and the two map actions (transit camp, housing project) with the reason when one is not possible
    /// (MapActions.Check). Tile yields are deliberately not shown (owner decision, M3 rulings 2). A selected unit
    /// (<see cref="ShowUnit"/>) shows its card instead (<see cref="UnitCard"/>), with a button to the Defence screen (owner
    /// ruling, 2026-10-11) and, in the GDD loop, the Move, Fortify, Hold and Attack orders (GDD Phase 5 PR 15, #754),
    /// and Send home after the unit's front signs its armistice (#1309).</summary>
    public sealed class InfoPanelPresenter : System.IDisposable
    {
        readonly System.Action? clearSelection;
        readonly EventCallback<NavigationCancelEvent> onCancel;

        /// <summary>The class of a fact row (Game.uxml): a sign, then the fact's line; hidden while the line is empty.</summary>
        public const string RowClass = "info-row";
        /// <summary>On the panel while it shows a unit's card: the rows keep their lines but drop the hex signs (Statecraft.uss).</summary>
        public const string UnitClass = "info-unit";
        /// <summary>On the panel while it shows the GDD loop's city or site part (#381): a second column (Statecraft.uss).</summary>
        public const string GddClass = "info-gdd-shown";
        readonly List<(VisualElement row, Label line)> rows = new List<(VisualElement, Label)>();

        readonly VisualElement panel; readonly SimBridge bridge; readonly UiText text;
        readonly Button campButton, housingButton, defenceButton, moveButton, fortifyButton, holdButton, attackButton, sendHomeButton, skipButton, reinforceButton;
        /// <summary>The Reinforce order the card offers now (#1510), or null: what its button queues.</summary>
        Reinforce? reinforce;
        readonly System.Action onCamp, onHousing, onDefence, onMove, onFortify, onHold, onAttack, onSendHome, onSkip, onReinforce;
        readonly System.Func<PlacedToken?> selectedUnit;
        readonly System.Action? unitDone;
        bool unitMode;
        /// <summary>Lights the hexes the Move or Attack pick offers on the map (none: clears them); null: nothing is drawn.</summary>
        readonly System.Action<IReadOnlyCollection<(int q, int r)>>? showReachable;
        /// <summary>Draws a multi-turn go-to route and its arrival-turn numbers; null keeps picking functional without it.</summary>
        readonly System.Action<IReadOnlyList<(int q, int r, int turn)>>? showRoute;
        /// <summary>While the player picks a Move's or an Attack's hex: the unit, the hexes the sim gave (Movement.Reachable's,
        /// or those Combat.Check takes: the only ones a click takes), the session and turn they were read on (a turn or a
        /// load ends the pick, as the hexes change), and whether it is the attack's.</summary>
        (int unit, HashSet<(int q, int r)> hexes, GameSession session, int turn, bool attack, (int q, int r)? touchRoute)? pick;
        (int q, int r)? shownRoute;
        /// <summary>TakesClickThrough's last answer, by hex: the pointer asks it every frame it hovers the card, and a go-to's
        /// route is a whole-map search. Forgotten when the pick ends and on every render (an order or a new state).</summary>
        ((int q, int r) hex, bool takes)? clickThrough;
        /// <summary>A sim unit's name on screen, for the attack's targets (GameRoot: its token's card title); null, or no
        /// name, gives the hex's coordinates.</summary>
        readonly System.Func<int, string?>? unitName;
        readonly Label title, subtitle, region, settlements, localitiesLabel, placement, reason;
        readonly System.Func<int, int, IReadOnlyList<Statecraft.Game.Map.Kits.Localities.Place>>? localitiesAt;
        readonly UiIcon thumb;
        /// <summary>The GDD loop's part (GddHexPanel, #381): a city's counts and orders, or the designated site's.</summary>
        readonly VisualElement gdd;
        /// <summary>The hex's city projects (GddHexPanel, #1122), under the facts.</summary>
        readonly VisualElement projects;
        /// <summary>The map stop the panel is opened from: the last place to put the focus when a press leaves its whole
        /// part with nothing that can take it (#41, "Still open" item 2).</summary>
        readonly VisualElement? mapFocus;
        readonly IReadOnlyDictionary<string, string> settlementNames;
        readonly MapRegions? regions;
        readonly System.Func<string, (int q, int r)>? townHex;
        readonly IReadOnlyDictionary<string, string>? townNamesHe;
        /// <summary>Whether the last press in the panel came from the keyboard: only then does a rebuilt GDD part give the
        /// focus back (RenderLines).</summary>
        readonly PressSource presses;

        /// <param name="localitiesAt">The 1948 localities that lie in a hex (null: the panel lists none); they are listed under the
        /// settlements with today's name where the shared rule (Localities.TodayName) allows (#245).</param>
        /// <param name="nav">Opens the Defence screen from a unit's card (null: the button does nothing).</param>
        /// <param name="selectedUnit">The selected unit's token now (null: none), read at every render so the card follows
        /// the state.</param>
        /// <param name="regions">What the map outlines (the 1948 sub-districts and the 1949 armistice lines in force); the
        /// panel names the selected hex's sub-district or armistice zone, and any armistice line through it, with its
        /// agreement (null: no region line).</param>
        /// <param name="townHex">The hex each settlement's own point lies in, by id (<see cref="TownHex"/>; null: no note on
        /// a town drawn from the water hex next door, #204).</param>
        /// <param name="townNamesHe">Each settlement's Hebrew name (name_he), by id: the Hebrew UI names the town with it in the
        /// title, the "Settlements" line and that note (#899), or with the English name where it has none (null: English
        /// names).</param>
        /// <param name="showReachable">Lights the hexes a Move can go to while the player picks one, and clears them with none
        /// (GameRoot: MapScene.ShowReachable; null: the pick still works, with nothing lit). The Attack pick lights its hexes
        /// with it too.</param>
        /// <param name="unitName">A sim unit's name on screen, by id (GameRoot: its token's card title), for the Attack pick's
        /// targets (null: the hex's coordinates).</param>
        public InfoPanelPresenter(VisualElement root, SimBridge bridge, UiText text, IReadOnlyDictionary<string, string> settlementNames,
            ScreenNav? nav = null, System.Func<PlacedToken?>? selectedUnit = null, MapRegions? regions = null,
            System.Func<int, int, IReadOnlyList<Statecraft.Game.Map.Kits.Localities.Place>>? localitiesAt = null,
            System.Func<string, (int q, int r)>? townHex = null, IReadOnlyDictionary<string, string>? townNamesHe = null,
            System.Action<IReadOnlyCollection<(int q, int r)>>? showReachable = null, System.Func<int, string?>? unitName = null,
            System.Action? unitDone = null,
            System.Action<IReadOnlyList<(int q, int r, int turn)>>? showRoute = null, System.Action? clearSelection = null)
        {
            panel = root.Q<VisualElement>("info-panel"); this.bridge = bridge; this.text = text;
            this.clearSelection = clearSelection;
            onCancel = e => { if (!e.isPropagationStopped && Escape()) e.StopPropagation(); };
            panel.RegisterCallback(onCancel);
            this.settlementNames = settlementNames; this.regions = regions; this.localitiesAt = localitiesAt; this.townHex = townHex;
            this.townNamesHe = townNamesHe;
            title = panel.Q<Label>("info-title"); subtitle = panel.Q<Label>("info-subtitle"); region = panel.Q<Label>("info-region"); settlements = panel.Q<Label>("info-settlements");
            localitiesLabel = panel.Q<Label>("info-localities");
            placement = panel.Q<Label>("info-placement"); reason = panel.Q<Label>("info-reason");
            thumb = panel.Q<UiIcon>("info-thumb");
            gdd = panel.Q<VisualElement>(GddHexPanel.HostName);
            projects = panel.Q<VisualElement>(GddHexPanel.ProjectsHostName);
            mapFocus = root.Q<VisualElement>("map-focus");
            foreach (var row in panel.Query(className: RowClass).ToList()) rows.Add((row, row.Q<Label>()));
            campButton = panel.Q<Button>("build-camp"); housingButton = panel.Q<Button>("build-housing");
            onCamp = () => Build(MapActions.TransitCamp); onHousing = () => Build(MapActions.Housing);
            campButton.clicked += onCamp; housingButton.clicked += onHousing;
            defenceButton = panel.Q<Button>("info-defence");
            this.selectedUnit = selectedUnit ?? (() => null);
            onDefence = () => nav?.Open("defence");
            defenceButton.clicked += onDefence;
            defenceButton.text = text["unit.defence"];
            this.showReachable = showReachable;
            this.unitDone = unitDone;
            this.showRoute = showRoute;
            moveButton = panel.Q<Button>("info-move");
            onMove = () => TogglePick(attack: false);
            moveButton.clicked += onMove;
            moveButton.text = text["unit.order.move"];
            fortifyButton = panel.Q<Button>("info-fortify"); holdButton = panel.Q<Button>("info-hold");
            onFortify = () => GiveOrder(Orders.Fortify); onHold = () => GiveOrder(Orders.Hold);
            fortifyButton.clicked += onFortify; holdButton.clicked += onHold;
            fortifyButton.text = text["unit.order.fortify"]; holdButton.text = text["unit.order.hold"];
            presses = new PressSource(panel);
            this.unitName = unitName;
            attackButton = panel.Q<Button>("info-attack");
            onAttack = () => TogglePick(attack: true);
            attackButton.clicked += onAttack;
            attackButton.text = text["unit.order.attack"];
            sendHomeButton = panel.Q<Button>("info-send-home");
            onSendHome = () => GiveOrder(Demobilization.SendHome);
            sendHomeButton.clicked += onSendHome;
            sendHomeButton.text = text["unit.order.send_home"];
            skipButton = panel.Q<Button>("info-skip");
            onSkip = Skip;
            skipButton.clicked += onSkip;
            skipButton.text = text["unit.order.skip"];
            reinforceButton = panel.Q<Button>("info-reinforce");
            onReinforce = () => { if (reinforce is Reinforce r) { bridge.Queue(r); Render(); } };
            reinforceButton.clicked += onReinforce;
            bridge.Changed += Render;
            panel.parent?.RegisterCallback<GeometryChangedEvent>(OnHudGeometry);
            Render();
        }

        void OnHudGeometry(GeometryChangedEvent e) => panel.EnableInClassList(ShortClass, e.newRect.height < ShortBelowPx);

        /// <summary>Unsubscribes from the bridge and the buttons (the owner calls this when disabled).</summary>
        public void Dispose() { panel.UnregisterCallback(onCancel); presses.Dispose(); bridge.Changed -= Render; panel.parent?.UnregisterCallback<GeometryChangedEvent>(OnHudGeometry); campButton.clicked -= onCamp; housingButton.clicked -= onHousing; defenceButton.clicked -= onDefence; moveButton.clicked -= onMove; fortifyButton.clicked -= onFortify; holdButton.clicked -= onHold; attackButton.clicked -= onAttack; sendHomeButton.clicked -= onSendHome; skipButton.clicked -= onSkip; reinforceButton.clicked -= onReinforce; EndPick(); }

        /// <summary>Below this window height (logical px; 1366x657 is about 998) the panel gets ShortClass, and the city's own
        /// hex folds its projects behind their toggle as on a stacked bar (GddHexPanel.ToggleName): there the full list ran into
        /// the left column (InfoPanelLayoutTests).</summary>
        public const float ShortBelowPx = 1040f;
        public const string ShortClass = "info-short";

        /// <summary>Escape dismisses a reachable selection and its pick, without queuing an order. Covers keep priority.</summary>
        public bool Escape()
        {
            var hud = panel.parent;
            if (Covered || !panel.enabledInHierarchy || hud?.ClassListContains(ScreenNav.ScreenOpenClass) == true
                || (Selected == null && !ShowsUnit)) return false;
            Clear(); clearSelection?.Invoke();
            return true;
        }

        public (int q, int r)? Selected { get; private set; }

        /// <summary>True while the panel shows a unit's card.</summary>
        public bool ShowsUnit => unitMode && selectedUnit() != null;

        /// <summary>True while a screen covers the panel (a wide screen, or any screen in the two-row HUD): a map click
        /// then changes neither the hex nor the unit shown (#210; units-in-play merge note).</summary>
        public bool Covered
        {
            get
            {
                var hud = panel.parent;
                return hud != null && (hud.ClassListContains(ScreenNav.WideOpenClass)
                    || (hud.ClassListContains(ScreenNav.ScreenOpenClass) && hud.ClassListContains(HudPresenter.TwoRowClass)));
            }
        }

        /// <summary>Shows the hex; ignored while a screen covers the panel, so a map click cannot change a panel the player
        /// cannot see.</summary>
        public void Show(int q, int r)
        {
            if (Covered) return;   // covered (#210)
            if (unitMode || Selected != (q, r)) projects.RemoveFromClassList(GddHexPanel.OpenClass);   // a newly chosen hex starts with its city projects folded
            EndPick();
            unitMode = false; Selected = (q, r);
            Render();
        }

        /// <summary>Shows the selected unit's card (the unit comes from the selectedUnit callback); ignored while covered.</summary>
        public void ShowUnit()
        {
            if (Covered) return;
            EndPick();
            unitMode = true; Selected = null; Render();
        }

        /// <summary>Shows nothing selected (the hint), as before the first click; ignored while covered.</summary>
        public void Clear()
        {
            if (Covered) return;
            EndPick();
            unitMode = false; Selected = null; Render();
        }

        /// <summary>The shown unit's sim id and the hexes its Move can go to (Movement.Reachable: the sim's answer, spec section
        /// 2.6), or null when there is no Move to give: no unit card, no sim unit for it, no hex in reach, or the game is over.</summary>
        (int unit, IReadOnlyDictionary<(int Q, int R), int> hexes)? MoveTargets()
        {
            var unit = unitMode ? selectedUnit() : null;
            if (unit == null || bridge.State.GameOver != null || !(UnitCard.SimUnit(bridge.Content, bridge.State, unit.Value.Token) is int id)
                || bridge.GoingHome(id)) return null;
            var reach = Movement.Reachable(bridge.Content, bridge.State, id, bridge.Spent);
            return reach.Count == 0 ? null : (id, reach);
        }

        /// <summary>The shown unit's sim id, the hexes its attack can name (each unit's hex the sim's Combat.Check takes: the
        /// sim's answer, nothing reckoned here), and the sim's reasons for the hexes it refuses that Combat.Preview gives odds
        /// for (an enemy in the unit's reach: no Arms for it, or the attack it already has). Null when the unit can take no
        /// order (OrderUnit).</summary>
        (int unit, List<(int q, int r)> hexes, List<string> refused)? AttackTargets()
        {
            if (!(OrderUnit() is int id)) return null;
            var c = bridge.Content; var s = bridge.State;
            var hexes = new List<(int q, int r)>(); var refused = new List<string>();
            foreach (var (q, r) in s.Units.Values.Select(u => (u.Q, u.R)).Distinct())
            {
                var why = Combat.Check(c, s, id, q, r, spent: bridge.Spent);
                if (why == null) hexes.Add((q, r));
                else if (Combat.Preview(c, s, id, q, r) != null && !refused.Contains(why)) refused.Add(why);
            }
            return (id, hexes, refused);
        }

        /// <summary>Move's or Attack's button: starts its pick (the sim's hexes lit), or ends it when pressed again; a press
        /// of the other ends the open pick and starts its own.</summary>
        void TogglePick(bool attack)
        {
            var again = pick?.attack == attack;
            EndPick();
            (int unit, List<(int q, int r)> hexes)? offer = null;
            if (!again && attack && AttackTargets() is { } a && a.hexes.Count > 0) offer = (a.unit, a.hexes);
            if (!again && !attack && MoveTargets() is { } m) offer = (m.unit, m.hexes.Keys.Select(h => (h.Q, h.R)).ToList());
            if (offer is { } o)
            {
                pick = (o.unit, new HashSet<(int q, int r)>(o.hexes), bridge.Session, bridge.State.Turn, attack, null);
                showReachable?.Invoke(o.hexes);
            }
            Render();
        }

        /// <summary>Q-UI-1's preview of the shown unit's attack on (q, r), for the card: the defenders' names and the sim's
        /// strength ratio (Combat.Preview), then its combat_table row's outcome bands (Combat.Outcomes); empty where Preview
        /// has none. Every number is the sim's, read on the state after the turn's committed orders.</summary>
        string Odds(int unit, int q, int r)
        {
            var c = bridge.Content; var s = bridge.State;
            if (!(Combat.Preview(c, s, unit, q, r) is { } p)) return "";
            // The enemy units there (Combat's defenders: a side, not Israel's; Check has made sure there is one).
            var names = s.Units.Values.Where(u => u.Q == q && u.R == r && u.Side != "" && u.Side != Movement.PlayerSide).Select(u => unitName?.Invoke(u.Id)).Where(n => !string.IsNullOrEmpty(n)).ToList();
            var who = names.Count > 0 ? string.Join(", ", names) : string.Format(CultureInfo.InvariantCulture, "{0},{1}", q, r);
            var bands = Combat.Outcomes(c, s, p.Row).Select(b => text.Format(b.Retreat ? "unit.attack_band_retreat" : "unit.attack_band",
                b.Faces, b.Of, b.AttackersLose, b.DefendersLose));
            return text.Format("unit.attack_odds", who, p.RatioPercent) + "\n" + string.Join("\n", bands);   // one band a line
        }

        /// <summary>The shown unit's sim id, or null: no unit card, no sim unit for it, or the game is over.</summary>
        int? ShownUnit()
        {
            var unit = unitMode ? selectedUnit() : null;
            return unit == null || bridge.State.GameOver != null ? null : UnitCard.SimUnit(bridge.Content, bridge.State, unit.Value.Token);
        }

        /// <summary>The shown unit's sim id, or null when it can take no order: none is shown (ShownUnit), or Send home is
        /// queued for it (the bridge refuses it every other order, spec 3.2).</summary>
        int? OrderUnit() => ShownUnit() is int id && !bridge.GoingHome(id) ? id : (int?)null;

        /// <summary>Fortify, Hold or Send home's button: queues the sim's order for the shown unit (one per unit: another
        /// UnitOrder is replaced). A refusal is raised by the bridge (Refused); the card redraws either way.</summary>
        void GiveOrder(string order)
        {
            if (!(OrderUnit() is int id)) return;
            if (bridge.Queue(new UnitOrder(id, order)) == null) UnitFinished();
            Render();
        }

        void Skip()
        {
            if (!(OrderUnit() is int id)) return;
            var keyboard = presses.ByKeyboard;
            bridge.Skip(id);
            UnitFinished();
            Render();
            // Skip hides its current button before cycling. Keep keyboard navigation on the next card, or End turn.
            if (keyboard) FocusReturn.To(skipButton.style.display.value == DisplayStyle.Flex
                ? skipButton : panel.panel?.visualTree.Q<Button>("end-turn"));
        }

        void UnitFinished()
        {
            if (bridge.NeedsOrders.Count > 0) unitDone?.Invoke();
        }

        void EndPick()
        {
            if (pick == null) return;
            pick = null;
            shownRoute = null;
            clickThrough = null;
            showReachable?.Invoke(System.Array.Empty<(int q, int r)>());
            showRoute?.Invoke(System.Array.Empty<(int q, int r, int turn)>());
        }

        IReadOnlyList<(int q, int r, int turn)> RouteTo(
            (int unit, HashSet<(int q, int r)> hexes, GameSession session, int turn, bool attack, (int q, int r)? touchRoute) p,
            int q, int r)
        {
            if (p.attack || p.hexes.Contains((q, r))) return System.Array.Empty<(int, int, int)>();
            var now = bridge.Session.Now;
            return Movement.Route(bridge.Content, now.State, p.unit, q, r, now.Spent)
                .Select(h => (h.Q, h.R, h.Turn)).ToList();
        }

        /// <summary>Clears a hovered route (the mouse left the map): once, and so that the same hex shows it again later.</summary>
        public void HideRoute()
        {
            if (shownRoute == null) return;
            shownRoute = null;
            showRoute?.Invoke(System.Array.Empty<(int q, int r, int turn)>());
        }

        /// <summary>Shows the route to the hovered unlit hex while Move is picking; otherwise clears it.</summary>
        public void ShowRouteTo(int q, int r)
        {
            if (shownRoute == (q, r)) return;
            shownRoute = (q, r);
            var route = pick is { } p ? RouteTo(p, q, r) : System.Array.Empty<(int, int, int)>();
            showRoute?.Invoke(route);
        }

        /// <summary>#1617 (player report): the lit hexes of an open Move or Attack pick show through this card (its navy is
        /// translucent), so a click on the card over one of them is meant for the map. True when a pick is open,
        /// <paramref name="picked"/> (the UI element under the pointer) is this card or inside it but not inside one of its
        /// buttons, and the hex under the pointer (<paramref name="hexUnder"/>, asked only then) is one PickTarget takes: lit,
        /// or, for Move, an unlit hex with a route (go-to, #1398).</summary>
        public bool TakesClickThrough(VisualElement? picked, System.Func<(int q, int r)?> hexUnder)
        {
            if (!(pick is { } p) || picked == null || (picked != panel && !panel.Contains(picked))) return false;
            for (var e = picked; e != panel; e = e.parent) if (e is Button) return false;
            if (!(hexUnder() is { } h)) return false;
            if (clickThrough is { } known && known.hex == h) return known.takes;
            var takes = p.hexes.Contains(h) || RouteTo(p, h.q, h.r).Count > 0;   // RouteTo: PickTarget's go-to rule
            clickThrough = (h, takes);
            return takes;
        }

        /// <summary>A map click while the player picks a Move's or an Attack's hex (GameRoot.OnHexClicked asks first): a lit
        /// hex gives the Move or attack; an unlit hex with a route gives go-to. Touch previews that route on the first tap and
        /// confirms it on a second tap of the same hex. A hex with no target ends the pick and selects as usual.</summary>
        public bool PickTarget(int q, int r, bool touch = false)
        {
            if (!(pick is { } p)) return false;
            Command? command = p.hexes.Contains((q, r))
                ? p.attack ? new UnitOrder(p.unit, Combat.Attack, q, r) : new Move(p.unit, q, r)
                : null;
            if (command == null && !p.attack)
            {
                var route = RouteTo(p, q, r);
                if (route.Count > 0)
                {
                    if (touch && p.touchRoute != (q, r))
                    {
                        pick = (p.unit, p.hexes, p.session, p.turn, p.attack, (q, r));
                        shownRoute = (q, r);
                        showRoute?.Invoke(route);
                        Render();
                        return true;
                    }
                    command = new UnitOrder(p.unit, Movement.GoTo, q, r);
                }
            }
            EndPick();
            if (command == null) return false;
            var refused = bridge.Queue(command);   // a refusal is raised by the bridge (Refused)
            if (refused == null && (command is UnitOrder || Movement.PointsLeft(bridge.Content, bridge.State, p.unit, bridge.Spent) == 0))
            {
                UnitFinished();
                Render();
            }
            else if (refused == null && command is Move && MoveTargets() is { }) TogglePick(false);
            else Render();   // a refused Queue raises no Changed
            return true;
        }

        string GoToLine(int unit)
        {
            var now = bridge.Session.Now;
            if (!now.State.Units.TryGetValue(unit, out var u) || u.Order != Movement.GoTo) return "";
            var parts = u.OrderTarget.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var q)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r)) return "";
            var route = Movement.Route(bridge.Content, now.State, unit, q, r, now.Spent);
            return route.Count == 0 ? "" : text.Format("unit.go_to_given", System.Math.Max(0, route[route.Count - 1].Turn - 1));
        }

        void Build(string action)
        {
            if (Selected == null) return;
            // The bridge reports a refusal itself; the sim enforces legality and the per-turn cap when the turn runs.
            bridge.Queue(new MapAction(action, Selected.Value.q, Selected.Value.r));
        }

        /// <summary>The terrain sign: the biome's glyph on the biome's own map colour (MapBuilder.BiomeColours), hidden when no
        /// hex (or no hex on the map) is selected.</summary>
        void ShowThumb(string? biome)
        {
            var known = biome != null && MapBuilder.BiomeColours.ContainsKey(biome);
            thumb.style.display = known ? DisplayStyle.Flex : DisplayStyle.None;
            thumb.Glyph = known ? "biome-" + biome : "";
            thumb.style.backgroundColor = known ? new StyleColor(MapBuilder.BiomeColours[biome!]) : new StyleColor(StyleKeyword.Null);
        }

        /// <summary>The region lines for a hex on a date: its armistice zone (name, agreement, date) if one is in force, else
        /// its 1948 sub-district; then each armistice line in force through it.</summary>
        public static string RegionText(MapRegions regions, UiText text, int q, int r, System.DateTime date)
        {
            // Isolate a Hebrew date from the surrounding English fallback names: otherwise the row's
            // LTR paragraph can place its day before its year/month (the mixed-language #558 browser regression).
            string DateText(System.DateTime signed)
            {
                var value = Control1948.DateText(signed, k => text[k]);
                return UiDirection.HasRightToLeft(value) ? "\u2067" + value + "\u2069" : value;
            }
            var parts = new List<string>();
            var zone = regions.ZoneAt(q, r, date);
            var control = ControlText(regions, text, q, r, date);
            // a demilitarized zone is named by its own line below; the control line would only repeat "Demilitarized zone"
            if (control != "" && !(zone != null && regions.Control?.At(q, r, date).side == Control1948.Side.Dmz)) parts.Add(control);
            // the sub-district only while the map draws the sub-district bands (before the first sourced line, or with no side data)
            var sub = regions.Subdistricts.ShowsOn(date) && regions.ControlPeriod(date) == null ? regions.Subdistricts.SubdistrictAt(q, r) : null;
            if (zone != null) parts.Add(text.Format("info.armistice_zone", zone.Name, zone.Agreement, DateText(zone.Signed)));
            else if (sub != null) parts.Add(text.Format("info.region", sub.Name, sub.District));
            foreach (var l in regions.LinesAt(q, r, date))
                    parts.Add(text.Format(l.LowAccuracy ? "info.armistice_line_low" : "info.armistice_line", l.Name, l.Agreement, DateText(l.Signed)));
            return string.Join("\n", parts);
        }

        /// <summary>Who held the hex on a date (owner rulings 2026-10-14; the colours claim military control, nothing else):
        /// "Held by Israel", "Held by Arab forces (Egypt)" while the period's legend is the 1948 one, the side of the armistice
        /// line where that hex's front has switched; "" with no side data or before the first period.</summary>
        public static string ControlText(MapRegions regions, UiText text, int q, int r, System.DateTime date)
        {
            var p = regions.ControlPeriod(date);
            if (p == null || regions.Control == null) return "";
            var (side, holder) = regions.Control.At(q, r, date);
            bool armistice = p.Legend == Control1948.Legend.Armistice || (p.Legend == Control1948.Legend.Mixed && ArmisticeHere(p, side, holder));
            string By(string plain, string by) => holder == null ? text[plain] : text.Format(by, text["control.holder_" + holder]);
            return side switch
            {
                Control1948.Side.Israel => text[armistice ? "info.control_side_israel" : "info.control_israel"],
                Control1948.Side.Arab => armistice ? By("info.control_side_arab", "info.control_side_arab_by") : By("info.control_arab", "info.control_arab_by"),
                Control1948.Side.Contested => text["info.control_contested"],
                Control1948.Side.NoMansLand => text["info.control_no_mans_land"],
                Control1948.Side.Dmz => text["info.control_dmz"],
                Control1948.Side.OffData => "",   // beyond the frontier or the sea: the data says nothing there
                _ => text["info.control_none"],
            };
        }

        /// <summary>In a period where only some fronts have switched, the armistice wording is used for the other side when its
        /// holder's front has switched (Egypt, Jordan, Syria, Lebanon); the Israeli side keeps "held by" (which front faces a
        /// hex is not in the data).</summary>
        static bool ArmisticeHere(Control1948.Period p, Control1948.Side side, string? holder) =>
            side == Control1948.Side.Arab && holder != null && p.Fronts.Contains(holder);

        /// <summary>The "1948 localities" line for a hex: each place's 1948 name, with today's name where Localities.TodayName
        /// allows ("Beisan (today: Beit She'an)"); empty for a hex with none. The Hebrew UI names a place by its name_he, by the
        /// English name where it has none (#899), and drops a today name that is the same Hebrew word (Umm el Fahm and Umm al-Fahm
        /// are both one name in Hebrew: 48 rows that would otherwise show a today name).</summary>
        public static string LocalitiesText(UiText text, IReadOnlyList<Statecraft.Game.Map.Kits.Localities.Place> places)
        {
            if (places.Count == 0) return "";
            bool hebrew = text["ui.lang"] == "he";
            var names = places.Select(p =>
            {
                var name = hebrew && !string.IsNullOrWhiteSpace(p.NameHe) ? p.NameHe! : p.Settlement.Name;
                return Statecraft.Game.Map.Kits.Localities.TodayName(p, hebrew) is string today && today != name
                    ? text.Format("info.locality_today", name, today) : name;
            });
            return text.Format("info.localities", string.Join(", ", names));
        }

        /// <summary>The hex each settlement's own point lies in, by id, picked as the map picks hexes (HexPicker, the rule of
        /// tools/hexgrid.py). It differs from the settlement's hex in settlement_hexes.yaml only where hexgrid.settlement_hex
        /// moved a town in a water hex to its nearest land neighbour (Tiberias, #204), so no content/map field is needed.</summary>
        public static System.Func<string, (int q, int r)> TownHex(IEnumerable<Settlement> settlements, HexFile hexes)
        {
            var at = settlements.ToDictionary(s => s.Id, s => HexPicker.FromLonLat(s.Lon, s.Lat, hexes.bounds, hexes.hex_size_km));
            return id => at[id];
        }

        /// <summary>A settlement's name in the UI's language: its name_he under Hebrew, the English name where it has none.</summary>
        string TownName(string id) =>
            text["ui.lang"] == UiLanguage.Hebrew && townNamesHe != null && townNamesHe.TryGetValue(id, out var he)
                && !string.IsNullOrWhiteSpace(he) ? he : settlementNames[id];

        void ShowLocalities(string line)
        {
            localitiesLabel.text = line;
            localitiesLabel.style.display = line.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void Render()
        {
            clickThrough = null;
            RenderLines();
            panel.EnableInClassList(GddClass, gdd.style.display.value == DisplayStyle.Flex);
            // A fact row shows only when its line has something to say (an empty row would leave a lone sign).
            foreach (var (row, line) in rows)
                row.style.display = string.IsNullOrEmpty(line.text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void RenderLines()
        {
            var c = bridge.Content; var s = bridge.State;
            var camp = campButton; var housing = housingButton;
            var unit = unitMode ? selectedUnit() : null;
            // A unit that has left the state (destroyed or surrendered in its own attack) closes its card; a row with no
            // sim unit at all (an aircraft squadron, the legacy loop) keeps its card.
            if (unit != null && (unit.Value.Token.SimId != null || c.StartingForces.Any(f => f.Row == unit.Value.Token.Unit.Id))
                && UnitCard.SimUnit(c, s, unit.Value.Token) == null)
            {
                unitMode = false;
                unit = null;
            }
            defenceButton.style.display = unit != null ? DisplayStyle.Flex : DisplayStyle.None;
            // A pick read on another turn or session, or for a unit no longer shown, is over: its hexes are not the sim's now.
            if (pick is { } open
                && (open.session != bridge.Session || open.turn != s.Turn || unit == null || UnitCard.SimUnit(c, s, unit.Value.Token) != open.unit))
                EndPick();
            var moves = MoveTargets();
            moveButton.style.display = moves != null ? DisplayStyle.Flex : DisplayStyle.None;
            // Fortify and Hold (#754): each offered only where the sim's Orders.Check takes it; else the sim's reason, as given,
            // on Israel's units only (an enemy's card would only repeat the sim's English "That unit is not yours").
            var orderUnit = OrderUnit(); var shownUnit = ShownUnit();
            var own = unit?.Token.Unit.Side == UnitData.Israel;
            var refusals = new List<string>();
            foreach (var (order, button) in new[] { (Orders.Fortify, fortifyButton), (Orders.Hold, holdButton) })
            {
                var why = orderUnit is int ou ? Orders.Check(c, s, ou, order) : null;
                button.style.display = orderUnit != null && why == null ? DisplayStyle.Flex : DisplayStyle.None;
                if (why != null && own && !refusals.Contains(why)) refusals.Add(why);
            }
            // Attack (#754): offered where the sim's Combat.Check takes some hex; its refusals in reach show as the others'.
            var attacks = AttackTargets();
            attackButton.style.display = attacks is { } at && at.hexes.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var why in attacks?.refused ?? new List<string>()) if (own && !refusals.Contains(why)) refusals.Add(why);
            // Send home (#1309): Demobilization.Check is the complete eligibility rule. A refusal hides the order and adds
            // no card copy; the player sees it only after the relevant armistice makes the order available.
            var sendHomeWhy = orderUnit is int du ? Demobilization.Check(c, s, du, bridge.Session.Now.Attacked) : null;
            sendHomeButton.style.display = orderUnit != null && sendHomeWhy == null ? DisplayStyle.Flex : DisplayStyle.None;
            skipButton.style.display = own == true && orderUnit != null && bridge.NeedsOrders.Contains(orderUnit.Value) ? DisplayStyle.Flex : DisplayStyle.None;
            // Reinforce (#1510, R-TRAIN-004): offered for a unit of Israel's that has lost strength and stands in a city, for
            // the missing strength up to the city's free civilians; the sim's own Reinforce, run on a copy, decides, and its
            // refusal shows as the other orders' do. None while one is queued for the unit.
            reinforce = null;
            var reinforceQueued = shownUnit is int rq && bridge.QueuedLike(new Reinforce(rq, "", 0)) != null;
            if (orderUnit is int ru && own && !reinforceQueued && s.Units.TryGetValue(ru, out var formation) && Training.Room(s, formation) > 0
                && c.SettlementHexes.FirstOrDefault(h => h.Q == formation.Q && h.R == formation.R && s.Cities.ContainsKey(h.Id)) is { } home)
            {
                var n = System.Math.Max(1, System.Math.Min(Training.Room(s, formation), Training.Available(s, home.Id, bridge.Queued)));
                var why = Training.Reinforce(c, s.Clone(), ru, home.Id, n, new List<Cause>());
                if (why == null) reinforce = new Reinforce(ru, home.Id, n);
                else if (!refusals.Contains(why)) refusals.Add(why);
            }
            reinforceButton.style.display = reinforce != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (reinforce is Reinforce offer) reinforceButton.text = text.Format("unit.order.reinforce", offer.Count);
            panel.EnableInClassList(UnitClass, unit != null);
            if (unit != null)
            {
                var card = UnitCard.For(unit.Value, s, text, c.IsGdd, c, townNamesHe);
                panel.EnableInClassList("info-empty", false);
                // The orders' state under the card's lines: the pick asks for a hex (an attack's with each lit hex's odds, Q-UI-1).
                var order = pick is { attack: true } ap ? string.Join("\n", new[] { text["unit.attack_pick"] }.Concat(ap.hexes.Select(h => Odds(ap.unit, h.q, h.r))).Where(x => x != ""))
                    : pick != null ? text["unit.move_pick"] : "";
                var standing = shownUnit is int su && s.Units.TryGetValue(su, out var shown)
                    ? shown.Order == Orders.Fortify || shown.Order == Orders.Fortifying ? text["unit.fortify_queued"]
                    : shown.Order == Orders.Hold ? text["unit.hold_queued"]
                    : bridge.QueuedLike(new UnitOrder(su, Demobilization.SendHome)) != null ? text["unit.send_home_queued"] : ""
                    : "";
                var points = shownUnit is int pu && s.Units.TryGetValue(pu, out var military) && military.Side == Movement.PlayerSide
                    && c.UnitTypes.FirstOrDefault(t => t.Id == military.Type) is { MovementPoints: > 0 } type
                    ? text.Format("unit.points", Movement.PointsLeft(c, s, pu, bridge.Spent), type.MovementPoints) : "";
                var routeLine = shownUnit is int routeUnit ? GoToLine(routeUnit) : "";
                var lines = new[] { card.Body, points, order, routeLine, standing, reinforceQueued ? text["unit.reinforce_queued"] : "" }.Where(x => x != "");
                title.text = card.Title; subtitle.text = card.Subtitle; settlements.text = string.Join("\n", lines);
                ShowLocalities("");
                region.text = "";   // a unit's card names no sub-district (the last hex's would stay under it)
                placement.text = card.Footer; reason.text = string.Join("\n", new[] { card.Note }.Where(x => x != "").Concat(refusals));
                camp.style.display = DisplayStyle.None; housing.style.display = DisplayStyle.None;
                ShowThumb(null);
                GddHexPanel.Hide(gdd); GddHexPanel.Hide(projects);
                return;
            }
            panel.EnableInClassList("info-empty", Selected == null);
            if (Selected == null)
            {
                title.text = text["info.hint"];
                subtitle.text = region.text = settlements.text = placement.text = reason.text = "";
                ShowLocalities("");
                camp.style.display = DisplayStyle.None; housing.style.display = DisplayStyle.None;
                ShowThumb(null);
                GddHexPanel.Hide(gdd); GddHexPanel.Hide(projects);
                return;
            }
            var (q, r) = Selected.Value;
            var hex = c.HexAt(q, r);
            ShowThumb(hex?.Biome);
            var owned = c.SettlementHexes.Where(x => x.Q == q && x.R == r).Select(x => TownName(x.Id)).ToList();   // strict: every settlement hex has a name (final review M4)
            title.text = owned.Count > 0 ? owned[0] : hex == null ? text["info.off_map"] : text["info.biome." + hex.Biome];
            // The GDD loop's city or designated site (#381); a site's hex has no settlement, so its name is the title.
            // GddHexPanel rebuilds its part on every render, so a button pressed from the keyboard (Train, Assign, Build) went
            // with the focus and the next Tab started again at Cabinet (#41, the keyboard-only turn): the rebuilt button of
            // the same name takes it back. A pointer press leaves no focus (PointerBlur), so it gets none. PressSource reads
            // "keyboard" until the first pointer press in the panel; here that only means a focus nobody pressed stays put.
            // A hex with two sites (the Hula swamp and its field) or two corridor segments gives each part buttons of the same
            // names (site-build-<unit>, site-stop), so the button is looked up inside the part it was in.
            var focused = presses.ByKeyboard ? panel.focusController?.focusedElement as VisualElement : null;
            var keep = focused != null && (gdd.Contains(focused) || projects.Contains(focused)) ? focused.name : null;
            // Which host it was in, read before Render while it is still in the tree: afterwards a button that is gone
            // from gdd would look as though it had been in projects.
            var keptIn = keep == null ? null : gdd.Contains(focused!) ? gdd : projects;
            var part = keep == null ? null : focused!.parent;
            while (part != null && !(part.name is { } pn && (pn.StartsWith("site-part-", System.StringComparison.Ordinal) || pn.StartsWith("corridor-part-", System.StringComparison.Ordinal))))
                part = part.parent;
            var partName = part?.name;
            var siteName = GddHexPanel.Render(gdd, projects, bridge, text, q, r, TownName);
            if (!string.IsNullOrEmpty(keep))
            {
                // The rebuilt button of the same name; failing that the nearest control that can take the focus, the
                // part first, then the host it was in, then the other host, and last the map stop (#41, "Still open"
                // item 2). A press can leave its whole part unfocusable: Build disables every Build in the part once one
                // group is ordered, and Stop and a project's Cancel leave a state line and a disabled button.
                var rebuiltPart = partName != null ? gdd.Q(partName) : null;
                var again = rebuiltPart != null ? rebuiltPart.Q(keep) : keptIn!.Q(keep);
                FocusReturn.ToOrWithin(again, mapFocus, rebuiltPart, keptIn, keptIn == gdd ? projects : gdd);
            }
            if (siteName != null && owned.Count == 0) title.text = siteName;
            subtitle.text = hex == null ? "" : text.Format("info.hex", text["info.biome." + hex.Biome], hex.ElevM);   // no debug coordinates in player text (playtest 2, P2-6)
            // The armistice zone in force, else the sub-district on 15 May 1948, and any armistice line in force through the
            // hex, each with its agreement (true lines drawn to the nearest hex edge: the owner's ruling).
            region.text = regions == null ? "" : RegionText(regions, text, q, r, s.Date);
            settlements.text = owned.Count > 0 ? text.Format("info.owned", string.Join(", ", owned)) : "";
            // A town in a water hex is drawn on its nearest land hex (Tiberias, in the lake, on the 70 m hex): one line says
            // so, so this hex's elevation does not read as the town's (#204).
            if (townHex != null)
                foreach (var town in c.SettlementHexes.Where(x => x.Q == q && x.R == r && townHex(x.Id) != (q, r)))
                    settlements.text += "\n" + text.Format("info.town_in_water", TownName(town.Id));
            ShowLocalities(localitiesAt == null ? "" : LocalitiesText(text, localitiesAt(q, r)));
            var here = MapActions.At(s, q, r);
            var queued = bridge.QueuedLike(new MapAction(MapActions.Housing, q, r)) as MapAction;
            // The action's name from the UI strings (#929): PlayerText.MapAction is the content's English name (#924's tools
            // test keeps the English string equal to it).
            placement.text = queued != null ? text.Format("info.queued", PlayerText.Capitalised(text["info.map_action." + queued.Action]))
                : here != null ? text.Format("info.built", PlayerText.Capitalised(text["info.map_action." + here.Action]),
                    Control1948.LineDateText(here.Date.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture), k => text[k])) : "";   // PlayerText.Month is always English (#701)
            // The GDD loop refuses every map action and builds with city projects instead (R-PROJ-004): it has no map-action
            // button and no reason, so its refusal is not asked for (the UI has no words for it).
            var campReason = c.IsGdd ? null : MapActions.CheckReason(c, s, MapActions.TransitCamp, q, r);
            var housingReason = c.IsGdd ? null : MapActions.CheckReason(c, s, MapActions.Housing, q, r);
            var campWhy = campReason == null ? null : MapActionReasonText(campReason, text);
            var housingWhy = housingReason == null ? null : MapActionReasonText(housingReason, text);
            // eras.yaml systems.map_actions (#58): an action the era does not allow has no button and no reason
            var allowed = c.IsGdd ? new System.Collections.Generic.List<string>() : c.SystemsAt(s.Date).MapActions;
            bool campOn = allowed.Contains(MapActions.TransitCamp), housingOn = allowed.Contains(MapActions.Housing);
            // Both refused for the same reason (in 1948: "...can be placed from 1 January 1950"; or not land, too far, the
            // turn's cap): neither can be ordered on this hex now, so the reason is said once and no button shows (#398).
            bool shared = campOn && housingOn && campWhy != null && campWhy == housingWhy;
            camp.style.display = campOn && !shared ? DisplayStyle.Flex : DisplayStyle.None;
            housing.style.display = housingOn && !shared ? DisplayStyle.Flex : DisplayStyle.None;
            camp.SetEnabled(campOn && campWhy == null); housing.SetEnabled(housingOn && housingWhy == null);
            if (!campOn) campWhy = null;
            if (!housingOn) housingWhy = null;
            // with no rule for an action there is no cost to show (it is disabled with the sim's reason anyway)
            camp.text = c.RulesAt(s.Date).MapActions.TryGetValue(MapActions.TransitCamp, out var cr) ? text.Format("info.build_camp", PlayerText.Money(c, "treasury", cr.Cost)) : text["info.build_camp_plain"];
            housing.text = c.RulesAt(s.Date).MapActions.TryGetValue(MapActions.Housing, out var hr) ? text.Format("info.build_housing", PlayerText.Money(c, "treasury", hr.Cost)) : text["info.build_housing_plain"];
            reason.text = string.Join("\n", new[] { campWhy, housingWhy }.Where(x => x != null).Distinct());   // the sim's reasons, each once
        }

        static string MapActionReasonText(MapActionReason reason, UiText text)
        {
            string Action(string id) => text["info.map_action." + id];
            string Date(string iso) => Control1948.DateText(System.DateTime.ParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture), k => text[k]);
            var key = "info.map_action_reason." + reason.Kind;
            switch (reason.Kind)
            {
                case "action_from": case "action_until": return text.Format(key, PlayerText.Capitalised(Action(reason.Values[0])), Date(reason.Values[1]));
                case "all_from": return text.Format(key, Date(reason.Values[0]));
                case "too_far": {
                    var distance = int.Parse(reason.Values[0], CultureInfo.InvariantCulture);
                    return text.Format(key + (distance == 1 ? "_one" : "_many"), distance);
                }
                case "turn_cap": return text.Format(key, int.Parse(reason.Values[0], CultureInfo.InvariantCulture));
                case "occupied": return text.Format(key, Action(reason.Values[0]));
                default: return text[key];
            }
        }
    }
}
