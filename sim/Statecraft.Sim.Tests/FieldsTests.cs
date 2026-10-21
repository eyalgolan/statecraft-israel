using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Plow fields (GDD Phase 4 PR 4; D-D10, GDD "Prepare agricultural land"): PioneerWork(plow_fields) at a field site
/// (R-FIELD-001) and the Food a plowed field adds in step 6 (R-FIELD-002). The owner took the plan's recommendations on #782
/// (17 October 2026): a field in each yishuv's hex, ready once that yishuv is built (Q-FIELD-1), no ongoing labour
/// (Q-FIELD-2), 1 Food a turn (Q-FIELD-3).</summary>
public class FieldsTests
{
    /// <summary>The GDD fixture with site_a's field, site_a_field, in site_a's hex. Plowing takes plow_fields_turns 2 and
    /// costs IL 0.5m; a plowed field yields field_food_per_turn 7 (fixture values, so a yield cannot pass as 1 by chance).</summary>
    static Content Town(string fieldKinds = "[field]") => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["sites.yaml"] = Fixture.GddSites + FieldSite(fieldKinds),
    })));

    static string FieldSite(string kinds) => Fixture.GddSites.Replace("sites:\n", "").Replace("site_a", "site_a_field")
        .Replace("Site A", "Site A fields").Replace("kinds: [yishuv]", "kinds: " + kinds).Replace("    founded: 1949-06-12\n", "");

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    static GameState WithHalutz(Content c, out int id)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", 1, 0));
        return s;
    }

    static PioneerWork Plow(int unit, string site = "site_a_field") => new PioneerWork(unit, Projects.PlowFields, site);

    /// <summary>A state whose imports are cut (the fx crisis), so the stock shows the field's Food instead of an import
    /// topping it up to its cover (R-FOOD-002).</summary>
    static GameState NoImports(Content c)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.ActiveSituations.Add(EconomySystem.FxCrisis);
        s.FxReserves = -1000;
        s.Sites["site_a"] = SiteWork.Built("site_a", new DateTime(1948, 5, 15));
        return s;
    }

    // R-FIELD-002 (Review focus 4: a field yields only after it is plowed, by the stated amount)
    [Fact]
    public void APlowedFieldAddsFoodInStepSix()
    {
        var c = Town();
        var without = NoImports(c);
        var plowed = without.Clone();
        plowed.Sites["site_a_field"] = SiteWork.Built("site_a_field", new DateTime(1948, 5, 15));
        var plowing = without.Clone();
        plowing.Units.Add(99, new Unit(99, "halutz", 1, 0));   // a site being built names its unit (SiteWork, R-VET-002)
        plowing.Sites["site_a_field"] = new SiteWork("site_a_field", 99, 5, null);   // still being plowed: no Food yet

        var a = Step(c, without).State;
        var b = Step(c, plowed);
        Assert.Equal(300 - 100, a.Food);                    // ate a month's need, imported nothing (PoolsTests)
        Assert.Equal(a.Food + 7, b.State.Food);             // exactly the yield, once
        Assert.Equal(a.Food, Step(c, plowing).State.Food);
        Assert.Contains(b.Why, w => w.System == "economy" && w.Target == "food" && w.Delta == 7 && w.Message == "Site A fields: the plowed land yielded 7 units of Food");
    }

    // R-FIELD-002, D-A6: the field's Food reaches the stock before the nation eats (step 7), so it covers part of the need. Added
    // after eating, the same turn would leave all 100 units unmet and 7 in the stock. Production (step 6's jobs income) moves
    // only the Treasury, so the order against it shows only in the order of the causes.
    [Fact]
    public void FieldFoodComesBeforeTheNationEats()
    {
        var c = Town();
        var s = NoImports(c);
        s.Sites["site_a_field"] = SiteWork.Built("site_a_field", new DateTime(1948, 5, 15));
        s.Food = 0;
        var r = Step(c, s);
        Assert.Equal((0, 100 - 7), (r.State.Food, r.State.FoodUnmet));
        // After production: town_a's jobs income is listed before the field's Food.
        var paid = r.Why.ToList().FindIndex(w => w.System == "city" && w.Message.Contains("jobs paid"));
        Assert.InRange(paid, 0, r.Why.ToList().FindIndex(w => w.Message.StartsWith("Site A fields: the plowed land")) - 1);
    }

    // R-FIELD-001 (Q-FIELD-1): a field in a yishuv's hex is ready only once that yishuv is built.
    [Theory]
    [InlineData("no record")]
    [InlineData("being built")]
    public void AFieldBeforeItsYishuvIsRefused(string yishuv)
    {
        var c = Town();
        var s = WithHalutz(c, out var id);
        if (yishuv == "being built")
        {
            s.Units.Add(99, new Unit(99, "halutz", 1, 0));   // a site being built names its unit (SiteWork, R-VET-002)
            s.Sites["site_a"] = new SiteWork("site_a", 99, 2, null);
        }
        var r = Step(c, s, Plow(id));
        Assert.Equal("Site A fields can be plowed once Site A is built", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-FIELD-001: plowing follows R-SITE-001 to R-SITE-003. It pays in full at the start and takes its turns, counting the
    // order's; built, the field is plowed and the unit is idle again. R-FIELD-002, D-A6: plowing ends in step 5, so the field
    // already yields in step 6 of the turn it is done (imports cut, so an import cannot hide the Food).
    [Fact]
    public void PlowingPaysAtTheStartAndIsDoneAfterItsTurns()
    {
        var c = Town();
        var s = NoImports(c);
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", 1, 0));
        var r = Step(c, s, Plow(id));
        Assert.Empty(r.Rejected);
        Assert.Equal(("site_a_field", (int?)id, 1), (r.State.Sites["site_a_field"].Site, r.State.Sites["site_a_field"].Unit, r.State.Sites["site_a_field"].TurnsLeft));
        Assert.Equal(Step(c, s).State.Treasury - 0.5, r.State.Treasury, 9);
        Assert.Contains(r.Why, x => x.System == "map" && x.Message == "Site A fields: a group of halutzim began plowing, cost IL 0.5m");

        var done = Step(c, r.State);
        Assert.Equal(done.State.Date, done.State.Sites["site_a_field"].BuiltOn);   // the end of the order's turn + 1
        Assert.False(Projects.IsBusy(done.State, id));
        Assert.Contains(done.Why, x => x.System == "map" && x.Message == "Site A fields plowed");
        Assert.DoesNotContain(done.Why, x => x.Message.Contains("founded"));
        var unplowed = r.State.Clone();
        unplowed.Sites.Remove("site_a_field");
        Assert.Equal(Step(c, unplowed).State.Food + 7, done.State.Food);   // this turn's step 6, not the next turn's
    }

    // R-FIELD-001 follows R-SITE-002 (D-A3): a plow can be stopped; the progress is lost and the IL 0.5m is not returned.
    [Fact]
    public void StoppingAPlowFreesTheUnitAndTheFieldAndReturnsNoMoney()
    {
        var c = Town();
        var s = WithHalutz(c, out var id);
        s.Sites["site_a"] = SiteWork.Built("site_a", new DateTime(1948, 5, 15));
        var started = Step(c, s, Plow(id));
        var stopped = Step(c, started.State, new StopWork(id));
        Assert.Empty(stopped.Rejected);
        Assert.False(stopped.State.Sites.ContainsKey("site_a_field"));
        Assert.False(Projects.IsBusy(stopped.State, id));
        Assert.Equal(Step(c, started.State).State.Treasury, stopped.State.Treasury, 9);   // the IL 0.5m paid stays paid
        Assert.Contains(stopped.Why, x => x.System == "map" && x.Message == "Site A fields: the halutzim stopped building; the Money spent is not returned");
        var again = Step(c, stopped.State, Plow(id));   // the field is available again, from the start
        Assert.Empty(again.Rejected);
        Assert.Equal(started.State.Sites["site_a_field"].TurnsLeft, again.State.Sites["site_a_field"].TurnsLeft);
    }

    // R-SITE-004, R-FIELD-001: each action only at its own kind of site.
    [Fact]
    public void PlowingIsRefusedAtAYishuvSite()
    {
        var c = Town();
        var s = WithHalutz(c, out var id);
        var r = Step(c, s, Plow(id, "site_a"));
        Assert.Equal("Site A is not a site for fields", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-FIELD-001: a field site's built record is its plowed state, so a field site is not also a yishuv site (a built yishuv
    // on it would yield Food), and the field rules are required in the GDD loop, so a missing key is never read as 0.
    [Theory]
    [InlineData("kinds", "sites.yaml", "a field site cannot also be a yishuv site")]
    [InlineData("plow_fields_turns", "rules.yaml", "mvp.plow_fields_turns")]
    [InlineData("plow_fields_cost", "rules.yaml", "mvp.plow_fields_cost")]
    [InlineData("field_food_per_turn", "rules.yaml", "mvp.field_food_per_turn")]
    public void BadFieldContentFailsLoad(string what, string file, string message)
    {
        var files = Fixture.Gdd(new Dictionary<string, string> { ["sites.yaml"] = Fixture.GddSites + FieldSite(what == "kinds" ? "[field, yishuv]" : "[field]") });
        if (what != "kinds") files["rules.yaml"] = files["rules.yaml"].Replace(what switch
        {
            "plow_fields_turns" => "  plow_fields_turns: 2\n",
            "plow_fields_cost" => "  plow_fields_cost: 0.5\n",
            _ => "  field_food_per_turn: 7\n",
        }, "");
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Equal(file, e.File);
        Assert.Contains(message, e.Message);
    }

    // The real content: every field site lies in a yishuv's hex (Q-FIELD-1) or the swamp's (Q-SWAMP-1), and the design
    // values are those of Q-FIELD-3.
    [Fact]
    public void TheRealFieldsAreThePlannedOnes()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var m = c.Rules.Mvp!;
        Assert.Equal((1, 3, 0.5), (m.FieldFoodPerTurn!.Value, m.PlowFieldsTurns, m.PlowFieldsCost!.Value));
        foreach (var f in c.Sites.Where(x => x.Kinds.Contains(Projects.FieldKind)))
            Assert.Contains(c.Sites, y => (y.Kinds.Contains(Projects.YishuvKind) || y.Kinds.Contains(Projects.SwampKind)) && (y.Q, y.R) == (f.Q, f.R));
    }
}
