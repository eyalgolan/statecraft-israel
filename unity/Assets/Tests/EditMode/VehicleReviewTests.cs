#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.VehicleReview;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

public class VehicleReviewTests
{
    [SetUp] public void EmptyScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    [TearDown] public void Cleanup() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    [TestCase("https://game.test/?vehicle-review", true)]
    [TestCase("https://game.test/?vehicle-review=1", true)]
    [TestCase("https://game.test/vehicle-review/index.html", false)]
    [TestCase("https://game.test/?vehicle-review-extra", false)]
    [TestCase("https://game.test/", false)]
    public void ReviewModeRequiresItsOwnExplicitFlag(string url, bool expected) =>
        Assert.AreEqual(expected, VehicleReviewStage.Requested(url, Array.Empty<string>(), true));

    [Test]
    public void DesktopFlagIsExactAndDoesNotAlterNormalUnitCatalogs()
    {
        Assert.IsFalse(VehicleReviewStage.Requested(null, new[] { "-vehicle-review-extra" }, false));
        Assert.IsTrue(VehicleReviewStage.Requested(null, new[] { "-vehicle-review" }, false));
        foreach (var id in VehicleReviewCatalog.Ids)
        {
            Assert.IsNull(CollectionCatalog.BundleOf(id));
            Assert.IsFalse(UnitFigures.BySide.Values.Contains(id));
            Assert.IsFalse(UnitFigures.ByVehicle.Values.Contains(id));
        }
    }

    [Test]
    public void ReviewPlacesAllThreeOwnModelsWithSharedPivotsAndRealNearFarGeometry()
    {
        var catalog = Resources.Load<VehicleReviewCatalog>(VehicleReviewCatalog.Resource);
        Assert.IsNotNull(catalog, "run VehicleReviewAssets.Import before the integration test");
        catalog!.Validate();
        var stage = VehicleReviewStage.Create(catalog);
        try
        {
            Assert.AreEqual(3, stage.Placed.Count);
            for (int i = 0; i < stage.Placed.Count; i++)
            {
                var placed = stage.Placed[i];
                Assert.AreEqual(VehicleReviewCatalog.Ids[i], placed.Id);
                foreach (var root in new[] { placed.Near, placed.Far })
                {
                    var box = VehicleReviewStage.BoundsOf(root);
                    Assert.AreEqual(0f, box.min.y, .5f, placed.Id + " ground contact");
                    Assert.AreEqual(placed.At.x, box.center.x, .5f, placed.Id + " x pivot");
                    Assert.AreEqual(placed.At.z, box.center.z, .5f, placed.Id + " z pivot");
                    Assert.AreEqual(placed.LongSideM, Mathf.Max(box.size.x, box.size.z), 1f, placed.Id + " presentation scale");
                    Assert.AreEqual(1, root.GetComponentsInChildren<MeshRenderer>(true).Length);
                }
                int near = Triangles(placed.Near), far = Triangles(placed.Far);
                Assert.Greater(near, 0); Assert.Greater(far, 0);
                Assert.Less(far, near, placed.Id + " far is actually simplified");
                Assert.LessOrEqual(far, 4500, placed.Id + " existing vehicle delivery ceiling");
                var mat = placed.Near.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
                Assert.IsNotNull(mat.mainTexture); Assert.LessOrEqual(mat.mainTexture.width, 512);
            }
            stage.Rig.Distance = 12000f; stage.Rig.Apply(); stage.Choose(stage.Camera);
            Assert.IsTrue(stage.Placed.All(p => p.Near.activeSelf && !p.Far.activeSelf));
            stage.Rig.Distance = 60000f; stage.Rig.Apply(); stage.Choose(stage.Camera);
            Assert.IsTrue(stage.Placed.All(p => !p.Near.activeSelf && p.Far.activeSelf));
            Assert.AreEqual(CameraRig.Fov, stage.Camera.fieldOfView);
            Assert.AreEqual(CameraRig.Pitch, stage.Camera.transform.eulerAngles.x, .01f);
            StringAssert.Contains("Non-historical", VehicleReviewStage.ReviewNotice);
        }
        finally { UnityEngine.Object.DestroyImmediate(stage.gameObject); }
    }

    [Test]
    public void ReviewLabelsFitTheActualOverviewAndInspectViewports()
    {
        var catalog = Resources.Load<VehicleReviewCatalog>(VehicleReviewCatalog.Resource);
        Assert.IsNotNull(catalog);
        var stage = VehicleReviewStage.Create(catalog!);
        try
        {
            // A 16:9 game view must show the entire overview label layout. In particular,
            // fontSize and characterSize multiply: large characterSize values hid the models.
            stage.Camera.aspect = 16f / 9f;
            stage.Overview(); stage.Choose(stage.Camera);
            foreach (var label in stage.GetComponentsInChildren<TextMesh>())
                AssertViewport(stage.Camera, label.GetComponent<MeshRenderer>().bounds, .03f, .97f, label.text);
            for (int i = 0; i < stage.Placed.Count; i++)
            {
                stage.Focus(i); stage.Choose(stage.Camera);
                var label = stage.GetComponentsInChildren<TextMesh>().Single(t => t.text.StartsWith((i + 1) + " · ", StringComparison.Ordinal));
                var box = Viewport(stage.Camera, label.GetComponent<MeshRenderer>().bounds);
                AssertViewport(stage.Camera, label.GetComponent<MeshRenderer>().bounds, .03f, .97f, label.text);
                Assert.Less(box.width, .75f, label.text + " must leave room around the model");
                var model = Viewport(stage.Camera, VehicleReviewStage.BoundsOf(stage.Placed[i].Near));
                Assert.IsFalse(box.Overlaps(model), label.text + " must not cover the inspected model");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(stage.gameObject); }
    }

    static void AssertViewport(Camera camera, Bounds bounds, float min, float max, string message)
    {
        var box = Viewport(camera, bounds);
        Assert.GreaterOrEqual(box.xMin, min, message); Assert.LessOrEqual(box.xMax, max, message);
        Assert.GreaterOrEqual(box.yMin, min, message); Assert.LessOrEqual(box.yMax, max, message);
    }

    static Rect Viewport(Camera camera, Bounds bounds)
    {
        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < 8; i++)
        {
            var p = camera.WorldToViewportPoint(bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
            min = Vector2.Min(min, p); max = Vector2.Max(max, p);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    [Test]
    public void ReviewTeardownRestoresLightingAndLeavesExistingCameraUntouched()
    {
        var catalog = Resources.Load<VehicleReviewCatalog>(VehicleReviewCatalog.Resource);
        Assert.IsNotNull(catalog);
        var original = new GameObject("Existing game camera").AddComponent<Camera>(); original.tag = "MainCamera"; original.fieldOfView = 43f;
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientSkyColor = new Color(.2f, .3f, .4f);
        var before = RenderSettings.ambientSkyColor;
        var stage = VehicleReviewStage.Create(catalog!);
        UnityEngine.Object.DestroyImmediate(stage.gameObject);
        Assert.AreEqual(AmbientMode.Flat, RenderSettings.ambientMode);
        Assert.AreEqual(before, RenderSettings.ambientSkyColor);
        Assert.AreEqual(43f, original.fieldOfView);
        Assert.AreSame(original, Camera.main);
    }

    static int Triangles(GameObject root) => root.GetComponentsInChildren<MeshFilter>(true)
        .Sum(f => (int)f.sharedMesh.GetIndexCount(0) / 3);
}
