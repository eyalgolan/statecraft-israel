using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The GDD slice's objectives (Phase 1 PR 6): an objective is recorded once, at the end of the turn its site is built,
/// and scores once (R-OBJ-001), with the owner's points and timing bonus (R-OBJ-002; D-B6 and D-B2, #456), and its card fires
/// only once it is recorded, never in the legacy loop (R-OBJ-003). The first objective is the first new yishuv, Mefalsim (#378).</summary>
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

    // The real content: the first new yishuv is Mefalsim, and its reference date is the site's founding date (#378).
    [Fact]
    public void TheObjectiveIsTheFirstNewYishuvAtMefalsim()
    {
        var o = Assert.Single(Real.Objectives);
        Assert.Equal(("first_new_yishuv", "mefalsim", "first_new_yishuv"), (o.Id, o.CompletesWhen!.SiteBuilt, o.Card));
        Assert.Equal(Real.Sites.Single(x => x.Id == "mefalsim").Founded, o.ReferenceDate);
        Assert.Equal(new DateTime(1949, 6, 12), o.ReferenceDate);
        var card = Real.Events.Single(e => e.Id == o.Card);
        Assert.Equal(("first_new_yishuv", "soft"), (card.Objective, card.Anchor));
        Assert.True(Assert.Single(card.Options).Historical);
        Assert.Empty(card.Tags);
    }

    // R-OBJ-001 to R-OBJ-003 in the golden run: the reference player builds Mefalsim on 15 June 1949 (#566), the month of its
    // 12 June 1949 founding, so the first new yishuv is on time and scores 100, and its card fires and is answered.
    [Fact]
    public void TheGoldenRunCompletesTheFirstNewYishuvOnTheBuildDate()
    {
        var s = GoldenRun.Play(Real, GoldenRun.Until1950);
        var built = s.Sites["mefalsim"].BuiltOn!.Value;
        Assert.Equal(new DateTime(1949, 6, 15), built);
        Assert.Equal(built, s.Objectives["first_new_yishuv"]);
        Assert.Equal(100, s.Score);
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
            new Dictionary<string, string> { ["first_new_yishuv.yaml"] = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "events", "first_new_yishuv.yaml")) }));
        Assert.False(legacy.IsGdd);
        Assert.Single(legacy.Objectives);
        var s = Simulation.NewGame(legacy, GoldenRun.Seed, Mode.Historical);
        for (int i = 0; i < 40 && s.GameOver == null; i++)
        {
            s = Step(legacy, s).State;
            Assert.DoesNotContain("first_new_yishuv", s.FiredEvents);
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

    static string GddRules(string from, string to) => Fixture.Gdd()["rules.yaml"].Replace(from, to);

    static string TwoOptions => Card + "  - id: refuse\n    text: Refuse\n";

    static string Objective(string from, string to) => ObjectivesYaml.Replace(from, to);

    public static IEnumerable<object[]> BadContent() => new List<object[]>
    {
        new object[] { "objectives.yaml", Objective("id: first_goal", "id: First"), Card, "objectives.yaml", "objectives[First].id", "must be lowercase" },
        new object[] { "objectives.yaml", ObjectivesYaml + ObjectivesYaml.Replace("objectives:\n", ""), Card, "objectives.yaml", "objectives[first_goal].id", "duplicate id" },
        new object[] { "objectives.yaml", Objective("name: First goal, ", ""), Card, "objectives.yaml", "objectives[first_goal].name", "required" },
        new object[] { "objectives.yaml", Objective("completes_when: { site_built: site_a }, ", ""), Card, "objectives.yaml", "objectives[first_goal].completes_when", "required" },
        new object[] { "objectives.yaml", Objective("reference_date: 1949-06-12, ", ""), Card, "objectives.yaml", "objectives[first_goal].reference_date", "required" },
        new object[] { "objectives.yaml", Objective("card: first_card", "card: no_card"), Card, "objectives.yaml", "objectives[first_goal].card", "unknown event 'no_card'" },
        new object[] { "objectives.yaml", ObjectivesYaml, Card.Replace("objective: first_goal\n", ""), "events/first_card.yaml", "events[first_card].objective", "must be 'first_goal'" },
        new object[] { "objectives.yaml", ObjectivesYaml, TwoOptions, "events/first_card.yaml", "events[first_card].options", "exactly one option" },
        new object[] { "objectives.yaml", "objectives: []\n", Card, "events/first_card.yaml", "events[first_card].objective", "unknown objective 'first_goal'" },
        new object[] { "objectives.yaml", Objective("site_built: site_a", "site_built: nowhere"), Card, "objectives.yaml", "objectives[first_goal].completes_when.site_built", "unknown site 'nowhere'" },
        new object[] { "objectives.yaml", Objective("reference_date: 1949-06-12", "reference_date: 1949-06-08"), Card, "objectives.yaml", "objectives[first_goal].reference_date", "must be 1949-06-12, the founding date of site 'site_a'" },
        new object[] { "sites.yaml", Fixture.GddSites.Replace("kinds: [yishuv]", "kinds: []"), Card, "objectives.yaml", "objectives[first_goal].completes_when.site_built", "cannot be built as a yishuv" },
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
}
