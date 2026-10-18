using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The GDD's two national parameters (Phase 2 PR 4; D-C1, decided 15 October 2026): Social cohesion is the 0 to 1
/// `morale` indicator in the effects web, shown as Social cohesion, and International Support is `intl_standing`; their
/// thresholds are situations (Q-NAT-3). The inputs are Q-NAT-2's: the existing housing link, the cities' employed share and
/// food_supply for shortages (Reading 2: Phase 1's separate GameState.Cohesion is folded into `morale`).</summary>
public class NationalParametersTests
{
    static readonly Content Mvp = ContentLoader.Load(RealContentTests.ContentRoot());
    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);
    static string Name(Content c, string indicator) => c.Indicators.First(i => i.Id == indicator).Name;

    /// <summary>A real GDD game a few turns in, so every effect contribution is under way.</summary>
    static GameState Played()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        for (int i = 0; i < 3; i++) s = Step(Mvp, s).State;
        return s;
    }

    // R-NAT-001
    [Fact]
    public void CohesionIsMoraleShownAsSocialCohesion()
    {
        Assert.Equal("Social cohesion", Name(Mvp, "morale"));
        Assert.Null(typeof(GameState).GetField("Cohesion"));   // no second cohesion value beside the indicator
        var s = Played();
        Assert.NotEmpty(s.Cities);
        Assert.DoesNotContain("coh=", s.Describe());
        Assert.InRange(s.Indicators["morale"], 0.0, 1.0);
    }

    // R-NAT-003
    [Fact]
    public void InternationalSupportIsIntlStanding()
    {
        Assert.Equal("International support", Name(Mvp, "intl_standing"));   // #683's name, the top bar's caption
        var s = Played();
        s.PendingEvents.Add("armistice_lebanon");
        var before = s.Indicators["intl_standing"];
        Assert.Null(EventSystem.Choose(Mvp, s, "armistice_lebanon", "sign", new List<Cause>()));
        Assert.Equal(before + 0.05, s.Indicators["intl_standing"], 9);        // the option's effect on intl_standing
    }

    /// <summary>Each input Q-NAT-2 names moves Social cohesion in its direction: two states that differ only in that input
    /// step to a higher cohesion on the side with more of it.</summary>
    // R-CITY-005, R-NAT-001
    [Fact]
    public void CohesionReadsItsInputs()
    {
        var s = Played();
        double After(GameState x) => Step(Mvp, x).State.Indicators["morale"];

        // Employment: the same city with more of its civilians in work, each number fixed (R-CITY-009's default off).
        var city = s.Cities["tel_aviv"].WithJobsDefault(false);
        Assert.True(city.Employed > 0 && city.Employed < city.Civilians, "precondition: room to move employment both ways");
        var idle = s.Clone(); idle.Cities["tel_aviv"] = city.WithCounts(city.Civilians, 0, city.Refugees);
        var busy = s.Clone(); busy.Cities["tel_aviv"] = city.WithCounts(city.Civilians, Math.Min(city.Civilians, city.JobSlots), city.Refugees);
        Assert.True(After(busy) > After(idle), "more of the civilians in work raises Social cohesion");

        // Shortages: a lower food_supply, which a Food shortfall lowers (R-FOOD-003, PoolsTests).
        var hungry = s.Clone(); EffectsSystem.Shift(Mvp, hungry, "food_supply", -0.2);
        Assert.True(After(s) > After(hungry), "a Food shortage lowers Social cohesion");

        // Housing: the existing link (homes_lift_morale) stays.
        var cramped = s.Clone(); EffectsSystem.Shift(Mvp, cramped, "housing", -0.2);
        Assert.True(After(s) > After(cramped), "worse housing lowers Social cohesion");

        // Security: the existing security link stays.
        var lowSecurity = s.Clone(); EffectsSystem.Shift(Mvp, lowSecurity, "security", 0.2 - lowSecurity.Indicators["security"]);
        var highSecurity = s.Clone(); EffectsSystem.Shift(Mvp, highSecurity, "security", 0.8 - highSecurity.Indicators["security"]);
        Assert.True(After(highSecurity) > After(lowSecurity), "better security raises Social cohesion");
    }

    /// <summary>The cities' employed share, the effect web's source: employed over civilians summed over all cities, 1 with
    /// no civilians (nobody is out of work).</summary>
    // R-CITY-005
    [Fact]
    public void TheEmployedShareIsOverAllCities()
    {
        var s = new GameState();
        Assert.Equal(1.0, Cities.EmployedShare(s));
        s.Cities["a"] = new City("a", 10, 10, 10, 6, 0);
        s.Cities["b"] = new City("b", 30, 30, 30, 0, 5);
        Assert.Equal(6.0 / 40, Cities.EmployedShare(s), 12);
    }

    /// <summary>The legacy loop has no cities, so the employed share reads 0 there and its effect adds nothing; the real
    /// content still loads with the loop switch taken out (Unity's SimBridgeGddTests.Legacy).</summary>
    // R-CITY-005
    [Fact]
    public void TheLegacyLoopHasNoEmployedShare()
    {
        var model = Fixture.Files["model.yaml"].Replace("effects:\n", "effects:\n  - { id: work, source: employed_share, target: morale, k: 0.1, inertia: 2, name: Work }\n");
        var legacy = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["model.yaml"] = model }));
        Assert.False(legacy.IsGdd);
        var s = Step(legacy, Simulation.NewGame(legacy, GoldenRun.Seed, Mode.Historical)).State;
        Assert.Equal(0.0, s.EffectContributions["work"]);
        var gdd = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string> { ["model.yaml"] = model })));
        var g = Step(gdd, Simulation.NewGame(gdd, GoldenRun.Seed, Mode.Historical)).State;
        Assert.True(g.EffectContributions["work"] > 0, "the GDD loop's cities feed it");
    }

    /// <summary>Q-NAT-3: "Low social cohesion" and "Low international support" are situations with food_crisis's bands, on
    /// below 0.30 and off above 0.45.</summary>
    // R-NAT-002, R-NAT-004
    [Theory]
    [InlineData("morale", "low_cohesion", "Low social cohesion")]
    [InlineData("intl_standing", "low_intl_support", "Low international support")]
    public void BelowTheThresholdOpensTheSituation(string indicator, string situation, string name)
    {
        var sit = Mvp.Situations.Single(x => x.Id == situation);
        Assert.Equal((indicator, 0.30, 0.45, name), (sit.Indicator, sit.OnBelow, sit.OffAbove, sit.Name));
        var s = Played();
        var why = new List<Cause>();
        EffectsSystem.Shift(Mvp, s, indicator, 0.25 - s.Indicators[indicator]);
        EffectsSystem.UpdateSituations(Mvp, s, why);
        Assert.Contains(situation, s.ActiveSituations);
        Assert.Contains(why, w => w.Message.StartsWith(name + " began", StringComparison.Ordinal));
        EffectsSystem.Shift(Mvp, s, indicator, 0.40 - s.Indicators[indicator]);   // inside the band: stays on
        EffectsSystem.UpdateSituations(Mvp, s, why);
        Assert.Contains(situation, s.ActiveSituations);
        EffectsSystem.Shift(Mvp, s, indicator, 0.50 - s.Indicators[indicator]);
        EffectsSystem.UpdateSituations(Mvp, s, why);
        Assert.DoesNotContain(situation, s.ActiveSituations);
    }
}
