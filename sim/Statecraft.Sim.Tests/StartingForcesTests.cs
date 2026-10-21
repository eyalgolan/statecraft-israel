using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>The sourced starting forces, both sides (GDD Phase 5 PR 4; #743, owner, 20 October 2026: "all five defaults").
/// A Historical game starts with one unit per row of map/units_1948.yaml that is not hidden, is an army row, has a 15 May
/// strength and has an anchor window covering the start; the unit stands on the anchor's hex, read from
/// map/unit_anchor_hexes.yaml (written by tools/unit_anchor_hexes.py, so the sim needs no projection maths).</summary>
public class StartingForcesTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The fixture's military types: Israel's brigade type and one Egyptian type (design values for the test).</summary>
    const string Types = Fixture.GddUnitTypes
        + "  - { id: veteran_soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
        + "  - { id: egyptian_army, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n";

    /// <summary>One units_1948.yaml row in the fixture's map (an army row with one anchor from 15 May 1948) and its anchor's
    /// unit_anchor_hexes.yaml row on hex (q, r). The anchor's coordinates are those of hex (1, 0); the sim never reads them.</summary>
    static (string row, string hex) Row(string id, string side, int? people, int q = 1, int r = 0, string extra = "", string from = "1948-05-15",
        string service = "army", string status = "draft") =>
        (FormattableString.Invariant($"- id: {id}\n  service: {service}\n  side: {side}\n  status: {status}\n{extra}")
            + (people is int p ? FormattableString.Invariant($"  strength: {{ value: {p}, as_of: '1948-05-15' }}\n") : "")
            + FormattableString.Invariant($"  anchors:\n  - {{ place: Here, lon: 34.29, lat: 33.35, from: '{from}', to: '1949-01-01' }}\n"),
         FormattableString.Invariant($"  - {{ id: {id}, anchor: 0, q: {q}, r: {r} }}\n"));

    static Content Load(params (string row, string hex)[] rows) => Load("anchors:\n" + string.Concat(rows.Select(x => x.hex)), rows);

    static Content Load(string hexes, params (string row, string hex)[] rows) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["unit_types.yaml"] = Types,
        ["map/units_1948.yaml"] = "units:\n" + string.Concat(rows.Select(x => x.row)),
        ["map/unit_anchor_hexes.yaml"] = hexes,
    })));

    // R-UNIT-006: every starting unit is one row's, at that row's anchor covering 15 May 1948, with the owner's type and
    // its strength in 1,000-person units (halves up). The unit with id i is the content's i-th starting force.
    [Fact]
    public void EveryStartingUnitNamesItsRowAndAnchor()
    {
        Assert.Equal(new[]
        {
            ("golani", "Yavne'el", "veteran_soldier", "israel", 5, 10, 9),
            ("carmeli", "Haifa", "veteran_soldier", "israel", 2, 5, 8),
            ("alexandroni", "Netanya (Camp Dora)", "veteran_soldier", "israel", 5, 0, 15),
            ("kiryati", "Tel Aviv", "veteran_soldier", "israel", 1, -3, 19),
            ("givati", "Gedera", "veteran_soldier", "israel", 5, -5, 23),
            ("harel", "Sha'ar HaGai", "veteran_soldier", "israel", 1, -2, 23),
            ("negev", "Nir Am (Mekorot camp)", "veteran_soldier", "israel", 2, -9, 27),
            ("egyptian_expeditionary_force", "Gaza", "egyptian_army", "egypt", 10, -11, 27),
            ("arab_legion", "Ramallah", "arab_legion", "jordan", 5, 0, 22),
            ("syrian_army", "South of Lake Tiberias, before Samakh", "syrian_army", "syria", 3, 10, 10),
            ("iraqi_expeditionary_force", "The high ground over Gesher", "iraqi_army", "iraq", 3, 9, 11),
            ("arab_liberation_army", "al-Malikiyya", "arab_liberation_army", "ala", 4, 12, 4),
        }, Real.StartingForces.Select(f => (f.Row, f.Anchor, f.Type, f.Side, f.Strength, f.Q, f.R)));
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        Assert.Equal(Real.StartingForces.Count, s.Units.Count);
        for (int i = 0; i < Real.StartingForces.Count; i++)
        {
            var (f, u) = (Real.StartingForces[i], s.Units[i]);
            Assert.Equal((f.Type, f.Side, f.Strength, f.Q, f.R, 0, "", ""), (u.Type, u.Side, u.Strength, u.Q, u.R, u.Service, u.Order, u.OrderTarget));
        }
        Assert.All(Real.StartingForces, f => Assert.NotNull(Real.HexAt(f.Q, f.R)));
    }

    // R-UNIT-006: a hidden row, a row with no sourced strength and a row with no window on 15 May start nothing.
    [Fact]
    public void HiddenRowsStartNothing()
    {
        var rows = Real.StartingForces.Select(f => f.Row).ToList();
        foreach (var id in new[] { "oded", "brigade_8", "lebanese_army" }) Assert.DoesNotContain(id, rows);   // hidden
        Assert.DoesNotContain("etzioni", rows);                                                              // no strength
        foreach (var id in new[] { "yiftach", "brigade_7" }) Assert.DoesNotContain(id, rows);   // no 15 May window
        // The real hidden rows have no anchor, so the fixture holds the case that would otherwise start a unit.
        var c = Load(Row("seen", "israel", 20), Row("unseen", "israel", 20, extra: "  hidden: \"no sourced place\"\n"),
                     Row("unsized", "israel", null), Row("nobody", "israel", 0), Row("later", "israel", 20, from: "1948-06-01"));
        Assert.Equal(new[] { "seen" }, c.StartingForces.Select(f => f.Row));
    }

    // R-UNIT-006, Q-UNIT-5 (owner, 15 October 2026): the squadron and the two ships stay presentation-only tokens.
    [Fact]
    public void AirAndNavyFollowTheAnswer()
    {
        var rows = Real.StartingForces.Select(f => f.Row).ToList();
        foreach (var id in new[] { "squadron_101", "ins_haganah", "ins_wedgwood" }) Assert.DoesNotContain(id, rows);
        var c = Load(Row("ships", "israel", 20, service: "navy"));
        Assert.Empty(c.StartingForces);
    }

    // R-UNIT-006 (#743, owner, 20 October 2026): people to population units, the nearest unit, a half up, never below 1.
    // The fixture's people_per_unit is 10.
    [Fact]
    public void StrengthsRoundToTheNearestUnitHalvesUpNeverBelowOne()
    {
        var c = Load(Row("a", "israel", 45), Row("b", "egypt", 14, q: 2), Row("c", "israel", 4, q: 3));
        Assert.Equal(new[] { ("a", 5), ("b", 1), ("c", 1) }, c.StartingForces.Select(f => (f.Row, f.Strength)));
        Assert.Equal(new[] { ("a", "veteran_soldier"), ("b", "egyptian_army"), ("c", "veteran_soldier") }, c.StartingForces.Select(f => (f.Row, f.Type)));
        Assert.Equal(new[] { (1, 0), (2, 0), (3, 0) }, c.StartingForces.Select(f => (f.Q, f.R)));
    }

    // R-UNIT-006: a starting row the sim cannot place is a load error naming the file and the row, never a silent skip.
    [Theory]
    [InlineData("syria", 1, 0, "units[x].side", "no military type of side 'syria'")]
    [InlineData("israel", 0, 0, "units[x].anchors[0]", "hex q=0 r=0 is not a land hex")]
    [InlineData("israel", 40, 0, "units[x].anchors[0]", "hex q=40 r=0 is not on the map")]
    public void AStartingRowNeedsALandHexAndAType(string side, int q, int r, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(Row("x", side, 20, q, r)));
        Assert.Equal("map/units_1948.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // R-UNIT-006, Q-START-1 (owner, 15 October 2026): the sim reads units_1948.yaml, so the file is in the content hash.
    [Fact]
    public void TheUnitFileEntersTheContentHash()
    {
        var a = Load(Row("a", "israel", 20));
        var row = Row("a", "israel", 20);
        var b = Load((row.row + "  # a comment changes the file\n", row.hex));
        Assert.NotEqual(a.Hash, b.Hash);
        var h = Load("anchors:\n" + row.hex + "# a comment changes the hex file\n", row);
        Assert.NotEqual(a.Hash, h.Hash);
    }

    // docs/determinism.md: the sim does no projection maths. A starting unit stands on the hex unit_anchor_hexes.yaml gives
    // its anchor (tools/unit_anchor_hexes.py), not on one the sim works out from lon/lat: the fixture's anchor lies in hex
    // (1, 0), the file says (2, 1).
    [Fact]
    public void TheHexIsReadFromTheGeneratedFileNotProjected()
    {
        var c = Load(Row("a", "israel", 20, q: 2, r: 1));
        Assert.Equal(new[] { (2, 1) }, c.StartingForces.Select(f => (f.Q, f.R)));
    }

    // The real file is the tool's output (tools/tests/test_unit_anchor_hexes.py); these are its hexes for the 12 starting
    // anchors, the ones the sim's own projection gave before #1186's review moved it out of the sim (goldens unchanged).
    [Fact]
    public void TheRealStartingHexesArePinned() =>
        Assert.Equal(new[]
        {
            ("golani", 10, 9), ("carmeli", 5, 8), ("alexandroni", 0, 15), ("kiryati", -3, 19), ("givati", -5, 23), ("harel", -2, 23),
            ("negev", -9, 27), ("egyptian_expeditionary_force", -11, 27), ("arab_legion", 0, 22), ("syrian_army", 10, 10),
            ("iraqi_expeditionary_force", 9, 11), ("arab_liberation_army", 12, 4),
        }, Real.StartingForces.Select(f => (f.Row, f.Q, f.R)));

    // A starting anchor with no hex row, a row for an anchor that does not exist, a repeated row and a q that is not a whole
    // number are load errors naming
    // unit_anchor_hexes.yaml: the file is stale and the tool must be rerun.
    [Theory]
    [InlineData("anchors: []\n", "anchors", "no hex for units[x].anchors[0]")]
    [InlineData("anchors:\n  - { id: x, anchor: 0, q: 1, r: 0 }\n  - { id: y, anchor: 0, q: 1, r: 0 }\n", "anchors[y]", "no anchor 0 of a row 'y'")]
    [InlineData("anchors:\n  - { id: x, anchor: 0, q: 1, r: 0 }\n  - { id: x, anchor: 1, q: 1, r: 0 }\n", "anchors[x]", "no anchor 1 of a row 'x'")]
    [InlineData("anchors:\n  - { id: x, anchor: 0, q: 1, r: 0 }\n  - { id: x, anchor: 0, q: 2, r: 0 }\n", "anchors[x]", "anchor 0 is listed twice")]
    [InlineData("anchors:\n  - { id: x, anchor: 0, q: 1.5, r: 0 }\n", "anchors[x].q", "required, a whole number")]
    public void TheHexFileMustMatchTheUnitFile(string hexes, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(hexes, Row("x", "israel", 20)));
        Assert.Equal("map/unit_anchor_hexes.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // R-UNIT-007, Q-START-2 (owner, 15 October 2026): Open mode starts no forces; it cannot start a game yet at all.
    [Fact]
    public void OpenModeStartsNoForces() =>
        Assert.Throws<NotSupportedException>(() => Simulation.NewGame(Real, GoldenRun.Seed, Mode.Open));

    // R-UNIT-006, Q-START-3 (#743, owner, 20 October 2026): a shareable build waits until every row the sim starts a unit
    // from is fact-checked.
    [Fact]
    public void TheReleaseGateWaitsForEveryStartingRowToBeFactChecked()
    {
        Assert.Equal(Real.StartingForces.Select(f => f.Row).OrderBy(x => x, StringComparer.Ordinal), ReleaseGate.StartingForcesUnchecked(Real));
        var c = Load(Row("b", "israel", 20), Row("a", "israel", 20, q: 2, status: "fact-checked"));
        Assert.Equal(new[] { "b" }, ReleaseGate.StartingForcesUnchecked(c));
    }
}
