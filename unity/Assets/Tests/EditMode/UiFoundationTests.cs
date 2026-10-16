using System.Collections;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class UiFoundationTests
{
    [Test]
    public void PanelSettingsScaleWithA1080pReference()
    {
        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel);
        Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, ps.scaleMode);
        Assert.AreEqual(new Vector2Int(1920, 1080), ps.referenceResolution);
        Assert.AreEqual(PanelScreenMatchMode.MatchWidthOrHeight, ps.screenMatchMode);
        Assert.AreEqual(0.5f, ps.match);
        Assert.IsNotNull(ps.themeStyleSheet, "theme");
    }

    [Test]
    public void ASubmittedButtonFiresClickedSynchronously()
    {
        using var ui = new UiTestKit();
        int clicks = 0;
        ui.Q<Button>("end-turn").clicked += () => clicks++;
        ui.Press("end-turn");
        Assert.AreEqual(1, clicks);
    }

    [Test]
    public void TheLayoutHasEveryRegionOfTheTargetScreen()
    {
        using var ui = new UiTestKit();
        foreach (var name in new[] { "top-bar", "event-card", "info-panel", "minimap", "screen-cabinet", "screen-budget", "screen-defence", "screen-why", "toast" })
            ui.Q<VisualElement>(name);
        foreach (var name in new[] { "treasury", "immigration", "security", "support", "cohesion", "standing", "fx" })
        {
            ui.Q<Label>(name + "-value");
            ui.Q<Label>(name + "-delta");
        }
        Assert.AreEqual(PickingMode.Ignore, ui.Q<VisualElement>("root").pickingMode, "empty screen areas must let clicks reach the map");
    }

    [UnityTest]
    public IEnumerator TextUsesTheSerifFaces()
    {
        using var ui = new UiTestKit();
        for (int i = 0; i < 10 && ui.Q<Label>("year").resolvedStyle.unityFontDefinition.font == null; i++) yield return null;
        var regular = AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontRegular);
        var semibold = AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontSemiBold);
        Assert.AreSame(semibold, ui.Q<Label>("year").resolvedStyle.unityFontDefinition.font);
        Assert.AreSame(regular, ui.Q<Label>("turn").resolvedStyle.unityFontDefinition.font);
    }

    [Test]
    public void StringsLoadFromTheYamlFileAndFormatInvariantly()
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        Assert.AreEqual("Turn 3", text.Format("hud.turn", 3));
        Assert.AreEqual("1,234,567", text.Format("hud.people", 1234567.0));
        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => { var _ = text["no.such.key"]; });
    }

    [UnityTest]
    public IEnumerator PickingFindsButtonsButNotTheTransparentRoot()
    {
        using var ui = new UiTestKit();
        foreach (var name in new[] { "screen-cabinet", "screen-budget", "screen-defence", "screen-why", "toast" })
            ui.Q<VisualElement>(name).style.display = DisplayStyle.None;   // closed, as at game start
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");        // ready, as GameRoot leaves it
        var button = ui.Q<Button>("end-turn");
        for (int i = 0; i < 10 && float.IsNaN(button.worldBound.width); i++) yield return null;
        var panel = ui.Root.panel;
        Assert.AreSame(button, panel.Pick(button.worldBound.center));
        Assert.IsNull(panel.Pick(ui.Root.worldBound.center), "the empty middle of the screen must reach the map");
        // Input System screen positions are y-up; ToPanel flips y before RuntimePanelUtils.ScreenToPanel (which does not).
        Assert.AreEqual(RuntimePanelUtils.ScreenToPanel(panel, new Vector2(10, 20)), UiPointer.ToPanel(panel, new Vector2(10, 1060), 1080f));
    }

    [UnityTest]
    public IEnumerator ButtonsUseTheStatecraftTheme()
    {
        using var ui = new UiTestKit();
        var navy = new Color(9 / 255f, 14 / 255f, 26 / 255f, 1f);
        var goldDim = new Color(196 / 255f, 160 / 255f, 92 / 255f, 0.45f);
        var gold = new Color(196 / 255f, 160 / 255f, 92 / 255f, 1f);
        // A screen button is a round, gold-rimmed navy disc with the screen's name under it (promo HUD pass): the disc
        // carries the theme's navy and dim gold.
        var nav = ui.Q<Button>("nav-cabinet").Q<UiIcon>(className: "nav-disc");
        Assert.IsNotNull(nav, "the screen button has its disc");
        for (int i = 0; i < 10 && nav.resolvedStyle.backgroundColor.a < 0.5f; i++) yield return null;
        void Near(Color e, Color a, string what) => Assert.That(Mathf.Abs(e.r - a.r) + Mathf.Abs(e.g - a.g) + Mathf.Abs(e.b - a.b) + Mathf.Abs(e.a - a.a), Is.LessThan(0.02f), $"{what}: expected {e} got {a}");
        Near(navy, nav.resolvedStyle.backgroundColor, "nav background");
        Near(goldDim, nav.resolvedStyle.borderTopColor, "nav border");
        var end = ui.Q<Button>("end-turn");
        Near(navy, end.resolvedStyle.backgroundColor, "end-turn background");
        Near(gold, end.resolvedStyle.borderTopColor, "end-turn border");
    }

    [Test]
    public void AMissingKeyNamesTheFileGivenToParse()
    {
        var text = UiText.Parse("a: b", "other.yaml");
        var e = Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => { var _ = text["x"]; });
        StringAssert.Contains("other.yaml", e.Message);
    }

    /// <summary>Task 24 review M7: world-space settlement labels must not show through the top bar.</summary>
    [UnityTest]
    public IEnumerator TheTopBarIsOpaque()
    {
        using var ui = new UiTestKit();
        var bar = ui.Q<VisualElement>("top-bar");
        for (int i = 0; i < 10 && bar.resolvedStyle.backgroundColor.a < 0.5f; i++) yield return null;
        Assert.AreEqual(1f, bar.resolvedStyle.backgroundColor.a, 1e-3f);
    }

    /// <summary>Playtest 2, P2-1: until GameRoot has started the game, the tree shows only a styled loading line: no screen,
    /// no event card (and never its DRAFT placeholder), no empty top-bar values, info panel or minimap.</summary>
    [UnityTest]
    public IEnumerator BeforeTheGameIsReadyOnlyALoadingLineShows()
    {
        using var ui = new UiTestKit();
        var loading = ui.Q<Label>("loading");
        for (int i = 0; i < 10 && float.IsNaN(loading.worldBound.width); i++) yield return null;
        Assert.IsTrue(ui.Q<VisualElement>("root").ClassListContains("loading"), "the tree starts in the loading state");
        Assert.AreEqual(DisplayStyle.Flex, loading.resolvedStyle.display);
        Assert.AreEqual(1f, ui.Q<VisualElement>("root").resolvedStyle.backgroundColor.a, 1e-3f, "a navy page while loading, not the grey sky (final review M1)");
        Assert.IsNotEmpty(loading.text);
        foreach (var s in ScreenNav.Screens)
            Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("screen-" + s).resolvedStyle.display, s);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-card").resolvedStyle.display, "event card");
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-draft").resolvedStyle.display, "DRAFT placeholder");
        foreach (var cls in new[] { "date-box", "meters", "nav", "turn-box" })
            Assert.AreEqual(Visibility.Hidden, ui.Root.Q(className: cls).resolvedStyle.visibility, cls);
        foreach (var name in new[] { "info-panel", "right-column" })
            Assert.AreEqual(Visibility.Hidden, ui.Q<VisualElement>(name).resolvedStyle.visibility, name);
    }
}
