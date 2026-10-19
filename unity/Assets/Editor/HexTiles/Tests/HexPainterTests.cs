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

/// <summary>The map builder's hand edits (HexBoardOverrides), the Hex Painter's write-back and one-hex rebuild, and
/// the environment's region species (docs/hex-tiles/README.md, "Laying out the whole map").</summary>
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
