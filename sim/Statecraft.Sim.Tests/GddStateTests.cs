using System;
using System.Collections.Generic;
using Statecraft.Sim;

/// <summary>The GDD v0.1 state (spec 2026-10-12 section 2.2): written into the hash only when set, so the legacy loop's
/// hash text, and every golden, is unchanged.</summary>
public class GddStateTests
{
    // R-LOOP-002
    [Fact]
    public void WithTheSwitchOffTheGoldenSnapshotIsByteIdentical()
    {
        var s = GoldenRun.Play(ContentLoader.Load(RealContentTests.ContentRoot()), GoldenRun.Until1950);
        Assert.Equal(GoldenSnapshot.Read("hash1950"), s.Describe());
        Assert.Equal(GoldenRun.Hash1950, s.Hash());
    }

    // R-LOOP-002
    [Fact]
    public void AnEmptyStateWritesNoNewSection()
    {
        var text = new GameState().Describe();
        foreach (var label in new[] { "city{", "coh=", "units[", "next=", "obj{", "score=" }) Assert.DoesNotContain(label, text);
        Assert.EndsWith("over=", text);
    }

    // R-LOOP-002, R-CITY-005
    [Fact]
    public void CohesionIsHashedWithTheCities()
    {
        var s = new GameState { Cohesion = 0.5 };
        Assert.DoesNotContain("coh=", s.Describe());   // no cities: the legacy loop's text is unchanged whatever the field holds
        s.Cities["x"] = new City("x", 1, 1, 1, 0, 0);
        Assert.Contains("coh=0.5 [3FE0000000000000]", s.Describe());
        var before = s.Hash();
        s.Cohesion = 0.25;
        Assert.NotEqual(before, s.Hash());
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
        var units = new[] { new Unit(1, "halutz", 0, 0), new Unit(2, "halutz", 0, 0), new Unit(1, "recruit", 0, 0), new Unit(1, "halutz", 1, 0), new Unit(1, "halutz", 0, 1) };
        Assert.Equal(units.Length, new HashSet<string>(Array.ConvertAll(units, U)).Count);
    }

    [Fact]
    public void CloneCopiesEveryNewCollection()
    {
        var a = new GameState();
        a.Cities["x"] = new City("x", 1, 1, 1, 0, 0);
        a.Units.Add(1, new Unit(1, "halutz", 0, 0));
        a.Objectives["o"] = new DateTime(1949, 1, 1);
        a.NextId = 2; a.Score = 3;
        var before = a.Hash();
        var b = a.Clone();
        b.Cities["y"] = a.Cities["x"]; b.Units.Add(2, a.Units[1]); b.Objectives.Clear(); b.NextId = 9; b.Score = 9;
        Assert.Equal(before, a.Hash());
        Assert.Single(a.Cities); Assert.Single(a.Units); Assert.Single(a.Objectives);
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
