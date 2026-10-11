using System;
using System.Collections.Generic;
using System.IO;
using Statecraft.Sim;

public class ContentLoaderTests
{
    [Fact]
    public void LoadsValidFixture()
    {
        var c = ContentLoader.Load(Fixture.Write());
        Assert.Equal(2, c.Parties.Count);
        Assert.Equal(2, c.People.Count);
        Assert.Equal(10, c.Legislatures[1].Size);
        Assert.Equal(2, c.Effects.Count);
        Assert.Equal(5, c.Rules.StartRung);
    }

    [Theory]
    [InlineData("policy", "model.yaml", "policies[rationing].office")]
    [InlineData("effect", "model.yaml", "effects[rationing_food].office")]
    [InlineData("event", "events/drought.yaml", "events[drought].office")]
    [InlineData("budget", "rules.yaml", "budget_start.historical_title")]
    public void HistoricalTitlesCannotOwnGameplayResponsibilities(string kind, string file, string field)
    {
        var overrides = new Dictionary<string, string>
        {
            ["offices.yaml"] = Fixture.Files["offices.yaml"] +
                "\n  - { id: historical_title, title: Historical title, historical_only: true }\n"
        };
        var events = new Dictionary<string, string>();
        if (kind == "policy") overrides["model.yaml"] = Fixture.Files["model.yaml"].Replace(
            "default: 0.0\n    office: finance_minister", "default: 0.0\n    office: historical_title");
        if (kind == "effect") overrides["model.yaml"] = Fixture.Files["model.yaml"].Replace(
            "inertia: 2\n    office: finance_minister", "inertia: 2\n    office: historical_title");
        if (kind == "event") events["drought.yaml"] = ValidationTests.Event.Replace(
            "office: finance_minister", "office: historical_title");
        if (kind == "budget") overrides["rules.yaml"] = Fixture.Files["rules.yaml"].Replace(
            "budget_start: { finance_minister: 1.0 }", "budget_start: { historical_title: 1.0 }");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(overrides, events)));
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains("historical-only", ex.Message);
    }

    [Fact]
    public void HistoricalTitlesRemainAvailableForDatedTextPlaceholders()
    {
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
        {
            ["offices.yaml"] = Fixture.Files["offices.yaml"] +
                "\n  - { id: historical_title, title: Historical title, historical_only: true }\n",
            ["people.yaml"] = Fixture.Files["people.yaml"].Replace("office: finance_minister", "office: historical_title")
        }, new Dictionary<string, string>
        {
            ["drought.yaml"] = ValidationTests.Event.Replace("{finance_minister}", "{historical_title}")
        }));
        Assert.Equal("Def Two reports a drought.", EventSystem.Render(c, c.Events[0], new DateTime(1948, 6, 1)));
        Assert.Equal("finance_minister", c.Events[0].Office);
    }

    [Fact]
    public void PersonLifeMetadataHasSourcesPerFieldAndRemainsOptional()
    {
        var yaml = Fixture.Files["people.yaml"].Replace("    name: Pm One\n", @"    name: Pm One
    name_he: דמות לדוגמה
    born: 1900-01-01
    biography_sources:
      name_he: [{ url: 'https://example.org/name', checked: 2026-10-11 }]
      born: [{ url: 'https://example.org/birth', checked: 2026-10-11 }]
");
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = yaml }));
        var person = c.People[0];
        Assert.Equal("דמות לדוגמה", person.NameHe);
        Assert.Equal(new DateTime(1900, 1, 1), person.Born);
        Assert.Equal("https://example.org/name", person.BiographySources["name_he"][0].Url);
        Assert.Equal("https://example.org/birth", person.BiographySources["born"][0].Url);
        Assert.Null(c.People[1].Born);
        Assert.Empty(c.People[1].BiographySources);
    }

    static ContentException LoadPmOneWith(string lines) =>
        Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
        {
            ["people.yaml"] = Fixture.Files["people.yaml"].Replace("    name: Pm One\n", "    name: Pm One\n" + lines),
        })));

    [Theory]
    [InlineData("      bron: [{ url: 'https://example.org/birth', checked: 2026-10-11 }]\n", "people[pm_one].biography_sources.bron")]
    [InlineData("      born:\n", "people[pm_one].biography_sources.born")]
    [InlineData("      born: [~]\n", "people[pm_one].biography_sources.born[0]")]
    [InlineData("      born: [{}]\n", "people[pm_one].biography_sources.born[0]")]
    [InlineData("      born: [{ checked: 2026-10-11 }]\n", "people[pm_one].biography_sources.born[0]")]
    [InlineData("      born: [{ url: 'https://example.org/birth', checked: 2026-10-11 }, { url: 'https://example.org/b2' }]\n", "people[pm_one].biography_sources.born[1]")]
    public void BadBiographySourceNamesFileAndField(string sources, string field)
    {
        var ex = LoadPmOneWith("    born: 1900-01-01\n    biography_sources:\n" + sources);
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal(field, ex.Field);
    }

    [Theory]
    [InlineData("    born: 1900-01-01\n    died: 1900-01-01\n")]
    [InlineData("    born: 1948-05-15\n")]
    public void ImpossibleBirthDateIsAnError(string lines)
    {
        var ex = LoadPmOneWith(lines);
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[pm_one].born", ex.Field);
    }

    [Fact]
    public void BirthOnFirstTermStartLoads()
    {
        var yaml = Fixture.Files["people.yaml"].Replace("    name: Pm One\n", "    name: Pm One\n    born: 1948-05-14\n");
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = yaml }));
        Assert.Equal(new DateTime(1948, 5, 14), c.People[0].Born);
    }

    [Fact]
    public void UnknownPartyInSeatsNamesFileAndField()
    {
        var bad = Fixture.Files["legislatures.yaml"].Replace("beta: 6", "gamma: 6");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["legislatures.yaml"] = bad })));
        Assert.Equal("legislatures.yaml", ex.File);
        Assert.Equal("legislatures[house].seats.gamma", ex.Field);
    }

    [Fact]
    public void SeatsMustSumToSize()
    {
        var bad = Fixture.Files["legislatures.yaml"].Replace("size: 10", "size: 11");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["legislatures.yaml"] = bad })));
        Assert.Equal("legislatures[house].size", ex.Field);
    }

    [Fact]
    public void UnknownYamlPropertyIsAnError()
    {
        var bad = Fixture.Files["parties.yaml"].Replace("name: Beta", "nmae: Beta");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["parties.yaml"] = bad })));
        Assert.Equal("parties.yaml", ex.File);
    }

    [Fact]
    public void EffectTargetMustBeAnIndicator()
    {
        var bad = Fixture.Files["model.yaml"].Replace("target: morale", "target: moral");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["model.yaml"] = bad })));
        Assert.Equal("effects[rationing_morale].target", ex.Field);
    }

    [Fact]
    public void PersonOfficeMustExist()
    {
        var bad = Fixture.Files["people.yaml"].Replace("office: finance_minister", "office: treasury_boss");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = bad })));
        Assert.Equal("people[def_two].offices.treasury_boss", ex.Field);
    }

    [Fact]
    public void OfficeTermPartyMustExist()
    {
        var bad = Fixture.Files["people.yaml"].Replace("      - office: finance_minister\n", "      - office: finance_minister\n        party: gamma\n");
        Assert.NotEqual(Fixture.Files["people.yaml"], bad);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = bad })));
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[def_two].offices.finance_minister.party", ex.Field);
    }

    [Fact]
    public void OverlappingTermsMustAgreeOnParty()
    {
        var bad = Fixture.Files["people.yaml"]
            .Replace("      - office: prime_minister\n        from: 1948-05-14\n", "      - office: prime_minister\n        from: 1948-05-14\n        party: alpha\n")
            .Replace("      - office: defense_minister\n        from: 1948-05-14\n", "      - office: defense_minister\n        from: 1948-05-14\n        party: beta\n");
        Assert.Equal(2, bad.Split("party: ").Length - 1);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = bad })));
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[pm_one].offices", ex.Field);
        Assert.Contains("overlapping terms disagree on party", ex.Message);
        ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = bad.Replace("party: beta", "party: alpha") }));
    }

    [Fact]
    public void OfficeTermEndingBeforeItStartsIsAnError()
    {
        var bad = Fixture.Files["people.yaml"].Replace("to: 1949-01-01", "to: 1947-01-01");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = bad })));
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[pm_one].offices.defense_minister", ex.Field);
    }

    [Theory]
    [InlineData("offices.yaml", "id: finance_minister", "id: defense_minister", "offices[defense_minister].id")]
    [InlineData("parties.yaml", "id: beta", "id: alpha", "parties[alpha].id")]
    [InlineData("people.yaml", "id: def_two", "id: pm_one", "people[pm_one].id")]
    [InlineData("legislatures.yaml", "id: house", "id: council", "legislatures[council].id")]
    [InlineData("governments.yaml", "id: gov2", "id: gov1", "governments[gov1].id")]
    [InlineData("model.yaml", "id: food_supply", "id: morale", "indicators[morale].id")]
    [InlineData("model.yaml", "id: rationing\n    default", "id: morale\n    default", "policies[morale].id")]
    public void DuplicateIdIsAnError(string file, string original, string duplicate, string field)
    {
        var bad = Fixture.Files[file].Replace(original, duplicate);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { [file] = bad })));
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
    }
    [Fact]
    public void UnreadableFileIsAContentError()
    {
        if (OperatingSystem.IsWindows()) return;   // unix permissions only
        var dir = Fixture.Write();
        var path = Path.Combine(dir, "parties.yaml");
        File.SetUnixFileMode(path, UnixFileMode.None);
        try
        {
            var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
            Assert.Equal("parties.yaml", ex.File);
            Assert.Equal("(file)", ex.Field);
        }
        finally { File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
    }

    [Fact]
    public void LockedFileIsAContentError()
    {
        var dir = Fixture.Write();
        using (new FileStream(Path.Combine(dir, "parties.yaml"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
            Assert.Equal("parties.yaml", ex.File);
            Assert.Equal("(file)", ex.Field);
        }
    }

    [Fact]
    public void RulesStartIsRequired()
    {
        var bad = Fixture.Files["rules.yaml"].Replace("start: 1948-05-15\n", "");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = bad })));
        Assert.Equal("rules.yaml", ex.File);
        Assert.Equal("start", ex.Field);
        Assert.EndsWith("required", ex.Message);
    }

    [Theory]
    [InlineData("office:")]
    [InlineData("office: ''")]
    public void OfficeTermNeedsAnOffice(string replacement)
    {
        var bad = Fixture.Files["people.yaml"].Replace("office: finance_minister", replacement);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = bad })));
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[def_two].offices", ex.Field);
    }

    // I5: a WebGL build reads StreamingAssets through a URL, so the loader also takes a reader function.
    static Func<string, string?> Reader(string dir) => rel =>
    {
        var path = Path.Combine(dir, rel);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    };

    static string[] EventFiles(string dir) =>
        Array.ConvertAll(Directory.GetFiles(Path.Combine(dir, "events"), "*.yaml"), f => "events/" + Path.GetFileName(f));

    [Fact]
    public void ReaderAndDirectoryEntryPointsLoadTheSameContent()
    {
        var dir = Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event });
        var fromDir = ContentLoader.Load(dir);
        var fromReader = ContentLoader.Load(Reader(dir), EventFiles(dir));
        Assert.Single(fromReader.Events);
        Assert.Equal("events/drought.yaml", fromReader.Events[0].File);
        Assert.Equal(Simulation.NewGame(fromDir, 7, Mode.Historical).Hash(), Simulation.NewGame(fromReader, 7, Mode.Historical).Hash());
    }

    [Theory]
    [InlineData("parties.yaml")]
    [InlineData("events/drought.yaml")]
    public void MissingFileGivesTheSameErrorThroughBothEntryPoints(string missing)
    {
        var dir = Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event });
        var events = EventFiles(dir);
        File.Delete(Path.Combine(dir, missing));
        var viaReader = Assert.Throws<ContentException>(() => ContentLoader.Load(Reader(dir), events));
        Assert.Equal((missing, "(file)", "missing file"), (viaReader.File, viaReader.Field, viaReader.Message.Substring(viaReader.Message.LastIndexOf(": ") + 2)));
        if (missing.StartsWith("events/")) return;   // the directory entry point lists events itself, so it never asks for a deleted one
        var viaDir = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
        Assert.Equal(viaDir.Message, viaReader.Message);
        Assert.Equal(viaDir.Field, viaReader.Field);
    }
}

public class ContentTextTests
{
    static Func<string, string?> Reader(string file, string text)
    {
        var dir = Fixture.Write();   // a file on disk would turn the lone surrogate into U+FFFD, so the bad file is served from memory
        return rel => { if (rel == file) return text; var f = Path.Combine(dir, rel); return File.Exists(f) ? File.ReadAllText(f) : null; };
    }

    // (<S> stands for a lone surrogate: xUnit would mangle a real one in theory data.) A lone surrogate in a string handed to the loader (not through a YAML escape, which YamlDotNet rejects itself)
    // cannot be saved; it must be a load error naming file and field, not a crash later in End Turn.
    [Theory]
    [InlineData("offices.yaml", "id: finance_minister", "id: fin<S>", "offices[")]
    [InlineData("people.yaml", "name: Def Two", "name: Def<S>", "people[")]
    [InlineData("rules.yaml", "budget_start: { finance_minister: 1.0 }", "budget_start: { fin<S>: 1.0 }", "budget_start")]
    public void ATextThatIsNotValidIsALoadError(string file, string from, string to, string field)
    {
        var bad = Fixture.Files[file].Replace(from, to.Replace("<S>", "\uD800"));
        Assert.NotEqual(Fixture.Files[file], bad);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Reader(file, bad), new string[0]));
        Assert.Equal(file, ex.File);
        Assert.Contains("valid text", ex.Message);
        Assert.Contains(field, ex.Field);
    }
}
