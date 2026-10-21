#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>GDD Phase 5 PR 15, slice 1 (#754): the unit card's Move order. The card offers Move only when the sim's
/// Movement.Reachable has a hex for the unit; pressing it lights exactly those hexes on the map, a click on one queues the
/// sim's Move through SimBridge, and a click anywhere else ends the pick and selects as before.</summary>
public class UnitOrdersTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Units => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    static MapScene BuildMap()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        map.Build();
        return map;
    }

    static bool Shown(VisualElement e) => e.style.display.value != DisplayStyle.None;

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    /// <summary>The sim unit a row stands for is the starting force of that row (R-UNIT-006: unit i of a new game is
    /// StartingForces[i]); none for a row with no starting force, or once the unit has left the state.</summary>
    [Test]
    public void ARowsSimUnitIsItsStartingForce()
    {
        var s = GameSession.New(C, GoldenRun.Seed, Mode.Historical).State;
        var golani = Units.Single(u => u.Id == "golani");
        var id = C.StartingForces.ToList().FindIndex(f => f.Row == "golani");
        Assert.GreaterOrEqual(id, 0, "precondition: Golani is a starting force");
        Assert.AreEqual(id, UnitCard.SimUnit(C, s, golani));
        Assert.IsNull(UnitCard.SimUnit(C, s, Units.Single(u => u.Id == "etzioni")), "Etzioni has no starting force (#1205)");
        s.Units.Remove(id);
        Assert.IsNull(UnitCard.SimUnit(C, s, golani), "a unit no longer in the state");
        Assert.IsNull(UnitCard.SimUnit(SimBridgeGddTests.Legacy, s, golani), "the legacy loop has no starting forces");
    }

    /// <summary>The card shows Move only where Movement.Reachable has a hex: Golani's at the start; not an enemy force's
    /// (Reachable is empty for it), and not once the game is over.</summary>
    [Test]
    public void TheCardOffersMoveOnlyWhereTheSimHasAHex()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var bridge = new SimBridge(g, Text);
        var tokens = UnitRoster.FromState(Units, C, g.State, _ => true, (row, a, q, r) => a);
        PlacedToken? shown = null;
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(), selectedUnit: () => shown);
        var move = ui.Q<Button>("info-move");
        var golani = tokens.Single(t => t.Unit.Id == "golani");
        Assert.IsNotEmpty(Movement.Reachable(C, g.State, UnitCard.SimUnit(C, g.State, golani.Unit)!.Value), "precondition");
        shown = new PlacedToken(golani, new AnchorSpot(0, 0, 0, 0, false));
        info.ShowUnit();
        Assert.AreEqual(DisplayStyle.Flex, move.style.display.value, "Golani can move");
        Assert.AreEqual("Move", move.text, "Q-UI-2's approved name");
        var enemy = tokens.First(t => t.Unit.Side != UnitData.Israel && UnitCard.SimUnit(C, g.State, t.Unit) != null);
        Assert.IsEmpty(Movement.Reachable(C, g.State, UnitCard.SimUnit(C, g.State, enemy.Unit)!.Value), "precondition");
        shown = new PlacedToken(enemy, new AnchorSpot(0, 0, 0, 0, false));
        info.ShowUnit();
        Assert.AreEqual(DisplayStyle.None, move.style.display.value, "an enemy force has no Move");
        shown = new PlacedToken(golani, new AnchorSpot(0, 0, 0, 0, false));
        g.State.GameOver = GameOverCodes.MvpEnded;
        info.ShowUnit();
        Assert.AreEqual(DisplayStyle.None, move.style.display.value, "no orders once the game is over");
        info.Dispose();
    }

    /// <summary>Move lights exactly Movement.Reachable's hexes. A click on one queues Move(unit, q, r) through SimBridge and
    /// keeps the card, which says the move waits for End turn; a click on an unlit hex queues nothing and shows that hex;
    /// a second press of Move ends the pick. Each end of the pick clears the overlay.</summary>
    [Test]
    public void AClickOnALitHexQueuesTheMoveAndAnyOtherClickEndsThePick()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            layer.Show(bridge.Content, bridge.State);
            var lit = new List<IReadOnlyCollection<(int q, int r)>>();
            var towns = SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName))).ToDictionary(t => t.Id, t => t.Name);
            var info = new InfoPanelPresenter(ui.Root, bridge, Text, towns,
                selectedUnit: () => layer.Selected is string sel ? layer.Tokens.First(p => p.Token.Unit.Id == sel) : (PlacedToken?)null,
                showReachable: hexes => { lit.Add(hexes.ToList()); map.ShowReachable(hexes); });
            var golani = layer.Tokens.Single(p => p.Token.Unit.Id == "golani");
            var id = UnitCard.SimUnit(C, bridge.State, golani.Token.Unit)!.Value;
            var reach = Movement.Reachable(C, bridge.State, id).Keys.ToList();
            var move = ui.Q<Button>("info-move");
            GameRoot.OnHexClicked(map, layer, info, golani.Q, golani.R);
            Assert.IsTrue(info.ShowsUnit);
            UiTestKit.Press(move);
            CollectionAssert.AreEqual(reach, lit.Last(), "the overlay is Reachable's hexes, in its order");
            Assert.IsTrue(map.Terrain.transform.Find(MapScene.ReachableName).GetComponent<MeshRenderer>().enabled);
            Assert.IsTrue(ui.Q<Label>("info-settlements").text.Contains(Text["unit.move_pick"]), ui.Q<Label>("info-settlements").text);
            UiTestKit.Press(move);   // a second press ends the pick
            Assert.IsEmpty(lit.Last());
            Assert.IsFalse(map.Terrain.transform.Find(MapScene.ReachableName).GetComponent<MeshRenderer>().enabled);
            // An unlit hex: nothing queued, the pick ends and the click selects as before (the hex, as the unit is not on it).
            var tlv = C.SettlementHexes.Single(t => t.Id == "tel_aviv");
            Assert.IsFalse(reach.Contains((tlv.Q, tlv.R)), "precondition: Tel Aviv is out of Golani's reach");
            UiTestKit.Press(move);
            GameRoot.OnHexClicked(map, layer, info, tlv.Q, tlv.R);
            Assert.IsEmpty(lit.Last());
            Assert.IsEmpty(bridge.Queued);
            Assert.IsFalse(info.ShowsUnit);
            Assert.AreEqual((tlv.Q, tlv.R), info.Selected);
            // A lit hex: the sim's Move is queued, the unit stays selected and its card says the move waits for End turn.
            GameRoot.OnHexClicked(map, layer, info, golani.Q, golani.R);
            UiTestKit.Press(move);
            var (q, r) = reach.Last();
            map.Select(q, r);   // MapPointer outlines the hex before it raises HexClicked
            GameRoot.OnHexClicked(map, layer, info, q, r);
            var queued = bridge.Queued.OfType<Move>().Single();
            Assert.AreEqual((id, q, r), (queued.Unit, queued.Q, queued.R));
            Assert.IsEmpty(lit.Last());
            Assert.AreEqual("golani", layer.Selected);
            Assert.IsTrue(info.ShowsUnit);
            Assert.IsFalse(map.Terrain.transform.Find(MapScene.SelectionName).GetComponent<MeshRenderer>().enabled, "one highlight: the ring");
            Assert.IsTrue(ui.Q<Label>("info-settlements").text.Contains(Text["unit.move_queued"]), ui.Q<Label>("info-settlements").text);
            // A refused order (here the game ends while the pick is open): nothing more is queued and the pick line goes.
            bridge.Withdraw(queued);
            UiTestKit.Press(move);
            bridge.State.GameOver = GameOverCodes.MvpEnded;
            GameRoot.OnHexClicked(map, layer, info, q, r);
            Assert.IsEmpty(bridge.Queued, "the bridge refuses an order once the game is over");
            Assert.IsEmpty(lit.Last());
            Assert.IsFalse(ui.Q<Label>("info-settlements").text.Contains(Text["unit.move_pick"]), ui.Q<Label>("info-settlements").text);
            bridge.State.GameOver = null;
            // A turn ends the pick (its hexes were the last turn's), as does another selection.
            UiTestKit.Press(move);
            Assert.IsNotEmpty(lit.Last());
            bridge.EndTurn();
            Assert.IsEmpty(lit.Last(), "End turn ends the pick");
            Assert.IsFalse(ui.Q<Label>("info-settlements").text.Contains(Text["unit.move_pick"]));
            Assert.IsTrue(info.ShowsUnit && Shown(move), "precondition: Golani's card still offers Move on the next turn");
            UiTestKit.Press(move);
            Assert.IsNotEmpty(lit.Last());
            info.Clear();
            Assert.IsEmpty(lit.Last(), "another selection ends the pick");
            info.Dispose();
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The overlay draws one band per hex given (two rings of MapBuilder.RingVertices each) and hides when given
    /// none, or only hexes off the map.</summary>
    [Test]
    public void TheReachableOverlayHasOneBandPerHex()
    {
        var map = BuildMap();
        try
        {
            var hexes = C.Hexes.Where(h => h.Land).Take(3).Select(h => (h.Q, h.R)).ToList();
            map.ShowReachable(hexes);
            var go = map.Terrain.transform.Find(MapScene.ReachableName);
            Assert.IsTrue(go.GetComponent<MeshRenderer>().enabled);
            Assert.AreEqual(3 * 2 * MapBuilder.RingVertices, go.GetComponent<MeshFilter>().sharedMesh.vertexCount);
            map.ShowReachable(new[] { (9999, 9999) });
            Assert.IsFalse(go.GetComponent<MeshRenderer>().enabled, "off the map: nothing to light");
            map.ShowReachable(hexes);
            map.ShowReachable(new (int, int)[0]);
            Assert.IsFalse(go.GetComponent<MeshRenderer>().enabled);
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }
}
