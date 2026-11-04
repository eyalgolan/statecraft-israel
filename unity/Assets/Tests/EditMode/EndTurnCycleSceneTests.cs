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

    // The capture dialog is modal like the event card (#802): End turn hides it too, nothing chosen, so the unit it selects
    // can take an order. Found by the v0.3.5 WebGL smoke, stuck on 15 Aug 1948 behind Ramla's dialog (#2087's card order).
    [UnityTest]
    public IEnumerator BlockedEndTurnHidesAnOpenCaptureDialogSoTheUnitCanBeSkipped()
    {
        VisualElement ui = null!; GameRoot root = null!;
        dir = Path.Combine(Path.GetTempPath(), "statecraft-endturn-capture-" + Guid.NewGuid().ToString("N"));
        yield return CivMovementShots.Boot(false, dir, (t, p, u, g) => { rt = t; ps = p; ui = u; root = g; });
        var bridge = root.Bridge!;
        var g = Statecraft.Sim.GameSession.New(bridge.Content, Statecraft.Sim.GoldenRun.Seed, Statecraft.Sim.Mode.Historical);
        g.State.PendingEvents.Clear();
        g.State.Settlements["ramla"] = new Statecraft.Sim.SettlementControl(Statecraft.Sim.Movement.PlayerSide,
            Statecraft.Sim.SettlementControl.AwaitingChoice, g.State.Date);
        bridge.Replace(g);
        for (int i = 0; i < 5; i++) yield return null;
        Assert.IsTrue(ui.Q("root").ClassListContains(CaptureDialogPresenter.OpenClass), "precondition: Ramla's capture dialog is open");
        Assert.IsFalse(ui.Q("info-panel").enabledInHierarchy, "precondition: the open dialog disables the unit card");
        var needs = bridge.NeedsOrders.Count;
        Assert.Greater(needs, 0, "precondition: units need orders");
        UiTestKit.Press(ui.Q<Button>("end-turn"));
        yield return null;
        Assert.IsTrue(root.Capture!.Collapsed, "End turn hid the dialog");
        Assert.IsFalse(ui.Q("root").ClassListContains(CaptureDialogPresenter.OpenClass), "the root no longer says the dialog is open");
        Assert.IsTrue(ui.Q("info-panel").enabledInHierarchy, "the selected unit's card takes orders");
        UiTestKit.Press(ui.Q<Button>("info-skip"));
        yield return null;
        Assert.AreEqual(needs - 1, bridge.NeedsOrders.Count, "Skip took the selected unit out of the cycle");
        Assert.AreEqual("ramla", root.Capture.Shown, "nothing was chosen: the capture still waits, behind Decision waiting");
    }

    [UnityTest]
    public IEnumerator BlockedEndTurnClosesDefenceAndShowsTheUnit() =>
        BlockedEndTurnRevealsTheUnit("defence", twoRows: false);

    [UnityTest]
    public IEnumerator BlockedEndTurnClosesBudgetInTheTwoRowHudAndShowsTheUnit() =>
        BlockedEndTurnRevealsTheUnit("budget", twoRows: true);

    IEnumerator BlockedEndTurnRevealsTheUnit(string screen, bool twoRows)
    {
        VisualElement ui = null!; GameRoot root = null!;
        dir = Path.Combine(Path.GetTempPath(), "statecraft-endturn-covered-" + Guid.NewGuid().ToString("N"));
        yield return CivMovementShots.Boot(false, dir, (t, p, u, g) => { rt = t; ps = p; ui = u; root = g; });
        var bridge = root.Bridge!;
        Assert.Greater(bridge.NeedsOrders.Count, 1, "precondition: several units need orders");
        root.Units!.Select(null); root.Info!.Clear();
        var next = bridge.NeedsOrders[0];
        var token = root.Units.Tokens.First(t => UnitCard.SimUnit(bridge.Content, bridge.State, t.Token) == next);
        var text = UiText.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        var expectedTitle = UnitCard.For(token, bridge.State, text, bridge.Content.IsGdd, bridge.Content).Title;
        var date = bridge.State.Date; var needs = bridge.NeedsOrders.Count;
        // Exercise both existing cover rules. The HUD sets its tier from the panel width on every layout, so a class set
        // by hand is gone after the next frame: the two-row case renders at 960x1080, as DecisionCardTests' narrow tier.
        if (twoRows)
        {
            var narrow = new RenderTexture(960, 1080, 24, RenderTextureFormat.ARGB32);
            ps!.targetTexture = narrow;
            rt!.Release(); Object.DestroyImmediate(rt); rt = narrow;
            for (int i = 0; i < 10; i++) yield return null;
        }
        Assert.AreEqual(twoRows, ui.Q("root").ClassListContains(HudPresenter.TwoRowClass), "precondition: the HUD tier");
        UiTestKit.Press(ui.Q<Button>("nav-" + screen));
        yield return null;
        Assert.IsTrue(root.Info.Covered, "precondition: the real side screen covers the card");
        Assert.IsFalse(ui.Q("info-panel").enabledInHierarchy);
        Assert.IsTrue(ui.Q<Button>("end-turn").enabledInHierarchy);
        UiTestKit.Press(ui.Q<Button>("end-turn"));
        // Allow ScreenNav's normal close transition to finish, rather than disabling motion in the fixture.
        for (int i = 0; i < 30; i++) yield return null;
        Assert.IsFalse(ui.Q("root").ClassListContains(ScreenNav.ScreenOpenClass));
        Assert.AreEqual(DisplayStyle.None, ui.Q("screen-" + screen).resolvedStyle.display);
        Assert.IsFalse(root.Info.Covered);
        Assert.IsTrue(ui.Q("info-panel").enabledInHierarchy);
        Assert.AreNotEqual(DisplayStyle.None, ui.Q("info-panel").resolvedStyle.display);
        Assert.Greater(ui.Q("info-panel").worldBound.width, 0);
        Assert.IsTrue(root.Info.ShowsUnit);
        Assert.AreEqual(token.Token.Unit.Id, root.Units.Selected, "the unit needing orders is selected");
        Assert.AreEqual(expectedTitle, ui.Q<Label>("info-title").text, "the visible card names that unit");
        Assert.AreEqual(date, bridge.State.Date, "blocked End turn did not advance time");
        Assert.AreEqual(needs, bridge.NeedsOrders.Count, "selection did not submit an order");
        // #1680: covering the selected unit again must reveal the NEXT one, not restart or select twice.
        var second = bridge.NeedsOrders[1];
        var secondToken = root.Units.Tokens.First(t => UnitCard.SimUnit(bridge.Content, bridge.State, t.Token) == second);
        UiTestKit.Press(ui.Q<Button>("nav-" + screen));
        yield return null;
        Assert.IsTrue(root.Info.Covered);
        UiTestKit.Press(ui.Q<Button>("end-turn"));
        for (int i = 0; i < 30; i++) yield return null;
        Assert.IsFalse(root.Info.Covered);
        Assert.AreEqual(secondToken.Token.Unit.Id, root.Units.Selected, "exactly one cycle step after revealing");
        Assert.AreEqual(date, bridge.State.Date);
        Assert.AreEqual(needs, bridge.NeedsOrders.Count);
        CollectionAssert.IsEmpty(bridge.Skipped.ToList(), "all drawn units remain in the cycle");
        UiTestKit.Press(ui.Q<Button>("info-skip"));
        yield return null;
        Assert.AreEqual(needs - 1, bridge.NeedsOrders.Count, "the revealed unit card is usable");
    }

}
