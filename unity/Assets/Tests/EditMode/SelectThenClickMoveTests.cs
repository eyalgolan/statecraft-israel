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
using UnityEngine.InputSystem;

/// <summary>The owner, playing on 25 October 2026: "i still can't move a fucking unit", "the unit must move when you click on
/// the fucking lit hex" and "not wait for the end of the fucking turn"; asked how, "Civ: select, then click (Recommended)".
/// Selecting a unit that can move lights its reach at once (spec 2.1, "Selecting a unit lights its one-turn reach"), with no
/// press of Move: a left click (or a tap) on a lit hex moves it now, on a farther hex gives a go-to, on another unit's hex
/// selects that unit, and on a hex it cannot reach selects that hex as before. A short right click gives the left click's
/// order (even on another unit's hex), and nothing else; a right drag stays the camera's pan.</summary>
public class SelectThenClickMoveTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Units => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    internal sealed class Rig : System.IDisposable
    {
        public readonly MapScene Map;
        public readonly UiTestKit Ui;
        public readonly SimBridge Bridge;
        public readonly UnitLayer Layer;
        public readonly InfoPanelPresenter Info;
        public readonly List<IReadOnlyCollection<(int q, int r)>> Lit = new List<IReadOnlyCollection<(int q, int r)>>();

        public Rig(UiText? text = null)
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Map = new GameObject("MapScene").AddComponent<MapScene>();
            Map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
            Map.Build();
            Ui = new UiTestKit();   // after the new scene, which would take the UI document with it
            Bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text ?? Text);
            Layer = UnitLayer.Create(Map, Units, C.Hexes, still: true);
            Layer.Show(Bridge.Content, Bridge.State);
            var layer = Layer; var map = Map; var lit = Lit;
            Info = new InfoPanelPresenter(Ui.Root, Bridge, text ?? Text, C.SettlementNames,
                selectedUnit: () => layer.Selected is string sel ? layer.Tokens.First(p => p.Token.Unit.Id == sel) : (PlacedToken?)null,
                showReachable: hexes => { lit.Add(hexes.ToList()); map.ShowReachable(hexes); }, showRoute: map.ShowRoute,
                clearSelection: () => { layer.Select(null); map.ClearSelection(); });
        }

        public PlacedToken Token(string row) => Layer.Tokens.Single(p => p.Token.Unit.Id == row);
        public int Id(PlacedToken t) => UnitCard.SimUnit(C, Bridge.State, t.Token.Unit)!.Value;
        public List<(int q, int r)> Reach(int id) => Movement.Reachable(C, Bridge.State, id, Bridge.Spent).Keys.Select(h => (h.Q, h.R)).ToList();
        public (int q, int r) At(int id) => (Bridge.State.Units[id].Q, Bridge.State.Units[id].R);
        public void Click(int q, int r, bool touch = false) => GameRoot.OnHexClicked(Map, Layer, Info, q, r, touch);
        public void Select(PlacedToken t) => Click(t.Q, t.R);

        /// <summary>#1661: another Israeli unit moved onto a city hex Golani has a route to (no game start has one there).
        /// Hands back Golani's token and the city hex; the friend's Move is committed. The friend's token is drawn near the
        /// city, where UnitPlacer finds it a spot, not necessarily on it.</summary>
        public (PlacedToken mover, (int q, int r) city) FriendOnACity()
        {
            var golani = Id(Token("golani"));
            foreach (var t in Layer.Tokens.Where(t => t.Token.Unit.Side == UnitData.Israel && t.Token.Unit.Id != "golani").ToList())
            {
                if (UnitCard.SimUnit(C, Bridge.State, t.Token.Unit) is not int x) continue;
                var city = Reach(x).Cast<(int q, int r)?>().FirstOrDefault(h => Info.IsCity(h!.Value.q, h.Value.r)
                    && Layer.UnitToSelectAt(h.Value.q, h.Value.r) == null
                    && Movement.Route(C, Bridge.State, golani, h.Value.q, h.Value.r, Bridge.Spent).Count > 0);
                if (city is not (int q, int r)) continue;
                Assert.IsNull(Bridge.Queue(new Move(x, q, r)), "precondition: the friend moves onto the city");
                Layer.Show(C, Bridge.State);
                return (Token("golani"), (q, r));
            }
            throw new AssertionException("no Israeli unit can reach a city Golani has a route to");
        }

        public void Dispose()
        {
            Info.Dispose();
            Ui.Dispose();
            Object.DestroyImmediate(Map.gameObject);
        }

        static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");
    }

    /// <summary>A click on the unit lights Movement.Reachable's hexes at once, with no press of Move, and the card says to
    /// choose one; a click on a lit hex then moves the unit at once (no End turn), and its remaining reach lights again.</summary>
    [Test]
    public void SelectingAUnitLightsItsReachAndALeftClickOnALitHexMovesItAtOnce()
    {
        using var rig = new Rig();
        var golani = rig.Token("golani"); var id = rig.Id(golani);
        var reach = rig.Reach(id);
        Assert.IsNotEmpty(reach, "precondition: Golani can move");
        rig.Select(golani);
        Assert.IsTrue(rig.Info.ShowsUnit);
        CollectionAssert.AreEqual(reach, rig.Lit.Last(), "the reach lights on selection, no Move press");
        Assert.IsTrue(rig.Info.MovePicking);
        StringAssert.Contains(Text["unit.move_pick"], rig.Ui.Q<Label>("info-settlements").text);
        var turn = rig.Bridge.State.Turn;
        var to = reach.First(h => rig.Layer.UnitToSelectAt(h.q, h.r) == null);
        rig.Click(to.q, to.r);
        Assert.AreEqual(to, rig.At(id), "the unit moved at once");
        Assert.AreEqual(turn, rig.Bridge.State.Turn, "no End turn");
        Assert.AreEqual("golani", rig.Layer.Selected, "the unit stays selected");
        if (Movement.PointsLeft(C, rig.Bridge.State, id, rig.Bridge.Spent) > 0 && rig.Reach(id).Count > 0)
            CollectionAssert.AreEquivalent(rig.Reach(id), rig.Lit.Last(), "the points left light again");
    }

    /// <summary>A left click on an unlit land hex with a route gives the go-to, with no press of Move.</summary>
    [Test]
    public void ALeftClickOnAFarHexGivesAGoTo()
    {
        using var rig = new Rig();
        var golani = rig.Token("golani"); var id = rig.Id(golani);
        var lit = rig.Reach(id).ToHashSet();
        var far = C.Hexes.Where(h => h.Land && !lit.Contains((h.Q, h.R)) && (h.Q, h.R) != (golani.Q, golani.R))
            .Select(h => (h.Q, h.R)).First(h => rig.Layer.UnitToSelectAt(h.Q, h.R) == null
                && Movement.Route(C, rig.Bridge.State, id, h.Q, h.R, new Dictionary<int, int>()).LastOrDefault().Turn == 2);
        rig.Select(golani);
        rig.Click(far.Q, far.R);
        Assert.AreEqual((Movement.GoTo, $"{far.Q},{far.R}"), (rig.Bridge.State.Units[id].Order, rig.Bridge.State.Units[id].OrderTarget));
    }

    /// <summary>A left click on a hex the unit cannot reach (the sea: no move, no route) gives no order and selects that hex,
    /// as before; the lit hexes go away.</summary>
    [Test]
    public void ALeftClickOnAnUnreachableHexSelectsThatHex()
    {
        using var rig = new Rig();
        var golani = rig.Token("golani");
        var sea = C.Hexes.First(h => !h.Land);
        rig.Select(golani);
        rig.Click(sea.Q, sea.R);
        Assert.IsEmpty(rig.Bridge.Session.Committed);
        Assert.IsEmpty(rig.Bridge.Queued);
        Assert.IsFalse(rig.Info.ShowsUnit);
        Assert.AreEqual((sea.Q, sea.R), rig.Info.Selected);
        Assert.IsEmpty(rig.Lit.Last());
    }

    /// <summary>A unit of Israel's that stands on another one's lit hex: a left click there selects it (Civ) and gives no
    /// order; a right click there moves the first unit onto it.</summary>
    [Test]
    public void ALeftClickOnAnotherUnitSelectsItAndARightClickMovesOntoIt()
    {
        using var rig = new Rig();
        var own = rig.Layer.Tokens.Where(t => t.Token.Unit.Side == UnitData.Israel && UnitCard.SimUnit(C, rig.Bridge.State, t.Token.Unit) != null).ToList();
        var pair = (from a in own from b in own
                    where a.Token.Unit.Id != b.Token.Unit.Id && (a.Q, a.R) != (b.Q, b.R)
                        && rig.Layer.At(b.Q, b.R)?.Token.Unit.Id == b.Token.Unit.Id && rig.Reach(rig.Id(a)).Contains((b.Q, b.R))
                    select (a, b)).FirstOrDefault();
        Assert.IsNotNull(pair.a, "precondition: one unit of Israel's has another's hex in its reach");
        var (mover, other) = pair;
        var id = rig.Id(mover);
        rig.Select(mover);
        rig.Click(other.Q, other.R);
        Assert.IsEmpty(rig.Bridge.Session.Committed, "a left click on another unit gives no order");
        Assert.AreEqual(other.Token.Unit.Id, rig.Layer.Selected, "it selects that unit");
        rig.Select(mover);
        GameRoot.OnHexOrdered(rig.Map, rig.Info, other.Q, other.R);
        Assert.AreEqual((other.Q, other.R), rig.At(id), "a right click moves onto it");
        Assert.AreEqual(mover.Token.Unit.Id, rig.Layer.Selected);
    }

    /// <summary>An enemy in Golani's reach (UnitOrdersTests.EnemyInGolanisReach): with Golani's Move pick open from the
    /// selection, neither a left nor a right click on the enemy's hex orders a move or a go-to into it (Attack is its own
    /// pick, from its button).</summary>
    [Test]
    public void NoClickOnAnEnemyOrdersAMoveIntoIt()
    {
        using var rig = new Rig();
        var s = rig.Bridge.State;
        var id = UnitOrdersTests.EnemyInGolanisReach(s);
        rig.Layer.Show(rig.Bridge.Content, s);
        var enemy = s.Units.Values.Where(u => u.Side != "" && u.Side != Movement.PlayerSide).Select(u => (u.Q, u.R))
            .First(h => Combat.Check(C, s, id, h.Q, h.R) == null);
        var golani = rig.Token("golani");
        var from = rig.At(id);
        rig.Select(golani);
        Assert.IsTrue(rig.Info.MovePicking, "precondition: the Move pick is open");
        rig.Click(enemy.Q, enemy.R);
        Assert.IsEmpty(rig.Bridge.Session.Committed, "a left click on the enemy orders nothing");
        Assert.AreEqual(from, rig.At(id));
        Assert.AreNotEqual(Movement.GoTo, rig.Bridge.State.Units[id].Order);
        rig.Select(golani);
        GameRoot.OnHexOrdered(rig.Map, rig.Info, enemy.Q, enemy.R);
        Assert.IsEmpty(rig.Bridge.Session.Committed, "a right click on the enemy orders nothing");
        Assert.AreEqual(from, rig.At(id));
    }

    /// <summary>A short right click on a lit hex moves the shown unit, as a left click would; with no unit shown, or on a
    /// hex the unit cannot reach, it does nothing: the card, its lit hexes and the selection stay.</summary>
    [Test]
    public void ARightClickMovesTheShownUnitAndOtherwiseDoesNothing()
    {
        using var rig = new Rig();
        var golani = rig.Token("golani"); var id = rig.Id(golani);
        var reach = rig.Reach(id);
        var to = reach.First(h => rig.Layer.UnitToSelectAt(h.q, h.r) == null);
        GameRoot.OnHexOrdered(rig.Map, rig.Info, to.q, to.r);
        Assert.IsEmpty(rig.Bridge.Session.Committed, "no unit shown: nothing");
        Assert.IsNull(rig.Layer.Selected);
        rig.Select(golani);
        var sea = C.Hexes.First(h => !h.Land);
        GameRoot.OnHexOrdered(rig.Map, rig.Info, sea.Q, sea.R);
        Assert.IsEmpty(rig.Bridge.Session.Committed, "an unreachable hex: nothing");
        Assert.IsTrue(rig.Info.ShowsUnit, "the card stays");
        CollectionAssert.AreEqual(reach, rig.Lit.Last(), "and its lit hexes");
        GameRoot.OnHexOrdered(rig.Map, rig.Info, to.q, to.r);
        Assert.AreEqual(to, rig.At(id), "a right click on a lit hex moves the unit at once");
        Assert.AreEqual("golani", rig.Layer.Selected);
    }

    /// <summary>On a touchscreen (no right button) a tap on a lit hex after selecting moves the unit, as a left click does.</summary>
    [Test]
    public void ATapOnALitHexMovesTheUnit()
    {
        using var rig = new Rig();
        var golani = rig.Token("golani"); var id = rig.Id(golani);
        var to = rig.Reach(id).First(h => rig.Layer.UnitToSelectAt(h.q, h.r) == null);
        rig.Click(golani.Q, golani.R, touch: true);
        rig.Click(to.q, to.r, touch: true);
        Assert.AreEqual(to, rig.At(id));
    }

    /// <summary>#1650: Escape clears the selected unit and its lit hexes, and orders nothing.</summary>
    [Test]
    public void EscapeClearsTheUnitAndItsReachWithoutAnOrder()
    {
        using var rig = new Rig();
        rig.Select(rig.Token("golani"));
        Assert.IsNotEmpty(rig.Lit.Last(), "precondition");
        Assert.IsTrue(rig.Info.Escape());
        Assert.IsFalse(rig.Info.ShowsUnit);
        Assert.IsNull(rig.Layer.Selected);
        Assert.IsEmpty(rig.Lit.Last());
        Assert.IsEmpty(rig.Bridge.Session.Committed);
    }

    /// <summary>#210: under a screen covering the panel, neither a left nor a right click on a lit hex orders the unit.</summary>
    [Test]
    public void UnderACoveringScreenNoClickMovesTheUnit()
    {
        using var rig = new Rig();
        var golani = rig.Token("golani"); var id = rig.Id(golani);
        var to = rig.Reach(id).First(h => rig.Layer.UnitToSelectAt(h.q, h.r) == null);
        rig.Select(golani);
        var hud = rig.Ui.Q<VisualElement>("info-panel").parent;
        hud.AddToClassList(ScreenNav.WideOpenClass);
        rig.Click(to.q, to.r);
        GameRoot.OnHexOrdered(rig.Map, rig.Info, to.q, to.r);
        Assert.IsEmpty(rig.Bridge.Session.Committed);
        Assert.AreEqual("golani", rig.Layer.Selected);
        hud.RemoveFromClassList(ScreenNav.WideOpenClass);
        rig.Click(to.q, to.r);
        Assert.AreEqual(to, rig.At(id), "uncovered, the same click moves it");
    }

    /// <summary>A second press of Move, or of Attack, never leaves a unit that can move with no lit reach; nor does a new
    /// turn (the turn's pick is over, the next one lights).</summary>
    [Test]
    public void AUnitThatCanMoveKeepsItsReachLit()
    {
        using var rig = new Rig();
        var golani = rig.Token("golani"); var id = rig.Id(golani);
        rig.Select(golani);
        UiTestKit.Press(rig.Ui.Q<Button>("info-move"));
        Assert.IsTrue(rig.Info.MovePicking, "Move keeps its pick");
        CollectionAssert.AreEqual(rig.Reach(id), rig.Lit.Last());
        rig.Bridge.EndTurn();
        Assert.IsTrue(rig.Bridge.State.Units.ContainsKey(id) && rig.Reach(id).Count > 0, "precondition: Golani can move on the next turn");
        Assert.IsTrue(rig.Info.ShowsUnit);
        Assert.IsTrue(rig.Info.MovePicking, "the new turn lights its reach");
        CollectionAssert.AreEquivalent(rig.Reach(id), rig.Lit.Last());
    }

    /// <summary>Owner, 25 Oct 2026: "Clicking another unit or a city still selects it." With Golani's Move pick open, a
    /// left click on a city's hex (Tel Aviv: out of reach this turn, with a go-to route) shows the city and orders nothing; a
    /// right click there gives the go-to.</summary>
    [Test]
    public void ALeftClickOnACityShowsItAndARightClickSendsTheUnitThere()
    {
        using var rig = new Rig();
        var golani = rig.Token("golani"); var id = rig.Id(golani);
        var tlv = C.SettlementHexes.First(x => x.Id == "tel_aviv" && rig.Layer.UnitToSelectAt(x.Q, x.R) == null);
        Assert.IsNotEmpty(Movement.Route(C, rig.Bridge.State, id, tlv.Q, tlv.R, rig.Bridge.Spent), "precondition: a go-to route reaches Tel Aviv");
        rig.Select(golani);
        Assert.IsTrue(rig.Info.MovePicking, "precondition: the Move pick is open");
        rig.Click(tlv.Q, tlv.R);
        Assert.IsEmpty(rig.Bridge.Session.Committed, "a left click on a city orders nothing");
        Assert.IsFalse(rig.Info.ShowsUnit);
        Assert.AreEqual((tlv.Q, tlv.R), rig.Info.Selected, "it shows the city's hex");
        rig.Select(golani);
        GameRoot.OnHexOrdered(rig.Map, rig.Info, tlv.Q, tlv.R);
        Assert.AreEqual((Movement.GoTo, $"{tlv.Q},{tlv.R}"), (rig.Bridge.State.Units[id].Order, rig.Bridge.State.Units[id].OrderTarget), "a right click sends the unit there");
    }

    /// <summary>A unit of Israel's with no reach left (Golani, its points spent by moves this turn) opens no pick: a left
    /// click on land selects that hex and a right click does nothing.</summary>
    [Test]
    public void AUnitWithNoReachOpensNoPick()
    {
        using var rig = new Rig();
        var id = rig.Id(rig.Token("golani"));
        for (var reach = rig.Reach(id); reach.Count > 0; reach = rig.Reach(id))
            Assert.IsNull(rig.Bridge.Queue(new Move(id, reach.Last().q, reach.Last().r)), "precondition: the move is taken");
        rig.Layer.Show(rig.Bridge.Content, rig.Bridge.State);
        var still = rig.Token("golani");
        Assert.AreEqual(rig.At(id), (still.Q, still.R), "precondition: the token stands where Golani moved");
        var given = rig.Bridge.Session.Committed.Count;   // the moves above are committed at once
        var land = C.Hexes.First(h => h.Land && rig.Layer.At(h.Q, h.R) == null && !rig.Info.IsCity(h.Q, h.R) && (h.Q, h.R) != (still.Q, still.R));
        rig.Layer.Select(null);
        rig.Select(still);
        Assert.AreEqual("golani", rig.Layer.Selected, "precondition: Golani's card");
        Assert.IsFalse(rig.Info.MovePicking, "no reach: no pick");
        GameRoot.OnHexOrdered(rig.Map, rig.Info, land.Q, land.R);
        Assert.AreEqual(given, rig.Bridge.Session.Committed.Count, "a right click orders nothing");
        Assert.IsTrue(rig.Info.ShowsUnit, "and changes nothing");
        rig.Click(land.Q, land.R);
        Assert.AreEqual(given, rig.Bridge.Session.Committed.Count, "a left click orders nothing");
        Assert.AreEqual((land.Q, land.R), rig.Info.Selected, "a left click selects the hex");
    }

    /// <summary>The Attack pick is the button's, as before: pressing Attack lights the enemy, a right click on it gives the
    /// attack (as a left click does), and pressing Attack again goes back to the Move pick.</summary>
    [Test]
    public void ARightClickOnAnAttackPicksTargetAttacksAndAttackAgainGoesBackToMove()
    {
        using var rig = new Rig();
        var s = rig.Bridge.State;
        var id = UnitOrdersTests.EnemyInGolanisReach(s);
        rig.Layer.Show(rig.Bridge.Content, s);
        var enemy = s.Units.Values.Where(u => u.Side != "" && u.Side != Movement.PlayerSide).Select(u => (u.Q, u.R))
            .First(h => Combat.Check(C, s, id, h.Q, h.R) == null);
        rig.Select(rig.Token("golani"));
        var attack = rig.Ui.Q<Button>("info-attack");
        UiTestKit.Press(attack);
        UiTestKit.Press(attack);
        Assert.IsTrue(rig.Info.MovePicking, "Attack pressed again goes back to the Move pick");
        UiTestKit.Press(attack);
        Assert.IsFalse(rig.Info.MovePicking, "precondition: the Attack pick is open");
        GameRoot.OnHexOrdered(rig.Map, rig.Info, enemy.Q, enemy.R);
        var given = rig.Bridge.Session.Committed.OfType<UnitOrder>().Single();
        Assert.AreEqual((id, Combat.Attack), (given.Unit, given.Order), "the right click gave the attack");
    }
    // #1661 owner rulings 1 and 2 (the confirm step). Red on main: no offer exists there.
    [TestCase(false)]
    [TestCase(true)]
    public void AFriendlyDestinationOffersTheOriginalMoverExactlyOnce(bool touch)
    {
        using var rig = new Rig();
        var own = rig.Layer.Tokens.Where(t => t.Token.Unit.Side == UnitData.Israel
            && UnitCard.SimUnit(C, rig.Bridge.State, t.Token.Unit) != null).ToList();
        var pair = (from a in own from b in own
                    where a.Token.Unit.Id != b.Token.Unit.Id && (a.Q, a.R) != (b.Q, b.R)
                        && rig.Layer.UnitToSelectAt(b.Q, b.R) == b.Token.Unit.Id
                        && rig.Reach(rig.Id(a)).Contains((b.Q, b.R))
                    select (a, b)).First();
        var id = rig.Id(pair.a); var other = rig.Id(pair.b);
        // The tapped hex is where B's token is drawn (its anchor), which can differ from B's sim hex.
        var from = rig.At(id); (int q, int r) target = (pair.b.Q, pair.b.R); var otherAt = rig.At(other);
        rig.Select(pair.a);
        rig.Click(target.q, target.r, touch);
        Assert.IsEmpty(rig.Bridge.Session.Committed, "selection itself gives no order");
        Assert.AreEqual(pair.b.Token.Unit.Id, rig.Layer.Selected, "ordinary selection stays");
        var send = rig.Ui.Root.Q<Button>("info-confirm-move");
        Assert.IsNotNull(send, "a visible confirmation is offered");
        Assert.AreEqual(DisplayStyle.Flex, send.style.display.value);
        StringAssert.Contains(UnitCard.For(pair.a, rig.Bridge.State, Text, C.IsGdd, C).Title, send.text);
        StringAssert.Contains(HexPlaces.Of(string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{0},{1}", target.q, target.r), C, Text, null), send.text);
        UiTestKit.Press(send);
        Assert.AreEqual(target, rig.At(id), "the retained mover A moves, not selected B");
        Assert.AreEqual(otherAt, rig.At(other), "B stays where it was");
        Assert.AreEqual(1, rig.Bridge.Session.Committed.Count);
        Assert.Greater(rig.Bridge.Spent[id], 0);
        UiTestKit.Press(send);
        Assert.AreEqual(1, rig.Bridge.Session.Committed.Count, "a second activation cannot repeat the move");
        Assert.AreNotEqual(from, rig.At(id));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ACityOffersAConfirmedGoToWithoutAnExtraTouchTap(bool touch)
    {
        using var rig = new Rig();
        var mover = rig.Token("golani"); var id = rig.Id(mover);
        var city = C.SettlementHexes.First(x => x.Id == "tel_aviv" && rig.Layer.UnitToSelectAt(x.Q, x.R) == null);
        Assert.IsNotEmpty(Movement.Route(C, rig.Bridge.State, id, city.Q, city.R, rig.Bridge.Spent));
        rig.Select(mover);
        rig.Click(city.Q, city.R, touch);
        Assert.IsFalse(rig.Info.ShowsUnit);
        Assert.IsEmpty(rig.Bridge.Session.Committed);
        var send = rig.Ui.Root.Q<Button>("info-confirm-move");
        Assert.IsNotNull(send);
        Assert.AreEqual(DisplayStyle.Flex, send.style.display.value);
        UiTestKit.Press(send);
        Assert.AreEqual(1, rig.Bridge.Session.Committed.Count);
        Assert.AreEqual(Movement.GoTo, rig.Bridge.State.Units[id].Order);
        Assert.AreEqual(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}", city.Q, city.R),
            rig.Bridge.State.Units[id].OrderTarget);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AnOccupiedCityPillStillShowsTheCityAndOffersTheOriginalMover(bool pill)
    {
        using var rig = new Rig();
        var (mover, city) = rig.FriendOnACity();
        var baseline = rig.Bridge.Session.Committed.Count;
        var id = rig.Id(mover);
        var near = rig.Reach(id).Contains(city);
        rig.Select(mover);
        var unitFirst = rig.Layer.UnitToSelectAt(city.q, city.r);
        GameRoot.OnHexClicked(rig.Map, rig.Layer, rig.Info, city.q, city.r, true, pill);
        Assert.AreEqual(baseline, rig.Bridge.Session.Committed.Count, "the pick itself gives no order");
        if (pill) Assert.AreEqual(((int, int)?)city, rig.Info.Selected, "a pill means the city");
        else Assert.AreEqual(unitFirst, rig.Layer.Selected, "a badge/terrain or keyboard pick keeps unit-first routing");
        var send = rig.Ui.Root.Q<Button>("info-confirm-move");
        Assert.IsNotNull(send);
        Assert.AreEqual(DisplayStyle.Flex, send.style.display.value, "a city a friend holds offers the retained mover");
        UiTestKit.Press(send);
        if (near) Assert.AreEqual(city, rig.At(id), "a city in reach: the Move");
        else Assert.AreEqual(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}", city.q, city.r),
            rig.Bridge.State.Units[id].OrderTarget, "a city beyond reach: the go-to");
        Assert.AreEqual(baseline + 1, rig.Bridge.Session.Committed.Count);
    }

    [TestCase("en-US", "en")]
    [TestCase("he-IL", "he")]
    public void ConfirmationUsesLocalizedNamesAndTextUnderEitherCulture(string culture, string language)
    {
        var before = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);
            var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/UI/strings." + language + ".yaml").text);
            using var rig = new Rig(text);
            var mover = rig.Token("golani");
            var city = C.SettlementHexes.First(x => x.Id == "tel_aviv" && rig.Layer.UnitToSelectAt(x.Q, x.R) == null);
            rig.Select(mover); rig.Click(city.Q, city.R, touch: true);
            var send = rig.Ui.Root.Q<Button>("info-confirm-move");
            Assert.IsNotNull(send);
            Assert.AreEqual(DisplayStyle.Flex, send.style.display.value);
            var place = HexPlaces.Of(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1}", city.Q, city.R), C, text, null);
            Assert.AreEqual(text.Format("unit.confirm_move", UnitCard.For(mover, rig.Bridge.State, text, C.IsGdd, C).Title, place), send.text);
            Assert.IsEmpty(rig.Bridge.Session.Committed);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = before; }
    }

    [TestCase("selection")]
    [TestCase("turn")]
    [TestCase("screen")]
    [TestCase("escape")]
    [TestCase("session")]
    [TestCase("modal")]
    public void AnOccupiedDestinationConfirmationIsDiscardedWhenStale(string change)
    {
        using var rig = new Rig();
        var mover = rig.Token("golani");
        var city = C.SettlementHexes.First(x => x.Id == "tel_aviv" && rig.Layer.UnitToSelectAt(x.Q, x.R) == null);
        rig.Select(mover); rig.Click(city.Q, city.R, touch: true);
        var send = rig.Ui.Root.Q<Button>("info-confirm-move");
        Assert.IsNotNull(send);
        Assert.AreEqual(DisplayStyle.Flex, send.style.display.value);
        if (change == "selection") rig.Select(mover);
        if (change == "turn") rig.Bridge.EndTurn();
        if (change == "escape") rig.Info.Escape();
        if (change == "session") rig.Bridge.Replace(GameSession.New(C, GoldenRun.Seed, Mode.Historical));
        if (change == "modal")
        {
            var root = rig.Ui.Root.Q("root");
            root.AddToClassList(TurnReportPresenter.OpenClass); ScreenNav.DisableCovered(rig.Ui.Root);
            root.RemoveFromClassList(TurnReportPresenter.OpenClass); ScreenNav.DisableCovered(rig.Ui.Root);
        }
        if (change == "screen")
        {
            // Even a narrow screen that leaves the panel enabled invalidates the proposal.
            using var nav = new ScreenNav(rig.Ui.Root);
            nav.Open("why");
            Assert.IsTrue(rig.Ui.Q<VisualElement>("info-panel").enabledSelf, "narrow screen precondition");
            nav.Open(null);
        }
        var committed = rig.Bridge.Session.Committed.Count;
        Assert.AreEqual(DisplayStyle.None, send.style.display.value);
        UiTestKit.Press(send);
        Assert.AreEqual(committed, rig.Bridge.Session.Committed.Count);
    }

    [Test]
    public void EmptyFarTerrainStillRequiresTwoTouchPicksAndOffersNoFriendlyConfirmation()
    {
        using var rig = new Rig();
        var c = rig.Bridge.Content;
        var mover = rig.Token("golani"); var id = rig.Id(mover);
        var reach = rig.Reach(id).ToHashSet();
        var target = c.Hexes.First(h => h.Land && !reach.Contains((h.Q, h.R))
            && rig.Layer.UnitToSelectAt(h.Q, h.R) == null && !rig.Info.IsCity(h.Q, h.R)
            && Movement.Route(c, rig.Bridge.State, id, h.Q, h.R, rig.Bridge.Spent).Count > 0);
        rig.Select(mover);
        rig.Click(target.Q, target.R, touch: true);
        Assert.IsEmpty(rig.Bridge.Session.Committed, "first tap previews the existing far route");
        Assert.AreEqual(DisplayStyle.None, rig.Ui.Q<Button>("info-confirm-move").style.display.value);
        rig.Click(target.Q, target.R, touch: true);
        Assert.AreEqual(1, rig.Bridge.Session.Committed.Count, "second tap confirms the existing far route");
        Assert.AreEqual(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}", target.Q, target.R),
            rig.Bridge.State.Units[id].OrderTarget);
    }

    [TestCase("full-friendly")]
    [TestCase("no-points")]
    [TestCase("send-home")]
    [TestCase("game-over")]
    [TestCase("sea")]
    [TestCase("hostile")]
    [TestCase("covered")]
    public void RefusedDestinationsOfferNoConfirmationAndPreserveOrdinarySelection(string reason)
    {
        using var rig = new Rig();
        var c = rig.Bridge.Content; var s = rig.Bridge.State;
        var mover = rig.Token("golani"); var id = rig.Id(mover);
        var city = c.SettlementHexes.First(x => x.Id == "tel_aviv" && rig.Layer.UnitToSelectAt(x.Q, x.R) == null);
        (int q, int r) target = (city.Q, city.R);
        if (reason == "full-friendly")
        {
            var u = s.Units[id];
            var limit = c.RulesAt(s.Date).Mvp!.StackLimit!.Value;
            for (int i = 0; i < limit; i++)
            {
                int clone = s.Units.Keys.Max() + 1;
                s.Units[clone] = new Unit(clone, u.Type, target.q, target.r, u.Service, u.Side,
                    u.Strength, "", "", u.HomeCity, u.Full);
            }
            Assert.IsNotEmpty(Movement.Route(c, s, id, target.q, target.r, rig.Bridge.Spent), "route alone permits a full endpoint");
        }
        if (reason == "no-points")
        {
            for (var reach = rig.Reach(id); reach.Count > 0; reach = rig.Reach(id))
                Assert.IsNull(rig.Bridge.Queue(new Move(id, reach.Last().q, reach.Last().r)));
            Assert.AreEqual(0, Movement.PointsLeft(c, rig.Bridge.State, id, rig.Bridge.Spent));
        }
        if (reason == "send-home")
        {
            var u = s.Units[id];
            s.Units[id] = new Unit(u.Id, u.Type, city.Q, city.R, u.Service, u.Side,
                u.Strength, u.Order, u.OrderTarget, u.HomeCity, u.Full);
            Assert.IsNull(rig.Bridge.Queue(new UnitOrder(id, Demobilization.SendHome)), "precondition: home-city Send home");
            Assert.IsTrue(rig.Bridge.GoingHome(id));
            var otherCity = c.SettlementHexes.First(x => (x.Q, x.R) != (city.Q, city.R)
                && Movement.Route(c, rig.Bridge.State, id, x.Q, x.R, rig.Bridge.Spent).Count > 0);
            target = (otherCity.Q, otherCity.R);
        }
        if (reason == "game-over") s.GameOver = GameOverCodes.MvpEnded;
        if (reason == "sea") { var sea = c.Hexes.First(h => !h.Land); target = (sea.Q, sea.R); }
        if (reason == "hostile")
        {
            UnitOrdersTests.EnemyInGolanisReach(s);
            var enemy = s.Units.Values.First(u => u.Side != "" && u.Side != Movement.PlayerSide
                && Combat.Check(c, s, id, u.Q, u.R) == null);
            target = (enemy.Q, enemy.R);
        }
        rig.Layer.Show(c, rig.Bridge.State);
        mover = rig.Token("golani"); rig.Select(mover);
        var selected = rig.Layer.Selected;
        var committed = rig.Bridge.Session.Committed.Count;
        var queued = rig.Bridge.Queued.Count;
        var before = rig.At(id);
        var expectedSelection = rig.Layer.UnitToSelectAt(target.q, target.r);
        if (reason == "covered")
        {
            // A wide screen over the panel (InfoPanelPresenter.Covered, #210), as UnderACoveringScreenNoClickMovesTheUnit.
            rig.Ui.Q<VisualElement>("info-panel").parent.AddToClassList(ScreenNav.WideOpenClass);
            ScreenNav.DisableCovered(rig.Ui.Root);
        }
        rig.Click(target.q, target.r, touch: true);
        var send = rig.Ui.Root.Q<Button>("info-confirm-move");
        Assert.IsNotNull(send);
        Assert.AreEqual(DisplayStyle.None, send.style.display.value, reason);
        UiTestKit.Press(send);
        Assert.AreEqual(committed, rig.Bridge.Session.Committed.Count);
        Assert.AreEqual(queued, rig.Bridge.Queued.Count);
        Assert.AreEqual(before, rig.At(id));
        if (reason == "covered") Assert.AreEqual(selected, rig.Layer.Selected, "coverage preserves the existing selection");
        else
        {
            Assert.AreEqual(expectedSelection, rig.Layer.Selected, "ordinary unit selection stays");
            Assert.AreEqual(expectedSelection != null, rig.Info.ShowsUnit);
            if (expectedSelection == null) Assert.AreEqual(target, rig.Info.Selected, "ordinary hex selection stays");
        }
    }
}


/// <summary>#1661 ruling 1 by touch: a virtual touchscreen through MapPointer's real frame, so an InputTestFixture.</summary>
public class TouchConfirmTests : InputTestFixture
{
    /// <summary>Touch: a still tap through MapPointer's real frame (a virtual touchscreen) picks the destination, for a
    /// friend's token, a city a friend stands on and an empty city beyond reach; the offer then moves the retained unit once.
    /// Keyboard Tab/Enter/Space and the UI hit test on the offer run in the real game in
    /// KeyboardOrdersTests.AFriendlyDestinationIsConfirmedWithTheKeyboardAlone.</summary>
    [Test]
    public void TouchSelectsAndActivatesTheConfirmation()
    {
        foreach (var kind in new[] { "friendly", "occupied-city", "far-city" })
            TouchOffersAConfirmationForTheRetainedMover(kind);
    }

    void TouchOffersAConfirmationForTheRetainedMover(string kind)
    {
        using var rig = new SelectThenClickMoveTests.Rig();
        var c = rig.Bridge.Content;
        var mover = rig.Token("golani");
        var city = c.SettlementHexes.First(x => x.Id == "tel_aviv" && rig.Layer.UnitToSelectAt(x.Q, x.R) == null);
        (int Q, int R) target = (city.Q, city.R);
        if (kind == "occupied-city")
        {
            var (friendMover, friendCity) = rig.FriendOnACity();
            mover = friendMover; target = (friendCity.q, friendCity.r);
        }
        if (kind == "friendly")
        {
            var own = rig.Layer.Tokens.Where(t => t.Token.Unit.Side == UnitData.Israel
                && UnitCard.SimUnit(c, rig.Bridge.State, t.Token.Unit) != null).ToList();
            var pair = (from a in own from b in own
                        where a.Token.Unit.Id != b.Token.Unit.Id && (a.Q, a.R) != (b.Q, b.R)
                            && rig.Layer.UnitToSelectAt(b.Q, b.R) == b.Token.Unit.Id
                            && rig.Reach(rig.Id(a)).Contains((b.Q, b.R))
                        select (a, b)).First();
            mover = pair.a; target = (pair.b.Q, pair.b.R);
        }
        var id = rig.Id(mover);
        var before = rig.At(id);
        var baseline = rig.Bridge.Session.Committed.Count;
        var near = rig.Reach(id).Contains((target.Q, target.R));
        var others = rig.Bridge.State.Units.Values.Where(u => u.Id != id).ToDictionary(u => u.Id, u => (u.Q, u.R));
        Assert.IsNotEmpty(Movement.Route(c, rig.Bridge.State, id, target.Q, target.R, rig.Bridge.Spent));
        var cam = Camera.main;
        var view = cam.GetComponent<CameraRig>() ?? cam.gameObject.AddComponent<CameraRig>();
        var hex = c.HexAt(target.Q, target.R)!;
        var at = rig.Map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        at.y = rig.Map.Terrain.SampleHeight(at) + rig.Map.Terrain.transform.position.y;
        view.LookAt = at; view.Distance = 30000f; view.Yaw = 0; view.Apply();
        Physics.SyncTransforms();
        var pointer = cam.gameObject.AddComponent<MapPointer>();
        pointer.Map = rig.Map; pointer.IsOverUi = _ => false;
        try
        {
            pointer.HexClicked += (q, r, pill) => GameRoot.OnHexClicked(rig.Map, rig.Layer, rig.Info, q, r, true, pill);
            rig.Select(mover);
            var expectedUnit = rig.Layer.UnitToSelectAt(target.Q, target.R);   // before the pick moves the selection
            InputSystem.AddDevice<Touchscreen>();
            pointer.Tick(); // warm frame APIs, as Ella documents; this is a still tap across updates
            var screen = cam.WorldToScreenPoint(at);
            var position = new Vector2(screen.x, screen.y);
            BeginTouch(1, position); pointer.Tick();
            Assert.AreEqual(baseline, rig.Bridge.Session.Committed.Count);
            EndTouch(1, position); pointer.Tick();
            pointer.Tick(); // tap is enqueued after the release frame's drain
            if (expectedUnit == null) Assert.AreEqual(((int, int)?)(target.Q, target.R), rig.Info.Selected, "ordinary hex selection");
            else Assert.AreEqual(expectedUnit, rig.Layer.Selected, "ordinary unit selection");
            Assert.AreEqual(baseline, rig.Bridge.Session.Committed.Count, "the tap itself gives no order");
            var send = rig.Ui.Root.Q<Button>("info-confirm-move");
            Assert.IsNotNull(send);
            Assert.AreEqual(DisplayStyle.Flex, send.style.display.value);
            // The button is taken as the UI takes any button press (Button.clicked); that a finger on it lands on the
            // button and not the map is KeyboardOrdersTests' hit test in the real game.
            UiTestKit.Press(send);
            Assert.AreEqual(baseline + 1, rig.Bridge.Session.Committed.Count);
            Assert.AreNotEqual(before, rig.At(id), "the original mover moves immediately");
            Assert.Greater(rig.Bridge.Spent[id], 0);
            foreach (var other in others) Assert.AreEqual(other.Value, rig.At(other.Key), "other units stay put");
            if (!near)
            {
                Assert.AreEqual(Movement.GoTo, rig.Bridge.State.Units[id].Order);
                Assert.AreEqual(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}", target.Q, target.R),
                    rig.Bridge.State.Units[id].OrderTarget);
            }
            else Assert.AreEqual((target.Q, target.R), rig.At(id));
            UiTestKit.Press(send);
            Assert.AreEqual(baseline + 1, rig.Bridge.Session.Committed.Count, "confirmation consumed exactly once");
        }
        finally { Object.DestroyImmediate(pointer); }
    }
}
