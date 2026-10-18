#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Settlement names as UI Toolkit pills (EB Garamond SemiBold on navy with a gold edge, Statecraft.uss
    /// ".map-label") in the click-through "map-labels" layer under the panels. Each pill sits in a zero-size absolute slot;
    /// Refresh (GameRoot.LateUpdate, after the camera has moved) moves each slot with an inline pixel translate to its
    /// kit's top edge as the camera sees it, LiftPx above it, so no layout pass runs per frame, and returns early when
    /// nothing it reads has changed (the camera's view-projection and rect, the panel's size, the layer's origin, the zoom,
    /// which panels are shown, the pills' sizes, and no GeometryChangedEvent since). A GeometryChangedEvent on the layer or
    /// on a panel re-places the pills at once, inside the layout pass and before the repaint. The mapping assumes the camera's view fills the panel,
    /// as it does in the game (both span the canvas), so DPR, render scale and the panel's scale cancel out.
    ///
    /// Kits (issue #214): a pill keeps clear of its settlement's kit, as the promo hangs its names above the towns. Each
    /// refresh projects the kit's columns (MapScene.TryKitColumns, a few world boxes that hold its buildings; the marker
    /// box when there is no kit, or while the "kits" toggle is off and the boxes are drawn) to a screen rectangle, and
    /// every spot a pill may take is placed against that rectangle: above it (centred, or to the left or right of its
    /// centre), then beside it on the right or the left, then below it. Only when none of those fits may a pill sit over
    /// its kit, its bottom centre LiftPx above the settlement's point (or, a city failing that, its top centre LiftPx
    /// below it), and only if it covers at most MaxKitCover of the kit's rectangle (a big city's name over its own
    /// streets, never a kibbutz's roofs under a pill).
    ///
    /// Declutter: pills are placed in priority order (city, then town and Arab town, then the rest, each in file order). A
    /// pill is hidden (visibility, so it keeps its size and the choice does not flicker) when its marker is behind the
    /// camera, off screen or under a panel (OccluderNames), when it is a minor
    /// place (village, kibbutz, moshav) beyond DeclutterDistanceM, or when no spot fits. Its anchor class (USS) picks which
    /// of the three spots above the kit is tried first; the spots are applied as a pixel shift on the slot's translate (no
    /// class change, so no style pass). A spot fits when the pill lies wholly on screen and clear of every panel
    /// (OccluderNames: the top bar and its hanging date plate, the left icon column, the info panel, the minimap and its map-layer caption, the event card), does not
    /// overlap an already placed pill (its last laid-out size plus MarginPx), covers at most MaxKitCover of its own kit's
    /// rectangle and of every other visible kit's, and is more than twice as far from every other visible marker's top as
    /// from its own kit (the kit alone, not the tokens on it), plus MarginPx, so no name reads as a neighbour's. Whatever
    /// the spot, a pill is also nearer its own marker's top than any other visible marker's, by more than MarginPx
    /// (issue #599: two sprawling kits' rectangles overlap and each holds the other's marker, so a spot against one's
    /// edge can sit by the other's town). A city that finds no such spot keeps its name if it can: it takes, in this
    /// order, the first spot round its kit alone that still reads as its own (over a unit token if need be), then the
    /// first clear of panels, pills, tokens and its own kit, then the first clear of panels, pills and its own kit, each
    /// still nearer its own marker than any other; last, the same with only the game settlements' markers counted, so a
    /// 1948 locality's marker never takes a city's name. A pill hidden in the last refresh needs HysteresisPx more
    /// room to come back, so it does not blink at a threshold. Refresh allocates nothing. The dense Tel Aviv - Lod cluster
    /// keeps M2's anchor classes. The kits' bounds are read once, when the presenter is created (a later MapScene.Build
    /// needs a new presenter; GameRoot makes one per Begin).
    ///
    /// Units (issue #197): a pill also keeps clear of the unit tokens (UnitLayer.KeepClear, passed in as keepClear): each
    /// token's boxes (its standing figures, aircraft or ship, and its badge) are projected to one screen rectangle, and a
    /// spot that overlaps one, grown by MarginPx, does not fit, as a spot under a panel does not. A token that stands on a
    /// settlement's kit (its rectangle overlaps the kit's) counts as part of it for the spots: they are placed round the
    /// kit and that token together, so a town's name hangs just above the brigade standing in it rather than beside the
    /// whole town; how far the name is from its own town is still measured from the kit alone. A token is no settlement's marker, so it never hides a marker the way a panel does. The rectangles are
    /// re-read when the camera moves and whenever the layer hands over a new list (the tokens were rebuilt, or the "units"
    /// toggle changed).
    ///
    /// The 1948 localities (MapScene.Localities, owner rulings 2026-10-11) get smaller pills with their 1948 names, placed
    /// after every game settlement by the same rules and only nearer than LocalityDistanceM (farther out they are skipped
    /// outright, so the overview pays nothing for them); Hover shows today's name in a tooltip under the pill the pointer
    /// is over.</summary>
    public sealed class MapLabelsPresenter : IDisposable
    {
        public const string ContainerName = "map-labels", SlotClass = "map-label-slot", LabelClass = "map-label";
        public const float LiftPx = 6f, MarginPx = 4f, DeclutterDistanceM = 150000f;
        /// <summary>The 1948 localities are named only nearer than this (owner ruling 2026-10-11: names at closer zoom, so
        /// the map is not cluttered); farther out their pills are hidden and their places are nobody's neighbour.</summary>
        public const float LocalityDistanceM = 40000f;
        public const string LocalityClass = "map-label-locality", TooltipClass = "map-label-tooltip", TooltipName = "map-label-tooltip";
        /// <summary>Extra room a pill hidden in the last refresh needs before it is shown again (review M7).</summary>
        public const float HysteresisPx = 2f;
        /// <summary>How far outside a pill a tap still counts as on it (a fingertip is wider than a mouse pointer).</summary>
        public const float TapSlopPx = 8f;
        /// <summary>The most of a kit's screen rectangle a pill may cover (issue #214): its own kit's, and any other
        /// visible kit's.</summary>
        public const float MaxKitCover = 0.1f;
        /// <summary>The population-tier badge (issue #710) inside a city's or town's pill, and the class that makes room for it.</summary>
        public const string TierClass = "map-label-tier", WithTierClass = "map-label-with-tier";
        /// <summary>OWNER-APPROVED DESIGN VALUES (#710, 18 October 2026, "Accept as shipped"): tiers 1 to 9, doubling
        /// from 1,000 people (under 1,000 is tier 1, 1,000-1,999 tier 2, ..., 128,000 and over tier 9). The badge is at
        /// the start of the name, mirrored in Hebrew, and remains part of the pill at every zoom where the pill is shown.</summary>
        public const int TierBase = 1000, TierCount = 9;
        /// <summary>The badge's inset from its pill's edge (Statecraft.uss ".map-label-with-tier" leaves the room), and its width
        /// (".map-label-tier"); in the Hebrew UI the badge is inset from the right edge (owner, on #897).</summary>
        const float TierInsetPx = 7f, TierWidthPx = 23f;
        /// <summary>The panels a pill never sits under (Game.uxml names).</summary>
        public static readonly string[] OccluderNames = { "top-bar", "date-plate", "left-column", "info-panel", "minimap", "map-layer", "event-card" };

        const string Centre = "anchor-lower-centre", Left = "anchor-lower-left", Right = "anchor-lower-right";

        /// <summary>Where a pill sits against its kit's screen rectangle: above it (centred on it, starting at its centre,
        /// or ending at it), beside it, below it, or over it (its bottom centre above the settlement's point), or, for a
        /// city only and last in each of its passes, under the point (its top centre below it: issue #599, where the spots
        /// round a sprawl are all by a neighbour and the point is too near the top bar for Over).</summary>
        public enum Spot { AboveCentre, AboveLeft, AboveRight, BesideRight, BesideLeft, Below, Over, Under }
        static readonly Spot[] Fallbacks = { Spot.AboveRight, Spot.AboveLeft, Spot.AboveCentre, Spot.BesideRight, Spot.BesideLeft, Spot.Below, Spot.Over };
        static readonly HashSet<string> MinorTypes = new HashSet<string> { "village", "kibbutz", "moshav" };
        static readonly Dictionary<string, string> AnchorClass = new Dictionary<string, string>
        {
            ["jaffa"] = Right, ["rishon_lezion"] = Right, ["lod"] = Left,
            ["petah_tikva"] = Left, ["safed"] = Right, ["rosh_pinna"] = Left,
        };

        sealed class Item
        {
            public Settlement Settlement = null!; public Vector3 MarkerTop; public Bounds[] Kit = null!, Marker = null!; public VisualElement Slot = null!;
            public Label Label = null!; public Spot First; public bool IsCity, IsLocality, TopVisible, KitInFront, ShownLast = true;
            public Vector2 TopPanel; public Rect KitPanel, Around, Placed; public string? Tip;
        }

        readonly VisualElement container;
        readonly Camera camera;
        readonly MapScene map;
        readonly CameraRig? rig;
        readonly Func<float>? zoomDistance;
        readonly List<Item> items = new List<Item>();        // in placing (priority) order
        readonly List<Item> settlementOrder = new List<Item>();
        readonly List<VisualElement> occluders = new List<VisualElement>();
        readonly List<Rect> occluderRects = new List<Rect>();  // in the layer's coordinates
        readonly List<Rect> placed = new List<Rect>();
        readonly Func<IReadOnlyList<Bounds[]>?>? keepClear;     // the unit tokens' world boxes (UnitLayer.KeepClear), or none
        readonly List<Rect> keepClearRects = new List<Rect>(); // their screen rectangles, in the layer's coordinates
        IReadOnlyList<Bounds[]>? lastKeepClear;
        Rect layerRect;                                        // the panel in the layer's coordinates: a pill stays inside it
        readonly EventCallback<GeometryChangedEvent> onOccluderGeometry, onContainerGeometry;
        Matrix4x4 lastViewProjection; Rect lastCameraRect; Vector2 lastPanelSize, lastOrigin; float lastZoom = float.NaN;
        bool lastKitsShown;
        int lastOccluderMask = -1; float lastLabelSizes = float.NaN; bool sizesKnown, geometryDirty = true;
        Action? removeToggle;   // the labels switch's removal token (VisualToggles.Register): a disposed presenter never removes a newer one

        /// <summary>The game settlements' pills, in settlements_1948.yaml order.</summary>
        public IReadOnlyList<Label> Labels { get; }
        public IReadOnlyList<VisualElement> Slots { get; }
        /// <summary>The 1948 localities' pills (smaller, named only within LocalityDistanceM), in localities_1948.yaml order.</summary>
        public IReadOnlyList<Label> LocalityLabels { get; }
        public IReadOnlyList<VisualElement> LocalitySlots { get; }
        /// <summary>The tooltip of the locality whose pill the pointer is over: today's name where a living place carries on
        /// ("Today: Beit She'an" under "Beisan"; owner ruling 2026-10-12: none for a place that did not), "Mixed locality" for
        /// Nesher; hidden otherwise, and never for a pill with nothing to say. Ignores the pointer, like the pills.</summary>
        public Label Tooltip { get; }
        Item? tapped, shownFor;   // the pill a tap opened; the pill the tooltip shows now

        /// <summary>The static 1948 population tier of a city of <paramref name="people"/> (owner, 15 October 2026, on #398:
        /// it does not follow the sim's live population).</summary>
        public static int Tier(decimal people)
        {
            int tier = 1;
            for (decimal bar = TierBase; tier < TierCount && people >= bar; bar *= 2) tier++;
            return tier;
        }

        /// <summary>The default tier tooltip (tests; GameRoot passes the UI strings). Keep it equal to strings.en.yaml's map.tier.</summary>
        public static string DefaultTierTip(int tier, decimal people) =>
            string.Format(CultureInfo.InvariantCulture, "Population tier {0} of {1}: {2:N0} people in 1948", tier, TierCount, people);

        /// <summary>The default tooltip text (tests; GameRoot passes the UI strings): null when there is nothing to say.</summary>
        public static string? DefaultTip(Statecraft.Game.Map.Kits.Localities.Place p) =>
            TipOf(p, n => "Today: " + n, "Mixed locality");

        /// <summary>A locality's tooltip: "Mixed locality" for a mixed one, then today's name where a living place carries on
        /// (Localities.TodayName, the rule the info panel shares); null when neither.</summary>
        public static string? TipOf(Statecraft.Game.Map.Kits.Localities.Place p, Func<string, string> today, string mixed, bool hebrew = false)
        {
            var lines = new List<string>();
            if (p.Mixed) lines.Add(mixed);
            var name = Statecraft.Game.Map.Kits.Localities.TodayName(p, hebrew);
            if (name != null) lines.Add(today(name));
            return lines.Count == 0 ? null : string.Join("\n", lines);
        }

        public MapLabelsPresenter(VisualElement root, MapScene map, Camera camera, Func<float>? zoomDistance = null, Func<Statecraft.Game.Map.Kits.Localities.Place, string?>? tipText = null,
            Func<IReadOnlyList<Bounds[]>?>? keepClear = null, IReadOnlyDictionary<string, decimal>? population = null,
            Func<int, decimal, string>? tierTip = null, bool hebrew = false)
        {
            var tip = tipText ?? DefaultTip;
            this.keepClear = keepClear;
            container = root.Q<VisualElement>(ContainerName) ?? throw new InvalidOperationException($"Game.uxml has no '{ContainerName}' element");
            this.camera = camera;
            this.map = map;
            this.zoomDistance = zoomDistance;
            rig = camera.TryGetComponent(out CameraRig r) ? r : null;
            container.Clear();   // one layer, one owner: the newest presenter (GameRoot makes one per Begin)
            var labels = new List<Label>(); var slots = new List<VisualElement>();
            foreach (var s in map.Settlements)
            {
                var slot = new VisualElement { name = "slot-" + s.Id, pickingMode = PickingMode.Ignore };
                slot.AddToClassList(SlotClass);
                var anchorClass = AnchorClass.TryGetValue(s.Id, out var a) ? a : Centre;
                // #880: the Hebrew UI shows the row's name_he, and the English name where the row has none.
                var label = new Label(hebrew && !string.IsNullOrWhiteSpace(s.NameHe) ? s.NameHe : s.Name) { name = "label-" + s.Id, pickingMode = PickingMode.Ignore };
                label.AddToClassList(LabelClass);
                label.AddToClassList("map-label-" + s.Type);
                label.AddToClassList(anchorClass);
                slot.Add(label);
                // Issue #710: a pill with a 1948 figure (cities.yaml's people) starts with its tier; the rest have no badge.
                // The badge is the pill's sibling, drawn over its start padding (left, or right in Hebrew): a child would stop the Label sizing to its text.
                string? tierText = null;
                if (population != null && population.TryGetValue(s.Id, out var people))
                {
                    int tier = Tier(people);
                    var badge = new Label(tier.ToString(CultureInfo.InvariantCulture)) { name = "tier-" + s.Id, pickingMode = PickingMode.Ignore };
                    badge.AddToClassList(TierClass);
                    label.AddToClassList(WithTierClass);
                    slot.Add(badge);
                    // The pill's USS anchor translates it by a share of its own size, so the badge follows its laid-out size.
                    float anchorShare = anchorClass == Left ? 0f : anchorClass == Right ? 1f : 0.5f;
                    label.RegisterCallback<GeometryChangedEvent>(e =>
                    {
                        // Hebrew reads from the right, so the badge starts the name there (owner, 17 Oct 2026, on #897).
                        badge.style.left = -anchorShare * e.newRect.width + (hebrew ? e.newRect.width - TierInsetPx - TierWidthPx : TierInsetPx);
                        badge.style.top = -0.5f * e.newRect.height;
                    });
                    tierText = (tierTip ?? DefaultTierTip)(tier, people);
                }
                container.Add(slot);
                labels.Add(label); slots.Add(slot);
                var top = MarkerTop(map, s);
                settlementOrder.Add(new Item
                {
                    Settlement = s, MarkerTop = top, Kit = KitBounds(map, s, top), Marker = MarkerBox(s, top), Slot = slot, Label = label, IsCity = s.Type == "city", Tip = tierText,
                    First = anchorClass == Left ? Spot.AboveLeft : anchorClass == Right ? Spot.AboveRight : Spot.AboveCentre,
                });
            }
            // Stable sort: priority, then file order.
            for (int p = 0; p <= 2; p++)
                foreach (var it in settlementOrder) if (Priority(it.Settlement.Type) == p) items.Add(it);
            Labels = labels; Slots = slots;
            // The 1948 localities after every game settlement, largest first (the file's order): presentation only.
            var localityLabels = new List<Label>(); var localitySlots = new List<VisualElement>();
            foreach (var place in map.Localities)
            {
                var s = place.Settlement;
                var slot = new VisualElement { name = "slot-" + s.Id, pickingMode = PickingMode.Ignore };
                slot.AddToClassList(SlotClass);
                var label = new Label(hebrew && !string.IsNullOrWhiteSpace(place.NameHe) ? place.NameHe : s.Name) { name = "label-" + s.Id, pickingMode = PickingMode.Ignore };
                label.AddToClassList(LabelClass); label.AddToClassList(LocalityClass); label.AddToClassList(Centre);
                slot.Add(label);
                container.Add(slot);
                localityLabels.Add(label); localitySlots.Add(slot);
                var top = MarkerTop(map, s);
                var it = new Item
                {
                    Settlement = s, MarkerTop = top, Kit = KitBounds(map, s, top), Marker = MarkerBox(s, top), Slot = slot, Label = label,
                    First = Spot.AboveCentre, IsLocality = true, Tip = tip(place),
                };
                settlementOrder.Add(it); items.Add(it);
            }
            LocalityLabels = localityLabels; LocalitySlots = localitySlots;
            Tooltip = new Label { name = TooltipName, pickingMode = PickingMode.Ignore };
            Tooltip.AddToClassList(TooltipClass);
            Tooltip.style.visibility = Visibility.Hidden;
            container.Add(Tooltip);

            onOccluderGeometry = _ => { geometryDirty = true; Refresh(force: true); };   // a panel that appears hides the pills it covers before the repaint (re-review N1)
            onContainerGeometry = _ => { geometryDirty = true; Refresh(force: true); };   // a resize is placed before the repaint (review M4)
            foreach (var name in OccluderNames)
            {
                var e = root.Q<VisualElement>(name);
                if (e == null) continue;
                occluders.Add(e);
                e.RegisterCallback(onOccluderGeometry);
            }
            container.RegisterCallback(onContainerGeometry);
            // On clears the inline display, so the loading rule (".loading .map-labels") still wins (review M1).
            removeToggle = VisualToggles.Register("labels", on => container.style.display = on ? new StyleEnum<DisplayStyle>(StyleKeyword.Null) : DisplayStyle.None);
            Refresh(force: true);
        }

        /// <summary>The top of a settlement's marker box: the point a pill over its kit hangs from, and the point that says
        /// whether the place is on screen.</summary>
        public static Vector3 MarkerTop(MapScene map, Settlement s)
        {
            var p = map.Frame.ToWorld(s.Lon, s.Lat, 0);
            p.y = map.Terrain.SampleHeight(p) + map.Terrain.transform.position.y + SettlementPlacer.MarkerHeightM(s.Type);
            return p;
        }

        /// <summary>The world boxes a settlement's pill keeps clear of while the kits are drawn: its kit's columns, or
        /// without a kit its marker box.</summary>
        public static Bounds[] KitBounds(MapScene map, Settlement s, Vector3 markerTop) =>
            map.TryKitColumns(s.Id, out var columns) ? columns : MarkerBox(s, markerTop);

        readonly List<Item> siteItems = new List<Item>();

        /// <summary>GDD Phase 1 PR 8 (#381): the built sites' pills (SiteLayer.Places), replacing the last ones given. Each is
        /// placed like a kibbutz's: after the game settlements and before the 1948 localities, against its kit's columns
        /// (SiteLayer.Columns) or, with none, its marker box, under the same declutter and clearance rules as every pill.</summary>
        public void SetSites(IReadOnlyList<(Settlement place, Bounds[]? columns)> sites)
        {
            foreach (var it in siteItems) { it.Slot.RemoveFromHierarchy(); items.Remove(it); settlementOrder.Remove(it); }
            siteItems.Clear();
            var at = items.FindIndex(x => x.IsLocality);   // the sites go before the localities
            if (at < 0) at = items.Count;
            foreach (var (s, columns) in sites)
            {
                var slot = new VisualElement { name = "slot-" + s.Id, pickingMode = PickingMode.Ignore };
                slot.AddToClassList(SlotClass);
                var label = new Label(s.Name) { name = "label-" + s.Id, pickingMode = PickingMode.Ignore };
                label.AddToClassList(LabelClass); label.AddToClassList("map-label-" + s.Type); label.AddToClassList(Centre);
                slot.Add(label);
                container.Insert(System.Math.Max(0, container.IndexOf(Tooltip)), slot);   // under the tooltip, as the others
                var top = MarkerTop(map, s);
                var it = new Item { Settlement = s, MarkerTop = top, Kit = columns ?? MarkerBox(s, top), Marker = MarkerBox(s, top), Slot = slot, Label = label, First = Spot.AboveCentre };
                items.Insert(at++, it); settlementOrder.Add(it); siteItems.Add(it);
            }
            geometryDirty = true;
        }

        /// <summary>The built sites' pills (SetSites), in the order given.</summary>
        public IReadOnlyList<Label> SiteLabels { get { var l = new List<Label>(); foreach (var x in siteItems) l.Add(x.Label); return l; } }

        /// <summary>A settlement's marker box (what is drawn while the "kits" toggle is off).</summary>
        public static Bounds[] MarkerBox(Settlement s, Vector3 markerTop)
        {
            float h = SettlementPlacer.MarkerHeightM(s.Type);
            return new[] { new Bounds(markerTop - Vector3.up * (h / 2f), new Vector3(h, h, h)) };
        }

        /// <summary>The pill's rectangle at <paramref name="spot"/> against the kit's screen rectangle <paramref name="kit"/>
        /// (LiftPx clear of it); Over hangs it LiftPx above <paramref name="point"/> instead, Under LiftPx below it.</summary>
        public static Rect SpotRect(Spot spot, Rect kit, Vector2 point, Vector2 size)
        {
            float above = kit.yMin - LiftPx - size.y, cx = kit.center.x, cy = kit.center.y;
            return spot switch
            {
                Spot.AboveCentre => new Rect(cx - size.x / 2f, above, size.x, size.y),
                Spot.AboveLeft => new Rect(cx, above, size.x, size.y),
                Spot.AboveRight => new Rect(cx - size.x, above, size.x, size.y),
                Spot.BesideRight => new Rect(kit.xMax + LiftPx, cy - size.y / 2f, size.x, size.y),
                Spot.BesideLeft => new Rect(kit.xMin - LiftPx - size.x, cy - size.y / 2f, size.x, size.y),
                Spot.Below => new Rect(cx - size.x / 2f, kit.yMax + LiftPx, size.x, size.y),
                Spot.Under => new Rect(point.x - size.x / 2f, point.y + LiftPx, size.x, size.y),
                _ => new Rect(point.x - size.x / 2f, point.y - LiftPx - size.y, size.x, size.y),
            };
        }

        /// <summary>The share of <paramref name="kit"/>'s area that <paramref name="pill"/> covers (0 for an empty kit).</summary>
        public static float Cover(Rect pill, Rect kit)
        {
            float w = Mathf.Min(pill.xMax, kit.xMax) - Mathf.Max(pill.xMin, kit.xMin), h = Mathf.Min(pill.yMax, kit.yMax) - Mathf.Max(pill.yMin, kit.yMin);
            float area = kit.width * kit.height;
            return w <= 0f || h <= 0f || area <= 0f ? 0f : w * h / area;
        }

        /// <summary>Placing order: lower first. Cities, then towns and Arab towns, then villages, kibbutzim and moshavim.</summary>
        public static int Priority(string type) => type == "city" ? 0 : type == "town" || type == "arab_town" ? 1 : 2;

        public static bool HiddenByZoom(string type, float distance) => MinorTypes.Contains(type) && distance > DeclutterDistanceM;

        /// <summary>The pill's panel rectangle when its anchor point is <paramref name="at"/> (the anchor class says which corner
        /// of the pill sits there, USS translate).</summary>
        public static Rect PillRect(Vector2 at, Vector2 size, string anchorClass) => anchorClass switch
        {
            Left => new Rect(at.x, at.y - size.y, size.x, size.y),
            Right => new Rect(at.x - size.x, at.y - size.y, size.x, size.y),
            _ => new Rect(at.x - size.x / 2f, at.y - size.y, size.x, size.y),
        };

        float Zoom() => zoomDistance != null ? zoomDistance() : rig != null ? rig.Distance : 0f;

        int OccluderMask()
        {
            int mask = 0;
            for (int i = 0; i < occluders.Count; i++)
            {
                var s = occluders[i].resolvedStyle;
                if (s.display != DisplayStyle.None && s.visibility == Visibility.Visible) mask |= 1 << i;
            }
            return mask;
        }

        public void Refresh(bool force = false)
        {
            var panel = container.panel;
            if (panel == null || camera == null) return;
            var viewProjection = camera.projectionMatrix * camera.worldToCameraMatrix;
            var cameraRect = camera.rect;
            var screen = panel.visualTree.layout.size;    // the whole panel, which the camera's view fills
            var origin = container.worldBound.position;   // slots translate from the layer's top-left
            float distance = Zoom();
            bool kitsShown = map != null && map.KitsShown;   // with the "kits" toggle off the marker boxes are drawn instead
            int mask = OccluderMask();
            float labelSizes = 0f;   // a pill that grows or shrinks (a late font, a new text) is re-placed (re-review N2)
            bool localities = distance <= LocalityDistanceM;   // farther out the localities are skipped outright (cost)
            var tokens = keepClear?.Invoke();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IsLocality && !localities) continue;
                var st = items[i].Label.resolvedStyle; if (!float.IsNaN(st.width) && !float.IsNaN(st.height)) labelSizes += st.width + st.height;
            }
            if (!force && sizesKnown && !geometryDirty && labelSizes == lastLabelSizes && viewProjection == lastViewProjection && cameraRect == lastCameraRect
                && screen == lastPanelSize && origin == lastOrigin && distance == lastZoom && mask == lastOccluderMask && kitsShown == lastKitsShown
                && tokens == lastKeepClear) return;
            lastKeepClear = tokens;
            lastViewProjection = viewProjection; lastCameraRect = cameraRect; lastPanelSize = screen; lastOrigin = origin;
            lastZoom = distance; lastOccluderMask = mask; lastKitsShown = kitsShown; lastLabelSizes = labelSizes; geometryDirty = false;
            sizesKnown = true;
            bool laidOut = !(float.IsNaN(screen.x) || screen.x <= 0f || float.IsNaN(origin.x) || float.IsNaN(origin.y));
            if (!laidOut) { sizesKnown = false; origin = Vector2.zero; }   // before the first layout: cull by view only, place later

            layerRect = new Rect(-origin, screen);
            occluderRects.Clear();
            if (laidOut)
                for (int i = 0; i < occluders.Count; i++)
                    if ((mask & (1 << i)) != 0)
                    {
                        var b = occluders[i].worldBound;
                        if (!float.IsNaN(b.x) && b.width > 0f) occluderRects.Add(new Rect(b.position - origin, b.size));
                    }
            keepClearRects.Clear();
            if (tokens != null)
                for (int i = 0; i < tokens.Count; i++)
                    if (ProjectBoxes(tokens[i], viewProjection, cameraRect, screen, origin, out var r)) keepClearRects.Add(r);

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.IsLocality && !localities) { it.TopVisible = it.KitInFront = false; continue; }
                var t = camera.WorldToViewportPoint(it.MarkerTop);
                it.TopPanel = ToPanel(t, cameraRect, screen, origin);
                it.TopVisible = t.z > 0f && t.x >= 0f && t.x <= 1f && t.y >= 0f && t.y <= 1f && !UnderAPanel(it.TopPanel);   // a marker under a panel is no one's neighbour
                it.KitInFront = ProjectBoxes(kitsShown ? it.Kit : it.Marker, viewProjection, cameraRect, screen, origin, out it.KitPanel);
                // A kit reaching behind the camera at the closest zoom has no screen rectangle: any settlement's pill is then
                // placed round its marker box, as with the kits off, rather than hidden (second review of #599: Tel Aviv at 6 km).
                if (!it.KitInFront && kitsShown) it.KitInFront = ProjectBoxes(it.Marker, viewProjection, cameraRect, screen, origin, out it.KitPanel);
                it.Around = it.KitPanel;   // the kit, and every token that stands on it: the spots are placed round both
                for (int k = 0; k < keepClearRects.Count; k++)
                    if (keepClearRects[k].Overlaps(it.KitPanel)) it.Around = Rect.MinMaxRect(Mathf.Min(it.Around.xMin, keepClearRects[k].xMin),
                        Mathf.Min(it.Around.yMin, keepClearRects[k].yMin), Mathf.Max(it.Around.xMax, keepClearRects[k].xMax), Mathf.Max(it.Around.yMax, keepClearRects[k].yMax));
            }

            placed.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.IsLocality && !localities)
                {
                    if (it.ShownLast) { it.ShownLast = false; it.Slot.style.visibility = Visibility.Hidden; }
                    continue;
                }
                bool shown = it.KitInFront && it.TopVisible && (it.IsLocality || !HiddenByZoom(it.Settlement.Type, distance));
                var at = new Vector2(it.KitPanel.center.x, it.KitPanel.yMin - LiftPx);   // the slot: the USS anchor puts the pill above the kit
                var shift = Vector2.zero;
                if (shown)
                {
                    var size = new Vector2(it.Label.resolvedStyle.width, it.Label.resolvedStyle.height);
                    if (!laidOut || float.IsNaN(size.x) || size.x <= 0f) sizesKnown = false;   // not laid out yet: show it, place on a later refresh
                    else
                    {
                        float extra = it.ShownLast ? 0f : HysteresisPx;
                        var home = SpotRect(it.First, it.KitPanel, it.TopPanel, size);   // where the USS anchor class draws it
                        bool found = TryPlace(it, size, home, it.First, extra, true, true, ref shift);
                        for (int k = 0; !found && k < Fallbacks.Length; k++)
                            if (Fallbacks[k] != it.First) found = TryPlace(it, size, home, Fallbacks[k], extra, true, true, ref shift);
                        // A city keeps its name (review of #518): next round its kit alone, still reading as its own but
                        // over a token if need be; then clear of the tokens but not of its neighbours; then neither; last
                        // (second review of #599), nearer its own marker than any other game settlement's, the 1948
                        // localities' markers aside (they are placed after it and keep clear of its pill). Each pass ends
                        // with Under, which only a city takes.
                        for (int pass = 0; !found && it.IsCity && pass < 4; pass++)
                        {
                            bool neighbours = pass == 0, avoidTokens = pass == 1, countLocalities = pass < 3;
                            found = TryPlace(it, size, home, it.First, extra, neighbours, avoidTokens, ref shift, countLocalities);
                            for (int k = 0; !found && k < Fallbacks.Length; k++)
                                if (Fallbacks[k] != it.First) found = TryPlace(it, size, home, Fallbacks[k], extra, neighbours, avoidTokens, ref shift, countLocalities);
                            if (!found) found = TryPlace(it, size, home, Spot.Under, extra, neighbours, avoidTokens, ref shift, countLocalities);
                        }
                        shown = found;
                    }
                }
                it.ShownLast = shown;
                it.Slot.style.visibility = shown ? Visibility.Visible : Visibility.Hidden;
                if (shown && laidOut) it.Slot.style.translate = new Translate(at.x + shift.x, at.y + shift.y);
            }
        }

        /// <summary>Whether the pill fits at <paramref name="spot"/>; if so records it as placed and sets the shift from
        /// its USS anchor's rectangle <paramref name="home"/>. With <paramref name="clearOfTokens"/> the spot is placed
        /// round the kit and the tokens on it and keeps clear of every token; without, round the kit alone. Without
        /// <paramref name="localitiesCount"/> the 1948 localities' markers do not count as others for the own-marker rule
        /// (a city's last pass).</summary>
        bool TryPlace(Item it, Vector2 size, Rect home, Spot spot, float extra, bool clearOfNeighbours, bool clearOfTokens, ref Vector2 shift,
            bool localitiesCount = true)
        {
            var r = SpotRect(spot, clearOfTokens ? it.Around : it.KitPanel, it.TopPanel, size);
            float m = MarginPx + extra;
            var grown = new Rect(r.x - m, r.y - m, r.width + 2f * m, r.height + 2f * m);
            if (r.xMin < layerRect.xMin || r.yMin < layerRect.yMin || r.xMax > layerRect.xMax || r.yMax > layerRect.yMax) return false;
            for (int i = 0; i < occluderRects.Count; i++) if (occluderRects[i].Overlaps(r)) return false;
            if (clearOfTokens) for (int i = 0; i < keepClearRects.Count; i++) if (keepClearRects[i].Overlaps(grown)) return false;
            for (int i = 0; i < placed.Count; i++) if (placed[i].Overlaps(grown)) return false;
            if (Cover(r, it.KitPanel) > MaxKitCover) return false;
            // Issue #599: in every pass the pill is nearer its own marker than any other visible marker, by more than
            // MarginPx. Two sprawling kits' rectangles overlap and hold each other's markers, so a spot against one's
            // edge can sit by the other's town.
            float mine = Gap(r, it.TopPanel) + MarginPx + extra;
            float need = 2f * Gap(r, it.KitPanel) + MarginPx + extra;   // owned by the town itself, never by the tokens on it
            for (int i = 0; i < items.Count; i++)
            {
                var other = items[i];
                if (other == it || !other.TopVisible) continue;
                float gap = Gap(r, other.TopPanel);
                if (gap <= mine && (localitiesCount || !other.IsLocality)) return false;
                if (clearOfNeighbours && (gap <= need || (other.KitInFront && Cover(r, other.KitPanel) > MaxKitCover))) return false;
            }
            placed.Add(r);
            it.Placed = r;
            shift = r.position - home.position;
            return true;
        }

        /// <summary>The pointer position the hover tooltip may follow, or null: a touchscreen has no hover (the Input System
        /// keeps its last touch position after the finger lifts, which would leave a tooltip up), so a touch is answered by
        /// <see cref="Tap"/> instead. A mouse, a pen or any other pointer hovers. Decided by the device type, not guessed.</summary>
        public static Vector2? HoverPosition(UnityEngine.InputSystem.Pointer? pointer) =>
            pointer == null || pointer is UnityEngine.InputSystem.Touchscreen ? (Vector2?)null : pointer.position.ReadValue();

        /// <summary>Shows today's name under the locality pill at <paramref name="screenYUp"/> (an Input System pointer position,
        /// y up), or hides the hover tooltip (null: no hovering pointer, see <see cref="HoverPosition"/>). A position means a
        /// mouse is in use, so a tooltip a tap left up goes. Called after Refresh each frame (GameRoot.LateUpdate); it also
        /// keeps a tapped tooltip under its pill as the camera moves; allocates only when the shown pill changes.</summary>
        public void Hover(Vector2? screenYUp)
        {
            var panel = container.panel;
            if (screenYUp.HasValue && panel != null)
            {
                tapped = null;
                HoverAt(UiPointer.ToPanel(panel, screenYUp.Value, Screen.height) - container.worldBound.position);
                return;
            }
            ShowTooltipFor(tapped);
        }

        /// <summary>A touch at <paramref name="screenYUp"/> (a Touchscreen position, y up): on a locality pill it toggles that
        /// pill's tooltip (a second tap on it hides it); anywhere else it hides the tooltip.</summary>
        public void Tap(Vector2 screenYUp)
        {
            var panel = container.panel;
            if (panel == null) { TapAt(new Vector2(float.NaN, float.NaN)); return; }
            TapAt(UiPointer.ToPanel(panel, screenYUp, Screen.height) - container.worldBound.position);
        }

        /// <summary>The hit test under Tap, at a point in the labels layer's coordinates. The pill is grown by TapSlopPx,
        /// as a fingertip is wider than a mouse pointer.</summary>
        public void TapAt(Vector2 layerPoint)
        {
            Item? hit = null;
            for (int i = 0; i < items.Count && hit == null; i++)
                if (Hittable(items[i]) && Grow(items[i].Placed, TapSlopPx).Contains(layerPoint)) hit = items[i];
            tapped = hit != null && hit != tapped ? hit : null;
            ShowTooltipFor(tapped);
        }

        static Rect Grow(Rect r, float by) => new Rect(r.x - by, r.y - by, r.width + 2f * by, r.height + 2f * by);
        static bool Hittable(Item it) => it.Tip != null && it.ShownLast;   // a locality with something to say, or a pill with a tier (#710)

        /// <summary>The hit test under Hover, at a point in the labels layer's coordinates: the shown locality pill (with
        /// something to say) that contains it, or none.</summary>
        public void HoverAt(Vector2 layerPoint)
        {
            Item? over = null;
            for (int i = 0; i < items.Count && over == null; i++)
            {
                var it = items[i];
                if (Hittable(it) && it.Placed.Contains(layerPoint)) over = it;
            }
            ShowTooltipFor(over);
        }

        /// <summary>Hover by locality id (tests): shows its tooltip when its pill is shown.</summary>
        public void HoverLocality(string? id)
        {
            Item? over = null;
            foreach (var it in items) if (Hittable(it) && it.Settlement.Id == id) over = it;
            ShowTooltipFor(over);
        }

        void ShowTooltipFor(Item? over)
        {
            if (over != null && !Hittable(over)) { over = null; if (tapped != null && !Hittable(tapped)) tapped = null; }   // its pill went (zoom, a panel)
            if (over == null) { if (shownFor != null) { Tooltip.style.visibility = Visibility.Hidden; shownFor = null; } return; }
            if (over != shownFor) { Tooltip.text = over.Tip; shownFor = over; }
            // left/top, not an inline translate: that would replace the USS translate (-50%) that centres it under the pill.
            Tooltip.style.left = over.Placed.center.x; Tooltip.style.top = over.Placed.yMax + 2f;
            Tooltip.style.visibility = Visibility.Visible;
        }

        bool UnderAPanel(Vector2 p)
        {
            for (int i = 0; i < occluderRects.Count; i++) if (occluderRects[i].Contains(p)) return true;
            return false;
        }

        /// <summary>The screen rectangle of world boxes (their corners' hull in the layer's coordinates), projected with the
        /// camera's view-projection as Camera.WorldToViewportPoint does; false when a corner is behind the camera.</summary>
        static bool ProjectBoxes(Bounds[] boxes, Matrix4x4 vp, Rect cameraRect, Vector2 screen, Vector2 origin, out Rect rect)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int k = 0; k < boxes.Length; k++)
            {
                var min = boxes[k].min; var max = boxes[k].max;
                for (int c = 0; c < 8; c++)
                {
                    float x = (c & 1) == 0 ? min.x : max.x, y = (c & 2) == 0 ? min.y : max.y, z = (c & 4) == 0 ? min.z : max.z;
                    float w = vp.m30 * x + vp.m31 * y + vp.m32 * z + vp.m33;
                    if (w <= 0f) { rect = default; return false; }
                    var v = new Vector3(((vp.m00 * x + vp.m01 * y + vp.m02 * z + vp.m03) / w + 1f) * 0.5f, ((vp.m10 * x + vp.m11 * y + vp.m12 * z + vp.m13) / w + 1f) * 0.5f, w);
                    var p = ToPanel(v, cameraRect, screen, origin);
                    x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
                }
            }
            rect = Rect.MinMaxRect(x0, y0, x1, y1);
            return true;
        }

        /// <summary>Viewport to the layer's coordinates through the camera's own rect, not
        /// RuntimePanelUtils.CameraTransformWorldToPanel: that flips y with Screen.height, which is wrong whenever the camera
        /// draws to a texture of another size (edit-mode tests, captures).</summary>
        static Vector2 ToPanel(Vector3 v, Rect cameraRect, Vector2 screen, Vector2 origin) =>
            new Vector2((cameraRect.x + v.x * cameraRect.width) * screen.x, (1f - (cameraRect.y + v.y * cameraRect.height)) * screen.y) - origin;

        static float Gap(Rect r, Vector2 p) =>
            new Vector2(Mathf.Max(0f, Mathf.Max(r.xMin - p.x, p.x - r.xMax)), Mathf.Max(0f, Mathf.Max(r.yMin - p.y, p.y - r.yMax))).magnitude;

        /// <summary>The distance between two rectangles (0 when they touch or overlap).</summary>
        static float Gap(Rect a, Rect b) =>
            new Vector2(Mathf.Max(0f, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax)), Mathf.Max(0f, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax))).magnitude;

        /// <summary>Removes only this presenter's own pills and callbacks, so an older presenter disposed after a newer one
        /// was made leaves the newer one's pills (review M2).</summary>
        public void Dispose()
        {
            removeToggle?.Invoke(); removeToggle = null;
            foreach (var e in occluders) e.UnregisterCallback(onOccluderGeometry);
            container.UnregisterCallback(onContainerGeometry);
            occluders.Clear();
            foreach (var slot in Slots) slot.RemoveFromHierarchy();
            foreach (var slot in LocalitySlots) slot.RemoveFromHierarchy();
            foreach (var it in siteItems) it.Slot.RemoveFromHierarchy();
            siteItems.Clear();
            Tooltip.RemoveFromHierarchy();
            items.Clear(); settlementOrder.Clear();
        }
    }
}
