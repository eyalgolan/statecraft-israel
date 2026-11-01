using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Statecraft.Sim;
using YamlDotNet.RepresentationModel;

/// <summary>#1618 step 2 (docs/translations.md, "Loading"): translations/ is read with the content, each language is checked
/// against the content it translates, a translations/he/ must equal the inline Hebrew, and none of it reaches the hash.</summary>
public class TranslationLoaderTests
{
    static string ContentDir => RealContentTests.ContentRoot();
    static string RepoDir => Path.GetDirectoryName(ContentDir)!;
    static string UiFile(string lang) => Path.Combine(RepoDir, "unity", "Assets", "UI", $"strings.{lang}.yaml");

    /// <summary>Today's inline Hebrew as a translation set (step 1's reader), re-read each time so a test can change it.</summary>
    static TranslationSet InlineHe() => Translations.InlineHebrew(ContentDir, UiFile("en"), UiFile("he"));

    static Dictionary<string, string> Files(TranslationSet set)
    {
        var (ui, events, content) = Translations.Write(set);
        var files = new Dictionary<string, string>();
        if (set.Ui.Count > 0) files[$"{set.Lang}/ui.yaml"] = ui;
        if (set.Events.Count > 0) files[$"{set.Lang}/events.yaml"] = events;
        if (set.Content.Count > 0) files[$"{set.Lang}/content.yaml"] = content;
        return files;
    }

    /// <summary>A scratch translations folder holding <paramref name="files"/> ("ro/ui.yaml" to its text).</summary>
    static string Folder(Dictionary<string, string> files)
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-tl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        foreach (var kv in files)
        {
            var path = Path.Combine(dir, kv.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, kv.Value);
        }
        return dir;
    }

    static T WithFolder<T>(Dictionary<string, string> files, Func<string, T> body)
    {
        var dir = Folder(files);
        try { return body(dir); }
        finally { Directory.Delete(dir, true); }
    }

    static Content LoadReal(Dictionary<string, string> files) => WithFolder(files, dir => ContentLoader.Load(ContentDir, dir));
    static ContentException Refused(Dictionary<string, string> files) => Assert.Throws<ContentException>(() => LoadReal(files));

    const string Ro = "ui.lang: \"ro\"\n";

    static string RoEvent(string id, params string[] options) =>
        $"{id}:\n  title: \"T\"\n  text: \"X\"\n  options:\n" + string.Concat(options.Select(o => $"    {o}: \"O\"\n"));

    static GameEvent RealEvent(Func<GameEvent, bool> pick) => ContentLoader.Load(ContentDir, null).Events.First(pick);

    // ---- where the translations come from ----

    [Fact]
    public void BesideFindsTheFolderNextToAFolderNamedContent()
    {
        Assert.Equal(Path.Combine(RepoDir, "translations"), TranslationLoader.Beside(ContentDir));
        Assert.Equal(Path.Combine(RepoDir, "translations"), TranslationLoader.Beside(ContentDir + Path.DirectorySeparatorChar));
        Assert.Null(TranslationLoader.Beside(Path.Combine(Path.GetTempPath(), "fixture-1234")));
    }

    [Fact]
    public void TheRealContentCarriesTheRepositorysTranslations()
    {
        var c = ContentLoader.Load(ContentDir);
        var ro = Assert.Single(c.Translations, t => t.Lang == "ro");
        Assert.Contains(ro.Ui, kv => kv.Key == Translations.LangKey && kv.Value == "ro");
        Assert.Contains(ro.Events, e => e.Id == "first_truce");
        Assert.Equal(c.Translations.Select(t => t.Lang).OrderBy(l => l, StringComparer.Ordinal), c.Translations.Select(t => t.Lang));
    }

    [Fact]
    public void AFixtureNeverPicksUpTranslations()
    {
        var dir = Fixture.Write();
        Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(dir)!, "translations", "ro"));
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(dir)!, "translations", "ro", "ui.yaml"), Ro);
        try { Assert.Empty(ContentLoader.Load(dir).Translations); }
        finally { Directory.Delete(Path.Combine(Path.GetDirectoryName(dir)!, "translations"), true); }
    }

    [Fact]
    public void TranslationsStayOutsideTheHash()
    {
        var bare = ContentLoader.Load(ContentDir, null);
        var withRo = ContentLoader.Load(ContentDir);
        var withHe = LoadReal(Files(InlineHe()));
        Assert.Empty(bare.Translations);
        Assert.NotEmpty(withRo.Translations);
        Assert.Equal("he", Assert.Single(withHe.Translations).Lang);
        Assert.Equal(bare.Hash, withRo.Hash);
        Assert.Equal(bare.Hash, withHe.Hash);
        var game = Simulation.NewGame(bare, 7, Mode.Historical).Hash();
        Assert.Equal(game, Simulation.NewGame(withRo, 7, Mode.Historical).Hash());
        Assert.Equal(game, Simulation.NewGame(withHe, 7, Mode.Historical).Hash());
    }

    // ---- the folder ----

    [Fact]
    public void CollectListsLanguageFilesAndIgnoresHiddenMetadata()
    {
        var dir = Folder(new Dictionary<string, string>
        {
            ["README.md"] = "top-level files are not languages",
            [".DS_Store"] = "", ["ro/.DS_Store"] = "", ["ro/.cache/x"] = "", [".git-keep/x"] = "",
            ["ro/ui.yaml"] = Ro, ["ro/events.yaml"] = "", ["pt-BR/content.yaml"] = "",
        });
        try { Assert.Equal(new[] { "pt-BR/content.yaml", "ro/events.yaml", "ro/ui.yaml" }, TranslationLoader.Collect(dir)); }
        finally { Directory.Delete(dir, true); }
        Assert.Empty(TranslationLoader.Collect(null));
        Assert.Empty(TranslationLoader.Collect(Path.Combine(Path.GetTempPath(), "no-such-" + Guid.NewGuid().ToString("N"))));
    }

    [Theory]
    [InlineData("ro/notes.txt", "translations/ro/notes.txt: (file): ")]
    [InlineData("ro/extra.yaml", "translations/ro/extra.yaml: (file): ")]
    [InlineData("Romanian/ui.yaml", "translations/Romanian: (folder): not a language code")]
    [InlineData("ro/old/ui.yaml", "translations/ro: (folder): a language folder has no sub-folders")]
    public void CollectRefusesWhatIsNotALanguagesFile(string stray, string message)
    {
        var dir = Folder(new Dictionary<string, string> { ["ro/ui.yaml"] = Ro, [stray] = "" });
        try { Assert.StartsWith(message, Assert.Throws<ContentException>(() => TranslationLoader.Collect(dir)).Message); }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void AFolderThatCannotBeListedIsAContentError()
    {
        // A language folder that cannot be listed is reported as a content error naming the folder (the CLI's exit 2,
        // ContentSync's BuildFailedException), not an unhandled exception (review C4).
        if (OperatingSystem.IsWindows()) return;
        var dir = Folder(new Dictionary<string, string> { ["ro/ui.yaml"] = Ro });
        var ro = Path.Combine(dir, "ro");
        File.SetUnixFileMode(ro, UnixFileMode.None);
        try
        {
            try { Directory.GetFiles(ro); return; }   // a superuser lists it anyway: nothing to test
            catch (UnauthorizedAccessException) { }
            var e = Assert.Throws<ContentException>(() => TranslationLoader.Collect(dir));
            Assert.Equal(("translations", "(folder)"), (e.File, e.Field));
            Assert.Contains(ro, e.Message);
        }
        finally
        {
            File.SetUnixFileMode(ro, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            Directory.Delete(dir, true);
        }
    }

    // ---- each language's checks ----

    [Fact]
    public void ALanguageMustNameItself()
    {
        Assert.Equal("translations/ro/ui.yaml: ui.lang: a UI language must name itself: add ui.lang: \"ro\"",
            Refused(new Dictionary<string, string> { ["ro/events.yaml"] = RoEvent("first_truce", "accept", "refuse") }).Message);
        Assert.Equal("translations/ro/ui.yaml: ui.lang: says 'fr' but the file is ro's",
            Refused(new Dictionary<string, string> { ["ro/ui.yaml"] = "ui.lang: \"fr\"\n" }).Message);
    }

    [Theory]
    [InlineData("advisor.defeat_economy")]
    [InlineData("how_to_play.title")]
    public void OnlyHebrewTranslatesTheAdvisorsAndHowToPlay(string key)
    {
        var e = Refused(new Dictionary<string, string> { ["ro/ui.yaml"] = Ro + $"{key}: \"x\"\n" });
        Assert.Equal("translations/ro/ui.yaml", e.File);
        Assert.Equal(key, e.Field);
    }

    [Fact]
    public void AnEventTranslationNamesARealEventAndEveryOption()
    {
        var e = RealEvent(x => x.Options.Count >= 2);
        var ids = e.Options.Select(o => o.Id).ToArray();
        Assert.Single(LoadReal(new Dictionary<string, string> { ["ro/ui.yaml"] = Ro, ["ro/events.yaml"] = RoEvent(e.Id, ids) }).Translations);

        var unknown = Refused(new Dictionary<string, string> { ["ro/ui.yaml"] = Ro, ["ro/events.yaml"] = RoEvent("no_such_event", "a") });
        Assert.Equal(("translations/ro/events.yaml", "no_such_event"), (unknown.File, unknown.Field));
        var missing = Refused(new Dictionary<string, string> { ["ro/ui.yaml"] = Ro, ["ro/events.yaml"] = RoEvent(e.Id, ids.Skip(1).ToArray()) });
        Assert.Equal($"{e.Id}.options.{ids[0]}", missing.Field);
        Assert.Contains("all or none", missing.Message);
        var extra = Refused(new Dictionary<string, string> { ["ro/ui.yaml"] = Ro, ["ro/events.yaml"] = RoEvent(e.Id, ids.Append("no_such_option").ToArray()) });
        Assert.Equal($"{e.Id}.options.no_such_option", extra.Field);
    }

    [Fact]
    public void AnEventARulesetLeavesOutCanStillBeTranslated()
    {
        // A gdd-0.3 fixture that leaves its one event out (gdd03-events.yaml): the event is still real content.
        var f = Fixture.Gdd();
        f["eras.yaml"] = f["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3");
        f[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(Path.Combine(ContentDir, Gdd03BalanceCatalogue.FileName));
        f["gdd03-events.yaml"] = "ruleset: gdd-0.3\nexclusions:\n  - { id: probe, reason: Fixture }\n";
        var dir = Fixture.Write(f, new Dictionary<string, string>
        {
            ["probe.yaml"] = "id: probe\ntitle: Fixture\ntext: Fixture.\nfrom: 1948-05-15\noptions:\n  - id: ok\n    text: Accept\n    historical: true\n",
        });
        var c = WithFolder(new Dictionary<string, string> { ["ro/ui.yaml"] = Ro, ["ro/events.yaml"] = RoEvent("probe", "ok") }, t => ContentLoader.Load(dir, t));
        Assert.DoesNotContain(c.Events, e => e.Id == "probe");
        Assert.Equal("probe", Assert.Single(Assert.Single(c.Translations).Events).Id);
    }

    static YamlMappingNode YamlRoot(string rel)
    {
        var s = new YamlStream();
        s.Load(new StringReader(File.ReadAllText(Path.Combine(ContentDir, rel))));
        return (YamlMappingNode)s.Documents[0].RootNode;
    }

    [Fact]
    public void AContentKeyMustBeATranslatableField()
    {
        var key = Translations.Fields("people.yaml", YamlRoot("people.yaml")).First().Key;
        var good = $"\"{key}\":\n  text: \"Nume\"\n";
        Assert.Single(LoadReal(new Dictionary<string, string> { ["ro/ui.yaml"] = Ro, ["ro/content.yaml"] = good }).Translations);

        var notOffered = new ContentKey(key.File, key.Path, "born").ToString();
        var e = Refused(new Dictionary<string, string> { ["ro/ui.yaml"] = Ro, ["ro/content.yaml"] = $"\"{notOffered}\":\n  text: \"x\"\n" });
        Assert.Equal(("translations/ro/content.yaml", notOffered), (e.File, e.Field));
        var noFile = Refused(new Dictionary<string, string> { ["ro/ui.yaml"] = Ro, ["ro/content.yaml"] = "\"nope.yaml#things.a.name\":\n  text: \"x\"\n" });
        Assert.Equal("nope.yaml#things.a.name", noFile.Field);
        Assert.Contains("no content file nope.yaml", noFile.Message);
    }

    /// <summary>The readFile overload, as a player loads (WebGL): the real content without the files a player does not ship.</summary>
    static Content LoadAsPlayer(Dictionary<string, string> translations, Func<string, string?>? content = null)
    {
        var events = Directory.GetFiles(Path.Combine(ContentDir, "events"), "*.yaml").Select(f => "events/" + Path.GetFileName(f)).ToList();
        string? Disk(string rel) { var p = Path.Combine(ContentDir, rel); return File.Exists(p) ? File.ReadAllText(p) : null; }
        return ContentLoader.Load(content ?? (rel => rel == "map/localities_1948.yaml" ? null : Disk(rel)), events,
            rel => translations.TryGetValue(rel, out var t) ? t : null, translations.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList());
    }

    [Fact]
    public void AKeyIntoAFileAPlayerDoesNotShipIsCheckedWhereTheFileIsRead()
    {
        var key = Translations.Fields("map/localities_1948.yaml", YamlRoot("map/localities_1948.yaml")).First().Key;
        var bad = new ContentKey(key.File, key.Path, "no_such_field").ToString();
        var files = new Dictionary<string, string> { ["ro/ui.yaml"] = Ro, ["ro/content.yaml"] = $"\"{bad}\":\n  text: \"x\"\n" };
        Assert.Equal(bad, Refused(files).Field);   // on disk: the editor, the CLI and the build gate
        Assert.Single(LoadAsPlayer(files).Translations);   // a player has only the baked TSV, so the key is not checked there
    }

    // ---- translations/he/ must equal the inline Hebrew ----

    [Fact]
    public void AHebrewSetEqualToTheInlineHebrewLoads()
    {
        var he = Assert.Single(LoadReal(Files(InlineHe())).Translations);
        Assert.Equal(Translations.Write(InlineHe()), Translations.Write(he));
        Assert.Equal("he", Assert.Single(LoadAsPlayer(Files(InlineHe())).Translations).Lang);
    }

    [Fact]
    public void EveryContentFileWithInlineHebrewIsCompared()
    {
        var compared = new HashSet<string>(HebrewOverlay.Files.Concat(HebrewOverlay.UnityReads), StringComparer.Ordinal);
        var withHebrew = InlineHe().Content.Select(t => ContentKey.TryParse(t.Key)!.File).Distinct();
        Assert.All(withHebrew, f => Assert.Contains(f, compared));
    }

    [Fact]
    public void AHebrewSetMissingAFileUnityReadsIsRefused()
    {
        // A file whose Hebrew Unity reads itself (HebrewOverlay.UnityReads, not a sim binding), with every key into it
        // dropped from he/: the comparison must still walk the file and find its fields missing (review C7).
        var he = InlineHe();
        var file = HebrewOverlay.UnityReads.Except(HebrewOverlay.Files)
            .First(f => he.Content.Any(t => ContentKey.TryParse(t.Key)!.File == f));
        var first = he.Content.First(t => ContentKey.TryParse(t.Key)!.File == file).Key;
        he.Content.RemoveAll(t => ContentKey.TryParse(t.Key)!.File == file);
        var e = Refused(Files(he));
        Assert.Equal(("translations/he/content.yaml", first), (e.File, e.Field));
        Assert.Contains("missing", e.Message);
    }

    [Fact]
    public void AHebrewEventThatDiffersOrIsMissingIsRefused()
    {
        var he = InlineHe();
        he.Events[0].Title += "x";
        var e = Refused(Files(he));
        Assert.Equal(("translations/he/events.yaml", InlineHe().Events[0].Id), (e.File, e.Field));
        Assert.Contains("differs", e.Message);

        he = InlineHe();
        he.Events[0].Options[0] = new KeyValuePair<string, string>(he.Events[0].Options[0].Key, he.Events[0].Options[0].Value + "x");
        Assert.Contains("differs", Refused(Files(he)).Message);

        he = InlineHe();
        he.Events[0].Review = he.Events[0].Review == "" ? "agent-checked" : "";
        Assert.Contains("differs", Refused(Files(he)).Message);

        he = InlineHe();
        var dropped = he.Events[0].Id;
        he.Events.RemoveAt(0);
        e = Refused(Files(he));
        Assert.Equal(dropped, e.Field);
        Assert.Contains("missing", e.Message);
    }

    [Fact]
    public void AHebrewContentFieldThatDiffersIsMissingOrHasNoInlineHebrewIsRefused()
    {
        var he = InlineHe();
        var first = he.Content[0].Key;
        he.Content[0].Text += "x";
        var e = Refused(Files(he));
        Assert.Equal(("translations/he/content.yaml", first), (e.File, e.Field));
        Assert.Contains("differs", e.Message);

        he = InlineHe();
        he.Content[0].Review = he.Content[0].Review == "" ? "agent-checked" : "";
        Assert.Contains("differs", Refused(Files(he)).Message);

        he = InlineHe();
        he.Content.RemoveAt(0);
        e = Refused(Files(he));
        Assert.Equal(first, e.Field);
        Assert.Contains("missing", e.Message);

        // A field offered for translation that has no inline Hebrew yet.
        var bare = HebrewOverlay.Files.Concat(HebrewOverlay.UnityReads).Where(f => File.Exists(Path.Combine(ContentDir, f)))
            .SelectMany(f => Translations.Fields(f, YamlRoot(f))).First(f => f.Hebrew == "").Key.ToString();
        he = InlineHe();
        he.Content.Add(new ContentTranslation { Key = bare, Text = "שם" });
        e = Refused(Files(he));
        Assert.Equal(bare, e.Field);
        Assert.Contains("no inline Hebrew", e.Message);
    }

    [Fact]
    public void AHebrewEventWithNoInlineHebrewIsRefused()
    {
        // A fixture event with no Hebrew at all, so the case cannot vanish when the real content gains Hebrew everywhere.
        var dir = Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event });
        var plain = ContentLoader.Load(dir, null).Events.Single(x => x.Id == "drought");
        Assert.Equal("", plain.TitleHe);
        var files = new Dictionary<string, string>
        {
            ["he/ui.yaml"] = "ui.lang: \"he\"\n",
            ["he/events.yaml"] = "drought:\n  title: \"בצורת\"\n  text: \"טקסט\"\n  options:\n    ration: \"קיצוב\"\n    wait: \"המתנה\"\n",
        };
        var e = WithFolder(files, t => Assert.Throws<ContentException>(() => ContentLoader.Load(dir, t)));
        Assert.Equal(("translations/he/events.yaml", "drought"), (e.File, e.Field));
        Assert.Contains("no inline Hebrew to match", e.Message);
    }


    // ---- read failures keep their file ----

    [Fact]
    public void ATranslationThatCannotBeReadNamesItsFile()
    {
        var files = new Dictionary<string, string> { ["ro/ui.yaml"] = Ro };
        foreach (var fault in new Exception[] { new IOException("disk"), new UnauthorizedAccessException("denied") })
        {
            var events = Directory.GetFiles(Path.Combine(ContentDir, "events"), "*.yaml").Select(f => "events/" + Path.GetFileName(f)).ToList();
            string? Disk(string rel) { var p = Path.Combine(ContentDir, rel); return File.Exists(p) ? File.ReadAllText(p) : null; }
            var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Disk, events, _ => throw fault, files.Keys.ToList()));
            Assert.Equal(("translations/ro/ui.yaml", "(file)"), (e.File, e.Field));
            Assert.Contains(fault.Message, e.Message);
        }
    }

    [Fact]
    public void AContentFileThatCannotBeReadForTheChecksNamesItsFile()
    {
        // map/localities_1948.yaml is read only by the translation checks, so the fault is in that reader.
        var key = Translations.Fields("map/localities_1948.yaml", YamlRoot("map/localities_1948.yaml")).First().Key.ToString();
        var files = new Dictionary<string, string> { ["ro/ui.yaml"] = Ro, ["ro/content.yaml"] = $"\"{key}\":\n  text: \"x\"\n" };
        foreach (var fault in new Exception[] { new IOException("disk"), new UnauthorizedAccessException("denied") })
        {
            string? Content(string rel)
            {
                if (rel == "map/localities_1948.yaml") throw fault;
                var p = Path.Combine(ContentDir, rel);
                return File.Exists(p) ? File.ReadAllText(p) : null;
            }
            var e = Assert.Throws<ContentException>(() => LoadAsPlayer(files, Content));
            Assert.Equal(("map/localities_1948.yaml", "(file)"), (e.File, e.Field));
        }
    }

    // ---- the CLI ----

    static (int Code, string Output) Cli(params string[] args)
    {
        var info = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        info.ArgumentList.Add(typeof(Statecraft.Cli.InputParse).Assembly.Location);
        foreach (var a in args) info.ArgumentList.Add(a);
        using var child = Process.Start(info)!;
        var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(120_000)) { child.Kill(entireProcessTree: true); throw new TimeoutException(string.Join(" ", args)); }
        return (child.ExitCode, stdout.Result + stderr.Result);
    }

    [Fact]
    public void TheCliTakesATranslationsFolderAndRefusesABadOne()
    {
        var missing = Path.Combine(Path.GetTempPath(), "no-such-" + Guid.NewGuid().ToString("N"));
        var (code, output) = Cli("--content", ContentDir, "--translations", missing, "--auto", "historical", "--turns", "1");
        Assert.Equal(2, code);
        Assert.Contains($"translations directory not found: {missing}", output);

        (code, output) = WithFolder(new Dictionary<string, string> { ["ro/ui.yaml"] = "ui.lang: \"fr\"\n" },
            dir => Cli("--content", ContentDir, "--translations", dir, "--auto", "historical", "--turns", "1"));
        Assert.Equal(2, code);
        Assert.Contains("Content error: translations/ro/ui.yaml: ui.lang: says 'fr'", output);

        (code, output) = Cli("--preview", "--translations", ContentDir);
        Assert.Equal(1, code);
        Assert.Contains("--translations", output);
    }
}
