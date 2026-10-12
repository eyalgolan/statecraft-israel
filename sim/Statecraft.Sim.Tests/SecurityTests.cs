using System.Linq;
using System;
using System.Collections.Generic;
using Statecraft.Sim;

public class SecurityTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    [Theory]
    [InlineData(1, TurnLength.Quarter)] [InlineData(3, TurnLength.Quarter)]
    [InlineData(4, TurnLength.Month)] [InlineData(5, TurnLength.Week)]
    public void TurnLengthByRung(int rung, TurnLength expected) => Assert.Equal(expected, SecuritySystem.TurnLengthFor(rung));

    [Fact]
    public void StartAtWarSetsCeasefireClockAndCallUp()
    {
        var s = new GameState(); SecuritySystem.Init(C, s, new List<Cause>());
        Assert.Equal(5, s.Rung); Assert.Equal(4, s.CeasefireWeeksLeft); Assert.Equal(0.8, s.Mobilization);
    }

    [Fact]
    public void CeasefireRungDropIsCountedOnce()
    {
        var s = new GameState(); var why = new List<Cause>();
        SecuritySystem.Init(C, s, why);
        why.Clear();
        for (int i = 0; i < 4; i++) SecuritySystem.Apply(C, s, TurnLength.Week, why);
        Assert.Equal(3, s.Rung);
        Assert.Equal(-2, why.Where(w => w.Target == "rung").Sum(w => w.Delta));
        Assert.Contains(why, w => w.Target == "ceasefire" && w.Delta == 0 && w.Message.Contains("ceasefire"));
    }

    [Fact]
    public void CeasefireClockForcesDeescalation()
    {
        var s = new GameState(); var why = new List<Cause>();
        SecuritySystem.Init(C, s, why);
        for (int i = 0; i < 3; i++) SecuritySystem.Apply(C, s, TurnLength.Week, why);
        Assert.Equal(5, s.Rung);
        SecuritySystem.Apply(C, s, TurnLength.Week, why);
        Assert.Equal(3, s.Rung); Assert.Equal(0, s.CeasefireWeeksLeft);
        Assert.Contains(why, w => w.Target == "ceasefire" && w.Message.Contains("ceasefire"));
        Assert.Equal(0.8, s.Mobilization); // de-escalation does not demobilize
    }

    [Fact]
    public void CallUpOrderAboveFloorRecordsCause()
    {
        var s = new GameState { Rung = 1 }; var why = new List<Cause>();
        Assert.Null(SecuritySystem.SetMobilization(C, s, 0.9, why));
        var cause = Assert.Single(why, w => w.System == "security" && w.Target == "mobilization");
        Assert.Equal(0.9, cause.Delta);
        Assert.Equal("Reserve call-up set to 90%", cause.Message);
        Assert.Equal(0.9, s.Mobilization);
    }

    [Fact]
    public void EscalationRaisesCallUpFloor()
    {
        var s = new GameState { Rung = 1 }; var why = new List<Cause>();
        SecuritySystem.SetRung(C, s, 4, why);
        Assert.Equal(0.4, s.Mobilization);
        SecuritySystem.SetRung(C, s, 9, why);
        Assert.Equal(5, s.Rung); Assert.Equal(4, s.CeasefireWeeksLeft);
    }

    [Fact]
    public void MobilizationOrdersAreValidatedAndFloored()
    {
        var s = new GameState { Rung = 3 }; var why = new List<Cause>();
        Assert.Equal("Mobilization must be between 0 and 1", SecuritySystem.SetMobilization(C, s, 1.5, why));
        Assert.Null(SecuritySystem.SetMobilization(C, s, 0.0, why));
        Assert.Equal(0.15, s.Mobilization);
    }

    [Theory]
    [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)] [InlineData(double.NegativeInfinity)]
    public void NonFiniteMobilizationIsRejectedAndStateUnchanged(double level)
    {
        var s = new GameState { Rung = 3, Mobilization = 0.3 }; var why = new List<Cause>();
        Assert.Equal("Mobilization must be between 0 and 1", SecuritySystem.SetMobilization(C, s, level, why));
        Assert.Equal(0.3, s.Mobilization);
    }

    [Fact]
    public void LoaderRequiresFiveRungFloors()
    {
        var bad = Fixture.Files["rules.yaml"].Replace("[0.02, 0.05, 0.15, 0.4, 0.8]", "[0.1]");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = bad })));
        Assert.Equal("rung_min_mobilization", ex.Field);
    }
    [Fact]
    public void OutOfRangeRungInStateUsesTheRungFiveFloor()
    {
        var s = new GameState { Rung = 9, Mobilization = 0.0 }; var why = new List<Cause>();
        Assert.Null(SecuritySystem.SetMobilization(C, s, 0.0, why));   // reaches RaiseToFloor
        Assert.Equal(0.8, s.Mobilization);
    }

    [Theory]
    [InlineData(-3, 1)] [InlineData(0, 1)] [InlineData(1, 1)] [InlineData(5, 5)] [InlineData(9, 5)]
    public void ClampRungKeepsTheLadderInRange(int rung, int expected) => Assert.Equal(expected, SecuritySystem.ClampRung(rung));

    [Theory]
    [InlineData(1, 0.02)] [InlineData(4, 0.4)] [InlineData(5, 0.8)] [InlineData(9, 0.8)] [InlineData(0, 0.02)]
    public void MinMobilizationIsTheRungsFloor(int rung, double expected) => Assert.Equal(expected, SecuritySystem.MinMobilization(C, C.Rules.Start, rung));

    [Fact]
    public void EffectiveMobilizationIsNeverBelowTheFloor()
    {
        Assert.Equal(0.8, SecuritySystem.EffectiveMobilization(C, C.Rules.Start, 5, 0.1));
        Assert.Equal(0.9, SecuritySystem.EffectiveMobilization(C, C.Rules.Start, 5, 0.9));
        Assert.Equal(0.4, SecuritySystem.EffectiveMobilization(C, C.Rules.Start, 4, 0.0));
    }

    [Fact]
    public void EffectiveMobilizationMatchesWhatTheSimDoes()
    {
        foreach (var rung in new[] { 1, 3, 5 })
            foreach (var level in new[] { 0.0, 0.3, 1.0 })
            {
                var s = new GameState { Rung = rung, Mobilization = 0 };
                Assert.Null(SecuritySystem.SetMobilization(C, s, level, new List<Cause>()));
                Assert.Equal(SecuritySystem.EffectiveMobilization(C, s.Date, rung, level), s.Mobilization);
            }
    }

    [Theory]
    [InlineData(3, 0, 5, 4)]    // entering full war starts the clock
    [InlineData(5, 2, 5, 2)]    // staying keeps it
    [InlineData(5, 2, 4, 0)]    // leaving ends it
    [InlineData(2, 0, 3, 0)]
    public void CeasefireWeeksAfterAnOrderFollowsSetRung(int from, int left, int to, int expected)
    {
        Assert.Equal(expected, SecuritySystem.CeasefireWeeksAfter(C, C.Rules.Start, from, left, to));
        var s = new GameState { Rung = from, CeasefireWeeksLeft = left };
        SecuritySystem.SetRung(C, s, to, new List<Cause>());
        Assert.Equal(expected, s.CeasefireWeeksLeft);
    }
}
