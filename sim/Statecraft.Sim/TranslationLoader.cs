using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Statecraft.Sim
{
    /// <summary>Brings translations/ to the sim with the content (#1618 step 2; docs/translations.md, "Loading"). Each
    /// language is parsed and checked against the content it translates, then kept in <see cref="Content.Translations"/>,
    /// outside the content hash. Nothing shows them yet: the game's Hebrew still comes from the inline fields, and a
    /// translations/he/ that is present must equal them, so the later steps can switch the source with no text changing.</summary>
    public static class TranslationLoader
    {
        public const string Folder = "translations";

        static readonly string[] LanguageFiles = { Translations.UiFile, Translations.EventsFile, Translations.ContentFile };

        /// <summary>UI keys only Hebrew may translate, for now (owner, 24 October 2026, #1604): the advisors and How to play.</summary>
        public static readonly IReadOnlyList<string> HebrewOnlyUi = new[] { "advisor.", "how_to_play." };

        /// <summary>translations/ beside a folder named content: the repository's beside content/, StreamingAssets/translations
        /// beside StreamingAssets/content. Null for a content folder with another name, such as a test fixture, so a fixture
        /// never picks up translations from wherever it happens to be written.</summary>
        public static string? Beside(string contentRoot)
        {
            var dir = Path.GetFullPath(contentRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.GetFileName(dir) == "content" ? Path.Combine(Path.GetDirectoryName(dir)!, Folder) : null;
        }

        static bool Hidden(string path) => Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal);

        /// <summary>The language files under <paramref name="translationsRoot"/>, as "ro/ui.yaml", in ordinal order (none when
        /// it is null or missing). Files at the top (README.md) are not languages, and hidden names (.DS_Store) are system
        /// metadata, so both are passed over. Anything else that is not a language's ui.yaml, events.yaml or content.yaml
        /// is refused rather than skipped, so a misnamed file cannot drop out unnoticed. A folder that cannot be listed is a
        /// <see cref="ContentException"/> too, so it is reported as a content error.</summary>
        public static List<string> Collect(string? translationsRoot)
        {
            try { return CollectFiles(translationsRoot); }
            catch (IOException e) { throw new ContentException(Folder, "(folder)", e.Message); }
            catch (UnauthorizedAccessException e) { throw new ContentException(Folder, "(folder)", e.Message); }
        }

        static List<string> CollectFiles(string? translationsRoot)
        {
            var files = new List<string>();
            if (translationsRoot == null || !Directory.Exists(translationsRoot)) return files;
            foreach (var dir in Directory.GetDirectories(translationsRoot).Where(d => !Hidden(d)))
            {
                var lang = Path.GetFileName(dir);
                if (!Translations.IsLanguage(lang))
                    throw new ContentException($"{Folder}/{lang}", "(folder)", "not a language code (fr, ar, pt-BR)");
                if (Directory.GetDirectories(dir).Any(d => !Hidden(d)))
                    throw new ContentException($"{Folder}/{lang}", "(folder)", "a language folder has no sub-folders");
                foreach (var file in Directory.GetFiles(dir).Where(f => !Hidden(f)))
                {
                    var name = Path.GetFileName(file);
                    if (!LanguageFiles.Contains(name))
                        throw new ContentException($"{Folder}/{lang}/{name}", "(file)", $"a language has only {string.Join(", ", LanguageFiles)}");
                    files.Add(lang + "/" + name);
                }
            }
            files.Sort(StringComparer.Ordinal);
            return files;
        }

        /// <summary>Reads and checks every language in <paramref name="files"/> (as <see cref="Collect"/> lists them, or as a
        /// player's manifest does). <paramref name="readContent"/> reads a content file without hashing it (null: not there);
        /// <paramref name="events"/> are every parsed event, including those the ruleset leaves out.</summary>
        internal static List<TranslationSet> Load(IReadOnlyList<GameEvent> events, Func<string, string?> readContent,
            Func<string, string?> readTranslation, IEnumerable<string> files)
        {
            var languages = new SortedDictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var f in files)
            {
                var parts = (f ?? "").Split('/');
                if (parts.Length != 2 || !Translations.IsLanguage(parts[0]) || !LanguageFiles.Contains(parts[1]))
                    throw new ContentException($"{Folder}/{f}", "(file)", "not <language>/ui.yaml, events.yaml or content.yaml");
                if (!languages.TryGetValue(parts[0], out var names)) languages[parts[0]] = names = new HashSet<string>(StringComparer.Ordinal);
                if (!names.Add(parts[1])) throw new ContentException($"{Folder}/{f}", "(file)", "listed twice");
            }

            var parsed = new Dictionary<string, YamlNode?>(StringComparer.Ordinal);
            YamlNode? Yaml(string rel)
            {
                if (parsed.TryGetValue(rel, out var cached)) return cached;
                var text = readContent(rel);
                YamlNode? root = null;
                if (text != null)
                {
                    var stream = new YamlStream();
                    try { stream.Load(new StringReader(text)); }
                    catch (YamlException e) { throw new ContentException(rel, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
                    root = stream.Documents.Count > 0 ? stream.Documents[0].RootNode : new YamlMappingNode();
                }
                return parsed[rel] = root;
            }

            var sets = new List<TranslationSet>();
            foreach (var language in languages)
            {
                string? Text(string name)
                {
                    if (!language.Value.Contains(name)) return null;
                    var where = $"{Folder}/{language.Key}/{name}";
                    try { return readTranslation(language.Key + "/" + name) ?? throw new ContentException(where, "(file)", "listed but missing"); }
                    catch (IOException e) { throw new ContentException(where, "(file)", e.Message); }
                    catch (UnauthorizedAccessException e) { throw new ContentException(where, "(file)", e.Message); }
                }
                var set = Translations.Parse(language.Key, Text(Translations.UiFile), Text(Translations.EventsFile), Text(Translations.ContentFile));
                Translations.CheckDeclaredLanguage(set);
                CheckUi(set);
                CheckEvents(set, events);
                CheckContent(set, Yaml);
                if (set.Lang == Translations.Hebrew) CheckEqualsInlineHebrew(set, events, Yaml);
                sets.Add(set);
            }
            return sets;
        }

        static string Where(TranslationSet set, string file) => $"{Folder}/{set.Lang}/{file}";

        static void CheckUi(TranslationSet set)
        {
            if (set.Lang == Translations.Hebrew) return;
            foreach (var kv in set.Ui)
                if (HebrewOnlyUi.Any(p => kv.Key.StartsWith(p, StringComparison.Ordinal)))
                    throw new ContentException(Where(set, Translations.UiFile), kv.Key,
                        "the advisors and How to play stay English or Hebrew only, for now (owner, 24 October 2026, #1604)");
        }

        /// <summary>A translated event exists and translates exactly its options: an event is all or none, so a card never
        /// shows one button in English.</summary>
        static void CheckEvents(TranslationSet set, IReadOnlyList<GameEvent> events)
        {
            var where = Where(set, Translations.EventsFile);
            foreach (var t in set.Events)
            {
                var e = events.FirstOrDefault(x => x.Id == t.Id) ?? throw new ContentException(where, t.Id, "no event has this id");
                foreach (var o in t.Options)
                    if (!e.Options.Any(x => x.Id == o.Key)) throw new ContentException(where, $"{t.Id}.options.{o.Key}", "the event has no such option");
                foreach (var o in e.Options)
                    if (!t.Options.Any(x => x.Key == o.Id))
                        throw new ContentException(where, $"{t.Id}.options.{o.Id}", "required: a translated event translates every option (all or none)");
            }
        }

        /// <summary>A content key names a field <see cref="Translations.Fields"/> offers in its file. A file a player does not
        /// ship (<see cref="HebrewOverlay.UnityReads"/>, read there in baked form) is checked where it can be read: the editor,
        /// the CLI and the build gate.</summary>
        static void CheckContent(TranslationSet set, Func<string, YamlNode?> yaml)
        {
            var where = Where(set, Translations.ContentFile);
            var offered = new Dictionary<string, HashSet<string>?>(StringComparer.Ordinal);
            foreach (var t in set.Content)
            {
                var file = ContentKey.TryParse(t.Key)!.File;   // Parse has checked the key's shape
                if (!offered.TryGetValue(file, out var keys))
                {
                    var root = yaml(file);
                    if (root == null && !HebrewOverlay.UnityReads.Contains(file)) throw new ContentException(where, t.Key, $"no content file {file}");
                    offered[file] = keys = root == null ? null : new HashSet<string>(Translations.Fields(file, root).Select(f => f.Key.ToString()), StringComparer.Ordinal);
                }
                if (keys != null && !keys.Contains(t.Key))
                    throw new ContentException(where, t.Key, "not a translatable field: it has no Hebrew form in its file (docs/translations.md, \"Content keys\")");
            }
        }

        const string Authoritative = "the inline Hebrew stays authoritative until a later step of #1618 moves it";

        /// <summary>translations/he/ equals the inline Hebrew: the same events with the same title, text, options and review
        /// flag, and the same content fields with the same text and flag, none missing and none extra. A file a player does
        /// not ship is compared where it can be read.</summary>
        static void CheckEqualsInlineHebrew(TranslationSet he, IReadOnlyList<GameEvent> events, Func<string, YamlNode?> yaml)
        {
            var where = Where(he, Translations.EventsFile);
            var inline = events.Where(e => e.TitleHe != "").ToDictionary(e => e.Id, StringComparer.Ordinal);
            foreach (var t in he.Events)
            {
                if (!inline.TryGetValue(t.Id, out var e)) throw new ContentException(where, t.Id, "the event has no inline Hebrew to match");
                var same = t.Title == e.TitleHe && t.Text == e.TextHe && t.Review == e.TextHeReview
                    && t.Options.All(o => e.Options.Single(x => x.Id == o.Key).TextHe == o.Value);
                if (!same) throw new ContentException(where, t.Id, "differs from the event's inline Hebrew; " + Authoritative);
            }
            var translated = new HashSet<string>(he.Events.Select(t => t.Id), StringComparer.Ordinal);
            foreach (var id in inline.Keys.OrderBy(id => id, StringComparer.Ordinal))
                if (!translated.Contains(id)) throw new ContentException(where, id, "missing: the event has inline Hebrew");

            where = Where(he, Translations.ContentFile);
            var mine = he.Content.ToDictionary(t => t.Key, StringComparer.Ordinal);
            var files = HebrewOverlay.Files.Concat(HebrewOverlay.UnityReads).Concat(mine.Keys.Select(k => ContentKey.TryParse(k)!.File))
                .Distinct().OrderBy(f => f, StringComparer.Ordinal);
            foreach (var file in files)
            {
                var root = yaml(file);
                if (root == null) continue;   // not shipped to a player (checked on disk), or not in this content set
                var fields = Translations.Fields(file, root).ToList();
                foreach (var f in fields.Where(f => f.Hebrew != ""))
                {
                    var key = f.Key.ToString();
                    if (!mine.TryGetValue(key, out var t)) throw new ContentException(where, key, "missing: the field has inline Hebrew");
                    if (t.Text != f.Hebrew || t.Review != f.Review) throw new ContentException(where, key, "differs from the inline Hebrew; " + Authoritative);
                }
                foreach (var f in fields.Where(f => f.Hebrew == "" && mine.ContainsKey(f.Key.ToString())))
                    throw new ContentException(where, f.Key.ToString(), "the field has no inline Hebrew to match");
            }
        }
    }
}
