using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The GDD objectives: an objective is recorded once when its condition holds and its card fires once
/// (R-OBJ-001 to R-OBJ-005). A multipart objective pays its base points once, when its final part completes (R-OBJ-006).</summary>
public class ObjectivesTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>One objective on the fixture's site_a (founded 1949-06-12, built in 3 turns).</summary>
    const string ObjectivesYaml = "objectives:\n  - { id: first_goal, name: First goal, completes_when: { site_built: site_a }, reference_date: 1949-06-12, card: first_card }\n";

    const string Card = @"
id: first_card
title: A new yishuv
text: The first new yishuv is founded.
from: 1948-05-15
to: 1957-01-01
objective: first_goal
options:
  - id: continue
    text: Continue
    historical: true
";

    const string PartsYaml = @"
objectives:
  - id: railway
    part_of: infrastructure
    name: Railway
    completes_when: { corridor_complete: rail_a }
    reference_date: 1949-09-20
    card: railway_card
    sources: [{ url: https://example.org/railway, checked: 2026-10-20 }]
  - id: road
    part_of: infrastructure
    name: Road
    completes_when: { corridor_complete: road_a }
    reference_date: 1948-12-07
    card: road_card
    sources: [{ url: https://example.org/road, checked: 2026-10-20 }]
  - id: yishuvim
    part_of: infrastructure
    name: Yishuvim
    completes_when: { sites_built: [site_a, site_b] }
    reference_date: 1950-12-15
    card: yishuvim_card
    sources: [{ url: https://example.org/yishuvim, checked: 2026-10-20 }]
";

    static string PartCard(string id, string objective) => $@"
id: {id}
title: Completed
text: Completed.
from: 1948-05-15
to: 1957-01-01
objective: {objective}
options:
  - id: continue
    text: Continue
    historical: true
";

    static Content LoadParts(string objectives = PartsYaml)
    {
        var secondSite = Fixture.GddSites.Substring(Fixture.GddSites.IndexOf("  - id:", StringComparison.Ordinal))
            .Replace("site_a", "site_b").Replace("Site A", "Site B").Replace("q: 2", "q: 3");
        var files = Fixture.Gdd(new Dictionary<string, string>
        {
            ["objectives.yaml"] = objectives,
            ["sites.yaml"] = Fixture.GddSites + secondSite,
        });
        return ContentLoader.Load(Fixture.Write(files, new Dictionary<string, string>
        {
            ["railway_card.yaml"] = PartCard("railway_card", "railway"),
            ["road_card.yaml"] = PartCard("road_card", "road"),
            ["yishuvim_card.yaml"] = PartCard("yishuvim_card", "yishuvim"),
        }));
    }

    static readonly MvpRules Rules = new MvpRules { ObjectivePoints = 100, ObjectiveBonusPerMonth = 5, ObjectiveBonusCap = 60 };

    static Content Load(bool gdd, Dictionary<string, string>? files = null, string card = Card)
    {
        var o = gdd ? Fixture.Gdd() : new Dictionary<string, string>();
        o["objectives.yaml"] = ObjectivesYaml;
        if (files != null) foreach (var kv in files) o[kv.Key] = kv.Value;
        return ContentLoader.Load(Fixture.Write(o, new Dictionary<string, string> { ["first_card.yaml"] = card }));
    }

    static DateTime D(string iso) => DateTime.Parse(iso, System.Globalization.CultureInfo.InvariantCulture);

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    /// <summary>A new game with one idle halutz group at town_a's hex, ordered on the first turn to build site_a.</summary>
    static StepResult StartBuilding(Content c)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", 1, 0));
        var r = Step(c, s, new PioneerWork(id, Projects.BuildYishuv, "site_a"));
        Assert.Empty(r.Rejected);
        return r;
    }

    // R-OBJ-002: calendar months, so the same month is "on time, as in history" (owner's card wording, #379).
    [Theory]
    [InlineData("1949-06-12", "1949-06-12", 0)]    // the same day
    [InlineData("1949-06-11", "1949-06-12", 0)]    // one day early, the same month: on time
    [InlineData("1949-06-30", "1949-06-12", 0)]    // later in the same month: on time
    [InlineData("1949-05-31", "1949-06-12", 1)]    // one day early, the month before: a month early
    [InlineData("1949-05-12", "1949-06-12", 1)]    // one month early, to the day
    [InlineData("1949-03-15", "1949-06-12", 3)]    // the slice's build date
    [InlineData("1948-05-15", "1949-06-12", 13)]
    [InlineData("1949-07-01", "1949-06-12", -1)]   // a month late
    [InlineData("1950-06-12", "1949-06-12", -12)]
    public void MonthsFromHistoryCountsCalendarMonths(string done, string reference, int months)
    {
        Assert.Equal(months, Objectives.MonthsFromHistory(D(done), D(reference)));
        Assert.Equal(Math.Max(0, months), Objectives.MonthsEarly(D(done), D(reference)));
    }

    // R-OBJ-002: 100 points, +5 a month early, the bonus capped at +60 (D-B6, D-B2); late costs nothing.
    [Theory]
    [InlineData("1949-06-12", 100)]
    [InlineData("1949-06-01", 100)]
    [InlineData("1949-09-15", 100)]   // late
    [InlineData("1949-05-31", 105)]
    [InlineData("1948-07-15", 155)]   // 11 months early
    [InlineData("1948-06-15", 160)]   // 12 months early: the cap
    [InlineData("1948-05-15", 160)]   // 13 months early: still the cap
    [InlineData("0001-01-01", 160)]   // a gap whose bonus would overflow an int is still the cap
    public void PointsAreTheBaseAndTheCappedBonus(string done, int points) =>
        Assert.Equal(points, Objectives.Points(Rules, D(done), D("1949-06-12")));

    // R-OBJ-001, R-OBJ-002
    [Fact]
    public void AnObjectiveIsRecordedOnceAndScoredOnce()
    {
        var c = Load(gdd: true);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var why = new List<Cause>();
        Assert.True(Objectives.Record(c, s, c.Rules.Mvp!, "first_goal", D("1949-04-15"), why));
        Assert.Equal(D("1949-04-15"), Assert.Single(s.Objectives).Value);
        Assert.Equal(110, s.Score);   // 2 months early (April to June)
        Assert.Contains(why, x => x.System == "events" && x.Message == "First goal: completed, 110 points");

        var hash = s.Hash();
        Assert.False(Objectives.Record(c, s, c.Rules.Mvp!, "first_goal", D("1949-05-15"), why));
        Assert.Equal(hash, s.Hash());
    }

    // R-OBJ-001: the legacy loop records nothing.
    [Fact]
    public void TheLegacyLoopRecordsNoObjective()
    {
        var c = Load(gdd: false);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var hash = s.Hash();
        Assert.False(Objectives.Record(c, s, c.Rules.Mvp!, "first_goal", D("1949-04-15"), new List<Cause>()));
        Assert.Equal(hash, s.Hash());
    }

    // R-OBJ-001, R-OBJ-002, R-OBJ-003: the objective completes on the turn its site is built, with that turn's end date, and
    // its card fires at the end of the same turn, so the player sees it at the start of the next one.
    [Fact]
    public void TheSiteBuiltCompletesTheObjectiveAndFiresTheCardOnce()
    {
        var c = Load(gdd: true);
        var r = StartBuilding(c);
        var turns = 1;
        while (r.State.Sites["site_a"].BuiltOn == null)
        {
            Assert.Empty(r.State.Objectives);
            Assert.Equal(0, r.State.Score);
            Assert.DoesNotContain("first_card", r.State.FiredEvents);
            r = Step(c, r.State);
            Assert.True(++turns <= 3, "the fixture builds in 3 turns");
        }
        var built = r.State.Sites["site_a"].BuiltOn!.Value;
        Assert.Equal(r.State.Date, built);
        Assert.Equal(built, Assert.Single(r.State.Objectives).Value);
        Assert.Equal(Objectives.Points(c.Rules.Mvp!, built, D("1949-06-12")), r.State.Score);
        Assert.Contains("first_card", r.State.PendingEvents);
        Assert.Contains(r.Why, x => x.System == "events" && x.Target == "first_goal");

        var score = r.State.Score;
        var s = Step(c, r.State, new ChooseEventOption("first_card", "continue")).State;
        for (int i = 0; i < 4; i++)
        {
            var next = Step(c, s);
            Assert.DoesNotContain("first_card", next.State.PendingEvents);
            Assert.DoesNotContain(next.Why, x => x.Target == "first_card" || x.Target == "first_goal");
            s = next.State;
        }
        Assert.Equal(score, s.Score);
    }

    // R-OBJ-002: a turn that crosses an era boundary scores with the rules of the date it starts from (docs/determinism.md,
    // "Rules per era"), not those of its end date. A turn that reaches the next era's start ends on that day, so its end
    // date is in the next era. The fixture's build ends on the turn 29 May to 12 June 1948, and a later era from 12 June 1948
    // sets objective_points to 1.
    [Fact]
    public void ATurnAcrossAnEraBoundaryScoresWithTheRulesItStartedIn()
    {
        const string eras = @"
eras:
  - id: founding
    name: The Founding
    from: 1948-05-15
    to: 1948-06-12
    systems: { loop: gdd }
  - id: later
    name: A Later Chapter
    from: 1948-06-12
    to: 1957-01-01
    systems: { loop: gdd }
";
        var o = Fixture.Gdd(new Dictionary<string, string> { ["eras.yaml"] = eras, ["objectives.yaml"] = ObjectivesYaml });
        var card = Card.Replace("from: 1948-05-15", "from: 1948-06-12");   // an event may not span an era boundary
        var dir = Fixture.Write(o, new Dictionary<string, string> { ["first_card.yaml"] = card });
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dir, "rules"));
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "rules", "later.yaml"), "mvp: { objective_points: 1 }\n");
        var c = ContentLoader.Load(dir);
        Assert.Equal(1, c.RulesAt(D("1948-06-12")).Mvp!.ObjectivePoints);

        var r = StartBuilding(c);
        var from = r.State.Date;
        while (r.State.Sites["site_a"].BuiltOn == null) { from = r.State.Date; r = Step(c, r.State); }
        var built = r.State.Sites["site_a"].BuiltOn!.Value;
        Assert.Equal((D("1948-05-29"), D("1948-06-12")), (from, built));   // the turn that reaches an era's start ends on it
        Assert.Equal(160, r.State.Score);   // 100 and the capped bonus; the later era would give 1 + 60
    }

    // R-OBJ-001: a build that is stopped completes nothing.
    [Fact]
    public void AStoppedBuildCompletesNothing()
    {
        var c = Load(gdd: true);
        var r = StartBuilding(c);
        var unit = r.State.Units.Keys.Single();
        var s = Step(c, r.State, new StopWork(unit)).State;
        for (int i = 0; i < 6; i++) s = Step(c, s).State;
        Assert.Empty(s.Objectives);
        Assert.Equal(0, s.Score);
        Assert.DoesNotContain("first_card", s.FiredEvents);
    }

    // R-OBJ-003: in the legacy loop no objective is recorded, so the card never fires, however long the game runs.
    [Fact]
    public void TheCardNeverFiresInTheLegacyLoop()
    {
        var c = Load(gdd: false);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        for (int i = 0; i < 20 && s.GameOver == null; i++)
        {
            s = Step(c, s).State;
            Assert.DoesNotContain("first_card", s.FiredEvents);
        }
    }

    // R-OBJ-003: an unanswered card resolves to its one option without drawing from the RNG (Phase 1 adds no draw).
    [Fact]
    public void AnUnansweredCardIsDelegatedWithoutADraw()
    {
        var c = Load(gdd: true);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Objectives.Record(c, s, c.Rules.Mvp!, "first_goal", s.Date, new List<Cause>());
        s = Step(c, s).State;
        Assert.Contains("first_card", s.PendingEvents);

        var delegated = Step(c, s).State;
        var answered = Step(c, s, new ChooseEventOption("first_card", "continue")).State;
        Assert.Equal("continue", delegated.ChosenOptions["first_card"]);
        Assert.Empty(delegated.PendingEvents);
        Assert.Equal(answered.RngState, delegated.RngState);
    }

    // R-OBJ-001: a save holds the orders only, so the objective and the score come back by replay.
    [Fact]
    public void TheObjectiveAndScoreSurviveASave()
    {
        var c = Load(gdd: true);
        var g = GameSession.New(c, 1, Mode.Historical);
        Assert.Empty(g.Step(new Command[] { new Train("town_a", "halutz", 1) }).Rejected);
        for (int i = 0; i < 10 && g.State.Units.Count == 0; i++) g.Step(new Command[0]);
        var id = Assert.Single(g.State.Units.Keys);   // the cohort took an id first; the unit takes the next
        Assert.Empty(g.Step(new Command[] { new PioneerWork(id, Projects.BuildYishuv, "site_a") }).Rejected);
        for (int i = 0; i < 10 && g.State.Objectives.Count == 0; i++) g.Step(new Command[0]);
        Assert.Single(g.State.Objectives);

        var loaded = GameSession.Load(c, g.Save.Write(), out _).State;
        Assert.Equal(g.State.Hash(), loaded.Hash());
        Assert.Equal(g.State.Objectives, loaded.Objectives);
        Assert.Equal(g.State.Score, loaded.Score);
    }

    // The real content: objective 3 is three parts. Each names its condition, sourced reference date and card (#791).
    [Fact]
    public void TheInfrastructureObjectiveHasItsThreeParts()
    {
        var parts = Real.Objectives.Where(x => x.PartOf == "establish_key_infrastructure").ToList();
        Assert.Equal(new[] { "burma_road", "railway", "yishuvim" }, parts.Select(x => x.Id));
        Assert.Equal("burma_road", parts.Single(x => x.Id == "burma_road").CompletesWhen!.CorridorComplete);
        Assert.Equal("tlv_haifa_rail", parts.Single(x => x.Id == "railway").CompletesWhen!.CorridorComplete);
        Assert.Equal(new[] { "mefalsim", "beit_kama", "reim", "gilat", "kiryat_shmona", "beit_shemesh" },
            parts.Single(x => x.Id == "yishuvim").CompletesWhen!.SitesBuilt);
        Assert.Equal(new DateTime(1948, 12, 7), parts.Single(x => x.Id == "burma_road").ReferenceDate);
        Assert.Equal(new DateTime(1949, 9, 20), parts.Single(x => x.Id == "railway").ReferenceDate);
        Assert.Equal(new DateTime(1950, 12, 15), parts.Single(x => x.Id == "yishuvim").ReferenceDate);
        Assert.All(parts, x => Assert.NotEmpty(x.Sources));
        Assert.All(parts, x =>
        {
            var card = Real.Events.Single(e => e.Id == x.Card);
            Assert.Equal((x.Id, "soft"), (card.Objective, card.Anchor));
            Assert.True(Assert.Single(card.Options).Historical);
        });
    }

    // The real content: the absorb objective as #738 decides it (Q-ABS-1's 1,000,000; Q-ABS-2's 10% and 40%), its reference
    // date Ingathering of the Exiles Day, and its card (#1039). Nothing else pins these values.
    [Fact]
    public void TheAbsorbObjectiveIsTheMillionMilestone()
    {
        var o = Real.Objectives.Single(x => x.Id == "absorb_immigrants");
        var a = o.CompletesWhen!.Absorbed!;
        Assert.Equal((1_000_000L, 0.10m, 0.40m), (a.PopulationAtLeast, a.OverflowAtMost!.Value, a.EmployedAtLeast));
        Assert.Equal(new DateTime(1949, 12, 20), o.ReferenceDate);
        Assert.Equal("absorb_immigrants", o.Card);
        Assert.Equal("absorb_immigrants", Real.Events.Single(e => e.Id == o.Card).Objective);
    }

    // R-OBJ-005, R-OBJ-006 in the golden run: the reference player builds the last required yishuv on 15 July 1951, so
    // the yishuvim part records and its card fires. Neither corridor finishes in the run, so the objective's base points
    // have not yet been paid.
    [Fact]
    public void TheGoldenRunCompletesTheYishuvimPartWhenTheLastSiteIsBuilt()
    {
        var s = GoldenRun.Play(Real, GoldenRun.Until1952);
        var built = Real.Objectives.Single(x => x.Id == "yishuvim").CompletesWhen!.SitesBuilt
            .Max(id => s.Sites[id].BuiltOn!.Value);
        Assert.Equal(new DateTime(1951, 7, 15), built);
        Assert.Equal(built, s.Objectives["yishuvim"]);
        Assert.Equal(0, s.Score);
        Assert.Equal("continue", s.ChosenOptions["first_new_yishuv"]);
    }

    // The legacy loop (a fixture since the cutover) loads the real objective and its card (they are content), and never
    // fires the card.
    [Fact]
    public void TheRealCardNeverFiresInTheLegacyLoop()
    {
        var root = RealContentTests.ContentRoot();
        var legacy = ContentLoader.Load(Fixture.Write(
            new Dictionary<string, string> { ["objectives.yaml"] = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "objectives.yaml")) },
            new Dictionary<string, string>
            {
                ["first_new_yishuv.yaml"] = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "events", "first_new_yishuv.yaml")),
                ["absorb_immigrants.yaml"] = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "events", "absorb_immigrants.yaml")),   // objectives.yaml's second card (#1039)
                ["railway_complete.yaml"] = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "events", "railway_complete.yaml")),
                ["burma_road_complete.yaml"] = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "events", "burma_road_complete.yaml")),
            }));
        Assert.False(legacy.IsGdd);
        Assert.Equal(4, legacy.Objectives.Count);
        var s = Simulation.NewGame(legacy, GoldenRun.Seed, Mode.Historical);
        for (int i = 0; i < 40 && s.GameOver == null; i++)
        {
            s = Step(legacy, s).State;
            Assert.DoesNotContain("first_new_yishuv", s.FiredEvents);
            Assert.DoesNotContain("absorb_immigrants", s.FiredEvents);
            Assert.DoesNotContain("railway_complete", s.FiredEvents);
            Assert.DoesNotContain("burma_road_complete", s.FiredEvents);
        }
        Assert.Empty(s.Objectives);
        Assert.Equal(0, s.Score);
    }

    // R-OBJ-002: the point values are rules.yaml design values (DesignValuesTests), never in objectives.yaml.
    [Fact]
    public void NoPointValueIsInObjectivesYaml()
    {
        var objectives = System.IO.File.ReadAllText(System.IO.Path.Combine(RealContentTests.ContentRoot(), "objectives.yaml"));
        Assert.DoesNotMatch(@"(?m)^[^#\n]*\b\w*(points|bonus)\w*\s*:", objectives);   // no such key outside a comment
    }

    [Fact]
    public void ContentWithoutObjectivesHasNone() => Assert.Empty(ContentLoader.Load(Fixture.Write()).Objectives);

    // R-OBJ-005: a corridor part is open while even one segment is absent and records on the first end turn after every
    // segment is built (R-COR-003).
    [Fact]
    public void TheRailwayPartCompletesWithItsCorridor()
    {
        // road_a has two segments in the fixture, so this also pins the gap mutation without changing the real rail content.
        var c = LoadParts(PartsYaml.Replace("corridor_complete: rail_a", "corridor_complete: road_a"));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Corridors["road_a"] = new CorridorWork("road_a", new SegmentWork?[]
        {
            new SegmentWork(null, 3, D("1948-06-15")), null,
        });
        Objectives.Complete(c, s, c.Rules.Mvp!, D("1948-06-15"), new List<Cause>());
        Assert.DoesNotContain("railway", s.Objectives.Keys);

        s.Corridors["road_a"] = new CorridorWork("road_a", new SegmentWork?[]
        {
            new SegmentWork(null, 3, D("1948-06-15")), new SegmentWork(null, 3, D("1948-06-15")),
        });
        Objectives.Complete(c, s, c.Rules.Mvp!, D("1948-06-15"), new List<Cause>());
        Assert.Equal(D("1948-06-15"), s.Objectives["railway"]);
    }

    // R-OBJ-005: every listed site is required; building a proper subset does not complete the part.
    [Fact]
    public void TheYishuvimPartNeedsEveryListedSite()
    {
        var c = LoadParts();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Sites["site_a"] = SiteWork.Built("site_a", D("1948-06-15"));
        Objectives.Complete(c, s, c.Rules.Mvp!, D("1948-06-15"), new List<Cause>());
        Assert.DoesNotContain("yishuvim", s.Objectives.Keys);

        s.Sites["site_b"] = SiteWork.Built("site_b", D("1948-06-15"));
        Objectives.Complete(c, s, c.Rules.Mvp!, D("1948-06-15"), new List<Cause>());
        Assert.Equal(D("1948-06-15"), s.Objectives["yishuvim"]);
    }

    // R-OBJ-006: each part adds its timing bonus, but objective_points is added once, with the last part. Rechecking the
    // completed group changes neither its records nor its score.
    [Fact]
    public void ThePointsArePaidOnce()
    {
        var c = LoadParts();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var done = D("1948-06-15");
        s.Corridors["rail_a"] = new CorridorWork("rail_a", new SegmentWork?[] { new SegmentWork(null, 2, done) });
        s.Corridors["road_a"] = new CorridorWork("road_a", new SegmentWork?[]
        {
            new SegmentWork(null, 3, done), new SegmentWork(null, 3, done),
        });
        s.Sites["site_a"] = SiteWork.Built("site_a", done);
        s.Sites["site_b"] = SiteWork.Built("site_b", done);

        Objectives.Complete(c, s, c.Rules.Mvp!, done, new List<Cause>());
        Assert.Equal(3, s.Objectives.Count);
        Assert.Equal(250, s.Score);   // base 100 once; bonuses 30 (road), 60 (railway) and 60 (yishuvim)
        var hash = s.Hash();
        Objectives.Complete(c, s, c.Rules.Mvp!, done.AddMonths(1), new List<Cause>());
        Assert.Equal(hash, s.Hash());
    }

    // R-OBJ-006: completing parts on different turns pays no base early, in every possible completion order.
    [Theory]
    [InlineData("railway", "road", "yishuvim")]
    [InlineData("railway", "yishuvim", "road")]
    [InlineData("road", "railway", "yishuvim")]
    [InlineData("road", "yishuvim", "railway")]
    [InlineData("yishuvim", "railway", "road")]
    [InlineData("yishuvim", "road", "railway")]
    public void OnlyTheLastPartPaysTheBaseInAnyOrder(string first, string second, string last)
    {
        var c = LoadParts();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var why = new List<Cause>();
        var ids = new[] { first, second, last };
        for (int i = 0; i < ids.Length; i++)
        {
            var done = D("1951-01-15").AddMonths(i); // late for every part, so no timing bonus
            Assert.True(Objectives.Record(c, s, c.Rules.Mvp!, ids[i], done, why));
            Assert.Equal(done, s.Objectives[ids[i]]);
            Assert.Equal(i == 2 ? 100 : 0, s.Score);
            var hash = s.Hash();
            Assert.False(Objectives.Record(c, s, c.Rules.Mvp!, ids[i], done.AddMonths(1), why));
            Assert.Equal(hash, s.Hash());
        }
    }

    // R-OBJ-003, R-OBJ-005: all three newly completed parts make their cards pending that turn, and resolving them keeps
    // them from firing again.
    [Fact]
    public void EachCardFiresOnceTheTurnAfter()
    {
        var c = LoadParts();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Corridors["rail_a"] = new CorridorWork("rail_a", new SegmentWork?[] { new SegmentWork(null, 2, s.Date) });
        s.Corridors["road_a"] = new CorridorWork("road_a", new SegmentWork?[]
        {
            new SegmentWork(null, 3, s.Date), new SegmentWork(null, 3, s.Date),
        });
        s.Sites["site_a"] = SiteWork.Built("site_a", s.Date);
        s.Sites["site_b"] = SiteWork.Built("site_b", s.Date);

        var first = Step(c, s);
        Assert.Equal(new[] { "railway_card", "road_card", "yishuvim_card" }, first.State.PendingEvents.OrderBy(x => x));
        var resolved = Step(c, first.State,
            new ChooseEventOption("railway_card", "continue"), new ChooseEventOption("road_card", "continue"),
            new ChooseEventOption("yishuvim_card", "continue"));
        Assert.Empty(resolved.State.PendingEvents);
        var next = Step(c, resolved.State);
        Assert.Empty(next.State.PendingEvents);
        Assert.Equal(3, next.State.Objectives.Count);
    }

    // R-OBJ-005: every part carries a sourced in-window reference date; its condition names content of the right kind.
    [Theory]
    [InlineData("    sources: [{ url: https://example.org/railway, checked: 2026-10-20 }]\n", "", "objectives[railway].sources", "needs a source")]
    [InlineData("    reference_date: 1949-09-20", "    reference_date: 1957-01-01", "objectives[railway].reference_date", "inside the MVP")]
    [InlineData("corridor_complete: rail_a", "corridor_complete: nowhere", "objectives[railway].completes_when.corridor_complete", "unknown corridor")]
    [InlineData("sites_built: [site_a, site_b]", "sites_built: [site_a, site_a]", "objectives[yishuvim].completes_when.sites_built[1]", "duplicate site")]
    public void EveryPartHasASourcedDate(string from, string to, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => LoadParts(PartsYaml.Replace(from, to)));
        Assert.Equal("objectives.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // R-OBJ-005: grouping changes the base-score contract, so standalone conditions cannot masquerade as parts.
    [Theory]
    [InlineData("site_built: site_a")]
    [InlineData("absorbed: { population_at_least: 1500, overflow_at_most: 0.10, employed_at_least: 0.40 }")]
    public void PartsRejectStandaloneConditions(string condition)
    {
        var yaml = PartsYaml.Replace("corridor_complete: rail_a", condition)
            .Replace("reference_date: 1949-09-20", "reference_date: 1949-06-12");
        var ex = Assert.Throws<ContentException>(() => LoadParts(yaml));
        Assert.Equal("objectives.yaml", ex.File);
        Assert.Equal("objectives[railway].completes_when", ex.Field);
        Assert.Contains("part_of is set", ex.Message);
        Assert.Contains("corridor_complete or sites_built", ex.Message);
    }

    // R-OBJ-001, R-OBJ-004, R-OBJ-005: a missing part_of must not turn a part condition into a standalone base award.
    [Theory]
    [InlineData("corridor_complete: rail_a")]
    [InlineData("sites_built: [site_a, site_b]")]
    public void StandaloneObjectivesRejectPartConditions(string condition)
    {
        var yaml = PartsYaml.Replace("  - id: railway\n    part_of: infrastructure", "  - id: railway")
            .Replace("corridor_complete: rail_a", condition);
        var ex = Assert.Throws<ContentException>(() => LoadParts(yaml));
        Assert.Equal("objectives.yaml", ex.File);
        Assert.Equal("objectives[railway].completes_when", ex.Field);
        Assert.Contains("part_of is not set", ex.Message);
        Assert.Contains("site_built or absorbed", ex.Message);
    }

    // ---- R-OBJ-004, the "absorb immigrants" objective (D-B4; owner, 18 October 2026, #738: option (a)) ----

    /// <summary>The fixture's absorb objective: the fixture starts with 1000 people, so its gate is 1500.</summary>
    const string AbsorbYaml = "objectives:\n  - { id: first_goal, name: First goal, completes_when: { absorbed: { population_at_least: 1500, overflow_at_most: 0.10, employed_at_least: 0.40 } }, reference_date: 1949-06-12, card: first_card }\n";

    static Content LoadAbsorb(string objectives = AbsorbYaml) => Load(gdd: true, new Dictionary<string, string> { ["objectives.yaml"] = objectives });

    // R-OBJ-004: town_a houses 10 units and its people are civilians only, so overflow = civilians - 10. Each row is just
    // inside or just outside one bound; the shares are "at or below" and "at or above".
    [Theory]
    [InlineData(1500, 11, 5, 0, true)]     // overflow 1 <= 1.1, employed 5 >= 4.4
    [InlineData(1499, 11, 5, 0, false)]    // below the population gate
    [InlineData(1500, 10, 4, 0, true)]     // no overflow, employed 4 = 40% of 10: on the bound
    [InlineData(1500, 10, 3, 0, false)]    // employed 30%
    [InlineData(1500, 12, 5, 0, false)]    // overflow 2 > 1.2
    [InlineData(1500, 11, 5, 7, true)]     // refugees waiting do not block it (the owner dropped that clause, #738)
    public void AbsorptionCompletesWhenTheShareIsHousedAndEmployed(int population, int civilians, int employed, int refugees, bool done)
    {
        var c = LoadAbsorb();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Population = new Dictionary<string, double> { ["veterans"] = population };
        s.Cities["town_a"] = new City("town_a", 10, 8, civilians, employed, refugees);
        Objectives.Complete(c, s, c.Rules.Mvp!, D("1949-04-15"), new List<Cause>());
        Assert.Equal(done, s.Objectives.ContainsKey("first_goal"));
        Assert.Equal(done ? 110 : 0, s.Score);
    }

    // R-OBJ-004: overflow exactly on the bound completes it. With capacity 10 no integer overflow equals 10% of the people
    // housed, so town_a here houses 9: 10 civilians, overflow 1 = 10% of 10 (review on #1019).
    [Fact]
    public void OverflowOnTheBoundCompletes()
    {
        var c = LoadAbsorb();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Population = new Dictionary<string, double> { ["veterans"] = 1500 };
        s.Cities["town_a"] = new City("town_a", 9, 8, 10, 4, 0);
        Objectives.Complete(c, s, c.Rules.Mvp!, D("1949-04-15"), new List<Cause>());
        Assert.True(s.Objectives.ContainsKey("first_goal"));
    }

    // R-OBJ-004 (#738): Q-ABS-2 as first recorded (no refugees waiting, overflow <= 10%, employed >= 40%) already
    // holds at the end of the real game's first turn, with nobody absorbed: the census start values are 41% employed with no
    // overflow (#738, issuecomment-6242964308). The population gate keeps the objective open.
    [Fact]
    public void TheStartValuesDoNotCompleteItOnTheFirstTurn()
    {
        var c = Real;
        var s = Step(c, Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical)).State;
        Assert.Equal(0, s.Cities.Values.Sum(x => x.Refugees));
        Assert.Equal(0, s.Cities.Values.Sum(x => Cities.Overflow(s, x)));
        Assert.True(Cities.EmployedShare(s) >= 0.40, "precondition: the old condition holds on turn 1");
        Assert.DoesNotContain("absorb_immigrants", s.Objectives.Keys);
    }

    // R-OBJ-004 (#738): arrivals with no housing or jobs built never complete it. The historical run with no orders
    // passes 1,000,000 people, but its cities are then far below 40% employed.
    [Fact]
    public void ArrivalAloneDoesNotComplete()
    {
        var c = Real;
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var passed = false;
        while (s.GameOver == null)
        {
            s = Step(c, s).State;
            passed |= s.TotalPopulation >= 1_000_000;
            Assert.DoesNotContain("absorb_immigrants", s.Objectives.Keys);
        }
        Assert.True(passed, "precondition: the run passes 1,000,000 people");
    }

    // R-OBJ-004 with R-OBJ-003: once the condition holds, the objective is recorded at the end of that turn and its card is
    // pending for the next; it neither records nor fires again.
    [Fact]
    public void TheAbsorbCardFiresOnceTheTurnAfter()
    {
        var c = LoadAbsorb(AbsorbYaml.Replace("population_at_least: 1500", "population_at_least: 1000"));   // the start: 60% employed, no overflow
        var r = Step(c, Simulation.NewGame(c, 1, Mode.Historical));
        Assert.Equal(r.State.Date, Assert.Single(r.State.Objectives).Value);
        Assert.Contains("first_card", r.State.PendingEvents);
        var score = r.State.Score;
        var s = Step(c, r.State, new ChooseEventOption("first_card", "continue")).State;
        for (int i = 0; i < 3; i++)
        {
            s = Step(c, s).State;
            Assert.DoesNotContain("first_card", s.PendingEvents);
        }
        Assert.Single(s.Objectives);
        Assert.Equal(score, s.Score);
    }

    static string GddRules(string from, string to) => Fixture.Gdd()["rules.yaml"].Replace(from, to);

    static string TwoOptions => Card + "  - id: refuse\n    text: Refuse\n    alternative: { basis: design }\n";

    static string Objective(string from, string to) => ObjectivesYaml.Replace(from, to);

    public static IEnumerable<object[]> BadContent() => new List<object[]>
    {
        new object[] { "objectives.yaml", Objective("id: first_goal", "id: First"), Card, "objectives.yaml", "objectives[First].id", "must be lowercase" },
        new object[] { "objectives.yaml", ObjectivesYaml + ObjectivesYaml.Replace("objectives:\n", ""), Card, "objectives.yaml", "objectives[first_goal].id", "duplicate id" },
        new object[] { "objectives.yaml", Objective("name: First goal, ", ""), Card, "objectives.yaml", "objectives[first_goal].name", "required" },
        new object[] { "objectives.yaml", Objective("completes_when: { site_built: site_a }, ", ""), Card, "objectives.yaml", "objectives[first_goal].completes_when", "required" },
        new object[] { "objectives.yaml", Objective("site_built: site_a", "site_built: site_a, absorbed: { population_at_least: 1, overflow_at_most: 0.1, employed_at_least: 0.4 }"), Card, "objectives.yaml", "objectives[first_goal].completes_when", "exactly one of" },
        new object[] { "objectives.yaml", AbsorbYaml.Replace("population_at_least: 1500", "population_at_least: 0"), Card, "objectives.yaml", "objectives[first_goal].completes_when.absorbed.population_at_least", "above 0" },
        new object[] { "objectives.yaml", AbsorbYaml.Replace("overflow_at_most: 0.10, ", ""), Card, "objectives.yaml", "objectives[first_goal].completes_when.absorbed.overflow_at_most", "required" },
        new object[] { "objectives.yaml", AbsorbYaml.Replace("overflow_at_most: 0.10", "overflow_at_most: 1.5"), Card, "objectives.yaml", "objectives[first_goal].completes_when.absorbed.overflow_at_most", "share from 0 to 1" },
        new object[] { "objectives.yaml", AbsorbYaml.Replace("employed_at_least: 0.40", "employed_at_least: 0"), Card, "objectives.yaml", "objectives[first_goal].completes_when.absorbed.employed_at_least", "share above 0" },
        new object[] { "objectives.yaml", Objective("reference_date: 1949-06-12, ", ""), Card, "objectives.yaml", "objectives[first_goal].reference_date", "required" },
        new object[] { "objectives.yaml", Objective("card: first_card", "card: no_card"), Card, "objectives.yaml", "objectives[first_goal].card", "unknown event 'no_card'" },
        new object[] { "objectives.yaml", ObjectivesYaml, Card.Replace("objective: first_goal\n", ""), "events/first_card.yaml", "events[first_card].objective", "must be 'first_goal'" },
        new object[] { "objectives.yaml", ObjectivesYaml, TwoOptions, "events/first_card.yaml", "events[first_card].options", "exactly one option" },
        new object[] { "objectives.yaml", "objectives: []\n", Card, "events/first_card.yaml", "events[first_card].objective", "unknown objective 'first_goal'" },
        new object[] { "objectives.yaml", Objective("site_built: site_a", "site_built: nowhere"), Card, "objectives.yaml", "objectives[first_goal].completes_when.site_built", "unknown site 'nowhere'" },
        new object[] { "objectives.yaml", Objective("reference_date: 1949-06-12", "reference_date: 1949-06-08"), Card, "objectives.yaml", "objectives[first_goal].reference_date", "must be 1949-06-12, the founding date of site 'site_a'" },
        new object[] { "sites.yaml", Fixture.GddSites.Replace("kinds: [yishuv]", "kinds: [field]").Replace("    founded: 1949-06-12\n", ""), Card, "objectives.yaml", "objectives[first_goal].completes_when.site_built", "cannot be built as a yishuv" },
        new object[] { "rules.yaml", GddRules("  objective_points: 100\n", ""), Card, "rules.yaml", "mvp.objective_points", "required when eras.yaml systems.loop is gdd" },
        new object[] { "rules.yaml", GddRules("objective_bonus_per_month: 5", "objective_bonus_per_month: -5"), Card, "rules.yaml", "mvp.objective_bonus_per_month", "must be >= 0" },
        new object[] { "rules.yaml", GddRules("objective_bonus_cap: 60", "objective_bonus_cap: -1"), Card, "rules.yaml", "mvp.objective_bonus_cap", "must be >= 0" },
    };

    // R-OBJ-001: an objective names a site whose founding date is its reference date; R-OBJ-003: an objective and its card
    // name each other; R-OBJ-002: the point values are required in the GDD loop.
    [Theory]
    [MemberData(nameof(BadContent))]
    public void BadObjectiveContentFailsNamingTheFileAndField(string file, string text, string card, string errorFile, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(gdd: true, new Dictionary<string, string> { [file] = text }, card));
        Assert.Equal(errorFile, ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    /// <summary>#1189: the Objectives screen's reading, one row per objective in content order, with its name, whether it is done,
    /// the player's date (none until it is done) and the historical date. Pure: the state's hash is the same afterwards.</summary>
    [Fact]
    public void StatusListsEveryObjectiveDoneOrNotWithBothDates()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        var hash = s.Hash();
        var open = Objectives.Status(Real, s);
        Assert.Equal(hash, s.Hash());
        Assert.Equal(Real.Objectives.Select(o => (o.Id, o.Name, o.ReferenceDate)), open.Select(o => (o.Id, o.Name, o.Historical)));
        Assert.All(open, o => { Assert.False(o.Done); Assert.Null(o.Completed); });

        var first = Real.Objectives[0];
        var date = first.ReferenceDate.AddMonths(-2);
        Assert.True(Objectives.Record(Real, s, Real.RulesAt(s.Date).Mvp!, first.Id, date, new List<Cause>()));
        hash = s.Hash();
        var after = Objectives.Status(Real, s);
        Assert.Equal(hash, s.Hash());
        Assert.True(after[0].Done);
        Assert.Equal(date, after[0].Completed);
        Assert.Equal(first.ReferenceDate, after[0].Historical);
        Assert.All(after.Skip(1), o => { Assert.False(o.Done); Assert.Null(o.Completed); });
    }

    /// <summary>#1187: an objective's Hebrew name loads beside the English, agent-checked until a native read (#564).</summary>
    [Fact]
    public void TheHebrewNameLoadsBesideTheEnglish()
    {
        var c = Load(true, new Dictionary<string, string> { ["objectives.yaml"] = ObjectivesYaml.Replace("name: First goal,", "name: First goal, name_he: \u05de\u05d8\u05e8\u05d4 \u05e8\u05d0\u05e9\u05d5\u05e0\u05d4, name_he_review: agent-checked,") });
        var o = c.Objectives.Single(x => x.Id == "first_goal");
        Assert.Equal(("First goal", "\u05de\u05d8\u05e8\u05d4 \u05e8\u05d0\u05e9\u05d5\u05e0\u05d4", HebrewReview.AgentChecked, true), (o.Name, o.NameHe, o.NameHeReview, o.NameHeReviewGiven));
    }

    /// <summary>#1187: the Hebrew name is Hebrew letters, and the review flag is agent-checked or absent.</summary>
    [Theory]
    [InlineData("name_he: First goal", "name_he", "Hebrew letters")]
    [InlineData("name_he: \u05de\u05d8\u05e8\u05d4 \u05e8\u05d0\u05e9\u05d5\u05e0\u05d4, name_he_review: reviewed", "name_he_review", "agent-checked")]
    [InlineData("name_he_review: agent-checked", "name_he", "required")]
    public void BadHebrewNameIsAContentError(string insert, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(true, new Dictionary<string, string> { ["objectives.yaml"] = ObjectivesYaml.Replace("name: First goal,", $"name: First goal, {insert},") }));
        Assert.Equal("objectives.yaml", ex.File);
        Assert.Equal($"objectives[first_goal].{field}", ex.Field);
        Assert.Contains(message, ex.Message);
    }
}
