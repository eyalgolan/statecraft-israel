#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Fills the top bar from HudModel and runs End Turn.</summary>
    public sealed class HudPresenter : System.IDisposable
    {
        readonly VisualElement root; readonly SimBridge bridge; readonly UiText text;
        readonly Button endTurn; readonly System.Action onEndTurn;
        readonly VisualElement bar, hudRoot, nav;
        readonly System.Collections.Generic.List<VisualElement> meters;
        /// <summary>Each meter's element and its caption in the UI's language, for the words in its tooltip (#398).</summary>
        readonly System.Collections.Generic.Dictionary<string, (VisualElement element, string caption)> said =
            new System.Collections.Generic.Dictionary<string, (VisualElement, string)>();

        // Three tiers by the bar's logical width (1080p reference, match 0.5: the panel's scale is the plain mean of the two
        // ratios, (w/1920 + h/1080) / 2, so logical width = w / that; TopBarTests logs it). HUD round 2 moved the four screen
        // buttons to the left column (#602), about 248 px out of the bar, so every tier starts narrower (part 3). With six
        // meters the one-row edge sat between a fullscreen 5:4 and 4:3 (1640; review of #624).
        // Social cohesion made seven meters (#380). The owner chose "tighten and re-tier" (16 October 2026): the meters' padding
        // is about 6 px narrower, and the edges move to the seven meters' measured widths (widest values, the Hebrew bar the
        // widest: one row about 1285 px of meters, stacked about 1055, without icons about 811, each with 5% to spare, beside
        // about 484 px of plate, turn box and padding). 16:9 at 1920 keeps the one-row bar; 16:10, 4:3, and 1280x720 and
        // 1366x768 (where the text floor raises the captions) take the stacked bar; 5:4 drops the icons; a square window
        // takes two rows; a phone's meters wrap to two lines. TopBarTests holds one row at 1865 and the stacked bar at 1855:
        //   21:9 ~2217 | 16:9 1920 | NarrowBelowPx 1860 | 16:10 ~1819 | 4:3 ~1646 | CompactBelowPx 1640 | 5:4 ~1585
        //   | TwoRowBelowPx 1400 | square ~1382 | half a 1080p screen (960x1080) ~1280.
        // PromoHudTests.EveryWindowShapeFitsTheBarAfterATurn proves each tier holds the widest late-game values and changes.

        /// <summary>Below this width the HUD's root gets NarrowClass: a taller bar (124 px) whose meters stack the change under
        /// the value, and the panels below it move down to match (Statecraft.uss .hud-narrow). Above it, the 70 px one-row bar
        /// (promo 03's 6.5%) holds seven values and their changes side by side with the icons.</summary>
        public const float NarrowBelowPx = 1860f;
        public const string NarrowClass = "hud-narrow";
        /// <summary>Below this width the meters also drop their icons (Statecraft.uss .top-bar--compact): a square window does not
        /// have the room for them on the stacked bar.</summary>
        public const float CompactBelowPx = 1640f;
        public const string CompactClass = "top-bar--compact";
        /// <summary>Below this width (windows narrower than about square, e.g. a browser snapped to half a 1080p screen, about
        /// 1360 logical px, and a square one, about 1382) one row cannot hold the badge, seven meters, New Game and End Turn: the root also gets
        /// TwoRowClass, and the bar wraps into two rows, the date and the meters above, the screen buttons and End Turn
        /// below (Statecraft.uss .hud-two-row).</summary>
        public const float TwoRowBelowPx = 1400f;
        public const string TwoRowClass = "hud-two-row";
        /// <summary>Below this width (a phone held upright: 390x844 is about 790 logical px) the badge and the meters do not
        /// fit one row either: the root also gets ThreeRowClass, and the bar takes three rows, the badge, the meters (seven,
        /// wrapped to two lines, #380), then the screen buttons and End Turn (Statecraft.uss .hud-three-row; #210).</summary>
        public const float ThreeRowBelowPx = 1100f;
        public const string ThreeRowClass = "hud-three-row";
        /// <summary>On the one-row bar, when the meters leave enough room, the bar gets RoomyClass: promo-sized icons (38 px)
        /// and values (25 px) (Statecraft.uss .top-bar--roomy). It turns on only when the measured spare room covers the
        /// predicted growth (RoomyGrowth) plus SpareShare, and off when the spare room falls below SpareShare, so it never
        /// flips back and forth.</summary>
        public const string RoomyClass = "top-bar--roomy";
        /// <summary>The share of the meters' width kept free after them (room for the browser's text measuring wider than the
        /// editor's; PromoHudTests.EveryWindowShapeFitsTheBarAfterATurn asserts it).</summary>
        public const float SpareShare = 0.05f;

        /// <summary>The meters' predicted growth when roomy: each of the <paramref name="icons"/> shown icons 30 -> 38 px (six,
        /// or seven with Social cohesion in the GDD loop), values 22 -> 25 px (about 14% wider), and each wrapped caption back
        /// to its one-line width (<paramref name="captionGrowth"/>).</summary>
        public static float RoomyGrowth(float valueWidths, float captionGrowth = 0f, int icons = 6) => icons * 8f + 0.14f * valueWidths + captionGrowth;

        /// <summary>The top bar's static texts and their strings (#193): the screen buttons, New Game and End Turn. The
        /// meters' captions are hud.caption.*, the same words their tooltips say.</summary>
        static readonly (string name, string key)[] Texts =
        {
            ("nav-cabinet", "hud.nav.cabinet"), ("nav-budget", "hud.nav.budget"), ("nav-defence", "hud.nav.defence"),
            ("nav-why", "hud.nav.why"), ("nav-settings", "hud.nav.settings"), ("new-game", "hud.new_game"), ("end-turn", "hud.end_turn"),
        };

        /// <summary>The owner's text floor (#624, 15 October 2026): the bar's small text (the seven captions, Security's level
        /// and the attention line, SmallTextPx in the stylesheet) stays at least this many pixels on screen. The panel scales
        /// with the window (about 0.67 at 1280x720, 0.62 at 1024x768), so at small windows the text is set larger in panel
        /// px (SmallTextSize).</summary>
        public const float TextFloorPx = 12f;
        /// <summary>The bar's small text in the stylesheet (Statecraft.uss .meter-name, #security-level, #attention).</summary>
        public const float SmallTextPx = 15f;

        /// <summary>The small text's size in panel px when one panel px is <paramref name="screenPxPerPanelPx"/> pixels on
        /// screen: the stylesheet's SmallTextPx, or the whole px that keeps it at TextFloorPx on screen if that is larger.</summary>
        public static float SmallTextSize(float screenPxPerPanelPx) =>
            screenPxPerPanelPx > 0f ? UnityEngine.Mathf.Max(SmallTextPx, UnityEngine.Mathf.Ceil(TextFloorPx / screenPxPerPanelPx - 0.01f)) : SmallTextPx;

        readonly List<Label> smallText;
        /// <summary>The plate's turn and date line (Statecraft.uss .date-row Label, 15 px): raised with the bar's small text (the
        /// owner's second answer, 15 October 2026). The plate keeps its width; the turn and the date take a line each, and the
        /// plate grows.</summary>
        readonly List<Label> plateText;
        readonly VisualElement? dateRow;
        /// <summary>The plate's padding while the floor raises its text (the stylesheet's is 2 px top and bottom, 10-12 at the
        /// sides): PlateSidePx at both sides keeps every line at least 0 px from the 18 px corner zones (TopBarTests), each the
        /// corner scrolls' reach (about 14 px in) plus 4, and PlateEdgePx at the top and bottom keeps it inside the frame's
        /// inner rule (7 px in). The plate grows to
        /// hold them (its height is a minimum), rather than shift its lines.</summary>
        public const float PlateSidePx = 18f, PlateEdgePx = 9f;
        readonly VisualElement turnBox;
        readonly IVisualElementScheduledItem floorCheck;
        float smallTextSize = SmallTextPx;

        /// <summary>The left icon column (promo 03): the screens' buttons (ScreenNav.Screens).</summary>
        public const string RailName = "left-column";
        readonly HoverTip? railTips, barTips;

        /// <summary>The panels under the bar (Statecraft.uss: top 456 px under the three-row bar), which PlaceUnderBar moves
        /// down by what the three-row bar grows past its 440 px.</summary>
        readonly List<VisualElement> underBar;
        /// <summary>The bar's items, whose rows set the three-row bar's height (RowsHeight).</summary>
        readonly List<VisualElement> barItems;
        /// <summary>Statecraft.uss .hud-three-row .top-bar height.</summary>
        const float ThreeRowBarPx = 440f;

        public HudPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            this.root = root; this.bridge = bridge; this.text = text;
            endTurn = root.Q<Button>("end-turn");
            onEndTurn = () => bridge.EndTurn();
            endTurn.clicked += onEndTurn;
            endTurn.generateVisualContent += PaintHourglass;
            bar = root.Q<VisualElement>("top-bar");
            hudRoot = root.Q<VisualElement>("root");
            bar.RegisterCallback<GeometryChangedEvent>(OnBarGeometry);
            meters = root.Q<VisualElement>("meters").Children().ToList();
            nav = root.Q<VisualElement>("nav");
            foreach (var m in meters) m.RegisterCallback<GeometryChangedEvent>(OnMeterGeometry);
            foreach (var (name, key) in Texts) root.Q<TextElement>(name).text = text[key];
            // The left column's buttons are icon-only (HUD round 2, #398): their name, kept as their (clear) text, is also
            // their tooltip, which HoverTip shows beside the disc on hover and on focus.
            var rail = root.Q<VisualElement>(RailName)?.Query<Button>().ToList() ?? new List<Button>();
            foreach (var b in rail) b.tooltip = b.text;
            if (rail.Count > 0 && root.Q<Label>(HoverTip.TipName) != null) railTips = new HoverTip(root, rail, () => HoverTip.Side.Right);
            // New Game in the bar is icon-only too (HUD round 2, part 3); its tip opens toward the bar's middle: left of it,
            // or right of it in the mirrored Hebrew bar.
            var newGame = root.Q<Button>("new-game");
            if (newGame != null)
            {
                newGame.tooltip = newGame.text;
                if (root.Q<Label>(HoverTip.TipName) != null)
                    barTips = new HoverTip(root, new[] { newGame }, () => hudRoot.ClassListContains(UiLanguage.HebrewClass) ? HoverTip.Side.Right : HoverTip.Side.Left);
            }
            foreach (var id in new[] { "treasury", "immigration", "security", "support", "cohesion", "standing", "fx" })
            {
                var meter = root.Q<VisualElement>("meter-" + id); var caption = text["hud.caption." + id];
                meter.Q<Label>(className: "meter-name").text = caption;   // the bar shows the words its tooltip says
                said[id] = (meter, caption);
            }
            // The text floor: checked now and twice a second, since a window resized at the same shape changes the panel's
            // scale without changing its layout (no geometry event).
            smallText = bar.Query<Label>(className: "meter-name").ToList();   // the seven captions and Security's level
            smallText.Add(root.Q<Label>("attention"));
            turnBox = bar.Q<VisualElement>(className: "turn-box");
            underBar = new[] { "right-column", "left-column", "event-gutter", "event-layer", "screen", "toast" }
                .SelectMany(c => root.Query(className: c).ToList()).ToList();
            barItems = bar.Children().ToList();
            foreach (var k in barItems) k.RegisterCallback<GeometryChangedEvent>(OnRowGeometry);   // a row grows inside the fixed bar
            dateRow = bar.Q<VisualElement>(className: "date-row");
            plateText = dateRow?.Query<Label>().ToList() ?? new List<Label>();
            dateRow?.parent?.RegisterCallback<GeometryChangedEvent>(OnPlateGeometry);   // a wider raised plate needs room in the bar
            ApplyTextFloor();
            floorCheck = root.schedule.Execute(ApplyTextFloor).Every(500);
            SetCompact(bar.layout.width);   // the bar may already be laid out (the loading screen shows it) and never resize
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge and the button (the owner calls this when it is disabled).</summary>
        public void Dispose()
        {
            railTips?.Dispose(); barTips?.Dispose();
            floorCheck.Pause();
            foreach (var l in smallText) { l.style.fontSize = StyleKeyword.Null; l.style.whiteSpace = StyleKeyword.Null; l.style.maxWidth = StyleKeyword.Null; }
            turnBox.style.width = StyleKeyword.Null; turnBox.style.minWidth = StyleKeyword.Null;
            foreach (var k in barItems) k.UnregisterCallback<GeometryChangedEvent>(OnRowGeometry);
            foreach (var e in underBar) e.style.marginTop = StyleKeyword.Null;
            bar.style.height = StyleKeyword.Null;
            foreach (var l in plateText) { l.style.fontSize = StyleKeyword.Null; l.style.whiteSpace = StyleKeyword.Null; }
            if (dateRow != null) { dateRow.style.flexDirection = StyleKeyword.Null; if (dateRow.parent != null) PadPlate(dateRow.parent, false); }
            bridge.Changed -= Render; endTurn.clicked -= onEndTurn; endTurn.generateVisualContent -= PaintHourglass;
            bar.UnregisterCallback<GeometryChangedEvent>(OnBarGeometry);
            dateRow?.parent?.UnregisterCallback<GeometryChangedEvent>(OnPlateGeometry);
            foreach (var m in meters) m.UnregisterCallback<GeometryChangedEvent>(OnMeterGeometry);
            bar.RemoveFromClassList(RoomyClass);
            bar.RemoveFromClassList(CompactClass); hudRoot.RemoveFromClassList(NarrowClass); hudRoot.RemoveFromClassList(TwoRowClass); hudRoot.RemoveFromClassList(ThreeRowClass);
            ScreenNav.DisableCovered(hudRoot);
        }

        // The bar's width does not depend on the class, so this settles in one pass.
        void OnBarGeometry(GeometryChangedEvent e) => SetCompact(e.newRect.width);

        // The raised plate sizes to its widest line (PadPlate), so the tiers re-read the room it takes (FloorGrowth). The
        // plate's width does not depend on the tier, so this settles in one pass.
        void OnPlateGeometry(GeometryChangedEvent e) { if (smallTextSize > SmallTextPx && e.oldRect.width != e.newRect.width) SetCompact(bar.layout.width); }

        void OnMeterGeometry(GeometryChangedEvent e) => UpdateRoomy();

        // The rows' height does not depend on the bar's (RowsHeight), so this settles in one pass.
        void OnRowGeometry(GeometryChangedEvent e) => PlaceUnderBar();

        /// <summary>Roomy only on the one-row bar with icons; see RoomyClass.</summary>
        void UpdateRoomy()
        {
            var meters = this.meters.Where(m => m.resolvedStyle.display != DisplayStyle.None).ToList();   // a hidden meter takes no room
            if (meters.Count == 0) return;
            var oneRow = !hudRoot.ClassListContains(NarrowClass) && !bar.ClassListContains(CompactClass);
            // Panel coordinates (logical px), so the meters and the screen buttons compare directly.
            float first = meters[0].worldBound.xMin, last = meters[meters.Count - 1].worldBound.xMax, limit = nav.worldBound.xMin;
            if (!oneRow || float.IsNaN(first) || float.IsNaN(limit)) { bar.RemoveFromClassList(RoomyClass); return; }
            var used = last - first; var spare = limit - last;
            if (bar.ClassListContains(RoomyClass))
            {
                if (spare < SpareShare * used) bar.RemoveFromClassList(RoomyClass);
            }
            else
            {
                var values = meters.Sum(m => m.Q<Label>(className: "meter-value")?.worldBound.width ?? 0f);
                var captions = meters.Sum(m =>
                {
                    var c = m.Q<Label>(className: "meter-name");
                    if (c == null || string.IsNullOrEmpty(c.text)) return 0f;
                    var oneLine = c.MeasureTextSize(c.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
                    return UnityEngine.Mathf.Max(0f, oneLine - c.worldBound.width);
                });
                if (spare - RoomyGrowth(values, captions, meters.Count) >= SpareShare * used + 8f) bar.AddToClassList(RoomyClass);
            }
        }

        /// <summary>Sets the small text to SmallTextSize for the panel's current scale and re-picks the tier, since larger
        /// captions need more room (TierWidth).</summary>
        void ApplyTextFloor()
        {
            var p = root.panel;
            if (p == null) return;
            var size = SmallTextSize(p.scaledPixelsPerPoint / DevicePixelRatio());
            if (size == smallTextSize) return;
            smallTextSize = size;
            SetCompact(bar.layout.width);
        }

        /// <summary>Whether the floor raises the text at this bar width: at every tier, the three-row bar of a phone held upright
        /// (about 0.49 scale, 25 px) included (#642).</summary>
        bool FloorRaises(float width) => smallTextSize > SmallTextPx;

        /// <summary>The room the raised text takes beyond what the tiers were set for (the 15 px captions, wrapped at their 120 px
        /// max-width, and the 150 px turn box): each caption on one line at the larger size (text width scales with font
        /// size), and the turn box widened in step. The one-row and icon tiers read the bar's width less this.</summary>
        float FloorGrowth()
        {
            float growth = TurnBoxPx * (smallTextSize / SmallTextPx - 1f) + (RaisedPlateWidth() - PlateWidthPx);
            foreach (var l in smallText)
            {
                if (l.name == "attention" || string.IsNullOrEmpty(l.text)) continue;
                var current = l.resolvedStyle.fontSize;
                if (!(current > 0f)) continue;
                var oneLine = l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
                growth += oneLine * smallTextSize / current - UnityEngine.Mathf.Min(oneLine * SmallTextPx / current, CaptionMaxPx);
            }
            return UnityEngine.Mathf.Max(0f, growth);
        }
        // Statecraft.uss .turn-box width, .meter .meter-name max-width (170 on the three-row bar, where the tier is fixed), .date-box width.
        const float TurnBoxPx = 150f, CaptionMaxPx = 120f, PlateWidthPx = 230f;
        /// <summary>The widest date line's width per px of font size, with a little to spare: the Hebrew 31 October 1956 is
        /// about 7.8 times its size (196 px at 25 px), the English "30 September 1956" 7.2 (TopBarScoreLayoutTests.WorstDate).</summary>
        const float DateLinePerPx = 8f;

        /// <summary>The raised plate's laid-out width (PadPlate lets it size to its widest line, at least the stylesheet's 230 px;
        /// at 23 px, 960x600, the date is wider), or 230 before it is laid out.</summary>
        float RaisedPlateWidth()
        {
            var w = dateRow?.parent?.layout.width ?? float.NaN;
            return w > PlateWidthPx ? w : PlateWidthPx;
        }

        /// <summary>The largest raised small text the 70 px one-row bar holds: the attention line above End Turn is taller with
        /// it, and at 19 px (1366x657) the Hebrew turn box leaves the bar (TopBarTests.TheBarFitsEveryWindowInEnglishAndHebrew),
        /// so above this the bar takes the stacked tier.</summary>
        public const float OneRowSmallMaxPx = 18f;

        /// <summary>Inline styles for the floor, only while it raises the text: its size; the captions on one line on the one-row
        /// and stacked bars (a wrapped caption at the larger size would leave the bar), where FloorGrowth has made room (the
        /// two-row bar keeps the stylesheet's wrapping); and the turn box widened so the attention line fits.</summary>
        void StyleSmallText(bool raised)
        {
            bool oneLine = raised && !hudRoot.ClassListContains(TwoRowClass);
            foreach (var l in smallText)
            {
                l.style.fontSize = raised ? new StyleLength(smallTextSize) : new StyleLength(StyleKeyword.Null);
                if (l.name == "attention") continue;
                l.style.whiteSpace = oneLine ? new StyleEnum<WhiteSpace>(WhiteSpace.NoWrap) : new StyleEnum<WhiteSpace>(StyleKeyword.Null);
                l.style.maxWidth = oneLine ? new StyleLength(StyleKeyword.None) : new StyleLength(StyleKeyword.Null);
            }
            foreach (var l in plateText)
            {
                l.style.fontSize = raised ? new StyleLength(smallTextSize) : new StyleLength(StyleKeyword.Null);
                // One line each; the plate widens for them (PadPlate).
                l.style.whiteSpace = raised ? new StyleEnum<WhiteSpace>(WhiteSpace.NoWrap) : new StyleEnum<WhiteSpace>(StyleKeyword.Null);
            }
            // Raised, the turn and the date take a line each (a raised date squeezed beside the turn was clipped in Hebrew),
            // and the plate's padding keeps every line inside its gilt frame's inner rule and at least 0 px from the 18 px corner
            // zones, which keep 4 px clear of the scrolls (PlateSidePx, PlateEdgePx; reviews of #624 in WebGL); the plate grows
            // to hold them.
            if (dateRow != null)
            {
                dateRow.style.flexDirection = raised ? new StyleEnum<FlexDirection>(FlexDirection.Column) : new StyleEnum<FlexDirection>(StyleKeyword.Null);
                if (dateRow.parent != null) PadPlate(dateRow.parent, raised);
            }
            var box = TurnBoxPx * smallTextSize / SmallTextPx;
            turnBox.style.width = raised ? new StyleLength(box) : new StyleLength(StyleKeyword.Null);
            turnBox.style.minWidth = raised ? new StyleLength(box) : new StyleLength(StyleKeyword.Null);
        }

        void PadPlate(VisualElement plate, bool raised)
        {
            // Raised, the plate sizes to its widest line (the turn and the date, a line each, never wrapped), at least 230 px.
            plate.style.width = raised ? new StyleLength(StyleKeyword.Auto) : new StyleLength(StyleKeyword.Null);
            // At least the widest date line too, so the plate keeps its width when the month changes (PromoHudTests): at 25 px
            // (a phone upright, #642) the Hebrew 31 October 1956 is about 196 px, wider than 230 px less the padding.
            plate.style.minWidth = raised ? new StyleLength(UnityEngine.Mathf.Max(PlateWidthPx, DateLinePerPx * smallTextSize + 2f * PlateSidePx + 2f))
                                          : new StyleLength(StyleKeyword.Null);
            StyleLength Px(float v) => raised ? new StyleLength(v) : new StyleLength(StyleKeyword.Null);
            plate.style.paddingTop = Px(PlateEdgePx); plate.style.paddingBottom = Px(PlateEdgePx);
            plate.style.paddingLeft = Px(PlateSidePx); plate.style.paddingRight = Px(PlateSidePx);
        }

        /// <summary>The three-row bar takes its rows' height when that is more than its 440 px (Statecraft.uss), so the raised
        /// text of a smaller phone fits it (#642, the owner: "down to 375x667"), and the panels under it move down by what it
        /// grew: a top margin, not a top, so the stylesheet's other placements (the toast at the bottom beside a wide screen)
        /// still apply. Set here, not as height: auto in the stylesheet: there Yoga sized the wrapping bar as if each quarter-width
        /// meter took a line of its own (about 1300 px). Its items' geometry events re-run this (OnRowGeometry), and so does SetCompact.</summary>
        void PlaceUnderBar()
        {
            // A hidden bar (the start screen) moves nothing: the start screen's toast keeps its own top (review of #859).
            var rows = hudRoot.ClassListContains(ThreeRowClass) && bar.resolvedStyle.display != DisplayStyle.None ? RowsHeight() : 0f;
            var grown = rows - ThreeRowBarPx;
            bar.style.height = grown > 0.5f ? new StyleLength(rows) : new StyleLength(StyleKeyword.Null);
            var margin = grown > 0.5f ? new StyleLength(grown) : new StyleLength(StyleKeyword.Null);
            foreach (var e in underBar) e.style.marginTop = margin;
        }

        /// <summary>The height the bar's rows take: each row's tallest item (its children laid out in the flow, grouped by the
        /// rows they share), with the bar's padding and border. It does not depend on the bar's own height (the rows' spacing,
        /// align-content, is not counted), so a bar set to it keeps it. NaN before layout.</summary>
        float RowsHeight()
        {
            float total = 0f, top = float.NaN, bottom = float.NaN;
            foreach (var k in bar.Children().Where(k => k.resolvedStyle.position == Position.Relative && k.resolvedStyle.display != DisplayStyle.None).OrderBy(k => k.layout.y))
            {
                float t = k.layout.y - k.resolvedStyle.marginTop, b = k.layout.yMax + k.resolvedStyle.marginBottom;
                if (t < bottom) { top = UnityEngine.Mathf.Min(top, t); bottom = UnityEngine.Mathf.Max(bottom, b); continue; }   // the same row
                if (bottom > top) total += bottom - top;
                top = t; bottom = b;
            }
            if (bottom > top) total += bottom - top;
            var s = bar.resolvedStyle;
            return total + s.paddingTop + s.paddingBottom + s.borderTopWidth + s.borderBottomWidth;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // The panel scales by the canvas's device pixels, so a screen pixel here is a CSS pixel: the device's pixels / the
        // browser's devicePixelRatio (DevicePixelRatio.jslib).
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern float StatecraftDevicePixelRatio();
        static float DevicePixelRatio() { var r = StatecraftDevicePixelRatio(); return r > 0f ? r : 1f; }
#else
        static float DevicePixelRatio() => 1f;
#endif

        void SetCompact(float width)
        {
            if (!(width > 0)) return;   // NaN before layout: wait for it
            var raised = FloorRaises(width);
            var tierWidth = raised ? width - FloorGrowth() : width;   // the owner's text floor needs room (#624)
            bar.EnableInClassList(CompactClass, tierWidth < CompactBelowPx);
            hudRoot.EnableInClassList(NarrowClass, tierWidth < NarrowBelowPx || raised && smallTextSize > OneRowSmallMaxPx);
            hudRoot.EnableInClassList(TwoRowClass, width < TwoRowBelowPx);
            hudRoot.EnableInClassList(ThreeRowClass, width < ThreeRowBelowPx);
            StyleSmallText(raised);
            PlaceUnderBar();
            ScreenNav.DisableCovered(hudRoot);   // two rows or one: what an open screen covers changes with it (#400)
        }

        /// <summary>End Turn's hourglass, in the button's left padding (Statecraft.uss .end-turn), painted into the button so it
        /// stays a leaf sized by its text (UiIcon.Paint).</summary>
        static void PaintHourglass(MeshGenerationContext m)
        {
            if (!m.visualElement.enabledInHierarchy) return;   // no bright sign on a disabled End Turn (game over)
            UiIcon.Paint(m, "hourglass", new UnityEngine.Rect(UiLanguage.Within(m.visualElement) ? m.visualElement.layout.width - 32f : 10f,
                (m.visualElement.layout.height - 22f) / 2f, 22f, 22f), UiIcon.IsFocused(m.visualElement));   // the right side in Hebrew (#193)
        }

        void Set(string name, string value) => root.Q<Label>(name).text = value;

        void Set(string meter, Meter m)
        {
            Set(meter + "-value", m.Value);
            var delta = root.Q<Label>(meter + "-delta");
            delta.text = m.Delta;
            delta.EnableInClassList("delta-up", m.Sign > 0);
            delta.EnableInClassList("delta-down", m.Sign < 0);
            // The meter in words, under its caption in the UI's language (#398).
            var (element, caption) = said[meter];
            element.tooltip = HudModel.Spoken(text, caption, m);
        }

        public void Render()
        {
            var m = HudModel.From(bridge, text);
            Set("year", m.Year); Set("turn", m.Turn); Set("date", m.Date); Set("attention", m.Attention);
            Set("security-level", m.SecurityLevel);
            var score = root.Q<Label>("score");   // the GDD loop only (#380): hidden when HudModel gives none
            score.text = m.Score;
            score.style.display = m.Score.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            Set("treasury", m.Treasury); Set("immigration", m.Immigration); Set("security", m.Security);
            Set("support", m.Support); Set("standing", m.Standing); Set("fx", m.Fx);
            // Social cohesion: the GDD loop only (#380), as the score; a hidden meter takes no room in the bar.
            Set("cohesion", m.Cohesion);
            said["cohesion"].element.style.display = m.ShowsCohesion ? DisplayStyle.Flex : DisplayStyle.None;
            root.Q<Button>("end-turn").SetEnabled(bridge.State.GameOver == null);
        }
    }
}
