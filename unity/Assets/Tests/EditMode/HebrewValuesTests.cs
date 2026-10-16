#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>A value inside a Hebrew line (#543): in a right-to-left paragraph the bidi rules move a value's minus to its
/// right ("-$4.2m" reads "$4.2m-") and split an English date ("15 May 1948" reads "May 1948 15"). UiText.Format puts a
/// left-to-right mark (U+200E) on both sides of each value it inserts into a Hebrew template, so the value is read as one
/// left-to-right run; an English template is left as it was.</summary>
public class HebrewValuesTests
{
    const string Lrm = "‎";
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>What Format gave before #543: string.Format over the pattern, nothing added.</summary>
    static string Plain(UiText t, string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, t[key], args);

    // --- UiText --------------------------------------------------------------------------------------------------------

    [Test]
    public void AValueInAHebrewTemplateIsWrappedInLeftToRightMarks()
    {
        Assert.AreEqual("כסף " + Lrm + "$12.0m" + Lrm + " · חוב " + Lrm + "IL 3.0m" + Lrm + " · מטבע חוץ " + Lrm + "-$4.2m" + Lrm,
            He.Format("budget.money", "$12.0m", "IL 3.0m", "-$4.2m"));
        Assert.AreEqual("מ־" + Lrm + "15 May 1948" + Lrm + " עד " + Lrm + "1 June 1948" + Lrm, He.Format("why.turn", "15 May 1948", "1 June 1948"));
        // A format item keeps its format: the value is formatted, then wrapped.
        Assert.AreEqual("תשומת לב " + Lrm + "2.5" + Lrm + " מתוך " + Lrm + "10.0" + Lrm, He.Format("hud.attention", 2.5, 10.0));
        // A template that starts with its value: the value's own first letter still sets the line's direction (UiDirection
        // passes over the marks), English here and Hebrew below.
        var appointed = He.Format("card.appointed_name", "David Ben-Gurion");
        Assert.AreEqual(Lrm + "David Ben-Gurion" + Lrm + " (מינוי שלך)", appointed);
        Assert.IsFalse(UiDirection.RightToLeft(appointed), appointed);
    }

    /// <summary>A value with Hebrew in it is not wrapped. A Hebrew word (the seat unit by body, #550) between marks would
    /// make the space between it and an English neighbour left to right, and the space would move to the wrong side; a
    /// value Format has made from a Hebrew pattern (the spoken amount inside the meter's spoken line) has its own values
    /// marked already.</summary>
    [Test]
    public void AHebrewValueInAHebrewTemplateIsLeftAlone()
    {
        Assert.AreEqual("אופוזיציה: " + Lrm + "45" + Lrm + " חברים", He.Format("cabinet.opposition", 45, "חברים"));
        Assert.AreEqual("המפלגה הגדולה: " + Lrm + "Mapai" + Lrm + ", " + Lrm + "46" + Lrm + " מנדטים", He.Format("cabinet.largest", "Mapai", 46, "מנדטים"));
        var appointed = He.Format("card.appointed_name", "דוד בן־גוריון");
        Assert.AreEqual("דוד בן־גוריון (מינוי שלך)", appointed);
        Assert.IsTrue(UiDirection.RightToLeft(appointed), appointed);
        var amount = He.Format("hud.say_million", "-IL ", "3.0");
        Assert.AreEqual(Lrm + "-IL " + Lrm + Lrm + "3.0" + Lrm + " מיליון", amount);
        Assert.AreEqual("כסף: " + amount + ", קיטון של " + Lrm + "IL " + Lrm + Lrm + "4.0" + Lrm + " מיליון מאז התור הקודם",
            He.Format("hud.say_down", "כסף", amount, He.Format("hud.say_million", "IL ", "4.0")));
    }

    /// <summary>A template with no letter of its own ("{0} {1}", "{0:N0}") is neither language: its values are left alone,
    /// and the line takes its direction from them.</summary>
    [Test]
    public void ATemplateWithNoLetterIsLeftAlone()
    {
        Assert.AreEqual("45 חברים", He.Format("cabinet.party_seats", 45, "חברים"));
        Assert.AreEqual("-$4.2m", He.Format("cabinet.select_office", "-$4.2m"));
        Assert.AreEqual(Plain(He, "hud.people", 1234567), He.Format("hud.people", 1234567));
    }

    /// <summary>A malformed pattern throws in Hebrew as string.Format does in English (a lone brace would otherwise reach
    /// the screen): a stray "}", a stray "{", a format item that is not a number.</summary>
    [Test]
    public void AMalformedHebrewPatternThrowsAsStringFormatDoes()
    {
        var t = UiText.Parse("a: \"שלום {0} }\"\nb: \"שלום { {0}\"\nc: \"שלום {x}\"\nd: \"שלום {{{0}}}\"\ne: \"hello {0} }\"", "test.yaml");
        foreach (var key in new[] { "a", "b", "c" })
        {
            var e = Assert.Throws<FormatException>(() => t.Format(key, "x"), key);
            StringAssert.Contains("test.yaml", e.Message, key);
            Assert.Throws<FormatException>(() => string.Format(CultureInfo.InvariantCulture, t[key], "x"), $"{key}: string.Format throws too");
        }
        Assert.Throws<FormatException>(() => t.Format("e", "x"), "English, as before");
        Assert.AreEqual("שלום {" + Lrm + "x" + Lrm + "}", t.Format("d", "x"), "escaped braces are not malformed");
    }

    /// <summary>English is unchanged byte for byte: every key of strings.en.yaml formats as string.Format did, with values
    /// that a Hebrew template would wrap (a negative amount, a date, a number).</summary>
    [Test]
    public void EnglishIsUnchangedByteForByte()
    {
        var en = En;
        var keys = Keys(UiPaths.Strings);
        Assert.Greater(keys.Count, 100, "the keys are read");
        var args = new object[] { "-$4.2m", "15 May 1948", 2.5, 46, "IL 3.0m", "Mapai", 7, 1948 };
        foreach (var key in keys)
        {
            string expected;
            try { expected = Plain(en, key, args); }
            catch (FormatException) { continue; }                       // a key whose format items need other types
            var got = en.Format(key, args);
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(got), key);
            Assert.AreEqual(-1, got.IndexOf(Lrm, StringComparison.Ordinal), key);
        }
        Assert.AreEqual("Money $12.0m · debt IL 3.0m · foreign currency -$4.2m", en.Format("budget.money", "$12.0m", "IL 3.0m", "-$4.2m"));
        Assert.AreEqual("15 May 1948 to 1 June 1948", en.Format("why.turn", "15 May 1948", "1 June 1948"));
    }

    /// <summary>Every Hebrew key formats to the English-free pattern with marks only around the values: taking the marks
    /// out gives string.Format's text back.</summary>
    [Test]
    public void TakingTheMarksOutOfAnyHebrewLineGivesTheOldTextBack()
    {
        var he = He;
        var args = new object[] { "-$4.2m", "15 May 1948", 2.5, 46, "IL 3.0m", "Mapai", 7, 1948 };
        int wrapped = 0;
        foreach (var key in Keys(UiPaths.StringsHebrew))
        {
            string expected;
            try { expected = Plain(he, key, args); }
            catch (FormatException) { continue; }
            var got = he.Format(key, args);
            Assert.AreEqual(expected, got.Replace(Lrm, ""), key);
            if (got.IndexOf(Lrm, StringComparison.Ordinal) >= 0) wrapped++;
        }
        Assert.Greater(wrapped, 20, "Hebrew templates with values are wrapped");
    }

    [Test]
    public void TheFirstLetterRuleSkipsTheMarks()
    {
        foreach (var s in new[] { Lrm + "45" + Lrm + " חברים", "‏" + Lrm + "אוצר", Lrm + "-4.2%" + Lrm + " · אוצר" + Lrm })
            Assert.IsTrue(UiDirection.RightToLeft(s), s);
        foreach (var s in new[] { Lrm + "15 May 1948" + Lrm + " עד", "‏" + "Treasury", Lrm + Lrm, Lrm + "-$4.2m" + Lrm })
            Assert.IsFalse(UiDirection.RightToLeft(s), s);
    }

    static List<string> Keys(string path) =>
        new YamlDotNet.Serialization.DeserializerBuilder().Build()
            .Deserialize<Dictionary<string, string>>(AssetDatabase.LoadAssetAtPath<TextAsset>(path).text).Keys.ToList();

    // --- Rendered ------------------------------------------------------------------------------------------------------

    const int W = 1600, RowH = 70, FontSize = 40;

    /// <summary>Labels set as the Hebrew UI sets them (.lang-he on a parent: Frank Ruhl Libre through the advanced
    /// generator; the paragraph direction from the text's first letter, as UiDirection does), one per row, rendered at
    /// one pixel per point; the rows' pixels once ink shows in each.</summary>
    sealed class Rendered : IDisposable
    {
        readonly RenderTexture rt; readonly PanelSettings ps; readonly GameObject go; readonly Texture2D tex;
        public readonly List<Label> Labels = new List<Label>();
        readonly int h;

        public Rendered(params string[] texts)
        {
            h = RowH * texts.Length;
            rt = new RenderTexture(W, h, 0);
            ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            ps.scaleMode = PanelScaleMode.ConstantPixelSize; ps.scale = 1f;
            ps.clearColor = true; ps.colorClearValue = Color.black;
            go = new GameObject("HebrewValuesTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            go.SetActive(true);
            doc.rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UiPaths.Folder + "/Statecraft.uss"));
            var he = new VisualElement();
            he.AddToClassList(UiLanguage.HebrewClass);
            he.style.position = Position.Absolute; he.style.left = 0; he.style.top = 0; he.style.width = W; he.style.height = h;
            doc.rootVisualElement.Add(he);
            for (int i = 0; i < texts.Length; i++)
            {
                var l = new Label(texts[i]);
                l.languageDirection = UiDirection.RightToLeft(texts[i]) ? LanguageDirection.RTL : LanguageDirection.LTR;
                l.style.position = Position.Absolute; l.style.left = 40; l.style.top = i * RowH; l.style.width = W - 80; l.style.height = RowH;
                l.style.fontSize = FontSize; l.style.color = Color.white; l.style.unityTextAlign = TextAnchor.MiddleLeft;
                l.style.marginLeft = l.style.marginRight = l.style.marginTop = l.style.marginBottom = 0;
                l.style.paddingLeft = l.style.paddingRight = l.style.paddingTop = l.style.paddingBottom = 0;
                l.style.whiteSpace = WhiteSpace.NoWrap;
                he.Add(l);
                Labels.Add(l);
            }
            tex = new Texture2D(W, h, TextureFormat.RGBA32, false);
        }

        /// <summary>Row <paramref name="row"/>'s grey levels, [x, y] with y = 0 at the row's top.</summary>
        public float[,] Row(int row)
        {
            var g = new float[W, RowH];
            for (int y = 0; y < RowH; y++)
            {
                int ty = h - 1 - (row * RowH + y);                            // ReadPixels row 0 is the bottom
                for (int x = 0; x < W; x++) g[x, y] = tex.GetPixel(x, ty).grayscale;
            }
            return g;
        }

        public IEnumerator Draw()
        {
            for (int frame = 0; frame < 30; frame++)
            {
                yield return null;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, W, h), 0, 0); tex.Apply();
                RenderTexture.active = null;
                if (Enumerable.Range(0, Labels.Count).All(r => Ink(Row(r)) > 20f)) yield break;
            }
            Assert.Fail("the labels were not drawn (no ink in the render)");
        }

        public void Dispose() { Object.DestroyImmediate(tex); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
    }

    static float Ink(float[,] g) { float s = 0; foreach (var v in g) s += v; return s; }

    /// <summary>The height in pixels of the ink in the leftmost <paramref name="cols"/> columns that carry ink: a minus is
    /// a short dash, a dollar sign is taller than the digits.</summary>
    static int LeftmostInkHeight(float[,] g, int cols = 4)
    {
        int first = Enumerable.Range(0, W).First(x => Enumerable.Range(0, RowH).Any(y => g[x, y] > 0.4f));
        int top = RowH, bottom = -1;
        for (int x = first; x < first + cols; x++)
            for (int y = 0; y < RowH; y++)
                if (g[x, y] > 0.4f) { top = Math.Min(top, y); bottom = Math.Max(bottom, y); }
        return bottom - top + 1;
    }

    /// <summary>The unmarked why.turn line as the bidi rules draw it, written as a left-to-right paragraph whose runs need
    /// no reordering past the Hebrew words: each date's day has moved to the right of its month and year.</summary>
    const string Split = "June 1948 1 עד May 1948 15מ־";

    /// <summary>The words of a row, left to right: runs of inked columns split where at least six columns in a row carry
    /// no ink (a space at this size is wider; the gaps inside a word are narrower).</summary>
    static List<(int Start, int End)> Words(float[,] g)
    {
        var words = new List<(int, int)>();
        int start = -1, last = -1;
        for (int x = 0; x < W; x++)
        {
            bool ink = false;
            for (int y = 0; y < RowH && !ink; y++) ink = g[x, y] > 0.4f;
            if (!ink) continue;
            if (start >= 0 && x - last > 6) { words.Add((start, last)); start = -1; }
            if (start < 0) start = x;
            last = x;
        }
        if (start >= 0) words.Add((start, last));
        return words;
    }

    /// <summary>Whether <paramref name="n"/> columns of two rows from the given starts carry alike ink: the columns' summed
    /// grey differs by under a quarter of the ink (a glyph placed a fraction of a pixel apart differs by about a tenth).</summary>
    static bool Same(float[,] a, int xa, float[,] b, int xb, int n)
    {
        float diff = 0, ink = 0;
        for (int x = 0; x < n; x++)
        {
            float ca = 0, cb = 0;
            for (int y = 0; y < RowH; y++) { ca += a[xa + x, y]; cb += b[xb + x, y]; }
            diff += Math.Abs(ca - cb); ink += cb;
        }
        return diff < 0.25f * ink;
    }

    static string Show(List<(int Start, int End)> w) => string.Join(" ", w.Select(p => $"{p.Start}-{p.End}"));

    /// <summary>Whether <paramref name="line"/> draws <paramref name="part"/>'s words (one run on its own row) side by side
    /// in the same order: the same widths and the same gaps between them, to a pixel (the last word at least as wide), and
    /// each word's ink alike.</summary>
    static bool Found(float[,] line, float[,] part)
    {
        var l = Words(line); var p = Words(part);
        for (int i = 0; i + p.Count <= l.Count; i++)
        {
            bool all = true;
            for (int k = 0; k < p.Count && all; k++)
            {
                int lw = l[i + k].End - l[i + k].Start, pw = p[k].End - p[k].Start;
                // The last word may run on into a neighbour with no space between (the date's year into "מ־").
                all &= k == p.Count - 1 ? lw >= pw - 1 : Math.Abs(lw - pw) <= 1;
                all &= Same(line, l[i + k].Start, part, p[k].Start, pw + 1);
                if (k > 0) all &= Math.Abs((l[i + k].Start - l[i + k - 1].End) - (p[k].Start - p[k - 1].End)) <= 1;
            }
            if (all) return true;
        }
        return false;
    }

    static float MaxDiff(float[,] a, float[,] b)
    {
        float m = 0;
        for (int x = 0; x < W; x++) for (int y = 0; y < RowH; y++) m = Math.Max(m, Math.Abs(a[x, y] - b[x, y]));
        return m;
    }

    /// <summary>The check #543 asked for first: the Hebrew face through the advanced generator honours U+200E (a minus
    /// wrapped in marks is drawn on the left of its dollar in a right-to-left line, an English date stays one run) and
    /// draws nothing for it (no glyph, no box, no advance: a line with marks has the same pixels as the line without).</summary>
    [UnityTest]
    public IEnumerator TheHebrewFaceHonoursTheMarkAndDrawsNothingForIt()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontHebrew);
        Debug.Log($"#543 probe: Frank Ruhl Libre HasCharacter(U+200E) = {font.HasCharacter('‎')}");
        using var r = new Rendered(
            "מטבע חוץ " + Lrm + "-$4.2m" + Lrm,                                  // 0: marked
            "מטבע חוץ -$4.2m",                                                   // 1: the control
            "אב",                                                                // 2
            "א" + Lrm + "ב",                                                     // 3: a mark between two letters
            "-$4.2m",                                                            // 4
            Lrm + "-$4.2m" + Lrm,                                                // 5: marks at the ends of a left-to-right line
            "מ־" + Lrm + "15 May 1948" + Lrm + " עד " + Lrm + "1 June 1948" + Lrm, // 6: marked
            "מ־15 May 1948 עד 1 June 1948",                                      // 7: the control
            "15 May 1948",                                                       // 8: the date alone
            Split);                                                              // 9: the order the control has
        yield return r.Draw();
        Assert.AreEqual("Advanced", r.Labels[0].resolvedStyle.unityTextGenerator.ToString());
        Assert.AreSame(font, r.Labels[0].resolvedStyle.unityFontDefinition.fontAsset != null
            ? r.Labels[0].resolvedStyle.unityFontDefinition.fontAsset.sourceFontFile : r.Labels[0].resolvedStyle.unityFontDefinition.font);
        Assert.AreEqual(LanguageDirection.RTL, r.Labels[0].languageDirection);
        int marked = LeftmostInkHeight(r.Row(0)), control = LeftmostInkHeight(r.Row(1));
        float between = MaxDiff(r.Row(2), r.Row(3)), ends = MaxDiff(r.Row(4), r.Row(5));
        bool dateMarked = Found(r.Row(6), r.Row(8)), dateControl = Found(r.Row(7), r.Row(8));
        float controlSplit = MaxDiff(r.Row(7), r.Row(9));
        Vector2 w2 = r.Labels[2].MeasureTextSize(r.Labels[2].text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
        Vector2 w3 = r.Labels[3].MeasureTextSize(r.Labels[3].text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
        Debug.Log($"#543 probe: leftmost ink height marked {marked}px, control {control}px; max pixel diff with/without a mark: between letters {between:0.000}, " +
                  $"at the ends {ends:0.000}; widths {w2.x:0.00} / {w3.x:0.00}; control vs the split order {controlSplit:0.000}; " +
                  $"words of \"15 May 1948\" {Show(Words(r.Row(8)))}, of the marked line {Show(Words(r.Row(6)))}, of the control {Show(Words(r.Row(7)))}");
        Assert.AreEqual(3, Words(r.Row(8)).Count, "\"15 May 1948\" is three words");
        Assert.Greater(control, FontSize / 2, $"control: unmarked, the dollar sign is the leftmost glyph ({control}px tall)");
        Assert.Less(marked, FontSize / 5, $"marked, the minus is the leftmost glyph ({marked}px tall)");
        Assert.Less(between, 0.02f, "a mark between two Hebrew letters draws nothing and takes no room");
        Assert.Less(ends, 0.02f, "marks at the ends of a line draw nothing and take no room");
        Assert.AreEqual(w2.x, w3.x, 0.01f, "a mark has no advance");
        Assert.Less(controlSplit, 0.05f, "control: unmarked, each day is drawn right of its month and year");
        Assert.IsFalse(dateControl, "control: unmarked, \"15 May 1948\" is nowhere in the line (the check can fail)");
        Assert.IsTrue(dateMarked, "marked, \"15 May 1948\" is drawn as one run, as it is on its own");
    }

    /// <summary>The lines the game shows (#543's "done when"): Hebrew budget.money with negative foreign currency draws its
    /// minus on the left of the dollar, and Hebrew why.turn draws "15 May 1948" as one run.</summary>
    [UnityTest]
    public IEnumerator InAHebrewLineTheMinusIsLeftOfItsDollarAndTheDateIsOneRun()
    {
        using var r = new Rendered(
            He.Format("budget.money", "$12.0m", "IL 3.0m", "-$4.2m"),
            He.Format("why.turn", "15 May 1948", "1 June 1948"),
            "15 May 1948");
        yield return r.Draw();
        Assert.AreEqual(LanguageDirection.RTL, r.Labels[0].languageDirection, r.Labels[0].text);
        Assert.AreEqual(LanguageDirection.RTL, r.Labels[1].languageDirection, r.Labels[1].text);
        int minus = LeftmostInkHeight(r.Row(0));
        Assert.Less(minus, FontSize / 5, $"the leftmost glyph of the line (its last value, '-$4.2m') is the minus ({minus}px tall), not the dollar");
        Assert.IsTrue(Found(r.Row(1), r.Row(2)), $"\"15 May 1948\" is drawn as one run, as it is on its own: its words {Show(Words(r.Row(2)))}, the line's {Show(Words(r.Row(1)))}");
    }
}
