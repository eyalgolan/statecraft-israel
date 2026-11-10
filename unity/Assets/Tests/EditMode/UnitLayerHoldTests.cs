#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>#2202 (plan docs/superpowers/plans/2026-11-02-enemy-move-playback.md, Task 3): a token can wait at the place it
/// was drawn while the sim has already moved its unit, and slide from there when its beat comes. It extends #2176's slide
/// (UnitsFromStateTests.AMovedTokenSlidesFromWhereItWasDrawnAndStops), with no second offset path. Presentation only.</summary>
public class UnitLayerHoldTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Units => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static GameState NewGame() => Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();

    MapScene map = null!;
    UnitLayer layer = null!;
    GameState state = null!;

    [SetUp]
    public void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        Statecraft.Game.Map.Kits.KitPopulation.Use(Statecraft.Game.Map.Kits.KitPopulation.Parse(
            File.ReadAllText(Path.Combine(Paths.Content, "map", Statecraft.Game.Map.Kits.KitPopulation.FileName))));
        map.Build();
        state = NewGame();
        layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
        layer.AnimateMoves = true;
        layer.Show(C, state);
    }

    [TearDown]
    public void Free() { if (map != null) Object.DestroyImmediate(map.gameObject); }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    (int q, int r) FreeNeighbour(int q, int r)
    {
        foreach (var (dq, dr) in new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) })
        {
            var h = C.HexAt(q + dq, r + dr);
            if (h != null && h.Land && !state.Units.Values.Any(u => u.Q == h.Q && u.R == h.R)) return (h.Q, h.R);
        }
        throw new InvalidOperationException($"no free land hex next to ({q},{r})");
    }

    void Move(Unit unit)
    {
        var u = state.Units[unit.Id];
        var to = FreeNeighbour(u.Q, u.R);
        state.Units[u.Id] = new Unit(u.Id, u.Type, to.q, to.r, u.Service, u.Side, u.Strength, u.Order, u.OrderTarget);
    }

    Unit Israeli(int index) => state.Units.Values.Where(u => u.Side == Movement.PlayerSide)
        .Where(u => layer.Tokens.Any(t => t.Token.StateUnit?.Id == u.Id)).OrderBy(u => u.Id).ElementAt(index);

    UnitTokenMotion MotionOf(int simId) =>
        layer.GetComponentsInChildren<UnitTokenMotion>(true).Single(m => m.SimId == simId);

    Vector2 SpotOf(int simId)
    {
        var spot = layer.Tokens.Single(t => t.Token.StateUnit?.Id == simId).Spot;
        return new Vector2((float)spot.X, (float)spot.Z);
    }

    static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);

    /// <summary>Where the token of <paramref name="simId"/> is drawn: its spot plus its root's offset (UnitTokenMotion).</summary>
    Vector2 DrawnAt(int simId) => SpotOf(simId) + XZ(MotionOf(simId).transform.position);

    [Test]
    public void AHeldTokenStandsAtItsOldPlaceThroughARebuild()
    {
        var unit = Israeli(0);
        var old = SpotOf(unit.Id);
        layer.Hold(new[] { unit.Id });
        Move(unit);
        Assert.IsTrue(layer.Show(C, state), "fixture: the move rebuilds the tokens");
        Assert.Greater((SpotOf(unit.Id) - old).magnitude, 1000f, "fixture: a real move");
        var motion = MotionOf(unit.Id);
        Assert.IsTrue(motion.Held);
        Assert.IsFalse(motion.Moving, "it waits, it does not slide");
        Assert.IsFalse(motion.enabled, "a waiting token runs nothing per frame");
        Assert.Less((DrawnAt(unit.Id) - old).magnitude, 1f, "it stands where it was drawn");

        Move(Israeli(1));   // another unit's order rebuilds every token
        Assert.IsTrue(layer.Show(C, state));
        Assert.IsTrue(MotionOf(unit.Id).Held);
        Assert.Less((DrawnAt(unit.Id) - old).magnitude, 1f, "still there after another rebuild");
    }

    [Test]
    public void ReleaseSlidesItFromThereLikeAnyMove()
    {
        var unit = Israeli(0);
        var old = SpotOf(unit.Id);
        layer.Hold(new[] { unit.Id });
        Move(unit);
        layer.Show(C, state);
        Assert.IsTrue(layer.Release(unit.Id));
        var motion = MotionOf(unit.Id);
        Assert.IsFalse(motion.Held);
        Assert.IsTrue(motion.Moving && motion.enabled, "the slide starts");
        Assert.Less((DrawnAt(unit.Id) - old).magnitude, 1f, "from the old place");
        motion.Tick(UnitTokenMotion.Duration);
        Assert.AreEqual(Vector3.zero, motion.transform.position, "it ends on its spot");
        Assert.IsFalse(motion.Moving);
        Assert.IsFalse(layer.Release(unit.Id), "released once: it is no longer held");
    }

    [Test]
    public void ReleaseAllWithSnapPutsEveryHeldTokenAtItsPlaceAtOnce()
    {
        var a = Israeli(0); var b = Israeli(1);
        layer.Hold(new[] { a.Id, b.Id });
        Move(a); Move(b);
        layer.Show(C, state);
        Assert.IsTrue(MotionOf(a.Id).Held && MotionOf(b.Id).Held, "fixture");
        layer.ReleaseAll(snap: true);
        foreach (var id in new[] { a.Id, b.Id })
        {
            var m = MotionOf(id);
            Assert.IsFalse(m.Held, id.ToString()); Assert.IsFalse(m.Moving, id.ToString());
            Assert.AreEqual(Vector3.zero, m.transform.position, "on its spot at once");
        }
        Move(Israeli(2));
        layer.Show(C, state);
        Assert.AreEqual(Vector3.zero, MotionOf(a.Id).transform.position, "the held set is empty after ReleaseAll");
    }

    [Test]
    public void ReleaseAllWithoutSnapSlidesThem()
    {
        var a = Israeli(0);
        layer.Hold(new[] { a.Id });
        Move(a);
        layer.Show(C, state);
        layer.ReleaseAll(snap: false);
        Assert.IsTrue(MotionOf(a.Id).Moving);
    }

    [Test]
    public void AnUnheldMovedTokenStillSlidesAtOnce()
    {
        var held = Israeli(0); var free = Israeli(1);
        layer.Hold(new[] { held.Id });
        Move(held); Move(free);
        layer.Show(C, state);
        Assert.IsTrue(MotionOf(free.Id).Moving, "#2176's slide is unchanged for a unit nothing holds");
        Assert.IsFalse(MotionOf(free.Id).Held);
        Assert.IsFalse(MotionOf(held.Id).Moving);
    }

    [Test]
    public void MidHoldTheBadgeAndItsBoxesAreAtTheOldPlace()
    {
        var unit = Israeli(0);
        var old = SpotOf(unit.Id);
        var eye = new GameObject("HoldCamera").AddComponent<Camera>();
        try
        {
            eye.farClipPlane = 1e6f;
            layer.FaceCamera(eye);
            var badgeBefore = XZ(MotionOf(unit.Id).transform.Find("Badge").position);
            layer.Hold(new[] { unit.Id });
            Move(unit);
            layer.Show(C, state);
            layer.FaceCamera(eye);
            var badge = XZ(MotionOf(unit.Id).transform.Find("Badge").position);
            float moved = (SpotOf(unit.Id) - old).magnitude;
            // A compact spot sets the badge a little nearer its figures than a full one (UnitLayer.Spread), so the badge is
            // compared with the length of the move, not to the metre.
            Assert.Less((badge - badgeBefore).magnitude, 0.1f * moved, "the badge waits with the token");
            int i = layer.Tokens.Select((t, k) => (t, k)).Single(x => x.t.Token.StateUnit?.Id == unit.Id).k;
            Assert.Less((XZ(layer.KeepClear[i][0].center) - badge).magnitude, 1f, "and so does the box a click finds");
        }
        finally { Object.DestroyImmediate(eye.gameObject); }
    }

    [Test]
    public void ANewGameClearsTheHeldSet()
    {
        var unit = Israeli(0);
        layer.Hold(new[] { unit.Id });
        Move(unit);
        layer.Show(C, state, slide: false);   // SimBridge.Replace: another game, the same sim ids
        Assert.IsFalse(MotionOf(unit.Id).Held);
        Assert.AreEqual(Vector3.zero, MotionOf(unit.Id).transform.position);
        // The unit goes back to where it started, a place the layer draws it apart from its moved one (the slide the tests
        // above see). A second Move from the moved hex could be placed on the very spot it is drawn on (a moved anchor's spot
        // is any clear one within a hex), and a token that has not moved does not slide.
        var drawn = MotionOf(unit.Id).GroundHome;
        state = NewGame();
        layer.Show(C, state);
        Assert.AreNotEqual(drawn, MotionOf(unit.Id).GroundHome, "the unit is drawn somewhere else");
        Assert.IsFalse(MotionOf(unit.Id).Held, "nothing is left held for the new game");
        Assert.IsTrue(MotionOf(unit.Id).Moving, "so it slides there, as any unheld move");
    }

    [Test]
    public void AHiddenLayerHoldsNothing()
    {
        var unit = Israeli(0);
        layer.transform.Find("Tokens").gameObject.SetActive(false);
        layer.Hold(new[] { unit.Id });
        Move(unit);
        layer.Show(C, state);
        var motion = MotionOf(unit.Id);
        Assert.IsFalse(motion.Held, "a token no one can see does not wait");
        Assert.IsFalse(motion.Moving);
        Assert.AreEqual(Vector3.zero, motion.transform.position);
    }

    [Test]
    public void ReleasingAnUndrawnUnitIsFalse()
    {
        Assert.IsFalse(layer.Release(987654));
        Assert.IsFalse(layer.Draws(987654));
        Assert.IsTrue(layer.Draws(Israeli(0).Id));
        layer.Hold(new[] { 987654 });
        Assert.IsFalse(layer.Release(987654), "held but never drawn");
    }

    [Test]
    public void HoldingATokenMidSlideStopsItWhereItIsDrawn()
    {
        var unit = Israeli(0);
        Move(unit);
        layer.Show(C, state);
        var motion = MotionOf(unit.Id);
        motion.Tick(UnitTokenMotion.Duration / 2f);
        var drawn = DrawnAt(unit.Id);
        layer.Hold(new[] { unit.Id });
        Assert.IsTrue(motion.Held); Assert.IsFalse(motion.Moving);
        Assert.Less((DrawnAt(unit.Id) - drawn).magnitude, 1f);
    }
}
