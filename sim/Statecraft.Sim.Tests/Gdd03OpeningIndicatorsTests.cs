using Statecraft.Sim;

// R-NAT-001, R-NAT-003: #1715 opening slice, owner Q17/Q18, 1 November 2026.
public class Gdd03OpeningIndicatorsTests
{
    static Content Load(bool modern = true)
    {
        var root = RealContentTests.ContentRoot();
        var c = ContentLoader.Load(rel =>
        {
            var path = Path.Combine(root, rel);
            if (!File.Exists(path)) return null;
            var text = File.ReadAllText(path);
            return modern && rel == "eras.yaml"
                ? text.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : text;
        }, Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p)));
        c.Events.Clear(); // isolate opening and the effect web from event choices.
        return c;
    }

    static double Base(Content c, string id) => c.Indicators.Single(i => i.Id == id).Base;

    [Fact]
    public void Gdd03OpensAtTheCatalogueCohesionAndSupport()
    {
        var c = Load();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(SaveGame.Gdd03Ruleset, c.Ruleset);
        Assert.Equal(0.65, s.Indicators["morale"], 12);
        Assert.Equal(0.60, s.Indicators["intl_standing"], 12);
        Assert.Equal(0.55, Base(c, "morale"));
        Assert.Equal(0.50, Base(c, "intl_standing"));
        Assert.DoesNotContain("low_cohesion", s.ActiveSituations);
        Assert.DoesNotContain("low_intl_support", s.ActiveSituations);
    }

    [Theory]
    [InlineData(70, 40, 100, 0.70, 0.40)]
    [InlineData(130, 120, 200, 0.65, 0.60)]
    [InlineData(0, 100, 100, 0.0, 1.0)]
    [InlineData(65.5, 60.25, 100, 0.655, 0.6025)]
    public void OpeningReadsTheCatalogueAndItsDisplayScale(double cohesion, double support, double maximum,
        double expectedCohesion, double expectedSupport)
    {
        var c = Load();
        c.Gdd03Balance!.Values["start.cohesion"].Values = new() { (decimal)cohesion };
        c.Gdd03Balance.Values["start.support"].Values = new() { (decimal)support };
        c.Gdd03Balance.Values["indicator.display_max"].Values = new() { (decimal)maximum };
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(expectedCohesion, s.Indicators["morale"], 12);
        Assert.Equal(expectedSupport, s.Indicators["intl_standing"], 12);
    }

    [Fact]
    public void Gdd03OpeningSurvivesTheFirstTurn()
    {
        var c = Load();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var next = Simulation.Step(c, s, Array.Empty<Command>()).State;
        foreach (var (id, opening) in new[] { ("morale", 0.65), ("intl_standing", 0.60) })
        {
            var shift = opening - Base(c, id);
            var inner = Math.Clamp(Base(c, id)
                + c.At(s.Date).Effects.Where(e => e.Target == id).Sum(e => next.EffectContributions[e.Id]), 0, 1);
            Assert.Equal(shift, next.IndicatorShifts[id], 12);
            Assert.Equal(Math.Clamp(inner + shift, 0, 1), next.Indicators[id], 12);
        }
    }

    [Fact]
    public void LaterMovementKeepsTheBaseAndExistingRationingAndSecurityLinks()
    {
        var modern = Load();
        var old = Load(false);
        var a = Simulation.NewGame(modern, 1, Mode.Historical);
        var b = Simulation.NewGame(old, 1, Mode.Historical);
        foreach (var (c, s) in new[] { (modern, a), (old, b) })
        {
            s.Date = new DateTime(1949, 3, 10);
            foreach (var e in c.At(s.Date).Effects) s.EffectContributions[e.Id] = 0;
            s.Policies["rationing"] = 1;
            s.Indicators["security"] = 0.8;
        }
        EffectsSystem.Apply(modern, a, new List<Cause>());
        EffectsSystem.Apply(old, b, new List<Cause>());
        foreach (var id in new[] { "rationing_grates", "safety_lifts_morale" })
            Assert.Equal(b.EffectContributions[id], a.EffectContributions[id]);
        Assert.True(a.EffectContributions["rationing_grates"] < 0);
        Assert.True(a.EffectContributions["safety_lifts_morale"] > 0);
        Assert.Equal(0.10, a.IndicatorShifts["morale"], 12);
        Assert.Equal(0.10, a.IndicatorShifts["intl_standing"], 12);
        Assert.Equal(0.55, Base(modern, "morale"));
        Assert.Equal(0.50, Base(modern, "intl_standing"));
    }

    // Q17: the opening is outside Inner's clamp. Rebasing morale would instead give zero in the negative case.
    [Theory]
    [InlineData(-10, 0.10)]
    [InlineData(10, 1.0)]
    public void OpeningRemainsAPermanentShiftAfterTheInnerClamp(double contributionScale, double expected)
    {
        var c = Load();
        foreach (var e in c.Effects.Where(e => e.Target == "morale"))
        {
            e.K = e.Id == "safety_lifts_morale" ? contributionScale : 0;
            e.Inertia = 1;
            e.Office = "";
        }
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Indicators["security"] = 1; // Q18 retains this link, unlike the housing link owned by #2291.
        EffectsSystem.Apply(c, s, new List<Cause>());
        var beforeClamp = Base(c, "morale")
            + c.At(s.Date).Effects.Where(e => e.Target == "morale").Sum(e => s.EffectContributions[e.Id]);
        Assert.True(contributionScale < 0 ? beforeClamp < 0 : beforeClamp > 1);
        Assert.Equal(0.10, s.IndicatorShifts["morale"], 12);
        Assert.Equal(expected, s.Indicators["morale"], 12);
        Assert.Equal(0.55, Base(c, "morale"));
    }

    [Fact]
    public void Gdd01AndLegacyKeepTheirOpeningAndDoNotReadTheOpeningCatalogue()
    {
        foreach (var c in new[] { Load(false), RealContentTests.Legacy() })
        {
            // Poison only these rows: neither old identity may consult the new opening reader.
            c.Gdd03Balance = null;
            var s = Simulation.NewGame(c, 1, Mode.Historical);
            Assert.NotEqual(SaveGame.Gdd03Ruleset, c.Ruleset);
            Assert.Equal(0.55, s.Indicators["morale"]);
            Assert.Equal(0.50, s.Indicators["intl_standing"]);
            Assert.Equal(0, s.IndicatorShifts["morale"]);
            Assert.Equal(0, s.IndicatorShifts["intl_standing"]);
        }
    }

    [Theory]
    [InlineData("start.cohesion", "missing")]
    [InlineData("start.support", "missing")]
    [InlineData("indicator.display_max", "missing")]
    [InlineData("start.cohesion", "deferred")]
    [InlineData("start.support", "multiple")]
    [InlineData("start.cohesion", "expression")]
    [InlineData("start.support", "parts")]
    [InlineData("start.cohesion", "negative")]
    [InlineData("start.support", "above-scale")]
    [InlineData("indicator.display_max", "zero")]
    [InlineData("indicator.display_max", "negative")]
    public void InvalidOpeningRowsAreRefusedInsteadOfUsingTheOldBase(string key, string damage)
    {
        var c = Load();
        var row = c.Gdd03Balance!.Values[key];
        switch (damage)
        {
            case "missing": c.Gdd03Balance.Values.Remove(key); break;
            case "deferred": row.Basis = "Deferred"; break;
            case "multiple": row.Values.Add(1); break;
            case "expression": row.Expression = "unresolved"; break;
            case "parts": row.Parts.Add("extra", new Gdd03DesignValue()); break;
            case "negative": row.Values[0] = -1; break;
            case "above-scale": row.Values[0] = 101; break;
            case "zero": row.Values[0] = 0; break;
        }
        var ex = Assert.Throws<ContentException>(() => Simulation.NewGame(c, 1, Mode.Historical));
        Assert.Equal(Gdd03BalanceCatalogue.FileName, ex.File);
        Assert.Contains(key, ex.Message);
    }

    [Fact]
    public void MissingOpeningCatalogueIsRefusedWithoutFallback()
    {
        var c = Load();
        c.Gdd03Balance = null;
        var ex = Assert.Throws<ContentException>(() => Simulation.NewGame(c, 1, Mode.Historical));
        Assert.Equal(Gdd03BalanceCatalogue.FileName, ex.File);
    }
}
