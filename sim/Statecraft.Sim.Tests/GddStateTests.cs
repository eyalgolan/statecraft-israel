using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The GDD v0.1 state (spec 2026-10-12 section 2.2): written into the hash only when set, so the legacy loop's
/// hash text, and every golden, is unchanged.</summary>
public class GddStateTests
{
    // R-LOOP-002
    [Fact]
    public void WithTheSwitchOffAPlayedGameWritesNoNewSection()
    {
        // Since the cutover the legacy loop is a fixture's (the real goldens run gdd): a legacy game played a year writes
        // none of the GDD sections.
        var c = ContentLoader.Load(Fixture.Write());
        var s = GoldenRun.Play(c, new DateTime(1949, 5, 15));
        Assert.True(s.Turn > 1);
        foreach (var label in new[] { "city{", "coh=", "units[", "train[", "next=", "obj{", "score=" }) Assert.DoesNotContain(label, s.Describe());
    }

    // R-LOOP-002
    [Fact]
    public void ALegacyStateHashesItsExactCanonicalText()
    {
        // The whole legacy text, byte for byte: a reordered, renamed or reformatted legacy section moves every golden, so it must
        // fail here first with the text in view. Doubles are IEEE bits (1.0 = 3FF0..., 0.5 = 3FE0..., 2.0 = 4000..., 0.25 = 3FD0...,
        // 1.5 = 3FF8..., 3.0 = 4008..., 4.0 = 4010...). No section the GDD loop adds appears.
        var d = new DateTime(1949, 3, 15);
        var s = new GameState
        {
            Date = d, RngState = 42, Mode = Mode.Open, Turn = 7,
            Indicators = { ["b"] = 0.5, ["a"] = 1.0 }, Policies = { ["p"] = 2.0 }, EffectContributions = { ["e"] = 0.25 },
            IndicatorShifts = { ["s"] = 1.5 }, Budget = { ["defence"] = 3.0 }, Population = { ["jews"] = 4.0 },
            ActiveSituations = { "war", "boom" }, Treasury = 1.0, Debt = 0.5, FxReserves = 2.0, Rung = 3, CeasefireWeeksLeft = 2,
            Mobilization = 0.25, CoalitionStability = 0.5, Attention = 3.0, FiredEvents = { "e2", "e1" },
            PendingEvents = { "e3" }, ChosenOptions = { ["e1"] = "a" }, Appointments = { ["pm"] = new Appointment("beni", new DateTime(1948, 5, 14)) },
            AppointedUnder = "gov1", Placements = { new Placement("camp", 3, -2, new DateTime(1949, 2, 15)) },
            MapActionsTurnDate = d, MapActionsThisTurn = 1, GameOver = "mvp_ended",
        };
        const string text = "1949-03-15;42;Open;7;ind{a=3FF0000000000000;b=3FE0000000000000;}pol{p=4000000000000000;}eff{e=3FD0000000000000;}"
            + "shift{s=3FF8000000000000;}bud{defence=4008000000000000;}pop{jews=4010000000000000;}sit[boom,war]"
            + "treasury=3FF0000000000000;debt=3FE0000000000000;fx=4000000000000000;rung=3;cf=2;mob=3FD0000000000000;stab=3FE0000000000000;"
            + "att=4008000000000000;fired[e1,e2]pending[e3]chosen{e1=a;}appt{pm=beni@1948-05-14;}under=gov1;map[camp@3,-2@1949-02-15;]"
            + "mapturn=1949-03-15:1;over=mvp_ended";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var expected = string.Concat(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text)).Select(b => b.ToString("x2")));
        Assert.Equal(expected, s.Hash());
    }

    // R-LOOP-002
    [Fact]
    public void AnEmptyStateWritesNoNewSection()
    {
        var text = new GameState().Describe();
        foreach (var label in new[] { "city{", "coh=", "units[", "train[", "site{", "next=", "obj{", "score=" }) Assert.DoesNotContain(label, text);
        Assert.EndsWith("over=", text);
    }

    // R-LOOP-002
    [Fact]
    public void EachNewSectionAppearsOnlyWhenSetAndMovesTheHash()
    {
        var s = new GameState();
        var seen = new HashSet<string> { s.Hash() };
        var steps = new List<(string label, Action<GameState> set)>
        {
            ("city{", x => x.Cities["tel_aviv"] = new City("tel_aviv", 10, 4, 6, 3, 2)),
            ("units[", x => x.Units.Add(1, new Unit(1, "halutz", -3, 19))),
            ("train[", x => x.Cohorts.Add(2, new Cohort(2, "recruit", "tel_aviv", 3, 2))),
            ("site{", x => x.Sites["mefalsim"] = new SiteWork("mefalsim", 1, 5, null)),
            ("corr{", x => x.Corridors["burma_road"] = new CorridorWork("burma_road", new SegmentWork?[] { null, new SegmentWork(1, 2, null) })),
            ("next=", x => x.NextId = 2),
            ("obj{", x => x.Objectives["first_new_yishuv"] = new DateTime(1949, 3, 15)),
            ("score=", x => x.Score = 5),
            ("scoreparts=", x => x.ScoreTimingBonuses = 5),
        };
        foreach (var (label, set) in steps)
        {
            Assert.DoesNotContain(label, s.Describe());
            set(s);
            Assert.Contains(label, s.Describe());
            Assert.True(seen.Add(s.Hash()), $"setting {label} did not move the hash");
        }
    }

    // R-LOOP-002
    [Fact]
    public void EveryFieldOfARecordReachesTheHash()
    {
        string H(City c) { var s = new GameState(); s.Cities[c.Id] = c; return s.Hash(); }
        string U(Unit u) { var s = new GameState(); s.Units.Add(u.Id, u); return s.Hash(); }
        var cities = new[] { new City("a", 1, 1, 1, 1, 1), new City("b", 1, 1, 1, 1, 1), new City("a", 2, 1, 1, 1, 1), new City("a", 1, 2, 1, 1, 1),
                             new City("a", 1, 1, 2, 1, 1), new City("a", 1, 1, 1, 2, 1), new City("a", 1, 1, 1, 1, 2) };
        Assert.Equal(cities.Length, new HashSet<string>(Array.ConvertAll(cities, H)).Count);
        var units = new[] { new Unit(1, "halutz", 0, 0), new Unit(2, "halutz", 0, 0), new Unit(1, "recruit", 0, 0), new Unit(1, "halutz", 1, 0), new Unit(1, "halutz", 0, 1),
                            new Unit(1, "halutz", 0, 0, 1), new Unit(1, "halutz", 0, 0, side: "israel"), new Unit(1, "halutz", 0, 0, strength: 1),
                            new Unit(1, "halutz", 0, 0, order: "hold"), new Unit(1, "halutz", 0, 0, order: "hold", orderTarget: "2") };
        Assert.Equal(units.Length, new HashSet<string>(Array.ConvertAll(units, U)).Count);
        string K(Cohort k) { var s = new GameState(); s.Cohorts.Add(k.Id, k); return s.Hash(); }
        var cohorts = new[] { new Cohort(1, "recruit", "a", 1, 1), new Cohort(2, "recruit", "a", 1, 1), new Cohort(1, "halutz", "a", 1, 1),
                              new Cohort(1, "recruit", "b", 1, 1), new Cohort(1, "recruit", "a", 2, 1), new Cohort(1, "recruit", "a", 1, 2),
                              new Cohort(1, "recruit", "a", 1, 1, arms: 1) };
        Assert.Equal(cohorts.Length, new HashSet<string>(Array.ConvertAll(cohorts, K)).Count);
        string W(SiteWork w) { var s = new GameState(); s.Sites[w.Site] = w; return s.Hash(); }
        var works = new[] { new SiteWork("a", 1, 1, null), new SiteWork("b", 1, 1, null), new SiteWork("a", 2, 1, null), new SiteWork("a", 1, 2, null),
                            SiteWork.Built("a", new DateTime(1949, 1, 15)), SiteWork.Built("a", new DateTime(1949, 2, 15)), SiteWork.Built("b", new DateTime(1949, 1, 15)) };
        Assert.Equal(works.Length, new HashSet<string>(Array.ConvertAll(works, W)).Count);
    }

    // R-LOOP-002, R-SITE-001, R-SITE-003: a site being built and a built site, as the hash writes them.
    [Fact]
    public void ASiteIsHashedAsBuildingOrBuilt()
    {
        Assert.Equal("mefalsim=b,4,5", new SiteWork("mefalsim", 4, 5, null).Canonical());
        Assert.Equal("mefalsim=done,1949-06-15", SiteWork.Built("mefalsim", new DateTime(1949, 6, 15)).Canonical());
    }

    // R-SITE-003, R-MIN-002 (#2733 item 21): a fractional progress is hashed as the double's IEEE bits, 16 uppercase hex
    // digits, so a change to decimal or culture-dependent formatting moves the hash and fails here.
    [Fact]
    public void ASitesFractionalProgressIsHashedAsItsBits()
    {
        Assert.Equal("mefalsim=b,4,5,3FD0000000000000", new SiteWork("mefalsim", 4, 5, null).WithProgress(5, 0.25).Canonical());
    }

    // R-LOOP-002, R-VET-001: a unit's service reaches the hash, and a unit with none hashes as it did before Phase 4 PR 6.
    [Fact]
    public void ServiceReachesTheHashOnlyWhenThereIsSome()
    {
        Assert.Equal("1:halutz@0,0", new Unit(1, "halutz", 0, 0).Canonical());
        Assert.Equal("1:veteran_halutz@0,0/6", new Unit(1, "veteran_halutz", 0, 0, 6).Canonical());
    }

    // R-LOOP-002, R-COR-002: a corridor's segments in index order, each unstarted, being built or built.
    [Fact]
    public void ACorridorIsHashedSegmentBySegment()
    {
        var w = new CorridorWork("burma_road", new SegmentWork?[] { null, new SegmentWork(4, 2, null), new SegmentWork(null, 3, new DateTime(1948, 12, 15)) });
        Assert.Equal("burma_road=-;b,4,2;done,1948-12-15", w.Canonical());
        Assert.Throws<ArgumentException>(() => new SegmentWork(null, 1, null));   // a segment being built has its unit
        Assert.Throws<ArgumentException>(() => new SegmentWork(0, 1, new DateTime(1949, 3, 15)));
        string H(CorridorWork x) { var s = new GameState(); s.Corridors[x.Id] = x; return s.Hash(); }
        var all = new[] { w, new CorridorWork("b", w.Segments.ToArray()), new CorridorWork("burma_road", new SegmentWork?[] { null, new SegmentWork(5, 2, null), w.Segments[2] }),
                          new CorridorWork("burma_road", new SegmentWork?[] { null, new SegmentWork(4, 3, null), w.Segments[2] }), new CorridorWork("burma_road", new SegmentWork?[] { w.Segments[1], null, w.Segments[2] }) };
        Assert.Equal(all.Length, new HashSet<string>(Array.ConvertAll(all, H)).Count);
    }

    // R-SITE-003: a built site holds no unit. Unit ids start at 0, so "no unit" is null, never 0: unit 0 is free once its
    // site is built, and a record cannot be built and have a unit, or be building without one.
    [Fact]
    public void ABuiltSiteHoldsNoUnitNotUnitZero()
    {
        var built = SiteWork.Built("mefalsim", new DateTime(1949, 3, 15));
        Assert.Null(built.Unit);
        var s = new GameState();
        s.Units.Add(0, new Unit(0, "halutz", -3, 19));
        s.Sites["mefalsim"] = new SiteWork("mefalsim", 0, 1, null);
        Assert.True(Projects.IsBusy(s, 0));
        s.Sites["mefalsim"] = built;
        Assert.False(Projects.IsBusy(s, 0));
        Assert.Throws<ArgumentException>(() => new SiteWork("mefalsim", null, 1, null));
        Assert.Throws<ArgumentException>(() => new SiteWork("mefalsim", 0, 0, new DateTime(1949, 3, 15)));
    }

    // R-LOOP-002, R-TRAIN-003: a city's recruits reach the hash, and a city with none hashes as it did before PR 4, so
    // scenario-mvp-absorb-first (no training) does not move.
    [Fact]
    public void RecruitsReachTheHashOnlyWhenThereAreSome()
    {
        var none = new City("a", 1, 2, 3, 1, 0);
        Assert.Equal("a=1,2,3,1,0", none.Canonical());
        Assert.Equal("a=1,2,3,1,0,4", none.WithRecruits(4).Canonical());
        Assert.Equal(4, none.WithRecruits(4).WithCounts(5, 1, 0).Recruits);   // a change of counts keeps the recruits
    }

    // R-LOOP-002, R-CRAFT-001, R-CITY-009: defaults preserve the old text; only non-zero craftsmen and a manual job
    // standing order add fields.
    [Fact]
    public void CraftsmenAndTheJobDefaultReachTheHashOnlyWhenSet()
    {
        var none = new City("a", 1, 2, 3, 1, 0);
        Assert.Equal("a=1,2,3,1,0", none.Canonical());
        Assert.Equal("a=1,2,3,1,0,craftsmen=2", none.WithCraftsmen(2).Canonical());
        Assert.Equal("a=1,2,3,1,0,jobs=manual", none.WithJobsDefault(false).Canonical());
        Assert.Equal(2, none.WithCraftsmen(2).WithCounts(4, 1, 0).Craftsmen);
    }

    [Fact]
    public void CloneCopiesEveryNewCollection()
    {
        var a = new GameState();
        a.Cities["x"] = new City("x", 1, 1, 1, 0, 0);
        a.Units.Add(1, new Unit(1, "halutz", 0, 0));
        a.Cohorts.Add(2, new Cohort(2, "recruit", "x", 1, 1));
        a.Sites["s"] = new SiteWork("s", 1, 1, null);
        a.Corridors["c"] = new CorridorWork("c", new SegmentWork?[] { new SegmentWork(1, 1, null) });
        a.Objectives["o"] = new DateTime(1949, 1, 1);
        a.NextId = 2; a.Score = 3;
        var before = a.Hash();
        var b = a.Clone();
        b.Cities["y"] = a.Cities["x"]; b.Units.Add(2, a.Units[1]); b.Cohorts.Clear(); b.Sites.Clear(); b.Corridors.Clear(); b.Objectives.Clear(); b.NextId = 9; b.Score = 9;
        Assert.Equal(before, a.Hash());
        Assert.Single(a.Cities); Assert.Single(a.Units); Assert.Single(a.Cohorts); Assert.Single(a.Sites); Assert.Single(a.Corridors); Assert.Single(a.Objectives);
    }

    [Fact]
    public void TheNewCollectionsOrderTheirKeysOrdinally()
    {
        var s = new GameState();
        s.Cities["b"] = new City("b", 1, 1, 1, 0, 0);
        s.Cities["B"] = new City("B", 1, 1, 1, 0, 0);
        s.Cities["a"] = new City("a", 1, 1, 1, 0, 0);
        var text = s.Describe();
        Assert.True(text.IndexOf("\n  B=", StringComparison.Ordinal) < text.IndexOf("\n  a=", StringComparison.Ordinal));
        Assert.True(text.IndexOf("\n  a=", StringComparison.Ordinal) < text.IndexOf("\n  b=", StringComparison.Ordinal));
    }
}
