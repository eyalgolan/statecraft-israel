using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Event effects on units and cities (D-H2; GDD Phase 7 PR 2, #806). The owner took the plan's recommendations for
/// Q-HFX-1 to Q-HFX-3 on 17 October 2026 (#784): grants of units, city housing capacity, condition and job slots, and
/// project unlock, each validated at load; a grant is free and stands at the military camp or at the site the event
/// names (here a city it names); a sensitive option gains nothing from them (R-EFX-005). This first slice builds the kinds whose records are on
/// main: unit grants and a city's housing capacity and job slots; the second adds its housing condition (#855). Project unlock
/// (#735) and site targets (once a site carries a date, R-EFX-006) follow.</summary>
public class StructuredEffectTests
{
    const string Arms = @"
id: arms
title: Arms arrive
text: A shipment arrives.
from: 1948-05-15
{TAGS}options:
  - id: accept
    text: Accept
    historical: true
    effects:
      - target: fx_reserves
        delta: -10
{STRUCTURED}  - id: refuse
    text: Refuse
";

    const string Grants = @"    grant_units:
      - { type: halutz, n: 2 }
      - { type: halutz, n: 1, at: town_b }
    city_effects:
      - { city: town_a, field: housing_capacity, delta: 3 }
      - { city: town_a, field: job_slots, delta: -100 }
      - { city: town_b, field: condition, delta: -0.05 }
";

    static string Event(string structured, string tags = "") =>
        Arms.Replace("{STRUCTURED}", structured).Replace("{TAGS}", tags == "" ? "" : "tags: [" + tags + "]\n");

    // A second town on its own hex, so a grant's `at` lands somewhere other than the camp city. Fixture values, not history.
    static Content Load(string ev, string facilities = "[military_camp, hakhshara]") =>
        ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["map/settlement_hexes.yaml"] = "settlements:\n  - { id: town_a, q: 1, r: 0 }\n  - { id: town_b, q: 2, r: 1 }\n",
            ["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"]
                + "  - { id: town_b, name: Town B, lon: 34.42, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n",
            ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 12, 8, 10, 6, facilities) + Fixture.City("town_b", 5, 2, 2, 1),
        }), new Dictionary<string, string> { ["arms.yaml"] = ev }));

    static ContentException Refused(string structured, string tags = "") =>
        Assert.Throws<ContentException>(() => Load(Event(structured, tags)));

    static (int Q, int R) TownHex(Content c) { var h = c.SettlementHexes.First(x => x.Id == "town_a"); return (h.Q, h.R); }

    // R-EFX-003: a grant adds exactly its units, free, taking the next ids in turn: with no `at` they stand at the city with
    // the military camp, with `at` at the named city. Nothing else of the units changes.
    [Fact]
    public void AGrantAddsExactlyItsUnits()
    {
        var c = Load(Event(Grants));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(new[] { "arms" }, s.PendingEvents);
        var before = s.Clone();
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, s, "arms", "accept", why));

        var added = s.Units.Values.Where(u => !before.Units.ContainsKey(u.Id)).OrderBy(u => u.Id).ToList();
        Assert.Equal(new[] { before.NextId, before.NextId + 1, before.NextId + 2 }, added.Select(u => u.Id));
        Assert.Equal(before.NextId + 3, s.NextId);
        Assert.All(added, u => { Assert.Equal("halutz", u.Type); Assert.Equal(0, u.Service); });
        var town = TownHex(c);
        Assert.Equal(new[] { town, town, (2, 1) }, added.Select(u => (u.Q, u.R)));
        foreach (var u in before.Units.Values) Assert.Equal(u.Canonical(), s.Units[u.Id].Canonical());
        // Free (Q-HFX-2): the only Money that moves is the option's own numeric effect.
        Assert.Equal(before.Treasury, s.Treasury);
        Assert.Equal(before.FxReserves - 10, s.FxReserves);
    }

    // R-EFX-004: a city effect changes only the field it names. Housing capacity never drops below 0; job slots never drop
    // below the city's employed, so a cut larger than that applies only to the free slots, and the cause carries what applied.
    [Fact]
    public void ACityEffectChangesOnlyItsField()
    {
        var c = Load(Event(Grants));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var x = s.Cities["town_a"];
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, s, "arms", "accept", why));
        var y = s.Cities["town_a"];
        Assert.Equal(x.HousingCapacity + 3, y.HousingCapacity);
        Assert.Equal(x.Employed, y.JobSlots);
        Assert.Equal((x.Civilians, x.Employed, x.Refugees, x.Recruits, x.Craftsmen, x.JobsDefault),
            (y.Civilians, y.Employed, y.Refugees, y.Recruits, y.Craftsmen, y.JobsDefault));
        Assert.Contains(why, w => w.System == "events" && w.Target == "town_a.housing_capacity" && w.Delta == 3);
        Assert.Contains(why, w => w.System == "events" && w.Target == "town_a.job_slots" && w.Delta == x.Employed - x.JobSlots);
    }

    // R-EFX-004: changing a city's capacity or slots preserves its housing condition with every other unnamed field.
    [Fact]
    public void ACityEffectKeepsTheHousingCondition()
    {
        var c = Load(Event(Grants));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Cities["town_a"] = s.Cities["town_a"].WithCondition(0.73);

        Assert.Null(EventSystem.Choose(c, s, "arms", "accept", new List<Cause>()));

        Assert.Equal(0.73, s.Cities["town_a"].Condition);
    }

    // R-EFX-006: the structured effects apply after the numeric ones, in the option's list order (grants, then city effects).
    [Fact]
    public void TheyApplyAfterTheNumericEffectsInListOrder()
    {
        var c = Load(Event(Grants));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, s, "arms", "accept", why));
        Assert.Equal(new[] { "fx_reserves", "halutz", "halutz", "town_a.housing_capacity", "town_a.job_slots", "town_b.condition" },
            why.Where(w => w.System == "events" && w.Target != "arms").Select(w => w.Target));
    }

    // R-EFX-006: a target that does not exist on the event's date is refused at load, naming file and field, never skipped
    // at run time. A site has no window yet and starts unbuilt, so a site target is refused until sites carry a date.
    [Theory]
    [InlineData("    grant_units:\n      - { type: tank, n: 1 }\n", "grant_units[0].type", "unknown unit type")]
    [InlineData("    grant_units:\n      - { type: recruit, n: 1 }\n", "grant_units[0].type", "not a unit")]
    [InlineData("    grant_units:\n      - { type: halutz, n: 0 }\n", "grant_units[0].n", "at least 1")]
    [InlineData("    grant_units:\n      - { type: halutz, n: -2 }\n", "grant_units[0].n", "at least 1")]
    [InlineData("    grant_units:\n      - { type: halutz, n: 1, at: nowhere }\n", "grant_units[0].at", "unknown city")]
    [InlineData("    grant_units:\n      - { type: halutz, n: 1, at: site_a }\n", "grant_units[0].at", "sites cannot be grant targets")]
    [InlineData("    city_effects:\n      - { city: nowhere, field: job_slots, delta: 1 }\n", "city_effects[0].city", "unknown city")]
    [InlineData("    city_effects:\n      - { city: town_a, field: morale, delta: 1 }\n", "city_effects[0].field", "housing_capacity, job_slots or condition")]
    [InlineData("    city_effects:\n      - { city: town_a, field: job_slots, delta: 0 }\n", "city_effects[0].delta", "must not be 0")]
    [InlineData("    city_effects:\n      - { city: town_a, field: condition, delta: 0 }\n", "city_effects[0].delta", "must not be 0")]
    [InlineData("    city_effects:\n      - { city: town_a, field: housing_capacity, delta: 1.5 }\n", "city_effects[0].delta", "whole number")]
    [InlineData("    city_effects:\n      - { city: town_a, field: job_slots, delta: 3000000000 }\n", "city_effects[0].delta", "whole number")]
    [InlineData("    city_effects:\n      - { city: town_a, field: condition, delta: 1.5 }\n", "city_effects[0].delta", "from -1 to 1")]
    [InlineData("    city_effects:\n      - { city: town_a, field: condition, delta: -1.01 }\n", "city_effects[0].delta", "from -1 to 1")]
    [InlineData("    city_effects:\n      - { city: town_a, field: condition, delta: .nan }\n", "city_effects[0].delta", "from -1 to 1")]
    public void TheLoaderRefusesAnUnknownTarget(string structured, string field, string message)
    {
        var ex = Refused(structured);
        Assert.Equal("events/arms.yaml", ex.File);
        Assert.Equal("events[arms].options[accept]." + field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // R-EFX-003: with no `at`, the grant needs the one city that has the military camp; otherwise the option must name one.
    [Fact]
    public void AGrantWithNoPlaceNeedsTheMilitaryCamp()
    {
        var ex = Assert.Throws<ContentException>(() => Load(Event(Grants), "[hakhshara]"));
        Assert.Equal("events[arms].options[accept].grant_units[0].at", ex.Field);
        Assert.Contains("military camp", ex.Message);
    }

    // R-EFX-005: an option of a sensitive event carries no grant and no capacity, slot or condition gain; a cut stays allowed.
    [Theory]
    [InlineData("    grant_units:\n      - { type: halutz, n: 1 }\n", "grant_units[0]")]
    [InlineData("    city_effects:\n      - { city: town_a, field: housing_capacity, delta: 1 }\n", "city_effects[0]")]
    [InlineData("    city_effects:\n      - { city: town_a, field: job_slots, delta: 2 }\n", "city_effects[0]")]
    [InlineData("    city_effects:\n      - { city: town_a, field: condition, delta: 0.1 }\n", "city_effects[0]")]
    public void ASensitiveOptionGrantsNothing(string structured, string field)
    {
        var ex = Refused(structured, "war-casualties");
        Assert.Equal("events[arms].options[accept]." + field, ex.Field);
        Assert.Contains("sensitive events cannot reward", ex.Message);
    }

    // R-EFX-005: a sensitive option may still cut a city's capacity, slots or condition.
    [Fact]
    public void ASensitiveOptionMayCut()
    {
        Assert.Single(Load(Event("    city_effects:\n      - { city: town_a, field: job_slots, delta: -1 }\n", "terror")).Events);
        Assert.Single(Load(Event("    city_effects:\n      - { city: town_a, field: housing_capacity, delta: -1 }\n", "war-casualties")).Events);
        Assert.Single(Load(Event("    city_effects:\n      - { city: town_a, field: condition, delta: -0.2 }\n", "massacre")).Events);
    }

    // R-EFX-004 with R-HOUSE-001: a condition effect moves only that city's housing condition, and it stays from 0 to 1; the
    // cause carries the change that applied. The fixture starts every city at housing_start_condition.
    [Theory]
    [InlineData(0.25)]
    [InlineData(-0.2)]
    [InlineData(1.0)]     // clipped at 1
    [InlineData(-1.0)]   // clipped at 0
    public void AConditionEffectStaysFrom0To1(double delta)
    {
        var c = Load(Event(FormattableString.Invariant($"    city_effects:\n      - {{ city: town_a, field: condition, delta: {delta} }}\n")));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var x = s.Cities["town_a"];
        var other = s.Cities["town_b"].Canonical();
        var start = x.Condition!.Value;
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, s, "arms", "accept", why));
        var y = s.Cities["town_a"];
        var want = Math.Min(1.0, Math.Max(0.0, start + delta));
        Assert.Equal(want, y.Condition!.Value, 12);
        Assert.Equal((x.HousingCapacity, x.JobSlots, x.Civilians, x.Employed, x.Refugees, x.Recruits, x.Craftsmen, x.JobsDefault),
            (y.HousingCapacity, y.JobSlots, y.Civilians, y.Employed, y.Refugees, y.Recruits, y.Craftsmen, y.JobsDefault));
        Assert.Equal(other, s.Cities["town_b"].Canonical());   // R-HOUSE-001: per city
        var cause = Assert.Single(why, w => w.Target == "town_a.condition");
        Assert.Equal(want - start, cause.Delta, 12);
    }

    // R-EFX-004: housing capacity never drops below 0; the cause carries the cut that applied.
    [Fact]
    public void HousingCapacityStopsAtZero()
    {
        var c = Load(Event("    city_effects:\n      - { city: town_a, field: housing_capacity, delta: -1000 }\n"));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var old = s.Cities["town_a"].HousingCapacity;
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, s, "arms", "accept", why));
        Assert.Equal(0, s.Cities["town_a"].HousingCapacity);
        Assert.Contains(why, w => w.Target == "town_a.housing_capacity" && w.Delta == -old);
    }

    // Structured effects act on GDD records, so the legacy loop (no cities, no units) refuses them at load.
    [Fact]
    public void TheLegacyLoopRefusesThem()
    {
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["arms.yaml"] = Event(Grants) })));
        Assert.Equal("events[arms].options[accept].grant_units", ex.Field);
        Assert.Contains("GDD loop", ex.Message);
    }

    // R-EVT-003 with R-EFX-003: a delegated option applies the same structured effects as a chosen one.
    [Fact]
    public void DelegationAppliesThemToo()
    {
        var c = Load(Event(Grants).Replace("from: 1948-05-15", "anchor: hard\nfrom: 1948-05-15"));
        var chosen = Simulation.NewGame(c, 1, Mode.Historical);
        var baseline = chosen.Clone();
        var delegated = chosen.Clone();
        Assert.Null(EventSystem.Choose(c, chosen, "arms", "accept", new List<Cause>()));
        EventSystem.Delegate(c, delegated, new List<Cause>());   // Historical mode: a hard anchor takes its historical option
        Assert.Equal("accept", delegated.ChosenOptions["arms"]);
        Assert.Equal(baseline.NextId + 3, delegated.NextId);   // the grants applied, not merely the same nothing
        Assert.Equal(baseline.Units.Count + 3, delegated.Units.Count);
        Assert.Equal(chosen.Hash(), delegated.Hash());
    }

    // R-EVT-005: the preview lists the structured effects after the numeric ones, in words, in the order they apply.
    [Fact]
    public void ThePreviewListsThem()
    {
        var c = Load(Event(Grants));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var lines = EventSystem.OptionEffects(c, s, c.Events.Single().Options[0]);
        Assert.Equal(new[] { "fx_reserves", "halutz", "halutz", "town_a.housing_capacity", "town_a.job_slots", "town_b.condition" }, lines.Select(l => l.Target));
        Assert.Equal(new[] { -10.0, 2, 1, 3, -100, -0.05 }, lines.Select(l => l.Delta));
        Assert.Equal("2 halutz units at Town A", lines[1].Message);
        Assert.Equal("1 halutz unit at Town B", lines[2].Message);
        Assert.Equal("Town A: housing capacity +3", lines[3].Message);
        Assert.Equal("Town A: job slots -100", lines[4].Message);
        Assert.Equal("Town B: housing condition -5.0%", lines[5].Message);
    }

    // A game with a granted unit replays from its save to the same hash.
    [Fact]
    public void ASaveWithAGrantedUnitReplays()
    {
        var c = Load(Event(Grants));
        var g = GameSession.New(c, 7, Mode.Historical);
        g.Step(new Command[] { new ChooseEventOption("arms", "accept") });
        g.Step(new Command[0]);
        Assert.Contains(g.State.Units.Values, u => u.Q == 2 && u.R == 1);
        var loaded = GameSession.Load(c, g.Save.Write(), out var warnings);
        Assert.Empty(warnings);
        // Field by field first, so a persistence bug says what broke; then the hash.
        Assert.Equal(g.State.Units.Values.OrderBy(u => u.Id).Select(u => (u.Id, u.Type, u.Q, u.R)),
            loaded.State.Units.Values.OrderBy(u => u.Id).Select(u => (u.Id, u.Type, u.Q, u.R)));
        Assert.Equal(g.State.NextId, loaded.State.NextId);
        Assert.Equal((g.State.Cities["town_a"].HousingCapacity, g.State.Cities["town_a"].JobSlots, g.State.Cities["town_b"].Condition),
            (loaded.State.Cities["town_a"].HousingCapacity, loaded.State.Cities["town_a"].JobSlots, loaded.State.Cities["town_b"].Condition));
        Assert.Equal(g.State.Hash(), loaded.State.Hash());
    }
}
