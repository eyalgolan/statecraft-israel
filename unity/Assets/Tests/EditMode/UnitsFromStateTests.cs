using System;
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

/// <summary>GDD Phase 5 PR 14 (#753; impact plan §2.6): in the GDD loop the map's army tokens come from the sim's units
/// (GameState.Units), one per unit on its sim hex, and a token's figures follow Q-UI-3, not the call-up. The squadron and
/// the ships stay tokens drawn from their rows (Q-UNIT-5).</summary>
public class UnitsFromStateTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Units => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static GameState NewGame() => Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>A moved unit's anchor as the layer makes it: the hex's centre, made once per row and hex.</summary>
    sealed class Moved
    {
        public readonly Dictionary<(string, int, int), UnitAnchor> Made = new Dictionary<(string, int, int), UnitAnchor>();
        public UnitAnchor At(UnitEntry row, UnitAnchor start, int q, int r)
        {
            if (!Made.TryGetValue((row.Id, q, r), out var a))
            {
                var h = C.HexAt(q, r)!;
                Made[(row.Id, q, r)] = a = new UnitAnchor { Lon = h.Lon, Lat = h.Lat, From = start.From, To = start.To };
            }
            return a;
        }
    }

    static List<UnitToken> Army(List<UnitToken> tokens) => tokens.Where(t => t.Unit.Service == UnitData.Army).ToList();

    /// <summary>A land hex next to (q, r) that holds no unit.</summary>
    static (int q, int r) FreeNeighbour(GameState s, int q, int r)
    {
        foreach (var (dq, dr) in new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) })
        {
            var h = C.HexAt(q + dq, r + dr);
            if (h != null && h.Land && !s.Units.Values.Any(u => u.Q == h.Q && u.R == h.R)) return (h.Q, h.R);
        }
        throw new InvalidOperationException($"no free land hex next to ({q},{r})");
    }

    static void MoveTo(GameState s, int id, int q, int r)
    {
        var u = s.Units[id];
        s.Units[id] = new Unit(u.Id, u.Type, q, r, u.Service, u.Side, u.Strength, u.Order, u.OrderTarget);
    }

    [Test]
    public void OneTokenPerUnitOnItsHex()
    {
        Assert.IsTrue(C.IsGdd, "the real content runs the GDD loop");
        var s = NewGame();
        var units = Units;
        var moved = new Moved();
        var drawn = s.Units.Values.Where(u => u.Side != "" && UnitRoster.OnMap(units.Single(e => e.Id == C.StartingForces[u.Id].Row))).ToList();
        Assert.Greater(drawn.Count, 0);
        var army = Army(UnitRoster.FromState(units, C, s, _ => true, moved.At));
        CollectionAssert.AreEquivalent(drawn.Select(u => C.StartingForces[u.Id].Row), army.Select(t => t.Unit.Id), "one token per drawn unit");
        Assert.AreEqual(army.Count, army.Select(t => t.Unit.Id).Distinct().Count());
        var hexes = MapBuilder.LoadHexes(Path.Combine(Paths.Content, "map", "hexes.json"));
        foreach (var u in drawn)
        {
            var t = army.Single(x => x.Unit.Id == C.StartingForces[u.Id].Row);
            Assert.AreEqual((u.Q, u.R), HexPicker.FromLonLat(t.Anchor.Lon, t.Anchor.Lat, hexes.bounds, hexes.hex_size_km), $"{t}: the sim hex");
            Assert.AreEqual(C.StartingForces[u.Id].Anchor, t.Anchor.Place, $"{t}: unmoved, it stands on its starting anchor");
        }
        Assert.IsEmpty(moved.Made, "no unit moved, so no moved anchor");
        // Grouped per hex: the army tokens come in (q, r) order.
        var order = army.Select(t => drawn.Single(u => C.StartingForces[u.Id].Row == t.Unit.Id)).Select(u => (u.Q, u.R)).ToList();
        CollectionAssert.AreEqual(order.OrderBy(h => h.Q).ThenBy(h => h.R).ToList(), order);

        // A unit that moves is drawn on its new hex, once, and a halutz draws nothing.
        var first = drawn[0];
        var to = FreeNeighbour(s, first.Q, first.R);
        MoveTo(s, first.Id, to.q, to.r);
        s.Units.Add(s.NextId, new Unit(s.NextId, "halutz", first.Q, first.R)); s.NextId++;
        army = Army(UnitRoster.FromState(units, C, s, _ => true, moved.At));
        Assert.AreEqual(drawn.Count, army.Count, "still one token per unit; the halutz has none");
        var mt = army.Single(t => t.Unit.Id == C.StartingForces[first.Id].Row);
        Assert.AreSame(moved.Made.Values.Single(), mt.Anchor, "the moved unit takes its moved anchor");
        Assert.AreEqual(to, HexPicker.FromLonLat(mt.Anchor.Lon, mt.Anchor.Lat, hexes.bounds, hexes.hex_size_km));
        Assert.AreEqual("", mt.Anchor.Place, "no source names the hex it moved to");
        // The squadron and the ships are still the rows' tokens.
        var late = NewGame(); late.Date = new DateTime(1948, 6, 1);
        var rows = UnitRoster.Build(units, C, late, _ => true).Where(t => t.Unit.Service != UnitData.Army).ToList();
        Assert.IsNotEmpty(rows);
        CollectionAssert.AreEqual(rows, UnitRoster.FromState(units, C, late, _ => true, moved.At).Where(t => t.Unit.Service != UnitData.Army).ToList());
    }

    [TestCase(6, 6, 3)]
    [TestCase(4, 6, 3)]
    [TestCase(3, 6, 2)]
    [TestCase(2, 6, 2)]
    [TestCase(1, 6, 1)]
    [TestCase(1, 1, 3)]
    [TestCase(1, 2, 2)]
    [TestCase(1, 4, 1)]
    public void FiguresFollowTheAnswerAtEveryStrength(int strength, int full, int figures) =>
        Assert.AreEqual(figures, UnitRoster.FiguresForStrength(strength, full), "Q-UI-3: 3 at two thirds or more, 2 at a third or more, 1 below");

    /// <summary>Q-UI-3 on the map: a new game draws every unit at full strength with three figures whatever the call-up,
    /// and a unit that lost strength shows it.</summary>
    [Test]
    public void FiguresFollowTheAnswer()
    {
        var s = NewGame();
        s.Mobilization = 0; s.Rung = 1;   // the call-up no longer sets the figures (legacy: 1 figure here)
        var units = Units;
        var moved = new Moved();
        var army = Army(UnitRoster.FromState(units, C, s, _ => true, moved.At));
        Assert.IsNotEmpty(army);
        foreach (var t in army) Assert.AreEqual(3, t.Figures, $"{t}: full strength");
        var big = s.Units.Values.Where(u => u.Side == "israel").OrderByDescending(u => u.Strength).ThenBy(u => u.Id).First();
        Assert.AreEqual(4, big.Strength, "the largest brigades start at 4 units (docs/units.md, the starting forces; #1240)");
        var row = C.StartingForces[big.Id].Row;
        foreach (var (strength, figures) in new[] { (4, 3), (3, 3), (2, 2), (1, 1) })
        {
            var u = s.Units[big.Id];
            s.Units[big.Id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, strength, u.Order, u.OrderTarget);
            var t = Army(UnitRoster.FromState(units, C, s, _ => true, moved.At)).Single(x => x.Unit.Id == row);
            Assert.AreEqual(figures, t.Figures, $"{row} at {strength} of 4");
            Assert.AreEqual(figures, t.Headgear.Count, "one headgear per figure");
        }
    }

    /// <summary>A unit that moved stands on a hex no source names: its card gives no position and the row's sources.</summary>
    [Test]
    public void AMovedUnitsCardNamesNoPlaceAndGivesTheRowsSources()
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        var s = NewGame();
        var u = s.Units.Values.First(x => x.Side == "israel");
        var to = FreeNeighbour(s, u.Q, u.R);
        MoveTo(s, u.Id, to.q, to.r);
        var row = C.StartingForces[u.Id].Row;
        var t = Army(UnitRoster.FromState(Units, C, s, _ => true, new Moved().At)).Single(x => x.Unit.Id == row);
        var card = UnitCard.For(new PlacedToken(t, default), s, text);
        var start = UnitRoster.AnchorAt(t.Unit, C.Rules.Start)!;
        var position = string.IsNullOrWhiteSpace(start.Since) ? text.Format("unit.position_plain", start.Place) : text.Format("unit.position", start.Place, start.Since!);
        var home = UnitCard.For(new PlacedToken(new UnitToken(t.Unit, start, 3, t.ArtType, false), default), s, text);
        CollectionAssert.Contains(home.Body.Split('\n'), position, "on its starting anchor the card names the place");
        CollectionAssert.DoesNotContain(card.Body.Split('\n'), position, "no position line for a hex no source names");
        foreach (var host in t.Unit.AllSources().Select(x => new Uri(x.Url).Host)) StringAssert.Contains(host, card.Footer);
        Assert.AreEqual(UnitData.IsWikipediaOnly(t.Unit) ? text["unit.draft_wikipedia"] : text["unit.draft"], card.Note);
    }

    static Unit Raise(GameState s, string type = "recruit", int strength = 2)
    {
        var like = s.Units.Values.First(x => x.Side == UnitData.Israel);
        var to = FreeNeighbour(s, like.Q, like.R);
        var unit = new Unit(s.NextId++, type, to.q, to.r, side: UnitData.Israel, strength: strength, homeCity: "tel_aviv");
        s.Units.Add(unit.Id, unit);
        return unit;
    }

    /// <summary>#1396: each military type raised after the starting forces gets an army token on its sim hex. Its reusable
    /// type default supplies the existing figures and vehicle, and Q-UI-3 supplies the figure count from strength.</summary>
    [TestCase("recruit", 1, 1, null)]
    [TestCase("veteran_soldier", 4, 3, null)]
    [TestCase("artillery", 2, 2, UnitFigures.Napoleonchik)]
    [TestCase("armoured_car", 4, 3, UnitFigures.SandwichArmouredCar)]
    public void ARaisedUnitDrawsItsTypeAndStrengthOnItsHex(string type, int strength, int figures, string vehicle)
    {
        var s = NewGame();
        var raised = Raise(s, type, strength);
        var tokens = UnitRoster.FromState(Units, C, s, _ => true, new Moved().At);
        var token = tokens.Single(t => t.SimId == raised.Id);
        Assert.AreEqual(UnitData.Army, token.Unit.Service);
        Assert.AreEqual(figures, token.Figures, "Q-UI-3 against the reused four-strength Israeli army default");
        Assert.AreEqual(figures, token.Headgear.Count, "one existing figure per strength band");
        if (vehicle == null) CollectionAssert.IsEmpty(token.Vehicles); else CollectionAssert.Contains(token.Vehicles, vehicle);
        var hexes = MapBuilder.LoadHexes(Path.Combine(Paths.Content, "map", "hexes.json"));
        Assert.AreEqual((raised.Q, raised.R), HexPicker.FromLonLat(token.Anchor.Lon, token.Anchor.Lat, hexes.bounds, hexes.hex_size_km));
    }

    /// <summary>#1396: the raised token has its own stable map id, so the same UnitLayer path as a starting unit selects it.</summary>
    [Test]
    public void ARaisedUnitCanBeSelected()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        try
        {
            map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
            Statecraft.Game.Map.Kits.KitPopulation.Use(Statecraft.Game.Map.Kits.KitPopulation.Parse(
                File.ReadAllText(Path.Combine(Paths.Content, "map", Statecraft.Game.Map.Kits.KitPopulation.FileName))));
            map.Build();
            var s = NewGame();
            var raised = Raise(s);
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            Assert.IsTrue(layer.Show(C, s));
            var token = layer.Tokens.Single(t => t.Token.SimId == raised.Id);
            Assert.AreEqual(token.Token.Unit.Id, layer.UnitToSelectAt(token.Q, token.R));
            layer.Select(token.Token.Unit.Id);
            Assert.AreEqual(token.Token.Unit.Id, layer.Selected);
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>#1396: a raised formation's card names its type, strength and home city, and routes Move to its real sim id.</summary>
    [Test]
    public void ARaisedUnitShowsItsCardAndMove()
    {
        using var ui = new UiTestKit();
        var game = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var raised = Raise(game.State);
        var bridge = new SimBridge(game, Text);
        var token = UnitRoster.FromState(Units, C, game.State, _ => true, new Moved().At).Single(t => t.SimId == raised.Id);
        Assert.IsNotEmpty(Movement.Reachable(C, game.State, raised.Id), "precondition: the raised recruits can move");
        PlacedToken? selected = new PlacedToken(token, new AnchorSpot(raised.Q, raised.R, 0, 0, false));
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, C.SettlementNames, selectedUnit: () => selected);
        info.ShowUnit();
        Assert.AreEqual(Text["city.track_recruit"], ui.Q<Label>("info-title").text);
        Assert.AreEqual(Text["unit.service.army"], ui.Q<Label>("info-subtitle").text);
        Assert.AreEqual(Text.Format("unit.strength", raised.Strength) + "\n" + Text.Format("unit.home_city", C.SettlementNames[raised.HomeCity]),
            ui.Q<Label>("info-settlements").text, "strength and home city, and no empty sector line");
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<Button>("info-move").style.display.value);
        Assert.AreEqual(Text["unit.order.move"], ui.Q<Button>("info-move").text);
        info.Dispose();
    }

    /// <summary>#1396 is additive: giving a raised unit a default token does not change any starting unit's row, art or anchor.</summary>
    [Test]
    public void StartingUnitsAreUnchangedWhenARaisedUnitIsDrawn()
    {
        var s = NewGame();
        var moved = new Moved();
        var before = UnitRoster.FromState(Units, C, s, _ => true, moved.At);
        var raised = Raise(s);
        var after = UnitRoster.FromState(Units, C, s, _ => true, moved.At);
        CollectionAssert.AreEqual(before, after.Where(t => t.SimId != raised.Id).ToList());
        Assert.AreEqual(before.Count + 1, after.Count);
    }

    /// <summary>Review point 3 on #1205: in the GDD loop the call-up no longer sets the figures, so the card's footer does not
    /// lead with it. An army token's footer gives only its sources; the squadron's keeps the security level, which still
    /// sets whether it flies. The legacy loop keeps the call-up line.</summary>
    [Test]
    public void TheGddCardDoesNotLeadWithTheCallUp()
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        var s = NewGame(); s.Date = new DateTime(1948, 7, 1);   // the squadron at Herzliya
        var tokens = UnitRoster.FromState(Units, C, s, _ => true, new Moved().At);
        var army = tokens.First(t => t.Unit.Service == UnitData.Army && t.Unit.Side == UnitData.Israel);
        var air = tokens.First(t => t.Unit.Service == UnitData.Air);
        var national = text.Format("unit.national", PlayerText.Percent(s.Mobilization), s.Rung);
        string First(UnitToken t, bool gdd) => UnitCard.For(new PlacedToken(t, default), s, text, gdd).Footer.Split('\n')[0];
        StringAssert.StartsWith(text.Format("unit.sources", ""), First(army, true), "an army token: sources only");
        Assert.AreEqual(text.Format("unit.security", s.Rung), First(air, true), "the squadron: the security level alone");
        Assert.AreEqual(national, First(army, false), "the legacy loop still leads with the call-up");
        Assert.AreEqual(national, First(air, false));
    }

    /// <summary>#563's spot cache: a rebuild after a move places only the unit that moved; every other token keeps its spot,
    /// and a unit back on its starting hex stands on its starting spot again.</summary>
    [Test]
    public void ARebuildAfterAMoveKeepsTheSpotCache()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        try
        {
            map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
            Statecraft.Game.Map.Kits.KitPopulation.Use(Statecraft.Game.Map.Kits.KitPopulation.Parse(
                File.ReadAllText(Path.Combine(Paths.Content, "map", Statecraft.Game.Map.Kits.KitPopulation.FileName))));
            map.Build();
            var s = NewGame();
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            Assert.IsTrue(layer.Show(C, s));
            Assert.AreEqual(0, layer.MovedSpotsPlaced);
            var start = layer.Tokens.ToDictionary(p => p.Token.Unit.Id, p => p.Spot);
            var israel = s.Units.Values.Where(u => u.Side == "israel").Take(2).ToArray();
            string a = C.StartingForces[israel[0].Id].Row, b = C.StartingForces[israel[1].Id].Row;
            var home = (israel[0].Q, israel[0].R);

            var to = FreeNeighbour(s, israel[0].Q, israel[0].R);
            MoveTo(s, israel[0].Id, to.q, to.r);
            Assert.IsTrue(layer.Show(C, s));
            Assert.AreEqual(1, layer.MovedSpotsPlaced, "only the moved unit is placed");
            var spotA = layer.Tokens.Single(p => p.Token.Unit.Id == a).Spot;
            Assert.LessOrEqual(MapActions.Distance(spotA.Q, spotA.R, to.q, to.r), UnitPlacer.MaxShift, "within one hex of its sim hex");
            foreach (var p in layer.Tokens.Where(p => p.Token.Unit.Id != a)) Assert.AreEqual(start[p.Token.Unit.Id], p.Spot, $"{p}: kept its spot");
            Assert.IsFalse(layer.Tokens.Any(p => p.Token.Unit.Id != a && p.Q == spotA.Q && p.R == spotA.R), "off the other tokens' hexes");

            var toB = FreeNeighbour(s, israel[1].Q, israel[1].R);
            MoveTo(s, israel[1].Id, toB.q, toB.r);
            Assert.IsTrue(layer.Show(C, s));
            Assert.AreEqual(2, layer.MovedSpotsPlaced, "the second move places only the second unit");
            Assert.AreEqual(spotA, layer.Tokens.Single(p => p.Token.Unit.Id == a).Spot, "the first keeps its cached spot");
            Assert.IsFalse(layer.Show(C, s), "nothing changed, nothing rebuilt");

            MoveTo(s, israel[0].Id, home.Item1, home.Item2);
            Assert.IsTrue(layer.Show(C, s));
            Assert.AreEqual(2, layer.MovedSpotsPlaced, "back home is the starting spot, not a new placement");
            Assert.AreEqual(start[a], layer.Tokens.Single(p => p.Token.Unit.Id == a).Spot);
            Assert.AreNotEqual(start[b], layer.Tokens.Single(p => p.Token.Unit.Id == b).Spot, "the second still stands where it moved");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");
}
