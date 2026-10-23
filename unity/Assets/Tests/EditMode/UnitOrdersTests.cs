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

/// <summary>GDD Phase 5 PR 15 (#754): the unit card's orders. Slice 1, Move: the card offers Move only when the sim's
/// Movement.Reachable has a hex for the unit; pressing it lights exactly those hexes on the map, a click on one queues the
/// sim's Move through SimBridge, and a click anywhere else ends the pick and selects as before. Slice 2, Fortify and Hold:
/// each shows only where the sim's Orders.Check takes it, else the sim's reason, and a press queues the sim's UnitOrder.
/// Slice 3, Attack: shown only where the sim's Combat.Check takes some hex; pressing it lights those hexes with
/// Combat.Preview's strength ratio and Combat.Outcomes' bands for each (Q-UI-1), and a click on one queues the attack.</summary>
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
        Assert.IsNull(UnitCard.SimUnit(C, s, Units.Single(u => u.Id == "yiftach")), "Yiftach has no starting force (its window does not cover the start, #1241)");
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

    /// <summary>Slice 2 (#754): for every unit with a sim unit, Israel's and the enemy's, the card shows Fortify and Hold each
    /// only where the sim's Orders.Check takes it, and where it does not, on Israel's units the sim's reason, whatever it is
    /// (so a refusal the sim adds later, such as artillery's hold, shows with no change here); an enemy's card gives no reason
    /// (the sim's "not yours" is in English and says nothing the card does not). A unit already holding has no Hold, with the
    /// sim's reason; once the game is over neither shows.</summary>
    [Test]
    public void TheCardOffersFortifyAndHoldOnlyWhereTheSimTakesThem()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var bridge = new SimBridge(g, Text);
        PlacedToken? shown = null;
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(), selectedUnit: () => shown);
        var buttons = new[] { (Orders.Fortify, ui.Q<Button>("info-fortify")), (Orders.Hold, ui.Q<Button>("info-hold")) };
        var tokens = UnitRoster.FromState(Units, C, g.State, _ => true, (row, a, q, r) => a).Where(t => UnitCard.SimUnit(C, g.State, t.Unit) != null).ToList();
        Assert.IsTrue(tokens.Any(t => t.Unit.Side == UnitData.Israel) && tokens.Any(t => t.Unit.Side != UnitData.Israel), "precondition: both sides");
        void Expect(UnitToken token, string at)
        {
            shown = new PlacedToken(token, new AnchorSpot(0, 0, 0, 0, false));
            info.ShowUnit();
            var id = UnitCard.SimUnit(C, bridge.State, token.Unit)!.Value;
            foreach (var (order, button) in buttons)
            {
                var why = Orders.Check(C, bridge.State, id, order);
                Assert.AreEqual(why == null ? DisplayStyle.Flex : DisplayStyle.None, button.style.display.value, $"{at} {token.Unit.Id}: {order} ({why})");
                if (why == null) continue;
                if (token.Unit.Side == UnitData.Israel) StringAssert.Contains(why, ui.Q<Label>("info-reason").text, $"{at} {token.Unit.Id}: the sim's reason");
                else StringAssert.DoesNotContain(why, ui.Q<Label>("info-reason").text, $"{at} {token.Unit.Id}: no reason on an enemy's card");
            }
        }
        foreach (var token in tokens) Expect(token, "start");
        Assert.AreEqual(("Fortify", "Hold"), (buttons[0].Item2.text, buttons[1].Item2.text), "Q-UI-2's approved names");
        var golani = tokens.Single(t => t.Unit.Id == "golani");
        var id0 = UnitCard.SimUnit(C, g.State, golani.Unit)!.Value;
        Assert.IsNull(bridge.Queue(new UnitOrder(id0, Orders.Hold)));
        bridge.EndTurn();
        Assert.IsNotNull(Orders.Check(C, bridge.State, id0, Orders.Hold), "precondition: Golani holds");
        Expect(golani, "holding");
        Assert.IsTrue(Shown(buttons[0].Item2), "a holding unit can still fortify");
        bridge.State.GameOver = GameOverCodes.MvpEnded;
        info.ShowUnit();
        Assert.IsFalse(buttons.Any(b => Shown(b.Item2)), "no orders once the game is over");
        info.Dispose();
    }

    /// <summary>Fortify queues the sim's UnitOrder(unit, fortify) through SimBridge and the card says when it is set; Hold
    /// then replaces it (one standing order per unit) and the card says so instead.</summary>
    [Test]
    public void FortifyAndHoldQueueTheSimsOrder()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var bridge = new SimBridge(g, Text);
        var golani = UnitRoster.FromState(Units, C, g.State, _ => true, (row, a, q, r) => a).Single(t => t.Unit.Id == "golani");
        PlacedToken? shown = new PlacedToken(golani, new AnchorSpot(0, 0, 0, 0, false));
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(), selectedUnit: () => shown);
        var id = UnitCard.SimUnit(C, g.State, golani.Unit)!.Value;
        info.ShowUnit();
        string Lines() => ui.Q<Label>("info-settlements").text;
        Assert.IsFalse(Lines().Contains(Text["unit.fortify_queued"]) || Lines().Contains(Text["unit.hold_queued"]), Lines());
        UiTestKit.Press(ui.Q<Button>("info-fortify"));
        var queued = bridge.Queued.OfType<UnitOrder>().Single();
        Assert.AreEqual((id, Orders.Fortify, (int?)null, (int?)null), (queued.Unit, queued.Order, queued.Q, queued.R));
        StringAssert.Contains(Text["unit.fortify_queued"], Lines());
        UiTestKit.Press(ui.Q<Button>("info-hold"));
        queued = bridge.Queued.OfType<UnitOrder>().Single();
        Assert.AreEqual((id, Orders.Hold), (queued.Unit, queued.Order));
        StringAssert.Contains(Text["unit.hold_queued"], Lines());
        Assert.IsFalse(Lines().Contains(Text["unit.fortify_queued"]), Lines());
        Assert.IsEmpty(bridge.EndTurn().Rejected, "the sim takes the order the card queued");
        Assert.AreEqual(Orders.Hold, bridge.State.Units[id].Order);
        info.Dispose();
    }

    /// <summary>R-UNIT-005 (#1309): Send home is absent until the shown Israeli unit's own front has signed its armistice,
    /// then carries the localized label, queues the sim's UnitOrder through the same bridge as the standing orders and shows
    /// its queued line under the card's lines.</summary>
    [Test]
    public void SendHomeAppearsAfterTheFrontsArmisticeAndQueuesTheSimsOrder()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        foreach (var (t, label) in new[] { (Text, "Send home"), (he, "שליחה הביתה") })
        {
            using var ui = new UiTestKit();
            var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            var bridge = new SimBridge(g, t);
            var tokens = UnitRoster.FromState(Units, C, g.State, _ => true, (row, a, q, r) => a);
            var token = tokens.Single(x => x.Unit.Id == "golani");
            var id = UnitCard.SimUnit(C, g.State, token.Unit)!.Value;
            var u = g.State.Units[id];
            var next = new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) }.Select(d => (q: u.Q + d.Item1, r: u.R + d.Item2))
                .First(h => C.HexAt(h.q, h.r)?.Land == true && !g.State.Units.Values.Any(x => (x.Q, x.R) == h));
            var egypt = g.State.Units.Values.First(x => x.Side == "egypt");
            g.State.Units[egypt.Id] = new Unit(egypt.Id, egypt.Type, next.q, next.r, egypt.Service, egypt.Side, egypt.Strength,
                egypt.Order, egypt.OrderTarget, egypt.HomeCity);
            PlacedToken? selected = new PlacedToken(token, new AnchorSpot(0, 0, 0, 0, false));
            var info = new InfoPanelPresenter(ui.Root, bridge, t, new Dictionary<string, string>(), selectedUnit: () => selected);
            var button = ui.Q<Button>("info-send-home");
            Assert.AreEqual("The armistice with Egypt is not signed", Demobilization.Check(C, g.State, id), "precondition");
            info.ShowUnit();
            Assert.AreEqual(DisplayStyle.None, button.style.display.value, "hidden before the relevant armistice");

            g.State.ChosenOptions["armistice_egypt"] = "sign";
            Assert.IsNull(Demobilization.Check(C, g.State, id), "precondition: the relevant armistice opens the order");
            info.ShowUnit();
            Assert.AreEqual(DisplayStyle.Flex, button.style.display.value, "shown after the relevant armistice");
            Assert.AreEqual(label, button.text);
            UiTestKit.Press(button);
            var queued = bridge.Queued.OfType<UnitOrder>().Single();
            Assert.AreEqual((id, Demobilization.SendHome, (int?)null, (int?)null), (queued.Unit, queued.Order, queued.Q, queued.R));
            StringAssert.Contains(t["unit.send_home_queued"], ui.Q<Label>("info-settlements").text, "the card says the order waits for End turn");
            info.Dispose();
        }
    }

    /// <summary>Gives the state the Arms for an attack and, when Golani has no enemy in reach (the sim's Combat.Check takes no
    /// hex), puts the first enemy sim unit on the first free land hex next to it; returns Golani's sim id.</summary>
    internal static int EnemyInGolanisReach(GameState s)
    {
        var id = C.StartingForces.ToList().FindIndex(f => f.Row == "golani");
        s.Arms = 1000;
        if (Targets(s, id).Count > 0) return id;
        var g = s.Units[id];
        var enemy = s.Units.Values.First(u => u.Side != "" && u.Side != Movement.PlayerSide);
        var at = new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) }.Select(d => (q: g.Q + d.Item1, r: g.R + d.Item2))
            .First(h => C.HexAt(h.q, h.r)?.Land == true && !s.Units.Values.Any(u => (u.Q, u.R) == h));
        s.Units[enemy.Id] = new Unit(enemy.Id, enemy.Type, at.q, at.r, enemy.Service, enemy.Side, enemy.Strength);
        return id;
    }

    /// <summary>The hexes the card offers for an attack: every unit's hex the sim's Combat.Check takes.</summary>
    static List<(int q, int r)> Targets(GameState s, int unit) =>
        s.Units.Values.Select(u => (u.Q, u.R)).Distinct().Where(h => Combat.Check(C, s, unit, h.Item1, h.Item2) == null).ToList();

    /// <summary>Slice 3 (#754): for every unit with a sim unit, Israel's and the enemy's, the card shows Attack only where
    /// the sim's Combat.Check takes some hex, and on Israel's units the sim's reason for each hex in reach it refuses (an
    /// enemy's card gives none, as for Fortify and Hold). With no Arms, Golani
    /// (an enemy in its reach) has no Attack and shows the sim's reason; once the game is over no card has one.</summary>
    [Test]
    public void TheCardOffersAttackOnlyWhereTheSimTakesAHex()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var bridge = new SimBridge(g, Text);
        var id = EnemyInGolanisReach(g.State);
        var tokens = UnitRoster.FromState(Units, C, g.State, _ => true, (row, a, q, r) => a).Where(t => UnitCard.SimUnit(C, g.State, t.Unit) != null).ToList();
        // An enemy with a card next to Golani, so an enemy's card has a hex in reach the sim refuses ("not yours").
        var foe = UnitCard.SimUnit(C, g.State, tokens.First(t => t.Unit.Side != UnitData.Israel).Unit)!.Value;
        var gq = g.State.Units[id].Q; var gr = g.State.Units[id].R;
        var next = new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) }.Select(d => (q: gq + d.Item1, r: gr + d.Item2))
            .First(h => C.HexAt(h.q, h.r)?.Land == true && !g.State.Units.Values.Any(u => (u.Q, u.R) == h));
        var f = g.State.Units[foe];
        g.State.Units[foe] = new Unit(f.Id, f.Type, next.q, next.r, f.Service, f.Side, f.Strength);
        Assert.IsNotNull(Combat.Preview(C, g.State, foe, gq, gr), "precondition: Golani is in an enemy card's reach");
        PlacedToken? shown = null;
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(), selectedUnit: () => shown);
        var attack = ui.Q<Button>("info-attack");
        var offered = new HashSet<bool>();
        foreach (var token in tokens)
        {
            shown = new PlacedToken(token, new AnchorSpot(0, 0, 0, 0, false));
            info.ShowUnit();
            var u = UnitCard.SimUnit(C, g.State, token.Unit)!.Value;
            var has = Targets(g.State, u).Count > 0;
            offered.Add(has);
            Assert.AreEqual(has ? DisplayStyle.Flex : DisplayStyle.None, attack.style.display.value, token.Unit.Id);
            foreach (var h in g.State.Units.Values.Select(x => (x.Q, x.R)).Distinct().Where(h => Combat.Preview(C, g.State, u, h.Item1, h.Item2) != null))
                if (Combat.Check(C, g.State, u, h.Item1, h.Item2) is string why)
                    if (token.Unit.Side == UnitData.Israel) StringAssert.Contains(why, ui.Q<Label>("info-reason").text, $"{token.Unit.Id}: the sim's reason");
                    else StringAssert.DoesNotContain(why, ui.Q<Label>("info-reason").text, $"{token.Unit.Id}: no reason on an enemy's card");
        }
        CollectionAssert.AreEquivalent(new[] { true, false }, offered, "precondition: some cards offer Attack and some do not");
        shown = new PlacedToken(tokens.Single(t => t.Unit.Id == "golani"), new AnchorSpot(0, 0, 0, 0, false));
        info.ShowUnit();
        Assert.AreEqual("Attack", attack.text, "Q-UI-2's approved name");
        var at = Targets(g.State, id)[0];
        g.State.Arms = 0;
        info.ShowUnit();
        var short_ = Combat.Check(C, g.State, id, at.q, at.r);
        Assert.IsNotNull(short_, "precondition: no Arms");
        Assert.AreEqual(DisplayStyle.None, attack.style.display.value, "no Arms for an attack");
        StringAssert.Contains(short_, ui.Q<Label>("info-reason").text, "the sim's reason");
        g.State.Arms = 1000;
        g.State.GameOver = GameOverCodes.MvpEnded;
        info.ShowUnit();
        Assert.AreEqual(DisplayStyle.None, attack.style.display.value, "no orders once the game is over");
        info.Dispose();
    }

    /// <summary>Attack lights exactly the hexes Combat.Check takes, and the card gives each its units' names with
    /// Combat.Preview's strength ratio and Combat.Outcomes' bands for its row (Q-UI-1). A click on an unlit hex queues
    /// nothing; a click on a lit one queues UnitOrder(unit, attack, q, r) through SimBridge and the card says the attack
    /// waits for End turn, with that hex's odds; the sim takes the queued attack at End turn.</summary>
    [Test]
    public void AttackShowsEachTargetsOddsAndAClickOnOneQueuesTheAttack()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            var id = EnemyInGolanisReach(bridge.State);
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            layer.Show(bridge.Content, bridge.State);
            var lit = new List<IReadOnlyCollection<(int q, int r)>>();
            var towns = SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName))).ToDictionary(t => t.Id, t => t.Name);
            var info = new InfoPanelPresenter(ui.Root, bridge, Text, towns,
                selectedUnit: () => layer.Selected is string sel ? layer.Tokens.First(p => p.Token.Unit.Id == sel) : (PlacedToken?)null,
                showReachable: hexes => { lit.Add(hexes.ToList()); map.ShowReachable(hexes); },
                unitName: u => "Unit-" + u);
            var golani = layer.Tokens.Single(p => p.Token.Unit.Id == "golani");
            var attack = ui.Q<Button>("info-attack");
            string Lines() => ui.Q<Label>("info-settlements").text;
            string Odds(int q, int r)
            {
                var p = Combat.Preview(C, bridge.State, id, q, r)!.Value;
                var bands = Combat.Outcomes(C, bridge.State, p.Row).Select(b => Text.Format(b.Retreat ? "unit.attack_band_retreat" : "unit.attack_band",
                    b.Faces, b.Of, b.AttackersLose, b.DefendersLose));
                var names = string.Join(", ", bridge.State.Units.Values.Where(u => (u.Q, u.R) == (q, r)).Where(u => u.Side != "" && u.Side != Movement.PlayerSide).Select(u => "Unit-" + u.Id));
                return Text.Format("unit.attack_odds", names, p.RatioPercent) + "\n" + string.Join("\n", bands);   // one band a line
            }
            GameRoot.OnHexClicked(map, layer, info, golani.Q, golani.R);
            UiTestKit.Press(attack);
            var targets = Targets(bridge.State, id);
            Assert.IsNotEmpty(targets, "precondition");
            CollectionAssert.AreEquivalent(targets, lit.Last(), "the lit hexes are the ones Combat.Check takes");
            StringAssert.Contains(Text["unit.attack_pick"], Lines());
            foreach (var (q, r) in targets) StringAssert.Contains(Odds(q, r), Lines());
            StringAssert.Contains("strength ratio", Lines());
            // An unlit hex: nothing queued, the pick ends.
            var tlv = C.SettlementHexes.Single(t => t.Id == "tel_aviv");
            Assert.IsFalse(targets.Contains((tlv.Q, tlv.R)), "precondition");
            GameRoot.OnHexClicked(map, layer, info, tlv.Q, tlv.R);
            Assert.IsEmpty(lit.Last());
            Assert.IsEmpty(bridge.Queued);
            // A lit hex: the sim's attack is queued and the card keeps the unit, with the order's line and its odds.
            GameRoot.OnHexClicked(map, layer, info, golani.Q, golani.R);
            UiTestKit.Press(attack);
            var at = targets[0];
            GameRoot.OnHexClicked(map, layer, info, at.q, at.r);
            var queued = bridge.Queued.OfType<UnitOrder>().Single();
            Assert.AreEqual((id, Combat.Attack, (int?)at.q, (int?)at.r), (queued.Unit, queued.Order, queued.Q, queued.R));
            Assert.IsEmpty(lit.Last());
            Assert.IsTrue(info.ShowsUnit);
            Assert.IsFalse(Lines().Contains(Text["unit.attack_pick"]), Lines());
            StringAssert.Contains(Text["unit.attack_queued"], Lines());
            StringAssert.Contains(Odds(at.q, at.r), Lines());
            Assert.IsEmpty(bridge.EndTurn().Rejected, "the sim takes the attack the card queued");
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

    /// <summary>The smoke-orders golden (slice 4): GameRoot.SmokeOrders hides turn 1's event card, gives GoldenRun.SmokeMove
    /// through the unit card (a click on the unit, Move, a click on the lit hex), ends the turn with End Turn, and ends on the state the sim's
    /// PlaySmokeOrders pins in the golden manifest.</summary>
    [Test]
    public void TheSmokeOrdersRunGivesItsMoveThroughTheCardAndEndsOnTheGolden()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            using var toast = new ToastPresenter(ui.Root, bridge, Text);
            using var hud = new HudPresenter(ui.Root, bridge, Text);   // wires End Turn
            using var card = new EventCardPresenter(ui.Root, bridge, Text, null);
            Assert.IsTrue(ui.Root.Q("root").ClassListContains(EventCardPresenter.OpenClass), "precondition: turn 1's card is open, as in the player");
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            layer.Show(bridge.Content, bridge.State);
            using var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(),
                selectedUnit: () => layer.Selected is string sel ? layer.Tokens.First(p => p.Token.Unit.Id == sel) : (PlacedToken?)null,
                showReachable: map.ShowReachable);
            var log = new List<string>();
            var run = GameRoot.SmokeOrders(ui.Root, bridge, layer, (q, r) => GameRoot.OnHexClicked(map, layer, info, q, r), log.Add);
            while (run.MoveNext()) { }
            var golden = GoldenRun.PlaySmokeOrders(C);
            Assert.AreEqual(System.FormattableString.Invariant($"SMOKE ORDERS OK {golden.Date:yyyy-MM-dd} {golden.Hash()}"), log.Last(), string.Join("\n", log));
            var move = (Move)bridge.LastBatch.Single();
            var expected = GoldenRun.SmokeMove(C, GameSession.New(C, GoldenRun.Seed, Mode.Historical).State);
            Assert.AreEqual((expected.Unit, expected.Q, expected.R), (move.Unit, move.Q, move.R), "the one order End Turn sent");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>A click that misses the lit hex queues no Move: the smoke-orders run fails and ends no turn.</summary>
    [Test]
    public void ASmokeOrdersRunWhoseMoveIsNotQueuedFails()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            using var hud = new HudPresenter(ui.Root, bridge, Text);
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            layer.Show(bridge.Content, bridge.State);
            using var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(),
                selectedUnit: () => layer.Selected is string sel ? layer.Tokens.First(p => p.Token.Unit.Id == sel) : (PlacedToken?)null);
            int clicks = 0, turn = bridge.State.Turn;
            var log = new List<string>();
            var run = GameRoot.SmokeOrders(ui.Root, bridge, layer, (q, r) => { if (clicks++ == 0) GameRoot.OnHexClicked(map, layer, info, q, r); }, log.Add);
            while (run.MoveNext()) { }
            StringAssert.StartsWith("SMOKE FAIL", log.Last(), string.Join("\n", log));
            Assert.IsFalse(log.Any(l => l.StartsWith("SMOKE ORDERS OK")));
            Assert.AreEqual(turn, bridge.State.Turn, "no turn ended");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>"smoke-orders" is its own whole flag: it starts the one-turn run and is not the "smoke" flag.</summary>
    [Test]
    public void SmokeOrdersIsItsOwnFlag()
    {
        var none = new string[0];
        Assert.IsTrue(GameRoot.SmokeOrdersRequested("http://localhost/?smoke-orders", none, true));
        Assert.IsTrue(GameRoot.SmokeOrdersRequested("http://localhost/?golden&smoke-orders", none, true));
        Assert.IsFalse(GameRoot.SmokeOrdersRequested("http://localhost/?smoke-orders-x", none, true));
        Assert.IsFalse(GameRoot.SmokeOrdersRequested("http://localhost/?smoke=5", none, true));
        Assert.AreEqual(0, GameRoot.SmokeTurns("http://localhost/?smoke-orders", none, true), "not the smoke flag");
        Assert.IsTrue(GameRoot.SmokeOrdersRequested(null, new[] { "Statecraft", "-smoke-orders" }, false));
        Assert.IsFalse(GameRoot.SmokeOrdersRequested("http://localhost/?smoke-orders", new[] { "Statecraft" }, false), "desktop reads only the command line");
    }
}
