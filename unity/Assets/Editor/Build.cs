#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Headless player builds. Map.unity is the boot scene; ContentSync copies content/ into StreamingAssets
    /// first (IPreprocessBuildWithReport), and CollectionImport.BuildBundles puts The 1948 Collection's streamed bundles in
    /// StreamingAssets/collection. Exits non-zero on failure.
    ///   Web: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web
    ///        [-compression disabled|gzip|brotli] [-decompressionFallback] [-out Builds/Web-disabled] [-textureSubtarget dxt|etc2|astc]
    ///        (-decompressionFallback: Unity's Decompression Fallback, for hosts that cannot set response headers,
    ///        like GitHub Pages; the website's demo build uses it, docs/RELEASING.md)
    ///        (-textureSubtarget: the compressed format of every automatic texture; the default build is DXT, for desktop
    ///        browsers; astc or etc2 makes a build for browsers without S3TC, as on phones)
    ///   Mac: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget OSXUniversal -executeMethod Statecraft.Game.EditorTools.Build.Mac
    ///        [-out Builds/Mac/Statecraft.app] [-ffp-contract-off]
    ///   Linux (Mono): ... -buildTarget Linux64 -executeMethod Statecraft.Game.EditorTools.Build.LinuxMono [-out Builds/Linux/Statecraft.x86_64]
    ///   Windows (Mono): ... -buildTarget Win64 -executeMethod Statecraft.Game.EditorTools.Build.WindowsMono [-out Builds/Windows/Statecraft.exe]
    ///   Every player takes -shareable (the sensitivity gate). tools/release_build.py runs these under the machine lock.</summary>
    public static class Build
    {
        static string? Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        static bool Flag(string name) => Environment.GetCommandLineArgs().Contains(name);

        public static WebGLCompressionFormat Compression(string? name) => (name ?? "disabled") switch
        {
            "disabled" => WebGLCompressionFormat.Disabled,
            "gzip" => WebGLCompressionFormat.Gzip,
            "brotli" => WebGLCompressionFormat.Brotli,
            _ => throw new ArgumentException($"-compression must be disabled, gzip or brotli (got '{name}')"),
        };

        /// <summary>The WebGL texture subtarget named by -textureSubtarget (Unity's own default when absent).</summary>
        public static WebGLTextureSubtarget? TextureSubtarget(string? name) => name switch
        {
            null => null,
            "dxt" => WebGLTextureSubtarget.DXT,
            "etc2" => WebGLTextureSubtarget.ETC2,
            "astc" => WebGLTextureSubtarget.ASTC,
            _ => throw new ArgumentException($"-textureSubtarget must be dxt, etc2 or astc (got '{name}')"),
        };

        /// <summary>Why a shareable build may not be made, or null: every event with a sensitive tag needs the owner's
        /// sensitivity review first (status sensitivity-reviewed; docs/m3-release.md, M3 final review I4), and so does every
        /// sensitive event photograph the build would carry (EventImageCatalog; docs/event-images.md), and every sensitive
        /// illustration clip (<paramref name="clips"/>, event-videos.yaml; docs/event-videos.md). See
        /// <see cref="EventImageRefusal"/> for <paramref name="catalogFileExists"/> and <paramref name="mapping"/>.</summary>
        public static string? ShareableRefusal(Statecraft.Sim.Content c, EventImageCatalog? photos, bool catalogFileExists = false,
            IReadOnlyList<EventImageMapping.Row>? mapping = null, IReadOnlyList<EventVideoMapping.Row>? clips = null)
        {
            var open = Statecraft.Sim.ReleaseGate.SensitiveUnreviewed(c);
            var reasons = new List<string>();
            if (open.Count > 0)
                reasons.Add("a shareable build needs the owner's sensitivity review of: " + string.Join(", ", open) + " (set status: sensitivity-reviewed; see docs/m3-release.md)");
            if (EventImageRefusal(photos, catalogFileExists, mapping) is string pictures) reasons.Add(pictures);
            if (clips != null && EventVideoMapping.SensitiveUnreviewed(clips) is { Count: > 0 } openClips)
                reasons.Add("a shareable build needs the owner's sensitivity review of the illustration clips: " + string.Join(", ", openClips)
                    + " (set sensitivity_reviewed: true in " + EventVideoMapping.Path + "; see docs/event-videos.md)");
            return reasons.Count == 0 ? null : string.Join("; ", reasons);
        }

        /// <summary>Why the event photographs may not ship in a shareable build, or null: a picture marked sensitive needs
        /// the owner's sensitivity review (sensitivity_reviewed: true in Assets/UI/EventImages/event-images.yaml, then
        /// EventImageImport.Run). An OWNER step, like the event review. It fails closed (review I5): a catalog file that
        /// exists but did not load is a refusal, not "nothing to review"; and when the mapping is given, a sensitive and
        /// unreviewed row there refuses too, as does any row the catalog does not match (a hand-edited or stale catalog).</summary>
        public static string? EventImageRefusal(EventImageCatalog? photos, bool catalogFileExists = false,
            IReadOnlyList<EventImageMapping.Row>? mapping = null)
        {
            var reasons = new List<string>();
            if (photos == null && catalogFileExists)
                reasons.Add($"the event photograph catalog {EventImageImport.CatalogPath} exists but did not load (fix the import, then run EventImageImport)");
            var open = new SortedSet<string>(StringComparer.Ordinal);
            if (photos != null) open.UnionWith(photos.SensitiveUnreviewed());
            if (mapping != null) open.UnionWith(mapping.Where(r => r.Sensitive && !r.SensitivityReviewed).Select(r => r.Event));
            if (open.Count > 0)
                reasons.Add("a shareable build needs the owner's sensitivity review of the event photographs: " + string.Join(", ", open)
                    + " (set sensitivity_reviewed: true in " + EventImageMapping.Path + ", run EventImageImport; see docs/event-images.md)");
            if (mapping != null && photos != null && CatalogMismatches(photos, mapping) is { Count: > 0 } stale)
                reasons.Add("the event photograph catalog does not match " + EventImageMapping.Path + " for: " + string.Join(", ", stale)
                    + " (run EventImageImport)");
            return reasons.Count == 0 ? null : string.Join("; ", reasons);
        }

        /// <summary>The events whose catalog entry and mapping row disagree (one is missing, or the credit, the alt text or a
        /// sensitivity flag differs), in id order.</summary>
        public static List<string> CatalogMismatches(EventImageCatalog photos, IReadOnlyList<EventImageMapping.Row> mapping)
        {
            var rows = mapping.GroupBy(r => r.Event).ToDictionary(g => g.Key, g => g.First());
            var entries = photos.entries.GroupBy(e => e.eventId ?? "").ToDictionary(g => g.Key, g => g.First());
            var bad = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var id in rows.Keys.Union(entries.Keys))
            {
                if (!rows.TryGetValue(id, out var r) || !entries.TryGetValue(id, out var e) || e.image == null) { bad.Add(id); continue; }
                if (e.credit != r.Credit || e.alt != r.Alt || e.sensitive != r.Sensitive || e.sensitivityReviewed != r.SensitivityReviewed) bad.Add(id);
            }
            if (photos.entries.Count != entries.Count || mapping.Count != rows.Count) bad.Add("(repeated rows)");
            return bad.ToList();
        }

        /// <summary>The gate for a build made with these command-line args: a -shareable build (Web or Mac) is refused
        /// until the reviews are done; a dev build is never gated (web_check and the golden checks need them).</summary>
        public static string? GateRefusal(string[] args, Statecraft.Sim.Content c, EventImageCatalog? photos, bool catalogFileExists = false,
            IReadOnlyList<EventImageMapping.Row>? mapping = null, IReadOnlyList<EventVideoMapping.Row>? clips = null) =>
            Array.IndexOf(args, "-shareable") >= 0 ? ShareableRefusal(c, photos, catalogFileExists, mapping, clips) : null;

        /// <summary>The clips a shareable build is gated on, from the mapping's text: the card loops and the End Turn outcome
        /// clips.</summary>
        public static List<EventVideoMapping.Row> GatedClips(string yaml) =>
            EventVideoMapping.Parse(yaml).Concat(EventVideoMapping.ParseOutcomes(yaml)).ToList();

        /// <summary>Logs "BUILD REFUSED ..." and exits 1 when the gate refuses this build; true if it did. The photographs
        /// checked are the catalog the build ships (Resources/EventImageCatalog.asset), cross-checked against the mapping.</summary>
        static bool RefuseUnlessShareable()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-shareable") < 0) return false;
            var photos = AssetDatabase.LoadAssetAtPath<EventImageCatalog>(EventImageImport.CatalogPath);
            string? refusal;
            try
            {
                var mapping = EventImageMapping.Parse(File.ReadAllText(EventImageMapping.Path));
                var videos = File.ReadAllText(EventVideoMapping.Path);   // fails closed: a missing file is a refusal
                var clips = GatedClips(videos);
                refusal = GateRefusal(args, Statecraft.Sim.ContentLoader.Load(Paths.Content), photos, File.Exists(EventImageImport.CatalogPath), mapping, clips);
            }
            catch (Exception e) when (e is IOException || e is InvalidOperationException || e is UnauthorizedAccessException)
            {
                refusal = "cannot read the event photograph or clip mapping (" + EventImageMapping.Path + ", " + EventVideoMapping.Path + "): " + e.Message;
            }
            if (refusal == null) return false;
            Debug.LogError("BUILD REFUSED " + refusal);
            EditorApplication.Exit(1);
            return true;
        }

        /// <summary>The name of the manifest every build writes beside its output (review I1): which event photographs it
        /// carries, whether each is sensitive and reviewed, and whether it was a -shareable build. tools/package_release.py
        /// refuses a build folder without it, or with an unreviewed sensitive photograph, or not built -shareable; the
        /// pictures themselves are packed into Unity's data files, where the packager cannot see them.</summary>
        public const string PhotoManifestName = "event-photos.json";

        [Serializable] sealed class PhotoManifest { public int version = 1; public bool shareable; public List<PhotoRecord> photos = new List<PhotoRecord>(); }
        [Serializable] sealed class PhotoRecord { public string event_id = ""; public bool sensitive; public bool sensitivity_reviewed; }

        /// <summary>The manifest's JSON for a build carrying <paramref name="photos"/> (null: none).</summary>
        public static string PhotoManifestJson(EventImageCatalog? photos, bool shareable)
        {
            var m = new PhotoManifest { shareable = shareable };
            if (photos != null)
                foreach (var e in photos.entries.Where(e => e.image != null).OrderBy(e => e.eventId, StringComparer.Ordinal))
                    m.photos.Add(new PhotoRecord { event_id = e.eventId, sensitive = e.sensitive, sensitivity_reviewed = e.sensitivityReviewed });
            return JsonUtility.ToJson(m, true) + "\n";
        }

        /// <summary>Writes the manifest into <paramref name="folder"/> (the WebGL build folder; for the Mac app, the folder
        /// holding it, so the signed bundle is not touched).</summary>
        static void WritePhotoManifest(string folder)
        {
            Directory.CreateDirectory(folder);
            var photos = AssetDatabase.LoadAssetAtPath<EventImageCatalog>(EventImageImport.CatalogPath);
            File.WriteAllText(Path.Combine(folder, PhotoManifestName), PhotoManifestJson(photos, Flag("-shareable")));
        }

        public static void Web()
        {
            if (RefuseUnlessShareable()) return;
            var name = Arg("-compression") ?? "disabled";
            ProjectSetup.Apply();
            PlayerSettings.WebGL.compressionFormat = Compression(name);
            PlayerSettings.WebGL.decompressionFallback = Flag("-decompressionFallback");   // off by default: itch.io and web_check send .gz/.br with Content-Encoding (#879); Pages needs the fallback
            var savedSubtarget = EditorUserBuildSettings.webGLBuildSubtarget;
            var sub = TextureSubtarget(Arg("-textureSubtarget"));
            if (sub.HasValue) EditorUserBuildSettings.webGLBuildSubtarget = sub.Value;
            bool ok;
            var web = Arg("-out") ?? "Builds/Web-" + name;
            try { ok = Run(BuildTarget.WebGL, web, sub.HasValue ? (int)sub.Value : 0); if (ok) WritePhotoManifest(web); }
            finally { EditorUserBuildSettings.webGLBuildSubtarget = savedSubtarget; ProjectSetup.Apply(); }   // back to the committed settings
            ExitIfFailed(ok);
        }

        public static void Mac()
        {
            if (RefuseUnlessShareable()) return;
            ProjectSetup.Apply();
            // Apple clang fuses a*b+c into fmadd by default (tech notes 4.2); turn it off if the Mac golden check fails.
            PlayerSettings.SetAdditionalIl2CppArgs(Flag("-ffp-contract-off") ? "--compiler-flags=-ffp-contract=off" : "");
            bool ok;
            var app = Arg("-out") ?? "Builds/Mac/Statecraft.app";
            try { ok = Run(BuildTarget.StandaloneOSX, app); if (ok) WritePhotoManifest(Path.GetDirectoryName(Path.GetFullPath(app))!); }
            finally { PlayerSettings.SetAdditionalIl2CppArgs(""); AssetDatabase.SaveAssets(); }
            ExitIfFailed(ok);
        }

        /// <summary>The Linux x64 player on Mono, built from the Mac (decision O7; tools/release_build.py).</summary>
        public static void LinuxMono() => StandaloneMono(BuildTarget.StandaloneLinux64, "Builds/Linux/Statecraft.x86_64");

        /// <summary>The Windows x64 player on Mono: Unity builds a Windows IL2CPP player only on Windows (decision O8).</summary>
        public static void WindowsMono() => StandaloneMono(BuildTarget.StandaloneWindows64, "Builds/Windows/Statecraft.exe");

        /// <summary>Builds <paramref name="target"/> with the Mono backend, then puts the committed IL2CPP setting back
        /// (ProjectSetup.Apply): the Standalone backend is one setting for Mac, Linux and Windows.</summary>
        static void StandaloneMono(BuildTarget target, string defaultOut)
        {
            if (RefuseUnlessShareable()) return;
            ProjectSetup.Apply();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            bool ok;
            var player = Arg("-out") ?? defaultOut;
            try { ok = Run(target, player); if (ok) WritePhotoManifest(Path.GetDirectoryName(Path.GetFullPath(player))!); }
            finally { ProjectSetup.Apply(); AssetDatabase.SaveAssets(); }
            ExitIfFailed(ok);
        }

        /// <summary>Exits with code 1 after the caller's settings have been restored.</summary>
        static void ExitIfFailed(bool ok) { if (!ok) EditorApplication.Exit(1); }

        /// <summary>The real size of the build output (summary.totalSize also counts build intermediates).</summary>
        static double OutputMegabytes(string path)
        {
            if (File.Exists(path)) return new FileInfo(path).Length / 1048576.0;
            return Directory.Exists(path) ? new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length) / 1048576.0 : 0;
        }

        static bool Run(BuildTarget target, string location, int subtarget = 0)
        {
            // The 1948 Collection's streamed bundles (issue #664) go in StreamingAssets/collection first; their manifest keeps
            // the classes only they use (Animator, SkinnedMeshRenderer) in the player.
            var bundles = CollectionImport.BuildBundles(target);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.BootScene }, locationPathName = location, target = target, options = BuildOptions.None,
                subtarget = subtarget,   // WebGL: the texture subtarget (0, Generic: the project's own setting)
                assetBundleManifestPath = bundles,
            });
            var s = report.summary;
            Debug.Log($"BUILD {s.result} {target} -> {location}: {OutputMegabytes(location):0.0} MB on disk, {s.totalErrors} errors, {s.totalTime.TotalSeconds:0}s");
            return s.result == BuildResult.Succeeded;
        }
    }
}
