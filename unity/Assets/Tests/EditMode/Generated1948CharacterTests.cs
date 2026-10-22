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

    [TestCase("syria", "syrian_army", "syrian_army", "figure-syrian-infantry-1948-v1")]
    [TestCase("ala", "arab_liberation_army", "arab_liberation_army", "figure-ala-infantry-1948-v1")]
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
    public void LegionAndLocalIrregularKeepTheirIdentitiesWithoutCreatingDeployments()
    {
        Assert.AreEqual("figure-c08-arab-legion-v2", UnitFigures.SideFigure("jordan"));
        Assert.AreEqual("figure-c09-irregular-v2", UnitFigures.SideFigure("local_arab_forces"));
        Assert.AreEqual("figure-ala-infantry-1948-v1", UnitFigures.SideFigure("ala"));
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
    [TestCase("figure-ala-infantry-1948-v1")]
    [TestCase("figure-iraqi-infantry-1948-v1")]
    public void NearPrefabUsesItsOwnSkinnedMeshAndIdleClip(string id)
    {
        Assert.AreEqual(CollectionCatalog.People, CollectionCatalog.BundleOf(id));
        CollectionAssert.Contains(CollectionCatalog.Animated, id);
        string dir = $"{CollectionCatalog.Folder}/{id}", source = $"{dir}/{id}-near.fbx";
        var importer = AssetImporter.GetAtPath(source) as ModelImporter;
        Assert.IsNotNull(importer, source);
        Assert.AreEqual(ModelImporterAnimationType.Generic, importer!.animationType);
        Assert.IsTrue(importer.importAnimation);
        var assets = AssetDatabase.LoadAllAssetsAtPath(source);
        var mesh = assets.OfType<Mesh>().Single();
        Assert.Greater(mesh.bindposeCount, 10, "the delivered near mesh keeps its skin");
        Assert.IsTrue(mesh.HasVertexAttribute(VertexAttribute.BlendWeight));
        var clips = assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
        Assert.AreEqual(1, clips.Length, "runtime delivery keeps only idle");
        Assert.AreEqual("idle", clips[0].name);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));
        Assert.IsNotNull(prefab);
        var skin = prefab!.GetComponentInChildren<SkinnedMeshRenderer>(true);
        Assert.IsNotNull(skin, "an A-pose static renderer cannot masquerade as the rigged delivery");
        Assert.AreSame(mesh, skin!.sharedMesh);
        var animator = prefab.GetComponentInChildren<Animator>(true);
        Assert.IsNotNull(animator);
        var controller = animator!.runtimeAnimatorController as AnimatorController;
        Assert.IsNotNull(controller);
        Assert.AreEqual($"{dir}/{id}-near.controller", AssetDatabase.GetAssetPath(controller));
        Assert.AreEqual(1, controller!.layers.Length);
        var machine = controller.layers[0].stateMachine;
        Assert.AreEqual(1, machine.states.Length);
        Assert.IsNotNull(machine.defaultState);
        Assert.AreSame(clips[0], machine.defaultState.motion, "the default idle comes from this character's FBX");
        Assert.AreEqual(source, AssetDatabase.GetAssetPath(machine.defaultState.motion));
    }
}
