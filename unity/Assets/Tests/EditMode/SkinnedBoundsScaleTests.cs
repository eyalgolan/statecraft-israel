#nullable enable
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using UnityEngine;

public class SkinnedBoundsScaleTests
{
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void KnownVerticesHaveTheHandComputedWorldBox(bool civilian, bool afterFit)
    {
        var frame = new GameObject("identity measurement frame");
        var root = new GameObject("scaled parent");
        var mesh = new Mesh { vertices = new[] { new Vector3(-1, 0, -2), new Vector3(1, 2, 2), new Vector3(0, 1, 0) },
            triangles = new[] { 0, 1, 2 }, bindposes = new[] { Matrix4x4.identity },
            boneWeights = new[] { Weight(), Weight(), Weight() } };
        try
        {
            root.transform.position = new Vector3(10, 20, 30);
            root.transform.localScale = new Vector3(5, 7, 11);
            var child = new GameObject("scaled renderer", typeof(SkinnedMeshRenderer));
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = new Vector3(1, 2, 3);
            child.transform.localScale = new Vector3(2, 3, 4);
            var bone = new GameObject("identity rest bone");
            bone.transform.SetParent(child.transform, false);
            var skin = child.GetComponent<SkinnedMeshRenderer>();
            skin.sharedMesh = mesh; skin.bones = new[] { bone.transform }; skin.rootBone = bone.transform;
            if (afterFit) child.transform.localScale *= 0.5f;
            // By hand: world = (10,20,30) + (5,7,11) * ((1,2,3) + (2,3,4) * vertex).
            // With the half-size fit, the last scale is (1,1.5,2). No baked vertices or measured matrix is the oracle.
            var expected = afterFit ? new Bounds(new Vector3(15, 44.5f, 63), new Vector3(10, 21, 88))
                : new Bounds(new Vector3(15, 55, 63), new Vector3(20, 42, 176));
            Bounds actual;
            if (civilian)
            {
                // Bounds is private: invoke the real fitting measurement without widening production visibility.
                var method = typeof(CivilianPeople).GetMethod("Bounds", BindingFlags.Static | BindingFlags.NonPublic)!;
                actual = (Bounds)method.Invoke(null, new object[] { root })!;
            }
            else
            {
                var box = UnitFigures.BoxIn(frame.transform, root.transform);
                Assert.IsTrue(box.HasValue); actual = box!.Value;
            }
            Assert.Less(Vector3.Distance(expected.center, actual.center), 0.001f, $"center expected {expected.center}, got {actual.center}");
            Assert.Less(Vector3.Distance(expected.size, actual.size), 0.001f, $"size expected {expected.size}, got {actual.size}");
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(frame); Object.DestroyImmediate(mesh); }
    }

    static BoneWeight Weight() => new BoneWeight { boneIndex0 = 0, weight0 = 1 };
}
