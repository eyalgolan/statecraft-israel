#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Spec 7.3 in the real scene: End turn, blocked while a unit needs orders, selects that unit with its card usable
/// even while a turn's event card is open (the event card is modal: it disables the unit card). Found by the WebGL smoke
/// run, which stalled on turn 3 when the turn's card stayed open.</summary>
public class EndTurnCycleSceneTests
{
    RenderTexture? rt; PanelSettings? ps; string dir = "";

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (dir != "" && Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    [UnityTest]
    public IEnumerator BlockedEndTurnHidesAnOpenEventCardSoTheUnitCanBeSkipped()
    {
        VisualElement ui = null!; GameRoot root = null!;
        dir = Path.Combine(Path.GetTempPath(), "statecraft-endturn-cycle-" + Guid.NewGuid().ToString("N"));
        yield return CivMovementShots.Boot(false, dir, (t, p, u, g) => { rt = t; ps = p; ui = u; root = g; });
        var bridge = root.Bridge!;
        foreach (var id in bridge.NeedsOrders.ToList()) bridge.Skip(id);
        UiTestKit.Press(ui.Q<Button>("end-turn"));
        yield return null;
        if (ui.Q("root").ClassListContains(TurnReportPresenter.OpenClass)) UiTestKit.Press(ui.Q<Button>("turn-report-close"));
        yield return null;
        Assert.IsTrue(ui.Q("root").ClassListContains(EventCardPresenter.OpenClass), "precondition: the new turn's event card is open");
        Assert.IsFalse(ui.Q("info-panel").enabledInHierarchy, "precondition: the open card disables the unit card");
        var needs = bridge.NeedsOrders.Count;
        Assert.Greater(needs, 0, "precondition: units need orders");
        UiTestKit.Press(ui.Q<Button>("end-turn"));
        yield return null;
        Assert.IsTrue(ui.Q("info-panel").enabledInHierarchy, "the selected unit's card takes orders");
        UiTestKit.Press(ui.Q<Button>("info-skip"));
        yield return null;
        Assert.AreEqual(needs - 1, bridge.NeedsOrders.Count, "Skip took the selected unit out of the cycle");
    }
}
