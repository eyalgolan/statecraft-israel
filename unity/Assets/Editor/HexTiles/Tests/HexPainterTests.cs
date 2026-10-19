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
    [Test]
    public void RebuildingAHexReleasesOnlyItsOwnedUnitAssets()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var board = Board(Hex(0, 0, prototype: ""), Hex(1, 0, prototype: ""));
        var (root, _) = HexBoardBuilder.Build(board, null, options: new HexBoardBuilder.Options { PerHex = true });
        string sharedPath = "Assets/__HexPainterShared-" + Guid.NewGuid().ToString("N") + ".mat";
        Material? createdMaterial = null;
        try
        {
            var state = root.GetComponent<HexBoardState>();
            var owner = root.GetComponent<HexBoardAssets>();
            var units = root.transform.Find(HexBoardBuilder.UnitsName);
            var oldHolder = new GameObject(HexBoardBuilder.UnitName(0, 0));
            oldHolder.transform.SetParent(units, false);
            // Empty model is guaranteed missing by HexModels.Prefab's public contract, independently of local assets.
            Assert.That(HexModels.Prefab(""), Is.Null);
            var oldUnit = new HexUnit { Members = new List<HexUnitMember> { new HexUnitMember { Model = "", StandIn = StandIn.Box } } };
            var motion = new HexUnitMotion(oldUnit, new[] { (0, 0) }, state.Surface, oldHolder.transform);
            motion.Pose(0f, false);
            var oldMaterial = motion.Objects.Single().GetComponent<MeshRenderer>().sharedMaterial;
            Assert.That(oldHolder.GetComponentInParent<HexBoardAssets>(), Is.SameAs(owner));
            Assert.That(oldMaterial != null, Is.True, "the old fallback must really exist");
            Assert.That(AssetDatabase.Contains(oldMaterial), Is.False, "this is a generated stand-in, not an imported shared asset");
            int oldId = oldMaterial!.GetInstanceID();
            var primitiveMesh = motion.Objects.Single().GetComponent<MeshFilter>().sharedMesh;
            Assert.That(primitiveMesh != null, Is.True, "the primitive must have a shared mesh");
            var untouchedHolder = new GameObject(HexBoardBuilder.UnitName(1, 0));
            untouchedHolder.transform.SetParent(units, false);
            var untouched = new HexUnitMotion(oldUnit, new[] { (1, 0) }, state.Surface, untouchedHolder.transform);
            untouched.Pose(0f, false);
            var untouchedMaterial = untouched.Objects.Single().GetComponent<MeshRenderer>().sharedMaterial;
            Assert.That(untouchedMaterial != null, Is.True);
            Assert.That(untouchedMaterial, Is.Not.SameAs(oldMaterial), "each unit owns a distinct fallback");
            Assert.That(untouched.Objects.Single().GetComponent<MeshFilter>().sharedMesh, Is.SameAs(primitiveMesh));
            createdMaterial = HexMaterials.New(HexMaterials.Lit);
            AssetDatabase.CreateAsset(createdMaterial, sharedPath);
            var importedMaterial = AssetDatabase.LoadAssetAtPath<Material>(sharedPath)
                ?? throw new InvalidOperationException("the created shared material asset must load before the edit");
            Assert.That(AssetDatabase.Contains(importedMaterial), Is.True, "the shared-material control must be a real asset");
            int importedId = importedMaterial.GetInstanceID();
            string importedPath = AssetDatabase.GetAssetPath(importedMaterial);
            string importedGuid = AssetDatabase.AssetPathToGUID(sharedPath, AssetPathToGUIDOptions.OnlyExistingAssets);
            Assert.That(importedPath, Is.EqualTo(sharedPath));
            Assert.That(importedGuid, Is.Not.Empty);
            var loadedBefore = AssetDatabase.LoadAssetAtPath<Material>(sharedPath);
            Assert.That(loadedBefore != null, Is.True);
            UnityEngine.Debug.Log($"PAINTERASSET before createdId={createdMaterial.GetInstanceID()} originalId={importedId} loadedId={loadedBefore!.GetInstanceID()} guid={importedGuid} path={importedPath} unityEqual={loadedBefore == importedMaterial} managedEqual={ReferenceEquals(loadedBefore, importedMaterial)}");
            var importedRenderer = new GameObject("shared-imported-material", typeof(MeshRenderer));
            importedRenderer.transform.SetParent(oldHolder.transform, false);
            importedRenderer.GetComponent<MeshRenderer>().sharedMaterial = importedMaterial;

            // Change uses the real write-back + RebuildHex path; no type registry/private ownership seam is replaced.
            HexPainter.Change(root, HexPainter.OverridesPath(dir, "t"), 0, 0, "unit", "sandwich_truck");
            Assert.That(oldHolder == null, Is.True, "the old unit's hierarchy must be removed");
            var current = units.Find(HexBoardBuilder.UnitName(0, 0));
            Assert.That(current != null, Is.True, "the replacement unit must exist");
            var currentMaterials = current!.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).ToArray();
            Assert.That(currentMaterials.Length, Is.GreaterThan(0));
            Assert.That(currentMaterials.All(m => m != null), Is.True, "the replacement must keep valid materials");
            UnityEngine.Debug.Log($"PAINTERMATERIAL oldId={oldId} oldAlive={oldMaterial != null} currentMaterials={currentMaterials.Length} currentAlive={currentMaterials.All(m => m != null)}");
            Assert.That(oldMaterial == null, Is.True, "removed unit fallback material must be released, not retained by the board owner");
            Assert.That(primitiveMesh != null, Is.True, "a shared primitive mesh must survive the removed unit");
            Assert.That(untouchedHolder != null && untouchedMaterial != null, Is.True, "another unit and its material must survive");
            Assert.That(untouched.Objects.Single().GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(untouchedMaterial));
            Assert.That(importedMaterial != null && AssetDatabase.Contains(importedMaterial), Is.True, "imported shared materials must survive");
            var loadedAfter = AssetDatabase.LoadAssetAtPath<Material>(sharedPath);
            Assert.That(loadedAfter != null, Is.True, "the imported material must still load");
            string afterGuid = AssetDatabase.AssetPathToGUID(sharedPath, AssetPathToGUIDOptions.OnlyExistingAssets);
            string afterPath = AssetDatabase.GetAssetPath(loadedAfter);
            UnityEngine.Debug.Log($"PAINTERASSET after originalId={importedMaterial!.GetInstanceID()} loadedId={loadedAfter!.GetInstanceID()} guid={afterGuid} path={afterPath} unityEqual={loadedAfter == importedMaterial} managedEqual={ReferenceEquals(loadedAfter, importedMaterial)}");
            Assert.That(importedMaterial!.GetInstanceID(), Is.EqualTo(importedId), "the original native material must survive");
            Assert.That(afterGuid, Is.EqualTo(importedGuid));
            Assert.That(afterPath, Is.EqualTo(importedPath));
            Assert.That(loadedAfter == importedMaterial, Is.True, "the path must resolve to the same native Unity material");
        }
        finally
        {
            root.GetComponent<HexBoardAssets>().Teardown();
            Object.DestroyImmediate(root);
            AssetDatabase.DeleteAsset(sharedPath);
            if (createdMaterial != null) Object.DestroyImmediate(createdMaterial);
        }
    }

    [Test]
    public void PainterPreviewHierarchyIsExcludedFromAnOrdinarySceneSave()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string token = Guid.NewGuid().ToString("N");
        string id = "__PainterSave-" + token;
        string jsonPath = HexPainter.BoardsFolder + "/" + id + ".json";
        string scenePath = "Assets/__PainterSave-" + token + ".unity";
        string previewName = "PainterPreview-" + token;
        string markerName = "PainterOrdinaryMarker-" + token;
        GameObject? preview = null, secondPreview = null, ordinaryBoard = null;
        try
        {
            var file = new HexBoardFile { id = id, hexes = new[] { Hex(0, 0, prototype: "", unit: "sandwich_truck") } };
            File.WriteAllText(jsonPath, JsonUtility.ToJson(file));
            AssetDatabase.ImportAsset(jsonPath, ImportAssetOptions.ForceSynchronousImport);
            // This is the same public load path the window calls, with no synthetic hide flags or private seam.
            preview = HexPainter.Load(id);
            Assert.That(preview != null, Is.True);
            secondPreview = HexPainter.Load(id);
            ordinaryBoard = new GameObject("PainterOrdinaryBoard-" + token);
            var ordinaryState = ordinaryBoard.AddComponent<HexBoardState>();
            var previewObjects = preview!.GetComponentsInChildren<Transform>(true)
                .Concat(secondPreview.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
            Assert.That(previewObjects.Length, Is.GreaterThan(1), "the real preview must include descendants");
            for (int i = 0; i < previewObjects.Length; i++) previewObjects[i].name = previewName + "-" + i.ToString("D4");
            string[] previewNames = previewObjects.Select(go => go.name).ToArray();
            var state = preview.GetComponent<HexBoardState>();
            var expectedStates = new[] { state, secondPreview.GetComponent<HexBoardState>(), ordinaryState };
            var loaded = HexPainter.LoadedBoards();
            Assert.That(loaded.Select(s => s.GetInstanceID()), Is.EquivalentTo(expectedStates.Select(s => s.GetInstanceID())),
                "the window snapshot must contain both real previews and the ordinary scene board once each");
            Assert.That(loaded.Select(s => s.GetInstanceID()).Distinct().Count(), Is.EqualTo(loaded.Length));
            var localCentre = HexGeometry.Centre(0, 0, HexScale.HexM);
            localCentre.y = state.Surface.Height(localCentre);
            var ray = new Ray(preview.transform.TransformPoint(localCentre) + Vector3.up * 10000f, Vector3.down);
            Physics.SyncTransforms();
            Assert.That(Physics.RaycastAll(ray, 20000f).Any(h => h.collider.transform.IsChildOf(preview.transform)), Is.True,
                "preview terrain must remain in the physics scene after editor save flags");
            Assert.That(HexPainter.Pick(preview, ray), Is.EqualTo((0, 0)), "the public painter pick must keep working");
            Assert.That(preview.activeInHierarchy, Is.True);
            var marker = new GameObject(markerName);
            Assert.That(marker.hideFlags, Is.EqualTo(HideFlags.None));
            bool saved = EditorSceneManager.SaveScene(scene, scenePath);
            Assert.That(saved, Is.True, "the ordinary scene save must actually succeed");
            string yaml = File.ReadAllText(scenePath);
            Assert.That(yaml.Contains(markerName), Is.True, "positive control: ordinary scene objects must be serialized");
            string[] serializedPreviewNames = previewNames.Where(name => yaml.Contains(name)).ToArray();
            bool previewSerialized = serializedPreviewNames.Length > 0;
            string yamlHash;
            using (var hash = System.Security.Cryptography.SHA256.Create())
                yamlHash = BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(yaml))).Replace("-", "").ToLowerInvariant();
            UnityEngine.Debug.Log($"PAINTERSCENE saved={saved} previewFlags={preview.hideFlags} previewSerialized={previewSerialized} previewObjects={previewObjects.Length} serializedNames={string.Join(",", serializedPreviewNames)} markerSerialized={yaml.Contains(markerName)} yamlSHA256={yamlHash} path={scenePath}");
            Assert.That(previewSerialized, Is.False, "no painter preview root or descendant may be serialized by an ordinary scene save");
            foreach (var old in loaded)
            {
                old.GetComponent<HexBoardAssets>()?.Teardown();
                Object.DestroyImmediate(old.gameObject);
            }
            Assert.That(preview == null && secondPreview == null && ordinaryBoard == null, Is.True,
                "the materialized window snapshot must unload every discovered board");
            Assert.That(marker != null, Is.True, "unloading boards must leave unrelated scene objects alive");
        }
        finally
        {
            if (preview != null)
            {
                preview.GetComponent<HexBoardAssets>().Teardown();
                Object.DestroyImmediate(preview);
            }
            if (secondPreview != null)
            {
                secondPreview.GetComponent<HexBoardAssets>().Teardown();
                Object.DestroyImmediate(secondPreview);
            }
            if (ordinaryBoard != null) Object.DestroyImmediate(ordinaryBoard);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(scenePath);
            AssetDatabase.DeleteAsset(jsonPath);
        }
    }

}

// Real domain-reload regression: generated native ownership survives reload until actual preview teardown.
[Serializable]
public sealed class PainterDomainReloadTests
{
    const string PendingKey = "Statecraft.Tests.PainterDomainReload.Pending";
    static readonly string DomainNonce = Guid.NewGuid().ToString("N");
    [Serializable] sealed class Config { public string token = ""; }
    [Serializable] sealed class AssetRef
    {
        public int id; public string name = "", type = "";
    }
    [Serializable] sealed class Record
    {
        public string token = "", nonce = "", boardId = "", jsonPath = "", materialPath = "", materialGuid = "";
        public int previewId, stateId, markerId, sharedMeshId;
        public AssetRef createdControl = new AssetRef();
        public AssetRef[] owned = Array.Empty<AssetRef>();
    }
    [SerializeField] string evidence = ""; // TF1.6 restores serializable fixture fields after the actual domain reload.
    string Evidence
    {
        get
        {
            const string prefix = "statecraft-painter-domain-";
            var full = Path.GetFullPath(evidence);
            var name = Path.GetFileName(full);
            var parent = Path.GetDirectoryName(full);
            var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parent != temp || !name.StartsWith(prefix, StringComparison.Ordinal)
                || !Guid.TryParseExact(name.Substring(prefix.Length), "N", out _))
                throw new InvalidOperationException("invalid owned domain-reload evidence path");
            return full;
        }
    }
    string RecordPath => Path.Combine(Evidence, "before-reload.json");
    Config Settings => JsonUtility.FromJson<Config>(File.ReadAllText(Path.Combine(Evidence, "config.json")));
    Record Before => JsonUtility.FromJson<Record>(File.ReadAllText(RecordPath));
    static string Key(string token) => "Statecraft.PainterDomainProbe." + token;
    static UnityEngine.Object? Native(int id) => id == 0 ? null : Resources.FindObjectsOfTypeAll<UnityEngine.Object>()
        .FirstOrDefault(o => o != null && o.GetInstanceID() == id);
    static bool Alive(AssetRef a)
    {
        var o = Native(a.id);
        return o != null && o.name == a.name && o.GetType().FullName == a.type;
    }

    [UnityEngine.TestTools.UnitySetUp]
    public System.Collections.IEnumerator RequestAnActualDomainReload()
    {
        // A new fixture allocates its own checkpoint; TF restores only this field across the actual reload.
        if (string.IsNullOrEmpty(evidence))
        {
            var token = Guid.NewGuid().ToString("N");
            Assert.That(SessionState.GetString(PendingKey, ""), Is.Empty, "unfinished domain fixture must not be mistaken for a new reload");
            var path = Path.Combine(Path.GetTempPath(), "statecraft-painter-domain-" + token);
            Assert.That(Directory.Exists(path) || File.Exists(path), Is.False, "new fixture evidence path must be fresh");
            evidence = path;
            Directory.CreateDirectory(Evidence);
            SessionState.SetString(PendingKey, evidence);
            File.WriteAllText(Path.Combine(Evidence, "config.json"), JsonUtility.ToJson(new Config { token = token }));
        }
        if (!File.Exists(RecordPath))
        {
            var cfg = Settings;
            Assert.That(cfg.token, Does.Match("^[a-f0-9]{32}$"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rec = new Record { token = cfg.token, nonce = DomainNonce, boardId = "000Domain-" + cfg.token };
            rec.jsonPath = HexPainter.BoardsFolder + "/" + rec.boardId + ".json";
            rec.materialPath = "Assets/__DomainControl-" + cfg.token + ".mat";
            // Save the cleanup paths before any Unity object creation.
            File.WriteAllText(RecordPath, JsonUtility.ToJson(rec, true));
            var file = new HexBoardFile { id = rec.boardId, hexes = new[] { new BoardHex { q = 0, r = 0, prototype = "" } } };
            File.WriteAllText(rec.jsonPath, JsonUtility.ToJson(file));
            AssetDatabase.ImportAsset(rec.jsonPath, ImportAssetOptions.ForceSynchronousImport);
            var preview = HexPainter.Load(rec.boardId);
            preview.name = "DomainPreview-" + cfg.token;
            rec.previewId = preview.GetInstanceID();
            var state = preview.GetComponent<HexBoardState>() ?? throw new InvalidOperationException("loaded preview lacks HexBoardState");
            rec.stateId = state.GetInstanceID();
            File.WriteAllText(RecordPath, JsonUtility.ToJson(rec, true));
            var sea = preview.transform.Find("HexBoardSea");
            Assert.That(sea != null, Is.True);
            if (sea == null) throw new InvalidOperationException("loaded preview lacks sea");
            var holder = new GameObject("DomainFallback-" + cfg.token);
            holder.transform.SetParent(preview.transform, false);
            Assert.That(HexModels.Prefab(""), Is.Null);
            var motion = new HexUnitMotion(new HexUnit { Members = new List<HexUnitMember> {
                new HexUnitMember { Model = "", StandIn = StandIn.Box } } }, new[] { (0, 0) }, state.Surface, holder.transform);
            motion.Pose(0f, false);
            var fallback = motion.Objects.Single();
            rec.sharedMeshId = fallback.GetComponent<MeshFilter>().sharedMesh.GetInstanceID();
            // These four actual references are created and registered by BuildSea/BuildGround/HexUnitMotion.
            UnityEngine.Object[] owned = { sea.GetComponent<MeshFilter>().sharedMesh,
                sea.GetComponent<MeshRenderer>().sharedMaterial, state.Terrain,
                fallback.GetComponent<MeshRenderer>().sharedMaterial };
            rec.owned = owned.Select((o, i) =>
            {
                Assert.That(o != null && !EditorUtility.IsPersistent(o), Is.True);
                if (o == null) throw new InvalidOperationException("known generated asset is missing");
                o.name = "DomainOwned-" + cfg.token + "-" + i;
                return new AssetRef { id = o.GetInstanceID(), name = o.name, type = o.GetType().FullName ?? o.GetType().Name };
            }).ToArray();
            var marker = new GameObject("DomainOrdinary-" + cfg.token);
            rec.markerId = marker.GetInstanceID();
            Assert.That(marker.hideFlags, Is.EqualTo(HideFlags.None));
            var created = HexMaterials.New(HexMaterials.Lit);
            created.name = "DomainImported-" + cfg.token;
            rec.createdControl = new AssetRef { id = created.GetInstanceID(), name = created.name, type = created.GetType().FullName ?? created.GetType().Name };
            File.WriteAllText(RecordPath, JsonUtility.ToJson(rec, true));
            AssetDatabase.CreateAsset(created, rec.materialPath);
            rec.createdControl.name = created.name;
            File.WriteAllText(RecordPath, JsonUtility.ToJson(rec, true));
            var imported = AssetDatabase.LoadAssetAtPath<Material>(rec.materialPath);
            Assert.That(imported != null && AssetDatabase.Contains(imported), Is.True);
            marker.AddComponent<MeshRenderer>().sharedMaterial = imported;
            rec.materialGuid = AssetDatabase.AssetPathToGUID(rec.materialPath, AssetPathToGUIDOptions.OnlyExistingAssets);
            Assert.That(rec.materialGuid, Is.Not.Empty);
            File.WriteAllText(RecordPath, JsonUtility.ToJson(rec, true));
            SessionState.SetString(Key(rec.token), DomainNonce);
            UnityEngine.Debug.Log("DOMAINPROBE requesting actual reload " + JsonUtility.ToJson(rec));
            EditorUtility.RequestScriptReload();
            yield return new UnityEngine.TestTools.WaitForDomainReload();
        }
        Assert.That(SessionState.GetString(PendingKey, ""), Is.EqualTo(evidence), "fixture checkpoint must survive the actual reload");
        var before = Before;
        Assert.That(SessionState.GetString(Key(before.token), ""), Is.EqualTo(before.nonce));
        Assert.That(DomainNonce, Is.Not.EqualTo(before.nonce), "a real managed domain reload must change the static nonce");
        UnityEngine.Debug.Log($"DOMAINPROBE actualReload=True oldNonce={before.nonce} newNonce={DomainNonce}");
    }

    [Test]
    public void GeneratedAssetsAreReleasedWhenTheReloadedPreviewIsDestroyed()
    {
        var rec = Before;
        Assert.That(DomainNonce, Is.Not.EqualTo(rec.nonce));
        var preview = Native(rec.previewId) as GameObject;
        var state = Native(rec.stateId) as HexBoardState;
        bool previewWasAlive = preview != null;
        bool boardNull = state == null || state.Board == null;
        var beforeAlive = rec.owned.Where(Alive).Select(a => a.name).ToArray();
        var marker = Native(rec.markerId) as GameObject;
        Assert.That(marker != null && marker.name == "DomainOrdinary-" + rec.token, Is.True,
            "ordinary scene positive control must survive the domain-only reload");
        var imported = AssetDatabase.LoadAssetAtPath<Material>(rec.materialPath);
        Assert.That(imported != null && AssetDatabase.Contains(imported), Is.True);
        Assert.That(AssetDatabase.AssetPathToGUID(rec.materialPath, AssetPathToGUIDOptions.OnlyExistingAssets), Is.EqualTo(rec.materialGuid));
        bool sharedBefore = Native(rec.sharedMeshId) != null;
        UnityEngine.Debug.Log($"DOMAINPROBE afterReload previewAlive={preview != null} stateBoardNull={boardNull} ownedAlive={string.Join(",", beforeAlive)} sharedBeforeDestroy={sharedBefore}");
        // Board=null or a preview already removed by reload is recorded, not called a bug: the documented UX reloads it.
        if (preview != null)
        {
            Assert.That(preview.name, Is.EqualTo("DomainPreview-" + rec.token));
            Object.DestroyImmediate(preview); // actual HexBoardAssets.OnDestroy/Teardown, never a simulated owned-list clear
        }
        var retained = rec.owned.Where(Alive).Select(a => a.name).ToArray();
        TestContext.WriteLine("DOMAINLIFETIME " + JsonUtility.ToJson(new Observation {
            previewAliveAfterReload = previewWasAlive, stateBoardNull = boardNull, retained = retained,
            ownedAliveBeforeDestroy = beforeAlive, sharedBeforeDestroy = sharedBefore }));
        File.WriteAllText(Path.Combine(Evidence, "lifetime-observation.json"), JsonUtility.ToJson(new Observation {
            previewAliveAfterReload = previewWasAlive, stateBoardNull = boardNull, retained = retained,
            ownedAliveBeforeDestroy = beforeAlive, sharedBeforeDestroy = sharedBefore }, true));
        UnityEngine.Debug.Log($"DOMAINPROBE afterDestroy retained={string.Join(",", retained)} ordinaryAlive={marker != null} importedAlive={imported != null}");
        Assert.That(marker != null, Is.True);
        Assert.That(imported != null && AssetDatabase.Contains(imported), Is.True);
        Assert.That(AssetDatabase.AssetPathToGUID(rec.materialPath, AssetPathToGUIDOptions.OnlyExistingAssets), Is.EqualTo(rec.materialGuid));
        if (sharedBefore) Assert.That(Native(rec.sharedMeshId) != null, Is.True, "a shared primitive alive after reload must survive preview destruction");
        Assert.That(retained, Is.Empty, "generated native assets must be released by reloaded preview teardown");
    }
    [Serializable] sealed class Observation
    {
        public bool previewAliveAfterReload, stateBoardNull, sharedBeforeDestroy;
        public string[] retained = Array.Empty<string>(), ownedAliveBeforeDestroy = Array.Empty<string>();
    }

    [TearDown]
    public void RemoveOnlyThisDiagnosticsOwnedObjectsAndAssets()
    {
        if (string.IsNullOrEmpty(evidence)) return;
        var ownedDirectory = Evidence;
        try
        {
        if (!File.Exists(RecordPath)) return;
        var rec = Before;
        var preview = Native(rec.previewId) as GameObject;
        if (preview != null && preview.name == "DomainPreview-" + rec.token) Object.DestroyImmediate(preview);
        var marker = Native(rec.markerId) as GameObject;
        if (marker != null && marker.name == "DomainOrdinary-" + rec.token) Object.DestroyImmediate(marker);
        // Explicit cleanup after recording the result: only the exact named nonpersistent objects this fixture created.
        foreach (var a in rec.owned)
        {
            var o = Native(a.id);
            if (o != null && Alive(a) && !EditorUtility.IsPersistent(o)) Object.DestroyImmediate(o);
        }
        AssetDatabase.DeleteAsset(rec.materialPath);
        var created = Native(rec.createdControl.id);
        if (Alive(rec.createdControl) && created != null && !EditorUtility.IsPersistent(created)) Object.DestroyImmediate(created);
        AssetDatabase.DeleteAsset(rec.jsonPath);
        SessionState.EraseString(Key(rec.token));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        finally
        {
            foreach (var file in new[] { "config.json", "before-reload.json", "lifetime-observation.json" })
            {
                var path = Path.Combine(ownedDirectory, file);
                if (File.Exists(path)) File.Delete(path);
            }
            // Preserve unknown files instead of recursively deleting a directory after a failed fixture.
            if (Directory.Exists(ownedDirectory))
            {
                Assert.That(Directory.GetFileSystemEntries(ownedDirectory), Is.Empty, "unexpected domain fixture evidence must be preserved");
                Directory.Delete(ownedDirectory);
            }
            if (SessionState.GetString(PendingKey, "") == evidence) SessionState.EraseString(PendingKey);
            evidence = "";
        }
    }
}
