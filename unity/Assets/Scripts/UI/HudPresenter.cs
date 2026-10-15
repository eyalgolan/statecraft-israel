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

        // Three tiers by the bar's logical width (1080p reference, match 0.5: logical width = sqrt(aspect x 1920 x 1080)).
        // HUD round 2 moved the four screen buttons to the left column (#602), about 248 px out of the bar, so every tier
        // starts that much narrower (part 3). Each threshold sits clear of a common window shape:
        //   16:9 1920, 16:10 ~1821, 4:3 ~1662 | NarrowBelowPx 1612 | 5:4 ~1585 | CompactBelowPx 1452 | square ~1440 (1382 at
        //   1024x1024) | TwoRowBelowPx 1370 | half a 1080p screen ~1358.
        // PromoHudTests.EveryWindowShapeFitsTheBarAfterATurn proves each tier holds the widest late-game values and changes.

        /// <summary>Below this width the HUD's root gets NarrowClass: a taller bar (124 px) whose meters stack the change under
        /// the value, and the panels below it move down to match (Statecraft.uss .hud-narrow). Above it, the 70 px one-row bar
        /// (promo 03's 6.5%) holds six values and their changes side by side with the icons.</summary>
        public const float NarrowBelowPx = 1612f;
        public const string NarrowClass = "hud-narrow";
        /// <summary>Below this width the meters also drop their icons (Statecraft.uss .top-bar--compact): 5:4 keeps them on the
        /// stacked bar; a square window does not have the room.</summary>
        public const float CompactBelowPx = 1452f;
        public const string CompactClass = "top-bar--compact";
        /// <summary>Below this width (windows narrower than about square, e.g. a browser snapped to half a 1080p screen, about
        /// 1360 logical px) one row cannot hold the badge, six meters, New Game and End Turn: the root also gets
        /// TwoRowClass, and the bar wraps into two rows, the date and the meters above, the screen buttons and End Turn
        /// below (Statecraft.uss .hud-two-row).</summary>
        public const float TwoRowBelowPx = 1370f;
        public const string TwoRowClass = "hud-two-row";
        /// <summary>Below this width (a phone held upright: 390x844 is about 790 logical px) the badge and six meters do not
        /// fit one row either: the root also gets ThreeRowClass, and the bar takes three rows, the badge, the meters, then the
        /// screen buttons and End Turn (Statecraft.uss .hud-three-row; #210).</summary>
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

        /// <summary>The meters' predicted growth when roomy: six icons 30 -> 38 px, values 22 -> 25 px (about 14% wider), and
        /// each wrapped caption back to its one-line width (<paramref name="captionGrowth"/>).</summary>
        public static float RoomyGrowth(float valueWidths, float captionGrowth = 0f) => 6 * 8f + 0.14f * valueWidths + captionGrowth;

        /// <summary>The top bar's static texts and their strings (#193): the screen buttons, New Game and End Turn. The
        /// meters' captions are hud.caption.*, the same words their tooltips say.</summary>
        static readonly (string name, string key)[] Texts =
        {
            ("nav-cabinet", "hud.nav.cabinet"), ("nav-budget", "hud.nav.budget"), ("nav-defence", "hud.nav.defence"),
            ("nav-why", "hud.nav.why"), ("new-game", "hud.new_game"), ("end-turn", "hud.end_turn"),
        };

        /// <summary>The left icon column (promo 03): the four screens' buttons.</summary>
        public const string RailName = "left-column";
        readonly HoverTip? railTips, barTips;

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
            foreach (var id in new[] { "treasury", "immigration", "security", "support", "standing", "fx" })
            {
                var meter = root.Q<VisualElement>("meter-" + id); var caption = text["hud.caption." + id];
                meter.Q<Label>(className: "meter-name").text = caption;   // the bar shows the words its tooltip says
                said[id] = (meter, caption);
            }
            SetCompact(bar.layout.width);   // the bar may already be laid out (the loading screen shows it) and never resize
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge and the button (the owner calls this when it is disabled).</summary>
        public void Dispose()
        {
            railTips?.Dispose(); barTips?.Dispose();
            bridge.Changed -= Render; endTurn.clicked -= onEndTurn; endTurn.generateVisualContent -= PaintHourglass;
            bar.UnregisterCallback<GeometryChangedEvent>(OnBarGeometry);
            foreach (var m in meters) m.UnregisterCallback<GeometryChangedEvent>(OnMeterGeometry);
            bar.RemoveFromClassList(RoomyClass);
            bar.RemoveFromClassList(CompactClass); hudRoot.RemoveFromClassList(NarrowClass); hudRoot.RemoveFromClassList(TwoRowClass); hudRoot.RemoveFromClassList(ThreeRowClass);
            ScreenNav.DisableCovered(hudRoot);
        }

        // The bar's width does not depend on the class, so this settles in one pass.
        void OnBarGeometry(GeometryChangedEvent e) => SetCompact(e.newRect.width);

        void OnMeterGeometry(GeometryChangedEvent e) => UpdateRoomy();

        /// <summary>Roomy only on the one-row bar with icons; see RoomyClass.</summary>
        void UpdateRoomy()
        {
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
                if (spare - RoomyGrowth(values, captions) >= SpareShare * used + 8f) bar.AddToClassList(RoomyClass);
            }
        }

        void SetCompact(float width)
        {
            if (!(width > 0)) return;   // NaN before layout: wait for it
            bar.EnableInClassList(CompactClass, width < CompactBelowPx);
            hudRoot.EnableInClassList(NarrowClass, width < NarrowBelowPx);
            hudRoot.EnableInClassList(TwoRowClass, width < TwoRowBelowPx);
            hudRoot.EnableInClassList(ThreeRowClass, width < ThreeRowBelowPx);
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
            root.Q<Button>("end-turn").SetEnabled(bridge.State.GameOver == null);
        }
    }
}
