using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>The generated 1948 infantry use their own sides' existing formations; importing art creates no forces.</summary>
public class Generated1948CharacterTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Units => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));

    [Test]
    public void ReviewOnlyAlaIsNotInAnyPlayerBundleOrAnimationCatalog()
    {
        const string id = "figure-ala-infantry-1948-v1";
        Assert.IsNull(CollectionCatalog.BundleOf(id), "review-only ALA must not enlarge the People download");
        CollectionAssert.DoesNotContain(CollectionCatalog.Animated, id);
        var models = AssetDatabase.FindAssets(id).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".fbx", StringComparison.Ordinal)).ToArray();
        Assert.IsNotEmpty(models);
        Assert.IsTrue(models.All(p => p.StartsWith("Assets/Editor/", StringComparison.Ordinal)));
    }

    static readonly string[] AllGenerated = {
        "figure-egyptian-infantry-1948-v1", "figure-syrian-infantry-1948-v1", "figure-ala-infantry-1948-v1",
        "figure-iraqi-infantry-1948-v1", "figure-lebanese-regular-1948-v1", "figure-bedouin-civilian-1948-v1",
        "figure-local-arab-irregular-1948-v1"
    };
    static string FullDir(string id) => (CollectionCatalog.IsFullQualityGeneratedInfantry(id)
        ? "Assets/Art/Collection/" : "Assets/Editor/Generated1948Review/") + id;

    [TestCaseSource(nameof(AllGenerated))]
    public void FullQualitySourceMapAndImportRetainEveryAvailableClip(string id)
    {
        string dir = FullDir(id);
        var model = AssetImporter.GetAtPath($"{dir}/{id}.fbx") as ModelImporter;
        Assert.IsNotNull(model);
        Assert.AreEqual(ModelImporterMeshCompression.Off, model.meshCompression);
        Assert.AreEqual(ModelImporterAnimationCompression.Off, model.animationCompression);
        Assert.IsTrue(model.importAnimation);
        Assert.IsFalse(model.resampleCurves);
        CollectionAssert.AreEquivalent(new[] { "idle", "walk" }, model.clipAnimations.Select(c => c.name));
        var map = AssetImporter.GetAtPath($"{dir}/{id}_basecolor.jpg") as TextureImporter;
        Assert.IsNotNull(map);
        Assert.GreaterOrEqual(map.maxTextureSize, 4096);
        Assert.IsFalse(map.crunchedCompression);
        Assert.AreEqual(TextureImporterCompression.Uncompressed, map.textureCompression);
        foreach (var target in new[] { "Standalone", "WebGL" })
            Assert.IsFalse(map.GetPlatformTextureSettings(target).overridden, "no hidden downsize/compression override");
        string original = Path.GetFullPath(Path.Combine(Application.dataPath, "../..", "asset-sources/generated-1948", id, "original_basecolor.jpg"));
        Assert.IsTrue(File.Exists(original));
        Assert.AreEqual(CollectionCatalog.Sha256(original), CollectionCatalog.Sha256($"{dir}/{id}_basecolor.jpg"),
            "the embedded provider JPEG is copied without resize or re-encoding");
        CollectionAssert.IsEmpty(Directory.GetFiles(dir, "*-far.fbx"));
        CollectionAssert.IsEmpty(Directory.GetFiles(dir, "*-near.fbx"));
    }

    [TestCase("figure-syrian-infantry-1948-v1")]
    [TestCase("figure-iraqi-infantry-1948-v1")]
    public void FullQualityFigureKeepsOneModelWithoutTheLegacyLodSwitcher(string id)
    {
        var root = new GameObject("Full quality figure fixture");
        var piece = new GameObject("Rifleman"); piece.transform.SetParent(root.transform, false);
        UnitFigures.Forget();
        UnitFigures.Source = (model, detail, done) => done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(model, detail)));
        try
        {
            UnitFigures.Dress(piece.transform, id);
            Assert.IsTrue(UnitFigures.Dressed(piece.transform));
            Assert.IsNull(root.GetComponent<UnitFigureDetail>(), "same full skinned model at both zoom levels; no duplicate near instance/request");
            Assert.AreEqual(1, piece.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
            Assert.IsTrue(piece.GetComponentInChildren<Animator>(true).enabled);
        }
        finally { UnitFigures.Source = null; UnitFigures.Forget(); UnityEngine.Object.DestroyImmediate(root); }
    }

    [Test]
    public void PlayerSceneAndResourcesDoNotReferenceReviewOnlyCharacters()
    {
        var paths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path)
            .Concat(AssetDatabase.FindAssets("", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath));
        CollectionAssert.IsEmpty(AssetDatabase.GetDependencies(paths.ToArray(), true)
            .Where(p => p.StartsWith("Assets/Editor/Generated1948Review/", StringComparison.Ordinal)));
        foreach (string id in AllGenerated.Where(id => !CollectionCatalog.IsFullQualityGeneratedInfantry(id)))
        {
            Assert.IsNull(CollectionCatalog.BundleOf(id));
            CollectionAssert.DoesNotContain(CollectionCatalog.Animated, id);
        }
    }

    [Test]
    public void OriginalC09RemainsOnExistingAlaCampaignTokens()
    {
        Assert.AreEqual(UnitFigures.Irregular, UnitFigures.SideFigure("ala"),
            "new candidate art must not remove the original C09 campaign role");
        var rows = Units;
        Assert.IsFalse(rows.Any(r => r.Side == "local_arab_forces"),
            "a mapping to an undeployed side cannot preserve C09's campaign use");
        UnitAnchor Unmoved(UnitEntry row, UnitAnchor start, int q, int r) => start;
        var tokens = UnitRoster.FromState(rows, C,
            Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical),
            id => id == UnitFigures.Irregular, Unmoved);
        var ala = tokens.Single(t => t.Unit.Id == "arab_liberation_army");
        Assert.AreEqual(UnitFigures.Irregular, ala.ArtType);
        CollectionAssert.AreEqual(Enumerable.Repeat(UnitFigures.Irregular, 3),
            Enumerable.Range(0, 3).Select(i => UnitFigures.FigureFor(ala.Unit, i, null)));
        Assert.IsFalse(UnitFigures.BySide.Values.Contains(UnitFigures.AlaInfantry),
            "the new ALA candidate remains in the explicit review scene");
    }

    [TestCase("syria", "syrian_army", "syrian_army", "figure-syrian-infantry-1948-v1")]
    [TestCase("ala", "arab_liberation_army", "arab_liberation_army", "figure-c09-irregular-v2")]
    [TestCase("iraq", "iraqi_expeditionary_force", "iraqi_army", "figure-iraqi-infantry-1948-v1")]
    public void AnExistingFormationWaitsForItsOwnFigure(string side, string rowId, string type, string model)
    {
        var rows = Units;
        var state = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var force = C.StartingForces.Single(f => f.Row == rowId);
        Assert.AreEqual(side, force.Side);
        Assert.AreEqual(type, force.Type);
        // No unit moves in this test: the original sourced anchor supplies its position.
        UnitAnchor Unmoved(UnitEntry row, UnitAnchor start, int q, int r) => start;
        var missing = UnitRoster.FromState(rows, C, state, _ => false, Unmoved);
        Assert.IsFalse(missing.Any(t => t.Unit.Id == rowId), "no placeholder while the figure is missing");
        var arrived = UnitRoster.FromState(rows, C, state, id => id == model, Unmoved);
        var token = arrived.Single(t => t.Unit.Id == rowId);
        Assert.AreEqual(model, token.ArtType);
        Assert.AreEqual(side, token.Unit.Side);
        Assert.AreEqual(3, token.Figures);
        Assert.IsEmpty(token.Headgear, "no Israeli headgear substitution");
        Assert.IsEmpty(token.Vehicles, "infantry art does not introduce equipment");
        CollectionAssert.AreEqual(Enumerable.Repeat(model, 3), Enumerable.Range(0, 3).Select(i => UnitFigures.FigureFor(token.Unit, i, null)));
        CollectionAssert.AreEqual(new[] { rowId }, arrived.Where(t => t.Unit.Side != UnitData.Israel).Select(t => t.Unit.Id),
            "another side cannot borrow the arrived figure");
    }

    [Test]
    public void LegionAndAlaKeepTheirOriginalFiguresWithoutCreatingDeployments()
    {
        Assert.AreEqual("figure-c08-arab-legion-v2", UnitFigures.SideFigure("jordan"));
        Assert.IsNull(UnitFigures.SideFigure("local_arab_forces"), "no undeployed alias masks loss of the original campaign role");
        Assert.AreEqual("figure-c09-irregular-v2", UnitFigures.SideFigure("ala"));
        Assert.IsNull(UnitFigures.SideFigure("lebanon"), "the Lebanese candidate is review-only");
        var rows = Units;
        Assert.IsFalse(rows.Any(r => r.Side == "local_arab_forces"), "art does not author a local-force deployment");
        var lebanon = rows.Single(r => r.Id == "lebanese_army");
        Assert.IsNotNull(lebanon.Hidden);
        Assert.IsEmpty(lebanon.Anchors);
        Assert.IsFalse(UnitRoster.OnMap(lebanon));
        CollectionAssert.IsEmpty(C.StartingForces.Where(f => f.Side == "lebanon" || f.Side == "local_arab_forces"));
    }

    [Test]
    public void EgyptianFigureStaysInReviewUntilItsSimultaneousBasesHaveAnApprovedMapRepresentation()
    {
        const string id = "figure-egyptian-infantry-1948-v1", rowId = "egyptian_expeditionary_force";
        Assert.IsNull(UnitFigures.SideFigure("egypt"), "art alone does not decide how to draw simultaneous bases");
        Assert.IsNull(CollectionCatalog.BundleOf(id), "review art does not enter a runtime bundle");
        CollectionAssert.DoesNotContain(CollectionCatalog.Animated, id);
        var rows = Units;
        var egypt = rows.Single(r => r.Id == rowId);
        Assert.IsFalse(UnitRoster.OnMap(egypt));
        // Preserve the owner's simultaneous forward/rear bases at the first truce (#860, 17 October 2026).
        var truce = new DateTime(1948, 6, 11);
        CollectionAssert.AreEquivalent(new[] { "Isdud", "Majdal" }, egypt.Anchors
            .Where(a => UnitData.Day(a.From) <= truce && truce < UnitData.Day(a.To)).Select(a => a.Place.Split(',')[0]));
        Assert.AreEqual("egyptian_army", C.StartingForces.Single(f => f.Row == rowId).Type,
            "holding map art does not remove the simulation's existing force");
        UnitAnchor Unmoved(UnitEntry row, UnitAnchor start, int q, int r) => start;
        var arrived = UnitRoster.FromState(rows, C, Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical), _ => true, Unmoved);
        Assert.IsFalse(arrived.Any(t => t.Unit.Id == rowId), "even available art cannot invent a token placement");
    }

    [TestCase("figure-syrian-infantry-1948-v1")]
    [TestCase("figure-iraqi-infantry-1948-v1")]
    public void NearPrefabUsesItsOwnSkinnedMeshAndIdleClip(string id)
    {
        Assert.AreEqual(CollectionCatalog.People, CollectionCatalog.BundleOf(id));
        CollectionAssert.Contains(CollectionCatalog.Animated, id);
        string dir = $"{CollectionCatalog.Folder}/{id}", source = $"{dir}/{id}.fbx";
        var importer = AssetImporter.GetAtPath(source) as ModelImporter;
        Assert.IsNotNull(importer, source);
        Assert.AreEqual(ModelImporterAnimationType.Generic, importer!.animationType);
        Assert.IsTrue(importer.importAnimation);
        var assets = AssetDatabase.LoadAllAssetsAtPath(source);
        var mesh = assets.OfType<Mesh>().Single();
        Assert.Greater(mesh.bindposeCount, 10, "the delivered near mesh keeps its skin");
        Assert.IsTrue(mesh.HasVertexAttribute(VertexAttribute.BlendWeight));
        var clips = assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
        Assert.AreEqual(2, clips.Length, "both locally authored actions remain in the full export");
        CollectionAssert.AreEquivalent(new[] { "idle", "walk" }, clips.Select(c => c.name));
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));
        Assert.IsNotNull(prefab);
        var skin = prefab!.GetComponentInChildren<SkinnedMeshRenderer>(true);
        Assert.IsNotNull(skin, "an A-pose static renderer cannot masquerade as the rigged delivery");
        Assert.AreSame(mesh, skin!.sharedMesh);
        var animator = prefab.GetComponentInChildren<Animator>(true);
        Assert.IsNotNull(animator);
        var controller = animator!.runtimeAnimatorController as AnimatorController;
        Assert.IsNotNull(controller);
        Assert.AreEqual($"{dir}/{id}.controller", AssetDatabase.GetAssetPath(controller));
        Assert.AreEqual(1, controller!.layers.Length);
        var machine = controller.layers[0].stateMachine;
        Assert.AreEqual(1, machine.states.Length);
        Assert.IsNotNull(machine.defaultState);
        Assert.AreSame(clips.Single(c => c.name == "idle"), machine.defaultState.motion, "the default idle comes from this character's FBX");
        Assert.AreEqual(source, AssetDatabase.GetAssetPath(machine.defaultState.motion));
    }
}
