using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Prototype;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class CommandArtTests
{
    [Test] public void AllThirtyFourModelsLoadWithCompleteGeometryAndMaterials()
    {
        Assert.AreEqual(34, CommandArt.Catalog.Count);
        Assert.AreEqual(34, CommandArt.Catalog.Distinct(StringComparer.Ordinal).Count());
        foreach (string name in CommandArt.Catalog) {
            var prefab = CommandArt.Load(name); Assert.IsNotNull(prefab, name);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.IsNotEmpty(renderers, name);
            foreach (var renderer in renderers) {
                var mesh = renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                Assert.IsNotNull(mesh, name + "/" + renderer.name);
                Assert.Greater(mesh.vertexCount, 0, name); Assert.Greater(mesh.subMeshCount, 0, name);
                Assert.GreaterOrEqual(renderer.sharedMaterials.Length, mesh.subMeshCount, name);
                foreach (var material in renderer.sharedMaterials) {
                    Assert.IsNotNull(material, name); Assert.IsNotNull(material.shader, name);
                    Assert.AreNotEqual("Hidden/InternalErrorShader", material.shader.name, name);
                    if (material.HasProperty("_BaseMap")) Assert.IsNotNull(material.GetTexture("_BaseMap"), name + " base texture");
                }
            }
        }
    }
    [Test] public void UnitBindingsUseTheCorrectModelsAndNeverMakeACivilianTheEnemy()
    {
        StringAssert.Contains("soldier", CommandArt.ModelsFor(CommandKind.Brigade)[0]);
        StringAssert.Contains("scout", CommandArt.ModelsFor(CommandKind.Scout)[0]);
        StringAssert.Contains("builder", CommandArt.ModelsFor(CommandKind.Worker)[0]);
        StringAssert.Contains("truck", CommandArt.ModelsFor(CommandKind.Convoy)[0]);
        StringAssert.Contains("maabara", CommandArt.ModelsFor(CommandKind.Camp)[0]);
        StringAssert.Contains("kibbutz", CommandArt.ModelsFor(CommandKind.Settlement)[0]);
        Assert.IsEmpty(CommandArt.ModelsFor(CommandKind.Threat));
        foreach (CommandKind kind in Enum.GetValues(typeof(CommandKind)))
            foreach (string name in CommandArt.ModelsFor(kind)) CollectionAssert.Contains(CommandArt.Catalog, name);
    }
    [Test] public void GalleryRestoresPauseAndDestroysItsIndependentPreviewCamera()
    {
        var model = new CommandModel();
        var gallery = new CommandGallery(new UnityEngine.UIElements.VisualElement(), model);
        try {
            Assert.IsFalse(model.Paused); gallery.Open(); Assert.IsTrue(model.Paused); Assert.IsTrue(gallery.IsOpen);
            gallery.Close(); Assert.IsFalse(model.Paused); Assert.IsFalse(gallery.IsOpen);
            model.Paused = true; gallery.Open(); gallery.Close(); Assert.IsTrue(model.Paused);
        }
        finally { gallery.Dispose(); }
        Assert.IsNull(GameObject.Find("Command model inspection stage"));
        Assert.IsNull(GameObject.Find("Command model inspection camera"));
    }

    [Test] public void GalleryDisablesUnderlyingControlsAndRestoresTheirExactEnabledStates()
    {
        var parent = new VisualElement(); var enabled = new Button(); var disabled = new Button();
        parent.Add(enabled); parent.Add(disabled); disabled.SetEnabled(false);
        var gallery = new CommandGallery(parent, new CommandModel());
        try {
            gallery.Open(); Assert.IsFalse(enabled.enabledSelf); Assert.IsFalse(disabled.enabledSelf);
            Assert.IsTrue(parent.Q("command-model-gallery").Q<Button>().enabledInHierarchy);
            gallery.Close(); Assert.IsTrue(enabled.enabledSelf); Assert.IsFalse(disabled.enabledSelf);
            gallery.Open(); Assert.IsFalse(enabled.enabledSelf);
        }
        finally { gallery.Dispose(); }
        Assert.IsTrue(enabled.enabledSelf); Assert.IsFalse(disabled.enabledSelf);
        Assert.IsNull(parent.Q("command-model-gallery"));
    }

    [Test] public void ImportedInstanceFitsTheSymbolicFootprintWithoutChangingThePrefab()
    {
        var prefab = CommandArt.Load("sandwich-armoured-truck-v1"); Assert.IsNotNull(prefab);
        var before = prefab.transform.localScale; var host = new GameObject("Command art test");
        try {
            Assert.IsTrue(CommandArt.TryAttach(CommandKind.Convoy, host.transform, out _));
            var bounds = CommandArt.BoundsOf(host);
            Assert.AreEqual(1150, Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)), 1);
            Assert.AreEqual(0, bounds.min.y, 1); Assert.AreEqual(before, prefab.transform.localScale);
        }
        finally { UnityEngine.Object.DestroyImmediate(host); }
    }
}
