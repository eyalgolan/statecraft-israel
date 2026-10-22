using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Generated1948CharacterReviewTests
{
    const string ScenePath = "Assets/Generated1948CharacterReview.unity";
    static readonly string[] Expected = {
        "figure-egyptian-infantry-1948-v1", "figure-syrian-infantry-1948-v1", "figure-ala-infantry-1948-v1",
        "figure-iraqi-infantry-1948-v1", "figure-lebanese-regular-1948-v1", "figure-bedouin-civilian-1948-v1",
        "figure-local-arab-irregular-1948-v1"
    };

    [Test]
    public void PlayableReviewSceneOwnsSevenRealRigsAndCameraWithoutHistoricalPositions()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            var roots = scene.GetRootGameObjects();
            var review = roots.SelectMany(g => g.GetComponentsInChildren<Generated1948CharacterReview>()).Single();
            Assert.IsEmpty(roots.SelectMany(g => g.GetComponentsInChildren<GameBoot>()));
            Assert.IsEmpty(roots.SelectMany(g => g.GetComponentsInChildren<MapScene>()));
            Assert.IsNotNull(review.ReviewCamera);
            Assert.IsTrue(review.ReviewCamera.enabled);
            Assert.AreEqual(scene, review.ReviewCamera.gameObject.scene);
            StringAssert.Contains("No historical positions", review.Notice);
            CollectionAssert.AreEqual(Expected, review.Characters.Select(c => c.Id));
            CollectionAssert.AreEqual(new[] { false, true, false, true, false, false, false }, review.Characters.Select(c => c.Campaign));
            foreach (var entry in review.Characters)
            {
                Assert.IsNotNull(entry.Model, entry.Id);
                Assert.IsNotNull(entry.Material, entry.Id);
                StringAssert.EndsWith(entry.Id + "-near.fbx", AssetDatabase.GetAssetPath(entry.Model));
                StringAssert.EndsWith(entry.Id + "-near.fbx", AssetDatabase.GetAssetPath(entry.Idle));
                StringAssert.EndsWith(entry.Id + "-walk.fbx", AssetDatabase.GetAssetPath(entry.Walk));
                Assert.AreEqual("idle", entry.Idle.name);
                Assert.AreEqual("walk", entry.Walk.name);
                Assert.IsNotNull(entry.Model.GetComponentInChildren<SkinnedMeshRenderer>(true));
                Assert.IsNotNull(entry.Model.GetComponentInChildren<Animator>(true).avatar);
            }
            review.BuildForReview();
            Assert.AreEqual(7, review.Instances.Count);
            Assert.IsTrue(review.Instances.All(g => g.GetComponentInChildren<Animator>(true).avatar.isValid));
            review.SetGameCamera(true);
            Assert.IsFalse(review.ReviewCamera.orthographic);
            Assert.AreEqual(CameraRig.Pitch, review.ReviewCamera.transform.eulerAngles.x, .001f);
            Assert.AreEqual(CameraRig.Fov, review.ReviewCamera.fieldOfView);
            review.SetGameCamera(false);
            Assert.IsTrue(review.ReviewCamera.orthographic);
            Assert.AreEqual(12f, review.ReviewCamera.transform.eulerAngles.x, .001f);
            review.SetMotion(true);
            review.SampleMotion(.6f);
            Assert.IsTrue(review.Walking);
            AssertGrounded(review);
            review.SetMotion(false);
            review.SampleMotion(2.5f);
            Assert.IsFalse(review.Walking);
            AssertGrounded(review);
            review.Focus(0);
            Assert.AreEqual(0, review.Selected);
            review.Focus(-1);
            Assert.AreEqual(-1, review.Selected);
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }

    static void AssertGrounded(Generated1948CharacterReview review)
    {
        var mesh = new Mesh();
        float floor = review.transform.Find("Review stage - local display coordinates").GetComponent<Renderer>().bounds.max.y;
        try
        {
            foreach (var instance in review.Instances)
            {
                var skin = instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
                skin.BakeMesh(mesh, true);
                float minimum = mesh.vertices.Min(v => skin.transform.TransformPoint(v).y);
                Assert.That(minimum - floor, Is.InRange(-.01f, .04f), instance.name + ": evaluated feet/hem meet the actual floor surface");
                Assert.LessOrEqual(skin.bounds.min.y, floor + .1f,
                    instance.name + ": independently computed renderer world bounds must reach the floor");
            }
        }
        finally { Object.DestroyImmediate(mesh); }
    }
}
