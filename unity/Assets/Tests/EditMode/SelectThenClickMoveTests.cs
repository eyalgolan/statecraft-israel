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

    sealed class Rig : System.IDisposable
    {
        public readonly MapScene Map;
        public readonly UiTestKit Ui;
        public readonly SimBridge Bridge;
        public readonly UnitLayer Layer;
        public readonly InfoPanelPresenter Info;
        public readonly List<IReadOnlyCollection<(int q, int r)>> Lit = new List<IReadOnlyCollection<(int q, int r)>>();

        public Rig()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Map = new GameObject("MapScene").AddComponent<MapScene>();
            Map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
            Map.Build();
            Ui = new UiTestKit();   // after the new scene, which would take the UI document with it
            Bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            Layer = UnitLayer.Create(Map, Units, C.Hexes, still: true);
            Layer.Show(Bridge.Content, Bridge.State);
            var layer = Layer; var map = Map; var lit = Lit;
            Info = new InfoPanelPresenter(Ui.Root, Bridge, Text, C.SettlementNames,
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
        var land = C.Hexes.First(h => h.Land && rig.Layer.At(h.Q, h.R) == null && !rig.Info.IsCity(h.Q, h.R) && (h.Q, h.R) != (still.Q, still.R));
        rig.Layer.Select(null);
        rig.Select(still);
        Assert.AreEqual("golani", rig.Layer.Selected, "precondition: Golani's card");
        Assert.IsFalse(rig.Info.MovePicking, "no reach: no pick");
        GameRoot.OnHexOrdered(rig.Map, rig.Info, land.Q, land.R);
        Assert.IsEmpty(rig.Bridge.Session.Committed, "a right click orders nothing");
        Assert.IsTrue(rig.Info.ShowsUnit, "and changes nothing");
        rig.Click(land.Q, land.R);
        Assert.IsEmpty(rig.Bridge.Session.Committed);
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
}
