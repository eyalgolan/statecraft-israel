using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Generated candidates are reviewed outside Resources and the shipped collection until their placement is verified.</summary>
public class Generated1948AssetTests
{
    const string Folder = "Assets/Art/Generated1948Review";
    static readonly string[] Ids = {
        "generated-1948-02-netanya-two-storey-balcony-house", "generated-1948-03-netanya-flat-roof-porch-house",
        "generated-1948-04-dark-basalt-house", "generated-1948-06-bedouin-b-enclosed-striped-tent",
        "generated-1948-08-khaki-canvas-tent", "generated-1948-09-stone-village-well",
        "generated-1948-10-low-sandbag-position", "generated-1948-11-supply-crate-stack",
        "generated-1948-12-low-stone-wall-module"
    };

    [Test]
    public void NewHouseVariantsStayInTheirRequestedTownsAndKeepTheOriginals()
    {
        var netanya = new Statecraft.Game.Map.Settlement { Id = "netanya", Type = "town" };
        var tiberias = new Statecraft.Game.Map.Settlement { Id = "tiberias", Type = "town" };
        var safed = new Statecraft.Game.Map.Settlement { Id = "safed", Type = "town" };
        string[] Houses(Statecraft.Game.Map.Settlement s) => Statecraft.Game.Map.Kits.TownModels.ForArchetype(
            Statecraft.Game.Map.Kits.SettlementKits.ArchetypeOf(s), s).Select(m => Statecraft.Game.Map.Kits.TownModels.Ids[(int)m]).ToArray();
        CollectionAssert.Contains(Houses(netanya), Ids[0]);
        CollectionAssert.Contains(Houses(netanya), Ids[1]);
        CollectionAssert.Contains(Houses(netanya), "b13a-netanya-two-storey-v2");
        CollectionAssert.Contains(Houses(netanya), "generated-1948-01-textured-hip-roof-repair-v2");
        var arab = Statecraft.Game.Map.Kits.TownModels.ForArchetype(Statecraft.Game.Map.Kits.Archetype.ArabTown, safed);
        CollectionAssert.Contains(arab, Statecraft.Game.Map.Kits.TownModel.CourtyardRepair2026);
        CollectionAssert.Contains(arab, Statecraft.Game.Map.Kits.TownModel.B17CourtyardHouse);
        CollectionAssert.Contains(Houses(tiberias), Ids[2]);
        CollectionAssert.Contains(Houses(tiberias), "b17-stone-courtyard-house-v2");
        CollectionAssert.AreEquivalent(new[] { "b17-stone-courtyard-house-v2", "generated-1948-05-textured-courtyard-roof-repair-v2" }, Houses(safed));
    }

    [Test]
    public void StaticCandidatesHaveCompleteIndependentPrefabsWithUvAndExternalColourMaps()
    {
        foreach (var id in Ids)
        {
            var dir = Folder + "/" + id;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(dir + "/" + id + ".prefab");
            Assert.IsNotNull(go, id);
            var filters = go.GetComponentsInChildren<MeshFilter>(true);
            Assert.AreEqual(1, filters.Length, id);
            var mesh = filters.Single().sharedMesh;
            Assert.Greater(mesh.vertexCount, 0, id);
            Assert.IsTrue(mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0), id + " UV");
            Assert.Greater(mesh.GetIndexCount(0), 0, id);
            Assert.IsEmpty(go.GetComponentsInChildren<SkinnedMeshRenderer>(true), id + " is static, not a rigged character");
            var mat = go.GetComponentsInChildren<MeshRenderer>(true).Single().sharedMaterial;
            Assert.IsNotNull(mat, id);
            Assert.IsNotNull(mat.mainTexture, id + " colour map");
            StringAssert.StartsWith(dir, AssetDatabase.GetAssetPath(mat.mainTexture), id);
            Assert.LessOrEqual(mat.mainTexture.width, 512, id);
            Assert.IsFalse(Statecraft.Game.Map.Collection.CollectionCatalog.Ids.Contains(id), id + " has no invented placement");
        }
    }
}
