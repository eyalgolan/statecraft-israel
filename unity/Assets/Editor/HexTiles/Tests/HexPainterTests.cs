#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.HexTiles;
using Statecraft.HexTiles.Painter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>The map builder's hand edits (HexBoardOverrides), the Hex Painter's write-back and one-hex rebuild, its
/// palette, brushes and preview switches, and the environment's region species (docs/hex-tiles/README.md, "Laying out the whole map").</summary>
public class HexPainterTests
{
    string dir = "";

    [SetUp] public void MakeDir() { dir = Path.Combine(Path.GetTempPath(), "hex-painter-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir); }
    [TearDown] public void RemoveDir() { if (Directory.Exists(dir)) Directory.Delete(dir, true); }

    /// <summary>Every edit as (q, r, field, value), field by field in HexBoardOverrides.Fields order.</summary>
    static IEnumerable<(int, int, string, string)> All(HexBoardOverrides o) =>
        HexBoardOverrides.Fields.SelectMany(f => o.List(f).Select(e => (e.hex.q, e.hex.r, f, e.value))).ToList();

    static HexBoard Board(params BoardHex[] hexes) => new HexBoard(new HexBoardFile { id = "t", hexes = hexes });
    static BoardHex Hex(int q, int r, string prototype = "wheat_fields", string unit = "", float facing = 0f, string ground = "grass") =>
        new BoardHex { q = q, r = r, prototype = prototype, unit = unit, facing = facing, ground = ground };

    // --- Overrides merge ---

    [Test]
    public void AnEditSetsOnlyItsOwnField()
    {
        var board = Board(Hex(0, 0, unit: "palmach_squad", facing: 30f));
        var o = new HexBoardOverrides(); o.Record(0, 0, "prototype", "kibbutz");
        o.Apply(board);
        var h = board.Hexes[(0, 0)];
        Assert.That((h.prototype, h.unit, h.facing, h.ground), Is.EqualTo(("kibbutz", "palmach_squad", 30f, "grass")));
    }

    [Test]
    public void AnEmptyUnitAndAZeroFacingAreEditsToo()
    {
        var board = Board(Hex(0, 0, unit: "palmach_squad", facing: 30f));
        var o = new HexBoardOverrides(); o.Record(0, 0, "unit", ""); o.Record(0, 0, "facing", "0");
        HexBoardOverrides.FromJson(o.ToJson()).Apply(board);
        Assert.That((board.Hexes[(0, 0)].unit, board.Hexes[(0, 0)].facing), Is.EqualTo(("", 0f)));
    }

    [Test]
    public void ALaterEditOfTheSameFieldReplacesTheEarlierOne()
    {
        var o = new HexBoardOverrides();
        o.Record(1, 2, "prototype", "kibbutz"); o.Record(1, 2, "ground", "plains"); o.Record(1, 2, "prototype", "pine_forest");
        Assert.That(All(o), Is.EqualTo(new[] { (1, 2, "prototype", "pine_forest"), (1, 2, "ground", "plains") }));
    }

    [Test]
    public void EditsOfHexesTheBoardLacksAreReportedNotApplied()
    {
        var board = Board(Hex(0, 0));
        var o = new HexBoardOverrides(); o.Record(5, 5, "prototype", "kibbutz"); o.Record(0, 0, "ground", "desert");
        var missing = o.Apply(board);
        Assert.That(missing.Select(m => (m.field, m.edit.hex.q, m.edit.hex.r)), Is.EqualTo(new[] { ("prototype", 5, 5) }));
        Assert.That(board.Hexes[(0, 0)].ground, Is.EqualTo("desert"));
    }

    [Test]
    public void AnUnknownFieldOrAHexTwiceIsRefused()
    {
        // Terrain, control and locations are the map editor's (content/map/overrides.yaml), not the hex painter's.
        Assert.Throws<ArgumentException>(() => new HexBoardOverrides().Record(0, 0, "terrain", "water"));
        Assert.Throws<ArgumentException>(() => HexBoardOverrides.FromJson(
            "{\"ground\": [{\"hex\": {\"q\": 0, \"r\": 0}, \"value\": \"desert\"}, {\"hex\": {\"q\": 0, \"r\": 0}, \"value\": \"grass\"}]}"));
    }

    [Test]
    public void TheGeneratorsFileFormatReadsBack()
    {
        // As tools/hex_tiles/make_board.py reads it: the shape of content/map/overrides.yaml, one list per field.
        var o = HexBoardOverrides.FromJson("{\"board\": \"israel\", \"facing\": [{\"hex\": {\"q\": -3, \"r\": 19}, \"value\": \"90.5\"}]}");
        var board = Board(Hex(-3, 19));
        o.Apply(board);
        Assert.That(board.Hexes[(-3, 19)].facing, Is.EqualTo(90.5f));
    }

    // --- The painter's write-back ---

    [Test]
    public void WriteBackRecordsTheEditAndChangesTheBoard()
    {
        var board = Board(Hex(0, 0), Hex(1, 0));
        var path = HexPainter.OverridesPath(dir, "t");
        HexPainter.Write(board, path, 0, 0, "prototype", "kibbutz");
        HexPainter.Write(board, path, 1, 0, "unit", "palmach_squad");
        HexPainter.Write(board, path, 0, 0, "prototype", "pine_forest");
        Assert.That(Path.GetFileName(path), Is.EqualTo("t.overrides.json"));
        var saved = HexBoardOverrides.FromJson(File.ReadAllText(path));
        Assert.That(saved.board, Is.EqualTo("t"));
        Assert.That(All(saved), Is.EqualTo(new[] { (0, 0, "prototype", "pine_forest"), (1, 0, "unit", "palmach_squad") }));
        Assert.That((board.Hexes[(0, 0)].prototype, board.Hexes[(1, 0)].unit), Is.EqualTo(("pine_forest", "palmach_squad")));
        // The file alone gives the same board again (what regenerating and then loading does).
        var fresh = Board(Hex(0, 0), Hex(1, 0));
        saved.Apply(fresh);
        Assert.That((fresh.Hexes[(0, 0)].prototype, fresh.Hexes[(1, 0)].unit), Is.EqualTo(("pine_forest", "palmach_squad")));
    }

    [Test]
    public void WriteBackRefusesUnknownValuesAndLeavesTheFileAlone()
    {
        var board = Board(Hex(0, 0));
        var path = HexPainter.OverridesPath(dir, "t");
        Assert.Throws<ArgumentException>(() => HexPainter.Write(board, path, 0, 0, "prototype", "no_such_tile"));
        Assert.Throws<ArgumentException>(() => HexPainter.Write(board, path, 0, 0, "ground", "lava"));
        Assert.Throws<ArgumentException>(() => HexPainter.Write(board, path, 9, 9, "ground", "desert"));
        Assert.That(File.Exists(path), Is.False);
        Assert.That(board.Hexes[(0, 0)].prototype, Is.EqualTo("wheat_fields"));
    }

    [Test]
    public void BoardIdsLeaveOutOverridesFiles()
    {
        File.WriteAllText(Path.Combine(dir, "b.json"), "{}");
        File.WriteAllText(Path.Combine(dir, "a.json"), "{}");
        File.WriteAllText(Path.Combine(dir, "a.overrides.json"), "{}");
        Assert.That(HexPainter.BoardIds(dir), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void ChangeRebuildsOnlyThatHexAndItsGround()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var file = new HexBoardFile
        {
            id = "t",
            hexes = new[] { Hex(0, 0), Hex(1, 0, "pine_forest"), Hex(0, 1, "olive_terraces"), Hex(-1, 1, unit: "palmach_squad"), Hex(1, -1, ""),
                new BoardHex { q = -1, r = 0, terrain = "hills", ground = "hills", env = true } },
            regions = new[] { new BoardRegion { id = "south", hills = new[] { new BoardSpecies { model = "sandbox:acacia_lite" } }, density = 3f } },
        };
        var (root, _) = HexBoardBuilder.Build(new HexBoard(file), null, options: new HexBoardBuilder.Options { PerHex = true });
        try
        {
            var pieces = root.transform.Find(HexBoardBuilder.PiecesName);
            int Id(int q, int r) => pieces.Find(HexBoardBuilder.HexName(q, r))!.gameObject.GetInstanceID();
            int other = Id(1, 0), third = Id(0, 1), mine = Id(0, 0);
            var state = root.GetComponent<HexBoardState>();
            float Desert()
            {
                var c = HexGeometry.Centre(1, -1, HexScale.HexM);
                int n = state.Terrain.alphamapResolution;
                int x = Mathf.RoundToInt((c.x - state.Rect.x) / state.Rect.width * (n - 1)), z = Mathf.RoundToInt((c.z - state.Rect.y) / state.Rect.height * (n - 1));
                return state.Terrain.GetAlphamaps(x, z, 1, 1)[0, 0, HexGround.Ids.ToList().IndexOf("desert")];
            }
            Assert.That(Desert(), Is.LessThan(0.01f));

            // Picking: a click meets the terrain, so even a shallow ray onto a hill's top finds that hill, not the hex
            // where the ray would reach sea level (about 7 km on).
            Physics.SyncTransforms();
            var top = HexGeometry.Centre(-1, 0, HexScale.HexM); top.y = state.Surface.Height(top);
            Assert.That(top.y, Is.GreaterThan(400f));
            Assert.That(HexPainter.Pick(root, new Ray(top + Vector3.up * 5000f, Vector3.down)), Is.EqualTo(((int, int)?)(-1, 0)));
            var shallow = Quaternion.Euler(5f, 0f, 0f) * Vector3.forward;
            Assert.That(HexPainter.Pick(root, new Ray(top - shallow * 20000f, shallow)), Is.EqualTo(((int, int)?)(-1, 0)));
            Assert.That(HexPainter.Pick(root, new Ray(new Vector3(9e5f, 3000f, 9e5f), Vector3.down)), Is.Null, "off the board");

            var path = HexPainter.OverridesPath(dir, "t");
            var report = HexPainter.Change(root, path, 0, 0, "prototype", "pine_forest");
            Assert.That(report!.Prototype, Is.EqualTo("pine_forest"));
            Assert.That(Id(1, 0), Is.EqualTo(other)); Assert.That(Id(0, 1), Is.EqualTo(third));
            Assert.That(Id(0, 0), Is.Not.EqualTo(mine));
            Assert.That(pieces.Cast<Transform>().Count(t => t.name == HexBoardBuilder.HexName(0, 0)), Is.EqualTo(1));

            Assert.That(HexPainter.Change(root, path, 1, -1, "ground", "desert"), Is.Null, "a hex with no prototype has no tile");
            Assert.That(Desert(), Is.GreaterThan(0.5f), "the hex's ground is repainted");

            var units = root.transform.Find(HexBoardBuilder.UnitsName);
            HexPainter.Change(root, path, -1, 1, "unit", "");
            Assert.That(units.Find(HexBoardBuilder.UnitName(-1, 1)), Is.Null);
            HexPainter.Change(root, path, 0, 0, "unit", "palmach_squad");
            Assert.That(units.Find(HexBoardBuilder.UnitName(0, 0)).childCount, Is.EqualTo(HexUnitScale.Characters));

            // An environment hex's region sets its trees: only its own env group is rebuilt.
            int env = pieces.Find(HexBoardBuilder.EnvName(-1, 0))!.gameObject.GetInstanceID();
            Assert.Throws<ArgumentException>(() => HexPainter.Change(root, path, -1, 0, "region", "nowhere"));
            HexPainter.Change(root, path, -1, 0, "region", "south");
            Assert.That(pieces.Find(HexBoardBuilder.EnvName(-1, 0))!.gameObject.GetInstanceID(), Is.Not.EqualTo(env));
            Assert.That(HexEnvironment.DressHex(state.Board, state.Board.Hexes[(-1, 0)], state.Surface, state.Seed).Where(p => p.StandIn != StandIn.Rock)
                .Select(p => p.Model).Distinct(), Is.EqualTo(new[] { "sandbox:acacia_lite" }));
            Assert.That(Id(1, 0), Is.EqualTo(other));

            var saved = HexBoardOverrides.FromJson(File.ReadAllText(path));
            Assert.That(All(saved).Select(e => (e.Item1, e.Item2, e.Item3)),
                Is.EqualTo(new[] { (0, 0, "prototype"), (1, -1, "ground"), (-1, 1, "unit"), (0, 0, "unit"), (-1, 0, "region") }));
        }
        finally
        {
            root.GetComponent<HexBoardAssets>().Teardown();
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void LoadAppliesTheBoardsSiblingOverrides()
    {
        const string id = "zz_painter_test";
        string folder = HexPainter.BoardsFolder, board = $"{folder}/{id}.json", over = $"{folder}/{id}{HexBoardOverrides.Suffix}.json";
        try
        {
            File.WriteAllText(board, JsonUtility.ToJson(new HexBoardFile { id = id, hexes = new[] { Hex(0, 0), Hex(1, 0) } }));
            var o = new HexBoardOverrides { board = id }; o.Record(1, 0, "prototype", "kibbutz");
            File.WriteAllText(over, o.ToJson());
            AssetDatabase.ImportAsset(board); AssetDatabase.ImportAsset(over);
            var loaded = HexBoard.Load(id);
            Assert.That((loaded.Hexes[(0, 0)].prototype, loaded.Hexes[(1, 0)].prototype), Is.EqualTo(("wheat_fields", "kibbutz")));
        }
        finally
        {
            AssetDatabase.DeleteAsset(board); AssetDatabase.DeleteAsset(over);
        }
    }

    // --- The palette ---

    [Test]
    public void TheTilesPaletteListsEveryPrototypeOnceInKindThenRegionOrder()
    {
        var groups = HexPalette.Tiles();
        var ids = groups.SelectMany(g => g.Entries).Select(e => e.Id).ToList();
        Assert.That(ids.Count, Is.EqualTo(ids.Distinct().Count()), "listed twice");
        CollectionAssert.AreEquivalent(HexPrototypes.All.Keys, ids);
        int Order(HexPaletteEntry e) => Array.IndexOf(HexPrototypes.Kinds, HexPrototypes.Kinds.Contains(e.Kind) ? e.Kind : "other") * 10
            + (HexPrototypes.Regions.Contains(e.Region) ? Array.IndexOf(HexPrototypes.Regions, e.Region) : HexPrototypes.Regions.Length);
        var orders = groups.SelectMany(g => g.Entries).Select(Order).ToList();
        Assert.That(orders, Is.Ordered, "kind order, then region order");
        Assert.That(groups.Select(g => g.Name), Is.Unique);
        foreach (var e in groups.SelectMany(g => g.Entries))
            Assert.That((e.Kind, e.Region, e.Title), Is.EqualTo((HexPrototypes.Info(e.Id).Kind, HexPrototypes.Info(e.Id).Region, HexPrototypes.Create(e.Id).Title)));
    }

    [Test]
    public void TheTilesGroupingComesFromTheAttributeNotAList()
    {
        // As the regional variants another lane adds will register: farm and forest, north, centre or south.
        HexPaletteEntry E(string id, string kind, string region = "") => new HexPaletteEntry { Id = id, Kind = kind, Region = region };
        var groups = HexPalette.GroupTiles(new[]
        {
            E("wheat_fields_south_a", "farm", "south"), E("odd_one", ""), E("pine_forest_north_b", "forest", "north"), E("wheat_fields", "farm"),
            E("wheat_fields_north_b", "farm", "north"), E("wheat_fields_north_a", "farm", "north"), E("tel_aviv_city", "city"), E("odd_two", "nowhere", "mars"),
        });
        Assert.That(groups.Select(g => g.Name), Is.EqualTo(new[] { "city", "farm, north", "farm, south", "farm", "forest, north", "other" }));
        Assert.That(groups[1].Entries.Select(e => e.Id), Is.EqualTo(new[] { "wheat_fields_north_a", "wheat_fields_north_b" }));
        Assert.That(groups.Last().Entries.Select(e => e.Id), Is.EqualTo(new[] { "odd_one", "odd_two" }));
    }

    [Test]
    public void TheUnitsPaletteListsEveryUnitTypeOnceCharactersThenVehicles()
    {
        var groups = HexPalette.Units();
        var ids = groups.SelectMany(g => g.Entries).Select(e => e.Id).ToList();
        Assert.That(ids.Count, Is.EqualTo(ids.Distinct().Count()), "listed twice");
        CollectionAssert.AreEquivalent(HexUnitTypes.All.Keys, ids);
        Assert.That(groups.Select(g => g.Name).Take(2), Is.EqualTo(new[] { HexPalette.Characters, HexPalette.Vehicles }));
        foreach (var e in groups[0].Entries) Assert.That(HexUnitTypes.Create(e.Id).Create().Members.Count, Is.EqualTo(HexUnitScale.Characters), e.Id);
        foreach (var e in groups[1].Entries) Assert.That(HexUnitTypes.Create(e.Id).Create().Members.Count, Is.EqualTo(1), e.Id);
        Assert.That(groups[0].Entries.Select(e => e.Id), Does.Contain("palmach_squad"));
        Assert.That(groups[1].Entries.Select(e => e.Id), Does.Contain("willys_jeep"));
        Assert.That(groups.SelectMany(g => g.Entries).All(e => e.IsUnit));
    }

    [Test]
    public void TheSixFacingsPointAtTheSixNeighbours()
    {
        Assert.That(HexPalette.Facings.Select(f => f.degrees), Is.EqualTo(new[] { 30f, 90f, 150f, 210f, 270f, 330f }));
        Assert.That(HexPalette.Facings.Select(f => f.label), Is.EqualTo(new[] { "NE", "E", "SE", "SW", "W", "NW" }));
        // East is the neighbour (1, 0): the heading HexUnitMotion gives a unit walking there.
        var east = HexGeometry.Centre(1, 0, HexScale.HexM) - HexGeometry.Centre(0, 0, HexScale.HexM);
        Assert.That(Mathf.Atan2(east.x, east.z) * Mathf.Rad2Deg, Is.EqualTo(90f).Within(1e-3f));
    }

    /// <summary>A small per-hex board in a new scene, as the painter loads one.</summary>
    static GameObject PainterBoard()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var file = new HexBoardFile { id = "t", hexes = new[] { Hex(0, 0), Hex(1, 0, "pine_forest"), Hex(0, 1, ""), Hex(-1, 1, unit: "palmach_squad") } };
        return HexBoardBuilder.Build(new HexBoard(file), null, options: new HexBoardBuilder.Options { PerHex = true }).root;
    }

    static void Destroy(GameObject? root)
    {
        if (root == null) return;
        root.GetComponent<HexBoardAssets>().Teardown();
        Object.DestroyImmediate(root);
    }

    [Test]
    public void TheBrushWritesAnOverrideForEachPaintedHex()
    {
        var root = PainterBoard();
        try
        {
            var path = HexPainter.OverridesPath(dir, "t");
            var painted = HexPalette.Paint(root, path, new[] { (0, 0), (1, 0), (0, 1), (1, 0) }, "kibbutz");
            Assert.That(painted, Is.EqualTo(new[] { (0, 0), (1, 0), (0, 1) }), "a hex painted twice in a stroke is painted once");
            var saved = HexBoardOverrides.FromJson(File.ReadAllText(path));
            Assert.That(All(saved), Is.EqualTo(new[] { (0, 0, "prototype", "kibbutz"), (1, 0, "prototype", "kibbutz"), (0, 1, "prototype", "kibbutz") }));
            var state = root.GetComponent<HexBoardState>();
            var pieces = root.transform.Find(HexBoardBuilder.PiecesName);
            foreach (var hex in painted)
            {
                Assert.That(state.Board.Hexes[hex].prototype, Is.EqualTo("kibbutz"));
                Assert.That(state.Reports[hex].Prototype, Is.EqualTo("kibbutz"), "the hex was rebuilt");
                Assert.That(pieces.Find(HexBoardBuilder.HexName(hex.Item1, hex.Item2)), Is.Not.Null);
            }
        }
        finally { Destroy(root); }
    }

    [Test]
    public void PlacingAUnitWritesItsUnitAndFacingAndClearingRemovesIt()
    {
        var root = PainterBoard();
        try
        {
            var path = HexPainter.OverridesPath(dir, "t");
            HexPalette.PlaceUnit(root, path, 0, 0, "willys_jeep", 120f);
            var saved = HexBoardOverrides.FromJson(File.ReadAllText(path));
            Assert.That(All(saved), Is.EqualTo(new[] { (0, 0, "unit", "willys_jeep"), (0, 0, "facing", "120") }));
            var h = root.GetComponent<HexBoardState>().Board.Hexes[(0, 0)];
            Assert.That((h.unit, h.facing), Is.EqualTo(("willys_jeep", 120f)));
            var unit = root.transform.Find(HexBoardBuilder.UnitsName).Find(HexBoardBuilder.UnitName(0, 0));
            Assert.That(unit.childCount, Is.EqualTo(1));
            Assert.That(unit.GetChild(0).localEulerAngles.y, Is.EqualTo(Mathf.Repeat(120f - 90f, 360f)).Within(0.01f), "the jeep's -90 model turn on top of the facing");

            HexPalette.Face(root, path, 0, 0, 270f);
            Assert.That(HexBoardOverrides.FromJson(File.ReadAllText(path)).List("facing").Single().value, Is.EqualTo("270"));
            HexPalette.ClearUnit(root, path, 0, 0);
            Assert.That(root.transform.Find(HexBoardBuilder.UnitsName).Find(HexBoardBuilder.UnitName(0, 0)), Is.Null);
            Assert.That(HexBoardOverrides.FromJson(File.ReadAllText(path)).List("unit").Single().value, Is.EqualTo(""), "cleared stays cleared after regenerating");
            Assert.Throws<ArgumentException>(() => HexPalette.PlaceUnit(root, path, 0, 0, "no_such_unit", 0f));
        }
        finally { Destroy(root); }
    }

    // --- The time-of-day and under-attack switches (previews: undone, never written) ---

    [Test]
    public void TheTimeOfDayIsUndoneBeforeTheNextPresetAndOnClear()
    {
        var log = new List<string>();
        Action Apply(HexTime t) { log.Add("apply " + HexTimeOfDay.Id(t)); return () => log.Add("undo " + HexTimeOfDay.Id(t)); }
        var switches = new HexPainterSwitches();
        switches.SetTime(HexTime.Dusk, Apply);
        switches.SetTime(HexTime.Night, Apply);
        Assert.That(switches.Time, Is.EqualTo(HexTime.Night));
        // A painter edit rebuilds a hex: night's lit windows are made again for it.
        switches.Rebuilt(0, 0);
        Assert.That(switches.Time, Is.EqualTo(HexTime.Night));
        switches.Clear();
        switches.Clear();
        switches.Rebuilt(0, 0);   // day: nothing to apply again
        Assert.That(log, Is.EqualTo(new[] { "apply dusk", "undo dusk", "apply night", "undo night", "apply night", "undo night" }));
        Assert.That(switches.Time, Is.EqualTo(HexTime.Day));
    }

    [Test]
    public void UnderAttackSurvivesARebuildAndIsHiddenOnClearNeverWritten()
    {
        var root = PainterBoard();
        try
        {
            var hidden = new List<(int, int)>();
            var switches = new HexPainterSwitches(hide: (b, q, r) => { hidden.Add((q, r)); return HexUnderAttack.Hide(b, q, r); });
            var state = root.GetComponent<HexBoardState>();
            var path = HexPainter.OverridesPath(dir, "t");
            switches.SetAttack(root.transform, state.Surface, 0, 0, true);
            switches.SetAttack(root.transform, state.Surface, 1, 0, true);
            HexPainter.Change(root, path, 0, 0, "prototype", "kibbutz");
            switches.Rebuilt(0, 0);
            Assert.That(HexUnderAttack.IsShown(root.transform, 0, 0), Is.True, "after the hex's rebuild");
            switches.SetAttack(root.transform, state.Surface, 1, 0, false);
            Assert.That(HexUnderAttack.IsShown(root.transform, 1, 0), Is.False);
            switches.Clear();
            Assert.That(hidden, Is.EqualTo(new[] { (1, 0), (0, 0) }));
            Assert.That(HexUnderAttack.IsShown(root.transform, 0, 0), Is.False);
            Assert.That(switches.Attacked, Is.Empty);
            Assert.That(File.ReadAllText(path), Does.Not.Contain("attack"));
            Assert.That(All(HexBoardOverrides.FromJson(File.ReadAllText(path))), Is.EqualTo(new[] { (0, 0, "prototype", "kibbutz") }));
        }
        finally { Destroy(root); }
    }

    [Test]
    public void TheWindowUndoesTheSwitchesOnUnloadAndOnClose()
    {
        var root = PainterBoard();
        var window = ScriptableObject.CreateInstance<HexPainterWindow>();
        try
        {
            var log = new List<string>();
            window.Switches = new HexPainterSwitches(hide: (b, q, r) => { log.Add($"hide {q},{r}"); return HexUnderAttack.Hide(b, q, r); });
            window.Switches.SetTime(HexTime.Night, t => () => log.Add("undo night"));
            window.Switches.SetAttack(root.transform, root.GetComponent<HexBoardState>().Surface, 0, 0, true);
            window.UnloadAll();
            Assert.That(log, Is.EqualTo(new[] { "hide 0,0", "undo night" }), "the previews are undone while the board still stands");
            Assert.That(root == null, Is.True, "the board is unloaded");

            log.Clear();
            window.Switches.SetTime(HexTime.Dusk, t => () => log.Add("undo dusk"));
            Object.DestroyImmediate(window);   // closing the window (and a script reload) runs OnDisable
            Assert.That(log, Is.EqualTo(new[] { "undo dusk" }));
        }
        finally
        {
            if (window != null) Object.DestroyImmediate(window);
            Destroy(root);
        }
    }

    // --- Thumbnails and the command line ---

    [Test]
    public void AMissingThumbnailFallsBackToText()
    {
        var tile = new HexPaletteEntry { Id = "kibbutz" };
        var unit = new HexPaletteEntry { Id = "palmach_squad", IsUnit = true };
        Assert.That(Path.GetFileName(HexPalette.ThumbPath(dir, tile)), Is.EqualTo("kibbutz.png"));
        Assert.That(Path.GetFileName(HexPalette.ThumbPath(dir, unit)), Is.EqualTo("unit-palmach_squad.png"));
        Assert.That(HexPalette.LoadThumb(dir, tile), Is.Null);
        var png = new Texture2D(4, 4); File.WriteAllBytes(HexPalette.ThumbPath(dir, unit), png.EncodeToPNG()); Object.DestroyImmediate(png);
        var loaded = HexPalette.LoadThumb(dir, unit);
        Assert.That(loaded!.width, Is.EqualTo(4));
        Object.DestroyImmediate(loaded);
    }

    [Test]
    public void AThumbnailBoardIsTheEntryRingedByPlainGround()
    {
        var board = HexPaletteThumbs.ThumbBoard(new HexPaletteEntry { Id = "judean_highlands" });
        Assert.That(board.Hexes.Count, Is.EqualTo(7));
        var c = board.Hexes[(0, 0)];
        Assert.That((c.prototype, c.terrain, c.unit), Is.EqualTo(("judean_highlands", "mountain", "")));
        Assert.That(board.Hexes.Values.Count(h => h.prototype == "" && h.unit == ""), Is.EqualTo(6));
        var u = HexPaletteThumbs.ThumbBoard(new HexPaletteEntry { Id = "palmach_squad", IsUnit = true }).Hexes[(0, 0)];
        Assert.That((u.prototype, u.unit), Is.EqualTo(("", "palmach_squad")));
    }

    [Test]
    public void TheCaptureReadsAnOperationList()
    {
        var ops = HexPainterCapture.ParseOps("prototype=kibbutz; unit=palmach_squad@120;time=dusk;attack");
        Assert.That(ops.Select(o => o.ToString()), Is.EqualTo(new[] { "prototype=kibbutz", "unit=palmach_squad@120", "time=dusk", "attack" }));
        Assert.That(ops.Select((o, i) => o.Folder(i + 1)), Is.EqualTo(new[] { "1-prototype-kibbutz", "2-unit-palmach_squad-120", "3-time-dusk", "4-attack" }));
        Assert.Throws<ArgumentException>(() => HexPainterCapture.ParseOps("time=noon"));
        Assert.Throws<ArgumentException>(() => HexPainterCapture.ParseOps("prototype=no_such_tile"));
        Assert.Throws<ArgumentException>(() => HexPainterCapture.ParseOps("terrain=water"));
    }

    // --- The environment's region species ---

    static HexBoard EnvBoard(BoardRegion[] regions, string region, string terrain = "flat")
    {
        var hexes = new List<BoardHex>();
        for (int q = -2; q <= 2; q++)
            for (int r = -2; r <= 2; r++)
                if (HexGeometry.Distance(0, 0, q, r) <= 2) hexes.Add(new BoardHex { q = q, r = r, terrain = terrain, env = true, region = region });
        return new HexBoard(new HexBoardFile { id = "env", hexes = hexes.ToArray(), regions = regions });
    }

    [Test]
    public void EnvironmentPlantsTheRegionsSpecies()
    {
        var desert = new BoardRegion
        {
            id = "south", density = 2f, flat = new[] { new BoardSpecies { model = "sandbox:acacia_lite", weight = 1f } },
            rock = "sandbox:desert_rock*", rockChance = 1f, rocksOnFlat = true,
        };
        var board = EnvBoard(new[] { desert }, "south");
        var models = HexEnvironment.Dress(board, new FlatSurface()).Select(p => p.Model).Distinct().OrderBy(m => m).ToArray();
        Assert.That(models, Is.EqualTo(new[] { "sandbox:acacia_lite", "sandbox:desert_rock*" }));
    }

    [Test]
    public void DensityScalesTheRegionsTrees()
    {
        int Count(float density) => HexEnvironment.Dress(EnvBoard(new[] { new BoardRegion { id = "x", density = density } }, "x"), new FlatSurface())
            .Count(p => p.StandIn != StandIn.Rock);
        Assert.That(Count(0.3f), Is.LessThan(Count(1f)));
        Assert.That(Count(1f), Is.LessThan(Count(2f)));
    }

    [TestCase("flat")]
    [TestCase("hills")]
    public void AHexWithNoRegionPlantsWhatTheEnvironmentDidBeforeRegions(string terrain)
    {
        // An unknown region falls back to the built-in mix too.
        foreach (var region in new[] { "", "nowhere" })
        {
            var board = EnvBoard(Array.Empty<BoardRegion>(), region, terrain);
            var now = HexEnvironment.Dress(board, new FlatSurface()).Select(p => (p.Model, p.StandIn, p.Matrix)).ToList();
            var before = LegacyDress(board, new FlatSurface()).Select(p => (p.Model, p.StandIn, p.Matrix)).ToList();
            Assert.That(now.Count, Is.GreaterThan(0));
            CollectionAssert.AreEqual(before, now, $"{terrain}, region '{region}'");
        }
    }

    /// <summary>HexEnvironment.Dress as it was before regions (hard-coded olive, carob, oak, pine and limestone).</summary>
    static IEnumerable<HexPlacement> LegacyDress(HexBoard board, IHexSurface surface, int seed = 1948)
    {
        const float GridM = 820f, WoodScaleM = 7000f, DetailScaleM = 1900f;
        foreach (var h in board.Hexes.Values)
        {
            if (!h.env || HexBoard.TerrainOf(h) == HexTerrain.Water) continue;
            var ctx = new HexTileContext(h.q, h.r, surface, seed ^ 0x5eed, HexScale.HexM, insetShare: 0f);
            var c = ctx.Centre;
            bool hills = HexBoard.TerrainOf(h) != HexTerrain.Flat;
            int n = Mathf.CeilToInt(HexScale.HexM / GridM);
            for (int i = -n; i <= n; i++)
                for (int j = -n; j <= n; j++)
                {
                    float gx = Mathf.Round(c.x / GridM) * GridM + i * GridM, gz = Mathf.Round(c.z / GridM) * GridM + j * GridM;
                    uint hsh = unchecked((uint)(Mathf.FloorToInt(gx) * 73856093 ^ Mathf.FloorToInt(gz) * 19349663 ^ seed));
                    float r1 = (hsh & 0xFFFF) / 65535f, r2 = ((hsh >> 16) & 0xFFFF) / 65535f;
                    var p = new Vector3(gx + (r1 - 0.5f) * GridM * 0.8f, 0f, gz + (r2 - 0.5f) * GridM * 0.8f);
                    if (!HexGeometry.InHex(p, c, HexScale.HexM)) continue;
                    float wood = Mathf.PerlinNoise(p.x / WoodScaleM + 3.3f, p.z / WoodScaleM + 7.7f);
                    float detail = Mathf.PerlinNoise(p.x / DetailScaleM + 1.1f, p.z / DetailScaleM + 2.2f);
                    float density = hills ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.48f, 0.6f, wood)) : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.56f, 0.68f, wood)) * 0.85f;
                    density = Mathf.Max(density * (0.6f + 0.6f * detail), 0.035f);
                    if (r1 * 0.7f + r2 * 0.3f > density) continue;
                    string model = hills ? (detail > 0.55f ? "sandbox:aleppo_pine_lite" : "sandbox:tabor_oak_lite") : (detail > 0.6f ? "sandbox:carob_lite" : "sandbox:olive_lite");
                    ctx.PlaceByHeight(model, p, r1 * 360f, (0.11f + 0.04f * r2) * HexScale.HexM, hills && detail > 0.55f ? StandIn.Pine : StandIn.Tree, null, 0.3f, 40f);
                }
            if (hills)
                for (int k = 0; k < 2; k++)
                    if (ctx.Rng.NextDouble() < 0.6)
                        ctx.PlaceByWidth("sandbox:limestone*", ctx.RandomPoint(), ctx.Range(0f, 360f), ctx.Range(0.06f, 0.1f) * HexScale.HexM, StandIn.Rock, null, 60f);
            foreach (var placement in ctx.Placements) yield return placement;
        }
    }
}
