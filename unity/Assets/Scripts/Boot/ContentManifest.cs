#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.Boot
{
    /// <summary>The list of content files a player needs, so a WebGL build (no directory listing over HTTP) can fetch
    /// them: content/manifest.json in StreamingAssets, {"files": [...]} with paths relative to content/, '/' separated,
    /// ordinal order. The editor build step (ContentSync) copies the files and writes the manifest, and packs every listed
    /// file's text into content/bundle.json, which is what the web boot fetches: one request instead of one per file
    /// (#1054). Desktop players and the editor read the files themselves.
    /// The translations beside content/ (translations/, #1618 step 2) travel with it: copied to StreamingAssets/translations,
    /// listed in the manifest's "translations" and packed into the same bundle, so the web boot still makes one request.</summary>
    public static class ContentManifest
    {
        public const string FileName = "manifest.json";
        /// <summary>The empty file <see cref="Copy"/> leaves at the top of the translations folder it makes. Hidden and at the
        /// top, so the loader passes over it (TranslationLoader.Collect), and Unity does not import it.</summary>
        public const string CopyMarker = ".contentmanifest-copy";
        /// <summary>Every listed file's text in one file: {"files": [...], "texts": [...]}, the same order (#1054).</summary>
        public const string BundleFileName = "bundle.json";
        /// <summary>The map's hex table, always in the manifest. The map files players read are this and every map/*.yaml
        /// (settlements); the precipitation files are tool inputs only.</summary>
        public const string HexesJson = "map/hexes.json";

        /// <summary>Map files that only the offline tools read, so players never download them: routes_1948.yaml is the
        /// hand-written input of tools/routes_1948.py; the map reads its traced output (review M10).</summary>
        /// localities_1948.yaml is the sourced input of tools/localities_bake.py; the map reads its baked runtime form.
        public static readonly IReadOnlyCollection<string> ToolInputs = new HashSet<string> { "map/routes_1948.yaml", "map/localities_1948.yaml" };

        /// <summary>Baked map files that are not YAML, listed when present: the runtime forms of the 1948 localities and of the
        /// route paths and the region files (boot budget, issue #321). Their YAML still ships: each runtime file is checked
        /// against it. Also the true armistice lines, which the map draws as they are, on unless turned off (#237, #196).</summary>
        public static readonly IReadOnlyList<string> RuntimeData = new[] { "map/localities_1948.tsv", "map/route_paths_1948.tsv",
            "map/subdistrict_hexes_1948.tsv", "map/armistice_hexes_1949.tsv", "map/control_hexes_1948_49.tsv", "map/armistice_lines_1949.geojson" };

        // "translations" (and the bundle's "translationTexts") are paths relative to translations/, such as "ro/ui.yaml";
        // a manifest or bundle written before #1618 step 2 has none, which reads as no translations.
        [Serializable] sealed class ManifestJson { public string[] files = new string[0]; public string[] translations = new string[0]; }
        [Serializable] sealed class BundleJson
        {
            public string[] files = new string[0]; public string[] texts = new string[0];
            public string[] translations = new string[0]; public string[] translationTexts = new string[0];
        }

        /// <summary>Every *.yaml in <paramref name="contentDir"/>, events/, map/ and rules/ (era rule overrides, #58; the folder may be absent) (but <see cref="ToolInputs"/>), plus
        /// map/hexes.json; ordinal order.</summary>
        public static List<string> Collect(string contentDir)
        {
            IEnumerable<string> Yaml(string sub) => sub == "rules" && !Directory.Exists(Path.Combine(contentDir, sub)) ? new string[0] : Directory.GetFiles(Path.Combine(contentDir, sub), "*.yaml")
                .Select(f => (sub == "" ? "" : sub + "/") + Path.GetFileName(f));
            if (!File.Exists(Path.Combine(contentDir, HexesJson))) throw new FileNotFoundException("content file missing", HexesJson);
            return Yaml("").Concat(Yaml("events")).Concat(Yaml("map")).Concat(Yaml("rules")).Append(HexesJson)
                .Concat(RuntimeData.Where(f => File.Exists(Path.Combine(contentDir, f))))
                .Where(f => !ToolInputs.Contains(f)).OrderBy(f => f, StringComparer.Ordinal).ToList();
        }

        /// <summary>The translation files beside <paramref name="contentDir"/> (TranslationLoader.Collect of the translations/
        /// next to a folder named content; none otherwise), as "ro/ui.yaml", ordinal order.</summary>
        public static List<string> CollectTranslations(string contentDir) => TranslationLoader.Collect(TranslationLoader.Beside(contentDir));

        static string TranslationPath(string contentDir, string file) =>
            Path.Combine(TranslationLoader.Beside(contentDir) ?? throw new InvalidDataException($"{contentDir} has no translations folder beside it"), file);

        public static string Write(IEnumerable<string> files, IEnumerable<string>? translations = null) =>
            JsonUtility.ToJson(new ManifestJson { files = files.ToArray(), translations = (translations ?? new string[0]).ToArray() }, true);

        /// <summary>The translation files manifest.json lists (none in an older manifest); throws as Parse does.</summary>
        public static List<string> ParseTranslations(string json)
        {
            Parse(json);
            return (JsonUtility.FromJson<ManifestJson>(json).translations ?? new string[0]).ToList();
        }

        /// <summary>Parses manifest.json; throws InvalidDataException when it is not a manifest.</summary>
        public static List<string> Parse(string json)
        {
            ManifestJson? m;
            try { m = JsonUtility.FromJson<ManifestJson>(json); }
            catch (ArgumentException e) { throw new InvalidDataException($"{FileName}: malformed JSON ({e.Message})", e); }
            if (m?.files == null || m.files.Length == 0) throw new InvalidDataException($"{FileName}: no files");
            return m.files.ToList();
        }

        /// <summary>bundle.json for <paramref name="files"/> (paths relative to <paramref name="contentDir"/>), each text
        /// read as the disk path reads it (File.ReadAllText), so the content hash is the disk's; a <paramref name="release"/>
        /// build's as <see cref="Shipped"/> gives it. The translations beside it are packed too (<paramref name="translations"/>,
        /// default CollectTranslations), each as File.ReadAllText reads it.</summary>
        public static string WriteBundle(string contentDir, IReadOnlyList<string> files, bool release = false, IReadOnlyList<string>? translations = null)
        {
            var packed = translations ?? CollectTranslations(contentDir);
            return JsonUtility.ToJson(new BundleJson
            {
                files = files.ToArray(),
                texts = files.Select(f => Shipped(contentDir, f, release)).ToArray(),
                translations = packed.ToArray(),
                translationTexts = packed.Select(f => File.ReadAllText(TranslationPath(contentDir, f))).ToArray(),
            });
        }

        /// <summary>A file's text as a build ships it: the source's, but a <paramref name="release"/> build's true armistice
        /// lines hold only the features the owner has signed off (ArmisticeLines.ReleaseText; owner review of #2012). The
        /// sim never reads that file, so the content hash is the same either way.</summary>
        static string Shipped(string contentDir, string file, bool release)
        {
            var text = File.ReadAllText(Path.Combine(contentDir, file));
            return release && file == "map/" + ArmisticeLines.FileName ? ArmisticeLines.ReleaseText(text) : text;
        }

        /// <summary>Parses bundle.json into the file list (as manifest.json lists it) and each file's text; throws
        /// InvalidDataException when it is not a bundle.</summary>
        public static (List<string> Files, Dictionary<string, string> Texts) ParseBundle(string json)
        {
            var b = ParseBundleWithTranslations(json);
            return (b.Files, b.Texts);
        }

        /// <summary>ParseBundle, with the translation files the bundle packs and their texts (none in an older bundle).</summary>
        public static (List<string> Files, Dictionary<string, string> Texts, List<string> Translations, Dictionary<string, string> TranslationTexts)
            ParseBundleWithTranslations(string json)
        {
            BundleJson? b;
            try { b = JsonUtility.FromJson<BundleJson>(json); }
            catch (ArgumentException e) { throw new InvalidDataException($"{BundleFileName}: malformed JSON ({e.Message})", e); }
            if (b?.files == null || b.files.Length == 0) throw new InvalidDataException($"{BundleFileName}: no files");
            if (b.texts == null || b.texts.Length != b.files.Length)
                throw new InvalidDataException($"{BundleFileName}: {b.files.Length} files but {b.texts?.Length ?? 0} texts");
            var texts = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < b.files.Length; i++)
                if (!texts.TryAdd(b.files[i], b.texts[i])) throw new InvalidDataException($"{BundleFileName}: {b.files[i]} twice");
            var translations = b.translations ?? new string[0];
            var translationTexts = b.translationTexts ?? new string[0];
            if (translationTexts.Length != translations.Length)
                throw new InvalidDataException($"{BundleFileName}: {translations.Length} translation files but {translationTexts.Length} texts");
            var byFile = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < translations.Length; i++)
                if (!byFile.TryAdd(translations[i], translationTexts[i])) throw new InvalidDataException($"{BundleFileName}: translation {translations[i]} twice");
            return (b.files.ToList(), texts, translations.ToList(), byFile);
        }

        /// <summary>Checks a copy made by Copy: manifest.json equals Collect(contentDir) and every listed file exists, and
        /// bundle.json lists the same files with each one's text as the source has it (as a <paramref name="release"/> build
        /// ships it). The same for the translations: both files list CollectTranslations(contentDir), the bundle packs each
        /// one's text, and the copy beside a target named content holds exactly those files, byte for byte. Throws
        /// InvalidDataException naming the problem.</summary>
        public static void Verify(string contentDir, string targetDir, bool release = false)
        {
            var expected = Collect(contentDir);
            var written = Parse(File.ReadAllText(Path.Combine(targetDir, FileName)));
            if (!expected.SequenceEqual(written))
                throw new InvalidDataException($"{FileName} in {targetDir} does not match the content files ({written.Count} listed, {expected.Count} expected)");
            foreach (var f in written)
                if (!File.Exists(Path.Combine(targetDir, f))) throw new InvalidDataException($"{f}: listed in {FileName} but missing from {targetDir}");
            var bundle = Path.Combine(targetDir, BundleFileName);
            if (!File.Exists(bundle)) throw new InvalidDataException($"{BundleFileName}: missing from {targetDir}");
            var (files, texts, packed, packedTexts) = ParseBundleWithTranslations(File.ReadAllText(bundle));
            if (!expected.SequenceEqual(files))
                throw new InvalidDataException($"{BundleFileName} in {targetDir} does not match the content files ({files.Count} listed, {expected.Count} expected)");
            foreach (var f in files)
                if (texts[f] != Shipped(contentDir, f, release)) throw new InvalidDataException($"{f}: its text in {BundleFileName} differs from the source");

            var translations = CollectTranslations(contentDir);
            var listed = ParseTranslations(File.ReadAllText(Path.Combine(targetDir, FileName)));
            if (!translations.SequenceEqual(listed))
                throw new InvalidDataException($"{FileName} in {targetDir} does not list the translation files ({listed.Count} listed, {translations.Count} expected)");
            if (!translations.SequenceEqual(packed))
                throw new InvalidDataException($"{BundleFileName} in {targetDir} does not pack the translation files ({packed.Count} packed, {translations.Count} expected)");
            foreach (var f in translations)
                if (packedTexts[f] != File.ReadAllText(TranslationPath(contentDir, f)))
                    throw new InvalidDataException($"translations/{f}: its text in {BundleFileName} differs from the source");
            var copy = TranslationLoader.Beside(targetDir);
            if (copy == null) return;   // a target not named content: the bundle alone carries them
            var found = Directory.Exists(copy)
                ? Directory.GetFiles(copy, "*", SearchOption.AllDirectories).Select(p => Path.GetRelativePath(copy, p).Replace('\\', '/'))
                    .Where(p => p != CopyMarker).OrderBy(p => p, StringComparer.Ordinal).ToList()
                : new List<string>();
            if (!translations.SequenceEqual(found))
                throw new InvalidDataException($"{copy} holds {found.Count} files, not the {translations.Count} translation files");
            foreach (var f in translations)
                if (!File.ReadAllBytes(Path.Combine(copy, f)).SequenceEqual(File.ReadAllBytes(TranslationPath(contentDir, f))))
                    throw new InvalidDataException($"translations/{f}: the copy differs from the source");
        }

        /// <summary>Loads the copy in <paramref name="targetDir"/> as a player loads it, and the source in
        /// <paramref name="contentDir"/> as the editor does, each with the translations beside it, so a broken translation
        /// that was never committed (so no CI job saw it) fails the build, not a player's boot. Both loads are needed: the
        /// copy leaves out the <see cref="ToolInputs"/> (map/localities_1948.yaml), so a key into one is checked only
        /// against the source. Returns the number of languages; throws ContentException.</summary>
        public static int CheckLoads(string contentDir, string targetDir)
        {
            var languages = ContentLoader.Load(targetDir).Translations.Count;
            ContentLoader.Load(contentDir);
            return languages;
        }

        /// <summary>Replaces <paramref name="targetDir"/> with a copy of the collected files plus manifest.json and bundle.json;
        /// for a <paramref name="release"/> build, the files as <see cref="Shipped"/> gives them. When the target is named
        /// content (StreamingAssets/content), the translations folder beside it is replaced with a copy of the translations
        /// beside <paramref name="contentDir"/>; the bundle packs them either way. Copy marks the translations folder it makes
        /// with <see cref="CopyMarker"/> and replaces only a folder so marked: it throws InvalidDataException, before deleting
        /// anything, rather than delete a translations folder it did not make.</summary>
        public static List<string> Copy(string contentDir, string targetDir, bool release = false)
        {
            var files = Collect(contentDir);
            var translations = CollectTranslations(contentDir);
            var copy = TranslationLoader.Beside(targetDir);
            if (copy != null && Directory.Exists(copy) && !File.Exists(Path.Combine(copy, CopyMarker)))
                throw new InvalidDataException($"{copy}: not a copy ContentManifest.Copy made (no {CopyMarker}), so it is left alone; move or delete it");
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            if (copy != null)
            {
                if (Directory.Exists(copy)) Directory.Delete(copy, true);
                Directory.CreateDirectory(copy);
                File.WriteAllText(Path.Combine(copy, CopyMarker), "");
                foreach (var f in translations)
                {
                    var to = Path.Combine(copy, f);
                    Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                    File.Copy(TranslationPath(contentDir, f), to);
                }
            }
            foreach (var f in files)
            {
                var to = Path.Combine(targetDir, f);
                Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                if (release && f == "map/" + ArmisticeLines.FileName) File.WriteAllText(to, Shipped(contentDir, f, release));
                else File.Copy(Path.Combine(contentDir, f), to);
            }
            File.WriteAllText(Path.Combine(targetDir, FileName), Write(files, translations));
            File.WriteAllText(Path.Combine(targetDir, BundleFileName), WriteBundle(contentDir, files, release, translations));
            return files;
        }
    }
}
