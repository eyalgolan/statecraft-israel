#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using YamlDotNet.RepresentationModel;
using Object = UnityEngine.Object;

/// <summary>The opening's build gate (#530): a shareable build refuses an opening photograph without the owner's sensitivity
/// review, and draft text or an unchecked source (owner, "Photos and text"); the photo manifest lists the three photographs
/// so the packager checks them too. Fixtures change cloned objects and YAML in memory only, never the reviews on disk.
/// Build is in the Editor assembly, so it is reached by reflection, as BuildGateTests does.</summary>
public class OpeningBuildGateTests
{
    readonly List<Object> owned = new List<Object>();
    static Type BuildType()
    {
        var type = Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor");
        Assert.IsNotNull(type, "the real Editor Build assembly must be compiled and loaded");
        return type!;
    }
    static MethodInfo Require(string name)
    {
        var method = BuildType().GetMethod(name, BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(method, "Build." + name + " must exist (#530)");
        return method!;
    }
    static YamlMappingNode Read(string text)
    {
        var yaml = new YamlStream(); yaml.Load(new StringReader(text));
        return (YamlMappingNode)yaml.Documents[0].RootNode;
    }
    static YamlNode Get(YamlMappingNode map, string key) => map.Children[new YamlScalarNode(key)];
    static void Set(YamlMappingNode map, string key, string value) => map.Children[new YamlScalarNode(key)] = new YamlScalarNode(value);
    static List<YamlMappingNode> Panels(YamlMappingNode doc) => ((YamlSequenceNode)Get(doc, "panels")).Children.Cast<YamlMappingNode>().ToList();
    static List<YamlMappingNode> Sources(YamlMappingNode paragraph) => ((YamlSequenceNode)Get(paragraph, "sources")).Children.Cast<YamlMappingNode>().ToList();
    static List<YamlMappingNode> Photos(YamlMappingNode json) => ((YamlSequenceNode)Get(json, "photos")).Children.Cast<YamlMappingNode>().ToList();

    OpeningContent Fixture(Action<YamlMappingNode>? change = null)
    {
        var shipped = AssetDatabase.LoadAssetAtPath<OpeningContent>("Assets/Resources/OpeningContent.asset");
        Assert.IsNotNull(shipped, "the product's reference-only catalog");
        var copy = Object.Instantiate(shipped); owned.Add(copy);
        copy.photos = shipped.photos.ToArray();
        var yaml = new YamlStream(); yaml.Load(new StringReader(shipped.source.text));
        var doc = (YamlMappingNode)yaml.Documents[0].RootNode;
        Set(doc, "status", "sensitivity-reviewed");
        foreach (var paragraph in Panels(doc).Concat(new[] { (YamlMappingNode)Get(doc, "closing") }))
            foreach (var source in Sources(paragraph)) Set(source, "checked", "2026-10-22");
        foreach (var panel in Panels(doc))
        {
            var image = (YamlMappingNode)Get(panel, "image");
            Set(image, "sensitive", "true"); Set(image, "sensitivity_reviewed", "true");
        }
        change?.Invoke(doc);
        using var writer = new StringWriter(); yaml.Save(writer, false);
        var text = new TextAsset(writer.ToString()); owned.Add(text); copy.source = text;
        return copy;   // a fully checked fixture is not a human approval of the shipped data
    }
    [TearDown]
    public void Clean() { for (int i = owned.Count - 1; i >= 0; i--) Object.DestroyImmediate(owned[i]); owned.Clear(); }

    [TestCase("draft status")]
    [TestCase("unchecked source")]
    public void GatedTextRefusesTheShareableBuild(string gap)
    {
        var gate = Require("OpeningShareableRefusal");
        var draft = Fixture(doc =>
        {
            if (gap == "draft status") Set(doc, "status", "draft");
            else Sources((YamlMappingNode)Get(doc, "closing"))[0].Children.Remove(new YamlScalarNode("checked"));
        });
        Assert.IsTrue(draft.Read().PhotosReviewed, "precondition: reviewed photographs, so only the text is gated");
        Assert.IsNotNull(gate.Invoke(null, new object?[] { draft }), gap + ": gated text must not reach a shareable build");
    }

    [Test]
    public void FullyReviewedMemoryFixtureWithAllMediaCanPass()
    {
        var gate = Require("OpeningShareableRefusal");
        var reviewed = Fixture();
        Assert.IsTrue(reviewed.Read().Reviewed, "precondition: fixture only, not a signed source file");
        Assert.IsNull(gate.Invoke(null, new object?[] { reviewed }));
    }

    [TestCase("photo review")]
    [TestCase("photo reference")]
    public void AnUnreviewedOrMissingPhotographRefuses(string gap)
    {
        var gate = Require("OpeningShareableRefusal");
        var damaged = Fixture(doc =>
        {
            if (gap == "photo review") Set((YamlMappingNode)Get(Panels(doc)[0], "image"), "sensitivity_reviewed", "false");
        });
        if (gap == "photo reference") damaged.photos[1] = null!;
        Assert.IsNotNull(gate.Invoke(null, new object?[] { damaged }), gap + " must not become shareable");
    }

    [Test]
    public void TheManifestListsTheThreeOpeningPhotosAndIsNotShareableWhileOneIsUnreviewed()
    {
        var manifest = Require("PhotoManifestWithOpeningJson");
        var unreviewed = Fixture(doc => Set((YamlMappingNode)Get(Panels(doc)[2], "image"), "sensitivity_reviewed", "false"));
        var doc = Read((string)manifest.Invoke(null, new object?[] { null, true, unreviewed })!);
        Assert.AreEqual("false", Get(doc, "shareable").ToString(), "a requested shareable flag cannot pass an unreviewed photograph");
        var rows = Photos(doc);
        CollectionAssert.AreEqual(new[] { "opening/europe", "opening/arab_countries", "opening/yemen" },
            rows.Select(p => Get(p, "event_id").ToString()));
        CollectionAssert.AreEqual(new[] { "true", "true", "false" }, rows.Select(p => Get(p, "sensitivity_reviewed").ToString()),
            "the packager sees which photograph waits");
    }

    [Test]
    public void ReviewedManifestRetainsEveryEventPhotoAlongsideTheOpening()
    {
        var manifest = Require("PhotoManifestWithOpeningJson");
        var photos = AssetDatabase.LoadAssetAtPath<EventImageCatalog>("Assets/Resources/EventImageCatalog.asset");
        Assert.IsNotNull(photos);
        var reviewed = Fixture();
        var doc = Read((string)manifest.Invoke(null, new object?[] { photos, true, reviewed })!);
        var actual = Photos(doc).Select(p => Get(p, "event_id").ToString()).ToArray();
        var expected = photos.entries.Where(p => p.image != null).Select(p => p.eventId)
            .Concat(new[] { "opening/europe", "opening/arab_countries", "opening/yemen" }).ToArray();
        CollectionAssert.AreEquivalent(expected, actual);
        Assert.AreEqual("true", Get(doc, "shareable").ToString(), "positive fixture guards against an always-false implementation");
    }

    [Test]
    public void RealBuildManifestWriterUsesTheOpeningAwareFunction()
    {
        Require("PhotoManifestWithOpeningJson");
        var writer = BuildType().GetMethod("WritePhotoManifest", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(writer, "the production writer used by Web/Mac/Mono builds");
        Assert.IsFalse(Environment.GetCommandLineArgs().Contains("-shareable"), "this test is a normal Native test, not a shareable build");
        var source = File.ReadAllBytes(OpeningContent.SourcePath);
        var folder = Path.Combine(Path.GetTempPath(), "statecraft-opening-manifest-" + Guid.NewGuid().ToString("N"));
        try
        {
            writer!.Invoke(null, new object[] { folder });
            var doc = Read(File.ReadAllText(Path.Combine(folder, "event-photos.json")));
            Assert.AreEqual("false", Get(doc, "shareable").ToString(), "a build without -shareable is never shareable");
            CollectionAssert.AreEqual(new[] { "opening/europe", "opening/arab_countries", "opening/yemen" },
                Photos(doc).Select(p => Get(p, "event_id").ToString()).Where(id => id.StartsWith("opening/", StringComparison.Ordinal)));
            CollectionAssert.AreEqual(source, File.ReadAllBytes(OpeningContent.SourcePath), "writer cannot sign off or rewrite the opening");
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
