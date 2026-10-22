using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Exercise delivered rigs in Unity. Bounds checks catch gross skinning failures, not artistic approval.</summary>
public class Generated1948RigTests
{
    static readonly string[] Ids = {
        "figure-egyptian-infantry-1948-v1", "figure-syrian-infantry-1948-v1", "figure-ala-infantry-1948-v1",
        "figure-iraqi-infantry-1948-v1", "figure-lebanese-regular-1948-v1", "figure-bedouin-civilian-1948-v1",
        "figure-local-arab-irregular-1948-v1"
    };
    static string Dir(string id) => "Assets/Art/" + (Array.IndexOf(Ids, id) > 0 && Array.IndexOf(Ids, id) < 4 ? "Collection/" : "Generated1948Review/") + id;
    static AnimationClip Clip(string id, string level) => AssetDatabase.LoadAllAssetsAtPath($"{Dir(id)}/{id}-{level}.fbx")
        .OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));

    [TestCaseSource(nameof(Ids))]
    public void DeliveredRigHasValidAvatarWeightsIdleAndStaticFar(string id)
    {
        string source = $"{Dir(id)}/{id}-near.fbx";
        var imp = AssetImporter.GetAtPath(source) as ModelImporter;
        Assert.IsNotNull(imp, source);
        Assert.AreEqual(ModelImporterAnimationType.Generic, imp.animationType);
        Assert.AreEqual(ModelImporterAvatarSetup.CreateFromThisModel, imp.avatarSetup);
        Assert.IsTrue(imp.importAnimation);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir(id)}/{id}-near.prefab");
        Assert.IsNotNull(prefab, id);
        var animator = prefab.GetComponentInChildren<Animator>(true);
        Assert.IsNotNull(animator);
        Assert.IsNotNull(animator.avatar);
        Assert.IsTrue(animator.avatar.isValid, id + " valid actual Avatar");
        var skin = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
        var mesh = skin.sharedMesh;
        Assert.Greater(mesh.bindposeCount, 10);
        Assert.IsTrue(mesh.HasVertexAttribute(VertexAttribute.BlendWeight));
        var weights = mesh.boneWeights;
        Assert.AreEqual(mesh.vertexCount, weights.Length);
        foreach (var w in weights)
        {
            float sum = w.weight0 + w.weight1 + w.weight2 + w.weight3;
            Assert.IsFalse(float.IsNaN(sum) || float.IsInfinity(sum));
            Assert.That(sum, Is.InRange(.98f, 1.02f));
            var values = new[] { w.weight0, w.weight1, w.weight2, w.weight3 };
            var indices = new[] { w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3 };
            for (int k = 0; k < 4; k++)
            {
                Assert.That(values[k], Is.InRange(0f, 1f));
                if (values[k] > 0f) Assert.That(indices[k], Is.InRange(0, mesh.bindposeCount - 1));
            }
        }
        var idle = Clip(id, "near");
        Assert.AreEqual("idle", idle.name);
        Assert.IsTrue(idle.isLooping);
        Assert.AreSame(idle, animator.runtimeAnimatorController.animationClips.Single());
        var far = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir(id)}/{id}-far.prefab");
        Assert.IsNotNull(far);
        Assert.IsEmpty(far.GetComponentsInChildren<Animator>(true));
        Assert.IsEmpty(far.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        var farMeshes = far.GetComponentsInChildren<MeshFilter>(true);
        Assert.IsNotEmpty(farMeshes);
        Assert.LessOrEqual(farMeshes.Sum(f => (long)f.sharedMesh.GetIndexCount(0) / 3), 1500);
    }

    [TestCaseSource(nameof(Ids))]
    public void ActualAnimatorIdleAndWalkDeformFiniteVerticesWithoutExplodingFeetOrHeadcloth(string id)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir(id)}/{id}-near.prefab");
        Assert.IsNotNull(prefab, id);
        var go = Object.Instantiate(prefab);
        try
        {
            foreach (string level in new[] { "near", "walk" })
            {
                var clip = Clip(id, level);
                Assert.Greater(clip.length, .1f, level);
                var first = Pose(go, clip, .05f);
                var b = new Bounds(first[0], Vector3.zero);
                foreach (var v in first) b.Encapsulate(v);
                float h = b.size.y;
                Assert.Greater(h, .1f, "actual evaluated world-space height, not a 100x unit error");
                float maximumMovement = 0f;
                foreach (float fraction in new[] { .23f, .49f, .73f, .91f })
                {
                    var current = Pose(go, clip, fraction);
                    Assert.AreEqual(first.Length, current.Length);
                    var now = new Bounds(current[0], Vector3.zero);
                    for (int i = 0; i < current.Length; i++)
                    {
                        var v = current[i];
                        Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                            float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z), id + " " + level);
                        now.Encapsulate(v);
                        float movement = Vector3.Distance(v, first[i]);
                        maximumMovement = Mathf.Max(maximumMovement, movement);
                        if (first[i].y < b.min.y + h * .15f) Assert.Less(movement, h * .85f, "foot/hem explosion");
                        if (first[i].y > b.min.y + h * .82f) Assert.Less(movement, h * .65f, "head/headcloth explosion");
                    }
                    Assert.That(now.size.y / h, Is.InRange(.65f, 1.4f), "animated silhouette height");
                    Assert.Greater(now.min.y, b.min.y - h * .3f, "feet/cloth below ground region");
                    Assert.Less(now.min.y, b.min.y + h * .4f, "entire figure floating");
                    Assert.Less(now.max.y, b.max.y + h * .4f, "head/headcloth spike");
                }
                Assert.Greater(maximumMovement, h * .00001f, level + " must move baked vertices through Animator evaluation");
            }
        }
        finally { Object.DestroyImmediate(go); }
    }

    static Vector3[] Pose(GameObject go, AnimationClip clip, float fraction)
    {
        var animator = go.GetComponentInChildren<Animator>(true);
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.Rebind();
        var graph = PlayableGraph.Create("Generated1948 rig test");
        var baked = new Mesh();
        try
        {
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);
            var output = AnimationPlayableOutput.Create(graph, "actual Animator", animator);
            output.SetSourcePlayable(playable);
            graph.Play();
            playable.SetTime(clip.length * fraction);
            graph.Evaluate(0f);
            var skin = go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            skin.BakeMesh(baked);
            return baked.vertices.Select(v => skin.transform.TransformPoint(v)).ToArray();
        }
        finally { graph.Destroy(); Object.DestroyImmediate(baked); }
    }
}
