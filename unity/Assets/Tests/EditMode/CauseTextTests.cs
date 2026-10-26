#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>B1: the turn report, the meter tip and the Why screen word the economy, population, effect and security lines
/// (CauseKeys) in the player's language through CauseText. In English every row is the sim's Message, word for word, so
/// the English report is what it was before B1; in Hebrew each keyed line is Hebrew, its amounts the only Latin text.</summary>
public class CauseTextTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly IReadOnlyList<UnitEntry> NoRows = Array.Empty<UnitEntry>();
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    /// <summary>The golden seed's whole game, played by the reference player, turn by turn.</summary>
    static IEnumerable<StepResult> Turns()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        for (int t = 0; t < 1000 && s.GameOver == null; t++)
        {
            var r = Simulation.Step(C, s, GoldenRun.Orders(C, s));
            yield return r;
            s = r.State;
        }
    }

    /// <summary>Before B1 the report and the tip showed each cause's Message; now CauseText's English. For a seeded game,
    /// every turn and every meter, the rows are the same, character for character, and so is every Why line.</summary>
    [Test]
    public void TheEnglishRowsAreTheSameAsBefore()
    {
        var en = En; int rows = 0, keyed = 0;
        foreach (var turn in Turns())
        {
            foreach (var id in HudModel.MeterIds)
            {
                var before = HudModel.Causes(turn, id, w => w.Message);
                CollectionAssert.AreEqual(before, HudModel.Causes(turn, id, w => CauseText.Line(w, C, NoRows, en)), id);
                rows += before.Count;
            }
            foreach (var w in turn.Why.Where(w => CauseKeys.All.Contains(w.Key)))
            {
                Assert.AreEqual(w.Message, CauseText.Line(w, C, NoRows, en), w.Key);
                keyed++;
            }
        }
        Assert.Greater(rows, 100, "precondition: the game has report rows");
        Assert.Greater(keyed, 400, "precondition: the game has keyed lines (531 at 7ec163c4)");
    }

    /// <summary>In Hebrew every keyed line of the seeded game is Hebrew: right to left, every placeholder filled, and no
    /// Latin letter outside a value UiText marked left to right (an amount such as "IL -6.8m").</summary>
    [Test]
    public void EveryKeyedLineIsHebrewInHebrew()
    {
        var he = He; var seen = new HashSet<string>();
        // GameRoot passes the map's sourced Hebrew settlement names to every report renderer.
        var names = Statecraft.Game.Map.SettlementPlacer.Parse(System.IO.File.ReadAllText(
            System.IO.Path.Combine(Paths.Content, "map", "settlements_1948.yaml"))).ToDictionary(s => s.Id, s => s.NameHe);
        foreach (var turn in Turns())
            foreach (var w in turn.Why.Where(w => CauseKeys.All.Contains(w.Key)))
            {
                AssertHebrew(CauseText.Line(w, C, NoRows, he, names), w.Key);
                seen.Add(w.Key);
            }
        Assert.GreaterOrEqual(seen.Count, 8, "precondition: the game reaches most keys: " + string.Join(", ", seen));
    }

    [Test]
    public void MissingHebrewPlaceNamesKeepTheMarkedEnglishFallback()
    {
        var row = new Cause("city", "hadera", 0, "Hadera: jobs paid IL 0.0m", key: CauseKeys.WagesPaid);
        var missing = new Dictionary<string, string>();
        var line = CauseText.Line(row, C, NoRows, He);
        Assert.AreEqual(line, CauseText.Line(row, C, NoRows, He, missing));
        StringAssert.Contains(UiText.LeftToRightMark + "Hadera" + UiText.LeftToRightMark, line);
        StringAssert.Contains("תשלום מעבודה", line);
        var unmarked = Regex.Replace(line, UiText.LeftToRightMark + "[^" + UiText.LeftToRightMark + "]*" + UiText.LeftToRightMark, "");
        Assert.IsFalse(Regex.IsMatch(unmarked, "[A-Za-z]"));
    }

    /// <summary>The keyed lines the seeded game does not reach: a call-up order, a situation that ends, civilian upkeep and
    /// its borrowing, the currency crisis on both sides, and the legacy loop's food cut and UN ceasefire.</summary>
    [Test]
    public void TheRareLinesAreWordedInBothLanguages()
    {
        var why = new List<Cause>();
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        Assert.IsNull(SecuritySystem.SetMobilization(C, s, 1.0, why));
        s.ActiveSituations.Add("food_crisis"); s.Indicators["food_supply"] = 0.6;
        EffectsSystem.UpdateSituations(C, s, why);
        // Civilian upkeep: the real content's places cost nothing yet, so the line is the sim's own (EconomySystem.PayCivilianUpkeep),
        // and the borrowing comes from PayOrBorrow with that purpose.
        why.Add(new Cause("economy", "treasury", -2, PlayerText.Change(C, "treasury", -2) + " (civilian upkeep)", key: CauseKeys.CivilianUpkeep));
        EconomySystem.PayOrBorrow(C, s, s.Treasury + 3, "for civilian upkeep", why, CauseKeys.Purpose.CivilianUpkeep);
        s.ActiveSituations.Add(EconomySystem.FxCrisis); s.FxReserves = 1e6;
        EconomySystem.Apply(C, s, 0.25, why);
        s.ActiveSituations.Remove(EconomySystem.FxCrisis); s.FxReserves = -1e6;
        EconomySystem.Apply(C, s, 0.25, why);

        var legacy = SimBridgeGddTests.Legacy;
        var l = Simulation.NewGame(legacy, GoldenRun.Seed, Mode.Historical);
        l.ActiveSituations.Add(EconomySystem.FxCrisis); l.FxReserves = -1e6;
        EconomySystem.Apply(legacy, l, 0.25, why);
        l.Rung = 5; l.CeasefireWeeksLeft = 1;
        SecuritySystem.Apply(legacy, l, 1, why);

        var keys = why.Where(w => CauseKeys.All.Contains(w.Key)).Select(w => w.Key).Distinct().ToList();
        foreach (var key in new[] { CauseKeys.CallUpSet, CauseKeys.SituationEnded, CauseKeys.CivilianUpkeep, CauseKeys.Borrowed,
                     CauseKeys.FxCrisisEased, CauseKeys.FxCrisisBegan, CauseKeys.FxCrisisFood, CauseKeys.Ceasefire })
            CollectionAssert.Contains(keys, key, "precondition");
        var en = En; var he = He;
        foreach (var w in why.Where(w => CauseKeys.All.Contains(w.Key)))
        {
            var content = w.Target == "food_supply" || w.Key == CauseKeys.Ceasefire ? legacy : C;
            Assert.AreEqual(w.Message, CauseText.Line(w, content, NoRows, en), w.Key);
            AssertHebrew(CauseText.Line(w, content, NoRows, he), w.Key);
        }
    }

    /// <summary>Borrowing for every purpose PayOrBorrow's callers give (the English purpose as each caller words it): English
    /// is the Message, Hebrew is Hebrew, and an event's title is its title_he.</summary>
    [Test]
    public void EveryBorrowingPurposeIsWordedInBothLanguages()
    {
        var place = C.SettlementTargets[0].Id; var placeName = C.SettlementNames[place];
        var housing = C.ProjectKinds.First(k => k.Id == "housing");
        var ev = C.Events.First(e => e.TitleHe != "");
        var purposes = new (string Key, string English, string[] Args)[]
        {
            (CauseKeys.Purpose.CivilianUpkeep, "for civilian upkeep", new string[0]),
            (CauseKeys.Purpose.MilitaryUpkeep, "for military upkeep", new string[0]),
            (CauseKeys.Purpose.Corridor, "to build a corridor segment", new string[0]),
            (CauseKeys.Purpose.Arms, "for Arms", new string[0]),
            (CauseKeys.Purpose.Training, "for training", new string[0]),
            (CauseKeys.Purpose.Prisoners, "for our soldiers held by the enemy", new string[0]),
            (CauseKeys.Purpose.TrainingCamp, "to build a training camp", new string[0]),
            (CauseKeys.Purpose.Plow, "to plow fields", new string[0]),
            (CauseKeys.Purpose.Drain, "to drain a swamp", new string[0]),
            (CauseKeys.Purpose.Yishuv, "to build a yishuv", new string[0]),
            (CauseKeys.Purpose.Project, "to build " + housing.Name, new[] { housing.Id }),
            (CauseKeys.Purpose.Deport, $"for the Deport of {placeName}", new[] { place }),
            (CauseKeys.Purpose.Occupation, $"for the occupation of {placeName}", new[] { place }),
            (CauseKeys.Purpose.Event, $"for {ev.Title}", new[] { ev.Id, ev.Title, "1" }),
        };
        CollectionAssert.AreEquivalent(CauseKeys.Purpose.All.Where(p => p != CauseKeys.Purpose.Deficit), purposes.Select(p => p.Key), "every purpose");
        var en = En; var he = He;
        foreach (var (key, english, args) in purposes)
        {
            var why = new List<Cause>();
            var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
            EconomySystem.PayOrBorrow(C, s, s.Treasury + 3, english, why, key, args);
            var line = why.Single();
            Assert.AreEqual(line.Message, CauseText.Line(line, C, NoRows, en), key);
            AssertHebrew(CauseText.Line(line, C, NoRows, he), key);
        }
        var title = new Cause("economy", "debt", 3, "x", key: CauseKeys.Borrowed, args: new[] { CauseKeys.Purpose.Event, ev.Id, ev.Title, "1" });
        StringAssert.Contains(ev.TitleHe, CauseText.Line(title, C, NoRows, he), "the event's Hebrew title");
        StringAssert.Contains(housing.NameHe, CauseText.Line(new Cause("economy", "debt", 3, "x", key: CauseKeys.Borrowed,
            args: new[] { CauseKeys.Purpose.Project, housing.Id }), C, NoRows, he), "the project kind's Hebrew name");
    }

    /// <summary>A line with no key yet keeps the sim's English; a unit line is still UnitNames' (#1399).</summary>
    [Test]
    public void OtherLinesAreUnchanged()
    {
        var plain = new Cause("economy", "treasury", -1, "Money IL -1.0m (military upkeep)");
        Assert.AreEqual(plain.Message, CauseText.Line(plain, C, NoRows, He));
        var turn = Turns().First(t => t.Why.Any(w => WhyKeys.All.Contains(w.Key)));
        foreach (var w in turn.Why.Where(w => WhyKeys.All.Contains(w.Key)))
            Assert.AreEqual(UnitNames.Line(w, C, NoRows, He), CauseText.Line(w, C, NoRows, He));
    }

    /// <summary>C136: a positive producer set prevents missing keys from being silently skipped by the existing tests.</summary>
    [Test]
    public void EveryEventAndUpkeepLineUsesATemplateAndKeepsItsEnglish()
    {
        var en = En; var he = He;
        var lines = Turns().SelectMany(t => t.Why).Where(w => w.System == "events" || w.Key == CauseKeys.MilitaryUpkeep).ToList();
        Assert.IsNotEmpty(lines);
        Assert.IsTrue(lines.Any(w => w.Key == CauseKeys.MilitaryUpkeep));
        foreach (var w in lines)
        {
            CollectionAssert.Contains(CauseKeys.All, w.Key, w.Message);
            Assert.AreEqual(w.Message, CauseText.Line(w, C, NoRows, en), w.Key);
            AssertHebrew(CauseText.Line(w, C, NoRows, he), w.Key);
        }
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        foreach (var row in C.Events.SelectMany(e => e.Options).SelectMany(o => EventSystem.OptionEffects(C, s, o)))
        {
            CollectionAssert.Contains(CauseKeys.All, row.Key);
            Assert.AreEqual(row.Message, CauseText.Line(row, C, NoRows, en), row.Key);
            AssertHebrew(CauseText.Line(row, C, NoRows, he), row.Key);
        }
    }

    [Test]
    public void AResolvedEventUsesItsHistoricalContentSnapshots()
    {
        var w = new Cause("events", "example", 0, "Delegated to Previous name: Previous choice",
            key: CauseKeys.EventDelegated, args: new[] { "Previous choice", "הבחירה הקודמת", "Previous name", "השם הקודם" });
        Assert.AreEqual(w.Message, CauseText.Line(w, C, NoRows, En));
        var line = CauseText.Line(w, C, NoRows, He);
        StringAssert.Contains("השם הקודם", line);
        StringAssert.Contains("הבחירה הקודמת", line);
        AssertHebrew(line, w.Key);
    }

    [Test]
    public void ObjectiveAndExchangeEventRowsKeepEnglishAndRenderHebrew()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var objective = C.Objectives.First(o => o.NameHe != "" && !s.Objectives.ContainsKey(o.Id));
        var lines = new List<Cause>();
        Assert.IsTrue(Objectives.Record(C, s, C.RulesAt(s.Date).Mvp!, objective.Id, s.Date, lines));
        var objectiveLine = lines.Single();
        Assert.AreEqual(CauseKeys.ObjectiveCompleted, objectiveLine.Key);
        StringAssert.Contains(objective.NameHe, CauseText.Line(objectiveLine, C, NoRows, He));
        foreach (var front in Prisoners.Fronts)
        {
            // Renderer fixtures use people counts, not population-unit Delta: preserve the producer's N0 display.
            lines.Add(new Cause("events", "prisoners", 2,
                $"Prisoners exchanged: 12,300 prisoners returned to {PlayerText.Capitalised(front)}",
                key: CauseKeys.PrisonersReturned, args: new[] { "12300", front }));
            lines.Add(new Cause("events", "prisoners", 3,
                $"Prisoners exchanged: 45,600 of our soldiers came home from {PlayerText.Capitalised(front)}",
                key: CauseKeys.SoldiersReturned, args: new[] { "45600", front }));
        }
        foreach (var row in lines)
        {
            CollectionAssert.Contains(CauseKeys.All, row.Key);
            Assert.AreEqual(row.Message, CauseText.Line(row, C, NoRows, En), row.Key);
            AssertHebrew(CauseText.Line(row, C, NoRows, He), row.Key);
        }
    }

    static void AssertHebrew(string line, string key)
    {
        Assert.IsTrue(UiDirection.RightToLeft(line), $"{key}: '{line}' reads right to left");
        StringAssert.DoesNotContain("{", line, key);
        var unmarked = Regex.Replace(line, UiText.LeftToRightMark + "[^" + UiText.LeftToRightMark + "]*" + UiText.LeftToRightMark, "");
        Assert.IsFalse(Regex.IsMatch(unmarked, "[A-Za-z]"), $"{key}: '{line}' has Latin text outside a marked value");
    }

    // Synthetic identities/titles are fixture text, native-review-pending; no content record is edited.
    [Test]
    public void ActualAppointmentRowsKeepDatedTitlesAfterHolderAndDateChange()
    {
        var c = RemainingFixture();
        var date = new DateTime(1948, 5, 15);
        c.Governments[0].From = date.AddDays(-1);
        c.Governments[0].Coalition.Add("party");
        c.Rules.AppointStabilityCostOtherParty = 0.1;
        c.Rules.AppointStabilityCostSameParty = 0.03;
        var office = c.Offices[0];
        office.TitleHe = "תפקיד קודם"; office.TitleHeFrom = date; office.TitleHeUntil = date.AddDays(1);
        c.People[1].Offices.Add(new OfficeTerm { Office = office.Id, From = date.AddDays(-1) });
        c.People[0].Party = "party";
        c.People.Add(new Person { Id = "next", Name = "Next", NameHe = "הבא", Party = "party" });
        var s = new GameState { Date = date, CoalitionStability = 1 };
        var why = new List<Cause>();
        Assert.IsNull(Cabinet.Appoint(c, s, office.Id, "person", why));
        Assert.IsNull(Cabinet.Appoint(c, s, office.Id, "next", why));
        var appointments = why.Where(x => x.Key == "appointed").ToArray();
        var reshuffles = why.Where(x => x.Key == "reshuffle").ToArray();
        Assert.AreEqual(2, appointments.Length); Assert.AreEqual(2, reshuffles.Length);
        CollectionAssert.AreEqual(new[] { "person", "תפקיד קודם", "former" }, appointments[0].Args);
        CollectionAssert.AreEqual(new[] { "", "former", "other", "Office", "תפקיד קודם" }, reshuffles[0].Args);
        CollectionAssert.AreEqual(new[] { "party", "person", "same", "Office", "תפקיד קודם" }, reshuffles[1].Args);
        Assert.AreEqual("Person appointed Office, replacing Former", appointments[0].Message);
        s.Date = date.AddDays(2); office.TitleHe = "תפקיד חדש";
        Assert.AreEqual("next", Cabinet.Holder(c, s, office.Id)!.Id);
        foreach (var row in why)
        {
            Assert.AreEqual(row.Message, CauseText.Line(row, c, NoRows, En), row.Key);
            var he = CauseText.Line(row, c, NoRows, He);
            AssertHebrew(he, row.Key);
            StringAssert.DoesNotContain("תפקיד חדש", he);
            if (row.Key == "appointed" || row.Args[2] == "other") StringAssert.Contains("תפקיד קודם", he);
        }
    }

    [TestCase(false)] [TestCase(true)]
    public void ActualResidentRowsRenderAfterSettlementRecordsAreRemoved(bool fled)
    {
        // Existing CaptureDialog fixture pattern: real rules/map, synthetic capture eligibility and resident facts.
        var events = System.IO.Directory.GetFiles(System.IO.Path.Combine(Paths.Content, "events"), "*.yaml")
            .Select(f => "events/" + System.IO.Path.GetFileName(f));
        var c = ContentLoader.Load(rel => {
            var path = System.IO.Path.Combine(Paths.Content, rel);
            if (!System.IO.File.Exists(path)) return null;
            if (rel == "settlement_targets.yaml") return "targets:\n  - { id: lod, q: -3, r: 21, operation: Fixture, sources: [{ url: \"https://example.org/fixture\", checked: 2026-10-21 }], residents: stayed, residents_sources: [{ url: \"https://example.org/fixture\", checked: 2026-10-21 }], annexation: { people: { value: 60, status: sourced, date: 1948-11-08, sources: [{ url: \"https://example.org/fixture\", checked: 2026-10-21 }] }, employed_share: { value: 0.5, status: design } }, status: draft }\n";
            return System.IO.File.ReadAllText(path);
        }, events);
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        s.Units.Clear(); s.Units.Add(100, new Unit(100, "veteran_soldier", -3, 21, side: Movement.PlayerSide, strength: 3));
        s.Settlements["lod"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date, fled: fled);
        // Preview shares RunOrders with Step and invokes the real producers, without unrelated monthly effects.
        var occupied = Simulation.Preview(c, s, new Command[] { new ResolveCapture("lod", SettlementControl.Occupy) });
        Assert.IsEmpty(occupied.Rejected);
        var why = new List<Cause> { occupied.Why.Single(x => x.Key == "occupation_started") };
        s = occupied.State;
        s.Settlements["lod"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingEnd, s.Date, fled: fled);
        s.Cities.Remove("lod");
        var annexed = Simulation.Preview(c, s, new Command[] { new EndOccupation("lod", SettlementControl.Annex) });
        Assert.IsEmpty(annexed.Rejected);
        why.Add(annexed.Why.Single(x => x.Key == "place_annexed"));
        annexed.State.Settlements.Clear();
        Assert.AreEqual(2, why.Count);
        CollectionAssert.AreEqual(new[] { "occupation_started", "place_annexed" }, why.Select(x => x.Key));
        s.Settlements.Clear();
        var names = new Dictionary<string, string> { ["lod"] = "לוד" };
        foreach (var row in why)
        {
            CollectionAssert.AreEqual(new[] { fled ? "1" : "0" }, row.Args);
            Assert.AreEqual(row.Message, CauseText.Line(row, c, NoRows, En), row.Key);
            var he = CauseText.Line(row, c, NoRows, He, names);
            AssertHebrew(he, row.Key);
            StringAssert.Contains(fled ? "נמלטו" : row.Key == "place_annexed" ? "מצטרפים" : "לא השתנה", he);
        }
    }

    // #1682 synthetic content is deliberately not historical. These values exercise every new formatter branch,
    // including large counts, money rounding and Hebrew names. Actual producer metadata is gated in managed tests.
    static Content RemainingFixture() => new Content
    {
        Money = new List<MoneyAccount> { new MoneyAccount { Id = "treasury", Name = "Money", Prefix = "IL " }, new MoneyAccount { Id = "debt", Name = "Debt", Prefix = "IL " } },
        SettlementNames = new Dictionary<string, string> { ["town"] = "Town" },
        Sites = new List<Site> { new Site { Id = "site", Name = "Site", NameHe = "אתר" } },
        Corridors = new List<CorridorDef> { new CorridorDef { Id = "road", Name = "Road", NameHe = "דרך" } },
        ProjectKinds = new List<ProjectKind> { new ProjectKind { Id = "housing", Name = "Housing", NameHe = "דיור" } },
        People = new List<Person> { new Person { Id = "person", Name = "Person", NameHe = "אדם" }, new Person { Id = "former", Name = "Former", NameHe = "קודם" } },
        Offices = new List<Office> { new Office { Id = "office", Title = "Office", TitleHe = "תפקיד נוכחי" } },
        Parties = new List<Party> { new Party { Id = "party", Name = "Party", NameHe = "מפלגה" }, new Party { Id = "other", Name = "Other", NameHe = "אחרת" } },
        Governments = new List<Government> { new Government { Id = "gov", Name = "Government", NameHe = "ממשלה" } },
        Legislatures = new List<Legislature> { new Legislature { Id = "leg", Body = "Body", BodyHe = "מועצה", Size = 120, Elected = new DateTime(1948, 5, 15) } },
    };

    static Cause[] RemainingRows() => new[]
    {
            new Cause("draft", "town", 1, "Town: 2000000000 groups in work", key: CauseKeys.WorkersAssigned, args: new[] { "2000000000" }),
            new Cause("draft", "town", 1.25, "Town: jobs paid IL +1.3m", key: CauseKeys.WagesPaid),
            new Cause("draft", "town", 3, "Town: 3 groups of immigrants housed", key: CauseKeys.ImmigrantsHoused, args: new[] { "3" }),
            new Cause("draft", "town", 3, "Town: 3 groups more than its homes hold", key: CauseKeys.HousingShortage, args: new[] { "3" }),
            new Cause("draft", "town", 3, "Town: 3 groups of immigrants arrived and wait for homes", key: CauseKeys.ImmigrantsWaiting, args: new[] { "3" }),
            new Cause("draft", "units", 3, "Recruits became veteran soldiers", key: CauseKeys.RecruitsVeteran),
            new Cause("draft", "road", 1, "Road, segment 2: a group of halutzim began building, cost IL 1.3m", key: CauseKeys.CorridorStarted, args: new[] { "road", "2", "1.25" }),
            new Cause("draft", "road", -1, "Road, segment 2: the halutzim stopped building; the Money spent is not returned", key: CauseKeys.CorridorCancelled, args: new[] { "road", "2" }),
            new Cause("draft", "road", 1, "Road, segment 2 built", key: CauseKeys.CorridorCompleted, args: new[] { "road", "2" }),
            new Cause("draft", "site", 1, "A group of halutzim became veteran pioneers", key: CauseKeys.PioneersVeteran),
            new Cause("draft", "debt", 1.25, "Borrowed IL 1.3m to build a housing project", key: CauseKeys.MapBorrowed, args: new[] { "housing" }),
            new Cause("draft", "housing", 1, "Built a housing project: cost IL 1.3m, Housing +2.5%, Social cohesion -2.5%", key: CauseKeys.MapBuilt, args: new[] { "1.25", "0.025", "-0.025" }),
            new Cause("draft", "site", 1, "Site: a group of halutzim began plowing, cost IL 1.3m", key: CauseKeys.LandWorkStarted, args: new[] { "plow", "1.25" }),
            new Cause("draft", "site", 1, "Site: a group of halutzim began building a yishuv, cost IL 1.3m", key: CauseKeys.SiteStarted, args: new[] { "yishuv", "1.25" }),
            new Cause("draft", "site", -1, "Site: the halutzim stopped building; the Money spent is not returned", key: CauseKeys.SiteCancelled),
            new Cause("draft", "site", 1, "Site founded", key: CauseKeys.SiteCompleted, args: new[] { "founded" }),
            new Cause("draft", "town", 1, "Town: Housing began, cost IL 1.3m", key: CauseKeys.ProjectStarted, args: new[] { "housing", "1.25" }),
            new Cause("draft", "town", -1, "Town: Housing was cancelled; the Money spent is not returned", key: CauseKeys.ProjectCancelled, args: new[] { "housing" }),
            new Cause("draft", "town", 1, "Town: Housing was finished", key: CauseKeys.ProjectCompleted, args: new[] { "housing" }),
            new Cause("draft", "arms", 2000000000, "Bought 2000000000 Arms, cost IL 1.3m", key: CauseKeys.ArmsBought, args: new[] { "1.25" }),
            new Cause("draft", "food", 2000000000, "Site: the plowed land yielded 2000000000 units of Food", key: CauseKeys.FoodHarvest, args: new[] { "site" }),
            new Cause("draft", "food", -2000000000, "Food ran out: 2000000000 of 2000000001 units of the month's need went unmet", key: CauseKeys.FoodShortage, args: new[] { "2000000001" }),
            new Cause("draft", "food", -1, "No Food imported (foreign currency crisis): the stock fell to 2000000000 units", key: CauseKeys.FoodWithoutImports, args: new[] { "2000000000" }),
            new Cause("draft", "food", -2000000000, "The prisoners held ate 2000000000 units of Food", key: CauseKeys.PrisonerFood),
            new Cause("draft", "morale", -0.025, "The prisoners held went short of 2000000000 units of Food: Social cohesion -2.5%", key: CauseKeys.PrisonerFoodShortage, args: new[] { "2000000000" }),
            new Cause("draft", "town", 1, "Town: 1 group of craftsmen returned to civilian life and keep their training", key: CauseKeys.CraftsmenReturned, args: new[] { "1" }),
            new Cause("draft", "town", -3, "Town: 3 groups of qualified craftsmen are craftsmen again", key: CauseKeys.CraftsmenReassigned, args: new[] { "3" }),
            new Cause("draft", "42", 1, "Unit 42 returned to civilian life in Town and keeps its training", key: CauseKeys.PioneerReturned, args: new[] { "town" }),
            new Cause("draft", "town", -3, "Town: 3 groups of qualified veteran pioneers are units again", key: CauseKeys.PioneersReassigned, args: new[] { "3", "veteran" }),
            new Cause("draft", "town", -3, "Town: 3 groups began basic training as replacements for unit 42, cost IL 1.3m", key: CauseKeys.TrainingStarted, args: new[] { "3", "basic", "42", "1.25" }),
            new Cause("draft", "arms", -3, "Town: basic training took 3 Arms", key: CauseKeys.TrainingArms, args: new[] { "town", "basic" }),
            new Cause("draft", "town", 3, "Town: 3 groups left basic training; the Money spent is not returned", key: CauseKeys.TrainingCancelled, args: new[] { "3", "basic" }),
            new Cause("draft", "arms", 3, "Town: basic training returned 3 Arms", key: CauseKeys.TrainingArmsReturned, args: new[] { "town", "basic" }),
            new Cause("draft", "town", 0, "Town: unit 42 is no longer in Town, so its replacements stop training", key: CauseKeys.ReplacementsStopped, args: new[] { "42" }),
            new Cause("draft", "town", 3, "Town: 3 groups finished basic training and joined unit 42", key: CauseKeys.ReplacementsJoined, args: new[] { "3", "basic", "42" }),
            new Cause("draft", "town", 3, "Town: 3 groups finished artillery training and are artillery", key: CauseKeys.MilitaryTrained, args: new[] { "3", "artillery", "artillery" }),
            new Cause("draft", "town", 3, "Town: 3 groups finished pioneer training and are halutzim", key: CauseKeys.PioneersTrained, args: new[] { "3", "pioneer" }),
            new Cause("draft", "town", 3, "Town: 3 groups finished craft training and are craftsmen", key: CauseKeys.CraftsmenTrained, args: new[] { "3", "craft" }),
            new Cause("draft", "office", 0, "Person appointed Office, replacing Former", key: CauseKeys.Appointed, args: new[] { "person", "התפקיד שנשמר", "former" }),
            new Cause("draft", "coalition_stability", -0.025, "Coalition stability -2.5% (Party lost the Office)", key: CauseKeys.Reshuffle, args: new[] { "party", "former", "other", "Office", "התפקיד שנשמר" }),
            new Cause("draft", "appointments", -1, "A new government took office with its own ministers; your appointments ended", key: CauseKeys.AppointmentsEnded),
            new Cause("draft", "coalition_stability", -0.025, "Coalition stability -2.5% (public approval)", key: CauseKeys.ApprovalStability),
            new Cause("draft", "game_over", 0, "The coalition collapsed. Your premiership is over.", key: CauseKeys.CoalitionCollapsed),
            new Cause("draft", "leg", 0, "Body convened (120 seats, elected 15 May 1948); coalition holds 61/120", key: CauseKeys.LegislatureConvened, args: new[] { "61", "120" }),
            new Cause("draft", "gov", 0, "Government took office (Party)", key: CauseKeys.GovernmentTookOffice, args: new[] { "party" }),
            new Cause("draft", "gov", 0, "Party left the coalition; Other joined the coalition (coalition holds 61/120)", key: CauseKeys.CoalitionChanged, args: new[] { "party", "other", "61", "120" }),
            new Cause("draft", "era_ended", 0, "The chapter has ended", key: CauseKeys.EraEnded),
            new Cause("draft", "rationing", 0, "Body voted against Rationing at 25%, but its vote does not bind", key: CauseKeys.AdvisoryPolicyRejected, args: new[] { "leg", "0.25" }),
            new Cause("draft", "rationing", 0, "Body voted down Rationing at 25%", key: CauseKeys.PolicyRejected, args: new[] { "leg", "0.25" }),
            new Cause("draft", "rationing", 0.25, "Rationing set to 25%", key: CauseKeys.PolicySet),
            new Cause("draft", "budget", 0, "Body voted against the budget, but its vote does not bind", key: CauseKeys.AdvisoryBudgetRejected, args: new[] { "leg" }),
            new Cause("draft", "budget", 0, "Body voted the budget down", key: CauseKeys.BudgetRejected, args: new[] { "leg" }),
            new Cause("draft", "town", 0, "Town is captured: Israeli units hold it and no enemy unit is there. Its residents fled. It awaits a decision", key: CauseKeys.PlaceCaptured, args: new[] { "1" }),
            new Cause("draft", "town", 0, "Town is under enemy control again: enemy units hold it and no Israeli unit is there. Its residents fled when it was captured", key: CauseKeys.EnemyRegained, args: new[] { "1" }),
            new Cause("draft", "town", 0, "Town is annexed. Its residents fled when it was captured, so no people, homes or jobs are added", key: CauseKeys.PlaceAnnexed, args: new[] { "1" }),
            new Cause("draft", "town", 0, "Town: Israeli units withdrew and it is under enemy control again. Its residents fled when it was captured", key: CauseKeys.PlaceWithdrawn, args: new[] { "1", "1" }),
            new Cause("draft", "town", 0, "Town: Deport. Its residents are displaced and the place is depopulated", key: CauseKeys.PlaceDeported),
            new Cause("draft", "morale", -0.025, "Social cohesion -2.5% (Deport of Town)", key: CauseKeys.DeportEffect, args: new[] { "town" }),
            new Cause("draft", "treasury", -1.25, "Money IL -1.3m (Deport of Town)", key: CauseKeys.DeportCost, args: new[] { "town" }),
            new Cause("draft", "town", 3, "Town is occupied for 3 turns: an Israeli unit stays there. Its residents fled when it was captured", key: CauseKeys.OccupationStarted, args: new[] { "1" }),
            new Cause("draft", "town", 0, "Town: no decision was made before the end, so it counts as a raid", key: CauseKeys.PendingBecameRaid),
            new Cause("draft", "town", 0, "Town: no Israeli unit holds it any more, so the occupation ended as a raid. It is under enemy control again. Its residents fled when it was captured", key: CauseKeys.OccupationLostUnit, args: new[] { "1" }),
            new Cause("draft", "town", 3, "Town: 3 turns of occupation left", key: CauseKeys.OccupationCountdown),
            new Cause("draft", "intl_standing", -0.025, "International support -2.5% (the places depopulated by Deport)", key: CauseKeys.DepopulationSupport),
    };

    [Test]
    public void EveryRemainingKeyPreservesEnglishAndFormatsHebrewIncludingLargeCounts()
    {
        var c = RemainingFixture(); var en = En; var he = He;
        var names = new Dictionary<string, string> { ["town"] = "עיר" };
        var rows = RemainingRows();
        foreach (var row in rows)
        {
            Assert.AreEqual(row.Message, CauseText.Line(row, c, NoRows, en), row.Key);
            AssertHebrew(CauseText.Line(row, c, NoRows, he, names), row.Key);
        }
        CollectionAssert.AreEquivalent(CauseKeys.All.Except(new[] {
            CauseKeys.MoneyFlows, CauseKeys.FxFlows, CauseKeys.Borrowed, CauseKeys.CivilianUpkeep,
            CauseKeys.FxCrisisBegan, CauseKeys.FxCrisisEased, CauseKeys.FxCrisisFood, CauseKeys.Arrived,
            CauseKeys.EffectChange, CauseKeys.SituationBegan, CauseKeys.SituationEnded, CauseKeys.RungChanged,
            CauseKeys.CallUpSet, CauseKeys.CallUpRaised, CauseKeys.CallUpLowered, CauseKeys.Ceasefire,
            CauseKeys.EventAmount, CauseKeys.EventChosen, CauseKeys.EventCity, CauseKeys.EventDelegated,
            CauseKeys.EventDelegatedPartial, CauseKeys.EventGrant, CauseKeys.EventNew, CauseKeys.EventPolicy,
            CauseKeys.EventRejoin, CauseKeys.EventSetRung, CauseKeys.EventStock, CauseKeys.EventUnlock,
            CauseKeys.MilitaryUpkeep, CauseKeys.ObjectiveCompleted, CauseKeys.PrisonersReturned, CauseKeys.SoldiersReturned }), rows.Select(x => x.Key), "all remaining keys");
        var appointed = rows.Single(x => x.Key == CauseKeys.Appointed);
        StringAssert.Contains("התפקיד שנשמר", CauseText.Line(appointed, c, NoRows, he, names), "historical title, not current title");
    }

    [Test]
    public void RemainingOptionalBranchesKeepExactEnglishAndResidentSnapshots()
    {
        var c = RemainingFixture(); var en = En; var he = He;
        var names = new Dictionary<string, string> { ["town"] = "עיר" };
        var rows = new[]
        {
            new Cause("draft", "town", 0, "Town is captured: Israeli units hold it and no enemy unit is there. It awaits a decision", key: CauseKeys.PlaceCaptured, args: new[] { "0" }),
            new Cause("draft", "town", 0, "Town is under enemy control again: enemy units hold it and no Israeli unit is there", key: CauseKeys.EnemyRegained, args: new[] { "0" }),
            new Cause("draft", "town", 0, "Town is annexed. Its residents join the population", key: CauseKeys.PlaceAnnexed, args: new[] { "0" }),
            new Cause("draft", "town", 0, "Town is under enemy control again. Its residents are unchanged", key: CauseKeys.PlaceWithdrawn, args: new[] { "0", "0" }),
            new Cause("draft", "town", 0, "Town: the occupation period has ended. It awaits a decision", key: CauseKeys.OccupationCountdown),
            new Cause("draft", "office", 0, "Person appointed Office", key: CauseKeys.Appointed, args: new[] { "person", "התפקיד שנשמר", "" }),
            new Cause("draft", "coalition_stability", -0.025, "Coalition stability -2.5% (Former resents the reshuffle)", key: CauseKeys.Reshuffle, args: new[] { "", "former", "same", "Office", "התפקיד שנשמר" }),
            new Cause("draft", "town", -1, "Town: 1 group began armoured car training, cost IL 1.3m", key: CauseKeys.TrainingStarted, args: new[] { "1", "armour", "", "1.25" }),
            new Cause("draft", "site", 1, "Site: a group of halutzim began draining the swamp, cost IL 1.3m", key: CauseKeys.LandWorkStarted, args: new[] { "drain", "1.25" }),
            new Cause("draft", "site", 1, "Site: a group of halutzim began building a training camp, cost IL 1.3m", key: CauseKeys.SiteStarted, args: new[] { "camp", "1.25" }),
        };
        // A renderer receives no live state: both resident variants must survive the settlement record's removal.
        foreach (var row in rows)
        {
            Assert.AreEqual(row.Message, CauseText.Line(row, c, NoRows, en), row.Key);
            AssertHebrew(CauseText.Line(row, c, NoRows, he, names), row.Key);
        }
        var stayed = rows.Single(x => x.Key == CauseKeys.PlaceWithdrawn);
        StringAssert.Contains("מצב התושבים לא השתנה", CauseText.Line(stayed, c, NoRows, he, names));
        foreach (var verb in new[] { "built", "plowed", "drained", "founded" })
        {
            var row = new Cause("draft", "site", 1, "Site " + verb, key: CauseKeys.SiteCompleted, args: new[] { verb });
            Assert.AreEqual(row.Message, CauseText.Line(row, c, NoRows, en));
            AssertHebrew(CauseText.Line(row, c, NoRows, he), row.Key);
        }
    }

    [Test]
    public void RemainingRowsPreserveTheWholeEnglishReportInBothLoops()
    {
        foreach (var c in new[] { C, SimBridgeGddTests.Legacy })
        {
            var state = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
            for (var turn = 0; turn < 1000 && state.GameOver == null; turn++)
            {
                var result = Simulation.Step(c, state, GoldenRun.Orders(c, state));
                foreach (var row in result.Why.Where(x => CauseKeys.All.Contains(x.Key)))
                    Assert.AreEqual(row.Message, CauseText.Line(row, c, NoRows, En), row.Key);
                state = result.State;
            }
            Assert.IsNotNull(state.GameOver);
        }
    }
}
