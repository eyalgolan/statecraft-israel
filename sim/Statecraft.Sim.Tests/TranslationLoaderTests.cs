using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>translations/ loaded with the content (#1618, step 2; docs/translations.md, "Loading"): every language parsed
/// and checked against the content, translations/he/ required to match the inline Hebrew, which stays authoritative, and
/// none of it in the content hash.</summary>
public class TranslationLoaderTests
{
    static string Root => RealContentTests.ContentRoot();
    static string Repo => Path.GetDirectoryName(Root)!;

    static readonly Lazy<TranslationSet> InlineHe = new Lazy<TranslationSet>(() => Translations.InlineHebrew(Root,
        Path.Combine(Repo, "unity", "Assets", "UI", "strings.en.yaml"), Path.Combine(Repo, "unity", "Assets", "UI", "strings.he.yaml")));

    /// <summary>The real content with a translations folder holding exactly <paramref name="files"/>.</summary>
    static Content Load(Dictionary<string, string> files)
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-translations-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var kv in files)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(dir, kv.Key))!);
                File.WriteAllText(Path.Combine(dir, kv.Key), kv.Value);
            }
            return ContentLoader.Load(Root, dir);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    static ContentException Bad(Dictionary<string, string> files) => Assert.Throws<ContentException>(() => Load(files));

    static Dictionary<string, string> Hebrew(TranslationSet set)
    {
        var (ui, events, content) = Translations.Write(set);
        return new Dictionary<string, string> { ["he/ui.yaml"] = ui, ["he/events.yaml"] = events, ["he/content.yaml"] = content };
    }

    static TranslationSet Copy(TranslationSet s) => Translations.Parse(s.Lang, Translations.Write(s).Ui, Translations.Write(s).Events, Translations.Write(s).Content);

    const string RoUi = "ui.lang: \"ro\"\n";
    const string FirstTruce = "first_truce:\n  title: \"Primul armistițiu\"\n  text: \"Text\"\n  options:\n    accept: \"Acceptă\"\n    refuse: \"Refuză\"\n";

    // ---- where translations come from ----

    [Fact]
    public void TheRealContentLoadsTheRepositorysTranslations()
    {
        var c = ContentLoader.Load(Root);
        var expected = TranslationLoader.Collect(Path.Combine(Repo, "translations")).Select(f => f.Split('/')[0]).Distinct();
        Assert.Equal(expected, c.Translations.Select(t => t.Lang));
        var ro = c.Translations.Single(t => t.Lang == "ro");
        Assert.Equal("ro", ro.Ui.Single(kv => kv.Key == Translations.LangKey).Value);
        Assert.Contains(ro.Events, e => e.Id == "first_truce");
    }

    [Fact]
    public void TranslationsSitBesideAFolderNamedContentOnly()
    {
        var sep = Path.DirectorySeparatorChar;
        Assert.Equal(Path.Combine(Repo, "translations"), TranslationLoader.Beside(Root));
        Assert.Equal(Path.Combine(Repo, "translations"), TranslationLoader.Beside(Root + sep));
        Assert.Null(TranslationLoader.Beside(Fixture.Write()));   // a test fixture's folder: never a stray translations/ beside it
    }

    [Fact]
    public void TranslationsStayOutOfTheContentHash()
    {
        var none = ContentLoader.Load(Root, null);
        Assert.Empty(none.Translations);
        Assert.Equal(none.Hash, ContentLoader.Load(Root).Hash);
        var withHebrew = Load(Hebrew(InlineHe.Value));
        Assert.Equal(none.Hash, withHebrew.Hash);
        Assert.Equal(Simulation.NewGame(none, 7, Mode.Historical).Hash(), Simulation.NewGame(withHebrew, 7, Mode.Historical).Hash());
    }

    [Fact]
    public void CollectListsTheLanguageFilesAndRefusesAnythingElse()
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-collect-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(dir, "ro"));
            Directory.CreateDirectory(Path.Combine(dir, "fr"));
            File.WriteAllText(Path.Combine(dir, "README.md"), "not a language");
            File.WriteAllText(Path.Combine(dir, "ro", "ui.yaml"), RoUi);
            File.WriteAllText(Path.Combine(dir, "ro", "events.yaml"), "");
            File.WriteAllText(Path.Combine(dir, "fr", "ui.yaml"), "");
            Assert.Equal(new[] { "fr/ui.yaml", "ro/events.yaml", "ro/ui.yaml" }, TranslationLoader.Collect(dir));
            Assert.Empty(TranslationLoader.Collect(Path.Combine(dir, "missing")));
            Assert.Empty(TranslationLoader.Collect(null));
            File.WriteAllText(Path.Combine(dir, "ro", "notes.yaml"), "");
            Assert.Contains("has only ui.yaml", Assert.Throws<ContentException>(() => TranslationLoader.Collect(dir)).Message);
            File.Delete(Path.Combine(dir, "ro", "notes.yaml"));
            Directory.CreateDirectory(Path.Combine(dir, "Romanian"));
            Assert.Contains("not a language code", Assert.Throws<ContentException>(() => TranslationLoader.Collect(dir)).Message);
            Directory.Delete(Path.Combine(dir, "Romanian"));
            Directory.CreateDirectory(Path.Combine(dir, "ro", "old"));
            Assert.Contains("no sub-folders", Assert.Throws<ContentException>(() => TranslationLoader.Collect(dir)).Message);
        }
        finally { Directory.Delete(dir, true); }
    }

    // ---- every language is checked against the content ----

    [Theory]
    [InlineData("ui.lang: \"fr\"\n", "", "says 'fr'")]
    [InlineData("hud.turn: \"Tur {0}\"\n", "", "must name itself")]
    [InlineData(RoUi + "advisor.show_me: \"Arată-mi\"\n", "", "English or Hebrew only")]
    [InlineData(RoUi + "how_to_play.title: \"Cum se joacă\"\n", "", "English or Hebrew only")]
    [InlineData(RoUi, "nope:\n  title: \"a\"\n  text: \"b\"\n  options:\n    continue: \"c\"\n", "no event has this id")]
    [InlineData(RoUi, "first_truce:\n  title: \"a\"\n  text: \"b\"\n  options:\n    accept: \"c\"\n", "translates every option")]
    [InlineData(RoUi, "first_truce:\n  title: \"a\"\n  text: \"b\"\n  options:\n    accept: \"c\"\n    refuse: \"d\"\n    wait: \"e\"\n", "no such option")]
    public void ALanguageIsCheckedAgainstTheContent(string ui, string events, string message)
    {
        var files = new Dictionary<string, string> { ["ro/ui.yaml"] = ui };
        if (events != "") files["ro/events.yaml"] = events;
        var e = Bad(files);
        Assert.Contains(message, e.Message);
        Assert.StartsWith("translations/ro/", e.File);
    }

    [Theory]
    [InlineData("\"peopel.yaml#people.ben_gurion.name\":\n  text: \"x\"\n", "no content file peopel.yaml")]
    [InlineData("\"people.yaml#people.nobody.name\":\n  text: \"x\"\n", "not a translatable field")]
    [InlineData("\"people.yaml#people.ben_gurion.born\":\n  text: \"x\"\n", "not a translatable field")]
    [InlineData("\"rules.yaml#map_actions.capture.name\":\n  text: \"x\"\n", "not a translatable field")]
    public void AContentKeyMustBeATranslatableField(string content, string message)
    {
        Assert.Contains(message, Bad(new Dictionary<string, string> { ["ro/ui.yaml"] = RoUi, ["ro/content.yaml"] = content }).Message);
    }

    [Fact]
    public void AValidLanguageLoads()
    {
        var c = Load(new Dictionary<string, string>
        {
            ["ro/ui.yaml"] = RoUi + "hud.turn: \"Tura {0}\"\n",
            ["ro/events.yaml"] = FirstTruce,
            ["ro/content.yaml"] = "\"people.yaml#people.ben_gurion.name\":\n  text: \"David Ben-Gurion\"\n"
                + "\"map/units_1948.yaml#units.golani.name\":\n  text: \"Brigada Golani\"\n",
        });
        var ro = c.Translations.Single();
        Assert.Equal(("ro", 2, 1, 2), (ro.Lang, ro.Ui.Count, ro.Events.Count, ro.Content.Count));
        Assert.Equal("David Ben-Gurion", c.People.Single(p => p.Id == "ben_gurion").Name);   // the content itself is untouched
    }

    [Fact]
    public void AFileThePlayerDoesNotShipIsCheckedWhereItCanBeRead()
    {
        // localities_1948.yaml is a tool input: players get its baked TSV, not the YAML, so a player cannot check a key
        // into it. The editor, the CLI and the build gate read it, and refuse a bad one.
        const string bad = "\"map/localities_1948.yaml#places.nowhere.name\":\n  text: \"x\"\n";
        Assert.Contains("not a translatable field", Bad(new Dictionary<string, string> { ["ro/ui.yaml"] = RoUi, ["ro/content.yaml"] = bad }).Message);
        var events = Directory.GetFiles(Path.Combine(Root, "events"), "*.yaml").Select(f => "events/" + Path.GetFileName(f)).ToList();
        string? Player(string rel) => rel == "map/localities_1948.yaml" ? null
            : File.Exists(Path.Combine(Root, rel)) ? File.ReadAllText(Path.Combine(Root, rel)) : null;
        var files = new Dictionary<string, string> { ["ro/ui.yaml"] = RoUi, ["ro/content.yaml"] = bad };
        var c = ContentLoader.Load(Player, events, rel => files.TryGetValue(rel, out var t) ? t : null, files.Keys.OrderBy(k => k, StringComparer.Ordinal));
        Assert.Single(c.Translations.Single().Content);
    }

    [Fact]
    public void TheReadFileOverloadTakesTheManifestsList()
    {
        var events = Directory.GetFiles(Path.Combine(Root, "events"), "*.yaml").Select(f => "events/" + Path.GetFileName(f)).ToList();
        string? Disk(string rel) => File.Exists(Path.Combine(Root, rel)) ? File.ReadAllText(Path.Combine(Root, rel)) : null;
        var files = new Dictionary<string, string> { ["ro/ui.yaml"] = RoUi };
        Assert.Contains("listed but missing", Assert.Throws<ContentException>(() =>
            ContentLoader.Load(Disk, events, rel => null, new[] { "ro/ui.yaml" })).Message);
        Assert.Contains("listed twice", Assert.Throws<ContentException>(() =>
            ContentLoader.Load(Disk, events, rel => files[rel], new[] { "ro/ui.yaml", "ro/ui.yaml" })).Message);
        Assert.Contains("not <lang>/ui.yaml", Assert.Throws<ContentException>(() =>
            ContentLoader.Load(Disk, events, rel => files[rel], new[] { "../ui.yaml" })).Message);
        Assert.Equal("ro", ContentLoader.Load(Disk, events, rel => files[rel], files.Keys).Translations.Single().Lang);
    }

    // ---- translations/he/ must match the inline Hebrew ----

    [Fact]
    public void AHebrewSetThatMatchesTheInlineHebrewLoads()
    {
        var c = Load(Hebrew(InlineHe.Value));
        var he = c.Translations.Single(t => t.Lang == "he");
        Assert.Equal(Translations.Write(InlineHe.Value), Translations.Write(he));
    }

    [Fact]
    public void AHebrewSetThatDiffersFromTheInlineHebrewIsRefused()
    {
        var changedEvent = Copy(InlineHe.Value);
        changedEvent.Events[0].Title += " ";
        Assert.Contains("differs from the event's inline Hebrew", Bad(Hebrew(changedEvent)).Message);

        var changedOption = Copy(InlineHe.Value);
        var o = changedOption.Events[0].Options[0];
        changedOption.Events[0].Options[0] = new KeyValuePair<string, string>(o.Key, o.Value + "!");
        Assert.Contains("differs from the event's inline Hebrew", Bad(Hebrew(changedOption)).Message);

        var flag = Copy(InlineHe.Value);
        var flagged = flag.Content.First(t => t.Review != "");
        flagged.Review = "";
        Assert.Contains("differs from the inline Hebrew", Bad(Hebrew(flag)).Message);

        var text = Copy(InlineHe.Value);
        text.Content.First(t => t.Key.StartsWith("map/units_1948.yaml#", StringComparison.Ordinal)).Text += "!";
        Assert.Contains("differs from the inline Hebrew", Bad(Hebrew(text)).Message);
    }

    [Fact]
    public void AHebrewSetMissingAnInlineValueIsRefused()
    {
        var noEvent = Copy(InlineHe.Value);
        var id = noEvent.Events[0].Id;
        noEvent.Events.RemoveAt(0);
        var e = Bad(Hebrew(noEvent));
        Assert.Contains("missing: the event has inline Hebrew", e.Message);
        Assert.Equal(id, e.Field);

        var noField = Copy(InlineHe.Value);
        var key = noField.Content[noField.Content.Count / 2].Key;
        noField.Content.RemoveAll(t => t.Key == key);
        e = Bad(Hebrew(noField));
        Assert.Contains("missing: the field has inline Hebrew", e.Message);
        Assert.Equal(key, e.Field);
    }

    [Fact]
    public void AHebrewSetWithMoreThanTheInlineHebrewIsRefused()
    {
        var c = ContentLoader.Load(Root, null);
        var plain = c.Events.FirstOrDefault(e => e.TitleHe == "");
        var extra = Copy(InlineHe.Value);
        if (plain != null)
        {
            extra.Events.Add(new EventTranslation { Id = plain.Id, Title = "א", Text = "ב",
                Options = plain.Options.Select(o => new KeyValuePair<string, string>(o.Id, "ג")).ToList() });
            Assert.Contains("no inline Hebrew to match", Bad(Hebrew(extra)).Message);
        }
        // A content field offered for translation that has no Hebrew yet.
        var empty = Directory.GetFiles(Root, "*.yaml").Concat(Directory.GetFiles(Path.Combine(Root, "map"), "*.yaml"))
            .SelectMany(f =>
            {
                var y = new YamlDotNet.RepresentationModel.YamlStream();
                y.Load(new StringReader(File.ReadAllText(f)));
                return Translations.Fields(Path.GetRelativePath(Root, f).Replace('\\', '/'), y.Documents[0].RootNode);
            })
            .First(f => f.Hebrew == "");
        var field = Copy(InlineHe.Value);
        field.Content.Add(new ContentTranslation { Key = empty.Key.ToString(), Text = "שם" });
        var e = Bad(Hebrew(field));
        Assert.Contains("no inline Hebrew to match", e.Message);
        Assert.Equal(empty.Key.ToString(), e.Field);
    }
}
