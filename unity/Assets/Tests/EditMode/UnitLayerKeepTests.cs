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

/// <summary>#3055 (A of #2905): a rebuild keeps the GameObjects of the tokens that did not change and builds or destroys only
/// the ones that did. #2957's ?turnlog probe put the full rebuild at 25, 145 and 330 ms of End turns 1, 2 and 5 in WebGL. A
/// moved unit keeps its token's root and still slides from where it was drawn; a destroyed unit still raises its dust; a
/// changed unit's pieces are built again; another game (New Game, a load) builds every token afresh. Presentation only.</summary>
public class UnitLayerKeepTests
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

    Transform TokensRoot => layer.transform.Find("Tokens");

    (int q, int r) FreeNeighbour(int q, int r)
    {
        foreach (var (dq, dr) in new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) })
        {
            var h = C.HexAt(q + dq, r + dr);
            if (h != null && h.Land && !state.Units.Values.Any(u => u.Q == h.Q && u.R == h.R)) return (h.Q, h.R);
        }
        throw new InvalidOperationException($"no free land hex next to ({q},{r})");
    }

    /// <summary>Moves the unit one hex, every other field as it was (HomeCity and Strength are part of the token's identity).</summary>
    void Move(Unit unit)
    {
        var u = state.Units[unit.Id];
        var to = FreeNeighbour(u.Q, u.R);
        state.Units[u.Id] = new Unit(u.Id, u.Type, to.q, to.r, u.Service, u.Side, u.Strength, u.Order, u.OrderTarget, u.HomeCity, u.Full, u.CombatXp, u.Records);
    }

    void SetStrength(Unit unit, int strength)
    {
        var u = state.Units[unit.Id];
        state.Units[u.Id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, strength, u.Order, u.OrderTarget, u.HomeCity, u.Full, u.CombatXp, u.Records);
    }

    Unit Israeli(int index) => state.Units.Values.Where(u => u.Side == Movement.PlayerSide)
        .Where(u => layer.Tokens.Any(t => t.Token.StateUnit?.Id == u.Id)).OrderBy(u => u.Id).ElementAt(index);

    int IndexOf(int simId) => layer.Tokens.Select((t, k) => (t, k)).Single(x => x.t.Token.StateUnit?.Id == simId).k;

    /// <summary>The token root drawing <paramref name="simId"/>: the Tokens child in the order of <see cref="UnitLayer.Tokens"/>.</summary>
    GameObject TokenOf(int simId) => TokensRoot.GetChild(IndexOf(simId)).gameObject;

    UnitTokenMotion MotionOf(int simId) => TokenOf(simId).GetComponent<UnitTokenMotion>();

    Vector2 SpotOf(int simId)
    {
        var spot = layer.Tokens[IndexOf(simId)].Spot;
        return new Vector2((float)spot.X, (float)spot.Z);
    }

    static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);

    Vector2 DrawnAt(int simId) => SpotOf(simId) + XZ(MotionOf(simId).transform.position);

    static (int sim, string row) KeyOf(PlacedToken p) => p.Token.StateUnit is Unit u ? (u.Id, "") : (-1, p.Token.Unit.Id);

    /// <summary>Each drawn token's root and what it was built from, by its sim unit (else its row).</summary>
    Dictionary<(int sim, string row), (GameObject go, PlacedToken placed)> Drawn()
    {
        var drawn = new Dictionary<(int, string), (GameObject, PlacedToken)>();
        for (int i = 0; i < layer.Tokens.Count; i++) drawn[KeyOf(layer.Tokens[i])] = (TokensRoot.GetChild(i).gameObject, layer.Tokens[i]);
        return drawn;
    }

    void AssertTokensInOrder(string what)
    {
        Assert.AreEqual(layer.Tokens.Count, TokensRoot.childCount, $"{what}: one root per drawn token, no leftover");
        for (int i = 0; i < layer.Tokens.Count; i++)
            Assert.AreEqual(layer.Tokens[i].Token.Unit.Id, TokensRoot.GetChild(i).name, $"{what}: the roots stand in the tokens' order ({i})");
    }

    /// <summary>(a) A moved unit keeps its token's root (the same instance, with its motion) and still slides from the old
    /// place to the new one, as #2176's slide did on a new token.</summary>
    [Test]
    public void AMovedUnitKeepsItsTokenAndSlidesFromTheOldHex()
    {
        var unit = Israeli(0);
        var before = TokenOf(unit.Id); var motion = before.GetComponent<UnitTokenMotion>();
        var old = SpotOf(unit.Id);
        layer.Select(layer.Tokens[IndexOf(unit.Id)].Token.Unit.Id);
        Move(unit);
        Assert.IsTrue(layer.Show(C, state), "fixture: the move changes the roster");
        Assert.Greater((SpotOf(unit.Id) - old).magnitude, 1000f, "fixture: a real move, not a nudge");
        Assert.AreSame(before, TokenOf(unit.Id), "the moved unit keeps its token's GameObject");
        Assert.AreSame(motion, MotionOf(unit.Id), "and its motion");
        Assert.IsTrue(motion.Moving && motion.enabled, "it slides");
        Assert.Less((DrawnAt(unit.Id) - old).magnitude, 1f, "from where it was drawn");
        Assert.AreEqual(layer.Tokens[IndexOf(unit.Id)].Token.Unit.Id, layer.Selected, "the selection follows the unit");
        AssertTokensInOrder("after the move");
        motion.Tick(UnitTokenMotion.Duration);
        Assert.AreEqual(Vector3.zero, motion.transform.position, "it ends on its new spot");
        Assert.Less((XZ(motion.GroundHome) - SpotOf(unit.Id)).magnitude, 1f, "its home is the new spot");
    }

    /// <summary>(b) A destroyed unit still raises its dust where its token stood, once, and the tokens of the units that
    /// survived are the same GameObjects as before.</summary>
    [Test]
    public void ADestroyedUnitStillRaisesItsDustAndTheOthersKeepTheirTokens()
    {
        Assert.IsNotNull(layer.Combat, "fixture: the flipbooks ship");
        var lost = Israeli(1); var was = SpotOf(lost.Id);
        var drawn = Drawn();
        state.Units.Remove(lost.Id);
        Assert.IsTrue(layer.Show(C, state), "fixture: the roster changed");
        Assert.IsFalse(layer.Draws(lost.Id));
        Assert.IsTrue(drawn[(lost.Id, "")].go == null, "the lost unit's token is destroyed");
        for (int i = 0; i < layer.Tokens.Count; i++)
            Assert.AreSame(drawn[KeyOf(layer.Tokens[i])].go, TokensRoot.GetChild(i).gameObject, $"{layer.Tokens[i].Token.Unit.Id} keeps its token");
        AssertTokensInOrder("after the loss");
        Assert.IsTrue(layer.PlayDestroyed(lost.Id), "the lost unit raises its dust");
        var dust = layer.Combat!.Dust!;
        Assert.AreEqual(CombatBursts.DeathPuffs, dust.Busy);
        for (int i = 0; i < dust.Capacity; i++)
            if (!double.IsNaN(dust.StartOf(i)))
                Assert.LessOrEqual((XZ(dust.Foot(i)) - was).magnitude, CombatBursts.SpreadShare * CombatBursts.DeathDustM + 1f, "where its token stood");
        Assert.IsFalse(layer.PlayDestroyed(lost.Id), "once");
        Assert.IsFalse(layer.PlayDestroyed(Israeli(0).Id), "a unit still drawn raises none");
    }

    /// <summary>(c) Across a real End turn (Simulation.Step) and a move, every token whose unit, anchor, spot and looks did
    /// not change is the same GameObject, with its base's meshes alive.</summary>
    [Test]
    public void AnUnchangedUnitKeepsItsTokenAcrossAnEndTurn()
    {
        var drawn = Drawn();
        var bases = drawn.ToDictionary(kv => kv.Key, kv => kv.Value.go.transform.Find(UnitPlinth.PlinthName)?.GetComponent<MeshFilter>()?.sharedMesh);
        state = Simulation.Step(C, state, Array.Empty<Command>()).State.Clone();
        Move(Israeli(0));
        Assert.IsTrue(layer.Show(C, state), "fixture: the End turn and the move change the roster");
        int kept = 0;
        for (int i = 0; i < layer.Tokens.Count; i++)
        {
            var p = layer.Tokens[i];
            if (!drawn.TryGetValue(KeyOf(p), out var was) || !was.placed.Equals(p)) continue;
            Assert.AreSame(was.go, TokensRoot.GetChild(i).gameObject, $"{p.Token.Unit.Id}: unchanged, so the same token");
            var mesh = TokensRoot.GetChild(i).Find(UnitPlinth.PlinthName)?.GetComponent<MeshFilter>()?.sharedMesh;
            if (bases[KeyOf(p)] != null)
            {
                Assert.IsTrue(mesh != null, $"{p.Token.Unit.Id}: its base's mesh is not freed");
                Assert.AreSame(bases[KeyOf(p)], mesh, $"{p.Token.Unit.Id}: nor built again");
            }
            kept++;
        }
        Assert.Greater(kept, 3, "fixture: most tokens did not change (not a vacuous pass)");
        AssertTokensInOrder("after the End turn");
    }

    /// <summary>(d) A changed attribute the token is built from (the strength, which sets the figures) builds that token's
    /// pieces again on its own root: the old pieces are destroyed, the new ones match the new strength.</summary>
    [Test]
    public void AChangedStrengthBuildsThatTokensPiecesAgain()
    {
        var unit = Israeli(0);
        var root = TokenOf(unit.Id);
        int Figures() => root.transform.Cast<Transform>().Count(t => t.name.StartsWith("Rifleman"));
        int full = C.StartingForces[unit.Id].Strength;
        int want = UnitRoster.FiguresForStrength(1, full);
        Assert.AreNotEqual(want, Figures(), "fixture: strength 1 draws fewer figures");
        var oldFigure = root.transform.Cast<Transform>().First(t => t.name.StartsWith("Rifleman"));
        var other = TokenOf(Israeli(1).Id);
        SetStrength(unit, 1);
        Assert.IsTrue(layer.Show(C, state), "fixture: the strength changes the roster");
        Assert.AreSame(root, TokenOf(unit.Id), "the root is kept");
        Assert.IsTrue(oldFigure == null, "its old pieces are destroyed");
        Assert.AreEqual(want, Figures(), "its pieces are built for the new strength");
        Assert.AreEqual(1, root.transform.Cast<Transform>().Count(t => t.name == UnitPlinth.PlinthName), "one base, not the old one beside the new");
        Assert.AreEqual(1, root.transform.Cast<Transform>().Count(t => t.name == "Badge"), "one badge");
        Assert.AreSame(other, TokenOf(Israeli(1).Id), "another unit's token is untouched");
        Assert.IsFalse(MotionOf(unit.Id).Moving, "a strength change does not slide");
        AssertTokensInOrder("after the strength change");
    }

    /// <summary>Another game (New Game or a load: SimBridge.Replace, Show with slide false) reuses the sim ids, so no token of
    /// the old game is kept, even where the new game's unit looks the same.</summary>
    [Test]
    public void AnotherGameBuildsEveryTokenAfresh()
    {
        var before = new HashSet<GameObject>(TokensRoot.Cast<Transform>().Select(t => t.gameObject));
        Move(Israeli(0));
        layer.Show(C, state);
        before.UnionWith(TokensRoot.Cast<Transform>().Select(t => t.gameObject));
        Assert.IsTrue(layer.Show(C, NewGame(), slide: false), "fixture: the new game's roster differs");
        foreach (Transform t in TokensRoot) Assert.IsFalse(before.Contains(t.gameObject), $"{t.name}: a new token for the new game");
        AssertTokensInOrder("after another game");
    }

    /// <summary>A rebuild made from far draws the kept and the new tokens' figures at the same far scale (#2430): a new
    /// token's figures are built at their own scale beside kept ones already grown.</summary>
    [Test]
    public void ARebuildFromFarDrawsKeptAndNewFiguresAtTheFarScale()
    {
        var cam = Camera.main; Assert.IsNotNull(cam, "the default scene's camera");
        var unit = Israeli(0);
        var centre = TokenOf(unit.Id).transform.Find(UnitPlinth.PlinthName).position; centre.y = 0f;
        cam!.fieldOfView = CameraRig.Fov; cam.aspect = 16f / 9f; cam.farClipPlane = 1000000f;
        cam.transform.rotation = Quaternion.Euler(CameraRig.Pitch, 0f, 0f);
        cam.transform.position = centre - cam.transform.forward * 60000f;
        float zoom = UnitShowcase.ZoomScale(Statecraft.Game.Map.Kits.TownModelLayer.ViewDistance(cam));
        Assert.Greater(zoom, 1.05f, "fixture: a far view grows the figures");
        layer.FaceCamera(cam);
        Move(unit);
        Assert.IsTrue(layer.Show(C, state), "fixture: the move changes the roster");
        layer.FaceCamera(cam);
        var figures = TokensRoot.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Rifleman")).ToList();
        Assert.Greater(figures.Count, 10, "fixture: the figures of the opening day");
        foreach (var f in figures)
            Assert.AreEqual(zoom * UnitShowcase.FigureScale, f.localScale.x, 1e-3f * zoom * UnitShowcase.FigureScale, $"{f.parent.name}/{f.name} at the far scale");
    }
}
