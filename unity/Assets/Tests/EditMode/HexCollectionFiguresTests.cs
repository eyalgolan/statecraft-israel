#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using Statecraft.HexTiles;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class HexCollectionFiguresTests
{
    const string Scout = "proto:figure-palmach-scout-c12f-1948-v2";
    readonly List<(string id, CollectionDetail detail, Action<GameObject?> done)> pending = new();
    readonly List<GameObject> objects = new();

    [SetUp] public void SetUp()
    {
        HexModels.Clear(); HexModels.DrawCollectionFigures = true;
        HexCollectionFigures.Source = (id, detail, done) => pending.Add((id, detail, done));
    }
    [TearDown] public void TearDown()
    {
        HexCollectionFigures.Source = null;
        foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
        objects.Clear(); pending.Clear(); HexModels.DrawCollectionFigures = true; HexModels.Clear();
    }
    GameObject Root() { var go = new GameObject("hex test root"); objects.Add(go); return go; }
    GameObject Model()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "streamed-model";
        Object.DestroyImmediate(go.GetComponent<Collider>()); objects.Add(go); return go;
    }

    [Test] public void EveryAliasUsesTheCatalogDetailAndRequestsOnce()
    {
        int changes = 0;
        using var models = new HexCollectionFigures(() => changes++);
        foreach (var (id, figure, level) in HexModels.FigureAliases)
        {
            Assert.That(models.Prefab(id), Is.EqualTo(Resources.Load<GameObject>(HexModels.PathOf(id))), "fallback while pending");
            models.Prefab(id);
            var request = pending.Last();
            Assert.That(request.id, Is.EqualTo(figure));
            Assert.That(request.detail, Is.EqualTo(level == "far" ? CollectionDetail.Far : CollectionDetail.Near));
            Assert.That(CollectionCatalog.BundleOf(figure), Is.Not.Null);
        }
        Assert.That(pending.Count, Is.EqualTo(HexModels.FigureAliases.Length));
        var prefab = Model(); pending[0].done(prefab);
        Assert.That(models.Prefab(Scout), Is.SameAs(prefab));
        Assert.That(changes, Is.EqualTo(1));
    }

    [Test] public void FailureStaysOnFallbackAndDoesNotRequestEveryFrame()
    {
        using var models = new HexCollectionFigures(() => Assert.Fail("failure does not need a visual rebuild"));
        var before = models.Prefab(Scout); pending[0].done(null);
        for (int i = 0; i < 10; i++) Assert.That(models.Prefab(Scout), Is.EqualTo(before));
        Assert.That(pending.Count, Is.EqualTo(1));
    }

    [Test] public void DisposedAdapterIgnoresLateCallbackAndReloadUsesANewOwner()
    {
        int changes = 0;
        var first = new HexCollectionFigures(() => changes++); first.Prefab(Scout); first.Dispose(); first.Dispose();
        pending[0].done(Model());
        Assert.That(changes, Is.Zero);
        using var second = new HexCollectionFigures(() => changes++); second.Prefab(Scout);
        pending[1].done(Model()); Assert.That(changes, Is.EqualTo(1));
    }

    [Test] public void DisabledAndUnrelatedModelsDoNotStream()
    {
        using var models = new HexCollectionFigures(() => {});
        models.Prefab("missing:unrelated");
        HexModels.DrawCollectionFigures = false; models.Prefab(Scout);
        Assert.That(pending, Is.Empty);
    }

    [Test] public void UnitArrivalReplacesTheCapsulesAtTheLatestPoseWithoutColliders()
    {
        var root = Root(); root.AddComponent<HexBoardAssets>();
        var motion = new HexUnitMotion(HexUnitTypes.Squad("probe", Scout), new[] { (0, 0), (1, 0) }, new FlatSurface(), root.transform);
        motion.ModelYaw = 17f;
        motion.Pose(motion.Length * .7f, true);
        var before = motion.Objects.ToArray(); var positions = before.Select(g => g.transform.localPosition).ToArray();
        Assert.That(pending.Count, Is.EqualTo(1), "eight members share one runtime request");
        Assert.That(before.All(go => go.GetComponentInChildren<SkinnedMeshRenderer>() == null), Is.True, "pending figures use fallback, never Editor aliases");
        var rotations = before.Select(go => go.transform.localRotation).ToArray();
        pending[0].done(Model());
        root.GetComponentInChildren<HexUnitFigureRefresh>().RefreshNow();
        for (int i = 0; i < before.Length; i++)
        {
            Assert.That(motion.Objects[i], Is.Not.SameAs(before[i]));
            Assert.That(motion.Objects[i].transform.localPosition, Is.EqualTo(positions[i]));
            Assert.That(motion.Objects[i].transform.localRotation, Is.EqualTo(rotations[i]));
            Assert.That(motion.Objects[i].transform.localScale.y, Is.EqualTo(HexUnitScale.FigureHeight * HexScale.HexM).Within(.01f));
        }
        var refreshed = motion.Objects.ToArray();
        root.GetComponentInChildren<HexUnitFigureRefresh>().RefreshNow();
        Assert.That(motion.Objects, Is.EqualTo(refreshed), "second refresh is idempotent");
        Assert.That(root.GetComponentsInChildren<Collider>(), Is.Empty);
    }

    [Test] public void BoardRefreshKeepsUnrelatedPiecesAndReleasesOldGeneratedMeshes()
    {
        var root = Root(); var assets = root.AddComponent<HexBoardAssets>();
        HexPlacement Place(string id, StandIn shape, float x) => new(id, shape, Fit.Height, Color.white,
            Matrix4x4.TRS(new Vector3(x,0,0), Quaternion.identity, Vector3.one * 20), 0, 0);
        var layer = HexBoardRenderer.Emit(root.transform, new[] { Place(Scout, StandIn.Figure, 0), Place("", StandIn.Box, 100) }, assets);
        var staticPiece = layer.GetComponentsInChildren<MeshFilter>().First(f => !f.transform.IsChildOf(layer.GetComponentInChildren<HexStreamedPlacements>().transform));
        var refresh = layer.GetComponentInChildren<HexStreamedPlacements>();
        Assert.That(refresh, Is.Not.Null, "aliased placements have a runtime refresh owner");
        var oldMesh = refresh.GetComponentInChildren<MeshFilter>().sharedMesh;
        var oldMaterial = refresh.GetComponentInChildren<Renderer>().sharedMaterial;
        pending[0].done(Model()); refresh.RefreshNow();
        Assert.That(staticPiece != null && staticPiece.sharedMesh != null, Is.True);
        Assert.That(oldMesh == null, Is.True, "generated fallback mesh is released");
        Assert.That(oldMaterial == null, Is.True, "generated fallback material is released");
        Assert.That(refresh.GetComponentsInChildren<Renderer>(), Has.Length.EqualTo(1));
        Assert.That(root.GetComponentsInChildren<Collider>(), Is.Empty);
    }

    [Test] public void BoardTeardownStopsPendingRefreshBeforeDeferredDestruction()
    {
        var root = Root(); var assets = root.AddComponent<HexBoardAssets>();
        var p = new HexPlacement(Scout, StandIn.Figure, Fit.Height, Color.white, Matrix4x4.identity, 0, 0);
        HexBoardRenderer.Emit(root.transform, new[] { p }, assets);
        var refresh = root.GetComponentInChildren<HexStreamedPlacements>();
        assets.Teardown(); pending[0].done(Model()); refresh.RefreshNow();
        Assert.That(root.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("streamed-model", StringComparison.Ordinal)), Is.False);
    }
    [Test] public void SynchronousArrivalIsAvailableImmediatelyAndDoesNotRequestAgain()
    {
        var prefab = Model(); int requests = 0;
        HexCollectionFigures.Source = (_, _, done) => { requests++; done(prefab); };
        using var models = new HexCollectionFigures(() => {});
        Assert.That(models.Prefab(Scout), Is.SameAs(prefab));
        Assert.That(models.Prefab(Scout), Is.SameAs(prefab));
        Assert.That(requests, Is.EqualTo(1));
    }

    [Test] public void UnitTeardownIgnoresLateArrival()
    {
        var root = Root(); var assets = root.AddComponent<HexBoardAssets>();
        var motion = new HexUnitMotion(HexUnitTypes.Squad("probe", Scout), new[] { (0, 0) }, new FlatSurface(), root.transform);
        var before = motion.Objects.ToArray();
        var owner = root.GetComponentInChildren<HexUnitFigureRefresh>();
        assets.Teardown(); pending[0].done(Model()); owner.RefreshNow();
        Assert.That(motion.Objects, Is.EqualTo(before));
    }

    [Test] public void StandaloneUnitOwnerAlsoCancelsOnTeardown()
    {
        var root = Root();
        var motion = new HexUnitMotion(HexUnitTypes.Squad("probe", Scout), new[] { (0, 0) }, new FlatSurface(), root.transform);
        var before = motion.Objects.ToArray();
        var owner = root.GetComponentInChildren<HexUnitFigureRefresh>();
        var assets = root.GetComponentInChildren<HexBoardAssets>();
        Assert.That(assets, Is.Not.Null);
        assets.Teardown(); pending[0].done(Model()); owner.RefreshNow();
        Assert.That(motion.Objects, Is.EqualTo(before));
    }

    [Test] public void UnitArrivalReleasesFallbackMaterialAndKeepsBorrowedPrefab()
    {
        var root = Root(); root.AddComponent<HexBoardAssets>();
        var motion = new HexUnitMotion(HexUnitTypes.Squad("probe", Scout), new[] { (0, 0) }, new FlatSurface(), root.transform);
        var fallback = motion.Objects[0].GetComponent<Renderer>().sharedMaterial;
        var prefab = Model(); var borrowed = prefab.GetComponent<Renderer>().sharedMaterial;
        pending[0].done(prefab); root.GetComponentInChildren<HexUnitFigureRefresh>().RefreshNow();
        Assert.That(fallback == null, Is.True, "the replaced capsule material is no longer needed");
        Object.DestroyImmediate(root);
        Assert.That(prefab != null && borrowed != null, Is.True, "the Collection owns the source assets");
    }

    [Test] public void RemovingAStreamedUnitReleasesFallbackBeforeTheBoardIsDestroyed()
    {
        var root = Root(); root.AddComponent<HexBoardAssets>();
        var motion = new HexUnitMotion(HexUnitTypes.Squad("probe", Scout), new[] { (0, 0) }, new FlatSurface(), root.transform);
        var fallback = motion.Objects[0].GetComponent<Renderer>().sharedMaterial;
        Object.DestroyImmediate(root.GetComponentInChildren<HexUnitFigureRefresh>().gameObject);
        Assert.That(root != null, Is.True);
        Assert.That(fallback == null, Is.True, "a removed unit cannot retain its fallback until board teardown");
        pending[0].done(Model());
        Assert.That(root!.GetComponentsInChildren<Renderer>(), Is.Empty);
    }

}
