#nullable enable
using System;
using System.Linq;
using Statecraft.Game.Map;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using NUnit.Framework;
using Statecraft.HexTiles;

/// <summary>The hex-tile switch is opt-in and strict, as MapLook's is (the default game must not change).</summary>
public class HexTilesLookTests
{
    [TestCase("http://localhost/?hex-tiles", true)]
    [TestCase("http://localhost/?hex-tiles=1", true)]
    [TestCase("http://localhost/?map-look&hex-tiles", true)]
    [TestCase("http://localhost/?hex-tile", false)]
    [TestCase("http://localhost/?hex-tiles=0", false)]
    [TestCase("http://localhost/#hex-tiles", false)]
    [TestCase("http://localhost/?next=/p?hex-tiles", false)]
    [TestCase(null, false)]
    public void TheWebFlagIsStrict(string? url, bool expected) =>
        Assert.AreEqual(expected, HexTilesLook.Requested(url, Array.Empty<string>(), webgl: true));

    [Test]
    public void TheDesktopFlagIsTheExactArgument()
    {
        Assert.IsTrue(HexTilesLook.Requested(null, new[] { "Statecraft", "-hex-tiles" }, webgl: false));
        Assert.IsFalse(HexTilesLook.Requested(null, new[] { "-hex-tiles-x" }, webgl: false));
        Assert.IsFalse(HexTilesLook.Requested(null, Array.Empty<string>(), webgl: false));
    }

    [Test]
    public void TheBoardShipsInResources() => Assert.That(HexBoard.Load(HexTilesLook.Board).Hexes.Count, Is.GreaterThan(0));

    [TestCase("sandwich_truck", true)]
    [TestCase("willys_jeep", true)]
    [TestCase("palmach_squad", false)]
    public void MissingModelsKeepTheDeclaredUnitSize(string typeId, bool byWidth)
    {
        const float size = 1000f;
        var unit = HexUnitTypes.Create(typeId).Create();
        // Exercise fallback even on an artist's machine with the local pending-rights models.
        foreach (var m in unit.Members)
        {
            Assert.AreEqual(byWidth, m.ByWidth);
            m.Model = "tiles:__missing_unit_test_" + typeId;
            Assert.IsNull(HexModels.Prefab(m.Model));
        }
        var root = new GameObject("HexUnitFallbackTest");
        try
        {
            var motion = new HexUnitMotion(unit, new[] { (0, 0) }, new FlatSurface(), root.transform, size);
            motion.Pose(0f, false);
            Assert.AreEqual(unit.Members.Count, motion.Objects.Count);
            for (int i = 0; i < unit.Members.Count; i++)
            {
                var bounds = motion.Objects[i].GetComponentInChildren<Renderer>().bounds.size;
                float dimension = byWidth ? Mathf.Max(bounds.x, bounds.z) : bounds.y;
                Assert.That(dimension, Is.EqualTo(unit.Members[i].HeightShare * size).Within(0.01f), typeId);
                var mesh = motion.Objects[i].GetComponentInChildren<MeshFilter>().sharedMesh;
                StringAssert.Contains(byWidth ? "Cube" : "Capsule", mesh.name, "the declared Box/Figure stand-in");
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase("palmach_squad", false)]
    [TestCase("palmach_squad", true)]
    [TestCase("sandwich_truck", false)]
    [TestCase("sandwich_truck", true)]
    public void MissingModelsUseAnOwnedShippedMaterial(string typeId, bool existingBoardOwner)
    {
        var template = Resources.Load<Material>(HexMaterials.Folder + "/" + HexMaterials.Lit);
        Assert.IsNotNull(template, "the portable material must ship with the player");
        var unit = HexUnitTypes.Create(typeId).Create();
        foreach (var member in unit.Members)
        {
            member.Model = "tiles:__missing_unit_material_test_" + typeId;
            Assert.IsNull(HexModels.Prefab(member.Model));
        }
        var root = new GameObject("HexUnitMaterialLifetimeTest");
        try
        {
            var boardOwner = existingBoardOwner ? root.AddComponent<HexBoardAssets>() : null;
            var parent = existingBoardOwner ? new GameObject("Units").transform : root.transform;
            if (existingBoardOwner) parent.SetParent(root.transform, false);
            var motion = new HexUnitMotion(unit, new[] { (0, 0) }, new FlatSurface(), parent);
            motion.Pose(0f, false);
            Assert.AreEqual(unit.Members.Count, motion.Objects.Count);
            var material = motion.Objects[0].GetComponent<MeshRenderer>().sharedMaterial;
            Assert.IsNotNull(material);
            Assert.AreNotSame(template, material, "the shipped template is borrowed, not owned");
            Assert.AreSame(template.shader, material.shader);
            Assert.AreEqual(template.GetColor("_BaseColor"), material.GetColor("_BaseColor"));
            Assert.AreEqual(template.GetFloat("_Smoothness"), material.GetFloat("_Smoothness"));
            foreach (var go in motion.Objects)
                Assert.AreSame(material, go.GetComponent<MeshRenderer>().sharedMaterial, "one owned copy per motion");
            Assert.AreEqual(1, root.GetComponentsInChildren<HexBoardAssets>(true).Length, "reuse the existing ancestor owner");
            if (existingBoardOwner)
            {
                Assert.IsNull(parent.GetComponent<HexBoardAssets>(), "the child must not duplicate the board owner");
                boardOwner!.Teardown();
            }
            else Object.DestroyImmediate(root);
            Assert.IsTrue(material == null, "the parent/board lifetime releases the owned material");
            Assert.IsTrue(template != null, "teardown must never destroy the shipped template");
        }
        finally { if (root != null) Object.DestroyImmediate(root); }
    }

    [Test]
    public void TheBoardCanBeAppliedAfterReopeningTheEditorScene()
    {
        try
        {
            for (int pass = 0; pass < 2; pass++)
            {
                EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
                var map = Object.FindFirstObjectByType<MapScene>();
                Assert.IsNotNull(map);
                map.Build();
                // No cache reset or manual mesh destruction: reopening the real scene releases procedural assets.
                Assert.DoesNotThrow(() => HexTilesLook.Apply(map, true), "supported board, scene pass " + pass);
                Assert.IsNotNull(map.transform.Find(HexTilesLook.RootName));
                foreach (StandIn kind in Enum.GetValues(typeof(StandIn)))
                {
                    var parts = HexModels.StandInParts(kind);
                    Assert.IsTrue(parts.All(part => part.mesh != null), kind + ": cached meshes are alive");
                    Assert.AreSame(parts, HexModels.StandInParts(kind), kind + ": live cached parts are reused");
                    Assert.Greater(HexModels.StandInTriangles(kind), 0, kind.ToString());
                }
                HexTilesLook.Apply(map, false);
                Assert.IsNull(map.transform.Find(HexTilesLook.RootName));
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ShadowFit.Restore(); VisualToggles.Clear(); Statecraft.Game.Map.Kits.KitScale.Reset();
        }
    }

    [Test]
    public void RemovingAReplacementBoardRestoresTheDefaultMarkerStates()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            var map = Object.FindFirstObjectByType<MapScene>();
            Assert.IsNotNull(map);
            map.Build();
            var markers = map.transform.Find("Settlements").GetComponentsInChildren<MeshRenderer>(true);
            Assert.Greater(markers.Length, 0);
            var before = markers.Select(r => r.enabled).ToArray();
            Assert.IsTrue(before.All(on => !on), "the default kits hide their placeholder boxes");
            HexTilesLook.Apply(map, false);
            CollectionAssert.AreEqual(before, markers.Select(r => r.enabled).ToArray(), "no board is a no-op");
            HexTilesLook.Apply(map, true);
            HexTilesLook.Apply(map, true); // replacing the board must also unwind its previous state first
            Assert.IsNotNull(map.transform.Find(HexTilesLook.RootName));
            HexTilesLook.Apply(map, false);
            Assert.IsNull(map.transform.Find(HexTilesLook.RootName));
            CollectionAssert.AreEqual(before, markers.Select(r => r.enabled).ToArray(),
                "removing the replacement board must not enable placeholder boxes over the restored kits");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ShadowFit.Restore(); VisualToggles.Clear(); Statecraft.Game.Map.Kits.KitScale.Reset();
        }
    }
}
