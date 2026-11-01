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

    /// <summary>Owner, 27 October 2026: "Third figure (Recommended)". Tal Heuberger's ALA infantry streams in a bundle of its own,
    /// after the people's, and stands third on the ALA token; it never enlarges the people's bundle.</summary>
    [Test]
    public void AlaInfantryStreamsInItsOwnBundleAsTheAlaTokensThirdFigure()
    {
        const string id = "figure-ala-infantry-1948-v1";
        Assert.AreEqual("collection-" + id, CollectionCatalog.BundleOf(id));
        Assert.AreEqual(CollectionCatalog.People, CollectionCatalog.After["collection-" + id]);
        CollectionAssert.Contains(CollectionCatalog.Animated, id);
        Assert.AreEqual(id, UnitFigures.ThirdBySide["ala"]);
        var models = AssetDatabase.FindAssets(id).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".fbx", StringComparison.Ordinal)).ToArray();
        CollectionAssert.AreEqual(new[] { $"{CollectionCatalog.Folder}/{id}/{id}.fbx" }, models);
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
        Assert.AreEqual(TextureImporterCompression.Compressed, map.textureCompression);
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
    [TestCase("figure-egyptian-infantry-1948-v1")]
    [TestCase("figure-ala-infantry-1948-v1")]
    [TestCase("figure-bedouin-civilian-1948-v1")]
    [TestCase("figure-lebanese-regular-1948-v1")]
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
        CollectionAssert.AreEqual(new[] { UnitFigures.Irregular, UnitFigures.Irregular, UnitFigures.AlaInfantry },
            Enumerable.Range(0, 3).Select(i => UnitFigures.FigureFor(ala.Unit, i, null)), "two C09, the ALA infantry third");
        Assert.IsFalse(UnitFigures.BySide.Values.Contains(UnitFigures.AlaInfantry),
            "the token still waits only for C09, its side figure");
    }

    [TestCase("syria", "syrian_army", "syrian_army", "figure-syrian-infantry-1948-v1")]
    [TestCase("egypt", "egyptian_expeditionary_force", "egyptian_army", "figure-egyptian-infantry-1948-v1")]
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
        var third = UnitFigures.ThirdBySide.TryGetValue(side, out var t3) ? t3 : model;
        CollectionAssert.AreEqual(new[] { model, model, third }, Enumerable.Range(0, 3).Select(i => UnitFigures.FigureFor(token.Unit, i, null)));
        CollectionAssert.AreEqual(new[] { rowId }, arrived.Where(t => t.Unit.Side != UnitData.Israel).Select(t => t.Unit.Id),
            "another side cannot borrow the arrived figure");
    }

    [Test]
    public void LegionAndAlaKeepTheirOriginalFiguresWithoutCreatingDeployments()
    {
        Assert.AreEqual("figure-c08-arab-legion-v2", UnitFigures.SideFigure("jordan"));
        Assert.IsNull(UnitFigures.SideFigure("local_arab_forces"), "no undeployed alias masks loss of the original campaign role");
        Assert.AreEqual("figure-c09-irregular-v2", UnitFigures.SideFigure("ala"));
        var rows = Units;
        Assert.IsFalse(rows.Any(r => r.Side == "local_arab_forces"), "art does not author a local-force deployment");
        // The Lebanese row is a map-only token (LebanonIsAMapOnlyTokenAtAlMalikiyya): still no sim unit for it, nor for local forces.
        CollectionAssert.IsEmpty(C.StartingForces.Where(f => f.Side == "lebanon" || f.Side == "local_arab_forces"));
    }

    /// <summary>Owner, 27 October 2026: "Sign off all 19" and, for Egypt's forward and rear bases at the first truce, "One
    /// token". The GDD loop draws the one sim unit of the row, standing on Gaza from the first day; the legacy roster draws the
    /// row's first anchor whose window holds the date, so at the truce the forward base at Isdud, and Majdal stays in the
    /// row (owner, 17 October 2026, on #860).</summary>
    [Test]
    public void EgyptIsDrawnAsOneTokenWithItsOwnFigure()
    {
        const string id = "figure-egyptian-infantry-1948-v1", rowId = "egyptian_expeditionary_force";
        Assert.AreEqual(id, UnitFigures.SideFigure("egypt"));
        Assert.AreEqual("collection-" + id, CollectionCatalog.BundleOf(id), "its own bundle, not the Arab armies' or the people's");
        CollectionAssert.Contains(CollectionCatalog.Animated, id);
        var rows = Units;
        var egypt = rows.Single(r => r.Id == rowId);
        Assert.IsTrue(UnitRoster.OnMap(egypt));
        var truce = new DateTime(1948, 6, 11);
        CollectionAssert.AreEquivalent(new[] { "Isdud", "Majdal" }, egypt.Anchors
            .Where(a => UnitData.Day(a.From) <= truce && truce < UnitData.Day(a.To)).Select(a => a.Place.Split(',')[0]),
            "the row keeps both bases");
        Assert.AreEqual("egyptian_army", C.StartingForces.Single(f => f.Row == rowId).Type);
        UnitAnchor Unmoved(UnitEntry row, UnitAnchor start, int q, int r) => start;
        var gdd = UnitRoster.FromState(rows, C, Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical), _ => true, Unmoved)
            .Where(t => t.Unit.Id == rowId).ToList();
        Assert.AreEqual(1, gdd.Count, "one token for the one sim unit");
        Assert.AreEqual("Gaza", gdd[0].Anchor.Place);
        Assert.AreEqual(id, gdd[0].ArtType);
        var legacy = SimBridgeGddTests.Legacy;
        foreach (var (day, place) in new[] { (new DateTime(1948, 5, 15), "Gaza"), (truce, "Isdud"), (new DateTime(1948, 7, 7), "Isdud") })
        {
            var tokens = UnitRoster.Build(rows, legacy.Rules, day, 5, 0.8, _ => true).Where(t => t.Unit.Id == rowId).ToList();
            Assert.AreEqual(1, tokens.Count, $"{day:yyyy-MM-dd}: one token");
            Assert.AreEqual(place, tokens[0].Anchor.Place.Split(',')[0], $"{day:yyyy-MM-dd}");
        }
        Assert.IsFalse(UnitRoster.Build(rows, legacy.Rules, new DateTime(1948, 7, 8), 5, 0.8, _ => true).Any(t => t.Unit.Id == rowId),
            "no sourced anchor after the truce: no token");
    }

    /// <summary>Owner, 27 October 2026: "Sign off all 19" and, for Lebanon, "Map-only token (Recommended)"; 28 October 2026:
    /// "6 Jun to 8 Jul (Recommended)". The Lebanese army has no sim unit; its row is drawn with Tal Heuberger's Lebanese regular as a
    /// map-only token at al-Malikiyya from 6 June to 8 July 1948, in both loops, and it is the only Arab army row drawn so.</summary>
    [Test]
    public void LebanonIsAMapOnlyTokenAtAlMalikiyyaFrom6JuneTo8July()
    {
        const string id = "figure-lebanese-regular-1948-v1", rowId = "lebanese_army";
        Assert.AreEqual(id, UnitFigures.SideFigure("lebanon"));
        Assert.AreEqual("collection-" + id, CollectionCatalog.BundleOf(id), "its own bundle");
        Assert.AreEqual(CollectionCatalog.People, CollectionCatalog.After["collection-" + id], "opened after the people's");
        CollectionAssert.Contains(CollectionCatalog.Animated, id);
        var rows = Units;
        var lebanon = rows.Single(r => r.Id == rowId);
        Assert.IsNull(lebanon.Hidden);
        Assert.IsTrue(UnitRoster.OnMap(lebanon));
        var simRows = new HashSet<string>(C.StartingForces.Select(f => f.Row), StringComparer.Ordinal);
        CollectionAssert.AreEqual(new[] { rowId }, rows.Where(r => UnitRoster.OnMap(r) && UnitRoster.MapOnly(r, simRows)).Select(r => r.Id),
            "the only map-only army row");
        UnitAnchor Unmoved(UnitEntry row, UnitAnchor start, int q, int r) => start;
        var legacy = SimBridgeGddTests.Legacy;
        foreach (var (day, shown) in new[] { (new DateTime(1948, 6, 5), false), (new DateTime(1948, 6, 6), true), (new DateTime(1948, 7, 7), true), (new DateTime(1948, 7, 8), false) })
        {
            var state = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical); state.Date = day;
            var gdd = UnitRoster.FromState(rows, C, state, _ => true, Unmoved).Where(t => t.Unit.Id == rowId).ToList();
            var old = UnitRoster.Build(rows, legacy.Rules, day, 5, 0.8, _ => true).Where(t => t.Unit.Id == rowId).ToList();
            foreach (var (loop, tokens) in new[] { ("GDD", gdd), ("legacy", old) })
            {
                Assert.AreEqual(shown ? 1 : 0, tokens.Count, $"{loop} {day:yyyy-MM-dd}");
                if (!shown) continue;
                Assert.AreEqual("al-Malikiyya", tokens[0].Anchor.Place, $"{loop} {day:yyyy-MM-dd}");
                Assert.AreEqual(id, tokens[0].ArtType); Assert.AreEqual(UnitRoster.SideFigures, tokens[0].Figures);
                Assert.IsNull(tokens[0].StateUnit, $"{loop}: no sim unit, never moved or fought");
            }
            var waiting = UnitRoster.FromState(rows, C, state, f => f != id, Unmoved);
            Assert.IsFalse(waiting.Any(t => t.Unit.Id == rowId), $"{day:yyyy-MM-dd}: it waits for its own figure");
        }
    }

    [TestCase("figure-syrian-infantry-1948-v1", CollectionCatalog.ArabArmies)]
    [TestCase("figure-iraqi-infantry-1948-v1", CollectionCatalog.ArabArmies)]
    [TestCase("figure-egyptian-infantry-1948-v1", "collection-figure-egyptian-infantry-1948-v1")]
    [TestCase("figure-ala-infantry-1948-v1", "collection-figure-ala-infantry-1948-v1")]
    [TestCase("figure-bedouin-civilian-1948-v1", "collection-figure-bedouin-civilian-1948-v1")]
    [TestCase("figure-lebanese-regular-1948-v1", "collection-figure-lebanese-regular-1948-v1")]
    public void NearPrefabUsesItsOwnSkinnedMeshAndIdleClip(string id, string bundle)
    {
        Assert.AreEqual(bundle, CollectionCatalog.BundleOf(id), "a bundle of its own, not the opening people's (#1763)");
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
        // A figure that stands on unit tokens also walks with its own walk take (#1866); the Bedouin civilian stays idle only.
        bool token = id != "figure-bedouin-civilian-1948-v1";
        CollectionAssert.AreEquivalent(token ? new[] { "idle", "walk" } : new[] { "idle" }, machine.states.Select(s => s.state.name));
        if (token) Assert.AreSame(clips.Single(c => c.name == "walk"), machine.states.Single(s => s.state.name == "walk").state.motion);
        Assert.IsNotNull(machine.defaultState);
        Assert.AreSame(clips.Single(c => c.name == "idle"), machine.defaultState.motion, "the default idle comes from this character's FBX");
        Assert.AreEqual(source, AssetDatabase.GetAssetPath(machine.defaultState.motion));
    }
}
