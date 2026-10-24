using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Statecraft.Sim
{
    // The translation contract (#1618, docs/translations.md): the three files of translations/<lang>/, their keys, how a
    // content key names one record's field, and how today's inline Hebrew reads as those files. Step 1 of #1618 adds the
    // contract only: the game still reads the inline Hebrew, and these types are used by tests and, later, the loader.

    /// <summary>A content field's key in translations/&lt;lang&gt;/content.yaml: <c>&lt;file&gt;#&lt;path&gt;.&lt;field&gt;</c>,
    /// for example <c>people.yaml#people.ben_gurion.name</c> or <c>map/units_1948.yaml#units.golani.anchors.0.place</c>.
    /// The file is relative to content/; each path step is a mapping key, or in a list the item's <c>id</c> (its index
    /// when it has none); the field is the English field the text translates. <c>title_f</c> is the feminine form of
    /// <c>title</c> (offices.yaml's title_he_f).</summary>
    public sealed class ContentKey
    {
        static readonly Regex Shape = new Regex(@"^([a-z0-9_]+(?:/[a-z0-9_]+)*\.yaml)#((?:[A-Za-z0-9_\-]+\.)+)([a-z][a-z0-9_]*)$",
            RegexOptions.CultureInvariant);

        public string File { get; }
        public IReadOnlyList<string> Path { get; }
        public string Field { get; }

        public ContentKey(string file, IReadOnlyList<string> path, string field)
        {
            File = file; Path = path; Field = field;
        }

        /// <summary>The key, or null when <paramref name="text"/> is not one.</summary>
        public static ContentKey? TryParse(string text)
        {
            var m = Shape.Match(text ?? "");
            if (!m.Success) return null;
            return new ContentKey(m.Groups[1].Value, m.Groups[2].Value.TrimEnd('.').Split('.'), m.Groups[3].Value);
        }

        public override string ToString() => File + "#" + string.Join(".", Path) + "." + Field;
    }

    public sealed class EventTranslation
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        /// <summary>Option id and text, in the event file's order.</summary>
        public List<KeyValuePair<string, string>> Options { get; set; } = new List<KeyValuePair<string, string>>();
        /// <summary><see cref="HebrewReview.AgentChecked"/> until a native speaker has read the text, else "".</summary>
        public string Review { get; set; } = "";
    }

    public sealed class ContentTranslation
    {
        public string Key { get; set; } = "";
        public string Text { get; set; } = "";
        public string Review { get; set; } = "";
        /// <summary>A language other than Hebrew may carry the text's own sources; Hebrew keeps them on the record.</summary>
        public List<Source> Sources { get; set; } = new List<Source>();
    }

    /// <summary>One language's translations, in the order the files list them.</summary>
    public sealed class TranslationSet
    {
        public string Lang { get; set; } = "";
        public List<KeyValuePair<string, string>> Ui { get; set; } = new List<KeyValuePair<string, string>>();
        public List<EventTranslation> Events { get; set; } = new List<EventTranslation>();
        public List<ContentTranslation> Content { get; set; } = new List<ContentTranslation>();
    }

    public static class Translations
    {
        public const string Hebrew = "he";
        public const string UiFile = "ui.yaml", EventsFile = "events.yaml", ContentFile = "content.yaml";

        static readonly Regex LangCode = new Regex(@"^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$", RegexOptions.CultureInvariant);
        static readonly Regex UiKey = new Regex(@"^[A-Za-z0-9_]+(\.[A-Za-z0-9_\-]+)*$", RegexOptions.CultureInvariant);
        static readonly Regex Id = new Regex(@"^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant);
        static readonly Regex HeKey = new Regex(@"^(.+)_he(_f)?$", RegexOptions.CultureInvariant);
        static readonly Regex PlainKey = new Regex(@"^[A-Za-z0-9_.\-]+$", RegexOptions.CultureInvariant);

        /// <summary>Fields that stay on the English record when the Hebrew moves (#1618): the Hebrew's sources, the grammar
        /// and date-window facts about it, and the record's own review. A key never names one of these.</summary>
        public static readonly IReadOnlyList<string> Retained = new[]
        {
            "*_he_sources", "biography_sources.name_he", "title_he_form", "title_he_from", "title_he_until",
            "sources", "status", "reviewed_by", "reviewed_on",
        };

        public static bool IsLanguage(string code) => code != null && LangCode.IsMatch(code);

        // ---- parsing ----

        /// <summary>Reads one language's three files (each may be null: the language has none of that kind). Checks the
        /// shape only: the fields, the keys, non-empty text and the review flag. The text's own rules (all or none,
        /// placeholders, Hebrew letters) are the content rules, applied where the text is used.</summary>
        public static TranslationSet Parse(string lang, string? ui, string? events, string? content)
        {
            if (!IsLanguage(lang)) throw new ContentException("translations", "(language)", $"'{lang}' is not a language code (fr, ar, pt-BR)");
            var set = new TranslationSet { Lang = lang };
            string Where(string file) => $"translations/{lang}/{file}";

            foreach (var (key, node) in Entries(Root(ui, Where(UiFile)), Where(UiFile)))
            {
                if (!UiKey.IsMatch(key)) throw new ContentException(Where(UiFile), key, "not a UI string key (as in strings.en.yaml)");
                set.Ui.Add(new KeyValuePair<string, string>(key, Text(node, Where(UiFile), key)));
            }

            foreach (var (id, node) in Entries(Root(events, Where(EventsFile)), Where(EventsFile)))
            {
                if (!Id.IsMatch(id)) throw new ContentException(Where(EventsFile), id, "not an event id");
                var map = Mapping(node, Where(EventsFile), id);
                Only(map, Where(EventsFile), id, "title", "text", "options", "review");
                var e = new EventTranslation
                {
                    Id = id,
                    Title = Text(Get(map, "title", Where(EventsFile), id), Where(EventsFile), id + ".title"),
                    Text = Text(Get(map, "text", Where(EventsFile), id), Where(EventsFile), id + ".text"),
                    Review = Review(map, Where(EventsFile), id),
                };
                foreach (var (oid, text) in Entries(Mapping(Get(map, "options", Where(EventsFile), id), Where(EventsFile), id + ".options"),
                             Where(EventsFile)))
                {
                    if (!Id.IsMatch(oid)) throw new ContentException(Where(EventsFile), $"{id}.options.{oid}", "not an option id");
                    e.Options.Add(new KeyValuePair<string, string>(oid, Text(text, Where(EventsFile), $"{id}.options.{oid}")));
                }
                set.Events.Add(e);
            }

            foreach (var (key, node) in Entries(Root(content, Where(ContentFile)), Where(ContentFile)))
            {
                if (ContentKey.TryParse(key) == null)
                    throw new ContentException(Where(ContentFile), key, "not a content key (<file>#<path>.<field>, docs/translations.md)");
                var map = Mapping(node, Where(ContentFile), key);
                Only(map, Where(ContentFile), key, "text", "review", "sources");
                var t = new ContentTranslation
                {
                    Key = key,
                    Text = Text(Get(map, "text", Where(ContentFile), key), Where(ContentFile), key + ".text"),
                    Review = Review(map, Where(ContentFile), key),
                };
                if (map.Children.TryGetValue(new YamlScalarNode("sources"), out var sources))
                {
                    if (lang == Hebrew)
                        throw new ContentException(Where(ContentFile), key + ".sources", "the Hebrew's sources stay on the record (name_he_sources, biography_sources.name_he)");
                    t.Sources = Sources(sources, Where(ContentFile), key + ".sources");
                }
                set.Content.Add(t);
            }
            return set;
        }

        static YamlMappingNode Root(string? text, string file)
        {
            if (string.IsNullOrEmpty(text)) return new YamlMappingNode();
            var stream = new YamlStream();
            try { stream.Load(new StringReader(text)); }
            catch (YamlException e) { throw new ContentException(file, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
            catch (ArgumentException e) { throw new ContentException(file, "(file)", e.Message); }   // a repeated key
            if (stream.Documents.Count == 0) return new YamlMappingNode();
            if (stream.Documents.Count > 1) throw new ContentException(file, "(file)", "one YAML document only");
            var root = stream.Documents[0].RootNode;
            if (root is YamlScalarNode s && Core(s) == "null") return new YamlMappingNode();
            return root as YamlMappingNode ?? throw new ContentException(file, "(file)", "must be a mapping");
        }

        static IEnumerable<(string Key, YamlNode Value)> Entries(YamlMappingNode map, string file)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var kv in map.Children)
            {
                var key = kv.Key is YamlScalarNode k ? k.Value ?? "" : throw new ContentException(file, "(key)", "a key must be text");
                if (!seen.Add(key)) throw new ContentException(file, key, "repeated");
                yield return (key, kv.Value);
            }
        }

        static YamlMappingNode Mapping(YamlNode node, string file, string field) =>
            node as YamlMappingNode ?? throw new ContentException(file, field, "must be a mapping");

        static YamlNode Get(YamlMappingNode map, string key, string file, string field) =>
            map.Children.TryGetValue(new YamlScalarNode(key), out var v) ? v : throw new ContentException(file, $"{field}.{key}", "required");

        static void Only(YamlMappingNode map, string file, string field, params string[] keys)
        {
            foreach (var (key, _) in Entries(map, file))
                if (Array.IndexOf(keys, key) < 0) throw new ContentException(file, $"{field}.{key}", $"unknown field (must be {string.Join(", ", keys)})");
        }

        static string Text(YamlNode node, string file, string field)
        {
            if (!(node is YamlScalarNode s) || !IsString(s)) throw new ContentException(file, field, "must be text");
            if (string.IsNullOrWhiteSpace(s.Value)) throw new ContentException(file, field, "empty: leave the key out instead");
            return s.Value!;
        }

        static string Review(YamlMappingNode map, string file, string field)
        {
            if (!map.Children.TryGetValue(new YamlScalarNode("review"), out var v)) return "";
            var text = v is YamlScalarNode s ? s.Value : null;
            if (text != HebrewReview.AgentChecked)
                throw new ContentException(file, field + ".review", $"must be {HebrewReview.AgentChecked}, or left out once a native speaker has read the text (#564)");
            return text;
        }

        static List<Source> Sources(YamlNode node, string file, string field)
        {
            var list = new List<Source>();
            if (!(node is YamlSequenceNode items)) throw new ContentException(file, field, "must be a list of { url, checked }");
            for (int i = 0; i < items.Children.Count; i++)
            {
                var at = $"{field}[{i}]";
                var map = Mapping(items.Children[i], file, at);
                Only(map, file, at, "url", "checked");
                var url = Text(Get(map, "url", file, at), file, at + ".url");
                var day = Get(map, "checked", file, at) is YamlScalarNode d ? d.Value : null;
                if (!DateTime.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    throw new ContentException(file, at + ".checked", "must be a date, YYYY-MM-DD");
                list.Add(new Source { Url = url, Checked = dt });
            }
            return list;
        }

        // ---- YAML 1.2 core schema, for "is this scalar text" (tools/yaml12.py reads the same files the same way) ----

        static readonly (string Tag, Regex Pattern)[] CoreResolvers =
        {
            ("null", new Regex(@"^(?:null|Null|NULL|~|)$", RegexOptions.CultureInvariant)),
            ("bool", new Regex(@"^(?:true|True|TRUE|false|False|FALSE)$", RegexOptions.CultureInvariant)),
            ("int", new Regex(@"^(?:[-+]?[0-9]+|0o[0-7]+|0x[0-9a-fA-F]+)$", RegexOptions.CultureInvariant)),
            ("float", new Regex(@"^(?:[-+]?(?:\.[0-9]+|[0-9]+(?:\.[0-9]*)?)(?:[eE][-+]?[0-9]+)?|[-+]?\.(?:inf|Inf|INF)|\.(?:nan|NaN|NAN))$",
                RegexOptions.CultureInvariant)),
        };

        /// <summary>The core-schema type of a scalar: "str", or null/bool/int/float for a plain scalar that resolves so.</summary>
        static string Core(YamlScalarNode s)
        {
            if (!s.Tag.IsEmpty && !s.Tag.IsNonSpecific) return s.Tag.Value == "tag:yaml.org,2002:str" ? "str" : s.Tag.Value;
            if (s.Style != ScalarStyle.Plain) return "str";
            foreach (var (tag, pattern) in CoreResolvers)
                if (pattern.IsMatch(s.Value ?? "")) return tag;
            return "str";
        }

        static bool IsString(YamlNode? node) => node is YamlScalarNode s && Core(s) == "str";

        static string? StringOf(YamlMappingNode map, string key) =>
            map.Children.TryGetValue(new YamlScalarNode(key), out var v) && IsString(v) ? ((YamlScalarNode)v).Value : null;

        // ---- writing (the body tools/translate.py, from #1604, writes under its header comment, byte for byte) ----

        /// <summary>The three files' bodies, as tools/translate.py writes them: block mappings down to double-quoted (JSON)
        /// strings, in the set's order. A language with no entries of a kind gets an empty body.</summary>
        public static (string Ui, string Events, string Content) Write(TranslationSet set)
        {
            var ui = new StringBuilder();
            foreach (var kv in set.Ui) ui.Append(Key(kv.Key)).Append(": ").Append(Json(kv.Value)).Append('\n');
            var events = new StringBuilder();
            foreach (var e in set.Events)
            {
                events.Append(Key(e.Id)).Append(":\n");
                events.Append("  title: ").Append(Json(e.Title)).Append('\n');
                events.Append("  text: ").Append(Json(e.Text)).Append('\n');
                events.Append("  options:\n");
                foreach (var o in e.Options) events.Append("    ").Append(Key(o.Key)).Append(": ").Append(Json(o.Value)).Append('\n');
                if (e.Review != "") events.Append("  review: ").Append(Json(e.Review)).Append('\n');
            }
            var content = new StringBuilder();
            foreach (var t in set.Content)
            {
                content.Append(Key(t.Key)).Append(":\n");
                content.Append("  text: ").Append(Json(t.Text)).Append('\n');
                if (t.Review != "") content.Append("  review: ").Append(Json(t.Review)).Append('\n');
                if (t.Sources.Count > 0)
                    content.Append("  sources: [").Append(string.Join(", ", t.Sources.Select(s =>
                        "{\"url\": " + Json(s.Url) + ", \"checked\": " + Json(s.Checked.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) + "}")))
                        .Append("]\n");
            }
            return (ui.ToString(), events.ToString(), content.ToString());
        }

        static string Key(string key) => PlainKey.IsMatch(key) ? key : Json(key);

        /// <summary>A JSON string, as Python's json.dumps(ensure_ascii=False) writes it (a valid YAML double-quoted scalar).</summary>
        internal static string Json(string s)
        {
            var b = new StringBuilder(s.Length + 2).Append('"');
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '"': b.Append("\\\""); break;
                    case '\\': b.Append("\\\\"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\r': b.Append("\\r"); break;
                    case '\t': b.Append("\\t"); break;
                    case '\b': b.Append("\\b"); break;
                    case '\f': b.Append("\\f"); break;
                    default:
                        if (ch < 0x20) b.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else b.Append(ch);
                        break;
                }
            }
            return b.Append('"').ToString();
        }

        // ---- resolving a content key ----

        /// <summary>The record a content key names in its file's YAML, and the names of its English and Hebrew fields.
        /// Throws unless the path leads to exactly one mapping whose English field is text.</summary>
        public static (YamlMappingNode Record, string English, string Hebrew) Resolve(YamlNode root, ContentKey key)
        {
            string at = key.File + "#" + string.Join(".", key.Path);
            YamlNode node = root;
            foreach (var step in key.Path)
            {
                if (node is YamlMappingNode map)
                {
                    if (!map.Children.TryGetValue(new YamlScalarNode(step), out var next))
                        throw new ContentException(key.File, at, $"no '{step}' in the record");
                    node = next;
                }
                else if (node is YamlSequenceNode list)
                {
                    var hits = list.Children.Where((x, i) => Ident(x, i) == step).ToList();
                    if (hits.Count != 1)
                        throw new ContentException(key.File, at, hits.Count == 0 ? $"no record '{step}'" : $"{hits.Count} records are '{step}': the key must name one");
                    node = hits[0];
                }
                else throw new ContentException(key.File, at, $"'{step}' is past the end of the record");
            }
            var record = node as YamlMappingNode ?? throw new ContentException(key.File, at, "not a record");
            if (IsString(Value(record, key.Field))) return (record, key.Field, key.Field + "_he");
            if (key.Field.EndsWith("_f", StringComparison.Ordinal) && IsString(Value(record, key.Field.Substring(0, key.Field.Length - 2))))
                return (record, key.Field.Substring(0, key.Field.Length - 2), key.Field.Substring(0, key.Field.Length - 2) + "_he_f");
            throw new ContentException(key.File, key.ToString(), $"the record has no English '{key.Field}' to translate");
        }

        static YamlNode? Value(YamlMappingNode map, string key) =>
            map.Children.TryGetValue(new YamlScalarNode(key), out var v) ? v : null;

        /// <summary>A list item's path step: its id when it has a text id, else its index.</summary>
        static string Ident(YamlNode item, int index) =>
            item is YamlMappingNode m && StringOf(m, "id") is string id ? id : index.ToString(CultureInfo.InvariantCulture);

        // ---- today's inline Hebrew, read as the contract's files ----

        /// <summary>Today's inline Hebrew (strings.he.yaml, the events' *_he fields, every content field with a _he form)
        /// as one translation set, in the order tools/translate.py lists it: the events by date and id, the UI strings in
        /// strings.en.yaml's order (any others after, by key), then the content fields by file and position.
        /// <paramref name="contentFiles"/> are the YAML files relative to content/, in order; <paramref name="eventFiles"/>
        /// the event files.</summary>
        public static TranslationSet InlineHebrew(Func<string, string?> readFile, IEnumerable<string> contentFiles,
            IEnumerable<string> eventFiles, string uiEnglish, string uiHebrew)
        {
            var set = new TranslationSet { Lang = Hebrew };
            var events = new List<(string From, string Id, YamlMappingNode Node)>();
            foreach (var rel in eventFiles)
            {
                var root = Root(readFile(rel), rel);
                if (StringOf(root, "id") is string id) events.Add((StringOf(root, "from") ?? "", id, root));
            }
            foreach (var (_, id, e) in events.OrderBy(x => x.From, StringComparer.Ordinal).ThenBy(x => x.Id, StringComparer.Ordinal))
            {
                if (string.IsNullOrEmpty(StringOf(e, "title_he"))) continue;
                var t = new EventTranslation { Id = id, Title = StringOf(e, "title_he")!, Text = StringOf(e, "text_he") ?? "" };
                if (!string.IsNullOrEmpty(StringOf(e, "text_he_review"))) t.Review = StringOf(e, "text_he_review")!;
                if (Value(e, "options") is YamlSequenceNode options)
                    foreach (var o in options.Children.OfType<YamlMappingNode>())
                        if (StringOf(o, "id") is string oid && !string.IsNullOrEmpty(StringOf(o, "text_he")))
                            t.Options.Add(new KeyValuePair<string, string>(oid, StringOf(o, "text_he")!));
                set.Events.Add(t);
            }

            var order = Entries(Root(uiEnglish, "strings.en.yaml"), "strings.en.yaml").Select((kv, i) => (kv.Key, i))
                .ToDictionary(x => x.Key, x => x.i, StringComparer.Ordinal);
            set.Ui = Entries(Root(uiHebrew, "strings.he.yaml"), "strings.he.yaml")
                .Where(kv => IsString(kv.Value))
                .Select(kv => new KeyValuePair<string, string>(kv.Key, ((YamlScalarNode)kv.Value).Value ?? ""))
                .OrderBy(kv => order.TryGetValue(kv.Key, out var i) ? i : order.Count).ThenBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();

            foreach (var rel in contentFiles)
                foreach (var f in Fields(rel, Root(readFile(rel), rel)))
                    if (f.Hebrew != "")
                        set.Content.Add(new ContentTranslation { Key = f.Key.ToString(), Text = f.Hebrew, Review = f.Review });
            return set;
        }

        /// <summary><see cref="InlineHebrew(Func{string, string?}, IEnumerable{string}, IEnumerable{string}, string, string)"/>
        /// for a checkout: content/*.yaml then content/map/*.yaml, each in ordinal order.</summary>
        public static TranslationSet InlineHebrew(string contentRoot, string uiEnglishPath, string uiHebrewPath)
        {
            IEnumerable<string> Yaml(string dir, string prefix) => Directory.Exists(System.IO.Path.Combine(contentRoot, dir))
                ? Directory.GetFiles(System.IO.Path.Combine(contentRoot, dir), "*.yaml").Select(f => prefix + System.IO.Path.GetFileName(f))
                    .OrderBy(f => f, StringComparer.Ordinal)
                : Enumerable.Empty<string>();
            string? Read(string rel)
            {
                var path = System.IO.Path.Combine(contentRoot, rel);
                return System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : null;
            }
            return InlineHebrew(Read, Yaml(".", "").Concat(Yaml("map", "map/")), Yaml("events", "events/"),
                System.IO.File.ReadAllText(uiEnglishPath), System.IO.File.ReadAllText(uiHebrewPath));
        }

        /// <summary>Every field of a content file that has a Hebrew form: one entry for each record shaped like one that has
        /// the _he key, with the English beside it, whether or not that record has its Hebrew yet (tools/translate.py's
        /// catalog). Within a record the fields come in the order the file first shows them.</summary>
        public static IEnumerable<(ContentKey Key, string English, string Hebrew, string Review)> Fields(string file, YamlNode root)
        {
            var nodes = new List<(string Shape, List<string> Ids, YamlMappingNode Node)>();
            void Walk(YamlNode node, List<string> ids, string shape)
            {
                if (node is YamlMappingNode map)
                {
                    nodes.Add((shape, ids, map));
                    foreach (var kv in map.Children)
                    {
                        var k = (kv.Key as YamlScalarNode)?.Value ?? "";
                        Walk(kv.Value, new List<string>(ids) { k }, shape + "\u0001" + k);
                    }
                }
                else if (node is YamlSequenceNode list)
                    for (int i = 0; i < list.Children.Count; i++)
                        Walk(list.Children[i], new List<string>(ids) { Ident(list.Children[i], i) }, shape + "\u0001*");
            }
            Walk(root, new List<string>(), "");

            var fields = new List<(string Shape, string Key)>();
            var seen = new HashSet<(string, string)>();
            foreach (var (shape, _, node) in nodes)
                foreach (var kv in node.Children)
                    if (kv.Key is YamlScalarNode k && HeKey.IsMatch(k.Value ?? "") && IsString(kv.Value) && seen.Add((shape, k.Value!)))
                        fields.Add((shape, k.Value!));

            foreach (var (shape, ids, node) in nodes)
                foreach (var (s, heKey) in fields)
                {
                    if (s != shape) continue;
                    var m = HeKey.Match(heKey);
                    var english = StringOf(node, m.Groups[1].Value);
                    if (english == null) continue;
                    var field = m.Groups[1].Value + (m.Groups[2].Success ? "_f" : "");
                    yield return (new ContentKey(file, ids.ToArray(), field), english, StringOf(node, heKey) ?? "",
                        string.IsNullOrEmpty(StringOf(node, heKey + "_review")) ? "" : StringOf(node, heKey + "_review")!);
                }
        }
    }
}
