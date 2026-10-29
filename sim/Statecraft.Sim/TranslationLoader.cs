using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Statecraft.Sim
{
    /// <summary>Reads translations/ with the content (#1618, step 2; docs/translations.md, "Loading"). Every language is
    /// parsed strictly and checked against the content it translates. The Hebrew the game shows still comes from the
    /// inline fields: a translations/he/ that is present must match them exactly, so a later step can switch the source
    /// without a byte changing. Translations are display text, so they stay outside <see cref="Content.Hash"/>.</summary>
    public static class TranslationLoader
    {
        public const string Folder = "translations";

        static readonly Regex FileShape = new Regex(@"^([^/]+)/(ui|events|content)\.yaml$", RegexOptions.CultureInvariant);

        /// <summary>UI strings kept in English and Hebrew only, for now (owner, 24 October 2026, on #1604): the advisors and
        /// How to play. A language other than Hebrew may not translate them.</summary>
        public static readonly IReadOnlyList<string> HebrewAndEnglishOnly = new[] { "advisor.", "how_to_play." };

        /// <summary>The translations folder beside a content folder named "content" (&lt;repo&gt;/translations beside
        /// &lt;repo&gt;/content, StreamingAssets/translations beside StreamingAssets/content), or null for a content folder
        /// with another name (a test fixture), which has none.</summary>
        public static string? Beside(string contentRoot)
        {
            var full = Path.GetFullPath(contentRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.GetFileName(full) == "content" ? Path.Combine(Path.GetDirectoryName(full)!, Folder) : null;
        }

        /// <summary>The language files under <paramref name="translationsRoot"/> as "&lt;lang&gt;/&lt;file&gt;", ordinal order;
        /// none when the folder is missing. Files at the top (README.md) are not languages. A folder that is not a language
        /// code, or a file in a language folder that is not ui.yaml, events.yaml or content.yaml, is an error: it would
        /// otherwise be skipped silently. Dot-prefixed files and folders are operating-system/editor metadata and ignored.</summary>
        public static List<string> Collect(string? translationsRoot)
        {
            var files = new List<string>();
            if (translationsRoot == null || !Directory.Exists(translationsRoot)) return files;
            foreach (var dir in Directory.GetDirectories(translationsRoot))
            {
                var lang = Path.GetFileName(dir);
                if (lang.StartsWith(".", StringComparison.Ordinal)) continue;
                if (!Translations.IsLanguage(lang))
                    throw new ContentException($"{Folder}/{lang}", "(folder)", "not a language code (lowercase ISO 639: fr, ar, pt-BR)");
                foreach (var f in Directory.GetFiles(dir))
                {
                    if (Path.GetFileName(f).StartsWith(".", StringComparison.Ordinal)) continue;
                    var rel = lang + "/" + Path.GetFileName(f);
                    if (!FileShape.IsMatch(rel))
                        throw new ContentException($"{Folder}/{rel}", "(file)", $"a language has only {Translations.UiFile}, {Translations.EventsFile} and {Translations.ContentFile}");
                    files.Add(rel);
                }
                if (Directory.GetDirectories(dir).Any(d => !Path.GetFileName(d).StartsWith(".", StringComparison.Ordinal)))
                    throw new ContentException($"{Folder}/{lang}", "(folder)", "a language folder has no sub-folders");
            }
            return files.OrderBy(f => f, StringComparer.Ordinal).ToList();
        }

        /// <summary>Every language in <paramref name="files"/> (from <see cref="Collect"/> or a manifest), each parsed and
        /// checked; ordered by code. <paramref name="readContent"/> reads content files without adding them to the hash;
        /// <paramref name="events"/> are every parsed event, those a ruleset leaves out included.</summary>
        internal static List<TranslationSet> Load(Content c, IReadOnlyList<GameEvent> events, Func<string, string?> readContent,
            Func<string, string?> readTranslation, IEnumerable<string> files)
        {
            var byLang = new SortedDictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var f in files)
            {
                var m = FileShape.Match(f ?? "");
                if (!m.Success || !Translations.IsLanguage(m.Groups[1].Value))
                    throw new ContentException($"{Folder}/{f}", "(file)", "not <lang>/ui.yaml, <lang>/events.yaml or <lang>/content.yaml");
                if (!byLang.TryGetValue(m.Groups[1].Value, out var names)) byLang[m.Groups[1].Value] = names = new HashSet<string>();
                if (!names.Add(m.Groups[2].Value + ".yaml")) throw new ContentException($"{Folder}/{f}", "(file)", "listed twice");
            }
            var yaml = new Dictionary<string, YamlNode?>(StringComparer.Ordinal);
            YamlNode? Yaml(string rel)
            {
                if (yaml.TryGetValue(rel, out var node)) return node;
                var text = readContent(rel);
                if (text != null)
                {
                    var stream = new YamlStream();
                    try { stream.Load(new StringReader(text)); }
                    catch (YamlException e) { throw new ContentException(rel, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
                    node = stream.Documents.Count > 0 ? stream.Documents[0].RootNode : new YamlMappingNode();
                }
                return yaml[rel] = node;
            }

            var sets = new List<TranslationSet>();
            foreach (var kv in byLang)
            {
                var lang = kv.Key;
                string? Text(string name)
                {
                    if (!kv.Value.Contains(name)) return null;
                    try
                    {
                        return readTranslation(lang + "/" + name)
                            ?? throw new ContentException($"{Folder}/{lang}/{name}", "(file)", "listed but missing");
                    }
                    catch (IOException e) { throw new ContentException($"{Folder}/{lang}/{name}", "(file)", e.Message); }
                    catch (UnauthorizedAccessException e) { throw new ContentException($"{Folder}/{lang}/{name}", "(file)", e.Message); }
                }
                var set = Translations.Parse(lang, Text(Translations.UiFile), Text(Translations.EventsFile), Text(Translations.ContentFile));
                Translations.CheckDeclaredLanguage(set);
                CheckUi(set);
                CheckEvents(set, events);
                CheckContent(set, Yaml);
                if (lang == Translations.Hebrew) CheckMatchesInline(set, events, Yaml);
                sets.Add(set);
            }
            return sets;
        }

        static string Where(TranslationSet set, string file) => $"{Folder}/{set.Lang}/{file}";

        static void CheckUi(TranslationSet set)
        {
            if (set.Lang == Translations.Hebrew) return;
            foreach (var kv in set.Ui)
                if (HebrewAndEnglishOnly.Any(p => kv.Key.StartsWith(p, StringComparison.Ordinal)))
                    throw new ContentException(Where(set, Translations.UiFile), kv.Key,
                        "the advisors and How to play stay English or Hebrew only, for now (owner, 24 October 2026, #1604)");
        }

        /// <summary>Each translated event exists, and its options are exactly the event's: an event is all or none, so a
        /// translated card never shows one button in English.</summary>
        static void CheckEvents(TranslationSet set, IReadOnlyList<GameEvent> events)
        {
            var where = Where(set, Translations.EventsFile);
            foreach (var t in set.Events)
            {
                var e = events.FirstOrDefault(x => x.Id == t.Id) ?? throw new ContentException(where, t.Id, "no event has this id");
                var have = new HashSet<string>(t.Options.Select(o => o.Key));
                foreach (var o in t.Options)
                    if (!e.Options.Any(x => x.Id == o.Key)) throw new ContentException(where, $"{t.Id}.options.{o.Key}", "the event has no such option");
                foreach (var o in e.Options)
                    if (!have.Contains(o.Id))
                        throw new ContentException(where, $"{t.Id}.options.{o.Id}", "required: a translated event translates every option (all or none)");
            }
        }

        /// <summary>Each content key is a field <see cref="Translations.Fields"/> offers in its file. A file the player does
        /// not ship (a tool input Unity reads in baked form) is checked where it can be read: in the editor, the CLI and
        /// the build gate.</summary>
        static void CheckContent(TranslationSet set, Func<string, YamlNode?> yaml)
        {
            var where = Where(set, Translations.ContentFile);
            var offered = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var t in set.Content)
            {
                var key = ContentKey.TryParse(t.Key)!;
                var root = yaml(key.File);
                if (root == null)
                {
                    if (HebrewOverlay.UnityReads.Contains(key.File)) continue;
                    throw new ContentException(where, t.Key, $"no content file {key.File}");
                }
                if (!offered.TryGetValue(key.File, out var keys))
                    offered[key.File] = keys = new HashSet<string>(Translations.Fields(key.File, root).Select(f => f.Key.ToString()), StringComparer.Ordinal);
                if (!keys.Contains(t.Key))
                    throw new ContentException(where, t.Key, "not a translatable field: one with a Hebrew form in its file (docs/translations.md, \"Content keys\")");
            }
        }

        /// <summary>translations/he/ against the inline Hebrew, which stays authoritative until #1618 moves it: the same
        /// events with the same texts, options and review flags, and the same content fields with the same texts and flags,
        /// none missing and none extra. A file the player does not ship is compared where it can be read.</summary>
        static void CheckMatchesInline(TranslationSet he, IReadOnlyList<GameEvent> events, Func<string, YamlNode?> yaml)
        {
            var where = Where(he, Translations.EventsFile);
            var inline = events.Where(e => e.TitleHe != "").ToDictionary(e => e.Id, StringComparer.Ordinal);
            foreach (var t in he.Events)
            {
                if (!inline.TryGetValue(t.Id, out var e)) throw new ContentException(where, t.Id, "the event has no inline Hebrew to match");
                if (t.Title != e.TitleHe || t.Text != e.TextHe || t.Review != e.TextHeReview
                    || t.Options.Any(o => e.Options.First(x => x.Id == o.Key).TextHe != o.Value))
                    throw new ContentException(where, t.Id, "differs from the event's inline Hebrew, which stays authoritative until #1618 moves it");
            }
            foreach (var id in inline.Keys.Where(id => he.Events.All(t => t.Id != id)))
                throw new ContentException(where, id, "missing: the event has inline Hebrew");

            where = Where(he, Translations.ContentFile);
            var mine = he.Content.ToDictionary(t => t.Key, StringComparer.Ordinal);
            var files = HebrewOverlay.Files.Concat(HebrewOverlay.UnityReads)
                .Concat(he.Content.Select(t => ContentKey.TryParse(t.Key)!.File)).Distinct().OrderBy(f => f, StringComparer.Ordinal);
            foreach (var file in files)
            {
                var root = yaml(file);
                if (root == null) continue;   // not shipped (checked in the editor) or not in this content set
                var expected = Translations.Fields(file, root).Where(f => f.Hebrew != "").ToDictionary(f => f.Key.ToString(), StringComparer.Ordinal);
                foreach (var kv in expected)
                {
                    if (!mine.TryGetValue(kv.Key, out var t)) throw new ContentException(where, kv.Key, "missing: the field has inline Hebrew");
                    if (t.Text != kv.Value.Hebrew || t.Review != kv.Value.Review)
                        throw new ContentException(where, kv.Key, "differs from the inline Hebrew, which stays authoritative until #1618 moves it");
                }
                foreach (var t in he.Content.Where(t => ContentKey.TryParse(t.Key)!.File == file && !expected.ContainsKey(t.Key)))
                    throw new ContentException(where, t.Key, "the field has no inline Hebrew to match");
            }
        }
    }
}
