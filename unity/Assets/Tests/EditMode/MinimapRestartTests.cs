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

            Assert.AreEqual(MapBorders.ToggleOn, mini.BordersShown, "the Borders button says what the borders switch does");
            Assert.AreEqual(LabelsOn, mini.LabelsShown, "the Labels button says what the labels switch does");
            Assert.IsFalse(MapBorders.ToggleOn || LabelsOn, "the restart keeps the player's choice: both layers stay off");
            Assert.AreEqual(root.Regions!.Shown && MapBorders.ToggleOn, CaptionShown, "the border caption follows the borders");

            UiTestKit.Press(ui.Q<Button>("minimap-borders"));
            UiTestKit.Press(ui.Q<Button>("minimap-labels"));
            Assert.IsTrue(MapBorders.ToggleOn && mini.BordersShown, "one press brings the borders back");
            Assert.IsTrue(LabelsOn && mini.LabelsShown, "one press brings the names back");
            Assert.AreEqual(root.Regions!.Shown, CaptionShown, "and the border caption with them");
        }
        finally
        {
            // VisualToggles and MapBorders are static: leave both switches on for the next test.
            if (VisualToggles.Names.Contains(MapBorders.ToggleName)) VisualToggles.Set(MapBorders.ToggleName, true);
            if (VisualToggles.Names.Contains(MinimapPresenter.LabelsToggle)) VisualToggles.Set(MinimapPresenter.LabelsToggle, true);
        }
    }
}
