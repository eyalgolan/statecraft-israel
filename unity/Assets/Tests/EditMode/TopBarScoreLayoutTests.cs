#nullable enable
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

/// <summary>The score in the top bar's date box (#380, 7b): a worst-case score stays inside the box's frame, on no other
/// text, and whole, at every window size the HUD's tests use, in English and Hebrew. At the widest date the year, turn
/// and date labels stay inside that frame too (#625). Each runs with no exhausted turns (the line hidden, the plate as
/// before #812) and with two (the line shown under the date).</summary>
public class TopBarScoreLayoutTests
{
    static readonly Content Gdd = ContentLoader.Load(Paths.Content);   // the real game: GDD since the cutover
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    /// <summary>Five digits with the thousands separator: well past what a Phase 1 or MVP run can score.</summary>
    const int WorstScore = 99999;
    /// <summary>The widest date line the box is sized for (Statecraft.uss .date-box). In English (#625) the widest month is
    /// November, "30 November 1956", 111.3 px at the plate's 15 px in EB Garamond Regular against 110.8 for "30 September
    /// 1956"; in Hebrew (#687) October, "31 באוקטובר 1956", 109.8 px in Frank Ruhl Libre Medium against 109.0 for
    /// "30 בספטמבר 1956" (Pillow's advance widths over the twelve months).</summary>
    public static System.DateTime WorstDate(bool hebrew) => hebrew ? new System.DateTime(1956, 10, 31) : new System.DateTime(1956, 11, 30);
    const int WorstTurn = 120;

    static Rect ContentBox(VisualElement e)
    {
        var b = e.worldBound; var s = e.resolvedStyle;
        return Rect.MinMaxRect(b.xMin + s.borderLeftWidth + s.paddingLeft, b.yMin + s.borderTopWidth + s.paddingTop,
            b.xMax - s.borderRightWidth - s.paddingRight, b.yMax - s.borderBottomWidth - s.paddingBottom);
    }

    static bool Inside(Rect inner, Rect outer) =>
        inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;

    /// <summary>The label's text, measured on one line, fits its own box.</summary>
    /// <summary>The label's text is not clipped: on one line, or (with #624's text floor, which lets the plate's date wrap at
    /// the largest sizes) wrapped within its box's height.</summary>
    static bool Whole(TextElement l)
    {
        var box = l.contentRect;
        if (l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x <= box.width + 1f) return true;
        if (l.resolvedStyle.whiteSpace != WhiteSpace.Normal) return false;
        return l.MeasureTextSize(l.text, box.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined).y <= box.height + 1f;
    }

    static bool Apart(Rect a, Rect b) => !Rect.MinMaxRect(a.xMin + 0.5f, a.yMin + 0.5f, a.xMax - 0.5f, a.yMax - 0.5f).Overlaps(b);

    [UnityTest]
    public IEnumerator AWorstCaseScoreStaysInsideTheDateBox()
    {
        var problems = new List<string>();
        foreach (var (language, text, hebrew, turns) in new[] { ("en", En, false, 0), ("en", En, false, 2), ("he", He, true, 0), ("he", He, true, 2) })
            foreach (var (w, h) in InfoPanelLayoutTests.Windows)
            {
                var rt = new RenderTexture(w, h, 0);
                var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
                ps.targetTexture = rt;
                var go = new GameObject("ScoreLayoutTest");
                go.SetActive(false);
                var doc = go.AddComponent<UIDocument>();
                doc.panelSettings = ps;
                doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
                go.SetActive(true);
                var root = doc.rootVisualElement;
                root.Q("root").RemoveFromClassList("loading");
                root.Q("root").EnableInClassList(UiLanguage.HebrewClass, hebrew);
                var bridge = new SimBridge(GameSession.New(Gdd, GoldenRun.Seed, Mode.Historical), text);
                bridge.State.Score = WorstScore;
                bridge.State.Date = WorstDate(hebrew);
                bridge.State.Turn = WorstTurn;
                bridge.State.ExhaustedTurns = turns;   // #812: above 0 the count shows under the date and must fit the plate too
                var hud = new HudPresenter(root, bridge, text);
                try
                {
                    var score = root.Q<Label>("score");
                    for (int i = 0; i < 20 && float.IsNaN(score.worldBound.width); i++) yield return null;
                    for (int i = 0; i < 3; i++) yield return null;   // the bar's tier classes settle after the first layout
                    var at = $"{language} {w}x{h} exhausted {turns}";
                    Assert.AreEqual(text.Format("hud.score", WorstScore), score.text, $"{at}: precondition: the worst score shows");
                    Assert.AreEqual(text.Format("hud.date", Statecraft.Game.Map.Control1948.DateText(WorstDate(hebrew), k => text[k])), root.Q<Label>("date").text, $"{at}: precondition: the widest date shows");
                    var box = root.Q(className: "date-box");
                    if (!Inside(score.worldBound, ContentBox(box))) problems.Add($"{at}: '{score.text}' {score.worldBound} leaves the date box's frame {ContentBox(box)}");
                    if (!Whole(score)) problems.Add($"{at}: '{score.text}' is clipped in {score.contentRect}");
                    foreach (var other in new[] { "year", "turn", "date" })
                    {
                        var o = root.Q<Label>(other);
                        if (!Apart(score.worldBound, o.worldBound)) problems.Add($"{at}: '{score.text}' {score.worldBound} overlaps {other} '{o.text}' {o.worldBound}");
                        if (!Whole(o)) problems.Add($"{at}: {other} '{o.text}' is clipped in {o.contentRect}");
                        // #625: the year, turn and date labels lie inside the frame too, at the widest date.
                        if (!Inside(o.worldBound, ContentBox(box))) problems.Add($"{at}: {other} '{o.text}' {o.worldBound} leaves the date box's frame {ContentBox(box)}");
                    }
                    var exhausted = root.Q<Label>("exhausted");
                    if (turns == 0)
                    {
                        if (exhausted.resolvedStyle.display != DisplayStyle.None) problems.Add($"{at}: the exhausted label '{exhausted.text}' shows with no exhausted turns");
                    }
                    else
                    {
                        // The label must really be shown before its geometry means anything: a hidden label has no box to overlap.
                        if (exhausted.resolvedStyle.display == DisplayStyle.None || exhausted.resolvedStyle.visibility == Visibility.Hidden
                            || exhausted.worldBound.width <= 0f || exhausted.worldBound.height <= 0f)
                            problems.Add($"{at}: the exhausted label is not shown ({exhausted.resolvedStyle.display}, {exhausted.resolvedStyle.visibility}, {exhausted.worldBound})");
                        if (!Inside(exhausted.worldBound, ContentBox(box))) problems.Add($"{at}: '{exhausted.text}' {exhausted.worldBound} leaves the date box's frame {ContentBox(box)}");
                        if (!Whole(exhausted)) problems.Add($"{at}: '{exhausted.text}' is clipped in {exhausted.contentRect}");
                        // The owner's text floor (#624) covers this line like the date's: on screen it is at least TextFloorPx.
                        var onScreen = exhausted.resolvedStyle.fontSize * root.panel.scaledPixelsPerPoint;
                        if (onScreen < HudPresenter.TextFloorPx - 0.01f)
                            problems.Add($"{at}: the exhausted line is {onScreen:0.##} px on screen ({exhausted.resolvedStyle.fontSize} px at {root.panel.scaledPixelsPerPoint:0.###}; under the {HudPresenter.TextFloorPx} px floor)");
                        foreach (var other in new[] { "year", "score", "turn", "date" })
                            if (!Apart(exhausted.worldBound, root.Q<Label>(other).worldBound)) problems.Add($"{at}: '{exhausted.text}' {exhausted.worldBound} overlaps {other}");
                    }
                }
                finally
                {
                    hud.Dispose();
                    Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
                }
            }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>#812 review: where only the 13 px exhausted line is raised (scale 0.8 to 0.92: 13 px is under the 12 px floor on
    /// screen, 15 px is not), the plate still takes its frame clearance (PlateSidePx, PlateEdgePx). Inside(ContentBox) alone
    /// would not show it: it reads the plate's current padding, whatever that is. Run in English and Hebrew, at the normal tier
    /// (1600x900) and in the compact one (1400x1100, under CompactBelowPx), where the stylesheet's date box is 226 px: the plate
    /// must still keep the turn and the date on one row and clear of the meters.</summary>
    [UnityTest]
    public IEnumerator TheExhaustedLineRaisedAloneKeepsThePlatesFrameClearance()
    {
        var problems = new List<string>();
        foreach (var (language, text, hebrew) in new[] { ("en", En, false), ("he", He, true) })
            foreach (var (w, h, compact) in new[] { (1600, 900, false), (1400, 1100, true) })
            {
                var at = $"{language} {w}x{h}";
                var rt = new RenderTexture(w, h, 0);
                var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
                ps.targetTexture = rt;
                var go = new GameObject("ExhaustedClearanceTest");
                go.SetActive(false);
                var doc = go.AddComponent<UIDocument>();
                doc.panelSettings = ps;
                doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
                go.SetActive(true);
                var root = doc.rootVisualElement;
                root.Q("root").RemoveFromClassList("loading");
                root.Q("root").EnableInClassList(UiLanguage.HebrewClass, hebrew);
                var bridge = new SimBridge(GameSession.New(Gdd, GoldenRun.Seed, Mode.Historical), text);
                bridge.State.Score = WorstScore;
                bridge.State.Date = WorstDate(hebrew);
                bridge.State.Turn = WorstTurn;
                bridge.State.ExhaustedTurns = 2;
                var hud = new HudPresenter(root, bridge, text);
                try
                {
                    var exhausted = root.Q<Label>("exhausted");
                    for (int i = 0; i < 20 && float.IsNaN(exhausted.worldBound.width); i++) yield return null;
                    for (int i = 0; i < 3; i++) yield return null;
                    var scale = root.panel.scaledPixelsPerPoint;
                    Assert.Greater(exhausted.resolvedStyle.fontSize, HudPresenter.ExhaustedPx, $"{at}: precondition: the exhausted line is raised (scale {scale:0.###})");
                    Assert.AreEqual(HudPresenter.SmallTextPx, root.Q<Label>("date").resolvedStyle.fontSize, 0.01f, $"{at}: precondition: the 15 px text is not raised (scale {scale:0.###})");
                    Assert.AreEqual(compact, root.Q(className: HudPresenter.CompactClass) != null, $"{at}: precondition: the compact tier is {(compact ? "on" : "off")}");
                    var plate = root.Q(className: "date-box");
                    var s = plate.resolvedStyle;
                    // Layout snaps to whole screen pixels (9 px at 0.833 resolves to 9.6), so compare within a screen pixel.
                    var tol = 1f / scale;
                    if (Mathf.Abs(s.paddingTop - HudPresenter.PlateEdgePx) > tol) problems.Add($"{at}: plate paddingTop {s.paddingTop} is not the clearance {HudPresenter.PlateEdgePx}");
                    if (Mathf.Abs(s.paddingBottom - HudPresenter.PlateEdgePx) > tol) problems.Add($"{at}: plate paddingBottom {s.paddingBottom} is not the clearance {HudPresenter.PlateEdgePx}");
                    if (Mathf.Abs(s.paddingLeft - HudPresenter.PlateSidePx) > tol) problems.Add($"{at}: plate paddingLeft {s.paddingLeft} is not the clearance {HudPresenter.PlateSidePx}");
                    if (Mathf.Abs(s.paddingRight - HudPresenter.PlateSidePx) > tol) problems.Add($"{at}: plate paddingRight {s.paddingRight} is not the clearance {HudPresenter.PlateSidePx}");
                    // Inside the window, and clear of every meter beside it.
                    if (!Inside(plate.worldBound, root.worldBound)) problems.Add($"{at}: the plate {plate.worldBound} leaves the window {root.worldBound}");
                    foreach (var meter in root.Query(className: "meter").ToList())
                        if (meter.resolvedStyle.display != DisplayStyle.None && !Apart(plate.worldBound, meter.worldBound))
                            problems.Add($"{at}: the plate {plate.worldBound} overlaps a meter {meter.worldBound}");
                    // Every line inside the plate's frame, whole, and on no other line.
                    var lines = new[] { "year", "score", "turn", "date", "exhausted" }.Select(n => root.Q<Label>(n)).ToList();
                    foreach (var l in lines)
                    {
                        if (!Inside(l.worldBound, ContentBox(plate))) problems.Add($"{at}: {l.name} '{l.text}' {l.worldBound} leaves the plate's frame {ContentBox(plate)}");
                        if (!Whole(l)) problems.Add($"{at}: {l.name} '{l.text}' is clipped in {l.contentRect}");
                    }
                    for (int a = 0; a < lines.Count; a++)
                        for (int b = a + 1; b < lines.Count; b++)
                            if (!Apart(lines[a].worldBound, lines[b].worldBound)) problems.Add($"{at}: {lines[a].name} {lines[a].worldBound} overlaps {lines[b].name} {lines[b].worldBound}");
                    // The turn and the date keep their one row: side by side, the same line, not stacked.
                    var turn = root.Q<Label>("turn").worldBound; var date = root.Q<Label>("date").worldBound;
                    if (Mathf.Abs(turn.center.y - date.center.y) > tol + 1f) problems.Add($"{at}: the turn {turn} and the date {date} are not on one row");
                }
                finally
                {
                    hud.Dispose();
                    Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
                }
            }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>Review of #1961: at the scale where only the 13 px exhausted line is raised (1600x900, as above), a hidden line
    /// (no exhausted turns) leaves the plate as the stylesheet has it: no inline width, minimum width or padding. Showing the
    /// line then gives the plate its clearance, and hiding it again takes the clearance back.</summary>
    [UnityTest]
    public IEnumerator AHiddenExhaustedLineLeavesThePlateAsItWas()
    {
        var problems = new List<string>();
        foreach (var (language, text, hebrew) in new[] { ("en", En, false), ("he", He, true) })
        {
            var rt = new RenderTexture(1600, 900, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("HiddenExhaustedTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            root.Q("root").RemoveFromClassList("loading");
            root.Q("root").EnableInClassList(UiLanguage.HebrewClass, hebrew);
            var bridge = new SimBridge(GameSession.New(Gdd, GoldenRun.Seed, Mode.Historical), text);
            bridge.State.ExhaustedTurns = 0;
            var hud = new HudPresenter(root, bridge, text);
            try
            {
                var date = root.Q<Label>("date");
                for (int i = 0; i < 20 && float.IsNaN(date.worldBound.width); i++) yield return null;
                for (int i = 0; i < 3; i++) yield return null;
                var exhausted = root.Q<Label>("exhausted");
                var plate = root.Q(className: "date-box");
                Assert.Greater(exhausted.style.fontSize.value.value, HudPresenter.ExhaustedPx, $"{language}: precondition: the scale raises the exhausted line");
                Assert.AreEqual(HudPresenter.SmallTextPx, date.resolvedStyle.fontSize, 0.01f, $"{language}: precondition: the 15 px text is not raised");
                void Expect(string state, bool clearance)
                {
                    var st = plate.style;
                    var inline = new[] { ("width", st.width.keyword), ("minWidth", st.minWidth.keyword), ("paddingTop", st.paddingTop.keyword),
                        ("paddingBottom", st.paddingBottom.keyword), ("paddingLeft", st.paddingLeft.keyword), ("paddingRight", st.paddingRight.keyword) };
                    foreach (var (prop, keyword) in inline)
                        if ((keyword == StyleKeyword.Null) == clearance)
                            problems.Add($"{language} {state}: the plate's {prop} is {(clearance ? "the stylesheet's" : "set inline")}");
                    if (clearance && st.paddingLeft.value.value != HudPresenter.PlateSidePx)
                        problems.Add($"{language} {state}: the plate's paddingLeft {st.paddingLeft.value.value} is not the clearance {HudPresenter.PlateSidePx}");
                }
                Expect("hidden", false);
                bridge.State.ExhaustedTurns = 2;
                hud.Render();
                yield return null;
                Assert.AreEqual(DisplayStyle.Flex, exhausted.resolvedStyle.display, $"{language}: precondition: two exhausted turns show the line");
                Expect("shown", true);
                bridge.State.ExhaustedTurns = 0;
                hud.Render();
                yield return null;
                Expect("hidden again", false);
            }
            finally
            {
                hud.Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }
}
