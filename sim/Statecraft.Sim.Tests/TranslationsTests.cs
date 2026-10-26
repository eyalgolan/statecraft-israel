using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Statecraft.Sim;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

/// <summary>The translation contract (#1618, step 1; docs/translations.md): translations/&lt;lang&gt;/{ui,events,content}.yaml,
/// their keys, and today's inline Hebrew read as those files. The game still reads the inline Hebrew; these tests prove
/// every Hebrew value survives the contract unchanged before any of it moves.</summary>
public class TranslationsTests
{
    static string Repo => Path.GetDirectoryName(RealContentTests.ContentRoot())!;
    static string Ui(string lang) => Path.Combine(Repo, "unity", "Assets", "UI", $"strings.{lang}.yaml");
    static string FixtureDir => Path.Combine(Repo, "sim", "Statecraft.Sim.Tests", "translations_fixture");

    /// <summary>Content files with Hebrew the contract does not key, pending the owner's decision on #1618: map labels
    /// their digitisers write as a sourced set, beside the Arabic (tools/borders_digitise.py, tools/control_digitise.py).
    /// The sim never reads them (test_borders.py, test_control.py), so they are named here, not in the sim.</summary>
    static readonly string[] NotKeyed = { "map/control_1948_49.geojson", "map/subdistricts_1948.geojson" };

    static readonly Lazy<TranslationSet> Inline = new Lazy<TranslationSet>(() =>
        Translations.InlineHebrew(RealContentTests.ContentRoot(), Ui("en"), Ui("he")));

    static TranslationSet ParseBack(TranslationSet set)
    {
        var (ui, events, content) = Translations.Write(set);
        return Translations.Parse(set.Lang, ui, events, content);
    }

    static YamlNode Yaml(string rel)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), rel))));
        return stream.Documents[0].RootNode;
    }

    static Dictionary<string, (string Text, string Review)> ByKey(IEnumerable<ContentTranslation> list) =>
        list.ToDictionary(t => t.Key, t => (t.Text, t.Review));

    static string Events(TranslationSet set) => Translations.Write(set).Events;

    // ---- one contract, two implementations ----

    [Fact]
    public void TheFixtureWritesTheBodiesTranslatePyWrites()
    {
        // tools/translate.py (#1604) gets the same check, so the tool and the game write one format.
        var set = Translations.InlineHebrew(Path.Combine(FixtureDir, "content"),
            Path.Combine(FixtureDir, "ui", "strings.en.yaml"), Path.Combine(FixtureDir, "ui", "strings.he.yaml"));
        var (ui, events, content) = Translations.Write(set);
        string Expected(string file) => File.ReadAllText(Path.Combine(FixtureDir, "expected", "he", file));
        Assert.Equal(Expected(Translations.UiFile), ui);
        Assert.Equal(Expected(Translations.EventsFile), events);
        Assert.Equal(Expected(Translations.ContentFile), content);
    }

    // ---- the real Hebrew, read through the contract ----

    [Fact]
    public void EveryHebrewValueReadsBackUnchanged()
    {
        var once = Translations.Write(Inline.Value);
        var back = ParseBack(Inline.Value);
        Assert.Equal(once, Translations.Write(back));   // byte for byte, every key, value, flag and position
        Assert.Equal(Inline.Value.Ui, back.Ui);
        Assert.Equal(ByKey(Inline.Value.Content), ByKey(back.Content));
    }

    [Fact]
    public void NoHebrewFieldIsMissedOrCountedTwice()
    {
        // A plain scan, independent of Fields(): every non-empty *_he / *_he_f text in content/*.yaml and
        // content/map/*.yaml, and every event's title_he and options' text_he. The inventory must have exactly these.
        var root = RealContentTests.ContentRoot();
        static bool Text(YamlNode n) => n is YamlScalarNode s && !string.IsNullOrEmpty(s.Value)
            && (s.Style != YamlDotNet.Core.ScalarStyle.Plain || !Regex.IsMatch(s.Value, "^(?:null|Null|NULL|~|true|True|TRUE|false|False|FALSE|[-+]?[0-9][0-9.eE+-]*)$"));
        int Count(YamlNode n, Func<string, bool> key) => n switch
        {
            YamlMappingNode m => m.Children.Sum(kv => (kv.Key is YamlScalarNode k && key(k.Value!) && Text(kv.Value) ? 1 : 0) + Count(kv.Value, key)),
            YamlSequenceNode q => q.Children.Sum(x => Count(x, key)),
            _ => 0,
        };
        var files = Directory.GetFiles(root, "*.yaml").Concat(Directory.GetFiles(Path.Combine(root, "map"), "*.yaml"));
        var scanned = files.Sum(f => Count(Yaml(Path.GetRelativePath(root, f)), k => Regex.IsMatch(k, "_he(_f)?$")));
        Assert.Equal(scanned, Inline.Value.Content.Count);
        Assert.Equal(Inline.Value.Content.Count, Inline.Value.Content.Select(t => t.Key).Distinct().Count());
        var events = Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(f => Yaml(Path.GetRelativePath(root, f))).ToList();
        Assert.Equal(events.Sum(e => Count(e, k => k == "title_he")), Inline.Value.Events.Count);
        Assert.Equal(events.Sum(e => Count(e, k => k == "text_he")), Inline.Value.Events.Sum(e => 1 + e.Options.Count));
        Assert.Equal(35, Inline.Value.Events.Count);
    }

    [Fact]
    public void AKeyThatYamlWouldTypeIsQuotedAndReadsBackAsText()
    {
        // An id or key spelt like null, a boolean or a number must survive every YAML reader as that text.
        var set = new TranslationSet { Lang = "he" };
        foreach (var k in new[] { "1948", "1e3", "null", "true", "on", "yes", "plain_key" })
            set.Ui.Add(new KeyValuePair<string, string>(k, "א"));
        set.Events.Add(new EventTranslation { Id = "no", Title = "א", Text = "ב",
            Options = { new KeyValuePair<string, string>("off", "ג"), new KeyValuePair<string, string>("y", "ד") } });
        var (ui, events, _) = Translations.Write(set);
        Assert.Contains("\"1948\": ", ui);
        Assert.Contains("\"on\": ", ui);
        Assert.Contains("\nplain_key: ", "\n" + ui);
        Assert.Contains("\"no\":\n", events);
        Assert.Contains("    \"off\": ", events);
        var back = ParseBack(set);
        Assert.Equal(set.Ui, back.Ui);
        Assert.Equal(new[] { "off", "y" }, back.Events.Single().Options.Select(o => o.Key));
    }

    [Theory]
    [InlineData("1948: \"א\"\n")]
    [InlineData("true: \"א\"\n")]
    [InlineData("null: \"א\"\n")]
    public void APlainKeyYamlWouldTypeIsRefused(string ui)
    {
        var e = Assert.Throws<ContentException>(() => Translations.Parse("he", ui, null, null));
        Assert.Contains("must be quoted", e.Message);
        Assert.Equal("1948", Translations.Parse("he", "\"1948\": \"א\"\n", null, null).Ui.Single().Key);
    }

    [Fact]
    public void TheUiStringsAreStringsHeYamlExactly()
    {
        var he = new DeserializerBuilder().Build().Deserialize<Dictionary<string, string>>(File.ReadAllText(Ui("he")));
        var en = new DeserializerBuilder().Build().Deserialize<Dictionary<string, string>>(File.ReadAllText(Ui("en")));
        Assert.Equal(he.OrderBy(kv => kv.Key, StringComparer.Ordinal), ParseBack(Inline.Value).Ui.OrderBy(kv => kv.Key, StringComparer.Ordinal));
        Assert.Equal(en.Keys, Inline.Value.Ui.Select(kv => kv.Key));   // in strings.en.yaml's order
    }

    [Fact]
    public void EveryContentKeyNamesOneRecordWhoseHebrewIsTheText()
    {
        var files = new Dictionary<string, YamlNode>();
        foreach (var t in Inline.Value.Content)
        {
            var key = ContentKey.TryParse(t.Key);
            Assert.NotNull(key);
            Assert.Equal(t.Key, key!.ToString());
            if (!files.TryGetValue(key.File, out var root)) files[key.File] = root = Yaml(key.File);
            var (record, english, hebrew) = Translations.Resolve(root, key);
            Assert.Equal(t.Text, ((YamlScalarNode)record.Children[new YamlScalarNode(hebrew)]).Value);
            var flag = record.Children.TryGetValue(new YamlScalarNode(hebrew + "_review"), out var r) ? ((YamlScalarNode)r).Value : "";
            Assert.Equal(flag, t.Review);
            Assert.True(record.Children.ContainsKey(new YamlScalarNode(english)), t.Key);
        }
    }

    [Fact]
    public void TheEventsAreTheEventFilesHebrew()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var withHebrew = c.Events.Where(e => e.TitleHe != "").ToList();
        Assert.Equal(withHebrew.Select(e => e.Id).OrderBy(x => x, StringComparer.Ordinal),
            Inline.Value.Events.Select(e => e.Id).OrderBy(x => x, StringComparer.Ordinal));
        Assert.Equal(withHebrew.OrderBy(e => e.From).ThenBy(e => e.Id, StringComparer.Ordinal).Select(e => e.Id),
            Inline.Value.Events.Select(e => e.Id));   // by date, then id
    }

    // ---- the typed records: the overlay fills exactly what the loader fills ----

    [Fact]
    public void TheOverlayHoldsWhatTheLoaderReadsFromTheInlineFields()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var typed = HebrewOverlay.Snapshot(c);
        var simFiles = new HashSet<string>(typed.Content.Select(t => ContentKey.TryParse(t.Key)!.File));
        var inlineSim = Inline.Value.Content.Where(t => simFiles.Contains(ContentKey.TryParse(t.Key)!.File));
        Assert.Equal(ByKey(inlineSim), ByKey(typed.Content));
        Assert.Equal(Events(Inline.Value), Events(new TranslationSet { Events = typed.Events.OrderBy(e => Inline.Value.Events.FindIndex(x => x.Id == e.Id)).ToList() }));
        // No gap: every keyed file is the sim's, or one Unity reads itself.
        Assert.All(Inline.Value.Content.Select(t => ContentKey.TryParse(t.Key)!.File).Distinct(),
            f => Assert.True(simFiles.Contains(f) || HebrewOverlay.UnityReads.Contains(f), f));
    }

    [Fact]
    public void ClearedContentGetsItsHebrewBackFromTheSet()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var before = Translations.Write(HebrewOverlay.Snapshot(c));
        HebrewOverlay.Clear(c);
        var empty = HebrewOverlay.Snapshot(c);
        Assert.Empty(empty.Events);
        Assert.Empty(empty.Content);
        HebrewOverlay.Apply(c, ParseBack(Inline.Value));
        Assert.Equal(before, Translations.Write(HebrewOverlay.Snapshot(c)));
    }

    [Fact]
    public void EveryHebrewPropertyOfTheContentTypesIsCovered()
    {
        // A new *He field on a content type must join the overlay, or its Hebrew would be lost when it moves.
        var covered = new HashSet<(Type, string)>(HebrewOverlay.Covered);
        var all = typeof(Content).Assembly.GetTypes()
            .Where(t => t.IsPublic && t.Namespace == "Statecraft.Sim")
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.PropertyType == typeof(string) && p.CanWrite && Regex.IsMatch(p.Name, "He(F)?$"))
                .Select(p => (t, p.Name)))
            .ToList();
        Assert.NotEmpty(all);
        Assert.All(all, p => Assert.True(covered.Contains(p), $"{p.t.Name}.{p.Name} is not in HebrewOverlay"));
    }

    // ---- what stays on the record, and what the contract does not key yet ----

    [Fact]
    public void NoKeyNamesARetainedField()
    {
        var retained = new[] { "_sources", "_form", "_from", "_until", "status", "reviewed_by", "reviewed_on" };
        Assert.All(Inline.Value.Content, t =>
            Assert.DoesNotContain(retained, r => ContentKey.TryParse(t.Key)!.Field.EndsWith(r, StringComparison.Ordinal)));
        // The retained fields really are in the content, so this checks something.
        var people = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "people.yaml"));
        Assert.Contains("title_he_form:", people);
        Assert.Contains("biography_sources:", people);
    }

    [Fact]
    public void OnlyTheNamedGeoJsonFilesHaveHebrewTheContractDoesNotKey()
    {
        var root = RealContentTests.ContentRoot();
        var withHebrew = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".yaml", StringComparison.Ordinal) && !f.EndsWith(".tsv", StringComparison.Ordinal))
            .Where(f => !Path.GetRelativePath(root, f).Replace('\\', '/').StartsWith("schema/", StringComparison.Ordinal))   // field names, not content
            .Where(f => Regex.IsMatch(File.ReadAllText(f), "\"[a-z_]+_he(_f)?\"\\s*:"))
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .OrderBy(f => f, StringComparer.Ordinal);
        Assert.Equal(NotKeyed, withHebrew);
    }

    [Fact]
    public void TheLocalitiesBakeIsGeneratedFromKeyedFields()
    {
        // localities_1948.tsv is baked from the YAML (tools/localities_bake.py), so its Hebrew comes from keyed fields.
        var tsv = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "map", "localities_1948.tsv"));
        var keyed = new HashSet<string>(Inline.Value.Content
            .Where(t => t.Key.StartsWith("map/localities_1948.yaml#", StringComparison.Ordinal)).Select(t => t.Text));
        var lines = tsv.Split('\n');
        var header = lines.First(l => l.StartsWith("#place\t", StringComparison.Ordinal)).Substring(1).Split('\t');
        var columns = new[] { Array.IndexOf(header, "name_he"), Array.IndexOf(header, "today_he") };
        Assert.DoesNotContain(-1, columns);
        var hebrew = lines.Where(l => l.StartsWith("place\t", StringComparison.Ordinal)).Select(l => l.Split('\t'))
            .SelectMany(cells => columns.Select(i => cells[i])).Where(v => v != "").Distinct().ToList();
        Assert.All(hebrew, h => Assert.Contains(h, keyed));
    }

    // ---- the parser refuses what the contract does not allow ----

    const string GoodEvent = "vote:\n  title: \"ההצבעה\"\n  text: \"טקסט\"\n  options:\n    yes_vote: \"כן\"\n";

    [Theory]
    [InlineData("vote:\n  title: \"א\"\n  text: \"ב\"\n  options:\n    a: \"ג\"\n  extra: \"x\"\n", "unknown field")]
    [InlineData("vote:\n  title: \"\"\n  text: \"ב\"\n  options:\n    a: \"ג\"\n", "empty")]
    [InlineData("vote:\n  text: \"ב\"\n  options:\n    a: \"ג\"\n", "required")]
    [InlineData("vote:\n  title: \"א\"\n  text: \"ב\"\n", "required")]
    [InlineData("vote:\n  title: \"א\"\n  text: \"ב\"\n  options:\n    a: \"ג\"\n  review: \"native\"\n", "must be agent-checked")]
    [InlineData("vote:\n  title: [\"א\"]\n  text: \"ב\"\n  options:\n    a: \"ג\"\n", "must be text")]
    [InlineData("vote:\n  title: 12\n  text: \"ב\"\n  options:\n    a: \"ג\"\n", "must be text")]
    [InlineData("Vote:\n  title: \"א\"\n  text: \"ב\"\n  options:\n    a: \"ג\"\n", "not an event id")]
    [InlineData(GoodEvent + GoodEvent, "")]
    public void TheEventsFileIsStrict(string events, string message)
    {
        var e = Assert.Throws<ContentException>(() => Translations.Parse("he", null, events, null));
        Assert.Contains(message, e.Message);
    }

    [Theory]
    [InlineData("\"people.yaml#people.ben_gurion.name\":\n  text: \"דוד\"\n  sources: [{url: \"https://x\", checked: 2026-10-24}]\n", "he", "stay on the record")]
    [InlineData("\"people.yaml#people.ben_gurion.name\":\n  text: \"David\"\n  sources: [{url: \"https://x\"}]\n", "fr", "required")]
    [InlineData("\"people.yaml#people.ben_gurion.name\":\n  text: \"David\"\n  sources: [{url: \"https://x\", checked: 24-10-2026}]\n", "fr", "a date")]
    [InlineData("\"people.yaml#name\":\n  text: \"דוד\"\n", "he", "not a content key")]
    [InlineData("\"../people.yaml#people.ben_gurion.name\":\n  text: \"דוד\"\n", "he", "not a content key")]
    [InlineData("\"people.yaml#people.ben_gurion.name\":\n  text: \"דוד\"\n  note: \"x\"\n", "he", "unknown field")]
    public void TheContentFileIsStrict(string content, string lang, string message)
    {
        var e = Assert.Throws<ContentException>(() => Translations.Parse(lang, null, null, content));
        Assert.Contains(message, e.Message);
    }

    [Theory]
    [InlineData("../he")]
    [InlineData("HE")]
    [InlineData("")]
    public void TheLanguageIsACode(string lang)
    {
        Assert.Throws<ContentException>(() => Translations.Parse(lang, null, null, null));
    }

    [Fact]
    public void ALanguageOtherThanHebrewMayCarryItsOwnSources()
    {
        var set = Translations.Parse("fr", "hud.turn: \"Tour {0}\"\n", null,
            "\"people.yaml#people.ben_gurion.name\":\n  text: \"David Ben Gourion\"\n  sources: [{url: \"https://example.org\", checked: 2026-10-24}]\n");
        Assert.Equal("https://example.org", set.Content.Single().Sources.Single().Url);
        Assert.Equal(set.Content.Single().Sources.Single().Url, ParseBack(set).Content.Single().Sources.Single().Url);
    }

    [Theory]
    [InlineData("hud turn: \"x\"\n", "not a UI string key")]
    [InlineData("hud.turn: \" \"\n", "empty")]
    [InlineData("hud.turn: \"א\"\nhud.turn: \"ב\"\n", "")]
    public void TheUiFileIsStrict(string ui, string message)
    {
        var e = Assert.Throws<ContentException>(() => Translations.Parse("he", ui, null, null));
        Assert.Contains(message, e.Message);
    }

    // ---- a key names exactly one record ----

    static YamlNode Doc(string yaml)
    {
        var s = new YamlStream();
        s.Load(new StringReader(yaml));
        return s.Documents[0].RootNode;
    }

    [Theory]
    [InlineData("people:\n  - id: a\n    name: A\n", "people.yaml#people.b.name", "no record 'b'")]
    [InlineData("people:\n  - id: a\n    name: A\n  - id: a\n    name: B\n", "people.yaml#people.a.name", "2 records")]
    [InlineData("people:\n  - id: a\n    born: 1886\n", "people.yaml#people.a.name", "no English 'name'")]
    [InlineData("people:\n  - id: a\n    name: 1948\n", "people.yaml#people.a.name", "no English 'name'")]
    [InlineData("other:\n  - id: a\n    name: A\n", "people.yaml#people.a.name", "no 'people'")]
    public void AKeyThatNamesNoSingleRecordIsRefused(string yaml, string key, string message)
    {
        var e = Assert.Throws<ContentException>(() => Translations.Resolve(Doc(yaml), ContentKey.TryParse(key)!));
        Assert.Contains(message, e.Message);
    }

    [Fact]
    public void TheFeminineTitleIsTitleF()
    {
        var (_, english, hebrew) = Translations.Resolve(Doc("offices:\n  - id: pm\n    title: Prime Minister\n    title_he_f: x\n"),
            ContentKey.TryParse("offices.yaml#offices.pm.title_f")!);
        Assert.Equal(("title", "title_he_f"), (english, hebrew));
    }

    // ---- the overlay refuses a set that does not fit the content ----

    [Theory]
    [InlineData("nope:\n  title: \"א\"\n  text: \"ב\"\n  options:\n    a: \"ג\"\n", null, "no event has this id")]
    [InlineData("altalena:\n  title: \"א\"\n  text: \"ב\"\n  options:\n    nope: \"ג\"\n", null, "no such option")]
    [InlineData(null, "\"people.yaml#people.nobody.name\":\n  text: \"א\"\n", "no record 'nobody'")]
    [InlineData(null, "\"rules.yaml#map_actions.capture.name\":\n  text: \"א\"\n", "holds no Hebrew")]
    [InlineData(null, "\"sites.yaml#sites.mefalsim.name\":\n  text: \"מפלסים\"\n  review: \"agent-checked\"\n", "no review flag")]
    public void TheOverlayRefusesWhatDoesNotFit(string? events, string? content, string message)
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var e = Assert.Throws<ContentException>(() => HebrewOverlay.Apply(c, Translations.Parse("he", null, events, content)));
        Assert.Contains(message, e.Message);
    }
}
