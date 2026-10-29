#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CampParentDeactivationTests
{
    static System.Func<string, Statecraft.Game.Map.Collection.CollectionDetail, System.Action<GameObject?>, System.IDisposable>? savedAcquire;
    static GameObject? ownedLayer, ownedCamera, ownedPrefab;
    static Mesh? ownedMesh;

    static void ReleaseOwnedFixture()
    {
        if (savedAcquire != null) Statecraft.Game.Map.Collection.CampTentModels.Acquire = savedAcquire;
        savedAcquire = null;
        if (ownedLayer != null) Object.DestroyImmediate(ownedLayer);
        if (ownedCamera != null) Object.DestroyImmediate(ownedCamera);
        if (ownedPrefab != null) Object.DestroyImmediate(ownedPrefab);
        if (ownedMesh != null) Object.DestroyImmediate(ownedMesh);
        ownedLayer = ownedCamera = ownedPrefab = null; ownedMesh = null;
    }
    [UnityTearDown]
    public IEnumerator RestoreScene()
    {
        // The runner may abandon a nested iterator without disposing its finally.
        ReleaseOwnedFixture();
        if (Application.isPlaying) yield return new ExitPlayMode();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ShadowFit.Restore(); VisualToggles.Clear(); KitScale.Reset();
    }
    sealed class FakeLease : System.IDisposable
    {
        readonly System.Action release; bool done;
        public FakeLease(System.Action release) { this.release = release; }
        public void Dispose() { if (!done) { done = true; release(); } }
    }
    [UnityTest]
    public IEnumerator ADeactivatedCampRedrawKeepsTheStreamedTentMesh()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return VerifyDeactivatedCampRedraw();
    }

    static IEnumerator VerifyDeactivatedCampRedraw()
    {
        var saved = Statecraft.Game.Map.Collection.CampTentModels.Acquire;
        Assert.IsNotNull(saved, "the fixture must begin with a valid acquisition service");
        savedAcquire = saved;
        GameObject? camGo = null, prefab = null, layerGo = null;
        Mesh? shared = null;
        try
        {
            var map = Object.FindFirstObjectByType<MapScene>();
            Assert.IsNotNull(map); map.Build();
            var c = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
            var hex = c.SettlementHexes.Single(s => s.Id == "revivim");
            prefab = ownedPrefab = new GameObject("owned streamed tent fixture", typeof(MeshFilter), typeof(MeshRenderer));
            shared = ownedMesh = Object.Instantiate(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
            prefab.GetComponent<MeshFilter>().sharedMesh = shared;
            prefab.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kit.mat");
            prefab.transform.position = Vector3.down * 1000000f;
            int released = 0;
            Statecraft.Game.Map.Collection.CampTentModels.Acquire = (id, detail, done) => { done(prefab); return new FakeLease(() => released++); };
            layerGo = ownedLayer = new GameObject("owned camp redraw fixture");
            var layer = layerGo.AddComponent<PlacementLayer>();
            layer.Render(new[] { new Statecraft.Sim.Placement(Statecraft.Sim.MapActions.TransitCamp, hex.Q, hex.R,
                new System.DateTime(1950, 8, 26)) }, c, map);
            var camp = layer.transform.GetChild(0);
            var khaki = camp.Find(camp.name + "_khaki");
            Assert.IsNotNull(khaki);
            var campMesh = camp.GetComponent<MeshFilter>().sharedMesh;
            var khakiMesh = khaki.GetComponent<MeshFilter>().sharedMesh;
            Assert.IsNotNull(campMesh); Assert.IsNotNull(khakiMesh);
            var tents = khaki.GetComponent<Statecraft.Game.Map.Collection.CampTentModels>();
            camGo = ownedCamera = new GameObject("owned camp redraw camera", typeof(Camera));
            camGo.transform.SetPositionAndRotation(khaki.GetComponent<MeshRenderer>().bounds.center + Vector3.up * 10000f,
                Quaternion.Euler(90f, 0f, 0f));
            tents.Choose(default, camGo.GetComponent<Camera>());
            yield return null; // Real play-mode LateUpdate admits the requested model.
            Assert.Greater(tents.ModelCount, 0, "the real camp must own streamed instances before deactivation");
            var models = tents.Instances.ToArray();
            Assert.IsTrue(models.All(m => m.activeInHierarchy));
            Assert.IsTrue(models.SelectMany(m => m.GetComponentsInChildren<MeshFilter>()).All(f => f.sharedMesh == shared));

            layerGo.SetActive(false);
            layer.Render(System.Array.Empty<Statecraft.Sim.Placement>(), c, map); // Same frame as OnDisable.
            yield return null; // Deferred instance and kit destruction has now completed.
            Assert.IsTrue(shared != null, "redrawing an inactive camp must never destroy the streamed prefab mesh");
            Assert.IsTrue(campMesh == null && khakiMesh == null, "the camp still destroys its own kit meshes");
            Assert.IsTrue(models.All(m => m == null), "the retired model instances are destroyed");
            Assert.AreEqual(1, released, "the original lease is released exactly once");
        }
        finally
        {
            ReleaseOwnedFixture();
        }
    }

}
