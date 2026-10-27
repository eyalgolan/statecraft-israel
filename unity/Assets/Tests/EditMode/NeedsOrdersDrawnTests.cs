#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Owner, v0.3.0 RC (25 October 2026): "the game is saying "a unit needs orders" but clicking on it does nothing and
/// i cant find any unit that needs orders". A unit that moved next to its own units found no clear spot within a hex, its
/// token was left out (UnitPlacer.PlaceAll), and End turn, which selects the units that need orders, could not select it, so
/// the turn could not end. Every unit is now drawn (UnitPlacer.OnOwnHex), and End turn never does nothing.</summary>
public class NeedsOrdersDrawnTests
{
    RenderTexture? rt; PanelSettings? ps; string dir = "";
    Action? unsubscribe;

    [TearDown]
    public void DiscardScene()
    {
        unsubscribe?.Invoke(); unsubscribe = null;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (dir != "" && Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    IEnumerator Boot(Action<VisualElement, GameRoot> booted)
    {
        dir = Path.Combine(Path.GetTempPath(), "statecraft-needs-orders-" + Guid.NewGuid().ToString("N"));
        VisualElement ui = null!; GameRoot root = null!;
        yield return CivMovementShots.Boot(false, dir, (t, p, u, g) => { rt = t; ps = p; ui = u; root = g; });
        booted(ui, root);
    }

    static HashSet<int> Drawn(SimBridge bridge, UnitLayer layer) =>
        layer.Tokens.Select(t => UnitCard.SimUnit(bridge.Content, bridge.State, t.Token)).Where(x => x != null).Select(x => x!.Value).ToHashSet();

    /// <summary>Eight turns of a new game, each unit that needs orders moved as far as it can toward its own units (mode
    /// 0: on e262d289 a unit had no token on six of the eight turns) or simply as far as it can (mode 1: from turn 1).
    /// Every unit that needs orders has a token, and End turn selects one of them.</summary>
    [UnityTest]
    public IEnumerator EveryUnitThatNeedsOrdersIsDrawnAndEndTurnSelectsIt([Values(0, 1)] int mode)
    {
        VisualElement ui = null!; GameRoot root = null!;
        yield return Boot((u, g) => { ui = u; root = g; });
        var bridge = root.Bridge!; var c = bridge.Content;
        var layer = Object.FindFirstObjectByType<UnitLayer>();
        for (int turn = 0; turn < 8; turn++)
        {
            var drawn = Drawn(bridge, layer);
            CollectionAssert.IsEmpty(bridge.NeedsOrders.Where(id => !drawn.Contains(id)).ToList(), $"turn {bridge.State.Turn}: units that need orders have no token");
            CollectionAssert.IsEmpty(bridge.State.Units.Values.Where(x => x.Side == Movement.PlayerSide && !drawn.Contains(x.Id)).Select(x => x.Id).ToList(),
                $"turn {bridge.State.Turn}: every player unit is drawn");
            layer.Select(null);
            UiTestKit.Press(ui.Q<Button>("end-turn"));
            yield return null;
            Assert.IsNotNull(layer.Selected, $"turn {bridge.State.Turn}: End turn selects a unit");
            var selected = layer.Tokens.First(t => t.Token.Unit.Id == layer.Selected).Token;
            CollectionAssert.Contains(bridge.NeedsOrders.ToList(), UnitCard.SimUnit(c, bridge.State, selected), "the unit End turn selects needs orders");
            CollectionAssert.IsEmpty(bridge.Skipped.ToList(), "nothing was skipped for want of a token");
            foreach (var id in bridge.NeedsOrders.ToList())
            {
                var reach = Movement.Reachable(c, bridge.State, id, bridge.Spent);
                if (reach.Count == 0) continue;
                var others = bridge.State.Units.Values.Where(x => x.Side == Movement.PlayerSide && x.Id != id).ToList();
                var pick = mode == 0
                    ? reach.Keys.OrderBy(h => others.Count == 0 ? 0 : others.Min(o => MapActions.Distance(o.Q, o.R, h.Q, h.R))).ThenBy(h => h).First()
                    : reach.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First().Key;
                bridge.Queue(new Move(id, pick.Q, pick.R));
                yield return null;
            }
            foreach (var id in bridge.NeedsOrders.ToList()) bridge.Skip(id);
            int before = bridge.State.Turn;
            bridge.EndTurn();
            yield return null;
            Assert.AreEqual(before + 1, bridge.State.Turn, "the turn ended");
            if (ui.Q("root").ClassListContains(TurnReportPresenter.OpenClass)) UiTestKit.Press(ui.Q<Button>("turn-report-close"));
            yield return null;
        }
    }

    /// <summary>Draws the layer without <paramref name="hidden"/>'s units after every bridge change (after GameRoot's own
    /// redraw, which subscribed first), so they have no token to select, as on the RC.</summary>
    void HideTokens(SimBridge bridge, UnitLayer layer, Func<IEnumerable<int>> hidden)
    {
        Action hide = () =>
        {
            var s = bridge.State.Clone();
            foreach (var id in hidden().ToList()) s.Units.Remove(id);
            layer.Show(bridge.Content, s);
        };
        bridge.Changed += hide;
        unsubscribe = () => bridge.Changed -= hide;
        hide();
    }

    /// <summary>The safety net: a unit that needs orders and has no token is skipped with a log line and a toast, and End
    /// turn selects the next one; on e262d289 End turn did nothing at all.</summary>
    [UnityTest]
    public IEnumerator EndTurnSkipsAUnitWithNoTokenAndSelectsTheNext([Values(false, true)] bool covered)
    {
        VisualElement ui = null!; GameRoot root = null!;
        yield return Boot((u, g) => { ui = u; root = g; });
        var bridge = root.Bridge!;
        var layer = Object.FindFirstObjectByType<UnitLayer>();
        var lost = bridge.NeedsOrders[0]; var next = bridge.NeedsOrders[1];
        HideTokens(bridge, layer, () => new[] { lost });
        layer.Select(null);
        if (covered)
        {
            UiTestKit.Press(ui.Q<Button>("nav-defence"));
            yield return null;
            Assert.IsTrue(root.Info!.Covered, "precondition: Defence covers the unit card");
        }
        LogAssert.Expect(LogType.Log, $"END TURN unit {lost} needs orders but has no token to select; skipped this turn");
        UiTestKit.Press(ui.Q<Button>("end-turn"));
        yield return null;
        Assert.IsTrue(bridge.IsSkipped(lost), "the unit with no token is skipped");
        Assert.IsNotNull(layer.Selected, "End turn selects the next unit");
        Assert.AreEqual(next, UnitCard.SimUnit(bridge.Content, bridge.State, layer.Tokens.First(t => t.Token.Unit.Id == layer.Selected).Token));
        if (covered)
        {
            for (int i = 0; i < 30; i++) yield return null;
            Assert.IsFalse(root.Info!.Covered, "the safety net still reveals the next unit");
            Assert.IsTrue(ui.Q("info-panel").enabledInHierarchy);
            Assert.IsFalse(ui.Q("root").ClassListContains(ScreenNav.ScreenOpenClass));
        }
        StringAssert.Contains("skips this turn", ui.Q<Label>("toast").text, "the player is told");
    }

    /// <summary>No loop: with no unit drawn, one End turn tries each unit once and skips them all, and the next ends the turn.</summary>
    [UnityTest]
    public IEnumerator EndTurnWithNoUnitDrawnSkipsEachOnceThenEndsTheTurn()
    {
        VisualElement ui = null!; GameRoot root = null!;
        yield return Boot((u, g) => { ui = u; root = g; });
        var bridge = root.Bridge!;
        var layer = Object.FindFirstObjectByType<UnitLayer>();
        var needs = bridge.NeedsOrders.ToList();
        Assert.Greater(needs.Count, 1, "precondition: several units need orders");
        HideTokens(bridge, layer, () => bridge.State.Units.Values.Where(x => x.Side == Movement.PlayerSide).Select(x => x.Id));
        foreach (var id in needs) LogAssert.Expect(LogType.Log, $"END TURN unit {id} needs orders but has no token to select; skipped this turn");
        UiTestKit.Press(ui.Q<Button>("end-turn"));
        yield return null;
        CollectionAssert.IsEmpty(bridge.NeedsOrders.ToList(), "each unit was tried once and skipped");
        Assert.IsNull(layer.Selected);
        int before = bridge.State.Turn;
        UiTestKit.Press(ui.Q<Button>("end-turn"));
        yield return null;
        Assert.AreEqual(before + 1, bridge.State.Turn, "End turn then ends the turn");
    }

    /// <summary>UnitPlacer.OnOwnHex: on the unit's own hex, compact, its ring off every drawn spot while the hex has room,
    /// the clear test preferred, and the same spot for the same inputs.</summary>
    [Test]
    public void OnOwnHexKeepsOffTheDrawnSpotsOnTheUnitsOwnHex()
    {
        const double hex = 5000, gap = 1.5 * 0.66 * 0.5 * hex;   // UnitLayer's gap: 1.5 compact ring radii
        var used = new List<(double x, double z)> { (0, 0) };
        var spots = new List<AnchorSpot>();
        for (int i = 0; i < 3; i++)
        {
            var s = UnitPlacer.OnOwnHex(4, 7, (0, 0), hex, used, gap, (x, z) => true);
            Assert.AreEqual((4, 7), (s.Q, s.R)); Assert.IsTrue(s.Compact);
            Assert.GreaterOrEqual(used.Min(u => Math.Sqrt((u.x - s.X) * (u.x - s.X) + (u.z - s.Z) * (u.z - s.Z))), gap);
            used.Add((s.X, s.Z)); spots.Add(s);
        }
        Assert.AreEqual(spots[0], UnitPlacer.OnOwnHex(4, 7, (0, 0), hex, new[] { (0.0, 0.0) }, gap, (x, z) => true), "deterministic");
        var clearOnlyLast = UnitPlacer.OnOwnHex(4, 7, (0, 0), hex, new[] { (0.0, 0.0) }, gap, (x, z) => x < -1000);
        Assert.Less(clearOnlyLast.X, -1000, "a clear spot is preferred");
        var full = UnitPlacer.OnOwnHex(4, 7, (0, 0), hex, new[] { (0.0, 0.0) }, 1e9, (x, z) => false);
        Assert.AreEqual((4, 7), (full.Q, full.R), "a full hex still gives a spot");
    }
}
