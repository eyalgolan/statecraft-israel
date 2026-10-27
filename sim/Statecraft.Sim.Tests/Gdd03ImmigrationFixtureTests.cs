using System.Globalization;
using System.Text;
using Statecraft.Sim;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class Gdd03ImmigrationFixtureTests
{
    static Gdd03BalanceCatalogue Catalogue() => new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance).WithDuplicateKeyChecking().Build()
        .Deserialize<Gdd03BalanceCatalogue>(File.ReadAllText(Path.Combine(
            RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName)));

    // All ids, port references, ordinal origin and passenger ORDER here are test-only synthetic choices.
    // The 20-slot fixture realizes the catalogue mix; it does not approve a production cycle or ship size.
    static string Yaml()
    {
        var b = new StringBuilder("ruleset: gdd-0.3\nfixture_only: true\nid: synthetic_manifest\nfixture_port: synthetic_port\ninterval_turns: 2\naverage_units_per_ship: 3\nslots:\n");
        for (int i = 0; i < 20; i++)
        {
            var role = i == 0 ? "trained_soldier" : i == 4 || i == 12 ? "apprentice_craftsman"
                : i == 8 || i == 16 ? "free_civilian" : "refugee";
            b.Append("  - { ordinal: ").Append(i.ToString(CultureInfo.InvariantCulture))
                .Append(", role: ").Append(role)
                .Append(", reception_complete: ").Append(role == "refugee" ? "false" : "true")
                .Append(", profession: ").Append(role == "apprentice_craftsman" ? "craftsman" : "none")
                .Append(", tier: ").Append(role == "apprentice_craftsman" ? "apprentice" : "none")
                .Append(", qualification: ").Append(role == "trained_soldier" ? "trained" : "none")
                .Append(", equipment: ").Append(role == "trained_soldier" ? "military_package" : "none")
                .Append(" }\n");
        }
        return b.ToString();
    }
    static Gdd03ImmigrationFixture Parse(string yaml, Gdd03BalanceCatalogue? catalogue = null) =>
        Gdd03ImmigrationFixture.Parse(yaml, catalogue ?? Catalogue(), new[] { "synthetic_port" });

    // R-CITY-003: fixture contract only. No scheduler, population insertion or CBS suppression.
    [Fact] public void PreservesEveryAuthoredSlotAndExplicitEquipmentDeclaration()
    {
        var m = Parse(Yaml());
        Assert.Equal("synthetic_manifest", m.Id); Assert.Equal("synthetic_port", m.FixturePort);
        Assert.Equal(2, m.IntervalTurns); Assert.Equal(3, m.AverageUnitsPerShip);
        Assert.Equal(Enumerable.Range(0, 20), m.Slots.Select(s => s.Ordinal));
        Assert.Equal(new[] { "trained_soldier", "refugee", "refugee", "refugee", "apprentice_craftsman",
            "refugee", "refugee", "refugee", "free_civilian", "refugee", "refugee", "refugee",
            "apprentice_craftsman", "refugee", "refugee", "refugee", "free_civilian", "refugee", "refugee", "refugee" },
            m.Slots.Select(s => s.Role));
        Assert.Equal("military_package", m.Slots[0].Equipment);
        Assert.Equal("none", Parse(Yaml().Replace("equipment: military_package", "equipment: none")).Slots[0].Equipment);
        Assert.Equal("craftsman", m.Slots[4].Profession); Assert.Equal("apprentice", m.Slots[4].Tier);
        Assert.All(m.Slots.Where(s => s.Role != "refugee"), s => Assert.True(s.ReceptionComplete));
    }

    // R-CITY-003: every contract declaration is required; invalid data never receives defaults.
    [Theory]
    [InlineData("fixture_only: true\n", "")]
    [InlineData("fixture_only: true", "fixture_only: false")]
    [InlineData("ruleset: gdd-0.3", "ruleset: gdd-0.1")]
    [InlineData("id: synthetic_manifest", "id: ''")]
    [InlineData("fixture_port: synthetic_port", "fixture_port: missing")]
    [InlineData("interval_turns: 2", "interval_turns: 0")]
    [InlineData("interval_turns: 2", "interval_turns: 2.5")]
    [InlineData("average_units_per_ship: 3", "average_units_per_ship: 4")]
    [InlineData("ordinal: 0", "ordinal: 1")]
    [InlineData("ordinal: 1,", "ordinal: -1,")]
    [InlineData("ordinal: 0,", "")]
    [InlineData("role: refugee", "role: free_civilian")]
    [InlineData("role: trained_soldier", "role: unknown")]
    [InlineData("reception_complete: false", "reception_complete: true")]
    [InlineData("reception_complete: true", "reception_complete: false")]
    [InlineData("reception_complete: true,", "")]
    [InlineData("profession: craftsman", "profession: farmer")]
    [InlineData("tier: apprentice", "tier: expert")]
    [InlineData("qualification: trained", "qualification: none")]
    [InlineData("equipment: military_package", "equipment: tools")]
    [InlineData(", equipment: military_package", "")]
    [InlineData("equipment: none", "equipment: military_package")]
    [InlineData("slots:\n", "slots: null\n#\n")]
    public void RejectsInvalidOrMissingContractFields(string from, string to)
    {
        var e = Assert.Throws<ContentException>(() => Parse(Yaml().Replace(from, to)));
        Assert.Equal(Gdd03ImmigrationFixture.DiagnosticFile, e.File); Assert.False(string.IsNullOrWhiteSpace(e.Field));
    }

    // R-CITY-003: unknown/duplicate keys, nulls and empty cycles fail at the fixture boundary.
    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("slots: []")]
    public void RejectsEmptyDocuments(string yaml) => Assert.Throws<ContentException>(() => Parse(yaml));
    [Fact] public void RejectsUnknownAndDuplicateKeysAndNullSlots()
    {
        foreach (var yaml in new[] { Yaml() + "unexpected: true\n", Yaml() + "id: duplicate\n",
            Yaml().Replace("ordinal: 0", "ordinal: 0, mystery: true"),
            Yaml().Replace("ordinal: 0", "ordinal: 0, ordinal: 0"),
            Yaml().Substring(0, Yaml().IndexOf("slots:", StringComparison.Ordinal)) + "slots: [null]\n" })
            Assert.Throws<ContentException>(() => Parse(yaml));
    }

    // R-CITY-003: compare exact whole-slot shares; never round each ship's average into role counts.
    [Fact] public void RejectsWrongMixEvenWhenEachSlotIsIndividuallyValid()
    {
        var yaml = Yaml();
        var line = yaml.Split('\n').Single(s => s.Contains("ordinal: 1,"));
        var replacement = line.Replace("role: refugee", "role: free_civilian")
            .Replace("reception_complete: false", "reception_complete: true");
        Assert.Equal("slots.mix", Assert.Throws<ContentException>(() => Parse(yaml.Replace(line, replacement))).Field);
    }

    // R-CITY-003: Deferred or malformed catalogue rows cannot silently become numeric defaults.
    [Theory]
    [InlineData("immigration.ship_interval")]
    [InlineData("immigration.units_per_ship")]
    [InlineData("immigration.mix")]
    public void RequiresExactCatalogueRows(string key)
    {
        foreach (var mode in new[] { "missing", "deferred", "fraction", "shape", "null" })
        {
            var c = Catalogue(); var row = c.Values[key];
            if (mode == "missing") c.Values.Remove(key);
            if (mode == "deferred") { row.Basis = "Deferred"; row.Values.Clear(); row.Expression = "Deferred"; }
            if (mode == "fraction") row.Values[0] = 0.5m; // synthetic invalid value
            if (mode == "shape") row.Values.Add(1m); // synthetic malformed row
            if (mode == "null") c.Values[key] = null!;
            Assert.Throws<ContentException>(() => Parse(Yaml(), c));
        }
    }

    // R-CITY-003: format ordinals/diagnostics independently of the calling thread's culture.
    [Theory]
    [InlineData("he-IL")]
    [InlineData("en-US")]
    public void FixtureAndDiagnosticsAreCultureIndependent(string name)
    {
        var old = CultureInfo.CurrentCulture; var ui = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; var yaml = Yaml();
            var expected = Assert.Throws<ContentException>(() => Parse(yaml.Replace("tier: apprentice", "tier: expert"))).Message;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name); CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
            Assert.Equal(yaml, Yaml()); Assert.Equal(20, Parse(yaml).Slots.Count);
            Assert.Equal(expected, Assert.Throws<ContentException>(() => Parse(yaml.Replace("tier: apprentice", "tier: expert"))).Message);
        } finally { CultureInfo.CurrentCulture = old; CultureInfo.CurrentUICulture = ui; }
    }
    // R-CITY-003: the schema does not select the production cycle length/order.
    [Fact] public void AcceptsAnotherExactSyntheticCycleWithoutSortingIt()
    {
        var yaml = Yaml();
        var rows = yaml.Substring(yaml.IndexOf("  -", StringComparison.Ordinal)).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var header = yaml.Substring(0, yaml.IndexOf("  -", StringComparison.Ordinal));
        var b = new StringBuilder(header);
        // Reverse two copies of the fixture role sequence, then supply explicit contiguous ordinals.
        for (int i = 0; i < 40; i++) {
            var old = 19 - i % 20;
            b.Append(rows[old].Replace("ordinal: " + old.ToString(CultureInfo.InvariantCulture) + ",",
                "ordinal: " + i.ToString(CultureInfo.InvariantCulture) + ",")).Append('\n');
        }
        var m = Parse(b.ToString());
        Assert.Equal(40, m.Slots.Count); Assert.Equal("refugee", m.Slots[0].Role);
        Assert.Equal("trained_soldier", m.Slots[19].Role);
        Assert.Equal("trained_soldier", m.Slots[39].Role);
        Assert.Throws<NotSupportedException>(() => ((IList<Gdd03ImmigrationSlot>)m.Slots).Clear());
    }

    // R-CITY-003: explicit declarations cannot be omitted even when the inferred role looks obvious.
    [Theory]
    [InlineData("ruleset: gdd-0.3\n")]
    [InlineData("id: synthetic_manifest\n")]
    [InlineData("fixture_port: synthetic_port\n")]
    [InlineData("interval_turns: 2\n")]
    [InlineData("average_units_per_ship: 3\n")]
    [InlineData("role: trained_soldier, ")]
    [InlineData("profession: craftsman, ")]
    [InlineData("tier: apprentice, ")]
    [InlineData("qualification: trained, ")]
    public void RejectsOmittedDeclarations(string field) =>
        Assert.Throws<ContentException>(() => Parse(Yaml().Replace(field, "")));

    // R-CITY-003: malformed catalogue totals/metadata and undeclared geography cannot become policy.
    [Fact] public void RejectsInvalidCatalogueAndGeographyDeclarations()
    {
        foreach (var mode in new[] { "identity", "sum", "basis", "zero", "overflow", "expression", "parts", "note" }) {
            var c = Catalogue(); var row = c.Values["immigration.mix"];
            if (mode == "identity") c.Ruleset = SaveGame.GddRuleset;
            if (mode == "sum") row.Values[0] = 74m; // synthetic invalid percentage
            if (mode == "basis") row.Basis = "Agreed";
            if (mode == "zero") c.Values["immigration.ship_interval"].Values[0] = 0m;
            if (mode == "overflow") c.Values["immigration.ship_interval"].Values[0] = decimal.MaxValue;
            if (mode == "expression") row.Expression = "Deferred";
            if (mode == "parts") row.Parts["synthetic"] = new Gdd03DesignValue();
            if (mode == "note") row.Note = "";
            Assert.Throws<ContentException>(() => Parse(Yaml(), c));
        }
        Assert.Throws<ContentException>(() => Parse(Yaml() + "region: null\n"));
        Assert.Throws<ContentException>(() => Gdd03ImmigrationFixture.Parse(Yaml(), Catalogue(), Array.Empty<string>()));
    }


    // R-CITY-003: otherwise-valid fixture headers isolate the null/empty cycle boundary.
    [Theory] [InlineData("[]")] [InlineData("null")]
    public void RejectsEmptyOrNullCycleWithValidHeader(string slots)
    {
        var yaml = Yaml();
        var header = yaml.Substring(0, yaml.IndexOf("slots:", StringComparison.Ordinal));
        var error = Assert.Throws<ContentException>(() => Parse(header + "slots: " + slots + "\n"));
        Assert.Equal(Gdd03ImmigrationFixture.DiagnosticFile, error.File);
        Assert.Equal("slots", error.Field);
    }

    // R-CITY-003: malformed YAML must take the invariant line-number diagnostic path in both locales.
    [Theory] [InlineData("he-IL")] [InlineData("en-US")]
    public void MalformedYamlDiagnosticsUseInvariantLineNumbers(string name)
    {
        var old = CultureInfo.CurrentCulture; var oldUi = CultureInfo.CurrentUICulture;
        try {
            var yaml = Yaml().Replace("interval_turns: 2", "interval_turns: not_an_integer");
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            var expected = Assert.Throws<ContentException>(() => Parse(yaml));
            Assert.StartsWith("line ", expected.Field, StringComparison.Ordinal);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
            var actual = Assert.Throws<ContentException>(() => Parse(yaml));
            Assert.Equal(expected.File, actual.File); Assert.Equal(expected.Field, actual.Field);
            Assert.Equal(expected.Message, actual.Message);
        } finally { CultureInfo.CurrentCulture = old; CultureInfo.CurrentUICulture = oldUi; }
    }
}
