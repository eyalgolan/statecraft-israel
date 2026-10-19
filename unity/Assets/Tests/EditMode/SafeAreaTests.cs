using System.Collections;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class SafeAreaTests
{
    [Test]
    public void CssPixelsBecomePanelPixelsAtTheDevicesRatioAndScale()
    {
        // a 47 px iPhone notch at 3x, the panel scaled 1.5 device px per panel px: exactly 94 panel px (no .5 boundary)
        Assert.AreEqual(94, SafeAreaFit.Panel(47f, 3f, 1.5f));
        Assert.AreEqual(0, SafeAreaFit.Panel(0f, 3f, 1.5f), "no inset, no padding");
        Assert.AreEqual(0, SafeAreaFit.Panel(47f, 3f, 0f), "no scale yet, no padding");
    }

    [Test]
    public void DevicePixelsBecomePanelPixelsOverTheScale()
    {
        Assert.AreEqual(40, SafeAreaFit.PanelPx(48f, 1.2f));
        Assert.AreEqual(0, SafeAreaFit.PanelPx(48f, 0f), "no scale yet, no padding");
    }

    /// <summary>The padding itself: Apply writes it, the layout follows (the top bar clears the notch), and Dispose
    /// puts it back. Red without the writes in Apply, and red without the reset in Dispose.</summary>
    [UnityTest]
    public IEnumerator TheRootIsPaddedAndDisposePutsItBack()
    {
        using var ui = new UiTestKit();
        var insets = (l: 0f, r: 0f, t: 0f, b: 0f);
        using var fit = new SafeAreaFit(ui.Root, () => insets);
        // Let the first layout settle, then keep the reader consistent with the synthetic insets under test.
        // The production poll remains active; it must not replace this fixture's padding with editor screen margins.
        for (int i = 0; i < 20 && float.IsNaN(ui.Root.resolvedStyle.width); i++) yield return null;
        yield return null; yield return null;
        insets = (10f, 20f, 47f, 34f);
        fit.Apply(insets.l, insets.r, insets.t, insets.b);
        // What Apply wrote, exactly; the resolved padding snaps to the panel's pixel grid (47 -> 46.29 at this scale).
        Assert.AreEqual(47f, ui.Root.style.paddingTop.value.value, 1e-3f, "the notch's inset pads the root");
        Assert.AreEqual(34f, ui.Root.style.paddingBottom.value.value, 1e-3f, "the home indicator's inset");
        Assert.AreEqual(10f, ui.Root.style.paddingLeft.value.value, 1e-3f);
        Assert.AreEqual(20f, ui.Root.style.paddingRight.value.value, 1e-3f);
        for (int i = 0; i < 10 && ui.Root.resolvedStyle.paddingTop < 45f; i++)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
        }
        Assert.AreEqual(47f, ui.Root.resolvedStyle.paddingTop, 1f, "the layout follows, snapped to the pixel grid");
        Assert.AreEqual(34f, ui.Root.resolvedStyle.paddingBottom, 1f);
        Assert.GreaterOrEqual(ui.Q<VisualElement>("top-bar").worldBound.yMin, 47f - 1f, "the top bar moves down out of the notch");
        fit.Dispose();
        Assert.AreEqual(StyleKeyword.Null, ui.Root.style.paddingTop.keyword, "Dispose resets the style");
        Assert.AreEqual(StyleKeyword.Null, ui.Root.style.paddingLeft.keyword);
        for (int i = 0; i < 10 && ui.Root.resolvedStyle.paddingTop > 0f; i++)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
        }
        Assert.AreEqual(0f, ui.Root.resolvedStyle.paddingTop, 1f, "Dispose resets the padding");
    }

    /// <summary>The review probe: two landscapes of the same dimensions swap the notch side with the root bounds
    /// unchanged, so no GeometryChangedEvent fires. The per-frame poll still moves the padding: settle on the left,
    /// flip to the right, and the padding follows without the test calling Update/Apply itself.</summary>
    [UnityTest]
    public IEnumerator InsetChangesArePickedUpWhenTheBoundsDoNotChange()
    {
        using var ui = new UiTestKit();
        var insets = (l: 47f, r: 0f, t: 0f, b: 0f);
        int reads = 0;
        using var fit = new SafeAreaFit(ui.Root, () => { reads++; return insets; });
        for (int i = 0; i < 20 && float.IsNaN(ui.Root.resolvedStyle.width); i++) yield return null;
        yield return null; yield return null;
        Assert.AreEqual(47f, ui.Root.style.paddingLeft.value.value, 1e-3f, "settled: the left inset pads the left");
        var bounds = ui.Root.layout;
        insets = (0f, 47f, 0f, 0f);   // the notch moved to the right edge; the bounds did not change
        // A quiet EditMode runtime panel needs player-loop requests to service scheduled callbacks.
        for (int i = 0; i < 30; i++)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
        }
        Assert.AreEqual(bounds, ui.Root.layout, "the root bounds did not change");
        Assert.AreEqual(47f, ui.Root.style.paddingRight.value.value, 1e-3f, "the poll moved the padding to the right");
        Assert.AreEqual(0f, ui.Root.style.paddingLeft.value.value, 1e-3f, "and cleared the left");
        fit.Dispose();
        int stoppedReads = reads;
        insets = (12f, 34f, 56f, 78f);
        for (int i = 0; i < 10; i++)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
        }
        Assert.AreEqual(stoppedReads, reads, "Dispose stops polling the reader");
        Assert.AreEqual(StyleKeyword.Null, ui.Root.style.paddingLeft.keyword);
        Assert.AreEqual(StyleKeyword.Null, ui.Root.style.paddingRight.keyword);
    }
}
