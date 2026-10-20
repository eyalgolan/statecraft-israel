#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Editor.Construction;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class ConstructionPreviewTests
{
    [Test]
    public void PreviewReplayAndDisposalLeaveTheCurrentSceneAndResourcesUnchanged()
    {
        var active = SceneManager.GetActiveScene(); bool dirty = active.isDirty;
        int previews = EditorSceneManager.previewSceneCount;
        var mode = RenderSettings.ambientMode; var sky = RenderSettings.ambientSkyColor; var sun = RenderSettings.sun;
        var target = RenderTexture.active;
        int Materials() => Resources.FindObjectsOfTypeAll<Material>().Count(m => m.name == "Construction dust (owned)");
        int owned = Materials();
        for (int i = 0; i < 3; i++)
        {
            using var stage = new ConstructionStage();
            stage.Reveal.Play(); stage.Reveal.Sample(0.65f);
            var image = stage.Frame(240, 180);
            try { Assert.That(image.width, Is.EqualTo(240)); }
            finally { Object.DestroyImmediate(image); }
            stage.Dispose(); stage.Dispose();
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(previews));
            Assert.That(Materials(), Is.EqualTo(owned));
        }
        Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
        Assert.That(active.isDirty, Is.EqualTo(dirty));
        Assert.That(RenderSettings.ambientMode, Is.EqualTo(mode));
        Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(sky));
        Assert.That(RenderSettings.sun, Is.EqualTo(sun));
        Assert.That(RenderTexture.active, Is.EqualTo(target));
    }
}
