using System;
using System.Collections.Generic;
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
            ("next=", x => x.NextId = 2),
            ("obj{", x => x.Objectives["first_new_yishuv"] = new DateTime(1949, 3, 15)),
            ("score=", x => x.Score = 5),
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
                              new Cohort(1, "recruit", "b", 1, 1), new Cohort(1, "recruit", "a", 2, 1), new Cohort(1, "recruit", "a", 1, 2) };
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

    // R-LOOP-002, R-VET-001: a unit's service reaches the hash, and a unit with none hashes as it did before Phase 4 PR 6.
    [Fact]
    public void ServiceReachesTheHashOnlyWhenThereIsSome()
    {
        Assert.Equal("1:halutz@0,0", new Unit(1, "halutz", 0, 0).Canonical());
        Assert.Equal("1:veteran_halutz@0,0/6", new Unit(1, "veteran_halutz", 0, 0, 6).Canonical());
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
        a.Objectives["o"] = new DateTime(1949, 1, 1);
        a.NextId = 2; a.Score = 3;
        var before = a.Hash();
        var b = a.Clone();
        b.Cities["y"] = a.Cities["x"]; b.Units.Add(2, a.Units[1]); b.Cohorts.Clear(); b.Sites.Clear(); b.Objectives.Clear(); b.NextId = 9; b.Score = 9;
        Assert.Equal(before, a.Hash());
        Assert.Single(a.Cities); Assert.Single(a.Units); Assert.Single(a.Cohorts); Assert.Single(a.Sites); Assert.Single(a.Objectives);
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
