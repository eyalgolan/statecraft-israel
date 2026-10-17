#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using YamlDotNet.RepresentationModel;

/// <summary>The event -> photograph mapping (Assets/UI/EventImages/event-images.yaml; owner rulings 2026-10-11): sound
/// against the content, outside the hashed content, matched by the shipped catalog, the files and ASSETS.yaml.</summary>
public class EventImageMappingTests
{
    const string CatalogPath = "Assets/Resources/EventImageCatalog.asset";
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static IReadOnlyList<EventImageMapping.Row> Rows => EventImageMapping.Parse(File.ReadAllText(EventImageMapping.Path));

    /// <summary>No free photograph was found for these (docs/event-images.md): they keep the minister's portrait plate.</summary>
    static readonly string[] Fallback = { "armistice_lebanon", "armistice_syria", "education_registration_1951", "forex_crisis_1951", "reparations_claim", "sinai_withdrawal", "rhodes_talks" };
    /// <summary>Photographs whose ASSETS.yaml row waits for the owner's sign-off (#380, "What needs maintainer sign-off"). When the
    /// owner signs one off, its row becomes verified and its name leaves this list.</summary>
    static readonly string[] AwaitingSignOff = { };   // first_new_yishuv signed off 17 Oct 2026 ("Approve both", #879; #913)
    static readonly string[] SensitivePhotos =
        { "altalena", "bernadotte_assassination", "operation_hiram", "operation_horev", "operation_yoav", "ten_days" };

    [Test]
    public void TheMappingIsSoundAgainstTheContent()
    {
        var rows = Rows;
        Assert.AreEqual(25, rows.Count);   // first_new_yishuv's photograph came with #380
        CollectionAssert.IsEmpty(EventImageMapping.Problems(rows, C));
        CollectionAssert.AreEquivalent(C.Events.Select(e => e.Id).Except(Fallback), rows.Select(r => r.Event), "every event but the fallbacks has a picture");
        CollectionAssert.AreEquivalent(SensitivePhotos, rows.Where(r => r.Sensitive).Select(r => r.Event), "the owner's sensitive list");
        CollectionAssert.AreEquivalent(SensitivePhotos, rows.Where(r => r.SensitivityReviewed).Select(r => r.Event), "the owner's sign-off of 2026-10-11, and nothing else");
    }

    [Test]
    public void ProblemsAreFound()
    {
        EventImageMapping.Row Good() => new EventImageMapping.Row
        {
            Event = "maabarot_policy", File = "maabarot_policy.jpg", Commons = "https://commons.wikimedia.org/wiki/File:X.jpg", Sha1 = new string('a', 40),
            Licence = "CC-BY-4.0", Artist = "Benno Rothenberg", Credit = "Benno Rothenberg / Meitar Collection · CC BY 4.0 · graded and cropped",
            Alt = "A tent ma'abara on the road to Jerusalem, 1950.", Crop = new List<double> { 0.5, 0.5, 1 },
        };
        CollectionAssert.IsEmpty(EventImageMapping.Problems(new[] { Good() }, C));
        void Bad(Action<EventImageMapping.Row> edit, string expected)
        {
            var r = Good(); edit(r);
            var problems = EventImageMapping.Problems(new[] { r }, C);
            Assert.IsTrue(problems.Any(p => p.Contains(expected)), $"'{expected}' expected in: {string.Join(" | ", problems)}");
        }
        Bad(r => r.Licence = "CC-BY-SA-4.0", "is not public domain, CC0 or CC BY 4.0");
        Bad(r => r.Licence = "unknown", "is not public domain, CC0 or CC BY 4.0");
        Bad(r => r.Credit = "Benno Rothenberg · CC BY 4.0", "graded and cropped");
        Bad(r => r.Credit = "Meitar Collection · CC BY 4.0 · graded and cropped", "does not name the artist");
        Bad(r => r.Credit = "Benno Rothenberg · graded and cropped", "does not name the licence");
        Bad(r => r.Alt = "", "alt text");
        Bad(r => r.Event = "no_such_event", "no such event");
        Bad(r => { r.Event = "altalena"; r.File = "altalena.jpg"; }, "must be sensitive: true");   // tagged war-casualties
        var twice = EventImageMapping.Problems(new[] { Good(), Good() }, C);
        Assert.IsTrue(twice.Any(p => p.Contains("more than one row")));
    }

    /// <summary>The pictures live outside content/ and nothing in content/ names them, so the content hash, the goldens and
    /// save compatibility cannot change with them (the goldens and SaveGameTests run unchanged in the same suite).</summary>
    [Test]
    public void TheMappingIsNotGameContent()
    {
        var content = Path.GetFullPath(Paths.Content);
        Assert.IsFalse(Path.GetFullPath(EventImageMapping.Path).StartsWith(content, StringComparison.Ordinal));
        foreach (var f in Directory.GetFiles(Path.Combine(content, "events"), "*.yaml"))
            StringAssert.DoesNotContain("EventImages", File.ReadAllText(f), f);
    }

    [Test]
    public void EveryPictureIsAShippedSixteenByNineFileAndEveryFileHasARow()
    {
        var rows = Rows;
        var files = Directory.GetFiles(EventImageMapping.Folder, "*.jpg").Select(f => Path.GetFileName(f)).OrderBy(x => x).ToArray();
        CollectionAssert.AreEqual(rows.Select(r => r.File).OrderBy(x => x).ToArray(), files);
        foreach (var r in rows)
        {
            var path = $"{EventImageMapping.Folder}/{r.File}";
            Assert.LessOrEqual(new FileInfo(path).Length, 450 * 1024, path);   // tools/event_images.py MAX_BYTES
            var tex = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(ImageConversion.LoadImage(tex, File.ReadAllBytes(path)), path);
                Assert.AreEqual(tex.width * 9, tex.height * 16, $"{path}: 16:9, the media header ({tex.width}x{tex.height})");
                Assert.AreEqual(0, tex.width % 4 + tex.height % 4, $"{path}: multiples of 4 for block compression");
                Assert.GreaterOrEqual(tex.width, 600, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(tex); }
        }
    }

    [Test]
    public void TheShippedCatalogMatchesTheMapping()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<EventImageCatalog>(CatalogPath);
        Assert.IsNotNull(catalog, CatalogPath + " (run EventImageImport.Run)");
        Assert.AreSame(catalog, Resources.Load<EventImageCatalog>(EventImageCatalog.ResourcePath), "the game loads it from Resources");
        var rows = Rows.OrderBy(r => r.Event, StringComparer.Ordinal).ToList();
        Assert.AreEqual(rows.Count, catalog.entries.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            var (r, e) = (rows[i], catalog.entries[i]);
            Assert.AreEqual(r.Event, e.eventId);
            Assert.AreEqual($"{EventImageMapping.Folder}/{r.File}", AssetDatabase.GetAssetPath(e.image), r.Event);
            Assert.AreEqual(r.Alt, e.alt, r.Event);
            Assert.AreEqual(r.Credit, e.credit, r.Event);
            Assert.AreEqual(r.Sensitive, e.sensitive, r.Event);
            Assert.AreEqual(r.SensitivityReviewed, e.sensitivityReviewed, r.Event);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath($"{EventImageMapping.Folder}/{rows[0].File}");
        Assert.IsFalse(importer.mipmapEnabled);
        Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, "kept at 16:9");
    }

    /// <summary>Every picture has its own ASSETS.yaml row with the same licence, source page and credit, still owner-verify
    /// (the release gate's check_repo --release refuses those rows until the owner verifies them).</summary>
    [Test]
    public void EveryPictureHasItsProvenanceRow()
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(File.ReadAllText(Path.Combine(Paths.RepoRoot, "ASSETS.yaml"))));
        static string Get(YamlMappingNode m, string key) => m.Children.TryGetValue(new YamlScalarNode(key), out var v) ? v.ToString() : "";
        var root = (YamlMappingNode)yaml.Documents[0].RootNode;
        var rules = ((YamlSequenceNode)root.Children[new YamlScalarNode("rules")]).Children.Cast<YamlMappingNode>().ToDictionary(m => Get(m, "glob"));
        foreach (var r in Rows)
        {
            var glob = $"unity/{EventImageMapping.Folder}/{r.File}";
            Assert.IsTrue(rules.TryGetValue(glob, out var rule), glob + " has no ASSETS.yaml row");
            Assert.AreEqual(r.Licence, Get(rule!, "licence"), glob);
            Assert.AreEqual(r.Commons, Get(rule!, "source"), glob);
            Assert.AreEqual(r.Credit, Get(rule!, "credit"), glob);
            // The owner's sign-off of 2026-10-11 (docs/event-images.md) covers every picture but the ones still waiting for it.
            Assert.AreEqual(AwaitingSignOff.Contains(r.Event) ? "owner-verify" : "verified", Get(rule!, "status"), glob);
            Assert.AreEqual("graded and cropped", Get(rule!, "adapted"), glob);
            if (r.Sensitive) StringAssert.Contains("sensitivity review", Get(rule!, "note"), glob);
        }
    }
}
