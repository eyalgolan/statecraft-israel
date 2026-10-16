#nullable enable
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Sim;

/// <summary>Final review I4: a shareable build (Build.Web -shareable) is refused while any sensitive event lacks the owner's
/// sensitivity review, and the refusal names each one.</summary>
public class BuildGateTests
{
    static string? Refusal(Content c)
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor");
        Assert.IsNotNull(t, "Build exists");
        return (string?)t!.GetMethod("ShareableRefusal", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object?[] { c, null, false, null, null });   // event content only
    }

    [Test]
    public void AShareableBuildWaitsForTheSensitivityReview()
    {
        var c = ContentLoader.Load(Paths.Content);
        Assert.IsNull(Refusal(c), "the owner signed off all seven on 2026-10-10 (D11)");
        foreach (var e in c.Events) if (ReleaseGate.SensitiveEvents(c).Contains(e.Id)) e.Status = "fact-checked";   // as before the sign-off
        var refusal = Refusal(c);
        Assert.IsNotNull(refusal, "an unreviewed sensitive event blocks a shareable build");
        foreach (var id in ReleaseGate.SensitiveUnreviewed(c)) StringAssert.Contains(id, refusal);
        foreach (var e in c.Events) if (ReleaseGate.SensitiveEvents(c).Contains(e.Id)) e.Status = ReleaseGate.ReviewedStatus;   // as the owner's sign-off would
        Assert.IsNull(Refusal(c), "all reviewed: the build may be shared");
    }

    /// <summary>Rereview: the gate applies to every player build made with -shareable (Build.Web and Build.Mac call the same
    /// check); without the flag, dev builds stay ungated (web_check needs them).</summary>
    [Test]
    public void TheShareableFlagGatesEveryPlayerBuild()
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor")!;
        var gate = t.GetMethod("GateRefusal", BindingFlags.Public | BindingFlags.Static)!;
        var c = ContentLoader.Load(Paths.Content);
        Assert.IsNull(gate.Invoke(null, new object?[] { new[] { "Unity", "-executeMethod", "Statecraft.Game.EditorTools.Build.Mac" }, c, null, false, null, null }), "a dev build is not gated");
        Assert.IsNull(gate.Invoke(null, new object?[] { new[] { "Unity", "-shareable" }, c, null, false, null, null }), "all seven are sensitivity-reviewed (D11)");
        foreach (var e in c.Events) if (e.Id == "reparations_claim") e.Status = "fact-checked";
        StringAssert.Contains("reparations_claim", (string?)gate.Invoke(null, new object?[] { new[] { "Unity", "-shareable" }, c, null, false, null, null }));
        var source = System.IO.File.ReadAllText("Assets/Editor/Build.cs");
        foreach (var method in new[] { "public static void Web()", "public static void Mac()", "static void StandaloneMono(" })
        {
            var body = source.Substring(source.IndexOf(method, StringComparison.Ordinal), 400);
            StringAssert.Contains("RefuseUnlessShareable()", body, method + " checks the gate first");
        }
        foreach (var method in new[] { "public static void LinuxMono()", "public static void WindowsMono()" })
            StringAssert.Contains("=> StandaloneMono(", source.Substring(source.IndexOf(method, StringComparison.Ordinal), 120), method + " goes through the gated Mono build");
    }

    /// <summary>#482: Linux and Windows are built from the Mac on Mono (decisions O7, O8). The Standalone backend is one
    /// setting for all three desktop targets, so the Mono build must put the committed IL2CPP back afterwards.</summary>
    [Test]
    public void TheMonoPlayersRestoreTheCommittedBackend()
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor")!;
        foreach (var name in new[] { "Mac", "LinuxMono", "WindowsMono" })
            Assert.IsNotNull(t.GetMethod(name, BindingFlags.Public | BindingFlags.Static), name + " is an -executeMethod target");
        var source = System.IO.File.ReadAllText("Assets/Editor/Build.cs");
        var start = source.IndexOf("static void StandaloneMono(", StringComparison.Ordinal);
        var body = source.Substring(start, source.IndexOf("ExitIfFailed(ok);", start, StringComparison.Ordinal) - start);
        StringAssert.Contains("ScriptingImplementation.Mono2x", body);
        StringAssert.Contains("finally { ProjectSetup.Apply();", body, "IL2CPP is restored even when the build throws");
        Assert.AreEqual(UnityEditor.ScriptingImplementation.IL2CPP,
            UnityEditor.PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone), "the committed Standalone backend");
    }

    // Owner ruling 2026-10-11: the event photographs of sensitive events need the
    // owner's sensitivity review before a shareable build may carry them. The gate reads the catalog the build ships.
    static readonly string[] SensitivePhotos =
        { "altalena", "bernadotte_assassination", "operation_hiram", "operation_horev", "operation_yoav", "ten_days" };

    static Statecraft.Game.UI.EventImageCatalog ShippedPhotos()
    {
        var real = UnityEditor.AssetDatabase.LoadAssetAtPath<Statecraft.Game.UI.EventImageCatalog>("Assets/Resources/EventImageCatalog.asset");
        Assert.IsNotNull(real, "the shipped catalog (run EventImageImport.Run)");
        return UnityEngine.Object.Instantiate(real);
    }

    /// <summary>The catalog as it was before the owner's sign-off of 2026-10-11: every sensitive picture unreviewed.</summary>
    static void Unreview(Statecraft.Game.UI.EventImageCatalog photos)
    {
        for (int i = 0; i < photos.entries.Count; i++) { var e = photos.entries[i]; e.sensitivityReviewed = false; photos.entries[i] = e; }
    }

    /// <summary>The mapping as it was before the owner's sign-off.</summary>
    static System.Collections.Generic.IReadOnlyList<Statecraft.Game.UI.EventImageMapping.Row> UnreviewedMapping
    {
        get { var rows = Mapping; foreach (var r in rows) r.SensitivityReviewed = false; return rows; }
    }

    [Test]
    public void ShareableBuildsWaitForTheSensitivityReviewOfTheEventPhotographs()
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor")!;
        var gate = t.GetMethod("GateRefusal", BindingFlags.Public | BindingFlags.Static)!;
        var c = ContentLoader.Load(Paths.Content);
        var photos = ShippedPhotos();
        try
        {
            CollectionAssert.IsEmpty(photos.SensitiveUnreviewed(), "the owner signed off all six on 2026-10-11");
            Assert.IsNull(gate.Invoke(null, new object?[] { new[] { "Unity", "-shareable" }, c, photos, true, Mapping, null }), "the shipped catalog and mapping pass");
            Unreview(photos);
            CollectionAssert.AreEqual(SensitivePhotos, photos.SensitiveUnreviewed(), "exactly the owner's list");
            var refusal = (string?)gate.Invoke(null, new object?[] { new[] { "Unity", "-shareable" }, c, photos, false, null, null });
            Assert.IsNotNull(refusal, "unreviewed sensitive photographs block a shareable build");
            foreach (var id in SensitivePhotos) StringAssert.Contains(id, refusal);
            StringAssert.Contains("event photographs", refusal);
            Assert.IsNull(gate.Invoke(null, new object?[] { new[] { "Unity", "-executeMethod", "Statecraft.Game.EditorTools.Build.Web" }, c, photos, false, null, null }),
                "a dev build still carries them (the owner reviews them in one)");
            for (int i = 0; i < photos.entries.Count; i++)                       // as the owner's sign-off would
            {
                var e = photos.entries[i]; e.sensitivityReviewed = e.sensitive; photos.entries[i] = e;
            }
            Assert.IsNull(gate.Invoke(null, new object?[] { new[] { "Unity", "-shareable" }, c, photos, false, null, null }), "all reviewed: the build may be shared");
        }
        finally { UnityEngine.Object.DestroyImmediate(photos); }
    }

    [Test]
    public void TheShareableBuildChecksTheShippedCatalog()
    {
        var source = System.IO.File.ReadAllText("Assets/Editor/Build.cs");
        var body = source.Substring(source.IndexOf("static bool RefuseUnlessShareable()", StringComparison.Ordinal), 900);
        StringAssert.Contains("EventImageImport.CatalogPath", body, "the gate loads the catalog the build ships");
        StringAssert.Contains("File.Exists(EventImageImport.CatalogPath)", body, "a catalog that fails to load is not 'nothing to review'");
        StringAssert.Contains("EventImageMapping.Path", body, "the gate cross-checks the mapping");
    }

    static MethodInfo Gate => Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor")!.GetMethod("GateRefusal", BindingFlags.Public | BindingFlags.Static)!;
    static System.Collections.Generic.IReadOnlyList<Statecraft.Game.UI.EventImageMapping.Row> Mapping =>
        Statecraft.Game.UI.EventImageMapping.Parse(System.IO.File.ReadAllText(Statecraft.Game.UI.EventImageMapping.Path));
    static readonly string[] Shareable = { "Unity", "-shareable" };

    /// <summary>Review I5: the gate fails closed. A catalog file that exists but did not load refuses, and the sensitive
    /// rows of the mapping refuse even when the catalog has none.</summary>
    [Test]
    public void TheGateFailsClosedWhenTheCatalogDoesNotLoad()
    {
        var c = ContentLoader.Load(Paths.Content);
        var refusal = (string?)Gate.Invoke(null, new object?[] { Shareable, c, null, true, null, null });
        Assert.IsNotNull(refusal, "an existing catalog that did not load is a refusal");
        StringAssert.Contains("did not load", refusal);
        Assert.IsNull(Gate.Invoke(null, new object?[] { Shareable, c, null, false, null, null }), "no catalog file and no mapping: no photographs ship");
        var fromMapping = (string?)Gate.Invoke(null, new object?[] { Shareable, c, null, true, UnreviewedMapping, null });
        foreach (var id in SensitivePhotos) StringAssert.Contains(id, fromMapping, "the mapping's own sensitive rows refuse");
    }

    /// <summary>Review I5: a catalog edited by hand (or stale) against the mapping refuses, even when the edit marks every
    /// photograph reviewed.</summary>
    [Test]
    public void TheGateRefusesACatalogThatDoesNotMatchTheMapping()
    {
        var c = ContentLoader.Load(Paths.Content);
        var photos = ShippedPhotos();
        try
        {
            Assert.IsNull(Gate.Invoke(null, new object?[] { Shareable, c, photos, true, Mapping, null }), "the shipped catalog matches the mapping");
            Unreview(photos);
            for (int i = 0; i < photos.entries.Count; i++)                       // a hand edit, not the owner's sign-off
            {
                var e = photos.entries[i]; e.sensitivityReviewed = e.sensitive; photos.entries[i] = e;
            }
            Assert.IsNull(Gate.Invoke(null, new object?[] { Shareable, c, photos, true, null, null }), "the catalog alone would pass");
            var refusal = (string?)Gate.Invoke(null, new object?[] { Shareable, c, photos, true, UnreviewedMapping, null });
            Assert.IsNotNull(refusal, "the mapping still says unreviewed");
            StringAssert.Contains("does not match", refusal);
            foreach (var id in SensitivePhotos) StringAssert.Contains(id, refusal);
            var j = photos.entries.FindIndex(e => e.eventId == "maabarot_policy");
            var m = photos.entries[j]; m.credit = "someone else"; photos.entries[j] = m;
            StringAssert.Contains("maabarot_policy", (string?)Gate.Invoke(null, new object?[] { Shareable, c, photos, true, Mapping, null }), "a changed credit");
        }
        finally { UnityEngine.Object.DestroyImmediate(photos); }
    }

    /// <summary>Review I1: every build writes event-photos.json beside its output, which tools/package_release.py reads
    /// (the pictures are packed into the data files, where the packager cannot see them).</summary>
    [Test]
    public void EveryBuildWritesThePhotoManifestForThePackager()
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor")!;
        Assert.AreEqual("event-photos.json", t.GetField("PhotoManifestName", BindingFlags.Public | BindingFlags.Static)!.GetValue(null));
        var json = t.GetMethod("PhotoManifestJson", BindingFlags.Public | BindingFlags.Static)!;
        var photos = ShippedPhotos();
        try
        {
            var dev = (string)json.Invoke(null, new object?[] { photos, false })!;
            StringAssert.IsMatch("\"shareable\":\\s*false", dev);
            foreach (var id in SensitivePhotos)
                StringAssert.IsMatch($"\"event_id\":\\s*\"{id}\",\\s*\"sensitive\":\\s*true,\\s*\"sensitivity_reviewed\":\\s*true", dev);
            Assert.AreEqual(25, System.Text.RegularExpressions.Regex.Matches(dev, "\"event_id\"").Count);   // first_new_yishuv came with #380
            StringAssert.IsMatch("\"shareable\":\\s*true", (string)json.Invoke(null, new object?[] { photos, true })!);
            StringAssert.IsMatch("\"photos\":\\s*\\[\\s*\\]", (string)json.Invoke(null, new object?[] { null, true })!);
        }
        finally { UnityEngine.Object.DestroyImmediate(photos); }
        var source = System.IO.File.ReadAllText("Assets/Editor/Build.cs");
        foreach (var method in new[] { "public static void Web()", "public static void Mac()" })
        {
            var body = source.Substring(source.IndexOf(method, StringComparison.Ordinal), 900);
            StringAssert.Contains("WritePhotoManifest(", body, method + " writes the manifest");
        }
    }
}
