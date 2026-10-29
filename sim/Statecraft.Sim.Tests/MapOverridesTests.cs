using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>content/map/overrides.yaml, the map editor's override layer (#1009 step 1; owner rulings of 18 October 2026).</summary>
public class MapOverridesTests
{
    const string Empty = "terrain: []\ncontrol: []\nlocations: []\n";

    static readonly List<MapHex> Hexes = new List<MapHex>
    {
        new MapHex { Q = 1, R = 2, Land = true, Biome = "hills" },
        new MapHex { Q = 2, R = 2, Land = true, Biome = "desert" },
        new MapHex { Q = 3, R = 2, Land = false, Biome = "sea" },
    };

    static MapOverrides Checked(string yaml)
    {
        var o = MapOverrides.Parse(yaml);
        o.ValidateMap(Hexes, new[] { "town_a", "fort_a" });
        o.ValidateControl(new[] { (1, 2), (2, 2) }, new[] { "opening", "truce1" }, new[] { "I", "E", "c", "-" });
        return o;
    }

    [Fact]
    public void TheShippedFileIsEmptySoNothingMoves()
    {
        var o = MapOverrides.Parse(File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), MapOverrides.FileName)));
        Assert.Empty(o.Terrain); Assert.Empty(o.Control); Assert.Empty(o.Locations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AMissingFileHasNoOverrides(string? yaml)
    {
        var o = MapOverrides.Parse(yaml);
        Assert.Empty(o.Terrain); Assert.Empty(o.Control); Assert.Empty(o.Locations);
    }

    [Fact]
    public void AValidFileChecks()
    {
        var o = Checked("terrain:\n  - { hex: { q: 1, r: 2 }, value: desert }\ncontrol:\n  - { hex: { q: 1, r: 2 }, period: truce1, value: E }\n"
            + "  - { hex: { q: 1, r: 2 }, period: opening, value: '-' }\nlocations:\n  - { id: fort_a, hex: { q: 2, r: 2 } }\n");
        Assert.True(o.TryLocation("fort_a", out var q, out var r));
        Assert.Equal((2, 2), (q, r));
        Assert.False(o.TryLocation("town_a", out _, out _));
    }

    [Fact]
    public void SerializeWritesTheShippedFormatInAStableOrderAndParsesBack()
    {
        // #1009 step 2: the editor's only writer. The shipped file comes back byte for byte, so an unchanged save writes nothing.
        var shipped = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), MapOverrides.FileName));
        Assert.Equal(shipped, MapOverrides.Serialize(MapOverrides.Parse(shipped)));
        var text = MapOverrides.Serialize(Checked("terrain:\n  - { hex: { q: 2, r: 2 }, value: desert }\n  - { hex: { q: 1, r: 2 }, value: hills }\n"
            + "control:\n  - { hex: { q: 1, r: 2 }, period: truce1, value: E }\n  - { hex: { q: 1, r: 2 }, period: opening, value: '-' }\n"
            + "locations:\n  - { id: town_a, hex: { q: 1, r: 2 } }\n  - { id: fort_a, hex: { q: 2, r: 2 } }\n"));
        Assert.Equal("# yaml-language-server: $schema=../schema/map-overrides.schema.json\n"
            + "terrain:\n  - { hex: { q: 1, r: 2 }, value: 'hills' }\n  - { hex: { q: 2, r: 2 }, value: 'desert' }\n"
            + "control:\n  - { hex: { q: 1, r: 2 }, period: 'opening', value: '-' }\n  - { hex: { q: 1, r: 2 }, period: 'truce1', value: 'E' }\n"
            + "locations:\n  - { id: 'fort_a', hex: { q: 2, r: 2 } }\n  - { id: 'town_a', hex: { q: 1, r: 2 } }\n", text);
        Assert.Equal(text, MapOverrides.Serialize(Checked(text)));
    }

    [Fact]
    public void AControlOverrideReplacesOnePeriodOfOneHexAndNothingElse()
    {
        var o = Checked("terrain: []\ncontrol:\n  - { hex: { q: 1, r: 2 }, period: truce1, value: E }\nlocations: []\n");
        var periods = new[] { "opening", "truce1" };
        Assert.Equal("IE", o.ApplyControl(1, 2, periods, "II"));
        Assert.Equal("cc", o.ApplyControl(2, 2, periods, "cc"));   // another hex keeps its history
    }

    [Theory]
    [InlineData("control: []\nlocations: []\n", "terrain", "must be a list")]
    [InlineData("terrain: []\ncontrol: []\nlocations: []\nnotes: x\n", "line 4", "")]
    [InlineData("terrain:\n  - { value: hills }\ncontrol: []\nlocations: []\n", "terrain[0].hex", "required")]
    [InlineData("terrain:\n  - { hex: { q: 1, r: 2 }, value: hills }\n  - { hex: { q: 1, r: 2 }, value: desert }\ncontrol: []\nlocations: []\n", "terrain[1].hex", "a second override")]
    [InlineData("terrain: []\ncontrol:\n  - { hex: { q: 1, r: 2 }, period: opening, value: I }\n  - { hex: { q: 1, r: 2 }, period: opening, value: E }\nlocations: []\n", "control[1].period", "a second override")]
    [InlineData("terrain: []\ncontrol: []\nlocations:\n  - { id: town_a, hex: { q: 1, r: 2 } }\n  - { id: town_a, hex: { q: 2, r: 2 } }\n", "locations[1].id", "a second override")]
    [InlineData("terrain:\n  - { hex: { q: 9, r: 9 }, value: hills }\ncontrol: []\nlocations: []\n", "terrain[0].hex", "unknown hex q=9 r=9")]
    [InlineData("terrain:\n  - { hex: { q: 3, r: 2 }, value: hills }\ncontrol: []\nlocations: []\n", "terrain[0].hex", "not a land hex")]
    [InlineData("terrain:\n  - { hex: { q: 1, r: 2 }, value: tundra }\ncontrol: []\nlocations: []\n", "terrain[0].value", "unknown terrain value 'tundra'")]
    [InlineData("terrain:\n  - { hex: { q: 1, r: 2 }, value: sea }\ncontrol: []\nlocations: []\n", "terrain[0].value", "unknown terrain value 'sea'")]
    [InlineData("terrain: []\ncontrol: []\nlocations:\n  - { id: nowhere, hex: { q: 1, r: 2 } }\n", "locations[0].id", "unknown location id 'nowhere'")]
    [InlineData("terrain: []\ncontrol: []\nlocations:\n  - { id: town_a, hex: { q: 9, r: 9 } }\n", "locations[0].hex", "unknown hex q=9 r=9")]
    [InlineData("terrain: []\ncontrol: []\nlocations:\n  - { id: town_a, hex: { q: 3, r: 2 } }\n", "locations[0].hex", "not a land hex")]
    [InlineData("terrain: []\ncontrol:\n  - { hex: { q: 9, r: 9 }, period: opening, value: I }\nlocations: []\n", "control[0].hex", "unknown hex q=9 r=9")]
    [InlineData("terrain: []\ncontrol:\n  - { hex: { q: 1, r: 2 }, period: later, value: I }\nlocations: []\n", "control[0].period", "unknown period 'later'")]
    [InlineData("terrain: []\ncontrol:\n  - { hex: { q: 1, r: 2 }, period: opening, value: Z }\nlocations: []\n", "control[0].value", "unknown control value 'Z'")]
    public void InvalidInputNamesTheFileAndField(string yaml, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Checked(yaml));
        Assert.Equal(MapOverrides.FileName, ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // Through the content loader: the sim applies terrain and location overrides last, so every caller (the CLI, the
    // goldens, the game) sees the same map.

    [Fact]
    public void TheLoaderAppliesTerrainAndLocationOverrides()
    {
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
        {
            [MapOverrides.FileName] = "terrain:\n  - { hex: { q: 3, r: 1 }, value: hills }\ncontrol: []\nlocations:\n  - { id: town_a, hex: { q: 2, r: 1 } }\n",
        }));
        Assert.Equal("hills", c.HexAt(3, 1)!.Biome);
        Assert.Equal("coastal_plain", c.HexAt(1, 0)!.Biome);   // every other hex as generated
        var s = c.SettlementHexes.Single(h => h.Id == "town_a");
        Assert.Equal((2, 1), (s.Q, s.R));
        var l = c.MapLocations.Single(x => x.Id == "town_a");
        Assert.Equal((2, 1), (l.Q, l.R));
        Assert.Equal((34.29, 33.35), (l.Lon, l.Lat));   // the sourced coordinates stay
    }

    [Fact]
    public void WithoutTheFileTheLoaderMovesNothing()
    {
        var c = ContentLoader.Load(Fixture.Write());
        Assert.Equal("desert", c.HexAt(3, 1)!.Biome);
        var s = c.SettlementHexes.Single(h => h.Id == "town_a");
        Assert.Equal((1, 0), (s.Q, s.R));
    }

    [Fact]
    public void TheLoaderRejectsAnUnknownLocationNamingTheOverrideFile()
    {
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
        {
            [MapOverrides.FileName] = "terrain: []\ncontrol: []\nlocations:\n  - { id: nowhere, hex: { q: 2, r: 1 } }\n",
        })));
        Assert.Equal(MapOverrides.FileName, ex.File);
        Assert.Equal("locations[0].id", ex.Field);
    }

    [Fact]
    public void TheEmptyFileIsInTheContentHashAndChangesNothingElse()
    {
        var without = ContentLoader.Load(Fixture.Write());
        var with = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { [MapOverrides.FileName] = Empty }));
        Assert.NotEqual(without.Hash, with.Hash);
        Assert.Equal(without.Hexes.Select(h => h.Biome), with.Hexes.Select(h => h.Biome));
        Assert.Equal(without.SettlementHexes.Select(h => (h.Id, h.Q, h.R)), with.SettlementHexes.Select(h => (h.Id, h.Q, h.R)));
    }
    // R-MEDIT-001, R-MEDIT-002, R-MEDIT-003, R-LOOP-001, R-LOOP-002:
    // All editor-0.3 examples are SYNTHETIC, fixture-only, never historical authoring data.
    const string EditorFixture = "version: map-editor-0.3\nruleset: gdd-0.3\ndraft: true\n"
        + "base_revision: fixture-only\nbase_digest: "
        + "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\n"
        + "overlay: { terrain: [], control: [], locations: [] }\n"
        + "workable: []\nregions: []\ninfrastructure: []\n";

    [Fact]
    public void EditorEnvelopeRoundTripsWithoutEnteringTheLegacyParser()
    {
        var value = MapEditorOverlay.Parse(EditorFixture, SaveGame.Gdd03Ruleset);
        var text = MapEditorOverlay.Serialize(value);
        Assert.Equal(text, MapEditorOverlay.Serialize(MapEditorOverlay.Parse(text, SaveGame.Gdd03Ruleset)));
        Assert.Throws<ContentException>(() => MapOverrides.Parse(text));
        Assert.Contains("MAPEDIT_RULESET", Assert.Throws<ContentException>(() =>
            MapEditorOverlay.Parse(text, SaveGame.GddRuleset)).Message);
        Assert.Contains("MAPEDIT_RULESET", Assert.Throws<ContentException>(() =>
            MapEditorOverlay.Parse(text, SaveGame.LegacyRuleset)).Message);
    }

    [Theory]
    [InlineData("overlay: { terrain: [], control: [], locations: [] }", "overlay: { terrain: [], control: [], locations: [{ id: fixture_only, hex: { q: 1 } }] }", "MAPEDIT_SHAPE")]
    [InlineData("draft: true", "draft: true\nunknown: 1", "MAPEDIT_SHAPE")]
    [InlineData("regions: []", "regions: [{ hex: { q: 1, r: 2 }, region_id: fixture_only }, { hex: { q: 1, r: 2 }, region_id: another_fixture_only }]", "MAPEDIT_DUPLICATE")]
    [InlineData("infrastructure: []", "infrastructure: [{ id: fixture_only, hex: { q: 1, r: 2 }, kind: fixture_only, parent_id: fixture_only }, { id: fixture_only, hex: { q: 2, r: 2 }, kind: fixture_only, parent_id: fixture_only }]", "MAPEDIT_DUPLICATE")]
    [InlineData("map-editor-0.3", "future", "MAPEDIT_VERSION")]
    [InlineData("draft: true", "draft: null", "MAPEDIT_SHAPE")]
    [InlineData("base_revision: fixture-only", "base_revision: ''", "MAPEDIT_BASE")]
    [InlineData("workable: []", "workable: [{ hex: { q: 1, r: -2 }, value: true }, { hex: { q: 1, r: -2 }, value: false }]", "MAPEDIT_DUPLICATE")]
    [InlineData("workable: []", "workable: [{ hex: { q: 1 }, value: true }]", "MAPEDIT_SHAPE")]
    [InlineData("regions: []", "regions: [{ hex: { q: 1, r: 2 }, region_id: '' }]", "MAPEDIT_SHAPE")]
    [InlineData("infrastructure: []", "infrastructure: [{ id: fixture_only, hex: { q: 1, r: 2 }, kind: fixture_only, parent_id: fixture_only, capacity: 3 }]", "MAPEDIT_SHAPE")]
    public void EditorShapeRefusalsHaveNamedErrors(string old, string replacement, string code)
    {
        var error = Assert.Throws<ContentException>(() =>
            MapEditorOverlay.Parse(EditorFixture.Replace(old, replacement), SaveGame.Gdd03Ruleset));
        Assert.Contains(code, error.Message);
        Assert.Equal(MapOverrides.FileName, error.File);
    }

    [Fact]
    public void MissingDependencyContractsAlwaysBlockCanonicalExportEvenForAnEmptyCandidate()
    {
        var value = MapEditorOverlay.Parse(EditorFixture.Replace("draft: true", "draft: false"), SaveGame.Gdd03Ruleset);
        var diagnostics = value.Diagnostics("current-fixture-only", new string('b', 64));
        Assert.Contains(diagnostics, x => x.Code == "MAPEDIT_BASE_CONFLICT" && x.Message.Contains("fixture-only"));
        Assert.Contains(diagnostics, x => x.Code == "MAPEDIT_DEPENDENCY_UNAVAILABLE");
        Assert.Equal(diagnostics.Select(x => x.Field).OrderBy(x => x, System.StringComparer.Ordinal), diagnostics.Select(x => x.Field));
        Assert.All(diagnostics, x => { Assert.Equal(MapOverrides.FileName, x.File); Assert.NotEmpty(x.Action); });
        Assert.False(value.CanExport);
    }

    [Fact]
    public void EditorNumbersAreInvariantAndCallerOwnedInputIsNotReordered()
    {
        var value = MapEditorOverlay.Parse(EditorFixture.Replace("workable: []",
            "workable: [{ hex: { q: 3, r: 2 }, value: false }, { hex: { q: 1, r: -2 }, value: true }]"), SaveGame.Gdd03Ruleset);
        var before = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var text = MapEditorOverlay.Serialize(value);
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("he-IL");
            Assert.Equal(text, MapEditorOverlay.Serialize(value));
            Assert.Contains("r: -2", text);
            Assert.Equal(3, value.Workable[0].Hex.Q);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = before; }
    }

    // R-MEDIT-002: same-run guard, no literal hash. A legacy overrides fixture parsed by the legacy path and the same
    // fixture carried through the authoring envelope give identical legacy text.
    [Fact]
    public void LegacyOverridesReadTheSameThroughTheLegacyPathAndTheAuthoringEnvelope()
    {
        const string legacy = "terrain:\n  - { hex: { q: 2, r: 2 }, value: desert }\n  - { hex: { q: 1, r: 2 }, value: hills }\n"
            + "control:\n  - { hex: { q: 1, r: 2 }, period: truce1, value: E }\n  - { hex: { q: 1, r: 2 }, period: opening, value: '-' }\n"
            + "locations:\n  - { id: town_a, hex: { q: 1, r: 2 } }\n  - { id: fort_a, hex: { q: 2, r: 2 } }\n";
        var viaLegacy = MapOverrides.Serialize(MapOverrides.Parse(legacy));
        var envelope = MapEditorOverlay.Parse(EditorFixture, SaveGame.Gdd03Ruleset);
        envelope.Overlay = MapOverrides.Parse(legacy);
        var back = MapEditorOverlay.Parse(MapEditorOverlay.Serialize(envelope), SaveGame.Gdd03Ruleset);
        Assert.Equal(viaLegacy, MapOverrides.Serialize(back.Overlay));
    }

    // R-MEDIT-002, R-LOOP-001/002: capture this test's baseline before authoring operations.
    // This checks noninterference in one run, not a base-versus-candidate runtime receipt.
    [Theory]
    [InlineData(false, "legacy")]
    [InlineData(true, "gdd-0.1")]
    public void AuthoringOperationsLeaveCapturedRuntimeBytesUnchanged(bool gdd, string ruleset)
    {
        var root = Fixture.Write(gdd ? Fixture.Gdd() : new Dictionary<string, string>());
        (string Hash, string State, string Save) Capture()
        {
            var content = ContentLoader.Load(root);
            Assert.Equal(ruleset, content.Ruleset);
            var session = GameSession.New(content, 99, Mode.Historical);
            session.Step(System.Array.Empty<Command>());
            session.Step(System.Array.Empty<Command>());
            return (content.Hash, session.State.Describe(), session.Save.Write());
        }
        var baseline = Capture();
        var value = MapEditorOverlay.Parse(EditorFixture, SaveGame.Gdd03Ruleset);
        var text = MapEditorOverlay.Serialize(value);
        Assert.False(MapEditorOverlay.Parse(text, SaveGame.Gdd03Ruleset).CanExport);
        Assert.NotEmpty(value.Diagnostics("fixture-only", new string('a', 64)));
        var after = Capture();
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(baseline.Hash), System.Text.Encoding.UTF8.GetBytes(after.Hash));
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(baseline.State), System.Text.Encoding.UTF8.GetBytes(after.State));
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(baseline.Save), System.Text.Encoding.UTF8.GetBytes(after.Save));
    }

    // R-MEDIT-001/002/003: fixture-only tokens are not approved regional or facility data.
    [Fact]
    public void ReferenceRecordsRoundTripButNeverImplyDomainApproval()
    {
        var yaml = EditorFixture.Replace("regions: []",
            "regions: [{ hex: { q: 1, r: -2 }, region_id: fixture_only }]")
            .Replace("infrastructure: []", "infrastructure: [{ id: fixture_only, hex: { q: 1, r: -2 }, kind: fixture_only, parent_id: fixture_only }]");
        var value = MapEditorOverlay.Parse(yaml, SaveGame.Gdd03Ruleset);
        var text = MapEditorOverlay.Serialize(value);
        var back = MapEditorOverlay.Parse(text, SaveGame.Gdd03Ruleset);
        Assert.Equal("fixture_only", back.Regions.Single().RegionId);
        Assert.Equal("fixture_only", back.Infrastructure.Single().ParentId);
        Assert.False(back.CanExport);
        Assert.Contains(back.Diagnostics("fixture-only", new string('a', 64)), x => x.Field == "facilities");
    }


    // R-MEDIT-002: all new provenance/registry fields share the schema token contract.
    [Theory]
    [InlineData("fixture-only")]
    [InlineData("fixture'only")]
    public void SingleLineTokensRoundTripExactly(string token)
    {
        var value = MapEditorOverlay.Parse(EditorFixture, SaveGame.Gdd03Ruleset);
        value.BaseRevision = token;
        value.Regions.Add(new MapEditorRegion { Hex = new MapEditorHex { Q = 1, R = -2 }, RegionId = token });
        value.Infrastructure.Add(new MapEditorInfrastructure {
            Hex = new MapEditorHex { Q = 1, R = -2 }, Id = token, Kind = token, ParentId = token });
        var text = MapEditorOverlay.Serialize(value);
        var back = MapEditorOverlay.Parse(text, SaveGame.Gdd03Ruleset);
        Assert.Equal(token, back.BaseRevision);
        Assert.Equal(token, back.Regions.Single().RegionId);
        Assert.Equal(token, back.Infrastructure.Single().Id);
        Assert.Equal(token, back.Infrastructure.Single().Kind);
        Assert.Equal(token, back.Infrastructure.Single().ParentId);
        Assert.Equal(text, MapEditorOverlay.Serialize(back));
    }

    [Theory]
    [InlineData("fixture\nrevision")]
    [InlineData("fixture\rrevision")]
    [InlineData("fixture\trevision")]
    [InlineData("fixture\n")]
    [InlineData("fixture revision")]
    public void TokenControlsAreRejectedByParsingAndObjectSerialization(string token)
    {
        // JSON quoting is a valid YAML double-quoted scalar, preserving escaped controls on input.
        var quoted = System.Text.Json.JsonSerializer.Serialize(token);
        foreach (var field in new[] { "base_revision", "region_id", "id", "kind", "parent_id" })
        {
            var yaml = field == "base_revision"
                ? EditorFixture.Replace("base_revision: fixture-only", "base_revision: " + quoted)
                : field == "region_id"
                    ? EditorFixture.Replace("regions: []", "regions: [{ hex: { q: 1, r: -2 }, region_id: " + quoted + " }]")
                    : EditorFixture.Replace("infrastructure: []",
                        "infrastructure: [{ hex: { q: 1, r: -2 }, id: fixture_only, kind: fixture_only, parent_id: fixture_only }]"
                            .Replace(", " + field + ": fixture_only", ", " + field + ": " + quoted));
            var code = field == "base_revision" ? "MAPEDIT_BASE" : "MAPEDIT_SHAPE";
            Assert.Contains(code, Assert.Throws<ContentException>(() => MapEditorOverlay.Parse(yaml, SaveGame.Gdd03Ruleset)).Message);
            var value = MapEditorOverlay.Parse(EditorFixture, SaveGame.Gdd03Ruleset);
            if (field == "base_revision") value.BaseRevision = token;
            else if (field == "region_id") value.Regions.Add(new MapEditorRegion {
                Hex = new MapEditorHex { Q = 1, R = -2 }, RegionId = token });
            else value.Infrastructure.Add(new MapEditorInfrastructure {
                Hex = new MapEditorHex { Q = 1, R = -2 }, Id = field == "id" ? token : "fixture_only",
                Kind = field == "kind" ? token : "fixture_only", ParentId = field == "parent_id" ? token : "fixture_only" });
            Assert.Contains(code, Assert.Throws<ContentException>(() => MapEditorOverlay.Serialize(value)).Message);
        }
    }

    // R-MEDIT-002: base_digest is exactly 64 lowercase hex characters; parse and object serialization agree.
    [Theory]
    [InlineData('A', 64)]   // uppercase hex: right length, wrong alphabet
    [InlineData('g', 64)]   // lowercase non-hex letter
    [InlineData('٣', 64)]   // non-ASCII digit
    [InlineData('a', 63)]   // one short
    [InlineData('a', 65)]   // one long
    public void BaseDigestMustBeSixtyFourLowercaseHexOnParseAndSerialize(char c, int length)
    {
        var digest = new string(c, length);
        var yaml = EditorFixture.Replace(new string('a', 64), digest);
        var parsed = Assert.Throws<ContentException>(() => MapEditorOverlay.Parse(yaml, SaveGame.Gdd03Ruleset));
        Assert.Contains("MAPEDIT_BASE", parsed.Message);
        Assert.Equal("base", parsed.Field);
        Assert.Equal(MapOverrides.FileName, parsed.File);

        var value = MapEditorOverlay.Parse(EditorFixture, SaveGame.Gdd03Ruleset);
        value.BaseDigest = digest;
        var written = Assert.Throws<ContentException>(() => MapEditorOverlay.Serialize(value));
        Assert.Contains("MAPEDIT_BASE", written.Message);
        Assert.Equal("base", written.Field);
        Assert.Equal(MapOverrides.FileName, written.File);
    }

}
