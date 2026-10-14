#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The small fixes from the accessibility audit (docs/accessibility-audit-2026-10-14.md, #41): a Budget step keeps
/// the focus on its button (H2); Escape from a screen gives the focus to its top-bar button (M1); text on the keyboard
/// focus fill and the lit escalation rung's turn line reach 4.5:1 (M3, M4); the Cabinet's orders line is not cut off and
/// its candidate list keeps two rows at the playtest window, 1568x752 (M7). The start screen and the decision card's own
/// cases of M1 are in StartScreenTests and DecisionCardTests.</summary>
public class KeyboardFocusTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>Game.uxml on a panel that renders into a texture of the window's size, so layout, style and focus run as in
    /// a player once frames pass.</summary>
    sealed class Panel : IDisposable
    {
        readonly GameObject go; readonly PanelSettings ps; readonly RenderTexture rt;
        public readonly VisualElement Root;
        public readonly List<IDisposable> Owned = new List<IDisposable>();

        public Panel(int w = 1920, int h = 1080)
        {
            rt = new RenderTexture(w, h, 0);
            ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            go = new GameObject("KeyboardFocusTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            Root = doc.rootVisualElement;
            Root.Q("root").RemoveFromClassList("loading");
        }

        public T Q<T>(string name) where T : VisualElement
        {
            var e = Root.Q<T>(name);
            Assert.IsNotNull(e, name);
            return e;
        }

        public Focusable? Focused => Root.panel.focusController.focusedElement;

        public void Dispose()
        {
            for (int i = Owned.Count - 1; i >= 0; i--) Owned[i].Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    static IEnumerator FocusOn(Panel p, VisualElement e)
    {
        e.Focus();
        for (int i = 0; i < 10 && p.Focused != e; i++) { yield return null; e.Focus(); }
    }

    /// <summary>H2: Enter on a Budget + or - moves the share and leaves the focus on the same button, so a second Enter
    /// steps again and Tab carries on from the row (it used to start again at Cabinet).</summary>
    [UnityTest]
    public IEnumerator ABudgetStepKeepsTheFocusOnItsButton()
    {
        using var p = new Panel();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        p.Owned.Add(new BudgetPresenter(p.Root, bridge, Text));
        var nav = new ScreenNav(p.Root); p.Owned.Add(nav);
        nav.Open("budget");
        yield return Frames(4);
        var share = p.Q<Label>("budget-education_minister-share");
        foreach (var name in new[] { "budget-education_minister-up", "budget-education_minister-down" })
        {
            var b = p.Q<Button>(name);
            yield return FocusOn(p, b);
            Assert.AreSame(b, p.Focused, name + ": precondition, focused");
            for (int press = 1; press <= 2; press++)
            {
                var before = share.text;
                UiTestKit.Press((Button)p.Focused!);
                yield return Frames(2);
                Assert.AreNotEqual(before, share.text, $"{name}, press {press}: the share moved");
                Assert.AreSame(share, p.Root.Q<Label>("budget-education_minister-share"), "the row is updated in place");
                Assert.AreSame(b, p.Root.Q<Button>(name), $"{name}, press {press}: the button is the same element");
                Assert.AreSame(b, p.Focused, $"{name}, press {press}: and keeps the focus");
            }
        }
    }

    /// <summary>M1: Escape from a screen whose contents had the focus gives it to the screen's top-bar button, so Tab carries
    /// on from there; Escape with the focus on the top bar leaves it where it is.</summary>
    [UnityTest]
    public IEnumerator EscapeFromAScreenGivesTheFocusToItsButton()
    {
        using var p = new Panel();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        p.Owned.Add(new BudgetPresenter(p.Root, bridge, Text));
        p.Owned.Add(new DefencePresenter(p.Root, bridge, Text));
        var nav = new ScreenNav(p.Root); p.Owned.Add(nav);
        foreach (var screen in new[] { "budget", "defence" })
        {
            nav.Open(screen);
            yield return Frames(4);
            var inside = p.Root.Q("screen-" + screen).Query<Button>().ToList().First(b => b.enabledInHierarchy);
            yield return FocusOn(p, inside);
            Assert.AreSame(inside, p.Focused, screen + ": precondition, a control on the screen has the focus");
            using (var e = NavigationCancelEvent.GetPooled()) { e.target = inside; inside.SendEvent(e); }
            yield return Frames(3);
            Assert.IsNull(nav.Current, screen + ": Escape closed it");
            Assert.AreSame(p.Q<Button>("nav-" + screen), p.Focused, screen + ": its top-bar button has the focus");
        }
        var endTurn = p.Q<Button>("end-turn");
        nav.Open("budget");
        yield return Frames(3);
        yield return FocusOn(p, endTurn);
        Assert.IsTrue(nav.Close());
        yield return Frames(3);
        Assert.AreSame(endTurn, p.Focused, "the focus was on the top bar: it stays there");
    }

    /// <summary>WCAG relative luminance, sRGB.</summary>
    static float Lum(Color c)
    {
        float Ch(float v) => v <= 0.04045f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
        return 0.2126f * Ch(c.r) + 0.7152f * Ch(c.g) + 0.0722f * Ch(c.b);
    }

    static float Ratio(Color a, Color b) { float x = Lum(a), y = Lum(b); return (Mathf.Max(x, y) + 0.05f) / (Mathf.Min(x, y) + 0.05f); }

    /// <summary>What shows behind <paramref name="e"/>: its own background and every ancestor's, composited in linear light
    /// (the project's colour space) down to the first opaque one, or the HUD's navy.</summary>
    static Color Backdrop(VisualElement e)
    {
        var layers = new List<Color>();
        for (var v = e; v != null; v = v.parent)
        {
            var bg = v.resolvedStyle.backgroundColor;
            if (bg.a <= 0f) continue;
            layers.Add(bg);
            if (bg.a >= 1f) break;
        }
        var under = new Color(9 / 255f, 14 / 255f, 26 / 255f).linear;
        for (int i = layers.Count - 1; i >= 0; i--)
        {
            var l = layers[i]; var lin = l.linear;
            under = new Color(lin.r * l.a + under.r * (1 - l.a), lin.g * l.a + under.g * (1 - l.a), lin.b * l.a + under.b * (1 - l.a));
        }
        return under.gamma;
    }

    /// <summary>Computed contrast the focus-fill text must reach: 4.5:1 plus a margin, because anti-aliasing on the canvas
    /// takes about 0.4 off (ink at 4.60:1 computed measured 4.16 to 4.21:1 on the audit's captures).</summary>
    const float FocusTextRatio = 5f;

    /// <summary>M3: the label of every kind of button is at least 4.5:1 on the keyboard focus fill, with margin
    /// (FocusTextRatio), the gold ones (End Turn, Decision waiting, the Illustration switch) included, and every text on a
    /// focused Cabinet card too, its stat numbers included.</summary>
    [UnityTest]
    public IEnumerator TextOnTheFocusFillIsAtLeastFourPointFiveToOne()
    {
        using var p = new Panel();
        var host = new VisualElement();
        host.style.position = Position.Absolute; host.style.left = 40; host.style.top = 200;
        p.Q<VisualElement>("root").Add(host);
        var kinds = new[] { "", "end-turn", "event-reopen", "event-illustration", EventCardPresenter.OptionClass, "pm-office" };
        var buttons = kinds.Select(k =>
        {
            var b = new Button { text = "Focused " + k, name = "probe-" + (k == "" ? "plain" : k) };
            if (k != "") b.AddToClassList(k);
            // Decision waiting and the Illustration switch are hidden and absolutely placed by the stylesheet until shown.
            b.style.display = DisplayStyle.Flex; b.style.position = Position.Relative;
            host.Add(b);
            return b;
        }).ToList();
        yield return Frames(3);
        foreach (var b in buttons)
        {
            yield return FocusOn(p, b);
            yield return Frames(2);
            Assert.AreSame(b, p.Focused, b.name + ": focused");
            var fill = b.resolvedStyle.backgroundColor;
            Assert.AreEqual(1f, fill.a, 0.001f, b.name + ": the focus fill is opaque");
            var ratio = Ratio(b.resolvedStyle.color, fill);
            Assert.GreaterOrEqual(ratio, FocusTextRatio, $"{b.name}: its label is {ratio:0.00}:1 on the focus fill");
        }

        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        p.Owned.Add(new CabinetPresenter(p.Root, bridge, Text, null));
        var nav = new ScreenNav(p.Root); p.Owned.Add(nav);
        nav.Open("cabinet");
        yield return Frames(4);
        foreach (var name in new[] { "seat-prime_minister", "seat-finance_minister" })
        {
            var card = p.Q<Button>(name);
            yield return FocusOn(p, card);
            yield return Frames(2);
            var fill = card.resolvedStyle.backgroundColor;
            foreach (var label in card.Query<Label>().ToList().Where(l => !string.IsNullOrEmpty(l.text) && l.resolvedStyle.display == DisplayStyle.Flex
                                                                          && l.GetFirstAncestorOfType<VisualElement>()?.ClassListContains("card-plate") != true))
            {
                var ratio = Ratio(label.resolvedStyle.color, fill);
                Assert.GreaterOrEqual(ratio, FocusTextRatio, $"{name}: '{label.text}' is {ratio:0.00}:1 on the focus fill");
            }
        }
    }

    /// <summary>M4: on the lit (current) escalation rung both the rung's name and its turn length are at least 4.5:1; on
    /// every plain rung the 15 px turn length is at least 5.5:1 computed, the margin anti-aliasing takes from it (ink-dim's
    /// 5.0:1 measured 4.0 to 4.2:1).</summary>
    [UnityTest]
    public IEnumerator TheLitRungsTextIsAtLeastFourPointFiveToOne()
    {
        using var p = new Panel();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        p.Owned.Add(new DefencePresenter(p.Root, bridge, Text));
        var nav = new ScreenNav(p.Root); p.Owned.Add(nav);
        nav.Open("defence");
        yield return Frames(4);
        var rung = p.Q<VisualElement>("rung-" + bridge.State.Rung);
        Assert.IsTrue(rung.ClassListContains("rung-current"), "precondition: today's rung is lit");
        var back = Backdrop(rung);
        foreach (var part in new[] { "name", "turn" })
        {
            var label = p.Q<Label>($"rung-{bridge.State.Rung}-{part}");
            var ratio = Ratio(label.resolvedStyle.color, back);
            Assert.GreaterOrEqual(ratio, 4.5f, $"the lit rung's {part} '{label.text}' is {ratio:0.00}:1");
        }
        for (int n = 1; n <= 5; n++)
        {
            if (n == bridge.State.Rung) continue;
            var plain = p.Q<VisualElement>("rung-" + n);
            var turn = p.Q<Label>($"rung-{n}-turn");
            var ratio = Ratio(turn.resolvedStyle.color, Backdrop(plain));
            Assert.GreaterOrEqual(ratio, 5.5f, $"rung {n}'s turn '{turn.text}' is {ratio:0.00}:1");
        }
    }

    /// <summary>M7: at the playtest window (1568x752) and four others, with an appointment ordered, the "Ordered:" line wraps
    /// inside the side column and the screen instead of being cut off, and the candidate list shows at least two rows; on
    /// the shortest the HUD supports (5120x1440, 32:9, 764 logical px) at least one. The Prime Minister's card never runs
    /// into the line under it. At 764 the card's text, his other offices and the orders line leave the list less than a
    /// row; it still shows some and scrolls.</summary>
    [UnityTest]
    public IEnumerator TheCabinetsOrdersLineAndCandidatesHaveRoom()
    {
        foreach (var (w, h, rowsShown) in new[] { (1568, 752, 2), (1531, 784, 2), (1920, 1080, 2), (2560, 1080, 2), (5120, 1440, 0) })
        {
            using var p = new Panel(w, h);
            var where = $"{w}x{h}";
            for (int i = 0; i < 20 && !(p.Root.Q("top-bar").worldBound.width > 0); i++) yield return null;
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            p.Owned.Add(new HudPresenter(p.Root, bridge, Text));
            var portraits = AssetDatabase.LoadAssetAtPath<PortraitCatalog>("Assets/UI/Portraits/PortraitCatalog.asset");
            Assert.IsNotNull(portraits, "the portrait catalogue");
            var cabinet = new CabinetPresenter(p.Root, bridge, Text, portraits); p.Owned.Add(cabinet);
            var nav = new ScreenNav(p.Root); p.Owned.Add(nav);
            nav.Open("cabinet");
            cabinet.Select("finance_minister");
            yield return Frames(4);
            var appoint = p.Q<ScrollView>("cabinet-candidates").Query<Button>().ToList().First(b => b.enabledSelf);
            UiTestKit.Press(appoint);
            Assert.AreEqual(1, bridge.Queued.OfType<Appoint>().Count(), where + ": precondition, an appointment is ordered");
            yield return Frames(4);

            var status = p.Q<Label>("cabinet-status");
            StringAssert.StartsWith("Ordered: ", status.text);
            var side = p.Q<VisualElement>("cabinet-side").worldBound;
            var screenEl = p.Q<VisualElement>("screen-cabinet");
            var s = screenEl.resolvedStyle;
            var inner = screenEl.worldBound;
            var bottom = inner.yMax - s.borderBottomWidth - s.paddingBottom;
            var r = status.worldBound;
            var sizes = $"side {side.height:0}, card {p.Q<VisualElement>("seat-" + CabinetPresenter.PrimeMinister).worldBound.height:0}, offices {p.Q<VisualElement>("cabinet-pm-offices").worldBound.height:0}, pick {p.Q<VisualElement>("cabinet-pick").worldBound.height:0}, status {r.height:0}";
            where += $" ({sizes})";
            Assert.LessOrEqual(r.xMax, side.xMax + 0.5f, $"{where}: the orders line {r} runs out of the side column {side}");
            Assert.LessOrEqual(r.yMax, bottom + 0.5f, $"{where}: the orders line {r} runs below the screen's content ({bottom:0})");
            var width = status.contentRect.width;
            var natural = status.MeasureTextSize(status.text, width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(natural.y, status.contentRect.height + 1f, $"{where}: '{status.text}' needs {natural.y:0} px and has {status.contentRect.height:0}");
            var oneLine = status.MeasureTextSize(status.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            if (oneLine.x > width + 1f) Assert.AreEqual(WhiteSpace.Normal, status.resolvedStyle.whiteSpace, $"{where}: a line wider than the column wraps");

            var list = p.Q<ScrollView>("cabinet-candidates");
            var view = list.contentViewport.worldBound;
            if (rowsShown > 0)
            {
                var rows = list.Query<VisualElement>(className: "candidate").ToList().Take(rowsShown).ToList();
                Assert.AreEqual(rowsShown, rows.Count);
                var need = rows[rowsShown - 1].worldBound.yMax - rows[0].worldBound.yMin;
                Assert.GreaterOrEqual(view.height + 0.5f, need, $"{where}: the candidate list is {view.height:0} px tall and {rowsShown} rows need {need:0}");
            }
            else Assert.GreaterOrEqual(view.height, 40f, $"{where}: the candidate list still shows part of a row");
            var pmCard = p.Q<VisualElement>("seat-" + CabinetPresenter.PrimeMinister);
            var lastLine = pmCard.Children().Where(e => e.resolvedStyle.display == DisplayStyle.Flex).Last().worldBound;
            Assert.LessOrEqual(lastLine.yMax, pmCard.worldBound.yMax + 0.5f, $"{where}: the Prime Minister's card text {lastLine} runs out of the card {pmCard.worldBound}");
            Assert.LessOrEqual(pmCard.worldBound.yMax, p.Q<VisualElement>("cabinet-pm-offices").worldBound.yMin + 0.5f, $"{where}: the card runs into his other offices");
            Assert.LessOrEqual(view.yMax, r.yMin + 0.5f, $"{where}: the list runs into the orders line");

            // A portrait that gave way is framed for the box it has, so the face stays in view.
            var pm = Cabinet.Holder(C, bridge.State, CabinetPresenter.PrimeMinister)!;
            var entry = portraits.Find(pm.Id);
            Assert.IsNotNull(entry, "precondition: the Prime Minister has a portrait");
            var portrait = p.Q<VisualElement>("seat-" + CabinetPresenter.PrimeMinister + "-portrait");
            var box = portrait.layout.size;
            if (box.y >= 1f)   // at 764 the portrait has given way entirely: nothing to frame
            {
                var expected = PortraitFraming.PositionYPercent(entry!.Value.image.width, entry.Value.image.height, box.x, box.y, entry.Value.focusY);
                Assert.AreEqual(expected, portrait.style.backgroundPositionY.value.offset.value, 0.5f, $"{where}: the portrait is framed for its {box} box");
            }
        }
    }
}
