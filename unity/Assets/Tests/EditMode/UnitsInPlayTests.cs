using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>The units in play (docs/units.md, "Units in play"): the sourced map-only data, the roster read from the
/// sim's state, the placement clear of the towns and routes, the layer, the badge, the selection and its read-only card.</summary>
public class UnitsInPlayTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static string CommittedYaml => File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName));
    static List<UnitEntry> Units => UnitData.Parse(CommittedYaml);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static readonly DateTime LastArmistice = new DateTime(1949, 7, 20);
    /// <summary>The starting rows the owner person-checked on 21 October 2026 (#1240), and Etzioni and the 7th after them
    /// (#1241, https://github.com/eyalgolan/statecraft-israel/issues/793#issuecomment-6338280840); a row added later is not covered.</summary>
    static readonly HashSet<string> PersonChecked = new HashSet<string> { "alexandroni", "arab_legion", "arab_liberation_army", "brigade_7",
        "carmeli", "egyptian_expeditionary_force", "etzioni", "givati", "golani", "harel", "iraqi_expeditionary_force", "kiryati", "negev",
        "syrian_army" };

    static IEnumerable<string> Ids(IEnumerable<UnitToken> tokens) => tokens.Select(t => t.Unit.Id);

    static string Sha256(string text)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return string.Concat(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text)).Select(b => b.ToString("x2")));
    }

    /// <summary>A copy of the new-game state at <paramref name="date"/> with the given security level and call-up (a
    /// fixture for the presentation; the sim never sees it).</summary>
    static GameState StateAt(DateTime date, int rung, double mobilization)
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();
        s.Date = date; s.Rung = rung; s.Mobilization = mobilization;
        return s;
    }

    // ---- The data -----------------------------------------------------------------------------------------------

    /// <summary>The sim reads the unit file for the starting forces (R-UNIT-006; Q-START-1), so it is in the content hash and
    /// the goldens follow it; the WebGL manifest ships it (the player reads it).</summary>
    [Test]
    public void TheUnitFileEntersTheContentHash()
    {
        var copy = Path.Combine(Path.GetTempPath(), "statecraft-units-" + Guid.NewGuid().ToString("N"));
        try
        {
            Statecraft.Game.Boot.ContentManifest.Copy(Paths.Content, copy);
            var file = Path.Combine(copy, "map", UnitData.DefaultFileName);
            Assert.IsTrue(File.Exists(file), "the manifest copies the unit file");
            File.AppendAllText(file, "# a comment changes the file\n");
            Assert.AreNotEqual(ContentLoader.Load(Paths.Content).Hash, ContentLoader.Load(copy).Hash);
            CollectionAssert.Contains(Statecraft.Game.Boot.ContentManifest.Collect(Paths.Content), "map/" + UnitData.DefaultFileName);
        }
        finally { if (Directory.Exists(copy)) Directory.Delete(copy, true); }
    }

    /// <summary>R-UNIT-006: the sim's starting hexes are the hexes the map computes for the same rows' anchors (HexPicker,
    /// the map's own lon/lat to hex). The token may still stand one hex away (UnitPlacer); the sim's hex is the anchor's.</summary>
    [Test]
    public void TheStartMatchesTheMap()
    {
        var hexes = MapBuilder.LoadHexes(Path.Combine(Paths.Content, "map", "hexes.json"));
        Assert.AreEqual(14, C.StartingForces.Count, "the thirteen sourced forces of 15 May 1948 (#743, #1241) and the 7th's arrival (R-UNIT-009)");
        foreach (var f in C.StartingForces)
        {
            var start = (f.Arrives ?? C.Rules.Start).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var row = Units.Single(u => u.Id == f.Row);
            Assert.IsNull(row.Hidden, f.Row);
            Assert.AreEqual(UnitData.Army, row.Service, f.Row);
            Assert.AreEqual(f.Side, row.Side, f.Row);
            var anchor = row.Anchors.First(a => string.CompareOrdinal(a.From, start) <= 0 && string.CompareOrdinal(start, a.To) < 0);
            Assert.AreEqual(anchor.Place, f.Anchor, f.Row);
            Assert.AreEqual(HexPicker.FromLonLat(anchor.Lon, anchor.Lat, hexes.bounds, hexes.hex_size_km), (f.Q, f.R), f.Row);
        }
    }

    /// <summary>Review C1: every window ends for a reason. A source-based end cites its source and says which; any other
    /// end is the armistice of the anchor's front (the owner's "after the armistices, only what a source says continued").
    /// No window runs past its unit's sourced disbandment.</summary>
    [Test]
    public void EveryRowIsSourcedDatedAndOnTheMap()
    {
        var units = Units;
        var israel = units.Where(u => u.Side == UnitData.Israel).ToList();
        Assert.AreEqual(12, israel.Count(u => u.Service == UnitData.Army), "the twelve brigades of 1948");
        Assert.AreEqual(1, israel.Count(u => u.Service == UnitData.Air));
        Assert.AreEqual(2, israel.Count(u => u.Service == UnitData.Navy));
        CollectionAssert.AreEquivalent(new[] { "brigade_8", "oded" }, israel.Where(u => u.Hidden != null).Select(u => u.Id),
            "hidden: Oded (no sourced position) and the 8th (sourced at Tel Litvinsky, no clear spot on its own side); Carmeli (at Haifa, then from 9 July 1948 by Mishmar HaYarden; #588) and Alexandroni are placed (refs #385)");
        double lon0 = C.Hexes.Min(h => h.Lon), lon1 = C.Hexes.Max(h => h.Lon), lat0 = C.Hexes.Min(h => h.Lat), lat1 = C.Hexes.Max(h => h.Lat);
        // The Arab forces (#740): data only, each with its echelon and its 15 May strength (owner, 17 October 2026).
        var arab = units.Where(u => u.Side != UnitData.Israel).ToList();
        CollectionAssert.AreEquivalent(new[] { "egypt", "jordan", "syria", "iraq", "lebanon", "ala" }, arab.Select(u => u.Side).Distinct());
        foreach (var u in arab)
        {
            CollectionAssert.Contains(UnitData.Echelons, u.Echelon, u.Id);
            Assert.IsNotNull(u.Strength, u.Id);
            Assert.Greater(u.Strength.Value, 0, u.Id);
            Assert.AreEqual(new DateTime(1948, 5, 15), UnitData.Day(u.Strength.AsOf), $"{u.Id}: the 15 May strength only");
            Assert.IsNotEmpty(u.Strength.Sources, u.Id);
        }
        // Owner, 17 October 2026, on #860: "Isdud is the forward base and Majdal the rear base" at the first truce.
        var truce = new DateTime(1948, 6, 11);
        CollectionAssert.AreEquivalent(new[] { "Isdud", "Majdal" }, units.Single(u => u.Id == "egyptian_expeditionary_force").Anchors
            .Where(a => UnitData.Day(a.From) <= truce && truce < UnitData.Day(a.To)).Select(a => a.Place.Split(',')[0]));
        foreach (var u in units)
        {
            // Owner, 20 October 2026, #743: an IDF brigade's secondary-source strength "carries `status: to_verify`"; the
            // starting rows the owner person-checked (#1240, #1241) are fact-checked.
            Assert.AreEqual(PersonChecked.Contains(u.Id) ? "fact-checked" : u.Side == UnitData.Israel && u.Strength != null ? "to_verify" : "draft", u.Status, u.Id);
            Assert.AreEqual(UnitData.IsWikipediaOnly(u), u.WikipediaOnly, $"{u.Id}: wikipedia_only must say whether every source is Wikipedia or Wikidata");
            foreach (var s in u.AllSources())
                Assert.LessOrEqual(UnitData.Day(s.Checked), DateTime.Today, $"{u.Id}: {s.Url} checked in the future");
            foreach (var a in u.Anchors)
            {
                var at = $"{u.Id} {a.Place}";
                Assert.That(a.Lon, Is.InRange(lon0, lon1), at);
                Assert.That(a.Lat, Is.InRange(lat0, lat1), at);
                Assert.GreaterOrEqual(UnitData.Day(a.From), C.Rules.Start, at);
                Assert.IsNotEmpty(a.Sources, at);
                CollectionAssert.Contains(new[] { UnitData.EndBySource, UnitData.EndByFrontArmistice }, a.EndBasis, $"{at}: every window has an end_basis");
                if (a.EndBasis == UnitData.EndBySource)
                    Assert.IsFalse(string.IsNullOrWhiteSpace(a.EndNote), $"{at}: a source-based end says which source");
                else
                    Assert.AreEqual(UnitData.FrontArmistice[a.Front], UnitData.Day(a.To), $"{at}: ends on its front's armistice");
                Assert.LessOrEqual(UnitData.Day(a.To), LastArmistice, at);
                if (u.Disbanded != null) Assert.LessOrEqual(UnitData.Day(a.To), UnitData.Day(u.Disbanded.Date), $"{at}: after its disbandment");
                Assert.AreNotEqual("15 May 1948", a.Since, $"{at}: the game's first day is not a sourced arrival");
            }
            // A drawn row stands at one place at a time (one token per row). A row the map does not draw may stand at two
            // at once: the Egyptian force's forward and rear bases at the first truce (owner, 17 October 2026, on #860).
            if (UnitRoster.OnMap(u))
                for (int i = 1; i < u.Anchors.Count; i++)
                    Assert.LessOrEqual(UnitData.Day(u.Anchors[i - 1].To), UnitData.Day(u.Anchors[i].From), $"{u.Id}: anchor windows in order, not overlapping");
        }
        Assert.AreEqual(new DateTime(1949, 2, 24), UnitData.FrontArmistice["egypt"]);
        Assert.AreEqual(new DateTime(1949, 3, 23), UnitData.FrontArmistice["lebanon"]);
        Assert.AreEqual(new DateTime(1949, 4, 3), UnitData.FrontArmistice["jordan"]);
        Assert.AreEqual(LastArmistice, UnitData.FrontArmistice["syria"]);
    }

    /// <summary>#902: every Hebrew unit name has a dated source and stays agent-checked pending the native-speaker
    /// review in #564.</summary>
    [Test]
    public void HebrewUnitNamesAreSourcedAndAgentChecked()
    {
        var units = Units;
        CollectionAssert.IsEmpty(units.Where(u => string.IsNullOrEmpty(u.NameHe)).Select(u => u.Id));
        foreach (var u in units)
        {
            StringAssert.IsMatch("[א-ת]", u.NameHe, u.Id);
            Assert.AreEqual("agent-checked", u.NameHeReview, u.Id);
            Assert.IsNotEmpty(u.NameHeSources, u.Id);
            Assert.That(u.NameHeSources.All(s => s.Url.StartsWith("https://", StringComparison.Ordinal) && !string.IsNullOrEmpty(s.Checked)), u.Id);
        }
    }

    const string Anchor = "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', end_basis: source, end_note: n, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}";
    const string Unit = "units:\n- {id: a, name: A, number: '1', service: army, side: israel, sector: s, status: draft, wikipedia_only: true, anchors: [";

    [TestCase("units: []", "no units")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: cavalry, side: israel, sector: s, status: draft, wikipedia_only: true, anchors: []}", "unknown service 'cavalry'")]
    [TestCase("units:\n- {id: a, name: A, name_he: אב, number: '1', service: army, side: israel, sector: s, status: draft, wikipedia_only: true, anchors: []}", "name_he_sources is empty")]
    [TestCase("units:\n- {id: a, name: A, name_he_sources: [{url: 'https://x.org', checked: '2026-10-17'}], number: '1', service: army, side: israel, sector: s, status: draft, wikipedia_only: true, anchors: []}", "name_he_sources is set without name_he")]
    [TestCase("units:\n- {id: a, name: A, name_he: אב, name_he_sources: [{url: 'https://x.org', checked: '2026-10-17'}], name_he_review: reviewed, number: '1', service: army, side: israel, sector: s, status: draft, wikipedia_only: true, anchors: []}", "name_he_review must be agent-checked")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, sector: s, status: draft, wikipedia_only: true, anchors: []}", "unknown side ''")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, side: atlantis, sector: s, status: draft, wikipedia_only: true, anchors: []}", "unknown side 'atlantis'")]
    [TestCase(Unit + "]}", "'a' has no anchors")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', end_basis: source, end_note: n, sources: []}]}", "has no source")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-06-01', to: '1948-05-15', end_basis: source, end_note: n, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "to must be after from")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-5-15', to: '1948-06-01', end_basis: source, end_note: n, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "from/to")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', end_basis: source, end_note: n, sources: [{url: 'https://x.org'}]}]}", "checked")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "needs end_basis")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: ~, end_basis: source, end_note: n, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "to is empty")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', end_basis: source, end_note: n, toward: {place: T, lon: 34.8, lat: 32.0, note: n, sources: []}, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "toward needs a note and a source")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', end_basis: source, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "no end_note")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1949-07-20', end_basis: front_armistice, front: egypt, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "must end on the egypt armistice, 1949-02-24")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1949-02-24', end_basis: front_armistice, front: iraq, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "needs a front")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, side: israel, sector: s, status: draft, wikipedia_only: true, disbanded: {date: '1948-05-20', sources: [{url: 'https://x.org', checked: '2026-10-11'}]}, anchors: [" + Anchor + "]}", "runs past the unit's disbandment")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, side: israel, sector: s, status: draft, wikipedia_only: true, headgear: [wool_cap, wool_cap, wool_cap], anchors: [" + Anchor + "]}", "headgear has no source")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: air, side: israel, sector: s, status: draft, wikipedia_only: true, anchors: [" + Anchor + "]}", "without aircraft")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, side: israel, colour: red, sector: s, status: draft, wikipedia_only: true, anchors: []}", "colour")]
    // The Arab forces' rows (#570, #740): an echelon and one strength, never a range (CONTENT_POLICY section 1).
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, side: egypt, sector: s, status: draft, wikipedia_only: true, anchors: [" + Anchor + "]}", "needs an echelon and a strength")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, side: egypt, echelon: corps, strength: {value: 10, as_of: '1948-05-15', sources: [{url: 'https://x.org', checked: '2026-10-11'}]}, sector: s, status: draft, wikipedia_only: true, anchors: [" + Anchor + "]}", "unknown echelon 'corps'")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, side: egypt, echelon: force, strength: {value: '6,000 to 12,000', as_of: '1948-05-15', sources: [{url: 'https://x.org', checked: '2026-10-11'}]}, sector: s, status: draft, wikipedia_only: true, anchors: [" + Anchor + "]}", "u.yaml")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, side: egypt, echelon: force, strength: {value: 10, as_of: '1948-05-15', sources: []}, sector: s, status: draft, wikipedia_only: true, anchors: [" + Anchor + "]}", "strength has no source")]
    // A person's sign-off (#375): both or neither, a GitHub handle (never an email), a day.
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, side: israel, sector: s, status: draft, reviewed_by: eyalgolan, wikipedia_only: true, anchors: [" + Anchor + "]}", "reviewed_on")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, side: israel, sector: s, status: draft, reviewed_by: a@example.org, reviewed_on: '2026-10-15', wikipedia_only: true, anchors: [" + Anchor + "]}", "reviewed_by")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, side: israel, sector: s, status: draft, reviewed_by: eyalgolan, reviewed_on: soon, wikipedia_only: true, anchors: [" + Anchor + "]}", "reviewed_on")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, side: israel, sector: s, status: draft, reviewed_by: '', reviewed_on: '2026-10-15', wikipedia_only: true, anchors: [" + Anchor + "]}", "reviewed_by")]
    public void BadUnitFilesNameTheFileAndTheProblem(string yaml, string expected)
    {
        var e = Assert.Throws<InvalidDataException>(() => UnitData.Parse(yaml, "u.yaml"));
        StringAssert.StartsWith("u.yaml:", e.Message);
        StringAssert.Contains(expected, e.Message);
    }

    [Test]
    public void AUnitSignOffParses()
    {
        var u = UnitData.Parse("units:\n- {id: a, name: A, number: '1', service: army, side: israel, sector: s, status: draft, reviewed_by: eyalgolan, reviewed_on: '2026-10-15', wikipedia_only: true, anchors: [" + Anchor + "]}", "u.yaml");
        Assert.AreEqual(("eyalgolan", "2026-10-15"), (u[0].ReviewedBy, u[0].ReviewedOn));
    }

    [Test]
    public void AMissingOrBrokenFileDrawsNoUnitsAndWarns()
    {
        var warnings = new List<string>();
        Assert.IsEmpty(UnitData.ReadOrNone("", warnings.Add));
        Assert.IsEmpty(UnitData.ReadOrNone("units: [", warnings.Add));
        Assert.AreEqual(2, warnings.Count);
    }

    // ---- The roster ----------------------------------------------------------------------------------------------

    [Test]
    public void ANewGameShowsTheSourcedUnitsOfMay1948()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var tokens = UnitRoster.Build(Units, C, s);
        CollectionAssert.AreEqual(new[] { "golani", "carmeli", "alexandroni", "kiryati", "givati", "etzioni", "harel", "negev", "ins_haganah", "ins_wedgwood" }, Ids(tokens),
            "on 15 May 1948: no aircraft yet (101 Squadron from 20 May), the 7th Brigade from 24 May, Carmeli at Haifa (its sector seat, #588), Yiftach only in Operation Dani");
        Assert.AreEqual(5, s.Rung);
        Assert.IsTrue(tokens.Where(t => t.Unit.Service == UnitData.Army).All(t => t.Figures == 3), "full war: call-up at least 0.8");
        Assert.IsTrue(tokens.Where(t => t.Unit.Service == UnitData.Navy).All(t => t.ArtType == UnitShowcase.CorvetteType && t.Anchor.Place == "Haifa harbour"));
        CollectionAssert.AreEqual(new[] { UnitData.WoolCap, UnitData.SteelHelmet, UnitData.WoolCap }, tokens.Single(t => t.Unit.Id == "negev").Headgear,
            "the Negev Brigade's headgear is sourced to its 1948 photographs");
        CollectionAssert.AreEqual(UnitData.DefaultHeadgear, tokens.Single(t => t.Unit.Id == "givati").Headgear, "no source: the uniform default");
    }

    /// <summary>Owner, 17 October 2026 (#740): "Data only. PR 1 adds the sourced Arab rows to units_1948.yaml and filters
    /// them off the map in UnitRoster until Phase 5 PR 17's figures land ... Nothing is drawn with IDF figures, and there is
    /// no placeholder art." Each deployed force has its own figure; Egypt awaits a map representation, Lebanon's hidden row and local forces without a row
    /// stay off the map. The forces are drawn only once their own figure has arrived, with three of it and nothing else: until then the
    /// 15 May roster is exactly Israel's tokens.</summary>
    [Test]
    public void OnlyTheForcesWithAFigureAreDrawnAndOnlyOnceItHasArrived()
    {
        var units = Units;
        var others = units.Where(u => u.Side != UnitData.Israel).ToList();
        Assert.IsNotEmpty(others.Where(u => u.Hidden == null && u.Anchors.Count > 0), "the file holds Arab rows with anchors (#740)");
        var drawn = new[] { "arab_legion", "arab_liberation_army", "syrian_army", "iraqi_expeditionary_force" };
        CollectionAssert.AreEquivalent(drawn, others.Where(UnitRoster.OnMap).Select(u => u.Id), "the forces with a Collection figure");
        var israelOnly = units.Where(u => u.Side == UnitData.Israel).ToList();
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        CollectionAssert.AreEqual(Ids(UnitRoster.Build(israelOnly, C, s)), Ids(UnitRoster.Build(units, C, s)), "before any figure arrives: Israel's tokens, exactly");
        CollectionAssert.AreEqual(Ids(UnitRoster.Build(israelOnly, C, s)), Ids(UnitRoster.Build(units, C, s, id => false)), "nor while none has");
        var seen = new HashSet<string>();
        for (var d = C.Rules.Start; d < LastArmistice; d = d.AddDays(1))
        {
            var none = UnitRoster.Build(units, C.Rules, d, 5, 0.8);
            Assert.IsTrue(none.All(t => t.Unit.Side == UnitData.Israel), $"{d:yyyy-MM-dd}: no figure has arrived: {string.Join(",", none.Where(t => t.Unit.Side != UnitData.Israel))}");
            foreach (var t in UnitRoster.Build(units, C.Rules, d, 5, 0.8, id => true).Where(t => t.Unit.Side != UnitData.Israel))
            {
                CollectionAssert.Contains(drawn, t.Unit.Id, $"{d:yyyy-MM-dd}");
                Assert.AreEqual(UnitFigures.SideFigure(t.Unit.Side), t.ArtType, $"{t}: its own figure");
                Assert.AreEqual(3, t.Figures, $"{t}: Q-UI-3 at full strength (owner, 17 October 2026, #226)");
                Assert.IsEmpty(t.Headgear, $"{t}: no IDF rifleman's headgear");
                Assert.IsEmpty(t.Vehicles, $"{t}: no vehicle");
                seen.Add(t.Unit.Id);
            }
            var legionOnly = UnitRoster.Build(units, C.Rules, d, 5, 0.8, id => id == UnitFigures.ArabLegion);
            Assert.IsFalse(legionOnly.Any(t => t.Unit.Id == "arab_liberation_army"), $"{d:yyyy-MM-dd}: the ALA waits for its own figure");
        }
        CollectionAssert.AreEquivalent(drawn, seen, "the four forces stand on the map on their sourced days");
        var hexes = C.Hexes;
        var spots = UnitPlacer.PlaceAll(units, hexes, 5000, (lon, lat) => (lon * 1e5, lat * 1e5), (sv, x, z, c, t, ax, az) => true, (x, z) => 0.0);
        var dataOnly = others.Where(u => !drawn.Contains(u.Id)).ToList();
        Assert.IsFalse(dataOnly.SelectMany(u => u.Anchors).Any(spots.ContainsKey), "no spot for a row the map does not draw");
        Assert.IsEmpty(UnitPlacer.SkipMessages(dataOnly, spots), "and no warning for it either");
        Assert.IsTrue(others.Where(u => drawn.Contains(u.Id)).SelectMany(u => u.Anchors).All(spots.ContainsKey), "a spot for each drawn force's anchor");
    }

    [TestCase(0.80, 3)]
    [TestCase(0.40, 3)]
    [TestCase(0.39, 2)]
    [TestCase(0.15, 2)]
    [TestCase(0.10, 1)]
    [TestCase(0.02, 1)]
    public void TheFiguresFollowTheCallUp(double mobilization, int figures)
    {
        var tokens = UnitRoster.Build(Units, C.Rules, new DateTime(1948, 6, 5), 5, mobilization);
        Assert.IsTrue(tokens.Where(t => t.Unit.Service == UnitData.Army).All(t => t.Figures == figures && t.Headgear.Count == figures));
        Assert.IsTrue(tokens.Where(t => t.Unit.Service != UnitData.Army).All(t => t.Figures == 0));
    }

    /// <summary>Review I3 and I4: the bases where two sources agree (no base from 30 May to mid-June, where they do not),
    /// and the Spitfire from its first sourced sortie, 15 October 1948.</summary>
    [Test]
    public void TheSquadronFliesInWarAndItsAircraftAndBaseFollowTheSources()
    {
        UnitToken Squadron(DateTime d, int rung) => UnitRoster.Build(Units, C.Rules, d, rung, 0.8).SingleOrDefault(t => t.Unit.Id == "squadron_101");
        var may = Squadron(new DateTime(1948, 5, 22), 5);
        Assert.AreEqual((UnitShowcase.S199Type, "Ekron (RAF Aqir)", true), (may.ArtType, may.Anchor.Place, may.Flying));
        Assert.IsNull(Squadron(new DateTime(1948, 6, 5), 5), "the sources disagree on its base from 30 May to mid-June");
        Assert.IsTrue(Squadron(new DateTime(1948, 7, 1), 4).Flying);
        Assert.IsFalse(Squadron(new DateTime(1948, 7, 1), 3).Flying, "below security level 4 it stands parked");
        Assert.AreEqual("Herzliya strip", Squadron(new DateTime(1948, 7, 1), 5).Anchor.Place);
        Assert.AreEqual(UnitShowcase.S199Type, Squadron(new DateTime(1948, 10, 14), 5).ArtType);
        var oct = Squadron(new DateTime(1948, 10, 16), 5);
        Assert.AreEqual((UnitShowcase.SpitfireType, "Herzliya strip"), (oct.ArtType, oct.Anchor.Place),
            "the Spitfire from 15 October 1948; Modi Alon landed at Herzliya on 16 October");
        Assert.AreEqual("Hatzor", Squadron(new DateTime(1948, 11, 21), 5).Anchor.Place, "Hatzor from 21 November 1948 (Eduard p. 54, he.wiki)");
        Assert.IsNull(Squadron(new DateTime(1949, 6, 1), 2), "it left Hatzor in June 1949");
    }

    [Test]
    public void AfterTheArmisticesOnlyWhatASourceSaysContinuedShows()
    {
        CollectionAssert.AreEqual(new[] { "kiryati", "etzioni", "squadron_101" }, Ids(UnitRoster.Build(Units, C.Rules, new DateTime(1949, 2, 24), 4, 0.4)),
            "the southern brigades and the ships end on the Egypt armistice");
        CollectionAssert.DoesNotContain(Ids(UnitRoster.Build(Units, C.Rules, new DateTime(1948, 9, 1), 4, 0.4)), "carmeli",
            "Carmeli's window ends with Operation Brosh and the second truce (18 July 1948): no source keeps it there later");
        CollectionAssert.DoesNotContain(Ids(UnitRoster.Build(Units, C.Rules, new DateTime(1948, 12, 15), 4, 0.4)), "givati",
            "Givati moved to the centre in November 1948 (the Haganah lexicon)");
        CollectionAssert.AreEqual(new[] { "squadron_101" }, Ids(UnitRoster.Build(Units, C.Rules, new DateTime(1949, 4, 3), 2, 0.05)),
            "the central and Jerusalem brigades end on the Jordan armistice");
        Assert.IsEmpty(UnitRoster.Build(Units, C.Rules, LastArmistice, 2, 0.05));
        Assert.IsEmpty(UnitRoster.Build(Units, C.Rules, new DateTime(1951, 1, 1), 1, 0.02));
    }

    [Test]
    public void TheRosterNeverChangesTheStateAndTheGoldenHolds()
    {
        var units = Units;
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var digests = new List<string>();
        for (int turn = 0; turn < 1000 && s.Date < GoldenRun.Until1950 && s.GameOver == null; turn++)
        {
            var before = s.Hash();
            var tokens = UnitRoster.Build(units, C, s);
            Assert.AreEqual(before, s.Hash(), $"turn {turn}: building the roster changed the state");
            digests.Add($"{s.Date:yyyy-MM-dd} {string.Join(",", tokens)}");
            s = Simulation.Step(C, s, GoldenRun.Orders(C, s)).State;   // the golden runs' orders (the reference player since the cutover)
        }
        Assert.AreEqual(GoldenRun.Hash1950, s.Hash(), "the golden does not move");
        // The presentation golden: the roster of every turn of the historical playthrough, pinned. A deliberate change to
        // the unit data or the roster rules updates it (and says why in the commit).
        var all = string.Join("\n", digests);
        Debug.Log("UNITS roster golden " + Sha256(all) + "\n" + all);
        Assert.AreEqual(RosterGolden1950, Sha256(all), "the roster of the historical playthrough changed (see the log for the turns)");
    }

    /// <summary>Review M4: the historical run's turns are monthly or quarterly in places, so a weekly sweep of the war (at
    /// full war and full call-up) pins every window too.</summary>
    [Test]
    public void AWeeklySweepOfTheWarIsPinned()
    {
        var units = Units;
        var lines = new List<string>();
        for (var d = C.Rules.Start; d < LastArmistice; d = d.AddDays(7))
            lines.Add($"{d:yyyy-MM-dd} {string.Join(",", UnitRoster.Build(units, C.Rules, d, 5, 0.8))}");
        var all = string.Join("\n", lines);
        Debug.Log("UNITS weekly golden " + Sha256(all) + "\n" + all);
        Assert.AreEqual(WeeklyGolden, Sha256(all));
    }

    // GDD Phase 2 PR 1 (the cutover): the golden run is 20 monthly turns with the reference player (was f07b1655).
    // Issue #665: each token's line now ends with its sourced vehicles ("+figure-vehicle-..."), the 65 mm gun and the jeep
    // (was 573f3099... and 323cbe04...; every line is otherwise the same, checked against main's logged rosters).
    // #588: Carmeli stands at Haifa from 15 May to 9 July 1948 (was 0612f518... and db15a031...; with its "carmeli@Haifa"
    // entries taken out, the logged rosters hash to those again).
    const string RosterGolden1950 = "e11d180e619f602009b652679e1967f0faa18c674bcdee440125cf15d2de1036";
    const string WeeklyGolden = "b459d9a4b8af7547b64a5f82f00dc877d707afabd1e065202ae361972e169ac0";

    // ---- Placement, the layer and the card (a built map) ---------------------------------------------------------

    static MapScene BuildMap()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        // The towns' footprints as the game sizes them (its kits read the 1945 populations), whatever an earlier test left.
        Statecraft.Game.Map.Kits.KitPopulation.Use(Statecraft.Game.Map.Kits.KitPopulation.Parse(
            File.ReadAllText(Path.Combine(Paths.Content, "map", Statecraft.Game.Map.Kits.KitPopulation.FileName))));
        map.Build();
        return map;
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    /// <summary>Review I2 and M9: every anchor gets one spot, from the data alone, within one hex step of its sourced
    /// place, clear of the towns' footprints and, for the figures, of the railways and roads by the full clearance; no two
    /// anchors whose windows overlap share a hex. Each spot and its distance is logged.</summary>
    [Test]
    public void EveryAnchorStandsWithinOneHexClearOfTheTownsAndTheRoutes()
    {
        var map = BuildMap();
        try
        {
            Assert.IsNotEmpty(map.SettlementFootprints);
            Assert.IsNotEmpty(map.RouteCorridors);
            var units = Units;
            var layer = UnitLayer.Create(map, units, C.Hexes, still: true);
            float hex = map.Hexes.hex_size_km * 1000f;
            var hexAt = C.Hexes.ToDictionary(h => (h.Q, h.R));
            var anchors = units.Where(UnitRoster.OnMap).SelectMany(u => u.Anchors.Select(a => (u, a))).ToList();
            var log = new List<string>();
            foreach (var (u, a) in anchors)
            {
                var at = $"{u.Id} {a.Place}";
                Assert.IsTrue(layer.Spots.TryGetValue(a, out var s), $"{at}: no spot");
                var w = map.Frame.ToWorld(a.Lon, a.Lat, 0);
                var c = new Vector2((float)s.X, (float)s.Z);
                float km = (c - new Vector2(w.x, w.z)).magnitude / 1000f;
                var (slon, slat) = map.Frame.ToLonLat(new Vector3((float)s.X, 0, (float)s.Z));
                log.Add(FormattableString.Invariant($"{at} -> {s} {km:0.0} km at {slon:0.000} E {slat:0.000} N"));
                Assert.LessOrEqual(km * 1000f, Mathf.Sqrt(3f) * hex + 1f, $"{at}: more than one hex step from its anchor");
                Assert.AreEqual(u.Service != UnitData.Navy, hexAt[(s.Q, s.R)].Land, $"{at}: land for the army and the air force, sea for ships");
                if (u.Service == UnitData.Navy) continue;
                if (s.InTown)
                {
                    Assert.IsTrue(s.Compact, at);
                    Assert.IsTrue(map.UnitClearings.Any(x => x.centre == c), $"{at}: a token in a town has the kits' clearing, so it stands in no building");
                }
                else
                {
                    float disc = UnitShowcase.RingRadius * hex * (s.Compact ? UnitGround.CompactScale : 1f);
                    foreach (var (fc, fr) in map.SettlementFootprints)
                        Assert.GreaterOrEqual((fc - c).magnitude, fr * UnitGround.FootprintMargin + disc, $"{at}: the ring reaches a town's buildings");
                }
                var points = u.Service == UnitData.Army ? layer.PlacementGround.FigurePoints(c, s.Compact) : new[] { c };
                foreach (var f in points)
                    foreach (var (line, half) in map.RouteCorridors)
                        for (int i = 0; i + 1 < line.Count; i++)
                            Assert.GreaterOrEqual(UnitGround.DistanceToSegment(f, line[i], line[i + 1]), half + UnitGround.FigureClearanceM, $"{at}: a figure stands on a railway or road");
            }
            for (int i = 0; i < anchors.Count; i++)
                for (int j = i + 1; j < anchors.Count; j++)
                {
                    var (a, b) = (anchors[i].a, anchors[j].a);
                    bool overlap = UnitData.Day(a.From) < UnitData.Day(b.To) && UnitData.Day(b.From) < UnitData.Day(a.To);
                    if (overlap) Assert.AreNotEqual((layer.Spots[a].Q, layer.Spots[a].R), (layer.Spots[b].Q, layer.Spots[b].R), $"{anchors[i].u.Id} and {anchors[j].u.Id} share a hex");
                }
            Debug.Log("UNITS spots\n" + string.Join("\n", log));
            // Review MF1: the anchors whose nearest clear spot was the other side's ground stand on their own side (Etzioni in
            // West Jerusalem; Harel by Saris on the corridor road), and the 7th Brigade is anchored at Hulda, not Latrun.
            (int, int) Spot(string id, string place) { var a = anchors.Single(x => x.u.Id == id && x.a.Place == place).a; return (layer.Spots[a].Q, layer.Spots[a].R); }
            float Km(string id, string place, double lon, double lat)
            {
                var sp = layer.Spots[anchors.Single(x => x.u.Id == id && x.a.Place.StartsWith(place)).a];
                var w = map.Frame.ToWorld(lon, lat, 0);
                return (new Vector2((float)sp.X, (float)sp.Z) - new Vector2(w.x, w.z)).magnitude / 1000f;
            }
            // The 7th Brigade at its Latrun headquarters, Hulda, far from Sar'a (Arab until 13-14 July).
            Assert.Less(Km("brigade_7", "Hulda", 34.88333, 31.83222), 2.6f, "the 7th stands by Hulda");
            Assert.Greater(Km("brigade_7", "Hulda", 34.985, 31.77528), 3f, "and well away from Sar'a");
            Assert.Greater(Km("harel", "Sha'ar HaGai", 34.985, 31.77528), 3f, "Harel well away from Sar'a too");
            Assert.AreEqual((-2, 23), Spot("harel", "Sha'ar HaGai"));
            foreach (var (u, a) in anchors.Where(x => x.a.PlaceHex != null))
                Assert.AreEqual((a.PlaceHex[0], a.PlaceHex[1]), (layer.Spots[a].Q, layer.Spots[a].R), $"{u.Id} {a.Place}: in its place_hex");
            var etzioni = layer.Spots[anchors.Single(x => x.u.Id == "etzioni").a];
            var wj = map.Frame.ToWorld(35.21944, 31.78194, 0);
            Assert.Less((new Vector2((float)etzioni.X, (float)etzioni.Z) - new Vector2(wj.x, wj.z)).magnitude, 2100f, "Etzioni stands in West Jerusalem");
            Assert.Less(etzioni.X, wj.x, "on the west side of the West Jerusalem point, away from the line");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>promo-villages review I5: with the kits on, the units keep off the 1948 villages and the Bedouin camps as they
    /// keep off the game towns. Every token outside a town has its ring clear of every kit footprint (MapScene.KitFootprints:
    /// a town's or a locality's with its tinted margin, a camp's); every token in a town has the kits' clearing, which village
    /// houses and tents keep off like a road.</summary>
    [Test]
    public void NoTokenStandsInAVillageOrACamp()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = UnityEngine.Object.FindFirstObjectByType<MapScene>();
        map.Build();
        Assert.Greater(map.Localities.Count, 200, "the kits and the 1948 localities are on");
        var footprints = map.KitFootprints;
        Assert.IsTrue(footprints.Any(f => f.id.StartsWith("camp:")), "the camps are built");
        Assert.AreEqual(map.Settlements.Count + map.Localities.Count, map.SettlementFootprints.Count,
            "the units' footprints are the game settlements' and the localities'");
        var units = Units;
        var layer = UnitLayer.Create(map, units, C.Hexes, still: true);
        int outside = 0, inTown = 0;
        foreach (var u in units.Where(u => UnitRoster.OnMap(u) && u.Service != UnitData.Navy))
            foreach (var a in u.Anchors)
            {
                var at = $"{u.Id} {a.Place}";
                Assert.IsTrue(layer.Spots.TryGetValue(a, out var s), $"{at}: no spot");
                var c = new Vector2((float)s.X, (float)s.Z);
                if (s.InTown)
                {
                    inTown++;
                    Assert.IsTrue(map.UnitClearings.Any(x => x.centre == c), $"{at}: a token in a town has the kits' clearing");
                    continue;
                }
                outside++;
                float ring = layer.PlacementGround.RingRadius(s.Compact);
                foreach (var (fc, fr, id) in footprints)
                {
                    float reach = fr * (id.StartsWith("camp:") ? 1f : UnitGround.FootprintMargin) + ring;
                    Assert.GreaterOrEqual((fc - c).magnitude, reach, $"{at}: its ring reaches {id}'s kit");
                }
            }
        TestContext.WriteLine($"{outside} tokens outside the towns, {inTown} in one; {footprints.Count} kit footprints, "
            + $"{footprints.Count(f => f.id.StartsWith("camp:"))} of them camps");
        Assert.Greater(outside, 0);
    }

    /// <summary>Review of #518: the tokens' keep-clear boxes (what the map's names keep clear of) are right even when the
    /// tokens are built while the "units" toggle hides the layer, as a ?shot run without &amp;units does before the first
    /// Show: once the layer is shown again, each token's pieces box is the hull of its drawn pieces (Renderer.bounds, now
    /// that they are active), not a box stretched to the world origin; and a hidden layer hands out no boxes.</summary>
    [Test]
    public void KeepClearBoxesBuiltWhileTheLayerIsHiddenHoldTheirToken()
    {
        var map = BuildMap();
        try
        {
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            VisualToggles.Set(UnitLayer.ToggleName, false);
            layer.Show(C, Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical));   // built inactive
            Assert.AreEqual(0, layer.KeepClear.Count, "a hidden layer hands out no boxes");
            VisualToggles.Set(UnitLayer.ToggleName, true);
            Assert.AreEqual(layer.Tokens.Count, layer.KeepClear.Count, "one entry per token");
            var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
            int pieces = 0;
            for (int i = 0; i < layer.Tokens.Count; i++)
            {
                var token = tokens.Find(layer.Tokens[i].Token.Unit.Id);
                var drawn = token.GetComponentsInChildren<Renderer>().Where(r => r.GetComponentInParent<UnitBadge>() == null && r.GetComponentInParent<ShowcaseFlight>() == null).ToList();
                var boxes = layer.KeepClear[i];
                if (drawn.Count == 0) { Assert.AreEqual(1, boxes.Length, $"{token.name}: only its badge"); continue; }
                Assert.AreEqual(2, boxes.Length, $"{token.name}: its badge and its pieces");
                var hull = drawn[0].bounds; foreach (var r in drawn) hull.Encapsulate(r.bounds);
                Assert.AreEqual(hull.min.x, boxes[1].min.x, 1f, token.name); Assert.AreEqual(hull.max.x, boxes[1].max.x, 1f, token.name);
                Assert.AreEqual(hull.min.y, boxes[1].min.y, 1f, token.name); Assert.AreEqual(hull.max.y, boxes[1].max.y, 1f, token.name);
                Assert.AreEqual(hull.min.z, boxes[1].min.z, 1f, token.name); Assert.AreEqual(hull.max.z, boxes[1].max.z, 1f, token.name);
                pieces++;
            }
            Assert.Greater(pieces, 5, "brigades and ships were checked");
        }
        finally { if (VisualToggles.Names.Contains(UnitLayer.ToggleName)) VisualToggles.Set(UnitLayer.ToggleName, true); UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The same on the map's real hills: every rifleman of every army anchor of the war stands on its token's draped
    /// base at its own point, and the ground under its boots rises at most a tenth of its height over its feet.</summary>
    [Test]
    public void NoRiflemanSinksIntoTheHillsOfTheMap()
    {
        var map = BuildMap();
        try
        {
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            var checkedFigures = 0; float worstFloat = 0f;
            foreach (var d in new[] { new DateTime(1948, 5, 29), new DateTime(1948, 6, 5), new DateTime(1948, 7, 12), new DateTime(1948, 12, 4) })
            {
                layer.Show(SimBridgeGddTests.Legacy, StateAt(d, 5, 0.8));   // the legacy loop: the war's tokens by date
                foreach (var f in map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Rifleman")))
                {
                    var p = f.position; float lowest = float.MaxValue;
                    for (int k = 0; k < 24; k++)   // round its boots (0.2 m from its centre), at 0.9 of their radius
                    {
                        float a = Mathf.PI / 12f * k + 0.1f;
                        var q = UnitShowcase.Ground(map, p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.2f * UnitShowcase.FigureScale * 0.9f);
                        Assert.LessOrEqual(q.y, p.y + 0.1f * UnitBadge.FigureHeightM * UnitShowcase.FigureScale, $"{f.parent.name}/{f.name} on {d:yyyy-MM-dd}: the ground under a boot rises more than the base hides");
                        lowest = Mathf.Min(lowest, q.y);
                    }
                    worstFloat = Mathf.Max(worstFloat, p.y - lowest);
                    var plinth = f.parent.Find(UnitPlinth.PlinthName);
                    Assert.IsNotNull(plinth, $"{f.parent.name}: the figures stand on a plinth");
                    Assert.AreEqual(UnitPlinth.FigureY(map, plinth.localScale.x, p.x, p.z), p.y, 0.5f, $"{f.parent.name}/{f.name}: on the base's top over the ground at its feet");
                    checkedFigures++;
                }
            }
            Debug.Log($"UNITS slope: {checkedFigures} riflemen, worst rise of a figure's feet over the ground at its boots {worstFloat:0} m");
            Assert.Greater(checkedFigures, 40);
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    static readonly DateTime[] OrchardDates = { new DateTime(1948, 5, 29), new DateTime(1948, 6, 5), new DateTime(1948, 7, 12), new DateTime(1948, 12, 4) };

    /// <summary>The game's map scene with its kits and trees on (BuildMap configures no kit materials).</summary>
    static MapScene BuildMapWithTrees()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = UnityEngine.Object.FindFirstObjectByType<MapScene>();
        Statecraft.Game.Map.Kits.KitPopulation.Use(Statecraft.Game.Map.Kits.KitPopulation.Parse(
            File.ReadAllText(Path.Combine(Paths.Content, "map", Statecraft.Game.Map.Kits.KitPopulation.FileName))));
        map.Build();
        return map;
    }

    /// <summary>The shown army tokens' plinths, as discs (the figures' boots are all on them).</summary>
    static List<(Vector2 centre, float radius)> FigureFeet(MapScene map) =>
        map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").GetComponentsInChildren<Transform>()
            .Where(t => t.name == UnitPlinth.PlinthName).Select(t => (new Vector2(t.position.x, t.position.z), t.localScale.x * UnitPlinth.RimOuter)).ToList();

    static bool UnderAFigure(Statecraft.Game.Map.Kits.TreeKind kind, Matrix4x4 m, List<(Vector2 centre, float radius)> plinths)
    {
        var p = m.GetColumn(3); var at = new Vector2(p.x, p.z);
        float crown = Statecraft.Game.Map.Kits.VegetationLayer.CrownRadius(kind, m);
        return plinths.Any(f => (f.centre - at).sqrMagnitude < (f.radius + crown) * (f.radius + crown));
    }

    /// <summary>Issue #249: on every date the units are checked on, no kit tree that is drawn has its crown within a shown
    /// figure's boots (the boot radius plus the tree's crown radius), and some trees were in the way, so the check bites.</summary>
    [Test]
    public void NoKitTreeGrowsThroughAShownFiguresFeet()
    {
        var map = BuildMapWithTrees();
        try
        {
            var veg = map.GetComponentInChildren<Statecraft.Game.Map.Kits.VegetationLayer>(true);
            Assert.IsNotNull(veg, "the kits and their trees are on");
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            int figures = 0, hidden = 0;
            foreach (var d in OrchardDates)
            {
                layer.Show(SimBridgeGddTests.Legacy, StateAt(d, 5, 0.8));   // the legacy loop: the war's tokens by date
                var feet = FigureFeet(map);
                Assert.Greater(feet.Count, 0, $"{d:yyyy-MM-dd}: figures are drawn");
                var through = veg.Drawable().Where(t => UnderAFigure(t.kind, t.m, feet)).ToList();
                Assert.IsEmpty(through, $"{d:yyyy-MM-dd}: {through.Count} drawn trees reach a figure's boots, the first at {(through.Count > 0 ? (Vector3)through[0].m.GetColumn(3) : Vector3.zero)}");
                figures += feet.Count; hidden += veg.Hidden;
                TestContext.WriteLine($"{d:yyyy-MM-dd}: {feet.Count} figures, {veg.Hidden} trees left out");
            }
            Assert.Greater(hidden, 0, "some figure stands where trees grow, so this test can fail");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Issue #249: only the trees under the figures drawn that day are left out (a token not shown that day leaves
    /// its trees alone, so Lydda's come back after Yiftach's window); the planting never changes; and with the "units"
    /// toggle off (a plain screenshot run) every tree is drawn, so plain checkpoints stay comparable (review I5).</summary>
    [Test]
    public void OnlyTheShownFiguresTreesGoAndTheToggleBringsThemBack()
    {
        var map = BuildMapWithTrees();
        try
        {
            var veg = map.GetComponentInChildren<Statecraft.Game.Map.Kits.VegetationLayer>(true);
            var planted = veg.Planted().Select(t => t.m).ToList();
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            foreach (var d in OrchardDates.Concat(new[] { new DateTime(1948, 7, 25) }))
            {
                layer.Show(SimBridgeGddTests.Legacy, StateAt(d, 5, 0.8));   // the legacy loop: the war's tokens by date
                var feet = FigureFeet(map);
                int expected = veg.Planted().Count(t => UnderAFigure(t.kind, t.m, feet));
                Assert.AreEqual(expected, veg.Hidden, $"{d:yyyy-MM-dd}: exactly the trees under the shown figures are left out");
                Assert.AreEqual(veg.Count - expected, veg.Drawable().Count(), $"{d:yyyy-MM-dd}: every other tree is drawn");
            }
            CollectionAssert.AreEqual(planted, veg.Planted().Select(t => t.m).ToList(), "the planting (the kits' layout) never changes");

            Assert.Greater(veg.Hidden, 0);
            VisualToggles.Set(UnitLayer.ToggleName, false);
            Assert.AreEqual(0, veg.Hidden, "the layer off gives every tree back");
            Assert.AreEqual(veg.Count, veg.Drawable().Count());
            VisualToggles.Set(UnitLayer.ToggleName, true);
            Assert.Greater(veg.Hidden, 0, "and on again leaves them out again");

            // A screenshot run that does not ask for the units: the toggle is off before the first draw.
            var shot = UnitLayer.Create(map, Units, C.Hexes, still: true);
            VisualToggles.Set(UnitLayer.ToggleName, false);
            shot.Show(C, StateAt(OrchardDates[0], 5, 0.8));
            Assert.AreEqual(0, veg.Hidden, "a plain checkpoint draws every tree");
        }
        finally { if (VisualToggles.Names.Contains(UnitLayer.ToggleName)) VisualToggles.Set(UnitLayer.ToggleName, true); UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void TheTrioIsSpreadSoEachFigureReadsOnItsOwn()
    {
        float step = 5000f * UnitShowcase.TrioSpread;   // a 5 km hex
        var o = UnitShowcase.FigureOffsets(3);
        for (int i = 0; i < 3; i++)
            for (int j = i + 1; j < 3; j++)
                Assert.GreaterOrEqual((o[i] - o[j]).magnitude * step, 1800f, "a figure is about 0.85 km wide at 1,400x: keep a gap");
        Assert.Less(o.Max(v => v.magnitude) * step + 600f, UnitShowcase.RingRadius * 5000f, "the figures stay inside the ring");
    }

    /// <summary>Issue #644: the trio and its flag were laid out for camera yaw 0, so at yaw 90 and 270 the figures stood in a
    /// column and from about yaw 180 the pole stood in front of them. At every yaw from 0 to 315 in 45 degree steps, on a 30 km
    /// look at each army token: its figures' feet, seen along the camera's right (flattened), stand side by side, each gap at
    /// least 0.9 of the token's gap at yaw 0; each figure faces the camera as it does at yaw 0; and the pole's foot is farther
    /// from the camera than every figure's feet, so the pole is drawn behind them.</summary>
    [Test]
    public void TheTrioReadsSideBySideAndThePoleStandsBehindItAtEveryYaw()
    {
        var map = BuildMap();
        try
        {
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            layer.Show(C, StateAt(new DateTime(1948, 5, 29), 5, 0.8));
            var cam = Camera.main; Assert.IsNotNull(cam, "the default scene's camera");
            var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").Cast<Transform>().Where(t => t.Find(UnitPlinth.PlinthName) != null).ToList();
            var atZero = new Dictionary<string, (float[] gaps, float[] facing)>();
            int judged = 0, poles = 0;
            foreach (var yaw in new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f })
                foreach (var t in tokens)
                {
                    cam.transform.rotation = Quaternion.Euler(CameraRig.Pitch, yaw, 0f);
                    cam.transform.position = t.Find(UnitPlinth.PlinthName).position - cam.transform.forward * 30000f;
                    layer.FaceCamera(cam);
                    var right = cam.transform.right; right.y = 0f; right.Normalize();
                    var away = cam.transform.forward; away.y = 0f; away.Normalize();
                    var figures = t.Cast<Transform>().Where(x => x.name.StartsWith("Rifleman")).OrderBy(x => x.name).ToList();
                    if (figures.Count < 2) continue;
                    var across = figures.Select(f => Vector3.Dot(f.position, right)).OrderBy(v => v).ToList();
                    var gaps = Enumerable.Range(1, across.Count - 1).Select(i => across[i] - across[i - 1]).ToArray();
                    var facing = figures.Select(f => Vector3.SignedAngle(-away, Vector3.ProjectOnPlane(f.forward, Vector3.up), Vector3.up)).ToArray();
                    if (yaw == 0f) atZero[t.name] = (gaps, facing);
                    var zero = atZero[t.name];
                    for (int i = 0; i < gaps.Length; i++)
                        Assert.GreaterOrEqual(gaps[i], 0.9f * zero.gaps[i], $"yaw {yaw} {t.name}: neighbours {i} and {i + 1} stand side by side across the view, not in a column");
                    for (int i = 0; i < facing.Length; i++)
                        Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(zero.facing[i], facing[i])), 1f, $"yaw {yaw} {t.name}/{figures[i].name}: faces the camera as at yaw 0");
                    judged++;
                    var flag = t.Find(UnitPlinth.FlagName); if (flag == null) continue;
                    float pole = Vector3.Dot(flag.position, away), farthest = figures.Max(f => Vector3.Dot(f.position, away));
                    Assert.Greater(pole, farthest, $"yaw {yaw} {t.name}: the pole's foot stands behind the figures, not in front ({pole - farthest:0} m)");
                    poles++;
                }
            Debug.Log($"UNITS yaw: {judged} trios and {poles} poles judged over 8 yaws");
            int trios = tokens.Count(t => t.Cast<Transform>().Count(x => x.name.StartsWith("Rifleman")) >= 2);
            Assert.GreaterOrEqual(trios, 5, "the army tokens of 29 May 1948 with two or more figures (not a vacuous pass)");
            Assert.AreEqual(8 * trios, judged, "every army token with two or more figures, at every yaw");
            Assert.AreEqual(judged, poles, "each with its flag");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Review of #644: the turn holds through what can stale it. A roster rebuilt while the camera already looks at
    /// yaw 90 stands its new figures turned for that yaw, as before the rebuild (the yaw cache is cleared with the tokens). A
    /// second camera (the scene view, a preview) rendering at another yaw turns no figure: only the game camera does. A pan
    /// and a zoom at the same yaw move no figure.</summary>
    [Test]
    public void TheTurnHoldsThroughARebuildASecondCameraAndAPan()
    {
        var map = BuildMap();
        var other = new GameObject("PreviewCamera").AddComponent<Camera>();
        try
        {
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            var day = new DateTime(1948, 5, 29);
            // The legacy loop, whose roster follows the dates and the call-up (the GDD loop draws the sim's units).
            layer.Show(SimBridgeGddTests.Legacy, StateAt(day, 5, 0.8));
            var cam = Camera.main; Assert.IsNotNull(cam, "the default scene's camera");
            var tokensRoot = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
            Dictionary<string, Vector3> Figures() => tokensRoot.Cast<Transform>().SelectMany(t => t.Cast<Transform>().Where(x => x.name.StartsWith("Rifleman"))
                .Select(x => (key: t.name + "/" + x.name, at: x.position))).ToDictionary(p => p.key, p => p.at);
            void AssertStill(Dictionary<string, Vector3> want, string what)
            {
                var now = Figures();
                Assert.AreEqual(want.Count, now.Count, $"{what}: the same figures");
                foreach (var kv in want) Assert.Less((now[kv.Key] - kv.Value).magnitude, 1f, $"{what}: {kv.Key} stands where it stood at yaw 90");
            }
            cam.transform.rotation = Quaternion.Euler(CameraRig.Pitch, 90f, 0f);
            cam.transform.position = tokensRoot.GetChild(0).position - cam.transform.forward * 30000f;
            layer.FaceCamera(cam);
            var at90 = Figures();
            Assert.Greater(at90.Count, 10, "the figures of 29 May 1948 (not a vacuous pass)");
            // A roster change and back, the camera still at yaw 90: Rebuild faces the new tokens to it.
            var fewer = StateAt(day, 5, 0.3);
            Assert.IsTrue(layer.Show(SimBridgeGddTests.Legacy, fewer), "the call-up changes the roster");
            Assert.IsTrue(layer.Show(SimBridgeGddTests.Legacy, StateAt(day, 5, 0.8)), "and back");
            AssertStill(at90, "rebuilt at yaw 90");
            // A second camera at yaw 0 renders: the badges and flags face it, the figures stay turned for the game camera.
            other.transform.SetPositionAndRotation(cam.transform.position, Quaternion.Euler(CameraRig.Pitch, 0f, 0f));
            layer.FaceCamera(other);
            AssertStill(at90, "after a second camera at yaw 0");
            layer.FaceCamera(cam);
            // A pan and a zoom at yaw 90.
            cam.transform.position += cam.transform.right * 8000f; layer.FaceCamera(cam);
            cam.transform.position += cam.transform.forward * 12000f; layer.FaceCamera(cam);
            AssertStill(at90, "after a pan and a zoom");
        }
        finally { UnityEngine.Object.DestroyImmediate(other.gameObject); UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void ScreenshotsShowUnitsOnlyWhenAsked()
    {
        Assert.IsTrue(UnitLayer.HiddenInCaptures("https://x/?golden&shot", new string[0], true), "plain checkpoints stay comparable");
        Assert.IsFalse(UnitLayer.HiddenInCaptures("https://x/?golden&shot&units", new string[0], true));
        Assert.IsFalse(UnitLayer.HiddenInCaptures("https://x/?vprofile", new string[0], true), "the frame profile prices them");
        Assert.IsFalse(UnitLayer.HiddenInCaptures("https://x/", new string[0], true), "normal play shows them");
        Assert.IsTrue(UnitLayer.HiddenInCaptures(null, new[] { "player", "-shot" }, false));
        Assert.IsFalse(UnitLayer.HiddenInCaptures(null, new[] { "player", "-shot", "-units" }, false));
    }

    [Test]
    public void TheLayerReadsTheStateNeverWritesItAndRebuildsOnlyOnChange()
    {
        var map = BuildMap();
        try
        {
            var session = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            var before = session.State.Hash();
            Assert.IsTrue(layer.Show(C, session.State));
            Assert.AreEqual(before, session.State.Hash(), "drawing the units changed the state");
            Assert.IsFalse(layer.Show(C, session.State), "the same roster draws nothing new");
            // The GDD loop draws the sim's units (GDD Phase 5 PR 14): eight brigades and the two ships; the 7th arrives from the 15 June turn (R-UNIT-009).
            Assert.AreEqual(10, layer.Tokens.Count);
            var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
            Assert.AreEqual(10, tokens.childCount);
            var kiryati = tokens.Find("kiryati");
            Assert.AreEqual(3, kiryati.Cast<Transform>().Count(t => t.name.StartsWith("Rifleman")), "three riflemen at full strength (Q-UI-3)");
            var badge = kiryati.Find("Badge");
            Assert.IsNotNull(badge, "the promo's badge over every token");
            Assert.AreEqual(2, badge.GetComponentsInChildren<MeshRenderer>().Length, "a plate and a glyph");
            Assert.Greater(badge.position.y, kiryati.Find("Rifleman0").position.y + UnitBadge.FigureHeightM * UnitShowcase.FigureScale, "above the figures");
            for (int i = 0; i < 3; i++)
            {
                var b = session.State.Hash();
                session.Step(HistoricalAI.Commands(C, session.State));
                var after = session.State.Hash();
                layer.Show(C, session.State);
                Assert.AreEqual(after, session.State.Hash(), $"turn {i}: drawing changed the state");
                Assert.AreNotEqual(b, after);
                if (i == 0) Assert.IsNotNull(layer.Tokens.FirstOrDefault(p => p.Token.Unit.Id == "squadron_101").Token, "101 Squadron from 20 May 1948");
            }
            CollectionAssert.Contains(VisualToggles.Names, UnitLayer.ToggleName);
            var any = layer.Tokens[0];
            VisualToggles.Set(UnitLayer.ToggleName, false);
            Assert.IsFalse(map.Terrain.transform.Find(UnitLayer.RootName).gameObject.activeSelf);
            Assert.IsNull(layer.UnitToSelectAt(any.Q, any.R), "a hidden unit cannot be selected (review M1)");
            VisualToggles.Set(UnitLayer.ToggleName, true);
            Assert.AreEqual(any.Token.Unit.Id, layer.UnitToSelectAt(any.Q, any.R));
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
        CollectionAssert.DoesNotContain(VisualToggles.Names, UnitLayer.ToggleName, "the map's teardown removes the switch");
    }

    [Test]
    public void TheHebrewUnitCardShowsTheSourcedNameAndEnglishKeepsItsName()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        var state = StateAt(C.Rules.Start, 5, 0.8);
        foreach (var u in Units)
        {
            // Hidden rows can have no sourced anchor; the card formatter still reads their names.
            var anchor = u.Anchors.FirstOrDefault() ?? new UnitAnchor { Place = "test position" };
            var placed = new PlacedToken(new UnitToken(u, anchor, 0, "", false), default);
            Assert.AreEqual(u.NameHe, UnitCard.For(placed, state, he).Title, u.Id + ": Hebrew title");
            Assert.AreEqual(u.Name, UnitCard.For(placed, state, Text).Title, u.Id + ": English title");
            // The sector in Hebrew where the row has one (#1292), else its English; the English card keeps the English.
            StringAssert.Contains(u.SectorHe ?? u.Sector, UnitCard.For(placed, state, he).Body, u.Id + ": sourced biography retained");
            StringAssert.Contains(u.Sector, UnitCard.For(placed, state, Text).Body, u.Id + ": English biography retained");
        }
    }

    [Test]
    public void AUnitWithNoHebrewNameKeepsItsEnglishNameInTheHebrewCard()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        var u = Units.First(x => x.Id == "golani");
        u.NameHe = "";
        var placed = new PlacedToken(new UnitToken(u, u.Anchors[0], 0, "", false), default);
        var state = StateAt(C.Rules.Start, 5, 0.8);
        Assert.AreEqual(u.Name, UnitCard.For(placed, state, he).Title);
    }

    // ---- Optional Hebrew card prose (#558) -------------------------------------------------------------------------
    // Test-only fixtures: the Hebrew below is placeholder text ("test place", "test commander", "test sector"), not a
    // translation of any real row and not a historical source.

    const string HeSource = "[{url: 'https://he.example.org/a', checked: '2026-10-19'}]";
    const string PlaceHe = "place_he: מקום בדיקה, place_he_sources: " + HeSource + ", place_he_review: agent-checked, ";
    const string CommanderHe = "first_commander_he: מפקד בדיקה, first_commander_he_sources: " + HeSource + ", first_commander_he_review: agent-checked, ";
    const string SectorHe = "sector_he: גזרת בדיקה, sector_he_sources: " + HeSource + ", sector_he_review: agent-checked, ";

    /// <summary>A one-unit file with the three English prose fields, an anchor whose only source is Wikipedia, and the
    /// given Hebrew counterparts (any of <see cref="PlaceHe"/>, <see cref="CommanderHe"/>, <see cref="SectorHe"/>).</summary>
    static UnitEntry ProseUnit(string placeHe = "", string unitHe = "", string commander = "first_commander: Test Commander, ")
        => UnitData.Parse("units:\n- {id: a, name: A, number: '1', service: army, side: israel, " + commander + unitHe
            + "sector: Test sector, status: draft, wikipedia_only: true, anchors: [{place: Test Place, " + placeHe
            + "lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', end_basis: source, end_note: n, "
            + "sources: [{url: 'https://en.wikipedia.org/wiki/X', checked: '2026-10-11'}]}]}", "u.yaml")[0];

    static UnitCard ProseCard(UnitEntry u, UiText text)
        => UnitCard.For(new PlacedToken(new UnitToken(u, u.Anchors[0], 0, "", false), default), StateAt(C.Rules.Start, 5, 0.8), text);

    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    /// <summary>#558: the three Hebrew counterparts load beside the English and show in the Hebrew card only; the English
    /// card is the same as for the row without them, and their sources join the footer without clearing the draft note.</summary>
    [Test]
    public void TheHebrewProseShowsInTheHebrewCardAndTheEnglishCardIsUnchanged()
    {
        var u = ProseUnit(PlaceHe, CommanderHe + SectorHe);
        Assert.AreEqual(("מקום בדיקה", "מפקד בדיקה", "גזרת בדיקה"), (u.Anchors[0].PlaceHe, u.FirstCommanderHe, u.SectorHe));
        Assert.AreEqual(("agent-checked", "agent-checked", "agent-checked"), (u.Anchors[0].PlaceHeReview, u.FirstCommanderHeReview, u.SectorHeReview));
        var he = ProseCard(u, He);
        Assert.AreEqual("מיקום: מקום בדיקה\nמפקד ראשון: מפקד בדיקה\nגזרת בדיקה", he.Body);
        var english = ProseCard(u, Text);
        var plain = ProseCard(ProseUnit(), Text);
        Assert.AreEqual((plain.Title, plain.Subtitle, plain.Body, plain.Note), (english.Title, english.Subtitle, english.Body, english.Note));
        Assert.AreEqual("At Test Place\nFirst commander: Test Commander\nTest sector", english.Body);
        StringAssert.Contains("he.example.org", he.Footer, "the Hebrew text's sources are listed, as name_he's are");
        StringAssert.Contains("he.example.org", english.Footer, "in both languages, as name_he's are");
        Assert.AreEqual(He["unit.draft_wikipedia"], he.Note, "a Hebrew text's source does not check a fact (#902)");
        Assert.IsTrue(UnitData.IsWikipediaOnly(u));
    }

    /// <summary>#558: each Hebrew field falls back to its own English value when it is left out, whatever the other two do.
    /// (The Hebrew strings wrap an English value in left-to-right marks, U+200E, as they do on main.)</summary>
    [TestCase("", CommanderHe + SectorHe, "מיקום: \u200ETest Place\u200E\nמפקד ראשון: מפקד בדיקה\nגזרת בדיקה")]
    [TestCase(PlaceHe, SectorHe, "מיקום: מקום בדיקה\nמפקד ראשון: \u200ETest Commander\u200E\nגזרת בדיקה")]
    [TestCase(PlaceHe, CommanderHe, "מיקום: מקום בדיקה\nמפקד ראשון: מפקד בדיקה\nTest sector")]
    [TestCase("", "", "מיקום: \u200ETest Place\u200E\nמפקד ראשון: \u200ETest Commander\u200E\nTest sector")]
    public void EachHebrewFieldFallsBackToItsOwnEnglish(string placeHe, string unitHe, string body)
    {
        Assert.AreEqual(body, ProseCard(ProseUnit(placeHe, unitHe), He).Body);
    }

    /// <summary>#558 (owner, 19 October 2026): a Hebrew commander never adds the line the English row does not have.</summary>
    [Test]
    public void AHebrewCommanderWithoutTheEnglishAddsNoLine()
    {
        var u = ProseUnit(commander: "");
        u.FirstCommanderHe = "מפקד בדיקה"; // past the loader, which refuses this row (BadHebrewProseNamesTheProblem)
        Assert.AreEqual("מיקום: \u200ETest Place\u200E\nTest sector", ProseCard(u, He).Body);
    }

    /// <summary>#1292: the Hebrew card names a formed or since date's month in Hebrew (date.month_*), as the event cards'
    /// dates do; a date in prose without formed_he or since_he keeps its English, and the English card is unchanged.</summary>
    [TestCase("March 1948", "24 May 1948", "הוקמה: מרץ 1948", "מאז ‎24‎ במאי ‎1948‎")]
    [TestCase("Late 1947", "the first half of May 1948", "הוקמה: ‎Late 1947‎", "מאז ‎the first half of May 1948‎")]
    public void TheHebrewCardNamesTheMonthsOfItsDatesInHebrew(string formed, string since, string formedLine, string sinceText)
    {
        var u = ProseUnit();
        u.Formed = formed; u.Anchors[0].Since = since;
        var body = ProseCard(u, He).Body;
        StringAssert.Contains(formedLine, body);
        StringAssert.Contains(sinceText, body);
        StringAssert.Contains($"At Test Place since {since}\nFormed {formed}\n", ProseCard(u, Text).Body);
    }

    const string FormedHe = "formed_he: סוף 1947, formed_he_sources: " + HeSource + ", formed_he_review: agent-checked, ";
    const string SinceHe = "since: the first half of May 1948, since_he_sources: " + HeSource + ", since_he_review: agent-checked, since_he: המחצית הראשונה של מאי 1948, ";

    /// <summary>#1292: a formed or since date in prose shows its Hebrew counterpart (formed_he, since_he) in the Hebrew
    /// card, each falling back to its English alone; the English card is unchanged.</summary>
    [TestCase(FormedHe, SinceHe, "מיקום: מקום בדיקה, מאז המחצית הראשונה של מאי 1948\nהוקמה: סוף 1947\n")]
    [TestCase("", SinceHe, "מיקום: מקום בדיקה, מאז המחצית הראשונה של מאי 1948\nהוקמה: \u200ELate 1947\u200E\n")]
    [TestCase(FormedHe, "since: the first half of May 1948, ", "מיקום: מקום בדיקה, מאז \u200Ethe first half of May 1948\u200E\nהוקמה: סוף 1947\n")]
    public void AProseDateShowsItsHebrewInTheHebrewCard(string formedHe, string sinceHe, string start)
    {
        var u = ProseUnit(PlaceHe + sinceHe, "formed: Late 1947, " + formedHe);
        StringAssert.StartsWith(start, ProseCard(u, He).Body);
        StringAssert.StartsWith("At Test Place since the first half of May 1948\nFormed Late 1947\n", ProseCard(u, Text).Body);
    }

    /// <summary>#1292, the issue's example: the Negev Brigade's Hebrew card has no English month name left.</summary>
    [Test]
    public void TheNegevBrigadesHebrewCardHasNoEnglishMonth()
    {
        var u = Units.Single(x => x.Id == "negev");
        var body = ProseCard(u, He).Body;
        StringAssert.Contains("הוקמה: מרץ 1948", body);
        StringAssert.Contains("מאז מרץ 1948", body);
        StringAssert.DoesNotContain("March", body);
    }

    /// <summary>#1292, the issue's example: the Negev Brigade's Hebrew card shows its first commander and sector in Hebrew.</summary>
    [Test]
    public void TheNegevBrigadesHebrewCardShowsItsCommanderAndSectorInHebrew()
    {
        var body = ProseCard(Units.Single(x => x.Id == "negev"), He).Body;
        StringAssert.Contains("מפקד ראשון: נחום שריג\n", body);
        StringAssert.Contains("\nהנגב, בשתי גזרות שכביש באר שבע-עזה מפריד ביניהן; מטה החטיבה במחנה מקורות שליד ניר עם.", body);
        StringAssert.DoesNotContain("Nahum Sarig", body);
        StringAssert.DoesNotContain("Beersheba", body);
    }

    // hidden: the rows with "anchors: []" reach the source checks, which run after the anchors'.
    const string ProseRow = "units:\n- {id: a, name: A, number: '1', service: army, side: israel, sector: s, status: draft, wikipedia_only: true, hidden: h, ";
    const string ProseAnchor = "anchors: [{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', end_basis: source, end_note: n, sources: [{url: 'https://x.org', checked: '2026-10-11'}], ";

    // Orphans: a Hebrew text, its sources or its review flag without what it belongs to.
    [TestCase(ProseRow + CommanderHe + "anchors: []}", "first_commander_he is set without first_commander")]
    [TestCase(ProseRow + "first_commander: C, first_commander_he_sources: " + HeSource + ", anchors: []}", "first_commander_he_sources is set without first_commander_he")]
    [TestCase(ProseRow + "first_commander: C, first_commander_he_review: agent-checked, anchors: []}", "first_commander_he_review is set without first_commander_he")]
    [TestCase(ProseRow + "sector_he_sources: " + HeSource + ", anchors: []}", "sector_he_sources is set without sector_he")]
    [TestCase(ProseRow + "sector_he_review: agent-checked, anchors: []}", "sector_he_review is set without sector_he")]
    [TestCase(ProseRow + ProseAnchor + "place_he_sources: " + HeSource + "}]}", "place_he_sources is set without place_he")]
    [TestCase(ProseRow + ProseAnchor + "place_he_review: agent-checked}]}", "place_he_review is set without place_he")]
    // Present but blank: an empty or null key is an error, never read as "left out".
    [TestCase(ProseRow + "sector_he: '', anchors: []}", "sector_he is blank")]
    [TestCase(ProseRow + "sector_he: ~, anchors: []}", "sector_he is blank")]
    [TestCase(ProseRow + ProseAnchor + "place_he: '  '}]}", "place_he is blank")]
    [TestCase(ProseRow + "first_commander: C, first_commander_he: מפקד, first_commander_he_sources: ~, anchors: []}", "first_commander_he_sources is empty")]
    [TestCase(ProseRow + "sector_he: גזרה, sector_he_sources: [], anchors: []}", "sector_he_sources is empty")]
    [TestCase(ProseRow + "sector_he: גזרה, sector_he_sources: " + HeSource + ", sector_he_review: '', anchors: []}", "sector_he_review must be agent-checked")]
    [TestCase(ProseRow + "sector_he_review: ~, anchors: []}", "sector_he_review must be agent-checked")]
    // Invalid metadata and text.
    [TestCase(ProseRow + "sector_he: גזרה, sector_he_sources: " + HeSource + ", sector_he_review: reviewed, anchors: []}", "sector_he_review must be agent-checked")]
    [TestCase(ProseRow + "sector_he: Sector, sector_he_sources: " + HeSource + ", anchors: []}", "sector_he must contain Hebrew letters")]
    [TestCase(ProseRow + "sector_he: גזרה, sector_he_sources: [{url: 'http://x.org', checked: '2026-10-19'}], anchors: []}", "without an https url")]
    [TestCase(ProseRow + "sector_he: גזרה, sector_he_sources: [{url: 'https://x.org'}], anchors: []}", "source https://x.org: checked")]
    [TestCase(ProseRow + FormedHe + "anchors: []}", "formed_he is set without formed")]
    [TestCase(ProseRow + ProseAnchor + "since_he_review: agent-checked}]}", "since_he_review is set without since_he")]
    [TestCase(ProseRow + ProseAnchor + "since_he: סוף 1947, since_he_sources: " + HeSource + "}]}", "since_he is set without since")]
    // Only the five meanings: other Hebrew keys are unknown, and a key twice is refused.
    [TestCase(ProseRow + "number_he: 'חטיבה 1', anchors: []}", "Property 'number_he' not found")]
    [TestCase(ProseRow + "sector_he: גזרה, sector_he: גזרה, anchors: []}", "duplicate key sector_he")]
    public void BadHebrewProseNamesTheProblem(string yaml, string expected)
    {
        var e = Assert.Throws<InvalidDataException>(() => UnitData.Parse(yaml, "u.yaml"));
        StringAssert.StartsWith("u.yaml:", e.Message);
        StringAssert.Contains(expected, e.Message);
    }

    /// <summary>Review I1: "since" only from a sourced start; never the game's first day.</summary>
    [Test]
    public void TheCardGivesASinceOnlyFromASourcedStart()
    {
        var text = Text;
        PlacedToken Token(string id, GameState s) => new PlacedToken(UnitRoster.Build(Units, C, s).Single(t => t.Unit.Id == id), default);
        var start = StateAt(C.Rules.Start, 5, 0.8);
        var kiryati = UnitCard.For(Token("kiryati", start), start, text);
        StringAssert.Contains("At Tel Aviv since February 1948", kiryati.Body);
        StringAssert.DoesNotContain("15 May 1948", kiryati.Body);
        var givati = UnitCard.For(Token("givati", start), start, text);
        StringAssert.Contains("At Gedera\n", givati.Body + "\n");
        StringAssert.DoesNotContain("since", givati.Body.Split('\n').First(l => l.StartsWith("At ")), "its move south is not dated");
        var latrun = StateAt(new DateTime(1948, 5, 29), 5, 0.8);
        StringAssert.Contains("At Hulda (headquarters for the Latrun attacks) since 24 May 1948", UnitCard.For(Token("brigade_7", latrun), latrun, text).Body);
        var ship = UnitCard.For(Token("ins_haganah", start), start, text);
        StringAssert.Contains("commissioned 1948", ship.Subtitle);
        StringAssert.Contains("Wikipedia only", UnitCard.For(Token("golani", start), start, text).Note, "Golani is still Wikipedia only; Kiryati cites the Haganah lexicon (P66)");
        var hatzor = StateAt(new DateTime(1948, 12, 1), 5, 0.8);
        StringAssert.DoesNotContain("Wikipedia only", UnitCard.For(Token("squadron_101", hatzor), hatzor, text).Note,
            "Hatzor's position cites Eduard (review M8: the note is per position)");
        var july = StateAt(new DateTime(1948, 7, 1), 5, 0.8);
        StringAssert.Contains("Wikipedia only", UnitCard.For(Token("squadron_101", july), july, text).Note, "Herzliya's position is Wikipedia only");
    }

    [Test]
    public void ClickingAUnitSelectsItWithOneHighlightAndTheCardLinksToDefence()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            layer.Show(bridge.Content, bridge.State);
            var nav = new ScreenNav(ui.Root);
            var subdistricts = Subdistricts1948.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", Subdistricts1948.FileName)));
            var regions = new MapRegions(subdistricts, Armistice1949.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", Armistice1949.FileName))));
            regions.Apply(subdistricts.ShownFrom);   // shown, as at the game's start
            var towns = SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName))).ToDictionary(t => t.Id, t => t.Name);
            var info = new InfoPanelPresenter(ui.Root, bridge, Text, towns, nav: nav,
                selectedUnit: () => layer.Selected is string id ? layer.Tokens.First(p => p.Token.Unit.Id == id) : (PlacedToken?)null,
                regions: regions);
            var root = map.Terrain.transform.Find(UnitLayer.RootName);
            var golani = layer.Tokens.Single(p => p.Token.Unit.Id == "golani");
            var tlv = C.SettlementHexes.Single(t => t.Id == "tel_aviv");
            info.Show(tlv.Q, tlv.R);
            Assert.IsNotEmpty(ui.Q<Label>("info-region").text, "a hex names its 1948 sub-district");
            map.Select(golani.Q, golani.R);   // MapPointer outlines the hex before it raises HexClicked
            GameRoot.OnHexClicked(map, layer, info, golani.Q, golani.R);
            Assert.AreEqual("golani", layer.Selected);
            Assert.IsNotNull(root.Find("SelectionRing"), "the promo's white ring round the selected token");
            Assert.IsFalse(map.Terrain.transform.Find(MapScene.SelectionName).GetComponent<MeshRenderer>().enabled, "one highlight: the ring, not the hex outline (review M2)");
            Assert.IsTrue(info.ShowsUnit);
            Assert.AreEqual("", ui.Q<Label>("info-region").text, "a unit's card names no sub-district (review MF2)");
            var words = ui.Q<VisualElement>("info-panel").Query<Label>().ToList().Select(l => l.text).ToList();
            Assert.AreEqual("Golani Brigade", ui.Q<Label>("info-title").text);
            Assert.IsTrue(words.Any(w => w.Contains("At Yavne'el since February 1948")), string.Join(" | ", words));
            Assert.IsFalse(words.Any(w => w.Contains("Reserve call-up")), "the GDD loop: the call-up no longer sets the figures (#1205) | " + string.Join(" | ", words));
            Assert.IsTrue(words.Any(w => w.Contains("Wikipedia only")), "a Wikipedia-only position says it is a draft");
            Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("build-camp").style.display.value, "no orders on a unit");
            var defence = ui.Q<Button>("info-defence");
            Assert.AreEqual(DisplayStyle.Flex, defence.style.display.value);
            UiTestKit.Press(defence);
            Assert.AreEqual("defence", nav.Current);
            nav.Open(null);   // the Defence screen covers the panel; close it before clicking the map again
            GameRoot.OnHexClicked(map, layer, info, golani.Q, golani.R);
            Assert.IsNull(layer.Selected, "a second click shows the hex and its map actions");
            Assert.IsNull(root.Find("SelectionRing"));
            Assert.IsFalse(info.ShowsUnit);
            Assert.AreEqual(DisplayStyle.None, defence.style.display.value);
            // A screen covering the panel (#210): a click behind it moves neither the ring nor the card.
            var hud = ui.Q<VisualElement>("info-panel").parent;
            hud.AddToClassList(ScreenNav.WideOpenClass);
            var tlvHex = C.SettlementHexes.Single(t => t.Id == "tel_aviv");
            map.Select(tlvHex.Q, tlvHex.R);   // MapPointer outlines the clicked hex (Tel Aviv) before it raises HexClicked
            GameRoot.OnHexClicked(map, layer, info, tlvHex.Q, tlvHex.R);
            Assert.IsNull(layer.Selected, "covered: no selection");
            Assert.AreEqual((golani.Q, golani.R), info.Selected, "covered: the panel keeps its hex");
            Assert.IsTrue(map.Terrain.transform.Find(MapScene.SelectionName).GetComponent<MeshRenderer>().enabled, "the outline is back on the panel's hex");
            Assert.IsFalse(info.ShowsUnit);
            hud.RemoveFromClassList(ScreenNav.WideOpenClass);
            info.Dispose(); nav.Dispose();
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Review M5: a selected unit that leaves the map on a turn is deselected and the card falls back to the hint.</summary>
    [Test]
    public void ASelectedUnitThatLeavesTheMapClearsItsCard()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(), null,
                () => layer.Selected is string id ? layer.Tokens.First(p => p.Token.Unit.Id == id) : (PlacedToken?)null);
            // The legacy loop, whose roster follows the dates and the call-up (the GDD loop draws the sim's units).
            layer.Show(SimBridgeGddTests.Legacy, StateAt(new DateTime(1948, 7, 12), 5, 0.8));
            var yiftach = layer.Tokens.Single(p => p.Token.Unit.Id == "yiftach");
            GameRoot.OnHexClicked(map, layer, info, yiftach.Q, yiftach.R);
            Assert.IsTrue(info.ShowsUnit);
            layer.Show(SimBridgeGddTests.Legacy, StateAt(new DateTime(1948, 7, 24), 5, 0.8));   // Operation Dani is over
            Assert.IsNull(layer.Selected);
            Assert.IsNull(map.Terrain.transform.Find(UnitLayer.RootName).Find("SelectionRing"));
            info.Render();
            Assert.IsFalse(info.ShowsUnit);
            Assert.AreEqual(Text["info.hint"], ui.Q<Label>("info-title").text);
            info.Dispose();
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The 1948-49 IAF roundel (owner question, 2026-10-12): a FILLED blue Star of David, not an outline, its
    /// points clear of the white disc's edge, no outer ring (Efrati, Colors &amp; Markings of the Israeli Air Force, p. 6:
    /// the open star was filled for ground visibility in May 1948, "as applied on IAF aircraft ever since"; FOTW).</summary>
    [Test]
    public void TheRoundelIsAFilledStarOfDavidOnAWhiteDiscWithNoRing()
    {
        var star = UnitModels.RoundelStar();
        Assert.AreEqual(2, star.Length, "two triangles");
        foreach (var tri in star)
        {
            Assert.AreEqual(3, tri.Count, "each triangle is one filled plate (an outline would be a band with a hole)");
            Assert.IsTrue(Inside(Vector2.zero, tri), "filled: the star covers the disc's centre, which an outline star leaves white");
            foreach (var v in tri) Assert.AreEqual(UnitModels.RoundelStarShare, v.magnitude, 1e-4f);
        }
        Assert.Less(UnitModels.RoundelStarShare, 0.9f, "the points stay clear of the disc's edge");
        Assert.Greater(UnitModels.RoundelStarShare, 0.6f);
        var points = star.SelectMany(t => t).Select(v => Mathf.Repeat(Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg, 360f)).OrderBy(a => a).ToList();
        for (int i = 0; i < 6; i++) Assert.AreEqual(30f + 60f * i, points[i], 1e-3f, "a six-pointed star, a point straight up the plane's local axis");
        var turned = UnitModels.RoundelStar(30f).SelectMany(t => t).Select(v => Mathf.Repeat(Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg, 360f)).ToList();
        Assert.IsTrue(turned.Any(a => Mathf.Abs(a - 0f) < 1e-3f || Mathf.Abs(a - 360f) < 1e-3f) && turned.Any(a => Mathf.Abs(a - 180f) < 1e-3f),
            "turned 30 degrees on a fuselage side, a point is straight up on either side");
        // On the models: six roundels on the Spitfire (two fuselage, two on top of the wings, two under), four on the S-199
        // (two fuselage, two under the wings: no over-wing roundels before October 1948), each star 2 triangles x 2 faces.
        var spit = UnitModels.Spitfire("s");
        try
        {
            int blue = spit.uv.Count(uv => Mathf.Abs(uv.x - UnitPalette.U(UnitPalette.RoundelBlue)) < 1e-4f);
            Assert.AreEqual(6 * 2 * 3 * 2, blue, "six filled stars, no blue ring");
            var avia = UnitModels.AviaS199("a-wings");
            try
            {
                int wingBlue = avia.vertices.Where((p, i) => Mathf.Abs(p.x) > 1.5f && Mathf.Abs(avia.uv[i].x - UnitPalette.U(UnitPalette.RoundelBlue)) < 1e-4f).Count();
                Assert.AreEqual(2 * 2 * 3 * 2, wingBlue, "the S-199's wing stars: under the wings only, none on top (Efrati pp. 6, 11)");
            }
            finally { UnityEngine.Object.DestroyImmediate(avia); }
            // The fuselage roundels face outwards: each star sits outside its white disc, not behind it.
            foreach (var mesh in new[] { spit, UnitModels.AviaS199("a") })
            {
                var v = mesh.vertices; var uv = mesh.uv;
                bool Is(int i, int slot) => Mathf.Abs(uv[i].x - UnitPalette.U(slot)) < 1e-4f;
                bool Fuselage(Vector3 p) => p.z > -3.0f && p.z < -1.8f && Mathf.Abs(p.y) < 0.7f && Mathf.Abs(p.x) > 0.3f && Mathf.Abs(p.x) < 0.8f;
                foreach (var side in new[] { 1f, -1f })
                {
                    var blueX = Enumerable.Range(0, v.Length).Where(i => Is(i, UnitPalette.RoundelBlue) && Fuselage(v[i]) && Mathf.Sign(v[i].x) == side).Select(i => Mathf.Abs(v[i].x)).ToList();
                    var whiteX = Enumerable.Range(0, v.Length).Where(i => Is(i, UnitPalette.RoundelWhite) && Fuselage(v[i]) && Mathf.Sign(v[i].x) == side && Mathf.Abs(v[i].x) > 0.38f).Select(i => Mathf.Abs(v[i].x)).ToList();
                    Assert.IsNotEmpty(blueX, $"{mesh.name}: a fuselage star on side {side}");
                    Assert.Greater(blueX.Min(), whiteX.Max() - 1e-4f, $"{mesh.name}: the star on side {side} is outside its disc");
                }
                if (mesh != spit) UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(spit); }
    }

    static bool Inside(Vector2 p, List<Vector2> tri)
    {
        float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        float d1 = Cross(tri[0], tri[1], p), d2 = Cross(tri[1], tri[2], p), d3 = Cross(tri[2], tri[0], p);
        return (d1 >= 0 && d2 >= 0 && d3 >= 0) || (d1 <= 0 && d2 <= 0 && d3 <= 0);
    }

    [Test]
    public void TheS199IsRepeatableAndToScale()
    {
        var plain = UnitModels.AviaS199("a"); var again = UnitModels.AviaS199("b");
        var prop = UnitModels.S199Propeller("p");
        try
        {
            CollectionAssert.AreEqual(plain.vertices, again.vertices);
            CollectionAssert.AreEqual(plain.uv, again.uv);
            var b = plain.bounds;
            Assert.AreEqual(9.92f, b.size.x, 0.25f, "span (Avia S-199 specifications)");
            Assert.AreEqual(8.94f, b.size.z, 0.25f, "length");
            Assert.Less(plain.triangles.Length / 3 + prop.triangles.Length / 3, 6000);
            int Slot(Mesh m, int slot) => m.uv.Count(uv => Mathf.Abs(uv.x - UnitPalette.U(slot)) < 1e-4f);
            Assert.AreEqual(0, Slot(plain, UnitPalette.MarkingRed), "no Operation Yoav markings: the map shows the Spitfire from 15 October 1948");
            Assert.Greater(Slot(plain, UnitPalette.RoundelBlue), 0, "roundels under the wings");
            Assert.Greater(Slot(plain, UnitPalette.RoundelWhite), 0, "and the white rear-fuselage bands");
        }
        finally { foreach (var m in new[] { plain, again, prop }) UnityEngine.Object.DestroyImmediate(m); }
    }
}
