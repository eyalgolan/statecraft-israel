#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#2227: GameRoot.Begin (New Game, a load, the language switch) makes a new MinimapPresenter over the same
/// document and the same map, which is built once at boot. Its Borders and Labels buttons read "on" while the map's
/// "borders" and "labels" switches stayed off from the last press, so the next press "turned off" something already off.
/// Same harness as ScreenEntryTests.</summary>
public class MinimapRestartTests
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-minimaprestart-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    IEnumerator Boot()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(1280, 720, 0);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(MapFocusTests.Inputs.Map);
        root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(MapFocusTests.Inputs, 0);
        ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(3);
        GameSceneTests.HideTheOpeningCard(ui);
        yield return Frames(2);
    }

    /// <summary>The "labels" switch's real state: MapLabelsPresenter's switch hides the names' container inline.</summary>
    bool LabelsOn => ui.Q(MapLabelsPresenter.ContainerName).style.display.value != DisplayStyle.None;

    bool CaptionShown => ui.Q<Label>("map-layer").style.display.value == DisplayStyle.Flex;

    /// <summary>Borders and Labels pressed off, then the language switch's Begin (GameRoot.SwitchLanguage: the same game,
    /// kept): each button of the new minimap says what its switch really does, the player's choice is kept, and one press
    /// brings the layer back.</summary>
    [UnityTest]
    public IEnumerator AfterARestartTheMinimapButtonsMatchTheMapsSwitches()
    {
        yield return Boot();
        try
        {
            Assert.IsTrue(VisualToggles.Names.Contains(MapBorders.ToggleName) && VisualToggles.Names.Contains(MinimapPresenter.LabelsToggle), "precondition: the map's switches");
            var first = root.Minimap ?? throw new AssertionException("precondition: Begin made a minimap");
            UiTestKit.Press(ui.Q<Button>("minimap-borders"));
            UiTestKit.Press(ui.Q<Button>("minimap-labels"));
            Assert.IsFalse(MapBorders.ToggleOn, "precondition: the borders switch is off");
            Assert.IsFalse(LabelsOn, "precondition: the labels switch is off");
            Assert.IsFalse(first.BordersShown || first.LabelsShown, "precondition: the first minimap's buttons read off");

            root.Language = UiLanguage.Hebrew;                     // as SwitchLanguage does: a new language over this game
            root.Begin(MapFocusTests.Inputs, 0, keep: root.Bridge);
            GameSceneTests.HideTheOpeningCard(ui);
            yield return Frames(3);
            var mini = root.Minimap ?? throw new AssertionException("precondition: the restart made a minimap");
            Assert.AreNotSame(first, mini, "precondition: Begin made a new minimap");

            // The buttons' dim against the switches' own state (review of #2244, C1: BordersShown is read from the switch, so
            // comparing the two would hold whatever the button shows).
            Assert.AreEqual(!MapBorders.ToggleOn, ui.Q<Button>("minimap-borders").ClassListContains(MinimapPresenter.OffClass), "the Borders button's dim says what the borders switch does");
            Assert.AreEqual(!LabelsOn, ui.Q<Button>("minimap-labels").ClassListContains(MinimapPresenter.OffClass), "the Labels button's dim says what the labels switch does");
            Assert.IsFalse(mini.BordersShown || mini.LabelsShown, "the new minimap reads both layers off");
            Assert.IsFalse(MapBorders.ToggleOn || LabelsOn, "the restart keeps the player's choice: both layers stay off");
            // Review of #2231, C2: MapRegions.Shown is false when no layer is drawn, and then both caption asserts hold
            // whatever the caption does; the start date draws one, so they pin it.
            Assert.IsTrue(root.Regions!.Shown, "precondition: the start date draws a region layer");
            Assert.AreEqual(root.Regions!.Shown && MapBorders.ToggleOn, CaptionShown, "the border caption follows the borders");

            UiTestKit.Press(ui.Q<Button>("minimap-borders"));
            UiTestKit.Press(ui.Q<Button>("minimap-labels"));
            Assert.IsTrue(MapBorders.ToggleOn && mini.BordersShown, "one press brings the borders back");
            Assert.IsTrue(LabelsOn && mini.LabelsShown, "one press brings the names back");
            Assert.AreEqual(root.Regions!.Shown, CaptionShown, "and the border caption with them");
        }
        finally { SwitchesOn(); }
    }

    /// <summary>Review of #2231, C1: the map's switches are the truth, not the buttons' dim. A switch set behind its button's
    /// back (VisualToggles.Set, as the dev instruments do) disagrees with the button until the next Begin, whose minimap
    /// starts from the switches and dims its buttons to match; the armistice lines go with the Borders button (#2195).</summary>
    [UnityTest]
    public IEnumerator AfterARestartTheButtonsFollowTheSwitchesNotTheirOldDim()
    {
        yield return Boot();
        try
        {
            Assert.IsTrue(VisualToggles.Names.Contains(MapBorders.ToggleName) && VisualToggles.Names.Contains(MinimapPresenter.LabelsToggle)
                && VisualToggles.Names.Contains(ArmisticeLines.ToggleName), "precondition: the map's switches, the armistice lines' too");
            var bordersButton = ui.Q<Button>("minimap-borders"); var labelsButton = ui.Q<Button>("minimap-labels");
            UiTestKit.Press(labelsButton);                                // the Labels button dims with its switch
            VisualToggles.Set(MapBorders.ToggleName, false);              // the borders go off behind an undimmed button
            VisualToggles.Set(MinimapPresenter.LabelsToggle, true);       // the names come back behind a dimmed one
            Assert.IsFalse(MapBorders.ToggleOn || bordersButton.ClassListContains(MinimapPresenter.OffClass), "precondition: borders off, their button not dimmed");
            Assert.IsTrue(LabelsOn && labelsButton.ClassListContains(MinimapPresenter.OffClass), "precondition: names on, their button dimmed");
            Assert.IsTrue(ArmisticeOn, "precondition: the armistice lines are still shown");

            root.Begin(MapFocusTests.Inputs, 0, keep: root.Bridge);      // a restart over the same map (as SwitchLanguage)
            GameSceneTests.HideTheOpeningCard(ui);
            yield return Frames(3);
            var mini = root.Minimap ?? throw new AssertionException("precondition: the restart made a minimap");

            Assert.IsFalse(mini.BordersShown, "the Borders button reads the borders switch: off");
            Assert.IsTrue(bordersButton.ClassListContains(MinimapPresenter.OffClass), "and dims to say so");
            Assert.IsTrue(mini.LabelsShown, "the Labels button reads the names' switch: on");
            Assert.IsFalse(labelsButton.ClassListContains(MinimapPresenter.OffClass), "and is not dimmed");
            Assert.IsFalse(ArmisticeOn, "the armistice lines go with the Borders button");
            Assert.IsTrue(root.Regions!.Shown, "precondition: the start date draws a region layer");
            Assert.IsFalse(CaptionShown, "no border caption over hidden borders");

            UiTestKit.Press(bordersButton);
            Assert.IsTrue(MapBorders.ToggleOn && mini.BordersShown && ArmisticeOn, "one press brings the borders and the lines back");
            Assert.IsTrue(CaptionShown, "and the border caption with them");
        }
        finally { SwitchesOn(); }
    }

    /// <summary>The armistice lines' switch's real state: it shows or hides the overlay's "Lines" child.</summary>
    bool ArmisticeOn
    {
        get
        {
            var lines = Object.FindFirstObjectByType<MapScene>().Terrain.transform.Find(ArmisticeLines.RootName + "/Lines");
            Assert.IsNotNull(lines, "precondition: the map has the armistice overlay's Lines node");
            return lines.gameObject.activeSelf;
        }
    }

    /// <summary>The global state these tests change: VisualToggles' switches and MapBorders' toggleOn (static, set through
    /// the "borders" switch). The minimap's buttons, the GameRoot and its Language belong to the scene, which TearDown
    /// discards. Left on for the next test however the test ended.</summary>
    static void SwitchesOn()
    {
        foreach (var name in new[] { MapBorders.ToggleName, MinimapPresenter.LabelsToggle, ArmisticeLines.ToggleName })
            if (VisualToggles.Names.Contains(name)) VisualToggles.Set(name, true);
    }
}
