#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Regression R1: a roster token owns the strategic Far originals for its full lifetime. Camera residency still
/// controls Near only, so leaving the approved unit band retires close detail without leaving a drawn bare plinth.</summary>
public class UnitRosterResidencyTests
{
    const float StartViewM = 90000f;
    const float DisplacedViewM = 11800f;
    const int DeferredRetirementSteps = 3;
    const int NoOwners = 0;
    const int OneOwnerPerFarKey = 1;
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Rows => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));

    sealed class Lease : IDisposable
    {
        Action? release;
        internal Lease(Action release) { this.release = release; }
        public void Dispose() { var done = release; release = null; done?.Invoke(); }
    }

    sealed class DeferredRetirement
    {
        sealed class Batch
        {
            internal readonly IDisposable[] Owners;
            internal readonly Action Complete;
            internal int Steps = DeferredRetirementSteps;
            internal Batch(IReadOnlyList<IDisposable> owners, Action complete)
            { Owners = owners.ToArray(); Complete = complete; }
        }
        readonly Queue<Batch> batches = new Queue<Batch>();
        internal bool Pending => batches.Count != 0;
        internal void Retire(IReadOnlyList<Object> _, IReadOnlyList<IDisposable> owners,
            IReadOnlyList<(string id, CollectionDetail detail)> __, Action complete)
        {
            if (owners.Count == 0) { complete(); return; }
            batches.Enqueue(new Batch(owners, complete));
        }
        internal void Step()
        {
            Assert.IsNotEmpty(batches, "a retirement batch is waiting");
            var batch = batches.Peek();
            if (--batch.Steps != 0) return;
            batches.Dequeue();
            foreach (var owner in batch.Owners) owner.Dispose();
            batch.Complete();
        }
    }

    [SetUp]
    public void Fresh()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        UnitFigures.Source = null;
        UnitFigures.Forget();
    }

    [TearDown]
    public void Restore()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        UnitFigures.Source = null;
        UnitFigures.Forget();
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    static MapScene BuildMap()
    {
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        KitPopulation.Use(KitPopulation.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", KitPopulation.FileName))));
        map.Build();
        return map;
    }

    static GameState JuneState()
    {
        var state = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();
        state.Date = new DateTime(1948, 6, 15);
        return state;
    }

    static Camera TopDownCamera(float height, Vector3 focus)
    {
        var camera = new GameObject("TEST ONLY residency camera").AddComponent<Camera>();
        camera.transform.SetPositionAndRotation(new Vector3(focus.x, height, focus.z), Quaternion.Euler(90f, 0f, 0f));
        return camera;
    }

    static GameObject Prefab(string id, CollectionDetail detail) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail));

    static Dictionary<string, Action<GameObject?>> PendingSource()
    {
        var pending = new Dictionary<string, Action<GameObject?>>(StringComparer.Ordinal);
        UnitFigures.Source = (id, detail, done) =>
        {
            Assert.AreEqual(CollectionDetail.Far, detail, "no Near request before a close-detail switch");
            pending[id] = done;
        };
        return pending;
    }

    static void AnswerAll(Dictionary<string, Action<GameObject?>> pending)
    {
        while (pending.Count != 0)
        {
            var batch = pending.ToArray();
            pending.Clear();
            foreach (var (id, done) in batch) done(Prefab(id, CollectionDetail.Far));
        }
    }

    static Transform Tokens(UnitLayer layer) => layer.transform.Find("Tokens");

    static void AssertNoOwnedOriginals(string because)
    {
        Assert.AreEqual(NoOwners, UnitFigures.OwnedRefCount, because + ": model refcount");
        Assert.AreEqual(NoOwners, UnitFigures.OwnedDetailRefCount, because + ": detail refcount");
    }

    [Test]
    public void AnsweredDrawnHoldersReplaceEveryRosterGatePrefetchOwner()
    {
        var map = BuildMap();
        var pending = PendingSource();
        try
        {
            var layer = UnitLayer.Create(map, Rows, C.Hexes, still: true);
            layer.Show(C, JuneState());
            Assert.Greater(layer.FigurePrefetchCount, NoOwners, "foreign roster gates start with temporary owners");

            AnswerAll(pending);

            Assert.AreEqual(NoOwners, layer.FigurePrefetchCount, "every answered model transfers from prefetch to its drawn holders");
            var holders = Tokens(layer).GetComponentsInChildren<UnitFigureResidency>(true);
            Assert.IsNotEmpty(holders, "fixture has drawn figure holders");
            foreach (var holder in holders)
                Assert.AreEqual(OneOwnerPerFarKey, holder.ResidencySnapshot().ownedLeases,
                    $"{holder.transform.parent.name}/{holder.name}: one owner for its one retained (model, Far) key");
            Assert.AreEqual(holders.Length, UnitFigures.OwnedRefCount, "no extra model owner remains beside the drawn holders");
            Assert.AreEqual(holders.Length, UnitFigures.OwnedDetailRefCount, "no extra detail owner remains beside the drawn holders");

            Object.DestroyImmediate(map.gameObject);
            AssertNoOwnedOriginals("layer teardown releases all transferred owners");
        }
        finally
        {
            if (map != null) Object.DestroyImmediate(map.gameObject);
        }
    }

    [Test]
    public void InactiveDrawnHolderCannotRetireItsGateOrLoseTheRosteredToken()
    {
        var map = BuildMap();
        var pending = PendingSource();
        try
        {
            var layer = UnitLayer.Create(map, Rows, C.Hexes, still: true);
            var state = JuneState();
            layer.Show(C, state);
            string model = UnitFigures.LebaneseRegular;
            Assert.IsTrue(pending.TryGetValue(model, out var answer), "fixture has an unanswered foreign roster gate");
            layer.gameObject.SetActive(false);

            answer!(Prefab(model, CollectionDetail.Far));

            Assert.IsTrue(UnitFigures.Arrived(model), "the roster-gate model remains available during inactive dressing");
            Assert.Greater(layer.FigurePrefetchCount, NoOwners, "an inactive holder cannot retire the live gate owner early");
            Assert.IsNotNull(Tokens(layer).Find("lebanese_army"), "the rostered token is built under the inactive parent");
            Assert.IsFalse(layer.Show(C, state), "the unchanged roster does not rebuild");
            Assert.IsNotNull(Tokens(layer).Find("lebanese_army"), "the roster gate cannot flicker the shown token away");
            Assert.IsTrue(UnitFigures.Arrived(model), "a shown roster token never loses its side model during handoff");

            Object.DestroyImmediate(map.gameObject);
            AssertNoOwnedOriginals("inactive layer teardown releases retained gate and holder owners");
        }
        finally
        {
            if (map != null) Object.DestroyImmediate(map.gameObject);
        }
    }

    [Test]
    public void StartViewRetainsEveryOnMapNonIsraeliTokenAfterItsFakeSourceAnswers()
    {
        var map = BuildMap();
        var pending = PendingSource();
        var camera = TopDownCamera(StartViewM, Vector3.zero);
        try
        {
            var rows = Rows;
            var state = JuneState();
            var expectedSides = rows.Where(UnitRoster.OnMap).Where(u => u.Side != UnitData.Israel)
                .Select(u => u.Side).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();
            var candidates = UnitRoster.FromState(rows, C, state, _ => true, (_, start, _, _) => start)
                .Where(t => t.Unit.Side != UnitData.Israel).ToArray();
            CollectionAssert.AreEqual(expectedSides, candidates.Select(t => t.Unit.Side).Distinct()
                .OrderBy(s => s, StringComparer.Ordinal).ToArray(), "fixture date contains every on-map non-Israeli side");
            var expectedTokens = candidates.Select(t => t.Unit.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();

            var layer = UnitLayer.Create(map, rows, C.Hexes, still: true);
            layer.Show(C, state);
            foreach (var holder in layer.GetComponentsInChildren<UnitFigureResidency>(true)) holder.Choose(default, camera);
            Assert.AreEqual(StartViewM, TownModelLayer.ViewDistance(camera), 1f, "the verified 90 km start view");

            AnswerAll(pending);

            foreach (var holder in layer.GetComponentsInChildren<UnitFigureResidency>(true)) holder.Choose(default, camera);

            var actual = layer.Tokens.Where(t => t.Token.Unit.Side != UnitData.Israel).Select(t => t.Token.Unit.Id)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(expectedTokens, actual, "source answers restore every side's tokens even outside camera residency");
            var tokenRoot = layer.transform.Find("Tokens");
            foreach (var token in layer.Tokens.Where(t => t.Token.Unit.Side != UnitData.Israel))
            {
                var drawn = tokenRoot.Find(token.Token.Unit.Id);
                Assert.IsNotNull(drawn, $"{token.Token.Unit.Id}: roster token is drawn");
                var holders = drawn.GetComponentsInChildren<UnitFigureResidency>(true);
                Assert.IsNotEmpty(holders, $"{token.Token.Unit.Id}: drawn token has figure holders");
                foreach (var holder in holders)
                {
                    Assert.IsFalse(holder.CameraResident, $"{token.Token.Unit.Id}/{holder.name}: 90 km is outside camera residency");
                    Assert.IsNotNull(holder.transform.Find(UnitFigures.ModelName),
                        $"{token.Token.Unit.Id}/{holder.name}: retained Far model dresses the out-of-residency holder");
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(map.gameObject);
        }
    }

    [Test]
    public void DisplacedFocusNeverLeavesAFigureHolderOnABarePlinth()
    {
        var map = BuildMap();
        var pending = PendingSource();
        Camera? camera = null;
        try
        {
            var layer = UnitLayer.Create(map, Rows, C.Hexes, still: true);
            layer.Show(C, JuneState());
            AnswerAll(pending);
            Assert.IsNotEmpty(layer.Tokens.Where(t => t.Token.Unit.Side != UnitData.Israel), "fixture draws the foreign tokens");

            var subject = layer.Tokens.First(t => t.Token.Unit.Side != UnitData.Israel);
            var policy = ProductionResidency.Approved().Units;
            var focus = new Vector3((float)subject.Spot.X + policy.ExitM + policy.EnterM, 0f, (float)subject.Spot.Z);
            camera = TopDownCamera(DisplacedViewM, focus);
            Assert.AreEqual(DisplacedViewM, TownModelLayer.ViewDistance(camera), 1f, "the verified 11.8 km view");
            Assert.Greater(Vector2.Distance(new Vector2(focus.x, focus.z), new Vector2((float)subject.Spot.X, (float)subject.Spot.Z)),
                policy.ExitM, "focus is beyond the approved leave radius from the token's displaced placement spot");
            foreach (var holder in layer.GetComponentsInChildren<UnitFigureResidency>(true)) holder.Choose(default, camera);

            var tokenRoot = layer.transform.Find("Tokens");
            var holders = tokenRoot.GetComponentsInChildren<UnitFigureResidency>(true);
            Assert.IsNotEmpty(holders, "drawn army tokens have figure or vehicle holders");
            foreach (var holder in holders)
                Assert.IsNotNull(holder.transform.Find(UnitFigures.ModelName), $"{holder.transform.parent.name}/{holder.name}: no bare plinth");
        }
        finally
        {
            if (camera != null) Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(map.gameObject);
        }
    }

    [Test]
    public void NearLeaseStillRetiresWhenCameraLeavesWhileRosterFarRemains()
    {
        var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var live = new List<CollectionDetail>();
        IDisposable Acquire(string _, CollectionDetail detail, Action<GameObject?> answer)
        {
            live.Add(detail);
            answer(prefab);
            return new Lease(() => live.Remove(detail));
        }
        var policy = ProductionResidency.Approved().Units;
        var camera = TopDownCamera(DisplacedViewM, new Vector3(policy.ExitM + policy.EnterM, 0f, 0f));
        try
        {
            var owner = piece.AddComponent<UnitFigureResidency>();
            owner.Initialize(UnitFigures.YoungRecruit, policy, Acquire, retainFar: true);
            owner.RequestNear();
            CollectionAssert.AreEquivalent(new[] { CollectionDetail.Far, CollectionDetail.Near }, live,
                "fixture starts with both roster Far and admitted Near leases");

            owner.Choose(default, camera);

            CollectionAssert.AreEqual(new[] { CollectionDetail.Far }, live, "leaving retires Near but keeps roster Far");
            Assert.IsNotNull(piece.transform.Find(UnitFigures.ModelName), "the retained Far model still dresses the holder");
            Object.DestroyImmediate(piece);
            Assert.IsEmpty(live, "the roster Far lease retires when its holder is destroyed");
        }
        finally
        {
            Object.DestroyImmediate(camera.gameObject);
            if (piece != null) Object.DestroyImmediate(piece);
            Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void DeferredNearRetirementNeverRemovesTheIndependentRosterFarModel()
    {
        var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var live = new List<CollectionDetail>();
        var retirement = new DeferredRetirement();
        IDisposable Acquire(string _, CollectionDetail detail, Action<GameObject?> answer)
        {
            live.Add(detail);
            answer(prefab);
            return new Lease(() => live.Remove(detail));
        }
        var policy = ProductionResidency.Approved().Units;
        var close = TopDownCamera(DisplacedViewM, Vector3.zero);
        var strategic = TopDownCamera(StartViewM, Vector3.zero);
        try
        {
            var owner = piece.AddComponent<UnitFigureResidency>();
            owner.Initialize(UnitFigures.YoungRecruit, policy, Acquire, retainFar: true, retirement: retirement.Retire);
            owner.Choose(default, close);
            owner.RequestNear();
            CollectionAssert.AreEquivalent(new[] { CollectionDetail.Far, CollectionDetail.Near }, live,
                "close view owns independent Far and Near leases");
            var closeSnapshot = owner.ResidencySnapshot();
            CollectionAssert.AreEqual(new[] { UnitFigures.YoungRecruit }, closeSnapshot.originalIds,
                "one original id is reported once while both Far and Near keys are held");
            Assert.IsNotNull(piece.transform.Find(UnitFigures.ModelName));
            Assert.IsNotNull(piece.transform.Find(UnitFigures.ModelName + "/" + UnitFigureDetail.NearName));

            owner.Choose(default, strategic);

            Assert.IsTrue(retirement.Pending, "Near uses the deferred multi-step retirement seam");
            Assert.IsNull(piece.transform.Find(UnitFigures.ModelName + "/" + UnitFigureDetail.NearName),
                "strategic view immediately reverts the camera-gated detail");
            while (retirement.Pending)
            {
                Assert.IsNotNull(piece.transform.Find(UnitFigures.ModelName), "Far exists before every deferred step");
                retirement.Step();
                Assert.IsNotNull(piece.transform.Find(UnitFigures.ModelName), "Far exists after every deferred step");
            }
            CollectionAssert.AreEqual(new[] { CollectionDetail.Far }, live, "deferred completion releases Near only");
            Assert.IsNull(UnitFigures.Near(UnitFigures.YoungRecruit), "the last Near owner releases the Near cache while Far remains");
            var strategicSnapshot = owner.ResidencySnapshot();
            Assert.AreEqual(1, strategicSnapshot.demandedAnchors);
            CollectionAssert.AreEqual(new[] { UnitFigures.YoungRecruit + "|Far" }, strategicSnapshot.detailIds,
                "snapshot reports the one detail key the strategic holder actually owns");

            owner.enabled = false;
            var disabled = owner.ResidencySnapshot();
            Assert.AreEqual(0, disabled.demandedAnchors, "disabled holder reports no active demand");
            Assert.AreEqual(0, disabled.originalIds.Length, "disabled holder reports no actively held ids");
            Assert.AreEqual(0, disabled.detailIds.Length, "disabled holder reports no actively held detail keys");
            while (retirement.Pending) retirement.Step();
            Assert.IsEmpty(live, "disabling releases the lifetime Far lease after retirement");
        }
        finally
        {
            Object.DestroyImmediate(close.gameObject);
            Object.DestroyImmediate(strategic.gameObject);
            if (piece != null) Object.DestroyImmediate(piece);
            Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void DeactivatedRosterHolderKeepsFarUntilDestroyButComponentDisableReleasesIt()
    {
        var inactivePiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var disabledPiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var live = new List<string>();
        IDisposable Acquire(string model, CollectionDetail detail, Action<GameObject?> answer)
        {
            Assert.AreEqual(CollectionDetail.Far, detail);
            live.Add(model);
            answer(prefab);
            return new Lease(() => live.Remove(model));
        }
        try
        {
            var inactive = inactivePiece.AddComponent<UnitFigureResidency>();
            inactive.Initialize(UnitFigures.YoungRecruit, ProductionResidency.Approved().Units, Acquire, retainFar: true);
            inactivePiece.SetActive(false);
            CollectionAssert.Contains(live, UnitFigures.YoungRecruit,
                "ancestor or object deactivation retains Far for the token object's lifetime");
            Assert.IsNotNull(inactivePiece.transform.Find(UnitFigures.ModelName), "deactivation keeps the dressed Far model");
            Object.DestroyImmediate(inactivePiece);
            CollectionAssert.DoesNotContain(live, UnitFigures.YoungRecruit, "destroy releases the deactivated holder's Far lease");

            var disabled = disabledPiece.AddComponent<UnitFigureResidency>();
            disabled.Initialize(UnitFigures.VeteranFighter, ProductionResidency.Approved().Units, Acquire, retainFar: true);
            disabled.enabled = false;
            CollectionAssert.DoesNotContain(live, UnitFigures.VeteranFighter,
                "explicit component disable releases Far even while its token object lives");
        }
        finally
        {
            if (inactivePiece != null) Object.DestroyImmediate(inactivePiece);
            if (disabledPiece != null) Object.DestroyImmediate(disabledPiece);
            Object.DestroyImmediate(prefab);
        }
    }
}
